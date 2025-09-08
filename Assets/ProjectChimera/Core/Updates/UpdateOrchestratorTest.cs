using ProjectChimera.Core.Logging;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Core.Updates
{
    /// <summary>
    /// Test component for verifying UpdateOrchestrator functionality
    /// Demonstrates the Phase 0.5 Central Update Bus in action
    /// </summary>
    public class UpdateOrchestratorTest : MonoBehaviour, ITickable
    {
        [Header("Test Settings")]
        [SerializeField] private bool _runTestOnStart = true;
        [SerializeField] private bool _showPerformanceStats = true;
        [SerializeField] private float _testDuration = 10f;
        
        [Header("Test Actions")]
        [SerializeField] private bool _runBasicTest = false;
        [SerializeField] private bool _runPerformanceTest = false;
        [SerializeField] private bool _showStatistics = false;

        private UpdateOrchestrator _orchestrator;
        private TestTickable _testTickable;
        private float _testStartTime;

        public int Priority => TickPriority.UIManager;
        public bool Enabled => enabled;
        
        private void Start()
        {
            if (_runTestOnStart)
            {
                RunBasicTest();
            }
            
            UpdateOrchestrator.Instance?.RegisterTickable(this);
        }
        
        private void OnDestroy()
        {
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
        }

        public void Tick(float deltaTime)
        {
            if (_runBasicTest)
            {
                _runBasicTest = false;
                RunBasicTest();
            }

            if (_runPerformanceTest)
            {
                _runPerformanceTest = false;
                RunPerformanceTest();
            }

            if (_showStatistics)
            {
                _showStatistics = false;
                ShowStatistics();
            }

            // Show continuous performance stats
            if (_showPerformanceStats && _orchestrator != null)
            {
                var stats = _orchestrator.GetStatistics();
                ChimeraLogger.Log($"[UpdateOrchestratorTest] Active: {stats.ActiveTickables}/{stats.RegisteredTickables}, " +
                         $"Avg: {stats.AverageUpdateTime:F2}ms, Last: {stats.LastUpdateTime:F2}ms");
            }
        }

        [ContextMenu("Run Basic Test")]
        public void RunBasicTest()
        {
            ChimeraLogger.Log("=== UPDATE ORCHESTRATOR BASIC TEST ===");

            // Get UpdateOrchestrator instance
            _orchestrator = UpdateOrchestrator.Instance;
            if (_orchestrator == null)
            {
                ChimeraLogger.LogError("UpdateOrchestrator not found!");
                return;
            }

            ChimeraLogger.Log("✅ UpdateOrchestrator instance found");

            // Create test tickable
            _testTickable = new TestTickable("BasicTest", TickPriority.ConstructionSystem);
            
            // Register with orchestrator
            _orchestrator.RegisterTickable(_testTickable);
            ChimeraLogger.Log("✅ Test tickable registered");

            // Test DI integration
            TestDIIntegration();

            // Schedule cleanup
            Invoke(nameof(CleanupBasicTest), 5f);
            _testStartTime = Time.time;

            ChimeraLogger.Log("🚀 Basic test running for 5 seconds...");
        }

        [ContextMenu("Run Performance Test")]
        public void RunPerformanceTest()
        {
            ChimeraLogger.Log("=== UPDATE ORCHESTRATOR PERFORMANCE TEST ===");

            _orchestrator = UpdateOrchestrator.Instance;
            if (_orchestrator == null)
            {
                ChimeraLogger.LogError("UpdateOrchestrator not found!");
                return;
            }

            // Create multiple test tickables with different priorities
            var tickables = new TestTickable[]
            {
                new TestTickable("HighPriority", TickPriority.TimeManager),
                new TestTickable("MediumPriority", TickPriority.CultivationManager),
                new TestTickable("LowPriority", TickPriority.UIManager),
                new TestTickable("EffectsPriority", TickPriority.ParticleEffects)
            };

            foreach (var tickable in tickables)
            {
                _orchestrator.RegisterTickable(tickable);
            }

            ChimeraLogger.Log($"✅ Registered {tickables.Length} test tickables");

            // Schedule cleanup
            Invoke(nameof(CleanupPerformanceTest), _testDuration);
            _testStartTime = Time.time;

            ChimeraLogger.Log($"🚀 Performance test running for {_testDuration} seconds...");
        }

        private void TestDIIntegration()
        {
            ChimeraLogger.Log("🔧 Testing DI Integration:");

            // Test service resolution through DI container
            var orchestratorInterface = ServiceContainerFactory.Instance?.TryResolve<IUpdateOrchestrator>();
            if (orchestratorInterface != null)
            {
                ChimeraLogger.Log($"✅ IUpdateOrchestrator resolved: {orchestratorInterface.GetType().Name}");
                
                // Test interface methods
                var stats = orchestratorInterface.GetStatistics();
                ChimeraLogger.Log($"✅ Statistics retrieved via interface: {stats}");
                
                var status = orchestratorInterface.GetStatus();
                ChimeraLogger.Log($"✅ Status retrieved via interface: {status}");
            }
            else
            {
                ChimeraLogger.LogWarning("⚠️ IUpdateOrchestrator not resolved from DI container");
            }
        }

        [ContextMenu("Show Statistics")]
        public void ShowStatistics()
        {
            ChimeraLogger.Log("=== UPDATE ORCHESTRATOR STATISTICS ===");

            _orchestrator = UpdateOrchestrator.Instance;
            if (_orchestrator == null)
            {
                ChimeraLogger.LogError("UpdateOrchestrator not found!");
                return;
            }

            var stats = _orchestrator.GetStatistics();
            var metrics = _orchestrator.GetMetrics();

            ChimeraLogger.Log($"📊 ORCHESTRATOR STATISTICS:");
            ChimeraLogger.Log($"   Registered Tickables: {stats.RegisteredTickables}");
            ChimeraLogger.Log($"   Active Tickables: {stats.ActiveTickables}");
            ChimeraLogger.Log($"   Fixed Tickables: {stats.RegisteredFixedTickables}");
            ChimeraLogger.Log($"   Late Tickables: {stats.RegisteredLateTickables}");
            ChimeraLogger.Log($"   Last Update Time: {stats.LastUpdateTime:F3}ms");
            ChimeraLogger.Log($"   Average Update Time: {stats.AverageUpdateTime:F3}ms");
            ChimeraLogger.Log($"   Priority Groups: [{string.Join(", ", stats.PriorityGroups)}]");

            ChimeraLogger.Log($"📈 MANAGER METRICS:");
            ChimeraLogger.Log($"   Manager: {metrics.ManagerName}");
            ChimeraLogger.Log($"   Healthy: {metrics.IsHealthy}");
            ChimeraLogger.Log($"   Performance: {metrics.Performance:F2}");
            ChimeraLogger.Log($"   Managed Items: {metrics.ManagedItems}");
            ChimeraLogger.Log($"   Uptime: {metrics.Uptime:F1}s");
            ChimeraLogger.Log($"   Last Activity: {metrics.LastActivity}");
        }

        private void CleanupBasicTest()
        {
            if (_testTickable != null && _orchestrator != null)
            {
                _orchestrator.UnregisterTickable(_testTickable);
                ChimeraLogger.Log($"✅ Basic test completed after {Time.time - _testStartTime:F1}s");
                ChimeraLogger.Log($"   Test tickable ticked {_testTickable.TickCount} times");
            }
        }

        private void CleanupPerformanceTest()
        {
            if (_orchestrator != null)
            {
                var stats = _orchestrator.GetStatistics();
                ChimeraLogger.Log($"✅ Performance test completed after {Time.time - _testStartTime:F1}s");
                ChimeraLogger.Log($"   Final stats - Active: {stats.ActiveTickables}, Avg: {stats.AverageUpdateTime:F2}ms");
                
                // Clear all for cleanup
                _orchestrator.ClearAll();
                ChimeraLogger.Log("🧹 Cleared all test tickables");
            }
        }
    }

    /// <summary>
    /// Simple test implementation of ITickable for verification
    /// </summary>
    public class TestTickable : ITickable
    {
        public string Name { get; }
        public int Priority { get; }
        public bool Enabled { get; set; } = true;
        public int TickCount { get; private set; } = 0;

        private float _lastTickTime;

        public TestTickable(string name, int priority)
        {
            Name = name;
            Priority = priority;
        }

        public void Tick(float deltaTime)
        {
            TickCount++;
            _lastTickTime = Time.time;
            
            // Log periodically to verify ticking
            if (TickCount % 60 == 0) // Every 60 ticks (~1 second at 60fps)
            {
                ChimeraLogger.Log($"[{Name}] Ticked {TickCount} times (Priority: {Priority})");
            }
        }

        public void OnRegistered()
        {
            ChimeraLogger.Log($"[{Name}] Registered with UpdateOrchestrator (Priority: {Priority})");
        }

        public void OnUnregistered()
        {
            ChimeraLogger.Log($"[{Name}] Unregistered from UpdateOrchestrator (Final tick count: {TickCount})");
        }
    }
}

#if UNITY_EDITOR
namespace ProjectChimera.Core.Updates.Editor
{
    using UnityEditor;

    /// <summary>
    /// Custom inspector for UpdateOrchestratorTest
    /// </summary>
    [CustomEditor(typeof(UpdateOrchestratorTest))]
    public class UpdateOrchestratorTestInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            var testComponent = (UpdateOrchestratorTest)target;

            if (GUILayout.Button("Run Basic Test"))
            {
                testComponent.RunBasicTest();
            }

            if (GUILayout.Button("Run Performance Test"))
            {
                testComponent.RunPerformanceTest();
            }

            if (GUILayout.Button("Show Statistics"))
            {
                testComponent.ShowStatistics();
            }
        }
    }
}
#endif