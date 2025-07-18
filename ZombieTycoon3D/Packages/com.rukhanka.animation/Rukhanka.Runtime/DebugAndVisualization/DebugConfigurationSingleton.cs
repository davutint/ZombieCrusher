
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Serialization;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
	public struct DebugConfigurationComponent: IComponentData
	{
		public bool logAnimatorControllerProcesses;
		public bool logAnimationCalculationProcesses;
		public bool logAnimationEvents;
		public bool logAnimatorControllerEvents;

		public bool visualizeAllRigs;
		public float4 cpuRigColor;
		public float4 gpuRigColor;
		public float4 serverRigColor;

/////////////////////////////////////////////////////////////////////////////////

		public static DebugConfigurationComponent Default()
		{
			var rv = new DebugConfigurationComponent()
			{
				cpuRigColor = new float4(0, 1, 1, 0.3f),
				gpuRigColor = new float4(0, 1, 0, 0.3f),
				serverRigColor = new float4(1, 1, 0, 0.3f),
			};
			return rv;
		}
	}
}

