using ProjectChimera.Core.Logging;
using ProjectChimera.Core;
using ProjectChimera.Core.Updates;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectChimera.Core.DependencyInjection
{
    /// <summary>
    /// Bootstrap component that automatically sets up the dependency injection system
    /// Add this component to your main GameManager or create a dedicated bootstrapper GameObject
    /// </summary>
    public class ServiceBootstrapper : MonoBehaviour
    {
        [Header("Bootstrapper Configuration")]
        [SerializeField] private bool _initializeOnAwake = true;
        [SerializeField] private bool _registerCoreModule = true;
        [SerializeField] private bool _enableDebugLogging = true;

        [Header("Module Registration")]
        [SerializeField] private string[] _moduleAssemblies = new string[]
        {
            "ProjectChimera.Systems.Cultivation",
            "ProjectChimera.Systems.Environment", 
            "ProjectChimera.Systems.Genetics",
            "ProjectChimera.Systems.Economy"
        };

        private IServiceContainer _serviceContainer;
        private bool _isBootstrapped = false;

        #region Unity Lifecycle

        private void Awake()
        {
            if (_initializeOnAwake)
            {
                BootstrapServices();
            }
        }

        private void Start()
        {
            if (!_isBootstrapped)
            {
                BootstrapServices();
            }
        }

        #endregion

        #region Bootstrapping

        /// <summary>
        /// Bootstrap the dependency injection system
        /// </summary>
        public void BootstrapServices()
        {
            if (_isBootstrapped)
            {
                LogBootstrap("Services already bootstrapped");
                return;
            }

            try
            {
                LogBootstrap("Starting service bootstrap");

                // Initialize ServiceContainer (unified DI system)
                _serviceContainer = ServiceContainerFactory.Instance;
                
                // Register core services
                if (_registerCoreModule)
                {
                    RegisterCoreServices();
                }

                // Auto-discover and register modules
                AutoDiscoverModules();

                // Validate all service registrations
                ValidateServiceRegistrations();

                _isBootstrapped = true;
                LogBootstrap("Service bootstrap completed successfully");
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError($"[ServiceBootstrapper] Bootstrap failed: {ex.Message}");
            }
        }

        private void RegisterCoreServices()
        {
            LogBootstrap("Registering core services with unified ServiceContainer");
            
            // Register ServiceContainer itself
            if (!_serviceContainer.IsRegistered<IServiceContainer>())
            {
                _serviceContainer.RegisterSingleton<IServiceContainer>(_serviceContainer);
            }
            
            // Register UpdateOrchestrator
            var updateOrchestrator = UpdateOrchestrator.Instance;
            if (!_serviceContainer.IsRegistered<UpdateOrchestrator>())
            {
                _serviceContainer.RegisterSingleton<UpdateOrchestrator>(updateOrchestrator);
                _serviceContainer.RegisterSingleton<IUpdateOrchestrator>(updateOrchestrator);
            }
            
            // Register additional core services as they become available
            RegisterSceneServices();
            
            LogBootstrap("Core services registered with ServiceContainer");
        }
        
        private void RegisterSceneServices()
        {
            LogBootstrap("Registering scene services");
            
            // Find and register common manager types in the scene
            var managers = FindObjectsOfType<MonoBehaviour>();
            int registeredCount = 0;
            
            foreach (var manager in managers)
            {
                var type = manager.GetType();
                
                // Register managers by type name patterns
                if ((type.Name.EndsWith("Manager") || type.Name.EndsWith("Service")) && 
                    !_serviceContainer.IsRegistered(type))
                {
                    _serviceContainer.RegisterSingleton(type, manager);
                    registeredCount++;
                    
                    // Also register by interfaces
                    var interfaces = type.GetInterfaces();
                    foreach (var interfaceType in interfaces)
                    {
                        if (interfaceType.Name.StartsWith("I") && !_serviceContainer.IsRegistered(interfaceType))
                        {
                            _serviceContainer.RegisterSingleton(interfaceType, manager);
                        }
                    }
                }
            }
            
            LogBootstrap($"Registered {registeredCount} scene services with ServiceContainer");
        }

        private void AutoDiscoverModules()
        {
            LogBootstrap("Auto-discovering additional services");

            // Register specialized services that may not follow naming conventions
            RegisterSpecializedServices();
            
            LogBootstrap($"Service auto-discovery completed");
        }
        
        private void RegisterSpecializedServices()
        {
            // Register AddressablesInfrastructure if present (using reflection to avoid circular dependency)
            var addressablesType = System.Type.GetType("ProjectChimera.Systems.Addressables.AddressablesInfrastructure, ProjectChimera.Systems.Addressables");
            if (addressablesType != null)
            {
                // Find by name instead of FindObjectOfType to avoid anti-pattern
                var addressablesGameObject = GameObject.Find("AddressablesInfrastructure");
                if (addressablesGameObject != null)
                {
                    var addressables = addressablesGameObject.GetComponent(addressablesType);
                    if (addressables != null)
                    {
                        // Use reflection to register the service to avoid type dependency
                        var registerMethod = typeof(ServiceContainer).GetMethod("RegisterSingleton").MakeGenericMethod(addressablesType);
                        registerMethod.Invoke(_serviceContainer, new[] { addressables });
                        LogBootstrap("Registered AddressablesInfrastructure");
                    }
                }
            }
            
            // Register other specialized services as needed
            // This method can be expanded as new services are added
        }

        /// <summary>
        /// Validate all critical service registrations at startup
        /// This catches missing dependencies early and provides clear error messages
        /// </summary>
        private void ValidateServiceRegistrations()
        {
            LogBootstrap("Validating service registrations...");

            var container = ServiceContainerFactory.Instance;
            if (container == null)
            {
                ChimeraLogger.LogError("[ServiceBootstrapper] ServiceContainerFactory.Instance is null - critical system failure");
                return;
            }

            var validationResults = new System.Collections.Generic.List<ServiceValidationResult>();

            // Validate Core Manager Interfaces
            validationResults.Add(ValidateService<ITimeManager>(container, "ITimeManager", true));
            validationResults.Add(ValidateService<IDataManager>(container, "IDataManager", true));
            validationResults.Add(ValidateService<IEventManager>(container, "IEventManager", true));
            validationResults.Add(ValidateService<ISettingsManager>(container, "ISettingsManager", true));

            // Validate UI System Interfaces
            validationResults.Add(ValidateService<IUIManager>(container, "IUIManager", false));
            validationResults.Add(ValidateService<ISchematicLibraryPanel>(container, "ISchematicLibraryPanel", false));
            validationResults.Add(ValidateService<IConstructionPaletteManager>(container, "IConstructionPaletteManager", false));

            // Validate Construction System Interfaces
            validationResults.Add(ValidateService<IGridPlacementController>(container, "IGridPlacementController", false));
            validationResults.Add(ValidateService<IGridSystem>(container, "IGridSystem", false));
            validationResults.Add(ValidateService<IInteractiveFacilityConstructor>(container, "IInteractiveFacilityConstructor", false));
            validationResults.Add(ValidateService<IConstructionCostManager>(container, "IConstructionCostManager", false));

            // Validate Asset Service Interfaces
            validationResults.Add(ValidateService<ISchematicAssetService>(container, "ISchematicAssetService", false));
            
            // Validate Update Management Interfaces
            validationResults.Add(ValidateService<IUpdateOrchestrator>(container, "IUpdateOrchestrator", true));

            // Validate Domain Manager Interfaces
            validationResults.Add(ValidateService<IPlantManager>(container, "IPlantManager", false));
            validationResults.Add(ValidateService<IGeneticsManager>(container, "IGeneticsManager", false));
            validationResults.Add(ValidateService<IEnvironmentalManager>(container, "IEnvironmentalManager", false));
            validationResults.Add(ValidateService<IEconomyManager>(container, "IEconomyManager", false));
            validationResults.Add(ValidateService<IProgressionManager>(container, "IProgressionManager", false));
            validationResults.Add(ValidateService<IResearchManager>(container, "IResearchManager", false));
            validationResults.Add(ValidateService<IAudioManager>(container, "IAudioManager", false));

            // Generate validation report
            GenerateValidationReport(validationResults);
        }

        /// <summary>
        /// Validate a specific service registration
        /// </summary>
        private ServiceValidationResult ValidateService<T>(IServiceContainer container, string serviceName, bool isCritical) where T : class
        {
            var result = new ServiceValidationResult
            {
                ServiceName = serviceName,
                IsCritical = isCritical,
                IsRegistered = false,
                IsNullImplementation = false,
                ErrorMessage = null
            };

            try
            {
                var service = container.TryResolve<T>();
                if (service != null)
                {
                    result.IsRegistered = true;
                    
                    // Check if it's a null implementation (starts with "Null")
                    var typeName = service.GetType().Name;
                    result.IsNullImplementation = typeName.StartsWith("Null");
                    
                    if (result.IsNullImplementation)
                    {
                        LogBootstrap($"Service '{serviceName}' resolved to null implementation '{typeName}'");
                    }
                    else
                    {
                        LogBootstrap($"Service '{serviceName}' resolved to concrete implementation '{typeName}'");
                    }
                }
                else
                {
                    result.ErrorMessage = "Service returned null from container";
                    if (isCritical)
                    {
                        ChimeraLogger.LogError($"[ServiceBootstrapper] CRITICAL: Service '{serviceName}' not available");
                    }
                    else
                    {
                        ChimeraLogger.LogWarning($"[ServiceBootstrapper] Service '{serviceName}' not available - using fallback");
                    }
                }
            }
            catch (System.Exception ex)
            {
                result.ErrorMessage = ex.Message;
                ChimeraLogger.LogError($"[ServiceBootstrapper] Error validating service '{serviceName}': {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Generate comprehensive validation report
        /// </summary>
        private void GenerateValidationReport(System.Collections.Generic.List<ServiceValidationResult> results)
        {
            var registeredCount = results.Count(r => r.IsRegistered);
            var nullImplementationCount = results.Count(r => r.IsNullImplementation);
            var criticalFailuresCount = results.Count(r => r.IsCritical && !r.IsRegistered);
            var warningCount = results.Count(r => !r.IsCritical && !r.IsRegistered);

            LogBootstrap($"Service Validation Report:");
            LogBootstrap($"  Total Services: {results.Count}");
            LogBootstrap($"  Registered: {registeredCount}");
            LogBootstrap($"  Using Null Implementations: {nullImplementationCount}");
            LogBootstrap($"  Critical Failures: {criticalFailuresCount}");
            LogBootstrap($"  Warnings: {warningCount}");

            if (criticalFailuresCount > 0)
            {
                ChimeraLogger.LogError($"[ServiceBootstrapper] {criticalFailuresCount} critical service failures detected - system may not function properly");
                
                foreach (var failure in results.Where(r => r.IsCritical && !r.IsRegistered))
                {
                    ChimeraLogger.LogError($"[ServiceBootstrapper] CRITICAL FAILURE: {failure.ServiceName} - {failure.ErrorMessage}");
                }
            }

            if (_enableDebugLogging && nullImplementationCount > 0)
            {
                LogBootstrap($"Services using null implementations (expected during development):");
                foreach (var nullService in results.Where(r => r.IsNullImplementation))
                {
                    LogBootstrap($"  - {nullService.ServiceName}");
                }
            }

            LogBootstrap("Service validation completed");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get a service from the dependency injection system
        /// </summary>
        public T GetService<T>() where T : class
        {
            if (!_isBootstrapped)
            {
                LogBootstrap("Services not bootstrapped yet - attempting bootstrap");
                BootstrapServices();
            }

            return _serviceContainer?.TryResolve<T>();
        }

        /// <summary>
        /// Check if the service system is ready
        /// </summary>
        public bool IsBootstrapped => _isBootstrapped && _serviceContainer != null;

        /// <summary>
        /// Get the service manager instance
        /// </summary>
        public IServiceContainer GetServiceContainer()
        {
            return _serviceContainer;
        }

        #endregion

        #region Utility

        private void LogBootstrap(string message)
        {
            if (_enableDebugLogging)
            {
                ChimeraLogger.Log($"[ServiceBootstrapper] {message}");
            }
        }

        #endregion

        #region Static Access

        private static ServiceBootstrapper _instance;
        
        /// <summary>
        /// Global bootstrapper instance for easy access
        /// Replaces FindObjectOfType anti-pattern with proper DI container registration
        /// </summary>
        public static ServiceBootstrapper Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Try to get from DI container first
                    try
                    {
                        _instance = ServiceContainerFactory.Instance?.TryResolve<ServiceBootstrapper>();
                    }
                    catch (Exception ex)
                    {
                        ChimeraLogger.LogWarning($"[ServiceBootstrapper] Failed to resolve from DI container: {ex.Message}");
                    }
                    
                    if (_instance == null)
                    {
                        ChimeraLogger.LogWarning("[ServiceBootstrapper] No ServiceBootstrapper registered in DI container");
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Quick access to get services globally
        /// </summary>
        public static T GetGlobalService<T>() where T : class
        {
            return Instance?.GetService<T>();
        }

        /// <summary>
        /// Run service validation on-demand (useful for debugging and health checks)
        /// </summary>
        public void RunServiceValidation()
        {
            if (_isBootstrapped)
            {
                ValidateServiceRegistrations();
            }
            else
            {
                ChimeraLogger.LogWarning("[ServiceBootstrapper] Cannot validate services - system not bootstrapped yet");
            }
        }

        /// <summary>
        /// Get validation status for all registered services
        /// </summary>
        public System.Collections.Generic.List<ServiceValidationResult> GetServiceValidationStatus()
        {
            var container = ServiceContainerFactory.Instance;
            if (container == null || !_isBootstrapped)
            {
                return new System.Collections.Generic.List<ServiceValidationResult>();
            }

            var results = new System.Collections.Generic.List<ServiceValidationResult>();

            // Validate all registered services
            results.Add(ValidateService<ITimeManager>(container, "ITimeManager", true));
            results.Add(ValidateService<IDataManager>(container, "IDataManager", true));
            results.Add(ValidateService<IEventManager>(container, "IEventManager", true));
            results.Add(ValidateService<ISettingsManager>(container, "ISettingsManager", true));
            results.Add(ValidateService<IUIManager>(container, "IUIManager", false));
            results.Add(ValidateService<ISchematicLibraryPanel>(container, "ISchematicLibraryPanel", false));
            results.Add(ValidateService<IConstructionPaletteManager>(container, "IConstructionPaletteManager", false));
            results.Add(ValidateService<IGridPlacementController>(container, "IGridPlacementController", false));
            results.Add(ValidateService<IGridSystem>(container, "IGridSystem", false));
            results.Add(ValidateService<IInteractiveFacilityConstructor>(container, "IInteractiveFacilityConstructor", false));
            results.Add(ValidateService<IConstructionCostManager>(container, "IConstructionCostManager", false));
            results.Add(ValidateService<ISchematicAssetService>(container, "ISchematicAssetService", false));
            results.Add(ValidateService<IUpdateOrchestrator>(container, "IUpdateOrchestrator", true));
            results.Add(ValidateService<IPlantManager>(container, "IPlantManager", false));
            results.Add(ValidateService<IGeneticsManager>(container, "IGeneticsManager", false));
            results.Add(ValidateService<IEnvironmentalManager>(container, "IEnvironmentalManager", false));
            results.Add(ValidateService<IEconomyManager>(container, "IEconomyManager", false));
            results.Add(ValidateService<IProgressionManager>(container, "IProgressionManager", false));
            results.Add(ValidateService<IResearchManager>(container, "IResearchManager", false));
            results.Add(ValidateService<IAudioManager>(container, "IAudioManager", false));

            return results;
        }

        /// <summary>
        /// Generate comprehensive service registration report
        /// </summary>
        public ServiceRegistrationReport GenerateServiceReport()
        {
            var report = new ServiceRegistrationReport
            {
                GeneratedAt = System.DateTime.Now,
                IsBootstrapped = _isBootstrapped,
                ServiceManagerInitialized = _serviceContainer != null
            };

            var container = ServiceContainerFactory.Instance;
            if (container == null)
            {
                report.Errors.Add("ServiceContainerFactory.Instance is null - critical system failure");
                return report;
            }

            // Get validation results
            var validationResults = GetServiceValidationStatus();
            report.ValidationResults = validationResults;

            // Calculate statistics
            report.TotalServices = validationResults.Count;
            report.RegisteredServices = validationResults.Count(r => r.IsRegistered);
            report.CriticalServices = validationResults.Count(r => r.IsCritical);
            report.CriticalFailures = validationResults.Count(r => r.IsCritical && !r.IsRegistered);
            report.NullImplementations = validationResults.Count(r => r.IsNullImplementation);
            report.Warnings = validationResults.Count(r => !r.IsCritical && !r.IsRegistered);

            // Service health assessment
            if (report.CriticalFailures == 0)
            {
                report.OverallHealth = ServiceHealth.Healthy;
            }
            else if (report.CriticalFailures <= 2)
            {
                report.OverallHealth = ServiceHealth.Warning;
            }
            else
            {
                report.OverallHealth = ServiceHealth.Critical;
            }

            // Performance metrics (if available)
            try
            {
                var serviceContainer = _serviceContainer;
                if (serviceContainer != null)
                {
                    report.ServiceManagerMetrics = new ServiceManagerMetrics
                    {
                        IsInitialized = serviceContainer != null,
                        InitializationTime = (double)GetInitializationTime(),
                        RegisteredModules = GetRegisteredModuleCount()
                    };
                }
            }
            catch (System.Exception ex)
            {
                report.Errors.Add($"Failed to gather service manager metrics: {ex.Message}");
            }

            // Dependency analysis
            AnalyzeDependencies(report, validationResults);

            return report;
        }

        /// <summary>
        /// Print service registration report to console
        /// </summary>
        public void PrintServiceReport()
        {
            var report = GenerateServiceReport();
            
            ChimeraLogger.Log("=== PROJECT CHIMERA SERVICE REGISTRATION REPORT ===");
            ChimeraLogger.Log($"Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
            ChimeraLogger.Log($"Overall Health: {report.OverallHealth}");
            ChimeraLogger.Log("");

            ChimeraLogger.Log("SYSTEM STATUS:");
            ChimeraLogger.Log($"  Bootstrapped: {report.IsBootstrapped}");
            ChimeraLogger.Log($"  Service Manager: {(report.ServiceManagerInitialized ? "Initialized" : "Not Initialized")}");
            ChimeraLogger.Log("");

            ChimeraLogger.Log("SERVICE STATISTICS:");
            ChimeraLogger.Log($"  Total Services: {report.TotalServices}");
            ChimeraLogger.Log($"  Registered: {report.RegisteredServices}/{report.TotalServices}");
            ChimeraLogger.Log($"  Critical Services: {report.CriticalServices}");
            ChimeraLogger.Log($"  Critical Failures: {report.CriticalFailures}");
            ChimeraLogger.Log($"  Null Implementations: {report.NullImplementations}");
            ChimeraLogger.Log($"  Warnings: {report.Warnings}");
            ChimeraLogger.Log("");

            if (report.ServiceManagerMetrics != null)
            {
                ChimeraLogger.Log("PERFORMANCE METRICS:");
                ChimeraLogger.Log($"  Initialization Time: {report.ServiceManagerMetrics.InitializationTime:F2}ms");
                ChimeraLogger.Log($"  Registered Modules: {report.ServiceManagerMetrics.RegisteredModules}");
                ChimeraLogger.Log("");
            }

            if (report.CriticalFailures > 0)
            {
                ChimeraLogger.LogError("CRITICAL FAILURES:");
                foreach (var failure in report.ValidationResults.Where(r => r.IsCritical && !r.IsRegistered))
                {
                    ChimeraLogger.LogError($"  - {failure.ServiceName}: {failure.ErrorMessage}");
                }
                ChimeraLogger.Log("");
            }

            if (report.NullImplementations > 0)
            {
                ChimeraLogger.LogWarning("NULL IMPLEMENTATIONS (Expected in Development):");
                foreach (var nullService in report.ValidationResults.Where(r => r.IsNullImplementation))
                {
                    ChimeraLogger.LogWarning($"  - {nullService.ServiceName}");
                }
                ChimeraLogger.Log("");
            }

            if (report.DependencyIssues.Count > 0)
            {
                ChimeraLogger.LogWarning("DEPENDENCY ISSUES:");
                foreach (var issue in report.DependencyIssues)
                {
                    ChimeraLogger.LogWarning($"  - {issue}");
                }
                ChimeraLogger.Log("");
            }

            if (report.Errors.Count > 0)
            {
                ChimeraLogger.LogError("ERRORS:");
                foreach (var error in report.Errors)
                {
                    ChimeraLogger.LogError($"  - {error}");
                }
                ChimeraLogger.Log("");
            }

            ChimeraLogger.Log("=== END SERVICE REGISTRATION REPORT ===");
        }

        /// <summary>
        /// Save service registration report to file
        /// </summary>
        public void SaveServiceReportToFile(string filePath = null)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                var timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                filePath = System.IO.Path.Combine(Application.persistentDataPath, $"ServiceReport_{timestamp}.json");
            }

            var report = GenerateServiceReport();
            var json = JsonUtility.ToJson(report, true);
            System.IO.File.WriteAllText(filePath, json);
            
            ChimeraLogger.Log($"Service registration report saved to: {filePath}");
        }

        private double GetInitializationTime()
        {
            // This would need to be implemented to track actual initialization time
            // For now, return 0 as a placeholder
            return 0.0;
        }

        private int GetRegisteredModuleCount()
        {
            // This would need to be implemented to count registered modules
            // For now, return estimated count
            return 1; // ChimeraServiceModule
        }

        private void AnalyzeDependencies(ServiceRegistrationReport report, System.Collections.Generic.List<ServiceValidationResult> validationResults)
        {
            // Analyze critical service dependencies
            var criticalServices = validationResults.Where(r => r.IsCritical).ToList();
            var failedCritical = criticalServices.Where(r => !r.IsRegistered).ToList();

            if (failedCritical.Any())
            {
                report.DependencyIssues.Add($"Critical service failures may cause cascading failures in dependent systems");
            }

            // Check for services that depend on failed services
            if (failedCritical.Any(f => f.ServiceName == "IUpdateOrchestrator"))
            {
                report.DependencyIssues.Add("UpdateOrchestrator failure will prevent centralized update management");
            }

            if (failedCritical.Any(f => f.ServiceName == "IServiceContainer"))
            {
                report.DependencyIssues.Add("ServiceContainer failure will prevent dependency injection");
            }

            if (failedCritical.Any(f => f.ServiceName == "IEventManager"))
            {
                report.DependencyIssues.Add("EventManager failure will prevent inter-system communication");
            }
        }

        #endregion
    }

    /// <summary>
    /// Result of validating a service registration
    /// </summary>
    public class ServiceValidationResult
    {
        public string ServiceName { get; set; }
        public bool IsCritical { get; set; }
        public bool IsRegistered { get; set; }
        public bool IsNullImplementation { get; set; }
        public string ErrorMessage { get; set; }
        
        // Additional properties for comprehensive validation
        public bool IsValid { get; set; }
        public List<Type> InvalidServices { get; set; } = new List<Type>();
        public List<string> Errors { get; set; } = new List<string>();
        public List<Type> ValidServices { get; set; } = new List<Type>();
        public int TotalServices { get; set; }
        public int ValidServiceCount { get; set; }
        public int InvalidServiceCount { get; set; }
    }


}