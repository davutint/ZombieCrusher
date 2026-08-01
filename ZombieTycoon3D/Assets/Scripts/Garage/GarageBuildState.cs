using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class GarageVehicleLoadoutData
{
    public string vehicleId;
    public List<string> attachmentIds = new();
}

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
    private readonly Dictionary<string, List<EquippedEntry>> vehicleLoadouts =
        new(StringComparer.Ordinal);

    private GarageVehicleDefinition previewVehicle;
    private GarageAttachmentDefinition previewAttachment;

    public event Action Changed;

    public GarageCatalog Catalog => catalog;
    public GarageVehicleDefinition SelectedVehicle => selectedVehicle;
    public GarageVehicleDefinition DisplayedVehicle => previewVehicle != null ? previewVehicle : selectedVehicle;
    public GarageAttachmentDefinition PreviewAttachment => previewAttachment;

    public VehicleStats CurrentStats =>
        CalculateStats(
            selectedVehicle,
            null,
            replacePreviewSlot: false,
            includeLoadout: true);

    public VehicleStats DisplayedCurrentStats
    {
        get
        {
            GarageVehicleDefinition vehicle = DisplayedVehicle;
            return CalculateStats(
                vehicle,
                null,
                replacePreviewSlot: false,
                includeLoadout: vehicle == selectedVehicle);
        }
    }

    public VehicleStats PreviewStats
    {
        get
        {
            GarageVehicleDefinition vehicle = DisplayedVehicle;
            bool sameVehicle = vehicle == selectedVehicle;
            return CalculateStats(
                vehicle,
                previewAttachment,
                replacePreviewSlot: sameVehicle && previewAttachment != null,
                includeLoadout: sameVehicle);
        }
    }

    public GarageBuildEffects CurrentEffects =>
        CalculateEffects(
            selectedVehicle,
            null,
            replacePreviewSlot: false,
            includeLoadout: true);

    public GarageBuildEffects PreviewEffects
    {
        get
        {
            GarageVehicleDefinition vehicle = DisplayedVehicle;
            bool sameVehicle = vehicle == selectedVehicle;
            return CalculateEffects(
                vehicle,
                previewAttachment,
                replacePreviewSlot: sameVehicle && previewAttachment != null,
                includeLoadout: sameVehicle);
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

        vehicleLoadouts.Clear();
        RemoveIncompatibleEntries(equipped, selectedVehicle);
        SaveCurrentLoadout();

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

    public IEnumerable<string> GetOwnedVehicleIds()
    {
        foreach (string vehicleId in ownedVehicleIds)
        {
            yield return vehicleId;
        }
    }

    public IEnumerable<string> GetOwnedAttachmentIds()
    {
        foreach (string attachmentId in ownedAttachmentIds)
        {
            yield return attachmentId;
        }
    }

    public void RestoreProgression(
        IReadOnlyList<string> vehicleIds,
        IReadOnlyList<string> attachmentIds,
        string selectedVehicleId,
        IReadOnlyList<GarageVehicleLoadoutData> savedLoadouts)
    {
        if (catalog == null)
        {
            return;
        }

        ownedVehicleIds.Clear();
        GarageVehicleDefinition startingVehicle = catalog.StartingVehicle;
        if (startingVehicle != null)
        {
            ownedVehicleIds.Add(startingVehicle.VehicleId);
        }

        if (vehicleIds != null)
        {
            for (int i = 0; i < vehicleIds.Count; i++)
            {
                GarageVehicleDefinition vehicle =
                    catalog.FindVehicle(vehicleIds[i]);
                if (vehicle != null)
                {
                    ownedVehicleIds.Add(vehicle.VehicleId);
                }
            }
        }

        ownedAttachmentIds.Clear();
        if (attachmentIds != null)
        {
            for (int i = 0; i < attachmentIds.Count; i++)
            {
                GarageAttachmentDefinition attachment =
                    catalog.FindAttachment(attachmentIds[i]);
                if (attachment != null)
                {
                    ownedAttachmentIds.Add(attachment.AttachmentId);
                }
            }
        }

        GarageVehicleDefinition savedVehicle =
            catalog.FindVehicle(selectedVehicleId);
        selectedVehicle =
            savedVehicle != null && IsVehicleOwned(savedVehicle)
                ? savedVehicle
                : startingVehicle;

        vehicleLoadouts.Clear();
        if (savedLoadouts != null)
        {
            for (int i = 0; i < savedLoadouts.Count; i++)
            {
                GarageVehicleLoadoutData savedLoadout = savedLoadouts[i];
                GarageVehicleDefinition vehicle =
                    savedLoadout != null
                        ? catalog.FindVehicle(savedLoadout.vehicleId)
                        : null;
                if (vehicle == null || savedLoadout.attachmentIds == null)
                {
                    continue;
                }

                if (!vehicleLoadouts.TryGetValue(
                        vehicle.VehicleId,
                        out List<EquippedEntry> loadout))
                {
                    loadout = new List<EquippedEntry>();
                    vehicleLoadouts[vehicle.VehicleId] = loadout;
                }

                for (int attachmentIndex = 0;
                     attachmentIndex < savedLoadout.attachmentIds.Count;
                     attachmentIndex++)
                {
                    GarageAttachmentDefinition attachment =
                        catalog.FindAttachment(
                            savedLoadout.attachmentIds[attachmentIndex]);
                    if (attachment != null
                        && IsAttachmentOwned(attachment)
                        && attachment.TryGetPose(vehicle.VehicleId, out _))
                    {
                        SetEquipped(loadout, attachment.Slot, attachment);
                    }
                }
            }
        }

        LoadSelectedLoadout();

        previewVehicle = null;
        previewAttachment = null;
        Changed?.Invoke();
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

        SaveCurrentLoadout();
        selectedVehicle = vehicle;
        previewVehicle = null;
        previewAttachment = null;
        LoadSelectedLoadout();
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

    public bool Unequip(GarageAttachmentSlot slot)
    {
        for (int i = 0; i < equipped.Count; i++)
        {
            if (equipped[i].slot != slot || equipped[i].attachment == null)
            {
                continue;
            }

            equipped.RemoveAt(i);
            SaveCurrentLoadout();
            previewAttachment = null;
            Changed?.Invoke();
            return true;
        }

        return false;
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

    public List<GarageVehicleLoadoutData> CreateLoadoutSaveData()
    {
        SaveCurrentLoadout();
        List<GarageVehicleLoadoutData> result = new();
        IReadOnlyList<GarageVehicleDefinition> vehicles = catalog?.Vehicles;
        if (vehicles == null)
        {
            return result;
        }

        for (int i = 0; i < vehicles.Count; i++)
        {
            GarageVehicleDefinition vehicle = vehicles[i];
            if (vehicle == null
                || !vehicleLoadouts.TryGetValue(
                    vehicle.VehicleId,
                    out List<EquippedEntry> loadout))
            {
                continue;
            }

            GarageVehicleLoadoutData data = new GarageVehicleLoadoutData
            {
                vehicleId = vehicle.VehicleId
            };
            for (int entryIndex = 0; entryIndex < loadout.Count; entryIndex++)
            {
                GarageAttachmentDefinition attachment =
                    loadout[entryIndex].attachment;
                if (attachment != null)
                {
                    data.attachmentIds.Add(attachment.AttachmentId);
                }
            }

            result.Add(data);
        }

        return result;
    }

    private VehicleStats CalculateStats(
        GarageVehicleDefinition vehicle,
        GarageAttachmentDefinition extraAttachment,
        bool replacePreviewSlot,
        bool includeLoadout)
    {
        if (vehicle == null)
        {
            return default;
        }

        VehicleStats result = vehicle.BaseStats;
        IReadOnlyList<EquippedEntry> activeLoadout =
            includeLoadout ? equipped : Array.Empty<EquippedEntry>();
        for (int i = 0; i < activeLoadout.Count; i++)
        {
            GarageAttachmentDefinition attachment = activeLoadout[i].attachment;
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

    private GarageBuildEffects CalculateEffects(
        GarageVehicleDefinition vehicle,
        GarageAttachmentDefinition extraAttachment,
        bool replacePreviewSlot,
        bool includeLoadout)
    {
        GarageBuildEffects result = GarageBuildEffects.Neutral;
        if (vehicle == null)
        {
            return result;
        }

        IReadOnlyList<EquippedEntry> activeLoadout =
            includeLoadout ? equipped : Array.Empty<EquippedEntry>();
        for (int i = 0; i < activeLoadout.Count; i++)
        {
            GarageAttachmentDefinition attachment = activeLoadout[i].attachment;
            if (attachment == null
                || !attachment.TryGetPose(vehicle.VehicleId, out _)
                || (replacePreviewSlot
                    && extraAttachment != null
                    && attachment.Slot == extraAttachment.Slot))
            {
                continue;
            }

            result = result.Apply(attachment.GameplayEffect);
        }

        if (extraAttachment != null
            && extraAttachment.TryGetPose(vehicle.VehicleId, out _))
        {
            result = result.Apply(extraAttachment.GameplayEffect);
        }

        return result;
    }

    private void SetEquipped(
        GarageAttachmentSlot slot,
        GarageAttachmentDefinition attachment)
    {
        SetEquipped(equipped, slot, attachment);
        SaveCurrentLoadout();
    }

    private static void SetEquipped(
        List<EquippedEntry> loadout,
        GarageAttachmentSlot slot,
        GarageAttachmentDefinition attachment)
    {
        for (int i = 0; i < loadout.Count; i++)
        {
            if (loadout[i].slot != slot)
            {
                continue;
            }

            EquippedEntry entry = loadout[i];
            entry.attachment = attachment;
            loadout[i] = entry;
            return;
        }

        loadout.Add(new EquippedEntry
        {
            slot = slot,
            attachment = attachment
        });
    }

    private void SaveCurrentLoadout()
    {
        if (selectedVehicle == null)
        {
            return;
        }

        vehicleLoadouts[selectedVehicle.VehicleId] =
            new List<EquippedEntry>(equipped);
    }

    private void LoadSelectedLoadout()
    {
        equipped.Clear();
        if (selectedVehicle != null
            && vehicleLoadouts.TryGetValue(
                selectedVehicle.VehicleId,
                out List<EquippedEntry> loadout))
        {
            equipped.AddRange(loadout);
        }
    }

    private static void RemoveIncompatibleEntries(
        List<EquippedEntry> loadout,
        GarageVehicleDefinition vehicle)
    {
        if (vehicle == null)
        {
            loadout.Clear();
            return;
        }

        for (int i = loadout.Count - 1; i >= 0; i--)
        {
            GarageAttachmentDefinition attachment = loadout[i].attachment;
            if (attachment == null
                || !attachment.TryGetPose(vehicle.VehicleId, out _))
            {
                loadout.RemoveAt(i);
            }
        }
    }
}
