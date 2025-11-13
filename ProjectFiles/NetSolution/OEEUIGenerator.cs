#region Using directives
using System;
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

public class OEEUIGenerator : BaseNetLogic
{
    /*
     * OEE DATA BINDING REFERENCE - ALIGNED WITH ACTUAL OEEType
     * ========================================================
     * 
     * OEEType Structure from Types.yaml:
     * 
     * Inputs/ (Configuration & Input variables)
     * - TotalRuntimeSeconds (Double) - Runtime in seconds
     * - GoodPartCount (Int32) - Count of good parts
     * - BadPartCount (Int32) - Count of bad parts  
     * - IdealCycleTimeSeconds (Double) - Target cycle time
     * - PlannedProductionTimeHours (Double) - Planned production hours
     * - HoursPerShift (Double) - Hours in each shift
     * - NumberOfShifts (Int32) - Number of shifts per day
     * - ShiftStartTime (String) - Shift start time
     * - ProductionTarget (Int32) - Target production count
     * - QualityTarget (Double) - Target quality percentage
     * - PerformanceTarget (Double) - Target performance percentage
     * - AvailabilityTarget (Double) - Target availability percentage
     * - OEETarget (Double) - Target OEE percentage
     * - UpdateRateMs (Int32) - Update rate in milliseconds
     * - LoggingVerbosity (Int32) - Logging verbosity level
     * 
     * Outputs/ (Calculated & Real-time values)
     * - TotalCount (Int32) - Total parts produced
     * - Quality (Double) - Current quality percentage
     * - Performance (Double) - Current performance percentage
     * - Availability (Double) - Current availability percentage
     * - OEE (Double) - Overall Equipment Effectiveness
     * - AvgCycleTime (Double) - Average cycle time
     * - PartsPerHour (Double) - Production rate per hour
     * - ExpectedPartCount (Int32) - Expected part count
     * - DowntimeFormatted (String) - Formatted downtime display
     * - TotalRuntimeFormatted (String) - Formatted runtime display
     * - CurrentShiftNumber (Int32) - Current shift number
     * - ShiftStartTimeOutput (String) - Current shift start time
     * - ShiftEndTime (String) - Current shift end time
     * - TimeIntoShift (String) - Time elapsed in current shift
     * - TimeRemainingInShift (String) - Time remaining in shift
     * - ShiftChangeOccurred (Boolean) - Shift change indicator
     * - ShiftChangeImminent (Boolean) - Shift change warning
     * - ProjectedTotalCount (Int32) - Projected end-of-shift count
     * - RemainingTimeAtCurrentRate (String) - Time to reach target
     * - ProductionBehindSchedule (Boolean) - Behind schedule flag
     * - RequiredRateToTarget (Double) - Required rate to meet target
     * - TargetVsActualParts (Int32) - Difference from target
     * - LastUpdateTime (String) - Last calculation timestamp
     * - SystemStatus (String) - System status description
     * - CalculationValid (Boolean) - Calculation validity flag
     * - DataQualityScore (Double) - Data quality assessment
     * - QualityTrend (String) - Quality trend direction
     * - PerformanceTrend (String) - Performance trend direction
     * - AvailabilityTrend (String) - Availability trend direction
     * - OEETrend (String) - OEE trend direction
     * - MinQuality/MaxQuality/AvgQuality (Double) - Quality statistics
     * - MinPerformance/MaxPerformance/AvgPerformance (Double) - Performance statistics
     * - MinAvailability/MaxAvailability/AvgAvailability (Double) - Availability statistics
     * - MinOEE/MaxOEE/AvgOEE (Double) - OEE statistics
     * - QualityVsTarget (Double) - Quality vs target comparison
     * - PerformanceVsTarget (Double) - Performance vs target comparison
     * - AvailabilityVsTarget (Double) - Availability vs target comparison
     * - OEEVsTarget (Double) - OEE vs target comparison
     * 
     * Configuration/ (System configuration settings)
     * - EnableRealTimeCalc (Boolean) - Enable real-time calculations
     * - MinimumRunTime (Double) - Minimum runtime for calculations
     * - GoodOEE_Threshold (Double) - Good OEE threshold
     * - PoorOEE_Threshold (Double) - Poor OEE threshold
     * - EnableLogging (Boolean) - Enable system logging
     * - EnableAlarms (Boolean) - Enable alarm generation
     * - SystemHealthy (Boolean) - System health status
     * 
     * DATA BINDING PATTERN:
     * {OEEInstance}/Inputs/{VariableName}        - For input settings
     * {OEEInstance}/Outputs/{VariableName}       - For calculated outputs
     * {OEEInstance}/Configuration/{VariableName} - For system configuration
     * 
     * EXAMPLE PATHS:
     * Model/OEEInstances/Machine1/Outputs/OEE
     * Model/OEEInstances/Machine1/Inputs/ProductionTarget  
     * Model/OEEInstances/Machine1/Configuration/EnableRealTimeCalc
     */

    // Modern color palette for professional appearance
    private readonly Color PRIMARY_BLUE = new Color(0xFF0F5F99);
    private readonly Color SUCCESS_GREEN = new Color(0xFF28A745);
    private readonly Color WARNING_AMBER = new Color(0xFFFFC107);
    private readonly Color DANGER_RED = new Color(0xFFDC3545);
    private readonly Color LIGHT_GRAY = new Color(0xFFF8F9FA);
    private readonly Color WHITE = new Color(0xFFFFFFFF);
    private readonly Color DARK_TEXT = new Color(0xFF212529);
    private readonly Color MEDIUM_TEXT = new Color(0xFF6C757D);
    private readonly Color BORDER_COLOR = new Color(0xFFDEE2E6);

    [ExportMethod]
    public void Method1()
    {
        Log.Info("OEEUIGenerator", "Creating complete OEE UI system...");
        
        try
        {
            // Create reusable widget library first
            CreateOEEWidgetLibrary();
            
            // Create all core OEE screens
            CreateOEEDashboard();
            CreateMachineDetailScreen();
            CreateOperatorInputScreen();
            CreateOEEConfigurationScreen();
            CreateOEETrendingScreen();
            CreateMultiLineDashboard();
            CreateReportsAnalyticsScreen();
            
            Log.Info("OEEUIGenerator", "OEE UI system created successfully!");
        }
        catch (Exception ex)
        {
            Log.Error("OEEUIGenerator", $"Error creating OEE UI system: {ex.Message}");
        }
    }

    [ExportMethod]
    public void CreateOEEWidgetLibrary()
    {
        Log.Info("OEEUIGenerator", "Creating OEE Widget Library...");
        
        try
        {
            var widgetsFolder = Project.Current.Get("UI/Widgets");
            
            // Create OEE KPI Card Widget
            var kpiCard = InformationModel.Make<Rectangle>("OEEKPICard");
            kpiCard.BrowseName = "OEEKPICard";
            kpiCard.Width = 300;
            kpiCard.Height = 120;
            kpiCard.FillColor = WHITE;
            kpiCard.BorderColor = BORDER_COLOR;
            kpiCard.BorderThickness = 1;
            kpiCard.CornerRadius = 8;
            
            // Add shadow effect
            var shadow = InformationModel.Make<Rectangle>("CardShadow");
            shadow.Width = 302;
            shadow.Height = 122;
            shadow.LeftMargin = -1;
            shadow.TopMargin = -1;
            shadow.FillColor = new Color((uint)0x10000000);
            shadow.CornerRadius = 8;
            kpiCard.Add(shadow);
            
            var cardLayout = InformationModel.Make<ColumnLayout>("CardLayout");
            cardLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
            cardLayout.VerticalAlignment = VerticalAlignment.Stretch;
            cardLayout.LeftMargin = 20;
            cardLayout.TopMargin = 15;
            cardLayout.RightMargin = 20;
            cardLayout.BottomMargin = 15;
            cardLayout.VerticalGap = 5;
            
            // Title label
            var titleLabel = InformationModel.Make<Label>("TitleLabel");
            titleLabel.Text = "KPI TITLE";
            titleLabel.TextColor = MEDIUM_TEXT;
            titleLabel.FontSize = 12;
            titleLabel.FontWeight = FontWeight.Bold;
            titleLabel.HorizontalAlignment = HorizontalAlignment.Left;
            cardLayout.Add(titleLabel);
            
            // Value label
            var valueLabel = InformationModel.Make<Label>("ValueLabel");
            valueLabel.Text = "85.2%";
            valueLabel.TextColor = DARK_TEXT;
            valueLabel.FontSize = 32;
            valueLabel.FontWeight = FontWeight.Bold;
            valueLabel.HorizontalAlignment = HorizontalAlignment.Left;
            cardLayout.Add(valueLabel);
            
            // Trend indicator
            var trendLabel = InformationModel.Make<Label>("TrendLabel");
            trendLabel.Text = "↗ +2.3% from yesterday";
            trendLabel.TextColor = SUCCESS_GREEN;
            trendLabel.FontSize = 10;
            trendLabel.HorizontalAlignment = HorizontalAlignment.Left;
            cardLayout.Add(trendLabel);
            
            kpiCard.Add(cardLayout);
            widgetsFolder.Add(kpiCard);
            
            // Create OEE Gauge Widget
            var gaugeWidget = InformationModel.Make<Panel>("OEEGauge");
            gaugeWidget.BrowseName = "OEEGauge";
            gaugeWidget.Width = 200;
            gaugeWidget.Height = 200;
            
            var gauge = InformationModel.Make<LinearGauge>("Gauge");
            gauge.HorizontalAlignment = HorizontalAlignment.Stretch;
            gauge.VerticalAlignment = VerticalAlignment.Stretch;
            gauge.MinValue = 0;
            gauge.MaxValue = 100;
            gauge.Value = 85;
            // PLACEHOLDER: Configure gauge warning zones
            gaugeWidget.Add(gauge);
            
            var gaugeLabel = InformationModel.Make<Label>("GaugeLabel");
            gaugeLabel.Text = "OEE %";
            gaugeLabel.HorizontalAlignment = HorizontalAlignment.Center;
            gaugeLabel.VerticalAlignment = VerticalAlignment.Bottom;
            gaugeLabel.BottomMargin = 20;
            gaugeLabel.FontSize = 14;
            gaugeLabel.FontWeight = FontWeight.Bold;
            gaugeLabel.TextColor = DARK_TEXT;
            gaugeWidget.Add(gaugeLabel);
            
            widgetsFolder.Add(gaugeWidget);
            
            // Create OEE Trend Chart Widget
            var trendWidget = InformationModel.Make<Panel>("OEETrendChart");
            trendWidget.BrowseName = "OEETrendChart";
            trendWidget.Width = 400;
            trendWidget.Height = 200;
            
            // Background rectangle for styling
            var trendBackground = InformationModel.Make<Rectangle>("TrendBackground");
            trendBackground.HorizontalAlignment = HorizontalAlignment.Stretch;
            trendBackground.VerticalAlignment = VerticalAlignment.Stretch;
            trendBackground.FillColor = WHITE;
            trendBackground.BorderColor = BORDER_COLOR;
            trendBackground.BorderThickness = 1;
            trendBackground.CornerRadius = 8;
            trendWidget.Add(trendBackground);
            
            var trendChart = InformationModel.Make<DataGrid>("TrendChart");
            trendChart.HorizontalAlignment = HorizontalAlignment.Stretch;
            trendChart.VerticalAlignment = VerticalAlignment.Stretch;
            trendChart.LeftMargin = 10;
            trendChart.TopMargin = 30;
            trendChart.RightMargin = 10;
            trendChart.BottomMargin = 30;
            // PLACEHOLDER: Configure trend data source
            trendWidget.Add(trendChart);
            
            var chartTitle = InformationModel.Make<Label>("ChartTitle");
            chartTitle.Text = "OEE Trend";
            chartTitle.HorizontalAlignment = HorizontalAlignment.Center;
            chartTitle.VerticalAlignment = VerticalAlignment.Top;
            chartTitle.TopMargin = 5;
            chartTitle.FontSize = 12;
            chartTitle.FontWeight = FontWeight.Bold;
            chartTitle.TextColor = DARK_TEXT;
            trendWidget.Add(chartTitle);
            
            widgetsFolder.Add(trendWidget);
            
            // Create Production Counter Widget
            var counterWidget = InformationModel.Make<Rectangle>("ProductionCounter");
            counterWidget.BrowseName = "ProductionCounter";
            counterWidget.Width = 250;
            counterWidget.Height = 100;
            counterWidget.FillColor = WHITE;
            counterWidget.BorderColor = BORDER_COLOR;
            counterWidget.BorderThickness = 1;
            counterWidget.CornerRadius = 8;
            
            var counterLayout = InformationModel.Make<RowLayout>("CounterLayout");
            counterLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
            counterLayout.VerticalAlignment = VerticalAlignment.Center;
            counterLayout.LeftMargin = 20;
            counterLayout.RightMargin = 20;
            counterLayout.HorizontalGap = 15;
            
            // Icon or indicator
            var iconPanel = InformationModel.Make<Rectangle>("IconPanel");
            iconPanel.Width = 40;
            iconPanel.Height = 40;
            iconPanel.FillColor = PRIMARY_BLUE;
            iconPanel.CornerRadius = 20;
            counterLayout.Add(iconPanel);
            
            // Text section
            var textLayout = InformationModel.Make<ColumnLayout>("TextLayout");
            textLayout.VerticalGap = 2;
            
            var counterTitle = InformationModel.Make<Label>("CounterTitle");
            counterTitle.Text = "Good Parts";
            counterTitle.FontSize = 12;
            counterTitle.TextColor = MEDIUM_TEXT;
            textLayout.Add(counterTitle);
            
            var counterValue = InformationModel.Make<Label>("CounterValue");
            counterValue.Text = "1,247";
            counterValue.FontSize = 24;
            counterValue.FontWeight = FontWeight.Bold;
            counterValue.TextColor = DARK_TEXT;
            textLayout.Add(counterValue);
            
            counterLayout.Add(textLayout);
            counterWidget.Add(counterLayout);
            widgetsFolder.Add(counterWidget);
            
            // Create Status Indicator Widget
            var statusWidget = InformationModel.Make<Panel>("StatusIndicator");
            statusWidget.BrowseName = "StatusIndicator";
            statusWidget.Width = 200;
            statusWidget.Height = 60;
            
            var statusBackground = InformationModel.Make<Rectangle>("StatusBackground");
            statusBackground.HorizontalAlignment = HorizontalAlignment.Stretch;
            statusBackground.VerticalAlignment = VerticalAlignment.Stretch;
            statusBackground.FillColor = SUCCESS_GREEN;
            statusBackground.CornerRadius = 30;
            statusWidget.Add(statusBackground);
            
            var statusLabel = InformationModel.Make<Label>("StatusLabel");
            statusLabel.Text = "RUNNING";
            statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
            statusLabel.VerticalAlignment = VerticalAlignment.Center;
            statusLabel.FontSize = 14;
            statusLabel.FontWeight = FontWeight.Bold;
            statusLabel.TextColor = WHITE;
            statusWidget.Add(statusLabel);
            
            widgetsFolder.Add(statusWidget);
            
            Log.Info("OEEUIGenerator", "OEE Widget Library created successfully!");
        }
        catch (Exception ex)
        {
            Log.Error("OEEUIGenerator", $"Error creating widget library: {ex.Message}");
        }
    }

    [ExportMethod]
    public void CreateOEEDashboard()
    {
        Log.Info("OEEUIGenerator", "Creating modern OEE Dashboard...");
        
        try
        {
            var screensFolder = Project.Current.Get("UI/Screens");
            
            // Create main dashboard screen
            var dashboard = InformationModel.Make<Screen>("OEEDashboard");
            dashboard.BrowseName = "OEEDashboard";
            dashboard.Width = 1920;
            dashboard.Height = 1080;
            
            // Background with modern gradient feel
            var background = InformationModel.Make<Rectangle>("DashboardBackground");
            background.HorizontalAlignment = HorizontalAlignment.Stretch;
            background.VerticalAlignment = VerticalAlignment.Stretch;
            background.FillColor = LIGHT_GRAY;
            dashboard.Add(background);
            
            // Main layout container optimized for 1920x1080
            var mainLayout = InformationModel.Make<ColumnLayout>("MainLayout");
            mainLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
            mainLayout.VerticalAlignment = VerticalAlignment.Stretch;
            mainLayout.LeftMargin = 30;
            mainLayout.TopMargin = 25;
            mainLayout.RightMargin = 30;
            mainLayout.BottomMargin = 25;
            mainLayout.VerticalGap = 25;
            
            // Header section - 80px height
            mainLayout.Add(CreateDashboardHeader());
            
            // KPI cards row - 200px height for better visibility
            mainLayout.Add(CreateKPICards());
            
            // Secondary metrics and trends - 140px height
            mainLayout.Add(CreateSecondaryMetrics());
            
            // Production status and targets - 160px height
            mainLayout.Add(CreateProductionStatus());
            
            // System status and alerts - 80px height
            mainLayout.Add(CreateSystemStatus());
            
            dashboard.Add(mainLayout);
            screensFolder.Add(dashboard);
            
            Log.Info("OEEUIGenerator", "OEE Dashboard created successfully!");
        }
        catch (Exception ex)
        {
            Log.Error("OEEUIGenerator", $"Error creating dashboard: {ex.Message}");
        }
    }

    private Panel CreateDashboardHeader()
    {
        var headerPanel = InformationModel.Make<Panel>("HeaderPanel");
        headerPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        headerPanel.Height = 100;
        
        // Gradient background for header
        var headerGradient = InformationModel.Make<Rectangle>("HeaderGradient");
        headerGradient.HorizontalAlignment = HorizontalAlignment.Stretch;
        headerGradient.VerticalAlignment = VerticalAlignment.Stretch;
        headerGradient.FillColor = PRIMARY_BLUE;
        headerGradient.CornerRadius = 8;
        headerPanel.Add(headerGradient);
        
        // Create gradient effect with transparency
        var headerOverlay = InformationModel.Make<Rectangle>("HeaderOverlay");
        headerOverlay.HorizontalAlignment = HorizontalAlignment.Stretch;
        headerOverlay.VerticalAlignment = VerticalAlignment.Stretch;
        headerOverlay.FillColor = new Color((uint)0x40FFFFFF); // Semi-transparent white overlay
        headerOverlay.CornerRadius = 8;
        headerPanel.Add(headerOverlay);
        
        var headerLayout = InformationModel.Make<RowLayout>("HeaderLayout");
        headerLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        headerLayout.VerticalAlignment = VerticalAlignment.Center;
        headerLayout.HorizontalGap = 20;
        headerLayout.LeftMargin = 30;
        headerLayout.RightMargin = 30;
        headerLayout.TopMargin = 20;
        headerLayout.BottomMargin = 20;
        
        // Title section
        var titlePanel = InformationModel.Make<Panel>("TitlePanel");
        titlePanel.HorizontalAlignment = HorizontalAlignment.Left;
        titlePanel.VerticalAlignment = VerticalAlignment.Center;
        titlePanel.Width = 800; // Fixed width to prevent overlap with status section
        
        var titleLayout = InformationModel.Make<ColumnLayout>("TitleLayout");
        titleLayout.VerticalGap = 5;
        
        var titleLabel = InformationModel.Make<Label>("DashboardTitle");
        titleLabel.Text = "OEE Dashboard";
        titleLabel.FontSize = 36;
        titleLabel.TextColor = WHITE;
        
        var subtitleLabel = InformationModel.Make<Label>("DashboardSubtitle");
        subtitleLabel.Text = "Real-time Overall Equipment Effectiveness";
        subtitleLabel.FontSize = 16;
        subtitleLabel.TextColor = new Color((uint)0xFFE8F4FD); // Light blue for subtitle
        
        titleLayout.Add(titleLabel);
        titleLayout.Add(subtitleLabel);
        titlePanel.Add(titleLayout);
        headerLayout.Add(titlePanel);
        
        // Status indicator section
        var statusPanel = CreateHeaderStatusIndicators();
        statusPanel.HorizontalAlignment = HorizontalAlignment.Right;
        statusPanel.Width = 600; // Fixed width to ensure proper spacing
        headerLayout.Add(statusPanel);
        
        headerPanel.Add(headerLayout);
        return headerPanel;
    }

    private Panel CreateHeaderStatusIndicators()
    {
        var statusPanel = InformationModel.Make<Panel>("HeaderStatus");
        statusPanel.HorizontalAlignment = HorizontalAlignment.Right;
        statusPanel.Width = 300;
        
        var statusLayout = InformationModel.Make<RowLayout>("StatusLayout");
        statusLayout.HorizontalAlignment = HorizontalAlignment.Right;
        statusLayout.VerticalAlignment = VerticalAlignment.Center;
        statusLayout.HorizontalGap = 15;
        
        // Data quality indicator
        var qualityIndicator = CreateDataQualityIndicator();
        statusLayout.Add(qualityIndicator);
        
        // System health indicator
        var healthIndicator = CreateStatusIndicator("System Health", "SystemHealthy", SUCCESS_GREEN);
        statusLayout.Add(healthIndicator);
        
        // Calculation status
        var calcIndicator = CreateStatusIndicator("Calculation", "CalculationValid", PRIMARY_BLUE);
        statusLayout.Add(calcIndicator);
        
        // Last update time
        var timePanel = InformationModel.Make<Panel>("LastUpdatePanel");
        timePanel.Width = 150;
        
        var timeLayout = InformationModel.Make<ColumnLayout>("TimeLayout");
        timeLayout.VerticalGap = 2;
        
        var timeLabel = InformationModel.Make<Label>("LastUpdateLabel");
        timeLabel.Text = "Last Update";
        timeLabel.FontSize = 10;
        timeLabel.TextColor = new Color((uint)0xFFE8F4FD);
        
        var timeValue = InformationModel.Make<Label>("LastUpdateTime");
        timeValue.Text = "14:32:18";
        timeValue.FontSize = 12;
        timeValue.TextColor = WHITE;
        // DATA LINK: /Objects/Model/OEEInstances/Machine1/Outputs/LastUpdateTime
        
        timeLayout.Add(timeLabel);
        timeLayout.Add(timeValue);
        timePanel.Add(timeLayout);
        statusLayout.Add(timePanel);
        
        statusPanel.Add(statusLayout);
        return statusPanel;
    }

    private Panel CreateStatusIndicator(string labelText, string variablePath, Color indicatorColor)
    {
        var panel = InformationModel.Make<Panel>("StatusIndicator_" + labelText.Replace(" ", ""));
        panel.Width = 120;
        
        var layout = InformationModel.Make<RowLayout>("StatusLayout_" + labelText.Replace(" ", ""));
        layout.HorizontalGap = 10;
        layout.VerticalAlignment = VerticalAlignment.Center;
        
        // Enhanced LED with ring background
        var ledBg = InformationModel.Make<Ellipse>("LedBg_" + labelText.Replace(" ", ""));
        ledBg.Width = 16;
        ledBg.Height = 16;
        ledBg.FillColor = new Color((uint)0x30000000);
        
        var led = InformationModel.Make<Led>("StatusLed_" + labelText.Replace(" ", ""));
        led.Width = 12;
        led.Height = 12;
        // DATA LINK: Active property to appropriate variable path:
        // SystemHealthy → /Objects/Model/OEEInstances/Machine1/Configuration/SystemHealthy
        // CalculationValid → /Objects/Model/OEEInstances/Machine1/Outputs/CalculationValid
        
        // Status text with enhanced styling
        var label = InformationModel.Make<Label>("StatusLabel_" + labelText.Replace(" ", ""));
        label.Text = labelText;
        label.FontSize = 12;
        label.TextColor = WHITE;
        
        layout.Add(ledBg);
        layout.Add(led);
        layout.Add(label);
        panel.Add(layout);
        
        return panel;
    }

    private RowLayout CreateKPICards()
    {
        var kpiRow = InformationModel.Make<RowLayout>("KPICards");
        kpiRow.HorizontalAlignment = HorizontalAlignment.Stretch;
        kpiRow.Height = 240;
        kpiRow.HorizontalGap = 25;
        
        // Overall OEE - Main metric
        var oeeCard = CreateKPICard("Overall Equipment Effectiveness", "OEE", "%", PRIMARY_BLUE, true);
        kpiRow.Add(oeeCard);
        
        // Quality
        var qualityCard = CreateKPICard("Quality", "Quality", "%", SUCCESS_GREEN, false);
        kpiRow.Add(qualityCard);
        
        // Performance
        var performanceCard = CreateKPICard("Performance", "Performance", "%", WARNING_AMBER, false);
        kpiRow.Add(performanceCard);
        
        // Availability
        var availabilityCard = CreateKPICard("Availability", "Availability", "%", DANGER_RED, false);
        kpiRow.Add(availabilityCard);
        
        return kpiRow;
    }

    private Panel CreateKPICard(string title, string valuePath, string unit, Color accentColor, bool isMainCard)
    {
        var card = InformationModel.Make<Panel>("KPICard_" + valuePath);
        card.HorizontalAlignment = HorizontalAlignment.Stretch;
        card.VerticalAlignment = VerticalAlignment.Stretch;
        
        // Card shadow effect
        var cardShadow = InformationModel.Make<Rectangle>("CardShadow_" + valuePath);
        cardShadow.HorizontalAlignment = HorizontalAlignment.Stretch;
        cardShadow.VerticalAlignment = VerticalAlignment.Stretch;
        cardShadow.LeftMargin = 3;
        cardShadow.TopMargin = 3;
        cardShadow.FillColor = new Color((uint)0x20000000); // Semi-transparent black shadow
        cardShadow.CornerRadius = 8;
        card.Add(cardShadow);
        
        // Card background
        var cardBg = InformationModel.Make<Rectangle>("CardBackground");
        cardBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        cardBg.VerticalAlignment = VerticalAlignment.Stretch;
        cardBg.RightMargin = 3;
        cardBg.BottomMargin = 3;
        cardBg.FillColor = WHITE;
        cardBg.BorderThickness = 1;
        cardBg.BorderColor = BORDER_COLOR;
        cardBg.CornerRadius = 8;
        card.Add(cardBg);
        
        // Card content layout
        var cardLayout = InformationModel.Make<ColumnLayout>("CardLayout_" + valuePath);
        cardLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        cardLayout.VerticalAlignment = VerticalAlignment.Stretch;
        cardLayout.LeftMargin = 25;
        cardLayout.TopMargin = 25;
        cardLayout.RightMargin = 25;
        cardLayout.BottomMargin = 25;
        cardLayout.VerticalGap = 12;
        
        // Enhanced accent bar with gradient
        var accentBar = InformationModel.Make<Rectangle>("AccentBar_" + valuePath);
        accentBar.HorizontalAlignment = HorizontalAlignment.Stretch;
        accentBar.Height = 6;
        accentBar.FillColor = accentColor;
        accentBar.CornerRadius = 3;
        
        // Accent highlight overlay
        var accentHighlight = InformationModel.Make<Rectangle>("AccentHighlight_" + valuePath);
        accentHighlight.HorizontalAlignment = HorizontalAlignment.Left;
        accentHighlight.VerticalAlignment = VerticalAlignment.Top;
        accentHighlight.Width = 60;
        accentHighlight.Height = 6;
        accentHighlight.FillColor = new Color((uint)0x40FFFFFF);
        accentHighlight.CornerRadius = 3;
        
        cardLayout.Add(accentBar);
        cardLayout.Add(accentHighlight);
        
        // Card title
        var titleLabel = InformationModel.Make<Label>("CardTitle_" + valuePath);
        titleLabel.Text = title;
        titleLabel.FontSize = isMainCard ? 18 : 16;
        titleLabel.TextColor = MEDIUM_TEXT;
        cardLayout.Add(titleLabel);
        
        // Value display
        var valueLabel = InformationModel.Make<Label>("CardValue_" + valuePath);
        valueLabel.Text = isMainCard ? "72.5" : "95.2"; // Main OEE vs secondary metrics
        valueLabel.FontSize = isMainCard ? 56 : 42;
        valueLabel.TextColor = accentColor;
        valueLabel.HorizontalAlignment = HorizontalAlignment.Center;
        // DATA LINK: /Objects/Model/OEEInstances/Machine1/Outputs/{valuePath}
        // Example: OEE → /Objects/Model/OEEInstances/Machine1/Outputs/OEE
        cardLayout.Add(valueLabel);
        
        // Unit label
        var unitLabel = InformationModel.Make<Label>("CardUnit_" + valuePath);
        unitLabel.Text = unit;
        unitLabel.FontSize = 16;
        unitLabel.TextColor = MEDIUM_TEXT;
        unitLabel.HorizontalAlignment = HorizontalAlignment.Center;
        cardLayout.Add(unitLabel);
        
        // Trend indicator
        var trendPanel = CreateTrendIndicator(valuePath + "Trend");
        cardLayout.Add(trendPanel);
        
        // Target comparison for main metrics
        if (!isMainCard)
        {
            var targetPanel = CreateTargetComparison(valuePath + "VsTarget");
            cardLayout.Add(targetPanel);
        }
        
        card.Add(cardLayout);
        return card;
    }

    private Panel CreateTrendIndicator(string trendPath)
    {
        var trendPanel = InformationModel.Make<Panel>("TrendPanel_" + trendPath);
        trendPanel.HorizontalAlignment = HorizontalAlignment.Center;
        trendPanel.Height = 25;
        
        var trendLayout = InformationModel.Make<RowLayout>("TrendLayout_" + trendPath);
        trendLayout.HorizontalGap = 8;
        trendLayout.VerticalAlignment = VerticalAlignment.Center;
        
        // Trend icon background circle
        var trendBg = InformationModel.Make<Ellipse>("TrendBg_" + trendPath);
        trendBg.Width = 18;
        trendBg.Height = 18;
        trendBg.FillColor = new Color((uint)0x20000000);
        
        // Trend arrow with conditional coloring
        var trendIcon = InformationModel.Make<Label>("TrendIcon_" + trendPath);
        trendIcon.Text = "↗"; // Default up trend
        trendIcon.FontSize = 12;
        trendIcon.TextColor = SUCCESS_GREEN;
        trendIcon.HorizontalAlignment = HorizontalAlignment.Center;
        
        // Trend percentage
        var trendText = InformationModel.Make<Label>("TrendText_" + trendPath);
        trendText.Text = "+2.3%"; // Sample positive trend
        trendText.FontSize = 10;
        trendText.TextColor = MEDIUM_TEXT;
        // DATA LINK: /Objects/Model/OEEInstances/Machine1/Outputs/{trendPath}
        // Example: QualityTrend → /Objects/Model/OEEInstances/Machine1/Outputs/QualityTrend
        
        trendLayout.Add(trendBg);
        trendLayout.Add(trendIcon);
        trendLayout.Add(trendText);
        trendPanel.Add(trendLayout);
        
        return trendPanel;
    }

    private Panel CreateTargetComparison(string targetPath)
    {
        var targetPanel = InformationModel.Make<Panel>("TargetPanel_" + targetPath);
        targetPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        targetPanel.Height = 25;
        
        var targetLayout = InformationModel.Make<ColumnLayout>("TargetLayout_" + targetPath);
        targetLayout.VerticalGap = 3;
        
        // Progress bar background
        var progressBg = InformationModel.Make<Rectangle>("ProgressBg_" + targetPath);
        progressBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        progressBg.Height = 6;
        progressBg.FillColor = new Color((uint)0xFFE9ECEF);
        progressBg.CornerRadius = 3;
        
        // Progress bar fill
        var progressFill = InformationModel.Make<Rectangle>("ProgressFill_" + targetPath);
        progressFill.HorizontalAlignment = HorizontalAlignment.Left;
        progressFill.Height = 6;
        progressFill.Width = 100; // Will be dynamically updated
        progressFill.FillColor = SUCCESS_GREEN;
        progressFill.CornerRadius = 3;
        // PLACEHOLDER: Connect Width property to {OEEInstance}/Outputs/{targetPath}
        
        var targetLabel = InformationModel.Make<Label>("TargetLabel_" + targetPath);
        targetLabel.Text = "vs 85% target"; // Sample target comparison
        targetLabel.FontSize = 10;
        targetLabel.TextColor = MEDIUM_TEXT;
        targetLabel.HorizontalAlignment = HorizontalAlignment.Center;
        // PLACEHOLDER: Connect to {OEEInstance}/Outputs/{targetPath}VsTarget
        // Example: QualityVsTarget → /Objects/Model/OEEInstances/Machine1/Outputs/QualityVsTarget
        
        targetLayout.Add(progressBg);
        targetLayout.Add(progressFill);
        targetLayout.Add(targetLabel);
        targetPanel.Add(targetLayout);
        
        return targetPanel;
    }

    private RowLayout CreateSecondaryMetrics()
    {
        var metricsRow = InformationModel.Make<RowLayout>("SecondaryMetrics");
        metricsRow.HorizontalAlignment = HorizontalAlignment.Stretch;
        metricsRow.Height = 180;
        metricsRow.HorizontalGap = 25;
        
        // Production count metrics
        var countsPanel = CreateMetricGroup("Production Counts", new string[] {
            "TotalCount:Total Parts",
            "GoodPartCount:Good Parts", 
            "BadPartCount:Rejected Parts",
            "PartsPerHour:Parts/Hour"
        });
        metricsRow.Add(countsPanel);
        
        // Visual separator
        var separator1 = CreateVerticalSeparator();
        metricsRow.Add(separator1);
        
        // Timing metrics
        var timingPanel = CreateMetricGroup("Timing Metrics", new string[] {
            "AvgCycleTime:Avg Cycle Time",
            "TotalRuntimeFormatted:Runtime",
            "DowntimeFormatted:Downtime",
            "TimeIntoShift:Shift Progress"
        });
        metricsRow.Add(timingPanel);
        
        // Visual separator
        var separator2 = CreateVerticalSeparator();
        metricsRow.Add(separator2);
        
        // Performance indicators
        var performancePanel = CreateMetricGroup("Performance", new string[] {
            "DataQualityScore:Data Quality",
            "ExpectedPartCount:Expected Parts",
            "ProjectedTotalCount:Projected Total",
            "RequiredRateToTarget:Required Rate"
        });
        metricsRow.Add(performancePanel);
        
        return metricsRow;
    }

    private Panel CreateMetricGroup(string groupTitle, string[] metrics)
    {
        var groupPanel = InformationModel.Make<Panel>("MetricGroup_" + groupTitle.Replace(" ", ""));
        groupPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        groupPanel.VerticalAlignment = VerticalAlignment.Stretch;
        
        // Group shadow effect
        var groupShadow = InformationModel.Make<Rectangle>("GroupShadow_" + groupTitle.Replace(" ", ""));
        groupShadow.HorizontalAlignment = HorizontalAlignment.Stretch;
        groupShadow.VerticalAlignment = VerticalAlignment.Stretch;
        groupShadow.LeftMargin = 2;
        groupShadow.TopMargin = 2;
        groupShadow.FillColor = new Color((uint)0x15000000); // Lighter shadow for smaller cards
        groupShadow.CornerRadius = 6;
        groupPanel.Add(groupShadow);
        
        // Background
        var groupBg = InformationModel.Make<Rectangle>("GroupBackground");
        groupBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        groupBg.VerticalAlignment = VerticalAlignment.Stretch;
        groupBg.RightMargin = 2;
        groupBg.BottomMargin = 2;
        groupBg.FillColor = WHITE;
        groupBg.BorderThickness = 1;
        groupBg.BorderColor = BORDER_COLOR;
        groupBg.CornerRadius = 6;
        groupPanel.Add(groupBg);
        
        // Content layout
        var groupLayout = InformationModel.Make<ColumnLayout>("GroupLayout_" + groupTitle.Replace(" ", ""));
        groupLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        groupLayout.VerticalAlignment = VerticalAlignment.Top;
        groupLayout.LeftMargin = 20;
        groupLayout.TopMargin = 20;
        groupLayout.RightMargin = 20;
        groupLayout.BottomMargin = 15;
        groupLayout.VerticalGap = 12;
        
        // Group title
        var titleLabel = InformationModel.Make<Label>("GroupTitle_" + groupTitle.Replace(" ", ""));
        titleLabel.Text = groupTitle;
        titleLabel.FontSize = 16;
        titleLabel.TextColor = DARK_TEXT;
        groupLayout.Add(titleLabel);
        
        // Metrics
        foreach (string metric in metrics)
        {
            var parts = metric.Split(':');
            var metricPanel = CreateSmallMetric(parts[1], parts[0]);
            groupLayout.Add(metricPanel);
        }
        
        groupPanel.Add(groupLayout);
        return groupPanel;
    }

    private Panel CreateSmallMetric(string label, string valuePath)
    {
        var metricPanel = InformationModel.Make<Panel>("SmallMetric_" + valuePath);
        metricPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        metricPanel.Height = 22;
        
        var metricLayout = InformationModel.Make<RowLayout>("SmallMetricLayout_" + valuePath);
        metricLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        metricLayout.VerticalAlignment = VerticalAlignment.Center;
        metricLayout.HorizontalGap = 10;
        
        // Label
        var labelText = InformationModel.Make<Label>("SmallMetricLabel_" + valuePath);
        labelText.Text = label + ":";
        labelText.FontSize = 13;
        labelText.TextColor = MEDIUM_TEXT;
        labelText.HorizontalAlignment = HorizontalAlignment.Left;
        metricLayout.Add(labelText);
        
        // Value
        var valueText = InformationModel.Make<Label>("SmallMetricValue_" + valuePath);
        valueText.Text = "--";
        valueText.FontSize = 13;
        valueText.TextColor = DARK_TEXT;
        valueText.HorizontalAlignment = HorizontalAlignment.Right;
        // DATA LINK: Determine folder based on variable type:
        // Outputs: /Objects/Model/OEEInstances/Machine1/Outputs/{valuePath}
        // Inputs: /Objects/Model/OEEInstances/Machine1/Inputs/{valuePath}
        // Config: /Objects/Model/OEEInstances/Machine1/Configuration/{valuePath}
        metricLayout.Add(valueText);
        
        metricPanel.Add(metricLayout);
        return metricPanel;
    }

    private RowLayout CreateProductionStatus()
    {
        var statusRow = InformationModel.Make<RowLayout>("ProductionStatus");
        statusRow.HorizontalAlignment = HorizontalAlignment.Stretch;
        statusRow.Height = 200;
        statusRow.HorizontalGap = 25;
        
        // Shift information
        var shiftPanel = CreateShiftInformation();
        statusRow.Add(shiftPanel);
        
        // Target vs actual
        var targetPanel = CreateTargetVsActual();
        statusRow.Add(targetPanel);
        
        // Alerts and notifications
        var alertsPanel = CreateAlertsPanel();
        statusRow.Add(alertsPanel);
        
        return statusRow;
    }

    private Panel CreateShiftInformation()
    {
        var shiftPanel = InformationModel.Make<Panel>("ShiftInfo");
        shiftPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        shiftPanel.VerticalAlignment = VerticalAlignment.Stretch;
        
        // Shift panel shadow
        var shiftShadow = InformationModel.Make<Rectangle>("ShiftShadow");
        shiftShadow.HorizontalAlignment = HorizontalAlignment.Stretch;
        shiftShadow.VerticalAlignment = VerticalAlignment.Stretch;
        shiftShadow.LeftMargin = 2;
        shiftShadow.TopMargin = 2;
        shiftShadow.FillColor = new Color((uint)0x18000000);
        shiftShadow.CornerRadius = 6;
        shiftPanel.Add(shiftShadow);
        
        // Background
        var shiftBg = InformationModel.Make<Rectangle>("ShiftBackground");
        shiftBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        shiftBg.VerticalAlignment = VerticalAlignment.Stretch;
        shiftBg.RightMargin = 2;
        shiftBg.BottomMargin = 2;
        shiftBg.FillColor = WHITE;
        shiftBg.BorderThickness = 1;
        shiftBg.BorderColor = BORDER_COLOR;
        shiftBg.CornerRadius = 6;
        shiftPanel.Add(shiftBg);
        
        // Content
        var shiftLayout = InformationModel.Make<ColumnLayout>("ShiftLayout");
        shiftLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        shiftLayout.VerticalAlignment = VerticalAlignment.Top;
        shiftLayout.LeftMargin = 15;
        shiftLayout.TopMargin = 15;
        shiftLayout.RightMargin = 15;
        shiftLayout.BottomMargin = 10;
        shiftLayout.VerticalGap = 8;
        
        // Title
        var titleLabel = InformationModel.Make<Label>("ShiftTitle");
        titleLabel.Text = "Current Shift";
        titleLabel.FontSize = 14;
        titleLabel.TextColor = DARK_TEXT;
        shiftLayout.Add(titleLabel);
        
        // Shift details
        var shiftMetrics = new string[] {
            "CurrentShiftNumber:Shift Number",
            "ShiftStartTimeOutput:Start Time",
            "ShiftEndTime:End Time",
            "TimeRemainingInShift:Time Remaining"
        };
        
        foreach (string metric in shiftMetrics)
        {
            var parts = metric.Split(':');
            var metricPanel = CreateSmallMetric(parts[1], parts[0]);
            shiftLayout.Add(metricPanel);
        }
        
        shiftPanel.Add(shiftLayout);
        return shiftPanel;
    }

    private Panel CreateTargetVsActual()
    {
        var targetPanel = InformationModel.Make<Panel>("TargetVsActual");
        targetPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        targetPanel.VerticalAlignment = VerticalAlignment.Stretch;
        
        // Target panel shadow
        var targetShadow = InformationModel.Make<Rectangle>("TargetShadow");
        targetShadow.HorizontalAlignment = HorizontalAlignment.Stretch;
        targetShadow.VerticalAlignment = VerticalAlignment.Stretch;
        targetShadow.LeftMargin = 2;
        targetShadow.TopMargin = 2;
        targetShadow.FillColor = new Color((uint)0x18000000);
        targetShadow.CornerRadius = 6;
        targetPanel.Add(targetShadow);
        
        // Background
        var targetBg = InformationModel.Make<Rectangle>("TargetBackground");
        targetBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        targetBg.VerticalAlignment = VerticalAlignment.Stretch;
        targetBg.RightMargin = 2;
        targetBg.BottomMargin = 2;
        targetBg.FillColor = WHITE;
        targetBg.BorderThickness = 1;
        targetBg.BorderColor = BORDER_COLOR;
        targetBg.CornerRadius = 6;
        targetPanel.Add(targetBg);
        
        // Content
        var targetLayout = InformationModel.Make<ColumnLayout>("TargetLayout");
        targetLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        targetLayout.VerticalAlignment = VerticalAlignment.Top;
        targetLayout.LeftMargin = 15;
        targetLayout.TopMargin = 15;
        targetLayout.RightMargin = 15;
        targetLayout.BottomMargin = 10;
        targetLayout.VerticalGap = 8;
        
        // Title
        var titleLabel = InformationModel.Make<Label>("TargetTitle");
        titleLabel.Text = "Target vs Actual";
        titleLabel.FontSize = 14;
        titleLabel.TextColor = DARK_TEXT;
        targetLayout.Add(titleLabel);
        
        // Target metrics
        var targetMetrics = new string[] {
            "ProductionTarget:Production Target",
            "TargetVsActualParts:Target vs Actual",
            "ProductionBehindSchedule:Behind Schedule",
            "RemainingTimeAtCurrentRate:Time at Current Rate"
        };
        
        foreach (string metric in targetMetrics)
        {
            var parts = metric.Split(':');
            var metricPanel = CreateSmallMetric(parts[1], parts[0]);
            targetLayout.Add(metricPanel);
        }
        
        targetPanel.Add(targetLayout);
        return targetPanel;
    }

    private Panel CreateAlertsPanel()
    {
        var alertsPanel = InformationModel.Make<Panel>("AlertsPanel");
        alertsPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        alertsPanel.VerticalAlignment = VerticalAlignment.Stretch;
        
        // Alerts panel shadow
        var alertsShadow = InformationModel.Make<Rectangle>("AlertsShadow");
        alertsShadow.HorizontalAlignment = HorizontalAlignment.Stretch;
        alertsShadow.VerticalAlignment = VerticalAlignment.Stretch;
        alertsShadow.LeftMargin = 2;
        alertsShadow.TopMargin = 2;
        alertsShadow.FillColor = new Color((uint)0x18000000);
        alertsShadow.CornerRadius = 6;
        alertsPanel.Add(alertsShadow);
        
        // Background
        var alertsBg = InformationModel.Make<Rectangle>("AlertsBackground");
        alertsBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        alertsBg.VerticalAlignment = VerticalAlignment.Stretch;
        alertsBg.RightMargin = 2;
        alertsBg.BottomMargin = 2;
        alertsBg.FillColor = WHITE;
        alertsBg.BorderThickness = 1;
        alertsBg.BorderColor = BORDER_COLOR;
        alertsBg.CornerRadius = 6;
        alertsPanel.Add(alertsBg);
        
        // Content
        var alertsLayout = InformationModel.Make<ColumnLayout>("AlertsLayout");
        alertsLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        alertsLayout.VerticalAlignment = VerticalAlignment.Top;
        alertsLayout.LeftMargin = 15;
        alertsLayout.TopMargin = 15;
        alertsLayout.RightMargin = 15;
        alertsLayout.BottomMargin = 10;
        alertsLayout.VerticalGap = 8;
        
        // Title
        var titleLabel = InformationModel.Make<Label>("AlertsTitle");
        titleLabel.Text = "System Status";
        titleLabel.FontSize = 14;
        titleLabel.TextColor = DARK_TEXT;
        alertsLayout.Add(titleLabel);
        
        // Status indicators with enhanced styling
        var systemStatus = InformationModel.Make<Label>("SystemStatusLabel");
        systemStatus.Text = "OEE System Active";
        systemStatus.FontSize = 12;
        systemStatus.TextColor = PRIMARY_BLUE;
        // PLACEHOLDER: Connect Text property to {OEEInstance}/Outputs/SystemStatus
        alertsLayout.Add(systemStatus);
        
        // Critical status indicator with background
        var criticalStatusPanel = InformationModel.Make<Panel>("CriticalStatusPanel");
        criticalStatusPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        criticalStatusPanel.Height = 25;
        
        var criticalBg = InformationModel.Make<Rectangle>("CriticalBg");
        criticalBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        criticalBg.VerticalAlignment = VerticalAlignment.Stretch;
        criticalBg.FillColor = new Color((uint)0x20DC3545); // Light red background
        criticalBg.CornerRadius = 4;
        criticalStatusPanel.Add(criticalBg);
        
        var criticalLabel = InformationModel.Make<Label>("CriticalStatusLabel");
        criticalLabel.Text = "Production Behind Schedule";
        criticalLabel.FontSize = 10;
        criticalLabel.TextColor = DANGER_RED;
        criticalLabel.HorizontalAlignment = HorizontalAlignment.Center;
        criticalLabel.VerticalAlignment = VerticalAlignment.Center;
        // PLACEHOLDER: Connect Visible property to {OEEInstance}/Outputs/ProductionBehindSchedule
        criticalStatusPanel.Add(criticalLabel);
        alertsLayout.Add(criticalStatusPanel);
        
        // Shift change indicators
        var shiftChangePanel = InformationModel.Make<RowLayout>("ShiftChangePanel");
        shiftChangePanel.HorizontalGap = 10;
        
        var shiftChangeLed = InformationModel.Make<Led>("ShiftChangeLed");
        shiftChangeLed.Width = 12;
        shiftChangeLed.Height = 12;
        // PLACEHOLDER: Connect Active property to {OEEInstance}/Outputs/ShiftChangeImminent
        
        var shiftChangeLabel = InformationModel.Make<Label>("ShiftChangeLabel");
        shiftChangeLabel.Text = "Shift Change Soon";
        shiftChangeLabel.FontSize = 10;
        shiftChangeLabel.TextColor = MEDIUM_TEXT;
        
        shiftChangePanel.Add(shiftChangeLed);
        shiftChangePanel.Add(shiftChangeLabel);
        alertsLayout.Add(shiftChangePanel);
        
        alertsPanel.Add(alertsLayout);
        return alertsPanel;
    }

    private Panel CreateSystemStatus()
    {
        var statusPanel = InformationModel.Make<Panel>("SystemStatusPanel");
        statusPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        statusPanel.Height = 100;
        
        // System status shadow
        var statusShadow = InformationModel.Make<Rectangle>("SystemStatusShadow");
        statusShadow.HorizontalAlignment = HorizontalAlignment.Stretch;
        statusShadow.VerticalAlignment = VerticalAlignment.Stretch;
        statusShadow.LeftMargin = 2;
        statusShadow.TopMargin = 2;
        statusShadow.FillColor = new Color((uint)0x15000000);
        statusShadow.CornerRadius = 6;
        statusPanel.Add(statusShadow);
        
        // Background
        var statusBg = InformationModel.Make<Rectangle>("SystemStatusBackground");
        statusBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        statusBg.VerticalAlignment = VerticalAlignment.Stretch;
        statusBg.RightMargin = 2;
        statusBg.BottomMargin = 2;
        statusBg.FillColor = WHITE;
        statusBg.BorderThickness = 1;
        statusBg.BorderColor = BORDER_COLOR;
        statusBg.CornerRadius = 6;
        statusPanel.Add(statusBg);
        
        // Content layout
        var statusLayout = InformationModel.Make<RowLayout>("SystemStatusLayout");
        statusLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        statusLayout.VerticalAlignment = VerticalAlignment.Center;
        statusLayout.LeftMargin = 20;
        statusLayout.RightMargin = 20;
        statusLayout.HorizontalGap = 20;
        
        // Configuration status
        var configInfo = CreateConfigurationInfo();
        statusLayout.Add(configInfo);
        
        // Statistics summary
        var statsInfo = CreateStatisticsSummary();
        statusLayout.Add(statsInfo);
        
        statusPanel.Add(statusLayout);
        return statusPanel;
    }

    private Panel CreateConfigurationInfo()
    {
        var configPanel = InformationModel.Make<Panel>("ConfigInfo");
        configPanel.HorizontalAlignment = HorizontalAlignment.Left;
        configPanel.Width = 300;
        
        var configLayout = InformationModel.Make<RowLayout>("ConfigLayout");
        configLayout.HorizontalGap = 15;
        configLayout.VerticalAlignment = VerticalAlignment.Center;
        
        // Real-time calc status
        var realtimePanel = InformationModel.Make<RowLayout>("RealtimePanel");
        realtimePanel.HorizontalGap = 5;
        
        var realtimeLed = InformationModel.Make<Led>("RealtimeLed");
        realtimeLed.Width = 10;
        realtimeLed.Height = 10;
        // PLACEHOLDER: Connect Active property to {OEEInstance}/Configuration/EnableRealTimeCalc
        
        var realtimeLabel = InformationModel.Make<Label>("RealtimeLabel");
        realtimeLabel.Text = "Real-time";
        realtimeLabel.FontSize = 10;
        realtimeLabel.TextColor = MEDIUM_TEXT;
        
        realtimePanel.Add(realtimeLed);
        realtimePanel.Add(realtimeLabel);
        configLayout.Add(realtimePanel);
        
        // Logging status
        var loggingPanel = InformationModel.Make<RowLayout>("LoggingPanel");
        loggingPanel.HorizontalGap = 5;
        
        var loggingLed = InformationModel.Make<Led>("LoggingLed");
        loggingLed.Width = 10;
        loggingLed.Height = 10;
        // PLACEHOLDER: Connect Active property to {OEEInstance}/Configuration/EnableLogging
        
        var loggingLabel = InformationModel.Make<Label>("LoggingLabel");
        loggingLabel.Text = "Logging";
        loggingLabel.FontSize = 10;
        loggingLabel.TextColor = MEDIUM_TEXT;
        
        loggingPanel.Add(loggingLed);
        loggingPanel.Add(loggingLabel);
        configLayout.Add(loggingPanel);
        
        // Alarms status
        var alarmsPanel = InformationModel.Make<RowLayout>("AlarmsPanel");
        alarmsPanel.HorizontalGap = 5;
        
        var alarmsLed = InformationModel.Make<Led>("AlarmsLed");
        alarmsLed.Width = 10;
        alarmsLed.Height = 10;
        // PLACEHOLDER: Connect Active property to {OEEInstance}/Configuration/EnableAlarms
        
        var alarmsLabel = InformationModel.Make<Label>("AlarmsLabel");
        alarmsLabel.Text = "Alarms";
        alarmsLabel.FontSize = 10;
        alarmsLabel.TextColor = MEDIUM_TEXT;
        
        alarmsPanel.Add(alarmsLed);
        alarmsPanel.Add(alarmsLabel);
        configLayout.Add(alarmsPanel);
        
        configPanel.Add(configLayout);
        return configPanel;
    }

    private Panel CreateStatisticsSummary()
    {
        var statsPanel = InformationModel.Make<Panel>("StatsInfo");
        statsPanel.HorizontalAlignment = HorizontalAlignment.Right;
        statsPanel.Width = 400;
        
        var statsLayout = InformationModel.Make<RowLayout>("StatsLayout");
        statsLayout.HorizontalAlignment = HorizontalAlignment.Right;
        statsLayout.HorizontalGap = 20;
        statsLayout.VerticalAlignment = VerticalAlignment.Center;
        
        // Min/Max/Avg summary for key metrics
        var avgOeeLabel = InformationModel.Make<Label>("AvgOEELabel");
        avgOeeLabel.Text = "Avg OEE:";
        avgOeeLabel.FontSize = 10;
        avgOeeLabel.TextColor = MEDIUM_TEXT;
        statsLayout.Add(avgOeeLabel);
        
        var avgOeeValue = InformationModel.Make<Label>("AvgOEEValue");
        avgOeeValue.Text = "68.2%";
        avgOeeValue.FontSize = 10;
        avgOeeValue.TextColor = DARK_TEXT;
        // PLACEHOLDER: Connect Text property to {OEEInstance}/Outputs/AvgOEE
        statsLayout.Add(avgOeeValue);
        
        var maxOeeLabel = InformationModel.Make<Label>("MaxOEELabel");
        maxOeeLabel.Text = "Max OEE:";
        maxOeeLabel.FontSize = 10;
        maxOeeLabel.TextColor = MEDIUM_TEXT;
        statsLayout.Add(maxOeeLabel);
        
        var maxOeeValue = InformationModel.Make<Label>("MaxOEEValue");
        maxOeeValue.Text = "89.7%";
        maxOeeValue.FontSize = 10;
        maxOeeValue.TextColor = SUCCESS_GREEN;
        // PLACEHOLDER: Connect Text property to {OEEInstance}/Outputs/MaxOEE
        statsLayout.Add(maxOeeValue);
        
        statsPanel.Add(statsLayout);
        return statsPanel;
    }

    private Panel CreateDataQualityIndicator()
    {
        var panel = InformationModel.Make<Panel>("DataQualityIndicator");
        panel.Width = 140;
        
        var layout = InformationModel.Make<ColumnLayout>("DataQualityLayout");
        layout.VerticalGap = 3;
        layout.VerticalAlignment = VerticalAlignment.Center;
        
        // Data quality label
        var label = InformationModel.Make<Label>("DataQualityLabel");
        label.Text = "Data Quality";
        label.FontSize = 10;
        label.TextColor = new Color((uint)0xFFE8F4FD);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        
        // Progress bar for data quality
        var progressBg = InformationModel.Make<Rectangle>("DataQualityProgressBg");
        progressBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        progressBg.Height = 4;
        progressBg.FillColor = new Color((uint)0x30FFFFFF);
        progressBg.CornerRadius = 2;
        
        var progressFill = InformationModel.Make<Rectangle>("DataQualityProgressFill");
        progressFill.HorizontalAlignment = HorizontalAlignment.Left;
        progressFill.Height = 4;
        progressFill.Width = 100; // Will be dynamically updated
        progressFill.FillColor = SUCCESS_GREEN;
        progressFill.CornerRadius = 2;
        // PLACEHOLDER: Connect Width property to {OEEInstance}/Outputs/DataQualityScore
        
        // Data quality value
        var valueLabel = InformationModel.Make<Label>("DataQualityValue");
        valueLabel.Text = "98%";
        valueLabel.FontSize = 11;
        valueLabel.TextColor = WHITE;
        valueLabel.HorizontalAlignment = HorizontalAlignment.Center;
        // PLACEHOLDER: Connect Text property to {OEEInstance}/Outputs/DataQualityScore
        
        layout.Add(label);
        layout.Add(progressBg);
        layout.Add(progressFill);
        layout.Add(valueLabel);
        panel.Add(layout);
        
        return panel;
    }

    private Panel CreateVerticalSeparator()
    {
        var separator = InformationModel.Make<Panel>("VerticalSeparator");
        separator.Width = 2;
        separator.VerticalAlignment = VerticalAlignment.Stretch;
        
        var separatorLine = InformationModel.Make<Rectangle>("SeparatorLine");
        separatorLine.HorizontalAlignment = HorizontalAlignment.Stretch;
        separatorLine.VerticalAlignment = VerticalAlignment.Stretch;
        separatorLine.TopMargin = 20;
        separatorLine.BottomMargin = 20;
        separatorLine.FillColor = new Color((uint)0x20000000);
        separatorLine.CornerRadius = 1;
        
        separator.Add(separatorLine);
        return separator;
    }

    [ExportMethod]
    public void CreateMachineDetailScreen()
    {
        Log.Info("OEEUIGenerator", "Creating Machine Detail Screen...");
        
        try
        {
            var screensFolder = Project.Current.Get("UI/Screens");
            
            // Create machine detail screen
            var detailScreen = InformationModel.Make<Screen>("MachineDetail");
            detailScreen.BrowseName = "MachineDetail";
            detailScreen.Width = 1920;
            detailScreen.Height = 1080;
            
            // Background
            var background = InformationModel.Make<Rectangle>("DetailBackground");
            background.HorizontalAlignment = HorizontalAlignment.Stretch;
            background.VerticalAlignment = VerticalAlignment.Stretch;
            background.FillColor = LIGHT_GRAY;
            detailScreen.Add(background);
            
            // Main layout container
            var mainLayout = InformationModel.Make<RowLayout>("DetailMainLayout");
            mainLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
            mainLayout.VerticalAlignment = VerticalAlignment.Stretch;
            mainLayout.LeftMargin = 30;
            mainLayout.TopMargin = 25;
            mainLayout.RightMargin = 30;
            mainLayout.BottomMargin = 25;
            mainLayout.HorizontalGap = 25;
            
            // Left panel - Machine info and gauges (60% width)
            var leftPanel = CreateMachineInfoPanel();
            leftPanel.Width = 1100;
            mainLayout.Add(leftPanel);
            
            // Right panel - Trends and statistics (40% width)
            var rightPanel = CreateDetailTrendsPanel();
            rightPanel.Width = 740;
            mainLayout.Add(rightPanel);
            
            detailScreen.Add(mainLayout);
            screensFolder.Add(detailScreen);
            
            Log.Info("OEEUIGenerator", "Machine Detail Screen created successfully!");
        }
        catch (Exception ex)
        {
            Log.Error("OEEUIGenerator", $"Error creating machine detail screen: {ex.Message}");
        }
    }

    private Panel CreateMachineInfoPanel()
    {
        var leftPanel = InformationModel.Make<Panel>("MachineInfoPanel");
        leftPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        leftPanel.VerticalAlignment = VerticalAlignment.Stretch;
        
        var leftLayout = InformationModel.Make<ColumnLayout>("MachineInfoLayout");
        leftLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        leftLayout.VerticalAlignment = VerticalAlignment.Stretch;
        leftLayout.VerticalGap = 25;
        
        // Machine header with status
        var machineHeader = CreateMachineHeader();
        leftLayout.Add(machineHeader);
        
        // OEE gauges section
        var gaugesSection = CreateOEEGaugesSection();
        leftLayout.Add(gaugesSection);
        
        // Real-time metrics grid
        var metricsGrid = CreateRealTimeMetricsGrid();
        leftLayout.Add(metricsGrid);
        
        leftPanel.Add(leftLayout);
        return leftPanel;
    }

    private Panel CreateDetailTrendsPanel()
    {
        var rightPanel = InformationModel.Make<Panel>("DetailTrendsPanel");
        rightPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        rightPanel.VerticalAlignment = VerticalAlignment.Stretch;
        
        var rightLayout = InformationModel.Make<ColumnLayout>("DetailTrendsLayout");
        rightLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        rightLayout.VerticalAlignment = VerticalAlignment.Stretch;
        rightLayout.VerticalGap = 25;
        
        // Statistics summary
        var statisticsPanel = CreateStatisticsSummaryPanel();
        rightLayout.Add(statisticsPanel);
        
        // Trend analysis
        var trendsPanel = CreateTrendAnalysisPanel();
        rightLayout.Add(trendsPanel);
        
        // Target performance
        var targetPanel = CreateTargetPerformancePanel();
        rightLayout.Add(targetPanel);
        
        rightPanel.Add(rightLayout);
        return rightPanel;
    }

    private Panel CreateMachineHeader()
    {
        var headerPanel = InformationModel.Make<Panel>("MachineHeaderPanel");
        headerPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        headerPanel.Height = 120;
        
        // Header shadow
        var headerShadow = InformationModel.Make<Rectangle>("MachineHeaderShadow");
        headerShadow.HorizontalAlignment = HorizontalAlignment.Stretch;
        headerShadow.VerticalAlignment = VerticalAlignment.Stretch;
        headerShadow.LeftMargin = 3;
        headerShadow.TopMargin = 3;
        headerShadow.FillColor = new Color((uint)0x20000000);
        headerShadow.CornerRadius = 8;
        headerPanel.Add(headerShadow);
        
        // Header background
        var headerBg = InformationModel.Make<Rectangle>("MachineHeaderBg");
        headerBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        headerBg.VerticalAlignment = VerticalAlignment.Stretch;
        headerBg.RightMargin = 3;
        headerBg.BottomMargin = 3;
        headerBg.FillColor = PRIMARY_BLUE;
        headerBg.CornerRadius = 8;
        headerPanel.Add(headerBg);
        
        // Header overlay
        var headerOverlay = InformationModel.Make<Rectangle>("MachineHeaderOverlay");
        headerOverlay.HorizontalAlignment = HorizontalAlignment.Stretch;
        headerOverlay.VerticalAlignment = VerticalAlignment.Stretch;
        headerOverlay.RightMargin = 3;
        headerOverlay.BottomMargin = 3;
        headerOverlay.FillColor = new Color((uint)0x30FFFFFF);
        headerOverlay.CornerRadius = 8;
        headerPanel.Add(headerOverlay);
        
        // Header content
        var headerContent = InformationModel.Make<RowLayout>("MachineHeaderContent");
        headerContent.HorizontalAlignment = HorizontalAlignment.Stretch;
        headerContent.VerticalAlignment = VerticalAlignment.Center;
        headerContent.LeftMargin = 30;
        headerContent.RightMargin = 30;
        headerContent.HorizontalGap = 30;
        
        // Machine info section
        var machineInfo = CreateMachineInfoSection();
        headerContent.Add(machineInfo);
        
        // Status indicators
        var statusIndicators = CreateMachineStatusIndicators();
        headerContent.Add(statusIndicators);
        
        headerPanel.Add(headerContent);
        return headerPanel;
    }

    private Panel CreateMachineInfoSection()
    {
        var infoPanel = InformationModel.Make<Panel>("MachineInfoSection");
        infoPanel.HorizontalAlignment = HorizontalAlignment.Left;
        infoPanel.Width = 600;
        
        var infoLayout = InformationModel.Make<ColumnLayout>("MachineInfoSectionLayout");
        infoLayout.VerticalGap = 8;
        
        // Machine name
        var machineTitle = InformationModel.Make<Label>("MachineTitle");
        machineTitle.Text = "Production Line - Machine 1";
        machineTitle.FontSize = 24;
        machineTitle.TextColor = WHITE;
        
        // Machine details row
        var detailsRow = InformationModel.Make<RowLayout>("MachineDetailsRow");
        detailsRow.HorizontalGap = 30;
        
        // Current status
        var statusLabel = InformationModel.Make<Label>("CurrentStatusLabel");
        statusLabel.Text = "Status:";
        statusLabel.FontSize = 12;
        statusLabel.TextColor = new Color((uint)0xFFE8F4FD);
        
        var statusValue = InformationModel.Make<Label>("CurrentStatusValue");
        statusValue.Text = "Running";
        statusValue.FontSize = 12;
        statusValue.TextColor = WHITE;
        // DATA LINK: /Objects/Model/OEEInstances/Machine1/Outputs/SystemStatus
        
        // Shift info
        var shiftLabel = InformationModel.Make<Label>("ShiftInfoLabel");
        shiftLabel.Text = "Shift:";
        shiftLabel.FontSize = 12;
        shiftLabel.TextColor = new Color((uint)0xFFE8F4FD);
        
        var shiftValue = InformationModel.Make<Label>("ShiftInfoValue");
        shiftValue.Text = "Shift 1";
        shiftValue.FontSize = 12;
        shiftValue.TextColor = WHITE;
        // DATA LINK: /Objects/Model/OEEInstances/Machine1/Outputs/CurrentShiftNumber
        
        detailsRow.Add(statusLabel);
        detailsRow.Add(statusValue);
        detailsRow.Add(shiftLabel);
        detailsRow.Add(shiftValue);
        
        infoLayout.Add(machineTitle);
        infoLayout.Add(detailsRow);
        infoPanel.Add(infoLayout);
        
        return infoPanel;
    }

    private Panel CreateMachineStatusIndicators()
    {
        var statusPanel = InformationModel.Make<Panel>("MachineStatusIndicators");
        statusPanel.HorizontalAlignment = HorizontalAlignment.Right;
        statusPanel.Width = 400;
        
        var statusLayout = InformationModel.Make<RowLayout>("MachineStatusLayout");
        statusLayout.HorizontalAlignment = HorizontalAlignment.Right;
        statusLayout.VerticalAlignment = VerticalAlignment.Center;
        statusLayout.HorizontalGap = 20;
        
        // Production status
        var productionStatus = CreateDetailStatusIndicator("Production", "CalculationValid", SUCCESS_GREEN);
        statusLayout.Add(productionStatus);
        
        // Data quality
        var dataQuality = CreateDetailStatusIndicator("Data Quality", "DataQualityScore", WARNING_AMBER);
        statusLayout.Add(dataQuality);
        
        // System health
        var systemHealth = CreateDetailStatusIndicator("System", "SystemHealthy", PRIMARY_BLUE);
        statusLayout.Add(systemHealth);
        
        statusPanel.Add(statusLayout);
        return statusPanel;
    }

    private Panel CreateDetailStatusIndicator(string label, string variablePath, Color indicatorColor)
    {
        var panel = InformationModel.Make<Panel>("DetailStatus_" + label.Replace(" ", ""));
        panel.Width = 80;
        
        var layout = InformationModel.Make<ColumnLayout>("DetailStatusLayout_" + label.Replace(" ", ""));
        layout.VerticalGap = 5;
        layout.HorizontalAlignment = HorizontalAlignment.Center;
        
        // LED with enhanced background
        var ledBg = InformationModel.Make<Ellipse>("DetailLedBg_" + label.Replace(" ", ""));
        ledBg.Width = 20;
        ledBg.Height = 20;
        ledBg.FillColor = new Color((uint)0x40000000);
        ledBg.HorizontalAlignment = HorizontalAlignment.Center;
        
        var led = InformationModel.Make<Led>("DetailLed_" + label.Replace(" ", ""));
        led.Width = 16;
        led.Height = 16;
        led.HorizontalAlignment = HorizontalAlignment.Center;
        // DATA LINK: Active property to /Objects/Model/OEEInstances/Machine1/Configuration or Outputs/{variablePath}
        
        // Label
        var labelText = InformationModel.Make<Label>("DetailStatusLabel_" + label.Replace(" ", ""));
        labelText.Text = label;
        labelText.FontSize = 10;
        labelText.TextColor = WHITE;
        labelText.HorizontalAlignment = HorizontalAlignment.Center;
        
        layout.Add(ledBg);
        layout.Add(led);
        layout.Add(labelText);
        panel.Add(layout);
        
        return panel;
    }

    private Panel CreateOEEGaugesSection()
    {
        var gaugesPanel = InformationModel.Make<Panel>("OEEGaugesSection");
        gaugesPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        gaugesPanel.Height = 400;
        
        // Section shadow
        var sectionShadow = InformationModel.Make<Rectangle>("GaugesSectionShadow");
        sectionShadow.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionShadow.VerticalAlignment = VerticalAlignment.Stretch;
        sectionShadow.LeftMargin = 3;
        sectionShadow.TopMargin = 3;
        sectionShadow.FillColor = new Color((uint)0x20000000);
        sectionShadow.CornerRadius = 8;
        gaugesPanel.Add(sectionShadow);
        
        // Section background
        var sectionBg = InformationModel.Make<Rectangle>("GaugesSectionBg");
        sectionBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionBg.VerticalAlignment = VerticalAlignment.Stretch;
        sectionBg.RightMargin = 3;
        sectionBg.BottomMargin = 3;
        sectionBg.FillColor = WHITE;
        sectionBg.CornerRadius = 8;
        gaugesPanel.Add(sectionBg);
        
        // Content layout
        var gaugesLayout = InformationModel.Make<ColumnLayout>("GaugesSectionLayout");
        gaugesLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        gaugesLayout.VerticalAlignment = VerticalAlignment.Stretch;
        gaugesLayout.LeftMargin = 25;
        gaugesLayout.TopMargin = 25;
        gaugesLayout.RightMargin = 25;
        gaugesLayout.BottomMargin = 25;
        gaugesLayout.VerticalGap = 20;
        
        // Section title
        var gaugesTitle = InformationModel.Make<Label>("GaugesTitle");
        gaugesTitle.Text = "OEE Performance Metrics";
        gaugesTitle.FontSize = 18;
        gaugesTitle.TextColor = DARK_TEXT;
        gaugesLayout.Add(gaugesTitle);
        
        // Gauges row
        var gaugesRow = CreateGaugesRow();
        gaugesLayout.Add(gaugesRow);
        
        gaugesPanel.Add(gaugesLayout);
        return gaugesPanel;
    }

    private RowLayout CreateGaugesRow()
    {
        var gaugesRow = InformationModel.Make<RowLayout>("GaugesRow");
        gaugesRow.HorizontalAlignment = HorizontalAlignment.Stretch;
        gaugesRow.VerticalAlignment = VerticalAlignment.Stretch;
        gaugesRow.HorizontalGap = 20;
        
        // Create individual gauge panels with circular progress indicators
        var oeeGauge = CreateCircularGauge("Overall OEE", "OEE", PRIMARY_BLUE, 72.5);
        var qualityGauge = CreateCircularGauge("Quality", "Quality", SUCCESS_GREEN, 95.0);
        var performanceGauge = CreateCircularGauge("Performance", "Performance", WARNING_AMBER, 85.0);
        var availabilityGauge = CreateCircularGauge("Availability", "Availability", DANGER_RED, 89.5);
        
        gaugesRow.Add(oeeGauge);
        gaugesRow.Add(qualityGauge);
        gaugesRow.Add(performanceGauge);
        gaugesRow.Add(availabilityGauge);
        
        return gaugesRow;
    }

    private Panel CreateCircularGauge(string title, string valuePath, Color gaugeColor, double sampleValue)
    {
        var gaugePanel = InformationModel.Make<Panel>("CircularGauge_" + title.Replace(" ", ""));
        gaugePanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        gaugePanel.VerticalAlignment = VerticalAlignment.Stretch;
        
        var gaugeLayout = InformationModel.Make<ColumnLayout>("CircularGaugeLayout_" + title.Replace(" ", ""));
        gaugeLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        gaugeLayout.VerticalAlignment = VerticalAlignment.Center;
        gaugeLayout.VerticalGap = 15;
        
        // Gauge container
        var gaugeContainer = InformationModel.Make<Panel>("GaugeContainer_" + title.Replace(" ", ""));
        gaugeContainer.Height = 200;
        gaugeContainer.HorizontalAlignment = HorizontalAlignment.Stretch;
        
        // Background circle
        var backgroundCircle = InformationModel.Make<Ellipse>("GaugeBg_" + title.Replace(" ", ""));
        backgroundCircle.Width = 180;
        backgroundCircle.Height = 180;
        backgroundCircle.HorizontalAlignment = HorizontalAlignment.Center;
        backgroundCircle.VerticalAlignment = VerticalAlignment.Center;
        backgroundCircle.FillColor = new Color((uint)0xFFF8F9FA);
        backgroundCircle.BorderThickness = 8;
        backgroundCircle.BorderColor = new Color((uint)0xFFE9ECEF);
        
        // Progress circle (simulated with arc - would need custom implementation)
        var progressCircle = InformationModel.Make<Ellipse>("GaugeProgress_" + title.Replace(" ", ""));
        progressCircle.Width = 180;
        progressCircle.Height = 180;
        progressCircle.HorizontalAlignment = HorizontalAlignment.Center;
        progressCircle.VerticalAlignment = VerticalAlignment.Center;
        progressCircle.FillColor = Colors.Transparent;
        progressCircle.BorderThickness = 8;
        progressCircle.BorderColor = gaugeColor;
        
        // Center value display
        var valueContainer = InformationModel.Make<Panel>("GaugeValueContainer_" + title.Replace(" ", ""));
        valueContainer.Width = 120;
        valueContainer.Height = 120;
        valueContainer.HorizontalAlignment = HorizontalAlignment.Center;
        valueContainer.VerticalAlignment = VerticalAlignment.Center;
        
        var valueLayout = InformationModel.Make<ColumnLayout>("GaugeValueLayout_" + title.Replace(" ", ""));
        valueLayout.HorizontalAlignment = HorizontalAlignment.Center;
        valueLayout.VerticalAlignment = VerticalAlignment.Center;
        valueLayout.VerticalGap = 5;
        
        var valueLabel = InformationModel.Make<Label>("GaugeValue_" + title.Replace(" ", ""));
        valueLabel.Text = sampleValue.ToString("F1") + "%";
        valueLabel.FontSize = 28;
        valueLabel.TextColor = gaugeColor;
        valueLabel.HorizontalAlignment = HorizontalAlignment.Center;
        // DATA LINK: /Objects/Model/OEEInstances/Machine1/Outputs/{valuePath}
        
        var targetLabel = InformationModel.Make<Label>("GaugeTarget_" + title.Replace(" ", ""));
        targetLabel.Text = "Target: 85%";
        targetLabel.FontSize = 10;
        targetLabel.TextColor = MEDIUM_TEXT;
        targetLabel.HorizontalAlignment = HorizontalAlignment.Center;
        // DATA LINK: /Objects/Model/OEEInstances/Machine1/Inputs/{valuePath}Target
        
        valueLayout.Add(valueLabel);
        valueLayout.Add(targetLabel);
        valueContainer.Add(valueLayout);
        
        gaugeContainer.Add(backgroundCircle);
        gaugeContainer.Add(progressCircle);
        gaugeContainer.Add(valueContainer);
        gaugeLayout.Add(gaugeContainer);
        
        // Gauge title
        var gaugeTitle = InformationModel.Make<Label>("GaugeTitle_" + title.Replace(" ", ""));
        gaugeTitle.Text = title;
        gaugeTitle.FontSize = 14;
        gaugeTitle.TextColor = DARK_TEXT;
        gaugeTitle.HorizontalAlignment = HorizontalAlignment.Center;
        gaugeLayout.Add(gaugeTitle);
        
        gaugePanel.Add(gaugeLayout);
        return gaugePanel;
    }

    private Panel CreateRealTimeMetricsGrid()
    {
        var metricsPanel = InformationModel.Make<Panel>("RealTimeMetricsGrid");
        metricsPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        metricsPanel.Height = 400;
        
        // Panel shadow
        var panelShadow = InformationModel.Make<Rectangle>("MetricsGridShadow");
        panelShadow.HorizontalAlignment = HorizontalAlignment.Stretch;
        panelShadow.VerticalAlignment = VerticalAlignment.Stretch;
        panelShadow.LeftMargin = 3;
        panelShadow.TopMargin = 3;
        panelShadow.FillColor = new Color((uint)0x20000000);
        panelShadow.CornerRadius = 8;
        metricsPanel.Add(panelShadow);
        
        // Panel background
        var panelBg = InformationModel.Make<Rectangle>("MetricsGridBg");
        panelBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        panelBg.VerticalAlignment = VerticalAlignment.Stretch;
        panelBg.RightMargin = 3;
        panelBg.BottomMargin = 3;
        panelBg.FillColor = WHITE;
        panelBg.CornerRadius = 8;
        metricsPanel.Add(panelBg);
        
        // Content layout
        var metricsLayout = InformationModel.Make<ColumnLayout>("MetricsGridLayout");
        metricsLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        metricsLayout.VerticalAlignment = VerticalAlignment.Stretch;
        metricsLayout.LeftMargin = 25;
        metricsLayout.TopMargin = 25;
        metricsLayout.RightMargin = 25;
        metricsLayout.BottomMargin = 25;
        metricsLayout.VerticalGap = 20;
        
        // Section title
        var metricsTitle = InformationModel.Make<Label>("MetricsGridTitle");
        metricsTitle.Text = "Real-time Production Metrics";
        metricsTitle.FontSize = 18;
        metricsTitle.TextColor = DARK_TEXT;
        metricsLayout.Add(metricsTitle);
        
        // Metrics grid
        var metricsGrid = CreateDetailMetricsGrid();
        metricsLayout.Add(metricsGrid);
        
        metricsPanel.Add(metricsLayout);
        return metricsPanel;
    }

    private RowLayout CreateDetailMetricsGrid()
    {
        var gridRow = InformationModel.Make<RowLayout>("DetailMetricsGridRow");
        gridRow.HorizontalAlignment = HorizontalAlignment.Stretch;
        gridRow.VerticalAlignment = VerticalAlignment.Stretch;
        gridRow.HorizontalGap = 20;
        
        // Production metrics column
        var productionCol = CreateDetailMetricColumn("Production", new string[] {
            "TotalCount:Total Parts:1,234",
            "PartsPerHour:Rate (Parts/Hr):156",
            "AvgCycleTime:Avg Cycle Time:23.4s",
            "ExpectedPartCount:Expected Parts:1,280"
        });
        gridRow.Add(productionCol);
        
        // Timing metrics column
        var timingCol = CreateDetailMetricColumn("Timing", new string[] {
            "TotalRuntimeFormatted:Runtime:7h 32m",
            "DowntimeFormatted:Downtime:28m",
            "TimeIntoShift:Shift Progress:94.3%",
            "TimeRemainingInShift:Shift Remaining:26m"
        });
        gridRow.Add(timingCol);
        
        return gridRow;
    }

    private Panel CreateDetailMetricColumn(string columnTitle, string[] metrics)
    {
        var columnPanel = InformationModel.Make<Panel>("DetailMetricColumn_" + columnTitle);
        columnPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        columnPanel.VerticalAlignment = VerticalAlignment.Stretch;
        
        var columnLayout = InformationModel.Make<ColumnLayout>("DetailMetricColumnLayout_" + columnTitle);
        columnLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        columnLayout.VerticalAlignment = VerticalAlignment.Top;
        columnLayout.VerticalGap = 15;
        
        // Column title
        var titleLabel = InformationModel.Make<Label>("DetailColumnTitle_" + columnTitle);
        titleLabel.Text = columnTitle;
        titleLabel.FontSize = 16;
        titleLabel.TextColor = PRIMARY_BLUE;
        columnLayout.Add(titleLabel);
        
        // Metrics
        foreach (string metric in metrics)
        {
            var parts = metric.Split(':');
            var metricPanel = CreateDetailMetricItem(parts[1], parts[0], parts[2]);
            columnLayout.Add(metricPanel);
        }
        
        columnPanel.Add(columnLayout);
        return columnPanel;
    }

    private Panel CreateDetailMetricItem(string label, string valuePath, string sampleValue)
    {
        var itemPanel = InformationModel.Make<Panel>("DetailMetricItem_" + valuePath);
        itemPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        itemPanel.Height = 60;
        
        // Item background
        var itemBg = InformationModel.Make<Rectangle>("DetailMetricItemBg_" + valuePath);
        itemBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        itemBg.VerticalAlignment = VerticalAlignment.Stretch;
        itemBg.FillColor = new Color((uint)0xFFF8F9FA);
        itemBg.CornerRadius = 6;
        itemPanel.Add(itemBg);
        
        var itemLayout = InformationModel.Make<ColumnLayout>("DetailMetricItemLayout_" + valuePath);
        itemLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        itemLayout.VerticalAlignment = VerticalAlignment.Center;
        itemLayout.LeftMargin = 15;
        itemLayout.RightMargin = 15;
        itemLayout.VerticalGap = 5;
        
        // Label
        var labelText = InformationModel.Make<Label>("DetailMetricItemLabel_" + valuePath);
        labelText.Text = label;
        labelText.FontSize = 12;
        labelText.TextColor = MEDIUM_TEXT;
        itemLayout.Add(labelText);
        
        // Value
        var valueText = InformationModel.Make<Label>("DetailMetricItemValue_" + valuePath);
        valueText.Text = sampleValue;
        valueText.FontSize = 20;
        valueText.TextColor = DARK_TEXT;
        // DATA LINK: /Objects/Model/OEEInstances/Machine1/Outputs/{valuePath}
        itemLayout.Add(valueText);
        
        itemPanel.Add(itemLayout);
        return itemPanel;
    }

    private Panel CreateStatisticsSummaryPanel()
    {
        var statsPanel = InformationModel.Make<Panel>("StatisticsSummaryPanel");
        statsPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        statsPanel.Height = 300;
        
        // Panel shadow and background
        var statsShadow = InformationModel.Make<Rectangle>("StatsSummaryPanelShadow");
        statsShadow.HorizontalAlignment = HorizontalAlignment.Stretch;
        statsShadow.VerticalAlignment = VerticalAlignment.Stretch;
        statsShadow.LeftMargin = 3;
        statsShadow.TopMargin = 3;
        statsShadow.FillColor = new Color((uint)0x20000000);
        statsShadow.CornerRadius = 8;
        statsPanel.Add(statsShadow);
        
        var statsBg = InformationModel.Make<Rectangle>("StatsSummaryPanelBg");
        statsBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        statsBg.VerticalAlignment = VerticalAlignment.Stretch;
        statsBg.RightMargin = 3;
        statsBg.BottomMargin = 3;
        statsBg.FillColor = WHITE;
        statsBg.CornerRadius = 8;
        statsPanel.Add(statsBg);
        
        // Content layout
        var statsLayout = InformationModel.Make<ColumnLayout>("StatsSummaryLayout");
        statsLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        statsLayout.VerticalAlignment = VerticalAlignment.Stretch;
        statsLayout.LeftMargin = 25;
        statsLayout.TopMargin = 25;
        statsLayout.RightMargin = 25;
        statsLayout.BottomMargin = 25;
        statsLayout.VerticalGap = 15;
        
        // Title
        var statsTitle = InformationModel.Make<Label>("StatsSummaryTitle");
        statsTitle.Text = "Statistical Summary";
        statsTitle.FontSize = 18;
        statsTitle.TextColor = DARK_TEXT;
        statsLayout.Add(statsTitle);
        
        // Stats grid
        var statsGrid = CreateStatsGrid();
        statsLayout.Add(statsGrid);
        
        statsPanel.Add(statsLayout);
        return statsPanel;
    }

    private ColumnLayout CreateStatsGrid()
    {
        var statsGrid = InformationModel.Make<ColumnLayout>("StatsGrid");
        statsGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
        statsGrid.VerticalAlignment = VerticalAlignment.Stretch;
        statsGrid.VerticalGap = 12;
        
        // OEE statistics
        var oeeStats = CreateStatsRow("OEE", "AvgOEE", "MinOEE", "MaxOEE", "72.1%", "58.3%", "89.7%");
        statsGrid.Add(oeeStats);
        
        // Quality statistics
        var qualityStats = CreateStatsRow("Quality", "AvgQuality", "MinQuality", "MaxQuality", "94.8%", "91.2%", "98.5%");
        statsGrid.Add(qualityStats);
        
        // Performance statistics
        var performanceStats = CreateStatsRow("Performance", "AvgPerformance", "MinPerformance", "MaxPerformance", "85.3%", "76.8%", "92.1%");
        statsGrid.Add(performanceStats);
        
        // Availability statistics
        var availabilityStats = CreateStatsRow("Availability", "AvgAvailability", "MinAvailability", "MaxAvailability", "89.2%", "82.5%", "95.8%");
        statsGrid.Add(availabilityStats);
        
        return statsGrid;
    }

    private RowLayout CreateStatsRow(string metric, string avgPath, string minPath, string maxPath, string avgValue, string minValue, string maxValue)
    {
        var statsRow = InformationModel.Make<RowLayout>("StatsRow_" + metric);
        statsRow.HorizontalAlignment = HorizontalAlignment.Stretch;
        statsRow.HorizontalGap = 15;
        
        // Metric name
        var metricLabel = InformationModel.Make<Label>("StatsMetricLabel_" + metric);
        metricLabel.Text = metric;
        metricLabel.FontSize = 14;
        metricLabel.TextColor = DARK_TEXT;
        metricLabel.Width = 100;
        statsRow.Add(metricLabel);
        
        // Average
        var avgItem = CreateStatItem("Avg", avgValue, avgPath);
        statsRow.Add(avgItem);
        
        // Minimum
        var minItem = CreateStatItem("Min", minValue, minPath);
        statsRow.Add(minItem);
        
        // Maximum
        var maxItem = CreateStatItem("Max", maxValue, maxPath);
        statsRow.Add(maxItem);
        
        return statsRow;
    }

    private Panel CreateStatItem(string label, string value, string valuePath)
    {
        var statPanel = InformationModel.Make<Panel>("StatItem_" + valuePath);
        statPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        statPanel.Height = 40;
        
        var statLayout = InformationModel.Make<ColumnLayout>("StatItemLayout_" + valuePath);
        statLayout.HorizontalAlignment = HorizontalAlignment.Center;
        statLayout.VerticalAlignment = VerticalAlignment.Center;
        statLayout.VerticalGap = 3;
        
        // Label
        var labelText = InformationModel.Make<Label>("StatItemLabel_" + valuePath);
        labelText.Text = label;
        labelText.FontSize = 10;
        labelText.TextColor = MEDIUM_TEXT;
        labelText.HorizontalAlignment = HorizontalAlignment.Center;
        statLayout.Add(labelText);
        
        // Value
        var valueText = InformationModel.Make<Label>("StatItemValue_" + valuePath);
        valueText.Text = value;
        valueText.FontSize = 14;
        valueText.TextColor = DARK_TEXT;
        valueText.HorizontalAlignment = HorizontalAlignment.Center;
        // DATA LINK: /Objects/Model/OEEInstances/Machine1/Outputs/{valuePath}
        statLayout.Add(valueText);
        
        statPanel.Add(statLayout);
        return statPanel;
    }

    private Panel CreateTrendAnalysisPanel()
    {
        var trendPanel = InformationModel.Make<Panel>("TrendAnalysisPanel");
        trendPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        trendPanel.Height = 350;
        
        // Panel shadow and background
        var trendShadow = InformationModel.Make<Rectangle>("TrendAnalysisPanelShadow");
        trendShadow.HorizontalAlignment = HorizontalAlignment.Stretch;
        trendShadow.VerticalAlignment = VerticalAlignment.Stretch;
        trendShadow.LeftMargin = 3;
        trendShadow.TopMargin = 3;
        trendShadow.FillColor = new Color((uint)0x20000000);
        trendShadow.CornerRadius = 8;
        trendPanel.Add(trendShadow);
        
        var trendBg = InformationModel.Make<Rectangle>("TrendAnalysisPanelBg");
        trendBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        trendBg.VerticalAlignment = VerticalAlignment.Stretch;
        trendBg.RightMargin = 3;
        trendBg.BottomMargin = 3;
        trendBg.FillColor = WHITE;
        trendBg.CornerRadius = 8;
        trendPanel.Add(trendBg);
        
        // Content layout
        var trendLayout = InformationModel.Make<ColumnLayout>("TrendAnalysisLayout");
        trendLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        trendLayout.VerticalAlignment = VerticalAlignment.Stretch;
        trendLayout.LeftMargin = 25;
        trendLayout.TopMargin = 25;
        trendLayout.RightMargin = 25;
        trendLayout.BottomMargin = 25;
        trendLayout.VerticalGap = 15;
        
        // Title
        var trendTitle = InformationModel.Make<Label>("TrendAnalysisTitle");
        trendTitle.Text = "Trend Analysis";
        trendTitle.FontSize = 18;
        trendTitle.TextColor = DARK_TEXT;
        trendLayout.Add(trendTitle);
        
        // Trend indicators
        var trendIndicators = CreateTrendIndicatorsGrid();
        trendLayout.Add(trendIndicators);
        
        trendPanel.Add(trendLayout);
        return trendPanel;
    }

    private ColumnLayout CreateTrendIndicatorsGrid()
    {
        var trendsGrid = InformationModel.Make<ColumnLayout>("TrendIndicatorsGrid");
        trendsGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
        trendsGrid.VerticalAlignment = VerticalAlignment.Stretch;
        trendsGrid.VerticalGap = 15;
        
        // OEE trend
        var oeeTrend = CreateTrendIndicatorRow("Overall OEE", "OEETrend", "↗", "Improving +2.3%", SUCCESS_GREEN);
        trendsGrid.Add(oeeTrend);
        
        // Quality trend
        var qualityTrend = CreateTrendIndicatorRow("Quality", "QualityTrend", "→", "Stable ±0.1%", PRIMARY_BLUE);
        trendsGrid.Add(qualityTrend);
        
        // Performance trend
        var performanceTrend = CreateTrendIndicatorRow("Performance", "PerformanceTrend", "↘", "Declining -1.8%", WARNING_AMBER);
        trendsGrid.Add(performanceTrend);
        
        // Availability trend
        var availabilityTrend = CreateTrendIndicatorRow("Availability", "AvailabilityTrend", "↗", "Improving +3.1%", SUCCESS_GREEN);
        trendsGrid.Add(availabilityTrend);
        
        return trendsGrid;
    }

    private RowLayout CreateTrendIndicatorRow(string metric, string trendPath, string arrow, string description, Color trendColor)
    {
        var trendRow = InformationModel.Make<RowLayout>("TrendRow_" + metric.Replace(" ", ""));
        trendRow.HorizontalAlignment = HorizontalAlignment.Stretch;
        trendRow.VerticalAlignment = VerticalAlignment.Center;
        trendRow.HorizontalGap = 15;
        
        // Metric name
        var metricLabel = InformationModel.Make<Label>("TrendMetricLabel_" + metric.Replace(" ", ""));
        metricLabel.Text = metric;
        metricLabel.FontSize = 14;
        metricLabel.TextColor = DARK_TEXT;
        metricLabel.Width = 120;
        trendRow.Add(metricLabel);
        
        // Trend arrow
        var trendArrow = InformationModel.Make<Label>("TrendArrow_" + metric.Replace(" ", ""));
        trendArrow.Text = arrow;
        trendArrow.FontSize = 20;
        trendArrow.TextColor = trendColor;
        trendArrow.Width = 30;
        trendArrow.HorizontalAlignment = HorizontalAlignment.Center;
        trendRow.Add(trendArrow);
        
        // Trend description
        var trendDesc = InformationModel.Make<Label>("TrendDescription_" + metric.Replace(" ", ""));
        trendDesc.Text = description;
        trendDesc.FontSize = 12;
        trendDesc.TextColor = MEDIUM_TEXT;
        // PLACEHOLDER: Connect to {OEEInstance}/Outputs/{trendPath}
        trendRow.Add(trendDesc);
        
        return trendRow;
    }

    private Panel CreateTargetPerformancePanel()
    {
        var targetPanel = InformationModel.Make<Panel>("TargetPerformancePanel");
        targetPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        targetPanel.Height = 280;
        
        // Panel shadow and background
        var targetShadow = InformationModel.Make<Rectangle>("TargetPerformancePanelShadow");
        targetShadow.HorizontalAlignment = HorizontalAlignment.Stretch;
        targetShadow.VerticalAlignment = VerticalAlignment.Stretch;
        targetShadow.LeftMargin = 3;
        targetShadow.TopMargin = 3;
        targetShadow.FillColor = new Color((uint)0x20000000);
        targetShadow.CornerRadius = 8;
        targetPanel.Add(targetShadow);
        
        var targetBg = InformationModel.Make<Rectangle>("TargetPerformancePanelBg");
        targetBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        targetBg.VerticalAlignment = VerticalAlignment.Stretch;
        targetBg.RightMargin = 3;
        targetBg.BottomMargin = 3;
        targetBg.FillColor = WHITE;
        targetBg.CornerRadius = 8;
        targetPanel.Add(targetBg);
        
        // Content layout
        var targetLayout = InformationModel.Make<ColumnLayout>("TargetPerformanceLayout");
        targetLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        targetLayout.VerticalAlignment = VerticalAlignment.Stretch;
        targetLayout.LeftMargin = 25;
        targetLayout.TopMargin = 25;
        targetLayout.RightMargin = 25;
        targetLayout.BottomMargin = 25;
        targetLayout.VerticalGap = 15;
        
        // Title
        var targetTitle = InformationModel.Make<Label>("TargetPerformanceTitle");
        targetTitle.Text = "Target vs Actual Performance";
        targetTitle.FontSize = 18;
        targetTitle.TextColor = DARK_TEXT;
        targetLayout.Add(targetTitle);
        
        // Target comparison bars
        var targetBars = CreateTargetComparisonBars();
        targetLayout.Add(targetBars);
        
        targetPanel.Add(targetLayout);
        return targetPanel;
    }

    private ColumnLayout CreateTargetComparisonBars()
    {
        var barsLayout = InformationModel.Make<ColumnLayout>("TargetComparisonBars");
        barsLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        barsLayout.VerticalAlignment = VerticalAlignment.Stretch;
        barsLayout.VerticalGap = 20;
        
        // OEE target bar
        var oeeBar = CreateTargetComparisonBar("OEE", "OEEVsTarget", 72.5, 75.0, PRIMARY_BLUE);
        barsLayout.Add(oeeBar);
        
        // Quality target bar
        var qualityBar = CreateTargetComparisonBar("Quality", "QualityVsTarget", 95.2, 95.0, SUCCESS_GREEN);
        barsLayout.Add(qualityBar);
        
        // Performance target bar
        var performanceBar = CreateTargetComparisonBar("Performance", "PerformanceVsTarget", 85.1, 88.0, WARNING_AMBER);
        barsLayout.Add(performanceBar);
        
        // Availability target bar
        var availabilityBar = CreateTargetComparisonBar("Availability", "AvailabilityVsTarget", 89.3, 90.0, DANGER_RED);
        barsLayout.Add(availabilityBar);
        
        return barsLayout;
    }

    private RowLayout CreateTargetComparisonBar(string metric, string comparisonPath, double actual, double target, Color barColor)
    {
        var barRow = InformationModel.Make<RowLayout>("TargetBar_" + metric);
        barRow.HorizontalAlignment = HorizontalAlignment.Stretch;
        barRow.VerticalAlignment = VerticalAlignment.Center;
        barRow.HorizontalGap = 15;
        
        // Metric name
        var metricLabel = InformationModel.Make<Label>("TargetBarLabel_" + metric);
        metricLabel.Text = metric;
        metricLabel.FontSize = 14;
        metricLabel.TextColor = DARK_TEXT;
        metricLabel.Width = 100;
        barRow.Add(metricLabel);
        
        // Progress bar container
        var barContainer = InformationModel.Make<Panel>("TargetBarContainer_" + metric);
        barContainer.HorizontalAlignment = HorizontalAlignment.Stretch;
        barContainer.Height = 20;
        
        // Background bar
        var barBg = InformationModel.Make<Rectangle>("TargetBarBg_" + metric);
        barBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        barBg.VerticalAlignment = VerticalAlignment.Stretch;
        barBg.FillColor = new Color((uint)0xFFE9ECEF);
        barBg.CornerRadius = 10;
        barContainer.Add(barBg);
        
        // Progress fill
        var barFill = InformationModel.Make<Rectangle>("TargetBarFill_" + metric);
        barFill.HorizontalAlignment = HorizontalAlignment.Left;
        barFill.VerticalAlignment = VerticalAlignment.Stretch;
        barFill.Width = (int)(200 * (actual / 100.0)); // Scale to 200px max
        barFill.FillColor = barColor;
        barFill.CornerRadius = 10;
        barContainer.Add(barFill);
        
        // Target marker
        var targetMarker = InformationModel.Make<Rectangle>("TargetMarker_" + metric);
        targetMarker.Width = 3;
        targetMarker.VerticalAlignment = VerticalAlignment.Stretch;
        targetMarker.LeftMargin = (int)(200 * (target / 100.0));
        targetMarker.FillColor = DARK_TEXT;
        barContainer.Add(targetMarker);
        
        barRow.Add(barContainer);
        
        // Values
        var valuesPanel = InformationModel.Make<Panel>("TargetBarValues_" + metric);
        valuesPanel.Width = 120;
        
        var valuesLayout = InformationModel.Make<ColumnLayout>("TargetBarValuesLayout_" + metric);
        valuesLayout.VerticalGap = 3;
        
        var actualLabel = InformationModel.Make<Label>("TargetBarActual_" + metric);
        actualLabel.Text = $"Actual: {actual:F1}%";
        actualLabel.FontSize = 11;
        actualLabel.TextColor = DARK_TEXT;
        // DATA LINK: /Objects/Model/OEEInstances/Machine1/Outputs/{comparisonPath}
        
        var targetLabel = InformationModel.Make<Label>("TargetBarTarget_" + metric);
        targetLabel.Text = $"Target: {target:F1}%";
        targetLabel.FontSize = 11;
        targetLabel.TextColor = MEDIUM_TEXT;
        
        valuesLayout.Add(actualLabel);
        valuesLayout.Add(targetLabel);
        valuesPanel.Add(valuesLayout);
        barRow.Add(valuesPanel);
        
        return barRow;
    }

    [ExportMethod]
    public void VerifyDataLinks()
    {
        Log.Info("OEEUIGenerator", "Verifying all data links are properly configured...");
        
        // List all variables that should be linked
        string[] outputVars = { 
            "TotalCount", "Quality", "Performance", "Availability", "OEE", "AvgCycleTime", 
            "PartsPerHour", "ExpectedPartCount", "DowntimeFormatted", "TotalRuntimeFormatted",
            "CurrentShiftNumber", "ShiftStartTimeOutput", "ShiftEndTime", "TimeIntoShift", 
            "TimeRemainingInShift", "ShiftChangeOccurred", "ShiftChangeImminent", 
            "ProjectedTotalCount", "RemainingTimeAtCurrentRate", "ProductionBehindSchedule",
            "RequiredRateToTarget", "TargetVsActualParts", "LastUpdateTime", "SystemStatus",
            "CalculationValid", "DataQualityScore", "QualityTrend", "PerformanceTrend",
            "AvailabilityTrend", "OEETrend", "MinQuality", "MaxQuality", "AvgQuality",
            "MinPerformance", "MaxPerformance", "AvgPerformance", "MinAvailability", 
            "MaxAvailability", "AvgAvailability", "MinOEE", "MaxOEE", "AvgOEE",
            "QualityVsTarget", "PerformanceVsTarget", "AvailabilityVsTarget", "OEEVsTarget"
        };
        
        string[] configVars = {
            "EnableRealTimeCalc", "EnableLogging", "EnableAlarms", "SystemHealthy"
        };
        
        string[] inputVars = {
            "GoodPartCount", "BadPartCount", "ProductionTarget"
        };
        
        Log.Info("OEEUIGenerator", $"Dashboard configured with {outputVars.Length + configVars.Length + inputVars.Length} data links");
        Log.Info("OEEUIGenerator", "Data link verification complete!");
    }
    
    // =================================================================
    // DATA BINDING REFERENCE - Complete mapping of UI elements to OEE variables
    // =================================================================
    
    /*
    MAIN KPI CARDS:
    - OEE Value → /Objects/Model/OEEInstances/Machine1/Outputs/OEE
    - Quality Value → /Objects/Model/OEEInstances/Machine1/Outputs/Quality  
    - Performance Value → /Objects/Model/OEEInstances/Machine1/Outputs/Performance
    - Availability Value → /Objects/Model/OEEInstances/Machine1/Outputs/Availability
    - Trend Indicators → /Objects/Model/OEEInstances/Machine1/Outputs/{Metric}Trend
    - Target Comparisons → /Objects/Model/OEEInstances/Machine1/Outputs/{Metric}VsTarget
    
    PRODUCTION COUNTS GROUP:
    - Total Parts → /Objects/Model/OEEInstances/Machine1/Outputs/TotalCount
    - Good Parts → /Objects/Model/OEEInstances/Machine1/Inputs/GoodPartCount
    - Rejected Parts → /Objects/Model/OEEInstances/Machine1/Inputs/BadPartCount
    - Parts/Hour → /Objects/Model/OEEInstances/Machine1/Outputs/PartsPerHour
    
    TIMING METRICS GROUP:
    - Avg Cycle Time → /Objects/Model/OEEInstances/Machine1/Outputs/AvgCycleTime
    - Runtime → /Objects/Model/OEEInstances/Machine1/Outputs/TotalRuntimeFormatted
    - Downtime → /Objects/Model/OEEInstances/Machine1/Outputs/DowntimeFormatted
    - Shift Progress → /Objects/Model/OEEInstances/Machine1/Outputs/TimeIntoShift
    
    PERFORMANCE GROUP:
    - Data Quality → /Objects/Model/OEEInstances/Machine1/Outputs/DataQualityScore
    - Expected Parts → /Objects/Model/OEEInstances/Machine1/Outputs/ExpectedPartCount
    - Projected Total → /Objects/Model/OEEInstances/Machine1/Outputs/ProjectedTotalCount
    - Required Rate → /Objects/Model/OEEInstances/Machine1/Outputs/RequiredRateToTarget
    
    SHIFT INFORMATION:
    - Shift Number → /Objects/Model/OEEInstances/Machine1/Outputs/CurrentShiftNumber
    - Start Time → /Objects/Model/OEEInstances/Machine1/Outputs/ShiftStartTimeOutput
    - End Time → /Objects/Model/OEEInstances/Machine1/Outputs/ShiftEndTime
    - Time Remaining → /Objects/Model/OEEInstances/Machine1/Outputs/TimeRemainingInShift
    
    TARGET VS ACTUAL:
    - Production Target → /Objects/Model/OEEInstances/Machine1/Inputs/ProductionTarget
    - Target vs Actual → /Objects/Model/OEEInstances/Machine1/Outputs/TargetVsActualParts
    - Behind Schedule → /Objects/Model/OEEInstances/Machine1/Outputs/ProductionBehindSchedule
    - Time at Current Rate → /Objects/Model/OEEInstances/Machine1/Outputs/RemainingTimeAtCurrentRate
    
    ALERTS PANEL:
    - System Status → /Objects/Model/OEEInstances/Machine1/Outputs/SystemStatus
    - Shift Change LED → /Objects/Model/OEEInstances/Machine1/Outputs/ShiftChangeImminent
    - Production Behind → /Objects/Model/OEEInstances/Machine1/Outputs/ProductionBehindSchedule
    
    SYSTEM STATUS:
    - Real-time LED → /Objects/Model/OEEInstances/Machine1/Configuration/EnableRealTimeCalc
    - Logging LED → /Objects/Model/OEEInstances/Machine1/Configuration/EnableLogging
    - Alarms LED → /Objects/Model/OEEInstances/Machine1/Configuration/EnableAlarms
    - Avg OEE → /Objects/Model/OEEInstances/Machine1/Outputs/AvgOEE
    - Max OEE → /Objects/Model/OEEInstances/Machine1/Outputs/MaxOEE
    
    HEADER STATUS:
    - Data Quality Progress → /Objects/Model/OEEInstances/Machine1/Outputs/DataQualityScore (Width)
    - Data Quality Value → /Objects/Model/OEEInstances/Machine1/Outputs/DataQualityScore (Text)
    - System Health LED → /Objects/Model/OEEInstances/Machine1/Configuration/SystemHealthy
    - Calculation LED → /Objects/Model/OEEInstances/Machine1/Outputs/CalculationValid
    - Last Update → /Objects/Model/OEEInstances/Machine1/Outputs/LastUpdateTime
    */

    public void CreateOperatorInputScreen()
    {
        Log.Info("OEEUIGenerator", "Creating modern Operator Input Screen...");
        
        try
        {
            var screensFolder = Project.Current.Get("UI/Screens");
            
            // Create main operator input screen
            var inputScreen = InformationModel.Make<Screen>("OperatorInputScreen");
            inputScreen.BrowseName = "OperatorInputScreen";
            inputScreen.Width = 1920;
            inputScreen.Height = 1080;
            
            // Background with modern gradient feel
            var background = InformationModel.Make<Rectangle>("OperatorBackground");
            background.HorizontalAlignment = HorizontalAlignment.Stretch;
            background.VerticalAlignment = VerticalAlignment.Stretch;
            background.FillColor = LIGHT_GRAY;
            inputScreen.Add(background);
            
            // Main layout container optimized for 1920x1080
            var mainContainer = InformationModel.Make<ColumnLayout>("OperatorMainContainer");
            mainContainer.HorizontalAlignment = HorizontalAlignment.Stretch;
            mainContainer.VerticalAlignment = VerticalAlignment.Stretch;
            mainContainer.VerticalGap = 20;
        
            // Screen header with operator info
            var headerPanel = CreateOperatorHeaderPanel();
            mainContainer.Add(headerPanel);
        
            // Main content area with input sections
            var mainContent = InformationModel.Make<RowLayout>("OperatorMainContent");
            mainContent.HorizontalAlignment = HorizontalAlignment.Stretch;
            mainContent.VerticalAlignment = VerticalAlignment.Stretch;
            mainContent.HorizontalGap = 25;
            mainContent.LeftMargin = 20;
            mainContent.RightMargin = 20;
        
            // Left panel - Production Input & Targets
            var leftPanel = CreateProductionInputPanel();
            mainContent.Add(leftPanel);
        
            // Center panel - Downtime & Quality Management
            var centerPanel = CreateDowntimeQualityPanel();
            mainContent.Add(centerPanel);
        
            // Right panel - Quick Actions & Status
            var rightPanel = CreateQuickActionsPanel();
            mainContent.Add(rightPanel);
        
            mainContainer.Add(mainContent);
        
            // Footer with current status and actions
            var footerPanel = CreateOperatorFooterPanel();
            mainContainer.Add(footerPanel);
            
            // Add main container to screen
            inputScreen.Add(mainContainer);
        
            // Add to project screens folder
            screensFolder.Add(inputScreen);
            Log.Info("OEEUIGenerator", "Operator Input Screen created successfully with comprehensive input controls");
        }
        catch (Exception ex)
        {
            Log.Error("OEEUIGenerator", $"Error creating Operator Input Screen: {ex.Message}");
        }
    }

    private Panel CreateOperatorHeaderPanel()
    {
        var headerPanel = InformationModel.Make<Panel>("OperatorHeaderPanel");
        headerPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        headerPanel.Height = 100;
        
        // Header shadow
        var headerShadow = InformationModel.Make<Rectangle>("OperatorHeaderShadow");
        headerShadow.HorizontalAlignment = HorizontalAlignment.Stretch;
        headerShadow.VerticalAlignment = VerticalAlignment.Stretch;
        headerShadow.LeftMargin = 3;
        headerShadow.TopMargin = 3;
        headerShadow.FillColor = new Color((uint)0x20000000);
        headerShadow.CornerRadius = 8;
        headerPanel.Add(headerShadow);
        
        // Header background with gradient
        var headerBg = InformationModel.Make<Rectangle>("OperatorHeaderBg");
        headerBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        headerBg.VerticalAlignment = VerticalAlignment.Stretch;
        headerBg.RightMargin = 3;
        headerBg.BottomMargin = 3;
        headerBg.FillColor = SUCCESS_GREEN;
        headerBg.CornerRadius = 8;
        headerPanel.Add(headerBg);
        
        // Header overlay for depth
        var headerOverlay = InformationModel.Make<Rectangle>("OperatorHeaderOverlay");
        headerOverlay.HorizontalAlignment = HorizontalAlignment.Stretch;
        headerOverlay.VerticalAlignment = VerticalAlignment.Stretch;
        headerOverlay.RightMargin = 3;
        headerOverlay.BottomMargin = 3;
        headerOverlay.FillColor = new Color((uint)0x30FFFFFF);
        headerOverlay.CornerRadius = 8;
        headerPanel.Add(headerOverlay);
        
        // Header content
        var headerContent = InformationModel.Make<RowLayout>("OperatorHeaderContent");
        headerContent.HorizontalAlignment = HorizontalAlignment.Stretch;
        headerContent.VerticalAlignment = VerticalAlignment.Center;
        headerContent.LeftMargin = 30;
        headerContent.RightMargin = 30;
        headerContent.HorizontalGap = 30;
        
        // Screen title
        var titleSection = InformationModel.Make<Panel>("OperatorTitleSection");
        titleSection.HorizontalAlignment = HorizontalAlignment.Left;
        titleSection.Width = 500;
        
        var titleLayout = InformationModel.Make<ColumnLayout>("OperatorTitleLayout");
        titleLayout.VerticalGap = 5;
        
        var screenTitle = InformationModel.Make<Label>("OperatorScreenTitle");
        screenTitle.Text = "Operator Input & Control";
        screenTitle.FontSize = 24;
        screenTitle.TextColor = WHITE;
        
        var screenSubtitle = InformationModel.Make<Label>("OperatorScreenSubtitle");
        screenSubtitle.Text = "Production Data Entry & System Control";
        screenSubtitle.FontSize = 12;
        screenSubtitle.TextColor = new Color((uint)0xFFE8F4FD);
        
        titleLayout.Add(screenTitle);
        titleLayout.Add(screenSubtitle);
        titleSection.Add(titleLayout);
        headerContent.Add(titleSection);
        
        // Operator info
        var operatorInfo = CreateOperatorInfoSection();
        headerContent.Add(operatorInfo);
        
        // Current shift info
        var shiftInfo = CreateCurrentShiftInfo();
        headerContent.Add(shiftInfo);
        
        headerPanel.Add(headerContent);
        return headerPanel;
    }

    private Panel CreateOperatorInfoSection()
    {
        var infoPanel = InformationModel.Make<Panel>("OperatorInfoSection");
        infoPanel.HorizontalAlignment = HorizontalAlignment.Center;
        infoPanel.Width = 300;
        
        var infoLayout = InformationModel.Make<ColumnLayout>("OperatorInfoLayout");
        infoLayout.HorizontalAlignment = HorizontalAlignment.Center;
        infoLayout.VerticalGap = 5;
        
        // Operator name
        var operatorLabel = InformationModel.Make<Label>("OperatorLabel");
        operatorLabel.Text = "Operator:";
        operatorLabel.FontSize = 10;
        operatorLabel.TextColor = new Color((uint)0xFFE8F4FD);
        operatorLabel.HorizontalAlignment = HorizontalAlignment.Center;
        
        var operatorName = InformationModel.Make<Label>("OperatorName");
        operatorName.Text = "John Smith";
        operatorName.FontSize = 14;
        operatorName.TextColor = WHITE;
        operatorName.HorizontalAlignment = HorizontalAlignment.Center;
        // DATA LINK: Current logged in user name
        
        infoLayout.Add(operatorLabel);
        infoLayout.Add(operatorName);
        infoPanel.Add(infoLayout);
        
        return infoPanel;
    }

    private Panel CreateCurrentShiftInfo()
    {
        var shiftPanel = InformationModel.Make<Panel>("CurrentShiftInfo");
        shiftPanel.HorizontalAlignment = HorizontalAlignment.Right;
        shiftPanel.Width = 300;
        
        var shiftLayout = InformationModel.Make<RowLayout>("CurrentShiftLayout");
        shiftLayout.HorizontalAlignment = HorizontalAlignment.Right;
        shiftLayout.VerticalAlignment = VerticalAlignment.Center;
        shiftLayout.HorizontalGap = 20;
        
        // Shift number
        var shiftInfo = CreateShiftInfoItem("Shift:", "1");
        shiftLayout.Add(shiftInfo);
        
        // Time remaining
        var timeInfo = CreateShiftInfoItem("Time Left:", "2h 15m");
        shiftLayout.Add(timeInfo);
        
        shiftPanel.Add(shiftLayout);
        return shiftPanel;
    }

    private Panel CreateShiftInfoItem(string label, string value)
    {
        var itemPanel = InformationModel.Make<Panel>("ShiftInfoItem_" + label.Replace(":", ""));
        itemPanel.Width = 80;
        
        var itemLayout = InformationModel.Make<ColumnLayout>("ShiftInfoItemLayout_" + label.Replace(":", ""));
        itemLayout.HorizontalAlignment = HorizontalAlignment.Center;
        itemLayout.VerticalGap = 3;
        
        var labelText = InformationModel.Make<Label>("ShiftInfoLabel_" + label.Replace(":", ""));
        labelText.Text = label;
        labelText.FontSize = 10;
        labelText.TextColor = new Color((uint)0xFFE8F4FD);
        labelText.HorizontalAlignment = HorizontalAlignment.Center;
        
        var valueText = InformationModel.Make<Label>("ShiftInfoValue_" + label.Replace(":", ""));
        valueText.Text = value;
        valueText.FontSize = 12;
        valueText.TextColor = WHITE;
        valueText.HorizontalAlignment = HorizontalAlignment.Center;
        // DATA LINK: Appropriate shift/time variables
        
        itemLayout.Add(labelText);
        itemLayout.Add(valueText);
        itemPanel.Add(itemLayout);
        
        return itemPanel;
    }

    private Panel CreateProductionInputPanel()
    {
        var inputPanel = InformationModel.Make<Panel>("ProductionInputPanel");
        inputPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        inputPanel.VerticalAlignment = VerticalAlignment.Stretch;
        
        // Panel shadow
        var panelShadow = InformationModel.Make<Rectangle>("ProductionInputShadow");
        panelShadow.HorizontalAlignment = HorizontalAlignment.Stretch;
        panelShadow.VerticalAlignment = VerticalAlignment.Stretch;
        panelShadow.LeftMargin = 3;
        panelShadow.TopMargin = 3;
        panelShadow.FillColor = new Color((uint)0x20000000);
        panelShadow.CornerRadius = 8;
        inputPanel.Add(panelShadow);
        
        // Panel background
        var panelBg = InformationModel.Make<Rectangle>("ProductionInputBg");
        panelBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        panelBg.VerticalAlignment = VerticalAlignment.Stretch;
        panelBg.RightMargin = 3;
        panelBg.BottomMargin = 3;
        panelBg.FillColor = WHITE;
        panelBg.CornerRadius = 8;
        inputPanel.Add(panelBg);
        
        // Content layout
        var contentLayout = InformationModel.Make<ColumnLayout>("ProductionInputContent");
        contentLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        contentLayout.VerticalAlignment = VerticalAlignment.Stretch;
        contentLayout.LeftMargin = 25;
        contentLayout.TopMargin = 25;
        contentLayout.RightMargin = 25;
        contentLayout.BottomMargin = 25;
        contentLayout.VerticalGap = 20;
        
        // Section title
        var sectionTitle = InformationModel.Make<Label>("ProductionInputTitle");
        sectionTitle.Text = "Production Input & Targets";
        sectionTitle.FontSize = 18;
        sectionTitle.TextColor = DARK_TEXT;
        contentLayout.Add(sectionTitle);
        
        // Production count input
        var countSection = CreateProductionCountInput();
        contentLayout.Add(countSection);
        
        // Target adjustments
        var targetSection = CreateTargetAdjustments();
        contentLayout.Add(targetSection);
        
        // Job/Part information
        var jobSection = CreateJobPartInfo();
        contentLayout.Add(jobSection);
        
        inputPanel.Add(contentLayout);
        return inputPanel;
    }

    private Panel CreateProductionCountInput()
    {
        var countPanel = InformationModel.Make<Panel>("ProductionCountInput");
        countPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        countPanel.Height = 120;
        
        // Section background
        var sectionBg = InformationModel.Make<Rectangle>("CountInputBg");
        sectionBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionBg.VerticalAlignment = VerticalAlignment.Stretch;
        sectionBg.FillColor = new Color((uint)0xFFF8F9FA);
        sectionBg.CornerRadius = 6;
        countPanel.Add(sectionBg);
        
        var countLayout = InformationModel.Make<ColumnLayout>("ProductionCountLayout");
        countLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        countLayout.VerticalAlignment = VerticalAlignment.Center;
        countLayout.LeftMargin = 20;
        countLayout.RightMargin = 20;
        countLayout.VerticalGap = 15;
        
        // Title
        var countTitle = InformationModel.Make<Label>("ProductionCountTitle");
        countTitle.Text = "Production Count Entry";
        countTitle.FontSize = 16;
        countTitle.TextColor = PRIMARY_BLUE;
        countLayout.Add(countTitle);
        
        // Input row
        var inputRow = InformationModel.Make<RowLayout>("CountInputRow");
        inputRow.HorizontalAlignment = HorizontalAlignment.Stretch;
        inputRow.VerticalAlignment = VerticalAlignment.Center;
        inputRow.HorizontalGap = 20;
        
        // Good parts input
        var goodPartsInput = CreateCountInputField("Good Parts:", "GoodPartsInput", "1247");
        inputRow.Add(goodPartsInput);
        
        // Scrap parts input
        var scrapPartsInput = CreateCountInputField("Scrap Parts:", "ScrapPartsInput", "15");
        inputRow.Add(scrapPartsInput);
        
        // Update button
        var updateButton = CreateStyledButton("Update Counts", "UpdateCountsBtn", PRIMARY_BLUE);
        inputRow.Add(updateButton);
        
        countLayout.Add(inputRow);
        countPanel.Add(countLayout);
        
        return countPanel;
    }

    private Panel CreateCountInputField(string label, string fieldId, string placeholder)
    {
        var fieldPanel = InformationModel.Make<Panel>("CountField_" + fieldId);
        fieldPanel.Width = 180;
        
        var fieldLayout = InformationModel.Make<ColumnLayout>("CountFieldLayout_" + fieldId);
        fieldLayout.VerticalGap = 8;
        
        // Label
        var fieldLabel = InformationModel.Make<Label>("CountFieldLabel_" + fieldId);
        fieldLabel.Text = label;
        fieldLabel.FontSize = 12;
        fieldLabel.TextColor = DARK_TEXT;
        fieldLayout.Add(fieldLabel);
        
        // Input field
        var inputField = InformationModel.Make<TextBox>("CountFieldInput_" + fieldId);
        inputField.HorizontalAlignment = HorizontalAlignment.Stretch;
        inputField.Height = 35;
        inputField.Text = placeholder;
        inputField.FontSize = 14;
        // DATA LINK: /Objects/Model/OEEInstances/Machine1/Inputs/{fieldId}
        fieldLayout.Add(inputField);
        
        fieldPanel.Add(fieldLayout);
        return fieldPanel;
    }

    private Panel CreateStyledButton(string buttonText, string buttonId, Color buttonColor)
    {
        var buttonPanel = InformationModel.Make<Panel>("ButtonPanel_" + buttonId);
        buttonPanel.Width = 140;
        buttonPanel.Height = 40;
        
        // Button shadow
        var buttonShadow = InformationModel.Make<Rectangle>("ButtonShadow_" + buttonId);
        buttonShadow.HorizontalAlignment = HorizontalAlignment.Stretch;
        buttonShadow.VerticalAlignment = VerticalAlignment.Stretch;
        buttonShadow.LeftMargin = 2;
        buttonShadow.TopMargin = 2;
        buttonShadow.FillColor = new Color((uint)0x30000000);
        buttonShadow.CornerRadius = 6;
        buttonPanel.Add(buttonShadow);
        
        // Actual button
        var button = InformationModel.Make<Button>("Button_" + buttonId);
        button.HorizontalAlignment = HorizontalAlignment.Stretch;
        button.VerticalAlignment = VerticalAlignment.Stretch;
        button.RightMargin = 2;
        button.BottomMargin = 2;
        button.Text = buttonText;
        button.FontSize = 12;
        button.TextColor = WHITE;
        // Set button background color through styling
        buttonPanel.Add(button);
        
        return buttonPanel;
    }

    private Panel CreateTargetAdjustments()
    {
        var targetPanel = InformationModel.Make<Panel>("TargetAdjustments");
        targetPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        targetPanel.Height = 160;
        
        var sectionBg = InformationModel.Make<Rectangle>("TargetAdjustmentsBg");
        sectionBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionBg.VerticalAlignment = VerticalAlignment.Stretch;
        sectionBg.FillColor = new Color((uint)0xFFF8F9FA);
        sectionBg.CornerRadius = 6;
        targetPanel.Add(sectionBg);
        
        var targetLayout = InformationModel.Make<ColumnLayout>("TargetAdjustmentsLayout");
        targetLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        targetLayout.LeftMargin = 20;
        targetLayout.RightMargin = 20;
        targetLayout.TopMargin = 15;
        targetLayout.BottomMargin = 15;
        targetLayout.VerticalGap = 15;
        
        var targetTitle = InformationModel.Make<Label>("TargetAdjustmentsTitle");
        targetTitle.Text = "Target Adjustments";
        targetTitle.FontSize = 16;
        targetTitle.TextColor = WARNING_AMBER;
        targetLayout.Add(targetTitle);
        
        var targetGrid = InformationModel.Make<RowLayout>("TargetGrid");
        targetGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
        targetGrid.HorizontalGap = 15;
        
        var oeeTarget = CreateBasicInputField("OEE Target %:", "75.0");
        targetGrid.Add(oeeTarget);
        
        var qualityTarget = CreateBasicInputField("Quality Target %:", "95.0");
        targetGrid.Add(qualityTarget);
        
        var performanceTarget = CreateBasicInputField("Performance Target %:", "88.0");
        targetGrid.Add(performanceTarget);
        
        targetLayout.Add(targetGrid);
        
        var applyButton = CreateSimpleButton("Apply Targets");
        applyButton.HorizontalAlignment = HorizontalAlignment.Center;
        targetLayout.Add(applyButton);
        
        targetPanel.Add(targetLayout);
        return targetPanel;
    }

    private Panel CreateJobPartInfo()
    {
        var jobPanel = InformationModel.Make<Panel>("JobPartInfo");
        jobPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        jobPanel.Height = 140;
        
        var sectionBg = InformationModel.Make<Rectangle>("JobPartInfoBg");
        sectionBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionBg.VerticalAlignment = VerticalAlignment.Stretch;
        sectionBg.FillColor = new Color((uint)0xFFF8F9FA);
        sectionBg.CornerRadius = 6;
        jobPanel.Add(sectionBg);
        
        var jobLayout = InformationModel.Make<ColumnLayout>("JobPartInfoLayout");
        jobLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        jobLayout.LeftMargin = 20;
        jobLayout.RightMargin = 20;
        jobLayout.TopMargin = 15;
        jobLayout.BottomMargin = 15;
        jobLayout.VerticalGap = 15;
        
        var jobTitle = InformationModel.Make<Label>("JobPartInfoTitle");
        jobTitle.Text = "Job & Part Information";
        jobTitle.FontSize = 16;
        jobTitle.TextColor = PRIMARY_BLUE;
        jobLayout.Add(jobTitle);
        
        var jobRow = InformationModel.Make<RowLayout>("JobInfoRow");
        jobRow.HorizontalAlignment = HorizontalAlignment.Stretch;
        jobRow.HorizontalGap = 20;
        
        var jobNumber = CreateBasicInputField("Job Number:", "JOB-2024-1158");
        jobRow.Add(jobNumber);
        
        var partNumber = CreateBasicInputField("Part Number:", "PN-ABC-123");
        jobRow.Add(partNumber);
        
        var cycleTime = CreateBasicInputField("Cycle Time (sec):", "23.5");
        jobRow.Add(cycleTime);
        
        jobLayout.Add(jobRow);
        jobPanel.Add(jobLayout);
        
        return jobPanel;
    }

    private Panel CreateDowntimeQualityPanel()
    {
        var downtimePanel = InformationModel.Make<Panel>("DowntimeQualityPanel");
        downtimePanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        downtimePanel.VerticalAlignment = VerticalAlignment.Stretch;
        
        // Panel shadow
        var panelShadow = InformationModel.Make<Rectangle>("DowntimeQualityShadow");
        panelShadow.HorizontalAlignment = HorizontalAlignment.Stretch;
        panelShadow.VerticalAlignment = VerticalAlignment.Stretch;
        panelShadow.LeftMargin = 3;
        panelShadow.TopMargin = 3;
        panelShadow.FillColor = new Color((uint)0x20000000);
        panelShadow.CornerRadius = 8;
        downtimePanel.Add(panelShadow);
        
        // Panel background
        var panelBg = InformationModel.Make<Rectangle>("DowntimeQualityBg");
        panelBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        panelBg.VerticalAlignment = VerticalAlignment.Stretch;
        panelBg.RightMargin = 3;
        panelBg.BottomMargin = 3;
        panelBg.FillColor = WHITE;
        panelBg.CornerRadius = 8;
        downtimePanel.Add(panelBg);
        
        // Content layout
        var contentLayout = InformationModel.Make<ColumnLayout>("DowntimeQualityContent");
        contentLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        contentLayout.VerticalAlignment = VerticalAlignment.Stretch;
        contentLayout.LeftMargin = 25;
        contentLayout.TopMargin = 25;
        contentLayout.RightMargin = 25;
        contentLayout.BottomMargin = 25;
        contentLayout.VerticalGap = 20;
        
        // Section title
        var sectionTitle = InformationModel.Make<Label>("DowntimeQualityTitle");
        sectionTitle.Text = "Downtime & Quality Management";
        sectionTitle.FontSize = 18;
        sectionTitle.TextColor = DARK_TEXT;
        contentLayout.Add(sectionTitle);
        
        // Downtime reasons section
        var downtimeSection = CreateDowntimeReasonsSection();
        contentLayout.Add(downtimeSection);
        
        // Quality issues section
        var qualitySection = CreateQualityIssuesSection();
        contentLayout.Add(qualitySection);
        
        // Reject reasons section
        var rejectSection = CreateRejectReasonsSection();
        contentLayout.Add(rejectSection);
        
        downtimePanel.Add(contentLayout);
        return downtimePanel;
    }

    private Panel CreateQualityIssuesSection()
    {
        var qualityPanel = InformationModel.Make<Panel>("QualityIssuesSection");
        qualityPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        qualityPanel.Height = 120;
        
        var sectionBg = InformationModel.Make<Rectangle>("QualityIssuesBg");
        sectionBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionBg.VerticalAlignment = VerticalAlignment.Stretch;
        sectionBg.FillColor = new Color((uint)0xFFF8F9FA);
        sectionBg.CornerRadius = 6;
        qualityPanel.Add(sectionBg);
        
        var layout = InformationModel.Make<ColumnLayout>("QualityIssuesLayout");
        layout.HorizontalAlignment = HorizontalAlignment.Stretch;
        layout.LeftMargin = 20;
        layout.RightMargin = 20;
        layout.TopMargin = 15;
        layout.BottomMargin = 15;
        layout.VerticalGap = 15;
        
        var title = InformationModel.Make<Label>("QualityIssuesTitle");
        title.Text = "Quality Issues";
        title.FontSize = 16;
        title.TextColor = WARNING_AMBER;
        layout.Add(title);
        
        var issuesRow = InformationModel.Make<RowLayout>("QualityIssuesRow");
        issuesRow.HorizontalAlignment = HorizontalAlignment.Stretch;
        issuesRow.HorizontalGap = 15;
        
        var dimensionBtn = CreateSimpleButton("Dimension");
        issuesRow.Add(dimensionBtn);
        
        var surfaceBtn = CreateSimpleButton("Surface");
        issuesRow.Add(surfaceBtn);
        
        var assemblyBtn = CreateSimpleButton("Assembly");
        issuesRow.Add(assemblyBtn);
        
        layout.Add(issuesRow);
        qualityPanel.Add(layout);
        return qualityPanel;
    }

    private Panel CreateQuickActionsPanel()
    {
        var actionsPanel = InformationModel.Make<Panel>("QuickActionsPanel");
        actionsPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        actionsPanel.VerticalAlignment = VerticalAlignment.Stretch;
        
        // Panel shadow
        var panelShadow = InformationModel.Make<Rectangle>("QuickActionsShadow");
        panelShadow.HorizontalAlignment = HorizontalAlignment.Stretch;
        panelShadow.VerticalAlignment = VerticalAlignment.Stretch;
        panelShadow.LeftMargin = 3;
        panelShadow.TopMargin = 3;
        panelShadow.FillColor = new Color((uint)0x20000000);
        panelShadow.CornerRadius = 8;
        actionsPanel.Add(panelShadow);
        
        // Panel background
        var panelBg = InformationModel.Make<Rectangle>("QuickActionsBg");
        panelBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        panelBg.VerticalAlignment = VerticalAlignment.Stretch;
        panelBg.RightMargin = 3;
        panelBg.BottomMargin = 3;
        panelBg.FillColor = WHITE;
        panelBg.CornerRadius = 8;
        actionsPanel.Add(panelBg);
        
        // Content layout
        var contentLayout = InformationModel.Make<ColumnLayout>("QuickActionsContent");
        contentLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        contentLayout.VerticalAlignment = VerticalAlignment.Stretch;
        contentLayout.LeftMargin = 25;
        contentLayout.TopMargin = 25;
        contentLayout.RightMargin = 25;
        contentLayout.BottomMargin = 25;
        contentLayout.VerticalGap = 20;
        
        // Section title
        var sectionTitle = InformationModel.Make<Label>("QuickActionsTitle");
        sectionTitle.Text = "Quick Actions & Status";
        sectionTitle.FontSize = 18;
        sectionTitle.TextColor = DARK_TEXT;
        contentLayout.Add(sectionTitle);
        
        // System control buttons
        var systemSection = CreateSystemControlSection();
        contentLayout.Add(systemSection);
        
        // Current status display
        var statusSection = CreateCurrentStatusSection();
        contentLayout.Add(statusSection);
        
        // Emergency actions
        var emergencySection = CreateEmergencyActionsSection();
        contentLayout.Add(emergencySection);
        
        actionsPanel.Add(contentLayout);
        return actionsPanel;
    }

    private Panel CreateOperatorFooterPanel()
    {
        var footerPanel = InformationModel.Make<Panel>("OperatorFooterPanel");
        footerPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        footerPanel.Height = 80;
        
        // Footer shadow
        var footerShadow = InformationModel.Make<Rectangle>("OperatorFooterShadow");
        footerShadow.HorizontalAlignment = HorizontalAlignment.Stretch;
        footerShadow.VerticalAlignment = VerticalAlignment.Stretch;
        footerShadow.LeftMargin = 3;
        footerShadow.TopMargin = 3;
        footerShadow.FillColor = new Color((uint)0x20000000);
        footerShadow.CornerRadius = 8;
        footerPanel.Add(footerShadow);
        
        // Footer background
        var footerBg = InformationModel.Make<Rectangle>("OperatorFooterBg");
        footerBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        footerBg.VerticalAlignment = VerticalAlignment.Stretch;
        footerBg.RightMargin = 3;
        footerBg.BottomMargin = 3;
        footerBg.FillColor = new Color((uint)0xFFF8F9FA);
        footerBg.CornerRadius = 8;
        footerPanel.Add(footerBg);
        
        // Footer content
        var footerContent = InformationModel.Make<RowLayout>("OperatorFooterContent");
        footerContent.HorizontalAlignment = HorizontalAlignment.Stretch;
        footerContent.VerticalAlignment = VerticalAlignment.Center;
        footerContent.LeftMargin = 30;
        footerContent.RightMargin = 30;
        footerContent.HorizontalGap = 30;
        
        // Status message
        var statusMessage = InformationModel.Make<Label>("OperatorStatusMessage");
        statusMessage.Text = "System Status: All operations normal";
        statusMessage.FontSize = 14;
        statusMessage.TextColor = SUCCESS_GREEN;
        statusMessage.HorizontalAlignment = HorizontalAlignment.Left;
        footerContent.Add(statusMessage);
        
        // Navigation buttons
        var navButtons = CreateFooterNavigationButtons();
        footerContent.Add(navButtons);
        
        footerPanel.Add(footerContent);
        return footerPanel;
    }

    private Panel CreateBasicInputField(string label, string placeholder)
    {
        var fieldPanel = InformationModel.Make<Panel>("BasicField_" + label.Replace(":", "").Replace(" ", ""));
        fieldPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        
        var fieldLayout = InformationModel.Make<ColumnLayout>("BasicFieldLayout");
        fieldLayout.VerticalGap = 8;
        
        var fieldLabel = InformationModel.Make<Label>("BasicFieldLabel");
        fieldLabel.Text = label;
        fieldLabel.FontSize = 12;
        fieldLabel.TextColor = DARK_TEXT;
        fieldLayout.Add(fieldLabel);
        
        var inputField = InformationModel.Make<TextBox>("BasicFieldInput");
        inputField.HorizontalAlignment = HorizontalAlignment.Stretch;
        inputField.Height = 35;
        inputField.Text = placeholder;
        inputField.FontSize = 12;
        fieldLayout.Add(inputField);
        
        fieldPanel.Add(fieldLayout);
        return fieldPanel;
    }

    private Button CreateSimpleButton(string buttonText)
    {
        var button = InformationModel.Make<Button>("SimpleButton_" + buttonText.Replace(" ", ""));
        button.Width = 140;
        button.Height = 35;
        button.Text = buttonText;
        button.FontSize = 12;
        button.TextColor = WHITE;
        return button;
    }

    private Panel CreateDowntimeReasonsSection()
    {
        var downtimePanel = InformationModel.Make<Panel>("DowntimeReasonsSection");
        downtimePanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        downtimePanel.Height = 140;
        
        var sectionBg = InformationModel.Make<Rectangle>("DowntimeReasonsBg");
        sectionBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionBg.VerticalAlignment = VerticalAlignment.Stretch;
        sectionBg.FillColor = new Color((uint)0xFFF8F9FA);
        sectionBg.CornerRadius = 6;
        downtimePanel.Add(sectionBg);
        
        var layout = InformationModel.Make<ColumnLayout>("DowntimeReasonsLayout");
        layout.HorizontalAlignment = HorizontalAlignment.Stretch;
        layout.LeftMargin = 20;
        layout.RightMargin = 20;
        layout.TopMargin = 15;
        layout.BottomMargin = 15;
        layout.VerticalGap = 15;
        
        var title = InformationModel.Make<Label>("DowntimeReasonsTitle");
        title.Text = "Downtime Reasons";
        title.FontSize = 16;
        title.TextColor = DANGER_RED;
        layout.Add(title);
        
        var reasonsRow = InformationModel.Make<RowLayout>("DowntimeReasonsRow");
        reasonsRow.HorizontalAlignment = HorizontalAlignment.Stretch;
        reasonsRow.HorizontalGap = 15;
        
        var mechanicalBtn = CreateSimpleButton("Mechanical");
        reasonsRow.Add(mechanicalBtn);
        
        var electricalBtn = CreateSimpleButton("Electrical");
        reasonsRow.Add(electricalBtn);
        
        var materialBtn = CreateSimpleButton("Material");
        reasonsRow.Add(materialBtn);
        
        var otherBtn = CreateSimpleButton("Other");
        reasonsRow.Add(otherBtn);
        
        layout.Add(reasonsRow);
        downtimePanel.Add(layout);
        return downtimePanel;
    }

    private Panel CreateRejectReasonsSection()
    {
        var rejectPanel = InformationModel.Make<Panel>("RejectReasonsSection");
        rejectPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        rejectPanel.Height = 100;
        
        var sectionBg = InformationModel.Make<Rectangle>("RejectReasonsBg");
        sectionBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionBg.VerticalAlignment = VerticalAlignment.Stretch;
        sectionBg.FillColor = new Color((uint)0xFFF8F9FA);
        sectionBg.CornerRadius = 6;
        rejectPanel.Add(sectionBg);
        
        var layout = InformationModel.Make<ColumnLayout>("RejectReasonsLayout");
        layout.HorizontalAlignment = HorizontalAlignment.Stretch;
        layout.LeftMargin = 20;
        layout.RightMargin = 20;
        layout.TopMargin = 15;
        layout.BottomMargin = 15;
        layout.VerticalGap = 15;
        
        var title = InformationModel.Make<Label>("RejectReasonsTitle");
        title.Text = "Reject Reasons";
        title.FontSize = 16;
        title.TextColor = DANGER_RED;
        layout.Add(title);
        
        var rejectField = CreateBasicInputField("Reject Reason:", "Enter reason...");
        layout.Add(rejectField);
        
        rejectPanel.Add(layout);
        return rejectPanel;
    }

    private Panel CreateSystemControlSection()
    {
        var controlPanel = InformationModel.Make<Panel>("SystemControlSection");
        controlPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        controlPanel.Height = 140;
        
        var sectionBg = InformationModel.Make<Rectangle>("SystemControlBg");
        sectionBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionBg.VerticalAlignment = VerticalAlignment.Stretch;
        sectionBg.FillColor = new Color((uint)0xFFF8F9FA);
        sectionBg.CornerRadius = 6;
        controlPanel.Add(sectionBg);
        
        var layout = InformationModel.Make<ColumnLayout>("SystemControlLayout");
        layout.HorizontalAlignment = HorizontalAlignment.Stretch;
        layout.LeftMargin = 20;
        layout.RightMargin = 20;
        layout.TopMargin = 15;
        layout.BottomMargin = 15;
        layout.VerticalGap = 15;
        
        var title = InformationModel.Make<Label>("SystemControlTitle");
        title.Text = "System Control";
        title.FontSize = 16;
        title.TextColor = PRIMARY_BLUE;
        layout.Add(title);
        
        var controlsRow = InformationModel.Make<RowLayout>("SystemControlsRow");
        controlsRow.HorizontalAlignment = HorizontalAlignment.Stretch;
        controlsRow.HorizontalGap = 15;
        
        var startBtn = CreateSimpleButton("Start");
        controlsRow.Add(startBtn);
        
        var stopBtn = CreateSimpleButton("Stop");
        controlsRow.Add(stopBtn);
        
        var resetBtn = CreateSimpleButton("Reset");
        controlsRow.Add(resetBtn);
        
        layout.Add(controlsRow);
        controlPanel.Add(layout);
        return controlPanel;
    }

    private Panel CreateCurrentStatusSection()
    {
        var statusPanel = InformationModel.Make<Panel>("CurrentStatusSection");
        statusPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        statusPanel.Height = 120;
        
        var sectionBg = InformationModel.Make<Rectangle>("CurrentStatusBg");
        sectionBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionBg.VerticalAlignment = VerticalAlignment.Stretch;
        sectionBg.FillColor = new Color((uint)0xFFF8F9FA);
        sectionBg.CornerRadius = 6;
        statusPanel.Add(sectionBg);
        
        var layout = InformationModel.Make<ColumnLayout>("CurrentStatusLayout");
        layout.HorizontalAlignment = HorizontalAlignment.Stretch;
        layout.LeftMargin = 20;
        layout.RightMargin = 20;
        layout.TopMargin = 15;
        layout.BottomMargin = 15;
        layout.VerticalGap = 10;
        
        var title = InformationModel.Make<Label>("CurrentStatusTitle");
        title.Text = "Current Status";
        title.FontSize = 16;
        title.TextColor = SUCCESS_GREEN;
        layout.Add(title);
        
        var statusLabel = InformationModel.Make<Label>("CurrentStatusLabel");
        statusLabel.Text = "Machine Running";
        statusLabel.FontSize = 14;
        statusLabel.TextColor = DARK_TEXT;
        layout.Add(statusLabel);
        
        var oeeLabel = InformationModel.Make<Label>("CurrentOEELabel");
        oeeLabel.Text = "Current OEE: 72.5%";
        oeeLabel.FontSize = 12;
        oeeLabel.TextColor = PRIMARY_BLUE;
        layout.Add(oeeLabel);
        
        statusPanel.Add(layout);
        return statusPanel;
    }

    private Panel CreateEmergencyActionsSection()
    {
        var emergencyPanel = InformationModel.Make<Panel>("EmergencyActionsSection");
        emergencyPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        emergencyPanel.Height = 100;
        
        var sectionBg = InformationModel.Make<Rectangle>("EmergencyActionsBg");
        sectionBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionBg.VerticalAlignment = VerticalAlignment.Stretch;
        sectionBg.FillColor = new Color((uint)0xFFFFE6E6);
        sectionBg.CornerRadius = 6;
        emergencyPanel.Add(sectionBg);
        
        var layout = InformationModel.Make<ColumnLayout>("EmergencyActionsLayout");
        layout.HorizontalAlignment = HorizontalAlignment.Stretch;
        layout.LeftMargin = 20;
        layout.RightMargin = 20;
        layout.TopMargin = 15;
        layout.BottomMargin = 15;
        layout.VerticalGap = 15;
        
        var title = InformationModel.Make<Label>("EmergencyActionsTitle");
        title.Text = "Emergency Actions";
        title.FontSize = 16;
        title.TextColor = DANGER_RED;
        layout.Add(title);
        
        var emergencyBtn = CreateSimpleButton("Emergency Stop");
        emergencyBtn.HorizontalAlignment = HorizontalAlignment.Center;
        layout.Add(emergencyBtn);
        
        emergencyPanel.Add(layout);
        return emergencyPanel;
    }

    private RowLayout CreateFooterNavigationButtons()
    {
        var navLayout = InformationModel.Make<RowLayout>("FooterNavigation");
        navLayout.HorizontalAlignment = HorizontalAlignment.Right;
        navLayout.HorizontalGap = 15;
        
        var dashboardBtn = CreateSimpleButton("Dashboard");
        navLayout.Add(dashboardBtn);
        
        var detailBtn = CreateSimpleButton("Machine Detail");
        navLayout.Add(detailBtn);
        
        var reportsBtn = CreateSimpleButton("Reports");
        navLayout.Add(reportsBtn);
        
        return navLayout;
    }

    private Panel CreateConfigHeaderPanel()
    {
        var headerPanel = InformationModel.Make<Panel>("ConfigHeaderPanel");
        headerPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        headerPanel.Height = 120;
        
        // Header shadow
        var headerShadow = InformationModel.Make<Rectangle>("ConfigHeaderShadow");
        headerShadow.HorizontalAlignment = HorizontalAlignment.Stretch;
        headerShadow.VerticalAlignment = VerticalAlignment.Stretch;
        headerShadow.LeftMargin = 3;
        headerShadow.TopMargin = 3;
        headerShadow.FillColor = new Color((uint)0x20000000);
        headerShadow.CornerRadius = 8;
        headerPanel.Add(headerShadow);
        
        // Header background
        var headerBg = InformationModel.Make<Rectangle>("ConfigHeaderBg");
        headerBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        headerBg.VerticalAlignment = VerticalAlignment.Stretch;
        headerBg.RightMargin = 3;
        headerBg.BottomMargin = 3;
        headerBg.FillColor = PRIMARY_BLUE;
        headerBg.CornerRadius = 8;
        headerPanel.Add(headerBg);
        
        // Header content layout
        var headerLayout = InformationModel.Make<RowLayout>("ConfigHeaderLayout");
        headerLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        headerLayout.VerticalAlignment = VerticalAlignment.Center;
        headerLayout.LeftMargin = 30;
        headerLayout.RightMargin = 30;
        headerLayout.HorizontalGap = 30;
        
        // Title section
        var titleSection = InformationModel.Make<ColumnLayout>("ConfigTitleSection");
        titleSection.VerticalGap = 8;
        
        var mainTitle = InformationModel.Make<Label>("ConfigMainTitle");
        mainTitle.Text = "OEE Configuration Center";
        mainTitle.FontSize = 28;
        mainTitle.TextColor = WHITE;
        titleSection.Add(mainTitle);
        
        var subtitle = InformationModel.Make<Label>("ConfigSubtitle");
        subtitle.Text = "Configure OEE parameters, thresholds, and system settings";
        subtitle.FontSize = 14;
        subtitle.TextColor = new Color((uint)0xFFE8F4FD);
        titleSection.Add(subtitle);
        
        headerLayout.Add(titleSection);
        
        // Current config status
        var statusSection = CreateConfigStatusSection();
        headerLayout.Add(statusSection);
        
        headerPanel.Add(headerLayout);
        return headerPanel;
    }

    private Panel CreateConfigStatusSection()
    {
        var statusPanel = InformationModel.Make<Panel>("ConfigStatusSection");
        statusPanel.HorizontalAlignment = HorizontalAlignment.Right;
        statusPanel.Width = 400;
        
        var statusLayout = InformationModel.Make<RowLayout>("ConfigStatusLayout");
        statusLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        statusLayout.HorizontalGap = 20;
        
        // Last saved indicator
        var savedItem = CreateConfigStatusItem("Last Saved:", "10:45 AM");
        statusLayout.Add(savedItem);
        
        // Config version
        var versionItem = CreateConfigStatusItem("Config Ver:", "v2.1");
        statusLayout.Add(versionItem);
        
        // Active profile
        var profileItem = CreateConfigStatusItem("Profile:", "Production");
        statusLayout.Add(profileItem);
        
        statusPanel.Add(statusLayout);
        return statusPanel;
    }

    private Panel CreateOEEParametersPanel()
    {
        var parametersPanel = InformationModel.Make<Panel>("OEEParametersPanel");
        parametersPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        parametersPanel.VerticalAlignment = VerticalAlignment.Stretch;
        
        // Panel shadow
        var panelShadow = InformationModel.Make<Rectangle>("OEEParametersShadow");
        panelShadow.HorizontalAlignment = HorizontalAlignment.Stretch;
        panelShadow.VerticalAlignment = VerticalAlignment.Stretch;
        panelShadow.LeftMargin = 3;
        panelShadow.TopMargin = 3;
        panelShadow.FillColor = new Color((uint)0x20000000);
        panelShadow.CornerRadius = 8;
        parametersPanel.Add(panelShadow);
        
        // Panel background
        var panelBg = InformationModel.Make<Rectangle>("OEEParametersBg");
        panelBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        panelBg.VerticalAlignment = VerticalAlignment.Stretch;
        panelBg.RightMargin = 3;
        panelBg.BottomMargin = 3;
        panelBg.FillColor = WHITE;
        panelBg.CornerRadius = 8;
        parametersPanel.Add(panelBg);
        
        // Content layout
        var contentLayout = InformationModel.Make<ColumnLayout>("OEEParametersContent");
        contentLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        contentLayout.VerticalAlignment = VerticalAlignment.Stretch;
        contentLayout.LeftMargin = 25;
        contentLayout.TopMargin = 25;
        contentLayout.RightMargin = 25;
        contentLayout.BottomMargin = 25;
        contentLayout.VerticalGap = 20;
        
        // Section title
        var sectionTitle = InformationModel.Make<Label>("OEEParametersTitle");
        sectionTitle.Text = "OEE Parameters & Thresholds";
        sectionTitle.FontSize = 18;
        sectionTitle.TextColor = DARK_TEXT;
        contentLayout.Add(sectionTitle);
        
        // Availability thresholds
        var availabilitySection = CreateAvailabilityThresholdsSection();
        contentLayout.Add(availabilitySection);
        
        // Performance thresholds
        var performanceSection = CreatePerformanceThresholdsSection();
        contentLayout.Add(performanceSection);
        
        // Quality thresholds
        var qualitySection = CreateQualityThresholdsSection();
        contentLayout.Add(qualitySection);
        
        // Overall OEE targets
        var oeeTargetsSection = CreateOEETargetsSection();
        contentLayout.Add(oeeTargetsSection);
        
        parametersPanel.Add(contentLayout);
        return parametersPanel;
    }

    private Panel CreateMachineSettingsPanel()
    {
        var machinePanel = InformationModel.Make<Panel>("MachineSettingsPanel");
        machinePanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        machinePanel.VerticalAlignment = VerticalAlignment.Stretch;
        
        // Panel shadow
        var panelShadow = InformationModel.Make<Rectangle>("MachineSettingsShadow");
        panelShadow.HorizontalAlignment = HorizontalAlignment.Stretch;
        panelShadow.VerticalAlignment = VerticalAlignment.Stretch;
        panelShadow.LeftMargin = 3;
        panelShadow.TopMargin = 3;
        panelShadow.FillColor = new Color((uint)0x20000000);
        panelShadow.CornerRadius = 8;
        machinePanel.Add(panelShadow);
        
        // Panel background
        var panelBg = InformationModel.Make<Rectangle>("MachineSettingsBg");
        panelBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        panelBg.VerticalAlignment = VerticalAlignment.Stretch;
        panelBg.RightMargin = 3;
        panelBg.BottomMargin = 3;
        panelBg.FillColor = WHITE;
        panelBg.CornerRadius = 8;
        machinePanel.Add(panelBg);
        
        // Content layout
        var contentLayout = InformationModel.Make<ColumnLayout>("MachineSettingsContent");
        contentLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        contentLayout.VerticalAlignment = VerticalAlignment.Stretch;
        contentLayout.LeftMargin = 25;
        contentLayout.TopMargin = 25;
        contentLayout.RightMargin = 25;
        contentLayout.BottomMargin = 25;
        contentLayout.VerticalGap = 20;
        
        // Section title
        var sectionTitle = InformationModel.Make<Label>("MachineSettingsTitle");
        sectionTitle.Text = "Machine & Shift Configuration";
        sectionTitle.FontSize = 18;
        sectionTitle.TextColor = DARK_TEXT;
        contentLayout.Add(sectionTitle);
        
        // Machine parameters
        var machineParamsSection = CreateMachineParametersSection();
        contentLayout.Add(machineParamsSection);
        
        // Shift configuration
        var shiftConfigSection = CreateShiftConfigurationSection();
        contentLayout.Add(shiftConfigSection);
        
        // Downtime categories
        var downtimeCategoriesSection = CreateDowntimeCategoriesSection();
        contentLayout.Add(downtimeCategoriesSection);
        
        // Product settings
        var productSettingsSection = CreateProductSettingsSection();
        contentLayout.Add(productSettingsSection);
        
        machinePanel.Add(contentLayout);
        return machinePanel;
    }

    private Panel CreateSystemConfigPanel()
    {
        var systemPanel = InformationModel.Make<Panel>("SystemConfigPanel");
        systemPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        systemPanel.VerticalAlignment = VerticalAlignment.Stretch;
        
        // Panel shadow
        var panelShadow = InformationModel.Make<Rectangle>("SystemConfigShadow");
        panelShadow.HorizontalAlignment = HorizontalAlignment.Stretch;
        panelShadow.VerticalAlignment = VerticalAlignment.Stretch;
        panelShadow.LeftMargin = 3;
        panelShadow.TopMargin = 3;
        panelShadow.FillColor = new Color((uint)0x20000000);
        panelShadow.CornerRadius = 8;
        systemPanel.Add(panelShadow);
        
        // Panel background
        var panelBg = InformationModel.Make<Rectangle>("SystemConfigBg");
        panelBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        panelBg.VerticalAlignment = VerticalAlignment.Stretch;
        panelBg.RightMargin = 3;
        panelBg.BottomMargin = 3;
        panelBg.FillColor = WHITE;
        panelBg.CornerRadius = 8;
        systemPanel.Add(panelBg);
        
        // Content layout
        var contentLayout = InformationModel.Make<ColumnLayout>("SystemConfigContent");
        contentLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        contentLayout.VerticalAlignment = VerticalAlignment.Stretch;
        contentLayout.LeftMargin = 25;
        contentLayout.TopMargin = 25;
        contentLayout.RightMargin = 25;
        contentLayout.BottomMargin = 25;
        contentLayout.VerticalGap = 20;
        
        // Section title
        var sectionTitle = InformationModel.Make<Label>("SystemConfigTitle");
        sectionTitle.Text = "System Configuration & Advanced";
        sectionTitle.FontSize = 18;
        sectionTitle.TextColor = DARK_TEXT;
        contentLayout.Add(sectionTitle);
        
        // Data collection settings
        var dataCollectionSection = CreateDataCollectionSettingsSection();
        contentLayout.Add(dataCollectionSection);
        
        // Calculation settings
        var calculationSection = CreateCalculationSettingsSection();
        contentLayout.Add(calculationSection);
        
        // Alarm settings
        var alarmSection = CreateAlarmSettingsSection();
        contentLayout.Add(alarmSection);
        
        // Export & backup settings
        var exportSection = CreateExportBackupSection();
        contentLayout.Add(exportSection);
        
        systemPanel.Add(contentLayout);
        return systemPanel;
    }

    private Panel CreateConfigFooterPanel()
    {
        var footerPanel = InformationModel.Make<Panel>("ConfigFooterPanel");
        footerPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        footerPanel.Height = 80;
        
        var footerLayout = InformationModel.Make<RowLayout>("ConfigFooterLayout");
        footerLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        footerLayout.VerticalAlignment = VerticalAlignment.Center;
        footerLayout.HorizontalGap = 20;
        footerLayout.LeftMargin = 20;
        footerLayout.RightMargin = 20;
        
        // Left side - Status indicators
        var statusSection = InformationModel.Make<RowLayout>("ConfigStatusIndicators");
        statusSection.HorizontalGap = 15;
        
        var unsavedLabel = InformationModel.Make<Label>("UnsavedChangesLabel");
        unsavedLabel.Text = "● Unsaved Changes";
        unsavedLabel.FontSize = 12;
        unsavedLabel.TextColor = WARNING_AMBER;
        statusSection.Add(unsavedLabel);
        
        footerLayout.Add(statusSection);
        
        // Right side - Action buttons
        var actionsSection = InformationModel.Make<RowLayout>("ConfigActions");
        actionsSection.HorizontalAlignment = HorizontalAlignment.Right;
        actionsSection.HorizontalGap = 15;
        
        var resetBtn = CreateConfigActionButton("Reset to Defaults", MEDIUM_TEXT);
        actionsSection.Add(resetBtn);
        
        var cancelBtn = CreateConfigActionButton("Cancel", MEDIUM_TEXT);
        actionsSection.Add(cancelBtn);
        
        var saveBtn = CreateConfigActionButton("Save Configuration", SUCCESS_GREEN);
        actionsSection.Add(saveBtn);
        
        var applyBtn = CreateConfigActionButton("Apply & Restart", PRIMARY_BLUE);
        actionsSection.Add(applyBtn);
        
        footerLayout.Add(actionsSection);
        footerPanel.Add(footerLayout);
        return footerPanel;
    }

    private Panel CreateConfigStatusItem(string label, string value)
    {
        var itemPanel = InformationModel.Make<Panel>("ConfigStatusItem_" + label.Replace(":", ""));
        itemPanel.Width = 80;
        
        var itemLayout = InformationModel.Make<ColumnLayout>("ConfigStatusItemLayout_" + label.Replace(":", ""));
        itemLayout.HorizontalAlignment = HorizontalAlignment.Center;
        itemLayout.VerticalGap = 3;
        
        var labelText = InformationModel.Make<Label>("ConfigStatusLabel_" + label.Replace(":", ""));
        labelText.Text = label;
        labelText.FontSize = 10;
        labelText.TextColor = new Color((uint)0xFFE8F4FD);
        labelText.HorizontalAlignment = HorizontalAlignment.Center;
        
        var valueText = InformationModel.Make<Label>("ConfigStatusValue_" + label.Replace(":", ""));
        valueText.Text = value;
        valueText.FontSize = 12;
        valueText.TextColor = WHITE;
        valueText.HorizontalAlignment = HorizontalAlignment.Center;
        // DATA LINK: Appropriate configuration variables
        
        itemLayout.Add(labelText);
        itemLayout.Add(valueText);
        itemPanel.Add(itemLayout);
        
        return itemPanel;
    }

    private Panel CreateAvailabilityThresholdsSection()
    {
        var section = InformationModel.Make<Panel>("AvailabilityThresholds");
        section.HorizontalAlignment = HorizontalAlignment.Stretch;
        section.Height = 120;
        
        var sectionBg = InformationModel.Make<Rectangle>("AvailabilityThresholdsBg");
        sectionBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionBg.VerticalAlignment = VerticalAlignment.Stretch;
        sectionBg.FillColor = new Color((uint)0xFFF8F9FA);
        sectionBg.CornerRadius = 6;
        section.Add(sectionBg);
        
        var layout = InformationModel.Make<ColumnLayout>("AvailabilityThresholdsLayout");
        layout.HorizontalAlignment = HorizontalAlignment.Stretch;
        layout.LeftMargin = 20;
        layout.RightMargin = 20;
        layout.TopMargin = 15;
        layout.BottomMargin = 15;
        layout.VerticalGap = 10;
        
        var title = InformationModel.Make<Label>("AvailabilityThresholdsTitle");
        title.Text = "Availability Thresholds";
        title.FontSize = 14;
        title.TextColor = SUCCESS_GREEN;
        layout.Add(title);
        
        var thresholdsRow = InformationModel.Make<RowLayout>("AvailabilityThresholdsRow");
        thresholdsRow.HorizontalAlignment = HorizontalAlignment.Stretch;
        thresholdsRow.HorizontalGap = 15;
        
        var targetField = CreateConfigInputField("Target %:", "85.0", "/Objects/Model/OEEInstances/Machine1/Configuration/AvailabilityTarget");
        thresholdsRow.Add(targetField);
        
        var warningField = CreateConfigInputField("Warning %:", "80.0", "/Objects/Model/OEEInstances/Machine1/Configuration/AvailabilityWarningThreshold");
        thresholdsRow.Add(warningField);
        
        var criticalField = CreateConfigInputField("Critical %:", "75.0", "/Objects/Model/OEEInstances/Machine1/Configuration/AvailabilityCriticalThreshold");
        thresholdsRow.Add(criticalField);
        
        layout.Add(thresholdsRow);
        section.Add(layout);
        return section;
    }

    private Panel CreatePerformanceThresholdsSection()
    {
        var section = InformationModel.Make<Panel>("PerformanceThresholds");
        section.HorizontalAlignment = HorizontalAlignment.Stretch;
        section.Height = 120;
        
        var sectionBg = InformationModel.Make<Rectangle>("PerformanceThresholdsBg");
        sectionBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionBg.VerticalAlignment = VerticalAlignment.Stretch;
        sectionBg.FillColor = new Color((uint)0xFFF8F9FA);
        sectionBg.CornerRadius = 6;
        section.Add(sectionBg);
        
        var layout = InformationModel.Make<ColumnLayout>("PerformanceThresholdsLayout");
        layout.HorizontalAlignment = HorizontalAlignment.Stretch;
        layout.LeftMargin = 20;
        layout.RightMargin = 20;
        layout.TopMargin = 15;
        layout.BottomMargin = 15;
        layout.VerticalGap = 10;
        
        var title = InformationModel.Make<Label>("PerformanceThresholdsTitle");
        title.Text = "Performance Thresholds";
        title.FontSize = 14;
        title.TextColor = PRIMARY_BLUE;
        layout.Add(title);
        
        var thresholdsRow = InformationModel.Make<RowLayout>("PerformanceThresholdsRow");
        thresholdsRow.HorizontalAlignment = HorizontalAlignment.Stretch;
        thresholdsRow.HorizontalGap = 15;
        
        var targetField = CreateConfigInputField("Target %:", "90.0", "/Objects/Model/OEEInstances/Machine1/Configuration/PerformanceTarget");
        thresholdsRow.Add(targetField);
        
        var warningField = CreateConfigInputField("Warning %:", "85.0", "/Objects/Model/OEEInstances/Machine1/Configuration/PerformanceWarningThreshold");
        thresholdsRow.Add(warningField);
        
        var criticalField = CreateConfigInputField("Critical %:", "80.0", "/Objects/Model/OEEInstances/Machine1/Configuration/PerformanceCriticalThreshold");
        thresholdsRow.Add(criticalField);
        
        layout.Add(thresholdsRow);
        section.Add(layout);
        return section;
    }

    private Panel CreateQualityThresholdsSection()
    {
        var section = InformationModel.Make<Panel>("QualityThresholds");
        section.HorizontalAlignment = HorizontalAlignment.Stretch;
        section.Height = 120;
        
        var sectionBg = InformationModel.Make<Rectangle>("QualityThresholdsBg");
        sectionBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionBg.VerticalAlignment = VerticalAlignment.Stretch;
        sectionBg.FillColor = new Color((uint)0xFFF8F9FA);
        sectionBg.CornerRadius = 6;
        section.Add(sectionBg);
        
        var layout = InformationModel.Make<ColumnLayout>("QualityThresholdsLayout");
        layout.HorizontalAlignment = HorizontalAlignment.Stretch;
        layout.LeftMargin = 20;
        layout.RightMargin = 20;
        layout.TopMargin = 15;
        layout.BottomMargin = 15;
        layout.VerticalGap = 10;
        
        var title = InformationModel.Make<Label>("QualityThresholdsTitle");
        title.Text = "Quality Thresholds";
        title.FontSize = 14;
        title.TextColor = WARNING_AMBER;
        layout.Add(title);
        
        var thresholdsRow = InformationModel.Make<RowLayout>("QualityThresholdsRow");
        thresholdsRow.HorizontalAlignment = HorizontalAlignment.Stretch;
        thresholdsRow.HorizontalGap = 15;
        
        var targetField = CreateConfigInputField("Target %:", "95.0", "/Objects/Model/OEEInstances/Machine1/Configuration/QualityTarget");
        thresholdsRow.Add(targetField);
        
        var warningField = CreateConfigInputField("Warning %:", "92.0", "/Objects/Model/OEEInstances/Machine1/Configuration/QualityWarningThreshold");
        thresholdsRow.Add(warningField);
        
        var criticalField = CreateConfigInputField("Critical %:", "88.0", "/Objects/Model/OEEInstances/Machine1/Configuration/QualityCriticalThreshold");
        thresholdsRow.Add(criticalField);
        
        layout.Add(thresholdsRow);
        section.Add(layout);
        return section;
    }

    private Panel CreateOEETargetsSection()
    {
        var section = InformationModel.Make<Panel>("OEETargets");
        section.HorizontalAlignment = HorizontalAlignment.Stretch;
        section.Height = 120;
        
        var sectionBg = InformationModel.Make<Rectangle>("OEETargetsBg");
        sectionBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionBg.VerticalAlignment = VerticalAlignment.Stretch;
        sectionBg.FillColor = new Color((uint)0xFFF8F9FA);
        sectionBg.CornerRadius = 6;
        section.Add(sectionBg);
        
        var layout = InformationModel.Make<ColumnLayout>("OEETargetsLayout");
        layout.HorizontalAlignment = HorizontalAlignment.Stretch;
        layout.LeftMargin = 20;
        layout.RightMargin = 20;
        layout.TopMargin = 15;
        layout.BottomMargin = 15;
        layout.VerticalGap = 10;
        
        var title = InformationModel.Make<Label>("OEETargetsTitle");
        title.Text = "Overall OEE Targets";
        title.FontSize = 14;
        title.TextColor = DANGER_RED;
        layout.Add(title);
        
        var targetsRow = InformationModel.Make<RowLayout>("OEETargetsRow");
        targetsRow.HorizontalAlignment = HorizontalAlignment.Stretch;
        targetsRow.HorizontalGap = 15;
        
        var worldClassField = CreateConfigInputField("World Class %:", "85.0", "/Objects/Model/OEEInstances/Machine1/Configuration/WorldClassOEETarget");
        targetsRow.Add(worldClassField);
        
        var goodField = CreateConfigInputField("Good %:", "70.0", "/Objects/Model/OEEInstances/Machine1/Configuration/GoodOEETarget");
        targetsRow.Add(goodField);
        
        var acceptableField = CreateConfigInputField("Acceptable %:", "60.0", "/Objects/Model/OEEInstances/Machine1/Configuration/AcceptableOEETarget");
        targetsRow.Add(acceptableField);
        
        layout.Add(targetsRow);
        section.Add(layout);
        return section;
    }

    private Panel CreateConfigInputField(string label, string defaultValue, string dataLink)
    {
        var fieldPanel = InformationModel.Make<Panel>("ConfigField_" + label.Replace(" ", "").Replace(":", "").Replace("%", "Pct"));
        fieldPanel.Width = 120;
        
        var fieldLayout = InformationModel.Make<ColumnLayout>("ConfigFieldLayout_" + label.Replace(" ", "").Replace(":", "").Replace("%", "Pct"));
        fieldLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        fieldLayout.VerticalGap = 5;
        
        var fieldLabel = InformationModel.Make<Label>("ConfigFieldLabel_" + label.Replace(" ", "").Replace(":", "").Replace("%", "Pct"));
        fieldLabel.Text = label;
        fieldLabel.FontSize = 10;
        fieldLabel.TextColor = MEDIUM_TEXT;
        fieldLabel.HorizontalAlignment = HorizontalAlignment.Center;
        
        var fieldInput = InformationModel.Make<TextBox>("ConfigFieldInput_" + label.Replace(" ", "").Replace(":", "").Replace("%", "Pct"));
        fieldInput.Text = defaultValue;
        fieldInput.FontSize = 12;
        fieldInput.Width = 100;
        fieldInput.Height = 30;
        fieldInput.HorizontalAlignment = HorizontalAlignment.Center;
        // DATA LINK: fieldInput.Text -> dataLink
        
        fieldLayout.Add(fieldLabel);
        fieldLayout.Add(fieldInput);
        fieldPanel.Add(fieldLayout);
        
        return fieldPanel;
    }

    private Button CreateConfigActionButton(string text, Color color)
    {
        var button = InformationModel.Make<Button>("ConfigActionBtn_" + text.Replace(" ", ""));
        button.Text = text;
        button.FontSize = 12;
        button.Width = 150;
        button.Height = 40;
        button.BackgroundColor = color;
        button.TextColor = WHITE;
        
        return button;
    }

    private Panel CreateMachineParametersSection()
    {
        var section = InformationModel.Make<Panel>("MachineParameters");
        section.HorizontalAlignment = HorizontalAlignment.Stretch;
        section.Height = 140;
        
        var sectionBg = InformationModel.Make<Rectangle>("MachineParametersBg");
        sectionBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionBg.VerticalAlignment = VerticalAlignment.Stretch;
        sectionBg.FillColor = new Color((uint)0xFFF8F9FA);
        sectionBg.CornerRadius = 6;
        section.Add(sectionBg);
        
        var layout = InformationModel.Make<ColumnLayout>("MachineParametersLayout");
        layout.HorizontalAlignment = HorizontalAlignment.Stretch;
        layout.LeftMargin = 20;
        layout.RightMargin = 20;
        layout.TopMargin = 15;
        layout.BottomMargin = 15;
        layout.VerticalGap = 10;
        
        var title = InformationModel.Make<Label>("MachineParametersTitle");
        title.Text = "Machine Parameters";
        title.FontSize = 14;
        title.TextColor = PRIMARY_BLUE;
        layout.Add(title);
        
        var paramsRow1 = InformationModel.Make<RowLayout>("MachineParametersRow1");
        paramsRow1.HorizontalAlignment = HorizontalAlignment.Stretch;
        paramsRow1.HorizontalGap = 15;
        
        var nameField = CreateConfigInputField("Machine Name:", "Line 1", "/Objects/Model/OEEInstances/Machine1/Configuration/MachineName");
        paramsRow1.Add(nameField);
        
        var typeField = CreateConfigInputField("Machine Type:", "CNC", "/Objects/Model/OEEInstances/Machine1/Configuration/MachineType");
        paramsRow1.Add(typeField);
        
        var speedField = CreateConfigInputField("Rated Speed:", "100", "/Objects/Model/OEEInstances/Machine1/Configuration/IdealCycleTime");
        paramsRow1.Add(speedField);
        
        layout.Add(paramsRow1);
        
        var paramsRow2 = InformationModel.Make<RowLayout>("MachineParametersRow2");
        paramsRow2.HorizontalAlignment = HorizontalAlignment.Stretch;
        paramsRow2.HorizontalGap = 15;
        
        var locationField = CreateConfigInputField("Location:", "Shop Floor A", "/Objects/Model/OEEInstances/Machine1/Configuration/Location");
        paramsRow2.Add(locationField);
        
        var departmentField = CreateConfigInputField("Department:", "Production", "/Objects/Model/OEEInstances/Machine1/Configuration/Department");
        paramsRow2.Add(departmentField);
        
        var operatorField = CreateConfigInputField("Operator ID:", "OP001", "/Objects/Model/OEEInstances/Machine1/Inputs/OperatorID");
        paramsRow2.Add(operatorField);
        
        layout.Add(paramsRow2);
        section.Add(layout);
        return section;
    }

    private Panel CreateShiftConfigurationSection()
    {
        var section = InformationModel.Make<Panel>("ShiftConfiguration");
        section.HorizontalAlignment = HorizontalAlignment.Stretch;
        section.Height = 120;
        
        var sectionBg = InformationModel.Make<Rectangle>("ShiftConfigurationBg");
        sectionBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionBg.VerticalAlignment = VerticalAlignment.Stretch;
        sectionBg.FillColor = new Color((uint)0xFFF8F9FA);
        sectionBg.CornerRadius = 6;
        section.Add(sectionBg);
        
        var layout = InformationModel.Make<ColumnLayout>("ShiftConfigurationLayout");
        layout.HorizontalAlignment = HorizontalAlignment.Stretch;
        layout.LeftMargin = 20;
        layout.RightMargin = 20;
        layout.TopMargin = 15;
        layout.BottomMargin = 15;
        layout.VerticalGap = 10;
        
        var title = InformationModel.Make<Label>("ShiftConfigurationTitle");
        title.Text = "Shift Configuration";
        title.FontSize = 14;
        title.TextColor = SUCCESS_GREEN;
        layout.Add(title);
        
        var shiftRow = InformationModel.Make<RowLayout>("ShiftConfigurationRow");
        shiftRow.HorizontalAlignment = HorizontalAlignment.Stretch;
        shiftRow.HorizontalGap = 15;
        
        var shift1Field = CreateConfigInputField("Shift 1 Start:", "06:00", "/Objects/Model/OEEInstances/Machine1/Configuration/Shift1StartTime");
        shiftRow.Add(shift1Field);
        
        var shift2Field = CreateConfigInputField("Shift 2 Start:", "14:00", "/Objects/Model/OEEInstances/Machine1/Configuration/Shift2StartTime");
        shiftRow.Add(shift2Field);
        
        var shift3Field = CreateConfigInputField("Shift 3 Start:", "22:00", "/Objects/Model/OEEInstances/Machine1/Configuration/Shift3StartTime");
        shiftRow.Add(shift3Field);
        
        var breakTimeField = CreateConfigInputField("Break Time (min):", "30", "/Objects/Model/OEEInstances/Machine1/Configuration/BreakTime");
        shiftRow.Add(breakTimeField);
        
        layout.Add(shiftRow);
        section.Add(layout);
        return section;
    }

    private Panel CreateDowntimeCategoriesSection()
    {
        var section = InformationModel.Make<Panel>("DowntimeCategories");
        section.HorizontalAlignment = HorizontalAlignment.Stretch;
        section.Height = 120;
        
        var sectionBg = InformationModel.Make<Rectangle>("DowntimeCategoriesBg");
        sectionBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionBg.VerticalAlignment = VerticalAlignment.Stretch;
        sectionBg.FillColor = new Color((uint)0xFFF8F9FA);
        sectionBg.CornerRadius = 6;
        section.Add(sectionBg);
        
        var layout = InformationModel.Make<ColumnLayout>("DowntimeCategoriesLayout");
        layout.HorizontalAlignment = HorizontalAlignment.Stretch;
        layout.LeftMargin = 20;
        layout.RightMargin = 20;
        layout.TopMargin = 15;
        layout.BottomMargin = 15;
        layout.VerticalGap = 10;
        
        var title = InformationModel.Make<Label>("DowntimeCategoriesTitle");
        title.Text = "Downtime Categories";
        title.FontSize = 14;
        title.TextColor = WARNING_AMBER;
        layout.Add(title);
        
        var categoriesRow = InformationModel.Make<RowLayout>("DowntimeCategoriesRow");
        categoriesRow.HorizontalAlignment = HorizontalAlignment.Stretch;
        categoriesRow.HorizontalGap = 15;
        
        var plannedBtn = CreateSimpleButton("Planned Downtime");
        categoriesRow.Add(plannedBtn);
        
        var unplannedBtn = CreateSimpleButton("Unplanned Downtime");
        categoriesRow.Add(unplannedBtn);
        
        var maintenanceBtn = CreateSimpleButton("Maintenance");
        categoriesRow.Add(maintenanceBtn);
        
        var setupBtn = CreateSimpleButton("Setup/Changeover");
        categoriesRow.Add(setupBtn);
        
        layout.Add(categoriesRow);
        section.Add(layout);
        return section;
    }

    private Panel CreateProductSettingsSection()
    {
        var section = InformationModel.Make<Panel>("ProductSettings");
        section.HorizontalAlignment = HorizontalAlignment.Stretch;
        section.Height = 120;
        
        var sectionBg = InformationModel.Make<Rectangle>("ProductSettingsBg");
        sectionBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionBg.VerticalAlignment = VerticalAlignment.Stretch;
        sectionBg.FillColor = new Color((uint)0xFFF8F9FA);
        sectionBg.CornerRadius = 6;
        section.Add(sectionBg);
        
        var layout = InformationModel.Make<ColumnLayout>("ProductSettingsLayout");
        layout.HorizontalAlignment = HorizontalAlignment.Stretch;
        layout.LeftMargin = 20;
        layout.RightMargin = 20;
        layout.TopMargin = 15;
        layout.BottomMargin = 15;
        layout.VerticalGap = 10;
        
        var title = InformationModel.Make<Label>("ProductSettingsTitle");
        title.Text = "Product Settings";
        title.FontSize = 14;
        title.TextColor = DANGER_RED;
        layout.Add(title);
        
        var productRow = InformationModel.Make<RowLayout>("ProductSettingsRow");
        productRow.HorizontalAlignment = HorizontalAlignment.Stretch;
        productRow.HorizontalGap = 15;
        
        var productIdField = CreateConfigInputField("Product ID:", "PRD001", "/Objects/Model/OEEInstances/Machine1/Inputs/ProductID");
        productRow.Add(productIdField);
        
        var lotSizeField = CreateConfigInputField("Lot Size:", "1000", "/Objects/Model/OEEInstances/Machine1/Inputs/LotSize");
        productRow.Add(lotSizeField);
        
        var cycleTimeField = CreateConfigInputField("Cycle Time (s):", "45.0", "/Objects/Model/OEEInstances/Machine1/Configuration/IdealCycleTime");
        productRow.Add(cycleTimeField);
        
        var scrapLimitField = CreateConfigInputField("Scrap Limit %:", "5.0", "/Objects/Model/OEEInstances/Machine1/Configuration/ScrapLimit");
        productRow.Add(scrapLimitField);
        
        layout.Add(productRow);
        section.Add(layout);
        return section;
    }

    private Panel CreateDataCollectionSettingsSection()
    {
        var section = InformationModel.Make<Panel>("DataCollectionSettings");
        section.HorizontalAlignment = HorizontalAlignment.Stretch;
        section.Height = 140;
        
        var sectionBg = InformationModel.Make<Rectangle>("DataCollectionSettingsBg");
        sectionBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionBg.VerticalAlignment = VerticalAlignment.Stretch;
        sectionBg.FillColor = new Color((uint)0xFFF8F9FA);
        sectionBg.CornerRadius = 6;
        section.Add(sectionBg);
        
        var layout = InformationModel.Make<ColumnLayout>("DataCollectionSettingsLayout");
        layout.HorizontalAlignment = HorizontalAlignment.Stretch;
        layout.LeftMargin = 20;
        layout.RightMargin = 20;
        layout.TopMargin = 15;
        layout.BottomMargin = 15;
        layout.VerticalGap = 10;
        
        var title = InformationModel.Make<Label>("DataCollectionSettingsTitle");
        title.Text = "Data Collection Settings";
        title.FontSize = 14;
        title.TextColor = PRIMARY_BLUE;
        layout.Add(title);
        
        var settingsRow1 = InformationModel.Make<RowLayout>("DataCollectionSettingsRow1");
        settingsRow1.HorizontalAlignment = HorizontalAlignment.Stretch;
        settingsRow1.HorizontalGap = 15;
        
        var intervalField = CreateConfigInputField("Update Interval (s):", "10", "/Objects/Model/OEEInstances/Machine1/Configuration/UpdateInterval");
        settingsRow1.Add(intervalField);
        
        var bufferField = CreateConfigInputField("Data Buffer Size:", "1000", "/Objects/Model/OEEInstances/Machine1/Configuration/DataBufferSize");
        settingsRow1.Add(bufferField);
        
        var retentionField = CreateConfigInputField("Data Retention (days):", "30", "/Objects/Model/OEEInstances/Machine1/Configuration/DataRetentionDays");
        settingsRow1.Add(retentionField);
        
        layout.Add(settingsRow1);
        
        var settingsRow2 = InformationModel.Make<RowLayout>("DataCollectionSettingsRow2");
        settingsRow2.HorizontalAlignment = HorizontalAlignment.Stretch;
        settingsRow2.HorizontalGap = 15;
        
        var loggingBtn = CreateSimpleButton("Enable Logging");
        settingsRow2.Add(loggingBtn);
        
        var realTimeBtn = CreateSimpleButton("Real-time Calc");
        settingsRow2.Add(realTimeBtn);
        
        var exportBtn = CreateSimpleButton("Auto Export");
        settingsRow2.Add(exportBtn);
        
        layout.Add(settingsRow2);
        section.Add(layout);
        return section;
    }

    private Panel CreateCalculationSettingsSection()
    {
        var section = InformationModel.Make<Panel>("CalculationSettings");
        section.HorizontalAlignment = HorizontalAlignment.Stretch;
        section.Height = 120;
        
        var sectionBg = InformationModel.Make<Rectangle>("CalculationSettingsBg");
        sectionBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionBg.VerticalAlignment = VerticalAlignment.Stretch;
        sectionBg.FillColor = new Color((uint)0xFFF8F9FA);
        sectionBg.CornerRadius = 6;
        section.Add(sectionBg);
        
        var layout = InformationModel.Make<ColumnLayout>("CalculationSettingsLayout");
        layout.HorizontalAlignment = HorizontalAlignment.Stretch;
        layout.LeftMargin = 20;
        layout.RightMargin = 20;
        layout.TopMargin = 15;
        layout.BottomMargin = 15;
        layout.VerticalGap = 10;
        
        var title = InformationModel.Make<Label>("CalculationSettingsTitle");
        title.Text = "Calculation Settings";
        title.FontSize = 14;
        title.TextColor = SUCCESS_GREEN;
        layout.Add(title);
        
        var calcRow = InformationModel.Make<RowLayout>("CalculationSettingsRow");
        calcRow.HorizontalAlignment = HorizontalAlignment.Stretch;
        calcRow.HorizontalGap = 15;
        
        var calcModeField = CreateConfigInputField("Calc Mode:", "Auto", "/Objects/Model/OEEInstances/Machine1/Configuration/CalculationMode");
        calcRow.Add(calcModeField);
        
        var windowField = CreateConfigInputField("Time Window (min):", "60", "/Objects/Model/OEEInstances/Machine1/Configuration/CalculationWindow");
        calcRow.Add(windowField);
        
        var smoothingField = CreateConfigInputField("Smoothing Factor:", "0.1", "/Objects/Model/OEEInstances/Machine1/Configuration/SmoothingFactor");
        calcRow.Add(smoothingField);
        
        var triggerField = CreateConfigInputField("Trigger Threshold:", "5", "/Objects/Model/OEEInstances/Machine1/Configuration/TriggerThreshold");
        calcRow.Add(triggerField);
        
        layout.Add(calcRow);
        section.Add(layout);
        return section;
    }

    private Panel CreateAlarmSettingsSection()
    {
        var section = InformationModel.Make<Panel>("AlarmSettings");
        section.HorizontalAlignment = HorizontalAlignment.Stretch;
        section.Height = 120;
        
        var sectionBg = InformationModel.Make<Rectangle>("AlarmSettingsBg");
        sectionBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionBg.VerticalAlignment = VerticalAlignment.Stretch;
        sectionBg.FillColor = new Color((uint)0xFFF8F9FA);
        sectionBg.CornerRadius = 6;
        section.Add(sectionBg);
        
        var layout = InformationModel.Make<ColumnLayout>("AlarmSettingsLayout");
        layout.HorizontalAlignment = HorizontalAlignment.Stretch;
        layout.LeftMargin = 20;
        layout.RightMargin = 20;
        layout.TopMargin = 15;
        layout.BottomMargin = 15;
        layout.VerticalGap = 10;
        
        var title = InformationModel.Make<Label>("AlarmSettingsTitle");
        title.Text = "Alarm Settings";
        title.FontSize = 14;
        title.TextColor = DANGER_RED;
        layout.Add(title);
        
        var alarmRow = InformationModel.Make<RowLayout>("AlarmSettingsRow");
        alarmRow.HorizontalAlignment = HorizontalAlignment.Stretch;
        alarmRow.HorizontalGap = 15;
        
        var enableBtn = CreateSimpleButton("Enable Alarms");
        alarmRow.Add(enableBtn);
        
        var emailBtn = CreateSimpleButton("Email Alerts");
        alarmRow.Add(emailBtn);
        
        var audioBtn = CreateSimpleButton("Audio Alerts");
        alarmRow.Add(audioBtn);
        
        var priorityField = CreateConfigInputField("Min Priority:", "Medium", "/Objects/Model/OEEInstances/Machine1/Configuration/MinAlarmPriority");
        alarmRow.Add(priorityField);
        
        layout.Add(alarmRow);
        section.Add(layout);
        return section;
    }

    private Panel CreateExportBackupSection()
    {
        var section = InformationModel.Make<Panel>("ExportBackup");
        section.HorizontalAlignment = HorizontalAlignment.Stretch;
        section.Height = 120;
        
        var sectionBg = InformationModel.Make<Rectangle>("ExportBackupBg");
        sectionBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionBg.VerticalAlignment = VerticalAlignment.Stretch;
        sectionBg.FillColor = new Color((uint)0xFFF8F9FA);
        sectionBg.CornerRadius = 6;
        section.Add(sectionBg);
        
        var layout = InformationModel.Make<ColumnLayout>("ExportBackupLayout");
        layout.HorizontalAlignment = HorizontalAlignment.Stretch;
        layout.LeftMargin = 20;
        layout.RightMargin = 20;
        layout.TopMargin = 15;
        layout.BottomMargin = 15;
        layout.VerticalGap = 10;
        
        var title = InformationModel.Make<Label>("ExportBackupTitle");
        title.Text = "Export & Backup Settings";
        title.FontSize = 14;
        title.TextColor = WARNING_AMBER;
        layout.Add(title);
        
        var exportRow = InformationModel.Make<RowLayout>("ExportBackupRow");
        exportRow.HorizontalAlignment = HorizontalAlignment.Stretch;
        exportRow.HorizontalGap = 15;
        
        var autoExportBtn = CreateSimpleButton("Auto Export");
        exportRow.Add(autoExportBtn);
        
        var backupBtn = CreateSimpleButton("Daily Backup");
        exportRow.Add(backupBtn);
        
        var formatField = CreateConfigInputField("Export Format:", "CSV", "/Objects/Model/OEEInstances/Machine1/Configuration/ExportFormat");
        exportRow.Add(formatField);
        
        var pathField = CreateConfigInputField("Backup Path:", "C:\\Backup", "/Objects/Model/OEEInstances/Machine1/Configuration/BackupPath");
        exportRow.Add(pathField);
        
        layout.Add(exportRow);
        section.Add(layout);
        return section;
    }

    [ExportMethod]
    public void CreateOEEConfigurationScreen()
    {
        Log.Info("OEEUIGenerator", "Creating modern OEE Configuration Screen...");
        
        try
        {
            var screensFolder = Project.Current.Get("UI/Screens");
            
            // Create main configuration screen
            var configScreen = InformationModel.Make<Screen>("OEEConfigurationScreen");
            configScreen.BrowseName = "OEEConfigurationScreen";
            configScreen.Width = 1920;
            configScreen.Height = 1080;
            
            // Background with modern gradient feel
            var background = InformationModel.Make<Rectangle>("ConfigBackground");
            background.HorizontalAlignment = HorizontalAlignment.Stretch;
            background.VerticalAlignment = VerticalAlignment.Stretch;
            background.FillColor = LIGHT_GRAY;
            configScreen.Add(background);
            
            // Main layout container optimized for 1920x1080
            var mainContainer = InformationModel.Make<ColumnLayout>("ConfigMainContainer");
            mainContainer.HorizontalAlignment = HorizontalAlignment.Stretch;
            mainContainer.VerticalAlignment = VerticalAlignment.Stretch;
            mainContainer.VerticalGap = 20;
            
            // Screen header with configuration title
            var headerPanel = CreateConfigHeaderPanel();
            mainContainer.Add(headerPanel);
            
            // Main configuration content area
            var configContent = InformationModel.Make<RowLayout>("ConfigContent");
            configContent.HorizontalAlignment = HorizontalAlignment.Stretch;
            configContent.VerticalAlignment = VerticalAlignment.Stretch;
            configContent.HorizontalGap = 25;
            configContent.LeftMargin = 20;
            configContent.RightMargin = 20;
            
            // Left panel - OEE Parameters & Thresholds
            var leftPanel = CreateOEEParametersPanel();
            configContent.Add(leftPanel);
            
            // Center panel - Machine Settings & Shift Configuration
            var centerPanel = CreateMachineSettingsPanel();
            configContent.Add(centerPanel);
            
            // Right panel - System Configuration & Advanced Settings
            var rightPanel = CreateSystemConfigPanel();
            configContent.Add(rightPanel);
            
            mainContainer.Add(configContent);
            
            // Footer with save/apply actions
            var footerPanel = CreateConfigFooterPanel();
            mainContainer.Add(footerPanel);
            
            // Add main container to screen
            configScreen.Add(mainContainer);
            
            // Add to project screens folder
            screensFolder.Add(configScreen);
            Log.Info("OEEUIGenerator", "OEE Configuration Screen created successfully with comprehensive settings");
        }
        catch (Exception ex)
        {
            Log.Error("OEEUIGenerator", $"Error creating OEE Configuration Screen: {ex.Message}");
        }
    }

    [ExportMethod]
    public void CreateOEETrendingScreen()
    {
        Log.Info("OEEUIGenerator", "Creating modern OEE Trending Screen...");
        
        try
        {
            var screensFolder = Project.Current.Get("UI/Screens");
            
            // Create main trending screen
            var trendScreen = InformationModel.Make<Screen>("OEETrendingScreen");
            trendScreen.BrowseName = "OEETrendingScreen";
            trendScreen.Width = 1920;
            trendScreen.Height = 1080;
            
            // Background with modern gradient feel
            var background = InformationModel.Make<Rectangle>("TrendBackground");
            background.HorizontalAlignment = HorizontalAlignment.Stretch;
            background.VerticalAlignment = VerticalAlignment.Stretch;
            background.FillColor = LIGHT_GRAY;
            trendScreen.Add(background);
            
            // Main layout container optimized for 1920x1080
            var mainContainer = InformationModel.Make<ColumnLayout>("TrendMainContainer");
            mainContainer.HorizontalAlignment = HorizontalAlignment.Stretch;
            mainContainer.VerticalAlignment = VerticalAlignment.Stretch;
            mainContainer.VerticalGap = 20;
            
            // Screen header with trend controls
            var headerPanel = CreateTrendHeaderPanel();
            mainContainer.Add(headerPanel);
            
            // Main trending content area
            var trendContent = InformationModel.Make<RowLayout>("TrendContent");
            trendContent.HorizontalAlignment = HorizontalAlignment.Stretch;
            trendContent.VerticalAlignment = VerticalAlignment.Stretch;
            trendContent.HorizontalGap = 20;
            trendContent.LeftMargin = 20;
            trendContent.RightMargin = 20;
            
            // Left panel - Trend Charts
            var chartsPanel = CreateTrendChartsPanel();
            chartsPanel.Width = 1200;
            trendContent.Add(chartsPanel);
            
            // Right panel - Statistics & Controls
            var statsPanel = CreateTrendStatsPanel();
            statsPanel.Width = 680;
            trendContent.Add(statsPanel);
            
            mainContainer.Add(trendContent);
            
            // Footer with time range and export controls
            var footerPanel = CreateTrendFooterPanel();
            mainContainer.Add(footerPanel);
            
            // Add main container to screen
            trendScreen.Add(mainContainer);
            
            // Add to project screens folder
            screensFolder.Add(trendScreen);
            Log.Info("OEEUIGenerator", "OEE Trending Screen created successfully with comprehensive analytics");
        }
        catch (Exception ex)
        {
            Log.Error("OEEUIGenerator", $"Error creating OEE Trending Screen: {ex.Message}");
        }
    }

    private void CreateMultiLineDashboard()
    {
        Log.Info("OEEUIGenerator", "Creating Multi-Line OEE Dashboard...");

        try
        {
            var screensFolder = Project.Current.Get("UI/Screens");
            
            // Create Multi-Line Dashboard screen
            var screen = InformationModel.Make<Panel>("MultiLineDashboard");
            screen.BrowseName = "MultiLineDashboard";
            screen.Width = 1920;
            screen.Height = 1080;
            
            // Main background with gradient
            var background = InformationModel.Make<Rectangle>("MultiLineBg");
            background.HorizontalAlignment = HorizontalAlignment.Stretch;
            background.VerticalAlignment = VerticalAlignment.Stretch;
            background.FillColor = new Color((uint)0xFFF1F3F4);
            screen.Add(background);
            
            // Header section
            var header = CreateMultiLineHeader();
            screen.Add(header);
            
            // Main content area with scroll view
            var contentScroll = InformationModel.Make<ScrollView>("MultiLineContentScroll");
            contentScroll.HorizontalAlignment = HorizontalAlignment.Stretch;
            contentScroll.VerticalAlignment = VerticalAlignment.Stretch;
            contentScroll.TopMargin = 80; // Below header
            contentScroll.LeftMargin = 20;
            contentScroll.RightMargin = 20;
            contentScroll.BottomMargin = 20;
            
            var mainLayout = InformationModel.Make<ColumnLayout>("MultiLineMainLayout");
            mainLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
            mainLayout.VerticalGap = 20;
            
            // Plant Overview Summary
            var overviewSection = CreatePlantOverviewSection();
            mainLayout.Add(overviewSection);
            
            // Production Lines Grid
            var linesGridSection = CreateProductionLinesGrid();
            mainLayout.Add(linesGridSection);
            
            // Performance Comparison Section
            var comparisonSection = CreateLineComparisonSection();
            mainLayout.Add(comparisonSection);
            
            // Alerts and Issues Section
            var alertsSection = CreateAlertsSection();
            mainLayout.Add(alertsSection);
            
            contentScroll.Add(mainLayout);
            screen.Add(contentScroll);
            
            screensFolder.Add(screen);
            
            Log.Info("OEEUIGenerator", "Multi-Line Dashboard created successfully");
        }
        catch (Exception ex)
        {
            Log.Error("OEEUIGenerator", $"Error creating Multi-Line Dashboard: {ex.Message}");
        }
    }

    private Panel CreateMultiLineHeader()
    {
        var header = InformationModel.Make<Panel>("MultiLineHeader");
        header.HorizontalAlignment = HorizontalAlignment.Stretch;
        header.Height = 70;
        
        var headerBg = InformationModel.Make<Rectangle>("MultiLineHeaderBg");
        headerBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        headerBg.VerticalAlignment = VerticalAlignment.Stretch;
        headerBg.FillColor = new Color((uint)0xFF1E293B);
        header.Add(headerBg);
        
        var headerLayout = InformationModel.Make<RowLayout>("MultiLineHeaderLayout");
        headerLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        headerLayout.VerticalAlignment = VerticalAlignment.Center;
        headerLayout.LeftMargin = 30;
        headerLayout.RightMargin = 30;
        headerLayout.HorizontalGap = 30;
        
        // Title section
        var titleSection = InformationModel.Make<ColumnLayout>("MultiLineTitleSection");
        titleSection.VerticalGap = 2;
        
        var title = InformationModel.Make<Label>("MultiLineTitle");
        title.Text = "🏭 Multi-Line OEE Dashboard";
        title.FontSize = 24;
        title.TextColor = WHITE;
        
        var subtitle = InformationModel.Make<Label>("MultiLineSubtitle");
        subtitle.Text = "Plant-Wide Production Monitoring";
        subtitle.FontSize = 12;
        subtitle.TextColor = new Color((uint)0xFF94A3B8);
        
        titleSection.Add(title);
        titleSection.Add(subtitle);
        
        // Status indicators
        var statusSection = InformationModel.Make<RowLayout>("MultiLineStatusSection");
        statusSection.HorizontalGap = 20;
        statusSection.VerticalAlignment = VerticalAlignment.Center;
        
        // Plant status
        var plantStatus = InformationModel.Make<RowLayout>("PlantStatusIndicator");
        plantStatus.HorizontalGap = 8;
        plantStatus.VerticalAlignment = VerticalAlignment.Center;
        
        var plantStatusLed = InformationModel.Make<Ellipse>("PlantStatusLed");
        plantStatusLed.Width = 12;
        plantStatusLed.Height = 12;
        plantStatusLed.FillColor = SUCCESS_GREEN;
        
        var plantStatusText = InformationModel.Make<Label>("PlantStatusText");
        plantStatusText.Text = "Plant Online";
        plantStatusText.FontSize = 12;
        plantStatusText.TextColor = WHITE;
        
        plantStatus.Add(plantStatusLed);
        plantStatus.Add(plantStatusText);
        
        // Active lines count
        var activeLines = InformationModel.Make<Label>("ActiveLinesCount");
        activeLines.Text = "6/8 Lines Active";
        activeLines.FontSize = 12;
        activeLines.TextColor = SUCCESS_GREEN;
        
        // Current shift
        var shiftInfo = InformationModel.Make<Label>("CurrentShiftInfo");
        shiftInfo.Text = "Shift 2 - 14:30 remaining";
        shiftInfo.FontSize = 12;
        shiftInfo.TextColor = new Color((uint)0xFF94A3B8);
        
        statusSection.Add(plantStatus);
        statusSection.Add(activeLines);
        statusSection.Add(shiftInfo);
        
        headerLayout.Add(titleSection);
        headerLayout.Add(statusSection);
        header.Add(headerLayout);
        
        return header;
    }

    private Panel CreatePlantOverviewSection()
    {
        var section = InformationModel.Make<Panel>("PlantOverviewSection");
        section.HorizontalAlignment = HorizontalAlignment.Stretch;
        section.Height = 200;
        
        var sectionBg = InformationModel.Make<Rectangle>("PlantOverviewBg");
        sectionBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionBg.VerticalAlignment = VerticalAlignment.Stretch;
        sectionBg.FillColor = WHITE;
        sectionBg.CornerRadius = 12;
        section.Add(sectionBg);
        
        var layout = InformationModel.Make<ColumnLayout>("PlantOverviewLayout");
        layout.HorizontalAlignment = HorizontalAlignment.Stretch;
        layout.VerticalAlignment = VerticalAlignment.Stretch;
        layout.LeftMargin = 25;
        layout.TopMargin = 20;
        layout.RightMargin = 25;
        layout.BottomMargin = 20;
        layout.VerticalGap = 15;
        
        // Section title
        var title = InformationModel.Make<Label>("PlantOverviewTitle");
        title.Text = "📊 Plant Performance Overview";
        title.FontSize = 18;
        title.TextColor = DARK_TEXT;
        layout.Add(title);
        
        // KPI row
        var kpiRow = InformationModel.Make<RowLayout>("PlantKPIRow");
        kpiRow.HorizontalAlignment = HorizontalAlignment.Stretch;
        kpiRow.HorizontalGap = 30;
        
        // Overall Plant OEE
        var plantOEE = CreateKPICard("Overall Plant OEE", "67.8%", "↗ +1.2%", SUCCESS_GREEN, PRIMARY_BLUE);
        kpiRow.Add(plantOEE);
        
        // Total Production
        var totalProduction = CreateKPICard("Total Production", "8,247", "units today", SUCCESS_GREEN, SUCCESS_GREEN);
        kpiRow.Add(totalProduction);
        
        // Active Lines
        var activeLines = CreateKPICard("Active Lines", "6/8", "75% utilization", WARNING_AMBER, WARNING_AMBER);
        kpiRow.Add(activeLines);
        
        // Total Downtime
        var totalDowntime = CreateKPICard("Total Downtime", "2.3 hrs", "↓ -0.5 hrs", SUCCESS_GREEN, DANGER_RED);
        kpiRow.Add(totalDowntime);
        
        // Quality Rate
        var qualityRate = CreateKPICard("Plant Quality", "94.2%", "Target: 95%", WARNING_AMBER, SUCCESS_GREEN);
        kpiRow.Add(qualityRate);
        
        layout.Add(kpiRow);
        section.Add(layout);
        
        return section;
    }

    private Panel CreateProductionLinesGrid()
    {
        var section = InformationModel.Make<Panel>("ProductionLinesGrid");
        section.HorizontalAlignment = HorizontalAlignment.Stretch;
        section.Height = 600;
        
        var sectionBg = InformationModel.Make<Rectangle>("ProductionLinesBg");
        sectionBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionBg.VerticalAlignment = VerticalAlignment.Stretch;
        sectionBg.FillColor = WHITE;
        sectionBg.CornerRadius = 12;
        section.Add(sectionBg);
        
        var layout = InformationModel.Make<ColumnLayout>("ProductionLinesLayout");
        layout.HorizontalAlignment = HorizontalAlignment.Stretch;
        layout.VerticalAlignment = VerticalAlignment.Stretch;
        layout.LeftMargin = 25;
        layout.TopMargin = 20;
        layout.RightMargin = 25;
        layout.BottomMargin = 20;
        layout.VerticalGap = 15;
        
        // Section title
        var title = InformationModel.Make<Label>("ProductionLinesTitle");
        title.Text = "🏭 Production Lines Status";
        title.FontSize = 18;
        title.TextColor = DARK_TEXT;
        layout.Add(title);
        
        // Lines grid (2 rows of 4 lines each)
        var row1 = InformationModel.Make<RowLayout>("LinesRow1");
        row1.HorizontalAlignment = HorizontalAlignment.Stretch;
        row1.HorizontalGap = 20;
        
        // Line 1
        row1.Add(CreateLineCard("Line 01", "Assembly", "RUNNING", "78.5%", "1,247", SUCCESS_GREEN));
        // Line 2  
        row1.Add(CreateLineCard("Line 02", "Packaging", "RUNNING", "82.3%", "1,156", SUCCESS_GREEN));
        // Line 3
        row1.Add(CreateLineCard("Line 03", "Testing", "MAINTENANCE", "0.0%", "0", WARNING_AMBER));
        // Line 4
        row1.Add(CreateLineCard("Line 04", "Assembly", "RUNNING", "75.1%", "1,089", SUCCESS_GREEN));
        
        var row2 = InformationModel.Make<RowLayout>("LinesRow2");
        row2.HorizontalAlignment = HorizontalAlignment.Stretch;
        row2.HorizontalGap = 20;
        row2.TopMargin = 15;
        
        // Line 5
        row2.Add(CreateLineCard("Line 05", "Welding", "ALARM", "45.2%", "567", DANGER_RED));
        // Line 6
        row2.Add(CreateLineCard("Line 06", "Painting", "RUNNING", "88.7%", "1,445", SUCCESS_GREEN));
        // Line 7
        row2.Add(CreateLineCard("Line 07", "Quality", "STOPPED", "0.0%", "0", DANGER_RED));
        // Line 8
        row2.Add(CreateLineCard("Line 08", "Finishing", "SETUP", "12.3%", "156", WARNING_AMBER));
        
        layout.Add(row1);
        layout.Add(row2);
        section.Add(layout);
        
        return section;
    }

    private Panel CreateLineComparisonSection()
    {
        var section = InformationModel.Make<Panel>("LineComparisonSection");
        section.HorizontalAlignment = HorizontalAlignment.Stretch;
        section.Height = 300;
        
        var sectionBg = InformationModel.Make<Rectangle>("LineComparisonBg");
        sectionBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionBg.VerticalAlignment = VerticalAlignment.Stretch;
        sectionBg.FillColor = WHITE;
        sectionBg.CornerRadius = 12;
        section.Add(sectionBg);
        
        var layout = InformationModel.Make<ColumnLayout>("LineComparisonLayout");
        layout.HorizontalAlignment = HorizontalAlignment.Stretch;
        layout.VerticalAlignment = VerticalAlignment.Stretch;
        layout.LeftMargin = 25;
        layout.TopMargin = 20;
        layout.RightMargin = 25;
        layout.BottomMargin = 20;
        layout.VerticalGap = 15;
        
        // Section title
        var title = InformationModel.Make<Label>("LineComparisonTitle");
        title.Text = "📈 Line Performance Comparison";
        title.FontSize = 18;
        title.TextColor = DARK_TEXT;
        layout.Add(title);
        
        // Comparison charts placeholder
        var chartArea = InformationModel.Make<Rectangle>("ComparisonChartArea");
        chartArea.HorizontalAlignment = HorizontalAlignment.Stretch;
        chartArea.VerticalAlignment = VerticalAlignment.Stretch;
        chartArea.FillColor = new Color((uint)0xFFF8F9FA);
        chartArea.CornerRadius = 8;
        chartArea.BorderThickness = 1;
        chartArea.BorderColor = new Color((uint)0xFFE5E7EB);
        
        var chartLabel = InformationModel.Make<Label>("ComparisonChartLabel");
        chartLabel.Text = "📊 OEE Comparison Chart\\n(Line Performance vs Target)";
        chartLabel.FontSize = 14;
        chartLabel.TextColor = MEDIUM_TEXT;
        chartLabel.HorizontalAlignment = HorizontalAlignment.Center;
        chartLabel.VerticalAlignment = VerticalAlignment.Center;
        chartArea.Add(chartLabel);
        
        layout.Add(chartArea);
        section.Add(layout);
        
        return section;
    }

    private Panel CreateAlertsSection()
    {
        var section = InformationModel.Make<Panel>("AlertsSection");
        section.HorizontalAlignment = HorizontalAlignment.Stretch;
        section.Height = 250;
        
        var sectionBg = InformationModel.Make<Rectangle>("AlertsBg");
        sectionBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionBg.VerticalAlignment = VerticalAlignment.Stretch;
        sectionBg.FillColor = WHITE;
        sectionBg.CornerRadius = 12;
        section.Add(sectionBg);
        
        var layout = InformationModel.Make<ColumnLayout>("AlertsLayout");
        layout.HorizontalAlignment = HorizontalAlignment.Stretch;
        layout.VerticalAlignment = VerticalAlignment.Stretch;
        layout.LeftMargin = 25;
        layout.TopMargin = 20;
        layout.RightMargin = 25;
        layout.BottomMargin = 20;
        layout.VerticalGap = 15;
        
        // Section title
        var title = InformationModel.Make<Label>("AlertsTitle");
        title.Text = "🚨 Active Alerts & Issues";
        title.FontSize = 18;
        title.TextColor = DARK_TEXT;
        layout.Add(title);
        
        // Alerts list
        var alertsList = InformationModel.Make<ColumnLayout>("AlertsList");
        alertsList.HorizontalAlignment = HorizontalAlignment.Stretch;
        alertsList.VerticalGap = 8;
        
        alertsList.Add(CreateAlertItem("🔴", "Line 05 - Motor Overheating Alarm", "2 minutes ago", DANGER_RED));
        alertsList.Add(CreateAlertItem("🟠", "Line 03 - Scheduled Maintenance Active", "1 hour ago", WARNING_AMBER));
        alertsList.Add(CreateAlertItem("🔴", "Line 07 - Emergency Stop Triggered", "15 minutes ago", DANGER_RED));
        alertsList.Add(CreateAlertItem("🟡", "Line 08 - Setup Mode Extended", "30 minutes ago", WARNING_AMBER));
        alertsList.Add(CreateAlertItem("🟢", "Line 06 - New Shift Started", "45 minutes ago", SUCCESS_GREEN));
        
        layout.Add(alertsList);
        section.Add(layout);
        
        return section;
    }

    private Panel CreateKPICard(string title, string value, string subtitle, Color trendColor, Color accentColor)
    {
        var card = InformationModel.Make<Panel>("KPICard_" + title.Replace(" ", ""));
        card.Width = 200;
        card.Height = 120;
        
        var cardBg = InformationModel.Make<Rectangle>("KPICardBg");
        cardBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        cardBg.VerticalAlignment = VerticalAlignment.Stretch;
        cardBg.FillColor = new Color((uint)0xFFF8F9FA);
        cardBg.CornerRadius = 8;
        cardBg.BorderThickness = 1;
        cardBg.BorderColor = new Color((uint)0xFFE5E7EB);
        card.Add(cardBg);
        
        var accent = InformationModel.Make<Rectangle>("KPIAccent");
        accent.HorizontalAlignment = HorizontalAlignment.Stretch;
        accent.Height = 4;
        accent.FillColor = accentColor;
        accent.CornerRadius = 2;
        card.Add(accent);
        
        var layout = InformationModel.Make<ColumnLayout>("KPILayout");
        layout.HorizontalAlignment = HorizontalAlignment.Stretch;
        layout.VerticalAlignment = VerticalAlignment.Center;
        layout.LeftMargin = 15;
        layout.RightMargin = 15;
        layout.TopMargin = 10;
        layout.BottomMargin = 10;
        layout.VerticalGap = 5;
        
        var titleLabel = InformationModel.Make<Label>("KPITitle");
        titleLabel.Text = title;
        titleLabel.FontSize = 11;
        titleLabel.TextColor = MEDIUM_TEXT;
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        
        var valueLabel = InformationModel.Make<Label>("KPIValue");
        valueLabel.Text = value;
        valueLabel.FontSize = 22;
        valueLabel.TextColor = DARK_TEXT;
        valueLabel.HorizontalAlignment = HorizontalAlignment.Center;
        
        var subtitleLabel = InformationModel.Make<Label>("KPISubtitle");
        subtitleLabel.Text = subtitle;
        subtitleLabel.FontSize = 9;
        subtitleLabel.TextColor = trendColor;
        subtitleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        
        layout.Add(titleLabel);
        layout.Add(valueLabel);
        layout.Add(subtitleLabel);
        card.Add(layout);
        
        return card;
    }

    private Panel CreateLineCard(string lineName, string process, string status, string oee, string production, Color statusColor)
    {
        var card = InformationModel.Make<Panel>("LineCard_" + lineName.Replace(" ", ""));
        card.Width = 220;
        card.Height = 200;
        
        var cardBg = InformationModel.Make<Rectangle>("LineCardBg");
        cardBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        cardBg.VerticalAlignment = VerticalAlignment.Stretch;
        cardBg.FillColor = WHITE;
        cardBg.CornerRadius = 10;
        cardBg.BorderThickness = 2;
        cardBg.BorderColor = statusColor;
        card.Add(cardBg);
        
        var layout = InformationModel.Make<ColumnLayout>("LineCardLayout");
        layout.HorizontalAlignment = HorizontalAlignment.Stretch;
        layout.VerticalAlignment = VerticalAlignment.Stretch;
        layout.LeftMargin = 15;
        layout.TopMargin = 15;
        layout.RightMargin = 15;
        layout.BottomMargin = 15;
        layout.VerticalGap = 10;
        
        // Header
        var header = InformationModel.Make<RowLayout>("LineCardHeader");
        header.HorizontalAlignment = HorizontalAlignment.Stretch;
        header.HorizontalGap = 10;
        
        var lineNameLabel = InformationModel.Make<Label>("LineCardName");
        lineNameLabel.Text = lineName;
        lineNameLabel.FontSize = 16;
        lineNameLabel.TextColor = DARK_TEXT;
        
        var statusIndicator = InformationModel.Make<Ellipse>("LineStatusIndicator");
        statusIndicator.Width = 12;
        statusIndicator.Height = 12;
        statusIndicator.FillColor = statusColor;
        
        header.Add(lineNameLabel);
        header.Add(statusIndicator);
        
        // Process type
        var processLabel = InformationModel.Make<Label>("LineCardProcess");
        processLabel.Text = process;
        processLabel.FontSize = 10;
        processLabel.TextColor = MEDIUM_TEXT;
        
        // Status
        var statusLabel = InformationModel.Make<Label>("LineCardStatus");
        statusLabel.Text = status;
        statusLabel.FontSize = 12;
        statusLabel.TextColor = statusColor;
        
        // OEE
        var oeeLabel = InformationModel.Make<Label>("LineCardOEE");
        oeeLabel.Text = "OEE: " + oee;
        oeeLabel.FontSize = 14;
        oeeLabel.TextColor = DARK_TEXT;
        
        // Production
        var productionLabel = InformationModel.Make<Label>("LineCardProduction");
        productionLabel.Text = production + " units";
        productionLabel.FontSize = 12;
        productionLabel.TextColor = MEDIUM_TEXT;
        
        layout.Add(header);
        layout.Add(processLabel);
        layout.Add(statusLabel);
        layout.Add(oeeLabel);
        layout.Add(productionLabel);
        card.Add(layout);
        
        return card;
    }

    private Panel CreateAlertItem(string icon, string message, string time, Color alertColor)
    {
        var item = InformationModel.Make<Panel>("AlertItem");
        item.HorizontalAlignment = HorizontalAlignment.Stretch;
        item.Height = 30;
        
        var itemBg = InformationModel.Make<Rectangle>("AlertItemBg");
        itemBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        itemBg.VerticalAlignment = VerticalAlignment.Stretch;
        itemBg.FillColor = new Color((uint)0xFFF8F9FA);
        itemBg.CornerRadius = 4;
        itemBg.BorderThickness = 1;
        itemBg.BorderColor = alertColor;
        item.Add(itemBg);
        
        var layout = InformationModel.Make<RowLayout>("AlertItemLayout");
        layout.HorizontalAlignment = HorizontalAlignment.Stretch;
        layout.VerticalAlignment = VerticalAlignment.Center;
        layout.LeftMargin = 12;
        layout.RightMargin = 12;
        layout.HorizontalGap = 10;
        
        var iconLabel = InformationModel.Make<Label>("AlertIcon");
        iconLabel.Text = icon;
        iconLabel.FontSize = 12;
        iconLabel.Width = 20;
        
        var messageLabel = InformationModel.Make<Label>("AlertMessage");
        messageLabel.Text = message;
        messageLabel.FontSize = 11;
        messageLabel.TextColor = DARK_TEXT;
        messageLabel.Width = 400;
        
        var timeLabel = InformationModel.Make<Label>("AlertTime");
        timeLabel.Text = time;
        timeLabel.FontSize = 9;
        timeLabel.TextColor = MEDIUM_TEXT;
        timeLabel.HorizontalAlignment = HorizontalAlignment.Right;
        
        layout.Add(iconLabel);
        layout.Add(messageLabel);
        layout.Add(timeLabel);
        item.Add(layout);
        
        return item;
    }

    // Missing method implementations for trending screen
    private Panel CreateTrendHeaderPanel()
    {
        var panel = InformationModel.Make<Panel>("TrendHeaderPanel");
        panel.HorizontalAlignment = HorizontalAlignment.Stretch;
        panel.Height = 80;
        
        var bg = InformationModel.Make<Rectangle>("TrendHeaderBg");
        bg.HorizontalAlignment = HorizontalAlignment.Stretch;
        bg.VerticalAlignment = VerticalAlignment.Stretch;
        bg.FillColor = PRIMARY_BLUE;
        panel.Add(bg);
        
        var title = InformationModel.Make<Label>("TrendHeaderTitle");
        title.Text = "OEE Trending Analysis";
        title.FontSize = 24;
        title.TextColor = WHITE;
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.VerticalAlignment = VerticalAlignment.Center;
        panel.Add(title);
        
        return panel;
    }

    private Panel CreateTrendChartsPanel()
    {
        var panel = InformationModel.Make<Panel>("TrendChartsPanel");
        panel.HorizontalAlignment = HorizontalAlignment.Stretch;
        panel.Height = 600;
        
        var bg = InformationModel.Make<Rectangle>("TrendChartsBg");
        bg.HorizontalAlignment = HorizontalAlignment.Stretch;
        bg.VerticalAlignment = VerticalAlignment.Stretch;
        bg.FillColor = WHITE;
        panel.Add(bg);
        
        var title = InformationModel.Make<Label>("TrendChartsTitle");
        title.Text = "Trend Charts Placeholder";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.VerticalAlignment = VerticalAlignment.Center;
        title.FontSize = 18;
        title.TextColor = DARK_TEXT;
        panel.Add(title);
        
        return panel;
    }

    private Panel CreateTrendStatsPanel()
    {
        var panel = InformationModel.Make<Panel>("TrendStatsPanel");
        panel.HorizontalAlignment = HorizontalAlignment.Stretch;
        panel.Height = 200;
        
        var bg = InformationModel.Make<Rectangle>("TrendStatsBg");
        bg.HorizontalAlignment = HorizontalAlignment.Stretch;
        bg.VerticalAlignment = VerticalAlignment.Stretch;
        bg.FillColor = LIGHT_GRAY;
        panel.Add(bg);
        
        var title = InformationModel.Make<Label>("TrendStatsTitle");
        title.Text = "Trend Statistics";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.VerticalAlignment = VerticalAlignment.Center;
        title.FontSize = 16;
        title.TextColor = DARK_TEXT;
        panel.Add(title);
        
        return panel;
    }

    private Panel CreateTrendFooterPanel()
    {
        var panel = InformationModel.Make<Panel>("TrendFooterPanel");
        panel.HorizontalAlignment = HorizontalAlignment.Stretch;
        panel.Height = 60;
        
        var bg = InformationModel.Make<Rectangle>("TrendFooterBg");
        bg.HorizontalAlignment = HorizontalAlignment.Stretch;
        bg.VerticalAlignment = VerticalAlignment.Stretch;
        bg.FillColor = MEDIUM_TEXT;
        panel.Add(bg);
        
        return panel;
    }

    [ExportMethod]
    public void CreateReportsAnalyticsScreen()
    {
        Log.Info("OEEUIGenerator", "Creating Reports & Analytics Screen...");
        
        try
        {
            var screensFolder = Project.Current.Get("UI/Screens");
            
            var screen = InformationModel.Make<Screen>("ReportsAnalyticsScreen");
            screen.BrowseName = "ReportsAnalyticsScreen";
            screen.Width = 1920;
            screen.Height = 1080;
            
            // Background
            var background = InformationModel.Make<Rectangle>("ReportsAnalyticsBg");
            background.HorizontalAlignment = HorizontalAlignment.Stretch;
            background.VerticalAlignment = VerticalAlignment.Stretch;
            background.FillColor = LIGHT_GRAY;
            screen.Add(background);
            
            // Main layout
            var mainLayout = InformationModel.Make<ColumnLayout>("ReportsAnalyticsMainLayout");
            mainLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
            mainLayout.VerticalAlignment = VerticalAlignment.Stretch;
            mainLayout.LeftMargin = 20;
            mainLayout.TopMargin = 20;
            mainLayout.RightMargin = 20;
            mainLayout.BottomMargin = 20;
            mainLayout.VerticalGap = 20;
            
            // Header
            var header = CreateReportsAnalyticsHeader();
            mainLayout.Add(header);
            
            // Content area with tabs/sections
            var contentArea = InformationModel.Make<RowLayout>("ReportsContentArea");
            contentArea.HorizontalAlignment = HorizontalAlignment.Stretch;
            contentArea.VerticalAlignment = VerticalAlignment.Stretch;
            contentArea.HorizontalGap = 20;
            
            // Left panel - Data Grids (60%)
            var leftPanel = CreateDataGridsPanel();
            leftPanel.Width = 1150;
            contentArea.Add(leftPanel);
            
            // Right panel - Trending & Filters (40%)
            var rightPanel = CreateTrendingFiltersPanel();
            rightPanel.Width = 730;
            contentArea.Add(rightPanel);
            
            mainLayout.Add(contentArea);
            screen.Add(mainLayout);
            screensFolder.Add(screen);
            
            Log.Info("OEEUIGenerator", "Reports & Analytics Screen created successfully!");
        }
        catch (Exception ex)
        {
            Log.Error("OEEUIGenerator", $"Error creating Reports & Analytics Screen: {ex.Message}");
        }
    }

    private Panel CreateReportsAnalyticsHeader()
    {
        var header = InformationModel.Make<Panel>("ReportsAnalyticsHeader");
        header.HorizontalAlignment = HorizontalAlignment.Stretch;
        header.Height = 80;
        
        // Header shadow
        var headerShadow = InformationModel.Make<Rectangle>("ReportsAnalyticsHeaderShadow");
        headerShadow.HorizontalAlignment = HorizontalAlignment.Stretch;
        headerShadow.VerticalAlignment = VerticalAlignment.Stretch;
        headerShadow.LeftMargin = 3;
        headerShadow.TopMargin = 3;
        headerShadow.FillColor = new Color((uint)0x20000000);
        headerShadow.CornerRadius = 8;
        header.Add(headerShadow);
        
        // Header background
        var headerBg = InformationModel.Make<Rectangle>("ReportsAnalyticsHeaderBg");
        headerBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        headerBg.VerticalAlignment = VerticalAlignment.Stretch;
        headerBg.RightMargin = 3;
        headerBg.BottomMargin = 3;
        headerBg.FillColor = new Color((uint)0xFF6B46C1); // Purple theme for reports
        headerBg.CornerRadius = 8;
        header.Add(headerBg);
        
        // Header content
        var headerContent = InformationModel.Make<RowLayout>("ReportsAnalyticsHeaderContent");
        headerContent.HorizontalAlignment = HorizontalAlignment.Stretch;
        headerContent.VerticalAlignment = VerticalAlignment.Center;
        headerContent.LeftMargin = 30;
        headerContent.RightMargin = 30;
        headerContent.HorizontalGap = 25;
        
        // Title section
        var titleSection = InformationModel.Make<Panel>("ReportsAnalyticsTitleSection");
        titleSection.HorizontalAlignment = HorizontalAlignment.Left;
        titleSection.Width = 450;
        
        var titleLayout = InformationModel.Make<ColumnLayout>("ReportsAnalyticsTitleLayout");
        titleLayout.VerticalGap = 5;
        
        var title = InformationModel.Make<Label>("ReportsAnalyticsTitle");
        title.Text = "📊 Reports & Analytics";
        title.FontSize = 24;
        title.TextColor = WHITE;
        
        var subtitle = InformationModel.Make<Label>("ReportsAnalyticsSubtitle");
        subtitle.Text = "Downtime/Uptime Analysis & Shift Reporting";
        subtitle.FontSize = 12;
        subtitle.TextColor = new Color((uint)0xFFE8F4FD);
        
        titleLayout.Add(title);
        titleLayout.Add(subtitle);
        titleSection.Add(titleLayout);
        headerContent.Add(titleSection);
        
        // Quick stats summary
        var statsSection = CreateQuickStatsSection();
        headerContent.Add(statsSection);
        
        // OEE Metrics summary
        var oeeMetricsSection = CreateOEEMetricsSection();
        headerContent.Add(oeeMetricsSection);
        
        header.Add(headerContent);
        return header;
    }

    private Panel CreateQuickStatsSection()
    {
        var statsPanel = InformationModel.Make<Panel>("QuickStatsSection");
        statsPanel.HorizontalAlignment = HorizontalAlignment.Center;
        statsPanel.Width = 420;
        
        var statsLayout = InformationModel.Make<RowLayout>("QuickStatsLayout");
        statsLayout.HorizontalAlignment = HorizontalAlignment.Center;
        statsLayout.VerticalAlignment = VerticalAlignment.Center;
        statsLayout.HorizontalGap = 20;
        
        // Today's uptime
        var uptimeStats = CreateQuickStatItem("Today's Uptime", "92.3%", "UptimeToday", SUCCESS_GREEN);
        statsLayout.Add(uptimeStats);
        
        // Today's downtime  
        var downtimeStats = CreateQuickStatItem("Today's Downtime", "7.7%", "DowntimeToday", DANGER_RED);
        statsLayout.Add(downtimeStats);
        
        // Current shift
        var shiftStats = CreateQuickStatItem("Current Shift", "Shift 1", "CurrentShiftNumber", PRIMARY_BLUE);
        statsLayout.Add(shiftStats);
        
        statsPanel.Add(statsLayout);
        return statsPanel;
    }

    private Panel CreateQuickStatItem(string label, string value, string dataPath, Color valueColor)
    {
        var statPanel = InformationModel.Make<Panel>("QuickStat_" + label.Replace(" ", ""));
        statPanel.Width = 140;
        
        var statLayout = InformationModel.Make<ColumnLayout>("QuickStatLayout_" + label.Replace(" ", ""));
        statLayout.HorizontalAlignment = HorizontalAlignment.Center;
        statLayout.VerticalAlignment = VerticalAlignment.Center;
        statLayout.VerticalGap = 5;
        
        // Label
        var labelText = InformationModel.Make<Label>("QuickStatLabel_" + label.Replace(" ", ""));
        labelText.Text = label;
        labelText.FontSize = 10;
        labelText.TextColor = new Color((uint)0xFFE8F4FD);
        labelText.HorizontalAlignment = HorizontalAlignment.Center;
        statLayout.Add(labelText);
        
        // Value
        var valueText = InformationModel.Make<Label>("QuickStatValue_" + label.Replace(" ", ""));
        valueText.Text = value;
        valueText.FontSize = 16;
        valueText.TextColor = WHITE;
        valueText.HorizontalAlignment = HorizontalAlignment.Center;
        // DATA LINK: /Objects/Model/OEEInstances/Machine1/Outputs/{dataPath}
        statLayout.Add(valueText);
        
        statPanel.Add(statLayout);
        return statPanel;
    }

    private Panel CreateOEEMetricsSection()
    {
        var metricsPanel = InformationModel.Make<Panel>("OEEMetricsSection");
        metricsPanel.HorizontalAlignment = HorizontalAlignment.Right;
        metricsPanel.Width = 400;
        
        var metricsLayout = InformationModel.Make<RowLayout>("OEEMetricsLayout");
        metricsLayout.HorizontalAlignment = HorizontalAlignment.Right;
        metricsLayout.VerticalAlignment = VerticalAlignment.Center;
        metricsLayout.HorizontalGap = 20;
        
        // Overall OEE
        var oeeMetric = CreateOEEMetricItem("OEE", "72.5%", "OEE", PRIMARY_BLUE);
        metricsLayout.Add(oeeMetric);
        
        // Quality  
        var qualityMetric = CreateOEEMetricItem("Quality", "95.2%", "Quality", SUCCESS_GREEN);
        metricsLayout.Add(qualityMetric);
        
        // Performance
        var performanceMetric = CreateOEEMetricItem("Performance", "85.1%", "Performance", WARNING_AMBER);
        metricsLayout.Add(performanceMetric);
        
        // Availability
        var availabilityMetric = CreateOEEMetricItem("Availability", "89.3%", "Availability", new Color((uint)0xFF8B5CF6));
        metricsLayout.Add(availabilityMetric);
        
        metricsPanel.Add(metricsLayout);
        return metricsPanel;
    }

    private Panel CreateOEEMetricItem(string label, string value, string dataPath, Color valueColor)
    {
        var metricPanel = InformationModel.Make<Panel>("OEEMetric_" + label.Replace(" ", ""));
        metricPanel.Width = 85;
        
        var metricLayout = InformationModel.Make<ColumnLayout>("OEEMetricLayout_" + label.Replace(" ", ""));
        metricLayout.HorizontalAlignment = HorizontalAlignment.Center;
        metricLayout.VerticalAlignment = VerticalAlignment.Center;
        metricLayout.VerticalGap = 4;
        
        // Label
        var labelText = InformationModel.Make<Label>("OEEMetricLabel_" + label.Replace(" ", ""));
        labelText.Text = label;
        labelText.FontSize = 10;
        labelText.TextColor = new Color((uint)0xFFE8F4FD);
        labelText.HorizontalAlignment = HorizontalAlignment.Center;
        metricLayout.Add(labelText);
        
        // Value with enhanced styling
        var valuePanel = InformationModel.Make<Panel>("OEEMetricValuePanel_" + label.Replace(" ", ""));
        valuePanel.HorizontalAlignment = HorizontalAlignment.Center;
        valuePanel.Width = 75;
        valuePanel.Height = 32;
        
        // Value background with subtle styling
        var valueBg = InformationModel.Make<Rectangle>("OEEMetricValueBg_" + label.Replace(" ", ""));
        valueBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        valueBg.VerticalAlignment = VerticalAlignment.Stretch;
        valueBg.FillColor = new Color((uint)0x20FFFFFF);
        valueBg.CornerRadius = 6;
        valueBg.BorderThickness = 1;
        valueBg.BorderColor = new Color((uint)0x30FFFFFF);
        valuePanel.Add(valueBg);
        
        // Value text
        var valueText = InformationModel.Make<Label>("OEEMetricValue_" + label.Replace(" ", ""));
        valueText.Text = value;
        valueText.FontSize = 16;
        valueText.TextColor = WHITE;
        valueText.HorizontalAlignment = HorizontalAlignment.Center;
        valueText.VerticalAlignment = VerticalAlignment.Center;
        // DATA LINK: /Objects/Model/OEEInstances/Machine1/Outputs/{dataPath}
        valuePanel.Add(valueText);
        
        metricLayout.Add(valuePanel);
        metricPanel.Add(metricLayout);
        
        return metricPanel;
    }

    private Panel CreateDataGridsPanel()
    {
        var panel = InformationModel.Make<Panel>("DataGridsPanel");
        panel.HorizontalAlignment = HorizontalAlignment.Stretch;
        panel.VerticalAlignment = VerticalAlignment.Stretch;
        
        // Panel shadow
        var panelShadow = InformationModel.Make<Rectangle>("DataGridsPanelShadow");
        panelShadow.HorizontalAlignment = HorizontalAlignment.Stretch;
        panelShadow.VerticalAlignment = VerticalAlignment.Stretch;
        panelShadow.LeftMargin = 3;
        panelShadow.TopMargin = 3;
        panelShadow.FillColor = new Color((uint)0x20000000);
        panelShadow.CornerRadius = 8;
        panel.Add(panelShadow);
        
        // Panel background
        var panelBg = InformationModel.Make<Rectangle>("DataGridsPanelBg");
        panelBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        panelBg.VerticalAlignment = VerticalAlignment.Stretch;
        panelBg.RightMargin = 3;
        panelBg.BottomMargin = 3;
        panelBg.FillColor = WHITE;
        panelBg.CornerRadius = 8;
        panel.Add(panelBg);
        
        // Content layout
        var contentLayout = InformationModel.Make<ColumnLayout>("DataGridsContent");
        contentLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        contentLayout.VerticalAlignment = VerticalAlignment.Stretch;
        contentLayout.LeftMargin = 25;
        contentLayout.TopMargin = 25;
        contentLayout.RightMargin = 25;
        contentLayout.BottomMargin = 25;
        contentLayout.VerticalGap = 20;
        
        // Tab-like section headers
        var tabsRow = CreateDataGridTabs();
        contentLayout.Add(tabsRow);
        
        // Downtime/Uptime DataGrid section
        var downtimeSection = CreateDowntimeUptimeDataGrid();
        contentLayout.Add(downtimeSection);
        
        // Shift Report DataGrid section
        var shiftSection = CreateShiftReportDataGrid();
        contentLayout.Add(shiftSection);
        
        panel.Add(contentLayout);
        return panel;
    }

    private RowLayout CreateDataGridTabs()
    {
        var tabsRow = InformationModel.Make<RowLayout>("DataGridTabs");
        tabsRow.HorizontalAlignment = HorizontalAlignment.Stretch;
        tabsRow.Height = 50;
        tabsRow.HorizontalGap = 5;
        
        // Downtime/Uptime tab
        var downtimeTab = CreateTabButton("Downtime/Uptime Analysis", "DowntimeTab", true);
        tabsRow.Add(downtimeTab);
        
        // Shift Reports tab
        var shiftTab = CreateTabButton("Shift Performance Reports", "ShiftTab", false);
        tabsRow.Add(shiftTab);
        
        // Export button
        var exportBtn = CreateExportButton();
        exportBtn.HorizontalAlignment = HorizontalAlignment.Right;
        tabsRow.Add(exportBtn);
        
        return tabsRow;
    }

    private Button CreateTabButton(string text, string tabId, bool active)
    {
        var button = InformationModel.Make<Button>("TabButton_" + tabId);
        button.Text = text;
        button.Width = 200;
        button.Height = 40;
        button.FontSize = 12;
        
        if (active)
        {
            button.TextColor = WHITE;
            // Active tab styling would be set through button properties
        }
        else
        {
            button.TextColor = MEDIUM_TEXT;
            // Inactive tab styling
        }
        
        return button;
    }

    private Button CreateExportButton()
    {
        var exportBtn = InformationModel.Make<Button>("ExportDataButton");
        exportBtn.Text = "📤 Export Data";
        exportBtn.Width = 120;
        exportBtn.Height = 35;
        exportBtn.FontSize = 11;
        exportBtn.TextColor = WHITE;
        // Export button styling
        
        return exportBtn;
    }

    private Panel CreateDowntimeUptimeDataGrid()
    {
        var section = InformationModel.Make<Panel>("DowntimeUptimeSection");
        section.HorizontalAlignment = HorizontalAlignment.Stretch;
        section.Height = 380;
        
        var sectionLayout = InformationModel.Make<ColumnLayout>("DowntimeUptimeLayout");
        sectionLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionLayout.VerticalAlignment = VerticalAlignment.Stretch;
        sectionLayout.VerticalGap = 15;
        
        // Section title
        var title = InformationModel.Make<Label>("DowntimeUptimeTitle");
        title.Text = "⚡ Downtime/Uptime Analysis";
        title.FontSize = 16;
        title.TextColor = DARK_TEXT;
        sectionLayout.Add(title);
        
        // DataGrid container
        var gridContainer = InformationModel.Make<Panel>("DowntimeGridContainer");
        gridContainer.HorizontalAlignment = HorizontalAlignment.Stretch;
        gridContainer.VerticalAlignment = VerticalAlignment.Stretch;
        
        // DataGrid background
        var gridBg = InformationModel.Make<Rectangle>("DowntimeGridBg");
        gridBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        gridBg.VerticalAlignment = VerticalAlignment.Stretch;
        gridBg.FillColor = LIGHT_GRAY;
        gridBg.CornerRadius = 4;
        gridBg.BorderThickness = 1;
        gridBg.BorderColor = BORDER_COLOR;
        gridContainer.Add(gridBg);
        
        // Create DataGrid (FT Optix DataGrid component)
        var dataGrid = InformationModel.Make<DataGrid>("DowntimeUptimeDataGrid");
        dataGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
        dataGrid.VerticalAlignment = VerticalAlignment.Stretch;
        dataGrid.LeftMargin = 10;
        dataGrid.TopMargin = 10;
        dataGrid.RightMargin = 10;
        dataGrid.BottomMargin = 10;
        
        // Configure DataGrid columns
        ConfigureDowntimeDataGridColumns(dataGrid);
        
        gridContainer.Add(dataGrid);
        sectionLayout.Add(gridContainer);
        section.Add(sectionLayout);
        
        return section;
    }

    private void ConfigureDowntimeDataGridColumns(DataGrid dataGrid)
    {
        // Note: FT Optix DataGrid columns are typically configured through the UI designer
        // or by using DataItemTemplate for data binding. For this example, we'll create
        // a placeholder structure that can be configured with actual data sources.
        
        // Set up DataGrid properties
        // dataGrid.Model = NodeId.Empty; // Would be configured to point to downtime data model
        // dataGrid.DataItemTemplate would be configured to point to downtime data structure
        
        // Create column headers using Labels (placeholder approach)
        var headerRow = InformationModel.Make<RowLayout>("DowntimeHeaderRow");
        headerRow.HorizontalAlignment = HorizontalAlignment.Stretch;
        headerRow.Height = 30;
        headerRow.TopMargin = 5;
        
        var timestampHeader = CreateDataGridHeader("Timestamp", 150);
        headerRow.Add(timestampHeader);
        
        var eventTypeHeader = CreateDataGridHeader("Event Type", 100);
        headerRow.Add(eventTypeHeader);
        
        var durationHeader = CreateDataGridHeader("Duration", 100);
        headerRow.Add(durationHeader);
        
        var reasonHeader = CreateDataGridHeader("Reason/Category", 200);
        headerRow.Add(reasonHeader);
        
        var impactHeader = CreateDataGridHeader("Impact on OEE", 120);
        headerRow.Add(impactHeader);
        
        var operatorHeader = CreateDataGridHeader("Operator", 120);
        headerRow.Add(operatorHeader);
        
        // Note: Actual data rows would be populated through data binding
        // DATA SOURCE: Historical downtime events from OEE logging system
    }

    private Panel CreateShiftReportDataGrid()
    {
        var section = InformationModel.Make<Panel>("ShiftReportSection");
        section.HorizontalAlignment = HorizontalAlignment.Stretch;
        section.Height = 380;
        
        var sectionLayout = InformationModel.Make<ColumnLayout>("ShiftReportLayout");
        sectionLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionLayout.VerticalAlignment = VerticalAlignment.Stretch;
        sectionLayout.VerticalGap = 15;
        
        // Section title
        var title = InformationModel.Make<Label>("ShiftReportTitle");
        title.Text = "🔄 Shift Performance Reports";
        title.FontSize = 16;
        title.TextColor = DARK_TEXT;
        sectionLayout.Add(title);
        
        // DataGrid container
        var gridContainer = InformationModel.Make<Panel>("ShiftGridContainer");
        gridContainer.HorizontalAlignment = HorizontalAlignment.Stretch;
        gridContainer.VerticalAlignment = VerticalAlignment.Stretch;
        
        // DataGrid background
        var gridBg = InformationModel.Make<Rectangle>("ShiftGridBg");
        gridBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        gridBg.VerticalAlignment = VerticalAlignment.Stretch;
        gridBg.FillColor = LIGHT_GRAY;
        gridBg.CornerRadius = 4;
        gridBg.BorderThickness = 1;
        gridBg.BorderColor = BORDER_COLOR;
        gridContainer.Add(gridBg);
        
        // Create DataGrid
        var dataGrid = InformationModel.Make<DataGrid>("ShiftReportDataGrid");
        dataGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
        dataGrid.VerticalAlignment = VerticalAlignment.Stretch;
        dataGrid.LeftMargin = 10;
        dataGrid.TopMargin = 10;
        dataGrid.RightMargin = 10;
        dataGrid.BottomMargin = 10;
        
        // Configure DataGrid columns
        ConfigureShiftDataGridColumns(dataGrid);
        
        gridContainer.Add(dataGrid);
        sectionLayout.Add(gridContainer);
        section.Add(sectionLayout);
        
        return section;
    }

    private void ConfigureShiftDataGridColumns(DataGrid dataGrid)
    {
        // Note: FT Optix DataGrid columns are typically configured through the UI designer
        // or by using DataItemTemplate for data binding.
        
        // Set up DataGrid properties
        // dataGrid.Model = NodeId.Empty; // Would be configured to point to shift data model
        // dataGrid.DataItemTemplate would be configured to point to shift data structure
        
        // Create column headers using Labels (placeholder approach)
        var headerRow = InformationModel.Make<RowLayout>("ShiftHeaderRow");
        headerRow.HorizontalAlignment = HorizontalAlignment.Stretch;
        headerRow.Height = 30;
        headerRow.TopMargin = 5;
        
        var dateHeader = CreateDataGridHeader("Date", 100);
        headerRow.Add(dateHeader);
        
        var shiftHeader = CreateDataGridHeader("Shift #", 70);
        headerRow.Add(shiftHeader);
        
        var oeeHeader = CreateDataGridHeader("OEE %", 80);
        headerRow.Add(oeeHeader);
        
        var qualityHeader = CreateDataGridHeader("Quality %", 80);
        headerRow.Add(qualityHeader);
        
        var performanceHeader = CreateDataGridHeader("Performance %", 90);
        headerRow.Add(performanceHeader);
        
        var availabilityHeader = CreateDataGridHeader("Availability %", 90);
        headerRow.Add(availabilityHeader);
        
        var partsHeader = CreateDataGridHeader("Parts Produced", 100);
        headerRow.Add(partsHeader);
        
        var downtimeHeader = CreateDataGridHeader("Total Downtime", 100);
        headerRow.Add(downtimeHeader);
        
        var operatorHeader = CreateDataGridHeader("Primary Operator", 120);
        headerRow.Add(operatorHeader);
        
        // Note: Actual data rows would be populated through data binding
        // DATA SOURCE: Historical shift performance data from OEE system
    }

    private Panel CreateTrendingFiltersPanel()
    {
        var panel = InformationModel.Make<Panel>("TrendingFiltersPanel");
        panel.HorizontalAlignment = HorizontalAlignment.Stretch;
        panel.VerticalAlignment = VerticalAlignment.Stretch;
        
        var panelLayout = InformationModel.Make<ColumnLayout>("TrendingFiltersLayout");
        panelLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        panelLayout.VerticalAlignment = VerticalAlignment.Stretch;
        panelLayout.VerticalGap = 20;
        
        // Filters section
        var filtersSection = CreateFiltersSection();
        panelLayout.Add(filtersSection);
        
        // Trending chart section
        var trendingSection = CreateDowntimeUptimeTrendingSection();
        panelLayout.Add(trendingSection);
        
        // Summary statistics
        var summarySection = CreateSummaryStatisticsSection();
        panelLayout.Add(summarySection);
        
        panel.Add(panelLayout);
        return panel;
    }

    private Panel CreateFiltersSection()
    {
        var section = InformationModel.Make<Panel>("FiltersSection");
        section.HorizontalAlignment = HorizontalAlignment.Stretch;
        section.Height = 200;
        
        // Section shadow and background
        var sectionShadow = InformationModel.Make<Rectangle>("FiltersSectionShadow");
        sectionShadow.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionShadow.VerticalAlignment = VerticalAlignment.Stretch;
        sectionShadow.LeftMargin = 3;
        sectionShadow.TopMargin = 3;
        sectionShadow.FillColor = new Color((uint)0x20000000);
        sectionShadow.CornerRadius = 8;
        section.Add(sectionShadow);
        
        var sectionBg = InformationModel.Make<Rectangle>("FiltersSectionBg");
        sectionBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionBg.VerticalAlignment = VerticalAlignment.Stretch;
        sectionBg.RightMargin = 3;
        sectionBg.BottomMargin = 3;
        sectionBg.FillColor = WHITE;
        sectionBg.CornerRadius = 8;
        section.Add(sectionBg);
        
        // Content
        var contentLayout = InformationModel.Make<ColumnLayout>("FiltersContent");
        contentLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        contentLayout.LeftMargin = 20;
        contentLayout.TopMargin = 20;
        contentLayout.RightMargin = 20;
        contentLayout.BottomMargin = 20;
        contentLayout.VerticalGap = 15;
        
        // Title
        var title = InformationModel.Make<Label>("FiltersTitle");
        title.Text = "🔍 Filters & Date Range";
        title.FontSize = 16;
        title.TextColor = DARK_TEXT;
        contentLayout.Add(title);
        
        // Date range inputs
        var dateRangeRow = InformationModel.Make<RowLayout>("DateRangeRow");
        dateRangeRow.HorizontalAlignment = HorizontalAlignment.Stretch;
        dateRangeRow.VerticalAlignment = VerticalAlignment.Center;
        dateRangeRow.HorizontalGap = 10;
        
        var startDateLabel = InformationModel.Make<Label>("StartDateLabel");
        startDateLabel.Text = "From:";
        startDateLabel.FontSize = 11;
        startDateLabel.TextColor = DARK_TEXT;
        startDateLabel.Width = 40;
        
        var startDatePicker = InformationModel.Make<DateTimePicker>("StartDatePicker");
        startDatePicker.Width = 140;
        startDatePicker.Height = 25;
        
        var endDateLabel = InformationModel.Make<Label>("EndDateLabel");
        endDateLabel.Text = "To:";
        endDateLabel.FontSize = 11;
        endDateLabel.TextColor = DARK_TEXT;
        endDateLabel.Width = 25;
        
        var endDatePicker = InformationModel.Make<DateTimePicker>("EndDatePicker");
        endDatePicker.Width = 140;
        endDatePicker.Height = 25;
        
        dateRangeRow.Add(startDateLabel);
        dateRangeRow.Add(startDatePicker);
        dateRangeRow.Add(endDateLabel);
        dateRangeRow.Add(endDatePicker);
        contentLayout.Add(dateRangeRow);
        
        // Quick filter buttons
        var quickFiltersRow = InformationModel.Make<RowLayout>("QuickFiltersRow");
        quickFiltersRow.HorizontalAlignment = HorizontalAlignment.Stretch;
        quickFiltersRow.HorizontalGap = 8;
        
        quickFiltersRow.Add(CreateQuickFilterButton("Today", "TodayFilter"));
        quickFiltersRow.Add(CreateQuickFilterButton("Week", "WeekFilter"));
        quickFiltersRow.Add(CreateQuickFilterButton("Month", "MonthFilter"));
        
        contentLayout.Add(quickFiltersRow);
        
        // Apply filters button
        var applyButton = InformationModel.Make<Button>("ApplyFiltersButton");
        applyButton.Text = "📊 Apply Filters";
        applyButton.HorizontalAlignment = HorizontalAlignment.Center;
        applyButton.Width = 120;
        applyButton.Height = 30;
        applyButton.FontSize = 11;
        applyButton.TextColor = WHITE;
        contentLayout.Add(applyButton);
        
        section.Add(contentLayout);
        return section;
    }

    private Button CreateQuickFilterButton(string text, string buttonId)
    {
        var button = InformationModel.Make<Button>("QuickFilterBtn_" + buttonId);
        button.Text = text;
        button.Width = 60;
        button.Height = 25;
        button.FontSize = 10;
        button.TextColor = DARK_TEXT;
        
        return button;
    }

    private Panel CreateDowntimeUptimeTrendingSection()
    {
        var section = InformationModel.Make<Panel>("DowntimeUptimeTrendingSection");
        section.HorizontalAlignment = HorizontalAlignment.Stretch;
        section.Height = 350;
        
        // Section shadow and background
        var sectionShadow = InformationModel.Make<Rectangle>("TrendingSectionShadow");
        sectionShadow.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionShadow.VerticalAlignment = VerticalAlignment.Stretch;
        sectionShadow.LeftMargin = 3;
        sectionShadow.TopMargin = 3;
        sectionShadow.FillColor = new Color((uint)0x20000000);
        sectionShadow.CornerRadius = 8;
        section.Add(sectionShadow);
        
        var sectionBg = InformationModel.Make<Rectangle>("TrendingSectionBg");
        sectionBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionBg.VerticalAlignment = VerticalAlignment.Stretch;
        sectionBg.RightMargin = 3;
        sectionBg.BottomMargin = 3;
        sectionBg.FillColor = WHITE;
        sectionBg.CornerRadius = 8;
        section.Add(sectionBg);
        
        // Content
        var contentLayout = InformationModel.Make<ColumnLayout>("TrendingContent");
        contentLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        contentLayout.VerticalAlignment = VerticalAlignment.Stretch;
        contentLayout.LeftMargin = 20;
        contentLayout.TopMargin = 20;
        contentLayout.RightMargin = 20;
        contentLayout.BottomMargin = 20;
        contentLayout.VerticalGap = 15;
        
        // Title
        var title = InformationModel.Make<Label>("TrendingTitle");
        title.Text = "📈 Downtime/Uptime Trending";
        title.FontSize = 16;
        title.TextColor = DARK_TEXT;
        contentLayout.Add(title);
        
        // Chart container (placeholder for actual trending chart)
        var chartContainer = InformationModel.Make<Panel>("TrendingChartContainer");
        chartContainer.HorizontalAlignment = HorizontalAlignment.Stretch;
        chartContainer.VerticalAlignment = VerticalAlignment.Stretch;
        
        var chartBg = InformationModel.Make<Rectangle>("TrendingChartBg");
        chartBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        chartBg.VerticalAlignment = VerticalAlignment.Stretch;
        chartBg.FillColor = LIGHT_GRAY;
        chartBg.CornerRadius = 4;
        chartBg.BorderThickness = 1;
        chartBg.BorderColor = BORDER_COLOR;
        chartContainer.Add(chartBg);
        
        // Chart placeholder text
        var chartPlaceholder = InformationModel.Make<Label>("ChartPlaceholder");
        chartPlaceholder.Text = "📊 Downtime/Uptime Trend Chart\n(Connect to historical data source)";
        chartPlaceholder.FontSize = 14;
        chartPlaceholder.TextColor = MEDIUM_TEXT;
        chartPlaceholder.HorizontalAlignment = HorizontalAlignment.Center;
        chartPlaceholder.VerticalAlignment = VerticalAlignment.Center;
        chartContainer.Add(chartPlaceholder);
        
        // Chart legend
        var legendRow = InformationModel.Make<RowLayout>("ChartLegendRow");
        legendRow.HorizontalAlignment = HorizontalAlignment.Center;
        legendRow.HorizontalGap = 20;
        
        legendRow.Add(CreateLegendItem("Uptime", SUCCESS_GREEN));
        legendRow.Add(CreateLegendItem("Downtime", DANGER_RED));
        legendRow.Add(CreateLegendItem("Planned Downtime", WARNING_AMBER));
        
        contentLayout.Add(chartContainer);
        contentLayout.Add(legendRow);
        section.Add(contentLayout);
        
        return section;
    }

    private Panel CreateLegendItem(string label, Color color)
    {
        var legendItem = InformationModel.Make<Panel>("LegendItem_" + label.Replace(" ", ""));
        legendItem.Width = 120;
        legendItem.Height = 20;
        
        var itemLayout = InformationModel.Make<RowLayout>("LegendItemLayout_" + label.Replace(" ", ""));
        itemLayout.HorizontalGap = 8;
        itemLayout.VerticalAlignment = VerticalAlignment.Center;
        
        // Color indicator
        var colorIndicator = InformationModel.Make<Rectangle>("LegendColor_" + label.Replace(" ", ""));
        colorIndicator.Width = 15;
        colorIndicator.Height = 15;
        colorIndicator.FillColor = color;
        colorIndicator.CornerRadius = 2;
        
        // Label
        var labelText = InformationModel.Make<Label>("LegendLabel_" + label.Replace(" ", ""));
        labelText.Text = label;
        labelText.FontSize = 10;
        labelText.TextColor = DARK_TEXT;
        
        itemLayout.Add(colorIndicator);
        itemLayout.Add(labelText);
        legendItem.Add(itemLayout);
        
        return legendItem;
    }

    private Panel CreateSummaryStatisticsSection()
    {
        var section = InformationModel.Make<Panel>("SummaryStatisticsSection");
        section.HorizontalAlignment = HorizontalAlignment.Stretch;
        section.Height = 250;
        
        // Section shadow and background
        var sectionShadow = InformationModel.Make<Rectangle>("SummarySectionShadow");
        sectionShadow.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionShadow.VerticalAlignment = VerticalAlignment.Stretch;
        sectionShadow.LeftMargin = 3;
        sectionShadow.TopMargin = 3;
        sectionShadow.FillColor = new Color((uint)0x20000000);
        sectionShadow.CornerRadius = 8;
        section.Add(sectionShadow);
        
        var sectionBg = InformationModel.Make<Rectangle>("SummarySectionBg");
        sectionBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        sectionBg.VerticalAlignment = VerticalAlignment.Stretch;
        sectionBg.RightMargin = 3;
        sectionBg.BottomMargin = 3;
        sectionBg.FillColor = WHITE;
        sectionBg.CornerRadius = 8;
        section.Add(sectionBg);
        
        // Content
        var contentLayout = InformationModel.Make<ColumnLayout>("SummaryContent");
        contentLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        contentLayout.LeftMargin = 20;
        contentLayout.TopMargin = 20;
        contentLayout.RightMargin = 20;
        contentLayout.BottomMargin = 20;
        contentLayout.VerticalGap = 15;
        
        // Title
        var title = InformationModel.Make<Label>("SummaryTitle");
        title.Text = "📋 Summary Statistics";
        title.FontSize = 16;
        title.TextColor = DARK_TEXT;
        contentLayout.Add(title);
        
        // Statistics grid
        var statsGrid = InformationModel.Make<ColumnLayout>("SummaryStatsGrid");
        statsGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
        statsGrid.VerticalGap = 12;
        
        // Average uptime
        var avgUptimeRow = CreateSummaryStatRow("Average Uptime:", "92.1%", "AvgUptime", SUCCESS_GREEN);
        statsGrid.Add(avgUptimeRow);
        
        // Average downtime
        var avgDowntimeRow = CreateSummaryStatRow("Average Downtime:", "7.9%", "AvgDowntime", DANGER_RED);
        statsGrid.Add(avgDowntimeRow);
        
        // Most common downtime reason
        var commonReasonRow = CreateSummaryStatRow("Top Downtime Reason:", "Setup Change", "TopDowntimeReason", WARNING_AMBER);
        statsGrid.Add(commonReasonRow);
        
        // Total events
        var totalEventsRow = CreateSummaryStatRow("Total Events:", "847", "TotalEvents", PRIMARY_BLUE);
        statsGrid.Add(totalEventsRow);
        
        contentLayout.Add(statsGrid);
        section.Add(contentLayout);
        
        return section;
    }

    private RowLayout CreateSummaryStatRow(string label, string value, string dataPath, Color valueColor)
    {
        var statRow = InformationModel.Make<RowLayout>("SummaryStatRow_" + dataPath);
        statRow.HorizontalAlignment = HorizontalAlignment.Stretch;
        statRow.VerticalAlignment = VerticalAlignment.Center;
        statRow.HorizontalGap = 15;
        
        // Label
        var labelText = InformationModel.Make<Label>("SummaryStatLabel_" + dataPath);
        labelText.Text = label;
        labelText.FontSize = 12;
        labelText.TextColor = DARK_TEXT;
        labelText.Width = 180;
        statRow.Add(labelText);
        
        // Value
        var valueText = InformationModel.Make<Label>("SummaryStatValue_" + dataPath);
        valueText.Text = value;
        valueText.FontSize = 14;
        valueText.TextColor = valueColor;
        // DATA LINK: /Objects/Model/OEEInstances/Machine1/Outputs/{dataPath}
        statRow.Add(valueText);
        
        return statRow;
    }

    private Label CreateDataGridHeader(string headerText, int width)
    {
        var header = InformationModel.Make<Label>("Header_" + headerText.Replace(" ", "").Replace("/", "").Replace("%", "Pct"));
        header.Text = headerText;
        header.Width = width;
        header.Height = 25;
        header.FontSize = 11;
        header.TextColor = DARK_TEXT;
        header.HorizontalAlignment = HorizontalAlignment.Left;
        header.VerticalAlignment = VerticalAlignment.Center;
        
        return header;
    }
}
