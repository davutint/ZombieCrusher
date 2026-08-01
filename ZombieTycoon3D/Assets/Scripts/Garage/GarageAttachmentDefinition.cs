using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum GarageAttachmentFeedbackTone
{
    Impact,
    Defense,
    Repair
}

[Serializable]
public struct GarageAttachmentEffect
{
    [Header("Build-wide Passive Effect")]
    [SerializeField] private float damageTakenMultiplier;
    [SerializeField] private float lateralGripMultiplier;
    [SerializeField, Min(0f)] private float downforceBonus;

    [Header("Attachment Contact")]
    [SerializeField] private float contactImpactPowerMultiplier;

    [Header("Kill-based Repair")]
    [SerializeField, Min(0)] private int repairEveryKills;
    [SerializeField, Min(0f)] private float repairAmount;
    [SerializeField, Min(0)] private int maximumRepairs;

    [Header("Runtime Feedback")]
    [SerializeField] private string feedbackLabel;
    [SerializeField] private GarageAttachmentFeedbackTone feedbackTone;

    public float DamageTakenMultiplier =>
        damageTakenMultiplier > 0f ? damageTakenMultiplier : 1f;
    public float LateralGripMultiplier =>
        lateralGripMultiplier > 0f ? lateralGripMultiplier : 1f;
    public float DownforceBonus => Mathf.Max(0f, downforceBonus);
    public float ContactImpactPowerMultiplier =>
        contactImpactPowerMultiplier > 0f
            ? contactImpactPowerMultiplier
            : 1f;
    public int RepairEveryKills => Mathf.Max(0, repairEveryKills);
    public float RepairAmount => Mathf.Max(0f, repairAmount);
    public int MaximumRepairs => Mathf.Max(0, maximumRepairs);
    public string FeedbackLabel => feedbackLabel;
    public GarageAttachmentFeedbackTone FeedbackTone => feedbackTone;

    public bool HasRepair =>
        RepairEveryKills > 0
        && RepairAmount > 0f
        && MaximumRepairs > 0;
}

[Serializable]
public struct GarageBuildEffects
{
    private float damageTakenMultiplier;
    private float lateralGripMultiplier;
    private float downforceBonus;
    private int repairEveryKills;
    private float repairAmount;
    private int maximumRepairs;
    private string damageFeedbackLabel;
    private GarageAttachmentFeedbackTone damageFeedbackTone;
    private string repairFeedbackLabel;
    private GarageAttachmentFeedbackTone repairFeedbackTone;

    public static GarageBuildEffects Neutral => new GarageBuildEffects
    {
        damageTakenMultiplier = 1f,
        lateralGripMultiplier = 1f
    };

    public float DamageTakenMultiplier =>
        damageTakenMultiplier > 0f ? damageTakenMultiplier : 1f;
    public float LateralGripMultiplier =>
        lateralGripMultiplier > 0f ? lateralGripMultiplier : 1f;
    public float DownforceBonus => Mathf.Max(0f, downforceBonus);
    public int RepairEveryKills => Mathf.Max(0, repairEveryKills);
    public float RepairAmount => Mathf.Max(0f, repairAmount);
    public int MaximumRepairs => Mathf.Max(0, maximumRepairs);
    public string DamageFeedbackLabel => damageFeedbackLabel;
    public GarageAttachmentFeedbackTone DamageFeedbackTone =>
        damageFeedbackTone;
    public string RepairFeedbackLabel => repairFeedbackLabel;
    public GarageAttachmentFeedbackTone RepairFeedbackTone =>
        repairFeedbackTone;
    public bool HasRepair =>
        RepairEveryKills > 0
        && RepairAmount > 0f
        && MaximumRepairs > 0;

    public GarageBuildEffects Apply(GarageAttachmentEffect effect)
    {
        GarageBuildEffects result = this;
        result.damageTakenMultiplier =
            DamageTakenMultiplier * effect.DamageTakenMultiplier;
        result.lateralGripMultiplier =
            LateralGripMultiplier * effect.LateralGripMultiplier;
        result.downforceBonus = DownforceBonus + effect.DownforceBonus;

        if (effect.DamageTakenMultiplier < 0.999f
            && !string.IsNullOrWhiteSpace(effect.FeedbackLabel))
        {
            result.damageFeedbackLabel = effect.FeedbackLabel;
            result.damageFeedbackTone = effect.FeedbackTone;
        }

        if (effect.HasRepair)
        {
            result.repairEveryKills = effect.RepairEveryKills;
            result.repairAmount = effect.RepairAmount;
            result.maximumRepairs = effect.MaximumRepairs;
            result.repairFeedbackLabel = effect.FeedbackLabel;
            result.repairFeedbackTone = effect.FeedbackTone;
        }

        return result;
    }
}

[Serializable]
public enum GarageAttachmentAnchor
{
    Body,
    FrontLeftWheel,
    FrontRightWheel,
    RearLeftWheel,
    RearRightWheel
}

[Serializable]
public struct GarageAttachmentPose
{
    [SerializeField] private string vehicleId;
    [SerializeField] private GarageAttachmentAnchor anchor;
    [SerializeField] private Vector3 localPosition;
    [SerializeField] private Vector3 localEuler;
    [SerializeField] private Vector3 localScale;

    [Header("Gameplay Contact")]
    [SerializeField] private bool createsImpactZone;
    [SerializeField] private Vector3 impactZoneCenter;
    [SerializeField] private Vector3 impactZoneEuler;
    [SerializeField] private Vector3 impactZoneSize;

    public string VehicleId => vehicleId;
    public GarageAttachmentAnchor Anchor => anchor;
    public Vector3 LocalPosition => localPosition;
    public Quaternion LocalRotation => Quaternion.Euler(localEuler);
    public Vector3 LocalScale =>
        localScale.sqrMagnitude < 0.0001f ? Vector3.one : localScale;
    public bool CreatesImpactZone =>
        createsImpactZone && impactZoneSize.sqrMagnitude > 0.0001f;
    public Vector3 ImpactZoneCenter => impactZoneCenter;
    public Quaternion ImpactZoneRotation => Quaternion.Euler(impactZoneEuler);
    public Vector3 ImpactZoneSize => new Vector3(
        Mathf.Max(0.01f, Mathf.Abs(impactZoneSize.x)),
        Mathf.Max(0.01f, Mathf.Abs(impactZoneSize.y)),
        Mathf.Max(0.01f, Mathf.Abs(impactZoneSize.z)));
}

[CreateAssetMenu(fileName = "GarageAttachment", menuName = "Zombie Tycoon/Garage/Attachment")]
public sealed class GarageAttachmentDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string attachmentId;
    [SerializeField] private string displayName;
    [TextArea(2, 4)]
    [SerializeField] private string description;
    [SerializeField] private GarageAttachmentSlot slot;

    [Header("Visual")]
    [SerializeField] private GameObject visualPrefab;
    [SerializeField] private List<GarageAttachmentPose> compatibleVehicles = new();

    [Header("Stat Trade-off")]
    [SerializeField, Min(0)] private int price;
    [SerializeField] private VehicleStatModifier modifier;

    [Header("Gameplay Effect")]
    [SerializeField] private GarageAttachmentEffect gameplayEffect;
    [TextArea(2, 3)]
    [SerializeField] private string gameplayEffectSummary;
    [TextArea(2, 3)]
    [SerializeField] private string tradeoffSummary;

    public string AttachmentId => attachmentId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public GarageAttachmentSlot Slot => slot;
    public GameObject VisualPrefab => visualPrefab;
    public int Price => Mathf.Max(0, price);
    public VehicleStatModifier Modifier => modifier;
    public GarageAttachmentEffect GameplayEffect => gameplayEffect;
    public string GameplayEffectSummary => gameplayEffectSummary;
    public string TradeoffSummary => tradeoffSummary;

    public bool TryGetPose(string vehicleId, out GarageAttachmentPose pose)
    {
        return TryGetPose(vehicleId, 0, out pose);
    }

    public int GetPoseCount(string vehicleId)
    {
        int count = 0;
        for (int i = 0; i < compatibleVehicles.Count; i++)
        {
            if (string.Equals(
                    compatibleVehicles[i].VehicleId,
                    vehicleId,
                    StringComparison.Ordinal))
            {
                count++;
            }
        }

        return count;
    }

    public bool TryGetPose(
        string vehicleId,
        int poseIndex,
        out GarageAttachmentPose pose)
    {
        int matchingIndex = 0;
        for (int i = 0; i < compatibleVehicles.Count; i++)
        {
            GarageAttachmentPose candidate = compatibleVehicles[i];
            if (string.Equals(candidate.VehicleId, vehicleId, StringComparison.Ordinal))
            {
                if (matchingIndex == poseIndex)
                {
                    pose = candidate;
                    return true;
                }

                matchingIndex++;
            }
        }

        pose = default;
        return false;
    }

    private void OnValidate()
    {
        attachmentId = attachmentId?.Trim();
        displayName = displayName?.Trim();
        price = Mathf.Max(0, price);
    }
}
