using System;
using UnityEngine;

public enum ZombieArchetype
{
    Standard,
    Runner,
    Brute,
    Hazard,
    Bounty
}

[Serializable]
public struct ZombieGameplayProfile
{
    [SerializeField] private ZombieArchetype archetype;
    [SerializeField, Min(0.1f)] private float movementSpeedMultiplier;
    [SerializeField, Min(0.1f)] private float requiredImpactSpeedMultiplier;
    [SerializeField, Min(0f)] private float contactThreatMultiplier;
    [SerializeField, Min(0f)] private float killScoreMultiplier;
    [SerializeField] private string killFeedbackLabel;

    public ZombieArchetype Archetype => archetype;
    public float MovementSpeedMultiplier =>
        movementSpeedMultiplier > 0f
            ? movementSpeedMultiplier
            : 1f;
    public float RequiredImpactSpeedMultiplier =>
        requiredImpactSpeedMultiplier > 0f
            ? requiredImpactSpeedMultiplier
            : 1f;
    public float ContactThreatMultiplier =>
        contactThreatMultiplier > 0f
            ? contactThreatMultiplier
            : 1f;
    public float KillScoreMultiplier =>
        killScoreMultiplier > 0f
            ? killScoreMultiplier
            : 1f;
    public string KillFeedbackLabel => killFeedbackLabel;
}

public readonly struct ZombieKillEvent
{
    public ZombieKillEvent(
        Vector3 position,
        ZombieArchetype archetype,
        float scoreMultiplier,
        string feedbackLabel)
    {
        Position = position;
        Archetype = archetype;
        ScoreMultiplier = Mathf.Max(0f, scoreMultiplier);
        FeedbackLabel = feedbackLabel;
    }

    public Vector3 Position { get; }
    public ZombieArchetype Archetype { get; }
    public float ScoreMultiplier { get; }
    public string FeedbackLabel { get; }
}

public readonly struct ZombieScoreAward
{
    public ZombieScoreAward(
        ZombieArchetype archetype,
        int earnedScore,
        int bonusScore,
        string feedbackLabel)
    {
        Archetype = archetype;
        EarnedScore = earnedScore;
        BonusScore = bonusScore;
        FeedbackLabel = feedbackLabel;
    }

    public ZombieArchetype Archetype { get; }
    public int EarnedScore { get; }
    public int BonusScore { get; }
    public string FeedbackLabel { get; }
}
