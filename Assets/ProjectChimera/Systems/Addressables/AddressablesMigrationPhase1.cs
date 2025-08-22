using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using ProjectChimera.Systems.Diagnostics;

namespace ProjectChimera.Systems.Addressables
{
    /// <summary>
    /// Phase 1 migration for Addressables system
    /// Handles migration of construction prefabs, schematics, UI assets, and equipment/facility prefabs
    /// Provides compatibility layer with Resources.Load fallback during transition
    /// </summary>
    public class AddressablesMigrationPhase1 : MonoBehaviour
    {
        [Header("Migration Phase 1 Configuration")]
        [SerializeField] private bool _enableMigration = true;
        [SerializeField] private bool _enableCompatibilityMode = true;
        [SerializeField] private bool _enableMigrationValidation = true;
        [SerializeField] private bool _enableProgressTracking = true;
        
        [Header("Construction Assets")]
        [SerializeField] private ConstructionAssetMapping[] _constructionPrefabs;
        [SerializeField] private ConstructionAssetMapping[] _schematicAssets;
        
        [Header("UI Assets")]
        [SerializeField] private UIAssetMapping[] _uiTemplates;
        [SerializeField] private UIAssetMapping[] _uiIcons;
        [SerializeField] private UIAssetMapping[] _uiPanels;
        
        [Header("Equipment & Facility Assets")]
        [SerializeField] private EquipmentAssetMapping[] _equipmentPrefabs;
        [SerializeField] private EquipmentAssetMapping[] _facilityPrefabs;
        
        // Core Systems
        private AddressablesInfrastructure _addressablesInfrastructure;
        private AddressablePrefabResolver _prefabResolver;
        
        // Migration State
        private readonly Dictionary<string, string> _resourcesPathToAddressableMap = new Dictionary<string, string>();
        private readonly Dictionary<string, MigrationStatus> _migrationProgress = new Dictionary<string, MigrationStatus>();
        private readonly List<string> _failedMigrations = new List<string>();
        
        // Compatibility Layer
        private readonly Dictionary<string, UnityEngine.Object> _compatibilityCache = new Dictionary<string, UnityEngine.Object>();
        
        // Migration Statistics
        private int _totalAssetsToMigrate = 0;
        private int _successfulMigrations = 0;
        private int _failedMigrationCount = 0;
        
        // Events
        public event Action OnMigrationStarted;
        public event Action OnMigrationCompleted;
        public event Action<string, float> OnMigrationProgress;
        public event Action<string, string> OnMigrationError;
        
        private void Start()
        {
            FindSystemReferences();
            
            if (_enableMigration)
            {
                _ = StartMigrationAsync();
            }
        }
        
        private void FindSystemReferences()
        {
            _addressablesInfrastructure = FindObjectOfType<AddressablesInfrastructure>();
            _prefabResolver = UnityEngine.Object.FindObjectOfType<AddressablePrefabResolver>();
            
            if (_addressablesInfrastructure == null)
            {
                LoggingInfrastructure.LogError("AddressablesMigrationPhase1", "AddressablesInfrastructure not found");
            }
            
            if (_prefabResolver == null)
            {
                LoggingInfrastructure.LogWarning("AddressablesMigrationPhase1", "AddressablePrefabResolver not found");
            }
        }
        
        private async Task StartMigrationAsync()
        {
            LoggingInfrastructure.LogInfo("AddressablesMigrationPhase1", "Starting Phase 1 Addressables migration");
            OnMigrationStarted?.Invoke();
            
            try
            {
                // Calculate total assets to migrate
                CalculateTotalAssets();
                
                // Build migration mapping
                BuildMigrationMapping();
                
                // Execute migration phases
                await MigrateConstructionAssetsAsync();
                await MigrateUIAssetsAsync();
                await MigrateEquipmentAndFacilityAssetsAsync();
                
                // Validate migration if enabled
                if (_enableMigrationValidation)
                {
                    await ValidateMigrationAsync();
                }
                
                // Log migration results
                LogMigrationResults();
                
                OnMigrationCompleted?.Invoke();
                LoggingInfrastructure.LogInfo("AddressablesMigrationPhase1", "Phase 1 migration completed successfully");
            }
            catch (Exception ex)
            {
                LoggingInfrastructure.LogError("AddressablesMigrationPhase1", "Error during Phase 1 migration", ex);
                OnMigrationError?.Invoke("Migration", ex.Message);
            }
        }
        
        private void CalculateTotalAssets()
        {
            _totalAssetsToMigrate = 0;
            
            if (_constructionPrefabs != null) _totalAssetsToMigrate += _constructionPrefabs.Length;
            if (_schematicAssets != null) _totalAssetsToMigrate += _schematicAssets.Length;
            if (_uiTemplates != null) _totalAssetsToMigrate += _uiTemplates.Length;
            if (_uiIcons != null) _totalAssetsToMigrate += _uiIcons.Length;
            if (_uiPanels != null) _totalAssetsToMigrate += _uiPanels.Length;
            if (_equipmentPrefabs != null) _totalAssetsToMigrate += _equipmentPrefabs.Length;
            if (_facilityPrefabs != null) _totalAssetsToMigrate += _facilityPrefabs.Length;
            
            LoggingInfrastructure.LogInfo("AddressablesMigrationPhase1", $"Total assets to migrate: {_totalAssetsToMigrate}");
        }
        
        private void BuildMigrationMapping()
        {
            LoggingInfrastructure.LogInfo("AddressablesMigrationPhase1", "Building migration mapping");
            
            // Construction assets
            BuildMappingForCategory(_constructionPrefabs, "Construction");
            BuildMappingForCategory(_schematicAssets, "Schematics");
            
            // UI assets
            BuildMappingForCategory(_uiTemplates, "UI Templates");
            BuildMappingForCategory(_uiIcons, "UI Icons");
            BuildMappingForCategory(_uiPanels, "UI Panels");
            
            // Equipment and facility assets
            BuildMappingForCategory(_equipmentPrefabs, "Equipment");
            BuildMappingForCategory(_facilityPrefabs, "Facilities");
        }
        
        private void BuildMappingForCategory<T>(T[] assets, string categoryName) where T : AssetMapping
        {
            if (assets == null) return;
            
            foreach (var asset in assets)
            {
                if (!string.IsNullOrEmpty(asset.ResourcesPath) && !string.IsNullOrEmpty(asset.AddressablePath))
                {
                    _resourcesPathToAddressableMap[asset.ResourcesPath] = asset.AddressablePath;
                    _migrationProgress[asset.AddressablePath] = new MigrationStatus
                    {
                        AddressablePath = asset.AddressablePath,
                        ResourcesPath = asset.ResourcesPath,
                        Category = categoryName,
                        Status = MigrationState.Pending
                    };
                }
            }
        }
        
        private async Task MigrateConstructionAssetsAsync()
        {
            LoggingInfrastructure.LogInfo("AddressablesMigrationPhase1", "Migrating construction assets");
            
            // Migrate construction prefabs
            await MigrateAssetCategory(_constructionPrefabs, "Construction Prefabs");
            
            // Migrate schematic assets
            await MigrateAssetCategory(_schematicAssets, "Schematic Assets");
        }
        
        private async Task MigrateUIAssetsAsync()
        {
            LoggingInfrastructure.LogInfo("AddressablesMigrationPhase1", "Migrating UI assets");
            
            // Migrate UI templates
            await MigrateAssetCategory(_uiTemplates, "UI Templates");
            
            // Migrate UI icons
            await MigrateAssetCategory(_uiIcons, "UI Icons");
            
            // Migrate UI panels
            await MigrateAssetCategory(_uiPanels, "UI Panels");
        }
        
        private async Task MigrateEquipmentAndFacilityAssetsAsync()
        {
            LoggingInfrastructure.LogInfo("AddressablesMigrationPhase1", "Migrating equipment and facility assets");
            
            // Migrate equipment prefabs
            await MigrateAssetCategory(_equipmentPrefabs, "Equipment Prefabs");
            
            // Migrate facility prefabs
            await MigrateAssetCategory(_facilityPrefabs, "Facility Prefabs");
        }
        
        private async Task MigrateAssetCategory<T>(T[] assets, string categoryName) where T : AssetMapping
        {
            if (assets == null || assets.Length == 0) return;
            
            LoggingInfrastructure.LogInfo("AddressablesMigrationPhase1", $"Migrating {categoryName}: {assets.Length} assets");
            
            var migrationTasks = new List<Task>();
            
            foreach (var asset in assets)
            {
                migrationTasks.Add(MigrateAssetAsync(asset, categoryName));
            }
            
            await Task.WhenAll(migrationTasks);
        }
        
        private async Task MigrateAssetAsync(AssetMapping asset, string category)
        {
            if (string.IsNullOrEmpty(asset.AddressablePath))
            {
                LoggingInfrastructure.LogWarning("AddressablesMigrationPhase1", $"Empty addressable path for {category} asset");
                return;
            }
            
            try
            {
                // Update migration status
                if (_migrationProgress.TryGetValue(asset.AddressablePath, out var status))
                {
                    status.Status = MigrationState.InProgress;
                    status.StartTime = DateTime.UtcNow;
                }
                
                // Attempt to load via Addressables
                UnityEngine.Object loadedAsset = null;
                
                if (asset is ConstructionAssetMapping || asset is EquipmentAssetMapping)
                {
                    // Load as GameObject for prefabs
                    loadedAsset = await _addressablesInfrastructure.LoadAssetAsync<GameObject>(asset.AddressablePath);
                }
                else if (asset is UIAssetMapping uiAsset)
                {
                    // Load based on UI asset type
                    switch (uiAsset.AssetType)
                    {
                        case UIAssetType.Template:
                        case UIAssetType.Panel:
                            loadedAsset = await _addressablesInfrastructure.LoadAssetAsync<GameObject>(asset.AddressablePath);
                            break;
                        case UIAssetType.Icon:
                            loadedAsset = await _addressablesInfrastructure.LoadAssetAsync<Sprite>(asset.AddressablePath);
                            break;
                        case UIAssetType.Texture:
                            loadedAsset = await _addressablesInfrastructure.LoadAssetAsync<Texture2D>(asset.AddressablePath);
                            break;
                    }
                }
                
                if (loadedAsset != null)
                {
                    // Migration successful
                    if (status != null)
                    {
                        status.Status = MigrationState.Completed;
                        status.EndTime = DateTime.UtcNow;
                        status.Success = true;
                    }
                    
                    _successfulMigrations++;
                    
                    // Cache for compatibility layer if enabled
                    if (_enableCompatibilityMode && !string.IsNullOrEmpty(asset.ResourcesPath))
                    {
                        _compatibilityCache[asset.ResourcesPath] = loadedAsset;
                    }
                    
                    LoggingInfrastructure.LogTrace("AddressablesMigrationPhase1", $"Successfully migrated: {asset.AddressablePath}");
                }
                else
                {
                    // Migration failed
                    if (status != null)
                    {
                        status.Status = MigrationState.Failed;
                        status.EndTime = DateTime.UtcNow;
                        status.Success = false;
                        status.ErrorMessage = "Failed to load via Addressables";
                    }
                    
                    _failedMigrationCount++;
                    _failedMigrations.Add(asset.AddressablePath);
                    
                    LoggingInfrastructure.LogWarning("AddressablesMigrationPhase1", $"Failed to migrate: {asset.AddressablePath}");
                    OnMigrationError?.Invoke(asset.AddressablePath, "Failed to load via Addressables");
                }
                
                // Update progress
                UpdateMigrationProgress();
            }
            catch (Exception ex)
            {
                // Migration error
                if (_migrationProgress.TryGetValue(asset.AddressablePath, out var status))
                {
                    status.Status = MigrationState.Failed;
                    status.EndTime = DateTime.UtcNow;
                    status.Success = false;
                    status.ErrorMessage = ex.Message;
                }
                
                _failedMigrationCount++;
                _failedMigrations.Add(asset.AddressablePath);
                
                LoggingInfrastructure.LogError("AddressablesMigrationPhase1", $"Exception migrating {asset.AddressablePath}", ex);
                OnMigrationError?.Invoke(asset.AddressablePath, ex.Message);
            }
        }
        
        private void UpdateMigrationProgress()
        {
            var completedMigrations = _successfulMigrations + _failedMigrationCount;
            var progress = _totalAssetsToMigrate > 0 ? (float)completedMigrations / _totalAssetsToMigrate : 0f;
            
            if (_enableProgressTracking)
            {
                OnMigrationProgress?.Invoke("Phase1Migration", progress);
            }
        }
        
        private async Task ValidateMigrationAsync()
        {
            LoggingInfrastructure.LogInfo("AddressablesMigrationPhase1", "Validating Phase 1 migration");
            
            var validationTasks = new List<Task>();
            
            foreach (var mapping in _resourcesPathToAddressableMap)
            {
                validationTasks.Add(ValidateAssetMigrationAsync(mapping.Key, mapping.Value));
            }
            
            await Task.WhenAll(validationTasks);
        }
        
        private async Task ValidateAssetMigrationAsync(string resourcesPath, string addressablePath)
        {
            try
            {
                // Attempt to load the asset via Addressables
                var asset = await _addressablesInfrastructure.LoadAssetAsync<UnityEngine.Object>(addressablePath);
                
                if (asset == null)
                {
                    LoggingInfrastructure.LogWarning("AddressablesMigrationPhase1", $"Validation failed for {addressablePath} - asset not loadable");
                    return;
                }
                
                // Try compatibility fallback if enabled
                if (_enableCompatibilityMode)
                {
                    var fallbackAsset = await LoadAssetWithFallbackAsync<UnityEngine.Object>(resourcesPath);
                    if (fallbackAsset == null)
                    {
                        LoggingInfrastructure.LogWarning("AddressablesMigrationPhase1", $"Validation warning: fallback failed for {resourcesPath}");
                    }
                }
                
                LoggingInfrastructure.LogTrace("AddressablesMigrationPhase1", $"Validation passed for {addressablePath}");
            }
            catch (Exception ex)
            {
                LoggingInfrastructure.LogError("AddressablesMigrationPhase1", $"Validation error for {addressablePath}", ex);
            }
        }
        
        private void LogMigrationResults()
        {
            var successRate = _totalAssetsToMigrate > 0 ? (float)_successfulMigrations / _totalAssetsToMigrate * 100f : 0f;
            
            LoggingInfrastructure.LogInfo("AddressablesMigrationPhase1", $"Phase 1 Migration Results:");
            LoggingInfrastructure.LogInfo("AddressablesMigrationPhase1", $"  Total Assets: {_totalAssetsToMigrate}");
            LoggingInfrastructure.LogInfo("AddressablesMigrationPhase1", $"  Successful: {_successfulMigrations}");
            LoggingInfrastructure.LogInfo("AddressablesMigrationPhase1", $"  Failed: {_failedMigrationCount}");
            LoggingInfrastructure.LogInfo("AddressablesMigrationPhase1", $"  Success Rate: {successRate:F1}%");
            
            if (_failedMigrations.Count > 0)
            {
                LoggingInfrastructure.LogWarning("AddressablesMigrationPhase1", $"Failed migrations: {string.Join(", ", _failedMigrations)}");
            }
        }
        
        // Compatibility Layer Methods
        
        public async Task<T> LoadAssetWithFallbackAsync<T>(string resourcesPath) where T : UnityEngine.Object
        {
            // Check if we have an Addressable mapping
            if (_resourcesPathToAddressableMap.TryGetValue(resourcesPath, out var addressablePath))
            {
                try
                {
                    var asset = await _addressablesInfrastructure.LoadAssetAsync<T>(addressablePath);
                    if (asset != null)
                    {
                        return asset;
                    }
                }
                catch (Exception ex)
                {
                    LoggingInfrastructure.LogWarning("AddressablesMigrationPhase1", $"Addressables load failed for {addressablePath}, falling back to Resources: {ex.Message}");
                }
            }
            
            // Fallback to Resources.Load
            if (_enableCompatibilityMode)
            {
                try
                {
                    // Check compatibility cache first
                    if (_compatibilityCache.TryGetValue(resourcesPath, out var cachedAsset) && cachedAsset is T)
                    {
                        return cachedAsset as T;
                    }
                    
                    // Load from Resources
                    var resourceAsset = Resources.Load<T>(resourcesPath);
                    if (resourceAsset != null)
                    {
                        _compatibilityCache[resourcesPath] = resourceAsset;
                        return resourceAsset;
                    }
                }
                catch (Exception ex)
                {
                    LoggingInfrastructure.LogError("AddressablesMigrationPhase1", $"Resources.Load fallback failed for {resourcesPath}", ex);
                }
            }
            
            return null;
        }
        
        public async Task<GameObject> LoadConstructionPrefabAsync(string resourcesPath)
        {
            return await LoadAssetWithFallbackAsync<GameObject>(resourcesPath);
        }
        
        public async Task<GameObject> LoadEquipmentPrefabAsync(string resourcesPath)
        {
            return await LoadAssetWithFallbackAsync<GameObject>(resourcesPath);
        }
        
        public async Task<GameObject> LoadUIPrefabAsync(string resourcesPath)
        {
            return await LoadAssetWithFallbackAsync<GameObject>(resourcesPath);
        }
        
        public async Task<Sprite> LoadUIIconAsync(string resourcesPath)
        {
            return await LoadAssetWithFallbackAsync<Sprite>(resourcesPath);
        }
        
        public Dictionary<string, object> GetMigrationStatus()
        {
            var categoryStats = new Dictionary<string, object>();
            
            foreach (var category in new[] { "Construction", "Schematics", "UI Templates", "UI Icons", "UI Panels", "Equipment", "Facilities" })
            {
                var categoryProgress = _migrationProgress.Values.Where(p => p.Category == category).ToArray();
                categoryStats[category] = new
                {
                    total = categoryProgress.Length,
                    completed = categoryProgress.Count(p => p.Status == MigrationState.Completed),
                    failed = categoryProgress.Count(p => p.Status == MigrationState.Failed),
                    in_progress = categoryProgress.Count(p => p.Status == MigrationState.InProgress)
                };
            }
            
            return new Dictionary<string, object>
            {
                ["total_assets"] = _totalAssetsToMigrate,
                ["successful_migrations"] = _successfulMigrations,
                ["failed_migrations"] = _failedMigrationCount,
                ["success_rate"] = _totalAssetsToMigrate > 0 ? (float)_successfulMigrations / _totalAssetsToMigrate : 0f,
                ["compatibility_mode_enabled"] = _enableCompatibilityMode,
                ["cached_assets"] = _compatibilityCache.Count,
                ["failed_migration_list"] = _failedMigrations.ToArray(),
                ["category_stats"] = categoryStats
            };
        }
        
        public void ClearCompatibilityCache()
        {
            _compatibilityCache.Clear();
            LoggingInfrastructure.LogInfo("AddressablesMigrationPhase1", "Compatibility cache cleared");
        }
    }
    
    // Asset Mapping Base Classes
    
    [System.Serializable]
    public abstract class AssetMapping
    {
        public string ResourcesPath;
        public string AddressablePath;
        public string Description;
    }
    
    [System.Serializable]
    public class ConstructionAssetMapping : AssetMapping
    {
        public ConstructionAssetType AssetType;
        public string Category;
    }
    
    [System.Serializable]
    public class UIAssetMapping : AssetMapping
    {
        public UIAssetType AssetType;
        public string UICategory;
    }
    
    [System.Serializable]
    public class EquipmentAssetMapping : AssetMapping
    {
        public EquipmentType EquipmentType;
        public string EquipmentCategory;
    }
    
    // Enums
    
    public enum ConstructionAssetType
    {
        BuildingPrefab,
        RoomPrefab,
        WallPrefab,
        UtilityPrefab,
        SchematicTemplate
    }
    
    public enum UIAssetType
    {
        Template,
        Panel,
        Icon,
        Texture
    }
    
    public enum EquipmentType
    {
        GrowLight,
        HVACUnit,
        IrrigationSystem,
        Sensor,
        ProcessingEquipment,
        FacilityStructure
    }
    
    // Migration Status Tracking
    
    public class MigrationStatus
    {
        public string AddressablePath;
        public string ResourcesPath;
        public string Category;
        public MigrationState Status;
        public DateTime StartTime;
        public DateTime EndTime;
        public bool Success;
        public string ErrorMessage;
        
        public TimeSpan Duration => EndTime > StartTime ? EndTime - StartTime : TimeSpan.Zero;
    }
    
    public enum MigrationState
    {
        Pending,
        InProgress,
        Completed,
        Failed
    }
}