using UnityEngine;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Base class for all Project Chimera MonoBehaviours.
    /// Provides common functionality, lifecycle management, and enforces coding standards.
    /// </summary>
    public abstract class ChimeraMonoBehaviour : MonoBehaviour
    {
        [Header("Chimera Base Properties")]
        [SerializeField] private string _componentID;
        [SerializeField] private bool _enableDebugLogging = false;

        /// <summary>
        /// Unique identifier for this component instance.
        /// Generated automatically if not set.
        /// </summary>
        public string ComponentID 
        { 
            get 
            { 
                if (string.IsNullOrEmpty(_componentID))
                {
                    _componentID = System.Guid.NewGuid().ToString();
                }
                return _componentID; 
            } 
        }

        /// <summary>
        /// Whether debug logging is enabled for this component.
        /// </summary>
        public bool EnableDebugLogging => _enableDebugLogging;

        /// <summary>
        /// Called when the component is first created.
        /// Override for component-specific initialization that doesn't depend on other objects.
        /// </summary>
        protected virtual void Awake()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Called before the first frame update, after all Awake calls.
        /// Override for initialization that may depend on other objects being initialized.
        /// </summary>
        protected virtual void Start()
        {
            // Base implementation - override in derived classes
        }

        /// <summary>
        /// Called when the component becomes enabled and active.
        /// Override for event subscription and resource allocation.
        /// </summary>
        protected virtual void OnEnable()
        {
            // Base implementation - override in derived classes
        }

        /// <summary>
        /// Called when the component becomes disabled.
        /// Override for event unsubscription and resource cleanup.
        /// </summary>
        protected virtual void OnDisable()
        {
            // Base implementation - override in derived classes
        }

        /// <summary>
        /// Called when the component is destroyed.
        /// Override for final cleanup operations.
        /// </summary>
        protected virtual void OnDestroy()
        {
            CleanupComponent();
        }

        /// <summary>
        /// Initialize component-specific data and cache references.
        /// Called from Awake().
        /// </summary>
        protected virtual void InitializeComponent()
        {
            if (_enableDebugLogging)
            {
                LogDebug($"Initializing {GetType().Name} component");
            }
        }

        /// <summary>
        /// Cleanup component resources and references.
        /// Called from OnDestroy().
        /// </summary>
        protected virtual void CleanupComponent()
        {
            if (_enableDebugLogging)
            {
                LogDebug($"Cleaning up {GetType().Name} component");
            }
        }

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The message to log</param>
        protected void LogInfo(string message)
        {
            Debug.Log($"[Chimera][{GetType().Name}] {message}", this);
        }

        /// <summary>
        /// Logs a debug message if debug logging is enabled.
        /// </summary>
        /// <param name="message">The message to log</param>
        protected void LogDebug(string message)
        {
            if (_enableDebugLogging)
            {
                Debug.Log($"[Chimera][{GetType().Name}] {message}", this);
            }
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The warning message to log</param>
        protected void LogWarning(string message)
        {
            Debug.LogWarning($"[Chimera][{GetType().Name}] {message}", this);
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The error message to log</param>
        protected void LogError(string message)
        {
            Debug.LogError($"[Chimera][{GetType().Name}] {message}", this);
        }

        /// <summary>
        /// Returns a formatted string representation of this component.
        /// </summary>
        public override string ToString()
        {
            return $"{GetType().Name} on {gameObject.name} (ID: {ComponentID})";
        }
    }
}