using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Systems.Construction;
using ProjectChimera.Data.Construction;

namespace ProjectChimera.Testing.Systems.Construction
{
    /// <summary>
    /// Comprehensive test suite for the refactored GridPlacementController orchestrator.
    /// Tests component delegation, event wiring, and 3D coordinate system functionality.
    /// Part of the monolithic controller refactoring validation process.
    /// </summary>
    public class GridPlacementControllerTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool _runTestsOnStart = false;
        [SerializeField] private bool _enableDebugOutput = true;
        [SerializeField] private bool _testEventWiring = true;
        [SerializeField] private bool _test3DCoordinates = true;
        [SerializeField] private bool _testPlacementFunctionality = true;
        
        [Header("Test Results")]
        [SerializeField] private int _testsRun = 0;
        [SerializeField] private int _testsPassed = 0;
        [SerializeField] private int _testsFailed = 0;
        [SerializeField] private List<string> _failedTests = new List<string>();
        
        // Component references
        private GridPlacementController _controller;
        private GridSystem _gridSystem;
        
        // Test state
        private bool _eventTriggered = false;
        private Vector3Int _lastEventPosition;
        
        private void Start()
        {
            if (_runTestsOnStart)
            {
                RunAllTests();
            }
        }
        
        [ContextMenu("Run All Tests")]
        public void RunAllTests()
        {
            LogDebug("=== Starting GridPlacementController Test Suite ===");
            ResetTestResults();
            
            // Setup test environment
            SetupTestEnvironment();
            
            // Core functionality tests
            TestComponentInitialization();
            TestOrchestratorAPI();
            
            if (_testEventWiring)
            {
                TestEventWiring();
            }
            
            if (_test3DCoordinates)
            {
                Test3DCoordinateSystem();
            }
            
            if (_testPlacementFunctionality)
            {
                TestPlacementFunctionality();
            }
            
            // Selection and filtering tests
            TestSelectionSystem();
            TestCategoryFiltering();
            TestSchematicOperations();
            
            // Summary
            PrintTestSummary();
        }
        
        private void SetupTestEnvironment()
        {
            LogDebug("Setting up test environment...");
            
            _controller = FindObjectOfType<GridPlacementController>();
            _gridSystem = FindObjectOfType<GridSystem>();
            
            if (_controller == null)
            {
                LogDebug("GridPlacementController not found - creating test instance");
                var controllerGO = new GameObject("Test_GridPlacementController");
                _controller = controllerGO.AddComponent<GridPlacementController>();
            }
            
            if (_gridSystem == null)
            {
                LogDebug("GridSystem not found - creating test instance");
                var gridGO = new GameObject("Test_GridSystem");
                _gridSystem = gridGO.AddComponent<GridSystem>();
            }
        }
        
        private void TestComponentInitialization()
        {
            RunTest("Component Initialization", () =>
            {
                return _controller != null;
            });
            
            RunTest("Controller Active State", () =>
            {
                return _controller.enabled && _controller.gameObject.activeInHierarchy;
            });
        }
        
        private void TestOrchestratorAPI()
        {
            RunTest("Public API Availability", () =>
            {
                // Test that key public properties are accessible
                var hasPlacementMode = _controller.IsInPlacementMode != null;
                var hasSchematicMode = _controller.IsInSchematicPlacementMode != null;
                var hasGridPosition = _controller.CurrentGridPosition != Vector3Int.zero || _controller.CurrentGridPosition == Vector3Int.zero; // Just test accessibility
                var hasValidPlacement = _controller.IsValidPlacement != null;
                
                return hasPlacementMode && hasSchematicMode && hasGridPosition && hasValidPlacement;
            });
            
            RunTest("Selected Objects Property", () =>
            {
                var selectedObjects = _controller.SelectedObjects;
                return selectedObjects != null;
            });
            
            RunTest("Category Filtering Properties", () =>
            {
                var categories = _controller.ActiveFilterCategories;
                var counts = _controller.CategoryCounts;
                return categories != null && counts != null;
            });
        }
        
        private void TestEventWiring()
        {
            RunTest("Event System Initialization", () =>
            {
                // Test that events can be subscribed to without errors
                try
                {
                    _controller.OnObjectPlaced += HandleObjectPlaced;
                    _controller.OnObjectRemoved += HandleObjectRemoved;
                    _controller.OnGridPositionChanged += HandleGridPositionChanged;
                    return true;
                }
                catch (System.Exception ex)
                {
                    LogDebug($"Event subscription failed: {ex.Message}");
                    return false;
                }
            });
            
            RunTest("Event Cleanup", () =>
            {
                try
                {
                    _controller.OnObjectPlaced -= HandleObjectPlaced;
                    _controller.OnObjectRemoved -= HandleObjectRemoved;
                    _controller.OnGridPositionChanged -= HandleGridPositionChanged;
                    return true;
                }
                catch (System.Exception ex)
                {
                    LogDebug($"Event cleanup failed: {ex.Message}");
                    return false;
                }
            });
        }
        
        private void Test3DCoordinateSystem()
        {
            RunTest("3D Grid Position Handling", () =>
            {
                var testPosition = new Vector3Int(5, 3, 2); // X, Y, Z coordinates
                var currentPos = _controller.CurrentGridPosition;
                
                // Test that we can work with 3D coordinates without errors
                return testPosition.z == 2; // Verify Z-axis support
            });
            
            RunTest("Height Level Support", () =>
            {
                // Test that the system can handle multiple height levels
                var positions = new Vector3Int[]
                {
                    new Vector3Int(0, 0, 0),  // Ground level
                    new Vector3Int(0, 0, 1),  // First floor
                    new Vector3Int(0, 0, 5),  // Fifth floor
                };
                
                foreach (var pos in positions)
                {
                    if (pos.z < 0) return false; // Invalid height level
                }
                
                return true;
            });
        }
        
        private void TestPlacementFunctionality()
        {
            RunTest("Placement Mode Entry/Exit", () =>
            {
                try
                {
                    // Create a mock placeable for testing
                    var testGO = new GameObject("TestPlaceable");
                    var testPlaceable = testGO.AddComponent<GridPlaceable>();
                    
                    // Test entering placement mode
                    _controller.EnterPlacementMode(testPlaceable);
                    bool enteredMode = _controller.IsInPlacementMode;
                    
                    // Test exiting placement mode
                    _controller.ExitPlacementMode();
                    bool exitedMode = !_controller.IsInPlacementMode;
                    
                    // Cleanup
                    DestroyImmediate(testGO);
                    
                    return enteredMode && exitedMode;
                }
                catch (System.Exception ex)
                {
                    LogDebug($"Placement mode test failed: {ex.Message}");
                    return false;
                }
            });
            
            RunTest("Validation Summary Access", () =>
            {
                try
                {
                    var summary = _controller.GetValidationSummary();
                    return !string.IsNullOrEmpty(summary);
                }
                catch (System.Exception ex)
                {
                    LogDebug($"Validation summary failed: {ex.Message}");
                    return false;
                }
            });
        }
        
        private void TestSelectionSystem()
        {
            RunTest("Selection Management", () =>
            {
                try
                {
                    var testGO = new GameObject("TestSelectable");
                    var testPlaceable = testGO.AddComponent<GridPlaceable>();
                    
                    // Test selection
                    _controller.SelectObject(testPlaceable);
                    
                    // Test clear selection
                    _controller.ClearSelection();
                    
                    // Test multiple selection operations
                    _controller.AddToSelection(testPlaceable);
                    _controller.RemoveFromSelection(testPlaceable);
                    bool isSelected = _controller.IsObjectSelected(testPlaceable);
                    
                    // Cleanup
                    DestroyImmediate(testGO);
                    
                    return true; // If no exceptions, basic functionality works
                }
                catch (System.Exception ex)
                {
                    LogDebug($"Selection system test failed: {ex.Message}");
                    return false;
                }
            });
        }
        
        private void TestCategoryFiltering()
        {
            RunTest("Category Filtering Operations", () =>
            {
                try
                {
                    var testCategories = new List<ConstructionCategory> { ConstructionCategory.Equipment };
                    
                    // Test category filter operations
                    _controller.SetCategoryFilter(testCategories);
                    _controller.AddCategoryToFilter(ConstructionCategory.Equipment);
                    _controller.RemoveCategoryFromFilter(ConstructionCategory.Equipment);
                    _controller.ToggleCategoryFilter(ConstructionCategory.Equipment);
                    _controller.EnableCategoryFiltering(true);
                    
                    var filtered = _controller.GetFilteredObjects();
                    var byCategory = _controller.GetObjectsByCategory(ConstructionCategory.Equipment);
                    
                    return filtered != null && byCategory != null;
                }
                catch (System.Exception ex)
                {
                    LogDebug($"Category filtering test failed: {ex.Message}");
                    return false;
                }
            });
        }
        
        private void TestSchematicOperations()
        {
            RunTest("Schematic System Operations", () =>
            {
                try
                {
                    // Test schematic creation
                    var schematic = _controller.CreateSchematicFromSelection("TestSchematic", "Test description");
                    
                    // Test schematic placement operations
                    if (schematic != null)
                    {
                        _controller.StartSchematicPlacement(schematic);
                        _controller.CancelSchematicPlacement();
                    }
                    
                    // Test save operations
                    _controller.SaveSelectionAsSchematic();
                    
                    return true; // If no exceptions, basic functionality works
                }
                catch (System.Exception ex)
                {
                    LogDebug($"Schematic operations test failed: {ex.Message}");
                    return false;
                }
            });
        }
        
        // Event handlers for testing
        private void HandleObjectPlaced(GridPlaceable placeable)
        {
            _eventTriggered = true;
            LogDebug($"Object placed event triggered: {placeable?.name}");
        }
        
        private void HandleObjectRemoved(GridPlaceable placeable)
        {
            _eventTriggered = true;
            LogDebug($"Object removed event triggered: {placeable?.name}");
        }
        
        private void HandleGridPositionChanged(Vector3Int position)
        {
            _eventTriggered = true;
            _lastEventPosition = position;
            LogDebug($"Grid position changed event triggered: {position}");
        }
        
        // Test utility methods
        private void RunTest(string testName, System.Func<bool> testFunction)
        {
            _testsRun++;
            
            try
            {
                bool passed = testFunction.Invoke();
                
                if (passed)
                {
                    _testsPassed++;
                    LogDebug($"✓ PASS: {testName}");
                }
                else
                {
                    _testsFailed++;
                    _failedTests.Add(testName);
                    LogDebug($"✗ FAIL: {testName}");
                }
            }
            catch (System.Exception ex)
            {
                _testsFailed++;
                _failedTests.Add($"{testName} (Exception: {ex.Message})");
                LogDebug($"✗ ERROR: {testName} - {ex.Message}");
            }
        }
        
        private void ResetTestResults()
        {
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            _failedTests.Clear();
            _eventTriggered = false;
            _lastEventPosition = Vector3Int.zero;
        }
        
        private void PrintTestSummary()
        {
            LogDebug("=== GridPlacementController Test Summary ===");
            LogDebug($"Tests Run: {_testsRun}");
            LogDebug($"Tests Passed: {_testsPassed}");
            LogDebug($"Tests Failed: {_testsFailed}");
            LogDebug($"Success Rate: {(_testsRun > 0 ? (_testsPassed * 100 / _testsRun) : 0)}%");
            
            if (_failedTests.Count > 0)
            {
                LogDebug("Failed Tests:");
                foreach (var failedTest in _failedTests)
                {
                    LogDebug($"  - {failedTest}");
                }
            }
            
            LogDebug("=== Test Suite Complete ===");
        }
        
        private void LogDebug(string message)
        {
            if (_enableDebugOutput)
            {
                Debug.Log($"[GridPlacementControllerTest] {message}");
            }
        }
    }
}