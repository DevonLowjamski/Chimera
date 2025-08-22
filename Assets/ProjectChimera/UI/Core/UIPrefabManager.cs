using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core;

namespace ProjectChimera.UI.Core
{
    /// <summary>
    /// Central manager for UI prefab creation, pooling, and lifecycle management in Project Chimera.
    /// Handles instantiation of UI components, widgets, modals, and notifications.
    /// </summary>
    public class UIPrefabManager : ChimeraManager
    {
        [Header("Prefab Configuration")]
        [SerializeField] private UIPrefabLibrarySO _prefabLibrary;
        [SerializeField] private int _initialPoolSize = 10;
        [SerializeField] private int _maxPoolSize = 50;
        [SerializeField] private bool _enablePooling = true;
        [SerializeField] private bool _preloadPrefabs = true;
        
        [Header("Container References")]
        [SerializeField] private Transform _componentContainer;
        [SerializeField] private Transform _widgetContainer;
        [SerializeField] private Transform _modalContainer;
        [SerializeField] private Transform _notificationContainer;
        
        // Pooling system
        private Dictionary<string, Queue<UIComponentPrefab>> _componentPools;
        private Dictionary<string, List<UIComponentPrefab>> _activeComponents;
        private Dictionary<string, UIComponentPrefab> _prefabTemplates;
        
        // Active instances tracking
        private List<UIModalPrefab> _activeModals;
        private List<UINotificationPrefab> _activeNotifications;
        private Dictionary<string, UIWidgetPrefab> _activeWidgets;
        
        // Events
        public System.Action<UIComponentPrefab> OnComponentCreated;
        public System.Action<UIComponentPrefab> OnComponentDestroyed;
        public System.Action<UIModalPrefab> OnModalOpened;
        public System.Action<UIModalPrefab> OnModalClosed;
        public System.Action<UINotificationPrefab> OnNotificationShown;
        public System.Action<UINotificationPrefab> OnNotificationDismissed;
        
        // Properties
        public UIPrefabLibrarySO PrefabLibrary => _prefabLibrary;
        public bool EnablePooling => _enablePooling;
        public int ActiveComponentCount => _activeComponents.Values.Sum(list => list.Count);
        public int ActiveModalCount => _activeModals.Count;
        public int ActiveNotificationCount => _activeNotifications.Count;
        public int ActiveWidgetCount => _activeWidgets.Count;
        
        protected override void OnManagerInitialize()
        {
            
            InitializePools();
            InitializeContainers();
            
            if (_preloadPrefabs && _prefabLibrary != null)
            {
                PreloadPrefabs();
            }
            
            // LogInfo("UI Prefab Manager initialized successfully");
        }
        
        protected override void OnManagerShutdown()
        {
            ClearPools();
            _activeWidgets.Clear();
            _activeComponents.Clear();
            _activeModals.Clear();
            
            // LogInfo("UI Prefab Manager shutdown completed");
        }
        
        /// <summary>
        /// Initialize pooling system
        /// </summary>
        private void InitializePools()
        {
            _componentPools = new Dictionary<string, Queue<UIComponentPrefab>>();
            _activeComponents = new Dictionary<string, List<UIComponentPrefab>>();
            _prefabTemplates = new Dictionary<string, UIComponentPrefab>();
            _activeModals = new List<UIModalPrefab>();
            _activeNotifications = new List<UINotificationPrefab>();
            _activeWidgets = new Dictionary<string, UIWidgetPrefab>();
        }
        
        /// <summary>
        /// Initialize container references
        /// </summary>
        private void InitializeContainers()
        {
            if (_componentContainer == null)
            {
                var containerGO = new GameObject("UI Components");
                containerGO.transform.SetParent(transform);
                _componentContainer = containerGO.transform;
            }
            
            if (_widgetContainer == null)
            {
                var containerGO = new GameObject("UI Widgets");
                containerGO.transform.SetParent(transform);
                _widgetContainer = containerGO.transform;
            }
            
            if (_modalContainer == null)
            {
                var containerGO = new GameObject("UI Modals");
                containerGO.transform.SetParent(transform);
                _modalContainer = containerGO.transform;
            }
            
            if (_notificationContainer == null)
            {
                var containerGO = new GameObject("UI Notifications");
                containerGO.transform.SetParent(transform);
                _notificationContainer = containerGO.transform;
            }
        }
        
        /// <summary>
        /// Preload prefabs into pools
        /// </summary>
        private void PreloadPrefabs()
        {
            if (_prefabLibrary == null)
                return;
                
            foreach (var entry in _prefabLibrary.GetAllPrefabs())
            {
                if (entry.Prefab != null && entry.PreloadCount > 0)
                {
                    PreloadPrefab(entry.PrefabId, entry.PreloadCount);
                }
            }
            
            LogInfo($"Preloaded {_prefabTemplates.Count} UI prefab types");
        }
        
        /// <summary>
        /// Preload specific prefab type
        /// </summary>
        public void PreloadPrefab(string prefabId, int count)
        {
            if (!_enablePooling)
                return;
                
            var prefab = GetPrefabTemplate(prefabId);
            if (prefab == null)
                return;
                
            if (!_componentPools.ContainsKey(prefabId))
            {
                _componentPools[prefabId] = new Queue<UIComponentPrefab>();
                _activeComponents[prefabId] = new List<UIComponentPrefab>();
            }
            
            var pool = _componentPools[prefabId];
            for (int i = 0; i < count && pool.Count < _maxPoolSize; i++)
            {
                var instance = CreatePooledInstance(prefab);
                if (instance != null)
                {
                    pool.Enqueue(instance);
                }
            }
            
            LogInfo($"Preloaded {count} instances of prefab: {prefabId}");
        }
        
        /// <summary>
        /// Get prefab template by ID
        /// </summary>
        private UIComponentPrefab GetPrefabTemplate(string prefabId)
        {
            if (_prefabTemplates.ContainsKey(prefabId))
            {
                return _prefabTemplates[prefabId];
            }
            
            if (_prefabLibrary != null)
            {
                var prefab = _prefabLibrary.GetPrefab(prefabId);
                if (prefab != null)
                {
                    _prefabTemplates[prefabId] = prefab;
                    return prefab;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Create pooled instance
        /// </summary>
        private UIComponentPrefab CreatePooledInstance(UIComponentPrefab template)
        {
            var instance = Instantiate(template, GetContainerForPrefab(template));
            instance.gameObject.SetActive(false);
            instance.name = $"{template.name} (Pooled)";
            
            // Bind lifecycle events
            instance.OnComponentCreated += OnComponentInstanceCreated;
            instance.OnComponentDestroyed += OnComponentInstanceDestroyed;
            
            return instance;
        }
        
        /// <summary>
        /// Get appropriate container for prefab type
        /// </summary>
        private Transform GetContainerForPrefab(UIComponentPrefab prefab)
        {
            return prefab switch
            {
                UIWidgetPrefab => _widgetContainer,
                UIModalPrefab => _modalContainer,
                UINotificationPrefab => _notificationContainer,
                _ => _componentContainer
            };
        }
        
        /// <summary>
        /// Create UI component instance
        /// </summary>
        public T CreateComponent<T>(string prefabId) where T : UIComponentPrefab
        {
            return CreateComponent(prefabId) as T;
        }
        
        /// <summary>
        /// Create UI component instance
        /// </summary>
        public UIComponentPrefab CreateComponent(string prefabId)
        {
            var template = GetPrefabTemplate(prefabId);
            if (template == null)
            {
                LogError($"Prefab not found: {prefabId}");
                return null;
            }
            
            UIComponentPrefab instance = null;
            
            // Try to get from pool first
            if (_enablePooling && template.EnablePooling)
            {
                instance = GetFromPool(prefabId);
            }
            
            // Create new instance if pooling disabled or pool empty
            if (instance == null)
            {
                instance = CreateNewInstance(template);
            }
            
            if (instance != null)
            {
                ActivateComponent(instance);
            }
            
            return instance;
        }
        
        /// <summary>
        /// Get component from pool
        /// </summary>
        private UIComponentPrefab GetFromPool(string prefabId)
        {
            if (!_componentPools.ContainsKey(prefabId) || _componentPools[prefabId].Count == 0)
            {
                return null;
            }
            
            return _componentPools[prefabId].Dequeue();
        }
        
        /// <summary>
        /// Create new component instance
        /// </summary>
        private UIComponentPrefab CreateNewInstance(UIComponentPrefab template)
        {
            var instance = Instantiate(template, GetContainerForPrefab(template));
            instance.name = template.name;
            
            // Bind lifecycle events
            instance.OnComponentCreated += OnComponentInstanceCreated;
            instance.OnComponentDestroyed += OnComponentInstanceDestroyed;
            
            return instance;
        }
        
        /// <summary>
        /// Activate component instance
        /// </summary>
        private void ActivateComponent(UIComponentPrefab instance)
        {
            if (instance == null)
                return;
                
            instance.gameObject.SetActive(true);
            
            if (!instance.IsInitialized)
            {
                instance.Initialize();
            }
            
            // Track active instance
            var prefabId = instance.ComponentId;
            if (!_activeComponents.ContainsKey(prefabId))
            {
                _activeComponents[prefabId] = new List<UIComponentPrefab>();
            }
            _activeComponents[prefabId].Add(instance);
            
            // Track specialized types
            TrackSpecializedComponent(instance);
            
            LogInfo($"Component activated: {instance.ComponentName} ({instance.ComponentId})");
        }
        
        /// <summary>
        /// Track specialized component types
        /// </summary>
        private void TrackSpecializedComponent(UIComponentPrefab instance)
        {
            switch (instance)
            {
                case UIModalPrefab modal:
                    _activeModals.Add(modal);
                    modal.OnModalClosed += OnModalInstanceClosed;
                    break;
                    
                case UINotificationPrefab notification:
                    _activeNotifications.Add(notification);
                    notification.OnNotificationDismissed += OnNotificationInstanceDismissed;
                    break;
                    
                case UIWidgetPrefab widget:
                    _activeWidgets[widget.ComponentId] = widget;
                    widget.OnWidgetRemoved += OnWidgetInstanceRemoved;
                    break;
            }
        }
        
        /// <summary>
        /// Return component to pool or destroy
        /// </summary>
        public void ReturnComponent(UIComponentPrefab instance)
        {
            if (instance == null)
                return;
                
            // Remove from active tracking
            var prefabId = instance.ComponentId;
            if (_activeComponents.ContainsKey(prefabId))
            {
                _activeComponents[prefabId].Remove(instance);
            }
            
            // Remove from specialized tracking
            UntrackSpecializedComponent(instance);
            
            // Return to pool or destroy
            if (_enablePooling && instance.EnablePooling)
            {
                ReturnToPool(instance);
            }
            // else
            // {
                DestroyInstance(instance);
            // }
        }
        
        /// <summary>
        /// Untrack specialized component types
        /// </summary>
        private void UntrackSpecializedComponent(UIComponentPrefab instance)
        {
            switch (instance)
            {
                case UIModalPrefab modal:
                    _activeModals.Remove(modal);
                    modal.OnModalClosed -= OnModalInstanceClosed;
                    break;
                    
                case UINotificationPrefab notification:
                    _activeNotifications.Remove(notification);
                    notification.OnNotificationDismissed -= OnNotificationInstanceDismissed;
                    break;
                    
                case UIWidgetPrefab widget:
                    _activeWidgets.Remove(widget.ComponentId);
                    widget.OnWidgetRemoved -= OnWidgetInstanceRemoved;
                    break;
            }
        }
        
        /// <summary>
        /// Return instance to pool
        /// </summary>
        private void ReturnToPool(UIComponentPrefab instance)
        {
            instance.gameObject.SetActive(false);
            instance.name = $"{instance.ComponentName} (Pooled)";
            
            var templateId = GetTemplateId(instance);
            if (!string.IsNullOrEmpty(templateId))
            {
                if (!_componentPools.ContainsKey(templateId))
                {
                    _componentPools[templateId] = new Queue<UIComponentPrefab>();
                }
                
                if (_componentPools[templateId].Count < _maxPoolSize)
                {
                    _componentPools[templateId].Enqueue(instance);
                }
                // else
                // {
                    DestroyInstance(instance);
                // }
            }
            // else
            // {
                DestroyInstance(instance);
            // }
        }
        
        /// <summary>
        /// Get template ID for instance
        /// </summary>
        private string GetTemplateId(UIComponentPrefab instance)
        {
            if (_prefabLibrary != null)
            {
                return _prefabLibrary.GetPrefabId(instance);
            }
            return instance.ComponentId;
        }
        
        /// <summary>
        /// Destroy component instance
        /// </summary>
        private void DestroyInstance(UIComponentPrefab instance)
        {
            if (instance != null)
            {
                instance.Cleanup();
                Destroy(instance.gameObject);
            }
        }
        
        /// <summary>
        /// Create and show modal
        /// </summary>
        public T ShowModal<T>(string prefabId) where T : UIModalPrefab
        {
            var modal = CreateComponent<T>(prefabId);
            if (modal != null)
            {
                modal.ShowModal();
            }
            return modal;
        }
        
        /// <summary>
        /// Create and show notification
        /// </summary>
        public UINotificationPrefab ShowNotification(string prefabId, string title, string message, 
            NotificationSeverity severity = NotificationSeverity.Info)
        {
            var notification = CreateComponent<UINotificationPrefab>(prefabId);
            if (notification != null)
            {
                notification.SetContent(title, message, severity);
                notification.ShowNotification();
            }
            return notification;
        }
        
        /// <summary>
        /// Close all active modals
        /// </summary>
        public void CloseAllModals()
        {
            var modalsToClose = new List<UIModalPrefab>(_activeModals);
            foreach (var modal in modalsToClose)
            {
                modal.CloseModal();
            }
        }
        
        /// <summary>
        /// Dismiss all active notifications
        /// </summary>
        public void DismissAllNotifications()
        {
            var notificationsToClose = new List<UINotificationPrefab>(_activeNotifications);
            foreach (var notification in notificationsToClose)
            {
                notification.DismissNotification();
            }
        }
        
        /// <summary>
        /// Get active components of specific type
        /// </summary>
        public List<T> GetActiveComponents<T>() where T : UIComponentPrefab
        {
            var result = new List<T>();
            foreach (var componentList in _activeComponents.Values)
            {
                result.AddRange(componentList.OfType<T>());
            }
            return result;
        }
        
        /// <summary>
        /// Clear all pools
        /// </summary>
        public void ClearPools()
        {
            foreach (var pool in _componentPools.Values)
            {
                while (pool.Count > 0)
                {
                    var instance = pool.Dequeue();
                    if (instance != null)
                    {
                        DestroyInstance(instance);
                    }
                }
            }
            
            _componentPools.Clear();
            LogInfo("All UI component pools cleared");
        }
        
        // Event handlers
        private void OnComponentInstanceCreated(UIComponentPrefab component)
        {
            OnComponentCreated?.Invoke(component);
        }
        
        private void OnComponentInstanceDestroyed(UIComponentPrefab component)
        {
            OnComponentDestroyed?.Invoke(component);
        }
        
        private void OnModalInstanceClosed(UIModalPrefab modal)
        {
            ReturnComponent(modal);
            OnModalClosed?.Invoke(modal);
        }
        
        private void OnNotificationInstanceDismissed(UINotificationPrefab notification)
        {
            ReturnComponent(notification);
            OnNotificationDismissed?.Invoke(notification);
        }
        
        private void OnWidgetInstanceRemoved(UIWidgetPrefab widget)
        {
            ReturnComponent(widget);
        }
        
        protected void OnValidate()
        {
            
            _initialPoolSize = Mathf.Max(0, _initialPoolSize);
            _maxPoolSize = Mathf.Max(_initialPoolSize, _maxPoolSize);
        }
        
        protected override void OnDestroy()
        {
            ClearPools();
            base.OnDestroy();
        }
        
        // Development/Debug methods
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public void LogPoolStats()
        {
            LogInfo($"UI Pool Statistics:");
            LogInfo($"- Total active components: {ActiveComponentCount}");
            LogInfo($"- Active modals: {ActiveModalCount}");
            LogInfo($"- Active notifications: {ActiveNotificationCount}");
            LogInfo($"- Active widgets: {ActiveWidgetCount}");
            
            foreach (var kvp in _componentPools)
            {
                LogInfo($"- Pool '{kvp.Key}': {kvp.Value.Count} pooled instances");
            }
        }
    }
}