using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using ProjectChimera.Core;
using ProjectChimera.Core.DependencyInjection;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Testing
{
    /// <summary>
    /// Health monitoring and lifecycle management tests for DIGameManager.
    /// Tests health reporting, monitoring integration, and lifecycle management.
    /// </summary>
    public class DIHealthMonitoringTests : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool _runTestsOnStart = false;
        [SerializeField] private bool _enableTestLogging = true;

        // Test state
        private List<ValidationResult> _testResults = new List<ValidationResult>();
        private DIGameManager _gameManager;
        private ServiceHealthMonitor _healthMonitor;
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
                StartCoroutine(RunHealthMonitoringTests());
            }
        }

        /// <summary>
        /// Run all health monitoring and lifecycle tests
        /// </summary>
        public IEnumerator RunHealthMonitoringTests()
        {
            if (_testsRunning) yield break;

            _testsRunning = true;
            _testResults.Clear();

            LogTest("=== Starting DIGameManager Health Monitoring Tests ===");

            // Initialize test components
            yield return StartCoroutine(InitializeTestComponents());

            // Test 1: Health monitoring integration
            yield return StartCoroutine(TestHealthMonitoringIntegration());

            // Test 2: Health report generation
            yield return StartCoroutine(TestHealthReportGeneration());

            // Test 3: Manager lifecycle management
            yield return StartCoroutine(TestManagerLifecycleManagement());

            // Test 4: Service monitoring
            yield return StartCoroutine(TestServiceMonitoring());

            // Generate test summary
            GenerateTestSummary();

            _testsRunning = false;
            OnTestsCompleted?.Invoke(_testResults);
            LogTest("=== DIGameManager Health Monitoring Tests Completed ===");
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
                    result.AddError("DIGameManager not found for health monitoring tests");
                    _testResults.Add(result);
                    yield break;
                }

                // Get health monitor
                _healthMonitor = _gameManager.HealthMonitor;
                if (_healthMonitor == null)
                {
                    result.AddError("ServiceHealthMonitor not accessible for testing");
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
        /// Test health monitoring integration
        /// </summary>
        private IEnumerator TestHealthMonitoringIntegration()
        {
            LogTest("Testing health monitoring integration...");

            var result = new ValidationResult { ValidationName = "Health Monitoring Integration" };

            try
            {
                // Test direct health monitor access and status
                var isMonitoring = _healthMonitor.IsMonitoring;
                var trackedCount = _healthMonitor.TrackedServicesCount;

                LogTest($"Health monitor status: {(isMonitoring ? "Running" : "Stopped")}");
                LogTest($"Tracking {trackedCount} services");

                if (!isMonitoring)
                {
                    result.AddWarning("Health monitor is not currently monitoring");
                }

                if (trackedCount == 0)
                {
                    result.AddWarning("Health monitor not tracking any services");
                }
                else
                {
                    LogTest("Health monitor is tracking services correctly");
                }

                // Test health monitor functionality
                try
                {
                    // Start monitoring if not already started
                    if (!isMonitoring)
                    {
                        _healthMonitor.StartMonitoring();

                        if (_healthMonitor.IsMonitoring)
                        {
                            LogTest("Successfully started health monitoring");
                        }
                        else
                        {
                            result.AddWarning("Failed to start health monitoring");
                        }
                    }

                    // Test service registration with monitor
                    var testService = CreateTestService("HealthMonitorTest");
                    try
                    {
                        _healthMonitor.RegisterService(testService.GetInstanceID().ToString(), testService);
                        LogTest("Test service registered with health monitor");

                        var newTrackedCount = _healthMonitor.TrackedServicesCount;
                        if (newTrackedCount > trackedCount)
                        {
                            LogTest($"Service tracking count increased: {trackedCount} → {newTrackedCount}");
                        }
                        else
                        {
                            result.AddWarning("Service registration did not increase tracked count");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.AddWarning($"Service registration with monitor failed: {ex.Message}");
                    }
                    finally
                    {
                        if (testService != null) DestroyImmediate(testService.gameObject);
                    }
                }
                catch (Exception ex)
                {
                    result.AddError($"Health monitor functionality test failed: {ex.Message}");
                }

                result.Success = result.Errors.Count == 0;
                LogTest($"Health monitoring integration: {(result.Success ? "PASSED" : "FAILED")}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.AddError($"Exception during health monitoring integration test: {ex.Message}");
            }

            _testResults.Add(result);

            // Move yield statements outside try-catch blocks
            yield return new WaitForSeconds(0.2f);
            yield return new WaitForSeconds(0.1f);
        }

        /// <summary>
        /// Test health report generation
        /// </summary>
        private IEnumerator TestHealthReportGeneration()
        {
            LogTest("Testing health report generation...");

            var result = new ValidationResult { ValidationName = "Health Report Generation" };

            try
            {
                // Test health report generation through DIGameManager
                var healthReport = _gameManager.GetServiceHealthReport();
                if (healthReport == null)
                {
                    result.AddError("GetServiceHealthReport returned null");
                    _testResults.Add(result);
                    yield break;
                }

                LogTest("Health report generated successfully");

                // Validate health report structure
                if (healthReport.ServiceStatuses == null)
                {
                    result.AddError("Health report ServiceStatuses is null");
                }
                else
                {
                    LogTest($"Health report contains {healthReport.ServiceStatuses.Count} service statuses");
                }

                if (healthReport.CriticalErrors == null)
                {
                    result.AddError("Health report CriticalErrors is null");
                }
                else
                {
                    LogTest($"Health report contains {healthReport.CriticalErrors.Count} critical errors");
                }

                if (healthReport.Warnings == null)
                {
                    result.AddError("Health report Warnings is null");
                }
                else
                {
                    LogTest($"Health report contains {healthReport.Warnings.Count} warnings");
                }

                // Validate health status
                LogTest($"Overall health status: {(healthReport.IsHealthy ? "Healthy" : "Issues detected")}");

                if (!healthReport.IsHealthy)
                {
                    LogTest("Health issues detected:");
                    if (healthReport.CriticalErrors != null)
                    {
                        foreach (var error in healthReport.CriticalErrors)
                        {
                            LogTest($"  Critical: {error}");
                        }
                    }
                    if (healthReport.Warnings != null)
                    {
                        foreach (var warning in healthReport.Warnings)
                        {
                            LogTest($"  Warning: {warning}");
                        }
                    }
                }

                // Test report timing and freshness
                var reportTime = healthReport.Timestamp;
                var timeDiff = DateTime.Now - reportTime;

                if (timeDiff.TotalMinutes > 1)
                {
                    result.AddWarning($"Health report timestamp seems old: {timeDiff.TotalMinutes:F1} minutes");
                }
                else
                {
                    LogTest($"Health report timestamp is fresh: {timeDiff.TotalSeconds:F1} seconds old");
                }

                yield return new WaitForSeconds(0.1f);

                result.Success = result.Errors.Count == 0;
                LogTest($"Health report generation: {(result.Success ? "PASSED" : "FAILED")}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.AddError($"Exception during health report test: {ex.Message}");
            }

            _testResults.Add(result);
        }

        /// <summary>
        /// Test manager lifecycle management
        /// </summary>
        private IEnumerator TestManagerLifecycleManagement()
        {
            LogTest("Testing manager lifecycle management...");

            var result = new ValidationResult { ValidationName = "Manager Lifecycle Management" };

            bool pauseTestSuccessful = false;
            bool resumeTestSuccessful = false;

            try
            {
                // Test game state management
                var initialState = _gameManager.CurrentGameState;
                LogTest($"Initial game state: {initialState}");

                // Test pause/resume functionality
                bool wasPaused = _gameManager.IsGamePaused;
                LogTest($"Initial pause state: {wasPaused}");

                // Test pause functionality
                _gameManager.PauseGame();
                pauseTestSuccessful = true;

                yield return new WaitForSeconds(0.1f);

                if (!_gameManager.IsGamePaused)
                {
                    result.AddError("Game pause functionality failed - IsGamePaused still false");
                }
                else
                {
                    LogTest("Game pause successful");
                }

                // Test resume functionality
                _gameManager.ResumeGame();
                resumeTestSuccessful = true;

                yield return new WaitForSeconds(0.1f);

                if (_gameManager.IsGamePaused)
                {
                    result.AddError("Game resume functionality failed - IsGamePaused still true");
                }
                else
                {
                    LogTest("Game resume successful");
                }

                // Test singleton lifecycle persistence
                var instance = DIGameManager.Instance;
                if (instance != _gameManager)
                {
                    result.AddError("Singleton instance not properly maintained during lifecycle operations");
                }
                else
                {
                    LogTest("Singleton instance maintained correctly");
                }

                // Test time tracking during lifecycle operations
                var gameStartTime = _gameManager.GameStartTime;
                var totalTime = _gameManager.TotalGameTime;

                LogTest($"Game start time: {gameStartTime}");
                LogTest($"Total game time: {totalTime.TotalSeconds:F2} seconds");

                if (gameStartTime == default(DateTime))
                {
                    result.AddWarning("Game start time not initialized");
                }

                if (totalTime.TotalSeconds < 0)
                {
                    result.AddError("Total game time calculation failed");
                }

                // Test lifecycle event integration with health monitoring
                if (_healthMonitor.IsMonitoring)
                {
                    var healthAfterLifecycle = _gameManager.GetServiceHealthReport();
                    if (healthAfterLifecycle == null)
                    {
                        result.AddError("Health reporting failed after lifecycle operations");
                    }
                    else if (!healthAfterLifecycle.IsHealthy)
                    {
                        result.AddWarning("System health degraded after lifecycle operations");
                    }
                    else
                    {
                        LogTest("System health maintained after lifecycle operations");
                    }
                }

                result.Success = result.Errors.Count == 0;
                LogTest($"Manager lifecycle management: {(result.Success ? "PASSED" : "FAILED")}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.AddError($"Exception during lifecycle management test: {ex.Message}");

                // Try to restore normal state on exception
                if (pauseTestSuccessful && !resumeTestSuccessful)
                {
                    try
                    {
                        _gameManager.ResumeGame();
                        LogTest("Restored game state after exception");
                    }
                    catch
                    {
                        LogTest("Failed to restore game state after exception");
                    }
                }
            }

            _testResults.Add(result);
        }

        /// <summary>
        /// Test service monitoring functionality
        /// </summary>
        private IEnumerator TestServiceMonitoring()
        {
            LogTest("Testing service monitoring functionality...");

            var result = new ValidationResult { ValidationName = "Service Monitoring Functionality" };

            try
            {
                // Test service health checks
                var initialTrackedCount = _healthMonitor.TrackedServicesCount;

                // Create and register a test service for monitoring
                var testService = CreateTestService("ServiceMonitoringTest");
                string serviceId = $"test_service_{testService.GetInstanceID()}";

                try
                {
                    // Register service for monitoring
                    _healthMonitor.RegisterService(serviceId, testService);

                    yield return new WaitForSeconds(0.1f);

                    var newTrackedCount = _healthMonitor.TrackedServicesCount;
                    if (newTrackedCount <= initialTrackedCount)
                    {
                        result.AddWarning("Service registration may not have increased tracked count");
                    }
                    else
                    {
                        LogTest($"Service registered for monitoring: {initialTrackedCount} → {newTrackedCount}");
                    }

                    // Test health check for specific service
                    try
                    {
                        var serviceHealth = _healthMonitor.GetServiceHealth(serviceId);
                        if (serviceHealth == null)
                        {
                            result.AddWarning("Individual service health check returned null");
                        }
                        else
                        {
                            LogTest($"Service health status: {serviceHealth.IsHealthy}");
                            if (serviceHealth.LastCheckTime == default(DateTime))
                            {
                                result.AddWarning("Service health check time not set");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result.AddWarning($"Individual service health check failed: {ex.Message}");
                    }

                    // Test service unregistration
                    try
                    {
                        _healthMonitor.UnregisterService(serviceId);

                        yield return new WaitForSeconds(0.1f);

                        var finalTrackedCount = _healthMonitor.TrackedServicesCount;
                        if (finalTrackedCount >= newTrackedCount)
                        {
                            result.AddWarning("Service unregistration may not have reduced tracked count");
                        }
                        else
                        {
                            LogTest($"Service unregistered from monitoring: {newTrackedCount} → {finalTrackedCount}");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.AddWarning($"Service unregistration failed: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    result.AddWarning($"Service registration for monitoring failed: {ex.Message}");
                }
                finally
                {
                    if (testService != null) DestroyImmediate(testService.gameObject);
                }

                // Test monitoring state persistence
                var isStillMonitoring = _healthMonitor.IsMonitoring;
                if (!isStillMonitoring)
                {
                    result.AddWarning("Health monitor stopped monitoring during service tests");
                }

                yield return new WaitForSeconds(0.1f);

                result.Success = result.Errors.Count == 0;
                LogTest($"Service monitoring functionality: {(result.Success ? "PASSED" : "FAILED")}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.AddError($"Exception during service monitoring test: {ex.Message}");
            }

            _testResults.Add(result);
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

            LogTest("\n=== DIGameManager Health Monitoring Tests Summary ===");

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
                LogTest("🎉 All DIGameManager health monitoring tests PASSED!");
            }
            else
            {
                LogTest($"❌ {failed} test(s) FAILED - Review errors above");
            }
        }

        private void LogTest(string message)
        {
            if (_enableTestLogging)
                ChimeraLogger.Log($"[DIHealthMonitoringTests] {message}");
        }

        /// <summary>
        /// Test service for monitoring validation
        /// </summary>
        private class TestService : MonoBehaviour
        {
            public string ServiceName => "TestMonitoringService";
            public bool IsHealthy => true;
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
