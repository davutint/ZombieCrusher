using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public const string MAIN_MENU_SCENE = "MainMenu";
    public const string GAMEPLAY_SCENE = "Gameplay";
    public const string FIRST_TIME_KEY = "first_time_player";

    private void Start()
    {
        // İlk kez oyuna giren oyuncu kontrolü
        if (!PlayerPrefs.HasKey(FIRST_TIME_KEY))
        {
            PlayerPrefs.SetInt(FIRST_TIME_KEY, 1);
            SceneManager.LoadScene(GAMEPLAY_SCENE);
        }
    }

    public static void LoadMainMenu()
    {
        SceneManager.LoadScene(MAIN_MENU_SCENE);
    }

    public static void LoadGameplay()
    {
        SceneManager.LoadScene(GAMEPLAY_SCENE);
    }

    public static void RestartGame()
    {
        // Save'i sıfırla ve gameplay'e git
        SaveSystem.ResetSave();
        PlayerPrefs.DeleteKey(FIRST_TIME_KEY);
        SceneManager.LoadScene(GAMEPLAY_SCENE);
    }
} 