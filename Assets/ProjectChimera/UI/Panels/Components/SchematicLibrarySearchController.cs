using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using System;
using ProjectChimera.Data.Construction;

namespace ProjectChimera.UI.Panels.Components
{
    /// <summary>
    /// Handles search and filtering logic for the schematic library.
    /// Manages search queries, category filters, complexity filters, and tag filters.
    /// </summary>
    public class SchematicLibrarySearchController : MonoBehaviour
    {
        [Header("Search Configuration")]
        [SerializeField] private bool _enableAdvancedSearch = true;
        [SerializeField] private bool _enableTagFiltering = true;
        [SerializeField] private bool _enableCategoryFiltering = true;
        [SerializeField] private bool _enableComplexityFiltering = true;
        [SerializeField] private float _searchDebounceTime = 0.3f;
        
        // Search state
        private string _currentSearchQuery = "";
        private ConstructionCategory _currentCategoryFilter = ConstructionCategory.All;
        private SchematicComplexity _currentComplexityFilter = SchematicComplexity.All;
        private List<string> _currentTagFilters = new List<string>();
        private bool _searchByName = true;
        private bool _searchByDescription = true;
        private bool _searchByTags = true;
        
        // Debouncing
        private float _searchDebounceTimer = 0f;
        private string _pendingSearchQuery = "";
        private bool _hasSearchUpdate = false;
        
        // Events
        public System.Action<string> OnSearchQueryChanged;
        public System.Action<ConstructionCategory> OnCategoryFilterChanged;
        public System.Action<SchematicComplexity> OnComplexityFilterChanged;
        public System.Action<List<string>> OnTagFiltersChanged;
        public System.Action OnFiltersChanged;
        
        // Properties
        public string CurrentSearchQuery => _currentSearchQuery;
        public ConstructionCategory CurrentCategoryFilter => _currentCategoryFilter;
        public SchematicComplexity CurrentComplexityFilter => _currentComplexityFilter;
        public List<string> CurrentTagFilters => new List<string>(_currentTagFilters);
        public bool EnableAdvancedSearch => _enableAdvancedSearch;
        
        private void Update()
        {
            // Handle search debouncing
            if (_hasSearchUpdate)
            {
                _searchDebounceTimer += Time.deltaTime;
                if (_searchDebounceTimer >= _searchDebounceTime)
                {
                    ApplyPendingSearch();
                    _hasSearchUpdate = false;
                    _searchDebounceTimer = 0f;
                }
            }
        }
        
        #region Search Operations
        
        /// <summary>
        /// Set search query with debouncing
        /// </summary>
        public void SetSearchQuery(string query)
        {
            if (query == null) query = "";
            
            _pendingSearchQuery = query;
            _hasSearchUpdate = true;
            _searchDebounceTimer = 0f;
        }
        
        /// <summary>
        /// Apply the pending search query
        /// </summary>
        private void ApplyPendingSearch()
        {
            if (_currentSearchQuery != _pendingSearchQuery)
            {
                _currentSearchQuery = _pendingSearchQuery;
                OnSearchQueryChanged?.Invoke(_currentSearchQuery);
                OnFiltersChanged?.Invoke();
            }
        }
        
        /// <summary>
        /// Set search options for advanced search
        /// </summary>
        public void SetSearchOptions(bool searchByName, bool searchByDescription, bool searchByTags)
        {
            bool changed = (_searchByName != searchByName) || 
                          (_searchByDescription != searchByDescription) || 
                          (_searchByTags != searchByTags);
            
            _searchByName = searchByName;
            _searchByDescription = searchByDescription;
            _searchByTags = searchByTags;
            
            if (changed && !string.IsNullOrEmpty(_currentSearchQuery))
            {
                OnFiltersChanged?.Invoke();
            }
        }
        
        /// <summary>
        /// Clear search query
        /// </summary>
        public void ClearSearch()
        {
            SetSearchQuery("");
        }
        
        #endregion
        
        #region Filter Operations
        
        /// <summary>
        /// Set category filter
        /// </summary>
        public void SetCategoryFilter(ConstructionCategory category)
        {
            if (_currentCategoryFilter != category)
            {
                _currentCategoryFilter = category;
                OnCategoryFilterChanged?.Invoke(category);
                OnFiltersChanged?.Invoke();
            }
        }
        
        /// <summary>
        /// Set complexity filter
        /// </summary>
        public void SetComplexityFilter(SchematicComplexity complexity)
        {
            if (_currentComplexityFilter != complexity)
            {
                _currentComplexityFilter = complexity;
                OnComplexityFilterChanged?.Invoke(complexity);
                OnFiltersChanged?.Invoke();
            }
        }
        
        /// <summary>
        /// Set tag filters
        /// </summary>
        public void SetTagFilters(List<string> tags)
        {
            if (tags == null) tags = new List<string>();
            
            bool changed = !_currentTagFilters.SequenceEqual(tags);
            if (changed)
            {
                _currentTagFilters = new List<string>(tags);
                OnTagFiltersChanged?.Invoke(_currentTagFilters);
                OnFiltersChanged?.Invoke();
            }
        }
        
        /// <summary>
        /// Add tag filter
        /// </summary>
        public void AddTagFilter(string tag)
        {
            if (!string.IsNullOrEmpty(tag) && !_currentTagFilters.Contains(tag))
            {
                _currentTagFilters.Add(tag);
                OnTagFiltersChanged?.Invoke(_currentTagFilters);
                OnFiltersChanged?.Invoke();
            }
        }
        
        /// <summary>
        /// Remove tag filter
        /// </summary>
        public void RemoveTagFilter(string tag)
        {
            if (_currentTagFilters.Remove(tag))
            {
                OnTagFiltersChanged?.Invoke(_currentTagFilters);
                OnFiltersChanged?.Invoke();
            }
        }
        
        /// <summary>
        /// Clear all filters
        /// </summary>
        public void ClearAllFilters()
        {
            bool hasChanges = !string.IsNullOrEmpty(_currentSearchQuery) ||
                             _currentCategoryFilter != ConstructionCategory.All ||
                             _currentComplexityFilter != SchematicComplexity.All ||
                             _currentTagFilters.Count > 0;
            
            _currentSearchQuery = "";
            _pendingSearchQuery = "";
            _currentCategoryFilter = ConstructionCategory.All;
            _currentComplexityFilter = SchematicComplexity.All;
            _currentTagFilters.Clear();
            _hasSearchUpdate = false;
            
            if (hasChanges)
            {
                OnSearchQueryChanged?.Invoke(_currentSearchQuery);
                OnCategoryFilterChanged?.Invoke(_currentCategoryFilter);
                OnComplexityFilterChanged?.Invoke(_currentComplexityFilter);
                OnTagFiltersChanged?.Invoke(_currentTagFilters);
                OnFiltersChanged?.Invoke();
            }
        }
        
        #endregion
        
        #region Filtering Logic
        
        /// <summary>
        /// Filter schematics based on current search and filter criteria
        /// </summary>
        public List<SchematicSO> FilterSchematics(List<SchematicSO> schematics)
        {
            if (schematics == null || schematics.Count == 0)
                return new List<SchematicSO>();
            
            var filtered = schematics.AsEnumerable();
            
            // Apply search filter
            if (!string.IsNullOrEmpty(_currentSearchQuery))
            {
                filtered = ApplySearchFilter(filtered, _currentSearchQuery);
            }
            
            // Apply category filter
            if (_enableCategoryFiltering && _currentCategoryFilter != ConstructionCategory.All)
            {
                filtered = filtered.Where(s => s.Category == _currentCategoryFilter);
            }
            
            // Apply complexity filter
            if (_enableComplexityFiltering && _currentComplexityFilter != SchematicComplexity.All)
            {
                filtered = filtered.Where(s => s.Complexity == _currentComplexityFilter);
            }
            
            // Apply tag filters
            if (_enableTagFiltering && _currentTagFilters.Count > 0)
            {
                filtered = ApplyTagFilter(filtered, _currentTagFilters);
            }
            
            return filtered.ToList();
        }
        
        /// <summary>
        /// Apply search query filter
        /// </summary>
        private IEnumerable<SchematicSO> ApplySearchFilter(IEnumerable<SchematicSO> schematics, string query)
        {
            query = query.ToLowerInvariant();
            
            return schematics.Where(schematic =>
            {
                bool matches = false;
                
                if (_searchByName && !string.IsNullOrEmpty(schematic.SchematicName))
                {
                    matches |= schematic.SchematicName.ToLowerInvariant().Contains(query);
                }
                
                if (_searchByDescription && !string.IsNullOrEmpty(schematic.Description))
                {
                    matches |= schematic.Description.ToLowerInvariant().Contains(query);
                }
                
                if (_searchByTags && schematic.Tags != null && schematic.Tags.Count > 0)
                {
                    matches |= schematic.Tags.Any(tag => 
                        !string.IsNullOrEmpty(tag) && tag.ToLowerInvariant().Contains(query));
                }
                
                return matches;
            });
        }
        
        /// <summary>
        /// Apply tag filters
        /// </summary>
        private IEnumerable<SchematicSO> ApplyTagFilter(IEnumerable<SchematicSO> schematics, List<string> tagFilters)
        {
            return schematics.Where(schematic =>
            {
                if (schematic.Tags == null || schematic.Tags.Count == 0)
                    return false;
                
                // Check if schematic has any of the required tags
                return tagFilters.Any(filterTag => 
                    schematic.Tags.Any(schematicTag => 
                        string.Equals(schematicTag, filterTag, StringComparison.OrdinalIgnoreCase)));
            });
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Get all available tags from a list of schematics
        /// </summary>
        public List<string> GetAllAvailableTags(List<SchematicSO> schematics)
        {
            if (schematics == null || schematics.Count == 0)
                return new List<string>();
            
            var allTags = new HashSet<string>();
            
            foreach (var schematic in schematics)
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
        /// Get filter summary text
        /// </summary>
        public string GetFilterSummary()
        {
            var parts = new List<string>();
            
            if (!string.IsNullOrEmpty(_currentSearchQuery))
                parts.Add($"Search: \"{_currentSearchQuery}\"");
            
            if (_currentCategoryFilter != ConstructionCategory.All)
                parts.Add($"Category: {_currentCategoryFilter}");
            
            if (_currentComplexityFilter != SchematicComplexity.All)
                parts.Add($"Complexity: {_currentComplexityFilter}");
            
            if (_currentTagFilters.Count > 0)
                parts.Add($"Tags: {string.Join(", ", _currentTagFilters)}");
            
            return parts.Count > 0 ? string.Join(" | ", parts) : "No filters applied";
        }
        
        /// <summary>
        /// Check if any filters are active
        /// </summary>
        public bool HasActiveFilters()
        {
            return !string.IsNullOrEmpty(_currentSearchQuery) ||
                   _currentCategoryFilter != ConstructionCategory.All ||
                   _currentComplexityFilter != SchematicComplexity.All ||
                   _currentTagFilters.Count > 0;
        }
        
        #endregion
    }
}