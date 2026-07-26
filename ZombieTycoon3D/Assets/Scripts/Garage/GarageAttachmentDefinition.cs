using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct GarageAttachmentPose
{
    [SerializeField] private string vehicleId;
    [SerializeField] private Vector3 localPosition;
    [SerializeField] private Vector3 localEuler;
    [SerializeField] private Vector3 localScale;

    public string VehicleId => vehicleId;
    public Vector3 LocalPosition => localPosition;
    public Quaternion LocalRotation => Quaternion.Euler(localEuler);
    public Vector3 LocalScale =>
        localScale.sqrMagnitude < 0.0001f ? Vector3.one : localScale;
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
    [SerializeField] private VehicleStatModifier modifier;

    public string AttachmentId => attachmentId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public GarageAttachmentSlot Slot => slot;
    public GameObject VisualPrefab => visualPrefab;
    public VehicleStatModifier Modifier => modifier;

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
    }
}
