#ifndef TRACK_GROUP_SAMPLER_HLSL_
#define TRACK_GROUP_SAMPLER_HLSL_

/////////////////////////////////////////////////////////////////////////////////

struct BoneTransformAndFlags
{
    BoneTransform bt;
    float3 flags;

    static BoneTransformAndFlags Identity()
    {
        BoneTransformAndFlags rv;
        rv.bt = BoneTransform::Identity();
        rv.flags = 0;
        return rv;
    }
};

/////////////////////////////////////////////////////////////////////////////////

interface ITrackSampler
{
    float Sample(Track tk, int keyFrameBaseAddress);
};

/////////////////////////////////////////////////////////////////////////////////

Quaternion ApplyHumanoidPostTransform(HumanRotationData hrd, Quaternion q)
{
    Quaternion rv = Quaternion::Multiply(Quaternion::Multiply(hrd.preRot, q), hrd.postRot);
    return rv;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

float3 MuscleRangeToRadians(float3 minA, float3 maxA, float3 muscle)
{
    float3 negativeRange = min(muscle, 0);
    float3 positiveRange = max(0, muscle);
    float3 negativeRot = lerp(0, minA, -negativeRange);
    float3 positiveRot = lerp(0, maxA, +positiveRange);

    float3 rv = negativeRot + positiveRot;
    return rv;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

Quaternion MuscleValuesToQuaternion(HumanRotationData humanBoneInfo, float3 muscleValues)
{
    float3 r = MuscleRangeToRadians(humanBoneInfo.minMuscleAngles, humanBoneInfo.maxMuscleAngles, muscleValues);
    r *= humanBoneInfo.sign;

    float3 rightVec = float3(1, 0, 0);
    float3 upVec = float3(0, 1, 0);
    float3 forwardVec = float3(0, 0, 1);

    Quaternion qx = Quaternion::AxisAngle(rightVec, r.x);
    Quaternion qy = Quaternion::AxisAngle(upVec, r.y);
    Quaternion qz = Quaternion::AxisAngle(forwardVec, r.z);
    Quaternion qzy = Quaternion::Multiply(qz, qy);
    qzy.value.x = 0;
    Quaternion rv = Quaternion::Multiply(Quaternion::Normalize(qzy), qx);

    rv = ApplyHumanoidPostTransform(humanBoneInfo, rv);
    return rv;
}

/////////////////////////////////////////////////////////////////////////////////

BoneTransformAndFlags SampleTrackGroup(TrackSet ts, ITrackSampler trackSampler, HumanRotationData hrd)
{
    int trackStartIndex = animationClips.Load(ts.trackGroupsOffset);
    int trackEndIndex = animationClips.Load(ts.trackGroupsOffset + 4);

    float pos[3] = {0, 0, 0};
    float rot[4] = {0, 0, 0, 1};
    float scale[3] = {1, 1, 1};
    float3 flags = 0;
    bool eulerToQuaternion = false;
    bool isHumanMuscle = false;

    for (int i = trackStartIndex; i < trackEndIndex; ++i)
    {
        Track tk = Track::ReadFromRawBuffer(animationClips, ts.tracksOffset, i);
        int channelIndex = tk.GetChannelIndex();
        float interpolatedCurveValue = trackSampler.Sample(tk, ts.keyFramesOffset);

        switch (tk.GetBindingType())
        {
        case BINDING_TYPE_TRANSLATION:
            pos[channelIndex] = interpolatedCurveValue;
            flags.x = 1;
            break;
        case BINDING_TYPE_QUATERNION:
            rot[channelIndex] = interpolatedCurveValue;
            flags.y = 1;
            break;
        case BINDING_TYPE_SCALE:
            scale[channelIndex] = interpolatedCurveValue;
            flags.z = 1;
            break;
        case BINDING_TYPE_EULER_ANGLES:
            eulerToQuaternion = true;
            rot[channelIndex] = interpolatedCurveValue;
            flags.y = 1;
            break;
        case BINDING_TYPE_HUMAN_MUSCLE:
            rot[channelIndex] = interpolatedCurveValue;
            isHumanMuscle = true;
            flags.y = 1;
            break;
        }
    }

    if (eulerToQuaternion)
    {
        float3 eulerAnglesInDegrees = float3(rot[0], rot[1], rot[2]);
        Quaternion q = Quaternion::EulerXYZ(eulerAnglesInDegrees * (1 / 180.0f * 3.1415926f));
        rot[0] = q.value.x;
        rot[1] = q.value.y;
        rot[2] = q.value.z;
    }

    if (isHumanMuscle)
    {
        float3 muscleValues = float3(rot[0], rot[1], rot[2]);
        Quaternion q = MuscleValuesToQuaternion(hrd, muscleValues);
        rot[0] = q.value.x;
        rot[1] = q.value.y;
        rot[2] = q.value.z;
        rot[3] = q.value.w;
    }

    BoneTransform bt = BoneTransform::Identity();
    bt.pos = float3(pos[0], pos[1], pos[2]);
    bt.rot.value = float4(rot[0], rot[1], rot[2], rot[3]);
    bt.scale = float3(scale[0], scale[1], scale[2]);

    BoneTransformAndFlags rv;
    rv.bt = bt;
    rv.flags = flags;

    return rv;
};

//-----------------------------------------------------------------------------------------//

struct DefaultTrackSampler: ITrackSampler
{
    float time;

    float Sample(Track tk, int keyFrameBaseAddress)
    {
        return tk.SampleByBinarySearch(time, keyFrameBaseAddress);
    }
};

//-----------------------------------------------------------------------------------------//

struct FirstFrameTrackSampler: ITrackSampler
{
    float Sample(Track tk, int keyFrameBaseAddress)
    {
        return tk.GetFirstFrameValue(keyFrameBaseAddress);
    }
};

//-----------------------------------------------------------------------------------------//

struct LastFrameTrackSampler: ITrackSampler
{
    float Sample(Track tk, int keyFrameBaseAddress)
    {
        return tk.GetLastFrameValue(keyFrameBaseAddress);
    }
};

#endif
