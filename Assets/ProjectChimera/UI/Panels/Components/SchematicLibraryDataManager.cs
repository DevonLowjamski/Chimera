using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using ProjectChimera.Data.Construction;
using ProjectChimera.Systems.Construction;

namespace ProjectChimera.UI.Panels.Components
{
    /// <summary>
    /// Manages schematic data, sorting, pagination, and data operations for the library.
    /// Handles loading, organizing, and providing access to schematic collections.
    /// </summary>
    public class SchematicLibraryDataManager : MonoBehaviour
    {
        [Header("Data Configuration")]
        [SerializeField] private List<SchematicSO> _librarySchemas = new List<SchematicSO>();
        [SerializeField] private bool _autoLoadOnStart = true;
        [SerializeField] private int _itemsPerPage = 20;
        [SerializeField] private LibrarySortMode _defaultSortMode = LibrarySortMode.Name;
        [SerializeField] private bool _defaultSortAscending = true;
        
        // Data state
        private List<SchematicSO> _filteredSchematics = new List<SchematicSO>();
        private List<SchematicSO> _displayedSchematics = new List<SchematicSO>();
        private SchematicSO _selectedSchematic;
        
        // Pagination state
        private int _currentPage = 0;
        private int _totalPages = 0;
        
        // Sorting state
        private LibrarySortMode _currentSortMode;
        private bool _currentSortAscending;
        
        // Events
        public System.Action<List<SchematicSO>> OnSchematicsLoaded;
        public System.Action<List<SchematicSO>> OnFilteredSchematicsChanged;
        public System.Action<List<SchematicSO>> OnDisplayedSchematicsChanged;
        public System.Action<SchematicSO> OnSelectedSchematicChanged;
        public System.Action<int, int, int> OnPaginationChanged; // currentPage, totalPages, totalItems
        public System.Action<LibrarySortMode, bool> OnSortingChanged;
        
        // Properties
        public List<SchematicSO> LibrarySchematics => new List<SchematicSO>(_librarySchemas);
        public List<SchematicSO> FilteredSchematics => new List<SchematicSO>(_filteredSchematics);
        public List<SchematicSO> DisplayedSchematics => new List<SchematicSO>(_displayedSchematics);
        public SchematicSO SelectedSchematic => _selectedSchematic;
        public int CurrentPage => _currentPage;
        public int TotalPages => _totalPages;
        public int ItemsPerPage => _itemsPerPage;
        public int TotalSchematicCount => _librarySchemas.Count;
        public int FilteredSchematicCount => _filteredSchematics.Count;
        public LibrarySortMode CurrentSortMode => _currentSortMode;
        public bool CurrentSortAscending => _currentSortAscending;
        
        private void Start()
        {
            Initialize();
        }
        
        /// <summary>
        /// Initialize data manager
        /// </summary>
        public void Initialize()
        {
            _currentSortMode = _defaultSortMode;
            _currentSortAscending = _defaultSortAscending;
            
            if (_autoLoadOnStart)
            {
                LoadAllSchematics();
            }
        }
        
        #region Data Loading
        
        /// <summary>
        /// Load all available schematics
        /// </summary>
        public void LoadAllSchematics()
        {
            // Load schematics from Resources or AssetDatabase
            var loadedSchematics = Resources.LoadAll<SchematicSO>("Schematics");
            
            _librarySchemas.Clear();
            _librarySchemas.AddRange(loadedSchematics);
            
            // Sort initially
            SortSchematics(_librarySchemas, _currentSortMode, _currentSortAscending);
            
            OnSchematicsLoaded?.Invoke(_librarySchemas);
            
            // Update filtered view
            SetFilteredSchematics(_librarySchemas);
            
            LogDebug($"Loaded {_librarySchemas.Count} schematics");
        }
        
        /// <summary>
        /// Refresh schematic data
        /// </summary>
        public void RefreshData()
        {
            LoadAllSchematics();
        }
        
        #endregion
        
        #region Schematic Management
        
        /// <summary>
        /// Add schematic to library
        /// </summary>
        public void AddSchematic(SchematicSO schematic)
        {
            if (schematic != null && !_librarySchemas.Contains(schematic))
            {
                _librarySchemas.Add(schematic);
                SortSchematics(_librarySchemas, _currentSortMode, _currentSortAscending);
                
                // Update displays
                RefreshFilteredView();
                
                LogDebug($"Added schematic: {schematic.SchematicName}");
            }
        }
        
        /// <summary>
        /// Remove schematic from library
        /// </summary>
        public bool RemoveSchematic(SchematicSO schematic)
        {
            if (_librarySchemas.Remove(schematic))
            {
                if (_selectedSchematic == schematic)
                {
                    SetSelectedSchematic(null);
                }
                
                RefreshFilteredView();
                LogDebug($"Removed schematic: {schematic.SchematicName}");
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Set selected schematic
        /// </summary>
        public void SetSelectedSchematic(SchematicSO schematic)
        {
            if (_selectedSchematic != schematic)
            {
                _selectedSchematic = schematic;
                OnSelectedSchematicChanged?.Invoke(_selectedSchematic);
                LogDebug($"Selected schematic: {schematic?.SchematicName ?? "None"}");
            }
        }
        
        /// <summary>
        /// Get schematic by name
        /// </summary>
        public SchematicSO GetSchematicByName(string name)
        {
            return _librarySchemas.FirstOrDefault(s => 
                string.Equals(s.SchematicName, name, StringComparison.OrdinalIgnoreCase));
        }
        
        /// <summary>
        /// Check if schematic exists in library
        /// </summary>
        public bool ContainsSchematic(SchematicSO schematic)
        {
            return _librarySchemas.Contains(schematic);
        }
        
        #endregion
        
        #region Filtering and Sorting
        
        /// <summary>
        /// Set filtered schematics (called by search controller)
        /// </summary>
        public void SetFilteredSchematics(List<SchematicSO> filtered)
        {
            _filteredSchematics = new List<SchematicSO>(filtered);
            
            // Apply current sorting to filtered results
            SortSchematics(_filteredSchematics, _currentSortMode, _currentSortAscending);
            
            OnFilteredSchematicsChanged?.Invoke(_filteredSchematics);
            
            // Reset to first page when filters change
            _currentPage = 0;
            UpdatePagination();
            UpdateDisplayedSchematics();
        }
        
        /// <summary>
        /// Refresh filtered view with current filters
        /// </summary>
        private void RefreshFilteredView()
        {
            // This would typically be called by the search controller
            // For now, assume no filters and show all
            SetFilteredSchematics(_librarySchemas);
        }
        
        /// <summary>
        /// Sort schematics by specified criteria
        /// </summary>
        public void SetSorting(LibrarySortMode sortMode, bool ascending)
        {
            if (_currentSortMode != sortMode || _currentSortAscending != ascending)
            {
                _currentSortMode = sortMode;
                _currentSortAscending = ascending;
                
                // Apply sorting to all collections
                SortSchematics(_librarySchemas, sortMode, ascending);
                SortSchematics(_filteredSchematics, sortMode, ascending);
                
                OnSortingChanged?.Invoke(sortMode, ascending);
                
                // Update displayed schematics
                UpdateDisplayedSchematics();
                
                LogDebug($"Sorted by {sortMode}, ascending: {ascending}");
            }
        }
        
        /// <summary>
        /// Sort a list of schematics
        /// </summary>
        private void SortSchematics(List<SchematicSO> schematics, LibrarySortMode sortMode, bool ascending)
        {
            if (schematics == null || schematics.Count <= 1) return;
            
            switch (sortMode)
            {
                case LibrarySortMode.Name:
                    schematics.Sort((a, b) => CompareByName(a, b, ascending));
                    break;
                    
                case LibrarySortMode.CreationDate:
                    schematics.Sort((a, b) => CompareByDate(a, b, ascending));
                    break;
                    
                case LibrarySortMode.Complexity:
                    schematics.Sort((a, b) => CompareByComplexity(a, b, ascending));
                    break;
                    
                case LibrarySortMode.ItemCount:
                    schematics.Sort((a, b) => CompareByItemCount(a, b, ascending));
                    break;
                    
                case LibrarySortMode.Cost:
                    schematics.Sort((a, b) => CompareByCost(a, b, ascending));
                    break;
            }
        }
        
        /// <summary>
        /// Compare schematics by name
        /// </summary>
        private int CompareByName(SchematicSO a, SchematicSO b, bool ascending)
        {
            int result = string.Compare(a.SchematicName, b.SchematicName, StringComparison.OrdinalIgnoreCase);
            return ascending ? result : -result;
        }
        
        /// <summary>
        /// Compare schematics by creation date
        /// </summary>
        private int CompareByDate(SchematicSO a, SchematicSO b, bool ascending)
        {
            int result = a.CreationDate.CompareTo(b.CreationDate);
            return ascending ? result : -result;
        }
        
        /// <summary>
        /// Compare schematics by complexity
        /// </summary>
        private int CompareByComplexity(SchematicSO a, SchematicSO b, bool ascending)
        {
            int result = a.Complexity.CompareTo(b.Complexity);
            return ascending ? result : -result;
        }
        
        /// <summary>
        /// Compare schematics by item count
        /// </summary>
        private int CompareByItemCount(SchematicSO a, SchematicSO b, bool ascending)
        {
            int result = a.ItemCount.CompareTo(b.ItemCount);
            return ascending ? result : -result;
        }
        
        /// <summary>
        /// Compare schematics by cost
        /// </summary>
        private int CompareByCost(SchematicSO a, SchematicSO b, bool ascending)
        {
            float costA = CalculateSchematicCost(a);
            float costB = CalculateSchematicCost(b);
            int result = costA.CompareTo(costB);
            return ascending ? result : -result;
        }
        
        /// <summary>
        /// Calculate schematic cost for sorting
        /// </summary>
        private float CalculateSchematicCost(SchematicSO schematic)
        {
            // This would integrate with the cost calculation system
            // For now, return a simple approximation
            return schematic.ItemCount * 10f; // Placeholder calculation
        }
        
        #endregion
        
        #region Pagination
        
        /// <summary>
        /// Set items per page
        /// </summary>
        public void SetItemsPerPage(int itemsPerPage)
        {
            if (itemsPerPage > 0 && _itemsPerPage != itemsPerPage)
            {
                _itemsPerPage = itemsPerPage;
                _currentPage = 0; // Reset to first page
                UpdatePagination();
                UpdateDisplayedSchematics();
                
                LogDebug($"Items per page set to: {itemsPerPage}");
            }
        }
        
        /// <summary>
        /// Go to specific page
        /// </summary>
        public void GoToPage(int pageIndex)
        {
            pageIndex = Mathf.Clamp(pageIndex, 0, _totalPages - 1);
            
            if (_currentPage != pageIndex)
            {
                _currentPage = pageIndex;
                UpdateDisplayedSchematics();
                OnPaginationChanged?.Invoke(_currentPage, _totalPages, _filteredSchematics.Count);
                
                LogDebug($"Navigated to page: {pageIndex + 1}");
            }
        }
        
        /// <summary>
        /// Navigate pages relative to current
        /// </summary>
        public void NavigatePage(int delta)
        {
            int newPage = _currentPage + delta;
            GoToPage(newPage);
        }
        
        /// <summary>
        /// Update pagination calculations
        /// </summary>
        private void UpdatePagination()
        {
            if (_itemsPerPage > 0)
            {
                _totalPages = Mathf.CeilToInt((float)_filteredSchematics.Count / _itemsPerPage);
                _totalPages = Mathf.Max(_totalPages, 1);
                
                // Ensure current page is valid
                _currentPage = Mathf.Clamp(_currentPage, 0, _totalPages - 1);
            }
            else
            {
                _totalPages = 1;
                _currentPage = 0;
            }
            
            OnPaginationChanged?.Invoke(_currentPage, _totalPages, _filteredSchematics.Count);
        }
        
        /// <summary>
        /// Update displayed schematics based on current page
        /// </summary>
        private void UpdateDisplayedSchematics()
        {
            _displayedSchematics.Clear();
            
            if (_filteredSchematics.Count > 0 && _itemsPerPage > 0)
            {
                int startIndex = _currentPage * _itemsPerPage;
                int endIndex = Mathf.Min(startIndex + _itemsPerPage, _filteredSchematics.Count);
                
                for (int i = startIndex; i < endIndex; i++)
                {
                    _displayedSchematics.Add(_filteredSchematics[i]);
                }
            }
            
            OnDisplayedSchematicsChanged?.Invoke(_displayedSchematics);
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Get all available tags from current schematics
        /// </summary>
        public List<string> GetAllAvailableTags()
        {
            var allTags = new HashSet<string>();
            
            foreach (var schematic in _librarySchemas)
            {
                if (schematic.Tags != null)
                {
                    foreach (var tag in schematic.Tags)
                    {
                        if (!string.IsNullOrEmpty(tag))
                        {
                            allTags.Add(tag);
                        }
                    }
                }
            }
            
            return allTags.OrderBy(tag => tag).ToList();
        }
        
        /// <summary>
        /// Get schematics by category
        /// </summary>
        public List<SchematicSO> GetSchematicsByCategory(ConstructionCategory category)
        {
            return _librarySchemas.Where(s => s.Category == category).ToList();
        }
        
        /// <summary>
        /// Get schematics by complexity
        /// </summary>
        public List<SchematicSO> GetSchematicsByComplexity(SchematicComplexity complexity)
        {
            return _librarySchemas.Where(s => s.Complexity == complexity).ToList();
        }
        
        /// <summary>
        /// Get schematics by tag
        /// </summary>
        public List<SchematicSO> GetSchematicsByTag(string tag)
        {
            return _librarySchemas.Where(s => 
                s.Tags != null && s.Tags.Any(t => 
                    string.Equals(t, tag, StringComparison.OrdinalIgnoreCase))).ToList();
        }
        
        /// <summary>
        /// Get library statistics
        /// </summary>
        public SchematicLibraryStats GetLibraryStats()
        {
            var stats = new SchematicLibraryStats
            {
                TotalSchematics = _librarySchemas.Count,
                FilteredSchematics = _filteredSchematics.Count,
                DisplayedSchematics = _displayedSchematics.Count,
                TotalPages = _totalPages,
                CurrentPage = _currentPage + 1
            };
            
            // Count by category
            foreach (ConstructionCategory category in System.Enum.GetValues(typeof(ConstructionCategory)))
            {
                if (category != ConstructionCategory.All)
                {
                    int count = _librarySchemas.Count(s => s.Category == category);
                    stats.SchematicsByCategory[category] = count;
                }
            }
            
            // Count by complexity
            foreach (SchematicComplexity complexity in System.Enum.GetValues(typeof(SchematicComplexity)))
            {
                if (complexity != SchematicComplexity.All)
                {
                    int count = _librarySchemas.Count(s => s.Complexity == complexity);
                    stats.SchematicsByComplexity[complexity] = count;
                }
            }
            
            return stats;
        }
        
        #endregion
        
        private void LogDebug(string message)
        {
            Debug.Log($"[SchematicLibraryDataManager] {message}");
        }
    }
    
    /// <summary>
    /// Library statistics data
    /// </summary>
    [System.Serializable]
    public class SchematicLibraryStats
    {
        public int TotalSchematics;
        public int FilteredSchematics;
        public int DisplayedSchematics;
        public int TotalPages;
        public int CurrentPage;
        public Dictionary<ConstructionCategory, int> SchematicsByCategory = new Dictionary<ConstructionCategory, int>();
        public Dictionary<SchematicComplexity, int> SchematicsByComplexity = new Dictionary<SchematicComplexity, int>();
    }
}