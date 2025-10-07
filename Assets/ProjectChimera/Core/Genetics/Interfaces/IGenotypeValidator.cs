using ProjectChimera.Data.Genetics;
using ProjectChimera.Core.Genetics.Validation;

namespace ProjectChimera.Core.Genetics.Validation
{
    /// <summary>
    /// Interface for genotype validation services
    /// </summary>
    public interface IGenotypeValidator
    {
        /// <summary>
        /// Validate genotype data integrity
        /// </summary>
        GenotypeValidationResult ValidateGenotype(PlantGenotype genotype);

        /// <summary>
        /// Check breeding compatibility between two genotypes
        /// </summary>
        BreedingCompatibilityResult CheckBreedingCompatibility(PlantGenotype genotype1, PlantGenotype genotype2);
    }
}
