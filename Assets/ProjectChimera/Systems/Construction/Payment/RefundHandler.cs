using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Construction;
using ProjectChimera.Data.Economy;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Implementation for refund processing and resource reservation management
    /// </summary>
    public class RefundHandler : IRefundHandler
    {
        private bool _enableRefunds = true;
        private float _refundPercentage = 0.8f;
        private bool _enableResourceReservation = true;
        private float _reservationDuration = 30f;
        private bool _autoReleaseReservations = true;
        private int _maxSimultaneousReservations = 10;
        private bool _isInitialized = false;
        
        // Resource reservation
        private Dictionary<string, ResourceReservation> _activeReservations = new Dictionary<string, ResourceReservation>();
        private Dictionary<Vector3Int, List<string>> _positionReservations = new Dictionary<Vector3Int, List<string>>();
        
        // External dependencies
        private MonoBehaviour _currencyManager;
        private MonoBehaviour _tradingManager;
        private IPaymentProcessor _paymentProcessor;
        private ICostCalculator _costCalculator;

        public bool EnableRefunds 
        { 
            get => _enableRefunds; 
            set => _enableRefunds = value; 
        }

        public float RefundPercentage 
        { 
            get => _refundPercentage; 
            set => _refundPercentage = Mathf.Clamp01(value); 
        }

        public bool EnableResourceReservation 
        { 
            get => _enableResourceReservation; 
            set => _enableResourceReservation = value; 
        }

        public float ReservationDuration 
        { 
            get => _reservationDuration; 
            set => _reservationDuration = value; 
        }

        public bool AutoReleaseReservations 
        { 
            get => _autoReleaseReservations; 
            set => _autoReleaseReservations = value; 
        }

        public int MaxSimultaneousReservations 
        { 
            get => _maxSimultaneousReservations; 
            set => _maxSimultaneousReservations = value; 
        }

        public Dictionary<string, ResourceReservation> ActiveReservations => new Dictionary<string, ResourceReservation>(_activeReservations);
        public Dictionary<Vector3Int, List<string>> PositionReservations => new Dictionary<Vector3Int, List<string>>(_positionReservations);

        public Action<string, ResourceReservation> OnResourceReserved { get; set; }
        public Action<string> OnReservationReleased { get; set; }

        public RefundHandler(MonoBehaviour currencyManager = null, MonoBehaviour tradingManager = null,
                           IPaymentProcessor paymentProcessor = null, ICostCalculator costCalculator = null)
        {
            _currencyManager = currencyManager;
            _tradingManager = tradingManager;
            _paymentProcessor = paymentProcessor;
            _costCalculator = costCalculator;
        }

        public void Initialize(bool enableRefunds, float refundPercentage, bool enableResourceReservation,
                             float reservationDuration, bool autoReleaseReservations, int maxSimultaneousReservations)
        {
            _enableRefunds = enableRefunds;
            _refundPercentage = Mathf.Clamp01(refundPercentage);
            _enableResourceReservation = enableResourceReservation;
            _reservationDuration = reservationDuration;
            _autoReleaseReservations = autoReleaseReservations;
            _maxSimultaneousReservations = maxSimultaneousReservations;
            _isInitialized = true;
            
            ChimeraLogger.Log("[RefundHandler] Refund handler initialized");
        }

        public void Shutdown()
        {
            _activeReservations.Clear();
            _positionReservations.Clear();
            _isInitialized = false;
            
            ChimeraLogger.Log("[RefundHandler] Refund handler shutdown");
        }

        public RefundResult ProcessRefund(Vector3Int gridPosition, string reason = "")
        {
            var result = new RefundResult();

            if (!_enableRefunds)
            {
                result.Success = false;
                result.ErrorMessage = "Refunds are disabled";
                return result;
            }

            if (_paymentProcessor == null)
            {
                result.Success = false;
                result.ErrorMessage = "Payment processor not available";
                return result;
            }

            try
            {
                var placementTransactions = _paymentProcessor.PlacementTransactions;
                if (!placementTransactions.ContainsKey(gridPosition))
                {
                    result.Success = false;
                    result.ErrorMessage = "No transaction record found for this position";
                    return result;
                }

                string transactionId = placementTransactions[gridPosition];
                var originalTransaction = _paymentProcessor.GetTransactionById(transactionId);

                if (string.IsNullOrEmpty(originalTransaction.TransactionId))
                {
                    result.Success = false;
                    result.ErrorMessage = "Original transaction not found";
                    return result;
                }

                // Calculate refund amount
                float refundAmount = originalTransaction.TotalCost * _refundPercentage;
                
                // Process refund
                bool refundProcessed = ProcessCurrencyRefund(refundAmount);
                if (!refundProcessed)
                {
                    result.Success = false;
                    result.ErrorMessage = "Failed to process currency refund";
                    return result;
                }

                // Refund resources (partial)
                var refundedResources = CalculateResourceRefund(originalTransaction.ResourceCosts);
                RefundResources(refundedResources);

                // Record refund transaction
                var refundTransaction = new PaymentTransaction
                {
                    TransactionId = System.Guid.NewGuid().ToString(),
                    Position = gridPosition,
                    TotalCost = refundAmount,
                    ResourceCosts = refundedResources,
                    Timestamp = Time.time,
                    Type = TransactionType.Refund,
                    Status = TransactionStatus.Completed,
                    Description = $"Refund for object at {gridPosition}: {reason}"
                };

                _paymentProcessor.RecordTransaction(refundTransaction);

                result.Success = true;
                result.RefundAmount = refundAmount;
                result.RefundedResources = refundedResources;

                ChimeraLogger.Log($"[RefundHandler] Refund processed: {refundAmount:F2} at {gridPosition}");
                return result;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[RefundHandler] Error processing refund: {ex.Message}");
                result.Success = false;
                result.ErrorMessage = $"Refund processing error: {ex.Message}";
                return result;
            }
        }

        public List<ResourceCost> CalculateResourceRefund(List<ResourceCost> originalCosts)
        {
            var refundedResources = new List<ResourceCost>();

            foreach (var originalCost in originalCosts)
            {
                // Refund a percentage of resources (e.g., 60% to account for wear/processing loss)
                int refundQuantity = Mathf.FloorToInt(originalCost.quantity * 0.6f);
                
                if (refundQuantity > 0)
                {
                    refundedResources.Add(new ResourceCost
                    {
                        resourceId = originalCost.resourceId,
                        quantity = refundQuantity,
                        isRequired = false // Refunds are not required to succeed
                    });
                }
            }

            return refundedResources;
        }

        public ResourceReservationResult CreateReservation(GridPlaceable placeable, Vector3Int gridPosition)
        {
            var result = new ResourceReservationResult();

            if (!_enableResourceReservation)
            {
                result.Success = false;
                result.ErrorMessage = "Resource reservation is disabled";
                return result;
            }

            // Check reservation limits
            if (_activeReservations.Count >= _maxSimultaneousReservations)
            {
                result.Success = false;
                result.ErrorMessage = "Maximum concurrent reservations reached";
                return result;
            }

            if (_costCalculator == null)
            {
                result.Success = false;
                result.ErrorMessage = "Cost calculator not available";
                return result;
            }

            try
            {
                // Calculate cost
                var costBreakdown = _costCalculator.CalculatePlacementCost(placeable, gridPosition);

                // Validate resource availability (done through validation component)
                // For now, assume resources are available

                // Create reservation
                string reservationId = System.Guid.NewGuid().ToString();
                var reservation = new ResourceReservation
                {
                    ReservationId = reservationId,
                    Position = gridPosition,
                    ReservedResources = costBreakdown.ResourceCosts,
                    ReservationTime = Time.time,
                    ExpiryTime = Time.time + _reservationDuration,
                    IsActive = true,
                    PlayerId = GetPlayerID()
                };

                // Reserve resources
                if (ReserveResources(reservation.ReservedResources))
                {
                    _activeReservations[reservationId] = reservation;

                    if (!_positionReservations.ContainsKey(gridPosition))
                        _positionReservations[gridPosition] = new List<string>();
                    _positionReservations[gridPosition].Add(reservationId);

                    result.Success = true;
                    result.ReservationId = reservationId;
                    result.ExpiryTime = reservation.ExpiryTime;

                    OnResourceReserved?.Invoke(reservationId, reservation);

                    ChimeraLogger.Log($"[RefundHandler] Reservation created: {reservationId} for {placeable.name} at {gridPosition}");
                    return result;
                }
                else
                {
                    result.Success = false;
                    result.ErrorMessage = "Failed to reserve resources";
                    return result;
                }
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[RefundHandler] Error creating reservation: {ex.Message}");
                result.Success = false;
                result.ErrorMessage = $"Reservation creation error: {ex.Message}";
                return result;
            }
        }

        public bool ReleaseReservation(string reservationId)
        {
            if (!_activeReservations.TryGetValue(reservationId, out var reservation))
            {
                ChimeraLogger.LogWarning($"[RefundHandler] Reservation not found: {reservationId}");
                return false;
            }

            try
            {
                // Release reserved resources
                ReleaseReservedResources(reservation.ReservedResources);

                // Remove from tracking
                _activeReservations.Remove(reservationId);
                
                if (_positionReservations.TryGetValue(reservation.Position, out var positionReservations))
                {
                    positionReservations.Remove(reservationId);
                    if (positionReservations.Count == 0)
                    {
                        _positionReservations.Remove(reservation.Position);
                    }
                }

                OnReservationReleased?.Invoke(reservationId);

                ChimeraLogger.Log($"[RefundHandler] Reservation released: {reservationId}");
                return true;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[RefundHandler] Error releasing reservation: {ex.Message}");
                return false;
            }
        }

        public void ProcessExpiredReservations()
        {
            if (!_autoReleaseReservations) return;

            var expiredReservations = _activeReservations.Values
                .Where(r => r.IsActive && Time.time > r.ExpiryTime)
                .ToList();

            foreach (var reservation in expiredReservations)
            {
                ChimeraLogger.Log($"[RefundHandler] Processing expired reservation: {reservation.ReservationId}");
                ReleaseReservation(reservation.ReservationId);
            }
        }

        public bool ReserveResources(List<ResourceCost> resources)
        {
            if (_tradingManager == null)
            {
                ChimeraLogger.LogWarning("[RefundHandler] Trading manager not available for resource reservation");
                return true; // Allow if we can't check
            }

            try
            {
                foreach (var resourceCost in resources)
                {
                    if (!resourceCost.isRequired) continue;

                    bool reserved = ReserveResourceFromInventory(resourceCost.resourceId, resourceCost.quantity);
                    if (!reserved)
                    {
                        ChimeraLogger.LogWarning($"[RefundHandler] Failed to reserve resource: {resourceCost.resourceId} x{resourceCost.quantity}");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[RefundHandler] Error reserving resources: {ex.Message}");
                return false;
            }
        }

        public void ReleaseReservedResources(List<ResourceCost> resources)
        {
            if (_tradingManager == null) return;

            try
            {
                foreach (var resourceCost in resources)
                {
                    ReleaseReservedResourceFromInventory(resourceCost.resourceId, resourceCost.quantity);
                }
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[RefundHandler] Error releasing reserved resources: {ex.Message}");
            }
        }

        public void RefundResources(List<ResourceCost> resources)
        {
            if (_tradingManager == null) return;

            try
            {
                foreach (var resourceCost in resources)
                {
                    AddResourceToInventory(resourceCost.resourceId, resourceCost.quantity);
                }
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[RefundHandler] Error refunding resources: {ex.Message}");
            }
        }

        public bool ReserveResourceFromInventory(string resourceId, int quantity)
        {
            if (_tradingManager == null) return true;

            try
            {
                var method = _tradingManager.GetType().GetMethod("ReserveResource");
                if (method != null)
                {
                    return (bool)method.Invoke(_tradingManager, new object[] { resourceId, quantity });
                }

                return true; // Allow if we can't check
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[RefundHandler] Error reserving resource from inventory: {ex.Message}");
                return false;
            }
        }

        public void ReleaseReservedResourceFromInventory(string resourceId, int quantity)
        {
            if (_tradingManager == null) return;

            try
            {
                var method = _tradingManager.GetType().GetMethod("ReleaseReservedResource");
                if (method != null)
                {
                    method.Invoke(_tradingManager, new object[] { resourceId, quantity });
                }
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[RefundHandler] Error releasing reserved resource from inventory: {ex.Message}");
            }
        }

        public void AddResourceToInventory(string resourceId, int quantity)
        {
            if (_tradingManager == null) return;

            try
            {
                var method = _tradingManager.GetType().GetMethod("AddResource");
                if (method != null)
                {
                    method.Invoke(_tradingManager, new object[] { resourceId, quantity });
                }
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[RefundHandler] Error adding resource to inventory: {ex.Message}");
            }
        }

        public string GetPlayerID()
        {
            // Simple player ID generation - in a real system this would come from player management
            return "Player1";
        }

        public void Tick(float deltaTime)
        {
            if (_autoReleaseReservations)
            {
                ProcessExpiredReservations();
            }
        }

        public void SetDependencies(MonoBehaviour currencyManager, MonoBehaviour tradingManager,
                                   IPaymentProcessor paymentProcessor, ICostCalculator costCalculator)
        {
            _currencyManager = currencyManager;
            _tradingManager = tradingManager;
            _paymentProcessor = paymentProcessor;
            _costCalculator = costCalculator;
        }

        private bool ProcessCurrencyRefund(float amount)
        {
            if (_currencyManager == null)
            {
                ChimeraLogger.LogWarning("[RefundHandler] Currency manager not available for refund");
                return true; // Allow if we can't check
            }

            try
            {
                // Use reflection to add refund funds
                var method = _currencyManager.GetType().GetMethod("AddCurrency");
                if (method != null)
                {
                    return (bool)method.Invoke(_currencyManager, new object[] { 0, amount, "Construction Refund" }); // 0 = CurrencyType.Cash
                }

                return true; // Allow if we can't determine
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[RefundHandler] Error processing currency refund: {ex.Message}");
                return false;
            }
        }
    }
}
