using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Core.Genetics.Conversion;
using ProjectChimera.Core.Genetics.Breeding;
using ProjectChimera.Core.Genetics.Validation;

namespace ProjectChimera.Systems.Genetics
{
    /// <summary>
    /// Phase 2.2.2: Genotype Factory - Refactored Facade
    /// Delegates to specialized services for conversion, breeding, and validation
    /// Maintains backward compatibility while using extracted services
    /// </summary>
    public static class GenotypeFactory
    {
        // Service instances (lazy initialized)
        private static IGenotypeConverter _converter;
        private static IOffspringGenerator _offspringGenerator;
        private static IGenotypeValidator _validator;
        private static IStrainGenotypeFactory _strainFactory;

        // Lazy initialization
        private static IGenotypeConverter Converter => _converter ??= new GenotypeConverter();
        private static IOffspringGenerator OffspringGenerator => _offspringGenerator ??= new OffspringGenerator();
        private static IGenotypeValidator Validator => _validator ??= new GenotypeValidator();
        private static IStrainGenotypeFactory StrainFactory => _strainFactory ??= new StrainGenotypeFactory();

        /// <summary>
        /// Convert GenotypeDataSO to runtime PlantGenotype
        /// Delegates to GenotypeConverter service
        /// </summary>
        public static PlantGenotype CreateFromScriptableObject(GenotypeDataSO genotypeDataSO)
        {
            return Converter.ConvertToRuntime(genotypeDataSO);
        }

        /// <summary>
        /// Convert runtime PlantGenotype back to GenotypeDataSO
        /// Delegates to GenotypeConverter service
        /// </summary>
        public static GenotypeDataSO CreateScriptableObject(PlantGenotype plantGenotype, GenotypeDataSO targetSO = null)
        {
            return Converter.ConvertToScriptableObject(plantGenotype, targetSO);
        }

        /// <summary>
        /// Create PlantGenotype from strain template
        /// Delegates to StrainGenotypeFactory service
        /// </summary>
        public static PlantGenotype CreateFromStrain(GeneticPlantStrainSO strainTemplate, string individualId = null)
        {
            return StrainFactory.CreateFromStrain(strainTemplate, individualId);
        }

        /// <summary>
        /// Create offspring from two parents
        /// Delegates to OffspringGenerator service
        /// </summary>
        public static PlantGenotype CreateOffspring(PlantGenotype parent1, PlantGenotype parent2, string offspringId = null)
        {
            return OffspringGenerator.GenerateOffspring(parent1, parent2, offspringId);
        }

        /// <summary>
        /// Validate genotype data integrity
        /// Delegates to GenotypeValidator service
        /// </summary>
        public static GenotypeValidationResult ValidateGenotype(PlantGenotype genotype)
        {
            return Validator.ValidateGenotype(genotype);
        }

        /// <summary>
        /// Check breeding compatibility
        /// Delegates to GenotypeValidator service
        /// </summary>
        public static BreedingCompatibilityResult CheckBreedingCompatibility(PlantGenotype genotype1, PlantGenotype genotype2)
        {
            return Validator.CheckBreedingCompatibility(genotype1, genotype2);
        }
    }
}
