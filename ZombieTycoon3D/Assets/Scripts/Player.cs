using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private float impactForce;
    [SerializeField] private Rigidbody rb;
    [SerializeField, Min(0.1f)] private float impactPower = 1f;
    [SerializeField] private float minImpactSpeed = 10f; // Zombiyi yok etmek için gereken minimum hız

    [SerializeField, Min(0f)] private float damage = 0.5f;
    [SerializeField] private GameObject explosionEffectPrefab;
    private const float ExplosionEffectLifetime = 3f;
    private const int ExplosionEffectInitialPoolSize = 1;
    private const int ExplosionEffectMaximumPoolSize = 4;
    private DeathEffectPool effectPool;
    private VehicleImpactFeedback impactFeedback;
    
    // Oyuncunun maksimum canı
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;
    
    // Araç patladı mı kontrolü (birden fazla patlama tetiklenmesin diye)
    private bool isExploded = false;

    private Renderer[] vehicleRenderers;
    private Collider[] vehicleColliders;
    private bool[] initialRendererStates;
    private bool[] initialColliderStates;
    
   
    
    
    
    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (impactFeedback == null)
        {
            impactFeedback = GetComponent<VehicleImpactFeedback>();
        }

        if (effectPool == null)
        {
            effectPool = FindFirstObjectByType<DeathEffectPool>();
        }

        vehicleRenderers = GetComponentsInChildren<Renderer>(true);
        vehicleColliders = GetComponentsInChildren<Collider>(true);
        initialRendererStates = new bool[vehicleRenderers.Length];
        initialColliderStates = new bool[vehicleColliders.Length];

        for (int i = 0; i < vehicleRenderers.Length; i++)
        {
            initialRendererStates[i] = vehicleRenderers[i].enabled;
        }

        for (int i = 0; i < vehicleColliders.Length; i++)
        {
            initialColliderStates[i] = vehicleColliders[i].enabled;
        }

        currentHealth = maxHealth;
    }

    private void Start()
    {
        effectPool?.Register(
            explosionEffectPrefab,
            ExplosionEffectInitialPoolSize,
            ExplosionEffectMaximumPoolSize);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Ragdoll"))
        {
            float currentSpeed = rb.linearVelocity.magnitude;
            float effectiveMinimumImpactSpeed =
                minImpactSpeed / Mathf.Max(0.1f, impactPower);
            if (currentSpeed >= effectiveMinimumImpactSpeed)
            {
                Enemy enemy = other.gameObject.GetComponentInParent<Enemy>();
                if (enemy != null && enemy.TryDestroy())
                {
                    float speedStrength = Mathf.InverseLerp(
                        effectiveMinimumImpactSpeed,
                        effectiveMinimumImpactSpeed * 3.5f,
                        currentSpeed);
                    float attachmentStrength = Mathf.InverseLerp(
                        0.75f,
                        1.75f,
                        impactPower);
                    float feedbackStrength = Mathf.Clamp01(
                        0.2f
                        + speedStrength * 0.55f
                        + attachmentStrength * 0.25f);
                    Vector3 impactPosition =
                        other.ClosestPoint(transform.position);
                    impactFeedback?.PlayZombieImpact(
                        impactPosition,
                        rb.linearVelocity,
                        feedbackStrength);
                    TakeDamage(damage);
                }
            }
        }
    }

    // Hasar uygulama metodu
    private void TakeDamage(float damage)
    {
        if (isExploded)
            return;
        
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            ExplodeVehicle();
        }
    }

    // Aracın patlama efektini oynatıp, görsel olarak devre dışı bırakmasını sağlayan metot
    private void ExplodeVehicle()
    {
        if (isExploded)
            return;
        
        isExploded = true;

        if (explosionEffectPrefab != null)
        {
            bool playedFromPool = effectPool != null
                && effectPool.Play(
                    explosionEffectPrefab,
                    transform.position,
                    Quaternion.identity,
                    ExplosionEffectLifetime);
            if (!playedFromPool)
            {
                GameObject explosion = Instantiate(
                    explosionEffectPrefab,
                    transform.position,
                    Quaternion.identity);
                Destroy(explosion, ExplosionEffectLifetime);
            }
        }
        
        // Aracın görsel bileşenlerini devre dışı bırak (destroy edilmiyor, sadece görünmez yapılıyor)
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = false;
        }
        
        // Colliders'ı devre dışı bırak
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders)
        {
            c.enabled = false;
        }
        
        // Fizik hesaplamalarını sonlandırmak için rigidbody'yi kinematik hale getiriyoruz.
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        
        // Oyuncu ölüm eventini tetikle
        EventManager.OnPlayerDeath?.Invoke();
        
      
    }

    /// <summary>
    /// Araç için maksimum can değerini döndürür
    /// </summary>
    public float GetMaxHealth()
    {
        return maxHealth;
    }
    
    /// <summary>
    /// Araç için maksimum can değerini ayarlar ve mevcut canı oranına göre günceller
    /// </summary>
    public void SetMaxHealth(float newMaxHealth)
    {
        if (newMaxHealth <= 0)
        {
            Debug.LogWarning("Player: Maksimum can değeri 0 veya daha küçük olamaz!");
            newMaxHealth = 1f;
        }
        
        // Mevcut can yüzdesini hesapla
        float healthPercentage = currentHealth / maxHealth;
        
        // Yeni maksimum can değerini ayarla
        maxHealth = newMaxHealth;
        
        // Mevcut canı yeni maksimum değere göre güncelle
        currentHealth = maxHealth * healthPercentage;
        
    }
    
    /// <summary>
    /// Araç için mevcut can değerini döndürür
    /// </summary>
    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    public void ApplyVehicleStats(VehicleStats stats)
    {
        maxHealth = Mathf.Max(1f, stats.durability);
        impactPower = Mathf.Max(0.1f, stats.impactPower);
        currentHealth = maxHealth;
    }

    public void ResetForRun()
    {
        isExploded = false;
        currentHealth = maxHealth;

        for (int i = 0; i < vehicleRenderers.Length; i++)
        {
            if (vehicleRenderers[i] != null)
            {
                vehicleRenderers[i].enabled = initialRendererStates[i];
            }
        }

        for (int i = 0; i < vehicleColliders.Length; i++)
        {
            if (vehicleColliders[i] != null)
            {
                vehicleColliders[i].enabled = initialColliderStates[i];
            }
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
