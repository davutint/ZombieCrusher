using System;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
[DisallowMultipleComponent]
public sealed class GamePlatformService : MonoBehaviour
{
    private static GamePlatformService instance;
    private static bool isShuttingDown;
    private IGamePlatformAdapter adapter;

    public static event Action RewardedStateChanged;

    public static bool IsReady => Adapter?.IsReady ?? false;

    public static bool IsAdRequestInProgress =>
        Adapter?.IsAdRequestInProgress ?? false;

    public static bool CanOfferRewardedAd =>
        Adapter?.CanOfferRewardedAd ?? false;

    public static bool SupportsSalvageDrop =>
        Adapter?.SupportsSalvageDrop ?? false;

    public static bool SupportsLeaderboards =>
        Adapter?.SupportsLeaderboards ?? false;

    public static bool SupportsAdFreeRewardsPurchase =>
        Adapter?.SupportsAdFreeRewardsPurchase ?? false;

    public static bool HasAdFreeRewards =>
        Adapter?.HasAdFreeRewards ?? false;

    public static bool IsPurchaseInProgress =>
        Adapter?.IsPurchaseInProgress ?? false;

    public static bool CanShowPrivacyOptions =>
        Adapter?.CanShowPrivacyOptions ?? false;

    public static bool SupportsLegalLinks =>
        Adapter?.SupportsLegalLinks ?? false;

    public static bool SupportsPlayerAccountDeletion =>
        Adapter?.SupportsPlayerAccountDeletion ?? false;

    public static bool IsPlayerAccountDeletionInProgress =>
        Adapter?.IsPlayerAccountDeletionInProgress ?? false;

    public static bool HasPendingServiceNotification =>
        Adapter?.HasPendingServiceNotification ?? false;

    public static string AdFreeRewardsLocalizedPrice =>
        Adapter?.AdFreeRewardsLocalizedPrice ?? string.Empty;

    public static string PendingServiceNotificationMessage =>
        Adapter?.PendingServiceNotificationMessage ?? string.Empty;

    public static bool UsesTouchControls =>
        Adapter?.UsesTouchControls ?? false;

    public static bool ShouldHideCustomFullscreen =>
        Adapter?.ShouldHideCustomFullscreen ?? false;

    public static int SalvageDropScrap =>
        Adapter?.SalvageDropScrap ?? 0;

    private static IGamePlatformAdapter Adapter =>
        EnsureExists()?.adapter;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        isShuttingDown = false;
        RewardedStateChanged = null;
    }

    public static GamePlatformService EnsureExists()
    {
        if (isShuttingDown)
        {
            return null;
        }

        if (instance != null)
        {
            return instance;
        }

        instance = FindFirstObjectByType<GamePlatformService>();
        if (instance != null)
        {
            instance.EnsureAdapter();
            return instance;
        }

        GameObject serviceObject = new GameObject(
            "Game Platform Service");
        instance = serviceObject.AddComponent<GamePlatformService>();
        return instance;
    }

    public static void SetGameplayActive(bool active)
    {
        Adapter?.SetGameplayActive(active);
    }

    public static void ReportHappyTime()
    {
        Adapter?.ReportHappyTime();
    }

    public static void RequestRewardedAd(
        Action onRewardGranted,
        Action<string> onUnavailable)
    {
        IGamePlatformAdapter currentAdapter = Adapter;
        if (currentAdapter == null)
        {
            onUnavailable?.Invoke("REWARD UNAVAILABLE");
            return;
        }

        currentAdapter.RequestRewardedAd(onRewardGranted, onUnavailable);
    }

    public static void ReportLifetimeZombieKills(
        long lifetimeZombieKills)
    {
        Adapter?.ReportLifetimeZombieKills(lifetimeZombieKills);
    }

    public static void ShowLeaderboards(Action<string> onUnavailable)
    {
        IGamePlatformAdapter currentAdapter = Adapter;
        if (currentAdapter == null)
        {
            onUnavailable?.Invoke("LEADERBOARD UNAVAILABLE");
            return;
        }

        currentAdapter.ShowLeaderboards(onUnavailable);
    }

    public static void PurchaseAdFreeRewards(
        Action<string> onUnavailable)
    {
        IGamePlatformAdapter currentAdapter = Adapter;
        if (currentAdapter == null)
        {
            onUnavailable?.Invoke("PURCHASE UNAVAILABLE");
            return;
        }

        currentAdapter.PurchaseAdFreeRewards(onUnavailable);
    }

    public static void RestorePurchases(
        Action<bool, string> onComplete)
    {
        IGamePlatformAdapter currentAdapter = Adapter;
        if (currentAdapter == null)
        {
            onComplete?.Invoke(false, "RESTORE UNAVAILABLE");
            return;
        }

        currentAdapter.RestorePurchases(onComplete);
    }

    public static void ShowPrivacyOptions(Action<string> onComplete)
    {
        IGamePlatformAdapter currentAdapter = Adapter;
        if (currentAdapter == null)
        {
            onComplete?.Invoke("PRIVACY OPTIONS UNAVAILABLE");
            return;
        }

        currentAdapter.ShowPrivacyOptions(onComplete);
    }

    public static void OpenPrivacyPolicy(
        Action<string> onUnavailable)
    {
        IGamePlatformAdapter currentAdapter = Adapter;
        if (currentAdapter == null)
        {
            onUnavailable?.Invoke("PRIVACY POLICY UNAVAILABLE");
            return;
        }

        currentAdapter.OpenPrivacyPolicy(onUnavailable);
    }

    public static void OpenSupport(Action<string> onUnavailable)
    {
        IGamePlatformAdapter currentAdapter = Adapter;
        if (currentAdapter == null)
        {
            onUnavailable?.Invoke("SUPPORT UNAVAILABLE");
            return;
        }

        currentAdapter.OpenSupport(onUnavailable);
    }

    public static void DeletePlayerAccount(
        Action<bool, string> onComplete)
    {
        IGamePlatformAdapter currentAdapter = Adapter;
        if (currentAdapter == null)
        {
            onComplete?.Invoke(false, "ACCOUNT DELETION UNAVAILABLE");
            return;
        }

        currentAdapter.DeletePlayerAccount(onComplete);
    }

    public static void AcknowledgeServiceNotification()
    {
        Adapter?.AcknowledgeServiceNotification();
    }

    public static bool StorageHasKey(string key)
    {
        IGamePlatformAdapter currentAdapter = Adapter;
        return currentAdapter != null
            ? currentAdapter.StorageHasKey(key)
            : PlayerPrefs.HasKey(key);
    }

    public static string StorageGetString(
        string key,
        string defaultValue = "")
    {
        IGamePlatformAdapter currentAdapter = Adapter;
        return currentAdapter != null
            ? currentAdapter.StorageGetString(key, defaultValue)
            : PlayerPrefs.GetString(key, defaultValue);
    }

    public static void StorageSetString(string key, string value)
    {
        IGamePlatformAdapter currentAdapter = Adapter;
        if (currentAdapter != null)
        {
            currentAdapter.StorageSetString(key, value);
            return;
        }

        PlayerPrefs.SetString(key, value ?? string.Empty);
        PlayerPrefs.Save();
    }

    public static void StorageDeleteKey(string key)
    {
        IGamePlatformAdapter currentAdapter = Adapter;
        if (currentAdapter != null)
        {
            currentAdapter.StorageDeleteKey(key);
            return;
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
        EnsureAdapter();
    }

    private void OnDestroy()
    {
        if (instance != this)
        {
            return;
        }

        if (adapter != null)
        {
            adapter.RewardedStateChanged -=
                HandleRewardedStateChanged;
        }

        isShuttingDown = true;
        RewardedStateChanged = null;
        instance = null;
    }

    private void OnApplicationQuit()
    {
        isShuttingDown = true;
    }

    private void EnsureAdapter()
    {
        if (adapter != null)
        {
            return;
        }

#if UNITY_WEBGL
        adapter = GetComponent<CrazyGamesPlatformAdapter>();
        if (adapter == null)
        {
            adapter = gameObject.AddComponent<CrazyGamesPlatformAdapter>();
        }
#elif UNITY_IOS
        adapter = GetComponent<IosPlatformAdapter>();
        if (adapter == null)
        {
            adapter = gameObject.AddComponent<IosPlatformAdapter>();
        }
#else
        adapter = GetComponent<LocalPlatformAdapter>();
        if (adapter == null)
        {
            adapter = gameObject.AddComponent<LocalPlatformAdapter>();
        }
#endif

        adapter.RewardedStateChanged += HandleRewardedStateChanged;
    }

    private static void HandleRewardedStateChanged()
    {
        RewardedStateChanged?.Invoke();
    }
}
