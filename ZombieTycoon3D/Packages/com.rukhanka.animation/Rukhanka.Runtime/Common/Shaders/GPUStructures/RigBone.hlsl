#ifndef RIG_BONE_HLSL_
#define RIG_BONE_HLSL_

/////////////////////////////////////////////////////////////////////////////////

struct RigBone
{
    uint hash;
    int parentBoneIndex;
    BoneTransform refPose;
    int humanBodyPart;

    static const uint size = (1 + 1 + 1) * 4 + BoneTransform::size;

    static RigBone ReadFromRawBuffer(ByteAddressBuffer b, int index)
    {
        int byteAddress = index * size;

        RigBone rv = (RigBone)0;
        rv.hash = b.Load(byteAddress);
        rv.parentBoneIndex = b.Load(byteAddress + 4);
        rv.refPose.pos = asfloat(b.Load3(byteAddress + 8));
        rv.refPose.rot.value = asfloat(b.Load4(byteAddress + 20));
        rv.refPose.scale = asfloat(b.Load3(byteAddress + 36));
        rv.humanBodyPart = b.Load(byteAddress + 48);

        CHECK_RAW_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_GPUANIMATOR_RIG_BONE_READ, byteAddress, size, b);

        return rv;
    }
};

/////////////////////////////////////////////////////////////////////////////////

#endif


