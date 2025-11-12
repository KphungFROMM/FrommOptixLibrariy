using System;
using System.Threading;
using System.Threading.Tasks;
using UAManagedCore;
using FTOptix.NetLogic;

/// <summary>
/// ShiftCalculator NetLogic (simple, robust)
/// - Required children variables (create them as children on the NetLogic object):
///     NumberOfShifts      (Int32)
///     HoursPerShift       (Double or Int32)
///     FirstShiftStartTime (DateTime, TimeSpan, or String like "06:00" / "0600")
///     CurrentShift        (Int32)  <-- this logic writes the current 1-based shift number
/// - Optional children that this version writes (create them if you want these signals):
///     ShiftChangeImminent (Bool)   <-- true while <= ImminentThresholdSeconds remain in the shift
///     ShiftChangeOccurred (Bool)   <-- pulses true for one loop when a shift change is written
/// - Polls once per second and writes CurrentShift only on change; writes ShiftChangeImminent only when it toggles;
///   pulses ShiftChangeOccurred true on shift change and clears it next loop.
/// </summary>
public class ShiftCalculator : BaseNetLogic
{
    private IUAVariable NumberOfShiftsVar;
    private IUAVariable HoursPerShiftVar;
    private IUAVariable FirstShiftStartTimeVar;
    private IUAVariable CurrentShiftVar;
    private IUAVariable ShiftChangeImminentVar;
    private IUAVariable ShiftChangeOccurredVar;

    // cached presence flags to avoid repeated exceptions
    private bool hasCurrentShiftVar;
    private bool hasShiftChangeImminentVar;
    private bool hasShiftChangeOccurredVar;

    // loop control
    private CancellationTokenSource _cts;
    private Task _loopTask;

    // last-known state
    private int lastReportedShift = -1;
    private bool? lastImminent = null;
    private bool lastOccurred = false;

    // tunables
    private const int UpdateRateMs = 1000;
    private const int ImminentThresholdSeconds = 5;

    public override void Start()
    {
        NumberOfShiftsVar = LogicObject.GetVariable("NumberOfShifts");
        HoursPerShiftVar = LogicObject.GetVariable("HoursPerShift");
        FirstShiftStartTimeVar = LogicObject.GetVariable("FirstShiftStartTime");
        CurrentShiftVar = LogicObject.GetVariable("CurrentShift");
        ShiftChangeImminentVar = LogicObject.GetVariable("ShiftChangeImminent");
        ShiftChangeOccurredVar = LogicObject.GetVariable("ShiftChangeOccurred");

        hasCurrentShiftVar = CurrentShiftVar != null;
        hasShiftChangeImminentVar = ShiftChangeImminentVar != null;
        hasShiftChangeOccurredVar = ShiftChangeOccurredVar != null;

        // initialize lastReportedShift from existing CurrentShift to avoid an initial write
        if (hasCurrentShiftVar)
        {
            var u = GetUnderlyingValue(CurrentShiftVar);
            if (u is int iv) lastReportedShift = iv;
            else if (u is long lv) lastReportedShift = Convert.ToInt32(lv);
            else lastReportedShift = -1;
        }

        // initialize imminent/occurred trackers from existing nodes if present
        if (hasShiftChangeImminentVar)
        {
            var u = GetUnderlyingValue(ShiftChangeImminentVar);
            if (u is bool b) lastImminent = b;
            else lastImminent = null;
        }

        if (hasShiftChangeOccurredVar)
        {
            var u = GetUnderlyingValue(ShiftChangeOccurredVar);
            if (u is bool b) lastOccurred = b;
            else lastOccurred = false;
        }

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
        catch (Exception) { /* ignore */ }
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
            bool justSetOccurredThisLoop = false;

            try
            {
                await Task.Delay(UpdateRateMs, token);

                // Validate configuration values
                if (!TryGetIntFromVariable(NumberOfShiftsVar, out int numberOfShifts) ||
                    !TryGetDoubleFromVariable(HoursPerShiftVar, out double hoursPerShift) ||
                    numberOfShifts <= 0 || hoursPerShift <= 0)
                {
                    // invalid configuration -> write a cleared state only once
                    if (lastReportedShift != -1)
                    {
                        if (TryWriteCurrentShift(0)) lastReportedShift = -1;
                        else lastReportedShift = -1;
                    }

                    // clear imminent if previously set
                    if (lastImminent != null)
                    {
                        if (TryWriteShiftChangeImminent(false)) lastImminent = false;
                        else lastImminent = null;
                    }

                    // clear occurred if previously set
                    if (lastOccurred)
                    {
                        if (TryWriteShiftChangeOccurred(false)) lastOccurred = false;
                    }
                    continue;
                }

                if (!TryGetShiftStartTime(out TimeSpan shiftStart))
                {
                    if (lastReportedShift != -1)
                    {
                        if (TryWriteCurrentShift(0)) lastReportedShift = -1;
                        else lastReportedShift = -1;
                    }
                    if (lastImminent != null)
                    {
                        if (TryWriteShiftChangeImminent(false)) lastImminent = false;
                        else lastImminent = null;
                    }
                    if (lastOccurred)
                    {
                        if (TryWriteShiftChangeOccurred(false)) lastOccurred = false;
                    }
                    continue;
                }

                // Use local time-of-day for shift calculation
                TimeSpan nowTime = DateTime.Now.TimeOfDay;

                // Compute time since configured shift start-of-day, wrapping past midnight
                TimeSpan timeSinceStart = nowTime - shiftStart;
                if (timeSinceStart.TotalSeconds < 0)
                    timeSinceStart = timeSinceStart + TimeSpan.FromHours(24);

                // Compute cycle hours (hoursPerShift * numberOfShifts)
                double cycleHours = hoursPerShift * numberOfShifts;
                if (cycleHours <= 0)
                {
                    if (lastReportedShift != -1)
                    {
                        if (TryWriteCurrentShift(0)) lastReportedShift = -1;
                        else lastReportedShift = -1;
                    }
                    if (lastImminent != null)
                    {
                        if (TryWriteShiftChangeImminent(false)) lastImminent = false;
                        else lastImminent = null;
                    }
                    if (lastOccurred)
                    {
                        if (TryWriteShiftChangeOccurred(false)) lastOccurred = false;
                    }
                    continue;
                }

                // Reduce timeSinceStart into the configured cycle (seconds)
                double cycleSeconds = cycleHours * 3600.0;
                double timeSinceStartSeconds = timeSinceStart.TotalSeconds;
                double inCycleSeconds = timeSinceStartSeconds % cycleSeconds;
                if (inCycleSeconds < 0) inCycleSeconds += cycleSeconds;

                double shiftSeconds = hoursPerShift * 3600.0;
                int shiftIndex = (int)Math.Floor(inCycleSeconds / shiftSeconds);

                double timeIntoShiftSeconds = inCycleSeconds - shiftIndex * shiftSeconds;
                double timeRemainingSeconds = shiftSeconds - timeIntoShiftSeconds;
                if (timeRemainingSeconds < 0) timeRemainingSeconds = 0.0;

                // Determine 1-based current shift number
                int currentShift = ((shiftIndex % numberOfShifts) + numberOfShifts) % numberOfShifts;
                currentShift += 1;

                // Write CurrentShift only when changed
                if (currentShift != lastReportedShift)
                {
                    if (TryWriteCurrentShift(currentShift))
                    {
                        lastReportedShift = currentShift;
                        Log.Debug("ShiftCalculator", $"Shift change: {currentShift}");

                        // On successful shift write, pulse ShiftChangeOccurred = true (will be cleared next loop)
                        if (hasShiftChangeOccurredVar)
                        {
                            if (TryWriteShiftChangeOccurred(true))
                            {
                                lastOccurred = true;
                                justSetOccurredThisLoop = true;
                            }
                            else
                            {
                                // if write failed, mark as not available to avoid spamming
                                hasShiftChangeOccurredVar = false;
                                lastOccurred = false;
                                justSetOccurredThisLoop = false;
                            }
                        }
                    }
                }

                // Imminent: true when timeRemainingSeconds <= threshold
                bool imminent = timeRemainingSeconds <= ImminentThresholdSeconds;

                // Write imminent only when changed
                if (lastImminent == null || imminent != lastImminent.Value)
                {
                    if (TryWriteShiftChangeImminent(imminent))
                        lastImminent = imminent;
                    else
                        lastImminent = null; // clear tracking to retry later
                }

                // If we previously wrote ShiftChangeOccurred=true in an earlier loop, clear it now.
                // Do NOT clear in the same loop we just set it (guarded by justSetOccurredThisLoop).
                if (lastOccurred && !justSetOccurredThisLoop)
                {
                    if (TryWriteShiftChangeOccurred(false))
                    {
                        lastOccurred = false;
                    }
                    else
                    {
                        // if clearing fails, leave lastOccurred true so we retry next loop
                    }
                }
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Error("ShiftCalculator", $"Run loop error: {ex.Message}");
            }
        }
    }

    // Try writing CurrentShift with presence and exception handling.
    // returns true on success, false on failure.
    private bool TryWriteCurrentShift(int value)
    {
        if (!hasCurrentShiftVar || CurrentShiftVar == null) return false;
        try
        {
            CurrentShiftVar.SetValue(value);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error("ShiftCalculator", $"Failed to write CurrentShift: {ex.Message}");
            hasCurrentShiftVar = false;
            return false;
        }
    }

    // Try writing ShiftChangeImminent (bool) with presence and exception handling.
    private bool TryWriteShiftChangeImminent(bool value)
    {
        if (!hasShiftChangeImminentVar || ShiftChangeImminentVar == null) return false;
        try
        {
            ShiftChangeImminentVar.SetValue(value);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error("ShiftCalculator", $"Failed to write ShiftChangeImminent: {ex.Message}");
            hasShiftChangeImminentVar = false;
            return false;
        }
    }

    // Try writing ShiftChangeOccurred (bool) with presence and exception handling.
    private bool TryWriteShiftChangeOccurred(bool value)
    {
        if (!hasShiftChangeOccurredVar || ShiftChangeOccurredVar == null) return false;
        try
        {
            ShiftChangeOccurredVar.SetValue(value);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error("ShiftCalculator", $"Failed to write ShiftChangeOccurred: {ex.Message}");
            hasShiftChangeOccurredVar = false;
            return false;
        }
    }

    // Helpers

    // Safely extract the underlying value from IUAVariable.Value,
    // unwrapping UAManagedCore.UAValue if necessary.
    private object GetUnderlyingValue(IUAVariable var)
    {
        if (var == null || var.Value == null) return null;
        var raw = var.Value;
        if (raw is UAValue uaVal) return uaVal.Value;
        return raw;
    }

    private bool TryGetIntFromVariable(IUAVariable var, out int value)
    {
        value = 0;
        var v = GetUnderlyingValue(var);
        if (v == null) return false;
        try
        {
            if (v is int iv) { value = iv; return true; }
            if (v is long lv) { value = Convert.ToInt32(lv); return true; }
            if (v is double dv) { value = Convert.ToInt32(dv); return true; }
            if (int.TryParse(v.ToString(), out int parsed)) { value = parsed; return true; }
        }
        catch { }
        return false;
    }

    private bool TryGetDoubleFromVariable(IUAVariable var, out double value)
    {
        value = 0;
        var v = GetUnderlyingValue(var);
        if (v == null) return false;
        try
        {
            if (v is double dv) { value = dv; return true; }
            if (v is float fv) { value = Convert.ToDouble(fv); return true; }
            if (v is int iv) { value = Convert.ToDouble(iv); return true; }
            if (v is long lv) { value = Convert.ToDouble(lv); return true; }
            if (double.TryParse(v.ToString(), out double parsed)) { value = parsed; return true; }
        }
        catch { }
        return false;
    }

    private bool TryGetShiftStartTime(out TimeSpan shiftStart)
    {
        shiftStart = TimeSpan.Zero;
        var v = GetUnderlyingValue(FirstShiftStartTimeVar);
        if (v == null) return false;

        try
        {
            if (v is DateTime dt)
            {
                shiftStart = dt.TimeOfDay;
                return true;
            }
            if (v is TimeSpan ts)
            {
                shiftStart = ts;
                return true;
            }

            string s = v.ToString()?.Trim();
            if (string.IsNullOrEmpty(s)) return false;

            // Try parse TimeSpan first (supports "hh:mm", "hh:mm:ss", etc.)
            if (TimeSpan.TryParse(s, out TimeSpan parsedTs))
            {
                shiftStart = parsedTs;
                return true;
            }

            // Try parse DateTime next
            if (DateTime.TryParse(s, out DateTime parsedDt))
            {
                shiftStart = parsedDt.TimeOfDay;
                return true;
            }

            // Support "HHmm" (e.g., "0600")
            if (s.Length == 4 && int.TryParse(s, out int hhmm))
            {
                string hh = s.Substring(0, 2);
                string mm = s.Substring(2, 2);
                if (int.TryParse(hh, out int h) && int.TryParse(mm, out int m))
                {
                    if (h >= 0 && h < 24 && m >= 0 && m < 60)
                    {
                        shiftStart = new TimeSpan(h, m, 0);
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error("ShiftCalculator", $"Parse FirstShiftStartTime error: {ex.Message}");
        }

        return false;
    }
}
