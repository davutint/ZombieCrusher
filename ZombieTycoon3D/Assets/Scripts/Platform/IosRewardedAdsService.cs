using System;
using System.Collections;
using UnityEngine;

#if UNITY_IOS
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
#endif

public sealed class IosRewardedAdsService : IDisposable
{
    private const string IosTestRewardedAdUnitId =
        "ca-app-pub-3940256099942544/1712485313";
    private const float RetryDelaySeconds = 30f;

    private readonly MonoBehaviour owner;
    private string adUnitId = string.Empty;
    private bool initialized;
    private bool loadInProgress;
    private bool showInProgress;
    private bool rewardDelivered;
    private bool requestConfigurationApplied;
    private Coroutine retryCoroutine;
    private Action rewardCallback;
    private Action<string> unavailableCallback;

#if UNITY_IOS
    private RewardedAd rewardedAd;
#endif

    public IosRewardedAdsService(MonoBehaviour owner)
    {
        this.owner = owner;
    }

    public event Action StateChanged;

    public bool IsRequestInProgress => showInProgress;

    public bool CanShow
    {
        get
        {
#if UNITY_IOS
            return initialized
                   && !showInProgress
                   && rewardedAd != null
                   && rewardedAd.CanShowAd();
#else
            return false;
#endif
        }
    }

    public bool CanShowPrivacyOptions
    {
        get
        {
#if UNITY_IOS && !UNITY_EDITOR
            return ConsentInformation.PrivacyOptionsRequirementStatus ==
                   PrivacyOptionsRequirementStatus.Required;
#else
            return false;
#endif
        }
    }

    public void Configure(IosPlatformSettings settings)
    {
        string configuredId = settings != null
            ? settings.RewardedAdUnitId
            : string.Empty;

#if UNITY_IOS && !UNITY_EDITOR
        if (Debug.isDebugBuild)
        {
            configuredId = IosTestRewardedAdUnitId;
        }

        adUnitId = configuredId;
        if (string.IsNullOrWhiteSpace(adUnitId))
        {
            Debug.LogWarning(
                "AdMob rewarded ads are disabled because the production ad unit ID is empty.");
            return;
        }

        LogDevelopment(
            $"Configured. Debug build: {Debug.isDebugBuild}, ad unit: {adUnitId}");
        GatherConsent();
#else
        adUnitId = configuredId;
#endif
    }

    public void RequestReward(
        Action onRewardGranted,
        Action<string> onUnavailable)
    {
        LogDevelopment(
            $"Reward requested. Initialized: {initialized}, loading: {loadInProgress}, " +
            $"showing: {showInProgress}, ad present: {HasRewardedAd()}, can show: {CanShow}");

        if (!CanShow)
        {
            LogDevelopment("Reward request rejected because the ad is not ready.");
            onUnavailable?.Invoke("AD IS STILL LOADING");
            return;
        }

#if UNITY_IOS && !UNITY_EDITOR
        rewardCallback = onRewardGranted;
        unavailableCallback = onUnavailable;
        rewardDelivered = false;
        showInProgress = true;
        StateChanged?.Invoke();

        try
        {
            LogDevelopment("Showing rewarded ad.");
            rewardedAd.Show(_ => RunOnMainThread(GrantReward));
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"AdMob rewarded ad could not be shown. {exception.Message}");
            FinishShow("AD UNAVAILABLE");
        }
#else
        onUnavailable?.Invoke("AD REQUIRES AN iOS DEVICE");
#endif
    }

    public void ShowPrivacyOptions(Action<string> onComplete)
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (!CanShowPrivacyOptions)
        {
            onComplete?.Invoke("PRIVACY OPTIONS ARE NOT REQUIRED");
            return;
        }

        ConsentForm.ShowPrivacyOptionsForm(error =>
            RunOnMainThread(() =>
            {
                if (error != null)
                {
                    onComplete?.Invoke(error.Message.ToUpperInvariant());
                }
                else
                {
                    onComplete?.Invoke("PRIVACY CHOICES UPDATED");
                }

                StateChanged?.Invoke();
            }));
#else
        onComplete?.Invoke("PRIVACY OPTIONS REQUIRE AN iOS DEVICE");
#endif
    }

    public void Dispose()
    {
        if (retryCoroutine != null && owner != null)
        {
            owner.StopCoroutine(retryCoroutine);
            retryCoroutine = null;
        }

#if UNITY_IOS
        DestroyRewardedAd();
#endif
        rewardCallback = null;
        unavailableCallback = null;
        StateChanged = null;
    }

#if UNITY_IOS
    private void GatherConsent()
    {
        LogDevelopment("Starting UMP consent update.");

        ConsentRequestParameters parameters = new()
        {
            TagForUnderAgeOfConsent = false
        };

        // UMP must be allowed to finish before the Mobile Ads event executor is used.
        // Queuing this entire callback through the executor before MobileAds.Initialize
        // can prevent the consent -> SDK initialization handoff on iOS.
        ConsentInformation.Update(parameters, updateError =>
        {
            LogDevelopment(
                $"UMP update completed. Status: {ConsentInformation.ConsentStatus}, " +
                $"can request ads: {ConsentInformation.CanRequestAds()}");

            if (updateError != null)
            {
                Debug.LogWarning(
                    $"AdMob consent update failed. {updateError.Message}");
                TryInitializeAdsAfterConsent("UMP update error");
                return;
            }

            if (ConsentInformation.CanRequestAds())
            {
                InitializeAds();
                return;
            }

            LogDevelopment("Loading the UMP consent form because consent is required.");
            ConsentForm.LoadAndShowConsentFormIfRequired(showError =>
            {
                LogDevelopment(
                    $"UMP form completed. Status: {ConsentInformation.ConsentStatus}, " +
                    $"can request ads: {ConsentInformation.CanRequestAds()}");

                if (showError != null)
                {
                    Debug.LogWarning(
                        $"AdMob consent form failed. {showError.Message}");
                }

                TryInitializeAdsAfterConsent("UMP form completion");
            });
        });
    }

    private void TryInitializeAdsAfterConsent(string source)
    {
        if (ConsentInformation.CanRequestAds())
        {
            LogDevelopment($"Consent permits ads after {source}; initializing SDK.");
            InitializeAds();
            return;
        }

        LogDevelopment($"Ads are still not permitted after {source}.");
    }

    private void InitializeAds()
    {
        if (initialized)
        {
            LogDevelopment("Mobile Ads SDK is already initialized; loading rewarded ad.");
            LoadRewardedAd();
            return;
        }

        ApplyReleasePrivacyConfiguration();
        LogDevelopment("Initializing Google Mobile Ads SDK.");
        MobileAds.Initialize(initializationStatus =>
        {
            if (initializationStatus == null)
            {
                Debug.LogWarning("Google Mobile Ads SDK initialization returned no status.");
                return;
            }

            LogDevelopment("Google Mobile Ads SDK initialization completed.");
            RunOnMainThread(() =>
            {
                initialized = true;
                StateChanged?.Invoke();
                LoadRewardedAd();
            });
        });
    }

    private void ApplyReleasePrivacyConfiguration()
    {
        if (requestConfigurationApplied)
        {
            return;
        }

        MobileAds.SetRequestConfiguration(new RequestConfiguration
        {
            MaxAdContentRating = MaxAdContentRating.T,
            PublisherFirstPartyIdEnabled = false,
            PublisherPrivacyPersonalizationState =
                PublisherPrivacyPersonalizationState.Disabled
        });
        requestConfigurationApplied = true;
    }

    private void LoadRewardedAd()
    {
        if (!initialized
            || loadInProgress
            || showInProgress
            || string.IsNullOrWhiteSpace(adUnitId))
        {
            return;
        }

        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            StateChanged?.Invoke();
            return;
        }

        DestroyRewardedAd();
        loadInProgress = true;
        StateChanged?.Invoke();
        LogDevelopment($"Loading rewarded ad with unit: {adUnitId}");

        RewardedAd.Load(
            adUnitId,
            new AdRequest(),
            (ad, error) => RunOnMainThread(() =>
            {
                loadInProgress = false;
                if (error != null || ad == null)
                {
                    Debug.LogWarning(
                        $"AdMob rewarded ad failed to load. {error}");
                    StateChanged?.Invoke();
                    ScheduleRetry();
                    return;
                }

                rewardedAd = ad;
                RegisterAdEvents(ad);
                LogDevelopment(
                    $"Rewarded ad loaded successfully. Response: {ad.GetResponseInfo()}");
                StateChanged?.Invoke();
            }));
    }

    private void RegisterAdEvents(RewardedAd ad)
    {
        ad.OnAdFullScreenContentOpened += () =>
            LogDevelopment("Rewarded ad opened full-screen content.");
        ad.OnAdImpressionRecorded += () =>
            LogDevelopment("Rewarded ad impression recorded.");
        ad.OnAdFullScreenContentClosed += () =>
        {
            LogDevelopment("Rewarded ad closed full-screen content.");
            RunOnMainThread(() => FinishShow(
                rewardDelivered ? null : "REWARD NOT COMPLETED"));
        };
        ad.OnAdFullScreenContentFailed += error =>
            RunOnMainThread(() =>
            {
                Debug.LogWarning(
                    $"AdMob rewarded ad failed to open. {error}");
                FinishShow("AD UNAVAILABLE");
            });
    }

    private void GrantReward()
    {
        if (!showInProgress || rewardDelivered)
        {
            return;
        }

        rewardDelivered = true;
        LogDevelopment("Reward callback received; granting the double-scrap reward.");
        Action callback = rewardCallback;
        rewardCallback = null;
        callback?.Invoke();
    }

    private void FinishShow(string errorMessage)
    {
        if (!showInProgress)
        {
            return;
        }

        Action<string> errorCallback = unavailableCallback;
        bool shouldReportError = !string.IsNullOrWhiteSpace(errorMessage)
                                 && !rewardDelivered;

        showInProgress = false;
        rewardDelivered = false;
        rewardCallback = null;
        unavailableCallback = null;
        DestroyRewardedAd();
        StateChanged?.Invoke();

        if (shouldReportError)
        {
            errorCallback?.Invoke(errorMessage);
        }

        LoadRewardedAd();
    }

    private void DestroyRewardedAd()
    {
        if (rewardedAd == null)
        {
            return;
        }

        rewardedAd.Destroy();
        rewardedAd = null;
    }

#endif

    private bool HasRewardedAd()
    {
#if UNITY_IOS
        return rewardedAd != null;
#else
        return false;
#endif
    }

    private void ScheduleRetry()
    {
        if (owner == null || retryCoroutine != null)
        {
            return;
        }

        retryCoroutine = owner.StartCoroutine(RetryAfterDelay());
    }

    private IEnumerator RetryAfterDelay()
    {
        yield return new WaitForSecondsRealtime(RetryDelaySeconds);
        retryCoroutine = null;
#if UNITY_IOS
        LoadRewardedAd();
#endif
    }

    private static void RunOnMainThread(Action action)
    {
#if UNITY_IOS
        MobileAdsEventExecutor.ExecuteInUpdate(action);
#else
        action?.Invoke();
#endif
    }

    private static void LogDevelopment(string message)
    {
        if (Debug.isDebugBuild)
        {
            Debug.Log($"[AdMob Rewarded] {message}");
        }
    }
}
