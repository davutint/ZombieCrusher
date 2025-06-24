using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("Owned Items")]
    private Dictionary<string, int> ownedCars = new(); // carId -> quantity (araçlar için genelde 1)
    private Dictionary<string, int> ownedUpgrades = new(); // upgradeName -> quantity

    /* ---------------- Mono ---------------- */
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /* ---------------- Cars ---------------- */
    public bool OwnsCarInInventory(string carId)
    {
        return ownedCars.ContainsKey(carId) && ownedCars[carId] > 0;
    }

    public void AddCarToInventory(string carId)
    {
        if (!ownedCars.ContainsKey(carId))
            ownedCars[carId] = 0;
        ownedCars[carId]++;
        Debug.Log($"Inventory: {carId} added to inventory");
    }

    public bool UseCarFromInventory(string carId)
    {
        if (!OwnsCarInInventory(carId)) return false;
        
        ownedCars[carId]--;
        if (ownedCars[carId] <= 0)
            ownedCars.Remove(carId);
        
        Debug.Log($"Inventory: {carId} used from inventory");
        return true;
    }

    /* ---------------- Upgrades ---------------- */
    public bool OwnsUpgradeInInventory(string upgradeName)
    {
        return ownedUpgrades.ContainsKey(upgradeName) && ownedUpgrades[upgradeName] > 0;
    }

    public int GetUpgradeQuantity(string upgradeName)
    {
        return ownedUpgrades.ContainsKey(upgradeName) ? ownedUpgrades[upgradeName] : 0;
    }

    public void AddUpgradeToInventory(string upgradeName, int quantity = 1)
    {
        if (!ownedUpgrades.ContainsKey(upgradeName))
            ownedUpgrades[upgradeName] = 0;
        ownedUpgrades[upgradeName] += quantity;
        Debug.Log($"Inventory: {upgradeName} x{quantity} added to inventory");
    }

    public bool UseUpgradeFromInventory(string upgradeName)
    {
        if (!OwnsUpgradeInInventory(upgradeName)) return false;
        
        ownedUpgrades[upgradeName]--;
        if (ownedUpgrades[upgradeName] <= 0)
            ownedUpgrades.Remove(upgradeName);
        
        Debug.Log($"Inventory: {upgradeName} used from inventory");
        return true;
    }

    /* ---------------- Data Access ---------------- */
    public Dictionary<string, int> GetOwnedCars() => new Dictionary<string, int>(ownedCars);
    public Dictionary<string, int> GetOwnedUpgrades() => new Dictionary<string, int>(ownedUpgrades);

    /* ---------------- Save/Load Integration ---------------- */
    public void LoadInventoryData(Dictionary<string, int> cars, Dictionary<string, int> upgrades)
    {
        ownedCars = cars ?? new Dictionary<string, int>();
        ownedUpgrades = upgrades ?? new Dictionary<string, int>();
        Debug.Log("Inventory data loaded");
    }
} 