using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Economy;
using ProjectChimera.Data.Shared;
using ProjectChimera.Systems.Economy.Components;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ProjectChimera.Systems.Economy
{
    /// <summary>
    /// Orchestrator for dynamic contract generation in Project Chimera's cannabis cultivation system.
    /// Coordinates specialized components for template processing, pricing algorithms, and lifecycle management
    /// to create procedural contracts based on game state and market dynamics.
    /// </summary>
    public class ContractGenerationService : ChimeraManager
    {
        [Header("Generation Configuration")]
        [SerializeField] private ContractGenerationTemplateSO _generationTemplate;
        [SerializeField] private int _maxActiveContracts = 5;
        [SerializeField] private int _contractGenerationInterval = 3600; // 1 hour in seconds
        [SerializeField] private bool _enableDynamicDifficulty = true;
        [SerializeField] private bool _enableSeasonalContracts = true;
        
        [Header("Contract Pool Management")]
        [SerializeField] private int _contractPoolSize = 10;
        [SerializeField] private float _contractRefreshRate = 0.3f; // 30% of contracts refresh daily
        [SerializeField] private int _minimumContractsAvailable = 3;
        
        [Header("Difficulty Scaling")]
        [SerializeField] private float _basePlayerLevel = 1f;
        [SerializeField] private float _difficultyScaleRate = 1.2f;
        [SerializeField] private Vector2 _quantityScaleRange = new Vector2(0.8f, 1.5f);
        [SerializeField] private Vector2 _qualityScaleRange = new Vector2(0.9f, 1.1f);
        
        [Header("Advanced Generation Algorithms")]
        [SerializeField] private bool _enableBalancedGeneration = true;
        [SerializeField] private bool _enableProgressiveUnlocking = true;
        [SerializeField] private float _strainVarietyWeight = 0.3f;
        [SerializeField] private float _difficultyDistributionWeight = 0.4f;
        [SerializeField] private float _valueBalanceWeight = 0.3f;
        [SerializeField] private AnimationCurve _difficultyProgressionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        [Header("Market Dynamics")]
        [SerializeField] private float _marketVolatility = 0.15f;
        [SerializeField] private int _marketTrendCycleDays = 7;
        [SerializeField] private bool _enableSeasonalAdjustments = true;
        [SerializeField] private Vector2 _competitionFactorRange = new Vector2(0.85f, 1.25f);
        
        // Specialized component managers
        private ContractTemplateService _templateService;
        private ContractPricingService _pricingService;
        private ContractLifecycleManager _lifecycleManager;
        
        // Service dependencies
        private float _lastGenerationTime;
        private CurrencyManager _currencyManager;
        
        public override ManagerPriority Priority => ManagerPriority.Normal;
        
        // Public Properties - delegated to components
        public int AvailableContractsCount => _lifecycleManager?.AvailableContractsCount ?? 0;
        public int ActiveContractsCount => _lifecycleManager?.ActiveContractsCount ?? 0;
        public int MaxActiveContracts => _maxActiveContracts;
        public bool CanAcceptMoreContracts => _lifecycleManager?.CanAcceptMoreContracts ?? false;
        public List<ActiveContractSO> AvailableContracts => _lifecycleManager?.AvailableContracts ?? new List<ActiveContractSO>();
        public List<ActiveContractSO> ActiveContracts => _lifecycleManager?.ActiveContracts ?? new List<ActiveContractSO>();
        public float PlayerDifficultyLevel => _templateService?.PlayerDifficultyLevel ?? 1f;
        
        // Events - aggregated from components
        public System.Action<ActiveContractSO> OnContractGenerated;
        public System.Action<ActiveContractSO> OnContractAccepted;
        public System.Action<ActiveContractSO> OnContractExpired;
        public System.Action<ActiveContractSO> OnContractCompleted;
        public System.Action<int> OnAvailableContractsChanged;
        
        protected override void OnManagerInitialize()
        {
            InitializeComponents();
            InitializeSystemReferences();
            ValidateConfiguration();
            GenerateInitialContracts();
            
            LogInfo($"ContractGenerationService initialized with {AvailableContractsCount} available contracts");
        }
        
        protected override void OnManagerUpdate()
        {
            float currentTime = Time.time;
            
            // Check if it's time to generate new contracts
            if (currentTime - _lastGenerationTime >= _contractGenerationInterval)
            {
                RefreshContractPool();
                _lastGenerationTime = currentTime;
            }
            
            // Update contract expiration
            _lifecycleManager?.UpdateContractExpiration();
            
            // Update difficulty scaling based on player progress
            if (_enableDynamicDifficulty)
            {
                _templateService?.UpdatePlayerDifficultyLevel();
            }
            
            // Update advanced generation systems
            if (_enableBalancedGeneration)
            {
                _pricingService?.UpdateMarketTrends();
                _pricingService?.UpdateStrainPricing();
                ProcessBalancedGenerationQueue();
            }
            
            // Ensure minimum contracts are always available
            EnsureMinimumContracts();
        }
        
        /// <summary>
        /// Generate a new contract using the template system
        /// </summary>
        public ActiveContractSO GenerateContract()
        {
            if (_generationTemplate == null)
            {
                LogError("No contract generation template assigned");
                return null;
            }
            
            var parameters = _templateService?.GenerateContractParameters();
            if (parameters == null) return null;
            
            // Apply difficulty scaling
            if (_enableDynamicDifficulty)
            {
                _templateService?.ApplyDifficultyScaling(parameters);
            }
            
            // Apply market demand modifiers
            _templateService?.ApplyMarketDemand(parameters);
            
            // Apply market pricing
            _pricingService?.ApplyMarketPricing(parameters);
            
            // Create the contract ScriptableObject
            var contract = _templateService?.CreateContractInstance(parameters);
            
            if (contract != null)
            {
                OnContractGenerated?.Invoke(contract);
                LogInfo($"Generated contract: {contract.ContractTitle}");
            }
            
            return contract;
        }
        
        /// <summary>
        /// Generate multiple contracts for the pool
        /// </summary>
        public List<ActiveContractSO> GenerateContracts(int count)
        {
            var contracts = new List<ActiveContractSO>();
            
            for (int i = 0; i < count; i++)
            {
                var contract = GenerateContract();
                if (contract != null)
                {
                    contracts.Add(contract);
                }
            }
            
            return contracts;
        }
        
        /// <summary>
        /// Accept a contract and move it to active state
        /// </summary>
        public bool AcceptContract(ActiveContractSO contract)
        {
            return _lifecycleManager?.AcceptContract(contract) ?? false;
        }
        
        /// <summary>
        /// Complete a contract and handle payout
        /// </summary>
        public bool CompleteContract(ActiveContractSO contract, float deliveredQuantity, float averageQuality)
        {
            return _lifecycleManager?.CompleteContract(contract, deliveredQuantity, averageQuality) ?? false;
        }
        
        /// <summary>
        /// Get available contracts filtered by criteria
        /// </summary>
        public List<ActiveContractSO> GetAvailableContracts(StrainType? strainFilter = null, float? maxDifficulty = null)
        {
            return _lifecycleManager?.GetAvailableContracts(strainFilter, maxDifficulty) ?? new List<ActiveContractSO>();
        }
        
        /// <summary>
        /// Get contract generation statistics
        /// </summary>
        public ContractGenerationStats GetGenerationStats()
        {
            var templateStats = _templateService?.GetTemplateStats();
            var lifecycleStats = _lifecycleManager?.GetLifecycleStats();
            var marketAnalysis = _pricingService?.GetMarketAnalysis();
            
            return new ContractGenerationStats
            {
                TotalContractsGenerated = templateStats?.TotalContractsGenerated ?? 0,
                AvailableContracts = lifecycleStats?.AvailableContracts ?? 0,
                ActiveContracts = lifecycleStats?.ActiveContracts ?? 0,
                PlayerDifficultyLevel = templateStats?.PlayerDifficultyLevel ?? 1f,
                MarketDemandMultiplier = marketAnalysis?.GlobalMarketTrend ?? 1f,
                ContractorRequestCounts = templateStats?.ContractorRequestCounts ?? new Dictionary<string, int>(),
                StrainDemandCounts = templateStats?.StrainDemandCounts ?? new Dictionary<StrainType, int>()
            };
        }
        
        /// <summary>
        /// Force refresh of available contracts (for testing or special events)
        /// </summary>
        public void ForceRefreshContracts()
        {
            RefreshContractPool();
            LogInfo("Forced contract pool refresh");
        }
        
        private void InitializeComponents()
        {
            // Initialize template service
            _templateService = GetOrAddComponent<ContractTemplateService>();
            _templateService.Initialize(_generationTemplate, _currencyManager, _basePlayerLevel, 
                _difficultyScaleRate, _quantityScaleRange, _qualityScaleRange);
            
            // Initialize pricing service
            _pricingService = GetOrAddComponent<ContractPricingService>();
            _pricingService.Initialize(_marketVolatility, _marketTrendCycleDays, _enableSeasonalAdjustments,
                _competitionFactorRange, _enableBalancedGeneration, _strainVarietyWeight,
                _difficultyDistributionWeight, _valueBalanceWeight, _difficultyProgressionCurve, _contractPoolSize);
            
            // Initialize lifecycle manager
            _lifecycleManager = GetOrAddComponent<ContractLifecycleManager>();
            _lifecycleManager.Initialize(_maxActiveContracts, _contractPoolSize, _contractRefreshRate,
                _minimumContractsAvailable, _currencyManager);
            
            SetupComponentEvents();
            
            LogInfo("Contract generation components initialized for cannabis cultivation system");
        }
        
        private void SetupComponentEvents()
        {
            // Template service events
            if (_templateService != null)
            {
                _templateService.OnContractCreated += (contract) => OnContractGenerated?.Invoke(contract);
            }
            
            // Lifecycle manager events
            if (_lifecycleManager != null)
            {
                _lifecycleManager.OnContractAccepted += (contract) => OnContractAccepted?.Invoke(contract);
                _lifecycleManager.OnContractExpired += (contract) => OnContractExpired?.Invoke(contract);
                _lifecycleManager.OnContractCompleted += (contract) => OnContractCompleted?.Invoke(contract);
                _lifecycleManager.OnAvailableContractsChanged += (count) => OnAvailableContractsChanged?.Invoke(count);
            }
            
            // Pricing service events
            if (_pricingService != null)
            {
                _pricingService.OnStrainPriceChanged += (strain, price) => 
                {
                    _templateService?.UpdateStrainDemand(strain);
                };
            }
        }
        
        private void InitializeSystemReferences()
        {
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                _currencyManager = gameManager.GetManager<CurrencyManager>();
            }
            
            if (_currencyManager == null)
            {
                LogWarning("CurrencyManager not found - contract payouts will not be processed");
            }
        }
        
        private void ValidateConfiguration()
        {
            if (_generationTemplate == null)
            {
                LogError("ContractGenerationTemplate is required");
                return;
            }
            
            if (_maxActiveContracts <= 0)
            {
                LogError("MaxActiveContracts must be positive");
                _maxActiveContracts = 3;
            }
            
            if (_contractGenerationInterval <= 0)
            {
                LogError("ContractGenerationInterval must be positive");
                _contractGenerationInterval = 3600;
            }
        }
        
        private void GenerateInitialContracts()
        {
            int initialCount = Mathf.Max(_minimumContractsAvailable, _contractPoolSize / 2);
            var initialContracts = GenerateContracts(initialCount);
            
            _lifecycleManager?.AddAvailableContracts(initialContracts);
            
            LogInfo($"Generated {initialContracts.Count} initial contracts");
        }
        
        private void RefreshContractPool()
        {
            _lifecycleManager?.RefreshContractPool();
            
            // Generate new contracts to replace refreshed ones
            int contractsToGenerate = Mathf.RoundToInt(AvailableContractsCount * _contractRefreshRate);
            contractsToGenerate = Mathf.Max(1, contractsToGenerate);
            
            var newContracts = GenerateContracts(contractsToGenerate);
            _lifecycleManager?.AddAvailableContracts(newContracts);
            
            LogInfo($"Refreshed contract pool with {newContracts.Count} new contracts");
        }
        
        private void ProcessBalancedGenerationQueue()
        {
            var processableRequests = _pricingService?.ProcessBalancedGenerationQueue(2);
            if (processableRequests == null) return;
            
            // Generate contracts for processable requests
            foreach (var request in processableRequests)
            {
                if (AvailableContractsCount < _contractPoolSize)
                {
                    var contract = GenerateBalancedContract(request);
                    if (contract != null)
                    {
                        _lifecycleManager?.AddAvailableContracts(new List<ActiveContractSO> { contract });
                    }
                }
            }
        }
        
        private ActiveContractSO GenerateBalancedContract(ContractGenerationRequest request)
        {
            var parameters = _templateService?.GenerateContractParameters();
            if (parameters == null) return null;
            
            // Override with balanced selections
            parameters.StrainType = request.PreferredStrain;
            
            // Apply difficulty tier adjustments
            _templateService?.ApplyDifficultyTierAdjustments(parameters, request.TargetDifficulty);
            _pricingService?.UpdateDifficultyDistribution(request.TargetDifficulty);
            
            // Apply market pricing
            _pricingService?.ApplyMarketPricing(parameters);
            
            // Apply progressive unlocking restrictions
            if (_enableProgressiveUnlocking)
            {
                _templateService?.ApplyProgressiveUnlockingRestrictions(parameters);
            }
            
            return _templateService?.CreateContractInstance(parameters);
        }
        
        private void EnsureMinimumContracts()
        {
            int needed = _lifecycleManager?.EnsureMinimumContracts() ?? 0;
            if (needed > 0)
            {
                var newContracts = GenerateContracts(needed);
                _lifecycleManager?.AddAvailableContracts(newContracts);
                LogInfo($"Generated {needed} contracts to maintain minimum");
            }
        }
        
        /// <summary>
        /// Get or add component to this GameObject
        /// </summary>
        private T GetOrAddComponent<T>() where T : Component
        {
            var component = GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }
        
        protected override void OnManagerShutdown()
        {
            var stats = GetGenerationStats();
            LogInfo($"ContractGenerationService shutdown - Generated {stats.TotalContractsGenerated} contracts total");
        }
    }
    
    /// <summary>
    /// Statistics for contract generation system
    /// </summary>
    [System.Serializable]
    public class ContractGenerationStats
    {
        public int TotalContractsGenerated;
        public int AvailableContracts;
        public int ActiveContracts;
        public float PlayerDifficultyLevel;
        public float MarketDemandMultiplier;
        public Dictionary<string, int> ContractorRequestCounts;
        public Dictionary<StrainType, int> StrainDemandCounts;
    }
    
    /// <summary>
    /// Contract generation request for balanced generation system
    /// </summary>
    [System.Serializable]
    public class ContractGenerationRequest
    {
        public StrainType PreferredStrain;
        public ContractDifficultyTier TargetDifficulty;
        public float Priority;
        public DateTime GenerationTime;
    }
    
    /// <summary>
    /// Contract difficulty tiers for balanced progression
    /// </summary>
    public enum ContractDifficultyTier
    {
        Beginner = 0,    // New players
        Novice = 1,      // Some experience
        Intermediate = 2, // Established players
        Advanced = 3,    // Skilled players
        Expert = 4       // Master growers
    }
}