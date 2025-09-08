using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
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
    /// Handles contract delivery creation, processing, and completion
    /// for Project Chimera's game economy system.
    /// </summary>
    public class ContractDeliveryService : MonoBehaviour, ITickable
    {
        [Header("Delivery Configuration")]
        [SerializeField] private bool _enableAutoDelivery = false;
        [SerializeField] private float _deliveryProcessingDelay = 2f;
        [SerializeField] private int _maxPendingDeliveries = 20;
        [SerializeField] private bool _enableDeliveryConfirmation = true;

        // Service dependencies
        private ContractGenerationService _contractService;
        private CurrencyManager _currencyManager;

        // Delivery state
        private List<ContractDelivery> _pendingDeliveries = new List<ContractDelivery>();
        private List<ContractDelivery> _completedDeliveries = new List<ContractDelivery>();
        private Dictionary<string, float> _deliveryProcessingTimers = new Dictionary<string, float>();

        // Events
        public System.Action<ContractDelivery> OnDeliveryCreated;
        public System.Action<ContractDelivery> OnDeliveryCompleted;
        public System.Action<ContractDelivery, string> OnDeliveryFailed;
        public System.Action<ActiveContractSO, ContractCompletionResult> OnContractReadyForDelivery;

        // Properties
        public int PendingDeliveriesCount => _pendingDeliveries.Count;
        public int CompletedDeliveriesCount => _completedDeliveries.Count;
        public List<ContractDelivery> PendingDeliveries => new List<ContractDelivery>(_pendingDeliveries);
        public bool AutoDeliveryEnabled => _enableAutoDelivery;

        private void Start()
        {
        // Register with UpdateOrchestrator
        UpdateOrchestrator.Instance?.RegisterTickable(this);
            InitializeServiceReferences();
        }

            public void Tick(float deltaTime)
    {
            if (_enableAutoDelivery)
            {
                ProcessPendingDeliveries();

    }
        }

        public void Initialize()
        {
            InitializeServiceReferences();
            LogInfo("Contract delivery service initialized for game economy");
        }

        #region Delivery Creation

        /// <summary>
        /// Create a delivery for a completed contract
        /// </summary>
        public bool CreateContractDelivery(ContractProgress progress, List<PlantProductionRecord> production)
        {
            if (progress == null || production == null)
            {
                LogError("Cannot create delivery with null progress or production data");
                return false;
            }

            // Check if we've reached the maximum pending deliveries
            if (_pendingDeliveries.Count >= _maxPendingDeliveries)
            {
                LogWarning($"Maximum pending deliveries reached ({_maxPendingDeliveries}). Cannot create new delivery.");
                return false;
            }

            var allocatedProduction = production.Where(p => p.IsAllocated).ToList();

            var delivery = new ContractDelivery
            {
                DeliveryId = Guid.NewGuid().ToString(),
                ContractId = progress.ContractId,
                Contract = progress.Contract,
                DeliveredQuantity = allocatedProduction.Sum(p => p.Quantity),
                AverageQuality = allocatedProduction.Count > 0 ? QualityGradeExtensions.FromFloat(allocatedProduction.Average(p => p.Quality.ToFloat())) : QualityGrade.BelowStandard,
                DeliveryDate = DateTime.Now,
                IsCompleted = false,
                PlantRecords = new List<PlantProductionRecord>(allocatedProduction)
            };

            // Calculate delivery value and bonuses
            CalculateDeliveryValue(delivery);

            _pendingDeliveries.Add(delivery);
            progress.IsReadyForDelivery = true;

            // Initialize processing timer if auto-delivery is enabled
            if (_enableAutoDelivery)
            {
                _deliveryProcessingTimers[delivery.DeliveryId] = 0f;
            }

            OnDeliveryCreated?.Invoke(delivery);
            LogInfo($"Created delivery for contract: {progress.Contract.ContractTitle} (ID: {delivery.DeliveryId})");

            return true;
        }

        /// <summary>
        /// Create delivery notification for ready contract
        /// </summary>
        public void NotifyContractReadyForDelivery(ContractProgress progress, ContractCompletionResult completionResult)
        {
            if (progress?.Contract == null) return;

            OnContractReadyForDelivery?.Invoke(progress.Contract, completionResult);
            LogInfo($"Contract ready for delivery: {progress.Contract.ContractTitle}");
        }

        #endregion

        #region Delivery Processing

        /// <summary>
        /// Process a specific delivery and complete the contract
        /// </summary>
        public bool ProcessDelivery(string deliveryId, bool forceProcess = false)
        {
            var delivery = _pendingDeliveries.FirstOrDefault(d => d.DeliveryId == deliveryId);
            if (delivery == null)
            {
                LogError($"Delivery {deliveryId} not found in pending deliveries");
                return false;
            }

            // Check if delivery confirmation is required and not forcing
            if (_enableDeliveryConfirmation && !forceProcess)
            {
                LogInfo($"Delivery {deliveryId} requires confirmation before processing");
                return false;
            }

            return ExecuteDeliveryProcessing(delivery);
        }

        /// <summary>
        /// Process all pending deliveries (for auto-delivery mode)
        /// </summary>
        private void ProcessPendingDeliveries()
        {
            var deliveriesToProcess = new List<ContractDelivery>();

            foreach (var delivery in _pendingDeliveries.ToList())
            {
                var deliveryId = delivery.DeliveryId;

                // Update processing timer
                if (_deliveryProcessingTimers.TryGetValue(deliveryId, out var currentTime))
                {
                    _deliveryProcessingTimers[deliveryId] = currentTime + Time.deltaTime;

                    // Check if delivery is ready for processing
                    if (_deliveryProcessingTimers[deliveryId] >= _deliveryProcessingDelay)
                    {
                        deliveriesToProcess.Add(delivery);
                    }
                }
            }

            // Process ready deliveries
            foreach (var delivery in deliveriesToProcess)
            {
                ExecuteDeliveryProcessing(delivery);
                _deliveryProcessingTimers.Remove(delivery.DeliveryId);
            }
        }

        /// <summary>
        /// Execute the actual delivery processing
        /// </summary>
        private bool ExecuteDeliveryProcessing(ContractDelivery delivery)
        {
            try
            {
                // Complete the contract through ContractGenerationService
                bool success = false;

                if (_contractService != null)
                {
                    success = _contractService.CompleteContract(
                        delivery.Contract,
                        delivery.DeliveredQuantity,
                        delivery.AverageQuality.ToFloat()
                    );
                }
                else
                {
                    LogWarning("ContractGenerationService not available, simulating successful completion");
                    success = true; // Fallback for missing service
                }

                if (success)
                {
                    CompleteDelivery(delivery);
                    return true;
                }
                else
                {
                    FailDelivery(delivery, "Contract completion failed");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError($"Exception during delivery processing: {ex.Message}");
                FailDelivery(delivery, $"Processing error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Complete a delivery successfully
        /// </summary>
        private void CompleteDelivery(ContractDelivery delivery)
        {
            delivery.IsCompleted = true;
            delivery.CompletionDate = DateTime.Now;

            // Move to completed deliveries
            _pendingDeliveries.Remove(delivery);
            _completedDeliveries.Add(delivery);

            // Process payment if currency manager is available
            if (_currencyManager != null && delivery.TotalValue > 0)
            {
                _currencyManager.AddCurrency(CurrencyType.Cash, delivery.TotalValue, "Contract delivery payment");
                LogInfo($"Payment processed: ${delivery.TotalValue:F2}");
            }

            OnDeliveryCompleted?.Invoke(delivery);
            LogInfo($"Successfully processed delivery: {delivery.DeliveryId} for contract {delivery.Contract.ContractTitle}");
        }

        /// <summary>
        /// Fail a delivery with reason
        /// </summary>
        private void FailDelivery(ContractDelivery delivery, string reason)
        {
            delivery.FailureReason = reason;
            delivery.CompletionDate = DateTime.Now;

            OnDeliveryFailed?.Invoke(delivery, reason);
            LogError($"Delivery failed: {delivery.DeliveryId} - {reason}");
        }

        #endregion

        #region Delivery Management

        /// <summary>
        /// Get delivery by ID
        /// </summary>
        public ContractDelivery GetDelivery(string deliveryId)
        {
            var pending = _pendingDeliveries.FirstOrDefault(d => d.DeliveryId == deliveryId);
            if (pending != null) return pending;

            return _completedDeliveries.FirstOrDefault(d => d.DeliveryId == deliveryId);
        }

        /// <summary>
        /// Get all deliveries for a specific contract
        /// </summary>
        public List<ContractDelivery> GetDeliveriesForContract(string contractId)
        {
            var deliveries = new List<ContractDelivery>();

            deliveries.AddRange(_pendingDeliveries.Where(d => d.ContractId == contractId));
            deliveries.AddRange(_completedDeliveries.Where(d => d.ContractId == contractId));

            return deliveries.OrderBy(d => d.DeliveryDate).ToList();
        }

        /// <summary>
        /// Cancel a pending delivery
        /// </summary>
        public bool CancelDelivery(string deliveryId, string reason = "Cancelled by player")
        {
            var delivery = _pendingDeliveries.FirstOrDefault(d => d.DeliveryId == deliveryId);
            if (delivery == null)
            {
                LogWarning($"Cannot cancel delivery {deliveryId} - not found in pending deliveries");
                return false;
            }

            // Remove from pending deliveries
            _pendingDeliveries.Remove(delivery);
            _deliveryProcessingTimers.Remove(deliveryId);

            // Mark plant records as unallocated
            foreach (var plant in delivery.PlantRecords)
            {
                plant.IsAllocated = false;
            }

            LogInfo($"Delivery cancelled: {deliveryId} - {reason}");
            return true;
        }

        /// <summary>
        /// Get delivery statistics
        /// </summary>
        public DeliveryStatistics GetDeliveryStatistics()
        {
            var stats = new DeliveryStatistics
            {
                TotalDeliveries = _completedDeliveries.Count + _pendingDeliveries.Count,
                CompletedDeliveries = _completedDeliveries.Count,
                PendingDeliveries = _pendingDeliveries.Count,
                FailedDeliveries = _completedDeliveries.Count(d => !string.IsNullOrEmpty(d.FailureReason))
            };

            if (_completedDeliveries.Count > 0)
            {
                stats.AverageDeliveryValue = _completedDeliveries.Average(d => d.TotalValue);
                stats.AverageDeliveryQuality = QualityGradeExtensions.FromFloat(_completedDeliveries.Average(d => d.AverageQuality.ToFloat()));
                stats.TotalDeliveryValue = _completedDeliveries.Sum(d => d.TotalValue);

                var processingTimes = _completedDeliveries
                    .Where(d => d.IsCompleted)
                    .Select(d => (d.CompletionDate - d.DeliveryDate).TotalMinutes)
                    .ToList();

                if (processingTimes.Count > 0)
                {
                    stats.AverageProcessingTimeMinutes = (float)processingTimes.Average();
                }
            }

            return stats;
        }

        #endregion

        #region Delivery Value Calculation

        /// <summary>
        /// Calculate the total value and bonuses for a delivery
        /// </summary>
        private void CalculateDeliveryValue(ContractDelivery delivery)
        {
            if (delivery?.Contract == null) return;

            var contract = delivery.Contract;

            // Base contract value
            delivery.BaseValue = contract.TotalValue;

            // Quality bonus
            delivery.QualityBonus = CalculateQualityBonus(delivery.AverageQuality.ToFloat(), contract.MinimumQuality);

            // Quantity bonus (for exceeding requirements)
            delivery.QuantityBonus = CalculateQuantityBonus(delivery.DeliveredQuantity, contract.RequiredQuantity);

            // Timeliness bonus
            delivery.TimelinessBonus = CalculateTimelinessBonus(delivery.DeliveryDate, contract.Deadline);

            // Calculate total value
            delivery.TotalValue = delivery.BaseValue + delivery.QualityBonus + delivery.QuantityBonus + delivery.TimelinessBonus;

            LogInfo($"Delivery value calculated: Base: ${delivery.BaseValue:F2}, Quality: ${delivery.QualityBonus:F2}, " +
                    $"Quantity: ${delivery.QuantityBonus:F2}, Timeliness: ${delivery.TimelinessBonus:F2}, " +
                    $"Total: ${delivery.TotalValue:F2}");
        }



        private float CalculateQualityBonus(float averageQuality, float minimumQuality)
        {
            if (averageQuality <= minimumQuality) return 0f;

            float qualityExcess = averageQuality - minimumQuality;
            return qualityExcess * 100f; // $1 per 1% quality above minimum
        }

        private float CalculateQuantityBonus(float deliveredQuantity, float requiredQuantity)
        {
            if (deliveredQuantity <= requiredQuantity) return 0f;

            float quantityExcess = deliveredQuantity - requiredQuantity;
            return quantityExcess * 0.5f; // $0.50 per gram above requirement
        }



        private float CalculateTimelinessBonus(DateTime deliveryDate, DateTime deadline)
        {
            if (deliveryDate >= deadline) return 0f;

            var timeEarly = deadline - deliveryDate;
            float daysEarly = (float)timeEarly.TotalDays;

            // Bonus for early delivery, maxing out at 7 days early
            float bonusMultiplier = Mathf.Min(daysEarly / 7f, 1f);
            return bonusMultiplier * 50f; // Up to $50 bonus for early delivery
        }

        #endregion

        #region Service Dependencies

        private void InitializeServiceReferences()
        {
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                _contractService = gameManager.GetManager<ContractGenerationService>();
                _currencyManager = gameManager.GetManager<CurrencyManager>();

                if (_contractService == null)
                {
                    LogWarning("ContractGenerationService not found - delivery processing may be limited");
                }

                if (_currencyManager == null)
                {
                    LogWarning("CurrencyManager not found - payment processing may be limited");
                }
            }
            else
            {
                LogWarning("GameManager not available - service dependencies not initialized");
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Clean up old completed deliveries to prevent memory buildup
        /// </summary>
        public void CleanupOldDeliveries(int maxHistoryCount = 100)
        {
            if (_completedDeliveries.Count <= maxHistoryCount) return;

            int toRemove = _completedDeliveries.Count - maxHistoryCount;
            var toRemoveList = _completedDeliveries
                .OrderBy(d => d.DeliveryDate)
                .Take(toRemove)
                .ToList();

            foreach (var delivery in toRemoveList)
            {
                _completedDeliveries.Remove(delivery);
            }

            LogInfo($"Cleaned up {toRemove} old delivery records");
        }

        #endregion

        private void LogInfo(string message)
        {
            ChimeraLogger.Log($"[ContractDeliveryService] {message}");
        }

        private void LogWarning(string message)
        {
            ChimeraLogger.LogWarning($"[ContractDeliveryService] {message}");
        }

        private void LogError(string message)
        {
            ChimeraLogger.LogError($"[ContractDeliveryService] {message}");
        }

    // ITickable implementation
    public int Priority => 0;
    public bool Enabled => enabled && gameObject.activeInHierarchy;

    public virtual void OnRegistered()
    {
        // Override in derived classes if needed
    }

    public virtual void OnUnregistered()
    {
        // Override in derived classes if needed
    }

}
}
