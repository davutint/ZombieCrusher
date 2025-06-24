using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Araç upgrade sistemi için yardımcı extension metodlar
/// </summary>
public static class CarUpgradeExtensions
{
    /// <summary>
    /// Araç upgrade'ini belirli bir araç üzerine uygular
    /// </summary>
    /// <param name="carUpgrade">Uygulanacak upgrade</param>
    /// <param name="upgradeManager">Hedef araç upgrade manager'ı</param>
    public static void ApplyTo(this CarUpgrade carUpgrade, CarUpgradeManager upgradeManager)
    {
        if (upgradeManager == null)
        {
            Debug.LogError("CarUpgradeExtensions: Upgrade uygulanacak araç bulunamadı!");
            return;
        }

        upgradeManager.ApplyUpgrade(carUpgrade);
    }
    
    /// <summary>
    /// Araç upgrade'ini belirli bir araç üzerine özel pozisyon ve rotasyon ile uygular
    /// </summary>
    /// <param name="carUpgrade">Uygulanacak upgrade</param>
    /// <param name="upgradeManager">Hedef araç upgrade manager'ı</param>
    /// <param name="positionOffset">Pozisyon offset değeri</param>
    /// <param name="rotationOffset">Rotasyon offset değeri</param>
    public static void ApplyTo(this CarUpgrade carUpgrade, CarUpgradeManager upgradeManager, Vector3 positionOffset, Quaternion rotationOffset)
    {
        if (upgradeManager == null)
        {
            Debug.LogError("CarUpgradeExtensions: Upgrade uygulanacak araç bulunamadı!");
            return;
        }

        upgradeManager.ApplyUpgrade(carUpgrade, positionOffset, rotationOffset);
    }
    
    /// <summary>
    /// Belirlenen bir slot için uygun bir upgrade mü diye kontrol eder
    /// </summary>
    /// <param name="carUpgrade">Kontrol edilecek upgrade</param>
    /// <param name="slot">Hedef slot</param>
    /// <returns>Slot uyumlu ise true</returns>
    public static bool IsCompatibleWithSlot(this CarUpgrade carUpgrade, CarUpgradeSlot slot)
    {
        return carUpgrade.upgradeSlot == slot;
    }
    
    /// <summary>
    /// Belirli bir performans değerini artırıyor mu kontrol eder
    /// </summary>
    /// <param name="carUpgrade">Kontrol edilecek upgrade</param>
    /// <returns>Hız artırıcı ise true</returns>
    public static bool IncreasesSpeed(this CarUpgrade carUpgrade)
    {
        return carUpgrade.maxSpeedModifier > 0;
    }
    
    /// <summary>
    /// Belirli bir performans değerini artırıyor mu kontrol eder
    /// </summary>
    /// <param name="carUpgrade">Kontrol edilecek upgrade</param>
    /// <returns>Hızlanma artırıcı ise true</returns>
    public static bool IncreasesAcceleration(this CarUpgrade carUpgrade)
    {
        return carUpgrade.accelerationModifier > 0;
    }
    
    /// <summary>
    /// Belirli bir performans değerini artırıyor mu kontrol eder
    /// </summary>
    /// <param name="carUpgrade">Kontrol edilecek upgrade</param>
    /// <returns>Sağlamlık artırıcı ise true</returns>
    public static bool IncreasesHealth(this CarUpgrade carUpgrade)
    {
        return carUpgrade.healthModifier > 0;
    }
    
    /// <summary>
    /// Verilen upgrade listesinden belirli bir slota uygun olanları filtreler
    /// </summary>
    /// <param name="upgrades">Upgrade listesi</param>
    /// <param name="slot">Filtrelenecek slot türü</param>
    /// <returns>Filtrelenmiş upgrade listesi</returns>
    public static List<CarUpgrade> FilterBySlot(this IEnumerable<CarUpgrade> upgrades, CarUpgradeSlot slot)
    {
        List<CarUpgrade> result = new List<CarUpgrade>();
        
        foreach (var upgrade in upgrades)
        {
            if (upgrade.IsCompatibleWithSlot(slot))
            {
                result.Add(upgrade);
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Verilen upgrade listesinden hız artıran yükseltmeleri filtreler
    /// </summary>
    /// <param name="upgrades">Upgrade listesi</param>
    /// <returns>Filtrelenmiş upgrade listesi</returns>
    public static List<CarUpgrade> FilterBySpeedIncrease(this IEnumerable<CarUpgrade> upgrades)
    {
        List<CarUpgrade> result = new List<CarUpgrade>();
        
        foreach (var upgrade in upgrades)
        {
            if (upgrade.IncreasesSpeed())
            {
                result.Add(upgrade);
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Verilen upgrade listesinden hızlanma artıran yükseltmeleri filtreler
    /// </summary>
    /// <param name="upgrades">Upgrade listesi</param>
    /// <returns>Filtrelenmiş upgrade listesi</returns>
    public static List<CarUpgrade> FilterByAccelerationIncrease(this IEnumerable<CarUpgrade> upgrades)
    {
        List<CarUpgrade> result = new List<CarUpgrade>();
        
        foreach (var upgrade in upgrades)
        {
            if (upgrade.IncreasesAcceleration())
            {
                result.Add(upgrade);
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Verilen upgrade listesinden sağlamlık artıran yükseltmeleri filtreler
    /// </summary>
    /// <param name="upgrades">Upgrade listesi</param>
    /// <returns>Filtrelenmiş upgrade listesi</returns>
    public static List<CarUpgrade> FilterByHealthIncrease(this IEnumerable<CarUpgrade> upgrades)
    {
        List<CarUpgrade> result = new List<CarUpgrade>();
        
        foreach (var upgrade in upgrades)
        {
            if (upgrade.IncreasesHealth())
            {
                result.Add(upgrade);
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Upgrade değerlerini karşılaştıran yardımcı metod
    /// </summary>
    /// <param name="carUpgrade">İlk upgrade</param>
    /// <param name="otherUpgrade">Karşılaştırılacak upgrade</param>
    /// <returns>İki upgrade'in toplam değerleri karşılaştırması (pozitif ise ilk upgrade daha iyi)</returns>
    public static float CompareOverallValue(this CarUpgrade carUpgrade, CarUpgrade otherUpgrade)
    {
        float thisValue = carUpgrade.maxSpeedModifier + carUpgrade.accelerationModifier + carUpgrade.healthModifier;
        float otherValue = otherUpgrade.maxSpeedModifier + otherUpgrade.accelerationModifier + otherUpgrade.healthModifier;
        
        return thisValue - otherValue;
    }
} 