#ifndef TRACK_HLSL_
#define TRACK_HLSL_

//  Use of potentially uninitialized variable
#pragma warning (disable: 4000)

/////////////////////////////////////////////////////////////////////////////////

#include "KeyFrame.hlsl"

/////////////////////////////////////////////////////////////////////////////////

#define BINDING_TYPE_UNKNOWN        0
#define BINDING_TYPE_TRANSLATION    1
#define BINDING_TYPE_QUATERNION     2
#define BINDING_TYPE_EULER_ANGLES   3
#define BINDING_TYPE_HUMAN_MUSCLE   4
#define BINDING_TYPE_SCALE          5

/////////////////////////////////////////////////////////////////////////////////

struct Track
{
    uint props;
    uint2 keyFrameRange;

    static const uint size = 3 * 4;

/////////////////////////////////////////////////////////////////////////////////

    int GetBindingType()
    {
        return props & 0xf;
    }

/////////////////////////////////////////////////////////////////////////////////

    int GetChannelIndex()
    {
        return props >> 4 & 3;
    }

/////////////////////////////////////////////////////////////////////////////////

    float SampleByBinarySearch(float time, int keyFrameBaseAddress)
    {
		uint startIndex = keyFrameRange.x;
		uint endIndex = keyFrameRange.x + keyFrameRange.y;
		bool less = true;
		bool greater = true;
		KeyFrame frame0 = (KeyFrame)0, frame1 = (KeyFrame)0;

		if (keyFrameRange.y < 3)
			return SampleByLinearSearch(time, keyFrameBaseAddress);

		while (endIndex - startIndex >= 1 && (less || greater) && endIndex > 1)
		{
			int middleIndex = (endIndex + startIndex) / 2;
			frame1 = KeyFrame::ReadFromRawBuffer(animationClips, keyFrameBaseAddress, middleIndex);
			frame0 = KeyFrame::ReadFromRawBuffer(animationClips, keyFrameBaseAddress, middleIndex - 1);
			
			less = time < frame0.time;
			greater = time > frame1.time;

			startIndex = greater ? middleIndex + 1 : startIndex;
			endIndex = less ? middleIndex : endIndex;
		}

		if (less)
			return KeyFrame::ReadFromRawBuffer(animationClips, keyFrameBaseAddress, startIndex).v;

		if (greater)
			return KeyFrame::ReadFromRawBuffer(animationClips, keyFrameBaseAddress, endIndex - 1).v;

		float f = (time - frame0.time) / (frame1.time - frame0.time);
		return EvaluateBezierCurve(frame0, frame1, f);
    }

/////////////////////////////////////////////////////////////////////////////////

    float SampleByLinearSearch(float time, int keyFrameBaseAddress)
    {
        uint keyFrameRangeEnd = keyFrameRange.x + keyFrameRange.y;
		for (uint i = keyFrameRange.x; i < keyFrameRangeEnd; ++i)
		{
			KeyFrame frame1 = KeyFrame::ReadFromRawBuffer(animationClips, keyFrameBaseAddress, i);
			if (frame1.time >= time)
			{
				if (i == keyFrameRange.x)
					return frame1.v;
				KeyFrame frame0 = KeyFrame::ReadFromRawBuffer(animationClips, keyFrameBaseAddress, i - 1);

				float f = (time - frame0.time) / (frame1.time - frame0.time);
				return EvaluateBezierCurve(frame0, frame1, f);
			}
		}
		return KeyFrame::ReadFromRawBuffer(animationClips, keyFrameBaseAddress, keyFrameRangeEnd - 1).v;
    }

/////////////////////////////////////////////////////////////////////////////////

    float GetFirstFrameValue(int keyFrameBaseAddress)
    {
        KeyFrame f = KeyFrame::ReadFromRawBuffer(animationClips, keyFrameBaseAddress, keyFrameRange.x);
        return f.v;
    }

/////////////////////////////////////////////////////////////////////////////////

    float GetLastFrameValue(int keyFrameBaseAddress)
    {
        KeyFrame f = KeyFrame::ReadFromRawBuffer(animationClips, keyFrameBaseAddress, keyFrameRange.x + keyFrameRange.y - 1);
        return f.v;
    }

/////////////////////////////////////////////////////////////////////////////////

	float EvaluateBezierCurve(KeyFrame f0, KeyFrame f1, float l)
	{
		float dt = f1.time - f0.time;
		float m0 = f0.outTan * dt;
		float m1 = f1.inTan * dt;

		float t2 = l * l;
		float t3 = t2 * l;

		float a = 2 * t3 - 3 * t2 + 1;
		float b = t3 - 2 * t2 + l;
		float c = t3 - t2;
		float d = -2 * t3 + 3 * t2;

		float rv = a * f0.v + b * m0 + c * m1 + d * f1.v;
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////

    static Track ReadFromRawBuffer(ByteAddressBuffer b, uint baseAddress, uint index)
    {
        uint addr = baseAddress + index * size;
        Track rv = (Track)0;
        rv.props = b.Load(addr);
        rv.keyFrameRange = b.Load2(addr + 4);

        CHECK_RAW_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_GPUANIMATOR_TRACK_READ, addr, size, b);

        return rv;
    }
};

#endif


