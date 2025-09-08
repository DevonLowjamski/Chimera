using ProjectChimera.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Systems.Services.Core;
using ProjectChimera.Systems.Analytics;

namespace ProjectChimera.Systems.Progression
{
    /// <summary>
    /// Core architecture for handling offline progression in Project Chimera
    /// Coordinates offline calculations across all game systems
    /// </summary>
    public class OfflineProgressionArchitecture : MonoBehaviour
    {
        [Header("Offline Progression Configuration")]
        [SerializeField] private bool _enableOfflineProgression = true;
        [SerializeField] private float _maxOfflineHours = 168f; // 7 days maximum
        [SerializeField] private float _minOfflineMinutes = 5f; // Minimum offline time to trigger progression
        [SerializeField] private bool _enableAcceleratedProgression = true;
        [SerializeField] private float _accelerationMultiplier = 1.5f; // Bonus for being offline
        [SerializeField] private bool _requireInternetConnection = false;

        [Header("Processing Settings")]
        [SerializeField] private int _maxProcessingSteps = 100;
        [SerializeField] private float _processingTimeLimit = 10f; // Max seconds for offline calculation
        [SerializeField] private bool _enableProgressionLogging = true;
        [SerializeField] private bool _enableProgressionValidation = true;

        // Core Systems
        private DataPipelineIntegration _dataPipeline;
        private AdvancedAnalytics _analytics;
        private ProjectChimera.Systems.Services.Core.ServiceLayerCoordinator _serviceCoordinator;

        // Progression Management
        private readonly Dictionary<string, IOfflineProgressionProvider> _progressionProviders = new Dictionary<string, IOfflineProgressionProvider>();
        private readonly List<OfflineProgressionResult> _lastProgressionResults = new List<OfflineProgressionResult>();
        private readonly Dictionary<string, float> _providerWeights = new Dictionary<string, float>();

        // State Management
        private DateTime _lastOnlineTime;
        private bool _isProcessingOfflineProgression = false;
        private OfflineProgressionSession _currentSession;
        private OfflineProgressionMetrics _sessionMetrics;

        // Events
        public event Action<OfflineProgressionSession> OnOfflineProgressionStarted;
        public event Action<OfflineProgressionResult> OnProviderProgressionCalculated;
        public event Action<OfflineProgressionSession> OnOfflineProgressionCompleted;
        public event Action<string> OnOfflineProgressionError;

        private void Awake()
        {
            InitializeOfflineProgression();
        }

        private void Start()
        {
            FindSystemReferences();
            RegisterDefaultProviders();
            LoadLastOnlineTime();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveLastOnlineTime();
            }
            else
            {
                CheckForOfflineProgression();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                CheckForOfflineProgression();
            }
            else
            {
                SaveLastOnlineTime();
            }
        }

        private void InitializeOfflineProgression()
        {
            _sessionMetrics = new OfflineProgressionMetrics();
            ChimeraLogger.Log("[OfflineProgressionArchitecture] Offline progression system initialized");
        }

        private void FindSystemReferences()
        {
            _dataPipeline = ServiceContainerFactory.Instance?.TryResolve<DataPipelineIntegration>();
            _analytics = ServiceContainerFactory.Instance?.TryResolve<AdvancedAnalytics>();
            _serviceCoordinator = ServiceContainerFactory.Instance?.TryResolve<ProjectChimera.Systems.Services.Core.ServiceLayerCoordinator>();
        }

        private void RegisterDefaultProviders()
        {
            // Register built-in providers with their respective weights
            RegisterProvider("time_management", new TimeManagementOfflineProvider(), 1.0f);
            RegisterProvider("resource_generation", new ResourceGenerationOfflineProvider(), 0.8f);
            RegisterProvider("system_maintenance", new SystemMaintenanceOfflineProvider(), 0.6f);
            RegisterProvider("automation_systems", new AutomationSystemsOfflineProvider(), 0.9f);
        }

        public void RegisterProvider(string providerId, IOfflineProgressionProvider provider, float weight = 1.0f)
        {
            _progressionProviders[providerId] = provider;
            _providerWeights[providerId] = weight;

            ChimeraLogger.Log($"[OfflineProgressionArchitecture] Registered offline progression provider: {providerId} (weight: {weight})");
        }

        public void UnregisterProvider(string providerId)
        {
            if (_progressionProviders.ContainsKey(providerId))
            {
                _progressionProviders.Remove(providerId);
                _providerWeights.Remove(providerId);
                ChimeraLogger.Log($"[OfflineProgressionArchitecture] Unregistered offline progression provider: {providerId}");
            }
        }

        private void LoadLastOnlineTime()
        {
            var lastOnlineTimeString = PlayerPrefs.GetString("LastOnlineTime", DateTime.UtcNow.ToString("O"));
            if (DateTime.TryParse(lastOnlineTimeString, out _lastOnlineTime))
            {
                ChimeraLogger.Log($"[OfflineProgressionArchitecture] Last online time loaded: {_lastOnlineTime:yyyy-MM-dd HH:mm:ss}");
            }
            else
            {
                _lastOnlineTime = DateTime.UtcNow;
                SaveLastOnlineTime();
            }
        }

        private void SaveLastOnlineTime()
        {
            _lastOnlineTime = DateTime.UtcNow;
            PlayerPrefs.SetString("LastOnlineTime", _lastOnlineTime.ToString("O"));
            PlayerPrefs.Save();

            if (_enableProgressionLogging)
            {
                ChimeraLogger.Log($"[OfflineProgressionArchitecture] Last online time saved: {_lastOnlineTime:yyyy-MM-dd HH:mm:ss}");
            }
        }

        private void CheckForOfflineProgression()
        {
            if (!_enableOfflineProgression || _isProcessingOfflineProgression)
                return;

            var currentTime = DateTime.UtcNow;
            var offlineTime = currentTime - _lastOnlineTime;

            if (offlineTime.TotalMinutes >= _minOfflineMinutes)
            {
                _ = ProcessOfflineProgressionAsync(offlineTime);
            }
        }

        public async Task<OfflineProgressionSession> ProcessOfflineProgressionAsync(TimeSpan offlineTime)
        {
            if (_isProcessingOfflineProgression)
            {
                ChimeraLogger.LogWarning("[OfflineProgressionArchitecture] Offline progression already in progress");
                return null;
            }

            _isProcessingOfflineProgression = true;

            try
            {
                // Cap offline time to maximum allowed
                var cappedOfflineTime = TimeSpan.FromHours(Math.Min(offlineTime.TotalHours, _maxOfflineHours));

                // Create progression session
                _currentSession = new OfflineProgressionSession
                {
                    SessionId = Guid.NewGuid().ToString(),
                    StartTime = DateTime.UtcNow,
                    OfflineTime = cappedOfflineTime,
                    OriginalOfflineTime = offlineTime,
                    EnabledProviders = _progressionProviders.Keys.ToList(),
                    AccelerationMultiplier = _enableAcceleratedProgression ? _accelerationMultiplier : 1.0f
                };

                OnOfflineProgressionStarted?.Invoke(_currentSession);

                if (_enableProgressionLogging)
                {
                    ChimeraLogger.Log($"[OfflineProgressionArchitecture] Processing offline progression for {cappedOfflineTime.TotalHours:F2} hours");
                }

                // Process each provider
                await ProcessProvidersAsync(_currentSession);

                // Finalize session
                _currentSession.EndTime = DateTime.UtcNow;
                _currentSession.ProcessingDuration = _currentSession.EndTime - _currentSession.StartTime;
                _currentSession.Success = true;

                // Update metrics
                UpdateSessionMetrics(_currentSession);

                // Send data to analytics
                if (_dataPipeline != null)
                {
                    SendSessionToDataPipeline(_currentSession);
                }

                OnOfflineProgressionCompleted?.Invoke(_currentSession);

                return _currentSession;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[OfflineProgressionArchitecture] Offline progression error: {ex.Message}");
                OnOfflineProgressionError?.Invoke(ex.Message);

                if (_currentSession != null)
                {
                    _currentSession.Success = false;
                    _currentSession.ErrorMessage = ex.Message;
                }

                return _currentSession;
            }
            finally
            {
                _isProcessingOfflineProgression = false;
                SaveLastOnlineTime();
            }
        }

        private async Task ProcessProvidersAsync(OfflineProgressionSession session)
        {
            var providerTasks = new List<Task<OfflineProgressionResult>>();

            // Start all provider calculations in parallel
            foreach (var provider in _progressionProviders)
            {
                var task = ProcessProviderAsync(provider.Key, provider.Value, session);
                providerTasks.Add(task);
            }

            // Wait for all providers to complete with timeout
            var completedTask = await Task.WhenAny(
                Task.WhenAll(providerTasks),
                Task.Delay(TimeSpan.FromSeconds(_processingTimeLimit))
            );

            if (completedTask != Task.WhenAll(providerTasks))
            {
                ChimeraLogger.LogWarning($"[OfflineProgressionArchitecture] Provider processing timed out after {_processingTimeLimit}s");
            }

            // Collect results from completed tasks
            foreach (var task in providerTasks)
            {
                if (task.IsCompleted && !task.IsFaulted)
                {
                    var result = await task;
                    session.ProviderResults.Add(result);
                    _lastProgressionResults.Add(result);

                    // Apply progression result
                    if (result.Success)
                    {
                        await ApplyProgressionResultAsync(result);
                    }

                    OnProviderProgressionCalculated?.Invoke(result);
                }
            }

            // Validate results if enabled
            if (_enableProgressionValidation)
            {
                ValidateProgressionResults(session);
            }
        }

        private async Task<OfflineProgressionResult> ProcessProviderAsync(string providerId, IOfflineProgressionProvider provider, OfflineProgressionSession session)
        {
            var startTime = DateTime.UtcNow;
            var result = new OfflineProgressionResult
            {
                ProviderId = providerId,
                SessionId = session.SessionId,
                StartTime = startTime,
                OfflineTime = session.OfflineTime,
                Weight = _providerWeights.GetValueOrDefault(providerId, 1.0f)
            };

            try
            {
                // Apply acceleration multiplier to offline time for calculation
                var acceleratedTime = TimeSpan.FromMilliseconds(
                    session.OfflineTime.TotalMilliseconds * session.AccelerationMultiplier
                );

                // Calculate progression
                var calculationResult = await provider.CalculateOfflineProgressionAsync(acceleratedTime);

                result.ProgressionData = calculationResult.ProgressionData;
                result.ResourceChanges = calculationResult.ResourceChanges;
                result.Events = calculationResult.Events;
                result.Notifications = calculationResult.Notifications;
                result.Success = calculationResult.Success;
                result.ErrorMessage = calculationResult.ErrorMessage;
                result.ValidationWarnings = calculationResult.ValidationWarnings;

            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                ChimeraLogger.LogError($"[OfflineProgressionArchitecture] Provider {providerId} calculation failed: {ex.Message}");
            }
            finally
            {
                result.EndTime = DateTime.UtcNow;
                result.ProcessingDuration = result.EndTime - startTime;
            }

            return result;
        }

        private async Task ApplyProgressionResultAsync(OfflineProgressionResult result)
        {
            if (!_progressionProviders.TryGetValue(result.ProviderId, out var provider))
                return;

            try
            {
                await provider.ApplyOfflineProgressionAsync(result);

                if (_enableProgressionLogging)
                {
                    ChimeraLogger.Log($"[OfflineProgressionArchitecture] Applied progression for {result.ProviderId}");
                }
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[OfflineProgressionArchitecture] Failed to apply progression for {result.ProviderId}: {ex.Message}");
                result.ApplicationErrors.Add($"Application failed: {ex.Message}");
            }
        }

        private void ValidateProgressionResults(OfflineProgressionSession session)
        {
            var validationIssues = new List<string>();

            foreach (var result in session.ProviderResults)
            {
                // Check for reasonable resource changes
                if (result.ResourceChanges != null)
                {
                    foreach (var change in result.ResourceChanges)
                    {
                        if (change.Value < -1000000 || change.Value > 1000000)
                        {
                            validationIssues.Add($"Extreme resource change in {result.ProviderId}: {change.Key} = {change.Value}");
                        }
                    }
                }

                // Check processing duration
                if (result.ProcessingDuration.TotalSeconds > _processingTimeLimit * 0.5f)
                {
                    validationIssues.Add($"Long processing time for {result.ProviderId}: {result.ProcessingDuration.TotalSeconds:F2}s");
                }
            }

            session.ValidationIssues = validationIssues;

            if (validationIssues.Count > 0)
            {
                ChimeraLogger.LogWarning($"[OfflineProgressionArchitecture] Validation issues found: {string.Join(", ", validationIssues)}");
            }
        }

        private void UpdateSessionMetrics(OfflineProgressionSession session)
        {
            _sessionMetrics.TotalSessions++;
            _sessionMetrics.TotalOfflineTime += session.OfflineTime;
            _sessionMetrics.TotalProcessingTime += session.ProcessingDuration;
            _sessionMetrics.SuccessfulSessions += session.Success ? 1 : 0;
            _sessionMetrics.LastSessionTime = session.StartTime;

            if (session.Success)
            {
                _sessionMetrics.AverageOfflineHours = _sessionMetrics.TotalOfflineTime.TotalHours / _sessionMetrics.SuccessfulSessions;
                _sessionMetrics.AverageProcessingTime = _sessionMetrics.TotalProcessingTime.TotalSeconds / _sessionMetrics.SuccessfulSessions;
            }
        }

        private void SendSessionToDataPipeline(OfflineProgressionSession session)
        {
            var eventData = new
            {
                session_id = session.SessionId,
                offline_hours = session.OfflineTime.TotalHours,
                processing_seconds = session.ProcessingDuration.TotalSeconds,
                providers_count = session.ProviderResults.Count,
                successful_providers = session.ProviderResults.Count(r => r.Success),
                acceleration_multiplier = session.AccelerationMultiplier,
                validation_issues = session.ValidationIssues?.Count ?? 0,
                success = session.Success
            };

            _dataPipeline.CollectEvent("offline_progression", "session_completed", eventData, new Dictionary<string, object>
            {
                ["source"] = "offline_progression_architecture",
                ["timestamp"] = session.StartTime.ToString("O")
            });
        }

        public OfflineProgressionMetrics GetMetrics()
        {
            return _sessionMetrics;
        }

        public OfflineProgressionSession GetLastSession()
        {
            return _currentSession;
        }

        public IReadOnlyList<OfflineProgressionResult> GetRecentResults(int maxResults = 10)
        {
            return _lastProgressionResults.TakeLast(maxResults).ToList().AsReadOnly();
        }

        public void SetOfflineProgressionEnabled(bool enabled)
        {
            _enableOfflineProgression = enabled;
            PlayerPrefs.SetInt("OfflineProgressionEnabled", enabled ? 1 : 0);
        }

        public void SetAccelerationMultiplier(float multiplier)
        {
            _accelerationMultiplier = Mathf.Clamp(multiplier, 1.0f, 5.0f);
        }

        public void SetMaxOfflineHours(float hours)
        {
            _maxOfflineHours = Mathf.Clamp(hours, 1f, 720f); // Max 30 days
        }

        public async Task<OfflineProgressionSession> SimulateOfflineProgressionAsync(TimeSpan simulatedOfflineTime)
        {
            if (_isProcessingOfflineProgression)
                return null;

            ChimeraLogger.Log($"[OfflineProgressionArchitecture] Simulating offline progression for {simulatedOfflineTime.TotalHours:F2} hours");
            return await ProcessOfflineProgressionAsync(simulatedOfflineTime);
        }

        public void ClearProgressionHistory()
        {
            _lastProgressionResults.Clear();
            _sessionMetrics = new OfflineProgressionMetrics();
            ChimeraLogger.Log("[OfflineProgressionArchitecture] Progression history cleared");
        }

        private void OnDestroy()
        {
            SaveLastOnlineTime();
        }
    }



    /// <summary>
    /// Offline progression session data
    /// </summary>
    [System.Serializable]
    public class OfflineProgressionSession
    {
        public string SessionId;
        public DateTime StartTime;
        public DateTime EndTime;
        public TimeSpan OfflineTime;
        public TimeSpan OriginalOfflineTime;
        public TimeSpan ProcessingDuration;
        public List<string> EnabledProviders = new List<string>();
        public List<OfflineProgressionResult> ProviderResults = new List<OfflineProgressionResult>();
        public float AccelerationMultiplier = 1.0f;
        public bool Success;
        public string ErrorMessage;
        public List<string> ValidationIssues;
    }

    /// <summary>
    /// Result from offline progression calculation
    /// </summary>
    [System.Serializable]
    public class OfflineProgressionResult
    {
        public string ProviderId;
        public string SessionId;
        public DateTime StartTime;
        public DateTime EndTime;
        public TimeSpan ProcessingDuration;
        public TimeSpan OfflineTime;
        public float Weight = 1.0f;
        public bool Success;
        public string ErrorMessage;
        public Dictionary<string, object> ProgressionData = new Dictionary<string, object>();
        public Dictionary<string, float> ResourceChanges = new Dictionary<string, float>();
        public List<OfflineProgressionEvent> Events = new List<OfflineProgressionEvent>();
        public List<string> Notifications = new List<string>();
        public List<string> ValidationWarnings = new List<string>();
        public List<string> ApplicationErrors = new List<string>();
    }

    /// <summary>
    /// Result from provider calculation
    /// </summary>
    [System.Serializable]
    public class OfflineProgressionCalculationResult
    {
        public bool Success = true;
        public string ErrorMessage;
        public Dictionary<string, object> ProgressionData = new Dictionary<string, object>();
        public Dictionary<string, float> ResourceChanges = new Dictionary<string, float>();
        public List<OfflineProgressionEvent> Events = new List<OfflineProgressionEvent>();
        public List<string> Notifications = new List<string>();
        public List<string> ValidationWarnings = new List<string>();
    }

    /// <summary>
    /// Offline progression event
    /// </summary>
    [System.Serializable]
    public class OfflineProgressionEvent
    {
        public string EventId;
        public string EventType;
        public DateTime Timestamp;
        public string Title;
        public string Description;
        public Dictionary<string, object> Data = new Dictionary<string, object>();
        public EventPriority Priority = EventPriority.Normal;

        public OfflineProgressionEvent()
        {
            EventId = Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Event priority levels
    /// </summary>
    public enum EventPriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    /// <summary>
    /// Offline progression metrics
    /// </summary>
    [System.Serializable]
    public class OfflineProgressionMetrics
    {
        public int TotalSessions;
        public int SuccessfulSessions;
        public TimeSpan TotalOfflineTime;
        public TimeSpan TotalProcessingTime;
        public double AverageOfflineHours;
        public double AverageProcessingTime;
        public DateTime LastSessionTime;

        public float SuccessRate => TotalSessions > 0 ? (float)SuccessfulSessions / TotalSessions : 0f;
    }

    // Built-in Offline Progression Providers

    /// <summary>
    /// Time management offline provider
    /// </summary>
    public class TimeManagementOfflineProvider : IOfflineProgressionProvider
    {
        public string GetProviderId() => "time_management";
        public float GetPriority() => 1.0f;

        public async Task<OfflineProgressionCalculationResult> CalculateOfflineProgressionAsync(TimeSpan offlineTime)
        {
            await Task.Delay(10); // Simulate calculation

            var result = new OfflineProgressionCalculationResult();

            // Calculate time-based progression
            var hours = (float)offlineTime.TotalHours;

            result.ProgressionData["time_progression_hours"] = hours;
            result.ProgressionData["time_bonus_multiplier"] = Math.Min(1.0f + hours * 0.01f, 2.0f);

            result.Events.Add(new OfflineProgressionEvent
            {
                EventType = "time_progression",
                Title = "Time Progression",
                Description = $"Time progressed by {hours:F1} hours while offline",
                Timestamp = DateTime.UtcNow.Subtract(offlineTime),
                Priority = EventPriority.Low
            });

            return result;
        }

        public async Task ApplyOfflineProgressionAsync(OfflineProgressionResult result)
        {
            await Task.Delay(5); // Simulate application
            // Apply time-based bonuses or effects
        }
    }

    /// <summary>
    /// Resource generation offline provider
    /// </summary>
    public class ResourceGenerationOfflineProvider : IOfflineProgressionProvider
    {
        public string GetProviderId() => "resource_generation";
        public float GetPriority() => 0.8f;

        public async Task<OfflineProgressionCalculationResult> CalculateOfflineProgressionAsync(TimeSpan offlineTime)
        {
            await Task.Delay(15);

            var result = new OfflineProgressionCalculationResult();
            var hours = (float)offlineTime.TotalHours;

            // Calculate basic resource generation
            var baseRate = 10f; // Resources per hour
            var generatedResources = baseRate * hours;

            result.ResourceChanges["energy"] = generatedResources;
            result.ResourceChanges["materials"] = generatedResources * 0.5f;

            result.ProgressionData["generation_rate"] = baseRate;
            result.ProgressionData["total_generated"] = generatedResources;

            if (generatedResources > 100f)
            {
                result.Events.Add(new OfflineProgressionEvent
                {
                    EventType = "resource_milestone",
                    Title = "Resource Generation",
                    Description = $"Generated {generatedResources:F0} resources while offline",
                    Priority = EventPriority.Normal
                });
            }

            return result;
        }

        public async Task ApplyOfflineProgressionAsync(OfflineProgressionResult result)
        {
            await Task.Delay(10);
            // Apply resource changes to player inventory
        }
    }

    /// <summary>
    /// System maintenance offline provider
    /// </summary>
    public class SystemMaintenanceOfflineProvider : IOfflineProgressionProvider
    {
        public string GetProviderId() => "system_maintenance";
        public float GetPriority() => 0.6f;

        public async Task<OfflineProgressionCalculationResult> CalculateOfflineProgressionAsync(TimeSpan offlineTime)
        {
            await Task.Delay(20);

            var result = new OfflineProgressionCalculationResult();
            var hours = (float)offlineTime.TotalHours;

            // Calculate system degradation and maintenance needs
            var degradationRate = 0.5f; // Degradation per hour
            var totalDegradation = degradationRate * hours;

            result.ProgressionData["system_degradation"] = totalDegradation;
            result.ProgressionData["maintenance_required"] = totalDegradation > 24f; // Maintenance needed after 48 hours

            if (totalDegradation > 24f)
            {
                result.Notifications.Add("Systems require maintenance after extended offline period");
                result.Events.Add(new OfflineProgressionEvent
                {
                    EventType = "maintenance_required",
                    Title = "System Maintenance",
                    Description = "Your systems need attention after being offline",
                    Priority = EventPriority.High
                });
            }

            return result;
        }

        public async Task ApplyOfflineProgressionAsync(OfflineProgressionResult result)
        {
            await Task.Delay(5);
            // Update system health and maintenance flags
        }
    }

    /// <summary>
    /// Automation systems offline provider
    /// </summary>
    public class AutomationSystemsOfflineProvider : IOfflineProgressionProvider
    {
        public string GetProviderId() => "automation_systems";
        public float GetPriority() => 0.9f;

        public async Task<OfflineProgressionCalculationResult> CalculateOfflineProgressionAsync(TimeSpan offlineTime)
        {
            await Task.Delay(25);

            var result = new OfflineProgressionCalculationResult();
            var hours = (float)offlineTime.TotalHours;

            // Calculate automated task completion
            var tasksPerHour = 2f;
            var completedTasks = Mathf.FloorToInt(tasksPerHour * hours);

            result.ProgressionData["automated_tasks_completed"] = completedTasks;
            result.ProgressionData["automation_efficiency"] = Math.Min(0.95f, 0.5f + hours * 0.01f);

            // Generate automation-related resources
            result.ResourceChanges["automation_points"] = completedTasks * 5f;

            if (completedTasks > 0)
            {
                result.Events.Add(new OfflineProgressionEvent
                {
                    EventType = "automation_completed",
                    Title = "Automated Tasks Completed",
                    Description = $"Automation systems completed {completedTasks} tasks while you were away",
                    Priority = EventPriority.Normal
                });
            }

            return result;
        }

        public async Task ApplyOfflineProgressionAsync(OfflineProgressionResult result)
        {
            await Task.Delay(8);
            // Update automation system states and rewards
        }
    }
}
