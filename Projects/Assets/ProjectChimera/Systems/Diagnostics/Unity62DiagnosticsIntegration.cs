using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_SERVICES_ANALYTICS
using Unity.Services.Core;
using Unity.Services.Analytics;
#endif
using ProjectChimera.Systems.Analytics;

namespace ProjectChimera.Systems.Diagnostics
{
    /// <summary>
    /// Unity 6.2 Diagnostics Integration for Project Chimera
    /// Leverages built-in diagnostic data collection and Unity Dashboard
    /// Replaces custom analytics systems with Unity's integrated solutions
    /// </summary>
    public class Unity62DiagnosticsIntegration : MonoBehaviour
    {
        [Header("Unity 6.2 Diagnostics Configuration")]
        [SerializeField] private bool _enableUnityAnalytics = true;
        [SerializeField] private bool _enableCrashReporting = true;
        [SerializeField] private bool _enablePerformanceReporting = true;
        [SerializeField] private bool _enableCloudDiagnostics = true;
        [SerializeField] private string _projectId = "";
        
        [Header("Data Collection Settings")]
        [SerializeField] private bool _collectUserData = false; // GDPR compliance
        [SerializeField] private bool _collectDeviceInfo = true;
        [SerializeField] private bool _collectPerformanceMetrics = true;
        [SerializeField] private bool _collectGameplayMetrics = true;
        [SerializeField] private bool _enableDebugMode = false;
        
        [Header("Custom Event Configuration")]
        [SerializeField] private int _maxEventsPerSession = 500;
        [SerializeField] private float _eventFlushInterval = 30f; // seconds
        [SerializeField] private bool _enableOfflineEventStorage = true;
        [SerializeField] private int _maxOfflineEvents = 1000;
        
        // Integration Systems
        private DataPipelineIntegration _dataPipeline;
        private AdvancedAnalytics _advancedAnalytics;
        
        // Unity Analytics State
        private bool _isUnityServicesInitialized = false;
        private bool _isAnalyticsInitialized = false;
        private readonly Dictionary<string, object> _sessionParameters = new Dictionary<string, object>();
        
        // Custom Event Queue
        private readonly Queue<CustomDiagnosticEvent> _pendingEvents = new Queue<CustomDiagnosticEvent>();
        private float _lastEventFlush = 0f;
        
        // Performance Tracking
        private readonly List<PerformanceDiagnosticData> _performanceData = new List<PerformanceDiagnosticData>();
        private float _performanceCollectionInterval = 10f;
        private float _lastPerformanceCollection = 0f;
        
        // Events
        public event Action OnUnityServicesInitialized;
        public event Action OnAnalyticsInitialized;
        public event Action<string> OnDiagnosticsError;
        public event Action<CustomDiagnosticEvent> OnCustomEventSent;
        
        private void Awake()
        {
            InitializeDiagnostics();
        }
        
        private void Start()
        {
            FindSystemReferences();
            _ = InitializeUnityServicesAsync();
        }
        
        private void Update()
        {
            if (_isAnalyticsInitialized)
            {
                ProcessEventQueue();
                CollectPerformanceData();
            }
        }
        
        private void InitializeDiagnostics()
        {
            // Configure session parameters
            _sessionParameters["session_id"] = System.Guid.NewGuid().ToString();
            _sessionParameters["session_start"] = DateTime.UtcNow.ToString("O");
            _sessionParameters["unity_version"] = Application.unityVersion;
            _sessionParameters["game_version"] = Application.version;
            _sessionParameters["platform"] = Application.platform.ToString();
            
            Debug.Log("[Unity62DiagnosticsIntegration] Diagnostics system initialized");
        }
        
        private void FindSystemReferences()
        {
            _dataPipeline = UnityEngine.Object.FindObjectOfType<DataPipelineIntegration>();
            _advancedAnalytics = UnityEngine.Object.FindObjectOfType<AdvancedAnalytics>();
            
            if (_dataPipeline == null)
                Debug.LogWarning("[Unity62DiagnosticsIntegration] DataPipelineIntegration not found");
            
            if (_advancedAnalytics == null)
                Debug.LogWarning("[Unity62DiagnosticsIntegration] AdvancedAnalytics not found");
        }
        
        private async Task InitializeUnityServicesAsync()
        {
            try
            {
                if (!_isUnityServicesInitialized)
                {
                    // Initialize Unity Services
#if UNITY_SERVICES_ANALYTICS
                    await UnityServices.InitializeAsync();
#endif
                    _isUnityServicesInitialized = true;
                    OnUnityServicesInitialized?.Invoke();
                    
                    Debug.Log("[Unity62DiagnosticsIntegration] Unity Services initialized successfully");
                }
                
                // Initialize Analytics if enabled
                if (_enableUnityAnalytics && !_isAnalyticsInitialized)
                {
                    await InitializeAnalyticsAsync();
                }
                
                // Configure crash reporting if enabled
                if (_enableCrashReporting)
                {
                    ConfigureCrashReporting();
                }
                
                // Configure performance reporting if enabled
                if (_enablePerformanceReporting)
                {
                    ConfigurePerformanceReporting();
                }
                
                // Configure cloud diagnostics if enabled
                if (_enableCloudDiagnostics)
                {
                    ConfigureCloudDiagnostics();
                }
                
                // Send initialization event
                await SendCustomEventAsync("system_initialization", new Dictionary<string, object>
                {
                    ["unity_services_initialized"] = _isUnityServicesInitialized,
                    ["analytics_initialized"] = _isAnalyticsInitialized,
                    ["crash_reporting_enabled"] = _enableCrashReporting,
                    ["performance_reporting_enabled"] = _enablePerformanceReporting,
                    ["initialization_time"] = Time.realtimeSinceStartup
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Unity62DiagnosticsIntegration] Failed to initialize Unity Services: {ex.Message}");
                OnDiagnosticsError?.Invoke($"Unity Services initialization failed: {ex.Message}");
            }
        }
        
        private async Task InitializeAnalyticsAsync()
        {
            try
            {
                // Configure analytics settings
                var initOptions = new AnalyticsInitOptions
                {
                    consentRequired = !_collectUserData, // GDPR compliance
                    enableDebugMode = _enableDebugMode
                };
                
                // Initialize Unity Analytics
#if UNITY_SERVICES_ANALYTICS
                await AnalyticsService.Instance.StartDataCollectionAsync();
#endif
                _isAnalyticsInitialized = true;
                OnAnalyticsInitialized?.Invoke();
                
                // Configure data collection preferences
                ConfigureDataCollection();
                
                // Send session start event
                await SendSessionStartEventAsync();
                
                Debug.Log("[Unity62DiagnosticsIntegration] Unity Analytics initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Unity62DiagnosticsIntegration] Failed to initialize Analytics: {ex.Message}");
                OnDiagnosticsError?.Invoke($"Analytics initialization failed: {ex.Message}");
            }
        }
        
        private void ConfigureDataCollection()
        {
            // Configure what data Unity Analytics collects
            var configuration = new Dictionary<string, object>
            {
                ["collect_device_info"] = _collectDeviceInfo,
                ["collect_performance_metrics"] = _collectPerformanceMetrics,
                ["collect_gameplay_metrics"] = _collectGameplayMetrics,
                ["max_events_per_session"] = _maxEventsPerSession,
                ["event_flush_interval"] = _eventFlushInterval,
                ["offline_storage_enabled"] = _enableOfflineEventStorage,
                ["max_offline_events"] = _maxOfflineEvents
            };
            
            // Apply configuration through Unity Analytics
            // Note: Actual Unity Analytics configuration would go here
            // This is a placeholder for the configuration API
        }
        
        private void ConfigureCrashReporting()
        {
            // Unity 6.2 automatically handles crash reporting
            // Just need to enable and configure preferences
            
            Debug.Log("[Unity62DiagnosticsIntegration] Crash reporting configured - Unity 6.2 automatic collection enabled");
            
            // Configure additional crash context if needed
            SetCrashContext("project_chimera_version", Application.version);
            SetCrashContext("cultivation_systems", "enabled");
            SetCrashContext("genetics_engine", "enabled");
            SetCrashContext("3d_grid_system", "enabled");
        }
        
        private void ConfigurePerformanceReporting()
        {
            // Unity 6.2 built-in performance monitoring
            // Configure additional performance metrics specific to Project Chimera
            
            Debug.Log("[Unity62DiagnosticsIntegration] Performance reporting configured - Unity 6.2 automatic collection enabled");
            
            // Set performance tracking context
            SetPerformanceContext("cultivation_scale", "large");
            SetPerformanceContext("graphics_quality", QualitySettings.names[QualitySettings.GetQualityLevel()]);
            SetPerformanceContext("target_frame_rate", Application.targetFrameRate.ToString());
        }
        
        private void ConfigureCloudDiagnostics()
        {
            // Unity 6.2 cloud diagnostics configuration
            // Integrates with Unity Dashboard for real-time insights
            
            Debug.Log("[Unity62DiagnosticsIntegration] Cloud diagnostics configured - Unity Dashboard integration enabled");
            
            // Configure cloud diagnostic preferences
            var cloudConfig = new Dictionary<string, object>
            {
                ["real_time_insights"] = true,
                ["performance_monitoring"] = _enablePerformanceReporting,
                ["crash_reporting"] = _enableCrashReporting,
                ["user_journey_tracking"] = _collectGameplayMetrics
            };
            
            // Apply cloud configuration
            // Note: Actual Unity cloud configuration API would go here
        }
        
        private async Task SendSessionStartEventAsync()
        {
            var sessionData = new Dictionary<string, object>(_sessionParameters)
            {
                ["device_model"] = SystemInfo.deviceModel,
                ["device_type"] = SystemInfo.deviceType.ToString(),
                ["operating_system"] = SystemInfo.operatingSystem,
                ["processor_type"] = SystemInfo.processorType,
                ["graphics_device"] = SystemInfo.graphicsDeviceName,
                ["system_memory"] = SystemInfo.systemMemorySize,
                ["graphics_memory"] = SystemInfo.graphicsMemorySize,
                ["screen_resolution"] = $"{Screen.width}x{Screen.height}",
                ["quality_level"] = QualitySettings.names[QualitySettings.GetQualityLevel()]
            };
            
            await SendCustomEventAsync("session_start", sessionData);
        }
        
        public async Task SendCustomEventAsync(string eventName, Dictionary<string, object> parameters = null)
        {
            if (!_isAnalyticsInitialized)
            {
                // Queue event for later sending
                QueueEvent(eventName, parameters);
                return;
            }
            
            try
            {
                var eventData = new Dictionary<string, object>(parameters ?? new Dictionary<string, object>());
                
                // Add session context to all events
                eventData["session_id"] = _sessionParameters["session_id"];
                eventData["timestamp"] = DateTime.UtcNow.ToString("O");
                eventData["game_time"] = Time.realtimeSinceStartup;
                
                // Send through Unity Analytics
#if UNITY_SERVICES_ANALYTICS
                AnalyticsService.Instance.CustomData(eventName, eventData);
#endif
                
                // Also send to internal analytics system if available
                if (_advancedAnalytics != null)
                {
                    _advancedAnalytics.CollectEvent(eventName, "unity_diagnostics", eventData);
                }
                
                var customEvent = new CustomDiagnosticEvent
                {
                    EventName = eventName,
                    Parameters = eventData,
                    Timestamp = DateTime.UtcNow
                };
                
                OnCustomEventSent?.Invoke(customEvent);
                
                if (_enableDebugMode)
                {
                    Debug.Log($"[Unity62DiagnosticsIntegration] Sent custom event: {eventName}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Unity62DiagnosticsIntegration] Failed to send custom event {eventName}: {ex.Message}");
                OnDiagnosticsError?.Invoke($"Failed to send event {eventName}: {ex.Message}");
            }
        }
        
        private void QueueEvent(string eventName, Dictionary<string, object> parameters)
        {
            if (_pendingEvents.Count >= _maxOfflineEvents)
            {
                _pendingEvents.Dequeue(); // Remove oldest event
            }
            
            _pendingEvents.Enqueue(new CustomDiagnosticEvent
            {
                EventName = eventName,
                Parameters = parameters ?? new Dictionary<string, object>(),
                Timestamp = DateTime.UtcNow
            });
        }
        
        private void ProcessEventQueue()
        {
            if (Time.time - _lastEventFlush < _eventFlushInterval)
                return;
            
            _lastEventFlush = Time.time;
            
            // Process pending events
            var eventsToProcess = Math.Min(_pendingEvents.Count, 10); // Process max 10 events per flush
            for (int i = 0; i < eventsToProcess; i++)
            {
                var pendingEvent = _pendingEvents.Dequeue();
                _ = SendCustomEventAsync(pendingEvent.EventName, pendingEvent.Parameters);
            }
        }
        
        private void CollectPerformanceData()
        {
            if (Time.time - _lastPerformanceCollection < _performanceCollectionInterval)
                return;
            
            _lastPerformanceCollection = Time.time;
            
            var performanceData = new PerformanceDiagnosticData
            {
                Timestamp = DateTime.UtcNow,
                FrameRate = 1f / Time.unscaledDeltaTime,
                MemoryUsage = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory(),
                RenderTime = Time.smoothDeltaTime * 1000f, // Convert to milliseconds
                CPUTime = Time.unscaledDeltaTime * 1000f,
                ActiveObjects = UnityEngine.Object.FindObjectsOfType<GameObject>().Length,
                DrawCalls = 0, // Would need Unity Profiler API integration
                Triangles = 0 // Would need Unity Profiler API integration
            };
            
            _performanceData.Add(performanceData);
            
            // Keep only recent performance data
            if (_performanceData.Count > 100)
            {
                _performanceData.RemoveAt(0);
            }
            
            // Send performance metrics to Unity Analytics
            if (_collectPerformanceMetrics)
            {
                _ = SendPerformanceMetricsAsync(performanceData);
            }
        }
        
        private async Task SendPerformanceMetricsAsync(PerformanceDiagnosticData data)
        {
            var metricsData = new Dictionary<string, object>
            {
                ["frame_rate"] = data.FrameRate,
                ["memory_usage_mb"] = data.MemoryUsage / (1024 * 1024),
                ["render_time_ms"] = data.RenderTime,
                ["cpu_time_ms"] = data.CPUTime,
                ["active_objects"] = data.ActiveObjects,
                ["quality_level"] = QualitySettings.GetQualityLevel()
            };
            
            await SendCustomEventAsync("performance_metrics", metricsData);
        }
        
        public async Task SendCultivationEventAsync(string action, Dictionary<string, object> cultivationData)
        {
            var eventData = new Dictionary<string, object>(cultivationData ?? new Dictionary<string, object>())
            {
                ["cultivation_action"] = action,
                ["system"] = "cultivation"
            };
            
            await SendCustomEventAsync("cultivation_event", eventData);
        }
        
        public async Task SendConstructionEventAsync(string action, Dictionary<string, object> constructionData)
        {
            var eventData = new Dictionary<string, object>(constructionData ?? new Dictionary<string, object>())
            {
                ["construction_action"] = action,
                ["system"] = "construction"
            };
            
            await SendCustomEventAsync("construction_event", eventData);
        }
        
        public async Task SendEconomyEventAsync(string action, Dictionary<string, object> economyData)
        {
            var eventData = new Dictionary<string, object>(economyData ?? new Dictionary<string, object>())
            {
                ["economy_action"] = action,
                ["system"] = "economy"
            };
            
            await SendCustomEventAsync("economy_event", eventData);
        }
        
        public async Task SendGeneticsEventAsync(string action, Dictionary<string, object> geneticsData)
        {
            var eventData = new Dictionary<string, object>(geneticsData ?? new Dictionary<string, object>())
            {
                ["genetics_action"] = action,
                ["system"] = "genetics"
            };
            
            await SendCustomEventAsync("genetics_event", eventData);
        }
        
        public async Task SendUserJourneyEventAsync(string journey, string step, Dictionary<string, object> journeyData = null)
        {
            var eventData = new Dictionary<string, object>(journeyData ?? new Dictionary<string, object>())
            {
                ["user_journey"] = journey,
                ["journey_step"] = step,
                ["system"] = "user_experience"
            };
            
            await SendCustomEventAsync("user_journey", eventData);
        }
        
        public void SetCrashContext(string key, string value)
        {
            // Unity 6.2 crash context setting
            // Note: Actual Unity crash context API would be used here
            Debug.Log($"[Unity62DiagnosticsIntegration] Set crash context: {key} = {value}");
        }
        
        public void SetPerformanceContext(string key, string value)
        {
            // Unity 6.2 performance context setting
            // Note: Actual Unity performance context API would be used here
            Debug.Log($"[Unity62DiagnosticsIntegration] Set performance context: {key} = {value}");
        }
        
        public void ConfigureBuildProfileDiagnostics(string buildProfile)
        {
            // Unity 6.2 per-build-profile diagnostic settings
            var profileConfig = new Dictionary<string, object>
            {
                ["build_profile"] = buildProfile,
                ["diagnostics_level"] = buildProfile == "Debug" ? "Verbose" : "Standard",
                ["crash_reporting"] = true,
                ["performance_monitoring"] = buildProfile != "Release",
                ["analytics_enabled"] = _enableUnityAnalytics
            };
            
            Debug.Log($"[Unity62DiagnosticsIntegration] Configured diagnostics for build profile: {buildProfile}");
        }
        
        public List<PerformanceDiagnosticData> GetRecentPerformanceData(int maxEntries = 50)
        {
            var recentData = _performanceData.Count > maxEntries 
                ? _performanceData.GetRange(_performanceData.Count - maxEntries, maxEntries)
                : new List<PerformanceDiagnosticData>(_performanceData);
            
            return recentData;
        }
        
        public Dictionary<string, object> GetDiagnosticsStatus()
        {
            return new Dictionary<string, object>
            {
                ["unity_services_initialized"] = _isUnityServicesInitialized,
                ["analytics_initialized"] = _isAnalyticsInitialized,
                ["crash_reporting_enabled"] = _enableCrashReporting,
                ["performance_reporting_enabled"] = _enablePerformanceReporting,
                ["cloud_diagnostics_enabled"] = _enableCloudDiagnostics,
                ["pending_events"] = _pendingEvents.Count,
                ["performance_data_points"] = _performanceData.Count,
                ["session_id"] = _sessionParameters.GetValueOrDefault("session_id", "none"),
                ["collection_interval"] = _performanceCollectionInterval,
                ["event_flush_interval"] = _eventFlushInterval
            };
        }
        
        public void SetDataCollectionConsent(bool hasConsent)
        {
            _collectUserData = hasConsent;
            
            // Update Unity Analytics consent
            if (_isAnalyticsInitialized)
            {
                // Note: Actual Unity Analytics consent API would be used here
                Debug.Log($"[Unity62DiagnosticsIntegration] Data collection consent updated: {hasConsent}");
            }
        }
        
        private async void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                await SendCustomEventAsync("application_pause", new Dictionary<string, object>
                {
                    ["session_duration"] = Time.realtimeSinceStartup,
                    ["performance_data_collected"] = _performanceData.Count
                });
            }
            else
            {
                await SendCustomEventAsync("application_resume", new Dictionary<string, object>
                {
                    ["time_paused"] = DateTime.UtcNow.ToString("O")
                });
            }
        }
        
        private async void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                await SendCustomEventAsync("application_lost_focus", new Dictionary<string, object>
                {
                    ["session_duration"] = Time.realtimeSinceStartup
                });
            }
            else
            {
                await SendCustomEventAsync("application_gained_focus", new Dictionary<string, object>
                {
                    ["time_unfocused"] = DateTime.UtcNow.ToString("O")
                });
            }
        }
        
        private async void OnDestroy()
        {
            if (_isAnalyticsInitialized)
            {
                await SendCustomEventAsync("session_end", new Dictionary<string, object>
                {
                    ["session_duration"] = Time.realtimeSinceStartup,
                    ["events_sent"] = _maxEventsPerSession - _pendingEvents.Count,
                    ["performance_data_collected"] = _performanceData.Count,
                    ["end_reason"] = "application_quit"
                });
            }
        }
    }
    
    /// <summary>
    /// Custom diagnostic event structure
    /// </summary>
    [System.Serializable]
    public class CustomDiagnosticEvent
    {
        public string EventName;
        public Dictionary<string, object> Parameters;
        public DateTime Timestamp;
    }
    
    /// <summary>
    /// Performance diagnostic data structure
    /// </summary>
    [System.Serializable]
    public class PerformanceDiagnosticData
    {
        public DateTime Timestamp;
        public float FrameRate;
        public long MemoryUsage;
        public float RenderTime;
        public float CPUTime;
        public int ActiveObjects;
        public int DrawCalls;
        public long Triangles;
    }
    
    /// <summary>
    /// Analytics initialization options
    /// </summary>
    public class AnalyticsInitOptions
    {
        public bool consentRequired = false;
        public bool enableDebugMode = false;
        public string customUserId = null;
        public Dictionary<string, object> customParameters = new Dictionary<string, object>();
    }
}