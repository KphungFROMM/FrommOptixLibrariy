using System;
using System.Threading;
using System.Threading.Tasks;
using UAManagedCore;
using FTOptix.NetLogic;

/// <summary>
/// MachineSimulator NetLogic
/// - Simulates a machine producing parts (good and bad) based on:
///     * a single cycle time control (seconds per part)
///     * a performance rate (%) that scales production speed
///     * a quality / GoodPct (%) that determines if a produced part is good or bad
/// - Controls:
///     StartMachine (Bool)  -> when true the simulator runs; when false it stops
///     CycleTimeSeconds     (Double) - effective cycle time per part in seconds (single control)
///     PerformanceRatePct   (Double) - performance percentage (100 = ideal speed). 0 disables production.
///     QualityPct           (Double) - percent chance a produced part is good (0..100)
///     ResetCounters        (Bool)   - when true resets counts; simulator pulses ResetDone true once
///
/// - Outputs this logic writes (create if you want to consume them):
///     Running               (Bool)     - true while simulator is running
///     GoodPartPulse         (Bool)     - true for one loop when at least one good part produced this interval
///     BadPartPulse          (Bool)     - true for one loop when at least one bad part produced this interval
///     TotalGoodCount        (Int32)    - cumulative good parts produced
///     TotalBadCount         (Int32)    - cumulative bad parts produced
///     TotalCount            (Int32)    - cumulative total parts produced
///     PartsPerHour          (Double)   - instantaneous parts/hour based on current settings (est)
///     LastPartTimestamp     (DateTime) - last produced part timestamp (local DateTime.Now)
///     ResetDone             (Bool)     - one-loop pulse true when counters were reset
///
/// Implementation notes:
/// - Single cycle control replaces previous Ideal/Override pair for simplicity and direct control.
/// - The simulator uses a 100ms loop and accumulates elapsed time to decide when to emit parts (CPU friendly).
/// - Production formula:
///     partsPerSecond = (PerformanceRatePct / 100.0) / cycleSeconds
///     interArrivalSec = cycleSeconds * 100 / PerformanceRatePct
/// - If PerformanceRatePct <= 0 production is paused.
/// </summary>
public class MachineSimulator : BaseNetLogic
{
    // Inputs
    private IUAVariable StartMachineVar;
    private IUAVariable CycleTimeSecondsVar;       // Single control for cycle time
    private IUAVariable PerformanceRatePctVar;
    private IUAVariable QualityPctVar;
    private IUAVariable ResetCountersVar;

    // Outputs
    private IUAVariable RunningVar;
    private IUAVariable GoodPartPulseVar;
    private IUAVariable BadPartPulseVar;
    private IUAVariable TotalGoodCountVar;
    private IUAVariable TotalBadCountVar;
    private IUAVariable TotalCountVar;
    private IUAVariable PartsPerHourVar;
    private IUAVariable LastPartTimestampVar;
    private IUAVariable ResetDoneVar;

    // presence flags
    private bool _hasRunningVar;
    private bool _hasGoodPartPulseVar;
    private bool _hasBadPartPulseVar;
    private bool _hasTotalGoodCountVar;
    private bool _hasTotalBadCountVar;
    private bool _hasTotalCountVar;
    private bool _hasPartsPerHourVar;
    private bool _hasLastPartTimestampVar;
    private bool _hasResetDoneVar;

    // runtime
    private CancellationTokenSource _cts;
    private Task _loopTask;

    // state
    private long totalGood = 0;
    private long totalBad = 0;
    private long totalAll = 0;
    private readonly Random _rand = new Random();

    // timing
    private const int LoopMs = 100; // 100ms loop - light CPU
    private double elapsedAccumulatorSec = 0.0;

    public override void Start()
    {
        // Inputs
        StartMachineVar = LogicObject.GetVariable("StartMachine");
        CycleTimeSecondsVar = LogicObject.GetVariable("CycleTimeSeconds");
        PerformanceRatePctVar = LogicObject.GetVariable("PerformanceRatePct");
        QualityPctVar = LogicObject.GetVariable("QualityPct");
        ResetCountersVar = LogicObject.GetVariable("ResetCounters");

        // Outputs
        RunningVar = LogicObject.GetVariable("Running");
        GoodPartPulseVar = LogicObject.GetVariable("GoodPartPulse");
        BadPartPulseVar = LogicObject.GetVariable("BadPartPulse");
        TotalGoodCountVar = LogicObject.GetVariable("TotalGoodCount");
        TotalBadCountVar = LogicObject.GetVariable("TotalBadCount");
        TotalCountVar = LogicObject.GetVariable("TotalCount");
        PartsPerHourVar = LogicObject.GetVariable("PartsPerHour");
        LastPartTimestampVar = LogicObject.GetVariable("LastPartTimestamp");
        ResetDoneVar = LogicObject.GetVariable("ResetDone");

        // presence flags
        _hasRunningVar = RunningVar != null;
        _hasGoodPartPulseVar = GoodPartPulseVar != null;
        _hasBadPartPulseVar = BadPartPulseVar != null;
        _hasTotalGoodCountVar = TotalGoodCountVar != null;
        _hasTotalBadCountVar = TotalBadCountVar != null;
        _hasTotalCountVar = TotalCountVar != null;
        _hasPartsPerHourVar = PartsPerHourVar != null;
        _hasLastPartTimestampVar = LastPartTimestampVar != null;
        _hasResetDoneVar = ResetDoneVar != null;

        // initialize counters from existing nodes if present (best-effort)
        if (_hasTotalGoodCountVar)
        {
            var u = GetUnderlyingValue(TotalGoodCountVar);
            if (u is int i) totalGood = i;
            else if (u is long l) totalGood = l;
        }
        if (_hasTotalBadCountVar)
        {
            var u = GetUnderlyingValue(TotalBadCountVar);
            if (u is int i) totalBad = i;
            else if (u is long l) totalBad = l;
        }
        totalAll = totalGood + totalBad;

        _cts = new CancellationTokenSource();
        _loopTask = Task.Run(() => RunAsyncLoop(_cts.Token));
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
        catch { /* ignore */ }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            _loopTask = null;
        }
    }

    private async Task RunAsyncLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(LoopMs, token);

                // read control inputs once per loop (cheap unwrap)
                bool startCmd = ReadBoolVar(StartMachineVar, false);
                double cycleSeconds = ReadDoubleVar(CycleTimeSecondsVar, 1.0); // single control
                double performancePct = ReadDoubleVar(PerformanceRatePctVar, 100.0);
                double qualityPct = ReadDoubleVar(QualityPctVar, 100.0);
                bool resetCmd = ReadBoolVar(ResetCountersVar, false);

                // handle reset request (one-loop pulse)
                if (resetCmd)
                {
                    totalGood = 0;
                    totalBad = 0;
                    totalAll = 0;
                    elapsedAccumulatorSec = 0.0;
                    // write counters and pulse ResetDone
                    WriteIfExists(TotalGoodCountVar, (int)totalGood, ref _hasTotalGoodCountVar);
                    WriteIfExists(TotalBadCountVar, (int)totalBad, ref _hasTotalBadCountVar);
                    WriteIfExists(TotalCountVar, (int)totalAll, ref _hasTotalCountVar);
                    if (_hasResetDoneVar) { TrySetValue(ResetDoneVar, true); } // will clear below
                }
                else
                {
                    // ensure ResetDone cleared if present
                    if (_hasResetDoneVar) TrySetValue(ResetDoneVar, false);
                }

                // update Running output
                bool running = startCmd;
                if (_hasRunningVar) TrySetValue(RunningVar, running);

                // If not running, clear pulses and continue
                if (!running)
                {
                    if (_hasGoodPartPulseVar) TrySetValue(GoodPartPulseVar, false);
                    if (_hasBadPartPulseVar) TrySetValue(BadPartPulseVar, false);
                    // update counts outputs
                    WriteIfExists(TotalGoodCountVar, (int)totalGood, ref _hasTotalGoodCountVar);
                    WriteIfExists(TotalBadCountVar, (int)totalBad, ref _hasTotalBadCountVar);
                    WriteIfExists(TotalCountVar, (int)totalAll, ref _hasTotalCountVar);
                    elapsedAccumulatorSec = 0.0;
                    continue;
                }

                // Running: compute effective cycle from the single control, ensure positive
                double effectiveCycle = Math.Max(1e-6, cycleSeconds);

                // If performancePct <= 0 we produce nothing
                if (!(performancePct > 0.0))
                {
                    if (_hasPartsPerHourVar) TrySetValue(PartsPerHourVar, 0.0);
                    if (_hasGoodPartPulseVar) TrySetValue(GoodPartPulseVar, false);
                    if (_hasBadPartPulseVar) TrySetValue(BadPartPulseVar, false);
                    continue;
                }

                // Compute inter-arrival:
                // partsPerSecond = (performancePct/100) / effectiveCycle
                // interArrivalSec = effectiveCycle * 100 / performancePct
                double interArrivalSec = effectiveCycle * 100.0 / performancePct;
                if (interArrivalSec <= 0.0) interArrivalSec = 1e-6;

                // Update partsPerHour estimate
                double partsPerHour = 3600.0 / interArrivalSec;
                if (_hasPartsPerHourVar) TrySetValue(PartsPerHourVar, partsPerHour);

                // advance accumulator by loop interval
                elapsedAccumulatorSec += LoopMs / 1000.0;

                int goodThisLoop = 0;
                int badThisLoop = 0;

                // produce as many parts as fit in the accumulator
                while (elapsedAccumulatorSec + 1e-12 >= interArrivalSec)
                {
                    elapsedAccumulatorSec -= interArrivalSec;

                    // determine good or bad via qualityPct
                    double r = _rand.NextDouble() * 100.0;
                    bool isGood = r <= qualityPct;

                    if (isGood)
                    {
                        totalGood++;
                        goodThisLoop++;
                    }
                    else
                    {
                        totalBad++;
                        badThisLoop++;
                    }

                    totalAll++;
                    if (_hasLastPartTimestampVar) TrySetValue(LastPartTimestampVar, DateTime.Now);
                }

                // Write pulses: true if any occurred this loop
                if (_hasGoodPartPulseVar) TrySetValue(GoodPartPulseVar, goodThisLoop > 0);
                if (_hasBadPartPulseVar) TrySetValue(BadPartPulseVar, badThisLoop > 0);

                // Write cumulative counters
                WriteIfExists(TotalGoodCountVar, (int)totalGood, ref _hasTotalGoodCountVar);
                WriteIfExists(TotalBadCountVar, (int)totalBad, ref _hasTotalBadCountVar);
                WriteIfExists(TotalCountVar, (int)totalAll, ref _hasTotalCountVar);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Error("MachineSimulator", $"Run loop error: {ex.Message}");
            }
        }
    }

    // Helper wrappers

    // TrySetValue returns true on success, false on failure (does not throw)
    private bool TrySetValue(IUAVariable var, object value)
    {
        if (var == null) return false;
        try
        {
            var.SetValue(value);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error("MachineSimulator", $"Write failed for variable (ToString()='{SafeVarToString(var)}') value='{value}' ({value?.GetType().Name}): {ex.Message}");
            return false;
        }
    }

    // WriteIfExists: write value if var exists and handle presence flag
    private void WriteIfExists(IUAVariable var, object value, ref bool presenceFlag)
    {
        if (!presenceFlag || var == null) return;
        if (!TrySetValue(var, value))
            presenceFlag = false;
    }

    // Unwrap UAValue where necessary
    private object GetUnderlyingValue(IUAVariable var)
    {
        if (var == null) return null;
        var v = var.Value;
        if (v == null) return null;
        return v is UAValue ua ? ua.Value : v;
    }

    private string SafeVarToString(IUAVariable var)
    {
        if (var == null) return "null";
        try { return var.ToString() ?? "iuavariable"; }
        catch { return "iuavariable"; }
    }

    // Simple typed readers with fallbacks
    private bool ReadBoolVar(IUAVariable var, bool fallback)
    {
        var o = GetUnderlyingValue(var);
        if (o == null) return fallback;
        if (o is bool b) return b;
        if (o is int i) return i != 0;
        if (o is long l) return l != 0;
        if (bool.TryParse(o.ToString(), out bool parsed)) return parsed;
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
        if (double.TryParse(o.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double parsed))
            return parsed;
        return fallback;
    }
}
