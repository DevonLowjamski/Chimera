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
    /// Core functionality tests for DIGameManager.
    /// Tests basic initialization, component access, and fundamental operations.
    /// </summary>
    public class DIGameManagerCoreTests : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool _runTestsOnStart = false;
        [SerializeField] private bool _enableTestLogging = true;

        // Test state
        private List<ValidationResult> _testResults = new List<ValidationResult>();
        private DIGameManager _gameManager;
        private ProjectChimera.Core.ManagerRegistry _managerRegistry;
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
                StartCoroutine(RunCoreTests());
            }
        }

        /// <summary>
        /// Run all core functionality tests for DIGameManager
        /// </summary>
        public IEnumerator RunCoreTests()
        {
            if (_testsRunning) yield break;

            _testsRunning = true;
            _testResults.Clear();

            LogTest("=== Starting DIGameManager Core Tests ===");

            // Test 1: Component initialization
            yield return StartCoroutine(TestComponentInitialization());

            // Test 2: Basic functionality access
            yield return StartCoroutine(TestBasicFunctionalityAccess());

            // Test 3: Singleton pattern enforcement
            yield return StartCoroutine(TestSingletonPatternEnforcement());

            // Test 4: Game state management
            yield return StartCoroutine(TestGameStateManagement());

            // Generate test summary
            GenerateTestSummary();

            _testsRunning = false;
            OnTestsCompleted?.Invoke(_testResults);
            LogTest("=== DIGameManager Core Tests Completed ===");
        }

        /// <summary>
        /// Test component initialization and access
        /// </summary>
        private IEnumerator TestComponentInitialization()
        {
            LogTest("Testing component initialization...");

            var result = new ValidationResult { ValidationName = "Component Initialization" };

            try
            {
                // Find DIGameManager - try multiple resolution methods
                _gameManager = DIGameManager.Instance ?? ServiceContainerFactory.Instance?.TryResolve<DIGameManager>();
                if (_gameManager == null)
                {
                    result.AddError("DIGameManager not found in scene");
                    _testResults.Add(result);
                    yield break;
                }

                // Test manager registry access
                _managerRegistry = _gameManager.ManagerRegistry;
                if (_managerRegistry == null)
                {
                    result.AddError("ManagerRegistry not accessible");
                }
                else
                {
                    LogTest($"ManagerRegistry accessed successfully, has {_managerRegistry.RegisteredManagerCount} managers");
                }

                // Test health monitor access
                _healthMonitor = _gameManager.HealthMonitor;
                if (_healthMonitor == null)
                {
                    result.AddError("ServiceHealthMonitor not accessible");
                }
                else
                {
                    LogTest($"ServiceHealthMonitor accessed successfully, monitoring: {_healthMonitor.IsMonitoring}");
                }

                // Test service container access
                var serviceContainer = _gameManager.GlobalServiceContainer;
                if (serviceContainer == null)
                {
                    result.AddError("GlobalServiceContainer not accessible");
                }
                else
                {
                    LogTest("GlobalServiceContainer accessed successfully");
                }

                result.Success = result.Errors.Count == 0;
                LogTest($"Component initialization: {(result.Success ? "PASSED" : "FAILED")}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.AddError($"Exception during initialization test: {ex.Message}");
            }

            _testResults.Add(result);

            // Move yield statement outside try-catch block
            yield return new WaitForSeconds(0.1f);
        }

        /// <summary>
        /// Test basic functionality access
        /// </summary>
        private IEnumerator TestBasicFunctionalityAccess()
        {
            LogTest("Testing basic functionality access...");

            var result = new ValidationResult { ValidationName = "Basic Functionality Access" };

            try
            {
                // Test GetManager functionality
                var gameManagerFromSelf = _gameManager.GetManager<DIGameManager>();
                if (gameManagerFromSelf == null)
                {
                    result.AddError("GetManager<DIGameManager>() returned null");
                }
                else if (gameManagerFromSelf != _gameManager)
                {
                    result.AddError("GetManager<DIGameManager>() returned wrong instance");
                }

                // Test GetAllManagers functionality
                var allManagers = _gameManager.GetAllManagers();
                if (allManagers == null)
                {
                    result.AddError("GetAllManagers() returned null");
                }
                else
                {
                    var managersList = new List<IChimeraManager>(allManagers);
                    LogTest($"GetAllManagers returned {managersList.Count} managers");

                    // Check if DIGameManager is in the list
                    bool foundSelf = false;
                    foreach (var manager in managersList)
                    {
                        if (manager == _gameManager)
                        {
                            foundSelf = true;
                            break;
                        }
                    }

                    if (!foundSelf)
                    {
                        result.AddWarning("DIGameManager not found in GetAllManagers result");
                    }
                }

                // Test basic properties
                var gameStartTime = _gameManager.GameStartTime;
                if (gameStartTime == default(DateTime))
                {
                    result.AddWarning("GameStartTime not initialized");
                }

                var totalGameTime = _gameManager.TotalGameTime;
                if (totalGameTime.TotalSeconds < 0)
                {
                    result.AddError("TotalGameTime calculation failed");
                }

                var currentState = _gameManager.CurrentGameState;
                LogTest($"Current game state: {currentState}");

                result.Success = result.Errors.Count == 0;
                LogTest($"Basic functionality access: {(result.Success ? "PASSED" : "FAILED")}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.AddError($"Exception during basic functionality test: {ex.Message}");
            }

            _testResults.Add(result);

            // Move yield statement outside try-catch block
            yield return new WaitForSeconds(0.1f);
        }

        /// <summary>
        /// Test singleton pattern enforcement
        /// </summary>
        private IEnumerator TestSingletonPatternEnforcement()
        {
            LogTest("Testing singleton pattern enforcement...");

            var result = new ValidationResult { ValidationName = "Singleton Pattern Enforcement" };

            try
            {
                // Test that Instance returns same object
                var instance1 = DIGameManager.Instance;
                var instance2 = DIGameManager.Instance;

                if (instance1 != instance2)
                {
                    result.AddError("Singleton Instance property returns different objects");
                }

                if (instance1 != _gameManager)
                {
                    result.AddError("Singleton Instance does not match discovered DIGameManager");
                }

                // Test that only one DIGameManager exists in scene
                // Primary: Try ServiceContainer resolution for registered managers
                var allGameManagers = ServiceContainerFactory.Instance.ResolveAll<DIGameManager>();
                if (allGameManagers?.Any() != true)
                {
                    // Fallback: Scene discovery for testing validation
                    allGameManagers = UnityEngine.Object.FindObjectsOfType<DIGameManager>();
                }
                if (allGameManagers.Length > 1)
                {
                    result.AddError($"Multiple DIGameManager instances found in scene: {allGameManagers.Length}");
                }
                else if (allGameManagers.Length == 0)
                {
                    result.AddError("No DIGameManager instances found in scene");
                }
                else if (allGameManagers[0] != _gameManager)
                {
                    result.AddError("Found DIGameManager instance does not match expected instance");
                }

                result.Success = result.Errors.Count == 0;
                LogTest($"Singleton pattern enforcement: {(result.Success ? "PASSED" : "FAILED")}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.AddError($"Exception during singleton test: {ex.Message}");
            }

            _testResults.Add(result);

            // Move yield statement outside try-catch block
            yield return new WaitForSeconds(0.1f);
        }

        /// <summary>
        /// Test game state management
        /// </summary>
        private IEnumerator TestGameStateManagement()
        {
            LogTest("Testing game state management...");

            var result = new ValidationResult { ValidationName = "Game State Management" };

            bool pauseTestSuccessful = false;
            bool resumeTestSuccessful = false;

            try
            {
                // Test initial state
                var initialState = _gameManager.CurrentGameState;
                var initialPauseState = _gameManager.IsGamePaused;
                LogTest($"Initial game state: {initialState}, paused: {initialPauseState}");

                // Test pause functionality
                _gameManager.PauseGame();
                pauseTestSuccessful = true;

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

                if (_gameManager.IsGamePaused)
                {
                    result.AddError("Game resume functionality failed - IsGamePaused still true");
                }
                else
                {
                    LogTest("Game resume successful");
                }

                // Test time tracking
                var gameStartTime = _gameManager.GameStartTime;
                var totalGameTime = _gameManager.TotalGameTime;

                LogTest($"Game start time: {gameStartTime}");
                LogTest($"Total game time: {totalGameTime.TotalSeconds:F2} seconds");

                if (gameStartTime == default(DateTime))
                {
                    result.AddWarning("Game start time not properly initialized");
                }

                if (totalGameTime.TotalSeconds < 0)
                {
                    result.AddError("Total game time is negative");
                }

                result.Success = result.Errors.Count == 0;
                LogTest($"Game state management: {(result.Success ? "PASSED" : "FAILED")}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.AddError($"Exception during game state test: {ex.Message}");

                // Try to restore game state on exception
                if (pauseTestSuccessful && !resumeTestSuccessful)
                {
                    try
                    {
                        _gameManager.ResumeGame();
                    }
                    catch
                    {
                        // Ignore resume errors in cleanup
                    }
                }
            }

            _testResults.Add(result);

            // Move yield statement outside try-catch block
            yield return new WaitForSeconds(0.1f);
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

            LogTest("\n=== DIGameManager Core Tests Summary ===");

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
                LogTest("🎉 All DIGameManager core tests PASSED!");
            }
            else
            {
                LogTest($"❌ {failed} test(s) FAILED - Review errors above");
            }
        }

        private void LogTest(string message)
        {
            if (_enableTestLogging)
                ChimeraLogger.Log($"[DIGameManagerCoreTests] {message}");
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
