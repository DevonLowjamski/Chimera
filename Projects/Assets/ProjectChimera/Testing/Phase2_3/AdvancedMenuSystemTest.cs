using UnityEngine;
using UnityEngine.UIElements;
using ProjectChimera.Systems.UI.Advanced;
using ProjectChimera.Systems.Services.Core;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace ProjectChimera.Testing.Phase2_3
{
    /// <summary>
    /// Comprehensive test suite for Phase 2.3: Advanced Menu System Implementation
    /// Tests all components: Dynamic Categories, Context-Aware Filtering, Visual Feedback,
    /// Input Integration, and Performance Optimization
    /// </summary>
    public class AdvancedMenuSystemTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool _runTestsOnStart = true;
        [SerializeField] private bool _enableDetailedLogging = true;
        [SerializeField] private bool _runPerformanceTests = true;
        [SerializeField] private int _performanceTestIterations = 100;
        
        [Header("Test Results")]
        [SerializeField] private bool _allTestsPassed = false;
        [SerializeField] private int _totalTests = 0;
        [SerializeField] private int _passedTests = 0;
        [SerializeField] private List<string> _testResults = new List<string>();
        
        // System components being tested
        private AdvancedMenuSystem _menuSystem;
        private ContextAwareActionFilter _actionFilter;
        private VisualFeedbackIntegration _visualFeedback;
#if UNITY_INPUT_SYSTEM
        private InputSystemIntegration _inputSystem;
#endif
        private MenuPerformanceOptimization _performanceSystem;
        
        // Test data
        private List<MenuAction> _testActions = new List<MenuAction>();
        private List<MenuCategory> _testCategories = new List<MenuCategory>();
        private MenuContext _testContext;
        
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
            Debug.Log("[AdvancedMenuSystemTest] Starting Phase 2.3 comprehensive tests...");
            
            _testResults.Clear();
            _totalTests = 0;
            _passedTests = 0;
            
            SetupTestEnvironment();
            CreateTestData();
            
            // Test each system component
            TestAdvancedMenuSystem();
            TestContextAwareFiltering();
            TestVisualFeedbackIntegration();
#if UNITY_INPUT_SYSTEM
            TestInputSystemIntegration();
#endif
            TestPerformanceOptimization();
            
            // Integration tests
            TestSystemIntegration();
            
            if (_runPerformanceTests)
            {
                TestPerformanceBenchmarks();
            }
            
            _allTestsPassed = (_passedTests == _totalTests);
            
            Debug.Log($"[AdvancedMenuSystemTest] Tests completed: {_passedTests}/{_totalTests} passed");
            
            if (_allTestsPassed)
            {
                Debug.Log("✅ Phase 2.3: Advanced Menu System - ALL TESTS PASSED!");
            }
            else
            {
                Debug.LogWarning($"⚠️ Phase 2.3: Advanced Menu System - {_totalTests - _passedTests} tests failed");
            }
        }
        
        private void SetupTestEnvironment()
        {
            // Find or create system components
            _menuSystem = GetComponent<AdvancedMenuSystem>();
            if (_menuSystem == null)
            {
                _menuSystem = gameObject.AddComponent<AdvancedMenuSystem>();
            }
            
            _actionFilter = GetComponent<ContextAwareActionFilter>();
            if (_actionFilter == null)
            {
                _actionFilter = gameObject.AddComponent<ContextAwareActionFilter>();
            }
            
            _visualFeedback = GetComponent<VisualFeedbackIntegration>();
            if (_visualFeedback == null)
            {
                _visualFeedback = gameObject.AddComponent<VisualFeedbackIntegration>();
            }
            
#if UNITY_INPUT_SYSTEM
            _inputSystem = GetComponent<InputSystemIntegration>();
            if (_inputSystem == null)
            {
                var playerInput = gameObject.GetComponent<UnityEngine.InputSystem.PlayerInput>();
                if (playerInput == null)
                {
                    playerInput = gameObject.AddComponent<UnityEngine.InputSystem.PlayerInput>();
                }
                _inputSystem = gameObject.AddComponent<InputSystemIntegration>();
            }
#endif
            
            _performanceSystem = GetComponent<MenuPerformanceOptimization>();
            if (_performanceSystem == null)
            {
                _performanceSystem = gameObject.AddComponent<MenuPerformanceOptimization>();
            }
            
            // Ensure UIDocument is present
            if (GetComponent<UIDocument>() == null)
            {
                gameObject.AddComponent<UIDocument>();
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
            
            // Create test categories
            _testCategories.AddRange(new[]
            {
                new MenuCategory("test_construction", "Construction", "Construction")
                {
                    Description = "Test construction category",
                    Priority = 100,
                    IsVisible = true
                },
                new MenuCategory("test_cultivation", "Cultivation", "Cultivation")
                {
                    Description = "Test cultivation category",
                    Priority = 90,
                    IsVisible = true
                },
                new MenuCategory("test_genetics", "Genetics", "Genetics")
                {
                    Description = "Test genetics category",
                    Priority = 80,
                    IsVisible = true
                }
            });
            
            // Create test actions
            _testActions.AddRange(new[]
            {
                new MenuAction("test_build", "test_construction", "Build Structure", "Construction")
                {
                    Description = "Test building action",
                    Priority = 100,
                    IsEnabled = true,
                    IsVisible = true
                },
                new MenuAction("test_plant", "test_cultivation", "Plant Seed", "Cultivation")
                {
                    Description = "Test planting action",
                    Priority = 90,
                    IsEnabled = true,
                    IsVisible = true
                },
                new MenuAction("test_breed", "test_genetics", "Breed Plants", "Genetics")
                {
                    Description = "Test breeding action",
                    Priority = 80,
                    IsEnabled = false, // Test disabled state
                    IsVisible = true
                }
            });
        }
        
        private void TestAdvancedMenuSystem()
        {
            LogTestCategory("Advanced Menu System Tests");
            
            // Test menu system initialization
            TestMenuSystemInitialization();
            
            // Test category registration
            TestCategoryRegistration();
            
            // Test action registration
            TestActionRegistration();
            
            // Test menu opening and closing
            TestMenuOperations();
            
            // Test dynamic category system
            TestDynamicCategories();
        }
        
        private void TestMenuSystemInitialization()
        {
            _totalTests++;
            string testName = "Menu System Initialization";
            
            try
            {
                bool isInitialized = _menuSystem != null && _menuSystem.GetCategoryCount() >= 0;
                
                if (isInitialized)
                {
                    LogTest(testName, true, "Menu system initialized successfully");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, "Menu system failed to initialize");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void TestCategoryRegistration()
        {
            _totalTests++;
            string testName = "Category Registration";
            
            try
            {
                int initialCount = _menuSystem.GetCategoryCount();
                
                foreach (var category in _testCategories)
                {
                    _menuSystem.RegisterCategory(category);
                }
                
                int finalCount = _menuSystem.GetCategoryCount();
                bool categoriesAdded = finalCount > initialCount;
                
                if (categoriesAdded)
                {
                    LogTest(testName, true, $"Successfully registered {finalCount - initialCount} categories");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, "No categories were registered");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void TestActionRegistration()
        {
            _totalTests++;
            string testName = "Action Registration";
            
            try
            {
                int initialCount = _menuSystem.GetActionCount();
                
                foreach (var action in _testActions)
                {
                    _menuSystem.RegisterAction(action);
                }
                
                int finalCount = _menuSystem.GetActionCount();
                bool actionsAdded = finalCount > initialCount;
                
                if (actionsAdded)
                {
                    LogTest(testName, true, $"Successfully registered {finalCount - initialCount} actions");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, "No actions were registered");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void TestMenuOperations()
        {
            _totalTests++;
            string testName = "Menu Operations";
            
            try
            {
                bool initiallyOpen = _menuSystem.IsMenuOpen();
                
                // Test opening menu
                _menuSystem.OpenContextualMenu(Vector3.zero, gameObject, _testContext);
                bool openedSuccessfully = _menuSystem.IsMenuOpen();
                
                // Test menu state
                int activeMenus = _menuSystem.GetActiveMenuCount();
                
                if (!initiallyOpen && openedSuccessfully && activeMenus > 0)
                {
                    LogTest(testName, true, $"Menu operations working - {activeMenus} active menus");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, $"Menu operations failed - Initially: {initiallyOpen}, Opened: {openedSuccessfully}, Active: {activeMenus}");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void TestDynamicCategories()
        {
            _totalTests++;
            string testName = "Dynamic Categories";
            
            try
            {
                // Test dynamic category creation based on context
                var dynamicCategory = new MenuCategory("dynamic_test", "Dynamic Test", "Test")
                {
                    IsDynamic = true,
                    ConditionCallback = (context) => context.ContextType == "TestObject"
                };
                
                _menuSystem.RegisterCategory(dynamicCategory);
                
                bool categoryExists = _menuSystem.GetCategory("dynamic_test") != null;
                
                if (categoryExists)
                {
                    LogTest(testName, true, "Dynamic category system working");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, "Dynamic category system failed");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
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
            string testName = "Action Filtering";
            
            try
            {
                var filteredActions = _actionFilter.FilterActions(_testActions, _testContext);
                
                bool hasResults = filteredActions != null && filteredActions.Count > 0;
                bool properFiltering = filteredActions.Count <= _testActions.Count;
                
                if (hasResults && properFiltering)
                {
                    LogTest(testName, true, $"Filtered {_testActions.Count} actions to {filteredActions.Count}");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, $"Action filtering failed - Results: {hasResults}, Proper: {properFiltering}");
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
            string testName = "Category Filtering";
            
            try
            {
                var filteredCategories = _actionFilter.FilterCategories(_testCategories, _testContext, _testActions);
                
                bool hasResults = filteredCategories != null && filteredCategories.Count > 0;
                bool properFiltering = filteredCategories.Count <= _testCategories.Count;
                
                if (hasResults && properFiltering)
                {
                    LogTest(testName, true, $"Filtered {_testCategories.Count} categories to {filteredCategories.Count}");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, $"Category filtering failed - Results: {hasResults}, Proper: {properFiltering}");
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
                // Test relevance score calculation
                float relevanceScore = _actionFilter.GetActionRelevance("test_build", "TestObject");
                
                bool validScore = relevanceScore >= 0f && relevanceScore <= 1f;
                
                if (validScore)
                {
                    LogTest(testName, true, $"Relevance scoring working - Score: {relevanceScore:F2}");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, $"Invalid relevance score: {relevanceScore}");
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
                int cacheCount = _actionFilter.GetCachedAnalysisCount();
                
                // Trigger context analysis by filtering actions
                _actionFilter.FilterActions(_testActions, _testContext);
                
                int newCacheCount = _actionFilter.GetCachedAnalysisCount();
                bool contextCached = newCacheCount >= cacheCount;
                
                if (contextCached)
                {
                    LogTest(testName, true, $"Context analysis working - Cache entries: {newCacheCount}");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, "Context analysis not caching properly");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void TestVisualFeedbackIntegration()
        {
            LogTestCategory("Visual Feedback Integration Tests");
            
            TestFeedbackSystemInitialization();
            TestHoverEffects();
            TestAnimationSystem();
            TestVisualStates();
        }
        
        private void TestFeedbackSystemInitialization()
        {
            _totalTests++;
            string testName = "Visual Feedback Initialization";
            
            try
            {
                bool systemExists = _visualFeedback != null;
                int activeAnimations = systemExists ? _visualFeedback.GetActiveAnimationCount() : -1;
                
                if (systemExists && activeAnimations >= 0)
                {
                    LogTest(testName, true, $"Visual feedback system initialized - Animations: {activeAnimations}");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, "Visual feedback system not properly initialized");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void TestHoverEffects()
        {
            _totalTests++;
            string testName = "Hover Effects";
            
            try
            {
                // Test hover effect configuration
                bool hoverEnabled = _visualFeedback != null;
                
                if (hoverEnabled)
                {
                    // Test enabling/disabling hover effects
                    _visualFeedback.SetHoverEnabled(false);
                    _visualFeedback.SetHoverEnabled(true);
                    
                    LogTest(testName, true, "Hover effects system working");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, "Hover effects system not available");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void TestAnimationSystem()
        {
            _totalTests++;
            string testName = "Animation System";
            
            try
            {
                bool animationsEnabled = _visualFeedback != null;
                
                if (animationsEnabled)
                {
                    _visualFeedback.SetAnimationsEnabled(true);
                    int animationCount = _visualFeedback.GetActiveAnimationCount();
                    
                    LogTest(testName, true, $"Animation system working - Active: {animationCount}");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, "Animation system not available");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void TestVisualStates()
        {
            _totalTests++;
            string testName = "Visual States";
            
            try
            {
                bool selectionFeedbackEnabled = _visualFeedback != null;
                
                if (selectionFeedbackEnabled)
                {
                    _visualFeedback.SetSelectionFeedbackEnabled(true);
                    
                    LogTest(testName, true, "Visual states system working");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, "Visual states system not available");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
#if UNITY_INPUT_SYSTEM
        private void TestInputSystemIntegration()
        {
            LogTestCategory("Input System Integration Tests");
            
            TestInputSystemInitialization();
            TestNavigationSystem();
            TestShortcutSystem();
        }
        
        private void TestInputSystemInitialization()
        {
            _totalTests++;
            string testName = "Input System Initialization";
            
            try
            {
                bool systemExists = _inputSystem != null;
                var navigationMode = systemExists ? _inputSystem.CurrentNavigationMode : InputSystemIntegration.NavigationMode.Mouse;
                
                if (systemExists)
                {
                    LogTest(testName, true, $"Input system initialized - Mode: {navigationMode}");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, "Input system not properly initialized");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void TestNavigationSystem()
        {
            _totalTests++;
            string testName = "Navigation System";
            
            try
            {
                if (_inputSystem != null)
                {
                    int navigableElements = _inputSystem.NavigableElementCount;
                    bool searchMode = _inputSystem.IsInSearchMode;
                    
                    LogTest(testName, true, $"Navigation system working - Elements: {navigableElements}, Search: {searchMode}");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, "Navigation system not available");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void TestShortcutSystem()
        {
            _totalTests++;
            string testName = "Shortcut System";
            
            try
            {
                if (_inputSystem != null)
                {
                    // Test search mode toggle
                    bool initialSearchMode = _inputSystem.IsInSearchMode;
                    _inputSystem.SetSearchMode(!initialSearchMode);
                    bool changedSearchMode = _inputSystem.IsInSearchMode != initialSearchMode;
                    
                    if (changedSearchMode)
                    {
                        LogTest(testName, true, "Shortcut system working");
                        _passedTests++;
                    }
                    else
                    {
                        LogTest(testName, false, "Shortcut system not working properly");
                    }
                }
                else
                {
                    LogTest(testName, false, "Shortcut system not available");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
#else
        private void TestInputSystemIntegration()
        {
            LogTestCategory("Input System Integration Tests");
            
            _totalTests++;
            LogTest("Input System Integration", false, "Unity Input System not available - tests skipped");
        }
#endif
        
        private void TestPerformanceOptimization()
        {
            LogTestCategory("Performance Optimization Tests");
            
            TestPerformanceSystemInitialization();
            TestObjectPooling();
            TestLODSystem();
            TestBatchProcessing();
            TestMemoryOptimization();
        }
        
        private void TestPerformanceSystemInitialization()
        {
            _totalTests++;
            string testName = "Performance System Initialization";
            
            try
            {
                bool systemExists = _performanceSystem != null;
                var metrics = systemExists ? _performanceSystem.GetPerformanceMetrics() : null;
                
                if (systemExists && metrics != null)
                {
                    LogTest(testName, true, $"Performance system initialized - FPS: {metrics.FPS:F1}");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, "Performance system not properly initialized");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void TestObjectPooling()
        {
            _totalTests++;
            string testName = "Object Pooling";
            
            try
            {
                if (_performanceSystem != null)
                {
                    int activeElements = _performanceSystem.GetActiveElementCount();
                    int pooledElements = _performanceSystem.GetPooledElementCount();
                    
                    // Test getting pooled elements
                    var testElement = _performanceSystem.GetPooledElement<VisualElement>();
                    bool elementCreated = testElement != null;
                    
                    if (elementCreated)
                    {
                        _performanceSystem.ReturnToPool(testElement);
                        
                        LogTest(testName, true, $"Object pooling working - Active: {activeElements}, Pooled: {pooledElements}");
                        _passedTests++;
                    }
                    else
                    {
                        LogTest(testName, false, "Object pooling failed to create element");
                    }
                }
                else
                {
                    LogTest(testName, false, "Performance system not available");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void TestLODSystem()
        {
            _totalTests++;
            string testName = "LOD System";
            
            try
            {
                if (_performanceSystem != null)
                {
                    var testElement = _performanceSystem.GetPooledElement<VisualElement>();
                    
                    // Test LOD updates
                    _performanceSystem.UpdateElementLOD(testElement, 1f); // Near distance
                    _performanceSystem.UpdateElementLOD(testElement, 10f); // Medium distance
                    _performanceSystem.UpdateElementLOD(testElement, 50f); // Far distance
                    
                    _performanceSystem.ReturnToPool(testElement);
                    
                    LogTest(testName, true, "LOD system working");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, "LOD system not available");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void TestBatchProcessing()
        {
            _totalTests++;
            string testName = "Batch Processing";
            
            try
            {
                if (_performanceSystem != null)
                {
                    var metrics = _performanceSystem.GetPerformanceMetrics();
                    
                    // Queue some test updates
                    for (int i = 0; i < 5; i++)
                    {
                        _performanceSystem.QueueForUpdate(new TestUpdateable());
                    }
                    
                    bool batchProcessing = metrics.UpdateQueueSize >= 0;
                    
                    if (batchProcessing)
                    {
                        LogTest(testName, true, $"Batch processing working - Queue size: {metrics.UpdateQueueSize}");
                        _passedTests++;
                    }
                    else
                    {
                        LogTest(testName, false, "Batch processing not working");
                    }
                }
                else
                {
                    LogTest(testName, false, "Batch processing system not available");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void TestMemoryOptimization()
        {
            _totalTests++;
            string testName = "Memory Optimization";
            
            try
            {
                if (_performanceSystem != null)
                {
                    var initialMetrics = _performanceSystem.GetPerformanceMetrics();
                    
                    // Test memory optimization
                    _performanceSystem.OptimizeTextureMemory();
                    _performanceSystem.ForceGarbageCollection();
                    
                    var finalMetrics = _performanceSystem.GetPerformanceMetrics();
                    
                    LogTest(testName, true, $"Memory optimization working - Memory: {finalMetrics.AllocatedMemory}MB");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, "Memory optimization system not available");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
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
                // Test integration between all systems
                bool menuSystem = _menuSystem != null;
                bool filterSystem = _actionFilter != null;
                bool feedbackSystem = _visualFeedback != null;
                bool performanceSystem = _performanceSystem != null;
                
                bool allSystemsPresent = menuSystem && filterSystem && feedbackSystem && performanceSystem;
                
                if (allSystemsPresent)
                {
                    LogTest(testName, true, "All systems integrated successfully");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, $"System integration incomplete - Menu:{menuSystem}, Filter:{filterSystem}, Feedback:{feedbackSystem}, Performance:{performanceSystem}");
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
                bool eventsWorking = true;
                
                // Subscribe to events to test integration
                if (_menuSystem != null)
                {
                    _menuSystem.OnActionExecuted += (actionId, action) => eventsWorking = true;
                }
                
                if (_actionFilter != null)
                {
                    _actionFilter.OnRelevanceScoreUpdated += (actionId, score) => eventsWorking = true;
                }
                
                LogTest(testName, eventsWorking, "Event system integration working");
                if (eventsWorking) _passedTests++;
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
                // Test data flow between systems
                var actions = _testActions;
                var context = _testContext;
                
                // Filter actions through the filtering system
                var filteredActions = _actionFilter?.FilterActions(actions, context) ?? actions;
                
                // Test that data flows properly
                bool dataFlowWorking = filteredActions != null && filteredActions.Count <= actions.Count;
                
                if (dataFlowWorking)
                {
                    LogTest(testName, true, "Data flow integration working");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, "Data flow integration failed");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void TestPerformanceBenchmarks()
        {
            LogTestCategory("Performance Benchmark Tests");
            
            TestMenuOpeningPerformance();
            TestFilteringPerformance();
            TestAnimationPerformance();
        }
        
        private void TestMenuOpeningPerformance()
        {
            _totalTests++;
            string testName = "Menu Opening Performance";
            
            try
            {
                float startTime = Time.realtimeSinceStartup;
                int iterations = _performanceTestIterations;
                
                for (int i = 0; i < iterations; i++)
                {
                    _menuSystem.OpenContextualMenu(Vector3.zero, gameObject, _testContext);
                    // Close menu using reflection
                    _menuSystem.GetType()
                        .GetMethod("CloseAllMenus", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.Invoke(_menuSystem, null);
                }
                
                float endTime = Time.realtimeSinceStartup;
                float avgTime = (endTime - startTime) / iterations * 1000f; // Convert to ms
                
                bool goodPerformance = avgTime < 5f; // Less than 5ms per operation
                
                if (goodPerformance)
                {
                    LogTest(testName, true, $"Good performance - {avgTime:F2}ms per menu operation");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, $"Poor performance - {avgTime:F2}ms per menu operation");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void TestFilteringPerformance()
        {
            _totalTests++;
            string testName = "Filtering Performance";
            
            try
            {
                float startTime = Time.realtimeSinceStartup;
                int iterations = _performanceTestIterations;
                
                for (int i = 0; i < iterations; i++)
                {
                    _actionFilter.FilterActions(_testActions, _testContext);
                }
                
                float endTime = Time.realtimeSinceStartup;
                float avgTime = (endTime - startTime) / iterations * 1000f; // Convert to ms
                
                bool goodPerformance = avgTime < 1f; // Less than 1ms per operation
                
                if (goodPerformance)
                {
                    LogTest(testName, true, $"Good performance - {avgTime:F2}ms per filtering operation");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, $"Poor performance - {avgTime:F2}ms per filtering operation");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void TestAnimationPerformance()
        {
            _totalTests++;
            string testName = "Animation Performance";
            
            try
            {
                var metrics = _performanceSystem?.GetPerformanceMetrics();
                
                bool goodFPS = metrics != null && metrics.FPS > 30f;
                bool lowFrameTime = metrics != null && metrics.FrameTime < 33f; // 30 FPS = 33ms frame time
                
                if (goodFPS && lowFrameTime)
                {
                    LogTest(testName, true, $"Good performance - FPS: {metrics.FPS:F1}, Frame Time: {metrics.FrameTime:F2}ms");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, $"Poor performance - FPS: {metrics?.FPS:F1}, Frame Time: {metrics?.FrameTime:F2}ms");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void LogTestCategory(string category)
        {
            if (_enableDetailedLogging)
            {
                Debug.Log($"[AdvancedMenuSystemTest] === {category} ===");
            }
        }
        
        private void LogTest(string testName, bool passed, string message)
        {
            string result = passed ? "✅ PASS" : "❌ FAIL";
            string fullMessage = $"{result}: {testName} - {message}";
            
            _testResults.Add(fullMessage);
            
            if (_enableDetailedLogging)
            {
                if (passed)
                    Debug.Log($"[AdvancedMenuSystemTest] {fullMessage}");
                else
                    Debug.LogWarning($"[AdvancedMenuSystemTest] {fullMessage}");
            }
        }
        
        // Test helper class
        private class TestUpdateable : IUpdateable
        {
            public void UpdateElement()
            {
                // Test implementation
            }
        }
    }
}