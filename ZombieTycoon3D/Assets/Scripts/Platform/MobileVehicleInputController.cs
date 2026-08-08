using ArcadeVP;
using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public sealed class MobileVehicleInputController : MonoBehaviour
{
    private const int NoFinger = -1;

    [Header("References")]
    [SerializeField] private ArcadeVehicleController vehicleController;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private UIDocument uiDocument;

    [Header("Touch Area")]
    [SerializeField, Range(0.35f, 0.8f)]
    private float touchAreaWidth = 0.6f;

    [SerializeField, Range(0.5f, 1f)]
    private float touchAreaHeight = 0.82f;

    [Header("Joystick")]
    [SerializeField, Min(32f)] private float joystickRadius = 90f;
    [SerializeField, Min(16f)] private float knobRadius = 34f;
    [SerializeField, Range(0f, 0.5f)] private float deadZone = 0.12f;
    [SerializeField, Range(15f, 120f)]
    private float fullSteeringAngle = 55f;
    [SerializeField] private bool enableMouseSimulation = true;

    private VisualElement joystickLayer;
    private VisualElement joystickBase;
    private VisualElement joystickKnob;
    private int activeFingerId = NoFinger;
    private bool mouseActive;
    private Vector2 originPanelPosition;
    private float lastTurnDirection = 1f;

    public void Configure(
        ArcadeVehicleController controller,
        Camera camera)
    {
        vehicleController = controller;
        gameplayCamera = camera;
        EnsureVisuals();
    }

    public void ResetInput()
    {
        activeFingerId = NoFinger;
        mouseActive = false;
        SetJoystickVisible(false);
        ProvideInput(Vector2.zero);
    }

    private void Awake()
    {
        if (uiDocument == null)
        {
            uiDocument = FindFirstObjectByType<UIDocument>();
        }
    }

    private void OnEnable()
    {
        EnsureVisuals();
        ResetInput();
    }

    private void Update()
    {
        if (vehicleController == null)
        {
            return;
        }

        if (Input.touchCount > 0 || activeFingerId != NoFinger)
        {
            HandleTouches();
            return;
        }

#if UNITY_EDITOR
        if (enableMouseSimulation)
        {
            HandleMouse();
            return;
        }
#endif

        if (mouseActive)
        {
            ResetInput();
        }
    }

    private void OnDisable()
    {
        ResetInput();
    }

    private void HandleTouches()
    {
        if (activeFingerId != NoFinger)
        {
            for (int index = 0; index < Input.touchCount; index++)
            {
                Touch touch = Input.GetTouch(index);
                if (touch.fingerId != activeFingerId)
                {
                    continue;
                }

                if (touch.phase == TouchPhase.Ended
                    || touch.phase == TouchPhase.Canceled)
                {
                    ResetInput();
                }
                else
                {
                    UpdateJoystick(touch.position);
                }

                return;
            }

            ResetInput();
            return;
        }

        for (int index = 0; index < Input.touchCount; index++)
        {
            Touch touch = Input.GetTouch(index);
            if (touch.phase != TouchPhase.Began
                || !CanStartJoystickAt(touch.position))
            {
                continue;
            }

            activeFingerId = touch.fingerId;
            BeginJoystick(touch.position);
            return;
        }
    }

#if UNITY_EDITOR
    private void HandleMouse()
    {
        Vector2 mousePosition = Input.mousePosition;

        if (Input.GetMouseButtonDown(0)
            && CanStartJoystickAt(mousePosition))
        {
            mouseActive = true;
            BeginJoystick(mousePosition);
        }

        if (!mouseActive)
        {
            return;
        }

        if (Input.GetMouseButton(0))
        {
            UpdateJoystick(mousePosition);
        }
        else
        {
            ResetInput();
        }
    }
#endif

    private bool IsInsideTouchArea(Vector2 screenPosition)
    {
        return screenPosition.x <= Screen.width * touchAreaWidth
               && screenPosition.y <= Screen.height * touchAreaHeight;
    }

    private bool CanStartJoystickAt(Vector2 screenPosition)
    {
        return IsInsideTouchArea(screenPosition)
               && !IsPointerOverInteractiveUi(screenPosition);
    }

    private bool IsPointerOverInteractiveUi(Vector2 screenPosition)
    {
        if (uiDocument == null)
        {
            return false;
        }

        VisualElement root = uiDocument.rootVisualElement;
        if (root?.panel == null)
        {
            return false;
        }

        VisualElement element = root.panel.Pick(
            ScreenToPanel(screenPosition));
        while (element != null && element != root)
        {
            if (element.focusable
                || element is Button
                || element is Toggle
                || element is TextField
                || element is DropdownField
                || element is Slider
                || element is SliderInt)
            {
                return true;
            }

            element = element.parent;
        }

        return false;
    }

    private void BeginJoystick(Vector2 screenPosition)
    {
        EnsureVisuals();
        originPanelPosition = ScreenToPanel(screenPosition);

        float diameter = joystickRadius * 2f;
        joystickBase.style.left =
            originPanelPosition.x - joystickRadius;
        joystickBase.style.top =
            originPanelPosition.y - joystickRadius;
        joystickBase.style.width = diameter;
        joystickBase.style.height = diameter;

        PositionKnob(Vector2.zero);
        SetJoystickVisible(true);
        ProvideInput(Vector2.zero);
    }

    private void UpdateJoystick(Vector2 screenPosition)
    {
        Vector2 currentPanelPosition = ScreenToPanel(screenPosition);
        Vector2 panelDelta = currentPanelPosition - originPanelPosition;
        Vector2 clampedDelta = Vector2.ClampMagnitude(
            panelDelta,
            joystickRadius);

        PositionKnob(clampedDelta);

        Vector2 normalized = clampedDelta / joystickRadius;
        float magnitude = normalized.magnitude;
        if (magnitude <= deadZone)
        {
            ProvideInput(Vector2.zero);
            return;
        }

        float remappedMagnitude = Mathf.InverseLerp(
            deadZone,
            1f,
            magnitude);

        Vector2 remapped =
            normalized.normalized * remappedMagnitude;

        ProvideInput(new Vector2(remapped.x, -remapped.y));
    }

    private void ProvideInput(Vector2 input)
    {
        if (vehicleController == null)
        {
            return;
        }

        float throttle = Mathf.Clamp01(input.magnitude);
        if (throttle <= 0f)
        {
            vehicleController.ProvideInputs(0f, 0f, 0f);
            return;
        }

        Transform vehicleTransform = vehicleController.carBody != null
            ? vehicleController.carBody.transform
            : vehicleController.transform;

        Vector3 cameraForward = gameplayCamera != null
            ? gameplayCamera.transform.forward
            : Vector3.forward;
        Vector3 cameraRight = gameplayCamera != null
            ? gameplayCamera.transform.right
            : Vector3.right;

        cameraForward = Vector3.ProjectOnPlane(
            cameraForward,
            Vector3.up).normalized;
        cameraRight = Vector3.ProjectOnPlane(
            cameraRight,
            Vector3.up).normalized;

        Vector3 desiredDirection =
            cameraRight * input.x + cameraForward * input.y;
        Vector3 vehicleForward = Vector3.ProjectOnPlane(
            vehicleTransform.forward,
            Vector3.up).normalized;

        if (desiredDirection.sqrMagnitude <= Mathf.Epsilon
            || vehicleForward.sqrMagnitude <= Mathf.Epsilon)
        {
            vehicleController.ProvideInputs(0f, 0f, 0f);
            return;
        }

        float angle = Vector3.SignedAngle(
            vehicleForward,
            desiredDirection.normalized,
            Vector3.up);
        if (Mathf.Abs(angle) >= 179f)
        {
            angle = 180f * lastTurnDirection;
        }

        float steering = Mathf.Clamp(
            angle / fullSteeringAngle,
            -1f,
            1f);
        if (Mathf.Abs(steering) > 0.01f)
        {
            lastTurnDirection = Mathf.Sign(steering);
        }

        vehicleController.ProvideInputs(
            steering,
            throttle,
            0f);
    }

    private Vector2 ScreenToPanel(Vector2 screenPosition)
    {
        if (uiDocument == null
            || uiDocument.rootVisualElement.panel == null)
        {
            return new Vector2(
                screenPosition.x,
                Screen.height - screenPosition.y);
        }

        return RuntimePanelUtils.ScreenToPanel(
            uiDocument.rootVisualElement.panel,
            new Vector2(
                screenPosition.x,
                Screen.height - screenPosition.y));
    }

    private void EnsureVisuals()
    {
        if (joystickLayer != null || uiDocument == null)
        {
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;
        if (root == null)
        {
            return;
        }

        joystickLayer = new VisualElement
        {
            name = "mobile-joystick-layer",
            pickingMode = PickingMode.Ignore
        };
        joystickLayer.style.position = Position.Absolute;
        joystickLayer.style.left = 0f;
        joystickLayer.style.right = 0f;
        joystickLayer.style.top = 0f;
        joystickLayer.style.bottom = 0f;

        joystickBase = new VisualElement
        {
            name = "mobile-joystick-base",
            pickingMode = PickingMode.Ignore
        };
        joystickBase.style.position = Position.Absolute;
        joystickBase.style.backgroundColor =
            new Color(0.05f, 0.07f, 0.08f, 0.42f);
        joystickBase.style.borderLeftColor =
            new Color(1f, 1f, 1f, 0.34f);
        joystickBase.style.borderRightColor =
            new Color(1f, 1f, 1f, 0.34f);
        joystickBase.style.borderTopColor =
            new Color(1f, 1f, 1f, 0.34f);
        joystickBase.style.borderBottomColor =
            new Color(1f, 1f, 1f, 0.34f);
        joystickBase.style.borderLeftWidth = 3f;
        joystickBase.style.borderRightWidth = 3f;
        joystickBase.style.borderTopWidth = 3f;
        joystickBase.style.borderBottomWidth = 3f;
        joystickBase.style.borderTopLeftRadius = joystickRadius;
        joystickBase.style.borderTopRightRadius = joystickRadius;
        joystickBase.style.borderBottomLeftRadius = joystickRadius;
        joystickBase.style.borderBottomRightRadius = joystickRadius;

        joystickKnob = new VisualElement
        {
            name = "mobile-joystick-knob",
            pickingMode = PickingMode.Ignore
        };
        joystickKnob.style.position = Position.Absolute;
        joystickKnob.style.width = knobRadius * 2f;
        joystickKnob.style.height = knobRadius * 2f;
        joystickKnob.style.backgroundColor =
            new Color(0.95f, 0.78f, 0.24f, 0.82f);
        joystickKnob.style.borderTopLeftRadius = knobRadius;
        joystickKnob.style.borderTopRightRadius = knobRadius;
        joystickKnob.style.borderBottomLeftRadius = knobRadius;
        joystickKnob.style.borderBottomRightRadius = knobRadius;

        joystickBase.Add(joystickKnob);
        joystickLayer.Add(joystickBase);
        root.Add(joystickLayer);
        SetJoystickVisible(false);
    }

    private void PositionKnob(Vector2 panelDelta)
    {
        if (joystickKnob == null)
        {
            return;
        }

        joystickKnob.style.left =
            joystickRadius + panelDelta.x - knobRadius;
        joystickKnob.style.top =
            joystickRadius + panelDelta.y - knobRadius;
    }

    private void SetJoystickVisible(bool visible)
    {
        if (joystickLayer == null)
        {
            return;
        }

        joystickLayer.style.display = visible
            ? DisplayStyle.Flex
            : DisplayStyle.None;
    }
}
