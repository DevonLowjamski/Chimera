using UnityEngine;
using System.Collections.Generic;

namespace ProjectChimera.Data.Construction
{
    /// <summary>
    /// SIMPLE: Basic schematic data structure aligned with Project Chimera's construction vision.
    /// Focuses on essential schematic functionality for saving and loading construction layouts.
    /// </summary>
    [CreateAssetMenu(fileName = "New Schematic", menuName = "Project Chimera/Construction/Schematic")]
    public class SchematicSO : ScriptableObject
    {
        [Header("Basic Schematic Info")]
        [SerializeField] private string _schematicId = "";
        [SerializeField] private string _schematicName = "New Schematic";
        [SerializeField] private string _description = "";
        [SerializeField] private string _createdBy = "";
        [SerializeField] private Sprite _previewIcon;
        [SerializeField] private Vector3Int _size = Vector3Int.one;

        [Header("Construction Items")]
        [SerializeField] private List<SchematicItem> _items = new List<SchematicItem>();

        // Basic properties
        public string SchematicId => _schematicId;
        public string SchematicName => _schematicName;
        public string Description => _description;
        public string CreatedBy => _createdBy;
        public Sprite PreviewIcon => _previewIcon;
        public Vector3Int Size => _size;
        public List<SchematicItem> Items => new List<SchematicItem>(_items);
        public int ItemCount => _items.Count;

        /// <summary>
        /// Add an item to the schematic
        /// </summary>
        public void AddItem(SchematicItem item)
        {
            if (item != null && !_items.Contains(item))
            {
                _items.Add(item);
            }
        }

        /// <summary>
        /// Remove an item from the schematic
        /// </summary>
        public bool RemoveItem(SchematicItem item)
        {
            return _items.Remove(item);
        }

        /// <summary>
        /// Clear all items
        /// </summary>
        public void ClearItems()
        {
            _items.Clear();
        }

        /// <summary>
        /// Set schematic basic properties (for runtime creation)
        /// </summary>
        public void SetSchematicId(string id) => _schematicId = id;
        public void SetSchematicName(string name) => _schematicName = name;
        public void SetDescription(string description) => _description = description;
        public void SetCreatedBy(string createdBy) => _createdBy = createdBy;
        public void SetSize(Vector3Int size) => _size = size;
        public void SetPreviewIcon(Sprite icon) => _previewIcon = icon;

        /// <summary>
        /// Get schematic summary
        /// </summary>
        public SchematicSummary GetSummary()
        {
            return new SchematicSummary
            {
                Name = _schematicName,
                Description = _description,
                ItemCount = _items.Count,
                HasPreview = _previewIcon != null
            };
        }

        /// <summary>
        /// Validate schematic
        /// </summary>
        public bool Validate()
        {
            if (string.IsNullOrEmpty(_schematicName)) return false;
            if (_items == null) return false;

            // Check that all items are valid
            foreach (var item in _items)
            {
                if (item == null || string.IsNullOrEmpty(item.ItemType)) return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Basic schematic item data
    /// </summary>
    [System.Serializable]
    public class SchematicItem
    {
        [SerializeField] private string _itemType;
        [SerializeField] private string _itemId;
        [SerializeField] private string _itemName;
        [SerializeField] private string _itemCategory;
        [SerializeField] private Vector3Int _position;
        [SerializeField] private Vector3Int _gridPosition;
        [SerializeField] private int _height;
        [SerializeField] private Quaternion _rotation = Quaternion.identity;
        [SerializeField] private Vector3 _scale = Vector3.one;

        public string ItemType => _itemType;
        public string ItemId => _itemId;
        public string ItemName => _itemName;
        public string ItemCategory => _itemCategory;
        public Vector3Int Position => _position;
        public Vector3Int GridPosition => _gridPosition;
        public int Height => _height;
        public Quaternion Rotation => _rotation;
        public Vector3 Scale => _scale;

        public SchematicItem(string itemType, string itemId, Vector3Int position)
        {
            _itemType = itemType;
            _itemId = itemId;
            _position = position;
        }

        // Setter methods for runtime modification
        public void SetItemName(string name) => _itemName = name;
        public void SetItemCategory(string category) => _itemCategory = category;
        public void SetGridPosition(Vector3Int gridPos) => _gridPosition = gridPos;
        public void SetHeight(int height) => _height = height;
        public void SetRotation(Quaternion rotation) => _rotation = rotation;
        public void SetScale(Vector3 scale) => _scale = scale;
    }

    /// <summary>
    /// Schematic utilities
    /// </summary>
    public static class SchematicUtils
    {
        /// <summary>
        /// Create a basic schematic
        /// </summary>
        public static SchematicSO CreateBasicSchematic(string name, string description = "")
        {
            var schematic = ScriptableObject.CreateInstance<SchematicSO>();
            // Note: In a real implementation, you'd set the private fields through methods or make them public
            return schematic;
        }

        /// <summary>
        /// Create a schematic item
        /// </summary>
        public static SchematicItem CreateSchematicItem(string itemType, string itemId, Vector3Int position)
        {
            return new SchematicItem(itemType, itemId, position);
        }

        /// <summary>
        /// Get bounding box for schematic items
        /// </summary>
        public static Bounds GetBoundingBox(List<SchematicItem> items)
        {
            if (items == null || items.Count == 0) return new Bounds();

            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;

            foreach (var item in items)
            {
                min = Vector3.Min(min, item.Position);
                max = Vector3.Max(max, item.Position + Vector3.one);
            }

            return new Bounds((min + max) / 2, max - min);
        }

        /// <summary>
        /// Calculate schematic complexity
        /// </summary>
        public static int CalculateComplexity(List<SchematicItem> items)
        {
            if (items == null) return 0;

            // Simple complexity calculation based on item count
            if (items.Count <= 5) return 1; // Simple
            if (items.Count <= 20) return 2; // Medium
            return 3; // Complex
        }

        /// <summary>
        /// Check if schematic is valid
        /// </summary>
        public static bool IsValid(SchematicSO schematic)
        {
            return schematic != null && schematic.Validate();
        }
    }

    /// <summary>
    /// Schematic summary
    /// </summary>
    [System.Serializable]
    public class SchematicSummary
    {
        public string Name;
        public string Description;
        public int ItemCount;
        public bool HasPreview;
    }
}
