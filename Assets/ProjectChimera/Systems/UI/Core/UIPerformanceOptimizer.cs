using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Memory;

namespace ProjectChimera.Systems.UI.Core
{
    /// <summary>
    /// REFACTORED: Focused UI Performance Optimization
    /// Handles only performance optimization concerns: batching, culling, pooling
    /// </summary>
    public class UIPerformanceOptimizer : MonoBehaviour
    {
        [Header("Performance Optimization Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableCanvasCulling = true;
        [SerializeField] private bool _enableBatchedUpdates = true;
        [SerializeField] private float _canvasCullDistance = 200f;
        [SerializeField] private int _maxUpdatesPerFrame = 20;

        // Optimization components
        private readonly List<Canvas> _managedCanvases = new List<Canvas>();
        private readonly Dictionary<UIUpdateType, List<IUIUpdatable>> _batchedUpdates = new Dictionary<UIUpdateType, List<IUIUpdatable>>();
        private readonly MemoryOptimizedQueue<UIUpdateRequest> _updateQueue = new MemoryOptimizedQueue<UIUpdateRequest>();

        // Performance tracking
        private int _optimizationsThisFrame;
        private float _lastOptimizationTime;

        // Events
        public System.Action<OptimizationType> OnOptimizationApplied;

        private void Start()
        {
            InitializeBatchedUpdates();
        }

        /// <summary>
        /// Update optimizations - called by UIManagerCore
        /// </summary>
        public void UpdateOptimizations(float deltaTime)
        {
            _optimizationsThisFrame = 0;

            if (_enableCanvasCulling)
                PerformCanvasCulling();

            if (_enableBatchedUpdates)
                ProcessBatchedUpdates(deltaTime);

            ProcessUpdateQueue();
        }

        /// <summary>
        /// Register canvas for optimization
        /// </summary>
        public void RegisterCanvas(Canvas canvas)
        {
            if (canvas != null && !_managedCanvases.Contains(canvas))
            {
                _managedCanvases.Add(canvas);

                if (_enableLogging)
                    ChimeraLogger.Log("UI", $"✅ Registered canvas for optimization: {canvas.name}", this);
            }
        }

        /// <summary>
        /// Unregister canvas from optimization
        /// </summary>
        public void UnregisterCanvas(Canvas canvas)
        {
            if (_managedCanvases.Remove(canvas))
            {
                if (_enableLogging)
                    ChimeraLogger.Log("UI", $"✅ Unregistered canvas from optimization: {canvas?.name}", this);
            }
        }

        /// <summary>
        /// Queue UI update for batch processing
        /// </summary>
        public void QueueUpdate(UIUpdateRequest request)
        {
            _updateQueue.Enqueue(request);
        }

        /// <summary>
        /// Register UI element for batched updates
        /// </summary>
        public void RegisterForBatchedUpdate(IUIUpdatable updatable, UIUpdateType updateType)
        {
            if (!_batchedUpdates.ContainsKey(updateType))
            {
                _batchedUpdates[updateType] = new List<IUIUpdatable>();
            }

            if (!_batchedUpdates[updateType].Contains(updatable))
            {
                _batchedUpdates[updateType].Add(updatable);
            }
        }

        /// <summary>
        /// Unregister UI element from batched updates
        /// </summary>
        public void UnregisterFromBatchedUpdate(IUIUpdatable updatable, UIUpdateType updateType)
        {
            if (_batchedUpdates.ContainsKey(updateType))
            {
                _batchedUpdates[updateType].Remove(updatable);
            }
        }

        private void InitializeBatchedUpdates()
        {
            var updateTypes = System.Enum.GetValues(typeof(UIUpdateType));
            foreach (UIUpdateType updateType in updateTypes)
            {
                _batchedUpdates[updateType] = new List<IUIUpdatable>();
            }
        }

        private void PerformCanvasCulling()
        {
            var mainCamera = UnityEngine.Camera.main;
            if (mainCamera == null) return;

            int culledCount = 0;

            foreach (var canvas in _managedCanvases)
            {
                if (canvas == null) continue;

                var distance = Vector3.Distance(mainCamera.transform.position, canvas.transform.position);
                var shouldBeCulled = distance > _canvasCullDistance;

                if (canvas.enabled != !shouldBeCulled)
                {
                    canvas.enabled = !shouldBeCulled;
                    if (shouldBeCulled) culledCount++;
                }
            }

            if (culledCount > 0)
            {
                OnOptimizationApplied?.Invoke(OptimizationType.CanvasCulling);
                _optimizationsThisFrame++;
            }
        }

        private void ProcessBatchedUpdates(float deltaTime)
        {
            foreach (var kvp in _batchedUpdates)
            {
                var updateType = kvp.Key;
                var updatables = kvp.Value;

                // Process updates in batches to avoid frame rate spikes
                int processed = 0;
                for (int i = 0; i < updatables.Count && processed < _maxUpdatesPerFrame; i++)
                {
                    var updatable = updatables[i];
                    if (updatable != null && updatable.ShouldUpdate())
                    {
                        updatable.UpdateUI(deltaTime);
                        processed++;
                    }
                }

                if (processed > 0)
                {
                    OnOptimizationApplied?.Invoke(OptimizationType.BatchedUpdate);
                    _optimizationsThisFrame++;
                }
            }
        }

        private void ProcessUpdateQueue()
        {
            int processed = 0;
            while (_updateQueue.Count > 0 && processed < _maxUpdatesPerFrame)
            {
                var request = _updateQueue.Dequeue();
                ProcessUpdateRequest(request);
                processed++;
            }

            if (processed > 0)
            {
                OnOptimizationApplied?.Invoke(OptimizationType.QueuedUpdate);
                _optimizationsThisFrame++;
            }
        }

        private void ProcessUpdateRequest(UIUpdateRequest request)
        {
            // Process individual update request
            switch (request.Type)
            {
                case UIUpdateType.Transform:
                    if (request.Target != null)
                        request.Target.transform.position = request.Position;
                    break;

                case UIUpdateType.Visibility:
                    if (request.Target != null)
                        request.Target.SetActive(request.IsVisible);
                    break;

                case UIUpdateType.Content:
                    // Handle content updates
                    break;
            }
        }

        /// <summary>
        /// Get active optimization count
        /// </summary>
        public int GetActiveOptimizationCount()
        {
            return _optimizationsThisFrame;
        }

        /// <summary>
        /// Get optimization statistics
        /// </summary>
        public UIOptimizationStats GetOptimizationStats()
        {
            return new UIOptimizationStats
            {
                ManagedCanvases = _managedCanvases.Count,
                QueuedUpdates = _updateQueue.Count,
                BatchedUpdatables = GetTotalBatchedUpdatables(),
                OptimizationsThisFrame = _optimizationsThisFrame
            };
        }

        private int GetTotalBatchedUpdatables()
        {
            int total = 0;
            foreach (var kvp in _batchedUpdates)
            {
                total += kvp.Value.Count;
            }
            return total;
        }
    }

    /// <summary>
    /// UI optimization statistics
    /// </summary>
    [System.Serializable]
    public struct UIOptimizationStats
    {
        public int ManagedCanvases;
        public int QueuedUpdates;
        public int BatchedUpdatables;
        public int OptimizationsThisFrame;
    }

    /// <summary>
    /// Types of UI optimizations
    /// </summary>
    public enum OptimizationType
    {
        CanvasCulling,
        BatchedUpdate,
        QueuedUpdate,
        PoolingOptimization
    }
}