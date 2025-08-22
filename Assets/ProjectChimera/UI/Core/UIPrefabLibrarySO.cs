using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core;
using ProjectChimera.Shared;
using ProjectChimera.UI.Core;

namespace ProjectChimera.UI.Core
{
    /// <summary>
    /// ScriptableObject library containing all UI prefab definitions and metadata.
    /// Provides centralized management of UI component prefabs for Project Chimera.
    /// </summary>
    [CreateAssetMenu(fileName = "New UI Prefab Library", menuName = "Project Chimera/UI/Prefab Library")]
    public class UIPrefabLibrarySO : ChimeraDataSO
    {
        [Header("Library Configuration")]
        [SerializeField] private string _libraryName = "UI Prefab Library";
        [SerializeField] private string _version = "1.0.0";
        [SerializeField] private bool _enableCaching = true;
        [SerializeField] private bool _validateOnLoad = true;
        
        [Header("Prefab Categories")]
        [SerializeField] private List<UIPrefabEntry> _componentPrefabs = new List<UIPrefabEntry>();
        [SerializeField] private List<UIPrefabEntry> _widgetPrefabs = new List<UIPrefabEntry>();
        [SerializeField] private List<UIPrefabEntry> _modalPrefabs = new List<UIPrefabEntry>();
        [SerializeField] private List<UIPrefabEntry> _notificationPrefabs = new List<UIPrefabEntry>();
        
        [Header("Default Configurations")]
        [SerializeField] private string _defaultComponentPrefab = "basic-component";
        [SerializeField] private string _defaultWidgetPrefab = "basic-widget";
        [SerializeField] private string _defaultModalPrefab = "basic-modal";
        [SerializeField] private string _defaultNotificationPrefab = "basic-notification";
        
        // Cached lookups for performance
        private Dictionary<string, UIPrefabEntry> _prefabCache;
        private Dictionary<UIComponentPrefab, string> _prefabIdCache;
        private bool _cacheInitialized = false;
        
        // Properties
        public string LibraryName => _libraryName;
        public string Version => _version;
        public bool EnableCaching => _enableCaching;
        public int TotalPrefabCount => _componentPrefabs.Count + _widgetPrefabs.Count + _modalPrefabs.Count + _notificationPrefabs.Count;
        public int ComponentPrefabCount => _componentPrefabs.Count;
        public int WidgetPrefabCount => _widgetPrefabs.Count;
        public int ModalPrefabCount => _modalPrefabs.Count;
        public int NotificationPrefabCount => _notificationPrefabs.Count;
        
        /// <summary>
        /// Initialize the library and cache
        /// </summary>
        public void Initialize()
        {
            if (_enableCaching)
            {
                InitializeCache();
            }
            
            if (_validateOnLoad)
            {
                ValidateLibrary();
            }
        }
        
        /// <summary>
        /// Initialize prefab cache for fast lookups
        /// </summary>
        private void InitializeCache()
        {
            if (_cacheInitialized)
                return;
                
            _prefabCache = new Dictionary<string, UIPrefabEntry>();
            _prefabIdCache = new Dictionary<UIComponentPrefab, string>();
            
            var allPrefabs = GetAllPrefabs();
            foreach (var entry in allPrefabs)
            {
                if (!string.IsNullOrEmpty(entry.PrefabId) && entry.Prefab != null)
                {
                    _prefabCache[entry.PrefabId] = entry;
                    _prefabIdCache[entry.Prefab] = entry.PrefabId;
                }
            }
            
            _cacheInitialized = true;
        }
        
        /// <summary>
        /// Get prefab by ID
        /// </summary>
        public UIComponentPrefab GetPrefab(string prefabId)
        {
            if (_enableCaching)
            {
                if (!_cacheInitialized)
                    InitializeCache();
                    
                return _prefabCache.TryGetValue(prefabId, out var entry) ? entry.Prefab : null;
            }
            
            return GetAllPrefabs().FirstOrDefault(p => p.PrefabId == prefabId)?.Prefab;
        }
        
        /// <summary>
        /// Get prefab entry by ID
        /// </summary>
        public UIPrefabEntry GetPrefabEntry(string prefabId)
        {
            if (_enableCaching)
            {
                if (!_cacheInitialized)
                    InitializeCache();
                    
                return _prefabCache.TryGetValue(prefabId, out var entry) ? entry : null;
            }
            
            return GetAllPrefabs().FirstOrDefault(p => p.PrefabId == prefabId);
        }
        
        /// <summary>
        /// Get prefab ID by prefab reference
        /// </summary>
        public string GetPrefabId(UIComponentPrefab prefab)
        {
            if (prefab == null)
                return null;
                
            if (_enableCaching)
            {
                if (!_cacheInitialized)
                    InitializeCache();
                    
                return _prefabIdCache.TryGetValue(prefab, out var id) ? id : null;
            }
            
            return GetAllPrefabs().FirstOrDefault(p => p.Prefab == prefab)?.PrefabId;
        }
        
        /// <summary>
        /// Get all prefabs in library
        /// </summary>
        public List<UIPrefabEntry> GetAllPrefabs()
        {
            var allPrefabs = new List<UIPrefabEntry>();
            allPrefabs.AddRange(_componentPrefabs);
            allPrefabs.AddRange(_widgetPrefabs);
            allPrefabs.AddRange(_modalPrefabs);
            allPrefabs.AddRange(_notificationPrefabs);
            return allPrefabs;
        }
        
        /// <summary>
        /// Get prefabs by category
        /// </summary>
        public List<UIPrefabEntry> GetPrefabsByCategory(UIPrefabCategory category)
        {
            return category switch
            {
                UIPrefabCategory.Component => new List<UIPrefabEntry>(_componentPrefabs),
                UIPrefabCategory.Widget => new List<UIPrefabEntry>(_widgetPrefabs),
                UIPrefabCategory.Modal => new List<UIPrefabEntry>(_modalPrefabs),
                UIPrefabCategory.Notification => new List<UIPrefabEntry>(_notificationPrefabs),
                _ => new List<UIPrefabEntry>()
            };
        }
        
        /// <summary>
        /// Get prefabs by tag
        /// </summary>
        public List<UIPrefabEntry> GetPrefabsByTag(string tag)
        {
            return GetAllPrefabs().Where(p => p.Tags.Contains(tag)).ToList();
        }
        
        /// <summary>
        /// Get prefabs by type
        /// </summary>
        public List<UIPrefabEntry> GetPrefabsByType<T>() where T : UIComponentPrefab
        {
            return GetAllPrefabs().Where(p => p.Prefab is T).ToList();
        }
        
        /// <summary>
        /// Add prefab to library
        /// </summary>
        public bool AddPrefab(UIPrefabEntry entry)
        {
            if (entry == null || entry.Prefab == null || string.IsNullOrEmpty(entry.PrefabId))
            {
                return false;
            }
            
            // Check for duplicate ID
            if (GetPrefab(entry.PrefabId) != null)
            {
                return false;
            }
            
            // Add to appropriate category
            var targetList = GetCategoryList(entry.Category);
            targetList.Add(entry);
            
            // Update cache
            if (_enableCaching && _cacheInitialized)
            {
                _prefabCache[entry.PrefabId] = entry;
                _prefabIdCache[entry.Prefab] = entry.PrefabId;
            }
            
            return true;
        }
        
        /// <summary>
        /// Remove prefab from library
        /// </summary>
        public bool RemovePrefab(string prefabId)
        {
            var entry = GetPrefabEntry(prefabId);
            if (entry == null)
                return false;
                
            var targetList = GetCategoryList(entry.Category);
            var removed = targetList.Remove(entry);
            
            // Update cache
            if (_enableCaching && _cacheInitialized)
            {
                _prefabCache.Remove(prefabId);
                _prefabIdCache.Remove(entry.Prefab);
            }
            
            return removed;
        }
        
        /// <summary>
        /// Get category list by category type
        /// </summary>
        private List<UIPrefabEntry> GetCategoryList(UIPrefabCategory category)
        {
            return category switch
            {
                UIPrefabCategory.Component => _componentPrefabs,
                UIPrefabCategory.Widget => _widgetPrefabs,
                UIPrefabCategory.Modal => _modalPrefabs,
                UIPrefabCategory.Notification => _notificationPrefabs,
                _ => _componentPrefabs
            };
        }
        
        /// <summary>
        /// Get default prefab for category
        /// </summary>
        public UIComponentPrefab GetDefaultPrefab(UIPrefabCategory category)
        {
            var defaultId = category switch
            {
                UIPrefabCategory.Component => _defaultComponentPrefab,
                UIPrefabCategory.Widget => _defaultWidgetPrefab,
                UIPrefabCategory.Modal => _defaultModalPrefab,
                UIPrefabCategory.Notification => _defaultNotificationPrefab,
                _ => _defaultComponentPrefab
            };
            
            return GetPrefab(defaultId);
        }
        
        /// <summary>
        /// Set default prefab for category
        /// </summary>
        public void SetDefaultPrefab(UIPrefabCategory category, string prefabId)
        {
            switch (category)
            {
                case UIPrefabCategory.Component:
                    _defaultComponentPrefab = prefabId;
                    break;
                case UIPrefabCategory.Widget:
                    _defaultWidgetPrefab = prefabId;
                    break;
                case UIPrefabCategory.Modal:
                    _defaultModalPrefab = prefabId;
                    break;
                case UIPrefabCategory.Notification:
                    _defaultNotificationPrefab = prefabId;
                    break;
            }
        }
        
        /// <summary>
        /// Search prefabs by name or description
        /// </summary>
        public List<UIPrefabEntry> SearchPrefabs(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return new List<UIPrefabEntry>();
                
            var term = searchTerm.ToLower();
            return GetAllPrefabs().Where(p => 
                p.PrefabId.ToLower().Contains(term) ||
                p.DisplayName.ToLower().Contains(term) ||
                p.Description.ToLower().Contains(term) ||
                p.Tags.Any(tag => tag.ToLower().Contains(term))
            ).ToList();
        }
        
        /// <summary>
        /// Validate library integrity
        /// </summary>
        public bool ValidateLibrary()
        {
            bool isValid = true;
            var allPrefabs = GetAllPrefabs();
            var usedIds = new HashSet<string>();
            
            foreach (var entry in allPrefabs)
            {
                // Check for null prefab
                if (entry.Prefab == null)
                {
                    LogError($"Null prefab in entry: {entry.PrefabId}");
                    isValid = false;
                    continue;
                }
                
                // Check for empty ID
                if (string.IsNullOrEmpty(entry.PrefabId))
                {
                    LogError($"Empty prefab ID for: {entry.Prefab.name}");
                    isValid = false;
                    continue;
                }
                
                // Check for duplicate IDs
                if (usedIds.Contains(entry.PrefabId))
                {
                    LogError($"Duplicate prefab ID: {entry.PrefabId}");
                    isValid = false;
                }
                // else
                // {
                    usedIds.Add(entry.PrefabId);
                // }
                
                // Validate prefab component
                if (!entry.Prefab.ValidateComponent())
                {
                    LogWarning($"Prefab validation failed: {entry.PrefabId}");
                }
            }
            
            // Validate default prefabs exist
            ValidateDefaultPrefab(_defaultComponentPrefab, "Default component prefab");
            ValidateDefaultPrefab(_defaultWidgetPrefab, "Default widget prefab");
            ValidateDefaultPrefab(_defaultModalPrefab, "Default modal prefab");
            ValidateDefaultPrefab(_defaultNotificationPrefab, "Default notification prefab");
            
            return isValid;
        }
        
        /// <summary>
        /// Validate default prefab exists
        /// </summary>
        private void ValidateDefaultPrefab(string prefabId, string description)
        {
            if (!string.IsNullOrEmpty(prefabId) && GetPrefab(prefabId) == null)
            {
                LogWarning($"{description} not found: {prefabId}");
            }
        }
        
        /// <summary>
        /// Clear cache and reinitialize
        /// </summary>
        public void RefreshCache()
        {
            _cacheInitialized = false;
            _prefabCache?.Clear();
            _prefabIdCache?.Clear();
            
            if (_enableCaching)
            {
                InitializeCache();
            }
        }
        
        /// <summary>
        /// Get library statistics
        /// </summary>
        public UIPrefabLibraryStats GetStats()
        {
            return new UIPrefabLibraryStats
            {
                TotalPrefabs = TotalPrefabCount,
                ComponentPrefabs = ComponentPrefabCount,
                WidgetPrefabs = WidgetPrefabCount,
                ModalPrefabs = ModalPrefabCount,
                NotificationPrefabs = NotificationPrefabCount,
                CacheEnabled = _enableCaching,
                CacheInitialized = _cacheInitialized
            };
        }
        
        protected override void OnValidate()
        {
            base.OnValidate();
            
            if (string.IsNullOrEmpty(_libraryName))
            {
                _libraryName = name;
            }
            
            // Ensure version format
            if (string.IsNullOrEmpty(_version))
            {
                _version = "1.0.0";
            }
            
            // Validate prefab entries
            ValidatePrefabEntries(_componentPrefabs);
            ValidatePrefabEntries(_widgetPrefabs);
            ValidatePrefabEntries(_modalPrefabs);
            ValidatePrefabEntries(_notificationPrefabs);
        }
        
        /// <summary>
        /// Validate prefab entries in a list
        /// </summary>
        private void ValidatePrefabEntries(List<UIPrefabEntry> entries)
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var entry = entries[i];
                if (entry.Prefab == null || string.IsNullOrEmpty(entry.PrefabId))
                {
                    entries.RemoveAt(i);
                }
            }
        }
        
        protected override bool ValidateDataSpecific()
        {
            ValidateLibrary();
            return true;
        }
        
        /// <summary>
        /// Reset cache when destroyed
        /// </summary>
        private void OnDestroy()
        {
            _prefabCache?.Clear();
            _prefabIdCache?.Clear();
            _cacheInitialized = false;
        }
    }
    
    /// <summary>
    /// UI prefab entry definition
    /// </summary>
    [System.Serializable]
    public class UIPrefabEntry
    {
        [SerializeField] public string PrefabId;
        [SerializeField] public string DisplayName;
        [SerializeField] public string Description;
        [SerializeField] public UIPrefabCategory Category = UIPrefabCategory.Component;
        [SerializeField] public UIComponentPrefab Prefab;
        [SerializeField] public List<string> Tags = new List<string>();
        [SerializeField] public int PreloadCount = 0;
        [SerializeField] public bool IsEnabled = true;
        [SerializeField] public int Priority = 0;
        [SerializeField] public Texture2D Icon;
        
        // Compatibility property for legacy code
        public string PrefabName => DisplayName ?? PrefabId ?? Prefab?.name ?? "Unknown";
    }
    
    /// <summary>
    /// UI prefab categories
    /// </summary>
    public enum UIPrefabCategory
    {
        Component,
        Widget,
        Modal,
        Notification
    }
    
    /// <summary>
    /// Library statistics
    /// </summary>
    public struct UIPrefabLibraryStats
    {
        public int TotalPrefabs;
        public int ComponentPrefabs;
        public int WidgetPrefabs;
        public int ModalPrefabs;
        public int NotificationPrefabs;
        public bool CacheEnabled;
        public bool CacheInitialized;
    }
}