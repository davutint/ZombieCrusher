#ifndef TRACK_SET_HLSL_
#define TRACK_SET_HLSL_

/////////////////////////////////////////////////////////////////////////////////

#include "PerfectHashTable.hlsl"

/////////////////////////////////////////////////////////////////////////////////

struct TrackSet
{
   	int keyFramesOffset;
	int tracksOffset;
	int trackGroupsOffset;
	int trackGroupPHTOffset;
    uint trackGroupPHTSeed;
    uint trackGroupPHTSizeMask;

    static const uint size = 6 * 4;

/////////////////////////////////////////////////////////////////////////////////

    void OffsetByAddress(int baseAddress)
    {
        keyFramesOffset += baseAddress;
        tracksOffset += baseAddress;
        trackGroupsOffset += baseAddress;
        trackGroupPHTOffset += baseAddress;
    }

/////////////////////////////////////////////////////////////////////////////////

    static TrackSet ReadFromRawBuffer(ByteAddressBuffer b, int byteAddress)
    {
        TrackSet rv = (TrackSet)0;
        rv.keyFramesOffset = b.Load(byteAddress);
        rv.tracksOffset = b.Load(byteAddress + 4);
        rv.trackGroupsOffset = b.Load(byteAddress + 8);
        rv.trackGroupPHTOffset = b.Load(byteAddress + 12);
        rv.trackGroupPHTSeed = b.Load(byteAddress + 16);
        rv.trackGroupPHTSizeMask = b.Load(byteAddress + 20);

        CHECK_RAW_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_GPUANIMATOR_TRACK_SET_READ, byteAddress, size, b);

        return rv;
    }
    
/////////////////////////////////////////////////////////////////////////////////

	int GetTrackGroupIndex(uint boneHash)
    {
        int rv = QueryPerfectHashTable(boneHash, trackGroupPHTSeed, trackGroupPHTOffset, trackGroupPHTSizeMask);
        return rv;
    }
};

#endif


