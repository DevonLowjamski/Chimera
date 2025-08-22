using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Helper component that can be added to any manager to provide easy analytics integration
    /// Automatically discovers and registers analytics providers, and provides convenient APIs
    /// </summary>
    public class AnalyticsIntegrationHelper : MonoBehaviour
    {
        [Header("Integration Configuration")]
        [SerializeField] private bool _autoRegisterOnStart = true;
        [SerializeField] private bool _enableDebugLogging = false;
        [SerializeField] private bool _createDefaultProviders = true;

        [Header("Provider Types")]
        [SerializeField] private bool _includeCultivation = false;
        [SerializeField] private bool _includeEconomic = false;
        [SerializeField] private bool _includeEnvironmental = false;
        [SerializeField] private bool _includeFacility = false;
        [SerializeField] private bool _includeOperational = false;

        private readonly List<IAnalyticsProvider> _providers = new List<IAnalyticsProvider>();
        private AnalyticsProviderRegistry _providerRegistry;
        private IAnalyticsService _analyticsService;
        private bool _isInitialized = false;

        public event Action<IAnalyticsProvider> OnProviderAdded;
        public event Action OnIntegrationComplete;

        #region Unity Lifecycle

        private void Start()
        {
            if (_autoRegisterOnStart)
            {
                InitializeIntegration();
            }
        }

        private void OnDestroy()
        {
            CleanupIntegration();
        }

        #endregion

        #region Integration Management

        public void InitializeIntegration()
        {
            if (_isInitialized)
            {
                if (_enableDebugLogging)
                    Debug.LogWarning("[AnalyticsIntegrationHelper] Already initialized");
                return;
            }

            try
            {
                // Get analytics service
                _analyticsService = AnalyticsManager.GetService();
                if (_analyticsService == null)
                {
                    Debug.LogWarning("[AnalyticsIntegrationHelper] AnalyticsService not available");
                    return;
                }

                // Get provider registry
                _providerRegistry = UnityEngine.Object.FindObjectOfType<AnalyticsProviderRegistry>();
                if (_providerRegistry == null && _enableDebugLogging)
                {
                    Debug.LogWarning("[AnalyticsIntegrationHelper] AnalyticsProviderRegistry not found");
                }

                // Discover existing providers on this GameObject
                DiscoverProviders();

                // Create default providers if enabled
                if (_createDefaultProviders)
                {
                    CreateDefaultProviders();
                }

                // Register all providers
                RegisterProviders();

                _isInitialized = true;
                OnIntegrationComplete?.Invoke();

                if (_enableDebugLogging)
                    Debug.Log($"[AnalyticsIntegrationHelper] Integration initialized with {_providers.Count} providers");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AnalyticsIntegrationHelper] Failed to initialize integration: {ex.Message}");
            }
        }

        private void DiscoverProviders()
        {
            var providers = GetComponents<IAnalyticsProvider>();
            foreach (var provider in providers)
            {
                _providers.Add(provider);
                OnProviderAdded?.Invoke(provider);

                if (_enableDebugLogging)
                    Debug.Log($"[AnalyticsIntegrationHelper] Discovered provider: {provider.GetType().Name}");
            }
        }

        private void CreateDefaultProviders()
        {
            if (_includeCultivation)
            {
                CreateCultivationProvider();
            }

            if (_includeEconomic)
            {
                CreateEconomicProvider();
            }

            if (_includeEnvironmental)
            {
                CreateEnvironmentalProvider();
            }

            if (_includeFacility)
            {
                CreateFacilityProvider();
            }

            if (_includeOperational)
            {
                CreateOperationalProvider();
            }
        }

        private void RegisterProviders()
        {
            foreach (var provider in _providers)
            {
                try
                {
                    // Register with provider registry if available
                    if (_providerRegistry != null)
                    {
                        var providerName = $"{gameObject.name}_{provider.GetType().Name}";
                        _providerRegistry.RegisterProvider(providerName, provider, this);
                    }

                    // Register metrics directly with analytics service
                    RegisterProviderMetrics(provider);

                    if (_enableDebugLogging)
                        Debug.Log($"[AnalyticsIntegrationHelper] Registered provider: {provider.GetType().Name}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AnalyticsIntegrationHelper] Failed to register provider {provider.GetType().Name}: {ex.Message}");
                }
            }
        }

        private void RegisterProviderMetrics(IAnalyticsProvider provider)
        {
            foreach (var metricName in provider.GetAvailableMetrics())
            {
                try
                {
                    var collector = new ProviderMetricCollector(provider, metricName);
                    var fullMetricName = $"{gameObject.name}_{metricName}";

                    _analyticsService.RegisterMetricCollector(fullMetricName, collector);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AnalyticsIntegrationHelper] Failed to register metric {metricName}: {ex.Message}");
                }
            }
        }

        private void CleanupIntegration()
        {
            if (!_isInitialized) return;

            try
            {
                // Unregister providers
                foreach (var provider in _providers)
                {
                    if (_providerRegistry != null)
                    {
                        var providerName = $"{gameObject.name}_{provider.GetType().Name}";
                        _providerRegistry.UnregisterProvider(providerName);
                    }
                }

                _providers.Clear();
                _isInitialized = false;

                if (_enableDebugLogging)
                    Debug.Log("[AnalyticsIntegrationHelper] Integration cleaned up");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AnalyticsIntegrationHelper] Error during cleanup: {ex.Message}");
            }
        }

        #endregion

        #region Provider Creation

        private void CreateCultivationProvider()
        {
            var cultivationManager = GetComponent<ProjectChimera.Systems.Cultivation.CultivationManager>();
            if (cultivationManager != null)
            {
                var provider = gameObject.AddComponent<ProjectChimera.Systems.Analytics.Providers.CultivationAnalyticsProvider>();
                provider.Initialize(cultivationManager);
                _providers.Add(provider);

                if (_enableDebugLogging)
                    Debug.Log("[AnalyticsIntegrationHelper] Created CultivationAnalyticsProvider");
            }
        }

        private void CreateEconomicProvider()
        {
            var currencyManager = UnityEngine.Object.FindObjectOfType<ProjectChimera.Systems.Economy.CurrencyManager>();
            if (currencyManager != null)
            {
                var provider = gameObject.AddComponent<ProjectChimera.Systems.Analytics.Providers.EconomicAnalyticsProvider>();
                provider.Initialize(currencyManager);
                _providers.Add(provider);

                if (_enableDebugLogging)
                    Debug.Log("[AnalyticsIntegrationHelper] Created EconomicAnalyticsProvider");
            }
        }

        private void CreateEnvironmentalProvider()
        {
            // Create environmental provider if environment manager exists
            var environmentManager = GetComponent<ProjectChimera.Systems.Environment.EnvironmentManager>();
            if (environmentManager != null)
            {
                // Would create EnvironmentalAnalyticsProvider here
                if (_enableDebugLogging)
                    Debug.Log("[AnalyticsIntegrationHelper] Environmental provider creation not yet implemented");
            }
        }

        private void CreateFacilityProvider()
        {
            // Create facility provider if facility manager exists
            var facilityManager = GetComponent<ProjectChimera.Systems.Facilities.FacilityManager>();
            if (facilityManager != null)
            {
                // Would create FacilityAnalyticsProvider here
                if (_enableDebugLogging)
                    Debug.Log("[AnalyticsIntegrationHelper] Facility provider creation not yet implemented");
            }
        }

        private void CreateOperationalProvider()
        {
            // Create operational provider for general operational metrics
            if (_enableDebugLogging)
                Debug.Log("[AnalyticsIntegrationHelper] Operational provider creation not yet implemented");
        }

        #endregion

        #region Public API

        public void AddProvider(IAnalyticsProvider provider)
        {
            if (provider == null) return;

            _providers.Add(provider);
            OnProviderAdded?.Invoke(provider);

            if (_isInitialized)
            {
                RegisterProviderMetrics(provider);
            }

            if (_enableDebugLogging)
                Debug.Log($"[AnalyticsIntegrationHelper] Added provider: {provider.GetType().Name}");
        }

        public void RemoveProvider(IAnalyticsProvider provider)
        {
            if (provider == null) return;

            _providers.Remove(provider);

            if (_enableDebugLogging)
                Debug.Log($"[AnalyticsIntegrationHelper] Removed provider: {provider.GetType().Name}");
        }

        public IEnumerable<IAnalyticsProvider> GetProviders()
        {
            return _providers;
        }

        public T GetProvider<T>() where T : class, IAnalyticsProvider
        {
            foreach (var provider in _providers)
            {
                if (provider is T typedProvider)
                    return typedProvider;
            }
            return null;
        }

        public int GetProviderCount()
        {
            return _providers.Count;
        }

        public bool IsInitialized => _isInitialized;

        public void SetDebugLogging(bool enabled)
        {
            _enableDebugLogging = enabled;
        }

        /// <summary>
        /// Force re-initialization of the integration
        /// </summary>
        public void RefreshIntegration()
        {
            CleanupIntegration();
            InitializeIntegration();
        }

        /// <summary>
        /// Get summary of all providers and their metrics
        /// </summary>
        public string GetIntegrationSummary()
        {
            var summary = $"[AnalyticsIntegrationHelper] Integration Summary for {gameObject.name}:\n";
            summary += $"  Initialized: {_isInitialized}\n";
            summary += $"  Provider Count: {_providers.Count}\n";
            summary += $"  Analytics Service: {(_analyticsService != null ? "Connected" : "Not Available")}\n\n";

            foreach (var provider in _providers)
            {
                summary += $"  Provider: {provider.GetType().Name}\n";
                summary += $"    Metrics: {provider.GetAvailableMetrics().Count()}\n";
                
                if (provider is AnalyticsProviderBase baseProvider)
                {
                    summary += $"    Status: {(baseProvider.ValidateMetrics() ? "Valid" : "Invalid")}\n";
                }
            }

            return summary;
        }

        #endregion
    }
}