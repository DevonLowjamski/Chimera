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
    public static class ServiceContainerBuilderExtensions
    {
        /// <summary>
        /// Register all implementations of an interface from an assembly
        /// </summary>
        public static ServiceContainerBuilder AddImplementationsOf<TInterface>(this ServiceContainerBuilder builder)
            where TInterface : class
        {
            // Scan assemblies for implementations of TInterface
            var interfaceType = typeof(TInterface);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && a.Location.Contains("ProjectChimera"))
                .ToArray();

            foreach (var assembly in assemblies)
            {
                try
                {
                    var implementations = assembly.GetTypes()
                        .Where(t => !t.IsInterface && !t.IsAbstract && interfaceType.IsAssignableFrom(t))
                        .ToArray();

                    foreach (var impl in implementations)
                    {
                        builder.Configure(container =>
                        {
                            container.RegisterSingleton(interfaceType, impl);
                            ChimeraLogger.LogInfo("ServiceContainerBuilder", $"Registered {impl.Name} as {interfaceType.Name}");
                        });
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    ChimeraLogger.LogWarning("ServiceContainerBuilder", $"Could not load types from {assembly.FullName}: {ex.Message}");
                }
            }

            return builder;
        }

        /// <summary>
        /// Register services based on naming conventions
        /// </summary>
        public static ServiceContainerBuilder AddByConvention(this ServiceContainerBuilder builder, Func<Type, bool> serviceFilter = null)
        {
            return builder.Configure(container =>
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic && a.Location.Contains("ProjectChimera"))
                    .ToArray();

                foreach (var assembly in assemblies)
                {
                    try
                    {
                        var serviceTypes = assembly.GetTypes()
                            .Where(t => !t.IsInterface && !t.IsAbstract)
                            .Where(t => serviceFilter?.Invoke(t) != false)
                            .Where(t => t.Name.EndsWith("Service") || t.Name.EndsWith("Manager"))
                            .ToArray();

                        foreach (var serviceType in serviceTypes)
                        {
                            var interfaces = serviceType.GetInterfaces()
                                .Where(i => i.IsPublic && i.Namespace?.StartsWith("ProjectChimera") == true)
                                .ToArray();

                            foreach (var interfaceType in interfaces)
                            {
                                container.RegisterSingleton(interfaceType, serviceType);
                                ChimeraLogger.LogInfo("ServiceContainerBuilder", $"Registered {serviceType.Name} as {interfaceType.Name} by convention");
                            }
                        }
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        ChimeraLogger.LogWarning("ServiceContainerBuilder", $"Could not load types from {assembly.FullName}: {ex.Message}");
                    }
                }
            });
        }

        /// <summary>
        /// Add all managers from Project Chimera using reflection
        /// </summary>
        public static ServiceContainerBuilder AddChimeraManagers(this ServiceContainerBuilder builder)
        {
            return builder.Configure(container =>
            {
                ChimeraLogger.LogInfo("ServiceContainerBuilder", "Registering all Chimera managers");

                // Register managers automatically using reflection
                var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic && a.Location.Contains("ProjectChimera"))
                    .ToArray();

                foreach (var assembly in assemblies)
                {
                    try
                    {
                        var managerTypes = assembly.GetTypes()
                            .Where(t => !t.IsInterface && !t.IsAbstract)
                            .Where(t => t.Name.EndsWith("Manager") && t.Namespace?.StartsWith("ProjectChimera") == true)
                            .ToArray();

                        foreach (var managerType in managerTypes)
                        {
                            var interfaces = managerType.GetInterfaces()
                                .Where(i => i.Namespace?.StartsWith("ProjectChimera") == true)
                                .ToArray();

                            foreach (var interfaceType in interfaces)
                            {
                                container.RegisterSingleton(interfaceType, managerType);
                                ChimeraLogger.LogInfo("ServiceContainerBuilder", $"Auto-registered {managerType.Name} as {interfaceType.Name}");
                            }
                        }
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        ChimeraLogger.LogWarning("ServiceContainerBuilder", $"Could not load types from {assembly.FullName}: {ex.Message}");
                    }
                }
            });
        }
    }
}