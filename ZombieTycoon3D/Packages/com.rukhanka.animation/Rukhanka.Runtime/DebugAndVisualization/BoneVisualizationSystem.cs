#if RUKHANKA_DEBUG_INFO

#if !RUKHANKA_NO_DEBUG_DRAWER
using Rukhanka.DebugDrawer;
#endif

using Rukhanka.Toolbox;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{

[WorldSystemFilter(WorldSystemFilterFlags.Default)]
[UpdateInGroup(typeof(PresentationSystemGroup))]
#if !RUKHANKA_NO_DEFORMATION_SYSTEM
[UpdateAfter(typeof(RukhankaDeformationSystemGroup))]
#endif
public partial class BoneVisualizationSystem: SystemBase
{
	EntityQuery cpuBoneVisualizeQuery, gpuBoneVisualizeQuery;
	
	struct GPUBoneRendererRigInfo
	{
		public LocalTransform rigWorldPose;
	}
	
	FrameFencedGPUBufferPool<GPUBoneRendererRigInfo> frameRigInfoCB;
	internal GPUBoneRenderer gpuBoneRenderer;
	
/////////////////////////////////////////////////////////////////////////////////

	protected override void OnCreate()
	{
		gpuBoneRenderer = new ();
		frameRigInfoCB = new (0xff, GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.LockBufferForWrite);
			
		cpuBoneVisualizeQuery = SystemAPI.QueryBuilder()
			.WithAll<RigDefinitionComponent, BoneVisualizationComponent>()
			.WithNone<GPUAnimationEngineTag>()
			.Build();
		
		gpuBoneVisualizeQuery = SystemAPI.QueryBuilder()
			.WithAll<RigDefinitionComponent, BoneVisualizationComponent, GPUAnimationEngineTag>()
			.Build();
		
		RequireForUpdate(cpuBoneVisualizeQuery);
	}
	
/////////////////////////////////////////////////////////////////////////////////

	protected override void OnUpdate()
	{
#if !RUKHANKA_NO_DEBUG_DRAWER
		Dependency = RenderBonesForCPUAnimators(Dependency);
#endif
		Dependency = RenderBonesForGPUAnimators(Dependency);
	}
	
/////////////////////////////////////////////////////////////////////////////////
	
	protected override void OnDestroy()
	{
		frameRigInfoCB.Dispose();
	}
	
/////////////////////////////////////////////////////////////////////////////////
	
	JobHandle RenderBonesForCPUAnimators(JobHandle dependsOn)
	{
		if (!SystemAPI.TryGetSingletonRW<Drawer>(out var dd))
			return dependsOn;
		
		if (!SystemAPI.TryGetSingleton<RuntimeAnimationData>(out var runtimeData))
			return dependsOn;
		
		var hasDCC = SystemAPI.TryGetSingleton<DebugConfigurationComponent>(out var dcc);
		var defaultColor = new float4(0, 1, 1, 1);
		var boneColorLines = hasDCC ? dcc.cpuRigColor : defaultColor;
		var boneColorTriangles = new float4(defaultColor.x, defaultColor.y, defaultColor.z, defaultColor.w * 0.3f);
		var boneColorLinesUINT = Drawer.ColorToUINT(boneColorLines);
		var boneColorTriUINT = Drawer.ColorToUINT(boneColorTriangles);
		
		var renderBonesJob = new RenderBonesCPUAnimatorsJob()
		{
			entityToDataOffsetMap = runtimeData.entityToDataOffsetMap,
			bonePoses = runtimeData.worldSpaceBonesBuffer,
			drawer = dd.ValueRW,
			colorLines = boneColorLinesUINT,
			colorTriangles = boneColorTriUINT
		};
		
		var rv = renderBonesJob.ScheduleParallel(cpuBoneVisualizeQuery, dependsOn);
		return rv;
	}
	
/////////////////////////////////////////////////////////////////////////////////
	
	JobHandle RenderBonesForGPUAnimators(JobHandle dependsOn)
	{
		if (!SystemAPI.TryGetSingleton<GPURuntimeAnimationData>(out var rad))
			return dependsOn;
		
		var rigCount = (int)rad.frameAnimatedRigsCounter;
		if (rigCount == 0 || gpuBoneVisualizeQuery.CalculateEntityCount() == 0)
			return dependsOn;
		
		frameRigInfoCB.Grow(rigCount);
		frameRigInfoCB.BeginFrame();
		var frameRigData = frameRigInfoCB.LockBufferForWrite(0, rigCount);
		
		var prepareBoneDataJob = new PrepareGPURigsJob()
		{
			frameRigData = frameRigData,
			frameEntityAnimatedDataOffsetsMap = rad.frameEntityAnimatedDataOffsetsMap,
			localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
			boneVisualizationLookup = SystemAPI.GetComponentLookup<BoneVisualizationComponent>(true),
		};
		
		var rv = prepareBoneDataJob.ScheduleParallel(dependsOn);
		rv.Complete();
		
		frameRigInfoCB.UnlockBufferAfterWrite(rigCount);
		
		var hasDCC = SystemAPI.TryGetSingleton<DebugConfigurationComponent>(out var dcc);
		var defaultColor = new float4(0, 1, 0, 1);
		var boneColor = hasDCC ? dcc.gpuRigColor : defaultColor;
		gpuBoneRenderer.RenderBones(frameRigInfoCB, boneColor);
		
		frameRigInfoCB.EndFrame();
		return rv;
	}
}
}

#endif