using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Economy;
using ProjectChimera.Data.Shared;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ProjectChimera.Systems.Economy
{
    /// <summary>
    /// Service responsible for managing available schematics and unlock status for Phase 8 MVP
    /// Handles schematic market operations, purchases, and progression unlocks
    /// </summary>
    public class SchematicMarketService : ChimeraManager
    {
        [Header("Market Configuration")]
        [SerializeField] private SchematicMarketSO _marketConfiguration;
        [SerializeField] private bool _enableMarketRefresh = true;
        [SerializeField] private float _refreshCheckInterval = 3600f; // 1 hour
        [SerializeField] private bool _enablePlayerLevelScaling = true;
        [SerializeField] private int _defaultPlayerLevel = 1;
        
        [Header("Unlock Management")]
        [SerializeField] private bool _enableProgressionLocks = true;
        [SerializeField] private bool _saveUnlockProgress = true;
        [SerializeField] private bool _enablePrerequisiteChecking = true;
        
        [Header("Market Dynamics")]
        [SerializeField] private bool _enableDynamicInventory = true;
        [SerializeField] private int _maxFeaturedSchematics = 6;
        [SerializeField] private bool _enableRecommendations = true;
        [SerializeField] private int _maxRecommendations = 5;
        
        // Service dependencies
        private CurrencyManager _currencyManager;
        
        // Market state
        private Dictionary<string, bool> _unlockedSchematics = new Dictionary<string, bool>();
        private Dictionary<string, DateTime> _schematicUnlockDates = new Dictionary<string, DateTime>();
        private List<ConstructionSchematicSO> _featuredSchematics = new List<ConstructionSchematicSO>();
        private List<ConstructionSchematicSO> _recommendedSchematics = new List<ConstructionSchematicSO>();
        
        // Market tracking
        private DateTime _lastMarketRefresh;
        private int _currentPlayerLevel = 1;
        private SchematicMarketMetrics _metrics = new SchematicMarketMetrics();
        private float _lastRefreshCheck;
        
        public override ManagerPriority Priority => ManagerPriority.Normal;
        
        // Public Properties
        public SchematicMarketSO MarketConfiguration => _marketConfiguration;
        public bool IsMarketActive => _marketConfiguration?.IsMarketActive ?? false;
        public int UnlockedSchematicsCount => _unlockedSchematics.Count(kvp => kvp.Value);
        public int TotalSchematicsCount => _marketConfiguration?.GetAllSchematics().Count ?? 0;
        public List<ConstructionSchematicSO> FeaturedSchematics => new List<ConstructionSchematicSO>(_featuredSchematics);
        public List<ConstructionSchematicSO> RecommendedSchematics => new List<ConstructionSchematicSO>(_recommendedSchematics);
        public SchematicMarketMetrics Metrics => _metrics;
        public int CurrentPlayerLevel { get => _currentPlayerLevel; set => UpdatePlayerLevel(value); }
        
        // Events
        public System.Action<ConstructionSchematicSO> OnSchematicUnlocked;
        public System.Action<ConstructionSchematicSO, float> OnSchematicPurchased; // schematic, cost
        public System.Action<List<ConstructionSchematicSO>> OnFeaturedSchematicsUpdated;
        public System.Action<List<ConstructionSchematicSO>> OnRecommendationsUpdated;
        public System.Action<string> OnPurchaseError; // error message
        public System.Action OnMarketRefreshed;
        
        protected override void OnManagerInitialize()
        {
            InitializeSystemReferences();
            ValidateConfiguration();
            InitializeMarketState();
            LoadUnlockProgress();
            RefreshMarketContent();
            
            LogInfo($"SchematicMarketService initialized with {TotalSchematicsCount} total schematics");
        }
        
        private void Update()
        {
            if (!IsInitialized) return;
            
            float currentTime = Time.time;
            
            // Check for market refresh
            if (_enableMarketRefresh && currentTime - _lastRefreshCheck >= _refreshCheckInterval)
            {
                CheckMarketRefresh();
                _lastRefreshCheck = currentTime;
            }
        }
        
        /// <summary>
        /// Purchase a schematic using skill points
        /// </summary>
        public bool PurchaseSchematic(ConstructionSchematicSO schematic)
        {
            if (schematic == null)
            {
                OnPurchaseError?.Invoke("Invalid schematic");
                return false;
            }
            
            // Check if already unlocked
            if (IsSchematicUnlocked(schematic.SchematicId))
            {
                OnPurchaseError?.Invoke("Schematic already unlocked");
                return false;
            }
            
            // Check prerequisites
            if (_enablePrerequisiteChecking && !ArePrerequisitesMet(schematic))
            {
                OnPurchaseError?.Invoke("Prerequisites not met");
                return false;
            }
            
            // Check player level
            if (_enableProgressionLocks && _currentPlayerLevel < schematic.UnlockLevel)
            {
                OnPurchaseError?.Invoke($"Requires level {schematic.UnlockLevel}");
                return false;
            }
            
            // Calculate cost
            float cost = _marketConfiguration.CalculateSchematicPrice(schematic);
            
            // Check if player can afford it
            if (_currencyManager == null || !_currencyManager.HasSufficientSkillPoints(cost))
            {
                float available = _currencyManager?.GetSkillPointsBalance() ?? 0f;
                OnPurchaseError?.Invoke($"Insufficient skill points. Need {cost:F0}, have {available:F0}");
                return false;
            }
            
            // Process purchase
            bool success = _currencyManager.PurchaseWithSkillPoints(cost, schematic.SchematicName, 
                $"Unlocked schematic: {schematic.SchematicDescription}");
            
            if (success)
            {
                UnlockSchematic(schematic.SchematicId);
                
                // Update metrics
                _metrics.TotalPurchases++;
                _metrics.TotalSkillPointsSpent += cost;
                
                OnSchematicPurchased?.Invoke(schematic, cost);
                LogInfo($"Purchased schematic: {schematic.SchematicName} for {cost:F0} skill points");
                
                // Refresh recommendations after purchase
                RefreshRecommendations();
                
                return true;
            }
            
            OnPurchaseError?.Invoke("Purchase failed");
            return false;
        }
        
        /// <summary>
        /// Check if a schematic is unlocked
        /// </summary>
        public bool IsSchematicUnlocked(string schematicId)
        {
            return _unlockedSchematics.TryGetValue(schematicId, out bool unlocked) && unlocked;
        }
        
        /// <summary>
        /// Manually unlock a schematic (for testing or admin functions)
        /// </summary>
        public void UnlockSchematic(string schematicId, bool saveProgress = true)
        {
            if (string.IsNullOrEmpty(schematicId)) return;
            
            bool wasUnlocked = IsSchematicUnlocked(schematicId);
            _unlockedSchematics[schematicId] = true;
            _schematicUnlockDates[schematicId] = DateTime.Now;
            
            if (!wasUnlocked)
            {
                var schematic = GetSchematicById(schematicId);
                if (schematic != null)
                {
                    schematic.IsUnlocked = true;
                    OnSchematicUnlocked?.Invoke(schematic);
                    
                    _metrics.TotalUnlocked++;
                    LogInfo($"Unlocked schematic: {schematic.SchematicName}");
                }
                
                if (saveProgress && _saveUnlockProgress)
                {
                    SaveUnlockProgress();
                }
            }
        }
        
        /// <summary>
        /// Get all schematics available for purchase
        /// </summary>
        public List<ConstructionSchematicSO> GetAvailableSchematics(SchematicCategoryType? categoryFilter = null)
        {
            if (_marketConfiguration == null) return new List<ConstructionSchematicSO>();
            
            var allSchematics = _marketConfiguration.GetAllSchematics();
            
            // Filter by category if specified
            if (categoryFilter.HasValue)
            {
                allSchematics = allSchematics.Where(s => s.CategoryType == categoryFilter.Value).ToList();
            }
            
            // Filter by player level if progression locks enabled
            if (_enableProgressionLocks)
            {
                allSchematics = allSchematics.Where(s => s.UnlockLevel <= _currentPlayerLevel + 2).ToList();
            }
            
            return allSchematics;
        }
        
        /// <summary>
        /// Get unlocked schematics by category
        /// </summary>
        public List<ConstructionSchematicSO> GetUnlockedSchematics(SchematicCategoryType? categoryFilter = null)
        {
            var available = GetAvailableSchematics(categoryFilter);
            return available.Where(s => IsSchematicUnlocked(s.SchematicId)).ToList();
        }
        
        /// <summary>
        /// Get locked schematics that can be purchased
        /// </summary>
        public List<ConstructionSchematicSO> GetPurchasableSchematics(SchematicCategoryType? categoryFilter = null)
        {
            var available = GetAvailableSchematics(categoryFilter);
            return available.Where(s => !IsSchematicUnlocked(s.SchematicId) && CanPurchaseSchematic(s)).ToList();
        }
        
        /// <summary>
        /// Check if a schematic can be purchased (ignoring skill points)
        /// </summary>
        public bool CanPurchaseSchematic(ConstructionSchematicSO schematic)
        {
            if (schematic == null || IsSchematicUnlocked(schematic.SchematicId))
                return false;
            
            // Check level requirements
            if (_enableProgressionLocks && _currentPlayerLevel < schematic.UnlockLevel)
                return false;
            
            // Check prerequisites
            if (_enablePrerequisiteChecking && !ArePrerequisitesMet(schematic))
                return false;
            
            return true;
        }
        
        /// <summary>
        /// Force refresh market content
        /// </summary>
        public void RefreshMarket()
        {
            RefreshMarketContent();
            OnMarketRefreshed?.Invoke();
            LogInfo("Market manually refreshed");
        }
        
        /// <summary>
        /// Get market statistics
        /// </summary>
        public SchematicMarketStats GetMarketStats()
        {
            var stats = new SchematicMarketStats
            {
                TotalSchematics = TotalSchematicsCount,
                UnlockedSchematics = UnlockedSchematicsCount,
                AvailableForPurchase = GetPurchasableSchematics().Count,
                FeaturedCount = _featuredSchematics.Count,
                RecommendedCount = _recommendedSchematics.Count,
                PlayerLevel = _currentPlayerLevel,
                TotalPurchases = _metrics.TotalPurchases,
                TotalSkillPointsSpent = _metrics.TotalSkillPointsSpent
            };
            
            // Calculate category breakdown
            if (_marketConfiguration != null)
            {
                stats.CategoryBreakdown = new Dictionary<SchematicCategoryType, int>();
                foreach (var category in _marketConfiguration.SchematicCategories)
                {
                    var unlockedInCategory = category.AvailableSchematics.Count(s => IsSchematicUnlocked(s.SchematicId));
                    stats.CategoryBreakdown[category.CategoryType] = unlockedInCategory;
                }
            }
            
            return stats;
        }
        
        private void InitializeSystemReferences()
        {
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                _currencyManager = gameManager.GetManager<CurrencyManager>();
            }
            
            if (_currencyManager == null)
            {
                LogError("CurrencyManager not found - schematic purchases will not work");
            }
        }
        
        private void ValidateConfiguration()
        {
            if (_marketConfiguration == null)
            {
                LogError("SchematicMarketConfiguration is required");
                return;
            }
            
            if (!_marketConfiguration.ValidateData())
            {
                LogWarning("Market configuration has validation errors");
            }
        }
        
        private void InitializeMarketState()
        {
            _unlockedSchematics.Clear();
            _schematicUnlockDates.Clear();
            _featuredSchematics.Clear();
            _recommendedSchematics.Clear();
            _lastMarketRefresh = DateTime.Now;
            
            _metrics = new SchematicMarketMetrics
            {
                ServiceStartTime = DateTime.Now
            };
        }
        
        private void LoadUnlockProgress()
        {
            // For Phase 8 MVP, start with no unlocks
            // In full implementation, this would load from save system
            LogInfo("Unlock progress initialized (MVP - no saves)");
        }
        
        private void SaveUnlockProgress()
        {
            // For Phase 8 MVP, no save system integration
            // In full implementation, this would save to persistent storage
            LogInfo("Unlock progress saved (MVP - no persistence)");
        }
        
        private void RefreshMarketContent()
        {
            RefreshFeaturedSchematics();
            RefreshRecommendations();
            _lastMarketRefresh = DateTime.Now;
        }
        
        private void RefreshFeaturedSchematics()
        {
            if (_marketConfiguration == null) return;
            
            _featuredSchematics.Clear();
            
            var availableSchematics = GetPurchasableSchematics();
            
            // Prioritize by rarity and player level appropriateness
            var featured = availableSchematics
                .Where(s => s.UnlockLevel <= _currentPlayerLevel + 1)
                .OrderByDescending(s => (int)s.Rarity)
                .ThenBy(s => s.UnlockLevel)
                .Take(_maxFeaturedSchematics)
                .ToList();
            
            _featuredSchematics.AddRange(featured);
            OnFeaturedSchematicsUpdated?.Invoke(_featuredSchematics);
        }
        
        private void RefreshRecommendations()
        {
            if (!_enableRecommendations || _marketConfiguration == null) return;
            
            _recommendedSchematics.Clear();
            
            var recommendations = _marketConfiguration.GetRecommendedSchematics(_currentPlayerLevel, _maxRecommendations);
            _recommendedSchematics.AddRange(recommendations.Where(s => !IsSchematicUnlocked(s.SchematicId)));
            
            OnRecommendationsUpdated?.Invoke(_recommendedSchematics);
        }
        
        private void CheckMarketRefresh()
        {
            if (_marketConfiguration == null) return;
            
            float hoursSinceRefresh = (float)(DateTime.Now - _lastMarketRefresh).TotalHours;
            
            if (hoursSinceRefresh >= _marketConfiguration.MarketRefreshIntervalHours)
            {
                RefreshMarketContent();
                LogInfo("Market automatically refreshed");
            }
        }
        
        private void UpdatePlayerLevel(int newLevel)
        {
            if (newLevel != _currentPlayerLevel)
            {
                _currentPlayerLevel = newLevel;
                RefreshMarketContent(); // Refresh when level changes
                LogInfo($"Player level updated to {newLevel}");
            }
        }
        
        private bool ArePrerequisitesMet(ConstructionSchematicSO schematic)
        {
            if (schematic.PrerequisiteSchematicIds.Count == 0) return true;
            
            var unlockedIds = _unlockedSchematics.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
            return schematic.ArePrerequisitesMet(unlockedIds);
        }
        
        private ConstructionSchematicSO GetSchematicById(string schematicId)
        {
            if (_marketConfiguration == null) return null;
            
            return _marketConfiguration.GetAllSchematics().FirstOrDefault(s => s.SchematicId == schematicId);
        }
        
        protected override void OnManagerShutdown()
        {
            if (_saveUnlockProgress)
            {
                SaveUnlockProgress();
            }
            
            LogInfo($"SchematicMarketService shutdown - {_metrics.TotalPurchases} purchases made, {_metrics.TotalUnlocked} schematics unlocked");
        }
    }
    
    /// <summary>
    /// Market service metrics
    /// </summary>
    [System.Serializable]
    public class SchematicMarketMetrics
    {
        public DateTime ServiceStartTime;
        public int TotalPurchases;
        public int TotalUnlocked;
        public float TotalSkillPointsSpent;
        public int MarketRefreshes;
    }
    
    /// <summary>
    /// Market statistics for UI display
    /// </summary>
    [System.Serializable]
    public class SchematicMarketStats
    {
        public int TotalSchematics;
        public int UnlockedSchematics;
        public int AvailableForPurchase;
        public int FeaturedCount;
        public int RecommendedCount;
        public int PlayerLevel;
        public int TotalPurchases;
        public float TotalSkillPointsSpent;
        public Dictionary<SchematicCategoryType, int> CategoryBreakdown;
    }
}