using ProjectDawn.Navigation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

   // Zombie'nin hedefini tutan component
    public struct ZombieTarget : IComponentData
    {
        public Entity targetEntity;
        public float3 lastKnownPosition;
        public float3 destination;
    }

    // Basitleştirilmiş navigation system - sadece AgentBody kullan
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ZombieNavigationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ZombieTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Player vehicle'ı bul
            Entity playerVehicle = Entity.Null;
            float3 playerPosition = float3.zero;
            
            foreach (var (transform, entity) in 
                     SystemAPI.Query<RefRO<LocalTransform>>()
                     .WithAll<VehicleTag>()
                     .WithEntityAccess())
            {
                playerVehicle = entity;
                playerPosition = transform.ValueRO.Position;
                break;
            }
            
            if (playerVehicle == Entity.Null)
                return;

            // Zombie'leri vehicle'a yönlendir
            foreach (var (zombieComp, transform, entity) in 
                     SystemAPI.Query<RefRO<ZombieComponent>, RefRO<LocalTransform>>()
                     .WithAll<ZombieTag>()
                     .WithEntityAccess())
            {
                if (!zombieComp.ValueRO.isAlive)
                    continue;

                // TODO: Agent Navigation API'sine göre destination set et
                // Dokümantasyonu kontrol et ve aşağıdaki yöntemlerden birini kullan:
                
                // Yöntem 1: AgentBody component'i varsa
                if (SystemAPI.HasComponent<AgentBody>(entity))
                {
                  var agent = SystemAPI.GetComponentRW<AgentBody>(entity);
                    agent.ValueRW.SetDestination(playerPosition);
                }
                
               
            }
        }
    }

    // Zombie target component'ini ekleyen system
    [BurstCompile]
    public partial struct ZombieTargetInitSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ZombieTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            
            // Target component'i olmayan zombilere ekle
            foreach (var (zombieTag, entity) in 
                     SystemAPI.Query<RefRO<ZombieTag>>()
                     .WithNone<ZombieTarget>()
                     .WithEntityAccess())
            {
                commandBuffer.AddComponent(entity, new ZombieTarget
                {
                    targetEntity = Entity.Null,
                    lastKnownPosition = float3.zero,
                    destination = float3.zero
                });
            }
            
            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();
        }
    }
