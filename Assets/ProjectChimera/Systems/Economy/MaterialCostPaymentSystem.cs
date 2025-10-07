using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;
using ProjectChimera.Data.Construction;

namespace ProjectChimera.Systems.Economy
{
    /// <summary>
    /// Material Cost Payment System - Handles material cost calculations for construction
    /// Simple payment processing aligned with Project Chimera's vision
    /// </summary>
    public class MaterialCostPaymentSystem : MonoBehaviour
    {
        [Header("Payment Settings")]
        [SerializeField] private bool _enableLogging = true;

        private ICurrencyManager _currencyManager;

        private void Awake()
        {
            InitializePaymentSystem();
        }

        private void InitializePaymentSystem()
        {
            // Primary: Resolve from ServiceContainer
            _currencyManager = ServiceContainerFactory.Instance?.TryResolve<ICurrencyManager>();

            if (_currencyManager == null)
            {
                // Fallback: Try to resolve SimpleEconomyManager specifically
                var economyManager = ServiceContainerFactory.Instance?.TryResolve<SimpleEconomyManager>();
                if (economyManager != null)
                {
                    _currencyManager = economyManager;
                }
            }

            if (_currencyManager == null)
            {
                ChimeraLogger.LogWarning("ECONOMY", "No currency manager found for MaterialCostPaymentSystem - register ICurrencyManager in ServiceContainer", this);
            }

            if (_enableLogging)
                ChimeraLogger.LogInfo("ECONOMY", "MaterialCostPaymentSystem initialized", this);
        }

        /// <summary>
        /// Process payment for construction materials
        /// </summary>
        public bool ProcessMaterialPayment(GridConstructionTemplate template)
        {
            if (_currencyManager == null || template == null)
                return false;

            float totalCost = template.GetTotalCost();
            int cost = Mathf.RoundToInt(totalCost);

            if (_currencyManager.CanAfford(cost))
            {
                bool success = _currencyManager.SpendCash(cost);

                if (success && _enableLogging)
                {
                    ChimeraLogger.LogInfo("ECONOMY", $"Processed material payment: ${cost} for {template.TemplateName}", this);
                }

                return success;
            }

            if (_enableLogging)
                ChimeraLogger.LogWarning("ECONOMY", $"Insufficient funds for {template.TemplateName}. Required: ${cost}, Available: ${_currencyManager.GetCurrentCurrency()}", this);

            return false;
        }

        /// <summary>
        /// Check if player can afford materials
        /// </summary>
        public bool CanAffordMaterials(GridConstructionTemplate template)
        {
            if (_currencyManager == null || template == null)
                return false;

            float totalCost = template.GetTotalCost();
            int cost = Mathf.RoundToInt(totalCost);

            return _currencyManager.CanAfford(cost);
        }

        /// <summary>
        /// Get material cost for template
        /// </summary>
        public int GetMaterialCost(GridConstructionTemplate template)
        {
            if (template == null)
                return 0;

            return Mathf.RoundToInt(template.GetTotalCost());
        }
    }
}