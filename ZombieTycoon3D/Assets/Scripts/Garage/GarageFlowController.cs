using ArcadeVP;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GarageFlowController : MonoBehaviour
{
    private const float MissionDurationSeconds = 120f;
    private const int MissionKillTarget = 100;
    private const int NormalKillScore = 100;
    private const int BonusKillScore = 200;

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
        spawnManager.SetMissionProgress(
            (MissionDurationSeconds - missionTimeRemaining)
            / MissionDurationSeconds);
        garageUi.UpdateMissionTimer(missionTimeRemaining);
        garageUi.UpdateMissionHealth(
            player.GetCurrentHealth(),
            player.GetMaxHealth());
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
        vehicleController.MaxSpeed = stats.maxSpeed;
        vehicleController.accelaration = stats.acceleration;
        vehicleController.turn = stats.handling;
        ApplyVehiclePhysics(buildState.SelectedVehicle);
        player.ApplyVehicleStats(stats);
        player.ResetForRun();
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

    private void ApplyVehiclePhysics(GarageVehicleDefinition vehicle)
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
        vehicleController.downforce = vehicle.Downforce;
        vehicleController.frictionCurve =
            ScaleCurve(baseFrictionCurve, vehicle.LateralGrip);

        if (runtimeFrictionMaterial != null
            && originalFrictionMaterial != null)
        {
            runtimeFrictionMaterial.staticFriction =
                originalFrictionMaterial.staticFriction
                * vehicle.LateralGrip;
            runtimeFrictionMaterial.dynamicFriction =
                originalFrictionMaterial.dynamicFriction
                * vehicle.LateralGrip;
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
