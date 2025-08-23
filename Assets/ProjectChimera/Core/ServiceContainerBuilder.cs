using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Core;
using ServiceLocatorAdapter = ProjectChimera.Core.ServiceLocatorAdapter;

namespace ProjectChimera.Core.DependencyInjection
{
    /// <summary>
    /// Fluent builder for configuring service containers with advanced patterns
    /// Provides a clean, chainable API for complex service registration scenarios
    /// </summary>
    public class ServiceContainerBuilder
    {
        private readonly IServiceContainer _container;
        private readonly List<Action<IServiceContainer>> _registrationActions = new List<Action<IServiceContainer>>();
        private readonly List<IServiceModule> _modules = new List<IServiceModule>();

        public ServiceContainerBuilder() : this(new Core.ServiceContainer()) { }

        public ServiceContainerBuilder(IServiceContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        #region Basic Registration

        /// <summary>
        /// Register a singleton service
        /// </summary>
        public ServiceContainerBuilder AddSingleton<TInterface, TImplementation>()
            where TImplementation : class, TInterface, new()
        {
            _registrationActions.Add(container => container.RegisterSingleton<TInterface, TImplementation>());
            return this;
        }

        /// <summary>
        /// Register a singleton instance
        /// </summary>
        public ServiceContainerBuilder AddSingleton<TInterface>(TInterface instance)
            where TInterface : class
        {
            _registrationActions.Add(container => container.RegisterSingleton(instance));
            return this;
        }

        /// <summary>
        /// Register a transient service
        /// </summary>
        public ServiceContainerBuilder AddTransient<TInterface, TImplementation>()
            where TImplementation : class, TInterface, new()
        {
            _registrationActions.Add(container => container.RegisterTransient<TInterface, TImplementation>());
            return this;
        }

        /// <summary>
        /// Register a scoped service
        /// </summary>
        public ServiceContainerBuilder AddScoped<TInterface, TImplementation>()
            where TImplementation : class, TInterface, new()
        {
            _registrationActions.Add(container => container.RegisterScoped<TInterface, TImplementation>());
            return this;
        }

        /// <summary>
        /// Register a factory function
        /// </summary>
        public ServiceContainerBuilder AddFactory<TInterface>(Func<IServiceLocator, TInterface> factory)
            where TInterface : class
        {
            _registrationActions.Add(container => container.RegisterFactory(factory));
            return this;
        }

        #endregion

        #region Advanced Registration

        /// <summary>
        /// Register multiple implementations as a collection
        /// </summary>
        public ServiceContainerBuilder AddCollection<TInterface>(params Type[] implementations)
            where TInterface : class
        {
            _registrationActions.Add(container => container.RegisterCollection<TInterface>(implementations));
            return this;
        }

        /// <summary>
        /// Register a named service
        /// </summary>
        public ServiceContainerBuilder AddNamed<TInterface, TImplementation>(string name)
            where TInterface : class
            where TImplementation : class, TInterface, new()
        {
            _registrationActions.Add(container => container.RegisterNamed<TInterface, TImplementation>(name));
            return this;
        }

        /// <summary>
        /// Register a conditional service
        /// </summary>
        public ServiceContainerBuilder AddConditional<TInterface, TImplementation>(Func<IServiceLocator, bool> condition)
            where TInterface : class
            where TImplementation : class, TInterface, new()
        {
            _registrationActions.Add(container => container.RegisterConditional<TInterface, TImplementation>(condition));
            return this;
        }

        /// <summary>
        /// Register a decorator
        /// </summary>
        public ServiceContainerBuilder AddDecorator<TInterface, TDecorator>()
            where TInterface : class
            where TDecorator : class, TInterface, new()
        {
            _registrationActions.Add(container => container.RegisterDecorator<TInterface, TDecorator>());
            return this;
        }

        /// <summary>
        /// Register a service with initialization callback
        /// </summary>
        public ServiceContainerBuilder AddWithCallback<TInterface, TImplementation>(Action<TImplementation> initializer)
            where TInterface : class
            where TImplementation : class, TInterface, new()
        {
            _registrationActions.Add(container => container.RegisterWithCallback<TInterface, TImplementation>(initializer));
            return this;
        }

        /// <summary>
        /// Register an open generic type
        /// </summary>
        public ServiceContainerBuilder AddOpenGeneric(Type serviceType, Type implementationType)
        {
            _registrationActions.Add(container => container.RegisterOpenGeneric(serviceType, implementationType));
            return this;
        }

        #endregion

        #region Module Registration

        /// <summary>
        /// Add a service module
        /// </summary>
        public ServiceContainerBuilder AddModule(IServiceModule module)
        {
            if (module != null)
            {
                _modules.Add(module);
            }
            return this;
        }

        /// <summary>
        /// Add multiple service modules
        /// </summary>
        public ServiceContainerBuilder AddModules(params IServiceModule[] modules)
        {
            foreach (var module in modules)
            {
                AddModule(module);
            }
            return this;
        }

        /// <summary>
        /// Add a module by type (must have parameterless constructor)
        /// </summary>
        public ServiceContainerBuilder AddModule<TModule>() where TModule : IServiceModule, new()
        {
            return AddModule(new TModule());
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Configure the container with a custom action
        /// </summary>
        public ServiceContainerBuilder Configure(Action<IServiceContainer> configurationAction)
        {
            if (configurationAction != null)
            {
                _registrationActions.Add(configurationAction);
            }
            return this;
        }

        /// <summary>
        /// Add conditional configuration based on environment
        /// </summary>
        public ServiceContainerBuilder ConfigureIf(bool condition, Action<ServiceContainerBuilder> configuration)
        {
            if (condition && configuration != null)
            {
                configuration(this);
            }
            return this;
        }

        /// <summary>
        /// Add Unity-specific configurations
        /// </summary>
        public ServiceContainerBuilder ConfigureForUnity()
        {
            return Configure(container =>
            {
                // Add Unity-specific service registrations
                Debug.Log("[ServiceContainerBuilder] Configured for Unity environment");
            });
        }

        /// <summary>
        /// Add development-only services
        /// </summary>
        public ServiceContainerBuilder ConfigureForDevelopment()
        {
            return ConfigureIf(Application.isEditor, builder =>
            {
                Debug.Log("[ServiceContainerBuilder] Adding development services");
                // Add development-specific services here
            });
        }

        /// <summary>
        /// Add production-only services
        /// </summary>
        public ServiceContainerBuilder ConfigureForProduction()
        {
            return ConfigureIf(!Application.isEditor, builder =>
            {
                Debug.Log("[ServiceContainerBuilder] Adding production services");
                // Add production-specific services here
            });
        }

        #endregion

        #region Validation and Building

        /// <summary>
        /// Validate all registrations before building
        /// </summary>
        public ServiceContainerBuilder Validate()
        {
            _registrationActions.Add(container =>
            {
                var result = container.Verify();
                
                if (!result.IsValid)
                {
                    var errorMessage = $"Container validation failed with {result.Errors.Count} errors:\n" +
                                     string.Join("\n", result.Errors);
                    
                    Debug.LogError($"[ServiceContainerBuilder] {errorMessage}");
                    throw new InvalidOperationException(errorMessage);
                }
                else
                {
                    Debug.Log($"[ServiceContainerBuilder] Container validation passed: {result.VerifiedServices}/{result.TotalServices} services verified in {result.VerificationTime.TotalMilliseconds:F2}ms");
                }
            });
            
            return this;
        }

        /// <summary>
        /// Build the configured container
        /// </summary>
        public IServiceContainer Build()
        {
            try
            {
                Debug.Log("[ServiceContainerBuilder] Building service container");

                // Apply all registration actions
                foreach (var action in _registrationActions)
                {
                    action(_container);
                }

                // Configure modules
                foreach (var module in _modules)
                {
                    try
                    {
                        module.ConfigureServices(new ServiceLocatorAdapter(_container));
                        Debug.Log($"[ServiceContainerBuilder] Module '{module.ModuleName}' configured");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[ServiceContainerBuilder] Error configuring module '{module.ModuleName}': {ex.Message}");
                        throw;
                    }
                }

                // Initialize modules
                foreach (var module in _modules)
                {
                    try
                    {
                        module.Initialize(new ServiceLocatorAdapter(_container));
                        Debug.Log($"[ServiceContainerBuilder] Module '{module.ModuleName}' initialized");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[ServiceContainerBuilder] Error initializing module '{module.ModuleName}': {ex.Message}");
                        throw;
                    }
                }

                Debug.Log($"[ServiceContainerBuilder] Container built successfully with {_registrationActions.Count} registrations and {_modules.Count} modules");
                return _container;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ServiceContainerBuilder] Container build failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Build and validate the container
        /// </summary>
        public IServiceContainer BuildAndValidate()
        {
            return Validate().Build();
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Create a new builder with a fresh container
        /// </summary>
        public static ServiceContainerBuilder Create()
        {
            return new ServiceContainerBuilder();
        }

        /// <summary>
        /// Create a builder with an existing container
        /// </summary>
        public static ServiceContainerBuilder Create(IServiceContainer container)
        {
            return new ServiceContainerBuilder(container);
        }

        /// <summary>
        /// Create a builder configured for Project Chimera
        /// </summary>
        public static ServiceContainerBuilder CreateForChimera()
        {
            return Create()
                .ConfigureForUnity()
                .AddModule<ChimeraServiceModule>()
                .Configure(container =>
                {
                    Debug.Log("[ServiceContainerBuilder] Configured for Project Chimera");
                });
        }

        #endregion
    }

    /// <summary>
    /// Extension methods for fluent container configuration
    /// </summary>
    public static class ServiceContainerBuilderExtensions
    {
        /// <summary>
        /// Register all implementations of an interface from an assembly
        /// </summary>
        public static ServiceContainerBuilder AddImplementationsOf<TInterface>(this ServiceContainerBuilder builder)
            where TInterface : class
        {
            // This would use reflection to find all implementations
            // For now, we'll just log the intent
            Debug.Log($"[ServiceContainerBuilder] Would register all implementations of {typeof(TInterface).Name}");
            return builder;
        }

        /// <summary>
        /// Register services based on naming conventions
        /// </summary>
        public static ServiceContainerBuilder AddByConvention(this ServiceContainerBuilder builder, Func<Type, bool> serviceFilter = null)
        {
            // This would use reflection and naming conventions
            Debug.Log("[ServiceContainerBuilder] Would register services by convention");
            return builder;
        }

        /// <summary>
        /// Add all managers from Project Chimera
        /// </summary>
        public static ServiceContainerBuilder AddChimeraManagers(this ServiceContainerBuilder builder)
        {
            return builder.Configure(container =>
            {
                Debug.Log("[ServiceContainerBuilder] Would register all Chimera managers");
                // This would register all the manager interfaces
            });
        }
    }
}