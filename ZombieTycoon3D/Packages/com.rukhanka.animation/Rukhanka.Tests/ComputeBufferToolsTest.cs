using NUnit.Framework;
using Unity.Collections;
using Rukhanka.Toolbox;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Tests
{ 
public class ComputeBufferToolsTests
{
    [Test]
    public void ComputeBufferClearTest()
    {
        var bufferSize = 0xffff;
        var readbackData = new int[bufferSize];
        var srcData = new NativeArray<int>(bufferSize, Allocator.Temp);
        using var buf = new GraphicsBuffer(GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.None, bufferSize, sizeof(int));
        
        var rng = new Random((uint)bufferSize);
        
        //  Fill buffer with random data
        for (var k = 0; k < bufferSize; ++k)
        {
            srcData[k] = rng.NextInt();
        }
        
        var numIterations = 10;
        for (var i = 0; i < numIterations; ++i)
        {
            buf.SetData(srcData);
            
            //  Now clear portion of buffer and check result
            var startIndex = rng.NextUInt((uint)bufferSize - 1);
            var count = rng.NextUInt((uint)bufferSize - startIndex);
            var clearValue = rng.NextInt();
            ComputeBufferTools.Clear(buf, startIndex * 4, count * 4, (uint)clearValue);
            
            buf.GetData(readbackData);
            
            //  Verify correctness
            for (var k = 0; k < bufferSize; ++k)
            {
                var v = readbackData[k];
                var srcV = srcData[k];
                if (k < startIndex || k >= startIndex + count)
                {
                    Assert.IsTrue(v == srcV);
                }
                else
                {
                    Assert.IsTrue(v == clearValue);
                }
            }
        }
    }
    
/////////////////////////////////////////////////////////////////////////////////

    [Test]
    public void ComputeBufferCopyTest()
    {
        var bufferSize = 0xffff;
        using var srcBuf = new GraphicsBuffer(GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.None, bufferSize, sizeof(int));
        var srcData = new NativeArray<int>(srcBuf.count, Allocator.Temp);
        using var dstBuf = new GraphicsBuffer(GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.None, bufferSize * 2, sizeof(int));
        var dstData = new NativeArray<int>(dstBuf.count, Allocator.Temp);
        var readbackData = new int[dstBuf.count];
        
        var rng = new Random((uint)bufferSize);
        
        //  Fill buffer with random data
        for (var k = 0; k < srcData.Length; ++k)
            srcData[k] = rng.NextInt();
        
        for (var k = 0; k < dstData.Length; ++k)
            dstData[k] = rng.NextInt();
        
        var numIterations = 100;
        for (var i = 0; i < numIterations; ++i)
        {
            srcBuf.SetData(srcData);
            dstBuf.SetData(dstData);
            
            var srcIndex = rng.NextUInt((uint)srcData.Length - 1);
            var copyCount = rng.NextUInt((uint)srcData.Length - srcIndex);
            var dstIndex = rng.NextUInt((uint)dstData.Length - copyCount);
            ComputeBufferTools.Copy(srcBuf, dstBuf, srcIndex * 4, dstIndex * 4, copyCount * 4);
            
            dstBuf.GetData(readbackData);
            
            //  Verify correctness
            for (var k = 0; k < readbackData.Length; ++k)
            {
                var v = readbackData[k];
                var si = k - dstIndex;
                if (si < 0 || si >= copyCount)
                {
                    var sv = dstData[k];
                    Assert.IsTrue(v == sv);
                }
                else
                {
                    var sv = srcData[(int)(si + srcIndex)];
                    Assert.IsTrue(v == sv);
                }
            }
        }
    }
    
/////////////////////////////////////////////////////////////////////////////////

    [Test]
    public void ComputeBufferResizeTest()
    {
        var srcBufferSize = 0xffff;
        var dstBufferSize = srcBufferSize * 2;
        var srcData = new NativeArray<int>(srcBufferSize, Allocator.Temp);
        
        var rng = new Random((uint)srcBufferSize);
        
        //  Fill buffer with random data
        for (var k = 0; k < srcData.Length; ++k)
            srcData[k] = rng.NextInt();
        
        var numIterations = 10;
        for (var i = 0; i < numIterations; ++i)
        {
            var srcBuf = new GraphicsBuffer(GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.None, srcBufferSize, sizeof(int));
            srcBuf.SetData(srcData);
            var newSize = rng.NextInt(dstBufferSize);
            using var newBuf = ComputeBufferTools.Resize(srcBuf, newSize);
            newBuf.Release();
            var readbackBuf = new int[newSize];
            newBuf.GetData(readbackBuf);
            
            var sz = math.min(newSize, srcData.Length);
            for (var k = 0; k < sz; ++k)
            {
                Assert.IsTrue(readbackBuf[k] == srcData[k]);
            }
        }
    }
}
}