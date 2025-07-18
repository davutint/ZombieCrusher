
using Rukhanka.Toolbox;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Samples
{
public partial class ModularRigInitPartsSystem: SystemBase
{
    EntityQuery modularRigQuery, switchableBodyPartsQuery;
    BufferTypeHandle<ModularRigPartComponent> modularRigBufAccessor;
    EntityTypeHandle entityTypeHandle;
    
////////////////////////////////////////////////////////////////////////////////////////
    
    protected override void OnCreate()
    {
        modularRigQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<ModularRigPartComponent>()
            .Build(EntityManager);
        
        switchableBodyPartsQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<SwitchableBodyPartComponent>()
            .WithOptions(EntityQueryOptions.IncludePrefab)
            .Build(EntityManager);
        
        modularRigBufAccessor = GetBufferTypeHandle<ModularRigPartComponent>();
    }
    
////////////////////////////////////////////////////////////////////////////////////////

    void InstantiateSkinnedPart(Entity prefabRoot, Entity smrRootBone, Entity animatedEntity, EntityCommandBuffer ecb, bool initDisabled)
    {
        if (!SystemAPI.HasComponent<AnimatedSkinnedMeshComponent>(prefabRoot) ||
            !SystemAPI.HasBuffer<SkinnedMeshRenderEntity>(prefabRoot))
            return;
        
        BurstAssert.IsTrue(!SystemAPI.HasBuffer<LinkedEntityGroup>(prefabRoot), "LEG should not be on skinned mesh entity");
        
        var smrInstance = ecb.Instantiate(prefabRoot);
        
        var p = new Parent();
        p.Value = animatedEntity;
        ecb.SetComponent(smrInstance, p);
        
        var asmc = EntityManager.GetComponentData<AnimatedSkinnedMeshComponent>(prefabRoot);
        
        p.Value = smrRootBone != Entity.Null ? smrRootBone : smrInstance;
        asmc.rootBoneEntity = p.Value;
        asmc.animatedRigEntity = animatedEntity;
        asmc.rootBoneIndexInRig = EntityManager.GetComponentData<AnimatorEntityRefComponent>(p.Value).boneIndexInAnimationRig;
        ecb.SetComponent(smrInstance, asmc);
        
        var renderEntities = ecb.AddBuffer<SkinnedMeshRenderEntity>(smrInstance);
        
        if (initDisabled)
            ecb.AddComponent<Disabled>(smrInstance);
        
        var smres = EntityManager.GetBuffer<SkinnedMeshRenderEntity>(prefabRoot);
        for (var i = 0; i < smres.Length; ++i)
        {
            var smre = smres[i].value;
            var smreInstance = ecb.Instantiate(smre);
            ecb.SetComponent(smreInstance, p);
            
            var arc = new AnimatedRendererComponent();
            arc.skinnedMeshEntity = smrInstance;
            arc.animatorEntity = animatedEntity;
            ecb.SetComponent(smreInstance, arc);
            
            renderEntities.Add(new () { value = smreInstance} );
            
            if (initDisabled)
                ecb.AddComponent<Disabled>(smreInstance);
        }
    }

////////////////////////////////////////////////////////////////////////////////////////

    protected override unsafe void OnUpdate()
    {
        if (switchableBodyPartsQuery.IsEmpty || modularRigQuery.IsEmpty)
            return;
        
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var chunks = modularRigQuery.ToArchetypeChunkArray(Allocator.Temp);
        
        modularRigBufAccessor.Update(this);
        entityTypeHandle.Update(this);
        
        //  The main idea is to define set of replaceable skinned mesh renderers in separate prefab. During instantiation of an animated entity that
        //  want to use them, copy all parts (represented as entities) to own hierarchy and switch them in runtime using 'Disabled' component
        //  Things is bit complicated because skinned mesh renderer in entity representation consists of main entity (corresponds to the original skinned mesh GameObject),
        //  and one or more render entities that are create during baking process (one for each submesh).
        foreach (var (sbp, e) in SystemAPI.Query<SwitchableBodyPartComponent>().WithEntityAccess().WithOptions(EntityQueryOptions.IncludePrefab))
        {
            //  Loop over all modular animated entities
            for (var k = 0; k < chunks.Length; ++k)
            {
                var c = chunks[k];
                var mrba = c.GetBufferAccessor(ref modularRigBufAccessor);
                var processedEntities = c.GetEntityDataPtrRO(entityTypeHandle);
                for (var l = 0; l < c.Count; ++l)
                {
                    var mrbArr = mrba[l];
                    var processedEntity = processedEntities[l];
                    for (var m = 0; m < mrbArr.Length; ++m)
                    {
                        ref var mrb = ref mrbArr.ElementAt(m);
                        if (mrb.bodyPart == sbp.bodyPart)
                        {
                            //  Instantiate copy of SMR entities and attach them to the 
                            Debug.Log($"Adding part {e} as {mrb.bodyPart}");
                            InstantiateSkinnedPart(e, mrb.skinnedMeshRootBoneEntity, processedEntity, ecb, mrb.currentPartIndex >= 0);
                            mrb.currentPartIndex = 0;
                            //  We cannot add created smr instance reference here because entity is still deferred.
                            //  Need to make separate loop after command buffer playback
                        }
                    }
                }
            }
            
            ecb.RemoveComponent<SwitchableBodyPartComponent>(e);
        }
        ecb.Playback(EntityManager);
        
        //  Loop one more time but this time we are processing already instantiated entities
        //  We need to populate parts list
        ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (sbp, asmc, e) in SystemAPI.Query<SwitchableBodyPartComponent, AnimatedSkinnedMeshComponent>().WithEntityAccess().WithOptions(EntityQueryOptions.IncludeDisabledEntities))
        {
            var mrpBuf = EntityManager.GetBuffer<ModularRigPartComponent>(asmc.animatedRigEntity);
            for (int i = 0; i < mrpBuf.Length; ++i)
            {
                ref var mrp = ref mrpBuf.ElementAt(i);
                if (mrp.bodyPart == sbp.bodyPart)
                {
                    mrp.partsList.Add(e);
                }
            }
            ecb.RemoveComponent<SwitchableBodyPartComponent>(e);
        }
        ecb.Playback(EntityManager);
    }
}
}
