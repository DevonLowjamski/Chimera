using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core;
using ProjectChimera.Data.Construction;
using ProjectChimera.Data.Economy;
// Economy managers will be accessed via FindObjectOfType for now

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Handles payment processing and cost calculations for grid-based construction.
    /// Manages economy integration, resource validation, and height-based cost modifiers.
    /// </summary>
    public class PlacementPaymentService : MonoBehaviour
    {
        [Header("Cost Configuration")]
        [SerializeField] private bool _enablePaymentValidation = true;
        [SerializeField] private float _heightCostMultiplier = 1.15f;
        [SerializeField] private float _foundationCostMultiplier = 1.25f;
        [SerializeField] private bool _enableBulkDiscounts = true;
        [SerializeField] private float _bulkDiscountThreshold = 5f;
        [SerializeField] private float _bulkDiscountRate = 0.1f;

        [Header("Resource Management")]
        [SerializeField] private bool _enableResourceReservation = true;
        [SerializeField] private float _reservationDuration = 30f;
        [SerializeField] private bool _autoReleaseReservations = true;
        [SerializeField] private int _maxSimultaneousReservations = 10;

        [Header("Payment Processing")]
        [SerializeField] private bool _requireInstantPayment = false;
        [SerializeField] private bool _enablePaymentPlans = true;
        [SerializeField] private float _creditLimit = 10000f;
        [SerializeField] private bool _enableRefunds = true;
        [SerializeField] private float _refundPercentage = 0.8f;

        // Core references
        private GridSystem _gridSystem;
        private MonoBehaviour _currencyManager; // Generic reference to avoid type dependency
        private MonoBehaviour _tradingManager;  // Generic reference to avoid type dependency

        // Cost calculation
        private Dictionary<PlaceableType, CostProfile> _baseCosts = new Dictionary<PlaceableType, CostProfile>();
        private Dictionary<Vector3Int, float> _positionCostModifiers = new Dictionary<Vector3Int, float>();

        // Resource reservation
        private Dictionary<string, ResourceReservation> _activeReservations = new Dictionary<string, ResourceReservation>();
        private Dictionary<Vector3Int, List<string>> _positionReservations = new Dictionary<Vector3Int, List<string>>();

        // Transaction tracking
        private List<PaymentTransaction> _transactionHistory = new List<PaymentTransaction>();
        private Dictionary<Vector3Int, string> _placementTransactions = new Dictionary<Vector3Int, string>();

        // Events
        public System.Action<PaymentValidationResult> OnPaymentValidated;
        public System.Action<PaymentTransaction> OnPaymentProcessed;
        public System.Action<string, ResourceReservation> OnResourceReserved;
        public System.Action<string> OnReservationReleased;
        public System.Action<PaymentError> OnPaymentError;

        [System.Serializable]
        public class CostProfile
        {
            public float baseCost = 100f;
            public List<ResourceCost> resourceCosts = new List<ResourceCost>();
            public bool scalableWithSize = true;
            public float complexityMultiplier = 1f;
        }

        [System.Serializable]
        public class ResourceCost
        {
            public string resourceId;
            public int quantity;
            public bool isRequired = true;
        }

        public struct ResourceReservation
        {
            public string ReservationId;
            public Vector3Int Position;
            public List<ResourceCost> ReservedResources;
            public float ReservationTime;
            public float ExpiryTime;
            public bool IsActive;
            public string PlayerId;
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

        private void Awake()
        {
            InitializeComponents();
            InitializeBaseCosts();
        }

        private void Start()
        {
            RegisterEconomyEvents();
        }

        private void Update()
        {
            if (_autoReleaseReservations)
                ProcessExpiredReservations();
        }

        private void InitializeComponents()
        {
            _gridSystem = FindObjectOfType<GridSystem>();
            // Find economy managers by name to avoid type dependencies
            var currencyObj = GameObject.Find("CurrencyManager");
            _currencyManager = currencyObj?.GetComponent<MonoBehaviour>();
            
            var tradingObj = GameObject.Find("TradingManager");
            _tradingManager = tradingObj?.GetComponent<MonoBehaviour>();

            if (_gridSystem == null)
                Debug.LogError("[PlacementPaymentService] GridSystem not found!");
        }

        private void InitializeBaseCosts()
        {
            // Initialize default cost profiles for each placeable type
            _baseCosts[PlaceableType.Structure] = new CostProfile
            {
                baseCost = 500f,
                resourceCosts = new List<ResourceCost>
                {
                    new ResourceCost { resourceId = "concrete", quantity = 10, isRequired = true },
                    new ResourceCost { resourceId = "steel", quantity = 5, isRequired = true }
                },
                scalableWithSize = true,
                complexityMultiplier = 1.2f
            };

            _baseCosts[PlaceableType.Equipment] = new CostProfile
            {
                baseCost = 1000f,
                resourceCosts = new List<ResourceCost>
                {
                    new ResourceCost { resourceId = "electronics", quantity = 1, isRequired = true },
                    new ResourceCost { resourceId = "steel", quantity = 3, isRequired = true }
                },
                scalableWithSize = false,
                complexityMultiplier = 1.5f
            };

            _baseCosts[PlaceableType.Structure] = new CostProfile
            {
                baseCost = 2000f,
                resourceCosts = new List<ResourceCost>
                {
                    new ResourceCost { resourceId = "concrete", quantity = 20, isRequired = true },
                    new ResourceCost { resourceId = "cable", quantity = 10, isRequired = true }
                },
                scalableWithSize = true,
                complexityMultiplier = 2f
            };
        }

        private void RegisterEconomyEvents()
        {
            if (_currencyManager != null)
            {
                // Register for economy-related events
            }
        }

        #region Public API

        /// <summary>
        /// Validate payment requirements for placing an object at specified position
        /// </summary>
        public PaymentValidationResult ValidatePayment(GridPlaceable placeable, Vector3Int gridPosition)
        {
            var result = new PaymentValidationResult();

            if (!_enablePaymentValidation)
            {
                result.IsValid = true;
                result.TotalCost = 0f;
                return result;
            }

            // Calculate total cost
            var costBreakdown = CalculatePlacementCost(placeable, gridPosition);
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
                }
            }

            // Validate resource availability
            var resourceValidation = ValidateResourceAvailability(result.ResourceCosts);
            if (!resourceValidation.IsValid)
            {
                result.IsValid = false;
                result.ErrorMessage = resourceValidation.ErrorMessage;
                result.MissingResources = resourceValidation.MissingResources;
            }

            if (result.IsValid)
            {
                result.IsValid = true;
                result.ErrorMessage = string.Empty;
            }

            OnPaymentValidated?.Invoke(result);
            return result;
        }

        /// <summary>
        /// Process payment for object placement
        /// </summary>
        public PaymentProcessingResult ProcessPayment(GridPlaceable placeable, Vector3Int gridPosition, string reservationId = null)
        {
            var result = new PaymentProcessingResult();

            // Use existing reservation if available
            if (!string.IsNullOrEmpty(reservationId) && _activeReservations.ContainsKey(reservationId))
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
                ResourceCosts = result.ResourceCosts,
                Timestamp = Time.time,
                Type = TransactionType.Purchase,
                Status = result.Success ? TransactionStatus.Completed : TransactionStatus.Failed,
                Description = $"Placement of {placeable.name} at {gridPosition}"
            };

            _transactionHistory.Add(transaction);
            if (result.Success)
                _placementTransactions[gridPosition] = transaction.TransactionId;

            OnPaymentProcessed?.Invoke(transaction);
            return result;
        }

        /// <summary>
        /// Create resource reservation for future placement
        /// </summary>
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

            // Calculate cost
            var costBreakdown = CalculatePlacementCost(placeable, gridPosition);

            // Validate resource availability
            var resourceValidation = ValidateResourceAvailability(costBreakdown.ResourceCosts);
            if (!resourceValidation.IsValid)
            {
                result.Success = false;
                result.ErrorMessage = resourceValidation.ErrorMessage;
                return result;
            }

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
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = "Failed to reserve resources";
            }

            return result;
        }

        /// <summary>
        /// Release resource reservation
        /// </summary>
        public bool ReleaseReservation(string reservationId)
        {
            if (!_activeReservations.ContainsKey(reservationId))
                return false;

            var reservation = _activeReservations[reservationId];

            // Release reserved resources
            ReleaseReservedResources(reservation.ReservedResources);

            // Clean up tracking
            _activeReservations.Remove(reservationId);
            if (_positionReservations.ContainsKey(reservation.Position))
            {
                _positionReservations[reservation.Position].Remove(reservationId);
                if (_positionReservations[reservation.Position].Count == 0)
                    _positionReservations.Remove(reservation.Position);
            }

            OnReservationReleased?.Invoke(reservationId);
            return true;
        }

        /// <summary>
        /// Process refund for removed object
        /// </summary>
        public RefundResult ProcessRefund(Vector3Int gridPosition)
        {
            var result = new RefundResult();

            if (!_enableRefunds)
            {
                result.Success = false;
                result.ErrorMessage = "Refunds are disabled";
                return result;
            }

            if (!_placementTransactions.ContainsKey(gridPosition))
            {
                result.Success = false;
                result.ErrorMessage = "No transaction record found for this position";
                return result;
            }

            string transactionId = _placementTransactions[gridPosition];
            var originalTransaction = _transactionHistory.FirstOrDefault(t => t.TransactionId == transactionId);

            if (originalTransaction.TransactionId == null)
            {
                result.Success = false;
                result.ErrorMessage = "Original transaction not found";
                return result;
            }

            // Calculate refund amount
            float refundAmount = originalTransaction.TotalCost * _refundPercentage;
            
            // Process refund
            if (_currencyManager != null)
            {
                // Simplified refund - in a real implementation this would call the economy system
                Debug.Log($"Processing refund of {refundAmount:F2}");
            }

            // Refund resources (partial)
            RefundResources(originalTransaction.ResourceCosts);

            // Record refund transaction
            var refundTransaction = new PaymentTransaction
            {
                TransactionId = System.Guid.NewGuid().ToString(),
                Position = gridPosition,
                TotalCost = refundAmount,
                ResourceCosts = originalTransaction.ResourceCosts,
                Timestamp = Time.time,
                Type = TransactionType.Refund,
                Status = TransactionStatus.Completed,
                Description = $"Refund for object at {gridPosition}"
            };

            _transactionHistory.Add(refundTransaction);
            _placementTransactions.Remove(gridPosition);

            result.Success = true;
            result.RefundAmount = refundAmount;
            result.RefundedResources = CalculateResourceRefund(originalTransaction.ResourceCosts);

            return result;
        }

        /// <summary>
        /// Get cost estimate for placing object at position
        /// </summary>
        public CostEstimate GetCostEstimate(GridPlaceable placeable, Vector3Int gridPosition)
        {
            var costBreakdown = CalculatePlacementCost(placeable, gridPosition);
            
            return new CostEstimate
            {
                TotalCost = costBreakdown.TotalCost,
                ResourceCosts = costBreakdown.ResourceCosts,
                CostBreakdown = costBreakdown.Breakdown,
                HeightModifier = CalculateHeightModifier(gridPosition.z),
                FoundationModifier = RequiresFoundation(gridPosition) ? _foundationCostMultiplier : 1f
            };
        }

        #endregion

        #region Cost Calculation

        public CostCalculationResult CalculatePlacementCost(GridPlaceable placeable, Vector3Int gridPosition)
        {
            var result = new CostCalculationResult();
            var breakdown = new Dictionary<string, float>();

            // Get base cost profile
            if (!_baseCosts.ContainsKey(placeable.Type))
            {
                result.TotalCost = 100f; // Default cost
                result.ResourceCosts = new List<ResourceCost>();
                result.Breakdown = breakdown;
                return result;
            }

            var costProfile = _baseCosts[placeable.Type];
            float baseCost = costProfile.baseCost;
            breakdown["Base Cost"] = baseCost;

            // Apply size scaling
            if (costProfile.scalableWithSize)
            {
                var bounds = placeable.GetObjectBounds();
                float sizeMultiplier = bounds.size.x * bounds.size.y * bounds.size.z;
                baseCost *= sizeMultiplier;
                breakdown["Size Modifier"] = sizeMultiplier;
            }

            // Apply complexity multiplier
            baseCost *= costProfile.complexityMultiplier;
            breakdown["Complexity Modifier"] = costProfile.complexityMultiplier;

            // Apply height-based cost modifier
            float heightModifier = CalculateHeightModifier(gridPosition.z);
            baseCost *= heightModifier;
            breakdown["Height Modifier"] = heightModifier;

            // Apply foundation cost modifier
            if (RequiresFoundation(gridPosition))
            {
                baseCost *= _foundationCostMultiplier;
                breakdown["Foundation Modifier"] = _foundationCostMultiplier;
            }

            // Apply position-specific modifiers
            if (_positionCostModifiers.ContainsKey(gridPosition))
            {
                float positionModifier = _positionCostModifiers[gridPosition];
                baseCost *= positionModifier;
                breakdown["Position Modifier"] = positionModifier;
            }

            result.TotalCost = baseCost;
            result.ResourceCosts = costProfile.resourceCosts.ToList();
            result.Breakdown = breakdown;

            return result;
        }

        private float CalculateHeightModifier(int height)
        {
            if (height <= 0) return 1f;
            return Mathf.Pow(_heightCostMultiplier, height);
        }

        private bool RequiresFoundation(Vector3Int gridPosition)
        {
            return gridPosition.z > 0;
        }

        #endregion

        #region Resource Management

        private ResourceValidationResult ValidateResourceAvailability(List<ResourceCost> requiredResources)
        {
            var result = new ResourceValidationResult { IsValid = true };
            var missingResources = new List<ResourceCost>();

            if (_tradingManager == null)
            {
                result.IsValid = false;
                result.ErrorMessage = "Inventory manager not available";
                return result;
            }

            foreach (var resource in requiredResources)
            {
                if (resource.isRequired)
                {
                    int available = GetResourceQuantityFromInventory(resource.resourceId);
                    if (available < resource.quantity)
                    {
                        missingResources.Add(new ResourceCost
                        {
                            resourceId = resource.resourceId,
                            quantity = resource.quantity - available,
                            isRequired = true
                        });
                    }
                }
            }

            if (missingResources.Count > 0)
            {
                result.IsValid = false;
                result.ErrorMessage = "Insufficient resources";
                result.MissingResources = missingResources;
            }

            return result;
        }

        private bool ReserveResources(List<ResourceCost> resources)
        {
            if (_tradingManager == null) return false;

            foreach (var resource in resources)
            {
                if (!ReserveResourceFromInventory(resource.resourceId, resource.quantity))
                    return false;
            }

            return true;
        }

        private void ReleaseReservedResources(List<ResourceCost> resources)
        {
            if (_tradingManager == null) return;

            foreach (var resource in resources)
            {
                ReleaseReservedResourceFromInventory(resource.resourceId, resource.quantity);
            }
        }

        private bool ConsumeResources(List<ResourceCost> resources)
        {
            if (_tradingManager == null) return false;

            foreach (var resource in resources)
            {
                if (!ConsumeResourceFromInventory(resource.resourceId, resource.quantity))
                    return false;
            }

            return true;
        }

        private void RefundResources(List<ResourceCost> resources)
        {
            if (_tradingManager == null) return;

            foreach (var resource in resources)
            {
                int refundQuantity = Mathf.RoundToInt(resource.quantity * _refundPercentage);
                AddResourceToInventory(resource.resourceId, refundQuantity);
            }
        }

        private List<ResourceCost> CalculateResourceRefund(List<ResourceCost> originalCosts)
        {
            return originalCosts.Select(cost => new ResourceCost
            {
                resourceId = cost.resourceId,
                quantity = Mathf.RoundToInt(cost.quantity * _refundPercentage),
                isRequired = cost.isRequired
            }).ToList();
        }

        #endregion

        #region Payment Processing

        private PaymentProcessingResult ProcessImmediatePayment(GridPlaceable placeable, Vector3Int gridPosition)
        {
            var result = new PaymentProcessingResult();
            var costBreakdown = CalculatePlacementCost(placeable, gridPosition);

            // Charge currency
            if (_currencyManager != null && costBreakdown.TotalCost > 0)
            {
                // Simplified payment - in a real implementation this would call the economy system
                bool paymentSuccessful = CanAffordAmount(costBreakdown.TotalCost);
                if (!paymentSuccessful)
                {
                    result.Success = false;
                    result.ErrorMessage = "Failed to process payment";
                    return result;
                }
            }

            // Consume resources
            if (!ConsumeResources(costBreakdown.ResourceCosts))
            {
                // Refund currency if resource consumption failed
                if (_currencyManager != null)
                {
                    Debug.Log($"Refunding {costBreakdown.TotalCost:F2} due to resource consumption failure");
                }

                result.Success = false;
                result.ErrorMessage = "Failed to consume resources";
                return result;
            }

            result.Success = true;
            result.TotalCost = costBreakdown.TotalCost;
            result.ResourceCosts = costBreakdown.ResourceCosts;

            return result;
        }

        private PaymentProcessingResult ProcessReservedPayment(string reservationId, Vector3Int gridPosition)
        {
            var result = new PaymentProcessingResult();

            if (!_activeReservations.ContainsKey(reservationId))
            {
                result.Success = false;
                result.ErrorMessage = "Reservation not found";
                return result;
            }

            var reservation = _activeReservations[reservationId];

            // Validate reservation hasn't expired
            if (Time.time > reservation.ExpiryTime)
            {
                ReleaseReservation(reservationId);
                result.Success = false;
                result.ErrorMessage = "Reservation has expired";
                return result;
            }

            // Convert reserved resources to consumed
            if (!ConsumeReservedResources(reservation.ReservedResources))
            {
                result.Success = false;
                result.ErrorMessage = "Failed to consume reserved resources";
                return result;
            }

            // Clean up reservation
            _activeReservations.Remove(reservationId);

            result.Success = true;
            result.ResourceCosts = reservation.ReservedResources;

            return result;
        }

        private bool ConsumeReservedResources(List<ResourceCost> resources)
        {
            // In a real implementation, this would convert reserved resources to consumed
            // For now, just return true since resources were already reserved
            return true;
        }

        #endregion

        #region Utility Methods

        private void ProcessExpiredReservations()
        {
            var expiredReservations = _activeReservations.Values
                .Where(r => Time.time > r.ExpiryTime)
                .ToList();

            foreach (var reservation in expiredReservations)
            {
                ReleaseReservation(reservation.ReservationId);
            }
        }

        #endregion

        #region Data Structures

        public struct PaymentValidationResult
        {
            public bool IsValid;
            public float TotalCost;
            public List<ResourceCost> ResourceCosts;
            public Dictionary<string, float> CostBreakdown;
            public string ErrorMessage;
            public List<ResourceCost> MissingResources;
        }

        public struct PaymentProcessingResult
        {
            public bool Success;
            public float TotalCost;
            public List<ResourceCost> ResourceCosts;
            public string ErrorMessage;
            public string TransactionId;
        }

        public struct ResourceReservationResult
        {
            public bool Success;
            public string ReservationId;
            public float ExpiryTime;
            public string ErrorMessage;
        }

        public struct RefundResult
        {
            public bool Success;
            public float RefundAmount;
            public List<ResourceCost> RefundedResources;
            public string ErrorMessage;
        }

        public struct CostEstimate
        {
            public float TotalCost;
            public List<ResourceCost> ResourceCosts;
            public Dictionary<string, float> CostBreakdown;
            public float HeightModifier;
            public float FoundationModifier;
        }

        public struct CostCalculationResult
        {
            public float TotalCost;
            public List<ResourceCost> ResourceCosts;
            public Dictionary<string, float> Breakdown;
        }

        private struct ResourceValidationResult
        {
            public bool IsValid;
            public string ErrorMessage;
            public List<ResourceCost> MissingResources;
        }

        public struct PaymentError
        {
            public string ErrorCode;
            public string ErrorMessage;
            public Vector3Int Position;
            public float AttemptedCost;
        }

        #endregion
        
        #region Player Management
        
        /// <summary>
        /// Gets the current player ID from the player manager or session
        /// </summary>
        private string GetPlayerID()
        {
            // Try to get player ID from various sources
            var playerManager = FindObjectOfType<MonoBehaviour>();
            if (playerManager != null && playerManager.name.Contains("Player"))
            {
                // In a real implementation, this would get from a player management system
                return "Player_" + System.Environment.UserName;
            }
            
            // Fallback to a default player ID
            return "Player_Default";
        }
        
        #endregion
        
        #region Currency Helper Methods
        
        private bool CanAffordAmount(float amount)
        {
            // Simplified check - in a real implementation this would call the economy system
            return _currencyManager != null; // Assume we can afford for now
        }
        
        #endregion
        
        #region Inventory Helper Methods
        
        private int GetResourceQuantityFromInventory(string resourceId)
        {
            // Simplified inventory check - in a real implementation this would query the inventory system
            return _tradingManager != null ? 100 : 0; // Assume we have 100 of any resource
        }
        
        private bool ReserveResourceFromInventory(string resourceId, int quantity)
        {
            // Simplified implementation - in a real system this would mark resources as reserved
            return GetResourceQuantityFromInventory(resourceId) >= quantity;
        }
        
        private void ReleaseReservedResourceFromInventory(string resourceId, int quantity)
        {
            // Simplified implementation - in a real system this would unmark resources as reserved
            // For now, this is a no-op
        }
        
        private bool ConsumeResourceFromInventory(string resourceId, int quantity)
        {
            if (_tradingManager == null) return false;
            
            // Simplified consumption - in a real implementation this would consume from inventory
            Debug.Log($"Consuming {quantity} of {resourceId}");
            return true; // Assume consumption always succeeds
        }
        
        private void AddResourceToInventory(string resourceId, int quantity)
        {
            if (_tradingManager == null) return;
            
            // Simplified addition - in a real implementation this would add to inventory
            Debug.Log($"Adding {quantity} of {resourceId} to inventory");
        }
        
        #endregion
        
        #region Public API for GridPlacementController
        
        /// <summary>
        /// Validate and reserve funds for a placeable object
        /// </summary>
        public bool ValidateAndReserveFunds(GridPlaceable placeable)
        {
            if (placeable == null) return false;
            if (!_enablePaymentValidation) return true;
            
            var cost = CalculatePlacementCost(placeable, Vector3Int.zero);
            Debug.Log($"[PlacementPaymentService] Validating funds for {placeable.name}: ${cost.TotalCost}");
            
            // In a real implementation, this would check player funds
            return true; // Placeholder - always valid for now
        }
        
        /// <summary>
        /// Complete the purchase for a placed object
        /// </summary>
        public void CompletePurchase(GridPlaceable placeable)
        {
            if (placeable == null) return;
            
            var cost = CalculatePlacementCost(placeable, Vector3Int.zero);
            Debug.Log($"[PlacementPaymentService] Purchase completed for {placeable.name}: ${cost.TotalCost}");
            
            // In a real implementation, this would deduct funds from player
        }
        
        /// <summary>
        /// Update player funds (called externally)
        /// </summary>
        public void UpdatePlayerFunds(float funds)
        {
            Debug.Log($"[PlacementPaymentService] Player funds updated to: ${funds}");
            // Implementation would update internal fund tracking
        }
        
        /// <summary>
        /// Update player resources (called externally)
        /// </summary>
        public void UpdatePlayerResources(Dictionary<string, int> resources)
        {
            Debug.Log($"[PlacementPaymentService] Player resources updated: {resources.Count} resource types");
            // Implementation would update internal resource tracking
        }
        
        #endregion

        private void OnDestroy()
        {
            // Release all active reservations
            foreach (var reservationId in _activeReservations.Keys.ToList())
            {
                ReleaseReservation(reservationId);
            }
        }
    }

    // Extension interface for inventory management
    public interface IInventoryManager
    {
        int GetResourceQuantity(string resourceId);
        bool ReserveResource(string resourceId, int quantity);
        void ReleaseReservedResource(string resourceId, int quantity);
        bool ConsumeResource(string resourceId, int quantity);
        void AddResource(string resourceId, int quantity);
    }
}