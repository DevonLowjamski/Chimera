using UnityEngine;
using System.Collections;
using System;
using ProjectChimera.Core;
using ProjectChimera.Core.DependencyInjection;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Testing
{
    /// <summary>
    /// Validates service container integration and dependency injection functionality.
    /// Tests container verification, service registration, and singleton pattern enforcement.
    /// </summary>
    public class ServiceContainerValidator : BaseValidator
    {
        [Header("Container Validation Settings")]
        [SerializeField] private bool _testServiceRegistration = true;
        [SerializeField] private bool _testSingletonPattern = true;
        [SerializeField] private bool _testFactoryRegistration = true;
        [SerializeField] private bool _testContainerVerification = true;

        private DIGameManager _gameManager;
        private IChimeraServiceContainer _serviceContainer;

        public override void Initialize(TestCore testCore)
        {
            base.Initialize(testCore);
            
            _gameManager = DIGameManager.Instance ?? ServiceContainerFactory.Instance?.TryResolve<DIGameManager>();
            if (_gameManager != null)
            {
                _serviceContainer = _gameManager.GlobalServiceContainer;
            }
        }

        public override IEnumerator RunValidation()
        {
            LogValidation("Starting service container validation...");
            
            yield return StartCoroutine(ValidateServiceContainerAccess());
            yield return StartCoroutine(ValidateContainerVerification());
            yield return StartCoroutine(ValidateCoreServiceRegistrations());
            yield return StartCoroutine(ValidateSingletonPatternEnforcement());
            yield return StartCoroutine(ValidateFactoryRegistration());
            yield return StartCoroutine(ValidateServiceResolution());
            
            LogValidation("Service container validation completed");
        }

        private IEnumerator ValidateServiceContainerAccess()
        {
            var result = new ValidationResult { ValidationName = "Service Container Access" };
            
            try
            {
                if (_gameManager == null)
                {
                    result.AddError("DIGameManager not available for testing");
                }
                else if (_serviceContainer == null)
                {
                    result.AddError("Global service container not accessible");
                }
                else
                {
                    LogValidation("Service container access confirmed");
                }
                
                result.MarkSuccess();
            }
            catch (Exception ex)
            {
                result.AddError($"Exception during container access validation: {ex.Message}");
            }
            
            _results.Add(result);
            yield return new WaitForSeconds(0.1f);
        }

        private IEnumerator ValidateContainerVerification()
        {
            if (!_testContainerVerification || _serviceContainer == null)
            {
                yield break;
            }

            var result = new ValidationResult { ValidationName = "Container Verification" };
            
            try
            {
                var verification = _serviceContainer.Verify();
                if (!verification.IsValid)
                {
                    result.AddWarning($"Container verification failed: {string.Join(", ", verification.Errors)}");
                }
                else
                {
                    LogValidation($"Container verified: {verification.VerifiedServices} services");
                }
                
                result.MarkSuccess();
            }
            catch (Exception ex)
            {
                result.AddWarning($"Container verification threw exception: {ex.Message}");
            }
            
            _results.Add(result);
            yield return new WaitForSeconds(0.1f);
        }

        private IEnumerator ValidateCoreServiceRegistrations()
        {
            if (!_testServiceRegistration || _serviceContainer == null)
            {
                yield break;
            }

            var result = new ValidationResult { ValidationName = "Core Service Registrations" };
            
            try
            {
                // Test IGameManager registration
                var gameManagerFromContainer = _serviceContainer.TryResolve<IGameManager>();
                if (gameManagerFromContainer == null)
                {
                    result.AddError("IGameManager not registered in service container");
                }
                else if (gameManagerFromContainer != _gameManager)
                {
                    result.AddError("Wrong IGameManager instance returned from container");
                }

                // Test DIGameManager registration
                var diGameManagerFromContainer = _serviceContainer.TryResolve<DIGameManager>();
                if (diGameManagerFromContainer == null)
                {
                    result.AddError("DIGameManager not registered in service container");
                }
                else if (diGameManagerFromContainer != _gameManager)
                {
                    result.AddError("Wrong DIGameManager instance returned from container");
                }

                // Test service container self-registration
                var containerFromContainer = _serviceContainer.TryResolve<IChimeraServiceContainer>();
                if (containerFromContainer == null)
                {
                    result.AddWarning("Service container not self-registered");
                }
                else if (containerFromContainer != _serviceContainer)
                {
                    result.AddWarning("Self-registered container returns different instance");
                }
                
                result.MarkSuccess();
            }
            catch (Exception ex)
            {
                result.AddError($"Exception during service registration validation: {ex.Message}");
            }
            
            _results.Add(result);
            yield return new WaitForSeconds(0.1f);
        }

        private IEnumerator ValidateSingletonPatternEnforcement()
        {
            if (!_testSingletonPattern || _serviceContainer == null)
            {
                yield break;
            }

            var result = new ValidationResult { ValidationName = "Singleton Pattern Enforcement" };
            
            try
            {
                // Test IGameManager singleton
                var gameManager1 = _serviceContainer.TryResolve<IGameManager>();
                var gameManager2 = _serviceContainer.TryResolve<IGameManager>();
                
                if (gameManager1 != gameManager2)
                {
                    result.AddError("IGameManager singleton pattern not enforced");
                }

                // Test DIGameManager singleton
                var diGameManager1 = _serviceContainer.TryResolve<DIGameManager>();
                var diGameManager2 = _serviceContainer.TryResolve<DIGameManager>();
                
                if (diGameManager1 != diGameManager2)
                {
                    result.AddError("DIGameManager singleton pattern not enforced");
                }

                // Test container singleton
                var container1 = _serviceContainer.TryResolve<IChimeraServiceContainer>();
                var container2 = _serviceContainer.TryResolve<IChimeraServiceContainer>();
                
                if (container1 != null && container2 != null && container1 != container2)
                {
                    result.AddError("Service container singleton pattern not enforced");
                }
                
                result.MarkSuccess();
            }
            catch (Exception ex)
            {
                result.AddError($"Exception during singleton validation: {ex.Message}");
            }
            
            _results.Add(result);
            yield return new WaitForSeconds(0.1f);
        }

        private IEnumerator ValidateFactoryRegistration()
        {
            if (!_testFactoryRegistration || _serviceContainer == null)
            {
                yield break;
            }

            var result = new ValidationResult { ValidationName = "Factory Registration" };
            
            try
            {
                var containerFromFactory = _serviceContainer.TryResolve<IChimeraServiceContainer>();
                if (containerFromFactory == null)
                {
                    result.AddWarning("Service container factory registration not working");
                }
                else if (containerFromFactory != _serviceContainer)
                {
                    result.AddWarning("Factory returned different container instance");
                }
                else
                {
                    LogValidation("Factory registration working correctly");
                }
                
                result.MarkSuccess();
            }
            catch (Exception ex)
            {
                result.AddWarning($"Factory resolution failed: {ex.Message}");
            }
            
            _results.Add(result);
            yield return new WaitForSeconds(0.1f);
        }

        private IEnumerator ValidateServiceResolution()
        {
            if (_serviceContainer == null)
            {
                yield break;
            }

            var result = new ValidationResult { ValidationName = "Service Resolution" };
            
            try
            {
                // Test null service resolution
                var nullService = _serviceContainer.TryResolve<NonExistentTestService>();
                if (nullService != null)
                {
                    result.AddError("Container returned non-null for non-existent service");
                }

                // Test that registered services can be resolved
                var resolvedDIGameManager = _serviceContainer.TryResolve<DIGameManager>();
                if (resolvedDIGameManager == null)
                {
                    result.AddError("DIGameManager cannot be resolved from container");
                }
                else if (resolvedDIGameManager != _gameManager)
                {
                    result.AddError("Wrong DIGameManager instance resolved");
                }

                // Test interface resolution
                var resolvedGameManager = _serviceContainer.TryResolve<IGameManager>();
                if (resolvedGameManager == null)
                {
                    result.AddError("IGameManager cannot be resolved from container");
                }
                else if (resolvedGameManager != _gameManager)
                {
                    result.AddError("Wrong IGameManager instance resolved");
                }
                
                result.MarkSuccess();
            }
            catch (Exception ex)
            {
                result.AddError($"Exception during service resolution validation: {ex.Message}");
            }
            
            _results.Add(result);
            yield return new WaitForSeconds(0.1f);
        }

        /// <summary>
        /// Get container verification status
        /// </summary>
        public bool IsContainerValid()
        {
            if (_serviceContainer == null) return false;
            
            try
            {
                var verification = _serviceContainer.Verify();
                return verification.IsValid;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get number of registered services
        /// </summary>
        public int GetRegisteredServiceCount()
        {
            if (_serviceContainer == null) return 0;
            
            try
            {
                var verification = _serviceContainer.Verify();
                return verification.VerifiedServices;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Test interface for non-existent service validation
        /// </summary>
        private interface NonExistentTestService { }
    }
}