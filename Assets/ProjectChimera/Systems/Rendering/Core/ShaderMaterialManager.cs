using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Rendering.Core
{
    /// <summary>
    /// REFACTORED: Shader & Material Manager
    /// Focused component for managing custom shaders and shared materials for optimization
    /// </summary>
    public class ShaderMaterialManager : MonoBehaviour
    {
        [Header("Shader & Material Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableShaderCaching = true;
        [SerializeField] private bool _enableMaterialSharing = true;
        [SerializeField] private int _maxCachedMaterials = 500;

        // Shader management
        private readonly Dictionary<string, Shader> _customShaders = new Dictionary<string, Shader>();
        private readonly Dictionary<string, Material> _sharedMaterials = new Dictionary<string, Material>();
        private readonly Dictionary<string, int> _materialUsageCount = new Dictionary<string, int>();

        // Material cache
        private readonly Queue<string> _materialCacheOrder = new Queue<string>();

        // Performance tracking
        private ShaderStats _stats = new ShaderStats();

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public int LoadedShaderCount => _customShaders.Count;
        public int SharedMaterialCount => _sharedMaterials.Count;
        public ShaderStats Stats => _stats;

        // Events
        public System.Action<string> OnShaderLoaded;
        public System.Action<string> OnMaterialCreated;
        public System.Action<string> OnMaterialDestroyed;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            LoadCustomShaders();
            ResetStats();

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", "✅ Shader & Material Manager initialized", this);
        }

        /// <summary>
        /// Get or create shared material for optimization
        /// </summary>
        public Material GetSharedMaterial(string materialKey, Shader shader, params object[] properties)
        {
            if (!IsEnabled || !_enableMaterialSharing)
            {
                // Create new material if sharing is disabled
                return CreateNewMaterial(shader, properties);
            }

            // Check if material already exists in cache
            if (_sharedMaterials.TryGetValue(materialKey, out var cachedMaterial))
            {
                if (cachedMaterial != null)
                {
                    IncrementMaterialUsage(materialKey);
                    _stats.MaterialCacheHitRate = CalculateCacheHitRate();
                    return cachedMaterial;
                }
                else
                {
                    // Cached material was destroyed, remove from cache
                    _sharedMaterials.Remove(materialKey);
                    _materialUsageCount.Remove(materialKey);
                }
            }

            // Create new shared material
            var newMaterial = CreateNewMaterial(shader, properties);

            if (newMaterial != null)
            {
                CacheMaterial(materialKey, newMaterial);
                OnMaterialCreated?.Invoke(materialKey);

                if (_enableLogging)
                    ChimeraLogger.Log("RENDERING", $"Created shared material: {materialKey}", this);
            }

            return newMaterial;
        }

        /// <summary>
        /// Get custom shader by name
        /// </summary>
        public Shader GetCustomShader(string shaderName)
        {
            return _customShaders.TryGetValue(shaderName, out var shader) ? shader : null;
        }

        /// <summary>
        /// Load custom shader and add to cache
        /// </summary>
        public void LoadShader(string shaderName, string shaderPath)
        {
            if (_customShaders.ContainsKey(shaderName)) return;

            var shader = Shader.Find(shaderPath);
            if (shader != null)
            {
                _customShaders[shaderName] = shader;
                _stats.LoadedShaders++;
                OnShaderLoaded?.Invoke(shaderName);

                if (_enableLogging)
                    ChimeraLogger.Log("RENDERING", $"Loaded custom shader: {shaderName}", this);
            }
            else
            {
                if (_enableLogging)
                    ChimeraLogger.Log("RENDERING", $"⚠️ Failed to load shader: {shaderPath}", this);
            }
        }

        /// <summary>
        /// Release shared material reference
        /// </summary>
        public void ReleaseMaterial(string materialKey)
        {
            if (_materialUsageCount.TryGetValue(materialKey, out var count))
            {
                count--;
                if (count <= 0)
                {
                    // Remove material from cache when no longer used
                    if (_sharedMaterials.TryGetValue(materialKey, out var material))
                    {
                        if (material != null && material != Resources.GetBuiltinResource<Material>("Default-Material"))
                        {
                            DestroyImmediate(material);
                        }
                    }

                    _sharedMaterials.Remove(materialKey);
                    _materialUsageCount.Remove(materialKey);
                    OnMaterialDestroyed?.Invoke(materialKey);

                    if (_enableLogging)
                        ChimeraLogger.Log("RENDERING", $"Released shared material: {materialKey}", this);
                }
                else
                {
                    _materialUsageCount[materialKey] = count;
                }
            }
        }

        /// <summary>
        /// Clear all cached materials
        /// </summary>
        public void ClearMaterialCache()
        {
            foreach (var kvp in _sharedMaterials)
            {
                if (kvp.Value != null && kvp.Value != Resources.GetBuiltinResource<Material>("Default-Material"))
                {
                    DestroyImmediate(kvp.Value);
                }
            }

            _sharedMaterials.Clear();
            _materialUsageCount.Clear();
            _materialCacheOrder.Clear();

            ResetStats();

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", "Material cache cleared", this);
        }

        /// <summary>
        /// Get shader and material statistics
        /// </summary>
        public ShaderStats GetShaderStats()
        {
            UpdateStats();
            return _stats;
        }

        /// <summary>
        /// Set shader/material management enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                ClearMaterialCache();
            }

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"Shader & Material Manager: {(enabled ? "enabled" : "disabled")}", this);
        }

        /// <summary>
        /// Optimize material cache by removing least used materials
        /// </summary>
        public void OptimizeMaterialCache()
        {
            if (_sharedMaterials.Count <= _maxCachedMaterials) return;

            int materialsToRemove = _sharedMaterials.Count - _maxCachedMaterials + 50; // Remove extra for buffer

            var materialsByUsage = new List<KeyValuePair<string, int>>();
            foreach (var kvp in _materialUsageCount)
            {
                materialsByUsage.Add(kvp);
            }

            // Sort by usage count (ascending)
            materialsByUsage.Sort((a, b) => a.Value.CompareTo(b.Value));

            for (int i = 0; i < materialsToRemove && i < materialsByUsage.Count; i++)
            {
                string materialKey = materialsByUsage[i].Key;
                ReleaseMaterial(materialKey);
            }

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"Material cache optimized - removed {materialsToRemove} materials", this);
        }

        private void LoadCustomShaders()
        {
            // Load common custom shaders
            LoadShader("PlantStandard", "ProjectChimera/PlantStandard");
            LoadShader("PlantTransparent", "ProjectChimera/PlantTransparent");
            LoadShader("PlantCutout", "ProjectChimera/PlantCutout");
            LoadShader("PlantWind", "ProjectChimera/PlantWind");
            LoadShader("PlantInstanced", "ProjectChimera/PlantInstanced");
            LoadShader("EnvironmentStandard", "ProjectChimera/EnvironmentStandard");
            LoadShader("UI_Cultivation", "ProjectChimera/UI_Cultivation");
        }

        private Material CreateNewMaterial(Shader shader, object[] properties)
        {
            if (shader == null) return null;

            var material = new Material(shader);

            // Apply properties if provided
            if (properties != null && properties.Length > 0)
            {
                ApplyMaterialProperties(material, properties);
            }

            return material;
        }

        private void ApplyMaterialProperties(Material material, object[] properties)
        {
            for (int i = 0; i < properties.Length - 1; i += 2)
            {
                if (properties[i] is string propertyName && properties[i + 1] != null)
                {
                    var value = properties[i + 1];

                    switch (value)
                    {
                        case Color color:
                            material.SetColor(propertyName, color);
                            break;
                        case float floatValue:
                            material.SetFloat(propertyName, floatValue);
                            break;
                        case Vector4 vectorValue:
                            material.SetVector(propertyName, vectorValue);
                            break;
                        case Texture texture:
                            material.SetTexture(propertyName, texture);
                            break;
                    }
                }
            }
        }

        private void CacheMaterial(string materialKey, Material material)
        {
            // Ensure cache doesn't exceed limit
            if (_sharedMaterials.Count >= _maxCachedMaterials)
            {
                OptimizeMaterialCache();
            }

            _sharedMaterials[materialKey] = material;
            _materialUsageCount[materialKey] = 1;
            _materialCacheOrder.Enqueue(materialKey);
            _stats.SharedMaterials++;
        }

        private void IncrementMaterialUsage(string materialKey)
        {
            if (_materialUsageCount.TryGetValue(materialKey, out var count))
            {
                _materialUsageCount[materialKey] = count + 1;
            }
        }

        private float CalculateCacheHitRate()
        {
            int totalRequests = _stats.MaterialInstances + _stats.SharedMaterials;
            return totalRequests > 0 ? (float)_stats.SharedMaterials / totalRequests : 0f;
        }

        private void UpdateStats()
        {
            _stats.MaterialInstances = _sharedMaterials.Count;
            _stats.MaterialCacheHitRate = CalculateCacheHitRate();

            // Calculate approximate memory usage
            _stats.ShaderMemoryUsage = _customShaders.Count * 1024; // Rough estimate
        }

        private void ResetStats()
        {
            _stats = new ShaderStats
            {
                LoadedShaders = _customShaders.Count,
                SharedMaterials = _sharedMaterials.Count,
                MaterialInstances = 0,
                MaterialCacheHitRate = 0f,
                ShaderMemoryUsage = 0
            };
        }
    }
}