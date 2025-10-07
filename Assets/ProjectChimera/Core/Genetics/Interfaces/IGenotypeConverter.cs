using ProjectChimera.Data.Genetics;

namespace ProjectChimera.Core.Genetics.Conversion
{
    /// <summary>
    /// Interface for genotype conversion services
    /// Converts between ScriptableObject and runtime representations
    /// </summary>
    public interface IGenotypeConverter
    {
        /// <summary>
        /// Convert GenotypeDataSO to runtime PlantGenotype
        /// </summary>
        PlantGenotype ConvertToRuntime(GenotypeDataSO genotypeDataSO);

        /// <summary>
        /// Convert runtime PlantGenotype to GenotypeDataSO
        /// </summary>
        GenotypeDataSO ConvertToScriptableObject(PlantGenotype plantGenotype, GenotypeDataSO targetSO = null);
    }
}
