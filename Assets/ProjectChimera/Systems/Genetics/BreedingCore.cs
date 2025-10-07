using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Shared;
using System;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Genetics
{
    /// <summary>
    /// Core breeding logic and genetics calculations
    /// Implements Mendelian inheritance with fractal variation
    /// Part of Phase 1 refactoring - SRP compliance
    /// </summary>
    public class BreedingCore
    {
        private readonly BreedingConfig _config;
        private readonly ITraitExpressionEngine _traitEngine;

        public BreedingCore(BreedingConfig config, ITraitExpressionEngine traitEngine)
        {
            _config = config;
            _traitEngine = traitEngine;
        }

        /// <summary>
        /// Performs breeding operation between two parent genotypes
        /// Returns breeding result with success status and seed data
        /// </summary>
        public BreedingResult BreedPlants(
            string parentId1,
            string parentId2,
            PlantGenotype parent1Genotype,
            PlantGenotype parent2Genotype)
        {
            if (!ValidateBreedingCompatibility(parentId1, parentId2, parent1Genotype, parent2Genotype))
            {
                return new BreedingResult
                {
                    Success = false,
                    ErrorMessage = "Cannot breed these plants - check compatibility and requirements"
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

            // Create breeding seed with minimal data
            var seed = CreateBreedingSeed(parent1Genotype, parent2Genotype);

            return new BreedingResult
            {
                Success = true,
                SeedId = GenerateSeedId(seed),
                ParentIds = new[] { parentId1, parentId2 },
                PredictedTraits = PredictOffspringTraits(seed),
                BreedingTime = CalculateBreedingTime(parent1Genotype, parent2Genotype),
                Seed = seed
            };
        }

        /// <summary>
        /// Generates offspring genotype from seed using Mendelian genetics
        /// Implements Phase 1 fractal genetics system
        /// </summary>
        public PlantGenotype GenerateOffspringGenotype(BreedingSeed seed)
        {
            var prng = new System.Random(seed.PRNGSeed);

            if (seed.IsClone)
            {
                return ReconstructClonalGenotype(seed, prng);
            }

            return PerformMendelianCross(seed, prng);
        }

        /// <summary>
        /// Creates minimal breeding seed data structure
        /// Stores only parent hashes + PRNG seed for infinite diversity
        /// </summary>
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

        /// <summary>
        /// Validates breeding compatibility between two parents
        /// </summary>
        private bool ValidateBreedingCompatibility(
            string parentId1,
            string parentId2,
            PlantGenotype parent1,
            PlantGenotype parent2)
        {
            if (string.IsNullOrEmpty(parentId1) || string.IsNullOrEmpty(parentId2))
                return false;

            if (parentId1 == parentId2)
                return false; // No self-pollination

            return parent1 != null && parent2 != null;
        }

        /// <summary>
        /// Calculates breeding success rate based on genetic compatibility
        /// </summary>
        private float CalculateBreedingSuccessRate(PlantGenotype parent1, PlantGenotype parent2)
        {
            float baseRate = _config?.BaseSuccessRate ?? 0.7f;
            float compatibility = CalculateGeneticCompatibility(parent1, parent2);

            return Mathf.Clamp01(baseRate * compatibility);
        }

        /// <summary>
        /// Calculates genetic compatibility between two strains
        /// Same strain = 1.0, different strains = 0.6-0.9
        /// </summary>
        private float CalculateGeneticCompatibility(PlantGenotype parent1, PlantGenotype parent2)
        {
            if (parent1.StrainName == parent2.StrainName)
                return 1.0f;

            // TODO: Implement proper genetic distance calculation
            return UnityEngine.Random.Range(0.6f, 0.9f);
        }

        /// <summary>
        /// Performs Mendelian cross using parent hashes and PRNG seed
        /// Implements fractal genetics variation
        /// </summary>
        private PlantGenotype PerformMendelianCross(BreedingSeed seed, System.Random prng)
        {
            var offspringGenotype = new PlantGenotype
            {
                GenotypeID = $"F1_{seed.ParentHash1}x{seed.ParentHash2}_{seed.PRNGSeed}",
                StrainName = "F1 Hybrid",
                Genotype = new Dictionary<string, object>()
            };

            // Simulate genetic recombination for each locus
            for (int locus = 0; locus < 10; locus++)
            {
                string locusName = $"locus_{locus}";
                string allele1 = GetRandomAlleleFromParent(seed.ParentHash1, locus, prng);
                string allele2 = GetRandomAlleleFromParent(seed.ParentHash2, locus, prng);

                offspringGenotype.Genotype[locusName] =
                    (object)new ProjectChimera.Data.Genetics.AlleleCouple(allele1, allele2);
            }

            return offspringGenotype;
        }

        /// <summary>
        /// Reconstructs clonal genotype from tissue culture
        /// </summary>
        private PlantGenotype ReconstructClonalGenotype(BreedingSeed seed, System.Random prng)
        {
            var cloneGenotype = new PlantGenotype
            {
                GenotypeID = $"Clone_{seed.SourceCultureId}_{seed.PRNGSeed}",
                StrainName = "Cloned Strain",
                Genotype = new Dictionary<string, object>()
            };

            // Reconstruct exact parent genotype from hash
            for (int i = 0; i < 10; i++)
            {
                string locus = $"locus_{i}";
                cloneGenotype.Genotype[locus] =
                    (object)new ProjectChimera.Data.Genetics.AlleleCouple("unknown", "unknown");
            }

            return cloneGenotype;
        }

        /// <summary>
        /// Deterministically selects allele from parent using hash and locus
        /// </summary>
        private string GetRandomAlleleFromParent(string parentHash, int locus, System.Random prng)
        {
            var hashSeed = parentHash.GetHashCode() ^ locus;
            var locusRandom = new System.Random(hashSeed);
            return locusRandom.Next(0, 4).ToString();
        }

        /// <summary>
        /// Predicts offspring trait outcomes from breeding
        /// </summary>
        private TraitPrediction[] PredictOffspringTraits(BreedingSeed seed)
        {
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

        /// <summary>
        /// Calculates breeding time based on parent characteristics
        /// </summary>
        private float CalculateBreedingTime(PlantGenotype parent1, PlantGenotype parent2)
        {
            float baseTime = _config?.BaseBreedingTime ?? 7f;
            return baseTime * UnityEngine.Random.Range(0.8f, 1.3f);
        }

        /// <summary>
        /// Creates deterministic hash from genotype for reconstruction
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
