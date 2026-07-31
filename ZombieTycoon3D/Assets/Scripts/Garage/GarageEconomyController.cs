using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(GarageBuildState))]
public sealed class GarageEconomyController : MonoBehaviour
{
    private const int CurrentSaveVersion = 2;
    private const string SaveKey = "zt3d.garage-progression.v2";
    private const string LegacySaveKey = "zt3d.garage-progression.v1";

    [Serializable]
    private sealed class SaveData
    {
        public int version = CurrentSaveVersion;
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
    private bool initialized;
    private bool restoring;

    public event Action Changed;

    public int Scrap => scrap;

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

    private void Start()
    {
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
        SaveProgression();
        Changed?.Invoke();
        return new MissionReward(
            killScrap,
            completionBonus,
            total,
            scrap);
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
        bool migratedLegacySave = false;
        restoring = true;
        try
        {
            ResetToDefaultProgression();
            if (PlayerPrefs.HasKey(SaveKey))
            {
                RestoreCurrentSave(PlayerPrefs.GetString(SaveKey));
                return;
            }

            if (PlayerPrefs.HasKey(LegacySaveKey))
            {
                migratedLegacySave =
                    RestoreLegacySave(PlayerPrefs.GetString(LegacySaveKey));
            }
        }
        finally
        {
            restoring = false;
        }

        if (migratedLegacySave)
        {
            SaveProgression();
        }
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
                $"Garage progression v2 save was corrupt and has been reset. {exception.Message}",
                this);
            ArchiveCorruptSave(SaveKey, json);
            return;
        }

        if (data == null)
        {
            Debug.LogWarning(
                "Garage progression v2 save was empty and has been reset.",
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
        buildState.RestoreProgression(
            data.ownedVehicleIds,
            data.ownedAttachmentIds,
            data.selectedVehicleId,
            data.vehicleLoadouts);
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
        buildState.RestoreProgression(
            data.ownedVehicleIds,
            data.ownedAttachmentIds,
            data.selectedVehicleId,
            loadouts);
        return true;
    }

    private static void ArchiveCorruptSave(string saveKey, string json)
    {
        PlayerPrefs.SetString(saveKey + ".corrupt", json ?? string.Empty);
        PlayerPrefs.DeleteKey(saveKey);
        PlayerPrefs.Save();
    }

    private void ResetToDefaultProgression()
    {
        scrap = Mathf.Max(0, startingScrap);
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

        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
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
