using UnityEngine;
using ProjectChimera.Core.Updates;
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
    /// Lightweight orchestrator for contract tracking functionality in Project Chimera's game economy.
    /// Coordinates specialized services for validation, analytics, delivery, and production tracking.
    /// </summary>
    public class ContractTrackingService : ChimeraManager, ITickable
    {
        [Header("Tracking Configuration")]
        [SerializeField] private bool _enableAutoTracking = true;
        [SerializeField] private float _trackingUpdateInterval = 60f;

        // Specialized components
        private ContractTrackingValidationService _validationService;
        private ContractTrackingAnalyticsService _analyticsService;
        private ContractDeliveryService _deliveryService;
        private ContractProductionTracker _productionTracker;

        // Contract tracking state
        private Dictionary<string, ContractProgress> _contractProgress = new Dictionary<string, ContractProgress>();

        // Properties for game systems
        public override ManagerPriority Priority => ManagerPriority.Normal;
        public int TrackedContractsCount => _contractProgress.Count;
        public int PendingDeliveriesCount => _deliveryService?.PendingDeliveriesCount ?? 0;
        public ContractTrackingMetrics Metrics => _analyticsService?.Metrics ?? new ContractTrackingMetrics();
        public bool AutoTrackingEnabled { get => _enableAutoTracking; set => _enableAutoTracking = value; }

        // Events for game integration
        public System.Action<ActiveContractSO, float> OnContractProgressUpdated;
        public System.Action<ActiveContractSO, int> OnDeadlineWarning;
        public System.Action<ActiveContractSO, ContractCompletionResult> OnContractReadyForDelivery;
        public System.Action<ActiveContractSO, string> OnContractValidationFailed;
        public System.Action<ContractDelivery> OnDeliveryCompleted;

        protected override void OnManagerInitialize()
        {
            InitializeComponents();
            SetupEventHandlers();
            LogInfo("Contract tracking service initialized - game economy orchestrator ready");
        }

        protected override void OnManagerShutdown()
        {
            CleanupEventHandlers();
            ClearTrackingData();
            LogInfo("Contract tracking service shut down");
        }

        public void Tick(float deltaTime)


        {
            if (!IsInitialized || !_enableAutoTracking) return;

            if (Time.time % _trackingUpdateInterval < deltaTime)
            {
                UpdateAllContractProgress();
                CheckContractDeadlines();
            }
        }

        #region Component Initialization

        /// <summary>
        /// Initialize all specialized tracking components
        /// </summary>
        private void InitializeComponents()
        {
            // Get or create components
            _validationService = GetOrAddComponent<ContractTrackingValidationService>();
            _analyticsService = GetOrAddComponent<ContractTrackingAnalyticsService>();
            _deliveryService = GetOrAddComponent<ContractDeliveryService>();
            _productionTracker = GetOrAddComponent<ContractProductionTracker>();

            // Initialize components
            _validationService?.Initialize();
            _analyticsService?.Initialize();
            _deliveryService?.Initialize();
            _productionTracker?.Initialize();
        }

        /// <summary>
        /// Setup event handlers between components
        /// </summary>
        private void SetupEventHandlers()
        {
            if (_analyticsService != null)
            {
                _analyticsService.OnContractProgressUpdated += (contract, progress) => OnContractProgressUpdated?.Invoke(contract, progress);
                _analyticsService.OnDeadlineWarning += (contract, days) => OnDeadlineWarning?.Invoke(contract, days);
            }

            if (_deliveryService != null)
            {
                _deliveryService.OnContractReadyForDelivery += (contract, result) => OnContractReadyForDelivery?.Invoke(contract, result);
                _deliveryService.OnDeliveryCompleted += (delivery) => OnDeliveryCompleted?.Invoke(delivery);
            }

            if (_productionTracker != null)
            {
                _productionTracker.OnPlantProductionRegistered += OnPlantRegistered;
            }
        }

        #endregion

        #region Contract Management

        /// <summary>
        /// Start tracking a new contract for the player
        /// </summary>
        public void StartTrackingContract(ActiveContractSO contract)
        {
            if (contract == null)
            {
                LogError("Cannot track null contract");
                return;
            }

            if (_contractProgress.ContainsKey(contract.ContractId))
            {
                LogWarning($"Already tracking contract: {contract.ContractId}");
                return;
            }

            var progress = new ContractProgress
            {
                ContractId = contract.ContractId,
                Contract = contract,
                StartTime = DateTime.Now,
                CurrentQuantity = 0,
                AverageQuality = QualityGrade.BelowStandard,
                CompletionProgress = 0f,
                QualifiedPlants = 0,
                IsReadyForDelivery = false
            };

            _contractProgress[contract.ContractId] = progress;
            LogInfo($"Started tracking contract: {contract.ContractTitle}");
        }

        /// <summary>
        /// Stop tracking a contract (when completed or cancelled)
        /// </summary>
        public void StopTrackingContract(string contractId)
        {
            if (_contractProgress.Remove(contractId))
            {
                _productionTracker?.ClearContractProduction(contractId);
                LogInfo($"Stopped tracking contract: {contractId}");
            }
        }

        /// <summary>
        /// Get current progress for a specific contract
        /// </summary>
        public ContractProgress GetContractProgress(string contractId)
        {
            return _contractProgress.TryGetValue(contractId, out var progress) ? progress : null;
        }

        /// <summary>
        /// Get all contract progress summaries
        /// </summary>
        public List<ContractProgress> GetAllContractProgress()
        {
            return new List<ContractProgress>(_contractProgress.Values);
        }

        #endregion

        #region Plant Production Integration

        /// <summary>
        /// Register harvested plants for contract fulfillment
        /// </summary>
        public void RegisterHarvestedPlant(string plantId, StrainType strainType, float quantity, float quality)
        {
            _productionTracker?.RegisterHarvestedPlant(plantId, strainType, quantity, quality);
            _analyticsService?.TrackPlantProduction(new PlantProductionRecord
            {
                PlantId = plantId,
                StrainType = strainType,
                Quantity = (int)quantity,
                Quality = QualityGradeExtensions.FromFloat(quality),
                HarvestDate = DateTime.Now,
                IsAllocated = false
            });

            // Update contract progress for matching contracts
            UpdateContractsWithNewPlant(plantId, strainType, quantity, quality);
        }

        /// <summary>
        /// Handle new plant registration and auto-allocation
        /// </summary>
        private void OnPlantRegistered(PlantProductionRecord plant)
        {
            // Auto-allocate to suitable contracts
            foreach (var progress in _contractProgress.Values)
            {
                if (_validationService?.CanPlantFulfillContract(plant, progress.Contract) == true)
                {
                    var plantIds = new List<string> { plant.PlantId };
                    _productionTracker?.AllocateToContract(progress.ContractId, plantIds);
                    break; // Allocate to first suitable contract
                }
            }
        }

        /// <summary>
        /// Update contract progress when new plants are available
        /// </summary>
        private void UpdateContractsWithNewPlant(string plantId, StrainType strainType, float quantity, float quality)
        {
            foreach (var progress in _contractProgress.Values)
            {
                var production = _productionTracker?.GetContractProduction(progress.ContractId);
                if (production?.Count > 0)
                {
                    _analyticsService?.UpdateContractProgress(progress, production);
                }
            }
        }

        #endregion

        #region Contract Validation and Completion

        /// <summary>
        /// Validate contract completion for game progression
        /// </summary>
        public ContractCompletionValidation ValidateContractCompletion(string contractId)
        {
            var progress = GetContractProgress(contractId);
            if (progress == null)
            {
                return new ContractCompletionValidation
                {
                    IsValid = false,
                    Reason = "Contract not found in tracking system"
                };
            }

            return _validationService?.ValidateContractCompletion(progress) ?? new ContractCompletionValidation
            {
                IsValid = false,
                Reason = "Validation service not available"
            };
        }

        /// <summary>
        /// Create a delivery for a completed contract
        /// </summary>
        public bool CreateContractDelivery(string contractId)
        {
            var progress = GetContractProgress(contractId);
            if (progress == null)
            {
                LogError($"Contract {contractId} not found for delivery");
                return false;
            }

            var production = _productionTracker?.GetContractProduction(contractId);
            if (production == null || production.Count == 0)
            {
                LogError($"No production found for contract {contractId}");
                return false;
            }

            // Validate contract can be completed
            var validation = ValidateContractCompletion(contractId);
            if (!validation.IsValid)
            {
                OnContractValidationFailed?.Invoke(progress.Contract, validation.Reason);
                return false;
            }

            // Create delivery through service
            bool success = _deliveryService?.CreateContractDelivery(progress, production) ?? false;
            if (success)
            {
                LogInfo($"Delivery created for contract: {progress.Contract.ContractTitle}");
            }

            return success;
        }

        /// <summary>
        /// Process a pending delivery
        /// </summary>
        public bool ProcessDelivery(string deliveryId)
        {
            bool success = _deliveryService?.ProcessDelivery(deliveryId) ?? false;

            if (success)
            {
                var delivery = _deliveryService?.GetDelivery(deliveryId);
                if (delivery != null)
                {
                    _analyticsService?.TrackContractCompletion(delivery, true);
                    StopTrackingContract(delivery.ContractId);
                }
            }

            return success;
        }

        #endregion

        #region Analytics and Reporting

        /// <summary>
        /// Get available production for contract planning
        /// </summary>
        public List<PlantProductionRecord> GetAvailableProduction(StrainType strainType, float minimumQuality = 0f)
        {
            return _productionTracker?.GetAvailableProduction(strainType, minimumQuality) ?? new List<PlantProductionRecord>();
        }

        /// <summary>
        /// Generate comprehensive analytics report
        /// </summary>
        public ContractAnalyticsReport GenerateAnalyticsReport()
        {
            return _analyticsService?.GenerateAnalyticsReport() ?? new ContractAnalyticsReport
            {
                GeneratedDate = DateTime.Now,
                OverallMetrics = new ContractTrackingMetrics()
            };
        }

        /// <summary>
        /// Get production statistics for player dashboard
        /// </summary>
        public ProductionStatistics GetProductionStatistics()
        {
            return _productionTracker?.GetProductionStatistics() ?? new ProductionStatistics();
        }

        /// <summary>
        /// Analyze performance trends for player insights
        /// </summary>
        public ContractPerformanceTrends AnalyzePerformanceTrends()
        {
            return _analyticsService?.AnalyzePerformanceTrends() ?? new ContractPerformanceTrends
            {
                AnalysisDate = DateTime.Now,
                TotalContractsAnalyzed = 0
            };
        }

        #endregion

        #region Periodic Updates

        /// <summary>
        /// Update progress for all active contracts
        /// </summary>
        private void UpdateAllContractProgress()
        {
            foreach (var progress in _contractProgress.Values)
            {
                var production = _productionTracker?.GetContractProduction(progress.ContractId);
                if (production?.Count > 0)
                {
                    _analyticsService?.UpdateContractProgress(progress, production);
                }
            }
        }

        /// <summary>
        /// Check contract deadlines and send warnings
        /// </summary>
        private void CheckContractDeadlines()
        {
            _analyticsService?.CheckContractDeadlines(_contractProgress);
        }

        #endregion

        #region Cleanup Methods

        /// <summary>
        /// Clean up event handlers and subscriptions
        /// </summary>
        private void CleanupEventHandlers()
        {
            // Clean up validation service events
            if (_validationService != null)
            {
                // Add event cleanup if there are any subscriptions
            }

            // Clean up analytics service events
            if (_analyticsService != null)
            {
                // Add event cleanup if there are any subscriptions
            }

            // Clean up delivery service events
            if (_deliveryService != null)
            {
                // Add event cleanup if there are any subscriptions
            }

            // Clean up production tracker events
            if (_productionTracker != null)
            {
                // Add event cleanup if there are any subscriptions
            }
        }

        /// <summary>
        /// Clear all tracking data
        /// </summary>
        private void ClearTrackingData()
        {
            _contractProgress.Clear();
        }

        /// <summary>
        /// Handle quality grade assignment event
        /// </summary>
        public void OnQualityGradeAssigned(string contractId, QualityGrade grade)
        {
            LogInfo($"Quality grade assigned for contract {contractId}: {grade}");

            // Delegate to notification service if available
            var notificationService = GetComponent<ContractNotificationService>();
            if (notificationService != null)
            {
                notificationService.SendQualityNotification(contractId, grade);
            }
        }

        /// <summary>
        /// Handle quality consistency alert event
        /// </summary>
        public void OnQualityConsistencyAlert(string contractId, float variance)
        {
            LogWarning($"Quality consistency alert for contract {contractId}: Variance {variance:F3}");

            // Delegate to notification service if available
            var notificationService = GetComponent<ContractNotificationService>();
            if (notificationService != null)
            {
                notificationService.SendQualityConsistencyAlert(contractId, variance);
            }
        }

        #endregion

        #region Utility Methods

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

        #endregion

        #region Unity Lifecycle

        protected virtual void Start()
        {
            base.Start();
            // Register with UpdateOrchestrator
            UpdateOrchestrator.Instance?.RegisterTickable(this);
        }

        protected virtual void OnDestroy()
        {
            // Unregister from UpdateOrchestrator
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
            base.OnDestroy();
        }

        #endregion

        #region ITickable Implementation

        // ITickable implementation
        int ITickable.Priority => 0;
        public bool Enabled => enabled && gameObject.activeInHierarchy;



        public virtual void OnRegistered()
        {
            // Override in derived classes if needed
        }

        public virtual void OnUnregistered()
        {
            // Override in derived classes if needed
        }

        #endregion
    }
}
