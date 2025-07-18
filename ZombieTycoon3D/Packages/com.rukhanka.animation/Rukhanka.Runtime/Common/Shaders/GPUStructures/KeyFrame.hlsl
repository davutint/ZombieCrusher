#ifndef KEYFRAME_HLSL_
#define KEYFRAME_HLSL_

/////////////////////////////////////////////////////////////////////////////////

struct KeyFrame
{
    float v;
    float inTan;
    float outTan;
    float time;

    static const uint size = 4 * 4;

    static KeyFrame ReadFromRawBuffer(ByteAddressBuffer b, int baseAddress, int index)
    {
        int addr = baseAddress + index * size;
        float4 v = asfloat(b.Load4(addr));

        KeyFrame rv = (KeyFrame)0;
        rv.v = v.x;
        rv.inTan = v.y;
        rv.outTan = v.z;
        rv.time = v.w;

        //CHECK_RAW_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_GPUANIMATOR_KEY_FRAME_READ, addr, size, b);

        return rv;
    }
};

/////////////////////////////////////////////////////////////////////////////////

#endif


