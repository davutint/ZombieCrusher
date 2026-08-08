using UnityEngine;

[CreateAssetMenu(
    fileName = "IosPlatformSettings",
    menuName = "Zombie Tycoon/iOS Platform Settings")]
public sealed class IosPlatformSettings : ScriptableObject
{
    public const string ResourcesPath = "IosPlatformSettings";

    [Header("Game Center")]
    [SerializeField]
    private string lifetimeZombieKillsLeaderboardId = string.Empty;

    [Header("AdMob")]
    [SerializeField] private string admobAppId = string.Empty;
    [SerializeField] private string rewardedAdUnitId = string.Empty;

    [Header("In-App Purchase")]
    [SerializeField]
    private string adFreeRewardsProductId = string.Empty;

    [Header("Store Links")]
    [SerializeField] private string privacyPolicyUrl = string.Empty;
    [SerializeField] private string supportUrl = string.Empty;

    public string LifetimeZombieKillsLeaderboardId =>
        lifetimeZombieKillsLeaderboardId?.Trim() ?? string.Empty;

    public string AdMobAppId => admobAppId?.Trim() ?? string.Empty;

    public string RewardedAdUnitId =>
        rewardedAdUnitId?.Trim() ?? string.Empty;

    public string AdFreeRewardsProductId =>
        adFreeRewardsProductId?.Trim() ?? string.Empty;

    public string PrivacyPolicyUrl =>
        privacyPolicyUrl?.Trim() ?? string.Empty;

    public string SupportUrl => supportUrl?.Trim() ?? string.Empty;

    public static IosPlatformSettings Load()
    {
        return Resources.Load<IosPlatformSettings>(ResourcesPath);
    }
}
