using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Genetics;
using System;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Genetics
{
    /// <summary>
    /// Manages tissue culture and micropropagation systems
    /// Implements Week 8 Phase 1 feature: Tissue culture & micropropagation
    /// Part of Phase 1 refactoring - SRP compliance
    /// </summary>
    public class TissueCultureManager
    {
        private readonly Dictionary<string, TissueCulture> _cultures;
        private readonly float _tissueCultureSuccessRate;
        private readonly float _micropropagationSuccessRate;

        // Events for tissue culture operations
        // Parameters: cultureId, cultureName, success, viability
        public event Action<string, string, bool, float> OnTissueCultureCreated;
        // Parameters: cultureId, requestedQuantity, successfulClones, cloneSeeds
        public event Action<string, int, int, BreedingSeed[]> OnMicropropagationCompleted;

        public TissueCultureManager(
            float tissueCultureSuccessRate = 0.85f,
            float micropropagationSuccessRate = 0.9f)
        {
            _cultures = new Dictionary<string, TissueCulture>();
            _tissueCultureSuccessRate = tissueCultureSuccessRate;
            _micropropagationSuccessRate = micropropagationSuccessRate;
        }

        /// <summary>
        /// Creates tissue culture from plant with clonal genetics
        /// Success rate based on configuration (default 85%)
        /// </summary>
        public bool CreateTissueCulture(
            string plantId,
            string cultureName,
            PlantGenotype plantGenotype)
        {
            if (plantGenotype == null)
            {
                ChimeraLogger.LogWarning("GENETICS",
                    $"Cannot create tissue culture - invalid genotype for plant {plantId}", null);
                return false;
            }

            // Simulate tissue culture success probability
            bool success = UnityEngine.Random.Range(0f, 1f) <= _tissueCultureSuccessRate;
            if (!success)
            {
                ChimeraLogger.Log("GENETICS",
                    $"Tissue culture creation failed for {cultureName} (success rate: {_tissueCultureSuccessRate:P0})", null);
                return false;
            }

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

            ChimeraLogger.Log("GENETICS",
                $"Tissue culture created: {cultureName} (ID: {culture.CultureId}, Viability: {culture.Viability:P0})", null);

            OnTissueCultureCreated?.Invoke(culture.CultureId, cultureName, true, culture.Viability);
            return true;
        }

        /// <summary>
        /// Micropropagates tissue culture to create identical clones
        /// Returns breeding seeds for clonal plants
        /// </summary>
        public bool Micropropagate(
            string cultureId,
            int quantity,
            out BreedingSeed[] cloneSeeds)
        {
            cloneSeeds = Array.Empty<BreedingSeed>();

            if (!_cultures.TryGetValue(cultureId, out var culture))
            {
                ChimeraLogger.LogWarning("GENETICS",
                    $"Cannot micropropagate - culture {cultureId} not found", null);
                return false;
            }

            // Viability affects success rate
            float adjustedSuccessRate = _micropropagationSuccessRate * culture.Viability;
            bool success = UnityEngine.Random.Range(0f, 1f) <= adjustedSuccessRate;

            if (!success)
            {
                ChimeraLogger.Log("GENETICS",
                    $"Micropropagation failed for {culture.Name} (adjusted rate: {adjustedSuccessRate:P0})", null);
                return false;
            }

            var resultSeeds = new List<BreedingSeed>();

            for (int i = 0; i < quantity; i++)
            {
                var cloneSeed = CreateClonalSeed(culture);
                resultSeeds.Add(cloneSeed);
            }

            cloneSeeds = resultSeeds.ToArray();

            ChimeraLogger.Log("GENETICS",
                $"Micropropagation successful: {quantity} clones created from {culture.Name}", null);

            OnMicropropagationCompleted?.Invoke(cultureId, quantity, cloneSeeds.Length, cloneSeeds);
            return true;
        }

        /// <summary>
        /// Creates clonal seed from tissue culture
        /// Clones have identical parent hashes for genetic reconstruction
        /// </summary>
        private BreedingSeed CreateClonalSeed(TissueCulture culture)
        {
            return new BreedingSeed
            {
                ParentHash1 = culture.ParentHash,
                ParentHash2 = culture.ParentHash, // Same hash for clones
                PRNGSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue),
                IsClone = true,
                SourceCultureId = culture.CultureId,
                CreationTime = Time.time
            };
        }

        /// <summary>
        /// Gets tissue culture by ID
        /// </summary>
        public TissueCulture? GetCulture(string cultureId)
        {
            if (_cultures.TryGetValue(cultureId, out var culture))
                return culture;

            return null;
        }

        /// <summary>
        /// Checks if tissue culture exists
        /// </summary>
        public bool HasCulture(string cultureId) => _cultures.ContainsKey(cultureId);

        /// <summary>
        /// Gets count of all tissue cultures
        /// </summary>
        public int GetCultureCount() => _cultures.Count;

        /// <summary>
        /// Gets all available culture IDs
        /// </summary>
        public string[] GetAvailableCultureIds() => new List<string>(_cultures.Keys).ToArray();

        /// <summary>
        /// Removes tissue culture from manager
        /// </summary>
        public bool RemoveCulture(string cultureId)
        {
            if (_cultures.Remove(cultureId))
            {
                ChimeraLogger.Log("GENETICS", $"Tissue culture removed: {cultureId}", null);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates tissue culture viability over time
        /// Cultures degrade if not properly maintained
        /// </summary>
        public void UpdateCultureViability(string cultureId, float viabilityDelta)
        {
            if (!_cultures.TryGetValue(cultureId, out var culture))
                return;

            culture.Viability = Mathf.Clamp01(culture.Viability + viabilityDelta);
            _cultures[cultureId] = culture;

            if (culture.Viability <= 0.1f)
            {
                ChimeraLogger.LogWarning("GENETICS",
                    $"Tissue culture {culture.Name} viability critically low: {culture.Viability:P0}", null);
            }
        }

        /// <summary>
        /// Generates unique culture identifier
        /// </summary>
        private string GenerateCultureId(string cultureName, string plantId)
        {
            return $"TC_{cultureName}_{plantId}_{Time.time:F0}";
        }

        /// <summary>
        /// Creates deterministic hash from genotype
        /// </summary>
        private string CalculateGenotypeHash(PlantGenotype genotype)
        {
            var hashData = $"{genotype.GenotypeID}_{genotype.StrainName}";

            foreach (var alleleCouple in genotype.Genotype.Values)
            {
                var couple = alleleCouple as ProjectChimera.Data.Genetics.AlleleCouple;
                hashData += $"_{couple?.Allele1 ?? "null"}_{couple?.Allele2 ?? "null"}";
            }

            return hashData.GetHashCode().ToString("X8");
        }

        /// <summary>
        /// Generates unique seed identifier
        /// </summary>
        private string GenerateSeedId(BreedingSeed seed)
        {
            return $"SEED_{seed.ParentHash1}x{seed.ParentHash2}_{seed.PRNGSeed:X8}";
        }
    }
}
