using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Streaming.LOD
{
    /// <summary>
    /// REFACTORED: LOD Renderer
    /// Focused component for managing renderer states and applying LOD changes
    /// </summary>
    public class LODRenderer : MonoBehaviour
    {
        [Header("Renderer Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableShadowLOD = true;
        [SerializeField] private bool _enableParticleLOD = true;
        [SerializeField] private bool _enableColliderLOD = true;

        [Header("LOD Thresholds")]
        [SerializeField] private int _shadowDisableLOD = 2;
        [SerializeField] private int _particleReduceLOD = 1;
        [SerializeField] private int _colliderDisableLOD = 3;

        // Properties
        public bool ShadowLODEnabled => _enableShadowLOD;
        public bool ParticleLODEnabled => _enableParticleLOD;
        public bool ColliderLODEnabled => _enableColliderLOD;

        private void Start()
        {
            if (_enableLogging)
                ChimeraLogger.Log("LOD", "✅ LOD Renderer initialized", this);
        }

        /// <summary>
        /// Cache original component states for an object
        /// </summary>
        public LODComponentCache CacheOriginalComponents(GameObject gameObject)
        {
            var cache = new LODComponentCache();

            // Cache renderer states
            var renderers = gameObject.GetComponentsInChildren<Renderer>(true);
            cache.RendererStates = new Dictionary<Renderer, RendererState>();
            foreach (var renderer in renderers)
            {
                cache.RendererStates[renderer] = new RendererState
                {
                    Enabled = renderer.enabled,
                    ShadowCastingMode = renderer.shadowCastingMode
                };
            }

            // Cache particle system states
            var particleSystems = gameObject.GetComponentsInChildren<ParticleSystem>(true);
            cache.ParticleStates = new Dictionary<ParticleSystem, ParticleState>();
            foreach (var ps in particleSystems)
            {
                cache.ParticleStates[ps] = new ParticleState
                {
                    IsPlaying = ps.isPlaying,
                    MaxParticles = ps.main.maxParticles
                };
            }

            // Cache collider states
            var colliders = gameObject.GetComponentsInChildren<Collider>(true);
            cache.ColliderStates = new Dictionary<Collider, bool>();
            foreach (var collider in colliders)
            {
                cache.ColliderStates[collider] = collider.enabled;
            }

            // Cache LOD Group if present
            var lodGroup = gameObject.GetComponent<LODGroup>();
            if (lodGroup != null)
            {
                cache.OriginalLODGroup = lodGroup;
                cache.OriginalLODs = lodGroup.GetLODs();
            }

            return cache;
        }

        /// <summary>
        /// Apply LOD level to an object
        /// </summary>
        public void ApplyLODLevel(LODObject lodObject, int lodLevel)
        {
            if (lodObject.GameObject == null) return;

            // Apply renderer LOD
            ApplyRendererLOD(lodObject, lodLevel);

            // Apply particle system LOD
            if (_enableParticleLOD)
            {
                ApplyParticleLOD(lodObject, lodLevel);
            }

            // Apply collider LOD
            if (_enableColliderLOD)
            {
                ApplyColliderLOD(lodObject, lodLevel);
            }

            // Apply Unity LODGroup if present
            ApplyUnityLODGroup(lodObject, lodLevel);

            if (_enableLogging)
                ChimeraLogger.Log("LOD", $"Applied LOD level {lodLevel} to {lodObject.GameObject.name}", this);
        }

        /// <summary>
        /// Restore original component states
        /// </summary>
        public void RestoreOriginalComponents(LODObject lodObject)
        {
            if (lodObject.GameObject == null) return;

            var cache = lodObject.OriginalComponents;

            // Restore renderers
            foreach (var kvp in cache.RendererStates)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.enabled = kvp.Value.Enabled;
                    kvp.Key.shadowCastingMode = kvp.Value.ShadowCastingMode;
                }
            }

            // Restore particles
            foreach (var kvp in cache.ParticleStates)
            {
                if (kvp.Key != null)
                {
                    var main = kvp.Key.main;
                    main.maxParticles = kvp.Value.MaxParticles;

                    if (kvp.Value.IsPlaying && !kvp.Key.isPlaying)
                    {
                        kvp.Key.Play();
                    }
                    else if (!kvp.Value.IsPlaying && kvp.Key.isPlaying)
                    {
                        kvp.Key.Stop();
                    }
                }
            }

            // Restore colliders
            if (cache.ColliderStates != null)
            foreach (var kvp in cache.ColliderStates)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.enabled = kvp.Value;
                }
            }

            // Restore Unity LODGroup
            if (cache.OriginalLODGroup != null && cache.OriginalLODs != null)
            {
                cache.OriginalLODGroup.SetLODs(cache.OriginalLODs);
            }

            if (_enableLogging)
                ChimeraLogger.Log("LOD", $"Restored original components for {lodObject.GameObject.name}", this);
        }

        /// <summary>
        /// Update LOD settings
        /// </summary>
        public void UpdateLODSettings(bool enableShadowLOD, bool enableParticleLOD, bool enableColliderLOD)
        {
            _enableShadowLOD = enableShadowLOD;
            _enableParticleLOD = enableParticleLOD;
            _enableColliderLOD = enableColliderLOD;

            if (_enableLogging)
                ChimeraLogger.Log("LOD", $"LOD settings updated - Shadow: {enableShadowLOD}, Particle: {enableParticleLOD}, Collider: {enableColliderLOD}", this);
        }

        private void ApplyRendererLOD(LODObject lodObject, int lodLevel)
        {
            var cache = lodObject.OriginalComponents;

            foreach (var kvp in cache.RendererStates)
            {
                var renderer = kvp.Key;
                if (renderer == null) continue;

                var originalState = kvp.Value;

                // Disable renderer at maximum LOD
                if (lodLevel >= 4) // Assuming 4 is max LOD level
                {
                    renderer.enabled = false;
                    continue;
                }

                // Restore enabled state
                renderer.enabled = originalState.Enabled;

                // Apply shadow LOD
                if (_enableShadowLOD && lodLevel >= _shadowDisableLOD)
                {
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                else
                {
                    renderer.shadowCastingMode = originalState.ShadowCastingMode;
                }
            }
        }

        private void ApplyParticleLOD(LODObject lodObject, int lodLevel)
        {
            var cache = lodObject.OriginalComponents;

            foreach (var kvp in cache.ParticleStates)
            {
                var particleSystem = kvp.Key;
                if (particleSystem == null) continue;

                var originalState = kvp.Value;

                // Disable particles at high LOD
                if (lodLevel >= 3)
                {
                    if (particleSystem.isPlaying)
                        particleSystem.Stop();
                    continue;
                }

                // Reduce particle count based on LOD
                var main = particleSystem.main;
                if (lodLevel >= _particleReduceLOD)
                {
                    int reducedParticles = Mathf.Max(1, originalState.MaxParticles / (lodLevel + 1));
                    main.maxParticles = reducedParticles;
                }
                else
                {
                    main.maxParticles = originalState.MaxParticles;
                }

                // Restore playing state
                if (originalState.IsPlaying && !particleSystem.isPlaying)
                {
                    particleSystem.Play();
                }
            }
        }

        private void ApplyColliderLOD(LODObject lodObject, int lodLevel)
        {
            var cache = lodObject.OriginalComponents;

            foreach (var kvp in cache.ColliderStates)
            {
                var collider = kvp.Key;
                if (collider == null) continue;

                var originalEnabled = kvp.Value;

                // Disable colliders at high LOD levels
                if (lodLevel >= _colliderDisableLOD)
                {
                    collider.enabled = false;
                }
                else
                {
                    collider.enabled = originalEnabled;
                }
            }
        }

        private void ApplyUnityLODGroup(LODObject lodObject, int lodLevel)
        {
            var cache = lodObject.OriginalComponents;
            if (cache.OriginalLODGroup == null) return;

            var lodGroup = cache.OriginalLODGroup;
            if (cache.OriginalLODs != null && lodLevel < cache.OriginalLODs.Length)
            {
                // Force specific LOD level on Unity's LODGroup
                lodGroup.ForceLOD(lodLevel);
            }
            else if (lodLevel >= cache.OriginalLODs.Length)
            {
                // Cull the object
                lodGroup.ForceLOD(-1);
            }
        }

        /// <summary>
        /// Get renderer performance statistics
        /// </summary>
        public LODRendererStats GetPerformanceStats()
        {
            return new LODRendererStats
            {
                ShadowLODEnabled = _enableShadowLOD,
                ParticleLODEnabled = _enableParticleLOD,
                ColliderLODEnabled = _enableColliderLOD,
                ShadowDisableLOD = _shadowDisableLOD,
                ParticleReduceLOD = _particleReduceLOD,
                ColliderDisableLOD = _colliderDisableLOD
            };
        }
    }

    /// <summary>
    /// Component cache for LOD objects
    /// </summary>
    [System.Serializable]
    public struct LODComponentCache
    {
        public Dictionary<Renderer, RendererState> RendererStates;
        public Dictionary<ParticleSystem, ParticleState> ParticleStates;
        public Dictionary<Collider, bool> ColliderStates;
        public LODGroup OriginalLODGroup;
        public UnityEngine.LOD[] OriginalLODs;
    }

    /// <summary>
    /// Renderer state data
    /// </summary>
    [System.Serializable]
    public struct RendererState
    {
        public bool Enabled;
        public UnityEngine.Rendering.ShadowCastingMode ShadowCastingMode;
    }

    /// <summary>
    /// Particle system state data
    /// </summary>
    [System.Serializable]
    public struct ParticleState
    {
        public bool IsPlaying;
        public int MaxParticles;
    }

    /// <summary>
    /// LOD renderer performance statistics
    /// </summary>
    [System.Serializable]
    public struct LODRendererStats
    {
        public bool ShadowLODEnabled;
        public bool ParticleLODEnabled;
        public bool ColliderLODEnabled;
        public int ShadowDisableLOD;
        public int ParticleReduceLOD;
        public int ColliderDisableLOD;
    }
}
