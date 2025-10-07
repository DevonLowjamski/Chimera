using ProjectChimera.Data.Genetics;

namespace ProjectChimera.Core.Genetics.Breeding
{
    /// <summary>
    /// Interface for creating genotypes from strain templates
    /// </summary>
    public interface IStrainGenotypeFactory
    {
        /// <summary>
        /// Create PlantGenotype from strain template
        /// </summary>
        PlantGenotype CreateFromStrain(GeneticPlantStrainSO strainTemplate, string individualId = null);
    }
}
