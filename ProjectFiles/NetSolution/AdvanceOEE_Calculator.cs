using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using UAManagedCore;
using FTOptix.NetLogic;
using FTOptix.Core;
using FTOptix.HMIProject;

/// <summary>
/// AdvanceOEE_Calculator - Multi-Instance OEE Calculator with Folder Scanning
/// 
/// Setup:
/// 1. Add a NodeId variable named "OEEInstancesFolder" to this NetLogic
/// 2. Set its value to point to a folder containing OEE ObjectType instances
/// 3. If left empty, it will use the Owner folder
/// 4. The script automatically discovers all OEE instances at startup
/// 
/// Features:
/// - Processes multiple OEE ObjectType instances with a single script
/// - Per-instance trending, statistics, and shift tracking
/// - Advanced calculations with caching and change detection
/// - Error isolation - one failing instance doesn't stop others
/// - Detailed logging with instance identification
/// </summary>
public class AdvanceOEE_Calculator : BaseNetLogic
{
    // Configuration
    private const int UPDATE_INTERVAL_MS = 1000; // Default 1 second
    private const int MaxHistorySize = 60;
    private readonly TimeSpan WriteRetryCooldown = TimeSpan.FromSeconds(30);

    // Runtime
    private IUANode _oeeInstancesFolder;
    private List<IUANode> _oeeInstances = new List<IUANode>();
    private Dictionary<NodeId, InstanceState> _instanceStates = new Dictionary<NodeId, InstanceState>();
    private PeriodicTask _periodicTask;

    #region Lifecycle Methods

    public override void Start()
    {
        try
        {
            // Determine which folder to monitor
            _oeeInstancesFolder = GetOEEInstancesFolder();
            
            if (_oeeInstancesFolder == null)
            {
                LogError("Could not determine OEE instances folder. Exiting.");
                return;
            }

            LogInfo($"Monitoring folder: {_oeeInstancesFolder.BrowseName}");

            // Discover OEE instances once at startup
            _oeeInstances = DiscoverOEEInstances();
            LogInfo($"Found {_oeeInstances.Count} OEE instance(s)");

            if (_oeeInstances.Count == 0)
            {
                LogWarning("No OEE ObjectType instances found. Ensure objects have Inputs, Outputs, and Configuration folders.");
            }
            else
            {
                // Initialize state for each instance
                foreach (var instance in _oeeInstances)
                {
                    try
                    {
                        var state = InitializeInstanceState(instance);
                        _instanceStates[instance.NodeId] = state;
                        LogInfo($"[{instance.BrowseName}] Initialized {state.InitializedVariableCount} variables");
                    }
                    catch (Exception ex)
                    {
                        LogError($"[{instance.BrowseName}] Failed to initialize: {ex.Message}");
                    }
                }

                // Run initial calculation
                ProcessAllInstances();

                // Start periodic updates
                int updateRate = UPDATE_INTERVAL_MS;
                if (_instanceStates.Count > 0)
                {
                    var firstState = _instanceStates.Values.First();
                    if (firstState.UpdateRateMsVar != null)
                    {
                        int configuredRate = ReadIntVar(firstState.UpdateRateMsVar, -1);
                        if (configuredRate > 0)
                            updateRate = configuredRate;
                    }
                }

                _periodicTask = new PeriodicTask(ProcessAllInstances, updateRate, LogicObject);
                _periodicTask.Start();

                LogInfo("Started successfully");
            }
        }
        catch (Exception ex)
        {
            LogError($"Startup error: {ex.Message}");
        }
    }

    public override void Stop()
    {
        try
        {
            _periodicTask?.Dispose();
            _periodicTask = null;
            LogInfo("Stopped successfully");
        }
        catch (Exception ex)
        {
            LogError($"Stop error: {ex.Message}");
        }
    }

    #endregion

    #region Discovery Methods

    /// <summary>
    /// Get the folder to monitor from the NodeId variable or use Owner as fallback
    /// </summary>
    private IUANode GetOEEInstancesFolder()
    {
        var folderVariable = LogicObject.GetVariable("OEEInstancesFolder");
        
        if (folderVariable != null)
        {
            var nodeId = GetUnderlyingValue(folderVariable);
            
            if (nodeId != null && nodeId is NodeId nId && nId != NodeId.Empty)
            {
                var folder = InformationModel.Get(nId);
                if (folder != null)
                {
                    return folder;
                }
                LogWarning("OEEInstancesFolder points to invalid node. Using Owner.");
            }
        }
        
        return Owner;
    }

    /// <summary>
    /// Discover all OEE ObjectType instances in the folder
    /// </summary>
    private List<IUANode> DiscoverOEEInstances()
    {
        var instances = new List<IUANode>();
        
        foreach (var child in _oeeInstancesFolder.Children)
        {
            if (IsOEEInstance(child))
            {
                instances.Add(child);
            }
        }
        
        return instances;
    }

    /// <summary>
    /// Check if a node is an OEE instance by verifying required structure
    /// </summary>
    private bool IsOEEInstance(IUANode node)
    {
        return node.Get("Inputs") != null && 
               node.Get("Outputs") != null && 
               node.Get("Configuration") != null;
    }

    #endregion

    #region Instance State Initialization

    /// <summary>
    /// Initialize all state for a single OEE instance
    /// </summary>
    private InstanceState InitializeInstanceState(IUANode instance)
    {
        var state = new InstanceState
        {
            Instance = instance,
            InstanceName = instance.BrowseName
        };

        // Map ObjectType structure to state variables
        
        // Input mappings - Data
        state.TotalRuntimeSecondsVar = GetVariableFromPath(instance, "Inputs/Data/TotalRuntimeSeconds");
        state.GoodPartCountVar = GetVariableFromPath(instance, "Inputs/Data/GoodPartCount");
        state.BadPartCountVar = GetVariableFromPath(instance, "Inputs/Data/BadPartCount");
        
        // Input mappings - Production
        state.IdealCycleTimeSecondsVar = GetVariableFromPath(instance, "Inputs/Production/IdealCycleTimeSeconds");
        state.PlannedProductionTimeHoursVar = GetVariableFromPath(instance, "Inputs/Production/PlannedProductionTimeHours");
        state.NumberOfShiftsVar = GetVariableFromPath(instance, "Inputs/Production/NumberOfShifts");
        state.ShiftStartTimeVar = GetVariableFromPath(instance, "Inputs/Production/ShiftStartTime");
        state.ProductionTargetVar = GetVariableFromPath(instance, "Inputs/Production/ProductionTarget");

        // Input mappings - System
        state.UpdateRateMsVar = GetVariableFromPath(instance, "Inputs/System/UpdateRateMs");
        state.LoggingVerbosityVar = GetVariableFromPath(instance, "Inputs/System/LoggingVerbosity");

        // Input mappings - Targets
        state.QualityTargetVar = GetVariableFromPath(instance, "Inputs/Targets/QualityTarget");
        state.PerformanceTargetVar = GetVariableFromPath(instance, "Inputs/Targets/PerformanceTarget");
        state.AvailabilityTargetVar = GetVariableFromPath(instance, "Inputs/Targets/AvailabilityTarget");
        state.OEETargetVar = GetVariableFromPath(instance, "Inputs/Targets/OEETarget");
        
        // Configuration folder mappings
        state.EnableRealTimeCalcVar = GetVariableFromPath(instance, "Configuration/EnableRealTimeCalc");
        state.MinimumRunTimeVar = GetVariableFromPath(instance, "Configuration/MinimumRunTime");
        state.GoodOEEThresholdVar = GetVariableFromPath(instance, "Configuration/GoodOEE_Threshold");
        state.PoorOEEThresholdVar = GetVariableFromPath(instance, "Configuration/PoorOEE_Threshold");
        state.EnableLoggingVar = GetVariableFromPath(instance, "Configuration/EnableLogging");
        state.EnableAlarmsVar = GetVariableFromPath(instance, "Configuration/EnableAlarms");
        state.SystemHealthyVar = GetVariableFromPath(instance, "Configuration/SystemHealthy");

        // Output mappings - Core
        state.TotalCountVar = GetVariableFromPath(instance, "Outputs/Core/TotalCount");
        state.QualityVar = GetVariableFromPath(instance, "Outputs/Core/Quality");
        state.PerformanceVar = GetVariableFromPath(instance, "Outputs/Core/Performance");
        state.AvailabilityVar = GetVariableFromPath(instance, "Outputs/Core/Availability");
        state.OEEVar = GetVariableFromPath(instance, "Outputs/Core/OEE");
        state.AvgCycleTimeVar = GetVariableFromPath(instance, "Outputs/Core/AvgCycleTime");
        state.PartsPerHourVar = GetVariableFromPath(instance, "Outputs/Core/PartsPerHour");
        state.ExpectedPartCountVar = GetVariableFromPath(instance, "Outputs/Core/ExpectedPartCount");

        // Output mappings - Shift
        state.HoursPerShiftVar = GetVariableFromPath(instance, "Outputs/Shift/HoursPerShift");
        state.CurrentShiftNumberVar = GetVariableFromPath(instance, "Outputs/Shift/CurrentShiftNumber");
        state.ShiftStartTimeOutputVar = GetVariableFromPath(instance, "Outputs/Shift/ShiftStartTimeOutput");
        state.ShiftEndTimeVar = GetVariableFromPath(instance, "Outputs/Shift/ShiftEndTime");
        state.TimeIntoShiftVar = GetVariableFromPath(instance, "Outputs/Shift/TimeIntoShift");
        state.TimeRemainingInShiftVar = GetVariableFromPath(instance, "Outputs/Shift/TimeRemainingInShift");
        state.ShiftChangeOccurredVar = GetVariableFromPath(instance, "Outputs/Shift/ShiftChangeOccurred");
        state.ShiftChangeImminentVar = GetVariableFromPath(instance, "Outputs/Shift/ShiftChangeImminent");
        state.ShiftProgressVar = GetVariableFromPath(instance, "Outputs/Shift/ShiftProgress");

        // Output mappings - Production
        state.ProjectedTotalCountVar = GetVariableFromPath(instance, "Outputs/Production/ProjectedTotalCount");
        state.RemainingTimeAtCurrentRateVar = GetVariableFromPath(instance, "Outputs/Production/RemainingTimeAtCurrentRate");
        state.ProductionBehindScheduleVar = GetVariableFromPath(instance, "Outputs/Production/ProductionBehindSchedule");
        state.RequiredRateToTargetVar = GetVariableFromPath(instance, "Outputs/Production/RequiredRateToTarget");
        state.TargetVsActualPartsVar = GetVariableFromPath(instance, "Outputs/Production/TargetVsActualParts");
        state.ProductionProgressVar = GetVariableFromPath(instance, "Outputs/Production/ProductionProgress");

        // Output mappings - System
        state.SystemStatusVar = GetVariableFromPath(instance, "Outputs/System/SystemStatus");
        state.CalculationValidVar = GetVariableFromPath(instance, "Outputs/System/CalculationValid");
        state.DataQualityScoreVar = GetVariableFromPath(instance, "Outputs/System/DataQualityScore");
        state.LastUpdateTimeVar = GetVariableFromPath(instance, "Outputs/System/LastUpdateTime");
        state.DowntimeFormattedVar = GetVariableFromPath(instance, "Outputs/System/DowntimeFormatted");
        state.TotalRuntimeFormattedVar = GetVariableFromPath(instance, "Outputs/System/TotalRuntimeFormatted");

        // Trending variables
        state.QualityTrendVar = GetVariableFromPath(instance, "Outputs/Trends/QualityTrend");
        state.PerformanceTrendVar = GetVariableFromPath(instance, "Outputs/Trends/PerformanceTrend");
        state.AvailabilityTrendVar = GetVariableFromPath(instance, "Outputs/Trends/AvailabilityTrend");
        state.OEETrendVar = GetVariableFromPath(instance, "Outputs/Trends/OEETrend");

        // Statistics variables
        state.MinQualityVar = GetVariableFromPath(instance, "Outputs/Statistics/MinQuality");
        state.MaxQualityVar = GetVariableFromPath(instance, "Outputs/Statistics/MaxQuality");
        state.AvgQualityVar = GetVariableFromPath(instance, "Outputs/Statistics/AvgQuality");
        state.MinPerformanceVar = GetVariableFromPath(instance, "Outputs/Statistics/MinPerformance");
        state.MaxPerformanceVar = GetVariableFromPath(instance, "Outputs/Statistics/MaxPerformance");
        state.AvgPerformanceVar = GetVariableFromPath(instance, "Outputs/Statistics/AvgPerformance");
        state.MinAvailabilityVar = GetVariableFromPath(instance, "Outputs/Statistics/MinAvailability");
        state.MaxAvailabilityVar = GetVariableFromPath(instance, "Outputs/Statistics/MaxAvailability");
        state.AvgAvailabilityVar = GetVariableFromPath(instance, "Outputs/Statistics/AvgAvailability");
        state.MinOEEVar = GetVariableFromPath(instance, "Outputs/Statistics/MinOEE");
        state.MaxOEEVar = GetVariableFromPath(instance, "Outputs/Statistics/MaxOEE");
        state.AvgOEEVar = GetVariableFromPath(instance, "Outputs/Statistics/AvgOEE");

        // Target comparison variables
        state.QualityVsTargetVar = GetVariableFromPath(instance, "Outputs/Targets/QualityVsTarget");
        state.PerformanceVsTargetVar = GetVariableFromPath(instance, "Outputs/Targets/PerformanceVsTarget");
        state.AvailabilityVsTargetVar = GetVariableFromPath(instance, "Outputs/Targets/AvailabilityVsTarget");
        state.OEEVsTargetVar = GetVariableFromPath(instance, "Outputs/Targets/OEEVsTarget");

        // Count initialized variables
        state.InitializedVariableCount = CountNonNullVariables(state);
        
        // Cache presence flags for output variables
        CachePresenceFlags(state);
        
        // Ensure input defaults
        EnsureInputDefaults(state);
        
        // Read configuration
        ReadConfiguration(state);

        return state;
    }

    private IUAVariable GetVariableFromPath(IUANode instance, string path)
    {
        if (instance == null || string.IsNullOrEmpty(path))
            return null;

        try
        {
            return instance.Get(path) as IUAVariable;
        }
        catch
        {
            return null;
        }
    }

    private int CountNonNullVariables(InstanceState state)
    {
        var allVars = new IUAVariable[] 
        {
            state.TotalRuntimeSecondsVar, state.GoodPartCountVar, state.BadPartCountVar, state.IdealCycleTimeSecondsVar,
            state.PlannedProductionTimeHoursVar, state.NumberOfShiftsVar, state.ShiftStartTimeVar, state.ProductionTargetVar,
            state.TotalCountVar, state.QualityVar, state.PerformanceVar, state.AvailabilityVar, state.OEEVar, state.AvgCycleTimeVar,
            state.PartsPerHourVar, state.SystemStatusVar, state.CalculationValidVar, state.DataQualityScoreVar,
            state.HoursPerShiftVar, state.CurrentShiftNumberVar, state.ShiftChangeOccurredVar
        };
        
        return allVars.Count(v => v != null);
    }

    private void CachePresenceFlags(InstanceState state)
    {
        var allOutputVars = new IUAVariable[]
        {
            state.TotalCountVar, state.QualityVar, state.PerformanceVar, state.AvailabilityVar, state.OEEVar, 
            state.AvgCycleTimeVar, state.PartsPerHourVar, state.ExpectedPartCountVar, state.HoursPerShiftVar,
            state.DowntimeFormattedVar, state.TotalRuntimeFormattedVar, state.CurrentShiftNumberVar,
            state.ShiftStartTimeOutputVar, state.ShiftEndTimeVar, state.TimeIntoShiftVar, state.TimeRemainingInShiftVar,
            state.ShiftChangeOccurredVar, state.ShiftChangeImminentVar,
            state.ProjectedTotalCountVar, state.RemainingTimeAtCurrentRateVar, state.ProductionBehindScheduleVar,
            state.RequiredRateToTargetVar, state.TargetVsActualPartsVar, state.ShiftProgressVar, state.ProductionProgressVar,
            state.SystemStatusVar, state.CalculationValidVar, state.DataQualityScoreVar, state.LastUpdateTimeVar,
            state.QualityTrendVar, state.PerformanceTrendVar, state.AvailabilityTrendVar, state.OEETrendVar,
            state.MinQualityVar, state.MaxQualityVar, state.AvgQualityVar,
            state.MinPerformanceVar, state.MaxPerformanceVar, state.AvgPerformanceVar,
            state.MinAvailabilityVar, state.MaxAvailabilityVar, state.AvgAvailabilityVar,
            state.MinOEEVar, state.MaxOEEVar, state.AvgOEEVar,
            state.QualityVsTargetVar, state.PerformanceVsTargetVar, state.AvailabilityVsTargetVar, state.OEEVsTargetVar
        };

        foreach (var var in allOutputVars)
        {
            if (var != null)
            {
                state.OutputPresenceFlags[var] = true;
            }
        }
    }

    private void EnsureInputDefaults(InstanceState state)
    {
        if (state.DefaultsInitialized) return;

        WriteDefaultIfEmpty(state, state.TotalRuntimeSecondsVar, 0.0, "TotalRuntimeSeconds");
        WriteDefaultIfEmpty(state, state.GoodPartCountVar, 0, "GoodPartCount");
        WriteDefaultIfEmpty(state, state.BadPartCountVar, 0, "BadPartCount");
        WriteDefaultIfEmpty(state, state.IdealCycleTimeSecondsVar, 30.0, "IdealCycleTimeSeconds");
        WriteDefaultIfEmpty(state, state.PlannedProductionTimeHoursVar, 8.0, "PlannedProductionTimeHours");
        WriteDefaultIfEmpty(state, state.NumberOfShiftsVar, 3, "NumberOfShifts");
        WriteDefaultIfEmpty(state, state.ShiftStartTimeVar, DateTime.Today.AddHours(6), "ShiftStartTime");
        WriteDefaultIfEmpty(state, state.ProductionTargetVar, 1000, "ProductionTarget");
        WriteDefaultIfEmpty(state, state.UpdateRateMsVar, 1000, "UpdateRateMs");
        WriteDefaultIfEmpty(state, state.LoggingVerbosityVar, 1, "LoggingVerbosity");
        WriteDefaultIfEmpty(state, state.QualityTargetVar, 95.0, "QualityTarget");
        WriteDefaultIfEmpty(state, state.PerformanceTargetVar, 85.0, "PerformanceTarget");
        WriteDefaultIfEmpty(state, state.AvailabilityTargetVar, 90.0, "AvailabilityTarget");
        WriteDefaultIfEmpty(state, state.OEETargetVar, 72.7, "OEETarget");
        WriteDefaultIfEmpty(state, state.EnableRealTimeCalcVar, true, "EnableRealTimeCalc");
        WriteDefaultIfEmpty(state, state.MinimumRunTimeVar, 60.0, "MinimumRunTime");
        WriteDefaultIfEmpty(state, state.GoodOEEThresholdVar, 80.0, "GoodOEE_Threshold");
        WriteDefaultIfEmpty(state, state.PoorOEEThresholdVar, 60.0, "PoorOEE_Threshold");
        WriteDefaultIfEmpty(state, state.EnableLoggingVar, true, "EnableLogging");
        WriteDefaultIfEmpty(state, state.EnableAlarmsVar, true, "EnableAlarms");
        WriteDefaultIfEmpty(state, state.SystemHealthyVar, true, "SystemHealthy");

        state.DefaultsInitialized = true;
    }

    private void WriteDefaultIfEmpty(InstanceState state, IUAVariable var, object defaultValue, string varName)
    {
        if (var == null) 
        {
            LogWarning($"[{state.InstanceName}] {varName} variable not found, cannot set default");
            return;
        }

        try
        {
            var currentValue = GetUnderlyingValue(var);
            
            bool isEmpty = false;
            if (currentValue == null)
            {
                isEmpty = true;
            }
            else if (defaultValue is double && currentValue is double d && Math.Abs(d) < 0.0001)
            {
                isEmpty = true;
            }
            else if (defaultValue is int && currentValue is int i && i == 0)
            {
                isEmpty = true;
            }
            else if (defaultValue is string && string.IsNullOrWhiteSpace(currentValue.ToString()))
            {
                isEmpty = true;
            }
            else if (defaultValue is DateTime && (currentValue is DateTime cdt && cdt == default(DateTime)))
            {
                isEmpty = true;
            }

            if (isEmpty)
            {
                var.SetValue(defaultValue);
                if (state.LoggingVerbosity >= 2)
                    LogInfo($"[{state.InstanceName}] Set default {varName} = {defaultValue}");
            }
        }
        catch (Exception ex)
        {
            LogWarning($"[{state.InstanceName}] Failed to set default for {varName}: {ex.Message}");
        }
    }

    private void ReadConfiguration(InstanceState state)
    {
        if (state.UpdateRateMsVar != null && ReadIntVar(state.UpdateRateMsVar, -1) is int u && u > 0)
            state.UpdateRateMs = u;
        else
            state.UpdateRateMs = UPDATE_INTERVAL_MS;

        if (state.ShiftStartTimeVar != null)
        {
            state.ShiftStartDateTime = ReadDateTimeVar(state.ShiftStartTimeVar, DateTime.Today.AddHours(6));
        }
        else
        {
            state.ShiftStartDateTime = DateTime.Today.AddHours(6);
        }

        state.ProductionTarget = ReadIntVar(state.ProductionTargetVar, 1000);
        state.NumberOfShifts = ReadIntVar(state.NumberOfShiftsVar, 3);
        state.QualityTarget = ReadDoubleVar(state.QualityTargetVar, 95.0);
        state.PerformanceTarget = ReadDoubleVar(state.PerformanceTargetVar, 85.0);
        state.AvailabilityTarget = ReadDoubleVar(state.AvailabilityTargetVar, 90.0);
        state.OEETarget = ReadDoubleVar(state.OEETargetVar, 72.7);
        state.EnableRealTimeCalc = ReadBoolVar(state.EnableRealTimeCalcVar, true);
        state.MinimumRunTime = ReadDoubleVar(state.MinimumRunTimeVar, 60.0);
        state.GoodOEEThreshold = ReadDoubleVar(state.GoodOEEThresholdVar, 80.0);
        state.PoorOEEThreshold = ReadDoubleVar(state.PoorOEEThresholdVar, 60.0);
        state.EnableLogging = ReadBoolVar(state.EnableLoggingVar, true);
        state.EnableAlarms = ReadBoolVar(state.EnableAlarmsVar, true);
        state.SystemHealthy = ReadBoolVar(state.SystemHealthyVar, true);
        state.LoggingVerbosity = ReadIntVar(state.LoggingVerbosityVar, 1);
    }

    #endregion

    #region Main Processing

    /// <summary>
    /// Process all OEE instances - called every UPDATE_INTERVAL_MS
    /// </summary>
    private void ProcessAllInstances()
    {
        foreach (var instance in _oeeInstances)
        {
            // Error isolation - one failing instance doesn't stop others
            try
            {
                if (_instanceStates.TryGetValue(instance.NodeId, out var state))
                {
                    CalculateForInstance(instance, state);
                }
            }
            catch (Exception ex)
            {
                LogError($"[{instance.BrowseName}] Processing error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Calculate all metrics for a single instance
    /// </summary>
    private void CalculateForInstance(IUANode instance, InstanceState state)
    {
        var results = PerformCalculations(state);
        UpdateTrendingData(state, results);
        UpdateShiftInformation(state, DateTime.Now, results);
        WriteAllOutputs(state, results);
    }

    #endregion

    #region OEE Calculations

    private CalculationResults PerformCalculations(InstanceState state)
    {
        var results = new CalculationResults();
        
        var runtimeSeconds = ReadDoubleVar(state.TotalRuntimeSecondsVar, 0.0);
        var goodParts = ReadIntVar(state.GoodPartCountVar, 0);
        var badParts = ReadIntVar(state.BadPartCountVar, 0);
        var totalCount = goodParts + badParts;
        var idealCycle = GetIdealCycleTimeSeconds(state);
        var plannedSeconds = GetPlannedProductionSeconds(state);
        
        results.TotalCount = totalCount;
        results.GoodPartCount = goodParts;
        results.BadPartCount = badParts;
        results.RuntimeSeconds = runtimeSeconds;
        
        if (totalCount == 0)
        {
            results.Quality = 0.0;
        }
        else if (runtimeSeconds <= 0.0)
        {
            results.Quality = 0.0;
        }
        else if (idealCycle <= 0.0)
        {
            results.Quality = 0.0;
        }
        else
        {
            results.Quality = totalCount > 0 ? (goodParts / (double)totalCount) * 100.0 : 0.0;
            results.Performance = (totalCount * idealCycle / runtimeSeconds) * 100.0;
            results.Performance = Math.Min(100.0, results.Performance);
        }
        
        if (!double.IsNaN(plannedSeconds) && plannedSeconds > 0.0)
        {
            results.Availability = Math.Min(100.0, (runtimeSeconds / plannedSeconds) * 100.0);
        }
        else
        {
            results.Availability = 0.0;
        }

        results.OEE = (results.Quality * results.Performance * results.Availability) / 10000.0;
        
        if (totalCount > 0 && runtimeSeconds > 0.0)
            results.AvgCycleTime = runtimeSeconds / totalCount;
        
        if (plannedSeconds > 0.0 && idealCycle > 0.0)
            results.ExpectedPartCount = (int)(plannedSeconds / idealCycle);
        else
            results.ExpectedPartCount = 0;

        if (runtimeSeconds > 0)
            results.PartsPerHour = (totalCount / runtimeSeconds) * 3600.0;

        if (Math.Abs(runtimeSeconds - state.LastTotalRuntimeSeconds) > 0.1)
        {
            state.LastTotalRuntimeSeconds = runtimeSeconds;
            results.TotalRuntimeFormatted = FormatTimeSpan(TimeSpan.FromSeconds(runtimeSeconds));
        }
        else
        {
            results.TotalRuntimeFormatted = state.LastTotalRuntimeFormatted;
        }

        var downtimeSeconds = Math.Max(0, plannedSeconds - runtimeSeconds);
        if (Math.Abs(downtimeSeconds - state.LastDowntimeSeconds) > 0.1)
        {
            state.LastDowntimeSeconds = downtimeSeconds;
            results.DowntimeFormatted = FormatTimeSpan(TimeSpan.FromSeconds(downtimeSeconds));
        }
        else
        {
            results.DowntimeFormatted = state.LastDowntimeFormatted;
        }

        results.HoursPerShift = state.CalculatedHoursPerShift;

        if (state.ProductionTarget > 0)
            results.ProductionProgress = Math.Min(100.0, (totalCount / (double)state.ProductionTarget) * 100.0);
        else
            results.ProductionProgress = 0.0;

        if (runtimeSeconds > 0)
        {
            var currentRate = totalCount / runtimeSeconds;
            var timeRemaining = state.ProductionTarget - totalCount;
            
            if (currentRate > 0)
            {
                var secondsRemaining = timeRemaining / currentRate;
                results.RemainingTimeAtCurrentRate = FormatTimeSpan(TimeSpan.FromSeconds(secondsRemaining));
            }
            else
            {
                results.RemainingTimeAtCurrentRate = "∞";
            }
        }
        else
        {
            results.RemainingTimeAtCurrentRate = "N/A";
            results.ProjectedTotalCount = 0;
        }

        results.SystemStatus = DetermineSystemStatus(state, runtimeSeconds);
        state.PreviousRuntimeSeconds = runtimeSeconds;

        results.TargetVsActualParts = totalCount - state.ProductionTarget;
        results.ProductionBehindSchedule = results.TargetVsActualParts < 0;

        return results;
    }

    private string DetermineSystemStatus(InstanceState state, double currentRuntime)
    {
        if (state.PreviousRuntimeSeconds < 0) return "Starting";
        if (currentRuntime > state.PreviousRuntimeSeconds + 0.001) return "Running";
        return "Stopped";
    }

    private double GetIdealCycleTimeSeconds(InstanceState state)
    {
        var idealRaw = GetUnderlyingValue(state.IdealCycleTimeSecondsVar);
        if (!object.Equals(idealRaw, state.CachedIdealRaw))
        {
            if (TryGetDoubleFromRaw(idealRaw, out double sec))
            {
                state.CachedIdealSeconds = sec;
                state.CachedIdealValid = true;
            }
            else
            {
                state.CachedIdealSeconds = 0.0;
                state.CachedIdealValid = false;
            }
            state.CachedIdealRaw = idealRaw;
        }
        return state.CachedIdealValid ? state.CachedIdealSeconds : 0.0;
    }

    private double GetPlannedProductionSeconds(InstanceState state)
    {
        var plannedRaw = GetUnderlyingValue(state.PlannedProductionTimeHoursVar);
        if (!object.Equals(plannedRaw, state.CachedPlannedRaw))
        {
            if (TryGetDoubleFromRaw(plannedRaw, out double hours))
            {
                state.CachedPlannedHours = hours;
                state.CachedPlannedValid = true;
            }
            else
            {
                state.CachedPlannedHours = double.NaN;
                state.CachedPlannedValid = false;
            }
            state.CachedPlannedRaw = plannedRaw;
        }

        if (state.CachedPlannedValid)
        {
            return state.CachedPlannedHours * 3600.0;
        }

        return double.NaN;
    }

    #endregion

    #region Trending and Statistics

    private void UpdateTrendingData(InstanceState state, CalculationResults results)
    {
        bool shouldAddToHistory = (results.TotalCount > 0 || results.RuntimeSeconds > 0);
        
        if (shouldAddToHistory)
        {
            state.QualityHistory.Enqueue(results.Quality);
            state.PerformanceHistory.Enqueue(results.Performance);
            state.AvailabilityHistory.Enqueue(results.Availability);
            state.OEEHistory.Enqueue(results.OEE);

            while (state.QualityHistory.Count > MaxHistorySize) state.QualityHistory.Dequeue();
            while (state.PerformanceHistory.Count > MaxHistorySize) state.PerformanceHistory.Dequeue();
            while (state.AvailabilityHistory.Count > MaxHistorySize) state.AvailabilityHistory.Dequeue();
            while (state.OEEHistory.Count > MaxHistorySize) state.OEEHistory.Dequeue();
        }

        if (state.QualityHistory.Count >= 2)
        {
            results.QualityTrend = CalculateTrend(state.QualityHistory);
            results.PerformanceTrend = CalculateTrend(state.PerformanceHistory);
            results.AvailabilityTrend = CalculateTrend(state.AvailabilityHistory);
            results.OEETrend = CalculateTrend(state.OEEHistory);
        }

        if (state.QualityHistory.Count >= 1)
        {
            var qData = state.QualityHistory.ToArray();
            var pData = state.PerformanceHistory.ToArray();
            var aData = state.AvailabilityHistory.ToArray();
            var oData = state.OEEHistory.ToArray();

            results.MinQuality = qData.Min();
            results.MaxQuality = qData.Max();
            results.AvgQuality = qData.Average();

            results.MinPerformance = pData.Min();
            results.MaxPerformance = pData.Max();
            results.AvgPerformance = pData.Average();

            results.MinAvailability = aData.Min();
            results.MaxAvailability = aData.Max();
            results.AvgAvailability = aData.Average();

            results.MinOEE = oData.Min();
            results.MaxOEE = oData.Max();
            results.AvgOEE = oData.Average();

            results.QualityVsTarget = results.Quality - state.QualityTarget;
            results.PerformanceVsTarget = results.Performance - state.PerformanceTarget;
            results.AvailabilityVsTarget = results.Availability - state.AvailabilityTarget;
            results.OEEVsTarget = results.OEE - state.OEETarget;
        }
    }

    private string CalculateTrend(Queue<double> history)
    {
        if (history.Count < 2) return "Insufficient Data";

        var dataArray = history.ToArray();
        
        if (dataArray.Length <= 4)
        {
            var simpleChange = dataArray[dataArray.Length - 1] - dataArray[0];
            
            if (simpleChange >= 2.0) return "Rising Strongly";
            if (simpleChange >= 0.5) return "Rising";
            if (simpleChange <= -2.0) return "Falling Strongly";
            if (simpleChange <= -0.5) return "Falling";
            return "Stable";
        }

        var halfWindow = dataArray.Length / 2;
        var firstHalfAvg = dataArray.Take(halfWindow).Average();
        var secondHalfAvg = dataArray.Skip(dataArray.Length - halfWindow).Average();
        var avgChange = secondHalfAvg - firstHalfAvg;
        
        if (avgChange >= 2.0) return "Rising Strongly";
        if (avgChange >= 0.5) return "Rising";
        if (avgChange <= -2.0) return "Falling Strongly";
        if (avgChange <= -0.5) return "Falling";
        return "Stable";
    }

    #endregion

    #region Shift Calculations

    private void UpdateShiftInformation(InstanceState state, DateTime now, CalculationResults results)
    {
        if ((now - state.LastShiftCalculation).TotalSeconds < 1.0)
        {
            results.CurrentShiftNumber = state.CurrentShiftNumber;
            results.ShiftStartTimeOutput = state.CurrentShiftStart.ToString("HH:mm:ss");
            results.ShiftEndTime = state.CurrentShiftEnd.ToString("HH:mm:ss");
            results.ShiftChangeOccurred = state.ShiftChangeOccurred;
            results.ShiftChangeImminent = state.ShiftChangeImminent;
            results.HoursPerShift = state.CalculatedHoursPerShift;
            return;
        }

        state.LastShiftCalculation = now;

        var numberOfShifts = Math.Max(1, Math.Min(3, state.NumberOfShifts));
        var shiftDurationSeconds = (24 * 3600) / numberOfShifts;
        state.CalculatedHoursPerShift = shiftDurationSeconds / 3600.0;

        var shift1Time = state.ShiftStartDateTime.TimeOfDay;

        var currentTime = now.TimeOfDay;
        var secondsSinceShift1 = (currentTime - shift1Time).TotalSeconds;
        if (secondsSinceShift1 < 0)
        {
            secondsSinceShift1 += 24 * 3600;
        }

        var currentShift = ((int)(secondsSinceShift1 / shiftDurationSeconds) % numberOfShifts) + 1;
        var shiftStartOffset = (currentShift - 1) * shiftDurationSeconds;
        var shiftStartTime = now.Date.Add(shift1Time).AddSeconds(shiftStartOffset);
        
        if (shiftStartTime > now)
        {
            shiftStartTime = shiftStartTime.AddDays(-1);
        }

        var shiftElapsedTime = (now - shiftStartTime).TotalSeconds;
        var shiftRemainingTime = shiftDurationSeconds - shiftElapsedTime;

        var shiftEndTime = shiftStartTime.AddSeconds(shiftDurationSeconds);

        bool shiftChangeOccurred = (state.CurrentShiftNumber != currentShift);
        bool shiftChangeImminent = (shiftRemainingTime >= 0 && shiftRemainingTime <= 300);

        state.CurrentShiftNumber = currentShift;
        state.CurrentShiftStart = shiftStartTime;
        state.CurrentShiftEnd = shiftEndTime;
        state.ShiftChangeOccurred = shiftChangeOccurred;
        state.ShiftChangeImminent = shiftChangeImminent;

        results.CurrentShiftNumber = currentShift;
        results.ShiftStartTimeOutput = shiftStartTime.ToString("HH:mm:ss");
        results.ShiftEndTime = shiftEndTime.ToString("HH:mm:ss");
        results.TimeIntoShift = FormatTimeSpan(TimeSpan.FromSeconds(shiftElapsedTime));
        results.TimeRemainingInShift = FormatTimeSpan(TimeSpan.FromSeconds(Math.Max(0, shiftRemainingTime)));
        results.ShiftChangeOccurred = shiftChangeOccurred;
        results.ShiftChangeImminent = shiftChangeImminent;
        results.ShiftProgress = Math.Min(100.0, (shiftElapsedTime / shiftDurationSeconds) * 100.0);
        results.HoursPerShift = state.CalculatedHoursPerShift;
    }

    #endregion

    #region Output Writing

    private void WriteAllOutputs(InstanceState state, CalculationResults results)
    {
        results.CalculationValid = true;
        results.DataQualityScore = CalculateDataQuality(state, results);
        results.LastUpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        WriteIfExists(state, state.TotalCountVar, results.TotalCount);
        WriteIfExists(state, state.QualityVar, results.Quality);
        WriteIfExists(state, state.PerformanceVar, results.Performance);
        WriteIfExists(state, state.AvailabilityVar, results.Availability);
        WriteIfExists(state, state.OEEVar, results.OEE);
        WriteIfExists(state, state.AvgCycleTimeVar, results.AvgCycleTime);
        WriteIfExists(state, state.PartsPerHourVar, results.PartsPerHour);
        WriteIfExists(state, state.ExpectedPartCountVar, results.ExpectedPartCount);
        WriteIfExists(state, state.HoursPerShiftVar, results.HoursPerShift);
        WriteIfExists(state, state.DowntimeFormattedVar, results.DowntimeFormatted);
        WriteIfExists(state, state.TotalRuntimeFormattedVar, results.TotalRuntimeFormatted);
        WriteIfExists(state, state.CurrentShiftNumberVar, results.CurrentShiftNumber);
        WriteIfExists(state, state.ShiftStartTimeOutputVar, results.ShiftStartTimeOutput);
        WriteIfExists(state, state.ShiftEndTimeVar, results.ShiftEndTime);
        WriteIfExists(state, state.TimeIntoShiftVar, results.TimeIntoShift);
        WriteIfExists(state, state.TimeRemainingInShiftVar, results.TimeRemainingInShift);
        WriteIfExists(state, state.ShiftChangeOccurredVar, results.ShiftChangeOccurred);
        WriteIfExists(state, state.ShiftChangeImminentVar, results.ShiftChangeImminent);
        WriteIfExists(state, state.ProjectedTotalCountVar, results.ProjectedTotalCount);
        WriteIfExists(state, state.RemainingTimeAtCurrentRateVar, results.RemainingTimeAtCurrentRate);
        WriteIfExists(state, state.ProductionBehindScheduleVar, results.ProductionBehindSchedule);
        WriteIfExists(state, state.RequiredRateToTargetVar, results.RequiredRateToTarget);
        WriteIfExists(state, state.TargetVsActualPartsVar, results.TargetVsActualParts);
        WriteIfExists(state, state.ShiftProgressVar, results.ShiftProgress);
        WriteIfExists(state, state.ProductionProgressVar, results.ProductionProgress);
        WriteIfExists(state, state.SystemStatusVar, results.SystemStatus);
        WriteIfExists(state, state.CalculationValidVar, results.CalculationValid);
        WriteIfExists(state, state.DataQualityScoreVar, results.DataQualityScore);
        WriteIfExists(state, state.LastUpdateTimeVar, results.LastUpdateTime);
        WriteIfExists(state, state.QualityTrendVar, results.QualityTrend);
        WriteIfExists(state, state.PerformanceTrendVar, results.PerformanceTrend);
        WriteIfExists(state, state.AvailabilityTrendVar, results.AvailabilityTrend);
        WriteIfExists(state, state.OEETrendVar, results.OEETrend);
        WriteIfExists(state, state.MinQualityVar, results.MinQuality);
        WriteIfExists(state, state.MaxQualityVar, results.MaxQuality);
        WriteIfExists(state, state.AvgQualityVar, results.AvgQuality);
        WriteIfExists(state, state.MinPerformanceVar, results.MinPerformance);
        WriteIfExists(state, state.MaxPerformanceVar, results.MaxPerformance);
        WriteIfExists(state, state.AvgPerformanceVar, results.AvgPerformance);
        WriteIfExists(state, state.MinAvailabilityVar, results.MinAvailability);
        WriteIfExists(state, state.MaxAvailabilityVar, results.MaxAvailability);
        WriteIfExists(state, state.AvgAvailabilityVar, results.AvgAvailability);
        WriteIfExists(state, state.MinOEEVar, results.MinOEE);
        WriteIfExists(state, state.MaxOEEVar, results.MaxOEE);
        WriteIfExists(state, state.AvgOEEVar, results.AvgOEE);
        WriteIfExists(state, state.QualityVsTargetVar, results.QualityVsTarget);
        WriteIfExists(state, state.PerformanceVsTargetVar, results.PerformanceVsTarget);
        WriteIfExists(state, state.AvailabilityVsTargetVar, results.AvailabilityVsTarget);
        WriteIfExists(state, state.OEEVsTargetVar, results.OEEVsTarget);
    }

    private void WriteIfExists(InstanceState state, IUAVariable var, object value)
    {
        if (!state.OutputPresenceFlags.ContainsKey(var) || !state.OutputPresenceFlags[var] || var == null) return;

        if (state.LastWrittenValues.TryGetValue(var, out object lastValue))
        {
            if (ValuesAreEqual(lastValue, value)) return;
        }
        
        if (TrySetValueWithCooldown(state, var, value))
        {
            state.LastWrittenValues[var] = value;
        }
    }

    private bool ValuesAreEqual(object value1, object value2)
    {
        if (value1 == null && value2 == null) return true;
        if (value1 == null || value2 == null) return false;

        if (value1 is double d1 && value2 is double d2)
        {
            return Math.Abs(d1 - d2) < 0.001;
        }

        return object.Equals(value1, value2);
    }

    private bool TrySetValueWithCooldown(InstanceState state, IUAVariable var, object value)
    {
        if (var == null) return false;
        if (!state.OutputPresenceFlags.ContainsKey(var)) return false;

        if (state.LastWriteFailureUtc.TryGetValue(var, out DateTime lastFailure))
        {
            if ((DateTime.UtcNow - lastFailure) < WriteRetryCooldown)
                return false;
        }

        try
        {
            var.SetValue(value);
            state.LastWriteFailureUtc.Remove(var);
            return true;
        }
        catch
        {
            state.LastWriteFailureUtc[var] = DateTime.UtcNow;
            return false;
        }
    }

    private double CalculateDataQuality(InstanceState state, CalculationResults results)
    {
        double score = 100.0;
        if (results.TotalCount == 0) score -= 10;
        if (results.RuntimeSeconds <= 0) score -= 10;
        if (results.ProductionBehindSchedule) score -= 5;
        if (results.OEE < state.PoorOEEThreshold) score -= 10;
        return Math.Max(0, score);
    }

    #endregion

    #region Helper Methods

    private object GetUnderlyingValue(IUAVariable var)
    {
        if (var == null) return null;
        var v = var.Value;
        if (v == null) return null;
        return v.Value;
    }

    private int ReadIntVar(IUAVariable var, int fallback)
    {
        var o = GetUnderlyingValue(var);
        if (o == null) return fallback;
        if (o is int i) return i;
        if (o is long l) return (int)l;
        if (int.TryParse(o.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)) return parsed;
        return fallback;
    }

    private double ReadDoubleVar(IUAVariable var, double fallback)
    {
        var o = GetUnderlyingValue(var);
        if (o == null) return fallback;
        if (o is double d) return d;
        if (o is float f) return f;
        if (o is int i) return i;
        if (o is long l) return l;
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

    private DateTime ReadDateTimeVar(IUAVariable var, DateTime fallback)
    {
        var o = GetUnderlyingValue(var);
        if (o == null) return fallback;
        if (o is DateTime dt) return dt;
        // Some nodes may store as string; try parse
        if (DateTime.TryParse(o.ToString(), out var parsed)) return parsed;
        // If numeric seconds since midnight are provided
        if (double.TryParse(o.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
        {
            var today = DateTime.Today.AddSeconds(Math.Max(0, Math.Min(86400, seconds)));
            return today;
        }
        return fallback;
    }

    private bool TryGetDoubleFromRaw(object raw, out double result)
    {
        result = 0.0;
        if (raw == null) return false;

        if (raw is double d) { result = d; return true; }
        if (raw is float f) { result = f; return true; }
        if (raw is int i) { result = i; return true; }
        if (raw is long l) { result = l; return true; }
        if (raw is uint ui) { result = ui; return true; }
        if (raw is short s) { result = s; return true; }
        if (raw is ushort us) { result = us; return true; }
        if (raw is byte by) { result = by; return true; }
        if (raw is sbyte sb) { result = sb; return true; }
        
        if (double.TryParse(raw.ToString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double parsed))
        {
            result = parsed;
            return true;
        }

        return false;
    }

    private string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalDays >= 1)
        {
            return $"{(int)timeSpan.TotalHours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
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

    private void LogError(string message)
    {
        Log.Error("AdvanceOEE_Calculator", message);
    }

    private void LogInfo(string message)
    {
        Log.Info("AdvanceOEE_Calculator", message);
    }

    private void LogWarning(string message)
    {
        Log.Warning("AdvanceOEE_Calculator", message);
    }

    #endregion

    #region Exported Methods

    [ExportMethod]
    public void ForceCalculation()
    {
        try
        {
            ProcessAllInstances();
            LogInfo("Force calculation completed");
        }
        catch (Exception ex)
        {
            LogError($"Force calculation error: {ex.Message}");
        }
    }

    [ExportMethod]
    public void GetInstanceInfo()
    {
        try
        {
            LogInfo($"=== OEE CALCULATOR STATUS ===");
            LogInfo($"Folder: {_oeeInstancesFolder?.BrowseName ?? "NULL"}");
            LogInfo($"Instances found: {_oeeInstances.Count}");
            
            foreach (var instance in _oeeInstances)
            {
                if (_instanceStates.TryGetValue(instance.NodeId, out var state))
                {
                    LogInfo($"[{instance.BrowseName}] Variables: {state.InitializedVariableCount}, Trending history: {state.OEEHistory.Count}/{MaxHistorySize}");
                }
            }
            LogInfo($"=== END STATUS ===");
        }
        catch (Exception ex)
        {
            LogError($"GetInstanceInfo error: {ex.Message}");
        }
    }

    #endregion

    #region Data Classes

    private class InstanceState
    {
        public IUANode Instance;
        public string InstanceName;
        public int InitializedVariableCount;

        // Input variables - Data
        public IUAVariable TotalRuntimeSecondsVar;
        public IUAVariable GoodPartCountVar;
        public IUAVariable BadPartCountVar;
        
        // Input variables - Production
        public IUAVariable IdealCycleTimeSecondsVar;
        public IUAVariable PlannedProductionTimeHoursVar;
        public IUAVariable NumberOfShiftsVar;
        public IUAVariable ShiftStartTimeVar;
        public IUAVariable ProductionTargetVar;

        // Input variables - System
        public IUAVariable UpdateRateMsVar;
        public IUAVariable LoggingVerbosityVar;

        // Input variables - Targets
        public IUAVariable QualityTargetVar;
        public IUAVariable PerformanceTargetVar;
        public IUAVariable AvailabilityTargetVar;
        public IUAVariable OEETargetVar;
        
        // Configuration variables
        public IUAVariable EnableRealTimeCalcVar;
        public IUAVariable MinimumRunTimeVar;
        public IUAVariable GoodOEEThresholdVar;
        public IUAVariable PoorOEEThresholdVar;
        public IUAVariable EnableLoggingVar;
        public IUAVariable EnableAlarmsVar;
        public IUAVariable SystemHealthyVar;

        // Output variables - Core
        public IUAVariable TotalCountVar;
        public IUAVariable QualityVar;
        public IUAVariable PerformanceVar;
        public IUAVariable AvailabilityVar;
        public IUAVariable OEEVar;
        public IUAVariable AvgCycleTimeVar;
        public IUAVariable PartsPerHourVar;
        public IUAVariable ExpectedPartCountVar;

        // Output variables - Shift
        public IUAVariable HoursPerShiftVar;
        public IUAVariable CurrentShiftNumberVar;
        public IUAVariable ShiftStartTimeOutputVar;
        public IUAVariable ShiftEndTimeVar;
        public IUAVariable TimeIntoShiftVar;
        public IUAVariable TimeRemainingInShiftVar;
        public IUAVariable ShiftChangeOccurredVar;
        public IUAVariable ShiftChangeImminentVar;
        public IUAVariable ShiftProgressVar;

        // Output variables - Production
        public IUAVariable ProjectedTotalCountVar;
        public IUAVariable RemainingTimeAtCurrentRateVar;
        public IUAVariable ProductionBehindScheduleVar;
        public IUAVariable RequiredRateToTargetVar;
        public IUAVariable TargetVsActualPartsVar;
        public IUAVariable ProductionProgressVar;

        // Output variables - System
        public IUAVariable SystemStatusVar;
        public IUAVariable CalculationValidVar;
        public IUAVariable DataQualityScoreVar;
        public IUAVariable LastUpdateTimeVar;
        public IUAVariable DowntimeFormattedVar;
        public IUAVariable TotalRuntimeFormattedVar;

        // Trending variables
        public IUAVariable QualityTrendVar;
        public IUAVariable PerformanceTrendVar;
        public IUAVariable AvailabilityTrendVar;
        public IUAVariable OEETrendVar;

        // Statistics variables
        public IUAVariable MinQualityVar;
        public IUAVariable MaxQualityVar;
        public IUAVariable AvgQualityVar;
        public IUAVariable MinPerformanceVar;
        public IUAVariable MaxPerformanceVar;
        public IUAVariable AvgPerformanceVar;
        public IUAVariable MinAvailabilityVar;
        public IUAVariable MaxAvailabilityVar;
        public IUAVariable AvgAvailabilityVar;
        public IUAVariable MinOEEVar;
        public IUAVariable MaxOEEVar;
        public IUAVariable AvgOEEVar;

        // Target comparison variables
        public IUAVariable QualityVsTargetVar;
        public IUAVariable PerformanceVsTargetVar;
        public IUAVariable AvailabilityVsTargetVar;
        public IUAVariable OEEVsTargetVar;

        // Configuration dictionaries
        public Dictionary<IUAVariable, bool> OutputPresenceFlags = new Dictionary<IUAVariable, bool>();
        public Dictionary<IUAVariable, DateTime> LastWriteFailureUtc = new Dictionary<IUAVariable, DateTime>();
        public Dictionary<IUAVariable, object> LastWrittenValues = new Dictionary<IUAVariable, object>();

        // Caching
        public object CachedIdealRaw = null;
        public double CachedIdealSeconds = 0.0;
        public bool CachedIdealValid = false;
        public object CachedPlannedRaw = null;
        public double CachedPlannedHours = double.NaN;
        public bool CachedPlannedValid = false;

        // Formatted strings cache
        public string LastTotalRuntimeFormatted = "";
        public double LastTotalRuntimeSeconds = -1;
        public string LastDowntimeFormatted = "";
        public double LastDowntimeSeconds = -1;

        // Trending data
        public Queue<double> QualityHistory = new Queue<double>();
        public Queue<double> PerformanceHistory = new Queue<double>();
        public Queue<double> AvailabilityHistory = new Queue<double>();
        public Queue<double> OEEHistory = new Queue<double>();

        // Configuration values
        public double QualityTarget = 95.0;
        public double PerformanceTarget = 85.0;
        public double AvailabilityTarget = 90.0;
        public double OEETarget = 72.7;
        public int ProductionTarget = 1000;
        public int NumberOfShifts = 3;
        public DateTime ShiftStartDateTime = DateTime.Today.AddHours(6);

        // Shift tracking
        public DateTime LastShiftCalculation = DateTime.MinValue;
        public int CurrentShiftNumber = 1;
        public DateTime CurrentShiftStart = DateTime.Today.AddHours(6);
        public DateTime CurrentShiftEnd = DateTime.Today.AddHours(14);
        public bool ShiftChangeOccurred = false;
        public bool ShiftChangeImminent = false;
        public double CalculatedHoursPerShift = 8.0;

        // Configuration flags
        public bool EnableRealTimeCalc = true;
        public double MinimumRunTime = 60.0;
        public double GoodOEEThreshold = 80.0;
        public double PoorOEEThreshold = 60.0;
        public bool EnableLogging = true;
        public bool EnableAlarms = true;
        public bool SystemHealthy = true;

        // State tracking
        public double PreviousRuntimeSeconds = -1.0;

        // Settings
        public int UpdateRateMs = 1000;
        public int LoggingVerbosity = 1;
        public bool DefaultsInitialized = false;
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
        public double ShiftProgress { get; set; }
        public double ProductionProgress { get; set; }
        public string SystemStatus { get; set; } = "Starting";
        public bool CalculationValid { get; set; } = true;
        public double DataQualityScore { get; set; } = 100.0;
        public string LastUpdateTime { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        public string QualityTrend { get; set; } = "Insufficient Data";
        public string PerformanceTrend { get; set; } = "Insufficient Data";
        public string AvailabilityTrend { get; set; } = "Insufficient Data";
        public string OEETrend { get; set; } = "Insufficient Data";

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

        public double QualityVsTarget { get; set; }
        public double PerformanceVsTarget { get; set; }
        public double AvailabilityVsTarget { get; set; }
        public double OEEVsTarget { get; set; }
    }

    #endregion
}
