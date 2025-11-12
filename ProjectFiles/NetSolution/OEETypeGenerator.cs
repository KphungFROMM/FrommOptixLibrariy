using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.HMIProject;
using FTOptix.Core;
using FTOptix.CoreBase;

public class OEETypeGenerator : BaseNetLogic
{
    public override void Start()
    {
        // NetLogic started
    }

    public override void Stop()
    {
        // NetLogic stopped
    }

    [ExportMethod]
    public void CreateOEEObjectType()
    {
        try
        {
            Log.Info("Starting OEE ObjectType creation...");
            
            // Get the Types folder
            var typesFolder = Project.Current.Get("Types");
            if (typesFolder == null)
            {
                Log.Error("Types folder not found in project");
                return;
            }

            // Create ObjectTypes folder if it doesn't exist
            var objectTypesFolder = typesFolder.Get("ObjectTypes");
            if (objectTypesFolder == null)
            {
                objectTypesFolder = InformationModel.Make<Folder>("ObjectTypes");
                typesFolder.Add(objectTypesFolder);
                Log.Info("Created ObjectTypes folder");
            }

            // Check if OEE ObjectType already exists
            var existingOEEType = objectTypesFolder.Get("OEEType");
            if (existingOEEType != null)
            {
                Log.Warning("OEEType already exists. Delete it first if you want to recreate it.");
                return;
            }

            // Create the OEE ObjectType
            var oeeObjectType = InformationModel.MakeObjectType("OEEType");
            objectTypesFolder.Add(oeeObjectType);
            Log.Info("Created OEEType ObjectType");

            // Create the structure inside the ObjectType
            CreateInputsStructure(oeeObjectType);
            CreateOutputsStructure(oeeObjectType);
            CreateConfigurationStructure(oeeObjectType);

            Log.Info("OEE ObjectType creation completed successfully!");
            Log.Info("You can now create instances by dragging OEEType from Types/ObjectTypes to your Model or other locations.");
        }
        catch (Exception ex)
        {
            Log.Error($"Error creating OEE ObjectType: {ex.Message}");
            Log.Error($"Stack trace: {ex.StackTrace}");
        }
    }

    [ExportMethod]
    public void DeleteOEEObjectType()
    {
        try
        {
            var typesFolder = Project.Current.Get("Types");
            if (typesFolder == null)
            {
                Log.Error("Types folder not found");
                return;
            }

            var objectTypesFolder = typesFolder.Get("ObjectTypes");
            if (objectTypesFolder == null)
            {
                Log.Warning("ObjectTypes folder not found");
                return;
            }

            var oeeType = objectTypesFolder.Get("OEEType");
            if (oeeType != null)
            {
                oeeType.Delete();
                Log.Info("OEEType deleted successfully!");
            }
            else
            {
                Log.Warning("OEEType not found");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error deleting OEE ObjectType: {ex.Message}");
        }
    }

    [ExportMethod]
    public void CreateOEEInstance()
    {
        try
        {
            Log.Info("Creating OEE instance...");
            
            // Get the Model folder
            var model = Project.Current.Get("Model");
            if (model == null)
            {
                Log.Error("Model folder not found");
                return;
            }

            // Find the OEEType
            var oeeType = Project.Current.Get("Types/ObjectTypes/OEEType");
            if (oeeType == null)
            {
                Log.Error("OEEType not found. Create it first using CreateOEEObjectType method.");
                return;
            }

            // Create instance with a unique name
            string instanceName = $"OEEInstance_{DateTime.Now:yyyyMMdd_HHmmss}";
            var oeeInstance = InformationModel.MakeObject(instanceName, oeeType.NodeId);
            model.Add(oeeInstance);

            Log.Info($"OEE instance '{instanceName}' created successfully in Model folder!");
        }
        catch (Exception ex)
        {
            Log.Error($"Error creating OEE instance: {ex.Message}");
        }
    }

    private void CreateInputsStructure(IUANode parentType)
    {
        var inputsFolder = InformationModel.Make<Folder>("Inputs");
        parentType.Add(inputsFolder);

        // Core Production Input Variables (used by calculator)
        CreateTypeVariable(inputsFolder, "TotalRuntimeSeconds", OpcUa.DataTypes.Double);
        CreateTypeVariable(inputsFolder, "GoodPartCount", OpcUa.DataTypes.Int32);
        CreateTypeVariable(inputsFolder, "BadPartCount", OpcUa.DataTypes.Int32);
        CreateTypeVariable(inputsFolder, "IdealCycleTimeSeconds", OpcUa.DataTypes.Double);
        CreateTypeVariable(inputsFolder, "PlannedProductionTimeHours", OpcUa.DataTypes.Double);

        // Shift Configuration Inputs
        CreateTypeVariable(inputsFolder, "HoursPerShift", OpcUa.DataTypes.Double);
        CreateTypeVariable(inputsFolder, "NumberOfShifts", OpcUa.DataTypes.Int32);
        CreateTypeVariable(inputsFolder, "ShiftStartTime", OpcUa.DataTypes.String);

        // Target Configuration Inputs
        CreateTypeVariable(inputsFolder, "ProductionTarget", OpcUa.DataTypes.Int32);
        CreateTypeVariable(inputsFolder, "QualityTarget", OpcUa.DataTypes.Double);
        CreateTypeVariable(inputsFolder, "PerformanceTarget", OpcUa.DataTypes.Double);
        CreateTypeVariable(inputsFolder, "AvailabilityTarget", OpcUa.DataTypes.Double);
        CreateTypeVariable(inputsFolder, "OEETarget", OpcUa.DataTypes.Double);

        // System Configuration Inputs
        CreateTypeVariable(inputsFolder, "UpdateRateMs", OpcUa.DataTypes.Int32);
        CreateTypeVariable(inputsFolder, "LoggingVerbosity", OpcUa.DataTypes.Int32);

        Log.Info("Input structure created in ObjectType");
    }

    private void CreateOutputsStructure(IUANode parentType)
    {
        var outputsFolder = InformationModel.Make<Folder>("Outputs");
        parentType.Add(outputsFolder);

        // Core OEE Metrics
        CreateTypeVariable(outputsFolder, "TotalCount", OpcUa.DataTypes.Int32);
        CreateTypeVariable(outputsFolder, "Quality", OpcUa.DataTypes.Double);
        CreateTypeVariable(outputsFolder, "Performance", OpcUa.DataTypes.Double);
        CreateTypeVariable(outputsFolder, "Availability", OpcUa.DataTypes.Double);
        CreateTypeVariable(outputsFolder, "OEE", OpcUa.DataTypes.Double);
        CreateTypeVariable(outputsFolder, "AvgCycleTime", OpcUa.DataTypes.Double);
        CreateTypeVariable(outputsFolder, "PartsPerHour", OpcUa.DataTypes.Double);
        CreateTypeVariable(outputsFolder, "ExpectedPartCount", OpcUa.DataTypes.Int32);
        CreateTypeVariable(outputsFolder, "DowntimeFormatted", OpcUa.DataTypes.String);
        CreateTypeVariable(outputsFolder, "TotalRuntimeFormatted", OpcUa.DataTypes.String);

        // Shift & Time Tracking Outputs
        CreateTypeVariable(outputsFolder, "CurrentShiftNumber", OpcUa.DataTypes.Int32);
        CreateTypeVariable(outputsFolder, "ShiftStartTimeOutput", OpcUa.DataTypes.String);
        CreateTypeVariable(outputsFolder, "ShiftEndTime", OpcUa.DataTypes.String);
        CreateTypeVariable(outputsFolder, "TimeIntoShift", OpcUa.DataTypes.String);
        CreateTypeVariable(outputsFolder, "TimeRemainingInShift", OpcUa.DataTypes.String);
        CreateTypeVariable(outputsFolder, "ShiftChangeOccurred", OpcUa.DataTypes.Boolean);
        CreateTypeVariable(outputsFolder, "ShiftChangeImminent", OpcUa.DataTypes.Boolean);

        // Production Planning Outputs
        CreateTypeVariable(outputsFolder, "ProjectedTotalCount", OpcUa.DataTypes.Int32);
        CreateTypeVariable(outputsFolder, "RemainingTimeAtCurrentRate", OpcUa.DataTypes.String);
        CreateTypeVariable(outputsFolder, "ProductionBehindSchedule", OpcUa.DataTypes.Boolean);
        CreateTypeVariable(outputsFolder, "RequiredRateToTarget", OpcUa.DataTypes.Double);
        CreateTypeVariable(outputsFolder, "TargetVsActualParts", OpcUa.DataTypes.Int32);

        // System Health Outputs
        CreateTypeVariable(outputsFolder, "LastUpdateTime", OpcUa.DataTypes.String);
        CreateTypeVariable(outputsFolder, "SystemStatus", OpcUa.DataTypes.String);
        CreateTypeVariable(outputsFolder, "CalculationValid", OpcUa.DataTypes.Boolean);
        CreateTypeVariable(outputsFolder, "DataQualityScore", OpcUa.DataTypes.Double);

        // Trending Outputs
        CreateTypeVariable(outputsFolder, "QualityTrend", OpcUa.DataTypes.String);
        CreateTypeVariable(outputsFolder, "PerformanceTrend", OpcUa.DataTypes.String);
        CreateTypeVariable(outputsFolder, "AvailabilityTrend", OpcUa.DataTypes.String);
        CreateTypeVariable(outputsFolder, "OEETrend", OpcUa.DataTypes.String);

        // Statistics Outputs
        CreateTypeVariable(outputsFolder, "MinQuality", OpcUa.DataTypes.Double);
        CreateTypeVariable(outputsFolder, "MaxQuality", OpcUa.DataTypes.Double);
        CreateTypeVariable(outputsFolder, "AvgQuality", OpcUa.DataTypes.Double);
        CreateTypeVariable(outputsFolder, "MinPerformance", OpcUa.DataTypes.Double);
        CreateTypeVariable(outputsFolder, "MaxPerformance", OpcUa.DataTypes.Double);
        CreateTypeVariable(outputsFolder, "AvgPerformance", OpcUa.DataTypes.Double);
        CreateTypeVariable(outputsFolder, "MinAvailability", OpcUa.DataTypes.Double);
        CreateTypeVariable(outputsFolder, "MaxAvailability", OpcUa.DataTypes.Double);
        CreateTypeVariable(outputsFolder, "AvgAvailability", OpcUa.DataTypes.Double);
        CreateTypeVariable(outputsFolder, "MinOEE", OpcUa.DataTypes.Double);
        CreateTypeVariable(outputsFolder, "MaxOEE", OpcUa.DataTypes.Double);
        CreateTypeVariable(outputsFolder, "AvgOEE", OpcUa.DataTypes.Double);

        // Target Comparison Outputs
        CreateTypeVariable(outputsFolder, "QualityVsTarget", OpcUa.DataTypes.Double);
        CreateTypeVariable(outputsFolder, "PerformanceVsTarget", OpcUa.DataTypes.Double);
        CreateTypeVariable(outputsFolder, "AvailabilityVsTarget", OpcUa.DataTypes.Double);
        CreateTypeVariable(outputsFolder, "OEEVsTarget", OpcUa.DataTypes.Double);

        Log.Info("Output structure created in ObjectType");
    }

    private void CreateConfigurationStructure(IUANode parentType)
    {
        var configFolder = InformationModel.Make<Folder>("Configuration");
        parentType.Add(configFolder);

        // These are optional configuration settings - the main inputs are in Inputs folder
        // This section can be used for additional settings not covered by calculator inputs

        // Calculation Settings
        CreateTypeVariable(configFolder, "EnableRealTimeCalc", OpcUa.DataTypes.Boolean);
        CreateTypeVariable(configFolder, "MinimumRunTime", OpcUa.DataTypes.Double);

        // Thresholds for alerts/indicators
        CreateTypeVariable(configFolder, "GoodOEE_Threshold", OpcUa.DataTypes.Double);
        CreateTypeVariable(configFolder, "PoorOEE_Threshold", OpcUa.DataTypes.Double);

        // System Settings
        CreateTypeVariable(configFolder, "EnableLogging", OpcUa.DataTypes.Boolean);
        CreateTypeVariable(configFolder, "EnableAlarms", OpcUa.DataTypes.Boolean);
        CreateTypeVariable(configFolder, "SystemHealthy", OpcUa.DataTypes.Boolean);

        Log.Info("Configuration structure created in ObjectType");
    }

    private void CreateTypeVariable(IUANode parent, string name, NodeId dataType)
    {
        try
        {
            var variable = InformationModel.MakeVariable(name, dataType);
            
            // Set initial value based on data type
            if (dataType == OpcUa.DataTypes.Boolean)
                variable.Value = false;
            else if (dataType == OpcUa.DataTypes.Int32)
                variable.Value = 0;
            else if (dataType == OpcUa.DataTypes.Double)
                variable.Value = 0.0;
            else
                variable.Value = "";

            parent.Add(variable);
        }
        catch (Exception ex)
        {
            Log.Error($"Error creating type variable {name}: {ex.Message}");
        }
    }
}
