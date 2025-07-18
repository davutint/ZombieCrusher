
using ProjectDawn.Navigation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

  // System'ler Entity'leri işleyen kod parçalarıdır
    // ISystem interface'i kullanarak modern DOTS sistemi yazıyoruz
   /* [BurstCompile]
    public partial struct ZombieSpawnSystem : ISystem
    {
        // System başlatıldığında çalışır
        public void OnCreate(ref SystemState state)
        {
            // Bu system'in çalışması için gerekli component'leri belirtiyoruz
            state.RequireForUpdate<ZombieSpawnSettings>();
            state.RequireForUpdate<SpawnPointElement>();
        }

        // Her frame çalışır
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Zamanı al
            var deltaTime = SystemAPI.Time.DeltaTime;
            var currentTime = (float)SystemAPI.Time.ElapsedTime;

            // Spawn manager entity'sini bul
            var spawnManagerEntity = SystemAPI.GetSingletonEntity<ZombieSpawnSettings>();
            
            // Settings'i referans olarak al (değiştirebilmek için)
            var settings = SystemAPI.GetComponentRW<ZombieSpawnSettings>(spawnManagerEntity);
            
            // Spawn noktalarını al
            var spawnPoints = SystemAPI.GetBuffer<SpawnPointElement>(spawnManagerEntity);

            // Spawn zamanı geldi mi kontrol et
            if (currentTime >= settings.ValueRW.nextSpawnTime && 
                settings.ValueRO.currentZombieCount < settings.ValueRO.maxZombieCount)
            {
                // Rastgele spawn noktası seç
                var random = new Random((uint)(currentTime * 1000));
                var spawnIndex = random.NextInt(0, spawnPoints.Length);
                var spawnPoint = spawnPoints[spawnIndex];

                // Zombie entity'si oluştur
                var zombieEntity = state.EntityManager.Instantiate(settings.ValueRO.zombiePrefab);

                // Position ve rotation ayarla
                var transform = SystemAPI.GetComponentRW<LocalTransform>(zombieEntity);
                transform.ValueRW.Position = spawnPoint.position;
                transform.ValueRW.Rotation = spawnPoint.rotation;

                // Spawn data güncelle
                var spawnData = SystemAPI.GetComponentRW<ZombieSpawnData>(zombieEntity);
                spawnData.ValueRW.spawnPointIndex = spawnIndex;
                spawnData.ValueRW.spawnTime = currentTime;

                // Navigation için destination ekle
                if (SystemAPI.HasComponent<AgentBody>(zombieEntity))
                {
                    var agent = SystemAPI.GetComponentRW<AgentBody>(zombieEntity);
                    agent.ValueRW.IsStopped = false; // Agent'i aktif et
                }

                // Sayıları güncelle
                settings.ValueRW.currentZombieCount++;
                settings.ValueRW.nextSpawnTime = currentTime + settings.ValueRO.spawnInterval;

                // UI güncelleme isteği
                var uiUpdate = SystemAPI.GetComponentRW<UIUpdateRequest>(spawnManagerEntity);
                uiUpdate.ValueRW.totalZombies = settings.ValueRO.currentZombieCount;

                // Debug log (Burst'te çalışmaz, sadece editor'de)
                #if UNITY_EDITOR
                Debug.Log($"Zombie spawned! Total: {settings.ValueRO.currentZombieCount}");
                #endif
            }
        }
    }

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
    */