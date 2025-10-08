using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using ProjectChimera.Core;
using ProjectChimera.Core.DI;
using ProjectChimera.Core.DI.Validation;

namespace ProjectChimera.Testing.Integration
{
    /// <summary>
    /// Integration tests for ServiceContainer
    /// Validates service registration, resolution, and dependency injection workflows
    /// </summary>
    public class ServiceContainerIntegrationTests
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

        #region Service Registration Tests

        [Test]
        public void ServiceContainer_RegisterSingleton_ResolvesSuccessfully()
        {
            // Arrange
            var testService = new TestService();

            // Act
            _container.RegisterSingleton<ITestService>(testService);
            var resolved = _container.Resolve<ITestService>();

            // Assert
            Assert.IsNotNull(resolved);
            Assert.AreEqual(testService, resolved);
        }

        [Test]
        public void ServiceContainer_RegisterTransient_CreatesNewInstances()
        {
            // Arrange
            _container.RegisterTransient<ITestService, TestService>();

            // Act
            var instance1 = _container.Resolve<ITestService>();
            var instance2 = _container.Resolve<ITestService>();

            // Assert
            Assert.IsNotNull(instance1);
            Assert.IsNotNull(instance2);
            Assert.AreNotEqual(instance1, instance2);
        }

        [Test]
        public void ServiceContainer_RegisterFactory_UsesFactoryMethod()
        {
            // Arrange
            var createdCount = 0;
            _container.RegisterFactory<ITestService>(locator =>
            {
                createdCount++;
                return new TestService();
            });

            // Act
            var instance1 = _container.Resolve<ITestService>();
            var instance2 = _container.Resolve<ITestService>();

            // Assert
            Assert.AreEqual(2, createdCount);
            Assert.IsNotNull(instance1);
            Assert.IsNotNull(instance2);
        }

        #endregion

        #region Dependency Injection Tests

        [Test]
        public void ServiceContainer_ResolveWithDependencies_InjectsDependenciesCorrectly()
        {
            // Arrange
            _container.RegisterSingleton<ITestService>(new TestService());
            _container.RegisterTransient<IDependentService, DependentService>();

            // Act
            var dependent = _container.Resolve<IDependentService>();

            // Assert
            Assert.IsNotNull(dependent);
            Assert.IsNotNull(((DependentService)dependent).Dependency);
        }

        [Test]
        public void ServiceContainer_MultiLevelDependencies_ResolvesChainCorrectly()
        {
            // Arrange
            _container.RegisterSingleton<ITestService>(new TestService());
            _container.RegisterSingleton<IDependentService, DependentService>();
            _container.RegisterTransient<INestedDependentService, NestedDependentService>();

            // Act
            var nested = _container.Resolve<INestedDependentService>();

            // Assert
            Assert.IsNotNull(nested);
            var nestedImpl = (NestedDependentService)nested;
            Assert.IsNotNull(nestedImpl.DependentService);
            Assert.IsNotNull(((DependentService)nestedImpl.DependentService).Dependency);
        }

        #endregion

        #region Service Validation Tests

        [Test]
        public void ServiceContainerValidator_ValidServices_PassesValidation()
        {
            // Arrange
            _container.RegisterSingleton<ITestService>(new TestService());
            _container.RegisterTransient<IDependentService, DependentService>();
            var validator = new ServiceContainerValidator(_container, enableLogging: false);

            // Act
            var results = validator.Validate();

            // Assert
            Assert.IsTrue(results.IsValid);
            Assert.AreEqual(0, results.Errors.Count);
        }

        [Test]
        public void ServiceContainerValidator_MissingDependency_DetectsError()
        {
            // Arrange
            _container.RegisterTransient<IDependentService, DependentService>();
            // Intentionally not registering ITestService
            var validator = new ServiceContainerValidator(_container, enableLogging: false);

            // Act
            var results = validator.Validate();

            // Assert
            Assert.IsFalse(results.IsValid);
            Assert.Greater(results.Errors.Count, 0);
            Assert.IsTrue(results.Errors.Exists(e => e.ErrorType == "MissingDependency"));
        }

        #endregion

        #region Performance Tests

        [Test]
        public void ServiceContainer_ResolvePerformance_CompletesQuickly()
        {
            // Arrange
            _container.RegisterSingleton<ITestService>(new TestService());
            const int iterations = 10000;

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                _container.Resolve<ITestService>();
            }
            stopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, 100); // Should complete in < 100ms
            UnityEngine.TestTools.LogAssert.NoUnexpectedReceived();
            // Performance: {iterations} services resolved in {stopwatch.ElapsedMilliseconds}ms
        }

        #endregion

        #region MonoBehaviour Integration Tests

        [UnityTest]
        public IEnumerator ServiceContainer_MonoBehaviourInjection_ResolvesSuccessfully()
        {
            // Arrange
            var gameObject = new GameObject("TestObject");
            var testComponent = gameObject.AddComponent<TestMonoBehaviour>();
            _container.RegisterSingleton<ITestService>(new TestService());

            // Act
            testComponent.Initialize(_container);
            yield return null; // Wait one frame

            // Assert
            Assert.IsNotNull(testComponent.InjectedService);

            // Cleanup
            Object.Destroy(gameObject);
        }

        #endregion

        #region Test Helper Classes

        public interface ITestService
        {
            string GetMessage();
        }

        public class TestService : ITestService
        {
            public string GetMessage() => "Test Service";
        }

        public interface IDependentService
        {
            ITestService GetDependency();
        }

        public class DependentService : IDependentService
        {
            public ITestService Dependency { get; private set; }

            public DependentService() { }

            public DependentService(ITestService dependency)
            {
                Dependency = dependency;
            }

            public ITestService GetDependency() => Dependency;
        }

        public interface INestedDependentService
        {
            IDependentService GetDependent();
        }

        public class NestedDependentService : INestedDependentService
        {
            public IDependentService DependentService { get; private set; }

            public NestedDependentService() { }

            public NestedDependentService(IDependentService dependentService)
            {
                DependentService = dependentService;
            }

            public IDependentService GetDependent() => DependentService;
        }

        public class TestMonoBehaviour : MonoBehaviour
        {
            public ITestService InjectedService { get; private set; }

            public void Initialize(ServiceContainer container)
            {
                InjectedService = container.Resolve<ITestService>();
            }
        }

        #endregion
    }
}

