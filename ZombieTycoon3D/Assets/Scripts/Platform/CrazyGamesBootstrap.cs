using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class CrazyGamesBootstrap : MonoBehaviour
{
    [SerializeField, Min(1)] private int gameplaySceneBuildIndex = 1;

    private IEnumerator Start()
    {
        CrazyGamesPlatformService.EnsureExists();
        while (!CrazyGamesPlatformService.IsReady)
        {
            yield return null;
        }

        if (gameplaySceneBuildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError(
                $"CrazyGames bootstrap could not load build index {gameplaySceneBuildIndex}; check Build Settings.",
                this);
            yield break;
        }

        yield return SceneManager.LoadSceneAsync(gameplaySceneBuildIndex);
    }
}
