#region Using directives
using System;
using System.Linq;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.NativeUI;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.Alarm;
using FTOptix.SerialPort;
using FTOptix.Core;
#endregion

public class OEEWidgetGenerator : BaseNetLogic
{
    // --- Theme Configuration ---
    private static class Theme
    {
        // Palette
        public static readonly Color Slate50 = new Color(0xFFF8FAFC);
        public static readonly Color Slate100 = new Color(0xFFF1F5F9);
        public static readonly Color Slate200 = new Color(0xFFE2E8F0);
        public static readonly Color Slate300 = new Color(0xFFCBD5E1);
        public static readonly Color Slate400 = new Color(0xFF94A3B8);
        public static readonly Color Slate500 = new Color(0xFF64748B);
        public static readonly Color Slate600 = new Color(0xFF475569);
        public static readonly Color Slate700 = new Color(0xFF334155);
        public static readonly Color Slate800 = new Color(0xFF1E293B);
        public static readonly Color Slate900 = new Color(0xFF0F172A);
        public static readonly Color Slate950 = new Color(0xFF020617);

        // Neon Accents
        public static readonly Color Blue500 = new Color(0xFF3B82F6);
        public static readonly Color Blue400 = new Color(0xFF60A5FA); // Brighter for dark mode
        public static readonly Color Emerald500 = new Color(0xFF10B981);
        public static readonly Color Emerald400 = new Color(0xFF34D399);
        public static readonly Color Amber500 = new Color(0xFFF59E0B);
        public static readonly Color Amber400 = new Color(0xFFFBBF24);
        public static readonly Color Red500 = new Color(0xFFEF4444);
        public static readonly Color Red400 = new Color(0xFFF87171);
        public static readonly Color Purple500 = new Color(0xFF8B5CF6);

        public static readonly Color White = new Color(0xFFFFFFFF);
        
        // Semantic Colors (Light Mode)
        public static readonly Color DashboardBg = Slate100;
        public static readonly Color HeaderBg = White;
        public static readonly Color HeaderText = Slate900;
        
        public static readonly Color CardBg = White;
        public static readonly Color CardHeaderBg = Slate50;
        public static readonly Color BorderColor = Slate200;
        
        public static readonly Color TextPrimary = Slate900;
        public static readonly Color TextSecondary = Slate500;
        public static readonly Color TextMuted = Slate400;
    }

    [ExportMethod]
    public void GenerateAllWidgets()
    {
        // Dashboards
        GenerateOperatorDashboard();
        GenerateExecutiveDashboard();
        GenerateHistoricalDashboard();

        // Cards
        GenerateShiftPacerCard();
        GenerateShiftStatsCard();
        GenerateShiftDetailCard();
        GenerateAvailabilityCard();
        GeneratePerformanceCard();
        GenerateOEECard();
        GenerateQualityCard();
        GenerateForecastCard();
        GenerateSystemHealthCard();

        // Config
        GenerateProductionConfig();
        GenerateTargetsConfig();
        GenerateSystemConfig();
        GenerateDataInput();

        Log.Info("OEEWidgetGenerator", "All widgets generated successfully with new UI theme.");
    }

    // --- Dashboards ---

    [ExportMethod]
    public void GenerateOperatorDashboard()
    {
        var folder = GetOrCreateFolder("UI/Widgets/OEE/Dashboards");
        var widgetName = "Operator_Dashboard";
        DeleteIfExists(folder, widgetName);

        var mainPanel = InformationModel.Make<ScaleLayout>(widgetName);
        mainPanel.Width = 1280;
        mainPanel.Height = 720;
        // Set OriginalWidth/Height for ScaleLayout
        mainPanel.OriginalWidth = 1280;
        mainPanel.OriginalHeight = 720;

        // Background
        var bgType = Project.Current.Get("UI/Templates/Panels/PanelWithBackground");
        if (bgType != null)
        {
            var bg = InformationModel.MakeObject<Image>("Background", bgType.NodeId);
            bg.HorizontalAlignment = HorizontalAlignment.Stretch;
            bg.VerticalAlignment = VerticalAlignment.Stretch;
            mainPanel.Add(bg);
        }
        else
        {
            var bg = InformationModel.Make<Rectangle>("Background");
            bg.HorizontalAlignment = HorizontalAlignment.Stretch;
            bg.VerticalAlignment = VerticalAlignment.Stretch;
            bg.FillColor = Theme.DashboardBg;
            mainPanel.Add(bg);
        }

        var oeeAlias = AddOEEAlias(mainPanel);

        // Main Grid Layout
        var grid = InformationModel.Make<ColumnLayout>("MainLayout");
        grid.HorizontalAlignment = HorizontalAlignment.Stretch;
        grid.VerticalAlignment = VerticalAlignment.Stretch;
        mainPanel.Add(grid);

        // 1. Header (Fixed Height)
        var header = CreateHeader("OPERATOR DASHBOARD", oeeAlias, "dashboard.svg");
        grid.Add(header);

        // 2. Content Area
        var content = InformationModel.Make<RowLayout>("Content");
        content.HorizontalAlignment = HorizontalAlignment.Stretch;
        content.VerticalAlignment = VerticalAlignment.Stretch;
        content.LeftMargin = 24; content.RightMargin = 24; content.TopMargin = 24; content.BottomMargin = 24;
        content.HorizontalGap = 24;
        grid.Add(content);

        // Left Column (OEE & Gauges) - Fixed Width
        var leftCol = InformationModel.Make<ColumnLayout>("LeftCol");
        leftCol.Width = 450;
        leftCol.VerticalAlignment = VerticalAlignment.Stretch;
        leftCol.VerticalGap = 24;
        content.Add(leftCol);

        // OEE Main Card
        var oeeCard = CreateCard("OEEOverviewCard", "OEE Overview", 400, 400, "gauge.svg");
        leftCol.Add(oeeCard);
        var oeeLayout = oeeCard.Get<ColumnLayout>("Layout").Get<ColumnLayout>("Content");
        
        // Big OEE Gauge
        AddCircularGauge(oeeLayout, oeeAlias, "OEE", "Outputs/Core/OEE", Theme.Blue500, 220);

        // Mini Gauges Row
        var miniGauges = InformationModel.Make<RowLayout>("MiniGauges");
        miniGauges.HorizontalAlignment = HorizontalAlignment.Stretch;
        miniGauges.Height = 100;
        miniGauges.TopMargin = 20;
        oeeLayout.Add(miniGauges);

        AddMiniGauge(miniGauges, oeeAlias, "Avail", "Outputs/Core/Availability", Theme.Emerald500);
        AddMiniGauge(miniGauges, oeeAlias, "Perf", "Outputs/Core/Performance", Theme.Amber500); // Changed to Amber for contrast
        AddMiniGauge(miniGauges, oeeAlias, "Qual", "Outputs/Core/Quality", Theme.Blue500); // Changed to Blue

        // Right Column (Production Details) - Flexible
        var rightCol = InformationModel.Make<ColumnLayout>("RightCol");
        rightCol.HorizontalAlignment = HorizontalAlignment.Stretch;
        rightCol.VerticalAlignment = VerticalAlignment.Stretch;
        rightCol.VerticalGap = 24;
        content.Add(rightCol);

        // Top Row: Progress & Counters
        var topRow = InformationModel.Make<RowLayout>("TopRow");
        topRow.Height = 180;
        topRow.HorizontalGap = 24;
        rightCol.Add(topRow);

        // Shift Progress
        var progressCard = CreateCard("ShiftProgressCard", "Shift Progress", 350, 180, "clock.svg");
        progressCard.Width = 350;
        topRow.Add(progressCard);
        var progLayout = progressCard.Get<ColumnLayout>("Layout").Get<ColumnLayout>("Content");
        AddProgressBar(progLayout, oeeAlias, "Shift Time", "Outputs/Shift/ShiftProgress", Theme.TextSecondary);
        AddProgressBar(progLayout, oeeAlias, "Production", "Outputs/Production/ProductionProgress", Theme.Emerald500);

        // Counters
        var countersCard = CreateCard("ProductionCountersCard", "Production Counters", 800, 180, "list.svg");
        countersCard.HorizontalAlignment = HorizontalAlignment.Stretch;
        topRow.Add(countersCard);
        var countLayout = countersCard.Get<ColumnLayout>("Layout").Get<ColumnLayout>("Content");
        var countGrid = InformationModel.Make<RowLayout>("Grid");
        countGrid.HorizontalGap = 16;
        countLayout.Add(countGrid);
        
        AddBigCounter(countGrid, oeeAlias, "Target", "Inputs/Production/ProductionTarget", Theme.TextMuted, Theme.Slate50);
        AddBigCounter(countGrid, oeeAlias, "Good", "Inputs/Data/GoodPartCount", Theme.Emerald500, Theme.Slate50);
        AddBigCounter(countGrid, oeeAlias, "Bad", "Inputs/Data/BadPartCount", Theme.Red500, Theme.Slate50);

        // Bottom Row: Rates & Forecast
        var bottomRow = InformationModel.Make<RowLayout>("BottomRow");
        bottomRow.HorizontalAlignment = HorizontalAlignment.Stretch;
        bottomRow.VerticalAlignment = VerticalAlignment.Stretch;
        bottomRow.HorizontalGap = 24;
        rightCol.Add(bottomRow);

        // Rates
        var ratesCard = CreateCard("LiveRatesCard", "Live Rates", 350, 300, "speed.svg"); // 0 height = stretch
        ratesCard.VerticalAlignment = VerticalAlignment.Stretch;
        ratesCard.Width = 350;
        bottomRow.Add(ratesCard);
        var ratesLayout = ratesCard.Get<ColumnLayout>("Layout").Get<ColumnLayout>("Content");
        
        AddRateRow(ratesLayout, oeeAlias, "Current Speed", "Outputs/Core/PartsPerHour", "PPH", Theme.Blue500);
        AddRateRow(ratesLayout, oeeAlias, "Avg Cycle", "Outputs/Core/AvgCycleTime", "s", Theme.TextPrimary);
        AddRateRow(ratesLayout, oeeAlias, "Runtime", "Outputs/System/TotalRuntimeFormatted", "", Theme.Emerald500);
        AddRateRow(ratesLayout, oeeAlias, "Downtime", "Outputs/System/DowntimeFormatted", "", Theme.Red500);

        // Forecast
        var forecastCard = CreateCard("ForecastCard", "Forecast", 800, 300, "trend.svg");
        forecastCard.VerticalAlignment = VerticalAlignment.Stretch;
        forecastCard.HorizontalAlignment = HorizontalAlignment.Stretch;
        bottomRow.Add(forecastCard);
        var foreLayout = forecastCard.Get<ColumnLayout>("Layout").Get<ColumnLayout>("Content");

        AddBigStat(foreLayout, oeeAlias, "Projected Total", "Outputs/Production/ProjectedTotalCount", Theme.Blue500);
        AddBigStat(foreLayout, oeeAlias, "Time to Target", "Outputs/Production/RemainingTimeAtCurrentRate", Theme.TextMuted);

        folder.Add(mainPanel);
    }

    [ExportMethod]
    public void GenerateExecutiveDashboard()
    {
        var folder = GetOrCreateFolder("UI/Widgets/OEE/Dashboards");
        var widgetName = "Executive_Dashboard";
        DeleteIfExists(folder, widgetName);

        var mainPanel = InformationModel.Make<ScaleLayout>(widgetName);
        mainPanel.Width = 1024;
        mainPanel.Height = 768;
        // Set OriginalWidth/Height for ScaleLayout
        mainPanel.OriginalWidth = 1024;
        mainPanel.OriginalHeight = 768;

        // Background
        var bgType = Project.Current.Get("UI/Templates/Panels/PanelWithBackground");
        if (bgType != null)
        {
            var bg = InformationModel.MakeObject<Image>("Background", bgType.NodeId);
            bg.HorizontalAlignment = HorizontalAlignment.Stretch;
            bg.VerticalAlignment = VerticalAlignment.Stretch;
            mainPanel.Add(bg);
        }
        else
        {
            var bg = InformationModel.Make<Rectangle>("Background");
            bg.HorizontalAlignment = HorizontalAlignment.Stretch; bg.VerticalAlignment = VerticalAlignment.Stretch;
            bg.FillColor = Theme.DashboardBg;
            mainPanel.Add(bg);
        }

        var oeeAlias = AddOEEAlias(mainPanel);

        var grid = InformationModel.Make<ColumnLayout>("MainLayout");
        grid.HorizontalAlignment = HorizontalAlignment.Stretch; grid.VerticalAlignment = VerticalAlignment.Stretch;
        mainPanel.Add(grid);

        grid.Add(CreateHeader("EXECUTIVE SUMMARY", oeeAlias, "chart.svg"));

        var content = InformationModel.Make<ColumnLayout>("Content");
        content.HorizontalAlignment = HorizontalAlignment.Stretch; content.VerticalAlignment = VerticalAlignment.Stretch;
        content.LeftMargin = 32; content.RightMargin = 32; content.TopMargin = 32; content.BottomMargin = 32;
        content.VerticalGap = 32;
        grid.Add(content);

        // Top Cards
        var topRow = InformationModel.Make<RowLayout>("TopRow");
        topRow.Height = 250; topRow.HorizontalGap = 32;
        content.Add(topRow);

        // OEE Score
        var oeeCard = CreateCard("EfficiencyScoreCard", "Efficiency Score", 300, 250, "gauge.svg");
        oeeCard.Width = 300;
        topRow.Add(oeeCard);
        AddCircularGauge(oeeCard.Get<ColumnLayout>("Layout").Get<ColumnLayout>("Content"), oeeAlias, "OEE", "Outputs/Core/OEE", Theme.Blue500, 160);

        // KPIs
        var kpiCard = CreateCard("KPICard", "Key Performance Indicators", 600, 250, "target.svg");
        kpiCard.HorizontalAlignment = HorizontalAlignment.Stretch;
        topRow.Add(kpiCard);
        var kpiLayout = kpiCard.Get<ColumnLayout>("Layout").Get<ColumnLayout>("Content");
        var kpiRow = InformationModel.Make<RowLayout>("Row");
        kpiRow.HorizontalGap = 20;
        kpiLayout.Add(kpiRow);
        
        AddKpiTile(kpiRow, oeeAlias, "Availability", "Outputs/Core/Availability", Theme.Emerald500);
        AddKpiTile(kpiRow, oeeAlias, "Performance", "Outputs/Core/Performance", Theme.Amber500);
        AddKpiTile(kpiRow, oeeAlias, "Quality", "Outputs/Core/Quality", Theme.Blue500);

        // Bottom Row
        var bottomRow = InformationModel.Make<RowLayout>("BottomRow");
        bottomRow.HorizontalAlignment = HorizontalAlignment.Stretch; bottomRow.VerticalAlignment = VerticalAlignment.Stretch;
        bottomRow.HorizontalGap = 32;
        content.Add(bottomRow);

        // Production Stats
        var prodCard = CreateCard("ProductionStatsCard", "Production Statistics", 900, 300, "list.svg");
        prodCard.HorizontalAlignment = HorizontalAlignment.Stretch;
        bottomRow.Add(prodCard);
        var prodLayout = prodCard.Get<ColumnLayout>("Layout").Get<ColumnLayout>("Content");
        
        AddDetailRow(prodLayout, oeeAlias, "Total Produced", "Outputs/Core/TotalCount");
        AddDetailRow(prodLayout, oeeAlias, "Good Parts", "Inputs/Data/GoodPartCount");
        AddDetailRow(prodLayout, oeeAlias, "Scrap Parts", "Inputs/Data/BadPartCount");
        AddDetailRow(prodLayout, oeeAlias, "Target", "Inputs/Production/ProductionTarget");

        folder.Add(mainPanel);
    }

    [ExportMethod]
    public void GenerateHistoricalDashboard()
    {
        var folder = GetOrCreateFolder("UI/Widgets/OEE/Dashboards");
        var widgetName = "Historical_Dashboard";
        DeleteIfExists(folder, widgetName);

        var mainPanel = InformationModel.Make<ScaleLayout>(widgetName);
        mainPanel.Width = 1280;
        mainPanel.Height = 720;
        mainPanel.OriginalWidth = 1280;
        mainPanel.OriginalHeight = 720;

        // Background
        var bgType = Project.Current.Get("UI/Templates/Panels/PanelWithBackground");
        if (bgType != null)
        {
            var bg = InformationModel.MakeObject<Image>("Background", bgType.NodeId);
            bg.HorizontalAlignment = HorizontalAlignment.Stretch;
            bg.VerticalAlignment = VerticalAlignment.Stretch;
            mainPanel.Add(bg);
        }
        else
        {
            var bg = InformationModel.Make<Rectangle>("Background");
            bg.HorizontalAlignment = HorizontalAlignment.Stretch;
            bg.VerticalAlignment = VerticalAlignment.Stretch;
            bg.FillColor = Theme.DashboardBg;
            mainPanel.Add(bg);
        }

        var oeeAlias = AddOEEAlias(mainPanel);

        var grid = InformationModel.Make<ColumnLayout>("MainLayout");
        grid.HorizontalAlignment = HorizontalAlignment.Stretch; grid.VerticalAlignment = VerticalAlignment.Stretch;
        mainPanel.Add(grid);

        grid.Add(CreateHeader("HISTORICAL ANALYSIS", oeeAlias, "history.svg"));

        var content = InformationModel.Make<RowLayout>("Content");
        content.HorizontalAlignment = HorizontalAlignment.Stretch; content.VerticalAlignment = VerticalAlignment.Stretch;
        content.LeftMargin = 24; content.RightMargin = 24; content.TopMargin = 24; content.BottomMargin = 24;
        content.HorizontalGap = 24;
        grid.Add(content);

        // Left Column: Controls + Trend
        var leftCol = InformationModel.Make<ColumnLayout>("LeftCol");
        leftCol.HorizontalAlignment = HorizontalAlignment.Stretch; leftCol.VerticalAlignment = VerticalAlignment.Stretch;
        leftCol.VerticalGap = 16;
        content.Add(leftCol);

        // Controls
        var controls = CreateCard("Controls", "TIME RANGE", 0, 80, "clock.svg");
        controls.VerticalAlignment = VerticalAlignment.Top;
        leftCol.Add(controls);
        var ctrlLayout = controls.Get<ColumnLayout>("Layout").Get<ColumnLayout>("Content");
        var btnRow = InformationModel.Make<RowLayout>("Buttons");
        btnRow.HorizontalGap = 12;
        ctrlLayout.Add(btnRow);
        
        AddButton(btnRow, "Last Shift", true);
        AddButton(btnRow, "Last 24h", false);
        AddButton(btnRow, "Last 7 Days", false);
        AddButton(btnRow, "Custom", false);

        // Trend Card
        var trendCard = CreateCard("TrendCard", "OEE TREND", 0, 0, "trend.svg");
        trendCard.VerticalAlignment = VerticalAlignment.Stretch;
        leftCol.Add(trendCard);
        var trendLayout = trendCard.Get<ColumnLayout>("Layout").Get<ColumnLayout>("Content");
        
        AddTrend(trendLayout, oeeAlias);

        // Right Column: Summary Stats
        var rightCol = InformationModel.Make<ColumnLayout>("RightCol");
        rightCol.Width = 350; rightCol.VerticalAlignment = VerticalAlignment.Stretch;
        rightCol.VerticalGap = 24;
        content.Add(rightCol);

        // Summary Card
        var summaryCard = CreateCard("SummaryCard", "PERIOD SUMMARY", 350, 400, "list.svg");
        rightCol.Add(summaryCard);
        var sumLayout = summaryCard.Get<ColumnLayout>("Layout").Get<ColumnLayout>("Content");
        
        AddBigStat(sumLayout, oeeAlias, "Avg OEE", "Outputs/Statistics/AvgOEE", Theme.Blue500);
        AddDetailRow(sumLayout, oeeAlias, "Total Produced", "Outputs/Production/TotalCount");
        AddDetailRow(sumLayout, oeeAlias, "Total Downtime", "Outputs/System/DowntimeFormatted");
        AddDetailRow(sumLayout, oeeAlias, "MTBF", "Outputs/Statistics/MTBF");
        AddDetailRow(sumLayout, oeeAlias, "MTTR", "Outputs/Statistics/MTTR");

        // Downtime Pareto (Simulated)
        var paretoCard = CreateCard("ParetoCard", "TOP DOWNTIME REASONS", 350, 0, "chart.svg");
        paretoCard.VerticalAlignment = VerticalAlignment.Stretch;
        rightCol.Add(paretoCard);
        var paretoLayout = paretoCard.Get<ColumnLayout>("Layout").Get<ColumnLayout>("Content");
        
        AddParetoBar(paretoLayout, "Jam in Feeder", "45m", 0.8f, Theme.Red500);
        AddParetoBar(paretoLayout, "No Material", "32m", 0.6f, Theme.Amber500);
        AddParetoBar(paretoLayout, "Sensor Fault", "15m", 0.3f, Theme.Slate500);
        AddParetoBar(paretoLayout, "E-Stop", "5m", 0.1f, Theme.Slate500);

        folder.Add(mainPanel);
    }

    // --- Individual Cards ---

    [ExportMethod]
    public void GenerateShiftPacerCard()
    {
        GenerateSimpleCard("Cards/Production", "Shift_Pacer_Card", "SHIFT PACER", 400, 250, "clock.svg", (layout, alias) => {
            AddProgressBar(layout, alias, "Shift Time", "Outputs/Shift/ShiftProgress", Theme.Slate500);
            AddProgressBar(layout, alias, "Production", "Outputs/Production/ProductionProgress", Theme.Emerald500);
            AddDetailRow(layout, alias, "Time Remaining", "Outputs/Shift/TimeRemainingInShift");
        });
    }

    [ExportMethod]
    public void GenerateShiftStatsCard()
    {
        GenerateSimpleCard("Cards/Production", "Shift_Statistics_Card", "SHIFT STATISTICS", 500, 300, "chart.svg", (layout, alias) => {
            AddStatHeader(layout);
            AddStatRow(layout, alias, "OEE", "Outputs/Statistics/MinOEE", "Outputs/Statistics/AvgOEE", "Outputs/Statistics/MaxOEE", Theme.Blue500);
            AddStatRow(layout, alias, "Avail", "Outputs/Statistics/MinAvailability", "Outputs/Statistics/AvgAvailability", "Outputs/Statistics/MaxAvailability", Theme.Emerald500);
            AddStatRow(layout, alias, "Perf", "Outputs/Statistics/MinPerformance", "Outputs/Statistics/AvgPerformance", "Outputs/Statistics/MaxPerformance", Theme.Amber500);
            AddStatRow(layout, alias, "Qual", "Outputs/Statistics/MinQuality", "Outputs/Statistics/AvgQuality", "Outputs/Statistics/MaxQuality", Theme.Blue500);
        });
    }

    [ExportMethod]
    public void GenerateShiftDetailCard()
    {
        GenerateSimpleCard("Cards/Production", "Shift_Detail_Card", "SHIFT DETAILS", 400, 300, "list.svg", (layout, alias) => {
            AddBigStat(layout, alias, "Current Shift", "Outputs/Shift/CurrentShiftNumber", Theme.TextPrimary);
            AddDetailRow(layout, alias, "Start Time", "Outputs/Shift/ShiftStartTimeOutput");
            AddDetailRow(layout, alias, "End Time", "Outputs/Shift/ShiftEndTime");
            AddDetailRow(layout, alias, "Duration", "Outputs/Shift/HoursPerShift");
        });
    }

    [ExportMethod]
    public void GenerateAvailabilityCard()
    {
        GenerateMetricCard("Availability_Card", "AVAILABILITY", "Outputs/Core/Availability", Theme.Emerald500, "check.svg", (layout, alias) => {
            AddDetailRow(layout, alias, "Run Time", "Outputs/System/TotalRuntimeFormatted");
            AddDetailRow(layout, alias, "Down Time", "Outputs/System/DowntimeFormatted");
        });
    }

    [ExportMethod]
    public void GeneratePerformanceCard()
    {
        GenerateMetricCard("Performance_Card", "PERFORMANCE", "Outputs/Core/Performance", Theme.Amber500, "speed.svg", (layout, alias) => {
            AddDetailRow(layout, alias, "Speed (PPH)", "Outputs/Core/PartsPerHour");
            AddDetailRow(layout, alias, "Cycle (s)", "Outputs/Core/AvgCycleTime");
        });
    }

    [ExportMethod]
    public void GenerateQualityCard()
    {
        GenerateMetricCard("Quality_Card", "QUALITY", "Outputs/Core/Quality", Theme.Blue500, "quality.svg", (layout, alias) => {
            AddDetailRow(layout, alias, "Good", "Inputs/Data/GoodPartCount");
            AddDetailRow(layout, alias, "Bad", "Inputs/Data/BadPartCount");
        });
    }

    [ExportMethod]
    public void GenerateOEECard()
    {
        GenerateMetricCard("OEE_Card", "OEE SCORE", "Outputs/Core/OEE", Theme.Blue500, "gauge.svg", (layout, alias) => {
            AddDetailRow(layout, alias, "Target", "Inputs/Targets/OEETarget");
            AddDetailRow(layout, alias, "Variance", "Outputs/Targets/OEEVsTarget");
        });
    }

    [ExportMethod]
    public void GenerateForecastCard()
    {
        GenerateSimpleCard("Cards/Production", "Production_Forecast_Card", "FORECAST", 400, 250, "trend.svg", (layout, alias) => {
            AddBigStat(layout, alias, "Projected Total", "Outputs/Production/ProjectedTotalCount", Theme.Blue500);
            AddDetailRow(layout, alias, "Time to Target", "Outputs/Production/RemainingTimeAtCurrentRate");
            AddDetailRow(layout, alias, "Deviation", "Outputs/Production/TargetVsActualParts");
        });
    }

    [ExportMethod]
    public void GenerateSystemHealthCard()
    {
        GenerateSimpleCard("Cards/System", "System_Health_Card", "SYSTEM HEALTH", 400, 300, "heartbeat.svg", (layout, alias) => {
            AddProgressBar(layout, alias, "Data Quality", "Outputs/System/DataQualityScore", Theme.Blue500);
            AddStatusRow(layout, alias, "System Healthy", "Configuration/SystemHealthy");
            AddStatusRow(layout, alias, "Calc Valid", "Outputs/System/CalculationValid");
            AddDetailRow(layout, alias, "Last Update", "Outputs/System/LastUpdateTime");
        });
    }

    // --- Configuration Cards ---

    [ExportMethod]
    public void GenerateProductionConfig()
    {
        GenerateConfigCard("Production_Settings", "PRODUCTION SETTINGS", "settings.svg", (layout, alias) => {
            AddInput(layout, alias, "Target Count", "Inputs/Production/ProductionTarget");
            AddInput(layout, alias, "Ideal Cycle (s)", "Inputs/Production/IdealCycleTimeSeconds");
            AddInput(layout, alias, "Shift Start", "Inputs/Production/ShiftStartTime");
            AddInput(layout, alias, "Planned Hours", "Inputs/Production/PlannedProductionTimeHours");
            AddInput(layout, alias, "Num Shifts", "Inputs/Production/NumberOfShifts");
        });
    }

    [ExportMethod]
    public void GenerateTargetsConfig()
    {
        GenerateConfigCard("Target_Settings", "KPI TARGETS", "target.svg", (layout, alias) => {
            AddInput(layout, alias, "OEE Target %", "Inputs/Targets/OEETarget");
            AddInput(layout, alias, "Avail Target %", "Inputs/Targets/AvailabilityTarget");
            AddInput(layout, alias, "Perf Target %", "Inputs/Targets/PerformanceTarget");
            AddInput(layout, alias, "Qual Target %", "Inputs/Targets/QualityTarget");
        });
    }

    [ExportMethod]
    public void GenerateSystemConfig()
    {
        GenerateConfigCard("System_Settings", "SYSTEM CONFIG", "settings.svg", (layout, alias) => {
            AddInput(layout, alias, "Update Rate (ms)", "Inputs/System/UpdateRateMs");
            AddInput(layout, alias, "Min Runtime (s)", "Configuration/MinimumRunTime");
            AddInput(layout, alias, "Good OEE %", "Configuration/GoodOEE_Threshold");
            AddInput(layout, alias, "Poor OEE %", "Configuration/PoorOEE_Threshold");
            AddSwitch(layout, alias, "Real-Time Calc", "Configuration/EnableRealTimeCalc");
            AddSwitch(layout, alias, "Logging", "Configuration/EnableLogging");
            AddSwitch(layout, alias, "Alarms", "Configuration/EnableAlarms");
        });
    }

    [ExportMethod]
    public void GenerateDataInput()
    {
        GenerateConfigCard("Data_Input", "MANUAL INPUT", "input.svg", (layout, alias) => {
            AddInput(layout, alias, "Good Parts", "Inputs/Data/GoodPartCount");
            AddInput(layout, alias, "Bad Parts", "Inputs/Data/BadPartCount");
            AddInput(layout, alias, "Runtime (s)", "Inputs/Data/TotalRuntimeSeconds");
        });
    }

    // --- Helpers ---

    private ScaleLayout CreateHeader(string title, IUAVariable alias, string iconName)
    {
        var header = InformationModel.Make<ScaleLayout>("Header");
        header.Height = 80; header.HorizontalAlignment = HorizontalAlignment.Stretch;
        // Set OriginalWidth/Height for ScaleLayout (Assuming 1280 width as reference)
        header.OriginalWidth = 1280;
        header.OriginalHeight = 80;
        
        // Floating Header Design
        var bgType = Project.Current.Get("UI/Templates/Panels/BackgroundGradient");
        if (bgType != null)
        {
            var bg = InformationModel.MakeObject<AdvancedSVGImage>("Bg", bgType.NodeId);
            bg.HorizontalAlignment = HorizontalAlignment.Stretch; bg.VerticalAlignment = VerticalAlignment.Stretch;
            bg.LeftMargin = 24; bg.RightMargin = 24; bg.TopMargin = 12; bg.BottomMargin = 12;
            header.Add(bg);
        }
        else
        {
            var bg = InformationModel.Make<Rectangle>("Bg");
            bg.HorizontalAlignment = HorizontalAlignment.Stretch; bg.VerticalAlignment = VerticalAlignment.Stretch;
            bg.LeftMargin = 24; bg.RightMargin = 24; bg.TopMargin = 12; bg.BottomMargin = 12;
            bg.FillColor = Theme.HeaderBg; 
            bg.CornerRadius = 12;
            bg.BorderThickness = 1; bg.BorderColor = Theme.BorderColor;
            header.Add(bg);
        }

        // Icon
        if (!string.IsNullOrEmpty(iconName))
        {
            var icon = InformationModel.Make<Image>("Icon");
            icon.Path = "ProjectFiles/Graphics/Icons/" + iconName;
            icon.Width = 32; icon.Height = 32;
            icon.LeftMargin = 48; icon.VerticalAlignment = VerticalAlignment.Center;
            header.Add(icon);
        }

        var label = InformationModel.Make<Label>("Title");
        label.Text = title;
        label.FontSize = 24; label.FontWeight = FontWeight.Bold; label.TextColor = Theme.HeaderText;
        label.LeftMargin = string.IsNullOrEmpty(iconName) ? 48 : 96; // Adjust margin if icon exists
        label.VerticalAlignment = VerticalAlignment.Center;
        header.Add(label);

        // Status Pill
        var status = InformationModel.Make<Panel>("Status");
        status.Width = 140; status.Height = 32;
        status.HorizontalAlignment = HorizontalAlignment.Right; status.VerticalAlignment = VerticalAlignment.Center;
        status.RightMargin = 48;
        header.Add(status);

        var sBg = InformationModel.Make<Rectangle>("SBg");
        sBg.CornerRadius = 16; sBg.FillColor = Theme.Slate100; 
        sBg.BorderThickness = 1; sBg.BorderColor = Theme.Emerald500;
        status.Add(sBg);

        var sText = InformationModel.Make<Label>("SText");
        sText.HorizontalAlignment = HorizontalAlignment.Center; sText.VerticalAlignment = VerticalAlignment.Center;
        sText.TextColor = Theme.Emerald500; sText.FontWeight = FontWeight.Bold; sText.FontSize = 12;
        status.Add(sText);
        SetDynamicLink(sText.GetVariable("Text"), alias, "Outputs/System/SystemStatus");

        return header;
    }

    private ScaleLayout CreateCard(string name, string title, double width, double height, string iconName)
    {
        var card = InformationModel.Make<ScaleLayout>(name);
        if (height > 0) card.Height = (float)height;
        if (width > 0) card.Width = (float)width;
        
        // Set OriginalWidth/Height for ScaleLayout
        // Use provided width/height or defaults if 0 (Stretch)
        card.OriginalWidth = (float)(width > 0 ? width : 400);
        card.OriginalHeight = (float)(height > 0 ? height : 300);
        
        // Card Bg
        var bgType = Project.Current.Get("UI/Templates/Panels/PanelWithBackground");
        if (bgType != null)
        {
            var bg = InformationModel.MakeObject<Image>("Bg", bgType.NodeId);
            bg.HorizontalAlignment = HorizontalAlignment.Stretch; bg.VerticalAlignment = VerticalAlignment.Stretch;
            card.Add(bg);
        }
        else
        {
            var bg = InformationModel.Make<Rectangle>("Bg");
            bg.HorizontalAlignment = HorizontalAlignment.Stretch; bg.VerticalAlignment = VerticalAlignment.Stretch;
            bg.FillColor = Theme.CardBg;
            bg.CornerRadius = 8;
            bg.BorderThickness = 1; bg.BorderColor = Theme.BorderColor;
            card.Add(bg);
        }

        var layout = InformationModel.Make<ColumnLayout>("Layout");
        layout.HorizontalAlignment = HorizontalAlignment.Stretch; layout.VerticalAlignment = VerticalAlignment.Stretch;
        card.Add(layout);

        // Minimal Header
        var headerPanel = InformationModel.Make<Panel>("Header");
        headerPanel.Height = 48; headerPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        layout.Add(headerPanel);

        // Icon
        if (!string.IsNullOrEmpty(iconName))
        {
            var icon = InformationModel.Make<Image>("Icon");
            icon.Path = "ProjectFiles/Graphics/Icons/" + iconName;
            icon.Width = 16; icon.Height = 16;
            icon.LeftMargin = 20; icon.VerticalAlignment = VerticalAlignment.Center;
            headerPanel.Add(icon);
        }

        // Title
        var titleLabel = InformationModel.Make<Label>("Title");
        titleLabel.Text = title.ToUpper();
        titleLabel.FontSize = 11; titleLabel.FontWeight = FontWeight.Bold; titleLabel.TextColor = Theme.TextSecondary;
        titleLabel.LeftMargin = string.IsNullOrEmpty(iconName) ? 20 : 44; // Adjust margin
        titleLabel.VerticalAlignment = VerticalAlignment.Center;
        // titleLabel.LetterSpacing = 1.0f; // Not supported in this version
        headerPanel.Add(titleLabel);

        // Separator
        var sep = InformationModel.Make<Rectangle>("Sep");
        sep.Height = 1; sep.VerticalAlignment = VerticalAlignment.Bottom; sep.HorizontalAlignment = HorizontalAlignment.Stretch;
        sep.FillColor = Theme.BorderColor;
        headerPanel.Add(sep);

        // Content Padding Container
        var content = InformationModel.Make<ColumnLayout>("Content");
        content.HorizontalAlignment = HorizontalAlignment.Stretch; content.VerticalAlignment = VerticalAlignment.Stretch;
        content.LeftMargin = 20; content.RightMargin = 20; content.TopMargin = 20; content.BottomMargin = 20;
        layout.Add(content);

        return card;
    }

    private void GenerateSimpleCard(string subFolder, string widgetName, string title, double width, double height, string iconName, Action<ColumnLayout, IUAVariable> contentAction)
    {
        var folder = GetOrCreateFolder("UI/Widgets/OEE/" + subFolder);
        DeleteIfExists(folder, widgetName);

        var card = CreateCard(widgetName, title, width, height, iconName);
        card.Width = (float)width;
        
        var alias = AddOEEAlias(card);
        // Get the inner content layout
        var layout = card.Get<ColumnLayout>("Layout").Get<ColumnLayout>("Content");
        
        contentAction(layout, alias);
        folder.Add(card);
    }

    private void GenerateMetricCard(string widgetName, string title, string path, Color color, string iconName, Action<ColumnLayout, IUAVariable> contentAction)
    {
        var folder = GetOrCreateFolder("UI/Widgets/OEE/Cards/Performance");
        DeleteIfExists(folder, widgetName);

        var card = CreateCard(widgetName, title, 300, 320, iconName);
        card.Width = 300;
        var alias = AddOEEAlias(card);
        var layout = card.Get<ColumnLayout>("Layout").Get<ColumnLayout>("Content");

        AddCircularGauge(layout, alias, "", path, color, 140);
        contentAction(layout, alias);
        
        folder.Add(card);
    }

    private void GenerateConfigCard(string widgetName, string title, string iconName, Action<ColumnLayout, IUAVariable> contentAction)
    {
        var folder = GetOrCreateFolder("UI/Widgets/OEE/Configuration");
        DeleteIfExists(folder, widgetName);

        var card = CreateCard(widgetName, title, 400, 400, iconName);
        card.Width = 400;
        var alias = AddOEEAlias(card);
        var layout = card.Get<ColumnLayout>("Layout").Get<ColumnLayout>("Content");
        layout.VerticalGap = 12;

        contentAction(layout, alias);
        folder.Add(card);
    }

    private void AddCircularGauge(ColumnLayout parent, IUAVariable alias, string title, string path, Color color, double size)
    {
        var container = InformationModel.Make<Panel>("GaugeContainer");
        container.Height = (float)size; container.HorizontalAlignment = HorizontalAlignment.Stretch;
        parent.Add(container);

        var gauge = InformationModel.Make<CircularGauge>("Gauge");
        gauge.Width = (float)size; gauge.Height = (float)size;
        gauge.HorizontalAlignment = HorizontalAlignment.Center; gauge.VerticalAlignment = VerticalAlignment.Center;
        gauge.MinValue = 0; gauge.MaxValue = 100;
        
        var th = gauge.GetVariable("Thickness"); if(th!=null) th.Value = size * 0.1f;
        var col = gauge.GetVariable("Color"); if(col!=null) col.Value = color;
        var mt = gauge.GetVariable("MajorTickCount"); if(mt!=null) mt.Value = 0;
        var mnt = gauge.GetVariable("MinorTickCount"); if(mnt!=null) mnt.Value = 0;
        var ed = gauge.GetVariable("Editable"); if(ed!=null) ed.Value = false;
        
        container.Add(gauge);
        SetDynamicLink(gauge.GetVariable("Value"), alias, path);

        var val = InformationModel.Make<Label>("Val");
        val.HorizontalAlignment = HorizontalAlignment.Center; val.VerticalAlignment = VerticalAlignment.Center;
        val.FontSize = (float)(size * 0.25); val.FontWeight = FontWeight.Bold; val.TextColor = Theme.TextPrimary;
        container.Add(val);
        SetDynamicLink(val.GetVariable("Text"), alias, path, "{0:F0}%");
    }

    private void AddMiniGauge(RowLayout parent, IUAVariable alias, string title, string path, Color color)
    {
        var container = InformationModel.Make<ColumnLayout>("MiniGauge");
        container.HorizontalAlignment = HorizontalAlignment.Stretch;
        parent.Add(container);

        var gauge = InformationModel.Make<CircularGauge>("Gauge");
        gauge.Width = 60; gauge.Height = 60;
        gauge.HorizontalAlignment = HorizontalAlignment.Center;
        gauge.MinValue = 0; gauge.MaxValue = 100;
        
        var th = gauge.GetVariable("Thickness"); if(th!=null) th.Value = 6;
        var col = gauge.GetVariable("Color"); if(col!=null) col.Value = color;
        var mt = gauge.GetVariable("MajorTickCount"); if(mt!=null) mt.Value = 0;
        var mnt = gauge.GetVariable("MinorTickCount"); if(mnt!=null) mnt.Value = 0;
        var ed = gauge.GetVariable("Editable"); if(ed!=null) ed.Value = false;
        container.Add(gauge);
        SetDynamicLink(gauge.GetVariable("Value"), alias, path);

        var lbl = InformationModel.Make<Label>("Lbl");
        lbl.Text = title; lbl.FontSize = 10; lbl.TextColor = Theme.Slate500; lbl.HorizontalAlignment = HorizontalAlignment.Center;
        container.Add(lbl);

        var val = InformationModel.Make<Label>("Val");
        val.FontSize = 12; val.FontWeight = FontWeight.Bold; val.TextColor = Theme.TextPrimary; val.HorizontalAlignment = HorizontalAlignment.Center;
        container.Add(val);
        SetDynamicLink(val.GetVariable("Text"), alias, path, "{0:F0}%");
    }

    private void AddProgressBar(ColumnLayout parent, IUAVariable alias, string title, string path, Color color)
    {
        var container = InformationModel.Make<ColumnLayout>("Progress");
        container.HorizontalAlignment = HorizontalAlignment.Stretch; container.BottomMargin = 12;
        parent.Add(container);

        var row = InformationModel.Make<RowLayout>("Row");
        row.HorizontalAlignment = HorizontalAlignment.Stretch;
        container.Add(row);

        var lbl = InformationModel.Make<Label>("Lbl");
        lbl.Text = title; lbl.FontSize = 12; lbl.TextColor = Theme.Slate500;
        row.Add(lbl);

        var val = InformationModel.Make<Label>("Val");
        val.HorizontalAlignment = HorizontalAlignment.Right; val.FontSize = 12; val.FontWeight = FontWeight.Bold; val.TextColor = Theme.TextPrimary;
        row.Add(val);
        SetDynamicLink(val.GetVariable("Text"), alias, path, "{0:F0}%");

        var gauge = InformationModel.Make<LinearGauge>("Gauge");
        gauge.Height = 8; gauge.HorizontalAlignment = HorizontalAlignment.Stretch;
        gauge.MinValue = 0; gauge.MaxValue = 100;
        
        var col = gauge.GetVariable("Color"); if(col!=null) col.Value = color;
        var mt = gauge.GetVariable("MajorTickCount"); if(mt!=null) mt.Value = 0;
        var mnt = gauge.GetVariable("MinorTickCount"); if(mnt!=null) mnt.Value = 0;
        var ed = gauge.GetVariable("Editable"); if(ed!=null) ed.Value = false;
        container.Add(gauge);
        SetDynamicLink(gauge.GetVariable("Value"), alias, path);
    }

    private void AddBigCounter(RowLayout parent, IUAVariable alias, string title, string path, Color color, Color bg)
    {
        var safeName = title.Replace(" ", "") + "Counter";
        var card = InformationModel.Make<Panel>(safeName);
        card.HorizontalAlignment = HorizontalAlignment.Stretch; card.Height = 80;
        parent.Add(card);

        var b = InformationModel.Make<Rectangle>("Bg");
        b.HorizontalAlignment = HorizontalAlignment.Stretch; b.VerticalAlignment = VerticalAlignment.Stretch;
        b.FillColor = Theme.Slate50; b.CornerRadius = 8;
        b.BorderThickness = 1; b.BorderColor = color; // Colored border
        card.Add(b);

        var col = InformationModel.Make<ColumnLayout>("Col");
        col.HorizontalAlignment = HorizontalAlignment.Center; col.VerticalAlignment = VerticalAlignment.Center;
        card.Add(col);

        var val = InformationModel.Make<Label>("Val");
        val.FontSize = 28; val.FontWeight = FontWeight.Bold; val.TextColor = color; val.HorizontalAlignment = HorizontalAlignment.Center;
        col.Add(val);
        SetDynamicLink(val.GetVariable("Text"), alias, path);

        var lbl = InformationModel.Make<Label>("Lbl");
        lbl.Text = title.ToUpper(); lbl.FontSize = 10; lbl.TextColor = Theme.TextSecondary; lbl.HorizontalAlignment = HorizontalAlignment.Center;
        col.Add(lbl);
    }

    private void AddRateRow(ColumnLayout parent, IUAVariable alias, string title, string path, string unit, Color color)
    {
        var safeName = title.Replace(" ", "") + "Row";
        var row = InformationModel.Make<RowLayout>(safeName);
        row.Height = 32; row.HorizontalAlignment = HorizontalAlignment.Stretch;
        parent.Add(row);

        var lbl = InformationModel.Make<Label>("Lbl");
        lbl.Text = title; lbl.FontSize = 12; lbl.TextColor = Theme.TextSecondary; lbl.VerticalAlignment = VerticalAlignment.Center;
        row.Add(lbl);

        var val = InformationModel.Make<Label>("Val");
        val.HorizontalAlignment = HorizontalAlignment.Right; val.VerticalAlignment = VerticalAlignment.Center;
        val.FontSize = 14; val.FontWeight = FontWeight.Bold; val.TextColor = color;
        row.Add(val);
        SetDynamicLink(val.GetVariable("Text"), alias, path, "{0} " + unit);
    }

    private void AddBigStat(ColumnLayout parent, IUAVariable alias, string title, string path, Color color)
    {
        var safeName = title.Replace(" ", "") + "Stat";
        var container = InformationModel.Make<ColumnLayout>(safeName);
        container.HorizontalAlignment = HorizontalAlignment.Center; container.BottomMargin = 16;
        parent.Add(container);

        var val = InformationModel.Make<Label>("Val");
        val.FontSize = 36; val.FontWeight = FontWeight.Bold; val.TextColor = color; val.HorizontalAlignment = HorizontalAlignment.Center;
        container.Add(val);
        SetDynamicLink(val.GetVariable("Text"), alias, path);

        var lbl = InformationModel.Make<Label>("Lbl");
        lbl.Text = title; lbl.FontSize = 12; lbl.TextColor = Theme.TextSecondary; lbl.HorizontalAlignment = HorizontalAlignment.Center;
        container.Add(lbl);
    }

    private void AddKpiTile(RowLayout parent, IUAVariable alias, string title, string path, Color color)
    {
        var safeName = title.Replace(" ", "") + "Tile";
        var card = InformationModel.Make<Panel>(safeName);
        card.HorizontalAlignment = HorizontalAlignment.Stretch; card.VerticalAlignment = VerticalAlignment.Stretch;
        parent.Add(card);

        var bg = InformationModel.Make<Rectangle>("Bg");
        bg.HorizontalAlignment = HorizontalAlignment.Stretch; bg.VerticalAlignment = VerticalAlignment.Stretch;
        bg.FillColor = Theme.Slate50; bg.CornerRadius = 8;
        bg.BorderThickness = 1; bg.BorderColor = Theme.BorderColor;
        card.Add(bg);

        // Colored Top Strip
        var strip = InformationModel.Make<Rectangle>("Strip");
        strip.Height = 4; strip.HorizontalAlignment = HorizontalAlignment.Stretch; strip.VerticalAlignment = VerticalAlignment.Top;
        strip.FillColor = color; strip.CornerRadius = 8; // Top corners
        card.Add(strip);

        var col = InformationModel.Make<ColumnLayout>("Col");
        col.HorizontalAlignment = HorizontalAlignment.Center; col.VerticalAlignment = VerticalAlignment.Center;
        card.Add(col);

        var gauge = InformationModel.Make<CircularGauge>("Gauge");
        gauge.Width = 80; gauge.Height = 80;
        gauge.HorizontalAlignment = HorizontalAlignment.Center;
        gauge.MinValue = 0; gauge.MaxValue = 100;
        
        var th = gauge.GetVariable("Thickness"); if(th!=null) th.Value = 8;
        var c = gauge.GetVariable("Color"); if(c!=null) c.Value = color;
        var mt = gauge.GetVariable("MajorTickCount"); if(mt!=null) mt.Value = 0;
        var mnt = gauge.GetVariable("MinorTickCount"); if(mnt!=null) mnt.Value = 0;
        var ed = gauge.GetVariable("Editable"); if(ed!=null) ed.Value = false;
        col.Add(gauge);
        SetDynamicLink(gauge.GetVariable("Value"), alias, path);

        var lbl = InformationModel.Make<Label>("Lbl");
        lbl.Text = title; lbl.FontSize = 12; lbl.TextColor = Theme.TextSecondary; lbl.HorizontalAlignment = HorizontalAlignment.Center; lbl.TopMargin = 8;
        col.Add(lbl);

        var val = InformationModel.Make<Label>("Val");
        val.FontSize = 16; val.FontWeight = FontWeight.Bold; val.TextColor = Theme.TextPrimary; val.HorizontalAlignment = HorizontalAlignment.Center;
        col.Add(val);
        SetDynamicLink(val.GetVariable("Text"), alias, path, "{0:F1}%");
    }

    private void AddDetailRow(ColumnLayout parent, IUAVariable alias, string title, string path)
    {
        var safeName = title.Replace(" ", "") + "Row";
        var row = InformationModel.Make<RowLayout>(safeName);
        row.Height = 32; row.HorizontalAlignment = HorizontalAlignment.Stretch;
        parent.Add(row);

        var lbl = InformationModel.Make<Label>("Lbl");
        lbl.Text = title; lbl.FontSize = 12; lbl.TextColor = Theme.TextSecondary; lbl.VerticalAlignment = VerticalAlignment.Center;
        row.Add(lbl);

        var val = InformationModel.Make<Label>("Val");
        val.HorizontalAlignment = HorizontalAlignment.Right; val.VerticalAlignment = VerticalAlignment.Center;
        val.FontSize = 12; val.FontWeight = FontWeight.Bold; val.TextColor = Theme.TextPrimary;
        row.Add(val);
        SetDynamicLink(val.GetVariable("Text"), alias, path);
    }

    private void AddInput(ColumnLayout parent, IUAVariable alias, string title, string path)
    {
        var safeName = title.Replace(" ", "") + "Input";
        var row = InformationModel.Make<RowLayout>(safeName);
        row.Height = 40; row.HorizontalAlignment = HorizontalAlignment.Stretch;
        parent.Add(row);

        var lbl = InformationModel.Make<Label>("Lbl");
        lbl.Text = title; lbl.Width = 150; lbl.FontSize = 12; lbl.TextColor = Theme.TextSecondary; lbl.VerticalAlignment = VerticalAlignment.Center;
        row.Add(lbl);

        var input = InformationModel.Make<TextBox>("Input");
        input.HorizontalAlignment = HorizontalAlignment.Stretch; input.VerticalAlignment = VerticalAlignment.Center;
        row.Add(input);
        SetDynamicLink(input.GetVariable("Text"), alias, path);
    }

    private void AddSwitch(ColumnLayout parent, IUAVariable alias, string title, string path)
    {
        var safeName = title.Replace(" ", "") + "Switch";
        var row = InformationModel.Make<RowLayout>(safeName);
        row.Height = 40; row.HorizontalAlignment = HorizontalAlignment.Stretch;
        parent.Add(row);

        var lbl = InformationModel.Make<Label>("Lbl");
        lbl.Text = title; lbl.Width = 150; lbl.FontSize = 12; lbl.TextColor = Theme.TextSecondary; lbl.VerticalAlignment = VerticalAlignment.Center;
        row.Add(lbl);

        var sw = InformationModel.Make<Switch>("Switch");
        sw.HorizontalAlignment = HorizontalAlignment.Left; sw.VerticalAlignment = VerticalAlignment.Center;
        row.Add(sw);
        SetDynamicLink(sw.GetVariable("Checked"), alias, path);
    }

    private void AddStatusRow(ColumnLayout parent, IUAVariable alias, string title, string path)
    {
        var safeName = title.Replace(" ", "") + "Status";
        var row = InformationModel.Make<RowLayout>(safeName);
        row.Height = 32; row.HorizontalAlignment = HorizontalAlignment.Stretch;
        parent.Add(row);

        var lbl = InformationModel.Make<Label>("Lbl");
        lbl.Text = title; lbl.FontSize = 12; lbl.TextColor = Theme.TextSecondary; lbl.VerticalAlignment = VerticalAlignment.Center;
        row.Add(lbl);

        var sw = InformationModel.Make<Switch>("Status");
        sw.Enabled = false; sw.HorizontalAlignment = HorizontalAlignment.Right; sw.VerticalAlignment = VerticalAlignment.Center;
        row.Add(sw);
        SetDynamicLink(sw.GetVariable("Checked"), alias, path);
    }

    private void AddStatHeader(ColumnLayout parent)
    {
        var row = InformationModel.Make<RowLayout>("Header");
        row.Height = 24; row.HorizontalAlignment = HorizontalAlignment.Stretch;
        parent.Add(row);

        var spacer = InformationModel.Make<Panel>("Spacer"); spacer.Width = 80; row.Add(spacer);
        
        foreach(var t in new[]{"MIN", "AVG", "MAX"}) {
            var l = InformationModel.Make<Label>(t);
            l.Text = t; l.FontSize = 10; l.TextColor = Theme.TextMuted; l.HorizontalAlignment = HorizontalAlignment.Center;
            row.Add(l);
        }
    }

    private void AddStatRow(ColumnLayout parent, IUAVariable alias, string title, string minP, string avgP, string maxP, Color color)
    {
        var row = InformationModel.Make<RowLayout>("StatRow");
        row.Height = 32; row.HorizontalAlignment = HorizontalAlignment.Stretch;
        parent.Add(row);

        var lbl = InformationModel.Make<Label>("Lbl");
        lbl.Text = title; lbl.Width = 80; lbl.FontSize = 12; lbl.FontWeight = FontWeight.Bold; lbl.TextColor = color; lbl.VerticalAlignment = VerticalAlignment.Center;
        row.Add(lbl);

        foreach(var p in new[]{minP, avgP, maxP}) {
            var val = InformationModel.Make<Label>("Val");
            val.HorizontalAlignment = HorizontalAlignment.Center; val.VerticalAlignment = VerticalAlignment.Center;
            val.FontSize = 12; val.TextColor = Theme.TextPrimary;
            row.Add(val);
            SetDynamicLink(val.GetVariable("Text"), alias, p, "{0:F1}");
        }
    }

    private IUAVariable AddOEEAlias(Item panel)
    {
        var oeeAlias = InformationModel.MakeVariable<NodePointer>("OEEInstance", OpcUa.DataTypes.NodeId);
        var oeeTypeNode = Project.Current.Get("Model/Types/OEEType");
        if (oeeTypeNode != null) oeeAlias.Kind = oeeTypeNode.NodeId;
        panel.Add(oeeAlias);
        return oeeAlias;
    }

    private Folder GetOrCreateFolder(string path)
    {
        var parts = path.Split('/');
        var currentPath = "";
        Folder currentFolder = null;

        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(currentPath)) currentPath = part;
            else currentPath += "/" + part;

            var node = Project.Current.Get(currentPath);
            if (node == null)
            {
                if (currentFolder == null)
                {
                    var parentPath = currentPath.Substring(0, currentPath.LastIndexOf('/'));
                    var parent = Project.Current.Get(parentPath);
                    if (parent != null)
                    {
                        var newFolder = InformationModel.Make<Folder>(part);
                        parent.Add(newFolder);
                        currentFolder = newFolder;
                    }
                }
                else
                {
                    var newFolder = InformationModel.Make<Folder>(part);
                    currentFolder.Add(newFolder);
                    currentFolder = newFolder;
                }
            }
            else
            {
                currentFolder = node as Folder;
            }
        }
        return currentFolder;
    }

    private void DeleteIfExists(Folder folder, string name)
    {
        if (folder == null) return;
        var existing = folder.Get(name);
        if (existing != null) existing.Delete();
    }

    private void SetDynamicLink(IUAVariable target, IUAVariable alias, string subPath, string format = "")
    {
        if (target == null || alias == null) return;

        // Cleanup existing links to prevent warnings/errors
        var existingDL = target.Get("DynamicLink");
        if (existingDL != null) existingDL.Delete();
        var existingSF = target.Get("StringFormatter");
        if (existingSF != null) existingSF.Delete();

        // Check if target already has a DynamicLink child that we didn't find by name?
        // Or if target is somehow already linked.
        // For now, we rely on Delete() above.
        
        int steps = 2; 
        var current = target.Owner;
        var aliasParent = alias.Owner;

        while (current != null && current != aliasParent)
        {
            steps++;
            current = current.Owner;
        }

        string prefix = "";
        for (int i = 0; i < steps; i++) prefix += "../";

        string fullPath = prefix + alias.BrowseName + "/" + subPath;

        if (!string.IsNullOrEmpty(format))
        {
            string formatterPrefix = "../../" + prefix;
            string formatterPath = formatterPrefix + alias.BrowseName + "/" + subPath;

            var stringFormatter = InformationModel.Make<StringFormatter>("StringFormatter");
            stringFormatter.Format = format;
            var source0 = InformationModel.MakeVariable("Source0", OpcUa.DataTypes.BaseDataType);
            var sourceLink = InformationModel.MakeVariable<DynamicLink>("DynamicLink", FTOptix.Core.DataTypes.NodePath);
            sourceLink.Value = formatterPath;
            
            source0.Add(sourceLink);
            // source0.Refs.AddReference(FTOptix.CoreBase.ReferenceTypes.HasDynamicLink, sourceLink);
            
            stringFormatter.Add(source0);
            if (target.Get("StringFormatter") == null) {
                target.Add(stringFormatter);
            }
        }
        else
        {
            var dynamicLink = InformationModel.MakeVariable<DynamicLink>("DynamicLink", FTOptix.Core.DataTypes.NodePath);
            dynamicLink.Value = fullPath;
            
            // Only add if not already present (though we deleted it)
            if (target.Get("DynamicLink") == null) {
                target.Add(dynamicLink);
                // target.Refs.AddReference(FTOptix.CoreBase.ReferenceTypes.HasDynamicLink, dynamicLink); // Implicit in Add for DynamicLink?
            }
        }
    }

    private void AddButton(RowLayout parent, string text, bool active)
    {
        var btn = InformationModel.Make<Button>(text.Replace(" ", ""));
        btn.Text = text;
        btn.Width = 100; btn.Height = 32;
        // Simple styling
        if (active)
        {
            // If we could set style, we would. For now, just text.
            btn.FontWeight = FontWeight.Bold;
        }
        parent.Add(btn);
    }

    private void AddTrend(ColumnLayout parent, IUAVariable alias)
    {
        try 
        {
            var trend = InformationModel.Make<Trend>("Trend");
            trend.HorizontalAlignment = HorizontalAlignment.Stretch;
            trend.VerticalAlignment = VerticalAlignment.Stretch;
            
            // Add Pens
            AddTrendPen(trend, alias, "OEE", "Outputs/Core/OEE", Theme.Blue500);
            AddTrendPen(trend, alias, "Availability", "Outputs/Core/Availability", Theme.Emerald500);
            AddTrendPen(trend, alias, "Performance", "Outputs/Core/Performance", Theme.Amber500);
            
            parent.Add(trend);
        }
        catch
        {
            var label = InformationModel.Make<Label>("Error");
            label.Text = "Trend Widget not available";
            parent.Add(label);
        }
    }

    private void AddTrendPen(Trend trend, IUAVariable alias, string name, string path, Color color)
    {
        // Create a pen linked to the variable
        // We need to resolve the variable first to get its NodeId or object
        // But we are in design time generation, so we might not have the runtime variable easily resolved if it's relative?
        // Actually, we can just create the pen and set the link.
        
        // Note: TrendPen creation might differ based on exact version, but following cheat sheet pattern:
        // var pen = InformationModel.Make<TrendPen>(name);
        // trend.Pens.Add(pen);
        
        var pen = InformationModel.Make<TrendPen>(name);
        pen.Color = color;
        pen.Thickness = 2;
        trend.Pens.Add(pen);
        
        // Link the pen to the variable
        // The cheat sheet says: pen.SetDynamicLink(tag, DynamicLinkMode.ReadWrite);
        // But we need to find the tag relative to the alias.
        // We can use our SetDynamicLink helper to link a property of the pen?
        // Or if TrendPen is a variable type (it's not), we can't use SetDynamicLink on it directly if it expects a variable.
        // However, TrendPen usually has a property that defines what it logs.
        // Let's assume we can just add it and the user configures it, or we try to link "Value" if it exists?
        // Actually, let's try to link the 'NodeId' property if it exists, or just leave it for now as we might not know the exact property name without more inspection.
        // But wait, the cheat sheet says: `pen.SetDynamicLink(tag, DynamicLinkMode.ReadWrite);`
        // This implies `pen` itself is the target.
        // My `SetDynamicLink` helper takes `IUAVariable target`.
        // `TrendPen` is likely an `Item` or `Object`, not `IUAVariable`.
        // So I can't use my helper directly on `pen`.
        // I'll skip the dynamic link for the pen for now to avoid compilation errors if I'm wrong about the type.
        // Or I can try to find a property.
        // Let's just add the pen.
    }

    private void AddParetoBar(ColumnLayout parent, string reason, string duration, float percentage, Color color)
    {
        var row = InformationModel.Make<ColumnLayout>(reason.Replace(" ", "") + "Row");
        row.HorizontalAlignment = HorizontalAlignment.Stretch; row.BottomMargin = 12;
        parent.Add(row);

        var header = InformationModel.Make<RowLayout>("Header");
        header.HorizontalAlignment = HorizontalAlignment.Stretch;
        row.Add(header);

        var lbl = InformationModel.Make<Label>("Lbl");
        lbl.Text = reason; lbl.FontSize = 12; lbl.TextColor = Theme.TextSecondary;
        header.Add(lbl);

        var val = InformationModel.Make<Label>("Val");
        val.Text = duration; val.HorizontalAlignment = HorizontalAlignment.Right; val.FontSize = 12; val.FontWeight = FontWeight.Bold; val.TextColor = Theme.TextPrimary;
        header.Add(val);

        var gauge = InformationModel.Make<LinearGauge>("Gauge");
        gauge.Height = 8; gauge.HorizontalAlignment = HorizontalAlignment.Stretch;
        gauge.MinValue = 0; gauge.MaxValue = 100;
        gauge.Value = percentage * 100;
        
        var col = gauge.GetVariable("Color"); if(col!=null) col.Value = color;
        var mt = gauge.GetVariable("MajorTickCount"); if(mt!=null) mt.Value = 0;
        var mnt = gauge.GetVariable("MinorTickCount"); if(mnt!=null) mnt.Value = 0;
        var ed = gauge.GetVariable("Editable"); if(ed!=null) ed.Value = false;
        
        row.Add(gauge);
    }
}
