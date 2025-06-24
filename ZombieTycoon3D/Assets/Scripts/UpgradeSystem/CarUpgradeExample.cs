using System;
using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class CarUpgradeExample : MonoBehaviour
{
    [SerializeField] private CarUpgradeManager upgradeManager;
    
    [Header("Test Upgrade'leri")]
    [SerializeField] private CarUpgrade[] availableUpgrades;
    
    [Header("Özel Pozisyon/Rotasyon")]
    [SerializeField] private Vector3 customPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 customRotationEuler = Vector3.zero;

    private void Start()
    {
        Invoke(nameof(ApplyRandomUpgrade),10);
    }

    // UI düğmeleri için gerekli metod
    public void ApplyRandomUpgrade()
    {
        if (availableUpgrades == null || availableUpgrades.Length == 0)
        {
            Debug.LogWarning("CarUpgradeExample: Uygulanacak upgrade bulunamadı!");
            return;
        }
        
        // Rastgele bir upgrade seç
        CarUpgrade randomUpgrade = availableUpgrades[Random.Range(0, availableUpgrades.Length)];
        
        // Upgrade'i extension metodu ile uygula
        randomUpgrade.ApplyTo(upgradeManager);
    }
    
    // Belirli bir slotta upgrade uygula
    public void ApplyUpgradeToSlot(CarUpgradeSlot slot)
    {
        if (availableUpgrades == null || availableUpgrades.Length == 0)
        {
            Debug.LogWarning("CarUpgradeExample: Uygulanacak upgrade bulunamadı!");
            return;
        }
        
        // Extension metodunu kullanarak slot bazlı filtreleme
        List<CarUpgrade> compatibleUpgrades = System.Array.AsReadOnly(availableUpgrades).FilterBySlot(slot);
        
        if (compatibleUpgrades.Count == 0)
        {
            Debug.LogWarning($"CarUpgradeExample: {slot} slotu için uygun upgrade bulunamadı!");
            return;
        }
        
        // Uygun upgrade'lerden rastgele birini seç
        CarUpgrade selectedUpgrade = compatibleUpgrades[Random.Range(0, compatibleUpgrades.Count)];
        
        // Upgrade'i özel pozisyon ve rotasyon ile uygula
        selectedUpgrade.ApplyTo(upgradeManager, customPositionOffset, Quaternion.Euler(customRotationEuler));
    }
    
    // Belirli bir performans tipine göre upgrade uygula
    public void ApplySpeedUpgrade()
    {
        if (availableUpgrades == null || availableUpgrades.Length == 0)
        {
            Debug.LogWarning("CarUpgradeExample: Uygulanacak upgrade bulunamadı!");
            return;
        }
        
        // Extension metodunu kullanarak hız artışı bazlı filtreleme
        List<CarUpgrade> speedUpgrades = System.Array.AsReadOnly(availableUpgrades).FilterBySpeedIncrease();
        
        if (speedUpgrades.Count == 0)
        {
            Debug.LogWarning("CarUpgradeExample: Hız artıran upgrade bulunamadı!");
            return;
        }
        
        // Hız artıran upgradeleri uygula
        speedUpgrades[Random.Range(0, speedUpgrades.Count)].ApplyTo(upgradeManager);
    }
    
    // Belirli bir performans tipine göre upgrade uygula
    public void ApplyAccelerationUpgrade()
    {
        if (availableUpgrades == null || availableUpgrades.Length == 0)
        {
            Debug.LogWarning("CarUpgradeExample: Uygulanacak upgrade bulunamadı!");
            return;
        }
        
        // Extension metodunu kullanarak hızlanma artışı bazlı filtreleme
        List<CarUpgrade> accelerationUpgrades = System.Array.AsReadOnly(availableUpgrades).FilterByAccelerationIncrease();
        
        if (accelerationUpgrades.Count == 0)
        {
            Debug.LogWarning("CarUpgradeExample: Hızlanma artıran upgrade bulunamadı!");
            return;
        }
        
        // Hızlanma artıran upgradeleri uygula
        accelerationUpgrades[Random.Range(0, accelerationUpgrades.Count)].ApplyTo(upgradeManager);
    }
    
    // Belirli bir performans tipine göre upgrade uygula
    public void ApplyHealthUpgrade()
    {
        if (availableUpgrades == null || availableUpgrades.Length == 0)
        {
            Debug.LogWarning("CarUpgradeExample: Uygulanacak upgrade bulunamadı!");
            return;
        }
        
        // Extension metodunu kullanarak sağlamlık artışı bazlı filtreleme
        List<CarUpgrade> healthUpgrades = System.Array.AsReadOnly(availableUpgrades).FilterByHealthIncrease();
        
        if (healthUpgrades.Count == 0)
        {
            Debug.LogWarning("CarUpgradeExample: Sağlamlık artıran upgrade bulunamadı!");
            return;
        }
        
        // Sağlamlık artıran upgradeleri uygula
        healthUpgrades[Random.Range(0, healthUpgrades.Count)].ApplyTo(upgradeManager);
    }
    
    // En iyi performans veren upgrade'i uygula
    public void ApplyBestUpgrade()
    {
        if (availableUpgrades == null || availableUpgrades.Length == 0)
        {
            Debug.LogWarning("CarUpgradeExample: Uygulanacak upgrade bulunamadı!");
            return;
        }
        
        // En iyi upgrade'i bul
        CarUpgrade bestUpgrade = availableUpgrades[0];
        
        for (int i = 1; i < availableUpgrades.Length; i++)
        {
            if (bestUpgrade.CompareOverallValue(availableUpgrades[i]) < 0)
            {
                bestUpgrade = availableUpgrades[i];
            }
        }
        
        // En iyi upgrade'i uygula
        bestUpgrade.ApplyTo(upgradeManager);
        Debug.Log($"CarUpgradeExample: En iyi upgrade uygulandı: {bestUpgrade.upgradeName}");
    }
    
    // Tüm upgrade'leri sıfırla
    public void ResetAllUpgrades()
    {
        upgradeManager.ResetUpgrades();
    }
    
    // Mevcut upgrade verilerini loglama
    public void LogCurrentUpgrades()
    {
        var activeUpgrades = upgradeManager.GetActiveUpgrades();
        
        Debug.Log($"Aktif Upgrade Sayısı: {activeUpgrades.Count}");
        
        foreach (var kvp in activeUpgrades)
        {
            Debug.Log($"Slot: {kvp.Key}, Obje: {kvp.Value.name}");
        }
        
        var modifiers = upgradeManager.GetTotalModifiers();
        Debug.Log($"Toplam Modifierlar: MaxSpeed: {modifiers.maxSpeed}, Acceleration: {modifiers.acceleration}, Health: {modifiers.health}");
    }
} 