using UnityEngine;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Interfaces
{
    /// <summary>
    /// Construction service interfaces for dependency injection
    /// Eliminates FindObjectOfType anti-patterns in construction systems
    ///
    /// Note: SchematicSO is defined in ProjectChimera.Data.Construction
    /// These interfaces use it but it's forward-declared to avoid circular dependencies
    /// </summary>

    // Forward declaration placeholder - actual type in ProjectChimera.Data.Construction
    [System.Serializable]
    public class SchematicItem
    {
        public string ItemName;
        public string ItemCategory;
        public Vector3Int GridPosition;
        public int Height;
    }

    // Implementations will use the real type
    public class SchematicSO : ScriptableObject
    {
        public string SchematicName;
        public string SchematicId;
        public string Description;
        public string CreatedBy;
        public Vector3Int Size;
        public List<SchematicItem> Items = new List<SchematicItem>();
    }

    // GridItem - Basic grid placement data
    [System.Serializable]
    public class GridItem
    {
        public string ItemId;
        public Vector3Int Position;
        public SchematicSO Schematic;
    }

    // ConstructionSaveData - Save data structure
    [System.Serializable]
    public class ConstructionSaveData
    {
        public string SaveId;
        public DateTime SaveTime;
        public GridItem[] PlacedItems;
    }

    public interface IGridInputHandler
    {
        bool IsInputEnabled { get; set; }
        bool IsInitialized { get; }
        void Initialize();
        void EnableInput();
        void DisableInput();
        Vector3Int GetMouseGridPosition();
        bool IsValidGridPosition(Vector3Int position);
    }

    public interface IGridPlacementController
    {
        bool IsInitialized { get; }
        void Initialize();
        Task<bool> PlaceItemAsync(Vector3Int gridPosition, SchematicSO schematic);
        bool CanPlaceItem(Vector3Int gridPosition, SchematicSO schematic);
        bool RemoveItem(Vector3Int gridPosition);
        GridItem GetItemAt(Vector3Int gridPosition);
    }

    public interface IConstructionSaveProvider
    {
        bool IsInitialized { get; }
        void Initialize();
        Task SaveConstructionDataAsync();
        Task LoadConstructionDataAsync();
        ConstructionSaveData GetCurrentSaveData();
        void ApplySaveData(ConstructionSaveData saveData);
    }

    public interface IGridSystem
    {
        bool IsInitialized { get; }
        Vector3Int GridSize { get; }
        void Initialize();
        bool IsValidPosition(Vector3Int position);
        bool IsOccupied(Vector3Int position);
        bool CanPlace(SchematicSO schematic, Vector3Int position);
        void SetOccupied(Vector3Int position, GridItem item);
        void SetEmpty(Vector3Int position);
        GridItem GetItemAt(Vector3Int position);
        Vector3 GridToWorld(Vector3Int gridPosition);
        Vector3Int WorldToGrid(Vector3 worldPosition);
    }

    public interface IConstructionManager
    {
        bool IsInitialized { get; }
        void Initialize();
        Task<bool> PlaceStructureAsync(SchematicSO schematic, Vector3Int position);
        bool RemoveStructure(Vector3Int position);
        bool CanPlaceStructure(SchematicSO schematic, Vector3Int position);
        GridItem[] GetAllPlacedItems();
    }
}
