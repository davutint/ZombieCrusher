#if !RUKHANKA_NO_DEFORMATION_SYSTEM

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Rendering;
using Hash128 = Unity.Entities.Hash128;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{

[UpdateInGroup(typeof(RukhankaDeformationSystemGroup))]
[UpdateBefore(typeof(GPUAnimationSystem))]
public partial struct GPUAnimationPreparationSystem: ISystem
{
    NativeParallelHashMap<Hash128, BlobAssetReference<AnimationClipBlob>> newAnimationsMap;
    NativeParallelHashMap<Hash128, BlobAssetReference<RigDefinitionBlob>> newRigsMap;
    NativeParallelHashMap<Hash128, GPUSkinnedMeshPlacementData> newSkinnedMeshesDataMap;
    NativeParallelHashMap<Hash128, BlobAssetReference<AvatarMaskBlob>> newAvatarMaskMap;
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void OnCreate(ref SystemState ss)
    {
#if !HYBRID_RENDERER_DISABLED
        if (!EntitiesGraphicsUtils.IsEntitiesGraphicsSupportedOnSystem())
#endif
		{
			ss.Enabled = false;
			return;
		}
		
		var runtimeAnimationDataSingleton = GPURuntimeAnimationData.Construct();
		ss.EntityManager.CreateSingleton(runtimeAnimationDataSingleton, "Rukhanka GPU Animation Data");
		
		var initialCapacity = 0x1000;
		newAnimationsMap = new (initialCapacity, Allocator.Persistent);
		newRigsMap = new (initialCapacity, Allocator.Persistent);
		newSkinnedMeshesDataMap = new (initialCapacity, Allocator.Persistent);
		newAvatarMaskMap = new (initialCapacity, Allocator.Persistent);
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	[BurstCompile]
    public void OnDestroy(ref SystemState ss)
    {
		if (SystemAPI.TryGetSingletonRW<GPURuntimeAnimationData>(out var radRW))
		{
			ref var rad = ref radRW.ValueRW;
			rad.Dispose();
			ss.EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<GPURuntimeAnimationData>());
		}
		
		newAnimationsMap.Dispose();
		newRigsMap.Dispose();
		newSkinnedMeshesDataMap.Dispose();
		newAvatarMaskMap.Dispose();
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	[BurstCompile]
    public void OnUpdate(ref SystemState ss)
    {
		ref var runtimeAnimationData = ref SystemAPI.GetSingletonRW<GPURuntimeAnimationData>().ValueRW;
		
		var resetFrameDataJH = ResetFrameData(ref ss, ref runtimeAnimationData, ss.Dependency);
		
		//	Prepare new resources that appear this frame
		var newAnimationResourcesJH = GatherNewRigsAndAnimations(ref ss, ref runtimeAnimationData, resetFrameDataJH);
		var newRigRemapTablesJH = GatherNewRigRemapTables(ref ss, ref runtimeAnimationData, resetFrameDataJH);
		var newResourcesJH = JobHandle.CombineDependencies(newAnimationResourcesJH, newRigRemapTablesJH);
		
		//	Register new resources in persistent db
		var registerNewResourcesJH = RegisterNewResources(ref ss, ref runtimeAnimationData, newResourcesJH);
			
		ss.Dependency = registerNewResourcesJH;
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	unsafe JobHandle ResetFrameData(ref SystemState ss, ref GPURuntimeAnimationData rad, JobHandle dependsOn)
	{
		var resetFrameDataJob = new ResetFrameDataJob()
		{
			newAnimationsMap = newAnimationsMap,
			newRigsMap = newRigsMap,
			newAnimationsList = rad.newAnimationsList,
			newRigsList = rad.newRigsList,
			newSkinnedMeshDataMap = newSkinnedMeshesDataMap,
			newSkinnedMeshDataList = rad.newSkinnedMeshesDataList,
			newAvatarMasksList = rad.newAvatarMasksList,
			newAvatarMasksMap = newAvatarMaskMap,
			totalGPURigsCount = (uint2*)UnsafeUtility.AddressOf(ref rad.totalGPURigsCount),
			totalGPUHumanRotationDataEntriesCount = (uint2*)UnsafeUtility.AddressOf(ref rad.totalGPUHumanRotationDataEntriesCount),
			totalGPURigBonesCount = (uint2*)UnsafeUtility.AddressOf(ref rad.totalGPURigBonesCount),
			totalGPUAnimationClipsDataSize = (uint2*)UnsafeUtility.AddressOf(ref rad.totalGPUAnimationClipsSize),
			totalGPUBoneRemapIndicesCount = (uint2*)UnsafeUtility.AddressOf(ref rad.totalGPUSkinnedMeshBonesCount),
			totalGPUBoneRemapTablesCount = (uint2*)UnsafeUtility.AddressOf(ref rad.totalGPUSkinnedMeshesCount),
			totalGPUAvatarMasksDataCount = (uint2*)UnsafeUtility.AddressOf(ref rad.totalGPUAvatarMasksDataCount),
		};
		var rv = resetFrameDataJob.Schedule(dependsOn);
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	unsafe JobHandle CreateNewRigsData(ref SystemState ss, ref GPURuntimeAnimationData rad, JobHandle dependsOn)
	{
		//	Cannot process hash map in a parallel manner, so copy data to the list
		var copyNewRigsToListJob = new CreateNewRigsListJob()
		{
			outList = rad.newRigsList,
			newBlobAssetsMap = newRigsMap
		};
		var copyNewRigsToListJH = copyNewRigsToListJob.Schedule(dependsOn);
		
		//	Calculate new data capacity. Increased capacity is needed for GPU buffer resize
		var calculateNewRigsOffsetsJob = new CalculateNewRigsOffsets()
		{
			newRigs = rad.newRigsList,
			totalGPURigsCount = new UnsafeAtomicCounter32(UnsafeUtility.AddressOf(ref rad.totalGPURigsCount.x)),
			totalGPURigBonesCount = new UnsafeAtomicCounter32(UnsafeUtility.AddressOf(ref rad.totalGPURigBonesCount.x)),
			totalGPUHumanRotationDataEntriesCount = new UnsafeAtomicCounter32(UnsafeUtility.AddressOf(ref rad.totalGPUHumanRotationDataEntriesCount.x))
		};
		var calculateNewRigsOffsetsJH = calculateNewRigsOffsetsJob.Schedule(rad.newRigsList, 1, copyNewRigsToListJH);
		
		return calculateNewRigsOffsetsJH;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	unsafe JobHandle CreateNewAvatarMasksData(ref SystemState ss, ref GPURuntimeAnimationData rad, JobHandle dependsOn)
	{
		var copyNewAvatarMasksToListJob = new CreateNewAvatarMasksListJob()
		{
			outList = rad.newAvatarMasksList,
			newBlobAssetsMap = newAvatarMaskMap
		};
		var copyNewAvatarMasksToListJH = copyNewAvatarMasksToListJob.Schedule(dependsOn);
		
		//	Calculate new data capacity. Increased capacity is needed for GPU buffer resize
		var calculateNewAvatarMaskOffsetsJob = new CalculateNewAvatarMasksOffsetsJob()
		{
			newAvatarMasks = rad.newAvatarMasksList,
			totalAvatarMasksCounter = new UnsafeAtomicCounter32(UnsafeUtility.AddressOf(ref rad.totalGPUAvatarMasksDataCount.x)),
		};
		var calculateNewAvatarMaskOffsetsJH = calculateNewAvatarMaskOffsetsJob.Schedule(rad.newAvatarMasksList, 1, copyNewAvatarMasksToListJH);
		
		return calculateNewAvatarMaskOffsetsJH;
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	unsafe JobHandle CreateNewAnimationsData(ref SystemState ss, ref GPURuntimeAnimationData rad, JobHandle dependsOn)
	{
		//	Cannot process hash map in a parallel manner, so copy data to the list
		var copyNewAnimationsToListJob = new CreateNewAnimationsListJob()
		{
			outList = rad.newAnimationsList,
			newBlobAssetsMap = newAnimationsMap
		};
		var copyNewAnimationsToListJH = copyNewAnimationsToListJob.Schedule(dependsOn);
		
		//	Calculate new data capacity. Increased capacity is needed for GPU buffer resize
		var calculateNewAnimationsOffsetsJob = new CalculateNewAnimationOffsets()
		{
			newAnimationClips = rad.newAnimationsList,
			totalAnimationClipsOffsetCounter = new UnsafeAtomicCounter32(UnsafeUtility.AddressOf(ref rad.totalGPUAnimationClipsSize.x)),
		};
		var calculateNewAnimationsOffsetsJH = calculateNewAnimationsOffsetsJob.Schedule(rad.newAnimationsList, 1, copyNewAnimationsToListJH);
		
		return calculateNewAnimationsOffsetsJH;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	JobHandle GatherNewRigsAndAnimations(ref SystemState ss, ref GPURuntimeAnimationData rad, JobHandle dependsOn)
	{
		var newFrameResourcesJob = new GatherNewRigsAndAnimationsJob()
		{
			existingAnimations = rad.animationClipsMap,
			existingRigs = rad.rigDefinitionsMap,
			existingAvatarMasks = rad.avatarMasksDataMap,
			newFrameAnimations = newAnimationsMap.AsParallelWriter(),
			newRigDefinitions = newRigsMap.AsParallelWriter(),
			newFrameAvatarMasks = newAvatarMaskMap.AsParallelWriter()
		};
		var newFrameResourcesJH = newFrameResourcesJob.ScheduleParallel(dependsOn);
		
		var createNewAnimationsJH = CreateNewAnimationsData(ref ss, ref rad, newFrameResourcesJH);
		var createNewAvatarMasksJH = CreateNewAvatarMasksData(ref ss, ref rad, newFrameResourcesJH);
		var createNewRigsJH = CreateNewRigsData(ref ss, ref rad, newFrameResourcesJH);
		
		var rv = JobHandle.CombineDependencies(createNewAnimationsJH, createNewAvatarMasksJH, createNewRigsJH);
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	unsafe JobHandle RegisterNewResources(ref SystemState ss, ref GPURuntimeAnimationData rad, JobHandle dependsOn)
	{
		var registerNewResourcesJob = new RegisterNewResourcesJob()
		{
			newAnimations = rad.newAnimationsList,
			animationClipsOffsets = rad.animationClipsMap,
			newRigDefs = rad.newRigsList,
			rigDefinitionOffsets = rad.rigDefinitionsMap,
			newSkinnedMeshesDatas = rad.newSkinnedMeshesDataList,
			skinnedMeshDataOffsets = rad.skinnedMeshesDataMap,
			newAvatarMasks = rad.newAvatarMasksList,
			avatarMasksDataOffsets = rad.avatarMasksDataMap,
			maximumKeyFrameArrayLength = (uint*)UnsafeUtility.AddressOf(ref rad.maxTrackKeyframesCount)
		};
		
		var registerNewResourcesJH = registerNewResourcesJob.Schedule(dependsOn);
		return registerNewResourcesJH;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	
	unsafe JobHandle GatherNewRigRemapTables(ref SystemState ss, ref GPURuntimeAnimationData rad, JobHandle dependsOn)
	{
		var newBoneRemapTablesJob = new NewFrameRigToSkinnedMeshRemapTablesJob()
		{
			skinnedMeshesDataMap = rad.skinnedMeshesDataMap,
			rigDefComponentLookup = SystemAPI.GetComponentLookup<RigDefinitionComponent>(true),
			gpuAnimationEngineComponentLookup = SystemAPI.GetComponentLookup<GPUAnimationEngineTag>(true),
			newFrameSkinnedMeshData = newSkinnedMeshesDataMap.AsParallelWriter()
		};
		var newBoneRemapTablesJH = newBoneRemapTablesJob.ScheduleParallel(dependsOn);
		
		var copyNewRemapTablesToListJob = new CreateNewSkinnedMeshesDataListJob()
		{
			outList = rad.newSkinnedMeshesDataList,
			newSkinnedMeshesDataMap = newSkinnedMeshesDataMap
		};
		var copyNewRemapTablesToListJH = copyNewRemapTablesToListJob.Schedule(newBoneRemapTablesJH);
		
		var calculateNewBoneRemapTablesOffsetsJob = new CalculateNewBoneRemapTablesOffsetsJob()
		{
			newBoneRemapTables = rad.newSkinnedMeshesDataList,
			totalGPUBoneRemapIndicesCount = new UnsafeAtomicCounter32(UnsafeUtility.AddressOf(ref rad.totalGPUSkinnedMeshBonesCount)),
			totalGPUBoneRemapTablesCount = new UnsafeAtomicCounter32(UnsafeUtility.AddressOf(ref rad.totalGPUSkinnedMeshesCount)),
		};
		var calculateNewBoneRemapTablesOffsetsJH = calculateNewBoneRemapTablesOffsetsJob.Schedule(rad.newSkinnedMeshesDataList, 1, copyNewRemapTablesToListJH);
		
		return calculateNewBoneRemapTablesOffsetsJH;
	}
}
}

#endif