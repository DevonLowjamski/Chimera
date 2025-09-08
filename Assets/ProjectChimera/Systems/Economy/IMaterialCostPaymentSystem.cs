using System.Collections.Generic;
using ProjectChimera.Data.Construction;
using ProjectChimera.Data.Economy;

namespace ProjectChimera.Systems.Economy
{
    /// <summary>
    /// Interface for material cost payment system.
    /// Provides cost calculation, validation, and payment processing for construction materials.
    /// Essential for Project Chimera's Contextual Menu System in Construction Mode.
    /// </summary>
    public interface IMaterialCostPaymentSystem
    {
        /// <summary>
        /// Transaction history for tracking payments
        /// </summary>
        List<MaterialTransaction> TransactionHistory { get; }
        
        /// <summary>
        /// Total amount spent across all transactions
        /// </summary>
        float TotalSpent { get; }
        
        /// <summary>
        /// Calculate material costs breakdown for a schematic
        /// </summary>
        MaterialCostBreakdown CalculateMaterialCosts(SchematicSO schematic);
        
        /// <summary>
        /// Validate if payment can be processed for a schematic
        /// </summary>
        PaymentValidationResult ValidatePayment(SchematicSO schematic);
        
        /// <summary>
        /// Process payment for a schematic
        /// </summary>
        bool ProcessPayment(SchematicSO schematic, bool allowCredit = false);
        
        /// <summary>
        /// Process refund for a schematic
        /// </summary>
        float ProcessRefund(SchematicSO schematic, string reason = "Demolition");
        
        /// <summary>
        /// Get payment display data for UI components
        /// Essential for Contextual Menu System material cost display
        /// </summary>
        PaymentDisplayData GetPaymentDisplayData(SchematicSO schematic);
    }
}
