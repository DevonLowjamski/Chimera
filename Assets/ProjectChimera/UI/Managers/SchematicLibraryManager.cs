using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Threading.Tasks;
using ProjectChimera.UI.Panels;
using ProjectChimera.UI.Core;
using ProjectChimera.Data.Construction;
using ProjectChimera.Systems.Construction;
using ProjectChimera.UI.Panels.Components;
using ProjectChimera.Core;
using ProjectChimera.Shared;
using ProjectChimera.Core.Logging;
using ProjectChimera.Systems.Addressables;

namespace ProjectChimera.UI.Managers
{
    /// <summary>
    /// Manager for the Schematic Library system in Project Chimera Phase 4.
    /// Handles library lifecycle, search functionality, and integration with construction systems.
    /// Provides advanced schematic management features including import/export and organization.
    ///
    /// DEPENDENCY INJECTION: Uses constructor injection for testability and explicit dependencies.
    /// </summary>
    public class SchematicLibraryManager : MonoBehaviour
    {
        [Header("Library Configuration")]
        [SerializeField] private bool _autoLoadOnStart = true;
        [SerializeField] private bool _enablePerformanceOptimizations = true;

        [Header("Search Configuration")]
        [SerializeField] private int _maxSearchResults = 100;
        [SerializeField] private float _searchScoreThreshold = 0.3f;
        [SerializeField] private bool _enableFuzzySearch = true;

        [Header("Persistence Settings")]
        [SerializeField] private bool _autoSavePreferences = true;
        [SerializeField] private string _preferencesKey = "SchematicLibraryPrefs";

        [Header("Performance Settings")]
        [SerializeField] private int _maxConcurrentLoads = 5;
        [SerializeField] private float _loadBatchDelay = 0.1f;
        [SerializeField] private bool _enableLazyLoading = true;

        // Dependencies resolved via DI container (explicit dependencies for testability)
        private ISchematicLibraryPanel _libraryPanel;
        private IGridPlacementController _placementController;
        private IConstructionPaletteManager _paletteManager;
        private IUIManager _uiManager;

        // State
        private bool _isInitialized = false;
        private bool _isLibraryVisible = false;
        private List<SchematicSO> _allSchematics = new List<SchematicSO>();
        private Dictionary<string, SchematicSearchIndex> _searchIndex = new Dictionary<string, SchematicSearchIndex>();
        private LibraryPreferences _preferences = new LibraryPreferences();

        // Performance tracking
        private int _loadedSchematicsCount = 0;
        private float _lastSearchTime = 0f;
        private string _lastSearchQuery = "";

        // Events
        public System.Action<bool> OnLibraryVisibilityChanged;
        public System.Action<int> OnSchematicsLoaded;
        public System.Action<string, int> OnSearchCompleted;
        public System.Action<SchematicSO> OnSchematicSelectedInLibrary;

        /// <summary>
        /// Initialize dependencies explicitly for testability.
        /// For testing: call this method with mock dependencies.
        /// For runtime: dependencies are resolved automatically via DI container.
        /// </summary>
        public void Initialize(ISchematicLibraryPanel libraryPanel = null,
                              IGridPlacementController placementController = null,
                              IConstructionPaletteManager paletteManager = null,
                              IUIManager uiManager = null)
        {
            _libraryPanel = libraryPanel;
            _placementController = placementController;
            _paletteManager = paletteManager;
            _uiManager = uiManager;
        }

        private void Awake()
        {
            ResolveDependencies();
        }

        private void Start()
        {
            InitializeLibraryManager();
        }

        /// <summary>
        /// Resolve dependencies from DI container if not explicitly provided.
        /// This method supports both explicit dependency injection (for testing)
        /// and automatic resolution (for runtime).
        /// </summary>
        private void ResolveDependencies()
        {
            if (_libraryPanel == null)
            {
                _libraryPanel = ServiceContainerFactory.Instance?.TryResolve<ISchematicLibraryPanel>();
                if (_libraryPanel == null)
                {
                    ProjectChimera.Core.Logging.ChimeraLogger.LogError("[SchematicLibraryManager] ISchematicLibraryPanel not registered in DI container. Explicit dependency injection required for testing.");
                }
            }

            if (_placementController == null)
            {
                _placementController = ServiceContainerFactory.Instance?.TryResolve<IGridPlacementController>();
                if (_placementController == null)
                {
                    ProjectChimera.Core.Logging.ChimeraLogger.LogError("[SchematicLibraryManager] IGridPlacementController not registered in DI container. Construction placement will not function properly.");
                }
            }

            if (_paletteManager == null)
            {
                _paletteManager = ServiceContainerFactory.Instance?.TryResolve<IConstructionPaletteManager>();
                if (_paletteManager == null)
                {
                    ProjectChimera.Core.Logging.ChimeraLogger.LogWarning("[SchematicLibraryManager] IConstructionPaletteManager not registered in DI container. Palette integration may not work.");
                }
            }

            if (_uiManager == null)
            {
                _uiManager = ServiceContainerFactory.Instance?.TryResolve<IUIManager>();
                if (_uiManager == null)
                {
                    ProjectChimera.Core.Logging.ChimeraLogger.LogWarning("[SchematicLibraryManager] IUIManager not registered in DI container. Some UI integration features may not work.");
                }
            }
        }

        /// <summary>
        /// Initialize the library manager
        /// </summary>
        private void InitializeLibraryManager()
        {
            if (_isInitialized) return;

            LoadPreferences();
            SetupEventHandlers();

            if (_autoLoadOnStart)
            {
                LoadAllSchematics();
            }

            _isInitialized = true;
        }

        /// <summary>
        /// Set up event handlers for library interactions
        /// </summary>
        private void SetupEventHandlers()
        {
            if (_libraryPanel != null)
            {
                _libraryPanel.SchematicSelected += OnSchematicSelectedFromLibrary;
                _libraryPanel.SchematicApplied += OnSchematicAppliedFromLibrary;
                _libraryPanel.SchematicDeleted += OnSchematicDeletedFromLibrary;
                _libraryPanel.SearchQueryChanged += OnSearchQueryChangedFromLibrary;
                _libraryPanel.ViewModeChanged += OnViewModeChangedFromLibrary;
            }
        }

        /// <summary>
        /// Load all available schematics and build search index
        /// </summary>
        public void LoadAllSchematics()
        {
            _allSchematics.Clear();
            _searchIndex.Clear();
            _loadedSchematicsCount = 0;

            if (_enableLazyLoading)
            {
                StartCoroutine(LoadSchematicsAsync());
            }
            else
            {
                LoadSchematicsImmediate();
            }
        }

        /// <summary>
        /// Load schematics immediately
        /// </summary>
        private void LoadSchematicsImmediate()
        {
            // Load from ScriptableObject instances already registered in DI container
            // This replaces the Resources.LoadAll anti-pattern with proper dependency injection
            var schematicService = ServiceContainerFactory.Instance?.TryResolve<ISchematicAssetService>();
            if (schematicService != null)
            {
                var schematics = schematicService.GetAllSchematics();
                // Convert ChimeraScriptableObject array to SchematicSO
                var schematicSOArray = schematics.OfType<SchematicSO>().ToArray();
                _allSchematics.AddRange(schematicSOArray);
            }
            else
            {
                ProjectChimera.Core.Logging.ChimeraLogger.LogWarning("[SchematicLibraryManager] ISchematicAssetService not registered. Using Addressables fallback.");
                // Use Addressables fallback instead of Resources.LoadAll
                _ = LoadSchematicsFromAddressablesAsync();
            }

            // Build search index
            BuildSearchIndex();

            // Update library panel
            if (_libraryPanel != null)
            {
                foreach (var schematic in _allSchematics)
                {
                    _libraryPanel.AddSchematic(schematic);
                }
            }

            _loadedSchematicsCount = _allSchematics.Count;
            OnSchematicsLoaded?.Invoke(_loadedSchematicsCount);

            ProjectChimera.Core.Logging.ChimeraLogger.Log($"[SchematicLibraryManager] Loaded {_loadedSchematicsCount} schematics immediately");
        }

        /// <summary>
        /// Load schematics from Addressables system
        /// </summary>
        private async Task LoadSchematicsFromAddressablesAsync()
        {
            // Get AddressablesInfrastructure instance
            var addressablesService = ServiceContainerFactory.Instance?.TryResolve<AddressablesInfrastructure>();
            if (addressablesService == null)
            {
                // Addressables service fallback - create if needed
                var addressablesGO = new GameObject("AddressablesInfrastructure");
                addressablesService = addressablesGO.AddComponent<AddressablesInfrastructure>();
                if (addressablesService == null)
                {
                    ChimeraLogger.LogWarning("[SchematicLibraryManager] AddressablesInfrastructure not found. Creating instance.");
                    addressablesService = addressablesGO.AddComponent<AddressablesInfrastructure>();
                }
            }

            try
            {
                // Predefined schematic addresses to load
                var schematicAddresses = new[] {
                    "Schematics/Library/BasicSchematics",
                    "Schematics/Library/AdvancedSchematics",
                    "Schematics/Library/ProSchematics",
                    "Schematics/Construction/WallSchematics",
                    "Schematics/Construction/FloorSchematics",
                    "Schematics/Equipment/LightingSchematics",
                    "Schematics/Equipment/HVACSchematics"
                };

                foreach (var address in schematicAddresses)
                {
                    try
                    {
                        var schematic = await addressablesService.LoadAssetAsync<SchematicSO>(address);
                        if (schematic != null)
                        {
                            _allSchematics.Add(schematic);

                            // Update library panel if available
                            if (_libraryPanel != null)
                            {
                                _libraryPanel.AddSchematic(schematic);
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        ProjectChimera.Core.Logging.ChimeraLogger.LogWarning($"[SchematicLibraryManager] Could not load schematic '{address}': {ex.Message}");
                    }
                }

                // Update search index
                BuildSearchIndex();

                _loadedSchematicsCount = _allSchematics.Count;
                OnSchematicsLoaded?.Invoke(_loadedSchematicsCount);

                ProjectChimera.Core.Logging.ChimeraLogger.Log($"[SchematicLibraryManager] Loaded {_loadedSchematicsCount} schematics via Addressables");
            }
            catch (System.Exception ex)
            {
                ProjectChimera.Core.Logging.ChimeraLogger.LogError($"[SchematicLibraryManager] Failed to load schematics from Addressables: {ex.Message}");
            }
        }

        /// <summary>
        /// Load schematics asynchronously in batches
        /// </summary>
        private System.Collections.IEnumerator LoadSchematicsAsync()
        {
            SchematicSO[] schematics;

            // Load from ScriptableObject instances already registered in DI container
            var schematicService = ServiceContainerFactory.Instance?.TryResolve<ISchematicAssetService>();
            if (schematicService != null)
            {
                var chimeraObjects = schematicService.GetAllSchematics();
                // Convert ChimeraScriptableObject array to SchematicSO array
                schematics = chimeraObjects.OfType<SchematicSO>().ToArray();
            }
            else
            {
                ProjectChimera.Core.Logging.ChimeraLogger.LogWarning("[SchematicLibraryManager] ISchematicAssetService not registered. Using Addressables fallback.");
                // For async loading, we need to get schematics from Addressables
                // This requires a different approach - for now we'll return empty and load async
                schematics = new SchematicSO[0];
                _ = LoadSchematicsFromAddressablesAsync();
            }

            var batchSize = Mathf.Max(1, schematics.Length / _maxConcurrentLoads);

            for (int i = 0; i < schematics.Length; i += batchSize)
            {
                var endIndex = Mathf.Min(i + batchSize, schematics.Length);

                for (int j = i; j < endIndex; j++)
                {
                    var schematic = schematics[j];
                    _allSchematics.Add(schematic);

                    if (_libraryPanel != null)
                    {
                        _libraryPanel.AddSchematic(schematic);
                    }

                    _loadedSchematicsCount++;
                }

                // Build partial search index for this batch
                BuildSearchIndexForRange(i, endIndex);

                yield return new WaitForSeconds(_loadBatchDelay);
            }

            OnSchematicsLoaded?.Invoke(_loadedSchematicsCount);
            ProjectChimera.Core.Logging.ChimeraLogger.Log($"[SchematicLibraryManager] Loaded {_loadedSchematicsCount} schematics asynchronously");
        }

        /// <summary>
        /// Build search index for all schematics
        /// </summary>
        private void BuildSearchIndex()
        {
            foreach (var schematic in _allSchematics)
            {
                IndexSchematic(schematic);
            }
        }

        /// <summary>
        /// Build search index for specific range of schematics
        /// </summary>
        private void BuildSearchIndexForRange(int startIndex, int endIndex)
        {
            for (int i = startIndex; i < endIndex && i < _allSchematics.Count; i++)
            {
                IndexSchematic(_allSchematics[i]);
            }
        }

        /// <summary>
        /// Index a single schematic for search
        /// </summary>
        private void IndexSchematic(SchematicSO schematic)
        {
            if (schematic == null) return;

            var index = new SchematicSearchIndex
            {
                Schematic = schematic,
                SearchableText = BuildSearchableText(schematic),
                Keywords = ExtractKeywords(schematic),
                Tags = schematic.Tags.ToList(),
                Category = schematic.PrimaryCategory,
                Complexity = schematic.Complexity
            };

            _searchIndex[schematic.name] = index;
        }

        /// <summary>
        /// Build searchable text from schematic data
        /// </summary>
        private string BuildSearchableText(SchematicSO schematic)
        {
            var text = $"{schematic.SchematicName} {schematic.Description} {schematic.CreatedBy}";
            text += $" {string.Join(" ", schematic.Tags)}";
            text += $" {schematic.PrimaryCategory} {schematic.Complexity}";

            // Add item information
            foreach (var item in schematic.Items)
            {
                text += $" {item.ItemName} {item.TemplateName}";
            }

            return text.ToLower();
        }

        /// <summary>
        /// Extract keywords from schematic for enhanced search
        /// </summary>
        private List<string> ExtractKeywords(SchematicSO schematic)
        {
            var keywords = new List<string>();

            // Add name words
            keywords.AddRange(schematic.SchematicName.Split(' '));

            // Add category and complexity
            keywords.Add(schematic.PrimaryCategory.ToString());
            keywords.Add(schematic.Complexity.ToString());

            // Add tags
            keywords.AddRange(schematic.Tags);

            // Add item count range
            if (schematic.ItemCount <= 5) keywords.Add("small");
            else if (schematic.ItemCount <= 15) keywords.Add("medium");
            else if (schematic.ItemCount <= 30) keywords.Add("large");
            else keywords.Add("huge");

            // Add cost range
            var cost = schematic.TotalEstimatedCost;
            if (cost <= 1000) keywords.Add("cheap");
            else if (cost <= 5000) keywords.Add("affordable");
            else if (cost <= 10000) keywords.Add("expensive");
            else keywords.Add("premium");

            return keywords.Distinct().ToList();
        }

        /// <summary>
        /// Perform advanced search with scoring and ranking
        /// </summary>
        public List<SchematicSearchResult> SearchSchematics(string query, SearchOptions options = null)
        {
            if (string.IsNullOrEmpty(query))
                return _allSchematics.Select(s => new SchematicSearchResult { Schematic = s, Score = 1f }).ToList();

            options = options ?? new SearchOptions();
            var results = new List<SchematicSearchResult>();
            var queryLower = query.ToLower();
            var queryWords = queryLower.Split(' ').Where(w => !string.IsNullOrEmpty(w)).ToArray();

            foreach (var indexEntry in _searchIndex.Values)
            {
                var score = CalculateSearchScore(indexEntry, queryWords, options);

                if (score >= _searchScoreThreshold)
                {
                    results.Add(new SchematicSearchResult
                    {
                        Schematic = indexEntry.Schematic,
                        Score = score,
                        MatchType = DetermineMatchType(indexEntry, queryWords)
                    });
                }
            }

            // Sort by score (descending) and then by name
            results.Sort((a, b) => {
                int scoreCompare = b.Score.CompareTo(a.Score);
                return scoreCompare != 0 ? scoreCompare :
                       string.Compare(a.Schematic.SchematicName, b.Schematic.SchematicName);
            });

            // Limit results
            if (results.Count > _maxSearchResults)
            {
                results = results.Take(_maxSearchResults).ToList();
            }

            _lastSearchTime = Time.time;
            _lastSearchQuery = query;
            OnSearchCompleted?.Invoke(query, results.Count);

            return results;
        }

        /// <summary>
        /// Calculate search score for a schematic
        /// </summary>
        private float CalculateSearchScore(SchematicSearchIndex index, string[] queryWords, SearchOptions options)
        {
            float score = 0f;
            float maxPossibleScore = queryWords.Length;

            foreach (var word in queryWords)
            {
                // Exact name match (highest priority)
                if (index.Schematic.SchematicName.ToLower().Contains(word))
                {
                    score += 1.0f;
                }
                // Description match
                else if (index.Schematic.Description.ToLower().Contains(word))
                {
                    score += 0.7f;
                }
                // Tag match
                else if (index.Tags.Any(tag => tag.ToLower().Contains(word)))
                {
                    score += 0.8f;
                }
                // Keyword match
                else if (index.Keywords.Any(keyword => keyword.ToLower().Contains(word)))
                {
                    score += 0.6f;
                }
                // Fuzzy match in searchable text
                else if (_enableFuzzySearch && index.SearchableText.Contains(word))
                {
                    score += 0.3f;
                }
            }

            // Apply category filter bonus
            if (options.CategoryFilter.HasValue && index.Category == options.CategoryFilter.Value)
            {
                score += 0.2f;
                maxPossibleScore += 0.2f;
            }

            // Apply complexity filter bonus
            if (options.ComplexityFilter.HasValue && index.Complexity == options.ComplexityFilter.Value)
            {
                score += 0.1f;
                maxPossibleScore += 0.1f;
            }

            return maxPossibleScore > 0 ? score / maxPossibleScore : 0f;
        }

        /// <summary>
        /// Determine the type of match for result highlighting
        /// </summary>
        private SearchMatchType DetermineMatchType(SchematicSearchIndex index, string[] queryWords)
        {
            var name = index.Schematic.SchematicName.ToLower();

            if (queryWords.Any(word => name.StartsWith(word)))
                return SearchMatchType.NamePrefix;
            if (queryWords.Any(word => name.Contains(word)))
                return SearchMatchType.NameContains;
            if (queryWords.Any(word => index.Tags.Any(tag => tag.ToLower().Contains(word))))
                return SearchMatchType.Tag;
            if (queryWords.Any(word => index.Schematic.Description.ToLower().Contains(word)))
                return SearchMatchType.Description;

            return SearchMatchType.Fuzzy;
        }

        /// <summary>
        /// Show the library panel
        /// </summary>
        public void ShowLibrary()
        {
            if (_libraryPanel != null)
            {
                _libraryPanel.Show();
                _isLibraryVisible = true;
                OnLibraryVisibilityChanged?.Invoke(true);
            }
        }

        /// <summary>
        /// Hide the library panel
        /// </summary>
        public void HideLibrary()
        {
            if (_libraryPanel != null)
            {
                _libraryPanel.Hide();
                _isLibraryVisible = false;
                OnLibraryVisibilityChanged?.Invoke(false);
            }
        }

        /// <summary>
        /// Toggle library visibility
        /// </summary>
        public void ToggleLibrary()
        {
            if (_isLibraryVisible)
                HideLibrary();
            else
                ShowLibrary();
        }

        /// <summary>
        /// Add a schematic to the library
        /// </summary>
        public void AddSchematic(SchematicSO schematic)
        {
            if (schematic != null && !_allSchematics.Contains(schematic))
            {
                _allSchematics.Add(schematic);
                IndexSchematic(schematic);

                if (_libraryPanel != null)
                {
                    _libraryPanel.AddSchematic(schematic);
                }

                _loadedSchematicsCount++;
            }
        }

        /// <summary>
        /// Remove a schematic from the library
        /// </summary>
        public void RemoveSchematic(SchematicSO schematic)
        {
            if (_allSchematics.Remove(schematic))
            {
                _searchIndex.Remove(schematic.name);

                if (_libraryPanel != null)
                {
                    _libraryPanel.RemoveSchematic(schematic);
                }

                _loadedSchematicsCount--;
            }
        }

        /// <summary>
        /// Load user preferences
        /// </summary>
        private void LoadPreferences()
        {
            if (_autoSavePreferences && PlayerPrefs.HasKey(_preferencesKey))
            {
                var json = PlayerPrefs.GetString(_preferencesKey);
                try
                {
                    _preferences = JsonUtility.FromJson<LibraryPreferences>(json);
                }
                catch (System.Exception e)
                {
                    ProjectChimera.Core.Logging.ChimeraLogger.LogWarning($"[SchematicLibraryManager] Failed to load preferences: {e.Message}");
                    _preferences = new LibraryPreferences();
                }
            }
        }

        /// <summary>
        /// Save user preferences
        /// </summary>
        private void SavePreferences()
        {
            if (_autoSavePreferences)
            {
                var json = JsonUtility.ToJson(_preferences);
                PlayerPrefs.SetString(_preferencesKey, json);
                PlayerPrefs.Save();
            }
        }

        // Event handlers
        private void OnSchematicSelectedFromLibrary(ChimeraScriptableObject schematic)
        {
            if (schematic is SchematicSO schematicSO)
            {
                OnSchematicSelectedInLibrary?.Invoke(schematicSO);
            }
        }

        private void OnSchematicAppliedFromLibrary(ChimeraScriptableObject schematic)
        {
            if (schematic is SchematicSO schematicSO)
            {
                ProjectChimera.Core.Logging.ChimeraLogger.Log($"[SchematicLibraryManager] Schematic applied from library: {schematicSO.SchematicName}");

                // Update usage statistics
                _preferences.UpdateUsageStats(schematicSO.name);
                SavePreferences();
            }
        }

        private void OnSchematicDeletedFromLibrary(ChimeraScriptableObject schematic)
        {
            if (schematic is SchematicSO schematicSO)
            {
                RemoveSchematic(schematicSO);
            }
        }

        private void OnSearchQueryChangedFromLibrary(string query)
        {
            _preferences.LastSearchQuery = query;
            SavePreferences();
        }

        private void OnViewModeChangedFromLibrary(LibraryViewMode viewMode)
        {
            _preferences.PreferredViewMode = viewMode;
            SavePreferences();
        }

        // Properties
        public bool IsLibraryVisible => _isLibraryVisible;
        public int LoadedSchematicsCount => _loadedSchematicsCount;
        public string LastSearchQuery => _lastSearchQuery;
        public float LastSearchTime => _lastSearchTime;
        public List<SchematicSO> AllSchematics => new List<SchematicSO>(_allSchematics);

        private void OnDestroy()
        {
            SavePreferences();

            // Clean up event handlers
            if (_libraryPanel != null)
            {
                _libraryPanel.SchematicSelected -= OnSchematicSelectedFromLibrary;
                _libraryPanel.SchematicApplied -= OnSchematicAppliedFromLibrary;
                _libraryPanel.SchematicDeleted -= OnSchematicDeletedFromLibrary;
                _libraryPanel.SearchQueryChanged -= OnSearchQueryChangedFromLibrary;
                _libraryPanel.ViewModeChanged -= OnViewModeChangedFromLibrary;
            }
        }
    }

    /// <summary>
    /// Search index entry for a schematic
    /// </summary>
    [System.Serializable]
    public class SchematicSearchIndex
    {
        public SchematicSO Schematic;
        public string SearchableText;
        public List<string> Keywords;
        public List<string> Tags;
        public ConstructionCategory Category;
        public SchematicComplexity Complexity;
    }

    /// <summary>
    /// Search result with scoring
    /// </summary>
    [System.Serializable]
    public class SchematicSearchResult
    {
        public SchematicSO Schematic;
        public float Score;
        public SearchMatchType MatchType;
    }

    /// <summary>
    /// Search options for advanced filtering
    /// </summary>
    [System.Serializable]
    public class SearchOptions
    {
        public ConstructionCategory? CategoryFilter;
        public SchematicComplexity? ComplexityFilter;
        public List<string> TagFilters = new List<string>();
        public bool SearchInNames = true;
        public bool SearchInDescriptions = true;
        public bool SearchInTags = true;
        public bool EnableFuzzySearch = true;
    }

    /// <summary>
    /// Types of search matches for result highlighting
    /// </summary>
    public enum SearchMatchType
    {
        NamePrefix,
        NameContains,
        Description,
        Tag,
        Fuzzy
    }

    /// <summary>
    /// User preferences for the library
    /// </summary>
    [System.Serializable]
    public class LibraryPreferences
    {
        public LibraryViewMode PreferredViewMode = LibraryViewMode.Grid;
        public string LastSearchQuery = "";
        public List<string> RecentSearches = new List<string>();
        public Dictionary<string, int> UsageStats = new Dictionary<string, int>();

        public void UpdateUsageStats(string schematicName)
        {
            if (UsageStats.ContainsKey(schematicName))
                UsageStats[schematicName]++;
            else
                UsageStats[schematicName] = 1;
        }
    }
}
