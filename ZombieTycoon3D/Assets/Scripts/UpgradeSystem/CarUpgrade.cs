using UnityEngine;

// Araç parçalarının enum tanımlaması
public enum CarUpgradeSlot
{
    FrontBumper,
    RearBumper,
    LeftDoor,
    RightDoor,
    Roof
}

// Araç upgrade'lerinin ScriptableObject sınıfı
[CreateAssetMenu(fileName = "New Car Upgrade", menuName = "Vehicle/Car Upgrade")]
public class CarUpgrade : ScriptableObject
{
    [Header("Temel Bilgiler")]
    public string upgradeName;
    public GameObject upgradePrefab;
    public CarUpgradeSlot upgradeSlot;

    [Header("Performans Değişiklikleri")]
    [Tooltip("Aracın maksimum hızına eklenecek değer")]
    public float maxSpeedModifier;
    
    [Tooltip("Aracın hızlanmasına eklenecek değer")]
    public float accelerationModifier;
    
    [Tooltip("Aracın sağlamlığına eklenecek değer")]
    public float healthModifier;
} 