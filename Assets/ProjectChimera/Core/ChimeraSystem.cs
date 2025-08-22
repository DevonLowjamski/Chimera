using UnityEngine;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Base class for all Project Chimera system components.
    /// Systems handle specific gameplay functionality and can be enabled/disabled dynamically.
    /// </summary>
    public abstract class ChimeraSystem : ChimeraMonoBehaviour
    {
        [Header("System Properties")]
        [SerializeField] private bool _autoStart = true;
        [SerializeField] private int _systemPriority = 100;
        [SerializeField] private float _updateInterval = 0.0f; // 0 = every frame

        private float _lastUpdateTime;

        /// <summary>
        /// Whether this system is currently running.
        /// </summary>
        public bool IsSystemRunning { get; private set; }

        /// <summary>
        /// Priority of this system for update ordering (lower values = higher priority).
        /// </summary>
        public int SystemPriority => _systemPriority;

        /// <summary>
        /// Update interval in seconds (0 = every frame).
        /// </summary>
        public float UpdateInterval => _updateInterval;

        protected override void Start()
        {
            base.Start();

            if (_autoStart)
            {
                StartSystem();
            }
        }

        /// <summary>
        /// Starts the system and begins processing.
        /// </summary>
        public virtual void StartSystem()
        {
            if (IsSystemRunning)
            {
                LogWarning("System is already running");
                return;
            }

            LogDebug("Starting system");
            
            OnSystemStart();
            IsSystemRunning = true;
            _lastUpdateTime = Time.time;
            
            LogDebug("System started successfully");
        }

        /// <summary>
        /// Stops the system and halts processing.
        /// </summary>
        public virtual void StopSystem()
        {
            if (!IsSystemRunning)
            {
                LogWarning("System is not running");
                return;
            }

            LogDebug("Stopping system");
            
            OnSystemStop();
            IsSystemRunning = false;
            
            LogDebug("System stopped successfully");
        }

        /// <summary>
        /// Pauses the system without fully stopping it.
        /// </summary>
        public virtual void PauseSystem()
        {
            if (!IsSystemRunning)
            {
                LogWarning("Cannot pause system that is not running");
                return;
            }

            LogDebug("Pausing system");
            OnSystemPause();
        }

        /// <summary>
        /// Resumes a paused system.
        /// </summary>
        public virtual void ResumeSystem()
        {
            if (!IsSystemRunning)
            {
                LogWarning("Cannot resume system that is not running");
                return;
            }

            LogDebug("Resuming system");
            OnSystemResume();
            _lastUpdateTime = Time.time;
        }

        /// <summary>
        /// Override this method to implement system-specific startup logic.
        /// </summary>
        protected abstract void OnSystemStart();

        /// <summary>
        /// Override this method to implement system-specific shutdown logic.
        /// </summary>
        protected abstract void OnSystemStop();

        /// <summary>
        /// Override this method to implement system-specific pause logic.
        /// </summary>
        protected virtual void OnSystemPause()
        {
            // Base implementation - override in derived classes if needed
        }

        /// <summary>
        /// Override this method to implement system-specific resume logic.
        /// </summary>
        protected virtual void OnSystemResume()
        {
            // Base implementation - override in derived classes if needed
        }

        /// <summary>
        /// Main system update logic. Respects update interval settings.
        /// </summary>
        protected virtual void Update()
        {
            if (!IsSystemRunning) return;

            // Check if enough time has passed for the next update
            if (_updateInterval > 0.0f)
            {
                if (Time.time - _lastUpdateTime < _updateInterval)
                    return;
                
                _lastUpdateTime = Time.time;
            }

            OnSystemUpdate();
        }

        /// <summary>
        /// Override this method to implement system-specific update logic.
        /// Only called when the system is running and respects update intervals.
        /// </summary>
        protected virtual void OnSystemUpdate()
        {
            // Base implementation - override in derived classes
        }

        protected override void OnDestroy()
        {
            if (IsSystemRunning)
            {
                StopSystem();
            }
            base.OnDestroy();
        }

        protected override void OnDisable()
        {
            if (IsSystemRunning)
            {
                PauseSystem();
            }
            base.OnDisable();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (IsSystemRunning)
            {
                ResumeSystem();
            }
        }
    }
}