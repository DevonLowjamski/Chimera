using System;
using System.Collections.Generic;
using ProjectChimera.Data.Genetics;
using GeneticsCannabinoidProfile = ProjectChimera.Data.Genetics.CannabinoidProfile;
using GeneticsTerpeneProfile = ProjectChimera.Data.Genetics.TerpeneProfile;

namespace ProjectChimera.Systems.Cultivation
{
    [Serializable]
    public class SystemsHarvestResults
    {
        public string PlantId;
        public string PlantID => PlantId; // Compatibility property
        public float TotalYieldGrams;
        public float TotalYield => TotalYieldGrams; // Compatibility property
        public float TotalYieldKilograms => TotalYieldGrams / 1000f;
        public float QualityScore;
        public GeneticsCannabinoidProfile Cannabinoids;
        public Dictionary<string, float> CannabinoidProfile => 
            Cannabinoids != null ? 
            new Dictionary<string, float> { ["THC"] = Cannabinoids.thcContent, ["CBD"] = Cannabinoids.cbdContent, ["CBG"] = Cannabinoids.cbgContent, ["CBN"] = Cannabinoids.cbnContent } :
            new Dictionary<string, float>();
        public GeneticsTerpeneProfile TerpeneProfile;
        public Dictionary<string, float> Terpenes => 
            TerpeneProfile != null ? 
            new Dictionary<string, float> { ["Myrcene"] = TerpeneProfile.myrcene, ["Limonene"] = TerpeneProfile.limonene, ["Pinene"] = TerpeneProfile.pinene, ["Linalool"] = TerpeneProfile.linalool } :
            new Dictionary<string, float>();
        public DateTime HarvestDate;
        public bool IsSuccessful = true;
        public string FailureReason;
        public int FloweringDays;
        public float FinalHealth;

        // Convenience properties for cannabinoid percentages
        public float ThcPercentage => Cannabinoids?.ThcPercentage ?? 0f;
        public float CbdPercentage => Cannabinoids?.CbdPercentage ?? 0f;

        public SystemsHarvestResults()
        {
            HarvestDate = DateTime.Now;
        }
    }

    // CannabinoidProfile and TerpeneProfile classes are now imported from ProjectChimera.Data.Genetics namespace
}
