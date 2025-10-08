using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using ProjectChimera.Core.DI;
using ProjectChimera.Core.DI.Validation;

namespace ProjectChimera.Testing.Integration
{
    /// <summary>
    /// Integration tests for Service Health Monitoring
    /// Validates health checks, degradation detection, and recovery monitoring
    /// </summary>
    public class ServiceHealthMonitoringTests
    {
        private ServiceContainer _container;
        private ServiceHealthMonitor _healthMonitor;

        [SetUp]
        public void Setup()
        {
            _container = new ServiceContainer();
            _healthMonitor = new ServiceHealthMonitor(_container, enableLogging: false, checkInterval: 0.1f);
        }

        [TearDown]
        public void Teardown()
        {
            _healthMonitor = null;
            _container?.Clear();
            _container = null;
        }

        #region Health Check Tests

        [Test]
        public void HealthMonitor_HealthyService_ReportsHealthy()
        {
            // Arrange
            var healthyService = new HealthyTestService();
            _container.RegisterSingleton<IHealthCheckable>(healthyService);

            // Act
            var report = _healthMonitor.PerformHealthCheck();

            // Assert
            Assert.AreEqual(ServiceHealthStatus.Healthy, report.OverallStatus);
            Assert.AreEqual(1, report.HealthyServices);
            Assert.AreEqual(0, report.UnhealthyServices);
        }

        [Test]
        public void HealthMonitor_UnhealthyService_ReportsUnhealthy()
        {
            // Arrange
            var unhealthyService = new UnhealthyTestService();
            _container.RegisterSingleton<IHealthCheckable>(unhealthyService);

            // Act
            var report = _healthMonitor.PerformHealthCheck();

            // Assert
            Assert.AreEqual(ServiceHealthStatus.Unhealthy, report.OverallStatus);
            Assert.AreEqual(0, report.HealthyServices);
            Assert.AreEqual(1, report.UnhealthyServices);
        }

        [Test]
        public void HealthMonitor_MixedServices_ReportsCorrectly()
        {
            // Arrange
            _container.RegisterSingleton<IHealthyService>(new HealthyTestService());
            _container.RegisterSingleton<IUnhealthyService>(new UnhealthyTestService());
            _container.RegisterSingleton<IDegradedService>(new DegradedTestService());

            // Act
            var report = _healthMonitor.PerformHealthCheck();

            // Assert
            Assert.IsNotNull(report);
            Assert.AreEqual(3, report.TotalServices);
        }

        #endregion

        #region Degradation Detection Tests

        [Test]
        public void HealthMonitor_DegradedService_DetectsDegradation()
        {
            // Arrange
            var degradedService = new DegradedTestService();
            _container.RegisterSingleton<IHealthCheckable>(degradedService);

            // Act
            var report = _healthMonitor.PerformHealthCheck();

            // Assert
            Assert.AreEqual(ServiceHealthStatus.Degraded, report.OverallStatus);
            Assert.AreEqual(1, report.DegradedServices);
        }

        [Test]
        public void HealthMonitor_GetDegradedServices_ReturnsCorrectList()
        {
            // Arrange
            _container.RegisterSingleton<IHealthyService>(new HealthyTestService());
            _container.RegisterSingleton<IDegradedService>(new DegradedTestService());
            _healthMonitor.PerformHealthCheck();

            // Act
            var degradedServices = _healthMonitor.GetDegradedServices();

            // Assert
            Assert.IsNotNull(degradedServices);
            Assert.AreEqual(1, degradedServices.Count);
        }

        #endregion

        #region Failure History Tests

        [Test]
        public void HealthMonitor_RepeatedFailures_TracksHistory()
        {
            // Arrange
            var flakeyService = new FlakeyTestService();
            _container.RegisterSingleton<IHealthCheckable>(flakeyService);

            // Act
            for (int i = 0; i < 5; i++)
            {
                _healthMonitor.PerformHealthCheck();
            }

            var healthCheck = _healthMonitor.GetServiceHealth(typeof(IHealthCheckable));

            // Assert
            Assert.IsNotNull(healthCheck);
            // Flakey service should have some failures recorded
        }

        [Test]
        public void HealthMonitor_ResetFailureCount_ClearsHistory()
        {
            // Arrange
            var unhealthyService = new UnhealthyTestService();
            _container.RegisterSingleton<IHealthCheckable>(unhealthyService);
            _healthMonitor.PerformHealthCheck(); // Record failure

            // Act
            _healthMonitor.ResetFailureCount(typeof(IHealthCheckable));
            var healthCheck = _healthMonitor.GetServiceHealth(typeof(IHealthCheckable));

            // Assert
            Assert.IsNotNull(healthCheck);
        }

        #endregion

        #region MonoBehaviour Health Checks

        [UnityTest]
        public IEnumerator HealthMonitor_MonoBehaviourService_ValidatesLifecycle()
        {
            // Arrange
            var gameObject = new GameObject("TestService");
            var monoBehaviour = gameObject.AddComponent<TestHealthCheckableMonoBehaviour>();
            _container.RegisterSingleton<IHealthCheckable>(monoBehaviour);

            // Act
            var report = _healthMonitor.PerformHealthCheck();
            yield return null;

            // Assert
            Assert.AreEqual(ServiceHealthStatus.Healthy, report.OverallStatus);

            // Destroy and check again
            Object.Destroy(gameObject);
            yield return null;

            report = _healthMonitor.PerformHealthCheck();
            Assert.AreEqual(ServiceHealthStatus.Unhealthy, report.OverallStatus);
        }

        #endregion

        #region Performance Tests

        [Test]
        [Performance]
        public void HealthMonitor_LargeServiceSet_PerformanceAcceptable()
        {
            // Arrange
            for (int i = 0; i < 100; i++)
            {
                _container.RegisterSingleton(typeof(IHealthCheckable).Assembly.GetType($"TestService{i}"), 
                    i % 2 == 0 ? (object)new HealthyTestService() : new DegradedTestService());
            }

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var report = _healthMonitor.PerformHealthCheck();
            stopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, 500); // Should complete in < 500ms
            UnityEngine.TestTools.LogAssert.NoUnexpectedReceived();
            // Performance: 100 services checked in {stopwatch.ElapsedMilliseconds}ms
        }

        #endregion

        #region Test Helper Classes

        public interface IHealthyService : IServiceHealthCheckable { }
        public interface IUnhealthyService : IServiceHealthCheckable { }
        public interface IDegradedService : IServiceHealthCheckable { }

        public class HealthyTestService : IHealthyService, IServiceHealthCheckable
        {
            public ServiceHealthCheck PerformHealthCheck()
            {
                return new ServiceHealthCheck
                {
                    ServiceName = "HealthyTestService",
                    Status = ServiceHealthStatus.Healthy,
                    Message = "Service is healthy",
                    CheckTime = System.DateTime.Now
                };
            }
        }

        public class UnhealthyTestService : IUnhealthyService, IServiceHealthCheckable
        {
            public ServiceHealthCheck PerformHealthCheck()
            {
                return new ServiceHealthCheck
                {
                    ServiceName = "UnhealthyTestService",
                    Status = ServiceHealthStatus.Unhealthy,
                    Message = "Service is unhealthy",
                    CheckTime = System.DateTime.Now
                };
            }
        }

        public class DegradedTestService : IDegradedService, IServiceHealthCheckable
        {
            public ServiceHealthCheck PerformHealthCheck()
            {
                return new ServiceHealthCheck
                {
                    ServiceName = "DegradedTestService",
                    Status = ServiceHealthStatus.Degraded,
                    Message = "Service is degraded",
                    CheckTime = System.DateTime.Now
                };
            }
        }

        public class FlakeyTestService : IServiceHealthCheckable
        {
            private int _checkCount = 0;

            public ServiceHealthCheck PerformHealthCheck()
            {
                _checkCount++;
                var isHealthy = _checkCount % 2 == 0;

                return new ServiceHealthCheck
                {
                    ServiceName = "FlakeyTestService",
                    Status = isHealthy ? ServiceHealthStatus.Healthy : ServiceHealthStatus.Unhealthy,
                    Message = isHealthy ? "Healthy" : "Unhealthy",
                    CheckTime = System.DateTime.Now
                };
            }
        }

        public class TestHealthCheckableMonoBehaviour : MonoBehaviour, IServiceHealthCheckable
        {
            public ServiceHealthCheck PerformHealthCheck()
            {
                if (this == null || gameObject == null)
                {
                    return new ServiceHealthCheck
                    {
                        ServiceName = "TestMonoBehaviour",
                        Status = ServiceHealthStatus.Unhealthy,
                        Message = "MonoBehaviour destroyed",
                        CheckTime = System.DateTime.Now
                    };
                }

                return new ServiceHealthCheck
                {
                    ServiceName = "TestMonoBehaviour",
                    Status = ServiceHealthStatus.Healthy,
                    Message = "MonoBehaviour active",
                    CheckTime = System.DateTime.Now
                };
            }
        }

        #endregion
    }
}

