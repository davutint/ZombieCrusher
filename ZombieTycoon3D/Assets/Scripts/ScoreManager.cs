using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ScoreManager : MonoBehaviour
{
    private bool missionActive;
    private int kills;
    private int killTarget = 1;
    private int score;
    private int normalKillScore;
    private int bonusKillScore;

    public event Action<MissionProgress> ProgressChanged;

    public bool MissionActive => missionActive;

    public MissionProgress CurrentProgress => new MissionProgress(
        kills,
        killTarget,
        score,
        normalKillScore,
        bonusKillScore);

    private void OnEnable()
    {
        EventManager.OnZombieDead += HandleZombieDead;
    }

    private void OnDisable()
    {
        EventManager.OnZombieDead -= HandleZombieDead;
        missionActive = false;
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
        missionActive = true;
        ProgressChanged?.Invoke(CurrentProgress);
    }

    public MissionProgress FinishMission()
    {
        missionActive = false;
        return CurrentProgress;
    }

    public void CancelMission()
    {
        missionActive = false;
    }

    public void RegisterZombieKill()
    {
        if (!missionActive)
        {
            return;
        }

        int earnedScore = kills >= killTarget
            ? bonusKillScore
            : normalKillScore;
        kills++;
        score += earnedScore;
        ProgressChanged?.Invoke(CurrentProgress);
    }

    private void HandleZombieDead(Vector3 _)
    {
        RegisterZombieKill();
    }
}
