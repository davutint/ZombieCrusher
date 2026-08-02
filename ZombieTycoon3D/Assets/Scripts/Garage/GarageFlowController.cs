using ArcadeVP;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GarageFlowController : MonoBehaviour
{
    private const float MissionDurationSeconds = 120f;
    private const int MissionKillTarget = 100;
    private const int NormalKillScore = 100;
    private const int BonusKillScore = 200;
    private const float HealthRefreshInterval = 0.06f;

    [Header("Garage")]
    [SerializeField] private GarageBuildState buildState;
    [SerializeField] private GarageEconomyController economy;
    [SerializeField] private GarageUiController garageUi;
    [SerializeField] private GarageGameplayBuildPresenter gameplayBuildPresenter;

    [Header("Gameplay")]
    [SerializeField] private Transform gameplayVehicle;
    [SerializeField] private Rigidbody vehicleRigidbody;
    [SerializeField] private Rigidbody driveRigidbody;
    [SerializeField] private ArcadeVehicleController vehicleController;
    [SerializeField] private InputManager_ArcadeVP inputController;
    [SerializeField] private Player player;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private OldSpawnManager spawnManager;
    [SerializeField] private ScoreManager scoreManager;

    private DeathEffectPool deathEffectPool;
    private Vector3 missionStartPosition;
    private Quaternion missionStartRotation;
    private bool missionActive;
    private bool resultVisible;
    private float missionTimeRemaining;
    private Vector3 driveStartPosition;
    private Quaternion driveStartRotation;
    private AudioSource[] gameplayAudioSources;
    private AnimationCurve baseFrictionCurve;
    private PhysicsMaterial originalFrictionMaterial;
    private PhysicsMaterial originalDriveColliderMaterial;
    private PhysicsMaterial runtimeFrictionMaterial;
    private SphereCollider driveCollider;
    private GarageBuildEffects activeBuildEffects = GarageBuildEffects.Neutral;
    private int missionKills;
    private int repairsUsed;
    private int nextRepairKill;
    private float feedbackHideTime;
    private float nextFeedbackUpdateTime;
    private float nextHealthUpdateTime;
    private bool feedbackVisible;
    private MayhemTier previousMayhemTier;

    private void Awake()
    {
        if (buildState == null)
        {
            buildState = GetComponent<GarageBuildState>();
        }

        if (garageUi == null)
        {
            garageUi = GetComponent<GarageUiController>();
        }

        if (economy == null)
        {
            economy = GetComponent<GarageEconomyController>();
        }

        if (gameplayBuildPresenter == null)
        {
            gameplayBuildPresenter = GetComponent<GarageGameplayBuildPresenter>();
        }

        if (scoreManager == null)
        {
            scoreManager = FindFirstObjectByType<ScoreManager>();
        }

        deathEffectPool = FindFirstObjectByType<DeathEffectPool>();
    }

    private void OnEnable()
    {
        if (garageUi != null)
        {
            garageUi.MissionRequested += StartMission;
            garageUi.ResultAcknowledged += HandleResultAcknowledged;
        }

        if (scoreManager != null)
        {
            scoreManager.ProgressChanged += HandleMissionProgressChanged;
            scoreManager.MayhemChanged += HandleMayhemChanged;
        }

        if (player != null)
        {
            player.AttachmentFeedbackRequested +=
                HandleAttachmentFeedbackRequested;
        }

        EventManager.OnPlayerDeath += HandlePlayerDeath;
    }

    private void Start()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        spawnManager.SetGameplayCamera(gameplayCamera);
        missionStartPosition = gameplayVehicle.position;
        missionStartRotation = gameplayVehicle.rotation;
        driveStartPosition = driveRigidbody.position;
        driveStartRotation = driveRigidbody.rotation;
        gameplayAudioSources =
            gameplayVehicle.GetComponentsInChildren<AudioSource>(true);
        PrepareRuntimeFriction();
        OpenGarage();
    }

    private void Update()
    {
        if (!missionActive)
        {
            return;
        }

        missionTimeRemaining =
            Mathf.Max(0f, missionTimeRemaining - Time.unscaledDeltaTime);
        ProcessPendingRepair();
        UpdateFeedbackVisibility();
        spawnManager.SetMissionProgress(
            (MissionDurationSeconds - missionTimeRemaining)
            / MissionDurationSeconds);
        garageUi.UpdateMissionTimer(missionTimeRemaining);
        float currentSpeed = Mathf.Abs(vehicleController.carVelocity.z);
        garageUi.SetMissionSpeedTarget(currentSpeed);
        float currentTime = Time.unscaledTime;
        if (currentTime >= nextHealthUpdateTime)
        {
            nextHealthUpdateTime =
                currentTime + HealthRefreshInterval;
            garageUi.UpdateMissionHealth(
                player.GetCurrentHealth(),
                player.GetMaxHealth());
        }

        if (missionTimeRemaining <= 0f)
        {
            EndMission(MissionEndReason.TimeExpired);
        }
    }

    private void OnDisable()
    {
        if (garageUi != null)
        {
            garageUi.MissionRequested -= StartMission;
            garageUi.ResultAcknowledged -= HandleResultAcknowledged;
        }

        if (scoreManager != null)
        {
            scoreManager.ProgressChanged -= HandleMissionProgressChanged;
            scoreManager.MayhemChanged -= HandleMayhemChanged;
        }

        if (player != null)
        {
            player.AttachmentFeedbackRequested -=
                HandleAttachmentFeedbackRequested;
        }

        EventManager.OnPlayerDeath -= HandlePlayerDeath;
    }

    private void OnDestroy()
    {
        if (runtimeFrictionMaterial == null)
        {
            return;
        }

        if (vehicleController != null)
        {
            vehicleController.frictionMaterial = originalFrictionMaterial;
        }

        if (driveCollider != null)
        {
            driveCollider.sharedMaterial = originalDriveColliderMaterial;
        }

        Destroy(runtimeFrictionMaterial);
    }

    private bool ValidateReferences()
    {
        bool valid = buildState != null
                      && economy != null
                      && garageUi != null
                     && gameplayBuildPresenter != null
                     && gameplayVehicle != null
                     && vehicleRigidbody != null
                     && driveRigidbody != null
                     && vehicleController != null
                     && inputController != null
                     && player != null
                     && gameplayCamera != null
                     && spawnManager != null
                     && scoreManager != null;

        if (!valid)
        {
            Debug.LogError(
                "GarageFlowController: Garage and gameplay references are required.",
                this);
        }

        return valid;
    }

    private void StartMission()
    {
        if (missionActive
            || resultVisible
            || buildState.SelectedVehicle == null)
        {
            return;
        }

        ResetVehiclePose();
        vehicleRigidbody.isKinematic = false;
        driveRigidbody.isKinematic = false;

        VehicleStats stats = buildState.CurrentStats;
        activeBuildEffects = buildState.CurrentEffects;
        vehicleController.MaxSpeed = stats.maxSpeed;
        vehicleController.accelaration = stats.acceleration;
        vehicleController.turn = stats.handling;
        ApplyVehiclePhysics(buildState.SelectedVehicle, activeBuildEffects);
        player.ApplyVehicleStats(stats);
        player.ConfigureBuildEffects(activeBuildEffects);
        player.ResetForRun();
        garageUi.ConfigureMissionVehicle(
            buildState.SelectedVehicle.DisplayName,
            stats.maxSpeed);
        missionKills = 0;
        repairsUsed = 0;
        nextRepairKill = activeBuildEffects.HasRepair
            ? activeBuildEffects.RepairEveryKills
            : int.MaxValue;
        feedbackHideTime = 0f;
        nextFeedbackUpdateTime = 0f;
        nextHealthUpdateTime =
            Time.unscaledTime + HealthRefreshInterval;
        garageUi.HideMissionEffectFeedback();
        feedbackVisible = false;
        previousMayhemTier = MayhemTier.None;
        player.SetMayhemIntensity(0f);
        deathEffectPool?.SetMayhemIntensity(0f);
        scoreManager.BeginMission(
            MissionKillTarget,
            NormalKillScore,
            BonusKillScore);
        gameplayBuildPresenter.ApplyBuild(
            buildState.SelectedVehicle,
            buildState.GetEquippedAttachments());
        gameplayBuildPresenter.SetVisible(true);

        vehicleController.enabled = true;
        inputController.enabled = true;
        gameplayCamera.enabled = true;
        SetGameplayAudioEnabled(true);

        missionActive = true;
        resultVisible = false;
        missionTimeRemaining = MissionDurationSeconds;
        spawnManager.BeginMission();
        garageUi.UpdateMissionTimer(missionTimeRemaining);
        garageUi.SetMissionSpeedTarget(0f);
        garageUi.UpdateMissionHealth(
            player.GetCurrentHealth(),
            player.GetMaxHealth());
        garageUi.HideGarageForMission();
    }

    private void OpenGarage()
    {
        missionActive = false;
        resultVisible = false;
        missionTimeRemaining = 0f;
        scoreManager.CancelMission();

        StopMissionGameplay(false);
        gameplayBuildPresenter.SetVisible(false);

        ResetVehiclePose();
        garageUi.ShowGarage();
    }

    private void EndMission(MissionEndReason endReason)
    {
        if (!missionActive)
        {
            return;
        }

        missionActive = false;
        MissionProgress progress = scoreManager.FinishMission();
        bool succeeded =
            endReason == MissionEndReason.TimeExpired
            && progress.TargetReached;
        MissionReward reward =
            economy.AwardMission(progress, succeeded);
        MissionResult result = new MissionResult(
            endReason,
            succeeded,
            progress,
            reward,
            missionTimeRemaining,
            player.GetCurrentHealth(),
            player.GetMaxHealth());

        StopMissionGameplay(true);
        resultVisible = true;
        garageUi.ShowMissionResult(result);
    }

    private void StopMissionGameplay(bool keepGameplayCamera)
    {
        spawnManager.SetSpawningEnabled(false);
        spawnManager.DespawnAllToPool();

        inputController.enabled = false;
        vehicleController.enabled = false;
        vehicleController.ProvideInputs(0f, 0f, 0f);
        if (!vehicleRigidbody.isKinematic)
        {
            vehicleRigidbody.linearVelocity = Vector3.zero;
            vehicleRigidbody.angularVelocity = Vector3.zero;
        }

        vehicleRigidbody.isKinematic = true;
        if (!driveRigidbody.isKinematic)
        {
            driveRigidbody.linearVelocity = Vector3.zero;
            driveRigidbody.angularVelocity = Vector3.zero;
        }

        driveRigidbody.isKinematic = true;
        gameplayCamera.enabled = keepGameplayCamera;
        SetGameplayAudioEnabled(false);
        player.ResetBuildEffects();
        activeBuildEffects = GarageBuildEffects.Neutral;
        missionKills = 0;
        repairsUsed = 0;
        nextRepairKill = int.MaxValue;
        feedbackHideTime = 0f;
        nextFeedbackUpdateTime = 0f;
        feedbackVisible = false;
        previousMayhemTier = MayhemTier.None;
        player.SetMayhemIntensity(0f);
        deathEffectPool?.SetMayhemIntensity(0f);
        garageUi.HideMissionEffectFeedback();

        if (buildState != null && buildState.SelectedVehicle != null)
        {
            ApplyVehiclePhysics(
                buildState.SelectedVehicle,
                GarageBuildEffects.Neutral);
        }
    }

    private void ResetVehiclePose()
    {
        gameplayVehicle.SetPositionAndRotation(
            missionStartPosition,
            missionStartRotation);
        vehicleRigidbody.position = missionStartPosition;
        vehicleRigidbody.rotation = missionStartRotation;
        if (!vehicleRigidbody.isKinematic)
        {
            vehicleRigidbody.linearVelocity = Vector3.zero;
            vehicleRigidbody.angularVelocity = Vector3.zero;
        }

        driveRigidbody.position = driveStartPosition;
        driveRigidbody.rotation = driveStartRotation;
        if (!driveRigidbody.isKinematic)
        {
            driveRigidbody.linearVelocity = Vector3.zero;
            driveRigidbody.angularVelocity = Vector3.zero;
        }
    }

    private void HandlePlayerDeath()
    {
        if (missionActive)
        {
            EndMission(MissionEndReason.VehicleDestroyed);
        }
    }

    private void HandleResultAcknowledged()
    {
        if (resultVisible)
        {
            OpenGarage();
        }
    }

    private void HandleMissionProgressChanged(MissionProgress progress)
    {
        garageUi.UpdateMissionProgress(progress);
        if (missionActive)
        {
            missionKills = progress.Kills;
        }
    }

    private void HandleMayhemChanged(MayhemProgress progress)
    {
        garageUi.UpdateMayhem(progress);
        player.SetMayhemIntensity(progress.Meter01);
        deathEffectPool?.SetMayhemIntensity(progress.Meter01);

        bool tierReached = missionActive
            && (int)progress.Tier > (int)previousMayhemTier
            && progress.Tier != MayhemTier.None;
        if (tierReached)
        {
            garageUi.ShowMayhemTierReached(progress);
            player.PlayMayhemTierReached(progress.Tier);
        }

        previousMayhemTier = progress.Tier;
    }

    private void HandleAttachmentFeedbackRequested(
        string message,
        GarageAttachmentFeedbackTone tone)
    {
        ShowAttachmentFeedback(message, tone);
    }

    private void ShowAttachmentFeedback(
        string message,
        GarageAttachmentFeedbackTone tone)
    {
        if (!missionActive
            || string.IsNullOrWhiteSpace(message)
            || Time.unscaledTime < nextFeedbackUpdateTime)
        {
            return;
        }

        nextFeedbackUpdateTime = Time.unscaledTime + 0.08f;
        feedbackHideTime = Time.unscaledTime + 1.15f;
        feedbackVisible = true;
        garageUi.ShowMissionEffectFeedback(message, tone);
    }

    private void ProcessPendingRepair()
    {
        if (!activeBuildEffects.HasRepair
            || repairsUsed >= activeBuildEffects.MaximumRepairs
            || missionKills < nextRepairKill)
        {
            return;
        }

        while (repairsUsed < activeBuildEffects.MaximumRepairs
               && missionKills >= nextRepairKill)
        {
            repairsUsed++;
            nextRepairKill += activeBuildEffects.RepairEveryKills;
            float restored = player.RestoreHealth(
                activeBuildEffects.RepairAmount);
            if (restored <= 0f)
            {
                continue;
            }

            string label = activeBuildEffects.RepairFeedbackLabel;
            string message = string.IsNullOrWhiteSpace(label)
                ? $"+{restored:0.#} HP"
                : $"{label}  ·  +{restored:0.#} HP";
            ShowAttachmentFeedback(
                message,
                activeBuildEffects.RepairFeedbackTone);
        }
    }

    private void UpdateFeedbackVisibility()
    {
        if (!feedbackVisible || Time.unscaledTime < feedbackHideTime)
        {
            return;
        }

        feedbackVisible = false;
        garageUi.HideMissionEffectFeedback();
    }

    private void SetGameplayAudioEnabled(bool enabled)
    {
        if (gameplayAudioSources == null)
        {
            return;
        }

        for (int i = 0; i < gameplayAudioSources.Length; i++)
        {
            if (gameplayAudioSources[i] != null)
            {
                gameplayAudioSources[i].enabled = enabled;
            }
        }
    }

    private void PrepareRuntimeFriction()
    {
        baseFrictionCurve = CopyCurve(vehicleController.frictionCurve);
        driveCollider = driveRigidbody.GetComponent<SphereCollider>();
        originalDriveColliderMaterial =
            driveCollider != null ? driveCollider.sharedMaterial : null;
        originalFrictionMaterial = vehicleController.frictionMaterial;
        if (originalFrictionMaterial == null && driveCollider != null)
        {
            originalFrictionMaterial = originalDriveColliderMaterial;
        }

        if (originalFrictionMaterial == null)
        {
            return;
        }

        runtimeFrictionMaterial = Instantiate(originalFrictionMaterial);
        runtimeFrictionMaterial.name =
            originalFrictionMaterial.name + " (Garage Runtime)";
        runtimeFrictionMaterial.hideFlags = HideFlags.DontSave;
        vehicleController.frictionMaterial = runtimeFrictionMaterial;
        if (driveCollider != null)
        {
            driveCollider.sharedMaterial = runtimeFrictionMaterial;
        }
    }

    private void ApplyVehiclePhysics(
        GarageVehicleDefinition vehicle,
        GarageBuildEffects effects)
    {
        if (vehicle == null)
        {
            return;
        }

        vehicleRigidbody.mass = vehicle.BodyMass;
        vehicleRigidbody.automaticCenterOfMass = false;
        vehicleRigidbody.centerOfMass = vehicle.CenterOfMass;
        driveRigidbody.mass = vehicle.DriveMass;
        driveRigidbody.automaticCenterOfMass = false;
        driveRigidbody.centerOfMass = vehicle.DriveCenterOfMass;
        vehicleController.gravity = vehicle.Gravity;
        float lateralGrip =
            vehicle.LateralGrip * effects.LateralGripMultiplier;
        vehicleController.downforce =
            vehicle.Downforce + effects.DownforceBonus;
        vehicleController.frictionCurve =
            ScaleCurve(baseFrictionCurve, lateralGrip);

        if (runtimeFrictionMaterial != null
            && originalFrictionMaterial != null)
        {
            runtimeFrictionMaterial.staticFriction =
                originalFrictionMaterial.staticFriction
                * lateralGrip;
            runtimeFrictionMaterial.dynamicFriction =
                originalFrictionMaterial.dynamicFriction
                * lateralGrip;
        }
    }

    private static AnimationCurve CopyCurve(AnimationCurve source)
    {
        if (source == null)
        {
            return new AnimationCurve();
        }

        AnimationCurve copy = new AnimationCurve(source.keys)
        {
            preWrapMode = source.preWrapMode,
            postWrapMode = source.postWrapMode
        };
        return copy;
    }

    private static AnimationCurve ScaleCurve(
        AnimationCurve source,
        float multiplier)
    {
        if (source == null)
        {
            return new AnimationCurve();
        }

        Keyframe[] keys = source.keys;
        for (int i = 0; i < keys.Length; i++)
        {
            Keyframe key = keys[i];
            key.value *= multiplier;
            key.inTangent *= multiplier;
            key.outTangent *= multiplier;
            keys[i] = key;
        }

        AnimationCurve scaled = new AnimationCurve(keys)
        {
            preWrapMode = source.preWrapMode,
            postWrapMode = source.postWrapMode
        };
        return scaled;
    }
}
