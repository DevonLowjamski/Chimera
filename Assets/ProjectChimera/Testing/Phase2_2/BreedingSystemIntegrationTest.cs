using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Testing.Phase2_2
{
    /// <summary>
    /// BASIC: Simple breeding system test for Project Chimera.
    /// Focuses on essential breeding functionality validation without complex test suites.
    /// </summary>
    public class BreedingSystemIntegrationTest : MonoBehaviour
    {
        [Header("Basic Test Settings")]
        [SerializeField] private bool _runBasicTestsOnStart = false;
        [SerializeField] private bool _enableLogging = true;

        // Basic test results
        private int _testsRun = 0;
        private int _testsPassed = 0;
        private int _testsFailed = 0;

        /// <summary>
        /// Run basic breeding tests
        /// </summary>
        public void RunBasicTests()
        {
            if (_enableLogging)
            {
                ChimeraLogger.Log("[BreedingTest] Starting basic breeding tests...");
            }

            ResetTestResults();

            // Test basic breeding functionality
            TestBreedingSystemExists();
            TestBasicBreeding();
            TestStrainCreation();

            // Print results
            PrintTestSummary();
        }

        private void TestBreedingSystemExists()
        {
            _testsRun++;
            // Check if basic breeding components exist
            // Primary: Try ServiceContainer resolution
            bool geneticsManagerExists = ServiceContainerFactory.Instance.TryResolve<ProjectChimera.Systems.Genetics.FractalGeneticsEngine>(out var serviceEngine) ||
                                        // Fallback: Scene discovery for testing
                                        GameObject.FindObjectOfType<ProjectChimera.Systems.Genetics.FractalGeneticsEngine>() != null;

            if (geneticsManagerExists)
            {
                _testsPassed++;
                if (_enableLogging)
                {
                    ChimeraLogger.Log("[BreedingTest] ✓ Genetics system found");
                }
            }
            else
            {
                _testsFailed++;
                if (_enableLogging)
                {
                    ChimeraLogger.Log("[BreedingTest] ✗ Genetics system not found");
                }
            }
        }

        private void TestBasicBreeding()
        {
            _testsRun++;
            // Primary: Try ServiceContainer resolution
            if (!ServiceContainerFactory.Instance.TryResolve<ProjectChimera.Systems.Genetics.FractalGeneticsEngine>(out var geneticsEngine))
            {
                // Fallback: Scene discovery for testing
                geneticsEngine = GameObject.FindObjectOfType<ProjectChimera.Systems.Genetics.FractalGeneticsEngine>();
            }

            if (geneticsEngine != null)
            {
                // Test basic strain creation
                geneticsEngine.CreateGeneticData("Test_Strain_1", 20f, 0.5f, 450f);
                geneticsEngine.CreateGeneticData("Test_Strain_2", 18f, 1.0f, 400f);

                // Test breeding
                string offspringId = geneticsEngine.BreedStrains("Test_Strain_1", "Test_Strain_2", "Test_Offspring");

                if (!string.IsNullOrEmpty(offspringId))
                {
                    _testsPassed++;
                    if (_enableLogging)
                    {
                        ChimeraLogger.Log("[BreedingTest] ✓ Basic breeding works");
                    }
                }
                else
                {
                    _testsFailed++;
                    if (_enableLogging)
                    {
                        ChimeraLogger.Log("[BreedingTest] ✗ Basic breeding failed");
                    }
                }
            }
            else
            {
                _testsFailed++;
                if (_enableLogging)
                {
                    ChimeraLogger.Log("[BreedingTest] ✗ Cannot test breeding - no genetics engine");
                }
            }
        }

        private void TestStrainCreation()
        {
            _testsRun++;
            // Primary: Try ServiceContainer resolution
            if (!ServiceContainerFactory.Instance.TryResolve<ProjectChimera.Systems.Genetics.FractalGeneticsEngine>(out var geneticsEngine))
            {
                // Fallback: Scene discovery for testing
                geneticsEngine = GameObject.FindObjectOfType<ProjectChimera.Systems.Genetics.FractalGeneticsEngine>();
            }

            if (geneticsEngine != null)
            {
                // Test creating a new strain
                geneticsEngine.CreateGeneticData("Test_New_Strain", 22f, 0.8f, 425f);

                var strainData = geneticsEngine.GetGeneticData("Test_New_Strain");

                if (strainData != null && strainData.ThcContent == 22f)
                {
                    _testsPassed++;
                    if (_enableLogging)
                    {
                        ChimeraLogger.Log("[BreedingTest] ✓ Strain creation works");
                    }
                }
                else
                {
                    _testsFailed++;
                    if (_enableLogging)
                    {
                        ChimeraLogger.Log("[BreedingTest] ✗ Strain creation failed");
                    }
                }
            }
            else
            {
                _testsFailed++;
                if (_enableLogging)
                {
                    ChimeraLogger.Log("[BreedingTest] ✗ Cannot test strain creation - no genetics engine");
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
                ChimeraLogger.Log($"[BreedingTest] Test Summary: {_testsRun} run, {_testsPassed} passed, {_testsFailed} failed");

                if (_testsFailed == 0)
                {
                    ChimeraLogger.Log("[BreedingTest] ✓ All tests passed!");
                }
                else
                {
                    ChimeraLogger.Log($"[BreedingTest] ⚠ {_testsFailed} test(s) failed");
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
