#ifndef DEBUG_HLSL_
#define DEBUG_HLSL_

///////////////////////////////////////////////////////////////////////////////////////////

#include "Packages/com.rukhanka.animation/Rukhanka.Runtime/Common/Shaders/DebugMarkers.cs.hlsl"

///////////////////////////////////////////////////////////////////////////////////////////

#ifdef RUKHANKA_SHADER_DEBUG
#define CHECK_MIN_MAX_RANGE(debugMarker, v, minValue, maxValue) if (v < minValue || v >= maxValue) InterlockedAdd(debugLoggerCB[debugMarker], 1);

#define CHECK_RAW_BUFFER_OUT_OF_BOUNDS(debugMarker, index, count, buffer) \
{ \
    uint bufferSize = 0; \
    buffer.GetDimensions(bufferSize); \
    id ((int)index < 0 || (int)(index + count) > (int)bufferSize) InterlockedAdd(debugLoggerCB[debugMarker], increment); \
}

#define CHECK_STRUCTURED_BUFFER_OUT_OF_BOUNDS(debugMarker, index, buffer) \
{ \
    uint numStructs = 0; \
    uint stride = 0; \
    buffer.GetDimensions(numStructs, stride); \
    if ((int)index < 0 || (int)index >= (int)numStructs) InterlockedAdd(debugLoggerCB[debugMarker], 1); \
}

RWStructuredBuffer<uint> debugLoggerCB;
#else
#define CHECK_MIN_MAX_RANGE(debugMarker, v, minValue, maxValue)
#define CHECK_RAW_BUFFER_OUT_OF_BOUNDS(debugMarker, index, count, buffer)
#define CHECK_STRUCTURED_BUFFER_OUT_OF_BOUNDS(debugMarker, index, buffer)

#endif

///////////////////////////////////////////////////////////////////////////////////////////

#endif
