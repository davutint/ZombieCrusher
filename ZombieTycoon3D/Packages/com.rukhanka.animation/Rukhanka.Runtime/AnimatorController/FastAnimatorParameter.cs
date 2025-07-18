using Unity.Entities;
using FixedStringName = Unity.Collections.FixedString512Bytes;
using UnityEngine;
using System;
using Rukhanka.Toolbox;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{

public struct FastAnimatorParameter
{
#if RUKHANKA_DEBUG_INFO
	public FixedStringName paramName;
#endif
	public uint hash;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public FastAnimatorParameter(FixedStringName name)
	{
		hash = name.CalculateHash32();
#if RUKHANKA_DEBUG_INFO
		paramName = name;
#endif
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public FastAnimatorParameter(uint hash)
	{
		this.hash = hash;
#if RUKHANKA_DEBUG_INFO
		paramName = default;
#endif
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	bool GetRuntimeParameterDataInternal(int paramIdx, DynamicBuffer<AnimatorControllerParameterComponent> runtimeParameters, out ParameterValue outData)
	{
		bool isValid = paramIdx >= 0;

		if (isValid)
		{
			outData = runtimeParameters[paramIdx].value;
		}
		else
		{
			outData = default;
		#if RUKHANKA_DEBUG_INFO
			Debug.LogError($"Could find animator parameter with name {paramName} in hash table! Returning default value!");
		#endif
		}
		return isValid;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public bool GetRuntimeParameterData(BlobAssetReference<PerfectHashTableBlob> pt, DynamicBuffer<AnimatorControllerParameterComponent> runtimeParameters, out ParameterValue outData)
	{
		var paramIdx = GetRuntimeParameterIndex(pt, runtimeParameters);
		return GetRuntimeParameterDataInternal(paramIdx, runtimeParameters, out outData);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public bool GetRuntimeParameterData(DynamicBuffer<AnimatorControllerParameterComponent> runtimeParameters, out ParameterValue outData)
	{
		var paramIdx = GetRuntimeParameterIndex(runtimeParameters);
		return GetRuntimeParameterDataInternal(paramIdx, runtimeParameters, out outData);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	bool SetRuntimeParameterDataInternal(int paramIdx, DynamicBuffer<AnimatorControllerParameterComponent> runtimeParameters, in ParameterValue paramData)
	{
		bool isValid = paramIdx >= 0;

		if (isValid)
		{
			var p = runtimeParameters[paramIdx];
			p.value = paramData;
			runtimeParameters[paramIdx] = p;
		}
	#if RUKHANKA_DEBUG_INFO
		else
		{
			Debug.LogError($"Could find animator parameter with name {paramName} in hash table! Setting value is failed!");
		}
	#endif
		return isValid;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public bool SetRuntimeParameterData(BlobAssetReference<PerfectHashTableBlob> pt, DynamicBuffer<AnimatorControllerParameterComponent> runtimeParameters, in ParameterValue paramData)
	{
		var paramIdx = GetRuntimeParameterIndex(pt, runtimeParameters);
		return SetRuntimeParameterDataInternal(paramIdx, runtimeParameters, paramData);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public bool SetRuntimeParameterData(DynamicBuffer<AnimatorControllerParameterComponent> runtimeParameters, in ParameterValue paramData)
	{
		var paramIdx = GetRuntimeParameterIndex(runtimeParameters);
		return SetRuntimeParameterDataInternal(paramIdx, runtimeParameters, paramData);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public bool SetTrigger(BlobAssetReference<PerfectHashTableBlob> pt, DynamicBuffer<AnimatorControllerParameterComponent> runtimeParameters) => SetRuntimeParameterData(pt, runtimeParameters, new ParameterValue() { boolValue = true });
	public bool SetTrigger(DynamicBuffer<AnimatorControllerParameterComponent> runtimeParameters) => SetRuntimeParameterData(runtimeParameters, new ParameterValue() { boolValue = true });
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	//	Linear search variant
	public static int GetRuntimeParameterIndex(uint hash, in ReadOnlySpan<AnimatorControllerParameterComponent> parameters)
	{
		for (int i = 0; i < parameters.Length; ++i)
		{
			var p = parameters[i];
			if (p.hash == hash)
				return i;
		}
		return -1;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	//	Perfect hash table variant
	public static int GetRuntimeParameterIndex(uint hash, in BlobAssetReference<PerfectHashTableBlob> pt, in ReadOnlySpan<AnimatorControllerParameterComponent> parameters)
	{
		var paramIdx = pt.Value.Query(hash);
		if (paramIdx < 0)
			return -1;

		return paramIdx;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public unsafe int GetRuntimeParameterIndex(in BlobAssetReference<PerfectHashTableBlob> pt, in DynamicBuffer<AnimatorControllerParameterComponent> acpc)
	{
		var span = new ReadOnlySpan<AnimatorControllerParameterComponent>(acpc.GetUnsafePtr(), acpc.Length);
		return GetRuntimeParameterIndex(hash, pt, span);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public unsafe int GetRuntimeParameterIndex(in DynamicBuffer<AnimatorControllerParameterComponent> acpc)
	{
		var span = new ReadOnlySpan<AnimatorControllerParameterComponent>(acpc.GetUnsafePtr(), acpc.Length);
		return GetRuntimeParameterIndex(hash, span);
	}
}
}
