using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Shared;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Genetics
{
    /// <summary>
    /// Contains the complete genetic makeup (genotype) for a plant individual.
    /// Manages all gene-allele pairs and provides methods for genetic calculations and inheritance.
    /// </summary>
    [CreateAssetMenu(fileName = "New Genotype Data", menuName = "Project Chimera/Genetics/Genotype Data", order = 12)]
    public class GenotypeDataSO : ChimeraScriptableObject
    {
        [Header("Genotype Identity")]
        [SerializeField] private PlantSpeciesSO _species;
        [SerializeField] private PlantStrainSO _parentStrain;
        [SerializeField] private string _individualID;
        [SerializeField] private GenotypeType _genotypeType = GenotypeType.Individual;
        [SerializeField, TextArea(2, 4)] private string _genotypeDescription;

        [Header("Genetic Composition")]
        [SerializeField] private List<GenePair> _genePairs = new List<GenePair>();
        [SerializeField] private int _totalGeneCount = 0;
        [SerializeField] private int _homozygousCount = 0;
        [SerializeField] private int _heterozygousCount = 0;
        [SerializeField, Range(0f, 1f)] private float _geneticDiversity = 0.5f;

        [Header("Inheritance Information")]
        [SerializeField] private GenotypeDataSO _parent1Genotype;
        [SerializeField] private GenotypeDataSO _parent2Genotype;
        [SerializeField] private int _generation = 1; // F1, F2, etc.
        [SerializeField] private bool _isInbred = false;
        [SerializeField, Range(0f, 1f)] private float _inbreedingCoefficient = 0f;

        [Header("Genetic Fitness")]
        [SerializeField, Range(0f, 2f)] private float _overallFitness = 1f;
        [SerializeField, Range(0f, 1f)] private float _reproductiveFitness = 1f;
        [SerializeField, Range(0f, 1f)] private float _viabilityFitness = 1f;
        [SerializeField] private bool _isViable = true;
        [SerializeField] private bool _hasLethalCombinations = false;

        [Header("Mutation Tracking")]
        [SerializeField] private List<MutationEvent> _mutationHistory = new List<MutationEvent>();
        [SerializeField, Range(0f, 0.1f)] private float _overallMutationRate = 0.001f;
        [SerializeField] private bool _hasBeneficialMutations = false;
        [SerializeField] private bool _hasDetrimentalMutations = false;

        [Header("Phenotype Prediction")]
        [SerializeField] private List<PredictedTrait> _predictedTraits = new List<PredictedTrait>();
        [SerializeField] private bool _phenotypeCalculated = false;
        [SerializeField] private float _lastCalculationTime = 0f;

        [Header("Breeding Value")]
        [SerializeField, Range(0f, 100f)] private float _breedingValue = 50f;
        [SerializeField] private List<BreedingTrait> _breedingTraits = new List<BreedingTrait>();
        [SerializeField] private bool _isEliteGenotype = false;
        [SerializeField] private bool _recommenedForBreeding = true;

        // Public Properties
        public PlantSpeciesSO Species => _species;
        public PlantStrainSO ParentStrain => _parentStrain;
        public string IndividualID => _individualID;
        public string GenotypeID => _individualID; // Alias for compatibility
        public GenotypeType GenotypeType => _genotypeType;
        public string GenotypeDescription => _genotypeDescription;

        // Genetic Composition
        public List<GenePair> GenePairs => _genePairs;
        public int TotalGeneCount => _totalGeneCount;
        public int HomozygousCount => _homozygousCount;
        public int HeterozygousCount => _heterozygousCount;
        public float GeneticDiversity => _geneticDiversity;

        // Inheritance
        public GenotypeDataSO Parent1Genotype => _parent1Genotype;
        public GenotypeDataSO Parent2Genotype => _parent2Genotype;
        public int Generation => _generation;
        public bool IsInbred => _isInbred;
        public float InbreedingCoefficient => _inbreedingCoefficient;

        // Fitness
        public float OverallFitness => _overallFitness;
        public float ReproductiveFitness => _reproductiveFitness;
        public float ViabilityFitness => _viabilityFitness;
        public bool IsViable => _isViable;
        public bool HasLethalCombinations => _hasLethalCombinations;

        // Mutations
        public List<MutationEvent> MutationHistory => _mutationHistory;
        public float OverallMutationRate => _overallMutationRate;
        public bool HasBeneficialMutations => _hasBeneficialMutations;
        public bool HasDetrimentalMutations => _hasDetrimentalMutations;

        // Phenotype
        public List<PredictedTrait> PredictedTraits => _predictedTraits;
        public bool PhenotypeCalculated => _phenotypeCalculated;

        // Breeding
        public float BreedingValue => _breedingValue;
        public List<BreedingTrait> BreedingTraits => _breedingTraits;
        public bool IsEliteGenotype => _isEliteGenotype;
        public bool RecommendedForBreeding => _recommenedForBreeding;

        /// <summary>
        /// Initialize genotype data programmatically (replaces reflection-based setup)
        /// </summary>
        public void InitializeGenotype(string individualID, PlantStrainSO parentStrain, int generation, 
            float overallFitness, List<GenePair> genePairs = null, List<MutationEvent> mutationHistory = null)
        {
            _individualID = individualID;
            _parentStrain = parentStrain;
            _generation = generation;
            _overallFitness = overallFitness;
            
            if (genePairs != null)
            {
                _genePairs = new List<GenePair>(genePairs);
            }
            
            if (mutationHistory != null)
            {
                _mutationHistory = new List<MutationEvent>(mutationHistory);
            }
            
            RecalculateGeneticStats();
        }

        /// <summary>
        /// Initializes the genotype with a basic set of genes from the parent strain.
        /// </summary>
        public void InitializeFromStrain(PlantStrainSO strain, List<GeneDefinitionSO> geneSet = null)
        {
            _parentStrain = strain;
            // Note: BaseSpecies is a string, but _species expects PlantSpeciesSO
            // For now, we'll set _species to null and log a warning
            _species = null;
            SharedLogger.LogWarning($"[Chimera] Cannot assign BaseSpecies '{strain.BaseSpecies}' to PlantSpeciesSO. Manual assignment required.");
            _genePairs.Clear();

            // Use provided gene set or get default set for species
            var genes = geneSet ?? GetDefaultGeneSet();

            foreach (var gene in genes)
            {
                var genePair = new GenePair
                {
                    Gene = gene,
                    Allele1 = GetRandomAlleleForGene(gene),
                    Allele2 = GetRandomAlleleForGene(gene)
                };

                _genePairs.Add(genePair);
            }

            RecalculateGeneticStats();
            _phenotypeCalculated = false;
        }

        /// <summary>
        /// Creates offspring genotype through sexual reproduction with another genotype.
        /// </summary>
        public GenotypeDataSO CreateOffspring(GenotypeDataSO partner, string offspringID = "")
        {
            if (partner == null)
            {
                SharedLogger.LogError("[Chimera] Cannot create offspring with null partner genotype.");
                return null;
            }

            // Create new genotype data asset (this would typically be done through asset creation)
            var offspring = ScriptableObject.CreateInstance<GenotypeDataSO>();
            offspring._individualID = string.IsNullOrEmpty(offspringID) ? System.Guid.NewGuid().ToString() : offspringID;
            offspring._parent1Genotype = this;
            offspring._parent2Genotype = partner;
            offspring._generation = Mathf.Max(_generation, partner._generation) + 1;
            offspring._species = _species;

            // Perform genetic recombination
            offspring._genePairs = PerformRecombination(partner);

            // Check for mutations
            offspring.ApplyMutations();

            // Calculate fitness and viability
            offspring.CalculateFitness();

            offspring.RecalculateGeneticStats();
            offspring._phenotypeCalculated = false;

            return offspring;
        }

        /// <summary>
        /// Performs genetic recombination between this genotype and a partner.
        /// </summary>
        private List<GenePair> PerformRecombination(GenotypeDataSO partner)
        {
            var offspringGenePairs = new List<GenePair>();

            // Get union of all genes from both parents
            var allGenes = _genePairs.Select(gp => gp.Gene).Union(partner._genePairs.Select(gp => gp.Gene)).ToList();

            foreach (var gene in allGenes)
            {
                var thisGenePair = _genePairs.FirstOrDefault(gp => gp.Gene == gene);
                var partnerGenePair = partner._genePairs.FirstOrDefault(gp => gp.Gene == gene);

                AlleleSO allele1 = null;
                AlleleSO allele2 = null;

                // Get allele from this parent
                if (thisGenePair != null)
                {
                    allele1 = Random.value < 0.5f ? thisGenePair.Allele1 : thisGenePair.Allele2;
                }
                else
                {
                    // Gene not present in this parent - use wild type or random
                    allele1 = gene.WildTypeAllele() ?? gene.GetRandomAllele();
                }

                // Get allele from partner parent
                if (partnerGenePair != null)
                {
                    allele2 = Random.value < 0.5f ? partnerGenePair.Allele1 : partnerGenePair.Allele2;
                }
                else
                {
                    // Gene not present in partner - use wild type or random
                    allele2 = gene.WildTypeAllele() ?? gene.GetRandomAllele();
                }

                if (allele1 != null && allele2 != null)
                {
                    offspringGenePairs.Add(new GenePair
                    {
                        Gene = gene,
                        Allele1 = allele1,
                        Allele2 = allele2
                    });
                }
            }

            return offspringGenePairs;
        }

        /// <summary>
        /// Applies random mutations to the genotype based on mutation rates.
        /// </summary>
        private void ApplyMutations()
        {
            foreach (var genePair in _genePairs)
            {
                // Check for mutations in each allele
                if (genePair.Allele1.CanMutate() && Random.value < genePair.Allele1.MutationRate)
                {
                    var newAllele = genePair.Allele1.GetMutationTarget();
                    if (newAllele != null)
                    {
                        RecordMutation(genePair.Gene, genePair.Allele1, newAllele);
                        genePair.Allele1 = newAllele;
                    }
                }

                if (genePair.Allele2.CanMutate() && Random.value < genePair.Allele2.MutationRate)
                {
                    var newAllele = genePair.Allele2.GetMutationTarget();
                    if (newAllele != null)
                    {
                        RecordMutation(genePair.Gene, genePair.Allele2, newAllele);
                        genePair.Allele2 = newAllele;
                    }
                }
            }
        }

        /// <summary>
        /// Records a mutation event in the history.
        /// </summary>
        private void RecordMutation(GeneDefinitionSO gene, AlleleSO fromAllele, AlleleSO toAllele)
        {
            _mutationHistory.Add(new MutationEvent
            {
                Gene = gene,
                FromAllele = fromAllele,
                ToAllele = toAllele,
                MutationTime = Time.time,
                Generation = _generation
            });

            if (toAllele.IsBeneficial) _hasBeneficialMutations = true;
            if (toAllele.IsDetrimental) _hasDetrimentalMutations = true;
        }

        /// <summary>
        /// Calculates predicted phenotype values for all traits.
        /// </summary>
        public void CalculatePhenotype(EnvironmentalConditions environment = default)
        {
            _predictedTraits.Clear();

            // Get all unique traits affected by genes in this genotype
            var allTraits = _genePairs.SelectMany(gp => gp.Gene.InfluencedTraits.Select(ti => ti.TraitType)).Distinct().ToList();

            foreach (var trait in allTraits)
            {
                float traitValue = CalculateTraitValue(trait, environment);

                _predictedTraits.Add(new PredictedTrait
                {
                    Trait = trait,
                    PredictedValue = traitValue,
                    Confidence = CalculateConfidence(trait),
                    EnvironmentDependent = IsEnvironmentDependent(trait)
                });
            }

            _phenotypeCalculated = true;
            _lastCalculationTime = Time.time;
        }

        /// <summary>
        /// Calculates the predicted value for a specific trait.
        /// </summary>
        public float CalculateTraitValue(TraitType trait, EnvironmentalConditions environment = default)
        {
            float totalValue = 0f;
            float totalWeight = 0f;

            foreach (var genePair in _genePairs)
            {
                var traitInfluence = genePair.Gene.InfluencedTraits.FirstOrDefault(ti => ti.TraitType == trait);
                if (traitInfluence == null) continue;

                // Calculate effect from both alleles
                float allele1Effect = genePair.Gene.CalculatePhenotypicEffect(genePair.Allele1, genePair.Allele2, environment);
                float allele2Effect = genePair.Gene.CalculatePhenotypicEffect(genePair.Allele2, genePair.Allele1, environment);

                float geneEffect = (allele1Effect + allele2Effect) * 0.5f;
                float weight = traitInfluence.InfluenceStrength;

                totalValue += geneEffect * weight;
                totalWeight += weight;
            }

            return totalWeight > 0 ? totalValue / totalWeight : 0f;
        }

        /// <summary>
        /// Calculates overall fitness of this genotype.
        /// </summary>
        public void CalculateFitness(EnvironmentalConditions environment = default)
        {
            float viability = 1f;
            float reproductive = 1f;
            bool hasLethal = false;

            foreach (var genePair in _genePairs)
            {
                // Check for lethal combinations
                if (genePair.Allele1.CausesLethalPhenotype && genePair.Allele2.CausesLethalPhenotype)
                {
                    hasLethal = true;
                    viability = 0f;
                    break;
                }

                // Calculate fitness contributions
                var envConditions = environment.Temperature == 0f ? new EnvironmentalConditions() : environment;
                float allele1Fitness = genePair.Allele1.CalculateFitness(envConditions);
                float allele2Fitness = genePair.Allele2.CalculateFitness(envConditions);

                viability *= (allele1Fitness + allele2Fitness) * 0.5f;
            }

            _hasLethalCombinations = hasLethal;
            _isViable = viability > 0.1f;
            _viabilityFitness = viability;
            _reproductiveFitness = reproductive;
            _overallFitness = viability * reproductive;
        }

        /// <summary>
        /// Recalculates genetic statistics (homozygosity, diversity, etc.).
        /// </summary>
        private void RecalculateGeneticStats()
        {
            _totalGeneCount = _genePairs.Count;
            _homozygousCount = _genePairs.Count(gp => gp.Allele1.UniqueID == gp.Allele2.UniqueID);
            _heterozygousCount = _totalGeneCount - _homozygousCount;

            _geneticDiversity = _totalGeneCount > 0 ? (float)_heterozygousCount / _totalGeneCount : 0f;
            _isInbred = _geneticDiversity < 0.3f;
            _inbreedingCoefficient = 1f - _geneticDiversity;
        }

        private AlleleSO GetRandomAlleleForGene(GeneDefinitionSO gene)
        {
            if (gene.KnownAlleles.Count == 0) return gene.WildTypeAllele();

            // Weighted selection based on frequency
            float totalFreq = gene.KnownAlleles.Sum(a => a.PopulationFrequency);
            float randomValue = Random.value * totalFreq;
            float currentSum = 0f;

            foreach (var allele in gene.KnownAlleles)
            {
                currentSum += allele.PopulationFrequency;
                if (randomValue <= currentSum) return allele;
            }

            return gene.KnownAlleles[0]; // Fallback
        }

        private List<GeneDefinitionSO> GetDefaultGeneSet()
        {
            // This would typically load a predefined set of genes for the species
            // For now, return an empty list - this should be populated with actual gene definitions
            return new List<GeneDefinitionSO>();
        }

        private float CalculateConfidence(TraitType trait)
        {
            // Higher confidence for traits with more genetic support and less environmental sensitivity
            var supportingGenes = _genePairs.Count(gp => gp.Gene.InfluencedTraits.Any(ti => ti.TraitType == trait));
            var envSensitiveGenes = _genePairs.Count(gp => gp.Gene.EnvironmentallyRegulated &&
                                                          gp.Gene.InfluencedTraits.Any(ti => ti.TraitType == trait));

            float baseConfidence = Mathf.Min(1f, supportingGenes / 5f); // Normalize to 5 genes = full confidence
            float envPenalty = envSensitiveGenes > 0 ? 0.8f : 1f;

            return baseConfidence * envPenalty;
        }

        private bool IsEnvironmentDependent(TraitType trait)
        {
            return _genePairs.Any(gp => gp.Gene.EnvironmentallyRegulated &&
                                      gp.Gene.InfluencedTraits.Any(ti => ti.TraitType == trait));
        }

        /// <summary>
        /// Gets the overall growth potential based on genetic factors.
        /// </summary>
        public float GetGrowthPotential()
        {
                        return CalculateTraitValue(TraitType.PlantHeight) * 0.6f +
                   CalculateTraitValue(TraitType.FlowerYield) * 0.4f;
        }

        /// <summary>
        /// Gets the yield potential based on genetic factors.
        /// </summary>
        public float GetYieldPotential()
        {
            return CalculateTraitValue(TraitType.FlowerYield);
        }

        /// <summary>
        /// Gets the potency potential (THC/CBD) based on genetic factors.
        /// </summary>
        public float GetPotencyPotential()
        {
            float thcPotential = CalculateTraitValue(TraitType.THCPotency);
            float cbdPotential = CalculateTraitValue(TraitType.CBDContent);
            return Mathf.Max(thcPotential, cbdPotential); // Return the higher potential
        }

        /// <summary>
        /// Gets the width to height ratio for plant structure.
        /// </summary>
        public float GetWidthToHeightRatio()
        {
            // Calculate based on plant structure traits, default to indica vs sativa tendency
            float branchDensity = CalculateTraitValue(TraitType.BranchingDensity);
            float nodeSpacing = CalculateTraitValue(TraitType.InternodalSpacing);

            // Bushier plants (higher branch density, tighter nodes) have higher width ratio
            float ratio = 0.4f + (branchDensity * 0.3f) + ((1f - nodeSpacing) * 0.2f);
            return Mathf.Clamp(ratio, 0.3f, 0.9f);
        }

        public override bool ValidateData()
        {
            bool isValid = base.ValidateData();

            if (_species == null)
            {
                SharedLogger.LogWarning($"[Chimera] GenotypeDataSO '{DisplayName}' has no species assigned.");
                isValid = false;
            }

            if (_genePairs.Count == 0)
            {
                SharedLogger.LogWarning($"[Chimera] GenotypeDataSO '{DisplayName}' has no gene pairs defined.");
                isValid = false;
            }

            // Validate that all gene pairs have valid alleles
            foreach (var genePair in _genePairs)
            {
                if (genePair.Gene == null || genePair.Allele1 == null || genePair.Allele2 == null)
                {
                    SharedLogger.LogWarning($"[Chimera] GenotypeDataSO '{DisplayName}' has invalid gene pair.");
                    isValid = false;
                }
            }

            return isValid;
        }
    }

    [System.Serializable]
    public class GenePair
    {
        public GeneDefinitionSO Gene;
        public AlleleSO Allele1;
        public AlleleSO Allele2;

        public bool IsHomozygous => Allele1.UniqueID == Allele2.UniqueID;
        public bool IsHeterozygous => !IsHomozygous;

        public float GetCombinedEffect(TraitType trait, EnvironmentalConditions environment = default)
        {
            if (Gene == null) return 0f;
            return Gene.CalculatePhenotypicEffect(Allele1, Allele2, environment);
        }
    }

    [System.Serializable]
    public class MutationEvent
    {
        public GeneDefinitionSO Gene;
        public AlleleSO FromAllele;
        public AlleleSO ToAllele;
        public float MutationTime;
        public int Generation;
        [TextArea(2, 3)] public string MutationDescription;
    }

    [System.Serializable]
    public class PredictedTrait
    {
        public TraitType Trait;
        public float PredictedValue;
        [Range(0f, 1f)] public float Confidence = 1f;
        public bool EnvironmentDependent = false;
        public Vector2 PredictionRange = Vector2.zero; // Min/max possible values
    }

    [System.Serializable]
    public class BreedingTrait
    {
        public TraitType Trait;
        public float BreedingValue;
        public float Heritability;
        public bool IsDesirable = true;
        public float SelectionIntensity = 1f;
    }

    public enum GenotypeType
    {
        Individual,        // Single plant genotype
        Strain,            // Representative strain genotype
        Population,        // Population average genotype
        Elite,             // Elite breeding line
        Experimental,      // Experimental cross
        Wild,              // Wild type genotype
        Synthetic         // Artificially constructed
    }
}
