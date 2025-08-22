using System;
using System.Collections.Generic;
using ProjectChimera.Core;

namespace ProjectChimera.Core.DependencyInjection
{
    public interface IServiceContainer : IServiceLocator, IDisposable
    {
        void RegisterSingleton<TInterface, TImplementation>() where TImplementation : class, TInterface, new();
        void RegisterSingleton<TInterface>(TInterface instance) where TInterface : class;
        void RegisterSingleton(Type serviceType, object instance);
        void RegisterTransient<TInterface, TImplementation>() where TImplementation : class, TInterface, new();
        void RegisterScoped<TInterface, TImplementation>() where TImplementation : class, TInterface, new();
        void RegisterFactory<TInterface>(Func<IServiceLocator, TInterface> factory) where TInterface : class;

        #region Advanced Registration

        void RegisterCollection<TInterface>(params Type[] implementations) where TInterface : class;
        void RegisterNamed<TInterface, TImplementation>(string name) where TImplementation : class, TInterface, new();
        void RegisterConditional<TInterface, TImplementation>(Func<IServiceLocator, bool> condition) where TImplementation : class, TInterface, new();
        void RegisterDecorator<TInterface, TDecorator>() where TDecorator : class, TInterface where TInterface : class;
        void RegisterWithCallback<TInterface, TImplementation>(Action<TImplementation> initializer) where TImplementation : class, TInterface, new();
        void RegisterOpenGeneric(Type serviceType, Type implementationType);

        #endregion

        #region Advanced Resolution

        T ResolveNamed<T>(string name) where T : class;
        IEnumerable<T> ResolveWhere<T>(Func<T, bool> predicate) where T : class;
        T ResolveOrCreate<T>(Func<T> factory) where T : class;
        T ResolveLast<T>() where T : class;
        T ResolveWithLifetime<T>(ServiceLifetime lifetime) where T : class;
        object Resolve(Type serviceType);
        T TryResolve<T>() where T : class;
        IEnumerable<T> ResolveAll<T>() where T : class;


        #endregion

        #region Container Management

        IServiceScope CreateScope();
        void Clear();
        IDictionary<Type, ServiceRegistration> GetRegistrations();
        IServiceContainer CreateChildContainer();
        ContainerVerificationResult Verify();
        IEnumerable<AdvancedServiceDescriptor> GetServiceDescriptors();
        void Replace<TInterface, TImplementation>() where TImplementation : class, TInterface, new();
        bool Unregister<T>() where T : class;
        bool IsDisposed { get; }
        bool IsRegistered<T>() where T : class;
        bool IsRegistered(Type serviceType);

        #endregion

        #region Event Notifications

        event Action<AdvancedServiceDescriptor> ServiceRegistered;
        event Action<Type, object> ServiceResolved;
        event Action<Type, Exception> ResolutionFailed;

        #endregion
    }

    public class AdvancedServiceDescriptor
    {
        public Type ServiceType { get; set; }
        public Type ImplementationType { get; set; }
        public ServiceLifetime Lifetime { get; set; }
        public string Name { get; set; }
        public Func<IServiceLocator, object> Factory { get; set; }
        public Func<IServiceLocator, bool> Condition { get; set; }
        public object Instance { get; set; }
        public DateTime RegistrationTime { get; set; } = DateTime.Now;
        public int Priority { get; set; } = 0;
        public bool IsDecorator { get; set; } = false;
        public bool IsOpenGeneric { get; set; } = false;
        public string[] Tags { get; set; } = new string[0];
    }

    public class ContainerVerificationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public TimeSpan VerificationTime { get; set; }
        public int TotalServices { get; set; }
        public int VerifiedServices { get; set; }
    }

    public class ServiceCollectionHelper
    {
        public Type ServiceType { get; set; }
        public List<Type> ImplementationTypes { get; set; } = new List<Type>();
        public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Transient;
    }

    public class ConditionalRegistration
    {
        public Type ServiceType { get; set; }
        public Type ImplementationType { get; set; }
        public Func<IServiceLocator, bool> Condition { get; set; }
        public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Transient;
    }
}
