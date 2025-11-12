using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq; // Added this for LINQ extension methods
using System.Threading;
using System.Threading.Tasks;
using UAManagedCore;
using FTOptix.NetLogic;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.DataLogger;

/// <summary>
/// ManufacturingOEE_Calculator (improved and renamed from MachineOEE_Calculator)
/// - Purpose:
///     * Compute standard OEE metrics (Quality, Performance, Availability, OEE) and helpful derived values:
///       AvgCycleTime, PartsPerHour, ExpectedPartCount, formatted runtime/downtime strings.
///     * Advanced features: shift tracking, trending analysis, production planning, system health monitoring.
/// - Goals & improvements over the prior implementation:
///     * Robust, defensive parsing (numeric, TimeSpan, DateTime, and "HH:mm[:ss]" strings) using InvariantCulture.
///     * Cache parsed configuration values so static inputs are not reparsed every loop (reduces CPU & allocations).
///     * Graceful write handling with a retry cooldown (avoids permanent disabling on transient failures).
///     * Use long for internal counters and safe clamping when writing to Int32 outputs to avoid overflow.
///     * Configurable update rate (optional UpdateRateMs node), default 1s.
///     * Trending analysis with 60-measurement rolling window.
///     * Production planning and shift management features.
///     * System health monitoring and data quality scoring.
/// </summary>
public class ManufacturingOEE_Calculator : BaseNetLogic
{
    // Inputs
    private IUAVariable TotalRuntimeSecondsVar;
    private IUAVariable GoodPartCountVar;
    private IUAVariable BadPartCountVar;
    private IUAVariable IdealCycleTimeSecondsVar;
    private IUAVariable PlannedProductionTimeHoursVar;
    private IUAVariable HoursPerShiftVar;
    private IUAVariable NumberOfShiftsVar;
    private IUAVariable UpdateRateMsVar;
    private IUAVariable ShiftStartTimeVar;
    private IUAVariable ProductionTargetVar;
    private IUAVariable QualityTargetVar;
    private IUAVariable PerformanceTargetVar;
    private IUAVariable AvailabilityTargetVar;
    private IUAVariable OEETargetVar;
    private IUAVariable LoggingVerbosityVar;

    // Outputs - Core OEE
    private IUAVariable TotalCountVar;
    private IUAVariable QualityVar;
    private IUAVariable PerformanceVar;
    private IUAVariable AvailabilityVar;
    private IUAVariable OEEVar;
    private IUAVariable AvgCycleTimeVar;
    private IUAVariable PartsPerHourVar;
    private IUAVariable ExpectedPartCountVar;
    private IUAVariable DowntimeFormattedVar;
    private IUAVariable TotalRuntimeFormattedVar;

    // Outputs - Shift & Time Tracking
    private IUAVariable CurrentShiftNumberVar;
    private IUAVariable ShiftStartTimeOutputVar;
    private IUAVariable ShiftEndTimeVar;
    private IUAVariable TimeIntoShiftVar;
    private IUAVariable TimeRemainingInShiftVar;
    private IUAVariable ShiftChangeOccurredVar;
    private IUAVariable ShiftChangeImminent;

    // Outputs - Production Planning
    private IUAVariable ProjectedTotalCountVar;
    private IUAVariable RemainingTimeAtCurrentRateVar;
    private IUAVariable ProductionBehindScheduleVar;
    private IUAVariable RequiredRateToTargetVar;
    private IUAVariable TargetVsActualPartsVar;

    // Outputs - System Health
    private IUAVariable LastUpdateTimeVar;
    private IUAVariable SystemStatusVar;
    private IUAVariable CalculationValidVar;
    private IUAVariable DataQualityScoreVar;

    // Outputs - Trending
    private IUAVariable QualityTrendVar;
    private IUAVariable PerformanceTrendVar;
    private IUAVariable AvailabilityTrendVar;
    private IUAVariable OEETrendVar;

    // Outputs - Statistics
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

    // Outputs - Target Comparisons
    private IUAVariable QualityVsTargetVar;
    private IUAVariable PerformanceVsTargetVar;
    private IUAVariable AvailabilityVsTargetVar;
    private IUAVariable OEEVsTargetVar;

    // runtime
    private CancellationTokenSource _cts;
    private Task _loopTask;

    // presence flags cached once at Start() and updated if writes fail
    private readonly Dictionary<IUAVariable, bool> _outputPresenceFlags = new Dictionary<IUAVariable, bool>();
    private bool _avgCycleVarIsString;

    // write retry/cooldown state: prevents permanent disabling on transient write failures
    private readonly TimeSpan WriteRetryCooldown = TimeSpan.FromSeconds(30);
    private readonly Dictionary<IUAVariable, DateTime> _lastWriteFailureUtc = new Dictionary<IUAVariable, DateTime>();

    // cached parsed config values to avoid re-parsing each loop
    private object _cachedIdealRaw = null;
    private double _cachedIdealSeconds = 0.0;
    private bool _cachedIdealValid = false;

    private object _cachedPlannedRaw = null;
    private double _cachedPlannedHours = double.NaN;
    private bool _cachedPlannedValid = false;

    private object _cachedShiftStartRaw = null;
    private TimeSpan _cachedShiftStart = TimeSpan.Zero;
    private bool _cachedShiftStartValid = false;

    // trending data - rolling window
    private readonly Queue<double> _qualityHistory = new Queue<double>();
    private readonly Queue<double> _performanceHistory = new Queue<double>();
    private readonly Queue<double> _availabilityHistory = new Queue<double>();
    private readonly Queue<double> _oeeHistory = new Queue<double>();
    private const int MaxHistorySize = 60; // 60 measurements for trending

    // shift change tracking
    private int _lastShiftNumber = -1;
    private readonly TimeSpan ShiftChangeWarningMinutes = TimeSpan.FromSeconds(1); // Warning 1 second before shift change

    // targets with defaults (world-class OEE standards)
    private double _qualityTarget = 95.0;
    private double _performanceTarget = 85.0;
    private double _availabilityTarget = 90.0;
    private double _oeeTarget = 72.7; // 95% × 85% × 90%

    // tunables
    private int _updateRateMs = 1000; // default 1s
    private int _loggingVerbosity = 1; // 0=minimal, 1=normal, 2=verbose, 3=debug

    // Initialization flag to prevent re-setting defaults
    private bool _defaultsInitialized = false;

    // Runtime monitoring for detecting stale data
    private double _lastRuntimeSeconds = -1.0; // Track last runtime value
    private DateTime _lastRuntimeUpdateUtc = DateTime.UtcNow; // When runtime last changed
    private readonly TimeSpan _runtimeStaleThreshold = TimeSpan.FromMinutes(2); // Consider stale after 2 minutes
    private readonly TimeSpan _runtimeIdleThreshold = TimeSpan.FromMinutes(5); // Consider idle after 5 minutes

    public override void Start()
    {
        InitializeInputVariables();
        InitializeOutputVariables();
        CachePresenceFlags();
        InitializeDefaultInputValues();
        ReadConfigurationOnce();

        _cts = new CancellationTokenSource();
        _loopTask = Task.Run(() => RunLoopAsync(_cts.Token));

        LogInfo("ManufacturingOEE_Calculator started successfully");
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
            LogInfo("ManufacturingOEE_Calculator stopped");
        }
    }

    private void InitializeInputVariables()
    {
        // Core inputs
        TotalRuntimeSecondsVar = LogicObject.GetVariable("TotalRuntimeSeconds");
        GoodPartCountVar = LogicObject.GetVariable("GoodPartCount");
        BadPartCountVar = LogicObject.GetVariable("BadPartCount");
        IdealCycleTimeSecondsVar = LogicObject.GetVariable("IdealCycleTimeSeconds");
        PlannedProductionTimeHoursVar = LogicObject.GetVariable("PlannedProductionTimeHours");
        HoursPerShiftVar = LogicObject.GetVariable("HoursPerShift");
        NumberOfShiftsVar = LogicObject.GetVariable("NumberOfShifts");

        // Configuration
        UpdateRateMsVar = LogicObject.GetVariable("UpdateRateMs");
        ShiftStartTimeVar = LogicObject.GetVariable("ShiftStartTime");
        ProductionTargetVar = LogicObject.GetVariable("ProductionTarget");
        QualityTargetVar = LogicObject.GetVariable("QualityTarget");
        PerformanceTargetVar = LogicObject.GetVariable("PerformanceTarget");
        AvailabilityTargetVar = LogicObject.GetVariable("AvailabilityTarget");
        OEETargetVar = LogicObject.GetVariable("OEETarget");
        LoggingVerbosityVar = LogicObject.GetVariable("LoggingVerbosity");
    }

    private void InitializeOutputVariables()
    {
        // Core OEE outputs
        TotalCountVar = LogicObject.GetVariable("TotalCount");
        QualityVar = LogicObject.GetVariable("Quality");
        PerformanceVar = LogicObject.GetVariable("Performance");
        AvailabilityVar = LogicObject.GetVariable("Availability");
        OEEVar = LogicObject.GetVariable("OEE");
        AvgCycleTimeVar = LogicObject.GetVariable("AvgCycleTime");
        PartsPerHourVar = LogicObject.GetVariable("PartsPerHour");
        ExpectedPartCountVar = LogicObject.GetVariable("ExpectedPartCount");
        DowntimeFormattedVar = LogicObject.GetVariable("DowntimeFormatted");
        TotalRuntimeFormattedVar = LogicObject.GetVariable("TotalRuntimeFormatted");

        // Shift tracking outputs
        CurrentShiftNumberVar = LogicObject.GetVariable("CurrentShiftNumber");
        ShiftStartTimeOutputVar = LogicObject.GetVariable("ShiftStartTimeOutput");
        ShiftEndTimeVar = LogicObject.GetVariable("ShiftEndTime");
        TimeIntoShiftVar = LogicObject.GetVariable("TimeIntoShift");
        TimeRemainingInShiftVar = LogicObject.GetVariable("TimeRemainingInShift");
        ShiftChangeOccurredVar = LogicObject.GetVariable("ShiftChangeOccurred");
        ShiftChangeImminent = LogicObject.GetVariable("ShiftChangeImminent");

        // Production planning outputs
        ProjectedTotalCountVar = LogicObject.GetVariable("ProjectedTotalCount");
        RemainingTimeAtCurrentRateVar = LogicObject.GetVariable("RemainingTimeAtCurrentRate");
        ProductionBehindScheduleVar = LogicObject.GetVariable("ProductionBehindSchedule");
        RequiredRateToTargetVar = LogicObject.GetVariable("RequiredRateToTarget");
        TargetVsActualPartsVar = LogicObject.GetVariable("TargetVsActualParts");

        // System health outputs
        LastUpdateTimeVar = LogicObject.GetVariable("LastUpdateTime");
        SystemStatusVar = LogicObject.GetVariable("SystemStatus");
        CalculationValidVar = LogicObject.GetVariable("CalculationValid");
        DataQualityScoreVar = LogicObject.GetVariable("DataQualityScore");

        // Trending outputs
        QualityTrendVar = LogicObject.GetVariable("QualityTrend");
        PerformanceTrendVar = LogicObject.GetVariable("PerformanceTrend");
        AvailabilityTrendVar = LogicObject.GetVariable("AvailabilityTrend");
        OEETrendVar = LogicObject.GetVariable("OEETrend");

        // Statistics outputs
        MinQualityVar = LogicObject.GetVariable("MinQuality");
        MaxQualityVar = LogicObject.GetVariable("MaxQuality");
        AvgQualityVar = LogicObject.GetVariable("AvgQuality");
        MinPerformanceVar = LogicObject.GetVariable("MinPerformance");
        MaxPerformanceVar = LogicObject.GetVariable("MaxPerformance");
        AvgPerformanceVar = LogicObject.GetVariable("AvgPerformance");
        MinAvailabilityVar = LogicObject.GetVariable("MinAvailability");
        MaxAvailabilityVar = LogicObject.GetVariable("MaxAvailability");
        AvgAvailabilityVar = LogicObject.GetVariable("AvgAvailability");
        MinOEEVar = LogicObject.GetVariable("MinOEE");
        MaxOEEVar = LogicObject.GetVariable("MaxOEE");
        AvgOEEVar = LogicObject.GetVariable("AvgOEE");

        // Target comparison outputs
        QualityVsTargetVar = LogicObject.GetVariable("QualityVsTarget");
        PerformanceVsTargetVar = LogicObject.GetVariable("PerformanceVsTarget");
        AvailabilityVsTargetVar = LogicObject.GetVariable("AvailabilityVsTarget");
        OEEVsTargetVar = LogicObject.GetVariable("OEEVsTarget");
    }

    private void InitializeDefaultInputValues()
    {
        if (_defaultsInitialized)
        {
            return;
        }
        
        // Core production inputs - use SetDefaultIfNeeded for values that can be zero
        SetDefaultValue(TotalRuntimeSecondsVar, 0.0, "TotalRuntimeSeconds");
        SetDefaultValue(GoodPartCountVar, 0, "GoodPartCount");
        SetDefaultValue(BadPartCountVar, 0, "BadPartCount");
        SetDefaultValue(IdealCycleTimeSecondsVar, 30.0, "IdealCycleTimeSeconds"); // 30 seconds per part default
        
        // Configuration inputs - set defaults for all
        SetDefaultValue(PlannedProductionTimeHoursVar, 8.0, "PlannedProductionTimeHours");
        SetDefaultValue(HoursPerShiftVar, 8.0, "HoursPerShift");
        SetDefaultValue(NumberOfShiftsVar, 3, "NumberOfShifts");
        SetDefaultValue(ShiftStartTimeVar, "06:00:00", "ShiftStartTime");
        SetDefaultValue(ProductionTargetVar, 480, "ProductionTarget");
        
        // Target values - world-class OEE standards
        SetDefaultValue(QualityTargetVar, 95.0, "QualityTarget");
        SetDefaultValue(PerformanceTargetVar, 85.0, "PerformanceTarget");
        SetDefaultValue(AvailabilityTargetVar, 90.0, "AvailabilityTarget");
        SetDefaultValue(OEETargetVar, 72.7, "OEETarget");
        
        // System configuration
        SetDefaultValue(UpdateRateMsVar, 1000, "UpdateRateMs");
        SetDefaultValue(LoggingVerbosityVar, 1, "LoggingVerbosity"); // Set to 1 for minimal logging
        
        _defaultsInitialized = true;
    }
    
    private void SetDefaultValue(IUAVariable var, object defaultValue, string varName)
    {
        if (var == null)
        {
            return;
        }
        
        try
        {
            var currentValue = var.Value?.Value;
            bool needsDefault = IsValueInvalid(currentValue, defaultValue);
            
            if (needsDefault)
            {
                var.SetValue(defaultValue);
                
                // Invalidate related caches
                InvalidateCache(varName);
            }
        }
        catch (Exception ex)
        {
            LogError($"Failed to set default for '{varName}': {ex.Message}");
        }
    }
    
    private bool IsValueInvalid(object value, object defaultValue)
    {
        if (value == null) return true;
        
        // Check for invalid numeric values
        if (defaultValue is double)
        {
            if (value is double d && (double.IsNaN(d) || double.IsInfinity(d))) return true;
            // For IdealCycleTimeSeconds, zero is invalid (division by zero)
            if (value is double d2 && d2 <= 0 && defaultValue.ToString().Contains("30")) return true;
        }
        
        // Check for invalid string values
        if (defaultValue is string && string.IsNullOrWhiteSpace(value?.ToString())) return true;
        
        // Check for invalid integer values
        if (defaultValue is int && value is int i && i < 0) return true;
        
        return false;
    }
    
    private void InvalidateCache(string varName)
    {
        switch (varName)
        {
            case "IdealCycleTimeSeconds":
                _cachedIdealRaw = null;
                _cachedIdealValid = false;
                break;
            case "PlannedProductionTimeHours":
            case "HoursPerShift":
            case "NumberOfShifts":
                _cachedPlannedRaw = null;
                _cachedPlannedValid = false;
                break;
            case "ShiftStartTime":
                _cachedShiftStartRaw = null;
                _cachedShiftStartValid = false;
                break;
        }
    }

    private void CachePresenceFlags()
    {
        var allOutputVars = new IUAVariable[]
        {
            TotalCountVar, QualityVar, PerformanceVar, AvailabilityVar, OEEVar,
            AvgCycleTimeVar, PartsPerHourVar, ExpectedPartCountVar, DowntimeFormattedVar, TotalRuntimeFormattedVar,
            CurrentShiftNumberVar, ShiftStartTimeOutputVar, ShiftEndTimeVar, TimeIntoShiftVar, TimeRemainingInShiftVar,
            ShiftChangeOccurredVar, ShiftChangeImminent,
            ProjectedTotalCountVar, RemainingTimeAtCurrentRateVar, ProductionBehindScheduleVar, RequiredRateToTargetVar, TargetVsActualPartsVar,
            LastUpdateTimeVar, SystemStatusVar, CalculationValidVar, DataQualityScoreVar,
            QualityTrendVar, PerformanceTrendVar, AvailabilityTrendVar, OEETrendVar,
            MinQualityVar, MaxQualityVar, AvgQualityVar, MinPerformanceVar, MaxPerformanceVar, AvgPerformanceVar,
            MinAvailabilityVar, MaxAvailabilityVar, AvgAvailabilityVar, MinOEEVar, MaxOEEVar, AvgOEEVar,
            QualityVsTargetVar, PerformanceVsTargetVar, AvailabilityVsTargetVar, OEEVsTargetVar
        };

        foreach (var var in allOutputVars)
        {
            if (var != null)
            {
                _outputPresenceFlags[var] = true;
            }
        }

        // Detect whether AvgCycleTime node is string-typed
        _avgCycleVarIsString = false;
        if (AvgCycleTimeVar != null)
        {
            var underlying = GetUnderlyingValue(AvgCycleTimeVar);
            _avgCycleVarIsString = underlying is string;
        }
    }

    private void ReadConfigurationOnce()
    {
        // Update rate
        if (ReadIntVar(UpdateRateMsVar, -1) is int u && u > 0)
            _updateRateMs = u;

        // Read targets fresh each time to ensure user changes are picked up
        _qualityTarget = ReadDoubleVar(QualityTargetVar, 95.0);
        _performanceTarget = ReadDoubleVar(PerformanceTargetVar, 85.0);
        _availabilityTarget = ReadDoubleVar(AvailabilityTargetVar, 90.0);
        _oeeTarget = ReadDoubleVar(OEETargetVar, 72.7);

        // Logging verbosity
        _loggingVerbosity = ReadIntVar(LoggingVerbosityVar, 1);
    }

    private void ValidateAndFixInputs()
    {
        // Only fix truly invalid values, not user-configured zeros
        bool anyFixed = false;
        
        // Fix negative runtime (negative values are definitely invalid)
        if (ReadDoubleVar(TotalRuntimeSecondsVar, -1) < 0)
        {
            if (TotalRuntimeSecondsVar != null) TotalRuntimeSecondsVar.SetValue(0.0);
            anyFixed = true;
        }
        
        // Fix negative part counts (negative counts are definitely invalid)
        if (ReadIntVar(GoodPartCountVar, -1) < 0)
        {
            if (GoodPartCountVar != null) GoodPartCountVar.SetValue(0);
            anyFixed = true;
        }
        
        if (ReadIntVar(BadPartCountVar, -1) < 0)
        {
            if (BadPartCountVar != null) BadPartCountVar.SetValue(0);
            anyFixed = true;
        }
        
        // Only fix IdealCycleTime if it's truly invalid (NaN, Infinity, or null)
        // Don't override user-configured values
        var idealRaw = GetUnderlyingValue(IdealCycleTimeSecondsVar);
        if (idealRaw == null || (idealRaw is double dVal && (double.IsNaN(dVal) || double.IsInfinity(dVal))))
        {
            if (IdealCycleTimeSecondsVar != null) IdealCycleTimeSecondsVar.SetValue(1.0);
            _cachedIdealRaw = null; // Invalidate cache
            anyFixed = true;
        }
        
        if (anyFixed)
        {
            // Force cache invalidation after runtime fixes
            _cachedIdealRaw = null;
            _cachedIdealValid = false;
            _cachedPlannedRaw = null;
            _cachedPlannedValid = false;
        }
    }

    private void CheckRuntimeActivity(double currentRuntimeSeconds)
    {
        DateTime now = DateTime.UtcNow;
        
        // Check if runtime value has changed (indicating machine is running)
        if (Math.Abs(currentRuntimeSeconds - _lastRuntimeSeconds) > 0.1) // 0.1 second threshold for change detection
        {
            // Runtime has updated - machine is running
            _lastRuntimeUpdateUtc = now;
            _lastRuntimeSeconds = currentRuntimeSeconds;
        }
        else if (_lastRuntimeSeconds < 0) // First time initialization
        {
            _lastRuntimeSeconds = currentRuntimeSeconds;
            _lastRuntimeUpdateUtc = now;
        }
    }

    private bool IsRuntimeStale()
    {
        TimeSpan timeSinceUpdate = DateTime.UtcNow - _lastRuntimeUpdateUtc;
        return timeSinceUpdate > TimeSpan.FromSeconds(30); // Machine stopped if runtime hasn't changed for 30 seconds
    }

    private bool IsRuntimeIdle()
    {
        TimeSpan timeSinceUpdate = DateTime.UtcNow - _lastRuntimeUpdateUtc;
        return timeSinceUpdate > TimeSpan.FromMinutes(5); // Machine idle if runtime hasn't changed for 5 minutes
    }

    private TimeSpan GetTimeSinceLastRuntimeUpdate()
    {
        return DateTime.UtcNow - _lastRuntimeUpdateUtc;
    }

    private async Task RunLoopAsync(CancellationToken token)
    {
        int loopCount = 0;
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_updateRateMs, token);

                // Refresh configuration every 30 seconds to pick up target changes
                if (loopCount % 30 == 0)
                {
                    ReadConfigurationOnce();
                }

                // Validate inputs only every 10 loops (reduce CPU overhead)
                if (loopCount % 10 == 0)
                {
                    ValidateAndFixInputs();
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

        // Read inputs
        double runtimeSeconds = ReadDoubleVar(TotalRuntimeSecondsVar, 0.0);
        int good = ReadIntVar(GoodPartCountVar, 0);
        int bad = ReadIntVar(BadPartCountVar, 0);
        int totalCount = good + bad;

        results.TotalCount = totalCount;
        results.GoodPartCount = good;
        results.BadPartCount = bad;
        results.RuntimeSeconds = runtimeSeconds;

        // Check if runtime has been updating (machine activity detection)
        CheckRuntimeActivity(runtimeSeconds);

        // Calculate data quality score
        results.DataQualityScore = CalculateDataQualityScore();
        results.CalculationValid = results.DataQualityScore >= 75.0;

        // Core OEE calculations
        results.Quality = totalCount == 0 ? 0.0 : ((double)good / totalCount) * 100.0;

        // Get ideal cycle time (cached)
        double idealCycle = GetIdealCycleTimeSeconds();
        
        // Performance calculation
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
            // Performance = (Ideal Cycle Time × Total Count) / Run Time × 100
            double idealTime = idealCycle * totalCount;
            double performanceRatio = idealTime / runtimeSeconds;
            double rawPerformance = performanceRatio * 100.0;
            results.Performance = Math.Min(999.9, rawPerformance); // Cap at 999.9%
            
            if (rawPerformance > 999.9)
            {
                LogInfo($"Performance capped at 999.9% (calculated: {rawPerformance:F1}%) - Check IdealCycleTime configuration");
            }
        }

        // Get planned production time (cached)
        double plannedSeconds = GetPlannedProductionSeconds();
        
        // Availability calculation
        if (!double.IsNaN(plannedSeconds) && plannedSeconds > 0.0)
        {
            results.Availability = Math.Max(0.0, Math.Min(100.0, (runtimeSeconds / plannedSeconds) * 100.0));
            results.PlannedProductionSeconds = plannedSeconds;
            results.DowntimeSeconds = Math.Max(0.0, plannedSeconds - runtimeSeconds);
        }
        else
        {
            results.Availability = 100.0; // Default if no planned time available
            results.PlannedProductionSeconds = double.NaN;
            results.DowntimeSeconds = 0.0;
        }

        // OEE calculation
        results.OEE = (results.Quality * results.Performance * results.Availability) / 10000.0;

        // Derived metrics
        results.AvgCycleTimeSeconds = totalCount == 0 ? 0.0 : runtimeSeconds / totalCount;
        results.PartsPerHour = runtimeSeconds > 0.0 ? (totalCount / runtimeSeconds) * 3600.0 : 0.0;
        
        // ExpectedPartCount calculation with proper fallbacks
        if (plannedSeconds > 0.0 && idealCycle > 0.0)
            results.ExpectedPartCount = plannedSeconds / idealCycle;
        else
            results.ExpectedPartCount = 480.0; // Fallback: 480 parts for 8-hour shift (1 part per minute)

        // Shift calculations
        CalculateShiftInfo(results);

        // Check for shift change events
        CheckShiftChangeEvents(results);

        // Production planning
        CalculateProductionPlanning(results);

        // Target comparisons
        results.QualityVsTarget = results.Quality - _qualityTarget;
        results.PerformanceVsTarget = results.Performance - _performanceTarget;
        results.AvailabilityVsTarget = results.Availability - _availabilityTarget;
        results.OEEVsTarget = results.OEE - _oeeTarget;

        // System status
        results.SystemStatus = DetermineSystemStatus(results);
        results.LastUpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        return results;
    }

    private double GetIdealCycleTimeSeconds()
    {
        var idealRaw = GetUnderlyingValue(IdealCycleTimeSecondsVar);
        if (!object.Equals(idealRaw, _cachedIdealRaw))
        {
            if (TryGetSecondsFromRaw(idealRaw, out double sec))
            {
                _cachedIdealSeconds = sec;
                _cachedIdealValid = sec > 0.0;
            }
            else
            {
                _cachedIdealSeconds = 1.0; // Default fallback - 1 second per part (high-speed production)
                _cachedIdealValid = true;
            }
            _cachedIdealRaw = idealRaw;
        }
        double result = _cachedIdealValid ? _cachedIdealSeconds : 1.0; // Fallback if still invalid
        return result;
    }

    private double GetPlannedProductionSeconds()
    {
        var plannedRaw = GetUnderlyingValue(PlannedProductionTimeHoursVar);
        if (!object.Equals(plannedRaw, _cachedPlannedRaw))
        {
            if (TryGetHoursFromRaw(plannedRaw, out double hours))
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
            double result = _cachedPlannedHours * 3600.0;
            return result;
        }

        // Fallbacks
        double hoursPerShift = ReadDoubleVar(HoursPerShiftVar, double.NaN);
        if (!double.IsNaN(hoursPerShift) && hoursPerShift > 0.0)
        {
            double result = hoursPerShift * 3600.0;
            return result;
        }

        int numberOfShifts = ReadIntVar(NumberOfShiftsVar, -1);
        if (numberOfShifts > 0)
        {
            double computedHours = 24.0 / numberOfShifts;
            if (computedHours > 0.0)
            {
                double result = computedHours * 3600.0;
                return result;
            }
        }

        return 28800.0; // Default: 8 hours in seconds
    }

    private TimeSpan GetShiftStartTime()
    {
        var shiftRaw = GetUnderlyingValue(ShiftStartTimeVar);
        if (!object.Equals(shiftRaw, _cachedShiftStartRaw))
        {
            if (TryParseTimeSpan(shiftRaw, out TimeSpan ts))
            {
                _cachedShiftStart = ts;
                _cachedShiftStartValid = true;
            }
            else
            {
                _cachedShiftStart = TimeSpan.Zero;
                _cachedShiftStartValid = false;
            }
            _cachedShiftStartRaw = shiftRaw;
        }
        return _cachedShiftStartValid ? _cachedShiftStart : TimeSpan.FromHours(6); // Default 6 AM
    }

    private void CalculateShiftInfo(CalculationResults results)
    {
        TimeSpan shiftStart = GetShiftStartTime();
        double plannedSeconds = results.PlannedProductionSeconds;
        
        if (double.IsNaN(plannedSeconds) || plannedSeconds <= 0.0)
        {
            results.CurrentShiftNumber = 1;
            results.ShiftStartTime = shiftStart.ToString(@"hh\:mm\:ss") ?? "06:00:00";
            results.ShiftEndTime = shiftStart.Add(TimeSpan.FromHours(8)).ToString(@"hh\:mm\:ss") ?? "14:00:00";
            results.TimeIntoShift = "00:00:00";
            results.TimeRemainingInShift = "08:00:00";
            return;
        }

        DateTime now = DateTime.Now;
        TimeSpan currentTime = now.TimeOfDay;
        TimeSpan shiftDuration = TimeSpan.FromSeconds(plannedSeconds);

        // Calculate current shift
        TimeSpan shiftEnd = shiftStart.Add(shiftDuration);
        
        // Handle shifts that cross midnight
        if (shiftEnd >= TimeSpan.FromDays(1))
        {
            if (currentTime >= shiftStart || currentTime < shiftEnd.Subtract(TimeSpan.FromDays(1)))
            {
                results.CurrentShiftNumber = 1;
                results.ShiftStartTime = shiftStart.ToString(@"hh\:mm\:ss") ?? "06:00:00";
                results.ShiftEndTime = shiftEnd.Subtract(TimeSpan.FromDays(1)).ToString(@"hh\:mm\:ss") ?? "14:00:00";
                
                if (currentTime >= shiftStart)
                    results.TimeIntoShift = (currentTime - shiftStart).ToString(@"hh\:mm\:ss") ?? "00:00:00";
                else
                    results.TimeIntoShift = (currentTime + TimeSpan.FromDays(1) - shiftStart).ToString(@"hh\:mm\:ss") ?? "00:00:00";
                    
                results.TimeRemainingInShift = "08:00:00"; // Default remaining time
            }
            else
            {
                results.CurrentShiftNumber = 2;
                TimeSpan shift2Start = shiftEnd.Subtract(TimeSpan.FromDays(1));
                results.ShiftStartTime = shift2Start.ToString(@"hh\:mm\:ss") ?? "14:00:00";
                results.ShiftEndTime = shift2Start.Add(shiftDuration).ToString(@"hh\:mm\:ss") ?? "22:00:00";
                results.TimeIntoShift = (currentTime - shift2Start).ToString(@"hh\:mm\:ss") ?? "00:00:00";
                results.TimeRemainingInShift = "08:00:00"; // Default remaining time
            }
        }
        else
        {
            if (currentTime >= shiftStart && currentTime < shiftEnd)
            {
                results.CurrentShiftNumber = 1;
                results.ShiftStartTime = shiftStart.ToString(@"hh\:mm\:ss") ?? "06:00:00";
                results.ShiftEndTime = shiftEnd.ToString(@"hh\:mm\:ss") ?? "14:00:00";
                results.TimeIntoShift = (currentTime - shiftStart).ToString(@"hh\:mm\:ss") ?? "00:00:00";
                results.TimeRemainingInShift = (shiftEnd - currentTime).ToString(@"hh\:mm\:ss") ?? "08:00:00";
            }
            else
            {
                // Calculate which shift we're in for multi-shift operations
                double totalDayHours = 24.0;
                double shiftHours = plannedSeconds / 3600.0;
                int shiftsPerDay = (int)(totalDayHours / shiftHours);
                
                for (int i = 0; i < shiftsPerDay; i++)
                {
                    TimeSpan thisShiftStart = shiftStart.Add(TimeSpan.FromHours(i * shiftHours));
                    TimeSpan thisShiftEnd = thisShiftStart.Add(shiftDuration);
                    
                    if (currentTime >= thisShiftStart && currentTime < thisShiftEnd)
                    {
                        results.CurrentShiftNumber = i + 1;
                        results.ShiftStartTime = thisShiftStart.ToString(@"hh\:mm\:ss") ?? "06:00:00";
                        results.ShiftEndTime = thisShiftEnd.ToString(@"hh\:mm\:ss") ?? "14:00:00";
                        results.TimeIntoShift = (currentTime - thisShiftStart).ToString(@"hh\:mm\:ss") ?? "00:00:00";
                        results.TimeRemainingInShift = (thisShiftEnd - currentTime).ToString(@"hh\:mm\:ss") ?? "08:00:00";
                        break;
                    }
                }
                
                // If no shift matched, set defaults
                if (string.IsNullOrEmpty(results.ShiftStartTime))
                {
                    results.CurrentShiftNumber = 1;
                    results.ShiftStartTime = "06:00:00";
                    results.ShiftEndTime = "14:00:00";
                    results.TimeIntoShift = "00:00:00";
                    results.TimeRemainingInShift = "08:00:00";
                }
            }
        }
    }

    private void CheckShiftChangeEvents(CalculationResults results)
    {
        // Check for shift change occurrence
        bool shiftChangeOccurred = false;
        if (_lastShiftNumber != -1 && _lastShiftNumber != results.CurrentShiftNumber)
        {
            shiftChangeOccurred = true;
        }
        _lastShiftNumber = results.CurrentShiftNumber;
        results.ShiftChangeOccurred = shiftChangeOccurred;

        // Check if shift change is imminent (within warning period)
        bool shiftChangeImminent = false;
        if (!string.IsNullOrEmpty(results.TimeRemainingInShift) && 
            TimeSpan.TryParse(results.TimeRemainingInShift, out TimeSpan timeRemaining))
        {
            shiftChangeImminent = timeRemaining <= ShiftChangeWarningMinutes && timeRemaining > TimeSpan.Zero;
        }
        results.ShiftChangeImminent = shiftChangeImminent;
    }

    private void CalculateProductionPlanning(CalculationResults results)
    {
        int target = ReadIntVar(ProductionTargetVar, -1);
        
        if (target <= 0)
        {
            results.ProductionTarget = 0;
            results.ProductionBehindSchedule = false;
            results.ProjectedTotalCount = 0.0;
            results.RemainingTimeAtCurrentRate = "N/A";
            results.RequiredRateToTarget = 0.0;
            results.TargetVsActualParts = 0;
            return;
        }

        results.ProductionTarget = target;
        results.TargetVsActualParts = results.TotalCount - target;

        // Projected total at current rate
        if (!double.IsNaN(results.PlannedProductionSeconds) && results.PlannedProductionSeconds > 0.0 && results.RuntimeSeconds > 0.0)
        {
            double currentRate = results.TotalCount / results.RuntimeSeconds; // parts per second
            results.ProjectedTotalCount = currentRate * results.PlannedProductionSeconds;
        }
        else
        {
            results.ProjectedTotalCount = results.TotalCount;
        }

        // Production status
        if (!double.IsNaN(results.PlannedProductionSeconds) && results.PlannedProductionSeconds > 0.0)
        {
            double expectedProgress = results.RuntimeSeconds / results.PlannedProductionSeconds;
            double actualProgress = (double)results.TotalCount / target;
            results.ProductionBehindSchedule = actualProgress < expectedProgress * 0.95; // 5% tolerance
        }

        // Time remaining at current rate
        if (results.TotalCount >= target)
        {
            results.RemainingTimeAtCurrentRate = "Target Reached";
        }
        else if (results.PartsPerHour > 0.0)
        {
            double remainingParts = target - results.TotalCount;
            double remainingHours = remainingParts / results.PartsPerHour;
            results.RemainingTimeAtCurrentRate = FormatSeconds(remainingHours * 3600.0);
        }
        else
        {
            results.RemainingTimeAtCurrentRate = "N/A";
        }

        // Required rate to target
        if (!double.IsNaN(results.PlannedProductionSeconds) && results.PlannedProductionSeconds > 0.0)
        {
            double remainingSeconds = results.PlannedProductionSeconds - results.RuntimeSeconds;
            if (remainingSeconds > 0.0 && results.TotalCount < target)
            {
                double remainingParts = target - results.TotalCount;
                results.RequiredRateToTarget = (remainingParts / remainingSeconds) * 3600.0; // parts per hour
            }
            else
            {
                results.RequiredRateToTarget = 0.0;
            }
        }
    }

    private double CalculateDataQualityScore()
    {
        double score = 100.0;
        
        // Critical inputs (high penalty)
        if (TotalRuntimeSecondsVar == null || ReadDoubleVar(TotalRuntimeSecondsVar, -1) < 0) score -= 25.0;
        if (GoodPartCountVar == null || ReadIntVar(GoodPartCountVar, -1) < 0) score -= 25.0;
        if (IdealCycleTimeSecondsVar == null || GetIdealCycleTimeSeconds() <= 0.0) score -= 25.0;
        
        // Important inputs (medium penalty)
        if (PlannedProductionTimeHoursVar == null || double.IsNaN(GetPlannedProductionSeconds())) score -= 15.0;
        if (BadPartCountVar == null) score -= 5.0;
        
        // Optional inputs (small penalty)
        if (ShiftStartTimeVar == null) score -= 2.5;
        if (ProductionTargetVar == null) score -= 2.5;
        
        // No penalties for runtime activity - just let the SystemStatus handle machine running/stopped
        
        return Math.Max(0.0, score);
    }

    private string DetermineSystemStatus(CalculationResults results)
    {
        // Check for critical errors first
        if (results.DataQualityScore < 50.0) return "Error";
        if (!results.CalculationValid) return "Error";
        
        // Simple machine status based on runtime activity
        TimeSpan timeSinceUpdate = GetTimeSinceLastRuntimeUpdate();
        
        // Machine is running if runtime has changed within the last 30 seconds
        if (timeSinceUpdate <= TimeSpan.FromSeconds(30))
        {
            return "Running"; // Machine is actively running (runtime updating)
        }
        
        // Machine appears stopped if runtime hasn't changed for more than 30 seconds
        if (timeSinceUpdate <= TimeSpan.FromMinutes(5))
        {
            return "Stopped"; // Machine has stopped (runtime not updating)
        }
        
        // Machine has been idle for extended period
        return "Idle"; // Machine idle for more than 5 minutes
    }

    private void UpdateTrendingData(CalculationResults results)
    {
        // Add to history
        _qualityHistory.Enqueue(results.Quality);
        _performanceHistory.Enqueue(results.Performance);
        _availabilityHistory.Enqueue(results.Availability);
        _oeeHistory.Enqueue(results.OEE);

        // Maintain window size
        while (_qualityHistory.Count > MaxHistorySize) _qualityHistory.Dequeue();
        while (_performanceHistory.Count > MaxHistorySize) _performanceHistory.Dequeue();
        while (_availabilityHistory.Count > MaxHistorySize) _availabilityHistory.Dequeue();
        while (_oeeHistory.Count > MaxHistorySize) _oeeHistory.Dequeue();

        // Always calculate trends - even with limited data
        if (_qualityHistory.Count >= 2) // Minimum 2 data points for any trend analysis
        {
            results.QualityTrend = CalculateTrend(_qualityHistory);
            results.PerformanceTrend = CalculateTrend(_performanceHistory);
            results.AvailabilityTrend = CalculateTrend(_availabilityHistory);
            results.OEETrend = CalculateTrend(_oeeHistory);
        }
        else
        {
            // Set default values when insufficient data for trends
            results.QualityTrend = results.PerformanceTrend = results.AvailabilityTrend = results.OEETrend = "Insufficient Data";
        }

        // Always calculate statistics if we have any data
        if (_qualityHistory.Count >= 1)
        {
            // Calculate statistics using pre-cached arrays to reduce LINQ overhead
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
        else
        {
            // Set current values as defaults when no history exists yet
            results.MinQuality = results.MaxQuality = results.AvgQuality = results.Quality;
            results.MinPerformance = results.MaxPerformance = results.AvgPerformance = results.Performance;
            results.MinAvailability = results.MaxAvailability = results.AvgAvailability = results.Availability;
            results.MinOEE = results.MaxOEE = results.AvgOEE = results.OEE;
        }
    }

    private string CalculateTrend(Queue<double> history)
    {
        if (history.Count < 2) return "Insufficient Data"; // Need at least 2 data points
        
        var dataArray = history.ToArray();
        
        // For small datasets (2-4 points), just compare first and last
        if (dataArray.Length <= 4)
        {
            double firstValue = dataArray[0];
            double lastValue = dataArray[dataArray.Length - 1];
            double simpleChange = lastValue - firstValue;
            
            if (simpleChange >= 2.0) return "Rising Strongly";
            if (simpleChange >= 0.5) return "Rising";
            if (simpleChange <= -2.0) return "Falling Strongly";
            if (simpleChange <= -0.5) return "Falling";
            return "Stable";
        }
        
        // For larger datasets, compare halves for better trend analysis
        int halfSize = Math.Max(dataArray.Length / 2, 2); // Ensure minimum comparison size
        
        var firstHalf = dataArray.Take(halfSize);
        var secondHalf = dataArray.Skip(dataArray.Length - halfSize); // Take last half for better trend detection
        
        double firstAvg = firstHalf.Average();
        double secondAvg = secondHalf.Average();
        double avgChange = secondAvg - firstAvg;
        
        if (avgChange >= 2.0) return "Rising Strongly";
        if (avgChange >= 0.5) return "Rising";
        if (avgChange <= -2.0) return "Falling Strongly";
        if (avgChange <= -0.5) return "Falling";
        return "Stable";
    }

    private void WriteAllOutputs(CalculationResults results)
    {
        // Core OEE outputs
        WriteIfExists(TotalCountVar, results.TotalCount);
        WriteIfExists(QualityVar, results.Quality);
        WriteIfExists(PerformanceVar, results.Performance);
        WriteIfExists(AvailabilityVar, results.Availability);
        WriteIfExists(OEEVar, results.OEE);
        WriteIfExists(PartsPerHourVar, results.PartsPerHour);
        WriteIfExists(ExpectedPartCountVar, results.ExpectedPartCount);
        WriteIfExists(DowntimeFormattedVar, FormatSeconds(results.DowntimeSeconds));
        WriteIfExists(TotalRuntimeFormattedVar, FormatSeconds(results.RuntimeSeconds));

        // AvgCycleTime: write formatted string if the node is string-typed; otherwise write numeric seconds
        if (_outputPresenceFlags.ContainsKey(AvgCycleTimeVar) && _outputPresenceFlags[AvgCycleTimeVar])
        {
            if (_avgCycleVarIsString)
            {
                TrySetValueWithCooldown(AvgCycleTimeVar, FormatCycleTime(results.AvgCycleTimeSeconds));
            }
            else
            {
                TrySetValueWithCooldown(AvgCycleTimeVar, results.AvgCycleTimeSeconds);
            }
        }

        // Shift tracking outputs
        WriteIfExists(CurrentShiftNumberVar, results.CurrentShiftNumber);
        WriteIfExists(ShiftStartTimeOutputVar, results.ShiftStartTime);
        WriteIfExists(ShiftEndTimeVar, results.ShiftEndTime);
        WriteIfExists(TimeIntoShiftVar, results.TimeIntoShift);
        WriteIfExists(TimeRemainingInShiftVar, results.TimeRemainingInShift);
        WriteIfExists(ShiftChangeOccurredVar, results.ShiftChangeOccurred);
        WriteIfExists(ShiftChangeImminent, results.ShiftChangeImminent);

        // Production planning outputs
        WriteIfExists(ProjectedTotalCountVar, results.ProjectedTotalCount);
        WriteIfExists(RemainingTimeAtCurrentRateVar, results.RemainingTimeAtCurrentRate);
        WriteIfExists(ProductionBehindScheduleVar, results.ProductionBehindSchedule);
        WriteIfExists(RequiredRateToTargetVar, results.RequiredRateToTarget);
        WriteIfExists(TargetVsActualPartsVar, results.TargetVsActualParts);

        // System health outputs
        WriteIfExists(LastUpdateTimeVar, results.LastUpdateTime);
        WriteIfExists(SystemStatusVar, results.SystemStatus);
        WriteIfExists(CalculationValidVar, results.CalculationValid);
        WriteIfExists(DataQualityScoreVar, results.DataQualityScore);

        // Trending outputs
        WriteIfExists(QualityTrendVar, results.QualityTrend);
        WriteIfExists(PerformanceTrendVar, results.PerformanceTrend);
        WriteIfExists(AvailabilityTrendVar, results.AvailabilityTrend);
        WriteIfExists(OEETrendVar, results.OEETrend);

        // Statistics outputs
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

        // Target comparison outputs
        WriteIfExists(QualityVsTargetVar, results.QualityVsTarget);
        WriteIfExists(PerformanceVsTargetVar, results.PerformanceVsTarget);
        WriteIfExists(AvailabilityVsTargetVar, results.AvailabilityVsTarget);
        WriteIfExists(OEEVsTargetVar, results.OEEVsTarget);
    }

    // Helper methods and existing code continues...
    private bool TrySetValueWithCooldown(IUAVariable var, object value)
    {
        if (var == null) return false;
        if (!_outputPresenceFlags.ContainsKey(var)) return false;

        // If we previously marked presence false due to failure, allow retry after cooldown.
        if (!_outputPresenceFlags[var])
        {
            if (_lastWriteFailureUtc.TryGetValue(var, out DateTime lastFail))
            {
                if ((DateTime.UtcNow - lastFail) < WriteRetryCooldown)
                    return false; // skip quick retry
                // else allow retry attempt
            }
        }

        // Handle empty string values - provide meaningful defaults
        if (value is string stringValue && string.IsNullOrEmpty(stringValue))
        {
            return false; // Don't write empty strings
        }

        try
        {
            var.SetValue(value);
            // success -> clear failure record and mark present
            _lastWriteFailureUtc.Remove(var);
            _outputPresenceFlags[var] = true;
            return true;
        }
        catch (Exception ex)
        {
            LogError($"Write failed for '{GetVariableIdentifier(var)}' value='{value}' ({value?.GetType().Name}): {ex.Message}");
            _lastWriteFailureUtc[var] = DateTime.UtcNow;
            _outputPresenceFlags[var] = false;
            return false;
        }
    }

    private void WriteIfExists(IUAVariable var, object value)
    {
        if (!_outputPresenceFlags.ContainsKey(var) || !_outputPresenceFlags[var] || var == null) return;
        TrySetValueWithCooldown(var, value);
    }

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
        try { return var.ToString() ?? "iuavariable"; }
        catch { return "iuavariable"; }
    }

    private string GetVariableFullPath(IUAVariable var)
    {
        if (var == null) return "null";
        try 
        { 
            // Try to get the full browse path to show exactly where this variable is located
            return var.BrowseName?.ToString() ?? var.ToString() ?? "unknown_path";
        }
        catch 
        { 
            return "path_unavailable"; 
        }
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

    private bool TryGetSecondsFromRaw(object raw, out double seconds)
    {
        seconds = 0.0;
        if (raw == null) return false;
        try
        {
            if (raw is double dv) { seconds = dv; return seconds > 0.0; }
            if (raw is float fv) { seconds = Convert.ToDouble(fv); return seconds > 0.0; }
            if (raw is int iv) { seconds = Convert.ToDouble(iv); return seconds > 0.0; }
            if (raw is long lv) { seconds = Convert.ToDouble(lv); return seconds > 0.0; }
            if (raw is TimeSpan ts) { seconds = ts.TotalSeconds; return seconds > 0.0; }
            if (raw is DateTime dt) { seconds = dt.TimeOfDay.TotalSeconds; return seconds > 0.0; }

            string s = raw.ToString()?.Trim();
            if (string.IsNullOrEmpty(s)) return false;

            if (TimeSpan.TryParse(s, out TimeSpan parsedTs))
            {
                seconds = parsedTs.TotalSeconds;
                return seconds > 0.0;
            }

            if (double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double parsedD))
            {
                seconds = parsedD;
                return seconds > 0.0;
            }
        }
        catch (Exception ex)
        {
            LogError($"Parse seconds error: {ex.Message}");
        }
        return false;
    }

    private bool TryGetHoursFromRaw(object raw, out double hours)
    {
        hours = 0.0;
        if (raw == null) return false;
        try
        {
            if (raw is double dv) { hours = dv; return hours > 0.0; }
            if (raw is float fv) { hours = Convert.ToDouble(fv); return hours > 0.0; }
            if (raw is int iv) { hours = Convert.ToDouble(iv); return hours > 0.0; }
            if (raw is long lv) { hours = Convert.ToDouble(lv); return hours > 0.0; }
            if (raw is TimeSpan ts) { hours = ts.TotalHours; return hours > 0.0; }
            if (raw is DateTime dt) { hours = dt.TimeOfDay.TotalHours; return hours > 0.0; }

            string s = raw.ToString()?.Trim();
            if (string.IsNullOrEmpty(s)) return false;

            if (TimeSpan.TryParse(s, out TimeSpan parsedTs))
            {
                hours = parsedTs.TotalHours;
                return hours > 0.0;
            }

            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDt))
            {
                hours = parsedDt.TimeOfDay.TotalHours;
                return hours > 0.0;
            }

            if (double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double parsedD))
            {
                hours = parsedD;
                return hours > 0.0;
            }
        }
        catch (Exception ex)
        {
            LogError($"Parse hours error: {ex.Message}");
        }
        return false;
    }

    private bool TryParseTimeSpan(object raw, out TimeSpan timeSpan)
    {
        timeSpan = TimeSpan.Zero;
        if (raw == null) return false;
        
        try
        {
            if (raw is TimeSpan ts) { timeSpan = ts; return true; }
            if (raw is DateTime dt) { timeSpan = dt.TimeOfDay; return true; }
            
            string s = raw.ToString()?.Trim();
            if (string.IsNullOrEmpty(s)) return false;
            
            if (TimeSpan.TryParse(s, out TimeSpan parsed))
            {
                timeSpan = parsed;
                return true;
            }
        }
        catch (Exception ex)
        {
            LogError($"Parse TimeSpan error: {ex.Message}");
        }
        return false;
    }

    private string FormatSeconds(double seconds)
    {
        if (double.IsNaN(seconds) || seconds <= 0) return "00:00:00";
        TimeSpan ts = TimeSpan.FromSeconds(seconds);
        return string.Format("{0:D2}:{1:D2}:{2:D2}", (int)ts.TotalHours, ts.Minutes, ts.Seconds);
    }

    private string FormatCycleTime(double seconds)
    {
        if (double.IsNaN(seconds) || seconds <= 0.0) return "0.000 s";
        if (seconds < 60.0) return $"{seconds:F3} s";
        int minutes = (int)(seconds / 60);
        double rem = seconds - minutes * 60;
        int secs = (int)rem;
        int msec = (int)Math.Round((rem - secs) * 1000.0);
        if (msec >= 1000) { secs += 1; msec -= 1000; }
        return string.Format("{0:D2}:{1:D2}.{2:000}", minutes, secs, msec);
    }

    // Logging methods
    private void LogError(string message)
    {
        Log.Error("ManufacturingOEE_Calculator", message);
    }

    private void LogInfo(string message)
    {
        if (_loggingVerbosity >= 1)
            Log.Info("ManufacturingOEE_Calculator", message);
    }

    // Results class to hold all calculated values
    private class CalculationResults
    {
        // Core metrics
        public int TotalCount { get; set; }
        public int GoodPartCount { get; set; }
        public int BadPartCount { get; set; }
        public double RuntimeSeconds { get; set; }
        public double Quality { get; set; }
        public double Performance { get; set; }
        public double Availability { get; set; }
        public double OEE { get; set; }
        public double AvgCycleTimeSeconds { get; set; }
        public double PartsPerHour { get; set; }
        public double ExpectedPartCount { get; set; }
        public double PlannedProductionSeconds { get; set; }
        public double DowntimeSeconds { get; set; }

        // Shift info
        public int CurrentShiftNumber { get; set; } = 1;
        public string ShiftStartTime { get; set; } = "06:00:00";
        public string ShiftEndTime { get; set; } = "14:00:00";
        public string TimeIntoShift { get; set; } = "00:00:00";
        public string TimeRemainingInShift { get; set; } = "08:00:00";
        public bool ShiftChangeOccurred { get; set; }
        public bool ShiftChangeImminent { get; set; }

        // Production planning
        public int ProductionTarget { get; set; }
        public bool ProductionBehindSchedule { get; set; }
        public double ProjectedTotalCount { get; set; }
        public string RemainingTimeAtCurrentRate { get; set; } = "N/A";
        public double RequiredRateToTarget { get; set; }
        public int TargetVsActualParts { get; set; }

        // System health
        public string LastUpdateTime { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        public string SystemStatus { get; set; } = "Starting";
        public bool CalculationValid { get; set; } = true;
        public double DataQualityScore { get; set; } = 100.0;

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
