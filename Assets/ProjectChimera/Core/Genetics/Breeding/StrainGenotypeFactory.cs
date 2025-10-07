using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Genetics.Breeding
{
    /// <summary>
    /// Service for creating genotypes from strain templates
    /// </summary>
    public class StrainGenotypeFactory : IStrainGenotypeFactory
    {
        /// <summary>
        /// Create PlantGenotype from strain template
        /// </summary>
        public PlantGenotype CreateFromStrain(GeneticPlantStrainSO strainTemplate, string individualId = null)
        {
            if (strainTemplate == null)
            {
                ChimeraLogger.LogWarning("Genetics", "StrainTemplate is null - cannot create PlantGenotype", null);
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
            plantGenotype.Genotype = GenerateGenotypeFromStrain(strainTemplate);

            // Initialize with no mutations
            plantGenotype.Mutations = new List<object>();

            // Set fitness based on strain quality
            plantGenotype.OverallFitness = CalculateStrainFitness(strainTemplate);

            return plantGenotype;
        }

        // Private methods
        private Dictionary<string, object> GenerateGenotypeFromStrain(GeneticPlantStrainSO strain)
        {
            var genotype = new Dictionary<string, object>();

            // TODO: In full implementation, generate based on strain genetics
            // For now, create basic genotype structure

            return genotype;
        }

        private float CalculateStrainFitness(GeneticPlantStrainSO strain)
        {
            // Calculate fitness based on strain properties
            return strain.BaseYield * 0.4f + strain.THCContent * 0.3f + strain.GrowthRateModifier * 0.3f;
        }
    }
}
