#ifndef SKINNED_MESH_BONE_HLSL_
#define SKINNED_MESH_BONE_HLSL_

/////////////////////////////////////////////////////////////////////////////////

struct SkinnedMeshBone
{
    int boneRemapIndex;
    float4x4 bindPose;

    static const uint size = (1 + 12) * 4;

    static SkinnedMeshBone ReadFromRawBuffer(ByteAddressBuffer b, int index)
    {
        int byteAddress = index * size;

        SkinnedMeshBone rv = (SkinnedMeshBone)0;
        rv.boneRemapIndex = b.Load(byteAddress);
        rv.bindPose._11_21_31 = asfloat(b.Load3(byteAddress + 4 + 00));
        rv.bindPose._12_22_32 = asfloat(b.Load3(byteAddress + 4 + 12));
        rv.bindPose._13_23_33 = asfloat(b.Load3(byteAddress + 4 + 24));
        rv.bindPose._14_24_34 = asfloat(b.Load3(byteAddress + 4 + 36));
        rv.bindPose._41_42_43_44 = float4(0, 0, 0, 1);

        CHECK_RAW_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_GPUANIMATOR_SKINNED_MESH_BONE_READ, byteAddress, size, b);

        return rv;
    }
};

/////////////////////////////////////////////////////////////////////////////////

#endif


