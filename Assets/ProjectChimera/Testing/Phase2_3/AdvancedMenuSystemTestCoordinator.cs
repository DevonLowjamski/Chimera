using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Systems.Services.Core;
using System.Collections.Generic;
using System.Collections;

namespace ProjectChimera.Testing.Phase2_3
{
    /// <summary>
    /// Test coordinator for Phase 2.3: Advanced Menu System Implementation
    /// Coordinates and runs all focused test components in sequence
    /// </summary>
    public class AdvancedMenuSystemTestCoordinator : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool _runTestsOnStart = true;
        [SerializeField] private bool _enableDetailedLogging = true;
        [SerializeField] private bool _runPerformanceTests = true;
        
        [Header("Test Results")]
        [SerializeField] private bool _allTestsPassed = false;
        [SerializeField] private int _totalTestSuites = 0;
        [SerializeField] private int _passedTestSuites = 0;
        [SerializeField] private List<string> _testSuiteResults = new List<string>();
        
        // Test components
        private MenuSystemCoreTests _coreTests;
        private ContextFilteringTests _contextTests;
        private VisualFeedbackTests _visualTests;
        private InputSystemTests _inputTests;
        private PerformanceTests _performanceTests;
        private SystemIntegrationTests _integrationTests;
        
        private void Start()
        {
            if (_runTestsOnStart)
            {
                StartCoroutine(RunAllTestsCoroutine());
            }
        }
        
        private IEnumerator RunAllTestsCoroutine()
        {
            yield return new WaitForSeconds(0.5f); // Allow systems to initialize
            RunAllTests();
        }
        
        public void RunAllTests()
        {
            ChimeraLogger.Log("[AdvancedMenuSystemTestCoordinator] Starting Phase 2.3 comprehensive tests...");
            
            _testSuiteResults.Clear();
            _totalTestSuites = 0;
            _passedTestSuites = 0;
            
            SetupTestComponents();
            
            // Run all test suites in sequence
            RunTestSuite("Core Menu System", () => _coreTests.RunTests());
            RunTestSuite("Context Filtering", () => _contextTests.RunTests());
            RunTestSuite("Visual Feedback", () => _visualTests.RunTests());
            RunTestSuite("Input System", () => _inputTests.RunTests());
            RunTestSuite("System Integration", () => _integrationTests.RunTests());
            
            if (_runPerformanceTests)
            {
                RunTestSuite("Performance", () => _performanceTests.RunTests());
            }
            
            _allTestsPassed = (_passedTestSuites == _totalTestSuites);
            
            LogFinalResults();
        }
        
        private void SetupTestComponents()
        {
            // Get or create test components
            _coreTests = GetComponent<MenuSystemCoreTests>() ?? gameObject.AddComponent<MenuSystemCoreTests>();
            _contextTests = GetComponent<ContextFilteringTests>() ?? gameObject.AddComponent<ContextFilteringTests>();
            _visualTests = GetComponent<VisualFeedbackTests>() ?? gameObject.AddComponent<VisualFeedbackTests>();
            _inputTests = GetComponent<InputSystemTests>() ?? gameObject.AddComponent<InputSystemTests>();
            _performanceTests = GetComponent<PerformanceTests>() ?? gameObject.AddComponent<PerformanceTests>();
            _integrationTests = GetComponent<SystemIntegrationTests>() ?? gameObject.AddComponent<SystemIntegrationTests>();
            
            // Configure all test components for coordinated execution
            ConfigureTestComponent(_coreTests);
            ConfigureTestComponent(_contextTests);
            ConfigureTestComponent(_visualTests);
            ConfigureTestComponent(_inputTests);
            ConfigureTestComponent(_performanceTests);
            ConfigureTestComponent(_integrationTests);
        }
        
        private void ConfigureTestComponent(MonoBehaviour testComponent)
        {
            // Set common configuration for all test components
            var enableLoggingField = testComponent.GetType().GetField("_enableDetailedLogging", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (enableLoggingField != null)
            {
                enableLoggingField.SetValue(testComponent, _enableDetailedLogging);
            }
        }
        
        private void RunTestSuite(string suiteName, System.Action testAction)
        {
            _totalTestSuites++;
            
            ChimeraLogger.Log($"[TestCoordinator] Running {suiteName} Tests...");
            
            try
            {
                testAction.Invoke();
                
                // Test suite passed if no exceptions occurred
                LogTestSuite(suiteName, true, "Test suite completed successfully");
                _passedTestSuites++;
            }
            catch (System.Exception ex)
            {
                LogTestSuite(suiteName, false, $"Test suite failed with exception: {ex.Message}");
            }
        }
        
        private void LogTestSuite(string suiteName, bool passed, string message)
        {
            string result = passed ? "PASS" : "FAIL";
            string logMessage = $"[{result}] {suiteName}: {message}";
            
            _testSuiteResults.Add(logMessage);
            
            if (passed)
            {
                ChimeraLogger.Log($"✅ {logMessage}");
            }
            else
            {
                ChimeraLogger.LogError($"❌ {logMessage}");
            }
        }
        
        private void LogFinalResults()
        {
            ChimeraLogger.Log($"[TestCoordinator] All test suites completed: {_passedTestSuites}/{_totalTestSuites} passed");
            
            if (_allTestsPassed)
            {
                ChimeraLogger.Log("🎉 Phase 2.3: Advanced Menu System - ALL TEST SUITES PASSED!");
                ChimeraLogger.Log("✨ Ready for Phase 3 Development!");
            }
            else
            {
                ChimeraLogger.LogWarning($"⚠️ Phase 2.3: Advanced Menu System - {_totalTestSuites - _passedTestSuites} test suites failed");
                ChimeraLogger.LogWarning("🔧 Please review failed test suites before proceeding to Phase 3");
            }
            
            // Log summary of all test results
            ChimeraLogger.Log("📊 Test Suite Summary:");
            foreach (var result in _testSuiteResults)
            {
                ChimeraLogger.Log($"  {result}");
            }
        }
        
        // Public methods for manual test execution
        public void RunCoreTests() => _coreTests?.RunTests();
        public void RunContextTests() => _contextTests?.RunTests();
        public void RunVisualTests() => _visualTests?.RunTests();
        public void RunInputTests() => _inputTests?.RunTests();
        public void RunPerformanceTests() => _performanceTests?.RunTests();
        public void RunIntegrationTests() => _integrationTests?.RunTests();
        
        // Inspector button methods
        [ContextMenu("Run All Tests")]
        public void RunAllTestsFromMenu() => RunAllTests();
        
        [ContextMenu("Run Core Tests Only")]
        public void RunCoreTestsOnly() => RunCoreTests();
        
        [ContextMenu("Run Performance Tests Only")]
        public void RunPerformanceTestsOnly() => RunPerformanceTests();
    }
}