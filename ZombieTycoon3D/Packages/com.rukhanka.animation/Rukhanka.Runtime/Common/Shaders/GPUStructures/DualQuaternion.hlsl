#ifndef DUAL_QUATERNION_HLSL_
#define DUAL_QUATERBION_HLSL_

/////////////////////////////////////////////////////////////////////////////////

#include "Quaternion.hlsl"

/////////////////////////////////////////////////////////////////////////////////

struct DualQuaternion
{
    Quaternion qReal, qDual;

/////////////////////////////////////////////////////////////////////////////////

    static DualQuaternion Identity()
    {
        DualQuaternion rv;
        rv.qReal.value = float4(0, 0, 0, 1);
        rv.qDual.value = float4(0, 0, 0, 0);
        return rv;
    }

/////////////////////////////////////////////////////////////////////////////////

    static DualQuaternion Construct(Quaternion r, Quaternion d)
    {
        DualQuaternion rv;
        rv.qReal = Quaternion::Normalize(r);
        rv.qDual = d;
        return rv;
    }

/////////////////////////////////////////////////////////////////////////////////

    static DualQuaternion Construct(float3 t, Quaternion r)
    {
        DualQuaternion rv;
        rv.qReal = Quaternion::Normalize(r);

        Quaternion qt;
        qt.value = float4(t, 0);
        rv.qDual = Quaternion::Scale(Quaternion::Multiply(qt, rv.qReal), 0.5f);

        return rv;
    }

/////////////////////////////////////////////////////////////////////////////////

    static DualQuaternion Normalize(DualQuaternion q)
    {
        DualQuaternion rv = q;
        float m = length(q.qReal.value);
        float rcpM = rcp(m);
        rv.qReal = Quaternion::Scale(q.qReal, rcpM);
        rv.qDual = Quaternion::Scale(q.qDual, rcpM);
        return rv;
    }

/////////////////////////////////////////////////////////////////////////////////

    static DualQuaternion Multiply(DualQuaternion l, DualQuaternion r)
    {
        DualQuaternion rv;
        rv.qReal = Quaternion::Multiply(l.qReal, r.qReal);
        Quaternion qDual0 = Quaternion::Multiply(l.qDual, r.qReal);
        Quaternion qDual1 = Quaternion::Multiply(l.qReal, r.qDual);
        rv.qDual.value = qDual0.value + qDual1.value;
        return rv;
    }

/////////////////////////////////////////////////////////////////////////////////

    static DualQuaternion Add(DualQuaternion l, DualQuaternion r)
    {
        DualQuaternion rv;
        rv.qReal.value = l.qReal.value + r.qReal.value;
        rv.qDual.value = l.qDual.value + r.qDual.value;
        return rv;
    }

/////////////////////////////////////////////////////////////////////////////////

    static DualQuaternion Scale(DualQuaternion q, float s)
    {
        DualQuaternion rv;
        rv.qReal = Quaternion::Scale(q.qReal, s);
        rv.qDual = Quaternion::Scale(q.qDual, s);
        return rv;
    }

/////////////////////////////////////////////////////////////////////////////////

    BoneTransform GetBoneTransform()
    {
        BoneTransform rv;
        Quaternion t0 = Quaternion::Scale(qDual, 2);
        Quaternion t1 = Quaternion::Conjugate(qReal);
        Quaternion t = Quaternion::Multiply(t0, t1);
        rv.pos = t.value.xyz;

        rv.rot = qReal;
        rv.scale = 1;
        return rv;
    }

/////////////////////////////////////////////////////////////////////////////////

    float4x4 ToTransformMatrix()
    {
        DualQuaternion q = DualQuaternion::Normalize(this);
        float4x4 m = float4x4
        (
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1
        );

        float4 r = q.qReal.value;

        //  Extract rotation
        m._11 = r.w * r.w + r.x * r.x - r.y * r.y - r.z * r.z;
        m._12 = 2 * r.x * r.y + 2 * r.w * r.z;
        m._13 = 2 * r.x * r.z - 2 * r.w * r.y;

        m._21 = 2 * r.x * r.y - 2 * r.w * r.z;
        m._22 = r.w * r.w + r.y * r.y - r.x * r.x - r.z * r.z;
        m._23 = 2 * r.y * r.z + 2 * r.w * r.x;
 
        m._31 = 2 * r.x * r.z + 2 * r.w * r.y;
        m._32 = 2 * r.y * r.z - 2 * r.w * r.x;
        m._33 = r.w * r.w + r.z * r.z - r.x * r.x - r.y * r.y;

        //  Extract translation
        Quaternion t0 = Quaternion::Scale(q.qDual, 2);
        Quaternion t = Quaternion::Multiply(t0, Quaternion::Conjugate(q.qReal));
        m._41 = t.value.x;
        m._42 = t.value.y;
        m._43 = t.value.z;

        return m;
    }
};

/////////////////////////////////////////////////////////////////////////////////

#endif


