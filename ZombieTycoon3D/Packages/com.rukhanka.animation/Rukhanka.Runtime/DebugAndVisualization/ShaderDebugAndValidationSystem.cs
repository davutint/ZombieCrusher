#if RUKHANKA_SHADER_DEBUG

using System;
using Unity.Entities;
using UnityEngine;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{

[WorldSystemFilter(WorldSystemFilterFlags.Default)]
[UpdateInGroup(typeof(RukhankaDeformationSystemGroup), OrderLast = true)]
public partial class ShaderDebugAndValidationSystem: SystemBase
{
	GraphicsBuffer debugLoggerCB;
	int[] debugLoggerReadbackData;
	int[] debugLoggerZeroData;
	
/////////////////////////////////////////////////////////////////////////////////

	protected override void OnCreate()
	{
		var totalMarkers = (int)RukhankaDebugMarkers.Total;
		debugLoggerReadbackData = new int[totalMarkers];
		debugLoggerZeroData = new int[totalMarkers];
		debugLoggerCB = new GraphicsBuffer(GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.None, totalMarkers, sizeof(int));
		debugLoggerCB.SetData(debugLoggerZeroData);
		Shader.SetGlobalBuffer("debugLoggerCB", debugLoggerCB);
	}
	
/////////////////////////////////////////////////////////////////////////////////

	protected override void OnUpdate()
	{
		debugLoggerCB.GetData(debugLoggerReadbackData);
		debugLoggerCB.SetData(debugLoggerZeroData);
		
		for (var i = 0; i < debugLoggerReadbackData.Length; ++i)
		{
			var errorsCount = debugLoggerReadbackData[i];
			if (errorsCount == 0)
				continue;
			
			var dm = (RukhankaDebugMarkers)i;
			Debug.LogException(new Exception($"Shader Validation: '{dm.ToString()}' marker error count '{errorsCount}'"));
		}
	}
	
/////////////////////////////////////////////////////////////////////////////////
	
	protected override void OnDestroy()
	{
		if (debugLoggerCB != null)
			debugLoggerCB.Dispose();
	}
}
}

#endif