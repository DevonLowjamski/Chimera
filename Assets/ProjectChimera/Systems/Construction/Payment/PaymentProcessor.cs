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
    /// Implementation for payment processing and transaction management
    /// </summary>
    public class PaymentProcessor : IPaymentProcessor
    {
        private bool _requireInstantPayment = false;
        private bool _enablePaymentPlans = true;
        private bool _isInitialized = false;
        
        // Transaction tracking
        private List<PaymentTransaction> _transactionHistory = new List<PaymentTransaction>();
        private Dictionary<Vector3Int, string> _placementTransactions = new Dictionary<Vector3Int, string>();
        
        // External dependencies
        private MonoBehaviour _currencyManager;
        private MonoBehaviour _tradingManager;
        private ICostCalculator _costCalculator;
        private IRefundHandler _refundHandler;

        public bool RequireInstantPayment 
        { 
            get => _requireInstantPayment; 
            set => _requireInstantPayment = value; 
        }

        public bool EnablePaymentPlans 
        { 
            get => _enablePaymentPlans; 
            set => _enablePaymentPlans = value; 
        }

        public List<PaymentTransaction> TransactionHistory => new List<PaymentTransaction>(_transactionHistory);
        public Dictionary<Vector3Int, string> PlacementTransactions => new Dictionary<Vector3Int, string>(_placementTransactions);

        public Action<PaymentTransaction> OnPaymentProcessed { get; set; }
        public Action<PaymentError> OnPaymentError { get; set; }

        public PaymentProcessor(MonoBehaviour currencyManager = null, MonoBehaviour tradingManager = null, 
                               ICostCalculator costCalculator = null, IRefundHandler refundHandler = null)
        {
            _currencyManager = currencyManager;
            _tradingManager = tradingManager;
            _costCalculator = costCalculator;
            _refundHandler = refundHandler;
        }

        public void Initialize(bool requireInstantPayment, bool enablePaymentPlans)
        {
            _requireInstantPayment = requireInstantPayment;
            _enablePaymentPlans = enablePaymentPlans;
            _isInitialized = true;
            
            ChimeraLogger.Log("[PaymentProcessor] Payment processor initialized");
        }

        public void Shutdown()
        {
            _transactionHistory.Clear();
            _placementTransactions.Clear();
            _isInitialized = false;
            
            ChimeraLogger.Log("[PaymentProcessor] Payment processor shutdown");
        }

        public PaymentProcessingResult ProcessPayment(GridPlaceable placeable, Vector3Int gridPosition, string reservationId = null)
        {
            if (!_isInitialized)
            {
                return new PaymentProcessingResult 
                { 
                    Success = false, 
                    ErrorMessage = "Payment processor not initialized" 
                };
            }

            PaymentProcessingResult result;

            // Use existing reservation if available
            if (!string.IsNullOrEmpty(reservationId) && _refundHandler?.ActiveReservations.ContainsKey(reservationId) == true)
            {
                result = ProcessReservedPayment(reservationId, gridPosition);
            }
            else
            {
                result = ProcessImmediatePayment(placeable, gridPosition);
            }

            // Record transaction
            var transaction = new PaymentTransaction
            {
                TransactionId = System.Guid.NewGuid().ToString(),
                Position = gridPosition,
                TotalCost = result.TotalCost,
                ResourceCosts = result.ResourceCosts ?? new List<ResourceCost>(),
                Timestamp = Time.time,
                Type = TransactionType.Purchase,
                Status = result.Success ? TransactionStatus.Completed : TransactionStatus.Failed,
                Description = $"Placement of {placeable.name} at {gridPosition}"
            };

            RecordTransaction(transaction);
            
            if (result.Success)
            {
                _placementTransactions[gridPosition] = transaction.TransactionId;
                result.TransactionId = transaction.TransactionId;
            }

            OnPaymentProcessed?.Invoke(transaction);
            
            ChimeraLogger.Log($"[PaymentProcessor] Payment processing {(result.Success ? "successful" : "failed")} for {placeable.name} at {gridPosition}");
            return result;
        }

        public PaymentProcessingResult ProcessImmediatePayment(GridPlaceable placeable, Vector3Int gridPosition)
        {
            var result = new PaymentProcessingResult();

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
                result.TotalCost = costBreakdown.TotalCost;
                result.ResourceCosts = costBreakdown.ResourceCosts;

                // Process payment
                bool fundsDeducted = DeductFunds(result.TotalCost);
                if (!fundsDeducted)
                {
                    result.Success = false;
                    result.ErrorMessage = "Failed to deduct funds";
                    
                    OnPaymentError?.Invoke(new PaymentError
                    {
                        ErrorCode = "INSUFFICIENT_FUNDS",
                        ErrorMessage = result.ErrorMessage,
                        AttemptedCost = result.TotalCost,
                        Position = gridPosition
                    });
                    
                    return result;
                }

                // Consume resources
                bool resourcesConsumed = ConsumeResources(result.ResourceCosts);
                if (!resourcesConsumed)
                {
                    // Rollback funds
                    RefundFunds(result.TotalCost);
                    
                    result.Success = false;
                    result.ErrorMessage = "Failed to consume resources";
                    
                    OnPaymentError?.Invoke(new PaymentError
                    {
                        ErrorCode = "RESOURCE_CONSUMPTION_FAILED",
                        ErrorMessage = result.ErrorMessage,
                        AttemptedCost = result.TotalCost,
                        Position = gridPosition
                    });
                    
                    return result;
                }

                result.Success = true;
                ChimeraLogger.Log($"[PaymentProcessor] Immediate payment successful - Cost: {result.TotalCost:F2}");
                return result;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[PaymentProcessor] Error processing immediate payment: {ex.Message}");
                result.Success = false;
                result.ErrorMessage = $"Payment processing error: {ex.Message}";
                return result;
            }
        }

        public PaymentProcessingResult ProcessReservedPayment(string reservationId, Vector3Int gridPosition)
        {
            var result = new PaymentProcessingResult();

            if (_refundHandler == null)
            {
                result.Success = false;
                result.ErrorMessage = "Refund handler not available for reservation processing";
                return result;
            }

            if (!_refundHandler.ActiveReservations.TryGetValue(reservationId, out var reservation))
            {
                result.Success = false;
                result.ErrorMessage = $"Reservation {reservationId} not found";
                return result;
            }

            try
            {
                // Calculate cost from reservation
                result.TotalCost = CalculateReservationCost(reservation.ReservedResources);
                result.ResourceCosts = reservation.ReservedResources;

                // Consume reserved resources
                bool resourcesConsumed = ConsumeReservedResources(result.ResourceCosts);
                if (!resourcesConsumed)
                {
                    result.Success = false;
                    result.ErrorMessage = "Failed to consume reserved resources";
                    return result;
                }

                // Release the reservation
                _refundHandler.ReleaseReservation(reservationId);

                result.Success = true;
                ChimeraLogger.Log($"[PaymentProcessor] Reserved payment successful - Cost: {result.TotalCost:F2}");
                return result;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[PaymentProcessor] Error processing reserved payment: {ex.Message}");
                result.Success = false;
                result.ErrorMessage = $"Reserved payment processing error: {ex.Message}";
                return result;
            }
        }

        public void RecordTransaction(PaymentTransaction transaction)
        {
            _transactionHistory.Add(transaction);
            
            // Maintain transaction history size (keep last 1000 transactions)
            if (_transactionHistory.Count > 1000)
            {
                _transactionHistory.RemoveAt(0);
            }

            ChimeraLogger.Log($"[PaymentProcessor] Transaction recorded: {transaction.TransactionId} - {transaction.Type} - {transaction.TotalCost:F2}");
        }

        public PaymentTransaction GetTransactionById(string transactionId)
        {
            return _transactionHistory.FirstOrDefault(t => t.TransactionId == transactionId);
        }

        public List<PaymentTransaction> GetTransactionsByPosition(Vector3Int gridPosition)
        {
            return _transactionHistory.Where(t => t.Position == gridPosition).ToList();
        }

        public bool ConsumeResources(List<ResourceCost> resources)
        {
            if (_tradingManager == null)
            {
                ChimeraLogger.LogWarning("[PaymentProcessor] Trading manager not available for resource consumption");
                return true; // Allow if we can't check
            }

            if (resources == null || resources.Count == 0)
            {
                return true;
            }

            try
            {
                foreach (var resourceCost in resources)
                {
                    if (!resourceCost.isRequired) continue;

                    bool consumed = ConsumeResourceFromInventory(resourceCost.resourceId, resourceCost.quantity);
                    if (!consumed)
                    {
                        ChimeraLogger.LogWarning($"[PaymentProcessor] Failed to consume resource: {resourceCost.resourceId} x{resourceCost.quantity}");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[PaymentProcessor] Error consuming resources: {ex.Message}");
                return false;
            }
        }

        public bool ConsumeReservedResources(List<ResourceCost> resources)
        {
            // For reserved resources, we can use the same consumption logic
            // In a more sophisticated system, reserved resources might be handled differently
            return ConsumeResources(resources);
        }

        public void CompletePurchase(GridPlaceable placeable)
        {
            ChimeraLogger.Log($"[PaymentProcessor] Purchase completed for {placeable.name}");
        }

        public void UpdatePlayerFunds(float funds)
        {
            ChimeraLogger.Log($"[PaymentProcessor] Player funds updated: {funds:F2}");
        }

        public void UpdatePlayerResources(Dictionary<string, int> resources)
        {
            ChimeraLogger.Log($"[PaymentProcessor] Player resources updated: {resources.Count} resource types");
        }

        public void SetDependencies(MonoBehaviour currencyManager, MonoBehaviour tradingManager, 
                                   ICostCalculator costCalculator, IRefundHandler refundHandler)
        {
            _currencyManager = currencyManager;
            _tradingManager = tradingManager;
            _costCalculator = costCalculator;
            _refundHandler = refundHandler;
        }

        private bool DeductFunds(float amount)
        {
            if (_currencyManager == null)
            {
                ChimeraLogger.LogWarning("[PaymentProcessor] Currency manager not available for fund deduction");
                return true; // Allow if we can't check
            }

            try
            {
                // Use reflection to deduct funds
                var method = _currencyManager.GetType().GetMethod("SpendCurrency");
                if (method != null)
                {
                    // Try with CurrencyType.Cash parameter
                    return (bool)method.Invoke(_currencyManager, new object[] { 0, amount, "Construction Payment" }); // 0 = CurrencyType.Cash
                }

                // Fallback: try simpler spend method
                var spendMethod = _currencyManager.GetType().GetMethod("SpendCash");
                if (spendMethod != null)
                {
                    return (bool)spendMethod.Invoke(_currencyManager, new object[] { amount });
                }

                ChimeraLogger.LogWarning("[PaymentProcessor] Could not find suitable method to deduct funds");
                return true; // Allow if we can't determine
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[PaymentProcessor] Error deducting funds: {ex.Message}");
                return false;
            }
        }

        private void RefundFunds(float amount)
        {
            if (_currencyManager == null) return;

            try
            {
                // Use reflection to refund funds
                var method = _currencyManager.GetType().GetMethod("AddCurrency");
                if (method != null)
                {
                    method.Invoke(_currencyManager, new object[] { 0, amount, "Construction Refund" }); // 0 = CurrencyType.Cash
                }
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[PaymentProcessor] Error refunding funds: {ex.Message}");
            }
        }

        private bool ConsumeResourceFromInventory(string resourceId, int quantity)
        {
            if (_tradingManager == null) return true;

            try
            {
                var method = _tradingManager.GetType().GetMethod("ConsumeResource");
                if (method != null)
                {
                    return (bool)method.Invoke(_tradingManager, new object[] { resourceId, quantity });
                }

                return true; // Allow if we can't check
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[PaymentProcessor] Error consuming resource from inventory: {ex.Message}");
                return false;
            }
        }

        private float CalculateReservationCost(List<ResourceCost> reservedResources)
        {
            // Simple cost calculation for reserved resources
            // In a real system, this might involve complex pricing
            float totalCost = 0f;
            
            foreach (var resource in reservedResources)
            {
                totalCost += resource.quantity * 10f; // Simple cost of 10 per unit
            }

            return totalCost;
        }
    }
}
