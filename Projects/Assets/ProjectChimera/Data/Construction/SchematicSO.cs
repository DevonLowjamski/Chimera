using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Shared;

namespace ProjectChimera.Data.Construction
{
    /// <summary>
    /// ScriptableObject representing a saved construction schematic layout.
    /// Contains a collection of construction items with their positions, rotations, and configurations.
    /// Used for saving and applying construction templates in Phase 4 of the roadmap.
    /// </summary>
    [CreateAssetMenu(fileName = "New Construction Schematic", menuName = "Project Chimera/Construction/Construction Layout Schematic")]
    public class SchematicSO : ChimeraScriptableObject
    {
        [Header("Schematic Information")]
        [SerializeField] private string _schematicName = "New Schematic";
        [SerializeField] private string _description = "";
        [SerializeField] private Sprite _previewIcon;
        [SerializeField] private Texture2D _previewImage;
        [SerializeField] private ConstructionCategory _primaryCategory = ConstructionCategory.Structure;
        [SerializeField] private List<ConstructionCategory> _includedCategories = new List<ConstructionCategory>();
        
        [Header("Schematic Metadata")]
        [SerializeField] private string _createdBy = "Unknown";
        [SerializeField] private DateTime _creationDate = DateTime.Now;
        [SerializeField] private string _version = "1.0";
        [SerializeField] private Vector3Int _boundingSize = Vector3Int.one;
        [SerializeField] private Vector3Int _anchorPoint = Vector3Int.zero;
        
        [Header("Unlock Requirements")]
        [SerializeField] private bool _requiresUnlock = false;
        [SerializeField] private float _skillPointCost = 0f;
        [SerializeField] private int _requiredLevel = 1;
        [SerializeField] private List<string> _prerequisiteSchematicIds = new List<string>();
        
        [Header("Construction Items")]
        [SerializeField] private List<SchematicItem> _items = new List<SchematicItem>();
        
        [Header("Cost Information")]
        [SerializeField] private float _totalEstimatedCost = 0f;
        [SerializeField] private float _totalConstructionTime = 0f;
        [SerializeField] private bool _autoCalculateCosts = true;
        
        [Header("Tags and Filtering")]
        [SerializeField] private List<string> _tags = new List<string>();
        [SerializeField] private SchematicComplexity _complexity = SchematicComplexity.Simple;
        [SerializeField] private SchematicUsageType _usageType = SchematicUsageType.General;
        
        // Public Properties
        public string SchematicName { get => _schematicName; set => _schematicName = value; }
        public string Description { get => _description; set => _description = value; }
        public Sprite PreviewIcon => _previewIcon;
        public Texture2D PreviewImage => _previewImage;
        public ConstructionCategory PrimaryCategory => _primaryCategory;
        public ConstructionCategory Category => _primaryCategory; // Alias for PrimaryCategory for compatibility
        public List<ConstructionCategory> IncludedCategories => new List<ConstructionCategory>(_includedCategories);
        public string CreatedBy { get => _createdBy; set => _createdBy = value; }
        public DateTime CreationDate => _creationDate;
        public string Version => _version;
        public Vector3Int BoundingSize => _boundingSize;
        public Vector3Int AnchorPoint => _anchorPoint;
        public bool RequiresUnlock => _requiresUnlock;
        public float SkillPointCost => _skillPointCost;
        public int RequiredLevel => _requiredLevel;
        public List<string> PrerequisiteSchematicIds => new List<string>(_prerequisiteSchematicIds);
        public List<SchematicItem> Items => new List<SchematicItem>(_items);
        public float TotalEstimatedCost => _autoCalculateCosts ? CalculateTotalCost() : _totalEstimatedCost;
        public float TotalConstructionTime => _autoCalculateCosts ? CalculateTotalConstructionTime() : _totalConstructionTime;
        public List<string> Tags => new List<string>(_tags);
        public SchematicComplexity Complexity => _complexity;
        public SchematicUsageType UsageType => _usageType;
        public int ItemCount => _items.Count;
        
        /// <summary>
        /// Add an item to this schematic
        /// </summary>
        public void AddItem(SchematicItem item)
        {
            if (item != null && !_items.Contains(item))
            {
                _items.Add(item);
                RefreshMetadata();
            }
        }
        
        /// <summary>
        /// Remove an item from this schematic
        /// </summary>
        public bool RemoveItem(SchematicItem item)
        {
            if (_items.Remove(item))
            {
                RefreshMetadata();
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Clear all items from this schematic
        /// </summary>
        public void ClearItems()
        {
            _items.Clear();
            RefreshMetadata();
        }
        
        /// <summary>
        /// Get items by category
        /// </summary>
        public List<SchematicItem> GetItemsByCategory(ConstructionCategory category)
        {
            return _items.FindAll(item => item.ItemCategory == category);
        }
        
        /// <summary>
        /// Get items within a specific bounds
        /// </summary>
        public List<SchematicItem> GetItemsInBounds(Vector3Int min, Vector3Int max)
        {
            var itemsInBounds = new List<SchematicItem>();
            
            foreach (var item in _items)
            {
                var pos = item.GridPosition;
                if (pos.x >= min.x && pos.x <= max.x && pos.y >= min.y && pos.y <= max.y)
                {
                    itemsInBounds.Add(item);
                }
            }
            
            return itemsInBounds;
        }
        
        /// <summary>
        /// Calculate total cost of all items in schematic
        /// </summary>
        public float CalculateTotalCost()
        {
            float totalCost = 0f;
            foreach (var item in _items)
            {
                totalCost += item.EstimatedCost;
            }
            return totalCost;
        }
        
        /// <summary>
        /// Calculate total construction time for all items
        /// </summary>
        public float CalculateTotalConstructionTime()
        {
            float totalTime = 0f;
            foreach (var item in _items)
            {
                totalTime += item.ConstructionTime;
            }
            return totalTime;
        }
        
        /// <summary>
        /// Calculate bounding size from all items
        /// </summary>
        public Vector3Int CalculateBoundingSize()
        {
            if (_items.Count == 0) return Vector3Int.one;
            
            Vector3Int min = _items[0].GridPosition;
            Vector3Int max = _items[0].GridPosition + _items[0].GridSize;
            
            foreach (var item in _items)
            {
                var itemMin = item.GridPosition;
                var itemMax = item.GridPosition + item.GridSize;
                
                min = Vector3Int.Min(min, itemMin);
                max = Vector3Int.Max(max, itemMax);
            }
            
            return max - min;
        }
        
        /// <summary>
        /// Get all grid positions that would be occupied if placed at given position
        /// </summary>
        public List<Vector3Int> GetOccupiedPositions(Vector3Int gridPosition)
        {
            var occupiedPositions = new List<Vector3Int>();
            
            foreach (var item in _items)
            {
                var worldPos = gridPosition + item.GridPosition;
                var itemSize = item.GridSize;
                
                for (int x = 0; x < itemSize.x; x++)
                {
                    for (int y = 0; y < itemSize.y; y++)
                    {
                        occupiedPositions.Add(worldPos + new Vector3Int(x, y, 0));
                    }
                }
            }
            
            return occupiedPositions;
        }
        
        /// <summary>
        /// Refresh metadata based on current items
        /// </summary>
        private void RefreshMetadata()
        {
            // Update bounding size
            _boundingSize = CalculateBoundingSize();
            
            // Update included categories
            _includedCategories.Clear();
            foreach (var item in _items)
            {
                if (!_includedCategories.Contains(item.ItemCategory))
                {
                    _includedCategories.Add(item.ItemCategory);
                }
            }
            
            // Update complexity based on item count and categories
            if (_items.Count <= 5)
                _complexity = SchematicComplexity.Simple;
            else if (_items.Count <= 15)
                _complexity = SchematicComplexity.Moderate;
            else if (_items.Count <= 30)
                _complexity = SchematicComplexity.Complex;
            else
                _complexity = SchematicComplexity.Advanced;
        }
        
        /// <summary>
        /// Create a copy of this schematic with a new name
        /// </summary>
        public SchematicSO CreateCopy(string newName)
        {
            var copy = CreateInstance<SchematicSO>();
            
            copy._schematicName = newName;
            copy._description = _description;
            copy._previewIcon = _previewIcon;
            copy._previewImage = _previewImage;
            copy._primaryCategory = _primaryCategory;
            copy._includedCategories = new List<ConstructionCategory>(_includedCategories);
            copy._createdBy = _createdBy;
            copy._creationDate = DateTime.Now;
            copy._version = _version;
            copy._boundingSize = _boundingSize;
            copy._anchorPoint = _anchorPoint;
            copy._requiresUnlock = _requiresUnlock;
            copy._skillPointCost = _skillPointCost;
            copy._requiredLevel = _requiredLevel;
            copy._prerequisiteSchematicIds = new List<string>(_prerequisiteSchematicIds);
            copy._tags = new List<string>(_tags);
            copy._complexity = _complexity;
            copy._usageType = _usageType;
            copy._autoCalculateCosts = _autoCalculateCosts;
            
            // Deep copy items
            copy._items = new List<SchematicItem>();
            foreach (var item in _items)
            {
                copy._items.Add(item.CreateCopy());
            }
            
            return copy;
        }
        
        protected override bool ValidateDataSpecific()
        {
            bool isValid = true;
            
            if (string.IsNullOrEmpty(_schematicName))
            {
                Debug.LogError($"Schematic {name}: Schematic name cannot be empty", this);
                isValid = false;
            }
            
            if (_items.Count == 0)
            {
                Debug.LogWarning($"Schematic {name}: No items defined in schematic", this);
            }
            
            if (_skillPointCost < 0f)
            {
                Debug.LogError($"Schematic {name}: Skill point cost cannot be negative", this);
                isValid = false;
            }
            
            if (_requiredLevel < 1)
            {
                Debug.LogError($"Schematic {name}: Required level must be at least 1", this);
                isValid = false;
            }
            
            // Validate items
            foreach (var item in _items)
            {
                if (!item.IsValid())
                {
                    Debug.LogError($"Schematic {name}: Invalid item found - {item.ItemName}", this);
                    isValid = false;
                }
            }
            
            return isValid;
        }
    }
    
    /// <summary>
    /// Individual item within a construction schematic
    /// </summary>
    [System.Serializable]
    public class SchematicItem
    {
        [Header("Item Identity")]
        [SerializeField] private string _itemName = "";
        [SerializeField] private string _itemId = "";
        [SerializeField] private ConstructionCategory _itemCategory = ConstructionCategory.Structure;
        [SerializeField] private string _templateName = ""; // Reference to GridConstructionTemplate
        
        [Header("Positioning")]
        [SerializeField] private Vector3Int _gridPosition = Vector3Int.zero;
        [SerializeField] private Vector3Int _gridSize = Vector3Int.one;
        [SerializeField] private int _rotation = 0; // 0, 90, 180, 270 degrees
        [SerializeField] private float _height = 0f;
        
        [Header("Addressable References")]
        [SerializeField] private string _prefabAddressableId = "";
        [SerializeField] private string _previewAddressableId = "";
        [SerializeField] private bool _useAddressableSystem = false;
        
        [Header("Direct References (Fallback)")]
        [SerializeField] private GameObject _prefabReference;
        [SerializeField] private GameObject _previewPrefabReference;
        
        [Header("Item Configuration")]
        [SerializeField] private List<SchematicItemProperty> _customProperties = new List<SchematicItemProperty>();
        [SerializeField] private Dictionary<string, object> _configurationData = new Dictionary<string, object>();
        
        [Header("Cost Information")]
        [SerializeField] private float _estimatedCost = 0f;
        [SerializeField] private float _constructionTime = 60f;
        [SerializeField] private List<string> _requiredResources = new List<string>();
        
        // Public Properties
        public string ItemName => _itemName;
        public string ItemId => _itemId;
        public ConstructionCategory ItemCategory => _itemCategory;
        public string TemplateName => _templateName;
        public Vector3Int GridPosition { get => _gridPosition; set => _gridPosition = value; }
        public Vector3Int GridSize => _gridSize;
        public int Rotation { get => _rotation; set => _rotation = value; }
        public float Height => _height;
        public string PrefabAddressableId => _prefabAddressableId;
        public string PreviewAddressableId => _previewAddressableId;
        public bool UseAddressableSystem => _useAddressableSystem;
        public GameObject PrefabReference => _prefabReference;
        public GameObject PreviewPrefabReference => _previewPrefabReference;
        public List<SchematicItemProperty> CustomProperties => new List<SchematicItemProperty>(_customProperties);
        public float EstimatedCost => _estimatedCost;
        public float ConstructionTime => _constructionTime;
        public List<string> RequiredResources => new List<string>(_requiredResources);
        
        /// <summary>
        /// Constructor for creating a new schematic item
        /// </summary>
        public SchematicItem(string itemName, string templateName, Vector3Int gridPosition, int rotation = 0)
        {
            _itemName = itemName;
            _templateName = templateName;
            _gridPosition = gridPosition;
            _rotation = rotation;
            _itemId = Guid.NewGuid().ToString();
        }
        
        /// <summary>
        /// Default constructor for serialization
        /// </summary>
        public SchematicItem()
        {
            _itemId = Guid.NewGuid().ToString();
        }
        
        /// <summary>
        /// Get configuration value by key
        /// </summary>
        public T GetConfigValue<T>(string key, T defaultValue = default(T))
        {
            if (_configurationData.TryGetValue(key, out var value) && value is T)
            {
                return (T)value;
            }
            return defaultValue;
        }
        
        /// <summary>
        /// Set configuration value
        /// </summary>
        public void SetConfigValue<T>(string key, T value)
        {
            _configurationData[key] = value;
        }
        
        /// <summary>
        /// Add or update custom property
        /// </summary>
        public void SetCustomProperty(string propertyName, object value, SchematicPropertyType type)
        {
            var existingProperty = _customProperties.Find(p => p.PropertyName == propertyName);
            if (existingProperty != null)
            {
                existingProperty.SetValue(value, type);
            }
            else
            {
                _customProperties.Add(new SchematicItemProperty(propertyName, value, type));
            }
        }
        
        /// <summary>
        /// Get custom property value
        /// </summary>
        public T GetCustomProperty<T>(string propertyName, T defaultValue = default(T))
        {
            var property = _customProperties.Find(p => p.PropertyName == propertyName);
            if (property != null && property.GetValue() is T)
            {
                return (T)property.GetValue();
            }
            return defaultValue;
        }
        
        /// <summary>
        /// Calculate world bounds for this item
        /// </summary>
        public Bounds GetWorldBounds(Vector3 worldPosition, float gridSize)
        {
            Vector3 size = new Vector3(_gridSize.x * gridSize, 1f, _gridSize.y * gridSize);
            Vector3 center = worldPosition + new Vector3(size.x * 0.5f, _height, size.z * 0.5f);
            return new Bounds(center, size);
        }
        
        /// <summary>
        /// Check if this item overlaps with another item
        /// </summary>
        public bool OverlapsWith(SchematicItem other)
        {
            Vector3Int thisMin = _gridPosition;
            Vector3Int thisMax = _gridPosition + _gridSize;
            Vector3Int otherMin = other._gridPosition;
            Vector3Int otherMax = other._gridPosition + other._gridSize;
            
            return !(thisMax.x <= otherMin.x || thisMin.x >= otherMax.x ||
                     thisMax.y <= otherMin.y || thisMin.y >= otherMax.y);
        }
        
        /// <summary>
        /// Validate this item's data
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(_itemName)) return false;
            if (string.IsNullOrEmpty(_itemId)) return false;
            if (_gridSize.x <= 0 || _gridSize.y <= 0) return false;
            if (_rotation < 0 || _rotation >= 360) return false;
            if (_estimatedCost < 0f) return false;
            if (_constructionTime < 0f) return false;
            
            // Check that we have either addressable ID or direct reference
            if (_useAddressableSystem)
            {
                if (string.IsNullOrEmpty(_prefabAddressableId)) return false;
            }
            else
            {
                if (_prefabReference == null) return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Create a copy of this item
        /// </summary>
        public SchematicItem CreateCopy()
        {
            var copy = new SchematicItem();
            
            copy._itemName = _itemName;
            copy._itemId = Guid.NewGuid().ToString(); // New ID for copy
            copy._itemCategory = _itemCategory;
            copy._templateName = _templateName;
            copy._gridPosition = _gridPosition;
            copy._gridSize = _gridSize;
            copy._rotation = _rotation;
            copy._height = _height;
            copy._prefabAddressableId = _prefabAddressableId;
            copy._previewAddressableId = _previewAddressableId;
            copy._useAddressableSystem = _useAddressableSystem;
            copy._prefabReference = _prefabReference;
            copy._previewPrefabReference = _previewPrefabReference;
            copy._estimatedCost = _estimatedCost;
            copy._constructionTime = _constructionTime;
            copy._requiredResources = new List<string>(_requiredResources);
            
            // Deep copy custom properties
            copy._customProperties = new List<SchematicItemProperty>();
            foreach (var property in _customProperties)
            {
                copy._customProperties.Add(property.CreateCopy());
            }
            
            // Deep copy configuration data
            copy._configurationData = new Dictionary<string, object>(_configurationData);
            
            return copy;
        }
    }
    
    /// <summary>
    /// Custom property for schematic items
    /// </summary>
    [System.Serializable]
    public class SchematicItemProperty
    {
        [SerializeField] private string _propertyName;
        [SerializeField] private SchematicPropertyType _propertyType;
        [SerializeField] private string _stringValue;
        [SerializeField] private float _floatValue;
        [SerializeField] private int _intValue;
        [SerializeField] private bool _boolValue;
        [SerializeField] private Vector3 _vector3Value;
        [SerializeField] private Color _colorValue;
        
        public string PropertyName => _propertyName;
        public SchematicPropertyType PropertyType => _propertyType;
        
        public SchematicItemProperty(string propertyName, object value, SchematicPropertyType type)
        {
            _propertyName = propertyName;
            _propertyType = type;
            SetValue(value, type);
        }
        
        public object GetValue()
        {
            switch (_propertyType)
            {
                case SchematicPropertyType.String:
                    return _stringValue;
                case SchematicPropertyType.Float:
                    return _floatValue;
                case SchematicPropertyType.Int:
                    return _intValue;
                case SchematicPropertyType.Bool:
                    return _boolValue;
                case SchematicPropertyType.Vector3:
                    return _vector3Value;
                case SchematicPropertyType.Color:
                    return _colorValue;
                default:
                    return null;
            }
        }
        
        public void SetValue(object value, SchematicPropertyType type)
        {
            _propertyType = type;
            
            switch (type)
            {
                case SchematicPropertyType.String:
                    _stringValue = value?.ToString() ?? "";
                    break;
                case SchematicPropertyType.Float:
                    _floatValue = Convert.ToSingle(value);
                    break;
                case SchematicPropertyType.Int:
                    _intValue = Convert.ToInt32(value);
                    break;
                case SchematicPropertyType.Bool:
                    _boolValue = Convert.ToBoolean(value);
                    break;
                case SchematicPropertyType.Vector3:
                    _vector3Value = (Vector3)value;
                    break;
                case SchematicPropertyType.Color:
                    _colorValue = (Color)value;
                    break;
            }
        }
        
        public SchematicItemProperty CreateCopy()
        {
            return new SchematicItemProperty(_propertyName, GetValue(), _propertyType);
        }
    }
    
    /// <summary>
    /// Types of properties that can be stored in schematic items
    /// </summary>
    public enum SchematicPropertyType
    {
        String,
        Float,
        Int,
        Bool,
        Vector3,
        Color
    }
    
    /// <summary>
    /// Complexity levels for schematics
    /// </summary>
    public enum SchematicComplexity
    {
        All,        // For filtering - shows all complexities
        Simple,     // 1-5 items
        Moderate,   // 6-15 items
        Complex,    // 16-30 items
        Advanced    // 30+ items
    }
    
    /// <summary>
    /// Usage types for schematics
    /// </summary>
    public enum SchematicUsageType
    {
        General,        // General construction
        Specialized,    // Specialized facility setup
        Starter,        // Beginner-friendly layouts
        Production,     // Production-focused layouts
        Research,       // Research facility layouts
        Commercial,     // Commercial facility layouts
        Template,       // Base templates for customization
        Complete        // Complete facility designs
    }
}