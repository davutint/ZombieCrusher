using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using UnityEngine;
 // Blood mesh prefab authoring
    public class BloodMeshAuthoring : MonoBehaviour
    {
        [Header("Mesh Ayarları")]
        public float lifetime = 5f;
        
        class Baker : Baker<BloodMeshAuthoring>
        {
            public override void Bake(BloodMeshAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                
                // Tag ve lifetime
                AddComponent<BloodMeshTag>(entity);
                AddComponent(entity, new BloodMeshLifetime
                {
                    spawnTime = 0f,
                    lifetime = authoring.lifetime
                });
                
                // Fizik component'leri (Physics Shape ve Physics Body component'leri 
                // GameObject'te zaten varsa otomatik dönüşür)
            }
        }
    }
    
   