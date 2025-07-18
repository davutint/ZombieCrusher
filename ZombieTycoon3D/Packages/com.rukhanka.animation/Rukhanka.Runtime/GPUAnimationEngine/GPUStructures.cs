
using Unity.Collections;
using Unity.Mathematics;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.GPUStructures
{
internal struct KeyFrame
{
    public float v;
    public float inTan, outTan;
    public float time;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

internal struct Track
{
    public uint props;
    public int2 keyFrameRange;
    
    public uint bindingType
    {
        get => props & 0xf;
        set => props = value | props & 0xfffffff0;
    }
    
    public uint channelIndex
    {
        get => props >> 4 & 3;
        set => props = value << 4 | props & 0xffffffcf;
    }
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

internal struct TrackSet
{
	public int keyFramesOffset;
	public int tracksOffset;
	public int trackGroupsOffset;
	public int trackGroupPHTOffset;
    public uint trackGroupPHTSeed;
    public uint trackGroupPHTSizeMask;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

internal struct AnimationClip
{
    public uint4 hash;
    public TrackSet clipTracks;
    public TrackSet additiveReferencePoseTracks;
    public uint flags;
	public float cycleOffset;
	public float length;
    
    public bool looped
    {
        get => new BitField32(flags).IsSet(1);
        set
        {
            var k = new BitField32(flags);
            k.SetBits(1, value);
            flags = k.Value;
        }
    }
    public bool loopPoseBlend
    {
        get => new BitField32(flags).IsSet(2);
        set
        {
            var k = new BitField32(flags);
            k.SetBits(2, value);
            flags = k.Value;
        }
    }
    public bool hasRootMotionCurves
    {
        get => new BitField32(flags).IsSet(3);
        set
        {
            var k = new BitField32(flags);
            k.SetBits(3, value);
            flags = k.Value;
        }
    }
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

internal struct BoneTransform
{
    public float3 pos;
    public float4 rot;
    public float3 scale;
    
    public BoneTransform(Rukhanka.BoneTransform bt)
    {
        pos = bt.pos;
        rot = bt.rot.value;
        scale = bt.scale;
    }
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

internal struct RigBone
{
    public uint hash;
    public int parentBoneIndex;
    public BoneTransform refPose;
    public int humanBodyPart;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

internal struct RigDefinition
{
    public uint4 hash;
    public int2 rigBonesRange;
    public int rootBoneIndex;
    public int humanRotationDataOffset;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

internal struct AnimationJob
{
    public int rigDefinitionIndex;
    public int animatedBoneIndexOffset;
    public int2 animationsToProcessRange;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

internal struct AnimationToProcess
{
    public int animationClipAddress;
    public float weight;
    public float time;
    public int blendMode;
	public float layerWeight;
	public int layerIndex;
    public int avatarMaskDataOffset;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

internal struct AnimatedBoneWorkload
{
    public int boneIndexInRig;
    public int animationJobIndex;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

internal struct SkinnedMeshWorkload
{
    public int skinMatrixBaseOutIndex;
    public int boneRemapTableIndex;
    public int skinMatricesCount;
    public int rootBoneIndex;
    public int animatedBoneIndexOffset;
    public float4x4 skinnedMeshInverseTransform;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

internal struct SkinnedMeshBoneData
{
    public int boneRemapIndex;
    public float3x4 bindPose;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

internal struct HumanRotationData
{
	public float3 minMuscleAngles, maxMuscleAngles;
	public float4 preRot, postRot;
	public float3 sign;
}
}
