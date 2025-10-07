using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Services.SpeedTree.Performance
{
    /// <summary>
    /// BASIC: Simple batching manager for Project Chimera's rendering system.
    /// Focuses on essential batching operations without complex GPU instancing and optimization.
    /// </summary>
    public class BatchingManager : MonoBehaviour
    {
        [Header("Basic Batching Settings")]
        [SerializeField] private bool _enableBasicBatching = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private int _maxBatchSize = 100;

        // Basic batch tracking
        private readonly Dictionary<string, List<GameObject>> _batches = new Dictionary<string, List<GameObject>>();
        private bool _isInitialized = false;

        // Quality flags influenced by service settings
        public bool EnableGPUInstancing { get; set; }
        public bool EnableDynamicBatching { get; set; }

        /// <summary>
        /// Events for batching operations
        /// </summary>
        public event System.Action<string, int> OnBatchCreated;
        public event System.Action<string> OnBatchRemoved;

        /// <summary>
        /// Initialize basic batching system
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("SPEEDTREE/BATCH", "BatchingManager initialized", this);
            }
        }

        public void Initialize(SpeedTreeBatchingMethod method)
        {
            Initialize();
            EnableGPUInstancing = method == SpeedTreeBatchingMethod.GPUInstancing;
            EnableDynamicBatching = method == SpeedTreeBatchingMethod.DynamicBatching;
        }

        /// <summary>
        /// Add object to batch
        /// </summary>
        public void AddToBatch(GameObject obj, string batchKey)
        {
            if (!_enableBasicBatching || !_isInitialized || obj == null) return;

            if (!_batches.ContainsKey(batchKey))
            {
                _batches[batchKey] = new List<GameObject>();
                OnBatchCreated?.Invoke(batchKey, 0);
            }

            var batch = _batches[batchKey];
            if (!batch.Contains(obj) && batch.Count < _maxBatchSize)
            {
                batch.Add(obj);

                if (_enableLogging)
                {
                    ChimeraLogger.Log("SPEEDTREE/BATCH", $"Added to batch {batchKey}", this);
                }
            }
        }

        /// <summary>
        /// Remove object from batch
        /// </summary>
        public void RemoveFromBatch(GameObject obj, string batchKey)
        {
            if (!_batches.ContainsKey(batchKey)) return;

            var batch = _batches[batchKey];
            if (batch.Remove(obj))
            {
                if (_enableLogging)
                {
                    ChimeraLogger.Log("SPEEDTREE/BATCH", $"Removed from batch {batchKey}", this);
                }

                // Remove empty batches
                if (batch.Count == 0)
                {
                    _batches.Remove(batchKey);
                    OnBatchRemoved?.Invoke(batchKey);
                }
            }
        }

        /// <summary>
        /// Get batch by key
        /// </summary>
        public List<GameObject> GetBatch(string batchKey)
        {
            return _batches.TryGetValue(batchKey, out var batch) ? new List<GameObject>(batch) : new List<GameObject>();
        }

        /// <summary>
        /// Get all batch keys
        /// </summary>
        public List<string> GetBatchKeys()
        {
            return new List<string>(_batches.Keys);
        }

        /// <summary>
        /// Clear all batches
        /// </summary>
        public void ClearAllBatches()
        {
            var batchKeys = new List<string>(_batches.Keys);
            _batches.Clear();

            foreach (string key in batchKeys)
            {
                OnBatchRemoved?.Invoke(key);
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("SPEEDTREE/BATCH", "Cleared all batches", this);
            }
        }

        /// <summary>
        /// Enable/disable batch rendering
        /// </summary>
        public void SetBatchRendering(string batchKey, bool enabled)
        {
            var batch = GetBatch(batchKey);
            foreach (var obj in batch)
            {
                if (obj != null)
                {
                    var renderers = obj.GetComponentsInChildren<Renderer>();
                    foreach (var renderer in renderers)
                    {
                        renderer.enabled = enabled;
                    }
                }
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("SPEEDTREE/BATCH", $"Set batch {batchKey} rendering: {enabled}", this);
            }
        }

        /// <summary>
        /// Optimize batch performance
        /// </summary>
        public void OptimizeBatch(string batchKey)
        {
            var batch = GetBatch(batchKey);
            if (batch.Count == 0) return;

            // Simple optimization - could combine meshes in a real implementation
            if (_enableLogging)
            {
                ChimeraLogger.Log("SPEEDTREE/BATCH", $"Optimized batch {batchKey}", this);
            }
        }

        // API expected by orchestrator
        public void ClearBatches() => ClearAllBatches();

        public void UpdateBatches()
        {
            // No-op in basic implementation; hook for future metrics/maintenance
        }

        public int GetBatchCount() => _batches.Count;

        public void RemoveFromBatch(GameObject obj)
        {
            if (obj == null) return;
            string emptyKey = null;
            foreach (var kvp in _batches)
            {
                if (kvp.Value.Remove(obj))
                {
                    if (_enableLogging)
                    {
                        ChimeraLogger.Log("SPEEDTREE/BATCH", $"Removed object from batch {kvp.Key}", this);
                    }
                    if (kvp.Value.Count == 0)
                    {
                        emptyKey = kvp.Key;
                    }
                    break;
                }
            }
            if (!string.IsNullOrEmpty(emptyKey))
            {
                _batches.Remove(emptyKey);
                OnBatchRemoved?.Invoke(emptyKey);
            }
        }

        /// <summary>
        /// Get batching statistics
        /// </summary>
        public BatchingStats GetStats()
        {
            int totalObjects = 0;
            int totalBatches = _batches.Count;

            foreach (var batch in _batches.Values)
            {
                totalObjects += batch.Count;
            }

            return new BatchingStats
            {
                TotalBatches = totalBatches,
                TotalObjects = totalObjects,
                AverageBatchSize = totalBatches > 0 ? totalObjects / (float)totalBatches : 0f,
                IsBatchingEnabled = _enableBasicBatching
            };
        }
    }

    /// <summary>
    /// Batching statistics
    /// </summary>
    [System.Serializable]
    public struct BatchingStats
    {
        public int TotalBatches;
        public int TotalObjects;
        public float AverageBatchSize;
        public bool IsBatchingEnabled;
    }
}
