using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif
#if UNITY_LOCALIZATION
using UnityEngine.Localization;
#endif
using ProjectChimera.Core;
using ProjectChimera.Systems.Diagnostics;

namespace ProjectChimera.Systems.Addressables
{
    /// <summary>
    /// Phase 2 of Addressables migration - Audio, Visual Effects, Materials, and Localization
    /// Handles migration of complex asset types with specific loading requirements
    /// </summary>
    public class AddressablesMigrationPhase2 : MonoBehaviour
    {
        [Header("Migration Configuration")]
        [SerializeField] private bool _enableMigration = true;
        [SerializeField] private bool _useAddressables = true;
        [SerializeField] private float _migrationProgressUpdateInterval = 0.5f;
        
        [Header("Audio Migration")]
        [SerializeField] private List<AudioAssetMapping> _audioAssets = new List<AudioAssetMapping>();
        
        [Header("Visual Effects Migration")]
        [SerializeField] private List<EffectAssetMapping> _effectAssets = new List<EffectAssetMapping>();
        
        [Header("Materials Migration")]
        [SerializeField] private List<MaterialAssetMapping> _materialAssets = new List<MaterialAssetMapping>();
        
        [Header("Localization Migration")]
        [SerializeField] private List<LocalizationAssetMapping> _localizationAssets = new List<LocalizationAssetMapping>();

        private Dictionary<string, AudioClip> _audioCache = new Dictionary<string, AudioClip>();
        private Dictionary<string, ParticleSystem> _effectCache = new Dictionary<string, ParticleSystem>();
        private Dictionary<string, Material> _materialCache = new Dictionary<string, Material>();
#if UNITY_LOCALIZATION
        private Dictionary<string, LocalizedString> _localizationCache = new Dictionary<string, LocalizedString>();
#else
        private Dictionary<string, string> _localizationCache = new Dictionary<string, string>();
#endif
        
        private AddressablesInfrastructure _addressablesInfrastructure;
        private float _migrationProgress = 0f;
        private bool _migrationComplete = false;

        #region Asset Mapping Classes
        
        [Serializable]
        public class AudioAssetMapping
        {
            [Header("Asset Identification")]
            public string AssetName;
            public string ResourcesPath;
            public string AddressableAddress;
            
            [Header("Audio Properties")]
            public AudioType AudioType;
            public bool IsMusic;
            public bool IsAmbient;
            public bool IsSFX;
            
            [Header("Migration Status")]
            public bool MigrationComplete;
            public string LastMigrationError;
        }
        
        [Serializable]
        public class EffectAssetMapping
        {
            [Header("Asset Identification")]
            public string AssetName;
            public string ResourcesPath;
            public string AddressableAddress;
            
            [Header("Effect Properties")]
            public EffectType EffectType;
            public bool RequiresPooling;
            public int PoolSize = 5;
            
            [Header("Migration Status")]
            public bool MigrationComplete;
            public string LastMigrationError;
        }
        
        [Serializable]
        public class MaterialAssetMapping
        {
            [Header("Asset Identification")]
            public string AssetName;
            public string ResourcesPath;
            public string AddressableAddress;
            
            [Header("Material Properties")]
            public MaterialType MaterialType;
            public bool IsSharedMaterial;
            public bool RequiresInstancing;
            
            [Header("Migration Status")]
            public bool MigrationComplete;
            public string LastMigrationError;
        }
        
        [Serializable]
        public class LocalizationAssetMapping
        {
            [Header("Asset Identification")]
            public string AssetName;
            public string ResourcesPath;
            public string AddressableAddress;
            
            [Header("Localization Properties")]
            public LocalizationType LocalizationType;
            public string LocaleCode;
            public string StringTable;
            public string EntryKey;
            
            [Header("Migration Status")]
            public bool MigrationComplete;
            public string LastMigrationError;
        }

        public enum AudioType
        {
            Music,
            SFX,
            Ambient,
            Voice,
            UI
        }
        
        public enum EffectType
        {
            Particle,
            PostProcess,
            Lighting,
            Animation,
            Physics
        }
        
        public enum MaterialType
        {
            Standard,
            Plant,
            Equipment,
            UI,
            Environment,
            SpeedTree
        }
        
        public enum LocalizationType
        {
            UI,
            Gameplay,
            Tutorial,
            Dialogue,
            System
        }
        
        #endregion

        #region Initialization
        
        private void Awake()
        {
            _addressablesInfrastructure = ServiceContainerFactory.Instance?.TryResolve<AddressablesInfrastructure>();
            if (_addressablesInfrastructure == null)
            {
                LoggingInfrastructure.LogError("AddressablesMigrationPhase2", 
                    "AddressablesInfrastructure not found. Migration cannot proceed.");
                enabled = false;
                return;
            }
        }
        
        private void Start()
        {
            if (_enableMigration && !_migrationComplete)
            {
                _ = StartMigrationAsync();
            }
        }
        
        #endregion

        #region Migration Management
        
        public async Task StartMigrationAsync()
        {
            LoggingInfrastructure.LogInfo("AddressablesMigrationPhase2", "Starting Phase 2 migration");
            
            try
            {
                _migrationProgress = 0f;
                
                // Phase 2.1: Audio Assets Migration
                LoggingInfrastructure.LogInfo("AddressablesMigrationPhase2", "Migrating audio assets...");
                await MigrateAudioAssetsAsync();
                _migrationProgress = 0.25f;
                
                // Phase 2.2: Visual Effects Migration
                LoggingInfrastructure.LogInfo("AddressablesMigrationPhase2", "Migrating visual effects...");
                await MigrateEffectAssetsAsync();
                _migrationProgress = 0.5f;
                
                // Phase 2.3: Materials Migration
                LoggingInfrastructure.LogInfo("AddressablesMigrationPhase2", "Migrating materials...");
                await MigrateMaterialAssetsAsync();
                _migrationProgress = 0.75f;
                
                // Phase 2.4: Localization Migration
                LoggingInfrastructure.LogInfo("AddressablesMigrationPhase2", "Migrating localization assets...");
                await MigrateLocalizationAssetsAsync();
                _migrationProgress = 1f;
                
                _migrationComplete = true;
                LoggingInfrastructure.LogInfo("AddressablesMigrationPhase2", "Phase 2 migration completed successfully");
                
                // Validate migration results
                await ValidateMigrationAsync();
            }
            catch (Exception ex)
            {
                LoggingInfrastructure.LogError("AddressablesMigrationPhase2", 
                    $"Migration failed: {ex.Message}");
                throw;
            }
        }
        
        #endregion

        #region Audio Assets Migration
        
        private async Task MigrateAudioAssetsAsync()
        {
            for (int i = 0; i < _audioAssets.Count; i++)
            {
                var mapping = _audioAssets[i];
                try
                {
                    if (mapping.MigrationComplete) continue;
                    
                    LoggingInfrastructure.LogInfo("AddressablesMigrationPhase2", 
                        $"Migrating audio asset: {mapping.AssetName}");
                    
                    // Load via Addressables with fallback
                    AudioClip audioClip = await LoadAudioClipAsync(mapping);
                    
                    if (audioClip != null)
                    {
                        _audioCache[mapping.AddressableAddress] = audioClip;
                        mapping.MigrationComplete = true;
                        mapping.LastMigrationError = string.Empty;
                        
                        LoggingInfrastructure.LogInfo("AddressablesMigrationPhase2", 
                            $"Successfully migrated audio: {mapping.AssetName}");
                    }
                    else
                    {
                        mapping.LastMigrationError = "Failed to load audio clip";
                        LoggingInfrastructure.LogWarning("AddressablesMigrationPhase2", 
                            $"Failed to migrate audio: {mapping.AssetName}");
                    }
                }
                catch (Exception ex)
                {
                    mapping.LastMigrationError = ex.Message;
                    LoggingInfrastructure.LogError("AddressablesMigrationPhase2", 
                        $"Error migrating audio {mapping.AssetName}: {ex.Message}");
                }
                
                await Task.Yield(); // Prevent frame blocking
            }
        }
        
        private async Task<AudioClip> LoadAudioClipAsync(AudioAssetMapping mapping)
        {
            if (_useAddressables && !string.IsNullOrEmpty(mapping.AddressableAddress))
            {
                try
                {
                    return await _addressablesInfrastructure.LoadAssetAsync<AudioClip>(mapping.AddressableAddress);
                }
                catch (Exception ex)
                {
                    LoggingInfrastructure.LogWarning("AddressablesMigrationPhase2", 
                        $"Addressables failed for {mapping.AssetName}, falling back to Resources: {ex.Message}");
                }
            }
            
            // Fallback to Resources.Load
            if (!string.IsNullOrEmpty(mapping.ResourcesPath))
            {
                return Resources.Load<AudioClip>(mapping.ResourcesPath);
            }
            
            return null;
        }
        
        #endregion

        #region Visual Effects Migration
        
        private async Task MigrateEffectAssetsAsync()
        {
            for (int i = 0; i < _effectAssets.Count; i++)
            {
                var mapping = _effectAssets[i];
                try
                {
                    if (mapping.MigrationComplete) continue;
                    
                    LoggingInfrastructure.LogInfo("AddressablesMigrationPhase2", 
                        $"Migrating effect asset: {mapping.AssetName}");
                    
                    // Load effect prefab via Addressables with fallback
                    GameObject effectPrefab = await LoadEffectPrefabAsync(mapping);
                    
                    if (effectPrefab != null)
                    {
                        var particleSystem = effectPrefab.GetComponent<ParticleSystem>();
                        if (particleSystem != null)
                        {
                            _effectCache[mapping.AddressableAddress] = particleSystem;
                            mapping.MigrationComplete = true;
                            mapping.LastMigrationError = string.Empty;
                            
                            LoggingInfrastructure.LogInfo("AddressablesMigrationPhase2", 
                                $"Successfully migrated effect: {mapping.AssetName}");
                        }
                        else
                        {
                            mapping.LastMigrationError = "Effect prefab missing ParticleSystem component";
                        }
                    }
                    else
                    {
                        mapping.LastMigrationError = "Failed to load effect prefab";
                        LoggingInfrastructure.LogWarning("AddressablesMigrationPhase2", 
                            $"Failed to migrate effect: {mapping.AssetName}");
                    }
                }
                catch (Exception ex)
                {
                    mapping.LastMigrationError = ex.Message;
                    LoggingInfrastructure.LogError("AddressablesMigrationPhase2", 
                        $"Error migrating effect {mapping.AssetName}: {ex.Message}");
                }
                
                await Task.Yield(); // Prevent frame blocking
            }
        }
        
        private async Task<GameObject> LoadEffectPrefabAsync(EffectAssetMapping mapping)
        {
            if (_useAddressables && !string.IsNullOrEmpty(mapping.AddressableAddress))
            {
                try
                {
                    return await _addressablesInfrastructure.LoadAssetAsync<GameObject>(mapping.AddressableAddress);
                }
                catch (Exception ex)
                {
                    LoggingInfrastructure.LogWarning("AddressablesMigrationPhase2", 
                        $"Addressables failed for {mapping.AssetName}, falling back to Resources: {ex.Message}");
                }
            }
            
            // Fallback to Resources.Load
            if (!string.IsNullOrEmpty(mapping.ResourcesPath))
            {
                return Resources.Load<GameObject>(mapping.ResourcesPath);
            }
            
            return null;
        }
        
        #endregion

        #region Materials Migration
        
        private async Task MigrateMaterialAssetsAsync()
        {
            for (int i = 0; i < _materialAssets.Count; i++)
            {
                var mapping = _materialAssets[i];
                try
                {
                    if (mapping.MigrationComplete) continue;
                    
                    LoggingInfrastructure.LogInfo("AddressablesMigrationPhase2", 
                        $"Migrating material asset: {mapping.AssetName}");
                    
                    // Load material via Addressables with fallback
                    Material material = await LoadMaterialAsync(mapping);
                    
                    if (material != null)
                    {
                        // Create material instance if required
                        if (mapping.RequiresInstancing)
                        {
                            material = new Material(material);
                        }
                        
                        _materialCache[mapping.AddressableAddress] = material;
                        mapping.MigrationComplete = true;
                        mapping.LastMigrationError = string.Empty;
                        
                        LoggingInfrastructure.LogInfo("AddressablesMigrationPhase2", 
                            $"Successfully migrated material: {mapping.AssetName}");
                    }
                    else
                    {
                        mapping.LastMigrationError = "Failed to load material";
                        LoggingInfrastructure.LogWarning("AddressablesMigrationPhase2", 
                            $"Failed to migrate material: {mapping.AssetName}");
                    }
                }
                catch (Exception ex)
                {
                    mapping.LastMigrationError = ex.Message;
                    LoggingInfrastructure.LogError("AddressablesMigrationPhase2", 
                        $"Error migrating material {mapping.AssetName}: {ex.Message}");
                }
                
                await Task.Yield(); // Prevent frame blocking
            }
        }
        
        private async Task<Material> LoadMaterialAsync(MaterialAssetMapping mapping)
        {
            if (_useAddressables && !string.IsNullOrEmpty(mapping.AddressableAddress))
            {
                try
                {
                    return await _addressablesInfrastructure.LoadAssetAsync<Material>(mapping.AddressableAddress);
                }
                catch (Exception ex)
                {
                    LoggingInfrastructure.LogWarning("AddressablesMigrationPhase2", 
                        $"Addressables failed for {mapping.AssetName}, falling back to Resources: {ex.Message}");
                }
            }
            
            // Fallback to Resources.Load
            if (!string.IsNullOrEmpty(mapping.ResourcesPath))
            {
                return Resources.Load<Material>(mapping.ResourcesPath);
            }
            
            return null;
        }
        
        #endregion

        #region Localization Migration
        
        private async Task MigrateLocalizationAssetsAsync()
        {
            for (int i = 0; i < _localizationAssets.Count; i++)
            {
                var mapping = _localizationAssets[i];
                try
                {
                    if (mapping.MigrationComplete) continue;
                    
                    LoggingInfrastructure.LogInfo("AddressablesMigrationPhase2", 
                        $"Migrating localization asset: {mapping.AssetName}");
                    
                    // Create localized string reference
#if UNITY_LOCALIZATION
                    LocalizedString localizedString = await CreateLocalizedStringAsync(mapping);
#else
                    string localizedString = await CreateLocalizedStringAsync(mapping);
#endif
                    
                    if (localizedString != null)
                    {
                        _localizationCache[mapping.AddressableAddress] = localizedString;
                        mapping.MigrationComplete = true;
                        mapping.LastMigrationError = string.Empty;
                        
                        LoggingInfrastructure.LogInfo("AddressablesMigrationPhase2", 
                            $"Successfully migrated localization: {mapping.AssetName}");
                    }
                    else
                    {
                        mapping.LastMigrationError = "Failed to create localized string";
                        LoggingInfrastructure.LogWarning("AddressablesMigrationPhase2", 
                            $"Failed to migrate localization: {mapping.AssetName}");
                    }
                }
                catch (Exception ex)
                {
                    mapping.LastMigrationError = ex.Message;
                    LoggingInfrastructure.LogError("AddressablesMigrationPhase2", 
                        $"Error migrating localization {mapping.AssetName}: {ex.Message}");
                }
                
                await Task.Yield(); // Prevent frame blocking
            }
        }
        
#if UNITY_LOCALIZATION
        private async Task<LocalizedString> CreateLocalizedStringAsync(LocalizationAssetMapping mapping)
#else
        private async Task<string> CreateLocalizedStringAsync(LocalizationAssetMapping mapping)
#endif
        {
            try
            {
                if (!string.IsNullOrEmpty(mapping.StringTable) && !string.IsNullOrEmpty(mapping.EntryKey))
                {
#if UNITY_LOCALIZATION
                    var localizedString = new LocalizedString(mapping.StringTable, mapping.EntryKey);
                    
                    // Validate the localized string can be loaded
                    var loadOp = localizedString.GetLocalizedStringAsync();
                    await loadOp.Task;
                    
                    if (loadOp.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                    {
                        return localizedString;
                    }
#else
                    // Fallback to simple string key when Localization package not available
                    return $"{mapping.StringTable}:{mapping.EntryKey}";
#endif
                }
                
                return null;
            }
            catch (Exception ex)
            {
                LoggingInfrastructure.LogError("AddressablesMigrationPhase2", 
                    $"Failed to create localized string: {ex.Message}");
                return null;
            }
        }
        
        #endregion

        #region Migration Validation
        
        private async Task ValidateMigrationAsync()
        {
            LoggingInfrastructure.LogInfo("AddressablesMigrationPhase2", "Validating Phase 2 migration...");
            
            int totalAssets = _audioAssets.Count + _effectAssets.Count + _materialAssets.Count + _localizationAssets.Count;
            int migratedAssets = 0;
            int failedAssets = 0;
            
            // Count successful migrations
            foreach (var audio in _audioAssets)
                if (audio.MigrationComplete) migratedAssets++; else failedAssets++;
            
            foreach (var effect in _effectAssets)
                if (effect.MigrationComplete) migratedAssets++; else failedAssets++;
            
            foreach (var material in _materialAssets)
                if (material.MigrationComplete) migratedAssets++; else failedAssets++;
            
            foreach (var localization in _localizationAssets)
                if (localization.MigrationComplete) migratedAssets++; else failedAssets++;
            
            float successRate = totalAssets > 0 ? (float)migratedAssets / totalAssets * 100f : 0f;
            
            LoggingInfrastructure.LogInfo("AddressablesMigrationPhase2", 
                $"Migration validation complete: {migratedAssets}/{totalAssets} assets migrated successfully ({successRate:F1}%)");
            
            if (failedAssets > 0)
            {
                LoggingInfrastructure.LogWarning("AddressablesMigrationPhase2", 
                    $"{failedAssets} assets failed migration - check asset mappings and Resources fallbacks");
            }
            
            await Task.Yield();
        }
        
        #endregion

        #region Public API
        
        public AudioClip GetAudioClip(string address)
        {
            return _audioCache.ContainsKey(address) ? _audioCache[address] : null;
        }
        
        public ParticleSystem GetEffect(string address)
        {
            return _effectCache.ContainsKey(address) ? _effectCache[address] : null;
        }
        
        public Material GetMaterial(string address)
        {
            return _materialCache.ContainsKey(address) ? _materialCache[address] : null;
        }
        
#if UNITY_LOCALIZATION
        public LocalizedString GetLocalizedString(string address)
#else
        public string GetLocalizedString(string address)
#endif
        {
#if UNITY_LOCALIZATION
            return _localizationCache.ContainsKey(address) ? _localizationCache[address] : null;
#else
            return _localizationCache.ContainsKey(address) ? _localizationCache[address] : string.Empty;
#endif
        }
        
        public float GetMigrationProgress() => _migrationProgress;
        public bool IsMigrationComplete() => _migrationComplete;
        
        #endregion
    }
}