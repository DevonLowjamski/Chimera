using ProjectChimera.Core.Logging;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Economy;
using ProjectChimera.Data.Shared;
using System.Collections.Generic;
using System;

namespace ProjectChimera.Systems.Economy.Components
{
    /// <summary>
    /// Handles contract template processing and parameter generation for Project Chimera's cannabis cultivation contracts.
    /// Manages difficulty scaling, market demand adjustments, and contract instance creation.
    /// </summary>
    public class ContractTemplateService : MonoBehaviour
    {
        [Header("Template Configuration")]
        [SerializeField] private bool _enableDebugLogging = false;
        
        // Service dependencies
        private ContractGenerationTemplateSO _generationTemplate;
        private CurrencyManager _currencyManager;
        
        // Difficulty scaling parameters
        private float _basePlayerLevel = 1f;
        private float _difficultyScaleRate = 1.2f;
        private Vector2 _quantityScaleRange = new Vector2(0.8f, 1.5f);
        private Vector2 _qualityScaleRange = new Vector2(0.9f, 1.1f);
        
        // Player progression tracking
        private float _playerDifficultyLevel = 1f;
        
        // Generation tracking
        private Dictionary<string, int> _contractorRequestCounts = new Dictionary<string, int>();
        private Dictionary<StrainType, int> _strainDemandCounts = new Dictionary<StrainType, int>();
        
        // Events
        public System.Action<ActiveContractSO> OnContractCreated;
        public System.Action<ContractGenerationParameters> OnParametersGenerated;
        
        // Properties
        public float PlayerDifficultyLevel => _playerDifficultyLevel;
        public Dictionary<string, int> ContractorRequestCounts => new Dictionary<string, int>(_contractorRequestCounts);
        public Dictionary<StrainType, int> StrainDemandCounts => new Dictionary<StrainType, int>(_strainDemandCounts);
        
        public void Initialize(ContractGenerationTemplateSO template, CurrencyManager currencyManager, 
            float basePlayerLevel, float difficultyScaleRate, Vector2 quantityScaleRange, Vector2 qualityScaleRange)
        {
            _generationTemplate = template;
            _currencyManager = currencyManager;
            _basePlayerLevel = basePlayerLevel;
            _difficultyScaleRate = difficultyScaleRate;
            _quantityScaleRange = quantityScaleRange;
            _qualityScaleRange = qualityScaleRange;
            
            InitializeTrackingDictionaries();
            
            LogInfo("Contract template service initialized for cannabis cultivation contracts");
        }
        
        /// <summary>
        /// Generate contract parameters using the template system
        /// </summary>
        public ContractGenerationParameters GenerateContractParameters()
        {
            if (_generationTemplate == null)
            {
                LogError("No contract generation template assigned");
                return null;
            }
            
            var parameters = _generationTemplate.GenerateRandomContract();
            OnParametersGenerated?.Invoke(parameters);
            
            return parameters;
        }
        
        /// <summary>
        /// Apply difficulty scaling to contract parameters
        /// </summary>
        public void ApplyDifficultyScaling(ContractGenerationParameters parameters)
        {
            if (parameters == null) return;
            
            float difficultyScale = Mathf.Pow(_difficultyScaleRate, _playerDifficultyLevel - 1f);
            
            // Scale quantity requirements
            float quantityMultiplier = Mathf.Lerp(_quantityScaleRange.x, _quantityScaleRange.y, 
                (_playerDifficultyLevel - 1f) / 4f);
            parameters.Quantity *= quantityMultiplier;
            
            // Scale quality requirements
            float qualityMultiplier = Mathf.Lerp(_qualityScaleRange.x, _qualityScaleRange.y,
                (_playerDifficultyLevel - 1f) / 4f);
            parameters.MinimumQuality *= qualityMultiplier;
            parameters.MinimumQuality = Mathf.Clamp(parameters.MinimumQuality, 0.5f, 0.95f);
            
            // Scale contract value to match increased difficulty
            parameters.ContractValue *= difficultyScale;
            
            LogInfo($"Applied difficulty scaling: Level {_playerDifficultyLevel:F1}, Scale {difficultyScale:F2}");
        }
        
        /// <summary>
        /// Apply market demand adjustments to contract parameters
        /// </summary>
        public void ApplyMarketDemand(ContractGenerationParameters parameters)
        {
            if (parameters == null) return;
            
            // Apply market demand multiplier based on strain popularity
            if (_strainDemandCounts.TryGetValue(parameters.StrainType, out int demandCount))
            {
                int totalDemand = 0;
                foreach (var count in _strainDemandCounts.Values)
                    totalDemand += count;
                
                if (totalDemand > 0)
                {
                    float demandRatio = (float)demandCount / totalDemand;
                    
                    // Reduce value for oversupplied strains, increase for rare ones
                    if (demandRatio > 0.4f) // High demand
                    {
                        parameters.ContractValue *= 0.85f; // Reduce price due to oversupply
                        LogInfo($"Applied oversupply adjustment for {parameters.StrainType}: {demandRatio:P1}");
                    }
                    else if (demandRatio < 0.2f) // Low demand
                    {
                        parameters.ContractValue *= 1.15f; // Increase price due to scarcity
                        LogInfo($"Applied scarcity bonus for {parameters.StrainType}: {demandRatio:P1}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Apply difficulty tier adjustments to contract parameters
        /// </summary>
        public void ApplyDifficultyTierAdjustments(ContractGenerationParameters parameters, ContractDifficultyTier tier)
        {
            if (parameters == null) return;
            
            float difficultyMultiplier = 1f + ((int)tier * 0.3f); // 1.0, 1.3, 1.6, 1.9, 2.2
            
            // Adjust quantity (higher difficulty = more product needed)
            parameters.Quantity *= Mathf.Lerp(0.8f, 1.8f, (int)tier / 4f);
            
            // Adjust quality requirements
            float baseQuality = parameters.MinimumQuality;
            parameters.MinimumQuality = Mathf.Lerp(baseQuality, Mathf.Min(0.95f, baseQuality + 0.2f), (int)tier / 4f);
            
            // Adjust time window (higher difficulty = shorter time)
            parameters.TimeWindowDays = Mathf.RoundToInt(parameters.TimeWindowDays * Mathf.Lerp(1.2f, 0.6f, (int)tier / 4f));
            parameters.TimeWindowDays = Mathf.Max(1, parameters.TimeWindowDays);
            
            // Adjust contract value to match difficulty
            parameters.ContractValue *= difficultyMultiplier;
            
            LogInfo($"Applied {tier} tier adjustments: x{difficultyMultiplier:F1} value, {parameters.TimeWindowDays} days");
        }
        
        /// <summary>
        /// Apply progressive unlocking restrictions
        /// </summary>
        public void ApplyProgressiveUnlockingRestrictions(ContractGenerationParameters parameters)
        {
            if (parameters == null) return;
            
            // Limit high-tier contracts until player reaches appropriate level
            float difficultyScore = CalculateParameterDifficultyScore(parameters);
            float maxAllowedDifficulty = _playerDifficultyLevel / 5f; // 0.2 to 1.0
            
            if (difficultyScore > maxAllowedDifficulty)
            {
                float reductionFactor = maxAllowedDifficulty / difficultyScore;
                
                parameters.Quantity *= reductionFactor;
                parameters.MinimumQuality = Mathf.Lerp(0.5f, parameters.MinimumQuality, reductionFactor);
                parameters.ContractValue *= reductionFactor;
                parameters.TimeWindowDays = Mathf.RoundToInt(parameters.TimeWindowDays / reductionFactor);
                parameters.TimeWindowDays = Mathf.Max(1, parameters.TimeWindowDays);
                
                LogInfo($"Applied progressive restrictions: {reductionFactor:F2} reduction factor");
            }
        }
        
        /// <summary>
        /// Create a contract ScriptableObject instance from parameters
        /// </summary>
        public ActiveContractSO CreateContractInstance(ContractGenerationParameters parameters)
        {
            if (parameters == null) return null;
            
            // Create a new ScriptableObject instance
            var contract = ScriptableObject.CreateInstance<ActiveContractSO>();
            
            // Initialize with generated parameters
            contract.InitializeContract(
                parameters.Contractor.ContractorName,
                parameters.StrainType,
                parameters.Quantity,
                parameters.MinimumQuality,
                parameters.ContractValue,
                parameters.TimeWindowDays
            );
            
            // Set additional properties if available
            if (!string.IsNullOrEmpty(parameters.SpecificStrain))
            {
                LogInfo($"Contract prefers specific strain: {parameters.SpecificStrain}");
            }
            
            // Update tracking
            UpdateGenerationTracking(contract);
            OnContractCreated?.Invoke(contract);
            
            LogInfo($"Created contract: {contract.ContractTitle} - ${parameters.ContractValue:F2}");
            return contract;
        }
        
        /// <summary>
        /// Calculate skill point reward based on contract difficulty and performance
        /// </summary>
        public float CalculateSkillPointReward(ActiveContractSO contract, ContractCompletionResult result)
        {
            if (contract == null || result == null) return 0f;
            
            // Base skill points based on contract value (1 SP per $1000)
            float baseReward = contract.ContractValue / 1000f;
            
            // Difficulty multiplier based on contract complexity
            float difficultyMultiplier = contract.GetDifficultyScore() + 0.5f; // 0.5 to 1.5 range
            
            // Quality multiplier (0.5x to 2.0x based on quality)
            float qualityMultiplier = Mathf.Lerp(0.5f, 2.0f, result.FinalQuality);
            
            // Calculate final reward
            float totalReward = baseReward * difficultyMultiplier * qualityMultiplier;
            
            // Ensure minimum and maximum bounds
            float clampedReward = Mathf.Clamp(totalReward, 1f, 50f); // 1-50 skill points per contract
            
            LogInfo($"Skill reward calculation: Base {baseReward:F1} x Difficulty {difficultyMultiplier:F1} x Quality {qualityMultiplier:F1} = {clampedReward:F1} SP");
            return clampedReward;
        }
        
        /// <summary>
        /// Update player difficulty level based on progression
        /// </summary>
        public void UpdatePlayerDifficultyLevel()
        {
            // Update player difficulty based on completed contracts or other progression metrics
            if (_currencyManager != null)
            {
                float playerWealth = _currencyManager.Cash + _currencyManager.SkillPoints * 100f;
                _playerDifficultyLevel = _basePlayerLevel + (playerWealth / 10000f) * 0.1f;
                _playerDifficultyLevel = Mathf.Clamp(_playerDifficultyLevel, 1f, 5f);
            }
        }
        
        /// <summary>
        /// Calculate difficulty score for contract parameters
        /// </summary>
        public float CalculateParameterDifficultyScore(ContractGenerationParameters parameters)
        {
            if (parameters == null) return 0f;
            
            float score = 0f;
            
            // Quantity difficulty (normalized to 0-1)
            score += Mathf.Clamp01(parameters.Quantity / 1000f) * 0.3f;
            
            // Quality difficulty
            score += parameters.MinimumQuality * 0.4f;
            
            // Time pressure difficulty
            score += Mathf.Clamp01((30f - parameters.TimeWindowDays) / 29f) * 0.3f;
            
            return Mathf.Clamp01(score);
        }
        
        /// <summary>
        /// Get template generation statistics
        /// </summary>
        public ContractTemplateStats GetTemplateStats()
        {
            return new ContractTemplateStats
            {
                PlayerDifficultyLevel = _playerDifficultyLevel,
                ContractorRequestCounts = new Dictionary<string, int>(_contractorRequestCounts),
                StrainDemandCounts = new Dictionary<StrainType, int>(_strainDemandCounts),
                TotalContractsGenerated = GetTotalContractsGenerated()
            };
        }
        
        private void InitializeTrackingDictionaries()
        {
            _contractorRequestCounts.Clear();
            _strainDemandCounts.Clear();
            
            foreach (StrainType strainType in Enum.GetValues(typeof(StrainType)))
            {
                _strainDemandCounts[strainType] = 0;
            }
        }
        
        private void UpdateGenerationTracking(ActiveContractSO contract)
        {
            if (contract == null) return;
            
            // Track contractor requests
            string contractorName = contract.ContractorName;
            if (!_contractorRequestCounts.ContainsKey(contractorName))
            {
                _contractorRequestCounts[contractorName] = 0;
            }
            _contractorRequestCounts[contractorName]++;
            
            // Track strain demand
            _strainDemandCounts[contract.RequiredStrainType]++;
        }
        
        private int GetTotalContractsGenerated()
        {
            int total = 0;
            foreach (var count in _contractorRequestCounts.Values)
                total += count;
            return total;
        }
        
        /// <summary>
        /// Updates strain demand based on market price changes
        /// </summary>
        public void UpdateStrainDemand(StrainType strain)
        {
            if (!_strainDemandCounts.ContainsKey(strain))
            {
                _strainDemandCounts[strain] = 0;
            }
            
            // Increase demand tracking for this strain
            _strainDemandCounts[strain]++;
            
            LogInfo($"Updated strain demand for {strain}. Current count: {_strainDemandCounts[strain]}");
        }

        private void LogInfo(string message)
        {
            if (_enableDebugLogging)
                ChimeraLogger.Log($"[ContractTemplateService] {message}");
        }
        
        private void LogError(string message)
        {
            if (_enableDebugLogging)
                ChimeraLogger.LogError($"[ContractTemplateService] {message}");
        }
    }
    
    /// <summary>
    /// Statistics for contract template generation
    /// </summary>
    [System.Serializable]
    public class ContractTemplateStats
    {
        public float PlayerDifficultyLevel;
        public Dictionary<string, int> ContractorRequestCounts;
        public Dictionary<StrainType, int> StrainDemandCounts;
        public int TotalContractsGenerated;
    }
}