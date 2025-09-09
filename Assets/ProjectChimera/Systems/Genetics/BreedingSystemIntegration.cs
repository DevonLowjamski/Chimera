using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Shared;
using ProjectChimera.Systems.Services.Core;
using System.Collections.Generic;
using DataAlleleCouple = ProjectChimera.Data.Genetics.AlleleCouple;
using System;

namespace ProjectChimera.Systems.Genetics
{
    /// <summary>
    /// Phase 2.2.4: Breeding System Integration
    /// Implements "infinite diversity from seeds" concept with minimal data serialization
    /// Each seed contains only: parent hash + PRNG seed for deterministic trait generation
    /// </summary>
    public class BreedingSystemIntegration : MonoBehaviour
    {
        [Header("Breeding Configuration")]
        [SerializeField] private BreedingConfig _breedingConfig;
        [SerializeField] private bool _enableAdvancedGenetics = true;
        [SerializeField] private bool _useQuantumGenetics = false;

        [Header("Success Rates")]
        [Range(0f, 1f)]
        [SerializeField] private float _baseBreedingSuccessRate = 0.7f;
        [Range(0f, 1f)]
        [SerializeField] private float _tissueCultureSuccessRate = 0.85f;
        [Range(0f, 1f)]
        [SerializeField] private float _micropropagationSuccessRate = 0.9f;

        private IGeneticsService _geneticsService;
        private ITraitExpressionEngine _traitEngine;
        private Dictionary<string, BreedingSeed> _seedBank = new Dictionary<string, BreedingSeed>();
        private Dictionary<string, TissueCulture> _cultures = new Dictionary<string, TissueCulture>();

        // Events for breeding system integration
        public event Action<BreedingResult> OnBreedingCompleted;
        public event Action<string, TissueCulture> OnTissueCultureCreated;
        public event Action<string, string[]> OnMicropropagationCompleted;

        private void Awake()
        {
            InitializeBreedingSystem();
        }

        private void InitializeBreedingSystem()
        {
            // Try to resolve genetics service through proper DI container
            _geneticsService = ServiceContainerFactory.Instance?.TryResolve<IGeneticsService>();
            
            // If not found, try through service coordinator
            if (_geneticsService == null)
            {
                var serviceCoordinator = ServiceContainerFactory.Instance?.TryResolve<ServiceLayerCoordinator>();
                if (serviceCoordinator != null)
                {
                    // Request that ServiceLayerCoordinator properly expose the genetics service
                    // instead of accessing it through dangerous reflection
                    ChimeraLogger.LogWarning("[BreedingSystemIntegration] IGeneticsService not found in DI container. ServiceLayerCoordinator should register it properly.");
                }
            }

            _traitEngine = ServiceContainerFactory.Instance?.TryResolve<TraitExpressionEngine>();

            if (_breedingConfig == null)
            {
                ChimeraLogger.LogWarning("[BreedingSystemIntegration] No breeding config assigned - using default settings");
                _breedingConfig = CreateDefaultBreedingConfig();
            }
        }

        /// <summary>
        /// Breeds two plants using minimal data approach
        /// Each seed contains only parent hashes + PRNG seed for infinite diversity
        /// </summary>
        public BreedingResult BreedPlants(string parentId1, string parentId2)
        {
            if (!CanBreedPlants(parentId1, parentId2))
            {
                return new BreedingResult
                {
                    Success = false,
                    ErrorMessage = "Cannot breed these plants - check compatibility and requirements"
                };
            }

            // Get parent genotype data
            var parent1Genotype = GetPlantGenotype(parentId1);
            var parent2Genotype = GetPlantGenotype(parentId2);

            if (parent1Genotype == null || parent2Genotype == null)
            {
                return new BreedingResult
                {
                    Success = false,
                    ErrorMessage = "Could not retrieve parent genotype data"
                };
            }

            // Calculate breeding success probability
            float successRate = CalculateBreedingSuccessRate(parent1Genotype, parent2Genotype);
            bool breedingSuccess = UnityEngine.Random.Range(0f, 1f) <= successRate;

            if (!breedingSuccess)
            {
                return new BreedingResult
                {
                    Success = false,
                    ErrorMessage = "Breeding attempt failed - genetic incompatibility or environmental factors"
                };
            }

            // Create new seed with minimal data approach
            var newSeed = CreateBreedingSeed(parent1Genotype, parent2Genotype);
            string seedId = GenerateSeedId(newSeed);

            // Store seed in bank
            _seedBank[seedId] = newSeed;

            var result = new BreedingResult
            {
                Success = true,
                SeedId = seedId,
                ParentIds = new[] { parentId1, parentId2 },
                PredictedTraits = PredictOffspringTraits(newSeed),
                BreedingTime = CalculateBreedingTime(parent1Genotype, parent2Genotype)
            };

            OnBreedingCompleted?.Invoke(result);
            return result;
        }

        /// <summary>
        /// Creates tissue culture from plant with clonal genetics
        /// </summary>
        public bool CreateTissueCulture(string plantId, string cultureName)
        {
            var plantGenotype = GetPlantGenotype(plantId);
            if (plantGenotype == null) return false;

            bool success = UnityEngine.Random.Range(0f, 1f) <= _tissueCultureSuccessRate;
            if (!success) return false;

            var culture = new TissueCulture
            {
                CultureId = GenerateCultureId(cultureName, plantId),
                Name = cultureName,
                SourcePlantId = plantId,
                ParentHash = CalculateGenotypeHash(plantGenotype),
                CreationTime = Time.time,
                Viability = UnityEngine.Random.Range(0.8f, 1.0f)
            };

            _cultures[culture.CultureId] = culture;
            OnTissueCultureCreated?.Invoke(culture.CultureId, culture);

            return true;
        }

        /// <summary>
        /// Micropropagates tissue culture to create identical clones
        /// </summary>
        public bool Micropropagate(string cultureId, int quantity, out string[] seedIds)
        {
            seedIds = new string[0];

            if (!_cultures.TryGetValue(cultureId, out var culture))
                return false;

            bool success = UnityEngine.Random.Range(0f, 1f) <= _micropropagationSuccessRate;
            if (!success) return false;

            var resultSeedIds = new List<string>();

            for (int i = 0; i < quantity; i++)
            {
                // Create clonal seed with same genetic hash
                var cloneSeed = new BreedingSeed
                {
                    ParentHash1 = culture.ParentHash,
                    ParentHash2 = culture.ParentHash, // Same hash for clones
                    PRNGSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue),
                    IsClone = true,
                    SourceCultureId = cultureId
                };

                string seedId = GenerateSeedId(cloneSeed);
                _seedBank[seedId] = cloneSeed;
                resultSeedIds.Add(seedId);
            }

            seedIds = resultSeedIds.ToArray();
            OnMicropropagationCompleted?.Invoke(cultureId, seedIds);

            return true;
        }

        /// <summary>
        /// Generates actual plant genotype from seed data when planted
        /// This is where the "infinite diversity" is realized from minimal seed data
        /// </summary>
        public PlantGenotype GenerateGenotypeFromSeed(string seedId)
        {
            if (!_seedBank.TryGetValue(seedId, out var seed))
            {
                ChimeraLogger.LogError($"[BreedingSystemIntegration] Seed {seedId} not found in seed bank");
                return null;
            }

            // Use deterministic PRNG for consistent results
            var prng = new System.Random(seed.PRNGSeed);

            if (seed.IsClone)
            {
                // For clones, reconstruct exact parent genotype
                return ReconstructClonalGenotype(seed, prng);
            }
            else
            {
                // For crosses, perform Mendelian genetics simulation
                return PerformMendelianCross(seed, prng);
            }
        }

        private BreedingSeed CreateBreedingSeed(PlantGenotype parent1, PlantGenotype parent2)
        {
            return new BreedingSeed
            {
                ParentHash1 = CalculateGenotypeHash(parent1),
                ParentHash2 = CalculateGenotypeHash(parent2),
                PRNGSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue),
                IsClone = false,
                CreationTime = Time.time
            };
        }

        private string CalculateGenotypeHash(PlantGenotype genotype)
        {
            // Create deterministic hash from genotype that can be used to reconstruct genetics
            var hashData = $"{genotype.GenotypeID}_{genotype.StrainName}";

            foreach (var alleleCouple in genotype.Genotype.Values)
            {
                var couple = alleleCouple as ProjectChimera.Data.Genetics.AlleleCouple;
                hashData += $"_{couple?.Allele1 ?? "null"}_{couple?.Allele2 ?? "null"}";
            }

            return hashData.GetHashCode().ToString("X8");
        }

        private PlantGenotype ReconstructClonalGenotype(BreedingSeed seed, System.Random prng)
        {
            // For clones, we reconstruct the exact parent genotype
            // In a real implementation, this would use the hash to rebuild genetics
            var cloneGenotype = new PlantGenotype
            {
                GenotypeID = $"Clone_{seed.SourceCultureId}_{seed.PRNGSeed}",
                StrainName = "Cloned Strain",
                Genotype = new Dictionary<string, object>()
            };

            // Reconstruct alleles from parent hash (simplified for demo)
            for (int i = 0; i < 10; i++)
            {
                string locus = $"locus_{i}";
                cloneGenotype.Genotype[locus] = (object)new ProjectChimera.Data.Genetics.AlleleCouple("unknown", "unknown");
            }

            return cloneGenotype;
        }

        private PlantGenotype PerformMendelianCross(BreedingSeed seed, System.Random prng)
        {
            // Perform Mendelian genetics cross using parent hashes
            var offspringGenotype = new PlantGenotype
            {
                GenotypeID = $"F1_{seed.ParentHash1}x{seed.ParentHash2}_{seed.PRNGSeed}",
                StrainName = "F1 Hybrid",
                Genotype = new Dictionary<string, object>()
            };

            // Simulate genetic recombination
            for (int locus = 0; locus < 10; locus++)
            {
                // Simplified Mendelian inheritance
                string locusName = $"locus_{locus}";
                string allele1 = GetRandomAlleleFromParent(seed.ParentHash1, locus, prng);
                string allele2 = GetRandomAlleleFromParent(seed.ParentHash2, locus, prng);

                offspringGenotype.Genotype[locusName] = (object)new ProjectChimera.Data.Genetics.AlleleCouple(allele1, allele2);
            }

            return offspringGenotype;
        }

        private string GetRandomAlleleFromParent(string parentHash, int locus, System.Random prng)
        {
            // Use parent hash and locus to deterministically select allele
            var hashSeed = parentHash.GetHashCode() ^ locus;
            var locusRandom = new System.Random(hashSeed);
            return locusRandom.Next(0, 4).ToString();
        }

        private float CalculateBreedingSuccessRate(PlantGenotype parent1, PlantGenotype parent2)
        {
            float baseRate = _baseBreedingSuccessRate;

            // Adjust success rate based on genetic compatibility
            float compatibility = CalculateGeneticCompatibility(parent1, parent2);
            float adjustedRate = baseRate * compatibility;

            return Mathf.Clamp01(adjustedRate);
        }

        private float CalculateGeneticCompatibility(PlantGenotype parent1, PlantGenotype parent2)
        {
            // Simplified compatibility calculation
            if (parent1.StrainName == parent2.StrainName)
                return 1.0f; // Same strain = high compatibility

            // Different strains have variable compatibility
            return UnityEngine.Random.Range(0.6f, 0.9f);
        }

        private TraitPrediction[] PredictOffspringTraits(BreedingSeed seed)
        {
            // Predict likely trait outcomes from cross
            var predictions = new List<TraitPrediction>();

            foreach (TraitType trait in Enum.GetValues(typeof(TraitType)))
            {
                predictions.Add(new TraitPrediction
                {
                    Trait = trait,
                    PredictedValue = UnityEngine.Random.Range(0.3f, 0.9f),
                    Confidence = UnityEngine.Random.Range(0.6f, 0.95f)
                });
            }

            return predictions.ToArray();
        }

        private float CalculateBreedingTime(PlantGenotype parent1, PlantGenotype parent2)
        {
            // Calculate time needed for breeding process
            float baseTime = _breedingConfig != null ? _breedingConfig.BaseBreedingTime : 7f;

            // Adjust based on plant characteristics
            return baseTime * UnityEngine.Random.Range(0.8f, 1.3f);
        }

        private bool CanBreedPlants(string parentId1, string parentId2)
        {
            if (string.IsNullOrEmpty(parentId1) || string.IsNullOrEmpty(parentId2))
                return false;

            if (parentId1 == parentId2)
                return false; // No self-pollination in basic version

            var parent1 = GetPlantGenotype(parentId1);
            var parent2 = GetPlantGenotype(parentId2);

            return parent1 != null && parent2 != null;
        }

        private PlantGenotype GetPlantGenotype(string plantId)
        {
            // In real implementation, this would fetch from plant manager
            // For now, create a mock genotype
            return new PlantGenotype
            {
                GenotypeID = plantId,
                StrainName = $"Strain_{plantId}",
                Genotype = new Dictionary<string, object>()
            };
        }

        private string GenerateSeedId(BreedingSeed seed)
        {
            return $"SEED_{seed.ParentHash1}x{seed.ParentHash2}_{seed.PRNGSeed:X8}";
        }

        private string GenerateCultureId(string cultureName, string plantId)
        {
            return $"TC_{cultureName}_{plantId}_{Time.time:F0}";
        }

        private BreedingConfig CreateDefaultBreedingConfig()
        {
            var config = ScriptableObject.CreateInstance<BreedingConfig>();

            // Use proper API instead of dangerous reflection
            config.InitializeBreedingConfig(
                baseBreedingTime: 7f,
                maxBreedingAttempts: 3,
                requireCompatibilityCheck: true
            );

            return config;
        }

        // Public API for service integration
        public bool HasSeed(string seedId) => _seedBank.ContainsKey(seedId);
        public bool HasCulture(string cultureId) => _cultures.ContainsKey(cultureId);
        public int GetSeedCount() => _seedBank.Count;
        public int GetCultureCount() => _cultures.Count;
        public string[] GetAvailableSeedIds() => new List<string>(_seedBank.Keys).ToArray();
        public string[] GetAvailableCultureIds() => new List<string>(_cultures.Keys).ToArray();
    }

    /// <summary>
    /// Minimal seed data structure for "infinite diversity from seeds"
    /// Only stores parent hashes + PRNG seed, actual genetics generated on demand
    /// </summary>
    [System.Serializable]
    public struct BreedingSeed
    {
        public string ParentHash1;      // Hash of first parent's genetics
        public string ParentHash2;      // Hash of second parent's genetics
        public int PRNGSeed;           // Seed for deterministic trait generation
        public bool IsClone;           // Whether this is a clonal propagation
        public string SourceCultureId; // For clones, the tissue culture source
        public float CreationTime;     // When seed was created
    }

    /// <summary>
    /// Tissue culture data for clonal propagation
    /// </summary>
    [System.Serializable]
    public struct TissueCulture
    {
        public string CultureId;
        public string Name;
        public string SourcePlantId;
        public string ParentHash;      // Genetics hash of source plant
        public float CreationTime;
        public float Viability;        // 0-1, affects propagation success
    }

    /// <summary>
    /// Result of breeding operation
    /// </summary>
    [System.Serializable]
    public struct BreedingResult
    {
        public bool Success;
        public string SeedId;
        public string[] ParentIds;
        public string ErrorMessage;
        public TraitPrediction[] PredictedTraits;
        public float BreedingTime;
    }

    /// <summary>
    /// Predicted trait outcome from breeding
    /// </summary>
    [System.Serializable]
    public struct TraitPrediction
    {
        public TraitType Trait;
        public float PredictedValue;   // 0-1 predicted expression
        public float Confidence;       // 0-1 confidence in prediction
    }
}
