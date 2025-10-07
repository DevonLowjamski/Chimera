using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Genetics;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Genetics
{
    /// <summary>
    /// Manages breeding seed storage and genotype generation
    /// Implements "infinite diversity from seeds" - minimal data, maximum variation
    /// Part of Phase 1 refactoring - SRP compliance
    /// </summary>
    public class BreedingSeedBank
    {
        private readonly Dictionary<string, BreedingSeed> _seedBank;
        private readonly BreedingCore _breedingCore;

        public BreedingSeedBank(BreedingCore breedingCore)
        {
            _seedBank = new Dictionary<string, BreedingSeed>();
            _breedingCore = breedingCore;
        }

        /// <summary>
        /// Stores breeding seed in the bank
        /// Seeds contain only parent hashes + PRNG seed for deterministic generation
        /// </summary>
        public bool StoreSeed(string seedId, BreedingSeed seed)
        {
            if (string.IsNullOrEmpty(seedId))
            {
                ChimeraLogger.LogWarning("GENETICS", "Cannot store seed - invalid seedId", null);
                return false;
            }

            if (_seedBank.ContainsKey(seedId))
            {
                ChimeraLogger.LogWarning("GENETICS",
                    $"Seed {seedId} already exists in bank - overwriting", null);
            }

            _seedBank[seedId] = seed;

            ChimeraLogger.Log("GENETICS",
                $"Seed stored: {seedId} ({(seed.IsClone ? "Clone" : "Cross")})", null);

            return true;
        }

        /// <summary>
        /// Generates actual plant genotype from stored seed data
        /// This is where "infinite diversity" is realized from minimal seed data
        /// </summary>
        public PlantGenotype GenerateGenotypeFromSeed(string seedId)
        {
            if (!_seedBank.TryGetValue(seedId, out var seed))
            {
                ChimeraLogger.LogWarning("GENETICS",
                    $"Cannot generate genotype - seed {seedId} not found in bank", null);
                return null;
            }

            // Use breeding core to generate genotype from seed
            var genotype = _breedingCore.GenerateOffspringGenotype(seed);

            ChimeraLogger.Log("GENETICS",
                $"Genotype generated from seed {seedId}: {genotype?.GenotypeID ?? "null"}", null);

            return genotype;
        }

        /// <summary>
        /// Retrieves seed data without generating genotype
        /// </summary>
        public BreedingSeed? GetSeed(string seedId)
        {
            if (_seedBank.TryGetValue(seedId, out var seed))
                return seed;

            return null;
        }

        /// <summary>
        /// Checks if seed exists in bank
        /// </summary>
        public bool HasSeed(string seedId) => _seedBank.ContainsKey(seedId);

        /// <summary>
        /// Gets total number of seeds in bank
        /// </summary>
        public int GetSeedCount() => _seedBank.Count;

        /// <summary>
        /// Gets all available seed IDs
        /// </summary>
        public string[] GetAvailableSeedIds() => new List<string>(_seedBank.Keys).ToArray();

        /// <summary>
        /// Removes seed from bank (e.g., when planted or destroyed)
        /// </summary>
        public bool RemoveSeed(string seedId)
        {
            if (_seedBank.Remove(seedId))
            {
                ChimeraLogger.Log("GENETICS", $"Seed removed from bank: {seedId}", null);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets seeds by type (crosses vs clones)
        /// </summary>
        public BreedingSeed[] GetSeedsByType(bool clonesOnly)
        {
            var results = new List<BreedingSeed>();

            foreach (var seed in _seedBank.Values)
            {
                if (seed.IsClone == clonesOnly)
                    results.Add(seed);
            }

            return results.ToArray();
        }

        /// <summary>
        /// Gets seeds created from specific parent
        /// </summary>
        public BreedingSeed[] GetSeedsFromParent(string parentHash)
        {
            var results = new List<BreedingSeed>();

            foreach (var seed in _seedBank.Values)
            {
                if (seed.ParentHash1 == parentHash || seed.ParentHash2 == parentHash)
                    results.Add(seed);
            }

            return results.ToArray();
        }

        /// <summary>
        /// Clears all seeds from bank
        /// </summary>
        public void ClearBank()
        {
            int count = _seedBank.Count;
            _seedBank.Clear();

            ChimeraLogger.Log("GENETICS", $"Seed bank cleared - {count} seeds removed", null);
        }

        /// <summary>
        /// Gets seed statistics for UI display
        /// </summary>
        public SeedBankStats GetStatistics()
        {
            int totalSeeds = _seedBank.Count;
            int cloneCount = 0;
            int crossCount = 0;

            foreach (var seed in _seedBank.Values)
            {
                if (seed.IsClone)
                    cloneCount++;
                else
                    crossCount++;
            }

            return new SeedBankStats
            {
                TotalSeeds = totalSeeds,
                CloneSeeds = cloneCount,
                CrossSeeds = crossCount
            };
        }
    }

    /// <summary>
    /// Seed bank statistics for UI display
    /// </summary>
    public struct SeedBankStats
    {
        public int TotalSeeds;
        public int CloneSeeds;
        public int CrossSeeds;
    }
}
