using ProjectChimera.Core.Logging;
using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core;
using ProjectChimera.Systems.Camera;
using ProjectChimera.Data.Camera;

namespace ProjectChimera.Testing.Systems.Camera
{
    /// <summary>
    /// Comprehensive test suite for the refactored AdvancedCameraController orchestrator.
    /// Tests component delegation, event wiring, camera level management, and constraint handling.
    /// Part of the monolithic controller refactoring validation process.
    /// </summary>
    public class AdvancedCameraControllerTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool _runTestsOnStart = false;
        [SerializeField] private bool _enableDebugOutput = true;
        [SerializeField] private bool _testTransitionFunctionality = true;
        [SerializeField] private bool _testCameraLevels = true;
        [SerializeField] private bool _testConstraintHandling = true;
        
        [Header("Test Results")]
        [SerializeField] private int _testsRun = 0;
        [SerializeField] private int _testsPassed = 0;
        [SerializeField] private int _testsFailed = 0;
        [SerializeField] private List<string> _failedTests = new List<string>();
        
        // Component references
        private AdvancedCameraController _controller;
        private UnityEngine.Camera _testCamera;
        
        // Test state
        private bool _eventTriggered = false;
        private Transform _lastFocusTarget;
        private bool _transitionCompleted = false;
        
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
            LogDebug("=== Starting AdvancedCameraController Test Suite ===");
            ResetTestResults();
            
            // Setup test environment
            SetupTestEnvironment();
            
            // Core functionality tests
            TestComponentInitialization();
            TestOrchestratorAPI();
            TestEventWiring();
            
            if (_testCameraLevels)
            {
                TestCameraLevelSystem();
            }
            
            if (_testTransitionFunctionality)
            {
                TestTransitionFunctionality();
            }
            
            if (_testConstraintHandling)
            {
                TestConstraintHandling();
            }
            
            // Camera specific tests
            TestFocusAndTargeting();
            TestInputHandling();
            TestAnchorSystem();
            TestConfigurationManagement();
            
            // Summary
            PrintTestSummary();
        }
        
        private void SetupTestEnvironment()
        {
            LogDebug("Setting up test environment...");
            
            _controller = ServiceContainerFactory.Instance?.TryResolve<AdvancedCameraController>();
            _testCamera = ServiceContainerFactory.Instance?.TryResolve<UnityEngine.Camera>() ?? UnityEngine.Camera.main ?? ServiceContainerFactory.Instance?.TryResolve<UnityEngine.Camera>();
            
            if (_controller == null)
            {
                LogDebug("AdvancedCameraController not found - creating test instance");
                var controllerGO = new GameObject("Test_AdvancedCameraController");
                _controller = controllerGO.AddComponent<AdvancedCameraController>();
            }
            
            if (_testCamera == null)
            {
                LogDebug("Camera not found - creating test camera");
                var cameraGO = new GameObject("Test_Camera");
                _testCamera = cameraGO.AddComponent<UnityEngine.Camera>();
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
            RunTest("Public Properties Access", () =>
            {
                // Test that key public properties are accessible without errors
                var focusTarget = _controller.FocusTarget;
                var isTransitioning = _controller.IsTransitioning;
                var userControlActive = _controller.UserControlActive;
                var currentLevelAnchor = _controller.CurrentLevelAnchor;
                var isLevelTransitioning = _controller.IsLevelTransitioning;
                var position = _controller.Position;
                var fieldOfView = _controller.FieldOfView;
                var hasFocus = _controller.HasFocus;
                var distanceToFocus = _controller.DistanceToFocus;
                
                return true; // If we get here without exceptions, API is accessible
            });
            
            RunTest("Event System Properties", () =>
            {
                return _controller.OnFocusTargetChanged != null ||
                       _controller.OnCinematicModeChanged != null ||
                       _controller.OnTargetHover != null ||
                       _controller.OnLevelAnchorChanged != null;
            });
        }
        
        private void TestEventWiring()
        {
            RunTest("Event Subscription", () =>
            {
                try
                {
                    _controller.OnFocusTargetChanged += HandleFocusTargetChanged;
                    _controller.OnTargetHover += HandleTargetHover;
                    _controller.OnLevelAnchorChanged += HandleLevelAnchorChanged;
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
                    _controller.OnFocusTargetChanged -= HandleFocusTargetChanged;
                    _controller.OnTargetHover -= HandleTargetHover;
                    _controller.OnLevelAnchorChanged -= HandleLevelAnchorChanged;
                    return true;
                }
                catch (System.Exception ex)
                {
                    LogDebug($"Event cleanup failed: {ex.Message}");
                    return false;
                }
            });
        }
        
        private void TestCameraLevelSystem()
        {
            RunTest("Camera Level Validation", () =>
            {
                var levels = new CameraLevel[] 
                {
                    CameraLevel.Plant,
                    CameraLevel.Bench, 
                    CameraLevel.Room,
                    CameraLevel.Facility
                };
                
                foreach (var level in levels)
                {
                    if (!_controller.IsValidLevel(level))
                        return false;
                }
                
                return true;
            });
            
            RunTest("Level Distance and Height", () =>
            {
                try
                {
                    var plantDistance = _controller.GetLevelDistance(CameraLevel.Plant);
                    var plantHeight = _controller.GetLevelHeight(CameraLevel.Plant);
                    var facilityDistance = _controller.GetLevelDistance(CameraLevel.Facility);
                    var facilityHeight = _controller.GetLevelHeight(CameraLevel.Facility);
                    
                    // Expect reasonable values
                    return plantDistance > 0 && plantHeight > 0 && 
                           facilityDistance > plantDistance && facilityHeight > plantHeight;
                }
                catch (System.Exception ex)
                {
                    LogDebug($"Level distance/height test failed: {ex.Message}");
                    return false;
                }
            });
            
            RunTest("Level Transitions", () =>
            {
                try
                {
                    var distance = _controller.GetLevelDistance(CameraLevel.Plant, CameraLevel.Facility);
                    var duration = _controller.GetOptimalTransitionDuration(CameraLevel.Plant, CameraLevel.Facility);
                    var isValidTransition = _controller.IsValidLevelTransition(CameraLevel.Plant, CameraLevel.Facility);
                    
                    return distance >= 0 && duration > 0 && isValidTransition;
                }
                catch (System.Exception ex)
                {
                    LogDebug($"Level transition test failed: {ex.Message}");
                    return false;
                }
            });
            
            RunTest("Zoom Operations", () =>
            {
                try
                {
                    var zoomOutResult = _controller.ZoomOutOneLevel();
                    var zoomToResult = _controller.ZoomTo(CameraLevel.Room);
                    
                    return true; // If no exceptions, zoom operations work
                }
                catch (System.Exception ex)
                {
                    LogDebug($"Zoom operations test failed: {ex.Message}");
                    return false;
                }
            });
        }
        
        private void TestTransitionFunctionality()
        {
            RunTest("Movement Operations", () =>
            {
                try
                {
                    _controller.MoveCameraToPosition(Vector3.zero, Quaternion.identity, 1f);
                    _controller.OrbitAroundTarget(45f, 30f, 2f);
                    return true;
                }
                catch (System.Exception ex)
                {
                    LogDebug($"Movement operations test failed: {ex.Message}");
                    return false;
                }
            });
            
            RunTest("Focus Operations", () =>
            {
                try
                {
                    var testGO = new GameObject("TestFocusTarget");
                    var testTransform = testGO.transform;
                    testTransform.position = Vector3.forward * 5f;
                    
                    var focusResult = _controller.FocusOnTarget(testTransform);
                    var positionFocusResult = _controller.FocusOnPosition(Vector3.up * 3f);
                    var nearestFocusResult = _controller.FocusOnNearestTarget();
                    
                    _controller.ClearFocus();
                    
                    DestroyImmediate(testGO);
                    return true; // If no exceptions thrown
                }
                catch (System.Exception ex)
                {
                    LogDebug($"Focus operations test failed: {ex.Message}");
                    return false;
                }
            });
            
            RunTest("Cinematic and Effects", () =>
            {
                try
                {
                    _controller.ShakeCamera(1.5f, 1f);
                    // Note: CinematicSequence would need to be defined to test PlayCinematicSequence
                    return true;
                }
                catch (System.Exception ex)
                {
                    LogDebug($"Cinematic and effects test failed: {ex.Message}");
                    return false;
                }
            });
        }
        
        private void TestConstraintHandling()
        {
            RunTest("Camera Bounds", () =>
            {
                try
                {
                    _controller.SetCameraBounds(new Vector3(-10, 0, -10), new Vector3(10, 20, 10));
                    return true;
                }
                catch (System.Exception ex)
                {
                    LogDebug($"Camera bounds test failed: {ex.Message}");
                    return false;
                }
            });
            
            RunTest("Movement Smoothing", () =>
            {
                try
                {
                    _controller.SetMovementSmoothing(true, 0.2f, 0.15f);
                    _controller.SetMovementSmoothing(false);
                    return true;
                }
                catch (System.Exception ex)
                {
                    LogDebug($"Movement smoothing test failed: {ex.Message}");
                    return false;
                }
            });
            
            RunTest("User Control Management", () =>
            {
                try
                {
                    var originalState = _controller.UserControlActive;
                    _controller.SetUserControlEnabled(false);
                    _controller.SetUserControlEnabled(true);
                    return true;
                }
                catch (System.Exception ex)
                {
                    LogDebug($"User control management test failed: {ex.Message}");
                    return false;
                }
            });
        }
        
        private void TestFocusAndTargeting()
        {
            RunTest("Target Detection", () =>
            {
                try
                {
                    var ray = _controller.GetCameraRay(Vector2.one * 100f);
                    var target = _controller.GetTargetAtScreenPosition(Vector2.one * 100f);
                    var focusAtScreen = _controller.FocusOnTargetAtScreenPosition(Vector2.one * 100f);
                    
                    return true; // If no exceptions
                }
                catch (System.Exception ex)
                {
                    LogDebug($"Target detection test failed: {ex.Message}");
                    return false;
                }
            });
            
            RunTest("Click to Focus", () =>
            {
                try
                {
                    _controller.SetClickToFocusEnabled(true);
                    _controller.SetClickToFocusEnabled(false);
                    return true;
                }
                catch (System.Exception ex)
                {
                    LogDebug($"Click to focus test failed: {ex.Message}");
                    return false;
                }
            });
        }
        
        private void TestInputHandling()
        {
            RunTest("Keyboard Shortcuts", () =>
            {
                try
                {
                    var shortcuts = _controller.GetKeyboardShortcuts();
                    return shortcuts != null && shortcuts.Count > 0;
                }
                catch (System.Exception ex)
                {
                    LogDebug($"Keyboard shortcuts test failed: {ex.Message}");
                    return false;
                }
            });
        }
        
        private void TestAnchorSystem()
        {
            RunTest("Anchor Operations", () =>
            {
                try
                {
                    var testGO = new GameObject("TestAnchor");
                    var testTransform = testGO.transform;
                    
                    var suggestedDistance = _controller.GetSuggestedDistanceForTarget(testTransform);
                    var canFocus = _controller.CanFocusOnTarget(testTransform);
                    var hasAnchor = _controller.HasValidAnchor(testTransform);
                    var anchor = _controller.GetLogicalAnchor(testTransform);
                    var level = _controller.GetTargetCameraLevel(testTransform);
                    var transitionInfo = _controller.GetTargetTransitionInfo(testTransform);
                    var focusWithAnchor = _controller.FocusOnTargetWithAnchor(testTransform);
                    
                    _controller.RefreshAnchorMappings();
                    var anchors = _controller.GetAnchorsForLevel(CameraLevel.Facility);
                    
                    DestroyImmediate(testGO);
                    return suggestedDistance > 0 && anchors != null;
                }
                catch (System.Exception ex)
                {
                    LogDebug($"Anchor operations test failed: {ex.Message}");
                    return false;
                }
            });
        }
        
        private void TestConfigurationManagement()
        {
            RunTest("Level Information", () =>
            {
                try
                {
                    var semanticName = _controller.GetLevelSemanticName(CameraLevel.Plant);
                    var description = _controller.GetLevelDescription(CameraLevel.Plant);
                    var fov = _controller.GetLevelFieldOfView(CameraLevel.Plant);
                    var speed = _controller.GetLevelTransitionSpeed(CameraLevel.Plant);
                    var available = _controller.IsLevelAvailable(CameraLevel.Plant);
                    
                    return !string.IsNullOrEmpty(semanticName) && !string.IsNullOrEmpty(description) && 
                           fov > 0 && speed > 0;
                }
                catch (System.Exception ex)
                {
                    LogDebug($"Level information test failed: {ex.Message}");
                    return false;
                }
            });
            
            RunTest("Event Configuration", () =>
            {
                try
                {
                    var eventConfig = _controller.GetEventConfiguration();
                    _controller.SetLevelChangeEventConfiguration(null, false);
                    return true;
                }
                catch (System.Exception ex)
                {
                    LogDebug($"Event configuration test failed: {ex.Message}");
                    return false;
                }
            });
            
            RunTest("Global Position Offset", () =>
            {
                try
                {
                    var originalPos = Vector3.one;
                    var offsetPos = _controller.ApplyGlobalPositionOffset(originalPos);
                    return offsetPos != Vector3.zero; // Should return some position
                }
                catch (System.Exception ex)
                {
                    LogDebug($"Global position offset test failed: {ex.Message}");
                    return false;
                }
            });
        }
        
        // Event handlers for testing
        private void HandleFocusTargetChanged(Transform target)
        {
            _eventTriggered = true;
            _lastFocusTarget = target;
            LogDebug($"Focus target changed event: {target?.name}");
        }
        
        private void HandleTargetHover(Transform target)
        {
            _eventTriggered = true;
            LogDebug($"Target hover event: {target?.name}");
        }
        
        private void HandleLevelAnchorChanged(Transform anchor)
        {
            _eventTriggered = true;
            LogDebug($"Level anchor changed event: {anchor?.name}");
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
            _lastFocusTarget = null;
            _transitionCompleted = false;
        }
        
        private void PrintTestSummary()
        {
            LogDebug("=== AdvancedCameraController Test Summary ===");
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
                ChimeraLogger.Log($"[AdvancedCameraControllerTest] {message}");
            }
        }
    }
}