using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

// Zombie ölümünü işlemek için component
public struct ZombieDeathEvent : IComponentData
{
    public float3 deathPosition;
    public Entity killerEntity;
}

// Agent Navigation collision event'lerini dinleyen system
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
public partial struct ZombieVehicleCollisionSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ZombieSpawnSettings>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
        var spawnManagerEntity = SystemAPI.GetSingletonEntity<ZombieSpawnSettings>();
        var spawnSettings = SystemAPI.GetComponentRW<ZombieSpawnSettings>(spawnManagerEntity);

        // Tüm zombie'leri kontrol et
        foreach (var (zombieComp, transform, entity) in
                 SystemAPI.Query<RefRW<ZombieComponent>, RefRO<LocalTransform>>()
                 .WithAll<ZombieTag>()
                 .WithEntityAccess())
        {
            // Zombie zaten ölüyse atla
            if (!zombieComp.ValueRO.isAlive)
                continue;

            // Agent collision kontrolü
            // NOT: Agent Navigation'ın kendi collision event sistemi var
            // Bunu kullanmak için AgentCollisionSystem'e bağlanmamız gerekebilir

            // Basit mesafe kontrolü (geçici çözüm)
            // Vehicle'ların pozisyonlarını kontrol et
            foreach (var (vehicleTransform, vehicleEntity) in
                     SystemAPI.Query<RefRO<LocalTransform>>()
                     .WithAll<VehicleTag>()
                     .WithEntityAccess())
            {
                float3 zombiePos = transform.ValueRO.Position;
                float3 vehiclePos = vehicleTransform.ValueRO.Position;

                // Basit mesafe kontrolü (2 birim yakınlık)
                float distanceSq = math.distancesq(zombiePos, vehiclePos);
                if (distanceSq < 4f) // 2^2 = 4
                {
                    // Zombie'yi öldür
                    zombieComp.ValueRW.isAlive = false;

                    // Death event ekle (destroy yerine pool'a dönecek)
                    commandBuffer.AddComponent(entity, new ZombieDeathEvent
                    {
                        deathPosition = zombiePos,
                        killerEntity = vehicleEntity
                    });

                    // Kill count'u artır
                    spawnSettings.ValueRW.killedZombieCount++;

                    // Blood mesh spawn request oluştur
                    var bloodMeshEntity = commandBuffer.CreateEntity();

                    // Araç hızını al (momentum transfer için)
                    float3 vehicleVelocity = float3.zero;
                    if (SystemAPI.HasComponent<PhysicsVelocity>(vehicleEntity))
                    {
                        var vehiclePhysics = SystemAPI.GetComponent<PhysicsVelocity>(vehicleEntity);
                        vehicleVelocity = vehiclePhysics.Linear;
                    }

                    commandBuffer.AddComponent(bloodMeshEntity, new BloodMeshSpawnRequest
                    {
                        position = zombiePos,
                        rotation = transform.ValueRO.Rotation,
                        vehicleVelocity = vehicleVelocity
                    });

                    // UI güncelleme
                    var uiUpdate = SystemAPI.GetComponentRW<UIUpdateRequest>(spawnManagerEntity);
                    uiUpdate.ValueRW.killedZombies = spawnSettings.ValueRO.killedZombieCount;

                    break; // Bu zombie için kontrol bitti
                }
            }
        }

        commandBuffer.Playback(state.EntityManager);
        commandBuffer.Dispose();
    }
}

// ZombieCleanupSystem KALDIRILDI - ZombiePoolReturnSystem kullanılıyor
