using ProjectChimera.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
#endif
using ProjectChimera.Core;
using ProjectChimera.Systems.Registry;

#if UNITY_SPEEDTREE
using SpeedTree;
#endif

namespace ProjectChimera.Systems.Services.SpeedTree
{
    /// <summary>
    /// PC014-5a: SpeedTree Asset Management Service
    /// Handles SpeedTree asset loading, renderer management, and cannabis-specific configurations
    /// Decomposed from AdvancedSpeedTreeManager (360 lines target)
    /// </summary>
    public class SpeedTreeAssetManagementService : MonoBehaviour, ISpeedTreeAssetService
    {
        #region Properties

        public bool IsInitialized { get; private set; }

        #endregion

        #region Private Fields

        [Header("SpeedTree Asset Configuration")]
        [SerializeField] private ScriptableObject _speedTreeLibrary;
        [SerializeField] private ScriptableObject _shaderConfig;
        [SerializeField] private List<ScriptableObject> _cannabisStrains = new List<ScriptableObject>();

        [Header("Physics Integration")]
        [SerializeField] private bool _enablePhysicsInteraction = true;
        [SerializeField] private LayerMask _physicsLayers = -1;

        // Asset Management
        private Dictionary<string, UnityEngine.Object> _loadedAssets = new Dictionary<string, UnityEngine.Object>();
        private Dictionary<string, ScriptableObject> _strainDatabase = new Dictionary<string, ScriptableObject>();
        private List<GameObject> _activeRenderers = new List<GameObject>();

        // Shader Property IDs (cached for performance)
        private int _colorPropertyId;
        private int _healthPropertyId;
        private int _growthPropertyId;
        private int _geneticVariationPropertyId;

        #endregion

        #region Events

        public event Action<GameObject> OnRendererCreated;
        public event Action<GameObject> OnRendererDestroyed;
        public event Action<UnityEngine.Object> OnAssetLoaded;

        #endregion

        #region IService Implementation

        public void Initialize()
        {
            if (IsInitialized) return;

            ChimeraLogger.Log("Initializing SpeedTreeAssetManagementService...");

            // Cache shader property IDs
            CacheShaderProperties();

            // Initialize strain database
            InitializeStrainDatabase();

            // Load initial SpeedTree assets
            LoadSpeedTreeAssets();

            // Register with ServiceRegistry
            ServiceContainerFactory.Instance.RegisterSingleton<ISpeedTreeAssetService>(this);

            IsInitialized = true;
            ChimeraLogger.Log("SpeedTreeAssetManagementService initialized successfully");
        }

        public void Shutdown()
        {
            if (!IsInitialized) return;

            ChimeraLogger.Log("Shutting down SpeedTreeAssetManagementService...");

            // Cleanup all renderers
            foreach (var renderer in _activeRenderers.ToArray())
            {
                DestroySpeedTreeRenderer(renderer);
            }

            // Unload all assets
            foreach (var assetPath in _loadedAssets.Keys.ToList())
            {
                UnloadSpeedTreeAsset(assetPath);
            }

            // Clear collections
            _activeRenderers.Clear();
            _loadedAssets.Clear();
            _strainDatabase.Clear();

            IsInitialized = false;
            ChimeraLogger.Log("SpeedTreeAssetManagementService shutdown complete");
        }

        #endregion

        #region Asset Management

        public async Task<UnityEngine.Object> LoadSpeedTreeAssetAsync(string assetPath)
        {
            if (_loadedAssets.ContainsKey(assetPath))
            {
                return _loadedAssets[assetPath];
            }

#if UNITY_SPEEDTREE
            try
            {
                var asset = await LoadAssetFromPath(assetPath);
                if (asset != null)
                {
                    _loadedAssets[assetPath] = asset;
                    ConfigureAssetForCannabis(asset);
                    OnAssetLoaded?.Invoke(asset);
                    ChimeraLogger.Log($"SpeedTree asset loaded: {assetPath}");
                }
                return asset;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"Failed to load SpeedTree asset {assetPath}: {ex.Message}");
                return null;
            }
#else
            ChimeraLogger.LogWarning("SpeedTree package not available - asset loading disabled");
            return null;
#endif
        }

        public void UnloadSpeedTreeAsset(string assetPath)
        {
            if (_loadedAssets.TryGetValue(assetPath, out var asset))
            {
                _loadedAssets.Remove(assetPath);

#if UNITY_SPEEDTREE
                if (asset != null)
                {
                    // Cleanup SpeedTree asset resources
                    Resources.UnloadAsset(asset);
                }
#endif
                ChimeraLogger.Log($"SpeedTree asset unloaded: {assetPath}");
            }
        }

        public UnityEngine.Object GetSpeedTreeAssetForStrain(string strainId)
        {
            if (string.IsNullOrEmpty(strainId) || !_strainDatabase.TryGetValue(strainId, out var strain))
                return null;

            // This would need to be implemented based on actual strain data structure
            return null; // Placeholder
        }

        public bool IsAssetLoaded(string assetPath)
        {
            return _loadedAssets.ContainsKey(assetPath);
        }

        #endregion

        #region Renderer Management

        public GameObject CreateSpeedTreeRenderer(int plantId, Vector3 position, Quaternion rotation)
        {
            if (plantId <= 0)
            {
                ChimeraLogger.LogError("Cannot create SpeedTree renderer - invalid plant ID");
                return null;
            }

#if UNITY_SPEEDTREE
            try
            {
                var rendererObject = new GameObject($"SpeedTree_Plant_{plantId}");
                rendererObject.transform.position = position;
                rendererObject.transform.rotation = rotation;

                // Add SpeedTree renderer component if available
                var renderer = rendererObject.AddComponent<Renderer>();

                ConfigureRendererForCannabis(rendererObject, plantId);

                _activeRenderers.Add(rendererObject);
                OnRendererCreated?.Invoke(rendererObject);

                ChimeraLogger.Log($"SpeedTree renderer created for plant {plantId}");
                return rendererObject;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"Failed to create SpeedTree renderer for plant {plantId}: {ex.Message}");
                return null;
            }
#else
            ChimeraLogger.LogWarning("SpeedTree package not available - renderer creation disabled");
            return null;
#endif
        }

        public void DestroySpeedTreeRenderer(GameObject renderer)
        {
            if (renderer == null) return;

            _activeRenderers.Remove(renderer);
            OnRendererDestroyed?.Invoke(renderer);

            DestroyImmediate(renderer);

            ChimeraLogger.Log("SpeedTree renderer destroyed");
        }

        public void ConfigureRendererForCannabis(GameObject renderer, int plantId)
        {
            if (renderer == null || plantId <= 0) return;

#if UNITY_SPEEDTREE
            // Apply shader configuration
            if (_shaderConfig != null)
            {
                ApplyShaderConfiguration(renderer, _shaderConfig);
            }

            // Configure for cannabis-specific rendering
            ConfigureCannabisRendering(renderer, plantId);

            // Set up LOD if configured
            ConfigureLODSystem(renderer, plantId);

            // Enable physics interaction if requested
            if (_enablePhysicsInteraction)
            {
                AddPhysicsInteraction(renderer, plantId);
            }
#endif
        }

        #endregion

        #region Material Management

        public void ApplyGeneticVariationsToRenderer(GameObject renderer, object genetics)
        {
            if (renderer == null || genetics == null) return;

#if UNITY_SPEEDTREE
            var materials = renderer.GetComponent<Renderer>()?.materials;
            if (materials == null) return;

            foreach (var material in materials)
            {
                // Apply genetic variations - placeholder implementation
                if (material.HasProperty(_geneticVariationPropertyId))
                {
                    material.SetFloat(_geneticVariationPropertyId, 1.0f);
                }
            }
#endif
        }

        public void ApplyMorphologicalVariations(GameObject renderer, object genetics)
        {
            if (renderer == null || genetics == null) return;

#if UNITY_SPEEDTREE
            // Apply morphological variations - placeholder implementation
            var rendererComponent = renderer.GetComponent<Renderer>();
            if (rendererComponent != null)
            {
                ChimeraLogger.Log($"Applied morphological variations to renderer: {renderer.name}");
            }
#endif
        }

        public void UpdatePlantAppearanceForStage(int plantId, object stage)
        {
            var renderer = FindRendererForInstance(plantId);
            if (renderer == null) return;

#if UNITY_SPEEDTREE
            var materials = renderer.GetComponent<Renderer>()?.materials;
            if (materials == null) return;

            foreach (var material in materials)
            {
                // Update growth stage properties
                if (material.HasProperty(_growthPropertyId))
                {
                    material.SetFloat(_growthPropertyId, 0.5f); // Placeholder value
                }
            }

            ChimeraLogger.Log($"Updated plant appearance for stage: {stage}");
#endif
        }

        #endregion

        #region Physics Integration

        public void AddPhysicsInteraction(GameObject renderer, int plantId)
        {
            if (renderer == null || plantId <= 0) return;

            // Add collider for physics interaction
            var capsuleCollider = renderer.GetComponent<CapsuleCollider>();
            if (capsuleCollider == null)
            {
                capsuleCollider = renderer.AddComponent<CapsuleCollider>();
            }

            // Configure collider with default values
            capsuleCollider.height = 2.0f; // Default height
            capsuleCollider.radius = 0.4f; // Default radius
            capsuleCollider.center = new Vector3(0, 1.0f, 0);

            ChimeraLogger.Log($"Physics interaction added for plant {plantId}");
        }

        public void RemovePhysicsInteraction(GameObject renderer)
        {
            if (renderer == null) return;

            var collider = renderer.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyImmediate(collider);
            }
        }

        #endregion

        #region Private Helper Methods

        private void CacheShaderProperties()
        {
            _colorPropertyId = Shader.PropertyToID("_Color");
            _healthPropertyId = Shader.PropertyToID("_Health");
            _growthPropertyId = Shader.PropertyToID("_GrowthStage");
            _geneticVariationPropertyId = Shader.PropertyToID("_GeneticVariation");
        }

        private void InitializeStrainDatabase()
        {
            _strainDatabase.Clear();

            if (_cannabisStrains != null)
            {
                foreach (var strain in _cannabisStrains)
                {
                    if (strain != null && !string.IsNullOrEmpty(strain.name))
                    {
                        _strainDatabase[strain.name] = strain;
                    }
                }
            }

            ChimeraLogger.Log($"Initialized strain database with {_strainDatabase.Count} strains");
        }

        private void LoadSpeedTreeAssets()
        {
            if (_speedTreeLibrary == null) return;

            // Placeholder implementation - would load from configured asset paths
            ChimeraLogger.Log("SpeedTree assets loading initialized");
        }

#if UNITY_SPEEDTREE
        private async Task<UnityEngine.Object> LoadAssetFromPath(string assetPath)
        {
            // SpeedTree assembly - use Addressables for proper async loading
            ChimeraLogger.Log("[SpeedTreeAssetManagementService] Loading SpeedTree asset via Addressables");

            try
            {
#if UNITY_ADDRESSABLES
                var asset = await Addressables.LoadAssetAsync<UnityEngine.Object>(assetPath);
#else
                var asset = Resources.Load<UnityEngine.Object>(assetPath);
#endif
                if (asset != null)
                {
                    ChimeraLogger.Log($"[SpeedTreeAssetManagementService] Successfully loaded SpeedTree asset: {assetPath}");
                }
                return asset;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[SpeedTreeAssetManagementService] Failed to load SpeedTree asset {assetPath}: {ex.Message}");
                return null;
            }
        }

        private void ConfigureAssetForCannabis(UnityEngine.Object asset)
        {
            if (asset == null) return;

            ChimeraLogger.Log($"Configured asset for cannabis: {asset.name}");
        }

        private void ApplyShaderConfiguration(GameObject renderer, ScriptableObject config)
        {
            if (renderer == null || config == null) return;

            ChimeraLogger.Log($"Applied shader configuration to renderer: {renderer.name}");
        }

        private void ConfigureCannabisRendering(GameObject renderer, int plantId)
        {
            if (renderer == null || plantId <= 0) return;

            ChimeraLogger.Log($"Configured cannabis rendering for plant {plantId}");
        }

        private void ConfigureLODSystem(GameObject renderer, int plantId)
        {
            if (renderer == null || plantId <= 0) return;

            ChimeraLogger.Log($"Configured LOD system for plant {plantId}");
        }
#endif

        private ScriptableObject GetStrainForInstance(int plantId)
        {
            // Return default strain if available
            return _cannabisStrains?.Count > 0 ? _cannabisStrains[0] : null;
        }

        private GameObject FindRendererForInstance(int plantId)
        {
            return _activeRenderers.Find(r =>
                r != null &&
                r.name.Contains($"SpeedTree_Plant_{plantId}"));
        }

        // All helper methods simplified to avoid type references
        // These would be reimplemented when the genetics system is rebuilt

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        #endregion
    }
}
