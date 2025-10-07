using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Shared;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Genetics;


namespace ProjectChimera.Data.Visuals
{
    /// <summary>
    /// Links GeneticPlantStrainSO genetics data to SpeedTree assets, defining how genetic traits
    /// map to SpeedTree procedural parameters for dynamic plant visualization.
    /// </summary>
    [CreateAssetMenu(fileName = "New SpeedTree Plant Asset", menuName = "Project Chimera/Visuals/SpeedTree Plant Asset", order = 1)]
    public class SpeedTreePlantAssetSO : ChimeraDataSO
    {
        [Header("Asset References")]
        [SerializeField] private GeneticPlantStrainSO _targetStrain;
        [SerializeField, Tooltip("Path to SpeedTree .st/.st9 file in Assets folder")]
        private string _speedTreeAssetPath;
        [SerializeField] private GameObject _speedTreePrefab;

        [Header("Growth Stage Assets")]
        [SerializeField] private List<GrowthStageAsset> _growthStageAssets = new List<GrowthStageAsset>();

        [Header("Genetic Parameter Mapping")]
        [SerializeField] private SpeedTreeParameterMapperSO _parameterMapper;
        
        [Header("Visual Trait Overrides")]
        [SerializeField] private VisualTraitOverrides _visualOverrides;

        [Header("LOD Configuration")]
        [SerializeField] private SpeedTreeLODConfig _lodConfig;

        [Header("Performance Settings")]
        [SerializeField, Range(1, 1000)] private int _maxInstancesInScene = 100;
        [SerializeField] private bool _enableGPUInstancing = true;
        [SerializeField] private bool _enableOcclusionCulling = true;

        // Public Properties
        public GeneticPlantStrainSO TargetStrain => _targetStrain;
        public string SpeedTreeAssetPath => _speedTreeAssetPath;
        public GameObject SpeedTreePrefab => _speedTreePrefab;
        public List<GrowthStageAsset> GrowthStageAssets => _growthStageAssets;
        public SpeedTreeParameterMapperSO ParameterMapper => _parameterMapper;
        public VisualTraitOverrides VisualOverrides => _visualOverrides;
        public SpeedTreeLODConfig LODConfig => _lodConfig;
        public int MaxInstancesInScene => _maxInstancesInScene;
        public bool EnableGPUInstancing => _enableGPUInstancing;
        public bool EnableOcclusionCulling => _enableOcclusionCulling;

        /// <summary>
        /// Gets the appropriate SpeedTree asset for a specific growth stage.
        /// </summary>
        public GrowthStageAsset GetAssetForGrowthStage(PlantGrowthStage stage)
        {
            return _growthStageAssets.Find(asset => asset.GrowthStage == stage);
        }

        /// <summary>
        /// Calculates SpeedTree seasonal parameter (0-1) based on plant growth stage and environmental factors.
        /// </summary>
        public float CalculateSeasonalParameter(PlantGrowthStage currentStage, float environmentalStress = 0f)
        {
            float baseSeasonalValue = currentStage switch
            {
                PlantGrowthStage.Seedling => 0.1f,
                PlantGrowthStage.Vegetative => 0.3f,
                PlantGrowthStage.PreFlowering => 0.5f,
                PlantGrowthStage.Flowering => 0.7f,
                PlantGrowthStage.Ripening => 0.9f,
                PlantGrowthStage.Harvest => 1.0f,
                _ => 0f
            };

            // Apply environmental stress influence
            float stressInfluence = Mathf.Clamp01(environmentalStress) * 0.2f;
            return Mathf.Clamp01(baseSeasonalValue + stressInfluence);
        }

        /// <summary>
        /// Validates SpeedTree asset references and configuration.
        /// </summary>
        public override bool ValidateData()
        {
            bool isValid = base.ValidateData();

            if (_targetStrain == null)
            {
                SharedLogger.LogWarning($"[Chimera] SpeedTreePlantAssetSO '{DisplayName}' has no target strain assigned.", this);
                isValid = false;
            }

            if (string.IsNullOrEmpty(_speedTreeAssetPath))
            {
                SharedLogger.LogWarning($"[Chimera] SpeedTreePlantAssetSO '{DisplayName}' has no SpeedTree asset path specified.", this);
                isValid = false;
            }

            if (_speedTreePrefab == null)
            {
                SharedLogger.LogWarning($"[Chimera] SpeedTreePlantAssetSO '{DisplayName}' has no SpeedTree prefab assigned.", this);
                isValid = false;
            }

            if (_growthStageAssets.Count == 0)
            {
                SharedLogger.LogWarning($"[Chimera] SpeedTreePlantAssetSO '{DisplayName}' has no growth stage assets configured.", this);
                isValid = false;
            }

            return isValid;
        }
    }

    /// <summary>
    /// Configuration for SpeedTree asset specific to a growth stage.
    /// </summary>
    [System.Serializable]
    public class GrowthStageAsset
    {
        [SerializeField] private PlantGrowthStage _growthStage;
        [SerializeField] private GameObject _stageSpecificPrefab;
        [SerializeField] private Material[] _stageMaterials;
        [SerializeField, Range(0f, 1f)] private float _seasonalParameterOverride = -1f; // -1 = use calculated value
        [SerializeField] private Vector3 _scaleMultiplier = Vector3.one;
        
        public PlantGrowthStage GrowthStage => _growthStage;
        public GameObject StageSpecificPrefab => _stageSpecificPrefab;
        public Material[] StageMaterials => _stageMaterials;
        public float SeasonalParameterOverride => _seasonalParameterOverride;
        public Vector3 ScaleMultiplier => _scaleMultiplier;
        
        public bool HasOverride => _seasonalParameterOverride >= 0f;
    }

    /// <summary>
    /// Visual trait modifications that override strain defaults for specific aesthetic choices.
    /// </summary>
    [System.Serializable]
    public class VisualTraitOverrides
    {
        [Header("Color Overrides")]
        [SerializeField] private bool _overrideLeafColor = false;
        [SerializeField] private Color _customLeafColor = Color.green;
        [SerializeField] private bool _overrideBudColor = false;
        [SerializeField] private Color _customBudColor = Color.green;

        [Header("Scale Overrides")]
        [SerializeField] private bool _overrideScale = false;
        [SerializeField] private Vector3 _customScale = Vector3.one;
        [SerializeField] private bool _overrideLeafSize = false;
        [SerializeField, Range(0.5f, 2f)] private float _leafSizeMultiplier = 1f;

        [Header("Density Overrides")]
        [SerializeField] private bool _overrideFoliageDensity = false;
        [SerializeField, Range(0.3f, 2f)] private float _foliageDensityMultiplier = 1f;
        [SerializeField] private bool _overrideBranchDensity = false;
        [SerializeField, Range(0.5f, 2f)] private float _branchDensityMultiplier = 1f;

        // Public Properties
        public bool OverrideLeafColor => _overrideLeafColor;
        public Color CustomLeafColor => _customLeafColor;
        public bool OverrideBudColor => _overrideBudColor;
        public Color CustomBudColor => _customBudColor;
        public bool OverrideScale => _overrideScale;
        public Vector3 CustomScale => _customScale;
        public bool OverrideLeafSize => _overrideLeafSize;
        public float LeafSizeMultiplier => _leafSizeMultiplier;
        public bool OverrideFoliageDensity => _overrideFoliageDensity;
        public float FoliageDensityMultiplier => _foliageDensityMultiplier;
        public bool OverrideBranchDensity => _overrideBranchDensity;
        public float BranchDensityMultiplier => _branchDensityMultiplier;
    }

    /// <summary>
    /// LOD configuration specific to SpeedTree rendering pipeline.
    /// </summary>
    [System.Serializable]
    public class SpeedTreeLODConfig
    {
        [Header("LOD Distances")]
        [SerializeField] private float _highDetailDistance = 10f;
        [SerializeField] private float _mediumDetailDistance = 25f;
        [SerializeField] private float _lowDetailDistance = 50f;
        [SerializeField] private float _cullingDistance = 100f;

        [Header("LOD Quality Settings")]
        [SerializeField, Range(0.1f, 1f)] private float _highDetailQuality = 1f;
        [SerializeField, Range(0.1f, 1f)] private float _mediumDetailQuality = 0.7f;
        [SerializeField, Range(0.1f, 1f)] private float _lowDetailQuality = 0.4f;

        [Header("Wind LOD")]
        [SerializeField] private bool _enableWindOnAllLODs = false;
        [SerializeField] private bool _windOnHighLODOnly = true;

        // Public Properties
        public float HighDetailDistance => _highDetailDistance;
        public float MediumDetailDistance => _mediumDetailDistance;
        public float LowDetailDistance => _lowDetailDistance;
        public float CullingDistance => _cullingDistance;
        public float HighDetailQuality => _highDetailQuality;
        public float MediumDetailQuality => _mediumDetailQuality;
        public float LowDetailQuality => _lowDetailQuality;
        public bool EnableWindOnAllLODs => _enableWindOnAllLODs;
        public bool WindOnHighLODOnly => _windOnHighLODOnly;
    }

}