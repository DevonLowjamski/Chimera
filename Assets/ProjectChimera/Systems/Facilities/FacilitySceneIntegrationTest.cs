using ProjectChimera.Core.Logging;
using UnityEngine;
using ProjectChimera.Core;
// Migrated to unified ServiceContainer architecture
using ProjectChimera.Systems.Scene;
using System.Collections;

namespace ProjectChimera.Systems.Facilities
{
    /// <summary>
    /// Integration test for FacilityManager and SceneLoader coordination
    /// Demonstrates facility switching and upgrade workflows
    /// </summary>
    public class FacilitySceneIntegrationTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool _runTestOnStart = false;
        [SerializeField] private float _testDelayBetweenTransitions = 3f;

        [Header("Test Scenes")]
        [SerializeField] private string[] _testFacilityScenes = {
            SceneConstants.WAREHOUSE_SMALL_BAY,
            SceneConstants.WAREHOUSE_MEDIUM_BAY,
            SceneConstants.WAREHOUSE_SMALL_STANDALONE
        };

        private FacilityManager _facilityManager;
        private ISceneLoader _sceneLoader;

        private void Start()
        {
            if (_runTestOnStart)
            {
                StartCoroutine(RunIntegrationTest());
            }
        }

        /// <summary>
        /// Run comprehensive integration test
        /// </summary>
        public IEnumerator RunIntegrationTest()
        {
            ChimeraLogger.LogTesting("Starting facility scene integration test...");

            // Wait for services to initialize
            yield return new WaitForSeconds(1f);

            // Get services
            _facilityManager = ServiceContainerFactory.Instance?.TryResolve<FacilityManager>();
            _sceneLoader = ServiceContainerFactory.Instance?.TryResolve<ISceneLoader>();

            if (_facilityManager == null)
            {
                ChimeraLogger.LogError("FacilityManager not available");
                yield break;
            }

            if (_sceneLoader == null)
            {
                ChimeraLogger.LogError("SceneLoader not available");
                yield break;
            }

            ChimeraLogger.LogTesting("Services ready - starting test sequence");

            // Test 1: Verify initial state
            yield return StartCoroutine(TestInitialState());

            // Test 2: Test facility scene switching
            yield return StartCoroutine(TestFacilitySceneSwitching());

            // Test 3: Test scene validation
            yield return StartCoroutine(TestSceneValidation());

            ChimeraLogger.LogTesting("Integration test completed!");
        }

        /// <summary>
        /// Test initial facility manager state
        /// </summary>
        private IEnumerator TestInitialState()
        {
            ChimeraLogger.LogTesting("=== Test 1: Initial State ===");

            if (!_facilityManager.IsInitialized)
            {
                ChimeraLogger.LogError("FacilityManager not initialized!");
                yield break;
            }

            var stats = _facilityManager.GetProgressionStatisticsTyped();
            ChimeraLogger.LogTesting($"Current facility tier: {stats.CurrentTier}");
            ChimeraLogger.LogTesting($"Owned facilities: {stats.OwnedFacilities}");
            ChimeraLogger.Log($"[FacilitySceneIntegrationTest] Can upgrade: {stats.CanUpgrade}");

            var availableScenes = _facilityManager.GetAvailableFacilityScenes();
            ChimeraLogger.LogTesting($"Available facility scenes: {string.Join(", ", availableScenes)}");

            yield return new WaitForSeconds(1f);
        }

        /// <summary>
        /// Test facility scene switching
        /// </summary>
        private IEnumerator TestFacilitySceneSwitching()
        {
            ChimeraLogger.LogTesting("=== Test 2: Facility Scene Switching ===");

            foreach (string sceneName in _testFacilityScenes)
            {
                ChimeraLogger.LogTesting($"Testing transition to: {sceneName}");

                // Validate scene before attempting load
                if (!SceneConstants.IsValidScene(sceneName))
                {
                    ChimeraLogger.LogWarning($"Scene {sceneName} not valid - skipping");
                    continue;
                }

                // Test scene loading capability
                bool canLoad = BuildSettingsValidator.CanLoadScene(sceneName);
                ChimeraLogger.LogTesting($"Can load {sceneName}: {canLoad}");

                if (canLoad)
                {
                    // Get facility info for this scene
                    var facilityInfo = _facilityManager.GetFacilityInfoForScene(sceneName);
                    if (facilityInfo != null)
                    {
                        ChimeraLogger.Log($"[FacilitySceneIntegrationTest] Facility info: {facilityInfo.TierName} ({facilityInfo.BuildIndex})");
                    }

                    // Note: Not actually loading scenes in test to avoid disrupting Unity Editor
                    // In actual gameplay, this would call:
                    // _facilityManager.LoadFacilitySceneByName(sceneName);

                    ChimeraLogger.Log($"[FacilitySceneIntegrationTest] Scene {sceneName} ready for loading");
                }

                yield return new WaitForSeconds(_testDelayBetweenTransitions);
            }
        }

        /// <summary>
        /// Test scene validation integration
        /// </summary>
        private IEnumerator TestSceneValidation()
        {
            ChimeraLogger.Log("[FacilitySceneIntegrationTest] === Test 3: Scene Validation ===");

            // Test BuildSettingsValidator integration
            bool buildSettingsValid = BuildSettingsValidator.ValidateRuntimeBuildSettings();
            ChimeraLogger.Log($"[FacilitySceneIntegrationTest] Build Settings valid: {buildSettingsValid}");

            // Test scene constants integration
            foreach (string sceneName in _testFacilityScenes)
            {
                int buildIndex = SceneConstants.GetBuildIndex(sceneName);
                string resolvedName = SceneConstants.GetSceneName(buildIndex);

                bool resolutionValid = (resolvedName == sceneName);
                ChimeraLogger.Log($"[FacilitySceneIntegrationTest] Scene resolution {sceneName}: {buildIndex} -> {resolvedName} ({(resolutionValid ? "✓" : "✗")})");

                bool isWarehouse = SceneConstants.IsWarehouseScene(sceneName);
                ChimeraLogger.Log($"[FacilitySceneIntegrationTest] {sceneName} is warehouse scene: {isWarehouse}");
            }

            yield return new WaitForSeconds(1f);

            ChimeraLogger.Log("[FacilitySceneIntegrationTest] Scene validation test completed");
        }

        /// <summary>
        /// Manual test trigger - call from Unity Inspector or other scripts
        /// </summary>
        [ContextMenu("Run Integration Test")]
        public void RunIntegrationTestManual()
        {
            if (Application.isPlaying)
            {
                StartCoroutine(RunIntegrationTest());
            }
            else
            {
                ChimeraLogger.LogWarning("[FacilitySceneIntegrationTest] Test can only be run in Play mode");
            }
        }

        /// <summary>
        /// Get facility manager status for debugging
        /// </summary>
        [ContextMenu("Print Facility Status")]
        public void PrintFacilityStatus()
        {
            if (_facilityManager == null)
            {
                _facilityManager = ServiceContainerFactory.Instance?.TryResolve<FacilityManager>();
            }

            if (_facilityManager != null && _facilityManager.IsInitialized)
            {
                var stats = _facilityManager.GetProgressionStatisticsTyped();
                ChimeraLogger.Log($"[FacilitySceneIntegrationTest] === Facility Status ===\n{stats}");

                var availableScenes = _facilityManager.GetAvailableFacilityScenes();
                ChimeraLogger.Log($"[FacilitySceneIntegrationTest] Available scenes: {string.Join(", ", availableScenes)}");
            }
            else
            {
                ChimeraLogger.LogWarning("[FacilitySceneIntegrationTest] FacilityManager not available or not initialized");
            }
        }

        /// <summary>
        /// Test facility scene loading (Editor safe)
        /// </summary>
        [ContextMenu("Test Scene Loading Capability")]
        public void TestSceneLoadingCapability()
        {
            ChimeraLogger.Log("[FacilitySceneIntegrationTest] Testing scene loading capability...");

            foreach (string sceneName in _testFacilityScenes)
            {
                bool isValid = SceneConstants.IsValidScene(sceneName);
                bool canLoad = BuildSettingsValidator.CanLoadScene(sceneName);
                int buildIndex = SceneConstants.GetBuildIndex(sceneName);

                string status = $"Scene: {sceneName} | Valid: {isValid} | Can Load: {canLoad} | Build Index: {buildIndex}";
                ChimeraLogger.Log($"[FacilitySceneIntegrationTest] {status}");
            }
        }
    }
}
