using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Shared;
using ProjectChimera.Core.Logging;
using DataAlleleCouple = ProjectChimera.Data.Genetics.AlleleCouple;

using DataMutationRecord = ProjectChimera.Data.Genetics.MutationRecord;

namespace ProjectChimera.Systems.Genetics
{
    /// <summary>
    /// Phase 2.2.2: Genotype Factory for Type Normalization
    /// Handles conversion between GenotypeDataSO and PlantGenotype
    /// Ensures serialization compatibility and unified genotype usage
    /// </summary>
    public static class GenotypeFactory
    {
        /// <summary>
        /// Convert GenotypeDataSO to runtime PlantGenotype
        /// Primary method for SO → runtime conversion
        /// </summary>
        /// <param name="genotypeDataSO">ScriptableObject genotype data</param>
        /// <returns>Runtime PlantGenotype instance</returns>
        public static PlantGenotype CreateFromScriptableObject(GenotypeDataSO genotypeDataSO)
        {
            if (genotypeDataSO == null)
            {
                ChimeraLogger.LogError("[GenotypeFactory] GenotypeDataSO is null");
                return null;
            }

            var plantGenotype = new PlantGenotype();

            // Map identity properties
            plantGenotype.GenotypeID = genotypeDataSO.IndividualID ?? genotypeDataSO.name;
            plantGenotype.StrainOrigin = genotypeDataSO.ParentStrain;
            plantGenotype.Generation = genotypeDataSO.Generation;
            plantGenotype.IsFounder = genotypeDataSO.Generation <= 1;
            plantGenotype.CreationDate = System.DateTime.Now;

            // Map genetic properties
            plantGenotype.OverallFitness = genotypeDataSO.OverallFitness;
            plantGenotype.PlantSpecies = genotypeDataSO.Species?.speciesName ?? "Cannabis";
            plantGenotype.Cultivar = genotypeDataSO.ParentStrain?.strainName ?? "Unknown";

            // Convert gene pairs to allele couples
            plantGenotype.Genotype = new Dictionary<string, object>();
            foreach (var genePair in genotypeDataSO.GenePairs)
            {
                if (genePair.Gene != null)
                {
                    var alleleCouple = new ProjectChimera.Data.Genetics.AlleleCouple(genePair.Allele1?.name ?? "unknown", genePair.Allele2?.name ?? "unknown");

                    plantGenotype.Genotype[genePair.Gene.name] = (object)alleleCouple;
                }
            }

            // Convert mutation history
            plantGenotype.Mutations = new List<object>();
            foreach (var mutation in genotypeDataSO.MutationHistory)
            {
                var mutationRecord = (object)new ProjectChimera.Data.Genetics.MutationRecord
                {
                    MutationId = System.Guid.NewGuid().ToString(),
                    AffectedGene = mutation.Gene?.name ?? "Unknown",
                    MutationType = "PointMutation", // Default since original doesn't have this field
                    EffectMagnitude = 0f, // Default since original doesn't have this field
                    OccurrenceDate = System.DateTime.Now,
                    IsBeneficial = false
                };

                plantGenotype.Mutations.Add((object)mutationRecord);
            }

            // Map additional plant properties
            MapSpecificTraits(genotypeDataSO, plantGenotype);

            return plantGenotype;
        }

        /// <summary>
        /// Convert runtime PlantGenotype back to GenotypeDataSO
        /// For saving runtime changes back to ScriptableObject format
        /// </summary>
        /// <param name="plantGenotype">Runtime PlantGenotype</param>
        /// <param name="targetSO">Target ScriptableObject to update (optional)</param>
        /// <returns>Updated or new GenotypeDataSO</returns>
        public static GenotypeDataSO CreateScriptableObject(PlantGenotype plantGenotype, GenotypeDataSO targetSO = null)
        {
            if (plantGenotype == null)
            {
                ChimeraLogger.LogError("[GenotypeFactory] PlantGenotype is null");
                return null;
            }

            GenotypeDataSO genotypeDataSO = targetSO;
            if (genotypeDataSO == null)
            {
                genotypeDataSO = ScriptableObject.CreateInstance<GenotypeDataSO>();
            }

            // Convert allele couples back to gene pairs
            var genePairs = new List<GenePair>();
            foreach (var kvp in plantGenotype.Genotype)
            {
                var geneDefinition = FindGeneDefinition(kvp.Key);
                if (geneDefinition != null)
                {
                    var genePair = new GenePair
                    {
                        Gene = geneDefinition,
                        Allele1 = null, // kvp.Value.Allele1,
                        Allele2 = null  // kvp.Value.Allele2
                    };

                    genePairs.Add(genePair);
                }
            }

            // Convert mutations back
            var mutationHistory = new List<MutationEvent>();
            foreach (var mutation in plantGenotype.Mutations)
            {
                if (mutation is ProjectChimera.Data.Genetics.MutationRecord mutationRecord)
                {
                    var mutationEvent = new MutationEvent
                    {
                        Gene = FindGeneDefinition(mutationRecord.AffectedGene),
                        FromAllele = null, // Not available in MutationRecord
                        ToAllele = null,   // Not available in MutationRecord
                        MutationTime = 0f, // Not available in MutationRecord
                        Generation = 1, // Default since not available in current MutationRecord
                        MutationDescription = $"Mutation effect: {mutationRecord.EffectMagnitude}"
                    };

                    mutationHistory.Add(mutationEvent);
                }
            }

            // Use proper API instead of dangerous reflection
            genotypeDataSO.InitializeGenotype(
                individualID: plantGenotype.GenotypeID,
                parentStrain: plantGenotype.StrainOrigin as PlantStrainSO,
                generation: plantGenotype.Generation,
                overallFitness: plantGenotype.OverallFitness,
                genePairs: genePairs,
                mutationHistory: mutationHistory
            );

            return genotypeDataSO;
        }

        /// <summary>
        /// Create a new PlantGenotype from a strain template
        /// </summary>
        /// <param name="strainTemplate">Strain template to base genotype on</param>
        /// <param name="individualId">Unique ID for this individual</param>
        /// <returns>New PlantGenotype instance</returns>
        public static PlantGenotype CreateFromStrain(PlantStrainSO strainTemplate, string individualId = null)
        {
            if (strainTemplate == null)
            {
                ChimeraLogger.LogError("[GenotypeFactory] PlantStrainSO is null");
                return null;
            }

            var plantGenotype = new PlantGenotype();

            // Set identity
            plantGenotype.GenotypeID = individualId ?? System.Guid.NewGuid().ToString();
            plantGenotype.StrainOrigin = strainTemplate;
            plantGenotype.Generation = 1;
            plantGenotype.IsFounder = true;
            plantGenotype.CreationDate = System.DateTime.Now;

            // Set species info
            plantGenotype.PlantSpecies = "Cannabis";
            plantGenotype.Cultivar = strainTemplate.StrainName;

            // Generate genotype from strain genetics
            var strainGenotype = GenerateGenotypeFromStrain(strainTemplate);
            plantGenotype.Genotype = strainGenotype.ToDictionary(kvp => kvp.Key, kvp => (object)new ProjectChimera.Data.Genetics.AlleleCouple(kvp.Value.Allele1, kvp.Value.Allele2));

            // Initialize with no mutations
            plantGenotype.Mutations = new List<object>();

            // Set fitness based on strain quality
            plantGenotype.OverallFitness = CalculateStrainFitness(strainTemplate);

            return plantGenotype;
        }

        /// <summary>
        /// Create offspring genotype from two parent genotypes
        /// Implements Mendelian inheritance with crossover
        /// </summary>
        /// <param name="parent1">First parent genotype</param>
        /// <param name="parent2">Second parent genotype</param>
        /// <param name="offspringId">ID for the offspring</param>
        /// <returns>New offspring PlantGenotype</returns>
        public static PlantGenotype CreateOffspring(PlantGenotype parent1, PlantGenotype parent2, string offspringId = null)
        {
            if (parent1 == null || parent2 == null)
            {
                ChimeraLogger.LogError("[GenotypeFactory] Parent genotypes cannot be null");
                return null;
            }

            var offspring = new PlantGenotype();

            // Set identity
            offspring.GenotypeID = offspringId ?? System.Guid.NewGuid().ToString();
            offspring.Generation = Mathf.Max(parent1.Generation, parent2.Generation) + 1;
            offspring.IsFounder = false;
            offspring.CreationDate = System.DateTime.Now;

            // Determine strain origin (favor higher fitness parent)
            offspring.StrainOrigin = parent1.OverallFitness >= parent2.OverallFitness ? parent1.StrainOrigin : parent2.StrainOrigin;
            offspring.PlantSpecies = parent1.PlantSpecies;
            offspring.Cultivar = $"{parent1.Cultivar} × {parent2.Cultivar}";

            // Perform genetic crossover
            var parent1Dict = parent1.Genotype.ToDictionary(kvp => kvp.Key, kvp => kvp.Value as ProjectChimera.Data.Genetics.AlleleCouple).Where(kvp => kvp.Value != null).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            var parent2Dict = parent2.Genotype.ToDictionary(kvp => kvp.Key, kvp => kvp.Value as ProjectChimera.Data.Genetics.AlleleCouple).Where(kvp => kvp.Value != null).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            var crossoverResult = PerformGeneticCrossover(parent1Dict, parent2Dict);
            offspring.Genotype = crossoverResult.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);

            // Combine mutation histories
            offspring.Mutations = new List<object>();
            offspring.Mutations.AddRange(parent1.Mutations);
            offspring.Mutations.AddRange(parent2.Mutations);

            // Apply new mutations if any occur
            ApplyNewMutations(offspring);

            // Calculate offspring fitness
            offspring.OverallFitness = CalculateOffspringFitness(parent1, parent2);

            return offspring;
        }

        /// <summary>
        /// Validate genotype data integrity
        /// </summary>
        /// <param name="genotype">Genotype to validate</param>
        /// <returns>Validation result with any issues found</returns>
        public static GenotypeValidationResult ValidateGenotype(PlantGenotype genotype)
        {
            var result = new GenotypeValidationResult { IsValid = true };

            if (genotype == null)
            {
                result.IsValid = false;
                result.Issues.Add("Genotype is null");
                return result;
            }

            // Check required fields
            if (string.IsNullOrEmpty(genotype.GenotypeID))
            {
                result.IsValid = false;
                result.Issues.Add("GenotypeID is required");
            }

            if (genotype.Genotype == null || genotype.Genotype.Count == 0)
            {
                result.IsValid = false;
                result.Issues.Add("Genotype dictionary is empty");
            }

            // Validate allele couples
            foreach (var kvp in genotype.Genotype)
            {
                if (kvp.Value is ProjectChimera.Data.Genetics.AlleleCouple couple)
                {
                    if (couple.Allele1 == null || couple.Allele2 == null)
                    {
                        result.Issues.Add($"Incomplete allele couple for gene: {kvp.Key}");
                    }
                }
            }

            // Check fitness bounds
            if (genotype.OverallFitness < 0f || genotype.OverallFitness > 2f)
            {
                result.Issues.Add("OverallFitness is out of valid range (0-2)");
            }

            if (result.Issues.Count > 0)
            {
                result.IsValid = false;
            }

            return result;
        }

        /// <summary>
        /// Check compatibility between two genotypes for breeding
        /// </summary>
        /// <param name="genotype1">First genotype</param>
        /// <param name="genotype2">Second genotype</param>
        /// <returns>Compatibility result</returns>
        public static BreedingCompatibilityResult CheckBreedingCompatibility(PlantGenotype genotype1, PlantGenotype genotype2)
        {
            var result = new BreedingCompatibilityResult { IsCompatible = true, CompatibilityScore = 1f };

            if (genotype1 == null || genotype2 == null)
            {
                result.IsCompatible = false;
                result.Issues.Add("One or both genotypes are null");
                return result;
            }

            // Check species compatibility
            if (genotype1.PlantSpecies != genotype2.PlantSpecies)
            {
                result.IsCompatible = false;
                result.Issues.Add("Different species cannot breed");
                return result;
            }

            // Check for inbreeding depression
            if (genotype1.StrainOrigin == genotype2.StrainOrigin)
            {
                result.CompatibilityScore *= 0.8f; // Reduce compatibility for same strain
                result.Issues.Add("Inbreeding detected - reduced compatibility");
            }

            // Check genetic diversity
            int commonGenes = genotype1.Genotype.Keys.Intersect(genotype2.Genotype.Keys).Count();
            int totalGenes = genotype1.Genotype.Keys.Union(genotype2.Genotype.Keys).Count();
            float geneticSimilarity = (float)commonGenes / totalGenes;

            if (geneticSimilarity > 0.9f)
            {
                result.CompatibilityScore *= 0.7f;
                result.Issues.Add("High genetic similarity - reduced compatibility");
            }

            // Check fitness levels
            if (genotype1.OverallFitness < 0.3f || genotype2.OverallFitness < 0.3f)
            {
                result.CompatibilityScore *= 0.6f;
                result.Issues.Add("Low fitness parent detected");
            }

            return result;
        }

        // Private helper methods
        private static void MapSpecificTraits(GenotypeDataSO source, PlantGenotype target)
        {
            // Map any specific trait properties that exist in the SO
            // This can be expanded as needed for additional trait mappings

            if (source.Species != null)
            {
                target.PlantSpecies = source.Species.speciesName;
            }
        }

        private static GeneDefinitionSO FindGeneDefinition(string geneId)
        {
            // In a full implementation, this would search a gene database
            // For now, return null - would need access to gene library
            return null;
        }

        private static Dictionary<string, DataAlleleCouple> GenerateGenotypeFromStrain(PlantStrainSO strain)
        {
            var genotype = new Dictionary<string, DataAlleleCouple>();

            // In a full implementation, this would generate based on strain genetics
            // For now, create a basic genotype structure

            return genotype;
        }

        private static float CalculateStrainFitness(PlantStrainSO strain)
        {
            // Calculate fitness based on strain properties
            return strain.BaseYield * 0.4f + strain.THCContent * 0.3f + strain.GrowthRateModifier * 0.3f;
        }

        private static Dictionary<string, ProjectChimera.Data.Genetics.AlleleCouple> PerformGeneticCrossover(
            Dictionary<string, ProjectChimera.Data.Genetics.AlleleCouple> parent1Genotype,
            Dictionary<string, ProjectChimera.Data.Genetics.AlleleCouple> parent2Genotype)
        {
            var offspringGenotype = new Dictionary<string, ProjectChimera.Data.Genetics.AlleleCouple>();

            // Get all genes from both parents
            var allGenes = parent1Genotype.Keys.Union(parent2Genotype.Keys);

            foreach (var geneId in allGenes)
            {
                ProjectChimera.Data.Genetics.AlleleCouple offspring;

                // Randomly select alleles from each parent
                if (parent1Genotype.ContainsKey(geneId) && parent2Genotype.ContainsKey(geneId))
                {
                    var parent1Couple = parent1Genotype[geneId];
                    var parent2Couple = parent2Genotype[geneId];

                    // Random segregation - select one allele from each parent
                    var allele1 = Random.value < 0.5f ? parent1Couple.allele1 : parent1Couple.allele2;
                    var allele2 = Random.value < 0.5f ? parent2Couple.allele1 : parent2Couple.allele2;

                    offspring = new ProjectChimera.Data.Genetics.AlleleCouple(allele1 ?? "unknown", allele2 ?? "unknown");
                }
                else if (parent1Genotype.ContainsKey(geneId))
                {
                    // Only in parent 1
                    offspring = parent1Genotype[geneId];
                }
                else if (parent2Genotype.ContainsKey(geneId))
                {
                    // Only in parent 2
                    offspring = parent2Genotype[geneId];
                }
                else
                {
                    // This shouldn't happen, but handle it gracefully
                    offspring = new DataAlleleCouple("unknown", "unknown");
                }

                offspringGenotype[geneId] = offspring;
            }

            return offspringGenotype;
        }

        private static DominanceType DetermineDominance(AlleleSO allele1, AlleleSO allele2)
        {
            if (allele1 == null || allele2 == null) return DominanceType.Codominant;
            if (allele1 == allele2) return DominanceType.Homozygous;

            // Simplified dominance determination
            return DominanceType.Heterozygous;
        }

        private static void ApplyNewMutations(PlantGenotype offspring)
        {
            // Apply random mutations with low probability
            float mutationRate = 0.001f; // 0.1% chance per gene

            foreach (var geneId in offspring.Genotype.Keys.ToList())
            {
                if (Random.value < mutationRate)
                {
                    var mutation = new DataMutationRecord
                    {
                        MutationId = System.Guid.NewGuid().ToString(),
                        AffectedGene = geneId,
                        MutationType = "PointMutation", // Default mutation type
                        EffectMagnitude = Random.Range(-0.1f, 0.1f),
                        OccurrenceDate = System.DateTime.Now,
                        IsBeneficial = Random.Range(-0.1f, 0.1f) > 0f
                    };

                    offspring.Mutations.Add(mutation);
                }
            }
        }

        private static float CalculateOffspringFitness(PlantGenotype parent1, PlantGenotype parent2)
        {
            // Basic fitness calculation - average of parents with some variation
            float averageFitness = (parent1.OverallFitness + parent2.OverallFitness) / 2f;
            float variation = Random.Range(-0.1f, 0.1f);

            return Mathf.Clamp(averageFitness + variation, 0.1f, 2f);
        }
    }

    /// <summary>
    /// Result of genotype validation
    /// </summary>
    public class GenotypeValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Issues { get; set; } = new List<string>();
    }

    /// <summary>
    /// Result of breeding compatibility check
    /// </summary>
    public class BreedingCompatibilityResult
    {
        public bool IsCompatible { get; set; } = true;
        public float CompatibilityScore { get; set; } = 1f;
        public List<string> Issues { get; set; } = new List<string>();
    }
}
