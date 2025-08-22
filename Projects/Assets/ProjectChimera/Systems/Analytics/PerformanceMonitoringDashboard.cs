using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using UnityEngine;
using ProjectChimera.Systems.Services.Core;
using ProjectChimera.Systems.UI.Advanced;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Comprehensive performance monitoring dashboard for Project Chimera
    /// Provides real-time system metrics, performance visualization, and alerts
    /// </summary>
    public class PerformanceMonitoringDashboard : MonoBehaviour
    {
        [Header("Dashboard Configuration")]
        [SerializeField] private bool _enableRealTimeUpdates = true;
        [SerializeField] private float _updateInterval = 1f;
        [SerializeField] private int _maxHistoryPoints = 300;
        [SerializeField] private bool _enableAlerts = true;
        [SerializeField] private float _alertThresholdCPU = 80f;
        [SerializeField] private float _alertThresholdMemory = 85f;
        [SerializeField] private float _alertThresholdFrameTime = 33.33f; // 30 FPS threshold
        
        [Header("Display Settings")]
        [SerializeField] private bool _showSystemMetrics = true;
        [SerializeField] private bool _showGameplayMetrics = true;
        [SerializeField] private bool _showNetworkMetrics = true;
        [SerializeField] private bool _showDetailedBreakdown = false;
        
        // Core Systems
        private DataPipelineIntegration _dataPipeline;
        private AdvancedAnalytics _analytics;
        private ServiceLayerCoordinator _serviceCoordinator;
        private AdvancedMenuSystem _menuSystem;
        
        // Performance Tracking
        private readonly PerformanceMetrics _currentMetrics = new PerformanceMetrics();
        private readonly Queue<PerformanceSnapshot> _metricsHistory = new Queue<PerformanceSnapshot>();
        private readonly Dictionary<string, SystemPerformanceTracker> _systemTrackers = new Dictionary<string, SystemPerformanceTracker>();
        private readonly List<PerformanceAlert> _activeAlerts = new List<PerformanceAlert>();
        
        // Update Management
        private float _lastUpdateTime;
        private bool _isCollecting = false;
        private int _frameCount = 0;
        private float _frameTimeAccumulator = 0f;
        
        // Events
        public event Action<PerformanceSnapshot> OnMetricsUpdated;
        public event Action<PerformanceAlert> OnAlertTriggered;
        public event Action<string> OnAlertResolved;
        
        private void Awake()
        {
            InitializeTrackers();
        }
        
        private void Start()
        {
            FindSystemReferences();
            StartMonitoring();
        }
        
        private void Update()
        {
            if (_isCollecting)
            {
                CollectFrameMetrics();
                
                if (Time.time - _lastUpdateTime >= _updateInterval)
                {
                    UpdateMetrics();
                    _lastUpdateTime = Time.time;
                }
            }
        }
        
        private void FindSystemReferences()
        {
            _dataPipeline = UnityEngine.Object.FindObjectOfType<DataPipelineIntegration>();
            _analytics = UnityEngine.Object.FindObjectOfType<AdvancedAnalytics>();
            _serviceCoordinator = UnityEngine.Object.FindObjectOfType<ServiceLayerCoordinator>();
            _menuSystem = UnityEngine.Object.FindObjectOfType<AdvancedMenuSystem>();
            
            if (_dataPipeline == null)
                Debug.LogWarning("[PerformanceMonitoringDashboard] DataPipelineIntegration not found");
            
            if (_analytics == null)
                Debug.LogWarning("[PerformanceMonitoringDashboard] AdvancedAnalytics not found");
        }
        
        private void InitializeTrackers()
        {
            // Core system trackers
            RegisterSystemTracker("Unity", new UnitySystemTracker());
            RegisterSystemTracker("Rendering", new RenderingTracker());
            RegisterSystemTracker("Physics", new PhysicsTracker());
            RegisterSystemTracker("Audio", new AudioTracker());
            RegisterSystemTracker("Input", new InputTracker());
            RegisterSystemTracker("Networking", new NetworkingTracker());
            
            // Game-specific trackers
            RegisterSystemTracker("Plants", new PlantSystemTracker());
            RegisterSystemTracker("Genetics", new GeneticsTracker());
            RegisterSystemTracker("Environment", new EnvironmentTracker());
            RegisterSystemTracker("Economy", new EconomyTracker());
            RegisterSystemTracker("AI", new AISystemTracker());
        }
        
        public void RegisterSystemTracker(string systemName, SystemPerformanceTracker tracker)
        {
            _systemTrackers[systemName] = tracker;
            tracker.Initialize(systemName);
            Debug.Log($"[PerformanceMonitoringDashboard] Registered tracker for {systemName}");
        }
        
        public void StartMonitoring()
        {
            _isCollecting = true;
            _lastUpdateTime = Time.time;
            _frameCount = 0;
            _frameTimeAccumulator = 0f;
            
            Debug.Log("[PerformanceMonitoringDashboard] Performance monitoring started");
        }
        
        public void StopMonitoring()
        {
            _isCollecting = false;
            Debug.Log("[PerformanceMonitoringDashboard] Performance monitoring stopped");
        }
        
        private void CollectFrameMetrics()
        {
            _frameCount++;
            _frameTimeAccumulator += Time.unscaledDeltaTime * 1000f; // Convert to milliseconds
        }
        
        private void UpdateMetrics()
        {
            var snapshot = CreatePerformanceSnapshot();
            
            // Add to history
            _metricsHistory.Enqueue(snapshot);
            while (_metricsHistory.Count > _maxHistoryPoints)
            {
                _metricsHistory.Dequeue();
            }
            
            // Update current metrics
            UpdateCurrentMetrics(snapshot);
            
            // Check for alerts
            if (_enableAlerts)
            {
                CheckPerformanceAlerts(snapshot);
            }
            
            // Notify listeners
            OnMetricsUpdated?.Invoke(snapshot);
            
            // Reset frame metrics
            _frameCount = 0;
            _frameTimeAccumulator = 0f;
            
            // Send to data pipeline
            if (_dataPipeline != null)
            {
                SendMetricsToDataPipeline(snapshot);
            }
        }
        
        private PerformanceSnapshot CreatePerformanceSnapshot()
        {
            var snapshot = new PerformanceSnapshot
            {
                Timestamp = DateTime.UtcNow,
                FrameRate = _frameCount / _updateInterval,
                FrameTime = _frameCount > 0 ? _frameTimeAccumulator / _frameCount : 0f,
                CPUUsage = GetCPUUsage(),
                MemoryUsage = GetMemoryUsage(),
                GPUMemoryUsage = GetGPUMemoryUsage(),
                DrawCalls = GetDrawCalls(),
                Triangles = GetTriangleCount(),
                ActiveObjects = GetActiveObjectCount(),
                LoadedScenes = UnityEngine.SceneManagement.SceneManager.sceneCount,
                SystemMetrics = CollectSystemMetrics()
            };
            
            return snapshot;
        }
        
        private Dictionary<string, object> CollectSystemMetrics()
        {
            var metrics = new Dictionary<string, object>();
            
            foreach (var tracker in _systemTrackers)
            {
                try
                {
                    var systemMetrics = tracker.Value.CollectMetrics();
                    metrics[tracker.Key] = systemMetrics;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[PerformanceMonitoringDashboard] Failed to collect metrics for {tracker.Key}: {ex.Message}");
                    metrics[tracker.Key] = new { error = ex.Message };
                }
            }
            
            return metrics;
        }
        
        private void UpdateCurrentMetrics(PerformanceSnapshot snapshot)
        {
            _currentMetrics.AverageFrameRate = _metricsHistory.Count > 0 ? 
                _metricsHistory.Average(s => s.FrameRate) : snapshot.FrameRate;
            _currentMetrics.MinFrameRate = _metricsHistory.Count > 0 ? 
                _metricsHistory.Min(s => s.FrameRate) : snapshot.FrameRate;
            _currentMetrics.MaxFrameRate = _metricsHistory.Count > 0 ? 
                _metricsHistory.Max(s => s.FrameRate) : snapshot.FrameRate;
            
            _currentMetrics.AverageFrameTime = _metricsHistory.Count > 0 ? 
                _metricsHistory.Average(s => s.FrameTime) : snapshot.FrameTime;
            _currentMetrics.MaxFrameTime = _metricsHistory.Count > 0 ? 
                _metricsHistory.Max(s => s.FrameTime) : snapshot.FrameTime;
            
            _currentMetrics.CurrentCPUUsage = snapshot.CPUUsage;
            _currentMetrics.CurrentMemoryUsage = snapshot.MemoryUsage;
            _currentMetrics.CurrentGPUMemoryUsage = snapshot.GPUMemoryUsage;
            
            _currentMetrics.LastUpdateTime = snapshot.Timestamp;
        }
        
        private void CheckPerformanceAlerts(PerformanceSnapshot snapshot)
        {
            // CPU usage alert
            if (snapshot.CPUUsage > _alertThresholdCPU)
            {
                TriggerAlert(PerformanceAlertType.HighCPUUsage, $"CPU usage at {snapshot.CPUUsage:F1}%", snapshot.CPUUsage);
            }
            else
            {
                ResolveAlert(PerformanceAlertType.HighCPUUsage);
            }
            
            // Memory usage alert
            if (snapshot.MemoryUsage > _alertThresholdMemory)
            {
                TriggerAlert(PerformanceAlertType.HighMemoryUsage, $"Memory usage at {snapshot.MemoryUsage:F1}%", snapshot.MemoryUsage);
            }
            else
            {
                ResolveAlert(PerformanceAlertType.HighMemoryUsage);
            }
            
            // Frame time alert (low FPS)
            if (snapshot.FrameTime > _alertThresholdFrameTime)
            {
                TriggerAlert(PerformanceAlertType.LowFrameRate, $"Frame time at {snapshot.FrameTime:F2}ms ({(1000f / snapshot.FrameTime):F1} FPS)", snapshot.FrameTime);
            }
            else
            {
                ResolveAlert(PerformanceAlertType.LowFrameRate);
            }
            
            // System-specific alerts
            CheckSystemSpecificAlerts(snapshot);
        }
        
        private void CheckSystemSpecificAlerts(PerformanceSnapshot snapshot)
        {
            foreach (var systemMetric in snapshot.SystemMetrics)
            {
                if (_systemTrackers.TryGetValue(systemMetric.Key, out var tracker))
                {
                    var alerts = tracker.CheckAlerts(systemMetric.Value);
                    foreach (var alert in alerts)
                    {
                        TriggerAlert(alert.Type, alert.Message, alert.Value, systemMetric.Key);
                    }
                }
            }
        }
        
        private void TriggerAlert(PerformanceAlertType type, string message, float value, string system = null)
        {
            var alertId = $"{type}_{system ?? "System"}";
            var existingAlert = _activeAlerts.FirstOrDefault(a => a.AlertId == alertId);
            
            if (existingAlert == null)
            {
                var alert = new PerformanceAlert
                {
                    AlertId = alertId,
                    Type = type,
                    Message = message,
                    Value = value,
                    SystemName = system,
                    FirstDetected = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow,
                    IsActive = true
                };
                
                _activeAlerts.Add(alert);
                OnAlertTriggered?.Invoke(alert);
                Debug.LogWarning($"[PerformanceMonitoringDashboard] Alert: {message}");
            }
            else
            {
                existingAlert.LastUpdated = DateTime.UtcNow;
                existingAlert.Value = value;
            }
        }
        
        private void ResolveAlert(PerformanceAlertType type, string system = null)
        {
            var alertId = $"{type}_{system ?? "System"}";
            var alert = _activeAlerts.FirstOrDefault(a => a.AlertId == alertId);
            
            if (alert != null)
            {
                _activeAlerts.Remove(alert);
                alert.IsActive = false;
                alert.ResolvedAt = DateTime.UtcNow;
                OnAlertResolved?.Invoke(alertId);
            }
        }
        
        private void SendMetricsToDataPipeline(PerformanceSnapshot snapshot)
        {
            var eventData = new
            {
                frame_rate = snapshot.FrameRate,
                frame_time = snapshot.FrameTime,
                cpu_usage = snapshot.CPUUsage,
                memory_usage = snapshot.MemoryUsage,
                gpu_memory = snapshot.GPUMemoryUsage,
                draw_calls = snapshot.DrawCalls,
                triangles = snapshot.Triangles,
                active_objects = snapshot.ActiveObjects
            };
            
            _dataPipeline.CollectEvent("performance_metrics", "system_snapshot", eventData, new Dictionary<string, object>
            {
                ["source"] = "performance_dashboard",
                ["collection_interval"] = _updateInterval,
                ["timestamp"] = snapshot.Timestamp.ToString("O")
            });
        }
        
        public PerformanceMetrics GetCurrentMetrics() => _currentMetrics;
        
        public IReadOnlyList<PerformanceSnapshot> GetMetricsHistory() => _metricsHistory.ToList().AsReadOnly();
        
        public IReadOnlyList<PerformanceAlert> GetActiveAlerts() => _activeAlerts.AsReadOnly();
        
        public Dictionary<string, object> GetSystemMetrics(string systemName)
        {
            if (_systemTrackers.TryGetValue(systemName, out var tracker))
            {
                return tracker.CollectMetrics();
            }
            return null;
        }
        
        // Unity system metrics collection
        private float GetCPUUsage()
        {
            // Unity doesn't provide direct CPU usage, so we estimate based on frame time
            var targetFrameTime = 1000f / Application.targetFrameRate;
            var actualFrameTime = _frameCount > 0 ? _frameTimeAccumulator / _frameCount : 0f;
            return Mathf.Clamp01(actualFrameTime / targetFrameTime) * 100f;
        }
        
        private float GetMemoryUsage()
        {
            var totalMemory = SystemInfo.systemMemorySize;
            var usedMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory() / (1024 * 1024); // Convert to MB
            return totalMemory > 0 ? (float)usedMemory / totalMemory * 100f : 0f;
        }
        
        private long GetGPUMemoryUsage()
        {
            return SystemInfo.graphicsMemorySize;
        }
        
        private int GetDrawCalls()
        {
            // Would need to integrate with Unity's frame debugger or rendering stats
            return 0; // Placeholder
        }
        
        private long GetTriangleCount()
        {
            // Would need to integrate with Unity's rendering stats
            return 0; // Placeholder
        }
        
        private int GetActiveObjectCount()
        {
            return UnityEngine.Object.FindObjectsOfType<GameObject>().Length;
        }
        
        public void SetUpdateInterval(float interval)
        {
            _updateInterval = Mathf.Max(0.1f, interval);
        }
        
        public void SetMaxHistoryPoints(int maxPoints)
        {
            _maxHistoryPoints = Mathf.Max(10, maxPoints);
        }
        
        public void EnableRealTimeUpdates(bool enable)
        {
            _enableRealTimeUpdates = enable;
            if (!enable)
            {
                StopMonitoring();
            }
            else if (!_isCollecting)
            {
                StartMonitoring();
            }
        }
        
        public void ClearHistory()
        {
            _metricsHistory.Clear();
            Debug.Log("[PerformanceMonitoringDashboard] Metrics history cleared");
        }
        
        public async Task GeneratePerformanceReport()
        {
            if (_analytics != null)
            {
                var reportData = new
                {
                    current_metrics = _currentMetrics,
                    history_points = _metricsHistory.Count,
                    active_alerts = _activeAlerts.Count,
                    system_trackers = _systemTrackers.Keys.ToList(),
                    collection_interval = _updateInterval,
                    timestamp = DateTime.UtcNow
                };
                
                _analytics?.CollectEvent("performance_report", "dashboard_report", reportData);
            }
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                StopMonitoring();
            }
            else if (_enableRealTimeUpdates)
            {
                StartMonitoring();
            }
        }
        
        private void OnDestroy()
        {
            StopMonitoring();
        }
    }
    
    /// <summary>
    /// Performance metrics summary
    /// </summary>
    [System.Serializable]
    public class PerformanceMetrics
    {
        public float AverageFrameRate;
        public float MinFrameRate;
        public float MaxFrameRate;
        public float AverageFrameTime;
        public float MaxFrameTime;
        public float CurrentCPUUsage;
        public float CurrentMemoryUsage;
        public long CurrentGPUMemoryUsage;
        public DateTime LastUpdateTime;
    }
    
    /// <summary>
    /// Performance snapshot at a specific point in time
    /// </summary>
    [System.Serializable]
    public class PerformanceSnapshot
    {
        public DateTime Timestamp;
        public float FrameRate;
        public float FrameTime;
        public float CPUUsage;
        public float MemoryUsage;
        public long GPUMemoryUsage;
        public int DrawCalls;
        public long Triangles;
        public int ActiveObjects;
        public int LoadedScenes;
        public Dictionary<string, object> SystemMetrics;
        
        public PerformanceSnapshot()
        {
            SystemMetrics = new Dictionary<string, object>();
        }
    }
    
    /// <summary>
    /// Performance alert
    /// </summary>
    [System.Serializable]
    public class PerformanceAlert
    {
        public string AlertId;
        public PerformanceAlertType Type;
        public string Message;
        public float Value;
        public string SystemName;
        public DateTime FirstDetected;
        public DateTime LastUpdated;
        public DateTime ResolvedAt;
        public bool IsActive;
        
        public TimeSpan Duration => IsActive ? DateTime.UtcNow - FirstDetected : ResolvedAt - FirstDetected;
    }
    
    /// <summary>
    /// Performance monitoring alert types
    /// </summary>
    public enum PerformanceAlertType
    {
        HighCPUUsage,
        HighMemoryUsage,
        LowFrameRate,
        HighDrawCalls,
        LowGPUMemory,
        SystemError,
        NetworkLatency,
        DiskSpace,
        CustomMetric
    }
    
    /// <summary>
    /// Base class for system performance trackers
    /// </summary>
    public abstract class SystemPerformanceTracker
    {
        protected string SystemName { get; private set; }
        protected bool IsInitialized { get; private set; }
        
        public virtual void Initialize(string systemName)
        {
            SystemName = systemName;
            IsInitialized = true;
        }
        
        public abstract Dictionary<string, object> CollectMetrics();
        
        public virtual List<PerformanceAlert> CheckAlerts(object metrics)
        {
            return new List<PerformanceAlert>();
        }
    }
    
    /// <summary>
    /// Unity system tracker
    /// </summary>
    public class UnitySystemTracker : SystemPerformanceTracker
    {
        public override Dictionary<string, object> CollectMetrics()
        {
            return new Dictionary<string, object>
            {
                ["unity_version"] = Application.unityVersion,
                ["platform"] = Application.platform.ToString(),
                ["target_frame_rate"] = Application.targetFrameRate,
                ["is_focused"] = Application.isFocused,
                ["background_behavior"] = Application.runInBackground,
                ["time_scale"] = Time.timeScale,
                ["real_time"] = Time.realtimeSinceStartup
            };
        }
    }
    
    /// <summary>
    /// Rendering system tracker
    /// </summary>
    public class RenderingTracker : SystemPerformanceTracker
    {
        public override Dictionary<string, object> CollectMetrics()
        {
            return new Dictionary<string, object>
            {
                ["graphics_device"] = SystemInfo.graphicsDeviceName,
                ["graphics_memory"] = SystemInfo.graphicsMemorySize,
                ["max_texture_size"] = SystemInfo.maxTextureSize,
                ["shader_level"] = SystemInfo.graphicsShaderLevel,
                ["render_pipeline"] = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline?.name ?? "Built-in"
            };
        }
    }
    
    /// <summary>
    /// Physics system tracker
    /// </summary>
    public class PhysicsTracker : SystemPerformanceTracker
    {
        public override Dictionary<string, object> CollectMetrics()
        {
            return new Dictionary<string, object>
            {
                ["fixed_timestep"] = Time.fixedDeltaTime,
                ["gravity"] = Physics.gravity,
                ["default_solver_iterations"] = Physics.defaultSolverIterations,
                ["default_solver_velocity_iterations"] = Physics.defaultSolverVelocityIterations,
                ["rigidbodies"] = UnityEngine.Object.FindObjectsOfType<Rigidbody>().Length,
                ["colliders"] = UnityEngine.Object.FindObjectsOfType<Collider>().Length
            };
        }
    }
    
    /// <summary>
    /// Audio system tracker
    /// </summary>
    public class AudioTracker : SystemPerformanceTracker
    {
        public override Dictionary<string, object> CollectMetrics()
        {
            var audioConfig = AudioSettings.GetConfiguration();
            return new Dictionary<string, object>
            {
                ["sample_rate"] = audioConfig.sampleRate,
                ["buffer_size"] = audioConfig.dspBufferSize,
                ["speaker_mode"] = audioConfig.speakerMode.ToString(),
                ["audio_sources"] = UnityEngine.Object.FindObjectsOfType<AudioSource>().Length,
                ["audio_listeners"] = UnityEngine.Object.FindObjectsOfType<AudioListener>().Length
            };
        }
    }
    
    /// <summary>
    /// Input system tracker
    /// </summary>
    public class InputTracker : SystemPerformanceTracker
    {
        public override Dictionary<string, object> CollectMetrics()
        {
            return new Dictionary<string, object>
            {
                ["input_string"] = Input.inputString,
                ["mouse_present"] = Input.mousePresent,
                ["touch_supported"] = Input.touchSupported,
                ["touch_count"] = Input.touchCount,
                ["device_orientation"] = Input.deviceOrientation.ToString()
            };
        }
    }
    
    /// <summary>
    /// Networking system tracker
    /// </summary>
    public class NetworkingTracker : SystemPerformanceTracker
    {
        public override Dictionary<string, object> CollectMetrics()
        {
            return new Dictionary<string, object>
            {
                ["internet_reachability"] = Application.internetReachability.ToString(),
                ["network_time"] = Time.time, // Use Unity Time instead of deprecated Network.time
                ["is_client"] = false, // Default values since Unity Networking is deprecated
                ["is_server"] = false,
                ["connections"] = 0
            };
        }
    }
    
    // Game-specific trackers
    
    /// <summary>
    /// Plant system tracker
    /// </summary>
    public class PlantSystemTracker : SystemPerformanceTracker
    {
        public override Dictionary<string, object> CollectMetrics()
        {
            // Would integrate with actual plant system
            return new Dictionary<string, object>
            {
                ["total_plants"] = 0,
                ["active_plants"] = 0,
                ["growth_calculations_per_frame"] = 0,
                ["average_plant_complexity"] = 0
            };
        }
    }
    
    /// <summary>
    /// Genetics system tracker
    /// </summary>
    public class GeneticsTracker : SystemPerformanceTracker
    {
        public override Dictionary<string, object> CollectMetrics()
        {
            return new Dictionary<string, object>
            {
                ["genetic_calculations"] = 0,
                ["breeding_operations"] = 0,
                ["trait_calculations"] = 0
            };
        }
    }
    
    /// <summary>
    /// Environment system tracker
    /// </summary>
    public class EnvironmentTracker : SystemPerformanceTracker
    {
        public override Dictionary<string, object> CollectMetrics()
        {
            return new Dictionary<string, object>
            {
                ["climate_zones"] = 0,
                ["environmental_updates"] = 0,
                ["sensor_readings"] = 0
            };
        }
    }
    
    /// <summary>
    /// Economy system tracker
    /// </summary>
    public class EconomyTracker : SystemPerformanceTracker
    {
        public override Dictionary<string, object> CollectMetrics()
        {
            return new Dictionary<string, object>
            {
                ["market_calculations"] = 0,
                ["price_updates"] = 0,
                ["transaction_processing"] = 0
            };
        }
    }
    
    /// <summary>
    /// AI system tracker
    /// </summary>
    public class AISystemTracker : SystemPerformanceTracker
    {
        public override Dictionary<string, object> CollectMetrics()
        {
            return new Dictionary<string, object>
            {
                ["ai_decisions"] = 0,
                ["pathfinding_calls"] = 0,
                ["behavior_tree_updates"] = 0
            };
        }
    }
}