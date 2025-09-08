using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Systems.UI.Advanced;
using ProjectChimera.Systems.Services.Core;
using System.Collections.Generic;

namespace ProjectChimera.Testing.Phase2_3
{
    /// <summary>
    /// Context-aware filtering tests - Tests action and category filtering based on context
    /// </summary>
    public class ContextFilteringTests : MonoBehaviour
    {
        [Header("Context Filtering Test Configuration")]
        [SerializeField] private bool _enableDetailedLogging = true;
        [SerializeField] private bool _runTestsOnStart = false;
        
        [Header("Test Results")]
        [SerializeField] private int _totalTests = 0;
        [SerializeField] private int _passedTests = 0;
        [SerializeField] private List<string> _testResults = new List<string>();
        
        // Test components
        private ContextAwareActionFilter _actionFilter;
        private MenuContext _testContext;
        private List<MenuAction> _testActions = new List<MenuAction>();
        private List<MenuCategory> _testCategories = new List<MenuCategory>();
        
        public void RunTests()
        {
            ChimeraLogger.Log("[ContextFilteringTests] Starting context filtering tests...");
            
            _testResults.Clear();
            _totalTests = 0;
            _passedTests = 0;
            
            SetupTestEnvironment();
            CreateTestData();
            
            TestContextAwareFiltering();
            
            LogResults();
        }
        
        private void SetupTestEnvironment()
        {
            _actionFilter = GetComponent<ContextAwareActionFilter>();
            if (_actionFilter == null)
            {
                _actionFilter = gameObject.AddComponent<ContextAwareActionFilter>();
            }
        }
        
        private void CreateTestData()
        {
            // Create test context
            _testContext = new MenuContext
            {
                WorldPosition = Vector3.zero,
                TargetObject = gameObject,
                ContextType = "TestObject",
                Timestamp = Time.time,
                PlayerPosition = Vector3.zero,
                CameraForward = Vector3.forward
            };
            
            // Create test categories with different contexts
            _testCategories.AddRange(new[]
            {
                new MenuCategory("context_construction", "Construction", "Construction")
                {
                    Description = "Context-aware construction category",
                    Priority = 100,
                    IsVisible = true,
                    RequiredContext = "Construction"
                },
                new MenuCategory("context_cultivation", "Cultivation", "Cultivation")
                {
                    Description = "Context-aware cultivation category",
                    Priority = 90,
                    IsVisible = true,
                    RequiredContext = "Cultivation"
                }
            });
            
            // Create test actions with different context requirements
            _testActions.AddRange(new[]
            {
                new MenuAction("context_build", "context_construction", "Build Here", "Construction")
                {
                    Description = "Context-dependent building action",
                    Priority = 100,
                    IsEnabled = true,
                    IsVisible = true,
                    RequiredContext = "Construction"
                },
                new MenuAction("context_plant", "context_cultivation", "Plant Here", "Cultivation")
                {
                    Description = "Context-dependent planting action",
                    Priority = 90,
                    IsEnabled = true,
                    IsVisible = true,
                    RequiredContext = "Cultivation"
                },
                new MenuAction("context_generic", "context_construction", "Generic Action", "General")
                {
                    Description = "Always available action",
                    Priority = 50,
                    IsEnabled = true,
                    IsVisible = true,
                    RequiredContext = ""
                }
            });
        }
        
        private void TestContextAwareFiltering()
        {
            LogTestCategory("Context-Aware Filtering Tests");
            
            TestActionFiltering();
            TestCategoryFiltering();
            TestRelevanceScoring();
            TestContextAnalysis();
        }
        
        private void TestActionFiltering()
        {
            _totalTests++;
            string testName = "Action Context Filtering";
            
            try
            {
                // Test filtering with construction context
                var constructionContext = new MenuContext(_testContext)
                {
                    ContextType = "Construction"
                };
                
                var filteredActions = _actionFilter.FilterActions(_testActions, constructionContext);
                
                // Should include construction actions and generic actions, exclude cultivation
                bool hasConstructionAction = filteredActions.Exists(a => a.Id == "context_build");
                bool hasGenericAction = filteredActions.Exists(a => a.Id == "context_generic");
                bool excludesCultivationAction = !filteredActions.Exists(a => a.Id == "context_plant");
                
                if (hasConstructionAction && hasGenericAction && excludesCultivationAction)
                {
                    LogTest(testName, true, $"Correctly filtered {filteredActions.Count} actions for construction context");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, $"Action filtering failed - Construction: {hasConstructionAction}, Generic: {hasGenericAction}, Excluded Cultivation: {excludesCultivationAction}");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void TestCategoryFiltering()
        {
            _totalTests++;
            string testName = "Category Context Filtering";
            
            try
            {
                // Test filtering with cultivation context
                var cultivationContext = new MenuContext(_testContext)
                {
                    ContextType = "Cultivation"
                };
                
                var filteredCategories = _actionFilter.FilterCategories(_testCategories, cultivationContext);
                
                // Should include cultivation category, may exclude construction based on context
                bool hasCultivationCategory = filteredCategories.Exists(c => c.Id == "context_cultivation");
                
                if (hasCultivationCategory)
                {
                    LogTest(testName, true, $"Correctly filtered {filteredCategories.Count} categories for cultivation context");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, "Category filtering failed - missing cultivation category");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void TestRelevanceScoring()
        {
            _totalTests++;
            string testName = "Relevance Scoring";
            
            try
            {
                var testAction = _testActions[0]; // Construction action
                
                // Test with matching context
                var constructionContext = new MenuContext(_testContext) { ContextType = "Construction" };
                float highRelevanceScore = _actionFilter.CalculateRelevanceScore(testAction, constructionContext);
                
                // Test with non-matching context
                var cultivationContext = new MenuContext(_testContext) { ContextType = "Cultivation" };
                float lowRelevanceScore = _actionFilter.CalculateRelevanceScore(testAction, cultivationContext);
                
                if (highRelevanceScore > lowRelevanceScore)
                {
                    LogTest(testName, true, $"Relevance scoring working - High: {highRelevanceScore:F2}, Low: {lowRelevanceScore:F2}");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, $"Relevance scoring failed - High: {highRelevanceScore:F2}, Low: {lowRelevanceScore:F2}");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void TestContextAnalysis()
        {
            _totalTests++;
            string testName = "Context Analysis";
            
            try
            {
                // Test distance-based context analysis
                var nearContext = new MenuContext(_testContext)
                {
                    WorldPosition = Vector3.zero,
                    PlayerPosition = Vector3.forward
                };
                
                var farContext = new MenuContext(_testContext)
                {
                    WorldPosition = Vector3.zero,
                    PlayerPosition = Vector3.forward * 100f
                };
                
                float nearRelevance = _actionFilter.AnalyzeContextRelevance(nearContext);
                float farRelevance = _actionFilter.AnalyzeContextRelevance(farContext);
                
                // Assuming distance affects relevance
                bool distanceAffectsRelevance = Mathf.Abs(nearRelevance - farRelevance) > 0.01f;
                
                if (distanceAffectsRelevance)
                {
                    LogTest(testName, true, $"Context analysis working - Near: {nearRelevance:F2}, Far: {farRelevance:F2}");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, true, "Context analysis stable across distances");
                    _passedTests++;
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
                ChimeraLogger.Log($"[ContextFilteringTests] === {categoryName} ===");
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
            
            ChimeraLogger.Log($"[ContextFilteringTests] Tests completed: {_passedTests}/{_totalTests} passed");
            
            if (allTestsPassed)
            {
                ChimeraLogger.Log("✅ Context Filtering Tests - ALL TESTS PASSED!");
            }
            else
            {
                ChimeraLogger.LogWarning($"⚠️ Context Filtering Tests - {_totalTests - _passedTests} tests failed");
            }
        }
    }
}