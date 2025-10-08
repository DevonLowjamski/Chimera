using NUnit.Framework;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;
using ProjectChimera.Core;
using ProjectChimera.Core.DI;
using ProjectChimera.Core.DI.Validation;

namespace ProjectChimera.Testing.Integration
{
    /// <summary>
    /// End-to-end integration tests for dependency injection workflows
    /// Simulates real-world service orchestration scenarios
    /// </summary>
    public class EndToEndDIWorkflowTests
    {
        private ServiceContainer _container;

        [SetUp]
        public void Setup()
        {
            _container = new ServiceContainer();
        }

        [TearDown]
        public void Teardown()
        {
            _container?.Clear();
            _container = null;
        }

        #region Application Lifecycle Tests

        [Test]
        public void DIWorkflow_ApplicationStartup_InitializesAllCoreServices()
        {
            // Arrange - Simulate application bootstrap
            RegisterCoreServices();

            // Act
            var validator = new ServiceContainerValidator(_container, enableLogging: false);
            var results = validator.Validate();

            // Assert
            Assert.IsTrue(results.IsValid, "All core services should be valid");
            Assert.Greater(results.TotalServicesRegistered, 0);
            Assert.AreEqual(0, results.Errors.Count, "No errors should exist");
        }

        [Test]
        public void DIWorkflow_ServiceOrchestration_ResolvesComplexDependencyChain()
        {
            // Arrange
            _container.RegisterSingleton<IDataService>(new DataService());
            _container.RegisterSingleton<IBusinessLogicService, BusinessLogicService>();
            _container.RegisterSingleton<IUIService, UIService>();
            _container.RegisterTransient<IOrchestrator, ApplicationOrchestrator>();

            // Act
            var orchestrator = _container.Resolve<IOrchestrator>();

            // Assert
            Assert.IsNotNull(orchestrator);
            Assert.DoesNotThrow(() => orchestrator.Initialize());
        }

        [Test]
        public void DIWorkflow_DependencyUpdate_PropagatesChanges()
        {
            // Arrange
            var originalData = new DataService();
            _container.RegisterSingleton<IDataService>(originalData);
            _container.RegisterTransient<IBusinessLogicService, BusinessLogicService>();

            var logic1 = _container.Resolve<IBusinessLogicService>();

            // Act - Update singleton
            var updatedData = new DataService();
            _container.RegisterSingleton<IDataService>(updatedData);
            var logic2 = _container.Resolve<IBusinessLogicService>();

            // Assert
            Assert.AreNotEqual(
                ((BusinessLogicService)logic1).DataService,
                ((BusinessLogicService)logic2).DataService
            );
        }

        #endregion

        #region Manager Integration Tests

        [UnityTest]
        public IEnumerator DIWorkflow_ManagerInitialization_CompletesSuccessfully()
        {
            // Arrange
            RegisterCoreServices();
            var gameObject = new GameObject("TestManager");
            var manager = gameObject.AddComponent<TestManager>();

            // Act
            manager.Initialize(_container);
            yield return null;

            // Assert
            Assert.IsTrue(manager.IsInitialized);
            Assert.IsNotNull(manager.DataService);
            Assert.IsNotNull(manager.LogicService);

            // Cleanup
            Object.Destroy(gameObject);
        }

        [UnityTest]
        public IEnumerator DIWorkflow_MultipleManagers_CoordinateCorrectly()
        {
            // Arrange
            RegisterCoreServices();
            var dataManager = CreateManager<DataManager>("DataManager");
            var logicManager = CreateManager<LogicManager>("LogicManager");
            var uiManager = CreateManager<UIManagerTest>("UIManager");

            // Act
            dataManager.Initialize(_container);
            logicManager.Initialize(_container);
            uiManager.Initialize(_container);
            yield return null;

            // Assert
            Assert.IsTrue(dataManager.IsInitialized);
            Assert.IsTrue(logicManager.IsInitialized);
            Assert.IsTrue(uiManager.IsInitialized);

            // Cleanup
            Object.Destroy(dataManager.gameObject);
            Object.Destroy(logicManager.gameObject);
            Object.Destroy(uiManager.gameObject);
        }

        #endregion

        #region Error Recovery Tests

        [Test]
        public void DIWorkflow_MissingDependency_FailsGracefully()
        {
            // Arrange
            // Intentionally not registering IDataService
            _container.RegisterTransient<IBusinessLogicService, BusinessLogicService>();

            // Act & Assert
            Assert.Throws<System.Exception>(() => _container.Resolve<IBusinessLogicService>());
        }

        [Test]
        public void DIWorkflow_CircularDependency_DetectedDuringValidation()
        {
            // Arrange
            _container.RegisterTransient<ICircularA, CircularA>();
            _container.RegisterTransient<ICircularB, CircularB>();

            // Act
            var validator = new ServiceContainerValidator(_container, enableLogging: false);
            var results = validator.Validate();

            // Assert
            Assert.IsFalse(results.IsValid);
            Assert.IsTrue(results.Errors.Exists(e => e.ErrorType == "CircularDependency"));
        }

        #endregion

        #region Performance Tests

        [Test]
        public void DIWorkflow_HighFrequencyResolution_MaintainsPerformance()
        {
            // Arrange
            _container.RegisterSingleton<IDataService>(new DataService());
            const int iterations = 50000;

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                var service = _container.Resolve<IDataService>();
            }
            stopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, 500);
            UnityEngine.TestTools.LogAssert.NoUnexpectedReceived();
            // Performance: {iterations} services resolved in {stopwatch.ElapsedMilliseconds}ms
        }

        [Test]
        public void DIWorkflow_ComplexServiceGraph_ResolvesEfficiently()
        {
            // Arrange
            RegisterComplexServiceGraph();
            const int iterations = 1000;

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                var orchestrator = _container.Resolve<IOrchestrator>();
            }
            stopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, 1000);
            UnityEngine.TestTools.LogAssert.NoUnexpectedReceived();
            // Performance: Complex graph resolved {iterations} times in {stopwatch.ElapsedMilliseconds}ms
        }

        #endregion

        #region Helper Methods

        private void RegisterCoreServices()
        {
            _container.RegisterSingleton<IDataService>(new DataService());
            _container.RegisterSingleton<IBusinessLogicService, BusinessLogicService>();
            _container.RegisterSingleton<IUIService, UIService>();
        }

        private void RegisterComplexServiceGraph()
        {
            _container.RegisterSingleton<IDataService>(new DataService());
            _container.RegisterSingleton<IBusinessLogicService, BusinessLogicService>();
            _container.RegisterSingleton<IUIService, UIService>();
            _container.RegisterTransient<IOrchestrator, ApplicationOrchestrator>();
        }

        private T CreateManager<T>(string name) where T : MonoBehaviour
        {
            var gameObject = new GameObject(name);
            return gameObject.AddComponent<T>();
        }

        #endregion

        #region Test Service Interfaces & Implementations

        public interface IDataService { string GetData(); }
        public interface IBusinessLogicService { string ProcessData(); }
        public interface IUIService { void DisplayData(string data); }
        public interface IOrchestrator { void Initialize(); }

        public class DataService : IDataService
        {
            public string GetData() => "Test Data";
        }

        public class BusinessLogicService : IBusinessLogicService
        {
            public IDataService DataService { get; private set; }

            public BusinessLogicService() { }

            public BusinessLogicService(IDataService dataService)
            {
                DataService = dataService;
            }

            public string ProcessData() => DataService != null ? $"Processed: {DataService.GetData()}" : "No Data";
        }

        public class UIService : IUIService
        {
            public void DisplayData(string data) { /* Display logic */ }
        }

        public class ApplicationOrchestrator : IOrchestrator
        {
            private IDataService _dataService;
            private IBusinessLogicService _logicService;
            private IUIService _uiService;

            public ApplicationOrchestrator() { }

            public ApplicationOrchestrator(
                IDataService dataService,
                IBusinessLogicService logicService,
                IUIService uiService)
            {
                _dataService = dataService;
                _logicService = logicService;
                _uiService = uiService;
            }

            public void Initialize()
            {
                if (_logicService != null && _uiService != null)
                {
                    var data = _logicService.ProcessData();
                    _uiService.DisplayData(data);
                }
            }
        }

        // Circular dependency test classes
        public interface ICircularA { }
        public interface ICircularB { }

        public class CircularA : ICircularA
        {
            public CircularA() { }
            public CircularA(ICircularB b) { }
        }

        public class CircularB : ICircularB
        {
            public CircularB() { }
            public CircularB(ICircularA a) { }
        }

        // Test MonoBehaviour managers
        public class TestManager : MonoBehaviour
        {
            public bool IsInitialized { get; private set; }
            public IDataService DataService { get; private set; }
            public IBusinessLogicService LogicService { get; private set; }

            public void Initialize(ServiceContainer container)
            {
                DataService = container.Resolve<IDataService>();
                LogicService = container.Resolve<IBusinessLogicService>();
                IsInitialized = true;
            }
        }

        public class DataManager : MonoBehaviour
        {
            public bool IsInitialized { get; private set; }
            public void Initialize(ServiceContainer container) { IsInitialized = true; }
        }

        public class LogicManager : MonoBehaviour
        {
            public bool IsInitialized { get; private set; }
            public void Initialize(ServiceContainer container) { IsInitialized = true; }
        }

        public class UIManagerTest : MonoBehaviour
        {
            public bool IsInitialized { get; private set; }
            public void Initialize(ServiceContainer container) { IsInitialized = true; }
        }

        #endregion
    }
}

