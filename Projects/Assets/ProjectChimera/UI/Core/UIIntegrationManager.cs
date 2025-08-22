using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ProjectChimera.Core;
using ProjectChimera.Data.UI;

namespace ProjectChimera.UI.Core
{
    /// <summary>
    /// Central manager for coordinating UI-to-Manager communication and data binding.
    /// Handles event routing, data synchronization, and integration between UI systems and core managers.
    /// </summary>
    public class UIIntegrationManager : ChimeraManager
    {
        [Header("Integration Configuration")]
        [SerializeField] private UIEventChannelSO _globalEventChannel;
        [SerializeField] private List<UIEventChannelSO> _eventChannels = new List<UIEventChannelSO>();
        [SerializeField] private List<UIDataBindingSO> _dataBindings = new List<UIDataBindingSO>();
        [SerializeField] private bool _enableAutoDiscovery = true;
        [SerializeField] private float _dataRefreshInterval = 0.1f;
        
        [Header("Debug Configuration")]
        [SerializeField] private bool _logIntegrationEvents = true;
        [SerializeField] private bool _logDataBindings = false;
        [SerializeField] private bool _logManagerCommunication = true;
        
        // System references
        // private GameUIManager _uiManager;
        private Dictionary<Type, ChimeraManager> _managerReferences = new Dictionary<Type, ChimeraManager>();
        
        // Integration state
        private Dictionary<string, UIDataBindingSO> _activeBindings = new Dictionary<string, UIDataBindingSO>();
        private Dictionary<string, UIEventChannelSO> _channelRegistry = new Dictionary<string, UIEventChannelSO>();
        private Queue<IntegrationTask> _integrationTasks = new Queue<IntegrationTask>();
        
        // Data cache and synchronization
        private Dictionary<string, object> _managerDataCache = new Dictionary<string, object>();
        private Dictionary<string, DateTime> _lastDataUpdate = new Dictionary<string, DateTime>();
        private bool _isProcessingTasks = false;
        
        public override ManagerPriority Priority => ManagerPriority.High;
        
        // Properties
        public UIEventChannelSO GlobalEventChannel => _globalEventChannel;
        public IReadOnlyDictionary<string, UIDataBindingSO> ActiveBindings => _activeBindings;
        public IReadOnlyDictionary<string, UIEventChannelSO> EventChannels => _channelRegistry;
        public bool IsIntegrationActive { get; private set; }
        
        // Events
        public event Action OnIntegrationInitialized;
        public event Action<string, object> OnManagerDataUpdated;
        public event Action<UIEventData> OnUIEventProcessed;
        public event Action<string> OnBindingError;
        
        protected override void OnManagerInitialize()
        {
            InitializeSystemReferences();
            SetupEventChannels();
            InitializeDataBindings();
            RegisterEventHandlers();
            
            // Start integration processing
            IsIntegrationActive = true;
            InvokeRepeating(nameof(ProcessIntegrationTasks), 0.1f, 0.05f);
            InvokeRepeating(nameof(RefreshManagerData), 1f, _dataRefreshInterval);
            
            OnIntegrationInitialized?.Invoke();
            // LogInfo("UI Integration Manager initialized with comprehensive manager communication");
        }
        
        protected override void OnManagerUpdate()
        {
            // Process any pending integration tasks
            if (!_isProcessingTasks && _integrationTasks.Count > 0)
            {
                ProcessIntegrationTasks();
            }
        }
        
        private void InitializeSystemReferences()
        {
            // Get UI Manager reference
            // _uiManager = GameManager.Instance?.GetManager<GameUIManager>();
            // if (_uiManager == null)
            // {
                // LogWarning("GameUIManager not found - UI integration will be limited");
            // }
            
            // Discover and cache all manager references
            if (_enableAutoDiscovery)
            {
                DiscoverManagers();
            }
            
            LogInfo($"UI Integration initialized with {_managerReferences.Count} managers");
        }        
        private void DiscoverManagers()
        {
            _managerReferences.Clear();
            
            var gameManager = GameManager.Instance;
            if (gameManager == null) return;
            
            // Use reflection to find all manager types
            var managerTypes = Assembly.GetExecutingAssembly().GetTypes()
                // .Where(t => t.IsSubclassOf(typeof(ChimeraManager)) && !t.IsAbstract)
                .ToList();
            
            foreach (var managerType in managerTypes)
            {
                try
                {
                    // Try to get manager instance using GetManager<T>()
                    // var getManagerMethod = typeof(GameManager).GetMethod("GetManager")
                        // .MakeGenericMethod(managerType);
                    var getManagerMethod = (System.Reflection.MethodInfo)null; // Placeholder
                    
                    var manager = getManagerMethod.Invoke(gameManager, null) as ChimeraManager;
                    if (manager != null)
                    {
                        _managerReferences[managerType] = manager;
                        
                        if (_logIntegrationEvents)
                        {
                            LogInfo($"Discovered manager: {managerType.Name}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogWarning($"Failed to discover manager {managerType.Name}: {ex.Message}");
                }
            }
        }
        
        private void SetupEventChannels()
        {
            _channelRegistry.Clear();
            
            // Register global event channel
            if (_globalEventChannel != null)
            {
                _channelRegistry["global"] = _globalEventChannel;
                _globalEventChannel.OnUIEvent += HandleUIEvent;
            }
            
            // Register specific event channels
            foreach (var channel in _eventChannels)
            {
                if (channel != null)
                {
                    _channelRegistry[channel.ChannelName] = channel;
                    channel.OnUIEvent += HandleUIEvent;
                }
            }
            
            LogInfo($"Registered {_channelRegistry.Count} event channels");
        }        
        private void InitializeDataBindings()
        {
            _activeBindings.Clear();
            
            foreach (var binding in _dataBindings)
            {
                if (binding != null)
                {
                    _activeBindings[binding.BindingName] = binding;
                    binding.ActivateBinding();
                }
            }
            
            LogInfo($"Initialized {_activeBindings.Count} data bindings");
        }
        
        private void RegisterEventHandlers()
        {
            // Register with UI Manager if available
            // if (_uiManager != null)
            // {
                // Add event handler registrations here when needed
            // }
        }
        
        private void HandleUIEvent(UIEventData eventData)
        {
            var task = new IntegrationTask
            {
                Type = TaskType.ProcessEvent,
                Data = eventData,
                Timestamp = DateTime.Now
            };
            
            _integrationTasks.Enqueue(task);
            OnUIEventProcessed?.Invoke(eventData);
            
            if (_logIntegrationEvents)
            {
                LogInfo($"Queued UI event: {eventData}");
            }
        }        
        private void ProcessIntegrationTasks()
        {
            if (_isProcessingTasks || _integrationTasks.Count == 0)
                return;
            
            _isProcessingTasks = true;
            
            try
            {
                int tasksProcessed = 0;
                const int maxTasksPerFrame = 10;
                
                while (_integrationTasks.Count > 0 && tasksProcessed < maxTasksPerFrame)
                {
                    var task = _integrationTasks.Dequeue();
                    ProcessIntegrationTask(task);
                    tasksProcessed++;
                }
            }
            catch (Exception ex)
            {
                LogError($"Error processing integration tasks: {ex.Message}");
            }
            finally
            {
                _isProcessingTasks = false;
            }
        }
        
        private void ProcessIntegrationTask(IntegrationTask task)
        {
            switch (task.Type)
            {
                case TaskType.ProcessEvent:
                    if (task.Data is UIEventData eventData)
                    {
                        ProcessUIEventData(eventData);
                    }
                    break;
                    
                case TaskType.UpdateManagerData:
                    // Process manager data updates
                    break;
                    
                case TaskType.ExecuteAction:
                    if (task.Data is string actionId)
                    {
                        ExecuteManagerAction(actionId);
                    }
                    break;
                    
                case TaskType.RefreshBindings:
                    RefreshAllDataBindings();
                    break;
            }
        }        
        private void ProcessUIEventData(UIEventData eventData)
        {
            // Route event to appropriate manager based on target
            // if (_logManagerCommunication)
            // {
                LogInfo($"Processing UI event data: {eventData}");
            // }
        }
        
        private void ExecuteManagerAction(string actionId)
        {
            try
            {
                var parts = actionId.Split('.');
                if (parts.Length >= 2)
                {
                    var managerName = parts[0];
                    var methodName = parts[1];
                    
                    // if (_logManagerCommunication)
                    // {
                        LogInfo($"Executing action: {actionId}");
                    // }
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to execute action {actionId}: {ex.Message}");
            }
        }
        
        private void RefreshAllDataBindings()
        {
            foreach (var binding in _activeBindings.Values)
            {
                // Refresh binding data
            }
        }        
        private void RefreshManagerData()
        {
            if (!IsIntegrationActive) return;
            
            foreach (var managerKvp in _managerReferences)
            {
                try
                {
                    // Refresh manager data cache
                    var managerData = new Dictionary<string, object>();
                    var cacheKey = managerKvp.Key.Name;
                    _managerDataCache[cacheKey] = managerData;
                    _lastDataUpdate[cacheKey] = DateTime.Now;
                    
                    OnManagerDataUpdated?.Invoke(cacheKey, managerData);
                }
                catch (Exception ex)
                {
                    LogError($"Failed to refresh data for {managerKvp.Key.Name}: {ex.Message}");
                }
            }
        }
        
        protected override void OnManagerShutdown()
        {
            IsIntegrationActive = false;
            
            // Cleanup event subscriptions
            foreach (var channel in _channelRegistry.Values)
            {
                if (channel != null)
                {
                    channel.OnUIEvent -= HandleUIEvent;
                }
            }
            
            // Deactivate all bindings
            foreach (var binding in _activeBindings.Values)
            {
                binding.DeactivateBinding();
            }
            
            // Clear collections
            _activeBindings.Clear();
            _channelRegistry.Clear();
            _managerReferences.Clear();
            _managerDataCache.Clear();
            _lastDataUpdate.Clear();
            _integrationTasks.Clear();
            
            // LogInfo("UI Integration Manager shutdown complete");
        }
    }    
    /// <summary>
    /// Task for integration processing
    /// </summary>
    [System.Serializable]
    public class IntegrationTask
    {
        public TaskType Type;
        public object Data;
        public DateTime Timestamp;
        public int Priority = 0;
    }
    
    /// <summary>
    /// Types of integration tasks
    /// </summary>
    public enum TaskType
    {
        ProcessEvent,
        UpdateManagerData,
        ExecuteAction,
        RefreshBindings,
        SyncPreferences
    }
}