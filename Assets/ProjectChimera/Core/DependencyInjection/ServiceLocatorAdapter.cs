using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.DependencyInjection
{
    internal class ServiceScope : ProjectChimera.Core.IServiceScope
    {
        private readonly IServiceContainer _parentContainer;
        private readonly Dictionary<Type, object> _scopedInstances = new Dictionary<Type, object>();
        private bool _disposed = false;

        public ProjectChimera.Core.DependencyInjection.IServiceProvider ServiceProvider => new ServiceContainerProviderAdapter(_parentContainer);

        internal ServiceScope(IServiceContainer parentContainer)
        {
            _parentContainer = parentContainer;
        }

        public void Dispose()
        {
            if (_disposed) return;

            foreach (var instance in _scopedInstances.Values.OfType<IDisposable>())
            {
                try
                {
                    instance.Dispose();
                }
                catch (Exception ex)
                {
                    ChimeraLogger.LogError($"[ServiceScope] Error disposing scoped service: {ex.Message}");
                }
            }

            _scopedInstances.Clear();
            // Note: ServiceContainer doesn't have RemoveScope - scopes are managed internally
            _disposed = true;
        }
    }

    internal class ServiceContainerProviderAdapter : ProjectChimera.Core.DependencyInjection.IServiceProvider
    {
        private readonly IServiceContainer _serviceContainer;

        public ServiceContainerProviderAdapter(IServiceContainer serviceContainer)
        {
            _serviceContainer = serviceContainer ?? throw new ArgumentNullException(nameof(serviceContainer));
        }

        public object GetService(Type serviceType)
        {
            try
            {
                return _serviceContainer.TryResolve(serviceType);
            }
            catch
            {
                return null;
            }
        }

        public T GetService<T>() where T : class
        {
            try
            {
                return _serviceContainer.TryResolve<T>();
            }
            catch
            {
                return default;
            }
        }

        public object GetRequiredService(Type serviceType)
        {
            var result = _serviceContainer.Resolve(serviceType);
            if (result == null)
            {
                throw new InvalidOperationException($"Required service of type {serviceType.Name} could not be resolved");
            }
            return result;
        }

        public T GetRequiredService<T>() where T : class
        {
            try
            {
                var result = _serviceContainer.Resolve<T>();
                if (result == null)
                {
                    throw new InvalidOperationException($"Required service of type {typeof(T).Name} could not be resolved");
                }
                return result;
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                throw new InvalidOperationException($"Required service of type {typeof(T).Name} could not be resolved", ex);
            }
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            try
            {
                // ServiceContainer doesn't have ResolveAll for non-generic types
                // Return single service as enumerable if available
                var service = _serviceContainer.TryResolve(serviceType);
                return service != null ? new[] { service } : Array.Empty<object>();
            }
            catch
            {
                return Array.Empty<object>();
            }
        }

        public IEnumerable<T> GetServices<T>() where T : class
        {
            try
            {
                // ServiceContainer doesn't have ResolveAll - return single service as enumerable
                var service = _serviceContainer.TryResolve<T>();
                return service != null ? new[] { service } : Array.Empty<T>();
            }
            catch
            {
                return Array.Empty<T>();
            }
        }
    }
}