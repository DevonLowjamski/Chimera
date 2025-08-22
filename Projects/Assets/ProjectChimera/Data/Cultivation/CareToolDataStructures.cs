using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core;

namespace ProjectChimera.Data.Cultivation
{
    /// <summary>
    /// Care Tool Data Structures - Defines all tool types and related data for plant care
    /// Contains tool definitions, capabilities, and usage parameters
    /// </summary>
    
    #region Base Tool Classes
    
    [System.Serializable]
    public abstract class CareToolBase
    {
        [Header("Basic Tool Properties")]
        public string ToolId;
        public string ToolName;
        public string Description;
        [Range(0.1f, 5.0f)] public float Quality = 1.0f;
        [Range(0.1f, 2.0f)] public float EfficiencyMultiplier = 1.0f;
        [Range(0f, 1f)] public float Durability = 1.0f;
        [Range(0f, 1f)] public float Condition = 1.0f;
        
        [Header("Tool Capabilities")]
        public List<CultivationTaskType> CompatibleTaskTypes = new List<CultivationTaskType>();
        public ToolTier Tier = ToolTier.Basic;
        public bool RequiresSkill = false;
        [Range(1, 100)] public int MinimumSkillLevel = 1;
        
        [Header("Visual and Audio")]
        public Sprite ToolIcon;
        public GameObject ToolPrefab;
        public AudioClip UsageSound;
        
        public virtual float GetEffectiveness(CultivationTaskType taskType)
        {
            if (!CompatibleTaskTypes.Contains(taskType))
                return 0f;
            
            return Quality * EfficiencyMultiplier * Condition;
        }
        
        public virtual Dictionary<string, object> GetDetailedInspectionData(InteractivePlant plant)
        {
            return new Dictionary<string, object>
            {
                { "ToolQuality", Quality },
                { "ToolCondition", Condition },
                { "InspectionTime", System.DateTime.Now }
            };
        }
    }
    
    #endregion
    
    #region Watering Tools
    
    [System.Serializable]
    public class WateringTool : CareToolBase
    {
        [Header("Watering Properties")]
        [Range(0.1f, 10.0f)] public float FlowRate = 1.0f; // Liters per minute
        [Range(0.1f, 5.0f)] public float Precision = 1.0f;
        [Range(0.1f, 2.0f)] public float WaterPressure = 1.0f;
        public WateringType WateringType = WateringType.Manual;
        public bool HasVolumeControl = true;
        public bool HasFlowControl = false;
        
        [Header("Water Distribution")]
        public WateringPattern Pattern = WateringPattern.Targeted;
        [Range(0.1f, 2.0f)] public float CoverageRadius = 0.5f;
        [Range(0f, 1f)] public float WaterDistributionUniformity = 0.8f;
        
        public override float GetEffectiveness(CultivationTaskType taskType)
        {
            if (taskType != CultivationTaskType.Watering)
                return 0f;
            
            return base.GetEffectiveness(taskType) * Precision * WaterDistributionUniformity;
        }
    }
    
    #endregion
    
    #region Pruning Tools
    
    [System.Serializable]
    public class PruningTool : CareToolBase
    {
        [Header("Cutting Properties")]
        [Range(0.1f, 2.0f)] public float Sharpness = 1.0f;
        [Range(0.1f, 5.0f)] public float CuttingPrecision = 1.0f;
        [Range(0.1f, 50f)] public float MaxCuttingDiameter = 5f; // mm
        public CuttingType CuttingType = CuttingType.Bypass;
        public bool RequiresSterilization = true;
        
        [Header("Blade Properties")]
        public BladeType BladeType = BladeType.Straight;
        public BladeMaterial Material = BladeMaterial.StainlessSteel;
        [Range(0f, 1f)] public float BladeSharpness = 1.0f;
        [Range(0f, 1f)] public float SterilizationLevel = 1.0f;
        
        public override float GetEffectiveness(CultivationTaskType taskType)
        {
            if (taskType != CultivationTaskType.Pruning)
                return 0f;
            
            return base.GetEffectiveness(taskType) * Sharpness * CuttingPrecision * SterilizationLevel;
        }
        
        public float GetCutQuality()
        {
            return Sharpness * BladeSharpness * (Condition * 0.5f + 0.5f);
        }
    }
    
    #endregion
    
    #region Training Tools
    
    [System.Serializable]
    public class TrainingTool : CareToolBase
    {
        [Header("Training Properties")]
        public TrainingMethod SupportedMethods = TrainingMethod.LST;
        [Range(0.1f, 2.0f)] public float FlexibilityControl = 1.0f;
        [Range(0.1f, 5.0f)] public float TensionControl = 1.0f;
        public bool AllowsGradualAdjustment = true;
        public bool HasTensionIndicator = false;
        
        [Header("Material Properties")]
        public TrainingMaterial Material = TrainingMaterial.SoftWire;
        [Range(0.1f, 10f)] public float MaxTensionLoad = 5f; // Newtons
        [Range(0f, 1f)] public float PlantSafetyRating = 0.9f;
        
        public override float GetEffectiveness(CultivationTaskType taskType)
        {
            if (taskType != CultivationTaskType.Training)
                return 0f;
            
            return base.GetEffectiveness(taskType) * FlexibilityControl * PlantSafetyRating;
        }
    }
    
    #endregion
    
    #region Monitoring Tools
    
    [System.Serializable]
    public class InspectionTool : CareToolBase
    {
        [Header("Inspection Properties")]
        [Range(1f, 100f)] public float Magnification = 1.0f;
        [Range(0.1f, 2.0f)] public float InspectionAccuracy = 1.0f;
        public InspectionType InspectionType = InspectionType.Visual;
        public bool HasDigitalDisplay = false;
        public bool HasImageCapture = false;
        
        [Header("Detection Capabilities")]
        public List<DetectionCapability> DetectionCapabilities = new List<DetectionCapability>();
        [Range(0f, 1f)] public float PestDetectionAccuracy = 0.7f;
        [Range(0f, 1f)] public float DiseaseDetectionAccuracy = 0.6f;
        [Range(0f, 1f)] public float NutrientDeficiencyDetection = 0.5f;
        
        public override Dictionary<string, object> GetDetailedInspectionData(InteractivePlant plant)
        {
            var data = base.GetDetailedInspectionData(plant);
            
            data["Magnification"] = Magnification;
            data["InspectionAccuracy"] = InspectionAccuracy;
            data["PestDetectionScore"] = PestDetectionAccuracy * Quality;
            data["DiseaseDetectionScore"] = DiseaseDetectionAccuracy * Quality;
            data["NutrientDetectionScore"] = NutrientDeficiencyDetection * Quality;
            
            return data;
        }
    }
    
    #endregion
    
    #region Diagnostic Tools
    
    [System.Serializable]
    public class DiagnosticTool : CareToolBase
    {
        [Header("Diagnostic Properties")]
        public DiagnosticType DiagnosticType = DiagnosticType.Health;
        [Range(0.1f, 2.0f)] public float DiagnosticAccuracy = 1.0f;
        [Range(0.1f, 5.0f)] public float AnalysisSpeed = 1.0f;
        public bool RequiresCalibration = false;
        public bool ProvidesRecommendations = true;
        
        [Header("Measurement Capabilities")]
        public List<MeasurementType> MeasurementTypes = new List<MeasurementType>();
        [Range(0f, 1f)] public float MeasurementPrecision = 0.8f;
        [Range(0f, 1f)] public float RecommendationAccuracy = 0.7f;
        
        public virtual Dictionary<string, object> AnalyzePlantHealth(InteractivePlant plant)
        {
            return new Dictionary<string, object>
            {
                { "OverallHealthScore", UnityEngine.Random.Range(0.3f, 1.0f) * DiagnosticAccuracy },
                { "HydrationLevel", UnityEngine.Random.Range(0.2f, 1.0f) },
                { "NutritionLevel", UnityEngine.Random.Range(0.2f, 1.0f) },
                { "StressLevel", UnityEngine.Random.Range(0f, 0.8f) },
                { "DiagnosticTimestamp", System.DateTime.Now },
                { "ToolAccuracy", DiagnosticAccuracy * Quality }
            };
        }
        
        public virtual List<CareRecommendation> GenerateRecommendations(InteractivePlant plant)
        {
            var recommendations = new List<CareRecommendation>();
            
            if (ProvidesRecommendations)
            {
                recommendations.Add(new CareRecommendation
                {
                    RecommendationType = CultivationTaskType.Watering,
                    Priority = RecommendationPriority.Medium,
                    Description = "Consider adjusting watering schedule",
                    Confidence = RecommendationAccuracy * Quality
                });
            }
            
            return recommendations;
        }
    }
    
    #endregion
    
    #region Supporting Data Structures
    
    [System.Serializable]
    public class CareRecommendation
    {
        public CultivationTaskType RecommendationType;
        public RecommendationPriority Priority;
        public string Description;
        [Range(0f, 1f)] public float Confidence;
        public System.DateTime RecommendationTime;
        public Dictionary<string, object> Parameters = new Dictionary<string, object>();
    }
    
    #endregion
    
    #region Enums
    
    public enum ToolTier
    {
        Basic,
        Standard,
        Professional,
        Expert,
        Premium,
        Specialized
    }
    
    public enum WateringType
    {
        Manual,
        SemiAutomatic,
        Automatic,
        Drip,
        Flood,
        Mist
    }
    
    public enum WateringPattern
    {
        Targeted,
        Broadcast,
        Circular,
        Linear,
        Precision
    }
    
    public enum CuttingType
    {
        Bypass,
        Anvil,
        Ratchet,
        Compound
    }
    
    public enum BladeType
    {
        Straight,
        Curved,
        Serrated,
        MicroTip,
        Precision
    }
    
    public enum BladeMaterial
    {
        StainlessSteel,
        CarbonSteel,
        Titanium,
        Ceramic,
        CoatedSteel
    }
    
    public enum TrainingMaterial
    {
        SoftWire,
        PlantTies,
        VelcroStraps,
        String,
        RubberCoated,
        Biodegradable
    }
    
    public enum InspectionType
    {
        Visual,
        Magnified,
        Digital,
        Microscopic,
        Spectral,
        Thermal
    }
    
    public enum DetectionCapability
    {
        PestDetection,
        DiseaseDetection,
        NutrientDeficiency,
        TrichomeAnalysis,
        GrowthAssessment,
        HealthMonitoring
    }
    
    public enum DiagnosticType
    {
        Health,
        Environment,
        Nutrition,
        Water,
        pH,
        Conductivity,
        Temperature,
        Humidity
    }
    
    public enum MeasurementType
    {
        pH,
        EC,
        TDS,
        Temperature,
        Humidity,
        Light,
        CO2,
        Oxygen
    }
    
    public enum RecommendationPriority
    {
        Low,
        Medium,
        High,
        Critical,
        Emergency
    }
    
    #endregion
}