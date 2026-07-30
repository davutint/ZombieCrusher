using System;
using System.Collections.Generic;
using ArcadeVP;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GarageGameplayBuildPresenter : MonoBehaviour
{
    private sealed class VehicleVisualInstance
    {
        public GameObject container;
        public Transform bodyPivot;
        public Transform[] frontWheels;
        public Transform[] rearWheels;
        public readonly Dictionary<string, List<GameObject>> attachments =
            new(StringComparer.Ordinal);

        public bool HasCompleteWheelRig =>
            frontWheels != null
            && frontWheels.Length == 2
            && frontWheels[0] != null
            && frontWheels[1] != null
            && rearWheels != null
            && rearWheels.Length == 2
            && rearWheels[0] != null
            && rearWheels[1] != null;
    }

    [SerializeField] private Transform gameplayVehicleRoot;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private ArcadeVehicleController vehicleController;
    [SerializeField] private BoxCollider vehicleHitbox;

    private readonly Dictionary<string, VehicleVisualInstance> vehicleCache =
        new(StringComparer.Ordinal);
    private readonly List<Renderer> originalRenderers = new();

    private Transform originalBodyMesh;
    private Transform[] originalFrontWheels;
    private Transform[] originalRearWheels;
    private Vector3 originalHitboxCenter;
    private Vector3 originalHitboxSize;
    private VehicleVisualInstance activeInstance;

    private void Awake()
    {
        if (gameplayVehicleRoot == null)
        {
            gameplayVehicleRoot = transform;
        }

        if (vehicleController == null)
        {
            vehicleController =
                gameplayVehicleRoot.GetComponent<ArcadeVehicleController>();
        }

        if (vehicleHitbox == null)
        {
            vehicleHitbox = gameplayVehicleRoot.GetComponent<BoxCollider>();
        }

        CacheOriginalRig();

        if (visualRoot == null)
        {
            GameObject rootObject = new GameObject("Garage Build Visual");
            visualRoot = rootObject.transform;
            visualRoot.SetParent(gameplayVehicleRoot, false);
        }

        Renderer[] renderers =
            gameplayVehicleRoot.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer candidate = renderers[i];
            if (candidate != null
                && (candidate is MeshRenderer
                    || candidate is SkinnedMeshRenderer)
                && !candidate.transform.IsChildOf(visualRoot))
            {
                originalRenderers.Add(candidate);
            }
        }

        SetVisible(false);
    }

    public void ApplyBuild(
        GarageVehicleDefinition vehicle,
        IEnumerable<GarageAttachmentDefinition> equippedAttachments)
    {
        if (vehicle == null || vehicle.VisualPrefab == null)
        {
            SetOriginalRenderersEnabled(true);
            HideCachedVehicles();
            activeInstance = null;
            RestoreOriginalRig();
            return;
        }

        VehicleVisualInstance instance = GetOrCreateVehicle(vehicle);
        HideCachedVehicles();
        instance.container.SetActive(true);
        activeInstance = instance;

        HashSet<string> desiredAttachments = new(StringComparer.Ordinal);
        if (equippedAttachments != null)
        {
            foreach (GarageAttachmentDefinition attachment in equippedAttachments)
            {
                ShowAttachment(
                    instance,
                    vehicle,
                    attachment,
                    desiredAttachments);
            }
        }

        foreach (KeyValuePair<string, List<GameObject>> pair in instance.attachments)
        {
            List<GameObject> attachmentObjects = pair.Value;
            if (attachmentObjects == null)
            {
                continue;
            }

            bool active = desiredAttachments.Contains(pair.Key);
            for (int i = 0; i < attachmentObjects.Count; i++)
            {
                if (attachmentObjects[i] != null)
                {
                    attachmentObjects[i].SetActive(active);
                }
            }
        }

        SetOriginalRenderersEnabled(false);
        SetBuildRenderersEnabled(instance.container, true);
        ConfigureVehicleRig(instance, vehicle);
        visualRoot.gameObject.SetActive(true);
    }

    public void SetVisible(bool visible)
    {
        if (visualRoot != null)
        {
            visualRoot.gameObject.SetActive(visible);
        }
    }

    private VehicleVisualInstance GetOrCreateVehicle(
        GarageVehicleDefinition vehicle)
    {
        if (vehicleCache.TryGetValue(
                vehicle.VehicleId,
                out VehicleVisualInstance cached)
            && cached.container != null)
        {
            return cached;
        }

        string containerName = $"Gameplay_{vehicle.VehicleId}";
        RemoveStaleContainer(containerName);

        GameObject container = new GameObject(containerName);
        container.transform.SetParent(visualRoot, false);
        container.transform.localPosition = vehicle.GameplayLocalPosition;
        container.transform.localRotation = vehicle.GameplayLocalRotation;
        container.transform.localScale = Vector3.one * vehicle.GameplayScale;

        GameObject bodyPivotObject = new GameObject("Body");
        Transform bodyPivot = bodyPivotObject.transform;
        bodyPivot.SetParent(container.transform, false);

        GameObject visual = Instantiate(
            vehicle.VisualPrefab,
            bodyPivot,
            false);
        visual.name = vehicle.VisualPrefab.name;
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        VehicleVisualInstance created = new VehicleVisualInstance
        {
            container = container,
            bodyPivot = bodyPivot
        };
        TryCreateWheelRig(created, visual.transform);
        vehicleCache[vehicle.VehicleId] = created;
        return created;
    }

    private void RemoveStaleContainer(string containerName)
    {
        for (int i = visualRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = visualRoot.GetChild(i);
            if (!string.Equals(child.name, containerName, StringComparison.Ordinal))
            {
                continue;
            }

            child.gameObject.SetActive(false);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private static void ShowAttachment(
        VehicleVisualInstance instance,
        GarageVehicleDefinition vehicle,
        GarageAttachmentDefinition attachment,
        ISet<string> desiredAttachments)
    {
        if (attachment == null || attachment.VisualPrefab == null)
        {
            return;
        }

        int poseCount = attachment.GetPoseCount(vehicle.VehicleId);
        if (poseCount == 0)
        {
            return;
        }

        desiredAttachments.Add(attachment.AttachmentId);
        if (!instance.attachments.TryGetValue(
                attachment.AttachmentId,
                out List<GameObject> attachmentObjects)
            || attachmentObjects == null)
        {
            attachmentObjects = new List<GameObject>(poseCount);
            instance.attachments[attachment.AttachmentId] = attachmentObjects;
        }

        while (attachmentObjects.Count < poseCount)
        {
            GameObject attachmentObject = Instantiate(
                attachment.VisualPrefab,
                instance.bodyPivot,
                false);
            attachmentObject.name = attachment.VisualPrefab.name;
            attachmentObjects.Add(attachmentObject);
        }

        for (int i = 0; i < attachmentObjects.Count; i++)
        {
            GameObject attachmentObject = attachmentObjects[i];
            GarageAttachmentPose pose = default;
            bool active = i < poseCount
                          && attachment.TryGetPose(
                              vehicle.VehicleId,
                              i,
                              out pose);
            attachmentObject.SetActive(active);
            if (!active)
            {
                continue;
            }

            Transform attachmentTransform = attachmentObject.transform;
            attachmentTransform.localPosition = pose.LocalPosition;
            attachmentTransform.localRotation = pose.LocalRotation;
            attachmentTransform.localScale = pose.LocalScale;
        }
    }

    private void HideCachedVehicles()
    {
        foreach (VehicleVisualInstance instance in vehicleCache.Values)
        {
            if (instance?.container != null)
            {
                instance.container.SetActive(false);
            }
        }
    }

    private void SetOriginalRenderersEnabled(bool enabled)
    {
        for (int i = 0; i < originalRenderers.Count; i++)
        {
            if (originalRenderers[i] != null)
            {
                originalRenderers[i].enabled = enabled;
            }
        }
    }

    private void CacheOriginalRig()
    {
        if (vehicleController != null)
        {
            originalBodyMesh = vehicleController.BodyMesh;
            originalFrontWheels =
                CloneWheelArray(vehicleController.FrontWheels);
            originalRearWheels =
                CloneWheelArray(vehicleController.RearWheels);
        }

        if (vehicleHitbox != null)
        {
            originalHitboxCenter = vehicleHitbox.center;
            originalHitboxSize = vehicleHitbox.size;
        }
    }

    private void RestoreOriginalRig()
    {
        if (vehicleController != null)
        {
            vehicleController.BodyMesh = originalBodyMesh;
            vehicleController.FrontWheels =
                CloneWheelArray(originalFrontWheels);
            vehicleController.RearWheels =
                CloneWheelArray(originalRearWheels);
        }

        if (vehicleHitbox != null)
        {
            vehicleHitbox.center = originalHitboxCenter;
            vehicleHitbox.size = originalHitboxSize;
        }
    }

    private void ConfigureVehicleRig(
        VehicleVisualInstance instance,
        GarageVehicleDefinition vehicle)
    {
        if (vehicleController != null && instance.HasCompleteWheelRig)
        {
            instance.bodyPivot.localRotation = Quaternion.identity;
            ResetWheelRig(instance.frontWheels);
            ResetWheelRig(instance.rearWheels);

            vehicleController.BodyMesh = instance.bodyPivot;
            vehicleController.FrontWheels =
                CloneWheelArray(instance.frontWheels);
            vehicleController.RearWheels =
                CloneWheelArray(instance.rearWheels);
        }

        if (vehicleHitbox != null)
        {
            vehicleHitbox.center = vehicle.GameplayColliderCenter;
            vehicleHitbox.size = vehicle.GameplayColliderSize;
        }
    }

    private static void TryCreateWheelRig(
        VehicleVisualInstance instance,
        Transform visualRootTransform)
    {
        Transform frontLeft =
            FindWheelVisual(visualRootTransform, "wheel_fl");
        Transform frontRight =
            FindWheelVisual(visualRootTransform, "wheel_fr");
        Transform rearLeft =
            FindWheelVisual(visualRootTransform, "wheel_rl");
        Transform rearRight =
            FindWheelVisual(visualRootTransform, "wheel_rr");

        if (frontLeft == null
            || frontRight == null
            || rearLeft == null
            || rearRight == null)
        {
            return;
        }

        instance.frontWheels = new[]
        {
            CreateWheelProxy(instance.container.transform, frontLeft, "WheelFL"),
            CreateWheelProxy(instance.container.transform, frontRight, "WheelFR")
        };
        instance.rearWheels = new[]
        {
            CreateWheelProxy(instance.container.transform, rearLeft, "WheelRL"),
            CreateWheelProxy(instance.container.transform, rearRight, "WheelRR")
        };
    }

    private static Transform FindWheelVisual(
        Transform root,
        string wheelToken)
    {
        Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < descendants.Length; i++)
        {
            Transform candidate = descendants[i];
            if (candidate == root
                || candidate.GetComponent<Renderer>() == null)
            {
                continue;
            }

            string normalizedName =
                candidate.name.Replace("-", "_").ToLowerInvariant();
            if (normalizedName.Contains(wheelToken))
            {
                return candidate;
            }
        }

        return null;
    }

    private static Transform CreateWheelProxy(
        Transform container,
        Transform wheelVisual,
        string proxyName)
    {
        Vector3 localPosition =
            container.InverseTransformPoint(wheelVisual.position);
        Quaternion localRotation =
            Quaternion.Inverse(container.rotation) * wheelVisual.rotation;

        GameObject proxyObject = new GameObject(proxyName);
        Transform proxy = proxyObject.transform;
        proxy.SetParent(container, false);
        proxy.localPosition = localPosition;
        proxy.localRotation = Quaternion.identity;

        GameObject axleObject = new GameObject($"{proxyName} Axel");
        Transform axle = axleObject.transform;
        axle.SetParent(proxy, false);

        wheelVisual.SetParent(axle, true);
        wheelVisual.localPosition = Vector3.zero;
        wheelVisual.localRotation = localRotation;
        return proxy;
    }

    private static Transform[] CloneWheelArray(Transform[] source)
    {
        if (source == null)
        {
            return Array.Empty<Transform>();
        }

        Transform[] copy = new Transform[source.Length];
        Array.Copy(source, copy, source.Length);
        return copy;
    }

    private static void ResetWheelRig(Transform[] wheels)
    {
        if (wheels == null)
        {
            return;
        }

        for (int i = 0; i < wheels.Length; i++)
        {
            Transform wheel = wheels[i];
            if (wheel == null)
            {
                continue;
            }

            wheel.localRotation = Quaternion.identity;
            if (wheel.childCount > 0)
            {
                wheel.GetChild(0).localRotation = Quaternion.identity;
            }
        }
    }

    private static void SetBuildRenderersEnabled(
        GameObject buildRoot,
        bool enabled)
    {
        Renderer[] renderers =
            buildRoot.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = enabled;
        }
    }
}
