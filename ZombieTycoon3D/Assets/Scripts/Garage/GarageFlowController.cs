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
    private const float MissionIntroStepSeconds = 0.65f;
    private const int MissionIntroStepCount = 4;

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
    [SerializeField] private MobileVehicleInputController mobileInputController;
    [SerializeField] private Player player;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private OldSpawnManager spawnManager;
    [SerializeField] private ScoreManager scoreManager;

    private DeathEffectPool deathEffectPool;
    private Vector3 missionStartPosition;
    private Quaternion missionStartRotation;
    private bool missionActive;
    private bool missionIntroActive;
    private bool missionPaused;
    private bool resultVisible;
    private float missionTimeRemaining;
    private float missionIntroStartTime;
    private int displayedMissionIntroStep = -1;
    private float timeScaleBeforePause = 1f;
    private bool ownsTimeScalePause;
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
    private MissionResult currentMissionResult;
    private bool missionRewardDoubled;

    private void Awake()
    {
        GamePlatformService.EnsureExists();

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

        if (mobileInputController == null)
        {
            mobileInputController =
                GetComponent<MobileVehicleInputController>();
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
            garageUi.MissionPauseRequested += PauseMission;
            garageUi.MissionResumeRequested += ResumeMission;
            garageUi.MissionRestartRequested += RestartMission;
            garageUi.MissionGarageRequested += ReturnToGarageFromPause;
            garageUi.RewardedMissionDoubleRequested +=
                HandleRewardedMissionDoubleRequested;
            garageUi.SalvageDropRequested +=
                HandleSalvageDropRequested;
        }

        if (scoreManager != null)
        {
            scoreManager.ProgressChanged += HandleMissionProgressChanged;
            scoreManager.MayhemChanged += HandleMayhemChanged;
            scoreManager.SpecialKillScored += HandleSpecialKillScored;
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

        mobileInputController?.Configure(
            vehicleController,
            gameplayCamera);
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
        if (missionIntroActive)
        {
            UpdateMissionIntro();
            return;
        }

        if (!missionActive)
        {
            return;
        }

        if (Input.GetButtonDown("pause"))
        {
            if (garageUi.SettingsVisible)
            {
                garageUi.CloseSettingsPanel();
            }
            else if (missionPaused)
            {
                ResumeMission();
            }
            else
            {
                PauseMission();
            }

            return;
        }

        if (missionPaused)
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
            garageUi.MissionPauseRequested -= PauseMission;
            garageUi.MissionResumeRequested -= ResumeMission;
            garageUi.MissionRestartRequested -= RestartMission;
            garageUi.MissionGarageRequested -= ReturnToGarageFromPause;
            garageUi.RewardedMissionDoubleRequested -=
                HandleRewardedMissionDoubleRequested;
            garageUi.SalvageDropRequested -=
                HandleSalvageDropRequested;
        }

        if (scoreManager != null)
        {
            scoreManager.ProgressChanged -= HandleMissionProgressChanged;
            scoreManager.MayhemChanged -= HandleMayhemChanged;
            scoreManager.SpecialKillScored -= HandleSpecialKillScored;
        }

        if (player != null)
        {
            player.AttachmentFeedbackRequested -=
                HandleAttachmentFeedbackRequested;
        }

        EventManager.OnPlayerDeath -= HandlePlayerDeath;
        if (Application.isPlaying)
        {
            GamePlatformService.SetGameplayActive(false);
        }
        RestoreTemporalState();
    }

    private void OnDestroy()
    {
        RestoreTemporalState();
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
                     && (!GamePlatformService.UsesTouchControls
                         || mobileInputController != null)
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
            || missionIntroActive
            || resultVisible
            || GamePlatformService.IsAdRequestInProgress
            || buildState.SelectedVehicle == null)
        {
            return;
        }

        ResetVehiclePose();
        vehicleRigidbody.isKinematic = true;
        driveRigidbody.isKinematic = true;

        VehicleStats stats = buildState.CurrentStats;
        activeBuildEffects = buildState.CurrentEffects;
        vehicleController.MaxSpeed = stats.maxSpeed;
        vehicleController.accelaration = stats.acceleration;
        vehicleController.turn = stats.handling;
        ApplyVehiclePhysics(buildState.SelectedVehicle, activeBuildEffects);
        player.ApplyVehicleStats(stats);
        player.ConfigureBuildEffects(activeBuildEffects);
        player.ResetForRun();
        // ResetForRun Rigidbody'yi açar; intro boyunca hareket kapalı kalır.
        vehicleRigidbody.isKinematic = true;
        driveRigidbody.isKinematic = true;
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
        scoreManager.CancelMission();
        gameplayBuildPresenter.ApplyBuild(
            buildState.SelectedVehicle,
            buildState.GetEquippedAttachments());
        gameplayBuildPresenter.SetVisible(true);

        vehicleController.ProvideInputs(0f, 0f, 0f);
        vehicleController.enabled = false;
        SetVehicleInputEnabled(false);
        gameplayCamera.enabled = true;
        SetGameplayAudioEnabled(false);

        missionActive = false;
        missionIntroActive = true;
        missionPaused = false;
        resultVisible = false;
        missionTimeRemaining = MissionDurationSeconds;
        spawnManager.SetSpawningEnabled(false);
        garageUi.UpdateMissionTimer(missionTimeRemaining);
        garageUi.SetMissionSpeedTarget(0f);
        garageUi.UpdateMissionHealth(
            player.GetCurrentHealth(),
            player.GetMaxHealth());
        garageUi.HideGarageForMission();
        garageUi.ShowMissionIntro(
            buildState.SelectedVehicle.DisplayName);
        missionIntroStartTime = Time.unscaledTime;
        displayedMissionIntroStep = -1;
        UpdateMissionIntro();
    }

    private void OpenGarage()
    {
        RestoreTemporalState();
        GamePlatformService.SetGameplayActive(false);
        missionActive = false;
        missionIntroActive = false;
        missionPaused = false;
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

        RestoreTemporalState();
        GamePlatformService.SetGameplayActive(false);
        missionActive = false;
        missionPaused = false;
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
        currentMissionResult = result;
        missionRewardDoubled = false;

        StopMissionGameplay(true);
        resultVisible = true;
        garageUi.ShowMissionResult(result);
        if (succeeded)
        {
            GamePlatformService.ReportHappyTime();
        }
    }

    private void UpdateMissionIntro()
    {
        float elapsed = Time.unscaledTime - missionIntroStartTime;
        int step = Mathf.FloorToInt(
            elapsed / MissionIntroStepSeconds);
        if (step >= MissionIntroStepCount)
        {
            ActivateMission();
            return;
        }

        if (step == displayedMissionIntroStep)
        {
            return;
        }

        displayedMissionIntroStep = step;
        garageUi.UpdateMissionIntroCountdown(step switch
        {
            0 => "3",
            1 => "2",
            2 => "1",
            _ => "CRUSH!"
        });
    }

    private void ActivateMission()
    {
        if (!missionIntroActive)
        {
            return;
        }

        missionIntroActive = false;
        vehicleRigidbody.isKinematic = false;
        driveRigidbody.isKinematic = false;
        vehicleController.enabled = true;
        SetVehicleInputEnabled(true);
        SetGameplayAudioEnabled(true);
        scoreManager.BeginMission(
            MissionKillTarget,
            NormalKillScore,
            BonusKillScore);
        missionActive = true;
        spawnManager.BeginMission();
        garageUi.CompleteMissionIntro();
        GamePlatformService.SetGameplayActive(true);
    }

    private void PauseMission()
    {
        if (!missionActive || missionPaused)
        {
            return;
        }

        missionPaused = true;
        GamePlatformService.SetGameplayActive(false);
        scoreManager.SetMissionPaused(true);
        spawnManager.SetSpawningEnabled(false);
        SetVehicleInputEnabled(false);
        vehicleController.ProvideInputs(0f, 0f, 0f);
        vehicleController.enabled = false;

        timeScaleBeforePause = Time.timeScale;
        Time.timeScale = 0f;
        ownsTimeScalePause = true;
        AudioListener.pause = true;
        garageUi.ShowMissionPause(
            missionTimeRemaining,
            scoreManager.CurrentProgress);
    }

    private void ResumeMission()
    {
        if (!missionActive || !missionPaused)
        {
            return;
        }

        RestoreTemporalState();
        missionPaused = false;
        GamePlatformService.SetGameplayActive(true);
        scoreManager.SetMissionPaused(false);
        vehicleController.enabled = true;
        SetVehicleInputEnabled(true);
        spawnManager.SetSpawningEnabled(true);
        garageUi.HideMissionPause();
    }

    private void RestartMission()
    {
        if (!missionActive || !missionPaused)
        {
            return;
        }

        RestoreTemporalState();
        GamePlatformService.SetGameplayActive(false);
        missionActive = false;
        missionPaused = false;
        scoreManager.CancelMission();
        StopMissionGameplay(false);
        gameplayBuildPresenter.SetVisible(false);
        StartMission();
    }

    private void ReturnToGarageFromPause()
    {
        if (missionActive && missionPaused)
        {
            OpenGarage();
        }
    }

    private void RestoreTemporalState()
    {
        if (ownsTimeScalePause)
        {
            Time.timeScale = Mathf.Max(0f, timeScaleBeforePause);
            ownsTimeScalePause = false;
        }

        AudioListener.pause = false;
    }

    private void StopMissionGameplay(bool keepGameplayCamera)
    {
        spawnManager.SetSpawningEnabled(false);
        spawnManager.DespawnAllToPool();

        SetVehicleInputEnabled(false);
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

    private void SetVehicleInputEnabled(bool inputEnabled)
    {
        bool useTouchInput =
            GamePlatformService.UsesTouchControls
            && mobileInputController != null;

        inputController.enabled = inputEnabled && !useTouchInput;

        if (mobileInputController == null)
        {
            return;
        }

        mobileInputController.enabled = inputEnabled && useTouchInput;
        if (!inputEnabled)
        {
            mobileInputController.ResetInput();
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
        if (resultVisible
            && !GamePlatformService.IsAdRequestInProgress)
        {
            OpenGarage();
        }
    }

    private void HandleRewardedMissionDoubleRequested()
    {
        if (!resultVisible
            || !currentMissionResult.CanDoubleScrap
            || missionRewardDoubled)
        {
            return;
        }

        garageUi.SetMissionRewardedPending(true);
        GamePlatformService.RequestRewardedAd(
            () =>
            {
                if (this == null || !resultVisible)
                {
                    return;
                }

                int bonusScrap =
                    currentMissionResult.Reward.TotalScrap;
                int newBalance = economy.GrantScrap(bonusScrap);
                missionRewardDoubled = true;
                garageUi.ShowRewardedMissionGranted(
                    bonusScrap,
                    newBalance);
            },
            message =>
            {
                if (this != null)
                {
                    garageUi.ShowRewardedMissionUnavailable(message);
                }
            });
    }

    private void HandleSalvageDropRequested()
    {
        if (missionActive
            || missionIntroActive
            || resultVisible
            || GamePlatformService.IsAdRequestInProgress
            || !GamePlatformService.SupportsSalvageDrop)
        {
            return;
        }

        garageUi.SetSalvageDropPending(true);
        GamePlatformService.RequestRewardedAd(
            () =>
            {
                if (this == null)
                {
                    return;
                }

                int amount =
                    GamePlatformService.SalvageDropScrap;
                economy.GrantScrap(amount);
                garageUi.ShowSalvageDropGranted(amount);
            },
            message =>
            {
                if (this != null)
                {
                    garageUi.ShowSalvageDropUnavailable(message);
                }
            });
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

    private void HandleSpecialKillScored(ZombieScoreAward award)
    {
        if (!missionActive
            || missionPaused
            || string.IsNullOrWhiteSpace(award.FeedbackLabel))
        {
            return;
        }

        ShowAttachmentFeedback(
            $"{award.FeedbackLabel}  ·  +{award.BonusScore:N0} SCORE",
            GarageAttachmentFeedbackTone.Impact);
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
