using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
[RequireComponent(typeof(UIDocument))]
public sealed class GarageUiController : MonoBehaviour
{
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
    [SerializeField] private GaragePreviewController previewController;

    private readonly List<StatElements> statElements = new();
    private GarageScreen activeScreen = GarageScreen.Assembly;
    private GarageAttachmentSlot partsFilter = GarageAttachmentSlot.Front;

    private VisualElement garageRoot;
    private VisualElement missionHud;
    private Label missionTimer;
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
    private Button contextAction;
    private Label contextHint;
    private Label selectedBuildLabel;
    private Button missionButton;
    private VisualElement previewViewport;

    private bool pointerDragging;
    private Vector2 previousPointerPosition;

    public event Action MissionRequested;

    private void Reset()
    {
        document = GetComponent<UIDocument>();
        buildState = GetComponent<GarageBuildState>();
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

        if (previewController == null)
        {
            previewController = GetComponent<GaragePreviewController>();
        }

        if (document == null || buildState == null || previewController == null)
        {
            Debug.LogError(
                "GarageUiController: UIDocument, GarageBuildState and GaragePreviewController are required.",
                this);
            enabled = false;
            return;
        }

        BindVisualTree();
        buildState.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (buildState != null)
        {
            buildState.Changed -= Refresh;
        }
    }

    public void ShowGarage()
    {
        garageRoot.style.display = DisplayStyle.Flex;
        missionHud.style.display = DisplayStyle.None;
        previewController.SetVisible(true);
        activeScreen = GarageScreen.Assembly;
        buildState.ClearPreview();
        Refresh();
    }

    public void HideGarageForMission()
    {
        garageRoot.style.display = DisplayStyle.None;
        missionHud.style.display = DisplayStyle.Flex;
        previewController.SetVisible(false);
    }

    public void UpdateMissionTimer(float remainingSeconds)
    {
        if (missionTimer == null)
        {
            return;
        }

        int seconds = Mathf.Max(0, Mathf.CeilToInt(remainingSeconds));
        int minutesPart = seconds / 60;
        int secondsPart = seconds % 60;
        missionTimer.text = $"{minutesPart:0}:{secondsPart:00}";
    }

    private void BindVisualTree()
    {
        VisualElement root = document.rootVisualElement;
        garageRoot = RequireElement<VisualElement>(root, "garage-root");
        missionHud = RequireElement<VisualElement>(root, "mission-hud");
        missionTimer = RequireElement<Label>(root, "mission-timer");
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
        contextAction = RequireElement<Button>(root, "context-action");
        contextHint = RequireElement<Label>(root, "context-hint");
        selectedBuildLabel = RequireElement<Label>(root, "selected-build");
        missionButton = RequireElement<Button>(root, "mission-button");
        previewViewport = RequireElement<VisualElement>(root, "preview-viewport");

        assemblyTab.clicked += () => SwitchScreen(GarageScreen.Assembly);
        galleryTab.clicked += () => SwitchScreen(GarageScreen.Gallery);
        partsTab.clicked += () => SwitchScreen(GarageScreen.Parts);
        missionButton.clicked += () => MissionRequested?.Invoke();

        previewViewport.RegisterCallback<PointerDownEvent>(OnPreviewPointerDown);
        previewViewport.RegisterCallback<PointerMoveEvent>(OnPreviewPointerMove);
        previewViewport.RegisterCallback<PointerUpEvent>(OnPreviewPointerUp);
        previewViewport.RegisterCallback<PointerCaptureOutEvent>(_ => EndPreviewDrag());

        CreateStatElements();
    }

    private void SwitchScreen(GarageScreen screen)
    {
        activeScreen = screen;
        buildState.ClearPreview();
        Refresh();
    }

    private void Refresh()
    {
        if (garageRoot == null || buildState.Catalog == null)
        {
            return;
        }

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
            string suffix = buildState.IsVehicleOwned(vehicle) ? " · SAHİP" : string.Empty;
            Button button = CreateListButton(vehicle.DisplayName + suffix, selected);
            button.clicked += () => buildState.PreviewVehicle(vehicle);
            leftList.Add(button);
        }
    }

    private void PopulatePartFilters()
    {
        foreach (GarageAttachmentSlot slot in Enum.GetValues(typeof(GarageAttachmentSlot)))
        {
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
                : string.Empty;
            Button button = CreateListButton(
                attachment.DisplayName + suffix,
                selected);
            button.clicked += () => buildState.PreviewPart(attachment);
            leftList.Add(button);
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

            Button change = new Button(() =>
            {
                partsFilter = slot;
                SwitchScreen(GarageScreen.Parts);
            })
            {
                text = "DEĞİŞTİR"
            };
            change.AddToClassList("ghost-button");

            row.Add(copy);
            row.Add(change);
            rightList.Add(row);
        }
    }

    private void PopulateVehicleDetails()
    {
        rightTitle.text = "ARAÇ DETAYI";
        GarageVehicleDefinition vehicle = buildState.DisplayedVehicle;
        detailTitle.text = vehicle != null ? vehicle.DisplayName : "Araç seç";
        detailDescription.text = vehicle != null ? vehicle.Description : string.Empty;

        bool owned = buildState.IsVehicleOwned(vehicle);
        contextAction.text = owned ? "ARACI SEÇ" : "EDİNME KARARI BEKLİYOR";
        contextAction.SetEnabled(owned);
        contextAction.clicked += HandleContextAction;
        contextHint.text = owned
            ? "Seçim Montaj ekranındaki aktif aracı değiştirir."
            : "Fiyat, para ve kilit sözleşmesi ayrıca onaylanacak.";
    }

    private void PopulatePartDetails()
    {
        rightTitle.text = "PARÇA DETAYI";
        GarageAttachmentDefinition attachment = buildState.PreviewAttachment;
        detailTitle.text = attachment != null ? attachment.DisplayName : "Parça seç";
        detailDescription.text =
            attachment != null ? attachment.Description : "Uyumlu parçayı soldan seç.";

        bool owned = buildState.IsAttachmentOwned(attachment);
        contextAction.text = owned ? "MONTAJDA TAK" : "SATIN ALMA KARARI BEKLİYOR";
        contextAction.SetEnabled(owned);
        contextAction.clicked += HandleContextAction;
        contextHint.text = owned
            ? "Takıldığında önizlenen statlar aktif build olur."
            : "Parça şu anda yalnız görsel ve stat önizlemesindedir.";
    }

    private void HandleContextAction()
    {
        switch (activeScreen)
        {
            case GarageScreen.Assembly:
                MissionRequested?.Invoke();
                break;

            case GarageScreen.Gallery:
                if (buildState.SelectOwnedVehicle(buildState.DisplayedVehicle))
                {
                    activeScreen = GarageScreen.Assembly;
                    Refresh();
                }
                break;

            case GarageScreen.Parts:
                if (buildState.EquipPreviewPart())
                {
                    activeScreen = GarageScreen.Assembly;
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
        VehicleStats current = buildState.CurrentStats;
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
