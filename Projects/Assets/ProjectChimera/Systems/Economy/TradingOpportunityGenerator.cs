using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Data.Economy;
using ProjectChimera.Data.Economy.Trading;
using TradingTransactionType = ProjectChimera.Data.Economy.TradingTransactionType;
using ProjectChimera.Core;

// Add placeholder for missing manager references
namespace ProjectChimera.Systems.Economy
{
    /// <summary>
    /// Generates and manages trading opportunities, market analysis, and recommendations.
    /// Extracted from TradingManager for modular architecture.
    /// Handles opportunity discovery, profitability analysis, and market intelligence.
    /// </summary>
    public class TradingOpportunityGenerator : MonoBehaviour
    {
        [Header("Opportunity Generation Configuration")]
        [SerializeField] private bool _enableOpportunityLogging = true;
        [SerializeField] private float _opportunityGenerationInterval = 8f; // Check every 8 game hours
        [SerializeField] private int _maxActiveOpportunities = 10;
        [SerializeField] private float _baseOpportunityChance = 0.1f;
        
        // Dependencies
        private TradingInventoryManager _inventoryManager;
        private PlayerReputation _playerReputation;
        
        // Settings
        public bool IncludeModified = true;
        
        // Opportunity data
        private List<TradingOpportunity> _availableOpportunities = new List<TradingOpportunity>();
        private float _lastOpportunityCheck = 0f;
        
        // Events
        public System.Action<TradingOpportunity> OnTradingOpportunityAvailable;
        public System.Action<TradingOpportunity> OnTradingOpportunityExpired;
        public System.Action<TradingProfitabilityAnalysis> OnAnalysisCompleted;
        
        // Properties
        public List<TradingOpportunity> AvailableOpportunities => _availableOpportunities.ToList();
        public int ActiveOpportunityCount => _availableOpportunities.Count;
        
        /// <summary>
        /// Initialize opportunity generator with dependencies
        /// </summary>
        public void Initialize(TradingInventoryManager inventoryManager, PlayerReputation playerReputation)
        {
            _inventoryManager = inventoryManager;
            _playerReputation = playerReputation;
            
            LogDebug("Trading opportunity generator initialized");
        }
        
        private void Update()
        {
            var timeManager = GameManager.Instance?.GetManager<TimeManager>();
            float gameTimeDelta = timeManager?.GetScaledDeltaTime() ?? Time.deltaTime;
            
            _lastOpportunityCheck += gameTimeDelta;
            
            if (_lastOpportunityCheck >= _opportunityGenerationInterval)
            {
                UpdateTradingOpportunities();
                _lastOpportunityCheck = 0f;
            }
        }
        
        #region Opportunity Management
        
        /// <summary>
        /// Update trading opportunities - remove expired and generate new ones
        /// </summary>
        private void UpdateTradingOpportunities()
        {
            // Remove expired opportunities
            var expiredOpportunities = _availableOpportunities
                .Where(op => System.DateTime.Now > op.ExpirationDate)
                .ToList();
            
            foreach (var expired in expiredOpportunities)
            {
                _availableOpportunities.Remove(expired);
                OnTradingOpportunityExpired?.Invoke(expired);
                LogDebug($"Opportunity expired: {expired.Description}");
            }
            
            // Generate new opportunities if under limit
            if (_availableOpportunities.Count < _maxActiveOpportunities)
            {
                float adjustedChance = _baseOpportunityChance * GetReputationMultiplier();
                
                if (Random.Range(0f, 1f) < adjustedChance)
                {
                    var newOpportunity = GenerateRandomTradingOpportunity();
                    if (newOpportunity != null)
                    {
                        _availableOpportunities.Add(newOpportunity);
                        OnTradingOpportunityAvailable?.Invoke(newOpportunity);
                        LogDebug($"New opportunity generated: {newOpportunity.Description}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Get opportunities filtered by type
        /// </summary>
        public List<TradingOpportunity> GetTradingOpportunities(OpportunityType opportunityType = OpportunityType.All)
        {
            if (opportunityType == OpportunityType.All)
                return _availableOpportunities.ToList();
            
            return _availableOpportunities.Where(op => op.OpportunityType == opportunityType).ToList();
        }
        
        /// <summary>
        /// Get opportunities that player can actually use (based on reputation, resources, etc.)
        /// </summary>
        public List<TradingOpportunity> GetViableOpportunities()
        {
            return _availableOpportunities
                .Where(op => _playerReputation.OverallReputation >= op.RequiredReputationLevel)
                .Where(op => IsOpportunityViable(op))
                .OrderByDescending(op => CalculateOpportunityScore(op))
                .ToList();
        }
        
        /// <summary>
        /// Remove an opportunity (e.g., when player takes it)
        /// </summary>
        public bool ConsumeOpportunity(string opportunityId)
        {
            var opportunity = _availableOpportunities.FirstOrDefault(op => op.OpportunityId == opportunityId);
            if (opportunity != null)
            {
                _availableOpportunities.Remove(opportunity);
                LogDebug($"Opportunity consumed: {opportunity.Description}");
                return true;
            }
            return false;
        }
        
        #endregion
        
        #region Opportunity Generation
        
        /// <summary>
        /// Generate a random trading opportunity
        /// </summary>
        private TradingOpportunity GenerateRandomTradingOpportunity()
        {
            var opportunityTypes = new[] { 
                OpportunityType.Bulk_Discount, 
                OpportunityType.Quality_Premium,
                OpportunityType.Urgent_Sale, 
                OpportunityType.Seasonal_Special,
                OpportunityType.New_Market,
                OpportunityType.Liquidation
            };
            
            var selectedType = opportunityTypes[Random.Range(0, opportunityTypes.Length)];
            
            var opportunity = new TradingOpportunity
            {
                OpportunityId = System.Guid.NewGuid().ToString(),
                OpportunityType = selectedType,
                Description = GenerateOpportunityDescription(selectedType),
                PriceModifier = GeneratePriceModifier(selectedType),
                QuantityAvailable = GenerateQuantityAvailable(selectedType),
                ExpirationDate = GenerateExpirationDate(selectedType),
                DiscoveryDate = System.DateTime.Now,
                RequiredReputationLevel = GenerateReputationRequirement(selectedType)
            };
            
            // Try to assign a relevant product (in full implementation)
            opportunity.Product = null; // Would be assigned based on market analysis
            
            return opportunity;
        }
        
        /// <summary>
        /// Generate opportunity description based on type
        /// </summary>
        private string GenerateOpportunityDescription(OpportunityType opportunityType)
        {
            switch (opportunityType)
            {
                case OpportunityType.Bulk_Discount:
                    return "Large quantity available at significant discount - bulk purchase opportunity";
                case OpportunityType.Quality_Premium:
                    return "Premium quality product available with higher profit margins";
                case OpportunityType.Urgent_Sale:
                    return "Urgent sale required - below market price for quick transaction";
                case OpportunityType.Seasonal_Special:
                    return "Seasonal product with limited availability and high demand";
                case OpportunityType.New_Market:
                    return "New market opening with exclusive supplier opportunities";
                case OpportunityType.Liquidation:
                    return "Business liquidation - inventory must be sold quickly";
                default:
                    return "Special trading opportunity available";
            }
        }
        
        /// <summary>
        /// Generate price modifier based on opportunity type
        /// </summary>
        private float GeneratePriceModifier(OpportunityType opportunityType)
        {
            switch (opportunityType)
            {
                case OpportunityType.Bulk_Discount:
                    return Random.Range(0.6f, 0.8f); // 20-40% discount
                case OpportunityType.Quality_Premium:
                    return Random.Range(1.2f, 1.5f); // 20-50% premium
                case OpportunityType.Urgent_Sale:
                    return Random.Range(0.5f, 0.7f); // 30-50% discount
                case OpportunityType.Seasonal_Special:
                    return Random.Range(1.1f, 1.3f); // 10-30% premium
                case OpportunityType.New_Market:
                    return Random.Range(0.9f, 1.1f); // Near market rate
                case OpportunityType.Liquidation:
                    return Random.Range(0.4f, 0.6f); // 40-60% discount
                default:
                    return Random.Range(0.8f, 1.2f);
            }
        }
        
        /// <summary>
        /// Generate quantity available based on opportunity type
        /// </summary>
        private float GenerateQuantityAvailable(OpportunityType opportunityType)
        {
            switch (opportunityType)
            {
                case OpportunityType.Bulk_Discount:
                    return Random.Range(200f, 1000f); // Large quantities
                case OpportunityType.Quality_Premium:
                    return Random.Range(10f, 50f); // Limited premium stock
                case OpportunityType.Urgent_Sale:
                    return Random.Range(50f, 200f); // Moderate quantities
                case OpportunityType.Seasonal_Special:
                    return Random.Range(25f, 100f); // Limited seasonal stock
                case OpportunityType.New_Market:
                    return Random.Range(100f, 300f); // Market establishment quantities
                case OpportunityType.Liquidation:
                    return Random.Range(500f, 2000f); // Large liquidation stock
                default:
                    return Random.Range(50f, 500f);
            }
        }
        
        /// <summary>
        /// Generate expiration date based on opportunity type
        /// </summary>
        private System.DateTime GenerateExpirationDate(OpportunityType opportunityType)
        {
            switch (opportunityType)
            {
                case OpportunityType.Bulk_Discount:
                    return System.DateTime.Now.AddDays(Random.Range(3, 7)); // 3-7 days
                case OpportunityType.Quality_Premium:
                    return System.DateTime.Now.AddDays(Random.Range(1, 3)); // 1-3 days
                case OpportunityType.Urgent_Sale:
                    return System.DateTime.Now.AddHours(Random.Range(12, 48)); // 12-48 hours
                case OpportunityType.Seasonal_Special:
                    return System.DateTime.Now.AddDays(Random.Range(7, 14)); // 1-2 weeks
                case OpportunityType.New_Market:
                    return System.DateTime.Now.AddDays(Random.Range(5, 10)); // 5-10 days
                case OpportunityType.Liquidation:
                    return System.DateTime.Now.AddDays(Random.Range(1, 5)); // 1-5 days
                default:
                    return System.DateTime.Now.AddDays(Random.Range(1, 7));
            }
        }
        
        /// <summary>
        /// Generate reputation requirement based on opportunity type
        /// </summary>
        private float GenerateReputationRequirement(OpportunityType opportunityType)
        {
            switch (opportunityType)
            {
                case OpportunityType.Bulk_Discount:
                    return Random.Range(0.4f, 0.6f); // Moderate reputation needed
                case OpportunityType.Quality_Premium:
                    return Random.Range(0.6f, 0.8f); // High reputation for premium deals
                case OpportunityType.Urgent_Sale:
                    return Random.Range(0.2f, 0.4f); // Low reputation - desperate seller
                case OpportunityType.Seasonal_Special:
                    return Random.Range(0.5f, 0.7f); // Good reputation for seasonal access
                case OpportunityType.New_Market:
                    return Random.Range(0.7f, 0.9f); // Very high reputation for new markets
                case OpportunityType.Liquidation:
                    return Random.Range(0.1f, 0.3f); // Very low - liquidators need quick sales
                default:
                    return Random.Range(0.3f, 0.7f);
            }
        }
        
        #endregion
        
        #region Profitability Analysis
        
        /// <summary>
        /// Analyze profitability of a potential transaction
        /// </summary>
        public TradingProfitabilityAnalysis AnalyzeProfitability(MarketProductSO product, float quantity, TradingTransactionType transactionType)
        {
            var analysis = new TradingProfitabilityAnalysis
            {
                Product = product,
                Quantity = quantity,
                TransactionType = transactionType,
                IsAnalysisValid = false
            };
            
            var marketManager = GameManager.Instance.GetManager<MarketManager>();
            if (marketManager == null)
            {
                analysis.RecommendationReason = "Market system unavailable";
                return analysis;
            }
            
            try
            {
                if (transactionType == TradingTransactionType.Purchase)
                {
                    analysis = AnalyzePurchaseOpportunity(product, quantity, marketManager);
                }
                else
                {
                    analysis = AnalyzeSaleOpportunity(product, quantity, marketManager);
                }
                
                analysis.RecommendationScore = CalculateRecommendationScore(analysis);
                analysis.IsAnalysisValid = true;
                
                OnAnalysisCompleted?.Invoke(analysis);
            }
            catch (System.Exception ex)
            {
                LogDebug($"Analysis failed for {product.ProductName}: {ex.Message}");
                analysis.RecommendationReason = $"Analysis error: {ex.Message}";
            }
            
            return analysis;
        }
        
        /// <summary>
        /// Analyze purchase opportunity
        /// </summary>
        private TradingProfitabilityAnalysis AnalyzePurchaseOpportunity(MarketProductSO product, float quantity, MarketManager marketManager)
        {
            var analysis = new TradingProfitabilityAnalysis
            {
                Product = product,
                Quantity = quantity,
                TransactionType = TradingTransactionType.Purchase
            };
            
            float currentPrice = marketManager.GetProductPrice(product.ProductName);
            float buyPrice = currentPrice * quantity;
            
            // Estimate future selling price (could be enhanced with trend analysis)
            float estimatedSellingPrice = currentPrice * 1.1f; // Assume 10% growth
            float estimatedRevenue = estimatedSellingPrice * quantity;
            
            analysis.EstimatedCost = buyPrice;
            analysis.EstimatedRevenue = estimatedRevenue;
            analysis.EstimatedProfit = estimatedRevenue - buyPrice;
            analysis.ProfitMargin = buyPrice > 0 ? (analysis.EstimatedProfit / buyPrice) * 100f : 0f;
            
            // Generate recommendation reason
            if (analysis.ProfitMargin > 20f)
                analysis.RecommendationReason = "Excellent profit potential";
            else if (analysis.ProfitMargin > 10f)
                analysis.RecommendationReason = "Good profit potential";
            else if (analysis.ProfitMargin > 0f)
                analysis.RecommendationReason = "Modest profit potential";
            else
                analysis.RecommendationReason = "Loss expected - not recommended";
            
            return analysis;
        }
        
        /// <summary>
        /// Analyze sale opportunity
        /// </summary>
        private TradingProfitabilityAnalysis AnalyzeSaleOpportunity(MarketProductSO product, float quantity, MarketManager marketManager)
        {
            var analysis = new TradingProfitabilityAnalysis
            {
                Product = product,
                Quantity = quantity,
                TransactionType = TradingTransactionType.Sale
            };
            
            var inventoryItems = _inventoryManager.GetInventoryForProduct(product);
            if (inventoryItems.Count == 0)
            {
                analysis.RecommendationReason = "No inventory available for sale";
                return analysis;
            }
            
            // Calculate average acquisition cost
            float totalCost = inventoryItems.Sum(item => item.AcquisitionCost * item.Quantity);
            float totalQuantity = inventoryItems.Sum(item => item.Quantity);
            float averageCost = totalQuantity > 0 ? totalCost / totalQuantity : 0f;
            
            float currentPrice = marketManager.GetProductPrice(product.ProductName);
            float sellPrice = currentPrice * quantity;
            
            analysis.EstimatedCost = averageCost * quantity;
            analysis.EstimatedRevenue = sellPrice;
            analysis.EstimatedProfit = sellPrice - (averageCost * quantity);
            analysis.ProfitMargin = (averageCost * quantity) > 0 ? (analysis.EstimatedProfit / (averageCost * quantity)) * 100f : 0f;
            
            // Apply quality modifier
            float avgQuality = _inventoryManager.GetAverageQuality(product);
            analysis.EstimatedRevenue *= avgQuality;
            analysis.EstimatedProfit = analysis.EstimatedRevenue - analysis.EstimatedCost;
            
            // Generate recommendation reason
            if (analysis.ProfitMargin > 50f)
                analysis.RecommendationReason = "Excellent selling opportunity";
            else if (analysis.ProfitMargin > 25f)
                analysis.RecommendationReason = "Good selling opportunity";
            else if (analysis.ProfitMargin > 0f)
                analysis.RecommendationReason = "Profitable sale";
            else
                analysis.RecommendationReason = "Sale at loss - consider holding";
            
            return analysis;
        }
        
        /// <summary>
        /// Calculate recommendation score for an analysis
        /// </summary>
        private float CalculateRecommendationScore(TradingProfitabilityAnalysis analysis)
        {
            float score = 0f;
            
            // Profit margin scoring (40% of total score)
            if (analysis.ProfitMargin > 50f) score += 0.4f;
            else if (analysis.ProfitMargin > 25f) score += 0.3f;
            else if (analysis.ProfitMargin > 10f) score += 0.2f;
            else if (analysis.ProfitMargin > 0f) score += 0.1f;
            
            // Volume scoring (20% of total score)
            if (analysis.Quantity > 200f) score += 0.2f;
            else if (analysis.Quantity > 100f) score += 0.15f;
            else if (analysis.Quantity > 50f) score += 0.1f;
            else score += 0.05f;
            
            // Absolute profit scoring (20% of total score)
            if (analysis.EstimatedProfit > 1000f) score += 0.2f;
            else if (analysis.EstimatedProfit > 500f) score += 0.15f;
            else if (analysis.EstimatedProfit > 100f) score += 0.1f;
            else if (analysis.EstimatedProfit > 0f) score += 0.05f;
            
            // Risk assessment (20% of total score)
            if (analysis.EstimatedProfit > 0) score += 0.2f;
            else score -= 0.1f; // Penalty for losses
            
            return Mathf.Clamp01(score);
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Get reputation multiplier for opportunity generation
        /// </summary>
        private float GetReputationMultiplier()
        {
            if (_playerReputation == null) return 1f;
            
            // Higher reputation = more opportunities
            return 0.5f + (_playerReputation.OverallReputation * 1.5f);
        }
        
        /// <summary>
        /// Check if opportunity is viable for player
        /// </summary>
        private bool IsOpportunityViable(TradingOpportunity opportunity)
        {
            // Check reputation requirement
            if (_playerReputation.OverallReputation < opportunity.RequiredReputationLevel)
                return false;
            
            // Check if not expired
            if (System.DateTime.Now > opportunity.ExpirationDate)
                return false;
            
            // Additional viability checks could be added here
            return true;
        }
        
        /// <summary>
        /// Calculate overall opportunity attractiveness score
        /// </summary>
        private float CalculateOpportunityScore(TradingOpportunity opportunity)
        {
            float score = 0f;
            
            // Price modifier scoring
            if (opportunity.PriceModifier < 0.7f) score += 0.4f; // Great discount
            else if (opportunity.PriceModifier < 0.9f) score += 0.2f; // Good discount
            else if (opportunity.PriceModifier > 1.3f) score += 0.3f; // Premium opportunity
            else if (opportunity.PriceModifier > 1.1f) score += 0.1f; // Small premium
            
            // Quantity scoring
            if (opportunity.QuantityAvailable > 500f) score += 0.2f;
            else if (opportunity.QuantityAvailable > 100f) score += 0.1f;
            
            // Time urgency scoring
            var hoursRemaining = (opportunity.ExpirationDate - System.DateTime.Now).TotalHours;
            if (hoursRemaining < 24) score += 0.2f; // Urgent opportunities
            else if (hoursRemaining < 72) score += 0.1f;
            
            // Opportunity type scoring
            switch (opportunity.OpportunityType)
            {
                case OpportunityType.Bulk_Discount:
                case OpportunityType.Liquidation:
                    score += 0.2f; // High value opportunities
                    break;
                case OpportunityType.Quality_Premium:
                case OpportunityType.New_Market:
                    score += 0.15f; // Good opportunities
                    break;
            }
            
            return Mathf.Clamp01(score);
        }
        
        /// <summary>
        /// Clear all opportunities
        /// </summary>
        public void ClearAllOpportunities()
        {
            int count = _availableOpportunities.Count;
            _availableOpportunities.Clear();
            LogDebug($"Cleared {count} trading opportunities");
        }
        
        /// <summary>
        /// Force generate opportunity for testing
        /// </summary>
        public TradingOpportunity ForceGenerateOpportunity(OpportunityType type)
        {
            var opportunity = GenerateRandomTradingOpportunity();
            opportunity.OpportunityType = type;
            opportunity.Description = GenerateOpportunityDescription(type);
            opportunity.PriceModifier = GeneratePriceModifier(type);
            
            _availableOpportunities.Add(opportunity);
            OnTradingOpportunityAvailable?.Invoke(opportunity);
            
            LogDebug($"Force generated opportunity: {type}");
            return opportunity;
        }
        
        #endregion
        
        private void LogDebug(string message)
        {
            if (_enableOpportunityLogging)
                Debug.Log($"[TradingOpportunityGenerator] {message}");
        }
    }
}