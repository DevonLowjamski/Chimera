using System;
using UnityEngine;
using System.Collections.Generic;

namespace ProjectChimera.Data.Economy.ResourceFlow
{
    /// <summary>
    /// Resource flow and supply chain-focused data structures extracted from EconomicDataStructures.cs
    /// Contains resource management, supply chains, logistics, asset management, and consortium operations
    /// Part of Phase 1 Foundation Data Structures refactoring
    /// </summary>

    #region Asset Management and Positions

    [System.Serializable]
    public class StockPosition
    {
        public string Symbol;
        public decimal Shares;
        public decimal AveragePrice;
        public decimal CurrentPrice;
        public decimal TotalValue;
        public DateTime LastUpdated;
        
        public float CurrentValue => (float)TotalValue;
    }

    [System.Serializable]
    public class CommodityPosition
    {
        public string CommodityType;
        public decimal Quantity;
        public decimal AveragePrice;
        public decimal CurrentPrice;
        public decimal TotalValue;
        public DateTime LastUpdated;
        
        public float CurrentValue => (float)TotalValue;
    }

    [System.Serializable]
    public class FuturesPosition
    {
        public string ContractSymbol;
        public decimal Contracts;
        public decimal EntryPrice;
        public decimal CurrentPrice;
        public DateTime ExpirationDate;
        public decimal MarginRequired;
    }

    [System.Serializable]
    public class AssetAllocation
    {
        public string AllocationId;
        public Dictionary<string, float> AllocationWeights = new Dictionary<string, float>();
        public AllocationStrategy Strategy = AllocationStrategy.Balanced;
        public float RiskTolerance = 0.5f;
        public DateTime LastRebalance;
        public bool AutoRebalance = true;
    }

    [System.Serializable]
    public class StockExchangeInterface
    {
        public string ExchangeId;
        public string ExchangeName;
        public bool IsActive = true;
        public List<string> ListedStocks = new List<string>();
        public DateTime LastUpdate;
        public ExchangeStatus Status = ExchangeStatus.Open;
        
        public void Initialize() 
        {
            IsActive = true;
            LastUpdate = DateTime.Now;
        }
        
        public void Shutdown() 
        {
            IsActive = false;
        }
    }

    #endregion

    #region Supply Chain and Logistics

    [System.Serializable]
    public class LogisticsStrategy
    {
        public LogisticsModel Model = LogisticsModel.DirectDistribution;
        public List<string> DistributionChannels = new List<string>();
        public decimal LogisticsBudget = 0m;
        public float EfficiencyRating = 0.8f;
        public List<LogisticsPartner> Partners = new List<LogisticsPartner>();
        public DateTime LastOptimization;
    }

    [System.Serializable]
    public class LogisticsPartner
    {
        public string PartnerId;
        public string PartnerName;
        public PartnerType Type = PartnerType.Distributor;
        public List<string> ServiceAreas = new List<string>();
        public float ReliabilityRating = 0.9f;
        public decimal CostPerUnit = 5m;
        public bool IsActive = true;
    }

    [System.Serializable]
    public class SupplyChainManager
    {
        public string ManagerId;
        public List<SupplierRelationship> Suppliers = new List<SupplierRelationship>();
        public List<DistributionChannel> Channels = new List<DistributionChannel>();
        public SupplyChainMetrics Metrics;
        public DateTime LastOptimization;
        public bool IsActive = true;
    }

    [System.Serializable]
    public class SupplierRelationship
    {
        public string SupplierId;
        public string SupplierName;
        public SupplierType Type = SupplierType.Primary;
        public List<string> ProductCategories = new List<string>();
        public float ReliabilityScore = 0.8f;
        public decimal AverageLeadTime = 14m; // days
        public ContractTerms Terms;
        public bool IsActive = true;
    }

    [System.Serializable]
    public class DistributionChannel
    {
        public string ChannelId;
        public string ChannelName;
        public ChannelType Type = ChannelType.Direct;
        public List<string> GeographicCoverage = new List<string>();
        public float EfficiencyRating = 0.7f;
        public decimal CostPercentage = 0.15m;
        public bool IsActive = true;
    }

    [System.Serializable]
    public class SupplyChainMetrics
    {
        public float OverallEfficiency = 0.75f;
        public decimal TotalCost = 0m;
        public float OnTimeDeliveryRate = 0.9f;
        public float QualityScore = 0.85f;
        public int AverageLeadTime = 10; // days
        public DateTime LastCalculated;
    }

    [System.Serializable]
    public class SupplyDisruptionResult
    {
        public float Effectiveness = 0.5f;
        public float SupplyChainImpact = 0.3f;
        public decimal CostIncrease = 1000m;
        public int DelayDays = 7;
        public List<string> AffectedSuppliers = new List<string>();
        public DisruptionSeverity Severity = DisruptionSeverity.Moderate;
        public DateTime RecoveryEstimate;
    }

    [System.Serializable]
    public class ContractTerms
    {
        public decimal ContractValue = 10000m;
        public int TermDays = 365;
        public PaymentTerms PaymentTerms = PaymentTerms.Net30;
        public List<string> DeliverableRequirements = new List<string>();
        public PenaltyClause PenaltyClause;
        public DateTime StartDate;
        public DateTime EndDate;
    }

    [System.Serializable]
    public class PenaltyClause
    {
        public decimal LatePenaltyPercentage = 0.02m;
        public decimal QualityPenaltyPercentage = 0.05m;
        public int GracePeriodDays = 3;
        public decimal MaxPenaltyAmount = 5000m;
    }

    #endregion

    #region Cash Flow and Resource Management

    [System.Serializable]
    public class CashFlowManagement
    {
        public bool IsActive = true;
        public decimal CurrentCashFlow = 1000m;
        public List<CashFlowProjection> Projections = new List<CashFlowProjection>();
        public CashFlowStrategy Strategy = CashFlowStrategy.Conservative;
        public float LiquidityRatio = 1.5f;
        public DateTime LastAnalysis;
    }

    [System.Serializable]
    public class CashFlowProjection
    {
        public string ProjectionId;
        public DateTime ProjectionDate;
        public decimal ProjectedInflow = 5000m;
        public decimal ProjectedOutflow = 4000m;
        public decimal NetCashFlow = 1000m;
        public ProjectionMethod Method = ProjectionMethod.Historical;
        public float ConfidenceLevel = 0.8f;
        public ProjectionPeriod Period = ProjectionPeriod.Monthly;
    }

    [System.Serializable]
    public class ResourceAllocationManager
    {
        public string ManagerId;
        public Dictionary<string, decimal> ResourceAllocation = new Dictionary<string, decimal>();
        public AllocationStrategy Strategy = AllocationStrategy.Balanced;
        public float UtilizationRate = 0.75f;
        public DateTime LastReallocation;
        public bool AutoOptimize = true;
    }

    [System.Serializable]
    public class ResourcePool
    {
        public string PoolId;
        public string PoolName;
        public ResourceType Type = ResourceType.Financial;
        public decimal TotalCapacity = 100000m;
        public decimal AllocatedAmount = 75000m;
        public decimal AvailableAmount = 25000m;
        public List<string> Stakeholders = new List<string>();
        public DateTime LastUpdate;
    }

    [System.Serializable]
    public class SharedResourceManager
    {
        public string ManagerId;
        public List<string> SharedResources = new List<string>();
        public Dictionary<string, ResourceUsage> UsageTracking = new Dictionary<string, ResourceUsage>();
        public SharingPolicy Policy;
        public DateTime LastOptimization;
    }

    [System.Serializable]
    public class ResourceUsage
    {
        public string UserId;
        public decimal AmountUsed = 0m;
        public DateTime UsageDate;
        public UsageType Type = UsageType.Planned;
        public string Purpose;
        public bool IsApproved = true;
    }

    [System.Serializable]
    public class SharingPolicy
    {
        public string PolicyId;
        public AllocationMethod Method = AllocationMethod.EqualShare;
        public List<SharingRule> Rules = new List<SharingRule>();
        public bool RequireApproval = false;
        public float MaxUsagePercentage = 0.8f;
    }

    [System.Serializable]
    public class SharingRule
    {
        public string RuleId;
        public RuleType Type = RuleType.Usage;
        public string Condition;
        public string Action;
        public bool IsActive = true;
        public int Priority = 1;
    }

    #endregion

    #region Consortium and Partnership Management

    [System.Serializable]
    public class BusinessConsortium
    {
        public string ConsortiumId;
        public string ConsortiumName;
        public string Description;
        public DateTime EstablishedDate;
        public ConsortiumStatus Status = ConsortiumStatus.Active;
        public List<ConsortiumMember> Members = new List<ConsortiumMember>();
        public ConsortiumType ConsortiumType = ConsortiumType.Research;
        public ProfitSharingModel ProfitSharing;
        public List<string> SharedResources = new List<string>();
        public GovernanceStructure Governance;
    }

    [System.Serializable]
    public class ConsortiumMember
    {
        public string MemberId;
        public string MemberName;
        public ConsortiumRole Role = ConsortiumRole.Member;
        public decimal InvestmentContribution = 10000m;
        public float OwnershipPercentage = 0.2f;
        public DateTime JoinDate;
        public bool IsActive = true;
        public Dictionary<string, decimal> ResourceContributions = new Dictionary<string, decimal>();
    }

    [System.Serializable]
    public class ConsortiumConfiguration
    {
        public string ConfigurationId;
        public string ConsortiumName;
        public ConsortiumType ConsortiumType = ConsortiumType.Business;
        public decimal MinimumInvestment = 5000m;
        public int MaxMembers = 10;
        public ProfitSharingModel ProfitSharingModel;
        public List<string> SharedResources = new List<string>();
        public GovernanceModel GovernanceModel = GovernanceModel.Democratic;
    }

    [System.Serializable]
    public class ConsortiumEstablishmentResult
    {
        public bool Success = false;
        public string ConsortiumId;
        public BusinessConsortium Consortium;
        public decimal TotalCapital = 0m;
        public List<string> FoundingMembers = new List<string>();
        public DateTime EstablishmentDate;
        public string ErrorMessage;
    }

    [System.Serializable]
    public class PartnershipStrategy
    {
        public PartnershipType PreferredType = PartnershipType.Strategic;
        public List<string> TargetPartners = new List<string>();
        public PartnershipTerms Terms;
        public float MinSynergyThreshold = 0.7f;
        public List<PartnershipObjective> Objectives = new List<PartnershipObjective>();
    }

    [System.Serializable]
    public class PartnershipTerms
    {
        public string TermsId;
        public DateTime StartDate;
        public DateTime EndDate;
        public List<string> Responsibilities = new List<string>();
        public ProfitSharingModel ProfitSharing;
        public TerminationClause TerminationClause;
        public ConflictResolution ConflictResolution;
    }

    [System.Serializable]
    public class PartnershipObjective
    {
        public string ObjectiveId;
        public string Description;
        public ObjectiveType Type = ObjectiveType.Revenue;
        public decimal TargetValue = 50000m;
        public DateTime TargetDate;
        public float Progress = 0f;
        public bool IsAchieved = false;
    }

    [System.Serializable]
    public class GovernanceStructure
    {
        public GovernanceModel Model = GovernanceModel.Democratic;
        public List<string> BoardMembers = new List<string>();
        public DecisionMakingProcess DecisionProcess = DecisionMakingProcess.Majority;
        public VotingRights VotingStructure;
        public List<GovernanceRule> Rules = new List<GovernanceRule>();
    }

    [System.Serializable]
    public class GovernanceRule
    {
        public string RuleId;
        public string RuleName;
        public string Description;
        public RuleScope Scope = RuleScope.All;
        public bool IsActive = true;
        public DateTime EffectiveDate;
    }

    [System.Serializable]
    public class VotingRights
    {
        public VotingType Type = VotingType.EqualVote;
        public Dictionary<string, float> VotingWeights = new Dictionary<string, float>();
        public float QuorumRequirement = 0.5f;
        public float MajorityThreshold = 0.6f;
    }

    [System.Serializable]
    public class ProfitSharingModel
    {
        public SharingType Type = SharingType.Proportional;
        public Dictionary<string, float> SharePercentages = new Dictionary<string, float>();
        public PayoutFrequency Frequency = PayoutFrequency.Quarterly;
        public decimal MinimumPayout = 100m;
        public bool AutomaticDistribution = true;
    }

    [System.Serializable]
    public class TerminationClause
    {
        public int NoticePeriodDays = 30;
        public List<TerminationReason> ValidReasons = new List<TerminationReason>();
        public PenaltyStructure Penalties;
        public AssetDistribution AssetDistribution;
    }

    [System.Serializable]
    public class ConflictResolution
    {
        public ResolutionMethod Method = ResolutionMethod.Mediation;
        public string MediatorId;
        public int ResolutionTimeframeDays = 60;
        public EscalationProcess Escalation;
    }

    [System.Serializable]
    public class PenaltyStructure
    {
        public decimal EarlyTerminationFee = 5000m;
        public float ContractBreachPercentage = 0.1f;
        public List<PenaltyTier> Tiers = new List<PenaltyTier>();
    }

    [System.Serializable]
    public class PenaltyTier
    {
        public PenaltyType Type = PenaltyType.Financial;
        public decimal Amount = 1000m;
        public string Condition;
        public bool IsPercentage = false;
    }

    [System.Serializable]
    public class AssetDistribution
    {
        public DistributionMethod Method = DistributionMethod.ProRata;
        public List<AssetCategory> IncludedAssets = new List<AssetCategory>();
        public LiquidationPreference Preference;
    }

    [System.Serializable]
    public class LiquidationPreference
    {
        public PreferenceType Type = PreferenceType.NonParticipating;
        public float Multiplier = 1.0f;
        public List<string> PreferredMembers = new List<string>();
    }

    [System.Serializable]
    public class EscalationProcess
    {
        public List<EscalationLevel> Levels = new List<EscalationLevel>();
        public int MaxEscalationDays = 90;
        public bool RequireUnanimous = false;
    }

    [System.Serializable]
    public class EscalationLevel
    {
        public int Level = 1;
        public ResolutionMethod Method = ResolutionMethod.DirectNegotiation;
        public int TimeframeDays = 15;
        public List<string> Participants = new List<string>();
    }

    #endregion

    #region Supporting Enums

    public enum AllocationStrategy
    {
        Conservative,
        Balanced,
        Aggressive,
        Growth,
        Income,
        Custom
    }

    public enum ExchangeStatus
    {
        Open,
        Closed,
        PreMarket,
        PostMarket,
        Suspended,
        Maintenance
    }

    public enum LogisticsModel
    {
        DirectDistribution,
        ThirdPartyLogistics,
        HybridModel,
        DropShipping,
        CrossDocking,
        JustInTime
    }

    public enum PartnerType
    {
        Supplier,
        Distributor,
        Retailer,
        Manufacturer,
        ServiceProvider,
        Technology
    }

    public enum SupplierType
    {
        Primary,
        Secondary,
        Backup,
        Strategic,
        Commodity,
        Specialty
    }

    public enum ChannelType
    {
        Direct,
        Retail,
        Online,
        Wholesale,
        Partner,
        Franchise
    }

    public enum DisruptionSeverity
    {
        Minor,
        Moderate,
        Major,
        Critical,
        Catastrophic
    }

    public enum PaymentTerms
    {
        Immediate,
        Net15,
        Net30,
        Net60,
        Net90,
        COD,
        Prepaid
    }

    public enum CashFlowStrategy
    {
        Conservative,
        Moderate,
        Aggressive,
        Growth,
        Defensive
    }

    public enum ProjectionMethod
    {
        Historical,
        Trending,
        Seasonal,
        Statistical,
        MachineLearning
    }

    public enum ProjectionPeriod
    {
        Weekly,
        Monthly,
        Quarterly,
        Annual,
        Custom
    }

    public enum ResourceType
    {
        Financial,
        Human,
        Physical,
        Intellectual,
        Natural,
        Technology
    }

    public enum UsageType
    {
        Planned,
        Emergency,
        Maintenance,
        Growth,
        Operational
    }

    public enum AllocationMethod
    {
        EqualShare,
        ProportionalShare,
        NeedBased,
        MeritBased,
        HybridModel
    }

    public enum RuleType
    {
        Usage,
        Access,
        Priority,
        Approval,
        Limitation
    }

    public enum ConsortiumType
    {
        Research,
        Business,
        Investment,
        Marketing,
        Technology,
        Supply
    }

    public enum ConsortiumRole
    {
        Founder,
        Member,
        Partner,
        Investor,
        Advisor,
        Observer
    }

    public enum ConsortiumStatus
    {
        Active,
        Inactive,
        Dissolved,
        Suspended,
        UnderReview
    }

    public enum PartnershipType
    {
        Strategic,
        Operational,
        Financial,
        Technology,
        Marketing,
        Distribution
    }

    public enum ObjectiveType
    {
        Revenue,
        Cost,
        Market,
        Innovation,
        Efficiency,
        Growth
    }

    public enum GovernanceModel
    {
        Democratic,
        Hierarchical,
        Consensus,
        Delegated,
        Hybrid
    }

    public enum DecisionMakingProcess
    {
        Unanimous,
        Majority,
        SuperMajority,
        Weighted,
        Delegated
    }

    public enum VotingType
    {
        EqualVote,
        WeightedVote,
        ShareBased,
        ContributionBased
    }

    public enum SharingType
    {
        Equal,
        Proportional,
        Performance,
        Contribution,
        Hybrid
    }

    public enum PayoutFrequency
    {
        Monthly,
        Quarterly,
        SemiAnnual,
        Annual,
        OnDemand
    }

    public enum TerminationReason
    {
        Breach,
        NonPerformance,
        Insolvency,
        Strategic,
        Mutual,
        Force
    }

    public enum ResolutionMethod
    {
        DirectNegotiation,
        Mediation,
        Arbitration,
        Litigation,
        Expert
    }

    public enum PenaltyType
    {
        Financial,
        Operational,
        Strategic,
        Reputational
    }

    public enum DistributionMethod
    {
        ProRata,
        Waterfall,
        Equal,
        Priority,
        Negotiated
    }

    public enum PreferenceType
    {
        Participating,
        NonParticipating,
        Multiple,
        Capped
    }

    public enum AssetCategory
    {
        Cash,
        Investments,
        Equipment,
        IntellectualProperty,
        RealEstate,
        Inventory
    }

    public enum RuleScope
    {
        All,
        Financial,
        Operational,
        Strategic,
        Governance
    }

    #endregion
}