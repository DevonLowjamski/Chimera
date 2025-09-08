using ProjectChimera.Core.Logging;
using ProjectChimera.Core;
using ProjectChimera.Core.Updates;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Systems.Registry;
using ProjectChimera.Data.Economy;
using ProjectChimera.Data.Economy.Transactions;

namespace ProjectChimera.Systems.Services.Economy
{
    /// <summary>
    /// PC014-4b: Trading Post Management Service
    /// Handles trading post operations, availability, and state management
    /// Decomposed from TradingManager (500 lines target)
    /// </summary>
    public class TradingPostManagementService : MonoBehaviour, ITickable, ITradingPostManagementService
    {
        #region Properties

        public bool IsInitialized { get; private set; }

        #endregion

        #region Private Fields

        [Header("Trading Post Configuration")]
        [SerializeField] private bool _enableTradingPosts = true;
        [SerializeField] private float _restockInterval = 24f; // Hours
        [SerializeField] private float _priceUpdateInterval = 6f; // Hours
        [SerializeField] private int _maxTradingPosts = 20;

        [Header("Trading Post Data")]
        [SerializeField] private List<TradingPost> _availableTradingPosts = new List<TradingPost>();
        [SerializeField] private Dictionary<TradingPost, TradingPostState> _tradingPostStates = new Dictionary<TradingPost, TradingPostState>();
        [SerializeField] private List<TradingOpportunity> _currentOpportunities = new List<TradingOpportunity>();

        [Header("Market Settings")]
        [SerializeField] private float _basePriceMarkup = 1.1f;
        [SerializeField] private float _maxPriceMarkup = 1.5f;
        [SerializeField] private float _reputationPriceModifier = 0.1f;

        private float _lastRestockUpdate;
        private float _lastPriceUpdate;

        #endregion

        #region Events

        // Interface events
        public event Action<TradingPost> OnTradingPostStatusChanged;
        public event Action<TradingOpportunity> OnTradingOpportunityAdded;
        public event Action<string> OnTradingOpportunityExpired;

        // Additional events
        public event Action<TradingPost, TradingPostState> OnTradingPostStateChanged;
        public event Action<TradingPost> OnTradingPostRestocked;
        public event Action<TradingPost, float> OnTradingPostPricesUpdated;

        #endregion

        #region IService Implementation

        public void Initialize()
        {
            if (IsInitialized) return;

            ChimeraLogger.Log("Initializing TradingPostManagementService...");

            // Initialize collections
            InitializeTradingPostSystem();

            // Load trading posts
            InitializeTradingPosts();

            // Generate initial opportunities
            GenerateNewOpportunities();

            // Register with central ServiceRegistry
            ServiceContainerFactory.Instance.RegisterSingleton<ITradingPostManagementService>(this);

            IsInitialized = true;
            ChimeraLogger.Log("TradingPostManagementService initialized successfully");
        }

        public void Shutdown()
        {
            if (!IsInitialized) return;

            ChimeraLogger.Log("Shutting down TradingPostManagementService...");

            // Save trading post states
            SaveTradingPostStates();

            // Clear collections
            _availableTradingPosts.Clear();
            _tradingPostStates.Clear();
            _currentOpportunities.Clear();

            IsInitialized = false;
            ChimeraLogger.Log("TradingPostManagementService shutdown complete");
        }

        #endregion

        #region Trading Post Management

        public List<TradingPost> GetAvailableTradingPosts()
        {
            return _availableTradingPosts.Where(tp => tp.Status == TradingPostStatus.Open).ToList();
        }

        public List<TradingPost> GetTradingPostsByType(TradingPostType type)
        {
            return _availableTradingPosts.Where(tp => tp.Type == type && tp.Status == TradingPostStatus.Open).ToList();
        }

        public TradingPost GetTradingPost(string tradingPostId)
        {
            return _availableTradingPosts.FirstOrDefault(tp => tp.TradingPostId == tradingPostId);
        }

        public TradingPostStatus GetTradingPostStatus(string tradingPostId)
        {
            var tradingPost = GetTradingPost(tradingPostId);
            return tradingPost?.Status ?? TradingPostStatus.Closed;
        }

        public List<MarketProductSO> GetAvailableProducts(string tradingPostId)
        {
            var tradingPost = GetTradingPost(tradingPostId);
            if (tradingPost == null) return new List<MarketProductSO>();

            // Return placeholder - would need to resolve product IDs to actual SO references
            return new List<MarketProductSO>();
        }

        public float GetProductQuantity(string tradingPostId, MarketProductSO product)
        {
            var tradingPost = GetTradingPost(tradingPostId);
            if (tradingPost == null) return 0f;

            var state = GetTradingPostState(tradingPost);
            return state?.CurrentInventory?.GetValueOrDefault(product.name, 0f) ?? 0f;
        }

        public bool IsProductAvailable(string tradingPostId, MarketProductSO product, float quantity)
        {
            var tradingPost = GetTradingPost(tradingPostId);
            return tradingPost != null && IsTradingPostAvailable(tradingPost, product, quantity);
        }

        public List<TradingOpportunity> GetTradingOpportunities(OpportunityType opportunityType = OpportunityType.All)
        {
            if (opportunityType == OpportunityType.All)
                return GetCurrentTradingOpportunities();

            return _currentOpportunities.Where(o => o.Type == opportunityType && o.ExpirationTime > DateTime.Now).ToList();
        }

        public bool IsOpportunityValid(string opportunityId)
        {
            var opportunity = GetTradingOpportunity(opportunityId);
            return opportunity != null && opportunity.ExpirationTime > DateTime.Now;
        }

        public void UpdateTradingOpportunities()
        {
            GenerateNewOpportunities();
            RemoveExpiredOpportunities();
        }

        private void GenerateNewOpportunities()
        {
            // Generate new trading opportunities
            int numToGenerate = UnityEngine.Random.Range(1, 4);
            for (int i = 0; i < numToGenerate; i++)
            {
                CreateRandomTradingOpportunity();
            }
        }

        private void RemoveExpiredOpportunities()
        {
            var expired = _currentOpportunities.Where(o => o.ExpirationTime <= DateTime.Now).ToList();
            foreach (var opportunity in expired)
            {
                _currentOpportunities.Remove(opportunity);
                OnTradingOpportunityExpired?.Invoke(opportunity.OpportunityId);
            }
        }

        private void CreateRandomTradingOpportunity()
        {
            if (_availableTradingPosts.Count == 0) return;

            var tradingPost = _availableTradingPosts[UnityEngine.Random.Range(0, _availableTradingPosts.Count)];
            var opportunity = new TradingOpportunity
            {
                OpportunityId = Guid.NewGuid().ToString(),
                Name = "Trading Opportunity",
                Description = "Limited time trading opportunity",
                Type = (OpportunityType)UnityEngine.Random.Range(1, 5), // Skip 'All' which is 0
                SourcePost = tradingPost,
                PotentialProfit = UnityEngine.Random.Range(100f, 1000f),
                ProfitMargin = UnityEngine.Random.Range(0.1f, 0.3f),
                RequiredCapital = UnityEngine.Random.Range(500f, 5000f),
                ExpirationTime = DateTime.Now.AddHours(UnityEngine.Random.Range(6, 72)),
                RiskLevel = UnityEngine.Random.Range(0.1f, 0.8f),
                RecommendedAction = "Evaluate for profitability"
            };

            _currentOpportunities.Add(opportunity);
            OnTradingOpportunityAdded?.Invoke(opportunity);
        }


        public TradingPostState GetTradingPostState(TradingPost tradingPost)
        {
            return _tradingPostStates.GetValueOrDefault(tradingPost, null);
        }

        public bool IsTradingPostAvailable(TradingPost tradingPost, MarketProductSO product, float quantity)
        {
            if (!_enableTradingPosts || tradingPost == null || tradingPost.Status != TradingPostStatus.Open)
                return false;

            var state = GetTradingPostState(tradingPost);
            if (state == null)
                return false;

            // Check if trading post accepts this product type
            if (!tradingPost.AvailableProducts.Contains(product.name))
                return false;

            // Check available quantity
            var availableQuantity = state.CurrentInventory.GetValueOrDefault(product.name, 0f);
            if (availableQuantity < quantity)
                return false;

            return true;
        }

        public float GetTradingPostPrice(TradingPost tradingPost, MarketProductSO product, float quantity, bool isBuying)
        {
            var state = GetTradingPostState(tradingPost);
            if (state == null)
                return 0f;

            float basePrice = product.BaseWholesalePrice;
            float markup = isBuying ? state.PriceMarkup : (1f / state.PriceMarkup);

            return basePrice * markup * quantity;
        }

        public bool ReserveTradingPostProduct(TradingPost tradingPost, MarketProductSO product, float quantity)
        {
            if (!IsTradingPostAvailable(tradingPost, product, quantity))
                return false;

            var state = GetTradingPostState(tradingPost);
            if (state != null && state.CurrentInventory.ContainsKey(product.name))
            {
                state.CurrentInventory[product.name] -= quantity;
                OnTradingPostStateChanged?.Invoke(tradingPost, state);
                ChimeraLogger.Log($"Reserved {quantity} of {product.ProductName} at {tradingPost.Name}");
                return true;
            }

            return false;
        }

        public void UpdateTradingPostReputation(TradingPost tradingPost, float reputationChange)
        {
            var state = GetTradingPostState(tradingPost);
            if (state != null)
            {
                // Store reputation in DynamicData since ReputationWithPlayer doesn't exist
                float currentReputation = 0.5f; // Default reputation
                if (state.DynamicData.ContainsKey("PlayerReputation"))
                {
                    currentReputation = (float)state.DynamicData["PlayerReputation"];
                }

                currentReputation = Mathf.Clamp01(currentReputation + reputationChange);
                state.DynamicData["PlayerReputation"] = currentReputation;

                UpdateTradingPostPrices(state);
                OnTradingPostStateChanged?.Invoke(tradingPost, state);
                ChimeraLogger.Log($"Updated reputation with {tradingPost.Name}: {currentReputation:F2}");
            }
        }

        #endregion

        #region Trading Opportunities

        public List<TradingOpportunity> GetCurrentTradingOpportunities()
        {
            return _currentOpportunities.Where(o => o.ExpirationTime > DateTime.Now).ToList();
        }

        public TradingOpportunity GetTradingOpportunity(string opportunityId)
        {
            return _currentOpportunities.FirstOrDefault(o => o.OpportunityId == opportunityId);
        }

        public bool ClaimTradingOpportunity(string opportunityId, string playerId)
        {
            var opportunity = GetTradingOpportunity(opportunityId);
            if (opportunity == null || opportunity.ExpirationTime <= DateTime.Now)
                return false;

            // Mark opportunity as claimed in metadata since these properties don't exist
            opportunity.OpportunityData["IsActive"] = false;
            opportunity.OpportunityData["ClaimedBy"] = playerId;
            opportunity.OpportunityData["ClaimedDate"] = DateTime.Now;

            ChimeraLogger.Log($"Player {playerId} claimed trading opportunity: {opportunity.Type}");
            return true;
        }

        public void GenerateTradingOpportunity()
        {
            var opportunityTypes = new[] {
                OpportunityType.Buy,
                OpportunityType.Sell,
                OpportunityType.Arbitrage,
                OpportunityType.Special
            };

            var selectedType = opportunityTypes[UnityEngine.Random.Range(0, opportunityTypes.Length)];
            var tradingPost = _availableTradingPosts[UnityEngine.Random.Range(0, _availableTradingPosts.Count)];

            var opportunity = new TradingOpportunity
            {
                OpportunityId = Guid.NewGuid().ToString(),
                Name = "Trading Opportunity",
                Description = "Limited time trading opportunity",
                Type = selectedType,
                SourcePost = tradingPost,
                PotentialProfit = UnityEngine.Random.Range(100f, 1000f),
                ProfitMargin = UnityEngine.Random.Range(0.1f, 0.3f),
                RequiredCapital = UnityEngine.Random.Range(500f, 5000f),
                ExpirationTime = DateTime.Now.AddHours(UnityEngine.Random.Range(6, 72)),
                RiskLevel = UnityEngine.Random.Range(0.1f, 0.8f),
                RecommendedAction = "Evaluate for profitability"
            };

            _currentOpportunities.Add(opportunity);
            OnTradingOpportunityAdded?.Invoke(opportunity);
            ChimeraLogger.Log($"Generated trading opportunity: {opportunity.Type} at {tradingPost.Name}");
        }

        #endregion

        #region Private Helper Methods

        private void InitializeTradingPostSystem()
        {
            if (_tradingPostStates == null)
                _tradingPostStates = new Dictionary<TradingPost, TradingPostState>();

            if (_currentOpportunities == null)
                _currentOpportunities = new List<TradingOpportunity>();

            ChimeraLogger.Log("Trading post system initialized");
        }

        private void InitializeTradingPosts()
        {
            if (_availableTradingPosts.Count == 0)
            {
                CreateDefaultTradingPosts();
            }

            // Initialize states for all trading posts
            foreach (var tradingPost in _availableTradingPosts)
            {
                if (!_tradingPostStates.ContainsKey(tradingPost))
                {
                    _tradingPostStates[tradingPost] = CreateDefaultTradingPostState(tradingPost);
                }
            }

            ChimeraLogger.Log($"Initialized {_availableTradingPosts.Count} trading posts");
        }

        private void CreateDefaultTradingPosts()
        {
            // Create sample trading posts - would be loaded from data in production
            var dispensary = new TradingPost
            {
                TradingPostId = "dispensary_001",
                Name = "Green Valley Dispensary",
                Description = "Downtown dispensary specializing in premium flower and concentrates",
                Type = TradingPostType.Dispensary,
                Location = new Vector3(10f, 0f, 15f), // Downtown coordinates
                AvailableProducts = new List<string> { "Flower", "Concentrate" },
                Status = TradingPostStatus.Open,
                ContactInfo = "Downtown Location"
            };

            var processor = new TradingPost
            {
                TradingPostId = "processor_001",
                Name = "Premium Extracts Co",
                Description = "Industrial processing facility for biomass and trim",
                Type = TradingPostType.Processor,
                Location = new Vector3(-20f, 0f, 30f), // Industrial district coordinates
                AvailableProducts = new List<string> { "Biomass", "Trim" },
                Status = TradingPostStatus.Open,
                ContactInfo = "Industrial District Location"
            };

            _availableTradingPosts.Add(dispensary);
            _availableTradingPosts.Add(processor);
        }

        private TradingPostState CreateDefaultTradingPostState(TradingPost tradingPost)
        {
            var state = new TradingPostState
            {
                TradingPostId = tradingPost.TradingPostId,
                CurrentStatus = TradingPostStatus.Open,
                PriceMarkup = _basePriceMarkup,
                LastUpdate = DateTime.Now.AddHours(-UnityEngine.Random.Range(1, 12)),
                CurrentInventory = new Dictionary<string, float>(),
                DynamicData = new Dictionary<string, object>
                {
                    ["PlayerReputation"] = 0.5f
                }
            };

            RestockTradingPost(state);
            return state;
        }

        private void GenerateInitialOpportunities()
        {
            // Generate 2-5 initial opportunities
            int numOpportunities = UnityEngine.Random.Range(2, 6);
            for (int i = 0; i < numOpportunities; i++)
            {
                GenerateTradingOpportunity();
            }
        }

        private void SaveTradingPostStates()
        {
            // TODO: Implement persistent storage
            ChimeraLogger.Log("Saving trading post states...");
        }

        private void RestockTradingPost(TradingPostState state)
        {
            // Get the actual trading post from our list
            var tradingPost = _availableTradingPosts.FirstOrDefault(tp => tp.TradingPostId == state.TradingPostId);
            if (tradingPost == null) return;

            state.CurrentInventory.Clear();

            foreach (var productName in tradingPost.AvailableProducts)
            {
                if (UnityEngine.Random.Range(0f, 1f) < 0.7f) // 70% chance to have each type
                {
                    float quantity = UnityEngine.Random.Range(10f, 100f);
                    state.CurrentInventory[productName] = quantity;
                }
            }

            state.LastUpdate = DateTime.Now;
            OnTradingPostRestocked?.Invoke(tradingPost);
            ChimeraLogger.Log($"Restocked trading post: {tradingPost.Name}");
        }

        private void UpdateTradingPostPrices(TradingPostState state)
        {
            // Get the actual trading post from our list
            var tradingPost = _availableTradingPosts.FirstOrDefault(tp => tp.TradingPostId == state.TradingPostId);
            if (tradingPost == null) return;

            // Get reputation from dynamic data since ReputationWithPlayer doesn't exist
            float reputation = 0.5f; // Default reputation
            if (state.DynamicData.ContainsKey("PlayerReputation"))
            {
                reputation = (float)state.DynamicData["PlayerReputation"];
            }

            float reputationBonus = (reputation - 0.5f) * _reputationPriceModifier;
            float oldMarkup = state.PriceMarkup;
            state.PriceMarkup = Mathf.Clamp(oldMarkup - reputationBonus, 1.0f, _maxPriceMarkup);

            OnTradingPostPricesUpdated?.Invoke(tradingPost, state.PriceMarkup);
        }

        private float GetOpportunityPriceModifier(OpportunityType type)
        {
            return type switch
            {
                OpportunityType.Buy => 0.8f,
                OpportunityType.Sell => 1.3f,
                OpportunityType.Arbitrage => 1.2f,
                OpportunityType.Special => 0.9f,
                _ => 1.0f
            };
        }

        private float GetOpportunityQualityRequirement(OpportunityType type)
        {
            return type switch
            {
                OpportunityType.Special => 0.8f,
                OpportunityType.Arbitrage => 0.7f,
                _ => 0.5f
            };
        }

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
        // Register with UpdateOrchestrator
        UpdateOrchestrator.Instance?.RegisterTickable(this);
            Initialize();
        }

        private void OnDestroy()
        {
        // Unregister from UpdateOrchestrator
        UpdateOrchestrator.Instance?.UnregisterTickable(this);
            Shutdown();
        }

            public void Tick(float deltaTime)
    {
            if (!IsInitialized || !_enableTradingPosts) return;

            float currentTime = UnityEngine.Time.time;

            // Update trading post restocking
            if (currentTime - _lastRestockUpdate >= _restockInterval * 3600f) // Convert hours to seconds
            {
                UpdateTradingPostRestocking();
                _lastRestockUpdate = currentTime;

    }

            // Update trading post prices
            if (currentTime - _lastPriceUpdate >= _priceUpdateInterval * 3600f)
            {
                UpdateAllTradingPostPrices();
                _lastPriceUpdate = currentTime;
            }

            // Clean up expired opportunities
            CleanupExpiredOpportunities();
        }

        private void UpdateTradingPostRestocking()
        {
            foreach (var kvp in _tradingPostStates)
            {
                var state = kvp.Value;
                if ((DateTime.Now - state.LastUpdate).TotalHours >= _restockInterval)
                {
                    RestockTradingPost(state);
                }
            }
        }

        private void UpdateAllTradingPostPrices()
        {
            foreach (var state in _tradingPostStates.Values)
            {
                UpdateTradingPostPrices(state);
            }
        }

        private void CleanupExpiredOpportunities()
        {
            _currentOpportunities.RemoveAll(o => o.ExpirationTime < DateTime.Now);
        }

        #endregion

    // ITickable implementation
    public int Priority => 0;
    public bool Enabled => enabled && gameObject.activeInHierarchy;

    public virtual void OnRegistered()
    {
        // Override in derived classes if needed
    }

    public virtual void OnUnregistered()
    {
        // Override in derived classes if needed
    }

    }
}
