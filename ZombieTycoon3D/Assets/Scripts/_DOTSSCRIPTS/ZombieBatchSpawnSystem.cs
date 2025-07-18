using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
// Batch spawn settings
   
   // Batch spawn settings
   /* public struct ZombieBatchSpawnSettings : IComponentData
    {
        public int maxZombie;
        public int batchSize;        // Tek seferde kaç zombie spawn olacak
        public float batchInterval;  // Batch'ler arası süre
        public float nextBatchTime;
        public int currentZombieCount;       // Şu anki zombie sayısı
        public int killedZombieCount;    
    }

    // Basit ve performanslı batch spawn system
    [BurstCompile]
    public partial struct ZombieBatchSpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ZombieSpawnSettings>();
            
            // Batch spawn settings ekle
            var entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, new ZombieBatchSpawnSettings
            {
                batchSize = 10,      // 10'ar zombie spawn et
                batchInterval = 2f,  // 2 saniyede bir
                nextBatchTime = 2f
            });
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            
            // Singleton'ları al
            var spawnManagerEntity = SystemAPI.GetSingletonEntity<ZombieSpawnSettings>();
            var settings = SystemAPI.GetComponentRW<ZombieSpawnSettings>(spawnManagerEntity);
            var spawnPoints = SystemAPI.GetBuffer<SpawnPointElement>(spawnManagerEntity);
            
            var batchSettingsEntity = SystemAPI.GetSingletonEntity<ZombieBatchSpawnSettings>();
            var batchSettings = SystemAPI.GetComponentRW<ZombieBatchSpawnSettings>(batchSettingsEntity);
            
            // Batch spawn zamanı kontrolü
            if (currentTime < batchSettings.ValueRO.nextBatchTime)
                return;
                
            // Max zombie kontrolü
            if (settings.ValueRO.currentZombieCount >= settings.ValueRO.maxZombieCount)
                return;
            
            // Kaç zombie spawn edebiliriz?
            int spawnCount = math.min(
                batchSettings.ValueRO.batchSize,
                settings.ValueRO.maxZombieCount - settings.ValueRO.currentZombieCount
            );
            
            if (spawnCount <= 0)
                return;
            
            // Batch spawn için native array
            var spawnCommands = new NativeArray<Entity>(spawnCount, Allocator.TempJob);
            var spawnTransforms = new NativeArray<LocalTransform>(spawnCount, Allocator.TempJob);
            
            // Random seed
            uint seed = math.max(1, (uint)(currentTime * 1000));
            var random = new Random(seed);
            
            // Spawn verilerini hazırla
            for (int i = 0; i < spawnCount; i++)
            {
                var spawnIndex = random.NextInt(0, spawnPoints.Length);
                var spawnPoint = spawnPoints[spawnIndex];
                
                spawnTransforms[i] = LocalTransform.FromPositionRotation(
                    spawnPoint.position + random.NextFloat3(new float3(-1, 0, -1), new float3(1, 0, 1)),
                    spawnPoint.rotation
                );
            }
            
            // Batch instantiate - ÇOK DAHA HIZLI!
            state.EntityManager.Instantiate(settings.ValueRO.zombiePrefab, spawnCommands);
            
            // Transform'ları batch olarak set et
            for (int i = 0; i < spawnCount; i++)
            {
                state.EntityManager.SetComponentData(spawnCommands[i], spawnTransforms[i]);
                
                // Target component ekle (navigation için)
                state.EntityManager.AddComponentData(spawnCommands[i], new ZombieTarget
                {
                    targetEntity = Entity.Null,
                    lastKnownPosition = float3.zero,
                    destination = float3.zero
                });
            }
            
            // Cleanup
            spawnCommands.Dispose();
            spawnTransforms.Dispose();
            
            // Sayıları güncelle
            settings.ValueRW.currentZombieCount += spawnCount;
            batchSettings.ValueRW.nextBatchTime = currentTime + batchSettings.ValueRO.batchInterval;
            
            // UI güncelleme
            var uiUpdate = SystemAPI.GetComponentRW<UIUpdateRequest>(spawnManagerEntity);
            uiUpdate.ValueRW.totalZombies = settings.ValueRO.currentZombieCount;
        }
    }*/