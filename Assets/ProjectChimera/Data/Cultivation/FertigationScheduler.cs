using UnityEngine;
using ProjectChimera.Data.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.Data.Cultivation
{
    /// <summary>
    /// Handles irrigation scheduling and timing calculations.
    /// Implements smart scheduling algorithms for optimal plant hydration.
    /// </summary>
    public static class FertigationScheduler
    {
        /// <summary>
        /// Creates irrigation schedule based on plant needs and environmental conditions.
        /// Implements smart scheduling algorithms for optimal plant hydration.
        /// </summary>
        public static IrrigationSchedule CreateIrrigationSchedule(
            FertigationConfig config,
            PlantInstanceSO[] plants,
            EnvironmentalConditions environment,
            CultivationZoneSO zone,
            int scheduleDays = 7)
        {
            var schedule = new IrrigationSchedule
            {
                StartDate = DateTime.Now,
                DurationDays = scheduleDays,
                ZoneID = zone?.ZoneID ?? "Default",
                ScheduleMode = config.ScheduleMode
            };
            
            // Calculate base irrigation frequency
            float baseFrequency = CalculateBaseIrrigationFrequency(config, plants, environment);
            
            // Create daily schedules
            var dailySchedules = new List<DailyIrrigationSchedule>();
            
            for (int day = 0; day < scheduleDays; day++)
            {
                var dailySchedule = new DailyIrrigationSchedule
                {
                    Day = day,
                    Date = DateTime.Now.AddDays(day)
                };
                
                // Calculate irrigation events for the day
                dailySchedule.IrrigationEvents = CalculateDailyIrrigationEvents(
                    config, plants, environment, baseFrequency, day);
                
                // Calculate total daily water volume
                dailySchedule.TotalWaterVolume = CalculateDailyWaterVolume(
                    dailySchedule.IrrigationEvents, plants.Length);
                
                // Add nutrient schedule
                dailySchedule.NutrientSchedule = CalculateDailyNutrientSchedule(
                    config, plants, dailySchedule.IrrigationEvents);
                
                dailySchedules.Add(dailySchedule);
            }
            
            schedule.DailySchedules = dailySchedules.ToArray();
            
            // Add environmental adaptation rules
            schedule.AdaptationRules = CreateAdaptationRules(environment);
            
            // Calculate expected outcomes
            schedule.ExpectedOutcomes = PredictScheduleOutcomes(schedule, plants);
            
            return schedule;
        }
        
        #region Private Helper Methods
        
        private static float CalculateBaseIrrigationFrequency(FertigationConfig config, PlantInstanceSO[] plants, EnvironmentalConditions environment)
        {
            float baseFrequency = config.BaseIrrigationFrequency;
            
            // Adjust based on environmental conditions
            if (environment.Temperature > 28f)
            {
                baseFrequency *= 1.3f; // More frequent watering in high heat
            }
            else if (environment.Temperature < 18f)
            {
                baseFrequency *= 0.8f; // Less frequent watering in cool conditions
            }
            
            if (environment.Humidity < 40f)
            {
                baseFrequency *= 1.2f; // More frequent in low humidity
            }
            else if (environment.Humidity > 70f)
            {
                baseFrequency *= 0.9f; // Less frequent in high humidity
            }
            
            // Adjust based on growth stage
            if (plants != null && plants.Length > 0)
            {
                var dominantStage = config.GetDominantGrowthStage(plants);
                switch (dominantStage)
                {
                    case PlantGrowthStage.Seedling:
                        baseFrequency *= 0.7f; // Young plants need less frequent watering
                        break;
                    case PlantGrowthStage.Flowering:
                        baseFrequency *= 1.1f; // Flowering plants need more water
                        break;
                    case PlantGrowthStage.Vegetative:
                        // No adjustment needed for vegetative stage
                        break;
                }
            }
            
            // Apply irrigation curve by stage if configured
            if (config.IrrigationCurveByStage != null && plants != null && plants.Length > 0)
            {
                float stageProgress = GetAverageStageProgress(plants);
                float curveMultiplier = config.IrrigationCurveByStage.Evaluate(stageProgress);
                baseFrequency *= curveMultiplier;
            }
            
            return Mathf.Clamp(baseFrequency, 1f, 12f); // Between 1 and 12 times per day
        }
        
        private static IrrigationEvent[] CalculateDailyIrrigationEvents(
            FertigationConfig config, 
            PlantInstanceSO[] plants, 
            EnvironmentalConditions environment, 
            float baseFrequency, 
            int dayOffset)
        {
            var events = new List<IrrigationEvent>();
            int eventsPerDay = Mathf.RoundToInt(baseFrequency);
            
            // Calculate interval between irrigation events
            float intervalHours = 24f / eventsPerDay;
            
            // Start irrigation events after sunrise (assume 6 AM)
            DateTime startTime = DateTime.Now.Date.AddDays(dayOffset).AddHours(6);
            
            for (int i = 0; i < eventsPerDay; i++)
            {
                var eventTime = startTime.AddHours(i * intervalHours);
                
                // Calculate duration based on plant needs and environmental conditions
                float duration = CalculateIrrigationDuration(config, plants, environment, i, eventsPerDay);
                
                // Calculate volume based on plant count and stage
                float volume = CalculateIrrigationVolume(config, plants, duration);
                
                events.Add(new IrrigationEvent
                {
                    Time = eventTime.TimeOfDay,
                    Duration = duration,
                    Volume = volume
                });
            }
            
            // Adjust timing to avoid night-time irrigation (stop before 8 PM)
            var adjustedEvents = new List<IrrigationEvent>();
            foreach (var evt in events)
            {
                if (evt.Time.TotalHours < 20) // Before 8 PM
                {
                    adjustedEvents.Add(evt);
                }
            }
            
            return adjustedEvents.ToArray();
        }
        
        private static float CalculateDailyWaterVolume(IrrigationEvent[] events, int plantCount)
        {
            if (events == null || events.Length == 0) return 0f;
            
            float totalVolume = 0f;
            foreach (var evt in events)
            {
                totalVolume += evt.Volume;
            }
            
            return totalVolume;
        }
        
        private static NutrientScheduleEntry[] CalculateDailyNutrientSchedule(
            FertigationConfig config,
            PlantInstanceSO[] plants, 
            IrrigationEvent[] irrigationEvents)
        {
            var entries = new List<NutrientScheduleEntry>();
            
            if (irrigationEvents == null || irrigationEvents.Length == 0) 
                return entries.ToArray();
            
            // Get nutrient profile for dominant stage
            var dominantStage = config.GetDominantGrowthStage(plants);
            var profile = config.GetNutrientProfileForStage(dominantStage);
            
            // Create nutrient entries aligned with irrigation events
            foreach (var irrigationEvent in irrigationEvents)
            {
                // Add base nutrients (A & B) with each irrigation
                entries.Add(new NutrientScheduleEntry
                {
                    Time = irrigationEvent.Time,
                    NutrientType = NutrientType.MacronutrientA,
                    Amount = profile.NitrogenPPM * irrigationEvent.Volume / 1000f // Convert to ml
                });
                
                entries.Add(new NutrientScheduleEntry
                {
                    Time = irrigationEvent.Time,
                    NutrientType = NutrientType.MacronutrientB,
                    Amount = profile.PhosphorusPPM * irrigationEvent.Volume / 1000f
                });
                
                // Add Cal-Mag every other feeding
                if (entries.Count % 2 == 0)
                {
                    entries.Add(new NutrientScheduleEntry
                    {
                        Time = irrigationEvent.Time,
                        NutrientType = NutrientType.CalciumMagnesium,
                        Amount = profile.CalciumPPM * irrigationEvent.Volume / 2000f
                    });
                }
                
                // Add bloom enhancer for flowering stage
                if (dominantStage == PlantGrowthStage.Flowering)
                {
                    entries.Add(new NutrientScheduleEntry
                    {
                        Time = irrigationEvent.Time,
                        NutrientType = NutrientType.BloomEnhancer,
                        Amount = profile.PotassiumPPM * irrigationEvent.Volume / 1500f
                    });
                }
            }
            
            return entries.ToArray();
        }
        
        private static AdaptationRule[] CreateAdaptationRules(EnvironmentalConditions environment)
        {
            var rules = new List<AdaptationRule>();
            
            // Temperature-based adaptations
            rules.Add(new AdaptationRule
            {
                Condition = "Temperature > 30°C",
                Adaptation = "Increase irrigation frequency by 20% and reduce nutrient concentration by 5%"
            });
            
            rules.Add(new AdaptationRule
            {
                Condition = "Temperature < 16°C",
                Adaptation = "Decrease irrigation frequency by 15% and reduce feeding to every other irrigation"
            });
            
            // Humidity-based adaptations
            rules.Add(new AdaptationRule
            {
                Condition = "Humidity < 35%",
                Adaptation = "Increase irrigation frequency by 15% and monitor for water stress"
            });
            
            rules.Add(new AdaptationRule
            {
                Condition = "Humidity > 75%",
                Adaptation = "Decrease irrigation frequency by 10% and improve ventilation"
            });
            
            // Light-based adaptations
            rules.Add(new AdaptationRule
            {
                Condition = "Light intensity > 800 PPFD",
                Adaptation = "Increase feeding frequency to support high photosynthesis rates"
            });
            
            return rules.ToArray();
        }
        
        private static ScheduleOutcomes PredictScheduleOutcomes(IrrigationSchedule schedule, PlantInstanceSO[] plants)
        {
            float totalWaterUse = 0f;
            float totalNutrientUse = 0f;
            
            // Calculate total water and nutrient usage
            foreach (var dailySchedule in schedule.DailySchedules)
            {
                totalWaterUse += dailySchedule.TotalWaterVolume;
                
                if (dailySchedule.NutrientSchedule != null)
                {
                    foreach (var nutrientEntry in dailySchedule.NutrientSchedule)
                    {
                        totalNutrientUse += nutrientEntry.Amount;
                    }
                }
            }
            
            // Predict yield impact based on irrigation frequency and consistency
            float expectedYieldImpact = 1f;
            
            // Check for consistent irrigation timing
            var allEvents = schedule.DailySchedules.SelectMany(d => d.IrrigationEvents).ToArray();
            if (allEvents.Length > 1)
            {
                float avgInterval = 24f / (allEvents.Length / schedule.DurationDays);
                if (avgInterval >= 4f && avgInterval <= 8f) // Ideal range
                {
                    expectedYieldImpact = 1.05f; // 5% yield boost
                }
                else if (avgInterval < 2f || avgInterval > 12f) // Too frequent or infrequent
                {
                    expectedYieldImpact = 0.95f; // 5% yield penalty
                }
            }
            
            return new ScheduleOutcomes
            {
                ExpectedWaterUse = totalWaterUse,
                ExpectedNutrientUse = totalNutrientUse,
                ExpectedYieldImpact = expectedYieldImpact
            };
        }
        
        private static float CalculateIrrigationDuration(
            FertigationConfig config,
            PlantInstanceSO[] plants, 
            EnvironmentalConditions environment, 
            int eventIndex, 
            int totalEvents)
        {
            // Base duration from configuration or default
            float baseDuration = 2f; // 2 minutes default
            
            // Get nutrient profile for dominant stage
            if (plants != null && plants.Length > 0)
            {
                var dominantStage = config.GetDominantGrowthStage(plants);
                var profile = config.GetNutrientProfileForStage(dominantStage);
                baseDuration = profile.FeedingDuration;
            }
            
            // Adjust based on environmental conditions
            if (environment.Temperature > 28f)
            {
                baseDuration *= 1.1f; // Longer duration in hot conditions
            }
            
            if (environment.Humidity < 40f)
            {
                baseDuration *= 1.05f; // Slightly longer in low humidity
            }
            
            // Adjust first and last feedings of the day
            if (eventIndex == 0) // First feeding
            {
                baseDuration *= 1.2f; // Longer morning feeding
            }
            else if (eventIndex == totalEvents - 1) // Last feeding
            {
                baseDuration *= 0.8f; // Shorter evening feeding
            }
            
            return Mathf.Clamp(baseDuration, 0.5f, 10f); // Between 30 seconds and 10 minutes
        }
        
        private static float CalculateIrrigationVolume(FertigationConfig config, PlantInstanceSO[] plants, float duration)
        {
            // Base volume calculation: ~500ml per plant per feeding
            float baseVolumePerPlant = 500f;
            int plantCount = plants?.Length ?? 1;
            
            // Adjust based on plant growth stage
            if (plants != null && plants.Length > 0)
            {
                var dominantStage = config.GetDominantGrowthStage(plants);
                switch (dominantStage)
                {
                    case PlantGrowthStage.Seedling:
                        baseVolumePerPlant = 200f; // Less water for young plants
                        break;
                    case PlantGrowthStage.Vegetative:
                        baseVolumePerPlant = 500f; // Standard amount
                        break;
                    case PlantGrowthStage.Flowering:
                        baseVolumePerPlant = 600f; // More water for flowering
                        break;
                }
            }
            
            // Scale by duration
            float volumeMultiplier = duration / 2f; // 2 minutes = 1x multiplier
            
            return baseVolumePerPlant * plantCount * volumeMultiplier;
        }
        
        private static float GetAverageStageProgress(PlantInstanceSO[] plants)
        {
            if (plants == null || plants.Length == 0) return 0.5f;
            
            float totalProgress = 0f;
            int validPlants = 0;
            
            foreach (var plant in plants)
            {
                if (plant != null)
                {
                    // Map growth stages to progress values
                    switch (plant.CurrentGrowthStage)
                    {
                        case PlantGrowthStage.Seedling:
                            totalProgress += 0.1f;
                            break;
                        case PlantGrowthStage.Vegetative:
                            totalProgress += 0.5f;
                            break;
                        case PlantGrowthStage.Flowering:
                            totalProgress += 0.9f;
                            break;
                    }
                    validPlants++;
                }
            }
            
            return validPlants > 0 ? totalProgress / validPlants : 0.5f;
        }
        
        #endregion
    }
}