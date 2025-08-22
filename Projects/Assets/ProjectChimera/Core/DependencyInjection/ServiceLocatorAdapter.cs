using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectChimera.Core.DependencyInjection
{
    internal class ServiceScope : ProjectChimera.Core.IServiceScope
    {
        private readonly ServiceLocator _parentLocator;
        private readonly Dictionary<Type, object> _scopedInstances = new Dictionary<Type, object>();
        private bool _disposed = false;

        public ProjectChimera.Core.DependencyInjection.IServiceProvider ServiceProvider => new ServiceLocatorProviderAdapter(_parentLocator);

        internal ServiceScope(ServiceLocator parentLocator)
        {
            _parentLocator = parentLocator;
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
                    Debug.LogError($"[ServiceScope] Error disposing scoped service: {ex.Message}");
                }
            }

            _scopedInstances.Clear();
            _parentLocator.RemoveScope(this);
            _disposed = true;
        }
    }

    internal class ServiceLocatorProviderAdapter : ProjectChimera.Core.DependencyInjection.IServiceProvider
    {
        private readonly IServiceLocator _serviceLocator;

        public ServiceLocatorProviderAdapter(IServiceLocator serviceLocator)
        {
            _serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
        }

        public object GetService(Type serviceType)
        {
            try
            {
                return _serviceLocator.TryResolve<object>();
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
                return _serviceLocator.TryResolve<T>();
            }
            catch
            {
                return default;
            }
        }

        public object GetRequiredService(Type serviceType)
        {
            var result = _serviceLocator.Resolve<object>();
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
                var result = _serviceLocator.Resolve<T>();
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
                var method = typeof(IServiceLocator).GetMethod(nameof(IServiceLocator.ResolveAll))?.MakeGenericMethod(serviceType);
                var result = method?.Invoke(_serviceLocator, new object[0]);
                return result as IEnumerable<object> ?? Array.Empty<object>();
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
                return _serviceLocator.ResolveAll<T>();
            }
            catch
            {
                return Array.Empty<T>();
            }
        }
    }
}