using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class GarageCarItem : MonoBehaviour
{
    [Header("UI References")]
    public Image carIcon;
    public TextMeshProUGUI carNameText;
    public TextMeshProUGUI quantityText;
    public Button actionButton;
    public GameObject selectedIndicator;

    [Header("Button Colors")]
    public Color useButtonColor = Color.green;
    public Color selectButtonColor = Color.blue;
    public Color selectedButtonColor = Color.yellow;

    private CarDefinition carData;
    private int quantity;
    private bool isInventoryMode;
    private Action<CarDefinition> onActionCallback;

    public void Setup(CarDefinition carDef, int qty, bool inventoryMode, Action<CarDefinition> actionCallback)
    {
        carData = carDef;
        quantity = qty;
        isInventoryMode = inventoryMode;
        onActionCallback = actionCallback;

        UpdateUI();
        SetupButton();
    }

    private void UpdateUI()
    {
        if (carData == null) return;

        // Araç adını göster
        if (carNameText != null)
            carNameText.text = carData.carId;

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

        // Icon ayarla
        if (carIcon != null && carData.carIcon != null)
            carIcon.sprite = carData.carIcon;

        // Seçili araç indicator'ı
        UpdateSelectedIndicator();
    }

    private void UpdateSelectedIndicator()
    {
        if (selectedIndicator != null)
        {
            bool isSelected = !isInventoryMode && 
                             GameManager.Instance.selectedCarId == carData.carId;
            selectedIndicator.SetActive(isSelected);
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
            // Inventory mode - "USE" butonu
            if (textComponent != null)
                textComponent.text = "USE";
            
            buttonColors.normalColor = useButtonColor;
        }
        else
        {
            // Garage mode - "SELECT" butonu
            bool isSelected = GameManager.Instance.selectedCarId == carData.carId;
            
            if (textComponent != null)
                textComponent.text = isSelected ? "SELECTED" : "SELECT";
            
            buttonColors.normalColor = isSelected ? selectedButtonColor : selectButtonColor;
            actionButton.interactable = !isSelected;
        }

        actionButton.colors = buttonColors;
    }

    private void OnActionClicked()
    {
        if (carData != null && onActionCallback != null)
        {
            onActionCallback.Invoke(carData);
            
            // UI'ı güncelle
            UpdateUI();
            UpdateButtonAppearance();
        }
    }

    // Dış güncellemeler için
    public void RefreshState()
    {
        UpdateSelectedIndicator();
        UpdateButtonAppearance();
    }
} 