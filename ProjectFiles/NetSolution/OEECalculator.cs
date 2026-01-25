using System;
using System.Linq;
using System.Collections.Generic;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.Core;
using UAManagedCore;

/// <summary>
/// Dynamic OEE Calculator for FactoryTalk Optix
/// 
/// DESCRIPTION:
/// Automatically discovers and calculates OEE metrics for all ProductionLine object instances
/// in a specified folder. Runs calculations every 3 seconds using a periodic task.
/// 
/// SETUP:
/// 1. Add a NodeId variable named "ProductionLinesFolder" to this NetLogic
/// 2. Set it to point to your ProductionLines folder (drag and drop the folder)
/// 3. If left empty, it will use the Owner folder
/// 
/// REQUIREMENTS:
/// Each ProductionLine object must have this structure:
/// - Inputs/
///   - PLC_Data/ (runtime, partsProduced, defectiveUnits, idealCycleTime)
///   - Configuration/ (numberOfShifts, shift1StartTime, breakTime)
///   - Targets/ (oeeTarget, productionTarget, availabilityTarget, performanceTarget, qualityTarget)
/// - Outputs/
///   - OEE_Metrics/ (availability, performance, quality, oeeValue, downtime)
///   - Shift_Info/ (scheduledTime, currentShift, shiftStartTime, shiftElapsedTime, shiftRemainingTime)
///   - Production_Metrics/ (goodUnits, defectRate, currentRate)
///   - Gaps/ (oeeGap, productionGap, availabilityGap, performanceGap, qualityGap)
///   - Status/ (isRunning, isOeeOnTarget, isProductionOnTarget, etc.)
/// </summary>
public class OEECalculator : BaseNetLogic
{
    // Configuration
    private const int UPDATE_INTERVAL_MS = 3000; // 3 seconds
    
    // Runtime variables
    private IUANode productionLinesFolder;
    private PeriodicTask periodicTask;
    private Dictionary<NodeId, double> previousRuntime = new Dictionary<NodeId, double>();
    
    // Cache last good values for each production line
    private class CachedMetrics
    {
        public double availability;
        public double performance;
        public double quality;
        public double oee;
        public double downtime;
        public int goodUnits;
        public double defectRate;
        public double currentRate;
    }
    private Dictionary<NodeId, CachedMetrics> cachedValues = new Dictionary<NodeId, CachedMetrics>();

    #region Lifecycle Methods

    public override void Start()
    {
        try
        {
            // Determine which folder to monitor
            productionLinesFolder = GetProductionLinesFolder();
            
            if (productionLinesFolder == null)
            {
                Log.Error("OEECalculator", "Could not determine ProductionLines folder. Exiting.");
                return;
            }

            Log.Info("OEECalculator", $"Monitoring folder: {productionLinesFolder.BrowseName}");

            // Discover production lines
            var lines = DiscoverProductionLines();
            Log.Info("OEECalculator", $"Found {lines.Count} ProductionLine instance(s)");

            if (lines.Count == 0)
            {
                Log.Warning("OEECalculator", "No ProductionLine objects found. Ensure objects have Inputs and Outputs folders.");
            }
            else
            {
                // Log the structure of first production line for debugging
                var firstLine = lines[0];
                Log.Info("OEECalculator", $"=== STRUCTURE OF {firstLine.BrowseName} ===");
                foreach (var child in firstLine.Children)
                {
                    Log.Info("OEECalculator", $"  Folder: {child.BrowseName}");
                    foreach (var grandchild in child.Children)
                    {
                        Log.Info("OEECalculator", $"    - {grandchild.BrowseName}");
                    }
                }
                Log.Info("OEECalculator", $"=== END STRUCTURE ===");
            }

            // Run initial calculation
            ProcessAllProductionLines();

            // Start periodic updates
            periodicTask = new PeriodicTask(ProcessAllProductionLines, UPDATE_INTERVAL_MS, LogicObject);
            periodicTask.Start();

            Log.Info("OEECalculator", "Started successfully");
        }
        catch (Exception ex)
        {
            Log.Error("OEECalculator", $"Startup error: {ex.Message}");
        }
    }

    public override void Stop()
    {
        periodicTask?.Dispose();
        Log.Info("OEECalculator", "Stopped");
    }

    #endregion

    #region Discovery Methods

    /// <summary>
    /// Get the folder to monitor from the NodeId variable or use Owner as fallback
    /// </summary>
    private IUANode GetProductionLinesFolder()
    {
        var folderVariable = LogicObject.GetVariable("ProductionLinesFolder");
        
        if (folderVariable != null && folderVariable.Value != null)
        {
            var nodeId = (NodeId)folderVariable.Value;
            
            if (nodeId != NodeId.Empty)
            {
                var folder = InformationModel.Get(nodeId);
                if (folder != null)
                {
                    return folder;
                }
                Log.Warning("OEECalculator", "ProductionLinesFolder points to invalid node. Using Owner.");
            }
        }
        
        return Owner;
    }

    /// <summary>
    /// Discover all ProductionLine objects in the folder
    /// </summary>
    private List<IUANode> DiscoverProductionLines()
    {
        var lines = new List<IUANode>();
        
        foreach (var child in productionLinesFolder.Children)
        {
            if (IsProductionLine(child))
            {
                lines.Add(child);
            }
        }
        
        return lines;
    }

    /// <summary>
    /// Check if a node is a ProductionLine by verifying required structure
    /// </summary>
    private bool IsProductionLine(IUANode node)
    {
        return node.Get("Inputs") != null && node.Get("Outputs") != null;
    }

    #endregion

    #region Main Processing

    /// <summary>
    /// Process all production lines - called every UPDATE_INTERVAL_MS
    /// </summary>
    private void ProcessAllProductionLines()
    {
        var lines = DiscoverProductionLines();
        
        foreach (var line in lines)
        {
            try
            {
                CalculateOEE(line);
                CalculateShiftInfo(line);
            }
            catch (Exception ex)
            {
                Log.Warning("OEECalculator", $"Error processing {line.BrowseName}: {ex.Message}");
            }
        }
    }

    #endregion

    #region OEE Calculation

    /// <summary>
    /// Calculate all OEE metrics for a production line
    /// </summary>
    private void CalculateOEE(IUANode line)
    {
        // ===== READ INPUTS =====
        var runtime = ReadDouble(line, "Inputs/PLC_Data/runtime");
        var partsProduced = ReadInt(line, "Inputs/PLC_Data/partsProduced");
        var defectiveUnits = ReadInt(line, "Inputs/PLC_Data/defectiveUnits");
        var idealCycleTime = ReadDouble(line, "Inputs/PLC_Data/idealCycleTime");
        
        var numberOfShifts = Math.Max(1, Math.Min(3, ReadInt(line, "Inputs/Configuration/numberOfShifts")));
        var breakTime = ReadDouble(line, "Inputs/Configuration/breakTime");
        
        var oeeTarget = ReadDouble(line, "Inputs/Targets/oeeTarget");
        var productionTarget = ReadInt(line, "Inputs/Targets/productionTarget");
        var availabilityTarget = ReadDouble(line, "Inputs/Targets/availabilityTarget");
        var performanceTarget = ReadDouble(line, "Inputs/Targets/performanceTarget");
        var qualityTarget = ReadDouble(line, "Inputs/Targets/qualityTarget");

        // DEBUG: Log all input values to diagnose issues
        Log.Info("OEECalculator", $"DEBUG [{line.BrowseName}] runtime={runtime}, parts={partsProduced}, defects={defectiveUnits}, idealCycleTime={idealCycleTime}, shifts={numberOfShifts}, breakTime={breakTime}");

        // ===== VALIDATE INPUTS =====
        defectiveUnits = Math.Min(defectiveUnits, partsProduced);

        // ===== CALCULATE SCHEDULED TIME =====
        var shiftDuration = 24 * 3600 / numberOfShifts; // seconds per shift
        var scheduledTime = Math.Max(0, shiftDuration - breakTime);

        // ===== CALCULATE OEE COMPONENTS =====
        
        // Availability = (Runtime / Scheduled Time) * 100
        var availability = scheduledTime > 0 && runtime > 0
            ? Math.Min(100.0, (runtime / scheduledTime) * 100) 
            : 0.0;
        
        // Performance = (Parts * Ideal Cycle Time / Runtime) * 100
        var performance = (runtime > 0 && idealCycleTime > 0 && partsProduced > 0)
            ? Math.Min(100.0, (partsProduced * idealCycleTime / runtime) * 100)
            : 0.0;
        
        // Quality = ((Parts - Defects) / Parts) * 100
        var quality = partsProduced > 0
            ? ((partsProduced - defectiveUnits) / (double)partsProduced) * 100
            : 0.0;
        
        // OEE = Availability * Performance * Quality / 10000
        var oee = (availability * performance * quality) / 10000;

        // Downtime
        var downtime = Math.Max(0, scheduledTime - runtime);

        // ===== PRODUCTION METRICS =====
        var goodUnits = partsProduced - defectiveUnits;
        var defectRate = partsProduced > 0 ? (defectiveUnits / (double)partsProduced) * 100 : 0;
        var currentRate = runtime > 0 && partsProduced > 0 ? (partsProduced / runtime) * 3600 : 0; // parts/hour

        // ===== FORMATTED OUTPUTS =====
        var totalRuntimeFormatted = FormatTimeSpan(runtime);
        var downtimeFormatted = FormatTimeSpan(scheduledTime - runtime);
        var productionProgressPercent = productionTarget > 0 ? Math.Min(100.0, (partsProduced / (double)productionTarget) * 100) : 0.0;

        // DEBUG: Log calculated metrics
        Log.Info("OEECalculator", $"DEBUG [{line.BrowseName}] CALC: availability={availability:F2}%, performance={performance:F2}%, quality={quality:F2}%, oee={oee:F4}");
        Log.Info("OEECalculator", $"DEBUG [{line.BrowseName}] scheduledTime={scheduledTime:F0}s, downtime={downtime:F0}s, goodUnits={goodUnits}, defectRate={defectRate:F2}%, currentRate={currentRate:F2}");

        // ===== VALIDATION - Skip if all values are zero (indicates missing data) =====
        if (runtime <= 0 && partsProduced <= 0 && idealCycleTime <= 0)
        {
            // Use cached values if available
            if (cachedValues.TryGetValue(line.NodeId, out var cached))
            {
                availability = cached.availability;
                performance = cached.performance;
                quality = cached.quality;
                oee = cached.oee;
                downtime = cached.downtime;
                goodUnits = cached.goodUnits;
                defectRate = cached.defectRate;
                currentRate = cached.currentRate;
            }
            return;
        }

        // ===== CALCULATE GAPS =====
        var oeeGap = oee - oeeTarget;
        var productionGap = partsProduced - productionTarget;
        var availabilityGap = availability - availabilityTarget;
        var performanceGap = performance - performanceTarget;
        var qualityGap = quality - qualityTarget;

        // ===== STATUS FLAGS =====
        var isOeeOnTarget = oee >= oeeTarget;
        var isProductionOnTarget = partsProduced >= productionTarget;
        var isAvailabilityOnTarget = availability >= availabilityTarget;
        var isPerformanceOnTarget = performance >= performanceTarget;
        var isQualityOnTarget = quality >= qualityTarget;

        // ===== CACHE GOOD VALUES =====
        cachedValues[line.NodeId] = new CachedMetrics
        {
            availability = availability,
            performance = performance,
            quality = quality,
            oee = oee,
            downtime = downtime,
            goodUnits = goodUnits,
            defectRate = defectRate,
            currentRate = currentRate
        };

        // ===== WRITE OUTPUTS =====
        
        // OEE Metrics
        WriteDouble(line, "Outputs/OEE_Metrics/availability", availability);
        WriteDouble(line, "Outputs/OEE_Metrics/performance", performance);
        WriteDouble(line, "Outputs/OEE_Metrics/quality", quality);
        WriteDouble(line, "Outputs/OEE_Metrics/oeeValue", oee);
        WriteDouble(line, "Outputs/OEE_Metrics/downtime", downtime);
        
        // Shift Info
        WriteDouble(line, "Outputs/Shift_Info/scheduledTime", scheduledTime);
        
        // Production Metrics
        WriteInt(line, "Outputs/Production_Metrics/goodUnits", goodUnits);
        WriteDouble(line, "Outputs/Production_Metrics/defectRate", defectRate);
        WriteDouble(line, "Outputs/Production_Metrics/currentRate", currentRate);
        WriteDouble(line, "Outputs/Production_Metrics/productionProgressPercent", productionProgressPercent);
        WriteString(line, "Outputs/Production_Metrics/totalRuntimeFormatted", totalRuntimeFormatted);
        WriteString(line, "Outputs/Production_Metrics/downtimeFormatted", downtimeFormatted);
        
        // Gaps
        WriteDouble(line, "Outputs/Gaps/oeeGap", oeeGap);
        WriteInt(line, "Outputs/Gaps/productionGap", productionGap);
        WriteDouble(line, "Outputs/Gaps/availabilityGap", availabilityGap);
        WriteDouble(line, "Outputs/Gaps/performanceGap", performanceGap);
        WriteDouble(line, "Outputs/Gaps/qualityGap", qualityGap);
        
        // Status
        bool isRunning = UpdateRunningStatus(line);
        var systemStatus = DetermineSystemStatus(isRunning, runtime, partsProduced);
        WriteBool(line, "Outputs/Status/isOeeOnTarget", isOeeOnTarget);
        WriteBool(line, "Outputs/Status/isProductionOnTarget", isProductionOnTarget);
        WriteBool(line, "Outputs/Status/isAvailabilityOnTarget", isAvailabilityOnTarget);
        WriteBool(line, "Outputs/Status/isPerformanceOnTarget", isPerformanceOnTarget);
        WriteBool(line, "Outputs/Status/isQualityOnTarget", isQualityOnTarget);
        WriteString(line, "Outputs/Status/systemStatus", systemStatus);
    }

    #endregion

    #region Shift Calculation

    /// <summary>
    /// Calculate current shift information
    /// </summary>
    private void CalculateShiftInfo(IUANode line)
    {
        var numberOfShifts = Math.Max(1, Math.Min(3, ReadInt(line, "Inputs/Configuration/numberOfShifts")));
        var shift1StartTime = ReadDateTime(line, "Inputs/Configuration/shift1StartTime");
        
        var shiftDuration = 24 * 3600 / numberOfShifts; // seconds
        var now = DateTime.Now;
        var shift1Time = shift1StartTime.TimeOfDay;
        var currentTime = now.TimeOfDay;
        
        // Calculate seconds since shift 1 started today
        var secondsSinceShift1 = (currentTime - shift1Time).TotalSeconds;
        if (secondsSinceShift1 < 0)
        {
            secondsSinceShift1 += 24 * 3600; // Wrap to previous day
        }
        
        // Determine current shift number
        var currentShift = ((int)(secondsSinceShift1 / shiftDuration) % numberOfShifts) + 1;
        
        // Calculate shift start time
        var shiftStartOffset = (currentShift - 1) * shiftDuration;
        var shiftStartTime = now.Date.Add(shift1Time).AddSeconds(shiftStartOffset);
        if (shiftStartTime > now)
        {
            shiftStartTime = shiftStartTime.AddDays(-1);
        }
        
        // Calculate elapsed and remaining time
        var shiftElapsedTime = (now - shiftStartTime).TotalSeconds;
        var shiftRemainingTime = shiftDuration - shiftElapsedTime;

        // Shift progress and formatted times
        var shiftProgressPercent = Math.Min(100.0, (shiftElapsedTime / shiftDuration) * 100);
        var timeIntoShiftFormatted = FormatTimeSpan(shiftElapsedTime);
        var timeRemainingFormatted = FormatTimeSpan(shiftRemainingTime);

        // Shift change warning window
        var shiftChangeLeadSeconds = ReadDouble(line, "Inputs/Configuration/shiftChangeLeadSeconds");
        if (shiftChangeLeadSeconds <= 0)
        {
            shiftChangeLeadSeconds = 300; // default 5 minutes
        }
        var shiftChangeSoon = shiftRemainingTime >= 0 && shiftRemainingTime <= shiftChangeLeadSeconds;
        
        // Write outputs
        WriteInt(line, "Outputs/Shift_Info/currentShift", currentShift);
        WriteDateTime(line, "Outputs/Shift_Info/shiftStartTime", shiftStartTime);
        WriteDouble(line, "Outputs/Shift_Info/shiftElapsedTime", shiftElapsedTime);
        WriteDouble(line, "Outputs/Shift_Info/shiftRemainingTime", shiftRemainingTime);
        WriteDouble(line, "Outputs/Shift_Info/shiftProgressPercent", shiftProgressPercent);
        WriteString(line, "Outputs/Shift_Info/timeIntoShiftFormatted", timeIntoShiftFormatted);
        WriteString(line, "Outputs/Shift_Info/timeRemainingFormatted", timeRemainingFormatted);
        WriteBool(line, "Outputs/Status/shiftChangeSoon", shiftChangeSoon);
    }

    #endregion

    #region Running Status

    /// <summary>
    /// Determine if machine is running by comparing runtime to previous value
    /// </summary>
    private bool UpdateRunningStatus(IUANode line)
    {
        var currentRuntime = ReadDouble(line, "Inputs/PLC_Data/runtime");
        
        var isRunning = false;
        if (previousRuntime.TryGetValue(line.NodeId, out var lastRuntime))
        {
            isRunning = currentRuntime > lastRuntime;
        }
        
        previousRuntime[line.NodeId] = currentRuntime;
        WriteBool(line, "Outputs/Status/isRunning", isRunning);
        return isRunning;
    }

    #endregion

    #region Helper Methods - Format

    private string FormatTimeSpan(double totalSeconds)
    {
        if (totalSeconds < 0) totalSeconds = 0;
        var ts = TimeSpan.FromSeconds(totalSeconds);
        return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
    }

    private string DetermineSystemStatus(bool isRunning, double runtime, int partsProduced)
    {
        if (runtime <= 0 && partsProduced <= 0)
            return "Idle";
        if (isRunning)
            return "Running";
        return "Paused";
    }

    #endregion

    #region Helper Methods - Read

    private double ReadDouble(IUANode node, string path)
    {
        try
        {
            var variable = node.Get(path) as IUAVariable;
            if (variable?.Value?.Value != null)
            {
                var val = variable.Value.Value;
                double result = System.Convert.ToDouble(val, System.Globalization.CultureInfo.InvariantCulture);
                if (double.IsNaN(result) || double.IsInfinity(result))
                    return 0.0;
                return result;
            }
        }
        catch (Exception ex)
        {
            Log.Warning("OEECalculator", $"ReadDouble error at {path}: {ex.Message}");
        }
        return 0.0;
    }

    private int ReadInt(IUANode node, string path)
    {
        try
        {
            var variable = node.Get(path) as IUAVariable;
            if (variable?.Value?.Value != null)
            {
                var val = variable.Value.Value;
                double result = System.Convert.ToDouble(val, System.Globalization.CultureInfo.InvariantCulture);
                if (double.IsNaN(result) || double.IsInfinity(result))
                    return 0;
                return System.Convert.ToInt32(result);
            }
        }
        catch (Exception ex)
        {
            Log.Warning("OEECalculator", $"ReadInt error at {path}: {ex.Message}");
        }
        return 0;
    }

    private bool ReadBool(IUANode node, string path)
    {
        try
        {
            var variable = node.Get(path) as IUAVariable;
            if (variable?.Value?.Value != null)
            {
                var val = variable.Value.Value;
                if (val is bool b) return b;
                return System.Convert.ToBoolean(val, System.Globalization.CultureInfo.InvariantCulture);
            }
        }
        catch (Exception ex)
        {
            Log.Warning("OEECalculator", $"ReadBool error at {path}: {ex.Message}");
        }
        return false;
    }

    private DateTime ReadDateTime(IUANode node, string path)
    {
        try
        {
            var variable = node.Get(path) as IUAVariable;
            if (variable?.Value?.Value != null)
            {
                var val = variable.Value.Value;
                
                // Try direct cast first
                if (val.GetType() == typeof(DateTime))
                    return (DateTime)val;
                
                // Try string parsing
                string strVal = val as string;
                if (strVal != null && DateTime.TryParse(strVal, out var parsed))
                    return parsed;
            }
        }
        catch (Exception ex)
        {
            Log.Warning("OEECalculator", $"ReadDateTime error at {path}: {ex.Message}");
        }
        return DateTime.Now;
    }

    #endregion

    #region Helper Methods - Write

    private void WriteDouble(IUANode node, string path, double value)
    {
        try
        {
            var variable = node.Get(path) as IUAVariable;
            if (variable != null)
            {
                if (double.IsNaN(value) || double.IsInfinity(value))
                {
                    Log.Warning("OEECalculator", $"Skipping invalid value at {path}: {value}");
                    return;
                }
                variable.SetValue(value);
            }
            else
            {
                Log.Warning("OEECalculator", $"Output path not found: {path}");
            }
        }
        catch (Exception ex)
        {
            Log.Warning("OEECalculator", $"Failed to write {path}: {ex.Message}");
        }
    }

    private void WriteInt(IUANode node, string path, int value)
    {
        try
        {
            var variable = node.Get(path) as IUAVariable;
            if (variable != null)
                variable.SetValue(value);
        }
        catch (Exception ex)
        {
            Log.Warning("OEECalculator", $"Failed to write {path}: {ex.Message}");
        }
    }

    private void WriteBool(IUANode node, string path, bool value)
    {
        try
        {
            var variable = node.Get(path) as IUAVariable;
            if (variable != null)
                variable.SetValue(value);
        }
        catch (Exception ex)
        {
            Log.Warning("OEECalculator", $"Failed to write {path}: {ex.Message}");
        }
    }

    private void WriteDateTime(IUANode node, string path, DateTime value)
    {
        try
        {
            var variable = node.Get(path) as IUAVariable;
            if (variable != null)
                variable.SetValue(value);
        }
        catch (Exception ex)
        {
            Log.Warning("OEECalculator", $"Failed to write {path}: {ex.Message}");
        }
    }

    private void WriteString(IUANode node, string path, string value)
    {
        try
        {
            var variable = node.Get(path) as IUAVariable;
            if (variable != null)
                variable.SetValue(value);
        }
        catch (Exception ex)
        {
            Log.Warning("OEECalculator", $"Failed to write {path}: {ex.Message}");
        }
    }

    #endregion
}
