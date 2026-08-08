using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
[RequireComponent(typeof(UIDocument))]
public sealed class IosSafeAreaController : MonoBehaviour
{
    private const float ResultBasePadding = 14f;
    private const float PauseButtonTop = 18f;
    private const float PauseButtonRight = 20f;

    [SerializeField] private UIDocument uiDocument;

    private VisualElement documentRoot;
    private VisualElement garageRoot;
    private VisualElement missionHud;
    private VisualElement missionResult;
    private VisualElement missionIntro;
    private VisualElement missionPause;
    private VisualElement settingsOverlay;
    private VisualElement missionPauseButton;
    private Rect lastSafeArea = new(-1f, -1f, -1f, -1f);
    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;

    private void Reset()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (uiDocument == null)
        {
            Debug.LogError(
                "IosSafeAreaController: UIDocument is required.",
                this);
            enabled = false;
            return;
        }

        BindVisualTree();
        documentRoot.RegisterCallback<GeometryChangedEvent>(
            HandleGeometryChanged);
        ApplySafeArea(force: true);
    }

    private void OnDisable()
    {
        if (documentRoot != null)
        {
            documentRoot.UnregisterCallback<GeometryChangedEvent>(
                HandleGeometryChanged);
        }
    }

    private void Update()
    {
        ApplySafeArea(force: false);
    }

    private void BindVisualTree()
    {
        documentRoot = uiDocument.rootVisualElement;
        documentRoot.AddToClassList("platform-ios");
        garageRoot = documentRoot.Q<VisualElement>("garage-root");
        missionHud = documentRoot.Q<VisualElement>("mission-hud");
        missionResult = documentRoot.Q<VisualElement>("mission-result");
        missionIntro = documentRoot.Q<VisualElement>("mission-intro");
        missionPause = documentRoot.Q<VisualElement>("mission-pause");
        settingsOverlay = documentRoot.Q<VisualElement>("settings-overlay");
        missionPauseButton =
            documentRoot.Q<VisualElement>("mission-pause-button");
    }

    private void HandleGeometryChanged(GeometryChangedEvent evt)
    {
        ApplySafeArea(force: true);
    }

    private void ApplySafeArea(bool force)
    {
        Rect safeArea = Screen.safeArea;
        if (!force
            && Screen.width == lastScreenWidth
            && Screen.height == lastScreenHeight
            && safeArea == lastSafeArea)
        {
            return;
        }

        if (documentRoot?.panel == null
            || documentRoot.resolvedStyle.width <= 0f
            || documentRoot.resolvedStyle.height <= 0f)
        {
            return;
        }

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        lastSafeArea = safeArea;

        Vector2 safeTopLeft = RuntimePanelUtils.ScreenToPanel(
            documentRoot.panel,
            new Vector2(
                safeArea.xMin,
                Screen.height - safeArea.yMax));
        Vector2 safeBottomRight = RuntimePanelUtils.ScreenToPanel(
            documentRoot.panel,
            new Vector2(
                safeArea.xMax,
                Screen.height - safeArea.yMin));

        float left = Mathf.Max(0f, safeTopLeft.x);
        float top = Mathf.Max(0f, safeTopLeft.y);
        float right = Mathf.Max(
            0f,
            documentRoot.resolvedStyle.width - safeBottomRight.x);
        float bottom = Mathf.Max(
            0f,
            documentRoot.resolvedStyle.height - safeBottomRight.y);

        SetPadding(garageRoot, left, top, right, bottom, 0f);
        SetPadding(missionHud, left, top, right, bottom, 0f);
        SetPadding(
            missionResult,
            left,
            top,
            right,
            bottom,
            ResultBasePadding);
        SetPadding(missionIntro, left, top, right, bottom, 0f);
        SetPadding(missionPause, left, top, right, bottom, 0f);
        SetPadding(settingsOverlay, left, top, right, bottom, 0f);

        if (missionPauseButton != null)
        {
            missionPauseButton.style.top = PauseButtonTop + top;
            missionPauseButton.style.right = PauseButtonRight + right;
        }
    }

    private static void SetPadding(
        VisualElement element,
        float left,
        float top,
        float right,
        float bottom,
        float basePadding)
    {
        if (element == null)
        {
            return;
        }

        element.style.paddingLeft = basePadding + left;
        element.style.paddingTop = basePadding + top;
        element.style.paddingRight = basePadding + right;
        element.style.paddingBottom = basePadding + bottom;
    }
}
