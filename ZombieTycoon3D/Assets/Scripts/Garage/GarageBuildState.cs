using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GarageBuildState : MonoBehaviour
{
    [Serializable]
    private struct EquippedEntry
    {
        public GarageAttachmentSlot slot;
        public GarageAttachmentDefinition attachment;
    }

    [SerializeField] private GarageCatalog catalog;
    [SerializeField] private GarageVehicleDefinition selectedVehicle;
    [SerializeField] private List<EquippedEntry> equipped = new();

    private readonly HashSet<string> ownedVehicleIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> ownedAttachmentIds = new(StringComparer.Ordinal);

    private GarageVehicleDefinition previewVehicle;
    private GarageAttachmentDefinition previewAttachment;

    public event Action Changed;

    public GarageCatalog Catalog => catalog;
    public GarageVehicleDefinition SelectedVehicle => selectedVehicle;
    public GarageVehicleDefinition DisplayedVehicle => previewVehicle != null ? previewVehicle : selectedVehicle;
    public GarageAttachmentDefinition PreviewAttachment => previewAttachment;

    public VehicleStats CurrentStats =>
        CalculateStats(selectedVehicle, null, replacePreviewSlot: false);

    public VehicleStats PreviewStats
    {
        get
        {
            GarageVehicleDefinition vehicle = DisplayedVehicle;
            bool sameVehicle = vehicle == selectedVehicle;
            return CalculateStats(
                vehicle,
                sameVehicle ? previewAttachment : null,
                replacePreviewSlot: sameVehicle && previewAttachment != null);
        }
    }

    private void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (catalog == null)
        {
            Debug.LogError("GarageBuildState: GarageCatalog reference is required.", this);
            return;
        }

        if (selectedVehicle == null)
        {
            selectedVehicle = catalog.StartingVehicle;
        }

        ownedVehicleIds.Clear();
        if (selectedVehicle != null && !string.IsNullOrEmpty(selectedVehicle.VehicleId))
        {
            ownedVehicleIds.Add(selectedVehicle.VehicleId);
        }

        ownedAttachmentIds.Clear();
        for (int i = 0; i < equipped.Count; i++)
        {
            GarageAttachmentDefinition attachment = equipped[i].attachment;
            if (attachment != null && !string.IsNullOrEmpty(attachment.AttachmentId))
            {
                ownedAttachmentIds.Add(attachment.AttachmentId);
            }
        }

        previewVehicle = null;
        previewAttachment = null;
        Changed?.Invoke();
    }

    public bool IsVehicleOwned(GarageVehicleDefinition vehicle)
    {
        return vehicle != null && ownedVehicleIds.Contains(vehicle.VehicleId);
    }

    public bool IsAttachmentOwned(GarageAttachmentDefinition attachment)
    {
        return attachment != null && ownedAttachmentIds.Contains(attachment.AttachmentId);
    }

    public void GrantVehicle(GarageVehicleDefinition vehicle)
    {
        if (vehicle != null && ownedVehicleIds.Add(vehicle.VehicleId))
        {
            Changed?.Invoke();
        }
    }

    public void GrantAttachment(GarageAttachmentDefinition attachment)
    {
        if (attachment != null && ownedAttachmentIds.Add(attachment.AttachmentId))
        {
            Changed?.Invoke();
        }
    }

    public bool SelectOwnedVehicle(GarageVehicleDefinition vehicle)
    {
        if (!IsVehicleOwned(vehicle))
        {
            return false;
        }

        selectedVehicle = vehicle;
        previewVehicle = null;
        previewAttachment = null;
        RemoveIncompatibleEquippedAttachments();
        Changed?.Invoke();
        return true;
    }

    public void PreviewVehicle(GarageVehicleDefinition vehicle)
    {
        previewVehicle = vehicle;
        previewAttachment = null;
        Changed?.Invoke();
    }

    public void PreviewPart(GarageAttachmentDefinition attachment)
    {
        GarageVehicleDefinition vehicle = DisplayedVehicle;
        if (attachment != null
            && (vehicle == null || !attachment.TryGetPose(vehicle.VehicleId, out _)))
        {
            previewAttachment = null;
        }
        else
        {
            previewAttachment = attachment;
        }

        Changed?.Invoke();
    }

    public void ClearPreview()
    {
        previewVehicle = null;
        previewAttachment = null;
        Changed?.Invoke();
    }

    public bool EquipPreviewPart()
    {
        if (previewAttachment == null
            || selectedVehicle == null
            || !IsAttachmentOwned(previewAttachment)
            || !previewAttachment.TryGetPose(selectedVehicle.VehicleId, out _))
        {
            return false;
        }

        SetEquipped(previewAttachment.Slot, previewAttachment);
        previewAttachment = null;
        Changed?.Invoke();
        return true;
    }

    public GarageAttachmentDefinition GetEquipped(GarageAttachmentSlot slot)
    {
        for (int i = 0; i < equipped.Count; i++)
        {
            if (equipped[i].slot == slot)
            {
                return equipped[i].attachment;
            }
        }

        return null;
    }

    public IEnumerable<GarageAttachmentDefinition> GetEquippedAttachments()
    {
        for (int i = 0; i < equipped.Count; i++)
        {
            GarageAttachmentDefinition attachment = equipped[i].attachment;
            if (attachment != null)
            {
                yield return attachment;
            }
        }
    }

    private VehicleStats CalculateStats(
        GarageVehicleDefinition vehicle,
        GarageAttachmentDefinition extraAttachment,
        bool replacePreviewSlot)
    {
        if (vehicle == null)
        {
            return default;
        }

        VehicleStats result = vehicle.BaseStats;
        for (int i = 0; i < equipped.Count; i++)
        {
            GarageAttachmentDefinition attachment = equipped[i].attachment;
            if (attachment == null
                || !attachment.TryGetPose(vehicle.VehicleId, out _)
                || (replacePreviewSlot
                    && extraAttachment != null
                    && attachment.Slot == extraAttachment.Slot))
            {
                continue;
            }

            result = result.Apply(attachment.Modifier);
        }

        if (extraAttachment != null
            && extraAttachment.TryGetPose(vehicle.VehicleId, out _))
        {
            result = result.Apply(extraAttachment.Modifier);
        }

        return result;
    }

    private void SetEquipped(
        GarageAttachmentSlot slot,
        GarageAttachmentDefinition attachment)
    {
        for (int i = 0; i < equipped.Count; i++)
        {
            if (equipped[i].slot != slot)
            {
                continue;
            }

            EquippedEntry entry = equipped[i];
            entry.attachment = attachment;
            equipped[i] = entry;
            return;
        }

        equipped.Add(new EquippedEntry
        {
            slot = slot,
            attachment = attachment
        });
    }

    private void RemoveIncompatibleEquippedAttachments()
    {
        if (selectedVehicle == null)
        {
            equipped.Clear();
            return;
        }

        for (int i = equipped.Count - 1; i >= 0; i--)
        {
            GarageAttachmentDefinition attachment = equipped[i].attachment;
            if (attachment == null
                || !attachment.TryGetPose(selectedVehicle.VehicleId, out _))
            {
                equipped.RemoveAt(i);
            }
        }
    }
}
