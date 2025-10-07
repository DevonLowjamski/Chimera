using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Genetics.Conversion
{
    /// <summary>
    /// Service for converting between GenotypeDataSO and PlantGenotype
    /// Handles SO ↔ Runtime conversions for serialization compatibility
    /// </summary>
    public class GenotypeConverter : IGenotypeConverter
    {
        /// <summary>
        /// Convert GenotypeDataSO to runtime PlantGenotype
        /// </summary>
        public PlantGenotype ConvertToRuntime(GenotypeDataSO genotypeDataSO)
        {
            if (genotypeDataSO == null)
            {
                ChimeraLogger.LogWarning("Genetics", "GenotypeDataSO is null - cannot create PlantGenotype", null);
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
            plantGenotype.PlantSpecies = genotypeDataSO.Species.ToString();
            plantGenotype.Cultivar = genotypeDataSO.ParentStrain?.StrainName ?? "Unknown";

            // Convert gene pairs to allele couples
            plantGenotype.Genotype = ConvertGenePairsToAlleles(genotypeDataSO.GenePairs);

            // Convert mutation history
            plantGenotype.Mutations = ConvertMutationHistory(genotypeDataSO.MutationHistory);

            return plantGenotype;
        }

        /// <summary>
        /// Convert runtime PlantGenotype back to GenotypeDataSO
        /// </summary>
        public GenotypeDataSO ConvertToScriptableObject(PlantGenotype plantGenotype, GenotypeDataSO targetSO = null)
        {
            if (plantGenotype == null)
            {
                ChimeraLogger.LogWarning("Genetics", "PlantGenotype is null - cannot create ScriptableObject", null);
                return null;
            }

            GenotypeDataSO genotypeDataSO = targetSO ?? ScriptableObject.CreateInstance<GenotypeDataSO>();

            // Convert allele couples back to gene pairs
            var genePairs = ConvertAllelesToGenePairs(plantGenotype.Genotype);

            // Convert mutations back
            var mutationHistory = ConvertRuntimeMutations(plantGenotype.Mutations);

            // Initialize SO using proper API
            genotypeDataSO.InitializeGenotype(
                strainId: plantGenotype.StrainOrigin?.ToString() ?? "Unknown",
                individualId: plantGenotype.GenotypeID,
                generation: plantGenotype.Generation,
                overallFitness: plantGenotype.OverallFitness,
                genePairs: genePairs,
                mutationHistory: mutationHistory
            );

            return genotypeDataSO;
        }

        // Private conversion methods
        private Dictionary<string, object> ConvertGenePairsToAlleles(List<GenePair> genePairs)
        {
            var genotype = new Dictionary<string, object>();

            foreach (var genePair in genePairs)
            {
                if (genePair.Gene != null)
                {
                    var alleleCouple = new AlleleCouple(
                        genePair.Allele1?.name ?? "unknown",
                        genePair.Allele2?.name ?? "unknown"
                    );
                    genotype[genePair.Gene.name] = alleleCouple;
                }
            }

            return genotype;
        }

        private List<object> ConvertMutationHistory(List<MutationRecord> mutationHistory)
        {
            var mutations = new List<object>();

            foreach (var mutation in mutationHistory)
            {
                var mutationRecord = new MutationRecord
                {
                    MutationID = System.Guid.NewGuid().ToString(),
                    GeneAffected = mutation.GeneAffected ?? "Unknown",
                    MutationType = "PointMutation",
                    EffectMagnitude = 0f,
                    MutationDate = System.DateTime.Now,
                    IsBeneficial = false
                };

                mutations.Add(mutationRecord);
            }

            return mutations;
        }

        private List<GenePair> ConvertAllelesToGenePairs(Dictionary<string, object> genotype)
        {
            var genePairs = new List<GenePair>();

            foreach (var kvp in genotype)
            {
                var geneDefinition = FindGeneDefinition(kvp.Key);
                if (geneDefinition != null)
                {
                    var genePair = new GenePair
                    {
                        Gene = geneDefinition,
                        Allele1 = null, // Would need allele lookup
                        Allele2 = null
                    };
                    genePairs.Add(genePair);
                }
            }

            return genePairs;
        }

        private List<MutationRecord> ConvertRuntimeMutations(List<object> mutations)
        {
            var mutationHistory = new List<MutationRecord>();

            foreach (var mutation in mutations)
            {
                if (mutation is MutationRecord mutationRecord)
                {
                    mutationHistory.Add(mutationRecord);
                }
            }

            return mutationHistory;
        }

        private GeneDefinitionSO FindGeneDefinition(string geneId)
        {
            // TODO: Search gene database/library
            return null;
        }
    }
}
