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

public class ModernUIGenerator : BaseNetLogic
{
    // Modern color scheme - Professional blue/teal theme
    private readonly uint PrimaryBlue = 0xFF1E3A8A;      // Deep blue
    private readonly uint AccentTeal = 0xFF0D9488;       // Teal accent
    private readonly uint LightBlue = 0xFF3B82F6;        // Light blue
    private readonly uint SuccessGreen = 0xFF10B981;     // Success green
    private readonly uint WarningOrange = 0xFFF59E0B;    // Warning orange
    private readonly uint DangerRed = 0xFFDC2626;        // Danger red
    private readonly uint SurfaceGray = 0xFFF8FAFC;      // Surface gray
    private readonly uint TextDark = 0xFF1F2937;         // Dark text
    private readonly uint TextLight = 0xFF9CA3AF;        // Light text
    private readonly uint BorderGray = 0xFFE5E7EB;       // Border gray

    // Widget generator instance for reusable components
    private OEEWidgetGenerator widgetGenerator;

    // Helper method to ensure consistent dashboard folder structure
    private Folder GetDashboardFolder(string categoryName, string screenName)
    {
        var uiFolder = Project.Current.Get("UI");
        
        // Ensure main Dashboards folder exists
        var dashboardsFolder = uiFolder.Get("Dashboards");
        if (dashboardsFolder == null)
        {
            dashboardsFolder = InformationModel.Make<Folder>("Dashboards");
            uiFolder.Add(dashboardsFolder);
        }

        // Ensure category folder exists (e.g., "OEEScreens", "ConfigScreens", etc.)
        var categoryFolder = dashboardsFolder.Get(categoryName);
        if (categoryFolder == null)
        {
            categoryFolder = InformationModel.Make<Folder>(categoryName);
            dashboardsFolder.Add(categoryFolder);
        }

        // Ensure specific screen folder exists
        var specificScreenFolder = categoryFolder.Get(screenName);
        if (specificScreenFolder == null)
        {
            specificScreenFolder = InformationModel.Make<Folder>(screenName);
            categoryFolder.Add(specificScreenFolder);
        }

        return (Folder)specificScreenFolder;
    }

    [ExportMethod]
    public void CreateBothScreens()
    {
        try
        {
            // Initialize widget generator for reusable components
            if (widgetGenerator == null)
            {
                widgetGenerator = new OEEWidgetGenerator();
            }

            Log.Info("ModernUIGenerator", "Ensuring all OEE widgets are available...");
            widgetGenerator.CreateAllOEEWidgets();
            
            Log.Info("ModernUIGenerator", "Creating OEE Dashboard screen using widgets...");
            CreateOEEDashboard();
            Log.Info("ModernUIGenerator", "Dashboard screen created successfully with widgetized components!");
        }
        catch (Exception ex)
        {
            Log.Error("ModernUIGenerator", "Error creating screens: " + ex.Message);
        }
    }

    [ExportMethod]
    public void CreateOEEDashboard()
    {
        try
        {
            Log.Info("ModernUIGenerator", "Creating modern OEE Dashboard...");
            
            // Use organized folder structure: UI/Dashboards/OEEScreens/OEEDashboard/
            var screenFolder = GetDashboardFolder("OEEScreens", "OEEDashboard");

            // Remove existing screen if it exists
            var existingScreen = screenFolder.Get("OEEDashboard");
            if (existingScreen != null)
            {
                existingScreen.Delete();
            }

            // Create main dashboard panel
            var dashboardPanel = InformationModel.Make<Panel>("OEEDashboard");
            dashboardPanel.Width = 1200;
            dashboardPanel.Height = 800;

            // Dashboard background
            var dashboardBg = InformationModel.Make<Rectangle>("DashboardBackground");
            dashboardBg.Width = 1200;
            dashboardBg.Height = 800;
            dashboardBg.FillColor = new Color(SurfaceGray);
            dashboardPanel.Add(dashboardBg);

            // Title section with modern styling
            CreateDashboardTitle(dashboardPanel);

            // OEE Cards section
            CreateOEECards(dashboardPanel);

            // Machine status overview
            CreateMachineStatusOverview(dashboardPanel);

            // Chart placeholders with modern styling
            CreateChartPlaceholders(dashboardPanel);

            screenFolder.Add(dashboardPanel);
            Log.Info("ModernUIGenerator", "OEE Dashboard created successfully in UI/Dashboards/OEEScreens/OEEDashboard/!");
        }
        catch (Exception ex)
        {
            Log.Error("ModernUIGenerator", "Error creating OEE Dashboard: " + ex.Message);
        }
    }

    [ExportMethod]
    public void ClearAllScreens()
    {
        try
        {
            Log.Info("ModernUIGenerator", "Clearing all generated screens...");
            
            var uiFolder = Project.Current.Get("UI");
            var dashboardsFolder = uiFolder?.Get("Dashboards");
            
            if (dashboardsFolder != null)
            {
                // Delete organized dashboard folders
                var categoryFolders = new string[] { 
                    "OEEScreens", 
                    "ConfigScreens",
                    "DataScreens",
                    "MonitoringScreens",
                    "PerformanceScreens"
                };
                
                foreach (var categoryName in categoryFolders)
                {
                    var categoryFolder = dashboardsFolder.Get(categoryName);
                    if (categoryFolder != null)
                    {
                        categoryFolder.Delete();
                        Log.Info("ModernUIGenerator", $"Deleted dashboard category: {categoryName}");
                    }
                }
            }
            
            // Also clean up legacy UI folder screens if they exist
            if (uiFolder != null)
            {
                var legacyScreenNames = new string[] { 
                    "OEEDashboard", 
                    "OEEConfiguration",
                    "ProductionDataEntry",
                    "SystemMonitoring",
                    "TargetPerformance"
                };
                
                foreach (var screenName in legacyScreenNames)
                {
                    var legacyScreen = uiFolder.Get(screenName);
                    if (legacyScreen != null)
                    {
                        legacyScreen.Delete();
                        Log.Info("ModernUIGenerator", $"Deleted legacy screen: {screenName}");
                    }
                }
            }
            
            Log.Info("ModernUIGenerator", "All screens cleared successfully!");
        }
        catch (Exception ex)
        {
            Log.Error("ModernUIGenerator", "Error clearing screens: " + ex.Message);
        }
    }

    [ExportMethod]
    public void RefreshWithModernStyling()
    {
        try
        {
            Log.Info("ModernUIGenerator", "Refreshing UI with modern color scheme...");
            ClearAllScreens();
            System.Threading.Thread.Sleep(500); // Brief pause for clearing
            CreateBothScreens();
            Log.Info("ModernUIGenerator", "Modern styling applied successfully!");
        }
        catch (Exception ex)
        {
            Log.Error("ModernUIGenerator", "Error refreshing styling: " + ex.Message);
        }
    }

    [ExportMethod]
    public void CreateConfigurationScreen()
    {
        try
        {
            Log.Info("ModernUIGenerator", "Creating OEE Configuration screen...");
            
            // Use organized folder structure: UI/Dashboards/ConfigScreens/OEEConfiguration/
            var screenFolder = GetDashboardFolder("ConfigScreens", "OEEConfiguration");

            // Remove existing screen if it exists
            var existingScreen = screenFolder.Get("OEEConfiguration");
            if (existingScreen != null)
            {
                existingScreen.Delete();
            }

            // Create main configuration panel
            var configPanel = InformationModel.Make<Panel>("OEEConfiguration");
            configPanel.Width = 1400;
            configPanel.Height = 900;

            // Configuration background
            var configBg = InformationModel.Make<Rectangle>("ConfigBackground");
            configBg.Width = 1400;
            configBg.Height = 900;
            configBg.FillColor = new Color(SurfaceGray);
            configPanel.Add(configBg);

            // Create configuration sections
            CreateConfigurationHeader(configPanel);
            CreateProductionTargetsConfig(configPanel);
            CreateShiftTimesConfig(configPanel);
            CreateQualityThresholdsConfig(configPanel);
            CreateConfigurationButtons(configPanel);

            screenFolder.Add(configPanel);
            Log.Info("ModernUIGenerator", "OEE Configuration screen created successfully in UI/Dashboards/ConfigScreens/OEEConfiguration/!");
        }
        catch (Exception ex)
        {
            Log.Error("ModernUIGenerator", "Error creating Configuration screen: " + ex.Message);
        }
    }

    [ExportMethod]
    public void CreateProductionDataEntryScreen()
    {
        try
        {
            Log.Info("ModernUIGenerator", "Creating Production Data Entry screen...");
            
            // Use organized folder structure: UI/Dashboards/DataScreens/ProductionDataEntry/
            var screenFolder = GetDashboardFolder("DataScreens", "ProductionDataEntry");

            // Remove existing screen if it exists
            var existingScreen = screenFolder.Get("ProductionDataEntry");
            if (existingScreen != null)
            {
                existingScreen.Delete();
            }

            // Create main data entry panel
            var dataEntryPanel = InformationModel.Make<Panel>("ProductionDataEntry");
            dataEntryPanel.Width = 1400;
            dataEntryPanel.Height = 900;

            // Background
            var dataEntryBg = InformationModel.Make<Rectangle>("DataEntryBackground");
            dataEntryBg.Width = 1400;
            dataEntryBg.Height = 900;
            dataEntryBg.FillColor = new Color(SurfaceGray);
            dataEntryPanel.Add(dataEntryBg);

            // Create data entry sections
            CreateProductionDataHeader(dataEntryPanel);
            CreateRealTimeDataSection(dataEntryPanel);
            CreateProductionCountersSection(dataEntryPanel);

            screenFolder.Add(dataEntryPanel);
            Log.Info("ModernUIGenerator", "Production Data Entry screen created successfully in UI/Dashboards/DataScreens/ProductionDataEntry/!");
        }
        catch (Exception ex)
        {
            Log.Error("ModernUIGenerator", "Error creating Production Data Entry screen: " + ex.Message);
        }
    }

    [ExportMethod]
    public void CreateSystemMonitoringScreen()
    {
        try
        {
            Log.Info("ModernUIGenerator", "Creating System Monitoring screen...");
            
            // Use organized folder structure: UI/Dashboards/MonitoringScreens/SystemMonitoring/
            var screenFolder = GetDashboardFolder("MonitoringScreens", "SystemMonitoring");

            // Remove existing screen if it exists
            var existingScreen = screenFolder.Get("SystemMonitoring");
            if (existingScreen != null)
            {
                existingScreen.Delete();
            }

            // Create main monitoring panel
            var monitoringPanel = InformationModel.Make<Panel>("SystemMonitoring");
            monitoringPanel.Width = 1400;
            monitoringPanel.Height = 900;

            // Background
            var monitoringBg = InformationModel.Make<Rectangle>("MonitoringBackground");
            monitoringBg.Width = 1400;
            monitoringBg.Height = 900;
            monitoringBg.FillColor = new Color(SurfaceGray);
            monitoringPanel.Add(monitoringBg);

            // Create monitoring sections
            CreateSystemMonitoringHeader(monitoringPanel);
            CreateSystemHealthSection(monitoringPanel);
            CreateTrendingSection(monitoringPanel);
            CreateStatisticsSection(monitoringPanel);

            screenFolder.Add(monitoringPanel);
            Log.Info("ModernUIGenerator", "System Monitoring screen created successfully in UI/Dashboards/MonitoringScreens/SystemMonitoring/!");
        }
        catch (Exception ex)
        {
            Log.Error("ModernUIGenerator", "Error creating System Monitoring screen: " + ex.Message);
        }
    }

    [ExportMethod]
    public void CreateTargetPerformanceScreen()
    {
        try
        {
            Log.Info("ModernUIGenerator", "Creating Target Performance screen...");
            
            // Use organized folder structure: UI/Dashboards/PerformanceScreens/TargetPerformance/
            var screenFolder = GetDashboardFolder("PerformanceScreens", "TargetPerformance");

            // Remove existing screen if it exists
            var existingScreen = screenFolder.Get("TargetPerformance");
            if (existingScreen != null)
            {
                existingScreen.Delete();
            }

            // Create main target performance panel
            var targetPanel = InformationModel.Make<Panel>("TargetPerformance");
            targetPanel.Width = 1400;
            targetPanel.Height = 900;

            // Background
            var targetBg = InformationModel.Make<Rectangle>("TargetBackground");
            targetBg.Width = 1400;
            targetBg.Height = 900;
            targetBg.FillColor = new Color(SurfaceGray);
            targetPanel.Add(targetBg);

            // Create target performance sections
            CreateTargetPerformanceHeader(targetPanel);
            CreateTargetVsActualSection(targetPanel);
            CreateProductionPlanningSection(targetPanel);
            CreatePerformanceAlertsSection(targetPanel);

            screenFolder.Add(targetPanel);
            Log.Info("ModernUIGenerator", "Target Performance screen created successfully in UI/Dashboards/PerformanceScreens/TargetPerformance/!");
        }
        catch (Exception ex)
        {
            Log.Error("ModernUIGenerator", "Error creating Target Performance screen: " + ex.Message);
        }
    }

    [ExportMethod]
    public void CreateCompleteOEESystem()
    {
        try
        {
            // Initialize widget generator for reusable components
            if (widgetGenerator == null)
            {
                widgetGenerator = new OEEWidgetGenerator();
            }

            Log.Info("ModernUIGenerator", "Creating complete OEE system with live calculator integration...");
            
            // Ensure all widgets are available
            Log.Info("ModernUIGenerator", "Creating widget library...");
            widgetGenerator.CreateAllOEEWidgets();
            
            // Create OEE data instance and calculator
            Log.Info("ModernUIGenerator", "Setting up OEE calculator and data source...");
            CreateOEEDataSourceAndCalculator();
            
            // Create all screens using widgets
            CreateOEEDashboard();
            CreateConfigurationScreen();
            CreateProductionDataEntryScreen();
            CreateSystemMonitoringScreen();
            CreateTargetPerformanceScreen();
            
            // Connect dashboards to live data
            Log.Info("ModernUIGenerator", "Connecting dashboard widgets to live calculator data...");
            ConnectDashboardToCalculator();
            
            Log.Info("ModernUIGenerator", "Complete OEE system created successfully with live data integration!");
        }
        catch (Exception ex)
        {
            Log.Error("ModernUIGenerator", "Error creating complete OEE system: " + ex.Message);
        }
    }

    // Implementation methods (private)
    private void CreateDashboardTitle(Panel parent)
    {
        var titlePanel = InformationModel.Make<Panel>("TitlePanel");
        titlePanel.Width = 1180;
        titlePanel.Height = 80;
        titlePanel.TopMargin = 10;
        titlePanel.LeftMargin = 10;

        var titleBg = InformationModel.Make<Rectangle>("TitleBackground");
        titleBg.Width = 1180;
        titleBg.Height = 80;
        titleBg.FillColor = new Color(PrimaryBlue);
        titleBg.CornerRadius = 12;
        titlePanel.Add(titleBg);

        // Add chart icon
        var chartIcon = InformationModel.Make<Image>("ChartIcon");
        chartIcon.Width = 20;
        chartIcon.Height = 20;
        chartIcon.TopMargin = 25;
        chartIcon.LeftMargin = 20;
        chartIcon.Path = new ResourceUri("%APPLICATIONDIR%/Graphics/chart-icon.svg");
        titlePanel.Add(chartIcon);

        var titleLabel = InformationModel.Make<Label>("DashboardTitle");
        titleLabel.Text = "Manufacturing OEE Dashboard";
        titleLabel.Width = 600;
        titleLabel.Height = 40;
        titleLabel.TopMargin = 20;
        titleLabel.LeftMargin = 50;
        titleLabel.FontSize = 24;
        titleLabel.TextColor = Colors.White;
        titlePanel.Add(titleLabel);

        parent.Add(titlePanel);
    }

    private void CreateOEECards(Panel parent)
    {
        var cardsPanel = InformationModel.Make<Panel>("OEECardsPanel");
        cardsPanel.Width = 1180;
        cardsPanel.Height = 200;
        cardsPanel.TopMargin = 110;
        cardsPanel.LeftMargin = 10;

        // Use widget generator for OEE metric cards
        if (widgetGenerator == null) widgetGenerator = new OEEWidgetGenerator();

        var overallCard = widgetGenerator.CreateOEEMetricCardInstance("Overall OEE", "87.5%", "+2.3%", new Color(SuccessGreen), true);
        overallCard.LeftMargin = 0;
        overallCard.TopMargin = 0;
        cardsPanel.Add(overallCard);

        var availabilityCard = widgetGenerator.CreateOEEMetricCardInstance("Availability", "94.2%", "+1.1%", new Color(SuccessGreen), true);
        availabilityCard.LeftMargin = 295;
        availabilityCard.TopMargin = 0;
        cardsPanel.Add(availabilityCard);

        var performanceCard = widgetGenerator.CreateOEEMetricCardInstance("Performance", "91.8%", "-0.5%", new Color(WarningOrange), false);
        performanceCard.LeftMargin = 590;
        performanceCard.TopMargin = 0;
        cardsPanel.Add(performanceCard);

        var qualityCard = widgetGenerator.CreateOEEMetricCardInstance("Quality", "101.2%", "+3.2%", new Color(SuccessGreen), true);
        qualityCard.LeftMargin = 885;
        qualityCard.TopMargin = 0;
        cardsPanel.Add(qualityCard);

        parent.Add(cardsPanel);
    }

    private void CreateMachineStatusOverview(Panel parent)
    {
        var statusPanel = InformationModel.Make<Panel>("MachineStatusPanel");
        statusPanel.Width = 1180;
        statusPanel.Height = 150;
        statusPanel.TopMargin = 330;
        statusPanel.LeftMargin = 10;

        var statusBg = InformationModel.Make<Rectangle>("StatusBackground");
        statusBg.Width = 1180;
        statusBg.Height = 150;
        statusBg.FillColor = Colors.White;
        statusBg.BorderColor = new Color(BorderGray);
        statusBg.BorderThickness = 1;
        statusBg.CornerRadius = 12;
        statusPanel.Add(statusBg);

        // Add factory icon
        var factoryIcon = InformationModel.Make<Image>("FactoryIcon");
        factoryIcon.Width = 16;
        factoryIcon.Height = 16;
        factoryIcon.TopMargin = 18;
        factoryIcon.LeftMargin = 20;
        factoryIcon.Path = new ResourceUri("%APPLICATIONDIR%/Graphics/factory-icon.svg");
        statusPanel.Add(factoryIcon);

        var sectionTitle = InformationModel.Make<Label>("StatusTitle");
        sectionTitle.Text = "Production Line Status";
        sectionTitle.Width = 400;
        sectionTitle.Height = 25;
        sectionTitle.TopMargin = 15;
        sectionTitle.LeftMargin = 45;
        sectionTitle.FontSize = 16;
        sectionTitle.TextColor = new Color(TextDark);
        statusPanel.Add(sectionTitle);

        // Use widget generator for machine status indicators
        if (widgetGenerator == null) widgetGenerator = new OEEWidgetGenerator();

        var line1 = widgetGenerator.CreateMachineStatusIndicatorInstance("Line 1", "Running", new Color(SuccessGreen));
        line1.LeftMargin = 20;
        line1.TopMargin = 60;
        statusPanel.Add(line1);

        var line2 = widgetGenerator.CreateMachineStatusIndicatorInstance("Line 2", "Running", new Color(SuccessGreen));
        line2.LeftMargin = 210;
        line2.TopMargin = 60;
        statusPanel.Add(line2);

        var line3 = widgetGenerator.CreateMachineStatusIndicatorInstance("Line 3", "Idle", new Color(WarningOrange));
        line3.LeftMargin = 400;
        line3.TopMargin = 60;
        statusPanel.Add(line3);

        var line4 = widgetGenerator.CreateMachineStatusIndicatorInstance("Line 4", "Maintenance", new Color(DangerRed));
        line4.LeftMargin = 590;
        line4.TopMargin = 60;
        statusPanel.Add(line4);

        var line5 = widgetGenerator.CreateMachineStatusIndicatorInstance("Line 5", "Running", new Color(SuccessGreen));
        line5.LeftMargin = 780;
        line5.TopMargin = 60;
        statusPanel.Add(line5);

        var line6 = widgetGenerator.CreateMachineStatusIndicatorInstance("Line 6", "Setup", new Color(LightBlue));
        line6.LeftMargin = 970;
        line6.TopMargin = 60;
        statusPanel.Add(line6);

        parent.Add(statusPanel);
    }

    private void CreateChartPlaceholders(Panel parent)
    {
        var chartsPanel = InformationModel.Make<Panel>("ChartsPanel");
        chartsPanel.Width = 1180;
        chartsPanel.Height = 280;
        chartsPanel.TopMargin = 500;
        chartsPanel.LeftMargin = 10;

        CreateChartPlaceholder(chartsPanel, "Production Trend", 0, 0, 580, 280, true);
        CreateChartPlaceholder(chartsPanel, "Downtime Analysis", 600, 0, 580, 280, false);

        parent.Add(chartsPanel);
    }

    private void CreateChartPlaceholder(Panel parent, string title, int leftMargin, int topMargin, int width, int height, bool isTrendChart)
    {
        var chart = InformationModel.Make<Panel>("Chart_" + title.Replace(" ", ""));
        chart.Width = width;
        chart.Height = height;
        chart.LeftMargin = leftMargin;
        chart.TopMargin = topMargin;

        var chartBg = InformationModel.Make<Rectangle>("ChartBackground");
        chartBg.Width = width;
        chartBg.Height = height;
        chartBg.FillColor = Colors.White;
        chartBg.BorderColor = new Color(BorderGray);
        chartBg.BorderThickness = 1;
        chartBg.CornerRadius = 12;
        chart.Add(chartBg);

        // Add icon
        var chartIcon = InformationModel.Make<Image>("ChartIcon");
        chartIcon.Width = 16;
        chartIcon.Height = 16;
        chartIcon.TopMargin = 15;
        chartIcon.LeftMargin = 20;
        chartIcon.Path = new ResourceUri(isTrendChart ? "%APPLICATIONDIR%/Graphics/trend-icon.svg" : "%APPLICATIONDIR%/Graphics/clock-icon.svg");
        chart.Add(chartIcon);

        var chartTitle = InformationModel.Make<Label>("ChartTitle");
        chartTitle.Text = title;
        chartTitle.Width = width - 80;
        chartTitle.Height = 25;
        chartTitle.TopMargin = 15;
        chartTitle.LeftMargin = 50;
        chartTitle.FontSize = 14;
        chartTitle.TextColor = new Color(TextDark);
        chart.Add(chartTitle);

        parent.Add(chart);
    }

    // Configuration Screen Helper Methods
    private void CreateConfigurationHeader(Panel parent)
    {
        var headerPanel = InformationModel.Make<Panel>("ConfigurationHeader");
        headerPanel.Width = 1380;
        headerPanel.Height = 100;
        headerPanel.TopMargin = 10;
        headerPanel.LeftMargin = 10;

        var headerBg = InformationModel.Make<Rectangle>("ConfigurationHeaderBg");
        headerBg.Width = 1380;
        headerBg.Height = 100;
        headerBg.FillColor = new Color(WarningOrange);
        headerBg.CornerRadius = 12;
        headerPanel.Add(headerBg);

        // Add gear icon
        var gearIcon = InformationModel.Make<Image>("GearIcon");
        gearIcon.Width = 20;
        gearIcon.Height = 20;
        gearIcon.TopMargin = 32;
        gearIcon.LeftMargin = 30;
        gearIcon.Path = new ResourceUri("%APPLICATIONDIR%/Graphics/gear-icon.svg");
        headerPanel.Add(gearIcon);

        var titleLabel = InformationModel.Make<Label>("ConfigurationTitle");
        titleLabel.Text = "OEE Configuration Center";
        titleLabel.Width = 700;
        titleLabel.Height = 40;
        titleLabel.TopMargin = 20;
        titleLabel.LeftMargin = 60;
        titleLabel.FontSize = 28;
        titleLabel.TextColor = Colors.White;
        headerPanel.Add(titleLabel);

        var subtitleLabel = InformationModel.Make<Label>("ConfigurationSubtitle");
        subtitleLabel.Text = "Configure production targets, shift settings, and performance parameters";
        subtitleLabel.Width = 800;
        subtitleLabel.Height = 25;
        subtitleLabel.TopMargin = 65;
        subtitleLabel.LeftMargin = 40;
        subtitleLabel.FontSize = 14;
        subtitleLabel.TextColor = Colors.White;
        headerPanel.Add(subtitleLabel);

        parent.Add(headerPanel);
    }

    private void CreateProductionTargetsConfig(Panel parent)
    {
        var configSection = CreateConfigSection(parent, "Core Production Configuration", new Color(SuccessGreen), 20, 130, 670, 300);

        // Real calculator inputs only
        CreateConfigInputField(configSection, "Ideal Cycle Time:", "IdealCycleTimeSeconds", "seconds", "30.0", 20, 60);
        CreateConfigInputField(configSection, "Planned Production Time:", "PlannedProductionTimeHours", "hours", "8.0", 20, 120);
        CreateConfigInputField(configSection, "Production Target:", "ProductionTarget", "parts", "480", 20, 180);
        
        CreateConfigInputField(configSection, "Hours Per Shift:", "HoursPerShift", "hours", "8.0", 350, 60);
        CreateConfigInputField(configSection, "Number of Shifts:", "NumberOfShifts", "shifts", "3", 350, 120);
        CreateConfigInputField(configSection, "Update Rate:", "UpdateRateMs", "ms", "1000", 350, 180);
    }

    private void CreateShiftTimesConfig(Panel parent)
    {
        var configSection = CreateConfigSection(parent, "Shift Configuration", new Color(AccentTeal), 710, 130, 670, 300);

        // Real calculator inputs only
        CreateConfigInputField(configSection, "Shift Start Time:", "ShiftStartTime", "HH:MM:SS", "06:00:00", 20, 60);
        CreateConfigInputField(configSection, "Logging Verbosity:", "LoggingVerbosity", "level", "1", 350, 60);
        
        var noteLabel = InformationModel.Make<Label>("ShiftNote");
        noteLabel.Text = "💡 Shift Start Time: Use HH:MM:SS format (e.g., 06:00:00)\nLogging Verbosity: 0=minimal, 1=normal, 2=verbose, 3=debug";
        noteLabel.Width = 630;
        noteLabel.Height = 40;
        noteLabel.TopMargin = 120;
        noteLabel.LeftMargin = 20;
        noteLabel.FontSize = 11;
        noteLabel.TextColor = new Color(TextLight);
        configSection.Add(noteLabel);
    }

    private void CreateQualityThresholdsConfig(Panel parent)
    {
        var configSection = CreateConfigSection(parent, "Performance Targets", new Color(WarningOrange), 20, 450, 1360, 300);

        // Real calculator target inputs only
        CreateConfigInputField(configSection, "Quality Target:", "QualityTarget", "percentage", "95.0", 20, 60);
        CreateConfigInputField(configSection, "Performance Target:", "PerformanceTarget", "percentage", "85.0", 350, 60);
        CreateConfigInputField(configSection, "Availability Target:", "AvailabilityTarget", "percentage", "90.0", 680, 60);
        CreateConfigInputField(configSection, "OEE Target:", "OEETarget", "percentage", "72.7", 1020, 60);
        
        var targetNote = InformationModel.Make<Label>("TargetNote");
        targetNote.Text = "💡 Default targets represent world-class manufacturing standards:\nQuality 95% × Performance 85% × Availability 90% = OEE 72.7%";
        targetNote.Width = 1320;
        targetNote.Height = 40;
        targetNote.TopMargin = 120;
        targetNote.LeftMargin = 20;
        targetNote.FontSize = 11;
        targetNote.TextColor = new Color(TextLight);
        configSection.Add(targetNote);
    }

    private void CreateConfigurationButtons(Panel parent)
    {
        var buttonPanel = InformationModel.Make<Panel>("ConfigButtons");
        buttonPanel.Width = 1380;
        buttonPanel.Height = 80;
        buttonPanel.TopMargin = 780;
        buttonPanel.LeftMargin = 10;

        // Button background
        var buttonBg = InformationModel.Make<Rectangle>("ButtonBg");
        buttonBg.Width = 1380;
        buttonBg.Height = 80;
        buttonBg.FillColor = Colors.White;
        buttonBg.BorderColor = new Color(BorderGray);
        buttonBg.BorderThickness = 1;
        buttonBg.CornerRadius = 12;
        buttonPanel.Add(buttonBg);

        // Save Configuration Button
        var saveButton = InformationModel.Make<Button>("SaveConfigButton");
        saveButton.Text = "💾 Save Configuration";
        saveButton.Width = 200;
        saveButton.Height = 45;
        saveButton.TopMargin = 17;
        saveButton.LeftMargin = 50;
        saveButton.FontSize = 16;
        saveButton.TextColor = Colors.White;
        buttonPanel.Add(saveButton);

        // Load Defaults Button
        var defaultsButton = InformationModel.Make<Button>("LoadDefaultsButton");
        defaultsButton.Text = "🔄 Load Defaults";
        defaultsButton.Width = 200;
        defaultsButton.Height = 45;
        defaultsButton.TopMargin = 17;
        defaultsButton.LeftMargin = 280;
        defaultsButton.FontSize = 16;
        buttonPanel.Add(defaultsButton);

        // Export Settings Button
        var exportButton = InformationModel.Make<Button>("ExportConfigButton");
        exportButton.Text = "📤 Export Settings";
        exportButton.Width = 200;
        exportButton.Height = 45;
        exportButton.TopMargin = 17;
        exportButton.LeftMargin = 510;
        exportButton.FontSize = 16;
        buttonPanel.Add(exportButton);

        // Import Settings Button
        var importButton = InformationModel.Make<Button>("ImportConfigButton");
        importButton.Text = "📥 Import Settings";
        importButton.Width = 200;
        importButton.Height = 45;
        importButton.TopMargin = 17;
        importButton.LeftMargin = 740;
        importButton.FontSize = 16;
        buttonPanel.Add(importButton);

        // Test Configuration Button
        var testButton = InformationModel.Make<Button>("TestConfigButton");
        testButton.Text = "🧪 Test Configuration";
        testButton.Width = 220;
        testButton.Height = 45;
        testButton.TopMargin = 17;
        testButton.LeftMargin = 970;
        testButton.FontSize = 16;
        buttonPanel.Add(testButton);

        parent.Add(buttonPanel);
    }

    private Panel CreateConfigSection(Panel parent, string title, Color accentColor, int leftMargin, int topMargin, int width, int height)
    {
        var sectionPanel = InformationModel.Make<Panel>("ConfigSection_" + title.Replace(" ", "").Replace("&", ""));
        sectionPanel.Width = width;
        sectionPanel.Height = height;
        sectionPanel.TopMargin = topMargin;
        sectionPanel.LeftMargin = leftMargin;

        // Section background
        var sectionBg = InformationModel.Make<Rectangle>("SectionBg");
        sectionBg.Width = width;
        sectionBg.Height = height;
        sectionBg.FillColor = Colors.White;
        sectionBg.BorderColor = new Color(BorderGray);
        sectionBg.BorderThickness = 1;
        sectionBg.CornerRadius = 12;
        sectionPanel.Add(sectionBg);

        // Section header bar
        var headerBar = InformationModel.Make<Rectangle>("HeaderBar");
        headerBar.Width = width;
        headerBar.Height = 40;
        headerBar.TopMargin = 0;
        headerBar.LeftMargin = 0;
        headerBar.FillColor = accentColor;
        headerBar.CornerRadius = 12;
        sectionPanel.Add(headerBar);

        // Section title
        var sectionTitle = InformationModel.Make<Label>("SectionTitle");
        sectionTitle.Text = title;
        sectionTitle.Width = width - 40;
        sectionTitle.Height = 30;
        sectionTitle.TopMargin = 5;
        sectionTitle.LeftMargin = 20;
        sectionTitle.FontSize = 18;
        sectionTitle.TextColor = Colors.White;
        sectionPanel.Add(sectionTitle);

        parent.Add(sectionPanel);
        return sectionPanel;
    }

    private void CreateConfigInputField(Panel parent, string labelText, string variableName, string units, string defaultValue, int leftMargin, int topMargin)
    {
        // Field container
        var fieldContainer = InformationModel.Make<Panel>("ConfigField_" + variableName);
        fieldContainer.Width = 300;
        fieldContainer.Height = 50;
        fieldContainer.TopMargin = topMargin;
        fieldContainer.LeftMargin = leftMargin;

        // Label
        var label = InformationModel.Make<Label>("Label");
        label.Text = labelText;
        label.Width = 180;
        label.Height = 20;
        label.TopMargin = 0;
        label.LeftMargin = 0;
        label.FontSize = 12;
        label.TextColor = new Color(TextDark);
        fieldContainer.Add(label);

        // Input field
        var textBox = InformationModel.Make<TextBox>("Input");
        textBox.Text = defaultValue;
        textBox.Width = 120;
        textBox.Height = 30;
        textBox.TopMargin = 22;
        textBox.LeftMargin = 0;
        textBox.FontSize = 12;
        fieldContainer.Add(textBox);

        // Units label
        var unitsLabel = InformationModel.Make<Label>("Units");
        unitsLabel.Text = units;
        unitsLabel.Width = 80;
        unitsLabel.Height = 20;
        unitsLabel.TopMargin = 27;
        unitsLabel.LeftMargin = 130;
        unitsLabel.FontSize = 10;
        unitsLabel.TextColor = new Color(TextLight);
        fieldContainer.Add(unitsLabel);

        parent.Add(fieldContainer);
    }

    // Production Data Entry Screen Helper Methods
    private void CreateProductionDataHeader(Panel parent)
    {
        var headerPanel = InformationModel.Make<Panel>("ProductionDataHeader");
        headerPanel.Width = 1380;
        headerPanel.Height = 100;
        headerPanel.TopMargin = 10;
        headerPanel.LeftMargin = 10;

        var headerBg = InformationModel.Make<Rectangle>("ProductionDataHeaderBg");
        headerBg.Width = 1380;
        headerBg.Height = 100;
        headerBg.FillColor = new Color(SuccessGreen);
        headerBg.CornerRadius = 12;
        headerPanel.Add(headerBg);

        // Add chart icon
        var chartIcon = InformationModel.Make<Image>("ChartIcon");
        chartIcon.Width = 20;
        chartIcon.Height = 20;
        chartIcon.TopMargin = 32;
        chartIcon.LeftMargin = 30;
        chartIcon.Path = new ResourceUri("%APPLICATIONDIR%/Graphics/chart-icon.svg");
        headerPanel.Add(chartIcon);

        var titleLabel = InformationModel.Make<Label>("ProductionDataTitle");
        titleLabel.Text = "Production Data Entry & Monitoring";
        titleLabel.Width = 700;
        titleLabel.Height = 40;
        titleLabel.TopMargin = 20;
        titleLabel.LeftMargin = 60;
        titleLabel.FontSize = 28;
        titleLabel.TextColor = Colors.White;
        headerPanel.Add(titleLabel);

        var subtitleLabel = InformationModel.Make<Label>("ProductionDataSubtitle");
        subtitleLabel.Text = "Real-time production data input and live monitoring for OEE calculations";
        subtitleLabel.Width = 800;
        subtitleLabel.Height = 25;
        subtitleLabel.TopMargin = 65;
        subtitleLabel.LeftMargin = 60;
        subtitleLabel.FontSize = 14;
        subtitleLabel.TextColor = Colors.White;
        headerPanel.Add(subtitleLabel);

        parent.Add(headerPanel);
    }

    private void CreateRealTimeDataSection(Panel parent)
    {
        var dataSection = CreateConfigSection(parent, "Real-Time Data Inputs", new Color(SuccessGreen), 20, 130, 900, 280);

        CreateDataField(dataSection, "Total Runtime:", "TotalRuntimeSeconds", "seconds", "0.0", 20, 60);
        CreateDataField(dataSection, "Good Part Count:", "GoodPartCount", "parts", "0", 320, 60);
        CreateDataField(dataSection, "Bad Part Count:", "BadPartCount", "parts", "0", 620, 60);
        
        CreateDataField(dataSection, "Ideal Cycle Time:", "IdealCycleTimeSeconds", "seconds", "30.0", 20, 120);
        CreateDataField(dataSection, "Planned Production Time:", "PlannedProductionTimeHours", "hours", "8.0", 320, 120);
        CreateDataField(dataSection, "Production Target:", "ProductionTarget", "parts", "480", 620, 120);
        
        CreateDataField(dataSection, "System Status:", "SystemStatus", "status", "Running", 20, 180);
        CreateDataField(dataSection, "Data Quality Score:", "DataQualityScore", "percentage", "100.0", 320, 180);
        CreateDataField(dataSection, "Update Rate:", "UpdateRateMs", "ms", "1000", 620, 180);
    }

    private void CreateProductionCountersSection(Panel parent)
    {
        var countersSection = CreateConfigSection(parent, "Live Production Counters", new Color(AccentTeal), 20, 430, 1360, 200);

        CreateLiveCounter(countersSection, "Total Parts", "TotalCount", new Color(PrimaryBlue), 50, 60);
        CreateLiveCounter(countersSection, "Good Parts", "GoodPartCount", new Color(SuccessGreen), 250, 60);
        CreateLiveCounter(countersSection, "Defective Parts", "BadPartCount", new Color(DangerRed), 450, 60);
        CreateLiveCounter(countersSection, "Parts/Hour", "PartsPerHour", new Color(WarningOrange), 650, 60);
        CreateLiveCounter(countersSection, "Expected Parts", "ExpectedPartCount", new Color(LightBlue), 850, 60);
        CreateLiveCounter(countersSection, "Quality %", "Quality", new Color(SuccessGreen), 1050, 60);
    }

    // System Monitoring Screen Helper Methods
    private void CreateSystemMonitoringHeader(Panel parent)
    {
        var headerPanel = InformationModel.Make<Panel>("SystemMonitoringHeader");
        headerPanel.Width = 1380;
        headerPanel.Height = 100;
        headerPanel.TopMargin = 10;
        headerPanel.LeftMargin = 10;

        var headerBg = InformationModel.Make<Rectangle>("SystemMonitoringHeaderBg");
        headerBg.Width = 1380;
        headerBg.Height = 100;
        headerBg.FillColor = new Color(LightBlue);
        headerBg.CornerRadius = 12;
        headerPanel.Add(headerBg);

        // Add search icon
        var searchIcon = InformationModel.Make<Image>("SearchIcon");
        searchIcon.Width = 20;
        searchIcon.Height = 20;
        searchIcon.TopMargin = 32;
        searchIcon.LeftMargin = 30;
        searchIcon.Path = new ResourceUri("%APPLICATIONDIR%/Graphics/search-icon.svg");
        headerPanel.Add(searchIcon);

        var titleLabel = InformationModel.Make<Label>("SystemMonitoringTitle");
        titleLabel.Text = "System Health & Performance Monitoring";
        titleLabel.Width = 700;
        titleLabel.Height = 40;
        titleLabel.TopMargin = 20;
        titleLabel.LeftMargin = 60;
        titleLabel.FontSize = 28;
        titleLabel.TextColor = Colors.White;
        headerPanel.Add(titleLabel);

        var subtitleLabel = InformationModel.Make<Label>("SystemMonitoringSubtitle");
        subtitleLabel.Text = "Monitor system health, trends, and statistical analysis of OEE performance";
        subtitleLabel.Width = 800;
        subtitleLabel.Height = 25;
        subtitleLabel.TopMargin = 65;
        subtitleLabel.LeftMargin = 60;
        subtitleLabel.FontSize = 14;
        subtitleLabel.TextColor = Colors.White;
        headerPanel.Add(subtitleLabel);

        parent.Add(headerPanel);
    }

    private void CreateSystemHealthSection(Panel parent)
    {
        var healthSection = CreateConfigSection(parent, "System Health Status", new Color(SuccessGreen), 20, 130, 670, 320);

        CreateStatusIndicator(healthSection, "System Status", "SystemStatus", new Color(SuccessGreen), "Running", 20, 60);
        CreateStatusIndicator(healthSection, "Calculation Valid", "CalculationValid", new Color(LightBlue), "True", 350, 60);
        
        CreateHealthMetric(healthSection, "Data Quality Score:", "DataQualityScore", "percentage", "100.0%", 20, 120);
        CreateHealthMetric(healthSection, "Last Update Time:", "LastUpdateTime", "timestamp", "2025-11-12 14:30:00", 20, 160);
        CreateHealthMetric(healthSection, "Update Rate:", "UpdateRateMs", "milliseconds", "1000 ms", 20, 200);
        CreateHealthMetric(healthSection, "Logging Level:", "LoggingVerbosity", "level", "Normal (1)", 20, 240);
    }

    private void CreateTrendingSection(Panel parent)
    {
        var trendSection = CreateConfigSection(parent, "Performance Trends", new Color(AccentTeal), 710, 130, 670, 320);

        CreateTrendDisplay(trendSection, "Quality Trend", "QualityTrend", new Color(SuccessGreen), "Stable", 20, 60);
        CreateTrendDisplay(trendSection, "Performance Trend", "PerformanceTrend", new Color(WarningOrange), "Rising", 20, 110);
        CreateTrendDisplay(trendSection, "Availability Trend", "AvailabilityTrend", new Color(LightBlue), "Stable", 20, 160);
        CreateTrendDisplay(trendSection, "OEE Trend", "OEETrend", new Color(PrimaryBlue), "Rising", 20, 210);

        var trendNote = InformationModel.Make<Label>("TrendNote");
        trendNote.Text = "Trends based on 60-measurement rolling window";
        trendNote.Width = 630;
        trendNote.Height = 20;
        trendNote.TopMargin = 270;
        trendNote.LeftMargin = 20;
        trendNote.FontSize = 10;
        trendNote.TextColor = new Color(TextLight);
        trendSection.Add(trendNote);
    }

    private void CreateStatisticsSection(Panel parent)
    {
        var statsSection = CreateConfigSection(parent, "Statistical Analysis", new Color(PrimaryBlue), 20, 470, 1360, 280);

        // Quality Statistics
        CreateStatGroup(statsSection, "Quality Statistics", 50, 60, new Color(SuccessGreen));
        CreateStatMetric(statsSection, "Min:", "MinQuality", "%", "95.2%", 50, 100);
        CreateStatMetric(statsSection, "Max:", "MaxQuality", "%", "99.8%", 50, 130);
        CreateStatMetric(statsSection, "Avg:", "AvgQuality", "%", "97.5%", 50, 160);

        // Performance Statistics
        CreateStatGroup(statsSection, "Performance Statistics", 350, 60, new Color(WarningOrange));
        CreateStatMetric(statsSection, "Min:", "MinPerformance", "%", "82.1%", 350, 100);
        CreateStatMetric(statsSection, "Max:", "MaxPerformance", "%", "95.7%", 350, 130);
        CreateStatMetric(statsSection, "Avg:", "AvgPerformance", "%", "88.9%", 350, 160);

        // Availability Statistics
        CreateStatGroup(statsSection, "Availability Statistics", 650, 60, new Color(LightBlue));
        CreateStatMetric(statsSection, "Min:", "MinAvailability", "%", "85.3%", 650, 100);
        CreateStatMetric(statsSection, "Max:", "MaxAvailability", "%", "98.9%", 650, 130);
        CreateStatMetric(statsSection, "Avg:", "AvgAvailability", "%", "91.2%", 650, 160);

        // OEE Statistics
        CreateStatGroup(statsSection, "OEE Statistics", 950, 60, new Color(PrimaryBlue));
        CreateStatMetric(statsSection, "Min:", "MinOEE", "%", "72.5%", 950, 100);
        CreateStatMetric(statsSection, "Max:", "MaxOEE", "%", "92.1%", 950, 130);
        CreateStatMetric(statsSection, "Avg:", "AvgOEE", "%", "79.8%", 950, 160);
    }

    // Target Performance Screen Helper Methods
    private void CreateTargetPerformanceHeader(Panel parent)
    {
        var headerPanel = InformationModel.Make<Panel>("TargetPerformanceHeader");
        headerPanel.Width = 1380;
        headerPanel.Height = 100;
        headerPanel.TopMargin = 10;
        headerPanel.LeftMargin = 10;

        var headerBg = InformationModel.Make<Rectangle>("TargetPerformanceHeaderBg");
        headerBg.Width = 1380;
        headerBg.Height = 100;
        headerBg.FillColor = new Color(WarningOrange);
        headerBg.CornerRadius = 12;
        headerPanel.Add(headerBg);

        // Add target icon
        var targetIcon = InformationModel.Make<Image>("TargetIcon");
        targetIcon.Width = 20;
        targetIcon.Height = 20;
        targetIcon.TopMargin = 32;
        targetIcon.LeftMargin = 30;
        targetIcon.Path = new ResourceUri("%APPLICATIONDIR%/Graphics/target-icon.svg");
        headerPanel.Add(targetIcon);

        var titleLabel = InformationModel.Make<Label>("TargetPerformanceTitle");
        titleLabel.Text = "Target Performance Analysis";
        titleLabel.Width = 700;
        titleLabel.Height = 40;
        titleLabel.TopMargin = 20;
        titleLabel.LeftMargin = 60;
        titleLabel.FontSize = 28;
        titleLabel.TextColor = Colors.White;
        headerPanel.Add(titleLabel);

        var subtitleLabel = InformationModel.Make<Label>("TargetPerformanceSubtitle");
        subtitleLabel.Text = "Compare actual performance against targets and production planning goals";
        subtitleLabel.Width = 800;
        subtitleLabel.Height = 25;
        subtitleLabel.TopMargin = 65;
        subtitleLabel.LeftMargin = 60;
        subtitleLabel.FontSize = 14;
        subtitleLabel.TextColor = Colors.White;
        headerPanel.Add(subtitleLabel);

        parent.Add(headerPanel);
    }

    private void CreateTargetVsActualSection(Panel parent)
    {
        var targetSection = CreateConfigSection(parent, "Target vs Actual Performance", new Color(WarningOrange), 20, 130, 1360, 280);

        CreateTargetComparison(targetSection, "Quality", "Quality", "QualityTarget", "QualityVsTarget", new Color(SuccessGreen), 50, 60);
        CreateTargetComparison(targetSection, "Performance", "Performance", "PerformanceTarget", "PerformanceVsTarget", new Color(WarningOrange), 390, 60);
        CreateTargetComparison(targetSection, "Availability", "Availability", "AvailabilityTarget", "AvailabilityVsTarget", new Color(LightBlue), 730, 60);
        CreateTargetComparison(targetSection, "OEE", "OEE", "OEETarget", "OEEVsTarget", new Color(PrimaryBlue), 1070, 60);
    }

    private void CreateProductionPlanningSection(Panel parent)
    {
        var planningSection = CreateConfigSection(parent, "Production Planning Status", new Color(AccentTeal), 20, 430, 670, 280);

        CreatePlanningMetric(planningSection, "Production Target:", "ProductionTarget", "parts", "480", 20, 60);
        CreatePlanningMetric(planningSection, "Projected Total:", "ProjectedTotalCount", "parts", "465", 20, 100);
        CreatePlanningMetric(planningSection, "Target vs Actual:", "TargetVsActualParts", "parts", "-15", 20, 140);
        CreatePlanningMetric(planningSection, "Required Rate:", "RequiredRateToTarget", "parts/hr", "62.5", 20, 180);
        
        var behindSchedule = InformationModel.Make<Label>("BehindScheduleStatus");
        behindSchedule.Text = "📅 Production Status: On Track";
        behindSchedule.Width = 630;
        behindSchedule.Height = 30;
        behindSchedule.TopMargin = 220;
        behindSchedule.LeftMargin = 20;
        behindSchedule.FontSize = 14;
        behindSchedule.TextColor = new Color(SuccessGreen);
        planningSection.Add(behindSchedule);
    }

    private void CreatePerformanceAlertsSection(Panel parent)
    {
        var alertsSection = CreateConfigSection(parent, "Performance Alerts & Notifications", new Color(DangerRed), 710, 430, 670, 280);

        var alertTitle = InformationModel.Make<Label>("AlertsTitle");
        alertTitle.Text = "� Live System Status";
        alertTitle.Width = 630;
        alertTitle.Height = 30;
        alertTitle.TopMargin = 50;
        alertTitle.LeftMargin = 20;
        alertTitle.FontSize = 16;
        alertTitle.TextColor = new Color(TextDark);
        alertsSection.Add(alertTitle);

        // Real calculator-based status indicators
        CreateCalculatorStatusField(alertsSection, "System Status:", "SystemStatus", 20, 90);
        CreateCalculatorStatusField(alertsSection, "Calculation Valid:", "CalculationValid", 20, 120);
        CreateCalculatorStatusField(alertsSection, "Data Quality:", "DataQualityScore", 20, 150);
        CreateCalculatorStatusField(alertsSection, "Production Behind Schedule:", "ProductionBehindSchedule", 20, 180);

        var alertNote = InformationModel.Make<Label>("AlertNote");
        alertNote.Text = "Alert thresholds configured in Configuration screen";
        alertNote.Width = 630;
        alertNote.Height = 20;
        alertNote.TopMargin = 220;
        alertNote.LeftMargin = 20;
        alertNote.FontSize = 10;
        alertNote.TextColor = new Color(TextLight);
        alertsSection.Add(alertNote);
    }

    // Helper methods for new screen components
    private void CreateDataField(Panel parent, string labelText, string variableName, string units, string defaultValue, int leftMargin, int topMargin)
    {
        var fieldContainer = InformationModel.Make<Panel>("DataField_" + variableName);
        fieldContainer.Width = 280;
        fieldContainer.Height = 50;
        fieldContainer.TopMargin = topMargin;
        fieldContainer.LeftMargin = leftMargin;

        var label = InformationModel.Make<Label>("Label");
        label.Text = labelText;
        label.Width = 150;
        label.Height = 20;
        label.TopMargin = 0;
        label.LeftMargin = 0;
        label.FontSize = 12;
        label.TextColor = new Color(TextDark);
        fieldContainer.Add(label);

        var valueLabel = InformationModel.Make<Label>("Value");
        valueLabel.Text = defaultValue;
        valueLabel.Width = 80;
        valueLabel.Height = 25;
        valueLabel.TopMargin = 20;
        valueLabel.LeftMargin = 0;
        valueLabel.FontSize = 14;
        valueLabel.TextColor = new Color(PrimaryBlue);
        fieldContainer.Add(valueLabel);

        var unitsLabel = InformationModel.Make<Label>("Units");
        unitsLabel.Text = units;
        unitsLabel.Width = 60;
        unitsLabel.Height = 20;
        unitsLabel.TopMargin = 23;
        unitsLabel.LeftMargin = 85;
        unitsLabel.FontSize = 10;
        unitsLabel.TextColor = new Color(TextLight);
        fieldContainer.Add(unitsLabel);

        parent.Add(fieldContainer);
    }

    private void CreateLiveCounter(Panel parent, string title, string variableName, Color accentColor, int leftMargin, int topMargin)
    {
        var counterPanel = InformationModel.Make<Panel>("Counter_" + variableName);
        counterPanel.Width = 180;
        counterPanel.Height = 120;
        counterPanel.TopMargin = topMargin;
        counterPanel.LeftMargin = leftMargin;

        var counterBg = InformationModel.Make<Rectangle>("CounterBg");
        counterBg.Width = 180;
        counterBg.Height = 120;
        counterBg.FillColor = Colors.White;
        counterBg.BorderColor = accentColor;
        counterBg.BorderThickness = 2;
        counterBg.CornerRadius = 8;
        counterPanel.Add(counterBg);

        var titleLabel = InformationModel.Make<Label>("Title");
        titleLabel.Text = title;
        titleLabel.Width = 160;
        titleLabel.Height = 20;
        titleLabel.TopMargin = 10;
        titleLabel.LeftMargin = 10;
        titleLabel.FontSize = 12;
        titleLabel.TextColor = new Color(TextDark);
        counterPanel.Add(titleLabel);

        var valueLabel = InformationModel.Make<Label>("Value");
        valueLabel.Text = "0";
        valueLabel.Width = 160;
        valueLabel.Height = 40;
        valueLabel.TopMargin = 40;
        valueLabel.LeftMargin = 10;
        valueLabel.FontSize = 24;
        valueLabel.TextColor = accentColor;
        counterPanel.Add(valueLabel);

        var statusIndicator = InformationModel.Make<Ellipse>("StatusIndicator");
        statusIndicator.Width = 10;
        statusIndicator.Height = 10;
        statusIndicator.TopMargin = 95;
        statusIndicator.LeftMargin = 15;
        statusIndicator.FillColor = accentColor;
        counterPanel.Add(statusIndicator);

        parent.Add(counterPanel);
    }

    private void CreateStatusIndicator(Panel parent, string title, string variableName, Color statusColor, string defaultValue, int leftMargin, int topMargin)
    {
        var statusPanel = InformationModel.Make<Panel>("Status_" + variableName);
        statusPanel.Width = 300;
        statusPanel.Height = 40;
        statusPanel.TopMargin = topMargin;
        statusPanel.LeftMargin = leftMargin;

        var titleLabel = InformationModel.Make<Label>("Title");
        titleLabel.Text = title + ":";
        titleLabel.Width = 150;
        titleLabel.Height = 20;
        titleLabel.TopMargin = 10;
        titleLabel.LeftMargin = 0;
        titleLabel.FontSize = 12;
        titleLabel.TextColor = new Color(TextDark);
        statusPanel.Add(titleLabel);

        var statusIndicator = InformationModel.Make<Ellipse>("Indicator");
        statusIndicator.Width = 12;
        statusIndicator.Height = 12;
        statusIndicator.TopMargin = 14;
        statusIndicator.LeftMargin = 160;
        statusIndicator.FillColor = statusColor;
        statusPanel.Add(statusIndicator);

        var valueLabel = InformationModel.Make<Label>("Value");
        valueLabel.Text = defaultValue;
        valueLabel.Width = 100;
        valueLabel.Height = 20;
        valueLabel.TopMargin = 10;
        valueLabel.LeftMargin = 180;
        valueLabel.FontSize = 12;
        valueLabel.TextColor = statusColor;
        statusPanel.Add(valueLabel);

        parent.Add(statusPanel);
    }

    private void CreateHealthMetric(Panel parent, string labelText, string variableName, string units, string defaultValue, int leftMargin, int topMargin)
    {
        var metricPanel = InformationModel.Make<Panel>("HealthMetric_" + variableName);
        metricPanel.Width = 300;
        metricPanel.Height = 30;
        metricPanel.TopMargin = topMargin;
        metricPanel.LeftMargin = leftMargin;

        var label = InformationModel.Make<Label>("Label");
        label.Text = labelText;
        label.Width = 150;
        label.Height = 20;
        label.TopMargin = 5;
        label.LeftMargin = 0;
        label.FontSize = 12;
        label.TextColor = new Color(TextDark);
        metricPanel.Add(label);

        var valueLabel = InformationModel.Make<Label>("Value");
        valueLabel.Text = defaultValue;
        valueLabel.Width = 140;
        valueLabel.Height = 20;
        valueLabel.TopMargin = 5;
        valueLabel.LeftMargin = 160;
        valueLabel.FontSize = 12;
        valueLabel.TextColor = new Color(PrimaryBlue);
        metricPanel.Add(valueLabel);

        parent.Add(metricPanel);
    }

    private void CreateTrendDisplay(Panel parent, string title, string variableName, Color trendColor, string defaultTrend, int leftMargin, int topMargin)
    {
        var trendPanel = InformationModel.Make<Panel>("Trend_" + variableName);
        trendPanel.Width = 300;
        trendPanel.Height = 40;
        trendPanel.TopMargin = topMargin;
        trendPanel.LeftMargin = leftMargin;

        var titleLabel = InformationModel.Make<Label>("Title");
        titleLabel.Text = title + ":";
        titleLabel.Width = 150;
        titleLabel.Height = 20;
        titleLabel.TopMargin = 5;
        titleLabel.LeftMargin = 0;
        titleLabel.FontSize = 12;
        titleLabel.TextColor = new Color(TextDark);
        trendPanel.Add(titleLabel);

        var trendLabel = InformationModel.Make<Label>("Trend");
        trendLabel.Text = defaultTrend;
        trendLabel.Width = 140;
        trendLabel.Height = 20;
        trendLabel.TopMargin = 5;
        trendLabel.LeftMargin = 160;
        trendLabel.FontSize = 12;
        trendLabel.TextColor = trendColor;
        trendPanel.Add(trendLabel);

        // Trend arrow indicator
        var arrow = InformationModel.Make<Label>("Arrow");
        arrow.Text = "→";
        arrow.Width = 20;
        arrow.Height = 20;
        arrow.TopMargin = 5;
        arrow.LeftMargin = 310;
        arrow.FontSize = 14;
        arrow.TextColor = trendColor;
        trendPanel.Add(arrow);

        parent.Add(trendPanel);
    }

    private void CreateStatGroup(Panel parent, string title, int leftMargin, int topMargin, Color accentColor)
    {
        var titleLabel = InformationModel.Make<Label>("StatGroup_" + title.Replace(" ", ""));
        titleLabel.Text = title;
        titleLabel.Width = 250;
        titleLabel.Height = 25;
        titleLabel.TopMargin = topMargin;
        titleLabel.LeftMargin = leftMargin;
        titleLabel.FontSize = 14;
        titleLabel.TextColor = accentColor;
        parent.Add(titleLabel);
    }

    private void CreateStatMetric(Panel parent, string labelText, string variableName, string units, string defaultValue, int leftMargin, int topMargin)
    {
        var metricPanel = InformationModel.Make<Panel>("StatMetric_" + variableName);
        metricPanel.Width = 250;
        metricPanel.Height = 25;
        metricPanel.TopMargin = topMargin;
        metricPanel.LeftMargin = leftMargin;

        var label = InformationModel.Make<Label>("Label");
        label.Text = labelText;
        label.Width = 60;
        label.Height = 20;
        label.TopMargin = 2;
        label.LeftMargin = 0;
        label.FontSize = 11;
        label.TextColor = new Color(TextDark);
        metricPanel.Add(label);

        var valueLabel = InformationModel.Make<Label>("Value");
        valueLabel.Text = defaultValue;
        valueLabel.Width = 80;
        valueLabel.Height = 20;
        valueLabel.TopMargin = 2;
        valueLabel.LeftMargin = 70;
        valueLabel.FontSize = 11;
        valueLabel.TextColor = new Color(PrimaryBlue);
        metricPanel.Add(valueLabel);

        parent.Add(metricPanel);
    }

    private void CreateTargetComparison(Panel parent, string metricName, string actualVar, string targetVar, string varianceVar, Color metricColor, int leftMargin, int topMargin)
    {
        var comparisonPanel = InformationModel.Make<Panel>("Comparison_" + metricName);
        comparisonPanel.Width = 300;
        comparisonPanel.Height = 180;
        comparisonPanel.TopMargin = topMargin;
        comparisonPanel.LeftMargin = leftMargin;

        var comparisonBg = InformationModel.Make<Rectangle>("ComparisonBg");
        comparisonBg.Width = 300;
        comparisonBg.Height = 180;
        comparisonBg.FillColor = Colors.White;
        comparisonBg.BorderColor = metricColor;
        comparisonBg.BorderThickness = 2;
        comparisonBg.CornerRadius = 8;
        comparisonPanel.Add(comparisonBg);

        var titleLabel = InformationModel.Make<Label>("Title");
        titleLabel.Text = metricName;
        titleLabel.Width = 280;
        titleLabel.Height = 25;
        titleLabel.TopMargin = 10;
        titleLabel.LeftMargin = 10;
        titleLabel.FontSize = 16;
        titleLabel.TextColor = metricColor;
        comparisonPanel.Add(titleLabel);

        CreateComparisonMetric(comparisonPanel, "Actual:", "87.5%", metricColor, 10, 45);
        CreateComparisonMetric(comparisonPanel, "Target:", "85.0%", new Color(TextDark), 10, 75);
        CreateComparisonMetric(comparisonPanel, "Variance:", "+2.5%", new Color(SuccessGreen), 10, 105);

        // Add checkmark icon
        var checkIcon = InformationModel.Make<Image>("CheckIcon");
        checkIcon.Width = 16;
        checkIcon.Height = 16;
        checkIcon.TopMargin = 138;
        checkIcon.LeftMargin = 10;
        checkIcon.Path = new ResourceUri("%APPLICATIONDIR%/Graphics/checkmark-icon.svg");
        comparisonPanel.Add(checkIcon);

        var statusLabel = InformationModel.Make<Label>("Status");
        statusLabel.Text = "Above Target";
        statusLabel.Width = 250;
        statusLabel.Height = 20;
        statusLabel.TopMargin = 140;
        statusLabel.LeftMargin = 30;
        statusLabel.FontSize = 12;
        statusLabel.TextColor = new Color(SuccessGreen);
        comparisonPanel.Add(statusLabel);

        parent.Add(comparisonPanel);
    }

    private void CreateComparisonMetric(Panel parent, string labelText, string value, Color valueColor, int leftMargin, int topMargin)
    {
        var metricPanel = InformationModel.Make<Panel>("CompMetric_" + labelText.Replace(":", ""));
        metricPanel.Width = 280;
        metricPanel.Height = 25;
        metricPanel.TopMargin = topMargin;
        metricPanel.LeftMargin = leftMargin;

        var label = InformationModel.Make<Label>("Label");
        label.Text = labelText;
        label.Width = 80;
        label.Height = 20;
        label.TopMargin = 2;
        label.LeftMargin = 0;
        label.FontSize = 12;
        label.TextColor = new Color(TextDark);
        metricPanel.Add(label);

        var valueLabel = InformationModel.Make<Label>("Value");
        valueLabel.Text = value;
        valueLabel.Width = 100;
        valueLabel.Height = 20;
        valueLabel.TopMargin = 2;
        valueLabel.LeftMargin = 90;
        valueLabel.FontSize = 14;
        valueLabel.TextColor = valueColor;
        metricPanel.Add(valueLabel);

        parent.Add(metricPanel);
    }

    private void CreatePlanningMetric(Panel parent, string labelText, string variableName, string units, string defaultValue, int leftMargin, int topMargin)
    {
        var planningPanel = InformationModel.Make<Panel>("PlanningMetric_" + variableName);
        planningPanel.Width = 300;
        planningPanel.Height = 30;
        planningPanel.TopMargin = topMargin;
        planningPanel.LeftMargin = leftMargin;

        var label = InformationModel.Make<Label>("Label");
        label.Text = labelText;
        label.Width = 150;
        label.Height = 20;
        label.TopMargin = 5;
        label.LeftMargin = 0;
        label.FontSize = 12;
        label.TextColor = new Color(TextDark);
        planningPanel.Add(label);

        var valueLabel = InformationModel.Make<Label>("Value");
        valueLabel.Text = defaultValue + " " + units;
        valueLabel.Width = 140;
        valueLabel.Height = 20;
        valueLabel.TopMargin = 5;
        valueLabel.LeftMargin = 160;
        valueLabel.FontSize = 12;
        valueLabel.TextColor = new Color(AccentTeal);
        planningPanel.Add(valueLabel);

        parent.Add(planningPanel);
    }

    private void CreateCalculatorStatusField(Panel parent, string labelText, string variableName, int leftMargin, int topMargin)
    {
        var statusPanel = InformationModel.Make<Panel>("CalcStatus_" + variableName);
        statusPanel.Width = 630;
        statusPanel.Height = 25;
        statusPanel.TopMargin = topMargin;
        statusPanel.LeftMargin = leftMargin;

        var label = InformationModel.Make<Label>("Label");
        label.Text = labelText;
        label.Width = 200;
        label.Height = 20;
        label.TopMargin = 2;
        label.LeftMargin = 0;
        label.FontSize = 12;
        label.TextColor = new Color(TextDark);
        statusPanel.Add(label);

        var valueLabel = InformationModel.Make<Label>("Value");
        valueLabel.Text = "Connected to " + variableName;
        valueLabel.Width = 400;
        valueLabel.Height = 20;
        valueLabel.TopMargin = 2;
        valueLabel.LeftMargin = 210;
        valueLabel.FontSize = 12;
        valueLabel.TextColor = new Color(PrimaryBlue);
        statusPanel.Add(valueLabel);

        parent.Add(statusPanel);
    }

    [ExportMethod]
    public void CreateLiveDashboard()
    {
        try
        {
            Log.Info("ModernUIGenerator", "Creating live OEE dashboard with real-time data...");
            
            // Initialize widget generator
            if (widgetGenerator == null)
            {
                widgetGenerator = new OEEWidgetGenerator();
            }

            // Create widget library
            widgetGenerator.CreateAllOEEWidgets();
            
            // Set up OEE data source and calculator
            CreateOEEDataSourceAndCalculator();
            
            // Create the visual dashboard
            CreateOEEDashboard();
            
            // Connect to live data from calculator
            ConnectDashboardToCalculator();
            
            Log.Info("ModernUIGenerator", "Live dashboard created successfully with real-time OEE data!");
            Log.Info("ModernUIGenerator", "Dashboard shows live values from ObjectTypeOEE_Calculator");
        }
        catch (Exception ex)
        {
            Log.Error("ModernUIGenerator", "Error creating live dashboard: " + ex.Message);
        }
    }

    [ExportMethod]
    public void TestSystemMonitoring()
    {
        try
        {
            Log.Info("ModernUIGenerator", "Testing System Monitoring screen creation...");
            
            // Test just the system monitoring screen
            CreateSystemMonitoringScreen();
            
            Log.Info("ModernUIGenerator", "System Monitoring test completed successfully!");
        }
        catch (Exception ex)
        {
            Log.Error("ModernUIGenerator", "System Monitoring test failed: " + ex.Message);
            Log.Error("ModernUIGenerator", "Stack trace: " + ex.StackTrace);
        }
    }

    [ExportMethod]
    public void ShowAvailableScreens()
    {
        Log.Info("ModernUIGenerator", "=== WIDGETIZED OEE SYSTEM SCREENS ===");
        Log.Info("ModernUIGenerator", "");
        Log.Info("ModernUIGenerator", "[DASHBOARD] OEE Dashboard (CreateOEEDashboard)");
        Log.Info("ModernUIGenerator", "   ✅ WIDGETIZED: Uses OEEMetricCard widgets for all metrics");
        Log.Info("ModernUIGenerator", "   ✅ WIDGETIZED: Uses MachineStatusIndicator widgets for line status");
        Log.Info("ModernUIGenerator", "   - Live OEE, Quality, Performance, Availability metrics");
        Log.Info("ModernUIGenerator", "   - Real-time counters and status indicators");
        Log.Info("ModernUIGenerator", "   - Location: UI/Dashboards/OEEScreens/OEEDashboard/");
        Log.Info("ModernUIGenerator", "");
        Log.Info("ModernUIGenerator", "[CONFIG] Configuration (CreateConfigurationScreen)");
        Log.Info("ModernUIGenerator", "   ✅ READY FOR WIDGETIZATION: ConfigSection and ConfigInputField widgets available");
        Log.Info("ModernUIGenerator", "   - ONLY actual calculator input variables");
        Log.Info("ModernUIGenerator", "   - Core production config, shift config, performance targets");
        Log.Info("ModernUIGenerator", "   - Location: UI/Dashboards/ConfigScreens/OEEConfiguration/");
        Log.Info("ModernUIGenerator", "");
        Log.Info("ModernUIGenerator", "[DATA] Production Data Entry (CreateProductionDataEntryScreen)");
        Log.Info("ModernUIGenerator", "   ✅ READY FOR WIDGETIZATION: DataField and LiveCounter widgets available");
        Log.Info("ModernUIGenerator", "   - Real-time data monitoring ONLY");
        Log.Info("ModernUIGenerator", "   - Live production counters");
        Log.Info("ModernUIGenerator", "   - Location: UI/Dashboards/DataScreens/ProductionDataEntry/");
        Log.Info("ModernUIGenerator", "");
        Log.Info("ModernUIGenerator", "[MONITOR] System Monitoring (CreateSystemMonitoringScreen)");
        Log.Info("ModernUIGenerator", "   ✅ READY FOR WIDGETIZATION: StatusIndicator and TrendDisplay widgets available");
        Log.Info("ModernUIGenerator", "   - System health indicators");
        Log.Info("ModernUIGenerator", "   - Performance trend analysis");
        Log.Info("ModernUIGenerator", "   - Location: UI/Dashboards/MonitoringScreens/SystemMonitoring/");
        Log.Info("ModernUIGenerator", "");
        Log.Info("ModernUIGenerator", "[TARGET] Target Performance Analysis (CreateTargetPerformanceScreen)");
        Log.Info("ModernUIGenerator", "   ✅ READY FOR WIDGETIZATION: TargetComparison widgets available");
        Log.Info("ModernUIGenerator", "   - Target vs actual comparisons");
        Log.Info("ModernUIGenerator", "   - Production planning status");
        Log.Info("ModernUIGenerator", "   - Location: UI/Dashboards/PerformanceScreens/TargetPerformance/");
        Log.Info("ModernUIGenerator", "");
        Log.Info("ModernUIGenerator", "[CREATE] Create Complete System (CreateCompleteOEESystem)");
        Log.Info("ModernUIGenerator", "   ✅ WIDGETIZED: Auto-creates widget library before screen generation");
        Log.Info("ModernUIGenerator", "   - Creates all 5 optimized screens in organized folders");
        Log.Info("ModernUIGenerator", "   - 100% verified calculator variable coverage");
        Log.Info("ModernUIGenerator", "   - All reusable components converted to widgets");
        Log.Info("ModernUIGenerator", "");
        Log.Info("ModernUIGenerator", "[CLEAR] Clear All Screens (ClearAllScreens)");
        Log.Info("ModernUIGenerator", "   - Removes all organized dashboard folders and legacy screens");
        Log.Info("ModernUIGenerator", "");
        Log.Info("ModernUIGenerator", "=== ORGANIZED DASHBOARD STRUCTURE ===");
        Log.Info("ModernUIGenerator", "📁 UI/Dashboards/");
        Log.Info("ModernUIGenerator", "  ├── 📁 OEEScreens/");
        Log.Info("ModernUIGenerator", "  │   └── 📁 OEEDashboard/");
        Log.Info("ModernUIGenerator", "  ├── 📁 ConfigScreens/");
        Log.Info("ModernUIGenerator", "  │   └── 📁 OEEConfiguration/");
        Log.Info("ModernUIGenerator", "  ├── 📁 DataScreens/");
        Log.Info("ModernUIGenerator", "  │   └── 📁 ProductionDataEntry/");
        Log.Info("ModernUIGenerator", "  ├── 📁 MonitoringScreens/");
        Log.Info("ModernUIGenerator", "  │   └── 📁 SystemMonitoring/");
        Log.Info("ModernUIGenerator", "  └── 📁 PerformanceScreens/");
        Log.Info("ModernUIGenerator", "      └── 📁 TargetPerformance/");
        Log.Info("ModernUIGenerator", "");
        Log.Info("ModernUIGenerator", "=== WIDGETIZATION STATUS ===");
        Log.Info("ModernUIGenerator", "✅ OEE Dashboard: FULLY WIDGETIZED");
        Log.Info("ModernUIGenerator", "   - OEE metric cards: Using OEEMetricCard widget");
        Log.Info("ModernUIGenerator", "   - Machine status indicators: Using MachineStatusIndicator widget");
        Log.Info("ModernUIGenerator", "   - Removed 120+ lines of duplicate code");
        Log.Info("ModernUIGenerator", "🔄 Configuration Screen: Widget library ready");
        Log.Info("ModernUIGenerator", "🔄 Data Entry Screen: Widget library ready");  
        Log.Info("ModernUIGenerator", "🔄 Monitoring Screen: Widget library ready");
        Log.Info("ModernUIGenerator", "🔄 Target Performance Screen: Widget library ready");
        Log.Info("ModernUIGenerator", "");
        Log.Info("ModernUIGenerator", "=== WIDGET LIBRARY ===");
        Log.Info("ModernUIGenerator", "📦 11 reusable widgets created in OEEWidgetGenerator");
        Log.Info("ModernUIGenerator", "📦 Widget instances created via OEEWidgetGenerator helper methods");
        Log.Info("ModernUIGenerator", "📦 Consistent styling across all components");
        Log.Info("ModernUIGenerator", "");
        Log.Info("ModernUIGenerator", "=== ORGANIZATION BENEFITS ===");
        Log.Info("ModernUIGenerator", "✅ Organized by screen type for better maintainability");
        Log.Info("ModernUIGenerator", "✅ Each screen in its own dedicated folder");
        Log.Info("ModernUIGenerator", "✅ Easy to locate and manage individual screens");
        Log.Info("ModernUIGenerator", "✅ Consistent with widget organization structure");
        Log.Info("ModernUIGenerator", "");
        Log.Info("ModernUIGenerator", "Total Calculator Variables: 70+ input/output variables");
        Log.Info("ModernUIGenerator", "Screen Coverage: Complete and optimized");
        Log.Info("ModernUIGenerator", "Redundancy: Eliminated via widgetization");
        Log.Info("ModernUIGenerator", "Design: Professional blue/teal theme");
        Log.Info("ModernUIGenerator", "Compatibility: FactoryTalk Optix 1.6.4");
        Log.Info("ModernUIGenerator", "");
        Log.Info("ModernUIGenerator", "🎯 NEXT: Run CreateCompleteOEESystem() to generate all screens with organized structure");
    }

    // Helper method to create OEE data source and calculator
    private void CreateOEEDataSourceAndCalculator()
    {
        try
        {
            // Use OEETypeGenerator to create an OEE instance if needed
            var oeeGenerator = new OEETypeGenerator();
            
            // Create OEE ObjectType if it doesn't exist
            oeeGenerator.CreateOEEObjectType();
            
            // Create a default OEE instance for the dashboard
            oeeGenerator.CreateOEEInstance();
            
            Log.Info("ModernUIGenerator", "OEE data source and calculator created successfully");
        }
        catch (Exception ex)
        {
            Log.Error("ModernUIGenerator", "Error creating OEE data source: " + ex.Message);
        }
    }

    // Helper method to connect dashboard widgets to live calculator data
    private void ConnectDashboardToCalculator()
    {
        try
        {
            // Find the OEE dashboard panel
            var dashboardFolder = Project.Current.Get("UI/Dashboards/OEEScreens/OEEDashboard");
            if (dashboardFolder == null)
            {
                Log.Warning("ModernUIGenerator", "Dashboard folder not found for data binding");
                return;
            }

            var dashboardPanel = dashboardFolder.Get("OEEDashboard");
            if (dashboardPanel == null)
            {
                Log.Warning("ModernUIGenerator", "Dashboard panel not found for data binding");
                return;
            }

            // Find OEE instance to connect to
            var oeeInstance = Project.Current.Get("Objects/GitHub_OEE_UI/OEE_Instance1");
            if (oeeInstance == null)
            {
                Log.Warning("ModernUIGenerator", "No OEE instance found to connect dashboard to");
                return;
            }

            // Connect OEE metric cards to live data
            ConnectMetricCardToData(dashboardPanel, "OEECard", oeeInstance, "OEE");
            ConnectMetricCardToData(dashboardPanel, "AvailabilityCard", oeeInstance, "Availability");
            ConnectMetricCardToData(dashboardPanel, "PerformanceCard", oeeInstance, "Performance");
            ConnectMetricCardToData(dashboardPanel, "QualityCard", oeeInstance, "Quality");

            Log.Info("ModernUIGenerator", "Dashboard connected to live OEE calculator data!");
        }
        catch (Exception ex)
        {
            Log.Error("ModernUIGenerator", "Error connecting dashboard to calculator: " + ex.Message);
        }
    }

    // Helper method to connect individual metric cards to calculator data
    private void ConnectMetricCardToData(IUANode dashboardPanel, string cardName, IUANode oeeInstance, string variableName)
    {
        try
        {
            var card = dashboardPanel.Find(cardName);
            if (card != null)
            {
                var valueLabel = card.Find("ValueLabel");
                if (valueLabel != null)
                {
                    // Create dynamic link to the calculator variable
                    var sourceVariable = oeeInstance.GetVariable(variableName);
                    if (sourceVariable != null)
                    {
                        valueLabel.GetVariable("Text").SetDynamicLink(sourceVariable);
                        Log.Info("ModernUIGenerator", $"{cardName} connected to {variableName}");
                    }
                    else
                    {
                        Log.Warning("ModernUIGenerator", $"Variable {variableName} not found in OEE instance");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error("ModernUIGenerator", $"Error connecting {cardName} to {variableName}: " + ex.Message);
        }
    }
}
