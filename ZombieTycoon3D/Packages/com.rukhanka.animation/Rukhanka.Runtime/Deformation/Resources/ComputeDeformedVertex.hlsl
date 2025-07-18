#pragma once

/////////////////////////////////////////////////////////////////////////////////

#include "Packages/com.rukhanka.animation/Rukhanka.Runtime/Common/Shaders/ShaderConf.hlsl"
#include "Packages/com.rukhanka.animation/Rukhanka.Runtime/Common/Shaders/Debug.hlsl"
#include "Packages/com.rukhanka.animation/Rukhanka.Runtime/Deformation/Resources/DeformationCommon.hlsl"
#ifdef RUKHANKA_INPLACE_SKINNING
#include "Packages/com.rukhanka.animation/Rukhanka.Runtime/Deformation/Resources/Skinning.hlsl"
#endif

#ifdef RUKHANKA_HALF_DEFORMED_DATA
StructuredBuffer<PackedDeformedVertex> _DeformedMeshData;
#else
StructuredBuffer<DeformedVertex> _DeformedMeshData;
#endif

#undef _DeformedMeshIndex
#undef _DeformationParamsForMotionVectors

/////////////////////////////////////////////////////////////////////////////////

void ComputeDeformedVertex_float(in uint vertexID, in float3 vertex, in float3 normal, in float3 tangent, out float3 deformedVertex, out float3 deformedNormal, out float3 deformedTangent)
{
    deformedVertex = vertex;
    deformedNormal = normal;
    deformedTangent = tangent;

#ifdef DOTS_INSTANCING_ON
#ifndef RUKHANKA_INPLACE_SKINNING

//-------- Preskinned data code path -------------//

#ifdef ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
    const uint4 materialProperty = asuint(UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, _DeformationParamsForMotionVectors));
    const uint currentFrameIndex = materialProperty[2];
    const uint index = materialProperty[currentFrameIndex];
#else
    const uint index = asuint(UNITY_ACCESS_DOTS_INSTANCED_PROP(float, _DeformedMeshIndex));
#endif  // ENABLE_DOTS_DEFORMATION_MOTION_VECTORS

#ifdef RUKHANKA_HALF_DEFORMED_DATA
    PackedDeformedVertex vertexData = _DeformedMeshData[index + vertexID];
    DeformedVertex v = vertexData.Unpack();
#else
    DeformedVertex v = _DeformedMeshData[index + vertexID];
#endif  // RUKHANKA_HALF_DEFORMED_DATA

    deformedVertex = v.position;
    deformedNormal = v.normal;
    deformedTangent = v.tangent;

//-------- Inplace skinning code path -------------//

#else

    DeformedVertex dv;
    dv.position = vertex;
    dv.normal = normal;
    dv.tangent = tangent;

#ifdef ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
    const uint4 materialProperty = asuint(UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, _DeformationParamsForMotionVectors));
    const uint currentFrameIndex = materialProperty[2];
    const uint frameDeformedMeshIndex = materialProperty[currentFrameIndex];
#else
    const uint frameDeformedMeshIndex = asuint(UNITY_ACCESS_DOTS_INSTANCED_PROP(float, _DeformedMeshIndex));
#endif  // ENABLE_DOTS_DEFORMATION_MOTION_VECTORS

    if (frameDeformedMeshIndex != 0xffffffff)
    {
        MeshFrameDeformationDescription mfd = frameDeformedMeshes[frameDeformedMeshIndex];

        uint absoluteInputMeshVertexIndex = vertexID + mfd.baseInputMeshVertexIndex;
        SourceSkinnedMeshVertex smv = SourceSkinnedMeshVertex::ReadFromRawBuffer(inputMeshVertexData, absoluteInputMeshVertexIndex);

        dv = ApplyBlendShapes(dv, vertexID, mfd);
        dv = ApplySkinMatrices(dv, smv.boneWeightsOffset, smv.boneWeightsCount, mfd);
    }
    else
    {
        dv = (DeformedVertex)0;
    }

    deformedVertex = dv.position;
    deformedNormal = dv.normal;
    deformedTangent = dv.tangent;

#endif // RUKHANKA_INPLACE_SKINNING
#endif // DOTS_INSTANCING_ON
}
