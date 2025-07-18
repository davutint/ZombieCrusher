#if !RUKHANKA_NO_DEFORMATION_SYSTEM

using System;
using System.Collections.Generic;
using Unity.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using Rukhanka.Toolbox;
using Unity.Burst;
using Hash128 = Unity.Entities.Hash128;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{

public partial struct GPUAnimationPreparationSystem
{
[BurstCompile]
unsafe struct ResetFrameDataJob: IJob
{
    public NativeParallelHashMap<Hash128, BlobAssetReference<AnimationClipBlob>> newAnimationsMap;
    public NativeParallelHashMap<Hash128, BlobAssetReference<RigDefinitionBlob>> newRigsMap;
    public NativeParallelHashMap<Hash128, BlobAssetReference<AvatarMaskBlob>> newAvatarMasksMap;
    
    public NativeList<GPURigDefinitionPlacementData> newRigsList;
    public NativeList<GPUAnimationClipPlacementData> newAnimationsList;
    public NativeList<GPUAvatarMaskPlacementData> newAvatarMasksList;
    
    public NativeParallelHashMap<Hash128, GPUSkinnedMeshPlacementData> newSkinnedMeshDataMap;
    public NativeList<GPUSkinnedMeshPlacementData> newSkinnedMeshDataList;
    
    [NativeDisableUnsafePtrRestriction]
    public uint2 *totalGPUAnimationClipsDataSize;
    [NativeDisableUnsafePtrRestriction]
    public uint2 *totalGPURigsCount;
    [NativeDisableUnsafePtrRestriction]
    public uint2 *totalGPURigBonesCount;
    [NativeDisableUnsafePtrRestriction]
    public uint2 *totalGPUBoneRemapIndicesCount;
    [NativeDisableUnsafePtrRestriction]
    public uint2 *totalGPUBoneRemapTablesCount;
    [NativeDisableUnsafePtrRestriction]
    public uint2 *totalGPUHumanRotationDataEntriesCount;
    [NativeDisableUnsafePtrRestriction]
    public uint2 *totalGPUAvatarMasksDataCount;
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public void Execute()
	{
		newAnimationsMap.Clear();
		newRigsMap.Clear();
		newAvatarMasksMap.Clear();
		
		newRigsList.Clear();
		newAnimationsList.Clear();
		newAvatarMasksList.Clear();
		
		newSkinnedMeshDataMap.Clear();
		foreach (var nbrt in newSkinnedMeshDataList)
		{
			nbrt.boneRemapTableBlob.Dispose();	
		}
		newSkinnedMeshDataList.Clear();
		
		totalGPUAnimationClipsDataSize->y = totalGPUAnimationClipsDataSize->x;
		totalGPURigsCount->y = totalGPURigsCount->x;
		totalGPURigBonesCount->y = totalGPURigBonesCount->x;
		totalGPUBoneRemapIndicesCount->y = totalGPUBoneRemapIndicesCount->x;
		totalGPUBoneRemapTablesCount->y = totalGPUBoneRemapTablesCount->x;
		totalGPUHumanRotationDataEntriesCount->y = totalGPUHumanRotationDataEntriesCount->x;
		totalGPUAvatarMasksDataCount->y = totalGPUAvatarMasksDataCount->x;
	}
}
	
//-----------------------------------------------------------------------------------------------------------------//

[BurstCompile]
[WithAll(typeof(GPUAnimationEngineTag))]
partial struct GatherNewRigsAndAnimationsJob: IJobEntity
{
	[ReadOnly]
	public NativeParallelHashMap<Hash128, int> existingAnimations;
	[ReadOnly]
	public NativeParallelHashMap<Hash128, int> existingRigs;
	[ReadOnly]
	public NativeParallelHashMap<Hash128, int> existingAvatarMasks;

	[NativeDisableContainerSafetyRestriction]
	public NativeParallelHashMap<Hash128, BlobAssetReference<AnimationClipBlob>>.ParallelWriter newFrameAnimations;
	[NativeDisableContainerSafetyRestriction]
	public NativeParallelHashMap<Hash128, BlobAssetReference<RigDefinitionBlob>>.ParallelWriter newRigDefinitions;
	[NativeDisableContainerSafetyRestriction]
	public NativeParallelHashMap<Hash128, BlobAssetReference<AvatarMaskBlob>>.ParallelWriter newFrameAvatarMasks;
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void Execute(in RigDefinitionComponent rdc, in DynamicBuffer<AnimationToProcessComponent> atps)
	{
		var rigHash = rdc.rigBlob.Value.hash;
		if (!existingRigs.TryGetValue(rigHash, out _))
		{
			newRigDefinitions.TryAdd(rigHash, rdc.rigBlob);
		}

		for (var i = 0; i < atps.Length; ++i)
		{
			var atp = atps[i];
			if (!atp.animation.IsCreated)
				continue;
			
			//	Add animation
			var animationHash = atp.animation.Value.hash;
			if (!existingAnimations.TryGetValue(animationHash, out _))
			{
				newFrameAnimations.TryAdd(animationHash, atp.animation);
			}
			
			//	Add avatar mask
			if (atp.avatarMask.IsCreated)
			{
				var avatarMaskHash = atp.avatarMask.Value.hash;
				if (!existingAvatarMasks.TryGetValue(avatarMaskHash, out _))
				{
					newFrameAvatarMasks.TryAdd(avatarMaskHash, atp.avatarMask);
				}
			}
		}
	}
}

//-----------------------------------------------------------------------------------------------------------------//

[BurstCompile]
struct CreateNewAnimationsListJob: IJob
{
	[ReadOnly]
	public NativeParallelHashMap<Hash128, BlobAssetReference<AnimationClipBlob>> newBlobAssetsMap;
	public NativeList<GPUAnimationClipPlacementData> outList;
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public void Execute()
	{
		outList.Capacity = math.max(newBlobAssetsMap.Count(), outList.Capacity);
		foreach (var nba in newBlobAssetsMap)
		{
			var acp = new GPUAnimationClipPlacementData()
			{
				animationClipBlob = nba.Value
			};
			outList.Add(acp);
		}
	}
}

//-----------------------------------------------------------------------------------------------------------------//

[BurstCompile]
struct CreateNewAvatarMasksListJob: IJob
{
	[ReadOnly]
	public NativeParallelHashMap<Hash128, BlobAssetReference<AvatarMaskBlob>> newBlobAssetsMap;
	public NativeList<GPUAvatarMaskPlacementData> outList;
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public void Execute()
	{
		outList.Capacity = math.max(newBlobAssetsMap.Count(), outList.Capacity);
		foreach (var nba in newBlobAssetsMap)
		{
			var acp = new GPUAvatarMaskPlacementData()
			{
				avatarMaskBlob = nba.Value
			};
			outList.Add(acp);
		}
	}
}

//-----------------------------------------------------------------------------------------------------------------//

[BurstCompile]
struct CreateNewRigsListJob: IJob
{
	[ReadOnly]
	public NativeParallelHashMap<Hash128, BlobAssetReference<RigDefinitionBlob>> newBlobAssetsMap;
	public NativeList<GPURigDefinitionPlacementData> outList;
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public void Execute()
	{
		outList.Capacity = math.max(newBlobAssetsMap.Count(), outList.Capacity);
		foreach (var nba in newBlobAssetsMap)
		{
			var rdp = new GPURigDefinitionPlacementData()
			{
				rigBlob = nba.Value,
			};
			outList.Add(rdp);
		}
	}
}

//-----------------------------------------------------------------------------------------------------------------//

[BurstCompile]
struct CreateNewSkinnedMeshesDataListJob: IJob
{
	[ReadOnly]
	public NativeParallelHashMap<Hash128, GPUSkinnedMeshPlacementData> newSkinnedMeshesDataMap;
	public NativeList<GPUSkinnedMeshPlacementData> outList;
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public void Execute()
	{
		outList.Capacity = math.max(newSkinnedMeshesDataMap.Count(), outList.Capacity);
		foreach (var nba in newSkinnedMeshesDataMap)
		{
			outList.Add(nba.Value);
		}
	}
}

//-----------------------------------------------------------------------------------------------------------------//

[BurstCompile]
partial struct NewFrameRigToSkinnedMeshRemapTablesJob: IJobEntity
{
	[ReadOnly]
	public NativeParallelHashMap<Hash128, int> skinnedMeshesDataMap;
	[ReadOnly]
	public ComponentLookup<GPUAnimationEngineTag> gpuAnimationEngineComponentLookup;
	[ReadOnly]
	public ComponentLookup<RigDefinitionComponent> rigDefComponentLookup;

	[WriteOnly, NativeDisableContainerSafetyRestriction]
	public NativeParallelHashMap<Hash128, GPUSkinnedMeshPlacementData>.ParallelWriter newFrameSkinnedMeshData;
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void Execute(in AnimatedSkinnedMeshComponent asmc)
	{
		if (!asmc.IsGPUAnimator(gpuAnimationEngineComponentLookup))
			return;
		
		rigDefComponentLookup.TryGetComponent(asmc.animatedRigEntity, out var rigDef);
		var hash = AnimationUtils.CalculateBoneRemapTableHash(asmc.smrInfoBlob, rigDef.rigBlob);
		
		if (skinnedMeshesDataMap.TryGetValue(hash, out _))
			return;

		var remapTableBlob = AnimationUtils.MakeSkinnedMeshToRigRemapTable(asmc, rigDef, Allocator.TempJob);
		var skinnedMeshInfo = new GPUSkinnedMeshPlacementData()
		{
			hash = hash,
			dataOffset = -1,
			skinnedMeshInfo = asmc.smrInfoBlob,
			boneRemapTableBlob = remapTableBlob
		};
		if (!newFrameSkinnedMeshData.TryAdd(hash, skinnedMeshInfo))
			remapTableBlob.Dispose();
	}
}

//-----------------------------------------------------------------------------------------------------------------//

[BurstCompile]
struct CalculateNewAnimationOffsets: IJobParallelForDefer
{
	[NativeDisableParallelForRestriction]
	public NativeList<GPUAnimationClipPlacementData> newAnimationClips;
	
	[NativeDisableUnsafePtrRestriction]
	public UnsafeAtomicCounter32 totalAnimationClipsOffsetCounter;
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public void Execute(int index)
	{
		ref var ac = ref newAnimationClips.ElementAt(index);
		ref var acb = ref ac.animationClipBlob.Value;
		var clipSize = CalculateGPUAnimationClipPlacementData(ref acb, ref ac);
		ac.animationClipDataOffset = totalAnimationClipsOffsetCounter.Add(clipSize);
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	int CalculateGPUAnimationClipPlacementData(ref AnimationClipBlob acb, ref GPUAnimationClipPlacementData acpd)
	{
		var totalBytes = 0;
		totalBytes += UnsafeUtility.SizeOf<GPUStructures.AnimationClip>();
		totalBytes = CalculateGPUTrackSetSize(totalBytes, ref acb.clipTracks, ref acpd.clipTracks);
		totalBytes = CalculateGPUTrackSetSize(totalBytes, ref acb.additiveReferencePoseFrame, ref acpd.additiveRefPoseTracks);
		return totalBytes;
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	int CalculateGPUTrackSetSize(int currentOffset, ref TrackSet ts, ref GPUTrackSetPlacementData tpd)
	{
		var rv = currentOffset;
		if (ts.keyframes.Length == 0)
		{
			tpd = GPUTrackSetPlacementData.Empty();
			return rv;
		}
				
		tpd.keyFramesOffset = rv;
		rv += ts.keyframes.Length * UnsafeUtility.SizeOf<GPUStructures.KeyFrame>();
		
		tpd.tracksOffset = rv;
		rv += ts.tracks.Length * UnsafeUtility.SizeOf<GPUStructures.Track>();
		
		tpd.trackGroupsOffset = rv;
		rv += ts.trackGroups.Length * UnsafeUtility.SizeOf<int>();
		
		tpd.hashTableOffset = rv;
		rv += ts.trackGroupPHT.pht.Length * UnsafeUtility.SizeOf<int2>();
		return rv;
	}
}

//-----------------------------------------------------------------------------------------------------------------//

[BurstCompile]
struct CalculateNewAvatarMasksOffsetsJob: IJobParallelForDefer
{
	[NativeDisableParallelForRestriction]
	public NativeList<GPUAvatarMaskPlacementData> newAvatarMasks;
	
	[NativeDisableUnsafePtrRestriction]
	public UnsafeAtomicCounter32 totalAvatarMasksCounter;
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public void Execute(int index)
	{
		ref var am = ref newAvatarMasks.ElementAt(index);
		ref var amb = ref am.avatarMaskBlob.Value;
		//	Plus one because we need space for human body parts mask
		am.dataOffset = totalAvatarMasksCounter.Add(amb.includedBoneMask.Length + 1);
	}
}

//-----------------------------------------------------------------------------------------------------------------//

[BurstCompile]
struct CalculateNewRigsOffsets: IJobParallelForDefer
{
	[NativeDisableParallelForRestriction]
	public NativeList<GPURigDefinitionPlacementData> newRigs;
	
	[NativeDisableUnsafePtrRestriction]
	public UnsafeAtomicCounter32 totalGPURigsCount;
	[NativeDisableUnsafePtrRestriction]
	public UnsafeAtomicCounter32 totalGPURigBonesCount;
	[NativeDisableUnsafePtrRestriction]
	public UnsafeAtomicCounter32 totalGPUHumanRotationDataEntriesCount;
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public void Execute(int index)
	{
		ref var rig = ref newRigs.ElementAt(index);
		rig.rigDefinitionIndex = totalGPURigsCount.Add(1);
		ref var rb = ref rig.rigBlob.Value;
		rig.rigBonesOffset = totalGPURigBonesCount.Add(rb.bones.Length);
		rig.humanRotationDataOffset = -1;
		if (rb.humanData.IsValid)
			rig.humanRotationDataOffset = totalGPUHumanRotationDataEntriesCount.Add(rb.humanData.Value.humanRotData.Length);
	}
}

//-----------------------------------------------------------------------------------------------------------------//

[BurstCompile]
struct CalculateNewBoneRemapTablesOffsetsJob: IJobParallelForDefer
{
	[NativeDisableParallelForRestriction]
	public NativeList<GPUSkinnedMeshPlacementData> newBoneRemapTables;
	
	[NativeDisableUnsafePtrRestriction]
	public UnsafeAtomicCounter32 totalGPUBoneRemapTablesCount;
	[NativeDisableUnsafePtrRestriction]
	public UnsafeAtomicCounter32 totalGPUBoneRemapIndicesCount;
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public void Execute(int index)
	{
		ref var rig = ref newBoneRemapTables.ElementAt(index);
		rig.dataOffset = totalGPUBoneRemapIndicesCount.Add(rig.boneRemapTableBlob.Value.remapIndices.Length);
		totalGPUBoneRemapTablesCount.Add(1);
	}
}

//-----------------------------------------------------------------------------------------------------------------//

[BurstCompile]
unsafe struct RegisterNewResourcesJob: IJob
{
	[ReadOnly]
	public NativeList<GPUAnimationClipPlacementData> newAnimations;
	[ReadOnly]
	public NativeList<GPURigDefinitionPlacementData> newRigDefs;
	[ReadOnly]
	public NativeList<GPUSkinnedMeshPlacementData> newSkinnedMeshesDatas;
	[ReadOnly]
	public NativeList<GPUAvatarMaskPlacementData> newAvatarMasks;
	
	public NativeParallelHashMap<Hash128, int> animationClipsOffsets;
	public NativeParallelHashMap<Hash128, int> rigDefinitionOffsets;
	public NativeParallelHashMap<Hash128, int> skinnedMeshDataOffsets;
	public NativeParallelHashMap<Hash128, int> avatarMasksDataOffsets;
	
	[NativeDisableUnsafePtrRestriction]
	public uint *maximumKeyFrameArrayLength;
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public void Execute()
	{
		foreach (var a in newAnimations)
		{
			animationClipsOffsets.Add(a.animationClipBlob.Value.hash, a.animationClipDataOffset);
			*maximumKeyFrameArrayLength = math.max(*maximumKeyFrameArrayLength, a.animationClipBlob.Value.maxTrackKeyframeLength);
		}

		foreach (var a in newRigDefs)
		{
			rigDefinitionOffsets.Add(a.rigBlob.Value.hash, a.rigDefinitionIndex);
		}
		
		foreach (var a in newSkinnedMeshesDatas)
		{
			skinnedMeshDataOffsets.Add(a.hash, a.dataOffset);
		}
		
		foreach (var a in newAvatarMasks)
		{
			avatarMasksDataOffsets.Add(a.avatarMaskBlob.Value.hash, a.dataOffset);
		}
	}
}

}
}

#endif