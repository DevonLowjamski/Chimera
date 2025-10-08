using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Construction.LOD
{
    /// <summary>
    /// REFACTORED: Construction LOD Renderer Controller - Focused renderer and component state management
    /// Handles renderer, collider, and particle system state changes based on LOD levels
    /// Single Responsibility: Component state management for LOD optimization
    /// </summary>
    public class ConstructionLODRendererController : MonoBehaviour
    {
        [Header("Renderer Control Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableShadowOptimization = true;
        [SerializeField] private bool _enableColliderOptimization = true;
        [SerializeField] private bool _enableParticleOptimization = true;

        // LOD configurations for different levels
        private readonly Dictionary<ConstructionLODLevel, LODRenderConfiguration> _lodConfigurations =
            new Dictionary<ConstructionLODLevel, LODRenderConfiguration>();

        // Object component tracking
        private readonly Dictionary<string, ConstructionObjectComponents> _objectComponents =
            new Dictionary<string, ConstructionObjectComponents>();

        // Render state cache for performance
        private readonly Dictionary<string, ConstructionLODLevel> _lastAppliedLODLevels =
            new Dictionary<string, ConstructionLODLevel>();

        // Statistics
        private RendererControllerStats _stats = new RendererControllerStats();

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public int ManagedObjectCount => _objectComponents.Count;
        public RendererControllerStats Stats => _stats;

        // Events
        public System.Action<string, ConstructionLODLevel> OnLODConfigurationApplied;
        public System.Action<string, int> OnRenderersToggled;
        public System.Action<string, bool> OnShadowCastingChanged;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            InitializeLODConfigurations();
            ResetStats();

            if (_enableLogging)
                ChimeraLogger.Log("CONSTRUCTION", "🎨 ConstructionLODRendererController initialized", this);
        }

        /// <summary>
        /// Register object components for LOD control
        /// </summary>
        public void RegisterObject(string objectId, GameObject constructionObject)
        {
            if (!IsEnabled || constructionObject == null) return;

            if (_objectComponents.ContainsKey(objectId))
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("CONSTRUCTION", $"Object components already registered: {objectId}", this);
                return;
            }

            var components = new ConstructionObjectComponents
            {
                ObjectId = objectId,
                GameObject = constructionObject,
                Renderers = constructionObject.GetComponentsInChildren<Renderer>(),
                Colliders = constructionObject.GetComponentsInChildren<Collider>(),
                ParticleSystems = constructionObject.GetComponentsInChildren<ParticleSystem>(),
                OriginalShadowModes = new UnityEngine.Rendering.ShadowCastingMode[0],
                OriginalRendererStates = new bool[0],
                OriginalColliderStates = new bool[0]
            };

            // Cache original states
            CacheOriginalStates(ref components);

            _objectComponents[objectId] = components;
            _lastAppliedLODLevels[objectId] = ConstructionLODLevel.High;
            _stats.RegisteredObjects++;

            if (_enableLogging)
                ChimeraLogger.Log("CONSTRUCTION", $"Registered object components: {objectId} (renderers: {components.Renderers.Length}, colliders: {components.Colliders.Length})", this);
        }

        /// <summary>
        /// Unregister object components
        /// </summary>
        public void UnregisterObject(string objectId)
        {
            if (!IsEnabled) return;

            if (_objectComponents.Remove(objectId))
            {
                _lastAppliedLODLevels.Remove(objectId);
                _stats.RegisteredObjects--;

                if (_enableLogging)
                    ChimeraLogger.Log("CONSTRUCTION", $"Unregistered object components: {objectId}", this);
            }
        }

        /// <summary>
        /// Apply LOD configuration to object
        /// </summary>
        public void ApplyLODConfiguration(string objectId, ConstructionLODLevel lodLevel)
        {
            if (!IsEnabled || !_objectComponents.TryGetValue(objectId, out var components)) return;

            // Skip if same LOD level already applied
            if (_lastAppliedLODLevels.TryGetValue(objectId, out var lastLevel) && lastLevel == lodLevel)
                return;

            var config = _lodConfigurations[lodLevel];
            ApplyConfiguration(components, config, lodLevel);

            _lastAppliedLODLevels[objectId] = lodLevel;
            _stats.LODApplications++;

            OnLODConfigurationApplied?.Invoke(objectId, lodLevel);

            if (_enableLogging)
                ChimeraLogger.Log("CONSTRUCTION", $"Applied LOD configuration to {objectId}: {lodLevel}", this);
        }

        /// <summary>
        /// Restore object to original state
        /// </summary>
        public void RestoreOriginalState(string objectId)
        {
            if (!IsEnabled || !_objectComponents.TryGetValue(objectId, out var components)) return;

            RestoreToOriginalState(components);
            _lastAppliedLODLevels[objectId] = ConstructionLODLevel.High;

            if (_enableLogging)
                ChimeraLogger.Log("CONSTRUCTION", $"Restored original state for {objectId}", this);
        }

        /// <summary>
        /// Restore all objects to original state
        /// </summary>
        public void RestoreAllToOriginalState()
        {
            if (!IsEnabled) return;

            foreach (var objectId in _objectComponents.Keys)
            {
                RestoreOriginalState(objectId);
            }

            if (_enableLogging)
                ChimeraLogger.Log("CONSTRUCTION", "Restored all objects to original state", this);
        }

        /// <summary>
        /// Get object components info
        /// </summary>
        public ConstructionObjectComponents? GetObjectComponents(string objectId)
        {
            return _objectComponents.TryGetValue(objectId, out var components) ? components : null;
        }

        /// <summary>
        /// Update LOD configuration
        /// </summary>
        public void UpdateLODConfiguration(ConstructionLODLevel level, LODRenderConfiguration config)
        {
            _lodConfigurations[level] = config;

            if (_enableLogging)
                ChimeraLogger.Log("CONSTRUCTION", $"Updated LOD configuration for level {level}", this);

            // Re-apply configuration to objects currently at this level
            foreach (var kvp in _lastAppliedLODLevels)
            {
                if (kvp.Value == level)
                {
                    ApplyLODConfiguration(kvp.Key, level);
                }
            }
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                RestoreAllToOriginalState();
            }

            if (_enableLogging)
                ChimeraLogger.Log("CONSTRUCTION", $"ConstructionLODRendererController: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Initialize LOD configurations
        /// </summary>
        private void InitializeLODConfigurations()
        {
            _lodConfigurations[ConstructionLODLevel.High] = new LODRenderConfiguration
            {
                RendererRatio = 1f,
                ColliderRatio = 1f,
                EnableShadowCasting = true,
                EnableParticles = true,
                RendererCullingMask = ~0 // All layers
            };

            _lodConfigurations[ConstructionLODLevel.Medium] = new LODRenderConfiguration
            {
                RendererRatio = 0.75f,
                ColliderRatio = 0.5f,
                EnableShadowCasting = true,
                EnableParticles = false,
                RendererCullingMask = ~0
            };

            _lodConfigurations[ConstructionLODLevel.Low] = new LODRenderConfiguration
            {
                RendererRatio = 0.4f,
                ColliderRatio = 0.25f,
                EnableShadowCasting = false,
                EnableParticles = false,
                RendererCullingMask = 1 << 0 // Only default layer
            };

            _lodConfigurations[ConstructionLODLevel.Culled] = new LODRenderConfiguration
            {
                RendererRatio = 0f,
                ColliderRatio = 0f,
                EnableShadowCasting = false,
                EnableParticles = false,
                RendererCullingMask = 0 // No layers
            };
        }

        /// <summary>
        /// Cache original component states
        /// </summary>
        private void CacheOriginalStates(ref ConstructionObjectComponents components)
        {
            // Cache renderer states and shadow modes
            if (components.Renderers != null)
            {
                components.OriginalRendererStates = new bool[components.Renderers.Length];
                components.OriginalShadowModes = new UnityEngine.Rendering.ShadowCastingMode[components.Renderers.Length];

                for (int i = 0; i < components.Renderers.Length; i++)
                {
                    if (components.Renderers[i] != null)
                    {
                        components.OriginalRendererStates[i] = components.Renderers[i].enabled;
                        components.OriginalShadowModes[i] = components.Renderers[i].shadowCastingMode;
                    }
                }
            }

            // Cache collider states
            if (components.Colliders != null)
            {
                components.OriginalColliderStates = new bool[components.Colliders.Length];

                for (int i = 0; i < components.Colliders.Length; i++)
                {
                    if (components.Colliders[i] != null)
                    {
                        components.OriginalColliderStates[i] = components.Colliders[i].enabled;
                    }
                }
            }
        }

        /// <summary>
        /// Apply configuration to object components
        /// </summary>
        private void ApplyConfiguration(ConstructionObjectComponents components, LODRenderConfiguration config, ConstructionLODLevel lodLevel)
        {
            // Apply renderer configuration
            if (components.Renderers != null && components.Renderers.Length > 0)
            {
                ApplyRendererConfiguration(components, config);
            }

            // Apply collider configuration
            if (_enableColliderOptimization && components.Colliders != null && components.Colliders.Length > 0)
            {
                ApplyColliderConfiguration(components, config);
            }

            // Apply particle system configuration
            if (_enableParticleOptimization && components.ParticleSystems != null && components.ParticleSystems.Length > 0)
            {
                ApplyParticleConfiguration(components, config);
            }

            // Handle complete culling
            if (lodLevel == ConstructionLODLevel.Culled)
            {
                components.GameObject.SetActive(false);
            }
            else if (!components.GameObject.activeInHierarchy)
            {
                components.GameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Apply renderer configuration
        /// </summary>
        private void ApplyRendererConfiguration(ConstructionObjectComponents components, LODRenderConfiguration config)
        {
            int renderersToEnable = Mathf.RoundToInt(components.Renderers.Length * config.RendererRatio);
            int enabledRenderers = 0;

            for (int i = 0; i < components.Renderers.Length; i++)
            {
                var renderer = components.Renderers[i];
                if (renderer == null) continue;

                bool shouldEnable = enabledRenderers < renderersToEnable;
                renderer.enabled = shouldEnable;

                if (_enableShadowOptimization)
                {
                    renderer.shadowCastingMode = config.EnableShadowCasting && shouldEnable
                        ? components.OriginalShadowModes[i]
                        : UnityEngine.Rendering.ShadowCastingMode.Off;
                }

                if (shouldEnable)
                    enabledRenderers++;
            }

            OnRenderersToggled?.Invoke(components.ObjectId, enabledRenderers);
            OnShadowCastingChanged?.Invoke(components.ObjectId, config.EnableShadowCasting);

            _stats.RendererStateChanges++;
        }

        /// <summary>
        /// Apply collider configuration
        /// </summary>
        private void ApplyColliderConfiguration(ConstructionObjectComponents components, LODRenderConfiguration config)
        {
            int collidersToEnable = Mathf.RoundToInt(components.Colliders.Length * config.ColliderRatio);
            int enabledColliders = 0;

            for (int i = 0; i < components.Colliders.Length && enabledColliders < collidersToEnable; i++)
            {
                var collider = components.Colliders[i];
                if (collider == null) continue;

                collider.enabled = enabledColliders < collidersToEnable;

                if (collider.enabled)
                    enabledColliders++;
            }

            _stats.ColliderStateChanges++;
        }

        /// <summary>
        /// Apply particle system configuration
        /// </summary>
        private void ApplyParticleConfiguration(ConstructionObjectComponents components, LODRenderConfiguration config)
        {
            foreach (var particleSystem in components.ParticleSystems)
            {
                if (particleSystem == null) continue;

                if (config.EnableParticles)
                {
                    if (!particleSystem.isPlaying) particleSystem.Play();
                }
                else
                {
                    if (particleSystem.isPlaying) particleSystem.Stop();
                }
            }

            _stats.ParticleStateChanges++;
        }

        /// <summary>
        /// Restore object to original state
        /// </summary>
        private void RestoreToOriginalState(ConstructionObjectComponents components)
        {
            // Restore renderers
            if (components.Renderers != null)
            {
                for (int i = 0; i < components.Renderers.Length; i++)
                {
                    var renderer = components.Renderers[i];
                    if (renderer == null) continue;

                    if (i < components.OriginalRendererStates.Length)
                        renderer.enabled = components.OriginalRendererStates[i];

                    if (_enableShadowOptimization && i < components.OriginalShadowModes.Length)
                        renderer.shadowCastingMode = components.OriginalShadowModes[i];
                }
            }

            // Restore colliders
            if (_enableColliderOptimization && components.Colliders != null)
            {
                for (int i = 0; i < components.Colliders.Length; i++)
                {
                    var collider = components.Colliders[i];
                    if (collider == null) continue;

                    if (i < components.OriginalColliderStates.Length)
                        collider.enabled = components.OriginalColliderStates[i];
                }
            }

            // Restore particle systems
            if (_enableParticleOptimization && components.ParticleSystems != null)
            {
                foreach (var particleSystem in components.ParticleSystems)
                {
                    if (particleSystem != null && !particleSystem.isPlaying)
                    {
                        particleSystem.Play();
                    }
                }
            }

            // Ensure object is active
            if (!components.GameObject.activeInHierarchy)
            {
                components.GameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Reset statistics
        /// </summary>
        private void ResetStats()
        {
            _stats = new RendererControllerStats
            {
                RegisteredObjects = 0,
                LODApplications = 0,
                RendererStateChanges = 0,
                ColliderStateChanges = 0,
                ParticleStateChanges = 0
            };
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// LOD render configuration
    /// </summary>
    [System.Serializable]
    public struct LODRenderConfiguration
    {
        public float RendererRatio;      // Fraction of renderers to keep enabled
        public float ColliderRatio;      // Fraction of colliders to keep enabled
        public bool EnableShadowCasting; // Whether to enable shadow casting
        public bool EnableParticles;     // Whether to enable particle systems
        public int RendererCullingMask;  // Layer mask for culling
    }

    /// <summary>
    /// Construction object components
    /// </summary>
    [System.Serializable]
    public struct ConstructionObjectComponents
    {
        public string ObjectId;
        public GameObject GameObject;
        public Renderer[] Renderers;
        public Collider[] Colliders;
        public ParticleSystem[] ParticleSystems;
        public bool[] OriginalRendererStates;
        public UnityEngine.Rendering.ShadowCastingMode[] OriginalShadowModes;
        public bool[] OriginalColliderStates;
    }

    /// <summary>
    /// Renderer controller statistics
    /// </summary>
    [System.Serializable]
    public struct RendererControllerStats
    {
        public int RegisteredObjects;
        public int LODApplications;
        public int RendererStateChanges;
        public int ColliderStateChanges;
        public int ParticleStateChanges;
    }

    #endregion
}