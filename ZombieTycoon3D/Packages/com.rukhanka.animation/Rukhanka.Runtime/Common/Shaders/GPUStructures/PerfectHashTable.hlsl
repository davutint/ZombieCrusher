#ifndef PERFECT_HASH_TABLE_HLSL_
#define PERFECT_HASH_TABLE_HLSL_

/////////////////////////////////////////////////////////////////////////////////

//  High-Order Half of 64-Bit Product
//  Ref "Hackers Delight" 8-2
uint MultiplyHighUnsigned(uint u, uint v)
{
    uint u0 = u & 0xffff;
    uint u1 = u >> 16;
    uint v0 = v & 0xffff;
    uint v1 = v >> 16;
    uint w0 = u0 * v0;
    uint t = u1 * v0 + (w0 >> 16);
    uint w1 = t & 0xFFFF;
    uint w2 = t >> 16;
    w1 = u0 * v1 + w1;
    return u1 * v1 + w2 + (w1 >> 16);
}

/////////////////////////////////////////////////////////////////////////////////

uint HashLemer(uint v, uint seed)
{
    seed = v ^ seed;
    uint tmpH = MultiplyHighUnsigned(seed, 0x4a39b70d);
    uint tmpL = seed * 0x4a39b70d;
    uint m1 = tmpH ^ tmpL;
    tmpH = MultiplyHighUnsigned(m1, 0x12fad5c9);
    tmpL = m1 * 0x12fad5c9;
    uint m2 = tmpH ^ tmpL;
    return m2;
}

/////////////////////////////////////////////////////////////////////////////////

uint GetValueIndex(uint v, uint seed, uint modMask)
{
    uint hv = HashLemer(v, seed);
    uint rv = hv & modMask;
    return rv;
}

/////////////////////////////////////////////////////////////////////////////////

int QueryPerfectHashTable(uint v, uint seed, uint phtAddress, uint sizeMask)
{
    uint index = GetValueIndex(v, seed, sizeMask);
    uint byteAddress = phtAddress + index * 4 * 2;
    CHECK_RAW_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_GPUANIMATOR_QUERY_PERFECT_HASH_TABLE_ANIMATION_CLIPS_READ0, byteAddress, 8, animationClips);
    uint2 phtv = animationClips.Load2(byteAddress);
    if (phtv.x == v)
        return (int)(phtv.y & 0xffff);
    uint nextIndex = phtv.y >> 16;
    uint byteAddress2 = phtAddress + nextIndex * 4 * 2;
    CHECK_RAW_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_GPUANIMATOR_QUERY_PERFECT_HASH_TABLE_ANIMATION_CLIPS_READ1, byteAddress2, 8, animationClips);
    uint2 phtv2 = animationClips.Load2(byteAddress2);
    if (phtv2.x == v)
        return (int)(phtv2.y & 0xffff);
    return -1;
}

#endif 
