#ifndef QUATERNION_HLSL_
#define QUATERNION_HLSL_

/////////////////////////////////////////////////////////////////////////////////

struct Quaternion
{
    float4 value;

/////////////////////////////////////////////////////////////////////////////////

    static Quaternion Identity()
    {
        Quaternion rv;
        rv.value = float4(0, 0, 0, 1);
        return rv;
    }

/////////////////////////////////////////////////////////////////////////////////

    static float3 Rotate(Quaternion q, float3 v)
    {
        float3 t = 2 * cross(q.value.xyz, v);
        float3 rv = v + q.value.w * t + cross(q.value.xyz, t);
        return rv;
    }

/////////////////////////////////////////////////////////////////////////////////

    static Quaternion Multiply(Quaternion a, Quaternion b)
    {
        Quaternion rv;
        rv.value = float4(a.value.wwww * b.value + (a.value.xyzx * b.value.wwwx + a.value.yzxy * b.value.zxyy) * float4(1.0f, 1.0f, 1.0f, -1.0f) - a.value.zxyz * b.value.yzxz);
        return rv;
    }

/////////////////////////////////////////////////////////////////////////////////

    static Quaternion ShortestRotation(Quaternion from, Quaternion to)
    {
        Quaternion rv;
		uint4 sign = asuint(dot(from.value, to.value)) & 0x80000000;
		rv.value = asfloat(sign ^ asuint(to.value));
		return rv;
    }

/////////////////////////////////////////////////////////////////////////////////

    static Quaternion AxisAngle(float3 axis, float angle)
    {
        float sina, cosa;
        sincos(0.5f * angle, sina, cosa);
        Quaternion rv;
        rv.value = float4(axis * sina, cosa);
        return rv;
    }

/////////////////////////////////////////////////////////////////////////////////

    static Quaternion Normalize(Quaternion q)
    {
        float4 x = q.value;
        Quaternion rv;
        rv.value = rsqrt(dot(x, x)) * x;
        return rv;
    }

/////////////////////////////////////////////////////////////////////////////////

    static Quaternion Inverse(Quaternion q)
    {
        float4 a = q.value;
        Quaternion rv;
        rv.value = rcp(dot(a, a)) * a * float4(-1.0f, -1.0f, -1.0f, 1.0f);
        return rv;
    }

/////////////////////////////////////////////////////////////////////////////////

    static Quaternion Conjugate(Quaternion q)
    {
        Quaternion rv;
        rv.value = q.value * float4(-1, -1, -1, 1);
        return rv;
    }

/////////////////////////////////////////////////////////////////////////////////

    static Quaternion NormalizeSafe(Quaternion q)
    {
        float FLT_MIN_NORMAL = 1.175494351e-38F;
        float len = dot(q.value, q.value);
        Quaternion rv;
        rv.value = float4(0, 0, 0, 1);

        if (len > FLT_MIN_NORMAL)
        {
            rv.value = q.value * rsqrt(len);
        }
        return rv;
    }

/////////////////////////////////////////////////////////////////////////////////

    static Quaternion FromMatrix(float3x3 m)
    {
        Quaternion rv;
        
        float3 u = m._m00_m10_m20;
        float3 v = m._m01_m11_m21;
        float3 w = m._m02_m12_m22;

        uint u_sign = asuint(u.x) & 0x80000000;
        float t = v.y + asfloat(asuint(w.z) ^ u_sign);
        uint1 u_mask1 = (int)u_sign >> 31;
        uint1 t_mask1 = asint(t) >> 31;
        uint4 u_mask = u_mask1.xxxx;
        uint4 t_mask = t_mask1.xxxx;
        uint4 u_mask_inv = uint4(~u_mask1.x, ~u_mask1.x, ~u_mask1.x, ~u_mask1.x);
        uint4 t_mask_inv = uint4(~t_mask1.x, ~t_mask1.x, ~t_mask1.x, ~t_mask1.x);

        float tr = 1.0f + abs(u.x);

        uint4 sign_flips = uint4(0x00000000, 0x80000000, 0x80000000, 0x80000000) ^ (u_mask & uint4(0x00000000, 0x80000000, 0x00000000, 0x80000000)) ^ (t_mask & uint4(0x80000000, 0x80000000, 0x80000000, 0x00000000));

        rv.value = float4(tr, u.y, w.x, v.z) + asfloat(asuint(float4(t, v.x, u.z, w.y)) ^ sign_flips);   // +---, +++-, ++-+, +-++

        rv.value = asfloat((asuint(rv.value) & u_mask_inv) | (asuint(rv.value.zwxy) & u_mask));
        rv.value = asfloat((asuint(rv.value.wzyx) & t_mask_inv) | (asuint(rv.value) & t_mask));
        rv.value = normalize(rv.value);
        return rv;
    }
    
/////////////////////////////////////////////////////////////////////////////////

    float3x3 ToRotationMatrix()
    {
        float4 value2 = value * 2;
        uint3 npn = uint3(0x80000000, 0x00000000, 0x80000000);
        uint3 nnp = uint3(0x80000000, 0x80000000, 0x00000000);
        uint3 pnn = uint3(0x00000000, 0x80000000, 0x80000000);
        float3 c0 = value2.y * asfloat(asuint(value.yxw) ^ npn) - value2.z * asfloat(asuint(value.zwx) ^ pnn) + float3(1, 0, 0);
        float3 c1 = value2.z * asfloat(asuint(value.wzy) ^ nnp) - value2.x * asfloat(asuint(value.yxw) ^ npn) + float3(0, 1, 0);
        float3 c2 = value2.x * asfloat(asuint(value.zwx) ^ pnn) - value2.y * asfloat(asuint(value.wzy) ^ nnp) + float3(0, 0, 1);

        float3x3 rv = float3x3(c0, c1, c2);
        return rv;
    }

/////////////////////////////////////////////////////////////////////////////////

    static Quaternion EulerXYZ(float3 eulerAngles)
    {
        float3 sinV, cosV;
        sincos(0.5f * eulerAngles, sinV, cosV);
        Quaternion rv;
        rv.value = float4(sinV.xyz, cosV.x) * cosV.yxxy * cosV.zzyz + sinV.yxxy * sinV.zzyz * float4(cosV.xyz, sinV.x) * float4(-1.0f, 1.0f, -1.0f, 1.0f);
        return rv;
    }

/////////////////////////////////////////////////////////////////////////////////

    static float4 ChageSign(float4 a, float4 b)
    {
         return asfloat(asuint(a) ^ asuint(b) & 0x80000000);
    }

/////////////////////////////////////////////////////////////////////////////////

    static Quaternion Scale(Quaternion q, float s)
    {
        Quaternion rv;
        rv.value = q.value * s;
        return rv;
    }

/////////////////////////////////////////////////////////////////////////////////

    static Quaternion Nlerp(Quaternion a, Quaternion b, float t)
    {
        Quaternion rv;
        rv.value = normalize(a.value + t * (ChageSign(b.value, dot(a.value, b.value)) - a.value));
        return rv;
    }

/////////////////////////////////////////////////////////////////////////////////

    static Quaternion Slerp(Quaternion a, Quaternion b, float t)
    {
        float dt = dot(a.value, b.value);
        if (dt < 0)
        {
            dt = -dt;
            b.value = -b.value;
        }

        if (dt < 0.9995f)
        {
            float angle = acos(dt);
            float s = rsqrt(1 - dt * dt);
            float w1 = sin(angle * (1 - t)) * s;
            float w2 = sin(angle * t) * s;
            Quaternion rv;
            rv.value = a.value * w1 + b.value * w2;
            return rv;
        }

        return Nlerp(a, b, t);
    }
};

/////////////////////////////////////////////////////////////////////////////////

#endif


