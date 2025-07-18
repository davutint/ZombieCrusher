#if !RUKHANKA_NO_DEFORMATION_SYSTEM

using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
partial struct GPUAttachmentsUpdateSystem
{
[BurstCompile]
partial struct UpdateGPUAttachmentBoneIndexJob: IJobEntity
{
    struct ParentBoneInfo
    {
        public Entity boneEntity;
        public Entity rigEntity;
        public int boneIndexInRig;
        public float4x4 attachmentToBoneTransform;
    }
    
    [ReadOnly]
    public NativeParallelHashMap<Entity, GPURuntimeAnimationData.FrameOffsets> frameEntityAnimatedDataOffsetsMap;
    [ReadOnly]
    public ComponentLookup<LocalTransform> localTransformLookup;
    [ReadOnly]
    public ComponentLookup<AnimatorEntityRefComponent> rigBoneRefLookup;
    [ReadOnly]
    public ComponentLookup<Parent> parentLookup;
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void Execute
    (
        Entity e,
        ref GPUAttachmentBoneIndexMPComponent abi,
        ref GPUAttachmentToBoneTransformMPComponent atbt,
        ref GPURigEntityLocalToWorldMPComponent rltw
    )
    {
        abi.boneIndex = -1;
        
        var pbi = GetParentRigBoneEntity(e);
        if (pbi.boneEntity == Entity.Null)
            return;
        
        atbt.value = pbi.attachmentToBoneTransform;
        
        localTransformLookup.TryGetComponent(pbi.rigEntity, out var rootLocalTransform);
        rltw.value = float4x4.TRS(rootLocalTransform.Position, rootLocalTransform.Rotation, rootLocalTransform.Scale);
        
        if (frameEntityAnimatedDataOffsetsMap.TryGetValue(pbi.rigEntity, out var boneOffset))
            abi.boneIndex = boneOffset.boneIndex + pbi.boneIndexInRig;
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    ParentBoneInfo GetParentRigBoneEntity(Entity e)
    {
        AnimatorEntityRefComponent rbr = default;
        float4x4 entityToBoneTransform = float4x4.identity;
        var parent = new Parent() { Value = e};
        
        do
        {
            if (!localTransformLookup.TryGetComponent(parent.Value, out var plt))
                break;
            if (rigBoneRefLookup.TryGetComponent(parent.Value, out rbr))
                break;
            entityToBoneTransform = math.mul(plt.ToMatrix(), entityToBoneTransform);
        }
        while (parentLookup.TryGetComponent(parent.Value, out parent));
        
        var rv = new ParentBoneInfo();
        if (rbr.animatorEntity != Entity.Null)
        {
            rv.boneEntity = parent.Value;
            rv.rigEntity = rbr.animatorEntity;
            rv.boneIndexInRig = rbr.boneIndexInAnimationRig;
            rv.attachmentToBoneTransform = entityToBoneTransform;
        }
        
        return rv;
    }
}
}
}

#endif
