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
    /// Service container and dependency injection tests for DIGameManager.
    /// Tests service registration, resolution, and DI functionality.
    /// </summary>
    public class DIServiceContainerTests : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool _runTestsOnStart = false;
        [SerializeField] private bool _enableTestLogging = true;

        // Test state
        private List<ValidationResult> _testResults = new List<ValidationResult>();
        private DIGameManager _gameManager;
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
                StartCoroutine(RunServiceContainerTests());
            }
        }

        /// <summary>
        /// Run all service container and DI tests
        /// </summary>
        public IEnumerator RunServiceContainerTests()
        {
            if (_testsRunning) yield break;

            _testsRunning = true;
            _testResults.Clear();

            LogTest("=== Starting DIGameManager Service Container Tests ===");

            // Initialize test components
            yield return StartCoroutine(InitializeTestComponents());

            // Test 1: Service container integration
            yield return StartCoroutine(TestServiceContainerIntegration());

            // Test 2: Dependency injection functionality
            yield return StartCoroutine(TestDependencyInjectionFunctionality());

            // Test 3: Service registration and resolution
            yield return StartCoroutine(TestServiceRegistrationAndResolution());

            // Test 4: Container verification
            yield return StartCoroutine(TestContainerVerification());

            // Generate test summary
            GenerateTestSummary();

            _testsRunning = false;
            OnTestsCompleted?.Invoke(_testResults);
            LogTest("=== DIGameManager Service Container Tests Completed ===");
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
                    result.AddError("DIGameManager not found for service container tests");
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
        /// Test service container integration
        /// </summary>
        private IEnumerator TestServiceContainerIntegration()
        {
            LogTest("Testing service container integration...");

            var result = new ValidationResult { ValidationName = "Service Container Integration" };

            try
            {
                // Test global service container access
                var serviceContainer = _gameManager.GlobalServiceContainer;
                if (serviceContainer == null)
                {
                    result.AddError("Global service container not accessible");
                    _testResults.Add(result);
                    yield break;
                }

                LogTest("Global service container accessed successfully");

                // Test core service registrations
                var gameManagerFromContainer = serviceContainer.TryResolve<IGameManager>();
                if (gameManagerFromContainer == null)
                {
                    result.AddError("IGameManager not registered in service container");
                }
                else if (gameManagerFromContainer != _gameManager)
                {
                    result.AddError("Wrong IGameManager instance returned from container");
                }
                else
                {
                    LogTest("IGameManager correctly registered and resolved");
                }

                var diGameManagerFromContainer = serviceContainer.TryResolve<DIGameManager>();
                if (diGameManagerFromContainer == null)
                {
                    result.AddError("DIGameManager not registered in service container");
                }
                else if (diGameManagerFromContainer != _gameManager)
                {
                    result.AddError("Wrong DIGameManager instance returned from container");
                }
                else
                {
                    LogTest("DIGameManager correctly registered and resolved");
                }

                // Test container self-registration
                var containerFromContainer = serviceContainer.TryResolve<IChimeraServiceContainer>();
                if (containerFromContainer == null)
                {
                    result.AddWarning("Service container not self-registered");
                }
                else if (containerFromContainer != serviceContainer)
                {
                    result.AddWarning("Container self-registration returns different instance");
                }
                else
                {
                    LogTest("Service container correctly self-registered");
                }

                result.Success = result.Errors.Count == 0;
                LogTest($"Service container integration: {(result.Success ? "PASSED" : "FAILED")}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.AddError($"Exception during service container integration test: {ex.Message}");
            }

            _testResults.Add(result);

            // Move yield statement outside try-catch block
            yield return new WaitForSeconds(0.1f);
        }

        /// <summary>
        /// Test dependency injection functionality
        /// </summary>
        private IEnumerator TestDependencyInjectionFunctionality()
        {
            LogTest("Testing dependency injection functionality...");

            var result = new ValidationResult { ValidationName = "Dependency Injection Functionality" };

            try
            {
                var serviceContainer = _gameManager.GlobalServiceContainer;
                if (serviceContainer == null)
                {
                    result.AddError("Service container not available for DI testing");
                    _testResults.Add(result);
                    yield break;
                }

                // Test singleton pattern enforcement through DI
                var gameManager1 = serviceContainer.TryResolve<IGameManager>();
                var gameManager2 = serviceContainer.TryResolve<IGameManager>();

                if (gameManager1 != gameManager2)
                {
                    result.AddError("Singleton pattern not enforced through DI - different instances returned");
                }
                else
                {
                    LogTest("Singleton pattern correctly enforced through DI");
                }

                // Test that both interface and concrete type resolve to same instance
                var diGameManager = serviceContainer.TryResolve<DIGameManager>();
                var iGameManager = serviceContainer.TryResolve<IGameManager>();

                if (diGameManager != iGameManager)
                {
                    result.AddError("Interface and concrete type resolve to different instances");
                }
                else
                {
                    LogTest("Interface and concrete type correctly resolve to same instance");
                }

                // Test service factory pattern (if supported)
                try
                {
                    var factoryService = serviceContainer.TryResolve<IServiceContainerFactory>();
                    if (factoryService != null)
                    {
                        LogTest("Service factory pattern supported");
                    }
                    else
                    {
                        result.AddWarning("Service factory pattern not implemented");
                    }
                }
                catch (Exception ex)
                {
                    result.AddWarning($"Service factory test failed: {ex.Message}");
                }

                result.Success = result.Errors.Count == 0;
                LogTest($"Dependency injection functionality: {(result.Success ? "PASSED" : "FAILED")}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.AddError($"Exception during DI functionality test: {ex.Message}");
            }

            // Move yield statement outside try-catch block
            yield return new WaitForSeconds(0.1f);

            _testResults.Add(result);
        }

        /// <summary>
        /// Test service registration and resolution
        /// </summary>
        private IEnumerator TestServiceRegistrationAndResolution()
        {
            LogTest("Testing service registration and resolution...");

            var result = new ValidationResult { ValidationName = "Service Registration and Resolution" };

            try
            {
                var serviceContainer = _gameManager.GlobalServiceContainer;
                if (serviceContainer == null)
                {
                    result.AddError("Service container not available for registration testing");
                    _testResults.Add(result);
                    yield break;
                }

                // Create test service
                var testService = CreateTestService("ServiceResolutionTest");
                bool registrationSuccessful = false;

                try
                {
                    // Test custom service registration (if supported)
                    serviceContainer.RegisterInstance<TestService>(testService);
                    registrationSuccessful = true;
                    LogTest("Test service registered successfully");
                }
                catch (Exception ex)
                {
                    result.AddWarning($"Custom service registration not supported or failed: {ex.Message}");
                }

                if (registrationSuccessful)
                {
                    // Test service resolution
                    var resolvedService = serviceContainer.TryResolve<TestService>();
                    if (resolvedService == null)
                    {
                        result.AddError("Registered test service could not be resolved");
                    }
                    else if (resolvedService != testService)
                    {
                        result.AddError("Resolved test service is not the same instance");
                    }
                    else
                    {
                        LogTest("Test service resolved correctly");
                    }
                }

                // Test resolution of non-existent service
                var nonExistentService = serviceContainer.TryResolve<NonExistentService>();
                if (nonExistentService != null)
                {
                    result.AddError("TryResolve returned non-null for non-existent service");
                }
                else
                {
                    LogTest("Non-existent service correctly returned null");
                }

                // Cleanup test service
                if (testService != null)
                {
                    DestroyImmediate(testService.gameObject);
                }

                result.Success = result.Errors.Count == 0;
                LogTest($"Service registration and resolution: {(result.Success ? "PASSED" : "FAILED")}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.AddError($"Exception during service registration test: {ex.Message}");
            }

            _testResults.Add(result);

            // Move yield statements outside try-catch block
            yield return new WaitForSeconds(0.1f);
            yield return new WaitForSeconds(0.1f);
        }

        /// <summary>
        /// Test container verification
        /// </summary>
        private IEnumerator TestContainerVerification()
        {
            LogTest("Testing container verification...");

            var result = new ValidationResult { ValidationName = "Container Verification" };

            try
            {
                var serviceContainer = _gameManager.GlobalServiceContainer;
                if (serviceContainer == null)
                {
                    result.AddError("Service container not available for verification testing");
                    _testResults.Add(result);
                    yield break;
                }

                // Test container verification
                try
                {
                    var verification = serviceContainer.Verify();
                    if (verification == null)
                    {
                        result.AddWarning("Container verification returned null");
                    }
                    else
                    {
                        LogTest($"Container verification completed: {verification.IsValid}");
                        LogTest($"Verified services: {verification.VerifiedServices}");

                        if (!verification.IsValid)
                        {
                            result.AddWarning($"Container verification failed with errors: {string.Join(", ", verification.Errors)}");

                            foreach (var error in verification.Errors)
                            {
                                LogTest($"Verification error: {error}");
                            }
                        }
                        else
                        {
                            LogTest("Container verification passed");
                        }

                        if (verification.Warnings != null && verification.Warnings.Count > 0)
                        {
                            foreach (var warning in verification.Warnings)
                            {
                                LogTest($"Verification warning: {warning}");
                                result.AddWarning($"Container verification warning: {warning}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.AddWarning($"Container verification threw exception: {ex.Message}");
                }

                result.Success = result.Errors.Count == 0;
                LogTest($"Container verification: {(result.Success ? "PASSED" : "FAILED")}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.AddError($"Exception during container verification test: {ex.Message}");
            }

            _testResults.Add(result);

            // Move yield statement outside try-catch block
            yield return new WaitForSeconds(0.1f);
        }

        /// <summary>
        /// Create a test service for testing purposes
        /// </summary>
        private TestService CreateTestService(string name)
        {
            var testObject = new GameObject(name);
            return testObject.AddComponent<TestService>();
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

            LogTest("\n=== DIGameManager Service Container Tests Summary ===");

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
                LogTest("🎉 All DIGameManager service container tests PASSED!");
            }
            else
            {
                LogTest($"❌ {failed} test(s) FAILED - Review errors above");
            }
        }

        private void LogTest(string message)
        {
            if (_enableTestLogging)
                ChimeraLogger.Log($"[DIServiceContainerTests] {message}");
        }

        /// <summary>
        /// Test service for validation
        /// </summary>
        private class TestService : MonoBehaviour
        {
            public string ServiceName => "TestService";
        }

        /// <summary>
        /// Non-existent service for error testing
        /// </summary>
        private interface NonExistentService { }

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
