using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class ModernGarageManager : MonoBehaviour
{
    [Header("Tab System")]
    public Button carsTabButton;
    public Button upgradesTabButton;
    public GameObject carsContent;
    public GameObject upgradesContent;
    public Image carsTabIndicator;
    public Image upgradesTabIndicator;

    [Header("Cars Section")]
    public Transform inventoryCarsParent;
    public Transform garageCarsParent;
    public GarageCarItem carItemPrefab;
    public TextMeshProUGUI selectedCarText;

    [Header("Upgrades Section")]
    public Transform inventoryUpgradesParent;
    public Transform equippedUpgradesParent;
    public GarageUpgradeItem upgradeItemPrefab;

    [Header("Colors")]
    public Color activeTabColor = Color.white;
    public Color inactiveTabColor = Color.gray;

    private bool isInitialized = false;

    private void OnEnable()
    {
        if (!isInitialized)
        {
            Initialize();
            isInitialized = true;
        }
        
        RefreshGarage();
        ShowCarsTab(); // Varsayılan olarak araçlar sekmesi
    }

    private void Initialize()
    {
        // Tab butonlarını ayarla
        if (carsTabButton != null)
            carsTabButton.onClick.AddListener(ShowCarsTab);
        
        if (upgradesTabButton != null)
            upgradesTabButton.onClick.AddListener(ShowUpgradesTab);
    }

    public void ShowCarsTab()
    {
        // Content'leri aktif/pasif yap
        if (carsContent != null) carsContent.SetActive(true);
        if (upgradesContent != null) upgradesContent.SetActive(false);

        // Tab indicator'ları güncelle
        UpdateTabIndicators(true);

        // Animasyon
        if (carsContent != null)
        {
            carsContent.transform.localScale = Vector3.one * 0.95f;
            carsContent.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }

        RefreshCarsSection();
    }

    public void ShowUpgradesTab()
    {
        // Content'leri aktif/pasif yap
        if (carsContent != null) carsContent.SetActive(false);
        if (upgradesContent != null) upgradesContent.SetActive(true);

        // Tab indicator'ları güncelle
        UpdateTabIndicators(false);

        // Animasyon
        if (upgradesContent != null)
        {
            upgradesContent.transform.localScale = Vector3.one * 0.95f;
            upgradesContent.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }

        RefreshUpgradesSection();
    }

    private void UpdateTabIndicators(bool carsActive)
    {
        if (carsTabIndicator != null)
            carsTabIndicator.color = carsActive ? activeTabColor : inactiveTabColor;
        
        if (upgradesTabIndicator != null)
            upgradesTabIndicator.color = carsActive ? inactiveTabColor : activeTabColor;
    }

    public void RefreshGarage()
    {
        RefreshCarsSection();
        RefreshUpgradesSection();
        UpdateSelectedCarText();
    }

    private void RefreshCarsSection()
    {
        // Inventory Cars
        RefreshInventoryCars();
        
        // Garage Cars (equipped)
        RefreshGarageCars();
        
        UpdateSelectedCarText();
    }

    private void RefreshInventoryCars()
    {
        if (inventoryCarsParent == null || carItemPrefab == null) return;

        // Mevcut itemları temizle
        foreach (Transform child in inventoryCarsParent)
            Destroy(child.gameObject);

        // Inventory'deki araçları göster
        var ownedCars = InventoryManager.Instance.GetOwnedCars();
        var carCatalog = GameManager.Instance.GetCarCatalog();

        foreach (var kvp in ownedCars)
        {
            if (kvp.Value <= 0) continue;

            var carDef = System.Array.Find(carCatalog, c => c.carId == kvp.Key);
            if (carDef == null) continue;

            var item = Instantiate(carItemPrefab, inventoryCarsParent);
            item.Setup(carDef, kvp.Value, true, OnUseCarClicked); // true = inventory mode
        }
    }

    private void RefreshGarageCars()
    {
        if (garageCarsParent == null || carItemPrefab == null) return;

        // Mevcut itemları temizle
        foreach (Transform child in garageCarsParent)
            Destroy(child.gameObject);

        // Garage'daki araçları göster
        var carCatalog = GameManager.Instance.GetCarCatalog();

        foreach (var carDef in carCatalog)
        {
            if (GameManager.Instance.OwnsCar(carDef.carId))
            {
                var item = Instantiate(carItemPrefab, garageCarsParent);
                item.Setup(carDef, 1, false, OnSelectCarClicked); // false = garage mode
            }
        }
    }

    private void RefreshUpgradesSection()
    {
        RefreshInventoryUpgrades();
        RefreshEquippedUpgrades();
    }

    private void RefreshInventoryUpgrades()
    {
        if (inventoryUpgradesParent == null || upgradeItemPrefab == null) return;

        // Mevcut itemları temizle
        foreach (Transform child in inventoryUpgradesParent)
            Destroy(child.gameObject);

        // Inventory'deki upgrade'leri göster
        var ownedUpgrades = InventoryManager.Instance.GetOwnedUpgrades();
        var upgradeCatalog = GameManager.Instance.GetUpgradeCatalog();

        foreach (var kvp in ownedUpgrades)
        {
            if (kvp.Value <= 0) continue;

            var upgrade = System.Array.Find(upgradeCatalog, u => u.upgradeName == kvp.Key);
            if (upgrade == null) continue;

            var item = Instantiate(upgradeItemPrefab, inventoryUpgradesParent);
            item.Setup(upgrade, kvp.Value, true, OnUseUpgradeClicked); // true = inventory mode
        }
    }

    private void RefreshEquippedUpgrades()
    {
        if (equippedUpgradesParent == null || upgradeItemPrefab == null) return;

        // Mevcut itemları temizle
        foreach (Transform child in equippedUpgradesParent)
            Destroy(child.gameObject);

        // Seçili araçtaki equipped upgrade'leri göster
        if (string.IsNullOrEmpty(GameManager.Instance.selectedCarId)) return;

        var equippedUpgrades = GameManager.Instance.GetUpgrades(GameManager.Instance.selectedCarId);
        if (equippedUpgrades == null) return;

        foreach (var kvp in equippedUpgrades)
        {
            var item = Instantiate(upgradeItemPrefab, equippedUpgradesParent);
            item.Setup(kvp.Value, 1, false, OnUnequipUpgradeClicked); // false = equipped mode
        }
    }

    private void UpdateSelectedCarText()
    {
        if (selectedCarText != null)
        {
            string selectedCar = GameManager.Instance.selectedCarId;
            if (string.IsNullOrEmpty(selectedCar))
                selectedCarText.text = "No car selected";
            else
                selectedCarText.text = $"Selected: {selectedCar}";
        }
    }

    /* ---------------- Callbacks ---------------- */
    private void OnUseCarClicked(CarDefinition carDef)
    {
        bool success = GameManager.Instance.UseCarFromInventory(carDef.carId);
        
        if (success)
        {
            Debug.Log($"Car {carDef.carId} moved to garage and selected");
            RefreshGarage();
            
            // Ana menüdeki coins UI'ını güncelle
            if (MainMenuManager.Instance != null)
                MainMenuManager.Instance.UpdateCoinsUI();

            // Başarı animasyonu
            ShowActionSuccess();
        }
    }

    private void OnSelectCarClicked(CarDefinition carDef)
    {
        GameManager.Instance.SelectCar(carDef.carId);
        Debug.Log($"Car {carDef.carId} selected");
        RefreshGarage();
    }

    private void OnUseUpgradeClicked(CarUpgrade upgrade)
    {
        bool success = GameManager.Instance.UseUpgradeFromInventory(upgrade.upgradeName);
        
        if (success)
        {
            Debug.Log($"Upgrade {upgrade.upgradeName} equipped to {GameManager.Instance.selectedCarId}");
            RefreshGarage();
            ShowActionSuccess();
        }
        else
        {
            Debug.Log("Select a car first!");
            ShowActionFailure();
        }
    }

    private void OnUnequipUpgradeClicked(CarUpgrade upgrade)
    {
        string selectedCar = GameManager.Instance.selectedCarId;
        if (string.IsNullOrEmpty(selectedCar)) return;

        bool success = GameManager.Instance.UnequipUpgrade(selectedCar, upgrade.upgradeSlot);
        
        if (success)
        {
            Debug.Log($"Upgrade {upgrade.upgradeName} unequipped and returned to inventory");
            RefreshGarage();
            ShowActionSuccess();
        }
    }

    private void ShowActionSuccess()
    {
        if (carsContent != null && carsContent.activeInHierarchy)
            carsContent.transform.DOPunchScale(Vector3.one * 0.03f, 0.2f, 2, 0.5f);
        
        if (upgradesContent != null && upgradesContent.activeInHierarchy)
            upgradesContent.transform.DOPunchScale(Vector3.one * 0.03f, 0.2f, 2, 0.5f);
    }

    private void ShowActionFailure()
    {
        if (carsContent != null && carsContent.activeInHierarchy)
            carsContent.transform.DOShakePosition(0.2f, 5f, 15, 90, false, true);
        
        if (upgradesContent != null && upgradesContent.activeInHierarchy)
            upgradesContent.transform.DOShakePosition(0.2f, 5f, 15, 90, false, true);
    }
} 