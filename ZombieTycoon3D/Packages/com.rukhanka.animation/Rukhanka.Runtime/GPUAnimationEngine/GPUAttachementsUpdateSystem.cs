#if !RUKHANKA_NO_DEFORMATION_SYSTEM

using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{

[WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Default)]
[UpdateInGroup(typeof(RukhankaDeformationSystemGroup))]
[UpdateAfter(typeof(MeshDeformationSystem))]
partial struct GPUAttachmentsUpdateSystem: ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState ss)
    {
        var gpuAttachmentsQuery = SystemAPI.QueryBuilder()
            .WithAll<GPUAttachmentBoneIndexMPComponent>()
            .Build();
        ss.RequireForUpdate(gpuAttachmentsQuery);
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    [BurstCompile]
    public void OnUpdate(ref SystemState ss)
    {
        var animatedDataOffsetsMap =
            SystemAPI.TryGetSingleton<GPURuntimeAnimationData>(out var rad)
            ? rad.frameEntityAnimatedDataOffsetsMap
            : new (0x4, ss.WorldUpdateAllocator);
        
        var attachmentBoneIndexUpdateJob = new UpdateGPUAttachmentBoneIndexJob()
        {
            frameEntityAnimatedDataOffsetsMap = animatedDataOffsetsMap,
            localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
            parentLookup = SystemAPI.GetComponentLookup<Parent>(true),
            rigBoneRefLookup = SystemAPI.GetComponentLookup<AnimatorEntityRefComponent>(true),
        };
        var attachmentBoneIndexUpdateJH = attachmentBoneIndexUpdateJob.ScheduleParallel(ss.Dependency);
        ss.Dependency = attachmentBoneIndexUpdateJH;
        ss.Dependency.Complete();
    }
}
}

#endif
