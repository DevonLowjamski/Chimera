using ProjectChimera.Core.Logging;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ProjectChimera.Data.Save;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Save
{
    /// <summary>
    /// Domain-specific save provider for the cultivation system.
    /// Handles plant instances, genetics, strains, environmental data, and cultivation metrics.
    /// Implements comprehensive validation, migration, and state management.
    /// </summary>
    public class CultivationSaveProvider : MonoBehaviour, ISaveSectionProvider
    {
        [Header("Provider Configuration")]
        [SerializeField] private string _sectionVersion = "1.2.0";
        [SerializeField] private int _priority = (int)SaveSectionPriority.High;
        [SerializeField] private bool _enableIncrementalSave = true;
        [SerializeField] private int _maxPlantsPerSave = 1000;

        [Header("Data Sources")]
        [SerializeField] private bool _autoDetectSystems = true;
        [SerializeField] private Transform _cultivationSystemRoot;
        [SerializeField] private string[] _plantManagerTags = { "PlantManager", "CultivationManager" };

        [Header("Validation Settings")]
        [SerializeField] private bool _enableDataValidation = true;
        [SerializeField] private bool _validateGeneticConsistency = true;
        [SerializeField] private float _maxAllowedDataCorruption = 0.05f; // 5%
        [SerializeField] private bool _enableAutoRepair = true;

        // System references
        private ICultivationSystem _cultivationSystem;
        private IGeneticsSystem _geneticsSystem;
        private IPlantManager _plantManager;
        private bool _systemsInitialized = false;

        // State tracking
        private CultivationStateDTO _lastSavedState;
        private DateTime _lastSaveTime;
        private bool _hasChanges = true;
        private long _estimatedDataSize = 0;

        // Dependencies
        private readonly string[] _dependencies = { 
            SaveSectionKeys.PLAYER, 
            SaveSectionKeys.SETTINGS, 
            SaveSectionKeys.TIME, 
            SaveSectionKeys.ECONOMY 
        };

        #region ISaveSectionProvider Implementation

        public string SectionKey => SaveSectionKeys.CULTIVATION;
        public string SectionName => "Plant Cultivation System";
        public string SectionVersion => _sectionVersion;
        public int Priority => _priority;
        public bool IsRequired => false;
        public bool SupportsIncrementalSave => _enableIncrementalSave;
        public long EstimatedDataSize => _estimatedDataSize;
        public IReadOnlyList<string> Dependencies => _dependencies;

        public async Task<ISaveSectionData> GatherSectionDataAsync()
        {
            await InitializeSystemsIfNeeded();

            var cultivationData = new CultivationSectionData
            {
                SectionKey = SectionKey,
                DataVersion = SectionVersion,
                Timestamp = DateTime.Now
            };

            try
            {
                // Gather core cultivation state
                cultivationData.CultivationState = await GatherCultivationStateAsync();
                
                // Calculate data size and hash
                cultivationData.EstimatedSize = CalculateDataSize(cultivationData.CultivationState);
                cultivationData.DataHash = GenerateDataHash(cultivationData);

                _estimatedDataSize = cultivationData.EstimatedSize;
                _lastSavedState = cultivationData.CultivationState;
                _lastSaveTime = DateTime.Now;
                _hasChanges = false;

                LogInfo($"Cultivation data gathered: {cultivationData.CultivationState?.ActivePlants?.Count ?? 0} plants, " +
                       $"{cultivationData.CultivationState?.AvailableStrains?.Count ?? 0} strains, " +
                       $"Data size: {cultivationData.EstimatedSize} bytes");

                return cultivationData;
            }
            catch (Exception ex)
            {
                LogError($"Failed to gather cultivation data: {ex.Message}");
                return CreateEmptySectionData();
            }
        }

        public async Task<SaveSectionResult> ApplySectionDataAsync(ISaveSectionData sectionData)
        {
            var startTime = DateTime.Now;
            
            try
            {
                if (!(sectionData is CultivationSectionData cultivationData))
                {
                    return SaveSectionResult.CreateFailure("Invalid section data type");
                }

                await InitializeSystemsIfNeeded();

                var result = await ApplyCultivationStateAsync(cultivationData.CultivationState);
                
                if (result.Success)
                {
                    _lastSavedState = cultivationData.CultivationState;
                    _hasChanges = false;

                    LogInfo($"Cultivation state applied successfully: {cultivationData.CultivationState?.ActivePlants?.Count ?? 0} plants restored");
                }

                var duration = DateTime.Now - startTime;
                return SaveSectionResult.CreateSuccess(duration, cultivationData.EstimatedSize, new Dictionary<string, object>
                {
                    { "plants_loaded", cultivationData.CultivationState?.ActivePlants?.Count ?? 0 },
                    { "strains_loaded", cultivationData.CultivationState?.AvailableStrains?.Count ?? 0 }
                });
            }
            catch (Exception ex)
            {
                LogError($"Failed to apply cultivation data: {ex.Message}");
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
                if (!(sectionData is CultivationSectionData cultivationData))
                {
                    return SaveSectionValidation.CreateInvalid(new List<string> { "Invalid section data type" });
                }

                var errors = new List<string>();
                var warnings = new List<string>();

                // Validate cultivation state
                if (cultivationData.CultivationState == null)
                {
                    errors.Add("Cultivation state is null");
                    return SaveSectionValidation.CreateInvalid(errors);
                }

                var validationResult = await ValidateCultivationStateAsync(cultivationData.CultivationState);
                
                errors.AddRange(validationResult.Errors);
                warnings.AddRange(validationResult.Warnings);

                // Check data corruption level
                float corruptionLevel = CalculateDataCorruption(cultivationData.CultivationState);
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
                if (!(oldData is CultivationSectionData cultivationData))
                {
                    throw new ArgumentException("Invalid data type for migration");
                }

                var migrator = GetVersionMigrator(fromVersion, SectionVersion);
                if (migrator != null)
                {
                    var migratedState = await migrator.MigrateCultivationStateAsync(cultivationData.CultivationState);
                    
                    var migratedData = new CultivationSectionData
                    {
                        SectionKey = SectionKey,
                        DataVersion = SectionVersion,
                        Timestamp = DateTime.Now,
                        CultivationState = migratedState,
                        DataHash = GenerateDataHash(cultivationData)
                    };

                    migratedData.EstimatedSize = CalculateDataSize(migratedState);

                    LogInfo($"Cultivation data migrated from {fromVersion} to {SectionVersion}");
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
            var state = _lastSavedState ?? GetCurrentCultivationState();
            
            return new SaveSectionSummary
            {
                SectionKey = SectionKey,
                SectionName = SectionName,
                StatusDescription = GetStatusDescription(state),
                ItemCount = state?.ActivePlants?.Count ?? 0,
                DataSize = _estimatedDataSize,
                LastUpdated = _lastSaveTime,
                KeyValuePairs = new Dictionary<string, string>
                {
                    { "Active Plants", (state?.ActivePlants?.Count ?? 0).ToString() },
                    { "Available Strains", (state?.AvailableStrains?.Count ?? 0).ToString() },
                    { "Cultivation Zones", (state?.CultivationZones?.Count ?? 0).ToString() },
                    { "Total Harvests", (state?.HarvestState?.TotalHarvests ?? 0).ToString() },
                    { "System Status", _systemsInitialized ? "Initialized" : "Not Initialized" }
                },
                HasErrors = !_systemsInitialized,
                ErrorMessages = _systemsInitialized ? new List<string>() : new List<string> { "Cultivation systems not initialized" }
            };
        }

        public bool HasChanges()
        {
            if (!_systemsInitialized || _lastSavedState == null)
                return true;

            // Quick change detection
            var currentState = GetCurrentCultivationState();
            if (currentState == null)
                return false;

            return _hasChanges || 
                   currentState.ActivePlants?.Count != _lastSavedState.ActivePlants?.Count ||
                   currentState.AvailableStrains?.Count != _lastSavedState.AvailableStrains?.Count ||
                   DateTime.Now.Subtract(_lastSaveTime).TotalMinutes > 5; // Force save every 5 minutes
        }

        public void MarkClean()
        {
            _hasChanges = false;
            _lastSaveTime = DateTime.Now;
        }

        public async Task ResetToDefaultStateAsync()
        {
            await InitializeSystemsIfNeeded();

            // Reset cultivation system to defaults
            if (_cultivationSystem != null)
            {
                await _cultivationSystem.ResetToDefaultsAsync();
            }

            _lastSavedState = null;
            _hasChanges = true;
            
            LogInfo("Cultivation system reset to default state");
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

            // Clean up temporary data, expired health events, etc.
            if (_cultivationSystem != null)
            {
                await _cultivationSystem.CleanupTemporaryDataAsync();
            }

            // Mark that changes occurred due to cleanup
            _hasChanges = true;
        }

        public async Task PostLoadInitializationAsync()
        {
            await InitializeSystemsIfNeeded();

            // Rebuild caches, recalculate derived data
            if (_cultivationSystem != null)
            {
                await _cultivationSystem.RebuildCachesAsync();
            }

            if (_geneticsSystem != null)
            {
                await _geneticsSystem.RecalculateGeneticDataAsync();
            }

            LogInfo("Cultivation system post-load initialization completed");
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
                    _cultivationSystem = ServiceContainerFactory.Instance?.TryResolve<ICultivationSystem>();
                    
                    _geneticsSystem = ServiceContainerFactory.Instance?.TryResolve<IGeneticsSystem>();
                    
                    _plantManager = ServiceContainerFactory.Instance?.TryResolve<IPlantManager>();
                }

                _systemsInitialized = true;
            });

            LogInfo($"Cultivation save provider initialized. Systems found: " +
                   $"Cultivation={_cultivationSystem != null}, " +
                   $"Genetics={_geneticsSystem != null}, " +
                   $"PlantManager={_plantManager != null}");
        }

        #endregion

        #region Data Gathering and Application

        private async Task<CultivationStateDTO> GatherCultivationStateAsync()
        {
            var state = new CultivationStateDTO
            {
                SaveTimestamp = DateTime.Now,
                SaveVersion = SectionVersion,
                EnableCultivationSystem = true
            };

            // Gather plant data
            if (_plantManager != null)
            {
                state.ActivePlants = await GatherActivePlantsAsync();
                state.PlantPositions = await GatherPlantPositionsAsync();
                state.PlantZoneAssignments = await GatherPlantZoneAssignmentsAsync();
            }

            // Gather genetic data
            if (_geneticsSystem != null)
            {
                state.AvailableStrains = await GatherAvailableStrainsAsync();
                state.StoredGenotypes = await GatherStoredGenotypesAsync();
                state.GeneticLibrary = await GatherGeneticLibraryStateAsync();
            }

            // Gather cultivation zones
            if (_cultivationSystem != null)
            {
                state.CultivationZones = await GatherCultivationZonesAsync();
                state.LifecycleState = await GatherLifecycleStateAsync();
                state.HarvestState = await GatherHarvestStateAsync();
                state.EnvironmentalState = await GatherEnvironmentalStateAsync();
                state.PlantCareState = await GatherPlantCareStateAsync();
                state.Metrics = await GatherCultivationMetricsAsync();
                state.Performance = await GatherCultivationPerformanceAsync();
            }

            return state;
        }

        private async Task<SaveSectionResult> ApplyCultivationStateAsync(CultivationStateDTO state)
        {
            try
            {
                // Apply plant data
                if (_plantManager != null && state.ActivePlants != null)
                {
                    await _plantManager.LoadPlantsAsync(state.ActivePlants);
                    await _plantManager.SetPlantPositionsAsync(state.PlantPositions);
                    await _plantManager.SetPlantZoneAssignmentsAsync(state.PlantZoneAssignments);
                }

                // Apply genetic data
                if (_geneticsSystem != null)
                {
                    if (state.AvailableStrains != null)
                        await _geneticsSystem.LoadStrainsAsync(state.AvailableStrains);
                    
                    if (state.StoredGenotypes != null)
                        await _geneticsSystem.LoadGenotypesAsync(state.StoredGenotypes);
                    
                    if (state.GeneticLibrary != null)
                        await _geneticsSystem.LoadGeneticLibraryAsync(state.GeneticLibrary);
                }

                // Apply cultivation system data
                if (_cultivationSystem != null)
                {
                    if (state.CultivationZones != null)
                        await _cultivationSystem.LoadCultivationZonesAsync(state.CultivationZones);
                    
                    if (state.LifecycleState != null)
                        await _cultivationSystem.LoadLifecycleStateAsync(state.LifecycleState);
                    
                    if (state.HarvestState != null)
                        await _cultivationSystem.LoadHarvestStateAsync(state.HarvestState);
                    
                    if (state.EnvironmentalState != null)
                        await _cultivationSystem.LoadEnvironmentalStateAsync(state.EnvironmentalState);
                    
                    if (state.PlantCareState != null)
                        await _cultivationSystem.LoadPlantCareStateAsync(state.PlantCareState);
                }

                return SaveSectionResult.CreateSuccess();
            }
            catch (Exception ex)
            {
                LogError($"Failed to apply cultivation state: {ex.Message}");
                return SaveSectionResult.CreateFailure($"Application failed: {ex.Message}", ex);
            }
        }

        #endregion

        #region Helper Methods

        private CultivationStateDTO GetCurrentCultivationState()
        {
            // Quick state snapshot for change detection
            if (!_systemsInitialized)
                return null;

            return new CultivationStateDTO
            {
                ActivePlants = _plantManager?.GetActivePlants()?.Select(ConvertToPlantInstanceDTO).ToList(),
                AvailableStrains = _geneticsSystem?.GetAvailableStrains()?.Select(ConvertToPlantStrainDTO).ToList(),
                CultivationZones = _cultivationSystem?.GetCultivationZones()?.Select(ConvertToCultivationZoneDTO).ToList(),
                SaveTimestamp = DateTime.Now
            };
        }

        private ISaveSectionData CreateEmptySectionData()
        {
            return new CultivationSectionData
            {
                SectionKey = SectionKey,
                DataVersion = SectionVersion,
                Timestamp = DateTime.Now,
                CultivationState = new CultivationStateDTO
                {
                    SaveTimestamp = DateTime.Now,
                    SaveVersion = SectionVersion,
                    ActivePlants = new List<PlantInstanceDTO>(),
                    AvailableStrains = new List<PlantStrainDTO>(),
                    CultivationZones = new List<CultivationZoneDTO>()
                },
                EstimatedSize = 1024 // Minimal size
            };
        }

        private long CalculateDataSize(CultivationStateDTO state)
        {
            if (state == null) return 0;

            // Estimate based on content
            long size = 1024; // Base overhead
            size += (state.ActivePlants?.Count ?? 0) * 2048; // ~2KB per plant
            size += (state.AvailableStrains?.Count ?? 0) * 1024; // ~1KB per strain
            size += (state.CultivationZones?.Count ?? 0) * 512; // ~0.5KB per zone

            return size;
        }

        private string GenerateDataHash(CultivationSectionData data)
        {
            // Simple hash based on key data points
            var hashSource = $"{data.Timestamp:yyyy-MM-dd-HH-mm-ss}" +
                           $"{data.CultivationState?.ActivePlants?.Count ?? 0}" +
                           $"{data.CultivationState?.AvailableStrains?.Count ?? 0}" +
                           $"{data.EstimatedSize}";
            
            return hashSource.GetHashCode().ToString("X8");
        }

        private string GetStatusDescription(CultivationStateDTO state)
        {
            if (state == null)
                return "No cultivation data";

            int plants = state.ActivePlants?.Count ?? 0;
            int strains = state.AvailableStrains?.Count ?? 0;
            
            if (plants == 0 && strains == 0)
                return "Empty cultivation system";
            
            if (plants == 0)
                return $"{strains} strains available, no active plants";
            
            return $"{plants} active plants, {strains} strains available";
        }

        // Placeholder conversion methods - would be implemented with actual conversion logic
        private PlantInstanceDTO ConvertToPlantInstanceDTO(object plant) => new PlantInstanceDTO();
        private PlantStrainDTO ConvertToPlantStrainDTO(object strain) => new PlantStrainDTO();
        private CultivationZoneDTO ConvertToCultivationZoneDTO(object zone) => new CultivationZoneDTO();

        // Placeholder async methods - would be implemented with actual system integration
        private async Task<List<PlantInstanceDTO>> GatherActivePlantsAsync() => new List<PlantInstanceDTO>();
        private async Task<Dictionary<string, Vector3>> GatherPlantPositionsAsync() => new Dictionary<string, Vector3>();
        private async Task<Dictionary<string, string>> GatherPlantZoneAssignmentsAsync() => new Dictionary<string, string>();
        private async Task<List<PlantStrainDTO>> GatherAvailableStrainsAsync() => new List<PlantStrainDTO>();
        private async Task<List<GenotypeDTO>> GatherStoredGenotypesAsync() => new List<GenotypeDTO>();
        private async Task<GeneticLibraryStateDTO> GatherGeneticLibraryStateAsync() => new GeneticLibraryStateDTO();
        private async Task<List<CultivationZoneDTO>> GatherCultivationZonesAsync() => new List<CultivationZoneDTO>();
        private async Task<PlantLifecycleStateDTO> GatherLifecycleStateAsync() => new PlantLifecycleStateDTO();
        private async Task<HarvestStateDTO> GatherHarvestStateAsync() => new HarvestStateDTO();
        private async Task<CultivationEnvironmentalStateDTO> GatherEnvironmentalStateAsync() => new CultivationEnvironmentalStateDTO();
        private async Task<PlantCareStateDTO> GatherPlantCareStateAsync() => new PlantCareStateDTO();
        private async Task<CultivationMetricsDTO> GatherCultivationMetricsAsync() => new CultivationMetricsDTO();
        private async Task<CultivationPerformanceDTO> GatherCultivationPerformanceAsync() => new CultivationPerformanceDTO();

        private async Task<CultivationValidationResult> ValidateCultivationStateAsync(CultivationStateDTO state)
        {
            // Comprehensive validation - placeholder implementation
            await Task.Delay(1);
            return new CultivationValidationResult { IsValid = true };
        }

        private float CalculateDataCorruption(CultivationStateDTO state) => 0.0f; // Placeholder

        private ICultivationMigrator GetVersionMigrator(string fromVersion, string toVersion) => null; // Placeholder

        private void LogInfo(string message) => ChimeraLogger.Log($"[CultivationSaveProvider] {message}");
        private void LogWarning(string message) => ChimeraLogger.LogWarning($"[CultivationSaveProvider] {message}");
        private void LogError(string message) => ChimeraLogger.LogError($"[CultivationSaveProvider] {message}");

        #endregion
    }

    /// <summary>
    /// Cultivation-specific save section data container
    /// </summary>
    [System.Serializable]
    public class CultivationSectionData : ISaveSectionData
    {
        public string SectionKey { get; set; }
        public string DataVersion { get; set; }
        public DateTime Timestamp { get; set; }
        public long EstimatedSize { get; set; }
        public string DataHash { get; set; }

        public CultivationStateDTO CultivationState;

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(SectionKey) && 
                   !string.IsNullOrEmpty(DataVersion) && 
                   CultivationState != null;
        }

        public string GetSummary()
        {
            var plants = CultivationState?.ActivePlants?.Count ?? 0;
            var strains = CultivationState?.AvailableStrains?.Count ?? 0;
            return $"Cultivation: {plants} plants, {strains} strains";
        }
    }

    /// <summary>
    /// Interfaces for system integration (would be implemented by actual systems)
    /// </summary>
    public interface ICultivationSystem
    {
        Task ResetToDefaultsAsync();
        Task CleanupTemporaryDataAsync();
        Task RebuildCachesAsync();
        Task LoadCultivationZonesAsync(List<CultivationZoneDTO> zones);
        Task LoadLifecycleStateAsync(PlantLifecycleStateDTO state);
        Task LoadHarvestStateAsync(HarvestStateDTO state);
        Task LoadEnvironmentalStateAsync(CultivationEnvironmentalStateDTO state);
        Task LoadPlantCareStateAsync(PlantCareStateDTO state);
        List<object> GetCultivationZones();
    }

    public interface IGeneticsSystem
    {
        Task RecalculateGeneticDataAsync();
        Task LoadStrainsAsync(List<PlantStrainDTO> strains);
        Task LoadGenotypesAsync(List<GenotypeDTO> genotypes);
        Task LoadGeneticLibraryAsync(GeneticLibraryStateDTO library);
        List<object> GetAvailableStrains();
    }

    public interface IPlantManager
    {
        Task LoadPlantsAsync(List<PlantInstanceDTO> plants);
        Task SetPlantPositionsAsync(Dictionary<string, Vector3> positions);
        Task SetPlantZoneAssignmentsAsync(Dictionary<string, string> assignments);
        List<object> GetActivePlants();
    }

    public interface ICultivationMigrator
    {
        Task<CultivationStateDTO> MigrateCultivationStateAsync(CultivationStateDTO oldState);
    }
}