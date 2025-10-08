// REFACTORED: Database Serializer
// Extracted from CostDatabasePersistenceManager for better separation of concerns

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using ProjectChimera.Data.Equipment;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Equipment.Degradation.Database
{
    /// <summary>
    /// Handles serialization and deserialization of cost database
    /// </summary>
    public class DatabaseSerializer
    {
        private readonly bool _enableLogging;

        public DatabaseSerializer(bool enableLogging = false)
        {
            _enableLogging = enableLogging;
        }

        public string SerializeDatabase(
            Dictionary<MalfunctionType, CostDatabaseEntry> costDatabase,
            Dictionary<EquipmentType, EquipmentCostProfile> equipmentProfiles,
            List<CostDataPoint> historicalData)
        {
            try
            {
                var databaseContainer = new
                {
                    CostDatabase = costDatabase,
                    EquipmentProfiles = equipmentProfiles,
                    HistoricalData = historicalData,
                    Version = "1.0",
                    SavedAt = DateTime.Now
                };

                var settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto
                };

                return JsonConvert.SerializeObject(databaseContainer, settings);
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                    ChimeraLogger.LogError("DB_SERIALIZE", $"Serialization error: {ex.Message}", null);
                return null;
            }
        }

        public (Dictionary<MalfunctionType, CostDatabaseEntry> costDatabase,
                Dictionary<EquipmentType, EquipmentCostProfile> equipmentProfiles,
                List<CostDataPoint> historicalData) DeserializeDatabase(string json)
        {
            try
            {
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };

                var container = JsonConvert.DeserializeObject<DatabaseContainer>(json, settings);

                return (
                    container?.CostDatabase ?? new Dictionary<MalfunctionType, CostDatabaseEntry>(),
                    container?.EquipmentProfiles ?? new Dictionary<EquipmentType, EquipmentCostProfile>(),
                    container?.HistoricalData ?? new List<CostDataPoint>()
                );
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                    ChimeraLogger.LogError("DB_SERIALIZE", $"Deserialization error: {ex.Message}", null);
                return (
                    new Dictionary<MalfunctionType, CostDatabaseEntry>(),
                    new Dictionary<EquipmentType, EquipmentCostProfile>(),
                    new List<CostDataPoint>()
                );
            }
        }

        private class DatabaseContainer
        {
            public Dictionary<MalfunctionType, CostDatabaseEntry> CostDatabase { get; set; }
            public Dictionary<EquipmentType, EquipmentCostProfile> EquipmentProfiles { get; set; }
            public List<CostDataPoint> HistoricalData { get; set; }
            public string Version { get; set; }
            public DateTime SavedAt { get; set; }
        }
    }
}

