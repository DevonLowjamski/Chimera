using System;
using System.IO;
using System.Linq;
using UnityEngine;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Construction;

namespace ProjectChimera.Tests.Integration
{
    /// <summary>
    /// Helper methods for integration tests.
    /// Extracted to maintain Phase 0 file size compliance.
    /// </summary>
    public static class IntegrationTestHelpers
    {
        /// <summary>
        /// Counts occurrences of a code pattern in codebase.
        /// </summary>
        public static int CountCodePattern(string pattern)
        {
            var projectPath = Path.Combine(Application.dataPath, "ProjectChimera");
            if (!Directory.Exists(projectPath)) return 0;

            var csFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories);
            int count = 0;

            foreach (var file in csFiles)
            {
                var content = File.ReadAllText(file);
                var lines = content.Split('\n');

                foreach (var line in lines)
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(line, pattern))
                        count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Gets test strain for integration tests.
        /// </summary>
        public static PlantGenotype GetTestStrain(string strainName = "TestStrain_OG")
        {
            return new PlantGenotype
            {
                StrainName = strainName,
                THCPotential = 0.20f,
                CBDPotential = 0.02f,
                YieldMultiplier = 1.0f,
                GrowthRate = 1.0f,
                DiseaseResistance = 0.5f,
                BlockchainHash = "TEST_HASH_" + Guid.NewGuid().ToString().Substring(0, 8)
            };
        }

        /// <summary>
        /// Loads test schematic (placeholder for actual implementation).
        /// </summary>
        public static SchematicSO LoadTestSchematic(string schematicName)
        {
            var schematic = ScriptableObject.CreateInstance<SchematicSO>();
            schematic.SchematicId = schematicName;
            schematic.DisplayName = schematicName;
            schematic.Size = new Vector3Int(2, 2, 2);
            return schematic;
        }

        /// <summary>
        /// Captures current game state for comparison.
        /// </summary>
        public static GameStateSnapshot CaptureGameState()
        {
            var snapshot = new GameStateSnapshot
            {
                PlantCount = 0,
                ConstructionItemCount = 0,
                GeneticsCount = 0,
                PlayerCurrency = 0f,
                SkillPoints = 0,
                ActiveIPMInfestations = 0,
                ProcessingBatches = 0,
                CurrentFacilityId = "Unknown",
                GameTime = Time.time
            };

            try
            {
                var cultivation = Core.ServiceContainerFactory.Instance?.Resolve<Systems.Cultivation.ICultivationManager>();
                if (cultivation != null)
                    snapshot.PlantCount = cultivation.GetAllPlants()?.Count ?? 0;

                var construction = Core.ServiceContainerFactory.Instance?.Resolve<Systems.Construction.IConstructionManager>();
                if (construction != null)
                    snapshot.ConstructionItemCount = construction.GetTotalPlacedItems();

                // Additional state capture as needed
            }
            catch (Exception ex)
            {
                Core.Logging.ChimeraLogger.LogWarning("TEST", $"Failed to capture full game state: {ex.Message}", null);
            }

            return snapshot;
        }

        /// <summary>
        /// Compares two game state snapshots.
        /// </summary>
        public static bool CompareGameStates(GameStateSnapshot a, GameStateSnapshot b)
        {
            if (a == null || b == null) return false;

            return a.PlantCount == b.PlantCount &&
                   a.ConstructionItemCount == b.ConstructionItemCount &&
                   a.GeneticsCount == b.GeneticsCount &&
                   Mathf.Approximately(a.PlayerCurrency, b.PlayerCurrency) &&
                   a.SkillPoints == b.SkillPoints;
        }

        /// <summary>
        /// Clears current game state for testing.
        /// </summary>
        public static void ClearGameState()
        {
            try
            {
                var cultivation = Core.ServiceContainerFactory.Instance?.Resolve<Systems.Cultivation.ICultivationManager>();
                cultivation?.ClearAllPlants();

                var construction = Core.ServiceContainerFactory.Instance?.Resolve<Systems.Construction.IConstructionManager>();
                construction?.ClearAll();
            }
            catch (Exception ex)
            {
                Core.Logging.ChimeraLogger.LogWarning("TEST", $"Failed to clear game state: {ex.Message}", null);
            }
        }
    }
}
