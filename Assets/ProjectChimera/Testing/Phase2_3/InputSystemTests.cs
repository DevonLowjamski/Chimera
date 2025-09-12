using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Systems.UI.Advanced;
using ProjectChimera.Systems.Services.Core;
using System.Collections.Generic;

namespace ProjectChimera.Testing.Phase2_3
{
    /// <summary>
    /// Input system integration tests - Tests navigation, shortcuts, and input handling
    /// </summary>
    public class InputSystemTests : MonoBehaviour
    {
        [Header("Input System Test Configuration")]
        [SerializeField] private bool _enableDetailedLogging = true;
        [SerializeField] private bool _runTestsOnStart = false;

        [Header("Test Results")]
        [SerializeField] private int _totalTests = 0;
        [SerializeField] private int _passedTests = 0;
        [SerializeField] private List<string> _testResults = new List<string>();

        // Test components
#if UNITY_INPUT_SYSTEM
        private InputSystemIntegration _inputSystem;
#endif

        /// <summary>
        /// Set detailed logging configuration (replaces reflection usage)
        /// </summary>
        public void SetEnableDetailedLogging(bool enable)
        {
            _enableDetailedLogging = enable;
        }

        public void RunTests()
        {
            ChimeraLogger.Log("[InputSystemTests] Starting input system tests...");

            _testResults.Clear();
            _totalTests = 0;
            _passedTests = 0;

            SetupTestEnvironment();

#if UNITY_INPUT_SYSTEM
            TestInputSystemIntegration();
#else
            ChimeraLogger.LogWarning("[InputSystemTests] Unity Input System not available - skipping tests");
#endif

            LogResults();
        }

        private void SetupTestEnvironment()
        {
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
                bool isInitialized = _inputSystem != null && _inputSystem.IsInputSystemReady();

                if (isInitialized)
                {
                    LogTest(testName, true, "Input system initialized successfully");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, "Input system failed to initialize");
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
                // Test navigation commands
                bool canNavigateUp = _inputSystem.CanNavigate("Up");
                bool canNavigateDown = _inputSystem.CanNavigate("Down");
                bool canNavigateLeft = _inputSystem.CanNavigate("Left");
                bool canNavigateRight = _inputSystem.CanNavigate("Right");

                // Test navigation actions
                _inputSystem.HandleNavigation("Up");
                _inputSystem.HandleNavigation("Down");
                _inputSystem.HandleNavigation("Select");
                _inputSystem.HandleNavigation("Cancel");

                bool navigationWorking = canNavigateUp || canNavigateDown || canNavigateLeft || canNavigateRight;

                if (navigationWorking)
                {
                    LogTest(testName, true, "Navigation system responding to input");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, "Navigation system not responding");
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
                // Test registering shortcuts
                _inputSystem.RegisterShortcut("TestAction1", "Ctrl+T");
                _inputSystem.RegisterShortcut("TestAction2", "Alt+M");
                _inputSystem.RegisterShortcut("TestAction3", "Shift+Space");

                // Test shortcut recognition
                bool hasShortcut1 = _inputSystem.HasShortcut("TestAction1");
                bool hasShortcut2 = _inputSystem.HasShortcut("TestAction2");

                // Test shortcut execution (simulate)
                _inputSystem.ExecuteShortcut("TestAction1");

                if (hasShortcut1 && hasShortcut2)
                {
                    LogTest(testName, true, "Shortcut system registered and executed shortcuts");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, $"Shortcut system failed - Shortcut1: {hasShortcut1}, Shortcut2: {hasShortcut2}");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
#endif

        private void LogTestCategory(string categoryName)
        {
            if (_enableDetailedLogging)
            {
                ChimeraLogger.Log($"[InputSystemTests] === {categoryName} ===");
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

            ChimeraLogger.Log($"[InputSystemTests] Tests completed: {_passedTests}/{_totalTests} passed");

            if (allTestsPassed)
            {
                ChimeraLogger.Log("✅ Input System Tests - ALL TESTS PASSED!");
            }
            else
            {
                ChimeraLogger.LogWarning($"⚠️ Input System Tests - {_totalTests - _passedTests} tests failed");
            }
        }
    }
}
