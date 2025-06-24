using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ModernCarShopItem : MonoBehaviour
{
    [Header("UI References")]
    public Image carIcon;
    public TextMeshProUGUI carNameText;
    public TextMeshProUGUI carStatsText;
    public TextMeshProUGUI priceText;
    public Button buyButton;
    public Button watchAdButton; // Reklam izleyerek açma butonu
    public GameObject ownedIndicator;
    public GameObject lockedIndicator;

    [Header("Button States")]
    public Color buyButtonNormalColor = Color.green;
    public Color buyButtonDisabledColor = Color.gray;
    public Color ownedButtonColor = Color.blue;

    private CarDefinition carData;
    private Action<CarDefinition> onPurchaseCallback;

    public void Setup(CarDefinition carDef, Action<CarDefinition> purchaseCallback)
    {
        carData = carDef;
        onPurchaseCallback = purchaseCallback;

        UpdateUI();
        SetupButtons();
    }

    private void UpdateUI()
    {
        if (carData == null) return;

        // Araç bilgilerini göster
        if (carNameText != null)
            carNameText.text = carData.carId;

        if (carStatsText != null)
            carStatsText.text = GetCarStatsText();

        if (priceText != null)
            priceText.text = $"💰 {carData.price:N0}";

        // Icon'u ayarla (eğer varsa)
        if (carIcon != null && carData.carIcon != null)
            carIcon.sprite = carData.carIcon;

        RefreshState();
    }

    private string GetCarStatsText()
    {
        // CarDefinition'da stats yoksa varsayılan değerler göster
        return "🏎️ High Performance\n⚡ Fast Acceleration\n🛡️ Durable Body";
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
        if (carData == null) return;

        bool hasEnoughCoins = GameManager.Instance.coins >= carData.price;
        bool isInInventory = InventoryManager.Instance.OwnsCarInInventory(carData.carId);
        bool isInGarage = GameManager.Instance.OwnsCar(carData.carId);
        bool isUnlocked = IsCarUnlocked(); // Reklam sistemi için

        // Durum indicator'larını güncelle
        if (ownedIndicator != null)
            ownedIndicator.SetActive(isInInventory || isInGarage);

        if (lockedIndicator != null)
            lockedIndicator.SetActive(!isUnlocked);

        // Buton durumlarını güncelle
        UpdateButtonStates(hasEnoughCoins, isInInventory, isInGarage, isUnlocked);
    }

    private void UpdateButtonStates(bool hasEnoughCoins, bool isInInventory, bool isInGarage, bool isUnlocked)
    {
        if (buyButton != null)
        {
            // Araç zaten sahip olunan mı?
            if (isInInventory || isInGarage)
            {
                buyButton.interactable = false;
                SetButtonColor(buyButton, ownedButtonColor);
                SetButtonText(buyButton, "OWNED");
            }
            // Araç kilitli mi ve reklam gerekli mi?
            else if (!isUnlocked)
            {
                buyButton.interactable = false;
                SetButtonColor(buyButton, buyButtonDisabledColor);
                SetButtonText(buyButton, "LOCKED");
            }
            // Normal satın alma
            else if (hasEnoughCoins)
            {
                buyButton.interactable = true;
                SetButtonColor(buyButton, buyButtonNormalColor);
                SetButtonText(buyButton, "BUY");
            }
            // Para yeterli değil
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
            // Sadece kilitli araçlarda reklam butonu görünsün
            bool shouldShowAdButton = !isUnlocked && !(isInInventory || isInGarage);
            watchAdButton.gameObject.SetActive(shouldShowAdButton);
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

    private bool IsCarUnlocked()
    {
        // TODO: Reklam sistemi entegrasyonu
        // Şimdilik ilk 3 araç unlock, diğerleri reklam gerektirir
        var allCars = GameManager.Instance.GetCarCatalog();
        if (allCars == null) return true;

        for (int i = 0; i < allCars.Length; i++)
        {
            if (allCars[i].carId == carData.carId)
            {
                return i < 3; // İlk 3 araç serbest
            }
        }
        return true;
    }

    private void OnBuyClicked()
    {
        if (carData != null && onPurchaseCallback != null)
        {
            onPurchaseCallback.Invoke(carData);
        }
    }

    private void OnWatchAdClicked()
    {
        Debug.Log($"Watch ad to unlock {carData.carId}");
        // TODO: Reklam sistemi entegrasyonu
        // Şimdilik debug mesajı ve otomatik unlock
        
        // Geçici olarak aracı unlock yap
        UnlockCar();
    }

    private void UnlockCar()
    {
        // TODO: Unlock durumunu kaydetmek için sistem gerekli
        Debug.Log($"{carData.carId} unlocked after watching ad!");
        RefreshState();
    }
} 