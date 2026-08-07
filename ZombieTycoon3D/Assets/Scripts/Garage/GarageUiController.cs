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
    private const float MissionCounterPulseDuration = 0.24f;
    private const float MissionFeedbackPulseDuration = 0.18f;
    private const float MissionIntroCountdownPulseDuration = 0.24f;
    private const float MissionMayhemCardPulseDuration = 0.24f;
    private const float MissionMayhemFillSmoothTime = 0.10f;
    private const float MayhemAnnouncementDuration = 0.72f;
    private const float MayhemAnnouncementEnterDuration = 0.12f;
    private const float MayhemAnnouncementSettleDuration = 0.11f;
    private const float MayhemAnnouncementExitStart = 0.50f;
    private const float RewardOfferRefreshInterval = 0.5f;
    private const float RewardFeedbackDuration = 1.6f;
    private const string MasterVolumeKey = "zt3d.settings.master-volume";
    private const float SettingsSaveDelay = 0.35f;

    private static readonly Color MissionPrimaryTextColor =
        new Color32(247, 240, 226, 255);
    private static readonly Color MissionKillsPulseColor =
        new Color32(255, 211, 92, 255);
    private static readonly Color MissionScorePulseColor =
        new Color32(255, 151, 60, 255);

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
    private VisualElement missionMayhemFillHead;
    private VisualElement missionMayhemAnnouncement;
    private Label missionMayhemAnnouncementLabel;
    private Label missionMayhemAnnouncementMultiplier;
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
    private VisualElement missionIntroCountdownShell;
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
    private Button salvageDropButton;
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
    private VisualElement resultRewardedBonusRow;
    private Label resultRewardedBonus;
    private Label resultRewardedStatus;
    private Button resultButton;
    private Button resultRewardedButton;
    private Button galleryTab;
    private Button partsTab;
    private VisualElement leftFilters;
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
    private VisualElement missionEffectFeedback;
    private Label missionEffectFeedbackTitle;
    private Label missionEffectFeedbackDetail;

    private bool pointerDragging;
    private Vector2 previousPointerPosition;
    private float mayhemAnnouncementStartTime;
    private float mayhemAnnouncementHideTime;
    private float mayhemPulseEndTime;
    private float mayhemPulseStartTime;
    private bool mayhemAnnouncementVisible;
    private bool mayhemCardPulsing;
    private bool missionDamagePulsing;
    private float missionDamagePulseEndTime;
    private int displayedMissionSeconds = int.MinValue;
    private int displayedMissionKills = int.MinValue;
    private int displayedMissionKillTarget = int.MinValue;
    private int missionScoreTarget = int.MinValue;
    private float missionKillsPulseStartTime;
    private float missionScorePulseStartTime;
    private bool missionKillsPulsing;
    private bool missionScorePulsing;
    private bool missionEffectFeedbackPulsing;
    private float missionEffectFeedbackPulseStartTime;
    private bool missionIntroCountdownPulsing;
    private float missionIntroCountdownPulseStartTime;
    private float targetMissionMayhemFill;
    private float displayedMissionMayhemFill = float.NaN;
    private float missionMayhemFillVelocity;
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
    private float nextRewardOfferRefreshTime;
    private float salvageFeedbackHideTime;
    private bool salvageDropPending;
    private bool salvageFeedbackVisible;
    private bool missionRewardedPending;
    private bool missionRewardGranted;
    private bool currentMissionCanDoubleScrap;
    private bool garageVisible;
    private int currentMissionBaseScrap;

    public event Action MissionRequested;
    public event Action ResultAcknowledged;
    public event Action RewardedMissionDoubleRequested;
    public event Action SalvageDropRequested;
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
        CrazyGamesPlatformService.RewardedStateChanged +=
            HandleRewardedStateChanged;
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

        CrazyGamesPlatformService.RewardedStateChanged -=
            HandleRewardedStateChanged;

        if (settingsSavePending)
        {
            PlayerPrefs.Save();
            settingsSavePending = false;
        }
    }

    private void Update()
    {
        float currentTime = Time.unscaledTime;
        if (mayhemAnnouncementVisible)
        {
            AnimateMayhemAnnouncement(currentTime);
        }

        AnimateMissionCounters(currentTime);
        AnimateMissionEffectFeedback(currentTime);
        AnimateMissionIntroCountdown(currentTime);
        AnimateMissionMayhem(currentTime);

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

        if (salvageFeedbackVisible
            && currentTime >= salvageFeedbackHideTime)
        {
            salvageFeedbackVisible = false;
            RefreshRewardedOffers();
        }

        if (currentTime >= nextRewardOfferRefreshTime)
        {
            nextRewardOfferRefreshTime =
                currentTime + RewardOfferRefreshInterval;
            RefreshRewardedOffers();
        }

        UpdatePartHotspotPositions();
    }

    public void ShowGarage()
    {
        garageVisible = true;
        currentMissionCanDoubleScrap = false;
        garageRoot.style.display = DisplayStyle.Flex;
        missionHud.style.display = DisplayStyle.None;
        missionResult.style.display = DisplayStyle.None;
        missionIntro.style.display = DisplayStyle.None;
        missionPause.style.display = DisplayStyle.None;
        missionPauseButton.style.display = DisplayStyle.None;
        CloseSettingsPanel();
        HideMayhemAnnouncement();
        ResetMissionMayhemDisplay();
        ResetMissionCounterAnimations();
        missionSpeedAnimationEnabled = false;
        ResetMissionDamagePulse();
        previewController.SetVisible(true);
        activeScreen = GarageScreen.Gallery;
        buildState.ClearPreview();
        Refresh();
        RefreshRewardedOffers();
    }

    public void HideGarageForMission()
    {
        garageVisible = false;
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
            ? "VEHICLE"
            : vehicleName.ToUpperInvariant();
        missionIntroCountdown.text = "3";
        missionIntroCountdownShell.EnableInClassList(
            "mission-intro-countdown-shell--launch",
            false);
        missionIntro.style.display = DisplayStyle.Flex;
        missionPauseButton.style.display = DisplayStyle.None;
        StartMissionIntroCountdownPulse();
    }

    public void UpdateMissionIntroCountdown(string value)
    {
        if (missionIntroCountdown != null)
        {
            missionIntroCountdown.text = value;
            missionIntroCountdownShell.EnableInClassList(
                "mission-intro-countdown-shell--launch",
                string.Equals(
                    value,
                    "CRUSH!",
                    StringComparison.OrdinalIgnoreCase));
            StartMissionIntroCountdownPulse();
        }
    }

    public void CompleteMissionIntro()
    {
        missionIntroCountdownPulsing = false;
        if (missionIntroCountdownShell != null)
        {
            missionIntroCountdownShell.style.opacity = 1f;
            SetElementScale(missionIntroCountdownShell, 1f);
        }

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

        float currentTime = Time.unscaledTime;
        if (displayedMissionKills != progress.Kills
            || displayedMissionKillTarget != progress.KillTarget)
        {
            bool shouldResetPulse = displayedMissionKills != int.MinValue
                && progress.Kills < displayedMissionKills;
            bool shouldPulse = displayedMissionKills != int.MinValue
                && progress.Kills > displayedMissionKills;
            displayedMissionKills = progress.Kills;
            displayedMissionKillTarget = progress.KillTarget;
            missionKills.text =
                $"{progress.Kills} / {progress.KillTarget}";
            if (shouldPulse)
            {
                missionKillsPulsing = true;
                missionKillsPulseStartTime = currentTime;
            }
            else if (shouldResetPulse)
            {
                missionKillsPulsing = false;
                ResetMissionValueStyle(missionKills);
            }
        }

        UpdateMissionScore(progress.Score, currentTime);
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
        targetMissionMayhemFill = Mathf.Clamp01(progress.Meter01);
        if (float.IsNaN(displayedMissionMayhemFill))
        {
            displayedMissionMayhemFill = targetMissionMayhemFill;
            missionMayhemFillVelocity = 0f;
            ApplyMissionMayhemFill(displayedMissionMayhemFill);
        }

        ApplyMayhemTierClass(missionMayhemCard, progress.Tier);
    }

    public void ShowMayhemTierReached(MayhemProgress progress)
    {
        if (missionMayhemAnnouncement == null)
        {
            return;
        }

        missionMayhemAnnouncementLabel.text =
            MayhemRules.GetLabel(progress.Tier);
        missionMayhemAnnouncementMultiplier.text =
            $"x{progress.ScoreMultiplier:0.00}";
        ApplyMayhemTierClass(
            missionMayhemAnnouncement,
            progress.Tier);
        missionMayhemAnnouncement.style.display = DisplayStyle.Flex;
        missionMayhemAnnouncement.style.opacity = 0f;
        SetElementScale(missionMayhemAnnouncement, 0.74f);
        mayhemAnnouncementVisible = true;
        mayhemAnnouncementStartTime = Time.unscaledTime;
        mayhemAnnouncementHideTime =
            mayhemAnnouncementStartTime + MayhemAnnouncementDuration;

        missionMayhemCard.AddToClassList("mission-mayhem-card--pulse");
        mayhemCardPulsing = true;
        mayhemPulseStartTime = Time.unscaledTime;
        mayhemPulseEndTime =
            mayhemPulseStartTime + MissionMayhemCardPulseDuration;
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
            ? "VEHICLE"
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

        SplitMissionFeedback(
            message,
            out string feedbackTitle,
            out string feedbackDetail);
        missionEffectFeedbackTitle.text = feedbackTitle;
        missionEffectFeedbackDetail.text = feedbackDetail;
        missionEffectFeedbackDetail.style.display =
            string.IsNullOrWhiteSpace(feedbackDetail)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        missionEffectFeedback.style.display = DisplayStyle.Flex;
        missionEffectFeedback.style.opacity = 0f;
        SetElementScale(missionEffectFeedback, 0.82f);
        missionEffectFeedbackPulsing = true;
        missionEffectFeedbackPulseStartTime = Time.unscaledTime;
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
            missionEffectFeedbackPulsing = false;
            missionEffectFeedback.style.opacity = 0f;
            SetElementScale(missionEffectFeedback, 0.82f);
            missionEffectFeedback.style.display = DisplayStyle.None;
        }
    }

    public void ShowMissionResult(MissionResult result)
    {
        garageVisible = false;
        garageRoot.style.display = DisplayStyle.None;
        missionHud.style.display = DisplayStyle.None;
        missionResult.style.display = DisplayStyle.Flex;
        missionIntro.style.display = DisplayStyle.None;
        missionPause.style.display = DisplayStyle.None;
        missionPauseButton.style.display = DisplayStyle.None;
        CloseSettingsPanel();
        missionSpeedAnimationEnabled = false;
        previewController.SetVisible(false);

        resultStatus.text = result.Succeeded ? "SUCCESS" : "FAILED";
        resultTitle.text = result.Succeeded
            ? "MISSION COMPLETE"
            : result.EndReason == MissionEndReason.VehicleDestroyed
                ? "VEHICLE DESTROYED"
                : "TARGET MISSED";
        resultDescription.text = result.Succeeded
            ? "Kill target secured. The Safehouse crew is ready for your return."
            : result.EndReason == MissionEndReason.VehicleDestroyed
                ? "The vehicle was disabled in the mission zone."
                : "Time expired before the kill target was reached.";

        resultKills.text =
            $"{result.Progress.Kills} / {result.Progress.KillTarget}";
        resultScore.text = result.Progress.Score.ToString("N0");
        resultBonusKills.text = result.Progress.BonusKills.ToString("N0");
        resultHealth.text =
            $"{Mathf.CeilToInt(Mathf.Max(0f, result.RemainingHealth))}"
            + $" / {Mathf.CeilToInt(Mathf.Max(1f, result.MaximumHealth))}";
        MayhemTier highestTier = result.Progress.Mayhem.HighestTier;
        resultMayhemTier.text = highestTier == MayhemTier.None
            ? "NO HEAT"
            : MayhemRules.GetLabel(highestTier);
        resultBestChain.text =
            $"{result.Progress.Mayhem.BestChain:N0} KILLS";
        ApplyMayhemTierClass(resultMayhem, highestTier);
        resultKillScrap.text = $"+{result.Reward.KillScrap:N0}";
        resultSuccessBonus.text = $"+{result.Reward.CompletionBonus:N0}";
        resultTotalScrap.text = $"+{result.Reward.TotalScrap:N0} SCRAP";
        resultBalance.text = $"{result.Reward.BalanceAfter:N0} SCRAP";
        currentMissionCanDoubleScrap = result.CanDoubleScrap;
        currentMissionBaseScrap = result.Reward.TotalScrap;
        missionRewardedPending = false;
        missionRewardGranted = false;
        resultRewardedBonusRow.style.display = DisplayStyle.None;
        resultRewardedStatus.style.display = DisplayStyle.None;
        resultRewardedStatus.RemoveFromClassList(
            "mission-result-rewarded-status--success");
        resultButton.text = result.Succeeded
            ? "COLLECT"
            : "RETURN TO GARAGE";
        resultButton.SetEnabled(true);
        RefreshRewardedOffers();

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

    public void SetMissionRewardedPending(bool pending)
    {
        missionRewardedPending = pending;
        resultButton.SetEnabled(!pending);
        resultRewardedButton.SetEnabled(!pending);
        resultRewardedStatus.text = pending
            ? "CONNECTING TO REWARD VIDEO..."
            : string.Empty;
        resultRewardedStatus.style.display = pending
            ? DisplayStyle.Flex
            : DisplayStyle.None;
        RefreshRewardedOffers();
    }

    public void ShowRewardedMissionGranted(
        int bonusScrap,
        int newBalance)
    {
        missionRewardedPending = false;
        missionRewardGranted = true;
        int safeBonus = Mathf.Max(0, bonusScrap);
        resultRewardedBonus.text = $"+{safeBonus:N0}";
        resultRewardedBonusRow.style.display = DisplayStyle.Flex;
        resultTotalScrap.text =
            $"+{currentMissionBaseScrap + safeBonus:N0} SCRAP";
        resultBalance.text = $"{Mathf.Max(0, newBalance):N0} SCRAP";
        resultRewardedStatus.text =
            $"REWARD SECURED  +{safeBonus:N0} SCRAP";
        resultRewardedStatus.style.display = DisplayStyle.Flex;
        resultRewardedStatus.AddToClassList(
            "mission-result-rewarded-status--success");
        resultButton.SetEnabled(true);
        RefreshRewardedOffers();
    }

    public void ShowRewardedMissionUnavailable(string message)
    {
        missionRewardedPending = false;
        resultButton.SetEnabled(true);
        resultRewardedStatus.text = string.IsNullOrWhiteSpace(message)
            ? "AD UNAVAILABLE — COLLECT NORMAL REWARD"
            : message;
        resultRewardedStatus.style.display = DisplayStyle.Flex;
        resultRewardedStatus.RemoveFromClassList(
            "mission-result-rewarded-status--success");
        RefreshRewardedOffers();
    }

    public void SetSalvageDropPending(bool pending)
    {
        salvageDropPending = pending;
        salvageFeedbackVisible = false;
        salvageDropButton.text = pending
            ? "CONNECTING..."
            : $"▶  SALVAGE DROP  +{CrazyGamesPlatformService.SalvageDropScrap:N0}";
        salvageDropButton.SetEnabled(!pending);
        salvageDropButton.style.display = DisplayStyle.Flex;
    }

    public void ShowSalvageDropGranted(int amount)
    {
        salvageDropPending = false;
        salvageFeedbackVisible = true;
        salvageFeedbackHideTime =
            Time.unscaledTime + RewardFeedbackDuration;
        salvageDropButton.text =
            $"✓  +{Mathf.Max(0, amount):N0} SCRAP SECURED";
        salvageDropButton.SetEnabled(false);
        salvageDropButton.style.display = DisplayStyle.Flex;
    }

    public void ShowSalvageDropUnavailable(string message)
    {
        salvageDropPending = false;
        salvageFeedbackVisible = true;
        salvageFeedbackHideTime =
            Time.unscaledTime + RewardFeedbackDuration;
        salvageDropButton.text = string.IsNullOrWhiteSpace(message)
            ? "AD UNAVAILABLE"
            : message;
        salvageDropButton.SetEnabled(false);
        salvageDropButton.style.display = DisplayStyle.Flex;
    }

    private void HandleRewardedStateChanged()
    {
        RefreshRewardedOffers();
    }

    private void RefreshRewardedOffers()
    {
        if (salvageDropButton == null || resultRewardedButton == null)
        {
            return;
        }

        bool canOffer =
            CrazyGamesPlatformService.CanOfferRewardedAd;
        bool showSalvage = salvageDropPending
                           || salvageFeedbackVisible
                           || (garageVisible && canOffer);
        salvageDropButton.style.display = showSalvage
            ? DisplayStyle.Flex
            : DisplayStyle.None;
        if (showSalvage
            && !salvageDropPending
            && !salvageFeedbackVisible)
        {
            salvageDropButton.text =
                $"▶  SALVAGE DROP  +{CrazyGamesPlatformService.SalvageDropScrap:N0}";
            salvageDropButton.SetEnabled(true);
        }

        bool showMissionReward = missionRewardedPending
                                 || (currentMissionCanDoubleScrap
                                     && !missionRewardGranted
                                     && canOffer);
        resultRewardedButton.style.display = showMissionReward
            ? DisplayStyle.Flex
            : DisplayStyle.None;
        if (showMissionReward && !missionRewardedPending)
        {
            resultRewardedButton.text = "▶  DOUBLE SCRAP";
            resultRewardedButton.SetEnabled(true);
        }
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
        missionMayhemFillHead =
            RequireElement<VisualElement>(root, "mission-mayhem-fill-head");
        missionMayhemAnnouncement =
            RequireElement<VisualElement>(root, "mission-mayhem-announcement");
        missionMayhemAnnouncementLabel =
            RequireElement<Label>(root, "mission-mayhem-announcement-label");
        missionMayhemAnnouncementMultiplier =
            RequireElement<Label>(
                root,
                "mission-mayhem-announcement-multiplier");
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
        missionIntroCountdownShell =
            RequireElement<VisualElement>(
                root,
                "mission-intro-countdown-shell");
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
        salvageDropButton =
            RequireElement<Button>(root, "salvage-drop-button");
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
        resultRewardedBonusRow =
            RequireElement<VisualElement>(root, "result-rewarded-bonus-row");
        resultRewardedBonus =
            RequireElement<Label>(root, "result-rewarded-bonus");
        resultRewardedStatus =
            RequireElement<Label>(root, "result-rewarded-status");
        resultButton = RequireElement<Button>(root, "result-button");
        resultRewardedButton =
            RequireElement<Button>(root, "result-rewarded-button");
        galleryTab = RequireElement<Button>(root, "gallery-tab");
        partsTab = RequireElement<Button>(root, "parts-tab");
        leftFilters = RequireElement<VisualElement>(root, "left-filters");
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
            RequireElement<VisualElement>(root, "mission-effect-feedback");
        missionEffectFeedbackTitle =
            RequireElement<Label>(root, "mission-effect-feedback-title");
        missionEffectFeedbackDetail =
            RequireElement<Label>(root, "mission-effect-feedback-detail");

        galleryTab.clicked += () => SwitchScreen(GarageScreen.Gallery);
        partsTab.clicked += () => SwitchScreen(GarageScreen.Parts);
        carouselPrev.clicked += () => CycleShowroom(-1);
        carouselNext.clicked += () => CycleShowroom(1);
        missionButton.clicked += () => MissionRequested?.Invoke();
        resultButton.clicked += () => ResultAcknowledged?.Invoke();
        resultRewardedButton.clicked +=
            () => RewardedMissionDoubleRequested?.Invoke();
        salvageDropButton.clicked +=
            () => SalvageDropRequested?.Invoke();
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
        fullscreenButton.style.display =
            CrazyGamesPlatformService.ShouldHideCustomFullscreen
                ? DisplayStyle.None
                : DisplayStyle.Flex;
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
            ? "EXIT FULLSCREEN"
            : "FULLSCREEN";
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
            missionMayhemAnnouncement.style.opacity = 0f;
            SetElementScale(missionMayhemAnnouncement, 0.74f);
            missionMayhemAnnouncement.style.display = DisplayStyle.None;
        }

        missionMayhemCard?.RemoveFromClassList(
            "mission-mayhem-card--pulse");
        SetElementScale(missionMayhemTier, 1f);
        SetElementScale(missionMayhemMultiplier, 1f);
    }

    private void AnimateMissionMayhem(float currentTime)
    {
        if (missionMayhemFill == null || missionMayhemFillHead == null)
        {
            return;
        }

        if (!float.IsNaN(displayedMissionMayhemFill))
        {
            displayedMissionMayhemFill = Mathf.SmoothDamp(
                displayedMissionMayhemFill,
                targetMissionMayhemFill,
                ref missionMayhemFillVelocity,
                MissionMayhemFillSmoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime);
            if (Mathf.Abs(
                    displayedMissionMayhemFill
                    - targetMissionMayhemFill) < 0.001f)
            {
                displayedMissionMayhemFill = targetMissionMayhemFill;
                missionMayhemFillVelocity = 0f;
            }

            ApplyMissionMayhemFill(displayedMissionMayhemFill);
            if (displayedMissionMayhemFill > 0.008f)
            {
                float energyPulse =
                    (Mathf.Sin(currentTime * 11f) + 1f) * 0.5f;
                missionMayhemFillHead.style.opacity =
                    Mathf.Lerp(0.50f, 1f, energyPulse);
                SetElementScale(
                    missionMayhemFillHead,
                    Mathf.Lerp(0.82f, 1.24f, energyPulse));
            }
        }

        if (!mayhemCardPulsing)
        {
            return;
        }

        float pulseProgress = Mathf.InverseLerp(
            mayhemPulseStartTime,
            mayhemPulseEndTime,
            currentTime);
        if (currentTime >= mayhemPulseEndTime)
        {
            mayhemCardPulsing = false;
            missionMayhemCard.RemoveFromClassList(
                "mission-mayhem-card--pulse");
            SetElementScale(missionMayhemTier, 1f);
            SetElementScale(missionMayhemMultiplier, 1f);
            return;
        }

        float pulse = Mathf.Sin(pulseProgress * Mathf.PI);
        SetElementScale(missionMayhemTier, 1f + pulse * 0.12f);
        SetElementScale(missionMayhemMultiplier, 1f + pulse * 0.18f);
    }

    private void ApplyMissionMayhemFill(float fill01)
    {
        float clampedFill = Mathf.Clamp01(fill01);
        missionMayhemFill.style.width = Length.Percent(clampedFill * 100f);
        missionMayhemFillHead.style.display = clampedFill > 0.008f
            ? DisplayStyle.Flex
            : DisplayStyle.None;
    }

    private void ResetMissionMayhemDisplay()
    {
        targetMissionMayhemFill = 0f;
        displayedMissionMayhemFill = float.NaN;
        missionMayhemFillVelocity = 0f;
        if (missionMayhemFill != null)
        {
            missionMayhemFill.style.width = Length.Percent(0f);
        }

        if (missionMayhemFillHead != null)
        {
            missionMayhemFillHead.style.display = DisplayStyle.None;
            missionMayhemFillHead.style.opacity = 0f;
            SetElementScale(missionMayhemFillHead, 1f);
        }
    }

    private void AnimateMayhemAnnouncement(float currentTime)
    {
        if (missionMayhemAnnouncement == null)
        {
            return;
        }

        float elapsed = currentTime - mayhemAnnouncementStartTime;
        if (currentTime >= mayhemAnnouncementHideTime)
        {
            HideMayhemAnnouncement();
            return;
        }

        float opacity;
        float scale;
        if (elapsed < MayhemAnnouncementEnterDuration)
        {
            float enter = Mathf.Clamp01(
                elapsed / MayhemAnnouncementEnterDuration);
            float easedEnter = 1f - Mathf.Pow(1f - enter, 3f);
            opacity = easedEnter;
            scale = Mathf.LerpUnclamped(0.74f, 1.08f, easedEnter);
        }
        else if (elapsed < MayhemAnnouncementEnterDuration
                 + MayhemAnnouncementSettleDuration)
        {
            float settle = Mathf.InverseLerp(
                MayhemAnnouncementEnterDuration,
                MayhemAnnouncementEnterDuration
                    + MayhemAnnouncementSettleDuration,
                elapsed);
            opacity = 1f;
            scale = Mathf.Lerp(1.08f, 1f, settle);
        }
        else if (elapsed < MayhemAnnouncementExitStart)
        {
            opacity = 1f;
            scale = 1f;
        }
        else
        {
            float exit = Mathf.InverseLerp(
                MayhemAnnouncementExitStart,
                MayhemAnnouncementDuration,
                elapsed);
            float easedExit = exit * exit;
            opacity = 1f - easedExit;
            scale = Mathf.Lerp(1f, 1.06f, easedExit);
        }

        missionMayhemAnnouncement.style.opacity = opacity;
        SetElementScale(missionMayhemAnnouncement, scale);
    }

    private void AnimateMissionEffectFeedback(float currentTime)
    {
        if (!missionEffectFeedbackPulsing || missionEffectFeedback == null)
        {
            return;
        }

        float progress = Mathf.Clamp01(
            (currentTime - missionEffectFeedbackPulseStartTime)
            / MissionFeedbackPulseDuration);
        if (progress >= 1f)
        {
            missionEffectFeedbackPulsing = false;
            missionEffectFeedback.style.opacity = 1f;
            SetElementScale(missionEffectFeedback, 1f);
            return;
        }

        float eased = 1f - Mathf.Pow(1f - progress, 3f);
        float scale = progress < 0.66f
            ? Mathf.Lerp(0.82f, 1.06f, eased)
            : Mathf.Lerp(1.06f, 1f, Mathf.InverseLerp(0.66f, 1f, progress));
        missionEffectFeedback.style.opacity = eased;
        SetElementScale(missionEffectFeedback, scale);
    }

    private void StartMissionIntroCountdownPulse()
    {
        if (missionIntroCountdownShell == null)
        {
            return;
        }

        missionIntroCountdownPulsing = true;
        missionIntroCountdownPulseStartTime = Time.unscaledTime;
        missionIntroCountdownShell.style.opacity = 0.12f;
        SetElementScale(missionIntroCountdownShell, 0.70f);
    }

    private void AnimateMissionIntroCountdown(float currentTime)
    {
        if (!missionIntroCountdownPulsing
            || missionIntroCountdownShell == null)
        {
            return;
        }

        float progress = Mathf.Clamp01(
            (currentTime - missionIntroCountdownPulseStartTime)
            / MissionIntroCountdownPulseDuration);
        if (progress >= 1f)
        {
            missionIntroCountdownPulsing = false;
            missionIntroCountdownShell.style.opacity = 1f;
            SetElementScale(missionIntroCountdownShell, 1f);
            return;
        }

        float eased = 1f - Mathf.Pow(1f - progress, 3f);
        float scale = progress < 0.70f
            ? Mathf.Lerp(0.70f, 1.08f, eased)
            : Mathf.Lerp(1.08f, 1f, Mathf.InverseLerp(0.70f, 1f, progress));
        missionIntroCountdownShell.style.opacity = eased;
        SetElementScale(missionIntroCountdownShell, scale);
    }

    private static void SplitMissionFeedback(
        string message,
        out string title,
        out string detail)
    {
        string safeMessage = message?.Trim() ?? string.Empty;
        int separatorIndex = safeMessage.IndexOf('\u00b7');
        if (separatorIndex <= 0)
        {
            title = safeMessage;
            detail = string.Empty;
            return;
        }

        title = safeMessage.Substring(0, separatorIndex).Trim();
        detail = safeMessage.Substring(separatorIndex + 1).Trim();
    }

    private void UpdateMissionScore(int score, float currentTime)
    {
        if (score == missionScoreTarget)
        {
            return;
        }

        bool shouldResetPulse = missionScoreTarget != int.MinValue
            && score < missionScoreTarget;
        bool shouldPulse = missionScoreTarget != int.MinValue
            && score > missionScoreTarget;
        missionScoreTarget = score;
        missionScore.text = score.ToString("N0");
        if (shouldPulse)
        {
            missionScorePulsing = true;
            missionScorePulseStartTime = currentTime;
        }
        else if (shouldResetPulse)
        {
            missionScorePulsing = false;
            ResetMissionValueStyle(missionScore);
        }
    }

    private void AnimateMissionCounters(float currentTime)
    {
        if (missionKillsPulsing)
        {
            missionKillsPulsing = AnimateMissionValuePulse(
                missionKills,
                currentTime - missionKillsPulseStartTime,
                0.18f,
                MissionKillsPulseColor);
        }

        if (missionScorePulsing)
        {
            missionScorePulsing = AnimateMissionValuePulse(
                missionScore,
                currentTime - missionScorePulseStartTime,
                0.14f,
                MissionScorePulseColor);
        }
    }

    private void ResetMissionCounterAnimations()
    {
        missionKillsPulsing = false;
        missionScorePulsing = false;
        ResetMissionValueStyle(missionKills);
        ResetMissionValueStyle(missionScore);
    }

    private static bool AnimateMissionValuePulse(
        Label label,
        float elapsed,
        float amplitude,
        Color pulseColor)
    {
        float progress = elapsed / MissionCounterPulseDuration;
        if (progress >= 1f)
        {
            ResetMissionValueStyle(label);
            return false;
        }

        float pulse = Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI);
        SetElementScale(label, 1f + pulse * amplitude);
        label.style.color = Color.Lerp(
            MissionPrimaryTextColor,
            pulseColor,
            pulse);
        return true;
    }

    private static void ResetMissionValueStyle(VisualElement element)
    {
        if (element == null)
        {
            return;
        }

        SetElementScale(element, 1f);
        element.style.color = MissionPrimaryTextColor;
    }

    private static void SetElementScale(
        VisualElement element,
        float uniformScale)
    {
        Scale scale = default;
        scale.value = new Vector3(uniformScale, uniformScale, 1f);
        StyleScale styleScale = default;
        styleScale.value = scale;
        element.style.scale = styleScale;
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

        balanceValue.text = $"{economy.Scrap:N0} SCRAP";
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
            return;
        }

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
        detailTitle.text = vehicle != null
            ? vehicle.DisplayName.ToUpperInvariant()
            : "SELECT VEHICLE";
        detailDescription.text = GetVehicleGalleryTagline(vehicle);

        bool owned = buildState.IsVehicleOwned(vehicle);
        bool selected = vehicle != null && vehicle == buildState.SelectedVehicle;
        contextAction.text = selected
            ? "ACTIVE VEHICLE"
            : owned
                ? "SELECT VEHICLE"
            : vehicle != null
                ? $"BUY · {vehicle.Price:N0} SCRAP"
                : "SELECT VEHICLE";
        contextAction.SetEnabled(
            vehicle != null
            && !selected
            && (owned || economy.CanAfford(vehicle.Price)));
        contextAction.clicked += HandleContextAction;
        contextHint.text = selected
            ? "You will deploy with this vehicle and its equipped build."
            : owned
                ? "Set this as your active mission vehicle."
            : vehicle != null && economy.CanAfford(vehicle.Price)
                ? "Purchasing adds the vehicle to your garage without selecting it."
                : "Not enough Scrap for this vehicle.";
    }

    private static string GetVehicleGalleryTagline(GarageVehicleDefinition vehicle)
    {
        if (vehicle == null)
        {
            return string.Empty;
        }

        switch (vehicle.VehicleId)
        {
            case "ambulance":
                return "BALANCED • DURABLE";
            case "buggy":
                return "LIGHT • FAST • AGILE";
            case "prison-bus":
                return "HEAVY • TOUGH • DEVASTATING";
            case "muscle-car":
                return "FAST • POWERFUL";
            case "ute":
                return "BALANCED • POWERFUL";
            case "golf-cart":
                return "LIGHT • AGILE • FRAGILE";
            default:
                return vehicle.Description.ToUpperInvariant();
        }
    }

    private void PopulatePartDetails()
    {
        detailMechanics.style.display = DisplayStyle.None;
        GarageVehicleDefinition vehicle = buildState.DisplayedVehicle;
        GarageAttachmentDefinition attachment = buildState.PreviewAttachment;
        detailTitle.text = attachment != null ? attachment.DisplayName : "No compatible parts";
        detailDescription.text =
            attachment != null
                ? attachment.Description
                : "No compatible part is available for this mount.";
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
            contextAction.text = "SELECT PART";
            contextAction.SetEnabled(false);
            contextHint.text = "SELECT A MOUNT";
        }
        else if (!vehicleOwned)
        {
            contextAction.text = "BUY VEHICLE FIRST";
            contextAction.SetEnabled(false);
            contextHint.text = $"{vehicle.DisplayName.ToUpperInvariant()} REQUIRED";
        }
        else if (!vehicleSelected)
        {
            contextAction.text = "SELECT VEHICLE FIRST";
            contextAction.SetEnabled(false);
            contextHint.text = "ACTIVE VEHICLE REQUIRED";
        }
        else
        {
            contextAction.text = equipped
                ? "REMOVE"
                : owned
                    ? "EQUIP"
                : $"BUY · {attachment.Price:N0} SCRAP";
            contextAction.SetEnabled(
                equipped || owned || economy.CanAfford(attachment.Price));
            contextHint.text = equipped
                ? "EQUIPPED"
                : owned
                    ? "OWNED"
                : economy.CanAfford(attachment.Price)
                    ? "AVAILABLE"
                    : "NOT ENOUGH SCRAP";
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

            Label name = new Label(GarageVehicleStatPresentation.GetEnglishLabel(stat));
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
        GarageAttachmentDefinition focusAttachment =
            activeScreen == GarageScreen.Parts
                ? buildState.PreviewAttachment
                : null;
        previewController.SetBuild(
            displayedVehicle,
            buildState.GetEquippedAttachments(),
            buildState.PreviewAttachment,
            showEquipped,
            focusAttachment,
            activeScreen == GarageScreen.Gallery);
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
                ? $"VEHICLE  {index + 1} / {vehicles.Count}"
                : "NO VEHICLES";
            carouselMeta.text = vehicle == buildState.SelectedVehicle
                ? "ACTIVE VEHICLE"
                : buildState.IsVehicleOwned(vehicle)
                    ? "OWNED"
                    : vehicle != null
                        ? $"{vehicle.Price:N0} SCRAP"
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
            : $"{GetSlotLabel(partsFilter).ToUpperInvariant()}  ·  NO COMPATIBLE PARTS";
        carouselMeta.text = attachment != null
            && buildState.GetEquipped(attachment.Slot) == attachment
                ? "EQUIPPED"
                : buildState.IsAttachmentOwned(attachment)
                    ? "OWNED"
                    : attachment != null
                        ? $"{attachment.Price:N0} SCRAP"
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
            GarageAttachmentSlot.Front => "FRONT",
            GarageAttachmentSlot.Armor => "ARMOR",
            GarageAttachmentSlot.Engine => "ENGINE",
            GarageAttachmentSlot.Wheels => "WHEELS",
            GarageAttachmentSlot.RearAero => "REAR / AERO",
            GarageAttachmentSlot.RoofUtility => "ROOF / UTILITY",
            _ => slot.ToString().ToUpperInvariant()
        };
    }

    private static string GetHotspotLabel(GarageAttachmentSlot slot)
    {
        return slot switch
        {
            GarageAttachmentSlot.Front => "FRONT",
            GarageAttachmentSlot.Armor => "ARMOR",
            GarageAttachmentSlot.Engine => "ENGINE",
            GarageAttachmentSlot.Wheels => "WHEEL",
            GarageAttachmentSlot.RearAero => "REAR",
            GarageAttachmentSlot.RoofUtility => "ROOF",
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
