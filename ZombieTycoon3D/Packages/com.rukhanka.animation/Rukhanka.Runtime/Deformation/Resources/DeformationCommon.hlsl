#ifndef DEFORMATION_COMMON_HLSL_
#define DEFORMATION_COMMON_HLSL_

/////////////////////////////////////////////////////////////////////////////////

struct SourceSkinnedMeshVertex
{
#ifndef RUKHANKA_INPLACE_SKINNING
    float3 position;
    float3 normal;
    float3 tangent;
    static const int size = 40;
#else
    static const int size = 4;
#endif
    uint boneWeightsOffset;
    uint boneWeightsCount;

    static uint GetBoneWeightsOffsetFromPackedUINT(uint boneWeightsOffsetAndCount)
    {
        return boneWeightsOffsetAndCount >> 8;
    }

    static uint GetBoneWeightsCountFromPackedUINT(uint boneWeightsOffsetAndCount)
    {
        return boneWeightsOffsetAndCount & 0xff;
    }

    static uint PackBoneOffsetAndCount(uint count, uint offset)
    {
        return count | (offset << 8);
    }

    void WriteIntoRawBuffer(RWByteAddressBuffer outBuffer, uint index)
    {
        uint boneWeightsOffsetAndCount = PackBoneOffsetAndCount(boneWeightsCount, boneWeightsOffset);

        uint byteOffset = index * size;
        CHECK_RAW_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_DEFORMATION_SKINNED_MESH_VERTEX_WRITE, byteOffset, size, outBuffer);
    #ifndef RUKHANKA_INPLACE_SKINNING
        uint4 u0 = asuint(float4(position, normal.x));
        uint4 u1 = asuint(float4(normal.yz, tangent.xy));
        uint2 u2 = uint2(asuint(tangent.z), boneWeightsOffsetAndCount);
        outBuffer.Store4(byteOffset + 0,  u0);
        outBuffer.Store4(byteOffset + 16, u1);
        outBuffer.Store2(byteOffset + 32, u2);
    #else
        outBuffer.Store(byteOffset + 0, boneWeightsOffsetAndCount);
    #endif
    }

    static SourceSkinnedMeshVertex ReadFromRawBuffer(ByteAddressBuffer inBuffer, uint index)
    {
        SourceSkinnedMeshVertex rv;

        uint byteOffset = index * size;
        CHECK_RAW_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_DEFORMATION_SKINNED_MESH_VERTEX_READ, byteOffset, size, inBuffer);
    #ifndef RUKHANKA_INPLACE_SKINNING
        uint4 u0 = inBuffer.Load4(byteOffset + 0);
        uint4 u1 = inBuffer.Load4(byteOffset + 16);
        uint2 u2 = inBuffer.Load2(byteOffset + 32);

        rv.position = asfloat(u0.xyz);
        rv.normal = asfloat(uint3(u0.w, u1.xy));
        rv.tangent = asfloat(uint3(u1.zw, u2.x));
        uint boneWeightsOffsetAndCount = u2.y;
    #else
        uint boneWeightsOffsetAndCount = inBuffer.Load(byteOffset + 0);
    #endif

        rv.boneWeightsOffset = GetBoneWeightsOffsetFromPackedUINT(boneWeightsOffsetAndCount);
        rv.boneWeightsCount = GetBoneWeightsCountFromPackedUINT(boneWeightsOffsetAndCount);

        return rv;
    }
};

/////////////////////////////////////////////////////////////////////////////////

struct DeformedVertex
{
    float3 position;
    float3 normal;
    float3 tangent;

    static const int size = 3 * 3 * 4;

    static DeformedVertex ReadFromRawBuffer(ByteAddressBuffer inBuffer, uint index)
    {
        uint byteOffset = index * size;
        uint4 v0 = inBuffer.Load4(byteOffset + 0);
        uint4 v1 = inBuffer.Load4(byteOffset + 16);
        uint  v2 = inBuffer.Load(byteOffset + 32);
        CHECK_RAW_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_DEFORMATION_DEFORMED_VERTEX_READ, byteOffset, size * 4, inBuffer);

        DeformedVertex rv;
        rv.position = asfloat(v0.xyz);
        rv.normal = asfloat(uint3(v0.w, v1.xy));
        rv.tangent = asfloat(uint3(v1.zw, v2.x));
        return rv;
    }

    void Scale(float v)
    {
        position *= v;
        normal *= v;
        tangent *= v;
    }
};

/////////////////////////////////////////////////////////////////////////////////

struct PackedDeformedVertex
{
    uint4 pack0;
    uint pack1;

    DeformedVertex Unpack()
    {
        DeformedVertex rv;

        rv.position.x = f16tof32(pack0.x >> 16);
        rv.position.y = f16tof32(pack0.x);
        rv.position.z = f16tof32(pack0.y >> 16);

        rv.normal.x = f16tof32(pack0.y);
        rv.normal.y = f16tof32(pack0.z >> 16);
        rv.normal.z = f16tof32(pack0.z);

        rv.tangent.x = f16tof32(pack0.w >> 16);
        rv.tangent.y = f16tof32(pack0.w);
        rv.tangent.z = f16tof32(pack1 >> 16);

        return rv;
    }

    static PackedDeformedVertex Pack(DeformedVertex v)
    {
        PackedDeformedVertex rv;

        uint3 p = f32tof16(v.position);
        uint3 n = f32tof16(v.normal);
        uint3 t = f32tof16(v.tangent);

        rv.pack0.x = p.x << 16 | p.y;
        rv.pack0.y = p.z << 16 | n.x;
        rv.pack0.z = n.y << 16 | n.z;
        rv.pack0.w = t.x << 16 | t.y;
        rv.pack1   = t.z << 16;

        return rv;
    }
};

/////////////////////////////////////////////////////////////////////////////////

struct BoneInfluence
{
    float weight;
    int boneIndex;

    static BoneInfluence ReadFromRawBuffer(ByteAddressBuffer inBuffer, uint index)
    {
        uint byteOffset = index * 8;
        uint2 u0 = inBuffer.Load2(byteOffset + 0);
        CHECK_RAW_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_DEFORMATION_BONE_INFLUENCE_READ, byteOffset, 8, inBuffer);

        BoneInfluence rv;
        rv.weight = asfloat(u0.x);
        rv.boneIndex = u0.y;
        return rv;
    }
};

/////////////////////////////////////////////////////////////////////////////////

struct MeshFrameDeformationDescription
{
    int baseSkinMatrixIndex;
    int baseBlendShapeWeightIndex;
	int baseOutVertexIndex;
	int baseInputMeshVertexIndex;
	int baseInputMeshBlendShapeIndex;
	int meshVerticesCount;
	int meshBlendShapesCount;
};

/////////////////////////////////////////////////////////////////////////////////

struct InputBlendShapeVertex
{
    uint meshVertexIndex;
    float3 positionDelta;
    float3 normalDelta;
    float3 tangentDelta;

    static const int size = (1 + 3 * 3) * 4;
};


/////////////////////////////////////////////////////////////////////////////////

StructuredBuffer<MeshFrameDeformationDescription> frameDeformedMeshes;

#endif // DEFORMATION_COMMON_HLSL_
