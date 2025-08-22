using UnityEngine;
using ProjectChimera.Shared;
using ProjectChimera.Data.Genetics;
using System.Collections.Generic;

namespace ProjectChimera.Data.Economy
{
    /// <summary>
    /// Defines a market product with pricing, demand, and quality characteristics.
    /// Represents items that can be bought, sold, or traded in the cannabis marketplace.
    /// </summary>
    [CreateAssetMenu(fileName = "New Market Product", menuName = "Project Chimera/Economy/Market Product")]
    public class MarketProductSO : ChimeraDataSO
    {
        [Header("Product Identity")]
        [SerializeField] private string _productName;
        [SerializeField] private ProductCategory _category = ProductCategory.Flower;
        [SerializeField] private ProductType _productType = ProductType.Dried_Flower;
        [SerializeField, TextArea(3, 5)] private string _description;
        [SerializeField] private Sprite _productIcon;
        
        [Header("Market Classification")]
        [SerializeField] private MarketTier _marketTier = MarketTier.Premium;
        [SerializeField] private ProductLegalStatus _legalStatus = ProductLegalStatus.Legal_Regulated;
        [SerializeField] private List<MarketSegment> _targetMarkets = new List<MarketSegment>();
        [SerializeField] private ProductLifecycle _lifecycle = ProductLifecycle.Growth;
        
        [Header("Pricing Structure")]
        [SerializeField] private PricingModel _pricingModel;
        [SerializeField] private float _baseWholesalePrice = 10f; // per gram
        [SerializeField] private float _baseRetailPrice = 15f; // per gram
        [SerializeField] private Vector2 _priceVolatility = new Vector2(0.8f, 1.2f); // multiplier range
        [SerializeField] private AnimationCurve _demandPriceCurve;
        
        [Header("Quality Specifications")]
        [SerializeField] private QualityStandards _qualityStandards;
        [SerializeField] private List<QualityMetric> _qualityMetrics = new List<QualityMetric>();
        [SerializeField] private float _qualityPremiumMultiplier = 1.2f;
        [SerializeField] private float _minimumQualityThreshold = 0.6f;
        
        [Header("Market Demand")]
        [SerializeField] private DemandProfile _demandProfile;
        [SerializeField] private List<SeasonalDemandModifier> _seasonalModifiers = new List<SeasonalDemandModifier>();
        [SerializeField] private List<DemandDriver> _demandDrivers = new List<DemandDriver>();
        
        [Header("Supply Chain")]
        [SerializeField] private List<SupplyChainRequirement> _supplyRequirements = new List<SupplyChainRequirement>();
        [SerializeField] private float _shelfLife = 365f; // days
        [SerializeField] private StorageRequirements _storageRequirements;
        [SerializeField] private float _spoilageRate = 0.01f; // per day
        
        [Header("Compliance and Regulations")]
        [SerializeField] private List<ComplianceRequirement> _complianceRequirements = new List<ComplianceRequirement>();
        [SerializeField] private TaxStructure _taxStructure;
        [SerializeField] private List<Certification> _requiredCertifications = new List<Certification>();
        
        [Header("Competition Analysis")]
        [SerializeField] private MarketCompetition _competition;
        [SerializeField] private List<MarketProductSO> _directCompetitors = new List<MarketProductSO>();
        [SerializeField] private List<MarketProductSO> _substitutes = new List<MarketProductSO>();
        
        // Public Properties
        public string ProductName => _productName;
        public ProductCategory Category => _category;
        public ProductType ProductType => _productType;
        public string Description => _description;
        public Sprite ProductIcon => _productIcon;
        public MarketTier MarketTier => _marketTier;
        public ProductLegalStatus LegalStatus => _legalStatus;
        public List<MarketSegment> TargetMarkets => _targetMarkets;
        public ProductLifecycle Lifecycle => _lifecycle;
        public PricingModel PricingModel => _pricingModel;
        public float BaseWholesalePrice => _baseWholesalePrice;
        public float BaseRetailPrice => _baseRetailPrice;
        public Vector2 PriceVolatility => _priceVolatility;
        public AnimationCurve DemandPriceCurve => _demandPriceCurve;
        public QualityStandards QualityStandards => _qualityStandards;
        public List<QualityMetric> QualityMetrics => _qualityMetrics;
        public float QualityPremiumMultiplier => _qualityPremiumMultiplier;
        public float MinimumQualityThreshold => _minimumQualityThreshold;
        public DemandProfile DemandProfile => _demandProfile;
        public List<SeasonalDemandModifier> SeasonalModifiers => _seasonalModifiers;
        public List<DemandDriver> DemandDrivers => _demandDrivers;
        public List<SupplyChainRequirement> SupplyRequirements => _supplyRequirements;
        public float ShelfLife => _shelfLife;
        public StorageRequirements StorageRequirements => _storageRequirements;
        public float SpoilageRate => _spoilageRate;
        public List<ComplianceRequirement> ComplianceRequirements => _complianceRequirements;
        public TaxStructure TaxStructure => _taxStructure;
        public List<Certification> RequiredCertifications => _requiredCertifications;
        public MarketCompetition Competition => _competition;
        public List<MarketProductSO> DirectCompetitors => _directCompetitors;
        public List<MarketProductSO> Substitutes => _substitutes;
        
        /// <summary>
        /// Calculates current market price based on demand, quality, and market conditions.
        /// </summary>
        /// <param name="isRetail">Whether this is retail or wholesale pricing</param>
        /// <param name="qualityScore">Product quality score (0-1)</param>
        /// <param name="marketConditions">Current market conditions</param>
        /// <returns>Current market price per unit</returns>
        public float CalculateCurrentPrice(bool isRetail, float qualityScore, MarketConditions marketConditions)
        {
            float basePrice = isRetail ? _baseRetailPrice : _baseWholesalePrice;
            
            // Apply quality premium/discount
            float qualityModifier = CalculateQualityPriceModifier(qualityScore);
            basePrice *= qualityModifier;
            
            // Apply demand modifier
            float demandModifier = CalculateDemandPriceModifier(marketConditions.DemandLevel);
            basePrice *= demandModifier;
            
            // Apply market volatility
            float volatilityModifier = Random.Range(_priceVolatility.x, _priceVolatility.y);
            basePrice *= volatilityModifier;
            
            // Apply seasonal modifiers
            float seasonalModifier = CalculateSeasonalModifier(marketConditions.CurrentSeason);
            basePrice *= seasonalModifier;
            
            // Apply supply/demand balance
            if (marketConditions.SupplyLevel < marketConditions.DemandLevel)
            {
                float supplyShortage = (marketConditions.DemandLevel - marketConditions.SupplyLevel) / marketConditions.DemandLevel;
                basePrice *= (1f + supplyShortage * 0.5f);
            }
            
            return Mathf.Max(0.01f, basePrice);
        }
        
        /// <summary>
        /// Evaluates market attractiveness for this product.
        /// </summary>
        public float EvaluateMarketAttractiveness(MarketConditions conditions)
        {
            float attractiveness = 0.5f; // Base attractiveness
            
            // Factor in demand strength
            attractiveness += (_demandProfile.BaseDemand / 100f) * 0.3f;
            
            // Factor in price potential
            float priceScore = _baseRetailPrice / 20f; // Normalize assuming $20 is high
            attractiveness += Mathf.Min(0.2f, priceScore * 0.2f);
            
            // Factor in competition level
            float competitionPenalty = _competition.CompetitorCount / 20f; // Assuming 20 is high competition
            attractiveness -= Mathf.Min(0.2f, competitionPenalty * 0.2f);
            
            // Factor in market growth
            if (_lifecycle == ProductLifecycle.Growth)
                attractiveness += 0.1f;
            else if (_lifecycle == ProductLifecycle.Decline)
                attractiveness -= 0.1f;
            
            return Mathf.Clamp01(attractiveness);
        }
        
        /// <summary>
        /// Calculates expected profit margin for this product.
        /// </summary>
        public float CalculateProfitMargin(float productionCost, bool isRetail, float qualityScore, MarketConditions marketConditions)
        {
            float sellingPrice = CalculateCurrentPrice(isRetail, qualityScore, marketConditions);
            float totalCost = productionCost + CalculateTaxes(sellingPrice) + CalculateComplianceCosts();
            
            return ((sellingPrice - totalCost) / sellingPrice) * 100f; // Percentage margin
        }
        
        /// <summary>
        /// Determines if the product meets quality standards for market entry.
        /// </summary>
        public bool MeetsQualityStandards(float qualityScore)
        {
            return qualityScore >= _minimumQualityThreshold;
        }
        
        /// <summary>
        /// Gets storage cost per unit per day.
        /// </summary>
        public float GetStorageCostPerDay()
        {
            return _storageRequirements.StorageCostPerGramPerDay;
        }
        
        private float CalculateQualityPriceModifier(float qualityScore)
        {
            if (qualityScore >= 0.9f)
                return _qualityPremiumMultiplier;
            else if (qualityScore >= 0.7f)
                return 1f;
            else if (qualityScore >= _minimumQualityThreshold)
                return 0.8f;
            else
                return 0.5f; // Substandard quality penalty
        }
        
        private float CalculateDemandPriceModifier(float demandLevel)
        {
            if (_demandPriceCurve != null)
                return _demandPriceCurve.Evaluate(demandLevel);
            
            // Default linear relationship
            return 0.5f + (demandLevel * 0.5f);
        }
        
        private float CalculateSeasonalModifier(Season season)
        {
            var modifier = _seasonalModifiers.Find(sm => sm.Season == season);
            return modifier?.DemandMultiplier ?? 1f;
        }
        
        private float CalculateTaxes(float sellingPrice)
        {
            return sellingPrice * (_taxStructure.ExciseTaxRate + _taxStructure.SalesTaxRate);
        }
        
        private float CalculateComplianceCosts()
        {
            float totalCost = 0f;
            foreach (var requirement in _complianceRequirements)
            {
                totalCost += requirement.ComplianceCostPerUnit;
            }
            return totalCost;
        }
        
        protected override bool ValidateDataSpecific()
        {
            bool isValid = true;
            
            if (string.IsNullOrEmpty(_productName))
            {
                Debug.LogError($"MarketProductSO '{name}' has no product name assigned.", this);
                isValid = false;
            }
            
            if (_baseWholesalePrice <= 0f)
            {
                Debug.LogError($"Market Product {name}: Base wholesale price must be positive");
                isValid = false;
            }
            
            if (_baseRetailPrice <= _baseWholesalePrice)
            {
                Debug.LogError($"Market Product {name}: Retail price must be higher than wholesale price");
                isValid = false;
            }
            
            if (_shelfLife <= 0f)
            {
                Debug.LogError($"Market Product {name}: Shelf life must be positive");
                isValid = false;
            }
            
            return isValid;
        }
    }
    
    [System.Serializable]
    public class PricingModel
    {
        public PricingStrategy Strategy = PricingStrategy.Market_Based;
        [Range(0.1f, 5f)] public float MarkupMultiplier = 1.5f;
        public bool DynamicPricing = true;
        public float MinimumPrice = 1f;
        public float MaximumPrice = 100f;
    }
    
    [System.Serializable]
    public class QualityStandards
    {
        [Range(0f, 35f)] public float MinTHC = 15f;
        [Range(0f, 25f)] public float MaxCBD = 2f;
        [Range(0f, 10f)] public float MaxMoisture = 8f;
        [Range(0f, 1f)] public float MaxPesticides = 0.1f;
        [Range(0f, 5f)] public float MaxHeavyMetals = 1f;
        public bool RequiresLabTesting = true;
    }
    
    [System.Serializable]
    public class QualityMetric
    {
        public string MetricName;
        public float Weight = 1f;
        public Vector2 AcceptableRange = new Vector2(0.7f, 1f);
        public string MetricDescription;
    }
    
    [System.Serializable]
    public class DemandProfile
    {
        [Range(0f, 100f)] public float BaseDemand = 50f;
        [Range(0f, 2f)] public float PriceElasticity = 1f;
        [Range(0f, 10f)] public float GrowthRate = 2f; // percent per month
        public DemandPattern Pattern = DemandPattern.Stable;
    }
    
    [System.Serializable]
    public class SeasonalDemandModifier
    {
        public Season Season;
        [Range(0.5f, 2f)] public float DemandMultiplier = 1f;
        public string ModifierDescription;
    }
    
    [System.Serializable]
    public class DemandDriver
    {
        public string DriverName;
        [Range(-2f, 2f)] public float ImpactStrength = 1f;
        public bool IsPositive = true;
        public string DriverDescription;
    }
    
    [System.Serializable]
    public class SupplyChainRequirement
    {
        public string RequirementName;
        public bool IsRequired = true;
        public float AdditionalCost = 0f;
        public string RequirementDescription;
    }
    
    [System.Serializable]
    public class StorageRequirements
    {
        public Vector2 TemperatureRange = new Vector2(15f, 20f);
        public Vector2 HumidityRange = new Vector2(55f, 65f);
        public bool RequiresRefrigeration = false;
        public bool RequiresLightProtection = true;
        public float StorageCostPerGramPerDay = 0.001f;
    }
    
    [System.Serializable]
    public class ComplianceRequirement
    {
        public string RequirementName;
        public ComplianceType ComplianceType;
        public float ComplianceCostPerUnit = 0.1f;
        public string RequirementDescription;
    }
    
    [System.Serializable]
    public class TaxStructure
    {
        [Range(0f, 0.5f)] public float ExciseTaxRate = 0.1f;
        [Range(0f, 0.2f)] public float SalesTaxRate = 0.08f;
        [Range(0f, 0.3f)] public float CorporateTaxRate = 0.21f;
        public bool HasSpecialTaxes = false;
        public float SpecialTaxAmount = 0f;
    }
    
    [System.Serializable]
    public class Certification
    {
        public string CertificationName;
        public CertificationType CertificationType;
        public float CertificationCost = 100f;
        public int ValidityDays = 365;
        public string CertificationDescription;
    }
    
    [System.Serializable]
    public class MarketCompetition
    {
        [Range(0, 50)] public int CompetitorCount = 5;
        [Range(1, 5)] public int CompetitionIntensity = 3;
        [Range(0f, 1f)] public float MarketShare = 0.1f;
        public MarketCompetitionType DominantCompetitionType = MarketCompetitionType.Price;
    }
    
    [System.Serializable]
    public class MarketConditions
    {
        [Range(0f, 1f)] public float DemandLevel = 0.5f;
        [Range(0f, 1f)] public float SupplyLevel = 0.5f;
        public Season CurrentSeason = Season.Spring;
        [Range(0f, 1f)] public float EconomicHealth = 0.7f;
        [Range(0f, 1f)] public float RegulatoryStability = 0.8f;
    }
    
    public enum ProductCategory
    {
        Flower,
        Concentrate,
        Edible,
        Topical,
        Seed,
        Clone,
        Equipment,
        Supplies,
        Service
    }
    
    public enum ProductType
    {
        Dried_Flower,
        Fresh_Flower,
        Hash,
        Rosin,
        Oil,
        Wax,
        Shatter,
        Gummy,
        Chocolate,
        Beverage,
        Capsule,
        Cream,
        Balm,
        Regular_Seed,
        Feminized_Seed,
        Auto_Seed,
        Rooted_Clone,
        Mother_Plant
    }
    
    public enum MarketTier
    {
        Economy,
        Standard,
        Premium,
        Luxury,
        Craft,
        Medical,
        Research
    }
    
    public enum ProductLegalStatus
    {
        Illegal,
        Legal_Regulated,
        Legal_Unregulated,
        Medical_Only,
        Decriminalized,
        Research_Only
    }
    
    public enum MarketSegment
    {
        Medical_Patients,
        Recreational_Users,
        Connoisseurs,
        Price_Conscious,
        Premium_Buyers,
        Research_Institutions,
        Industrial_Users
    }
    
    public enum ProductLifecycle
    {
        Introduction,
        Growth,
        Maturity,
        Decline,
        Revival
    }
    
    public enum PricingStrategy
    {
        Cost_Plus,
        Market_Based,
        Competitive,
        Premium,
        Penetration,
        Skimming,
        Dynamic
    }
    
    public enum DemandPattern
    {
        Stable,
        Growing,
        Declining,
        Seasonal,
        Cyclical,
        Volatile
    }
    
    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }
    
    public enum ComplianceType
    {
        Testing,
        Packaging,
        Labeling,
        Tracking,
        Security,
        Quality,
        Environmental,
        Safety
    }
    
    public enum CertificationType
    {
        Organic,
        Lab_Tested,
        Pesticide_Free,
        Solvent_Free,
        Third_Party_Verified,
        GMP_Certified,
        ISO_Certified
    }
    
    public enum MarketCompetitionType
    {
        Price,
        Quality,
        Brand,
        Innovation,
        Service,
        Distribution
    }
}