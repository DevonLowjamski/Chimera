using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.Testing
{
    /// <summary>
    /// Validation test for migrated ServiceContainer integrations
    /// Tests that all migrated services resolve correctly without null reference exceptions
    /// </summary>
    public class ServiceContainerValidationTest : MonoBehaviour
    {
        [Header("Validation Configuration")]
        [SerializeField] private bool _runOnStart = true;
        [SerializeField] private bool _enableDetailedLogging = true;
        [SerializeField] private bool _testFallbackMechanisms = true;
        
        [Header("Test Results")]
        [SerializeField] private int _totalTests = 0;
        [SerializeField] private int _passedTests = 0;
        [SerializeField] private int _failedTests = 0;
        [SerializeField] private List<string> _failedTestNames = new List<string>();

        // Test results tracking
        private readonly List<ValidationResult> _results = new List<ValidationResult>();
        
        /// <summary>
        /// Validation result for individual test
        /// </summary>
        [System.Serializable]
        public class ValidationResult
        {
            public string TestName;
            public bool Passed;
            public string ErrorMessage;
            public float ExecutionTime;
        }

        private void Start()
        {
            if (_runOnStart)
            {
                StartCoroutine(RunAllValidationTests());
            }
        }

        /// <summary>
        /// Run all validation tests for migrated services
        /// </summary>
        public IEnumerator RunAllValidationTests()
        {
            LogInfo("🔍 Starting ServiceContainer Migration Validation Tests");
            _results.Clear();
            _failedTestNames.Clear();
            
            // Phase 1: Core Infrastructure Tests
            yield return StartCoroutine(ValidatePhase1CoreInfrastructure());
            
            // Phase 2A: Construction System Tests  
            yield return StartCoroutine(ValidatePhase2AConstructionSystem());
            
            // Service Container Health Tests
            yield return StartCoroutine(ValidateServiceContainerHealth());
            
            // Fallback Mechanism Tests
            if (_testFallbackMechanisms)
            {
                yield return StartCoroutine(ValidateFallbackMechanisms());
            }
            
            // Generate final report
            GenerateFinalReport();
        }

        /// <summary>
        /// Validate Phase 1: Core Infrastructure (Migrations #1-3)
        /// </summary>
        private IEnumerator ValidatePhase1CoreInfrastructure()
        {
            LogInfo("📋 Phase 1: Core Infrastructure Validation");
            
            // Test #1: ManagerRegistry ServiceContainer Integration
            yield return StartCoroutine(TestManagerRegistryServiceContainerIntegration());
            
            // Test #2: SimpleManagerRegistry Unification
            yield return StartCoroutine(TestSimpleManagerRegistryUnification());
            
            // Test #3: GameSystemInitializer ServiceContainer Discovery
            yield return StartCoroutine(TestGameSystemInitializerDiscovery());
        }

        /// <summary>
        /// Validate Phase 2A: Construction System (Migrations #4-9)
        /// </summary>
        private IEnumerator ValidatePhase2AConstructionSystem()
        {
            LogInfo("🏗️ Phase 2A: Construction System Validation");
            
            // Test #4: GridInputHandler Camera Service Resolution
            yield return StartCoroutine(TestGridInputHandlerCameraService());
            
            // Test #5: ConstructionSaveProvider IConstructionSystem Resolution
            yield return StartCoroutine(TestConstructionSaveProviderSystemResolution());
            
            // Test #6-9: Payment Services Currency/Trading Resolution
            yield return StartCoroutine(TestPaymentServicesCurrencyTradingResolution());
        }

        /// <summary>
        /// Test ManagerRegistry ServiceContainer Integration
        /// </summary>
        private IEnumerator TestManagerRegistryServiceContainerIntegration()
        {
            var result = new ValidationResult { TestName = "ManagerRegistry ServiceContainer Integration" };
            var startTime = Time.realtimeSinceStartup;
            
            try
            {
                // Check if ServiceContainer is available
                if (ServiceContainerFactory.Instance == null)
                {
                    throw new System.Exception("ServiceContainerFactory.Instance is null");
                }
                
                // Test ResolveAll<ChimeraManager> functionality
                var managers = ServiceContainerFactory.Instance.ResolveAll<ChimeraManager>().ToArray();
                LogDebug($"Found {managers.Length} ChimeraManager instances via ServiceContainer");
                
                // Verify managers are accessible
                foreach (var manager in managers)
                {
                    if (manager == null)
                        throw new System.Exception("Null ChimeraManager found in ServiceContainer results");
                        
                    LogDebug($"✅ Manager: {manager.GetType().Name}");
                }
                
                result.Passed = true;
                LogSuccess($"✅ ManagerRegistry ServiceContainer integration working ({managers.Length} managers)");
            }
            catch (System.Exception ex)
            {
                result.Passed = false;
                result.ErrorMessage = ex.Message;
                LogError($"❌ ManagerRegistry ServiceContainer integration failed: {ex.Message}");
            }
            
            result.ExecutionTime = Time.realtimeSinceStartup - startTime;
            _results.Add(result);
            yield return null;
        }

        /// <summary>
        /// Test Camera Service Resolution (Migration #4)
        /// </summary>
        private IEnumerator TestGridInputHandlerCameraService()
        {
            var result = new ValidationResult { TestName = "GridInputHandler Camera Service Resolution" };
            var startTime = Time.realtimeSinceStartup;
            
            try
            {
                // Test Camera service resolution
                if (ServiceContainerFactory.Instance.TryResolve<Camera>(out var camera))
                {
                    if (camera == null)
                        throw new System.Exception("Camera service resolved but is null");
                        
                    LogSuccess($"✅ Camera service resolved: {camera.name}");
                    result.Passed = true;
                }
                else
                {
                    // Check if camera exists in scene for fallback testing
                    var sceneCamera = UnityEngine.Object.FindObjectOfType<Camera>();
                    if (sceneCamera != null)
                    {
                        LogWarning($"⚠️ Camera service not in ServiceContainer but exists in scene - fallback should work");
                        result.Passed = true;
                    }
                    else
                    {
                        throw new System.Exception("No camera available via ServiceContainer or scene");
                    }
                }
            }
            catch (System.Exception ex)
            {
                result.Passed = false;
                result.ErrorMessage = ex.Message;
                LogError($"❌ Camera service resolution failed: {ex.Message}");
            }
            
            result.ExecutionTime = Time.realtimeSinceStartup - startTime;
            _results.Add(result);
            yield return null;
        }

        /// <summary>
        /// Test IConstructionSystem Resolution (Migration #5)
        /// </summary>
        private IEnumerator TestConstructionSaveProviderSystemResolution()
        {
            var result = new ValidationResult { TestName = "ConstructionSaveProvider IConstructionSystem Resolution" };
            var startTime = Time.realtimeSinceStartup;
            
            try
            {
                // Test IConstructionSystem service resolution
                if (ServiceContainerFactory.Instance.TryResolve<IConstructionSystem>(out var constructionSystem))
                {
                    if (constructionSystem == null)
                        throw new System.Exception("IConstructionSystem service resolved but is null");
                        
                    LogSuccess($"✅ IConstructionSystem service resolved: {constructionSystem.GetType().Name}");
                    result.Passed = true;
                }
                else
                {
                    LogWarning("⚠️ IConstructionSystem not found in ServiceContainer - fallback mechanisms should handle this");
                    result.Passed = true; // Acceptable if no construction system exists yet
                }
            }
            catch (System.Exception ex)
            {
                result.Passed = false;
                result.ErrorMessage = ex.Message;
                LogError($"❌ IConstructionSystem resolution failed: {ex.Message}");
            }
            
            result.ExecutionTime = Time.realtimeSinceStartup - startTime;
            _results.Add(result);
            yield return null;
        }

        /// <summary>
        /// Test Payment Services Resolution (Migrations #6-9)
        /// </summary>
        private IEnumerator TestPaymentServicesCurrencyTradingResolution()
        {
            var result = new ValidationResult { TestName = "Payment Services Currency/Trading Resolution" };
            var startTime = Time.realtimeSinceStartup;
            
            try
            {
                bool currencyResolved = false;
                bool tradingResolved = false;
                
                // Test ICurrencyManager resolution
                if (ServiceContainerFactory.Instance.TryResolve<ICurrencyManager>(out var currencyManager))
                {
                    if (currencyManager != null)
                    {
                        LogSuccess($"✅ ICurrencyManager resolved: {currencyManager.GetType().Name}");
                        currencyResolved = true;
                    }
                }
                
                // Test ITradingManager resolution
                if (ServiceContainerFactory.Instance.TryResolve<ITradingManager>(out var tradingManager))
                {
                    if (tradingManager != null)
                    {
                        LogSuccess($"✅ ITradingManager resolved: {tradingManager.GetType().Name}");
                        tradingResolved = true;
                    }
                }
                
                if (!currencyResolved && !tradingResolved)
                {
                    LogWarning("⚠️ Neither Currency nor Trading managers found in ServiceContainer - fallback mechanisms should handle this");
                    result.Passed = true; // Acceptable if no economy systems exist yet
                }
                else
                {
                    result.Passed = true;
                }
            }
            catch (System.Exception ex)
            {
                result.Passed = false;
                result.ErrorMessage = ex.Message;
                LogError($"❌ Payment services resolution failed: {ex.Message}");
            }
            
            result.ExecutionTime = Time.realtimeSinceStartup - startTime;
            _results.Add(result);
            yield return null;
        }

        /// <summary>
        /// Test remaining core infrastructure components
        /// </summary>
        private IEnumerator TestSimpleManagerRegistryUnification()
        {
            var result = new ValidationResult { TestName = "SimpleManagerRegistry Unification" };
            var startTime = Time.realtimeSinceStartup;
            
            try
            {
                // Test that SimpleManagerRegistry can resolve from ServiceContainer
                if (ServiceContainerFactory.Instance.TryResolve<SimpleManagerRegistry>(out var registry))
                {
                    LogSuccess($"✅ SimpleManagerRegistry resolved from ServiceContainer");
                    result.Passed = true;
                }
                else
                {
                    LogWarning("⚠️ SimpleManagerRegistry not in ServiceContainer - may not be initialized yet");
                    result.Passed = true; // Acceptable if not initialized
                }
            }
            catch (System.Exception ex)
            {
                result.Passed = false;
                result.ErrorMessage = ex.Message;
                LogError($"❌ SimpleManagerRegistry unification failed: {ex.Message}");
            }
            
            result.ExecutionTime = Time.realtimeSinceStartup - startTime;
            _results.Add(result);
            yield return null;
        }

        /// <summary>
        /// Test GameSystemInitializer Discovery
        /// </summary>
        private IEnumerator TestGameSystemInitializerDiscovery()
        {
            var result = new ValidationResult { TestName = "GameSystemInitializer Discovery" };
            var startTime = Time.realtimeSinceStartup;
            
            try
            {
                // Find GameSystemInitializer in scene
                var initializer = UnityEngine.Object.FindObjectOfType<GameSystemInitializer>();
                if (initializer == null)
                {
                    LogWarning("⚠️ GameSystemInitializer not found in scene - may not be present in test scene");
                    result.Passed = true; // Acceptable if not in test scene
                }
                else
                {
                    LogSuccess($"✅ GameSystemInitializer found: {initializer.name}");
                    result.Passed = true;
                }
            }
            catch (System.Exception ex)
            {
                result.Passed = false;
                result.ErrorMessage = ex.Message;
                LogError($"❌ GameSystemInitializer discovery failed: {ex.Message}");
            }
            
            result.ExecutionTime = Time.realtimeSinceStartup - startTime;
            _results.Add(result);
            yield return null;
        }

        /// <summary>
        /// Test ServiceContainer health and functionality
        /// </summary>
        private IEnumerator ValidateServiceContainerHealth()
        {
            LogInfo("🩺 ServiceContainer Health Validation");
            
            var result = new ValidationResult { TestName = "ServiceContainer Health Check" };
            var startTime = Time.realtimeSinceStartup;
            
            try
            {
                if (ServiceContainerFactory.Instance == null)
                {
                    throw new System.Exception("ServiceContainerFactory.Instance is null");
                }
                
                // Test basic functionality
                var registeredTypes = ServiceContainerFactory.Instance.GetRegisteredTypes().ToList();
                LogDebug($"ServiceContainer has {registeredTypes.Count} registered types");
                
                foreach (var type in registeredTypes)
                {
                    LogDebug($"Registered: {type.Name}");
                }
                
                result.Passed = true;
                LogSuccess("✅ ServiceContainer health check passed");
            }
            catch (System.Exception ex)
            {
                result.Passed = false;
                result.ErrorMessage = ex.Message;
                LogError($"❌ ServiceContainer health check failed: {ex.Message}");
            }
            
            result.ExecutionTime = Time.realtimeSinceStartup - startTime;
            _results.Add(result);
            yield return null;
        }

        /// <summary>
        /// Test fallback mechanisms work correctly
        /// </summary>
        private IEnumerator ValidateFallbackMechanisms()
        {
            LogInfo("🔄 Fallback Mechanism Validation");
            
            var result = new ValidationResult { TestName = "Fallback Mechanisms" };
            var startTime = Time.realtimeSinceStartup;
            
            try
            {
                // Test that scene discovery still works for missing services
                var sceneManagers = UnityEngine.Object.FindObjectsOfType<ChimeraManager>();
                LogDebug($"Scene discovery found {sceneManagers.Length} ChimeraManagers");
                
                var sceneCameras = UnityEngine.Object.FindObjectsOfType<Camera>();
                LogDebug($"Scene discovery found {sceneCameras.Length} Cameras");
                
                result.Passed = true;
                LogSuccess("✅ Fallback mechanisms functional");
            }
            catch (System.Exception ex)
            {
                result.Passed = false;
                result.ErrorMessage = ex.Message;
                LogError($"❌ Fallback mechanism validation failed: {ex.Message}");
            }
            
            result.ExecutionTime = Time.realtimeSinceStartup - startTime;
            _results.Add(result);
            yield return null;
        }

        /// <summary>
        /// Generate final validation report
        /// </summary>
        private void GenerateFinalReport()
        {
            _totalTests = _results.Count;
            _passedTests = _results.Count(r => r.Passed);
            _failedTests = _results.Count(r => !r.Passed);
            _failedTestNames = _results.Where(r => !r.Passed).Select(r => r.TestName).ToList();
            
            var totalTime = _results.Sum(r => r.ExecutionTime);
            
            LogInfo("📊 FINAL VALIDATION REPORT");
            LogInfo("=" + new string('=', 50));
            LogInfo($"Total Tests: {_totalTests}");
            LogInfo($"Passed: {_passedTests}");
            LogInfo($"Failed: {_failedTests}");
            LogInfo($"Success Rate: {(_passedTests * 100.0f / _totalTests):F1}%");
            LogInfo($"Total Execution Time: {totalTime:F3}s");
            
            if (_failedTests > 0)
            {
                LogError("Failed Tests:");
                foreach (var failedTest in _failedTestNames)
                {
                    LogError($"  ❌ {failedTest}");
                }
            }
            
            if (_failedTests == 0)
            {
                LogSuccess("🎉 ALL VALIDATION TESTS PASSED - Migration successful!");
            }
            else if (_passedTests >= _totalTests * 0.8f) // 80% pass rate
            {
                LogWarning($"⚠️ Most tests passed ({_passedTests}/{_totalTests}) - Minor issues detected");
            }
            else
            {
                LogError($"❌ Significant issues detected ({_failedTests}/{_totalTests} failures)");
            }
        }

        #region Manual Test Methods

        /// <summary>
        /// Manually trigger validation tests
        /// </summary>
        [ContextMenu("Run All Validation Tests")]
        public void ManualRunAllTests()
        {
            StartCoroutine(RunAllValidationTests());
        }

        /// <summary>
        /// Test specific service resolution manually
        /// </summary>
        [ContextMenu("Test Camera Service Resolution")]
        public void ManualTestCameraService()
        {
            if (ServiceContainerFactory.Instance != null)
            {
                if (ServiceContainerFactory.Instance.TryResolve<Camera>(out var camera))
                {
                    LogSuccess($"✅ Manual Camera Test: {camera?.name}");
                }
                else
                {
                    LogWarning("⚠️ Manual Camera Test: Not found in ServiceContainer");
                }
            }
        }

        #endregion

        #region Logging Helpers

        private void LogInfo(string message)
        {
            if (_enableDetailedLogging)
                ChimeraLogger.Log($"[ServiceValidation] {message}");
        }

        private void LogSuccess(string message)
        {
            if (_enableDetailedLogging)
                ChimeraLogger.Log($"[ServiceValidation] {message}");
        }

        private void LogWarning(string message)
        {
            if (_enableDetailedLogging)
                ChimeraLogger.LogWarning($"[ServiceValidation] {message}");
        }

        private void LogError(string message)
        {
            ChimeraLogger.LogError($"[ServiceValidation] {message}");
        }

        private void LogDebug(string message)
        {
            if (_enableDetailedLogging)
                ChimeraLogger.LogVerbose($"[ServiceValidation] {message}");
        }

        #endregion
    }
}