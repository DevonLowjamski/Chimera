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
    /// Performance and error handling tests for DIGameManager.
    /// Tests system performance characteristics, error handling, and recovery mechanisms.
    /// </summary>
    public class DIPerformanceAndErrorTests : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool _runTestsOnStart = false;
        [SerializeField] private bool _enableTestLogging = true;
        [SerializeField] private bool _validatePerformance = true;
        [SerializeField] private int _performanceTestIterations = 100;
        
        [Header("Performance Thresholds")]
        [SerializeField] private float _registrationThresholdMs = 1.0f;
        [SerializeField] private float _registrationSpikeThresholdMs = 5.0f;
        [SerializeField] private float _retrievalThresholdMs = 0.1f;
        [SerializeField] private float _retrievalSpikeThresholdMs = 1.0f;
        
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
                StartCoroutine(RunPerformanceAndErrorTests());
            }
        }
        
        /// <summary>
        /// Run all performance and error handling tests
        /// </summary>
        public IEnumerator RunPerformanceAndErrorTests()
        {
            if (_testsRunning) yield break;
            
            _testsRunning = true;
            _testResults.Clear();
            
            LogTest("=== Starting DIGameManager Performance & Error Tests ===");
            
            // Initialize test components
            yield return StartCoroutine(InitializeTestComponents());
            
            // Test 1: Error handling and recovery
            yield return StartCoroutine(TestErrorHandlingAndRecovery());
            
            // Test 2: Performance characteristics (if enabled)
            if (_validatePerformance)
            {
                yield return StartCoroutine(TestPerformanceCharacteristics());
            }
            
            // Test 3: Stress testing
            yield return StartCoroutine(TestStressScenarios());
            
            // Test 4: Recovery mechanisms
            yield return StartCoroutine(TestRecoveryMechanisms());
            
            // Generate test summary
            GenerateTestSummary();
            
            _testsRunning = false;
            OnTestsCompleted?.Invoke(_testResults);
            LogTest("=== DIGameManager Performance & Error Tests Completed ===");
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
                    result.AddError("DIGameManager not found for performance and error tests");
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
        /// Test error handling and recovery mechanisms
        /// </summary>
        private IEnumerator TestErrorHandlingAndRecovery()
        {
            LogTest("Testing error handling and recovery...");
            
            var result = new ValidationResult { ValidationName = "Error Handling and Recovery" };
            
            try
            {
                // Test null manager registration handling
                try
                {
                    _gameManager.RegisterManager<ChimeraManager>(null);
                    LogTest("Null manager registration handled gracefully");
                }
                catch (ArgumentNullException)
                {
                    LogTest("Null manager registration properly throws ArgumentNullException");
                }
                catch (Exception ex)
                {
                    result.AddWarning($"Null manager registration threw unexpected exception: {ex.GetType().Name} - {ex.Message}");
                }
                
                // Test invalid manager retrieval
                var nonExistentManager = _gameManager.GetManager<NonExistentManager>();
                if (nonExistentManager != null)
                {
                    result.AddError("GetManager returned non-null for non-existent manager type");
                }
                else
                {
                    LogTest("Non-existent manager retrieval correctly returned null");
                }
                
                // Test service container error handling
                var serviceContainer = _gameManager.GlobalServiceContainer;
                if (serviceContainer != null)
                {
                    try
                    {
                        var nonExistentService = serviceContainer.TryResolve<NonExistentService>();
                        if (nonExistentService != null)
                        {
                            result.AddError("Service container returned non-null for non-existent service");
                        }
                        else
                        {
                            LogTest("Non-existent service resolution correctly returned null");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.AddWarning($"Service container error handling not graceful: {ex.Message}");
                    }
                }
                
                // Test multiple registration of same manager type
                var testManager1 = CreateTestManager("ErrorTest1");
                var testManager2 = CreateTestManager("ErrorTest2");
                
                try
                {
                    _gameManager.RegisterManager(testManager1);
                    _gameManager.RegisterManager(testManager2); // Should handle duplicate registration
                    
                    LogTest("Duplicate manager registration handled");
                    
                    // Check which one is retrieved
                    var retrievedManager = _gameManager.GetManager<TestManager>();
                    if (retrievedManager != null)
                    {
                        LogTest($"Retrieved manager after duplicate registration: {retrievedManager.name}");
                    }
                }
                catch (Exception ex)
                {
                    result.AddWarning($"Duplicate registration handling threw exception: {ex.Message}");
                }
                finally
                {
                    if (testManager1 != null) DestroyImmediate(testManager1.gameObject);
                    if (testManager2 != null) DestroyImmediate(testManager2.gameObject);
                }
                
                result.Success = result.Errors.Count == 0;
                LogTest($"Error handling and recovery: {(result.Success ? "PASSED" : "FAILED")}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.AddError($"Exception during error handling test: {ex.Message}");
            }

            // Move yield statement outside try-catch block
            yield return new WaitForSeconds(0.1f);
            
            _testResults.Add(result);
        }
        
        /// <summary>
        /// Test performance characteristics
        /// </summary>
        private IEnumerator TestPerformanceCharacteristics()
        {
            LogTest("Testing performance characteristics...");
            
            var result = new ValidationResult { ValidationName = "Performance Characteristics" };
            
            try
            {
                // Test manager registration performance
                var registrationTimes = new List<float>();
                var testManagers = new List<TestManager>();
                
                LogTest($"Running {_performanceTestIterations} manager registration performance tests...");
                
                for (int i = 0; i < _performanceTestIterations; i++)
                {
                    var testManager = CreateTestManager($"PerfTest{i}");
                    testManagers.Add(testManager);
                    
                    var startTime = Time.realtimeSinceStartup;
                    _gameManager.RegisterManager(testManager);
                    var endTime = Time.realtimeSinceStartup;
                    
                    registrationTimes.Add((endTime - startTime) * 1000f); // Convert to milliseconds
                    
                    // Yield periodically to avoid blocking
                    if (i % 10 == 0) yield return null;
                }
                
                // Analyze registration performance
                if (registrationTimes.Count > 0)
                {
                    var avgRegistrationTime = registrationTimes.Average();
                    var maxRegistrationTime = registrationTimes.Max();
                    var minRegistrationTime = registrationTimes.Min();
                    
                    LogTest($"Manager registration performance:");
                    LogTest($"  Average: {avgRegistrationTime:F3}ms");
                    LogTest($"  Max: {maxRegistrationTime:F3}ms");
                    LogTest($"  Min: {minRegistrationTime:F3}ms");
                    
                    if (avgRegistrationTime > _registrationThresholdMs)
                    {
                        result.AddWarning($"Manager registration performance concern: average {avgRegistrationTime:F3}ms exceeds threshold {_registrationThresholdMs}ms");
                    }
                    
                    if (maxRegistrationTime > _registrationSpikeThresholdMs)
                    {
                        result.AddWarning($"Manager registration performance spike: max {maxRegistrationTime:F3}ms exceeds threshold {_registrationSpikeThresholdMs}ms");
                    }
                }

                // Test manager retrieval performance
                var retrievalTimes = new List<float>();
                
                LogTest($"Running {_performanceTestIterations} manager retrieval performance tests...");
                
                for (int i = 0; i < _performanceTestIterations; i++)
                {
                    var startTime = Time.realtimeSinceStartup;
                    var retrieved = _gameManager.GetManager<TestManager>();
                    var endTime = Time.realtimeSinceStartup;
                    
                    retrievalTimes.Add((endTime - startTime) * 1000f);
                    
                    if (retrieved == null)
                    {
                        result.AddWarning($"Manager retrieval returned null during performance test iteration {i}");
                    }
                    
                    // Yield periodically
                    if (i % 10 == 0) yield return null;
                }
                
                // Analyze retrieval performance
                if (retrievalTimes.Count > 0)
                {
                    var avgRetrievalTime = retrievalTimes.Average();
                    var maxRetrievalTime = retrievalTimes.Max();
                    var minRetrievalTime = retrievalTimes.Min();
                    
                    LogTest($"Manager retrieval performance:");
                    LogTest($"  Average: {avgRetrievalTime:F4}ms");
                    LogTest($"  Max: {maxRetrievalTime:F4}ms");
                    LogTest($"  Min: {minRetrievalTime:F4}ms");
                    
                    if (avgRetrievalTime > _retrievalThresholdMs)
                    {
                        result.AddWarning($"Manager retrieval performance concern: average {avgRetrievalTime:F4}ms exceeds threshold {_retrievalThresholdMs}ms");
                    }
                    
                    if (maxRetrievalTime > _retrievalSpikeThresholdMs)
                    {
                        result.AddWarning($"Manager retrieval performance spike: max {maxRetrievalTime:F4}ms exceeds threshold {_retrievalSpikeThresholdMs}ms");
                    }
                }
                
                // Cleanup test managers
                foreach (var manager in testManagers)
                {
                    if (manager != null) DestroyImmediate(manager.gameObject);
                }

                result.Success = result.Errors.Count == 0;
                LogTest($"Performance characteristics: {(result.Success ? "PASSED" : "FAILED")}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.AddError($"Exception during performance test: {ex.Message}");
            }

            _testResults.Add(result);

            // Move yield statements outside try-catch block
            yield return new WaitForSeconds(0.1f);
            yield return new WaitForSeconds(0.1f);
        }
        
        /// <summary>
        /// Test stress scenarios and system limits
        /// </summary>
        private IEnumerator TestStressScenarios()
        {
            LogTest("Testing stress scenarios...");
            
            var result = new ValidationResult { ValidationName = "Stress Scenarios" };
            
            try
            {
                // Test rapid registration/unregistration cycles
                var stressManagers = new List<TestManager>();
                var cycleCount = Mathf.Min(50, _performanceTestIterations / 2);
                
                LogTest($"Running {cycleCount} rapid registration/unregistration cycles...");
                
                for (int cycle = 0; cycle < cycleCount; cycle++)
                {
                    // Create and register multiple managers quickly
                    for (int i = 0; i < 3; i++)
                    {
                        var manager = CreateTestManager($"Stress{cycle}_{i}");
                        stressManagers.Add(manager);
                        _gameManager.RegisterManager(manager);
                    }
                    
                    // Immediately try to retrieve them
                    var retrieved = _gameManager.GetManager<TestManager>();
                    if (retrieved == null)
                    {
                        result.AddWarning($"Manager retrieval failed during stress test cycle {cycle}");
                    }
                    
                    // Clean up managers
                    foreach (var manager in stressManagers)
                    {
                        if (manager != null) DestroyImmediate(manager.gameObject);
                    }
                    stressManagers.Clear();
                    
                    // Yield every few cycles
                    if (cycle % 5 == 0) yield return null;
                }
                
                LogTest("Rapid registration/unregistration stress test completed");
                
                // Test memory pressure scenario
                var memoryStressManagers = new List<TestManager>();
                try
                {
                    LogTest("Testing memory pressure scenario...");
                    
                    for (int i = 0; i < 100; i++)
                    {
                        var manager = CreateTestManager($"MemStress{i}");
                        memoryStressManagers.Add(manager);
                        _gameManager.RegisterManager(manager);
                        
                        if (i % 20 == 0) yield return null; // Yield periodically
                    }
                    
                    // Test system still responds under memory pressure
                    var healthReport = _gameManager.GetServiceHealthReport();
                    if (healthReport == null)
                    {
                        result.AddWarning("Health reporting failed under memory pressure");
                    }
                    
                    var allManagers = _gameManager.GetAllManagers().ToList();
                    if (allManagers.Count == 0)
                    {
                        result.AddError("GetAllManagers failed under memory pressure");
                    }
                    else
                    {
                        LogTest($"System responsive under memory pressure: {allManagers.Count} managers");
                    }
                }
                finally
                {
                    // Cleanup memory stress managers
                    foreach (var manager in memoryStressManagers)
                    {
                        if (manager != null) DestroyImmediate(manager.gameObject);
                    }
                }
                
                yield return new WaitForSeconds(0.2f);
                
                result.Success = result.Errors.Count == 0;
                LogTest($"Stress scenarios: {(result.Success ? "PASSED" : "FAILED")}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.AddError($"Exception during stress test: {ex.Message}");
            }
            
            _testResults.Add(result);
        }
        
        /// <summary>
        /// Test recovery mechanisms
        /// </summary>
        private IEnumerator TestRecoveryMechanisms()
        {
            LogTest("Testing recovery mechanisms...");
            
            var result = new ValidationResult { ValidationName = "Recovery Mechanisms" };
            
            try
            {
                // Test system state after errors
                var initialState = _gameManager.CurrentGameState;
                var initialHealthReport = _gameManager.GetServiceHealthReport();
                
                // Introduce controlled errors
                try
                {
                    // Attempt operations that might cause issues
                    _gameManager.RegisterManager<ChimeraManager>(null);
                    var _ = _gameManager.GetManager<NonExistentManager>();
                    
                    // Try invalid service container operations
                    var serviceContainer = _gameManager.GlobalServiceContainer;
                    if (serviceContainer != null)
                    {
                        var __ = serviceContainer.TryResolve<NonExistentService>();
                    }
                }
                catch
                {
                    // Expected exceptions - ignore them
                }
                
                yield return new WaitForSeconds(0.1f);
                
                // Test that system recovered
                var postErrorState = _gameManager.CurrentGameState;
                if (postErrorState != initialState)
                {
                    result.AddWarning($"Game state changed after errors: {initialState} → {postErrorState}");
                }
                else
                {
                    LogTest("Game state maintained after error scenarios");
                }
                
                // Test that core functionality still works
                var testManager = CreateTestManager("RecoveryTest");
                try
                {
                    _gameManager.RegisterManager(testManager);
                    var retrieved = _gameManager.GetManager<TestManager>();
                    
                    if (retrieved == null)
                    {
                        result.AddError("Core functionality failed after error scenarios");
                    }
                    else
                    {
                        LogTest("Core functionality recovered after error scenarios");
                    }
                }
                finally
                {
                    if (testManager != null) DestroyImmediate(testManager.gameObject);
                }
                
                // Test health reporting recovery
                var postErrorHealthReport = _gameManager.GetServiceHealthReport();
                if (postErrorHealthReport == null)
                {
                    result.AddError("Health reporting failed to recover after error scenarios");
                }
                else
                {
                    LogTest("Health reporting recovered after error scenarios");
                    
                    // Compare health status
                    if (initialHealthReport != null)
                    {
                        if (initialHealthReport.IsHealthy && !postErrorHealthReport.IsHealthy)
                        {
                            result.AddWarning("System health degraded after error scenarios");
                        }
                        else if (postErrorHealthReport.IsHealthy)
                        {
                            LogTest("System health maintained after error scenarios");
                        }
                    }
                }
                
                yield return new WaitForSeconds(0.1f);
                
                result.Success = result.Errors.Count == 0;
                LogTest($"Recovery mechanisms: {(result.Success ? "PASSED" : "FAILED")}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.AddError($"Exception during recovery test: {ex.Message}");
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
            
            LogTest("\n=== DIGameManager Performance & Error Tests Summary ===");
            
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
                LogTest("🎉 All DIGameManager performance and error tests PASSED!");
            }
            else
            {
                LogTest($"❌ {failed} test(s) FAILED - Review errors above");
            }
        }
        
        private void LogTest(string message)
        {
            if (_enableTestLogging)
                ChimeraLogger.Log($"[DIPerformanceAndErrorTests] {message}");
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
        /// Non-existent manager for error testing
        /// </summary>
        private class NonExistentManager : ChimeraManager
        {
            public override string ManagerName => "NonExistent";
            public override ManagerPriority Priority => ManagerPriority.Low;
            
            protected override void OnManagerInitialize() { }
            protected override void OnManagerShutdown() { }
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