using ProjectDawn.Navigation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using Random = Unity.Mathematics.Random;

// Spawn job - Burst ile multi-threaded
[BurstCompile]
public struct ZombieSpawnJob : IJobParallelFor
{
    public Entity Prototype;
    public EntityCommandBuffer.ParallelWriter Ecb;

    [ReadOnly] public NativeArray<float3> SpawnPositions;
    [ReadOnly] public NativeArray<quaternion> SpawnRotations;
    [ReadOnly] public NativeArray<Entity> PooledEntities; // Havuzdan gelenler

    public void Execute(int index)
    {
        Entity zombie;

        // Havuzda varsa onu kullan, yoksa yeni oluştur
        if (index < PooledEntities.Length)
        {
            zombie = PooledEntities[index];

            // Havuzdan çıkar
            Ecb.SetComponent(index, zombie, new ZombiePooled { isPooled = false });

            // Can ve durumunu yenile
            Ecb.SetComponent(index, zombie, new ZombieComponent
            {
                isAlive = true,
                health = 100f,
                moveSpeed = 3f
            });

            // Navigation'ı aktif et (AgentBody.IsStopped = false işlemi main thread'de yapılacak)
        }
        else
        {
            zombie = Ecb.Instantiate(index, Prototype);
            // Yeni doğanlara da Pool component ekle
            Ecb.AddComponent(index, zombie, new ZombiePooled { isPooled = false });
        }

        // Transform set et
        Ecb.SetComponent(index, zombie, new LocalTransform
        {
            Position = SpawnPositions[index],
            Rotation = SpawnRotations[index],
            Scale = 1f
        });

        // Spawn data
        Ecb.SetComponent(index, zombie, new ZombieSpawnData
        {
            spawnPointIndex = index % SpawnPositions.Length,
            spawnTime = 0f
        });

        // Navigation target - ARTIK SET EDIYORUZ (Bake'de eklendi)
        Ecb.SetComponent(index, zombie, new ZombieTarget
        {
            targetEntity = Entity.Null,
            lastKnownPosition = float3.zero,
            destination = float3.zero
        });
    }
}

// Unity Best Practices ile optimized spawn system
[BurstCompile]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct OptimizedZombieSpawnSystem : ISystem
{
    private Entity prototypeEntity;
    private bool prototypeCreated;
    private float nextBatchTime;
    private int batchSize;
    private float batchInterval;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ZombieSpawnSettings>();
        prototypeCreated = false;
        nextBatchTime = 0f;
        batchSize = 50;        // 20'den 50'ye çıkardım
        batchInterval = 0.5f;  // 2 saniyeden 0.5 saniyeye indirdim
    }

    public void OnUpdate(ref SystemState state)
    {
        var currentTime = (float)SystemAPI.Time.ElapsedTime;

        // Batch spawn zamanı kontrolü
        if (currentTime < nextBatchTime)
            return;

        // Spawn manager singleton
        var spawnManagerEntity = SystemAPI.GetSingletonEntity<ZombieSpawnSettings>();
        var settings = SystemAPI.GetComponent<ZombieSpawnSettings>(spawnManagerEntity);
        var spawnPoints = SystemAPI.GetBuffer<SpawnPointElement>(spawnManagerEntity);

        // Prototype oluştur (sadece ilk frame'de)
        if (!prototypeCreated)
        {
            CreatePrototype(ref state, settings.zombiePrefab);
            prototypeCreated = true;
            return; // İlk frame'de sadece prototype oluştur
        }

        // Max zombie kontrolü
        if (settings.currentZombieCount >= settings.maxZombieCount)
            return;

        // Spawn sayısını hesapla
        int spawnCount = math.min(
            batchSize,
            settings.maxZombieCount - settings.currentZombieCount
        );

        if (spawnCount <= 0 || spawnPoints.Length == 0)
            return;

        // Spawn pozisyonlarını hazırla
        var spawnPositions = new NativeArray<float3>(spawnCount, Allocator.TempJob);
        var spawnRotations = new NativeArray<quaternion>(spawnCount, Allocator.TempJob);

        uint seed = math.max(1, (uint)(currentTime * 1000));
        var random = new Random(seed);

        for (int i = 0; i < spawnCount; i++)
        {
            int spawnIndex = random.NextInt(0, spawnPoints.Length);
            var spawnPoint = spawnPoints[spawnIndex];

            // Biraz randomize et
            float3 randomOffset = random.NextFloat3(new float3(-1, 0, -1), new float3(1, 0, 1));
            spawnPositions[i] = spawnPoint.position + randomOffset;
            spawnRotations[i] = spawnPoint.rotation;
        }

        // EntityCommandBuffer al
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        // Havuzdaki zombileri topla
        var pooledEntitiesList = new NativeList<Entity>(spawnCount, Allocator.TempJob);

        // Query ile havuzdaki zombileri bul
        foreach (var (pooled, entity) in SystemAPI.Query<RefRO<ZombiePooled>>().WithEntityAccess())
        {
            if (pooled.ValueRO.isPooled)
            {
                pooledEntitiesList.Add(entity);
                if (pooledEntitiesList.Length >= spawnCount)
                    break;
            }
        }

        var pooledEntitiesArray = pooledEntitiesList.AsArray();

        // Parallel spawn job
        var spawnJob = new ZombieSpawnJob
        {
            Prototype = prototypeEntity,
            Ecb = ecb.AsParallelWriter(),
            SpawnPositions = spawnPositions,
            SpawnRotations = spawnRotations,
            PooledEntities = pooledEntitiesArray
        };

        // Job'ı schedule et ve bekle
        var handle = spawnJob.Schedule(spawnCount, math.max(1, spawnCount / 4));
        handle.Complete();

        // ECB'yi playback et
        ecb.Playback(state.EntityManager);

        // AgentBody IsStopped düzeltmesi
        foreach (var entity in pooledEntitiesArray)
        {
            if (state.EntityManager.HasComponent<AgentBody>(entity))
            {
                var agent = state.EntityManager.GetComponentData<AgentBody>(entity);
                agent.IsStopped = false;
                state.EntityManager.SetComponentData(entity, agent);
            }
        }

        // Cleanup
        spawnPositions.Dispose();
        spawnRotations.Dispose();
        pooledEntitiesList.Dispose();
        ecb.Dispose();

        // Component'leri TEKRAR al (structural change'den sonra)
        SystemAPI.SetComponent(spawnManagerEntity, new ZombieSpawnSettings
        {
            maxZombieCount = settings.maxZombieCount,
            currentZombieCount = settings.currentZombieCount + spawnCount,
            killedZombieCount = settings.killedZombieCount,
            spawnInterval = settings.spawnInterval,
            nextSpawnTime = settings.nextSpawnTime,
            zombiePrefab = settings.zombiePrefab
        });

        // UI güncelle
        SystemAPI.SetComponent(spawnManagerEntity, new UIUpdateRequest
        {
            totalZombies = settings.currentZombieCount + spawnCount,
            killedZombies = settings.killedZombieCount
        });

        nextBatchTime = currentTime + batchInterval;

        #if UNITY_EDITOR
        Debug.Log($"[Optimized] Spawned {spawnCount} zombies. Total: {settings.currentZombieCount + spawnCount}");
        #endif
    }

    private void CreatePrototype(ref SystemState state, Entity prefabEntity)
    {
        // Prefab'dan prototype oluştur
        prototypeEntity = state.EntityManager.Instantiate(prefabEntity);

        // Rendering component'leri kontrol et ve eksikse ekle
        if (!state.EntityManager.HasComponent<MaterialMeshInfo>(prototypeEntity))
        {
            // SpawnManager'dan mesh ve material al
            var spawnManagerEntity = SystemAPI.GetSingletonEntity<ZombieSpawnSettings>();

            if (state.EntityManager.HasComponent<RenderMesh>(spawnManagerEntity))
            {
                var renderMeshData = state.EntityManager.GetSharedComponentManaged<RenderMesh>(spawnManagerEntity);

                if (renderMeshData.mesh != null && renderMeshData.material != null)
                {
                    // RenderMeshDescription oluştur
                    var desc = new RenderMeshDescription(
                        shadowCastingMode: ShadowCastingMode.On,
                        receiveShadows: true);

                    // RenderMeshArray oluştur
                    var renderMeshArray = new RenderMeshArray(
                        new[] { renderMeshData.material },
                        new[] { renderMeshData.mesh });

                    // RenderMeshUtility ile component'leri ekle
                    RenderMeshUtility.AddComponents(
                        prototypeEntity,
                        state.EntityManager,
                        desc,
                        renderMeshArray,
                        MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));

                    // LocalToWorld component'i ekle (rendering için gerekli)
                    if (!state.EntityManager.HasComponent<LocalToWorld>(prototypeEntity))
                    {
                        state.EntityManager.AddComponentData(prototypeEntity, new LocalToWorld());
                    }

                    Debug.Log("RenderMeshUtility ile rendering component'leri eklendi!");
                }
            }
        }

        // Prototype'ı görünmez yap
        state.EntityManager.SetComponentData(prototypeEntity, new LocalTransform
        {
            Position = new float3(0, -1000, 0),
            Rotation = quaternion.identity,
            Scale = 1f
        });

        // Navigation'ı durdur
        if (state.EntityManager.HasComponent<AgentBody>(prototypeEntity))
        {
            var agent = state.EntityManager.GetComponentData<AgentBody>(prototypeEntity);
            agent.IsStopped = true;
            state.EntityManager.SetComponentData(prototypeEntity, agent);
        }
    }

    public void OnDestroy(ref SystemState state)
    {
        // Prototype'ı temizle
        if (prototypeCreated && state.EntityManager.Exists(prototypeEntity))
        {
            state.EntityManager.DestroyEntity(prototypeEntity);
        }
    }
}

// Performans monitör system
[BurstCompile]
public partial struct SpawnPerformanceMonitorSystem : ISystem
{
    private double lastFrameTime;
    private int frameCount;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ZombieSpawnSettings>();
        lastFrameTime = 0;
        frameCount = 0;
    }

    public void OnUpdate(ref SystemState state)
    {
        frameCount++;

        if (frameCount % 60 == 0) // Her 60 frame'de bir
        {
            var currentTime = SystemAPI.Time.ElapsedTime;
            var deltaTime = currentTime - lastFrameTime;

            if (deltaTime > 0)
            {
                var fps = 60.0 / deltaTime;

                #if UNITY_EDITOR
                var zombieCount = SystemAPI.GetSingleton<ZombieSpawnSettings>().currentZombieCount;
                Debug.Log($"[Performance] FPS: {fps:F1} | Zombies: {zombieCount}");
                #endif
            }

            lastFrameTime = currentTime;
        }
    }
}
