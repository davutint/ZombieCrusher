#if !RUKHANKA_NO_DEFORMATION_SYSTEM

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Rukhanka.Toolbox;
using Unity.Assertions;
using Unity.Rendering;
using Unity.Transforms;
using Hash128 = Unity.Entities.Hash128;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{

[UpdateInGroup(typeof(RukhankaDeformationSystemGroup))]
[UpdateAfter(typeof(SkinnedMeshPreparationSystem))]
[UpdateBefore(typeof(MeshDeformationSystem))]
public partial class GPUAnimationSystem: SystemBase
{
	ComputeShader animationEngineCS;
	//	Buffers for persistent GPU data. Equivalent for blob asset store
	GraphicsBuffer animationClipsGB;
	GraphicsBuffer avatarMasksGB;
	GraphicsBuffer rigDefinitionGB;
	GraphicsBuffer rigHumanAvatarRotationDataGB;
	GraphicsBuffer rigBonesGB;
	GraphicsBuffer skinnedMeshBoneDataGB;
	
	SparseUploaderPool sparseUploaderPool;
	
	//	Buffers for per frame animation data (workload, animation to process). Equivalent for animation components 
	FrameFencedGPUBufferPool<GPUStructures.AnimationToProcess> frameAnimationToProcessGB;
	FrameFencedGPUBufferPool<GPUStructures.AnimatedBoneWorkload> framePerBoneAnimationWorkloadGB;
	FrameFencedGPUBufferPool<GPUStructures.AnimationJob> frameRigAnimationJobsGB;
	FrameFencedGPUBufferPool<GPUStructures.SkinnedMeshWorkload> frameSkinMatrixWorkloadGB;
	
	//	Buffers filled by compute shaders
	GraphicsBuffer animatedBonesGB;
	GraphicsBuffer rigSpaceAnimatedBonesGB;
	
	ComputeKernel processAnimationKernel;
	ComputeKernel makeRigSpaceBoneTransformsKernel;
	ComputeKernel makeSkinMatricesKernel;
	ComputeKernel copyBufferKernel;
	ComputeKernel copyMeshDataKernel;
	
	NativeParallelHashMap<Hash128, BlobAssetReference<BoneRemapTableBlob>> skinnedMeshToRigBoneRemapTables;
	EntityQuery gpuAnimatedRigQuery;
	
#if RUKHANKA_DEBUG_INFO
	BoneVisualizationSystem boneVisualizationSystem;
#endif
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	protected override void OnCreate()
	{
		if (!SystemInfo.supportsComputeShaders)
		{
			Debug.LogWarning("Compute shaders is not supported on current platform. GPU animation engine is disabled.");
			Enabled = false;
			return;
		}
		
#if !HYBRID_RENDERER_DISABLED
        if (!EntitiesGraphicsUtils.IsEntitiesGraphicsSupportedOnSystem())
#endif
		{
			Enabled = false;
			return;
		}
		
		skinnedMeshToRigBoneRemapTables = new (0xff, Allocator.Persistent);
		
		CreateComputeBuffers();
		
		animationEngineCS = Resources.Load<ComputeShader>("GPUAnimationEngine");
		processAnimationKernel = new ComputeKernel(animationEngineCS, "ProcessAnimations");
		makeRigSpaceBoneTransformsKernel = new ComputeKernel(animationEngineCS, "MakeRigSpaceBoneTransforms");
		makeSkinMatricesKernel = new ComputeKernel(animationEngineCS, "ComputeSkinMatrices");
		
		gpuAnimatedRigQuery = new EntityQueryBuilder(Allocator.Temp)
			.WithAll<RigDefinitionComponent, AnimationToProcessComponent, GPUAnimationEngineTag>()
			.WithNone<CullAnimationsTag>()
			.WithOptions(EntityQueryOptions.IncludeDisabledEntities)
			.Build(EntityManager);
		
		Assert.IsTrue(UnsafeUtility.SizeOf<GPUStructures.KeyFrame>() == UnsafeUtility.SizeOf<KeyFrame>());
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	protected override void OnUpdate()
	{
		ref var runtimeAnimationData = ref SystemAPI.GetSingletonRW<GPURuntimeAnimationData>().ValueRW;
		
		PrepareFrameAnimationData(ref runtimeAnimationData, Dependency);
		DispatchAnimationCalculation(ref runtimeAnimationData);
		SetBuffersForBoneRenderer(ref runtimeAnimationData);
		GPUBuffersEndFrame();
		sparseUploaderPool.FrameCleanup();
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	protected override void OnDestroy()
	{
		animationClipsGB?.Release();
		avatarMasksGB?.Release();
		rigDefinitionGB?.Release();
		rigHumanAvatarRotationDataGB?.Release();
		rigBonesGB?.Release();
		skinnedMeshBoneDataGB?.Release();
		
		sparseUploaderPool?.Dispose();
		
		frameAnimationToProcessGB?.Dispose();
		framePerBoneAnimationWorkloadGB?.Dispose();
		frameRigAnimationJobsGB?.Dispose();
		frameSkinMatrixWorkloadGB?.Dispose();	
		
		animatedBonesGB?.Dispose();
		rigSpaceAnimatedBonesGB?.Dispose();
		
		if (skinnedMeshToRigBoneRemapTables.IsCreated)
		{
			foreach (var remapTable in skinnedMeshToRigBoneRemapTables)
			{
				remapTable.Value.Dispose();
			}
		}
		skinnedMeshToRigBoneRemapTables.Dispose();
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CreateComputeBuffers()
	{
		var bufferInitialCapacity = 0x4;
		frameAnimationToProcessGB = new (bufferInitialCapacity, GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.LockBufferForWrite);
		framePerBoneAnimationWorkloadGB = new (bufferInitialCapacity, GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.LockBufferForWrite);
		frameRigAnimationJobsGB = new (bufferInitialCapacity, GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.LockBufferForWrite);
		frameSkinMatrixWorkloadGB = new (bufferInitialCapacity, GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.LockBufferForWrite);
		
		animatedBonesGB = new (GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.None, bufferInitialCapacity, UnsafeUtility.SizeOf<GPUStructures.BoneTransform>());
		rigSpaceAnimatedBonesGB = new (GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.None, bufferInitialCapacity, UnsafeUtility.SizeOf<GPUStructures.BoneTransform>());
		
		//	Persistent data buffers
		var persistentBuffersInitialCapacity = 0x4;
		animationClipsGB = new GraphicsBuffer(GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.None, persistentBuffersInitialCapacity, UnsafeUtility.SizeOf<uint>());
		avatarMasksGB = new GraphicsBuffer(GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.None, persistentBuffersInitialCapacity, UnsafeUtility.SizeOf<uint>());
		rigDefinitionGB = new GraphicsBuffer(GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.None, persistentBuffersInitialCapacity, UnsafeUtility.SizeOf<GPUStructures.RigDefinition>());
		rigHumanAvatarRotationDataGB = new GraphicsBuffer(GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.None, persistentBuffersInitialCapacity, UnsafeUtility.SizeOf<GPUStructures.HumanRotationData>());
		rigBonesGB = new GraphicsBuffer(GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.None, persistentBuffersInitialCapacity, UnsafeUtility.SizeOf<GPUStructures.RigBone>());
		skinnedMeshBoneDataGB = new GraphicsBuffer(GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.None, persistentBuffersInitialCapacity, UnsafeUtility.SizeOf<GPUStructures.SkinnedMeshBoneData>());
		
		sparseUploaderPool = new SparseUploaderPool();
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	unsafe JobHandle CalculateFrameWorksetData(ref GPURuntimeAnimationData rad, JobHandle dependsOn)
	{
		//	All following jobs should be sequential. Interlocked math for a few counters are significantly slower then
		//	sequential execution
		
		//	Workload sizes for animation calculation
		var computeRigsWorkloadSizesJob = new ComputeFrameRigWorkloadSizesJob()
		{
			frameEntityAnimatedDataOffsetsMap = rad.frameEntityAnimatedDataOffsetsMap,
			frameAnimatedBonesCounter = (uint*)UnsafeUtility.AddressOf(ref rad.frameAnimatedBonesCounter),
			frameAnimatedRigsCounter = (uint*)UnsafeUtility.AddressOf(ref rad.frameAnimatedRigsCounter),
			frameAnimationToProcessCounter = (uint*)UnsafeUtility.AddressOf(ref rad.frameAnimationToProcessCounter)
		};
		var computeRigsWorkloadSizesJH = computeRigsWorkloadSizesJob.Schedule(gpuAnimatedRigQuery, dependsOn);
		
		//	Workload sizes for skin matrices calculation
		var computeSkinnedMeshCountJob = new ComputeFrameSkinnedMeshWorkloadSizesJob()
		{
			frameSkinnedMeshesCounter = (uint*)UnsafeUtility.AddressOf(ref rad.frameSkinnedMeshesCounter),
			gpuAnimationEngineTagLookup = SystemAPI.GetComponentLookup<GPUAnimationEngineTag>(true)
		};
		var computeSkinnedMeshCountJH = computeSkinnedMeshCountJob.Schedule(dependsOn);
		
		var rv = JobHandle.CombineDependencies(computeRigsWorkloadSizesJH, computeSkinnedMeshCountJH);
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void GPUBuffersBeginFrame()
	{
		frameAnimationToProcessGB.BeginFrame();
		framePerBoneAnimationWorkloadGB.BeginFrame();
		frameRigAnimationJobsGB.BeginFrame();
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void GPUBuffersEndFrame()
	{
		frameAnimationToProcessGB.EndFrame();
		framePerBoneAnimationWorkloadGB.EndFrame();
		frameRigAnimationJobsGB.EndFrame();
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	unsafe void FillFrameSkinMatrixCalculationWorkloadGPUBuffers
	(
		ref GPURuntimeAnimationData rad,
		in NativeParallelHashMap<Entity, SkinnedMeshRendererFrameDeformationData> entityToSMRFrameDataMap
	)
	{
		var frameSkinMatrixWorkloadBuf = frameSkinMatrixWorkloadGB.LockBufferForWrite(0, (int)rad.frameSkinnedMeshesCounter);
		rad.frameSkinnedMeshesCounter = 0;
		var frameSkinnedMeshesAtomicCounter = new UnsafeAtomicCounter32(UnsafeUtility.AddressOf(ref rad.frameSkinnedMeshesCounter));
		
		var fillFrameSkinmatrixWorkloadBuffersJob = new FillFrameSkinMatrixWorkloadBuffersJob()
		{
			frameSkinnedMeshesAtomicCounter = frameSkinnedMeshesAtomicCounter,
			frameSkinMatrixWorkloadBuf = frameSkinMatrixWorkloadBuf,
			skinnedMeshDataMap = rad.skinnedMeshesDataMap,
			rigDefLookup = SystemAPI.GetComponentLookup<RigDefinitionComponent>(true),
			gpuAnimationEngineTagLookup = SystemAPI.GetComponentLookup<GPUAnimationEngineTag>(true),
			localToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true),
			entityToSMRFrameDataMap = entityToSMRFrameDataMap,
			frameEntityAnimatedDataOffsetsMap = rad.frameEntityAnimatedDataOffsetsMap
		};
		fillFrameSkinmatrixWorkloadBuffersJob.ScheduleParallel(new JobHandle()).Complete();
		
		frameSkinMatrixWorkloadGB.UnlockBufferAfterWrite((int)rad.frameSkinnedMeshesCounter);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void FillFrameAnimationCalculationWorkloadGPUBuffers(ref GPURuntimeAnimationData rad)
	{
		var animatedBonesWorkloadBuf = framePerBoneAnimationWorkloadGB.LockBufferForWrite(0, (int)rad.frameAnimatedBonesCounter);
		var animationToProcessBuf = frameAnimationToProcessGB.LockBufferForWrite(0, (int)rad.frameAnimationToProcessCounter);
		var rigAnimationJobsBuf = frameRigAnimationJobsGB.LockBufferForWrite(0, (int)rad.frameAnimatedRigsCounter);
		
		//	Fill GPU buffer data for current frame animation calculation
		var fillFrameAnimatedRigWorkloadDataJob = new FillFrameAnimatedRigWorkloadBuffersJob()
		{
			animatedBonesWorkloadBuf = animatedBonesWorkloadBuf,
			animationToProcessBuf = animationToProcessBuf,
			frameRigAnimationJobs = rigAnimationJobsBuf,
			animationClipsOffsets = rad.animationClipsMap,
			rigDefinitionOffsets = rad.rigDefinitionsMap,
			avatarMasksOffsets = rad.avatarMasksDataMap,
			frameEntityAnimatedDataOffsetsMap = rad.frameEntityAnimatedDataOffsetsMap
		};
		fillFrameAnimatedRigWorkloadDataJob.ScheduleParallel(gpuAnimatedRigQuery, new JobHandle()).Complete();
		
		framePerBoneAnimationWorkloadGB.UnlockBufferAfterWrite((int)rad.frameAnimatedBonesCounter);
		frameAnimationToProcessGB.UnlockBufferAfterWrite((int)rad.frameAnimationToProcessCounter);
		frameRigAnimationJobsGB.UnlockBufferAfterWrite((int)rad.frameAnimatedRigsCounter);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	unsafe JobHandle ResetFrameData(ref GPURuntimeAnimationData rad, JobHandle dependsOn)
	{
		var resetFrameDataJob = new ResetFrameDataJob()
		{
			frameEntityAnimatedDataOffsets = rad.frameEntityAnimatedDataOffsetsMap,
			frameAnimatedBonesCounter = new UnsafeAtomicCounter32(UnsafeUtility.AddressOf(ref rad.frameAnimatedBonesCounter)),
			frameAnimatedRigsCounter = new UnsafeAtomicCounter32(UnsafeUtility.AddressOf(ref rad.frameAnimatedRigsCounter)),
			frameSkinnedMeshesCounter = new UnsafeAtomicCounter32(UnsafeUtility.AddressOf(ref rad.frameSkinnedMeshesCounter)),
			frameAnimationToProcessCounter = new UnsafeAtomicCounter32(UnsafeUtility.AddressOf(ref rad.frameAnimationToProcessCounter)),
		};
		var rv = resetFrameDataJob.Schedule(dependsOn);
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void PrepareFrameAnimationData(ref GPURuntimeAnimationData runtimeAnimationData, JobHandle dependsOn)
	{
		var resetFrameDataJH = ResetFrameData(ref runtimeAnimationData, dependsOn);
		var calculateFrameWorksetDataJH = CalculateFrameWorksetData(ref runtimeAnimationData, resetFrameDataJH);
		
		//	Complete dependency because need to resize GPU buffers with actualized frame counts
		calculateFrameWorksetDataJH.Complete();
		
		ResizeGPUBuffers(ref runtimeAnimationData);
		CopyPersistentAnimationDataToGPUBuffers(ref runtimeAnimationData);
		GPUBuffersBeginFrame();
		FillFrameAnimationCalculationWorkloadGPUBuffers(ref runtimeAnimationData);
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CopyPersistentAnimationDataToGPUBuffers(ref GPURuntimeAnimationData rad)
	{
		CopyNewAnimationsToGPUBuffers(ref rad);	
		CopyNewRigsToGPUBuffer(ref rad);
		CopyNewBoneRemapTablesToGPUBuffers(ref rad);
		CopyNewAvatarMasksToGPUBuffer(ref rad);
		//DebugBufRead(ref rad);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	GraphicsBuffer GrowPersistentDataComputeBuffer(GraphicsBuffer gb, uint newElementCount)
	{
		if (gb == null || gb.count >= newElementCount)
			return gb;
	
		var newBuf = new GraphicsBuffer(gb.target, gb.usageFlags, (int)newElementCount, gb.stride);
		ComputeBufferTools.Copy(gb, newBuf, 0, 0, (uint)(newElementCount * gb.stride));
		gb.Dispose();
		return newBuf;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void ResizeGPUBuffers(ref GPURuntimeAnimationData rad)
	{
		//	Persistent data buffers
		animationClipsGB = GrowPersistentDataComputeBuffer(animationClipsGB, rad.totalGPUAnimationClipsSize.x / 4);
		avatarMasksGB = GrowPersistentDataComputeBuffer(avatarMasksGB, rad.totalGPUAvatarMasksDataCount.x);
		rigDefinitionGB = GrowPersistentDataComputeBuffer(rigDefinitionGB, rad.totalGPURigsCount.x);
		rigHumanAvatarRotationDataGB = GrowPersistentDataComputeBuffer(rigHumanAvatarRotationDataGB, rad.totalGPUHumanRotationDataEntriesCount.x);
		rigBonesGB = GrowPersistentDataComputeBuffer(rigBonesGB, rad.totalGPURigBonesCount.x);
		skinnedMeshBoneDataGB = GrowPersistentDataComputeBuffer(skinnedMeshBoneDataGB, rad.totalGPUSkinnedMeshBonesCount.x);
		
		//	Everyframe updated buffers
		frameAnimationToProcessGB.Grow((int)rad.frameAnimationToProcessCounter);
		framePerBoneAnimationWorkloadGB.Grow((int)rad.frameAnimatedBonesCounter);
		frameRigAnimationJobsGB.Grow((int)rad.frameAnimatedRigsCounter);
		frameSkinMatrixWorkloadGB.Grow((int)rad.frameSkinnedMeshesCounter);
		
		animatedBonesGB = ComputeBufferTools.GrowNoCopy(animatedBonesGB, (int)rad.frameAnimatedBonesCounter);
		rigSpaceAnimatedBonesGB = ComputeBufferTools.GrowNoCopy(rigSpaceAnimatedBonesGB, (int)rad.frameAnimatedBonesCounter);
		Shader.SetGlobalBuffer(ShaderID_rigSpaceBoneTransformsBuf, rigSpaceAnimatedBonesGB);
		Shader.SetGlobalBuffer(ShaderID_boneLocalTransforms, animatedBonesGB);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CopyNewBoneRemapTablesToGPUBuffers(ref GPURuntimeAnimationData rad)
	{
		if (rad.newSkinnedMeshesDataList.IsEmpty)
			return;
				
		var newSkinnedMeshBonesCount = (int)(rad.totalGPUSkinnedMeshBonesCount.x - rad.totalGPUSkinnedMeshBonesCount.y);
		
		var skinnedMeshBonesUploader = sparseUploaderPool.GetUploader(skinnedMeshBoneDataGB);
		var skinnedMeshBonesUploadSizeInBytes = newSkinnedMeshBonesCount * UnsafeUtility.SizeOf<GPUStructures.SkinnedMeshBoneData>();
		var skinnedMeshBonesThreadedUploader = skinnedMeshBonesUploader.Begin(skinnedMeshBonesUploadSizeInBytes, skinnedMeshBonesUploadSizeInBytes, newSkinnedMeshBonesCount);
		
		var copyNewBoneRemapTables = new CopyNewSkinnedMeshesDataJob()
		{
			newSkinnedMeshData = rad.newSkinnedMeshesDataList,
			gpuSkinnedMeshBonesData = skinnedMeshBonesThreadedUploader,
		};
			
		copyNewBoneRemapTables.ScheduleParallel(rad.newSkinnedMeshesDataList.Length, 1, default).Complete();
		
		skinnedMeshBonesUploader.EndAndCommit(skinnedMeshBonesThreadedUploader);
		sparseUploaderPool.PutUploader(skinnedMeshBonesUploader);
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CopyNewAvatarMasksToGPUBuffer(ref GPURuntimeAnimationData rad)
	{
		if (rad.newAvatarMasksList.IsEmpty)
			return;
		
		var newAvatarMasksEntriesCount = (int)(rad.totalGPUAvatarMasksDataCount.x - rad.totalGPUAvatarMasksDataCount.y);
		
		var avatarMaskUploader = sparseUploaderPool.GetUploader(avatarMasksGB);
		var avatarMaskUploadSizeInBytes = newAvatarMasksEntriesCount * UnsafeUtility.SizeOf<uint>();
		var avatarMaskThreadedUploader = avatarMaskUploader.Begin(avatarMaskUploadSizeInBytes, avatarMaskUploadSizeInBytes, newAvatarMasksEntriesCount);
		
		var copyNewAvatarMasksToGPUJob = new CopyNewAvatarMasksToGPUJob()
		{
			gpuAvatarMasks = avatarMaskThreadedUploader,
			newAvatarMasks = rad.newAvatarMasksList
		};
			
		copyNewAvatarMasksToGPUJob.ScheduleParallel(rad.newAvatarMasksList.Length, 1, default).Complete();
		
		avatarMaskUploader.EndAndCommit(avatarMaskThreadedUploader);
		sparseUploaderPool.PutUploader(avatarMaskUploader);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CopyNewRigsToGPUBuffer(ref GPURuntimeAnimationData rad)
	{
		if (rad.newRigsList.IsEmpty)
			return;
		
		var newRigBonesCount = (int)(rad.totalGPURigBonesCount.x - rad.totalGPURigBonesCount.y);
		var newRigsCount = (int)(rad.totalGPURigsCount.x - rad.totalGPURigsCount.y);
		var newHumanRotationDataEntriesCount = (int)(rad.totalGPUHumanRotationDataEntriesCount.x - rad.totalGPUHumanRotationDataEntriesCount.y);
		
		var rigBonesUploader = sparseUploaderPool.GetUploader(rigBonesGB);
		var rigBonesUploadSizeInBytes = newRigBonesCount * UnsafeUtility.SizeOf<GPUStructures.RigBone>();
		var rigBonesThreadedUploader = rigBonesUploader.Begin(rigBonesUploadSizeInBytes, rigBonesUploadSizeInBytes, newRigBonesCount);
		
		var rigDefinitionUploader = sparseUploaderPool.GetUploader(rigDefinitionGB);
		var rigDefinitionsUploadSizeInBytes = newRigsCount * UnsafeUtility.SizeOf<GPUStructures.RigDefinition>();
		var rigDefinitionsThreadedUploader = rigDefinitionUploader.Begin(rigDefinitionsUploadSizeInBytes, rigDefinitionsUploadSizeInBytes, newRigsCount);
		
		var rigHumanRotationDataUploader = sparseUploaderPool.GetUploader(rigHumanAvatarRotationDataGB);
		var rigHumanRotationDataUploadSizeInBytes = newHumanRotationDataEntriesCount * UnsafeUtility.SizeOf<GPUStructures.HumanRotationData>();
		var rigHumanRotationDataThreadedUploader = rigHumanRotationDataUploader.Begin(rigHumanRotationDataUploadSizeInBytes, rigHumanRotationDataUploadSizeInBytes, newRigsCount);
		
		var copyNewRigsToGPUJob = new CopyNewRigsToGPUJob()
		{
			newRigs = rad.newRigsList,
			gpuRigBones = rigBonesThreadedUploader,
			gpuRigDefs = rigDefinitionsThreadedUploader,
			gpuHumanRotationData = rigHumanRotationDataThreadedUploader
		};
			
		copyNewRigsToGPUJob.ScheduleParallel(rad.newRigsList.Length, 1, default).Complete();
		
		rigDefinitionUploader.EndAndCommit(rigDefinitionsThreadedUploader);
		rigBonesUploader.EndAndCommit(rigBonesThreadedUploader);
		rigHumanRotationDataUploader.EndAndCommit(rigHumanRotationDataThreadedUploader);
		
		sparseUploaderPool.PutUploader(rigDefinitionUploader);
		sparseUploaderPool.PutUploader(rigBonesUploader);
		sparseUploaderPool.PutUploader(rigHumanRotationDataUploader);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CopyNewAnimationsToGPUBuffers(ref GPURuntimeAnimationData rad)
	{
		if (rad.newAnimationsList.IsEmpty)
			return;
		
		var newAnimationClipsCount = rad.newAnimationsList.Length;
		//	Very conservative value
		var maxNumUploadsPerClip = 0xf;
		
		var animationClipsUploader = sparseUploaderPool.GetUploader(animationClipsGB);
		var maxAnimationClipSingleUploadSize = rad.totalGPUAnimationClipsSize.x - rad.totalGPUAnimationClipsSize.y;
		var animationClipsThreadedUploader = animationClipsUploader.Begin((int)maxAnimationClipSingleUploadSize, (int)maxAnimationClipSingleUploadSize, newAnimationClipsCount * maxNumUploadsPerClip);
		
		var createGPUAnimationClip = new CopyNewAnimationsToGPUJob()
		{
			newAnimationClips = rad.newAnimationsList,
			gpuAnimationClips = animationClipsThreadedUploader,
		};
		
		createGPUAnimationClip.ScheduleParallel(rad.newAnimationsList.Length, 1, default).Complete();
		
		animationClipsUploader.EndAndCommit(animationClipsThreadedUploader);
		sparseUploaderPool.PutUploader(animationClipsUploader);
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	internal void BuildSkinMatrices(in NativeParallelHashMap<Entity, SkinnedMeshRendererFrameDeformationData> entityToSMRFrameDataMap, FrameFencedGPUBufferPool<SkinMatrix> outSkinMatrixGB)
	{
		if (!SystemAPI.TryGetSingletonRW<GPURuntimeAnimationData>(out var radRW))
			return;
		
		ref var runtimeAnimationData = ref radRW.ValueRW;
		
		frameSkinMatrixWorkloadGB.BeginFrame();
		
		FillFrameSkinMatrixCalculationWorkloadGPUBuffers(ref runtimeAnimationData, entityToSMRFrameDataMap);
		
		if (runtimeAnimationData.frameSkinnedMeshesCounter > 0)
		{
			animationEngineCS.SetBuffer(makeSkinMatricesKernel, ShaderID_skinMatrixWorkloadBuf, frameSkinMatrixWorkloadGB);
			animationEngineCS.SetBuffer(makeSkinMatricesKernel, ShaderID_rigSpaceBoneTransformsBuf, rigSpaceAnimatedBonesGB);
			animationEngineCS.SetBuffer(makeSkinMatricesKernel, ShaderID_outSkinMatrices, outSkinMatrixGB);
			animationEngineCS.SetBuffer(makeSkinMatricesKernel, ShaderID_skinnedMeshBoneData, skinnedMeshBoneDataGB);
			animationEngineCS.SetInt(ShaderID_totalSkinnedMeshes, (int)runtimeAnimationData.frameSkinnedMeshesCounter);
				
			makeSkinMatricesKernel.Dispatch(runtimeAnimationData.frameSkinnedMeshesCounter, 1, 1);
		}
		
		frameSkinMatrixWorkloadGB.EndFrame();
	}


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void DispatchAnimationCalculation(ref GPURuntimeAnimationData rad)
	{
		if (rad.frameAnimatedBonesCounter == 0)
			return;
		
		//	Animation computation
		animationEngineCS.SetBuffer(processAnimationKernel, ShaderID_outAnimatedBones, animatedBonesGB);
		animationEngineCS.SetBuffer(processAnimationKernel, ShaderID_animatedBoneWorkload, framePerBoneAnimationWorkloadGB);
		animationEngineCS.SetBuffer(processAnimationKernel, ShaderID_animationJobs, frameRigAnimationJobsGB);
		animationEngineCS.SetBuffer(processAnimationKernel, ShaderID_animationsToProcess, frameAnimationToProcessGB);
		animationEngineCS.SetBuffer(processAnimationKernel, ShaderID_rigDefinitions, rigDefinitionGB);
		animationEngineCS.SetBuffer(processAnimationKernel, ShaderID_rigBones, rigBonesGB);
		animationEngineCS.SetBuffer(processAnimationKernel, ShaderID_animationClips, animationClipsGB);
		animationEngineCS.SetBuffer(processAnimationKernel, ShaderID_humanRotationDataBuffer, rigHumanAvatarRotationDataGB);
		animationEngineCS.SetBuffer(processAnimationKernel, ShaderID_avatarMasksBuffer, avatarMasksGB);
		animationEngineCS.SetInt(ShaderID_animatedBonesCount, (int)rad.frameAnimatedBonesCounter);
		
		processAnimationKernel.Dispatch(rad.frameAnimatedBonesCounter, 1, 1);
		
		//	Compute rig space bone transforms
		animationEngineCS.SetBuffer(makeRigSpaceBoneTransformsKernel, ShaderID_boneLocalTransforms, animatedBonesGB);
		animationEngineCS.SetBuffer(makeRigSpaceBoneTransformsKernel, ShaderID_animatedBoneWorkload, framePerBoneAnimationWorkloadGB);
		animationEngineCS.SetBuffer(makeRigSpaceBoneTransformsKernel, ShaderID_animationJobs, frameRigAnimationJobsGB);
		animationEngineCS.SetBuffer(makeRigSpaceBoneTransformsKernel, ShaderID_rigDefinitions, rigDefinitionGB);
		animationEngineCS.SetBuffer(makeRigSpaceBoneTransformsKernel, ShaderID_rigBones, rigBonesGB);
		animationEngineCS.SetBuffer(makeRigSpaceBoneTransformsKernel, ShaderID_outBoneTransforms, rigSpaceAnimatedBonesGB);
		makeRigSpaceBoneTransformsKernel.Dispatch(rad.frameAnimatedBonesCounter, 1, 1);
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void SetBuffersForBoneRenderer(ref GPURuntimeAnimationData rad)
	{
	#if RUKHANKA_DEBUG_INFO
		boneVisualizationSystem	??= World.GetExistingSystemManaged<BoneVisualizationSystem>();
		boneVisualizationSystem.gpuBoneRenderer.SetGPUBuffersForFrame
		(
			framePerBoneAnimationWorkloadGB,
			frameRigAnimationJobsGB,
			rigDefinitionGB,
			rigBonesGB,
			rigSpaceAnimatedBonesGB,
			(int)rad.frameAnimatedBonesCounter
		);
	#endif
	}
}
}

#endif
