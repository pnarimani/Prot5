using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using SiegeSurvival.Core;
using SiegeSurvival.Data;
using SiegeSurvival.UI;
using SiegeSurvival.UI.Panels;
using SiegeSurvival.UI.Widgets;

namespace SiegeSurvival
{
    /// <summary>
    /// Builds the entire UI hierarchy programmatically on Awake.
    /// Dark siege-survival aesthetic with proper meters, bars, and visual hierarchy.
    /// </summary>
    public class SceneBootstrap : MonoBehaviour
    {
        [Header("Zone Definitions (must assign 5, ordered 0-4 in Inspector)")]
        public ZoneDefinition[] zoneDefinitions;

        // ── Colour palette ──────────────────────────────────
        static readonly Color COL_BG_DARK      = new(0.06f, 0.06f, 0.10f, 1f);
        static readonly Color COL_BG_PANEL     = new(0.10f, 0.10f, 0.16f, 0.95f);
        static readonly Color COL_BG_SECTION   = new(0.13f, 0.13f, 0.22f, 0.90f);
        static readonly Color COL_BG_HEADER    = new(0.08f, 0.16f, 0.30f, 0.95f);
        static readonly Color COL_BG_ROW_EVEN  = new(0.11f, 0.11f, 0.18f, 0.85f);
        static readonly Color COL_BG_ROW_ODD   = new(0.09f, 0.09f, 0.15f, 0.85f);
        static readonly Color COL_BTN          = new(0.18f, 0.22f, 0.35f, 1f);
        static readonly Color COL_BTN_ACCENT   = new(0.22f, 0.38f, 0.58f, 1f);
        static readonly Color COL_BTN_DANGER   = new(0.55f, 0.15f, 0.15f, 1f);
        static readonly Color COL_TAB_ACTIVE   = new(0.15f, 0.30f, 0.55f, 1f);
        static readonly Color COL_TAB_INACTIVE = new(0.12f, 0.12f, 0.20f, 1f);
        static readonly Color COL_SLIDER_BG    = new(0.08f, 0.08f, 0.12f, 1f);
        static readonly Color COL_TRANSPARENT  = new(0f, 0f, 0f, 0f);

        static readonly Color COL_FOOD  = new(0.55f, 0.76f, 0.22f, 1f);
        static readonly Color COL_WATER = new(0.26f, 0.65f, 0.96f, 1f);
        static readonly Color COL_FUEL  = new(1.00f, 0.60f, 0.00f, 1f);
        static readonly Color COL_MED   = new(0.81f, 0.58f, 0.85f, 1f);
        static readonly Color COL_MAT   = new(0.63f, 0.53f, 0.50f, 1f);

        static readonly Color COL_TEXT_PRIMARY   = new(0.92f, 0.92f, 0.96f, 1f);
        static readonly Color COL_TEXT_SECONDARY = new(0.65f, 0.65f, 0.72f, 1f);
        static readonly Color COL_TEXT_GOLD      = new(0.85f, 0.75f, 0.35f, 1f);

        private Canvas _mainCanvas;
        private GameObject _canvasGO;

        // ─── Awake ──────────────────────────────────────────
        private void Awake()
        {
            // 1. EventSystem
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
            }

            // 2. Canvas
            _canvasGO = new GameObject("MainCanvas");
            _mainCanvas = _canvasGO.AddComponent<Canvas>();
            _mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _mainCanvas.sortingOrder = 0;
            var scaler = _canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            _canvasGO.AddComponent<GraphicRaycaster>();

            var canvasBg = _canvasGO.AddComponent<Image>();
            canvasBg.color = COL_BG_DARK;

            // 3. GameManager
            var gmGo = new GameObject("GameManager");
            var gm = gmGo.AddComponent<GameManager>();
            gm.zoneDefinitions = zoneDefinitions;

            // 4. Build UI root
            var rootGo = MakeRect("Root", _canvasGO.transform);
            StretchToParent(rootGo);
            var rootVlg = rootGo.AddComponent<VerticalLayoutGroup>();
            rootVlg.childControlWidth = true;
            rootVlg.childControlHeight = true;
            rootVlg.childForceExpandWidth = true;
            rootVlg.childForceExpandHeight = false;
            rootVlg.spacing = 2;
            rootVlg.padding = new RectOffset(4, 4, 4, 4);

            // ═══════════════════════════════════════════════════════
            //  TOP BAR  (3 dense rows: info / resources / meters)
            // ═══════════════════════════════════════════════════════
            var topBarGo = MakePanel("TopBarPanel", rootGo.transform, COL_BG_PANEL);
            AddLE(topBarGo, preferredH: 112);
            var topBar = topBarGo.AddComponent<TopBarPanel>();
            BuildTopBar(topBar, topBarGo);

            // ═══════════════════════════════════════════════════════
            //  MIDDLE  (4 columns)
            // ═══════════════════════════════════════════════════════
            var middleGo = MakeRect("MiddleSection", rootGo.transform);
            AddLE(middleGo, flexH: 1f);
            var middleHlg = middleGo.AddComponent<HorizontalLayoutGroup>();
            middleHlg.childControlWidth = true;
            middleHlg.childControlHeight = true;
            middleHlg.childForceExpandWidth = false;
            middleHlg.childForceExpandHeight = true;
            middleHlg.spacing = 3;

            // ── COL 1  ZONES ─────────────────────────────────
            var col1Go = MakePanel("Col_Zones", middleGo.transform, COL_BG_PANEL);
            AddLE(col1Go, flexW: 0.23f);
            var col1Vlg = col1Go.AddComponent<VerticalLayoutGroup>();
            col1Vlg.childControlWidth = true;
            col1Vlg.childControlHeight = true;
            col1Vlg.childForceExpandWidth = true;
            col1Vlg.childForceExpandHeight = false;
            col1Vlg.spacing = 2;
            col1Vlg.padding = new RectOffset(4, 4, 4, 4);

            var zonePanelGo = MakeRect("ZonePanel", col1Go.transform);
            AddLE(zonePanelGo, flexH: 1f);
            var zoneVlg = zonePanelGo.AddComponent<VerticalLayoutGroup>();
            zoneVlg.childControlWidth = true;
            zoneVlg.childControlHeight = true;
            zoneVlg.childForceExpandWidth = true;
            zoneVlg.childForceExpandHeight = false;
            zoneVlg.spacing = 2;
            var zonePanel = zonePanelGo.AddComponent<ZonePanel>();
            BuildZonePanel(zonePanel, zonePanelGo);

            // Zone ring widget
            var zoneRingGo = MakePanel("ZoneRingWidget", col1Go.transform, COL_BG_SECTION);
            AddLE(zoneRingGo, preferredH: 140);
            var zoneRing = zoneRingGo.AddComponent<ZoneRingWidget>();
            BuildZoneRing(zoneRing, zoneRingGo);

            // Wells repair button
            var wellsBtnGo = MakeButton("WellsRepairBtn", col1Go.transform, "Repair Wells (10 Materials)", COL_BTN_ACCENT, 14);
            AddLE(wellsBtnGo, preferredH: 30);
            wellsBtnGo.GetComponent<Button>().onClick.AddListener(() => gm.RepairWells());
            wellsBtnGo.AddComponent<WellsRepairButtonToggle>();

            // ── COL 2  WORKERS ───────────────────────────────
            var col2Go = MakePanel("Col_Workers", middleGo.transform, COL_BG_PANEL);
            AddLE(col2Go, flexW: 0.24f);
            var workerPanelGo = MakeRect("WorkerAllocationPanel", col2Go.transform);
            StretchToParent(workerPanelGo);
            var workerPanel = workerPanelGo.AddComponent<WorkerAllocationPanel>();
            BuildWorkerPanel(workerPanel, workerPanelGo);

            // ── COL 3  ACTIONS ───────────────────────────────
            var col3Go = MakePanel("Col_Actions", middleGo.transform, COL_BG_PANEL);
            AddLE(col3Go, flexW: 0.30f);
            var col3Vlg = col3Go.AddComponent<VerticalLayoutGroup>();
            col3Vlg.childControlWidth = true;
            col3Vlg.childControlHeight = true;
            col3Vlg.childForceExpandWidth = true;
            col3Vlg.childForceExpandHeight = false;
            col3Vlg.spacing = 2;
            col3Vlg.padding = new RectOffset(4, 4, 4, 4);

            // Tab bar
            var tabBarGo = MakeRect("TabBar", col3Go.transform);
            AddLE(tabBarGo, preferredH: 34);
            var tabBarHlg = tabBarGo.AddComponent<HorizontalLayoutGroup>();
            tabBarHlg.childControlWidth = true;
            tabBarHlg.childForceExpandWidth = true;
            tabBarHlg.childControlHeight = true;
            tabBarHlg.childForceExpandHeight = true;
            tabBarHlg.spacing = 2;

            var lawsTabGo = MakeButton("LawsTab", tabBarGo.transform, "LAWS", COL_TAB_ACTIVE, 14);
            var ordersTabGo = MakeButton("OrdersTab", tabBarGo.transform, "ORDERS", COL_TAB_INACTIVE, 14);
            var missionsTabGo = MakeButton("MissionsTab", tabBarGo.transform, "MISSIONS", COL_TAB_INACTIVE, 14);
            var lawsTabImg = lawsTabGo.GetComponent<Image>();
            var ordersTabImg = ordersTabGo.GetComponent<Image>();
            var missionsTabImg = missionsTabGo.GetComponent<Image>();

            // Laws
            var lawsPanelGo = MakeRect("LawsPanel", col3Go.transform);
            AddLE(lawsPanelGo, flexH: 1f);
            var lawsScroll = AddScrollableContent(lawsPanelGo);
            var lawsPanelComp = lawsPanelGo.AddComponent<LawsPanel>();
            lawsPanelComp.headerText = MakeTMP("LawsHeader", lawsScroll.transform, "Laws \u2014 Ready", 16, COL_TEXT_GOLD);
            AddLE(lawsPanelComp.headerText.gameObject, preferredH: 24);
            lawsPanelComp.lawContainer = lawsScroll.transform;

            // Emergency Orders
            var ordersPanelGo = MakeRect("EmergencyOrdersPanel", col3Go.transform);
            AddLE(ordersPanelGo, flexH: 1f);
            var ordersPanel = ordersPanelGo.AddComponent<EmergencyOrdersPanel>();
            BuildOrdersPanel(ordersPanel, ordersPanelGo);
            ordersPanelGo.SetActive(false);

            // Missions
            var missionsPanelGo = MakeRect("MissionsPanel", col3Go.transform);
            AddLE(missionsPanelGo, flexH: 1f);
            var missionsPanel = missionsPanelGo.AddComponent<MissionsPanel>();
            BuildMissionsPanel(missionsPanel, missionsPanelGo);
            missionsPanelGo.SetActive(false);

            // Evacuation
            var evacPanelGo = MakePanel("EvacuationPanel", col3Go.transform, new Color(0.25f, 0.12f, 0.12f, 0.9f));
            AddLE(evacPanelGo, preferredH: 105);
            var evacVlg = evacPanelGo.AddComponent<VerticalLayoutGroup>();
            evacVlg.childControlWidth = true;
            evacVlg.childForceExpandWidth = true;
            evacVlg.childControlHeight = true;
            evacVlg.childForceExpandHeight = false;
            evacVlg.spacing = 4;
            evacVlg.padding = new RectOffset(8, 8, 6, 6);
            var evacPanel = evacPanelGo.AddComponent<EvacuationPanel>();
            evacPanel.headerText = MakeTMP("EvacHeader", evacPanelGo.transform, "Evacuation", 16, COL_TEXT_GOLD);
            AddLE(evacPanel.headerText.gameObject, preferredH: 20);
            evacPanel.costText = MakeTMP("Cost", evacPanelGo.transform, "", 13, COL_TEXT_SECONDARY);
            AddLE(evacPanel.costText.gameObject, preferredH: 16);
            evacPanel.penaltiesText = MakeTMP("Penalties", evacPanelGo.transform, "", 13, Color.red);
            AddLE(evacPanel.penaltiesText.gameObject, preferredH: 16);
            evacPanel.evacuateButton = MakeButton("EvacBtn", evacPanelGo.transform, "EVACUATE ZONE", COL_BTN_DANGER, 14).GetComponent<Button>();
            AddLE(evacPanel.evacuateButton.gameObject, preferredH: 30);
            evacPanelGo.SetActive(false);

            // Tab switching
            lawsTabGo.GetComponent<Button>().onClick.AddListener(() =>
            {
                lawsPanelGo.SetActive(true); ordersPanelGo.SetActive(false); missionsPanelGo.SetActive(false);
                lawsTabImg.color = COL_TAB_ACTIVE; ordersTabImg.color = COL_TAB_INACTIVE; missionsTabImg.color = COL_TAB_INACTIVE;
            });
            ordersTabGo.GetComponent<Button>().onClick.AddListener(() =>
            {
                lawsPanelGo.SetActive(false); ordersPanelGo.SetActive(true); missionsPanelGo.SetActive(false);
                lawsTabImg.color = COL_TAB_INACTIVE; ordersTabImg.color = COL_TAB_ACTIVE; missionsTabImg.color = COL_TAB_INACTIVE;
            });
            missionsTabGo.GetComponent<Button>().onClick.AddListener(() =>
            {
                lawsPanelGo.SetActive(false); ordersPanelGo.SetActive(false); missionsPanelGo.SetActive(true);
                lawsTabImg.color = COL_TAB_INACTIVE; ordersTabImg.color = COL_TAB_INACTIVE; missionsTabImg.color = COL_TAB_ACTIVE;
            });

            // ── COL 4  INTELLIGENCE ──────────────────────────
            var col4Go = MakePanel("Col_Info", middleGo.transform, COL_BG_PANEL);
            AddLE(col4Go, flexW: 0.23f);
            var col4Vlg = col4Go.AddComponent<VerticalLayoutGroup>();
            col4Vlg.childControlWidth = true;
            col4Vlg.childControlHeight = true;
            col4Vlg.childForceExpandWidth = true;
            col4Vlg.childForceExpandHeight = false;
            col4Vlg.spacing = 3;
            col4Vlg.padding = new RectOffset(4, 4, 4, 4);

            // Why Am I Dying
            var whyPanelGo = MakePanel("WhyAmIDyingPanel", col4Go.transform, COL_BG_SECTION);
            AddLE(whyPanelGo, flexH: 0.40f);
            var whyVlg = whyPanelGo.AddComponent<VerticalLayoutGroup>();
            whyVlg.childControlWidth = true;
            whyVlg.childForceExpandWidth = true;
            whyVlg.childControlHeight = true;
            whyVlg.childForceExpandHeight = false;
            whyVlg.spacing = 4;
            whyVlg.padding = new RectOffset(8, 8, 6, 6);
            var whyPanel = whyPanelGo.AddComponent<WhyAmIDyingPanel>();
            whyPanel.headerText = MakeTMP("WhyHeader", whyPanelGo.transform, "SITUATION REPORT", 16, new Color(0.9f, 0.3f, 0.3f, 1f));
            AddLE(whyPanel.headerText.gameObject, preferredH: 22);
            whyPanel.pressure1Text = MakeTMP("P1", whyPanelGo.transform, "", 12, COL_TEXT_PRIMARY);
            whyPanel.pressure2Text = MakeTMP("P2", whyPanelGo.transform, "", 12, COL_TEXT_PRIMARY);
            whyPanel.pressure3Text = MakeTMP("P3", whyPanelGo.transform, "", 12, COL_TEXT_PRIMARY);

            // Event Log
            var eventLogGo = MakePanel("EventLogPanel", col4Go.transform, COL_BG_SECTION);
            AddLE(eventLogGo, flexH: 0.60f);
            var eventLogContent = AddScrollableContent(eventLogGo);
            var eventLogPanel = eventLogGo.AddComponent<EventLogPanel>();
            MakeTMP("LogHeader", eventLogContent.transform, "EVENT LOG", 14, COL_TEXT_GOLD);
            AddLE(eventLogContent.transform.GetChild(0).gameObject, preferredH: 20);
            eventLogPanel.logText = MakeTMP("LogText", eventLogContent.transform, "", 11, COL_TEXT_SECONDARY);
            eventLogPanel.scrollRect = eventLogGo.GetComponent<ScrollRect>();

            // ═══════════════════════════════════════════════════════
            //  BOTTOM BAR
            // ═══════════════════════════════════════════════════════
            var bottomGo = MakePanel("BottomBar", rootGo.transform, COL_BG_HEADER);
            AddLE(bottomGo, preferredH: 48);
            var bottomHlg = bottomGo.AddComponent<HorizontalLayoutGroup>();
            bottomHlg.childControlWidth = true;
            bottomHlg.childForceExpandWidth = true;
            bottomHlg.childControlHeight = true;
            bottomHlg.childForceExpandHeight = true;
            bottomHlg.padding = new RectOffset(300, 300, 4, 4);

            var endDayBtnGo = MakeButton("EndDayButton", bottomGo.transform, "\u25b6  END DAY  \u25c0", COL_BTN_DANGER, 22);
            var endDayBtn = endDayBtnGo.GetComponent<Button>();

            // ═══════════════════════════════════════════════════════
            //  OVERLAY PANELS
            // ═══════════════════════════════════════════════════════

            // Daily Report
            var reportOverlay = MakeOverlayPanel("DailyReportOverlay");
            var reportScroll = AddScrollableContent(reportOverlay);
            var reportPanel = reportOverlay.AddComponent<DailyReportPanel>();
            reportPanel.titleText = MakeTMP("Title", reportScroll.transform, "", 26, COL_TEXT_GOLD);
            AddLE(reportPanel.titleText.gameObject, preferredH: 36);
            reportPanel.reportBody = MakeTMP("Body", reportScroll.transform, "", 13, COL_TEXT_PRIMARY);
            reportPanel.scrollRect = reportOverlay.GetComponent<ScrollRect>();
            var reportContGo = MakeButton("ContinueBtn", reportOverlay.transform, "CONTINUE", COL_BTN_ACCENT, 18);
            AddLE(reportContGo, preferredH: 44);
            reportPanel.continueButton = reportContGo.GetComponent<Button>();
            reportOverlay.SetActive(false);

            // Game Over
            var gameOverGo = MakeOverlayPanel("GameOverPanel");
            gameOverGo.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
            var gameOverPanel = gameOverGo.AddComponent<GameOverPanel>();
            gameOverPanel.titleText = MakeTMP("Title", gameOverGo.transform, "GAME OVER", 36, Color.red);
            AddLE(gameOverPanel.titleText.gameObject, preferredH: 50);
            gameOverPanel.causeText = MakeTMP("Cause", gameOverGo.transform, "", 22, COL_TEXT_PRIMARY);
            AddLE(gameOverPanel.causeText.gameObject, preferredH: 32);
            gameOverPanel.statsText = MakeTMP("Stats", gameOverGo.transform, "", 14, COL_TEXT_SECONDARY);
            gameOverPanel.telemetryText = MakeTMP("Telemetry", gameOverGo.transform, "", 13, COL_TEXT_SECONDARY);
            var goRestartGo = MakeButton("RestartBtn", gameOverGo.transform, "RESTART", COL_BTN_ACCENT, 18);
            AddLE(goRestartGo, preferredH: 44);
            gameOverPanel.restartButton = goRestartGo.GetComponent<Button>();
            gameOverGo.SetActive(false);

            // Victory
            var victoryGo = MakeOverlayPanel("VictoryPanel");
            victoryGo.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
            var victoryPanel = victoryGo.AddComponent<VictoryPanel>();
            victoryPanel.titleText = MakeTMP("Title", victoryGo.transform, "SURVIVED \u2014 40 DAYS", 36, Color.green);
            AddLE(victoryPanel.titleText.gameObject, preferredH: 50);
            victoryPanel.statsText = MakeTMP("Stats", victoryGo.transform, "", 14, COL_TEXT_SECONDARY);
            victoryPanel.telemetryText = MakeTMP("Telemetry", victoryGo.transform, "", 13, COL_TEXT_SECONDARY);
            var vRestartGo = MakeButton("RestartBtn", victoryGo.transform, "RESTART", COL_BTN_ACCENT, 18);
            AddLE(vRestartGo, preferredH: 44);
            victoryPanel.restartButton = vRestartGo.GetComponent<Button>();
            victoryGo.SetActive(false);

            // 5. UIManager
            var uiMgrGo = new GameObject("UIManager");
            var uiMgr = uiMgrGo.AddComponent<UIManager>();
            uiMgr.topBarPanel = topBarGo;
            uiMgr.zonePanel = zonePanelGo;
            uiMgr.workerAllocationPanel = workerPanelGo;
            uiMgr.lawsPanel = lawsPanelGo;
            uiMgr.emergencyOrdersPanel = ordersPanelGo;
            uiMgr.missionsPanel = missionsPanelGo;
            uiMgr.evacuationPanel = evacPanelGo;
            uiMgr.dailyReportPanel = reportOverlay;
            uiMgr.whyAmIDyingPanel = whyPanelGo;
            uiMgr.eventLogPanel = eventLogGo;
            uiMgr.gameOverPanel = gameOverGo;
            uiMgr.victoryPanel = victoryGo;
            uiMgr.endDayButton = endDayBtnGo;

            endDayBtn.onClick.AddListener(() => uiMgr.OnEndDayClicked());
            gameOverPanel.restartButton.onClick.AddListener(() => uiMgr.OnRestartClicked());
            victoryPanel.restartButton.onClick.AddListener(() => uiMgr.OnRestartClicked());
            reportPanel.continueButton.onClick.AddListener(() => uiMgr.OnContinueClicked());

            Debug.Log("[SceneBootstrap] UI hierarchy created.");
        }

        // ═══════════════════════════════════════════════════════════
        //  BUILD SECTIONS
        // ═══════════════════════════════════════════════════════════

        private void BuildTopBar(TopBarPanel tb, GameObject go)
        {
            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 1;
            vlg.padding = new RectOffset(8, 8, 4, 4);

            // Row 1 — Day | Siege | Profile | Pop | Wells
            var row1 = MakeRect("InfoRow", go.transform);
            AddLE(row1, preferredH: 26);
            var r1Hlg = row1.AddComponent<HorizontalLayoutGroup>();
            r1Hlg.childControlWidth = true;
            r1Hlg.childForceExpandWidth = false;
            r1Hlg.childControlHeight = true;
            r1Hlg.childForceExpandHeight = true;
            r1Hlg.spacing = 20;
            r1Hlg.childAlignment = TextAnchor.MiddleLeft;

            tb.dayText = MakeTMP("Day", row1.transform, "Day 1 / 40", 20, COL_TEXT_GOLD);
            AddLE(tb.dayText.gameObject, preferredW: 160);
            tb.siegeText = MakeTMP("Siege", row1.transform, "Siege: 1/6", 16, new Color(0.85f, 0.45f, 0.2f));
            AddLE(tb.siegeText.gameObject, preferredW: 120);
            tb.profileText = MakeTMP("Profile", row1.transform, "", 14, COL_TEXT_SECONDARY);
            AddLE(tb.profileText.gameObject, flexW: 0.3f);
            tb.populationText = MakeTMP("Pop", row1.transform, "", 14, COL_TEXT_PRIMARY);
            AddLE(tb.populationText.gameObject, flexW: 0.3f);
            tb.wellsText = MakeTMP("Wells", row1.transform, "", 14, Color.red);
            AddLE(tb.wellsText.gameObject, preferredW: 160);

            // Row 2 — Resources (colour-coded with background chips)
            var row2 = MakePanel("ResourceRow", go.transform, new Color(0.07f, 0.07f, 0.11f, 0.9f));
            AddLE(row2, preferredH: 30);
            var r2Hlg = row2.AddComponent<HorizontalLayoutGroup>();
            r2Hlg.childControlWidth = true;
            r2Hlg.childForceExpandWidth = true;
            r2Hlg.childControlHeight = true;
            r2Hlg.childForceExpandHeight = true;
            r2Hlg.spacing = 4;
            r2Hlg.padding = new RectOffset(6, 6, 2, 2);

            tb.foodText      = MakeResourceChip("Food", row2.transform, "Food: 320", COL_FOOD);
            tb.waterText     = MakeResourceChip("Water", row2.transform, "Water: 360", COL_WATER);
            tb.fuelText      = MakeResourceChip("Fuel", row2.transform, "Fuel: 240", COL_FUEL);
            tb.medicineText  = MakeResourceChip("Medicine", row2.transform, "Med: 40", COL_MED);
            tb.materialsText = MakeResourceChip("Materials", row2.transform, "Mat: 120", COL_MAT);

            // Row 3 — Meters (actual Unity Slider bars)
            var row3 = MakeRect("MeterRow", go.transform);
            AddLE(row3, preferredH: 34);
            var r3Hlg = row3.AddComponent<HorizontalLayoutGroup>();
            r3Hlg.childControlWidth = true;
            r3Hlg.childForceExpandWidth = true;
            r3Hlg.childControlHeight = true;
            r3Hlg.childForceExpandHeight = true;
            r3Hlg.spacing = 8;
            r3Hlg.padding = new RectOffset(6, 6, 2, 2);

            BuildMeterSlider("Morale", row3.transform, "Morale: 55", out tb.moraleSlider, out tb.moraleText, out tb.moraleFill);
            BuildMeterSlider("Unrest", row3.transform, "Unrest: 25", out tb.unrestSlider, out tb.unrestText, out tb.unrestFill);
            BuildMeterSlider("Sickness", row3.transform, "Sickness: 20", out tb.sicknessSlider, out tb.sicknessText, out tb.sicknessFill);
        }

        private TextMeshProUGUI MakeResourceChip(string name, Transform parent, string text, Color color)
        {
            var container = MakePanel(name + "Bg", parent, new Color(color.r * 0.15f, color.g * 0.15f, color.b * 0.15f, 0.6f));
            var hlg = container.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandHeight = true;
            hlg.padding = new RectOffset(8, 8, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleCenter;

            // Colour pip
            var pip = MakeRect("Pip", container.transform);
            AddLE(pip, preferredW: 4);
            pip.AddComponent<Image>().color = color;

            var tmp = MakeTMP(name, container.transform, text, 15, color);
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        private void BuildMeterSlider(string name, Transform parent, string label,
            out Slider slider, out TextMeshProUGUI text, out Image fill)
        {
            var container = MakeRect(name + "Meter", parent);
            var hlg = container.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandHeight = true;
            hlg.spacing = 6;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            text = MakeTMP(name + "Label", container.transform, label, 13, COL_TEXT_PRIMARY);
            text.fontStyle = FontStyles.Bold;
            AddLE(text.gameObject, preferredW: 110);

            // Slider
            var sliderGo = MakeRect(name + "Slider", container.transform);
            AddLE(sliderGo, flexW: 1f, preferredH: 14);
            slider = sliderGo.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 100;
            slider.wholeNumbers = true;
            slider.interactable = false;

            var bgGo = MakeRect("Background", sliderGo.transform);
            StretchToParent(bgGo);
            bgGo.AddComponent<Image>().color = COL_SLIDER_BG;

            var fillAreaGo = MakeRect("Fill Area", sliderGo.transform);
            StretchToParent(fillAreaGo);
            var fillGo = MakeRect("Fill", fillAreaGo.transform);
            var fillRT = fillGo.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;
            fill = fillGo.AddComponent<Image>();
            fill.color = Color.green;

            slider.fillRect = fillRT;
            slider.targetGraphic = fill;
        }

        // ── ZONES ───────────────────────────────────────────

        private void BuildZonePanel(ZonePanel zp, GameObject go)
        {
            var hdr = MakeTMP("ZoneHeader", go.transform, "ZONES", 16, COL_TEXT_GOLD);
            hdr.fontStyle = FontStyles.Bold;
            AddLE(hdr.gameObject, preferredH: 22);

            zp.zoneRows = new ZoneRowUI[5];
            string[] names = { "Outer Farms", "Outer Residential", "Artisan Quarter", "Inner District", "Keep" };
            for (int i = 0; i < 5; i++)
            {
                var rowGo = MakePanel($"Zone_{i}", go.transform, i % 2 == 0 ? COL_BG_ROW_EVEN : COL_BG_ROW_ODD);
                AddLE(rowGo, preferredH: 56);
                var rowVlg = rowGo.AddComponent<VerticalLayoutGroup>();
                rowVlg.childControlWidth = true;
                rowVlg.childForceExpandWidth = true;
                rowVlg.childControlHeight = true;
                rowVlg.childForceExpandHeight = false;
                rowVlg.spacing = 1;
                rowVlg.padding = new RectOffset(6, 6, 3, 3);

                var row = new ZoneRowUI();
                row.background = rowGo.GetComponent<Image>();

                // Top: Name | Status | Integrity% | Pop
                var topRow = MakeRect("Top", rowGo.transform);
                AddLE(topRow, preferredH: 18);
                var topHlg = topRow.AddComponent<HorizontalLayoutGroup>();
                topHlg.childControlWidth = true;
                topHlg.childForceExpandWidth = false;
                topHlg.childControlHeight = true;
                topHlg.childForceExpandHeight = true;
                topHlg.spacing = 6;
                topHlg.childAlignment = TextAnchor.MiddleLeft;

                row.nameText = MakeTMP("Name", topRow.transform, names[i], 14, COL_TEXT_PRIMARY);
                row.nameText.fontStyle = FontStyles.Bold;
                AddLE(row.nameText.gameObject, preferredW: 130);
                row.statusText = MakeTMP("Status", topRow.transform, "", 11, Color.green);
                AddLE(row.statusText.gameObject, preferredW: 110);
                row.integrityText = MakeTMP("IntPct", topRow.transform, "", 12, COL_TEXT_SECONDARY);
                AddLE(row.integrityText.gameObject, preferredW: 55);
                row.populationText = MakeTMP("Pop", topRow.transform, "", 12, COL_TEXT_SECONDARY);
                AddLE(row.populationText.gameObject, preferredW: 50);

                // Bottom: integrity bar | overcrowding | bonus
                var botRow = MakeRect("Bot", rowGo.transform);
                AddLE(botRow, preferredH: 14);
                var botHlg = botRow.AddComponent<HorizontalLayoutGroup>();
                botHlg.childControlWidth = true;
                botHlg.childForceExpandWidth = false;
                botHlg.childControlHeight = true;
                botHlg.childForceExpandHeight = true;
                botHlg.spacing = 4;
                botHlg.childAlignment = TextAnchor.MiddleLeft;

                // Integrity slider
                var intSliderGo = MakeRect("IntegritySlider", botRow.transform);
                AddLE(intSliderGo, flexW: 0.5f, preferredH: 10);
                row.integritySlider = intSliderGo.AddComponent<Slider>();
                row.integritySlider.minValue = 0;
                row.integritySlider.maxValue = 100;
                row.integritySlider.wholeNumbers = true;
                row.integritySlider.interactable = false;

                var intBg = MakeRect("IntBg", intSliderGo.transform);
                StretchToParent(intBg);
                intBg.AddComponent<Image>().color = COL_SLIDER_BG;

                var intFillArea = MakeRect("IntFillArea", intSliderGo.transform);
                StretchToParent(intFillArea);
                var intFillGo = MakeRect("IntFill", intFillArea.transform);
                var intFillRT = intFillGo.GetComponent<RectTransform>();
                intFillRT.anchorMin = Vector2.zero;
                intFillRT.anchorMax = Vector2.one;
                intFillRT.offsetMin = Vector2.zero;
                intFillRT.offsetMax = Vector2.zero;
                row.integrityFill = intFillGo.AddComponent<Image>();
                row.integrityFill.color = Color.green;
                row.integritySlider.fillRect = intFillRT;

                row.overcrowdingText = MakeTMP("Overcrowd", botRow.transform, "", 10, Color.red);
                AddLE(row.overcrowdingText.gameObject, preferredW: 90);
                row.bonusText = MakeTMP("Bonus", botRow.transform, "", 10, new Color(0.4f, 0.8f, 0.4f));
                AddLE(row.bonusText.gameObject, flexW: 0.4f);

                zp.zoneRows[i] = row;
            }
        }

        private void BuildZoneRing(ZoneRingWidget zr, GameObject go)
        {
            zr.zoneRings = new Image[5];
            float size = 1.0f;
            for (int i = 0; i < 5; i++)
            {
                var ringGo = new GameObject($"Ring_{i}");
                ringGo.transform.SetParent(go.transform, false);
                var img = ringGo.AddComponent<Image>();
                img.color = zr.intactColor;
                var rt = ringGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f - size / 2f, 0.5f - size / 2f);
                rt.anchorMax = new Vector2(0.5f + size / 2f, 0.5f + size / 2f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                zr.zoneRings[i] = img;
                size -= 0.16f;
            }
        }

        // ── WORKERS ─────────────────────────────────────────

        private void BuildWorkerPanel(WorkerAllocationPanel wp, GameObject go)
        {
            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 3;
            vlg.padding = new RectOffset(6, 6, 6, 6);

            wp.headerText = MakeTMP("Header", go.transform, "WORKER ALLOCATION", 16, COL_TEXT_GOLD);
            wp.headerText.fontStyle = FontStyles.Bold;
            AddLE(wp.headerText.gameObject, preferredH: 22);
            wp.idleWarningText = MakeTMP("IdleWarn", go.transform, "", 12, Color.red);
            AddLE(wp.idleWarningText.gameObject, preferredH: 18);
            MakeSeparator(go.transform);

            wp.foodRow       = BuildJobRow("FoodRow",       go.transform, "Food Production", COL_FOOD, 0);
            wp.waterRow      = BuildJobRow("WaterRow",      go.transform, "Water Drawing",   COL_WATER, 1);
            wp.materialsRow  = BuildJobRow("MaterialsRow",  go.transform, "Materials",        COL_MAT, 0);
            wp.repairsRow    = BuildJobRow("RepairsRow",    go.transform, "Repairs",           COL_TEXT_SECONDARY, 1);
            wp.sanitationRow = BuildJobRow("SanitationRow", go.transform, "Sanitation",        new Color(0.5f, 0.9f, 0.7f), 0);
            wp.clinicRow     = BuildJobRow("ClinicRow",     go.transform, "Clinic Staff",      COL_MED, 1);
            wp.fuelRow       = BuildJobRow("FuelRow",       go.transform, "Fuel Scavenging",   COL_FUEL, 0);

            MakeSeparator(go.transform);
            wp.guardInfoText = MakeTMP("GuardInfo", go.transform, "Guards: 10", 13, COL_TEXT_SECONDARY);
            AddLE(wp.guardInfoText.gameObject, preferredH: 20);
        }

        private WorkerJobRowUI BuildJobRow(string name, Transform parent, string label, Color labelColor, int parity)
        {
            var rowGo = MakePanel(name, parent, parity == 0 ? COL_BG_ROW_EVEN : COL_BG_ROW_ODD);
            AddLE(rowGo, preferredH: 50);
            var vlg = rowGo.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 1;
            vlg.padding = new RectOffset(6, 6, 3, 3);

            // Top row: Label | Assigned | -5 | +5
            var topRow = MakeRect("Top", rowGo.transform);
            AddLE(topRow, preferredH: 20);
            var topHlg = topRow.AddComponent<HorizontalLayoutGroup>();
            topHlg.childControlWidth = true;
            topHlg.childForceExpandWidth = false;
            topHlg.childControlHeight = true;
            topHlg.childForceExpandHeight = true;
            topHlg.spacing = 4;
            topHlg.childAlignment = TextAnchor.MiddleLeft;

            var row = new WorkerJobRowUI();
            row.labelText = MakeTMP("Label", topRow.transform, label, 13, labelColor);
            row.labelText.fontStyle = FontStyles.Bold;
            AddLE(row.labelText.gameObject, flexW: 0.45f);
            row.assignedText = MakeTMP("Assigned", topRow.transform, "Assigned: 0", 13, COL_TEXT_PRIMARY);
            AddLE(row.assignedText.gameObject, preferredW: 90);
            row.minusButton = MakeButton("Minus", topRow.transform, "\u22125", COL_BTN, 13).GetComponent<Button>();
            AddLE(row.minusButton.gameObject, preferredW: 36, preferredH: 22);
            row.plusButton = MakeButton("Plus", topRow.transform, "+5", COL_BTN_ACCENT, 13).GetComponent<Button>();
            AddLE(row.plusButton.gameObject, preferredW: 36, preferredH: 22);

            // Bottom row: projection | warning
            var botRow = MakeRect("Bot", rowGo.transform);
            AddLE(botRow, preferredH: 16);
            var botHlg = botRow.AddComponent<HorizontalLayoutGroup>();
            botHlg.childControlWidth = true;
            botHlg.childForceExpandWidth = true;
            botHlg.childControlHeight = true;
            botHlg.childForceExpandHeight = true;
            botHlg.spacing = 4;
            botHlg.childAlignment = TextAnchor.MiddleLeft;

            row.projectionText = MakeTMP("Proj", botRow.transform, "", 11, COL_TEXT_SECONDARY);
            row.warningText = MakeTMP("Warn", botRow.transform, "", 11, Color.red);
            row.warningText.gameObject.SetActive(false);

            return row;
        }

        // ── EMERGENCY ORDERS ────────────────────────────────

        private void BuildOrdersPanel(EmergencyOrdersPanel op, GameObject go)
        {
            var scroll = AddScrollableContent(go);
            op.headerText = MakeTMP("OrdersHeader", scroll.transform, "Emergency Orders \u2014 1 per day", 16, COL_TEXT_GOLD);
            op.headerText.fontStyle = FontStyles.Bold;
            AddLE(op.headerText.gameObject, preferredH: 24);

            op.o1Card = BuildOrderCard("O1", scroll.transform, 0);
            op.o2Card = BuildOrderCard("O2", scroll.transform, 1);
            op.o3Card = BuildOrderCard("O3", scroll.transform, 0);
            op.o4Card = BuildOrderCard("O4", scroll.transform, 1);
            op.o5Card = BuildOrderCard("O5", scroll.transform, 0);
            op.o6Card = BuildOrderCard("O6", scroll.transform, 1);

            // Zone selector (for O5)
            var zoneSel = MakePanel("ZoneSelector", scroll.transform, new Color(0.15f, 0.15f, 0.3f, 0.9f));
            AddLE(zoneSel, preferredH: 38);
            var zsHlg = zoneSel.AddComponent<HorizontalLayoutGroup>();
            zsHlg.childControlWidth = true;
            zsHlg.childForceExpandWidth = true;
            zsHlg.childControlHeight = true;
            zsHlg.childForceExpandHeight = true;
            zsHlg.spacing = 2;
            zsHlg.padding = new RectOffset(4, 4, 4, 4);
            op.zoneSelectorPanel = zoneSel;
            op.zoneSelectButtons = new Button[5];
            string[] zn = { "Farms", "Outer", "Artisan", "Inner", "Keep" };
            for (int i = 0; i < 5; i++)
                op.zoneSelectButtons[i] = MakeButton($"ZS_{i}", zoneSel.transform, zn[i], COL_BTN, 11).GetComponent<Button>();
            zoneSel.SetActive(false);
        }

        private OrderCardUI BuildOrderCard(string name, Transform parent, int parity)
        {
            var cardGo = MakePanel(name, parent, parity == 0 ? COL_BG_ROW_EVEN : COL_BG_ROW_ODD);
            AddLE(cardGo, preferredH: 68);
            var vlg = cardGo.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 1;
            vlg.padding = new RectOffset(6, 6, 4, 4);

            var card = new OrderCardUI();

            // Name + ISSUE button
            var topRow = MakeRect("Top", cardGo.transform);
            AddLE(topRow, preferredH: 20);
            var topHlg = topRow.AddComponent<HorizontalLayoutGroup>();
            topHlg.childControlWidth = true;
            topHlg.childForceExpandWidth = false;
            topHlg.childControlHeight = true;
            topHlg.childForceExpandHeight = true;
            topHlg.spacing = 4;
            topHlg.childAlignment = TextAnchor.MiddleLeft;

            card.nameText = MakeTMP("Name", topRow.transform, "", 13, COL_TEXT_PRIMARY);
            card.nameText.fontStyle = FontStyles.Bold;
            AddLE(card.nameText.gameObject, flexW: 1f);
            card.issueButton = MakeButton("Issue", topRow.transform, "ISSUE", COL_BTN_ACCENT, 12).GetComponent<Button>();
            AddLE(card.issueButton.gameObject, preferredW: 60, preferredH: 22);

            card.costText = MakeTMP("Cost", cardGo.transform, "", 11, new Color(0.9f, 0.6f, 0.3f));
            AddLE(card.costText.gameObject, preferredH: 14);
            card.effectText = MakeTMP("Effect", cardGo.transform, "", 11, COL_TEXT_SECONDARY);
            AddLE(card.effectText.gameObject, preferredH: 14);

            return card;
        }

        // ── MISSIONS ────────────────────────────────────────

        private void BuildMissionsPanel(MissionsPanel mp, GameObject go)
        {
            var scroll = AddScrollableContent(go);
            mp.headerText = MakeTMP("MHeader", scroll.transform, "Missions \u2014 Requires 10 Workers", 16, COL_TEXT_GOLD);
            mp.headerText.fontStyle = FontStyles.Bold;
            AddLE(mp.headerText.gameObject, preferredH: 24);

            // Active mission display
            mp.missionActiveDisplay = MakePanel("ActiveMission", scroll.transform, new Color(0.2f, 0.15f, 0.08f, 0.9f));
            AddLE(mp.missionActiveDisplay, preferredH: 30);
            var amVlg = mp.missionActiveDisplay.AddComponent<VerticalLayoutGroup>();
            amVlg.childControlWidth = true;
            amVlg.childForceExpandWidth = true;
            amVlg.padding = new RectOffset(8, 8, 4, 4);
            mp.missionActiveText = MakeTMP("ActiveText", mp.missionActiveDisplay.transform, "", 13, COL_TEXT_GOLD);

            // Selection container
            var selGo = MakeRect("MissionSelection", scroll.transform);
            mp.missionSelectionContainer = selGo;
            var selVlg = selGo.AddComponent<VerticalLayoutGroup>();
            selVlg.childControlWidth = true;
            selVlg.childForceExpandWidth = true;
            selVlg.childControlHeight = true;
            selVlg.childForceExpandHeight = false;
            selVlg.spacing = 4;

            mp.m1Card = BuildMissionCard("M1", selGo.transform, 0);
            mp.m2Card = BuildMissionCard("M2", selGo.transform, 1);
            mp.m3Card = BuildMissionCard("M3", selGo.transform, 0);
            mp.m4Card = BuildMissionCard("M4", selGo.transform, 1);
        }

        private MissionCardUI BuildMissionCard(string name, Transform parent, int parity)
        {
            var cardGo = MakePanel(name, parent, parity == 0 ? COL_BG_ROW_EVEN : COL_BG_ROW_ODD);
            AddLE(cardGo, preferredH: 76);
            var vlg = cardGo.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 2;
            vlg.padding = new RectOffset(6, 6, 4, 4);

            var card = new MissionCardUI();

            var topRow = MakeRect("Top", cardGo.transform);
            AddLE(topRow, preferredH: 22);
            var topHlg = topRow.AddComponent<HorizontalLayoutGroup>();
            topHlg.childControlWidth = true;
            topHlg.childForceExpandWidth = false;
            topHlg.childControlHeight = true;
            topHlg.childForceExpandHeight = true;
            topHlg.spacing = 4;
            topHlg.childAlignment = TextAnchor.MiddleLeft;

            card.nameText = MakeTMP("Name", topRow.transform, "", 13, COL_TEXT_PRIMARY);
            card.nameText.fontStyle = FontStyles.Bold;
            AddLE(card.nameText.gameObject, flexW: 1f);
            card.launchButton = MakeButton("Launch", topRow.transform, "LAUNCH", COL_BTN_ACCENT, 12).GetComponent<Button>();
            AddLE(card.launchButton.gameObject, preferredW: 70, preferredH: 22);

            card.oddsText = MakeTMP("Odds", cardGo.transform, "", 11, COL_TEXT_SECONDARY);
            card.oddsText.enableWordWrapping = true;
            AddLE(card.oddsText.gameObject, preferredH: 28);
            card.extraInfoText = MakeTMP("Extra", cardGo.transform, "", 11, COL_TEXT_SECONDARY);
            AddLE(card.extraInfoText.gameObject, preferredH: 14);

            return card;
        }

        // ═══════════════════════════════════════════════════════════
        //  UI PRIMITIVES
        // ═══════════════════════════════════════════════════════════

        private static GameObject MakeRect(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static GameObject MakePanel(string name, Transform parent, Color bg)
        {
            var go = MakeRect(name, parent);
            go.AddComponent<Image>().color = bg;
            return go;
        }

        private static TextMeshProUGUI MakeTMP(string name, Transform parent, string text, float fontSize, Color color)
        {
            var go = MakeRect(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.enableAutoSizing = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.richText = true;
            return tmp;
        }

        private static GameObject MakeButton(string name, Transform parent, string label, Color bg, float fontSize)
        {
            var go = MakeRect(name, parent);
            var img = go.AddComponent<Image>();
            img.color = bg;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colours = btn.colors;
            colours.normalColor = Color.white;
            colours.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            colours.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colours.disabledColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            btn.colors = colours;

            var txtGo = MakeRect("Text", go.transform);
            StretchToParent(txtGo);
            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = fontSize;
            tmp.color = COL_TEXT_PRIMARY;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            return go;
        }

        private static void MakeSeparator(Transform parent)
        {
            var go = MakeRect("Sep", parent);
            AddLE(go, preferredH: 1);
            go.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.4f, 0.5f);
        }

        private GameObject MakeOverlayPanel(string name)
        {
            var go = MakeRect(name, _canvasGO.transform);
            StretchToParent(go);
            go.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.07f, 0.97f);
            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 8;
            vlg.padding = new RectOffset(60, 60, 40, 40);
            return go;
        }

        private static GameObject AddScrollableContent(GameObject parent)
        {
            var sr = parent.AddComponent<ScrollRect>();
            var viewport = MakeRect("Viewport", parent.transform);
            StretchToParent(viewport);
            viewport.AddComponent<Image>().color = COL_TRANSPARENT;
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            var content = MakeRect("Content", viewport.transform);
            var cRT = content.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1);
            cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(0.5f, 1);
            cRT.sizeDelta = Vector2.zero;

            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 2;

            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.viewport = viewport.GetComponent<RectTransform>();
            sr.content = cRT;
            sr.horizontal = false;
            sr.vertical = true;
            sr.scrollSensitivity = 20f;
            return content;
        }

        // ── layout helpers ──────────────────────────────────

        private static void StretchToParent(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void StretchToParent(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void AddLE(GameObject go, float preferredW = -1, float preferredH = -1, float flexW = -1, float flexH = -1)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            if (preferredW >= 0) le.preferredWidth = preferredW;
            if (preferredH >= 0) le.preferredHeight = preferredH;
            if (flexW >= 0) le.flexibleWidth = flexW;
            if (flexH >= 0) le.flexibleHeight = flexH;
        }
    }

    /// <summary>
    /// Shows/hides the Wells Repair button based on game state.
    /// </summary>
    public class WellsRepairButtonToggle : MonoBehaviour
    {
        private GameManager _gm;
        private Button _btn;

        private void Start()
        {
            _gm = GameManager.Instance;
            _btn = GetComponent<Button>();
            _gm.OnStateChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_gm != null) _gm.OnStateChanged -= Refresh;
        }

        private void Refresh()
        {
            bool show = _gm != null && _gm.State != null && _gm.State.wellsDamaged;
            gameObject.SetActive(show);
            if (_btn && show) _btn.interactable = _gm.CanRepairWells();
        }
    }
}
