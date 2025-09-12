using UnityEngine;
using System.Collections;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Testing
{
    /// <summary>
    /// REFACTORED: DIGameManager validation test has been decomposed into focused validation components.
    /// This file now serves as a reference implementation using the new component structure.
    /// 
    /// New Component Structure:
    /// - TestCore.cs: Core testing infrastructure and component coordination
    /// - ServiceContainerValidator.cs: Service container integration validation
    /// - ManagerRegistryValidator.cs: Manager registration system validation
    /// - DependencyInjectionValidator.cs: Dependency injection functionality validation
    /// - PerformanceValidator.cs: Performance characteristics validation
    /// </summary>
    
    // The DIGameManagerValidationTest functionality has been moved to focused component files.
    // This file is kept for reference and to prevent breaking changes during migration.
    // 
    // To use the new component structure, inherit from TestCore:
    // 
    // public class DIGameManagerValidationTest : TestCore
    // {
    //     // Your custom validation implementation
    // }
    // 
    // The following validation areas are now available in their focused components:
    // 
    // From ServiceContainerValidator.cs:
    // - Service container access and verification
    // - Core service registrations validation
    // - Singleton pattern enforcement testing
    // - Factory registration validation
    // 
    // From ManagerRegistryValidator.cs:
    // - Manager registration system testing
    // - Auto-discovery mechanisms validation
    // - Interface-based discovery testing
    // - Service container integration verification
    // 
    // From DependencyInjectionValidator.cs:
    // - Health monitoring integration testing
    // - Manager lifecycle management validation
    // - Game state management testing
    // - Error handling and recovery validation
    // 
    // From PerformanceValidator.cs:
    // - Registration performance characteristics
    // - Retrieval speed optimization testing
    // - Memory usage pattern analysis
    // - Concurrent access validation
    // - Scalability characteristics testing

    /// <summary>
    /// Concrete implementation of DIGameManagerValidationTest using the new component structure.
    /// Inherits all functionality from TestCore and specialized validation components.
    /// </summary>
    public class DIGameManagerValidationTest : TestCore
    {
        [Header("DIGameManager Validation Settings")]
        [SerializeField] private bool _enableServiceContainerValidation = true;
        [SerializeField] private bool _enableManagerRegistryValidation = true;
        [SerializeField] private bool _enableDependencyInjectionValidation = true;
        [SerializeField] private bool _enablePerformanceValidation = true;

        // Specialized validation components
        private ServiceContainerValidator _serviceContainerValidator;
        private ManagerRegistryValidator _managerRegistryValidator;
        private DependencyInjectionValidator _dependencyInjectionValidator;
        private PerformanceValidator _performanceValidator;

        // Legacy support properties
        public bool EnableServiceContainerValidation => _enableServiceContainerValidation;
        public bool EnableManagerRegistryValidation => _enableManagerRegistryValidation;
        public bool EnableDependencyInjectionValidation => _enableDependencyInjectionValidation;
        public bool EnablePerformanceValidation => _enablePerformanceValidation;

        protected override void InitializeTestComponents()
        {
            base.InitializeTestComponents();
            
            // Initialize specialized validation components
            InitializeDIValidationComponents();
        }

        protected override IEnumerator RunComponentValidations()
        {
            // Run base validations first
            yield return StartCoroutine(base.RunComponentValidations());
            
            // Run specialized DI validations
            yield return StartCoroutine(RunDISpecificValidations());
        }

        private void InitializeDIValidationComponents()
        {
            // Initialize service container validator
            if (_enableServiceContainerValidation)
            {
                _serviceContainerValidator = GetComponent<ServiceContainerValidator>();
                if (_serviceContainerValidator == null)
                {
                    _serviceContainerValidator = gameObject.AddComponent<ServiceContainerValidator>();
                }
                _serviceContainerValidator.Initialize(this);
            }

            // Initialize manager registry validator
            if (_enableManagerRegistryValidation)
            {
                _managerRegistryValidator = GetComponent<ManagerRegistryValidator>();
                if (_managerRegistryValidator == null)
                {
                    _managerRegistryValidator = gameObject.AddComponent<ManagerRegistryValidator>();
                }
                _managerRegistryValidator.Initialize(this);
            }

            // Initialize dependency injection validator
            if (_enableDependencyInjectionValidation)
            {
                _dependencyInjectionValidator = GetComponent<DependencyInjectionValidator>();
                if (_dependencyInjectionValidator == null)
                {
                    _dependencyInjectionValidator = gameObject.AddComponent<DependencyInjectionValidator>();
                }
                _dependencyInjectionValidator.Initialize(this);
            }

            // Initialize performance validator
            if (_enablePerformanceValidation)
            {
                _performanceValidator = GetComponent<PerformanceValidator>();
                if (_performanceValidator == null)
                {
                    _performanceValidator = gameObject.AddComponent<PerformanceValidator>();
                }
                _performanceValidator.Initialize(this);
            }
        }

        private IEnumerator RunDISpecificValidations()
        {
            LogTest("Running DIGameManager-specific validations...");

            // Service container validation
            if (_serviceContainerValidator != null)
            {
                yield return StartCoroutine(_serviceContainerValidator.RunValidation());
                _validationResults.AddRange(_serviceContainerValidator.GetResults());
            }

            // Manager registry validation
            if (_managerRegistryValidator != null)
            {
                yield return StartCoroutine(_managerRegistryValidator.RunValidation());
                _validationResults.AddRange(_managerRegistryValidator.GetResults());
            }

            // Dependency injection validation
            if (_dependencyInjectionValidator != null)
            {
                yield return StartCoroutine(_dependencyInjectionValidator.RunValidation());
                _validationResults.AddRange(_dependencyInjectionValidator.GetResults());
            }

            // Performance validation (if enabled)
            if (_performanceValidator != null && ValidatePerformance)
            {
                yield return StartCoroutine(_performanceValidator.RunValidation(PerformanceTestIterations));
                _validationResults.AddRange(_performanceValidator.GetResults());
            }
        }

        /// <summary>
        /// Legacy support method for backward compatibility
        /// </summary>
        public bool IsServiceContainerValid()
        {
            return _serviceContainerValidator?.IsContainerValid() ?? false;
        }

        /// <summary>
        /// Legacy support method for backward compatibility
        /// </summary>
        public int GetRegisteredManagerCount()
        {
            return _managerRegistryValidator?.GetRegisteredManagerCount() ?? 0;
        }

        /// <summary>
        /// Legacy support method for backward compatibility
        /// </summary>
        public bool IsSystemHealthy()
        {
            return _dependencyInjectionValidator?.IsSystemHealthy() ?? false;
        }

        /// <summary>
        /// Legacy support method for backward compatibility
        /// </summary>
        public PerformanceValidator.PerformanceSummary GetPerformanceSummary()
        {
            return _performanceValidator?.GetPerformanceSummary() ?? new PerformanceValidator.PerformanceSummary();
        }

        /// <summary>
        /// Get service container validator
        /// </summary>
        public ServiceContainerValidator ServiceContainerValidator => _serviceContainerValidator;

        /// <summary>
        /// Get manager registry validator
        /// </summary>
        public ManagerRegistryValidator ManagerRegistryValidator => _managerRegistryValidator;

        /// <summary>
        /// Get dependency injection validator
        /// </summary>
        public DependencyInjectionValidator DependencyInjectionValidator => _dependencyInjectionValidator;

        /// <summary>
        /// Get performance validator
        /// </summary>
        public PerformanceValidator PerformanceValidator => _performanceValidator;

        /// <summary>
        /// Run comprehensive DIGameManager validation
        /// </summary>
        [ContextMenu("Run DIGameManager Validation")]
        public void RunDIGameManagerValidation()
        {
            if (Application.isPlaying)
            {
                StartCoroutine(RunCompleteValidation());
            }
            else
            {
                ChimeraLogger.Log("[DIGameManagerValidationTest] Validation only works during play mode");
            }
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Editor-only method for testing validation system
        /// </summary>
        [ContextMenu("Test Validation Components")]
        private void TestValidationComponents()
        {
            if (Application.isPlaying)
            {
                LogTest("Testing DIGameManager validation components...");
                LogTest($"Service container validation: {(_enableServiceContainerValidation ? "Enabled" : "Disabled")}");
                LogTest($"Manager registry validation: {(_enableManagerRegistryValidation ? "Enabled" : "Disabled")}");
                LogTest($"Dependency injection validation: {(_enableDependencyInjectionValidation ? "Enabled" : "Disabled")}");
                LogTest($"Performance validation: {(_enablePerformanceValidation ? "Enabled" : "Disabled")}");
                LogTest($"Performance test iterations: {PerformanceTestIterations}");
                
                if (_serviceContainerValidator != null)
                {
                    LogTest($"Service container registered services: {_serviceContainerValidator.GetRegisteredServiceCount()}");
                }
                
                if (_managerRegistryValidator != null)
                {
                    LogTest($"Registered manager count: {_managerRegistryValidator.GetRegisteredManagerCount()}");
                }
            }
            else
            {
                ChimeraLogger.Log("[DIGameManagerValidationTest] Test only works during play mode");
            }
        }
        #endif
    }
}