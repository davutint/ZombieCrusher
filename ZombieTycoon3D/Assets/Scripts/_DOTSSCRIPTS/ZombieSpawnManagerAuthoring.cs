using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

// Spawn Manager için Authoring
// Spawn Manager için Authoring
    public class ZombieSpawnManagerAuthoring : MonoBehaviour
    {
        [Header("Spawn Ayarları")]
        public GameObject zombiePrefab;      // Zombie prefab'i
        public int maxZombieCount = 100;     // Maksimum zombie sayısı
        public float spawnInterval = 1f;     // Her spawn arası süre

        [Header("Spawn Noktaları")]
        public Transform[] spawnPoints;      // Spawn noktaları
        
        [Header("Rendering (Opsiyonel)")]
        public Mesh zombieMesh;              // Eğer runtime'da mesh gerekirse
        public Material zombieMaterial;      // Eğer runtime'da material gerekirse

        class Baker : Baker<ZombieSpawnManagerAuthoring>
        {
            public override void Bake(ZombieSpawnManagerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                // Zombie prefab'ini Entity'ye çevir
                var zombiePrefabEntity = GetEntity(authoring.zombiePrefab, TransformUsageFlags.Dynamic);

                // Spawn settings component'i ekle
                AddComponent(entity, new ZombieSpawnSettings
                {
                    maxZombieCount = authoring.maxZombieCount,
                    currentZombieCount = 0,
                    killedZombieCount = 0,
                    spawnInterval = authoring.spawnInterval,
                    nextSpawnTime = 0f,
                    zombiePrefab = zombiePrefabEntity
                });

                // Spawn noktalarını buffer'a ekle
                var buffer = AddBuffer<SpawnPointElement>(entity);
                foreach (var spawnPoint in authoring.spawnPoints)
                {
                    if (spawnPoint != null)
                    {
                        buffer.Add(new SpawnPointElement
                        {
                            position = spawnPoint.position,
                            rotation = spawnPoint.rotation
                        });
                    }
                }

                // UI update request component'i ekle
                AddComponent(entity, new UIUpdateRequest
                {
                    totalZombies = 0,
                    killedZombies = 0
                });
                
                // Mesh ve Material referanslarını ekle (runtime spawn için)
                if (authoring.zombieMesh != null && authoring.zombieMaterial != null)
                {
                    AddSharedComponentManaged(entity, new RenderMesh
                    {
                        mesh = authoring.zombieMesh,
                        material = authoring.zombieMaterial
                    });
                }
            }
        }
    }