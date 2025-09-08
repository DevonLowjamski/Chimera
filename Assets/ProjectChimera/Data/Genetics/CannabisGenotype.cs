using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Genetics
{
    /// <summary>
    /// Cannabis genotype data structure for Project Chimera.
    /// Represents the genetic makeup of a cannabis plant with comprehensive trait definitions.
    /// </summary>
    [Serializable]
    public class CannabisGenotype
    {
        [Header("Genotype Identity")]
        public string GenotypeId;
        public string StrainId;
        public string StrainName;
        public string ParentStrain;
        public int GenerationNumber;
        public int Generation;
        public GenotypeOrigin Origin;
        public DateTime CreationDate;
        public bool IsFounderStrain;
        public bool IsHybrid;
        public List<string> ParentGenotypes = new List<string>();

        // UI Compatibility Properties
        public string GenotypeID => GenotypeId; // Alias for UI compatibility
        public List<string> ParentIDs { get; set; } = new List<string>(); // Settable for compatibility
        public string StrainOrigin => Origin.ToString(); // String representation for UI compatibility

        [Header("Genetic Traits")]
        public List<GeneticTrait> Traits = new List<GeneticTrait>();
        public List<AlleleExpression> AlleleExpressions = new List<AlleleExpression>();
        public List<ChromosomePair> Chromosomes = new List<ChromosomePair>();
        public Dictionary<string, List<object>> Alleles = new Dictionary<string, List<object>>();

        [Header("Phenotypic Expression")]
        public PhenotypicProfile PhenotypicProfile;
        public CannabinoidsProfile CannabinoidsProfile;
        public TerpenesProfile TerpenesProfile;
        public object ExpressedPhenotype;

        [Header("Growth Characteristics")]
        public float GrowthRate = 1.0f;
        public float MaxHeight = 2.0f;
        public float PlantSize = 1.0f;
        public float BranchingTendency = 1.0f;
        public float LeafDensity = 1.0f;
        public int FloweringSiteDensity = 100;

        // Missing properties for Error Wave 135
        public float BudDensity = 1.0f;
        public Color BudColor = Color.green;
        public float FloweringSpeed = 1.0f;

        // Missing property for Error Wave 136
        [Range(0f, 1f)] public float TrichromeAmount = 0.5f;

        // Missing property for Error Wave (test data)
        [Range(0f, 1f)] public float GeneticVariability = 0.5f;

        [Header("Resistance Traits")]
        public float DiseaseResistance = 75f;
        public float PestResistance = 70f;
        public float DroughtTolerance = 60f;
        public float ColdTolerance = 50f;
        public float HeatTolerance = 70f;

        [Header("Quality Traits")]
        public float PotencyPotential = 80f;
        public float YieldPotential = 75f;
        public float ResinProduction = 70f;
        public float AromaIntensity = 75f;
        public FlavorProfile FlavorProfile;

        [Header("Environmental Adaptation")]
        public float EnvironmentalStability = 70f;
        public List<EnvironmentalPreference> EnvironmentalPreferences = new List<EnvironmentalPreference>();
        public GxEInteractionProfile GxEProfile;

        // Additional properties for genetics engine compatibility
        public float HybridVigor = 1.0f;
        public Dictionary<string, float> EpigeneticModifications = new Dictionary<string, float>();

        // Missing properties for Systems layer
        public List<MutationRecord> Mutations = new List<MutationRecord>();
        public float InbreedingCoefficient = 0f;
        public List<GeneticMarker> GeneticMarkers = new List<GeneticMarker>();

        // Conversion methods for backwards compatibility
        public List<GeneticMutation> GetGeneticMutations()
        {
            return Mutations?.Select(m => new GeneticMutation
            {
                MutationID = m.MutationId,
                GeneLocusAffected = m.AffectedGene,
                MutationType = MutationType.Substitution, // Default value
                PhenotypicEffect = m.PhenotypicEffect.ToString(),
                Description = m.MutationType.ToString(),
                OccurrenceDate = m.OccurrenceDate.ToString(),
                IsBeneficial = m.IsBeneficial,
                IsHarmful = m.IsHarmful
            }).ToList() ?? new List<GeneticMutation>();
        }

        // Calculated properties
        public float OverallFitness => CalculateOverallFitness();
        public float GeneticDiversity => CalculateGeneticDiversity();
        public float HeterozygosityIndex => CalculateHeterozygosity();
        public bool IsStable => CheckGeneticStability();

        public CannabisGenotype()
        {
            GenotypeId = Guid.NewGuid().ToString();
            GenerationNumber = 1;
            Origin = GenotypeOrigin.Natural;
            InitializeDefaultTraits();
        }

        public CannabisGenotype(string parentStrain, int generation)
        {
            GenotypeId = Guid.NewGuid().ToString();
            ParentStrain = parentStrain;
            GenerationNumber = generation;
            Origin = generation > 1 ? GenotypeOrigin.Bred : GenotypeOrigin.Natural;
            InitializeDefaultTraits();
        }

        /// <summary>
        /// Initialize default genetic traits
        /// </summary>
        private void InitializeDefaultTraits()
        {
            // Add fundamental cannabis traits
            AddTrait("growth_rate", UnityEngine.Random.Range(0.7f, 1.3f), TraitDominance.Codominant);
            AddTrait("plant_height", UnityEngine.Random.Range(0.8f, 1.5f), TraitDominance.Dominant);
            AddTrait("leaf_size", UnityEngine.Random.Range(0.9f, 1.2f), TraitDominance.Recessive);
            AddTrait("branch_density", UnityEngine.Random.Range(0.8f, 1.4f), TraitDominance.Codominant);
            AddTrait("flowering_time", UnityEngine.Random.Range(0.85f, 1.15f), TraitDominance.Dominant);
            AddTrait("yield_potential", UnityEngine.Random.Range(0.7f, 1.4f), TraitDominance.Codominant);
            AddTrait("thc_production", UnityEngine.Random.Range(0.6f, 1.3f), TraitDominance.Dominant);
            AddTrait("cbd_production", UnityEngine.Random.Range(0.5f, 1.2f), TraitDominance.Recessive);
            AddTrait("disease_resistance", UnityEngine.Random.Range(0.6f, 1.2f), TraitDominance.Dominant);
            AddTrait("stress_tolerance", UnityEngine.Random.Range(0.7f, 1.1f), TraitDominance.Codominant);

            // Initialize profiles
            InitializeProfiles();
        }

        /// <summary>
        /// Initialize genetic profiles
        /// </summary>
        private void InitializeProfiles()
        {
            PhenotypicProfile = new PhenotypicProfile();
            CannabinoidsProfile = new CannabinoidsProfile();
            TerpenesProfile = new TerpenesProfile();
            FlavorProfile = new FlavorProfile();
            GxEProfile = new GxEInteractionProfile();
        }

        /// <summary>
        /// Add genetic trait
        /// </summary>
        public void AddTrait(string traitName, float expressedValue, TraitDominance dominance = TraitDominance.Codominant)
        {
            var trait = new GeneticTrait
            {
                TraitName = traitName,
                ExpressedValue = expressedValue,
                Dominance = dominance,
                HeritabilityIndex = UnityEngine.Random.Range(0.6f, 0.9f),
                EnvironmentalSensitivity = UnityEngine.Random.Range(0.3f, 0.8f)
            };

            Traits.Add(trait);
        }

        /// <summary>
        /// Get trait by name
        /// </summary>
        public GeneticTrait GetTrait(string traitName)
        {
            return Traits.FirstOrDefault(t => t.TraitName.Equals(traitName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Update trait expression
        /// </summary>
        public void UpdateTraitExpression(string traitName, float newValue)
        {
            var trait = GetTrait(traitName);
            if (trait != null)
            {
                trait.ExpressedValue = Mathf.Clamp(newValue, 0f, 2f); // Typical range for trait expression
            }
        }

        /// <summary>
        /// Calculate overall genetic fitness
        /// </summary>
        public float CalculateOverallFitness()
        {
            if (Traits.Count == 0) return 0f;

            float fitnessSum = 0f;
            float totalWeight = 0f;

            foreach (var trait in Traits)
            {
                float weight = GetTraitFitnessWeight(trait.TraitName);
                fitnessSum += trait.ExpressedValue * weight;
                totalWeight += weight;
            }

            return totalWeight > 0f ? fitnessSum / totalWeight : 0f;
        }

        /// <summary>
        /// Get fitness weight for trait
        /// </summary>
        private float GetTraitFitnessWeight(string traitName)
        {
            return traitName.ToLower() switch
            {
                "disease_resistance" => 1.2f,
                "stress_tolerance" => 1.1f,
                "yield_potential" => 1.0f,
                "growth_rate" => 0.9f,
                "thc_production" => 0.8f,
                "cbd_production" => 0.8f,
                _ => 0.7f
            };
        }

        /// <summary>
        /// Calculate genetic diversity
        /// </summary>
        private float CalculateGeneticDiversity()
        {
            if (AlleleExpressions.Count == 0) return 0f;

            float diversitySum = 0f;
            foreach (var allele in AlleleExpressions)
            {
                // Shannon diversity index calculation
                float p = allele.ExpressionLevel;
                if (p > 0f)
                {
                    diversitySum -= p * Mathf.Log(p);
                }
            }

            return diversitySum;
        }

        /// <summary>
        /// Calculate heterozygosity index
        /// </summary>
        private float CalculateHeterozygosity()
        {
            if (Chromosomes.Count == 0) return 0f;

            int heterozygousLoci = 0;
            int totalLoci = 0;

            foreach (var chromosome in Chromosomes)
            {
                totalLoci += chromosome.GeneCount;
                heterozygousLoci += chromosome.HeterozygousGenes;
            }

            return totalLoci > 0 ? (float)heterozygousLoci / totalLoci : 0f;
        }

        /// <summary>
        /// Check genetic stability
        /// </summary>
        private bool CheckGeneticStability()
        {
            float stabilityThreshold = 0.7f;
            return EnvironmentalStability >= stabilityThreshold && GeneticDiversity > 0.3f;
        }

        /// <summary>
        /// Cross with another genotype to create offspring
        /// </summary>
        public CannabisGenotype CrossWith(CannabisGenotype partner, float mutationRate = 0.02f)
        {
            var offspring = new CannabisGenotype(ParentStrain + " x " + partner.ParentStrain,
                                               Mathf.Max(GenerationNumber, partner.GenerationNumber) + 1);

            offspring.Origin = GenotypeOrigin.Bred;

            // Combine traits using Mendelian inheritance
            CombineTraits(offspring, partner, mutationRate);

            // Calculate new phenotypic profile
            offspring.CalculatePhenotypicExpression();

            return offspring;
        }

        /// <summary>
        /// Combine traits from two parents
        /// </summary>
        private void CombineTraits(CannabisGenotype offspring, CannabisGenotype partner, float mutationRate)
        {
            var allTraitNames = Traits.Select(t => t.TraitName)
                                    .Union(partner.Traits.Select(t => t.TraitName))
                                    .Distinct();

            foreach (var traitName in allTraitNames)
            {
                var parentTrait1 = GetTrait(traitName);
                var parentTrait2 = partner.GetTrait(traitName);

                float inheritedValue = CalculateInheritedValue(parentTrait1, parentTrait2);

                // Apply mutation
                if (UnityEngine.Random.value < mutationRate)
                {
                    inheritedValue *= UnityEngine.Random.Range(0.9f, 1.1f);
                }

                var dominance = DetermineDominance(parentTrait1, parentTrait2);
                offspring.AddTrait(traitName, inheritedValue, dominance);
            }
        }

        /// <summary>
        /// Calculate inherited trait value
        /// </summary>
        private float CalculateInheritedValue(GeneticTrait trait1, GeneticTrait trait2)
        {
            if (trait1 == null && trait2 == null) return 1.0f;
            if (trait1 == null) return trait2.ExpressedValue;
            if (trait2 == null) return trait1.ExpressedValue;

            // Mendelian inheritance with dominance effects
            return trait1.Dominance switch
            {
                TraitDominance.Dominant => trait1.ExpressedValue,
                TraitDominance.Recessive => trait2.Dominance == TraitDominance.Recessive ?
                                          (trait1.ExpressedValue + trait2.ExpressedValue) / 2f : trait2.ExpressedValue,
                TraitDominance.Codominant => (trait1.ExpressedValue + trait2.ExpressedValue) / 2f,
                _ => (trait1.ExpressedValue + trait2.ExpressedValue) / 2f
            };
        }

        /// <summary>
        /// Determine dominance relationship
        /// </summary>
        private TraitDominance DetermineDominance(GeneticTrait trait1, GeneticTrait trait2)
        {
            if (trait1 == null || trait2 == null) return TraitDominance.Codominant;

            // Simple dominance determination
            if (trait1.Dominance == TraitDominance.Dominant || trait2.Dominance == TraitDominance.Dominant)
                return TraitDominance.Dominant;

            if (trait1.Dominance == TraitDominance.Recessive && trait2.Dominance == TraitDominance.Recessive)
                return TraitDominance.Recessive;

            return TraitDominance.Codominant;
        }

        /// <summary>
        /// Calculate phenotypic expression from genotype
        /// </summary>
        public void CalculatePhenotypicExpression()
        {
            if (PhenotypicProfile == null) PhenotypicProfile = new PhenotypicProfile();

            // Calculate visual traits
            PhenotypicProfile.PlantHeight = GetTrait("plant_height")?.ExpressedValue ?? 1.0f;
            PhenotypicProfile.LeafSize = GetTrait("leaf_size")?.ExpressedValue ?? 1.0f;
            PhenotypicProfile.BranchDensity = GetTrait("branch_density")?.ExpressedValue ?? 1.0f;

            // Calculate cannabinoid production
            if (CannabinoidsProfile == null) CannabinoidsProfile = new CannabinoidsProfile();
            CannabinoidsProfile.THCContent = (GetTrait("thc_production")?.ExpressedValue ?? 0.8f) * 25f; // Max ~25%
            CannabinoidsProfile.CBDContent = (GetTrait("cbd_production")?.ExpressedValue ?? 0.6f) * 20f; // Max ~20%

            // Update derived properties
            GrowthRate = GetTrait("growth_rate")?.ExpressedValue ?? 1.0f;
            YieldPotential = (GetTrait("yield_potential")?.ExpressedValue ?? 1.0f) * 100f;
            DiseaseResistance = (GetTrait("disease_resistance")?.ExpressedValue ?? 0.8f) * 100f;
        }

        /// <summary>
        /// Get genotype summary information
        /// </summary>
        public GenotypeInfo GetGenotypeInfo()
        {
            return new GenotypeInfo
            {
                GenotypeId = GenotypeId,
                ParentStrain = ParentStrain,
                GenerationNumber = GenerationNumber,
                TraitCount = Traits.Count,
                OverallFitness = OverallFitness,
                GeneticDiversity = GeneticDiversity,
                IsStable = IsStable,
                PrimaryTraits = GetPrimaryTraits()
            };
        }

        /// <summary>
        /// Get primary traits for display
        /// </summary>
        private Dictionary<string, float> GetPrimaryTraits()
        {
            var primaryTraits = new Dictionary<string, float>();

            var importantTraits = new[] { "growth_rate", "yield_potential", "thc_production",
                                        "disease_resistance", "stress_tolerance" };

            foreach (var traitName in importantTraits)
            {
                var trait = GetTrait(traitName);
                if (trait != null)
                {
                    primaryTraits[traitName] = trait.ExpressedValue;
                }
            }

            return primaryTraits;
        }

        /// <summary>
        /// Apply environmental pressure to modify trait expression
        /// </summary>
        public void ApplyEnvironmentalPressure(string environmentType, float intensity)
        {
            foreach (var trait in Traits)
            {
                float environmentalEffect = CalculateEnvironmentalEffect(trait, environmentType, intensity);
                trait.ExpressedValue *= environmentalEffect;
                trait.ExpressedValue = Mathf.Clamp(trait.ExpressedValue, 0.1f, 2.0f);
            }

            CalculatePhenotypicExpression();
        }

        /// <summary>
        /// Calculate environmental effect on trait
        /// </summary>
        private float CalculateEnvironmentalEffect(GeneticTrait trait, string environmentType, float intensity)
        {
            float baseEffect = 1.0f;
            float sensitivity = trait.EnvironmentalSensitivity;

            // Different environments affect different traits
            float traitSusceptibility = environmentType.ToLower() switch
            {
                "drought" when trait.TraitName.Contains("stress") => 1.2f,
                "heat" when trait.TraitName.Contains("growth") => 0.9f,
                "disease" when trait.TraitName.Contains("resistance") => 1.1f,
                _ => 1.0f
            };

            return baseEffect + (intensity * sensitivity * (traitSusceptibility - 1.0f));
        }
    }

    /// <summary>
    /// Genetic trait definition
    /// </summary>
    [Serializable]
    public class GeneticTrait
    {
        public string TraitName;
        public float ExpressedValue = 1.0f;
        public TraitDominance Dominance = TraitDominance.Codominant;
        public float HeritabilityIndex = 0.7f; // How heritable this trait is (0-1)
        public float EnvironmentalSensitivity = 0.5f; // How sensitive to environment (0-1)
        public string Description;
        public List<string> AffectedSystems = new List<string>();
    }

    /// <summary>
    /// Allele expression data
    /// </summary>
    [Serializable]
    public class AlleleExpression
    {
        public string AlleleId;
        public string GeneLocus;
        public float ExpressionLevel;
        public bool IsActive;
        public GeneticEnvironmentalModifier[] Modifiers;
    }

    /// <summary>
    /// Chromosome pair representation
    /// </summary>
    [Serializable]
    public class ChromosomePair
    {
        public int ChromosomeNumber;
        public int GeneCount;
        public int HeterozygousGenes;
        public List<string> ActiveGenes = new List<string>();
        public float RecombinationRate = 0.5f;
    }

    /// <summary>
    /// Phenotypic profile
    /// </summary>
    [Serializable]
    public class PhenotypicProfile
    {
        public float PlantHeight = 1.0f;
        public float LeafSize = 1.0f;
        public float BranchDensity = 1.0f;
        public Color LeafColor = Color.green;
        public Color FlowerColor = Color.white;
        public PlantStructure StructureType = PlantStructure.Balanced;
    }

    /// <summary>
    /// Cannabinoids profile
    /// </summary>
    [Serializable]
    public class CannabinoidsProfile
    {
        public float THCContent = 15f; // Percentage
        public float CBDContent = 8f;  // Percentage
        public float CBGContent = 1f;  // Percentage
        public float CBNContent = 0.5f; // Percentage
        public float THCVContent = 0.3f; // Percentage
        public CannabinoidRatio DominantRatio = CannabinoidRatio.THC_Dominant;
    }

    /// <summary>
    /// Terpenes profile
    /// </summary>
    [Serializable]
    public class TerpenesProfile
    {
        public List<TerpeneContent> Terpenes = new List<TerpeneContent>();
        public TerpeneProfileType DominantProfile = TerpeneProfileType.Myrcene;
        public float OverallTerpeneContent = 2.5f; // Percentage
    }

    /// <summary>
    /// Individual terpene content
    /// </summary>
    [Serializable]
    public class TerpeneContent
    {
        public string TerpeneName;
        public float ContentPercentage;
        public string AromaDescriptor;
        public string EffectProfile;
    }

    /// <summary>
    /// Flavor profile
    /// </summary>
    [Serializable]
    public class FlavorProfile
    {
        public List<string> PrimaryFlavors = new List<string>();
        public List<string> SecondaryFlavors = new List<string>();
        public float FlavorIntensity = 70f;
        public float Sweetness = 50f;
        public float Earthiness = 60f;
        public float CitrusNotes = 30f;
        public float FloralNotes = 40f;
    }

    /// <summary>
    /// Environmental preference
    /// </summary>
    [Serializable]
    public class EnvironmentalPreference
    {
        public string EnvironmentType;
        public float OptimalValue;
        public float ToleranceRange;
        public float AdaptationRate;
    }

    /// <summary>
    /// GxE interaction profile
    /// </summary>
    [Serializable]
    public class GxEInteractionProfile
    {
        public Dictionary<string, float> EnvironmentalResponses = new Dictionary<string, float>();
        public float PlasticityIndex = 0.6f;
        public List<CriticalPeriod> CriticalPeriods = new List<CriticalPeriod>();
    }

    /// <summary>
    /// Critical development period
    /// </summary>
    [Serializable]
    public class CriticalPeriod
    {
        public string PeriodName;
        public PlantGrowthStage GrowthStage;
        public List<string> SensitiveTraits = new List<string>();
        public float SensitivityMultiplier = 1.5f;
    }

    /// <summary>
    /// Genetic environmental modifier
    /// </summary>
    [Serializable]
    public class GeneticEnvironmentalModifier
    {
        public string ModifierType;
        public float EffectMagnitude;
        public float Duration;
        public bool IsActive;
    }

    /// <summary>
    /// Genotype information for display
    /// </summary>
    [Serializable]
    public class GenotypeInfo
    {
        public string GenotypeId;
        public string ParentStrain;
        public int GenerationNumber;
        public int TraitCount;
        public float OverallFitness;
        public float GeneticDiversity;
        public bool IsStable;
        public Dictionary<string, float> PrimaryTraits;
    }

    // Enums

    public enum GenotypeOrigin
    {
        Natural,
        Bred,
        Hybrid,
        Mutant,
        Engineered
    }

    public enum TraitDominance
    {
        Dominant,
        Recessive,
        Codominant,
        Overdominant
    }

    /// <summary>
    /// Genetic dominance type for allele pairs
    /// </summary>
    public enum DominanceType
    {
        Homozygous,
        Heterozygous,
        Codominant
    }

    public enum PlantStructure
    {
        Compact,
        Tall,
        Bushy,
        Balanced,
        Spindly
    }

    public enum CannabinoidRatio
    {
        THC_Dominant,
        CBD_Dominant,
        Balanced,
        CBG_Enhanced,
        Low_Cannabinoid
    }

    public enum TerpeneProfileType
    {
        Myrcene,
        Limonene,
        Pinene,
        Linalool,
        Caryophyllene,
        Terpinolene,
        Mixed
    }

    /// <summary>
    /// Mutation record for tracking genetic changes
    /// </summary>
    [Serializable]
    public class MutationRecord
    {
        public string MutationId;
        public string AffectedGene;
        public string MutationType;
        public float EffectMagnitude;
        public System.DateTime OccurrenceDate;
        public bool IsBeneficial;

        // Missing properties for Systems layer
        public float PhenotypicEffect { get; set; }
        public bool IsNeutral = false;
        public bool IsHarmful { get; set; }

        // Additional properties for GeneticAnalysisEngine
        public float FitnessEffect { get; set; } = 0f;
        public Dictionary<string, float> TraitEffects { get; set; } = new Dictionary<string, float>();
    }

    /// <summary>
    /// Genetic marker for breeding analysis
    /// </summary>
    [Serializable]
    public class GeneticMarker
    {
        public string MarkerId;
        public string ChromosomeLocation;
        public string LinkedTrait;
        public float MarkerDistance;
        public bool IsPresent;

        // Missing property for Systems layer
        public string Key => MarkerId; // Key property for compatibility
        public float MarkerValue = 0f;
    }
}
