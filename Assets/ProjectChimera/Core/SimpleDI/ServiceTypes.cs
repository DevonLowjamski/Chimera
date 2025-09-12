using System;
using System.Collections.Generic;

namespace ProjectChimera.Core.DependencyInjection
{
    // ServiceLifetime enum moved to ServiceProviderBase.cs to avoid duplication

    /// <summary>
    /// Container verification result
    /// </summary>
    public class ContainerVerificationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public int TotalServices { get; set; }
        public int VerifiedServices { get; set; }
        public TimeSpan VerificationTime { get; set; }
        public List<string> ValidationMessages { get; set; } = new List<string>();
        public DateTime ValidationTimestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Advanced service descriptor
    /// </summary>
    public class AdvancedServiceDescriptor
    {
        public Type ServiceType { get; set; }
        public Type ImplementationType { get; set; }
        public ServiceLifetime Lifetime { get; set; }
        public object Instance { get; set; }
        public Func<object> Factory { get; set; }
        public bool IsGeneric { get; set; }
        public DateTime RegistrationTime { get; set; }
    }

    /// <summary>
    /// Service status enumeration
    /// </summary>
    public enum ServiceStatus
    {
        Unknown,
        Starting,
        Running,
        Stopping,
        Stopped,
        Error,
        Maintenance,
        Healthy,
        Warning,
        Failed
    }

    /// <summary>
    /// Simple service locator interface
    /// </summary>
    public interface IServiceLocator
    {
        T GetService<T>() where T : class;
        object GetService(Type serviceType);
        bool TryGetService<T>(out T service) where T : class;
        bool TryGetService(Type serviceType, out object service);
        IEnumerable<T> GetServices<T>() where T : class;
        IEnumerable<object> GetServices(Type serviceType);
    }
}
