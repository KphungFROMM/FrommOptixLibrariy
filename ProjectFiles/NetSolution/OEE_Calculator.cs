using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UAManagedCore;
using FTOptix.NetLogic;

/// <summary>
/// ObjectTypeOEE_Calculator - Works exclusively with OEE ObjectType instances
/// 
/// Setup:
/// 1. Add a NodePointer variable named "OEEDataSource" to this NetLogic
/// 2. Set its value to point to an OEE ObjectType instance 
/// 3. The script automatically uses the ObjectType structure for all calculations
/// 
/// Features:
/// - Clean, focused design for ObjectType usage only
/// - Automatic variable mapping from ObjectType structure
/// - Dynamic data source switching via SetOEEDataSource method
/// - Full OEE calculation suite with trending and statistics
/// </summary>
public class OEE_Calculator : BaseNetLogic
{
    // Configuration
    private IUAVariable OEEDataSourceVar; // NodePointer to OEE ObjectType instance
    private IUANode _oeeDataSource; // Resolved OEE data source

    // Input variables - resolved from ObjectType structure
    private IUAVariable TotalRuntimeSecondsVar;
    private IUAVariable GoodPartCountVar;
    private IUAVariable BadPartCountVar;
    private IUAVariable IdealCycleTimeSecondsVar;
    private IUAVariable PlannedProductionTimeHoursVar;
    private IUAVariable NumberOfShiftsVar;
    private IUAVariable ShiftStartTimeVar;
    private IUAVariable ProductionTargetVar;
    private IUAVariable UpdateRateMsVar;
    private IUAVariable QualityTargetVar;
    private IUAVariable PerformanceTargetVar;
    private IUAVariable AvailabilityTargetVar;
    private IUAVariable OEETargetVar;
    private IUAVariable LoggingVerbosityVar;

    // Output variables - resolved from ObjectType structure
    private IUAVariable TotalCountVar;
    private IUAVariable QualityVar;
    private IUAVariable PerformanceVar;
    private IUAVariable AvailabilityVar;
    private IUAVariable OEEVar;
    private IUAVariable AvgCycleTimeVar;
    private IUAVariable PartsPerHourVar;
    private IUAVariable ExpectedPartCountVar;
    private IUAVariable HoursPerShiftVar; // Moved from input to output
    private IUAVariable DowntimeFormattedVar;
    private IUAVariable TotalRuntimeFormattedVar;
    private IUAVariable CurrentShiftNumberVar;
    private IUAVariable ShiftStartTimeOutputVar;
    private IUAVariable ShiftEndTimeVar;
    private IUAVariable TimeIntoShiftVar;
    private IUAVariable TimeRemainingInShiftVar;
    private IUAVariable ShiftChangeOccurredVar;
    private IUAVariable ShiftChangeImminentVar;
    private IUAVariable ProjectedTotalCountVar;
    private IUAVariable RemainingTimeAtCurrentRateVar;
    private IUAVariable ProductionBehindScheduleVar;
    private IUAVariable RequiredRateToTargetVar;
    private IUAVariable TargetVsActualPartsVar;
    private IUAVariable ShiftProgressVar; // New: 0-100%
    private IUAVariable ProductionProgressVar; // New: 0-100%
    private IUAVariable SystemStatusVar;
    private IUAVariable CalculationValidVar;
    private IUAVariable DataQualityScoreVar;
    private IUAVariable LastUpdateTimeVar;

    // Trending outputs
    private IUAVariable QualityTrendVar;
    private IUAVariable PerformanceTrendVar;
    private IUAVariable AvailabilityTrendVar;
    private IUAVariable OEETrendVar;

    // Statistics outputs
    private IUAVariable MinQualityVar;
    private IUAVariable MaxQualityVar;
    private IUAVariable AvgQualityVar;
    private IUAVariable MinPerformanceVar;
    private IUAVariable MaxPerformanceVar;
    private IUAVariable AvgPerformanceVar;
    private IUAVariable MinAvailabilityVar;
    private IUAVariable MaxAvailabilityVar;
    private IUAVariable AvgAvailabilityVar;
    private IUAVariable MinOEEVar;
    private IUAVariable MaxOEEVar;
    private IUAVariable AvgOEEVar;

    // Target comparison outputs
    private IUAVariable QualityVsTargetVar;
    private IUAVariable PerformanceVsTargetVar;
    private IUAVariable AvailabilityVsTargetVar;
    private IUAVariable OEEVsTargetVar;

    // Configuration variables
    private IUAVariable EnableRealTimeCalcVar;
    private IUAVariable MinimumRunTimeVar;
    private IUAVariable GoodOEEThresholdVar;
    private IUAVariable PoorOEEThresholdVar;
    private IUAVariable EnableLoggingVar;
    private IUAVariable EnableAlarmsVar;
    private IUAVariable SystemHealthyVar;

    // Runtime
    private CancellationTokenSource _cts;
    private Task _loopTask;

    // Configuration
    private readonly Dictionary<IUAVariable, bool> _outputPresenceFlags = new Dictionary<IUAVariable, bool>();
    private readonly TimeSpan WriteRetryCooldown = TimeSpan.FromSeconds(30);
    private readonly Dictionary<IUAVariable, DateTime> _lastWriteFailureUtc = new Dictionary<IUAVariable, DateTime>();

    // Value change detection for efficiency
    private readonly Dictionary<IUAVariable, object> _lastWrittenValues = new Dictionary<IUAVariable, object>();

    // Cached values
    private object _cachedIdealRaw = null;
    private double _cachedIdealSeconds = 0.0;
    private bool _cachedIdealValid = false;

    private object _cachedPlannedRaw = null;
    private double _cachedPlannedHours = double.NaN;
    private bool _cachedPlannedValid = false;

    // Cache formatted strings to avoid repeated string operations
    private string _lastTotalRuntimeFormatted = "";
    private double _lastTotalRuntimeSeconds = -1;
    private string _lastDowntimeFormatted = "";
    private double _lastDowntimeSeconds = -1;

    // Trending data
    private readonly Queue<double> _qualityHistory = new Queue<double>();
    private readonly Queue<double> _performanceHistory = new Queue<double>();
    private readonly Queue<double> _availabilityHistory = new Queue<double>();
    private readonly Queue<double> _oeeHistory = new Queue<double>();
    private const int MaxHistorySize = 60;

    // Targets
    private double _qualityTarget = 95.0;
    private double _performanceTarget = 85.0;
    private double _availabilityTarget = 90.0;
    private double _oeeTarget = 72.7;
    private int _productionTarget = 1000;
    private int _numberOfShifts = 3;
    private string _shiftStartTime = "06:00:00";

    // Shift tracking
    private DateTime _lastShiftCalculation = DateTime.MinValue;
    private int _currentShiftNumber = 1;
    private DateTime _currentShiftStart = DateTime.Today.AddHours(6);
    private DateTime _currentShiftEnd = DateTime.Today.AddHours(14);
    private bool _shiftChangeOccurred = false;
    private bool _shiftChangeImminent = false;
    private double _calculatedHoursPerShift = 8.0;

    // Configuration
    private bool _enableRealTimeCalc = true;
    private double _minimumRunTime = 60.0;
    private double _goodOEEThreshold = 80.0;
    private double _poorOEEThreshold = 60.0;
    private bool _enableLogging = true;
    private bool _enableAlarms = true;
    private bool _systemHealthy = true;

    // State tracking
    private double _previousRuntimeSeconds = -1.0;

    // Settings
    private int _updateRateMs = 1000;
    private int _loggingVerbosity = 1;
    private bool _defaultsInitialized = false;

    public override void Start()
    {
        if (!InitializeDataSource())
        {
            LogError("Failed to initialize OEE data source. Ensure 'OEEDataSource' NodePointer variable is configured.");
            return;
        }

        InitializeVariables();
        CachePresenceFlags();
        // InitializeDefaultValues(); // Removed in favor of single EnsureInputDefaults method
        EnsureInputDefaults();
        ReadConfiguration();

        _cts = new CancellationTokenSource();
        _loopTask = Task.Run(() => RunLoopAsync(_cts.Token));

        LogInfo($"ObjectTypeOEE_Calculator started successfully with data source: {_oeeDataSource.BrowseName}");
    }

    public override void Stop()
    {
        try
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
                _loopTask?.Wait(500);
            }
        }
        catch { /* swallow stop-time exceptions */ }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            _loopTask = null;
            LogInfo("ObjectTypeOEE_Calculator stopped");
        }
    }

    private bool InitializeDataSource()
    {
        // Get the required OEEDataSource NodePointer
        OEEDataSourceVar = LogicObject.GetVariable("OEEDataSource");
        
        if (OEEDataSourceVar == null)
        {
            LogError("OEEDataSource NodePointer variable not found. Add a NodePointer variable named 'OEEDataSource' to this NetLogic.");
            return false;
        }

        var nodeId = GetUnderlyingValue(OEEDataSourceVar);
        if (nodeId == null)
        {
            LogError("OEEDataSource NodePointer is not set. Point it to an OEE ObjectType instance.");
            return false;
        }

        try
        {
            if (nodeId is NodeId nId)
            {
                _oeeDataSource = LogicObject.Context.GetNode(nId);
            }
            else if (nodeId.ToString() != "00000000-0000-0000-0000-000000000000")
            {
                // For string representations, try direct context resolution
                try
                {
                    // Try to resolve by browse path if it's a string
                    var resolveResult = LogicObject.Context.ResolvePath(LogicObject, nodeId.ToString());
                    if (resolveResult?.ResolvedNode != null)
                    {
                        _oeeDataSource = resolveResult.ResolvedNode;
                    }
                }
                catch
                {
                    LogError($"Failed to resolve NodeId from: {nodeId}");
                }
            }

            if (_oeeDataSource == null)
            {
                LogError($"Cannot resolve OEEDataSource NodeId: {nodeId}");
                return false;
            }

            LogInfo($"Successfully connected to OEE data source: {_oeeDataSource.BrowseName}");
            return true;
        }
        catch (Exception ex)
        {
            LogError($"Failed to resolve OEEDataSource: {ex.Message}");
            return false;
        }
    }

    private void InitializeVariables()
    {
        // Map ObjectType structure to calculator variables
        
        // Input mappings - Data
        TotalRuntimeSecondsVar = GetVariableFromPath("Inputs/Data/TotalRuntimeSeconds");
        GoodPartCountVar = GetVariableFromPath("Inputs/Data/GoodPartCount");
        BadPartCountVar = GetVariableFromPath("Inputs/Data/BadPartCount");
        
        // Input mappings - Production
        IdealCycleTimeSecondsVar = GetVariableFromPath("Inputs/Production/IdealCycleTimeSeconds");
        PlannedProductionTimeHoursVar = GetVariableFromPath("Inputs/Production/PlannedProductionTimeHours");
        NumberOfShiftsVar = GetVariableFromPath("Inputs/Production/NumberOfShifts");
        ShiftStartTimeVar = GetVariableFromPath("Inputs/Production/ShiftStartTime");
        ProductionTargetVar = GetVariableFromPath("Inputs/Production/ProductionTarget");

        // Input mappings - System
        UpdateRateMsVar = GetVariableFromPath("Inputs/System/UpdateRateMs");
        LoggingVerbosityVar = GetVariableFromPath("Inputs/System/LoggingVerbosity");

        // Input mappings - Targets
        QualityTargetVar = GetVariableFromPath("Inputs/Targets/QualityTarget");
        PerformanceTargetVar = GetVariableFromPath("Inputs/Targets/PerformanceTarget");
        AvailabilityTargetVar = GetVariableFromPath("Inputs/Targets/AvailabilityTarget");
        OEETargetVar = GetVariableFromPath("Inputs/Targets/OEETarget");
        
        // Configuration folder mappings
        EnableRealTimeCalcVar = GetVariableFromPath("Configuration/EnableRealTimeCalc");
        MinimumRunTimeVar = GetVariableFromPath("Configuration/MinimumRunTime");
        GoodOEEThresholdVar = GetVariableFromPath("Configuration/GoodOEE_Threshold");
        PoorOEEThresholdVar = GetVariableFromPath("Configuration/PoorOEE_Threshold");
        EnableLoggingVar = GetVariableFromPath("Configuration/EnableLogging");
        EnableAlarmsVar = GetVariableFromPath("Configuration/EnableAlarms");
        SystemHealthyVar = GetVariableFromPath("Configuration/SystemHealthy");

        // Output mappings - Core
        TotalCountVar = GetVariableFromPath("Outputs/Core/TotalCount");
        QualityVar = GetVariableFromPath("Outputs/Core/Quality");
        PerformanceVar = GetVariableFromPath("Outputs/Core/Performance");
        AvailabilityVar = GetVariableFromPath("Outputs/Core/Availability");
        OEEVar = GetVariableFromPath("Outputs/Core/OEE");
        AvgCycleTimeVar = GetVariableFromPath("Outputs/Core/AvgCycleTime");
        PartsPerHourVar = GetVariableFromPath("Outputs/Core/PartsPerHour");
        ExpectedPartCountVar = GetVariableFromPath("Outputs/Core/ExpectedPartCount");

        // Output mappings - Shift
        HoursPerShiftVar = GetVariableFromPath("Outputs/Shift/HoursPerShift");
        CurrentShiftNumberVar = GetVariableFromPath("Outputs/Shift/CurrentShiftNumber");
        ShiftStartTimeOutputVar = GetVariableFromPath("Outputs/Shift/ShiftStartTimeOutput");
        ShiftEndTimeVar = GetVariableFromPath("Outputs/Shift/ShiftEndTime");
        TimeIntoShiftVar = GetVariableFromPath("Outputs/Shift/TimeIntoShift");
        TimeRemainingInShiftVar = GetVariableFromPath("Outputs/Shift/TimeRemainingInShift");
        ShiftChangeOccurredVar = GetVariableFromPath("Outputs/Shift/ShiftChangeOccurred");
        ShiftChangeImminentVar = GetVariableFromPath("Outputs/Shift/ShiftChangeImminent");
        ShiftProgressVar = GetVariableFromPath("Outputs/Shift/ShiftProgress");

        // Output mappings - Production
        ProjectedTotalCountVar = GetVariableFromPath("Outputs/Production/ProjectedTotalCount");
        RemainingTimeAtCurrentRateVar = GetVariableFromPath("Outputs/Production/RemainingTimeAtCurrentRate");
        ProductionBehindScheduleVar = GetVariableFromPath("Outputs/Production/ProductionBehindSchedule");
        RequiredRateToTargetVar = GetVariableFromPath("Outputs/Production/RequiredRateToTarget");
        TargetVsActualPartsVar = GetVariableFromPath("Outputs/Production/TargetVsActualParts");
        ProductionProgressVar = GetVariableFromPath("Outputs/Production/ProductionProgress");

        // Output mappings - System
        SystemStatusVar = GetVariableFromPath("Outputs/System/SystemStatus");
        CalculationValidVar = GetVariableFromPath("Outputs/System/CalculationValid");
        DataQualityScoreVar = GetVariableFromPath("Outputs/System/DataQualityScore");
        LastUpdateTimeVar = GetVariableFromPath("Outputs/System/LastUpdateTime");
        DowntimeFormattedVar = GetVariableFromPath("Outputs/System/DowntimeFormatted");
        TotalRuntimeFormattedVar = GetVariableFromPath("Outputs/System/TotalRuntimeFormatted");

        // Trending variables
        QualityTrendVar = GetVariableFromPath("Outputs/Trends/QualityTrend");
        PerformanceTrendVar = GetVariableFromPath("Outputs/Trends/PerformanceTrend");
        AvailabilityTrendVar = GetVariableFromPath("Outputs/Trends/AvailabilityTrend");
        OEETrendVar = GetVariableFromPath("Outputs/Trends/OEETrend");

        // Statistics variables
        MinQualityVar = GetVariableFromPath("Outputs/Statistics/MinQuality");
        MaxQualityVar = GetVariableFromPath("Outputs/Statistics/MaxQuality");
        AvgQualityVar = GetVariableFromPath("Outputs/Statistics/AvgQuality");
        MinPerformanceVar = GetVariableFromPath("Outputs/Statistics/MinPerformance");
        MaxPerformanceVar = GetVariableFromPath("Outputs/Statistics/MaxPerformance");
        AvgPerformanceVar = GetVariableFromPath("Outputs/Statistics/AvgPerformance");
        MinAvailabilityVar = GetVariableFromPath("Outputs/Statistics/MinAvailability");
        MaxAvailabilityVar = GetVariableFromPath("Outputs/Statistics/MaxAvailability");
        AvgAvailabilityVar = GetVariableFromPath("Outputs/Statistics/AvgAvailability");
        MinOEEVar = GetVariableFromPath("Outputs/Statistics/MinOEE");
        MaxOEEVar = GetVariableFromPath("Outputs/Statistics/MaxOEE");
        AvgOEEVar = GetVariableFromPath("Outputs/Statistics/AvgOEE");

        // Target comparison variables
        QualityVsTargetVar = GetVariableFromPath("Outputs/Targets/QualityVsTarget");
        PerformanceVsTargetVar = GetVariableFromPath("Outputs/Targets/PerformanceVsTarget");
        AvailabilityVsTargetVar = GetVariableFromPath("Outputs/Targets/AvailabilityVsTarget");
        OEEVsTargetVar = GetVariableFromPath("Outputs/Targets/OEEVsTarget");

        int foundVars = CountNonNullVariables();
        LogInfo($"Initialized {foundVars} variables from ObjectType structure");
        
        if (foundVars < 5)
        {
            LogWarning("Few variables found. Verify ObjectType structure matches expected paths.");
        }
    }

    private IUAVariable GetVariableFromPath(string path)
    {
        if (_oeeDataSource == null || string.IsNullOrEmpty(path))
            return null;

        try
        {
            var result = _oeeDataSource.Get(path);
            return result as IUAVariable;
        }
        catch
        {
            return null;
        }
    }

    private int CountNonNullVariables()
    {
        var allVars = new IUAVariable[] 
        {
            TotalRuntimeSecondsVar, GoodPartCountVar, BadPartCountVar, IdealCycleTimeSecondsVar,
            PlannedProductionTimeHoursVar, NumberOfShiftsVar, ShiftStartTimeVar, ProductionTargetVar,
            TotalCountVar, QualityVar, PerformanceVar, AvailabilityVar, OEEVar, AvgCycleTimeVar,
            PartsPerHourVar, SystemStatusVar, CalculationValidVar, DataQualityScoreVar,
            HoursPerShiftVar, CurrentShiftNumberVar, ShiftChangeOccurredVar
        };
        
        return allVars.Count(v => v != null);
    }

    [ExportMethod]
    public void SetOEEDataSource(NodeId oeeInstanceNodeId)
    {
        try
        {
            if (OEEDataSourceVar != null)
            {
                OEEDataSourceVar.SetValue(oeeInstanceNodeId);
                LogInfo($"OEE Data Source updated to: {oeeInstanceNodeId}");
                
                // Reinitialize with new data source
                if (InitializeDataSource())
                {
                    InitializeVariables();
                    CachePresenceFlags();
                    LogInfo("Successfully switched to new OEE data source");
                }
            }
            else
            {
                LogError("OEEDataSource variable not found. Add a NodePointer variable named 'OEEDataSource' to this NetLogic.");
            }
        }
        catch (Exception ex)
        {
            LogError($"Failed to set OEE data source: {ex.Message}");
        }
    }

    [ExportMethod]
    public void GetDataSourceInfo()
    {
        try
        {
            LogInfo("=== ObjectType OEE Calculator Information ===");
            LogInfo($"Data Source: {_oeeDataSource?.BrowseName ?? "Not Connected"}");
            LogInfo($"Data Source NodeId: {_oeeDataSource?.NodeId ?? NodeId.Empty}");
            LogInfo($"Variables Found: {CountNonNullVariables()}");
            
            LogInfo("=== Key Variable Status ===");
            LogVariableStatus("Quality Input (Good Parts)", GoodPartCountVar);
            LogVariableStatus("Quality Input (Bad Parts)", BadPartCountVar);
            LogVariableStatus("Performance Input (Runtime)", TotalRuntimeSecondsVar);
            LogVariableStatus("Performance Input (Ideal Cycle)", IdealCycleTimeSecondsVar);
            LogInfo("=== Output Variable Status ===");
            LogVariableStatus("Quality Output", QualityVar);
            LogVariableStatus("Performance Output", PerformanceVar);
            LogVariableStatus("Availability Output", AvailabilityVar);
            LogVariableStatus("OEE Output", OEEVar);
        }
        catch (Exception ex)
        {
            LogError($"Error getting data source info: {ex.Message}");
        }
    }

    [ExportMethod]
    public void ForceCalculation()
    {
        try
        {
            LogInfo("Manual calculation triggered...");
            var calculations = PerformCalculations();
            UpdateTrendingData(calculations);
            WriteAllOutputs(calculations);
            LogInfo($"Manual calculation completed. OEE: {calculations.OEE:F2}%");
        }
        catch (Exception ex)
        {
            LogError($"Error in manual calculation: {ex.Message}");
        }
    }

    private void LogVariableStatus(string name, IUAVariable var)
    {
        if (var != null)
        {
            var value = GetUnderlyingValue(var);
            LogInfo($"  ✓ {name}: {var.BrowseName} = {value ?? "null"}");
        }
        else
        {
            LogInfo($"  ✗ {name}: NOT FOUND");
        }
    }

    private void CachePresenceFlags()
    {
        var allOutputVars = new IUAVariable[]
        {
            // Core outputs
            TotalCountVar, QualityVar, PerformanceVar, AvailabilityVar, OEEVar, 
            AvgCycleTimeVar, PartsPerHourVar, ExpectedPartCountVar, HoursPerShiftVar,
            
            // Time and shift outputs
            DowntimeFormattedVar, TotalRuntimeFormattedVar, CurrentShiftNumberVar,
            ShiftStartTimeOutputVar, ShiftEndTimeVar, TimeIntoShiftVar, TimeRemainingInShiftVar,
            ShiftChangeOccurredVar, ShiftChangeImminentVar,
            
            // Production analysis outputs
            ProjectedTotalCountVar, RemainingTimeAtCurrentRateVar, ProductionBehindScheduleVar,
            RequiredRateToTargetVar, TargetVsActualPartsVar, ShiftProgressVar, ProductionProgressVar,
            
            // System outputs
            SystemStatusVar, CalculationValidVar, DataQualityScoreVar, LastUpdateTimeVar,
            
            // Trending outputs
            QualityTrendVar, PerformanceTrendVar, AvailabilityTrendVar, OEETrendVar,
            
            // Statistics outputs
            MinQualityVar, MaxQualityVar, AvgQualityVar,
            MinPerformanceVar, MaxPerformanceVar, AvgPerformanceVar,
            MinAvailabilityVar, MaxAvailabilityVar, AvgAvailabilityVar,
            MinOEEVar, MaxOEEVar, AvgOEEVar,
            
            // Target comparison outputs
            QualityVsTargetVar, PerformanceVsTargetVar, AvailabilityVsTargetVar, OEEVsTargetVar
        };

        foreach (var var in allOutputVars)
        {
            if (var != null)
            {
                _outputPresenceFlags[var] = true;
            }
        }
    }

    private void EnsureInputDefaults()
    {
        if (_defaultsInitialized) return;

        // Initialize all input variables with fallback defaults to ensure consistent behavior
        LogInfo("Initializing input variables with default fallback values...");
        
        // Core input variables with their fallbacks - write to UI
        WriteDefaultIfEmpty(TotalRuntimeSecondsVar, 0.0, "TotalRuntimeSeconds");
        WriteDefaultIfEmpty(GoodPartCountVar, 0, "GoodPartCount");
        WriteDefaultIfEmpty(BadPartCountVar, 0, "BadPartCount");
        WriteDefaultIfEmpty(IdealCycleTimeSecondsVar, 30.0, "IdealCycleTimeSeconds");
        WriteDefaultIfEmpty(PlannedProductionTimeHoursVar, 8.0, "PlannedProductionTimeHours");
        WriteDefaultIfEmpty(NumberOfShiftsVar, 3, "NumberOfShifts");
        WriteDefaultIfEmpty(ShiftStartTimeVar, "06:00:00", "ShiftStartTime");
        WriteDefaultIfEmpty(ProductionTargetVar, 1000, "ProductionTarget");
        
        // Configuration input variables with fallbacks - write to UI
        WriteDefaultIfEmpty(UpdateRateMsVar, 1000, "UpdateRateMs");
        WriteDefaultIfEmpty(QualityTargetVar, 95.0, "QualityTarget");
        WriteDefaultIfEmpty(PerformanceTargetVar, 85.0, "PerformanceTarget");
        WriteDefaultIfEmpty(AvailabilityTargetVar, 90.0, "AvailabilityTarget");
        WriteDefaultIfEmpty(OEETargetVar, 72.7, "OEETarget");
        WriteDefaultIfEmpty(LoggingVerbosityVar, 1, "LoggingVerbosity");
        
        // Configuration variables with fallbacks - write to UI
        WriteDefaultIfEmpty(EnableRealTimeCalcVar, true, "EnableRealTimeCalc");
        WriteDefaultIfEmpty(MinimumRunTimeVar, 60.0, "MinimumRunTime");
        WriteDefaultIfEmpty(GoodOEEThresholdVar, 80.0, "GoodOEE_Threshold");
        WriteDefaultIfEmpty(PoorOEEThresholdVar, 60.0, "PoorOEE_Threshold");
        WriteDefaultIfEmpty(EnableLoggingVar, true, "EnableLogging");
        WriteDefaultIfEmpty(EnableAlarmsVar, true, "EnableAlarms");
        WriteDefaultIfEmpty(SystemHealthyVar, true, "SystemHealthy");
        
        _defaultsInitialized = true;
        LogInfo("Input variable initialization completed. Default values are now visible in UI.");
    }

    private void WriteDefaultIfEmpty(IUAVariable var, object defaultValue, string varName)
    {
        if (var == null) 
        {
            LogWarning($"Variable {varName} not found - cannot set default value");
            return;
        }
        
        try
        {
            var currentValue = GetUnderlyingValue(var);
            
            // Check if value is null, empty, or invalid
            bool needsDefault = false;
            
            if (currentValue == null)
            {
                needsDefault = true;
            }
            else if (defaultValue is double)
            {
                if (currentValue is double d && (double.IsNaN(d) || double.IsInfinity(d)))
                    needsDefault = true;
                else if (!double.TryParse(currentValue.ToString(), out double testDouble))
                    needsDefault = true;
                else if (testDouble <= 0.0 && (varName.Contains("Target") || varName.Contains("CycleTime") || varName.Contains("Planned"))) // Special case for target values, cycle time, and planned time
                    needsDefault = true;
            }
            else if (defaultValue is int)
            {
                if (!int.TryParse(currentValue.ToString(), out int testInt))
                    needsDefault = true;
                else if (testInt <= 0 && (varName.Contains("Target") || varName.Contains("Shifts"))) // Special case for targets and shifts
                    needsDefault = true;
            }
            else if (defaultValue is bool)
            {
                // For booleans, only set default if truly invalid
                if (!bool.TryParse(currentValue.ToString(), out _))
                    needsDefault = true;
            }
            else if (defaultValue is string)
            {
                if (string.IsNullOrWhiteSpace(currentValue?.ToString()))
                    needsDefault = true;
            }
            
            if (needsDefault)
            {
                // Write the default value to the variable so it appears in UI
                var.SetValue(defaultValue);
                LogInfo($"✓ Set UI default for {varName}: {defaultValue}");
            }
            else
            {
                LogInfo($"• {varName} already has value: {currentValue}");
            }
        }
        catch (Exception ex)
        {
            LogError($"Failed to write default to UI for '{varName}': {ex.Message}");
        }
    }

    private void ReadConfiguration()
    {
        // Read update rate with fallback
        if (UpdateRateMsVar != null && ReadIntVar(UpdateRateMsVar, -1) is int u && u > 0)
            _updateRateMs = u;
        else
            _updateRateMs = 1000; // Default fallback

        // Read target values with fallbacks
        _qualityTarget = ReadDoubleVar(QualityTargetVar, 95.0);
        _performanceTarget = ReadDoubleVar(PerformanceTargetVar, 85.0);
        _availabilityTarget = ReadDoubleVar(AvailabilityTargetVar, 90.0);
        _oeeTarget = ReadDoubleVar(OEETargetVar, 72.7);
        _productionTarget = ReadIntVar(ProductionTargetVar, 1000);
        
        // Read number of shifts
        int newNumberOfShifts = ReadIntVar(NumberOfShiftsVar, 3);
        _numberOfShifts = newNumberOfShifts;
        
        // Read shift start time with fallback
        if (ShiftStartTimeVar != null)
        {
            var shiftTimeStr = GetUnderlyingValue(ShiftStartTimeVar)?.ToString();
            if (!string.IsNullOrWhiteSpace(shiftTimeStr))
                _shiftStartTime = shiftTimeStr;
            else
                _shiftStartTime = "06:00:00"; // Default fallback
        }
        else
        {
            _shiftStartTime = "06:00:00"; // Default fallback
        }
        
        // Calculate hours per shift from 24 hours divided by number of shifts
        _calculatedHoursPerShift = _numberOfShifts > 0 ? 24.0 / _numberOfShifts : 8.0;
        
        // Read configuration flags with fallbacks
        _enableRealTimeCalc = ReadBoolVar(EnableRealTimeCalcVar, true);
        _minimumRunTime = ReadDoubleVar(MinimumRunTimeVar, 60.0);
        _goodOEEThreshold = ReadDoubleVar(GoodOEEThresholdVar, 80.0);
        _poorOEEThreshold = ReadDoubleVar(PoorOEEThresholdVar, 60.0);
        _enableLogging = ReadBoolVar(EnableLoggingVar, true);
        _enableAlarms = ReadBoolVar(EnableAlarmsVar, true);
        _systemHealthy = ReadBoolVar(SystemHealthyVar, true);

        // Read logging verbosity with fallback
        if (LoggingVerbosityVar != null)
            _loggingVerbosity = ReadIntVar(LoggingVerbosityVar, 1);
        else
            _loggingVerbosity = 1; // Default fallback
            
        LogInfo($"Configuration loaded - UpdateRate: {_updateRateMs}ms, Shifts: {_numberOfShifts}, StartTime: {_shiftStartTime}");
    }

    private void UpdatePlannedProductionTimeForShifts(int numberOfShifts)
    {
        if (PlannedProductionTimeHoursVar == null)
        {
            LogWarning("PlannedProductionTimeHours variable not found - cannot update for shift changes");
            return;
        }

        try
        {
            // Calculate hours per shift based on 24-hour day
            double hoursPerShift = numberOfShifts > 0 ? 24.0 / numberOfShifts : 8.0;
            
            // Set the planned production time to match the shift duration
            PlannedProductionTimeHoursVar.SetValue(hoursPerShift);
            
            LogInfo($"✓ Updated PlannedProductionTimeHours to {hoursPerShift} hours for {numberOfShifts} shifts");
            
            // Clear cached planned production values to force recalculation
            _cachedPlannedRaw = null;
            _cachedPlannedHours = double.NaN;
            _cachedPlannedValid = false;
        }
        catch (Exception ex)
        {
            LogError($"Failed to update planned production time for shifts: {ex.Message}");
        }
    }

    private async Task RunLoopAsync(CancellationToken token)
    {
        int loopCount = 0;
        DateTime lastConfigCheck = DateTime.MinValue;
        
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_updateRateMs, token);

                // Check configuration every 2-3 seconds regardless of update rate
                var timeSinceConfigCheck = DateTime.Now - lastConfigCheck;
                if (timeSinceConfigCheck.TotalSeconds >= 2.0)
                {
                    ReadConfiguration();
                    lastConfigCheck = DateTime.Now;
                }

                var calculations = PerformCalculations();
                UpdateTrendingData(calculations);
                WriteAllOutputs(calculations);
                
                loopCount++;
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                LogError($"RunLoop error: {ex.Message}");
            }
        }
    }

    private CalculationResults PerformCalculations()
    {
        var results = new CalculationResults();
        var now = DateTime.Now;
        
        // Update shift information first
        UpdateShiftInformation(now, results);

        double runtimeSeconds = ReadDoubleVar(TotalRuntimeSecondsVar, 0.0);
        int good = ReadIntVar(GoodPartCountVar, 0);
        int bad = ReadIntVar(BadPartCountVar, 0);
        int totalCount = good + bad;

        results.TotalCount = totalCount;
        results.GoodPartCount = good;
        results.BadPartCount = bad;
        results.RuntimeSeconds = runtimeSeconds;

        // Quality calculation
        results.Quality = totalCount == 0 ? 0.0 : ((double)good / totalCount) * 100.0;

        // Performance calculation
        double idealCycle = GetIdealCycleTimeSeconds();
        
        if (totalCount == 0)
        {
            results.Performance = 0.0;
        }
        else if (runtimeSeconds <= 0.0)
        {
            results.Performance = 100.0;
        }
        else if (idealCycle <= 0.0)
        {
            results.Performance = 0.0;
        }
        else
        {
            double idealTime = idealCycle * totalCount;
            double performanceRatio = idealTime / runtimeSeconds;
            results.Performance = Math.Min(999.9, performanceRatio * 100.0);
        }

        // Availability calculation
        double plannedSeconds = GetPlannedProductionSeconds();
        
        if (!double.IsNaN(plannedSeconds) && plannedSeconds > 0.0)
        {
            results.Availability = Math.Max(0.0, Math.Min(100.0, (runtimeSeconds / plannedSeconds) * 100.0));
        }
        else
        {
            results.Availability = 100.0;
        }

        // OEE calculation
        results.OEE = (results.Quality * results.Performance * results.Availability) / 10000.0;

        // Derived metrics
        results.PartsPerHour = runtimeSeconds > 0.0 ? (totalCount / runtimeSeconds) * 3600.0 : 0.0;
        results.AvgCycleTime = totalCount > 0 ? runtimeSeconds / totalCount : idealCycle;
        
        if (plannedSeconds > 0.0 && idealCycle > 0.0)
            results.ExpectedPartCount = plannedSeconds / idealCycle;
        else
            results.ExpectedPartCount = 480.0;

        // Hours per shift (derived from number of shifts)
        results.HoursPerShift = _calculatedHoursPerShift;

        // Time formatting with caching
        if (Math.Abs(runtimeSeconds - _lastTotalRuntimeSeconds) > 0.1)
        {
            _lastTotalRuntimeFormatted = FormatTimeSpan(TimeSpan.FromSeconds(runtimeSeconds));
            _lastTotalRuntimeSeconds = runtimeSeconds;
        }
        results.TotalRuntimeFormatted = _lastTotalRuntimeFormatted;
        
        double downtimeSeconds = Math.Max(0, plannedSeconds - runtimeSeconds);
        if (Math.Abs(downtimeSeconds - _lastDowntimeSeconds) > 0.1)
        {
            _lastDowntimeFormatted = FormatTimeSpan(TimeSpan.FromSeconds(downtimeSeconds));
            _lastDowntimeSeconds = downtimeSeconds;
        }
        results.DowntimeFormatted = _lastDowntimeFormatted;
        
        // Production analysis
        results.TargetVsActualParts = totalCount - _productionTarget;
        results.ProductionBehindSchedule = totalCount < _productionTarget;
        
        // Calculate Production Progress %
        if (_productionTarget > 0)
            results.ProductionProgress = Math.Min(100.0, ((double)totalCount / _productionTarget) * 100.0);
        else
            results.ProductionProgress = 0.0;

        if (runtimeSeconds > 0)
        {
            double currentRate = totalCount / (runtimeSeconds / 3600.0); // parts per hour
            double remainingHours = Math.Max(0, (_currentShiftEnd - now).TotalHours);
            results.RequiredRateToTarget = remainingHours > 0 ? Math.Max(0, (_productionTarget - totalCount) / remainingHours) : 0;
            results.ProjectedTotalCount = (int)(totalCount + (currentRate * remainingHours));
            
            if (currentRate > 0)
            {
                double hoursToTarget = Math.Max(0, (_productionTarget - totalCount) / currentRate);
                results.RemainingTimeAtCurrentRate = FormatTimeSpan(TimeSpan.FromHours(hoursToTarget));
            }
            else
            {
                results.RemainingTimeAtCurrentRate = "Indefinite";
            }
        }
        else
        {
            results.RequiredRateToTarget = 0;
            results.ProjectedTotalCount = totalCount;
            results.RemainingTimeAtCurrentRate = "Not Running";
        }

        // Target comparisons
        results.QualityVsTarget = results.Quality - _qualityTarget;
        results.PerformanceVsTarget = results.Performance - _performanceTarget;
        results.AvailabilityVsTarget = results.Availability - _availabilityTarget;
        results.OEEVsTarget = results.OEE - _oeeTarget;

        // System status (Option B: Check if runtime is increasing)
        results.SystemStatus = DetermineSystemStatus(runtimeSeconds);
        _previousRuntimeSeconds = runtimeSeconds;

        results.CalculationValid = _enableRealTimeCalc && runtimeSeconds >= _minimumRunTime;
        results.DataQualityScore = CalculateDataQuality(results);
        results.LastUpdateTime = now.ToString("yyyy-MM-dd HH:mm:ss");

        return results;
    }

    private string DetermineSystemStatus(double currentRuntime)
    {
        // If this is the first check, we can't determine trend yet
        if (_previousRuntimeSeconds < 0) return "Stopped";

        // If runtime has increased since the last cycle, the machine is running
        // We use a small tolerance to handle potential floating point oddities, though unlikely with time
        if (currentRuntime > _previousRuntimeSeconds + 0.001) return "Running";
        
        return "Stopped";
    }

    private void UpdateTrendingData(CalculationResults results)
    {
        // Add to history (only if significantly different to reduce noise)
        bool shouldAddToHistory = _qualityHistory.Count == 0 || 
                                 Math.Abs(_qualityHistory.Last() - results.Quality) > 0.1;
        
        if (shouldAddToHistory)
        {
            _qualityHistory.Enqueue(results.Quality);
            _performanceHistory.Enqueue(results.Performance);
            _availabilityHistory.Enqueue(results.Availability);
            _oeeHistory.Enqueue(results.OEE);

            // Maintain window size
            while (_qualityHistory.Count > MaxHistorySize) _qualityHistory.Dequeue();
            while (_performanceHistory.Count > MaxHistorySize) _performanceHistory.Dequeue();
            while (_availabilityHistory.Count > MaxHistorySize) _availabilityHistory.Dequeue();
            while (_oeeHistory.Count > MaxHistorySize) _oeeHistory.Dequeue();
        }

        // Calculate trends and statistics (only when we have enough data)
        if (_qualityHistory.Count >= 2)
        {
            results.QualityTrend = CalculateTrend(_qualityHistory);
            results.PerformanceTrend = CalculateTrend(_performanceHistory);
            results.AvailabilityTrend = CalculateTrend(_availabilityHistory);
            results.OEETrend = CalculateTrend(_oeeHistory);
        }

        if (_qualityHistory.Count >= 1)
        {
            var qualityArray = _qualityHistory.ToArray();
            results.MinQuality = qualityArray.Min();
            results.MaxQuality = qualityArray.Max();
            results.AvgQuality = qualityArray.Average();

            var performanceArray = _performanceHistory.ToArray();
            results.MinPerformance = performanceArray.Min();
            results.MaxPerformance = performanceArray.Max();
            results.AvgPerformance = performanceArray.Average();

            var availabilityArray = _availabilityHistory.ToArray();
            results.MinAvailability = availabilityArray.Min();
            results.MaxAvailability = availabilityArray.Max();
            results.AvgAvailability = availabilityArray.Average();

            var oeeArray = _oeeHistory.ToArray();
            results.MinOEE = oeeArray.Min();
            results.MaxOEE = oeeArray.Max();
            results.AvgOEE = oeeArray.Average();
        }
    }

    private string CalculateTrend(Queue<double> history)
    {
        if (history.Count < 2) return "Insufficient Data";
        
        var dataArray = history.ToArray();
        
        if (dataArray.Length <= 4)
        {
            double simpleChange = dataArray[dataArray.Length - 1] - dataArray[0];
            
            if (simpleChange >= 2.0) return "Rising Strongly";
            if (simpleChange >= 0.5) return "Rising";
            if (simpleChange <= -2.0) return "Falling Strongly";
            if (simpleChange <= -0.5) return "Falling";
            return "Stable";
        }
        
        int halfSize = Math.Max(dataArray.Length / 2, 2);
        var firstHalf = dataArray.Take(halfSize);
        var secondHalf = dataArray.Skip(dataArray.Length - halfSize);
        
        double avgChange = secondHalf.Average() - firstHalf.Average();
        
        if (avgChange >= 2.0) return "Rising Strongly";
        if (avgChange >= 0.5) return "Rising";
        if (avgChange <= -2.0) return "Falling Strongly";
        if (avgChange <= -0.5) return "Falling";
        return "Stable";
    }

    private double GetIdealCycleTimeSeconds()
    {
        var idealRaw = GetUnderlyingValue(IdealCycleTimeSecondsVar);
        if (!object.Equals(idealRaw, _cachedIdealRaw))
        {
            if (TryGetDoubleFromRaw(idealRaw, out double sec))
            {
                _cachedIdealSeconds = sec;
                _cachedIdealValid = sec > 0.0;
            }
            else
            {
                _cachedIdealSeconds = 1.0;
                _cachedIdealValid = true;
            }
            _cachedIdealRaw = idealRaw;
        }
        return _cachedIdealValid ? _cachedIdealSeconds : 1.0;
    }

    private double GetPlannedProductionSeconds()
    {
        var plannedRaw = GetUnderlyingValue(PlannedProductionTimeHoursVar);
        if (!object.Equals(plannedRaw, _cachedPlannedRaw))
        {
            if (TryGetDoubleFromRaw(plannedRaw, out double hours))
            {
                _cachedPlannedHours = hours;
                _cachedPlannedValid = hours > 0.0;
            }
            else
            {
                _cachedPlannedHours = double.NaN;
                _cachedPlannedValid = false;
            }
            _cachedPlannedRaw = plannedRaw;
        }

        if (_cachedPlannedValid)
        {
            return _cachedPlannedHours * 3600.0;
        }

        return 28800.0; // Default: 8 hours
    }

    private void WriteAllOutputs(CalculationResults results)
    {
        WriteIfExists(TotalCountVar, results.TotalCount);
        WriteIfExists(QualityVar, results.Quality);
        WriteIfExists(PerformanceVar, results.Performance);
        WriteIfExists(AvailabilityVar, results.Availability);
        WriteIfExists(OEEVar, results.OEE);
        WriteIfExists(AvgCycleTimeVar, results.AvgCycleTime);
        WriteIfExists(PartsPerHourVar, results.PartsPerHour);
        WriteIfExists(ExpectedPartCountVar, results.ExpectedPartCount);
        WriteIfExists(HoursPerShiftVar, results.HoursPerShift);
        WriteIfExists(DowntimeFormattedVar, results.DowntimeFormatted);
        WriteIfExists(TotalRuntimeFormattedVar, results.TotalRuntimeFormatted);
        WriteIfExists(CurrentShiftNumberVar, results.CurrentShiftNumber);
        WriteIfExists(ShiftStartTimeOutputVar, results.ShiftStartTimeOutput);
        WriteIfExists(ShiftEndTimeVar, results.ShiftEndTime);
        WriteIfExists(TimeIntoShiftVar, results.TimeIntoShift);
        WriteIfExists(TimeRemainingInShiftVar, results.TimeRemainingInShift);
        WriteIfExists(ShiftChangeOccurredVar, results.ShiftChangeOccurred);
        WriteIfExists(ShiftChangeImminentVar, results.ShiftChangeImminent);
        WriteIfExists(ProjectedTotalCountVar, results.ProjectedTotalCount);
        WriteIfExists(RemainingTimeAtCurrentRateVar, results.RemainingTimeAtCurrentRate);
        WriteIfExists(ProductionBehindScheduleVar, results.ProductionBehindSchedule);
        WriteIfExists(RequiredRateToTargetVar, results.RequiredRateToTarget);
        WriteIfExists(TargetVsActualPartsVar, results.TargetVsActualParts);
        WriteIfExists(ShiftProgressVar, results.ShiftProgress);
        WriteIfExists(ProductionProgressVar, results.ProductionProgress);
        WriteIfExists(SystemStatusVar, results.SystemStatus);
        WriteIfExists(CalculationValidVar, results.CalculationValid);
        WriteIfExists(DataQualityScoreVar, results.DataQualityScore);
        WriteIfExists(LastUpdateTimeVar, results.LastUpdateTime);

        // Trending
        WriteIfExists(QualityTrendVar, results.QualityTrend);
        WriteIfExists(PerformanceTrendVar, results.PerformanceTrend);
        WriteIfExists(AvailabilityTrendVar, results.AvailabilityTrend);
        WriteIfExists(OEETrendVar, results.OEETrend);

        // Statistics
        WriteIfExists(MinQualityVar, results.MinQuality);
        WriteIfExists(MaxQualityVar, results.MaxQuality);
        WriteIfExists(AvgQualityVar, results.AvgQuality);
        WriteIfExists(MinPerformanceVar, results.MinPerformance);
        WriteIfExists(MaxPerformanceVar, results.MaxPerformance);
        WriteIfExists(AvgPerformanceVar, results.AvgPerformance);
        WriteIfExists(MinAvailabilityVar, results.MinAvailability);
        WriteIfExists(MaxAvailabilityVar, results.MaxAvailability);
        WriteIfExists(AvgAvailabilityVar, results.AvgAvailability);
        WriteIfExists(MinOEEVar, results.MinOEE);
        WriteIfExists(MaxOEEVar, results.MaxOEE);
        WriteIfExists(AvgOEEVar, results.AvgOEE);

        // Target comparisons
        WriteIfExists(QualityVsTargetVar, results.QualityVsTarget);
        WriteIfExists(PerformanceVsTargetVar, results.PerformanceVsTarget);
        WriteIfExists(AvailabilityVsTargetVar, results.AvailabilityVsTarget);
        WriteIfExists(OEEVsTargetVar, results.OEEVsTarget);
    }

    private void WriteIfExists(IUAVariable var, object value)
    {
        if (!_outputPresenceFlags.ContainsKey(var) || !_outputPresenceFlags[var] || var == null) return;
        
        // Only write if value has changed (efficiency improvement)
        if (_lastWrittenValues.TryGetValue(var, out object lastValue))
        {
            if (ValuesAreEqual(lastValue, value)) return; // Skip write if unchanged
        }
        
        if (TrySetValueWithCooldown(var, value))
        {
            _lastWrittenValues[var] = value; // Cache the written value
        }
    }

    private bool ValuesAreEqual(object value1, object value2)
    {
        if (value1 == null && value2 == null) return true;
        if (value1 == null || value2 == null) return false;
        
        // Handle double precision comparison
        if (value1 is double d1 && value2 is double d2)
        {
            return Math.Abs(d1 - d2) < 0.001; // 0.1% precision for doubles
        }
        
        return value1.Equals(value2);
    }

    private bool TrySetValueWithCooldown(IUAVariable var, object value)
    {
        if (var == null) return false;
        if (!_outputPresenceFlags.ContainsKey(var)) return false;

        try
        {
            var.SetValue(value);
            _outputPresenceFlags[var] = true;
            return true;
        }
        catch (Exception ex)
        {
            LogError($"Write failed for '{GetVariableIdentifier(var)}': {ex.Message}");
            _outputPresenceFlags[var] = false;
            return false;
        }
    }

    // Helper methods
    private object GetUnderlyingValue(IUAVariable var)
    {
        if (var == null) return null;
        var v = var.Value;
        if (v == null) return null;
        return v is UAValue ua ? ua.Value : v;
    }

    private string GetVariableIdentifier(IUAVariable var)
    {
        if (var == null) return "null";
        try { return var.BrowseName?.ToString() ?? "iuavariable"; }
        catch { return "iuavariable"; }
    }

    private int ReadIntVar(IUAVariable var, int fallback)
    {
        var o = GetUnderlyingValue(var);
        if (o == null) return fallback;
        if (o is int i) return i;
        if (o is long l) return l > int.MaxValue ? int.MaxValue : Convert.ToInt32(l);
        if (int.TryParse(o.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)) return parsed;
        return fallback;
    }

    private double ReadDoubleVar(IUAVariable var, double fallback)
    {
        var o = GetUnderlyingValue(var);
        if (o == null) return fallback;
        if (o is double d) return d;
        if (o is float f) return Convert.ToDouble(f);
        if (o is int i) return Convert.ToDouble(i);
        if (o is long l) return Convert.ToDouble(l);
        if (double.TryParse(o.ToString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double parsed))
            return parsed;
        return fallback;
    }

    private bool ReadBoolVar(IUAVariable var, bool fallback)
    {
        var o = GetUnderlyingValue(var);
        if (o == null) return fallback;
        if (o is bool b) return b;
        if (o is int i) return i != 0;
        if (bool.TryParse(o.ToString(), out bool parsed))
            return parsed;
        return fallback;
    }

    private bool TryGetDoubleFromRaw(object raw, out double result)
    {
        result = 0.0;
        if (raw == null) return false;
        try
        {
            if (raw is double dv) { result = dv; return result > 0.0; }
            if (raw is float fv) { result = Convert.ToDouble(fv); return result > 0.0; }
            if (raw is int iv) { result = Convert.ToDouble(iv); return result > 0.0; }
            if (double.TryParse(raw.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedD))
            {
                result = parsedD;
                return result > 0.0;
            }
        }
        catch (Exception ex)
        {
            LogError($"Parse double error: {ex.Message}");
        }
        return false;
    }

    private void UpdateShiftInformation(DateTime now, CalculationResults results)
    {
        try
        {
            // Parse shift start time
            if (TimeSpan.TryParse(_shiftStartTime, out TimeSpan shiftStartTime))
            {
                var today = now.Date;
                _currentShiftStart = today.Add(shiftStartTime);
                _currentShiftEnd = _currentShiftStart.AddHours(_calculatedHoursPerShift);
                
                // Determine current shift number
                var hoursIntoDay = now.TimeOfDay.TotalHours;
                var shiftStartHours = shiftStartTime.TotalHours;
                
                if (_numberOfShifts > 0)
                {
                    var shiftDuration = 24.0 / _numberOfShifts;
                    var shiftIndex = (int)((hoursIntoDay - shiftStartHours + 24) % 24 / shiftDuration);
                    _currentShiftNumber = (shiftIndex % _numberOfShifts) + 1;
                    
                    // Adjust shift start/end for current shift
                    var currentShiftStartHours = shiftStartHours + (shiftIndex * shiftDuration);
                    if (currentShiftStartHours >= 24) currentShiftStartHours -= 24;
                    
                    _currentShiftStart = today.AddHours(currentShiftStartHours);
                    if (_currentShiftStart > now) _currentShiftStart = _currentShiftStart.AddDays(-1);
                    
                    _currentShiftEnd = _currentShiftStart.AddHours(shiftDuration);
                }
            }
            
            // Check for shift changes - check every calculation cycle for precise timing
            var timeToShiftEnd = (_currentShiftEnd - now).TotalSeconds;
            
            // ShiftChangeImminent: True 1 second before shift change (for data logging)
            _shiftChangeImminent = timeToShiftEnd <= 1.0 && timeToShiftEnd > 0.0;
            
            // ShiftChangeOccurred: True 0.5 seconds before shift change (for counter reset)
            _shiftChangeOccurred = timeToShiftEnd <= 0.5 && timeToShiftEnd > 0.0;
            
            // Update results
            results.CurrentShiftNumber = _currentShiftNumber;
            results.ShiftStartTimeOutput = _currentShiftStart.ToString("HH:mm:ss");
            results.ShiftEndTime = _currentShiftEnd.ToString("HH:mm:ss");
            results.TimeIntoShift = FormatTimeSpan(now - _currentShiftStart);
            results.TimeRemainingInShift = FormatTimeSpan(_currentShiftEnd - now);
            results.ShiftChangeOccurred = _shiftChangeOccurred;
            results.ShiftChangeImminent = _shiftChangeImminent;

            // Calculate Shift Progress %
            double totalShiftSeconds = (_currentShiftEnd - _currentShiftStart).TotalSeconds;
            double elapsedShiftSeconds = (now - _currentShiftStart).TotalSeconds;
            if (totalShiftSeconds > 0)
                results.ShiftProgress = Math.Max(0, Math.Min(100, (elapsedShiftSeconds / totalShiftSeconds) * 100.0));
            else
                results.ShiftProgress = 0;

        }
        catch (Exception ex)
        {
            LogError($"Error updating shift information: {ex.Message}");
            // Use defaults
            results.CurrentShiftNumber = 1;
            results.ShiftStartTimeOutput = "06:00:00";
            results.ShiftEndTime = "14:00:00";
            results.TimeIntoShift = "00:00:00";
            results.TimeRemainingInShift = "08:00:00";
            results.ShiftChangeOccurred = false;
            results.ShiftChangeImminent = false;
            results.ShiftProgress = 0;
        }
    }

    private string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalDays >= 1)
        {
            return $"{(int)timeSpan.TotalDays}d {timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }
        else if (timeSpan.TotalSeconds < 0)
        {
            return "00:00:00";
        }
        else
        {
            return $"{(int)timeSpan.TotalHours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }
    }

    private double CalculateDataQuality(CalculationResults results)
    {
        double score = 100.0;
        
        // Penalize if no parts produced
        if (results.TotalCount == 0) score -= 30;
        
        // Penalize if no runtime
        if (results.RuntimeSeconds <= 0) score -= 40;
        
        // Penalize if behind schedule
        if (results.ProductionBehindSchedule) score -= 15;
        
        // Penalize if OEE is very low
        if (results.OEE < _poorOEEThreshold) score -= 10;
        
        return Math.Max(0, Math.Min(100, score));
    }

    private void LogError(string message)
    {
        Log.Error("ObjectTypeOEE_Calculator", message);
    }

    private void LogInfo(string message)
    {
        if (_loggingVerbosity >= 1)
            Log.Info("ObjectTypeOEE_Calculator", message);
    }

    private void LogWarning(string message)
    {
        if (_loggingVerbosity >= 1)
            Log.Warning("ObjectTypeOEE_Calculator", message);
    }

    private class CalculationResults
    {
        public int TotalCount { get; set; }
        public int GoodPartCount { get; set; }
        public int BadPartCount { get; set; }
        public double RuntimeSeconds { get; set; }
        public double Quality { get; set; }
        public double Performance { get; set; }
        public double Availability { get; set; }
        public double OEE { get; set; }
        public double AvgCycleTime { get; set; }
        public double PartsPerHour { get; set; }
        public double ExpectedPartCount { get; set; }
        public double HoursPerShift { get; set; }
        public string DowntimeFormatted { get; set; } = "00:00:00";
        public string TotalRuntimeFormatted { get; set; } = "00:00:00";
        public int CurrentShiftNumber { get; set; } = 1;
        public string ShiftStartTimeOutput { get; set; } = "06:00:00";
        public string ShiftEndTime { get; set; } = "14:00:00";
        public string TimeIntoShift { get; set; } = "00:00:00";
        public string TimeRemainingInShift { get; set; } = "08:00:00";
        public bool ShiftChangeOccurred { get; set; } = false;
        public bool ShiftChangeImminent { get; set; } = false;
        public int ProjectedTotalCount { get; set; }
        public string RemainingTimeAtCurrentRate { get; set; } = "00:00:00";
        public bool ProductionBehindSchedule { get; set; } = false;
        public double RequiredRateToTarget { get; set; }
        public int TargetVsActualParts { get; set; }
        public double ShiftProgress { get; set; } // 0-100
        public double ProductionProgress { get; set; } // 0-100
        public string SystemStatus { get; set; } = "Starting";
        public bool CalculationValid { get; set; } = true;
        public double DataQualityScore { get; set; } = 100.0;
        public string LastUpdateTime { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // Trending
        public string QualityTrend { get; set; } = "Insufficient Data";
        public string PerformanceTrend { get; set; } = "Insufficient Data";
        public string AvailabilityTrend { get; set; } = "Insufficient Data";
        public string OEETrend { get; set; } = "Insufficient Data";

        // Statistics
        public double MinQuality { get; set; }
        public double MaxQuality { get; set; }
        public double AvgQuality { get; set; }
        public double MinPerformance { get; set; }
        public double MaxPerformance { get; set; }
        public double AvgPerformance { get; set; }
        public double MinAvailability { get; set; }
        public double MaxAvailability { get; set; }
        public double AvgAvailability { get; set; }
        public double MinOEE { get; set; }
        public double MaxOEE { get; set; }
        public double AvgOEE { get; set; }

        // Target comparisons
        public double QualityVsTarget { get; set; }
        public double PerformanceVsTarget { get; set; }
        public double AvailabilityVsTarget { get; set; }
        public double OEEVsTarget { get; set; }
    }
}
