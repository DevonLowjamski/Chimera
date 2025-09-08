using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core;
using ProjectChimera.Data.Construction;
using ProjectChimera.Data.Economy;
// using ProjectChimera.Systems.Construction; // Commented out - namespace verification needed

namespace ProjectChimera.Systems.Economy
{
    /// <summary>
    /// System for handling material cost payments for schematic applications.
    /// Integrates with CurrencyManager and provides cost validation, payment processing,
    /// and UI feedback for construction material costs in Project Chimera Phase 4.
    /// </summary>
    public class MaterialCostPaymentSystem : ChimeraManager, IMaterialCostPaymentSystem
    {
        [Header("Payment Configuration")]
        [SerializeField] private bool _enableCostPayments = true;
        [SerializeField] private bool _allowCreditPurchases = false;
        [SerializeField] private float _materialCostMultiplier = 1.0f;
        [SerializeField] private float _rushOrderMultiplier = 1.5f;

        [Header("Cost Validation")]
        [SerializeField] private bool _strictCostValidation = true;
        [SerializeField] private bool _showCostWarnings = true;
        [SerializeField] private float _warningThresholdPercentage = 0.8f; // Warn when 80% of funds would be spent

        [Header("Transaction Tracking")]
        [SerializeField] private bool _trackMaterialTransactions = true;
        [SerializeField] private int _maxTransactionHistory = 100;
        [SerializeField] private bool _enableRefunds = true;
        [SerializeField] private float _refundPercentage = 0.8f; // 80% refund when demolishing

        // System references
        private CurrencyManager _currencyManager;
        private object _placementController;

        // Payment tracking
        private List<MaterialTransaction> _transactionHistory = new List<MaterialTransaction>();
        private Dictionary<string, MaterialCostBreakdown> _costBreakdowns = new Dictionary<string, MaterialCostBreakdown>();

        // Events
        public System.Action<MaterialTransaction> OnMaterialPurchased;
        public System.Action<MaterialTransaction> OnPaymentFailed;
        public System.Action<float, float> OnInsufficientFunds; // cost, available
        public System.Action<MaterialCostBreakdown> OnCostCalculated;
        public System.Action<string> OnRefundProcessed;

        public override ManagerPriority Priority => ManagerPriority.High;

        // Public Properties
        public bool CostPaymentsEnabled => _enableCostPayments;
        public float MaterialCostMultiplier => _materialCostMultiplier;
        public List<MaterialTransaction> TransactionHistory => new List<MaterialTransaction>(_transactionHistory);
        public int TotalTransactions => _transactionHistory.Count;
        public float TotalSpent => _transactionHistory.Sum(t => t.Amount);

        protected override void OnManagerInitialize()
        {
            FindSystemReferences();
            InitializePaymentSystem();
            SetupEventHandlers();

            // Register service interface for dependency injection
            ServiceContainerFactory.Instance?.RegisterSingleton<IMaterialCostPaymentSystem>(this);

            LogInfo($"MaterialCostPaymentSystem initialized - Cost payments: {(_enableCostPayments ? "ENABLED" : "DISABLED")}");
        }

        /// <summary>
        /// Calculate material costs for a schematic
        /// </summary>
        public MaterialCostBreakdown CalculateMaterialCosts(SchematicSO schematic)
        {
            if (schematic == null)
            {
                LogError("Cannot calculate costs for null schematic");
                return new MaterialCostBreakdown();
            }

            var breakdown = new MaterialCostBreakdown
            {
                SchematicId = schematic.name,
                SchematicName = schematic.SchematicName,
                BaseItemCosts = new Dictionary<string, float>(),
                MaterialCategories = new Dictionary<MaterialCategory, float>(),
                TotalBaseCost = schematic.TotalEstimatedCost,
                CostMultiplier = _materialCostMultiplier,
                FinalCost = schematic.TotalEstimatedCost * _materialCostMultiplier,
                CalculationTime = System.DateTime.Now
            };

            // Calculate individual item costs
            foreach (var item in schematic.Items)
            {
                var itemCost = item.EstimatedCost * _materialCostMultiplier;
                breakdown.BaseItemCosts[item.ItemName] = itemCost;

                // Categorize costs by material type
                var category = DetermineMaterialCategory(item);
                if (!breakdown.MaterialCategories.ContainsKey(category))
                    breakdown.MaterialCategories[category] = 0f;

                breakdown.MaterialCategories[category] += itemCost;
            }

            // Store breakdown for reference
            _costBreakdowns[schematic.name] = breakdown;

            OnCostCalculated?.Invoke(breakdown);

            LogInfo($"Calculated material costs for '{schematic.SchematicName}': ${breakdown.FinalCost:F2}");
            return breakdown;
        }

        /// <summary>
        /// Validate if player can afford the schematic costs
        /// </summary>
        public PaymentValidationResult ValidatePayment(SchematicSO schematic)
        {
            if (!_enableCostPayments)
            {
                return new PaymentValidationResult
                {
                    CanAfford = true,
                    ValidationMessage = "Cost payments disabled",
                    CostBreakdown = new MaterialCostBreakdown()
                };
            }

            var breakdown = CalculateMaterialCosts(schematic);
            var availableFunds = _currencyManager.Cash;
            var canAfford = availableFunds >= breakdown.FinalCost;

            var result = new PaymentValidationResult
            {
                CanAfford = canAfford,
                CostBreakdown = breakdown,
                AvailableFunds = availableFunds,
                ShortfallAmount = canAfford ? 0f : breakdown.FinalCost - availableFunds,
                WarningLevel = CalculateWarningLevel(breakdown.FinalCost, availableFunds)
            };

            // Generate validation message
            if (!canAfford)
            {
                result.ValidationMessage = $"Insufficient funds: Need ${breakdown.FinalCost:F2}, Have ${availableFunds:F2}";
            }
            else if (result.WarningLevel == CostWarningLevel.High)
            {
                result.ValidationMessage = $"Warning: This will use {(breakdown.FinalCost / availableFunds):P1} of your funds";
            }
            else
            {
                result.ValidationMessage = $"Cost: ${breakdown.FinalCost:F2} (${availableFunds - breakdown.FinalCost:F2} remaining)";
            }

            return result;
        }

        /// <summary>
        /// Process payment for schematic material costs
        /// </summary>
        public bool ProcessPayment(SchematicSO schematic, bool allowCredit = false)
        {
            if (!_enableCostPayments)
            {
                LogInfo($"Cost payments disabled - allowing free application of '{schematic.SchematicName}'");
                return true;
            }

            var validation = ValidatePayment(schematic);

            if (!validation.CanAfford && (!allowCredit || !_allowCreditPurchases))
            {
                OnInsufficientFunds?.Invoke(validation.CostBreakdown.FinalCost, validation.AvailableFunds);
                OnPaymentFailed?.Invoke(new MaterialTransaction
                {
                    TransactionId = System.Guid.NewGuid().ToString(),
                    SchematicId = schematic.name,
                    SchematicName = schematic.SchematicName,
                    Amount = validation.CostBreakdown.FinalCost,
                    TransactionType = MaterialTransactionType.FailedPurchase,
                    Timestamp = System.DateTime.Now,
                    FailureReason = "Insufficient funds"
                });

                LogWarning($"Payment failed for '{schematic.SchematicName}': {validation.ValidationMessage}");
                return false;
            }

            // Process the payment
            bool paymentSuccess = _currencyManager.SpendCurrency(
                CurrencyType.Cash,
                validation.CostBreakdown.FinalCost,
                $"Material costs for schematic: {schematic.SchematicName}",
                TransactionCategory.Equipment,
                allowCredit && _allowCreditPurchases
            );

            if (paymentSuccess)
            {
                var transaction = new MaterialTransaction
                {
                    TransactionId = System.Guid.NewGuid().ToString(),
                    SchematicId = schematic.name,
                    SchematicName = schematic.SchematicName,
                    Amount = validation.CostBreakdown.FinalCost,
                    TransactionType = MaterialTransactionType.Purchase,
                    Timestamp = System.DateTime.Now,
                    CostBreakdown = validation.CostBreakdown,
                    PaymentMethod = allowCredit && validation.AvailableFunds < validation.CostBreakdown.FinalCost
                        ? PaymentMethodType.Credit : PaymentMethodType.Cash
                };

                RecordTransaction(transaction);
                OnMaterialPurchased?.Invoke(transaction);

                LogInfo($"Successfully processed payment for '{schematic.SchematicName}': ${validation.CostBreakdown.FinalCost:F2}");
                return true;
            }
            else
            {
                OnPaymentFailed?.Invoke(new MaterialTransaction
                {
                    TransactionId = System.Guid.NewGuid().ToString(),
                    SchematicId = schematic.name,
                    SchematicName = schematic.SchematicName,
                    Amount = validation.CostBreakdown.FinalCost,
                    TransactionType = MaterialTransactionType.FailedPurchase,
                    Timestamp = System.DateTime.Now,
                    FailureReason = "Payment processing failed"
                });

                LogError($"Payment processing failed for '{schematic.SchematicName}'");
                return false;
            }
        }

        /// <summary>
        /// Process refund when demolishing schematic items
        /// </summary>
        public float ProcessRefund(SchematicSO schematic, string reason = "Demolition")
        {
            if (!_enableRefunds || !_enableCostPayments)
            {
                LogInfo("Refunds disabled - no refund processed");
                return 0f;
            }

            // Find the original purchase transaction
            var originalTransaction = _transactionHistory
                .LastOrDefault(t => t.SchematicId == schematic.name && t.TransactionType == MaterialTransactionType.Purchase);

            if (originalTransaction == null)
            {
                LogWarning($"No purchase transaction found for refund of '{schematic.SchematicName}'");
                return 0f;
            }

            var refundAmount = originalTransaction.Amount * _refundPercentage;

            bool refundSuccess = _currencyManager.AddCurrency(
                CurrencyType.Cash,
                refundAmount,
                $"Refund for demolished schematic: {schematic.SchematicName}",
                TransactionCategory.Equipment
            );

            if (refundSuccess)
            {
                var refundTransaction = new MaterialTransaction
                {
                    TransactionId = System.Guid.NewGuid().ToString(),
                    SchematicId = schematic.name,
                    SchematicName = schematic.SchematicName,
                    Amount = refundAmount,
                    TransactionType = MaterialTransactionType.Refund,
                    Timestamp = System.DateTime.Now,
                    OriginalTransactionId = originalTransaction.TransactionId,
                    RefundReason = reason
                };

                RecordTransaction(refundTransaction);
                OnRefundProcessed?.Invoke($"Refunded ${refundAmount:F2} for {schematic.SchematicName}");

                LogInfo($"Processed refund for '{schematic.SchematicName}': ${refundAmount:F2} ({_refundPercentage:P0} of ${originalTransaction.Amount:F2})");
                return refundAmount;
            }
            else
            {
                LogError($"Failed to process refund for '{schematic.SchematicName}'");
                return 0f;
            }
        }

        /// <summary>
        /// Get cost breakdown for UI display
        /// </summary>
        public PaymentDisplayData GetPaymentDisplayData(SchematicSO schematic)
        {
            var validation = ValidatePayment(schematic);

            return new PaymentDisplayData
            {
                SchematicName = schematic.SchematicName,
                TotalCost = validation.CostBreakdown.FinalCost,
                AvailableFunds = validation.AvailableFunds,
                CanAfford = validation.CanAfford,
                WarningLevel = validation.WarningLevel,
                CostBreakdown = validation.CostBreakdown,
                FormattedCost = $"${validation.CostBreakdown.FinalCost:F2}",
                FormattedAvailable = $"${validation.AvailableFunds:F2}",
                AffordabilityMessage = validation.ValidationMessage
            };
        }

        private void FindSystemReferences()
        {
            _currencyManager = GameManager.Instance?.GetManager<CurrencyManager>();
            _placementController = ServiceContainerFactory.Instance?.TryResolve<Iobject>() as object;

            if (_currencyManager == null)
            {
                LogError("CurrencyManager not found - payment system will not function properly");
            }
        }

        private void InitializePaymentSystem()
        {
            _transactionHistory = new List<MaterialTransaction>();
            _costBreakdowns = new Dictionary<string, MaterialCostBreakdown>();
        }

        private void SetupEventHandlers()
        {
            // Listen for schematic application events if placement controller is available
            if (_placementController != null)
            {
                // In a full implementation, would hook into placement events here
            }
        }

        private MaterialCategory DetermineMaterialCategory(SchematicItem item)
        {
            // Determine material category based on item properties
            // This is a simplified categorization - in a full implementation would be more sophisticated

            switch (item.ItemCategory)
            {
                case ConstructionCategory.Structure:
                    return MaterialCategory.Structural;
                case ConstructionCategory.Equipment:
                    return MaterialCategory.Equipment;
                case ConstructionCategory.Utility:
                    return MaterialCategory.Utilities;
                case ConstructionCategory.Decoration:
                    return MaterialCategory.Decorative;
                default:
                    return MaterialCategory.General;
            }
        }

        private CostWarningLevel CalculateWarningLevel(float cost, float availableFunds)
        {
            if (cost > availableFunds)
                return CostWarningLevel.Critical;

            var percentageOfFunds = cost / availableFunds;

            if (percentageOfFunds >= _warningThresholdPercentage)
                return CostWarningLevel.High;
            else if (percentageOfFunds >= 0.5f)
                return CostWarningLevel.Medium;
            else
                return CostWarningLevel.Low;
        }

        private void RecordTransaction(MaterialTransaction transaction)
        {
            if (!_trackMaterialTransactions) return;

            _transactionHistory.Add(transaction);

            // Limit history size
            if (_transactionHistory.Count > _maxTransactionHistory)
            {
                _transactionHistory.RemoveAt(0);
            }
        }

        protected override void OnManagerShutdown()
        {
            LogInfo($"MaterialCostPaymentSystem shutdown - {TotalTransactions} transactions, ${TotalSpent:F2} total spent");
        }
    }

    /// <summary>
    /// Material cost breakdown for a schematic
    /// </summary>
    [System.Serializable]
    public class MaterialCostBreakdown
    {
        public string SchematicId;
        public string SchematicName;
        public Dictionary<string, float> BaseItemCosts = new Dictionary<string, float>();
        public Dictionary<MaterialCategory, float> MaterialCategories = new Dictionary<MaterialCategory, float>();
        public float TotalBaseCost;
        public float CostMultiplier;
        public float FinalCost;
        public System.DateTime CalculationTime;
        public MaterialCostBreakdown Breakdown; // For nested breakdowns if needed
    }

    /// <summary>
    /// Payment validation result
    /// </summary>
    [System.Serializable]
    public class PaymentValidationResult
    {
        public bool CanAfford;
        public MaterialCostBreakdown CostBreakdown;
        public float AvailableFunds;
        public float ShortfallAmount;
        public string ValidationMessage;
        public CostWarningLevel WarningLevel;
    }

    /// <summary>
    /// Payment display data for UI
    /// </summary>
    [System.Serializable]
    public class PaymentDisplayData
    {
        public string SchematicName;
        public float TotalCost;
        public float AvailableFunds;
        public bool CanAfford;
        public CostWarningLevel WarningLevel;
        public MaterialCostBreakdown CostBreakdown;
        public string FormattedCost;
        public string FormattedAvailable;
        public string AffordabilityMessage;
    }

    /// <summary>
    /// Material transaction record
    /// </summary>
    [System.Serializable]
    public class MaterialTransaction
    {
        public string TransactionId;
        public string SchematicId;
        public string SchematicName;
        public float Amount;
        public MaterialTransactionType TransactionType;
        public System.DateTime Timestamp;
        public MaterialCostBreakdown CostBreakdown;
        public PaymentMethodType PaymentMethod;
        public string OriginalTransactionId; // For refunds
        public string RefundReason;
        public string FailureReason;
    }

    /// <summary>
    /// Material categories for cost tracking
    /// </summary>
    public enum MaterialCategory
    {
        Structural,
        Equipment,
        Utilities,
        Decorative,
        General
    }

    /// <summary>
    /// Cost warning levels
    /// </summary>
    public enum CostWarningLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Material transaction types
    /// </summary>
    public enum MaterialTransactionType
    {
        Purchase,
        Refund,
        FailedPurchase
    }

}
