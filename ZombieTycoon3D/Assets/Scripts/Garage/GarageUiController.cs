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
    private const float MissionDamagePulseDuration = 0.18f;
    private const string MasterVolumeKey = "zt3d.settings.master-volume";
    private const float SettingsSaveDelay = 0.35f;

    private enum GarageScreen
    {
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
    private readonly Dictionary<GarageAttachmentSlot, Button> partHotspots = new();
    private readonly Dictionary<GarageAttachmentSlot, GarageAttachmentDefinition>
        partHotspotAttachments = new();
    private GarageScreen activeScreen = GarageScreen.Gallery;
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
    private VisualElement missionRunStatus;
    private VisualElement missionSpeedometer;
    private VisualElement missionSpeedNeedle;
    private Label missionCurrentSpeed;
    private Label missionHealth;
    private readonly VisualElement[] missionHealthSegments =
        new VisualElement[MissionHealthSegmentCount];
    private VisualElement missionResult;
    private VisualElement missionIntro;
    private Label missionIntroVehicle;
    private Label missionIntroCountdown;
    private Button missionPauseButton;
    private VisualElement missionPause;
    private Label pauseTimer;
    private Label pauseKills;
    private Label pauseScore;
    private Button pauseResumeButton;
    private Button pauseRestartButton;
    private Button pauseGarageButton;
    private Button pauseSettingsButton;
    private VisualElement settingsOverlay;
    private Button garageSettingsButton;
    private Slider masterVolume;
    private Label masterVolumeValue;
    private Button fullscreenButton;
    private Button settingsCloseButton;
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
    private Button galleryTab;
    private Button partsTab;
    private VisualElement leftFilters;
    private Label contextLabel;
    private VisualElement statGrid;
    private Button carouselPrev;
    private Button carouselNext;
    private Label carouselTitle;
    private Label carouselMeta;
    private Label detailTitle;
    private Label detailDescription;
    private VisualElement detailMechanics;
    private Label detailEffect;
    private Label detailTradeoff;
    private Button contextAction;
    private Label contextHint;
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
    private bool missionDamagePulsing;
    private float missionDamagePulseEndTime;
    private int displayedMissionSeconds = int.MinValue;
    private int displayedMissionSpeed = int.MinValue;
    private int displayedMissionHealth = int.MinValue;
    private int displayedMissionMaxHealth = int.MinValue;
    private float displayedMissionHealthRaw = float.NaN;
    private float missionGaugeMaximumSpeed = 120f;
    private float targetMissionSpeed;
    private float displayedMissionNeedleSpeed;
    private float missionSpeedNeedleVelocity;
    private bool missionSpeedAnimationEnabled;
    private bool settingsSavePending;
    private bool settingsVisible;
    private float settingsSaveTime;

    public event Action MissionRequested;
    public event Action ResultAcknowledged;
    public event Action MissionPauseRequested;
    public event Action MissionResumeRequested;
    public event Action MissionRestartRequested;
    public event Action MissionGarageRequested;

    public bool SettingsVisible => settingsVisible;

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
        LoadSettings();
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

        if (settingsSavePending)
        {
            PlayerPrefs.Save();
            settingsSavePending = false;
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

        if (missionDamagePulsing
            && currentTime >= missionDamagePulseEndTime)
        {
            missionDamagePulsing = false;
            missionRunStatus.RemoveFromClassList(
                "mission-run-status--damage");
        }

        if (missionSpeedAnimationEnabled)
        {
            AnimateMissionSpeedometer();
        }

        if (settingsSavePending && currentTime >= settingsSaveTime)
        {
            PlayerPrefs.Save();
            settingsSavePending = false;
        }

        UpdatePartHotspotPositions();
    }

    public void ShowGarage()
    {
        garageRoot.style.display = DisplayStyle.Flex;
        missionHud.style.display = DisplayStyle.None;
        missionResult.style.display = DisplayStyle.None;
        missionIntro.style.display = DisplayStyle.None;
        missionPause.style.display = DisplayStyle.None;
        missionPauseButton.style.display = DisplayStyle.None;
        CloseSettingsPanel();
        HideMayhemAnnouncement();
        missionSpeedAnimationEnabled = false;
        ResetMissionDamagePulse();
        previewController.SetVisible(true);
        activeScreen = GarageScreen.Gallery;
        buildState.ClearPreview();
        Refresh();
    }

    public void HideGarageForMission()
    {
        garageRoot.style.display = DisplayStyle.None;
        missionHud.style.display = DisplayStyle.Flex;
        missionResult.style.display = DisplayStyle.None;
        missionPause.style.display = DisplayStyle.None;
        missionPauseButton.style.display = DisplayStyle.None;
        CloseSettingsPanel();
        HideMayhemAnnouncement();
        missionSpeedAnimationEnabled = true;
        previewController.SetVisible(false);
    }

    public void ShowMissionIntro(string vehicleName)
    {
        missionIntroVehicle.text = string.IsNullOrWhiteSpace(vehicleName)
            ? "ARAÇ"
            : vehicleName.ToUpperInvariant();
        missionIntroCountdown.text = "3";
        missionIntro.style.display = DisplayStyle.Flex;
        missionPauseButton.style.display = DisplayStyle.None;
    }

    public void UpdateMissionIntroCountdown(string value)
    {
        if (missionIntroCountdown != null)
        {
            missionIntroCountdown.text = value;
        }
    }

    public void CompleteMissionIntro()
    {
        missionIntro.style.display = DisplayStyle.None;
        missionPauseButton.style.display = DisplayStyle.Flex;
    }

    public void ShowMissionPause(
        float remainingSeconds,
        MissionProgress progress)
    {
        int seconds = Mathf.Max(0, Mathf.CeilToInt(remainingSeconds));
        pauseTimer.text = $"{seconds / 60:0}:{seconds % 60:00}";
        pauseKills.text = $"{progress.Kills} / {progress.KillTarget}";
        pauseScore.text = progress.Score.ToString("N0");
        missionPause.style.display = DisplayStyle.Flex;
        missionPauseButton.style.display = DisplayStyle.None;
    }

    public void HideMissionPause()
    {
        missionPause.style.display = DisplayStyle.None;
        missionPauseButton.style.display = DisplayStyle.Flex;
        CloseSettingsPanel();
    }

    public void CloseSettingsPanel()
    {
        if (settingsOverlay == null)
        {
            return;
        }

        settingsOverlay.style.display = DisplayStyle.None;
        settingsVisible = false;
        if (settingsSavePending)
        {
            PlayerPrefs.Save();
            settingsSavePending = false;
        }
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
        displayedMissionHealthRaw = float.NaN;
        ResetMissionDamagePulse();
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
        if (!float.IsNaN(displayedMissionHealthRaw)
            && safeCurrent < displayedMissionHealthRaw - 0.0001f)
        {
            missionRunStatus.AddToClassList(
                "mission-run-status--damage");
            missionDamagePulsing = true;
            missionDamagePulseEndTime =
                Time.unscaledTime + MissionDamagePulseDuration;
        }

        displayedMissionHealthRaw = safeCurrent;
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
        missionIntro.style.display = DisplayStyle.None;
        missionPause.style.display = DisplayStyle.None;
        missionPauseButton.style.display = DisplayStyle.None;
        CloseSettingsPanel();
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
        missionRunStatus =
            RequireElement<VisualElement>(root, "mission-run-status");
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
        missionIntro = RequireElement<VisualElement>(root, "mission-intro");
        missionIntroVehicle =
            RequireElement<Label>(root, "mission-intro-vehicle");
        missionIntroCountdown =
            RequireElement<Label>(root, "mission-intro-countdown");
        missionPauseButton =
            RequireElement<Button>(root, "mission-pause-button");
        missionPause =
            RequireElement<VisualElement>(root, "mission-pause");
        pauseTimer = RequireElement<Label>(root, "pause-timer");
        pauseKills = RequireElement<Label>(root, "pause-kills");
        pauseScore = RequireElement<Label>(root, "pause-score");
        pauseResumeButton =
            RequireElement<Button>(root, "pause-resume-button");
        pauseRestartButton =
            RequireElement<Button>(root, "pause-restart-button");
        pauseGarageButton =
            RequireElement<Button>(root, "pause-garage-button");
        pauseSettingsButton =
            RequireElement<Button>(root, "pause-settings-button");
        settingsOverlay =
            RequireElement<VisualElement>(root, "settings-overlay");
        garageSettingsButton =
            RequireElement<Button>(root, "garage-settings-button");
        masterVolume = RequireElement<Slider>(root, "master-volume");
        masterVolumeValue =
            RequireElement<Label>(root, "master-volume-value");
        fullscreenButton =
            RequireElement<Button>(root, "fullscreen-button");
        settingsCloseButton =
            RequireElement<Button>(root, "settings-close-button");
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
        galleryTab = RequireElement<Button>(root, "gallery-tab");
        partsTab = RequireElement<Button>(root, "parts-tab");
        leftFilters = RequireElement<VisualElement>(root, "left-filters");
        contextLabel = RequireElement<Label>(root, "context-label");
        statGrid = RequireElement<VisualElement>(root, "stat-grid");
        carouselPrev = RequireElement<Button>(root, "carousel-prev");
        carouselNext = RequireElement<Button>(root, "carousel-next");
        carouselTitle = RequireElement<Label>(root, "carousel-title");
        carouselMeta = RequireElement<Label>(root, "carousel-meta");
        detailTitle = RequireElement<Label>(root, "detail-title");
        detailDescription = RequireElement<Label>(root, "detail-description");
        detailMechanics =
            RequireElement<VisualElement>(root, "detail-mechanics");
        detailEffect = RequireElement<Label>(root, "detail-effect");
        detailTradeoff = RequireElement<Label>(root, "detail-tradeoff");
        contextAction = RequireElement<Button>(root, "context-action");
        contextHint = RequireElement<Label>(root, "context-hint");
        balanceValue = RequireElement<Label>(root, "balance-value");
        missionButton = RequireElement<Button>(root, "mission-button");
        previewViewport = RequireElement<VisualElement>(root, "preview-viewport");
        missionEffectFeedback =
            RequireElement<Label>(root, "mission-effect-feedback");

        galleryTab.clicked += () => SwitchScreen(GarageScreen.Gallery);
        partsTab.clicked += () => SwitchScreen(GarageScreen.Parts);
        carouselPrev.clicked += () => CycleShowroom(-1);
        carouselNext.clicked += () => CycleShowroom(1);
        missionButton.clicked += () => MissionRequested?.Invoke();
        resultButton.clicked += () => ResultAcknowledged?.Invoke();
        missionPauseButton.clicked +=
            () => MissionPauseRequested?.Invoke();
        pauseResumeButton.clicked +=
            () => MissionResumeRequested?.Invoke();
        pauseRestartButton.clicked +=
            () => MissionRestartRequested?.Invoke();
        pauseGarageButton.clicked +=
            () => MissionGarageRequested?.Invoke();
        garageSettingsButton.clicked += OpenSettingsPanel;
        pauseSettingsButton.clicked += OpenSettingsPanel;
        settingsCloseButton.clicked += CloseSettingsPanel;
        fullscreenButton.clicked += ToggleFullscreen;
        masterVolume.RegisterValueChangedCallback(
            HandleMasterVolumeChanged);

        previewViewport.RegisterCallback<PointerDownEvent>(OnPreviewPointerDown);
        previewViewport.RegisterCallback<PointerMoveEvent>(OnPreviewPointerMove);
        previewViewport.RegisterCallback<PointerUpEvent>(OnPreviewPointerUp);
        previewViewport.RegisterCallback<PointerCaptureOutEvent>(_ => EndPreviewDrag());

        CreateStatElements();
    }

    private void LoadSettings()
    {
        float volume = Mathf.Clamp01(
            PlayerPrefs.GetFloat(MasterVolumeKey, 0.8f));
        AudioListener.volume = volume;
        masterVolume.SetValueWithoutNotify(volume);
        UpdateVolumeLabel(volume);
        UpdateFullscreenButton();
        settingsOverlay.style.display = DisplayStyle.None;
    }

    private void OpenSettingsPanel()
    {
        UpdateFullscreenButton();
        settingsOverlay.style.display = DisplayStyle.Flex;
        settingsVisible = true;
    }

    private void HandleMasterVolumeChanged(ChangeEvent<float> evt)
    {
        float volume = Mathf.Clamp01(evt.newValue);
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(MasterVolumeKey, volume);
        UpdateVolumeLabel(volume);
        settingsSavePending = true;
        settingsSaveTime = Time.unscaledTime + SettingsSaveDelay;
    }

    private void UpdateVolumeLabel(float volume)
    {
        masterVolumeValue.text =
            $"{Mathf.RoundToInt(Mathf.Clamp01(volume) * 100f)}%";
    }

    private void ToggleFullscreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
        UpdateFullscreenButton();
    }

    private void UpdateFullscreenButton()
    {
        fullscreenButton.text = Screen.fullScreen
            ? "TAM EKRANDAN ÇIK"
            : "TAM EKRAN";
    }

    private void ResetMissionDamagePulse()
    {
        missionDamagePulsing = false;
        missionDamagePulseEndTime = 0f;
        missionRunStatus?.RemoveFromClassList(
            "mission-run-status--damage");
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
        activeScreen = screen;
        if (screen == GarageScreen.Parts)
        {
            NormalizePartsFilter(buildState.DisplayedVehicle);
            SelectPartForSlot(partsFilter);
        }
        else
        {
            buildState.PreviewPart(null);
        }
    }

    private void Refresh()
    {
        if (garageRoot == null || buildState.Catalog == null)
        {
            return;
        }

        balanceValue.text = $"{economy.Scrap:N0} HURDA";
        UpdateTabs();
        PopulateScreen();
        PopulateContextDrawer();
        UpdateStats();
        UpdatePreview();
        UpdateCarousel();
        UpdateMissionButton();
    }

    private void UpdateTabs()
    {
        SetTabSelected(galleryTab, activeScreen == GarageScreen.Gallery);
        SetTabSelected(partsTab, activeScreen == GarageScreen.Parts);
        garageRoot.EnableInClassList(
            "garage-screen--gallery",
            activeScreen == GarageScreen.Gallery);
        garageRoot.EnableInClassList(
            "garage-screen--parts",
            activeScreen == GarageScreen.Parts);
    }

    private static void SetTabSelected(Button button, bool selected)
    {
        button.EnableInClassList("top-tab--selected", selected);
    }

    private void PopulateScreen()
    {
        leftFilters.Clear();
        partHotspots.Clear();
        partHotspotAttachments.Clear();

        if (activeScreen == GarageScreen.Gallery)
        {
            contextLabel.text = "Aracı yakından incele · oklarla vitrini değiştir";
            return;
        }

        contextLabel.text = "Montaj noktasını seç · oklarla uyumlu parçaları incele";
        NormalizePartsFilter(buildState.DisplayedVehicle);
        PopulatePartFilters();
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
            GarageAttachmentDefinition hotspotAttachment =
                FindCompatibleAttachment(vehicle, slot);
            Button button = new Button(() =>
            {
                partsFilter = slot;
                SelectPartForSlot(slot);
            })
            {
                text = GetHotspotLabel(slot),
                tooltip = GetSlotLabel(slot)
            };
            button.AddToClassList("filter-chip");
            button.EnableInClassList("filter-chip--selected", partsFilter == slot);
            leftFilters.Add(button);
            partHotspots[slot] = button;
            if (hotspotAttachment != null)
            {
                partHotspotAttachments[slot] = hotspotAttachment;
            }
        }

        UpdatePartHotspotPositions();
    }

    private GarageAttachmentDefinition FindCompatibleAttachment(
        GarageVehicleDefinition vehicle,
        GarageAttachmentSlot slot)
    {
        if (vehicle == null || buildState.Catalog == null)
        {
            return null;
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
                return attachment;
            }
        }

        return null;
    }

    private List<GarageAttachmentDefinition> GetCompatibleAttachments(
        GarageVehicleDefinition vehicle,
        GarageAttachmentSlot slot)
    {
        List<GarageAttachmentDefinition> compatible = new();
        if (vehicle == null || buildState.Catalog == null)
        {
            return compatible;
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
                compatible.Add(attachment);
            }
        }

        return compatible;
    }

    private void NormalizePartsFilter(GarageVehicleDefinition vehicle)
    {
        if (VehicleSupportsSlot(vehicle, partsFilter))
        {
            return;
        }

        foreach (GarageAttachmentSlot slot in Enum.GetValues(
                     typeof(GarageAttachmentSlot)))
        {
            if (VehicleSupportsSlot(vehicle, slot))
            {
                partsFilter = slot;
                return;
            }
        }
    }

    private void SelectPartForSlot(GarageAttachmentSlot slot)
    {
        GarageVehicleDefinition vehicle = buildState.DisplayedVehicle;
        GarageAttachmentDefinition candidate = null;
        if (vehicle != null && vehicle == buildState.SelectedVehicle)
        {
            GarageAttachmentDefinition equipped = buildState.GetEquipped(slot);
            if (equipped != null
                && equipped.TryGetPose(vehicle.VehicleId, out _))
            {
                candidate = equipped;
            }
        }

        candidate ??= FindCompatibleAttachment(vehicle, slot);
        buildState.PreviewPart(candidate);
    }

    private void UpdatePartHotspotPositions()
    {
        if (activeScreen != GarageScreen.Parts
            || previewController == null
            || !previewController.IsVisible
            || partHotspots.Count == 0)
        {
            return;
        }

        foreach (KeyValuePair<GarageAttachmentSlot, Button> pair in partHotspots)
        {
            if (!partHotspotAttachments.TryGetValue(
                    pair.Key,
                    out GarageAttachmentDefinition attachment)
                || !previewController.TryGetAttachmentViewportPosition(
                    attachment,
                    out Vector2 viewportPosition))
            {
                continue;
            }

            pair.Value.style.left = Length.Percent(
                Mathf.Clamp(viewportPosition.x * 100f, 16f, 84f));
            pair.Value.style.top = Length.Percent(
                Mathf.Clamp((1f - viewportPosition.y) * 100f, 15f, 76f));
        }
    }

    private void CycleShowroom(int direction)
    {
        if (activeScreen == GarageScreen.Gallery)
        {
            IReadOnlyList<GarageVehicleDefinition> vehicles =
                buildState.Catalog.Vehicles;
            if (vehicles.Count == 0)
            {
                return;
            }

            int currentIndex = IndexOfVehicle(
                vehicles,
                buildState.DisplayedVehicle);
            int nextIndex = WrapIndex(currentIndex + direction, vehicles.Count);
            buildState.PreviewVehicle(vehicles[nextIndex]);
            return;
        }

        List<GarageAttachmentDefinition> attachments =
            GetCompatibleAttachments(buildState.DisplayedVehicle, partsFilter);
        if (attachments.Count == 0)
        {
            buildState.PreviewPart(null);
            return;
        }

        int currentPartIndex = attachments.IndexOf(buildState.PreviewAttachment);
        if (currentPartIndex < 0)
        {
            currentPartIndex = direction > 0 ? -1 : 0;
        }

        int nextPartIndex = WrapIndex(
            currentPartIndex + direction,
            attachments.Count);
        buildState.PreviewPart(attachments[nextPartIndex]);
    }

    private static int IndexOfVehicle(
        IReadOnlyList<GarageVehicleDefinition> vehicles,
        GarageVehicleDefinition vehicle)
    {
        for (int i = 0; i < vehicles.Count; i++)
        {
            if (vehicles[i] == vehicle)
            {
                return i;
            }
        }

        return 0;
    }

    private static int WrapIndex(int index, int count)
    {
        return count > 0 ? (index % count + count) % count : 0;
    }

    private void PopulateContextDrawer()
    {
        contextAction.clicked -= HandleContextAction;

        if (activeScreen == GarageScreen.Gallery)
        {
            PopulateVehicleDetails();
        }
        else
        {
            PopulatePartDetails();
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
        GarageVehicleDefinition vehicle = buildState.DisplayedVehicle;
        detailTitle.text = vehicle != null ? vehicle.DisplayName : "Araç seç";
        detailDescription.text = vehicle != null ? vehicle.Description : string.Empty;

        bool owned = buildState.IsVehicleOwned(vehicle);
        bool selected = vehicle != null && vehicle == buildState.SelectedVehicle;
        contextAction.text = selected
            ? "AKTİF ARAÇ"
            : owned
                ? "ARACI SEÇ"
            : vehicle != null
                ? $"SATIN AL · {vehicle.Price:N0} HURDA"
                : "ARAÇ SEÇ";
        contextAction.SetEnabled(
            vehicle != null
            && !selected
            && (owned || economy.CanAfford(vehicle.Price)));
        contextAction.clicked += HandleContextAction;
        contextHint.text = selected
            ? "Göreve bu araç ve takılı build ile çıkacaksın."
            : owned
                ? "Bu aracı aktif görev aracı olarak seç."
            : vehicle != null && economy.CanAfford(vehicle.Price)
                ? "Satın alma aracı envantere ekler; otomatik seçmez."
                : "Bu araç için yeterli Hurda yok.";
    }

    private void PopulatePartDetails()
    {
        detailMechanics.style.display = DisplayStyle.None;
        GarageVehicleDefinition vehicle = buildState.DisplayedVehicle;
        GarageAttachmentDefinition attachment = buildState.PreviewAttachment;
        detailTitle.text = attachment != null ? attachment.DisplayName : "Uyumlu parça yok";
        detailDescription.text =
            attachment != null
                ? attachment.Description
                : "Bu montaj noktası için uyumlu parça bulunamadı.";
        detailEffect.text = string.Empty;
        detailTradeoff.text = string.Empty;

        bool vehicleOwned = buildState.IsVehicleOwned(vehicle);
        bool vehicleSelected = vehicle != null
            && vehicle == buildState.SelectedVehicle;
        bool owned = buildState.IsAttachmentOwned(attachment);
        bool equipped = attachment != null
            && buildState.GetEquipped(attachment.Slot) == attachment;

        if (attachment == null)
        {
            contextAction.text = "PARÇA SEÇ";
            contextAction.SetEnabled(false);
            contextHint.text = "MONTAJ NOKTASI SEÇ";
        }
        else if (!vehicleOwned)
        {
            contextAction.text = "ÖNCE ARACI SATIN AL";
            contextAction.SetEnabled(false);
            contextHint.text = $"{vehicle.DisplayName.ToUpperInvariant()} GEREKLİ";
        }
        else if (!vehicleSelected)
        {
            contextAction.text = "ÖNCE ARACI SEÇ";
            contextAction.SetEnabled(false);
            contextHint.text = "AKTİF ARAÇ GEREKLİ";
        }
        else
        {
            contextAction.text = equipped
                ? "SÖK"
                : owned
                    ? "TAK"
                : $"SATIN AL · {attachment.Price:N0} HURDA";
            contextAction.SetEnabled(
                equipped || owned || economy.CanAfford(attachment.Price));
            contextHint.text = equipped
                ? "TAKILI"
                : owned
                    ? "ENVANTERDE"
                : economy.CanAfford(attachment.Price)
                    ? "SATIN ALINABİLİR"
                    : "HURDA YETERSİZ";
        }

        contextAction.clicked += HandleContextAction;
    }

    private void HandleContextAction()
    {
        switch (activeScreen)
        {
            case GarageScreen.Gallery:
                GarageVehicleDefinition vehicle = buildState.DisplayedVehicle;
                if (buildState.IsVehicleOwned(vehicle)
                    && buildState.SelectOwnedVehicle(vehicle))
                {
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
                if (attachment == null)
                {
                    return;
                }

                if (buildState.GetEquipped(attachment.Slot) == attachment)
                {
                    if (buildState.Unequip(attachment.Slot))
                    {
                        buildState.PreviewPart(attachment);
                    }
                }
                else if (buildState.IsAttachmentOwned(attachment)
                         && buildState.EquipPreviewPart())
                {
                    buildState.PreviewPart(attachment);
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

            VisualElement header = new VisualElement();
            header.AddToClassList("stat-header");

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

            header.Add(name);
            header.Add(valueRow);
            card.Add(header);
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
        VehicleStats current = buildState.DisplayedCurrentStats;
        VehicleStats preview = buildState.PreviewStats;

        for (int i = 0; i < statElements.Count; i++)
        {
            StatElements elements = statElements[i];
            float currentValue = activeScreen == GarageScreen.Parts
                ? current.GetValue(elements.stat)
                : preview.GetValue(elements.stat);
            float previewValue = preview.GetValue(elements.stat);
            float delta = previewValue - currentValue;

            elements.value.text =
                $"{GarageVehicleStatPresentation.FormatValue(elements.stat, currentValue)}"
                + (Mathf.Abs(delta) > 0.0001f
                    ? $"  →  {GarageVehicleStatPresentation.FormatValue(elements.stat, previewValue)}"
                    : string.Empty);
            elements.delta.text = Mathf.Abs(delta) > 0.0001f
                ? GarageVehicleStatPresentation.FormatDelta(elements.stat, delta)
                : string.Empty;

            elements.delta.EnableInClassList("stat-positive", delta > 0.0001f);
            elements.delta.EnableInClassList("stat-negative", delta < -0.0001f);

            float displayMaximum =
                GarageVehicleStatPresentation.GetDisplayMaximum(elements.stat);
            float currentPercent = displayMaximum > 0f
                ? Mathf.Clamp01(currentValue / displayMaximum) * 100f
                : 0f;
            float previewPercent = displayMaximum > 0f
                ? Mathf.Clamp01(previewValue / displayMaximum) * 100f
                : 0f;
            elements.currentFill.style.width = Length.Percent(currentPercent);
            elements.previewFill.style.width = Length.Percent(previewPercent);
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

    private void UpdateCarousel()
    {
        if (activeScreen == GarageScreen.Gallery)
        {
            IReadOnlyList<GarageVehicleDefinition> vehicles =
                buildState.Catalog.Vehicles;
            GarageVehicleDefinition vehicle = buildState.DisplayedVehicle;
            int index = IndexOfVehicle(vehicles, vehicle);

            carouselTitle.text = vehicles.Count > 0
                ? $"ARAÇ  {index + 1} / {vehicles.Count}"
                : "ARAÇ YOK";
            carouselMeta.text = vehicle == buildState.SelectedVehicle
                ? "AKTİF ARAÇ"
                : buildState.IsVehicleOwned(vehicle)
                    ? "SAHİP"
                    : vehicle != null
                        ? $"{vehicle.Price:N0} HURDA"
                        : string.Empty;
            carouselPrev.SetEnabled(vehicles.Count > 1);
            carouselNext.SetEnabled(vehicles.Count > 1);
            return;
        }

        List<GarageAttachmentDefinition> attachments =
            GetCompatibleAttachments(buildState.DisplayedVehicle, partsFilter);
        GarageAttachmentDefinition attachment = buildState.PreviewAttachment;
        int partIndex = attachments.IndexOf(attachment);
        carouselTitle.text = attachments.Count > 0
            ? $"{GetSlotLabel(partsFilter).ToUpperInvariant()}  {partIndex + 1} / {attachments.Count}"
            : $"{GetSlotLabel(partsFilter).ToUpperInvariant()}  ·  UYUMLU PARÇA YOK";
        carouselMeta.text = attachment != null
            && buildState.GetEquipped(attachment.Slot) == attachment
                ? "TAKILI"
                : buildState.IsAttachmentOwned(attachment)
                    ? "SAHİP"
                    : attachment != null
                        ? $"{attachment.Price:N0} HURDA"
                        : string.Empty;
        carouselPrev.SetEnabled(attachments.Count > 1);
        carouselNext.SetEnabled(attachments.Count > 1);
    }

    private void UpdateMissionButton()
    {
        missionButton.SetEnabled(buildState.SelectedVehicle != null);
    }

    private void OnPreviewPointerDown(PointerDownEvent evt)
    {
        VisualElement target = evt.target as VisualElement;
        if (evt.button != 0
            || target is Button
            || target?.GetFirstAncestorOfType<Button>() != null)
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

    private static string GetHotspotLabel(GarageAttachmentSlot slot)
    {
        return slot switch
        {
            GarageAttachmentSlot.Front => "ÖN",
            GarageAttachmentSlot.Armor => "ZIRH",
            GarageAttachmentSlot.Engine => "MOTOR",
            GarageAttachmentSlot.Wheels => "TEKER",
            GarageAttachmentSlot.RearAero => "ARKA",
            GarageAttachmentSlot.RoofUtility => "TAVAN",
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
