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

    [ExportMethod]
    public void CreateAllOEEWidgets()
    {
        try
        {
            Log.Info("OEEWidgetGenerator", "Creating all OEE widgets...");
            CreateOEEMetricCard();
            CreateMachineStatusIndicator();
            CreateChartPlaceholder();
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
            
            // Ensure Widgets folder exists
            var uiFolder = Project.Current.Get("UI");
            var widgetsFolder = uiFolder.Get("Widgets");
            if (widgetsFolder == null)
            {
                widgetsFolder = InformationModel.Make<Folder>("Widgets");
                uiFolder.Add(widgetsFolder);
            }

            // Ensure OEEWidgets folder exists
            var oeeWidgetsFolder = widgetsFolder.Get("OEEWidgets");
            if (oeeWidgetsFolder == null)
            {
                oeeWidgetsFolder = InformationModel.Make<Folder>("OEEWidgets");
                widgetsFolder.Add(oeeWidgetsFolder);
            }

            // Remove existing widget if it exists
            var existingWidget = oeeWidgetsFolder.Get("OEEMetricCard");
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

            oeeWidgetsFolder.Add(card);
            Log.Info("OEEWidgetGenerator", "OEE Metric Card widget created successfully in UI/Widgets/OEEWidgets!");
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
            
            // Ensure Widgets folder exists
            var uiFolder = Project.Current.Get("UI");
            var widgetsFolder = uiFolder.Get("Widgets");
            if (widgetsFolder == null)
            {
                widgetsFolder = InformationModel.Make<Folder>("Widgets");
                uiFolder.Add(widgetsFolder);
            }

            // Ensure OEEWidgets folder exists
            var oeeWidgetsFolder = widgetsFolder.Get("OEEWidgets");
            if (oeeWidgetsFolder == null)
            {
                oeeWidgetsFolder = InformationModel.Make<Folder>("OEEWidgets");
                widgetsFolder.Add(oeeWidgetsFolder);
            }

            // Remove existing widget if it exists
            var existingWidget = oeeWidgetsFolder.Get("MachineStatusIndicator");
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

            oeeWidgetsFolder.Add(indicator);
            Log.Info("OEEWidgetGenerator", "Machine Status Indicator widget created successfully in UI/Widgets/OEEWidgets!");
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
            
            // Ensure Widgets folder exists
            var uiFolder = Project.Current.Get("UI");
            var widgetsFolder = uiFolder.Get("Widgets");
            if (widgetsFolder == null)
            {
                widgetsFolder = InformationModel.Make<Folder>("Widgets");
                uiFolder.Add(widgetsFolder);
            }

            // Ensure OEEWidgets folder exists
            var oeeWidgetsFolder = widgetsFolder.Get("OEEWidgets");
            if (oeeWidgetsFolder == null)
            {
                oeeWidgetsFolder = InformationModel.Make<Folder>("OEEWidgets");
                widgetsFolder.Add(oeeWidgetsFolder);
            }

            // Remove existing widget if it exists
            var existingWidget = oeeWidgetsFolder.Get("ChartPlaceholder");
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

            oeeWidgetsFolder.Add(chartPanel);
            Log.Info("OEEWidgetGenerator", "Chart Placeholder widget created successfully in UI/Widgets/OEEWidgets!");
        }
        catch (Exception ex)
        {
            Log.Error("OEEWidgetGenerator", "Error creating Chart Placeholder widget: " + ex.Message);
        }
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
                var oeeWidgetsFolder = widgetsFolder.Get("OEEWidgets");
                if (oeeWidgetsFolder != null)
                {
                    oeeWidgetsFolder.Delete();
                    Log.Info("OEEWidgetGenerator", "All OEE widgets deleted successfully!");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error("OEEWidgetGenerator", "Error deleting OEE widgets: " + ex.Message);
        }
    }
}
