using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;
using ProjectChimera.Systems.Analytics;

namespace ProjectChimera.Systems.Diagnostics
{
    /// <summary>
    /// Development-specific monitoring system for Project Chimera
    /// Provides custom performance profiling and debug overlays for development builds
    /// Integrates with Unity 6.2 diagnostics while adding Project Chimera-specific insights
    /// </summary>
    public class DevelopmentMonitoring : MonoBehaviour
    {
        [Header("Development Monitoring Configuration")]
        [SerializeField] private bool _enableDevelopmentMonitoring = true;
        [SerializeField] private bool _enableDebugOverlays = true;
        [SerializeField] private bool _enablePerformanceProfiling = true;
        [SerializeField] private bool _enableMemoryTracking = true;
        [SerializeField] private bool _enableSystemHealthMonitoring = true;
        
        [Header("Debug Overlay Settings")]
        [SerializeField] private KeyCode _toggleOverlayKey = KeyCode.F12;
        [SerializeField] private bool _showFPSCounter = true;
        [SerializeField] private bool _showMemoryUsage = true;
        [SerializeField] private bool _showSystemStatus = true;
        [SerializeField] private bool _showCultivationMetrics = true;
        [SerializeField] private bool _showConstructionMetrics = true;
        
        [Header("Performance Profiling Settings")]
        [SerializeField] private float _profilingInterval = 1f;
        [SerializeField] private int _maxProfileSamples = 300;
        [SerializeField] private bool _enableGPUProfiling = true;
        [SerializeField] private bool _enableDetailedMemoryProfiling = true;
        
        [Header("Alert Thresholds")]
        [SerializeField] private float _fpsWarningThreshold = 30f;
        [SerializeField] private float _fpsCriticalThreshold = 15f;
        [SerializeField] private long _memoryWarningThreshold = 1024 * 1024 * 512; // 512MB
        [SerializeField] private long _memoryCriticalThreshold = 1024 * 1024 * 1024; // 1GB
        
        // Core Systems
        private Unity62DiagnosticsIntegration _unity62Diagnostics;
        private DataPipelineIntegration _dataPipeline;
        private AdvancedAnalytics _analytics;
        
        // Monitoring State
        private bool _overlayVisible = false;
        private readonly List<PerformanceProfileSample> _performanceSamples = new List<PerformanceProfileSample>();
        private readonly Dictionary<string, SystemHealthData> _systemHealth = new Dictionary<string, SystemHealthData>();
        private float _lastProfilingTime = 0f;
        
        // GUI State
        private Rect _overlayRect = new Rect(10, 10, 400, 600);
        private Vector2 _scrollPosition = Vector2.zero;
        private GUIStyle _headerStyle;
        private GUIStyle _dataStyle;
        private GUIStyle _warningStyle;
        private GUIStyle _criticalStyle;
        
        // Performance Tracking
        private readonly Dictionary<string, float> _systemPerformanceMetrics = new Dictionary<string, float>();
        private readonly Dictionary<string, long> _memoryAllocations = new Dictionary<string, long>();
        private readonly List<string> _recentAlerts = new List<string>();
        
        // Events
        public event Action<PerformanceAlert> OnPerformanceAlert;
        public event Action<string, SystemHealthData> OnSystemHealthChanged;
        public event Action<MemoryProfileData> OnMemoryProfileUpdated;
        
        private void Awake()
        {
            InitializeDevelopmentMonitoring();
        }
        
        private void Start()
        {
            FindSystemReferences();
            SetupGUIStyles();
            
            if (_enableDevelopmentMonitoring)
            {
                StartMonitoring();
            }
        }
        
        private void Update()
        {
            if (!_enableDevelopmentMonitoring) return;
            
            HandleInput();
            
            if (Time.time - _lastProfilingTime >= _profilingInterval)
            {
                CollectPerformanceData();
                _lastProfilingTime = Time.time;
            }
        }
        
        private void OnGUI()
        {
            if (_overlayVisible && _enableDebugOverlays)
            {
                DrawDebugOverlay();
            }
        }
        
        private void InitializeDevelopmentMonitoring()
        {
            // Only enable in development builds
#if !DEVELOPMENT_BUILD && !UNITY_EDITOR
            _enableDevelopmentMonitoring = false;
            return;
#endif
            
            Debug.Log("[DevelopmentMonitoring] Development monitoring system initialized");
        }
        
        private void FindSystemReferences()
        {
            _unity62Diagnostics = UnityEngine.Object.FindObjectOfType<Unity62DiagnosticsIntegration>();
            _dataPipeline = UnityEngine.Object.FindObjectOfType<DataPipelineIntegration>();
            _analytics = UnityEngine.Object.FindObjectOfType<AdvancedAnalytics>();
        }
        
        private void SetupGUIStyles()
        {
            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.cyan }
            };
            
            _dataStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = Color.white }
            };
            
            _warningStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = Color.yellow }
            };
            
            _criticalStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = Color.red }
            };
        }
        
        private void StartMonitoring()
        {
            // Initialize system health tracking
            InitializeSystemHealthTracking();
            
            // Start profiling if enabled
            if (_enablePerformanceProfiling)
            {
                StartPerformanceProfiling();
            }
            
            // Start memory tracking if enabled
            if (_enableMemoryTracking)
            {
                StartMemoryTracking();
            }
            
            Debug.Log("[DevelopmentMonitoring] Development monitoring started");
        }
        
        private void InitializeSystemHealthTracking()
        {
            var systems = new[]
            {
                "CultivationManager",
                "ConstructionManager", 
                "EconomyManager",
                "GeneticsManager",
                "TimeManager",
                "SaveManager",
                "GridSystem",
                "UIManager"
            };
            
            foreach (var system in systems)
            {
                _systemHealth[system] = new SystemHealthData
                {
                    SystemName = system,
                    Status = SystemStatus.Unknown,
                    LastCheck = DateTime.UtcNow,
                    PerformanceMetric = 0f
                };
            }
        }
        
        private void StartPerformanceProfiling()
        {
            Debug.Log("[DevelopmentMonitoring] Performance profiling started");
        }
        
        private void StartMemoryTracking()
        {
            Debug.Log("[DevelopmentMonitoring] Memory tracking started");
        }
        
        private void HandleInput()
        {
            if (Input.GetKeyDown(_toggleOverlayKey))
            {
                _overlayVisible = !_overlayVisible;
            }
        }
        
        private void CollectPerformanceData()
        {
            var sample = new PerformanceProfileSample
            {
                Timestamp = DateTime.UtcNow,
                FrameRate = 1f / Time.unscaledDeltaTime,
                FrameTime = Time.unscaledDeltaTime * 1000f,
                MemoryUsage = Profiler.GetTotalAllocatedMemory(),
                ReservedMemory = Profiler.GetTotalReservedMemory(),
                MonoUsedSize = Profiler.GetMonoUsedSize(),
                MonoHeapSize = Profiler.GetMonoHeapSize(),
                ActiveObjects = UnityEngine.Object.FindObjectsOfType<GameObject>().Length,
                DrawCalls = 0, // Would need Graphics API integration
                Triangles = 0, // Would need Graphics API integration
                SetPassCalls = 0 // Would need Graphics API integration
            };
            
            // Collect Project Chimera specific metrics
            CollectCultivationMetrics(sample);
            CollectConstructionMetrics(sample);
            CollectMemoryMetrics(sample);
            
            _performanceSamples.Add(sample);
            
            // Keep only recent samples
            if (_performanceSamples.Count > _maxProfileSamples)
            {
                _performanceSamples.RemoveAt(0);
            }
            
            // Check for performance alerts
            CheckPerformanceAlerts(sample);
            
            // Update system health
            UpdateSystemHealth(sample);
            
            // Send to Unity 6.2 diagnostics if available
            if (_unity62Diagnostics != null)
            {
                _ = SendSampleToUnityDiagnosticsAsync(sample);
            }
        }
        
        private void CollectCultivationMetrics(PerformanceProfileSample sample)
        {
            // Find cultivation-related objects
            var plants = UnityEngine.Object.FindObjectsOfType<GameObject>()
                .Where(go => go.name.Contains("Plant") || go.tag == "Plant").ToArray();
            
            sample.CultivationMetrics = new CultivationMetrics
            {
                ActivePlants = plants.Length,
                PlantsPerSecond = plants.Length > 0 ? sample.FrameRate / plants.Length : 0f,
                CultivationMemoryUsage = EstimateCultivationMemoryUsage(plants)
            };
            
            _systemPerformanceMetrics["cultivation_plants"] = plants.Length;
            _systemPerformanceMetrics["cultivation_fps_impact"] = sample.CultivationMetrics.PlantsPerSecond;
        }
        
        private void CollectConstructionMetrics(PerformanceProfileSample sample)
        {
            // Find construction-related objects
            var buildings = UnityEngine.Object.FindObjectsOfType<GameObject>()
                .Where(go => go.name.Contains("Building") || go.name.Contains("Facility")).ToArray();
            
            sample.ConstructionMetrics = new ConstructionMetrics
            {
                ActiveBuildings = buildings.Length,
                BuildingsPerSecond = buildings.Length > 0 ? sample.FrameRate / buildings.Length : 0f,
                ConstructionMemoryUsage = EstimateConstructionMemoryUsage(buildings)
            };
            
            _systemPerformanceMetrics["construction_buildings"] = buildings.Length;
            _systemPerformanceMetrics["construction_fps_impact"] = sample.ConstructionMetrics.BuildingsPerSecond;
        }
        
        private void CollectMemoryMetrics(PerformanceProfileSample sample)
        {
            if (!_enableDetailedMemoryProfiling) return;
            
            // Collect detailed memory allocation data
            var memoryProfile = new MemoryProfileData
            {
                Timestamp = sample.Timestamp,
                TotalAllocated = sample.MemoryUsage,
                TotalReserved = sample.ReservedMemory,
                MonoUsed = sample.MonoUsedSize,
                MonoHeap = sample.MonoHeapSize,
                NativeAllocations = sample.MemoryUsage - sample.MonoUsedSize,
                GfxDriverAllocated = Profiler.GetAllocatedMemoryForGraphicsDriver(),
                TextureMemory = 0, // Would need Texture API integration
                MeshMemory = 0, // Would need Mesh API integration
                AudioMemory = 0 // Would need Audio API integration
            };
            
            sample.MemoryProfile = memoryProfile;
            OnMemoryProfileUpdated?.Invoke(memoryProfile);
        }
        
        private long EstimateCultivationMemoryUsage(GameObject[] plants)
        {
            // Rough estimation of memory usage for cultivation objects
            return plants.Length * 1024 * 10; // ~10KB per plant estimate
        }
        
        private long EstimateConstructionMemoryUsage(GameObject[] buildings)
        {
            // Rough estimation of memory usage for construction objects
            return buildings.Length * 1024 * 50; // ~50KB per building estimate
        }
        
        private void CheckPerformanceAlerts(PerformanceProfileSample sample)
        {
            // FPS alerts
            if (sample.FrameRate < _fpsCriticalThreshold)
            {
                TriggerPerformanceAlert(AlertLevel.Critical, $"Critical FPS: {sample.FrameRate:F1}", sample);
            }
            else if (sample.FrameRate < _fpsWarningThreshold)
            {
                TriggerPerformanceAlert(AlertLevel.Warning, $"Low FPS: {sample.FrameRate:F1}", sample);
            }
            
            // Memory alerts
            if (sample.MemoryUsage > _memoryCriticalThreshold)
            {
                TriggerPerformanceAlert(AlertLevel.Critical, $"Critical Memory: {sample.MemoryUsage / (1024*1024):F0}MB", sample);
            }
            else if (sample.MemoryUsage > _memoryWarningThreshold)
            {
                TriggerPerformanceAlert(AlertLevel.Warning, $"High Memory: {sample.MemoryUsage / (1024*1024):F0}MB", sample);
            }
            
            // Frame time alerts
            if (sample.FrameTime > 33.33f) // Longer than 30 FPS
            {
                TriggerPerformanceAlert(AlertLevel.Warning, $"Long Frame Time: {sample.FrameTime:F1}ms", sample);
            }
        }
        
        private void TriggerPerformanceAlert(AlertLevel level, string message, PerformanceProfileSample sample)
        {
            var alert = new PerformanceAlert
            {
                Level = level,
                Message = message,
                Timestamp = sample.Timestamp,
                FrameRate = sample.FrameRate,
                MemoryUsage = sample.MemoryUsage,
                FrameTime = sample.FrameTime
            };
            
            OnPerformanceAlert?.Invoke(alert);
            
            _recentAlerts.Add($"[{level}] {message} at {sample.Timestamp:HH:mm:ss}");
            
            // Keep only recent alerts
            if (_recentAlerts.Count > 20)
            {
                _recentAlerts.RemoveAt(0);
            }
            
            Debug.LogWarning($"[DevelopmentMonitoring] Performance Alert: {message}");
        }
        
        private void UpdateSystemHealth(PerformanceProfileSample sample)
        {
            // Update system health based on performance metrics
            foreach (var system in _systemHealth.Keys.ToList())
            {
                var health = _systemHealth[system];
                health.LastCheck = sample.Timestamp;
                health.PerformanceMetric = CalculateSystemPerformanceScore(system, sample);
                
                var previousStatus = health.Status;
                health.Status = DetermineSystemStatus(health.PerformanceMetric);
                
                if (health.Status != previousStatus)
                {
                    OnSystemHealthChanged?.Invoke(system, health);
                }
                
                _systemHealth[system] = health;
            }
        }
        
        private float CalculateSystemPerformanceScore(string systemName, PerformanceProfileSample sample)
        {
            // Calculate a performance score (0-1) for the system
            var baseScore = Mathf.Clamp01(sample.FrameRate / 60f);
            var memoryScore = Mathf.Clamp01(1f - (float)sample.MemoryUsage / _memoryCriticalThreshold);
            
            // Adjust based on system-specific metrics
            switch (systemName.ToLower())
            {
                case "cultivationmanager":
                    if (sample.CultivationMetrics != null)
                    {
                        var plantImpact = sample.CultivationMetrics.ActivePlants > 100 ? 0.8f : 1f;
                        return (baseScore + memoryScore) * 0.5f * plantImpact;
                    }
                    break;
                    
                case "constructionmanager":
                    if (sample.ConstructionMetrics != null)
                    {
                        var buildingImpact = sample.ConstructionMetrics.ActiveBuildings > 50 ? 0.8f : 1f;
                        return (baseScore + memoryScore) * 0.5f * buildingImpact;
                    }
                    break;
            }
            
            return (baseScore + memoryScore) * 0.5f;
        }
        
        private SystemStatus DetermineSystemStatus(float performanceScore)
        {
            if (performanceScore >= 0.8f) return SystemStatus.Healthy;
            if (performanceScore >= 0.6f) return SystemStatus.Warning;
            if (performanceScore >= 0.3f) return SystemStatus.Critical;
            return SystemStatus.Failed;
        }
        
        private async Task SendSampleToUnityDiagnosticsAsync(PerformanceProfileSample sample)
        {
            var diagnosticsData = new Dictionary<string, object>
            {
                ["dev_frame_rate"] = sample.FrameRate,
                ["dev_frame_time"] = sample.FrameTime,
                ["dev_memory_usage"] = sample.MemoryUsage,
                ["dev_active_objects"] = sample.ActiveObjects,
                ["dev_cultivation_plants"] = sample.CultivationMetrics?.ActivePlants ?? 0,
                ["dev_construction_buildings"] = sample.ConstructionMetrics?.ActiveBuildings ?? 0
            };
            
            await _unity62Diagnostics.SendCustomEventAsync("dev_performance_sample", diagnosticsData);
        }
        
        private void DrawDebugOverlay()
        {
            GUI.backgroundColor = new Color(0f, 0f, 0f, 0.8f);
            _overlayRect = GUI.Window(12345, _overlayRect, DrawOverlayContent, "Development Monitoring");
        }
        
        private void DrawOverlayContent(int windowID)
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            
            // Header
            GUILayout.Label("Project Chimera - Development Monitor", _headerStyle);
            GUILayout.Space(10);
            
            // Performance section
            if (_showFPSCounter)
            {
                DrawPerformanceSection();
            }
            
            // Memory section
            if (_showMemoryUsage)
            {
                DrawMemorySection();
            }
            
            // System health section
            if (_showSystemStatus)
            {
                DrawSystemHealthSection();
            }
            
            // Cultivation metrics section
            if (_showCultivationMetrics)
            {
                DrawCultivationSection();
            }
            
            // Construction metrics section
            if (_showConstructionMetrics)
            {
                DrawConstructionSection();
            }
            
            // Recent alerts section
            DrawAlertsSection();
            
            GUILayout.EndScrollView();
            
            GUI.DragWindow();
        }
        
        private void DrawPerformanceSection()
        {
            GUILayout.Label("Performance", _headerStyle);
            
            if (_performanceSamples.Count > 0)
            {
                var latest = _performanceSamples.Last();
                var style = latest.FrameRate < _fpsWarningThreshold ? _warningStyle : _dataStyle;
                
                GUILayout.Label($"FPS: {latest.FrameRate:F1}", style);
                GUILayout.Label($"Frame Time: {latest.FrameTime:F2}ms", _dataStyle);
                GUILayout.Label($"Active Objects: {latest.ActiveObjects}", _dataStyle);
                
                // Performance graph (simplified)
                var recentSamples = _performanceSamples.TakeLast(60).ToList();
                var avgFPS = recentSamples.Average(s => s.FrameRate);
                var minFPS = recentSamples.Min(s => s.FrameRate);
                var maxFPS = recentSamples.Max(s => s.FrameRate);
                
                GUILayout.Label($"Avg FPS (60s): {avgFPS:F1}", _dataStyle);
                GUILayout.Label($"Min/Max FPS: {minFPS:F1} / {maxFPS:F1}", _dataStyle);
            }
            
            GUILayout.Space(10);
        }
        
        private void DrawMemorySection()
        {
            GUILayout.Label("Memory", _headerStyle);
            
            if (_performanceSamples.Count > 0)
            {
                var latest = _performanceSamples.Last();
                var memoryMB = latest.MemoryUsage / (1024 * 1024);
                var style = latest.MemoryUsage > _memoryWarningThreshold ? _warningStyle : _dataStyle;
                
                GUILayout.Label($"Total Allocated: {memoryMB:F0}MB", style);
                GUILayout.Label($"Reserved: {latest.ReservedMemory / (1024 * 1024):F0}MB", _dataStyle);
                GUILayout.Label($"Mono Used: {latest.MonoUsedSize / (1024 * 1024):F0}MB", _dataStyle);
                GUILayout.Label($"Mono Heap: {latest.MonoHeapSize / (1024 * 1024):F0}MB", _dataStyle);
                
                if (latest.MemoryProfile != null)
                {
                    GUILayout.Label($"GFX Driver: {latest.MemoryProfile.GfxDriverAllocated / (1024 * 1024):F0}MB", _dataStyle);
                }
            }
            
            GUILayout.Space(10);
        }
        
        private void DrawSystemHealthSection()
        {
            GUILayout.Label("System Health", _headerStyle);
            
            foreach (var system in _systemHealth)
            {
                var style = system.Value.Status == SystemStatus.Healthy ? _dataStyle :
                           system.Value.Status == SystemStatus.Warning ? _warningStyle : _criticalStyle;
                
                GUILayout.Label($"{system.Key}: {system.Value.Status} ({system.Value.PerformanceMetric:F2})", style);
            }
            
            GUILayout.Space(10);
        }
        
        private void DrawCultivationSection()
        {
            GUILayout.Label("Cultivation Metrics", _headerStyle);
            
            if (_performanceSamples.Count > 0)
            {
                var latest = _performanceSamples.Last();
                if (latest.CultivationMetrics != null)
                {
                    GUILayout.Label($"Active Plants: {latest.CultivationMetrics.ActivePlants}", _dataStyle);
                    GUILayout.Label($"Plants/FPS Impact: {latest.CultivationMetrics.PlantsPerSecond:F3}", _dataStyle);
                    GUILayout.Label($"Cultivation Memory: {latest.CultivationMetrics.CultivationMemoryUsage / 1024:F0}KB", _dataStyle);
                }
            }
            
            GUILayout.Space(10);
        }
        
        private void DrawConstructionSection()
        {
            GUILayout.Label("Construction Metrics", _headerStyle);
            
            if (_performanceSamples.Count > 0)
            {
                var latest = _performanceSamples.Last();
                if (latest.ConstructionMetrics != null)
                {
                    GUILayout.Label($"Active Buildings: {latest.ConstructionMetrics.ActiveBuildings}", _dataStyle);
                    GUILayout.Label($"Buildings/FPS Impact: {latest.ConstructionMetrics.BuildingsPerSecond:F3}", _dataStyle);
                    GUILayout.Label($"Construction Memory: {latest.ConstructionMetrics.ConstructionMemoryUsage / 1024:F0}KB", _dataStyle);
                }
            }
            
            GUILayout.Space(10);
        }
        
        private void DrawAlertsSection()
        {
            GUILayout.Label("Recent Alerts", _headerStyle);
            
            if (_recentAlerts.Count == 0)
            {
                GUILayout.Label("No recent alerts", _dataStyle);
            }
            else
            {
                foreach (var alert in _recentAlerts.TakeLast(10))
                {
                    var style = alert.Contains("[Critical]") ? _criticalStyle :
                               alert.Contains("[Warning]") ? _warningStyle : _dataStyle;
                    GUILayout.Label(alert, style);
                }
            }
        }
        
        public List<PerformanceProfileSample> GetPerformanceSamples(int maxCount = 100)
        {
            return _performanceSamples.TakeLast(maxCount).ToList();
        }
        
        public Dictionary<string, SystemHealthData> GetSystemHealth()
        {
            return new Dictionary<string, SystemHealthData>(_systemHealth);
        }
        
        public void SetOverlayVisibility(bool visible)
        {
            _overlayVisible = visible;
        }
        
        public void ClearPerformanceData()
        {
            _performanceSamples.Clear();
            _recentAlerts.Clear();
            Debug.Log("[DevelopmentMonitoring] Performance data cleared");
        }
        
        private void OnDestroy()
        {
            if (_enableDevelopmentMonitoring)
            {
                Debug.Log("[DevelopmentMonitoring] Development monitoring stopped");
            }
        }
    }
    
    // Data Structures
    
    [System.Serializable]
    public class PerformanceProfileSample
    {
        public DateTime Timestamp;
        public float FrameRate;
        public float FrameTime;
        public long MemoryUsage;
        public long ReservedMemory;
        public long MonoUsedSize;
        public long MonoHeapSize;
        public int ActiveObjects;
        public int DrawCalls;
        public long Triangles;
        public int SetPassCalls;
        public CultivationMetrics CultivationMetrics;
        public ConstructionMetrics ConstructionMetrics;
        public MemoryProfileData MemoryProfile;
    }
    
    [System.Serializable]
    public class CultivationMetrics
    {
        public int ActivePlants;
        public float PlantsPerSecond;
        public long CultivationMemoryUsage;
    }
    
    [System.Serializable]
    public class ConstructionMetrics
    {
        public int ActiveBuildings;
        public float BuildingsPerSecond;
        public long ConstructionMemoryUsage;
    }
    
    [System.Serializable]
    public class MemoryProfileData
    {
        public DateTime Timestamp;
        public long TotalAllocated;
        public long TotalReserved;
        public long MonoUsed;
        public long MonoHeap;
        public long NativeAllocations;
        public long GfxDriverAllocated;
        public long TextureMemory;
        public long MeshMemory;
        public long AudioMemory;
    }
    
    [System.Serializable]
    public class SystemHealthData
    {
        public string SystemName;
        public SystemStatus Status;
        public DateTime LastCheck;
        public float PerformanceMetric;
    }
    
    [System.Serializable]
    public class PerformanceAlert
    {
        public AlertLevel Level;
        public string Message;
        public DateTime Timestamp;
        public float FrameRate;
        public long MemoryUsage;
        public float FrameTime;
    }
    
    public enum SystemStatus
    {
        Unknown,
        Healthy,
        Warning,
        Critical,
        Failed
    }
    
    public enum AlertLevel
    {
        Info,
        Warning,
        Critical
    }
}