using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Data.Genetics;

namespace ProjectChimera.Core.Genetics.Validation
{
    /// <summary>
    /// Service for validating genotype data integrity and breeding compatibility
    /// </summary>
    public class GenotypeValidator : IGenotypeValidator
    {
        /// <summary>
        /// Validate genotype data integrity
        /// </summary>
        public GenotypeValidationResult ValidateGenotype(PlantGenotype genotype)
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
                if (kvp.Value is AlleleCouple couple)
                {
                    if (string.IsNullOrEmpty(couple.Allele1) || string.IsNullOrEmpty(couple.Allele2))
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
        /// Check breeding compatibility between two genotypes
        /// </summary>
        public BreedingCompatibilityResult CheckBreedingCompatibility(PlantGenotype genotype1, PlantGenotype genotype2)
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
                result.CompatibilityScore *= 0.8f;
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
