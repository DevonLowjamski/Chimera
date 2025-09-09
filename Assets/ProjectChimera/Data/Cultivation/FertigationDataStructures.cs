using UnityEngine;
using System.Collections.Generic;
using System;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Shared;

namespace ProjectChimera.Data.Cultivation
{
    // Local type definitions to replace deleted types
    public enum CultivationGoal
    {
        MaxYield, MaxPotency, MaxTerpenes, MaxQuality, FastFlower, EnergyEfficient
    }
    
    public enum SensorType
    {
        pH, EC, Temperature, Humidity, Light, CO2, Moisture
    }
    
    [System.Serializable]
    public class EnvironmentalOptimization
    {
        public string OptimizationType;
        public float Value;
        public string Description;
    }

    [System.Serializable]
    public class NutrientLineConfiguration
    {
        public string LineName;
        public NutrientType NutrientType;
        [Range(0f, 200f)] public float Concentration = 100f; // g/L or ml/L
        [Range(0f, 100f)] public float MaxDosePerLiter = 5f; // ml per liter of solution
        public bool IsActive = true;
        public bool RequiresMixingOrder = false;
        [Range(1, 10)] public int MixingPriority = 5;
    }
    
    [System.Serializable]
    public class WaterQualityParameters
    {
        [Header("Basic Parameters")]
        [Range(0f, 3000f)] public float MaxTDS = 300f; // ppm
        [Range(0f, 50f)] public float MaxChlorine = 0.5f; // ppm
        [Range(4f, 9f)] public float SourcepHRange = 7f;
        [Range(0f, 50f)] public float SourceEC = 0.3f; // mS/cm
        
        [Header("Advanced Parameters")]
        [Range(0f, 500f)] public float Alkalinity = 50f; // ppm CaCO3
        [Range(0f, 1000f)] public float Hardness = 150f; // ppm CaCO3
        [Range(0f, 100f)] public float CalciumLevel = 40f; // ppm
        [Range(0f, 50f)] public float MagnesiumLevel = 15f; // ppm
        [Range(0f, 100f)] public float SodiumLevel = 10f; // ppm
    }
    
    [System.Serializable]
    public class WaterTreatmentSystem
    {
        [Header("Filtration")]
        public bool HasSedimentFilter = true;
        public bool HasCarbonFilter = true;
        public bool HasReverseOsmosis = false;
        public bool HasUVSterilization = false;
        
        [Header("Treatment Capabilities")]
        public bool CanRemoveChlorine = true;
        public bool CanAdjustpH = true;
        public bool CanRemoveTDS = false;
        public bool CanAddMinerals = false;
        
        [Header("System Parameters")]
        [Range(1f, 100f)] public float FlowRate = 10f; // L/min
        [Range(0.1f, 1f)] public float FilterEfficiency = 0.95f;
        [Range(100f, 10000f)] public float DailyCapacity = 1000f; // liters
    }
    
    [System.Serializable]
    public class pHControlSystem
    {
        [Header("pH Control Parameters")]
        [Range(4f, 8f)] public float TargetpHRange = 6f;
        [Range(0.1f, 1f)] public float pHDeadband = 0.2f;
        [Range(0.01f, 5f)] public float MaxCorrectionPerDose = 0.5f; // pH units
        [Range(1f, 3600f)] public float CorrectionInterval = 300f; // seconds
        
        [Header("pH Adjustment Solutions")]
        public pHAdjustmentSolution pHUpSolution = new pHAdjustmentSolution { Name = "Potassium Hydroxide", Strength = 1f };
        public pHAdjustmentSolution pHDownSolution = new pHAdjustmentSolution { Name = "Phosphoric Acid", Strength = 1f };
        
        [Header("Safety Parameters")]
        [Range(3f, 4.5f)] public float MinimumpH = 4f;
        [Range(7.5f, 9f)] public float MaximumpH = 8f;
        [Range(1f, 60f)] public float EmergencyResponseTime = 10f; // seconds
    }
    
    [System.Serializable]
    public class ECControlSystem
    {
        [Header("EC Control Parameters")]
        [Range(0.1f, 3f)] public float TargetECRange = 1.2f;
        [Range(0.05f, 0.5f)] public float ECDeadband = 0.1f;
        [Range(0.1f, 2f)] public float MaxCorrectionPerDose = 0.3f; // mS/cm
        [Range(1f, 3600f)] public float CorrectionInterval = 600f; // seconds
        
        [Header("Correction Methods")]
        public bool UseConcentrateAddition = true;
        public bool UseDilution = true;
        public bool UseNutrientBlending = true;
        
        [Header("Safety Parameters")]
        [Range(0f, 0.8f)] public float MinimumEC = 0.2f;
        [Range(2f, 5f)] public float MaximumEC = 3f;
        [Range(0.1f, 1f)] public float DilutionWaterEC = 0.1f;
    }
    
    [System.Serializable]
    public class NutrientProfile
    {
        [Header("Stage Information")]
        public PlantGrowthStage GrowthStage;
        [Range(0f, 14f)] public int StageWeek = 1;
        
        [Header("Basic Parameters")]
        [Range(0.2f, 3f)] public float TargetEC = 1.2f; // mS/cm
        [Range(4f, 8f)] public float TargetpH = 6f;
        [Range(50f, 100f)] public float WaterTemperature = 20f; // Celsius
        
        [Header("NPK Ratios")]
        public Vector3 NPKRatio = new Vector3(1, 1, 1); // N:P:K ratio
        [Range(50f, 400f)] public float NitrogenPPM = 150f;
        [Range(30f, 200f)] public float PhosphorusPPM = 50f;
        [Range(100f, 500f)] public float PotassiumPPM = 200f;
        
        [Header("Secondary Nutrients")]
        [Range(50f, 300f)] public float CalciumPPM = 150f;
        [Range(25f, 150f)] public float MagnesiumPPM = 50f;
        [Range(50f, 300f)] public float SulfurPPM = 100f;
        
        [Header("Feeding Schedule")]
        [Range(1f, 10f)] public float FeedingFrequency = 4f; // times per day
        [Range(0.1f, 10f)] public float FeedingDuration = 1f; // minutes
        [Range(10f, 50f)] public float RunoffPercentage = 20f; // % of applied volume
        
        [TextArea(2, 3)] public string StageNotes;
    }
    
    [System.Serializable]
    public class MonitoringConfiguration
    {
        [Header("Sensor Monitoring")]
        [Range(10f, 3600f)] public float SensorUpdateInterval = 60f; // seconds
        [Range(10f, 86400f)] public float DataLoggingInterval = 300f; // seconds
        public bool EnableRealTimeAlerting = true;
        public bool EnableDataTrending = true;
        
        [Header("Alert Thresholds")]
        [Range(0.1f, 2f)] public float pHAlertThreshold = 0.3f;
        [Range(0.1f, 1f)] public float ECAlertThreshold = 0.2f;
        [Range(1f, 10f)] public float TemperatureAlertThreshold = 3f;
        [Range(0.5f, 5f)] public float FlowRateAlertThreshold = 1f;
        
        [Header("Communication")]
        public bool EnableRemoteMonitoring = false;
        public bool EnableMobileAlerts = false;
        public bool EnableEmailReports = false;
        [Range(1f, 168f)] public float ReportingInterval = 24f; // hours
    }
    
    [System.Serializable]
    public class SafetyProtocols
    {
        [Header("Emergency Response")]
        public bool EnableEmergencyShutoff = true;
        public bool EnableBackupPumps = true;
        public bool EnableFailsafeMode = true;
        [Range(1f, 300f)] public float EmergencyResponseTime = 30f; // seconds
        
        [Header("Chemical Safety")]
        public bool EnableChemicalLockout = true;
        public bool RequireDoubleDosing = false;
        [Range(0.1f, 10f)] public float MaxDosePerMinute = 2f; // ml/min
        [Range(1f, 100f)] public float DailyDoseLimit = 50f; // ml/day
        
        [Header("System Protection")]
        public bool EnableOverflowProtection = true;
        public bool EnableLeakageDetection = true;
        public bool EnablePressureMonitoring = true;
        public bool EnableTemperatureProtection = true;
    }
    
    [System.Serializable]
    public class pHAdjustmentSolution
    {
        public string Name;
        [Range(0.1f, 10f)] public float Strength = 1f; // Concentration multiplier
        [Range(0.1f, 50f)] public float EffectivenessFactor = 1f; // pH change per ml
        [Range(1f, 3600f)] public float ActionTime = 300f; // seconds to take effect
        public bool IsSafe = true;
    }
    
    // Core data classes for fertigation system
    public class NutrientSolution
    {
        public DateTime Timestamp;
        public string ZoneID;
        public int PlantCount;
        public float TargetEC;
        public float TargetpH;
        public Vector3 NPKRatio;
        public Dictionary<NutrientType, float> NutrientConcentrations;
        public DosingSchedule DosingSchedule;
        public Dictionary<string, float> Micronutrients;
        public Dictionary<string, float> Supplements;
        public ValidationResults ValidationResults;
        public float TotalVolume;
        public float EstimatedCost;
    }
    
    public class FertigationSystemStatus
    {
        public DateTime Timestamp;
        public string ZoneID;
        public FertigationMode SystemMode;
        public NutrientConditions CurrentNutrientConditions;
        public NutrientSolution TargetNutrientConditions;
        public CorrectionRequirements RequiredCorrections;
        public float SystemHealth;
        public EquipmentHealthStatus EquipmentStatus;
        public NutrientTrends NutrientTrends;
        public AutomatedAction[] AutomatedActions;
        public EfficiencyMetrics EfficiencyMetrics;
    }
    
    public class IrrigationSchedule
    {
        public DateTime StartDate;
        public int DurationDays;
        public string ZoneID;
        public IrrigationScheduleMode ScheduleMode;
        public DailyIrrigationSchedule[] DailySchedules;
        public AdaptationRule[] AdaptationRules;
        public ScheduleOutcomes ExpectedOutcomes;
    }
    
    public class DailyIrrigationSchedule
    {
        public int Day;
        public DateTime Date;
        public IrrigationEvent[] IrrigationEvents;
        public float TotalWaterVolume;
        public NutrientScheduleEntry[] NutrientSchedule;
    }
    
    public class pHCorrectionAction
    {
        public DateTime Timestamp;
        public float CurrentpH;
        public float TargetpH;
        public float pHDeviation;
        public float SolutionVolume;
        public bool ActionRequired;
        public pHCorrectionType CorrectionType;
        public float CorrectionAmount;
        public float ExpectedResultingpH;
        public float EstimatedCorrectionTime;
    }
    
    public class ECCorrectionAction
    {
        public DateTime Timestamp;
        public float CurrentEC;
        public float TargetEC;
        public float ECDeviation;
        public float SolutionVolume;
        public bool ActionRequired;
        public ECCorrectionType CorrectionType;
        public float CorrectionAmount;
        public float ExpectedResultingEC;
        public float EstimatedCorrectionTime;
    }
    
    public class RunoffAnalysis
    {
        public DateTime Timestamp;
        public float RunoffVolume;
        public NutrientSolution AppliedSolution;
        public Dictionary<NutrientType, float> NutrientUptakeEfficiency;
        public float WaterUseEfficiency;
        public NutrientImbalance[] NutrientImbalances;
        public FertigationEnvironmentalImpact EnvironmentalImpact;
        public FertigationOptimizationRecommendation[] OptimizationRecommendations;
        public CostImplication[] CostImplications;
    }
    
    public class NutrientRecipe
    {
        public string RecipeName;
        public PlantGrowthStage TargetStage;
        public PlantStrainSO TargetStrain;
        public CultivationGoal CultivationGoal;
        public DateTime CreatedDate;
        public NutrientProfile BaseNutrientProfile;
        public GoalModification[] GoalModifications;
        public StrainAdjustment[] StrainAdjustments;
        public EnvironmentalOptimization[] EnvironmentalOptimizations;
        public Dictionary<NutrientType, float> FinalConcentrations;
        public FertigationRecipeOutcomes PredictedOutcomes;
        public UsageInstruction[] UsageInstructions;
    }
    
    // Supporting classes and data structures (simplified for initial implementation)
    public class FertigationSensorData { public SensorType Type; public float Value; public DateTime Timestamp; }
    public class NutrientConditions { public float EC; public float pH; public float Temperature; public WaterQualityData WaterQuality; }
    public class WaterQualityData { public float TDS; public float pH; public float Temperature; public float Volume; }
    public class CorrectionRequirements { public bool RequiresPHCorrection; public bool RequiresECCorrection; }
    public class EquipmentHealthStatus { public float OverallHealth; public string[] Issues; }
    public class NutrientTrends { public string[] TrendDescriptions; }
    public class AutomatedAction { public string ActionType; public string Target; public float Value; }
    public class EfficiencyMetrics { public float NutrientEfficiency; public float WaterEfficiency; public float EnergyEfficiency; }
    public class IrrigationEvent { public TimeSpan Time; public float Duration; public float Volume; }
    public class NutrientScheduleEntry { public TimeSpan Time; public NutrientType NutrientType; public float Amount; }
    public class AdaptationRule { public string Condition; public string Adaptation; }
    public class ScheduleOutcomes { public float ExpectedWaterUse; public float ExpectedNutrientUse; public float ExpectedYieldImpact; }
    public class DosingSchedule { public DosingEvent[] Events; }
    public class DosingEvent { public DateTime Time; public NutrientType Type; public float Amount; }
    public class ValidationResults { public bool IsValid; public string[] Warnings; public string[] Errors; }
    public class NutrientImbalance { public NutrientType Type; public float Deviation; public string Severity; }
    public class FertigationEnvironmentalImpact { public float NitrogenRunoff; public float PhosphorusRunoff; public float OverallImpact; }
    public class FertigationOptimizationRecommendation { public string Category; public string Recommendation; public float PotentialImprovement; }
    public class CostImplication { public string Category; public float CostChange; public string Description; }
    public class GoalModification { public string Parameter; public float Modification; }
    public class StrainAdjustment { public string Parameter; public float Adjustment; }
    public class FertigationRecipeOutcomes { public float ExpectedYield; public float ExpectedQuality; public float ResourceEfficiency; }
    public class UsageInstruction { public string Step; public string Instruction; }
    
    // Enums for fertigation system
    public enum FertigationMode
    {
        Manual,
        SemiAutomated,
        FullyAutomated,
        AIControlled
    }
    
    public enum DeliveryMethod
    {
        DripIrrigation,
        FloodAndDrain,
        NFT,
        DWC,
        Aeroponic,
        TopFeed,
        SubIrrigation
    }
    
    public enum NutrientMixingStrategy
    {
        PreMixed,
        RealTimeBlending,
        BatchMixing,
        ContinuousFlow,
        StageBasedSwitching
    }
    
    public enum NutrientType
    {
        MacronutrientA,
        MacronutrientB,
        BloomEnhancer,
        CalciumMagnesium,
        Micronutrients,
        pHUp,
        pHDown,
        Silica,
        Enzymes,
        Supplements
    }
    
    public enum IrrigationScheduleMode
    {
        FixedSchedule,
        MoistureBasedAir,
        EvapotranspirationBased,
        PlantStageAdaptive,
        EnvironmentallyResponsive,
        AIOptimized
    }
    
    public enum pHCorrectionType
    {
        pHUp,
        pHDown
    }
    
    public enum ECCorrectionType
    {
        AddNutrients,
        Dilute,
        AdjustConcentrate
    }
}