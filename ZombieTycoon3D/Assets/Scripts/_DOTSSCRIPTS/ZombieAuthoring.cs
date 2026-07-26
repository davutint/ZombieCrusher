using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Rendering;
using UnityEngine.Rendering; // Bu satır önemli!


  
// Baker'lar bu GameObject'leri Entity'lere dönüştürür
public class ZombieAuthoring : MonoBehaviour
{
    
    [Header("Uzuv Referansları")]
    public GameObject leftArm;
    public GameObject rightArm;
    public GameObject leftLeg;
    public GameObject rightLeg;
    
    [Header("Animasyon Ayarları")]
    [Range(1f, 20f)]
    public float animationSpeed = 8f;
    
    [Range(0.1f, 1.5f)]
    public float limbSwingAmount = 0.5f;
    
    [Tooltip("Spawn olur olmaz koşmaya başlasın mı?")]
    public bool startRunningOnSpawn = true;
    [Header("Zombie Ayarları")]
    public float moveSpeed = 3f;
    public float health = 100f;

    // Baker class'ı GameObject'ten Entity'ye dönüşümü yapar
    // Unity otomatik olarak bu Baker'ı bulur ve kullanır
    // Inspector'de uzuvların atanıp atanmadığını kontrol etmek için
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        
        bool allAssigned = leftArm && rightArm && leftLeg && rightLeg;
        if (!allAssigned)
        {
            Debug.LogWarning($"[{gameObject.name}] Bazı uzuvlar atanmamış! " +
                             $"Sol Kol: {leftArm != null}, Sağ Kol: {rightArm != null}, " +
                             $"Sol Bacak: {leftLeg != null}, Sağ Bacak: {rightLeg != null}");
        }
    }
    class Baker : Baker<ZombieAuthoring>
    {
        public override void Bake(ZombieAuthoring authoring)
        {
            // GetEntity ile bu GameObject için Entity referansı alıyoruz
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Entity'ye component'leri ekliyoruz
            AddComponent(entity, new ZombieComponent
            {
                moveSpeed = authoring.moveSpeed,
                health = authoring.health,
                isAlive = true
            });

            // Tag component ekle
            AddComponent<ZombieTag>(entity);

            // Spawn data - başlangıç değerleri
            AddComponent(entity, new ZombieSpawnData
            {
                spawnPointIndex = -1,
                spawnTime = 0f
            });
            
            // Koşma animasyon component'ini ekle
            AddComponent(entity, new RunningAnimation
            {
                animationSpeed = authoring.animationSpeed,
                limbSwingAmount = authoring.limbSwingAmount,
                currentTime = 0f,
                isRunning = authoring.startRunningOnSpawn
            });
            
            // Uzuv referanslarını entity'lere dönüştür ve ekle
            var limbRefs = new LimbReferences
            {
                leftArm = authoring.leftArm ? GetEntity(authoring.leftArm, TransformUsageFlags.Dynamic) : Entity.Null,
                rightArm = authoring.rightArm ? GetEntity(authoring.rightArm, TransformUsageFlags.Dynamic) : Entity.Null,
                leftLeg = authoring.leftLeg ? GetEntity(authoring.leftLeg, TransformUsageFlags.Dynamic) : Entity.Null,
                rightLeg = authoring.rightLeg ? GetEntity(authoring.rightLeg, TransformUsageFlags.Dynamic) : Entity.Null
            };
            
            AddComponent(entity, limbRefs);
            
            Debug.Log($"[ZombieBaker] Uzuv referansları atandı - " +
                      $"Sol Kol: {limbRefs.leftArm != Entity.Null}, " +
                      $"Sağ Kol: {limbRefs.rightArm != Entity.Null}, " +
                      $"Sol Bacak: {limbRefs.leftLeg != Entity.Null}, " +
                      $"Sağ Bacak: {limbRefs.rightLeg != Entity.Null}");
            
            // BakeLimbOriginalRotation KALDIRILDI - InitializeLimbRotationsSystem hallediyor

            // ZombieTarget component'i ekle (Runtime'da eklememek için)
            AddComponent(entity, new ZombieTarget
            {
                targetEntity = Entity.Null,
                lastKnownPosition = float3.zero,
                destination = float3.zero
            });
        }
    }
}
