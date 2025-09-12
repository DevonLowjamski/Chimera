using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using ProjectChimera.Core;
using ProjectChimera.Core.DependencyInjection;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Testing
{
    /// <summary>
    /// Validates dependency injection functionality and lifecycle management.
    /// Tests error handling, recovery, and manager lifecycle operations.
    /// </summary>
    public class DependencyInjectionValidator : BaseValidator
    {
        [Header("DI Validation Settings")]
        [SerializeField] private bool _testLifecycleManagement = true;
        [SerializeField] private bool _testErrorHandling = true;
        [SerializeField] private bool _testHealthMonitoring = true;
        [SerializeField] private bool _testGameStateManagement = true;

        private DIGameManager _gameManager;
        private ServiceHealthMonitor _healthMonitor;
        private IChimeraServiceContainer _serviceContainer;

        public override void Initialize(TestCore testCore)
        {
            base.Initialize(testCore);
            
            _gameManager = DIGameManager.Instance ?? ServiceContainerFactory.Instance?.TryResolve<DIGameManager>();
            if (_gameManager != null)
            {
                _healthMonitor = _gameManager.HealthMonitor;
                _serviceContainer = _gameManager.GlobalServiceContainer;
            }
        }

        public override IEnumerator RunValidation()
        {
            LogValidation("Starting dependency injection validation...");
            
            yield return StartCoroutine(ValidateHealthMonitoringIntegration());
            yield return StartCoroutine(ValidateManagerLifecycleManagement());
            yield return StartCoroutine(ValidateGameStateManagement());
            yield return StartCoroutine(ValidateErrorHandlingAndRecovery());
            yield return StartCoroutine(ValidateNullHandling());
            yield return StartCoroutine(ValidateServiceLifecycle());
            
            LogValidation("Dependency injection validation completed");
        }

        private IEnumerator ValidateHealthMonitoringIntegration()
        {
            if (!_testHealthMonitoring || _gameManager == null)
            {
                yield break;
            }

            var result = new ValidationResult { ValidationName = "Health Monitoring Integration" };
            
            try
            {
                // Test health report generation
                var healthReport = _gameManager.GetServiceHealthReport();
                if (healthReport == null)
                {
                    result.AddError("Health report generation failed");
                }
                else
                {
                    // Validate health report structure
                    if (healthReport.ServiceStatuses == null)
                    {
                        result.AddError("Health report ServiceStatuses is null");
                    }
                    
                    if (healthReport.CriticalErrors == null)
                    {
                        result.AddError("Health report CriticalErrors is null");
                    }
                    
                    LogValidation($"Health report: {healthReport.ServiceStatuses?.Count ?? 0} services monitored");
                    LogValidation($"Health status: {(healthReport.IsHealthy ? "Healthy" : "Issues detected")}");
                    
                    if (!healthReport.IsHealthy)
                    {
                        LogValidation($"Critical errors: {healthReport.CriticalErrors?.Count ?? 0}");
                        LogValidation($"Warnings: {healthReport.Warnings?.Count ?? 0}");
                    }
                }

                // Test direct health monitor access
                if (_healthMonitor != null)
                {
                    var isMonitoring = _healthMonitor.IsMonitoring;
                    var trackedCount = _healthMonitor.TrackedServicesCount;
                    
                    LogValidation($"Health monitor: {(isMonitoring ? "Running" : "Stopped")}, tracking {trackedCount} services");
                    
                    // Test health monitor initialization
                    if (trackedCount == 0)
                    {
                        result.AddWarning("Health monitor not tracking any services");
                    }
                }
                else
                {
                    result.AddError("Health monitor not accessible");
                }
                
                result.MarkSuccess();
            }
            catch (Exception ex)
            {
                result.AddError($"Exception during health monitoring validation: {ex.Message}");
            }
            
            _results.Add(result);
            yield return new WaitForSeconds(0.1f);
        }

        private IEnumerator ValidateManagerLifecycleManagement()
        {
            if (!_testLifecycleManagement || _gameManager == null)
            {
                yield break;
            }

            var result = new ValidationResult { ValidationName = "Manager Lifecycle Management" };
            
            bool pauseTestSuccessful = false;
            bool resumeTestSuccessful = false;
            
            try
            {
                // Test game state management
                var initialState = _gameManager.CurrentGameState;
                LogValidation($"Current game state: {initialState}");
                
                // Test pause/resume functionality
                bool wasPaused = _gameManager.IsGamePaused;
                
                _gameManager.PauseGame();
                pauseTestSuccessful = true;
                
                yield return new WaitForSeconds(0.1f);
                
                if (!_gameManager.IsGamePaused)
                {
                    result.AddError("Game pause functionality failed");
                }

                _gameManager.ResumeGame();
                resumeTestSuccessful = true;
                
                yield return new WaitForSeconds(0.1f);
                
                if (_gameManager.IsGamePaused)
                {
                    result.AddError("Game resume functionality failed");
                }

                // Test singleton lifecycle
                var instance = DIGameManager.Instance;
                if (instance != _gameManager)
                {
                    result.AddError("Singleton instance not properly maintained");
                }

                // Test game time tracking
                var startTime = _gameManager.GameStartTime;
                var totalTime = _gameManager.TotalGameTime;
                
                if (startTime == default(DateTime))
                {
                    result.AddWarning("Game start time not initialized");
                }
                
                if (totalTime.TotalSeconds < 0)
                {
                    result.AddError("Total game time calculation failed");
                }
                
                result.MarkSuccess();
            }
            catch (Exception ex)
            {
                result.AddError($"Exception during lifecycle validation: {ex.Message}");
            }
            
            _results.Add(result);
            yield return new WaitForSeconds(0.1f);
        }

        private IEnumerator ValidateGameStateManagement()
        {
            if (!_testGameStateManagement || _gameManager == null)
            {
                yield break;
            }

            var result = new ValidationResult { ValidationName = "Game State Management" };
            
            try
            {
                // Test game state transitions
                var currentState = _gameManager.CurrentGameState;
                LogValidation($"Testing game state: {currentState}");

                // Test pause state
                bool originalPauseState = _gameManager.IsGamePaused;
                
                _gameManager.PauseGame();
                yield return new WaitForSeconds(0.05f);
                
                if (!_gameManager.IsGamePaused)
                {
                    result.AddError("Pause state transition failed");
                }

                _gameManager.ResumeGame();
                yield return new WaitForSeconds(0.05f);
                
                if (_gameManager.IsGamePaused)
                {
                    result.AddError("Resume state transition failed");
                }

                // Test time tracking consistency
                var gameTime1 = _gameManager.TotalGameTime;
                yield return new WaitForSeconds(0.1f);
                var gameTime2 = _gameManager.TotalGameTime;
                
                if (gameTime2 <= gameTime1)
                {
                    result.AddWarning("Game time not progressing correctly");
                }

                // Test start time consistency
                var startTime = _gameManager.GameStartTime;
                if (startTime > DateTime.Now)
                {
                    result.AddError("Game start time is in the future");
                }
                
                result.MarkSuccess();
            }
            catch (Exception ex)
            {
                result.AddError($"Exception during game state validation: {ex.Message}");
            }
            
            _results.Add(result);
            yield return new WaitForSeconds(0.1f);
        }

        private IEnumerator ValidateErrorHandlingAndRecovery()
        {
            if (!_testErrorHandling || _gameManager == null)
            {
                yield break;
            }

            var result = new ValidationResult { ValidationName = "Error Handling and Recovery" };
            
            try
            {
                // Test null manager registration handling
                try
                {
                    _gameManager.RegisterManager<ChimeraManager>(null);
                    // Should not throw exception - graceful handling expected
                }
                catch (Exception ex)
                {
                    result.AddWarning($"Null manager registration threw exception: {ex.Message}");
                }

                // Test invalid manager retrieval
                var nonExistentManager = _gameManager.GetManager<NonExistentTestManager>();
                if (nonExistentManager != null)
                {
                    result.AddError("GetManager returned non-null for non-existent manager type");
                }

                // Test service container error handling
                if (_serviceContainer != null)
                {
                    try
                    {
                        var nonExistentService = _serviceContainer.TryResolve<NonExistentTestService>();
                        if (nonExistentService != null)
                        {
                            result.AddError("Service container returned non-null for non-existent service");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.AddWarning($"Service container error handling not graceful: {ex.Message}");
                    }
                }

                // Test duplicate registration handling
                var testManager1 = _testCore.CreateTestManager("ErrorTestManager1");
                var testManager2 = _testCore.CreateTestManager("ErrorTestManager2");
                
                try
                {
                    _gameManager.RegisterManager(testManager1);
                    _gameManager.RegisterManager(testManager2); // Second registration of same type
                    
                    // Should handle gracefully without throwing
                    LogValidation("Duplicate registration handled gracefully");
                }
                catch (Exception ex)
                {
                    result.AddWarning($"Duplicate registration threw exception: {ex.Message}");
                }
                finally
                {
                    if (testManager1 != null) DestroyImmediate(testManager1.gameObject);
                    if (testManager2 != null) DestroyImmediate(testManager2.gameObject);
                }
                
                result.MarkSuccess();
            }
            catch (Exception ex)
            {
                result.AddError($"Exception during error handling validation: {ex.Message}");
            }
            
            _results.Add(result);
            yield return new WaitForSeconds(0.1f);
        }

        private IEnumerator ValidateNullHandling()
        {
            var result = new ValidationResult { ValidationName = "Null Handling" };
            
            try
            {
                // Test null parameter handling
                if (_gameManager != null)
                {
                    // Test null manager registration
                    _gameManager.RegisterManager<TestManager>(null);
                    
                    // Test null service resolution
                    if (_serviceContainer != null)
                    {
                        var nullResolution = _serviceContainer.TryResolve<TestManager>();
                        // Should return null gracefully, not throw
                    }
                }

                // Test null health monitor handling
                if (_healthMonitor == null)
                {
                    result.AddWarning("Health monitor is null - testing null handling");
                }

                result.MarkSuccess();
            }
            catch (Exception ex)
            {
                result.AddError($"Exception during null handling validation: {ex.Message}");
            }
            
            _results.Add(result);
            yield return new WaitForSeconds(0.1f);
        }

        private IEnumerator ValidateServiceLifecycle()
        {
            var result = new ValidationResult { ValidationName = "Service Lifecycle" };
            
            try
            {
                // Test service initialization state
                if (_gameManager != null)
                {
                    var isInitialized = _gameManager.IsInitialized;
                    LogValidation($"DIGameManager initialization state: {isInitialized}");
                    
                    if (!isInitialized)
                    {
                        result.AddWarning("DIGameManager reports as not initialized");
                    }
                }

                // Test service container state
                if (_serviceContainer != null)
                {
                    try
                    {
                        var verification = _serviceContainer.Verify();
                        if (!verification.IsValid)
                        {
                            result.AddWarning($"Service container state invalid: {string.Join(", ", verification.Errors)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.AddWarning($"Service container verification failed: {ex.Message}");
                    }
                }

                // Test health monitoring lifecycle
                if (_healthMonitor != null)
                {
                    var isMonitoring = _healthMonitor.IsMonitoring;
                    LogValidation($"Health monitor lifecycle state: {isMonitoring}");
                    
                    if (!isMonitoring)
                    {
                        result.AddWarning("Health monitor not in monitoring state");
                    }
                }
                
                result.MarkSuccess();
            }
            catch (Exception ex)
            {
                result.AddError($"Exception during service lifecycle validation: {ex.Message}");
            }
            
            _results.Add(result);
            yield return new WaitForSeconds(0.1f);
        }

        /// <summary>
        /// Get current health status
        /// </summary>
        public bool IsSystemHealthy()
        {
            try
            {
                var healthReport = _gameManager?.GetServiceHealthReport();
                return healthReport?.IsHealthy ?? false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get current game state
        /// </summary>
        public GameState GetCurrentGameState()
        {
            return _gameManager?.CurrentGameState ?? GameState.Unknown;
        }

        /// <summary>
        /// Check if game is paused
        /// </summary>
        public bool IsGamePaused()
        {
            return _gameManager?.IsGamePaused ?? false;
        }

        /// <summary>
        /// Non-existent manager for error testing
        /// </summary>
        private class NonExistentTestManager : ChimeraManager
        {
            public override string ManagerName => "NonExistentTest";
            public override ManagerPriority Priority => ManagerPriority.Low;
            
            protected override void OnManagerInitialize() { }
            protected override void OnManagerShutdown() { }
        }

        /// <summary>
        /// Non-existent service for error testing
        /// </summary>
        private interface NonExistentTestService { }
    }
}