using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
 // GameObject particle pool manager
    /*public class ParticlePoolManager : MonoBehaviour
    {
        [Header("Particle Ayarları")]
        public GameObject bloodParticlePrefab;  // Kan efekti prefab'i
        public int poolSize = 50;               // Pool boyutu
        public float particleDuration = 2f;     // Particle süresi

        private Queue<GameObject> particlePool;
        private EntityManager entityManager;
        private EntityQuery particleRequestQuery;

        void Start()
        {
            // Pool'u oluştur
            InitializePool();
            
            // Entity Manager'ı al
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            
            // Particle spawn request'lerini sorgula
            particleRequestQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<ParticleSpawnRequest>()
            );
        }

        void InitializePool()
        {
            particlePool = new Queue<GameObject>();
            
            for (int i = 0; i < poolSize; i++)
            {
                GameObject particle = Instantiate(bloodParticlePrefab);
                particle.SetActive(false);
                particlePool.Enqueue(particle);
            }
        }

        void Update()
        {
            Debug.Log($"[POOL MANAGER] Requests: {particleRequestQuery.CalculateEntityCount()}");

            // Particle spawn request'leri kontrol et
            if (!particleRequestQuery.IsEmpty)
            {
                var requests = particleRequestQuery.ToComponentDataArray<ParticleSpawnRequest>(Allocator.Temp);
                var entities = particleRequestQuery.ToEntityArray(Allocator.Temp);
                
                for (int i = 0; i < requests.Length; i++)
                {
                    Debug.Log($"[POOL] Spawning particle for request at {requests[i].position}");

                    SpawnParticle(requests[i].position, requests[i].rotation);
                    
                    // Request entity'sini sil
                    entityManager.DestroyEntity(entities[i]);
                }
                
                requests.Dispose();
                entities.Dispose();
            }
        }

        void SpawnParticle(float3 position, quaternion rotation)
        {
            if (particlePool.Count > 0)
            {
                GameObject particle = particlePool.Dequeue();
                Debug.Log($"[POOL] Spawning particle at {position}");

                particle.transform.position = position;
                particle.transform.rotation = rotation;
                particle.SetActive(true);
                
                // Particle'ı geri pool'a döndür
                StartCoroutine(ReturnToPool(particle, particleDuration));
            }
            else
            {
                Debug.LogWarning("[POOL] Pool empty!");
            }
        }

        System.Collections.IEnumerator ReturnToPool(GameObject particle, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            particle.SetActive(false);
            particlePool.Enqueue(particle);
        }

        // Singleton erişim
        private static ParticlePoolManager instance;
        public static ParticlePoolManager Instance
        {
            get
            {
                if (instance == null)
                    instance = FindObjectOfType<ParticlePoolManager>();
                return instance;
            }
        }
    }*/

  