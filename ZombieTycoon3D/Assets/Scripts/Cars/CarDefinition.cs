using UnityEngine;

[CreateAssetMenu(fileName = "New Car", menuName = "Vehicle/Car Definition")]
public class CarDefinition : ScriptableObject
{
    [Header("IDs – değişmez")]
    public string  carId;              // benzersiz: "Jeep01"

    [Header("Mağaza Bilgisi")]
    public string  displayName;
    public Sprite  carIcon;            // UI'da gösterilecek icon
    public int     price;

    [Header("Prefab & Base Stats")]
    public GameObject carPrefab;       // Her araba aynı şablonu da kullanabilir
    public float  baseMaxSpeed;
    public float  baseAcceleration;
    public float  baseHealth;
}