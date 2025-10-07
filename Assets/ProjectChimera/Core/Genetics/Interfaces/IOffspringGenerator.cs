using System.Collections.Generic;
using ProjectChimera.Data.Genetics;

namespace ProjectChimera.Core.Genetics.Breeding
{
    /// <summary>
    /// Interface for offspring generation services
    /// Handles genetic crossover and mutation
    /// </summary>
    public interface IOffspringGenerator
    {
        /// <summary>
        /// Generate single offspring from two parents
        /// </summary>
        PlantGenotype GenerateOffspring(PlantGenotype parent1, PlantGenotype parent2, string offspringId = null);

        /// <summary>
        /// Generate multiple F1 offspring from two parents
        /// </summary>
        List<PlantGenotype> GenerateF1Generation(PlantGenotype parent1, PlantGenotype parent2, int count);
    }
}
