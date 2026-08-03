using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GaragePreviewController : MonoBehaviour
{
    private sealed class PreviewVehicleInstance
    {
        public string vehicleId;
        public GameObject container;
        public readonly Dictionary<GarageAttachmentAnchor, Transform> anchors =
            new();
        public readonly Dictionary<string, List<GameObject>> attachments =
            new(StringComparer.Ordinal);

        public Transform GetAnchor(GarageAttachmentAnchor anchor)
        {
            return anchors.TryGetValue(anchor, out Transform result)
                && result != null
                    ? result
                    : container.transform;
        }
    }

    [Header("Stage")]
    [SerializeField] private Camera previewCamera;
    [SerializeField] private Transform stageRoot;
    [SerializeField] private Light[] stageLights;

    [Header("Presentation")]
    [SerializeField, Min(0f)] private float autoRotationSpeed = 6f;
    [SerializeField, Min(0.01f)] private float dragRotationSensitivity = 0.22f;
    [SerializeField, Range(20f, 60f)] private float fieldOfView = 34f;
    [SerializeField, Range(31f, 45f)] private float partFocusFieldOfView = 31f;
    [SerializeField, Range(1f, 1.2f)] private float framingPadding = 1.06f;
    [SerializeField, Range(2.05f, 2.8f)] private float partFocusDistanceScale = 2.05f;
    [SerializeField, Min(0f)] private float cameraTransitionSpeed = 10f;
    [SerializeField] private Color garageBackgroundColor =
        new Color32(108, 104, 97, 255);

    private readonly Dictionary<string, PreviewVehicleInstance> vehicleCache =
        new(StringComparer.Ordinal);

    private PreviewVehicleInstance activeInstance;
    private GarageAttachmentDefinition focusedAttachment;
    private Vector3 targetCameraPosition;
    private Quaternion targetCameraRotation;
    private float targetCameraFieldOfView;
    private bool autoRotationEnabled = true;
    private bool cameraPoseInitialized;
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

        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = garageBackgroundColor;
        previewCamera.fieldOfView = fieldOfView;
        SetVisible(false);
    }

    private void Update()
    {
        if (!visible)
        {
            return;
        }

        UpdateCameraTransition();

        if (!autoRotationEnabled
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
            if (shouldBeVisible)
            {
                previewCamera.clearFlags = CameraClearFlags.SolidColor;
                previewCamera.backgroundColor = garageBackgroundColor;
            }

            previewCamera.enabled = shouldBeVisible;
        }

        if (!shouldBeVisible)
        {
            cameraPoseInitialized = false;
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
        bool showEquippedAttachments,
        GarageAttachmentDefinition focusAttachment,
        bool allowAutoRotation)
    {
        focusedAttachment = focusAttachment;
        autoRotationEnabled = allowAutoRotation;

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
        if (!FrameFocusedAttachment())
        {
            FrameActiveBuild();
        }
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

        if (focusedAttachment != null)
        {
            FrameFocusedAttachment();
        }
    }

    public void EndDrag()
    {
        inputDragging = false;
        if (focusedAttachment != null)
        {
            FrameFocusedAttachment();
        }
    }

    public bool TryGetAttachmentViewportPosition(
        GarageAttachmentDefinition attachment,
        out Vector2 viewportPosition)
    {
        viewportPosition = default;
        if (!visible
            || attachment == null
            || previewCamera == null
            || activeInstance?.container == null)
        {
            return false;
        }

        if (!TryGetAttachmentWorldPosition(attachment, out Vector3 worldPosition))
        {
            return false;
        }

        Vector3 viewportPoint = previewCamera.WorldToViewportPoint(worldPosition);
        if (viewportPoint.z <= 0f)
        {
            return false;
        }

        viewportPosition = new Vector2(viewportPoint.x, viewportPoint.y);
        return true;
    }

    private bool TryGetAttachmentWorldPosition(
        GarageAttachmentDefinition attachment,
        out Vector3 worldPosition)
    {
        worldPosition = default;
        if (attachment == null
            || previewCamera == null
            || activeInstance?.container == null)
        {
            return false;
        }

        string vehicleId = activeInstance.vehicleId;
        int poseCount = attachment.GetPoseCount(vehicleId);
        bool found = false;
        float closestDepth = float.MaxValue;
        for (int i = 0; i < poseCount; i++)
        {
            if (!attachment.TryGetPose(vehicleId, i, out GarageAttachmentPose pose))
            {
                continue;
            }

            Transform anchor = activeInstance.GetAnchor(pose.Anchor);
            Vector3 candidateWorldPosition = anchor.TransformPoint(pose.LocalPosition);
            Vector3 candidate = previewCamera.WorldToViewportPoint(candidateWorldPosition);
            if (candidate.z <= 0f || candidate.z >= closestDepth)
            {
                continue;
            }

            found = true;
            closestDepth = candidate.z;
            worldPosition = candidateWorldPosition;
        }

        return found;
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
        ArcadeToonMaterialRemapper.ApplyTo(visual);

        PreviewVehicleInstance created = new PreviewVehicleInstance
        {
            vehicleId = vehicle.VehicleId,
            container = container
        };
        created.anchors[GarageAttachmentAnchor.Body] = container.transform;
        CreateWheelAnchors(created, visual.transform);
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
            ArcadeToonMaterialRemapper.ApplyTo(attachmentObject);
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
            attachmentTransform.SetParent(instance.GetAnchor(pose.Anchor), false);
            attachmentTransform.localPosition = pose.LocalPosition;
            attachmentTransform.localRotation = pose.LocalRotation;
            attachmentTransform.localScale = pose.LocalScale;
        }
    }

    private static void CreateWheelAnchors(
        PreviewVehicleInstance instance,
        Transform visualRoot)
    {
        CreateWheelAnchor(
            instance,
            visualRoot,
            "wheel_fl",
            GarageAttachmentAnchor.FrontLeftWheel,
            "AttachmentAnchor_FL");
        CreateWheelAnchor(
            instance,
            visualRoot,
            "wheel_fr",
            GarageAttachmentAnchor.FrontRightWheel,
            "AttachmentAnchor_FR");
        CreateWheelAnchor(
            instance,
            visualRoot,
            "wheel_rl",
            GarageAttachmentAnchor.RearLeftWheel,
            "AttachmentAnchor_RL");
        CreateWheelAnchor(
            instance,
            visualRoot,
            "wheel_rr",
            GarageAttachmentAnchor.RearRightWheel,
            "AttachmentAnchor_RR");
    }

    private static void CreateWheelAnchor(
        PreviewVehicleInstance instance,
        Transform visualRoot,
        string wheelToken,
        GarageAttachmentAnchor anchorType,
        string anchorName)
    {
        Renderer wheelRenderer = FindWheelRenderer(visualRoot, wheelToken);
        if (wheelRenderer == null)
        {
            return;
        }

        GameObject anchorObject = new GameObject(anchorName);
        Transform anchor = anchorObject.transform;
        anchor.SetParent(instance.container.transform, false);
        anchor.position = wheelRenderer.bounds.center;
        anchor.localRotation = Quaternion.identity;
        anchor.localScale = Vector3.one;
        instance.anchors[anchorType] = anchor;
    }

    private static Renderer FindWheelRenderer(
        Transform root,
        string wheelToken)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer candidate = renderers[i];
            string normalizedName =
                candidate.name.Replace("-", "_").ToLowerInvariant();
            if (normalizedName.Contains(wheelToken))
            {
                return candidate;
            }
        }

        return null;
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

        Vector3 focus = new Vector3(
            bounds.center.x,
            Mathf.Lerp(bounds.min.y, bounds.max.y, 0.52f),
            bounds.center.z);

        float halfFovRadians = fieldOfView * 0.5f * Mathf.Deg2Rad;
        Vector3 viewDirection = new Vector3(1.15f, 0.52f, -1.45f).normalized;
        Quaternion viewRotation = Quaternion.LookRotation(
            -viewDirection,
            Vector3.up);
        Vector3 viewForward = viewRotation * Vector3.forward;
        Vector3 viewRight = viewRotation * Vector3.right;
        Vector3 viewUp = viewRotation * Vector3.up;
        float verticalTan = Mathf.Tan(halfFovRadians);
        float horizontalTan = verticalTan * Mathf.Max(0.1f, previewCamera.aspect);
        float edgePadding = Mathf.Clamp(framingPadding, 1.02f, 1.08f);
        float distance = 2f;

        Vector3 boundsMin = bounds.min;
        Vector3 boundsMax = bounds.max;
        for (int cornerIndex = 0; cornerIndex < 8; cornerIndex++)
        {
            Vector3 corner = new Vector3(
                (cornerIndex & 1) == 0 ? boundsMin.x : boundsMax.x,
                (cornerIndex & 2) == 0 ? boundsMin.y : boundsMax.y,
                (cornerIndex & 4) == 0 ? boundsMin.z : boundsMax.z);
            Vector3 relative = corner - focus;
            float depthOffset = Vector3.Dot(relative, viewForward);
            float verticalDistance =
                Mathf.Abs(Vector3.Dot(relative, viewUp))
                * edgePadding
                / verticalTan
                - depthOffset;
            float horizontalDistance =
                Mathf.Abs(Vector3.Dot(relative, viewRight))
                * edgePadding
                / horizontalTan
                - depthOffset;
            distance = Mathf.Max(
                distance,
                Mathf.Max(verticalDistance, horizontalDistance));
        }

        Vector3 cameraPosition = focus + viewDirection * distance;
        Quaternion cameraRotation = Quaternion.LookRotation(
            focus - cameraPosition,
            Vector3.up);
        float boundsRadius = bounds.extents.magnitude;
        SetCameraTarget(
            cameraPosition,
            cameraRotation,
            fieldOfView,
            Mathf.Max(0.03f, distance - boundsRadius * 2.2f),
            distance + boundsRadius * 3f + 10f);
    }

    private bool FrameFocusedAttachment()
    {
        if (focusedAttachment == null
            || activeInstance?.container == null
            || previewCamera == null
            || !TryCalculateBounds(activeInstance.container, out Bounds bounds)
            || !TryGetAttachmentWorldPosition(
                focusedAttachment,
                out Vector3 focus))
        {
            return false;
        }

        Vector3 viewDirection = new Vector3(1.15f, 0.52f, -1.45f).normalized;
        focus = Vector3.Lerp(focus, bounds.center, 0.28f);
        float distance = Mathf.Max(
            2.2f,
            bounds.extents.magnitude
            * Mathf.Clamp(partFocusDistanceScale, 2.05f, 2.8f));
        Vector3 cameraPosition = focus + viewDirection * distance;
        Quaternion cameraRotation = Quaternion.LookRotation(
            focus - cameraPosition,
            Vector3.up);
        float boundsRadius = bounds.extents.magnitude;
        SetCameraTarget(
            cameraPosition,
            cameraRotation,
            Mathf.Clamp(partFocusFieldOfView, 31f, 45f),
            0.03f,
            distance + boundsRadius * 3f + 10f);
        return true;
    }

    private void SetCameraTarget(
        Vector3 position,
        Quaternion rotation,
        float targetFieldOfView,
        float nearClipPlane,
        float farClipPlane)
    {
        targetCameraPosition = position;
        targetCameraRotation = rotation;
        targetCameraFieldOfView = targetFieldOfView;
        previewCamera.nearClipPlane = nearClipPlane;
        previewCamera.farClipPlane = farClipPlane;

        if (cameraPoseInitialized)
        {
            return;
        }

        previewCamera.transform.SetPositionAndRotation(position, rotation);
        previewCamera.fieldOfView = targetFieldOfView;
        cameraPoseInitialized = true;
    }

    private void UpdateCameraTransition()
    {
        if (!cameraPoseInitialized || previewCamera == null)
        {
            return;
        }

        if (cameraTransitionSpeed <= 0f)
        {
            previewCamera.transform.SetPositionAndRotation(
                targetCameraPosition,
                targetCameraRotation);
            previewCamera.fieldOfView = targetCameraFieldOfView;
            return;
        }

        float blend = 1f - Mathf.Exp(
            -cameraTransitionSpeed * Time.unscaledDeltaTime);
        previewCamera.transform.position = Vector3.Lerp(
            previewCamera.transform.position,
            targetCameraPosition,
            blend);
        previewCamera.transform.rotation = Quaternion.Slerp(
            previewCamera.transform.rotation,
            targetCameraRotation,
            blend);
        previewCamera.fieldOfView = Mathf.Lerp(
            previewCamera.fieldOfView,
            targetCameraFieldOfView,
            blend);
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
