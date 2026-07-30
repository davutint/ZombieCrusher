using System;

public enum MissionEndReason
{
    TimeExpired,
    VehicleDestroyed
}

public readonly struct MissionProgress
{
    public MissionProgress(
        int kills,
        int killTarget,
        int score,
        int normalKillScore,
        int bonusKillScore)
    {
        Kills = kills;
        KillTarget = killTarget;
        Score = score;
        NormalKillScore = normalKillScore;
        BonusKillScore = bonusKillScore;
    }

    public int Kills { get; }
    public int KillTarget { get; }
    public int Score { get; }
    public int NormalKillScore { get; }
    public int BonusKillScore { get; }
    public bool TargetReached => Kills >= KillTarget;
    public int BonusKills => Math.Max(0, Kills - KillTarget);
}

public readonly struct MissionReward
{
    public MissionReward(
        int killScrap,
        int completionBonus,
        int totalScrap,
        int balanceAfter)
    {
        KillScrap = killScrap;
        CompletionBonus = completionBonus;
        TotalScrap = totalScrap;
        BalanceAfter = balanceAfter;
    }

    public int KillScrap { get; }
    public int CompletionBonus { get; }
    public int TotalScrap { get; }
    public int BalanceAfter { get; }
}

public readonly struct MissionResult
{
    public MissionResult(
        MissionEndReason endReason,
        bool succeeded,
        MissionProgress progress,
        MissionReward reward,
        float remainingSeconds,
        float remainingHealth,
        float maximumHealth)
    {
        EndReason = endReason;
        Succeeded = succeeded;
        Progress = progress;
        Reward = reward;
        RemainingSeconds = remainingSeconds;
        RemainingHealth = remainingHealth;
        MaximumHealth = maximumHealth;
    }

    public MissionEndReason EndReason { get; }
    public bool Succeeded { get; }
    public MissionProgress Progress { get; }
    public MissionReward Reward { get; }
    public float RemainingSeconds { get; }
    public float RemainingHealth { get; }
    public float MaximumHealth { get; }
}
