using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Equipment;
using EquipmentType = ProjectChimera.Data.Equipment.EquipmentType;
using MalfunctionType = ProjectChimera.Systems.Equipment.Degradation.MalfunctionType;
using MalfunctionSeverity = ProjectChimera.Systems.Equipment.Degradation.MalfunctionSeverity;

namespace ProjectChimera.Systems.Equipment.Degradation.Database
{
    /// <summary>
    /// REFACTORED: Cost Database Storage Manager - Focused database storage and retrieval operations
    /// Single Responsibility: Managing cost database storage, queries, and equipment profiles
    /// Extracted from CostDatabaseManager for better SRP compliance
    /// </summary>
    public class CostDatabaseStorageManager
    {
        private readonly bool _enableLogging;
        private readonly float _updateThreshold;
        private readonly int _minimumSamplesForUpdate;

        // Core cost database
        private readonly Dictionary<MalfunctionType, CostDatabaseEntry> _costDatabase = new Dictionary<MalfunctionType, CostDatabaseEntry>();
        private readonly Dictionary<EquipmentType, EquipmentCostProfile> _equipmentProfiles = new Dictionary<EquipmentType, EquipmentCostProfile>();

        // Database statistics
        private DatabaseStorageStatistics _storageStats = new DatabaseStorageStatistics();

        // Events
        public event System.Action<MalfunctionType, CostDatabaseEntry> OnDatabaseEntryUpdated;
        public event System.Action<MalfunctionType, CostDatabaseEntry> OnDatabaseEntryCreated;
        public event System.Action<EquipmentType, EquipmentCostProfile> OnEquipmentProfileUpdated;

        public CostDatabaseStorageManager(bool enableLogging = false, float updateThreshold = 0.15f, int minimumSamplesForUpdate = 5)
        {
            _enableLogging = enableLogging;
            _updateThreshold = updateThreshold;
            _minimumSamplesForUpdate = minimumSamplesForUpdate;
        }

        // Properties
        public DatabaseStorageStatistics Statistics => _storageStats;
        public int EntryCount => _costDatabase.Count;
        public int EquipmentProfileCount => _equipmentProfiles.Count;
        public IEnumerable<MalfunctionType> AvailableMalfunctionTypes => _costDatabase.Keys;
        public IEnumerable<EquipmentType> AvailableEquipmentTypes => _equipmentProfiles.Keys;

        #region Initialization

        /// <summary>
        /// Initialize with default database entries
        /// </summary>
        public void Initialize()
        {
            InitializeDefaultDatabase();
            InitializeEquipmentProfiles();

            if (_enableLogging)
                ChimeraLogger.LogInfo("DB_STORAGE", "Cost database storage manager initialized", null);
        }

        /// <summary>
        /// Initialize default cost database with base values
        /// </summary>
        private void InitializeDefaultDatabase()
        {
            var defaultEntries = new Dictionary<MalfunctionType, (float baseCost, float baseTime, float confidence)>
            {
                { MalfunctionType.ElectricalFault, (150.0f, 3.0f, 0.8f) },
                { MalfunctionType.MechanicalWear, (200.0f, 4.0f, 0.85f) },
                { MalfunctionType.SoftwareGlitch, (75.0f, 1.5f, 0.9f) },
                { MalfunctionType.Overheating, (125.0f, 2.5f, 0.75f) },
                { MalfunctionType.Corrosion, (300.0f, 6.0f, 0.7f) },
                { MalfunctionType.Contamination, (100.0f, 2.0f, 0.8f) },
                { MalfunctionType.Calibration, (50.0f, 1.0f, 0.95f) },
                { MalfunctionType.SensorFailure, (175.0f, 3.5f, 0.85f) },
                { MalfunctionType.PowerSupplyIssue, (225.0f, 4.5f, 0.8f) },
                { MalfunctionType.NetworkConnectivity, (80.0f, 1.8f, 0.9f) }
            };

            foreach (var entry in defaultEntries)
            {
                var databaseEntry = new CostDatabaseEntry
                {
                    MalfunctionType = entry.Key,
                    BaseCost = entry.Value.baseCost,
                    BaseRepairTime = entry.Value.baseTime,
                    Confidence = entry.Value.confidence,
                    SampleCount = 0,
                    LastUpdated = DateTime.Now,
                    CostHistory = new List<CostDataPoint>()
                };

                _costDatabase[entry.Key] = databaseEntry;
            }

            if (_enableLogging)
                ChimeraLogger.LogInfo("DB_STORAGE", $"Initialized {_costDatabase.Count} default database entries", null);
        }

        /// <summary>
        /// Initialize equipment cost profiles
        /// </summary>
        private void InitializeEquipmentProfiles()
        {
            var equipmentData = new Dictionary<EquipmentType, (float multiplier, float complexity, float availability)>
            {
                { EquipmentType.PumpSystem, (1.2f, 0.7f, 0.9f) },
                { EquipmentType.ValveAssembly, (0.8f, 0.5f, 0.95f) },
                { EquipmentType.SensorArray, (1.5f, 0.8f, 0.85f) },
                { EquipmentType.ControlUnit, (2.0f, 0.9f, 0.7f) },
                { EquipmentType.HeatExchanger, (1.8f, 0.75f, 0.8f) },
                { EquipmentType.Compressor, (2.5f, 0.85f, 0.75f) },
                { EquipmentType.FilterSystem, (0.6f, 0.4f, 0.9f) },
                { EquipmentType.PowerDistribution, (1.4f, 0.6f, 0.85f) }
            };

            foreach (var equipment in equipmentData)
            {
                var profile = new EquipmentCostProfile
                {
                    EquipmentType = equipment.Key,
                    CostMultiplier = equipment.Value.multiplier,
                    ComplexityFactor = equipment.Value.complexity,
                    PartAvailability = equipment.Value.availability,
                    MaintenanceFrequency = CalculateMaintenanceFrequency(equipment.Value.complexity),
                    AverageLifespan = CalculateAverageLifespan(equipment.Key),
                    LastProfileUpdate = DateTime.Now
                };

                _equipmentProfiles[equipment.Key] = profile;
            }

            if (_enableLogging)
                ChimeraLogger.LogInfo("DB_STORAGE", $"Initialized {_equipmentProfiles.Count} equipment profiles", null);
        }

        #endregion

        #region Database Operations

        /// <summary>
        /// Get cost database entry for malfunction type
        /// </summary>
        public CostDatabaseEntry GetCostEntry(MalfunctionType type)
        {
            _storageStats.QueriesHandled++;

            if (_costDatabase.TryGetValue(type, out var entry))
            {
                _storageStats.SuccessfulQueries++;
                entry.AccessCount++;
                entry.LastAccessed = DateTime.Now;
                return entry;
            }

            _storageStats.MissedQueries++;

            if (_enableLogging)
                ChimeraLogger.LogWarning("DB_STORAGE", $"Cost entry not found for malfunction type: {type}", null);

            return null;
        }

        /// <summary>
        /// Update cost database entry with new data
        /// </summary>
        public bool UpdateCostEntry(MalfunctionType type, float actualCost, TimeSpan actualTime, MalfunctionSeverity severity, EquipmentType equipmentType)
        {
            try
            {
                if (!_costDatabase.TryGetValue(type, out var entry))
                {
                    // Create new entry if it doesn't exist
                    entry = CreateNewDatabaseEntry(type);
                    _costDatabase[type] = entry;
                    OnDatabaseEntryCreated?.Invoke(type, entry);
                }

                var dataPoint = new CostDataPoint
                {
                    Timestamp = DateTime.Now,
                    ActualCost = actualCost,
                    ActualTime = actualTime,
                    Severity = severity,
                    EquipmentType = equipmentType,
                    MalfunctionType = type
                };

                // Add to history
                entry.CostHistory.Add(dataPoint);
                entry.SampleCount++;
                entry.LastUpdated = DateTime.Now;

                // Update base values if we have enough samples and deviation is significant
                if (ShouldUpdateBaseCost(entry, actualCost))
                {
                    UpdateBaseCost(entry, actualCost, type);
                }

                // Recalculate averages
                RecalculateEntryAverages(entry);

                _storageStats.DatabaseUpdates++;
                OnDatabaseEntryUpdated?.Invoke(type, entry);

                if (_enableLogging)
                    ChimeraLogger.LogInfo("DB_STORAGE", $"Updated cost entry for {type}: Cost={actualCost:F2}, Time={actualTime.TotalHours:F1}h", null);

                return true;
            }
            catch (Exception ex)
            {
                _storageStats.UpdateErrors++;

                if (_enableLogging)
                    ChimeraLogger.LogError("DB_STORAGE", $"Failed to update cost entry for {type}: {ex.Message}", null);

                return false;
            }
        }

        /// <summary>
        /// Get equipment cost profile
        /// </summary>
        public EquipmentCostProfile GetEquipmentProfile(EquipmentType equipmentType)
        {
            _storageStats.ProfileQueries++;

            if (_equipmentProfiles.TryGetValue(equipmentType, out var profile))
            {
                profile.AccessCount++;
                profile.LastAccessed = DateTime.Now;
                return profile;
            }

            if (_enableLogging)
                ChimeraLogger.LogWarning("DB_STORAGE", $"Equipment profile not found for: {equipmentType}", null);

            return null;
        }

        /// <summary>
        /// Update equipment cost profile
        /// </summary>
        public bool UpdateEquipmentProfile(EquipmentType equipmentType, EquipmentCostProfile updatedProfile)
        {
            try
            {
                updatedProfile.LastProfileUpdate = DateTime.Now;
                _equipmentProfiles[equipmentType] = updatedProfile;
                _storageStats.ProfileUpdates++;

                OnEquipmentProfileUpdated?.Invoke(equipmentType, updatedProfile);

                if (_enableLogging)
                    ChimeraLogger.LogInfo("DB_STORAGE", $"Updated equipment profile for {equipmentType}", null);

                return true;
            }
            catch (Exception ex)
            {
                _storageStats.ProfileUpdateErrors++;

                if (_enableLogging)
                    ChimeraLogger.LogError("DB_STORAGE", $"Failed to update equipment profile for {equipmentType}: {ex.Message}", null);

                return false;
            }
        }

        /// <summary>
        /// Get all database entries
        /// </summary>
        public Dictionary<MalfunctionType, CostDatabaseEntry> GetAllEntries()
        {
            return new Dictionary<MalfunctionType, CostDatabaseEntry>(_costDatabase);
        }

        /// <summary>
        /// Get all equipment profiles
        /// </summary>
        public Dictionary<EquipmentType, EquipmentCostProfile> GetAllEquipmentProfiles()
        {
            return new Dictionary<EquipmentType, EquipmentCostProfile>(_equipmentProfiles);
        }

        /// <summary>
        /// Load database from external data
        /// </summary>
        public void LoadDatabaseData(Dictionary<MalfunctionType, CostDatabaseEntry> entries, Dictionary<EquipmentType, EquipmentCostProfile> profiles)
        {
            try
            {
                _costDatabase.Clear();
                _equipmentProfiles.Clear();

                if (entries != null)
                {
                    foreach (var entry in entries)
                    {
                        _costDatabase[entry.Key] = entry.Value;
                    }
                }

                if (profiles != null)
                {
                    foreach (var profile in profiles)
                    {
                        _equipmentProfiles[profile.Key] = profile.Value;
                    }
                }

                _storageStats.DatabaseLoads++;

                if (_enableLogging)
                    ChimeraLogger.LogInfo("DB_STORAGE", $"Loaded {_costDatabase.Count} entries and {_equipmentProfiles.Count} profiles", null);
            }
            catch (Exception ex)
            {
                _storageStats.LoadErrors++;

                if (_enableLogging)
                    ChimeraLogger.LogError("DB_STORAGE", $"Failed to load database data: {ex.Message}", null);
            }
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Create a new database entry
        /// </summary>
        private CostDatabaseEntry CreateNewDatabaseEntry(MalfunctionType type)
        {
            return new CostDatabaseEntry
            {
                MalfunctionType = type,
                BaseCost = GetDefaultBaseCost(type),
                BaseRepairTime = GetDefaultRepairTime(type),
                Confidence = 0.5f, // Low confidence for new entries
                SampleCount = 0,
                LastUpdated = DateTime.Now,
                CostHistory = new List<CostDataPoint>()
            };
        }

        /// <summary>
        /// Check if base cost should be updated
        /// </summary>
        private bool ShouldUpdateBaseCost(CostDatabaseEntry entry, float actualCost)
        {
            if (entry.SampleCount < _minimumSamplesForUpdate)
                return false;

            var deviation = Math.Abs(actualCost - entry.BaseCost) / entry.BaseCost;
            return deviation >= _updateThreshold;
        }

        /// <summary>
        /// Update base cost using weighted average
        /// </summary>
        private void UpdateBaseCost(CostDatabaseEntry entry, float actualCost, MalfunctionType type)
        {
            var weight = Math.Min(entry.SampleCount / 100.0f, 0.3f); // Max 30% weight on new data
            var oldBaseCost = entry.BaseCost;
            entry.BaseCost = entry.BaseCost * (1 - weight) + actualCost * weight;

            // Update confidence as we get more samples
            entry.Confidence = Math.Min(0.95f, 0.5f + (entry.SampleCount * 0.01f));

            if (_enableLogging)
                ChimeraLogger.LogInfo("DB_STORAGE", $"Updated base cost for {type}: {oldBaseCost:F2} -> {entry.BaseCost:F2}", null);
        }

        /// <summary>
        /// Recalculate entry averages
        /// </summary>
        private void RecalculateEntryAverages(CostDatabaseEntry entry)
        {
            if (entry.CostHistory.Count == 0)
                return;

            var recentHistory = entry.CostHistory.TakeLast(50).ToList(); // Use last 50 samples for averages

            entry.AverageCost = recentHistory.Average(h => h.ActualCost);
            entry.AverageRepairTime = (float)recentHistory.Average(h => h.ActualTime.TotalHours);
            entry.MinCost = recentHistory.Min(h => h.ActualCost);
            entry.MaxCost = recentHistory.Max(h => h.ActualCost);

            // Calculate cost standard deviation
            var costVariance = recentHistory.Average(h => Math.Pow(h.ActualCost - entry.AverageCost, 2));
            entry.CostStandardDeviation = (float)Math.Sqrt(costVariance);
        }

        /// <summary>
        /// Get default base cost for malfunction type
        /// </summary>
        private float GetDefaultBaseCost(MalfunctionType type)
        {
            return type switch
            {
                MalfunctionType.ElectricalFault => 150.0f,
                MalfunctionType.MechanicalWear => 200.0f,
                MalfunctionType.SoftwareGlitch => 75.0f,
                MalfunctionType.Overheating => 125.0f,
                MalfunctionType.Corrosion => 300.0f,
                MalfunctionType.Contamination => 100.0f,
                MalfunctionType.Calibration => 50.0f,
                MalfunctionType.SensorFailure => 175.0f,
                MalfunctionType.PowerSupplyIssue => 225.0f,
                MalfunctionType.NetworkConnectivity => 80.0f,
                _ => 150.0f
            };
        }

        /// <summary>
        /// Get default repair time for malfunction type
        /// </summary>
        private float GetDefaultRepairTime(MalfunctionType type)
        {
            return type switch
            {
                MalfunctionType.ElectricalFault => 3.0f,
                MalfunctionType.MechanicalWear => 4.0f,
                MalfunctionType.SoftwareGlitch => 1.5f,
                MalfunctionType.Overheating => 2.5f,
                MalfunctionType.Corrosion => 6.0f,
                MalfunctionType.Contamination => 2.0f,
                MalfunctionType.Calibration => 1.0f,
                MalfunctionType.SensorFailure => 3.5f,
                MalfunctionType.PowerSupplyIssue => 4.5f,
                MalfunctionType.NetworkConnectivity => 1.8f,
                _ => 3.0f
            };
        }

        /// <summary>
        /// Calculate maintenance frequency based on complexity
        /// </summary>
        private int CalculateMaintenanceFrequency(float complexity)
        {
            return (int)(30 + (complexity * 90)); // 30-120 days based on complexity
        }

        /// <summary>
        /// Calculate average lifespan for equipment type
        /// </summary>
        private int CalculateAverageLifespan(EquipmentType equipmentType)
        {
            return equipmentType switch
            {
                EquipmentType.PumpSystem => 8,
                EquipmentType.ValveAssembly => 12,
                EquipmentType.SensorArray => 5,
                EquipmentType.ControlUnit => 6,
                EquipmentType.HeatExchanger => 15,
                EquipmentType.Compressor => 10,
                EquipmentType.FilterSystem => 3,
                EquipmentType.PowerDistribution => 20,
                _ => 8
            };
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Reset storage statistics
        /// </summary>
        public void ResetStatistics()
        {
            _storageStats = new DatabaseStorageStatistics();

            if (_enableLogging)
                ChimeraLogger.LogInfo("DB_STORAGE", "Database storage statistics reset", null);
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Database storage statistics
    /// </summary>
    [System.Serializable]
    }
