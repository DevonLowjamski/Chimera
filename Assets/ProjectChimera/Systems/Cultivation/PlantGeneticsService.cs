using ProjectChimera.Core.Logging;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Genetics;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// SIMPLE: Basic plant genetics service aligned with Project Chimera's genetics vision.
    /// Focuses on essential genetic trait handling for basic cultivation mechanics.
    /// </summary>
    public class PlantGeneticsService
    {
        [Header("Basic Genetics Settings")]
        [SerializeField] private bool _enableLogging = true;

        // Basic genetics tracking
        private readonly Dictionary<string, CannabisGenotype> _plantGenotypes = new Dictionary<string, CannabisGenotype>();
        private bool _isInitialized = false;

        /// <summary>
        /// Initialize the plant genetics service
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
            }
        }

        /// <summary>
        /// Shutdown the plant genetics service
        /// </summary>
        public void Shutdown()
        {
            if (!_isInitialized) return;

            _plantGenotypes.Clear();
            _isInitialized = false;

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
            }
        }

        /// <summary>
        /// Register a plant with its genotype
        /// </summary>
        public void RegisterPlantGenotype(string plantId, CannabisGenotype genotype)
        {
            if (string.IsNullOrEmpty(plantId) || genotype == null) return;

            _plantGenotypes[plantId] = genotype;

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
            }
        }

        /// <summary>
        /// Get genotype for a plant
        /// </summary>
        public CannabisGenotype GetPlantGenotype(string plantId)
        {
            return _plantGenotypes.TryGetValue(plantId, out var genotype) ? genotype : null;
        }

        /// <summary>
        /// Update plant genetics based on environmental factors
        /// </summary>
        public void UpdatePlantGenetics(string plantId, float environmentalFactor)
        {
            var genotype = GetPlantGenotype(plantId);
            if (genotype == null) return;

            // Simple genetics update - could be expanded based on environmental conditions
            // For now, just log the update
            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
            }
        }

        /// <summary>
        /// Calculate genetic fitness for a plant
        /// </summary>
        public float CalculateGeneticFitness(string plantId)
        {
            var genotype = GetPlantGenotype(plantId);
            if (genotype == null) return 0.5f; // Default fitness

            // Simple fitness calculation based on yield and potency
            float fitness = (genotype.YieldPotential / 200f + genotype.PotencyPotential / 25f) / 2f;
            return Mathf.Clamp01(fitness);
        }

        /// <summary>
        /// Breed two plants to create offspring genotype
        /// </summary>
        public CannabisGenotype BreedPlants(string parent1Id, string parent2Id, string offspringId)
        {
            var parent1 = GetPlantGenotype(parent1Id);
            var parent2 = GetPlantGenotype(parent2Id);

            if (parent1 == null || parent2 == null) return null;

            // Simple breeding - average traits with some variation
            var offspring = new CannabisGenotype
            {
                GenotypeId = offspringId,
                StrainName = $"Hybrid_{offspringId}",
                YieldPotential = (parent1.YieldPotential + parent2.YieldPotential) / 2f + Random.Range(-20f, 20f),
                PotencyPotential = (parent1.PotencyPotential + parent2.PotencyPotential) / 2f + Random.Range(-2f, 2f),
                FloweringTime = Mathf.RoundToInt((parent1.FloweringTime + parent2.FloweringTime) / 2f),
                MaxHeight = (parent1.MaxHeight + parent2.MaxHeight) / 2f + Random.Range(-0.2f, 0.2f),
                PlantType = Random.value > 0.5f ? parent1.PlantType : parent2.PlantType,
                IsCustomStrain = true
            };

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
            }

            return offspring;
        }

        /// <summary>
        /// Get all registered plant genotypes
        /// </summary>
        public Dictionary<string, CannabisGenotype> GetAllPlantGenotypes()
        {
            return new Dictionary<string, CannabisGenotype>(_plantGenotypes);
        }

        /// <summary>
        /// Remove plant genotype
        /// </summary>
        public void RemovePlantGenotype(string plantId)
        {
            if (_plantGenotypes.Remove(plantId))
            {
                if (_enableLogging)
                {
                    ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
                }
            }
        }

        /// <summary>
        /// Get genetics statistics
        /// </summary>
        public ServiceGeneticsStatistics GetGeneticsStatistics()
        {
            return new ServiceGeneticsStatistics
            {
                TotalPlants = _plantGenotypes.Count,
                AverageYield = _plantGenotypes.Count > 0 ? _plantGenotypes.Values.Average(g => g.YieldPotential) : 0f,
                AveragePotency = _plantGenotypes.Count > 0 ? _plantGenotypes.Values.Average(g => g.PotencyPotential) : 0f,
                CustomStrains = _plantGenotypes.Values.Count(g => g.IsCustomStrain)
            };
        }
    }

    /// <summary>
    /// Basic genetics statistics
    /// </summary>
    [System.Serializable]
    public class ServiceGeneticsStatistics
    {
        public int TotalPlants;
        public float AverageYield;
        public float AveragePotency;
        public int CustomStrains;
    }
}
