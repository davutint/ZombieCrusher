using UnityEngine;
using ArcadeVP;

/// <summary>
/// CarDefinition’daki taban değerleri başlangıçta ArcadeVehicleController
/// ve Player bileşenlerine yazar.
/// </summary>
[DisallowMultipleComponent]
public class CarStatsInitializer : MonoBehaviour
{
    [SerializeField] private CarDefinition definition;
    [SerializeField] private ArcadeVehicleController vehicleController;
    [SerializeField] private Player player;

    private void Reset()               // Inspector’da “Add Component” anında doldurur
    {
        vehicleController = GetComponent<ArcadeVehicleController>();
        player            = GetComponent<Player>();
    }

    private void Awake()
    {
        if (definition == null)
        {
            Debug.LogError("CarStatsInitializer: CarDefinition atanmadı!");
            return;
        }

        if (vehicleController == null)
            vehicleController = GetComponent<ArcadeVehicleController>();
        if (player == null)
            player = GetComponent<Player>();

        // ---------- Base stat’leri uygula ----------
        vehicleController.MaxSpeed    = definition.baseMaxSpeed;
        vehicleController.accelaration = definition.baseAcceleration;
        if (player != null)
            player.SetMaxHealth(definition.baseHealth);
    }
}