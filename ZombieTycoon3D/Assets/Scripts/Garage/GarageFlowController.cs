using ArcadeVP;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GarageFlowController : MonoBehaviour
{
    private const float MissionDurationSeconds = 120f;

    [Header("Garage")]
    [SerializeField] private GarageBuildState buildState;
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

    private Vector3 missionStartPosition;
    private Quaternion missionStartRotation;
    private bool missionActive;
    private float missionTimeRemaining;
    private Vector3 driveStartPosition;
    private Quaternion driveStartRotation;
    private AudioSource[] gameplayAudioSources;

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

        if (gameplayBuildPresenter == null)
        {
            gameplayBuildPresenter = GetComponent<GarageGameplayBuildPresenter>();
        }
    }

    private void OnEnable()
    {
        if (garageUi != null)
        {
            garageUi.MissionRequested += StartMission;
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
        garageUi.UpdateMissionTimer(missionTimeRemaining);
        if (missionTimeRemaining <= 0f)
        {
            OpenGarage();
        }
    }

    private void OnDisable()
    {
        if (garageUi != null)
        {
            garageUi.MissionRequested -= StartMission;
        }

        EventManager.OnPlayerDeath -= HandlePlayerDeath;
    }

    private bool ValidateReferences()
    {
        bool valid = buildState != null
                     && garageUi != null
                     && gameplayBuildPresenter != null
                     && gameplayVehicle != null
                     && vehicleRigidbody != null
                     && driveRigidbody != null
                     && vehicleController != null
                     && inputController != null
                     && player != null
                     && gameplayCamera != null
                     && spawnManager != null;

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
        if (missionActive || buildState.SelectedVehicle == null)
        {
            return;
        }

        ResetVehiclePose();

        VehicleStats stats = buildState.CurrentStats;
        vehicleController.MaxSpeed = stats.maxSpeed;
        vehicleController.accelaration = stats.acceleration;
        vehicleController.turn = stats.handling;
        player.ApplyVehicleStats(stats);
        player.ResetForRun();
        gameplayBuildPresenter.ApplyBuild(
            buildState.SelectedVehicle,
            buildState.GetEquippedAttachments());
        gameplayBuildPresenter.SetVisible(true);

        vehicleRigidbody.isKinematic = false;
        driveRigidbody.isKinematic = false;
        vehicleController.enabled = true;
        inputController.enabled = true;
        gameplayCamera.enabled = true;
        SetGameplayAudioEnabled(true);

        spawnManager.SetSpawningEnabled(true);
        missionActive = true;
        missionTimeRemaining = MissionDurationSeconds;
        garageUi.UpdateMissionTimer(missionTimeRemaining);
        garageUi.HideGarageForMission();
    }

    private void OpenGarage()
    {
        missionActive = false;
        missionTimeRemaining = 0f;

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
        gameplayCamera.enabled = false;
        SetGameplayAudioEnabled(false);
        gameplayBuildPresenter.SetVisible(false);

        ResetVehiclePose();
        garageUi.ShowGarage();
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
            OpenGarage();
        }
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
}
