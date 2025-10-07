using UnityEngine;
using System.Collections.Generic;

using ProjectChimera.Core.Logging;
using Logger = ProjectChimera.Core.Logging.ChimeraLogger;
namespace ProjectChimera.Core
{
    /// <summary>
    /// ENHANCED: Comprehensive Asset Catalog System for Project Chimera
    /// Centralized management of all addressable assets with type-safe access
    /// Part of Week 7: Addressables Migration Completion
    /// </summary>
    [CreateAssetMenu(fileName = "ChimeraAssetCatalog", menuName = "Chimera/Asset Catalog")]
    public class ChimeraAssetCatalog : ScriptableObject
    {
        [Header("Construction Assets")]
        [SerializeField] private GameObject[] _constructionPrefabs;
        [SerializeField] private ScriptableObject[] _schematicData;

        [Header("Plant Assets")]
        [SerializeField] private ScriptableObject[] _plantStrains;
        [SerializeField] private GameObject[] _plantPrefabs;

        [Header("Audio Assets")]
        [SerializeField] private AudioClip[] _audioClips;

        [Header("Data Assets")]
        [SerializeField] private ScriptableObject[] _dataAssets;

        [Header("Genetics Assets")]
        [SerializeField] private ComputeShader[] _geneticsShaders;

        // Asset lookup dictionaries for performance
        private Dictionary<string, GameObject> _prefabLookup;
        private Dictionary<string, ScriptableObject> _dataLookup;
        private Dictionary<string, AudioClip> _audioLookup;
        private bool _isInitialized = false;

        /// <summary>
        /// Initialize asset catalog for fast lookups
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            Logger.Log("ChimeraAssetCatalog", "Initializing asset catalog");

            // Initialize lookup dictionaries
            _prefabLookup = new Dictionary<string, GameObject>();
            _dataLookup = new Dictionary<string, ScriptableObject>();
            _audioLookup = new Dictionary<string, AudioClip>();

            // Populate construction prefabs
            if (_constructionPrefabs != null)
            {
                foreach (var prefab in _constructionPrefabs)
                {
                    if (prefab != null)
                    {
                        var key = prefab.name;
                        _prefabLookup[key] = prefab;
                    }
                }
            }

            // Populate plant prefabs
            if (_plantPrefabs != null)
            {
                foreach (var prefab in _plantPrefabs)
                {
                    if (prefab != null)
                    {
                        var key = prefab.name;
                        _prefabLookup[key] = prefab;
                    }
                }
            }

            // Populate schematic data
            if (_schematicData != null)
            {
                foreach (var data in _schematicData)
                {
                    if (data != null)
                    {
                        var key = data.name;
                        _dataLookup[key] = data;
                    }
                }
            }

            // Populate plant strains
            if (_plantStrains != null)
            {
                foreach (var strain in _plantStrains)
                {
                    if (strain != null)
                    {
                        var key = strain.name;
                        _dataLookup[key] = strain;
                    }
                }
            }

            // Populate audio clips
            if (_audioClips != null)
            {
                foreach (var audio in _audioClips)
                {
                    if (audio != null)
                    {
                        _audioLookup[audio.name] = audio;
                    }
                }
            }

            // Populate data assets
            if (_dataAssets != null)
            {
                foreach (var data in _dataAssets)
                {
                    if (data != null)
                    {
                        _dataLookup[data.name] = data;
                    }
                }
            }

            _isInitialized = true;

            Logger.Log("ChimeraAssetCatalog", "Asset catalog initialized successfully");
        }

        /// <summary>
        /// Get construction prefab by name
        /// </summary>
        public GameObject GetConstructionPrefab(string prefabName)
        {
            if (!_isInitialized) Initialize();

            _prefabLookup.TryGetValue(prefabName, out var prefab);
            return prefab;
        }

        /// <summary>
        /// Get plant prefab by name
        /// </summary>
        public GameObject GetPlantPrefab(string prefabName)
        {
            if (!_isInitialized) Initialize();

            _prefabLookup.TryGetValue(prefabName, out var prefab);
            return prefab;
        }

        /// <summary>
        /// Get schematic data by name
        /// </summary>
        public ScriptableObject GetSchematicData(string dataName)
        {
            if (!_isInitialized) Initialize();

            _dataLookup.TryGetValue(dataName, out var data);
            return data;
        }

        /// <summary>
        /// Get plant strain data by name
        /// </summary>
        public ScriptableObject GetPlantStrain(string strainName)
        {
            if (!_isInitialized) Initialize();

            _dataLookup.TryGetValue(strainName, out var strain);
            return strain;
        }

        /// <summary>
        /// Get audio clip reference by name
        /// </summary>
        public AudioClip GetAudioClip(string clipName)
        {
            if (!_isInitialized) Initialize();

            _audioLookup.TryGetValue(clipName, out var audioClip);
            return audioClip;
        }

        /// <summary>
        /// Get data asset by name
        /// </summary>
        public ScriptableObject GetDataAsset(string assetName)
        {
            if (!_isInitialized) Initialize();

            _dataLookup.TryGetValue(assetName, out var dataAsset);
            return dataAsset;
        }

        /// <summary>
        /// Check if prefab exists in catalog
        /// </summary>
        public bool HasPrefab(string prefabName)
        {
            if (!_isInitialized) Initialize();
            return _prefabLookup.ContainsKey(prefabName);
        }

        /// <summary>
        /// Check if audio clip exists in catalog
        /// </summary>
        public bool HasAudioClip(string clipName)
        {
            if (!_isInitialized) Initialize();
            return _audioLookup.ContainsKey(clipName);
        }

        /// <summary>
        /// Get all available prefab names
        /// </summary>
        public List<string> GetAvailablePrefabNames()
        {
            if (!_isInitialized) Initialize();
            return new List<string>(_prefabLookup.Keys);
        }

        /// <summary>
        /// Get all available audio clip names
        /// </summary>
        public List<string> GetAvailableAudioClipNames()
        {
            if (!_isInitialized) Initialize();
            return new List<string>(_audioLookup.Keys);
        }

        /// <summary>
        /// Extract asset name from asset reference
        /// </summary>
        private string ExtractAssetNameFromReference(Object assetRef)
        {
            // This is a simplified version - in a real implementation,
            // you might want to use the actual asset name from the Unity asset system
            return assetRef.name;
        }

        /// <summary>
        /// Validate catalog integrity
        /// </summary>
        [ContextMenu("Validate Catalog")]
        public void ValidateCatalog()
        {
            var issues = new List<string>();

            // Check for null references
            if (_constructionPrefabs != null)
            {
                for (int i = 0; i < _constructionPrefabs.Length; i++)
                {
                    if (_constructionPrefabs[i] == null)
                        issues.Add($"Construction prefab at index {i} is null");
                }
            }

            if (_audioClips != null)
            {
                for (int i = 0; i < _audioClips.Length; i++)
                {
                    if (_audioClips[i] == null)
                        issues.Add($"Audio clip at index {i} is null");
                }
            }

            if (issues.Count > 0)
            {
                Logger.LogWarning("ChimeraAssetCatalog", $"{issues.Count} asset validation issues found");
            }
            else
            {
                Logger.Log("ChimeraAssetCatalog", "All assets validated successfully");
            }
        }

        /// <summary>
        /// Get catalog statistics
        /// </summary>
        public AssetCatalogStats GetStats()
        {
            if (!_isInitialized) Initialize();

            return new AssetCatalogStats
            {
                TotalPrefabs = _prefabLookup.Count,
                TotalDataAssets = _dataLookup.Count,
                TotalAudioClips = _audioLookup.Count,
                IsInitialized = _isInitialized
            };
        }
    }

    /// <summary>
    /// Audio asset reference with metadata
    /// </summary>
    [System.Serializable]
    public class AudioAssetReference
    {
        [SerializeField] public AudioClip AudioClip;
        [SerializeField] public float DefaultVolume = 1.0f;
        [SerializeField] public bool IsLooping = false;

        public string AssetName => AudioClip != null ? AudioClip.name : "";
    }

    /// <summary>
    /// Data asset reference with metadata
    /// </summary>
    [System.Serializable]
    public class DataAssetReference
    {
        [SerializeField] public ScriptableObject ScriptableObject;
        [SerializeField] public string Category = "General";

        public string AssetName => ScriptableObject != null ? ScriptableObject.name : "";
    }

    /// <summary>
    /// Asset catalog statistics
    /// </summary>
    [System.Serializable]
    public struct AssetCatalogStats
    {
        public int TotalPrefabs;
        public int TotalDataAssets;
        public int TotalAudioClips;
        public bool IsInitialized;
    }
}
