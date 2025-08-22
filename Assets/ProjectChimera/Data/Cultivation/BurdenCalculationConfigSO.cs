using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core;
using ProjectChimera.Shared;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data
{
    /// <summary>
    /// Burden Calculation Configuration - Configuration for workload and burden calculation systems
    /// Defines care burden, automation impact, and player capacity calculations
    /// Part of Enhanced Scientific Gaming System v2.0
    /// </summary>
    [CreateAssetMenu(fileName = "New Burden Calculation Config", menuName = "Project Chimera/Cultivation/Burden Calculation Config")]
    public class BurdenCalculationConfigSO : ChimeraConfigSO
    {
        [Header("Base Burden Settings")]
        [Range(0.1f, 10.0f)] public float BasePlantBurden = 1.0f;
        [Range(0.1f, 5.0f)] public float BurdenGrowthRateMultiplier = 1.2f;
        [Range(0.1f, 2.0f)] public float ComplexityBurdenMultiplier = 1.5f;
        
        [Header("Player Capacity")]
        [Range(1f, 1000f)] public float BasePlayerCapacity = 100f;
        [Range(0.1f, 10.0f)] public float CapacityGrowthPerLevel = 5.0f;
        [Range(1f, 10000f)] public float MaxPlayerCapacity = 5000f;
        
        [Header("Automation Impact")]
        [Range(0.1f, 1.0f)] public float AutomationBurdenReduction = 0.7f;
        [Range(0.01f, 0.5f)] public float AutomationEfficiencyGain = 0.05f;
        [Range(0.5f, 1.0f)] public float MaxAutomationReduction = 0.9f;
        
        [Header("Growth Stage Modifiers")]
        public List<GrowthStageBurdenModifier> GrowthStageModifiers = new List<GrowthStageBurdenModifier>();
        
        [Header("Care Quality Impact")]
        [Range(0.1f, 3.0f)] public float HighQualityCareMultiplier = 0.8f;
        [Range(1.1f, 5.0f)] public float LowQualityCareMultiplier = 2.0f;
        [Range(0.1f, 2.0f)] public float ExpertCareBonus = 0.6f;
        
        public float CalculatePlantBurden(PlantBurdenFactors factors)
        {
            var baseBurden = BasePlantBurden;
            var stageBurden = GetGrowthStageBurden(factors.GrowthStage);
            var complexityBurden = factors.StrainComplexity * ComplexityBurdenMultiplier;
            var careBurden = GetCareQualityMultiplier(factors.CareQuality);
            var automationReduction = GetAutomationReduction(factors.AutomationLevel);
            
            return (baseBurden + stageBurden + complexityBurden) * careBurden * automationReduction;
        }
        
        private float GetGrowthStageBurden(PlantGrowthStage stage)
        {
            var modifier = GrowthStageModifiers.Find(m => m.GrowthStage == stage);
            return modifier?.BurdenMultiplier ?? 1.0f;
        }
        
        private float GetCareQualityMultiplier(CareQuality quality)
        {
            return quality switch
            {
                CareQuality.Expert => ExpertCareBonus,
                CareQuality.High => HighQualityCareMultiplier,
                CareQuality.Medium => 1.0f,
                CareQuality.Low => LowQualityCareMultiplier,
                _ => 1.0f
            };
        }
        
        private float GetAutomationReduction(float automationLevel)
        {
            var reduction = automationLevel * AutomationBurdenReduction;
            return 1.0f - Mathf.Min(reduction, MaxAutomationReduction);
        }
    }
    
    [System.Serializable]
    public class GrowthStageBurdenModifier
    {
        public PlantGrowthStage GrowthStage;
        [Range(0.1f, 5.0f)] public float BurdenMultiplier = 1.0f;
        public string Description;
    }
    
    [System.Serializable]
    public class PlantBurdenFactors
    {
        public PlantGrowthStage GrowthStage;
        [Range(0.1f, 5.0f)] public float StrainComplexity = 1.0f;
        public CareQuality CareQuality = CareQuality.Medium;
        [Range(0f, 1f)] public float AutomationLevel = 0f;
        public bool HasSpecializedCare = false;
    }
    
    // PlantGrowthStage enum removed - now using ProjectChimera.Data.Genetics.PlantGrowthStage
    
    public enum CareQuality
    {
        Low,
        Medium,
        High,
        Expert
    }
}