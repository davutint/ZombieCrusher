# Araç Upgrade Sistemi

Bu sistem, ArcadeVehicleController üzerinde değişiklik yapmadan araç özelliklerini yükseltmek için kullanılır.

## Kullanım Adımları

1. **ScriptableObject Oluşturma:**
   - Project penceresinde sağ tıklayın
   - Create > Vehicle > Car Upgrade seçin
   - Upgrade adı, prefab ve slot tipini ayarlayın
   - Performans değiştiricilerini ayarlayın

2. **CarUpgradeManager Kurulumu:**
   - Araç GameObject'ine CarUpgradeManager component'ini ekleyin
   - ArcadeVehicleController referansını ayarlayın
   - Player referansını ayarlayın (health modifierlarının çalışması için)
   - Slot Transform'larını atayın

3. **Upgrade Uygulama:**
   - Kod içinde: `upgradeManager.ApplyUpgrade(upgradeObject);`
   - Özel pozisyon/rotasyon ile: `upgradeManager.ApplyUpgrade(upgradeObject, positionOffset, rotationOffset);`
   - Extension metodu ile: `upgradeObject.ApplyTo(upgradeManager);`
   - Extension metodu ile offset: `upgradeObject.ApplyTo(upgradeManager, positionOffset, rotationOffset);`

## Slot Transform'ları Oluşturma

Araç modelinizde aşağıdaki Transform'ları oluşturun ve CarUpgradeManager'a atayın:

- Front Bumper Slot
- Rear Bumper Slot
- Left Door Slot
- Right Door Slot
- Roof Slot

## Örnek Kod

```csharp
// CarUpgrade ScriptableObject'i uygulama (normal)
public void ApplyUpgrade(CarUpgrade upgrade)
{
    if (upgradeManager != null)
    {
        upgradeManager.ApplyUpgrade(upgrade);
    }
}

// CarUpgrade ScriptableObject'i uygulama (özel pozisyon/rotasyon ile)
public void ApplyCustomUpgrade(CarUpgrade upgrade)
{
    if (upgradeManager != null)
    {
        upgradeManager.ApplyUpgrade(upgrade, new Vector3(0, 0.5f, 0), Quaternion.Euler(0, 45, 0));
    }
}

// Extension metodu ile uygulama
public void ApplyUpgradeWithExtension(CarUpgrade upgrade)
{
    upgrade.ApplyTo(upgradeManager);
}

// Performansa göre filtreleme
public void ApplyHealthUpgrades()
{
    List<CarUpgrade> healthUpgrades = availableUpgrades.FilterByHealthIncrease();
    if (healthUpgrades.Count > 0)
    {
        healthUpgrades[0].ApplyTo(upgradeManager);
    }
}

// Tüm yükseltmeleri sıfırlama
public void ResetUpgrades()
{
    if (upgradeManager != null)
    {
        upgradeManager.ResetUpgrades();
    }
}
```

## Örnek ScriptableObject Ayarları

- **Güçlü Motor:**
  - Slot: FrontBumper
  - Max Speed Modifier: 10
  - Acceleration Modifier: 5
  - Health Modifier: 0

- **Zırh Kapısı:**
  - Slot: LeftDoor veya RightDoor
  - Max Speed Modifier: -2
  - Acceleration Modifier: -1
  - Health Modifier: 30

- **Aerodinamik Tavan:**
  - Slot: Roof
  - Max Speed Modifier: 5
  - Acceleration Modifier: 0
  - Health Modifier: 0

## Yardımcı Extension Metodlar

```csharp
// Upgrade'i uygun araca uygular
upgrade.ApplyTo(upgradeManager);

// Özel pozisyon/rotasyon ile uygulamak için
upgrade.ApplyTo(upgradeManager, new Vector3(0, 0.1f, 0), Quaternion.Euler(0, 90, 0));

// Belirli bir slota uygun mu kontrolü
bool isCompatible = upgrade.IsCompatibleWithSlot(CarUpgradeSlot.Roof);

// Belirli bir değeri artırıyor mu kontrolü
bool increasesSpeed = upgrade.IncreasesSpeed();
bool increasesAcceleration = upgrade.IncreasesAcceleration();
bool increasesHealth = upgrade.IncreasesHealth();

// Upgade listelerini filtreleme
List<CarUpgrade> roofUpgrades = allUpgrades.FilterBySlot(CarUpgradeSlot.Roof);
List<CarUpgrade> speedUpgrades = allUpgrades.FilterBySpeedIncrease();
List<CarUpgrade> accelerationUpgrades = allUpgrades.FilterByAccelerationIncrease();
List<CarUpgrade> healthUpgrades = allUpgrades.FilterByHealthIncrease();

// İki upgrade'i karşılaştırma (değer bazlı)
float comparisonResult = upgrade1.CompareOverallValue(upgrade2);
// Sonuç > 0 ise upgrade1 daha iyi
// Sonuç < 0 ise upgrade2 daha iyi
```

## Health Sistemi Entegrasyonu

Sistemimiz Player sınıfıyla entegre çalışır ve health modifierları araç sağlamlığını artırabilir. 
Bunun için Player sınıfında aşağıdaki metodları kullanın:

```csharp
// Araç maksimum sağlamlığını getir
public float GetMaxHealth();

// Araç maksimum sağlamlığını ayarla
public void SetMaxHealth(float newMaxHealth);

// Mevcut sağlamlık değerini getir
public float GetCurrentHealth();
```

## Notlar

- Sistem mevcut ArcadeVehicleController'a dokunmadan çalışır
- Aynı slota yeni bir yükseltme uygulandığında eski yükseltme otomatik olarak kaldırılır
- ResetUpgrades() metodu tüm yükseltmeleri kaldırır
- Özel pozisyon ve rotasyon offset değerleri kullanabilirsiniz
- Player sınıfıyla entegre edilerek health modifierlar aktif olarak kullanılabilir
- Çeşitli extension metodları sayesinde upgrade listeleri kolay filtrelenebilir 