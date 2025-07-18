using System.Runtime.CompilerServices;
using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Toolbox
{

//  This is semi-perfect hash table tuned for query performance but with reasonable build time in mind
//  True perfect hash table over known data can be made by brute forcing seed that produces hashes with all-unique
//  positions for input data elements. But such brute forcing can be very lengthy and/or produce to big hash map
//  data containers (array with many unoccupied slots). So, to reduce build time and space requirements, I've made
//  a perfect hash table with a relaxed restriction: each slot can have maximum 1 collision (2 elements). With this
//  constraint in mind table query can be very performant. It is also GPU friendly. I am suggesting very interesting
//  perfect hash optimization research: https://www.youtube.com/watch?v=DMQ_HcNSOAI
[BurstCompile]
public static class Perfect2HashTable
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int Query(uint v, uint seed, uint2* phtPtr, int phtLen)
    {
        var modMask = phtLen - 1;
        var index = GetValueIndex(v, seed, modMask);
        var phtv = phtPtr[index];
        if (Hint.Likely(phtv.x == v))
            return (int)(phtv.y & 0xffff);
        var nextIndex = (int)(phtv.y >> 16);
        var phtv2 = phtPtr[nextIndex];
        if (Hint.Likely(phtv2.x == v))
            return (int)(phtv2.y & 0xffff);
        return -1;
    }
    
/////////////////////////////////////////////////////////////////////////////////

    [BurstCompile]
    public static bool Build(in NativeArray<uint> inData, out NativeArray<uint2> outPht, out uint outSeed)
    {
        var numTries = 0xffff;
        var maxPHTSize = 0xffff;
        var phtSize = math.ceilpow2(inData.Length);
        BurstAssert.IsTrue(phtSize <= maxPHTSize, $"Maximum table size is {maxPHTSize}. Requested size is {phtSize}");
        
        var rng = new Random(0x811C9DC5);
        while (phtSize <= maxPHTSize)
        {
            var phtSlotUsage = new NativeArray<int>(phtSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var seed = rng.NextUInt();
            var i = 0;
            for (; i < numTries; ++i)
            {
                if (BuildIteration(inData, ref phtSlotUsage, seed))
                    break;
                seed = rng.NextUInt();
            }
            
            if (i < numTries)
            {
                outPht = MakePHTWithSeed(inData, seed, phtSlotUsage);
                outSeed = seed;
                return true;
            }
            phtSize <<= 1;    
        }
        outPht = default;
        outSeed = 0;
        return false;
    }
    
/////////////////////////////////////////////////////////////////////////////////

    static NativeArray<uint2> MakePHTWithSeed(in NativeArray<uint> inData, uint seed, NativeArray<int> phtSlotUsage)
    {
        BurstAssert.IsTrue(math.ispow2(phtSlotUsage.Length), "Hash table size must be power of 2");
        var modMask = phtSlotUsage.Length - 1;
        var rv = new NativeArray<uint2>(phtSlotUsage.Length, Allocator.Temp);
        for (var i = 0; i < inData.Length; ++i)
        {
            var v = inData[i];
            var hv = Hash_Lemer(v, seed);
            var index = (int)(hv & modMask);
            var slotUsageCount = phtSlotUsage[index];
            BurstAssert.IsTrue(slotUsageCount == 1 || slotUsageCount == 2, "Slot usage should be one or two");
            //  If slot already occupied, move value to next free slot
            if (rv[index].x != 0)
            {
                for (int k = 0; k < rv.Length; ++k)
                {
                    var nextIdx = (k + i) & modMask;
                    if (phtSlotUsage[nextIdx] == 0)
                    {
                        rv[nextIdx] = new uint2(v, (uint)i);
                        
                        //  Make a pointer to this slot from base slot
                        var iv = rv[index];
                        iv.y |= (uint)nextIdx << 16;
                        rv[index] = iv;
                        
                        //  Mark slot as occupied
                        phtSlotUsage[nextIdx] = 1;
                        break;
                    }
                }
            }
            else
            {
                rv[index] = new uint2(v, (uint)i);
            }
        }
        return rv;
    }

/////////////////////////////////////////////////////////////////////////////////

	static uint FNV1aBody(uint v, uint hash)
	{
		return (v ^ hash) * 0x01000193;
	}

////////////////////////////////////////////////////////////////////////////////////

	public static uint Hash_FNV1a(uint v, uint seed)
	{
		uint4 vb = new uint4(v, v >> 8, v >> 16, v >> 24);
		seed = FNV1aBody(vb.x, seed);
		seed = FNV1aBody(vb.y, seed);
		seed = FNV1aBody(vb.z, seed);
		seed = FNV1aBody(vb.w, seed);
		return seed;
	}

/////////////////////////////////////////////////////////////////////////////////
 
    static uint Hash_CRC(uint v, uint seed)
    {
        const uint poly = 0x82f63b78;
        for (var i = 0; i < 4; ++i)
        {
            seed ^= v >> (i * 8);
            for (int k = 0; k < 8; k++)
                seed = (seed & 1) != 0 ? (seed >> 1) ^ poly : seed >> 1;
        }
        return ~seed;
    }

/////////////////////////////////////////////////////////////////////////////////

    //  High-Order Half of 64-Bit Product
    //  Ref "Hackers Delight" 8-2
    static uint MultiplyHighUnsigned(uint u, uint v)
    {
        uint u0 = u & 0xffff;
        uint u1 = u >> 16;
        uint v0 = v & 0xffff;
        uint v1 = v >> 16;
        uint w0 = u0 * v0;
        uint t = u1*v0 + (w0 >> 16);
        uint w1 = t & 0xFFFF;
        uint w2 = t >> 16;
        w1 = u0 * v1 + w1;
        return u1 * v1 + w2 + (w1 >> 16);
    }
    
/////////////////////////////////////////////////////////////////////////////////

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static uint Hash_Lemer_32bitAriphmetic(uint v, uint seed)
    {
        seed = v ^ seed;
        uint tmpH = MultiplyHighUnsigned(seed, 0x4a39b70d);
        uint tmpL = seed * 0x4a39b70d;
        uint m1 = tmpH ^ tmpL;
        tmpH = MultiplyHighUnsigned(m1, 0x12fad5c9);
        tmpL = m1 * 0x12fad5c9;
        var m2 = tmpH ^ tmpL;
        return m2;
    }
    
/////////////////////////////////////////////////////////////////////////////////

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static uint Hash_Lemer(uint v, uint seed)
    {
        seed = v ^ seed;
        ulong tmp = seed * 0x4a39b70dul;
        var m1 = (uint)((tmp >> 32) ^ tmp);
        tmp = m1 * 0x12fad5c9ul;
        var m2 = (uint)((tmp >> 32) ^ tmp);
        return m2;
    }

/////////////////////////////////////////////////////////////////////////////////

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int GetValueIndex(uint v, uint seed, int modMask)
    {
        var hv = Hash_Lemer(v, seed);
        var rv = (int)(hv & modMask);
        return rv;
    }

/////////////////////////////////////////////////////////////////////////////////

    static unsafe bool BuildIteration(in NativeArray<uint> inData, ref NativeArray<int> phtSlotUsage, uint hashSeed)
    {
        UnsafeUtility.MemClear(phtSlotUsage.GetUnsafePtr(), phtSlotUsage.Length * sizeof(int));
        
        var modMask = phtSlotUsage.Length - 1;
        var maxCollisions = 0;
        
        for (int i = 0; i < inData.Length; ++i)
        {
            var v = inData[i];
            var index = GetValueIndex(v, hashSeed, modMask);
            phtSlotUsage[index] += 1;
            maxCollisions = math.max(phtSlotUsage[index], maxCollisions);
        }
        return maxCollisions <= 2;
    }
}
}
