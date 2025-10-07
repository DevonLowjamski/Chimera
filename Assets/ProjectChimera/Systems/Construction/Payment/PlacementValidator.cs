using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Construction;
using ProjectChimera.Data.Economy;
using ICurrencyManager = ProjectChimera.Systems.Services.Economy.ICurrencyManager;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Implementation for placement validation and payment verification
    /// </summary>
    public class PlacementValidator : IPlacementValidator
    {
        private bool _enablePaymentValidation = true;
        private float _creditLimit = 10000f;
        private bool _isInitialized = false;

        // External dependencies
        private ICurrencyManager _currencyManager;
        private MonoBehaviour _tradingManager;
        private ICostCalculator _costCalculator;

        public bool EnablePaymentValidation
        {
            get => _enablePaymentValidation;
            set => _enablePaymentValidation = value;
        }

        public float CreditLimit => _creditLimit;

        public PlacementValidator(ICurrencyManager currencyManager = null, MonoBehaviour tradingManager = null, ICostCalculator costCalculator = null)
        {
            _currencyManager = currencyManager;
            _tradingManager = tradingManager;
            _costCalculator = costCalculator;
        }

        public void Initialize(bool enablePaymentValidation, float creditLimit)
        {
            _enablePaymentValidation = enablePaymentValidation;
            _creditLimit = creditLimit;
            _isInitialized = true;

            ChimeraLogger.Log("OTHER", "PlacementValidator initialized", null);
        }

        public void Shutdown()
        {
            _isInitialized = false;
            ChimeraLogger.Log("OTHER", "PlacementValidator shutdown", null);
        }

        public PaymentValidationResult ValidatePayment(GridPlaceable placeable, Vector3Int gridPosition)
        {
            var result = new PaymentValidationResult();

            if (!_enablePaymentValidation)
            {
                result.IsValid = true;
                result.TotalCost = 0f;
                return result;
            }

            if (_costCalculator == null)
            {
                result.IsValid = false;
                result.ErrorMessage = "Cost calculator not available";
                return result;
            }

            // Calculate total cost
            var costBreakdown = _costCalculator.CalculatePlacementCost(placeable, gridPosition);
            result.TotalCost = costBreakdown.TotalCost;
            result.ResourceCosts = costBreakdown.ResourceCosts;
            result.CostBreakdown = costBreakdown.Breakdown;

            // Validate currency availability
            if (_currencyManager != null)
            {
                bool canAfford = CanAffordAmount(result.TotalCost);
                if (!canAfford)
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"Insufficient funds. Required: {result.TotalCost:F0}";
                    return result;
                }
            }

            // Validate resource availability
            var resourceValidation = ValidateResourceAvailability(result.ResourceCosts);
            if (!resourceValidation.IsValid)
            {
                result.IsValid = false;
                result.ErrorMessage = resourceValidation.ErrorMessage;
                result.MissingResources = resourceValidation.MissingResources;
                return result;
            }

            result.IsValid = true;
            result.ErrorMessage = string.Empty;

            ChimeraLogger.Log("OTHER", "Payment validation successful", null);
            return result;
        }

        public ResourceValidationResult ValidateResourceAvailability(List<ResourceCost> resourceCosts)
        {
            var result = new ResourceValidationResult
            {
                IsValid = true,
                MissingResources = new Dictionary<string, int>(),
                AvailableResources = new Dictionary<string, int>()
            };

            if (resourceCosts == null || resourceCosts.Count == 0)
            {
                return result;
            }

            foreach (var resourceCost in resourceCosts)
            {
                if (!resourceCost.isRequired) continue;

                bool hasEnough = HasSufficientResources(resourceCost.resourceId, resourceCost.quantity);
                if (!hasEnough)
                {
                    result.IsValid = false;
                    result.MissingResources[resourceCost.resourceId] = resourceCost.quantity;

                    if (string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        result.ErrorMessage = $"Insufficient resources: {resourceCost.resourceId} (need {resourceCost.quantity})";
                    }
                }
                else
                {
                    // Track available resources for reference
                    result.AvailableResources[resourceCost.resourceId] = GetAvailableResourceQuantity(resourceCost.resourceId);
                }
            }

            if (!result.IsValid)
            {
                ChimeraLogger.Log("OTHER", "Resource validation failed", null);
            }

            return result;
        }

        public bool CanAffordAmount(float amount)
        {
            if (_currencyManager == null)
            {
                ChimeraLogger.Log("OTHER", "Currency manager not available for affordability check", null);
                return true; // Allow if we can't check
            }

            try
            {
                // Use interface method directly instead of reflection
                return _currencyManager.HasSufficientFunds(amount);
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.Log("OTHER", "Error checking currency affordability", null);
                return true; // Allow if error occurs
            }
        }

        public bool ValidateAndReserveFunds(GridPlaceable placeable)
        {
            if (!_enablePaymentValidation)
            {
                return true;
            }

            if (_costCalculator == null)
            {
                ChimeraLogger.Log("OTHER", "Cost calculator not available for fund reservation", null);
                return false;
            }

            // Calculate cost for validation
            var costResult = _costCalculator.CalculatePlacementCost(placeable, Vector3Int.zero); // Position-agnostic validation

            return CanAffordAmount(costResult.TotalCost);
        }

        public bool HasSufficientResources(string resourceId, int quantity)
        {
            if (_tradingManager == null)
            {
                ChimeraLogger.Log("OTHER", "Trading manager not available for resource check", null);
                return true; // Allow if we can't check
            }

            try
            {
                // TODO: Replace with proper interface method call when TradingInventoryManager is integrated
                // For now, assume resource is available as inventory checking is not yet implemented
                ChimeraLogger.Log("OTHER", "Resource check not implemented yet", null);
                return true; // Allow if we can't determine
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.Log("OTHER", "Error checking resource availability", null);
                return true; // Allow if error occurs
            }
        }

        public bool HasSufficientFunds(float amount)
        {
            return CanAffordAmount(amount);
        }

        public void SetDependencies(ICurrencyManager currencyManager, MonoBehaviour tradingManager, ICostCalculator costCalculator)
        {
            _currencyManager = currencyManager;
            _tradingManager = tradingManager;
            _costCalculator = costCalculator;
        }

        private int GetAvailableResourceQuantity(string resourceId)
        {
            if (_tradingManager == null)
            {
                return 0;
            }

            try
            {
                // TODO: Replace with proper interface method call when TradingInventoryManager is integrated
                // For now, return a placeholder value as inventory quantity checking is not yet implemented
                ChimeraLogger.Log("OTHER", "Resource quantity check not implemented yet", null);
                return 999; // Placeholder value indicating plenty available
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.Log("OTHER", "Error getting resource quantity", null);
                return 0;
            }
        }
    }
}
