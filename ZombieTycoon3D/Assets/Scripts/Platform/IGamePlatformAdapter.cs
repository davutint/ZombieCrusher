using System;

public interface IGamePlatformAdapter
{
    event Action RewardedStateChanged;

    bool IsReady { get; }
    bool IsAdRequestInProgress { get; }
    bool CanOfferRewardedAd { get; }
    bool SupportsSalvageDrop { get; }
    bool SupportsLeaderboards { get; }
    bool SupportsAdFreeRewardsPurchase { get; }
    bool HasAdFreeRewards { get; }
    bool IsPurchaseInProgress { get; }
    bool CanShowPrivacyOptions { get; }
    bool SupportsLegalLinks { get; }
    bool SupportsPlayerAccountDeletion { get; }
    bool IsPlayerAccountDeletionInProgress { get; }
    bool HasPendingServiceNotification { get; }
    string AdFreeRewardsLocalizedPrice { get; }
    string PendingServiceNotificationMessage { get; }
    bool UsesTouchControls { get; }
    bool ShouldHideCustomFullscreen { get; }
    int SalvageDropScrap { get; }

    void SetGameplayActive(bool active);
    void ReportHappyTime();

    void RequestRewardedAd(
        Action onRewardGranted,
        Action<string> onUnavailable);

    void ReportLifetimeZombieKills(long lifetimeZombieKills);

    void ShowLeaderboards(Action<string> onUnavailable);

    void PurchaseAdFreeRewards(Action<string> onUnavailable);

    void RestorePurchases(Action<bool, string> onComplete);

    void ShowPrivacyOptions(Action<string> onComplete);

    void OpenPrivacyPolicy(Action<string> onUnavailable);

    void OpenSupport(Action<string> onUnavailable);

    void DeletePlayerAccount(Action<bool, string> onComplete);

    void AcknowledgeServiceNotification();

    bool StorageHasKey(string key);

    string StorageGetString(
        string key,
        string defaultValue = "");

    void StorageSetString(string key, string value);
    void StorageDeleteKey(string key);
}
