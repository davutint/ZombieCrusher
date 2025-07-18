using ProjectDawn.Navigation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

 // Zombie pool state
   /* public struct ZombiePooled : IComponentData
    {
        public bool isPooled; // true = havuzda bekliyor, false = aktif
    }
    
    // Performanslı spawn system - Entity Pooling ile
    [BurstCompile]
    public partial struct ZombiePoolSpawnSystem : ISystem
    {
        private EntityQuery pooledZombiesQuery;
        private EntityQuery activeZombiesQuery;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ZombieSpawnSettings>();
            
            // Pooled zombiler
            pooledZombiesQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<ZombieTag>(),
                ComponentType.ReadWrite<ZombiePooled>()
            );
            
            // Aktif zombiler  
            activeZombiesQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<ZombieTag>(),
                ComponentType.ReadOnly<ZombieComponent>()
            );
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            
            var spawnManagerEntity = SystemAPI.GetSingletonEntity<ZombieSpawnSettings>();
            var settings = SystemAPI.GetComponentRW<ZombieSpawnSettings>(spawnManagerEntity);
            var spawnPoints = SystemAPI.GetBuffer<SpawnPointElement>(spawnManagerEntity);
            
            // Spawn zamanı kontrolü
            if (currentTime < settings.ValueRO.nextSpawnTime)
                return;
                
            // Aktif zombie sayısını kontrol et
            int activeCount = 0;
            foreach (var (zombieComp, pooled) in 
                     SystemAPI.Query<RefRO<ZombieComponent>, RefRO<ZombiePooled>>()
                     .WithAll<ZombieTag>())
            {
                if (zombieComp.ValueRO.isAlive && !pooled.ValueRO.isPooled)
                    activeCount++;
            }
            
            if (activeCount >= settings.ValueRO.maxZombieCount)
                return;
            
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            
            // Önce pool'dan al
            bool foundInPool = false;
            foreach (var (pooled, zombieComp, transform, entity) in 
                     SystemAPI.Query<RefRW<ZombiePooled>, RefRW<ZombieComponent>, RefRW<LocalTransform>>()
                     .WithEntityAccess())
            {
                if (pooled.ValueRO.isPooled)
                {
                    // Pool'dan çıkar ve aktif et
                    pooled.ValueRW.isPooled = false;
                    zombieComp.ValueRW.isAlive = true;
                    zombieComp.ValueRW.health = 100f;
                    
                    // Pozisyon ayarla
                    var random = new Random((uint)(currentTime * 1000 + 1));
                    var spawnIndex = random.NextInt(0, spawnPoints.Length);
                    var spawnPoint = spawnPoints[spawnIndex];
                    
                    transform.ValueRW.Position = spawnPoint.position;
                    transform.ValueRW.Rotation = spawnPoint.rotation;
                    
                    // Navigation'ı aktif et
                    if (SystemAPI.HasComponent<AgentBody>(entity))
                    {
                        var agent = SystemAPI.GetComponentRW<AgentBody>(entity);
                        agent.ValueRW.IsStopped = false;
                    }
                    
                    foundInPool = true;
                    break;
                }
            }
            
            // Pool'da yoksa yeni oluştur (sadece ilk başta)
            if (!foundInPool && settings.ValueRO.currentZombieCount < settings.ValueRO.maxZombieCount)
            {
                var random = new Random((uint)(currentTime * 1000 + 1));
                var spawnIndex = random.NextInt(0, spawnPoints.Length);
                var spawnPoint = spawnPoints[spawnIndex];
                
                var zombieEntity = commandBuffer.Instantiate(settings.ValueRO.zombiePrefab);
                
                commandBuffer.SetComponent(zombieEntity, LocalTransform.FromPositionRotation(
                    spawnPoint.position,
                    spawnPoint.rotation
                ));
                
                // Pool component'i ekle
                commandBuffer.AddComponent(zombieEntity, new ZombiePooled { isPooled = false });
                
                // Navigation için target component ekle
                commandBuffer.AddComponent(zombieEntity, new ZombieTarget
                {
                    targetEntity = Entity.Null,
                    lastKnownPosition = float3.zero,
                    destination = float3.zero
                });
                
                settings.ValueRW.currentZombieCount++;
            }
            
            // Spawn timer güncelle
            settings.ValueRW.nextSpawnTime = currentTime + settings.ValueRO.spawnInterval;
            
            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();
        }
    }
    
    // Zombie ölünce pool'a geri koy
    [BurstCompile]
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
            
            foreach (var (deathEvent, zombieComp, pooled, transform, entity) in 
                     SystemAPI.Query<RefRO<ZombieDeathEvent>, RefRW<ZombieComponent>, RefRW<ZombiePooled>, RefRW<LocalTransform>>()
                     .WithEntityAccess())
            {
                // Pool'a geri koy
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
                
                // Death event'i kaldır
                commandBuffer.RemoveComponent<ZombieDeathEvent>(entity);
            }
            
            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();
        }
    }*/