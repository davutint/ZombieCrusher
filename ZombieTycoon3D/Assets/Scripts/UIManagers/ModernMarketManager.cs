using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class ModernMarketManager : MonoBehaviour
{
    [Header("Tab System")]
    public Button carsTabButton;
    public Button upgradesTabButton;
    public GameObject carsContent;
    public GameObject upgradesContent;
    public Image carsTabIndicator;
    public Image upgradesTabIndicator;

    [Header("Car Shop")]
    public Transform carShopParent;
    public ModernCarShopItem carItemPrefab;

    [Header("Upgrade Shop")]
    public Transform upgradeShopParent;
    public ModernUpgradeShopItem upgradeItemPrefab;

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
        
        RefreshShop();
        ShowCarsTab(); // Varsayılan olarak araçlar sekmesi
    }

    private void Initialize()
    {
        // Tab butonlarını ayarla
        if (carsTabButton != null)
            carsTabButton.onClick.AddListener(ShowCarsTab);
        
        if (upgradesTabButton != null)
            upgradesTabButton.onClick.AddListener(ShowUpgradesTab);

        PopulateCarShop();
        PopulateUpgradeShop();
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
    }

    private void UpdateTabIndicators(bool carsActive)
    {
        if (carsTabIndicator != null)
            carsTabIndicator.color = carsActive ? activeTabColor : inactiveTabColor;
        
        if (upgradesTabIndicator != null)
            upgradesTabIndicator.color = carsActive ? inactiveTabColor : activeTabColor;

        // Tab buton renklerini de güncelle
        if (carsTabButton != null)
        {
            var colors = carsTabButton.colors;
            colors.normalColor = carsActive ? activeTabColor : inactiveTabColor;
            carsTabButton.colors = colors;
        }

        if (upgradesTabButton != null)
        {
            var colors = upgradesTabButton.colors;
            colors.normalColor = carsActive ? inactiveTabColor : activeTabColor;
            upgradesTabButton.colors = colors;
        }
    }

    private void PopulateCarShop()
    {
        if (carItemPrefab == null || carShopParent == null) return;

        // Mevcut itemları temizle
        foreach (Transform child in carShopParent)
            Destroy(child.gameObject);

        // Araç kataloğunu al
        var carCatalog = GameManager.Instance.GetCarCatalog();
        if (carCatalog == null) return;

        // Her araç için item oluştur
        foreach (var carDef in carCatalog)
        {
            var item = Instantiate(carItemPrefab, carShopParent);
            item.Setup(carDef, OnCarPurchaseClicked);
        }
    }

    private void PopulateUpgradeShop()
    {
        if (upgradeItemPrefab == null || upgradeShopParent == null) return;

        // Mevcut itemları temizle
        foreach (Transform child in upgradeShopParent)
            Destroy(child.gameObject);

        // Upgrade kataloğunu al
        var upgradeCatalog = GameManager.Instance.GetUpgradeCatalog();
        if (upgradeCatalog == null) return;

        // Her upgrade için item oluştur
        foreach (var upgrade in upgradeCatalog)
        {
            var item = Instantiate(upgradeItemPrefab, upgradeShopParent);
            item.Setup(upgrade, OnUpgradePurchaseClicked);
        }
    }

    private void OnCarPurchaseClicked(CarDefinition carDef)
    {
        bool success = GameManager.Instance.BuyCarToInventory(carDef);
        
        if (success)
        {
            Debug.Log($"Successfully purchased {carDef.carId}");
            RefreshShop();
            
            // Ana menüdeki coins UI'ını güncelle
            if (MainMenuManager.Instance != null)
                MainMenuManager.Instance.UpdateCoinsUI();

            // Başarı animasyonu
            ShowPurchaseSuccess();
        }
        else
        {
            Debug.Log("Not enough coins!");
            ShowPurchaseFailure();
        }
    }

    private void OnUpgradePurchaseClicked(CarUpgrade upgrade, int price)
    {
        bool success = GameManager.Instance.BuyUpgradeToInventory(upgrade, price);
        
        if (success)
        {
            Debug.Log($"Successfully purchased {upgrade.upgradeName}");
            RefreshShop();
            
            // Ana menüdeki coins UI'ını güncelle
            if (MainMenuManager.Instance != null)
                MainMenuManager.Instance.UpdateCoinsUI();

            // Başarı animasyonu
            ShowPurchaseSuccess();
        }
        else
        {
            Debug.Log("Not enough coins!");
            ShowPurchaseFailure();
        }
    }

    private void RefreshShop()
    {
        // Her item'ın durumunu güncelle
        foreach (Transform child in carShopParent)
        {
            var item = child.GetComponent<ModernCarShopItem>();
            item?.RefreshState();
        }

        foreach (Transform child in upgradeShopParent)
        {
            var item = child.GetComponent<ModernUpgradeShopItem>();
            item?.RefreshState();
        }
    }

    private void ShowPurchaseSuccess()
    {
        // Basit success feedback
        if (carsContent != null && carsContent.activeInHierarchy)
            carsContent.transform.DOPunchScale(Vector3.one * 0.05f, 0.3f, 3, 0.5f);
        
        if (upgradesContent != null && upgradesContent.activeInHierarchy)
            upgradesContent.transform.DOPunchScale(Vector3.one * 0.05f, 0.3f, 3, 0.5f);
    }

    private void ShowPurchaseFailure()
    {
        // Basit failure feedback - shake
        if (carsContent != null && carsContent.activeInHierarchy)
            carsContent.transform.DOShakePosition(0.3f, 10f, 20, 90, false, true);
        
        if (upgradesContent != null && upgradesContent.activeInHierarchy)
            upgradesContent.transform.DOShakePosition(0.3f, 10f, 20, 90, false, true);
    }
} 