using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance;

    [Header("UI References")]
    public TextMeshProUGUI coinsText;
    public Button playButton;
    public Button garageButton;
    public Button marketButton;

    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject garagePanel;
    public GameObject marketPanel;
    public CanvasGroup fadeBackground;

    [Header("Animations")]
    public float animationDuration = 0.4f;
    public Ease animationEase = Ease.OutCubic;

    private GameObject currentActivePanel;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SetupButtons();
        ShowMainMenu();
        UpdateCoinsUI();
    }

    private void SetupButtons()
    {
        if (playButton != null)
            playButton.onClick.AddListener(PlayGame);
        
        if (garageButton != null)
            garageButton.onClick.AddListener(ShowGarage);
        
        if (marketButton != null)
            marketButton.onClick.AddListener(ShowMarket);
    }

    public void PlayGame()
    {
        Debug.Log("Starting gameplay...");
        
        // Seçili araç var mı kontrol et
        if (string.IsNullOrEmpty(GameManager.Instance.selectedCarId))
        {
            ShowNotification("Select a car first!");
            return;
        }
        
        // Gameplay sahnesine geç
        SceneManager.LoadScene(SceneController.GAMEPLAY_SCENE);
    }

    public void ShowMainMenu()
    {
        CloseAllPanels();
        ShowPanel(mainMenuPanel);
        UpdateCoinsUI();
    }

    public void ShowGarage()
    {
        CloseAllPanels();
        ShowPanel(garagePanel);
        UpdateCoinsUI();
    }

    public void ShowMarket()
    {
        CloseAllPanels();
        ShowPanel(marketPanel);
        UpdateCoinsUI();
    }

    private void ShowPanel(GameObject panel)
    {
        if (panel == null) return;

        currentActivePanel = panel;
        panel.SetActive(true);

        // Fade background animasyonu
        if (fadeBackground != null && panel != mainMenuPanel)
        {
            fadeBackground.gameObject.SetActive(true);
            fadeBackground.alpha = 0;
            fadeBackground.DOFade(0.7f, animationDuration).SetUpdate(true);
        }

        // Panel animasyonu
        var canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.transform.localScale = Vector3.one * 0.9f;
            
            canvasGroup.DOFade(1f, animationDuration).SetUpdate(true);
            canvasGroup.transform.DOScale(Vector3.one, animationDuration)
                .SetEase(animationEase).SetUpdate(true);
        }
    }

    private void CloseAllPanels()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (garagePanel != null) garagePanel.SetActive(false);
        if (marketPanel != null) marketPanel.SetActive(false);
        
        if (fadeBackground != null)
            fadeBackground.gameObject.SetActive(false);
    }

    public void UpdateCoinsUI()
    {
        if (coinsText != null)
        {
            coinsText.text = $"💰 {GameManager.Instance.coins:N0}";
        }
    }

    private void ShowNotification(string message)
    {
        Debug.Log($"Notification: {message}");
        // TODO: Notification popup sistemi eklenebilir
    }

    public void OnBackButtonClicked()
    {
        ShowMainMenu();
    }

    // Called by UI buttons to close panels with animation
    public void CloseCurrentPanel()
    {
        if (currentActivePanel == null) return;

        var canvasGroup = currentActivePanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(0f, animationDuration).SetUpdate(true)
                .OnComplete(() => ShowMainMenu());
        }
        else
        {
            ShowMainMenu();
        }
    }
} 