using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Shared;
using ProjectChimera.Systems.Services.Core;
using System;

namespace ProjectChimera.Systems.Genetics
{
    /// <summary>
    /// Phase 1 Refactored: Breeding System Integration
    /// Lightweight coordinator that delegates to specialized components
    /// Follows SRP - reduced from 466 to ~160 lines
    /// </summary>
    public class BreedingSystemIntegration : MonoBehaviour
    {
        [Header("Breeding Configuration")]
        [SerializeField] private BreedingConfig _breedingConfig;
        [SerializeField] private bool _enableAdvancedGenetics = true;

        [Header("Success Rates")]
        [Range(0f, 1f)]
        [SerializeField] private float _tissueCultureSuccessRate = 0.85f;
        [Range(0f, 1f)]
        [SerializeField] private float _micropropagationSuccessRate = 0.9f;

        // Specialized components (SRP refactoring)
        private BreedingCore _breedingCore;
        private TissueCultureManager _tissueCultureManager;
        private BreedingSeedBank _seedBank;

        // Service dependencies
        private IGeneticsService _geneticsService;
        private ITraitExpressionEngine _traitEngine;

        // Public events
        public event Action<BreedingResult> OnBreedingCompleted;
        // Parameters: cultureId, cultureName, success, viability
        public event Action<string, string, bool, float> OnTissueCultureCreated;
        // Parameters: cultureId, requestedQuantity, successfulClones, cloneSeeds
        public event Action<string, int, int, BreedingSeed[]> OnMicropropagationCompleted;

        private void Awake()
        {
            InitializeBreedingSystem();
        }

        private void InitializeBreedingSystem()
        {
            // Resolve dependencies through ServiceContainer
            _geneticsService = ServiceContainerFactory.Instance?.TryResolve<IGeneticsService>();
            _traitEngine = ServiceContainerFactory.Instance?.TryResolve<TraitExpressionEngine>();

            if (_breedingConfig == null)
            {
                ChimeraLogger.LogWarning("GENETICS",
                    "No BreedingConfig assigned - creating default configuration", this);
                _breedingConfig = CreateDefaultBreedingConfig();
            }

            // Initialize specialized components
            _breedingCore = new BreedingCore(_breedingConfig, _traitEngine);
            _tissueCultureManager = new TissueCultureManager(
                _tissueCultureSuccessRate,
                _micropropagationSuccessRate);
            _seedBank = new BreedingSeedBank(_breedingCore);

            // Wire up events from components
            _tissueCultureManager.OnTissueCultureCreated += (id, name, success, viability) =>
                OnTissueCultureCreated?.Invoke(id, name, success, viability);
            _tissueCultureManager.OnMicropropagationCompleted += (id, requestedQty, successfulClones, seeds) =>
                OnMicropropagationCompleted?.Invoke(id, requestedQty, successfulClones, seeds);

            ChimeraLogger.Log("GENETICS", "Breeding system initialized successfully", this);
        }

        /// <summary>
        /// Breeds two plants - delegates to BreedingCore
        /// </summary>
        public BreedingResult BreedPlants(string parentId1, string parentId2)
        {
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

            var result = _breedingCore.BreedPlants(
                parentId1, parentId2,
                parent1Genotype, parent2Genotype);

            if (result.Success)
            {
                _seedBank.StoreSeed(result.SeedId, result.Seed);
                OnBreedingCompleted?.Invoke(result);
            }

            return result;
        }

        /// <summary>
        /// Creates tissue culture - delegates to TissueCultureManager
        /// </summary>
        public bool CreateTissueCulture(string plantId, string cultureName)
        {
            var plantGenotype = GetPlantGenotype(plantId);
            return _tissueCultureManager.CreateTissueCulture(plantId, cultureName, plantGenotype);
        }

        /// <summary>
        /// Micropropagates tissue culture - delegates to TissueCultureManager
        /// </summary>
        public bool Micropropagate(string cultureId, int quantity, out string[] seedIds)
        {
            seedIds = Array.Empty<string>();

            if (!_tissueCultureManager.Micropropagate(cultureId, quantity, out var cloneSeeds))
                return false;

            // Store all clone seeds in seed bank
            var resultIds = new string[cloneSeeds.Length];
            for (int i = 0; i < cloneSeeds.Length; i++)
            {
                string seedId = $"SEED_{cloneSeeds[i].ParentHash1}x{cloneSeeds[i].ParentHash2}_{cloneSeeds[i].PRNGSeed:X8}";
                _seedBank.StoreSeed(seedId, cloneSeeds[i]);
                resultIds[i] = seedId;
            }

            seedIds = resultIds;
            return true;
        }

        /// <summary>
        /// Generates genotype from seed - delegates to BreedingSeedBank
        /// </summary>
        public PlantGenotype GenerateGenotypeFromSeed(string seedId)
        {
            return _seedBank.GenerateGenotypeFromSeed(seedId);
        }

        /// <summary>
        /// Retrieves plant genotype from genetics service
        /// TODO: Replace mock implementation with actual service call
        /// </summary>
        private PlantGenotype GetPlantGenotype(string plantId)
        {
            // TODO: Integrate with IGeneticsService once available
            // For now, return mock genotype for testing
            return new PlantGenotype
            {
                GenotypeID = plantId,
                StrainName = $"Strain_{plantId}",
                Genotype = new System.Collections.Generic.Dictionary<string, object>()
            };
        }

        /// <summary>
        /// Creates default breeding configuration
        /// </summary>
        private BreedingConfig CreateDefaultBreedingConfig()
        {
            var config = ScriptableObject.CreateInstance<BreedingConfig>();
            config.InitializeBreedingConfig(
                baseBreedingTime: 7f,
                maxBreedingAttempts: 3,
                requireCompatibilityCheck: true
            );
            return config;
        }

        // Public API for seed bank access
        public bool HasSeed(string seedId) => _seedBank.HasSeed(seedId);
        public int GetSeedCount() => _seedBank.GetSeedCount();
        public string[] GetAvailableSeedIds() => _seedBank.GetAvailableSeedIds();

        // Public API for tissue culture access
        public bool HasCulture(string cultureId) => _tissueCultureManager.HasCulture(cultureId);
        public int GetCultureCount() => _tissueCultureManager.GetCultureCount();
        public string[] GetAvailableCultureIds() => _tissueCultureManager.GetAvailableCultureIds();

        // Statistics for UI
        public SeedBankStats GetSeedBankStats() => _seedBank.GetStatistics();
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

        // Backward-compatible aliases for UI layer
        public string CultureName { get => Name; set => Name = value; }
        public float CurrentViability { get => Viability; set => Viability = value; }
        public PlantGenotype SourceGenotype { get; set; } // Extended property for UI
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
        public BreedingSeed Seed;      // The created seed data
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
