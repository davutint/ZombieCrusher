using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Hybrid.Baking;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{

[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
[UpdateAfter(typeof(BakingOnlyEntityAuthoringBakingSystem))]
partial class BoneVisualizationConversionSystem : SystemBase
{
    protected override void OnUpdate()
    {
    #if RUKHANKA_DEBUG_INFO
        if (!SystemAPI.TryGetSingleton<DebugConfigurationComponent>(out var dcc) || !dcc.visualizeAllRigs)
            return;

        var ecb = new EntityCommandBuffer(Allocator.Temp);
        
        foreach (var (_, e) in SystemAPI.Query<RigDefinitionComponent>()
                     .WithEntityAccess()
                     .WithOptions(EntityQueryOptions.IncludePrefab)
                     .WithNone<BoneVisualizationComponent>())
        {
            //  Add BoneVisualizationComponent to every animated entity
            ecb.AddComponent<BoneVisualizationComponent>(e);
        }
        
        ecb.Playback(EntityManager);
    #endif
    }
}
}
