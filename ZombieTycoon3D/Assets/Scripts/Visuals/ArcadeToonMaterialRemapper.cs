using UnityEngine;

public static class ArcadeToonMaterialRemapper
{
    private const string ResourceRoot = "ArcadeVisualStyle/";

    private static bool loadAttempted;
    private static Material vehicleBody;
    private static Material vehicleGlass;
    private static Material vehicleFence;
    private static Material zombie;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache()
    {
        loadAttempted = false;
        vehicleBody = null;
        vehicleGlass = null;
        vehicleFence = null;
        zombie = null;
    }

    public static void ApplyTo(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        EnsureLoaded();

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Renderer renderer = renderers[rendererIndex];
            Material[] sharedMaterials = renderer.sharedMaterials;
            bool changed = false;

            for (int materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
            {
                Material replacement = Resolve(sharedMaterials[materialIndex]);
                if (replacement == null || replacement == sharedMaterials[materialIndex])
                {
                    continue;
                }

                sharedMaterials[materialIndex] = replacement;
                changed = true;
            }

            if (changed)
            {
                renderer.sharedMaterials = sharedMaterials;
            }
        }
    }

    private static void EnsureLoaded()
    {
        if (loadAttempted)
        {
            return;
        }

        loadAttempted = true;
        vehicleBody = Resources.Load<Material>(ResourceRoot + "VehicleBody");
        vehicleGlass = Resources.Load<Material>(ResourceRoot + "VehicleGlass");
        vehicleFence = Resources.Load<Material>(ResourceRoot + "VehicleFence");
        zombie = Resources.Load<Material>(ResourceRoot + "Zombie");
    }

    private static Material Resolve(Material source)
    {
        if (source == null)
        {
            return null;
        }

        switch (source.name)
        {
            case "PolygonApocalypse_Material_Vehicle_Standard_01":
                return vehicleBody;
            case "PolygonApocalypse_Veh_Glass":
                return vehicleGlass;
            case "PolygonApocalypse_Material_Fence_01":
                return vehicleFence;
            case "PolygonZombie_Texture_01_A":
                return zombie;
            default:
                return null;
        }
    }
}
