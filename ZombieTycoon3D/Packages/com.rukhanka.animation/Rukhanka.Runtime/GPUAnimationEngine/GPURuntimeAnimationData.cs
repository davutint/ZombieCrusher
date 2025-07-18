
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
public struct GPUTrackSetPlacementData
{
    public int keyFramesOffset;
    public int tracksOffset;
    public int trackGroupsOffset;
    public int hashTableOffset;
    
    internal GPUStructures.TrackSet ToGPUTrackSet()
    {
        var rv = new GPUStructures.TrackSet()
        {
            tracksOffset = tracksOffset,
            keyFramesOffset = keyFramesOffset,
            trackGroupsOffset = trackGroupsOffset,
            trackGroupPHTOffset = hashTableOffset,
        };
        return rv;
    }
    
    internal static GPUTrackSetPlacementData Empty()
    {
        var rv = new GPUTrackSetPlacementData()
        {
            tracksOffset = -1,
            keyFramesOffset = -1,
            trackGroupsOffset = -1,
            hashTableOffset = -1,
        };
        return rv;
    }
}

//-----------------------------------------------------------------------------------------------------------------//

public struct GPUAnimationClipPlacementData
{
    public BlobAssetReference<AnimationClipBlob> animationClipBlob;
    public int animationClipDataOffset;
    public GPUTrackSetPlacementData clipTracks;
    public GPUTrackSetPlacementData additiveRefPoseTracks;
}
    
//-----------------------------------------------------------------------------------------------------------------//

public struct GPURigDefinitionPlacementData
{
    public BlobAssetReference<RigDefinitionBlob> rigBlob;
    public int rigDefinitionIndex;
    public int rigBonesOffset;
    public int humanRotationDataOffset;
}

//-----------------------------------------------------------------------------------------------------------------//

public struct GPUSkinnedMeshPlacementData
{
    public BlobAssetReference<BoneRemapTableBlob> boneRemapTableBlob;
    public BlobAssetReference<SkinnedMeshInfoBlob> skinnedMeshInfo;
    public Hash128 hash;
    public int dataOffset;
}

//-----------------------------------------------------------------------------------------------------------------//

public struct GPUAvatarMaskPlacementData
{
    public BlobAssetReference<AvatarMaskBlob> avatarMaskBlob;
    public int dataOffset;
}
    
//-----------------------------------------------------------------------------------------------------------------//

public struct GPURuntimeAnimationData: IComponentData, IDisposable
{
    public NativeList<GPUAnimationClipPlacementData> newAnimationsList;
    public NativeList<GPURigDefinitionPlacementData> newRigsList;
    public NativeList<GPUSkinnedMeshPlacementData> newSkinnedMeshesDataList;
    public NativeList<GPUAvatarMaskPlacementData> newAvatarMasksList;
    
	public NativeParallelHashMap<Hash128, int> animationClipsMap;
    public uint2 totalGPUAnimationClipsSize;
    public uint maxTrackKeyframesCount;
    
	public NativeParallelHashMap<Hash128, int> rigDefinitionsMap;
    public uint2 totalGPURigsCount;
    public uint2 totalGPUHumanRotationDataEntriesCount;
    public uint2 totalGPURigBonesCount;
    
    public NativeParallelHashMap<Hash128, int> skinnedMeshesDataMap;
    public uint2 totalGPUSkinnedMeshBonesCount;
    public uint2 totalGPUSkinnedMeshesCount;
    
    public NativeParallelHashMap<Hash128, int> avatarMasksDataMap;
    public uint2 totalGPUAvatarMasksDataCount;
    
    public uint frameAnimatedBonesCounter;
	public uint frameAnimatedRigsCounter;
	public uint frameAnimationToProcessCounter;
	public uint frameSkinnedMeshesCounter;
    
    public struct FrameOffsets
    {
        public int boneIndex;
        public int rigIndex;
        public int animationToProcessIndex;
    }
    
    public NativeParallelHashMap<Entity, FrameOffsets> frameEntityAnimatedDataOffsetsMap;
        
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static GPURuntimeAnimationData Construct()
    {
        var initialCapacity = 0x1000;
        var rv = new GPURuntimeAnimationData();
        rv.newAnimationsList = new (initialCapacity, Allocator.Persistent);
        rv.newRigsList = new (initialCapacity, Allocator.Persistent);
        rv.newSkinnedMeshesDataList = new (initialCapacity, Allocator.Persistent);
        rv.newAvatarMasksList = new (initialCapacity, Allocator.Persistent);
        rv.animationClipsMap = new (initialCapacity, Allocator.Persistent);
        rv.rigDefinitionsMap = new (initialCapacity, Allocator.Persistent);
        rv.skinnedMeshesDataMap = new (initialCapacity, Allocator.Persistent);
        rv.avatarMasksDataMap = new (initialCapacity, Allocator.Persistent);
        
		rv.totalGPUAnimationClipsSize = 0;
		rv.totalGPURigsCount = 0;
        rv.totalGPUHumanRotationDataEntriesCount = 0;
		rv.totalGPURigBonesCount = 0;
        rv.totalGPUSkinnedMeshBonesCount = 0;
        rv.totalGPUSkinnedMeshesCount = 0;
        rv.totalGPUAvatarMasksDataCount = 0;
        rv.maxTrackKeyframesCount = 0;
        
        rv.frameAnimatedBonesCounter = 0;
        rv.frameAnimatedRigsCounter = 0;
        rv.frameAnimationToProcessCounter = 0;
        rv.frameSkinnedMeshesCounter = 0;
        rv.frameEntityAnimatedDataOffsetsMap = new (initialCapacity, Allocator.Persistent);
        
        return rv;
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
    public void Dispose()
    {
        newAnimationsList.Dispose();
        newRigsList.Dispose();
        newSkinnedMeshesDataList.Dispose();
        newAvatarMasksList.Dispose();
        animationClipsMap.Dispose();
        rigDefinitionsMap.Dispose();
        skinnedMeshesDataMap.Dispose();
        frameEntityAnimatedDataOffsetsMap.Dispose();
        avatarMasksDataMap.Dispose();
    }
}

}
