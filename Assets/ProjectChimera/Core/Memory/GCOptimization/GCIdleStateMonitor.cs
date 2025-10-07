using UnityEngine;
using System.Collections;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Memory.GCOptimization
{
    /// <summary>
    /// REFACTORED: GC Idle State Monitor - Focused idle detection and application state monitoring
    /// Handles idle state detection, scene transition tracking, and application focus/pause events
    /// Single Responsibility: Application idle state monitoring and lifecycle tracking
    /// </summary>
    public class GCIdleStateMonitor : MonoBehaviour
    {
        [Header("Idle Detection Settings")]
        [SerializeField] private bool _enableIdleDetection = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _idleTimeThreshold = 2f; // Time before considering idle
        [SerializeField] private float _inputThreshold = 0.01f; // Minimum input to consider active

        [Header("Frame Time Settings")]
        [SerializeField] private float _stableFrameTimeThreshold = 0.02f; // 50fps or better
        [SerializeField] private int _stableFrameCount = 30; // Frames to consider stable

        // State tracking
        private bool _isIdle = false;
        private bool _isSceneTransitioning = false;
        private bool _hasApplicationFocus = true;
        private bool _isApplicationPaused = false;
        private float _lastInputTime;
        private int _stableFrames = 0;

        // Coroutines
        private Coroutine _idleDetectionCoroutine;

        // Statistics
        private IdleStateStats _stats = new IdleStateStats();

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public bool IsIdle => _isIdle;
        public bool IsSceneTransitioning => _isSceneTransitioning;
        public bool HasApplicationFocus => _hasApplicationFocus;
        public bool IsApplicationPaused => _isApplicationPaused;
        public IdleStateStats GetStats() => _stats;

        // Events
        public System.Action OnIdleStateEntered;
        public System.Action OnIdleStateExited;
        public System.Action OnSceneTransitionStarted;
        public System.Action OnSceneTransitionEnded;
        public System.Action OnApplicationFocusGained;
        public System.Action OnApplicationFocusLost;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _lastInputTime = Time.realtimeSinceStartup;
            _hasApplicationFocus = Application.isFocused;
            _isApplicationPaused = false;

            if (_enableLogging)
                ChimeraLogger.Log("GC", "🎮 GCIdleStateMonitor initialized", this);
        }

        /// <summary>
        /// Update idle state monitoring (called from coordinator)
        /// </summary>
        public void UpdateIdleMonitoring()
        {
            if (!IsEnabled || !_enableIdleDetection) return;

            CheckInputActivity();
            CheckFrameStability();
            UpdateIdleState();
            UpdateStatistics();
        }

        /// <summary>
        /// Manually set idle state
        /// </summary>
        public void SetIdleState(bool idle, string reason = "Manual")
        {
            if (idle && !_isIdle)
            {
                EnterIdleState(reason);
            }
            else if (!idle && _isIdle)
            {
                ExitIdleState(reason);
            }
        }

        /// <summary>
        /// Notify scene transition start
        /// </summary>
        public void NotifySceneTransitionStart()
        {
            _isSceneTransitioning = true;
            _stats.SceneTransitions++;

            OnSceneTransitionStarted?.Invoke();

            if (_enableLogging)
                ChimeraLogger.Log("GC", "Scene transition started", this);
        }

        /// <summary>
        /// Notify scene transition end
        /// </summary>
        public void NotifySceneTransitionEnd()
        {
            _isSceneTransitioning = false;

            OnSceneTransitionEnded?.Invoke();

            if (_enableLogging)
                ChimeraLogger.Log("GC", "Scene transition ended", this);
        }

        /// <summary>
        /// Get idle state information
        /// </summary>
        public IdleStateInfo GetIdleStateInfo()
        {
            return new IdleStateInfo
            {
                IsIdle = _isIdle,
                IsSceneTransitioning = _isSceneTransitioning,
                HasApplicationFocus = _hasApplicationFocus,
                IsApplicationPaused = _isApplicationPaused,
                TimeSinceLastInput = Time.realtimeSinceStartup - _lastInputTime,
                StableFrameCount = _stableFrames,
                IdleTimeThreshold = _idleTimeThreshold
            };
        }

        /// <summary>
        /// Force idle detection check
        /// </summary>
        public void ForceIdleCheck()
        {
            if (!IsEnabled) return;

            CheckInputActivity();
            UpdateIdleState();

            if (_enableLogging)
                ChimeraLogger.Log("GC", "Forced idle state check", this);
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                if (_isIdle)
                {
                    ExitIdleState("System disabled");
                }

                if (_idleDetectionCoroutine != null)
                {
                    StopCoroutine(_idleDetectionCoroutine);
                    _idleDetectionCoroutine = null;
                }
            }

            if (_enableLogging)
                ChimeraLogger.Log("GC", $"GCIdleStateMonitor: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Check for input activity
        /// </summary>
        private void CheckInputActivity()
        {
            bool hasInput = UnityEngine.Input.anyKey || UnityEngine.Input.anyKeyDown ||
                           Mathf.Abs(UnityEngine.Input.GetAxis("Mouse X")) > _inputThreshold ||
                           Mathf.Abs(UnityEngine.Input.GetAxis("Mouse Y")) > _inputThreshold ||
                           UnityEngine.Input.touchCount > 0;

            if (hasInput)
            {
                _lastInputTime = Time.realtimeSinceStartup;

                if (_isIdle)
                {
                    ExitIdleState("Input detected");
                }
            }
        }

        /// <summary>
        /// Check frame stability
        /// </summary>
        private void CheckFrameStability()
        {
            if (Time.unscaledDeltaTime < _stableFrameTimeThreshold)
            {
                _stableFrames = Mathf.Min(_stableFrames + 1, _stableFrameCount + 10);
            }
            else
            {
                _stableFrames = 0;
            }
        }

        /// <summary>
        /// Update idle state based on conditions
        /// </summary>
        private void UpdateIdleState()
        {
            var timeSinceInput = Time.realtimeSinceStartup - _lastInputTime;
            var shouldBeIdle = timeSinceInput >= _idleTimeThreshold &&
                              _stableFrames >= _stableFrameCount &&
                              _hasApplicationFocus &&
                              !_isApplicationPaused &&
                              !_isSceneTransitioning;

            if (shouldBeIdle && !_isIdle)
            {
                if (_idleDetectionCoroutine == null)
                {
                    _idleDetectionCoroutine = StartCoroutine(IdleDetectionCoroutine());
                }
            }
            else if (!shouldBeIdle && _isIdle)
            {
                ExitIdleState("Conditions no longer met");
            }
        }

        /// <summary>
        /// Enter idle state
        /// </summary>
        private void EnterIdleState(string reason)
        {
            if (_isIdle) return;

            _isIdle = true;
            _stats.IdleSessionsStarted++;
            _stats.LastIdleStartTime = Time.realtimeSinceStartup;

            OnIdleStateEntered?.Invoke();

            if (_enableLogging)
                ChimeraLogger.Log("GC", $"Entered idle state: {reason}", this);
        }

        /// <summary>
        /// Exit idle state
        /// </summary>
        private void ExitIdleState(string reason)
        {
            if (!_isIdle) return;

            var idleTime = Time.realtimeSinceStartup - _stats.LastIdleStartTime;
            _stats.TotalIdleTime += idleTime;

            _isIdle = false;

            if (_idleDetectionCoroutine != null)
            {
                StopCoroutine(_idleDetectionCoroutine);
                _idleDetectionCoroutine = null;
            }

            OnIdleStateExited?.Invoke();

            if (_enableLogging)
                ChimeraLogger.Log("GC", $"Exited idle state: {reason} (duration: {idleTime:F2}s)", this);
        }

        /// <summary>
        /// Update monitoring statistics
        /// </summary>
        private void UpdateStatistics()
        {
            _stats.CurrentFrameTime = Time.unscaledDeltaTime;
            _stats.TimeSinceLastInput = Time.realtimeSinceStartup - _lastInputTime;
            _stats.StableFrameCount = _stableFrames;

            if (_stats.TimeSinceLastInput > _stats.MaxTimeBetweenInputs)
            {
                _stats.MaxTimeBetweenInputs = _stats.TimeSinceLastInput;
            }
        }

        /// <summary>
        /// Coroutine for idle detection with delay
        /// </summary>
        private IEnumerator IdleDetectionCoroutine()
        {
            yield return new WaitForSecondsRealtime(0.5f); // Additional delay for confirmation

            // Double-check conditions
            var timeSinceInput = Time.realtimeSinceStartup - _lastInputTime;
            if (timeSinceInput >= _idleTimeThreshold && _stableFrames >= _stableFrameCount)
            {
                EnterIdleState("Idle detection confirmed");
            }

            _idleDetectionCoroutine = null;
        }

        #endregion

        #region Unity Lifecycle Events

        private void OnApplicationFocus(bool hasFocus)
        {
            _hasApplicationFocus = hasFocus;

            if (hasFocus)
            {
                OnApplicationFocusGained?.Invoke();

                if (_isIdle)
                {
                    ExitIdleState("Application focus gained");
                }

                if (_enableLogging)
                    ChimeraLogger.Log("GC", "Application focus gained", this);
            }
            else
            {
                OnApplicationFocusLost?.Invoke();

                if (_enableLogging)
                    ChimeraLogger.Log("GC", "Application focus lost", this);
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            _isApplicationPaused = pauseStatus;

            if (pauseStatus)
            {
                if (!_isIdle)
                {
                    EnterIdleState("Application paused");
                }

                if (_enableLogging)
                    ChimeraLogger.Log("GC", "Application paused", this);
            }
            else
            {
                if (_isIdle)
                {
                    ExitIdleState("Application resumed");
                }

                if (_enableLogging)
                    ChimeraLogger.Log("GC", "Application resumed", this);
            }
        }

        private void OnDestroy()
        {
            if (_idleDetectionCoroutine != null)
            {
                StopCoroutine(_idleDetectionCoroutine);
            }
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Idle state information
    /// </summary>
    [System.Serializable]
    public struct IdleStateInfo
    {
        public bool IsIdle;
        public bool IsSceneTransitioning;
        public bool HasApplicationFocus;
        public bool IsApplicationPaused;
        public float TimeSinceLastInput;
        public int StableFrameCount;
        public float IdleTimeThreshold;
    }

    /// <summary>
    /// Idle state monitoring statistics
    /// </summary>
    [System.Serializable]
    public struct IdleStateStats
    {
        public int IdleSessionsStarted;
        public float TotalIdleTime;
        public float LastIdleStartTime;
        public int SceneTransitions;
        public float CurrentFrameTime;
        public float TimeSinceLastInput;
        public int StableFrameCount;
        public float MaxTimeBetweenInputs;
    }

    #endregion
}