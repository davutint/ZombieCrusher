using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ZombieCrusher.DOTS
{
    
    // Zombie sayısını takip eden system
    [BurstCompile]
    public partial struct ZombieCountSystem : ISystem
    {
        private EntityQuery aliveZombieQuery;
        private int lastZombieCount;

        public void OnCreate(ref SystemState state)
        {
            // Sadece yaşayan zombie'leri say
            aliveZombieQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<ZombieTag>(),
                ComponentType.ReadOnly<ZombieComponent>()
            );

            state.RequireForUpdate<ZombieSpawnSettings>();
            lastZombieCount = 0;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var spawnManagerEntity = SystemAPI.GetSingletonEntity<ZombieSpawnSettings>();
            var settings = SystemAPI.GetComponentRW<ZombieSpawnSettings>(spawnManagerEntity);

            // Yaşayan zombie sayısını hesapla
            int aliveCount = 0;
            foreach (var (zombieComp, entity) in 
                     SystemAPI.Query<RefRO<ZombieComponent>>()
                     .WithAll<ZombieTag>()
                     .WithEntityAccess())
            {
                if (zombieComp.ValueRO.isAlive)
                {
                    aliveCount++;
                }
            }

            // Sayı değiştiyse güncelle
            if (aliveCount != lastZombieCount)
            {
                settings.ValueRW.currentZombieCount = aliveCount;
                lastZombieCount = aliveCount;
                
                // UI güncelleme isteği
                var uiUpdate = SystemAPI.GetComponentRW<UIUpdateRequest>(spawnManagerEntity);
                uiUpdate.ValueRW.totalZombies = aliveCount;
                uiUpdate.ValueRW.killedZombies = settings.ValueRO.killedZombieCount;
            }
        }
    }
}