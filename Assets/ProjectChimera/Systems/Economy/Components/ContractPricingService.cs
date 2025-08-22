using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Economy;
using ProjectChimera.Data.Shared;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ProjectChimera.Systems.Economy.Components
{
    /// <summary>
    /// Handles advanced pricing algorithms and market dynamics for Project Chimera's cannabis contract system.
    /// Manages strain pricing, market trends, balanced generation algorithms, and competitive pricing factors.
    /// </summary>
    public class ContractPricingService : MonoBehaviour
    {
        [Header("Market Configuration")]
        [SerializeField] private bool _enableDebugLogging = false;
        
        // Market dynamics parameters
        private float _marketVolatility = 0.15f;
        private int _marketTrendCycleDays = 7;
        private bool _enableSeasonalAdjustments = true;
        private Vector2 _competitionFactorRange = new Vector2(0.85f, 1.25f);
        
        // Advanced generation parameters
        private bool _enableBalancedGeneration = true;
        private float _strainVarietyWeight = 0.3f;
        private float _difficultyDistributionWeight = 0.4f;
        private float _valueBalanceWeight = 0.3f;
        private AnimationCurve _difficultyProgressionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        // Market state
        private Dictionary<StrainType, float> _strainMarketPrices = new Dictionary<StrainType, float>();
        private Dictionary<ContractDifficultyTier, int> _difficultyDistribution = new Dictionary<ContractDifficultyTier, int>();
        private Queue<ContractGenerationRequest> _balancedGenerationQueue = new Queue<ContractGenerationRequest>();
        private float _globalMarketTrend = 1f;
        private int _currentMarketCycle = 0;
        private DateTime _serviceStartTime;
        
        // Contract pool parameters
        private int _contractPoolSize = 10;
        private float _playerDifficultyLevel = 1f;
        
        // Demand tracking
        private Dictionary<StrainType, int> _strainDemandCounts = new Dictionary<StrainType, int>();
        
        // Events
        public System.Action<float> OnGlobalMarketTrendChanged;
        public System.Action<StrainType, float> OnStrainPriceChanged;
        public System.Action<ContractGenerationRequest> OnBalancedRequestGenerated;
        
        // Properties
        public float GlobalMarketTrend => _globalMarketTrend;
        public Dictionary<StrainType, float> StrainMarketPrices => new Dictionary<StrainType, float>(_strainMarketPrices);
        public int BalancedQueueCount => _balancedGenerationQueue.Count;
        public int CurrentMarketCycle => _currentMarketCycle;
        
        public void Initialize(float marketVolatility, int marketTrendCycleDays, bool enableSeasonalAdjustments,
            Vector2 competitionFactorRange, bool enableBalancedGeneration, float strainVarietyWeight,
            float difficultyDistributionWeight, float valueBalanceWeight, AnimationCurve difficultyProgressionCurve,
            int contractPoolSize)
        {
            _marketVolatility = marketVolatility;
            _marketTrendCycleDays = marketTrendCycleDays;
            _enableSeasonalAdjustments = enableSeasonalAdjustments;
            _competitionFactorRange = competitionFactorRange;
            _enableBalancedGeneration = enableBalancedGeneration;
            _strainVarietyWeight = strainVarietyWeight;
            _difficultyDistributionWeight = difficultyDistributionWeight;
            _valueBalanceWeight = valueBalanceWeight;
            _difficultyProgressionCurve = difficultyProgressionCurve;
            _contractPoolSize = contractPoolSize;
            
            InitializeMarketData();
            
            LogInfo("Contract pricing service initialized for cannabis cultivation market dynamics");
        }
        
        /// <summary>
        /// Update market trends and pricing cycles
        /// </summary>
        public void UpdateMarketTrends()
        {
            int daysSinceStart = (int)(DateTime.Now - _serviceStartTime).TotalDays;
            int newCycle = daysSinceStart / _marketTrendCycleDays;
            
            if (newCycle != _currentMarketCycle)
            {
                _currentMarketCycle = newCycle;
                
                // Generate new market trend
                float trendVariation = UnityEngine.Random.Range(-_marketVolatility, _marketVolatility);
                float newTrend = Mathf.Clamp(1f + trendVariation, 0.7f, 1.4f);
                
                if (Mathf.Abs(newTrend - _globalMarketTrend) > 0.01f)
                {
                    _globalMarketTrend = newTrend;
                    OnGlobalMarketTrendChanged?.Invoke(_globalMarketTrend);
                }
                
                // Apply seasonal adjustments if enabled
                if (_enableSeasonalAdjustments)
                {
                    ApplySeasonalAdjustments();
                }
                
                LogInfo($"Market cycle updated: Cycle {_currentMarketCycle}, Trend: {_globalMarketTrend:F2}");
            }
        }
        
        /// <summary>
        /// Update strain pricing based on market dynamics
        /// </summary>
        public void UpdateStrainPricing()
        {
            var strainsToUpdate = new List<StrainType>(_strainMarketPrices.Keys);
            
            foreach (var strain in strainsToUpdate)
            {
                float currentPrice = _strainMarketPrices[strain];
                
                // Calculate supply/demand ratio
                int demand = _strainDemandCounts.GetValueOrDefault(strain, 0);
                int totalDemand = 0;
                foreach (var count in _strainDemandCounts.Values)
                    totalDemand += count;
                
                float demandRatio = totalDemand > 0 ? (float)demand / totalDemand : 0f;
                
                // Price adjustment based on demand
                float targetPrice = 1f;
                if (demandRatio > 0.3f) // High demand
                {
                    targetPrice = Mathf.Lerp(1f, 1.3f, (demandRatio - 0.3f) / 0.4f);
                }
                else if (demandRatio < 0.15f) // Low demand
                {
                    targetPrice = Mathf.Lerp(0.8f, 1f, demandRatio / 0.15f);
                }
                
                // Apply global market trend
                targetPrice *= _globalMarketTrend;
                
                // Smooth price transitions
                float priceChangeRate = 0.1f; // 10% max change per update
                float newPrice = Mathf.Lerp(currentPrice, targetPrice, priceChangeRate);
                newPrice = Mathf.Clamp(newPrice, 0.6f, 1.8f);
                
                if (Mathf.Abs(newPrice - currentPrice) > 0.01f)
                {
                    _strainMarketPrices[strain] = newPrice;
                    OnStrainPriceChanged?.Invoke(strain, newPrice);
                }
            }
        }
        
        /// <summary>
        /// Apply market pricing to contract parameters
        /// </summary>
        public void ApplyMarketPricing(ContractGenerationParameters parameters)
        {
            if (parameters == null) return;
            
            // Apply strain-specific pricing
            parameters.ContractValue *= _strainMarketPrices.GetValueOrDefault(parameters.StrainType, 1f);
            
            // Apply global market trend
            parameters.ContractValue *= _globalMarketTrend;
            
            // Apply competition factor
            float competitionFactor = UnityEngine.Random.Range(_competitionFactorRange.x, _competitionFactorRange.y);
            parameters.ContractValue *= competitionFactor;
            
            LogInfo($"Applied market pricing: Strain {_strainMarketPrices.GetValueOrDefault(parameters.StrainType, 1f):F2}x, " +
                   $"Global {_globalMarketTrend:F2}x, Competition {competitionFactor:F2}x");
        }
        
        /// <summary>
        /// Build a balanced queue of contract generation requests
        /// </summary>
        public void BuildBalancedGenerationQueue()
        {
            if (!_enableBalancedGeneration) return;
            
            _balancedGenerationQueue.Clear();
            
            // Calculate desired strain distribution
            var strainTypes = Enum.GetValues(typeof(StrainType)).Cast<StrainType>().ToList();
            var desiredDistribution = CalculateDesiredStrainDistribution(strainTypes);
            
            // Calculate desired difficulty distribution
            var difficultyTiers = Enum.GetValues(typeof(ContractDifficultyTier)).Cast<ContractDifficultyTier>().ToList();
            var desiredDifficultyDistribution = CalculateDesiredDifficultyDistribution(difficultyTiers);
            
            // Generate balanced requests
            int totalRequests = _contractPoolSize * 2; // Generate extra for variety
            
            for (int i = 0; i < totalRequests; i++)
            {
                var strain = SelectBalancedStrain(strainTypes, desiredDistribution);
                var difficulty = SelectBalancedDifficulty(difficultyTiers, desiredDifficultyDistribution);
                var request = new ContractGenerationRequest
                {
                    PreferredStrain = strain,
                    TargetDifficulty = difficulty,
                    Priority = CalculateGenerationPriority(strain, difficulty),
                    GenerationTime = DateTime.Now.AddHours(UnityEngine.Random.Range(0f, 24f))
                };
                
                _balancedGenerationQueue.Enqueue(request);
                OnBalancedRequestGenerated?.Invoke(request);
            }
            
            LogInfo($"Built balanced generation queue with {_balancedGenerationQueue.Count} requests");
        }
        
        /// <summary>
        /// Process the balanced generation queue
        /// </summary>
        public List<ContractGenerationRequest> ProcessBalancedGenerationQueue(int maxProcessPerUpdate = 2)
        {
            var processableRequests = new List<ContractGenerationRequest>();
            
            if (_balancedGenerationQueue.Count == 0)
            {
                BuildBalancedGenerationQueue(); // Rebuild when empty
                return processableRequests;
            }
            
            // Process high-priority requests
            while (_balancedGenerationQueue.Count > 0 && processableRequests.Count < maxProcessPerUpdate)
            {
                var request = _balancedGenerationQueue.Dequeue();
                if (request.GenerationTime <= DateTime.Now)
                {
                    processableRequests.Add(request);
                }
                else
                {
                    _balancedGenerationQueue.Enqueue(request); // Re-queue for later
                    break;
                }
            }
            
            return processableRequests;
        }
        
        /// <summary>
        /// Update difficulty distribution tracking
        /// </summary>
        public void UpdateDifficultyDistribution(ContractDifficultyTier tier)
        {
            _difficultyDistribution[tier]++;
        }
        
        /// <summary>
        /// Update strain demand tracking
        /// </summary>
        public void UpdateStrainDemand(StrainType strain)
        {
            _strainDemandCounts[strain]++;
        }
        
        /// <summary>
        /// Set player difficulty level for balanced generation
        /// </summary>
        public void SetPlayerDifficultyLevel(float level)
        {
            _playerDifficultyLevel = Mathf.Clamp(level, 1f, 5f);
        }
        
        /// <summary>
        /// Get market analysis data
        /// </summary>
        public MarketAnalysisData GetMarketAnalysis()
        {
            return new MarketAnalysisData
            {
                GlobalMarketTrend = _globalMarketTrend,
                CurrentMarketCycle = _currentMarketCycle,
                StrainMarketPrices = new Dictionary<StrainType, float>(_strainMarketPrices),
                DifficultyDistribution = new Dictionary<ContractDifficultyTier, int>(_difficultyDistribution),
                StrainDemandCounts = new Dictionary<StrainType, int>(_strainDemandCounts),
                QueuedRequests = _balancedGenerationQueue.Count
            };
        }
        
        private void InitializeMarketData()
        {
            _serviceStartTime = DateTime.Now;
            _currentMarketCycle = 0;
            _globalMarketTrend = 1f;
            
            // Initialize strain market prices
            _strainMarketPrices.Clear();
            foreach (StrainType strainType in Enum.GetValues(typeof(StrainType)))
            {
                _strainMarketPrices[strainType] = 1f; // Base multiplier
            }
            
            // Initialize difficulty distribution tracking
            _difficultyDistribution.Clear();
            foreach (ContractDifficultyTier tier in Enum.GetValues(typeof(ContractDifficultyTier)))
            {
                _difficultyDistribution[tier] = 0;
            }
            
            // Initialize strain demand tracking
            _strainDemandCounts.Clear();
            foreach (StrainType strainType in Enum.GetValues(typeof(StrainType)))
            {
                _strainDemandCounts[strainType] = 0;
            }
            
            // Build initial balanced generation queue
            if (_enableBalancedGeneration)
            {
                BuildBalancedGenerationQueue();
            }
        }
        
        private Dictionary<StrainType, float> CalculateDesiredStrainDistribution(List<StrainType> strains)
        {
            var distribution = new Dictionary<StrainType, float>();
            
            foreach (var strain in strains)
            {
                float baseWeight = 1f / strains.Count; // Equal base distribution
                
                // Adjust based on current demand (inverse relationship for balance)
                int currentDemand = _strainDemandCounts.GetValueOrDefault(strain, 0);
                int totalDemand = 0;
                foreach (var count in _strainDemandCounts.Values)
                    totalDemand += count;
                
                float demandRatio = totalDemand > 0 ? (float)currentDemand / totalDemand : 0f;
                
                // Reduce weight for oversupplied strains, increase for undersupplied
                float balanceAdjustment = Mathf.Lerp(1.5f, 0.5f, demandRatio);
                
                // Apply market price influence
                float priceMultiplier = _strainMarketPrices.GetValueOrDefault(strain, 1f);
                float priceAdjustment = Mathf.Lerp(0.8f, 1.2f, priceMultiplier);
                
                distribution[strain] = baseWeight * balanceAdjustment * priceAdjustment * _strainVarietyWeight;
            }
            
            // Normalize distribution
            float totalWeight = 0f;
            foreach (var weight in distribution.Values)
                totalWeight += weight;
            
            var normalizedDistribution = new Dictionary<StrainType, float>();
            foreach (var kvp in distribution)
            {
                normalizedDistribution[kvp.Key] = kvp.Value / totalWeight;
            }
            
            return normalizedDistribution;
        }
        
        private Dictionary<ContractDifficultyTier, float> CalculateDesiredDifficultyDistribution(List<ContractDifficultyTier> tiers)
        {
            var distribution = new Dictionary<ContractDifficultyTier, float>();
            
            foreach (var tier in tiers)
            {
                float progressionValue = (float)(int)tier / (tiers.Count - 1); // 0 to 1
                float curveValue = _difficultyProgressionCurve.Evaluate(progressionValue);
                
                // Adjust based on player difficulty level
                float playerAdjustment = Mathf.Lerp(0.1f, 1f, (_playerDifficultyLevel - 1f) / 4f);
                
                // Current difficulty distribution balance
                int currentCount = _difficultyDistribution.GetValueOrDefault(tier, 0);
                int totalCount = 0;
                foreach (var count in _difficultyDistribution.Values)
                    totalCount += count;
                
                float currentRatio = totalCount > 0 ? (float)currentCount / totalCount : 0f;
                
                // Balance adjustment (favor underrepresented difficulties)
                float balanceAdjustment = Mathf.Lerp(1.5f, 0.5f, currentRatio);
                
                distribution[tier] = curveValue * playerAdjustment * balanceAdjustment * _difficultyDistributionWeight;
            }
            
            // Normalize distribution
            float totalWeight = 0f;
            foreach (var weight in distribution.Values)
                totalWeight += weight;
            
            var normalizedDistribution = new Dictionary<ContractDifficultyTier, float>();
            foreach (var kvp in distribution)
            {
                normalizedDistribution[kvp.Key] = kvp.Value / totalWeight;
            }
            
            return normalizedDistribution;
        }
        
        private StrainType SelectBalancedStrain(List<StrainType> strains, Dictionary<StrainType, float> distribution)
        {
            float random = UnityEngine.Random.value;
            float cumulative = 0f;
            
            foreach (var strain in strains)
            {
                cumulative += distribution.GetValueOrDefault(strain, 0f);
                if (random <= cumulative)
                {
                    return strain;
                }
            }
            
            // Fallback to random selection
            return strains[UnityEngine.Random.Range(0, strains.Count)];
        }
        
        private ContractDifficultyTier SelectBalancedDifficulty(List<ContractDifficultyTier> tiers, Dictionary<ContractDifficultyTier, float> distribution)
        {
            float random = UnityEngine.Random.value;
            float cumulative = 0f;
            
            foreach (var tier in tiers)
            {
                cumulative += distribution.GetValueOrDefault(tier, 0f);
                if (random <= cumulative)
                {
                    return tier;
                }
            }
            
            // Fallback to random selection
            return tiers[UnityEngine.Random.Range(0, tiers.Count)];
        }
        
        private float CalculateGenerationPriority(StrainType strain, ContractDifficultyTier difficulty)
        {
            float priority = 1f;
            
            // Strain rarity adjustment
            int strainDemand = _strainDemandCounts.GetValueOrDefault(strain, 0);
            int totalDemand = 0;
            foreach (var count in _strainDemandCounts.Values)
                totalDemand += count;
            
            float strainRarity = totalDemand > 0 ? 1f - ((float)strainDemand / totalDemand) : 1f;
            priority += strainRarity * 0.3f;
            
            // Difficulty balance adjustment
            int difficultyCount = _difficultyDistribution.GetValueOrDefault(difficulty, 0);
            int totalDifficulty = 0;
            foreach (var count in _difficultyDistribution.Values)
                totalDifficulty += count;
            
            float difficultyRarity = totalDifficulty > 0 ? 1f - ((float)difficultyCount / totalDifficulty) : 1f;
            priority += difficultyRarity * 0.4f;
            
            // Market value adjustment
            float marketPrice = _strainMarketPrices.GetValueOrDefault(strain, 1f);
            priority += (marketPrice - 1f) * _valueBalanceWeight;
            
            return Mathf.Clamp(priority, 0.1f, 2f);
        }
        
        private void ApplySeasonalAdjustments()
        {
            int dayOfYear = DateTime.Now.DayOfYear;
            float seasonalFactor = 1f;
            
            // Simple seasonal model (could be enhanced with more complex patterns)
            if (dayOfYear >= 60 && dayOfYear <= 150) // Spring (Mar-May)
            {
                seasonalFactor = 1.1f; // Higher demand in growing season
            }
            else if (dayOfYear >= 270 && dayOfYear <= 330) // Fall (Oct-Nov)
            {
                seasonalFactor = 1.15f; // Peak harvest season
            }
            else if (dayOfYear >= 330 || dayOfYear <= 60) // Winter (Dec-Feb)
            {
                seasonalFactor = 0.9f; // Lower activity in winter
            }
            
            _globalMarketTrend *= seasonalFactor;
            LogInfo($"Applied seasonal adjustment: {seasonalFactor:F2}");
        }
        
        private void LogInfo(string message)
        {
            if (_enableDebugLogging)
                Debug.Log($"[ContractPricingService] {message}");
        }
    }
    
    /// <summary>
    /// Market analysis data for cannabis cultivation contracts
    /// </summary>
    [System.Serializable]
    public class MarketAnalysisData
    {
        public float GlobalMarketTrend;
        public int CurrentMarketCycle;
        public Dictionary<StrainType, float> StrainMarketPrices;
        public Dictionary<ContractDifficultyTier, int> DifficultyDistribution;
        public Dictionary<StrainType, int> StrainDemandCounts;
        public int QueuedRequests;
    }
}