using ProjectChimera.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace ProjectChimera.Systems.Progression
{
    /// <summary>
    /// Domain-specific offline progression handlers orchestrator for Project Chimera systems
    /// Refactored from monolithic 969-line file into focused domain-specific providers
    /// Coordinates cultivation, construction, economy, and equipment offline progression
    /// </summary>
    public class DomainSpecificOfflineHandlers : MonoBehaviour
    {
        [Header("Provider Configuration")]
        [SerializeField] private bool _enableCultivationProvider = true;
        [SerializeField] private bool _enableConstructionProvider = true;
        [SerializeField] private bool _enableEconomyProvider = true;
        [SerializeField] private bool _enableEquipmentProvider = true;

        [Header("Orchestration Settings")]
        [SerializeField] private int _maxConcurrentProviders = 4;
        [SerializeField] private float _providerTimeoutSeconds = 30f;

        // Domain-specific offline providers
        private CultivationOfflineProvider _cultivationProvider;
        private ConstructionOfflineProvider _constructionProvider;
        private EconomyOfflineProvider _economyProvider;
        private EquipmentOfflineProvider _equipmentProvider;

        private List<IOfflineProgressionProvider> _activeProviders = new List<IOfflineProgressionProvider>();
        private bool _isInitialized = false;

        public string OrchestratorName => "Domain-Specific Offline Handlers";

        #region Lifecycle Management

        private void Awake()
        {
            InitializeProviders();
        }

        private void Start()
        {
            if (!_isInitialized)
            {
                InitializeProviders();
            }

            ChimeraLogger.Log($"[OfflineHandlersOrchestrator] Started with {_activeProviders.Count} active providers");
        }

        private void OnDestroy()
        {
            ShutdownProviders();
        }

        #endregion

        #region Provider Management

        private void InitializeProviders()
        {
            if (_isInitialized)
            {
                return;
            }

            try
            {
                CreateProviders();
                RegisterActiveProviders();
                ValidateProviderConfiguration();

                _isInitialized = true;
                ChimeraLogger.Log($"[OfflineHandlersOrchestrator] Initialized {_activeProviders.Count} domain providers");
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[OfflineHandlersOrchestrator] Failed to initialize providers: {ex.Message}");
            }
        }

        private void CreateProviders()
        {
            // Create domain-specific providers based on configuration
            if (_enableCultivationProvider)
            {
                _cultivationProvider = new CultivationOfflineProvider();
            }

            if (_enableConstructionProvider)
            {
                _constructionProvider = new ConstructionOfflineProvider();
            }

            if (_enableEconomyProvider)
            {
                _economyProvider = new EconomyOfflineProvider();
            }

            if (_enableEquipmentProvider)
            {
                _equipmentProvider = new EquipmentOfflineProvider();
            }
        }

        private void RegisterActiveProviders()
        {
            _activeProviders.Clear();

            if (_cultivationProvider != null)
            {
                _activeProviders.Add(_cultivationProvider);
            }

            if (_constructionProvider != null)
            {
                _activeProviders.Add(_constructionProvider);
            }

            if (_economyProvider != null)
            {
                _activeProviders.Add(_economyProvider);
            }

            if (_equipmentProvider != null)
            {
                _activeProviders.Add(_equipmentProvider);
            }

            // Sort providers by priority (higher priority first)
            _activeProviders = _activeProviders.OrderByDescending(p => p.GetPriority()).ToList();
        }

        private void ValidateProviderConfiguration()
        {
            if (_activeProviders.Count == 0)
            {
                ChimeraLogger.LogWarning("[OfflineHandlersOrchestrator] No providers enabled - offline progression will be limited");
                return;
            }

            if (_activeProviders.Count > _maxConcurrentProviders)
            {
                ChimeraLogger.LogWarning($"[OfflineHandlersOrchestrator] {_activeProviders.Count} providers exceed maximum of {_maxConcurrentProviders}");
            }

            // Validate provider IDs are unique
            var providerIds = _activeProviders.Select(p => p.GetProviderId()).ToList();
            var duplicateIds = providerIds.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key);

            if (duplicateIds.Any())
            {
                ChimeraLogger.LogError($"[OfflineHandlersOrchestrator] Duplicate provider IDs detected: {string.Join(", ", duplicateIds)}");
            }
        }

        private void ShutdownProviders()
        {
            _activeProviders.Clear();
            _isInitialized = false;
            ChimeraLogger.Log("[OfflineHandlersOrchestrator] All providers shutdown");
        }

        #endregion

        #region Public API - Orchestrated Offline Progression

        /// <summary>
        /// Processes offline progression across all enabled domain providers
        /// </summary>
        public async Task<List<OfflineProgressionCalculationResult>> ProcessAllDomainsOfflineProgressionAsync(TimeSpan offlineTime)
        {
            if (!_isInitialized || _activeProviders.Count == 0)
            {
                ChimeraLogger.LogWarning("[OfflineHandlersOrchestrator] Cannot process offline progression - no providers available");
                return new List<OfflineProgressionCalculationResult>();
            }

            var results = new List<OfflineProgressionCalculationResult>();

            try
            {
                ChimeraLogger.Log($"[OfflineHandlersOrchestrator] Processing offline progression for {offlineTime.TotalHours:F1} hours across {_activeProviders.Count} domains");

                // Process providers in priority order
                foreach (var provider in _activeProviders)
                {
                    try
                    {
                        var providerResult = await ProcessProviderWithTimeout(provider, offlineTime);
                        if (providerResult != null)
                        {
                            results.Add(providerResult);
                        }
                    }
                    catch (Exception ex)
                    {
                        ChimeraLogger.LogError($"[OfflineHandlersOrchestrator] Provider {provider.GetProviderId()} failed: {ex.Message}");

                        // Create error result
                        var errorResult = new OfflineProgressionCalculationResult
                        {
                            Success = false,
                            ErrorMessage = $"Provider {provider.GetProviderId()} failed: {ex.Message}"
                        };
                        results.Add(errorResult);
                    }
                }

                ChimeraLogger.Log($"[OfflineHandlersOrchestrator] Completed offline progression processing - {results.Count} results");
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[OfflineHandlersOrchestrator] Critical error during offline progression: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Applies offline progression results across all domain providers
        /// </summary>
        public async Task ApplyAllDomainsOfflineProgressionAsync(List<OfflineProgressionResult> results)
        {
            if (!_isInitialized || results == null || results.Count == 0)
            {
                return;
            }

            try
            {
                ChimeraLogger.Log($"[OfflineHandlersOrchestrator] Applying offline progression for {results.Count} domain results");

                // Group results by provider ID for efficient application
                var resultsByProvider = results.GroupBy(r => GetProviderForResult(r)).ToList();

                foreach (var providerGroup in resultsByProvider)
                {
                    var provider = providerGroup.Key;
                    if (provider == null) continue;

                    foreach (var result in providerGroup)
                    {
                        try
                        {
                            await provider.ApplyOfflineProgressionAsync(result);
                        }
                        catch (Exception ex)
                        {
                            ChimeraLogger.LogError($"[OfflineHandlersOrchestrator] Failed to apply result for {provider.GetProviderId()}: {ex.Message}");
                        }
                    }
                }

                ChimeraLogger.Log("[OfflineHandlersOrchestrator] Completed offline progression application");
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[OfflineHandlersOrchestrator] Error applying offline progression: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets offline progression capabilities of all enabled providers
        /// </summary>
        public Dictionary<string, float> GetProviderCapabilities()
        {
            var capabilities = new Dictionary<string, float>();

            foreach (var provider in _activeProviders)
            {
                capabilities[provider.GetProviderId()] = provider.GetPriority();
            }

            return capabilities;
        }

        /// <summary>
        /// Gets a specific domain provider by ID
        /// </summary>
        public IOfflineProgressionProvider GetProvider(string providerId)
        {
            return _activeProviders.FirstOrDefault(p => p.GetProviderId() == providerId);
        }

        #endregion

        #region Component Access (for advanced usage)

        /// <summary>
        /// Gets the cultivation offline provider
        /// </summary>
        public CultivationOfflineProvider GetCultivationProvider() => _cultivationProvider;

        /// <summary>
        /// Gets the construction offline provider
        /// </summary>
        public ConstructionOfflineProvider GetConstructionProvider() => _constructionProvider;

        /// <summary>
        /// Gets the economy offline provider
        /// </summary>
        public EconomyOfflineProvider GetEconomyProvider() => _economyProvider;

        /// <summary>
        /// Gets the equipment offline provider
        /// </summary>
        public EquipmentOfflineProvider GetEquipmentProvider() => _equipmentProvider;

        #endregion

        #region Helper Methods

        private async Task<OfflineProgressionCalculationResult> ProcessProviderWithTimeout(IOfflineProgressionProvider provider, TimeSpan offlineTime)
        {
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(_providerTimeoutSeconds));
            var progressionTask = provider.CalculateOfflineProgressionAsync(offlineTime);

            var completedTask = await Task.WhenAny(progressionTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                ChimeraLogger.LogWarning($"[OfflineHandlersOrchestrator] Provider {provider.GetProviderId()} timed out after {_providerTimeoutSeconds}s");
                return new OfflineProgressionCalculationResult
                {
                    Success = false,
                    ErrorMessage = $"Provider {provider.GetProviderId()} timed out"
                };
            }

            return await progressionTask;
        }

        private IOfflineProgressionProvider GetProviderForResult(OfflineProgressionResult result)
        {
            // Match results to providers based on progression data keys or other identifiers
            if (result.ProgressionData.ContainsKey("plant_growth") || result.ProgressionData.ContainsKey("harvests"))
            {
                return _cultivationProvider;
            }

            if (result.ProgressionData.ContainsKey("construction_projects") || result.ProgressionData.ContainsKey("building_completion"))
            {
                return _constructionProvider;
            }

            if (result.ProgressionData.ContainsKey("market_changes") || result.ProgressionData.ContainsKey("contract_fulfillment"))
            {
                return _economyProvider;
            }

            if (result.ProgressionData.ContainsKey("equipment_degradation") || result.ProgressionData.ContainsKey("equipment_production"))
            {
                return _equipmentProvider;
            }

            return null;
        }

        #endregion

        #region Configuration Methods

        /// <summary>
        /// Enables or disables specific domain providers
        /// </summary>
        public void ConfigureProviders(bool cultivation = true, bool construction = true, bool economy = true, bool equipment = true)
        {
            _enableCultivationProvider = cultivation;
            _enableConstructionProvider = construction;
            _enableEconomyProvider = economy;
            _enableEquipmentProvider = equipment;

            if (_isInitialized)
            {
                // Reinitialize with new configuration
                ShutdownProviders();
                InitializeProviders();
            }
        }

        /// <summary>
        /// Sets provider timeout configuration
        /// </summary>
        public void SetProviderTimeout(float timeoutSeconds)
        {
            _providerTimeoutSeconds = Mathf.Max(1f, timeoutSeconds);
            ChimeraLogger.Log($"[OfflineHandlersOrchestrator] Provider timeout set to {_providerTimeoutSeconds}s");
        }

        #endregion
    }


}
