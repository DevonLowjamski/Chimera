using UnityEngine;
using System;
using System.Collections.Generic;

namespace ProjectChimera.Data.Save
{
    /// <summary>
    /// Data Transfer Objects for Market System Save/Load Operations
    /// </summary>
    
    /// <summary>
    /// DTO for market system state
    /// </summary>
    [System.Serializable]
    public class MarketStateDTO
    {
        [Header("Marketplace Configuration")]
        public MarketplaceConfigDTO MarketplaceConfig;
        
        [Header("Product Catalog")]
        public Dictionary<string, MarketProductDTO> ProductCatalog = new Dictionary<string, MarketProductDTO>();
        public List<string> AvailableProductIds = new List<string>();
        
        [Header("Market Performance")]
        public bool IsMarketActive = true;
        public float TotalMarketVolume;
        public float AverageTransactionValue;
        public int TotalTransactions;
        public DateTime LastMarketUpdate;
        
        [Header("Market System Settings")]
        public bool EnableMarketSystem = true;
        public bool EnableDynamicPricing = true;
        public bool EnableSupplyDemandTracking = true;
        public float MarketUpdateFrequency = 4.0f;
        
        [Header("Market Data")]
        public List<MarketTrendDTO> MarketTrends = new List<MarketTrendDTO>();
        public Dictionary<string, float> ProductPrices = new Dictionary<string, float>();
        
        [Header("Market Events")]
        public List<MarketEventDTO> MarketEvents = new List<MarketEventDTO>();
        
        [Header("Supply and Demand")]
        public Dictionary<string, SupplyDemandDataDTO> SupplyDemandData = new Dictionary<string, SupplyDemandDataDTO>();
        
        [Header("Competitive Analysis")]
        public Dictionary<string, CompetitorDataDTO> CompetitorData = new Dictionary<string, CompetitorDataDTO>();
    }
    
    /// <summary>
    /// DTO for market product information
    /// </summary>
    [System.Serializable]
    public class MarketProductDTO
    {
        [Header("Product Identity")]
        public string ProductId;
        public string ProductName;
        public string Category;
        public string Description;
        
        [Header("Pricing")]
        public float BasePrice;
        public float CurrentPrice;
        public float MinPrice;
        public float MaxPrice;
        
        [Header("Quality and Standards")]
        public QualityStandardsDTO QualityStandards;
        public float QualityRating;
        
        [Header("Market Data")]
        public float Demand;
        public float Supply;
        public DemandProfileDTO DemandProfile;
        public List<SeasonalModifierDTO> SeasonalModifiers = new List<SeasonalModifierDTO>();
        
        [Header("Storage and Logistics")]
        public StorageRequirementsDTO StorageRequirements;
        
        [Header("Competition")]
        public MarketCompetitionDTO Competition;
        
        [Header("Performance Tracking")]
        public MarketProductPerformanceDTO Performance;
        
        [Header("Product Status")]
        public bool IsAvailable = true;
        public bool IsActive = true;
        public DateTime LastUpdated;
    }
    
    /// <summary>
    /// DTO for marketplace configuration
    /// </summary>
    [System.Serializable]
    public class MarketplaceConfigDTO
    {
        [Header("Marketplace Settings")]
        public string MarketplaceName;
        public float TransactionFeePercentage;
        public float MinimumTransactionValue;
        public bool EnableNegotiation;
        public float NegotiationRange;
        
        [Header("Operating Hours")]
        public TradingHoursDTO TradingHours;
        public List<HolidayDTO> Holidays = new List<HolidayDTO>();
    }
    
    /// <summary>
    /// DTO for market trends analysis
    /// </summary>
    [System.Serializable]
    public class MarketTrendDTO
    {
        public string ProductId;
        public string TrendType; // "Price", "Demand", "Supply"
        public float TrendDirection; // -1 to 1
        public float TrendStrength; // 0 to 1
        public DateTime TrendStartDate;
        public List<float> HistoricalData = new List<float>();
        public float PredictedValue;
        public DateTime LastAnalysis;
    }
    
    /// <summary>
    /// DTO for market events
    /// </summary>
    [System.Serializable]
    public class MarketEventDTO
    {
        public string EventId;
        public string EventType;
        public string Description;
        public DateTime EventDate;
        public Dictionary<string, float> ProductImpacts = new Dictionary<string, float>();
        public float Duration;
        public bool IsActive;
    }
    
    /// <summary>
    /// DTO for supply and demand data
    /// </summary>
    [System.Serializable]
    public class SupplyDemandDataDTO
    {
        public string ProductId;
        public float CurrentSupply;
        public float CurrentDemand;
        public float SupplyTrend;
        public float DemandTrend;
        public DateTime LastUpdated;
    }
    
    /// <summary>
    /// DTO for competitor data
    /// </summary>
    [System.Serializable]
    public class CompetitorDataDTO
    {
        public string CompetitorId;
        public string CompetitorName;
        public Dictionary<string, float> CompetitorPrices = new Dictionary<string, float>();
        public float MarketShare;
        public float QualityRating;
    }
    
    /// <summary>
    /// DTO for market product performance tracking
    /// </summary>
    [System.Serializable]
    public class MarketProductPerformanceDTO
    {
        [Header("Sales Performance")]
        public float TotalSalesVolume;
        public float TotalRevenue;
        public float AverageSellingPrice;
        public int TransactionCount;
        
        [Header("Market Position")]
        public float MarketShare;
        public int MarketRanking;
        
        [Header("Trends")]
        public float PriceVolatility;
        public float DemandStability;
        public DateTime LastPerformanceUpdate;
    }
    
    /// <summary>
    /// DTO for market conditions
    /// </summary>
    [System.Serializable]
    public class MarketConditionsDTO
    {
        [Header("Overall Market Health")]
        public float MarketHealthIndex; // 0.0 to 1.0
        public string MarketSentiment; // "Bullish", "Bearish", "Neutral"
        
        [Header("Economic Factors")]
        public float InflationRate;
        public float InterestRate;
        public float UnemploymentRate;
        
        [Header("Market Volatility")]
        public float VolatilityIndex;
        public List<string> VolatilityFactors = new List<string>();
    }
    
    /// <summary>
    /// DTO for market competition analysis
    /// </summary>
    [System.Serializable]
    public class MarketCompetitionDTO
    {
        public int CompetitorCount;
        public float AverageCompetitorPrice;
        public float PriceCompetitiveness; // How competitive our price is
        public string MarketPosition; // "Leader", "Challenger", "Follower", "Niche"
    }
    
    /// <summary>
    /// Supporting DTOs for market system
    /// </summary>
    
    [System.Serializable]
    public class QualityStandardsDTO
    {
        public float MinQuality;
        public float PreferredQuality;
        public List<QualityMetricDTO> QualityMetrics = new List<QualityMetricDTO>();
    }
    
    [System.Serializable]
    public class QualityMetricDTO
    {
        public string MetricName;
        public float MinValue;
        public float MaxValue;
        public float Weight; // Importance of this metric
    }
    
    [System.Serializable]
    public class DemandProfileDTO
    {
        public float BaseDemand;
        public float PeakDemand;
        public float OffPeakDemand;
        public List<TimeRangeDTO> PeakHours = new List<TimeRangeDTO>();
    }
    
    [System.Serializable]
    public class SeasonalModifierDTO
    {
        public string Season;
        public float DemandMultiplier;
        public float PriceMultiplier;
        public DateTime StartDate;
        public DateTime EndDate;
    }
    
    [System.Serializable]
    public class StorageRequirementsDTO
    {
        public float StorageSpace; // Required storage space
        public string StorageType; // "Cool", "Dry", "Frozen", etc.
        public float StorageCostPerUnit;
    }
    
    [System.Serializable]
    public class TradingHoursDTO
    {
        public TimeRangeDTO MondayHours;
        public TimeRangeDTO TuesdayHours;
        public TimeRangeDTO WednesdayHours;
        public TimeRangeDTO ThursdayHours;
        public TimeRangeDTO FridayHours;
        public TimeRangeDTO SaturdayHours;
        public TimeRangeDTO SundayHours;
    }
    
    [System.Serializable]
    public class TimeRangeDTO
    {
        public float StartTime; // Hours since midnight (0.0 - 24.0)
        public float EndTime;
    }
    
    [System.Serializable]
    public class HolidayDTO
    {
        public string HolidayName;
        public DateTime Date;
        public bool MarketClosed;
    }
}