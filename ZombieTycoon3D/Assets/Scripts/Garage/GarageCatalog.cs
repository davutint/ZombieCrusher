using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GarageCatalog", menuName = "Zombie Tycoon/Garage/Catalog")]
public sealed class GarageCatalog : ScriptableObject
{
    [SerializeField] private GarageVehicleDefinition startingVehicle;
    [SerializeField] private List<GarageVehicleDefinition> vehicles = new();
    [SerializeField] private List<GarageAttachmentDefinition> attachments = new();

    public GarageVehicleDefinition StartingVehicle => startingVehicle;
    public IReadOnlyList<GarageVehicleDefinition> Vehicles => vehicles;
    public IReadOnlyList<GarageAttachmentDefinition> Attachments => attachments;

    public GarageVehicleDefinition FindVehicle(string vehicleId)
    {
        for (int i = 0; i < vehicles.Count; i++)
        {
            GarageVehicleDefinition candidate = vehicles[i];
            if (candidate != null
                && string.Equals(candidate.VehicleId, vehicleId, StringComparison.Ordinal))
            {
                return candidate;
            }
        }

        return null;
    }

    public GarageAttachmentDefinition FindAttachment(string attachmentId)
    {
        for (int i = 0; i < attachments.Count; i++)
        {
            GarageAttachmentDefinition candidate = attachments[i];
            if (candidate != null
                && string.Equals(candidate.AttachmentId, attachmentId, StringComparison.Ordinal))
            {
                return candidate;
            }
        }

        return null;
    }
}
