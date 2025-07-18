#if !RUKHANKA_NO_DEFORMATION_SYSTEM

using Rukhanka.Toolbox;
using Unity.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{

[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
[UpdateInGroup(typeof(RukhankaDeformationSystemGroup))]
[UpdateAfter(typeof(SkinnedMeshPreparationSystem))]
public partial class MeshDeformationSystem: SystemBase
{
	GraphicsBuffer meshVertexDataCB;
	GraphicsBuffer meshBoneWeightDataCB;
	GraphicsBuffer meshBlendShapesDataCB;
	GraphicsBuffer newMeshBonesPerVertexCB;
	GraphicsBuffer frameVertexSkinningWorkloadCB;
	
	GraphicsBuffer finalDeformedVerticesCB;
#if ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
	//	Double buffer scheme. Current frame will read previous frame data to calculate motion delta
	GraphicsBuffer finalDeformedVerticesCB1;
	uint bufferSwapCounter = 0;
#endif
	
	SparseUploader sparseUploader;
	FrameFencedGPUBufferPool<SkinMatrix> frameSkinMatricesBuffer;
	FrameFencedGPUBufferPool<float> frameBlendShapeWeightsBuffer;
	FrameFencedGPUBufferPool<MeshFrameDeformationDescription> frameMeshDeformationDescriptionBuffer;
	
	ComputeShader meshDeformationSystemCS;
	ComputeKernel fillInitialMeshDataKernel;
	ComputeKernel fillInitialMeshBlendShapesKernel;
	ComputeKernel createPerVertexDeformationWorkloadKernel;
	ComputeKernel skinningKernel;
	
	EntitiesGraphicsSystem entitiesGraphicsSystem;
	GPUAnimationSystem gpuAnimationSystem;
	
	SharedComponentTypeHandle<RenderMeshArray> renderMeshArrayTypeHandle;

	struct InputMeshVertexDesc
	{
		public VertexAttribute vertexAttribute;
		public VertexAttributeFormat vertexAttributeFormat;
		public int streamIndex;
		public int dimension;
	}
	
	static readonly InputMeshVertexDesc[] inputMeshVertexDesc =
	{
		new () {vertexAttribute = VertexAttribute.Position, vertexAttributeFormat = VertexAttributeFormat.Float32, streamIndex = 0, dimension = 3 },
		new () {vertexAttribute = VertexAttribute.Normal, vertexAttributeFormat = VertexAttributeFormat.Float32, streamIndex = 0, dimension = 3 },
		new () {vertexAttribute = VertexAttribute.Tangent, vertexAttributeFormat = VertexAttributeFormat.Float32, streamIndex = 0, dimension = 4 },
	};
	
	EntityQuery activeDeformedEntitiesQuery, activeDeformedMeshesQuery;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	protected override void OnCreate()
	{
#if !HYBRID_RENDERER_DISABLED
        if (!EntitiesGraphicsUtils.IsEntitiesGraphicsSupportedOnSystem())
#endif
		{
			Enabled = false;
			return;
		}
        
		renderMeshArrayTypeHandle = GetSharedComponentTypeHandle<RenderMeshArray>();
		entitiesGraphicsSystem = World.GetExistingSystemManaged<EntitiesGraphicsSystem>();
		gpuAnimationSystem = World.GetExistingSystemManaged<GPUAnimationSystem>();
		frameSkinMatricesBuffer = new (0xffff, GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.None);
		frameBlendShapeWeightsBuffer = new (0xffff, GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.LockBufferForWrite);
		frameMeshDeformationDescriptionBuffer = new (0xffff, GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.LockBufferForWrite);
		sparseUploader = new (null);
		
		activeDeformedEntitiesQuery = SystemAPI.QueryBuilder()
			.WithAll<AnimatedSkinnedMeshComponent>()
			.Build();
		
		activeDeformedMeshesQuery = SystemAPI.QueryBuilder()
			.WithAll<AnimatedRendererComponent>()
			.Build();
		
		RequireForUpdate(activeDeformedEntitiesQuery);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	protected override void OnDestroy()
	{
#if !HYBRID_RENDERER_DISABLED
        if (!EntitiesGraphicsUtils.IsEntitiesGraphicsSupportedOnSystem())
#endif
		{ return; }
		
		meshVertexDataCB?.Dispose();
		meshBoneWeightDataCB?.Dispose();
		meshBlendShapesDataCB?.Dispose();
		newMeshBonesPerVertexCB?.Dispose();
		frameVertexSkinningWorkloadCB?.Dispose();
		
		finalDeformedVerticesCB?.Dispose();
	#if ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
		finalDeformedVerticesCB1?.Dispose();
	#endif
		
		frameMeshDeformationDescriptionBuffer?.Dispose();
		frameSkinMatricesBuffer?.Dispose();
		frameBlendShapeWeightsBuffer?.Dispose();
		sparseUploader.Dispose();
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	protected override void OnUpdate()
	{
		renderMeshArrayTypeHandle.Update(this);
		
		ref var deformationRuntimeData = ref SystemAPI.GetSingletonRW<DeformationRuntimeData>().ValueRW;
		
		var resetFrameCountersJH = ResetFrameCounters(ref deformationRuntimeData, Dependency);
		var prepareSkinningDataJH = PrepareMeshGPUSkinningData(ref deformationRuntimeData, resetFrameCountersJH);
		
		//	Complete previous jobs here, because we need to know skin matrix buffer and blend shape weights sizes here to resize GPU buffers
		prepareSkinningDataJH.Complete();
		
		var (copySkinMatricesToGPUBufferJH, skinMatrixThreadedUploader) = CopySkinMatricesToGPUBuffer(deformationRuntimeData, default);
		var copyBlendShapeWeightsToGPUBufferJH = CopyBlendShapeWeightsToGPUBuffer(deformationRuntimeData, default);
		var copyFrameDeformationDataToGPUBuffersJH = JobHandle.CombineDependencies(copySkinMatricesToGPUBufferJH, copyBlendShapeWeightsToGPUBufferJH);
		
		//	Complete dependency second time before compute shader execution. Need to make sure that SkinMatrix GPU buffer data writes
		//	is complete.
		copyFrameDeformationDataToGPUBuffersJH.Complete();
		
		sparseUploader.EndAndCommit(skinMatrixThreadedUploader);
		frameBlendShapeWeightsBuffer.UnlockBufferAfterWrite(deformationRuntimeData.frameBlendShapeWeightsCount);
		frameMeshDeformationDescriptionBuffer.UnlockBufferAfterWrite(deformationRuntimeData.frameActiveDeformedMeshesCount);
		
		CopyNewMeshesToInitialMeshDataBuffer(deformationRuntimeData);
		
		//	Need to inject GPU animation system here, for GPU animated entities skin matrices computation directly to skin matrix frame GPU buffer
		//	It is not pretty approach, I know
		gpuAnimationSystem?.BuildSkinMatrices(deformationRuntimeData.entityToSMRFrameDataMap, frameSkinMatricesBuffer);
		
	#if RUKHANKA_INPLACE_SKINNING
		SetInplaceSkinningGlobalBuffers();
	#else
		ScheduleSkinningDispatch(deformationRuntimeData);
	#endif
		
		frameSkinMatricesBuffer.EndFrame();
		sparseUploader.FrameCleanup();
		frameBlendShapeWeightsBuffer.EndFrame();
		frameMeshDeformationDescriptionBuffer.EndFrame();
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	unsafe JobHandle ResetFrameCounters(ref DeformationRuntimeData drd, JobHandle dependsOn)
	{
		var resetFrameCountersJob = new ResetFrameCountersJob()
		{
			frameActiveDeformedMeshesCounter = new UnsafeAtomicCounter32(UnsafeUtility.AddressOf(ref drd.frameActiveDeformedMeshesCount))
		};
		
		var rv = resetFrameCountersJob.Schedule(dependsOn);
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void SetInplaceSkinningGlobalBuffers()
	{
		Shader.SetGlobalBuffer(ShaderID_frameDeformedMeshes, frameMeshDeformationDescriptionBuffer);
		Shader.SetGlobalBuffer(ShaderID_frameSkinMatrices, frameSkinMatricesBuffer);
		Shader.SetGlobalBuffer(ShaderID_frameBlendShapeWeights, frameBlendShapeWeightsBuffer);
		Shader.SetGlobalBuffer(ShaderID_inputBlendShapes, meshBlendShapesDataCB);
		Shader.SetGlobalBuffer(ShaderID_inputBoneInfluences, meshBoneWeightDataCB);
		Shader.SetGlobalBuffer(ShaderID_inputMeshVertexData, meshVertexDataCB);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void ScheduleSkinningDispatch(in DeformationRuntimeData drd)
	{
		var frameDeformedVerticesCount = drd.frameDeformedVerticesCount;
		frameVertexSkinningWorkloadCB = ComputeBufferTools.CreateOrGrowGraphicsBuffer<uint>(frameVertexSkinningWorkloadCB, GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.None, frameDeformedVerticesCount, false);
		
		//	Schedule workload generation dispatch if we have visible/existing skinned mesh renderers
		if (drd.frameActiveDeformedMeshesCount > 0)
		{
			var cs0 = createPerVertexDeformationWorkloadKernel.computeShader;
			cs0.SetBuffer(createPerVertexDeformationWorkloadKernel, ShaderID_outFramePerVertexWorkload, frameVertexSkinningWorkloadCB);
			cs0.SetBuffer(createPerVertexDeformationWorkloadKernel, ShaderID_frameDeformedMeshes, frameMeshDeformationDescriptionBuffer);
			cs0.SetInt(ShaderID_totalDeformedMeshesCount, drd.frameActiveDeformedMeshesCount);
			createPerVertexDeformationWorkloadKernel.Dispatch(drd.frameActiveDeformedMeshesCount, 1, 1);
		}
		
		var deformedVerticesBufferSize = frameDeformedVerticesCount + drd.maximumVerticesAcrossAllRegisteredMeshes;
	#if RUKHANKA_HALF_DEFORMED_DATA
		finalDeformedVerticesCB = ComputeBufferTools.CreateOrGrowGraphicsBuffer<PackedDeformedVertex>(finalDeformedVerticesCB, GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.None, deformedVerticesBufferSize, false);
	#else
		finalDeformedVerticesCB = ComputeBufferTools.CreateOrGrowGraphicsBuffer<DeformedVertex>(finalDeformedVerticesCB, GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.None, deformedVerticesBufferSize, false);
	#endif
		
		var outDeformedVerticesCB = finalDeformedVerticesCB;
		
		//	Schedule skinning even for zero visible meshes, because we need to actualize void mesh zone to properly cull invisible meshes
		if (deformedVerticesBufferSize > 0)
		{
			var cs1 = skinningKernel.computeShader;
			cs1.SetBuffer(skinningKernel, ShaderID_frameDeformedMeshes, frameMeshDeformationDescriptionBuffer);
			cs1.SetBuffer(skinningKernel, ShaderID_framePerVertexWorkload, frameVertexSkinningWorkloadCB);
			cs1.SetBuffer(skinningKernel, ShaderID_inputMeshVertexData, meshVertexDataCB);
			cs1.SetBuffer(skinningKernel, ShaderID_inputBoneInfluences, meshBoneWeightDataCB);
			cs1.SetBuffer(skinningKernel, ShaderID_inputBlendShapes, meshBlendShapesDataCB);
			cs1.SetBuffer(skinningKernel, ShaderID_frameSkinMatrices, frameSkinMatricesBuffer);
			cs1.SetBuffer(skinningKernel, ShaderID_frameBlendShapeWeights, frameBlendShapeWeightsBuffer);
			cs1.SetBuffer(skinningKernel, ShaderID_outDeformedVertices, outDeformedVerticesCB);
			cs1.SetInt(ShaderID_totalSkinnedVerticesCount, frameDeformedVerticesCount);
			cs1.SetInt(ShaderID_voidMeshVertexCount, drd.maximumVerticesAcrossAllRegisteredMeshes);
			skinningKernel.Dispatch(deformedVerticesBufferSize, 1, 1);
		}
		
		Shader.SetGlobalBuffer(ShaderID_DeformedMeshData, outDeformedVerticesCB);
		
	#if ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
		if (finalDeformedVerticesCB1 != null)
			Shader.SetGlobalBuffer(ShaderID_PreviousFrameDeformedMeshData, finalDeformedVerticesCB1);
		(finalDeformedVerticesCB, finalDeformedVerticesCB1) = (finalDeformedVerticesCB1, finalDeformedVerticesCB);
		bufferSwapCounter += 1;
	#endif
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	(JobHandle, ThreadedSparseUploader) CopySkinMatricesToGPUBuffer(in DeformationRuntimeData drd, JobHandle dependsOn)
	{
		var skinMatrixCount = drd.frameSkinMatrixCount;
		frameSkinMatricesBuffer.Grow(skinMatrixCount);
		frameSkinMatricesBuffer.BeginFrame();
		sparseUploader.ReplaceBuffer(frameSkinMatricesBuffer);
		
		var q = SystemAPI.QueryBuilder()
			.WithAll<AnimatedSkinnedMeshComponent>()
			.Build();
		var numEntities = q.CalculateEntityCount();
		
		var skinMatrixDataSize = skinMatrixCount * UnsafeUtility.SizeOf<SkinMatrix>();
		var maxDataUploadSize = drd.maximumSkinMatrixCountAcrossAllRegisteredMeshes * UnsafeUtility.SizeOf<SkinMatrix>();
		var gpuSkinMatrixThreadedUploader = sparseUploader.Begin(skinMatrixDataSize, maxDataUploadSize, numEntities);
		
		var isEditor = (this.World.Flags & WorldFlags.Editor) == WorldFlags.Editor;
		var copySkinMatricesToGPUJob = new CopySkinMatricesToGPUJob()
		{
			entityToSMRFrameDataMap = drd.entityToSMRFrameDataMap,
			mappedGPUSkinMatrixBuffer = gpuSkinMatrixThreadedUploader,
			gpuAnimationEngineTag = SystemAPI.GetComponentLookup<GPUAnimationEngineTag>(true),
			isEditor = isEditor,
		};
		
		var jh = copySkinMatricesToGPUJob.ScheduleParallel(dependsOn);
		return (jh, gpuSkinMatrixThreadedUploader);
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	JobHandle CopyBlendShapeWeightsToGPUBuffer(in DeformationRuntimeData drd, JobHandle dependsOn)
	{
		var blendShapeWeightsCount = drd.frameBlendShapeWeightsCount;
		frameBlendShapeWeightsBuffer.Grow(blendShapeWeightsCount);
		frameBlendShapeWeightsBuffer.BeginFrame();
		
		var gpuBufferOutArr = frameBlendShapeWeightsBuffer.LockBufferForWrite(0, blendShapeWeightsCount);
		
		var copyBlendShapeWeightsToGPUJob = new CopyBlendShapeWeightToGPUJob()
		{
			entityToSMRFrameDataMap = drd.entityToSMRFrameDataMap,
			mappedGPUBlendShapeWeightsBuffer = gpuBufferOutArr
		};
		
		var jh = copyBlendShapeWeightsToGPUJob.ScheduleParallel(dependsOn);
		return jh;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	JobHandle PrepareMeshGPUSkinningData(ref DeformationRuntimeData drd, JobHandle dependsOn)
	{
		var deformedMeshCount = activeDeformedEntitiesQuery.CalculateEntityCount();
		frameMeshDeformationDescriptionBuffer.Grow(deformedMeshCount);
		frameMeshDeformationDescriptionBuffer.BeginFrame();
		var gpuBufferMeshDeformationOutArr = frameMeshDeformationDescriptionBuffer.LockBufferForWrite(0, deformedMeshCount);
		
		var setDeformedMeshIndicesJH = SetDeformedMeshIndicesForRenderEntities(ref drd, dependsOn);
		var prepareSkinningDataJH = PrepareSkinningCommands(ref drd, gpuBufferMeshDeformationOutArr, dependsOn);
		
		var rv = JobHandle.CombineDependencies(setDeformedMeshIndicesJH, prepareSkinningDataJH);
		
		return rv;
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	JobHandle PrepareMeshGPUSkinningDataForInplaceSkinning(ref DeformationRuntimeData drd, JobHandle dependsOn)
	{
		var deformedMeshCount = activeDeformedMeshesQuery.CalculateEntityCount();
		frameMeshDeformationDescriptionBuffer.Grow(deformedMeshCount);
		frameMeshDeformationDescriptionBuffer.BeginFrame();
		var gpuBufferMeshDeformationOutArr = frameMeshDeformationDescriptionBuffer.LockBufferForWrite(0, deformedMeshCount);
		
		var setDeformedMeshIndicesJH = SetDeformedMeshIndicesForRenderEntities(ref drd, dependsOn);
		var prepareSkinningDataJH = PrepareSkinningCommands(ref drd, gpuBufferMeshDeformationOutArr, dependsOn);
		
		var rv = JobHandle.CombineDependencies(setDeformedMeshIndicesJH, prepareSkinningDataJH);
		
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	unsafe JobHandle PrepareSkinningCommands(ref DeformationRuntimeData drd, NativeArray<MeshFrameDeformationDescription> gpuBufferMeshDeformationOutArr, JobHandle dependsOn)
	{
		var prepareSkinningDataJob = new PrepareSkinningCommandsJob()
		{
			meshFrameDeformationData = gpuBufferMeshDeformationOutArr,
			entityToSMRFrameDataMap = drd.entityToSMRFrameDataMap,
			registeredSkinnedMeshes = drd.registeredSkinnedMeshesMap,
			frameActiveDeformedMeshesCounter = new UnsafeAtomicCounter32(UnsafeUtility.AddressOf(ref drd.frameActiveDeformedMeshesCount))
		};
		var jh = prepareSkinningDataJob.ScheduleParallel(dependsOn);
		return jh;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	unsafe JobHandle SetDeformedMeshIndicesForRenderEntities(ref DeformationRuntimeData drd, JobHandle dependsOn)
	{
		var setDeformedMeshIndexJob = new SetDeformedMeshIndicesJob()
		{
			frameDeformedVerticesCounter = new UnsafeAtomicCounter32(UnsafeUtility.AddressOf(ref drd.frameDeformedVerticesCount)),
			entityToSMRFrameDataMap = drd.entityToSMRFrameDataMap,
		#if ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
			currentFrameDeformedBufferIndex = (int)(bufferSwapCounter & 1)
		#endif
		};
		
		var jh = setDeformedMeshIndexJob.ScheduleParallel(dependsOn);
		return jh;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void InitComputeShaders()
	{
		if (meshDeformationSystemCS != null)
			return;
		
		meshDeformationSystemCS = Resources.Load<ComputeShader>("RukhankaMeshDeformation");
		fillInitialMeshDataKernel = new ComputeKernel(meshDeformationSystemCS, "CopyInitialMeshData");
		fillInitialMeshBlendShapesKernel = new ComputeKernel(meshDeformationSystemCS, "CopyInitialMeshBlendShapes");
		createPerVertexDeformationWorkloadKernel = new ComputeKernel(meshDeformationSystemCS, "CreatePerVertexDeformationWorkload");
		
		//	Pick skinning kernel depending on hardware capabilities (max workgroup size)
		var workGroupSizeX = math.ceilpow2(SystemInfo.maxComputeWorkGroupSizeX);
		if (workGroupSizeX != SystemInfo.maxComputeWorkGroupSizeX)
			workGroupSizeX >>= 1;
		
		skinningKernel = new ComputeKernel(meshDeformationSystemCS, $"Skinning_{workGroupSizeX}");
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CopyNewMeshesToInitialMeshDataBuffer(in DeformationRuntimeData drd)
	{
		if (drd.newSkinnedMeshesToRegister.IsEmpty)
			return;
		
		meshVertexDataCB = ComputeBufferTools.CreateOrGrowGraphicsBuffer<SourceMeshVertex>(meshVertexDataCB, GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.None, drd.totalSkinnedVerticesCount, true);
		meshBoneWeightDataCB = ComputeBufferTools.CreateOrGrowGraphicsBuffer<BoneWeight1>(meshBoneWeightDataCB, GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.None, drd.totalBoneWeightsCount, true);
		meshBlendShapesDataCB = ComputeBufferTools.CreateOrGrowGraphicsBuffer<DeformedVertex>(meshBlendShapesDataCB, GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.None, drd.totalBlendShapeVerticesCount, true);
		
		InitComputeShaders();
		var meshToBoneWeightsOffsetMap = CreateNewMeshesBoneIndicesComputeBuffer(drd);
		
		sparseUploader.ReplaceBuffer(meshBoneWeightDataCB);
		var boneWeightDataSize = drd.totalBoneWeightsCount * UnsafeUtility.SizeOf<BoneWeight1>();
		var tsu = sparseUploader.Begin(boneWeightDataSize, boneWeightDataSize, 1);

		foreach (var sm in drd.newSkinnedMeshesToRegister)
		{
			var batchMeshID = sm.Key;
			var skinnedMeshHash = sm.Value.Value.hash;
			var smd = drd.registeredSkinnedMeshesMap[skinnedMeshHash];
			var mesh = entitiesGraphicsSystem.GetMesh(batchMeshID);
			var boneWeightsOffsetForMesh = meshToBoneWeightsOffsetMap[batchMeshID];
			
			CopyMeshVertexData(smd, boneWeightsOffsetForMesh, mesh);
			CopyMeshBoneWeightsData(smd, mesh, tsu);
			CopyMeshBlendShapes(smd, mesh);
		}
		sparseUploader.EndAndCommit(tsu);
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	unsafe NativeHashMap<BatchMeshID, int> CreateNewMeshesBoneIndicesComputeBuffer(in DeformationRuntimeData drd)
	{
		var meshToBoneWeightsOffsetMap = new NativeHashMap<BatchMeshID, int>(0xff, Allocator.Temp);
		var newMeshesBonesPerVertexData = new NativeList<uint>(0xff, Allocator.Temp);
		
		foreach (var newMesh in drd.newSkinnedMeshesToRegister)
		{
			var baseVertexIndex = newMeshesBonesPerVertexData.Length;
			var mesh = entitiesGraphicsSystem.GetMesh(newMesh.Key);
			
			if (!HasSupportedVertexLayout(mesh))
				continue;
			
			meshToBoneWeightsOffsetMap[newMesh.Key] = baseVertexIndex;
			newMeshesBonesPerVertexData.Resize(baseVertexIndex + mesh.vertexCount, NativeArrayOptions.UninitializedMemory);
			
			ref var bwi = ref newMesh.Value.Value.boneWeightsIndices;
			//BurstAssert.IsTrue(bwi.Length == mesh.vertexCount, "Bone weights offsets array does not match vertex count.");
			UnsafeUtility.MemCpy(newMeshesBonesPerVertexData.GetUnsafePtr() + baseVertexIndex, bwi.GetUnsafePtr(), bwi.Length * 4);
		}
		
		newMeshBonesPerVertexCB = ComputeBufferTools.CreateOrGrowGraphicsBuffer<uint>(newMeshBonesPerVertexCB, GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.None, newMeshesBonesPerVertexData.Length, false);
		newMeshBonesPerVertexCB.SetData(newMeshesBonesPerVertexData.AsArray());
		return meshToBoneWeightsOffsetMap;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CopyMeshBlendShapes(SkinnedMeshDescription smd, Mesh mesh)
	{
		if (mesh.blendShapeCount == 0)
			return;
		
		using var meshAllBlendShapes = mesh.GetBlendShapeBuffer(BlendShapeBufferLayout.PerShape);
		var blendShapeVertexDeltaSize = UnsafeUtility.SizeOf<BlendShapeVertexDelta>();
		Assert.IsTrue(blendShapeVertexDeltaSize == meshAllBlendShapes.stride);
		
		var cs = fillInitialMeshBlendShapesKernel.computeShader;
		cs.SetBuffer(fillInitialMeshBlendShapesKernel, ShaderID_meshBlendShapesBuffer, meshAllBlendShapes);
		cs.SetBuffer(fillInitialMeshBlendShapesKernel, ShaderID_outInitialMeshBlendShapesData, meshBlendShapesDataCB);
		
		var deformedVertexSize = UnsafeUtility.SizeOf<DeformedVertex>();
		ComputeBufferTools.Clear(meshBlendShapesDataCB, (uint)(smd.baseBlendShapeIndex * deformedVertexSize), (uint)(smd.vertexCount * deformedVertexSize * mesh.blendShapeCount));
		
		for (var i = 0; i < mesh.blendShapeCount; ++i)
		{
			var bsr = mesh.GetBlendShapeBufferRange(i);
			cs.SetInt(ShaderID_inputBlendShapeVerticesCount, (int)(bsr.endIndex - bsr.startIndex) + 1);
			cs.SetInt(ShaderID_inputBlendShapeVertexOffset, (int)bsr.startIndex);
			cs.SetInt(ShaderID_outBlendShapeVertexOffset, smd.baseBlendShapeIndex + i * smd.vertexCount );
			fillInitialMeshBlendShapesKernel.Dispatch(smd.vertexCount, 1, 1);
		}
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CopyMeshBoneWeightsData(SkinnedMeshDescription smd, Mesh mesh, ThreadedSparseUploader boneWeightDataUploader)
	{
		var meshAllBoneWeights = mesh.GetAllBoneWeights();
		boneWeightDataUploader.AddUpload(meshAllBoneWeights, smd.baseBoneWeightIndex * UnsafeUtility.SizeOf<BoneWeight1>(), 1);
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CopyMeshVertexData(SkinnedMeshDescription smd, int meshBoneWeightsOffset, Mesh mesh)
	{
		//	Copy initial vertex data
		mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
		using var meshVertexBuffer = mesh.GetVertexBuffer(0);
		
		meshDeformationSystemCS.SetInt(ShaderID_totalMeshVertices, smd.vertexCount);
		meshDeformationSystemCS.SetInt(ShaderID_outDataVertexOffset, smd.baseVertex);
		var vertexBufferStride = mesh.GetVertexBufferStride(0);
		meshDeformationSystemCS.SetInt(ShaderID_inputVertexSizeInBytes, vertexBufferStride);
		meshDeformationSystemCS.SetInt(ShaderID_inputBonesWeightsDataOffset, meshBoneWeightsOffset);
		meshDeformationSystemCS.SetInt(ShaderID_outBonesWeightsDataOffset, smd.baseBoneWeightIndex);
		meshDeformationSystemCS.SetBuffer(fillInitialMeshDataKernel, ShaderID_meshVertexData, meshVertexBuffer);
		meshDeformationSystemCS.SetBuffer(fillInitialMeshDataKernel, ShaderID_outInitialDeformedMeshData, meshVertexDataCB);
		meshDeformationSystemCS.SetBuffer(fillInitialMeshDataKernel, ShaderID_meshBonesPerVertexData, newMeshBonesPerVertexCB);
		
		fillInitialMeshDataKernel.Dispatch(smd.vertexCount, 1, 1);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	bool HasSupportedVertexLayout(Mesh mesh)
	{
		if (mesh.vertexAttributeCount < inputMeshVertexDesc.Length)
		{
			Debug.LogError($"Unsupported vertex layout for deformations in mesh ({mesh.name}). Expecting {inputMeshVertexDesc.Length} attributes but mash has only {mesh.vertexAttributeCount}.");
			return false;
		}
		
		//	Check each attribute
		for (var i = 0; i < inputMeshVertexDesc.Length; ++i)
		{
			var attrib = mesh.GetVertexAttribute(i);
			var vd = inputMeshVertexDesc[i];
			
			if (attrib.attribute != vd.vertexAttribute)
			{
				Debug.LogError($"Attribute mismatch for deformations in mesh ({mesh.name}). Expecting '{vd.vertexAttribute}', got '{attrib.attribute}'.");
				return false;
			}
			
			if (attrib.format != vd.vertexAttributeFormat)
			{
				Debug.LogError($"Format mismatch for deformations in mesh ({mesh.name}). Expecting '{vd.vertexAttributeFormat}', got '{attrib.format}'.");
				return false;
			}
			
			if (attrib.dimension != vd.dimension)
			{
				Debug.LogError($"Attribute dimension mismatch for deformations in mesh ({mesh.name}). Expecting '{vd.dimension}', got '{attrib.dimension}'.");
				return false;
			}
			
			if (attrib.stream != vd.streamIndex)
			{
				Debug.LogError($"Attribute stream index mismatch for deformations in mesh ({mesh.name}). Expecting '{vd.streamIndex}', got '{attrib.stream}'.");
				return false;
			}
		}
		
		return true;
	}
}
}

#endif
