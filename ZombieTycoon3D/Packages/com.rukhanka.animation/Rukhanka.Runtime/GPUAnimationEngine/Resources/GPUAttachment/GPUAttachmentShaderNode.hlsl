#pragma once

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.rukhanka.animation/Rukhanka.Runtime/GPUAnimationEngine/Resources/GPUStructures.hlsl"

/////////////////////////////////////////////////////////////////////////////////

#if defined(DOTS_INSTANCING_ON)

UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(int, _RukhankaGPUBoneIndex)
    UNITY_DOTS_INSTANCED_PROP(float4x4, _RukhankaAttachmentToBoneTransform)
    UNITY_DOTS_INSTANCED_PROP(float4x4, _RukhankaAnimatedEntityLocalToWorld)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

StructuredBuffer<BoneTransform> boneLocalTransforms;
StructuredBuffer<BoneTransform> rigSpaceBoneTransformsBuf;

#endif

/////////////////////////////////////////////////////////////////////////////////

void GPUAttachmentMeshMover_float(in float3 vertex, in float3 normal, in float3 tangent, out float3 animatedVertex, out float3 animatedNormal, out float3 animatedTangent)
{
#if defined(DOTS_INSTANCING_ON)
    int gpuBoneIndex = UNITY_ACCESS_DOTS_INSTANCED_PROP(int, _RukhankaGPUBoneIndex);
    if (gpuBoneIndex < 0)
    {
        animatedVertex = vertex;
        animatedNormal = normal;
        animatedTangent = tangent;
        return;
    }

    float4x4 attachmentToBoneTransform = UNITY_ACCESS_DOTS_INSTANCED_PROP(float4x4, _RukhankaAttachmentToBoneTransform);
    float4x4 entityRootLocalToWorld = UNITY_ACCESS_DOTS_INSTANCED_PROP(float4x4, _RukhankaAnimatedEntityLocalToWorld);

    animatedVertex = mul(attachmentToBoneTransform, float4(vertex, 1)).xyz;
    BoneTransform bt = rigSpaceBoneTransformsBuf[gpuBoneIndex];

    float3 transformedPos = BoneTransform::TransformPoint(bt, animatedVertex);
    animatedVertex = transformedPos;
    animatedVertex = mul(entityRootLocalToWorld, float4(animatedVertex, 1)).xyz;
    animatedVertex = GetCameraRelativePositionWS(animatedVertex);
    animatedVertex = TransformWorldToObject(animatedVertex);

    animatedNormal = mul((float3x3)attachmentToBoneTransform, normal);
    animatedNormal = BoneTransform::TransformDirection(bt, animatedNormal);
    animatedNormal = mul((float3x3)entityRootLocalToWorld, animatedNormal);
    animatedNormal = TransformWorldToObjectDir(animatedNormal);

    animatedTangent = mul((float3x3)attachmentToBoneTransform, tangent);
    animatedTangent = BoneTransform::TransformDirection(bt, animatedTangent);
    animatedTangent = mul((float3x3)entityRootLocalToWorld, animatedTangent);
    animatedTangent = TransformWorldToObjectDir(animatedTangent);
#else
    animatedVertex = vertex;
    animatedNormal = normal;
    animatedTangent = tangent;
#endif
}
