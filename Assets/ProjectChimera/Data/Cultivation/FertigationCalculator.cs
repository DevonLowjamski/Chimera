using UnityEngine;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Genetics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.Data.Cultivation
{
    /// <summary>
    /// Handles all nutrient solution calculations and custom recipe creation.
    /// Implements professional hydroponic nutrient management protocols.
    /// </summary>
    public static class FertigationCalculator
    {
        /// <summary>
        /// Calculates optimal nutrient solution for specific plants and conditions.
        /// Implements professional hydroponic nutrient management protocols.
        /// </summary>
        public static NutrientSolution CalculateOptimalNutrientSolution(
            FertigationConfig config,
            PlantInstanceSO[] plants,
            EnvironmentalConditions environmentalConditions,
            CultivationZoneSO zone,
            WaterQualityData sourceWater)
        {
            var solution = new NutrientSolution
            {
                Timestamp = DateTime.Now,
                ZoneID = zone?.ZoneID ?? "Default",
                PlantCount = plants?.Length ?? 0
            };
            
            // Determine dominant growth stage for the zone
            PlantGrowthStage dominantStage = config.GetDominantGrowthStage(plants);
            var stageProfile = config.GetNutrientProfileForStage(dominantStage);
            
            // Start with base nutrient profile
            solution.TargetEC = stageProfile.TargetEC;
            solution.TargetpH = stageProfile.TargetpH;
            solution.NPKRatio = stageProfile.NPKRatio;
            
            // Apply environmental adjustments
            solution = ApplyEnvironmentalAdjustments(solution, environmentalConditions);
            
            // Apply strain-specific adjustments
            solution = ApplyStrainAdjustments(solution, plants);
            
            // Calculate individual nutrient concentrations
            solution.NutrientConcentrations = CalculateNutrientConcentrations(solution, stageProfile);
            
            // Apply water quality corrections
            solution = ApplyWaterQualityCorrections(solution, sourceWater);
            
            // Calculate dosing schedule
            solution.DosingSchedule = CalculateDosingSchedule(solution, stageProfile, environmentalConditions);
            
            // Add micronutrients and supplements
            solution.Micronutrients = CalculateMicronutrients(solution, dominantStage);
            solution.Supplements = CalculateSupplements(solution, plants, environmentalConditions);
            
            // Validate solution safety and effectiveness
            solution.ValidationResults = ValidateNutrientSolution(solution);
            
            return solution;
        }
        
        /// <summary>
        /// Creates a custom nutrient recipe for specific cultivation goals.
        /// </summary>
        public static NutrientRecipe CreateCustomNutrientRecipe(
            FertigationConfig config,
            string recipeName,
            PlantGrowthStage targetStage,
            PlantStrainSO strain,
            CultivationGoal goal,
            EnvironmentalConditions targetEnvironment)
        {
            var recipe = new NutrientRecipe
            {
                RecipeName = recipeName,
                TargetStage = targetStage,
                TargetStrain = strain,
                CultivationGoal = goal,
                CreatedDate = DateTime.Now
            };
            
            // Start with base profile for stage
            var baseProfile = config.GetNutrientProfileForStage(targetStage);
            recipe.BaseNutrientProfile = baseProfile;
            
            // Apply goal-specific modifications
            recipe.GoalModifications = ApplyGoalModifications(baseProfile, goal);
            
            // Apply strain-specific adjustments
            if (strain != null)
            {
                recipe.StrainAdjustments = ApplyStrainSpecificNutrientAdjustments(baseProfile, strain);
            }
            
            // Environmental optimizations
            recipe.EnvironmentalOptimizations = ApplyEnvironmentalNutrientOptimizations(baseProfile, targetEnvironment);
            
            // Calculate final nutrient concentrations
            recipe.FinalConcentrations = CalculateFinalRecipeConcentrations(recipe);
            
            // Predict outcomes
            recipe.PredictedOutcomes = PredictRecipeOutcomes(recipe, strain);
            
            // Add usage instructions
            recipe.UsageInstructions = GenerateUsageInstructions(recipe);
            
            return recipe;
        }
        
        #region Private Helper Methods
        
        private static NutrientSolution ApplyEnvironmentalAdjustments(NutrientSolution solution, EnvironmentalConditions conditions)
        {
            // Adjust EC based on temperature - higher temps need slightly lower EC
            if (conditions.Temperature > 25f)
            {
                solution.TargetEC *= 0.95f;
            }
            else if (conditions.Temperature < 18f)
            {
                solution.TargetEC *= 1.05f;
            }
            
            // Adjust pH based on humidity and CO2
            if (conditions.CO2Level > 800f)
            {
                solution.TargetpH -= 0.1f; // Slightly more acidic for better CO2 uptake
            }
            
            // Adjust feeding frequency based on humidity
            if (conditions.Humidity < 40f)
            {
                // Lower humidity means more water uptake - increase feeding frequency slightly
                solution.TotalVolume *= 1.1f;
            }
            else if (conditions.Humidity > 70f)
            {
                // Higher humidity means less water uptake - reduce feeding volume slightly
                solution.TotalVolume *= 0.95f;
            }
            
            return solution;
        }
        
        private static NutrientSolution ApplyStrainAdjustments(NutrientSolution solution, PlantInstanceSO[] plants)
        {
            if (plants == null || plants.Length == 0) return solution;
            
            // Calculate average strain nutrient preferences
            float avgNitrogenPreference = 1f;
            float avgPhosphorusPreference = 1f;
            float avgPotassiumPreference = 1f;
            
            int validPlants = 0;
            foreach (var plant in plants)
            {
                if (plant?.Strain != null)
                {
                    // These would come from strain data in a real implementation
                    avgNitrogenPreference += GetStrainNitrogenPreference(plant.Strain);
                    avgPhosphorusPreference += GetStrainPhosphorusPreference(plant.Strain);
                    avgPotassiumPreference += GetStrainPotassiumPreference(plant.Strain);
                    validPlants++;
                }
            }
            
            if (validPlants > 0)
            {
                avgNitrogenPreference /= validPlants + 1;
                avgPhosphorusPreference /= validPlants + 1;
                avgPotassiumPreference /= validPlants + 1;
                
                // Apply strain-specific NPK adjustments
                solution.NPKRatio = new Vector3(
                    solution.NPKRatio.x * avgNitrogenPreference,
                    solution.NPKRatio.y * avgPhosphorusPreference,
                    solution.NPKRatio.z * avgPotassiumPreference
                );
            }
            
            return solution;
        }
        
        private static Dictionary<NutrientType, float> CalculateNutrientConcentrations(NutrientSolution solution, NutrientProfile profile)
        {
            var concentrations = new Dictionary<NutrientType, float>();
            
            // Calculate base macronutrient concentrations
            concentrations[NutrientType.MacronutrientA] = profile.NitrogenPPM * solution.NPKRatio.x;
            concentrations[NutrientType.MacronutrientB] = profile.PhosphorusPPM * solution.NPKRatio.y;
            
            // Calculate secondary nutrients
            concentrations[NutrientType.CalciumMagnesium] = profile.CalciumPPM + profile.MagnesiumPPM;
            concentrations[NutrientType.Micronutrients] = 50f; // Base micronutrient concentration
            
            // Add bloom enhancer for flowering stage
            if (solution.NPKRatio.z > solution.NPKRatio.x) // More K than N suggests flowering
            {
                concentrations[NutrientType.BloomEnhancer] = profile.PotassiumPPM * solution.NPKRatio.z;
            }
            
            return concentrations;
        }
        
        private static NutrientSolution ApplyWaterQualityCorrections(NutrientSolution solution, WaterQualityData waterQuality)
        {
            if (waterQuality == null) return solution;
            
            // Adjust for source water EC
            if (waterQuality.pH < 6.5f || waterQuality.pH > 7.5f)
            {
                // Source water pH is off - may need more pH correction later
                solution.EstimatedCost *= 1.1f; // Account for additional pH correction costs
            }
            
            // Adjust for high TDS in source water
            if (waterQuality.TDS > 300f)
            {
                // High TDS source water - reduce nutrient concentrations slightly
                solution.TargetEC *= 0.95f;
            }
            
            return solution;
        }
        
        private static DosingSchedule CalculateDosingSchedule(NutrientSolution solution, NutrientProfile profile, EnvironmentalConditions conditions)
        {
            var events = new List<DosingEvent>();
            
            // Calculate dosing intervals based on feeding frequency
            int dailyFeedings = (int)profile.FeedingFrequency;
            float intervalHours = 24f / dailyFeedings;
            
            for (int i = 0; i < dailyFeedings; i++)
            {
                var doseTime = DateTime.Now.AddHours(i * intervalHours);
                
                foreach (var concentration in solution.NutrientConcentrations)
                {
                    events.Add(new DosingEvent
                    {
                        Time = doseTime,
                        Type = concentration.Key,
                        Amount = concentration.Value / dailyFeedings // Split daily dose across feedings
                    });
                }
            }
            
            return new DosingSchedule { Events = events.ToArray() };
        }
        
        private static Dictionary<string, float> CalculateMicronutrients(NutrientSolution solution, PlantGrowthStage stage)
        {
            var micronutrients = new Dictionary<string, float>
            {
                ["Iron"] = 2.0f,
                ["Manganese"] = 0.5f,
                ["Zinc"] = 0.15f,
                ["Copper"] = 0.02f,
                ["Boron"] = 0.3f,
                ["Molybdenum"] = 0.01f
            };
            
            // Adjust based on growth stage
            switch (stage)
            {
                case PlantGrowthStage.Seedling:
                    // Reduce all micronutrients for young plants
                    var keys = micronutrients.Keys.ToArray();
                    foreach (var key in keys)
                    {
                        micronutrients[key] *= 0.5f;
                    }
                    break;
                    
                case PlantGrowthStage.Flowering:
                    // Increase potassium-related micronutrients
                    micronutrients["Boron"] *= 1.2f;
                    micronutrients["Molybdenum"] *= 1.1f;
                    break;
            }
            
            return micronutrients;
        }
        
        private static Dictionary<string, float> CalculateSupplements(NutrientSolution solution, PlantInstanceSO[] plants, EnvironmentalConditions conditions)
        {
            var supplements = new Dictionary<string, float>();
            
            // Add silica for stronger stems
            supplements["Silica"] = 0.5f;
            
            // Add enzymes for better nutrient uptake
            supplements["Enzymes"] = 0.25f;
            
            // Add Cal-Mag if needed
            if (solution.TargetEC > 1.4f)
            {
                supplements["CalMag"] = 1.0f;
            }
            
            // Environmental supplements
            if (conditions.Temperature > 28f)
            {
                // Add cooling/stress supplements in high heat
                supplements["StressRelief"] = 0.3f;
            }
            
            return supplements;
        }
        
        private static ValidationResults ValidateNutrientSolution(NutrientSolution solution)
        {
            var warnings = new List<string>();
            var errors = new List<string>();
            
            // Validate EC range
            if (solution.TargetEC < 0.3f)
            {
                warnings.Add("EC is very low - plants may be underfed");
            }
            else if (solution.TargetEC > 2.5f)
            {
                errors.Add("EC is too high - risk of nutrient burn");
            }
            
            // Validate pH range
            if (solution.TargetpH < 5.0f)
            {
                errors.Add("pH is too acidic - risk of nutrient lockout");
            }
            else if (solution.TargetpH > 7.5f)
            {
                errors.Add("pH is too alkaline - poor nutrient uptake");
            }
            
            // Validate NPK ratios
            if (solution.NPKRatio.x > 10f || solution.NPKRatio.y > 10f || solution.NPKRatio.z > 10f)
            {
                warnings.Add("One or more NPK values are very high");
            }
            
            return new ValidationResults
            {
                IsValid = errors.Count == 0,
                Warnings = warnings.ToArray(),
                Errors = errors.ToArray()
            };
        }
        
        private static GoalModification[] ApplyGoalModifications(NutrientProfile baseProfile, CultivationGoal goal)
        {
            var modifications = new List<GoalModification>();
            
            switch (goal)
            {
                case CultivationGoal.MaxYield:
                    modifications.Add(new GoalModification { Parameter = "Nitrogen", Modification = 1.15f });
                    modifications.Add(new GoalModification { Parameter = "Phosphorus", Modification = 1.1f });
                    break;
                    
                case CultivationGoal.MaxPotency:
                    modifications.Add(new GoalModification { Parameter = "Potassium", Modification = 1.2f });
                    modifications.Add(new GoalModification { Parameter = "Phosphorus", Modification = 1.15f });
                    break;
                    
                case CultivationGoal.MaxTerpenes:
                    modifications.Add(new GoalModification { Parameter = "Sulfur", Modification = 1.3f });
                    modifications.Add(new GoalModification { Parameter = "Magnesium", Modification = 1.1f });
                    break;
                    
                case CultivationGoal.EnergyEfficient:
                    modifications.Add(new GoalModification { Parameter = "OverallNutrients", Modification = 0.9f });
                    break;
            }
            
            return modifications.ToArray();
        }
        
        private static StrainAdjustment[] ApplyStrainSpecificNutrientAdjustments(NutrientProfile baseProfile, PlantStrainSO strain)
        {
            var adjustments = new List<StrainAdjustment>();
            
            // These would come from actual strain data in a real implementation
            adjustments.Add(new StrainAdjustment { Parameter = "Nitrogen", Adjustment = GetStrainNitrogenPreference(strain) });
            adjustments.Add(new StrainAdjustment { Parameter = "Phosphorus", Adjustment = GetStrainPhosphorusPreference(strain) });
            adjustments.Add(new StrainAdjustment { Parameter = "Potassium", Adjustment = GetStrainPotassiumPreference(strain) });
            
            return adjustments.ToArray();
        }
        
        private static EnvironmentalOptimization[] ApplyEnvironmentalNutrientOptimizations(NutrientProfile baseProfile, EnvironmentalConditions environment)
        {
            var optimizations = new List<EnvironmentalOptimization>();
            
            if (environment.Temperature > 25f)
            {
                optimizations.Add(new EnvironmentalOptimization 
                { 
                    OptimizationType = "High Temperature", 
                    Value = 0.95f, 
                    Description = "Reduced EC for high temperature conditions" 
                });
            }
            
            if (environment.Humidity < 40f)
            {
                optimizations.Add(new EnvironmentalOptimization 
                { 
                    OptimizationType = "Low Humidity", 
                    Value = 1.1f, 
                    Description = "Increased feeding frequency for low humidity" 
                });
            }
            
            return optimizations.ToArray();
        }
        
        private static Dictionary<NutrientType, float> CalculateFinalRecipeConcentrations(NutrientRecipe recipe)
        {
            var concentrations = new Dictionary<NutrientType, float>();
            
            // Start with base profile
            var profile = recipe.BaseNutrientProfile;
            concentrations[NutrientType.MacronutrientA] = profile.NitrogenPPM;
            concentrations[NutrientType.MacronutrientB] = profile.PhosphorusPPM;
            concentrations[NutrientType.CalciumMagnesium] = profile.CalciumPPM;
            
            // Apply goal modifications
            foreach (var mod in recipe.GoalModifications)
            {
                ApplyModificationToConcentrations(concentrations, mod);
            }
            
            // Apply strain adjustments
            foreach (var adj in recipe.StrainAdjustments)
            {
                ApplyAdjustmentToConcentrations(concentrations, adj);
            }
            
            return concentrations;
        }
        
        private static FertigationRecipeOutcomes PredictRecipeOutcomes(NutrientRecipe recipe, PlantStrainSO strain)
        {
            // Simple prediction based on nutrient profile and goals
            float expectedYield = 1f;
            float expectedQuality = 1f;
            float resourceEfficiency = 0.85f;
            
            // Adjust based on cultivation goal
            switch (recipe.CultivationGoal)
            {
                case CultivationGoal.MaxYield:
                    expectedYield = 1.15f;
                    expectedQuality = 0.95f;
                    resourceEfficiency = 0.8f;
                    break;
                    
                case CultivationGoal.MaxQuality:
                    expectedYield = 0.95f;
                    expectedQuality = 1.2f;
                    resourceEfficiency = 0.75f;
                    break;
                    
                case CultivationGoal.EnergyEfficient:
                    expectedYield = 0.9f;
                    expectedQuality = 1f;
                    resourceEfficiency = 1.1f;
                    break;
            }
            
            return new FertigationRecipeOutcomes
            {
                ExpectedYield = expectedYield,
                ExpectedQuality = expectedQuality,
                ResourceEfficiency = resourceEfficiency
            };
        }
        
        private static UsageInstruction[] GenerateUsageInstructions(NutrientRecipe recipe)
        {
            var instructions = new List<UsageInstruction>
            {
                new UsageInstruction { Step = "1", Instruction = "Prepare clean water at room temperature" },
                new UsageInstruction { Step = "2", Instruction = "Add nutrients in the order specified" },
                new UsageInstruction { Step = "3", Instruction = "Mix thoroughly between additions" },
                new UsageInstruction { Step = "4", Instruction = "Check and adjust pH to target range" },
                new UsageInstruction { Step = "5", Instruction = "Verify final EC matches target" },
                new UsageInstruction { Step = "6", Instruction = "Apply according to dosing schedule" }
            };
            
            return instructions.ToArray();
        }
        
        // Helper methods for strain preferences (would be implemented with real data)
        private static float GetStrainNitrogenPreference(PlantStrainSO strain) => 1f + UnityEngine.Random.Range(-0.1f, 0.1f);
        private static float GetStrainPhosphorusPreference(PlantStrainSO strain) => 1f + UnityEngine.Random.Range(-0.1f, 0.1f);
        private static float GetStrainPotassiumPreference(PlantStrainSO strain) => 1f + UnityEngine.Random.Range(-0.1f, 0.1f);
        
        private static void ApplyModificationToConcentrations(Dictionary<NutrientType, float> concentrations, GoalModification mod)
        {
            // Apply modifications based on parameter type
            switch (mod.Parameter)
            {
                case "Nitrogen":
                    if (concentrations.ContainsKey(NutrientType.MacronutrientA))
                        concentrations[NutrientType.MacronutrientA] *= mod.Modification;
                    break;
                case "Phosphorus":
                    if (concentrations.ContainsKey(NutrientType.MacronutrientB))
                        concentrations[NutrientType.MacronutrientB] *= mod.Modification;
                    break;
                // Add other parameter types as needed
            }
        }
        
        private static void ApplyAdjustmentToConcentrations(Dictionary<NutrientType, float> concentrations, StrainAdjustment adj)
        {
            // Similar to modifications but for strain-specific adjustments
            ApplyModificationToConcentrations(concentrations, new GoalModification 
            { 
                Parameter = adj.Parameter, 
                Modification = adj.Adjustment 
            });
        }
        
        #endregion
    }
}