using UnityEngine;
using System.Collections.Generic;


namespace ProjectChimera.Data.Genetics
{
    /// <summary>
    /// BASIC: Simple genetic data for Project Chimera's plant strains.
    /// Focuses on essential genetic information without complex genotype systems.
    /// </summary>
    [CreateAssetMenu(fileName = "Basic Genetic Data", menuName = "Project Chimera/Genetics/Basic Genetic Data")]
    public class GenotypeDataSO : ScriptableObject
    {
        [Header("Basic Genetic Info")]
        [SerializeField] private string _strainName = "Basic Strain";
        [SerializeField] private string _strainId;
        [SerializeField] private PlantSpecies _species = PlantSpecies.Cannabis;
        [SerializeField] private string _description = "Basic plant strain";

        [Header("Genetic Traits")]
        [SerializeField] private float _thcContent = 15f; // Percentage
        [SerializeField] private float _cbdContent = 1f; // Percentage
        [SerializeField] private float _yieldPotential = 400f; // Grams per plant
        [SerializeField] private float _growthTime = 90f; // Days
        [SerializeField] private float _heightPotential = 150f; // Centimeters

        [Header("Genetic Stability")]
        [SerializeField] private float _geneticStability = 0.8f; // 0-1 scale
        [SerializeField] private bool _isStable = true;
        [SerializeField] private string _parentStrain1 = "";
        [SerializeField] private string _parentStrain2 = "";
        [SerializeField] private string _individualId = "";
        [SerializeField] private int _generation = 1;
        [SerializeField] private float _overallFitness = 1.0f;

        [Header("Advanced Genetics")]
        [SerializeField] private List<GenePair> _genePairs = new List<GenePair>();
        [SerializeField] private List<MutationRecord> _mutationHistory = new List<MutationRecord>();

        // Public properties for compatibility
        public string Name => _strainName;
        public string StrainName => _strainName;
        public string StrainId => _strainId;
        public string GenotypeID => _strainId; // Use StrainId as GenotypeID for compatibility
        public PlantSpecies Species => _species;
        public string Description => _description;
        public string IndividualID => _individualId;
        public GeneticPlantStrainSO ParentStrain => null; // For compatibility - returns null for now
        public int Generation => _generation;
        public float OverallFitness => _overallFitness;
        public List<GenePair> GenePairs => _genePairs;
        public List<MutationRecord> MutationHistory => _mutationHistory;

        /// <summary>
        /// Get basic strain information
        /// </summary>
        public StrainInfo GetStrainInfo()
        {
            return new StrainInfo
            {
                StrainName = _strainName,
                StrainId = _strainId,
                Species = _species,
                Description = _description,
                ThcContent = _thcContent,
                CbdContent = _cbdContent,
                YieldPotential = _yieldPotential,
                GrowthTime = _growthTime,
                HeightPotential = _heightPotential,
                GeneticStability = _geneticStability,
                IsStable = _isStable,
                ParentStrain1 = _parentStrain1,
                ParentStrain2 = _parentStrain2
            };
        }

        /// <summary>
        /// Get THC content
        /// </summary>
        public float GetThcContent()
        {
            return _thcContent;
        }

        /// <summary>
        /// Get CBD content
        /// </summary>
        public float GetCbdContent()
        {
            return _cbdContent;
        }

        /// <summary>
        /// Get yield potential
        /// </summary>
        public float GetYieldPotential()
        {
            return _yieldPotential;
        }

        /// <summary>
        /// Get growth time
        /// </summary>
        public float GetGrowthTime()
        {
            return _growthTime;
        }

        /// <summary>
        /// Get height potential
        /// </summary>
        public float GetHeightPotential()
        {
            return _heightPotential;
        }

        /// <summary>
        /// Check if strain is stable
        /// </summary>
        public bool IsStableStrain()
        {
            return _isStable && _geneticStability > 0.7f;
        }

        /// <summary>
        /// Get strain potency ratio (THC:CBD)
        /// </summary>
        public float GetPotencyRatio()
        {
            if (_cbdContent <= 0) return float.MaxValue;
            return _thcContent / _cbdContent;
        }

        /// <summary>
        /// Get strain quality score
        /// </summary>
        public float GetQualityScore()
        {
            // Simple quality calculation based on THC, yield, and stability
            float thcScore = Mathf.Clamp01(_thcContent / 25f); // Max expected 25%
            float yieldScore = Mathf.Clamp01(_yieldPotential / 600f); // Max expected 600g
            float stabilityScore = _geneticStability;

            return (thcScore + yieldScore + stabilityScore) / 3f;
        }

        /// <summary>
        /// Check if strain is suitable for breeding
        /// </summary>
        public bool IsSuitableForBreeding()
        {
            return _isStable && _geneticStability > 0.6f && _thcContent > 10f;
        }

        /// <summary>
        /// Get breeding recommendations
        /// </summary>
        public string GetBreedingRecommendation()
        {
            if (!IsSuitableForBreeding())
            {
                return "Not recommended for breeding - unstable genetics";
            }

            if (_thcContent > 20f)
            {
                return "High THC strain - good for potency breeding";
            }
            else if (_cbdContent > _thcContent)
            {
                return "High CBD strain - good for medicinal breeding";
            }
            else
            {
                return "Balanced strain - good for general breeding";
            }
        }

        /// <summary>
        /// Create hybrid with another strain
        /// </summary>
        public StrainInfo CreateHybrid(GenotypeDataSO otherStrain)
        {
            if (otherStrain == null) return GetStrainInfo();

            var thisInfo = GetStrainInfo();
            var otherInfo = otherStrain.GetStrainInfo();

            // Simple hybrid calculation - average traits
            return new StrainInfo
            {
                StrainName = $"{thisInfo.StrainName} x {otherInfo.StrainName}",
                StrainId = System.Guid.NewGuid().ToString(),
                Species = thisInfo.Species,
                Description = $"Hybrid of {thisInfo.StrainName} and {otherInfo.StrainName}",
                ThcContent = (thisInfo.ThcContent + otherInfo.ThcContent) / 2f,
                CbdContent = (thisInfo.CbdContent + otherInfo.CbdContent) / 2f,
                YieldPotential = (thisInfo.YieldPotential + otherInfo.YieldPotential) / 2f,
                GrowthTime = (thisInfo.GrowthTime + otherInfo.GrowthTime) / 2f,
                HeightPotential = (thisInfo.HeightPotential + otherInfo.HeightPotential) / 2f,
                GeneticStability = Mathf.Min(thisInfo.GeneticStability, otherInfo.GeneticStability) * 0.9f, // Hybrids are less stable
                IsStable = false, // New hybrids need stabilization
                ParentStrain1 = thisInfo.StrainId,
                ParentStrain2 = otherInfo.StrainId
            };
        }

        /// <summary>
        /// Initialize genotype with advanced genetics data
        /// </summary>
        public void InitializeGenotype(string strainId, string individualId, int generation, float overallFitness, List<GenePair> genePairs, List<MutationRecord> mutationHistory)
        {
            _strainId = strainId;
            _individualId = individualId;
            _generation = generation;
            _overallFitness = overallFitness;
            _genePairs = genePairs ?? new List<GenePair>();
            _mutationHistory = mutationHistory ?? new List<MutationRecord>();
        }
    }

    /// <summary>
    /// Plant species
    /// </summary>
    public enum PlantSpecies
    {
        Cannabis,
        Tomato,
        Lettuce
    }

    /// <summary>
    /// Basic strain information
    /// </summary>
    [System.Serializable]
    public struct StrainInfo
    {
        public string StrainName;
        public string StrainId;
        public PlantSpecies Species;
        public string Description;
        public float ThcContent;
        public float CbdContent;
        public float YieldPotential;
        public float GrowthTime;
        public float HeightPotential;
        public float GeneticStability;
        public bool IsStable;
        public string ParentStrain1;
        public string ParentStrain2;
    }

    /// <summary>
    /// Gene pair data structure for genetics system
    /// </summary>
    [System.Serializable]
    public struct GenePair
    {
        public GeneDefinitionSO Gene;
        public AlleleSO Allele1;
        public AlleleSO Allele2;
    }

    /// <summary>
    /// Mutation event data structure for genetics system
    /// </summary>
    [System.Serializable]
    public struct MutationEvent
    {
        public GeneDefinitionSO Gene;
        public AlleleSO FromAllele;
        public AlleleSO ToAllele;
        public float MutationTime;
        public int Generation;
        public string MutationDescription;
    }
}
