using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

// Blood mesh spawn system
// 
// Blood mesh spawn system - DÜZELTME
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    public partial struct BloodMeshSpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BloodMeshSpawnSettings>();
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Settings yoksa çık
            if (!SystemAPI.TryGetSingletonEntity<BloodMeshSpawnSettings>(out Entity spawnerEntity))
                return;
                
            var settings = SystemAPI.GetComponentRW<BloodMeshSpawnSettings>(spawnerEntity);
            
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            
            // Random seed - asla 0 olamaz!
            uint seed = (uint)(SystemAPI.Time.ElapsedTime * 1000);
            if (seed == 0) seed = 1;
            var random = new Random(seed);
            
            // Spawn request'leri işle
            foreach (var (request, entity) in 
                     SystemAPI.Query<RefRO<BloodMeshSpawnRequest>>()
                     .WithEntityAccess())
            {
                // Max mesh kontrolü
                if (settings.ValueRO.currentMeshCount >= settings.ValueRO.maxMeshCount)
                {
                    // Request'i yine de sil
                    commandBuffer.DestroyEntity(entity);
                    continue;
                }
                
                // 3-5 mesh spawn et
                int meshCount = random.NextInt(3, 6);
                
                for (int i = 0; i < meshCount; i++)
                {
                    // Blood mesh entity oluştur
                    var bloodMesh = commandBuffer.Instantiate(settings.ValueRO.bloodMeshPrefab);
                    
                    // Pozisyon
                    float3 randomOffset = new float3(
                        random.NextFloat(-0.5f, 0.5f),
                        0f,
                        random.NextFloat(-0.5f, 0.5f)
                    );
                    
                    var transform = LocalTransform.FromPositionRotationScale(
                        request.ValueRO.position + randomOffset,
                        quaternion.Euler(
                            random.NextFloat(-math.PI, math.PI),
                            random.NextFloat(-math.PI, math.PI),
                            random.NextFloat(-math.PI, math.PI)
                        ),
                        random.NextFloat(0.5f, 1.5f)
                    );
                    
                    commandBuffer.SetComponent(bloodMesh, transform);
                    
                    // Fizik velocity
                    float3 randomVelocity = new float3(
                        random.NextFloat(settings.ValueRO.initialVelocityMin.x, settings.ValueRO.initialVelocityMax.x),
                        random.NextFloat(settings.ValueRO.initialVelocityMin.y, settings.ValueRO.initialVelocityMax.y),
                        random.NextFloat(settings.ValueRO.initialVelocityMin.z, settings.ValueRO.initialVelocityMax.z)
                    );
                    
                    // Araç momentumu ekle
                    randomVelocity += request.ValueRO.vehicleVelocity * 0.3f;
                    
                    commandBuffer.SetComponent(bloodMesh, new PhysicsVelocity
                    {
                        Linear = randomVelocity,
                        Angular = new float3(
                            random.NextFloat(-5f, 5f),
                            random.NextFloat(-5f, 5f),
                            random.NextFloat(-5f, 5f)
                        )
                    });
                    
                    // Lifetime
                    commandBuffer.SetComponent(bloodMesh, new BloodMeshLifetime
                    {
                        spawnTime = (float)SystemAPI.Time.ElapsedTime,
                        lifetime = settings.ValueRO.meshLifetime + random.NextFloat(-1f, 1f)
                    });
                    
                    // Damping
                    commandBuffer.AddComponent(bloodMesh, new PhysicsDamping
                    {
                        Linear = settings.ValueRO.damping,
                        Angular = settings.ValueRO.damping * 2f
                    });
                }
                
                // Request'i sil
                commandBuffer.DestroyEntity(entity);
                
                // Mesh count güncelle
                settings.ValueRW.currentMeshCount += meshCount;
            }
            
            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();
        }
    }
    
    // Cleanup system aynı kalabilir
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct BloodMeshCleanupSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BloodMeshTag>();
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Settings yoksa çık
            if (!SystemAPI.TryGetSingletonEntity<BloodMeshSpawnSettings>(out Entity spawnerEntity))
                return;
                
            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            var settings = SystemAPI.GetComponentRW<BloodMeshSpawnSettings>(spawnerEntity);
            
            int destroyedCount = 0;
            
            // Süresi dolan mesh'leri temizle
            foreach (var (lifetime, entity) in 
                     SystemAPI.Query<RefRO<BloodMeshLifetime>>()
                     .WithAll<BloodMeshTag>()
                     .WithEntityAccess())
            {
                if (currentTime - lifetime.ValueRO.spawnTime > lifetime.ValueRO.lifetime)
                {
                    commandBuffer.DestroyEntity(entity);
                    destroyedCount++;
                }
            }
            
            // Mesh count güncelle
            if (destroyedCount > 0)
            {
                settings.ValueRW.currentMeshCount = math.max(0, settings.ValueRO.currentMeshCount - destroyedCount);
            }
            
            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();
        }
    }