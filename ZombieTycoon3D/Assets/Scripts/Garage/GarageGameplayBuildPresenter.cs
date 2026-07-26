using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GarageGameplayBuildPresenter : MonoBehaviour
{
    private sealed class VehicleVisualInstance
    {
        public GameObject container;
        public readonly Dictionary<string, List<GameObject>> attachments =
            new(StringComparer.Ordinal);
    }

    [SerializeField] private Transform gameplayVehicleRoot;
    [SerializeField] private Transform visualRoot;

    private readonly Dictionary<string, VehicleVisualInstance> vehicleCache =
        new(StringComparer.Ordinal);
    private readonly List<Renderer> originalRenderers = new();

    private VehicleVisualInstance activeInstance;

    private void Awake()
    {
        if (gameplayVehicleRoot == null)
        {
            gameplayVehicleRoot = transform;
        }

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

        GameObject visual = Instantiate(
            vehicle.VisualPrefab,
            container.transform,
            false);
        visual.name = vehicle.VisualPrefab.name;
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        VehicleVisualInstance created = new VehicleVisualInstance
        {
            container = container
        };
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
                instance.container.transform,
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
