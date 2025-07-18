
using System.Runtime.CompilerServices;
using Rukhanka.Toolbox;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using FixedStringName = Unity.Collections.FixedString512Bytes;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
public enum BindingType
{
	Unknown,
	Translation,
	Quaternion,
	EulerAngles,
	HumanMuscle,
	Scale
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public struct KeyFrame
{
	public float v;
	public float inTan, outTan;
	public float time;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public struct AnimationEventBlob
{
#if RUKHANKA_DEBUG_INFO
	public BlobString name;
#endif
	public uint nameHash;
	public float time;
	public float floatParam;
	public int intParam;
	public uint stringParamHash;
#if RUKHANKA_DEBUG_INFO
	public BlobString stringParam;
#endif
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public struct PerfectHashTableBlob
{
	public BlobArray<uint2> pht;
	public uint seed;
	
	public unsafe int Query(uint v)
	{
		return Perfect2HashTable.Query(v, seed, (uint2*)pht.GetUnsafePtr(), pht.Length);
	}
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public enum TrackFrame
{
	First,
	Last,
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public struct Track
{
#if RUKHANKA_DEBUG_INFO
	public FixedString64Bytes name;
#endif
	public uint props;
	public int2 keyFrameRange;
	
	public Track(BindingType bt, uint channelIndex)
	{
		keyFrameRange = 0;
		props = 0;
	#if RUKHANKA_DEBUG_INFO
		name = default;
	#endif
		
		bindingType = bt;
		this.channelIndex = channelIndex;
	}
	
    public BindingType bindingType
    {
        get => (BindingType)(props & 0xf);
        set => props = (uint)value | props & 0xfffffff0;
    }
    
    public uint channelIndex
    {
        get => props >> 4 & 3;
        set => props = value << 4 | props & 0xffffffcf;
    }
	
	//	Zero out last 4 bits, to force Unknown binding type for such tracks
	public static uint CalculateHash(uint h) => h & 0xfffffff0;
	public static uint CalculateHash(in FixedStringName h) => CalculateHash(h.CalculateHash32());
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#if RUKHANKA_DEBUG_INFO
public struct TrackGroupInfo
{
	public FixedString128Bytes name;
	public uint hash;
}
#endif

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public struct TrackSet
{
	public BlobArray<KeyFrame> keyframes;
	public BlobArray<Track> tracks;
	public BlobArray<int> trackGroups;
	public PerfectHashTableBlob trackGroupPHT;
#if RUKHANKA_DEBUG_INFO
	public BlobArray<TrackGroupInfo> trackGroupDebugInfo;
#endif
	
	public int GetTrackGroupIndex(uint boneHash) => trackGroupPHT.Query(boneHash);
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public struct AnimationClipBlob: GenericAssetBlob
{
#if RUKHANKA_DEBUG_INFO
	public BlobString name;
	public string Name() => name.ToString();
	public float bakingTime;
	public float BakingTime() => bakingTime;
#endif
	public Hash128 hash;
	public Hash128 Hash() => hash;
	
	public TrackSet clipTracks;
	public TrackSet additiveReferencePoseFrame;
	public BlobArray<AnimationEventBlob> events;
	
	public uint flags;
	public float cycleOffset;
	public float length;
	public bool looped { get => GetFlag(1); set => SetFlag(1, value); }
	public bool loopPoseBlend { get => GetFlag(2); set => SetFlag(2, value); }
	public uint maxTrackKeyframeLength;
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	void SetFlag(int index, bool value)
	{
		var v = 1u << index;
		var mask = ~v;
		var valueBits = math.select(0, v, value);
		flags = flags & mask | valueBits;
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	bool GetFlag(int index)
	{
		var v = 1u << index;
		return (flags & v) != 0;
	}
}

}
