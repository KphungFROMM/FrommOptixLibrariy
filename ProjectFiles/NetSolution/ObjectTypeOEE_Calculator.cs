using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UAManagedCore;
using FTOptix.NetLogic;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.DataLogger;

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
public class ObjectTypeOEE_Calculator : BaseNetLogic
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
    private IUAVariable UpdateRateMsVar;
    private IUAVariable QualityTargetVar;
    private IUAVariable PerformanceTargetVar;
    private IUAVariable AvailabilityTargetVar;
    private IUAVariable OEETargetVar;
    private IUAVariable LoggingVerbosityVar;

    // Output variables - resolved from ObjectType structure
    private IUAVariable QualityVar;
    private IUAVariable PerformanceVar;
    private IUAVariable AvailabilityVar;
    private IUAVariable OEEVar;
    private IUAVariable PartsPerHourVar;
    private IUAVariable ExpectedPartCountVar;
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

    // Runtime
    private CancellationTokenSource _cts;
    private Task _loopTask;

    // Configuration
    private readonly Dictionary<IUAVariable, bool> _outputPresenceFlags = new Dictionary<IUAVariable, bool>();
    private readonly TimeSpan WriteRetryCooldown = TimeSpan.FromSeconds(30);
    private readonly Dictionary<IUAVariable, DateTime> _lastWriteFailureUtc = new Dictionary<IUAVariable, DateTime>();

    // Cached values
    private object _cachedIdealRaw = null;
    private double _cachedIdealSeconds = 0.0;
    private bool _cachedIdealValid = false;

    private object _cachedPlannedRaw = null;
    private double _cachedPlannedHours = double.NaN;
    private bool _cachedPlannedValid = false;

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
        InitializeDefaultValues();
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
        
        // Input mappings with fallbacks for different naming conventions
        TotalRuntimeSecondsVar = GetVariableFromPath("Inputs/Production/ActualProductionTime") ?? 
                               GetVariableFromPath("Inputs/Production/PlannedProductionTime") ??
                               GetVariableFromPath("TotalRuntimeSeconds");
                               
        GoodPartCountVar = GetVariableFromPath("Inputs/Quality/GoodUnitsProduced") ?? 
                          GetVariableFromPath("GoodPartCount");
                          
        BadPartCountVar = GetVariableFromPath("Inputs/Quality/DefectiveUnits") ?? 
                         GetVariableFromPath("BadPartCount");
                         
        IdealCycleTimeSecondsVar = GetVariableFromPath("Inputs/Performance/IdealCycleTime") ?? 
                                  GetVariableFromPath("IdealCycleTimeSeconds");
                                  
        PlannedProductionTimeHoursVar = GetVariableFromPath("Inputs/Production/PlannedProductionTime") ?? 
                                       GetVariableFromPath("PlannedProductionTimeHours");

        // Configuration mappings
        UpdateRateMsVar = GetVariableFromPath("Configuration/Calculation/CalculationInterval") ?? 
                         GetVariableFromPath("Configuration/UpdateRateMs");
                         
        QualityTargetVar = GetVariableFromPath("Configuration/Thresholds/TargetOEE") ?? 
                          GetVariableFromPath("QualityTarget");
                          
        PerformanceTargetVar = GetVariableFromPath("Configuration/Thresholds/PerformanceTarget") ?? 
                              GetVariableFromPath("PerformanceTarget");
                              
        AvailabilityTargetVar = GetVariableFromPath("Configuration/Thresholds/AvailabilityTarget") ?? 
                               GetVariableFromPath("AvailabilityTarget");
                               
        OEETargetVar = GetVariableFromPath("Configuration/Thresholds/TargetOEE") ?? 
                      GetVariableFromPath("OEETarget");
                      
        LoggingVerbosityVar = GetVariableFromPath("Configuration/System/LoggingVerbosity") ?? 
                             GetVariableFromPath("LoggingVerbosity");

        // Output mappings
        QualityVar = GetVariableFromPath("Outputs/Metrics/Quality");
        PerformanceVar = GetVariableFromPath("Outputs/Metrics/Performance");
        AvailabilityVar = GetVariableFromPath("Outputs/Metrics/Availability");
        OEEVar = GetVariableFromPath("Outputs/Metrics/OEE_Overall");
        PartsPerHourVar = GetVariableFromPath("Outputs/Operational/ProductionRate");
        ExpectedPartCountVar = GetVariableFromPath("Outputs/ExpectedPartCount");
        SystemStatusVar = GetVariableFromPath("Configuration/System/SystemHealthy") ?? 
                         GetVariableFromPath("Outputs/SystemStatus");
        CalculationValidVar = GetVariableFromPath("Outputs/CalculationValid");
        DataQualityScoreVar = GetVariableFromPath("Outputs/DataQualityScore");
        LastUpdateTimeVar = GetVariableFromPath("Outputs/LastUpdateTime");

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
        QualityVsTargetVar = GetVariableFromPath("Outputs/Comparisons/QualityVsTarget");
        PerformanceVsTargetVar = GetVariableFromPath("Outputs/Comparisons/PerformanceVsTarget");
        AvailabilityVsTargetVar = GetVariableFromPath("Outputs/Comparisons/AvailabilityVsTarget");
        OEEVsTargetVar = GetVariableFromPath("Outputs/Comparisons/OEEVsTarget");

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
            PlannedProductionTimeHoursVar, QualityVar, PerformanceVar, AvailabilityVar,
            OEEVar, PartsPerHourVar, SystemStatusVar, CalculationValidVar, DataQualityScoreVar
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
            QualityVar, PerformanceVar, AvailabilityVar, OEEVar, PartsPerHourVar,
            SystemStatusVar, CalculationValidVar, DataQualityScoreVar, LastUpdateTimeVar,
            QualityTrendVar, PerformanceTrendVar, AvailabilityTrendVar, OEETrendVar,
            MinQualityVar, MaxQualityVar, AvgQualityVar,
            MinPerformanceVar, MaxPerformanceVar, AvgPerformanceVar,
            MinAvailabilityVar, MaxAvailabilityVar, AvgAvailabilityVar,
            MinOEEVar, MaxOEEVar, AvgOEEVar,
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

    private void InitializeDefaultValues()
    {
        if (_defaultsInitialized) return;
        
        SetDefaultValue(TotalRuntimeSecondsVar, 0.0, "TotalRuntimeSeconds");
        SetDefaultValue(GoodPartCountVar, 0, "GoodPartCount");
        SetDefaultValue(BadPartCountVar, 0, "BadPartCount");
        SetDefaultValue(IdealCycleTimeSecondsVar, 30.0, "IdealCycleTimeSeconds");
        SetDefaultValue(PlannedProductionTimeHoursVar, 8.0, "PlannedProductionTimeHours");
        
        _defaultsInitialized = true;
    }

    private void SetDefaultValue(IUAVariable var, object defaultValue, string varName)
    {
        if (var == null) return;
        
        try
        {
            var currentValue = var.Value?.Value;
            if (IsValueInvalid(currentValue, defaultValue))
            {
                var.SetValue(defaultValue);
                LogInfo($"Set default value for {varName}: {defaultValue}");
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
        
        if (defaultValue is double)
        {
            if (value is double d && (double.IsNaN(d) || double.IsInfinity(d))) return true;
            if (value is double d2 && d2 <= 0 && defaultValue.ToString().Contains("30")) return true;
        }
        
        if (defaultValue is string && string.IsNullOrWhiteSpace(value?.ToString())) return true;
        if (defaultValue is int && value is int i && i < 0) return true;
        
        return false;
    }

    private void ReadConfiguration()
    {
        if (UpdateRateMsVar != null && ReadIntVar(UpdateRateMsVar, -1) is int u && u > 0)
            _updateRateMs = u;

        _qualityTarget = ReadDoubleVar(QualityTargetVar, 95.0);
        _performanceTarget = ReadDoubleVar(PerformanceTargetVar, 85.0);
        _availabilityTarget = ReadDoubleVar(AvailabilityTargetVar, 90.0);
        _oeeTarget = ReadDoubleVar(OEETargetVar, 72.7);

        if (LoggingVerbosityVar != null)
            _loggingVerbosity = ReadIntVar(LoggingVerbosityVar, 1);
    }

    private async Task RunLoopAsync(CancellationToken token)
    {
        int loopCount = 0;
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_updateRateMs, token);

                if (loopCount % 30 == 0)
                {
                    ReadConfiguration();
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
        
        if (plannedSeconds > 0.0 && idealCycle > 0.0)
            results.ExpectedPartCount = plannedSeconds / idealCycle;
        else
            results.ExpectedPartCount = 480.0;

        // Target comparisons
        results.QualityVsTarget = results.Quality - _qualityTarget;
        results.PerformanceVsTarget = results.Performance - _performanceTarget;
        results.AvailabilityVsTarget = results.Availability - _availabilityTarget;
        results.OEEVsTarget = results.OEE - _oeeTarget;

        // System status
        results.SystemStatus = DetermineSystemStatus(results);
        results.CalculationValid = true;
        results.DataQualityScore = 100.0;
        results.LastUpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        return results;
    }

    private string DetermineSystemStatus(CalculationResults results)
    {
        if (results.RuntimeSeconds > 0.0) return "Running";
        return "Stopped";
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

        // Calculate trends and statistics
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
            if (TryGetSecondsFromRaw(idealRaw, out double sec))
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
            return _cachedPlannedHours * 3600.0;
        }

        return 28800.0; // Default: 8 hours
    }

    private void WriteAllOutputs(CalculationResults results)
    {
        WriteIfExists(QualityVar, results.Quality);
        WriteIfExists(PerformanceVar, results.Performance);
        WriteIfExists(AvailabilityVar, results.Availability);
        WriteIfExists(OEEVar, results.OEE);
        WriteIfExists(PartsPerHourVar, results.PartsPerHour);
        WriteIfExists(ExpectedPartCountVar, results.ExpectedPartCount);
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
        TrySetValueWithCooldown(var, value);
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

    private bool TryGetSecondsFromRaw(object raw, out double seconds)
    {
        seconds = 0.0;
        if (raw == null) return false;
        try
        {
            if (raw is double dv) { seconds = dv; return seconds > 0.0; }
            if (raw is float fv) { seconds = Convert.ToDouble(fv); return seconds > 0.0; }
            if (raw is int iv) { seconds = Convert.ToDouble(iv); return seconds > 0.0; }
            if (double.TryParse(raw.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedD))
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
            if (double.TryParse(raw.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedD))
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
        public double PartsPerHour { get; set; }
        public double ExpectedPartCount { get; set; }
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
