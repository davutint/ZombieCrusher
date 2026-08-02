using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
[RequireComponent(typeof(UIDocument))]
public sealed class GarageUiController : MonoBehaviour
{
    private const int MissionHealthSegmentCount = 8;
    private const float MissionSpeedNeedleSmoothTime = 0.045f;
    private const float MissionSpeedSnapThreshold = 0.015f;

    private enum GarageScreen
    {
        Assembly,
        Gallery,
        Parts
    }

    private sealed class StatElements
    {
        public GarageVehicleStat stat;
        public Label value;
        public Label delta;
        public VisualElement currentFill;
        public VisualElement previewFill;
    }

    [SerializeField] private UIDocument document;
    [SerializeField] private GarageBuildState buildState;
    [SerializeField] private GarageEconomyController economy;
    [SerializeField] private GaragePreviewController previewController;

    private readonly List<StatElements> statElements = new();
    private GarageScreen activeScreen = GarageScreen.Assembly;
    private GarageAttachmentSlot partsFilter = GarageAttachmentSlot.Front;

    private VisualElement garageRoot;
    private VisualElement missionHud;
    private VisualElement missionObjectiveCard;
    private Label missionTimer;
    private Label missionKills;
    private Label missionScore;
    private VisualElement missionMayhemCard;
    private Label missionMayhemTier;
    private Label missionMayhemMultiplier;
    private VisualElement missionMayhemFill;
    private VisualElement missionMayhemAnnouncement;
    private Label missionMayhemAnnouncementLabel;
    private Label missionVehicleName;
    private VisualElement missionSpeedometer;
    private VisualElement missionSpeedNeedle;
    private Label missionCurrentSpeed;
    private Label missionHealth;
    private readonly VisualElement[] missionHealthSegments =
        new VisualElement[MissionHealthSegmentCount];
    private VisualElement missionResult;
    private VisualElement missionResultPanel;
    private Label resultStatus;
    private Label resultTitle;
    private Label resultDescription;
    private Label resultKills;
    private Label resultScore;
    private Label resultBonusKills;
    private Label resultHealth;
    private VisualElement resultMayhem;
    private Label resultMayhemTier;
    private Label resultBestChain;
    private Label resultKillScrap;
    private Label resultSuccessBonus;
    private Label resultTotalScrap;
    private Label resultBalance;
    private Button resultButton;
    private Button assemblyTab;
    private Button galleryTab;
    private Button partsTab;
    private Label leftTitle;
    private VisualElement leftFilters;
    private ScrollView leftList;
    private Label contextLabel;
    private VisualElement statGrid;
    private Label rightTitle;
    private ScrollView rightList;
    private Label detailTitle;
    private Label detailDescription;
    private VisualElement detailMechanics;
    private Label detailEffect;
    private Label detailTradeoff;
    private Button contextAction;
    private Label contextHint;
    private Label selectedBuildLabel;
    private Label balanceValue;
    private Button missionButton;
    private VisualElement previewViewport;
    private Label missionEffectFeedback;

    private bool pointerDragging;
    private Vector2 previousPointerPosition;
    private float mayhemAnnouncementHideTime;
    private float mayhemPulseEndTime;
    private bool mayhemAnnouncementVisible;
    private bool mayhemCardPulsing;
    private int displayedMissionSeconds = int.MinValue;
    private int displayedMissionSpeed = int.MinValue;
    private int displayedMissionHealth = int.MinValue;
    private int displayedMissionMaxHealth = int.MinValue;
    private float missionGaugeMaximumSpeed = 120f;
    private float targetMissionSpeed;
    private float displayedMissionNeedleSpeed;
    private float missionSpeedNeedleVelocity;
    private bool missionSpeedAnimationEnabled;

    public event Action MissionRequested;
    public event Action ResultAcknowledged;

    private void Reset()
    {
        document = GetComponent<UIDocument>();
        buildState = GetComponent<GarageBuildState>();
        economy = GetComponent<GarageEconomyController>();
        previewController = GetComponent<GaragePreviewController>();
    }

    private void OnEnable()
    {
        if (document == null)
        {
            document = GetComponent<UIDocument>();
        }

        if (buildState == null)
        {
            buildState = GetComponent<GarageBuildState>();
        }

        if (economy == null)
        {
            economy = GetComponent<GarageEconomyController>();
        }

        if (previewController == null)
        {
            previewController = GetComponent<GaragePreviewController>();
        }

        if (document == null
            || buildState == null
            || economy == null
            || previewController == null)
        {
            Debug.LogError(
                "GarageUiController: UIDocument, GarageBuildState, GarageEconomyController and GaragePreviewController are required.",
                this);
            enabled = false;
            return;
        }

        BindVisualTree();
        buildState.Changed += Refresh;
        economy.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (buildState != null)
        {
            buildState.Changed -= Refresh;
        }

        if (economy != null)
        {
            economy.Changed -= Refresh;
        }
    }

    private void Update()
    {
        float currentTime = Time.unscaledTime;
        if (mayhemAnnouncementVisible
            && currentTime >= mayhemAnnouncementHideTime)
        {
            mayhemAnnouncementVisible = false;
            missionMayhemAnnouncement.style.display = DisplayStyle.None;
        }

        if (mayhemCardPulsing && currentTime >= mayhemPulseEndTime)
        {
            mayhemCardPulsing = false;
            missionMayhemCard.RemoveFromClassList(
                "mission-mayhem-card--pulse");
        }

        if (missionSpeedAnimationEnabled)
        {
            AnimateMissionSpeedometer();
        }
    }

    public void ShowGarage()
    {
        garageRoot.style.display = DisplayStyle.Flex;
        missionHud.style.display = DisplayStyle.None;
        missionResult.style.display = DisplayStyle.None;
        HideMayhemAnnouncement();
        missionSpeedAnimationEnabled = false;
        previewController.SetVisible(true);
        activeScreen = GarageScreen.Assembly;
        buildState.ClearPreview();
        Refresh();
    }

    public void HideGarageForMission()
    {
        garageRoot.style.display = DisplayStyle.None;
        missionHud.style.display = DisplayStyle.Flex;
        missionResult.style.display = DisplayStyle.None;
        HideMayhemAnnouncement();
        missionSpeedAnimationEnabled = true;
        previewController.SetVisible(false);
    }

    public void UpdateMissionTimer(float remainingSeconds)
    {
        if (missionTimer == null)
        {
            return;
        }

        int seconds = Mathf.Max(0, Mathf.CeilToInt(remainingSeconds));
        if (seconds == displayedMissionSeconds)
        {
            return;
        }

        displayedMissionSeconds = seconds;
        int minutesPart = seconds / 60;
        int secondsPart = seconds % 60;
        missionTimer.text = $"{minutesPart:0}:{secondsPart:00}";
    }

    public void UpdateMissionProgress(MissionProgress progress)
    {
        if (missionKills == null)
        {
            return;
        }

        missionKills.text = $"{progress.Kills} / {progress.KillTarget}";
        missionScore.text = progress.Score.ToString("N0");
        missionObjectiveCard.EnableInClassList(
            "mission-objective-card--complete",
            progress.TargetReached);
    }

    public void UpdateMayhem(MayhemProgress progress)
    {
        if (missionMayhemCard == null)
        {
            return;
        }

        missionMayhemTier.text = MayhemRules.GetLabel(progress.Tier);
        missionMayhemMultiplier.text =
            $"x{progress.ScoreMultiplier:0.00}";
        missionMayhemFill.style.width =
            Length.Percent(Mathf.Clamp01(progress.Meter01) * 100f);
        ApplyMayhemTierClass(missionMayhemCard, progress.Tier);
    }

    public void ShowMayhemTierReached(MayhemProgress progress)
    {
        if (missionMayhemAnnouncement == null)
        {
            return;
        }

        missionMayhemAnnouncementLabel.text =
            $"{MayhemRules.GetLabel(progress.Tier)}   x{progress.ScoreMultiplier:0.00}";
        ApplyMayhemTierClass(
            missionMayhemAnnouncement,
            progress.Tier);
        missionMayhemAnnouncement.style.display = DisplayStyle.Flex;
        mayhemAnnouncementVisible = true;
        mayhemAnnouncementHideTime = Time.unscaledTime + 0.95f;

        missionMayhemCard.AddToClassList("mission-mayhem-card--pulse");
        mayhemCardPulsing = true;
        mayhemPulseEndTime = Time.unscaledTime + 0.24f;
    }

    public void ConfigureMissionVehicle(
        string vehicleName,
        float maximumSpeed)
    {
        if (missionVehicleName == null)
        {
            return;
        }

        missionVehicleName.text = string.IsNullOrWhiteSpace(vehicleName)
            ? "ARAÇ"
            : vehicleName.ToUpperInvariant();
        displayedMissionSpeed = int.MinValue;
        missionGaugeMaximumSpeed = Mathf.Max(40f, maximumSpeed);
        targetMissionSpeed = 0f;
        displayedMissionNeedleSpeed = 0f;
        missionSpeedNeedleVelocity = 0f;
        missionCurrentSpeed.text = "0";
        UpdateMissionSpeedometer(0f);
    }

    public void SetMissionSpeedTarget(float currentSpeed)
    {
        targetMissionSpeed = Mathf.Max(0f, Mathf.Abs(currentSpeed));
    }

    private void AnimateMissionSpeedometer()
    {
        float previousSpeed = displayedMissionNeedleSpeed;
        displayedMissionNeedleSpeed = Mathf.SmoothDamp(
            displayedMissionNeedleSpeed,
            targetMissionSpeed,
            ref missionSpeedNeedleVelocity,
            MissionSpeedNeedleSmoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime);

        if (Mathf.Abs(displayedMissionNeedleSpeed - targetMissionSpeed)
            <= MissionSpeedSnapThreshold)
        {
            displayedMissionNeedleSpeed = targetMissionSpeed;
            missionSpeedNeedleVelocity = 0f;
        }

        int roundedSpeed = Mathf.Max(
            0,
            Mathf.RoundToInt(displayedMissionNeedleSpeed));
        if (roundedSpeed != displayedMissionSpeed)
        {
            displayedMissionSpeed = roundedSpeed;
            missionCurrentSpeed.text = roundedSpeed.ToString();
        }

        if (!Mathf.Approximately(
                previousSpeed,
                displayedMissionNeedleSpeed))
        {
            UpdateMissionSpeedometer(displayedMissionNeedleSpeed);
        }
    }

    private void UpdateMissionSpeedometer(float currentSpeed)
    {
        if (missionSpeedNeedle == null || missionSpeedometer == null)
        {
            return;
        }

        float speedRatio = Mathf.Clamp01(
            currentSpeed / Mathf.Max(1f, missionGaugeMaximumSpeed));
        float needleAngle = Mathf.Lerp(-120f, 120f, speedRatio);
        missionSpeedNeedle.style.rotate =
            new Rotate(Angle.Degrees(needleAngle));
        missionSpeedometer.EnableInClassList(
            "mission-speedometer--fast",
            speedRatio >= 0.86f);
    }

    public void UpdateMissionHealth(float currentHealth, float maximumHealth)
    {
        if (missionHealth == null)
        {
            return;
        }

        float safeMaximum = Mathf.Max(1f, maximumHealth);
        float safeCurrent = Mathf.Clamp(currentHealth, 0f, safeMaximum);
        float ratio = safeCurrent / safeMaximum;
        int roundedCurrent = Mathf.CeilToInt(safeCurrent);
        int roundedMaximum = Mathf.CeilToInt(safeMaximum);
        if (roundedCurrent != displayedMissionHealth
            || roundedMaximum != displayedMissionMaxHealth)
        {
            displayedMissionHealth = roundedCurrent;
            displayedMissionMaxHealth = roundedMaximum;
            missionHealth.text = $"{roundedCurrent} / {roundedMaximum}";
        }

        int visibleSegmentCount = Mathf.CeilToInt(
            ratio * MissionHealthSegmentCount);
        bool critical = ratio <= 0.3f;
        for (int i = 0; i < missionHealthSegments.Length; i++)
        {
            VisualElement segment = missionHealthSegments[i];
            if (segment == null)
            {
                continue;
            }

            segment.EnableInClassList(
                "mission-health-segment--active",
                i < visibleSegmentCount);
            segment.EnableInClassList(
                "mission-health-segment--critical",
                critical);
        }
    }

    public void ShowMissionEffectFeedback(
        string message,
        GarageAttachmentFeedbackTone tone)
    {
        if (missionEffectFeedback == null)
        {
            return;
        }

        missionEffectFeedback.text = message;
        missionEffectFeedback.style.display = DisplayStyle.Flex;
        missionEffectFeedback.EnableInClassList(
            "mission-effect-feedback--impact",
            tone == GarageAttachmentFeedbackTone.Impact);
        missionEffectFeedback.EnableInClassList(
            "mission-effect-feedback--defense",
            tone == GarageAttachmentFeedbackTone.Defense);
        missionEffectFeedback.EnableInClassList(
            "mission-effect-feedback--repair",
            tone == GarageAttachmentFeedbackTone.Repair);
    }

    public void HideMissionEffectFeedback()
    {
        if (missionEffectFeedback != null)
        {
            missionEffectFeedback.style.display = DisplayStyle.None;
        }
    }

    public void ShowMissionResult(MissionResult result)
    {
        garageRoot.style.display = DisplayStyle.None;
        missionHud.style.display = DisplayStyle.None;
        missionResult.style.display = DisplayStyle.Flex;
        missionSpeedAnimationEnabled = false;
        previewController.SetVisible(false);

        resultStatus.text = result.Succeeded ? "BAŞARILI" : "BAŞARISIZ";
        resultTitle.text = result.Succeeded
            ? "GÖREV TAMAMLANDI"
            : result.EndReason == MissionEndReason.VehicleDestroyed
                ? "ARAÇ PARÇALANDI"
                : "HEDEF KAÇTI";
        resultDescription.text = result.Succeeded
            ? "İmha hedefi tamamlandı. Safehouse ekibi dönüş için hazır."
            : result.EndReason == MissionEndReason.VehicleDestroyed
                ? "Araç görev alanında kullanılamaz hâle geldi."
                : "Süre doldu ancak imha hedefi tamamlanamadı.";

        resultKills.text =
            $"{result.Progress.Kills} / {result.Progress.KillTarget}";
        resultScore.text = result.Progress.Score.ToString("N0");
        resultBonusKills.text = result.Progress.BonusKills.ToString("N0");
        resultHealth.text =
            $"{Mathf.CeilToInt(Mathf.Max(0f, result.RemainingHealth))}"
            + $" / {Mathf.CeilToInt(Mathf.Max(1f, result.MaximumHealth))}";
        MayhemTier highestTier = result.Progress.Mayhem.HighestTier;
        resultMayhemTier.text = highestTier == MayhemTier.None
            ? "TEMPO YOK"
            : MayhemRules.GetLabel(highestTier);
        resultBestChain.text =
            $"{result.Progress.Mayhem.BestChain:N0} KILL";
        ApplyMayhemTierClass(resultMayhem, highestTier);
        resultKillScrap.text = $"+{result.Reward.KillScrap:N0}";
        resultSuccessBonus.text = $"+{result.Reward.CompletionBonus:N0}";
        resultTotalScrap.text = $"+{result.Reward.TotalScrap:N0} HURDA";
        resultBalance.text = $"{result.Reward.BalanceAfter:N0} HURDA";

        missionResultPanel.EnableInClassList(
            "mission-result-panel--success",
            result.Succeeded);
        missionResultPanel.EnableInClassList(
            "mission-result-panel--failure",
            !result.Succeeded);
        resultStatus.EnableInClassList(
            "mission-result-status--success",
            result.Succeeded);
        resultStatus.EnableInClassList(
            "mission-result-status--failure",
            !result.Succeeded);
    }

    private void BindVisualTree()
    {
        VisualElement root = document.rootVisualElement;
        garageRoot = RequireElement<VisualElement>(root, "garage-root");
        missionHud = RequireElement<VisualElement>(root, "mission-hud");
        missionObjectiveCard =
            RequireElement<VisualElement>(root, "mission-objective-card");
        missionTimer = RequireElement<Label>(root, "mission-timer");
        missionKills = RequireElement<Label>(root, "mission-kills");
        missionScore = RequireElement<Label>(root, "mission-score");
        missionMayhemCard =
            RequireElement<VisualElement>(root, "mission-mayhem-card");
        missionMayhemTier =
            RequireElement<Label>(root, "mission-mayhem-tier");
        missionMayhemMultiplier =
            RequireElement<Label>(root, "mission-mayhem-multiplier");
        missionMayhemFill =
            RequireElement<VisualElement>(root, "mission-mayhem-fill");
        missionMayhemAnnouncement =
            RequireElement<VisualElement>(root, "mission-mayhem-announcement");
        missionMayhemAnnouncementLabel =
            RequireElement<Label>(root, "mission-mayhem-announcement-label");
        missionVehicleName =
            RequireElement<Label>(root, "mission-vehicle-name");
        missionSpeedometer =
            RequireElement<VisualElement>(root, "mission-speedometer");
        missionSpeedNeedle =
            RequireElement<VisualElement>(root, "mission-speed-needle");
        missionCurrentSpeed =
            RequireElement<Label>(root, "mission-current-speed");
        missionHealth = RequireElement<Label>(root, "mission-health");
        for (int i = 0; i < missionHealthSegments.Length; i++)
        {
            missionHealthSegments[i] = RequireElement<VisualElement>(
                root,
                $"mission-health-segment-{i}");
        }
        missionResult = RequireElement<VisualElement>(root, "mission-result");
        missionResultPanel =
            RequireElement<VisualElement>(root, "mission-result-panel");
        resultStatus = RequireElement<Label>(root, "result-status");
        resultTitle = RequireElement<Label>(root, "result-title");
        resultDescription =
            RequireElement<Label>(root, "result-description");
        resultKills = RequireElement<Label>(root, "result-kills");
        resultScore = RequireElement<Label>(root, "result-score");
        resultBonusKills =
            RequireElement<Label>(root, "result-bonus-kills");
        resultHealth = RequireElement<Label>(root, "result-health");
        resultMayhem =
            RequireElement<VisualElement>(root, "result-mayhem");
        resultMayhemTier =
            RequireElement<Label>(root, "result-mayhem-tier");
        resultBestChain =
            RequireElement<Label>(root, "result-best-chain");
        resultKillScrap = RequireElement<Label>(root, "result-kill-scrap");
        resultSuccessBonus =
            RequireElement<Label>(root, "result-success-bonus");
        resultTotalScrap =
            RequireElement<Label>(root, "result-total-scrap");
        resultBalance = RequireElement<Label>(root, "result-balance");
        resultButton = RequireElement<Button>(root, "result-button");
        assemblyTab = RequireElement<Button>(root, "assembly-tab");
        galleryTab = RequireElement<Button>(root, "gallery-tab");
        partsTab = RequireElement<Button>(root, "parts-tab");
        leftTitle = RequireElement<Label>(root, "left-title");
        leftFilters = RequireElement<VisualElement>(root, "left-filters");
        leftList = RequireElement<ScrollView>(root, "left-list");
        contextLabel = RequireElement<Label>(root, "context-label");
        statGrid = RequireElement<VisualElement>(root, "stat-grid");
        rightTitle = RequireElement<Label>(root, "right-title");
        rightList = RequireElement<ScrollView>(root, "right-list");
        detailTitle = RequireElement<Label>(root, "detail-title");
        detailDescription = RequireElement<Label>(root, "detail-description");
        detailMechanics =
            RequireElement<VisualElement>(root, "detail-mechanics");
        detailEffect = RequireElement<Label>(root, "detail-effect");
        detailTradeoff = RequireElement<Label>(root, "detail-tradeoff");
        contextAction = RequireElement<Button>(root, "context-action");
        contextHint = RequireElement<Label>(root, "context-hint");
        selectedBuildLabel = RequireElement<Label>(root, "selected-build");
        balanceValue = RequireElement<Label>(root, "balance-value");
        missionButton = RequireElement<Button>(root, "mission-button");
        previewViewport = RequireElement<VisualElement>(root, "preview-viewport");
        missionEffectFeedback =
            RequireElement<Label>(root, "mission-effect-feedback");

        assemblyTab.clicked += () => SwitchScreen(GarageScreen.Assembly);
        galleryTab.clicked += () => SwitchScreen(GarageScreen.Gallery);
        partsTab.clicked += () => SwitchScreen(GarageScreen.Parts);
        missionButton.clicked += () => MissionRequested?.Invoke();
        resultButton.clicked += () => ResultAcknowledged?.Invoke();

        previewViewport.RegisterCallback<PointerDownEvent>(OnPreviewPointerDown);
        previewViewport.RegisterCallback<PointerMoveEvent>(OnPreviewPointerMove);
        previewViewport.RegisterCallback<PointerUpEvent>(OnPreviewPointerUp);
        previewViewport.RegisterCallback<PointerCaptureOutEvent>(_ => EndPreviewDrag());

        CreateStatElements();
    }

    private void HideMayhemAnnouncement()
    {
        mayhemAnnouncementVisible = false;
        mayhemCardPulsing = false;
        if (missionMayhemAnnouncement != null)
        {
            missionMayhemAnnouncement.style.display = DisplayStyle.None;
        }

        missionMayhemCard?.RemoveFromClassList(
            "mission-mayhem-card--pulse");
    }

    private static void ApplyMayhemTierClass(
        VisualElement element,
        MayhemTier tier)
    {
        element.EnableInClassList(
            "mayhem-tier--none",
            tier == MayhemTier.None);
        element.EnableInClassList(
            "mayhem-tier--rampage",
            tier == MayhemTier.Rampage);
        element.EnableInClassList(
            "mayhem-tier--carnage",
            tier == MayhemTier.Carnage);
        element.EnableInClassList(
            "mayhem-tier--slaughter",
            tier == MayhemTier.Slaughter);
        element.EnableInClassList(
            "mayhem-tier--mayhem",
            tier == MayhemTier.Mayhem);
    }

    private void SwitchScreen(GarageScreen screen)
    {
        bool preserveGalleryVehicle =
            activeScreen == GarageScreen.Gallery
            && screen == GarageScreen.Parts
            && buildState.DisplayedVehicle != null;

        activeScreen = screen;
        if (preserveGalleryVehicle)
        {
            buildState.PreviewPart(null);
        }
        else
        {
            buildState.ClearPreview();
        }

        Refresh();
    }

    private void Refresh()
    {
        if (garageRoot == null || buildState.Catalog == null)
        {
            return;
        }

        balanceValue.text = $"{economy.Scrap:N0} HURDA";
        UpdateTabs();
        PopulateLeftRail();
        PopulateRightRail();
        UpdateStats();
        UpdatePreview();
        UpdateSelectedBuild();
    }

    private void UpdateTabs()
    {
        SetTabSelected(assemblyTab, activeScreen == GarageScreen.Assembly);
        SetTabSelected(galleryTab, activeScreen == GarageScreen.Gallery);
        SetTabSelected(partsTab, activeScreen == GarageScreen.Parts);
    }

    private static void SetTabSelected(Button button, bool selected)
    {
        button.EnableInClassList("top-tab--selected", selected);
    }

    private void PopulateLeftRail()
    {
        leftFilters.Clear();
        leftList.Clear();

        switch (activeScreen)
        {
            case GarageScreen.Assembly:
                leftTitle.text = "SAHİP OLUNAN ARAÇLAR";
                contextLabel.text = "Montaj tezgâhı · parçalar burada takılır";
                PopulateOwnedVehicles();
                break;

            case GarageScreen.Gallery:
                leftTitle.text = "ARAÇ GALERİSİ";
                contextLabel.text = "Galeride aracı incele · satın alma equip etmez";
                PopulateVehicleCatalog();
                break;

            case GarageScreen.Parts:
                leftTitle.text = "PARÇA DÜKKÂNI";
                contextLabel.text = "Parçayı araçta ve statlarda canlı önizle";
                PopulatePartFilters();
                PopulatePartCatalog();
                break;
        }
    }

    private void PopulateOwnedVehicles()
    {
        IReadOnlyList<GarageVehicleDefinition> vehicles = buildState.Catalog.Vehicles;
        for (int i = 0; i < vehicles.Count; i++)
        {
            GarageVehicleDefinition vehicle = vehicles[i];
            if (!buildState.IsVehicleOwned(vehicle))
            {
                continue;
            }

            Button button = CreateListButton(
                vehicle.DisplayName,
                vehicle == buildState.SelectedVehicle);
            button.clicked += () => buildState.SelectOwnedVehicle(vehicle);
            leftList.Add(button);
        }
    }

    private void PopulateVehicleCatalog()
    {
        IReadOnlyList<GarageVehicleDefinition> vehicles = buildState.Catalog.Vehicles;
        for (int i = 0; i < vehicles.Count; i++)
        {
            GarageVehicleDefinition vehicle = vehicles[i];
            bool selected = vehicle == buildState.DisplayedVehicle;
            string suffix = buildState.IsVehicleOwned(vehicle)
                ? " · SAHİP"
                : $" · {vehicle.Price:N0} HURDA";
            Button button = CreateListButton(vehicle.DisplayName + suffix, selected);
            button.clicked += () => buildState.PreviewVehicle(vehicle);
            leftList.Add(button);
        }
    }

    private void PopulatePartFilters()
    {
        GarageVehicleDefinition vehicle = buildState.DisplayedVehicle;
        List<GarageAttachmentSlot> compatibleSlots = new();
        foreach (GarageAttachmentSlot slot in Enum.GetValues(typeof(GarageAttachmentSlot)))
        {
            if (VehicleSupportsSlot(vehicle, slot))
            {
                compatibleSlots.Add(slot);
            }
        }

        if (compatibleSlots.Count > 0 && !compatibleSlots.Contains(partsFilter))
        {
            partsFilter = compatibleSlots[0];
        }

        for (int i = 0; i < compatibleSlots.Count; i++)
        {
            GarageAttachmentSlot slot = compatibleSlots[i];
            string label = GetSlotLabel(slot);
            Button button = new Button(() =>
            {
                partsFilter = slot;
                buildState.PreviewPart(null);
                Refresh();
            })
            {
                text = label
            };
            button.AddToClassList("filter-chip");
            button.EnableInClassList("filter-chip--selected", partsFilter == slot);
            leftFilters.Add(button);
        }
    }

    private void PopulatePartCatalog()
    {
        GarageVehicleDefinition vehicle = buildState.DisplayedVehicle;
        IReadOnlyList<GarageAttachmentDefinition> attachments =
            buildState.Catalog.Attachments;
        bool addedPart = false;

        for (int i = 0; i < attachments.Count; i++)
        {
            GarageAttachmentDefinition attachment = attachments[i];
            if (attachment == null
                || attachment.Slot != partsFilter
                || vehicle == null
                || !attachment.TryGetPose(vehicle.VehicleId, out _))
            {
                continue;
            }

            bool selected = attachment == buildState.PreviewAttachment;
            string suffix = buildState.IsAttachmentOwned(attachment)
                ? " · SAHİP"
                : $" · {attachment.Price:N0} HURDA";
            Button button = CreateListButton(
                attachment.DisplayName + suffix,
                selected);
            button.clicked += () => buildState.PreviewPart(attachment);
            leftList.Add(button);
            addedPart = true;
        }

        if (!addedPart)
        {
            Label emptyState = new Label(
                vehicle != null
                    ? "Bu araç için bu kategoride uyumlu parça yok."
                    : "Önce bir araç seç.");
            emptyState.AddToClassList("empty-state");
            leftList.Add(emptyState);
        }
    }

    private void PopulateRightRail()
    {
        rightList.Clear();
        contextAction.clicked -= HandleContextAction;

        switch (activeScreen)
        {
            case GarageScreen.Assembly:
                PopulateEquippedSlots();
                break;

            case GarageScreen.Gallery:
                PopulateVehicleDetails();
                break;

            case GarageScreen.Parts:
                PopulatePartDetails();
                break;
        }
    }

    private void PopulateEquippedSlots()
    {
        detailMechanics.style.display = DisplayStyle.None;
        rightTitle.text = "TAKILI PARÇALAR";
        detailTitle.text = buildState.SelectedVehicle != null
            ? buildState.SelectedVehicle.DisplayName
            : "Araç seçilmedi";
        detailDescription.text = "Sahip olunan uyumlu parçalar Montaj ekranında değiştirilir.";
        contextAction.text = "2:00 GÖREVE ÇIK";
        contextAction.SetEnabled(buildState.SelectedVehicle != null);
        contextAction.clicked += HandleContextAction;
        contextHint.text = "Görev seçili build ile başlar.";

        foreach (GarageAttachmentSlot slot in Enum.GetValues(typeof(GarageAttachmentSlot)))
        {
            if (!VehicleSupportsSlot(buildState.SelectedVehicle, slot))
            {
                continue;
            }

            GarageAttachmentDefinition equipped = buildState.GetEquipped(slot);
            VisualElement row = new VisualElement();
            row.AddToClassList("slot-row");

            VisualElement copy = new VisualElement();
            copy.AddToClassList("slot-copy");
            Label slotLabel = new Label(GetSlotLabel(slot));
            slotLabel.AddToClassList("slot-label");
            Label value = new Label(equipped != null ? equipped.DisplayName : "Stok");
            value.AddToClassList("slot-value");
            copy.Add(slotLabel);
            copy.Add(value);
            if (equipped != null
                && !string.IsNullOrWhiteSpace(
                    equipped.GameplayEffectSummary))
            {
                Label effect = new Label(equipped.GameplayEffectSummary);
                effect.AddToClassList("slot-effect");
                copy.Add(effect);
            }

            Button change = new Button(() =>
            {
                partsFilter = slot;
                SwitchScreen(GarageScreen.Parts);
            })
            {
                text = "DEĞİŞTİR"
            };
            change.AddToClassList("ghost-button");

            VisualElement actions = new VisualElement();
            actions.AddToClassList("slot-actions");
            actions.Add(change);
            if (equipped != null)
            {
                Button remove = new Button(() => buildState.Unequip(slot))
                {
                    text = "ÇIKAR"
                };
                remove.AddToClassList("ghost-button");
                remove.AddToClassList("ghost-button--danger");
                actions.Add(remove);
            }

            row.Add(copy);
            row.Add(actions);
            rightList.Add(row);
        }
    }

    private bool VehicleSupportsSlot(
        GarageVehicleDefinition vehicle,
        GarageAttachmentSlot slot)
    {
        if (vehicle == null || buildState.Catalog == null)
        {
            return false;
        }

        IReadOnlyList<GarageAttachmentDefinition> attachments =
            buildState.Catalog.Attachments;
        for (int i = 0; i < attachments.Count; i++)
        {
            GarageAttachmentDefinition attachment = attachments[i];
            if (attachment != null
                && attachment.Slot == slot
                && attachment.TryGetPose(vehicle.VehicleId, out _))
            {
                return true;
            }
        }

        return false;
    }

    private void PopulateVehicleDetails()
    {
        detailMechanics.style.display = DisplayStyle.None;
        rightTitle.text = "ARAÇ DETAYI";
        GarageVehicleDefinition vehicle = buildState.DisplayedVehicle;
        detailTitle.text = vehicle != null ? vehicle.DisplayName : "Araç seç";
        detailDescription.text = vehicle != null ? vehicle.Description : string.Empty;

        bool owned = buildState.IsVehicleOwned(vehicle);
        contextAction.text = owned
            ? "ARACI SEÇ"
            : vehicle != null
                ? $"SATIN AL · {vehicle.Price:N0} HURDA"
                : "ARAÇ SEÇ";
        contextAction.SetEnabled(
            vehicle != null && (owned || economy.CanAfford(vehicle.Price)));
        contextAction.clicked += HandleContextAction;
        contextHint.text = owned
            ? "Seçim Montaj ekranındaki aktif aracı değiştirir."
            : vehicle != null && economy.CanAfford(vehicle.Price)
                ? "Satın alma aracı envantere ekler; otomatik seçmez."
                : "Bu araç için yeterli Hurda yok.";
    }

    private void PopulatePartDetails()
    {
        detailMechanics.style.display = DisplayStyle.Flex;
        rightTitle.text = "PARÇA DETAYI";
        GarageVehicleDefinition vehicle = buildState.DisplayedVehicle;
        GarageAttachmentDefinition attachment = buildState.PreviewAttachment;
        detailTitle.text = attachment != null ? attachment.DisplayName : "Parça seç";
        detailDescription.text =
            attachment != null ? attachment.Description : "Uyumlu parçayı soldan seç.";
        detailEffect.text = attachment != null
            ? attachment.GameplayEffectSummary
            : "Parçanın gerçek pasif etkisi burada görünür.";
        detailTradeoff.text = attachment != null
            ? attachment.TradeoffSummary
            : "Her avantajın sürüş veya dayanıklılık bedeli burada görünür.";

        bool vehicleOwned = buildState.IsVehicleOwned(vehicle);
        bool vehicleSelected = vehicle != null
            && vehicle == buildState.SelectedVehicle;
        bool owned = buildState.IsAttachmentOwned(attachment);

        if (attachment == null)
        {
            contextAction.text = "PARÇA SEÇ";
            contextAction.SetEnabled(false);
            contextHint.text = "İncelemek istediğin uyumlu parçayı seç.";
        }
        else if (!vehicleOwned)
        {
            contextAction.text = "ÖNCE ARACI SATIN AL";
            contextAction.SetEnabled(false);
            contextHint.text =
                $"Bu parçayı satın almak veya takmak için önce {vehicle.DisplayName} aracına sahip ol.";
        }
        else if (!vehicleSelected)
        {
            contextAction.text = "ÖNCE ARACI SEÇ";
            contextAction.SetEnabled(false);
            contextHint.text =
                "Satın alma ve montaj için önce galeriden bu aracı aktif araç olarak seç.";
        }
        else
        {
            contextAction.text = owned
                ? "MONTAJDA TAK"
                : $"SATIN AL · {attachment.Price:N0} HURDA";
            contextAction.SetEnabled(
                owned || economy.CanAfford(attachment.Price));
            contextHint.text = owned
                ? "Takıldığında önizlenen statlar aktif build olur."
                : economy.CanAfford(attachment.Price)
                    ? "Satın alma parçayı envantere ekler; montaj ayrı yapılır."
                    : "Bu parça için yeterli Hurda yok.";
        }

        contextAction.clicked += HandleContextAction;
    }

    private void HandleContextAction()
    {
        switch (activeScreen)
        {
            case GarageScreen.Assembly:
                MissionRequested?.Invoke();
                break;

            case GarageScreen.Gallery:
                GarageVehicleDefinition vehicle = buildState.DisplayedVehicle;
                if (buildState.IsVehicleOwned(vehicle)
                    && buildState.SelectOwnedVehicle(vehicle))
                {
                    activeScreen = GarageScreen.Assembly;
                    Refresh();
                }
                else if (economy.TryPurchaseVehicle(vehicle))
                {
                    Refresh();
                }
                break;

            case GarageScreen.Parts:
                GarageVehicleDefinition displayedVehicle =
                    buildState.DisplayedVehicle;
                if (displayedVehicle == null
                    || displayedVehicle != buildState.SelectedVehicle
                    || !buildState.IsVehicleOwned(displayedVehicle))
                {
                    return;
                }

                GarageAttachmentDefinition attachment =
                    buildState.PreviewAttachment;
                if (buildState.IsAttachmentOwned(attachment)
                    && buildState.EquipPreviewPart())
                {
                    activeScreen = GarageScreen.Assembly;
                    Refresh();
                }
                else if (economy.TryPurchaseAttachment(attachment))
                {
                    Refresh();
                }
                break;
        }
    }

    private void CreateStatElements()
    {
        statGrid.Clear();
        statElements.Clear();

        GarageVehicleStat[] orderedStats =
            GarageVehicleStatPresentation.OrderedStats;
        for (int i = 0; i < orderedStats.Length; i++)
        {
            GarageVehicleStat stat = orderedStats[i];
            VisualElement card = new VisualElement();
            card.AddToClassList("stat-card");

            Label name = new Label(GarageVehicleStatPresentation.GetTurkishLabel(stat));
            name.AddToClassList("stat-name");

            VisualElement valueRow = new VisualElement();
            valueRow.AddToClassList("stat-value-row");
            Label value = new Label();
            value.AddToClassList("stat-value");
            Label delta = new Label();
            delta.AddToClassList("stat-delta");
            valueRow.Add(value);
            valueRow.Add(delta);

            VisualElement track = new VisualElement();
            track.AddToClassList("stat-track");
            VisualElement currentFill = new VisualElement();
            currentFill.AddToClassList("stat-current-fill");
            VisualElement previewFill = new VisualElement();
            previewFill.AddToClassList("stat-preview-fill");
            track.Add(currentFill);
            track.Add(previewFill);

            card.Add(name);
            card.Add(valueRow);
            card.Add(track);
            statGrid.Add(card);

            statElements.Add(new StatElements
            {
                stat = stat,
                value = value,
                delta = delta,
                currentFill = currentFill,
                previewFill = previewFill
            });
        }
    }

    private void UpdateStats()
    {
        VehicleStats current = activeScreen == GarageScreen.Parts
            ? buildState.DisplayedCurrentStats
            : buildState.CurrentStats;
        VehicleStats preview = buildState.PreviewStats;

        for (int i = 0; i < statElements.Count; i++)
        {
            StatElements elements = statElements[i];
            float currentValue = current.GetValue(elements.stat);
            float previewValue = preview.GetValue(elements.stat);
            float delta = previewValue - currentValue;
            float displayMaximum =
                GarageVehicleStatPresentation.GetDisplayMaximum(elements.stat);

            elements.value.text =
                $"{GarageVehicleStatPresentation.FormatValue(elements.stat, currentValue)}"
                + (Mathf.Abs(delta) > 0.0001f
                    ? $"  →  {GarageVehicleStatPresentation.FormatValue(elements.stat, previewValue)}"
                    : string.Empty);
            elements.delta.text = Mathf.Abs(delta) > 0.0001f
                ? GarageVehicleStatPresentation.FormatDelta(elements.stat, delta)
                : "—";

            elements.currentFill.style.width =
                Length.Percent(Mathf.Clamp01(currentValue / displayMaximum) * 100f);
            elements.previewFill.style.width =
                Length.Percent(Mathf.Clamp01(previewValue / displayMaximum) * 100f);

            elements.delta.EnableInClassList("stat-positive", delta > 0.0001f);
            elements.delta.EnableInClassList("stat-negative", delta < -0.0001f);
            elements.previewFill.EnableInClassList(
                "stat-preview-fill--positive",
                delta > 0.0001f);
            elements.previewFill.EnableInClassList(
                "stat-preview-fill--negative",
                delta < -0.0001f);
        }
    }

    private void UpdatePreview()
    {
        GarageVehicleDefinition displayedVehicle = buildState.DisplayedVehicle;
        bool showEquipped = displayedVehicle == buildState.SelectedVehicle;
        previewController.SetBuild(
            displayedVehicle,
            buildState.GetEquippedAttachments(),
            buildState.PreviewAttachment,
            showEquipped);
    }

    private void UpdateSelectedBuild()
    {
        GarageVehicleDefinition vehicle = buildState.SelectedVehicle;
        if (vehicle == null)
        {
            selectedBuildLabel.text = "Seçili build yok";
            missionButton.SetEnabled(false);
            return;
        }

        List<string> partNames = new();
        foreach (GarageAttachmentDefinition attachment in buildState.GetEquippedAttachments())
        {
            partNames.Add(attachment.DisplayName);
        }

        string parts = partNames.Count > 0
            ? string.Join(" · ", partNames)
            : "Stok";
        selectedBuildLabel.text = $"SEÇİLİ BUILD  ·  {vehicle.DisplayName}  ·  {parts}";
        missionButton.SetEnabled(true);
    }

    private static Button CreateListButton(string text, bool selected)
    {
        Button button = new Button
        {
            text = text
        };
        button.AddToClassList("rail-item");
        button.EnableInClassList("rail-item--selected", selected);
        return button;
    }

    private void OnPreviewPointerDown(PointerDownEvent evt)
    {
        if (evt.button != 0)
        {
            return;
        }

        pointerDragging = true;
        previousPointerPosition = evt.position;
        previewViewport.CapturePointer(evt.pointerId);
        previewController.BeginDrag();
        evt.StopPropagation();
    }

    private void OnPreviewPointerMove(PointerMoveEvent evt)
    {
        if (!pointerDragging || !previewViewport.HasPointerCapture(evt.pointerId))
        {
            return;
        }

        Vector2 current = evt.position;
        previewController.RotateByPointerDelta(current.x - previousPointerPosition.x);
        previousPointerPosition = current;
        evt.StopPropagation();
    }

    private void OnPreviewPointerUp(PointerUpEvent evt)
    {
        if (!pointerDragging || evt.button != 0)
        {
            return;
        }

        if (previewViewport.HasPointerCapture(evt.pointerId))
        {
            previewViewport.ReleasePointer(evt.pointerId);
        }

        EndPreviewDrag();
        evt.StopPropagation();
    }

    private void EndPreviewDrag()
    {
        pointerDragging = false;
        previewController.EndDrag();
    }

    private static string GetSlotLabel(GarageAttachmentSlot slot)
    {
        return slot switch
        {
            GarageAttachmentSlot.Front => "ÖN PARÇA",
            GarageAttachmentSlot.Armor => "ZIRH",
            GarageAttachmentSlot.Engine => "MOTOR",
            GarageAttachmentSlot.Wheels => "TEKERLEK",
            GarageAttachmentSlot.RearAero => "ARKA / AERO",
            GarageAttachmentSlot.RoofUtility => "TAVAN / EKİPMAN",
            _ => slot.ToString().ToUpperInvariant()
        };
    }

    private static T RequireElement<T>(VisualElement root, string name)
        where T : VisualElement
    {
        T element = root.Q<T>(name);
        if (element == null)
        {
            throw new InvalidOperationException(
                $"Garage UI element '{name}' ({typeof(T).Name}) was not found.");
        }

        return element;
    }
}
