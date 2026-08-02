using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ScoreManager : MonoBehaviour
{
    private const float KillRateWindowSeconds = 4f;
    private const float ChainTimeoutSeconds = 1.15f;
    private const float MeterDrainPerSecond = 0.28f;
    private const float MayhemPublishInterval = 0.08f;
    private const int TrackedKillCapacity = 128;

    private readonly float[] recentKillTimes =
        new float[TrackedKillCapacity];

    private bool missionActive;
    private bool missionPaused;
    private float pauseStartedAt;
    private int kills;
    private int killTarget = 1;
    private int score;
    private int normalKillScore;
    private int bonusKillScore;
    private int recentKillHead;
    private int recentKillCount;
    private int currentChain;
    private int bestChain;
    private float lastKillTime = float.NegativeInfinity;
    private float mayhemMeter;
    private float nextMayhemPublishTime;
    private bool mayhemDirty;
    private MayhemTier currentTier;
    private MayhemTier highestTier;

    public event Action<MissionProgress> ProgressChanged;
    public event Action<MayhemProgress> MayhemChanged;
    public event Action<ZombieScoreAward> SpecialKillScored;

    public bool MissionActive => missionActive;

    public MissionProgress CurrentProgress => new MissionProgress(
        kills,
        killTarget,
        score,
        normalKillScore,
        bonusKillScore,
        CurrentMayhem);

    public MayhemProgress CurrentMayhem => new MayhemProgress(
        currentTier,
        highestTier,
        mayhemMeter,
        MayhemRules.GetScoreMultiplier(currentTier),
        recentKillCount,
        currentChain,
        bestChain);

    private void OnEnable()
    {
        EventManager.OnZombieKilled += HandleZombieKilled;
    }

    private void OnDisable()
    {
        EventManager.OnZombieKilled -= HandleZombieKilled;
        missionActive = false;
        missionPaused = false;
    }

    private void Update()
    {
        if (!missionActive || missionPaused)
        {
            return;
        }

        float currentTime = Time.unscaledTime;
        bool stateChanged = PruneExpiredKills(currentTime);

        if (currentChain > 0
            && currentTime - lastKillTime > ChainTimeoutSeconds)
        {
            currentChain = 0;
            stateChanged = true;
        }

        float targetMeter = Mathf.Clamp01(
            recentKillCount / (float)MayhemRules.MayhemKillCount);
        float previousMeter = mayhemMeter;
        mayhemMeter = Mathf.MoveTowards(
            mayhemMeter,
            targetMeter,
            MeterDrainPerSecond * Time.unscaledDeltaTime);

        MayhemTier nextTier = MayhemRules.GetTier(recentKillCount);
        bool tierChanged = nextTier != currentTier;
        currentTier = nextTier;
        if ((int)currentTier > (int)highestTier)
        {
            highestTier = currentTier;
        }

        mayhemDirty |= stateChanged
            || tierChanged
            || !Mathf.Approximately(previousMeter, mayhemMeter);
        PublishMayhem(currentTime, tierChanged);
    }

    public void BeginMission(
        int target,
        int pointsPerKill,
        int pointsPerBonusKill)
    {
        killTarget = Mathf.Max(1, target);
        normalKillScore = Mathf.Max(0, pointsPerKill);
        bonusKillScore = Mathf.Max(0, pointsPerBonusKill);
        kills = 0;
        score = 0;
        ResetMayhemState();
        missionActive = true;
        missionPaused = false;
        ProgressChanged?.Invoke(CurrentProgress);
        MayhemChanged?.Invoke(CurrentMayhem);
    }

    public MissionProgress FinishMission()
    {
        missionActive = false;
        missionPaused = false;
        return CurrentProgress;
    }

    public void CancelMission()
    {
        missionActive = false;
        missionPaused = false;
        ResetMayhemState();
        MayhemChanged?.Invoke(CurrentMayhem);
    }

    public void RegisterZombieKill()
    {
        RegisterZombieKill(new ZombieKillEvent(
            Vector3.zero,
            ZombieArchetype.Standard,
            1f,
            null));
    }

    public void SetMissionPaused(bool paused)
    {
        if (!missionActive || missionPaused == paused)
        {
            return;
        }

        missionPaused = paused;
        if (paused)
        {
            pauseStartedAt = Time.unscaledTime;
            return;
        }

        float pauseDuration = Mathf.Max(
            0f,
            Time.unscaledTime - pauseStartedAt);
        for (int i = 0; i < recentKillCount; i++)
        {
            int index = (recentKillHead + i) % TrackedKillCapacity;
            recentKillTimes[index] += pauseDuration;
        }

        if (!float.IsNegativeInfinity(lastKillTime))
        {
            lastKillTime += pauseDuration;
        }

        nextMayhemPublishTime += pauseDuration;
    }

    private void RegisterZombieKill(ZombieKillEvent killEvent)
    {
        if (!missionActive || missionPaused)
        {
            return;
        }

        float currentTime = Time.unscaledTime;
        PruneExpiredKills(currentTime);
        RecordKill(currentTime);
        UpdateChain(currentTime);

        MayhemTier previousTier = currentTier;
        currentTier = MayhemRules.GetTier(recentKillCount);
        if ((int)currentTier > (int)highestTier)
        {
            highestTier = currentTier;
        }

        mayhemMeter = Mathf.Max(
            mayhemMeter,
            Mathf.Clamp01(
                recentKillCount / (float)MayhemRules.MayhemKillCount));

        int baseScore = kills >= killTarget
            ? bonusKillScore
            : normalKillScore;
        float mayhemMultiplier =
            MayhemRules.GetScoreMultiplier(currentTier);
        int standardScore = Mathf.RoundToInt(
            baseScore * mayhemMultiplier);
        int earnedScore = Mathf.RoundToInt(
            baseScore
            * Mathf.Max(0f, killEvent.ScoreMultiplier)
            * mayhemMultiplier);
        kills++;
        score += earnedScore;
        mayhemDirty = true;
        ProgressChanged?.Invoke(CurrentProgress);
        PublishMayhem(currentTime, currentTier != previousTier);

        int bonusScore = Mathf.Max(0, earnedScore - standardScore);
        if (bonusScore > 0
            && !string.IsNullOrWhiteSpace(killEvent.FeedbackLabel))
        {
            SpecialKillScored?.Invoke(new ZombieScoreAward(
                killEvent.Archetype,
                earnedScore,
                bonusScore,
                killEvent.FeedbackLabel));
        }
    }

    private void HandleZombieKilled(ZombieKillEvent killEvent)
    {
        RegisterZombieKill(killEvent);
    }

    private void ResetMayhemState()
    {
        recentKillHead = 0;
        recentKillCount = 0;
        currentChain = 0;
        bestChain = 0;
        lastKillTime = float.NegativeInfinity;
        mayhemMeter = 0f;
        nextMayhemPublishTime = 0f;
        mayhemDirty = false;
        currentTier = MayhemTier.None;
        highestTier = MayhemTier.None;
    }

    private bool PruneExpiredKills(float currentTime)
    {
        int previousCount = recentKillCount;
        while (recentKillCount > 0
               && currentTime - recentKillTimes[recentKillHead]
               > KillRateWindowSeconds)
        {
            recentKillHead =
                (recentKillHead + 1) % TrackedKillCapacity;
            recentKillCount--;
        }

        return recentKillCount != previousCount;
    }

    private void RecordKill(float currentTime)
    {
        if (recentKillCount == TrackedKillCapacity)
        {
            recentKillHead =
                (recentKillHead + 1) % TrackedKillCapacity;
            recentKillCount--;
        }

        int tail = (recentKillHead + recentKillCount)
                   % TrackedKillCapacity;
        recentKillTimes[tail] = currentTime;
        recentKillCount++;
    }

    private void UpdateChain(float currentTime)
    {
        currentChain = currentTime - lastKillTime <= ChainTimeoutSeconds
            ? currentChain + 1
            : 1;
        bestChain = Mathf.Max(bestChain, currentChain);
        lastKillTime = currentTime;
    }

    private void PublishMayhem(float currentTime, bool force)
    {
        if (!mayhemDirty
            || (!force && currentTime < nextMayhemPublishTime))
        {
            return;
        }

        nextMayhemPublishTime = currentTime + MayhemPublishInterval;
        mayhemDirty = false;
        MayhemChanged?.Invoke(CurrentMayhem);
    }
}
