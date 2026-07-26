using System;
using DestroyIt;
using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{
    [SerializeField] private float impactForce;
    [SerializeField] private Rigidbody rb;
    [SerializeField, Min(0.1f)] private float impactPower = 1f;
    [SerializeField] private float minImpactSpeed = 10f; // Zombiyi yok etmek için gereken minimum hız

    [SerializeField] private int damage;
    // Patlama efekti için prefab (Inspector'den atanmalı)
    [SerializeField] private GameObject explosionEffectPrefab;
    
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Ragdoll"))
        {
            float currentSpeed = rb.linearVelocity.magnitude;
            Debug.Log("Çarpma Hızı: " + currentSpeed);
            
            float effectiveMinimumImpactSpeed =
                minImpactSpeed / Mathf.Max(0.1f, impactPower);
            if (currentSpeed >= effectiveMinimumImpactSpeed)
            {
                Debug.Log("Minimum hız aşıldı! Zombie yok ediliyor...");
                Enemy enemy = other.gameObject.GetComponentInParent<Enemy>();
                if (enemy != null)
                {
                    enemy.DestroyIt();
                    TakeDamage(damage);
                }
            }
            else
            {
                Debug.Log("Çarpma hızı yeterli değil. Minimum gerekli hız: " + minImpactSpeed);
            }
        }
    }

    // Hasar uygulama metodu
    private void TakeDamage(float damage)
    {
        if (isExploded)
            return;
        
        currentHealth -= damage;
       //update ui
        Debug.Log("Alınan Hasar: " + damage + " - Kalan Can: " + currentHealth);

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
        Debug.Log("Araç Patladı!");

        // Patlama efektini spawn et (Prefab üzerinden)
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
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

    private int CalculateEarnedCoins()
    {
        // Burada oyun süresine, öldürülen zombie sayısına vs. göre coin hesaplaması yapabilirsiniz
        return 100; // Şimdilik sabit bir değer
    }

  

    // VehicleStatus'u döndüren property

    // Upgrade sistemi için gereken metodlar
    
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
        
        // UI'ı güncelle
        
        
        Debug.Log($"Player: Maksimum can değeri {maxHealth} olarak güncellendi. Mevcut can: {currentHealth}");
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
