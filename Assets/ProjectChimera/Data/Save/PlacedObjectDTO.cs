using UnityEngine;
using System;
using System.Collections.Generic;


namespace ProjectChimera.Data.Save
{
    /// <summary>
    /// Placed Object Data Transfer Object
    /// Handles equipment and object placement data for save/load operations
    /// Includes position, configuration, and operational state
    /// </summary>
    [Serializable]
    public class PlacedObjectDTO
    {
        [Header("Object Identity")]
        public string ObjectID;
        public string ObjectName;
        public string PrefabName;
        public string TemplateID;
        public ObjectType ObjectType;

        // Alias property for compatibility
        public string ObjectId { get => ObjectID; set => ObjectID = value; }
        public Dictionary<string, object> ObjectData = new Dictionary<string, object>();

        [Header("Placement Data")]
        public Vector3 Position;
        public Vector3 Rotation;
        public Vector3 Scale;
        public Vector3Int GridPosition;
        public Vector3Int GridSize;

        [Header("Ownership and Projects")]
        public string ProjectID;
        public string RoomID;
        public string OwnerID;
        public DateTime PlacementDate;

        [Header("Operational State")]
        public bool IsActive;
        public bool IsPowered;
        public float Health; // 0-1
        public float Efficiency; // 0-1

        [Header("Configuration")]
        public Dictionary<string, string> Configuration = new Dictionary<string, string>();
        public List<string> ConnectedObjects = new List<string>();
        public string ParentObjectID;

        [Header("Maintenance")]
        public DateTime LastMaintenanceDate;
        public DateTime NextMaintenanceDate;
        public bool RequiresMaintenance;
        public List<string> MaintenanceHistory = new List<string>();

        [Header("Cost and Resources")]
        public float InitialCost;
        public float MaintenanceCost;
        public Dictionary<string, float> ResourceConsumption = new Dictionary<string, float>();
        public float PowerConsumption;
        public float WaterConsumption;

        [Header("Environmental Impact")]
        public float TemperatureContribution;
        public float HumidityContribution;
        public float LightContribution;
        public float CO2Contribution;

        /// <summary>
        /// Gets the object's age in days
        /// </summary>
        public int GetAgeInDays()
        {
            return (int)(DateTime.Now - PlacementDate).TotalDays;
        }

        /// <summary>
        /// Gets the days since last maintenance
        /// </summary>
        public int GetDaysSinceMaintenance()
        {
            return (int)(DateTime.Now - LastMaintenanceDate).TotalDays;
        }

        /// <summary>
        /// Gets the days until next maintenance
        /// </summary>
        public int GetDaysUntilMaintenance()
        {
            if (NextMaintenanceDate == default(DateTime))
                return -1;

            return Math.Max(0, (int)(NextMaintenanceDate - DateTime.Now).TotalDays);
        }

        /// <summary>
        /// Checks if the object needs maintenance
        /// </summary>
        public bool NeedsMaintenance()
        {
            return RequiresMaintenance || DateTime.Now >= NextMaintenanceDate;
        }

        /// <summary>
        /// Checks if the object is operational
        /// </summary>
        public bool IsOperational()
        {
            return IsActive && Health > 0.1f && (!IsPowered || PowerConsumption <= 0);
        }

        /// <summary>
        /// Gets the object's operational status
        /// </summary>
        public ObjectOperationalStatus GetOperationalStatus()
        {
            if (!IsActive) return ObjectOperationalStatus.Inactive;
            if (Health < 0.1f) return ObjectOperationalStatus.Damaged;
            if (NeedsMaintenance()) return ObjectOperationalStatus.NeedsMaintenance;
            if (!IsOperational()) return ObjectOperationalStatus.Offline;

            return ObjectOperationalStatus.Operational;
        }

        /// <summary>
        /// Gets configuration value by key
        /// </summary>
        public string GetConfigurationValue(string key, string defaultValue = "")
        {
            if (Configuration.ContainsKey(key))
            {
                return Configuration[key];
            }
            return defaultValue;
        }

        /// <summary>
        /// Sets configuration value
        /// </summary>
        public void SetConfigurationValue(string key, string value)
        {
            Configuration[key] = value;
        }

        /// <summary>
        /// Gets configuration value as float
        /// </summary>
        public float GetConfigurationValueAsFloat(string key, float defaultValue = 0f)
        {
            if (Configuration.ContainsKey(key) && float.TryParse(Configuration[key], out float result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// Gets configuration value as int
        /// </summary>
        public int GetConfigurationValueAsInt(string key, int defaultValue = 0)
        {
            if (Configuration.ContainsKey(key) && int.TryParse(Configuration[key], out int result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// Adds a connected object
        /// </summary>
        public void AddConnectedObject(string objectID)
        {
            if (!ConnectedObjects.Contains(objectID))
            {
                ConnectedObjects.Add(objectID);
            }
        }

        /// <summary>
        /// Removes a connected object
        /// </summary>
        public void RemoveConnectedObject(string objectID)
        {
            ConnectedObjects.Remove(objectID);
        }

        /// <summary>
        /// Records maintenance activity
        /// </summary>
        public void RecordMaintenance(string maintenanceType, string description)
        {
            string maintenanceRecord = $"{DateTime.Now:yyyy-MM-dd}: {maintenanceType} - {description}";
            MaintenanceHistory.Add(maintenanceRecord);
            LastMaintenanceDate = DateTime.Now;

            // Schedule next maintenance (simplified - 30 days from now)
            NextMaintenanceDate = DateTime.Now.AddDays(30);
            RequiresMaintenance = false;
        }

        /// <summary>
        /// Updates operational metrics
        /// </summary>
        public void UpdateMetrics(float healthChange, float efficiencyChange)
        {
            Health = Mathf.Clamp01(Health + healthChange);
            Efficiency = Mathf.Clamp01(Efficiency + efficiencyChange);
        }

        /// <summary>
        /// Gets a summary of the object's current state
        /// </summary>
        public string GetObjectSummary()
        {
            string summary = $"{ObjectName} ({ObjectType})";
            summary += $"\nStatus: {GetOperationalStatus()}";
            summary += $"\nHealth: {(Health * 100):F1}%, Efficiency: {(Efficiency * 100):F1}%";

            if (NeedsMaintenance())
            {
                summary += $"\nMaintenance: Due in {GetDaysUntilMaintenance()} days";
            }

            return summary;
        }

        /// <summary>
        /// Checks if this object can connect to another
        /// </summary>
        public bool CanConnectTo(PlacedObjectDTO otherObject)
        {
            // Basic connection logic - can be expanded based on object types
            if (ObjectType == ObjectType.Equipment && otherObject.ObjectType == ObjectType.Equipment)
            {
                return true;
            }

            if (ObjectType == ObjectType.Utility && otherObject.ObjectType == ObjectType.Equipment)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the object's grid bounds
        /// </summary>
        public Bounds GetGridBounds()
        {
            Vector3 center = (Vector3)GridPosition + (Vector3)GridSize / 2f;
            Vector3 size = GridSize;
            return new Bounds(center, size);
        }
    }

    /// <summary>
    /// Object type enumeration
    /// </summary>
    public enum ObjectType
    {
        Equipment,
        Utility,
        Structure,
        Furniture,
        Decorative,
        Other
    }

    /// <summary>
    /// Object operational status enumeration
    /// </summary>
    public enum ObjectOperationalStatus
    {
        Operational,
        NeedsMaintenance,
        Damaged,
        Offline,
        Inactive
    }
}

