using UnityEngine;
using UnityEngine.UI;
using ProjectChimera.Systems.UI.Pooling;
using ProjectChimera.Systems.UI;
// using ProjectChimera.Systems.UI.Components; // Removed - using fully qualified names instead

namespace ProjectChimera.Systems.UI.Core
{
    /// <summary>
    /// REFACTORED: Focused UI Element Creation
    /// Handles only UI element instantiation, pooling, and factory concerns
    /// </summary>
    public class UIElementFactory : MonoBehaviour
    {
        [Header("Element Factory Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enablePooling = true;
        [SerializeField] private int _initialPoolSize = 50;
        [SerializeField] private int _maxPoolSize = 200;

        [Header("Element Prefabs")]
        [SerializeField] private GameObject _plantInfoPanelPrefab;
        [SerializeField] private GameObject _progressBarPrefab;
        [SerializeField] private GameObject _notificationPrefab;

        // Pooling system
        private UIElementPool _elementPool;

        // Events
        public System.Action<string, GameObject> OnElementCreated;
        public System.Action<string> OnElementDestroyed;

        private void Start()
        {
            InitializePooling();
        }

        /// <summary>
        /// Create plant info panel
        /// </summary>
        public PlantInfoPanel CreatePlantInfoPanel(string plantId, Vector3 worldPosition)
        {
            if (string.IsNullOrEmpty(plantId))
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("UI", "Cannot create plant info panel - invalid plant ID", this);
                return null;
            }

            GameObject panelObject;
            if (_enablePooling && _elementPool != null)
            {
                panelObject = _elementPool.GetPooledElement("PlantInfoPanel");
                if (panelObject == null)
                {
                    panelObject = CreateNewPlantInfoPanel();
                }
            }
            else
            {
                panelObject = CreateNewPlantInfoPanel();
            }

            if (panelObject == null)
            {
                if (_enableLogging)
                    ChimeraLogger.LogError("UI", $"Failed to create plant info panel for plant: {plantId}", this);
                return null;
            }

            var panel = panelObject.GetComponent<PlantInfoPanel>();
            if (panel != null)
            {
                panel.Initialize(plantId, worldPosition);
                OnElementCreated?.Invoke(plantId, panelObject);

                if (_enableLogging)
                    ChimeraLogger.Log("UI", $"✅ Created plant info panel: {plantId}", this);
            }

            return panel;
        }

        /// <summary>
        /// Create progress bar
        /// </summary>
        public ProgressBar CreateProgressBar(string operationId, string title, float duration)
        {
            if (string.IsNullOrEmpty(operationId))
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("UI", "Cannot create progress bar - invalid operation ID", this);
                return null;
            }

            GameObject progressObject;
            if (_enablePooling && _elementPool != null)
            {
                progressObject = _elementPool.GetPooledElement("ProgressBar");
                if (progressObject == null)
                {
                    progressObject = CreateNewProgressBar();
                }
            }
            else
            {
                progressObject = CreateNewProgressBar();
            }

            if (progressObject == null)
            {
                if (_enableLogging)
                    ChimeraLogger.LogError("UI", $"Failed to create progress bar for operation: {operationId}", this);
                return null;
            }

            var progressBar = progressObject.GetComponent<ProgressBar>();
            if (progressBar != null)
            {
                progressBar.Initialize(operationId, title, duration);
                OnElementCreated?.Invoke(operationId, progressObject);

                if (_enableLogging)
                    ChimeraLogger.Log("UI", $"✅ Created progress bar: {operationId}", this);
            }

            return progressBar;
        }

        /// <summary>
        /// Create notification
        /// </summary>
        public ProjectChimera.Systems.UI.NotificationDisplay CreateNotification(string message, ProjectChimera.Systems.UI.Components.NotificationType type, float duration)
        {
            if (string.IsNullOrEmpty(message))
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("UI", "Cannot create notification - empty message", this);
                return null;
            }

            GameObject notificationObject;
            if (_enablePooling && _elementPool != null)
            {
                notificationObject = _elementPool.GetPooledElement("Notification");
                if (notificationObject == null)
                {
                    notificationObject = CreateNewNotification();
                }
            }
            else
            {
                notificationObject = CreateNewNotification();
            }

            if (notificationObject == null)
            {
                if (_enableLogging)
                    ChimeraLogger.LogError("UI", "Failed to create notification", this);
                return null;
            }

            var notification = notificationObject.GetComponent<ProjectChimera.Systems.UI.NotificationDisplay>();
            if (notification != null)
            {
                notification.ShowNotification(message, type, duration);
                OnElementCreated?.Invoke($"Notification_{Time.time}", notificationObject);

                if (_enableLogging)
                    ChimeraLogger.Log("UI", $"✅ Created notification: {type}", this);
            }

            return notification;
        }

        /// <summary>
        /// Return element to pool or destroy it
        /// </summary>
        public void ReturnElement(GameObject element, string elementType)
        {
            if (element == null) return;

            if (_enablePooling && _elementPool != null)
            {
                _elementPool.ReturnToPool(element, elementType);
            }
            else
            {
                Destroy(element);
            }

            OnElementDestroyed?.Invoke(elementType);
        }

        /// <summary>
        /// Get pooled element count for debugging
        /// </summary>
        public int GetPooledElementCount()
        {
            return _elementPool?.GetTotalPooledCount() ?? 0;
        }

        private void InitializePooling()
        {
            if (!_enablePooling) return;

            var poolObject = new GameObject("UIElementPool");
            poolObject.transform.SetParent(transform);
            _elementPool = poolObject.AddComponent<UIElementPool>();

            // Initialize pools for different element types
            _elementPool.InitializePool("PlantInfoPanel", _plantInfoPanelPrefab, _initialPoolSize);
            _elementPool.InitializePool("ProgressBar", _progressBarPrefab, _initialPoolSize);
            _elementPool.InitializePool("Notification", _notificationPrefab, _initialPoolSize);

            if (_enableLogging)
                ChimeraLogger.Log("UI", "✅ UI element pooling initialized", this);
        }

        private GameObject CreateNewPlantInfoPanel()
        {
            if (_plantInfoPanelPrefab == null)
            {
                if (_enableLogging)
                    ChimeraLogger.LogError("UI", "PlantInfoPanel prefab not assigned", this);
                return null;
            }

            return Instantiate(_plantInfoPanelPrefab);
        }

        private GameObject CreateNewProgressBar()
        {
            if (_progressBarPrefab == null)
            {
                if (_enableLogging)
                    ChimeraLogger.LogError("UI", "ProgressBar prefab not assigned", this);
                return null;
            }

            return Instantiate(_progressBarPrefab);
        }

        private GameObject CreateNewNotification()
        {
            if (_notificationPrefab == null)
            {
                if (_enableLogging)
                    ChimeraLogger.LogError("UI", "Notification prefab not assigned", this);
                return null;
            }

            return Instantiate(_notificationPrefab);
        }
    }

    // NOTE: NotificationType moved to centralized definitions. Use UINotificationManager's NotificationType.
}
