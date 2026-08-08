using System;
using UnityEngine;

[DisallowMultipleComponent]
public class LocalPlatformAdapter :
    MonoBehaviour,
    IGamePlatformAdapter
{
    public event Action RewardedStateChanged;

    public virtual bool IsReady => true;
    public virtual bool IsAdRequestInProgress => false;
    public virtual bool CanOfferRewardedAd => false;
    public virtual bool SupportsSalvageDrop => false;
    public virtual bool SupportsLeaderboards => false;
    public virtual bool SupportsAdFreeRewardsPurchase => false;
    public virtual bool HasAdFreeRewards => false;
    public virtual bool IsPurchaseInProgress => false;
    public virtual bool CanShowPrivacyOptions => false;
    public virtual bool SupportsLegalLinks => false;
    public virtual bool SupportsPlayerAccountDeletion => false;
    public virtual bool IsPlayerAccountDeletionInProgress => false;
    public virtual bool HasPendingServiceNotification => false;
    public virtual string AdFreeRewardsLocalizedPrice => string.Empty;
    public virtual string PendingServiceNotificationMessage =>
        string.Empty;
    public virtual bool UsesTouchControls => false;
    public virtual bool ShouldHideCustomFullscreen => false;
    public virtual int SalvageDropScrap => 0;

    public virtual void SetGameplayActive(bool active)
    {
    }

    public virtual void ReportHappyTime()
    {
    }

    public virtual void RequestRewardedAd(
        Action onRewardGranted,
        Action<string> onUnavailable)
    {
        onUnavailable?.Invoke("REWARD UNAVAILABLE");
    }

    public virtual void ReportLifetimeZombieKills(
        long lifetimeZombieKills)
    {
    }

    public virtual void ShowLeaderboards(
        Action<string> onUnavailable)
    {
        onUnavailable?.Invoke("LEADERBOARD UNAVAILABLE");
    }

    public virtual void PurchaseAdFreeRewards(
        Action<string> onUnavailable)
    {
        onUnavailable?.Invoke("PURCHASE UNAVAILABLE");
    }

    public virtual void RestorePurchases(
        Action<bool, string> onComplete)
    {
        onComplete?.Invoke(false, "RESTORE UNAVAILABLE");
    }

    public virtual void ShowPrivacyOptions(Action<string> onComplete)
    {
        onComplete?.Invoke("PRIVACY OPTIONS UNAVAILABLE");
    }

    public virtual void OpenPrivacyPolicy(
        Action<string> onUnavailable)
    {
        onUnavailable?.Invoke("PRIVACY POLICY UNAVAILABLE");
    }

    public virtual void OpenSupport(Action<string> onUnavailable)
    {
        onUnavailable?.Invoke("SUPPORT UNAVAILABLE");
    }

    public virtual void DeletePlayerAccount(
        Action<bool, string> onComplete)
    {
        onComplete?.Invoke(false, "ACCOUNT DELETION UNAVAILABLE");
    }

    public virtual void AcknowledgeServiceNotification()
    {
    }

    public virtual bool StorageHasKey(string key)
    {
        return PlayerPrefs.HasKey(key);
    }

    public virtual string StorageGetString(
        string key,
        string defaultValue = "")
    {
        return PlayerPrefs.GetString(key, defaultValue);
    }

    public virtual void StorageSetString(string key, string value)
    {
        PlayerPrefs.SetString(key, value);
        PlayerPrefs.Save();
    }

    public virtual void StorageDeleteKey(string key)
    {
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }

    protected void NotifyRewardedStateChanged()
    {
        RewardedStateChanged?.Invoke();
    }
}
