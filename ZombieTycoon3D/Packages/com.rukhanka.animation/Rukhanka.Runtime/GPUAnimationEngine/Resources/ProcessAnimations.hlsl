#ifndef PROCESS_ANIMATIONS_HLSL_
#define PROCESS_ANIMATIONS_HLSL_

#include "Packages/com.rukhanka.animation/Rukhanka.Runtime/GPUAnimationEngine/Resources/TrackGroupSampler.hlsl"

/////////////////////////////////////////////////////////////////////////////////

#define NUM_MAXIMUM_LAYER_WEIGHTS 16
RWStructuredBuffer<BoneTransform> outAnimatedBones;
    
/////////////////////////////////////////////////////////////////////////////////

float2 NormalizeAnimationTime(float at, AnimationClip ac)
{
    at += ac.cycleOffset;
    float normalizedTime = ac.IsLooped() ? frac(at) : saturate(at);
    float timeInSeconds = normalizedTime * ac.length;
    float2 rv = float2(timeInSeconds, normalizedTime);
    return rv;
}

/////////////////////////////////////////////////////////////////////////////////

BoneTransform MakeAdditiveAnimation(BoneTransform bonePose, BoneTransform zeroFramePose)
{
    BoneTransform rv;
    rv.pos = bonePose.pos - zeroFramePose.pos;
    Quaternion conjugateZFRot = Quaternion::NormalizeSafe(Quaternion::Conjugate(zeroFramePose.rot));
    conjugateZFRot = Quaternion::ShortestRotation(bonePose.rot, conjugateZFRot);
    rv.rot = Quaternion::Multiply(Quaternion::Normalize(bonePose.rot), conjugateZFRot);
    rv.scale = bonePose.scale / zeroFramePose.scale;
    return rv;
}

/////////////////////////////////////////////////////////////////////////////////

BoneTransform CalculateLoopPose(BoneTransform bonePose, TrackSet ts, HumanRotationData hrd, float normalizedTime)
{
    float lerpFactor = normalizedTime;

    FirstFrameTrackSampler ffSampler;
    BoneTransformAndFlags rootPoseStart = SampleTrackGroup(ts, ffSampler, hrd);
    LastFrameTrackSampler lfSampler;
    BoneTransformAndFlags rootPoseEnd = SampleTrackGroup(ts, lfSampler, hrd);

    float3 dPos = rootPoseEnd.bt.pos - rootPoseStart.bt.pos;
    Quaternion dRot = Quaternion::Multiply(Quaternion::Conjugate(rootPoseEnd.bt.rot), rootPoseStart.bt.rot);

    BoneTransform rv;
    rv.pos = bonePose.pos - dPos * lerpFactor;
    rv.rot = Quaternion::Multiply(bonePose.rot, Quaternion::Slerp(Quaternion::Identity(), dRot, lerpFactor));
    return rv;
}

/////////////////////////////////////////////////////////////////////////////////

bool SampleAnimation(AnimationClip ac, uint baseAddress, float2 animTime, int rigBoneIndex, uint rigBoneHash, int blendMode, HumanRotationData hrd, out BoneTransformAndFlags btf)
{
    btf = BoneTransformAndFlags::Identity();

    TrackSet tsClip = ac.clipTracks;
    tsClip.OffsetByAddress(baseAddress);
    
    int trackGroupIndex = tsClip.GetTrackGroupIndex(rigBoneHash);
    if (trackGroupIndex < 0)
        return false;

    tsClip.trackGroupsOffset += trackGroupIndex * 4;

    float timeInSeconds = animTime.x;
    DefaultTrackSampler tSampler;
    tSampler.time = timeInSeconds;
    btf = SampleTrackGroup(tsClip, tSampler, hrd);

    if (blendMode == BLEND_MODE_ADDITIVE)
    {   
        TrackSet additiveTrackSet = ac.clipTracks;
        if (ac.additiveReferencePoseTracks.keyFramesOffset >= 0)
            additiveTrackSet = ac.additiveReferencePoseTracks;

        additiveTrackSet.OffsetByAddress(baseAddress);

        int additiveTrackGroupIndex = QueryPerfectHashTable(rigBoneHash, additiveTrackSet.trackGroupPHTSeed, additiveTrackSet.trackGroupPHTOffset, additiveTrackSet.trackGroupPHTSizeMask);
        if (additiveTrackGroupIndex >= 0)
        {
            FirstFrameTrackSampler ffSampler;
            additiveTrackSet.trackGroupsOffset += additiveTrackGroupIndex * 4;
            BoneTransformAndFlags additiveFramePose = SampleTrackGroup(additiveTrackSet, ffSampler, hrd);
            btf.bt = MakeAdditiveAnimation(btf.bt, additiveFramePose.bt);
        }
    }

    bool calculateLoopPose = ac.LoopPoseBlend() & rigBoneIndex != 0;
    if (calculateLoopPose)
    {
        btf.bt = CalculateLoopPose(btf.bt, tsClip, hrd, animTime.y);
    }

    return true;
}

/////////////////////////////////////////////////////////////////////////////////

BoneTransform MixPoses(BoneTransform curPose, BoneTransform inPose, float3 weight, int blendMode)
{
    //  Override
    if (blendMode == BLEND_MODE_OVERRIDE)
    {
        inPose.rot = Quaternion::ShortestRotation(curPose.rot, inPose.rot);
        BoneTransform scaledPose = BoneTransform::Scale(inPose, weight);

        curPose.pos += scaledPose.pos;
        curPose.rot.value += scaledPose.rot.value;
        curPose.scale += scaledPose.scale;
    }
    //  Additive
    else
    {
        curPose.pos += inPose.pos * weight.x;
        Quaternion layerRot;
        layerRot.value = float4(inPose.rot.value.xyz * weight.y, inPose.rot.value.w);
        layerRot = Quaternion::NormalizeSafe(layerRot);
        layerRot = Quaternion::ShortestRotation(curPose.rot, layerRot);
        curPose.rot = Quaternion::Multiply(layerRot, curPose.rot);
        curPose.scale *= (1 - weight.z) + (inPose.scale * weight.z);
    }
    return curPose;
}

/////////////////////////////////////////////////////////////////////////////////

BoneTransform BoneTransformMakePretty(BoneTransform animatedBonePose, BoneTransform refBonePose, float3 weights)
{
    float3 complWeights = saturate(float3(1, 1, 1) - weights);
    animatedBonePose.pos += refBonePose.pos * complWeights.x;
    Quaternion shortestRefRot = Quaternion::ShortestRotation(animatedBonePose.rot, refBonePose.rot);
    animatedBonePose.rot.value += shortestRefRot.value * complWeights.y;
    animatedBonePose.scale += refBonePose.scale * complWeights.z;

    animatedBonePose.rot = Quaternion::Normalize(animatedBonePose.rot);
    return animatedBonePose;
}

/////////////////////////////////////////////////////////////////////////////////

float CalculateFinalLayerWeights(out float layerWeights[NUM_MAXIMUM_LAYER_WEIGHTS], AnimationJob aj, int humanBodyPart, uint boneHash, int boneIndex)
{
    int layerIndex = -1;
    float w = 1.0f;

    for (int i = aj.animationsToProcessRange.y - 1; i >= 0; --i)
    {
        int atpIndex = aj.animationsToProcessRange.x + i;
        AnimationToProcess a = animationsToProcess[atpIndex];
        if (a.layerIndex == layerIndex) continue;

        AnimationClip ac = AnimationClip::ReadFromRawBuffer(animationClips, a.animationClipAddress);
        TrackSet tsClip = ac.clipTracks;
        tsClip.OffsetByAddress(a.animationClipAddress);
        
        bool inAvatarMask = IsBoneInAvatarMask(a.avatarMaskDataOffset, humanBodyPart, boneIndex);
		int hasTrack = tsClip.GetTrackGroupIndex(boneHash) >= 0;
        float layerWeight = inAvatarMask && hasTrack ? a.layerWeight : 0;

        float lw = w * layerWeight;
        layerWeights[a.layerIndex] = lw;
        if (a.blendMode == BLEND_MODE_OVERRIDE)
            w -= lw;
        layerIndex = a.layerIndex;
    }
    AnimationToProcess atp0 = animationsToProcess[aj.animationsToProcessRange.x];
    return atp0.blendMode == BLEND_MODE_OVERRIDE ? 0 : layerWeights[0];
}

/////////////////////////////////////////////////////////////////////////////////

[numthreads(128, 1, 1)]
void ProcessAnimations(uint tid: SV_DispatchThreadID)
{
    if (tid >= (uint)animatedBonesCount)
        return;

    CHECK_STRUCTURED_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_GPUANIMATOR_PROCESS_ANIMATIONS_ANIMATED_BONE_WORKLOAD_READ, tid, animatedBoneWorkload);
    AnimatedBoneWorkload boneWorkload = animatedBoneWorkload[tid];

    CHECK_STRUCTURED_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_GPUANIMATOR_PROCESS_ANIMATIONS_ANIMATION_JOBS_READ, boneWorkload.animationJobIndex, animationJobs);
    AnimationJob animationJob = animationJobs[boneWorkload.animationJobIndex];

    RigDefinition rigDef = RigDefinition::ReadFromRawBuffer(rigDefinitions, animationJob.rigDefinitionIndex);
    RigBone rigBone = RigBone::ReadFromRawBuffer(rigBones, rigDef.rigBonesRange.x + boneWorkload.boneIndexInRig);

    float layerWeights[NUM_MAXIMUM_LAYER_WEIGHTS];
    float refPoseWeight = CalculateFinalLayerWeights(layerWeights, animationJob, rigBone.humanBodyPart, rigBone.hash, boneWorkload.boneIndexInRig);
    float3 totalWeights = refPoseWeight;

	BoneTransform blendedBonePose = BoneTransform::Scale(rigBone.refPose, refPoseWeight);

    HumanRotationData hrd = (HumanRotationData)0;
    if (rigDef.humanRotationDataRange.x >= 0)
        hrd = HumanRotationData::ReadFromRawBuffer(humanRotationDataBuffer, rigDef.humanRotationDataRange.x + boneWorkload.boneIndexInRig);

    int atpIndexStart = animationJob.animationsToProcessRange.x;
    int atpIndexEnd = animationJob.animationsToProcessRange.x + animationJob.animationsToProcessRange.y;
    for (int i = atpIndexStart; i < atpIndexEnd; ++i)
    {
        AnimationToProcess atp = animationsToProcess[i];
        if (atp.animationClipAddress < 0)
            continue;

        int baseAddress = atp.animationClipAddress;
        AnimationClip ac = AnimationClip::ReadFromRawBuffer(animationClips, baseAddress);
        float2 animTime = NormalizeAnimationTime(atp.time, ac);

        BoneTransformAndFlags btf;
        if (SampleAnimation(ac, baseAddress, animTime, boneWorkload.boneIndexInRig, rigBone.hash, atp.blendMode, hrd, btf))
        {
            float3 weight = btf.flags * atp.weight * layerWeights[atp.layerIndex];
            if (atp.blendMode == BLEND_MODE_OVERRIDE)
                totalWeights += weight;
            blendedBonePose = MixPoses(blendedBonePose, btf.bt, weight, atp.blendMode);
        }
    }

    blendedBonePose = BoneTransformMakePretty(blendedBonePose, rigBone.refPose, totalWeights);
    int outIndex = animationJob.animatedBoneIndexOffset + boneWorkload.boneIndexInRig;

    CHECK_STRUCTURED_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_GPUANIMATOR_PROCESS_ANIMATIONS_OUT_ANIMATED_BONES_WRITE, outIndex, outAnimatedBones);
    outAnimatedBones[outIndex] = blendedBonePose;
}

#endif // PROCESS_ANIMATIONS_HLSL_
