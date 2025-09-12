using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Services.SpeedTree.Performance
{
    /// <summary>
    /// Manages Level of Detail (LOD) for SpeedTree rendering optimization
    /// Handles distance-based quality reduction and LOD transitions
    /// </summary>
    public class LODManager : ISpeedTreeLODManager
    {
        // LOD configuration
        private SpeedTreeLODConfig _config;
        private Dictionary<GameObject, SpeedTreeRendererData> _rendererData = new Dictionary<GameObject, SpeedTreeRendererData>();

        // LOD state tracking
        private Dictionary<SpeedTreeLODLevel, List<GameObject>> _lodGroups = new Dictionary<SpeedTreeLODLevel, List<GameObject>>();
        private Dictionary<SpeedTreeLODLevel, Material> _lodMaterials = new Dictionary<SpeedTreeLODLevel, Material>();

        // Performance tracking
        private int _totalLODSwitches = 0;
        private float _lastLODUpdate = 0f;
        private float _lodUpdateInterval = 0.1f; // Update LODs 10 times per second

        /// <summary>
        /// Initialize the LOD manager with configuration
        /// </summary>
        public void Initialize(SpeedTreeLODConfig config)
        {
            _config = config;

            // Initialize LOD groups
            foreach (SpeedTreeLODLevel lod in Enum.GetValues(typeof(SpeedTreeLODLevel)))
            {
                _lodGroups[lod] = new List<GameObject>();
            }

            // Create LOD materials if needed
            CreateLODMaterials();

            ChimeraLogger.LogVerbose($"LOD Manager initialized with {config.Distances.Length} LOD levels");
        }

        /// <summary>
        /// Get the appropriate LOD level for a given distance
        /// </summary>
        public SpeedTreeLODLevel GetLODForDistance(float distance)
        {
            if (_config.Distances == null || _config.Distances.Length == 0)
            {
                return SpeedTreeLODLevel.LOD0;
            }

            for (int i = 0; i < _config.Distances.Length; i++)
            {
                if (distance < _config.Distances[i])
                {
                    return (SpeedTreeLODLevel)i;
                }
            }

            return SpeedTreeLODLevel.Culled;
        }

        /// <summary>
        /// Update LODs for all SpeedTree objects
        /// </summary>
        public void UpdateLODs(GameObject[] speedTrees, Vector3 cameraPosition)
        {
            if (Time.time - _lastLODUpdate < _lodUpdateInterval)
            {
                return; // Throttle LOD updates
            }

            _lastLODUpdate = Time.time;

            foreach (GameObject speedTree in speedTrees)
            {
                if (speedTree == null) continue;

                UpdateSingleLOD(speedTree, cameraPosition);
            }
        }

        /// <summary>
        /// Update LOD for a single SpeedTree object
        /// </summary>
        private void UpdateSingleLOD(GameObject speedTree, Vector3 cameraPosition)
        {
            // Get or create renderer data
            if (!_rendererData.TryGetValue(speedTree, out SpeedTreeRendererData data))
            {
                data = new SpeedTreeRendererData(speedTree);
                _rendererData[speedTree] = data;
            }

            // Calculate distance to camera
            data.DistanceToCamera = Vector3.Distance(speedTree.transform.position, cameraPosition);

            // Get appropriate LOD level
            SpeedTreeLODLevel newLOD = GetLODForDistance(data.DistanceToCamera);

            // Check if LOD changed
            if (newLOD != data.CurrentLOD)
            {
                // Remove from old LOD group
                if (_lodGroups.ContainsKey(data.CurrentLOD))
                {
                    _lodGroups[data.CurrentLOD].Remove(speedTree);
                }

                // Add to new LOD group
                if (_lodGroups.ContainsKey(newLOD))
                {
                    _lodGroups[newLOD].Add(speedTree);
                }

                // Apply LOD change
                ApplyLODChange(speedTree, data, newLOD);
                data.CurrentLOD = newLOD;
                _totalLODSwitches++;

                ChimeraLogger.LogVerbose($"LOD changed for {speedTree.name}: {data.CurrentLOD} -> {newLOD}");
            }

            data.LastUpdateTime = Time.time;
        }

        /// <summary>
        /// Apply LOD change to a SpeedTree object
        /// </summary>
        private void ApplyLODChange(GameObject speedTree, SpeedTreeRendererData data, SpeedTreeLODLevel newLOD)
        {
            if (data.Renderer == null) return;

            // Apply LOD-specific material
            if (_lodMaterials.TryGetValue(newLOD, out Material lodMaterial))
            {
                data.Renderer.material = lodMaterial;
            }

            // Apply LOD-specific mesh (if available)
            // Note: This would require SpeedTree-specific mesh LOD handling
            // For now, we rely on material-based LOD

            // Update quality multiplier
            data.QualityLevel = GetQualityLevelForLOD(newLOD);

            // Handle culling
            data.IsCulled = (newLOD == SpeedTreeLODLevel.Culled);
            if (data.Renderer != null)
            {
                data.Renderer.enabled = !data.IsCulled;
            }
        }

        /// <summary>
        /// Set new LOD distances
        /// </summary>
        public void SetLODDistances(float[] distances)
        {
            if (distances == null || distances.Length == 0) return;

            _config.Distances = distances;

            // Reinitialize with new distances
            Initialize(_config);

            ChimeraLogger.LogVerbose($"LOD distances updated: {string.Join(", ", distances)}");
        }

        /// <summary>
        /// Get quality multiplier for a specific LOD level
        /// </summary>
        public float GetQualityMultiplier(SpeedTreeLODLevel lod)
        {
            if (_config.QualityMultipliers == null ||
                lod < 0 || (int)lod >= _config.QualityMultipliers.Length)
            {
                return 1f;
            }

            return _config.QualityMultipliers[(int)lod];
        }

        /// <summary>
        /// Force all objects to a specific LOD level
        /// </summary>
        public void ForceLOD(SpeedTreeLODLevel lod)
        {
            foreach (var kvp in _rendererData)
            {
                GameObject speedTree = kvp.Key;
                SpeedTreeRendererData data = kvp.Value;

                if (data.CurrentLOD != lod)
                {
                    ApplyLODChange(speedTree, data, lod);
                    data.CurrentLOD = lod;
                }
            }

            ChimeraLogger.LogVerbose($"Forced all SpeedTrees to LOD: {lod}");
        }

        /// <summary>
        /// Get the count of objects at each LOD level
        /// </summary>
        public Dictionary<SpeedTreeLODLevel, int> GetLODCounts()
        {
            var counts = new Dictionary<SpeedTreeLODLevel, int>();

            foreach (SpeedTreeLODLevel lod in Enum.GetValues(typeof(SpeedTreeLODLevel)))
            {
                counts[lod] = _lodGroups.ContainsKey(lod) ? _lodGroups[lod].Count : 0;
            }

            return counts;
        }

        /// <summary>
        /// Create materials for different LOD levels
        /// </summary>
        private void CreateLODMaterials()
        {
            // Create materials with different quality settings for each LOD
            foreach (SpeedTreeLODLevel lod in Enum.GetValues(typeof(SpeedTreeLODLevel)))
            {
                if (lod == SpeedTreeLODLevel.Culled) continue;

                Material lodMaterial = new Material(Shader.Find("SpeedTree/Billboard"));
                float qualityMultiplier = GetQualityMultiplier(lod);

                // Adjust material properties based on LOD
                lodMaterial.SetFloat("_QualityMultiplier", qualityMultiplier);

                // Reduce texture quality for distant LODs
                if (qualityMultiplier < 0.5f)
                {
                    lodMaterial.SetFloat("_TextureMipBias", 1f);
                }

                _lodMaterials[lod] = lodMaterial;
            }
        }

        /// <summary>
        /// Get quality level for a specific LOD
        /// </summary>
        private SpeedTreeQualityLevel GetQualityLevelForLOD(SpeedTreeLODLevel lod)
        {
            float qualityMultiplier = GetQualityMultiplier(lod);

            if (qualityMultiplier >= 0.9f) return SpeedTreeQualityLevel.Ultra;
            else if (qualityMultiplier >= 0.7f) return SpeedTreeQualityLevel.High;
            else if (qualityMultiplier >= 0.5f) return SpeedTreeQualityLevel.Medium;
            else if (qualityMultiplier >= 0.3f) return SpeedTreeQualityLevel.Low;
            else return SpeedTreeQualityLevel.Minimal;
        }

        /// <summary>
        /// Add a SpeedTree to LOD tracking
        /// </summary>
        public void AddSpeedTree(GameObject speedTree)
        {
            if (!_rendererData.ContainsKey(speedTree))
            {
                _rendererData[speedTree] = new SpeedTreeRendererData(speedTree);
                ChimeraLogger.LogVerbose($"Added SpeedTree to LOD tracking: {speedTree.name}");
            }
        }

        /// <summary>
        /// Remove a SpeedTree from LOD tracking
        /// </summary>
        public void RemoveSpeedTree(GameObject speedTree)
        {
            if (_rendererData.Remove(speedTree))
            {
                // Remove from LOD groups
                foreach (var lodGroup in _lodGroups.Values)
                {
                    lodGroup.Remove(speedTree);
                }

                ChimeraLogger.LogVerbose($"Removed SpeedTree from LOD tracking: {speedTree.name}");
            }
        }

        /// <summary>
        /// Clear all LOD data
        /// </summary>
        public void ClearLODData()
        {
            _rendererData.Clear();

            foreach (var lodGroup in _lodGroups.Values)
            {
                lodGroup.Clear();
            }

            _totalLODSwitches = 0;
            ChimeraLogger.LogVerbose("LOD data cleared");
        }

        /// <summary>
        /// Get LOD statistics
        /// </summary>
        public string GetLODStatistics()
        {
            var counts = GetLODCounts();
            return $"LOD Statistics - Switches: {_totalLODSwitches}, " +
                   $"LOD0: {counts[SpeedTreeLODLevel.LOD0]}, " +
                   $"LOD1: {counts[SpeedTreeLODLevel.LOD1]}, " +
                   $"LOD2: {counts[SpeedTreeLODLevel.LOD2]}, " +
                   $"LOD3: {counts[SpeedTreeLODLevel.LOD3]}, " +
                   $"Culled: {counts[SpeedTreeLODLevel.Culled]}";
        }

        // Public properties
        public SpeedTreeLODConfig Config => _config;
        public int TotalLODSwitches => _totalLODSwitches;
        public Dictionary<GameObject, SpeedTreeRendererData> RendererData => _rendererData;
        public Dictionary<SpeedTreeLODLevel, List<GameObject>> LODGroups => _lodGroups;
    }
}
