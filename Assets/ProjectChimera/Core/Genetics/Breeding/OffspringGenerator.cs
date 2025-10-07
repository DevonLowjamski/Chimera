using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Genetics.Breeding
{
    /// <summary>
    /// Service for generating offspring genotypes through genetic crossover
    /// Implements Mendelian inheritance with mutation support
    /// </summary>
    public class OffspringGenerator : IOffspringGenerator
    {
        private const float DEFAULT_MUTATION_RATE = 0.001f; // 0.1% per gene

        /// <summary>
        /// Create offspring genotype from two parents
        /// </summary>
        public PlantGenotype GenerateOffspring(PlantGenotype parent1, PlantGenotype parent2, string offspringId = null)
        {
            if (parent1 == null || parent2 == null)
            {
                ChimeraLogger.LogWarning("Genetics", "Parent genotype is null - cannot create offspring", null);
                return null;
            }

            var offspring = new PlantGenotype();

            // Set identity
            offspring.GenotypeID = offspringId ?? System.Guid.NewGuid().ToString();
            offspring.Generation = Mathf.Max(parent1.Generation, parent2.Generation) + 1;
            offspring.IsFounder = false;
            offspring.CreationDate = System.DateTime.Now;

            // Determine strain origin (favor higher fitness parent)
            offspring.StrainOrigin = parent1.OverallFitness >= parent2.OverallFitness
                ? parent1.StrainOrigin
                : parent2.StrainOrigin;
            offspring.PlantSpecies = parent1.PlantSpecies;
            offspring.Cultivar = $"{parent1.Cultivar} × {parent2.Cultivar}";

            // Perform genetic crossover
            var crossoverResult = PerformCrossover(parent1.Genotype, parent2.Genotype);
            offspring.Genotype = crossoverResult;

            // Combine mutation histories
            offspring.Mutations = CombineMutationHistories(parent1.Mutations, parent2.Mutations);

            // Apply new mutations
            ApplyNewMutations(offspring);

            // Calculate offspring fitness
            offspring.OverallFitness = CalculateOffspringFitness(parent1, parent2);

            return offspring;
        }

        /// <summary>
        /// Generate multiple F1 offspring from two parents
        /// </summary>
        public List<PlantGenotype> GenerateF1Generation(PlantGenotype parent1, PlantGenotype parent2, int count)
        {
            var offspring = new List<PlantGenotype>();

            for (int i = 0; i < count; i++)
            {
                var offspringId = $"F1_{i}_{System.Guid.NewGuid().ToString().Substring(0, 4)}";
                var child = GenerateOffspring(parent1, parent2, offspringId);
                if (child != null)
                {
                    offspring.Add(child);
                }
            }

            return offspring;
        }

        // Private methods
        private Dictionary<string, object> PerformCrossover(
            Dictionary<string, object> parent1Genotype,
            Dictionary<string, object> parent2Genotype)
        {
            var parent1Dict = ExtractAlleleCouples(parent1Genotype);
            var parent2Dict = ExtractAlleleCouples(parent2Genotype);

            var offspringGenotype = new Dictionary<string, object>();
            var allGenes = parent1Dict.Keys.Union(parent2Dict.Keys);

            foreach (var geneId in allGenes)
            {
                AlleleCouple offspring;

                if (parent1Dict.ContainsKey(geneId) && parent2Dict.ContainsKey(geneId))
                {
                    // Random segregation - Mendelian inheritance
                    var parent1Couple = parent1Dict[geneId];
                    var parent2Couple = parent2Dict[geneId];

                    var allele1 = Random.value < 0.5f ? parent1Couple.allele1 : parent1Couple.allele2;
                    var allele2 = Random.value < 0.5f ? parent2Couple.allele1 : parent2Couple.allele2;

                    offspring = new AlleleCouple(allele1 ?? "unknown", allele2 ?? "unknown");
                }
                else if (parent1Dict.ContainsKey(geneId))
                {
                    offspring = parent1Dict[geneId];
                }
                else
                {
                    offspring = parent2Dict[geneId];
                }

                offspringGenotype[geneId] = offspring;
            }

            return offspringGenotype;
        }

        private Dictionary<string, AlleleCouple> ExtractAlleleCouples(Dictionary<string, object> genotype)
        {
            return genotype
                .Where(kvp => kvp.Value is AlleleCouple)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value as AlleleCouple);
        }

        private List<object> CombineMutationHistories(List<object> mutations1, List<object> mutations2)
        {
            var combined = new List<object>();
            combined.AddRange(mutations1);
            combined.AddRange(mutations2);
            return combined;
        }

        private void ApplyNewMutations(PlantGenotype offspring)
        {
            foreach (var geneId in offspring.Genotype.Keys.ToList())
            {
                if (Random.value < DEFAULT_MUTATION_RATE)
                {
                    var mutation = new MutationRecord
                    {
                        MutationID = System.Guid.NewGuid().ToString(),
                        GeneAffected = geneId,
                        MutationType = "PointMutation",
                        EffectMagnitude = Random.Range(-0.1f, 0.1f),
                        MutationDate = System.DateTime.Now,
                        IsBeneficial = Random.Range(-0.1f, 0.1f) > 0f
                    };

                    offspring.Mutations.Add(mutation);
                }
            }
        }

        private float CalculateOffspringFitness(PlantGenotype parent1, PlantGenotype parent2)
        {
            float averageFitness = (parent1.OverallFitness + parent2.OverallFitness) / 2f;
            float variation = Random.Range(-0.1f, 0.1f);

            return Mathf.Clamp(averageFitness + variation, 0.1f, 2f);
        }
    }
}
