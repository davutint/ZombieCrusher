using UnityEngine;

[CreateAssetMenu(fileName = "GarageVehicle", menuName = "Zombie Tycoon/Garage/Vehicle")]
public sealed class GarageVehicleDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string vehicleId;
    [SerializeField] private string displayName;
    [TextArea(2, 4)]
    [SerializeField] private string description;

    [Header("Visual")]
    [SerializeField] private GameObject visualPrefab;
    [SerializeField] private Vector3 previewEuler = new Vector3(0f, 28f, 0f);
    [SerializeField, Min(0.01f)] private float previewScale = 1f;
    [SerializeField] private Vector3 gameplayLocalPosition;
    [SerializeField] private Vector3 gameplayLocalEuler;
    [SerializeField, Min(0.01f)] private float gameplayScale = 1f;
    [SerializeField] private Vector3 gameplayColliderCenter;
    [SerializeField] private Vector3 gameplayColliderSize = Vector3.one;

    [Header("Gameplay")]
    [SerializeField, Min(0)] private int price;
    [SerializeField] private VehicleStats baseStats =
        new VehicleStats(100f, 20f, 10f, 100f, 1f);

    [Header("Vehicle Physics Identity")]
    [SerializeField, Min(0.1f)] private float bodyMass = 1f;
    [SerializeField, Min(0.1f)] private float driveMass = 1f;
    [SerializeField, Min(0f)] private float gravity = 7f;
    [SerializeField, Min(0f)] private float downforce = 5f;
    [SerializeField, Min(0.1f)] private float lateralGrip = 1f;
    [SerializeField] private Vector3 centerOfMass;
    [SerializeField] private Vector3 driveCenterOfMass;

    public string VehicleId => vehicleId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public GameObject VisualPrefab => visualPrefab;
    public Vector3 PreviewEuler => previewEuler;
    public float PreviewScale => Mathf.Max(0.01f, previewScale);
    public Vector3 GameplayLocalPosition => gameplayLocalPosition;
    public Quaternion GameplayLocalRotation => Quaternion.Euler(gameplayLocalEuler);
    public float GameplayScale => Mathf.Max(0.01f, gameplayScale);
    public Vector3 GameplayColliderCenter => gameplayColliderCenter;
    public Vector3 GameplayColliderSize => new Vector3(
        Mathf.Max(0.01f, gameplayColliderSize.x),
        Mathf.Max(0.01f, gameplayColliderSize.y),
        Mathf.Max(0.01f, gameplayColliderSize.z));
    public int Price => Mathf.Max(0, price);
    public VehicleStats BaseStats => baseStats;
    public float BodyMass => Mathf.Max(0.1f, bodyMass);
    public float DriveMass => Mathf.Max(0.1f, driveMass);
    public float Gravity => Mathf.Max(0f, gravity);
    public float Downforce => Mathf.Max(0f, downforce);
    public float LateralGrip => Mathf.Max(0.1f, lateralGrip);
    public Vector3 CenterOfMass => centerOfMass;
    public Vector3 DriveCenterOfMass => driveCenterOfMass;

    private void OnValidate()
    {
        vehicleId = vehicleId?.Trim();
        displayName = displayName?.Trim();
        previewScale = Mathf.Max(0.01f, previewScale);
        gameplayScale = Mathf.Max(0.01f, gameplayScale);
        gameplayColliderSize = new Vector3(
            Mathf.Max(0.01f, gameplayColliderSize.x),
            Mathf.Max(0.01f, gameplayColliderSize.y),
            Mathf.Max(0.01f, gameplayColliderSize.z));
        bodyMass = Mathf.Max(0.1f, bodyMass);
        driveMass = Mathf.Max(0.1f, driveMass);
        gravity = Mathf.Max(0f, gravity);
        downforce = Mathf.Max(0f, downforce);
        lateralGrip = Mathf.Max(0.1f, lateralGrip);
        price = Mathf.Max(0, price);
    }
}
