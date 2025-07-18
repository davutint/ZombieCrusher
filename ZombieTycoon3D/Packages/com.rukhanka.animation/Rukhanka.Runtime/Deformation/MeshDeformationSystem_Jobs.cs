#if !RUKHANKA_NO_DEFORMATION_SYSTEM

using Rukhanka.Toolbox;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using Hash128 = Unity.Entities.Hash128;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
public partial class MeshDeformationSystem
{
	
//-----------------------------------------------------------------------------------------------------------------//

[BurstCompile]
struct ResetFrameCountersJob: IJob
{
    [NativeDisableUnsafePtrRestriction]
    public UnsafeAtomicCounter32 frameActiveDeformedMeshesCounter;
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public void Execute()
	{
		frameActiveDeformedMeshesCounter.Reset(0);
	}
}

//-----------------------------------------------------------------------------------------------------------------//

[BurstCompile]
partial struct PrepareSkinningCommandsJob: IJobEntity
{
    [ReadOnly]
    public NativeParallelHashMap<Entity, SkinnedMeshRendererFrameDeformationData> entityToSMRFrameDataMap;
    [ReadOnly]
    public NativeParallelHashMap<Hash128, SkinnedMeshDescription> registeredSkinnedMeshes;
    
    [NativeDisableUnsafePtrRestriction]
    public UnsafeAtomicCounter32 frameActiveDeformedMeshesCounter;
    [NativeDisableParallelForRestriction]
    public NativeArray<MeshFrameDeformationDescription> meshFrameDeformationData;
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void Execute(Entity e, in AnimatedSkinnedMeshComponent arc)
	{
		if (!entityToSMRFrameDataMap.TryGetValue(e, out var smrdd))
			return;
		
		if (!registeredSkinnedMeshes.TryGetValue(arc.smrInfoBlob.Value.hash, out var smd))
		{
		#if RUKHANKA_DEBUG_INFO
			BurstAssert.IsTrue(false, $"Skinned mesh '{arc.smrInfoBlob.Value.skeletonName.ToFixedString()}' is not properly registered.");
		#else
			BurstAssert.IsTrue(false, $"Skinned mesh with hash '{arc.smrInfoBlob.Value.hash.Value}' is not properly registered. Enable 'RUKHANKA_DEBUG_INFO' to see the mesh name.");
		#endif
			return;
		}
		
		//	Mesh skinning data offsets
		var meshFrameData = new MeshFrameDeformationDescription();
		meshFrameData.baseOutVertexIndex = smrdd.deformedVertexIndex;
		meshFrameData.baseSkinMatrixIndex = smrdd.skinMatrixIndex;
		meshFrameData.baseBlendShapeWeightIndex = smrdd.blendShapeWeightIndex;
		meshFrameData.baseInputMeshVertexIndex = smd.baseVertex;
		meshFrameData.baseInputMeshBlendShapeIndex = smd.baseBlendShapeIndex;
		meshFrameData.meshVerticesCount = arc.smrInfoBlob.Value.meshVerticesCount;
		meshFrameData.meshBlendShapesCount = arc.smrInfoBlob.Value.meshBlendShapesCount;
		
		var currentMeshFrameDeformationDataIndex = frameActiveDeformedMeshesCounter.Add(1);
	#if RUKHANKA_INPLACE_SKINNING
		//	Yes, I am overwriting the value here. Frame active meshes counter need to be incremented in any case
		currentMeshFrameDeformationDataIndex = smrdd.deformedVertexIndex; 
	#endif
		meshFrameDeformationData[currentMeshFrameDeformationDataIndex] = meshFrameData;
	}
}

//-----------------------------------------------------------------------------------------------------------------//

[BurstCompile]
partial struct SetDeformedMeshIndicesJob: IJobEntity
{
	[ReadOnly]
	public NativeParallelHashMap<Entity, SkinnedMeshRendererFrameDeformationData> entityToSMRFrameDataMap;
	[NativeDisableUnsafePtrRestriction]
	public UnsafeAtomicCounter32 frameDeformedVerticesCounter;
	
#if ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
	//	Either 0 or 1
	public int currentFrameDeformedBufferIndex;
#endif
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	unsafe void Execute(in AnimatedRendererComponent arc, ref DeformedMeshIndex dri, ref DeformedMeshIndexDeprecated drid)
	{
	#if ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
		dri.Value[2] = (uint)currentFrameDeformedBufferIndex;
	#endif
		
		//	Handle invisible mesh renderers by assigning theirs deformed mesh index to some value that can be handled in shaders:
		//	* In case of in-place skinning I need to simply check for this index for some given predefined distinct value
		//	* In case of preskinning path set deformed mesh index beyond valid data. Previously I have added some zeroes at the end
		//	of skinned vertices data. Indexing it will return zero values for skinning mesh
		if (!entityToSMRFrameDataMap.TryGetValue(arc.skinnedMeshEntity, out var smrdd))
		{
		#if ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
			#if RUKHANKA_INPLACE_SKINNING
				dri.Value[currentFrameDeformedBufferIndex] = 0xffffffff;
			#else
				dri.Value[currentFrameDeformedBufferIndex] = (uint)*frameDeformedVerticesCounter.Counter;
			#endif
			drid.Value[currentFrameDeformedBufferIndex] = dri.Value[currentFrameDeformedBufferIndex];
		#else
			#if RUKHANKA_INPLACE_SKINNING
				dri.Value = 0xffffffff;
			#else
				dri.Value = (uint)*frameDeformedVerticesCounter.Counter;
			#endif
			drid.Value = dri.Value;
		#endif
			return;
		}
		
	#if ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
		dri.Value[currentFrameDeformedBufferIndex] = (uint)smrdd.deformedVertexIndex;
		drid.Value[currentFrameDeformedBufferIndex] = dri.Value[currentFrameDeformedBufferIndex];
	#else
		dri.Value = (uint)smrdd.deformedVertexIndex;
		drid.Value = dri.Value;
	#endif
	}
}

//-----------------------------------------------------------------------------------------------------------------//

[BurstCompile]
partial struct CopySkinMatricesToGPUJob: IJobEntity
{
	[ReadOnly]
	public NativeParallelHashMap<Entity, SkinnedMeshRendererFrameDeformationData> entityToSMRFrameDataMap;
	[ReadOnly]
	public ComponentLookup<GPUAnimationEngineTag> gpuAnimationEngineTag;
	
	[NativeDisableParallelForRestriction]
	public ThreadedSparseUploader mappedGPUSkinMatrixBuffer;
	
	public bool isEditor;
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	unsafe void Execute(Entity e, AnimatedSkinnedMeshComponent arc, in DynamicBuffer<SkinMatrix> skinMatrices)
	{
		bool isGPUAnimator = !isEditor && arc.IsGPUAnimator(gpuAnimationEngineTag);
		if (isGPUAnimator || !entityToSMRFrameDataMap.TryGetValue(e, out var smrdd))
			return;
		
		var srcPtr = skinMatrices.GetUnsafeReadOnlyPtr();
		var srcSize = skinMatrices.Length * UnsafeUtility.SizeOf<SkinMatrix>();
		var dstOffset = smrdd.skinMatrixIndex * UnsafeUtility.SizeOf<SkinMatrix>();
		mappedGPUSkinMatrixBuffer.AddUpload(srcPtr, srcSize, dstOffset);
	}
}

//-----------------------------------------------------------------------------------------------------------------//

[BurstCompile]
partial struct CopyBlendShapeWeightToGPUJob: IJobEntity
{
	[ReadOnly]
	public NativeParallelHashMap<Entity, SkinnedMeshRendererFrameDeformationData> entityToSMRFrameDataMap;
	
	[NativeDisableParallelForRestriction]
	public NativeArray<float> mappedGPUBlendShapeWeightsBuffer;
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	unsafe void Execute(Entity e, in DynamicBuffer<BlendShapeWeight> blendShapeWeights)
	{
		if (!entityToSMRFrameDataMap.TryGetValue(e, out var smrdd))
			return;
		
		var dstPtr = (float*)mappedGPUBlendShapeWeightsBuffer.GetUnsafePtr() + smrdd.blendShapeWeightIndex;
		UnsafeUtility.MemCpy(dstPtr, blendShapeWeights.GetUnsafeReadOnlyPtr(), UnsafeUtility.SizeOf<float>() * blendShapeWeights.Length);
	}
}
}
}

#endif