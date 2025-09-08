using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Shared;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;

// Type alias removed - Events namespace no longer exists after cleanup

namespace ProjectChimera.Data.Cultivation
{
    /// <summary>
    /// Interactive Plant - Core plant instance for cultivation gaming
    /// Represents an individual plant with full interactive capabilities
    /// </summary>
    [System.Serializable]
    public class InteractivePlant
    {
        [Header("Plant Identity")]
        public int PlantInstanceID;
        public string PlantName;
        public PlantStrainSO PlantStrain;
        public PlantPhenotypeSO Phenotype;
        
        [Header("Growth State")]
        public PlantGrowthStage CurrentGrowthStage = PlantGrowthStage.Seed;
        public float GrowthProgress = 0f;
        
        // Alias property for compatibility
        public PlantGrowthStage CurrentGrowthState 
        { 
            get => CurrentGrowthStage; 
            set => CurrentGrowthStage = value; 
        }
        [Range(0f, 100f)] public float CurrentHealth = 100f;
        [Range(0f, 100f)] public float MaxHealth = 100f;
        [Range(0f, 100f)] public float CurrentHydration = 50f;
        [Range(0f, 100f)] public float CurrentNutrition = 50f;
        [Range(0f, 100f)] public float CurrentStressLevel = 0f;
        [Range(0.1f, 5f)] public float CurrentGrowthRate = 1f;
        [Range(0f, 100f)] public float CurrentLightSatisfaction = 50f;
        
        [Header("Physical Properties")]
        public Vector3 Position;
        public Vector3 Size = Vector3.one;
        public float Height = 0.3f;
        public float Width = 0.2f;
        public float RootDepth = 0.1f;
        public int NodeCount = 2;
        public int BranchCount = 1;
        
        [Header("Environmental Needs")]
        public EnvironmentalRequirements EnvironmentalNeeds;
        public float OptimalTemperature = 24f;
        public float OptimalHumidity = 60f;
        public float OptimalLightIntensity = 400f;
        public float OptimalpH = 6.5f;
        public float OptimalWaterAmount = 100f;
        
        [Header("Care History")]
        public List<CareRecord> CareHistory = new List<CareRecord>();
        public System.DateTime LastWatered;
        public System.DateTime LastFed;
        public System.DateTime LastPruned;
        public System.DateTime LastInspected;
        public System.DateTime PlantedTime;
        
        [Header("Cultivation Data")]
        public CultivationContainer Container;
        public GrowingMedium GrowingMedium;
        public bool IsAutomated = false;
        public List<string> ActiveAutomationSystems = new List<string>(); // Changed from AutomationSystemType to string after Events namespace cleanup
        
        [Header("Gaming Properties")]
        public float PlayerAttentionLevel = 1f;
        public float CareQualityAverage = 0.5f;
        public int TotalCareActions = 0;
        public float SkillExperienceGenerated = 0f;
        
        // Runtime properties
        public bool IsSelected { get; set; } = false;
        public bool IsHighlighted { get; set; } = false;
        public bool CanReceiveCare { get; set; } = true;
        public bool IsInCrisis { get; set; } = false;
        
        // Events
        public System.Action<InteractivePlant> OnHealthChanged;
        public System.Action<InteractivePlant> OnGrowthStageChanged;
        public System.Action<InteractivePlant> OnCareReceived;
        public System.Action<InteractivePlant> OnStressLevelChanged;
        
        public InteractivePlant()
        {
            PlantInstanceID = UnityEngine.Random.Range(1000, 9999);
            LastWatered = System.DateTime.Now.AddHours(-12);
            LastFed = System.DateTime.Now.AddDays(-3);
            LastPruned = System.DateTime.Now.AddDays(-7);
            LastInspected = System.DateTime.Now.AddDays(-1);
        }
        
        /// <summary>
        /// Update plant state over time
        /// </summary>
        public void UpdatePlant(float deltaTime)
        {
            UpdateGrowth(deltaTime);
            UpdateNeeds(deltaTime);
            UpdateHealth(deltaTime);
            CheckGrowthStageProgression();
        }
        
        private void UpdateGrowth(float deltaTime)
        {
            if (CurrentHealth > 20f && CurrentHydration > 10f && CurrentNutrition > 10f)
            {
                var growthMultiplier = CalculateGrowthMultiplier();
                GrowthProgress += CurrentGrowthRate * growthMultiplier * deltaTime;
                
                // Update physical dimensions based on growth
                UpdatePhysicalDimensions();
            }
        }
        
        private void UpdateNeeds(float deltaTime)
        {
            // Decrease hydration over time
            var hydrationDecay = CalculateHydrationDecay(deltaTime);
            CurrentHydration = Mathf.Max(0f, CurrentHydration - hydrationDecay);
            
            // Decrease nutrition over time
            var nutritionDecay = CalculateNutritionDecay(deltaTime);
            CurrentNutrition = Mathf.Max(0f, CurrentNutrition - nutritionDecay);
            
            // Update stress based on unmet needs
            UpdateStressLevel();
        }
        
        private void UpdateHealth(float deltaTime)
        {
            var previousHealth = CurrentHealth;
            
            // Health decreases with high stress
            if (CurrentStressLevel > 50f)
            {
                var healthDecay = (CurrentStressLevel - 50f) * 0.1f * deltaTime;
                CurrentHealth = Mathf.Max(0f, CurrentHealth - healthDecay);
            }
            
            // Health recovers when stress is low and needs are met
            if (CurrentStressLevel < 20f && CurrentHydration > 60f && CurrentNutrition > 60f)
            {
                var healthRecovery = 2f * deltaTime;
                CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + healthRecovery);
            }
            
            if (Mathf.Abs(CurrentHealth - previousHealth) > 0.1f)
            {
                OnHealthChanged?.Invoke(this);
            }
        }
        
        private void CheckGrowthStageProgression()
        {
            var newStage = CalculateGrowthStage();
            if (newStage != CurrentGrowthStage)
            {
                var previousStage = CurrentGrowthStage;
                CurrentGrowthStage = newStage;
                OnGrowthStageProgressed(previousStage, newStage);
            }
        }
        
        private PlantGrowthStage CalculateGrowthStage()
        {
            return GrowthProgress switch
            {
                < 10f => PlantGrowthStage.Seed,
                < 25f => PlantGrowthStage.Germination,
                < 50f => PlantGrowthStage.Seedling,
                < 150f => PlantGrowthStage.Vegetative,
                < 200f => PlantGrowthStage.PreFlower,
                < 300f => PlantGrowthStage.Flowering,
                >= 300f => PlantGrowthStage.Mature,
                _ => PlantGrowthStage.Seedling
            };
        }
        
        private float CalculateGrowthMultiplier()
        {
            var healthFactor = CurrentHealth / 100f;
            var hydrationFactor = Mathf.Clamp01(CurrentHydration / 80f);
            var nutritionFactor = Mathf.Clamp01(CurrentNutrition / 80f);
            var stressFactor = Mathf.Clamp01(1f - (CurrentStressLevel / 100f));
            
            return healthFactor * hydrationFactor * nutritionFactor * stressFactor;
        }
        
        private float CalculateHydrationDecay(float deltaTime)
        {
            var baseDecay = 2f; // Base hydration loss per hour
            var stageMultiplier = CurrentGrowthStage switch
            {
                PlantGrowthStage.Flowering => 1.5f,
                PlantGrowthStage.Vegetative => 1.3f,
                PlantGrowthStage.Mature => 1.2f,
                _ => 1f
            };
            
            return baseDecay * stageMultiplier * deltaTime;
        }
        
        private float CalculateNutritionDecay(float deltaTime)
        {
            var baseDecay = 1f; // Base nutrition loss per hour
            var stageMultiplier = CurrentGrowthStage switch
            {
                PlantGrowthStage.Flowering => 2f,
                PlantGrowthStage.Vegetative => 1.5f,
                PlantGrowthStage.PreFlower => 1.3f,
                _ => 1f
            };
            
            return baseDecay * stageMultiplier * deltaTime;
        }
        
        private void UpdateStressLevel()
        {
            var previousStress = CurrentStressLevel;
            
            float stressLevel = 0f;
            
            // Hydration stress
            if (CurrentHydration < 30f)
                stressLevel += (30f - CurrentHydration) * 1.5f;
            
            // Nutrition stress
            if (CurrentNutrition < 40f)
                stressLevel += (40f - CurrentNutrition) * 1.2f;
            
            // Health stress
            if (CurrentHealth < 50f)
                stressLevel += (50f - CurrentHealth) * 0.8f;
            
            CurrentStressLevel = Mathf.Clamp(stressLevel, 0f, 100f);
            
            if (Mathf.Abs(CurrentStressLevel - previousStress) > 5f)
            {
                OnStressLevelChanged?.Invoke(this);
            }
        }
        
        private void UpdatePhysicalDimensions()
        {
            var growthFactor = GrowthProgress / 300f; // Normalize to mature plant
            
            Height = 0.3f + (growthFactor * 1.5f); // 0.3m to 1.8m
            Width = 0.2f + (growthFactor * 0.8f);  // 0.2m to 1.0m
            RootDepth = 0.1f + (growthFactor * 0.4f); // 0.1m to 0.5m
            
            NodeCount = Mathf.RoundToInt(2 + (growthFactor * 20f)); // 2 to 22 nodes
            BranchCount = Mathf.RoundToInt(1 + (growthFactor * 8f)); // 1 to 9 branches
        }
        
        /// <summary>
        /// Apply care action to the plant
        /// </summary>
        public bool ApplyCare(CareAction action, float quality)
        {
            if (!CanReceiveCare) return false;
            
            var careRecord = new CareRecord
            {
                CareType = action.TaskType,
                CareQuality = quality,
                Timestamp = System.DateTime.Now,
                PlayerSkillLevel = action.PlayerSkillLevel
            };
            
            CareHistory.Add(careRecord);
            TotalCareActions++;
            
            // Update care quality average
            CareQualityAverage = (CareQualityAverage * (TotalCareActions - 1) + quality) / TotalCareActions;
            
            // Apply care effects
            ApplyCareEffectsInternal(action.TaskType, quality);
            
            // Update last care timestamps
            UpdateLastCareTimestamp(action.TaskType);
            
            OnCareReceived?.Invoke(this);
            return true;
        }
        
        /// <summary>
        /// Apply care effects to the plant (public method)
        /// </summary>
        public void ApplyCareEffects(CultivationTaskType careType, float quality)
        {
            ApplyCareEffectsInternal(careType, quality);
        }
        
        private void ApplyCareEffectsInternal(CultivationTaskType careType, float quality)
        {
            switch (careType)
            {
                case CultivationTaskType.Watering:
                    var hydrationIncrease = 20f * quality;
                    CurrentHydration = Mathf.Min(100f, CurrentHydration + hydrationIncrease);
                    break;
                    
                case CultivationTaskType.Feeding:
                    var nutritionIncrease = 15f * quality;
                    CurrentNutrition = Mathf.Min(100f, CurrentNutrition + nutritionIncrease);
                    break;
                    
                case CultivationTaskType.Pruning:
                    var healthIncrease = 5f * quality;
                    CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + healthIncrease);
                    var stressReduction = 10f * quality;
                    CurrentStressLevel = Mathf.Max(0f, CurrentStressLevel - stressReduction);
                    break;
                    
                case CultivationTaskType.Training:
                    var growthBoost = 0.1f * quality;
                    CurrentGrowthRate = Mathf.Min(5f, CurrentGrowthRate + growthBoost);
                    break;
            }
        }
        
        private void UpdateLastCareTimestamp(CultivationTaskType careType)
        {
            var now = System.DateTime.Now;
            switch (careType)
            {
                case CultivationTaskType.Watering:
                    LastWatered = now;
                    break;
                case CultivationTaskType.Feeding:
                    LastFed = now;
                    break;
                case CultivationTaskType.Pruning:
                    LastPruned = now;
                    break;
                case CultivationTaskType.Monitoring:
                    LastInspected = now;
                    break;
            }
        }
        
        private void OnGrowthStageProgressed(PlantGrowthStage previousStage, PlantGrowthStage newStage)
        {
            OnGrowthStageChanged?.Invoke(this);
        }
        
        /// <summary>
        /// Get current plant needs assessment
        /// </summary>
        public PlantNeedsAssessment GetNeedsAssessment()
        {
            return new PlantNeedsAssessment
            {
                WaterNeed = CalculateWaterNeed(),
                NutrientNeed = CalculateNutrientNeed(),
                CareNeed = CalculateCareNeed(),
                AttentionRequired = CurrentStressLevel > 60f || CurrentHealth < 40f,
                CriticalCondition = CurrentHealth < 20f || CurrentStressLevel > 80f
            };
        }
        
        
        private float CalculateWaterNeed()
        {
            return Mathf.Clamp01((100f - CurrentHydration) / 100f);
        }
        
        private float CalculateNutrientNeed()
        {
            return Mathf.Clamp01((100f - CurrentNutrition) / 100f);
        }
        
        private float CalculateCareNeed()
        {
            var timeSinceLastCare = (System.DateTime.Now - GetLastCareTime()).TotalHours;
            return Mathf.Clamp01((float)timeSinceLastCare / 24f); // Normalize to daily care
        }
        
        private System.DateTime GetLastCareTime()
        {
            var lastTimes = new[] { LastWatered, LastFed, LastPruned, LastInspected };
            var mostRecent = lastTimes[0];
            
            foreach (var time in lastTimes)
            {
                if (time > mostRecent)
                    mostRecent = time;
            }
            
            return mostRecent;
        }
    }
    
    #region Supporting Data Structures
    
    [System.Serializable]
    public class CareRecord
    {
        public CultivationTaskType CareType;
        public float CareQuality;
        public System.DateTime Timestamp;
        public float PlayerSkillLevel;
    }
    
    [System.Serializable]
    public class PlantNeedsAssessment
    {
        public float WaterNeed;
        public float NutrientNeed;
        public float CareNeed;
        public bool AttentionRequired;
        public bool CriticalCondition;
    }
    
    [System.Serializable]
    public class EnvironmentalRequirements
    {
        public float MinTemperature = 18f;
        public float MaxTemperature = 30f;
        public float MinHumidity = 40f;
        public float MaxHumidity = 70f;
        public float MinLightIntensity = 200f;
        public float MaxLightIntensity = 800f;
        public float MinpH = 5.5f;
        public float MaxpH = 7.5f;
    }
    
    [System.Serializable]
    public class CultivationContainer
    {
        public string ContainerName;
        public ContainerType Type;
        public float Volume; // Liters
        public float DrainageRating;
        public bool HasReservoir;
    }
    
    [System.Serializable]
    public class GrowingMedium
    {
        public string MediumName;
        public MediumType Type;
        public float pHLevel;
        public float DrainageRate;
        public float NutrientRetention;
        public float AerationLevel;
    }
    
    public enum MediumType
    {
        Soil,
        Coco,
        Rockwool,
        Perlite,
        Hydroponic,
        Aeroponic
    }
    
    // ContainerType enum is defined in CultivationZoneSO.cs to avoid duplicates
    
    #endregion
}