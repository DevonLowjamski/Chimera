using ProjectChimera.Data.Construction;
using ProjectChimera.Data.Economy;
using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Interface for payment processing and transaction management
    /// </summary>
    public interface IPaymentProcessor
    {
        bool RequireInstantPayment { get; set; }
        bool EnablePaymentPlans { get; set; }
        
        List<PaymentTransaction> TransactionHistory { get; }
        Dictionary<Vector3Int, string> PlacementTransactions { get; }
        
        PaymentProcessingResult ProcessPayment(GridPlaceable placeable, Vector3Int gridPosition, string reservationId = null);
        PaymentProcessingResult ProcessImmediatePayment(GridPlaceable placeable, Vector3Int gridPosition);
        PaymentProcessingResult ProcessReservedPayment(string reservationId, Vector3Int gridPosition);
        
        void RecordTransaction(PaymentTransaction transaction);
        PaymentTransaction GetTransactionById(string transactionId);
        List<PaymentTransaction> GetTransactionsByPosition(Vector3Int gridPosition);
        
        bool ConsumeResources(List<ResourceCost> resources);
        bool ConsumeReservedResources(List<ResourceCost> resources);
        
        void CompletePurchase(GridPlaceable placeable);
        void UpdatePlayerFunds(float funds);
        void UpdatePlayerResources(Dictionary<string, int> resources);
        
        // Events
        Action<PaymentTransaction> OnPaymentProcessed { get; set; }
        Action<PaymentError> OnPaymentError { get; set; }
        
        void Initialize(bool requireInstantPayment, bool enablePaymentPlans);
        void Shutdown();
    }

    public struct PaymentProcessingResult
    {
        public bool Success;
        public float TotalCost;
        public List<ResourceCost> ResourceCosts;
        public string ErrorMessage;
        public string TransactionId;
    }

    public struct PaymentTransaction
    {
        public string TransactionId;
        public Vector3Int Position;
        public float TotalCost;
        public List<ResourceCost> ResourceCosts;
        public float Timestamp;
        public TransactionType Type;
        public TransactionStatus Status;
        public string Description;
    }

    public enum TransactionType
    {
        Purchase,
        Refund,
        Reservation,
        Release
    }

    public enum TransactionStatus
    {
        Pending,
        Completed,
        Failed,
        Cancelled
    }

    public struct PaymentError
    {
        public string ErrorCode;
        public string ErrorMessage;
        public float AttemptedCost;
        public Vector3Int Position;
    }
}
