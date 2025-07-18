#ifndef BONE_RENDERER_GPU_HLSL_
#define BONE_RENDERER_GPU_HLSL_

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#ifdef IS_HDRP
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightDefinition.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariablesGlobal.cs.hlsl"
#endif

/////////////////////////////////////////////////////////////////////////////////

#include "Packages/com.rukhanka.animation/Rukhanka.DebugDrawer/Resources/RukhankaBoneRenderer.hlsl"
#include "Packages/com.rukhanka.animation/Rukhanka.Runtime/GPUAnimationEngine/Resources/GPUStructures.hlsl"

StructuredBuffer<BoneTransform> rigSpaceBoneTransforms;

struct RigVisInfo
{
    float3 pos;
    float scale;
    float4 rot;
};

StructuredBuffer<RigVisInfo> rigVisualizationData;
float4 boneColor;

/////////////////////////////////////////////////////////////////////////////////

VertexToPixel VSGPUAnimator(VertexInput i)
{
	VertexToPixel o = (VertexToPixel)0;

    AnimatedBoneWorkload boneWorkload = animatedBoneWorkload[i.instanceID];
    AnimationJob animationJob = animationJobs[boneWorkload.animationJobIndex];
    RigDefinition rigDef = RigDefinition::ReadFromRawBuffer(rigDefinitions, animationJob.rigDefinitionIndex);
    RigBone rigBone =  RigBone::ReadFromRawBuffer(rigBones, rigDef.rigBonesRange.x + boneWorkload.boneIndexInRig);
    int parentBoneIndex = rigBone.parentBoneIndex;

    if (parentBoneIndex < rigDef.rootBoneIndex)
        return o;

    BoneData bd;
    RigVisInfo rvi = rigVisualizationData[boneWorkload.animationJobIndex];

    int boneId = animationJob.animatedBoneIndexOffset + boneWorkload.boneIndexInRig;
    bd.pos0 = rigSpaceBoneTransforms[boneId].pos;
    if (parentBoneIndex >= 0)
        bd.pos1 = rigSpaceBoneTransforms[animationJob.animatedBoneIndexOffset + parentBoneIndex].pos;

    float3 worldPos = ComputeVertexWorldPos(bd, i.pos, i.vertexID);

    BoneTransform entityPose;
    entityPose.pos = rvi.pos;
    entityPose.scale = rvi.scale;
    entityPose.rot.value = rvi.rot;
    
    worldPos = BoneTransform::TransformPoint(entityPose, worldPos);

	worldPos = GetCameraRelativePositionWS(worldPos);
	o.pos = mul(unity_MatrixVP, float4(worldPos, 1));

    o.color = boneColor;
	return o;
}

#endif
