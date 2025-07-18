
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Samples
{
[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation)]
partial class DistanceBasedMixedModeAnimatorSystem: SystemBase
{
	public float switchDistance;
	
/////////////////////////////////////////////////////////////////////////////////
	
	[BurstCompile]
	[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
	partial struct AnimatorSwitchJob: IJobEntity
	{
		public float3 refPose;
		public float switchDistance;
		
		void Execute(EnabledRefRW<GPUAnimationEngineTag> gt, in LocalTransform lt)
		{
			var dv = refPose - lt.Position;
			var d = math.length(dv);
			
			gt.ValueRW = d > switchDistance;
		}
	}
	
/////////////////////////////////////////////////////////////////////////////////

	protected override void OnCreate()
	{
		Enabled = false;	
	}
	
/////////////////////////////////////////////////////////////////////////////////

	protected override void OnUpdate()
	{
		var switchJob = new AnimatorSwitchJob()
		{
			refPose = Camera.main.transform.position,
			switchDistance = switchDistance
		};
		switchJob.ScheduleParallel();
	}
}
}
