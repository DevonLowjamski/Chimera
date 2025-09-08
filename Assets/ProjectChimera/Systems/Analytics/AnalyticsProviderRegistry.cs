using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Registry for managing analytics providers and their integration with AnalyticsManager
    /// Automatically discovers providers and manages metric collection from multiple sources
    /// </summary>
    public class AnalyticsProviderRegistry : MonoBehaviour, ITickable
    {
        [Header("Registry Configuration")]
        [SerializeField] private bool _enableAutoDiscovery = true;
        [SerializeField] private bool _enableDebugLogging = false;
        [SerializeField] private float _discoveryInterval = 10f;
        [SerializeField] private bool _validateOnRegistration = true;

        private readonly Dictionary<string, IAnalyticsProvider> _providers = new Dictionary<string, IAnalyticsProvider>();
        private readonly Dictionary<string, ProviderInfo> _providerInfo = new Dictionary<string, ProviderInfo>();
        private IAnalyticsService _analyticsService;
        private float _lastDiscoveryTime;

        public event Action<IAnalyticsProvider> OnProviderRegistered;
        public event Action<string> OnProviderUnregistered;
        public event Action OnProvidersChanged;

        #region Unity Lifecycle

        private void Start()
        {
        // Register with UpdateOrchestrator
        UpdateOrchestrator.Instance?.RegisterTickable(this);
            InitializeRegistry();
        }

            public void Tick(float deltaTime)
    {
            if (_enableAutoDiscovery && Time.time >= _lastDiscoveryTime + _discoveryInterval)
            {
                DiscoverProviders();
                _lastDiscoveryTime = Time.time;

    }
        }

        #endregion

        #region Initialization

        private void InitializeRegistry()
        {
            try
            {
                // Get analytics service
                _analyticsService = AnalyticsManager.GetService();
                if (_analyticsService == null)
                {
                    ChimeraLogger.LogWarning("[AnalyticsProviderRegistry] AnalyticsService not available - registry will operate in limited mode");
                }

                // Initial discovery
                DiscoverProviders();
                _lastDiscoveryTime = Time.time;

                if (_enableDebugLogging)
                    ChimeraLogger.Log("[AnalyticsProviderRegistry] Registry initialized successfully");
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[AnalyticsProviderRegistry] Failed to initialize: {ex.Message}");
            }
        }

        #endregion

        #region Provider Discovery

        public void DiscoverProviders()
        {
            try
            {
                var discoveredCount = 0;

                // Find all MonoBehaviour components that implement IAnalyticsProvider
                // TODO: ServiceContainer.GetAll<MonoBehaviour>()
                var allComponents = new MonoBehaviour[0];
                foreach (var component in allComponents)
                {
                    if (component is IAnalyticsProvider provider)
                    {
                        var providerName = component.GetType().Name;
                        if (!_providers.ContainsKey(providerName))
                        {
                            RegisterProvider(providerName, provider, component);
                            discoveredCount++;
                        }
                    }
                }

                if (_enableDebugLogging && discoveredCount > 0)
                    ChimeraLogger.Log($"[AnalyticsProviderRegistry] Discovered {discoveredCount} new providers");
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[AnalyticsProviderRegistry] Error during provider discovery: {ex.Message}");
            }
        }

        #endregion

        #region Provider Management

        public void RegisterProvider(string providerName, IAnalyticsProvider provider, MonoBehaviour component = null)
        {
            try
            {
                if (_providers.ContainsKey(providerName))
                {
                    if (_enableDebugLogging)
                        ChimeraLogger.LogWarning($"[AnalyticsProviderRegistry] Provider '{providerName}' already registered");
                    return;
                }

                // Validate provider if enabled
                if (_validateOnRegistration && provider is AnalyticsProviderBase baseProvider)
                {
                    if (!baseProvider.ValidateMetrics())
                    {
                        ChimeraLogger.LogWarning($"[AnalyticsProviderRegistry] Provider '{providerName}' failed validation");
                    }
                }

                // Register provider
                _providers[providerName] = provider;
                _providerInfo[providerName] = new ProviderInfo
                {
                    Name = providerName,
                    Provider = provider,
                    Component = component,
                    RegistrationTime = Time.time,
                    AvailableMetrics = provider.GetAvailableMetrics().ToList()
                };

                // Register metrics with analytics service
                if (_analyticsService != null)
                {
                    RegisterProviderMetrics(providerName, provider);
                }

                OnProviderRegistered?.Invoke(provider);
                OnProvidersChanged?.Invoke();

                if (_enableDebugLogging)
                    ChimeraLogger.Log($"[AnalyticsProviderRegistry] Registered provider '{providerName}' with {provider.GetAvailableMetrics().Count()} metrics");
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[AnalyticsProviderRegistry] Failed to register provider '{providerName}': {ex.Message}");
            }
        }

        public void UnregisterProvider(string providerName)
        {
            try
            {
                if (!_providers.ContainsKey(providerName))
                    return;

                // Unregister metrics from analytics service
                if (_analyticsService != null && _providerInfo.ContainsKey(providerName))
                {
                    UnregisterProviderMetrics(providerName);
                }

                _providers.Remove(providerName);
                _providerInfo.Remove(providerName);

                OnProviderUnregistered?.Invoke(providerName);
                OnProvidersChanged?.Invoke();

                if (_enableDebugLogging)
                    ChimeraLogger.Log($"[AnalyticsProviderRegistry] Unregistered provider '{providerName}'");
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[AnalyticsProviderRegistry] Failed to unregister provider '{providerName}': {ex.Message}");
            }
        }

        #endregion

        #region Metrics Integration

        private void RegisterProviderMetrics(string providerName, IAnalyticsProvider provider)
        {
            foreach (var metricName in provider.GetAvailableMetrics())
            {
                try
                {
                    var collector = new ProviderMetricCollector(provider, metricName);
                    var fullMetricName = $"{providerName}_{metricName}";

                    _analyticsService.RegisterMetricCollector(fullMetricName, collector);

                    if (_enableDebugLogging)
                        ChimeraLogger.Log($"[AnalyticsProviderRegistry] Registered metric collector: {fullMetricName}");
                }
                catch (Exception ex)
                {
                    ChimeraLogger.LogError($"[AnalyticsProviderRegistry] Failed to register metric collector '{metricName}' for provider '{providerName}': {ex.Message}");
                }
            }
        }

        private void UnregisterProviderMetrics(string providerName)
        {
            if (!_providerInfo.ContainsKey(providerName))
                return;

            var info = _providerInfo[providerName];
            foreach (var metricName in info.AvailableMetrics)
            {
                try
                {
                    var fullMetricName = $"{providerName}_{metricName}";
                    _analyticsService.UnregisterMetricCollector(fullMetricName);
                }
                catch (Exception ex)
                {
                    ChimeraLogger.LogError($"[AnalyticsProviderRegistry] Failed to unregister metric collector '{metricName}' for provider '{providerName}': {ex.Message}");
                }
            }
        }

        #endregion

        #region Public API

        public IEnumerable<string> GetProviderNames()
        {
            return _providers.Keys;
        }

        public IAnalyticsProvider GetProvider(string providerName)
        {
            return _providers.TryGetValue(providerName, out var provider) ? provider : null;
        }

        public IEnumerable<IAnalyticsProvider> GetAllProviders()
        {
            return _providers.Values;
        }

        public int GetProviderCount()
        {
            return _providers.Count;
        }

        public Dictionary<string, IEnumerable<string>> GetAllProviderMetrics()
        {
            return _providers.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.GetAvailableMetrics()
            );
        }

        public bool ValidateAllProviders()
        {
            var allValid = true;
            foreach (var provider in _providers.Values.OfType<AnalyticsProviderBase>())
            {
                if (!provider.ValidateMetrics())
                    allValid = false;
            }
            return allValid;
        }

        public string GetRegistrySummary()
        {
            var summary = $"[AnalyticsProviderRegistry] Summary:\n";
            summary += $"  Total Providers: {_providers.Count}\n";
            summary += $"  Auto Discovery: {(_enableAutoDiscovery ? "Enabled" : "Disabled")}\n";
            summary += $"  Analytics Service: {(_analyticsService != null ? "Connected" : "Not Available")}\n\n";

            foreach (var info in _providerInfo.Values)
            {
                summary += $"  Provider: {info.Name}\n";
                summary += $"    Metrics: {info.AvailableMetrics.Count}\n";
                summary += $"    Registered: {Time.time - info.RegistrationTime:F1}s ago\n";
            }

            return summary;
        }

        #endregion

    // ITickable implementation
    public int Priority => 0;
    public bool Enabled => enabled && gameObject.activeInHierarchy;

    public virtual void OnRegistered()
    {
        // Override in derived classes if needed
    }

    public virtual void OnUnregistered()
    {
        // Override in derived classes if needed
    }

}

    /// <summary>
    /// Metric collector that wraps an analytics provider
    /// </summary>
    public class ProviderMetricCollector : IMetricCollector
    {
        private readonly IAnalyticsProvider _provider;
        private readonly string _metricName;

        public ProviderMetricCollector(IAnalyticsProvider provider, string metricName)
        {
            _provider = provider;
            _metricName = metricName;
        }

        public float CollectMetric()
        {
            return _provider.GetMetricValue(_metricName);
        }
    }

    /// <summary>
    /// Information about a registered provider
    /// </summary>
    public class ProviderInfo
    {
        public string Name { get; set; }
        public IAnalyticsProvider Provider { get; set; }
        public MonoBehaviour Component { get; set; }
        public float RegistrationTime { get; set; }
        public List<string> AvailableMetrics { get; set; } = new List<string>();
    }
}


