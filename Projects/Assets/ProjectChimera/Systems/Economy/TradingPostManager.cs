using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Data.Economy;
using ProjectChimera.Data.Economy.Trading;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Economy
{
    /// <summary>
    /// Manages trading post states, availability, pricing, and relationships.
    /// Extracted from TradingManager for modular architecture.
    /// Handles trading post operations, restocking, and player reputation.
    /// </summary>
    public class TradingPostManager : MonoBehaviour
    {
        [Header("Trading Post Configuration")]
        [SerializeField] private bool _enableTradingPostLogging = true;
        [SerializeField] private float _restockCheckInterval = 6f; // Check every 6 game hours
        [SerializeField] private float _priceUpdateInterval = 4f; // Update prices every 4 game hours
        [SerializeField] private bool _enableDynamicPricing = true;
        
        // Trading post data
        private List<TradingPost> _availableTradingPosts = new List<TradingPost>();
        private Dictionary<TradingPost, TradingPostState> _tradingPostStates = new Dictionary<TradingPost, TradingPostState>();
        
        // Update timers
        private float _lastRestockCheck = 0f;
        private float _lastPriceUpdate = 0f;
        
        // Events
        public System.Action<TradingPost, TradingPostState> OnTradingPostUpdated;
        public System.Action<TradingPost> OnTradingPostRestocked;
        public System.Action<TradingPost, float> OnReputationChanged;
        
        // Properties
        public List<TradingPost> AvailableTradingPosts => _availableTradingPosts.ToList();
        public int ActiveTradingPostCount => _tradingPostStates.Values.Count(s => s.IsActive);
        
        /// <summary>
        /// Initialize trading post manager with available posts
        /// </summary>
        public void Initialize(List<TradingPost> tradingPosts)
        {
            _availableTradingPosts.Clear();
            _tradingPostStates.Clear();
            
            if (tradingPosts != null)
            {
                _availableTradingPosts.AddRange(tradingPosts);
                InitializeTradingPostStates();
            }
            
            LogDebug($"Trading post manager initialized with {_availableTradingPosts.Count} posts");
        }
        
        private void Update()
        {
            var timeManager = GameManager.Instance?.GetManager<TimeManager>();
            float gameTimeDelta = timeManager?.GetScaledDeltaTime() ?? Time.deltaTime;
            
            _lastRestockCheck += gameTimeDelta;
            _lastPriceUpdate += gameTimeDelta;
            
            if (_lastRestockCheck >= _restockCheckInterval)
            {
                CheckForRestocking();
                _lastRestockCheck = 0f;
            }
            
            if (_enableDynamicPricing && _lastPriceUpdate >= _priceUpdateInterval)
            {
                UpdateTradingPostPrices();
                _lastPriceUpdate = 0f;
            }
        }
        
        #region Trading Post State Management
        
        /// <summary>
        /// Initialize states for all trading posts
        /// </summary>
        private void InitializeTradingPostStates()
        {
            foreach (var tradingPost in _availableTradingPosts)
            {
                var state = new TradingPostState
                {
                    TradingPost = tradingPost,
                    IsActive = true,
                    PriceMarkup = Random.Range(1.05f, 1.25f), // 5-25% markup
                    CommissionRate = Random.Range(0.02f, 0.08f), // 2-8% commission
                    ReputationWithPlayer = 0.5f, // Neutral starting reputation
                    AvailableProducts = new List<TradingPostProduct>(),
                    LastRestockDate = System.DateTime.Now
                };
                
                // Generate initial product availability
                RestockTradingPost(state);
                _tradingPostStates[tradingPost] = state;
                
                LogDebug($"Initialized trading post: {tradingPost.TradingPostName}");
            }
        }
        
        /// <summary>
        /// Get trading post state
        /// </summary>
        public TradingPostState GetTradingPostState(TradingPost tradingPost)
        {
            return _tradingPostStates.TryGetValue(tradingPost, out var state) ? state : null;
        }
        
        /// <summary>
        /// Update trading post activity status
        /// </summary>
        public void SetTradingPostActive(TradingPost tradingPost, bool isActive)
        {
            if (_tradingPostStates.TryGetValue(tradingPost, out var state))
            {
                state.IsActive = isActive;
                OnTradingPostUpdated?.Invoke(tradingPost, state);
                LogDebug($"Trading post {tradingPost.TradingPostName} set to {(isActive ? "active" : "inactive")}");
            }
        }
        
        #endregion
        
        #region Product Availability
        
        /// <summary>
        /// Check if trading post has sufficient quantity of a product
        /// </summary>
        public bool IsProductAvailable(TradingPost tradingPost, MarketProductSO product, float quantity)
        {
            if (!_tradingPostStates.TryGetValue(tradingPost, out var state) || !state.IsActive)
                return false;
            
            var availableProduct = state.AvailableProducts.Find(p => p.Product == product);
            return availableProduct != null && availableProduct.AvailableQuantity >= quantity;
        }
        
        /// <summary>
        /// Check if trading post will accept a product for selling
        /// </summary>
        public bool WillAcceptProduct(TradingPost tradingPost, InventoryItem inventoryItem, float quantity)
        {
            if (!_tradingPostStates.TryGetValue(tradingPost, out var state) || !state.IsActive)
                return false;
            
            // Check quality requirements
            if (inventoryItem.QualityScore < tradingPost.MinimumQualityThreshold)
            {
                LogDebug($"Product quality {inventoryItem.QualityScore:F2} below minimum {tradingPost.MinimumQualityThreshold:F2}");
                return false;
            }
            
            // Check if trading post deals with this product category
            string productCategory = inventoryItem.Product.Category.ToString();
            if (!tradingPost.AcceptedProductTypes.Contains(productCategory))
            {
                LogDebug($"Trading post {tradingPost.TradingPostName} doesn't accept {productCategory}");
                return false;
            }
            
            // Check reputation requirements (some posts might not deal with low-reputation players)
            if (state.ReputationWithPlayer < 0.2f)
            {
                LogDebug($"Player reputation too low for {tradingPost.TradingPostName}");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Get all products available at a trading post
        /// </summary>
        public List<TradingPostProduct> GetAvailableProducts(TradingPost tradingPost)
        {
            if (_tradingPostStates.TryGetValue(tradingPost, out var state))
            {
                return state.AvailableProducts.Where(p => p.AvailableQuantity > 0).ToList();
            }
            
            return new List<TradingPostProduct>();
        }
        
        /// <summary>
        /// Reserve product quantity for a pending transaction
        /// </summary>
        public bool ReserveProduct(TradingPost tradingPost, MarketProductSO product, float quantity)
        {
            if (_tradingPostStates.TryGetValue(tradingPost, out var state))
            {
                var availableProduct = state.AvailableProducts.Find(p => p.Product == product);
                if (availableProduct != null && availableProduct.AvailableQuantity >= (int)quantity)
                {
                    availableProduct.AvailableQuantity -= (int)quantity;
                    LogDebug($"Reserved {quantity:F1}g of {product.ProductName} at {tradingPost.TradingPostName}");
                    return true;
                }
            }
            
            return false;
        }
        
        #endregion
        
        #region Pricing
        
        /// <summary>
        /// Get price markup for a trading post
        /// </summary>
        public float GetPriceMarkup(TradingPost tradingPost)
        {
            if (_tradingPostStates.TryGetValue(tradingPost, out var state))
            {
                return state.PriceMarkup;
            }
            
            return 1.15f; // Default 15% markup
        }
        
        /// <summary>
        /// Get commission multiplier (1.0 - commission rate)
        /// </summary>
        public float GetCommissionMultiplier(TradingPost tradingPost)
        {
            if (_tradingPostStates.TryGetValue(tradingPost, out var state))
            {
                return 1f - state.CommissionRate;
            }
            
            return 0.95f; // Default 5% commission
        }
        
        /// <summary>
        /// Update trading post prices based on market conditions and reputation
        /// </summary>
        private void UpdateTradingPostPrices()
        {
            foreach (var kvp in _tradingPostStates.ToList())
            {
                var tradingPost = kvp.Key;
                var state = kvp.Value;
                
                if (!state.IsActive) continue;
                
                // Adjust markup based on player reputation
                float reputationBonus = (state.ReputationWithPlayer - 0.5f) * 0.1f; // ±5% based on reputation
                float newMarkup = Mathf.Clamp(state.PriceMarkup - reputationBonus, 1.0f, 1.5f);
                
                if (Mathf.Abs(newMarkup - state.PriceMarkup) > 0.01f)
                {
                    state.PriceMarkup = newMarkup;
                    OnTradingPostUpdated?.Invoke(tradingPost, state);
                    LogDebug($"Updated price markup for {tradingPost.TradingPostName}: {state.PriceMarkup:F2}");
                }
            }
        }
        
        #endregion
        
        #region Restocking
        
        /// <summary>
        /// Check all trading posts for restocking opportunities
        /// </summary>
        private void CheckForRestocking()
        {
            foreach (var kvp in _tradingPostStates.ToList())
            {
                var tradingPost = kvp.Key;
                var state = kvp.Value;
                
                if (!state.IsActive) continue;
                
                // Check if enough time has passed since last restock
                double daysSinceRestock = (System.DateTime.Now - state.LastRestockDate).TotalDays;
                
                // Random restock with higher probability as time passes
                float restockChance = Mathf.Min(0.8f, (float)daysSinceRestock * 0.2f);
                
                if (Random.Range(0f, 1f) < restockChance)
                {
                    RestockTradingPost(state);
                    OnTradingPostRestocked?.Invoke(tradingPost);
                    LogDebug($"Restocked trading post: {tradingPost.TradingPostName}");
                }
            }
        }
        
        /// <summary>
        /// Restock a trading post with new products
        /// </summary>
        private void RestockTradingPost(TradingPostState state)
        {
            state.AvailableProducts.Clear();
            
            // Generate product availability based on trading post accepted types
            foreach (var productType in state.TradingPost.AcceptedProductTypes)
            {
                // 70% chance to have each accepted product type
                if (Random.Range(0f, 1f) < 0.7f)
                {
                    var product = new TradingPostProduct
                    {
                        // In a full implementation, this would select from available products of this category
                        AvailableQuantity = Random.Range(10, 200),
                        QualityRange = Random.Range(0.6f, 0.95f),
                        PriceModifier = Random.Range(0.9f, 1.1f)
                    };
                    
                    state.AvailableProducts.Add(product);
                }
            }
            
            state.LastRestockDate = System.DateTime.Now;
        }
        
        /// <summary>
        /// Force restock a specific trading post
        /// </summary>
        public void ForceRestock(TradingPost tradingPost)
        {
            if (_tradingPostStates.TryGetValue(tradingPost, out var state))
            {
                RestockTradingPost(state);
                OnTradingPostRestocked?.Invoke(tradingPost);
                LogDebug($"Force restocked trading post: {tradingPost.TradingPostName}");
            }
        }
        
        #endregion
        
        #region Reputation Management
        
        /// <summary>
        /// Update player reputation with a trading post
        /// </summary>
        public void UpdateReputation(TradingPost tradingPost, float delta, string reason = "")
        {
            if (_tradingPostStates.TryGetValue(tradingPost, out var state))
            {
                float oldReputation = state.ReputationWithPlayer;
                state.ReputationWithPlayer = Mathf.Clamp01(state.ReputationWithPlayer + delta);
                
                if (Mathf.Abs(oldReputation - state.ReputationWithPlayer) > 0.001f)
                {
                    OnReputationChanged?.Invoke(tradingPost, state.ReputationWithPlayer);
                    LogDebug($"Reputation with {tradingPost.TradingPostName}: {oldReputation:F2} -> {state.ReputationWithPlayer:F2} ({reason})");
                }
            }
        }
        
        /// <summary>
        /// Get player reputation with a trading post
        /// </summary>
        public float GetReputation(TradingPost tradingPost)
        {
            if (_tradingPostStates.TryGetValue(tradingPost, out var state))
            {
                return state.ReputationWithPlayer;
            }
            
            return 0.5f; // Default neutral reputation
        }
        
        /// <summary>
        /// Process successful transaction reputation bonus
        /// </summary>
        public void ProcessTransactionReputation(TradingPost tradingPost, float transactionValue, float qualityScore, bool onTime)
        {
            float reputationChange = 0f;
            
            // Base bonus for completing transaction
            reputationChange += 0.01f;
            
            // Quality bonus
            if (qualityScore > 0.8f)
                reputationChange += 0.02f;
            else if (qualityScore < 0.5f)
                reputationChange -= 0.01f;
            
            // On-time delivery bonus
            if (onTime)
                reputationChange += 0.01f;
            else
                reputationChange -= 0.02f;
            
            // Large transaction bonus
            if (transactionValue > 1000f)
                reputationChange += 0.01f;
            
            UpdateReputation(tradingPost, reputationChange, "Transaction completed");
        }
        
        #endregion
        
        #region Trading Post Queries
        
        /// <summary>
        /// Find trading posts that accept a specific product category
        /// </summary>
        public List<TradingPost> FindTradingPostsForProduct(ProductCategory category)
        {
            string categoryString = category.ToString();
            return _availableTradingPosts
                .Where(post => post.AcceptedProductTypes.Contains(categoryString))
                .Where(post => _tradingPostStates.TryGetValue(post, out var state) && state.IsActive)
                .ToList();
        }
        
        /// <summary>
        /// Get trading posts sorted by reputation
        /// </summary>
        public List<TradingPost> GetTradingPostsByReputation(bool ascending = false)
        {
            var query = _availableTradingPosts
                .Where(post => _tradingPostStates.TryGetValue(post, out var state) && state.IsActive);
            
            if (ascending)
            {
                return query.OrderBy(post => GetReputation(post)).ToList();
            }
            else
            {
                return query.OrderByDescending(post => GetReputation(post)).ToList();
            }
        }
        
        /// <summary>
        /// Get best trading post for selling a product (highest commission multiplier + reputation)
        /// </summary>
        public TradingPost GetBestTradingPostForSale(ProductCategory category)
        {
            return FindTradingPostsForProduct(category)
                .OrderByDescending(post =>
                {
                    var multiplier = GetCommissionMultiplier(post);
                    var reputation = GetReputation(post);
                    return multiplier * 0.7f + reputation * 0.3f; // Weighted score
                })
                .FirstOrDefault();
        }
        
        /// <summary>
        /// Get best trading post for buying (lowest markup + reputation)
        /// </summary>
        public TradingPost GetBestTradingPostForPurchase(ProductCategory category)
        {
            return FindTradingPostsForProduct(category)
                .OrderBy(post =>
                {
                    var markup = GetPriceMarkup(post);
                    var reputation = GetReputation(post);
                    return markup - (reputation * 0.1f); // Lower is better, reputation reduces effective markup
                })
                .FirstOrDefault();
        }
        
        #endregion
        
        private void LogDebug(string message)
        {
            if (_enableTradingPostLogging)
                Debug.Log($"[TradingPostManager] {message}");
        }
    }
}