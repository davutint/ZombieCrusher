using System;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Samples
{
class UIController_RuntimeSkinnedMeshSwitch: MonoBehaviour
{
    public Button headsSwitchBtn;
    public Button bodiesSwitchBtn;
    public Button leftArmsSwitchBtn;
    public Button rightArmsSwitchBtn;
    
    EntityQuery modularRigQuery;
    
/////////////////////////////////////////////////////////////////////////////////

    void Start()
    {
        headsSwitchBtn.onClick.AddListener(() => OnSwitchButtonClick(ModularBodyPart.Head));
        bodiesSwitchBtn.onClick.AddListener(() => OnSwitchButtonClick(ModularBodyPart.Body));
        leftArmsSwitchBtn.onClick.AddListener(() => OnSwitchButtonClick(ModularBodyPart.LeftArm));
        rightArmsSwitchBtn.onClick.AddListener(() => OnSwitchButtonClick(ModularBodyPart.RightArm));
        
        modularRigQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<ModularRigPartComponent>()
            .Build(World.DefaultGameObjectInjectionWorld.EntityManager);
    }
    
/////////////////////////////////////////////////////////////////////////////////

    void SetSkinnedMeshEnabled(Entity smr, EntityManager em, EntityCommandBuffer ecb, bool enabled)
    {
        ecb.SetEnabled(smr, enabled);
        var smrRenderEntities = em.GetBuffer<SkinnedMeshRenderEntity>(smr);
        foreach (var smrre in smrRenderEntities)
        {
            ecb.SetEnabled(smrre.value, enabled);
        }
    }

/////////////////////////////////////////////////////////////////////////////////

    void OnSwitchButtonClick(ModularBodyPart mbp)
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        var entities = modularRigQuery.ToEntityArray(Allocator.Temp);
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        
        for (var i = 0; i < entities.Length; ++i)
        {
            var e = entities[i];
            var mrcBuf = em.GetBuffer<ModularRigPartComponent>(e);
            for (var k = 0; k < mrcBuf.Length; ++k)
            {
                ref var mrc = ref mrcBuf.ElementAt(k);
                if (mrc.bodyPart == mbp)
                {
                    var prevIdx = mrc.currentPartIndex;
                    var nextIdx = (mrc.currentPartIndex + 1) % mrc.partsList.Length;
                    SetSkinnedMeshEnabled(mrc.partsList[prevIdx], em, ecb, false);
                    SetSkinnedMeshEnabled(mrc.partsList[nextIdx], em, ecb, true);
                    mrc.currentPartIndex = nextIdx;
                }
            }
        }
        ecb.Playback(em);
    }
}
}

