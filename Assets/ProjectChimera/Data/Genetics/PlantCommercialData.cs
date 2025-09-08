using UnityEngine;
using ProjectChimera.Shared;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Genetics
{
    /// <summary>
    /// Contains commercial and market-related data for cannabis strains including pricing, 
    /// availability, and market demand. Separated from PlantStrainSO to follow 
    /// Single Responsibility Principle.
    /// </summary>
    [CreateAssetMenu(fileName = "New Plant Commercial Data", menuName = "Project Chimera/Genetics/Plant Commercial Data", order = 12)]
    public class PlantCommercialData : ChimeraDataSO
    {
        [Header("Market Pricing")]
        [SerializeField, Range(0f, 100f)] private float _marketValue = 10f; // per gram
        [SerializeField, Range(0f, 1f)] private float _marketDemand = 0.5f;
        [SerializeField, Range(0f, 50f)] private float _seedPrice = 15f;
        [SerializeField, Range(0f, 100f)] private float _clonePrice = 25f;

        [Header("Availability")]
        [SerializeField] private bool _seedsAvailable = true;
        [SerializeField] private bool _clonesAvailable = false;
        [SerializeField] private bool _tissueCultureAvailable = false;
        [SerializeField, Range(0, 1000)] private int _seedStockQuantity = 100;
        [SerializeField, Range(0, 100)] private int _cloneStockQuantity = 0;

        [Header("Market Position")]
        [SerializeField] private StrainRarity _marketRarity = StrainRarity.Common;
        [SerializeField] private MarketSegment _targetMarket = MarketSegment.Recreational;
        [SerializeField, Range(0f, 1f)] private float _brandRecognition = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _customerLoyalty = 0.5f;

        [Header("Commercial Properties")]
        [SerializeField] private bool _commercialViability = true;
        [SerializeField] private bool _boutiqueMaterial = false;
        [SerializeField] private bool _massProductionSuitable = true;
        [SerializeField, Range(0f, 1f)] private float _shelfStability = 0.7f;
        [SerializeField, Range(0f, 1f)] private float _trimAppeal = 0.7f;

        [Header("Distribution")]
        [SerializeField] private bool _dispensaryReady = true;
        [SerializeField] private bool _processingGrade = false;
        [SerializeField] private bool _extractionGrade = false;
        [SerializeField] private bool _premiumGrade = false;

        [Header("Market Trends")]
        [SerializeField, Range(-1f, 1f)] private float _demandTrend = 0f; // -1 declining, 0 stable, 1 rising
        [SerializeField, Range(-1f, 1f)] private float _priceTrend = 0f;
        [SerializeField] private SeasonalDemand _seasonalPattern = SeasonalDemand.Stable;

        [Header("Competitive Analysis")]
        [SerializeField] private int _marketCompetitors = 3;
        [SerializeField, Range(0f, 1f)] private float _marketShare = 0.1f;
        [SerializeField, Range(0f, 1f)] private float _profitMargin = 0.3f;

        // Public Properties
        public float MarketValue => _marketValue;
        public float MarketDemand => _marketDemand;
        public float SeedPrice => _seedPrice;
        public float ClonePrice => _clonePrice;

        // Availability
        public bool SeedsAvailable => _seedsAvailable;
        public bool ClonesAvailable => _clonesAvailable;
        public bool TissueCultureAvailable => _tissueCultureAvailable;
        public int SeedStockQuantity => _seedStockQuantity;
        public int CloneStockQuantity => _cloneStockQuantity;

        // Market Position
        public StrainRarity MarketRarity => _marketRarity;
        public MarketSegment TargetMarket => _targetMarket;
        public float BrandRecognition => _brandRecognition;
        public float CustomerLoyalty => _customerLoyalty;

        // Commercial Properties
        public bool CommercialViability => _commercialViability;
        public bool BoutiqueMaterial => _boutiqueMaterial;
        public bool MassProductionSuitable => _massProductionSuitable;
        public float ShelfStability => _shelfStability;
        public float TrimAppeal => _trimAppeal;

        // Distribution
        public bool DispensaryReady => _dispensaryReady;
        public bool ProcessingGrade => _processingGrade;
        public bool ExtractionGrade => _extractionGrade;
        public bool PremiumGrade => _premiumGrade;

        // Market Trends
        public float DemandTrend => _demandTrend;
        public float PriceTrend => _priceTrend;
        public SeasonalDemand SeasonalPattern => _seasonalPattern;

        // Competitive Analysis
        public int MarketCompetitors => _marketCompetitors;
        public float MarketShare => _marketShare;
        public float ProfitMargin => _profitMargin;

        /// <summary>
        /// Calculates current market price based on demand, trends, and rarity.
        /// </summary>
        public float GetCurrentMarketPrice()
        {
            float basePrice = _marketValue;
            
            // Apply demand multiplier
            float demandMultiplier = 1f + (_marketDemand - 0.5f) * 0.5f;
            
            // Apply rarity multiplier
            float rarityMultiplier = GetRarityMultiplier(_marketRarity);
            
            // Apply trend adjustments
            float trendMultiplier = 1f + _priceTrend * 0.2f;
            
            // Apply seasonal adjustments
            float seasonalMultiplier = GetSeasonalMultiplier();
            
            return basePrice * demandMultiplier * rarityMultiplier * trendMultiplier * seasonalMultiplier;
        }

        /// <summary>
        /// Determines if strain is currently in stock and purchasable.
        /// </summary>
        public bool IsInStock(string productType)
        {
            switch (productType.ToLower())
            {
                case "seeds":
                    return _seedsAvailable && _seedStockQuantity > 0;
                case "clones":
                    return _clonesAvailable && _cloneStockQuantity > 0;
                case "tissue":
                    return _tissueCultureAvailable;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Calculates the total commercial value score for this strain.
        /// </summary>
        public float GetCommercialValueScore()
        {
            float score = 0f;
            
            // Market factors (40%)
            score += _marketDemand * 0.2f;
            score += (1f - _marketShare) * 0.1f; // Lower market share = higher potential
            score += _brandRecognition * 0.1f;
            
            // Production factors (30%)
            score += (_massProductionSuitable ? 0.15f : 0f);
            score += _shelfStability * 0.1f;
            score += _trimAppeal * 0.05f;
            
            // Distribution factors (20%)
            score += (_dispensaryReady ? 0.1f : 0f);
            score += (_premiumGrade ? 0.1f : 0f);
            
            // Financial factors (10%)
            score += _profitMargin * 0.1f;
            
            return Mathf.Clamp01(score);
        }

        private float GetRarityMultiplier(StrainRarity rarity)
        {
            return rarity switch
            {
                StrainRarity.Common => 1f,
                StrainRarity.Uncommon => 1.2f,
                StrainRarity.Rare => 1.5f,
                StrainRarity.Epic => 2f,
                StrainRarity.Legendary => 3f,
                StrainRarity.Custom => 1.5f,
                _ => 1f
            };
        }

        private float GetSeasonalMultiplier()
        {
            // Simple seasonal calculation - could be enhanced with actual date/season checking
            return _seasonalPattern switch
            {
                SeasonalDemand.SpringPeak => 1.2f,
                SeasonalDemand.SummerPeak => 1.1f,
                SeasonalDemand.FallPeak => 1.3f,
                SeasonalDemand.WinterPeak => 1.1f,
                SeasonalDemand.Stable => 1f,
                _ => 1f
            };
        }

        public override bool ValidateData()
        {
            bool isValid = base.ValidateData();

            if (_marketValue <= 0)
            {
                SharedLogger.LogWarning($"[Chimera] PlantCommercialData '{DisplayName}' has invalid market value.");
                isValid = false;
            }

            if (_seedsAvailable && _seedPrice <= 0)
            {
                SharedLogger.LogWarning($"[Chimera] PlantCommercialData '{DisplayName}' has seeds available but invalid seed price.");
                isValid = false;
            }

            if (_clonesAvailable && _clonePrice <= 0)
            {
                SharedLogger.LogWarning($"[Chimera] PlantCommercialData '{DisplayName}' has clones available but invalid clone price.");
                isValid = false;
            }

            return isValid;
        }
    }

    /// <summary>
    /// Defines the target market segment for a cannabis strain.
    /// </summary>
    public enum MarketSegment
    {
        Recreational,
        Medical,
        Both,
        Industrial,
        Research,
        Premium,
        Budget
    }

    /// <summary>
    /// Defines seasonal demand patterns for market analysis.
    /// </summary>
    public enum SeasonalDemand
    {
        Stable,
        SpringPeak,
        SummerPeak,
        FallPeak,
        WinterPeak
    }
}