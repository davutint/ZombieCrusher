using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GaragePreviewController : MonoBehaviour
{
    private sealed class PreviewVehicleInstance
    {
        public GameObject container;
        public readonly Dictionary<string, List<GameObject>> attachments =
            new(StringComparer.Ordinal);
    }

    [Header("Stage")]
    [SerializeField] private Camera previewCamera;
    [SerializeField] private Transform stageRoot;
    [SerializeField] private Light[] stageLights;

    [Header("Presentation")]
    [SerializeField, Min(0f)] private float autoRotationSpeed = 6f;
    [SerializeField, Min(0.01f)] private float dragRotationSensitivity = 0.22f;
    [SerializeField, Range(20f, 60f)] private float fieldOfView = 34f;
    [SerializeField, Min(1f)] private float framingPadding = 1.3f;

    private readonly Dictionary<string, PreviewVehicleInstance> vehicleCache =
        new(StringComparer.Ordinal);

    private PreviewVehicleInstance activeInstance;
    private bool inputDragging;
    private bool visible;

    public bool IsVisible => visible;

    private void Awake()
    {
        if (previewCamera == null || stageRoot == null)
        {
            Debug.LogError(
                "GaragePreviewController: Preview Camera and Stage Root are required.",
                this);
            enabled = false;
            return;
        }

        previewCamera.fieldOfView = fieldOfView;
        SetVisible(false);
    }

    private void Update()
    {
        if (!visible
            || inputDragging
            || activeInstance?.container == null
            || autoRotationSpeed <= 0f)
        {
            return;
        }

        activeInstance.container.transform.Rotate(
            Vector3.up,
            autoRotationSpeed * Time.unscaledDeltaTime,
            Space.World);
    }

    public void SetVisible(bool shouldBeVisible)
    {
        visible = shouldBeVisible;

        if (previewCamera != null)
        {
            previewCamera.enabled = shouldBeVisible;
        }

        if (stageRoot != null)
        {
            stageRoot.gameObject.SetActive(shouldBeVisible);
        }

        if (stageLights != null)
        {
            for (int i = 0; i < stageLights.Length; i++)
            {
                if (stageLights[i] != null)
                {
                    stageLights[i].enabled = shouldBeVisible;
                }
            }
        }
    }

    public void SetBuild(
        GarageVehicleDefinition vehicle,
        IEnumerable<GarageAttachmentDefinition> equippedAttachments,
        GarageAttachmentDefinition previewAttachment,
        bool showEquippedAttachments)
    {
        if (vehicle == null || vehicle.VisualPrefab == null || stageRoot == null)
        {
            HideCachedVehicles();
            activeInstance = null;
            return;
        }

        PreviewVehicleInstance instance = GetOrCreateVehicle(vehicle);
        HideCachedVehicles();
        instance.container.SetActive(true);
        activeInstance = instance;

        HashSet<string> desiredAttachmentIds = new(StringComparer.Ordinal);
        GarageAttachmentSlot? replacedSlot =
            previewAttachment != null ? previewAttachment.Slot : null;

        if (showEquippedAttachments && equippedAttachments != null)
        {
            foreach (GarageAttachmentDefinition attachment in equippedAttachments)
            {
                if (attachment == null
                    || (replacedSlot.HasValue && attachment.Slot == replacedSlot.Value))
                {
                    continue;
                }

                ShowAttachment(instance, vehicle, attachment, desiredAttachmentIds);
            }
        }

        ShowAttachment(instance, vehicle, previewAttachment, desiredAttachmentIds);

        foreach (KeyValuePair<string, List<GameObject>> pair in instance.attachments)
        {
            List<GameObject> attachmentObjects = pair.Value;
            if (attachmentObjects == null)
            {
                continue;
            }

            bool active = desiredAttachmentIds.Contains(pair.Key);
            for (int i = 0; i < attachmentObjects.Count; i++)
            {
                if (attachmentObjects[i] != null)
                {
                    attachmentObjects[i].SetActive(active);
                }
            }
        }

        NormalizeToStage(instance.container.transform);
        FrameActiveBuild();
    }

    public void BeginDrag()
    {
        inputDragging = true;
    }

    public void RotateByPointerDelta(float horizontalDelta)
    {
        if (activeInstance?.container == null)
        {
            return;
        }

        activeInstance.container.transform.Rotate(
            Vector3.up,
            -horizontalDelta * dragRotationSensitivity,
            Space.World);
    }

    public void EndDrag()
    {
        inputDragging = false;
    }

    private PreviewVehicleInstance GetOrCreateVehicle(
        GarageVehicleDefinition vehicle)
    {
        if (vehicleCache.TryGetValue(vehicle.VehicleId, out PreviewVehicleInstance cached)
            && cached.container != null)
        {
            return cached;
        }

        string containerName = $"Preview_{vehicle.VehicleId}";
        RemoveStaleContainer(containerName);

        GameObject container = new GameObject(containerName);
        container.transform.SetParent(stageRoot, false);
        container.transform.localRotation = Quaternion.Euler(vehicle.PreviewEuler);
        container.transform.localScale = Vector3.one * vehicle.PreviewScale;

        GameObject visual = Instantiate(vehicle.VisualPrefab, container.transform, false);
        visual.name = vehicle.VisualPrefab.name;
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        PreviewVehicleInstance created = new PreviewVehicleInstance
        {
            container = container
        };
        vehicleCache[vehicle.VehicleId] = created;
        return created;
    }

    private void RemoveStaleContainer(string containerName)
    {
        for (int i = stageRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = stageRoot.GetChild(i);
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
        PreviewVehicleInstance instance,
        GarageVehicleDefinition vehicle,
        GarageAttachmentDefinition attachment,
        ISet<string> desiredAttachmentIds)
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

        desiredAttachmentIds.Add(attachment.AttachmentId);

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

    private void NormalizeToStage(Transform container)
    {
        container.position = stageRoot.position;

        if (!TryCalculateBounds(container.gameObject, out Bounds bounds))
        {
            return;
        }

        Vector3 desiredReference = stageRoot.position;
        Vector3 currentReference = new Vector3(
            bounds.center.x,
            bounds.min.y,
            bounds.center.z);
        container.position += desiredReference - currentReference;
    }

    private void FrameActiveBuild()
    {
        if (activeInstance?.container == null
            || previewCamera == null
            || !TryCalculateBounds(activeInstance.container, out Bounds bounds))
        {
            return;
        }

        previewCamera.fieldOfView = fieldOfView;

        Vector3 focus = new Vector3(
            bounds.center.x,
            Mathf.Lerp(bounds.min.y, bounds.max.y, 0.52f),
            bounds.center.z);

        float radius = Mathf.Max(
            bounds.extents.x,
            Mathf.Max(bounds.extents.y, bounds.extents.z));
        float halfFovRadians = fieldOfView * 0.5f * Mathf.Deg2Rad;
        float distance = Mathf.Max(
            2f,
            radius / Mathf.Tan(halfFovRadians) * framingPadding);

        Vector3 viewDirection = new Vector3(1.15f, 0.52f, -1.45f).normalized;
        previewCamera.transform.position = focus + viewDirection * distance;
        previewCamera.transform.rotation = Quaternion.LookRotation(
            focus - previewCamera.transform.position,
            Vector3.up);
        previewCamera.nearClipPlane = Mathf.Max(0.03f, distance - radius * 2.2f);
        previewCamera.farClipPlane = distance + radius * 3f + 10f;
    }

    private void HideCachedVehicles()
    {
        foreach (PreviewVehicleInstance instance in vehicleCache.Values)
        {
            if (instance?.container != null)
            {
                instance.container.SetActive(false);
            }
        }
    }

    private static bool TryCalculateBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(false);
        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return true;
    }
}
