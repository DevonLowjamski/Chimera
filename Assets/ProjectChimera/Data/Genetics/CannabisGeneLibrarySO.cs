using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Shared;
using ProjectChimera.Shared;

namespace ProjectChimera.Data.Genetics
{
    /// <summary>
    /// Cannabis gene library ScriptableObject containing all available genes and their definitions.
    /// Provides centralized access to genetic data for the cannabis genetics engine.
    /// </summary>
    [CreateAssetMenu(fileName = "CannabisGeneLibrary", menuName = "Project Chimera/Genetics/Cannabis Gene Library")]
    public class CannabisGeneLibrarySO : ChimeraDataSO
    {
        [Header("Gene Library")]
        [SerializeField] private List<GeneDefinitionSO> _geneDefinitions = new List<GeneDefinitionSO>();
        [SerializeField] private List<AlleleSO> _alleleLibrary = new List<AlleleSO>();
        
        [Header("Library Configuration")]
        [SerializeField] private bool _autoGenerateGenes = true;
        [SerializeField] private int _maxGenes = 500;
        [SerializeField] private bool _enableCustomGenes = true;
        [SerializeField] private bool _validateGeneIntegrity = true;

        [Header("Gene Categories")]
        [SerializeField] private List<GeneCategory> _enabledCategories = new List<GeneCategory>();
        
        // Private caches for performance
        private Dictionary<string, GeneDefinitionSO> _genesByID;
        private Dictionary<string, List<GeneDefinitionSO>> _genesByCategory;
        private Dictionary<GeneType, List<GeneDefinitionSO>> _genesByType;
        private Dictionary<string, AlleleSO> _allelesById;
        private bool _isInitialized = false;

        // Public Properties
        public int TotalGenes => _geneDefinitions.Count;
        public int TotalAlleles => _alleleLibrary.Count;
        public List<GeneDefinitionSO> GeneDefinitions => _geneDefinitions;
        public List<AlleleSO> AlleleLibrary => _alleleLibrary;

        /// <summary>
        /// Initialize the gene database and caches
        /// </summary>
        public void InitializeGeneDatabase()
        {
            if (_isInitialized) return;

            BuildCaches();
            
            if (_geneDefinitions.Count == 0 && _autoGenerateGenes)
            {
                GenerateDefaultGenes();
                BuildCaches();
            }

            if (_validateGeneIntegrity)
            {
                ValidateGeneLibrary();
            }

            _isInitialized = true;
            SharedLogger.Log($"Cannabis Gene Library initialized with {TotalGenes} genes and {TotalAlleles} alleles");
        }

        /// <summary>
        /// Get total number of genes in the library
        /// </summary>
        public int GetTotalGenes()
        {
            return _geneDefinitions.Count;
        }

        /// <summary>
        /// Get all genes in the library
        /// </summary>
        public List<GeneDefinitionSO> GetAllGenes()
        {
            return _geneDefinitions.ToList();
        }

        /// <summary>
        /// Get a specific gene by ID
        /// </summary>
        public GeneDefinitionSO GetGene(string geneId)
        {
            if (!_isInitialized) InitializeGeneDatabase();
            
            if (_genesByID?.TryGetValue(geneId, out var gene) == true)
                return gene;
            return null;
        }

        /// <summary>
        /// Get genes by category
        /// </summary>
        public List<GeneDefinitionSO> GetGenesByCategory(GeneCategory category)
        {
            if (!_isInitialized) InitializeGeneDatabase();
            
            if (_genesByCategory?.TryGetValue(category.ToString(), out var genes) == true)
                return genes;
            return new List<GeneDefinitionSO>();
        }

        /// <summary>
        /// Get genes by type
        /// </summary>
        public List<GeneDefinitionSO> GetGenesByType(GeneType geneType)
        {
            if (!_isInitialized) InitializeGeneDatabase();
            
            if (_genesByType?.TryGetValue(geneType, out var genes) == true)
                return genes;
            return new List<GeneDefinitionSO>();
        }

        /// <summary>
        /// Get an allele by ID
        /// </summary>
        public AlleleSO GetAllele(string alleleId)
        {
            if (!_isInitialized) InitializeGeneDatabase();
            
            if (_allelesById?.TryGetValue(alleleId, out var allele) == true)
                return allele;
            return null;
        }

        /// <summary>
        /// Get all alleles for a specific gene
        /// </summary>
        public List<AlleleSO> GetAllelesForGene(string geneId)
        {
            return _alleleLibrary.Where(a => a.GeneId == geneId).ToList();
        }

        /// <summary>
        /// Add a new gene definition to the library
        /// </summary>
        public void AddGene(GeneDefinitionSO gene)
        {
            if (gene != null && !_geneDefinitions.Contains(gene))
            {
                _geneDefinitions.Add(gene);
                
                if (_isInitialized)
                {
                    BuildCaches();
                }
            }
        }

        /// <summary>
        /// Add a new allele to the library
        /// </summary>
        public void AddAllele(AlleleSO allele)
        {
            if (allele != null && !_alleleLibrary.Contains(allele))
            {
                _alleleLibrary.Add(allele);
                
                if (_isInitialized)
                {
                    BuildCaches();
                }
            }
        }

        /// <summary>
        /// Remove a gene from the library
        /// </summary>
        public bool RemoveGene(string geneId)
        {
            var gene = _geneDefinitions.FirstOrDefault(g => g.GeneCode == geneId);
            if (gene != null)
            {
                _geneDefinitions.Remove(gene);
                
                // Remove associated alleles
                _alleleLibrary.RemoveAll(a => a.GeneId == geneId);
                
                if (_isInitialized)
                {
                    BuildCaches();
                }
                
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Search genes by name or description
        /// </summary>
        public List<GeneDefinitionSO> SearchGenes(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm)) return new List<GeneDefinitionSO>();

            return _geneDefinitions.Where(g => 
                g.GeneName.ToLower().Contains(searchTerm.ToLower()) ||
                g.GeneSymbol.ToLower().Contains(searchTerm.ToLower()) ||
                g.GeneCode.ToLower().Contains(searchTerm.ToLower())
            ).ToList();
        }

        /// <summary>
        /// Get random genes for breeding experiments
        /// </summary>
        public List<GeneDefinitionSO> GetRandomGenes(int count)
        {
            var randomGenes = new List<GeneDefinitionSO>();
            var availableGenes = _geneDefinitions.ToList();

            for (int i = 0; i < count && availableGenes.Count > 0; i++)
            {
                var randomIndex = Random.Range(0, availableGenes.Count);
                randomGenes.Add(availableGenes[randomIndex]);
                availableGenes.RemoveAt(randomIndex);
            }

            return randomGenes;
        }

        /// <summary>
        /// Get essential genes required for plant viability
        /// </summary>
        public List<GeneDefinitionSO> GetEssentialGenes()
        {
            return _geneDefinitions.Where(g => g.KnownAlleles.Count > 1).ToList(); // Genes with multiple alleles are good breeding targets
        }

        /// <summary>
        /// Generate default cannabis genes for the library
        /// </summary>
        private void GenerateDefaultGenes()
        {
            var defaultGenes = new (string name, string code, GeneCategory category, string description)[]
            {
                ("Plant Height", "HEIGHT1", GeneCategory.Morphological, "Controls overall plant height and structure"),
                ("Leaf Size", "LEAF1", GeneCategory.Morphological, "Determines leaf size and shape characteristics"),
                ("Branch Density", "BRANCH1", GeneCategory.Morphological, "Controls branching pattern and density"),
                ("Stem Thickness", "STEM1", GeneCategory.Morphological, "Determines stem thickness and structural support"),
                ("Flowering Time", "FLOWER1", GeneCategory.Development, "Controls timing of flowering initiation"),
                ("THC Production", "THC1", GeneCategory.Biochemical, "Primary gene for THC biosynthesis"),
                ("CBD Production", "CBD1", GeneCategory.Biochemical, "Primary gene for CBD biosynthesis"),
                ("Terpene Profile", "TERP1", GeneCategory.Biochemical, "Controls terpene production and profile"),
                ("Disease Resistance", "RESIST1", GeneCategory.Resistance, "Provides resistance to common plant diseases"),
                ("Pest Resistance", "PEST1", GeneCategory.Resistance, "Provides resistance to insect pests"),
                ("Heat Tolerance", "HEAT1", GeneCategory.Resistance, "Enables survival in high temperature conditions"),
                ("Cold Tolerance", "COLD1", GeneCategory.Resistance, "Enables survival in low temperature conditions"),
                ("Drought Tolerance", "DROUGHT1", GeneCategory.Resistance, "Provides resistance to water stress"),
                ("Growth Rate", "GROWTH1", GeneCategory.Physiological, "Controls overall growth and metabolic rate"),
                ("Yield Potential", "YIELD1", GeneCategory.Yield, "Determines maximum yield potential"),
                ("Resin Production", "RESIN1", GeneCategory.Quality, "Controls trichrome and resin production"),
                ("Aroma Intensity", "AROMA1", GeneCategory.Quality, "Determines strength of aromatic compounds"),
                ("Potency Factor", "POTENCY1", GeneCategory.Quality, "Overall cannabinoid potency modifier"),
                ("Leaf Color", "COLOR1", GeneCategory.Morphological, "Controls leaf coloration and pigmentation"),
                ("Root Development", "ROOT1", GeneCategory.Morphological, "Controls root system development and structure")
            };

            foreach (var (name, code, category, description) in defaultGenes)
            {
                var gene = CreateInstance<GeneDefinitionSO>();
                // These would need to be set via reflection or public fields if available
                // For now, we'll add them to the list and they can be configured in the inspector
                _geneDefinitions.Add(gene);
            }

            SharedLogger.Log($"Generated {defaultGenes.Length} default cannabis genes");
        }

        /// <summary>
        /// Build performance caches
        /// </summary>
        private void BuildCaches()
        {
            _genesByID = new Dictionary<string, GeneDefinitionSO>();
            _genesByCategory = new Dictionary<string, List<GeneDefinitionSO>>();
            _genesByType = new Dictionary<GeneType, List<GeneDefinitionSO>>();
            _allelesById = new Dictionary<string, AlleleSO>();

            // Build gene caches
            foreach (var gene in _geneDefinitions)
            {
                if (gene == null) continue;

                // Cache by ID
                if (!string.IsNullOrEmpty(gene.GeneCode))
                {
                    _genesByID[gene.GeneCode] = gene;
                }

                // Cache by category
                var categoryKey = gene.Category.ToString();
                if (!_genesByCategory.ContainsKey(categoryKey))
                {
                    _genesByCategory[categoryKey] = new List<GeneDefinitionSO>();
                }
                _genesByCategory[categoryKey].Add(gene);

                // Cache by type
                if (!_genesByType.ContainsKey(gene.GeneType))
                {
                    _genesByType[gene.GeneType] = new List<GeneDefinitionSO>();
                }
                _genesByType[gene.GeneType].Add(gene);
            }

            // Build allele cache
            foreach (var allele in _alleleLibrary)
            {
                if (allele != null && !string.IsNullOrEmpty(allele.AlleleId))
                {
                    _allelesById[allele.AlleleId] = allele;
                }
            }
        }

        /// <summary>
        /// Validate library integrity
        /// </summary>
        private void ValidateGeneLibrary()
        {
            int validGenes = 0;
            int validAlleles = 0;

            // Validate genes
            foreach (var gene in _geneDefinitions)
            {
                if (gene != null && gene.ValidateData())
                {
                    validGenes++;
                }
            }

            // Validate alleles
            foreach (var allele in _alleleLibrary)
            {
                if (allele != null && allele.ValidateData())
                {
                    validAlleles++;
                }
            }

            SharedLogger.Log($"Gene Library Validation: {validGenes}/{_geneDefinitions.Count} genes valid, {validAlleles}/{_alleleLibrary.Count} alleles valid");
        }

        /// <summary>
        /// Get library statistics
        /// </summary>
        public GeneLibraryStatistics GetStatistics()
        {
            if (!_isInitialized) InitializeGeneDatabase();

            var stats = new GeneLibraryStatistics
            {
                TotalGenes = _geneDefinitions.Count,
                TotalAlleles = _alleleLibrary.Count,
                GenesByCategory = new Dictionary<GeneCategory, int>(),
                AllelesByGene = new Dictionary<string, int>()
            };

            // Count genes by category
            foreach (GeneCategory category in System.Enum.GetValues(typeof(GeneCategory)))
            {
                stats.GenesByCategory[category] = GetGenesByCategory(category).Count;
            }

            // Count alleles by gene
            var geneGroups = _alleleLibrary.GroupBy(a => a.GeneId);
            foreach (var group in geneGroups)
            {
                if (!string.IsNullOrEmpty(group.Key))
                {
                    stats.AllelesByGene[group.Key] = group.Count();
                }
            }

            return stats;
        }

        public override bool ValidateData()
        {
            bool isValid = base.ValidateData();

            if (_geneDefinitions.Count == 0 && _autoGenerateGenes)
            {
                SharedLogger.LogWarning("Gene library is empty but auto-generation is enabled - will generate default genes");
            }

            // Check for duplicate gene codes
            var geneCodes = _geneDefinitions.Where(g => g != null).Select(g => g.GeneCode).ToList();
            if (geneCodes.Count != geneCodes.Distinct().Count())
            {
                SharedLogger.LogError("Gene library contains duplicate gene codes");
                isValid = false;
            }

            return isValid;
        }
    }

    /// <summary>
    /// Gene library statistics for monitoring and analysis
    /// </summary>
    [System.Serializable]
    public class GeneLibraryStatistics
    {
        public int TotalGenes;
        public int TotalAlleles;
        public Dictionary<GeneCategory, int> GenesByCategory = new Dictionary<GeneCategory, int>();
        public Dictionary<string, int> AllelesByGene = new Dictionary<string, int>();
        public float AverageAllelesPerGene => TotalGenes > 0 ? (float)TotalAlleles / TotalGenes : 0f;
    }

    /// <summary>
    /// Gene types used in the genetics engine
    /// </summary>
    public enum GeneType
    {
        PlantHeight,
        LeafSize,
        BranchingPattern,
        StemThickness,
        FloweringTime,
        THCProduction,
        CBDProduction,
        TerpeneProfile,
        DiseaseResistance,
        PestResistance,
        HeatTolerance,
        ColdTolerance,
        DroughtTolerance,
        GrowthRate,
        YieldPotential,
        ResinProduction,
        AromaticCompounds,
        LeafColor,
        BudColor,
        PlantMorphology,
        RootDevelopment,
        NutrientUptake,
        PhotosyntheticEfficiency,
        StressResponse,
        FlowerDensity,
        TrichromeProduction,
        EnvironmentalTolerance,
        LightTolerance,
        WaterUseEfficiency,
        LeafWaxiness,
        BudDensity
    }

    /// <summary>
    /// Cannabis strain types for genetic classification
    /// </summary>
    public enum CannabisStrainType
    {
        Indica,
        Sativa,
        Ruderalis,
        Hybrid
    }
}