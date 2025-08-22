using UnityEngine;
using ProjectChimera.Shared;
using ProjectChimera.Data.Shared;
using System.Collections.Generic;

namespace ProjectChimera.Data.Economy
{
    /// <summary>
    /// Defines commercial contracts for cannabis cultivation including supply agreements, processing contracts,
    /// and distribution deals. Includes advanced post-harvest processing requirements and quality specifications.
    /// </summary>
    [CreateAssetMenu(fileName = "New Contract", menuName = "Project Chimera/Economy/Contract")]
    public class ContractSO : ChimeraDataSO
    {
        [Header("Contract Identity")]
        [SerializeField] private string _contractName;
        [SerializeField] private ContractType _contractType = ContractType.Supply_Agreement;
        [SerializeField] private ContractCategory _category = ContractCategory.Processing;
        [SerializeField, TextArea(3, 5)] private string _description;
        [SerializeField] private string _contractorName;
        
        [Header("Contract Terms")]
        [SerializeField] private ContractTerms _terms;
        [SerializeField] private PaymentStructure _paymentStructure;
        [SerializeField] private List<DeliveryRequirement> _deliveryRequirements = new List<DeliveryRequirement>();
        [SerializeField] private List<QualityClause> _qualityClauses = new List<QualityClause>();
        
        [Header("Product Specifications")]
        [SerializeField] private List<ProductSpecification> _productSpecs = new List<ProductSpecification>();
        [SerializeField] private bool _allowsMultipleStrains = true;
        [SerializeField] private int _maxStrainVarieties = 5;
        [SerializeField] private StrainType _preferredStrainType = StrainType.Hybrid;
        
        [Header("Post-Harvest Processing Requirements")]
        [SerializeField] private PostHarvestProcessingRequirements _processingRequirements;
        [SerializeField] private List<ProcessingMethod> _requiredProcessingMethods = new List<ProcessingMethod>();
        [SerializeField] private List<QualityControlStep> _qualityControlSteps = new List<QualityControlStep>();
        [SerializeField] private bool _requiresSpecializedEquipment = false;
        
        [Header("Risk and Penalties")]
        [SerializeField] private List<ContractRisk> _contractRisks = new List<ContractRisk>();
        [SerializeField] private List<PenaltyClause> _penaltyClauses = new List<PenaltyClause>();
        [SerializeField] private List<BonusIncentive> _bonusIncentives = new List<BonusIncentive>();
        [SerializeField] private float _defaultRisk = 0.05f; // 5% chance
        
        [Header("Market Conditions")]
        [SerializeField] private MarketConditionRequirements _marketRequirements;
        [SerializeField] private bool _hasInflationProtection = false;
        [SerializeField] private bool _hasPriceVoLatilityProtection = false;
        [SerializeField] private float _priceAdjustmentThreshold = 0.2f; // 20%
        
        [Header("Compliance and Regulations")]
        [SerializeField] private List<ComplianceClause> _complianceClauses = new List<ComplianceClause>();
        [SerializeField] private List<CertificationRequirement> _certificationRequirements = new List<CertificationRequirement>();
        [SerializeField] private bool _requiresChainOfCustody = true;
        [SerializeField] private bool _requiresThirdPartyTesting = true;
        
        [Header("Environmental and Sustainability")]
        [SerializeField] private SustainabilityRequirements _sustainabilityRequirements;
        [SerializeField] private List<EnvironmentalStandard> _environmentalStandards = new List<EnvironmentalStandard>();
        [SerializeField] private bool _requiresOrganicCertification = false;
        [SerializeField] private float _carbonFootprintLimit = 100f; // kg CO2 per kg product
        
        // Public Properties
        public string ContractName => _contractName;
        public ContractType ContractType => _contractType;
        public ContractCategory Category => _category;
        public string Description => _description;
        public string ContractorName => _contractorName;
        public ContractTerms Terms => _terms;
        public PaymentStructure PaymentStructure => _paymentStructure;
        public List<DeliveryRequirement> DeliveryRequirements => _deliveryRequirements;
        public List<QualityClause> QualityClauses => _qualityClauses;
        public List<ProductSpecification> ProductSpecs => _productSpecs;
        public bool AllowsMultipleStrains => _allowsMultipleStrains;
        public int MaxStrainVarieties => _maxStrainVarieties;
        public StrainType PreferredStrainType => _preferredStrainType;
        public PostHarvestProcessingRequirements ProcessingRequirements => _processingRequirements;
        public List<ProcessingMethod> RequiredProcessingMethods => _requiredProcessingMethods;
        public List<QualityControlStep> QualityControlSteps => _qualityControlSteps;
        public bool RequiresSpecializedEquipment => _requiresSpecializedEquipment;
        public List<ContractRisk> ContractRisks => _contractRisks;
        public List<PenaltyClause> PenaltyClauses => _penaltyClauses;
        public List<BonusIncentive> BonusIncentives => _bonusIncentives;
        public float DefaultRisk => _defaultRisk;
        public MarketConditionRequirements MarketRequirements => _marketRequirements;
        public bool HasInflationProtection => _hasInflationProtection;
        public bool HasPriceVolatilityProtection => _hasPriceVoLatilityProtection;
        public float PriceAdjustmentThreshold => _priceAdjustmentThreshold;
        public List<ComplianceClause> ComplianceClauses => _complianceClauses;
        public List<CertificationRequirement> CertificationRequirements => _certificationRequirements;
        public bool RequiresChainOfCustody => _requiresChainOfCustody;
        public bool RequiresThirdPartyTesting => _requiresThirdPartyTesting;
        public SustainabilityRequirements SustainabilityRequirements => _sustainabilityRequirements;
        public List<EnvironmentalStandard> EnvironmentalStandards => _environmentalStandards;
        public bool RequiresOrganicCertification => _requiresOrganicCertification;
        public float CarbonFootprintLimit => _carbonFootprintLimit;
        
        /// <summary>
        /// Evaluates if the player can fulfill this contract with current capabilities.
        /// </summary>
        public ContractFeasibility EvaluateContractFeasibility(PlayerCapabilities playerCapabilities)
        {
            var feasibility = new ContractFeasibility();
            
            // Check production capacity
            float requiredQuantity = _terms.QuantityRequired;
            feasibility.CanMeetQuantity = playerCapabilities.ProductionCapacity >= requiredQuantity;
            
            // Check quality capabilities
            feasibility.CanMeetQuality = EvaluateQualityCapability(playerCapabilities);
            
            // Check processing capabilities
            feasibility.CanMeetProcessing = EvaluateProcessingCapability(playerCapabilities);
            
            // Check compliance capabilities
            feasibility.CanMeetCompliance = EvaluateComplianceCapability(playerCapabilities);
            
            // Calculate overall feasibility score
            feasibility.FeasibilityScore = CalculateOverallFeasibility(feasibility);
            
            return feasibility;
        }
        
        /// <summary>
        /// Calculates the expected profit from completing this contract.
        /// </summary>
        public float CalculateExpectedProfit(float productionCost, float processingCost, float complianceCost)
        {
            float totalRevenue = _terms.TotalContractValue;
            float totalCosts = productionCost + processingCost + complianceCost;
            
            // Apply potential bonuses
            float bonusRevenue = CalculatePotentialBonuses();
            totalRevenue += bonusRevenue;
            
            // Apply potential penalties
            float penaltyRisk = CalculatePenaltyRisk();
            totalRevenue -= penaltyRisk;
            
            return totalRevenue - totalCosts;
        }
        
        /// <summary>
        /// Determines the processing complexity score for this contract.
        /// </summary>
        public float CalculateProcessingComplexity()
        {
            float complexity = 0f;
            
            // Base complexity from processing methods
            foreach (var method in _requiredProcessingMethods)
            {
                complexity += GetProcessingMethodComplexity(method);
            }
            
            // Additional complexity from quality control steps
            complexity += _qualityControlSteps.Count * 0.1f;
            
            // Specialized equipment adds complexity
            if (_requiresSpecializedEquipment)
                complexity += 0.5f;
            
            // Multiple strain varieties add complexity
            if (_allowsMultipleStrains && _maxStrainVarieties > 1)
                complexity += _maxStrainVarieties * 0.1f;
            
            return Mathf.Clamp01(complexity);
        }
        
        /// <summary>
        /// Gets the minimum quality score required to fulfill this contract.
        /// </summary>
        public float GetMinimumQualityRequirement()
        {
            float minQuality = 0.6f; // Default minimum
            
            foreach (var qualityClause in _qualityClauses)
            {
                minQuality = Mathf.Max(minQuality, qualityClause.MinimumQualityScore);
            }
            
            return minQuality;
        }
        
        /// <summary>
        /// Evaluates contract risk level considering all factors.
        /// </summary>
        public float EvaluateOverallRisk()
        {
            float risk = _defaultRisk;
            
            // Add processing complexity risk
            risk += CalculateProcessingComplexity() * 0.1f;
            
            // Add market condition risk
            if (_marketRequirements.IsMarketSensitive)
                risk += 0.05f;
            
            // Add compliance risk
            risk += _complianceClauses.Count * 0.01f;
            
            // Add environmental standard risk
            risk += _environmentalStandards.Count * 0.005f;
            
            return Mathf.Clamp01(risk);
        }
        
        private bool EvaluateQualityCapability(PlayerCapabilities playerCapabilities)
        {
            float minRequired = GetMinimumQualityRequirement();
            return playerCapabilities.AverageQualityScore >= minRequired;
        }
        
        private bool EvaluateProcessingCapability(PlayerCapabilities playerCapabilities)
        {
            foreach (var method in _requiredProcessingMethods)
            {
                if (!playerCapabilities.AvailableProcessingMethods.Contains(method.ProcessingType))
                    return false;
            }
            return true;
        }
        
        private bool EvaluateComplianceCapability(PlayerCapabilities playerCapabilities)
        {
            foreach (var requirement in _certificationRequirements)
            {
                if (!playerCapabilities.Certifications.Contains(requirement.CertificationType))
                    return false;
            }
            return true;
        }
        
        private float CalculateOverallFeasibility(ContractFeasibility feasibility)
        {
            float score = 0f;
            if (feasibility.CanMeetQuantity) score += 0.25f;
            if (feasibility.CanMeetQuality) score += 0.25f;
            if (feasibility.CanMeetProcessing) score += 0.25f;
            if (feasibility.CanMeetCompliance) score += 0.25f;
            
            return score;
        }
        
        private float CalculatePotentialBonuses()
        {
            float totalBonus = 0f;
            foreach (var bonus in _bonusIncentives)
            {
                totalBonus += bonus.BonusAmount * bonus.AchievementProbability;
            }
            return totalBonus;
        }
        
        private float CalculatePenaltyRisk()
        {
            float totalPenalty = 0f;
            foreach (var penalty in _penaltyClauses)
            {
                totalPenalty += penalty.PenaltyAmount * penalty.ViolationProbability;
            }
            return totalPenalty;
        }
        
        private float GetProcessingMethodComplexity(ProcessingMethod method)
        {
            switch (method.ProcessingType)
            {
                case ProcessingType.Drying: return 0.1f;
                case ProcessingType.Curing: return 0.15f;
                case ProcessingType.Trimming: return 0.1f;
                case ProcessingType.Extraction: return 0.4f;
                case ProcessingType.Purification: return 0.5f;
                case ProcessingType.Distillation: return 0.6f;
                case ProcessingType.Crystallization: return 0.7f;
                case ProcessingType.Encapsulation: return 0.3f;
                default: return 0.2f;
            }
        }
        
        protected override bool ValidateDataSpecific()
        {
            bool isValid = true;
            
            if (string.IsNullOrEmpty(_contractName))
            {
                Debug.LogError($"Contract {name}: Contract name cannot be empty", this);
                isValid = false;
            }
                
            if (string.IsNullOrEmpty(_contractorName))
            {
                Debug.LogError($"Contract {name}: Contractor name must be assigned", this);
                isValid = false;
            }
                
            if (_terms.TotalContractValue <= 0f)
            {
                Debug.LogError($"Contract {name}: Contract value must be positive", this);
                isValid = false;
            }
                
            if (_productSpecs.Count == 0)
            {
                Debug.LogWarning($"Contract {name}: No product specifications defined", this);
                isValid = false;
            }
            
            return isValid;
        }
    }
    
    [System.Serializable]
    public class ContractTerms
    {
        [Range(1, 365)] public int DurationDays = 90;
        [Range(0.1f, 1000000f)] public float QuantityRequired = 100f; // kg
        [Range(100f, 10000000f)] public float TotalContractValue = 10000f;
        public bool IsRenewable = false;
        public int MaxRenewals = 0;
        [Range(0f, 1f)] public float EarlyTerminationPenalty = 0.1f;
    }
    
    [System.Serializable]
    public class PaymentStructure
    {
        public PaymentTerms PaymentTerms = PaymentTerms.Net_30;
        [Range(0f, 1f)] public float UpfrontPayment = 0.2f;
        [Range(0f, 1f)] public float ProgressPayments = 0.5f;
        [Range(0f, 1f)] public float FinalPayment = 0.3f;
        public bool AllowsPartialPayments = true;
        [Range(0f, 0.1f)] public float LatePaymentPenalty = 0.02f;
    }
    
    [System.Serializable]
    public class DeliveryRequirement
    {
        public string RequirementName;
        public DeliveryMethod DeliveryMethod = DeliveryMethod.Pickup;
        public int MaxDeliveryDays = 7;
        public Vector2 DeliveryTimeWindow = new Vector2(8f, 17f); // Hours
        public float DeliveryCost = 50f;
        public string SpecialInstructions;
    }
    
    [System.Serializable]
    public class QualityClause
    {
        public string ClauseName;
        [Range(0f, 1f)] public float MinimumQualityScore = 0.7f;
        public List<QualityMetric> RequiredMetrics = new List<QualityMetric>();
        public TestingRequirement TestingRequirement = TestingRequirement.Third_Party;
        public float QualityBonusThreshold = 0.9f;
        public float QualityBonusAmount = 500f;
    }
    
    [System.Serializable]
    public class ProductSpecification
    {
        public string ProductName;
        public ProductType ProductType;
        [Range(0.1f, 1000f)] public float QuantityKg = 10f;
        public Vector2 THCRange = new Vector2(15f, 25f);
        public Vector2 CBDRange = new Vector2(0f, 5f);
        public Vector2 MoistureRange = new Vector2(6f, 8f);
        public List<string> PreferredStrains = new List<string>();
    }
    
    [System.Serializable]
    public class PostHarvestProcessingRequirements
    {
        public bool RequiresControlledDrying = true;
        public Vector2 DryingTemperatureRange = new Vector2(18f, 22f);
        public Vector2 DryingHumidityRange = new Vector2(55f, 65f);
        [Range(5, 30)] public int MinCuringDays = 14;
        [Range(30, 90)] public int MaxCuringDays = 60;
        public bool RequiresTrimming = true;
        public TrimmingStandard TrimmingStandard = TrimmingStandard.Hand_Trimmed;
        public bool RequiresGrading = true;
        public PackagingRequirement PackagingRequirement = PackagingRequirement.Vacuum_Sealed;
    }
    
    [System.Serializable]
    public class ProcessingMethod
    {
        public ProcessingType ProcessingType;
        public bool IsRequired = true;
        public float QualityImpact = 1f;
        public float CostImpact = 1f;
        public string ProcessingDescription;
    }
    
    [System.Serializable]
    public class QualityControlStep
    {
        public string StepName;
        public QCStepType StepType;
        public bool IsCriticalStep = false;
        public float AcceptanceThreshold = 0.8f;
        public string StepDescription;
    }
    
    [System.Serializable]
    public class ContractRisk
    {
        public string RiskName;
        public RiskType RiskType;
        [Range(0f, 1f)] public float Probability = 0.1f;
        [Range(0f, 100000f)] public float PotentialLoss = 1000f;
        public string MitigationStrategy;
    }
    
    [System.Serializable]
    public class PenaltyClause
    {
        public string ViolationType;
        public float PenaltyAmount = 500f;
        [Range(0f, 1f)] public float ViolationProbability = 0.05f;
        public bool IsPercentage = false;
        public string PenaltyDescription;
    }
    
    [System.Serializable]
    public class BonusIncentive
    {
        public string IncentiveName;
        public float BonusAmount = 1000f;
        [Range(0f, 1f)] public float AchievementProbability = 0.3f;
        public string AchievementCriteria;
    }
    
    [System.Serializable]
    public class MarketConditionRequirements
    {
        public bool IsMarketSensitive = false;
        public float MinMarketPrice = 10f;
        public float MaxMarketPrice = 50f;
        public bool RequiresStableSupply = true;
        public bool AllowsPriceAdjustments = true;
    }
    
    [System.Serializable]
    public class ComplianceClause
    {
        public string ComplianceName;
        public ComplianceType ComplianceType;
        public bool IsMandatory = true;
        public float NonCompliancePenalty = 2000f;
        public string ComplianceDescription;
    }
    
    [System.Serializable]
    public class CertificationRequirement
    {
        public CertificationType CertificationType;
        public bool IsRequired = true;
        public float CertificationCost = 500f;
        public int ValidityDays = 365;
        public string RequirementDescription;
    }
    
    [System.Serializable]
    public class SustainabilityRequirements
    {
        public bool RequiresOrganicMethods = false;
        public bool RequiresRenewableEnergy = false;
        public bool RequiresWaterConservation = false;
        public bool RequiresWasteReduction = false;
        [Range(0f, 200f)] public float MaxCarbonFootprint = 100f;
        public bool RequiresSustainabilityCertification = false;
    }
    
    [System.Serializable]
    public class EnvironmentalStandard
    {
        public string StandardName;
        public EnvironmentalCategory Category;
        public float MaxAllowedValue = 100f;
        public string MeasurementUnit = "kg CO2";
        public string StandardDescription;
    }
    
    [System.Serializable]
    public class PlayerCapabilities
    {
        public float ProductionCapacity = 100f; // kg per harvest
        public float AverageQualityScore = 0.75f;
        public List<ProcessingType> AvailableProcessingMethods = new List<ProcessingType>();
        public List<CertificationType> Certifications = new List<CertificationType>();
        public bool HasOrganicCertification = false;
        public bool HasAdvancedEquipment = false;
    }
    
    [System.Serializable]
    public class ContractFeasibility
    {
        public bool CanMeetQuantity;
        public bool CanMeetQuality;
        public bool CanMeetProcessing;
        public bool CanMeetCompliance;
        public float FeasibilityScore;
    }
    
    public enum ContractType
    {
        Supply_Agreement,
        Processing_Contract,
        Distribution_Deal,
        Licensing_Agreement,
        Research_Contract,
        Consulting_Agreement,
        Equipment_Lease,
        Service_Contract
    }
    
    public enum ContractCategory
    {
        Cultivation,
        Processing,
        Distribution,
        Retail,
        Research,
        Consulting,
        Equipment,
        Services
    }
    
    public enum PaymentTerms
    {
        COD, // Cash on Delivery
        Net_15,
        Net_30,
        Net_60,
        Net_90,
        Immediate,
        Progressive
    }
    
    public enum DeliveryMethod
    {
        Pickup,
        Delivery,
        Shipping,
        Third_Party_Logistics,
        Customer_Pickup
    }
    
    public enum TestingRequirement
    {
        None,
        Self_Testing,
        Third_Party,
        Government_Lab,
        Multiple_Labs
    }
    
    public enum TrimmingStandard
    {
        Machine_Trimmed,
        Hand_Trimmed,
        Premium_Hand_Trimmed,
        Whole_Plant,
        Selective_Trimming
    }
    
    public enum PackagingRequirement
    {
        Basic,
        Vacuum_Sealed,
        Nitrogen_Flushed,
        Light_Protected,
        Humidity_Controlled,
        Premium_Packaging
    }
    
    public enum ProcessingType
    {
        Drying,
        Curing,
        Trimming,
        Extraction,
        Purification,
        Distillation,
        Crystallization,
        Encapsulation,
        Infusion,
        Pressing
    }
    
    public enum QCStepType
    {
        Visual_Inspection,
        Weight_Check,
        Moisture_Test,
        Potency_Test,
        Contaminant_Test,
        Terpene_Analysis,
        Microbial_Test,
        Heavy_Metal_Test
    }
    
    public enum RiskType
    {
        Quality_Risk,
        Delivery_Risk,
        Payment_Risk,
        Regulatory_Risk,
        Market_Risk,
        Operational_Risk,
        Environmental_Risk,
        Compliance_Risk
    }
    
    public enum EnvironmentalCategory
    {
        Carbon_Footprint,
        Water_Usage,
        Energy_Consumption,
        Waste_Generation,
        Chemical_Usage,
        Land_Use
    }
}