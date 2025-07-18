using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using UnityEngine;
public class BloodMeshSpawnerAuthoring : MonoBehaviour
{
    [Header("Spawn Ayarları")]
    public GameObject bloodMeshPrefab;
    public int maxMeshCount = 100;
    public float meshLifetime = 5f;
        
    [Header("Fizik Ayarları")]
    public Vector3 initialVelocityMin = new Vector3(-2, 1, -2);
    public Vector3 initialVelocityMax = new Vector3(2, 5, 2);
    public float gravityScale = 1f;
    public float damping = 0.5f;
        
    class Baker : Baker<BloodMeshSpawnerAuthoring>
    {
        public override void Bake(BloodMeshSpawnerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
                
            var prefabEntity = GetEntity(authoring.bloodMeshPrefab, TransformUsageFlags.Dynamic);
                
            AddComponent(entity, new BloodMeshSpawnSettings
            {
                bloodMeshPrefab = prefabEntity,
                maxMeshCount = authoring.maxMeshCount,
                currentMeshCount = 0,
                meshLifetime = authoring.meshLifetime,
                initialVelocityMin = authoring.initialVelocityMin,
                initialVelocityMax = authoring.initialVelocityMax,
                gravityScale = authoring.gravityScale,
                damping = authoring.damping
            });
        }
    }
}