using System;
using System.Collections;
using CrazyGames;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CrazyGamesPlatformService : MonoBehaviour
{
    private const string ConfigResourceName = "CrazyGamesPortalConfig";
    private const float InitializationTimeoutSeconds = 8f;
    private const int TemporaryAdBackoffSeconds = 60;

    private static CrazyGamesPlatformService instance;

    private CrazyGamesPortalConfig config;
    private bool initialized;
    private bool requestedGameplayActive;
    private bool reportedGameplayActive;
    private bool applicationFocused = true;
    private bool applicationPaused;
    private bool adRequestInProgress;
    private bool rewardedAdsDisabledForSession;
    private long temporaryAdBackoffUntilUtc;

    public static event Action RewardedStateChanged;

    public static bool IsReady => instance != null && instance.initialized;
    public static bool IsAdRequestInProgress =>
        instance != null && instance.adRequestInProgress;
    public static int SalvageDropScrap =>
        instance != null && instance.config != null
            ? instance.config.SalvageDropScrap
            : 100;

    public static bool CanOfferRewardedAd
    {
        get
        {
            EnsureExists();
            return instance != null && instance.CanOfferRewardedAdInternal();
        }
    }

    public static bool ShouldHideCustomFullscreen
    {
        get
        {
            if (!IsReady || !CrazySDK.IsAvailable)
            {
                return false;
            }

            try
            {
                return string.Equals(
                           CrazySDK.Environment,
                           "crazygames",
                           StringComparison.OrdinalIgnoreCase)
                       || Application.absoluteURL.Contains(
                           "crazygames",
                           StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public static CrazyGamesPlatformService EnsureExists()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindFirstObjectByType<CrazyGamesPlatformService>();
        if (instance != null)
        {
            return instance;
        }

        GameObject serviceObject = new GameObject(
            "CrazyGames Platform Service");
        instance = serviceObject.AddComponent<CrazyGamesPlatformService>();
        return instance;
    }

    public static void SetGameplayActive(bool active)
    {
        CrazyGamesPlatformService service = EnsureExists();
        service.requestedGameplayActive = active;
        service.ApplyGameplayState();
    }

    public static void ReportHappyTime()
    {
        if (!CanUseSdk())
        {
            return;
        }

        try
        {
            CrazySDK.Game.HappyTime();
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"CrazyGames HappyTime could not be reported: {exception.Message}");
        }
    }

    public static void RequestRewardedAd(
        Action onRewardGranted,
        Action<string> onUnavailable)
    {
        CrazyGamesPlatformService service = EnsureExists();
        if (!service.CanOfferRewardedAdInternal())
        {
            onUnavailable?.Invoke("REWARD UNAVAILABLE");
            return;
        }

        service.RequestRewardedAdInternal(
            onRewardGranted,
            onUnavailable);
    }

    public static bool StorageHasKey(string key)
    {
        if (CanUseSdk())
        {
            try
            {
                return CrazySDK.Data.HasKey(key);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"CrazyGames cloud read failed; using local save. {exception.Message}");
            }
        }

        return PlayerPrefs.HasKey(key);
    }

    public static string StorageGetString(
        string key,
        string defaultValue = "")
    {
        if (CanUseSdk())
        {
            try
            {
                return CrazySDK.Data.GetString(key, defaultValue);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"CrazyGames cloud read failed; using local save. {exception.Message}");
            }
        }

        return PlayerPrefs.GetString(key, defaultValue);
    }

    public static void StorageSetString(string key, string value)
    {
        if (CanUseSdk())
        {
            try
            {
                CrazySDK.Data.SetString(key, value);
                return;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"CrazyGames cloud write failed; using local save. {exception.Message}");
            }
        }

        PlayerPrefs.SetString(key, value);
        PlayerPrefs.Save();
    }

    public static void StorageDeleteKey(string key)
    {
        if (CanUseSdk())
        {
            try
            {
                CrazySDK.Data.DeleteKey(key);
                return;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"CrazyGames cloud delete failed; using local save. {exception.Message}");
            }
        }

        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        config = Resources.Load<CrazyGamesPortalConfig>(
            ConfigResourceName);
        applicationFocused = Application.isFocused;
        StartCoroutine(InitializeSdk());
    }

    private IEnumerator InitializeSdk()
    {
        if (!CrazySDK.IsAvailable)
        {
            CompleteInitialization();
            yield break;
        }

        bool callbackReceived = false;
        try
        {
            CrazySDK.Init(() => callbackReceived = true);
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"CrazyGames SDK initialization failed; local fallback remains active. {exception.Message}",
                this);
            CompleteInitialization();
            yield break;
        }

        float timeoutAt = Time.realtimeSinceStartup
                          + InitializationTimeoutSeconds;
        while (!callbackReceived
               && Time.realtimeSinceStartup < timeoutAt)
        {
            yield return null;
        }

        if (!callbackReceived)
        {
            Debug.LogWarning(
                "CrazyGames SDK initialization timed out; local fallback remains active.",
                this);
        }

        CompleteInitialization();
    }

    private void CompleteInitialization()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        ApplyGameplayState();
        RewardedStateChanged?.Invoke();
    }

    private void OnApplicationFocus(bool focused)
    {
        applicationFocused = focused;
        ApplyGameplayState();
    }

    private void OnApplicationPause(bool paused)
    {
        applicationPaused = paused;
        ApplyGameplayState();
    }

    private void OnDestroy()
    {
        if (instance != this)
        {
            return;
        }

        if (reportedGameplayActive && CanUseSdk())
        {
            try
            {
                CrazySDK.Game.GameplayStop();
            }
            catch (Exception)
            {
                // Unity is shutting down; no recovery is necessary here.
            }
        }

        instance = null;
    }

    private void ApplyGameplayState()
    {
        if (!initialized)
        {
            return;
        }

        bool shouldBeActive = requestedGameplayActive
                              && applicationFocused
                              && !applicationPaused;
        if (reportedGameplayActive == shouldBeActive)
        {
            return;
        }

        reportedGameplayActive = shouldBeActive;
        if (!CanUseSdk())
        {
            return;
        }

        try
        {
            if (shouldBeActive)
            {
                CrazySDK.Game.GameplayStart();
            }
            else
            {
                CrazySDK.Game.GameplayStop();
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"CrazyGames gameplay state could not be reported: {exception.Message}",
                this);
        }
    }

    private bool CanOfferRewardedAdInternal()
    {
        if (!initialized
            || adRequestInProgress
            || rewardedAdsDisabledForSession
            || !CrazySDK.IsAvailable
            || !CrazySDK.IsInitialized)
        {
            return false;
        }

        bool enabledForBuild = Application.isEditor
                               || (config != null
                                   && config.RewardedAdsEnabled);
        if (!enabledForBuild)
        {
            return false;
        }

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return now >= temporaryAdBackoffUntilUtc;
    }

    private void RequestRewardedAdInternal(
        Action onRewardGranted,
        Action<string> onUnavailable)
    {
        adRequestInProgress = true;
        RewardedStateChanged?.Invoke();
        try
        {
            CrazySDK.Ad.RequestAd(
                CrazyAdType.Rewarded,
                null,
                error => HandleRewardedAdError(error, onUnavailable),
                () => HandleRewardedAdFinished(onRewardGranted));
        }
        catch (Exception exception)
        {
            adRequestInProgress = false;
            temporaryAdBackoffUntilUtc =
                DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                + TemporaryAdBackoffSeconds;
            RewardedStateChanged?.Invoke();
            onUnavailable?.Invoke("AD UNAVAILABLE");
            Debug.LogWarning(
                $"CrazyGames rewarded ad request failed: {exception.Message}",
                this);
        }
    }

    private void HandleRewardedAdError(
        SdkError error,
        Action<string> onUnavailable)
    {
        adRequestInProgress = false;
        string errorCode = error != null ? error.code : string.Empty;
        if (string.Equals(
                errorCode,
                "adsDisabledBasicLaunch",
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                errorCode,
                "adblock",
                StringComparison.OrdinalIgnoreCase))
        {
            rewardedAdsDisabledForSession = true;
        }
        else
        {
            temporaryAdBackoffUntilUtc =
                DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                + TemporaryAdBackoffSeconds;
        }

        RewardedStateChanged?.Invoke();
        onUnavailable?.Invoke(
            string.Equals(
                errorCode,
                "adblock",
                StringComparison.OrdinalIgnoreCase)
                ? "AD BLOCKER DETECTED"
                : "AD UNAVAILABLE - TRY AGAIN LATER");
    }

    private void HandleRewardedAdFinished(Action onRewardGranted)
    {
        adRequestInProgress = false;
        RewardedStateChanged?.Invoke();
        onRewardGranted?.Invoke();
    }

    private static bool CanUseSdk()
    {
        return IsReady
               && CrazySDK.IsAvailable
               && CrazySDK.IsInitialized;
    }
}
