using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Shared;
using ProjectChimera.Data.Facilities;
using FacilityRoomTypeBase = ProjectChimera.Data.Facilities.FacilityRoomType;

namespace ProjectChimera.Data
{
    /// <summary>
    /// Facility Design Configuration - Configuration for facility design and layout systems
    /// Defines facility templates, design rules, and construction parameters
    /// Part of Enhanced Scientific Gaming System v2.0
    /// </summary>
    [CreateAssetMenu(fileName = "New Facility Design Config", menuName = "Project Chimera/Facilities/Facility Design Config")]
    public class FacilityDesignConfigSO : ChimeraConfigSO
    {
        [Header("Design Templates")]
        public List<FacilityTemplate> FacilityTemplates = new List<FacilityTemplate>();
        
        [Header("Room Types")]
        public List<RoomType> AvailableRoomTypes = new List<RoomType>();
        
        [Header("Design Rules")]
        public List<DesignRule> DesignRules = new List<DesignRule>();
        
        [Header("Layout Settings")]
        [Range(1f, 100f)] public float MinRoomSize = 10f;
        [Range(10f, 1000f)] public float MaxRoomSize = 200f;
        [Range(0.1f, 10f)] public float CorridorWidth = 2f;
        [Range(0.1f, 5f)] public float WallThickness = 0.3f;
        
        [Header("Efficiency Calculations")]
        [Range(0.1f, 2f)] public float LayoutEfficiencyMultiplier = 1f;
        [Range(0.1f, 2f)] public float OptimalDesignBonus = 1.2f;
        [Range(0.5f, 1f)] public float SuboptimalDesignPenalty = 0.8f;
        
        public FacilityTemplate GetTemplate(FacilityType facilityType, FacilitySize size)
        {
            return FacilityTemplates.Find(t => t.FacilityType == facilityType && t.Size == size);
        }
        
        public bool ValidateDesign(FacilityDesign design)
        {
            foreach (var rule in DesignRules)
            {
                if (!EvaluateRule(rule, design))
                    return false;
            }
            return true;
        }
        
        public float CalculateDesignEfficiency(FacilityDesign design)
        {
            float efficiency = 1f;
            
            // Apply layout efficiency
            efficiency *= LayoutEfficiencyMultiplier;
            
            // Check for optimal design patterns
            if (IsOptimalDesign(design))
                efficiency *= OptimalDesignBonus;
            else if (IsSuboptimalDesign(design))
                efficiency *= SuboptimalDesignPenalty;
            
            return efficiency;
        }
        
        private bool EvaluateRule(DesignRule rule, FacilityDesign design)
        {
            // Simplified rule evaluation - in real implementation would be more complex
            return true; // Placeholder
        }
        
        private bool IsOptimalDesign(FacilityDesign design)
        {
            // Simplified optimization check - in real implementation would analyze layout patterns
            return design.Rooms.Count > 0 && design.TotalArea > MinRoomSize;
        }
        
        private bool IsSuboptimalDesign(FacilityDesign design)
        {
            // Simplified suboptimal check
            return design.Rooms.Count == 0 || design.TotalArea < MinRoomSize;
        }
    }
    
    [System.Serializable]
    public class FacilityTemplate
    {
        public string TemplateName;
        public FacilityType FacilityType;
        public FacilitySize Size;
        
        [Header("Template Specifications")]
        public List<RoomSpecification> RoomSpecifications = new List<RoomSpecification>();
        [Range(10f, 10000f)] public float TotalArea = 100f;
        [Range(1, 50)] public int MaxRooms = 10;
        
        [Header("Cost and Requirements")]
        [Range(100f, 1000000f)] public float BaseCost = 1000f;
        [Range(1, 100)] public int RequiredLevel = 1;
        public List<string> RequiredAchievements = new List<string>();
        
        [Header("Efficiency Ratings")]
        [Range(0.1f, 2f)] public float OperationalEfficiency = 1f;
        [Range(0.1f, 2f)] public float EnergyEfficiency = 1f;
        [Range(0.1f, 2f)] public float SpaceEfficiency = 1f;
        
        public string Description;
        public Sprite TemplateIcon;
    }
    
    [System.Serializable]
    public class RoomType
    {
        public string RoomName;
        public FacilityRoomTypeBase RoomCategory;
        [Range(1f, 1000f)] public float MinSize = 10f;
        [Range(10f, 2000f)] public float MaxSize = 100f;
        [Range(100f, 100000f)] public float CostPerSquareMeter = 500f;
        
        [Header("Room Features")]
        public List<RoomFeature> RequiredFeatures = new List<RoomFeature>();
        public List<RoomFeature> OptionalFeatures = new List<RoomFeature>();
        
        [Header("Environmental Requirements")]
        [Range(10f, 40f)] public float OptimalTemperature = 22f;
        [Range(0f, 100f)] public float OptimalHumidity = 50f;
        [Range(0f, 2000f)] public float LightRequirement = 800f;
        
        public string Description;
        public Color RoomColor = Color.white;
    }
    
    [System.Serializable]
    public class DesignRule
    {
        public string RuleName;
        public DesignRuleType RuleType;
        public List<RuleCondition> Conditions = new List<RuleCondition>();
        public string Description;
        public bool IsMandatory = true;
        [Range(0.1f, 2f)] public float ViolationPenalty = 0.8f;
    }
    
    [System.Serializable]
    public class RoomSpecification
    {
        public FacilityRoomTypeBase RoomType;
        [Range(1, 20)] public int Quantity = 1;
        [Range(1f, 1000f)] public float RecommendedSize = 50f;
        public List<string> RequiredFeatures = new List<string>();
        public Vector2 PreferredPosition;
        public bool IsFlexiblePosition = true;
    }
    
    [System.Serializable]
    public class RoomFeature
    {
        public string FeatureName;
        public RoomFeatureType FeatureType;
        [Range(100f, 50000f)] public float FeatureCost = 1000f;
        [Range(0.1f, 2f)] public float EfficiencyModifier = 1f;
        public bool IsUpgradeable = true;
        public string Description;
    }
    
    [System.Serializable]
    public class RuleCondition
    {
        public FacilityConditionType ConditionType;
        public string TargetParameter;
        public float RequiredValue;
        public ComparisonType Comparison;
    }
    
    [System.Serializable]
    public class FacilityDesign
    {
        public string DesignName;
        public FacilityType FacilityType;
        public List<Room> Rooms = new List<Room>();
        [Range(10f, 10000f)] public float TotalArea = 0f;
        [Range(0f, 1000000f)] public float TotalCost = 0f;
        [Range(0.1f, 2f)] public float OverallEfficiency = 1f;
    }
    
    [System.Serializable]
    public class Room
    {
        public string RoomName;
        public FacilityRoomTypeBase RoomType;
        [Range(1f, 1000f)] public float Area = 10f;
        public Vector2 Position;
        public List<string> Features = new List<string>();
        [Range(0.1f, 2f)] public float Efficiency = 1f;
    }
    
    public enum FacilitySize
    {
        Micro,
        Small,
        Medium,
        Large,
        Industrial,
        Massive
    }
    
    public enum FacilityRoomType
    {
        GrowRoom,
        VegetativeRoom,
        FloweringRoom,
        NurseryRoom,
        DryingRoom,
        CuringRoom,
        ProcessingRoom,
        StorageRoom,
        OfficeSpace,
        Laboratory,
        UtilityRoom,
        Corridor
    }
    
    public enum RoomFeatureType
    {
        HVAC,
        Lighting,
        Irrigation,
        Ventilation,
        Security,
        Monitoring,
        PowerSupply,
        Storage,
        Workstation,
        Equipment
    }
    
    public enum DesignRuleType
    {
        MinimumArea,
        MaximumArea,
        RequiredFeature,
        ProhibitedCombination,
        OptimalLayout,
        SafetyRequirement,
        EfficiencyRule,
        CostConstraint
    }
    
    public enum FacilityConditionType
    {
        RoomCount,
        TotalArea,
        FeaturePresence,
        CostLimit,
        EfficiencyTarget,
        LayoutPattern,
        SafetyCompliance,
        RegulationCompliance
    }
    
    public enum ComparisonType
    {
        Equal,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual,
        NotEqual
    }
}