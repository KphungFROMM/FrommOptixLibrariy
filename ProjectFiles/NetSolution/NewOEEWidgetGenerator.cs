#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.NativeUI;
using FTOptix.CoreBase;
using FTOptix.Core;
#endregion

public class NewOEEWidgetGenerator : BaseNetLogic
{
    // 1. Simplified Theme - Clean & Modern
    private static class Theme
    {
        public static readonly Color White = new Color(0xFFFFFFFF);
        public static readonly Color Slate50 = new Color(0xFFF8FAFC);
        public static readonly Color Slate100 = new Color(0xFFF1F5F9); // Lightest Slate
        public static readonly Color Slate200 = new Color(0xFFE2E8F0); // Borders
        public static readonly Color Slate500 = new Color(0xFF64748B); // Secondary Text
        public static readonly Color Slate900 = new Color(0xFF0F172A); // Primary Text
        
        public static readonly Color Blue = new Color(0xFF3B82F6);     
        public static readonly Color BlueLight = new Color(0xFFDBEAFE);
        
        public static readonly Color Emerald = new Color(0xFF10B981);  
        public static readonly Color EmeraldLight = new Color(0xFFD1FAE5);

        public static readonly Color Amber = new Color(0xFFF59E0B);    
        public static readonly Color AmberLight = new Color(0xFFFEF3C7);

        public static readonly Color Red = new Color(0xFFEF4444);      
        public static readonly Color RedLight = new Color(0xFFFEE2E2);
    }

    [ExportMethod]
    public void GenerateGaugeCards()
    {
        string folder = "UI/Widgets/OEE/Cards/KPIs";
        // OEE: Shift Time + Total Produced
        GenerateSingleGaugeCard(folder, "OEE_Card", "OEE", "Outputs/Core/OEE", "Inputs/Targets/OEETarget", Theme.Blue, Theme.BlueLight, "gauge.svg", new[] {
            ("Shift Time", "Outputs/Shift/TimeIntoShift", ""),
            ("Produced", "Outputs/Core/TotalCount", "")
        });
        
        // Availability: Run Time + Down Time
        GenerateSingleGaugeCard(folder, "Availability_Card", "AVAILABILITY", "Outputs/Core/Availability", "Inputs/Targets/AvailabilityTarget", Theme.Emerald, Theme.EmeraldLight, "clock.svg", new[] {
            ("Run Time", "Outputs/System/TotalRuntimeFormatted", ""),
            ("Down Time", "Outputs/System/DowntimeFormatted", "")
        });
        
        // Performance: Speed + Cycle Time
        GenerateSingleGaugeCard(folder, "Performance_Card", "PERFORMANCE", "Outputs/Core/Performance", "Inputs/Targets/PerformanceTarget", Theme.Amber, Theme.AmberLight, "speed.svg", new[] {
            ("Speed", "Outputs/Core/PartsPerHour", "{0:F0} pph"),
            ("Cycle", "Outputs/Core/AvgCycleTime", "{0:F1} s")
        });
        
        // Quality: Good + Bad Counts
        GenerateSingleGaugeCard(folder, "Quality_Card", "QUALITY", "Outputs/Core/Quality", "Inputs/Targets/QualityTarget", Theme.Blue, Theme.BlueLight, "quality.svg", new[] {
            ("Good", "Inputs/Data/GoodPartCount", ""),
            ("Rejects", "Inputs/Data/BadPartCount", "")
        });
        
        Log.Info("Generator", "Individual Gauge Cards generated successfully.");
    }

    [ExportMethod]
    public void GenerateProductionCards()
    {
        string folder = "UI/Widgets/OEE/Cards/Production";
        // 1. Production Settings Card (Editable Inputs)
        GenerateSettingsCard(folder, "ProductionSettings_Card", "PRODUCTION SETTINGS", 250, 320, "input.svg", new[] {
            ("Target Count", "Inputs/Production/ProductionTarget", "{0}", true),
            ("Ideal Cycle (s)", "Inputs/Production/IdealCycleTimeSeconds", "{0:F2}", true),
            ("Planned Hours", "Inputs/Production/PlannedProductionTimeHours", "{0:F1}", true)
        });

        // 2. Production Status Card (Predictive Outputs)
        GenerateStatusCard(folder, "ProductionStatus_Card", "PRODUCTION STATUS", 250, 320, "dashboard.svg", new[] {
            ("Projected", "Outputs/Production/ProjectedTotalCount", "{0}", false),
            ("Variance", "Outputs/Production/ProductionBehindSchedule", "{0}", false),
            ("Req. Rate", "Outputs/Production/RequiredRateToTarget", "{0:F1} pph", false),
            ("Progress", "Outputs/Production/ProductionProgress", "{0:F1}%", false)
        });

        Log.Info("Generator", "Production Cards generated successfully.");
    }

    [ExportMethod]
    public void GenerateShiftCards()
    {
        string folder = "UI/Widgets/OEE/Cards/Shift";
        // 1. Shift Info Card (Time & Status)
        GenerateShiftInfoCard(folder, "ShiftInfo_Card", "SHIFT INFORMATION", 250, 320, "clock.svg", new[] {
            ("Current Shift", "Outputs/Shift/CurrentShiftNumber", "Shift {0}"),
            ("Start Time", "Outputs/Shift/ShiftStartTime", "{0}"),
            ("End Time", "Outputs/Shift/ShiftEndTime", "{0}"),
            ("Elapsed", "Outputs/Shift/TimeIntoShift", "{0}"),
            ("Remaining", "Outputs/Shift/TimeRemainingInShift", "{0}")
        });

        // 2. Statistics Card (Min/Max/Avg)
        GenerateStatsCard(folder, "ShiftStats_Card", "SHIFT STATISTICS", 250, 320, "trend.svg");

        // 3. Shift Configuration (Editable)
        GenerateSettingsCard(folder, "ShiftConfig_Card", "SHIFT CONFIGURATION", 250, 320, "settings.svg", new[] {
            ("Shifts", "Inputs/Production/NumberOfShifts", "{0}", true),
            ("Shift Start", "Inputs/Production/ShiftStartTime", "{0}", true)
        });

        Log.Info("Generator", "Shift Cards generated successfully.");
    }

    [ExportMethod]
    public void GenerateConfigurationCards()
    {
        string folder = "UI/Widgets/OEE/Cards/Configuration";
        // 1. Targets Configuration
        GenerateSettingsCard(folder, "TargetsConfig_Card", "TARGETS CONFIGURATION", 250, 320, "target.svg", new[] {
            ("OEE Target", "Inputs/Targets/OEETarget", "{0}", true),
            ("Availability", "Inputs/Targets/AvailabilityTarget", "{0}", true),
            ("Performance", "Inputs/Targets/PerformanceTarget", "{0}", true),
            ("Quality", "Inputs/Targets/QualityTarget", "{0}", true)
        });

        // 2. OEE Parameters
        GenerateSettingsCard(folder, "OEEParams_Card", "OEE PARAMETERS", 250, 320, "settings.svg", new[] {
            ("Good Threshold", "Configuration/GoodOEE_Threshold", "{0}", true),
            ("Poor Threshold", "Configuration/PoorOEE_Threshold", "{0}", true),
            ("Min Runtime (s)", "Configuration/MinimumRunTime", "{0}", true),
            ("Update Rate (ms)", "Inputs/System/UpdateRateMs", "{0}", true),
            ("Log Verbosity", "Inputs/System/LoggingVerbosity", "{0}", true)
        });

        // 3. System Controls (Booleans)
        GenerateSwitchCard(folder, "SystemControls_Card", "SYSTEM CONTROLS", 250, 320, "check.svg", new[] {
            ("Real-Time Calc", "Configuration/EnableRealTimeCalc"),
            ("Enable Logging", "Configuration/EnableLogging"),
            ("Enable Alarms", "Configuration/EnableAlarms"),
            ("System Healthy", "Configuration/SystemHealthy")
        });

        Log.Info("Generator", "Configuration Cards generated successfully.");
    }

    [ExportMethod]
    public void GenerateDiagnosticsCards()
    {
        string folder = "UI/Widgets/OEE/Cards/Diagnostics";
        // 1. System Health
        GenerateStatusCard(folder, "SystemHealth_Card", "SYSTEM HEALTH", 250, 320, "heartbeat.svg", new[] {
            ("Status", "Outputs/System/SystemStatus", "{0}", false),
            ("Calc Valid", "Outputs/System/CalculationValid", "{0}", false),
            ("Data Quality", "Outputs/System/DataQualityScore", "{0}%", false),
            ("Last Update", "Outputs/System/LastUpdateTime", "{0:HH:mm:ss}", false)
        });
        
        Log.Info("Generator", "Diagnostics Cards generated successfully.");
    }

    private void GenerateShiftInfoCard(string folderPath, string cardName, string title, double width, double height, string iconName, (string Label, string Path, string Format)[] items)
    {
        var folder = GetOrCreateFolder(folderPath);
        var existing = folder.Get(cardName);
        if (existing != null) existing.Delete();

        var card = CreateBaseCard(cardName, width, height, Theme.Blue);
        var alias = card.GetVariable("OEEInstance");

        // Main Layout
        var layout = InformationModel.Make<ColumnLayout>("MainLayout");
        layout.HorizontalAlignment = HorizontalAlignment.Stretch;
        layout.VerticalAlignment = VerticalAlignment.Stretch;
        layout.LeftMargin = 15; layout.RightMargin = 15; 
        layout.TopMargin = 20; layout.BottomMargin = 15;
        layout.VerticalGap = 5; // Consistent spacing
        card.Add(layout);

        // Header
        AddHeader(layout, title, iconName, Theme.Blue, Theme.BlueLight);

        // Separator
        AddSeparator(layout);

        // Items
        foreach (var (label, path, format) in items)
        {
            var row = InformationModel.Make<RowLayout>(label.Replace(" ", "") + "Row");
            row.HorizontalAlignment = HorizontalAlignment.Stretch;
            row.Height = 30;
            layout.Add(row);

            var l = InformationModel.Make<Label>("Label");
            l.Text = label; l.FontSize = 12; l.TextColor = Theme.Slate500; l.VerticalAlignment = VerticalAlignment.Center;
            l.Width = 80;
            row.Add(l);

            var val = InformationModel.Make<Label>("Val");
            val.FontSize = 16; val.FontWeight = FontWeight.Bold; val.TextColor = Theme.Slate900; 
            val.HorizontalAlignment = HorizontalAlignment.Right; val.VerticalAlignment = VerticalAlignment.Center;
            row.Add(val);
            Link(val.GetVariable("Text"), alias, path, format);
        }

        folder.Add(card);
    }

    private void GenerateStatsCard(string folderPath, string cardName, string title, double width, double height, string iconName)
    {
        var folder = GetOrCreateFolder(folderPath);
        var existing = folder.Get(cardName);
        if (existing != null) existing.Delete();

        var card = CreateBaseCard(cardName, width, height, Theme.Blue);
        var alias = card.GetVariable("OEEInstance");

        // Main Layout
        var layout = InformationModel.Make<ColumnLayout>("MainLayout");
        layout.HorizontalAlignment = HorizontalAlignment.Stretch;
        layout.VerticalAlignment = VerticalAlignment.Stretch;
        layout.LeftMargin = 15; layout.RightMargin = 15; 
        layout.TopMargin = 20; layout.BottomMargin = 15;
        layout.VerticalGap = 5;
        card.Add(layout);

        // Header
        AddHeader(layout, title, iconName, Theme.Blue, Theme.BlueLight);

        // Separator
        AddSeparator(layout);

        // Grid Header
        var gridHeader = InformationModel.Make<RowLayout>("GridHeader");
        gridHeader.HorizontalAlignment = HorizontalAlignment.Stretch; gridHeader.Height = 20;
        layout.Add(gridHeader);

        // Spacer
        var spacer = InformationModel.Make<Label>("Spacer"); 
        spacer.Width = 60; 
        gridHeader.Add(spacer);

        var hMin = InformationModel.Make<Label>("HMin"); hMin.Text = "MIN"; hMin.FontSize = 10; hMin.TextColor = Theme.Slate500; hMin.Width = 40; hMin.HorizontalAlignment = HorizontalAlignment.Center; gridHeader.Add(hMin);
        var hAvg = InformationModel.Make<Label>("HAvg"); hAvg.Text = "AVG"; hAvg.FontSize = 10; hAvg.TextColor = Theme.Slate500; hAvg.Width = 40; hAvg.HorizontalAlignment = HorizontalAlignment.Center; gridHeader.Add(hAvg);
        var hMax = InformationModel.Make<Label>("HMax"); hMax.Text = "MAX"; hMax.FontSize = 10; hMax.TextColor = Theme.Slate500; hMax.Width = 40; hMax.HorizontalAlignment = HorizontalAlignment.Center; gridHeader.Add(hMax);

        // Rows
        AddStatRow(layout, alias, "OEE", "Outputs/Statistics/MinOEE", "Outputs/Statistics/AvgOEE", "Outputs/Statistics/MaxOEE", Theme.Blue);
        AddStatRow(layout, alias, "Avail", "Outputs/Statistics/MinAvailability", "Outputs/Statistics/AvgAvailability", "Outputs/Statistics/MaxAvailability", Theme.Emerald);
        AddStatRow(layout, alias, "Perf", "Outputs/Statistics/MinPerformance", "Outputs/Statistics/AvgPerformance", "Outputs/Statistics/MaxPerformance", Theme.Amber);
        AddStatRow(layout, alias, "Qual", "Outputs/Statistics/MinQuality", "Outputs/Statistics/AvgQuality", "Outputs/Statistics/MaxQuality", Theme.Blue);

        folder.Add(card);
    }

    private void AddStatRow(ColumnLayout parent, IUAVariable alias, string label, string minPath, string avgPath, string maxPath, Color color)
    {
        var row = InformationModel.Make<RowLayout>(label + "Row");
        row.HorizontalAlignment = HorizontalAlignment.Stretch;
        row.Height = 30;
        parent.Add(row);

        var l = InformationModel.Make<Label>("Label");
        l.Text = label; l.FontSize = 12; l.FontWeight = FontWeight.Bold; l.TextColor = color; 
        l.Width = 60; l.VerticalAlignment = VerticalAlignment.Center;
        row.Add(l);

        var vMin = InformationModel.Make<Label>("Min"); vMin.FontSize = 12; vMin.TextColor = Theme.Slate900; vMin.Width = 40; vMin.HorizontalAlignment = HorizontalAlignment.Center; vMin.VerticalAlignment = VerticalAlignment.Center;
        row.Add(vMin); Link(vMin.GetVariable("Text"), alias, minPath, "{0:F0}");

        var vAvg = InformationModel.Make<Label>("Avg"); vAvg.FontSize = 12; vAvg.FontWeight = FontWeight.Bold; vAvg.TextColor = Theme.Slate900; vAvg.Width = 40; vAvg.HorizontalAlignment = HorizontalAlignment.Center; vAvg.VerticalAlignment = VerticalAlignment.Center;
        row.Add(vAvg); Link(vAvg.GetVariable("Text"), alias, avgPath, "{0:F0}");

        var vMax = InformationModel.Make<Label>("Max"); vMax.FontSize = 12; vMax.TextColor = Theme.Slate900; vMax.Width = 40; vMax.HorizontalAlignment = HorizontalAlignment.Center; vMax.VerticalAlignment = VerticalAlignment.Center;
        row.Add(vMax); Link(vMax.GetVariable("Text"), alias, maxPath, "{0:F0}");
    }

    private void GenerateSettingsCard(string folderPath, string cardName, string title, double width, double height, string iconName, (string Label, string Path, string Format, bool Editable)[] items)
    {
        var folder = GetOrCreateFolder(folderPath);
        var existing = folder.Get(cardName);
        if (existing != null) existing.Delete();

        var card = CreateBaseCard(cardName, width, height, Theme.Slate500);
        var alias = card.GetVariable("OEEInstance");

        // Main Layout
        var layout = InformationModel.Make<ColumnLayout>("MainLayout");
        layout.HorizontalAlignment = HorizontalAlignment.Stretch;
        layout.VerticalAlignment = VerticalAlignment.Stretch;
        layout.LeftMargin = 15; layout.RightMargin = 15; 
        layout.TopMargin = 20; layout.BottomMargin = 15;
        layout.VerticalGap = 10;
        card.Add(layout);

        // Header
        AddHeader(layout, title, iconName, Theme.Slate500, Theme.Slate100);
        
        // Separator
        AddSeparator(layout);

        // Items
        foreach (var (label, path, format, editable) in items)
        {
            var row = InformationModel.Make<ColumnLayout>(label.Replace(" ", "") + "Row");
            row.HorizontalAlignment = HorizontalAlignment.Stretch;
            layout.Add(row);

            var l = InformationModel.Make<Label>("Label");
            l.Text = label; l.FontSize = 11; l.TextColor = Theme.Slate500; l.BottomMargin = 2;
            row.Add(l);

            if (editable)
            {
                var box = InformationModel.Make<TextBox>("Box");
                box.Height = 30;
                box.HorizontalAlignment = HorizontalAlignment.Stretch;
                row.Add(box);
                Link(box.GetVariable("Text"), alias, path, format);
            }
            else
            {
                var val = InformationModel.Make<Label>("Val");
                val.FontSize = 16; val.FontWeight = FontWeight.Bold; val.TextColor = Theme.Slate900;
                row.Add(val);
                Link(val.GetVariable("Text"), alias, path, format);
            }
        }
        
        folder.Add(card);
    }

    private void GenerateSwitchCard(string folderPath, string cardName, string title, double width, double height, string iconName, (string Label, string Path)[] items)
    {
        var folder = GetOrCreateFolder(folderPath);
        var existing = folder.Get(cardName);
        if (existing != null) existing.Delete();

        var card = CreateBaseCard(cardName, width, height, Theme.Slate500);
        var alias = card.GetVariable("OEEInstance");

        // Main Layout
        var layout = InformationModel.Make<ColumnLayout>("MainLayout");
        layout.HorizontalAlignment = HorizontalAlignment.Stretch;
        layout.VerticalAlignment = VerticalAlignment.Stretch;
        layout.LeftMargin = 15; layout.RightMargin = 15; 
        layout.TopMargin = 20; layout.BottomMargin = 15;
        layout.VerticalGap = 5;
        card.Add(layout);

        // Header
        AddHeader(layout, title, iconName, Theme.Slate500, Theme.Slate100);
        
        // Separator
        AddSeparator(layout);

        // Items
        foreach (var (label, path) in items)
        {
            var row = InformationModel.Make<RowLayout>(label.Replace(" ", "") + "Row");
            row.HorizontalAlignment = HorizontalAlignment.Stretch;
            row.Height = 30;
            layout.Add(row);

            var l = InformationModel.Make<Label>("Label");
            l.Text = label; l.FontSize = 12; l.TextColor = Theme.Slate500; l.VerticalAlignment = VerticalAlignment.Center;
            l.Width = 120;
            row.Add(l);

            var sw = InformationModel.Make<Switch>("Switch");
            sw.HorizontalAlignment = HorizontalAlignment.Right;
            sw.VerticalAlignment = VerticalAlignment.Center;
            row.Add(sw);
            
            // Link Checked property
            Link(sw.GetVariable("Checked"), alias, path);
        }
        
        folder.Add(card);
    }

    private void GenerateStatusCard(string folderPath, string cardName, string title, double width, double height, string iconName, (string Label, string Path, string Format, bool IsBar)[] items)
    {
        var folder = GetOrCreateFolder(folderPath);
        var existing = folder.Get(cardName);
        if (existing != null) existing.Delete();

        var card = CreateBaseCard(cardName, width, height, Theme.Blue);
        var alias = card.GetVariable("OEEInstance");

        // Main Layout
        var layout = InformationModel.Make<ColumnLayout>("MainLayout");
        layout.HorizontalAlignment = HorizontalAlignment.Stretch;
        layout.VerticalAlignment = VerticalAlignment.Stretch;
        layout.LeftMargin = 15; layout.RightMargin = 15; 
        layout.TopMargin = 20; layout.BottomMargin = 15;
        layout.VerticalGap = 5;
        card.Add(layout);

        // Header
        AddHeader(layout, title, iconName, Theme.Blue, Theme.BlueLight);
        
        // Separator
        AddSeparator(layout);

        // Items
        foreach (var (label, path, format, isBar) in items)
        {
            var row = InformationModel.Make<RowLayout>(label.Replace(" ", "") + "Row");
            row.HorizontalAlignment = HorizontalAlignment.Stretch;
            row.Height = 30;
            layout.Add(row);

            var l = InformationModel.Make<Label>("Label");
            l.Text = label; l.FontSize = 12; l.TextColor = Theme.Slate500; l.VerticalAlignment = VerticalAlignment.Center;
            l.Width = 80;
            row.Add(l);

            var val = InformationModel.Make<Label>("Val");
            val.FontSize = 16; val.FontWeight = FontWeight.Bold; val.TextColor = Theme.Slate900; 
            val.HorizontalAlignment = HorizontalAlignment.Right; val.VerticalAlignment = VerticalAlignment.Center;
            row.Add(val);
            Link(val.GetVariable("Text"), alias, path, format);
        }
        
        // Add a visual progress bar at the bottom
        var barLabel = InformationModel.Make<Label>("BarLabel");
        barLabel.Text = "Shift Progress"; barLabel.FontSize = 11; barLabel.TextColor = Theme.Slate500; barLabel.TopMargin = 10;
        layout.Add(barLabel);

        var gauge = InformationModel.Make<LinearGauge>("ProgressGauge");
        gauge.Height = 10; gauge.HorizontalAlignment = HorizontalAlignment.Stretch;
        gauge.MinValue = 0; gauge.MaxValue = 100;
        
        // Set properties safely
        var th = gauge.GetVariable("Thickness"); if(th!=null) th.Value = 10.0f;
        var col = gauge.GetVariable("Color"); if(col!=null) col.Value = Theme.Blue;
        var mt = gauge.GetVariable("MajorTickCount"); if(mt!=null) mt.Value = 0;
        var mnt = gauge.GetVariable("MinorTickCount"); if(mnt!=null) mnt.Value = 0;
        var showT = gauge.GetVariable("ShowText"); if(showT!=null) showT.Value = false;
        var ed = gauge.GetVariable("Editable"); if(ed!=null) ed.Value = false;

        layout.Add(gauge);
        Link(gauge.GetVariable("Value"), alias, "Outputs/Production/ShiftProgress");

        folder.Add(card);
    }

    private void GenerateSingleGaugeCard(string folderPath, string cardName, string title, string path, string targetPath, Color color, Color lightColor, string iconName, (string Label, string Path, string Format)[] details)
    {
        var folder = GetOrCreateFolder(folderPath);
        var existing = folder.Get(cardName);
        if (existing != null) existing.Delete();

        // Create Card (250x320) - Taller for details
        var card = CreateBaseCard(cardName, 250, 320, color);
        var alias = card.GetVariable("OEEInstance");

        AddGaugeBlock(card, alias, title, path, targetPath, color, lightColor, iconName, details);

        folder.Add(card);
    }

    private void AddGaugeBlock(Item parent, IUAVariable alias, string title, string path, string targetPath, Color color, Color lightColor, string iconName, (string Label, string Path, string Format)[] details)
    {
        var container = InformationModel.Make<ColumnLayout>(title + "Block");
        container.HorizontalAlignment = HorizontalAlignment.Stretch;
        container.VerticalAlignment = VerticalAlignment.Stretch;
        container.LeftMargin = 15; container.RightMargin = 15; 
        container.TopMargin = 20; container.BottomMargin = 15;
        parent.Add(container);

        // Header
        AddHeader(container, title, iconName, color, lightColor);

        // Gauge Container
        var gaugePanel = InformationModel.Make<Panel>("GaugePanel");
        gaugePanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        gaugePanel.VerticalAlignment = VerticalAlignment.Stretch;
        container.Add(gaugePanel);

        // Gauge
        var gauge = InformationModel.Make<CircularGauge>("Gauge");
        gauge.HorizontalAlignment = HorizontalAlignment.Center;
        gauge.VerticalAlignment = VerticalAlignment.Center;
        gauge.Width = 200; 
        gauge.Height = 200;
        gauge.MinValue = 0; gauge.MaxValue = 100;
        
        // Style Properties
        var thickness = gauge.GetVariable("Thickness");
        if (thickness != null) thickness.Value = 12.0f;
        
        var colVar = gauge.GetVariable("Color");
        if (colVar != null) colVar.Value = color;
        
        var editable = gauge.GetVariable("Editable");
        if (editable != null) editable.Value = false;

        var majorTicks = gauge.GetVariable("MajorTickCount");
        if (majorTicks != null) majorTicks.Value = 0;

        var minorTicks = gauge.GetVariable("MinorTickCount");
        if (minorTicks != null) minorTicks.Value = 0;

        var labelOffset = gauge.GetVariable("LabelOffset");
        if (labelOffset != null) labelOffset.Value = 50.0f;

        var showText = gauge.GetVariable("ShowText");
        if (showText != null) showText.Value = false;
        
        gaugePanel.Add(gauge);
        Link(gauge.GetVariable("Value"), alias, path);

        // Text Container
        var textCol = InformationModel.Make<ColumnLayout>("TextCol");
        textCol.HorizontalAlignment = HorizontalAlignment.Center;
        textCol.VerticalAlignment = VerticalAlignment.Center;
        textCol.Height = 50;
        textCol.Width = 100;
        gaugePanel.Add(textCol);

        // Value Text
        var val = InformationModel.Make<Label>("Val");
        val.HorizontalAlignment = HorizontalAlignment.Center;
        val.FontSize = 22;
        val.FontWeight = FontWeight.Bold;
        val.TextColor = color;
        textCol.Add(val);
        Link(val.GetVariable("Text"), alias, path, "{0:F1}%");

        // Separator
        AddSeparator(container);

        // Details Section (Includes Target + Extra Details)
        var detailsContainer = InformationModel.Make<ColumnLayout>("Details");
        detailsContainer.HorizontalAlignment = HorizontalAlignment.Stretch;
        detailsContainer.TopMargin = 10;
        detailsContainer.VerticalGap = 5;
        container.Add(detailsContainer);

        // 1. Target Row (Always present)
        var targetRow = InformationModel.Make<RowLayout>("TargetRow");
        targetRow.HorizontalAlignment = HorizontalAlignment.Stretch;
        targetRow.Height = 20;
        detailsContainer.Add(targetRow);

        var tLbl = InformationModel.Make<Label>("TLbl");
        tLbl.Text = "Target";
        tLbl.FontSize = 12;
        tLbl.TextColor = Theme.Slate500;
        tLbl.HorizontalAlignment = HorizontalAlignment.Left;
        targetRow.Add(tLbl);

        var tVal = InformationModel.Make<Label>("TVal");
        tVal.FontSize = 12;
        tVal.FontWeight = FontWeight.Bold;
        tVal.TextColor = Theme.Slate900;
        tVal.HorizontalAlignment = HorizontalAlignment.Right;
        targetRow.Add(tVal);
        Link(tVal.GetVariable("Text"), alias, targetPath, "");

        // 2. Extra Details
        if (details != null && details.Length > 0)
        {
            foreach (var (dLabel, dPath, dFormat) in details)
            {
                var row = InformationModel.Make<RowLayout>(dLabel.Replace(" ", "") + "Row");
                row.HorizontalAlignment = HorizontalAlignment.Stretch;
                row.Height = 20;
                detailsContainer.Add(row);

                var dLbl = InformationModel.Make<Label>("Lbl");
                dLbl.Text = dLabel;
                dLbl.FontSize = 12;
                dLbl.TextColor = Theme.Slate500;
                dLbl.HorizontalAlignment = HorizontalAlignment.Left;
                row.Add(dLbl);

                var dVal = InformationModel.Make<Label>("Val");
                dVal.FontSize = 12;
                dVal.FontWeight = FontWeight.Bold;
                dVal.TextColor = Theme.Slate900;
                dVal.HorizontalAlignment = HorizontalAlignment.Right;
                row.Add(dVal);
                
                Link(dVal.GetVariable("Text"), alias, dPath, dFormat);
            }
        }
    }
    
    private void SetProperty(Item item, string propertyName, object value)
    {
        var prop = item.GetVariable(propertyName);
        if (prop != null)
        {
            prop.Value = new UAValue(value);
        }
    }

    // --- Helper: Create the Base Card Structure ---
    private ScaleLayout CreateBaseCard(string cardName, double width, double height, Color? accentColor = null)
    {
        // 1. Create the main container (ScaleLayout for responsiveness)
        var card = InformationModel.Make<ScaleLayout>(cardName);
        card.Width = (float)width;
        card.Height = (float)height;
        card.OriginalWidth = (float)width;
        card.OriginalHeight = (float)height;

        // 2. Background & Border
        var bg = InformationModel.Make<Rectangle>("Background");
        bg.HorizontalAlignment = HorizontalAlignment.Stretch;
        bg.VerticalAlignment = VerticalAlignment.Stretch;
        bg.FillColor = Theme.White;
        bg.CornerRadius = 8;
        bg.BorderThickness = 1;
        bg.BorderColor = Theme.Slate200;
        card.Add(bg);

        // 3. Accent Line
        var accent = InformationModel.Make<Rectangle>("Accent");
        accent.Height = 4;
        accent.HorizontalAlignment = HorizontalAlignment.Stretch;
        accent.VerticalAlignment = VerticalAlignment.Top;
        accent.FillColor = accentColor ?? Theme.Blue;
        accent.CornerRadius = 8;
        card.Add(accent);

        // 4. Add the OEE Alias (The magic link)
        var oeeAlias = InformationModel.MakeVariable<NodePointer>("OEEInstance", OpcUa.DataTypes.NodeId);
        // Try to find the type to restrict selection, otherwise generic
        var oeeType = Project.Current.Get("Model/Types/OEEType");
        if (oeeType != null) oeeAlias.Kind = oeeType.NodeId;
        card.Add(oeeAlias);

        return card;
    }

    private void AddHeader(ColumnLayout parent, string title, string iconName, Color iconColor, Color bgColor)
    {
        var header = InformationModel.Make<RowLayout>("Header");
        header.Height = 32; 
        header.HorizontalAlignment = HorizontalAlignment.Left; 
        header.BottomMargin = 15;
        parent.Add(header);

        // Icon Container
        var iconBox = InformationModel.Make<Rectangle>("IconBox");
        iconBox.Width = 32; iconBox.Height = 32;
        iconBox.CornerRadius = 6; 
        iconBox.FillColor = bgColor;
        iconBox.RightMargin = 10;
        header.Add(iconBox);

        var icon = InformationModel.Make<Image>("Icon");
        icon.Path = ResourceUri.FromProjectRelativePath("ProjectFiles/Graphics/Icons/" + iconName);
        icon.Width = 20; icon.Height = 20; 
        icon.HorizontalAlignment = HorizontalAlignment.Center;
        icon.VerticalAlignment = VerticalAlignment.Center;
        // icon.Color = iconColor; // Not supported on Image
        iconBox.Add(icon);

        var lbl = InformationModel.Make<Label>("Title");
        lbl.Text = title.ToUpper();
        lbl.FontSize = 12; lbl.FontWeight = FontWeight.Bold; lbl.TextColor = Theme.Slate500;
        lbl.VerticalAlignment = VerticalAlignment.Center;
        header.Add(lbl);
    }

    private void AddSeparator(ColumnLayout parent)
    {
        var sep = InformationModel.Make<Rectangle>("Separator");
        sep.Height = 1;
        sep.HorizontalAlignment = HorizontalAlignment.Stretch;
        sep.FillColor = Theme.Slate200;
        sep.BottomMargin = 10;
        parent.Add(sep);
    }

    // --- Helper: Dynamic Linking ---
    private void Link(IUAVariable target, IUAVariable alias, string subPath, string format = "")
    {
        if (target == null || alias == null) return;

        string prefix = "";
        var current = target.Owner;
        var aliasParent = alias.Owner;
        
        // Safety check
        if (current == null || aliasParent == null) return;

        int steps = 0;
        while (current != null && current != aliasParent && steps < 10)
        {
            prefix += "../";
            current = current.Owner;
            steps++;
        }
        
        // If we found the parent
        if (current == aliasParent)
        {
            if (!string.IsNullOrEmpty(format))
            {
                var formatter = InformationModel.Make<StringFormatter>("Formatter");
                formatter.Format = format;
                
                var source = InformationModel.MakeVariable("Source", OpcUa.DataTypes.BaseDataType);
                var link = InformationModel.MakeVariable<DynamicLink>("Link", FTOptix.Core.DataTypes.NodePath);
                
                // Formatter nesting: Text -> Formatter -> Source -> Link
                // We need 4 extra levels to get out of the property nesting
                link.Value = "../../../../" + prefix + alias.BrowseName + "/" + subPath;
                
                source.Add(link);
                formatter.Add(source);
                
                // Remove existing
                var existing = target.Get("Formatter");
                if (existing != null) existing.Delete();
                
                target.Add(formatter);
            }
            else
            {
                var link = InformationModel.MakeVariable<DynamicLink>("DynamicLink", FTOptix.Core.DataTypes.NodePath);
                
                // Direct nesting: Text -> Link
                // We need 1 extra level to get out of the property nesting
                link.Value = "../" + prefix + alias.BrowseName + "/" + subPath;
                
                // Remove existing
                var existing = target.Get("DynamicLink");
                if (existing != null) existing.Delete();
                
                target.Add(link);
            }
        }
    }
    
    // --- Helper: Folder Management ---
    private Folder GetOrCreateFolder(string path)
    {
        var parts = path.Split('/');
        // Start at UI root, but handle if "UI" is the first part
        var current = Project.Current.Get("UI"); 
        
        foreach (var part in parts)
        {
            if (part == "UI") continue;
            
            // Safety check for current
            if (current == null)
            {
                Log.Error("Generator", "Could not find UI folder root.");
                return null;
            }

            var next = current.Get(part);
            if (next == null)
            {
                next = InformationModel.Make<Folder>(part);
                current.Add(next);
            }
            current = (Folder)next;
        }
        return (Folder)current;
    }
}
