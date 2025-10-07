using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.UI.Core
{
    /// <summary>
    /// REFACTORED: UI Canvas Optimizer
    /// Single Responsibility: Canvas culling, sorting, and performance optimization
    /// Extracted from OptimizedUIManager for better separation of concerns
    /// </summary>
    public class UICanvasOptimizer : MonoBehaviour
    {
        [Header("Canvas Optimization Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableCanvasCulling = true;
        [SerializeField] private float _canvasCullDistance = 100f;
        [SerializeField] private float _optimizationInterval = 0.5f;

        // Managed canvases
        private readonly List<Canvas> _managedCanvases = new List<Canvas>();
        private readonly Dictionary<Canvas, CanvasOptimizationData> _canvasData = new Dictionary<Canvas, CanvasOptimizationData>();

        // State tracking
        private float _lastOptimizationTime;
        private UnityEngine.Camera _mainCamera;
        private bool _isInitialized = false;

        // Statistics
        private UICanvasOptimizerStats _stats = new UICanvasOptimizerStats();

        // Events
        public event System.Action<Canvas> OnCanvasRegistered;
        public event System.Action<Canvas> OnCanvasUnregistered;
        public event System.Action<Canvas, bool> OnCanvasVisibilityChanged;

        public bool IsInitialized => _isInitialized;
        public UICanvasOptimizerStats Stats => _stats;
        public int ManagedCanvasCount => _managedCanvases.Count;

        public void Initialize()
        {
            if (_isInitialized) return;

            _mainCamera = UnityEngine.Camera.main;
            _managedCanvases.Clear();
            _canvasData.Clear();
            ResetStats();

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("UI", "UI Canvas Optimizer initialized", this);
            }
        }

        /// <summary>
        /// Register canvas for optimization
        /// </summary>
        public bool RegisterCanvas(Canvas canvas)
        {
            if (canvas == null)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("UI", "Cannot register null canvas", this);
                }
                return false;
            }

            if (_managedCanvases.Contains(canvas))
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("UI", $"Canvas {canvas.name} already registered", this);
                }
                return false;
            }

            _managedCanvases.Add(canvas);
            _canvasData[canvas] = new CanvasOptimizationData
            {
                OriginalSortingOrder = canvas.sortingOrder,
                LastVisibilityCheck = Time.time,
                IsVisible = canvas.enabled,
                LastDistance = 0f
            };

            _stats.TotalCanvasesRegistered++;
            OnCanvasRegistered?.Invoke(canvas);

            if (_enableLogging)
            {
                ChimeraLogger.Log("UI", $"Registered canvas: {canvas.name} ({_managedCanvases.Count} total managed)", this);
            }

            return true;
        }

        /// <summary>
        /// Unregister canvas from optimization
        /// </summary>
        public bool UnregisterCanvas(Canvas canvas)
        {
            if (canvas == null || !_managedCanvases.Contains(canvas))
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("UI", $"Canvas {canvas?.name ?? "null"} not found for unregistration", this);
                }
                return false;
            }

            _managedCanvases.Remove(canvas);
            _canvasData.Remove(canvas);

            OnCanvasUnregistered?.Invoke(canvas);

            if (_enableLogging)
            {
                ChimeraLogger.Log("UI", $"Unregistered canvas: {canvas.name} ({_managedCanvases.Count} remaining managed)", this);
            }

            return true;
        }

        /// <summary>
        /// Process canvas optimizations (call from Update or ITickable)
        /// </summary>
        public void ProcessOptimizations(float deltaTime)
        {
            if (!_isInitialized) return;

            // Run optimizations at specified intervals
            if (Time.time - _lastOptimizationTime >= _optimizationInterval)
            {
                UpdateCanvasCulling();
                OptimizeCanvasSorting();
                ValidateManagedCanvases();

                _lastOptimizationTime = Time.time;
                _stats.OptimizationCycles++;
            }
        }

        /// <summary>
        /// Update canvas culling based on distance
        /// </summary>
        private void UpdateCanvasCulling()
        {
            if (!_enableCanvasCulling || _mainCamera == null)
                return;

            var cameraPos = _mainCamera.transform.position;
            var currentTime = Time.time;

            foreach (var canvas in _managedCanvases)
            {
                if (canvas == null) continue;

                var canvasData = _canvasData[canvas];
                float distance = Vector3.Distance(cameraPos, canvas.transform.position);
                bool shouldBeVisible = distance <= _canvasCullDistance;

                // Update distance tracking
                canvasData.LastDistance = distance;
                canvasData.LastVisibilityCheck = currentTime;

                if (canvas.enabled != shouldBeVisible)
                {
                    canvas.enabled = shouldBeVisible;
                    canvasData.IsVisible = shouldBeVisible;

                    _stats.CanvasCullOperations++;
                    OnCanvasVisibilityChanged?.Invoke(canvas, shouldBeVisible);

                    if (_enableLogging)
                    {
                        ChimeraLogger.Log("UI", $"Canvas {canvas.name} visibility changed to {shouldBeVisible} (distance: {distance:F1})", this);
                    }
                }

                _canvasData[canvas] = canvasData;
            }
        }

        /// <summary>
        /// Optimize canvas sorting orders
        /// </summary>
        private void OptimizeCanvasSorting()
        {
            if (_mainCamera == null) return;

            var cameraPos = _mainCamera.transform.position;
            var visibleCanvases = new List<(Canvas canvas, float distance)>();

            // Collect visible canvases with distances
            foreach (var canvas in _managedCanvases)
            {
                if (canvas == null || !canvas.enabled) continue;

                float distance = Vector3.Distance(cameraPos, canvas.transform.position);
                visibleCanvases.Add((canvas, distance));
            }

            // Sort by distance (closest first gets higher sorting order)
            visibleCanvases.Sort((a, b) => a.distance.CompareTo(b.distance));

            // Update sorting orders
            for (int i = 0; i < visibleCanvases.Count; i++)
            {
                var canvas = visibleCanvases[i].canvas;
                int newSortingOrder = i + 1; // Start from 1

                if (canvas.sortingOrder != newSortingOrder)
                {
                    canvas.sortingOrder = newSortingOrder;
                    _stats.SortingOrderUpdates++;
                }
            }
        }

        /// <summary>
        /// Validate and clean up managed canvases
        /// </summary>
        private int ValidateManagedCanvases()
        {
            var invalidCanvases = new List<Canvas>();

            foreach (var canvas in _managedCanvases)
            {
                if (canvas == null || canvas.gameObject == null)
                {
                    invalidCanvases.Add(canvas);
                }
            }

            foreach (var invalidCanvas in invalidCanvases)
            {
                _managedCanvases.Remove(invalidCanvas);
                _canvasData.Remove(invalidCanvas);
            }

            if (invalidCanvases.Count > 0)
            {
                _stats.InvalidCanvasesRemoved += invalidCanvases.Count;

                if (_enableLogging)
                {
                    ChimeraLogger.Log("UI", $"Cleaned up {invalidCanvases.Count} invalid canvases", this);
                }
            }

            return invalidCanvases.Count;
        }

        /// <summary>
        /// Set canvas culling distance
        /// </summary>
        public void SetCullDistance(float distance)
        {
            _canvasCullDistance = Mathf.Max(0f, distance);

            if (_enableLogging)
            {
                ChimeraLogger.Log("UI", $"Canvas cull distance set to {_canvasCullDistance}", this);
            }
        }

        /// <summary>
        /// Enable or disable canvas culling
        /// </summary>
        public void SetCanvasCullingEnabled(bool enabled)
        {
            _enableCanvasCulling = enabled;

            if (_enableLogging)
            {
                ChimeraLogger.Log("UI", $"Canvas culling {(enabled ? "enabled" : "disabled")}", this);
            }
        }

        /// <summary>
        /// Get canvas optimization data
        /// </summary>
        public CanvasOptimizationData? GetCanvasData(Canvas canvas)
        {
            if (_canvasData.TryGetValue(canvas, out var data))
            {
                return data;
            }
            return null;
        }

        /// <summary>
        /// Get all managed canvases
        /// </summary>
        public List<Canvas> GetManagedCanvases()
        {
            return new List<Canvas>(_managedCanvases);
        }

        /// <summary>
        /// Clear all managed canvases
        /// </summary>
        public void ClearAllCanvases()
        {
            _managedCanvases.Clear();
            _canvasData.Clear();

            if (_enableLogging)
            {
                ChimeraLogger.Log("UI", "All managed canvases cleared", this);
            }
        }

        /// <summary>
        /// Reset statistics
        /// </summary>
        private void ResetStats()
        {
            _stats = new UICanvasOptimizerStats
            {
                TotalCanvasesRegistered = 0,
                CanvasCullOperations = 0,
                SortingOrderUpdates = 0,
                OptimizationCycles = 0,
                InvalidCanvasesRemoved = 0,
                LastUpdateTime = Time.time
            };
        }

        private void OnDestroy()
        {
            ClearAllCanvases();
        }
    }

    /// <summary>
    /// Canvas optimization data structure
    /// </summary>
    [System.Serializable]
    public struct CanvasOptimizationData
    {
        public int OriginalSortingOrder;
        public float LastVisibilityCheck;
        public bool IsVisible;
        public float LastDistance;
    }

    /// <summary>
    /// Canvas optimizer statistics
    /// </summary>
    [System.Serializable]
    public struct UICanvasOptimizerStats
    {
        public int TotalCanvasesRegistered;
        public int CanvasCullOperations;
        public int SortingOrderUpdates;
        public int OptimizationCycles;
        public int InvalidCanvasesRemoved;
        public float LastUpdateTime;
    }
}
