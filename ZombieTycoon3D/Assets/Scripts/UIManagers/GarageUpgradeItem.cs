using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class GarageUpgradeItem : MonoBehaviour
{
    [Header("UI References")]
    public Image upgradeIcon;
    public TextMeshProUGUI upgradeNameText;
    public TextMeshProUGUI upgradeStatsText;
    public TextMeshProUGUI quantityText;
    public Button actionButton;
    public GameObject slotIndicator;

    [Header("Button Colors")]
    public Color equipButtonColor = Color.green;
    public Color unequipButtonColor = Color.red;

    private CarUpgrade upgradeData;
    private int quantity;
    private bool isInventoryMode;
    private Action<CarUpgrade> onActionCallback;

    public void Setup(CarUpgrade upgrade, int qty, bool inventoryMode, Action<CarUpgrade> actionCallback)
    {
        upgradeData = upgrade;
        quantity = qty;
        isInventoryMode = inventoryMode;
        onActionCallback = actionCallback;

        UpdateUI();
        SetupButton();
    }

    private void UpdateUI()
    {
        if (upgradeData == null) return;

        // Upgrade adını göster
        if (upgradeNameText != null)
            upgradeNameText.text = upgradeData.upgradeName;

        // Stats göster
        if (upgradeStatsText != null)
            upgradeStatsText.text = GetUpgradeStatsText();

        // Miktar göster (sadece inventory modunda)
        if (quantityText != null)
        {
            if (isInventoryMode && quantity > 1)
            {
                quantityText.text = $"x{quantity}";
                quantityText.gameObject.SetActive(true);
            }
            else
            {
                quantityText.gameObject.SetActive(false);
            }
        }

        // Slot indicator'ı göster
        if (slotIndicator != null)
        {
            var slotText = slotIndicator.GetComponentInChildren<TextMeshProUGUI>();
            if (slotText != null)
                slotText.text = upgradeData.upgradeSlot.ToString();
        }

        // Icon ayarla
        SetUpgradeIcon();
    }

    private string GetUpgradeStatsText()
    {
        if (upgradeData == null) return "";

        string stats = "";
        
        if (upgradeData.maxSpeedModifier != 0)
            stats += $"🏁 {(upgradeData.maxSpeedModifier > 0 ? "+" : "")}{upgradeData.maxSpeedModifier} Speed\n";
        
        if (upgradeData.accelerationModifier != 0)
            stats += $"⚡ {(upgradeData.accelerationModifier > 0 ? "+" : "")}{upgradeData.accelerationModifier} Accel\n";
        
        if (upgradeData.healthModifier != 0)
            stats += $"🛡️ {(upgradeData.healthModifier > 0 ? "+" : "")}{upgradeData.healthModifier} Health\n";

        return stats.Trim();
    }

    private void SetUpgradeIcon()
    {
        if (upgradeIcon == null || upgradeData.upgradePrefab == null) return;

        // Prefab'dan icon çıkarmaya çalış
        var renderer = upgradeData.upgradePrefab.GetComponentInChildren<Renderer>();
        if (renderer != null && renderer.material.mainTexture != null)
        {
            var texture = renderer.material.mainTexture as Texture2D;
            if (texture != null)
            {
                try
                {
                    var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
                    upgradeIcon.sprite = sprite;
                }
                catch
                {
                    // Hata varsa varsayılan icon bırak
                }
            }
        }
    }

    private void SetupButton()
    {
        if (actionButton == null) return;

        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(OnActionClicked);

        UpdateButtonAppearance();
    }

    private void UpdateButtonAppearance()
    {
        if (actionButton == null) return;

        var textComponent = actionButton.GetComponentInChildren<TextMeshProUGUI>();
        var buttonColors = actionButton.colors;

        if (isInventoryMode)
        {
            // Inventory mode - "EQUIP" butonu
            if (textComponent != null)
                textComponent.text = "EQUIP";
            
            buttonColors.normalColor = equipButtonColor;
            
            // Seçili araç yoksa disable
            bool hasSelectedCar = !string.IsNullOrEmpty(GameManager.Instance.selectedCarId);
            actionButton.interactable = hasSelectedCar;
            
            if (!hasSelectedCar && textComponent != null)
                textComponent.text = "SELECT CAR FIRST";
        }
        else
        {
            // Equipped mode - "UNEQUIP" butonu
            if (textComponent != null)
                textComponent.text = "UNEQUIP";
            
            buttonColors.normalColor = unequipButtonColor;
            actionButton.interactable = true;
        }

        actionButton.colors = buttonColors;
    }

    private void OnActionClicked()
    {
        if (upgradeData != null && onActionCallback != null)
        {
            onActionCallback.Invoke(upgradeData);
        }
    }

    // Dış güncellemeler için
    public void RefreshState()
    {
        UpdateButtonAppearance();
    }
} 