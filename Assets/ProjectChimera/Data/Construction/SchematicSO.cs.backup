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
        [SerializeField] private string _schematicName = "New Schematic";
        [SerializeField] private string _description = "";
        [SerializeField] private Sprite _previewIcon;

        [Header("Construction Items")]
        [SerializeField] private List<SchematicItem> _items = new List<SchematicItem>();

        // Basic properties
        public string SchematicName => _schematicName;
        public string Description => _description;
        public Sprite PreviewIcon => _previewIcon;
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
        [SerializeField] private Vector3Int _position;
        [SerializeField] private Quaternion _rotation = Quaternion.identity;
        [SerializeField] private Vector3 _scale = Vector3.one;

        public string ItemType => _itemType;
        public string ItemId => _itemId;
        public Vector3Int Position => _position;
        public Quaternion Rotation => _rotation;
        public Vector3 Scale => _scale;

        public SchematicItem(string itemType, string itemId, Vector3Int position)
        {
            _itemType = itemType;
            _itemId = itemId;
            _position = position;
        }
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
