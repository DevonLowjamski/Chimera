using ProjectChimera.Core.Logging;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Construction;
using ProjectChimera.Data.Economy;

namespace ProjectChimera.Systems.Economy
{
    /// <summary>
    /// Integration test for the material cost payment system.
    /// Validates the payment flow, cost calculations, and integration with CurrencyManager.
    /// </summary>
    public class MaterialCostPaymentTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private SchematicSO _testSchematic;
        [SerializeField] private float _testFunds = 10000f;
        [SerializeField] private bool _runTestsOnStart = false;
        
        [Header("Test Results")]
        [SerializeField] private bool _testsPassed = false;
        [SerializeField] private string _testResults = "";
        
        private MaterialCostPaymentSystem _paymentSystem;
        private CurrencyManager _currencyManager;
        
        private void Start()
        {
            if (_runTestsOnStart)
            {
                Invoke(nameof(RunPaymentTests), 1f);
            }
        }
        
        [ContextMenu("Run Payment Tests")]
        public void RunPaymentTests()
        {
            var results = new System.Text.StringBuilder();
            bool allTestsPassed = true;
            
            results.AppendLine("=== Material Cost Payment System Tests ===");
            
            // Test 1: System References
            allTestsPassed &= TestSystemReferences(results);
            
            // Test 2: Cost Calculation
            allTestsPassed &= TestCostCalculation(results);
            
            // Test 3: Payment Validation
            allTestsPassed &= TestPaymentValidation(results);
            
            // Test 4: Payment Processing
            allTestsPassed &= TestPaymentProcessing(results);
            
            // Test 5: Insufficient Funds Handling
            allTestsPassed &= TestInsufficientFunds(results);
            
            _testsPassed = allTestsPassed;
            _testResults = results.ToString();
            
            results.AppendLine($"\n=== OVERALL RESULT: {(allTestsPassed ? "PASSED" : "FAILED")} ===");
            ChimeraLogger.Log(results.ToString());
        }
        
        private bool TestSystemReferences(System.Text.StringBuilder results)
        {
            results.AppendLine("\n1. Testing System References:");
            
            _paymentSystem = ServiceContainerFactory.Instance?.TryResolve<IMaterialCostPaymentSystem>() as MaterialCostPaymentSystem;
            _currencyManager = ServiceContainerFactory.Instance?.TryResolve<IEconomyManager>() as CurrencyManager;
            
            bool paymentSystemFound = _paymentSystem != null;
            bool currencyManagerFound = _currencyManager != null;
            
            results.AppendLine($"   - MaterialCostPaymentSystem: {(paymentSystemFound ? "FOUND" : "NOT FOUND")}");
            results.AppendLine($"   - CurrencyManager: {(currencyManagerFound ? "FOUND" : "NOT FOUND")}");
            
            bool passed = paymentSystemFound && currencyManagerFound;
            results.AppendLine($"   Result: {(passed ? "PASS" : "FAIL")}");
            
            return passed;
        }
        
        private bool TestCostCalculation(System.Text.StringBuilder results)
        {
            results.AppendLine("\n2. Testing Cost Calculation:");
            
            if (_paymentSystem == null || _testSchematic == null)
            {
                results.AppendLine("   SKIP - Required components not available");
                return false;
            }
            
            var breakdown = _paymentSystem.CalculateMaterialCosts(_testSchematic);
            
            results.AppendLine($"   Schematic: {_testSchematic.SchematicName}");
            results.AppendLine($"   Base Cost: ${breakdown.TotalBaseCost:F2}");
            results.AppendLine($"   Multiplier: {breakdown.CostMultiplier:F2}");
            results.AppendLine($"   Final Cost: ${breakdown.FinalCost:F2}");
            results.AppendLine($"   Item Count: {breakdown.BaseItemCosts.Count}");
            results.AppendLine($"   Material Categories: {breakdown.MaterialCategories.Count}");
            
            bool passed = breakdown.FinalCost > 0 && 
                         breakdown.SchematicId == _testSchematic.name &&
                         breakdown.BaseItemCosts.Count > 0;
            
            results.AppendLine($"   Result: {(passed ? "PASS" : "FAIL")}");
            return passed;
        }
        
        private bool TestPaymentValidation(System.Text.StringBuilder results)
        {
            results.AppendLine("\n3. Testing Payment Validation:");
            
            if (_paymentSystem == null || _testSchematic == null)
            {
                results.AppendLine("   SKIP - Required components not available");
                return false;
            }
            
            // Set test funds
            if (_currencyManager != null)
            {
                _currencyManager.SetCurrencyForTesting(CurrencyType.Cash, _testFunds);
            }
            
            var validation = _paymentSystem.ValidatePayment(_testSchematic);
            
            results.AppendLine($"   Available Funds: ${validation.AvailableFunds:F2}");
            results.AppendLine($"   Required Cost: ${validation.CostBreakdown.FinalCost:F2}");
            results.AppendLine($"   Can Afford: {validation.CanAfford}");
            results.AppendLine($"   Warning Level: {validation.WarningLevel}");
            results.AppendLine($"   Message: {validation.ValidationMessage}");
            
            bool passed = validation.CostBreakdown != null && 
                         validation.AvailableFunds > 0;
            
            results.AppendLine($"   Result: {(passed ? "PASS" : "FAIL")}");
            return passed;
        }
        
        private bool TestPaymentProcessing(System.Text.StringBuilder results)
        {
            results.AppendLine("\n4. Testing Payment Processing:");
            
            if (_paymentSystem == null || _testSchematic == null || _currencyManager == null)
            {
                results.AppendLine("   SKIP - Required components not available");
                return false;
            }
            
            float initialBalance = _currencyManager.Cash;
            results.AppendLine($"   Initial Balance: ${initialBalance:F2}");
            
            bool paymentSuccess = _paymentSystem.ProcessPayment(_testSchematic, false);
            
            float finalBalance = _currencyManager.Cash;
            float amountSpent = initialBalance - finalBalance;
            
            results.AppendLine($"   Payment Success: {paymentSuccess}");
            results.AppendLine($"   Final Balance: ${finalBalance:F2}");
            results.AppendLine($"   Amount Spent: ${amountSpent:F2}");
            results.AppendLine($"   Transaction Count: {_paymentSystem.TotalTransactions}");
            
            bool passed = paymentSuccess && amountSpent > 0;
            results.AppendLine($"   Result: {(passed ? "PASS" : "FAIL")}");
            
            return passed;
        }
        
        private bool TestInsufficientFunds(System.Text.StringBuilder results)
        {
            results.AppendLine("\n5. Testing Insufficient Funds:");
            
            if (_paymentSystem == null || _testSchematic == null || _currencyManager == null)
            {
                results.AppendLine("   SKIP - Required components not available");
                return false;
            }
            
            // Set very low funds
            _currencyManager.SetCurrencyForTesting(CurrencyType.Cash, 1f);
            
            float lowBalance = _currencyManager.Cash;
            results.AppendLine($"   Low Balance: ${lowBalance:F2}");
            
            bool paymentSuccess = _paymentSystem.ProcessPayment(_testSchematic, false);
            
            float balanceAfter = _currencyManager.Cash;
            
            results.AppendLine($"   Payment Success: {paymentSuccess}");
            results.AppendLine($"   Balance After: ${balanceAfter:F2}");
            results.AppendLine($"   Balance Changed: {balanceAfter != lowBalance}");
            
            // Should fail with insufficient funds
            bool passed = !paymentSuccess && balanceAfter == lowBalance;
            
            results.AppendLine($"   Result: {(passed ? "PASS" : "FAIL")}");
            
            return passed;
        }
        
        [ContextMenu("Set Test Funds")]
        public void SetTestFunds()
        {
            if (_currencyManager == null)
            {
                _currencyManager = ServiceContainerFactory.Instance?.TryResolve<IEconomyManager>() as CurrencyManager;
            }
            
            if (_currencyManager != null)
            {
                _currencyManager.SetCurrencyForTesting(CurrencyType.Cash, _testFunds);
                ChimeraLogger.Log($"Set test funds to ${_testFunds:F2}");
            }
            else
            {
                ChimeraLogger.LogWarning("CurrencyManager not found");
            }
        }
        
        [ContextMenu("Test Single Payment")]
        public void TestSinglePayment()
        {
            if (_paymentSystem == null || _testSchematic == null)
            {
                ChimeraLogger.LogWarning("Required components not available for test");
                return;
            }
            
            var validation = _paymentSystem.ValidatePayment(_testSchematic);
            ChimeraLogger.Log($"Payment validation: {validation.CanAfford} - {validation.ValidationMessage}");
            
            if (validation.CanAfford)
            {
                bool success = _paymentSystem.ProcessPayment(_testSchematic);
                ChimeraLogger.Log($"Payment result: {(success ? "SUCCESS" : "FAILED")}");
            }
        }
    }
}