using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameplayUIManager : MonoBehaviour
{
    public static GameplayUIManager Instance { get; private set; }

    [Header("Gameplay UI")]
    public GameObject gameplayUI;
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI healthText;

    [Header("Game Over Panel")]
    public GameObject gameOverPanel;
    public Button claimButton;
    public Button doubleClaimButton;
    public TextMeshProUGUI earnedCoinsText;

    private int currentEarnedCoins;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (gameplayUI != null) gameplayUI.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (claimButton != null)
            claimButton.onClick.AddListener(ClaimReward);
        if (doubleClaimButton != null)
            doubleClaimButton.onClick.AddListener(ClaimDoubleReward);

        UpdateCoinsUI();
    }

    public void ShowGameOver(int earnedCoins)
    {
        currentEarnedCoins = earnedCoins;
        
        if (gameplayUI != null) gameplayUI.SetActive(false);
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (earnedCoinsText != null)
                earnedCoinsText.text = $"Earned Coins: {earnedCoins}";
        }
    }

    private void ClaimReward()
    {
        GameManager.Instance.AddCoins(currentEarnedCoins);
        SceneManager.LoadScene(SceneController.MAIN_MENU_SCENE);
    }

    private void ClaimDoubleReward()
    {
        GameManager.Instance.AddCoins(currentEarnedCoins * 2);
        SceneManager.LoadScene(SceneController.MAIN_MENU_SCENE);
    }

    public void UpdateCoinsUI()
    {
        if (coinsText != null && GameManager.Instance != null)
        {
            coinsText.text = $"Coins: {GameManager.Instance.coins}";
        }
    }

    public void UpdateHealthUI(float currentHealth)
    {
        if (healthText != null)
        {
            healthText.text = $"Health: {currentHealth:F0}";
        }
    }
} 