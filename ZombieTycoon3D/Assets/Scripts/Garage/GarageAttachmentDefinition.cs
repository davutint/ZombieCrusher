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

    [Header("Gameplay Contact")]
    [SerializeField] private bool createsImpactZone;
    [SerializeField] private Vector3 impactZoneCenter;
    [SerializeField] private Vector3 impactZoneEuler;
    [SerializeField] private Vector3 impactZoneSize;

    public string VehicleId => vehicleId;
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

    public string AttachmentId => attachmentId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public GarageAttachmentSlot Slot => slot;
    public GameObject VisualPrefab => visualPrefab;
    public int Price => Mathf.Max(0, price);
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
        price = Mathf.Max(0, price);
    }
}
