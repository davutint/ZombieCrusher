using ProjectDawn.Navigation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

    // Zombie pool state
    public struct ZombiePooled : IComponentData
    {
        public bool isPooled; // true = havuzda bekliyor, false = aktif
    }

    // Zombie ölünce pool'a geri koy
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(ZombieVehicleCollisionSystem))]
    public partial struct ZombiePoolReturnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ZombieDeathEvent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (deathEvent, zombieComp, transform, entity) in
                     SystemAPI.Query<RefRO<ZombieDeathEvent>, RefRW<ZombieComponent>, RefRW<LocalTransform>>()
                     .WithEntityAccess())
            {
                // Pool component'i var mı kontrol et
                if (SystemAPI.HasComponent<ZombiePooled>(entity))
                {
                    // Pool'a geri koy
                    var pooled = SystemAPI.GetComponentRW<ZombiePooled>(entity);
                    pooled.ValueRW.isPooled = true;
                    zombieComp.ValueRW.isAlive = false;

                    // Görünmez yap (Y = -100)
                    transform.ValueRW.Position = new float3(0, -100, 0);

                    // Navigation'ı durdur
                    if (SystemAPI.HasComponent<AgentBody>(entity))
                    {
                        var agent = SystemAPI.GetComponentRW<AgentBody>(entity);
                        agent.ValueRW.IsStopped = true;
                    }
                }
                else
                {
                    // Pool component'i yoksa (eski zombiler), yok et veya pool component ekle
                    // Biz pool component ekleyip havuza katalım
                    commandBuffer.AddComponent(entity, new ZombiePooled { isPooled = true });
                    zombieComp.ValueRW.isAlive = false;
                    transform.ValueRW.Position = new float3(0, -100, 0);

                    if (SystemAPI.HasComponent<AgentBody>(entity))
                    {
                        var agent = SystemAPI.GetComponentRW<AgentBody>(entity);
                        agent.ValueRW.IsStopped = true;
                    }
                }

                // Death event'i kaldır
                commandBuffer.RemoveComponent<ZombieDeathEvent>(entity);
            }

            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();
        }
    }
