using UnityEngine;
using System;
using System.Collections.Generic;


namespace ProjectChimera.Data.Save
{
    /// <summary>
    /// SIMPLE: Basic construction data transfer objects aligned with Project Chimera's construction vision.
    /// Focuses on essential construction state for basic grid placement and facility management.
    /// </summary>
    [System.Serializable]
    public class ConstructionStateDTO
    {
        [Header("Basic Construction State")]
        public System.DateTime SaveTimestamp;
        public GridSystemStateDTO GridSystem;
        public List<PlacedObjectDTO> PlacedObjects = new List<PlacedObjectDTO>();
        public List<RoomDTO> Rooms = new List<RoomDTO>();
        public ConstructionMetricsDTO Metrics;
        public bool EnableConstructionSystem = true;
        public string SaveVersion = "1.0";
    }

    [System.Serializable]
    public class ConstructionMetricsDTO
    {
        public int TotalObjectsPlaced;
        public int TotalRoomsCreated;
        public float TotalConstructionCost;
        public int TotalObjectsBuilt;
        public int TotalRoomsBuilt;
        public System.DateTime LastConstructionDate;
        public float ConstructionExperience;
    }

    /// <summary>
    /// Grid system state data
    /// </summary>
    [System.Serializable]
    public class GridSystemStateDTO
    {
        public Vector3Int GridSize;
        public int GridSizeX;
        public int GridSizeY;
        public float CellSize = 1f;
        public Vector3 GridOffset = Vector3.zero;
        public bool IsGridVisible = true;
        public List<PlacedObjectDTO> GridItems = new List<PlacedObjectDTO>();
        public string SaveVersion = "1.0";
    }


    /// <summary>
    /// Basic room data
    /// </summary>
    [System.Serializable]
    public class RoomDTO
    {
        public string RoomId;
        public string RoomName;
        public string RoomType;
        public List<Vector3Int> Positions = new List<Vector3Int>();
        public Vector3Int RoomSize;
        public int MaxCapacity;
        public int CurrentOccupancy;
        public float PowerRequirement;
        public bool IsSealed = false;
        public string EnvironmentType;
        public ConstructionEnvironmentalConditionsDTO EnvironmentalConditions;
        public bool IsActive = true;
    }

    [System.Serializable]
    public class ConstructionEnvironmentalConditionsDTO
    {
        public float Temperature;
        public float Humidity;
        public float CO2Level;
        public float LightLevel;
        public float LightIntensity;
        public float AirCirculation;
    }

    /// <summary>
    /// Basic construction utilities class
    /// </summary>
    public static class ConstructionDTOUtils
    {
        /// <summary>
        /// Create basic construction state
        /// </summary>
        public static ConstructionStateDTO CreateBasicState()
        {
            return new ConstructionStateDTO
            {
                PlacedObjects = new List<PlacedObjectDTO>(),
                Rooms = new List<RoomDTO>(),
                EnableConstructionSystem = true,
                SaveVersion = "1.0"
            };
        }

        /// <summary>
        /// Add placed object to state
        /// </summary>
        public static void AddPlacedObject(ConstructionStateDTO state, PlacedObjectDTO obj)
        {
            if (state != null && obj != null)
            {
                state.PlacedObjects.Add(obj);
            }
        }

        /// <summary>
        /// Add room to state
        /// </summary>
        public static void AddRoom(ConstructionStateDTO state, RoomDTO room)
        {
            if (state != null && room != null)
            {
                state.Rooms.Add(room);
            }
        }

        /// <summary>
        /// Get placed object by ID
        /// </summary>
        public static PlacedObjectDTO GetPlacedObject(ConstructionStateDTO state, string objectId)
        {
            if (state == null || string.IsNullOrEmpty(objectId)) return null;
            return state.PlacedObjects.Find(obj => obj.ObjectID == objectId);
        }

        /// <summary>
        /// Get room by ID
        /// </summary>
        public static RoomDTO GetRoom(ConstructionStateDTO state, string roomId)
        {
            if (state == null || string.IsNullOrEmpty(roomId)) return null;
            return state.Rooms.Find(room => room.RoomId == roomId);
        }

        /// <summary>
        /// Remove placed object by ID
        /// </summary>
        public static bool RemovePlacedObject(ConstructionStateDTO state, string objectId)
        {
            if (state == null || string.IsNullOrEmpty(objectId)) return false;
            return state.PlacedObjects.RemoveAll(obj => obj.ObjectID == objectId) > 0;
        }

        /// <summary>
        /// Remove room by ID
        /// </summary>
        public static bool RemoveRoom(ConstructionStateDTO state, string roomId)
        {
            if (state == null || string.IsNullOrEmpty(roomId)) return false;
            return state.Rooms.RemoveAll(room => room.RoomId == roomId) > 0;
        }

        /// <summary>
        /// Get construction statistics
        /// </summary>
        public static ConstructionStatistics GetStatistics(ConstructionStateDTO state)
        {
            if (state == null) return new ConstructionStatistics();

            int activeObjects = 0;
            foreach (var obj in state.PlacedObjects)
            {
                if (obj.IsActive) activeObjects++;
            }

            int activeRooms = 0;
            foreach (var room in state.Rooms)
            {
                if (room.IsActive) activeRooms++;
            }

            return new ConstructionStatistics
            {
                TotalPlacedObjects = state.PlacedObjects.Count,
                TotalRooms = state.Rooms.Count,
                ActiveObjects = activeObjects,
                ActiveRooms = activeRooms
            };
        }

        /// <summary>
        /// Validate construction state
        /// </summary>
        public static bool ValidateState(ConstructionStateDTO state)
        {
            if (state == null) return false;
            if (state.PlacedObjects == null || state.Rooms == null) return false;

            // Basic validation - check for duplicate IDs
            var objectIds = new HashSet<string>();
            foreach (var obj in state.PlacedObjects)
            {
                if (obj == null || string.IsNullOrEmpty(obj.ObjectID)) return false;
                if (!objectIds.Add(obj.ObjectID)) return false; // Duplicate ID
            }

            var roomIds = new HashSet<string>();
            foreach (var room in state.Rooms)
            {
                if (room == null || string.IsNullOrEmpty(room.RoomId)) return false;
                if (!roomIds.Add(room.RoomId)) return false; // Duplicate ID
            }

            return true;
        }
    }

    /// <summary>
    /// Construction statistics
    /// </summary>
    [System.Serializable]
    public class ConstructionStatistics
    {
        public int TotalPlacedObjects;
        public int TotalRooms;
        public int ActiveObjects;
        public int ActiveRooms;
    }

    /// <summary>
    /// Maintenance record data transfer object
    /// </summary>
    [System.Serializable]
    public class MaintenanceRecordDTO
    {
        public DateTime MaintenanceDate;
        public string MaintenanceType;
        public string Description;
        public string TechnicianName;
        public float Duration; // Hours
        public float Cost;
        public bool WasSuccessful;
        public Dictionary<string, string> AdditionalNotes;

        public MaintenanceRecordDTO()
        {
            AdditionalNotes = new Dictionary<string, string>();
        }
    }
}
