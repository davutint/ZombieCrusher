#ifndef RIG_DEFINITION_HLSL_
#define RIG_DEFINITION_HLSL_

/////////////////////////////////////////////////////////////////////////////////

struct RigDefinition
{
    uint4 hash;
    int2 rigBonesRange;
    int rootBoneIndex;
    int humanRotationDataRange;

    static const uint size = (4 + 2 + 1 + 1) * 4;

    static RigDefinition ReadFromRawBuffer(ByteAddressBuffer b, int index)
    {
        int byteAddress = index * size;

        RigDefinition rv = (RigDefinition)0;
        rv.hash = b.Load4(byteAddress);
        rv.rigBonesRange = b.Load2(byteAddress + 16);
        rv.rootBoneIndex = b.Load(byteAddress + 24);
        rv.humanRotationDataRange = b.Load(byteAddress + 28);

        CHECK_RAW_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_GPUANIMATOR_RIG_DEFINITION_READ, byteAddress, size, b);

        return rv;
    }
};

/////////////////////////////////////////////////////////////////////////////////

#endif


