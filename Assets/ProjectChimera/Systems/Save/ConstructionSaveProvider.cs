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
    /// Domain-specific save provider for the construction system.
    /// Handles grid-based building, facility construction, equipment placement,
    /// and utility systems with full 3D coordinate support.
    /// </summary>
    public class ConstructionSaveProvider : MonoBehaviour, ISaveSectionProvider
    {
        [Header("Provider Configuration")]
        [SerializeField] private string _sectionVersion = "1.2.0";
        [SerializeField] private int _priority = (int)SaveSectionPriority.High;
        [SerializeField] private bool _enableIncrementalSave = true;
        [SerializeField] private int _maxPlacedObjectsPerSave = 2000;

        [Header("Data Sources")]
        [SerializeField] private bool _autoDetectSystems = true;
        [SerializeField] private Transform _constructionSystemRoot;
        [SerializeField] private string[] _gridManagerTags = { "GridManager", "ConstructionManager" };

        [Header("Validation Settings")]
        [SerializeField] private bool _enableDataValidation = true;
        [SerializeField] private bool _validateGridIntegrity = true;
        [SerializeField] private bool _validateStructuralIntegrity = true;
        [SerializeField] private float _maxAllowedDataCorruption = 0.03f; // 3%
        [SerializeField] private bool _enableAutoRepair = true;

        [Header("Migration Settings")]
        [SerializeField] private bool _autoMigrate2DTo3D = true;
        [SerializeField] private bool _preserveLegacyCoordinates = true;

        // System references
        private IConstructionSystem _constructionSystem;
        private IGridManager _gridManager;
        private IPlacementSystem _placementSystem;
        private IUtilitySystem _utilitySystem;
        private bool _systemsInitialized = false;

        // State tracking
        private ConstructionStateDTO _lastSavedState;
        private DateTime _lastSaveTime;
        private bool _hasChanges = true;
        private long _estimatedDataSize = 0;

        // Dependencies
        private readonly string[] _dependencies = { 
            SaveSectionKeys.PLAYER, 
            SaveSectionKeys.SETTINGS, 
            SaveSectionKeys.ECONOMY 
        };

        #region ISaveSectionProvider Implementation

        public string SectionKey => SaveSectionKeys.CONSTRUCTION;
        public string SectionName => "Construction & Grid System";
        public string SectionVersion => _sectionVersion;
        public int Priority => _priority;
        public bool IsRequired => false;
        public bool SupportsIncrementalSave => _enableIncrementalSave;
        public long EstimatedDataSize => _estimatedDataSize;
        public IReadOnlyList<string> Dependencies => _dependencies;

        public async Task<ISaveSectionData> GatherSectionDataAsync()
        {
            await InitializeSystemsIfNeeded();

            var constructionData = new ConstructionSectionData
            {
                SectionKey = SectionKey,
                DataVersion = SectionVersion,
                Timestamp = DateTime.Now
            };

            try
            {
                // Gather core construction state
                constructionData.ConstructionState = await GatherConstructionStateAsync();
                
                // Calculate data size and hash
                constructionData.EstimatedSize = CalculateDataSize(constructionData.ConstructionState);
                constructionData.DataHash = GenerateDataHash(constructionData);

                _estimatedDataSize = constructionData.EstimatedSize;
                _lastSavedState = constructionData.ConstructionState;
                _lastSaveTime = DateTime.Now;
                _hasChanges = false;

                LogInfo($"Construction data gathered: {constructionData.ConstructionState?.PlacedObjects?.Count ?? 0} objects, " +
                       $"{constructionData.ConstructionState?.Rooms?.Count ?? 0} rooms, " +
                       $"{constructionData.ConstructionState?.ActiveProjects?.Count ?? 0} active projects, " +
                       $"Data size: {constructionData.EstimatedSize} bytes");

                return constructionData;
            }
            catch (Exception ex)
            {
                LogError($"Failed to gather construction data: {ex.Message}");
                return CreateEmptySectionData();
            }
        }

        public async Task<SaveSectionResult> ApplySectionDataAsync(ISaveSectionData sectionData)
        {
            var startTime = DateTime.Now;
            
            try
            {
                if (!(sectionData is ConstructionSectionData constructionData))
                {
                    return SaveSectionResult.CreateFailure("Invalid section data type");
                }

                await InitializeSystemsIfNeeded();

                // Handle version migration if needed
                var migratedState = constructionData.ConstructionState;
                if (RequiresMigration(constructionData.DataVersion))
                {
                    var migrationResult = await MigrateSectionDataAsync(constructionData, constructionData.DataVersion);
                    if (migrationResult is ConstructionSectionData migrated)
                    {
                        migratedState = migrated.ConstructionState;
                    }
                }

                var result = await ApplyConstructionStateAsync(migratedState);
                
                if (result.Success)
                {
                    _lastSavedState = migratedState;
                    _hasChanges = false;

                    LogInfo($"Construction state applied successfully: " +
                           $"{migratedState?.PlacedObjects?.Count ?? 0} objects, " +
                           $"{migratedState?.Rooms?.Count ?? 0} rooms restored");
                }

                var duration = DateTime.Now - startTime;
                return SaveSectionResult.CreateSuccess(duration, constructionData.EstimatedSize, new Dictionary<string, object>
                {
                    { "objects_loaded", migratedState?.PlacedObjects?.Count ?? 0 },
                    { "rooms_loaded", migratedState?.Rooms?.Count ?? 0 },
                    { "projects_loaded", migratedState?.ActiveProjects?.Count ?? 0 },
                    { "grid_cells_loaded", migratedState?.GridSystem?.GridCells?.Count ?? 0 }
                });
            }
            catch (Exception ex)
            {
                LogError($"Failed to apply construction data: {ex.Message}");
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
                if (!(sectionData is ConstructionSectionData constructionData))
                {
                    return SaveSectionValidation.CreateInvalid(new List<string> { "Invalid section data type" });
                }

                var errors = new List<string>();
                var warnings = new List<string>();

                // Validate construction state
                if (constructionData.ConstructionState == null)
                {
                    errors.Add("Construction state is null");
                    return SaveSectionValidation.CreateInvalid(errors);
                }

                var validationResult = await ValidateConstructionStateAsync(constructionData.ConstructionState);
                
                errors.AddRange(validationResult.Errors);
                warnings.AddRange(validationResult.Warnings);

                // Check for 2D to 3D migration needs
                if (HasLegacy2DData(constructionData.ConstructionState))
                {
                    if (_autoMigrate2DTo3D)
                    {
                        warnings.Add("Legacy 2D coordinates detected - automatic migration will be performed");
                    }
                    else
                    {
                        errors.Add("Legacy 2D coordinates detected but migration is disabled");
                    }
                }

                // Check data corruption level
                float corruptionLevel = CalculateDataCorruption(constructionData.ConstructionState);
                if (corruptionLevel > _maxAllowedDataCorruption)
                {
                    errors.Add($"Data corruption level ({corruptionLevel:P2}) exceeds maximum allowed ({_maxAllowedDataCorruption:P2})");
                }
                else if (corruptionLevel > 0)
                {
                    warnings.Add($"Minor data corruption detected ({corruptionLevel:P2})");
                }

                // Grid integrity validation
                if (_validateGridIntegrity && constructionData.ConstructionState.GridSystem != null)
                {
                    var gridValidation = ValidateGridIntegrity(constructionData.ConstructionState.GridSystem);
                    errors.AddRange(gridValidation.Errors);
                    warnings.AddRange(gridValidation.Warnings);
                }

                // Structural integrity validation
                if (_validateStructuralIntegrity)
                {
                    var structuralValidation = ValidateStructuralIntegrity(constructionData.ConstructionState);
                    errors.AddRange(structuralValidation.Errors);
                    warnings.AddRange(structuralValidation.Warnings);
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
                if (!(oldData is ConstructionSectionData constructionData))
                {
                    throw new ArgumentException("Invalid data type for migration");
                }

                var migrator = GetVersionMigrator(fromVersion, SectionVersion);
                if (migrator != null)
                {
                    var migratedState = await migrator.MigrateConstructionStateAsync(constructionData.ConstructionState);
                    
                    var migratedData = new ConstructionSectionData
                    {
                        SectionKey = SectionKey,
                        DataVersion = SectionVersion,
                        Timestamp = DateTime.Now,
                        ConstructionState = migratedState,
                        DataHash = GenerateDataHash(constructionData)
                    };

                    migratedData.EstimatedSize = CalculateDataSize(migratedState);

                    LogInfo($"Construction data migrated from {fromVersion} to {SectionVersion}");
                    return migratedData;
                }

                // Auto-migrate 2D to 3D if enabled
                if (_autoMigrate2DTo3D && HasLegacy2DData(constructionData.ConstructionState))
                {
                    var migrated2D = await Migrate2DTo3DAsync(constructionData.ConstructionState);
                    constructionData.ConstructionState = migrated2D;
                    constructionData.DataVersion = SectionVersion;
                    LogInfo("Automatically migrated 2D coordinates to 3D");
                }

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
            var state = _lastSavedState ?? GetCurrentConstructionState();
            
            return new SaveSectionSummary
            {
                SectionKey = SectionKey,
                SectionName = SectionName,
                StatusDescription = GetStatusDescription(state),
                ItemCount = state?.PlacedObjects?.Count ?? 0,
                DataSize = _estimatedDataSize,
                LastUpdated = _lastSaveTime,
                KeyValuePairs = new Dictionary<string, string>
                {
                    { "Placed Objects", (state?.PlacedObjects?.Count ?? 0).ToString() },
                    { "Rooms", (state?.Rooms?.Count ?? 0).ToString() },
                    { "Active Projects", (state?.ActiveProjects?.Count ?? 0).ToString() },
                    { "Utility Systems", (state?.UtilitySystems?.Count ?? 0).ToString() },
                    { "Grid Utilization", state?.GridSystem != null ? $"{state.GridSystem.GridUtilization:P1}" : "N/A" },
                    { "System Status", _systemsInitialized ? "Initialized" : "Not Initialized" }
                },
                HasErrors = !_systemsInitialized,
                ErrorMessages = _systemsInitialized ? new List<string>() : new List<string> { "Construction systems not initialized" }
            };
        }

        public bool HasChanges()
        {
            if (!_systemsInitialized || _lastSavedState == null)
                return true;

            // Quick change detection
            var currentState = GetCurrentConstructionState();
            if (currentState == null)
                return false;

            return _hasChanges || 
                   currentState.PlacedObjects?.Count != _lastSavedState.PlacedObjects?.Count ||
                   currentState.Rooms?.Count != _lastSavedState.Rooms?.Count ||
                   currentState.ActiveProjects?.Count != _lastSavedState.ActiveProjects?.Count ||
                   DateTime.Now.Subtract(_lastSaveTime).TotalMinutes > 10; // Force save every 10 minutes
        }

        public void MarkClean()
        {
            _hasChanges = false;
            _lastSaveTime = DateTime.Now;
        }

        public async Task ResetToDefaultStateAsync()
        {
            await InitializeSystemsIfNeeded();

            // Reset construction system to defaults
            if (_constructionSystem != null)
            {
                await _constructionSystem.ResetToDefaultsAsync();
            }

            if (_gridManager != null)
            {
                await _gridManager.ClearGridAsync();
            }

            _lastSavedState = null;
            _hasChanges = true;
            
            LogInfo("Construction system reset to default state");
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

            // Clean up temporary construction data, invalid placements, etc.
            if (_constructionSystem != null)
            {
                await _constructionSystem.CleanupTemporaryDataAsync();
            }

            if (_gridManager != null)
            {
                await _gridManager.ValidateAndCleanupGridAsync();
            }

            // Mark that changes occurred due to cleanup
            _hasChanges = true;
        }

        public async Task PostLoadInitializationAsync()
        {
            await InitializeSystemsIfNeeded();

            // Rebuild grid state, recalculate placement validity
            if (_gridManager != null)
            {
                await _gridManager.RebuildGridStateAsync();
            }

            if (_constructionSystem != null)
            {
                await _constructionSystem.RecalculatePlacementValidityAsync();
            }

            if (_utilitySystem != null)
            {
                await _utilitySystem.RebuildUtilityConnectionsAsync();
            }

            LogInfo("Construction system post-load initialization completed");
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
                    _constructionSystem = FindObjectOfType<MonoBehaviour>() as IConstructionSystem;
                    _gridManager = FindObjectOfType<MonoBehaviour>() as IGridManager;
                    _placementSystem = FindObjectOfType<MonoBehaviour>() as IPlacementSystem;
                    _utilitySystem = FindObjectOfType<MonoBehaviour>() as IUtilitySystem;
                }

                _systemsInitialized = true;
            });

            LogInfo($"Construction save provider initialized. Systems found: " +
                   $"Construction={_constructionSystem != null}, " +
                   $"Grid={_gridManager != null}, " +
                   $"Placement={_placementSystem != null}, " +
                   $"Utility={_utilitySystem != null}");
        }

        #endregion

        #region Data Gathering and Application

        private async Task<ConstructionStateDTO> GatherConstructionStateAsync()
        {
            var state = new ConstructionStateDTO
            {
                SaveTimestamp = DateTime.Now,
                SaveVersion = SectionVersion,
                EnableConstructionSystem = true
            };

            // Gather grid system state
            if (_gridManager != null)
            {
                state.GridState = await GatherGridSystemStateAsync();
                state.GridSystem = state.GridState; // Duplicate field for compatibility
            }

            // Gather placed objects and construction projects
            if (_constructionSystem != null)
            {
                state.PlacedObjects = await GatherPlacedObjectsAsync();
                state.ActiveProjects = await GatherActiveProjectsAsync();
                state.CompletedProjects = await GatherCompletedProjectsAsync();
                state.ReservedPositions = await GatherReservedPositionsAsync();
                state.Rooms = await GatherRoomsAsync();
                state.Metrics = await GatherConstructionMetricsAsync();
                state.CostSummary = await GatherCostSummaryAsync();
            }

            // Gather utility systems
            if (_utilitySystem != null)
            {
                state.UtilitySystems = await GatherUtilitySystemsAsync();
            }

            return state;
        }

        private async Task<SaveSectionResult> ApplyConstructionStateAsync(ConstructionStateDTO state)
        {
            try
            {
                // Apply grid system state
                if (_gridManager != null && state.GridSystem != null)
                {
                    await _gridManager.LoadGridStateAsync(state.GridSystem);
                }

                // Apply construction data
                if (_constructionSystem != null)
                {
                    if (state.PlacedObjects != null)
                        await _constructionSystem.LoadPlacedObjectsAsync(state.PlacedObjects);
                    
                    if (state.ActiveProjects != null)
                        await _constructionSystem.LoadActiveProjectsAsync(state.ActiveProjects);
                    
                    if (state.CompletedProjects != null)
                        await _constructionSystem.LoadCompletedProjectsAsync(state.CompletedProjects);
                    
                    if (state.ReservedPositions != null)
                        await _constructionSystem.LoadReservedPositionsAsync(state.ReservedPositions);
                    
                    if (state.Rooms != null)
                        await _constructionSystem.LoadRoomsAsync(state.Rooms);
                }

                // Apply utility systems
                if (_utilitySystem != null && state.UtilitySystems != null)
                {
                    await _utilitySystem.LoadUtilitySystemsAsync(state.UtilitySystems);
                }

                return SaveSectionResult.CreateSuccess();
            }
            catch (Exception ex)
            {
                LogError($"Failed to apply construction state: {ex.Message}");
                return SaveSectionResult.CreateFailure($"Application failed: {ex.Message}", ex);
            }
        }

        #endregion

        #region Helper Methods

        private ConstructionStateDTO GetCurrentConstructionState()
        {
            // Quick state snapshot for change detection
            if (!_systemsInitialized)
                return null;

            return new ConstructionStateDTO
            {
                PlacedObjects = _constructionSystem?.GetPlacedObjects()?.Select(ConvertToPlacedObjectDTO).ToList(),
                Rooms = _constructionSystem?.GetRooms()?.Select(ConvertToRoomDTO).ToList(),
                ActiveProjects = _constructionSystem?.GetActiveProjects()?.Select(ConvertToConstructionProjectDTO).ToList(),
                SaveTimestamp = DateTime.Now
            };
        }

        private ISaveSectionData CreateEmptySectionData()
        {
            return new ConstructionSectionData
            {
                SectionKey = SectionKey,
                DataVersion = SectionVersion,
                Timestamp = DateTime.Now,
                ConstructionState = new ConstructionStateDTO
                {
                    SaveTimestamp = DateTime.Now,
                    SaveVersion = SectionVersion,
                    PlacedObjects = new List<PlacedObjectDTO>(),
                    Rooms = new List<RoomDTO>(),
                    ActiveProjects = new List<ConstructionProjectDTO>(),
                    GridSystem = new GridSystemStateDTO()
                },
                EstimatedSize = 2048 // Minimal size
            };
        }

        private long CalculateDataSize(ConstructionStateDTO state)
        {
            if (state == null) return 0;

            // Estimate based on content
            long size = 2048; // Base overhead
            size += (state.PlacedObjects?.Count ?? 0) * 1024; // ~1KB per object
            size += (state.Rooms?.Count ?? 0) * 2048; // ~2KB per room
            size += (state.ActiveProjects?.Count ?? 0) * 1024; // ~1KB per project
            size += (state.GridSystem?.GridCells?.Count ?? 0) * 128; // ~128B per grid cell

            return size;
        }

        private string GenerateDataHash(ConstructionSectionData data)
        {
            // Simple hash based on key data points
            var hashSource = $"{data.Timestamp:yyyy-MM-dd-HH-mm-ss}" +
                           $"{data.ConstructionState?.PlacedObjects?.Count ?? 0}" +
                           $"{data.ConstructionState?.Rooms?.Count ?? 0}" +
                           $"{data.EstimatedSize}";
            
            return hashSource.GetHashCode().ToString("X8");
        }

        private string GetStatusDescription(ConstructionStateDTO state)
        {
            if (state == null)
                return "No construction data";

            int objects = state.PlacedObjects?.Count ?? 0;
            int rooms = state.Rooms?.Count ?? 0;
            int projects = state.ActiveProjects?.Count ?? 0;
            
            if (objects == 0 && rooms == 0 && projects == 0)
                return "Empty construction site";
            
            return $"{objects} objects, {rooms} rooms, {projects} active projects";
        }

        private bool RequiresMigration(string dataVersion) => dataVersion != SectionVersion;

        private bool HasLegacy2DData(ConstructionStateDTO state)
        {
            return state?.PlacedObjects?.Any(obj => obj.GridCoordinate.z == 0) == true ||
                   state?.GridSystem?.GridCells?.Any(cell => cell.GridCoordinate.z == 0) == true;
        }

        private async Task<ConstructionStateDTO> Migrate2DTo3DAsync(ConstructionStateDTO state)
        {
            // Convert all 2D coordinates to 3D (z=0 for ground level)
            if (state.PlacedObjects != null)
            {
                foreach (var obj in state.PlacedObjects)
                {
                    if (obj.GridCoordinate.z == 0 && (obj.GridCoordinate.x != 0 || obj.GridCoordinate.y != 0))
                    {
                        // This was likely a 2D coordinate, keep z=0 for ground level
                    }
                }
            }

            await Task.Delay(1); // Simulate async work
            LogInfo("Migrated 2D construction data to 3D coordinates");
            return state;
        }

        // Validation methods
        private async Task<ConstructionValidationResult> ValidateConstructionStateAsync(ConstructionStateDTO state) => new ConstructionValidationResult { IsValid = true };
        private ConstructionValidationResult ValidateGridIntegrity(GridSystemStateDTO gridState) => new ConstructionValidationResult { IsValid = true };
        private ConstructionValidationResult ValidateStructuralIntegrity(ConstructionStateDTO state) => new ConstructionValidationResult { IsValid = true };
        private float CalculateDataCorruption(ConstructionStateDTO state) => 0.0f;
        private IConstructionMigrator GetVersionMigrator(string fromVersion, string toVersion) => null;

        // Conversion methods - placeholders
        private PlacedObjectDTO ConvertToPlacedObjectDTO(object obj) => new PlacedObjectDTO();
        private RoomDTO ConvertToRoomDTO(object room) => new RoomDTO();
        private ConstructionProjectDTO ConvertToConstructionProjectDTO(object project) => new ConstructionProjectDTO();

        // Async gathering methods - placeholders
        private async Task<GridSystemStateDTO> GatherGridSystemStateAsync() => new GridSystemStateDTO();
        private async Task<List<PlacedObjectDTO>> GatherPlacedObjectsAsync() => new List<PlacedObjectDTO>();
        private async Task<List<ConstructionProjectDTO>> GatherActiveProjectsAsync() => new List<ConstructionProjectDTO>();
        private async Task<List<ConstructionProjectDTO>> GatherCompletedProjectsAsync() => new List<ConstructionProjectDTO>();
        private async Task<Dictionary<string, Vector3Int>> GatherReservedPositionsAsync() => new Dictionary<string, Vector3Int>();
        private async Task<List<RoomDTO>> GatherRoomsAsync() => new List<RoomDTO>();
        private async Task<ConstructionMetricsDTO> GatherConstructionMetricsAsync() => new ConstructionMetricsDTO();
        private async Task<ConstructionCostSummaryDTO> GatherCostSummaryAsync() => new ConstructionCostSummaryDTO();
        private async Task<List<UtilitySystemDTO>> GatherUtilitySystemsAsync() => new List<UtilitySystemDTO>();

        private void LogInfo(string message) => Debug.Log($"[ConstructionSaveProvider] {message}");
        private void LogWarning(string message) => Debug.LogWarning($"[ConstructionSaveProvider] {message}");
        private void LogError(string message) => Debug.LogError($"[ConstructionSaveProvider] {message}");

        #endregion
    }

    /// <summary>
    /// Construction-specific save section data container
    /// </summary>
    [System.Serializable]
    public class ConstructionSectionData : ISaveSectionData
    {
        public string SectionKey { get; set; }
        public string DataVersion { get; set; }
        public DateTime Timestamp { get; set; }
        public long EstimatedSize { get; set; }
        public string DataHash { get; set; }

        public ConstructionStateDTO ConstructionState;

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(SectionKey) && 
                   !string.IsNullOrEmpty(DataVersion) && 
                   ConstructionState != null;
        }

        public string GetSummary()
        {
            var objects = ConstructionState?.PlacedObjects?.Count ?? 0;
            var rooms = ConstructionState?.Rooms?.Count ?? 0;
            return $"Construction: {objects} objects, {rooms} rooms";
        }
    }

    /// <summary>
    /// Interfaces for system integration (would be implemented by actual systems)
    /// </summary>
    public interface IConstructionSystem
    {
        Task ResetToDefaultsAsync();
        Task CleanupTemporaryDataAsync();
        Task RecalculatePlacementValidityAsync();
        Task LoadPlacedObjectsAsync(List<PlacedObjectDTO> objects);
        Task LoadActiveProjectsAsync(List<ConstructionProjectDTO> projects);
        Task LoadCompletedProjectsAsync(List<ConstructionProjectDTO> projects);
        Task LoadReservedPositionsAsync(Dictionary<string, Vector3Int> positions);
        Task LoadRoomsAsync(List<RoomDTO> rooms);
        List<object> GetPlacedObjects();
        List<object> GetRooms();
        List<object> GetActiveProjects();
    }

    public interface IGridManager
    {
        Task ClearGridAsync();
        Task ValidateAndCleanupGridAsync();
        Task RebuildGridStateAsync();
        Task LoadGridStateAsync(GridSystemStateDTO gridState);
    }

    public interface IPlacementSystem
    {
        // Placeholder for placement-specific methods
    }

    public interface IUtilitySystem
    {
        Task RebuildUtilityConnectionsAsync();
        Task LoadUtilitySystemsAsync(List<UtilitySystemDTO> systems);
    }

    public interface IConstructionMigrator
    {
        Task<ConstructionStateDTO> MigrateConstructionStateAsync(ConstructionStateDTO oldState);
    }

    /// <summary>
    /// Validation result for construction data
    /// </summary>
    public struct ConstructionValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }
    }
}