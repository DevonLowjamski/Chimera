using UnityEngine;
using ProjectChimera.Data.Shared;
using ProjectChimera.Shared;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Shared;

namespace ProjectChimera.Data.Genetics
{
    /// <summary>
    /// Cannabis strain database ScriptableObject containing all available cannabis strains.
    /// Provides centralized access to base strains for breeding and genetics systems.
    /// </summary>
    [CreateAssetMenu(fileName = "CannabisStrainDatabase", menuName = "Project Chimera/Genetics/Cannabis Strain Database")]
    public class CannabisStrainDatabaseSO : ScriptableObject
    {
        [Header("Strain Database")]
        [SerializeField] private List<GeneticPlantStrainSO> _baseStrains = new List<GeneticPlantStrainSO>();
        [SerializeField] private List<CannabisStrainAssetSO> _strainAssets = new List<CannabisStrainAssetSO>();

        [Header("Database Configuration")]
        [SerializeField] private bool _autoGenerateStrains = true;
        [SerializeField] private int _maxStrains = 100;
        [SerializeField] private bool _enableCustomStrains = true;

        // Public Properties
        public int TotalStrains => _baseStrains.Count + _strainAssets.Count;
        public List<GeneticPlantStrainSO> BaseStrains => _baseStrains;
        public List<CannabisStrainAssetSO> StrainAssets => _strainAssets;

        /// <summary>
        /// Get all base strains from the database
        /// </summary>
        public List<GeneticPlantStrainSO> GetAllBaseStrains()
        {
            return _baseStrains.ToList();
        }

        /// <summary>
        /// Get all strain assets from the database
        /// </summary>
        public List<CannabisStrainAssetSO> GetAllStrainAssets()
        {
            return _strainAssets.ToList();
        }

        /// <summary>
        /// Get a strain by ID
        /// </summary>
        public GeneticPlantStrainSO GetStrainById(string strainId)
        {
            return _baseStrains.FirstOrDefault(s => s.StrainId == strainId);
        }

        /// <summary>
        /// Get a strain asset by ID
        /// </summary>
        public CannabisStrainAssetSO GetStrainAssetById(string strainId)
        {
            return _strainAssets.FirstOrDefault(s => s.StrainId == strainId);
        }

        /// <summary>
        /// Add a new base strain to the database
        /// </summary>
        public void AddBaseStrain(GeneticPlantStrainSO strain)
        {
            if (strain != null && !_baseStrains.Contains(strain))
            {
                _baseStrains.Add(strain);
            }
        }

        /// <summary>
        /// Add a new strain asset to the database
        /// </summary>
        public void AddStrainAsset(CannabisStrainAssetSO strainAsset)
        {
            if (strainAsset != null && !_strainAssets.Contains(strainAsset))
            {
                _strainAssets.Add(strainAsset);
            }
        }

        /// <summary>
        /// Get strains by type (Indica, Sativa, Hybrid)
        /// </summary>
        public List<GeneticPlantStrainSO> GetStrainsByType(StrainType strainType)
        {
            return _baseStrains.Where(s => s.StrainType == strainType).ToList();
        }

        /// <summary>
        /// Initialize the database with default strains if empty
        /// </summary>
        public void InitializeDatabase()
        {
            if (_baseStrains.Count == 0 && _autoGenerateStrains)
            {
                GenerateDefaultStrains();
            }
        }

        /// <summary>
        /// Generate default cannabis strains for the database
        /// </summary>
        private void GenerateDefaultStrains()
        {
            var defaultStrains = new (string name, StrainType type, string description)[]
            {
                ("Northern Lights", StrainType.Indica, "Classic indica strain known for its relaxing effects"),
                ("Sour Diesel", StrainType.Sativa, "Energizing sativa with diesel aroma"),
                ("Blue Dream", StrainType.Hybrid, "Balanced hybrid with berry flavors"),
                ("White Widow", StrainType.Hybrid, "Potent hybrid with high resin production"),
                ("OG Kush", StrainType.Hybrid, "Legendary strain with earthy flavors"),
                ("Purple Haze", StrainType.Sativa, "Classic sativa with purple coloration"),
                ("Granddaddy Purple", StrainType.Indica, "Deep purple indica with grape flavors"),
                ("Green Crack", StrainType.Sativa, "Energetic sativa with fruity taste"),
                ("Bubba Kush", StrainType.Indica, "Heavy indica with coffee and chocolate notes"),
                ("Jack Herer", StrainType.Sativa, "Uplifting sativa named after the cannabis activist")
            };

            foreach (var (name, type, description) in defaultStrains)
            {
                var strain = CreateInstance<GeneticPlantStrainSO>();
                strain.StrainName = name;
                strain.StrainType = type;
                strain.StrainDescription = description;
                strain.StrainId = System.Guid.NewGuid().ToString();

                // Note: Additional properties like BaseYieldGrams, BaseFloweringTime, etc.
                // would need to be set via their private fields or additional setters
                // For now, we'll rely on the default values set in the ScriptableObject

                _baseStrains.Add(strain);
            }

            SharedLogger.Log($"Generated {defaultStrains.Length} default cannabis strains");
        }

        /// <summary>
        /// Validate the database integrity
        /// </summary>
        public bool ValidateDatabase()
        {
            // Check for duplicate strain IDs
            var strainIds = _baseStrains.Where(s => s != null).Select(s => s.StrainId).ToList();
            if (strainIds.Count != strainIds.Distinct().Count())
            {
                SharedLogger.LogError("Cannabis Strain Database contains duplicate strain IDs");
                return false;
            }

            // Check for null entries
            if (_baseStrains.Any(s => s == null))
            {
                SharedLogger.LogWarning("Cannabis Strain Database contains null strain entries");
                _baseStrains.RemoveAll(s => s == null);
            }

            return true;
        }

        /// <summary>
        /// Get statistics about the strain database
        /// </summary>
        public DatabaseStatistics GetDatabaseStatistics()
        {
            return new DatabaseStatistics
            {
                TotalStrains = _baseStrains.Count,
                IndicaStrains = _baseStrains.Count(s => s.StrainType == StrainType.Indica),
                SativaStrains = _baseStrains.Count(s => s.StrainType == StrainType.Sativa),
                HybridStrains = _baseStrains.Count(s => s.StrainType == StrainType.Hybrid),
                AverageThc = _baseStrains.Average(s => s.THCContent),
                AverageCbd = _baseStrains.Average(s => s.CBDContent),
                AverageYield = _baseStrains.Average(s => s.BaseYield)
            };
        }

        [System.Serializable]
        public class DatabaseStatistics
        {
            public int TotalStrains;
            public int IndicaStrains;
            public int SativaStrains;
            public int HybridStrains;
            public float AverageThc;
            public float AverageCbd;
            public float AverageYield;
        }
    }

}
