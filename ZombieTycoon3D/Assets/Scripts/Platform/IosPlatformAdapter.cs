using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using Unity.Services.Core;
using UnityEngine;

#if UNITY_IOS
using Apple.Core.Runtime;
using Apple.GameKit;
using Apple.GameKit.Leaderboards;
using Apple.GameKit.Players;
#endif

[DisallowMultipleComponent]
public sealed class IosPlatformAdapter : LocalPlatformAdapter
{
    private const string TimestampSuffix = ".cloud-save-timestamp";
    private const string ProgressionCloudKey =
        "zt3d_garage_progression_v3";
    private const string DsaLastReadNotificationDateKey =
        "zt3d.ugs.dsa-last-read-notification-date";

    [Serializable]
    private sealed class CloudStorageEnvelope
    {
        public string value;
        public long modifiedAtUnixMilliseconds;
    }

    private readonly Dictionary<string, CloudStorageEnvelope>
        pendingCloudWrites = new();
    private readonly Dictionary<string, string> cloudWriteLocks = new();
    private readonly HashSet<string> cloudConflictKeys = new();
    private readonly List<Notification> pendingServiceNotifications =
        new();

    private IosPlatformSettings settings;
    private IosRewardedAdsService rewardedAds;
    private IosIapService iap;
    private bool initialized;
    private bool cloudReady;
    private bool cloudWriteLoopRunning;
    private bool gameCenterAuthenticationStarted;
    private bool gameCenterAuthenticated;
    private bool leaderboardSubmissionRunning;
    private bool playerAccountDeletionInProgress;
    private long pendingLifetimeZombieKills;

#if UNITY_IOS
    private GKLeaderboard lifetimeZombieKillsLeaderboard;
#endif

    public override bool IsReady => initialized;
    public override bool IsAdRequestInProgress =>
        rewardedAds != null && rewardedAds.IsRequestInProgress;
    public override bool CanOfferRewardedAd =>
        HasAdFreeRewards || (rewardedAds != null && rewardedAds.CanShow);
    public override bool UsesTouchControls => true;
    public override bool ShouldHideCustomFullscreen => true;

    public override bool SupportsAdFreeRewardsPurchase =>
        iap != null && iap.IsSupported;

    public override bool HasAdFreeRewards =>
        iap != null && iap.HasEntitlement;

    public override bool IsPurchaseInProgress =>
        iap != null && iap.IsPurchaseInProgress;

    public override bool CanShowPrivacyOptions =>
        rewardedAds != null && rewardedAds.CanShowPrivacyOptions;

    public override bool SupportsLegalLinks =>
        settings != null
        && IsHttpsUrl(settings.PrivacyPolicyUrl)
        && IsHttpsUrl(settings.SupportUrl);

    public override bool SupportsPlayerAccountDeletion => true;

    public override bool IsPlayerAccountDeletionInProgress =>
        playerAccountDeletionInProgress;

    public override bool HasPendingServiceNotification =>
        pendingServiceNotifications.Count > 0;

    public override string PendingServiceNotificationMessage =>
        BuildPendingServiceNotificationMessage();

    public override string AdFreeRewardsLocalizedPrice =>
        iap != null ? iap.LocalizedPrice : string.Empty;

    public override bool SupportsLeaderboards =>
        settings != null
        && !string.IsNullOrWhiteSpace(
            settings.LifetimeZombieKillsLeaderboardId);

    private void Awake()
    {
        settings = IosPlatformSettings.Load();
        rewardedAds = new IosRewardedAdsService(this);
        iap = new IosIapService();
        rewardedAds.StateChanged += HandlePlatformStateChanged;
        iap.StateChanged += HandlePlatformStateChanged;
        rewardedAds.Configure(settings);
        iap.Configure(settings);

#if UNITY_IOS && !UNITY_EDITOR
        _ = InitializeIosServicesAsync();
#else
        initialized = true;
#endif
    }

    private void OnDestroy()
    {
        if (rewardedAds != null)
        {
            rewardedAds.StateChanged -= HandlePlatformStateChanged;
            rewardedAds.Dispose();
        }

        if (iap != null)
        {
            iap.StateChanged -= HandlePlatformStateChanged;
            iap.Dispose();
        }
    }

    public override void RequestRewardedAd(
        Action onRewardGranted,
        Action<string> onUnavailable)
    {
        if (HasAdFreeRewards)
        {
            onRewardGranted?.Invoke();
            return;
        }

        if (rewardedAds == null)
        {
            onUnavailable?.Invoke("AD UNAVAILABLE");
            return;
        }

        rewardedAds.RequestReward(onRewardGranted, onUnavailable);
    }

    public override void PurchaseAdFreeRewards(
        Action<string> onUnavailable)
    {
        if (iap == null)
        {
            onUnavailable?.Invoke("PURCHASE UNAVAILABLE");
            return;
        }

        iap.Purchase(onUnavailable);
    }

    public override void RestorePurchases(
        Action<bool, string> onComplete)
    {
        if (iap == null)
        {
            onComplete?.Invoke(false, "RESTORE UNAVAILABLE");
            return;
        }

        iap.Restore(onComplete);
    }

    public override void ShowPrivacyOptions(Action<string> onComplete)
    {
        if (rewardedAds == null)
        {
            onComplete?.Invoke("PRIVACY OPTIONS UNAVAILABLE");
            return;
        }

        rewardedAds.ShowPrivacyOptions(onComplete);
    }

    public override void OpenPrivacyPolicy(
        Action<string> onUnavailable)
    {
        OpenConfiguredUrl(
            settings != null ? settings.PrivacyPolicyUrl : string.Empty,
            "PRIVACY POLICY UNAVAILABLE",
            onUnavailable);
    }

    public override void OpenSupport(Action<string> onUnavailable)
    {
        OpenConfiguredUrl(
            settings != null ? settings.SupportUrl : string.Empty,
            "SUPPORT UNAVAILABLE",
            onUnavailable);
    }

    public override void DeletePlayerAccount(
        Action<bool, string> onComplete)
    {
        if (playerAccountDeletionInProgress)
        {
            onComplete?.Invoke(false, "ACCOUNT DELETION IN PROGRESS");
            return;
        }

#if UNITY_IOS && !UNITY_EDITOR
        _ = DeletePlayerAccountAsync(onComplete);
#else
        onComplete?.Invoke(
            false,
            "ACCOUNT DELETION REQUIRES AN iOS DEVICE");
#endif
    }

    public override void AcknowledgeServiceNotification()
    {
        long latestReadDate = ReadLastServiceNotificationDate();
        foreach (Notification notification in pendingServiceNotifications)
        {
            if (long.TryParse(notification.CreatedAt, out long createdAt))
            {
                latestReadDate = Math.Max(latestReadDate, createdAt);
            }
        }

        PlayerPrefs.SetString(
            DsaLastReadNotificationDateKey,
            latestReadDate.ToString());
        PlayerPrefs.Save();
        pendingServiceNotifications.Clear();
        NotifyRewardedStateChanged();
    }

    public override bool StorageHasKey(string key)
    {
        return PlayerPrefs.HasKey(key);
    }

    public override string StorageGetString(
        string key,
        string defaultValue = "")
    {
        return PlayerPrefs.GetString(key, defaultValue);
    }

    public override void StorageSetString(string key, string value)
    {
        string safeValue = value ?? string.Empty;
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        PlayerPrefs.SetString(key, safeValue);
        PlayerPrefs.SetString(
            key + TimestampSuffix,
            timestamp.ToString());
        PlayerPrefs.Save();

        if (cloudReady && IsCloudSyncedKey(key))
        {
            QueueCloudWrite(key, safeValue, timestamp);
        }
    }

    public override void StorageDeleteKey(string key)
    {
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.DeleteKey(key + TimestampSuffix);
        PlayerPrefs.Save();

        if (cloudReady && IsCloudSyncedKey(key))
        {
            pendingCloudWrites.Remove(key);
            _ = DeleteCloudKeyAsync(key);
        }
    }

    public override void ReportLifetimeZombieKills(
        long lifetimeZombieKills)
    {
        pendingLifetimeZombieKills = Math.Max(
            pendingLifetimeZombieKills,
            Math.Max(0L, lifetimeZombieKills));
        TryReportPendingLeaderboardScore();
    }

    public override void ShowLeaderboards(
        Action<string> onUnavailable)
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (!SupportsLeaderboards || !gameCenterAuthenticated)
        {
            onUnavailable?.Invoke("GAME CENTER UNAVAILABLE");
            return;
        }

        _ = ShowLeaderboardsAsync(onUnavailable);
#else
        onUnavailable?.Invoke("GAME CENTER REQUIRES AN iOS DEVICE");
#endif
    }

    private async Task InitializeIosServicesAsync()
    {
        try
        {
            if (UnityServices.State
                != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            await AuthenticateGameCenterAsync();
            await SignInForCloudSaveAsync();
            await RefreshServiceNotificationsAsync();

            await SynchronizeCloudStorageAsync();
            cloudReady = true;
            if (pendingCloudWrites.Count > 0 && !cloudWriteLoopRunning)
            {
                _ = FlushCloudWritesAsync();
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"Unity Cloud Save is unavailable; local iOS save remains active. {exception.Message}",
                this);
        }
        finally
        {
            initialized = true;
            TryReportPendingLeaderboardScore();
            NotifyRewardedStateChanged();
        }
    }

    private async Task DeletePlayerAccountAsync(
        Action<bool, string> onComplete)
    {
        playerAccountDeletionInProgress = true;
        bool restoreCloudReadyOnFailure = cloudReady;
        bool succeeded = false;
        string resultMessage;
        try
        {
            if (UnityServices.State
                != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                throw new InvalidOperationException(
                    "The cloud account is not signed in. Try again when online.");
            }

            cloudReady = false;
            pendingCloudWrites.Clear();
            while (cloudWriteLoopRunning)
            {
                await Task.Yield();
            }

            HashSet<string> progressionKeys = new()
            {
                ProgressionCloudKey
            };
            Dictionary<string, Item> cloudItems =
                await CloudSaveService.Instance.Data.Player.LoadAsync(
                    progressionKeys);
            if (cloudItems.ContainsKey(ProgressionCloudKey))
            {
                await CloudSaveService.Instance.Data.Player.DeleteAsync(
                    ProgressionCloudKey);
            }

            await AuthenticationService.Instance.DeleteAccountAsync();

            AuthenticationService.Instance.ClearSessionToken();
            DeleteLocalSyncedKey(ProgressionCloudKey);
            pendingServiceNotifications.Clear();
            PlayerPrefs.DeleteKey(DsaLastReadNotificationDateKey);
            PlayerPrefs.Save();
            succeeded = true;
            resultMessage = "ACCOUNT AND CLOUD SAVE DELETED";
        }
        catch (Exception exception)
        {
            cloudReady = restoreCloudReadyOnFailure;
            Debug.LogWarning(
                $"Player account deletion failed. {exception.Message}",
                this);
            resultMessage =
                "COULD NOT DELETE ACCOUNT — CHECK CONNECTION AND TRY AGAIN";
        }
        finally
        {
            playerAccountDeletionInProgress = false;
        }

        onComplete?.Invoke(succeeded, resultMessage);
    }

    private async Task SynchronizeCloudStorageAsync()
    {
        HashSet<string> keys = new() { ProgressionCloudKey };
        Dictionary<string, Item> cloudItems =
            await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

        foreach (string key in keys)
        {
            bool hasLocal = PlayerPrefs.HasKey(key);
            long localTimestamp = ReadLocalTimestamp(key);

            if (!cloudItems.TryGetValue(key, out Item cloudItem))
            {
                cloudWriteLocks.Remove(key);
                cloudConflictKeys.Remove(key);
                if (hasLocal)
                {
                    QueueCloudWrite(
                        key,
                        PlayerPrefs.GetString(key),
                        localTimestamp > 0L
                            ? localTimestamp
                            : DateTimeOffset.UtcNow
                                .ToUnixTimeMilliseconds());
                }

                continue;
            }

            cloudWriteLocks[key] = cloudItem.WriteLock;
            cloudConflictKeys.Remove(key);

            string cloudJson = cloudItem.Value.GetAs<string>();
            CloudStorageEnvelope cloudEnvelope =
                ParseCloudEnvelope(cloudJson);
            string localValue = hasLocal
                ? PlayerPrefs.GetString(key)
                : string.Empty;
            bool localIsPreferred = hasLocal
                && localTimestamp
                > cloudEnvelope.modifiedAtUnixMilliseconds;
            string preferredValue = localIsPreferred
                ? localValue
                : cloudEnvelope.value;
            string secondaryValue = localIsPreferred
                ? cloudEnvelope.value
                : localValue;
            string reconciledValue = hasLocal
                ? GarageEconomyController.ReconcileCloudProgression(
                    preferredValue,
                    secondaryValue)
                : cloudEnvelope.value ?? string.Empty;

            if (localIsPreferred)
            {
                PlayerPrefs.SetString(key, reconciledValue);
                QueueCloudWrite(
                    key,
                    reconciledValue,
                    localTimestamp);
                continue;
            }

            PlayerPrefs.SetString(key, reconciledValue);
            PlayerPrefs.SetString(
                key + TimestampSuffix,
                cloudEnvelope.modifiedAtUnixMilliseconds.ToString());
            if (!string.Equals(
                    reconciledValue,
                    cloudEnvelope.value ?? string.Empty,
                    StringComparison.Ordinal))
            {
                long reconciliationTimestamp = DateTimeOffset.UtcNow
                    .ToUnixTimeMilliseconds();
                PlayerPrefs.SetString(
                    key + TimestampSuffix,
                    reconciliationTimestamp.ToString());
                QueueCloudWrite(
                    key,
                    reconciledValue,
                    reconciliationTimestamp);
            }
        }

        PlayerPrefs.Save();
    }

    private void QueueCloudWrite(
        string key,
        string value,
        long timestamp)
    {
        pendingCloudWrites[key] = new CloudStorageEnvelope
        {
            value = value ?? string.Empty,
            modifiedAtUnixMilliseconds = Math.Max(0L, timestamp)
        };

        if (!cloudWriteLoopRunning
            && !cloudConflictKeys.Contains(key))
        {
            _ = FlushCloudWritesAsync();
        }
    }

    private async Task FlushCloudWritesAsync()
    {
        cloudWriteLoopRunning = true;
        try
        {
            while (cloudReady && pendingCloudWrites.Count > 0)
            {
                Dictionary<string, CloudStorageEnvelope> writesToSave =
                    new(pendingCloudWrites);
                Dictionary<string, SaveItem> payload = new();
                foreach (KeyValuePair<string, CloudStorageEnvelope> pair
                         in writesToSave)
                {
                    cloudWriteLocks.TryGetValue(
                        pair.Key,
                        out string writeLock);
                    payload[pair.Key] = new SaveItem(
                        JsonUtility.ToJson(pair.Value),
                        writeLock);
                }

                Dictionary<string, string> updatedWriteLocks =
                    await CloudSaveService.Instance.Data.Player.SaveAsync(
                        payload);

                foreach (KeyValuePair<string, CloudStorageEnvelope> pair
                         in writesToSave)
                {
                    if (updatedWriteLocks.TryGetValue(
                            pair.Key,
                            out string updatedWriteLock))
                    {
                        cloudWriteLocks[pair.Key] = updatedWriteLock;
                    }

                    if (pendingCloudWrites.TryGetValue(
                            pair.Key,
                            out CloudStorageEnvelope currentWrite)
                        && ReferenceEquals(currentWrite, pair.Value))
                    {
                        pendingCloudWrites.Remove(pair.Key);
                    }
                }
            }
        }
        catch (CloudSaveConflictException exception)
        {
            if (exception.Details != null)
            {
                foreach (CloudSaveConflictErrorDetail detail
                         in exception.Details)
                {
                    if (!string.IsNullOrWhiteSpace(detail.Key))
                    {
                        cloudConflictKeys.Add(detail.Key);
                    }
                }
            }

            if (cloudConflictKeys.Count == 0)
            {
                foreach (string key in pendingCloudWrites.Keys)
                {
                    cloudConflictKeys.Add(key);
                }
            }

            Debug.LogWarning(
                "Unity Cloud Save detected a newer concurrent write. "
                + "The local pending save was preserved and will be "
                + "reconciled the next time the app starts; no cloud "
                + "value was overwritten.",
                this);
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"Unity Cloud Save write failed; the local save is still intact. {exception.Message}",
                this);
        }
        finally
        {
            cloudWriteLoopRunning = false;
        }
    }

    private static async Task DeleteCloudKeyAsync(string key)
    {
        try
        {
            await CloudSaveService.Instance.Data.Player.DeleteAsync(key);
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"Unity Cloud Save delete failed. {exception.Message}");
        }
    }

    private async Task AuthenticateGameCenterAsync()
    {
        if (gameCenterAuthenticationStarted)
        {
            return;
        }

        gameCenterAuthenticationStarted = true;

#if UNITY_IOS
        try
        {
            GKLocalPlayer localPlayer = await GKLocalPlayer.Authenticate();
            gameCenterAuthenticated =
                localPlayer != null && localPlayer.IsAuthenticated;

            if (!gameCenterAuthenticated)
            {
                Debug.LogWarning(
                    "Game Center authentication did not complete; local play remains available.",
                    this);
                return;
            }

            Debug.Log(
                "Game Center authentication succeeded.",
                this);

        }
        catch (Exception exception)
        {
            gameCenterAuthenticated = false;
            Debug.LogWarning(
                $"Game Center authentication failed; local play remains available. {exception.Message}",
                this);
        }
#endif
    }

    private async Task SignInForCloudSaveAsync()
    {
#if UNITY_IOS
        if (gameCenterAuthenticated)
        {
            try
            {
                GKIdentityVerificationResponse identity =
                    await GKLocalPlayer.Local.FetchItems();
                string signature = Convert.ToBase64String(
                    identity.GetSignature());
                string teamPlayerId =
                    GKLocalPlayer.Local.TeamPlayerId;
                string salt = Convert.ToBase64String(
                    identity.GetSalt());

                if (AuthenticationService.Instance.IsSignedIn)
                {
                    string linkedGameCenterId =
                        AuthenticationService.Instance.PlayerInfo
                            ?.GetAppleGameCenterId();
                    if (string.Equals(
                            linkedGameCenterId,
                            teamPlayerId,
                            StringComparison.Ordinal))
                    {
                        Debug.Log(
                            "Unity Authentication is already signed in with the current Game Center identity.",
                            this);
                        return;
                    }

                    try
                    {
                        await AuthenticationService.Instance
                            .LinkWithAppleGameCenterAsync(
                                signature,
                                teamPlayerId,
                                identity.PublicKeyUrl,
                                salt,
                                identity.Timestamp);
                        Debug.Log(
                            "The existing Unity Authentication player was linked to Game Center.",
                            this);
                        return;
                    }
                    catch (AuthenticationException exception)
                        when (exception.ErrorCode ==
                              AuthenticationErrorCodes
                                  .AccountAlreadyLinked)
                    {
                        // The Game Center identity already belongs to its
                        // canonical UGS player. Switch away from the temporary
                        // anonymous session and sign into that player below.
                        AuthenticationService.Instance.SignOut();
                    }
                }

                await AuthenticationService.Instance
                    .SignInWithAppleGameCenterAsync(
                        signature,
                        teamPlayerId,
                        identity.PublicKeyUrl,
                        salt,
                        identity.Timestamp,
                        new SignInOptions { CreateAccount = true });
                Debug.Log(
                    "Unity Authentication signed in with Game Center; Cloud Save can use the canonical player account.",
                    this);
                return;
            }
            catch (AuthenticationException exception)
                when (exception.Notifications != null
                      && exception.Notifications.Count > 0)
            {
                CaptureServiceNotifications(exception.Notifications);
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Unity Authentication could not use the Game Center identity; falling back to an anonymous cloud session. {exception.Message}",
                    this);
            }
        }
#endif

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            try
            {
                await AuthenticationService.Instance
                    .SignInAnonymouslyAsync();
                Debug.LogWarning(
                    "Unity Authentication signed in anonymously; Cloud Save will not follow the Game Center account until Game Center sign-in succeeds.",
                    this);
            }
            catch (AuthenticationException exception)
                when (exception.Notifications != null
                      && exception.Notifications.Count > 0)
            {
                CaptureServiceNotifications(exception.Notifications);
                throw;
            }
        }
    }

    private async Task RefreshServiceNotificationsAsync()
    {
        string latestNotificationDate =
            AuthenticationService.Instance.LastNotificationDate;
        if (!long.TryParse(
                latestNotificationDate,
                out long latestAvailableDate)
            || latestAvailableDate <= ReadLastServiceNotificationDate())
        {
            return;
        }

        List<Notification> notifications =
            await AuthenticationService.Instance.GetNotificationsAsync();
        CaptureServiceNotifications(notifications);
    }

    private void CaptureServiceNotifications(
        IReadOnlyList<Notification> notifications)
    {
        if (notifications == null || notifications.Count == 0)
        {
            return;
        }

        long lastReadDate = ReadLastServiceNotificationDate();
        pendingServiceNotifications.Clear();
        foreach (Notification notification in notifications)
        {
            if (!long.TryParse(notification.CreatedAt, out long createdAt)
                || createdAt <= lastReadDate)
            {
                continue;
            }

            pendingServiceNotifications.Add(notification);
        }
    }

    private string BuildPendingServiceNotificationMessage()
    {
        if (pendingServiceNotifications.Count == 0)
        {
            return string.Empty;
        }

        List<string> messages = new();
        foreach (Notification notification in pendingServiceNotifications)
        {
            string message = string.IsNullOrWhiteSpace(notification.Message)
                ? "Your player account was affected by a Unity Gaming Services action."
                : notification.Message.Trim();
            if (!string.IsNullOrWhiteSpace(notification.CaseId))
            {
                message += $"\nCase ID: {notification.CaseId}";
            }

            messages.Add(message);
        }

        return string.Join("\n\n", messages);
    }

    private static long ReadLastServiceNotificationDate()
    {
        return long.TryParse(
            PlayerPrefs.GetString(
                DsaLastReadNotificationDateKey,
                "0"),
            out long lastReadDate)
            ? Math.Max(0L, lastReadDate)
            : 0L;
    }

#if UNITY_IOS
    private async Task ShowLeaderboardsAsync(
        Action<string> onUnavailable)
    {
        try
        {
            NSString leaderboardId = new(
                settings.LifetimeZombieKillsLeaderboardId);
            GKGameCenterViewController viewController =
                GKGameCenterViewController.InitWithLeaderboardID(
                    leaderboardId,
                    GKLeaderboard.PlayerScope.Global,
                    GKLeaderboard.TimeScope.AllTime);

            if (viewController == null)
            {
                onUnavailable?.Invoke("GAME CENTER UNAVAILABLE");
                return;
            }

            await viewController.Present();
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"Game Center leaderboard could not be shown. {exception.Message}",
                this);
            onUnavailable?.Invoke("GAME CENTER UNAVAILABLE");
        }
    }
#endif

    private void TryReportPendingLeaderboardScore()
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (pendingLifetimeZombieKills <= 0L
            || !initialized
            || !SupportsLeaderboards
            || !gameCenterAuthenticated
            || leaderboardSubmissionRunning)
        {
            return;
        }

        _ = ReportPendingLeaderboardScoreAsync();
#endif
    }

#if UNITY_IOS
    private async Task ReportPendingLeaderboardScoreAsync()
    {
        leaderboardSubmissionRunning = true;
        try
        {
            if (lifetimeZombieKillsLeaderboard == null)
            {
                NSArray<GKLeaderboard> leaderboards =
                    await GKLeaderboard.LoadLeaderboards(
                        settings.LifetimeZombieKillsLeaderboardId);
                if (leaderboards == null || leaderboards.Count == 0)
                {
                    Debug.LogWarning(
                        "Game Center did not return the configured leaderboard.",
                        this);
                    return;
                }

                lifetimeZombieKillsLeaderboard = leaderboards[0];
            }

            while (pendingLifetimeZombieKills > 0L)
            {
                long score = pendingLifetimeZombieKills;
                await lifetimeZombieKillsLeaderboard.SubmitScore(
                    score,
                    0L,
                    GKLocalPlayer.Local);

                Debug.Log(
                    $"Game Center lifetime zombie kills score submitted successfully: {score}.",
                    this);

                if (pendingLifetimeZombieKills <= score)
                {
                    pendingLifetimeZombieKills = 0L;
                }
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"Game Center score submission failed and will be retried later. {exception.Message}",
                this);
        }
        finally
        {
            leaderboardSubmissionRunning = false;
        }
    }
#endif

    private static CloudStorageEnvelope ParseCloudEnvelope(string json)
    {
        if (!string.IsNullOrWhiteSpace(json)
            && json.Contains(
                "\"modifiedAtUnixMilliseconds\"",
                StringComparison.Ordinal)
            && json.Contains("\"value\"", StringComparison.Ordinal))
        {
            try
            {
                CloudStorageEnvelope envelope =
                    JsonUtility.FromJson<CloudStorageEnvelope>(json);
                if (envelope != null)
                {
                    return envelope;
                }
            }
            catch (Exception)
            {
                // Treat data written before the envelope format as raw text.
            }
        }

        return new CloudStorageEnvelope
        {
            value = json ?? string.Empty,
            modifiedAtUnixMilliseconds = 0L
        };
    }

    private static long ReadLocalTimestamp(string key)
    {
        return long.TryParse(
            PlayerPrefs.GetString(key + TimestampSuffix, "0"),
            out long timestamp)
            ? Math.Max(0L, timestamp)
            : 0L;
    }

    private static bool IsCloudSyncedKey(string key)
    {
        return string.Equals(
            key,
            ProgressionCloudKey,
            StringComparison.Ordinal);
    }

    private static bool IsHttpsUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out Uri uri)
               && string.Equals(
                   uri.Scheme,
                   Uri.UriSchemeHttps,
                   StringComparison.OrdinalIgnoreCase);
    }

    private static void OpenConfiguredUrl(
        string url,
        string unavailableMessage,
        Action<string> onUnavailable)
    {
        if (!IsHttpsUrl(url))
        {
            onUnavailable?.Invoke(unavailableMessage);
            return;
        }

        Application.OpenURL(url);
    }

    private static void DeleteLocalSyncedKey(string key)
    {
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.DeleteKey(key + TimestampSuffix);
        PlayerPrefs.Save();
    }

    private void HandlePlatformStateChanged()
    {
        NotifyRewardedStateChanged();
    }
}
