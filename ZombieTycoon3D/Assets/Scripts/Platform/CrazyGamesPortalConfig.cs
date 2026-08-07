using UnityEngine;

public enum CrazyGamesLaunchStage
{
    Basic,
    Full
}

[CreateAssetMenu(
    fileName = "CrazyGamesPortalConfig",
    menuName = "Scrap the Dead/Platform/CrazyGames Portal Config")]
public sealed class CrazyGamesPortalConfig : ScriptableObject
{
    [Header("Launch")]
    [SerializeField] private CrazyGamesLaunchStage launchStage =
        CrazyGamesLaunchStage.Basic;

    [Header("Rewarded Ads")]
    [SerializeField, Min(0)] private int salvageDropScrap = 100;

    public CrazyGamesLaunchStage LaunchStage => launchStage;
    public bool RewardedAdsEnabled =>
        launchStage == CrazyGamesLaunchStage.Full;
    public int SalvageDropScrap => Mathf.Max(0, salvageDropScrap);
}
