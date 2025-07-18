#ifndef MAKE_WORLD_SPACE_BONE_TRANSFORMS_HLSL_
#define MAKE_WORLD_SPACE_BONE_TRANSFORMS_HLSL_

/////////////////////////////////////////////////////////////////////////////////

RWStructuredBuffer<BoneTransform> outBoneTransforms;
StructuredBuffer<BoneTransform> boneLocalTransforms;

/////////////////////////////////////////////////////////////////////////////////

[numthreads(256, 1, 1)]
void MakeRigSpaceBoneTransforms(uint tid: SV_DispatchThreadID)
{
    if (tid >= (uint)animatedBonesCount)
        return;

    CHECK_STRUCTURED_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_GPUANIMATOR_MAKE_RIG_SPACE_BONE_TRANSFORMS_ANIMATED_BONE_WORKLOAD_READ, tid, animatedBoneWorkload);
    AnimatedBoneWorkload boneWorkload = animatedBoneWorkload[tid];

    CHECK_STRUCTURED_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_GPUANIMATOR_MAKE_RIG_SPACE_BONE_TRANSFORMS_ANIMATION_JOBS_READ, boneWorkload.animationJobIndex, animationJobs);
    AnimationJob animationJob = animationJobs[boneWorkload.animationJobIndex];
    RigDefinition rigDef = RigDefinition::ReadFromRawBuffer(rigDefinitions, animationJob.rigDefinitionIndex);
    RigBone rigBone = RigBone::ReadFromRawBuffer(rigBones, rigDef.rigBonesRange.x + boneWorkload.boneIndexInRig);

    int absoluteBoneIndex = animationJob.animatedBoneIndexOffset + boneWorkload.boneIndexInRig;
    CHECK_STRUCTURED_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_GPUANIMATOR_MAKE_RIG_SPACE_BONE_TRANSFORMS_BONE_LOCAL_TRANSFORMS_READ0, absoluteBoneIndex, boneLocalTransforms);
    BoneTransform bt = boneLocalTransforms[absoluteBoneIndex];
    int parentBoneIndex = rigBone.parentBoneIndex;
    while (parentBoneIndex > 0)
    {
        RigBone parentBoneData = RigBone::ReadFromRawBuffer(rigBones, rigDef.rigBonesRange.x + parentBoneIndex);
        int absoluteParentBoneIndex = animationJob.animatedBoneIndexOffset + parentBoneIndex;
        CHECK_STRUCTURED_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_GPUANIMATOR_MAKE_RIG_SPACE_BONE_TRANSFORMS_BONE_LOCAL_TRANSFORMS_READ1, absoluteParentBoneIndex, boneLocalTransforms);
        BoneTransform parentBoneTransform = boneLocalTransforms[absoluteParentBoneIndex];
        bt = BoneTransform::Multiply(parentBoneTransform, bt);
        parentBoneIndex = parentBoneData.parentBoneIndex;
    }
    CHECK_STRUCTURED_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_GPUANIMATOR_MAKE_RIG_SPACE_BONE_TRANSFORMS_OUT_BONE_TRANSFORMS_WRITE, absoluteBoneIndex, outBoneTransforms);
    outBoneTransforms[absoluteBoneIndex] = bt;
}

#endif // MAKE_WORLD_SPACE_BONE_TRANSFORMS_HLSL_
