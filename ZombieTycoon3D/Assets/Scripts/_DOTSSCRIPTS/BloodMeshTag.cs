using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
// Kan mesh'i için component
public struct BloodMeshTag : IComponentData { }
    
// Mesh'in yaşam süresi
public struct BloodMeshLifetime : IComponentData
{
    public float spawnTime;
    public float lifetime;
}
    
// Spawn ayarları - singleton
public struct BloodMeshSpawnSettings : IComponentData
{
    public Entity bloodMeshPrefab;
    public int maxMeshCount;
    public int currentMeshCount;
    public float meshLifetime;
        
    // Fizik ayarları
    public float3 initialVelocityMin;
    public float3 initialVelocityMax;
    public float gravityScale;
    public float damping;
}
    
// Spawn request
public struct BloodMeshSpawnRequest : IComponentData
{
    public float3 position;
    public quaternion rotation;
    public float3 vehicleVelocity; // Aracın hızını da ekleyelim
}