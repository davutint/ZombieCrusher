using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ModernUpgradeShopItem : MonoBehaviour
{
    [Header("UI References")]
    public Image upgradeIcon;
    public TextMeshProUGUI upgradeNameText;
    public TextMeshProUGUI upgradeStatsText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI quantityText; // Inventory'deki miktar
    public Button buyButton;
    public Button watchAdButton; // Reklam izleyerek açma
    public GameObject lockedIndicator;

    [Header("Button States")]
    public Color buyButtonNormalColor = Color.green;
    public Color buyButtonDisabledColor = Color.gray;

    private CarUpgrade upgradeData;
    private int upgradePrice = 100; // Varsayılan fiyat
    private Action<CarUpgrade, int> onPurchaseCallback;

    public void Setup(CarUpgrade upgrade, Action<CarUpgrade, int> purchaseCallback)
    {
        upgradeData = upgrade;
        onPurchaseCallback = purchaseCallback;

        // Fiyatı hesapla (basit algoritma)
        CalculatePrice();

        UpdateUI();
        SetupButtons();
    }

    private void CalculatePrice()
    {
        if (upgradeData == null) return;

        // Upgrade'in etkilerine göre fiyat hesapla
        int basePrice = 50;
        
        if (upgradeData.maxSpeedModifier > 0)
            basePrice += (int)(upgradeData.maxSpeedModifier * 10);
        
        if (upgradeData.accelerationModifier > 0)
            basePrice += (int)(upgradeData.accelerationModifier * 15);
        
        if (upgradeData.healthModifier > 0)
            basePrice += (int)(upgradeData.healthModifier * 5);

        upgradePrice = Mathf.Max(basePrice, 25); // Minimum fiyat
    }

    private void UpdateUI()
    {
        if (upgradeData == null) return;

        // Upgrade bilgilerini göster
        if (upgradeNameText != null)
            upgradeNameText.text = upgradeData.upgradeName;

        if (upgradeStatsText != null)
            upgradeStatsText.text = GetUpgradeStatsText();

        if (priceText != null)
            priceText.text = $"💰 {upgradePrice:N0}";

        // Icon'u ayarla (eğer prefab'da icon varsa)
        if (upgradeIcon != null && upgradeData.upgradePrefab != null)
        {
            // Prefab'dan icon çıkarmaya çalış
            var renderer = upgradeData.upgradePrefab.GetComponentInChildren<Renderer>();
            if (renderer != null && renderer.material.mainTexture != null)
            {
                var texture = renderer.material.mainTexture as Texture2D;
                if (texture != null)
                {
                    var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
                    upgradeIcon.sprite = sprite;
                }
            }
        }

        RefreshState();
    }

    private string GetUpgradeStatsText()
    {
        if (upgradeData == null) return "";

        string stats = "";
        
        if (upgradeData.maxSpeedModifier != 0)
            stats += $"🏁 Speed: {(upgradeData.maxSpeedModifier > 0 ? "+" : "")}{upgradeData.maxSpeedModifier}\n";
        
        if (upgradeData.accelerationModifier != 0)
            stats += $"⚡ Acceleration: {(upgradeData.accelerationModifier > 0 ? "+" : "")}{upgradeData.accelerationModifier}\n";
        
        if (upgradeData.healthModifier != 0)
            stats += $"🛡️ Health: {(upgradeData.healthModifier > 0 ? "+" : "")}{upgradeData.healthModifier}\n";

        if (string.IsNullOrEmpty(stats))
            stats = "🔧 Special Upgrade";

        stats += $"\n📦 Slot: {upgradeData.upgradeSlot}";

        return stats.Trim();
    }

    private void SetupButtons()
    {
        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyClicked);

        if (watchAdButton != null)
            watchAdButton.onClick.AddListener(OnWatchAdClicked);
    }

    public void RefreshState()
    {
        if (upgradeData == null) return;

        bool hasEnoughCoins = GameManager.Instance.coins >= upgradePrice;
        int ownedQuantity = InventoryManager.Instance.GetUpgradeQuantity(upgradeData.upgradeName);
        bool isUnlocked = IsUpgradeUnlocked();

        // Miktar metnini güncelle
        if (quantityText != null)
        {
            if (ownedQuantity > 0)
            {
                quantityText.text = $"Owned: {ownedQuantity}";
                quantityText.gameObject.SetActive(true);
            }
            else
            {
                quantityText.gameObject.SetActive(false);
            }
        }

        // Kilit durumunu güncelle
        if (lockedIndicator != null)
            lockedIndicator.SetActive(!isUnlocked);

        // Buton durumlarını güncelle
        UpdateButtonStates(hasEnoughCoins, isUnlocked);
    }

    private void UpdateButtonStates(bool hasEnoughCoins, bool isUnlocked)
    {
        if (buyButton != null)
        {
            if (!isUnlocked)
            {
                buyButton.interactable = false;
                SetButtonColor(buyButton, buyButtonDisabledColor);
                SetButtonText(buyButton, "LOCKED");
            }
            else if (hasEnoughCoins)
            {
                buyButton.interactable = true;
                SetButtonColor(buyButton, buyButtonNormalColor);
                SetButtonText(buyButton, "BUY");
            }
            else
            {
                buyButton.interactable = false;
                SetButtonColor(buyButton, buyButtonDisabledColor);
                SetButtonText(buyButton, "NOT ENOUGH COINS");
            }
        }

        // Reklam butonu
        if (watchAdButton != null)
        {
            watchAdButton.gameObject.SetActive(!isUnlocked);
        }
    }

    private void SetButtonColor(Button button, Color color)
    {
        var colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.1f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.1f);
        button.colors = colors;
    }

    private void SetButtonText(Button button, string text)
    {
        var textComponent = button.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null)
            textComponent.text = text;
    }

    private bool IsUpgradeUnlocked()
    {
        // TODO: Reklam sistemi entegrasyonu
        // Şimdilik ilk yarısı unlock, diğerleri reklam gerektirir
        var allUpgrades = GameManager.Instance.GetUpgradeCatalog();
        if (allUpgrades == null) return true;

        for (int i = 0; i < allUpgrades.Length; i++)
        {
            if (allUpgrades[i].upgradeName == upgradeData.upgradeName)
            {
                return i < allUpgrades.Length / 2; // İlk yarısı serbest
            }
        }
        return true;
    }

    private void OnBuyClicked()
    {
        if (upgradeData != null && onPurchaseCallback != null)
        {
            onPurchaseCallback.Invoke(upgradeData, upgradePrice);
        }
    }

    private void OnWatchAdClicked()
    {
        Debug.Log($"Watch ad to unlock {upgradeData.upgradeName}");
        // TODO: Reklam sistemi entegrasyonu
        // Şimdilik debug mesajı ve otomatik unlock
        
        UnlockUpgrade();
    }

    private void UnlockUpgrade()
    {
        // TODO: Unlock durumunu kaydetmek için sistem gerekli
        Debug.Log($"{upgradeData.upgradeName} unlocked after watching ad!");
        RefreshState();
    }
} 