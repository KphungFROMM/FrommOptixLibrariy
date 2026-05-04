#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.HMIProject;
using FTOptix.NativeUI;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.Core;
using FTOptix.NetLogic;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.EventLogger;
using FTOptix.DataLogger;
#endregion

public class OnBitLoggerWithHandShake : BaseNetLogic
{
    private const string LoggerInstanceVariableName = "LoggerInstance";
    private const string TriggerBitVariableName = "TriggerBit";
    private const string HandShakeVariableName = "HandShake";

    private IUAVariable loggerInstanceVariable;
    private IUAVariable triggerBitVariable;
    private IUAVariable handShakeVariable;
    private bool lastTriggerState;
    private bool isProcessing;

    public override void Start()
    {
        loggerInstanceVariable = LogicObject.GetVariable(LoggerInstanceVariableName);
        triggerBitVariable = LogicObject.GetVariable(TriggerBitVariableName);
        handShakeVariable = LogicObject.GetVariable(HandShakeVariableName);

        if (loggerInstanceVariable == null || triggerBitVariable == null || handShakeVariable == null)
        {
            Log.Error(nameof(OnBitLoggerWithHandShake), "Missing one or more required NetLogic variables: LoggerInstance, TriggerBit, HandShake.");
            return;
        }

        lastTriggerState = ReadBool(triggerBitVariable);
        triggerBitVariable.VariableChange += TriggerBitVariableChange;
    }

    public override void Stop()
    {
        if (triggerBitVariable != null)
            triggerBitVariable.VariableChange -= TriggerBitVariableChange;
    }

    private void TriggerBitVariableChange(object sender, VariableChangeEventArgs e)
    {
        bool currentTriggerState = ReadBool(triggerBitVariable);
        bool isRisingEdge = currentTriggerState && !lastTriggerState;
        lastTriggerState = currentTriggerState;

        if (!isRisingEdge || isProcessing)
            return;

        isProcessing = true;

        try
        {
            handShakeVariable.Value = false;

            FTOptix.DataLogger.DataLogger dataLogger = ResolveLogger();
            if (dataLogger == null)
            {
                Log.Error(nameof(OnBitLoggerWithHandShake), "LoggerInstance does not point to a valid DataLogger node.");
                return;
            }

            dataLogger.LogMethod.Execute(dataLogger);
            handShakeVariable.Value = true;
        }
        catch (Exception ex)
        {
            Log.Error(nameof(OnBitLoggerWithHandShake), $"Failed to execute log request: {ex.Message}");
        }
        finally
        {
            isProcessing = false;
        }
    }

    private FTOptix.DataLogger.DataLogger ResolveLogger()
    {
        object rawValue = loggerInstanceVariable.Value.Value;
        if (rawValue is not NodeId loggerNodeId || loggerNodeId == null || loggerNodeId == NodeId.Empty)
            return null;

        return InformationModel.Get<FTOptix.DataLogger.DataLogger>(loggerNodeId);
    }

    private static bool ReadBool(IUAVariable variable)
    {
        object rawValue = variable?.Value.Value;
        return rawValue is bool value && value;
    }
}
