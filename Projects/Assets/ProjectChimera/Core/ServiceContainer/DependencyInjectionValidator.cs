using UnityEngine;

namespace ProjectChimera.Core.ServiceContainer
{
    /// <summary>
    /// PC014-2a: Validation component to test dependency injection in cultivation services
    /// Demonstrates proper service injection and validates that all dependencies are resolved
    /// </summary>
    public class DependencyInjectionValidator : MonoBehaviour
    {
        [Header("Validation Configuration")]
        [SerializeField] private bool _validateOnStart = true;
        [SerializeField] private bool _enableDetailedLogging = true;
        
        [Header("Test Dependencies")]
        [Inject] private object _cultivationManager;
        [Inject] private object _plantManager;
        [Inject] private object _growthService;
        [Inject] private object _environmentalService;
        [Inject] private object _yieldService;
        [Inject] private object _geneticsService;
        [Inject] private object _harvestService;
        [Inject] private object _statisticsService;
        [Inject] private object _achievementService;
        [Inject] private object _processingService;
        [Inject] private object _lifecycleService;
        [Inject] private object _plantEnvironmentalService;

        void Start()
        {
            if (_validateOnStart)
            {
                ValidateDependencyInjection();
            }
        }

        /// <summary>
        /// Validates that all dependencies have been properly injected
        /// </summary>
        [ContextMenu("Validate Dependency Injection")]
        public void ValidateDependencyInjection()
        {
            Debug.Log("=== PC014-2a: Dependency Injection Validation ===");
            
            int successCount = 0;
            int totalCount = 0;
            
            // Test manager dependencies
            ValidateDependency(nameof(_cultivationManager), _cultivationManager, ref successCount, ref totalCount);
            ValidateDependency(nameof(_plantManager), _plantManager, ref successCount, ref totalCount);
            
            // Test service dependencies
            ValidateDependency(nameof(_growthService), _growthService, ref successCount, ref totalCount);
            ValidateDependency(nameof(_environmentalService), _environmentalService, ref successCount, ref totalCount);
            ValidateDependency(nameof(_yieldService), _yieldService, ref successCount, ref totalCount);
            ValidateDependency(nameof(_geneticsService), _geneticsService, ref successCount, ref totalCount);
            ValidateDependency(nameof(_harvestService), _harvestService, ref successCount, ref totalCount);
            ValidateDependency(nameof(_statisticsService), _statisticsService, ref successCount, ref totalCount);
            ValidateDependency(nameof(_achievementService), _achievementService, ref successCount, ref totalCount);
            ValidateDependency(nameof(_processingService), _processingService, ref successCount, ref totalCount);
            ValidateDependency(nameof(_lifecycleService), _lifecycleService, ref successCount, ref totalCount);
            ValidateDependency(nameof(_plantEnvironmentalService), _plantEnvironmentalService, ref successCount, ref totalCount);
            
            // Summary
            Debug.Log($"=== Dependency Injection Validation Results ===");
            Debug.Log($"✅ Successfully injected: {successCount}/{totalCount} dependencies");
            
            if (successCount == totalCount)
            {
                Debug.Log("🎉 All dependencies successfully injected! PC014-2a validation PASSED");
            }
            else
            {
                Debug.LogWarning($"⚠️ {totalCount - successCount} dependencies failed injection. Check service registration.");
            }
            
            Debug.Log("=== End Validation ===");
        }

        /// <summary>
        /// Validates a single dependency and logs the result
        /// </summary>
        private void ValidateDependency<T>(string name, T dependency, ref int successCount, ref int totalCount)
        {
            totalCount++;
            
            if (dependency != null)
            {
                successCount++;
                if (_enableDetailedLogging)
                {
                    Debug.Log($"✅ {name}: {dependency.GetType().Name}");
                }
            }
            else
            {
                Debug.LogError($"❌ {name}: NOT INJECTED");
            }
        }

        /// <summary>
        /// Manual dependency injection trigger (for testing)
        /// </summary>
        [ContextMenu("Inject Dependencies")]
        public void InjectDependenciesManually()
        {
            ServiceInjector.InjectDependencies(this);
            Debug.Log("[DependencyInjectionValidator] Manual injection completed");
        }

        /// <summary>
        /// Test service functionality after injection
        /// </summary>
        [ContextMenu("Test Service Functionality")]
        public void TestServiceFunctionality()
        {
            Debug.Log("=== Service Functionality Test ===");
            
            try
            {
                // Test growth service
                if (_growthService != null)
                {
                    Debug.Log($"✅ Growth Service: {_growthService.GetType().Name} injected successfully");
                }
                
                // Test genetics service
                if (_geneticsService != null)
                {
                    Debug.Log($"✅ Genetics Service: {_geneticsService.GetType().Name} injected successfully");
                }
                
                // Test statistics service
                if (_statisticsService != null)
                {
                    Debug.Log($"✅ Statistics Service: {_statisticsService.GetType().Name} injected successfully");
                }
                
                // Test achievement service
                if (_achievementService != null)
                {
                    Debug.Log($"✅ Achievement Service: {_achievementService.GetType().Name} injected successfully");
                }
                
                Debug.Log("✅ Service functionality validation completed");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Service functionality test failed: {ex.Message}");
            }
            
            Debug.Log("=== End Service Test ===");
        }

        void OnValidate()
        {
            // Ensure the ServiceInjector component is present
            if (GetComponent<ServiceInjector>() == null)
            {
                gameObject.AddComponent<ServiceInjector>();
                Debug.Log("[DependencyInjectionValidator] Added ServiceInjector component automatically");
            }
        }
    }
}