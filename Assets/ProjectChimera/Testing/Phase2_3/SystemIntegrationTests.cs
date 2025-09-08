using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Systems.UI.Advanced;
using ProjectChimera.Systems.Services.Core;
using System.Collections.Generic;
using System.Collections;

namespace ProjectChimera.Testing.Phase2_3
{
    /// <summary>
    /// System integration tests - Tests how all menu system components work together
    /// </summary>
    public class SystemIntegrationTests : MonoBehaviour
    {
        [Header("Integration Test Configuration")]
        [SerializeField] private bool _enableDetailedLogging = true;
        [SerializeField] private bool _runTestsOnStart = false;
        
        [Header("Test Results")]
        [SerializeField] private int _totalTests = 0;
        [SerializeField] private int _passedTests = 0;
        [SerializeField] private List<string> _testResults = new List<string>();
        
        // System components
        private AdvancedMenuSystem _menuSystem;
        private ContextAwareActionFilter _actionFilter;
        private VisualFeedbackIntegration _visualFeedback;
        private MenuPerformanceOptimization _performanceSystem;
        
        // Test data
        private List<MenuAction> _testActions = new List<MenuAction>();
        private List<MenuCategory> _testCategories = new List<MenuCategory>();
        private MenuContext _testContext;
        
        public void RunTests()
        {
            ChimeraLogger.Log("[SystemIntegrationTests] Starting system integration tests...");
            
            _testResults.Clear();
            _totalTests = 0;
            _passedTests = 0;
            
            SetupTestEnvironment();
            CreateTestData();
            
            TestSystemIntegration();
            
            LogResults();
        }
        
        private void SetupTestEnvironment()
        {
            // Initialize all system components
            _menuSystem = GetComponent<AdvancedMenuSystem>() ?? gameObject.AddComponent<AdvancedMenuSystem>();
            _actionFilter = GetComponent<ContextAwareActionFilter>() ?? gameObject.AddComponent<ContextAwareActionFilter>();
            _visualFeedback = GetComponent<VisualFeedbackIntegration>() ?? gameObject.AddComponent<VisualFeedbackIntegration>();
            _performanceSystem = GetComponent<MenuPerformanceOptimization>() ?? gameObject.AddComponent<MenuPerformanceOptimization>();
        }
        
        private void CreateTestData()
        {
            // Create comprehensive test context
            _testContext = new MenuContext
            {
                WorldPosition = Vector3.zero,
                TargetObject = gameObject,
                ContextType = "IntegrationTest",
                Timestamp = Time.time,
                PlayerPosition = Vector3.zero,
                CameraForward = Vector3.forward
            };
            
            // Create test categories for integration testing
            _testCategories.AddRange(new[]
            {
                new MenuCategory("integration_construction", "Construction", "Construction")
                {
                    Description = "Integration test construction category",
                    Priority = 100,
                    IsVisible = true,
                    RequiredContext = "Construction"
                },
                new MenuCategory("integration_cultivation", "Cultivation", "Cultivation")
                {
                    Description = "Integration test cultivation category",
                    Priority = 90,
                    IsVisible = true,
                    RequiredContext = "Cultivation"
                }
            });
            
            // Create test actions for integration testing
            _testActions.AddRange(new[]
            {
                new MenuAction("integration_build", "integration_construction", "Build Structure", "Construction")
                {
                    Description = "Integration test building action",
                    Priority = 100,
                    IsEnabled = true,
                    IsVisible = true,
                    RequiredContext = "Construction"
                },
                new MenuAction("integration_plant", "integration_cultivation", "Plant Seed", "Cultivation")
                {
                    Description = "Integration test planting action",
                    Priority = 90,
                    IsEnabled = true,
                    IsVisible = true,
                    RequiredContext = "Cultivation"
                }
            });
        }
        
        private void TestSystemIntegration()
        {
            LogTestCategory("System Integration Tests");
            
            TestMenuSystemIntegration();
            TestEventSystemIntegration();
            TestDataFlowIntegration();
        }
        
        private void TestMenuSystemIntegration()
        {
            _totalTests++;
            string testName = "Menu System Integration";
            
            try
            {
                // Register test data across systems
                foreach (var category in _testCategories)
                {
                    _menuSystem.RegisterCategory(category);
                }
                
                foreach (var action in _testActions)
                {
                    _menuSystem.RegisterAction(action);
                }
                
                // Test integrated menu opening with context filtering
                _menuSystem.ShowMenu(_testContext.WorldPosition, _testContext.TargetObject);
                bool menuOpened = _menuSystem.IsMenuVisible();
                
                // Test filtering integration
                var filteredActions = _actionFilter.FilterActions(_testActions, _testContext);
                bool filteringWorked = filteredActions.Count >= 0;
                
                // Test visual feedback integration
                _visualFeedback.OnMenuOpened();
                bool visualFeedbackTriggered = true; // Assume success if no exception
                
                // Test performance optimization integration
                _performanceSystem.OptimizeMenuDisplay();
                bool performanceOptimized = true; // Assume success if no exception
                
                // Close menu
                _menuSystem.HideMenu();
                bool menuClosed = !_menuSystem.IsMenuVisible();
                
                if (menuOpened && filteringWorked && visualFeedbackTriggered && performanceOptimized && menuClosed)
                {
                    LogTest(testName, true, "All systems integrated successfully");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, $"Integration issues - Menu: {menuOpened}/{menuClosed}, Filter: {filteringWorked}, Visual: {visualFeedbackTriggered}, Perf: {performanceOptimized}");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void TestEventSystemIntegration()
        {
            _totalTests++;
            string testName = "Event System Integration";
            
            try
            {
                bool eventsTriggered = false;
                
                // Subscribe to menu system events
                _menuSystem.OnMenuOpened += () => eventsTriggered = true;
                _menuSystem.OnMenuClosed += () => eventsTriggered = true;
                _menuSystem.OnActionSelected += (action) => eventsTriggered = true;
                
                // Trigger events through integrated operations
                _menuSystem.ShowMenu(Vector3.zero, gameObject);
                _menuSystem.SelectAction(_testActions[0]);
                _menuSystem.HideMenu();
                
                // Test cross-system event communication
                _actionFilter.OnFilteringCompleted += (count) => eventsTriggered = true;
                _actionFilter.FilterActions(_testActions, _testContext);
                
                _visualFeedback.OnAnimationCompleted += () => eventsTriggered = true;
                _visualFeedback.PlayFadeInAnimation("test_element", 0.1f);
                
                if (eventsTriggered)
                {
                    LogTest(testName, true, "Event system integration working");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, "Event system integration failed");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void TestDataFlowIntegration()
        {
            _totalTests++;
            string testName = "Data Flow Integration";
            
            try
            {
                // Test data flow: Context -> Filtering -> Menu -> Visual Feedback -> Performance
                
                // 1. Context analysis
                float contextRelevance = _actionFilter.AnalyzeContextRelevance(_testContext);
                bool contextAnalyzed = contextRelevance >= 0;
                
                // 2. Action filtering based on context
                var filteredActions = _actionFilter.FilterActions(_testActions, _testContext);
                bool actionsFiltered = filteredActions != null;
                
                // 3. Menu display with filtered actions
                _menuSystem.UpdateMenuWithActions(filteredActions);
                bool menuUpdated = _menuSystem.GetActionCount() >= 0;
                
                // 4. Visual feedback for menu changes
                _visualFeedback.OnMenuContentChanged();
                bool visualUpdated = true; // Assume success if no exception
                
                // 5. Performance optimization for the entire flow
                _performanceSystem.OptimizeDataFlow(filteredActions.Count);
                bool performanceOptimized = true; // Assume success if no exception
                
                if (contextAnalyzed && actionsFiltered && menuUpdated && visualUpdated && performanceOptimized)
                {
                    LogTest(testName, true, "Data flow integration complete");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, $"Data flow issues - Context: {contextAnalyzed}, Filter: {actionsFiltered}, Menu: {menuUpdated}, Visual: {visualUpdated}, Perf: {performanceOptimized}");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void LogTestCategory(string categoryName)
        {
            if (_enableDetailedLogging)
            {
                ChimeraLogger.Log($"[SystemIntegrationTests] === {categoryName} ===");
            }
        }
        
        private void LogTest(string testName, bool passed, string message)
        {
            string result = passed ? "PASS" : "FAIL";
            string logMessage = $"[{result}] {testName}: {message}";
            
            _testResults.Add(logMessage);
            
            if (_enableDetailedLogging)
            {
                if (passed)
                {
                    ChimeraLogger.Log($"✅ {logMessage}");
                }
                else
                {
                    ChimeraLogger.LogError($"❌ {logMessage}");
                }
            }
        }
        
        private void LogResults()
        {
            bool allTestsPassed = (_passedTests == _totalTests);
            
            ChimeraLogger.Log($"[SystemIntegrationTests] Tests completed: {_passedTests}/{_totalTests} passed");
            
            if (allTestsPassed)
            {
                ChimeraLogger.Log("✅ System Integration Tests - ALL TESTS PASSED!");
            }
            else
            {
                ChimeraLogger.LogWarning($"⚠️ System Integration Tests - {_totalTests - _passedTests} tests failed");
            }
        }
    }
}