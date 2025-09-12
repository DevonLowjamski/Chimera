using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Camera.Tests
{
    /// <summary>
    /// BASIC: Simple camera testing for Project Chimera.
    /// Focuses on essential camera functionality validation without comprehensive test suites.
    /// </summary>
    public class AdvancedCameraControllerTest : MonoBehaviour
    {
        [Header("Basic Test Settings")]
        [SerializeField] private bool _runBasicTestsOnStart = false;
        [SerializeField] private bool _enableLogging = true;

        // Basic test results
        private int _testsRun = 0;
        private int _testsPassed = 0;
        private int _testsFailed = 0;

        /// <summary>
        /// Run basic camera tests
        /// </summary>
        public void RunBasicTests()
        {
            if (_enableLogging)
            {
                ChimeraLogger.Log("[CameraTest] Starting basic camera tests...");
            }

            ResetTestResults();

            // Test camera existence
            TestCameraExists();

            // Test camera positioning
            TestCameraPositioning();

            // Test camera movement
            TestCameraMovement();

            // Print results
            PrintTestSummary();
        }

        private void TestCameraExists()
        {
            _testsRun++;
            var mainCamera = Camera.main;

            if (mainCamera != null)
            {
                _testsPassed++;
                if (_enableLogging)
                {
                    ChimeraLogger.Log("[CameraTest] ✓ Camera exists");
                }
            }
            else
            {
                _testsFailed++;
                if (_enableLogging)
                {
                    ChimeraLogger.Log("[CameraTest] ✗ No main camera found");
                }
            }
        }

        private void TestCameraPositioning()
        {
            _testsRun++;
            var mainCamera = Camera.main;

            if (mainCamera != null)
            {
                // Basic position check
                Vector3 position = mainCamera.transform.position;

                // Check if camera is at a reasonable position (not at origin)
                if (position != Vector3.zero)
                {
                    _testsPassed++;
                    if (_enableLogging)
                    {
                        ChimeraLogger.Log($"[CameraTest] ✓ Camera positioned at {position}");
                    }
                }
                else
                {
                    _testsFailed++;
                    if (_enableLogging)
                    {
                        ChimeraLogger.Log("[CameraTest] ✗ Camera at origin position");
                    }
                }
            }
            else
            {
                _testsFailed++;
                if (_enableLogging)
                {
                    ChimeraLogger.Log("[CameraTest] ✗ Cannot test positioning - no camera");
                }
            }
        }

        private void TestCameraMovement()
        {
            _testsRun++;
            var mainCamera = Camera.main;

            if (mainCamera != null)
            {
                // Store initial position
                Vector3 initialPosition = mainCamera.transform.position;

                // Try to move camera slightly
                mainCamera.transform.Translate(Vector3.forward * 0.1f);

                // Check if movement worked
                Vector3 newPosition = mainCamera.transform.position;

                if (newPosition != initialPosition)
                {
                    _testsPassed++;
                    if (_enableLogging)
                    {
                        ChimeraLogger.Log("[CameraTest] ✓ Camera movement works");
                    }

                    // Move back to original position
                    mainCamera.transform.position = initialPosition;
                }
                else
                {
                    _testsFailed++;
                    if (_enableLogging)
                    {
                        ChimeraLogger.Log("[CameraTest] ✗ Camera movement failed");
                    }
                }
            }
            else
            {
                _testsFailed++;
                if (_enableLogging)
                {
                    ChimeraLogger.Log("[CameraTest] ✗ Cannot test movement - no camera");
                }
            }
        }

        private void ResetTestResults()
        {
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
        }

        private void PrintTestSummary()
        {
            if (_enableLogging)
            {
                ChimeraLogger.Log($"[CameraTest] Test Summary: {_testsRun} run, {_testsPassed} passed, {_testsFailed} failed");

                if (_testsFailed == 0)
                {
                    ChimeraLogger.Log("[CameraTest] ✓ All tests passed!");
                }
                else
                {
                    ChimeraLogger.Log($"[CameraTest] ⚠ {_testsFailed} test(s) failed");
                }
            }
        }

        /// <summary>
        /// Get test results
        /// </summary>
        public TestResults GetTestResults()
        {
            return new TestResults
            {
                TestsRun = _testsRun,
                TestsPassed = _testsPassed,
                TestsFailed = _testsFailed,
                SuccessRate = _testsRun > 0 ? (float)_testsPassed / _testsRun : 0f
            };
        }

        /// <summary>
        /// Auto-run tests on start if enabled
        /// </summary>
        private void Start()
        {
            if (_runBasicTestsOnStart)
            {
                RunBasicTests();
            }
        }
    }

    /// <summary>
    /// Test results data
    /// </summary>
    [System.Serializable]
    public struct TestResults
    {
        public int TestsRun;
        public int TestsPassed;
        public int TestsFailed;
        public float SuccessRate;
    }
}
