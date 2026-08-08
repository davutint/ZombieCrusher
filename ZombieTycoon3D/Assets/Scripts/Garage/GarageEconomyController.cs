using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(GarageBuildState))]
public sealed class GarageEconomyController : MonoBehaviour
{
    private const int CurrentSaveVersion = 3;
    private const string SaveKey = "zt3d_garage_progression_v3";
    private const string PreviousSaveKey = "zt3d.garage-progression.v2";
    private const string LegacySaveKey = "zt3d.garage-progression.v1";

    [Serializable]
    private sealed class SaveData
    {
        public int version = CurrentSaveVersion;
        public int scrap;
        public long lifetimeZombieKills;
        public string selectedVehicleId;
        public List<string> ownedVehicleIds = new();
        public List<string> ownedAttachmentIds = new();
        public List<GarageVehicleLoadoutData> vehicleLoadouts = new();
    }

    [Serializable]
    private sealed class PreviousSaveData
    {
        public int version = 2;
        public int scrap;
        public string selectedVehicleId;
        public List<string> ownedVehicleIds = new();
        public List<string> ownedAttachmentIds = new();
        public List<GarageVehicleLoadoutData> vehicleLoadouts = new();
    }

    [Serializable]
    private sealed class LegacySaveData
    {
        public int version = 1;
        public int scrap;
        public string selectedVehicleId;
        public List<string> ownedVehicleIds = new();
        public List<string> ownedAttachmentIds = new();
        public List<string> equippedAttachmentIds = new();
    }

    [Header("Mission Rewards")]
    [SerializeField, Min(0)] private int scrapPerKill = 1;
    [SerializeField, Min(0)] private int successfulMissionBonus = 50;
    [SerializeField, Min(0)] private int startingScrap;
    [SerializeField] private GarageBuildState buildState;

    private int scrap;
    private long lifetimeZombieKills;
    private bool initialized;
    private bool restoring;

    public event Action Changed;

    public int Scrap => scrap;
    public long LifetimeZombieKills => lifetimeZombieKills;

    private void Reset()
    {
        buildState = GetComponent<GarageBuildState>();
    }

    private void Awake()
    {
        if (buildState == null)
        {
            buildState = GetComponent<GarageBuildState>();
        }
    }

    private void OnEnable()
    {
        if (buildState != null)
        {
            buildState.Changed += HandleBuildChanged;
        }
    }

    private IEnumerator Start()
    {
        GamePlatformService.EnsureExists();
        while (!GamePlatformService.IsReady)
        {
            yield return null;
        }

        LoadProgression();
        initialized = true;
        Changed?.Invoke();
    }

    private void OnDisable()
    {
        if (buildState != null)
        {
            buildState.Changed -= HandleBuildChanged;
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && initialized)
        {
            SaveProgression();
        }
    }

    private void OnApplicationQuit()
    {
        if (initialized)
        {
            SaveProgression();
        }
    }

    public bool CanAfford(int price)
    {
        return price >= 0 && scrap >= price;
    }

    public MissionReward AwardMission(
        MissionProgress progress,
        bool succeeded)
    {
        int killScrap = Mathf.Max(0, progress.Kills) * scrapPerKill;
        int completionBonus = succeeded ? successfulMissionBonus : 0;
        int total = killScrap + completionBonus;
        scrap = Mathf.Max(0, scrap + total);
        int safeKills = Mathf.Max(0, progress.Kills);
        lifetimeZombieKills = safeKills > long.MaxValue - lifetimeZombieKills
            ? long.MaxValue
            : lifetimeZombieKills + safeKills;
        SaveProgression();
        GamePlatformService.ReportLifetimeZombieKills(
            lifetimeZombieKills);
        Changed?.Invoke();
        return new MissionReward(
            killScrap,
            completionBonus,
            total,
            scrap);
    }

    public int GrantScrap(int amount)
    {
        int safeAmount = Mathf.Max(0, amount);
        if (safeAmount <= 0)
        {
            return scrap;
        }

        scrap = Mathf.Max(0, scrap + safeAmount);
        SaveProgression();
        Changed?.Invoke();
        return scrap;
    }

    public static string ReconcileCloudProgression(
        string preferredJson,
        string secondaryJson)
    {
        bool hasPreferred = TryParseCurrentSave(
            preferredJson,
            out SaveData preferred);
        bool hasSecondary = TryParseCurrentSave(
            secondaryJson,
            out SaveData secondary);

        if (!hasPreferred)
        {
            return hasSecondary ? secondaryJson : preferredJson;
        }

        if (!hasSecondary)
        {
            return preferredJson;
        }

        preferred.ownedVehicleIds ??= new List<string>();
        preferred.ownedAttachmentIds ??= new List<string>();
        preferred.vehicleLoadouts ??=
            new List<GarageVehicleLoadoutData>();
        MergeUniqueIds(
            preferred.ownedVehicleIds,
            secondary.ownedVehicleIds);
        MergeUniqueIds(
            preferred.ownedAttachmentIds,
            secondary.ownedAttachmentIds);
        preferred.lifetimeZombieKills = Math.Max(
            Math.Max(0L, preferred.lifetimeZombieKills),
            Math.Max(0L, secondary.lifetimeZombieKills));
        if (string.IsNullOrWhiteSpace(preferred.selectedVehicleId))
        {
            preferred.selectedVehicleId =
                secondary.selectedVehicleId ?? string.Empty;
        }

        MergeMissingLoadouts(
            preferred.vehicleLoadouts,
            secondary.vehicleLoadouts);
        return JsonUtility.ToJson(preferred);
    }

    public void ResetAfterPlayerAccountDeletion()
    {
        string[] progressionKeys =
        {
            SaveKey,
            PreviousSaveKey,
            LegacySaveKey,
            SaveKey + "_corrupt",
            PreviousSaveKey + "_corrupt",
            LegacySaveKey + "_corrupt"
        };
        foreach (string progressionKey in progressionKeys)
        {
            GamePlatformService.StorageDeleteKey(progressionKey);
        }

        restoring = true;
        try
        {
            ResetToDefaultProgression();
        }
        finally
        {
            restoring = false;
        }

        Changed?.Invoke();
    }

    public bool TryPurchaseVehicle(GarageVehicleDefinition vehicle)
    {
        if (vehicle == null
            || buildState.IsVehicleOwned(vehicle)
            || !CanAfford(vehicle.Price))
        {
            return false;
        }

        scrap -= vehicle.Price;
        restoring = true;
        try
        {
            buildState.GrantVehicle(vehicle);
        }
        finally
        {
            restoring = false;
        }

        SaveProgression();
        Changed?.Invoke();
        return true;
    }

    public bool TryPurchaseAttachment(
        GarageAttachmentDefinition attachment)
    {
        if (attachment == null
            || buildState.IsAttachmentOwned(attachment)
            || !CanAfford(attachment.Price))
        {
            return false;
        }

        scrap -= attachment.Price;
        restoring = true;
        try
        {
            buildState.GrantAttachment(attachment);
        }
        finally
        {
            restoring = false;
        }

        SaveProgression();
        Changed?.Invoke();
        return true;
    }

    private void LoadProgression()
    {
        bool migratedOlderSave = false;
        restoring = true;
        try
        {
            ResetToDefaultProgression();
            if (GamePlatformService.StorageHasKey(SaveKey))
            {
                RestoreCurrentSave(
                    GamePlatformService.StorageGetString(SaveKey));
            }
            else if (GamePlatformService.StorageHasKey(PreviousSaveKey))
            {
                migratedOlderSave = RestorePreviousSave(
                    GamePlatformService.StorageGetString(
                        PreviousSaveKey));
            }
            else if (GamePlatformService.StorageHasKey(LegacySaveKey))
            {
                migratedOlderSave =
                    RestoreLegacySave(
                        GamePlatformService.StorageGetString(
                            LegacySaveKey));
            }
        }
        finally
        {
            restoring = false;
        }

        if (migratedOlderSave)
        {
            SaveProgression();
        }

        GamePlatformService.ReportLifetimeZombieKills(
            lifetimeZombieKills);
    }

    private void RestoreCurrentSave(string json)
    {
        SaveData data;
        try
        {
            data = JsonUtility.FromJson<SaveData>(json);
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"Garage progression v3 save was corrupt and has been reset. {exception.Message}",
                this);
            ArchiveCorruptSave(SaveKey, json);
            return;
        }

        if (data == null)
        {
            Debug.LogWarning(
                "Garage progression v3 save was empty and has been reset.",
                this);
            ArchiveCorruptSave(SaveKey, json);
            return;
        }

        if (data.version != CurrentSaveVersion)
        {
            Debug.LogWarning(
                $"Garage progression save version {data.version} is unsupported; defaults are being used.",
                this);
            return;
        }

        scrap = Mathf.Max(0, data.scrap);
        lifetimeZombieKills = Math.Max(0L, data.lifetimeZombieKills);
        buildState.RestoreProgression(
            data.ownedVehicleIds,
            data.ownedAttachmentIds,
            data.selectedVehicleId,
            data.vehicleLoadouts);
    }

    private static bool TryParseCurrentSave(
        string json,
        out SaveData data)
    {
        data = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            data = JsonUtility.FromJson<SaveData>(json);
            return data != null && data.version == CurrentSaveVersion;
        }
        catch (Exception)
        {
            data = null;
            return false;
        }
    }

    private static void MergeUniqueIds(
        List<string> preferred,
        IReadOnlyList<string> secondary)
    {
        if (preferred == null || secondary == null)
        {
            return;
        }

        HashSet<string> knownIds = new(
            preferred,
            StringComparer.Ordinal);
        for (int i = 0; i < secondary.Count; i++)
        {
            string id = secondary[i];
            if (!string.IsNullOrWhiteSpace(id) && knownIds.Add(id))
            {
                preferred.Add(id);
            }
        }
    }

    private static void MergeMissingLoadouts(
        List<GarageVehicleLoadoutData> preferred,
        IReadOnlyList<GarageVehicleLoadoutData> secondary)
    {
        if (preferred == null || secondary == null)
        {
            return;
        }

        HashSet<string> knownVehicleIds = new(StringComparer.Ordinal);
        for (int i = 0; i < preferred.Count; i++)
        {
            GarageVehicleLoadoutData loadout = preferred[i];
            if (loadout != null
                && !string.IsNullOrWhiteSpace(loadout.vehicleId))
            {
                knownVehicleIds.Add(loadout.vehicleId);
            }
        }

        for (int i = 0; i < secondary.Count; i++)
        {
            GarageVehicleLoadoutData loadout = secondary[i];
            if (loadout == null
                || string.IsNullOrWhiteSpace(loadout.vehicleId)
                || !knownVehicleIds.Add(loadout.vehicleId))
            {
                continue;
            }

            preferred.Add(new GarageVehicleLoadoutData
            {
                vehicleId = loadout.vehicleId,
                attachmentIds = loadout.attachmentIds != null
                    ? new List<string>(loadout.attachmentIds)
                    : new List<string>()
            });
        }
    }

    private bool RestorePreviousSave(string json)
    {
        PreviousSaveData data;
        try
        {
            data = JsonUtility.FromJson<PreviousSaveData>(json);
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"Garage progression v2 save was corrupt and has been reset. {exception.Message}",
                this);
            ArchiveCorruptSave(PreviousSaveKey, json);
            return false;
        }

        if (data == null || data.version != 2)
        {
            Debug.LogWarning(
                "Garage progression v2 save could not be migrated; defaults are being used.",
                this);
            return false;
        }

        scrap = Mathf.Max(0, data.scrap);
        lifetimeZombieKills = 0L;
        buildState.RestoreProgression(
            data.ownedVehicleIds,
            data.ownedAttachmentIds,
            data.selectedVehicleId,
            data.vehicleLoadouts);
        return true;
    }

    private bool RestoreLegacySave(string json)
    {
        LegacySaveData data;
        try
        {
            data = JsonUtility.FromJson<LegacySaveData>(json);
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"Garage progression v1 save was corrupt and has been reset. {exception.Message}",
                this);
            ArchiveCorruptSave(LegacySaveKey, json);
            return false;
        }

        if (data == null || data.version != 1)
        {
            Debug.LogWarning(
                "Garage progression v1 save could not be migrated; defaults are being used.",
                this);
            return false;
        }

        List<GarageVehicleLoadoutData> loadouts = new();
        if (!string.IsNullOrWhiteSpace(data.selectedVehicleId))
        {
            loadouts.Add(new GarageVehicleLoadoutData
            {
                vehicleId = data.selectedVehicleId,
                attachmentIds = data.equippedAttachmentIds
                                ?? new List<string>()
            });
        }

        scrap = Mathf.Max(0, data.scrap);
        lifetimeZombieKills = 0L;
        buildState.RestoreProgression(
            data.ownedVehicleIds,
            data.ownedAttachmentIds,
            data.selectedVehicleId,
            loadouts);
        return true;
    }

    private static void ArchiveCorruptSave(string saveKey, string json)
    {
        GamePlatformService.StorageSetString(
            saveKey + "_corrupt",
            json ?? string.Empty);
        GamePlatformService.StorageDeleteKey(saveKey);
    }

    private void ResetToDefaultProgression()
    {
        scrap = Mathf.Max(0, startingScrap);
        lifetimeZombieKills = 0L;
        buildState?.RestoreProgression(
            null,
            null,
            string.Empty,
            null);
    }

    private void SaveProgression()
    {
        if (buildState == null)
        {
            return;
        }

        SaveData data = new SaveData
        {
            scrap = Mathf.Max(0, scrap),
            lifetimeZombieKills = Math.Max(0L, lifetimeZombieKills),
            selectedVehicleId =
                buildState.SelectedVehicle != null
                    ? buildState.SelectedVehicle.VehicleId
                    : string.Empty
        };

        foreach (string vehicleId in buildState.GetOwnedVehicleIds())
        {
            data.ownedVehicleIds.Add(vehicleId);
        }

        foreach (string attachmentId in buildState.GetOwnedAttachmentIds())
        {
            data.ownedAttachmentIds.Add(attachmentId);
        }

        data.vehicleLoadouts = buildState.CreateLoadoutSaveData();

        GamePlatformService.StorageSetString(
            SaveKey,
            JsonUtility.ToJson(data));
    }

    private void HandleBuildChanged()
    {
        if (!initialized || restoring)
        {
            return;
        }

        SaveProgression();
        Changed?.Invoke();
    }
}
