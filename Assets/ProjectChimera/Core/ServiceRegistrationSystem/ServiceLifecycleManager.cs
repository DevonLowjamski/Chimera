using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

using ServiceRegistration = ProjectChimera.Core.ServiceRegistrationData;

namespace ProjectChimera.Core.ServiceRegistrationSystem
{
    /// <summary>
    /// FOCUSED: Service lifecycle management for Project Chimera ServiceContainer
    /// Single responsibility: Handle container disposal, validation, and lifecycle operations
    /// Extracted from ServiceContainer.cs for SRP compliance (Week 8)
    /// </summary>
    public class ServiceLifecycleManager
    {
        private readonly Dictionary<Type, object> _services;
        private readonly object _lock;
        private bool _disposed = false;

        public ServiceLifecycleManager(Dictionary<Type, object> services, object lockObject)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _lock = lockObject ?? throw new ArgumentNullException(nameof(lockObject));
        }

        /// <summary>
        /// Check if container is disposed
        /// </summary>
        public bool IsDisposed => _disposed;

        /// <summary>
        /// Validate services and container state
        /// </summary>
        public void ValidateServices()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ServiceLifecycleManager));

            lock (_lock)
            {
                // Basic validation - ensure no null services
                var invalidServices = _services.Where(kvp => kvp.Value == null).ToList();
                if (invalidServices.Any())
                {
                    ChimeraLogger.LogInfo("ServiceLifecycleManager", "$1");
                }

                ChimeraLogger.LogInfo("ServiceLifecycleManager", "$1");
            }
        }

        /// <summary>
        /// Get registration information for all services
        /// </summary>
        public IEnumerable<ServiceRegistrationInfo> GetRegistrationInfo()
        {
            if (_disposed) yield break;

            lock (_lock)
            {
                foreach (var kvp in _services)
                {
                    yield return new ServiceRegistrationInfo
                    {
                        ServiceType = kvp.Key,
                        ImplementationType = kvp.Value?.GetType(),
                        Lifetime = ProjectChimera.Core.ServiceLifetime.Singleton,
                        HasInstance = kvp.Value != null,
                        RegistrationTime = DateTime.Now
                    };
                }
            }
        }

        /// <summary>
        /// Clear all services
        /// </summary>
        public void Clear()
        {
            if (_disposed) return;

            lock (_lock)
            {
                var serviceCount = _services.Count;
                _services.Clear();
                ChimeraLogger.LogInfo("ServiceLifecycleManager", "$1");
            }
        }

        /// <summary>
        /// Get all registrations
        /// </summary>
        public IDictionary<Type, ServiceRegistration> GetRegistrations()
        {
            if (_disposed) return new Dictionary<Type, ServiceRegistration>();

            lock (_lock)
            {
                var registrations = new Dictionary<Type, ServiceRegistration>();
                foreach (var kvp in _services)
                {
                    registrations[kvp.Key] = new ServiceRegistration(
                        kvp.Key,
                        kvp.Value?.GetType(),
                        ProjectChimera.Core.ServiceLifetime.Singleton,
                        kvp.Value,
                        null
                    );
                }
                return registrations;
            }
        }

        /// <summary>
        /// Verify container integrity
        /// </summary>
        public ContainerVerificationResult Verify()
        {
            var result = new ContainerVerificationResult
            {
                IsValid = !_disposed,
                TotalServices = _services.Count,
                VerifiedServices = _disposed ? 0 : _services.Count,
                VerificationTime = TimeSpan.Zero,
                ValidationTimestamp = DateTime.Now
            };

            if (_disposed)
            {
                result.Errors.Add("Container is disposed");
                return result;
            }

            lock (_lock)
            {
                // Check for null services
                var nullServices = _services.Where(kvp => kvp.Value == null).ToList();
                foreach (var nullService in nullServices)
                {
                    result.Errors.Add($"Service {nullService.Key.Name} has null instance");
                }

                // Check for factory functions that might fail
                foreach (var kvp in _services)
                {
                    if (kvp.Value is Func<object> factory)
                    {
                        try
                        {
                            factory();
                        }
                        catch (Exception ex)
                        {
                            result.Errors.Add($"Factory for {kvp.Key.Name} throws exception: {ex.Message}");
                        }
                    }
                }

                result.IsValid = result.Errors.Count == 0;
                result.VerifiedServices = _services.Count - nullServices.Count;
            }

            return result;
        }

        /// <summary>
        /// Get advanced service descriptors
        /// </summary>
        public IEnumerable<AdvancedServiceDescriptor> GetServiceDescriptors()
        {
            if (_disposed) yield break;

            lock (_lock)
            {
                foreach (var kvp in _services)
                {
                    yield return new AdvancedServiceDescriptor
                    {
                        ServiceType = kvp.Key,
                        ImplementationType = kvp.Value?.GetType(),
                        Lifetime = ProjectChimera.Core.ServiceLifetime.Singleton,
                        Instance = kvp.Value,
                        RegistrationTime = DateTime.Now
                    };
                }
            }
        }

        /// <summary>
        /// Create child container (simplified)
        /// </summary>
        public IServiceContainer CreateChildContainer()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ServiceLifecycleManager));

            // Return new instance for simplicity
            return new ServiceContainer();
        }

        /// <summary>
        /// Create service scope
        /// </summary>
        public IServiceScope CreateScope()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ServiceLifecycleManager));

            // Return basic scope implementation
            return new EmptyScope();
        }

        /// <summary>
        /// Dispose container and all services
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            lock (_lock)
            {
                // Dispose any disposable services
                foreach (var kvp in _services.ToArray())
                {
                    if (kvp.Value is IDisposable disposableService)
                    {
                        try
                        {
                            disposableService.Dispose();
                            ChimeraLogger.LogInfo("ServiceLifecycleManager", "$1");
                        }
                        catch (Exception ex)
                        {
                            ChimeraLogger.LogInfo("ServiceLifecycleManager", "$1");
                        }
                    }
                }

                var serviceCount = _services.Count;
                _services.Clear();
                _disposed = true;

                ChimeraLogger.LogInfo("ServiceLifecycleManager", "$1");
            }
        }

        /// <summary>
        /// Check if disposed and throw if so
        /// </summary>
        public void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ServiceLifecycleManager));
        }

        /// <summary>
        /// Get container statistics
        /// </summary>
        public ServiceContainerStats GetStats()
        {
            lock (_lock)
            {
                var transientCount = 0;
                var singletonCount = 0;

                foreach (var service in _services.Values)
                {
                    if (service is Func<object>)
                        transientCount++;
                    else
                        singletonCount++;
                }

                return new ServiceContainerStats
                {
                    TotalServices = _services.Count,
                    SingletonServices = singletonCount,
                    TransientServices = transientCount,
                    IsDisposed = _disposed,
                    HasNullServices = _services.Any(kvp => kvp.Value == null)
                };
            }
        }
    }

    internal sealed class EmptyScope : IServiceScope
    {
        public IServiceProvider ServiceProvider => null;
        public void Dispose() { }
    }

    /// <summary>
    /// Service container statistics
    /// </summary>
    [System.Serializable]
    public struct ServiceContainerStats
    {
        public int TotalServices;
        public int SingletonServices;
        public int TransientServices;
        public bool IsDisposed;
        public bool HasNullServices;
    }
}
