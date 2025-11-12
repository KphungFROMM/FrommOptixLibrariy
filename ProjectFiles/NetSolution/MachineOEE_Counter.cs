using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using UAManagedCore;
using FTOptix.NetLogic;

/// <summary>
/// MachineOEE_Counter
/// - Counts GoodPartPulse / BadPartPulse (edge detect) and accumulates runtime from MachineRunningBit.
/// - Inputs (children): MachineRunningBit (Bool), GoodPartPulse (Bool), BadPartPulse (Bool), ResetCommand (Bool)
/// - Outputs (children): GoodPartCount (Int32), BadPartCount (Int32), TotalRuntimeSeconds (Double), TotalRuntimeFormatted (String)
/// - Keep this logic on the object that receives real pulses/running bit.
/// </summary>
public class MachineOEE_Counter : BaseNetLogic
{
    private IUAVariable MachineRunningBit;
    private IUAVariable GoodPartPulse;
    private IUAVariable BadPartPulse;
    private IUAVariable ResetCommand;

    private IUAVariable GoodPartCountVar;
    private IUAVariable BadPartCountVar;
    private IUAVariable TotalRuntimeSecondsVar;
    private IUAVariable TotalRuntimeFormattedVar;

    private CancellationTokenSource cts;

    // internal state
    private int goodPartCount = 0;
    private int badPartCount = 0;
    private double totalRuntimeSeconds = 0.0;

    private bool lastGood = false;
    private bool lastBad = false;
    private bool lastReset = false;
    private bool lastRunning = false;

    private Stopwatch runningStopwatch = new Stopwatch();

    // tunables
    private const int pulsePollMs = 50; // keep short to reliably catch pulses

    // presence flags (avoid repeated null checks)
    private bool hasGoodPartCountVar;
    private bool hasBadPartCountVar;
    private bool hasTotalRuntimeSecondsVar;
    private bool hasTotalRuntimeFormattedVar;

    public override void Start()
    {
        MachineRunningBit = LogicObject.GetVariable("MachineRunningBit");
        GoodPartPulse = LogicObject.GetVariable("GoodPartPulse");
        BadPartPulse = LogicObject.GetVariable("BadPartPulse");
        ResetCommand = LogicObject.GetVariable("ResetCommand");

        GoodPartCountVar = LogicObject.GetVariable("GoodPartCount");
        BadPartCountVar = LogicObject.GetVariable("BadPartCount");
        TotalRuntimeSecondsVar = LogicObject.GetVariable("TotalRuntimeSeconds");
        TotalRuntimeFormattedVar = LogicObject.GetVariable("TotalRuntimeFormatted");

        hasGoodPartCountVar = GoodPartCountVar != null;
        hasBadPartCountVar = BadPartCountVar != null;
        hasTotalRuntimeSecondsVar = TotalRuntimeSecondsVar != null;
        hasTotalRuntimeFormattedVar = TotalRuntimeFormattedVar != null;

        cts = new CancellationTokenSource();
        Task.Run(() => RunLoop(cts.Token));
    }

    public override void Stop()
    {
        try { cts?.Cancel(); } catch { }
    }

    private async Task RunLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(pulsePollMs, token);

                bool running = ReadBoolVar(MachineRunningBit, false);
                bool goodPulse = ReadBoolVar(GoodPartPulse, false);
                bool badPulse = ReadBoolVar(BadPartPulse, false);
                bool resetPulse = ReadBoolVar(ResetCommand, false);

                // Reset on rising edge
                if (resetPulse && !lastReset)
                {
                    goodPartCount = 0;
                    badPartCount = 0;
                    totalRuntimeSeconds = 0.0;
                    runningStopwatch.Reset();
                    if (running) runningStopwatch.Restart();

                    try { ResetCommand?.SetValue(false); } catch { }
                }
                lastReset = resetPulse;

                // runtime stopwatch management
                if (running)
                {
                    if (!lastRunning) runningStopwatch.Restart();
                }
                else
                {
                    if (lastRunning && runningStopwatch.IsRunning)
                    {
                        runningStopwatch.Stop();
                        totalRuntimeSeconds += runningStopwatch.Elapsed.TotalSeconds;
                        runningStopwatch.Reset();
                    }
                }

                // edge detect pulses
                if (goodPulse && !lastGood) goodPartCount++;
                if (badPulse && !lastBad) badPartCount++;

                lastGood = goodPulse;
                lastBad = badPulse;
                lastRunning = running;

                // current runtime (include running stopwatch)
                double currentTotal = totalRuntimeSeconds + (runningStopwatch.IsRunning ? runningStopwatch.Elapsed.TotalSeconds : 0.0);

                // write outputs with simple guarded writes
                if (hasGoodPartCountVar)
                {
                    try { GoodPartCountVar.SetValue(goodPartCount); }
                    catch { hasGoodPartCountVar = false; }
                }
                if (hasBadPartCountVar)
                {
                    try { BadPartCountVar.SetValue(badPartCount); }
                    catch { hasBadPartCountVar = false; }
                }
                if (hasTotalRuntimeSecondsVar)
                {
                    try { TotalRuntimeSecondsVar.SetValue(currentTotal); }
                    catch { hasTotalRuntimeSecondsVar = false; }
                }
                if (hasTotalRuntimeFormattedVar)
                {
                    try { TotalRuntimeFormattedVar.SetValue(FormatSeconds(currentTotal)); }
                    catch { hasTotalRuntimeFormattedVar = false; }
                }
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Error("MachineOEE_Counter", $"RunLoop error: {ex.Message}");
            }
        }
    }

    // Helpers (same safe unwrapping/parsing used in other logic)
    private object GetUnderlyingValue(IUAVariable var)
    {
        if (var == null || var.Value == null) return null;
        var raw = var.Value;
        if (raw is UAValue uaVal) return uaVal.Value;
        return raw;
    }

    private bool ReadBoolVar(IUAVariable var, bool fallback)
    {
        var vObj = GetUnderlyingValue(var);
        if (vObj == null) return fallback;
        try
        {
            if (vObj is bool b) return b;
            if (vObj is int i) return i != 0;
            if (vObj is long l) return l != 0;
            if (bool.TryParse(vObj.ToString(), out bool parsed)) return parsed;
        }
        catch { }
        return fallback;
    }

    private string FormatSeconds(double seconds)
    {
        if (double.IsNaN(seconds) || seconds <= 0) return "00:00:00";
        TimeSpan ts = TimeSpan.FromSeconds(seconds);
        return string.Format("{0:D2}:{1:D2}:{2:D2}",
            (int)ts.TotalHours, ts.Minutes, ts.Seconds);
    }
}
