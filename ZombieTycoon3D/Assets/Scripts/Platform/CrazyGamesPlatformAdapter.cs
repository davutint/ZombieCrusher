#if UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Collections;
using CrazyGames;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CrazyGamesPlatformAdapter :
    MonoBehaviour,
    IGamePlatformAdapter
{
    private const string ConfigResourceName = "CrazyGamesPortalConfig";
    private const float InitializationTimeoutSeconds = 8f;
    private const int TemporaryAdBackoffSeconds = 60;

    private CrazyGamesPortalConfig config;
    private bool initialized;
    private bool requestedGameplayActive;
    private bool reportedGameplayActive;
    private bool applicationFocused = true;
    private bool applicationPaused;
    private bool adRequestInProgress;
    private bool rewardedAdsDisabledForSession;
    private long temporaryAdBackoffUntilUtc;

    public event Action RewardedStateChanged;

    public bool IsReady => initialized;
    public bool IsAdRequestInProgress => adRequestInProgress;
    public bool CanOfferRewardedAd
    {
        get
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
    }
    public bool SupportsSalvageDrop => true;
    public bool SupportsLeaderboards => false;
    public bool SupportsAdFreeRewardsPurchase => false;
    public bool HasAdFreeRewards => false;
    public bool IsPurchaseInProgress => false;
    public bool CanShowPrivacyOptions => false;
    public bool SupportsLegalLinks => false;
    public bool SupportsPlayerAccountDeletion => false;
    public bool IsPlayerAccountDeletionInProgress => false;
    public bool HasPendingServiceNotification => false;
    public string AdFreeRewardsLocalizedPrice => string.Empty;
    public string PendingServiceNotificationMessage => string.Empty;
    public bool UsesTouchControls => false;

    public int SalvageDropScrap => config != null
        ? config.SalvageDropScrap
        : 100;

    public bool ShouldHideCustomFullscreen
    {
        get
        {
            if (!CanUseSdk())
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

    public void SetGameplayActive(bool active)
    {
        requestedGameplayActive = active;
        ApplyGameplayState();
    }

    public void ReportHappyTime()
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
                $"CrazyGames HappyTime could not be reported: {exception.Message}",
                this);
        }
    }

    public void RequestRewardedAd(
        Action onRewardGranted,
        Action<string> onUnavailable)
    {
        if (!CanOfferRewardedAd)
        {
            onUnavailable?.Invoke("REWARD UNAVAILABLE");
            return;
        }

        RequestRewardedAdInternal(onRewardGranted, onUnavailable);
    }

    public void ReportLifetimeZombieKills(long lifetimeZombieKills)
    {
    }

    public void ShowLeaderboards(Action<string> onUnavailable)
    {
        onUnavailable?.Invoke("LEADERBOARD UNAVAILABLE");
    }

    public void PurchaseAdFreeRewards(Action<string> onUnavailable)
    {
        onUnavailable?.Invoke("PURCHASE UNAVAILABLE");
    }

    public void RestorePurchases(Action<bool, string> onComplete)
    {
        onComplete?.Invoke(false, "RESTORE UNAVAILABLE");
    }

    public void ShowPrivacyOptions(Action<string> onComplete)
    {
        onComplete?.Invoke("PRIVACY OPTIONS UNAVAILABLE");
    }

    public void OpenPrivacyPolicy(Action<string> onUnavailable)
    {
        onUnavailable?.Invoke("PRIVACY POLICY UNAVAILABLE");
    }

    public void OpenSupport(Action<string> onUnavailable)
    {
        onUnavailable?.Invoke("SUPPORT UNAVAILABLE");
    }

    public void DeletePlayerAccount(
        Action<bool, string> onComplete)
    {
        onComplete?.Invoke(false, "ACCOUNT DELETION UNAVAILABLE");
    }

    public void AcknowledgeServiceNotification()
    {
    }

    public bool StorageHasKey(string key)
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
                    $"CrazyGames cloud read failed; using local save. {exception.Message}",
                    this);
            }
        }

        return PlayerPrefs.HasKey(key);
    }

    public string StorageGetString(
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
                    $"CrazyGames cloud read failed; using local save. {exception.Message}",
                    this);
            }
        }

        return PlayerPrefs.GetString(key, defaultValue);
    }

    public void StorageSetString(string key, string value)
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
                    $"CrazyGames cloud write failed; using local save. {exception.Message}",
                    this);
            }
        }

        PlayerPrefs.SetString(key, value);
        PlayerPrefs.Save();
    }

    public void StorageDeleteKey(string key)
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
                    $"CrazyGames cloud delete failed; using local save. {exception.Message}",
                    this);
            }
        }

        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }

    private void Awake()
    {
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
        if (!reportedGameplayActive || !CanUseSdk())
        {
            return;
        }

        try
        {
            CrazySDK.Game.GameplayStop();
        }
        catch (Exception)
        {
            // Unity is shutting down; no recovery is necessary here.
        }
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

    private bool CanUseSdk()
    {
        return initialized
               && CrazySDK.IsAvailable
               && CrazySDK.IsInitialized;
    }
}
#endif
