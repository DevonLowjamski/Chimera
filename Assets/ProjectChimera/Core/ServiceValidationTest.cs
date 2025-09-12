using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using UnityEngine;
using System.Linq;
using ProjectChimera.Core.DependencyInjection;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Test component for validating dependency injection service registrations
    /// Attach to a GameObject to test service validation at runtime
    /// </summary>
    public class ServiceValidationTest : MonoBehaviour, ProjectChimera.Core.Updates.ITickable
    {
        [Header("Validation Settings")]
        [SerializeField] private bool _validateOnStart = true;
        [SerializeField] private bool _showDetailedReport = true;

        [Header("Test Actions")]
        [SerializeField] private bool _runValidationTest = false;

        private void Start()
        {
        // Register with UpdateOrchestrator
        ProjectChimera.Core.Updates.UpdateOrchestrator.Instance?.RegisterTickable(this);
            if (_validateOnStart)
            {
                RunValidationTest();
            }
        }

        /// <summary>
        /// ITickable Priority property
        /// </summary>
        public int Priority => ProjectChimera.Core.Updates.TickPriority.DebugSystems;

        /// <summary>
        /// ITickable Enabled property
        /// </summary>
        public bool Enabled => isActiveAndEnabled;

        public void Tick(float deltaTime)
        {
            if (_runValidationTest)
            {
                _runValidationTest = false;
                RunValidationTest();
            }
        }

        /// <summary>
        /// Run comprehensive service validation test
        /// </summary>
        [ContextMenu("Run Service Validation")]
        public void RunValidationTest()
        {
            ChimeraLogger.Log("=== SERVICE VALIDATION TEST ===");

            var bootstrapper = ServiceBootstrapper.Instance;
            if (bootstrapper == null)
            {
                ChimeraLogger.LogError("ServiceBootstrapper not found - DI system not initialized");
                return;
            }

            if (!bootstrapper.IsBootstrapped)
            {
                ChimeraLogger.LogWarning("ServiceBootstrapper not yet bootstrapped - attempting bootstrap");
                bootstrapper.BootstrapServices();
            }

            // Get validation status for all services
            var validationResults = bootstrapper.GetServiceValidationStatus();

            if (validationResults == null || validationResults.Count == 0)
            {
                ChimeraLogger.LogError("No validation results returned - DI system may not be functioning");
                return;
            }

            // Summary statistics
            var totalServices = validationResults.Count;
            var registeredServices = validationResults.Count(r => r.IsRegistered);
            var nullImplementations = validationResults.Count(r => r.IsNullImplementation);
            var criticalFailures = validationResults.Count(r => r.IsCritical && !r.IsRegistered);
            var warnings = validationResults.Count(r => !r.IsCritical && !r.IsRegistered);

            ChimeraLogger.Log($"📊 VALIDATION SUMMARY:");
            ChimeraLogger.Log($"   Total Services: {totalServices}");
            ChimeraLogger.Log($"   ✅ Registered: {registeredServices}");
            ChimeraLogger.Log($"   🔶 Null Implementations: {nullImplementations}");
            ChimeraLogger.Log($"   ❌ Critical Failures: {criticalFailures}");
            ChimeraLogger.Log($"   ⚠️ Warnings: {warnings}");

            if (_showDetailedReport)
            {
                ShowDetailedReport(validationResults);
            }

            // Test actual service resolution
            TestServiceResolution();

            ChimeraLogger.Log("=== VALIDATION TEST COMPLETE ===");
        }

        /// <summary>
        /// Show detailed validation report
        /// </summary>
        private void ShowDetailedReport(System.Collections.Generic.List<ServiceValidationResult> results)
        {
            ChimeraLogger.Log("📋 DETAILED VALIDATION REPORT:");

            foreach (var result in results)
            {
                var status = result.IsRegistered ? "✅" : "❌";
                var implementation = result.IsNullImplementation ? "(Null)" : "(Concrete)";
                var criticality = result.IsCritical ? "[CRITICAL]" : "[OPTIONAL]";

                ChimeraLogger.Log($"   {status} {result.ServiceName} {implementation} {criticality}");

                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    ChimeraLogger.LogError($"      Error: {result.ErrorMessage}");
                }
            }
        }

        /// <summary>
        /// Test actual service resolution to ensure DI is working
        /// </summary>
        private void TestServiceResolution()
        {
            ChimeraLogger.Log("🔧 TESTING SERVICE RESOLUTION:");

            try
            {
                // Test core services
                var eventManager = ServiceBootstrapper.GetGlobalService<IEventManager>();
                ChimeraLogger.Log($"   IEventManager: {(eventManager != null ? eventManager.GetType().Name : "NULL")}");

                var settingsManager = ServiceBootstrapper.GetGlobalService<ISettingsManager>();
                ChimeraLogger.Log($"   ISettingsManager: {(settingsManager != null ? settingsManager.GetType().Name : "NULL")}");

                // Test UI services
                var uiManager = ServiceBootstrapper.GetGlobalService<IUIManager>();
                ChimeraLogger.Log($"   IUIManager: {(uiManager != null ? uiManager.GetType().Name : "NULL")}");

                // Test construction services
                var gridSystem = ServiceBootstrapper.GetGlobalService<IGridSystem>();
                ChimeraLogger.Log($"   IGridSystem: {(gridSystem != null ? gridSystem.GetType().Name : "NULL")}");

                var assetService = ServiceBootstrapper.GetGlobalService<ISchematicAssetService>();
                ChimeraLogger.Log($"   ISchematicAssetService: {(assetService != null ? assetService.GetType().Name : "NULL")}");

                ChimeraLogger.Log("✅ Service resolution test completed");
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError($"❌ Service resolution test failed: {ex.Message}");
            }
        }

    // ITickable implementation properties are defined above

    public virtual void OnRegistered()
    {
        // Override in derived classes if needed
    }

    public virtual void OnUnregistered()
    {
        // Override in derived classes if needed
    }

    protected virtual void OnDestroy()
    {
        // Unregister from UpdateOrchestrator
        UpdateOrchestrator.Instance?.UnregisterTickable(this);
    }

    /// <summary>
    /// Service validation result data structure
    /// </summary>
    [System.Serializable]
    public class ServiceValidationResult
    {
        public string ServiceName;
        public bool IsRegistered;
        public bool IsNullImplementation;
        public bool IsCritical;
        public string ErrorMessage;
        public string ImplementationType;
        public System.DateTime ValidationTime;

        public ServiceValidationResult()
        {
            ValidationTime = System.DateTime.Now;
        }

        public ServiceValidationResult(string serviceName, bool isRegistered, bool isNullImplementation, bool isCritical, string errorMessage = "")
        {
            ServiceName = serviceName;
            IsRegistered = isRegistered;
            IsNullImplementation = isNullImplementation;
            IsCritical = isCritical;
            ErrorMessage = errorMessage;
            ValidationTime = System.DateTime.Now;
        }
    }

}
}

#if UNITY_EDITOR
namespace ProjectChimera.Core.Editor
{
    using UnityEditor;

    /// <summary>
    /// Custom inspector for ServiceValidationTest
    /// </summary>
    [CustomEditor(typeof(ServiceValidationTest))]
    public class ServiceValidationTestInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            var testComponent = (ServiceValidationTest)target;

            if (GUILayout.Button("Run Validation Test"))
            {
                testComponent.RunValidationTest();
            }
        }
    }
}
#endif
