using System;
using UnityEngine;

public enum GarageVehicleStat
{
    Speed,
    Acceleration,
    Handling,
    Durability,
    ImpactPower
}

[Serializable]
public struct VehicleStats
{
    [Min(0f)] public float maxSpeed;
    [Min(0f)] public float acceleration;
    [Min(0f)] public float handling;
    [Min(1f)] public float durability;
    [Min(0.1f)] public float impactPower;

    public VehicleStats(
        float maxSpeed,
        float acceleration,
        float handling,
        float durability,
        float impactPower)
    {
        this.maxSpeed = maxSpeed;
        this.acceleration = acceleration;
        this.handling = handling;
        this.durability = durability;
        this.impactPower = impactPower;
    }

    public VehicleStats Apply(VehicleStatModifier modifier)
    {
        return new VehicleStats(
            Mathf.Max(1f, maxSpeed + modifier.maxSpeed),
            Mathf.Max(0.1f, acceleration + modifier.acceleration),
            Mathf.Max(0.1f, handling + modifier.handling),
            Mathf.Max(1f, durability + modifier.durability),
            Mathf.Max(0.1f, impactPower + modifier.impactPower));
    }

    public float GetValue(GarageVehicleStat stat)
    {
        return stat switch
        {
            GarageVehicleStat.Speed => maxSpeed,
            GarageVehicleStat.Acceleration => acceleration,
            GarageVehicleStat.Handling => handling,
            GarageVehicleStat.Durability => durability,
            GarageVehicleStat.ImpactPower => impactPower,
            _ => 0f
        };
    }

    public bool IsFinite()
    {
        return IsFinite(maxSpeed)
               && IsFinite(acceleration)
               && IsFinite(handling)
               && IsFinite(durability)
               && IsFinite(impactPower);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

[Serializable]
public struct VehicleStatModifier
{
    public float maxSpeed;
    public float acceleration;
    public float handling;
    public float durability;
    public float impactPower;

    public VehicleStatModifier(
        float maxSpeed,
        float acceleration,
        float handling,
        float durability,
        float impactPower)
    {
        this.maxSpeed = maxSpeed;
        this.acceleration = acceleration;
        this.handling = handling;
        this.durability = durability;
        this.impactPower = impactPower;
    }
}

public static class GarageVehicleStatPresentation
{
    public static readonly GarageVehicleStat[] OrderedStats =
    {
        GarageVehicleStat.Speed,
        GarageVehicleStat.Acceleration,
        GarageVehicleStat.Handling,
        GarageVehicleStat.Durability,
        GarageVehicleStat.ImpactPower
    };

    public static string GetEnglishLabel(GarageVehicleStat stat)
    {
        return stat switch
        {
            GarageVehicleStat.Speed => "SPEED",
            GarageVehicleStat.Acceleration => "ACCELERATION",
            GarageVehicleStat.Handling => "HANDLING",
            GarageVehicleStat.Durability => "DURABILITY",
            GarageVehicleStat.ImpactPower => "IMPACT POWER",
            _ => stat.ToString().ToUpperInvariant()
        };
    }

    public static string FormatValue(GarageVehicleStat stat, float value)
    {
        return stat switch
        {
            GarageVehicleStat.Speed => $"{value:0} km/h",
            GarageVehicleStat.Acceleration => value.ToString("0.0"),
            GarageVehicleStat.Handling => value.ToString("0.0"),
            GarageVehicleStat.Durability => $"{value:0} HP",
            GarageVehicleStat.ImpactPower => $"{value:0.00}x",
            _ => value.ToString("0.0")
        };
    }

    public static string FormatDelta(GarageVehicleStat stat, float delta)
    {
        string sign = delta > 0.0001f ? "+" : string.Empty;
        return stat switch
        {
            GarageVehicleStat.Speed => $"{sign}{delta:0} km/h",
            GarageVehicleStat.Durability => $"{sign}{delta:0} HP",
            GarageVehicleStat.ImpactPower => $"{sign}{delta:0.00}x",
            _ => $"{sign}{delta:0.0}"
        };
    }

    public static float GetDisplayMaximum(GarageVehicleStat stat)
    {
        return stat switch
        {
            GarageVehicleStat.Speed => 160f,
            GarageVehicleStat.Acceleration => 40f,
            GarageVehicleStat.Handling => 20f,
            GarageVehicleStat.Durability => 240f,
            GarageVehicleStat.ImpactPower => 2f,
            _ => 100f
        };
    }
}
