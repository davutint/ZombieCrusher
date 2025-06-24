using System;
using System.Collections.Generic;
using UnityEngine;

public static class SaveSystem
{
    private const string KEY = "SAVE_DATA";

    /* ---------- Veri Modeli ---------- */

    [Serializable] private class SaveData
    {
        public int                         coins;
        public string                      selectedCarId;
        public List<CarRecord>             cars = new();   // sahip olunan ve equipped arabalar (garage)
        public InventoryData               inventory = new(); // satın alınan ama equipped olmayan itemlar
    }

    [Serializable] private class InventoryData
    {
        public List<InventoryCarItem>      ownedCars = new();
        public List<InventoryUpgradeItem>  ownedUpgrades = new();
    }

    [Serializable] private class InventoryCarItem
    {
        public string carId;
        public int quantity;
    }

    [Serializable] private class InventoryUpgradeItem
    {
        public string upgradeName;
        public int quantity;
    }

    [Serializable] private class CarRecord
    {
        public string                      carId;
        public List<EquippedSlot>          equipped = new();
    }

    [Serializable] private struct EquippedSlot
    {
        public string slot;       // "FrontBumper"
        public string upgradeId;  // "SpikedRam"
    }

    /* ---------- Public API ---------- */

    public static void Save(int coins,
                            string selectedCarId,
                            Dictionary<string,Dictionary<CarUpgradeSlot,CarUpgrade>> garage,
                            Dictionary<string, int> inventoryCars,
                            Dictionary<string, int> inventoryUpgrades)
    {
        var data = new SaveData { coins = coins, selectedCarId = selectedCarId };

        // Garage (equipped items) kaydet
        foreach (var car in garage)
        {
            var rec = new CarRecord { carId = car.Key };
            foreach (var kvp in car.Value)
                rec.equipped.Add(new EquippedSlot
                {
                    slot      = kvp.Key.ToString(),
                    upgradeId = kvp.Value.upgradeName
                });
            data.cars.Add(rec);
        }

        // Inventory kaydet
        foreach (var car in inventoryCars)
        {
            data.inventory.ownedCars.Add(new InventoryCarItem 
            { 
                carId = car.Key, 
                quantity = car.Value 
            });
        }

        foreach (var upgrade in inventoryUpgrades)
        {
            data.inventory.ownedUpgrades.Add(new InventoryUpgradeItem 
            { 
                upgradeName = upgrade.Key, 
                quantity = upgrade.Value 
            });
        }

        PlayerPrefs.SetString(KEY, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
        Debug.Log("Game saved successfully!");
    }

    public static bool TryLoad(out int coins,
                               out string selectedCarId,
                               out Dictionary<string,Dictionary<CarUpgradeSlot,string>> garage,
                               out Dictionary<string, int> inventoryCars,
                               out Dictionary<string, int> inventoryUpgrades)
    {
        coins              = 0;
        selectedCarId      = null;
        garage             = new();
        inventoryCars      = new();
        inventoryUpgrades  = new();

        if (!PlayerPrefs.HasKey(KEY)) return false;

        try
        {
            var data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(KEY));
            coins         = data.coins;
            selectedCarId = data.selectedCarId;

            // Garage (equipped items) yükle
            foreach (var rec in data.cars)
            {
                var dict = new Dictionary<CarUpgradeSlot,string>();
                foreach (var e in rec.equipped)
                    if (Enum.TryParse(e.slot, out CarUpgradeSlot slotEnum))
                        dict[slotEnum] = e.upgradeId;

                garage[rec.carId] = dict;
            }

            // Inventory yükle
            if (data.inventory != null)
            {
                foreach (var car in data.inventory.ownedCars)
                    inventoryCars[car.carId] = car.quantity;

                foreach (var upgrade in data.inventory.ownedUpgrades)
                    inventoryUpgrades[upgrade.upgradeName] = upgrade.quantity;
            }

            Debug.Log("Game loaded successfully!");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Save load error: {e.Message}");
            return false;
        }
    }

    public static void ResetSave() 
    { 
        PlayerPrefs.DeleteKey(KEY);
        Debug.Log("Save data reset!");
    }
}
