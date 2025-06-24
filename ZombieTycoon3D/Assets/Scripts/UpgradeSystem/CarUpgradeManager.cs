using System.Collections.Generic;
using UnityEngine;
using ArcadeVP;

public class CarUpgradeManager : MonoBehaviour
{
    [Header("Araç Referansları")]
    [SerializeField] private ArcadeVehicleController vehicleController;
    [SerializeField] private Player playerComponent;

    [Header("Upgrade Slotları")]
    [SerializeField] private Transform frontBumperSlot;
    [SerializeField] private Transform rearBumperSlot;
    [SerializeField] private Transform leftDoorSlot;
    [SerializeField] private Transform rightDoorSlot;
    [SerializeField] private Transform roofSlot;

    // Başlangıç değerlerini saklayacak değişkenler
    private float originalMaxSpeed;
    private float originalAcceleration;
    private float originalHealth;

    // Aktif upgrade objelerini tutan dictionary
    private Dictionary<CarUpgradeSlot, GameObject> activeUpgrades = new Dictionary<CarUpgradeSlot, GameObject>();
    
    // Upgrade verileri için dictionary (slot -> upgrade data)
    private Dictionary<CarUpgradeSlot, CarUpgrade> activeUpgradeData = new Dictionary<CarUpgradeSlot, CarUpgrade>();
    
    // Toplam modifiye değerleri
    private float totalMaxSpeedModifier = 0f;
    private float totalAccelerationModifier = 0f;
    private float totalHealthModifier = 0f;

    private void Awake()
    {
        // Araç kontrolcüsünün referansını kontrol et
        if (vehicleController == null)
        {
            vehicleController = GetComponent<ArcadeVehicleController>();
            
            if (vehicleController == null)
            {
                Debug.LogError("CarUpgradeManager: ArcadeVehicleController bulunamadı! Lütfen onu atayın veya aynı game object üzerine ekleyin.");
                enabled = false;
                return;
            }
        }

        // Player bileşenini kontrol et
        if (playerComponent == null)
        {
            playerComponent = GetComponent<Player>();
            
            if (playerComponent == null)
            {
                Debug.LogWarning("CarUpgradeManager: Player bileşeni bulunamadı! Health modifikatörleri devre dışı bırakılacak.");
            }
        }

        // Slot referanslarını kontrol et
        ValidateSlotReferences();
        
        // Başlangıç değerlerini kaydet
        SaveOriginalValues();
    }

    // Slot referanslarını doğrula
    private void ValidateSlotReferences()
    {
        if (frontBumperSlot == null)
            Debug.LogWarning("CarUpgradeManager: Front Bumper Slot atanmamış!");
            
        if (rearBumperSlot == null)
            Debug.LogWarning("CarUpgradeManager: Rear Bumper Slot atanmamış!");
            
        if (leftDoorSlot == null)
            Debug.LogWarning("CarUpgradeManager: Left Door Slot atanmamış!");
            
        if (rightDoorSlot == null)
            Debug.LogWarning("CarUpgradeManager: Right Door Slot atanmamış!");
            
        if (roofSlot == null)
            Debug.LogWarning("CarUpgradeManager: Roof Slot atanmamış!");
    }

    // Orijinal değerleri kaydet
    private void SaveOriginalValues()
    {
        originalMaxSpeed = vehicleController.MaxSpeed;
        originalAcceleration = vehicleController.accelaration;
        
        // Player varsa health değerini al
        if (playerComponent != null)
        {
            originalHealth = playerComponent.GetMaxHealth();
        }
    }

    // Belirli bir slot için transform referansını al
    private Transform GetSlotTransform(CarUpgradeSlot slot)
    {
        switch (slot)
        {
            case CarUpgradeSlot.FrontBumper:
                return frontBumperSlot;
            case CarUpgradeSlot.RearBumper:
                return rearBumperSlot;
            case CarUpgradeSlot.LeftDoor:
                return leftDoorSlot;
            case CarUpgradeSlot.RightDoor:
                return rightDoorSlot;
            case CarUpgradeSlot.Roof:
                return roofSlot;
            default:
                Debug.LogError($"CarUpgradeManager: {slot} için geçerli bir slot bulunamadı!");
                return null;
        }
    }

    // Upgrade uygula
    public void ApplyUpgrade(CarUpgrade upgrade)
    {
         ApplyUpgrade(upgrade, Vector3.zero, Quaternion.identity);
    }

    // Upgrade'i özel pozisyon ve rotasyon ile uygula
    public void ApplyUpgrade(CarUpgrade upgrade, Vector3 positionOffset, Quaternion rotationOffset)
    {
        if (upgrade == null)
        {
            Debug.LogError("CarUpgradeManager: Null upgrade uygulanamaz!");
            return;
        }

        // Slot pozisyonunu al
        Transform slotTransform = GetSlotTransform(upgrade.upgradeSlot);
        if (slotTransform == null)
        {
            Debug.LogError($"CarUpgradeManager: {upgrade.upgradeSlot} için geçerli bir slot bulunamadı!");
            return;
        }

        // Eğer bu slotta zaten bir upgrade varsa, önce onu kaldır
        RemoveUpgrade(upgrade.upgradeSlot);

        // Yeni upgrade'i instantiate et
        GameObject upgradeInstance = Instantiate(upgrade.upgradePrefab, slotTransform);
        upgradeInstance.transform.localPosition = positionOffset;
        upgradeInstance.transform.localRotation = rotationOffset;

        // Dictionary'lere ekle
        activeUpgrades[upgrade.upgradeSlot] = upgradeInstance;
        activeUpgradeData[upgrade.upgradeSlot] = upgrade;

        // Modifier değerlerini ekle
        totalMaxSpeedModifier += upgrade.maxSpeedModifier;
        totalAccelerationModifier += upgrade.accelerationModifier;
        totalHealthModifier += upgrade.healthModifier;

        // Araç değerlerini güncelle
        UpdateVehicleStats();
        
        Debug.Log($"CarUpgradeManager: {upgrade.upgradeName} başarıyla uygulandı! Slot: {upgrade.upgradeSlot}");
    }

    // Belirli bir slottaki upgrade'i kaldır
    public void RemoveUpgrade(CarUpgradeSlot slot)
    {
        if (activeUpgrades.TryGetValue(slot, out GameObject existingUpgrade))
        {
            // Slotta bulunan önceki upgrade verilerini al
            if (activeUpgradeData.TryGetValue(slot, out CarUpgrade existingUpgradeData))
            {
                // Modifier değerlerini çıkar
                totalMaxSpeedModifier -= existingUpgradeData.maxSpeedModifier;
                totalAccelerationModifier -= existingUpgradeData.accelerationModifier;
                totalHealthModifier -= existingUpgradeData.healthModifier;
                
                // Veri dictionary'sinden kaldır
                activeUpgradeData.Remove(slot);
            }

            // Upgrade objesini yok et
            Destroy(existingUpgrade);
            activeUpgrades.Remove(slot);
            
            // Araç değerlerini güncelle
            UpdateVehicleStats();
            
            Debug.Log($"CarUpgradeManager: {slot} slotundaki upgrade kaldırıldı.");
        }
    }

    // Tüm upgradeleri sıfırla
    public void ResetUpgrades()
    {
        // Tüm aktif upgrade objelerini yok et
        foreach (var upgrade in activeUpgrades.Values)
        {
            if (upgrade != null)
                Destroy(upgrade);
        }

        // Dictionary'leri temizle
        activeUpgrades.Clear();
        activeUpgradeData.Clear();

        // Modifier değerlerini sıfırla
        totalMaxSpeedModifier = 0f;
        totalAccelerationModifier = 0f;
        totalHealthModifier = 0f;

        // Araç değerlerini orijinal değerlerine döndür
        UpdateVehicleStats();
        
        Debug.Log("CarUpgradeManager: Tüm upgrade'ler sıfırlandı!");
    }

    // Araç istatistiklerini güncelle
    private void UpdateVehicleStats()
    {
        // Araç controller değerlerini modifiye et
        vehicleController.MaxSpeed = originalMaxSpeed + totalMaxSpeedModifier;
        vehicleController.accelaration = originalAcceleration + totalAccelerationModifier;
        
        // Player varsa health değerini güncelle
        if (playerComponent != null)
        {
            playerComponent.SetMaxHealth(originalHealth + totalHealthModifier);
        }
    }

    // Aktif upgradeleri getir
    public Dictionary<CarUpgradeSlot, GameObject> GetActiveUpgrades()
    {
        return activeUpgrades;
    }
    
    // Aktif upgrade verilerini getir
    public Dictionary<CarUpgradeSlot, CarUpgrade> GetActiveUpgradeData()
    {
        return activeUpgradeData;
    }

    // Toplam performans modifikatör değerlerini getir
    public (float maxSpeed, float acceleration, float health) GetTotalModifiers()
    {
        return (totalMaxSpeedModifier, totalAccelerationModifier, totalHealthModifier);
    }
    /// <summary>
    /// Kayıttan gelen upgradeId (upgradeName) listesine göre yeniden uygular.
    /// </summary>
    public void ApplySaved(Dictionary<CarUpgradeSlot,string> dict, IEnumerable<CarUpgrade> catalog)
    {
        ResetUpgrades();
        foreach (var kvp in dict)
        {
            foreach (var cu in catalog)
                if (cu.upgradeName == kvp.Value && cu.upgradeSlot == kvp.Key)
                    ApplyUpgrade(cu);
        }
    }

} 