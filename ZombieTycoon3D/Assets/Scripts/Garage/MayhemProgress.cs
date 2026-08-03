public enum MayhemTier
{
    None,
    Rampage,
    Carnage,
    Slaughter,
    Mayhem
}

public readonly struct MayhemProgress
{
    public MayhemProgress(
        MayhemTier tier,
        MayhemTier highestTier,
        float meter01,
        float scoreMultiplier,
        int recentKills,
        int currentChain,
        int bestChain)
    {
        Tier = tier;
        HighestTier = highestTier;
        Meter01 = meter01;
        ScoreMultiplier = scoreMultiplier;
        RecentKills = recentKills;
        CurrentChain = currentChain;
        BestChain = bestChain;
    }

    public MayhemTier Tier { get; }
    public MayhemTier HighestTier { get; }
    public float Meter01 { get; }
    public float ScoreMultiplier { get; }
    public int RecentKills { get; }
    public int CurrentChain { get; }
    public int BestChain { get; }
}

public static class MayhemRules
{
    public const int RampageKillCount = 6;
    public const int CarnageKillCount = 12;
    public const int SlaughterKillCount = 20;
    public const int MayhemKillCount = 28;

    public static MayhemTier GetTier(int recentKills)
    {
        if (recentKills >= MayhemKillCount)
        {
            return MayhemTier.Mayhem;
        }

        if (recentKills >= SlaughterKillCount)
        {
            return MayhemTier.Slaughter;
        }

        if (recentKills >= CarnageKillCount)
        {
            return MayhemTier.Carnage;
        }

        return recentKills >= RampageKillCount
            ? MayhemTier.Rampage
            : MayhemTier.None;
    }

    public static float GetScoreMultiplier(MayhemTier tier)
    {
        return tier switch
        {
            MayhemTier.Rampage => 1.15f,
            MayhemTier.Carnage => 1.35f,
            MayhemTier.Slaughter => 1.65f,
            MayhemTier.Mayhem => 2f,
            _ => 1f
        };
    }

    public static string GetLabel(MayhemTier tier)
    {
        return tier switch
        {
            MayhemTier.Rampage => "RAMPAGE",
            MayhemTier.Carnage => "CARNAGE",
            MayhemTier.Slaughter => "SLAUGHTER",
            MayhemTier.Mayhem => "MAYHEM",
            _ => "BUILD THE HEAT"
        };
    }
}
