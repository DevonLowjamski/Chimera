using UnityEngine;
using ProjectChimera.Shared;
using ProjectChimera.Data.Shared;
using System;

namespace ProjectChimera.Data.Economy
{
    /// <summary>
    /// Simplified contract structure for Phase 8 MVP
    /// Focuses on core contract generation, tracking, and completion mechanics
    /// </summary>
    [CreateAssetMenu(fileName = "New Active Contract", menuName = "Project Chimera/Economy/Active Contract")]
    public class ActiveContractSO : ChimeraDataSO
    {
        [Header("Contract Identity")]
        [SerializeField] private string _contractId;
        [SerializeField] private string _contractorName;
        [SerializeField] private string _contractTitle;
        [SerializeField, TextArea(2, 3)] private string _description;
        
        [Header("Product Requirements")]
        [SerializeField] private StrainType _requiredStrainType = StrainType.Hybrid;
        [SerializeField] private string _specificStrainName; // Optional specific strain
        [SerializeField] private float _quantityRequired = 10f; // kg
        [SerializeField] private float _minimumQuality = 0.7f; // 0-1 scale
        
        [Header("Contract Terms")]
        [SerializeField] private float _contractValue = 5000f; // Total payment
        [SerializeField] private int _timeWindowDays = 30; // Days to complete
        [SerializeField] private DateTime _contractStartDate;
        [SerializeField] private DateTime _deadline;
        
        [Header("Contract Status")]
        [SerializeField] private ContractStatus _status = ContractStatus.Available;
        [SerializeField] private float _completionProgress = 0f; // 0-1 scale
        [SerializeField] private DateTime _acceptedDate;
        [SerializeField] private DateTime _completedDate;
        
        [Header("Quality and Bonuses")]
        [SerializeField] private float _qualityBonusThreshold = 0.9f;
        [SerializeField] private float _qualityBonusAmount = 1000f;
        [SerializeField] private bool _earlyCompletionBonus = true;
        [SerializeField] private float _earlyBonusAmount = 500f;
        [SerializeField] private int _earlyBonusDays = 7; // Days early for bonus
        
        [Header("Penalties")]
        [SerializeField] private float _latePenaltyPerDay = 100f;
        [SerializeField] private float _qualityPenaltyThreshold = 0.6f;
        [SerializeField] private float _qualityPenaltyAmount = 500f;
        
        // Public Properties
        public string ContractId => _contractId;
        public string ContractorName => _contractorName;
        public string ContractTitle => _contractTitle;
        public string Description => _description;
        public StrainType RequiredStrainType => _requiredStrainType;
        public StrainType RequiredStrain => _requiredStrainType; // Alias for RequiredStrainType
        public string SpecificStrainName => _specificStrainName;
        public float QuantityRequired => _quantityRequired;
        public float RequiredQuantity => _quantityRequired; // Alias for QuantityRequired
        public float MinimumQuality => _minimumQuality;
        public float ContractValue => _contractValue;
        public float TotalValue => _contractValue; // Alias for ContractValue
        public int TimeWindowDays => _timeWindowDays;
        public DateTime ContractStartDate => _contractStartDate;
        public DateTime Deadline => _deadline;
        public ContractStatus Status => _status;
        public float CompletionProgress => _completionProgress;
        public DateTime AcceptedDate => _acceptedDate;
        public DateTime CompletedDate => _completedDate;
        public float QualityBonusThreshold => _qualityBonusThreshold;
        public float QualityBonusAmount => _qualityBonusAmount;
        public bool EarlyCompletionBonus => _earlyCompletionBonus;
        public float EarlyBonusAmount => _earlyBonusAmount;
        public int EarlyBonusDays => _earlyBonusDays;
        public float LatePenaltyPerDay => _latePenaltyPerDay;
        public float QualityPenaltyThreshold => _qualityPenaltyThreshold;
        public float QualityPenaltyAmount => _qualityPenaltyAmount;
        
        /// <summary>
        /// Initialize a new contract with generated values
        /// </summary>
        public void InitializeContract(string contractorName, StrainType strainType, float quantity, 
                                     float quality, float value, int timeWindow)
        {
            _contractId = System.Guid.NewGuid().ToString();
            _contractorName = contractorName;
            _requiredStrainType = strainType;
            _quantityRequired = quantity;
            _minimumQuality = quality;
            _contractValue = value;
            _timeWindowDays = timeWindow;
            _contractStartDate = DateTime.Now;
            _deadline = _contractStartDate.AddDays(timeWindow);
            _status = ContractStatus.Available;
            _completionProgress = 0f;
            
            // Generate contract title
            _contractTitle = $"{contractorName} - {quantity}kg {strainType}";
            _description = $"Supply {quantity}kg of {strainType} cannabis with minimum {quality:P0} quality within {timeWindow} days.";
        }
        
        /// <summary>
        /// Accept the contract and start tracking
        /// </summary>
        public bool AcceptContract()
        {
            if (_status != ContractStatus.Available)
                return false;
                
            _status = ContractStatus.Active;
            _acceptedDate = DateTime.Now;
            
            // Recalculate deadline from acceptance date if needed
            _deadline = _acceptedDate.AddDays(_timeWindowDays);
            
            return true;
        }
        
        /// <summary>
        /// Update contract progress
        /// </summary>
        public void UpdateProgress(float deliveredQuantity, float averageQuality)
        {
            if (_status != ContractStatus.Active)
                return;
                
            _completionProgress = Mathf.Clamp01(deliveredQuantity / _quantityRequired);
            
            // Check if contract is completed
            if (_completionProgress >= 1f && averageQuality >= _minimumQuality)
            {
                CompleteContract(averageQuality);
            }
        }
        
        /// <summary>
        /// Complete the contract and calculate final payout
        /// </summary>
        public ContractCompletionResult CompleteContract(float finalQuality)
        {
            if (_status != ContractStatus.Active)
                return new ContractCompletionResult { Success = false, Reason = "Contract not active" };
                
            _status = ContractStatus.Completed;
            _completedDate = DateTime.Now;
            
            var result = CalculateFinalPayout(finalQuality);
            return result;
        }
        
        /// <summary>
        /// Calculate final payout including bonuses and penalties
        /// </summary>
        public ContractCompletionResult CalculateFinalPayout(float finalQuality)
        {
            var result = new ContractCompletionResult
            {
                Success = true,
                BasePayment = _contractValue,
                FinalQuality = finalQuality
            };
            
            float totalPayout = _contractValue;
            
            // Quality bonus
            if (finalQuality >= _qualityBonusThreshold)
            {
                result.QualityBonus = _qualityBonusAmount;
                totalPayout += _qualityBonusAmount;
            }
            
            // Quality penalty
            if (finalQuality < _qualityPenaltyThreshold)
            {
                result.QualityPenalty = _qualityPenaltyAmount;
                totalPayout -= _qualityPenaltyAmount;
            }
            
            // Early completion bonus
            if (_earlyCompletionBonus && _completedDate <= _deadline.AddDays(-_earlyBonusDays))
            {
                result.EarlyBonus = _earlyBonusAmount;
                totalPayout += _earlyBonusAmount;
            }
            
            // Late penalty
            if (_completedDate > _deadline)
            {
                int daysLate = (_completedDate - _deadline).Days;
                result.LatePenalty = daysLate * _latePenaltyPerDay;
                totalPayout -= result.LatePenalty;
            }
            
            result.TotalPayout = Mathf.Max(0f, totalPayout); // Ensure non-negative
            return result;
        }
        
        /// <summary>
        /// Check if contract has expired
        /// </summary>
        public bool IsExpired()
        {
            return DateTime.Now > _deadline && _status == ContractStatus.Active;
        }
        
        /// <summary>
        /// Get days remaining until deadline
        /// </summary>
        public int GetDaysRemaining()
        {
            if (_status != ContractStatus.Active)
                return 0;
                
            var remaining = _deadline - DateTime.Now;
            return Mathf.Max(0, remaining.Days);
        }
        
        /// <summary>
        /// Get contract difficulty score based on requirements
        /// </summary>
        public float GetDifficultyScore()
        {
            float difficulty = 0f;
            
            // Quantity difficulty (normalized to typical range)
            difficulty += Mathf.Clamp01(_quantityRequired / 100f) * 0.3f;
            
            // Quality difficulty
            difficulty += _minimumQuality * 0.4f;
            
            // Time pressure difficulty
            difficulty += (1f - Mathf.Clamp01(_timeWindowDays / 60f)) * 0.3f;
            
            return Mathf.Clamp01(difficulty);
        }
        
        protected override bool ValidateDataSpecific()
        {
            bool isValid = true;
            
            if (string.IsNullOrEmpty(_contractorName))
            {
                Debug.LogError($"ActiveContract {name}: Contractor name cannot be empty", this);
                isValid = false;
            }
            
            if (_quantityRequired <= 0f)
            {
                Debug.LogError($"ActiveContract {name}: Quantity required must be positive", this);
                isValid = false;
            }
            
            if (_contractValue <= 0f)
            {
                Debug.LogError($"ActiveContract {name}: Contract value must be positive", this);
                isValid = false;
            }
            
            if (_timeWindowDays <= 0)
            {
                Debug.LogError($"ActiveContract {name}: Time window must be positive", this);
                isValid = false;
            }
            
            if (_minimumQuality < 0f || _minimumQuality > 1f)
            {
                Debug.LogError($"ActiveContract {name}: Minimum quality must be between 0 and 1", this);
                isValid = false;
            }
            
            return isValid;
        }
    }
    
    /// <summary>
    /// Contract completion result with payout breakdown
    /// </summary>
    [System.Serializable]
    public class ContractCompletionResult
    {
        public bool Success;
        public string Reason;
        public float BasePayment;
        public float QualityBonus;
        public float EarlyBonus;
        public float QualityPenalty;
        public float LatePenalty;
        public float TotalPayout;
        public float FinalQuality;
        
        public string GetSummary()
        {
            if (!Success)
                return $"Contract failed: {Reason}";
                
            string summary = $"Contract completed! Base: ${BasePayment:F0}";
            
            if (QualityBonus > 0)
                summary += $", Quality Bonus: +${QualityBonus:F0}";
            if (EarlyBonus > 0)
                summary += $", Early Bonus: +${EarlyBonus:F0}";
            if (QualityPenalty > 0)
                summary += $", Quality Penalty: -${QualityPenalty:F0}";
            if (LatePenalty > 0)
                summary += $", Late Penalty: -${LatePenalty:F0}";
                
            summary += $". Total: ${TotalPayout:F0}";
            return summary;
        }
    }
    
    /// <summary>
    /// Contract status enumeration
    /// </summary>
    public enum ContractStatus
    {
        Available,      // Available to accept
        Active,         // Accepted and in progress
        Completed,      // Successfully completed
        Failed,         // Failed to complete
        Expired,        // Expired without completion
        Cancelled       // Cancelled by player or contractor
    }
}