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
    /// Manages contract lifecycle operations for Project Chimera's cannabis cultivation contracts.
    /// Handles contract pools, acceptance, completion, expiration, and state management.
    /// </summary>
    public class ContractLifecycleManager : MonoBehaviour
    {
        [Header("Lifecycle Configuration")]
        [SerializeField] private bool _enableDebugLogging = false;
        
        // Contract pool management
        private int _maxActiveContracts = 5;
        private int _contractPoolSize = 10;
        private float _contractRefreshRate = 0.3f; // 30% of contracts refresh daily
        private int _minimumContractsAvailable = 3;
        
        // Contract collections
        private List<ActiveContractSO> _availableContracts = new List<ActiveContractSO>();
        private List<ActiveContractSO> _activeContracts = new List<ActiveContractSO>();
        private Queue<ActiveContractSO> _contractPool = new Queue<ActiveContractSO>();
        
        // Service dependencies
        private CurrencyManager _currencyManager;
        
        // Events
        public System.Action<ActiveContractSO> OnContractAccepted;
        public System.Action<ActiveContractSO> OnContractExpired;
        public System.Action<ActiveContractSO> OnContractCompleted;
        public System.Action<int> OnAvailableContractsChanged;
        public System.Action<string> OnContractRefreshed;
        
        // Properties
        public int AvailableContractsCount => _availableContracts.Count;
        public int ActiveContractsCount => _activeContracts.Count;
        public int MaxActiveContracts => _maxActiveContracts;
        public bool CanAcceptMoreContracts => _activeContracts.Count < _maxActiveContracts;
        public List<ActiveContractSO> AvailableContracts => new List<ActiveContractSO>(_availableContracts);
        public List<ActiveContractSO> ActiveContracts => new List<ActiveContractSO>(_activeContracts);
        
        public void Initialize(int maxActiveContracts, int contractPoolSize, float contractRefreshRate, 
            int minimumContractsAvailable, CurrencyManager currencyManager)
        {
            _maxActiveContracts = maxActiveContracts;
            _contractPoolSize = contractPoolSize;
            _contractRefreshRate = contractRefreshRate;
            _minimumContractsAvailable = minimumContractsAvailable;
            _currencyManager = currencyManager;
            
            InitializeContractPools();
            
            LogInfo("Contract lifecycle manager initialized for cannabis cultivation contracts");
        }
        
        /// <summary>
        /// Accept a contract and move it to active state
        /// </summary>
        public bool AcceptContract(ActiveContractSO contract)
        {
            if (contract == null)
            {
                LogError("Cannot accept null contract");
                return false;
            }
            
            if (!_availableContracts.Contains(contract))
            {
                LogError($"Contract {contract.ContractId} is not available for acceptance");
                return false;
            }
            
            if (!CanAcceptMoreContracts)
            {
                LogWarning($"Cannot accept more contracts. Maximum active: {_maxActiveContracts}");
                return false;
            }
            
            if (!contract.AcceptContract())
            {
                LogError($"Failed to accept contract {contract.ContractId}");
                return false;
            }
            
            // Move from available to active
            _availableContracts.Remove(contract);
            _activeContracts.Add(contract);
            
            OnContractAccepted?.Invoke(contract);
            OnAvailableContractsChanged?.Invoke(_availableContracts.Count);
            
            LogInfo($"Accepted contract: {contract.ContractTitle}");
            return true;
        }
        
        /// <summary>
        /// Complete a contract and handle payout
        /// </summary>
        public bool CompleteContract(ActiveContractSO contract, float deliveredQuantity, float averageQuality)
        {
            if (contract == null || !_activeContracts.Contains(contract))
            {
                LogError("Contract not found in active contracts");
                return false;
            }
            
            // Validate completion requirements
            if (deliveredQuantity < contract.QuantityRequired)
            {
                LogWarning($"Insufficient quantity delivered: {deliveredQuantity}/{contract.QuantityRequired}");
                return false;
            }
            
            if (averageQuality < contract.MinimumQuality)
            {
                LogWarning($"Quality too low: {averageQuality:P1} < {contract.MinimumQuality:P1}");
                return false;
            }
            
            var result = contract.CompleteContract(averageQuality);
            
            if (result.Success)
            {
                // Process comprehensive payout through CurrencyManager
                ProcessContractPayout(contract, result);
                
                // Remove from active contracts
                _activeContracts.Remove(contract);
                
                OnContractCompleted?.Invoke(contract);
                
                LogInfo($"Contract completed: {contract.ContractTitle} - Payout: ${result.TotalPayout:F2}");
                LogInfo(result.GetSummary());
                
                return true;
            }
            
            LogError($"Failed to complete contract: {result.Reason}");
            return false;
        }
        
        /// <summary>
        /// Add contracts to the available pool
        /// </summary>
        public void AddAvailableContracts(List<ActiveContractSO> contracts)
        {
            if (contracts == null) return;
            
            int addedCount = 0;
            foreach (var contract in contracts)
            {
                if (contract != null && !_availableContracts.Contains(contract))
                {
                    _availableContracts.Add(contract);
                    addedCount++;
                }
            }
            
            if (addedCount > 0)
            {
                OnAvailableContractsChanged?.Invoke(_availableContracts.Count);
                LogInfo($"Added {addedCount} contracts to available pool");
            }
        }
        
        /// <summary>
        /// Refresh contract pool by removing old contracts
        /// </summary>
        public void RefreshContractPool()
        {
            // Calculate how many contracts to refresh
            int contractsToRefresh = Mathf.RoundToInt(_availableContracts.Count * _contractRefreshRate);
            contractsToRefresh = Mathf.Max(1, contractsToRefresh); // At least 1
            
            // Remove oldest contracts
            int removedCount = 0;
            for (int i = 0; i < contractsToRefresh && _availableContracts.Count > _minimumContractsAvailable; i++)
            {
                var oldestContract = _availableContracts[0];
                _availableContracts.RemoveAt(0);
                removedCount++;
                LogInfo($"Expired contract: {oldestContract.ContractTitle}");
            }
            
            if (removedCount > 0)
            {
                OnAvailableContractsChanged?.Invoke(_availableContracts.Count);
                OnContractRefreshed?.Invoke($"Refreshed {removedCount} contracts from pool");
            }
        }
        
        /// <summary>
        /// Update contract expiration for active contracts
        /// </summary>
        public void UpdateContractExpiration()
        {
            var expiredContracts = _activeContracts.Where(c => c.IsExpired()).ToList();
            
            foreach (var contract in expiredContracts)
            {
                _activeContracts.Remove(contract);
                OnContractExpired?.Invoke(contract);
                LogInfo($"Contract expired: {contract.ContractTitle}");
            }
        }
        
        /// <summary>
        /// Ensure minimum number of contracts are always available
        /// </summary>
        public int EnsureMinimumContracts()
        {
            if (_availableContracts.Count < _minimumContractsAvailable)
            {
                int needed = _minimumContractsAvailable - _availableContracts.Count;
                LogInfo($"Need {needed} contracts to maintain minimum of {_minimumContractsAvailable}");
                return needed;
            }
            
            return 0;
        }
        
        /// <summary>
        /// Get available contracts filtered by criteria
        /// </summary>
        public List<ActiveContractSO> GetAvailableContracts(StrainType? strainFilter = null, float? maxDifficulty = null)
        {
            var filtered = _availableContracts.AsEnumerable();
            
            if (strainFilter.HasValue)
            {
                filtered = filtered.Where(c => c.RequiredStrainType == strainFilter.Value);
            }
            
            if (maxDifficulty.HasValue)
            {
                filtered = filtered.Where(c => c.GetDifficultyScore() <= maxDifficulty.Value);
            }
            
            return filtered.ToList();
        }
        
        /// <summary>
        /// Check if a specific contract is available
        /// </summary>
        public bool IsContractAvailable(ActiveContractSO contract)
        {
            return contract != null && _availableContracts.Contains(contract);
        }
        
        /// <summary>
        /// Check if a specific contract is active
        /// </summary>
        public bool IsContractActive(ActiveContractSO contract)
        {
            return contract != null && _activeContracts.Contains(contract);
        }
        
        /// <summary>
        /// Get contract lifecycle statistics
        /// </summary>
        public ContractLifecycleStats GetLifecycleStats()
        {
            return new ContractLifecycleStats
            {
                AvailableContracts = _availableContracts.Count,
                ActiveContracts = _activeContracts.Count,
                MaxActiveContracts = _maxActiveContracts,
                MinimumContractsAvailable = _minimumContractsAvailable,
                ContractPoolSize = _contractPoolSize,
                ContractRefreshRate = _contractRefreshRate,
                CanAcceptMoreContracts = CanAcceptMoreContracts
            };
        }
        
        /// <summary>
        /// Clear all contracts (for testing or reset)
        /// </summary>
        public void ClearAllContracts()
        {
            _availableContracts.Clear();
            _activeContracts.Clear();
            _contractPool.Clear();
            
            OnAvailableContractsChanged?.Invoke(0);
            LogInfo("All contracts cleared");
        }
        
        private void InitializeContractPools()
        {
            _contractPool.Clear();
            _availableContracts.Clear();
            _activeContracts.Clear();
            
            LogInfo($"Initialized contract pools - Max Active: {_maxActiveContracts}, Pool Size: {_contractPoolSize}");
        }
        
        private void ProcessContractPayout(ActiveContractSO contract, ContractCompletionResult result)
        {
            if (_currencyManager == null)
            {
                LogWarning("CurrencyManager not available - contract payouts will not be processed");
                return;
            }
            
            // Process cash payout
            if (result.TotalPayout > 0)
            {
                _currencyManager.AddCurrency(CurrencyType.Cash, result.TotalPayout, 
                    $"Contract completion: {contract.ContractTitle}", TransactionCategory.Operations);
            }
            
            // Award skill points for contract completion
            float skillPointReward = CalculateSkillPointReward(contract, result);
            if (skillPointReward > 0)
            {
                _currencyManager.AwardSkillPoints(skillPointReward, 
                    $"Contract completion: {contract.ContractTitle}");
            }
            
            // Bonus skill points for quality excellence
            if (result.FinalQuality >= contract.QualityBonusThreshold)
            {
                float qualityBonus = skillPointReward * 0.5f; // 50% bonus for high quality
                _currencyManager.AwardSkillPoints(qualityBonus, 
                    $"Quality excellence bonus: {contract.ContractTitle}");
            }
            
            // Early completion bonus skill points
            if (result.EarlyBonus > 0)
            {
                float earlyBonus = skillPointReward * 0.25f; // 25% bonus for early completion
                _currencyManager.AwardSkillPoints(earlyBonus, 
                    $"Early completion bonus: {contract.ContractTitle}");
            }
            
            LogInfo($"Processed contract payout: ${result.TotalPayout:F2} cash, {skillPointReward:F1} skill points");
        }
        
        private float CalculateSkillPointReward(ActiveContractSO contract, ContractCompletionResult result)
        {
            // Base skill points based on contract value (1 SP per $1000)
            float baseReward = contract.ContractValue / 1000f;
            
            // Difficulty multiplier based on contract complexity
            float difficultyMultiplier = contract.GetDifficultyScore() + 0.5f; // 0.5 to 1.5 range
            
            // Quality multiplier (0.5x to 2.0x based on quality)
            float qualityMultiplier = Mathf.Lerp(0.5f, 2.0f, result.FinalQuality);
            
            // Calculate final reward
            float totalReward = baseReward * difficultyMultiplier * qualityMultiplier;
            
            // Ensure minimum and maximum bounds
            return Mathf.Clamp(totalReward, 1f, 50f); // 1-50 skill points per contract
        }
        
        private void LogInfo(string message)
        {
            if (_enableDebugLogging)
                Debug.Log($"[ContractLifecycleManager] {message}");
        }
        
        private void LogWarning(string message)
        {
            if (_enableDebugLogging)
                Debug.LogWarning($"[ContractLifecycleManager] {message}");
        }
        
        private void LogError(string message)
        {
            if (_enableDebugLogging)
                Debug.LogError($"[ContractLifecycleManager] {message}");
        }
    }
    
    /// <summary>
    /// Statistics for contract lifecycle management
    /// </summary>
    [System.Serializable]
    public class ContractLifecycleStats
    {
        public int AvailableContracts;
        public int ActiveContracts;
        public int MaxActiveContracts;
        public int MinimumContractsAvailable;
        public int ContractPoolSize;
        public float ContractRefreshRate;
        public bool CanAcceptMoreContracts;
    }
}