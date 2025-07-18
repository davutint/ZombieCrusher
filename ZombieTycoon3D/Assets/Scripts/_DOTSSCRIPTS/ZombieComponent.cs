using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
// Zombie'nin temel özelliklerini tutan component
public struct ZombieComponent : IComponentData
{
    public float moveSpeed;      // Hareket hızı
    public float health;         // Can değeri
    public bool isAlive;         // Yaşıyor mu?
}

// Bu tag component'i sadece zombie entity'lerini işaretlemek için
// Tag component'ler boş struct'lardır, sadece filtreleme için kullanılır
public struct ZombieTag : IComponentData { }

// Zombie'nin hangi spawn noktasından geldiğini takip eder
public struct ZombieSpawnData : IComponentData
{
    public int spawnPointIndex;
    public float spawnTime;
}

// Araç tag'i - Vehicle entity'lerini işaretler
public struct VehicleTag : IComponentData { }

// Spawn sistemi için singleton component
// Singleton component'ler sahne genelinde tek bir instance olur
public struct ZombieSpawnSettings : IComponentData
{
    public int maxZombieCount;           // Maksimum zombie sayısı
    public int currentZombieCount;       // Şu anki zombie sayısı
    public int killedZombieCount;        // Öldürülen zombie sayısı
    public float spawnInterval;          // Spawn aralığı (saniye)
    public float nextSpawnTime;          // Sonraki spawn zamanı
    public Entity zombiePrefab;          // Zombie entity prefab referansı
}

// Spawn noktalarını tutan buffer
// Dynamic buffer'lar entity'ye birden fazla aynı tipte veri eklememizi sağlar
[InternalBufferCapacity(10)]
public struct SpawnPointElement : IBufferElementData
{
    public float3 position;
    public quaternion rotation;
}

// UI güncellemesi için event component
// System'ler bu component'i görünce UI'ı günceller
public struct UIUpdateRequest : IComponentData
{
    public int totalZombies;
    public int killedZombies;
}