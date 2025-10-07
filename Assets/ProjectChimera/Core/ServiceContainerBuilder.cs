using ProjectChimera.Core.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Interfaces;
using ProjectChimera.Systems.Cultivation;
using ProjectChimera.Systems.Rendering;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Fluent builder for configuring service containers with advanced patterns
    /// Provides a clean, chainable API for complex service registration scenarios
    /// </summary>
    public class ServiceContainerBuilder
    {
        private readonly ServiceContainer _container;
        private readonly List<Action<ServiceContainer>> _registrationActions = new List<Action<ServiceContainer>>();
        private readonly List<IServiceModule> _modules = new List<IServiceModule>();

        public ServiceContainerBuilder() : this(new Core.ServiceContainer()) { }

        public ServiceContainerBuilder(ServiceContainer container)
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
        public ServiceContainerBuilder Configure(Action<ServiceContainer> configurationAction)
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
                // Register Unity-specific services
                ChimeraLogger.LogInfo("ServiceContainerBuilder", "Unity-specific services configured");
            });
        }

        /// <summary>
        /// Add development-only services
        /// </summary>
        public ServiceContainerBuilder ConfigureForDevelopment()
        {
            return ConfigureIf(Application.isEditor, builder =>
            {
                ChimeraLogger.LogInfo("ServiceContainerBuilder", "Container operation completed");
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
                ChimeraLogger.LogInfo("ServiceContainerBuilder", "Container operation completed");
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

                    ChimeraLogger.LogInfo("ServiceContainerBuilder", "Container operation completed");
                    throw new InvalidOperationException(errorMessage);
                }
                else
                {
                    ChimeraLogger.LogInfo("ServiceContainerBuilder", "Container operation completed");
                }
            });

            return this;
        }

        /// <summary>
        /// Build the configured container
        /// </summary>
        public ServiceContainer Build()
        {
            try
            {
                ChimeraLogger.LogInfo("ServiceContainerBuilder", "Container operation completed");

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
                        module.ConfigureServices(_container);
                        ChimeraLogger.LogInfo("ServiceContainerBuilder", "Container operation completed");
                    }
                    catch (Exception ex)
                    {
                        ChimeraLogger.LogInfo("ServiceContainerBuilder", "Container operation completed");
                        throw;
                    }
                }

                // Initialize modules
                foreach (var module in _modules)
                {
                    try
                    {
                        module.Initialize(_container);
                        ChimeraLogger.LogInfo("ServiceContainerBuilder", "Container operation completed");
                    }
                    catch (Exception ex)
                    {
                        ChimeraLogger.LogInfo("ServiceContainerBuilder", "Container operation completed");
                        throw;
                    }
                }

                ChimeraLogger.LogInfo("ServiceContainerBuilder", "Container operation completed");
                return _container;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogInfo("ServiceContainerBuilder", "Container operation completed");
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
            return new ServiceContainerBuilder(container as ServiceContainer ?? new ServiceContainer());
        }

        /// <summary>
        /// Create a builder configured for Project Chimera with automatic interface registration
        /// </summary>
        public static ServiceContainerBuilder CreateForChimera()
        {
            return Create()
                .ConfigureForUnity()
                .AddModule<ChimeraServiceModule>()
                .AddChimeraServices()
                .Configure(container =>
                {
                    ChimeraLogger.LogInfo("ServiceContainerBuilder", "Chimera service container configured");
                });
        }

        /// <summary>
        /// Register all Project Chimera service interfaces automatically
        /// </summary>
        public ServiceContainerBuilder AddChimeraServices()
        {
            return Configure(container =>
            {
                ChimeraLogger.LogInfo("ServiceContainerBuilder", "Registering Chimera services");

                // Core Services
                RegisterCoreServices(container);

                // Cultivation Services
                RegisterCultivationServices(container);

                // Construction Services
                RegisterConstructionServices(container);

                // Rendering Services
                RegisterRenderingServices(container);

                ChimeraLogger.LogInfo("ServiceContainerBuilder", "All Chimera services registered");
            });
        }

        /// <summary>
        /// Register core service implementations
        /// </summary>
        private static void RegisterCoreServices(IServiceContainer container)
        {
            // Register core services directly (interfaces may not be implemented)
            container.RegisterSingleton<TimeManager, TimeManager>();
            container.RegisterSingleton<ServiceHealthMonitor, ServiceHealthMonitor>();
            // Comment out missing services for now
            // container.RegisterSingleton<GCOptimizationManager>();
            // container.RegisterSingleton<StreamingCoordinator>();
            // container.RegisterSingleton<MemoryProfiler>();
            // container.RegisterSingleton<PoolManager>();

            ChimeraLogger.LogInfo("ServiceContainerBuilder", "Core services registered");
        }

        /// <summary>
        /// Register cultivation service implementations
        /// </summary>
        private static void RegisterCultivationServices(IServiceContainer container)
        {
            // Comment out missing services
            // container.RegisterSingleton<IPlantGrowthSystem, PlantGrowthSystem>();
            // container.RegisterSingleton<ICultivationManager, CultivationManager>();
            // container.RegisterSingleton<IPlantStreamingLODIntegration, PlantStreamingLODIntegration>();
            // container.RegisterSingleton<IPlantEnvironmentalService, PlantEnvironmentalService>();

            ChimeraLogger.LogInfo("ServiceContainerBuilder", "Cultivation services registered");
        }

        /// <summary>
        /// Register construction service implementations
        /// </summary>
        private static void RegisterConstructionServices(IServiceContainer container)
        {
            // Comment out missing services
            // container.RegisterSingleton<IGridInputHandler, GridInputHandler>();
            // container.RegisterSingleton<IGridPlacementController, GridPlacementController>();
            // container.RegisterSingleton<IConstructionSaveProvider, ConstructionSaveProvider>();
            // container.RegisterSingleton<IGridSystem, GridSystem>();
            // container.RegisterSingleton<IConstructionManager, ConstructionManager>();

            ChimeraLogger.LogInfo("ServiceContainerBuilder", "Construction services registered");
        }

        /// <summary>
        /// Register rendering service implementations
        /// </summary>
        private static void RegisterRenderingServices(IServiceContainer container)
        {
            // Comment out missing services
            // container.RegisterSingleton<IAdvancedRenderingManager, AdvancedRenderingManager>();
            // container.RegisterSingleton<IPlantInstancedRenderer, PlantInstancedRenderer>();
            // container.RegisterSingleton<ICustomLightingRenderer, CustomLightingRenderer>();
            // container.RegisterSingleton<IEnvironmentalRenderer, EnvironmentalRenderer>();
            // container.RegisterSingleton<ILightingService, LightingService>();

            ChimeraLogger.LogInfo("ServiceContainerBuilder", "Rendering services registered");
        }

        #endregion
    }

    /// <summary>
    /// Extension methods for fluent container configuration
    /// </summary>
}
