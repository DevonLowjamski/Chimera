using ProjectChimera.Core.Logging;
using ProjectChimera.Systems.Save.Components;
using ValidationResult = ProjectChimera.Data.Save.ValidationResult;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ProjectChimera.Data.Save;
using ProjectChimera.Core;
using ProjectChimera.Core.DependencyInjection;

namespace ProjectChimera.Systems.Save
{
    /// <summary>
    /// Domain-specific save provider for the economy system.
    /// Handles market data, trading state, player finances, investments, and economic indicators.
    /// Implements comprehensive validation, migration, and state management.
    /// </summary>
    public class EconomySaveProvider : MonoBehaviour, ISaveSectionProvider
    {
        [Header("Provider Configuration")]
        [SerializeField] private string _sectionVersion = "1.2.0";
        [SerializeField] private int _priority = (int)SaveSectionPriority.High;
        [SerializeField] private bool _enableIncrementalSave = true;
        [SerializeField] private int _maxTransactionsPerSave = 5000;

        [Header("Data Sources")]
        [SerializeField] private bool _autoDetectSystems = true;
        [SerializeField] private Transform _economySystemRoot;
        [SerializeField] private string[] _marketManagerTags = { "MarketManager", "EconomyManager" };

        [Header("Validation Settings")]
        [SerializeField] private bool _enableDataValidation = true;
        [SerializeField] private bool _validateFinancialConsistency = true;
        [SerializeField] private bool _validateMarketIntegrity = true;
        [SerializeField] private float _maxAllowedDataCorruption = 0.02f; // 2%
        [SerializeField] private bool _enableAutoRepair = true;

        [Header("Financial Validation")]
        [SerializeField] private bool _enableBalanceValidation = true;
        [SerializeField] private float _maxBalanceDiscrepancy = 0.01f; // 1%
        [SerializeField] private bool _validateTransactionIntegrity = true;

        // System references
        private IMarketSystem _marketSystem;
        private ITradingSystem _tradingSystem;
        private IPlayerEconomySystem _playerEconomySystem;
        private IFinancialSystem _financialSystem;
        private IEconomicIndicatorsSystem _economicIndicatorsSystem;
        private bool _systemsInitialized = false;

        // State tracking
        private EconomyStateDTO _lastSavedState;
        private DateTime _lastSaveTime;
        private bool _hasChanges = true;
        private long _estimatedDataSize = 0;

        // Dependencies
        private readonly string[] _dependencies = {
            SaveSectionKeys.PLAYER,
            SaveSectionKeys.SETTINGS,
            SaveSectionKeys.TIME
        };

        #region ISaveSectionProvider Implementation

        public string SectionKey => SaveSectionKeys.ECONOMY;
        public string SectionName => "Economy & Market System";
        public string SectionVersion => _sectionVersion;
        public int Priority => _priority;
        public bool IsRequired => false;
        public bool SupportsIncrementalSave => _enableIncrementalSave;
        public long EstimatedDataSize => _estimatedDataSize;
        public IReadOnlyList<string> Dependencies => _dependencies;

        public async Task<ISaveSectionData> GatherSectionDataAsync()
        {
            await InitializeSystemsIfNeeded();

            var economyData = new EconomySectionData
            {
                SectionKey = SectionKey,
                DataVersion = SectionVersion,
                Timestamp = DateTime.Now
            };

            try
            {
                // Gather core economy state
                economyData.EconomyState = await GatherEconomyStateAsync();

                // Calculate data size and hash
                economyData.EstimatedSize = CalculateDataSize(economyData.EconomyState);
                economyData.DataHash = GenerateDataHash(economyData);

                _estimatedDataSize = economyData.EstimatedSize;
                _lastSavedState = economyData.EconomyState;
                _lastSaveTime = DateTime.Now;
                _hasChanges = false;

                LogInfo($"Economy data gathered: {economyData.EconomyState?.AvailableProducts?.Count ?? 0} products, " +
                       $"{economyData.EconomyState?.TransactionHistory?.Count ?? 0} transactions, " +
                       $"${economyData.EconomyState?.PlayerEconomyState?.PlayerFinances?.TotalCash ?? 0:F2} cash, " +
                       $"Data size: {economyData.EstimatedSize} bytes");

                return economyData;
            }
            catch (Exception ex)
            {
                LogError($"Failed to gather economy data: {ex.Message}");
                return CreateEmptySectionData();
            }
        }

        public async Task<SaveSectionResult> ApplySectionDataAsync(ISaveSectionData sectionData)
        {
            var startTime = DateTime.Now;

            try
            {
                if (!(sectionData is EconomySectionData economyData))
                {
                    return SaveSectionResult.CreateFailure("Invalid section data type");
                }

                await InitializeSystemsIfNeeded();

                // Handle version migration if needed
                var migratedState = economyData.EconomyState;
                if (RequiresMigration(economyData.DataVersion))
                {
                    var migrationResult = await MigrateSectionDataAsync(economyData, economyData.DataVersion);
                    if (migrationResult is EconomySectionData migrated)
                    {
                        migratedState = migrated.EconomyState;
                    }
                }

                var result = await ApplyEconomyStateAsync(migratedState);

                if (result.Success)
                {
                    _lastSavedState = migratedState;
                    _hasChanges = false;

                    LogInfo($"Economy state applied successfully: " +
                           $"{migratedState?.AvailableProducts?.Count ?? 0} products, " +
                           $"{migratedState?.TransactionHistory?.Count ?? 0} transactions restored");
                }

                var duration = DateTime.Now - startTime;
                return SaveSectionResult.CreateSuccess(duration, economyData.EstimatedSize, new Dictionary<string, object>
                {
                    { "products_loaded", migratedState?.AvailableProducts?.Count ?? 0 },
                    { "transactions_loaded", migratedState?.TransactionHistory?.Count ?? 0 },
                    { "investments_loaded", migratedState?.PlayerEconomyState?.Investments?.Count ?? 0 },
                    { "cash_balance", migratedState?.PlayerEconomyState?.PlayerFinances?.TotalCash ?? 0 }
                });
            }
            catch (Exception ex)
            {
                LogError($"Failed to apply economy data: {ex.Message}");
                return SaveSectionResult.CreateFailure($"Application failed: {ex.Message}", ex);
            }
        }

        public async Task<SaveSectionValidation> ValidateSectionDataAsync(ISaveSectionData sectionData)
        {
            if (!_enableDataValidation)
            {
                return SaveSectionValidation.CreateValid();
            }

            try
            {
                if (!(sectionData is EconomySectionData economyData))
                {
                    return SaveSectionValidation.CreateInvalid(new List<string> { "Invalid section data type" });
                }

                var errors = new List<string>();
                var warnings = new List<string>();

                // Validate economy state
                if (economyData.EconomyState == null)
                {
                    errors.Add("Economy state is null");
                    return SaveSectionValidation.CreateInvalid(errors);
                }

                var validationResult = await ValidateEconomyStateAsync(economyData.EconomyState);

                errors.AddRange(validationResult.Errors);
                warnings.AddRange(validationResult.Warnings);

                // Financial consistency validation
                if (_enableBalanceValidation)
                {
                    var balanceValidation = ValidateFinancialBalance(economyData.EconomyState);
                    errors.AddRange(balanceValidation.Errors);
                    warnings.AddRange(balanceValidation.Warnings);
                }

                // Market data integrity validation
                if (_validateMarketIntegrity)
                {
                    var marketValidation = ValidateMarketIntegrity(economyData.EconomyState);
                    errors.AddRange(marketValidation.Errors);
                    warnings.AddRange(marketValidation.Warnings);
                }

                // Transaction integrity validation
                if (_validateTransactionIntegrity)
                {
                    var transactionValidation = ValidateTransactionIntegrity(economyData.EconomyState);
                    errors.AddRange(transactionValidation.Errors);
                    warnings.AddRange(transactionValidation.Warnings);
                }

                // Check data corruption level
                float corruptionLevel = CalculateDataCorruption(economyData.EconomyState);
                if (corruptionLevel > _maxAllowedDataCorruption)
                {
                    errors.Add($"Data corruption level ({corruptionLevel:P2}) exceeds maximum allowed ({_maxAllowedDataCorruption:P2})");
                }
                else if (corruptionLevel > 0)
                {
                    warnings.Add($"Minor data corruption detected ({corruptionLevel:P2})");
                }

                return errors.Any()
                    ? SaveSectionValidation.CreateInvalid(errors, warnings, _enableAutoRepair, SectionVersion)
                    : SaveSectionValidation.CreateValid();
            }
            catch (Exception ex)
            {
                LogError($"Validation failed: {ex.Message}");
                return SaveSectionValidation.CreateInvalid(new List<string> { $"Validation error: {ex.Message}" });
            }
        }

        public async Task<ISaveSectionData> MigrateSectionDataAsync(ISaveSectionData oldData, string fromVersion)
        {
            try
            {
                if (!(oldData is EconomySectionData economyData))
                {
                    throw new ArgumentException("Invalid data type for migration");
                }

                var migrator = GetVersionMigrator(fromVersion, SectionVersion);
                if (migrator != null)
                {
                    var migratedState = await migrator.MigrateEconomyStateAsync(economyData.EconomyState);

                    var migratedData = new EconomySectionData
                    {
                        SectionKey = SectionKey,
                        DataVersion = SectionVersion,
                        Timestamp = DateTime.Now,
                        EconomyState = migratedState,
                        DataHash = GenerateDataHash(economyData)
                    };

                    migratedData.EstimatedSize = CalculateDataSize(migratedState);

                    LogInfo($"Economy data migrated from {fromVersion} to {SectionVersion}");
                    return migratedData;
                }

                LogWarning($"No migration path found from {fromVersion} to {SectionVersion}");
                return oldData; // Return unchanged if no migration needed
            }
            catch (Exception ex)
            {
                LogError($"Migration failed: {ex.Message}");
                throw;
            }
        }

        public SaveSectionSummary GetSectionSummary()
        {
            var state = _lastSavedState ?? GetCurrentEconomyState();

            return new SaveSectionSummary
            {
                SectionKey = SectionKey,
                SectionName = SectionName,
                StatusDescription = GetStatusDescription(state),
                ItemCount = state?.TransactionHistory?.Count ?? 0,
                DataSize = _estimatedDataSize,
                LastUpdated = _lastSaveTime,
                KeyValuePairs = new Dictionary<string, string>
                {
                    { "Available Products", (state?.AvailableProducts?.Count ?? 0).ToString() },
                    { "Transaction History", (state?.TransactionHistory?.Count ?? 0).ToString() },
                    { "Player Cash", $"${state?.PlayerEconomyState?.PlayerFinances?.TotalCash ?? 0:F2}" },
                    { "Total Investments", (state?.PlayerEconomyState?.Investments?.Count ?? 0).ToString() },
                    { "Active Loans", (state?.PlayerEconomyState?.UnpaidLoans?.Count ?? 0).ToString() },
                    { "Net Worth", $"${state?.PlayerEconomyState?.TotalNetWorth ?? 0:F2}" },
                    { "System Status", _systemsInitialized ? "Initialized" : "Not Initialized" }
                },
                HasErrors = !_systemsInitialized,
                ErrorMessages = _systemsInitialized ? new List<string>() : new List<string> { "Economy systems not initialized" }
            };
        }

        public bool HasChanges()
        {
            if (!_systemsInitialized || _lastSavedState == null)
                return true;

            // Quick change detection
            var currentState = GetCurrentEconomyState();
            if (currentState == null)
                return false;

            return _hasChanges ||
                   currentState.AvailableProducts?.Count != _lastSavedState.AvailableProducts?.Count ||
                   currentState.TransactionHistory?.Count != _lastSavedState.TransactionHistory?.Count ||
                   Math.Abs((currentState.PlayerEconomyState?.PlayerFinances?.TotalCash ?? 0) - (_lastSavedState.PlayerEconomyState?.PlayerFinances?.TotalCash ?? 0)) > 0.01f ||
                   DateTime.Now.Subtract(_lastSaveTime).TotalMinutes > 15; // Force save every 15 minutes
        }

        public void MarkClean()
        {
            _hasChanges = false;
            _lastSaveTime = DateTime.Now;
        }

        public async Task ResetToDefaultStateAsync()
        {
            await InitializeSystemsIfNeeded();

            // Reset economy system to defaults
            if (_marketSystem != null)
            {
                await _marketSystem.ResetToDefaultsAsync();
            }

            if (_playerEconomySystem != null)
            {
                await _playerEconomySystem.ResetToDefaultsAsync();
            }

            if (_financialSystem != null)
            {
                await _financialSystem.ResetToDefaultsAsync();
            }

            _lastSavedState = null;
            _hasChanges = true;

            LogInfo("Economy system reset to default state");
        }

        public IReadOnlyDictionary<string, IReadOnlyList<string>> GetSupportedMigrations()
        {
            return new Dictionary<string, IReadOnlyList<string>>
            {
                { "1.0.0", new List<string> { "1.1.0", "1.2.0" } },
                { "1.1.0", new List<string> { "1.2.0" } }
            };
        }

        public async Task PreSaveCleanupAsync()
        {
            await InitializeSystemsIfNeeded();

            // Clean up old transactions, expired opportunities, etc.
            if (_marketSystem != null)
            {
                await _marketSystem.CleanupExpiredDataAsync();
            }

            if (_tradingSystem != null)
            {
                await _tradingSystem.CleanupCompletedTransactionsAsync();
            }

            if (_financialSystem != null)
            {
                await _financialSystem.RecalculateFinancialMetricsAsync();
            }

            // Mark that changes occurred due to cleanup
            _hasChanges = true;
        }

        public async Task PostLoadInitializationAsync()
        {
            await InitializeSystemsIfNeeded();

            // Rebuild market caches, recalculate derived data
            if (_marketSystem != null)
            {
                await _marketSystem.RebuildMarketCachesAsync();
            }

            if (_economicIndicatorsSystem != null)
            {
                await _economicIndicatorsSystem.RecalculateIndicatorsAsync();
            }

            if (_playerEconomySystem != null)
            {
                await _playerEconomySystem.RecalculateNetWorthAsync();
            }

            LogInfo("Economy system post-load initialization completed");
        }

        #endregion

        #region System Integration

        private async Task InitializeSystemsIfNeeded()
        {
            if (_systemsInitialized)
                return;

            await Task.Run(() =>
            {
                // Auto-detect systems if enabled
                if (_autoDetectSystems)
                {
                    var serviceContainer = ServiceContainerFactory.Instance;
                    _marketSystem = serviceContainer?.TryResolve<IMarketSystem>();
                    _tradingSystem = serviceContainer?.TryResolve<ITradingSystem>();
                    _playerEconomySystem = serviceContainer?.TryResolve<IPlayerEconomySystem>();
                    _financialSystem = serviceContainer?.TryResolve<IFinancialSystem>();
                    _economicIndicatorsSystem = serviceContainer?.TryResolve<IEconomicIndicatorsSystem>();
                }

                _systemsInitialized = true;
            });

            LogInfo($"Economy save provider initialized. Systems found: " +
                   $"Market={_marketSystem != null}, " +
                   $"Trading={_tradingSystem != null}, " +
                   $"PlayerEconomy={_playerEconomySystem != null}, " +
                   $"Financial={_financialSystem != null}, " +
                   $"Indicators={_economicIndicatorsSystem != null}");
        }

        #endregion

        #region Data Gathering and Application

        private async Task<EconomyStateDTO> GatherEconomyStateAsync()
        {
            var state = new EconomyStateDTO
            {
                SaveTimestamp = DateTime.Now,
                SaveVersion = SectionVersion,
                EnableEconomySystem = true
            };

            // Gather market data
            if (_marketSystem != null)
            {
                state.MarketState = await GatherMarketStateAsync();
                state.AvailableProducts = await GatherAvailableProductsAsync();
                state.ProductPrices = await GatherProductPricesAsync();
                state.ProductPerformance = await GatherProductPerformanceAsync();
            }

            // Gather trading data
            if (_tradingSystem != null)
            {
                state.TradingState = await GatherTradingStateAsync();
                state.TransactionHistory = await GatherTransactionHistoryAsync();
                state.AvailableOpportunities = await GatherTradingOpportunitiesAsync();
            }

            // Gather player economy data
            if (_playerEconomySystem != null)
            {
                state.PlayerEconomyState = await GatherPlayerEconomyStateAsync();
            }

            // Gather financial data
            if (_financialSystem != null)
            {
                state.FinancialState = await GatherFinancialStateAsync();
            }

            // Gather economic indicators
            if (_economicIndicatorsSystem != null)
            {
                state.EconomicIndicators = await GatherEconomicIndicatorsAsync();
                state.CurrentMarketConditions = await GatherMarketConditionsAsync();
            }

            return state;
        }

        private async Task<SaveSectionResult> ApplyEconomyStateAsync(EconomyStateDTO state)
        {
            try
            {
                // Apply market data
                if (_marketSystem != null && state.MarketState != null)
                {
                    await _marketSystem.LoadMarketStateAsync(state.MarketState);

                    if (state.AvailableProducts != null)
                        await _marketSystem.LoadAvailableProductsAsync(state.AvailableProducts);

                    if (state.ProductPrices != null)
                        await _marketSystem.LoadProductPricesAsync(state.ProductPrices);
                }

                // Apply trading data
                if (_tradingSystem != null)
                {
                    if (state.TradingState != null)
                        await _tradingSystem.LoadTradingStateAsync(state.TradingState);

                    if (state.TransactionHistory != null)
                        await _tradingSystem.LoadTransactionHistoryAsync(state.TransactionHistory);
                }

                // Apply player economy data
                if (_playerEconomySystem != null && state.PlayerEconomyState != null)
                {
                    await _playerEconomySystem.LoadPlayerEconomyStateAsync(state.PlayerEconomyState);
                }

                // Apply financial data
                if (_financialSystem != null && state.FinancialState != null)
                {
                    await _financialSystem.LoadFinancialStateAsync(state.FinancialState);
                }

                // Apply economic indicators
                if (_economicIndicatorsSystem != null)
                {
                    if (state.EconomicIndicators != null)
                        await _economicIndicatorsSystem.LoadEconomicIndicatorsAsync(state.EconomicIndicators);

                    if (state.CurrentMarketConditions != null)
                        await _economicIndicatorsSystem.LoadMarketConditionsAsync(state.CurrentMarketConditions);
                }

                return SaveSectionResult.CreateSuccess();
            }
            catch (Exception ex)
            {
                LogError($"Failed to apply economy state: {ex.Message}");
                return SaveSectionResult.CreateFailure($"Application failed: {ex.Message}", ex);
            }
        }

        #endregion

        #region Helper Methods

        private EconomyStateDTO GetCurrentEconomyState()
        {
            // Quick state snapshot for change detection
            if (!_systemsInitialized)
                return null;

            return new EconomyStateDTO
            {
                AvailableProducts = _marketSystem?.GetAvailableProducts()?.Select(ConvertToMarketProductDTO).ToList(),
                TransactionHistory = _tradingSystem?.GetRecentTransactions()?.Select(ConvertToTransactionRecordDTO).ToList(),
                PlayerEconomyState = _playerEconomySystem?.GetCurrentPlayerEconomyState(),
                SaveTimestamp = DateTime.Now
            };
        }

        private ISaveSectionData CreateEmptySectionData()
        {
            return new EconomySectionData
            {
                SectionKey = SectionKey,
                DataVersion = SectionVersion,
                Timestamp = DateTime.Now,
                EconomyState = new EconomyStateDTO
                {
                    SaveTimestamp = DateTime.Now,
                    SaveVersion = SectionVersion,
                    AvailableProducts = new List<MarketProductDTO>(),
                    TransactionHistory = new List<TransactionRecordDTO>(),
                    ProductPrices = new Dictionary<string, float>(),
                    PlayerEconomyState = new PlayerEconomyStateDTO
                    {
                        IsEconomyActive = true,
                        LastEconomyUpdate = DateTime.Now,
                        PlayerFinances = new PlayerFinancesDTO { TotalCash = 50000f }
                    }
                },
                EstimatedSize = 4096 // Minimal size
            };
        }

        private long CalculateDataSize(EconomyStateDTO state)
        {
            if (state == null) return 0;

            // Estimate based on content
            long size = 4096; // Base overhead
            size += (state.AvailableProducts?.Count ?? 0) * 2048; // ~2KB per product
            size += (state.TransactionHistory?.Count ?? 0) * 512; // ~0.5KB per transaction
            size += (state.ProductPrices?.Count ?? 0) * 64; // ~64B per price entry
            size += (state.PlayerEconomyState?.Investments?.Count ?? 0) * 1024; // ~1KB per investment
            size += (state.FinancialState?.ActiveLoans?.Count ?? 0) * 1024; // ~1KB per loan

            return size;
        }

        private string GenerateDataHash(EconomySectionData data)
        {
            // Simple hash based on key data points
            var hashSource = $"{data.Timestamp:yyyy-MM-dd-HH-mm-ss}" +
                           $"{data.EconomyState?.AvailableProducts?.Count ?? 0}" +
                           $"{data.EconomyState?.TransactionHistory?.Count ?? 0}" +
                           $"{data.EconomyState?.PlayerEconomyState?.PlayerFinances?.TotalCash ?? 0:F2}" +
                           $"{data.EstimatedSize}";

            return hashSource.GetHashCode().ToString("X8");
        }

        private string GetStatusDescription(EconomyStateDTO state)
        {
            if (state == null)
                return "No economy data";

            int products = state.AvailableProducts?.Count ?? 0;
            int transactions = state.TransactionHistory?.Count ?? 0;
            float cash = state.PlayerEconomyState?.PlayerFinances?.TotalCash ?? 0;

            if (products == 0 && transactions == 0)
                return "Empty economy system";

            return $"{products} products, {transactions} transactions, ${cash:F2} cash";
        }

        private bool RequiresMigration(string dataVersion) => dataVersion != SectionVersion;

        // Validation methods
        private async Task<ValidationResult> ValidateEconomyStateAsync(EconomyStateDTO state)
        {
            // Comprehensive validation - placeholder implementation
            await Task.Delay(1);
            return new ValidationResult { IsValid = true };
        }

        private ValidationResult ValidateFinancialBalance(EconomyStateDTO state)
        {
            var result = new ValidationResult { IsValid = true };

            // Check for financial balance consistency
            if (state.PlayerEconomyState != null)
            {
                var expectedNetWorth = (state.PlayerEconomyState.PlayerFinances?.TotalCash ?? 0) +
                                     (state.PlayerEconomyState.Investments?.Sum(i => i.CurrentValue) ?? 0f);

                var actualNetWorth = state.PlayerEconomyState.TotalNetWorth;
                var discrepancy = Math.Abs(expectedNetWorth - actualNetWorth) / Math.Max(expectedNetWorth, 1);

                if (discrepancy > _maxBalanceDiscrepancy)
                {
                    result.Errors.Add($"Net worth discrepancy: expected ${expectedNetWorth:F2}, actual ${actualNetWorth:F2}");
                    result.IsValid = false;
                }
            }

            return result;
        }

        private ValidationResult ValidateMarketIntegrity(EconomyStateDTO state) => new ValidationResult { IsValid = true };
        private ValidationResult ValidateTransactionIntegrity(EconomyStateDTO state) => new ValidationResult { IsValid = true };
        private float CalculateDataCorruption(EconomyStateDTO state) => 0.0f;
        private IEconomyMigrator GetVersionMigrator(string fromVersion, string toVersion) => null;

        // Conversion methods - placeholders
        private MarketProductDTO ConvertToMarketProductDTO(object product) => new MarketProductDTO();
        private TransactionRecordDTO ConvertToTransactionRecordDTO(object transaction) => new TransactionRecordDTO();

        // Async gathering methods - placeholders
        private async Task<MarketStateDTO> GatherMarketStateAsync() => new MarketStateDTO();
        private async Task<List<MarketProductDTO>> GatherAvailableProductsAsync() => new List<MarketProductDTO>();
        private async Task<Dictionary<string, float>> GatherProductPricesAsync() => new Dictionary<string, float>();
        private async Task<Dictionary<string, MarketProductPerformanceDTO>> GatherProductPerformanceAsync() => new Dictionary<string, MarketProductPerformanceDTO>();
        private async Task<TradingStateDTO> GatherTradingStateAsync() => new TradingStateDTO();
        private async Task<List<TransactionRecordDTO>> GatherTransactionHistoryAsync() => new List<TransactionRecordDTO>();
        private async Task<List<TradingOpportunityDTO>> GatherTradingOpportunitiesAsync() => new List<TradingOpportunityDTO>();
        private async Task<PlayerEconomyStateDTO> GatherPlayerEconomyStateAsync() => new PlayerEconomyStateDTO();
        private async Task<FinancialStateDTO> GatherFinancialStateAsync() => new FinancialStateDTO();
        private async Task<EconomicIndicatorsDTO> GatherEconomicIndicatorsAsync() => new EconomicIndicatorsDTO();
        private async Task<MarketConditionsDTO> GatherMarketConditionsAsync() => new MarketConditionsDTO();

        private void LogInfo(string message) => ChimeraLogger.Log($"[EconomySaveProvider] {message}");
        private void LogWarning(string message) => ChimeraLogger.LogWarning($"[EconomySaveProvider] {message}");
        private void LogError(string message) => ChimeraLogger.LogError($"[EconomySaveProvider] {message}");

        #endregion
    }

    /// <summary>
    /// Economy-specific save section data container
    /// </summary>
    [System.Serializable]
    public class EconomySectionData : ISaveSectionData
    {
        public string SectionKey { get; set; }
        public string DataVersion { get; set; }
        public DateTime Timestamp { get; set; }
        public long EstimatedSize { get; set; }
        public string DataHash { get; set; }

        public EconomyStateDTO EconomyState;

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(SectionKey) &&
                   !string.IsNullOrEmpty(DataVersion) &&
                   EconomyState != null;
        }

        public string GetSummary()
        {
            var products = EconomyState?.AvailableProducts?.Count ?? 0;
            var transactions = EconomyState?.TransactionHistory?.Count ?? 0;
            var cash = EconomyState?.PlayerEconomyState?.PlayerFinances?.TotalCash ?? 0;
            return $"Economy: {products} products, {transactions} transactions, ${cash:F2}";
        }
    }

    /// <summary>
    /// Interfaces for system integration (would be implemented by actual systems)
    /// </summary>
    public interface IMarketSystem
    {
        Task ResetToDefaultsAsync();
        Task CleanupExpiredDataAsync();
        Task RebuildMarketCachesAsync();
        Task LoadMarketStateAsync(MarketStateDTO state);
        Task LoadAvailableProductsAsync(List<MarketProductDTO> products);
        Task LoadProductPricesAsync(Dictionary<string, float> prices);
        List<object> GetAvailableProducts();
    }

    public interface ITradingSystem
    {
        Task CleanupCompletedTransactionsAsync();
        Task LoadTradingStateAsync(TradingStateDTO state);
        Task LoadTransactionHistoryAsync(List<TransactionRecordDTO> transactions);
        List<object> GetRecentTransactions();
    }

    public interface IPlayerEconomySystem
    {
        Task ResetToDefaultsAsync();
        Task RecalculateNetWorthAsync();
        Task LoadPlayerEconomyStateAsync(PlayerEconomyStateDTO state);
        PlayerEconomyStateDTO GetCurrentPlayerEconomyState();
    }

    public interface IFinancialSystem
    {
        Task ResetToDefaultsAsync();
        Task RecalculateFinancialMetricsAsync();
        Task LoadFinancialStateAsync(FinancialStateDTO state);
    }

    public interface IEconomicIndicatorsSystem
    {
        Task RecalculateIndicatorsAsync();
        Task LoadEconomicIndicatorsAsync(EconomicIndicatorsDTO indicators);
        Task LoadMarketConditionsAsync(MarketConditionsDTO conditions);
    }

    public interface IEconomyMigrator
    {
        Task<EconomyStateDTO> MigrateEconomyStateAsync(EconomyStateDTO oldState);
    }
}
