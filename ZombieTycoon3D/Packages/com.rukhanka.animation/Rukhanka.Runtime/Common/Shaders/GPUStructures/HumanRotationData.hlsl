#ifndef HUMAN_ROTATION_DATA_HLSL_
#define HUMAN_ROTATION_DATA_HLSL_

/////////////////////////////////////////////////////////////////////////////////

struct HumanRotationData
{
	float3 minMuscleAngles, maxMuscleAngles;
	Quaternion preRot, postRot;
	float3 sign;

    static const uint size = (3 + 3 + 4 + 4 + 3) * 4;

    static HumanRotationData ReadFromRawBuffer(ByteAddressBuffer b, int index)
    {
        int byteAddress = index * size;

        HumanRotationData rv = (HumanRotationData)0;
        rv.minMuscleAngles  = asfloat(b.Load3(byteAddress + 0));
        rv.maxMuscleAngles  = asfloat(b.Load3(byteAddress + 12));
        rv.preRot.value     = asfloat(b.Load4(byteAddress + 24));
        rv.postRot.value    = asfloat(b.Load4(byteAddress + 40));
        rv.sign             = asfloat(b.Load3(byteAddress + 56));

        CHECK_RAW_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_GPUANIMATOR_HUMAN_ROTATION_DATA_READ, byteAddress, size, b);

        return rv;
    }
};

/////////////////////////////////////////////////////////////////////////////////

#endif


