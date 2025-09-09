using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using ProjectChimera.Core;
using ProjectChimera.Core.DependencyInjection;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Testing
{
    /// <summary>
    /// Manager registration and discovery tests for DIGameManager.
    /// Tests manager registration system, service discovery mechanisms, and registry functionality.
    /// </summary>
    public class DIManagerRegistrationTests : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool _runTestsOnStart = false;
        [SerializeField] private bool _enableTestLogging = true;

        // Test state
        private List<ValidationResult> _testResults = new List<ValidationResult>();
        private DIGameManager _gameManager;
        private ProjectChimera.Core.ManagerRegistry _managerRegistry;
        private bool _testsRunning = false;

        // Events
        public event System.Action<List<ValidationResult>> OnTestsCompleted;

        // Properties
        public List<ValidationResult> TestResults => new List<ValidationResult>(_testResults);
        public bool TestsRunning => _testsRunning;

        private void Start()
        {
            if (_runTestsOnStart)
            {
                StartCoroutine(RunManagerRegistrationTests());
            }
        }

        /// <summary>
        /// Run all manager registration and discovery tests
        /// </summary>
        public IEnumerator RunManagerRegistrationTests()
        {
            if (_testsRunning) yield break;

            _testsRunning = true;
            _testResults.Clear();

            LogTest("=== Starting DIGameManager Registration Tests ===");

            // Initialize test components
            yield return StartCoroutine(InitializeTestComponents());

            // Test 1: Manager registration system
            yield return StartCoroutine(TestManagerRegistrationSystem());

            // Test 2: Service discovery mechanisms
            yield return StartCoroutine(TestServiceDiscoveryMechanisms());

            // Test 3: Manager registry functionality
            yield return StartCoroutine(TestManagerRegistryFunctionality());

            // Test 4: Interface-based discovery
            yield return StartCoroutine(TestInterfaceBasedDiscovery());

            // Generate test summary
            GenerateTestSummary();

            _testsRunning = false;
            OnTestsCompleted?.Invoke(_testResults);
            LogTest("=== DIGameManager Registration Tests Completed ===");
        }

        /// <summary>
        /// Initialize test components
        /// </summary>
        private IEnumerator InitializeTestComponents()
        {
            LogTest("Initializing test components...");

            var result = new ValidationResult { ValidationName = "Test Component Initialization" };

            try
            {
                // Find DIGameManager
                _gameManager = DIGameManager.Instance ?? ServiceContainerFactory.Instance?.TryResolve<DIGameManager>();
                if (_gameManager == null)
                {
                    result.AddError("DIGameManager not found for registration tests");
                    _testResults.Add(result);
                    yield break;
                }

                // Get manager registry
                _managerRegistry = _gameManager.ManagerRegistry;
                if (_managerRegistry == null)
                {
                    result.AddError("ManagerRegistry not accessible for testing");
                    _testResults.Add(result);
                    yield break;
                }

                result.Success = result.Errors.Count == 0;
                LogTest($"Test component initialization: {(result.Success ? "PASSED" : "FAILED")}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.AddError($"Exception during test initialization: {ex.Message}");
            }

            _testResults.Add(result);

            // Move yield statement outside try-catch block
            yield return new WaitForSeconds(0.1f);
        }

        /// <summary>
        /// Test manager registration system
        /// </summary>
        private IEnumerator TestManagerRegistrationSystem()
        {
            LogTest("Testing manager registration system...");

            var result = new ValidationResult { ValidationName = "Manager Registration System" };

            var testManager1 = CreateTestManager("TestManager1");
            var testManager2 = CreateTestManager("TestManager2");
            bool registrationSuccessful = false;

            try
            {
                // Test manager registration through DIGameManager
                _gameManager.RegisterManager(testManager1);
                _gameManager.RegisterManager(testManager2);

                registrationSuccessful = true;
                LogTest("Test managers registered successfully");

                // Validate registration through GetManager
                var retrievedManager = _gameManager.GetManager<TestManager>();
                if (retrievedManager == null)
                {
                    result.AddError("Manager registration failed - GetManager returned null");
                }
                else if (retrievedManager != testManager1 && retrievedManager != testManager2)
                {
                    result.AddError("Manager registration failed - GetManager returned unexpected instance");
                }
                else
                {
                    LogTest("Manager retrieval through GetManager successful");
                }

                // Validate registration through GetAllManagers
                var allManagers = _gameManager.GetAllManagers().ToList();
                bool foundTestManager = allManagers.Any(m => m is TestManager);
                if (!foundTestManager)
                {
                    result.AddError("Registered manager not found in GetAllManagers");
                }
                else
                {
                    var testManagerCount = allManagers.Count(m => m is TestManager);
                    LogTest($"Found {testManagerCount} test managers in GetAllManagers");
                }

                // Test registry access directly
                var registeredCount = _managerRegistry.RegisteredManagerCount;
                LogTest($"Manager registry has {registeredCount} registered managers");

                var isRegistered = _managerRegistry.IsManagerRegistered<TestManager>();
                if (!isRegistered)
                {
                    result.AddError("Manager not registered in ManagerRegistry");
                }
                else
                {
                    LogTest("Manager correctly registered in ManagerRegistry");
                }

                // Test service container registration integration
                var serviceContainer = _gameManager.GlobalServiceContainer;
                if (serviceContainer != null)
                {
                    try
                    {
                        var managerFromContainer = serviceContainer.TryResolve<TestManager>();
                        if (managerFromContainer == null)
                        {
                            result.AddWarning("Registered manager not available through service container");
                        }
                        else
                        {
                            LogTest("Manager available through service container");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.AddWarning($"Service container manager resolution failed: {ex.Message}");
                    }
                }

                result.Success = result.Errors.Count == 0;
                LogTest($"Manager registration system: {(result.Success ? "PASSED" : "FAILED")}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.AddError($"Exception during manager registration test: {ex.Message}");
            }
            finally
            {
                // Cleanup test managers
                if (testManager1 != null) DestroyImmediate(testManager1.gameObject);
                if (testManager2 != null) DestroyImmediate(testManager2.gameObject);
            }

            _testResults.Add(result);

            // Move yield statement outside try-catch block
            yield return new WaitForSeconds(0.1f);
        }

        /// <summary>
        /// Test service discovery mechanisms
        /// </summary>
        private IEnumerator TestServiceDiscoveryMechanisms()
        {
            LogTest("Testing service discovery mechanisms...");

            var result = new ValidationResult { ValidationName = "Service Discovery Mechanisms" };

            try
            {
                // Test auto-discovery through ManagerRegistry
                int initialCount = _managerRegistry.RegisteredManagerCount;
                LogTest($"Initial manager count: {initialCount}");

                // Trigger auto-discovery
                _managerRegistry.DiscoverAndRegisterAllManagers();

                yield return new WaitForSeconds(0.2f);

                int finalCount = _managerRegistry.RegisteredManagerCount;
                LogTest($"Auto-discovery result: {initialCount} → {finalCount} managers");

                if (finalCount < initialCount)
                {
                    result.AddError("Auto-discovery reduced manager count");
                }
                else if (finalCount == initialCount)
                {
                    result.AddWarning("Auto-discovery did not discover any new managers");
                }
                else
                {
                    LogTest($"Auto-discovery found {finalCount - initialCount} additional managers");
                }

                // Validate that DIGameManager discovered itself
                var diGameManagerRegistered = _managerRegistry.IsManagerRegistered(_gameManager);
                if (!diGameManagerRegistered)
                {
                    result.AddError("DIGameManager not auto-discovered by ManagerRegistry");
                }
                else
                {
                    LogTest("DIGameManager correctly auto-discovered");
                }

                // Test discovery of specific manager types
                var discoveredGameManager = _managerRegistry.GetManagerByType(typeof(DIGameManager));
                if (discoveredGameManager == null)
                {
                    result.AddError("DIGameManager not discoverable by type");
                }
                else if (discoveredGameManager != _gameManager)
                {
                    result.AddError("Type-based discovery returned wrong DIGameManager instance");
                }
                else
                {
                    LogTest("Type-based discovery working correctly");
                }

                result.Success = result.Errors.Count == 0;
                LogTest($"Service discovery mechanisms: {(result.Success ? "PASSED" : "FAILED")}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.AddError($"Exception during service discovery test: {ex.Message}");
            }

            _testResults.Add(result);
        }

        /// <summary>
        /// Test manager registry functionality
        /// </summary>
        private IEnumerator TestManagerRegistryFunctionality()
        {
            LogTest("Testing manager registry functionality...");

            var result = new ValidationResult { ValidationName = "Manager Registry Functionality" };

            var testManager = CreateTestManager("RegistryTest");

            try
            {
                // Test direct registry operations
                int initialCount = _managerRegistry.RegisteredManagerCount;

                // Register manager directly through registry
                _managerRegistry.RegisterManager(testManager);

                yield return new WaitForSeconds(0.1f);

                int afterRegistrationCount = _managerRegistry.RegisteredManagerCount;
                if (afterRegistrationCount != initialCount + 1)
                {
                    result.AddError($"Registry manager count not updated correctly: expected {initialCount + 1}, got {afterRegistrationCount}");
                }

                // Test registry query methods
                var isRegistered = _managerRegistry.IsManagerRegistered<TestManager>();
                if (!isRegistered)
                {
                    result.AddError("IsManagerRegistered returned false for registered manager");
                }

                var managerByType = _managerRegistry.GetManagerByType(typeof(TestManager));
                if (managerByType == null)
                {
                    result.AddError("GetManagerByType returned null for registered manager");
                }
                else if (managerByType != testManager)
                {
                    result.AddError("GetManagerByType returned wrong instance");
                }

                var isInstanceRegistered = _managerRegistry.IsManagerRegistered(testManager);
                if (!isInstanceRegistered)
                {
                    result.AddError("IsManagerRegistered(instance) returned false for registered manager");
                }

                // Test registry enumeration
                var allManagers = _managerRegistry.GetAllRegisteredManagers().ToList();
                bool foundTestManager = allManagers.Contains(testManager);
                if (!foundTestManager)
                {
                    result.AddError("Test manager not found in GetAllRegisteredManagers");
                }

                // Test unregistration (if supported)
                try
                {
                    _managerRegistry.UnregisterManager(testManager);

                    yield return new WaitForSeconds(0.1f);

                    var stillRegistered = _managerRegistry.IsManagerRegistered<TestManager>();
                    if (stillRegistered)
                    {
                        result.AddWarning("Manager unregistration not working or not supported");
                    }
                    else
                    {
                        LogTest("Manager unregistration working correctly");
                    }
                }
                catch (Exception ex)
                {
                    result.AddWarning($"Manager unregistration not supported: {ex.Message}");
                }

                result.Success = result.Errors.Count == 0;
                LogTest($"Manager registry functionality: {(result.Success ? "PASSED" : "FAILED")}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.AddError($"Exception during registry functionality test: {ex.Message}");
            }
            finally
            {
                // Cleanup test manager
                if (testManager != null) DestroyImmediate(testManager.gameObject);
            }

            _testResults.Add(result);
        }

        /// <summary>
        /// Test interface-based discovery
        /// </summary>
        private IEnumerator TestInterfaceBasedDiscovery()
        {
            LogTest("Testing interface-based discovery...");

            var result = new ValidationResult { ValidationName = "Interface-Based Discovery" };

            try
            {
                // Test interface-based discovery (if supported)
                try
                {
                    var gameManagerByInterface = _managerRegistry.GetManagerByInterface<IGameManager>();
                    if (gameManagerByInterface == null)
                    {
                        result.AddWarning("Interface-based manager discovery returned null");
                    }
                    else if (gameManagerByInterface != _gameManager)
                    {
                        result.AddError("Interface-based discovery returned wrong instance");
                    }
                    else
                    {
                        LogTest("Interface-based discovery working correctly");
                    }
                }
                catch (Exception ex)
                {
                    result.AddWarning($"Interface-based discovery not supported: {ex.Message}");
                }

                // Test multiple interface implementations (if any exist)
                try
                {
                    var allGameManagers = _managerRegistry.GetAllManagersByInterface(typeof(IGameManager)).ToList();
                    if (allGameManagers.Count == 0)
                    {
                        result.AddWarning("GetAllManagersByInterface returned no results");
                    }
                    else
                    {
                        LogTest($"Found {allGameManagers.Count} managers implementing IGameManager");

                        bool foundOurGameManager = allGameManagers.Contains(_gameManager);
                        if (!foundOurGameManager)
                        {
                            result.AddError("DIGameManager not found in interface-based discovery results");
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.AddWarning($"Multiple interface-based discovery not supported: {ex.Message}");
                }

                yield return new WaitForSeconds(0.1f);

                result.Success = result.Errors.Count == 0;
                LogTest($"Interface-based discovery: {(result.Success ? "PASSED" : "FAILED")}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.AddError($"Exception during interface-based discovery test: {ex.Message}");
            }

            _testResults.Add(result);
        }

        /// <summary>
        /// Create a test manager for testing purposes
        /// </summary>
        private TestManager CreateTestManager(string name)
        {
            var testObject = new GameObject(name);
            return testObject.AddComponent<TestManager>();
        }

        /// <summary>
        /// Generate and display test summary
        /// </summary>
        private void GenerateTestSummary()
        {
            int passed = 0;
            int failed = 0;
            int totalErrors = 0;
            int totalWarnings = 0;

            LogTest("\n=== DIGameManager Registration Tests Summary ===");

            foreach (var result in _testResults)
            {
                if (result.Success)
                {
                    passed++;
                    LogTest($"✓ {result.ValidationName}: PASSED");
                }
                else
                {
                    failed++;
                    LogTest($"✗ {result.ValidationName}: FAILED");
                    foreach (var error in result.Errors)
                    {
                        LogTest($"    Error: {error}");
                        totalErrors++;
                    }
                }

                foreach (var warning in result.Warnings)
                {
                    LogTest($"    Warning: {warning}");
                    totalWarnings++;
                }
            }

            LogTest($"\nResults: {passed} passed, {failed} failed");
            LogTest($"Total Errors: {totalErrors}, Total Warnings: {totalWarnings}");

            if (failed == 0)
            {
                LogTest("🎉 All DIGameManager registration tests PASSED!");
            }
            else
            {
                LogTest($"❌ {failed} test(s) FAILED - Review errors above");
            }
        }

        private void LogTest(string message)
        {
            if (_enableTestLogging)
                ChimeraLogger.Log($"[DIManagerRegistrationTests] {message}");
        }

        /// <summary>
        /// Test manager for validation
        /// </summary>
        private class TestManager : ChimeraManager
        {
            public override string ManagerName => "ValidationTestManager";
            public override ManagerPriority Priority => ManagerPriority.Low;

            protected override void OnManagerInitialize()
            {
                // Test implementation
            }

            protected override void OnManagerShutdown()
            {
                // Test implementation
            }
        }

        /// <summary>
        /// Validation result data structure
        /// </summary>
        public class ValidationResult
        {
            public string ValidationName { get; set; }
            public bool Success { get; set; } = true;
            public List<string> Errors { get; set; } = new List<string>();
            public List<string> Warnings { get; set; } = new List<string>();

            public void AddError(string error)
            {
                Errors.Add(error);
                Success = false;
            }

            public void AddWarning(string warning)
            {
                Warnings.Add(warning);
            }
        }
    }
}
