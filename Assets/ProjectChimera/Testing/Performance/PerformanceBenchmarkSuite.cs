using UnityEngine;
using UnityEngine.Profiling;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using ProjectChimera.Systems.Cultivation;
using ProjectChimera.Systems.Environment;
using ProjectChimera.Systems.Genetics;

namespace ProjectChimera.Testing.Performance
{
    /// <summary>
    /// Performance Benchmark Suite for Project Chimera CI/CD Pipeline
    /// Tests critical performance scenarios to ensure 60 FPS with 1000+ plants
    /// Based on Phase 2 performance requirements
    /// </summary>
    public class PerformanceBenchmarkSuite : MonoBehaviour
    {
        [Header("Benchmark Configuration")]
        [SerializeField] private int _targetFrameRate = 60;
        [SerializeField] private float _benchmarkDurationSeconds = 30f;
        [SerializeField] private bool _enableGPUProfiling = true;
        [SerializeField] private bool _generateDetailedReport = true;

        [Header("Test Scenarios")]
        [SerializeField] private bool _runPlantStressTest = true;
        [SerializeField] private bool _runGeneticsCalculationTest = true;
        [SerializeField] private bool _runEnvironmentalSimulationTest = true;
        [SerializeField] private bool _runMassiveFacilityTest = true;

        private PerformanceResults _results;
        private Stopwatch _stopwatch;
        private List<float> _frameTimes;
        private List<long> _memorySnapshots;
        private TestResult _currentTestResult;

        /// <summary>
        /// Performance benchmark results
        /// </summary>
        [System.Serializable]
        public class PerformanceResults
        {
            public float averageFrameRate;
            public float minimumFrameRate;
            public float maximumFrameRate;
            public float frameTimeVariance;
            public long averageMemoryUsage;
            public long peakMemoryUsage;
            public int gcCollections;
            public TestResult[] individualTests;
            public bool overallPass;
        }

        [System.Serializable]
        public class TestResult
        {
            public string testName;
            public float averageFPS;
            public float minFPS;
            public long memoryUsed;
            public bool passed;
            public string details;
        }

        private void Start()
        {
            StartCoroutine(RunBenchmarkSuite());
        }

        /// <summary>
        /// Main benchmark suite execution
        /// </summary>
        private IEnumerator RunBenchmarkSuite()
        {
            ChimeraLogger.Log("🎯 Starting Performance Benchmark Suite");

            _results = new PerformanceResults();
            _frameTimes = new List<float>();
            _memorySnapshots = new List<long>();
            _stopwatch = Stopwatch.StartNew();

            var individualResults = new List<TestResult>();

            // Configure performance monitoring
            ConfigurePerformanceMonitoring();

            // Warm-up phase
            yield return StartCoroutine(WarmUpPhase());

            // Run individual performance tests
            if (_runPlantStressTest)
            {
                yield return StartCoroutine(RunPlantStressTestCoroutine(individualResults));
            }

            if (_runGeneticsCalculationTest)
            {
                yield return StartCoroutine(RunGeneticsCalculationTestCoroutine(individualResults));
            }

            if (_runEnvironmentalSimulationTest)
            {
                yield return StartCoroutine(RunEnvironmentalSimulationTestCoroutine(individualResults));
            }

            if (_runMassiveFacilityTest)
            {
                yield return StartCoroutine(RunMassiveFacilityTestCoroutine(individualResults));
            }

            // Compile final results
            CompileResults(individualResults);

            // Generate reports
            GeneratePerformanceReport();

            ChimeraLogger.Log($"🎯 Performance Benchmark Suite Complete - Overall Pass: {_results.overallPass}");

#if CHIMERA_PERFORMANCE_BUILD
            // Exit application in CI environment
            Application.Quit();
#endif
        }

        /// <summary>
        /// Configure Unity performance monitoring settings
        /// </summary>
        private void ConfigurePerformanceMonitoring()
        {
            Application.targetFrameRate = _targetFrameRate;
            Time.fixedDeltaTime = 1f / _targetFrameRate;

            // Enable GPU profiling if available
            if (_enableGPUProfiling)
            {
                Profiler.enableBinaryLog = true;
                Profiler.logFile = "performance_profile.raw";
                Profiler.enabled = true;
            }

            QualitySettings.vSyncCount = 0; // Disable VSync for accurate measurement

            ChimeraLogger.Log("Performance monitoring configured");
        }

        /// <summary>
        /// Warm-up phase to stabilize performance measurements
        /// </summary>
        private IEnumerator WarmUpPhase()
        {
            ChimeraLogger.Log("🔥 Running warm-up phase...");

            // Force garbage collection
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();

            // Wait for systems to stabilize
            yield return new WaitForSeconds(3f);

            ChimeraLogger.Log("Warm-up complete");
        }

        /// <summary>
        /// Wrapper coroutine for plant stress test
        /// </summary>
        private IEnumerator RunPlantStressTestCoroutine(List<TestResult> results)
        {
            yield return StartCoroutine(RunPlantStressTest());
            if (_currentTestResult != null)
            {
                results.Add(_currentTestResult);
                _currentTestResult = null;
            }
        }

        /// <summary>
        /// Wrapper coroutine for genetics calculation test
        /// </summary>
        private IEnumerator RunGeneticsCalculationTestCoroutine(List<TestResult> results)
        {
            yield return StartCoroutine(RunGeneticsCalculationTest());
            if (_currentTestResult != null)
            {
                results.Add(_currentTestResult);
                _currentTestResult = null;
            }
        }

        /// <summary>
        /// Wrapper coroutine for environmental simulation test
        /// </summary>
        private IEnumerator RunEnvironmentalSimulationTestCoroutine(List<TestResult> results)
        {
            yield return StartCoroutine(RunEnvironmentalSimulationTest());
            if (_currentTestResult != null)
            {
                results.Add(_currentTestResult);
                _currentTestResult = null;
            }
        }

        /// <summary>
        /// Wrapper coroutine for massive facility test
        /// </summary>
        private IEnumerator RunMassiveFacilityTestCoroutine(List<TestResult> results)
        {
            yield return StartCoroutine(RunMassiveFacilityTest());
            if (_currentTestResult != null)
            {
                results.Add(_currentTestResult);
                _currentTestResult = null;
            }
        }

        /// <summary>
        /// Test performance with 1000+ plants simulation
        /// Critical requirement from Phase 2
        /// </summary>
        private IEnumerator RunPlantStressTest()
        {
            ChimeraLogger.Log("🌱 Running Plant Stress Test (1000+ plants)");

            var testResult = new TestResult
            {
                testName = "Plant Stress Test",
                details = "Simulating 1000+ plants with full environmental interactions"
            };

            var frameTimesList = new List<float>();
            var startMemory = Profiler.GetTotalAllocatedMemory();
            var startTime = Time.realtimeSinceStartup;

            // Simulate creating 1000+ plants
            var plantInstances = new List<GameObject>();
            for (int i = 0; i < 1100; i++)
            {
                // Create simplified plant representation for testing
                var plant = new GameObject($"TestPlant_{i}");
                var plantComponent = plant.AddComponent<PlantPerformanceTest>();
                plantComponent.Initialize(i);
                plantInstances.Add(plant);

                // Yield occasionally to prevent frame drops during setup
                if (i % 50 == 0)
                    yield return null;
            }

            // Monitor performance for specified duration
            var endTime = startTime + _benchmarkDurationSeconds;
            while (Time.realtimeSinceStartup < endTime)
            {
                frameTimesList.Add(Time.unscaledDeltaTime);
                yield return null;
            }

            // Calculate results
            var frameRates = frameTimesList.ConvertAll(ft => 1f / ft);
            testResult.averageFPS = frameRates.Sum() / frameRates.Count;
            testResult.minFPS = frameRates.Min();
            testResult.memoryUsed = Profiler.GetTotalAllocatedMemory() - startMemory;
            testResult.passed = testResult.minFPS >= _targetFrameRate * 0.9f; // 90% of target FPS

            // Cleanup
            foreach (var plant in plantInstances)
            {
                DestroyImmediate(plant);
            }

            ChimeraLogger.Log($"Plant Stress Test Complete - Avg FPS: {testResult.averageFPS:F2}, Min FPS: {testResult.minFPS:F2}");

            _currentTestResult = testResult;
        }

        /// <summary>
        /// Test genetics calculation performance
        /// Critical for breeding system scalability
        /// </summary>
        private IEnumerator RunGeneticsCalculationTest()
        {
            ChimeraLogger.Log("🧬 Running Genetics Calculation Test");

            var testResult = new TestResult
            {
                testName = "Genetics Calculation Test",
                details = "Mass breeding calculations and trait expressions"
            };

            var frameTimesList = new List<float>();
            var startMemory = Profiler.GetTotalAllocatedMemory();
            var startTime = Time.realtimeSinceStartup;

            // Simulate complex genetics calculations
            var geneticsProcessor = new GeneticsPerformanceTest();
            var endTime = startTime + _benchmarkDurationSeconds;

            while (Time.realtimeSinceStartup < endTime)
            {
                // Perform genetics calculations each frame
                geneticsProcessor.ProcessBreedingCalculations(100); // 100 breeding calculations per frame

                frameTimesList.Add(Time.unscaledDeltaTime);
                yield return null;
            }

            var frameRates = frameTimesList.ConvertAll(ft => 1f / ft);
            testResult.averageFPS = frameRates.Sum() / frameRates.Count;
            testResult.minFPS = frameRates.Min();
            testResult.memoryUsed = Profiler.GetTotalAllocatedMemory() - startMemory;
            testResult.passed = testResult.minFPS >= _targetFrameRate * 0.95f; // Higher standard for calculations

            ChimeraLogger.Log($"Genetics Calculation Test Complete - Avg FPS: {testResult.averageFPS:F2}");

            _currentTestResult = testResult;
        }

        /// <summary>
        /// Test environmental simulation performance
        /// </summary>
        private IEnumerator RunEnvironmentalSimulationTest()
        {
            ChimeraLogger.Log("🌡️ Running Environmental Simulation Test");

            var testResult = new TestResult
            {
                testName = "Environmental Simulation Test",
                details = "HVAC, lighting, and atmospheric simulation at scale"
            };

            var frameTimesList = new List<float>();
            var startMemory = Profiler.GetTotalAllocatedMemory();
            var startTime = Time.realtimeSinceStartup;

            // Simulate environmental systems
            var envProcessor = new EnvironmentPerformanceTest();
            var endTime = startTime + _benchmarkDurationSeconds;

            while (Time.realtimeSinceStartup < endTime)
            {
                envProcessor.ProcessEnvironmentalUpdate(1000); // 1000 environmental zones

                frameTimesList.Add(Time.unscaledDeltaTime);
                yield return null;
            }

            var frameRates = frameTimesList.ConvertAll(ft => 1f / ft);
            testResult.averageFPS = frameRates.Sum() / frameRates.Count;
            testResult.minFPS = frameRates.Min();
            testResult.memoryUsed = Profiler.GetTotalAllocatedMemory() - startMemory;
            testResult.passed = testResult.minFPS >= _targetFrameRate * 0.9f;

            ChimeraLogger.Log($"Environmental Simulation Test Complete - Avg FPS: {testResult.averageFPS:F2}");

            _currentTestResult = testResult;
        }

        /// <summary>
        /// Test massive facility with all systems active
        /// Ultimate stress test combining all systems
        /// </summary>
        private IEnumerator RunMassiveFacilityTest()
        {
            ChimeraLogger.Log("🏗️ Running Massive Facility Test");

            var testResult = new TestResult
            {
                testName = "Massive Facility Test",
                details = "All systems active: 1000+ plants, genetics, environment, construction"
            };

            var frameTimesList = new List<float>();
            var startMemory = Profiler.GetTotalAllocatedMemory();
            var startTime = Time.realtimeSinceStartup;

            // Create comprehensive test scenario
            var facilityTest = new MassiveFacilityPerformanceTest();
            facilityTest.Initialize();

            var endTime = startTime + _benchmarkDurationSeconds;

            while (Time.realtimeSinceStartup < endTime)
            {
                facilityTest.UpdateAllSystems();

                frameTimesList.Add(Time.unscaledDeltaTime);
                yield return null;
            }

            var frameRates = frameTimesList.ConvertAll(ft => 1f / ft);
            testResult.averageFPS = frameRates.Sum() / frameRates.Count;
            testResult.minFPS = frameRates.Min();
            testResult.memoryUsed = Profiler.GetTotalAllocatedMemory() - startMemory;
            testResult.passed = testResult.minFPS >= _targetFrameRate * 0.85f; // Slightly lower for comprehensive test

            facilityTest.Cleanup();

            ChimeraLogger.Log($"Massive Facility Test Complete - Avg FPS: {testResult.averageFPS:F2}");

            _currentTestResult = testResult;
        }

        /// <summary>
        /// Compile all test results into final performance report
        /// </summary>
        private void CompileResults(List<TestResult> individualResults)
        {
            _results.individualTests = individualResults.ToArray();

            if (_frameTimes.Count > 0)
            {
                var frameRates = _frameTimes.ConvertAll(ft => 1f / ft);
                _results.averageFrameRate = frameRates.Sum() / frameRates.Count;
                _results.minimumFrameRate = frameRates.Min();
                _results.maximumFrameRate = frameRates.Max();

                var mean = _results.averageFrameRate;
                _results.frameTimeVariance = frameRates.Sum(fr => (fr - mean) * (fr - mean)) / frameRates.Count;
            }

            if (_memorySnapshots.Count > 0)
            {
                _results.averageMemoryUsage = (long)_memorySnapshots.Average();
                _results.peakMemoryUsage = _memorySnapshots.Max();
            }

            _results.gcCollections = System.GC.CollectionCount(0) + System.GC.CollectionCount(1) + System.GC.CollectionCount(2);

            // Determine overall pass/fail
            _results.overallPass = individualResults.All(r => r.passed) &&
                                   _results.minimumFrameRate >= _targetFrameRate * 0.85f;
        }

        /// <summary>
        /// Generate detailed performance report for CI consumption
        /// </summary>
        private void GeneratePerformanceReport()
        {
            var report = new
            {
                timestamp = System.DateTime.Now,
                targetFrameRate = _targetFrameRate,
                benchmarkDuration = _benchmarkDurationSeconds,
                results = _results,
                systemInfo = new
                {
                    platform = Application.platform.ToString(),
                    unityVersion = Application.unityVersion,
                    processorType = SystemInfo.processorType,
                    systemMemorySize = SystemInfo.systemMemorySize,
                    graphicsDeviceName = SystemInfo.graphicsDeviceName,
                    graphicsMemorySize = SystemInfo.graphicsMemorySize
                }
            };

            var reportJson = JsonUtility.ToJson(report, true);
            var reportPath = Path.Combine(Application.persistentDataPath, "performance-results.json");
            File.WriteAllText(reportPath, reportJson);

            ChimeraLogger.Log($"Performance report generated: {reportPath}");

            // Also log key metrics for CI parsing
            ChimeraLogger.Log($"PERFORMANCE_METRIC:AverageFPS:{_results.averageFrameRate:F2}");
            ChimeraLogger.Log($"PERFORMANCE_METRIC:MinimumFPS:{_results.minimumFrameRate:F2}");
            ChimeraLogger.Log($"PERFORMANCE_METRIC:PeakMemoryMB:{_results.peakMemoryUsage / 1048576L}");
            ChimeraLogger.Log($"PERFORMANCE_METRIC:OverallPass:{_results.overallPass}");
        }
    }

    #region Performance Test Helpers

    /// <summary>
    /// Simplified plant component for performance testing
    /// </summary>
    public class PlantPerformanceTest : MonoBehaviour, ITickable
    {
        private int _plantId;
        private float _nextUpdate;
        private Vector3 _position;

        public void Initialize(int plantId)
        {
            _plantId = plantId;
            _position = transform.position;
            _nextUpdate = Time.time + Random.Range(0f, 1f);
            
            // Register with UpdateOrchestrator
            UpdateOrchestrator.Instance?.RegisterTickable(this);
        }

        // ITickable implementation
        public int Priority => 200; // Low priority for performance test components
        public bool Enabled => enabled && gameObject.activeInHierarchy;
        
        public void Tick(float deltaTime)
        {
            if (Time.time >= _nextUpdate)
            {
                // Simulate plant processing
                ProcessPlantUpdate();
                _nextUpdate = Time.time + 0.1f; // Update every 100ms
            }
        }
        
        public void OnRegistered()
        {
            // Called when registered with UpdateOrchestrator
        }
        
        public void OnUnregistered()
        {
            // Called when unregistered from UpdateOrchestrator
        }
        
        private void OnDestroy()
        {
            // Unregister from UpdateOrchestrator
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
        }

        private void ProcessPlantUpdate()
        {
            // Simulate computation load similar to real plant processing
            var random = new System.Random(_plantId);
            for (int i = 0; i < 10; i++)
            {
                var calculation = Mathf.Sin(Time.time + _plantId) * random.Next(0, 100);
            }
        }
    }

    /// <summary>
    /// Genetics calculation performance test helper
    /// </summary>
    public class GeneticsPerformanceTest
    {
        private System.Random _random = new System.Random();

        public void ProcessBreedingCalculations(int count)
        {
            for (int i = 0; i < count; i++)
            {
                // Simulate genetics calculations
                var trait1 = _random.NextDouble();
                var trait2 = _random.NextDouble();
                var dominance = _random.NextDouble();

                var result = CalculateTraitExpression(trait1, trait2, dominance);
            }
        }

        private double CalculateTraitExpression(double trait1, double trait2, double dominance)
        {
            // Simplified genetics calculation
            return trait1 * dominance + trait2 * (1.0 - dominance) +
                   Mathf.Sin((float)(trait1 + trait2)) * 0.1;
        }
    }

    /// <summary>
    /// Environment simulation performance test helper
    /// </summary>
    public class EnvironmentPerformanceTest
    {
        private System.Random _random = new System.Random();

        public void ProcessEnvironmentalUpdate(int zoneCount)
        {
            for (int i = 0; i < zoneCount; i++)
            {
                // Simulate environmental calculations
                var temperature = 20f + (float)_random.NextDouble() * 10f;
                var humidity = 40f + (float)_random.NextDouble() * 30f;
                var airflow = (float)_random.NextDouble() * 5f;

                var heatIndex = CalculateHeatIndex(temperature, humidity);
                var comfort = CalculateComfortIndex(temperature, humidity, airflow);
            }
        }

        private float CalculateHeatIndex(float temp, float humidity)
        {
            return temp + (humidity / 100f) * (temp - 14f);
        }

        private float CalculateComfortIndex(float temp, float humidity, float airflow)
        {
            return Mathf.Clamp01((30f - Mathf.Abs(temp - 22f)) / 30f) *
                   Mathf.Clamp01((70f - Mathf.Abs(humidity - 50f)) / 70f) *
                   Mathf.Clamp01(airflow / 2f);
        }
    }

    /// <summary>
    /// Massive facility performance test helper
    /// </summary>
    public class MassiveFacilityPerformanceTest
    {
        private List<PlantPerformanceTest> _plants = new List<PlantPerformanceTest>();
        private GeneticsPerformanceTest _genetics = new GeneticsPerformanceTest();
        private EnvironmentPerformanceTest _environment = new EnvironmentPerformanceTest();

        public void Initialize()
        {
            // Create virtual facility with 1000+ plants
            for (int i = 0; i < 1200; i++)
            {
                var plantTest = new PlantPerformanceTest();
                _plants.Add(plantTest);
            }
        }

        public void UpdateAllSystems()
        {
            // Simulate all systems working together
            _genetics.ProcessBreedingCalculations(50);
            _environment.ProcessEnvironmentalUpdate(200);

            // Additional construction/UI simulation
            for (int i = 0; i < 100; i++)
            {
                var calculation = Mathf.Sin(Time.time * i) * Mathf.Cos(Time.time * i * 0.5f);
            }
        }

        public void Cleanup()
        {
            _plants.Clear();
        }
    }

    #endregion
}
