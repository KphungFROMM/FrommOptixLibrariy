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

public class OEEWidgetGenerator : BaseNetLogic
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

    // Helper method to ensure consistent widget folder structure
    private Folder GetWidgetFolder(string categoryName, string widgetName)
    {
        var uiFolder = Project.Current.Get("UI");
        
        // Ensure main Widgets folder exists
        var widgetsFolder = uiFolder.Get("Widgets");
        if (widgetsFolder == null)
        {
            widgetsFolder = InformationModel.Make<Folder>("Widgets");
            uiFolder.Add(widgetsFolder);
        }

        // Ensure category folder exists (e.g., "MetricWidgets", "StatusWidgets", etc.)
        var categoryFolder = widgetsFolder.Get(categoryName);
        if (categoryFolder == null)
        {
            categoryFolder = InformationModel.Make<Folder>(categoryName);
            widgetsFolder.Add(categoryFolder);
        }

        // Ensure specific widget folder exists
        var specificWidgetFolder = categoryFolder.Get(widgetName);
        if (specificWidgetFolder == null)
        {
            specificWidgetFolder = InformationModel.Make<Folder>(widgetName);
            categoryFolder.Add(specificWidgetFolder);
        }

        return (Folder)specificWidgetFolder;
    }

    [ExportMethod]
    public void CreateAllOEEWidgets()
    {
        try
        {
            Log.Info("OEEWidgetGenerator", "Creating all OEE widgets...");
            CreateOEEMetricCard();
            CreateMachineStatusIndicator();
            CreateChartPlaceholder();
            CreateConfigSection();
            CreateConfigInputField();
            CreateDataField();
            CreateLiveCounter();
            CreateStatusIndicator();
            CreateTargetComparison();
            CreateTrendDisplay();
            CreateHeaderPanel();
            Log.Info("OEEWidgetGenerator", "All OEE widgets created successfully!");
        }
        catch (Exception ex)
        {
            Log.Error("OEEWidgetGenerator", "Error creating widgets: " + ex.Message);
        }
    }

    [ExportMethod]
    public void CreateOEEMetricCard()
    {
        try
        {
            Log.Info("OEEWidgetGenerator", "Creating OEE Metric Card widget...");
            
            // Use organized folder structure: UI/Widgets/MetricWidgets/OEEMetricCard/
            var widgetFolder = GetWidgetFolder("MetricWidgets", "OEEMetricCard");

            // Remove existing widget if it exists
            var existingWidget = widgetFolder.Get("OEEMetricCard");
            if (existingWidget != null)
            {
                existingWidget.Delete();
            }

            // Create the metric card widget (280x180)
            var card = InformationModel.Make<Panel>("OEEMetricCard");
            card.Width = 280;
            card.Height = 180;

            var cardBg = InformationModel.Make<Rectangle>("CardBackground");
            cardBg.Width = 280;
            cardBg.Height = 180;
            cardBg.FillColor = Colors.White;
            cardBg.BorderColor = new Color(BorderGray);
            cardBg.BorderThickness = 1;
            cardBg.CornerRadius = 16;
            card.Add(cardBg);

            var accentBar = InformationModel.Make<Rectangle>("AccentBar");
            accentBar.Width = 4;
            accentBar.Height = 180;
            accentBar.LeftMargin = 0;
            accentBar.TopMargin = 0;
            accentBar.FillColor = new Color(AccentTeal);
            accentBar.CornerRadius = 2;
            card.Add(accentBar);

            var titleLabel = InformationModel.Make<Label>("CardTitle");
            titleLabel.Text = "Metric Name";
            titleLabel.Width = 250;
            titleLabel.Height = 25;
            titleLabel.TopMargin = 20;
            titleLabel.LeftMargin = 20;
            titleLabel.FontSize = 14;
            titleLabel.TextColor = new Color(TextDark);
            card.Add(titleLabel);

            var valueLabel = InformationModel.Make<Label>("CardValue");
            valueLabel.Text = "0.0%";
            valueLabel.Width = 250;
            valueLabel.Height = 50;
            valueLabel.TopMargin = 60;
            valueLabel.LeftMargin = 20;
            valueLabel.FontSize = 36;
            valueLabel.TextColor = new Color(PrimaryBlue);
            card.Add(valueLabel);

            var trendLabel = InformationModel.Make<Label>("CardTrend");
            trendLabel.Text = "+0.0%";
            trendLabel.Width = 220;
            trendLabel.Height = 25;
            trendLabel.TopMargin = 130;
            trendLabel.LeftMargin = 50;
            trendLabel.FontSize = 14;
            trendLabel.TextColor = new Color(SuccessGreen);
            card.Add(trendLabel);

            // Add simple arrow using rectangles (chevron shape)
            var arrowStem = InformationModel.Make<Rectangle>("ArrowStem");
            arrowStem.Width = 2;
            arrowStem.Height = 10;
            arrowStem.TopMargin = 135;
            arrowStem.LeftMargin = 30;
            arrowStem.FillColor = new Color(SuccessGreen);
            card.Add(arrowStem);

            var arrowLeft = InformationModel.Make<Rectangle>("ArrowLeft");
            arrowLeft.Width = 6;
            arrowLeft.Height = 2;
            arrowLeft.TopMargin = 135;
            arrowLeft.LeftMargin = 26;
            arrowLeft.FillColor = new Color(SuccessGreen);
            arrowLeft.Rotation = -45;
            card.Add(arrowLeft);

            var arrowRight = InformationModel.Make<Rectangle>("ArrowRight");
            arrowRight.Width = 6;
            arrowRight.Height = 2;
            arrowRight.TopMargin = 135;
            arrowRight.LeftMargin = 32;
            arrowRight.FillColor = new Color(SuccessGreen);
            arrowRight.Rotation = 45;
            card.Add(arrowRight);

            widgetFolder.Add(card);
            Log.Info("OEEWidgetGenerator", "OEE Metric Card widget created successfully in UI/Widgets/MetricWidgets/OEEMetricCard/!");
        }
        catch (Exception ex)
        {
            Log.Error("OEEWidgetGenerator", "Error creating OEE Metric Card widget: " + ex.Message);
        }
    }

    [ExportMethod]
    public void CreateMachineStatusIndicator()
    {
        try
        {
            Log.Info("OEEWidgetGenerator", "Creating Machine Status Indicator widget...");
            
            // Use organized folder structure: UI/Widgets/StatusWidgets/MachineStatusIndicator/
            var widgetFolder = GetWidgetFolder("StatusWidgets", "MachineStatusIndicator");

            // Remove existing widget if it exists
            var existingWidget = widgetFolder.Get("MachineStatusIndicator");
            if (existingWidget != null)
            {
                existingWidget.Delete();
            }

            // Create the machine status indicator widget (170x80)
            var indicator = InformationModel.Make<Panel>("MachineStatusIndicator");
            indicator.Width = 170;
            indicator.Height = 80;

            var indicatorBg = InformationModel.Make<Rectangle>("IndicatorBackground");
            indicatorBg.Width = 170;
            indicatorBg.Height = 80;
            indicatorBg.FillColor = new Color(SurfaceGray);
            indicatorBg.BorderColor = new Color(SuccessGreen);
            indicatorBg.BorderThickness = 2;
            indicatorBg.CornerRadius = 12;
            indicator.Add(indicatorBg);

            var statusPulse = InformationModel.Make<Ellipse>("StatusPulse");
            statusPulse.Width = 12;
            statusPulse.Height = 12;
            statusPulse.TopMargin = 15;
            statusPulse.LeftMargin = 15;
            statusPulse.FillColor = new Color(SuccessGreen);
            indicator.Add(statusPulse);

            var nameLabel = InformationModel.Make<Label>("MachineName");
            nameLabel.Text = "Machine Name";
            nameLabel.Width = 130;
            nameLabel.Height = 20;
            nameLabel.TopMargin = 12;
            nameLabel.LeftMargin = 35;
            nameLabel.FontSize = 12;
            nameLabel.TextColor = new Color(TextDark);
            indicator.Add(nameLabel);

            var statusLabel = InformationModel.Make<Label>("StatusLabel");
            statusLabel.Text = "Running";
            statusLabel.Width = 130;
            statusLabel.Height = 25;
            statusLabel.TopMargin = 45;
            statusLabel.LeftMargin = 35;
            statusLabel.FontSize = 14;
            statusLabel.TextColor = new Color(SuccessGreen);
            indicator.Add(statusLabel);

            widgetFolder.Add(indicator);
            Log.Info("OEEWidgetGenerator", "Machine Status Indicator widget created successfully in UI/Widgets/StatusWidgets/MachineStatusIndicator/!");
        }
        catch (Exception ex)
        {
            Log.Error("OEEWidgetGenerator", "Error creating Machine Status Indicator widget: " + ex.Message);
        }
    }

    [ExportMethod]
    public void CreateChartPlaceholder()
    {
        try
        {
            Log.Info("OEEWidgetGenerator", "Creating Chart Placeholder widget...");
            
            // Use organized folder structure: UI/Widgets/ChartWidgets/ChartPlaceholder/
            var widgetFolder = GetWidgetFolder("ChartWidgets", "ChartPlaceholder");

            // Remove existing widget if it exists
            var existingWidget = widgetFolder.Get("ChartPlaceholder");
            if (existingWidget != null)
            {
                existingWidget.Delete();
            }

            // Create the chart placeholder widget (580x280)
            var chartPanel = InformationModel.Make<Panel>("ChartPlaceholder");
            chartPanel.Width = 580;
            chartPanel.Height = 280;

            var chartBg = InformationModel.Make<Rectangle>("ChartBackground");
            chartBg.Width = 580;
            chartBg.Height = 280;
            chartBg.FillColor = Colors.White;
            chartBg.BorderColor = new Color(BorderGray);
            chartBg.BorderThickness = 1;
            chartBg.CornerRadius = 12;
            chartPanel.Add(chartBg);

            var chartTitle = InformationModel.Make<Label>("ChartTitle");
            chartTitle.Text = "Chart Title";
            chartTitle.Width = 540;
            chartTitle.Height = 30;
            chartTitle.TopMargin = 15;
            chartTitle.LeftMargin = 20;
            chartTitle.FontSize = 16;
            chartTitle.TextColor = new Color(TextDark);
            chartPanel.Add(chartTitle);

            var chartContent = InformationModel.Make<Rectangle>("ChartContent");
            chartContent.Width = 540;
            chartContent.Height = 200;
            chartContent.TopMargin = 60;
            chartContent.LeftMargin = 20;
            chartContent.FillColor = new Color(SurfaceGray);
            chartContent.CornerRadius = 8;
            chartPanel.Add(chartContent);

            var placeholderText = InformationModel.Make<Label>("PlaceholderText");
            placeholderText.Text = "Chart Content Area";
            placeholderText.Width = 540;
            placeholderText.Height = 200;
            placeholderText.TopMargin = 60;
            placeholderText.LeftMargin = 20;
            placeholderText.FontSize = 14;
            placeholderText.TextColor = new Color(TextDark);
            placeholderText.HorizontalAlignment = HorizontalAlignment.Center;
            placeholderText.VerticalAlignment = VerticalAlignment.Center;
            chartPanel.Add(placeholderText);

            widgetFolder.Add(chartPanel);
            Log.Info("OEEWidgetGenerator", "Chart Placeholder widget created successfully in UI/Widgets/ChartWidgets/ChartPlaceholder/!");
        }
        catch (Exception ex)
        {
            Log.Error("OEEWidgetGenerator", "Error creating Chart Placeholder widget: " + ex.Message);
        }
    }

    [ExportMethod]
    public void CreateConfigSection()
    {
        try
        {
            Log.Info("OEEWidgetGenerator", "Creating Config Section widget...");
            
            // Use organized folder structure: UI/Widgets/ConfigWidgets/ConfigSection/
            var widgetFolder = GetWidgetFolder("ConfigWidgets", "ConfigSection");

            var existingWidget = widgetFolder.Get("ConfigSection");
            if (existingWidget != null)
            {
                existingWidget.Delete();
            }

            // Create configurable section widget (default 670x300)
            var sectionPanel = InformationModel.Make<Panel>("ConfigSection");
            sectionPanel.Width = 670;
            sectionPanel.Height = 300;

            // Section background
            var sectionBg = InformationModel.Make<Rectangle>("SectionBackground");
            sectionBg.Width = 670;
            sectionBg.Height = 300;
            sectionBg.FillColor = Colors.White;
            sectionBg.BorderColor = new Color(BorderGray);
            sectionBg.BorderThickness = 1;
            sectionBg.CornerRadius = 12;
            sectionPanel.Add(sectionBg);

            // Section header bar
            var headerBar = InformationModel.Make<Rectangle>("HeaderBar");
            headerBar.Width = 670;
            headerBar.Height = 40;
            headerBar.TopMargin = 0;
            headerBar.LeftMargin = 0;
            headerBar.FillColor = new Color(SuccessGreen); // Default color
            headerBar.CornerRadius = 12;
            sectionPanel.Add(headerBar);

            // Section title
            var sectionTitle = InformationModel.Make<Label>("SectionTitle");
            sectionTitle.Text = "Section Title";
            sectionTitle.Width = 630;
            sectionTitle.Height = 30;
            sectionTitle.TopMargin = 5;
            sectionTitle.LeftMargin = 20;
            sectionTitle.FontSize = 18;
            sectionTitle.TextColor = Colors.White;
            sectionPanel.Add(sectionTitle);

            widgetFolder.Add(sectionPanel);
            Log.Info("OEEWidgetGenerator", "Config Section widget created successfully in UI/Widgets/ConfigWidgets/ConfigSection/!");
        }
        catch (Exception ex)
        {
            Log.Error("OEEWidgetGenerator", "Error creating Config Section widget: " + ex.Message);
        }
    }

    [ExportMethod]
    public void CreateConfigInputField()
    {
        try
        {
            Log.Info("OEEWidgetGenerator", "Creating Config Input Field widget...");
            
            var uiFolder = Project.Current.Get("UI");
            var widgetsFolder = uiFolder.Get("Widgets");
            if (widgetsFolder == null)
            {
                widgetsFolder = InformationModel.Make<Folder>("Widgets");
                uiFolder.Add(widgetsFolder);
            }

            var oeeWidgetsFolder = widgetsFolder.Get("OEEWidgets");
            if (oeeWidgetsFolder == null)
            {
                oeeWidgetsFolder = InformationModel.Make<Folder>("OEEWidgets");
                widgetsFolder.Add(oeeWidgetsFolder);
            }

            var existingWidget = oeeWidgetsFolder.Get("ConfigInputField");
            if (existingWidget != null)
            {
                existingWidget.Delete();
            }

            // Create configurable input field widget (300x50)
            var fieldContainer = InformationModel.Make<Panel>("ConfigInputField");
            fieldContainer.Width = 300;
            fieldContainer.Height = 50;

            // Label
            var label = InformationModel.Make<Label>("FieldLabel");
            label.Text = "Field Label:";
            label.Width = 180;
            label.Height = 20;
            label.TopMargin = 0;
            label.LeftMargin = 0;
            label.FontSize = 12;
            label.TextColor = new Color(TextDark);
            fieldContainer.Add(label);

            // Input field
            var textBox = InformationModel.Make<TextBox>("FieldInput");
            textBox.Text = "Default Value";
            textBox.Width = 120;
            textBox.Height = 30;
            textBox.TopMargin = 22;
            textBox.LeftMargin = 0;
            textBox.FontSize = 12;
            fieldContainer.Add(textBox);

            // Units label
            var unitsLabel = InformationModel.Make<Label>("FieldUnits");
            unitsLabel.Text = "units";
            unitsLabel.Width = 80;
            unitsLabel.Height = 20;
            unitsLabel.TopMargin = 27;
            unitsLabel.LeftMargin = 130;
            unitsLabel.FontSize = 10;
            unitsLabel.TextColor = new Color(TextLight);
            fieldContainer.Add(unitsLabel);

            oeeWidgetsFolder.Add(fieldContainer);
            Log.Info("OEEWidgetGenerator", "Config Input Field widget created successfully in UI/Widgets/OEEWidgets!");
        }
        catch (Exception ex)
        {
            Log.Error("OEEWidgetGenerator", "Error creating Config Input Field widget: " + ex.Message);
        }
    }

    [ExportMethod]
    public void CreateDataField()
    {
        try
        {
            Log.Info("OEEWidgetGenerator", "Creating Data Field widget...");
            
            var uiFolder = Project.Current.Get("UI");
            var widgetsFolder = uiFolder.Get("Widgets");
            if (widgetsFolder == null)
            {
                widgetsFolder = InformationModel.Make<Folder>("Widgets");
                uiFolder.Add(widgetsFolder);
            }

            var oeeWidgetsFolder = widgetsFolder.Get("OEEWidgets");
            if (oeeWidgetsFolder == null)
            {
                oeeWidgetsFolder = InformationModel.Make<Folder>("OEEWidgets");
                widgetsFolder.Add(oeeWidgetsFolder);
            }

            var existingWidget = oeeWidgetsFolder.Get("DataField");
            if (existingWidget != null)
            {
                existingWidget.Delete();
            }

            // Create data field widget (280x50)
            var fieldContainer = InformationModel.Make<Panel>("DataField");
            fieldContainer.Width = 280;
            fieldContainer.Height = 50;

            var label = InformationModel.Make<Label>("DataLabel");
            label.Text = "Data Label:";
            label.Width = 150;
            label.Height = 20;
            label.TopMargin = 0;
            label.LeftMargin = 0;
            label.FontSize = 12;
            label.TextColor = new Color(TextDark);
            fieldContainer.Add(label);

            var valueLabel = InformationModel.Make<Label>("DataValue");
            valueLabel.Text = "0.0";
            valueLabel.Width = 80;
            valueLabel.Height = 25;
            valueLabel.TopMargin = 20;
            valueLabel.LeftMargin = 0;
            valueLabel.FontSize = 14;
            valueLabel.TextColor = new Color(PrimaryBlue);
            fieldContainer.Add(valueLabel);

            var unitsLabel = InformationModel.Make<Label>("DataUnits");
            unitsLabel.Text = "units";
            unitsLabel.Width = 60;
            unitsLabel.Height = 20;
            unitsLabel.TopMargin = 23;
            unitsLabel.LeftMargin = 85;
            unitsLabel.FontSize = 10;
            unitsLabel.TextColor = new Color(TextLight);
            fieldContainer.Add(unitsLabel);

            oeeWidgetsFolder.Add(fieldContainer);
            Log.Info("OEEWidgetGenerator", "Data Field widget created successfully in UI/Widgets/OEEWidgets!");
        }
        catch (Exception ex)
        {
            Log.Error("OEEWidgetGenerator", "Error creating Data Field widget: " + ex.Message);
        }
    }

    [ExportMethod]
    public void CreateLiveCounter()
    {
        try
        {
            Log.Info("OEEWidgetGenerator", "Creating Live Counter widget...");
            
            var uiFolder = Project.Current.Get("UI");
            var widgetsFolder = uiFolder.Get("Widgets");
            if (widgetsFolder == null)
            {
                widgetsFolder = InformationModel.Make<Folder>("Widgets");
                uiFolder.Add(widgetsFolder);
            }

            var oeeWidgetsFolder = widgetsFolder.Get("OEEWidgets");
            if (oeeWidgetsFolder == null)
            {
                oeeWidgetsFolder = InformationModel.Make<Folder>("OEEWidgets");
                widgetsFolder.Add(oeeWidgetsFolder);
            }

            var existingWidget = oeeWidgetsFolder.Get("LiveCounter");
            if (existingWidget != null)
            {
                existingWidget.Delete();
            }

            // Create live counter widget (180x120)
            var counterPanel = InformationModel.Make<Panel>("LiveCounter");
            counterPanel.Width = 180;
            counterPanel.Height = 120;

            var counterBg = InformationModel.Make<Rectangle>("CounterBackground");
            counterBg.Width = 180;
            counterBg.Height = 120;
            counterBg.FillColor = Colors.White;
            counterBg.BorderColor = new Color(PrimaryBlue);
            counterBg.BorderThickness = 2;
            counterBg.CornerRadius = 8;
            counterPanel.Add(counterBg);

            var titleLabel = InformationModel.Make<Label>("CounterTitle");
            titleLabel.Text = "Counter Title";
            titleLabel.Width = 160;
            titleLabel.Height = 20;
            titleLabel.TopMargin = 10;
            titleLabel.LeftMargin = 10;
            titleLabel.FontSize = 12;
            titleLabel.TextColor = new Color(TextDark);
            counterPanel.Add(titleLabel);

            var valueLabel = InformationModel.Make<Label>("CounterValue");
            valueLabel.Text = "0";
            valueLabel.Width = 160;
            valueLabel.Height = 40;
            valueLabel.TopMargin = 40;
            valueLabel.LeftMargin = 10;
            valueLabel.FontSize = 24;
            valueLabel.TextColor = new Color(PrimaryBlue);
            counterPanel.Add(valueLabel);

            var statusIndicator = InformationModel.Make<Ellipse>("CounterStatusIndicator");
            statusIndicator.Width = 10;
            statusIndicator.Height = 10;
            statusIndicator.TopMargin = 95;
            statusIndicator.LeftMargin = 15;
            statusIndicator.FillColor = new Color(SuccessGreen);
            counterPanel.Add(statusIndicator);

            oeeWidgetsFolder.Add(counterPanel);
            Log.Info("OEEWidgetGenerator", "Live Counter widget created successfully in UI/Widgets/OEEWidgets!");
        }
        catch (Exception ex)
        {
            Log.Error("OEEWidgetGenerator", "Error creating Live Counter widget: " + ex.Message);
        }
    }

    [ExportMethod]
    public void CreateStatusIndicator()
    {
        try
        {
            Log.Info("OEEWidgetGenerator", "Creating Status Indicator widget...");
            
            var uiFolder = Project.Current.Get("UI");
            var widgetsFolder = uiFolder.Get("Widgets");
            if (widgetsFolder == null)
            {
                widgetsFolder = InformationModel.Make<Folder>("Widgets");
                uiFolder.Add(widgetsFolder);
            }

            var oeeWidgetsFolder = widgetsFolder.Get("OEEWidgets");
            if (oeeWidgetsFolder == null)
            {
                oeeWidgetsFolder = InformationModel.Make<Folder>("OEEWidgets");
                widgetsFolder.Add(oeeWidgetsFolder);
            }

            var existingWidget = oeeWidgetsFolder.Get("StatusIndicator");
            if (existingWidget != null)
            {
                existingWidget.Delete();
            }

            // Create status indicator widget (300x40)
            var statusPanel = InformationModel.Make<Panel>("StatusIndicator");
            statusPanel.Width = 300;
            statusPanel.Height = 40;

            var titleLabel = InformationModel.Make<Label>("StatusTitle");
            titleLabel.Text = "Status Title:";
            titleLabel.Width = 150;
            titleLabel.Height = 20;
            titleLabel.TopMargin = 10;
            titleLabel.LeftMargin = 0;
            titleLabel.FontSize = 12;
            titleLabel.TextColor = new Color(TextDark);
            statusPanel.Add(titleLabel);

            var statusIndicator = InformationModel.Make<Ellipse>("StatusPulse");
            statusIndicator.Width = 12;
            statusIndicator.Height = 12;
            statusIndicator.TopMargin = 14;
            statusIndicator.LeftMargin = 160;
            statusIndicator.FillColor = new Color(SuccessGreen);
            statusPanel.Add(statusIndicator);

            var valueLabel = InformationModel.Make<Label>("StatusValue");
            valueLabel.Text = "Active";
            valueLabel.Width = 100;
            valueLabel.Height = 20;
            valueLabel.TopMargin = 10;
            valueLabel.LeftMargin = 180;
            valueLabel.FontSize = 12;
            valueLabel.TextColor = new Color(SuccessGreen);
            statusPanel.Add(valueLabel);

            oeeWidgetsFolder.Add(statusPanel);
            Log.Info("OEEWidgetGenerator", "Status Indicator widget created successfully in UI/Widgets/OEEWidgets!");
        }
        catch (Exception ex)
        {
            Log.Error("OEEWidgetGenerator", "Error creating Status Indicator widget: " + ex.Message);
        }
    }

    [ExportMethod]
    public void CreateTargetComparison()
    {
        try
        {
            Log.Info("OEEWidgetGenerator", "Creating Target Comparison widget...");
            
            var uiFolder = Project.Current.Get("UI");
            var widgetsFolder = uiFolder.Get("Widgets");
            if (widgetsFolder == null)
            {
                widgetsFolder = InformationModel.Make<Folder>("Widgets");
                uiFolder.Add(widgetsFolder);
            }

            var oeeWidgetsFolder = widgetsFolder.Get("OEEWidgets");
            if (oeeWidgetsFolder == null)
            {
                oeeWidgetsFolder = InformationModel.Make<Folder>("OEEWidgets");
                widgetsFolder.Add(oeeWidgetsFolder);
            }

            var existingWidget = oeeWidgetsFolder.Get("TargetComparison");
            if (existingWidget != null)
            {
                existingWidget.Delete();
            }

            // Create target comparison widget (300x180)
            var comparisonPanel = InformationModel.Make<Panel>("TargetComparison");
            comparisonPanel.Width = 300;
            comparisonPanel.Height = 180;

            var comparisonBg = InformationModel.Make<Rectangle>("ComparisonBackground");
            comparisonBg.Width = 300;
            comparisonBg.Height = 180;
            comparisonBg.FillColor = Colors.White;
            comparisonBg.BorderColor = new Color(PrimaryBlue);
            comparisonBg.BorderThickness = 2;
            comparisonBg.CornerRadius = 8;
            comparisonPanel.Add(comparisonBg);

            var titleLabel = InformationModel.Make<Label>("ComparisonTitle");
            titleLabel.Text = "Metric Name";
            titleLabel.Width = 280;
            titleLabel.Height = 25;
            titleLabel.TopMargin = 10;
            titleLabel.LeftMargin = 10;
            titleLabel.FontSize = 16;
            titleLabel.TextColor = new Color(PrimaryBlue);
            comparisonPanel.Add(titleLabel);

            // Actual value
            var actualLabel = InformationModel.Make<Label>("ActualLabel");
            actualLabel.Text = "Actual:";
            actualLabel.Width = 80;
            actualLabel.Height = 20;
            actualLabel.TopMargin = 47;
            actualLabel.LeftMargin = 10;
            actualLabel.FontSize = 12;
            actualLabel.TextColor = new Color(TextDark);
            comparisonPanel.Add(actualLabel);

            var actualValue = InformationModel.Make<Label>("ActualValue");
            actualValue.Text = "87.5%";
            actualValue.Width = 100;
            actualValue.Height = 20;
            actualValue.TopMargin = 47;
            actualValue.LeftMargin = 100;
            actualValue.FontSize = 14;
            actualValue.TextColor = new Color(PrimaryBlue);
            comparisonPanel.Add(actualValue);

            // Target value
            var targetLabel = InformationModel.Make<Label>("TargetLabel");
            targetLabel.Text = "Target:";
            targetLabel.Width = 80;
            targetLabel.Height = 20;
            targetLabel.TopMargin = 77;
            targetLabel.LeftMargin = 10;
            targetLabel.FontSize = 12;
            targetLabel.TextColor = new Color(TextDark);
            comparisonPanel.Add(targetLabel);

            var targetValue = InformationModel.Make<Label>("TargetValue");
            targetValue.Text = "85.0%";
            targetValue.Width = 100;
            targetValue.Height = 20;
            targetValue.TopMargin = 77;
            targetValue.LeftMargin = 100;
            targetValue.FontSize = 14;
            targetValue.TextColor = new Color(TextDark);
            comparisonPanel.Add(targetValue);

            // Variance
            var varianceLabel = InformationModel.Make<Label>("VarianceLabel");
            varianceLabel.Text = "Variance:";
            varianceLabel.Width = 80;
            varianceLabel.Height = 20;
            varianceLabel.TopMargin = 107;
            varianceLabel.LeftMargin = 10;
            varianceLabel.FontSize = 12;
            varianceLabel.TextColor = new Color(TextDark);
            comparisonPanel.Add(varianceLabel);

            var varianceValue = InformationModel.Make<Label>("VarianceValue");
            varianceValue.Text = "+2.5%";
            varianceValue.Width = 100;
            varianceValue.Height = 20;
            varianceValue.TopMargin = 107;
            varianceValue.LeftMargin = 100;
            varianceValue.FontSize = 14;
            varianceValue.TextColor = new Color(SuccessGreen);
            comparisonPanel.Add(varianceValue);

            // Status indicator
            var statusLabel = InformationModel.Make<Label>("ComparisonStatus");
            statusLabel.Text = "✓ Above Target";
            statusLabel.Width = 250;
            statusLabel.Height = 20;
            statusLabel.TopMargin = 142;
            statusLabel.LeftMargin = 10;
            statusLabel.FontSize = 12;
            statusLabel.TextColor = new Color(SuccessGreen);
            comparisonPanel.Add(statusLabel);

            oeeWidgetsFolder.Add(comparisonPanel);
            Log.Info("OEEWidgetGenerator", "Target Comparison widget created successfully in UI/Widgets/OEEWidgets!");
        }
        catch (Exception ex)
        {
            Log.Error("OEEWidgetGenerator", "Error creating Target Comparison widget: " + ex.Message);
        }
    }

    [ExportMethod]
    public void CreateTrendDisplay()
    {
        try
        {
            Log.Info("OEEWidgetGenerator", "Creating Trend Display widget...");
            
            var uiFolder = Project.Current.Get("UI");
            var widgetsFolder = uiFolder.Get("Widgets");
            if (widgetsFolder == null)
            {
                widgetsFolder = InformationModel.Make<Folder>("Widgets");
                uiFolder.Add(widgetsFolder);
            }

            var oeeWidgetsFolder = widgetsFolder.Get("OEEWidgets");
            if (oeeWidgetsFolder == null)
            {
                oeeWidgetsFolder = InformationModel.Make<Folder>("OEEWidgets");
                widgetsFolder.Add(oeeWidgetsFolder);
            }

            var existingWidget = oeeWidgetsFolder.Get("TrendDisplay");
            if (existingWidget != null)
            {
                existingWidget.Delete();
            }

            // Create trend display widget (300x40)
            var trendPanel = InformationModel.Make<Panel>("TrendDisplay");
            trendPanel.Width = 300;
            trendPanel.Height = 40;

            var titleLabel = InformationModel.Make<Label>("TrendTitle");
            titleLabel.Text = "Trend Title:";
            titleLabel.Width = 150;
            titleLabel.Height = 20;
            titleLabel.TopMargin = 5;
            titleLabel.LeftMargin = 0;
            titleLabel.FontSize = 12;
            titleLabel.TextColor = new Color(TextDark);
            trendPanel.Add(titleLabel);

            var trendLabel = InformationModel.Make<Label>("TrendValue");
            trendLabel.Text = "Stable";
            trendLabel.Width = 100;
            trendLabel.Height = 20;
            trendLabel.TopMargin = 5;
            trendLabel.LeftMargin = 160;
            trendLabel.FontSize = 12;
            trendLabel.TextColor = new Color(SuccessGreen);
            trendPanel.Add(trendLabel);

            // Trend arrow
            var arrow = InformationModel.Make<Label>("TrendArrow");
            arrow.Text = "→";
            arrow.Width = 20;
            arrow.Height = 20;
            arrow.TopMargin = 5;
            arrow.LeftMargin = 270;
            arrow.FontSize = 14;
            arrow.TextColor = new Color(SuccessGreen);
            trendPanel.Add(arrow);

            oeeWidgetsFolder.Add(trendPanel);
            Log.Info("OEEWidgetGenerator", "Trend Display widget created successfully in UI/Widgets/OEEWidgets!");
        }
        catch (Exception ex)
        {
            Log.Error("OEEWidgetGenerator", "Error creating Trend Display widget: " + ex.Message);
        }
    }

    [ExportMethod]
    public void CreateHeaderPanel()
    {
        try
        {
            Log.Info("OEEWidgetGenerator", "Creating Header Panel widget...");
            
            var uiFolder = Project.Current.Get("UI");
            var widgetsFolder = uiFolder.Get("Widgets");
            if (widgetsFolder == null)
            {
                widgetsFolder = InformationModel.Make<Folder>("Widgets");
                uiFolder.Add(widgetsFolder);
            }

            var oeeWidgetsFolder = widgetsFolder.Get("OEEWidgets");
            if (oeeWidgetsFolder == null)
            {
                oeeWidgetsFolder = InformationModel.Make<Folder>("OEEWidgets");
                widgetsFolder.Add(oeeWidgetsFolder);
            }

            var existingWidget = oeeWidgetsFolder.Get("HeaderPanel");
            if (existingWidget != null)
            {
                existingWidget.Delete();
            }

            // Create header panel widget (1380x100)
            var headerPanel = InformationModel.Make<Panel>("HeaderPanel");
            headerPanel.Width = 1380;
            headerPanel.Height = 100;

            var headerBg = InformationModel.Make<Rectangle>("HeaderBackground");
            headerBg.Width = 1380;
            headerBg.Height = 100;
            headerBg.FillColor = new Color(PrimaryBlue); // Default color
            headerBg.CornerRadius = 12;
            headerPanel.Add(headerBg);

            // Icon placeholder
            var headerIcon = InformationModel.Make<Image>("HeaderIcon");
            headerIcon.Width = 20;
            headerIcon.Height = 20;
            headerIcon.TopMargin = 32;
            headerIcon.LeftMargin = 30;
            headerIcon.Path = new ResourceUri("%APPLICATIONDIR%/Graphics/default-icon.svg");
            headerPanel.Add(headerIcon);

            var titleLabel = InformationModel.Make<Label>("HeaderTitle");
            titleLabel.Text = "Screen Title";
            titleLabel.Width = 700;
            titleLabel.Height = 40;
            titleLabel.TopMargin = 20;
            titleLabel.LeftMargin = 60;
            titleLabel.FontSize = 28;
            titleLabel.TextColor = Colors.White;
            headerPanel.Add(titleLabel);

            var subtitleLabel = InformationModel.Make<Label>("HeaderSubtitle");
            subtitleLabel.Text = "Screen subtitle and description";
            subtitleLabel.Width = 800;
            subtitleLabel.Height = 25;
            subtitleLabel.TopMargin = 65;
            subtitleLabel.LeftMargin = 60;
            subtitleLabel.FontSize = 14;
            subtitleLabel.TextColor = Colors.White;
            headerPanel.Add(subtitleLabel);

            oeeWidgetsFolder.Add(headerPanel);
            Log.Info("OEEWidgetGenerator", "Header Panel widget created successfully in UI/Widgets/OEEWidgets!");
        }
        catch (Exception ex)
        {
            Log.Error("OEEWidgetGenerator", "Error creating Header Panel widget: " + ex.Message);
        }
    }

    [ExportMethod]
    public void ShowAvailableWidgets()
    {
        Log.Info("OEEWidgetGenerator", "=== AVAILABLE OEE WIDGETS ===");
        Log.Info("OEEWidgetGenerator", "");
        Log.Info("OEEWidgetGenerator", "[METRIC] OEEMetricCard (280x180)");
        Log.Info("OEEWidgetGenerator", "   - Card with title, value, trend, and arrow indicator");
        Log.Info("OEEWidgetGenerator", "   - Used for OEE, Quality, Performance, Availability displays");
        Log.Info("OEEWidgetGenerator", "   - Location: UI/Widgets/MetricWidgets/OEEMetricCard/");
        Log.Info("OEEWidgetGenerator", "");
        Log.Info("OEEWidgetGenerator", "[STATUS] MachineStatusIndicator (170x80)");
        Log.Info("OEEWidgetGenerator", "   - Machine status with pulse indicator and name/status labels");
        Log.Info("OEEWidgetGenerator", "   - Used for production line status displays");
        Log.Info("OEEWidgetGenerator", "   - Location: UI/Widgets/StatusWidgets/MachineStatusIndicator/");
        Log.Info("OEEWidgetGenerator", "");
        Log.Info("OEEWidgetGenerator", "[CHART] ChartPlaceholder (580x280)");
        Log.Info("OEEWidgetGenerator", "   - Chart container with title and content area");
        Log.Info("OEEWidgetGenerator", "   - Used for trend charts and analysis displays");
        Log.Info("OEEWidgetGenerator", "   - Location: UI/Widgets/ChartWidgets/ChartPlaceholder/");
        Log.Info("OEEWidgetGenerator", "");
        Log.Info("OEEWidgetGenerator", "[CONFIG] ConfigSection (670x300)");
        Log.Info("OEEWidgetGenerator", "   - Configuration section with header bar and title");
        Log.Info("OEEWidgetGenerator", "   - Used for organizing configuration groups");
        Log.Info("OEEWidgetGenerator", "   - Location: UI/Widgets/ConfigWidgets/ConfigSection/");
        Log.Info("OEEWidgetGenerator", "");
        Log.Info("OEEWidgetGenerator", "[INPUT] ConfigInputField (300x50)");
        Log.Info("OEEWidgetGenerator", "   - Input field with label, textbox, and units");
        Log.Info("OEEWidgetGenerator", "   - Used for configuration parameter entry");
        Log.Info("OEEWidgetGenerator", "   - Location: UI/Widgets/ConfigWidgets/ConfigInputField/");
        Log.Info("OEEWidgetGenerator", "");
        Log.Info("OEEWidgetGenerator", "[DATA] DataField (280x50)");
        Log.Info("OEEWidgetGenerator", "   - Data display field with label, value, and units");
        Log.Info("OEEWidgetGenerator", "   - Used for read-only data monitoring");
        Log.Info("OEEWidgetGenerator", "   - Location: UI/Widgets/DataWidgets/DataField/");
        Log.Info("OEEWidgetGenerator", "");
        Log.Info("OEEWidgetGenerator", "[COUNTER] LiveCounter (180x120)");
        Log.Info("OEEWidgetGenerator", "   - Production counter with title, value, and status pulse");
        Log.Info("OEEWidgetGenerator", "   - Used for live production data displays");
        Log.Info("OEEWidgetGenerator", "   - Location: UI/Widgets/CounterWidgets/LiveCounter/");
        Log.Info("OEEWidgetGenerator", "");
        Log.Info("OEEWidgetGenerator", "[STATUS] StatusIndicator (300x40)");
        Log.Info("OEEWidgetGenerator", "   - Status indicator with title, pulse, and value");
        Log.Info("OEEWidgetGenerator", "   - Used for system health monitoring");
        Log.Info("OEEWidgetGenerator", "   - Location: UI/Widgets/StatusWidgets/StatusIndicator/");
        Log.Info("OEEWidgetGenerator", "");
        Log.Info("OEEWidgetGenerator", "[COMPARE] TargetComparison (300x180)");
        Log.Info("OEEWidgetGenerator", "   - Target vs actual comparison with variance and status");
        Log.Info("OEEWidgetGenerator", "   - Used for performance target analysis");
        Log.Info("OEEWidgetGenerator", "   - Location: UI/Widgets/ComparisonWidgets/TargetComparison/");
        Log.Info("OEEWidgetGenerator", "");
        Log.Info("OEEWidgetGenerator", "[TREND] TrendDisplay (300x40)");
        Log.Info("OEEWidgetGenerator", "   - Trend display with title, value, and arrow");
        Log.Info("OEEWidgetGenerator", "   - Used for performance trend monitoring");
        Log.Info("OEEWidgetGenerator", "   - Location: UI/Widgets/TrendWidgets/TrendDisplay/");
        Log.Info("OEEWidgetGenerator", "");
        Log.Info("OEEWidgetGenerator", "[HEADER] HeaderPanel (1380x100)");
        Log.Info("OEEWidgetGenerator", "   - Screen header with icon, title, and subtitle");
        Log.Info("OEEWidgetGenerator", "   - Used for consistent screen headers");
        Log.Info("OEEWidgetGenerator", "   - Location: UI/Widgets/HeaderWidgets/HeaderPanel/");
        Log.Info("OEEWidgetGenerator", "");
        Log.Info("OEEWidgetGenerator", "=== ORGANIZED FOLDER STRUCTURE ===");
        Log.Info("OEEWidgetGenerator", "📁 UI/Widgets/");
        Log.Info("OEEWidgetGenerator", "  ├── 📁 MetricWidgets/");
        Log.Info("OEEWidgetGenerator", "  │   └── 📁 OEEMetricCard/");
        Log.Info("OEEWidgetGenerator", "  ├── 📁 StatusWidgets/");
        Log.Info("OEEWidgetGenerator", "  │   ├── 📁 MachineStatusIndicator/");
        Log.Info("OEEWidgetGenerator", "  │   └── 📁 StatusIndicator/");
        Log.Info("OEEWidgetGenerator", "  ├── 📁 ChartWidgets/");
        Log.Info("OEEWidgetGenerator", "  │   └── 📁 ChartPlaceholder/");
        Log.Info("OEEWidgetGenerator", "  ├── 📁 ConfigWidgets/");
        Log.Info("OEEWidgetGenerator", "  │   ├── 📁 ConfigSection/");
        Log.Info("OEEWidgetGenerator", "  │   └── 📁 ConfigInputField/");
        Log.Info("OEEWidgetGenerator", "  ├── 📁 DataWidgets/");
        Log.Info("OEEWidgetGenerator", "  │   └── 📁 DataField/");
        Log.Info("OEEWidgetGenerator", "  ├── 📁 CounterWidgets/");
        Log.Info("OEEWidgetGenerator", "  │   └── 📁 LiveCounter/");
        Log.Info("OEEWidgetGenerator", "  ├── 📁 ComparisonWidgets/");
        Log.Info("OEEWidgetGenerator", "  │   └── 📁 TargetComparison/");
        Log.Info("OEEWidgetGenerator", "  ├── 📁 TrendWidgets/");
        Log.Info("OEEWidgetGenerator", "  │   └── 📁 TrendDisplay/");
        Log.Info("OEEWidgetGenerator", "  └── 📁 HeaderWidgets/");
        Log.Info("OEEWidgetGenerator", "      └── 📁 HeaderPanel/");
        Log.Info("OEEWidgetGenerator", "");
        Log.Info("OEEWidgetGenerator", "=== WIDGET USAGE ===");
        Log.Info("OEEWidgetGenerator", "[CREATE] CreateAllOEEWidgets() - Creates all widgets in organized folders");
        Log.Info("OEEWidgetGenerator", "[CLEAR] DeleteAllOEEWidgets() - Removes all organized widget folders");
        Log.Info("OEEWidgetGenerator", "[INDIVIDUAL] Each widget has individual Create method");
        Log.Info("OEEWidgetGenerator", "");
        Log.Info("OEEWidgetGenerator", "✅ Organized by widget type for better maintainability");
        Log.Info("OEEWidgetGenerator", "✅ Each widget in its own dedicated folder");
        Log.Info("OEEWidgetGenerator", "✅ Easy to locate and manage individual widgets");
    }

    // Helper methods for widget instantiation (to be used by ModernUIGenerator)
    public Panel CreateOEEMetricCardInstance(string title, string value, string trend, Color trendColor, bool trendUp)
    {
        // Create instance of OEE metric card with specific values
        var card = InformationModel.Make<Panel>("Card_" + title.Replace(" ", ""));
        card.Width = 280;
        card.Height = 180;

        var cardBg = InformationModel.Make<Rectangle>("CardBackground");
        cardBg.Width = 280;
        cardBg.Height = 180;
        cardBg.FillColor = Colors.White;
        cardBg.BorderColor = new Color(BorderGray);
        cardBg.BorderThickness = 1;
        cardBg.CornerRadius = 16;
        card.Add(cardBg);

        var accentBar = InformationModel.Make<Rectangle>("AccentBar");
        accentBar.Width = 4;
        accentBar.Height = 180;
        accentBar.LeftMargin = 0;
        accentBar.TopMargin = 0;
        accentBar.FillColor = new Color(AccentTeal);
        accentBar.CornerRadius = 2;
        card.Add(accentBar);

        var titleLabel = InformationModel.Make<Label>("CardTitle");
        titleLabel.Text = title;
        titleLabel.Width = 250;
        titleLabel.Height = 25;
        titleLabel.TopMargin = 20;
        titleLabel.LeftMargin = 20;
        titleLabel.FontSize = 14;
        titleLabel.TextColor = new Color(TextDark);
        card.Add(titleLabel);

        var valueLabel = InformationModel.Make<Label>("CardValue");
        valueLabel.Text = value;
        valueLabel.Width = 250;
        valueLabel.Height = 50;
        valueLabel.TopMargin = 60;
        valueLabel.LeftMargin = 20;
        valueLabel.FontSize = 36;
        valueLabel.TextColor = new Color(PrimaryBlue);
        card.Add(valueLabel);

        // Add trend arrow (chevron style)
        var arrowStem = InformationModel.Make<Rectangle>("TrendArrowStem");
        arrowStem.Width = 2;
        arrowStem.Height = 10;
        arrowStem.TopMargin = 135;
        arrowStem.LeftMargin = 25;
        arrowStem.FillColor = trendColor;
        card.Add(arrowStem);

        if (trendUp)
        {
            var arrowLeft = InformationModel.Make<Rectangle>("TrendArrowLeft");
            arrowLeft.Width = 6;
            arrowLeft.Height = 2;
            arrowLeft.TopMargin = 135;
            arrowLeft.LeftMargin = 21;
            arrowLeft.FillColor = trendColor;
            arrowLeft.Rotation = -45;
            card.Add(arrowLeft);

            var arrowRight = InformationModel.Make<Rectangle>("TrendArrowRight");
            arrowRight.Width = 6;
            arrowRight.Height = 2;
            arrowRight.TopMargin = 135;
            arrowRight.LeftMargin = 27;
            arrowRight.FillColor = trendColor;
            arrowRight.Rotation = 45;
            card.Add(arrowRight);
        }
        else
        {
            var arrowLeft = InformationModel.Make<Rectangle>("TrendArrowLeft");
            arrowLeft.Width = 6;
            arrowLeft.Height = 2;
            arrowLeft.TopMargin = 143;
            arrowLeft.LeftMargin = 21;
            arrowLeft.FillColor = trendColor;
            arrowLeft.Rotation = 45;
            card.Add(arrowLeft);

            var arrowRight = InformationModel.Make<Rectangle>("TrendArrowRight");
            arrowRight.Width = 6;
            arrowRight.Height = 2;
            arrowRight.TopMargin = 143;
            arrowRight.LeftMargin = 27;
            arrowRight.FillColor = trendColor;
            arrowRight.Rotation = -45;
            card.Add(arrowRight);
        }

        var trendLabel = InformationModel.Make<Label>("CardTrend");
        trendLabel.Text = trend;
        trendLabel.Width = 220;
        trendLabel.Height = 25;
        trendLabel.TopMargin = 130;
        trendLabel.LeftMargin = 45;
        trendLabel.FontSize = 14;
        trendLabel.TextColor = trendColor;
        card.Add(trendLabel);

        return card;
    }

    public Panel CreateMachineStatusIndicatorInstance(string lineName, string status, Color statusColor)
    {
        var indicator = InformationModel.Make<Panel>("Indicator_" + lineName.Replace(" ", ""));
        indicator.Width = 170;
        indicator.Height = 80;

        var indicatorBg = InformationModel.Make<Rectangle>("IndicatorBg");
        indicatorBg.Width = 170;
        indicatorBg.Height = 80;
        indicatorBg.FillColor = new Color(SurfaceGray);
        indicatorBg.BorderColor = statusColor;
        indicatorBg.BorderThickness = 2;
        indicatorBg.CornerRadius = 12;
        indicator.Add(indicatorBg);

        var pulse = InformationModel.Make<Ellipse>("StatusPulse");
        pulse.Width = 12;
        pulse.Height = 12;
        pulse.TopMargin = 15;
        pulse.LeftMargin = 15;
        pulse.FillColor = statusColor;
        indicator.Add(pulse);

        var nameLabel = InformationModel.Make<Label>("LineName");
        nameLabel.Text = lineName;
        nameLabel.Width = 130;
        nameLabel.Height = 20;
        nameLabel.TopMargin = 12;
        nameLabel.LeftMargin = 35;
        nameLabel.FontSize = 12;
        nameLabel.TextColor = new Color(TextDark);
        indicator.Add(nameLabel);

        var statusLabel = InformationModel.Make<Label>("Status");
        statusLabel.Text = status;
        statusLabel.Width = 130;
        statusLabel.Height = 18;
        statusLabel.TopMargin = 35;
        statusLabel.LeftMargin = 35;
        statusLabel.FontSize = 11;
        statusLabel.TextColor = statusColor;
        indicator.Add(statusLabel);

        return indicator;
    }

    [ExportMethod]
    public void DeleteAllOEEWidgets()
    {
        try
        {
            Log.Info("OEEWidgetGenerator", "Deleting all OEE widgets...");
            
            var uiFolder = Project.Current.Get("UI");
            var widgetsFolder = uiFolder.Get("Widgets");
            if (widgetsFolder != null)
            {
                // Delete organized widget folders
                var categoryFolders = new string[] { 
                    "MetricWidgets", 
                    "StatusWidgets", 
                    "ChartWidgets", 
                    "ConfigWidgets", 
                    "DataWidgets", 
                    "CounterWidgets", 
                    "ComparisonWidgets", 
                    "TrendWidgets", 
                    "HeaderWidgets" 
                };
                
                foreach (var categoryName in categoryFolders)
                {
                    var categoryFolder = widgetsFolder.Get(categoryName);
                    if (categoryFolder != null)
                    {
                        categoryFolder.Delete();
                        Log.Info("OEEWidgetGenerator", $"Deleted widget category: {categoryName}");
                    }
                }
                
                // Also clean up old OEEWidgets folder if it exists
                var oldOeeWidgetsFolder = widgetsFolder.Get("OEEWidgets");
                if (oldOeeWidgetsFolder != null)
                {
                    oldOeeWidgetsFolder.Delete();
                    Log.Info("OEEWidgetGenerator", "Deleted legacy OEEWidgets folder");
                }
                
                Log.Info("OEEWidgetGenerator", "All OEE widgets deleted successfully!");
            }
        }
        catch (Exception ex)
        {
            Log.Error("OEEWidgetGenerator", "Error deleting OEE widgets: " + ex.Message);
        }
    }
}
