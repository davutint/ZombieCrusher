using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance => _instance;

    [Header("Kataloglar")]
    [SerializeField] private CarDefinition[]   carCatalog;     // Inspector'a sürükle
    [SerializeField] private CarUpgrade[]      upgradeCatalog; // Inspector'a sürükle

    [Header("Runtime")]
    public  int                                coins;
    public string                             selectedCarId;
    private readonly Dictionary<string,Dictionary<CarUpgradeSlot,CarUpgrade>>
                                              garage = new(); // carId -> slot->upgrade (EQUIPPED ITEMS)

    /* ---------------- Mono ---------------- */
    private void Awake()
    {
        // Singleton kontrolü
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // InventoryManager'ı da oluştur
        if (InventoryManager.Instance == null)
        {
            var inventoryGO = new GameObject("InventoryManager");
            inventoryGO.AddComponent<InventoryManager>();
            DontDestroyOnLoad(inventoryGO);
        }

        LoadGame();
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        Debug.Log("GameManager initialized!");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == SceneController.GAMEPLAY_SCENE)
            SpawnSelectedCar();
    }

    /* ---------------- Public API ---------------- */
    #region Coins
    public void AddCoins(int amount)   { coins += amount; SaveGame(); }
    public bool SpendCoins(int amount) { if (coins < amount) return false; coins -= amount; SaveGame(); return true; }
    #endregion

    #region Market Operations (Buy to Inventory)
    /// <summary>
    /// Market'ten araç satın al - inventory'ye gider
    /// </summary>
    public bool BuyCarToInventory(CarDefinition def)
    {
        if (!SpendCoins(def.price)) return false;        // parası yok
        
        InventoryManager.Instance.AddCarToInventory(def.carId);
        SaveGame();
        Debug.Log($"Market: {def.carId} purchased for {def.price} coins");
        return true;
    }

    /// <summary>
    /// Market'ten upgrade satın al - inventory'ye gider
    /// </summary>
    public bool BuyUpgradeToInventory(CarUpgrade upgrade, int price)
    {
        if (!SpendCoins(price)) return false;
        
        InventoryManager.Instance.AddUpgradeToInventory(upgrade.upgradeName);
        SaveGame();
        Debug.Log($"Market: {upgrade.upgradeName} purchased for {price} coins");
        return true;
    }
    #endregion

    #region Garage Operations (Use from Inventory)
    /// <summary>
    /// Inventory'den araç kullan - garage'a taşı ve seç
    /// </summary>
    public bool UseCarFromInventory(string carId)
    {
        if (!InventoryManager.Instance.UseCarFromInventory(carId)) return false;
        
        // Garage'a ekle (boş upgrade listesi ile)
        if (!garage.ContainsKey(carId))
            garage[carId] = new();
        
        // Seçili araç yap
        selectedCarId = carId;
        SaveGame();
        Debug.Log($"Garage: {carId} moved from inventory and selected");
        return true;
    }

    /// <summary>
    /// Inventory'den upgrade kullan - seçili araca tak
    /// </summary>
    public bool UseUpgradeFromInventory(string upgradeName)
    {
        if (string.IsNullOrEmpty(selectedCarId)) return false;
        if (!OwnsCar(selectedCarId)) return false;
        if (!InventoryManager.Instance.UseUpgradeFromInventory(upgradeName)) return false;

        var upgrade = FindUpgradeByName(upgradeName);
        if (upgrade == null) return false;

        var dict = garage[selectedCarId];
        dict[upgrade.upgradeSlot] = upgrade;
        SaveGame();
        Debug.Log($"Garage: {upgradeName} equipped to {selectedCarId}");
        return true;
    }

    /// <summary>
    /// Garage'dan upgrade çıkar - inventory'ye geri koy
    /// </summary>
    public bool UnequipUpgrade(string carId, CarUpgradeSlot slot)
    {
        if (!garage.ContainsKey(carId)) return false;
        if (!garage[carId].ContainsKey(slot)) return false;

        var upgrade = garage[carId][slot];
        garage[carId].Remove(slot);
        
        InventoryManager.Instance.AddUpgradeToInventory(upgrade.upgradeName);
        SaveGame();
        Debug.Log($"Garage: {upgrade.upgradeName} unequipped and returned to inventory");
        return true;
    }
    #endregion

    #region Garage Info
    public bool OwnsCar(string carId) => garage.ContainsKey(carId);
    public void SelectCar(string carId)
    {
        if (!OwnsCar(carId)) return;                    // sahip değilse seçilemez
        selectedCarId = carId;
        SaveGame();
    }

    public Dictionary<CarUpgradeSlot,CarUpgrade> GetUpgrades(string carId)
        => garage.TryGetValue(carId, out var d) ? d : null;
    #endregion

    /* ---------------- Gameplay Spawn ---------------- */
    private void SpawnSelectedCar()
    {
        CarDefinition def = System.Array.Find(carCatalog, c => c.carId == selectedCarId);
        if (def == null) { Debug.LogError("Seçili araç bulunamadı!"); return; }

        GameObject go = Instantiate(def.carPrefab, Vector3.zero, Quaternion.identity);

        // Upgrade'leri uygula (sadece garage'daki equipped olanlar)
        if (garage.TryGetValue(def.carId, out var slots))
        {
            var upMgr = go.GetComponent<CarUpgradeManager>();
            if (upMgr != null)
            {
                foreach (var kvp in slots)
                    upMgr.ApplyUpgrade(kvp.Value);
            }
        }

        // UI güncelle
        if (GameplayUIManager.Instance != null)
            GameplayUIManager.Instance.UpdateCoinsUI();
    }

    private CarUpgrade FindUpgradeByName(string name) =>
        System.Array.Find(upgradeCatalog, u => u.upgradeName == name);

    /* ---------------- Save/Load ---------------- */
    private void SaveGame()
    {
        var inventoryCars = InventoryManager.Instance?.GetOwnedCars() ?? new Dictionary<string, int>();
        var inventoryUpgrades = InventoryManager.Instance?.GetOwnedUpgrades() ?? new Dictionary<string, int>();
        
        SaveSystem.Save(coins, selectedCarId, garage, inventoryCars, inventoryUpgrades);
    }

    private void LoadGame()
    {
        if (SaveSystem.TryLoad(out int c, out string sel,
                               out Dictionary<string,Dictionary<CarUpgradeSlot,string>> g,
                               out Dictionary<string, int> invCars,
                               out Dictionary<string, int> invUpgrades))
        {
            coins         = c;
            selectedCarId = sel;
            
            // Garage yükle
            foreach (var car in g)
            {
                garage[car.Key] = new();
                foreach (var kvp in car.Value)
                    garage[car.Key][kvp.Key] = FindUpgradeByName(kvp.Value);
            }
            
            // Inventory yükle
            if (InventoryManager.Instance != null)
                InventoryManager.Instance.LoadInventoryData(invCars, invUpgrades);
        }
        else
        {
            // İlk kez giren kullanıcı için varsayılan araba hediye
            if (carCatalog != null && carCatalog.Length > 0)
            {
                var def = carCatalog[0];
                garage[def.carId] = new();
                selectedCarId     = def.carId;
                SaveGame(); // İlk kez oyuna girende kaydet
            }
        }
    }

    /* ---------------- Public Getters ---------------- */
    public CarDefinition[] GetCarCatalog() => carCatalog;
    public CarUpgrade[] GetUpgradeCatalog() => upgradeCatalog;
}
