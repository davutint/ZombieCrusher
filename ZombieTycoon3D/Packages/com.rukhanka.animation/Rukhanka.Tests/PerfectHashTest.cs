using System.Runtime.CompilerServices;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using Rukhanka.Toolbox;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.PerformanceTesting;
using UnityEngine;
using Random = Unity.Mathematics.Random;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Tests
{
/////////////////////////////////////////////////////////////////////////////////

[BurstCompile]
public class PerfectHashTest
{
	[Test]
	public void PerfectHashValidationTest()
	{
		var numTests = 100;
		var seed = 123124u;
		var rng = new Random(seed);

		for (var i = 0; i < numTests; ++i)
		{
			InternalHashFuncTest(rng.NextUInt(), math.abs(rng.NextInt() % 1000));
		}
	}
	
/////////////////////////////////////////////////////////////////////////////////

    unsafe void InternalHashFuncTest(uint rngSeed, int numHashedValues)
    {
	    var rng = new Random(rngSeed);

		var hashArr = new NativeList<uint>(Allocator.Temp);
		for (int i = 0; i < numHashedValues; ++i)
		{
			hashArr.Add(rng.NextUInt());
		};
		Perfect2HashTable.Build(hashArr.AsArray(), out var pht, out var seed);

		for (int i = 0; i < hashArr.Length; ++i)
		{
			var iHash = hashArr[i];
			var l = Perfect2HashTable.Query(iHash, seed, (uint2*)pht.GetUnsafePtr(), pht.Length);
			Assert.IsTrue(l == i);
		}
    }
    
/////////////////////////////////////////////////////////////////////////////////
    
	[MethodImpl(MethodImplOptions.NoInlining)]
	static void DummyUse(int s)
	{
		s += 1;
	}
    
/////////////////////////////////////////////////////////////////////////////////

	[BurstCompile]
	static void HashSetQueryFunc(in NativeHashSet<uint> inSet, in NativeArray<uint> srcValues, int numIterations)
	{
		var sum = 0;
		for (int i = 0; i < numIterations; ++i)
		{
			var l = i * 12 % srcValues.Length;
			var k = inSet.Contains(srcValues[l]);
			sum += k ? 1 : 0;
			//BurstAssert.IsTrue(l == shuffleArr[k], "Does not match");
		}
		//	Need to use sum somehow to keep query function body from stripping
		DummyUse(sum);
	}
	
/////////////////////////////////////////////////////////////////////////////////

	[BurstCompile]
	static unsafe void PHT2QueryFunc(in NativeArray<uint2> pht, in NativeArray<uint> srcValues, uint seed, int numIterations)
	{
		var sum = 0;
		for (int i = 0; i < numIterations; ++i)
		{
			var l = i * 12 % srcValues.Length;
			var k = Perfect2HashTable.Query(srcValues[l], seed, (uint2*)pht.GetUnsafeReadOnlyPtr(), pht.Length);
			sum += (k >= 0) ? 1 : 0;
			//BurstAssert.IsTrue(k >= 0, "Does not match");
		}
		//	Need to use sum somehow to keep query function body from stripping
		DummyUse(sum);
	}
	
/////////////////////////////////////////////////////////////////////////////////

	void NativeHashMapPerformanceSingleTest(NativeHashSet<uint> inSet, NativeArray<uint> srcValues, int numIterations)
	{
		Measure.Method(() =>
		{
			HashSetQueryFunc(inSet, srcValues, numIterations);
		})
		.MeasurementCount(10)
		.SampleGroup($"Native hash set query. Table size: {inSet.Count}, Queries count: {numIterations}")
		.Run();
	}
	
/////////////////////////////////////////////////////////////////////////////////

	void PHT2_SingleTest(NativeArray<uint2> pht, NativeArray<uint> srcValues, uint seed, int numIterations)
	{
		Measure.Method(() =>
		{
			PHT2QueryFunc(pht, srcValues, seed, numIterations);
		})
		.MeasurementCount(10)
		.SampleGroup($"PHT2 query. Table size: {pht.Length}, Queries count: {numIterations}")
		.Run();
	}


/////////////////////////////////////////////////////////////////////////////////

	[Test, Performance]
	public void PerfectHashTableQueryPerformance()
	{
		var iterationsCount = 1000000;
	    var rng = new Random((uint)(Time.time * 1000));
	    
		var testArraySizes = new [] { 10,  20, 50, 100, 200, 300 };
		
	    var maxElements = testArraySizes[^1];
		var hashArr = new NativeArray<uint>(maxElements, Allocator.Temp);
		var hashUniqCheck = new NativeHashSet<uint>(maxElements, Allocator.Temp);
		for (int i = 0; i < maxElements; ++i)
		{
			var v = rng.NextUInt();
			if (hashUniqCheck.Add(v))
				hashArr[i] = v;
			else
			{
				Debug.LogWarning("Generator collision!");
				i--;
			}
		};
		
		for (var i = 0; i < testArraySizes.Length; ++i)
		{
			var h0 = hashArr.Slice(0, testArraySizes[i]).AsArray();
			Perfect2HashTable.Build(h0, out var pht, out var seed);
			PHT2_SingleTest(pht, h0, seed, iterationsCount);
		}
	    
		for (var i = 0; i < testArraySizes.Length; ++i)
		{
			var h0 = hashArr.Slice(0, testArraySizes[i]).AsArray();
			var hs = new NativeHashSet<uint>(h0.Length, Allocator.Temp);
			foreach (var v in h0)
			{
				hs.Add(v);
			}
			NativeHashMapPerformanceSingleTest(hs, h0, iterationsCount);
		}
	}
}
}
