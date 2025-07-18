#ifndef ANIMATION_CLIP_HLSL_
#define ANIMATION_CLIP_HLSL_

/////////////////////////////////////////////////////////////////////////////////

#include "TrackSet.hlsl"

/////////////////////////////////////////////////////////////////////////////////

struct AnimationClip
{
    uint4 hash;
    TrackSet clipTracks;
    TrackSet additiveReferencePoseTracks;
    uint flags;
    float cycleOffset;
    float length;

    static const uint size = (4 + 1 + 1 + 1) * 4 + TrackSet::size * 2;

/////////////////////////////////////////////////////////////////////////////////

    bool IsLooped() { return GetFlag(1); }
    bool LoopPoseBlend() { return GetFlag(2); }

/////////////////////////////////////////////////////////////////////////////////

	bool GetFlag(int index)
	{
		uint v = 1u << index;
		return (flags & v) != 0;
	}

/////////////////////////////////////////////////////////////////////////////////

    static AnimationClip ReadFromRawBuffer(ByteAddressBuffer b, int byteAddress)
    {
        AnimationClip rv = (AnimationClip)0;
        rv.hash = b.Load4(byteAddress);
        rv.clipTracks = TrackSet::ReadFromRawBuffer(b, byteAddress + 16);
        rv.additiveReferencePoseTracks = TrackSet::ReadFromRawBuffer(b, byteAddress + 16 + TrackSet::size);
        rv.flags = b.Load(byteAddress + 16 + TrackSet::size * 2);
        rv.cycleOffset = asfloat(b.Load(byteAddress + 20 + TrackSet::size * 2));
        rv.length = asfloat(b.Load(byteAddress + 24 + TrackSet::size * 2));

        CHECK_RAW_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_GPUANIMATOR_ANIMATION_CLIP_READ, byteAddress, size, b);

        return rv;
    }
};

#endif


