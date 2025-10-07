using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
using ProjectChimera.Systems.UI;

namespace ProjectChimera.Systems.UI.Core
{
    /// <summary>
    /// REFACTORED: UI Progress Bar Manager
    /// Single Responsibility: Progress bar creation, tracking, and lifecycle management
    /// Extracted from OptimizedUIManager for better separation of concerns
    /// </summary>
    public class UIProgressBarManager : MonoBehaviour
    {
        [Header("Progress Bar Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private int _maxConcurrentProgressBars = 10;
        [SerializeField] private Transform _progressBarParent;

        // Active progress bars
        private readonly List<ProgressBar> _activeProgressBars = new List<ProgressBar>();
        private readonly System.Collections.Generic.Dictionary<string, ProgressBar> _progressBarLookup = new System.Collections.Generic.Dictionary<string, ProgressBar>();

        // Pooling support
        private UIElementPoolManager _poolManager;
        private bool _isInitialized = false;

        // Statistics
        private UIProgressBarStats _stats = new UIProgressBarStats();

        // Events
        public event System.Action<string> OnProgressBarCreated;
        public event System.Action<string> OnProgressBarCompleted;
        public event System.Action<string> OnProgressBarRemoved;

        public bool IsInitialized => _isInitialized;
        public UIProgressBarStats Stats => _stats;
        public int ActiveCount => _activeProgressBars.Count;

        public void Initialize(UIElementPoolManager poolManager = null)
        {
            if (_isInitialized) return;

            _poolManager = poolManager;
            _activeProgressBars.Clear();
            _progressBarLookup.Clear();
            ResetStats();

            // Setup progress bar parent if not assigned
            if (_progressBarParent == null)
            {
                var progressCanvas = GameObject.Find("ProgressCanvas");
                if (progressCanvas != null)
                {
                    _progressBarParent = progressCanvas.transform;
                }
            }

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("UI", "UI Progress Bar Manager initialized", this);
            }
        }

        /// <summary>
        /// Create a new progress bar
        /// </summary>
        public ProgressBar CreateProgressBar(string operationId, string title, float duration = 0f)
        {
            if (string.IsNullOrEmpty(operationId))
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("UI", "Cannot create progress bar: operationId is null or empty", this);
                }
                return null;
            }

            // Check if already exists
            if (_progressBarLookup.ContainsKey(operationId))
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("UI", $"Progress bar {operationId} already exists", this);
                }
                return _progressBarLookup[operationId];
            }

            // Check capacity
            if (_activeProgressBars.Count >= _maxConcurrentProgressBars)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("UI", $"Maximum concurrent progress bars ({_maxConcurrentProgressBars}) reached", this);
                }
                return null;
            }

            GameObject progressBarGO = null;

            // Try to get from pool first
            if (_poolManager != null)
            {
                var pooledElement = _poolManager.GetPooledElement<RectTransform>();
                if (pooledElement != null)
                {
                    progressBarGO = pooledElement.gameObject;
                }
            }

            // Create new if pooling failed
            if (progressBarGO == null)
            {
                progressBarGO = CreateProgressBarGameObject(operationId);
            }

            var progressBar = progressBarGO.GetComponent<ProgressBar>();
            if (progressBar == null)
            {
                progressBar = progressBarGO.AddComponent<ProgressBar>();
            }

            // Initialize the progress bar
            progressBar.Initialize(operationId, title, duration);
            progressBar.OnProgressCompleted += HandleProgressBarCompleted;

            // Register the progress bar
            _activeProgressBars.Add(progressBar);
            _progressBarLookup[operationId] = progressBar;

            // Update statistics
            _stats.TotalCreated++;
            _stats.CurrentlyActive++;

            // Notify listeners
            OnProgressBarCreated?.Invoke(operationId);

            if (_enableLogging)
            {
                ChimeraLogger.Log("UI", $"Created progress bar: {operationId} - {title} ({_activeProgressBars.Count} active)", this);
            }

            return progressBar;
        }

        /// <summary>
        /// Update progress bar value
        /// </summary>
        public bool UpdateProgressBar(string operationId, float progress)
        {
            if (_progressBarLookup.TryGetValue(operationId, out var progressBar))
            {
                progressBar.UpdateProgress(progress);
                return true;
            }

            if (_enableLogging)
            {
                ChimeraLogger.LogWarning("UI", $"Progress bar {operationId} not found for update", this);
            }
            return false;
        }

        /// <summary>
        /// Remove specific progress bar
        /// </summary>
        public bool RemoveProgressBar(string operationId)
        {
            if (!_progressBarLookup.TryGetValue(operationId, out var progressBar))
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("UI", $"Progress bar {operationId} not found for removal", this);
                }
                return false;
            }

            // Unregister events
            progressBar.OnProgressCompleted -= HandleProgressBarCompleted;

            // Remove from tracking
            _activeProgressBars.Remove(progressBar);
            _progressBarLookup.Remove(operationId);

            // Update statistics
            _stats.CurrentlyActive--;

            // Return to pool or destroy
            if (_poolManager != null)
            {
                _poolManager.ReturnPooledElement(progressBar.GetComponent<RectTransform>());
            }
            else
            {
                Destroy(progressBar.gameObject);
            }

            // Notify listeners
            OnProgressBarRemoved?.Invoke(operationId);

            if (_enableLogging)
            {
                ChimeraLogger.Log("UI", $"Removed progress bar: {operationId} ({_activeProgressBars.Count} remaining active)", this);
            }

            return true;
        }

        /// <summary>
        /// Get progress bar by operation ID
        /// </summary>
        public ProgressBar GetProgressBar(string operationId)
        {
            _progressBarLookup.TryGetValue(operationId, out var progressBar);
            return progressBar;
        }

        /// <summary>
        /// Check if progress bar exists
        /// </summary>
        public bool HasProgressBar(string operationId)
        {
            return _progressBarLookup.ContainsKey(operationId);
        }

        /// <summary>
        /// Update all active progress bars
        /// </summary>
        public void UpdateAllProgressBars(float deltaTime)
        {
            var progressBarsToUpdate = new List<ProgressBar>(_activeProgressBars);

            foreach (var progressBar in progressBarsToUpdate)
            {
                if (progressBar != null && progressBar.gameObject.activeInHierarchy)
                {
                    progressBar.UpdateUI(deltaTime);
                }
            }

            _stats.LastUpdateTime = Time.time;
        }

        /// <summary>
        /// Remove completed progress bars
        /// </summary>
        public int CleanupCompletedProgressBars()
        {
            var completedBars = _activeProgressBars.Where(pb => pb != null && pb.IsCompleted).ToList();

            foreach (var progressBar in completedBars)
            {
                RemoveProgressBar(progressBar.OperationId);
            }

            return completedBars.Count;
        }

        /// <summary>
        /// Remove all progress bars
        /// </summary>
        public void ClearAllProgressBars()
        {
            var activeIds = new List<string>(_progressBarLookup.Keys);

            foreach (var operationId in activeIds)
            {
                RemoveProgressBar(operationId);
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("UI", "All progress bars cleared", this);
            }
        }

        /// <summary>
        /// Create progress bar GameObject
        /// </summary>
        private GameObject CreateProgressBarGameObject(string operationId)
        {
            var progressBarGO = new GameObject($"ProgressBar_{operationId}");
            var rectTransform = progressBarGO.AddComponent<RectTransform>();

            // Set basic properties
            if (_progressBarParent != null)
            {
                rectTransform.SetParent(_progressBarParent);
            }

            return progressBarGO;
        }

        /// <summary>
        /// Handle progress bar completion
        /// </summary>
        private void HandleProgressBarCompleted(string operationId)
        {
            _stats.TotalCompleted++;
            OnProgressBarCompleted?.Invoke(operationId);

            if (_enableLogging)
            {
                ChimeraLogger.Log("UI", $"Progress bar completed: {operationId}", this);
            }
        }

        /// <summary>
        /// Reset statistics
        /// </summary>
        private void ResetStats()
        {
            _stats = new UIProgressBarStats
            {
                TotalCreated = 0,
                TotalCompleted = 0,
                CurrentlyActive = 0,
                LastUpdateTime = Time.time
            };
        }

        private void OnDestroy()
        {
            ClearAllProgressBars();
        }
    }

    /// <summary>
    /// Progress bar statistics
    /// </summary>
    [System.Serializable]
    public struct UIProgressBarStats
    {
        public int TotalCreated;
        public int TotalCompleted;
        public int CurrentlyActive;
        public float LastUpdateTime;
    }

    // ProgressBar component now lives in ProjectChimera.Systems.UI.Components.ProgressBar
}
