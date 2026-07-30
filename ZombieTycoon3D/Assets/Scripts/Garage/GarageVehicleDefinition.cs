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
        price = Mathf.Max(0, price);
    }
}
