using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using ProjectChimera.Core;
using ProjectChimera.Core.DependencyInjection;


namespace ProjectChimera.Testing
{
    /// <summary>
    /// Comprehensive validation test for DIGameManager service registration and discovery.
    /// Tests that the refactored orchestrator maintains full DI functionality.
    /// Validates manager registry, service container integration, and dependency resolution.
    /// </summary>
    public class DIGameManagerValidationTest : MonoBehaviour
    {
        [Header("Validation Configuration")]
        [SerializeField] private bool _runValidationOnStart = false;
        [SerializeField] private bool _enableValidationLogging = true;
        [SerializeField] private bool _validatePerformance = true;
        [SerializeField] private int _performanceTestIterations = 100;
        
        // Validation state
        private List<ValidationResult> _validationResults = new List<ValidationResult>();
        private DIGameManager _gameManager;
        private ProjectChimera.Core.ManagerRegistry _managerRegistry;
        private ServiceHealthMonitor _healthMonitor;
        private bool _validationRunning = false;
        
        private void Start()
        {
            if (_runValidationOnStart)
            {
                StartCoroutine(RunCompleteValidation());
            }
        }
        
        /// <summary>
        /// Run complete validation suite for DIGameManager service functionality
        /// </summary>
        public IEnumerator RunCompleteValidation()
        {
            if (_validationRunning) yield break;
            
            _validationRunning = true;
            _validationResults.Clear();
            
            LogValidation("=== Starting DIGameManager Service Registration & Discovery Validation ===");
            
            // Initialize validation components
            yield return StartCoroutine(InitializeValidationComponents());
            
            // Validation 1: Service container integration
            yield return StartCoroutine(ValidateServiceContainerIntegration());
            
            // Validation 2: Manager registration system
            yield return StartCoroutine(ValidateManagerRegistrationSystem());
            
            // Validation 3: Service discovery mechanisms
            yield return StartCoroutine(ValidateServiceDiscoveryMechanisms());
            
            // Validation 4: Dependency injection functionality
            yield return StartCoroutine(ValidateDependencyInjectionFunctionality());
            
            // Validation 5: Health monitoring integration
            yield return StartCoroutine(ValidateHealthMonitoringIntegration());
            
            // Validation 6: Manager lifecycle management
            yield return StartCoroutine(ValidateManagerLifecycleManagement());
            
            // Validation 7: Error handling and recovery
            yield return StartCoroutine(ValidateErrorHandlingAndRecovery());
            
            // Validation 8: Performance validation
            if (_validatePerformance)
            {
                yield return StartCoroutine(ValidatePerformanceCharacteristics());
            }
            
            // Generate validation summary
            GenerateValidationSummary();
            
            _validationRunning = false;
            LogValidation("=== DIGameManager Service Validation Completed ===");
        }
        
        /// <summary>
        /// Initialize validation components
        /// </summary>
        private IEnumerator InitializeValidationComponents()
        {
            LogValidation("Initializing validation components...");
            
            var result = new ValidationResult { ValidationName = "Component Initialization" };
            
            bool initializationSuccessful = false;
            
            try
            {
                // Find DIGameManager
                _gameManager = DIGameManager.Instance ?? FindObjectOfType<DIGameManager>();
                if (_gameManager == null)
                {
                    result.AddError("DIGameManager not found in scene");
                    _validationResults.Add(result);
                    yield break;
                }
                
                // Get manager registry
                _managerRegistry = _gameManager.ManagerRegistry;
                if (_managerRegistry == null)
                {
                    result.AddError("ManagerRegistry not accessible");
                }
                
                // Get health monitor
                _healthMonitor = _gameManager.HealthMonitor;
                if (_healthMonitor == null)
                {
                    result.AddError("ServiceHealthMonitor not accessible");
                }
                
                initializationSuccessful = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.AddError($"Exception during initialization: {ex.Message}");
            }
            
            // Wait for potential initialization outside try-catch
            if (initializationSuccessful)
            {
                yield return new WaitForSeconds(0.5f);
                
                result.Success = result.Errors.Count == 0;
                LogValidation($"Component initialization: {(result.Success ? "PASSED" : "FAILED")}");
            }
            
            _validationResults.Add(result);
        }
        
        /// <summary>
        /// Validate service container integration
        /// </summary>
        private IEnumerator ValidateServiceContainerIntegration()
        {
            LogValidation("Validating service container integration...");
            
            var result = new ValidationResult { ValidationName = "Service Container Integration" };
            
            try
            {
                // Test global service container access
                var serviceContainer = _gameManager.GlobalServiceContainer;
                if (serviceContainer == null)
                {
                    result.AddError("Global service container not accessible");
                }
                else
                {
                    // Test container verification
                    try
                    {
                        var verification = serviceContainer.Verify();
                        if (!verification.IsValid)
                        {
                            result.AddWarning($"Service container verification failed: {string.Join(", ", verification.Errors)}");
                        }
                        else
                        {
                            LogValidation($"Service container verified: {verification.VerifiedServices} services");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.AddWarning($"Service container verification threw exception: {ex.Message}");
                    }
                    
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
                    
                    var diGameManagerFromContainer = serviceContainer.TryResolve<DIGameManager>();
                    if (diGameManagerFromContainer == null)
                    {
                        result.AddError("DIGameManager not registered in service container");
                    }
                    else if (diGameManagerFromContainer != _gameManager)
                    {
                        result.AddError("Wrong DIGameManager instance returned from container");
                    }
                }
                
                result.Success = result.Errors.Count == 0;
                LogValidation($"Service container integration: {(result.Success ? "PASSED" : "FAILED")}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.AddError($"Exception during service container validation: {ex.Message}");
            }
            
            _validationResults.Add(result);
            yield return new WaitForSeconds(0.1f);
        }
        
        /// <summary>
        /// Validate manager registration system
        /// </summary>
        private IEnumerator ValidateManagerRegistrationSystem()
        {
            LogValidation("Validating manager registration system...");
            
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
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.AddError($"Exception during manager registration: {ex.Message}");
            }
            
            // Wait outside try-catch
            if (registrationSuccessful)
            {
                yield return new WaitForSeconds(0.1f);
            }
            
            try
            {
                
                // Validate registration through GetManager
                var retrievedManager1 = _gameManager.GetManager<TestManager>();
                if (retrievedManager1 == null)
                {
                    result.AddError("Manager registration failed - GetManager returned null");
                }
                else if (retrievedManager1 != testManager1 && retrievedManager1 != testManager2)
                {
                    result.AddError("Manager registration failed - GetManager returned unexpected instance");
                }
                
                // Validate registration through GetAllManagers
                var allManagers = _gameManager.GetAllManagers().ToList();
                bool foundTestManager = allManagers.Any(m => m is TestManager);
                if (!foundTestManager)
                {
                    result.AddError("Registered manager not found in GetAllManagers");
                }
                
                // Test registry access directly
                if (_managerRegistry != null)
                {
                    var registeredCount = _managerRegistry.RegisteredManagerCount;
                    LogValidation($"Manager registry has {registeredCount} registered managers");
                    
                    var isRegistered = _managerRegistry.IsManagerRegistered<TestManager>();
                    if (!isRegistered)
                    {
                        result.AddError("Manager not registered in ManagerRegistry");
                    }
                }
                
                // Test service container registration (if available)
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
                    }
                    catch (Exception ex)
                    {
                        result.AddWarning($"Service container manager resolution failed: {ex.Message}");
                    }
                }
                
                result.Success = result.Errors.Count == 0;
                LogValidation($"Manager registration system: {(result.Success ? "PASSED" : "FAILED")}");
                
                // Cleanup test managers
                if (testManager1 != null) DestroyImmediate(testManager1.gameObject);
                if (testManager2 != null) DestroyImmediate(testManager2.gameObject);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.AddError($"Exception during manager registration validation: {ex.Message}");
            }
            
            _validationResults.Add(result);
            yield return new WaitForSeconds(0.1f);
        }
        
        /// <summary>
        /// Validate service discovery mechanisms
        /// </summary>
        private IEnumerator ValidateServiceDiscoveryMechanisms()
        {
            LogValidation("Validating service discovery mechanisms...");
            
            var result = new ValidationResult { ValidationName = "Service Discovery Mechanisms" };
            
            bool discoverySuccessful = false;
            int initialCount = 0;
            int finalCount = 0;
            
            try
            {
                // Test auto-discovery through ManagerRegistry
                if (_managerRegistry != null)
                {
                    initialCount = _managerRegistry.RegisteredManagerCount;
                    
                    // Trigger auto-discovery
                    _managerRegistry.DiscoverAndRegisterAllManagers();
                    discoverySuccessful = true;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.AddError($"Exception during service discovery validation: {ex.Message}");
            }
            
            // Wait outside try-catch
            if (discoverySuccessful)
            {
                yield return new WaitForSeconds(0.1f);
            }
            
            try
            {
                if (_managerRegistry != null && discoverySuccessful)
                {
                    finalCount = _managerRegistry.RegisteredManagerCount;
                    LogValidation($"Auto-discovery: {initialCount} → {finalCount} managers");
                    
                    // Validate that DIGameManager discovered itself
                    var diGameManagerRegistered = _managerRegistry.IsManagerRegistered(_gameManager);
                    if (!diGameManagerRegistered)
                    {
                        result.AddWarning("DIGameManager not auto-discovered by ManagerRegistry");
                    }
                }
                
                // Test interface-based discovery (if supported)
                if (_managerRegistry != null)
                {
                    try
                    {
                        var gameManagerByInterface = _managerRegistry.GetManagerByInterface<IGameManager>();
                        if (gameManagerByInterface == null)
                        {
                            result.AddWarning("Interface-based manager discovery failed");
                        }
                        else if (gameManagerByInterface != _gameManager)
                        {
                            result.AddError("Interface-based discovery returned wrong instance");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.AddWarning($"Interface-based discovery not supported or failed: {ex.Message}");
                    }
                }
                
                result.Success = result.Errors.Count == 0;
                LogValidation($"Service discovery mechanisms: {(result.Success ? "PASSED" : "FAILED")}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.AddError($"Exception during service discovery validation: {ex.Message}");
            }
            
            _validationResults.Add(result);
            yield return new WaitForSeconds(0.1f);
        }
        
        /// <summary>
        /// Validate dependency injection functionality
        /// </summary>
        private IEnumerator ValidateDependencyInjectionFunctionality()
        {
            LogValidation("Validating dependency injection functionality...");
            
            var result = new ValidationResult { ValidationName = "Dependency Injection Functionality" };
            
            try
            {
                var serviceContainer = _gameManager.GlobalServiceContainer;
                if (serviceContainer == null)
                {
                    result.AddError("Service container not available for DI testing");
                }
                else
                {
                    // Test factory registration (if supported)
                    try
                    {
                        var containerFromFactory = serviceContainer.TryResolve<IChimeraServiceContainer>();
                        if (containerFromFactory == null)
                        {
                            result.AddWarning("Service container factory registration not working");
                        }
                        else if (containerFromFactory != serviceContainer)
                        {
                            result.AddWarning("Factory returned different container instance");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.AddWarning($"Factory resolution failed: {ex.Message}");
                    }
                    
                    // Test singleton pattern enforcement
                    var gameManager1 = serviceContainer.TryResolve<IGameManager>();
                    var gameManager2 = serviceContainer.TryResolve<IGameManager>();
                    
                    if (gameManager1 != gameManager2)
                    {
                        result.AddError("Singleton pattern not enforced - different instances returned");
                    }
                    
                    // Test that registered services can be resolved
                    var resolvedDIGameManager = serviceContainer.TryResolve<DIGameManager>();
                    if (resolvedDIGameManager == null)
                    {
                        result.AddError("DIGameManager cannot be resolved from container");
                    }
                    else if (resolvedDIGameManager != _gameManager)
                    {
                        result.AddError("Wrong DIGameManager instance resolved");
                    }
                }
                
                result.Success = result.Errors.Count == 0;
                LogValidation($"Dependency injection functionality: {(result.Success ? "PASSED" : "FAILED")}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.AddError($"Exception during DI functionality validation: {ex.Message}");
            }
            
            _validationResults.Add(result);
            yield return new WaitForSeconds(0.1f);
        }
        
        /// <summary>
        /// Validate health monitoring integration
        /// </summary>
        private IEnumerator ValidateHealthMonitoringIntegration()
        {
            LogValidation("Validating health monitoring integration...");
            
            var result = new ValidationResult { ValidationName = "Health Monitoring Integration" };
            
            try
            {
                // Test health report generation
                var healthReport = _gameManager.GetServiceHealthReport();
                if (healthReport == null)
                {
                    result.AddError("Health report generation failed");
                }
                else
                {
                    // Validate health report structure
                    if (healthReport.ServiceStatuses == null)
                    {
                        result.AddError("Health report ServiceStatuses is null");
                    }
                    
                    if (healthReport.CriticalErrors == null)
                    {
                        result.AddError("Health report CriticalErrors is null");
                    }
                    
                    LogValidation($"Health report: {healthReport.ServiceStatuses?.Count ?? 0} services monitored");
                    LogValidation($"Health status: {(healthReport.IsHealthy ? "Healthy" : "Issues detected")}");
                    
                    if (!healthReport.IsHealthy)
                    {
                        LogValidation($"Critical errors: {healthReport.CriticalErrors?.Count ?? 0}");
                        LogValidation($"Warnings: {healthReport.Warnings?.Count ?? 0}");
                    }
                }
                
                // Test direct health monitor access
                if (_healthMonitor != null)
                {
                    var isMonitoring = _healthMonitor.IsMonitoring;
                    var trackedCount = _healthMonitor.TrackedServicesCount;
                    
                    LogValidation($"Health monitor: {(isMonitoring ? "Running" : "Stopped")}, tracking {trackedCount} services");
                    
                    // Test health monitor initialization
                    if (trackedCount == 0)
                    {
                        result.AddWarning("Health monitor not tracking any services");
                    }
                }
                else
                {
                    result.AddError("Health monitor not accessible");
                }
                
                result.Success = result.Errors.Count == 0;
                LogValidation($"Health monitoring integration: {(result.Success ? "PASSED" : "FAILED")}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.AddError($"Exception during health monitoring validation: {ex.Message}");
            }
            
            _validationResults.Add(result);
            yield return new WaitForSeconds(0.1f);
        }
        
        /// <summary>
        /// Validate manager lifecycle management
        /// </summary>
        private IEnumerator ValidateManagerLifecycleManagement()
        {
            LogValidation("Validating manager lifecycle management...");
            
            var result = new ValidationResult { ValidationName = "Manager Lifecycle Management" };
            
            bool pauseTestSuccessful = false;
            bool resumeTestSuccessful = false;
            
            try
            {
                // Test game state management
                var initialState = _gameManager.CurrentGameState;
                LogValidation($"Current game state: {initialState}");
                
                // Test pause/resume functionality
                bool wasPaused = _gameManager.IsGamePaused;
                
                _gameManager.PauseGame();
                pauseTestSuccessful = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.AddError($"Exception during pause test: {ex.Message}");
            }
            
            // Wait outside try-catch
            if (pauseTestSuccessful)
            {
                yield return new WaitForSeconds(0.1f);
                
                if (!_gameManager.IsGamePaused)
                {
                    result.AddError("Game pause functionality failed");
                }
                
                try
                {
                    _gameManager.ResumeGame();
                    resumeTestSuccessful = true;
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.AddError($"Exception during resume test: {ex.Message}");
                }
            }
            
            // Wait outside try-catch
            if (resumeTestSuccessful)
            {
                yield return new WaitForSeconds(0.1f);
            }
            
            try
            {
                
                if (_gameManager.IsGamePaused)
                {
                    result.AddError("Game resume functionality failed");
                }
                
                // Test singleton lifecycle
                var instance = DIGameManager.Instance;
                if (instance != _gameManager)
                {
                    result.AddError("Singleton instance not properly maintained");
                }
                
                // Test game time tracking
                var startTime = _gameManager.GameStartTime;
                var totalTime = _gameManager.TotalGameTime;
                
                if (startTime == default(DateTime))
                {
                    result.AddWarning("Game start time not initialized");
                }
                
                if (totalTime.TotalSeconds < 0)
                {
                    result.AddError("Total game time calculation failed");
                }
                
                result.Success = result.Errors.Count == 0;
                LogValidation($"Manager lifecycle management: {(result.Success ? "PASSED" : "FAILED")}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.AddError($"Exception during lifecycle validation: {ex.Message}");
            }
            
            _validationResults.Add(result);
            yield return new WaitForSeconds(0.1f);
        }
        
        /// <summary>
        /// Validate error handling and recovery
        /// </summary>
        private IEnumerator ValidateErrorHandlingAndRecovery()
        {
            LogValidation("Validating error handling and recovery...");
            
            var result = new ValidationResult { ValidationName = "Error Handling and Recovery" };
            
            try
            {
                // Test null manager registration handling
                try
                {
                    _gameManager.RegisterManager<ChimeraManager>(null);
                    // Should not throw exception
                }
                catch (Exception ex)
                {
                    result.AddWarning($"Null manager registration threw exception: {ex.Message}");
                }
                
                // Test invalid manager retrieval
                var nonExistentManager = _gameManager.GetManager<NonExistentManager>();
                if (nonExistentManager != null)
                {
                    result.AddError("GetManager returned non-null for non-existent manager type");
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
                    }
                    catch (Exception ex)
                    {
                        result.AddWarning($"Service container error handling not graceful: {ex.Message}");
                    }
                }
                
                result.Success = result.Errors.Count == 0;
                LogValidation($"Error handling and recovery: {(result.Success ? "PASSED" : "FAILED")}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.AddError($"Exception during error handling validation: {ex.Message}");
            }
            
            _validationResults.Add(result);
            yield return new WaitForSeconds(0.1f);
        }
        
        /// <summary>
        /// Validate performance characteristics
        /// </summary>
        private IEnumerator ValidatePerformanceCharacteristics()
        {
            LogValidation("Validating performance characteristics...");
            
            var result = new ValidationResult { ValidationName = "Performance Characteristics" };
            
            // Test manager registration performance
            var registrationTimes = new List<float>();
            bool registrationTestSuccessful = true;
            
            for (int i = 0; i < _performanceTestIterations; i++)
            {
                try
                {
                    var testManager = CreateTestManager($"PerfTest{i}");
                    
                    var startTime = Time.realtimeSinceStartup;
                    _gameManager.RegisterManager(testManager);
                    var endTime = Time.realtimeSinceStartup;
                    
                    registrationTimes.Add((endTime - startTime) * 1000f); // Convert to milliseconds
                    
                    DestroyImmediate(testManager.gameObject);
                }
                catch (Exception ex)
                {
                    result.AddError($"Exception during registration performance test iteration {i}: {ex.Message}");
                    registrationTestSuccessful = false;
                    break;
                }
                
                // Yield outside try-catch
                if (i % 10 == 0) yield return null; // Yield periodically
            }
            
            try
            {
                if (registrationTestSuccessful && registrationTimes.Count > 0)
                {
                    var avgRegistrationTime = registrationTimes.Average();
                    var maxRegistrationTime = registrationTimes.Max();
                    
                    LogValidation($"Manager registration performance: avg {avgRegistrationTime:F2}ms, max {maxRegistrationTime:F2}ms");
                    
                    if (avgRegistrationTime > 1.0f) // 1ms threshold
                    {
                        result.AddWarning($"Manager registration performance concern: average {avgRegistrationTime:F2}ms");
                    }
                    
                    if (maxRegistrationTime > 5.0f) // 5ms threshold
                    {
                        result.AddWarning($"Manager registration performance spike: max {maxRegistrationTime:F2}ms");
                    }
                }
            }
            catch (Exception ex)
            {
                result.AddError($"Exception during registration performance analysis: {ex.Message}");
            }
            
            // Test manager retrieval performance
            var retrievalTimes = new List<float>();
            var retrievalTestManager = CreateTestManager("RetrievalTest");
            bool retrievalTestSuccessful = true;
            
            try
            {
                _gameManager.RegisterManager(retrievalTestManager);
            }
            catch (Exception ex)
            {
                result.AddError($"Exception during retrieval test setup: {ex.Message}");
                retrievalTestSuccessful = false;
            }
            
            if (retrievalTestSuccessful)
            {
                for (int i = 0; i < _performanceTestIterations; i++)
                {
                    try
                    {
                        var startTime = Time.realtimeSinceStartup;
                        var retrieved = _gameManager.GetManager<TestManager>();
                        var endTime = Time.realtimeSinceStartup;
                        
                        retrievalTimes.Add((endTime - startTime) * 1000f);
                    }
                    catch (Exception ex)
                    {
                        result.AddError($"Exception during retrieval performance test iteration {i}: {ex.Message}");
                        retrievalTestSuccessful = false;
                        break;
                    }
                    
                    // Yield outside try-catch
                    if (i % 10 == 0) yield return null;
                }
            }
            
            try
            {
                if (retrievalTestSuccessful && retrievalTimes.Count > 0)
                {
                    var avgRetrievalTime = retrievalTimes.Average();
                    var maxRetrievalTime = retrievalTimes.Max();
                    
                    LogValidation($"Manager retrieval performance: avg {avgRetrievalTime:F3}ms, max {maxRetrievalTime:F3}ms");
                    
                    if (avgRetrievalTime > 0.1f) // 0.1ms threshold
                    {
                        result.AddWarning($"Manager retrieval performance concern: average {avgRetrievalTime:F3}ms");
                    }
                }
                
                DestroyImmediate(retrievalTestManager.gameObject);
                
                result.Success = result.Errors.Count == 0;
                LogValidation($"Performance characteristics: {(result.Success ? "PASSED" : "FAILED")}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.AddError($"Exception during performance validation: {ex.Message}");
            }
            
            _validationResults.Add(result);
            yield return new WaitForSeconds(0.1f);
        }
        
        /// <summary>
        /// Create a test manager for validation purposes
        /// </summary>
        private TestManager CreateTestManager(string name)
        {
            var testObject = new GameObject(name);
            return testObject.AddComponent<TestManager>();
        }
        
        /// <summary>
        /// Generate and display validation summary
        /// </summary>
        private void GenerateValidationSummary()
        {
            int passed = 0;
            int failed = 0;
            int totalErrors = 0;
            int totalWarnings = 0;
            
            LogValidation("\n=== DIGameManager Service Validation Summary ===");
            
            foreach (var result in _validationResults)
            {
                if (result.Success)
                {
                    passed++;
                    LogValidation($"✓ {result.ValidationName}: PASSED");
                }
                else
                {
                    failed++;
                    LogValidation($"✗ {result.ValidationName}: FAILED");
                    foreach (var error in result.Errors)
                    {
                        LogValidation($"    Error: {error}");
                        totalErrors++;
                    }
                }
                
                foreach (var warning in result.Warnings)
                {
                    LogValidation($"    Warning: {warning}");
                    totalWarnings++;
                }
            }
            
            LogValidation($"\nResults: {passed} passed, {failed} failed");
            LogValidation($"Total Errors: {totalErrors}, Total Warnings: {totalWarnings}");
            
            if (failed == 0)
            {
                LogValidation("🎉 All DIGameManager service registration and discovery validations PASSED!");
            }
            else
            {
                LogValidation($"❌ {failed} validation(s) FAILED - Review errors above");
            }
        }
        
        private void LogValidation(string message)
        {
            if (_enableValidationLogging)
                Debug.Log($"[DIGameManagerValidation] {message}");
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
        private class ValidationResult
        {
            public string ValidationName { get; set; }
            public bool Success { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
            public List<string> Warnings { get; set; } = new List<string>();
            
            public void AddError(string error)
            {
                Errors.Add(error);
            }
            
            public void AddWarning(string warning)
            {
                Warnings.Add(warning);
            }
        }
    }
}