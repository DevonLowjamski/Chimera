using ProjectChimera.Data.Construction;
using ProjectChimera.Data.Economy;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Interface for placement validation and payment verification
    /// </summary>
    public interface IPlacementValidator
    {
        bool EnablePaymentValidation { get; set; }
        float CreditLimit { get; }
        
        PaymentValidationResult ValidatePayment(GridPlaceable placeable, Vector3Int gridPosition);
        ResourceValidationResult ValidateResourceAvailability(List<ResourceCost> resourceCosts);
        
        bool CanAffordAmount(float amount);
        bool ValidateAndReserveFunds(GridPlaceable placeable);
        
        bool HasSufficientResources(string resourceId, int quantity);
        bool HasSufficientFunds(float amount);
        
        void Initialize(bool enablePaymentValidation, float creditLimit);
        void Shutdown();
    }

    public struct PaymentValidationResult
    {
        public bool IsValid;
        public float TotalCost;
        public List<ResourceCost> ResourceCosts;
        public string ErrorMessage;
        public Dictionary<string, float> CostBreakdown;
        public Dictionary<string, int> MissingResources;
    }

    public struct ResourceValidationResult
    {
        public bool IsValid;
        public string ErrorMessage;
        public Dictionary<string, int> MissingResources;
        public Dictionary<string, int> AvailableResources;
    }
}
