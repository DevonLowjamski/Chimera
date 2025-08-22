using UnityEngine;
using ProjectChimera.Core.DependencyInjection;
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
            Debug.Log("[FacilitySceneIntegrationTest] Starting facility scene integration test...");

            // Wait for services to initialize
            yield return new WaitForSeconds(1f);

            // Get services
            _facilityManager = ServiceLocator.Instance.TryResolve<FacilityManager>();
            _sceneLoader = ServiceLocator.Instance.TryResolve<ISceneLoader>();

            if (_facilityManager == null)
            {
                Debug.LogError("[FacilitySceneIntegrationTest] FacilityManager not available");
                yield break;
            }

            if (_sceneLoader == null)
            {
                Debug.LogError("[FacilitySceneIntegrationTest] SceneLoader not available");
                yield break;
            }

            Debug.Log("[FacilitySceneIntegrationTest] Services ready - starting test sequence");

            // Test 1: Verify initial state
            yield return StartCoroutine(TestInitialState());

            // Test 2: Test facility scene switching
            yield return StartCoroutine(TestFacilitySceneSwitching());

            // Test 3: Test scene validation
            yield return StartCoroutine(TestSceneValidation());

            Debug.Log("[FacilitySceneIntegrationTest] Integration test completed!");
        }

        /// <summary>
        /// Test initial facility manager state
        /// </summary>
        private IEnumerator TestInitialState()
        {
            Debug.Log("[FacilitySceneIntegrationTest] === Test 1: Initial State ===");

            if (!_facilityManager.IsInitialized)
            {
                Debug.LogError("[FacilitySceneIntegrationTest] FacilityManager not initialized!");
                yield break;
            }

            var stats = _facilityManager.GetProgressionStatistics();
            Debug.Log($"[FacilitySceneIntegrationTest] Current facility tier: {stats.CurrentTier}");
            Debug.Log($"[FacilitySceneIntegrationTest] Owned facilities: {stats.OwnedFacilities}");
            Debug.Log($"[FacilitySceneIntegrationTest] Can upgrade: {stats.CanUpgrade}");

            var availableScenes = _facilityManager.GetAvailableFacilityScenes();
            Debug.Log($"[FacilitySceneIntegrationTest] Available facility scenes: {string.Join(", ", availableScenes)}");

            yield return new WaitForSeconds(1f);
        }

        /// <summary>
        /// Test facility scene switching
        /// </summary>
        private IEnumerator TestFacilitySceneSwitching()
        {
            Debug.Log("[FacilitySceneIntegrationTest] === Test 2: Facility Scene Switching ===");

            foreach (string sceneName in _testFacilityScenes)
            {
                Debug.Log($"[FacilitySceneIntegrationTest] Testing transition to: {sceneName}");

                // Validate scene before attempting load
                if (!SceneConstants.IsValidScene(sceneName))
                {
                    Debug.LogWarning($"[FacilitySceneIntegrationTest] Scene {sceneName} not valid - skipping");
                    continue;
                }

                // Test scene loading capability
                bool canLoad = BuildSettingsValidator.CanLoadScene(sceneName);
                Debug.Log($"[FacilitySceneIntegrationTest] Can load {sceneName}: {canLoad}");

                if (canLoad)
                {
                    // Get facility info for this scene
                    var facilityInfo = _facilityManager.GetFacilityInfoForScene(sceneName);
                    if (facilityInfo != null)
                    {
                        Debug.Log($"[FacilitySceneIntegrationTest] Facility info: {facilityInfo.TierName} ({facilityInfo.BuildIndex})");
                    }

                    // Note: Not actually loading scenes in test to avoid disrupting Unity Editor
                    // In actual gameplay, this would call:
                    // _facilityManager.LoadFacilitySceneByName(sceneName);
                    
                    Debug.Log($"[FacilitySceneIntegrationTest] Scene {sceneName} ready for loading");
                }

                yield return new WaitForSeconds(_testDelayBetweenTransitions);
            }
        }

        /// <summary>
        /// Test scene validation integration
        /// </summary>
        private IEnumerator TestSceneValidation()
        {
            Debug.Log("[FacilitySceneIntegrationTest] === Test 3: Scene Validation ===");

            // Test BuildSettingsValidator integration
            bool buildSettingsValid = BuildSettingsValidator.ValidateRuntimeBuildSettings();
            Debug.Log($"[FacilitySceneIntegrationTest] Build Settings valid: {buildSettingsValid}");

            // Test scene constants integration
            foreach (string sceneName in _testFacilityScenes)
            {
                int buildIndex = SceneConstants.GetBuildIndex(sceneName);
                string resolvedName = SceneConstants.GetSceneName(buildIndex);
                
                bool resolutionValid = (resolvedName == sceneName);
                Debug.Log($"[FacilitySceneIntegrationTest] Scene resolution {sceneName}: {buildIndex} -> {resolvedName} ({(resolutionValid ? "✓" : "✗")})");
                
                bool isWarehouse = SceneConstants.IsWarehouseScene(sceneName);
                Debug.Log($"[FacilitySceneIntegrationTest] {sceneName} is warehouse scene: {isWarehouse}");
            }

            yield return new WaitForSeconds(1f);

            Debug.Log("[FacilitySceneIntegrationTest] Scene validation test completed");
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
                Debug.LogWarning("[FacilitySceneIntegrationTest] Test can only be run in Play mode");
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
                _facilityManager = ServiceLocator.Instance.TryResolve<FacilityManager>();
            }

            if (_facilityManager != null && _facilityManager.IsInitialized)
            {
                var stats = _facilityManager.GetProgressionStatistics();
                Debug.Log($"[FacilitySceneIntegrationTest] === Facility Status ===\n{stats}");
                
                var availableScenes = _facilityManager.GetAvailableFacilityScenes();
                Debug.Log($"[FacilitySceneIntegrationTest] Available scenes: {string.Join(", ", availableScenes)}");
            }
            else
            {
                Debug.LogWarning("[FacilitySceneIntegrationTest] FacilityManager not available or not initialized");
            }
        }

        /// <summary>
        /// Test facility scene loading (Editor safe)
        /// </summary>
        [ContextMenu("Test Scene Loading Capability")]
        public void TestSceneLoadingCapability()
        {
            Debug.Log("[FacilitySceneIntegrationTest] Testing scene loading capability...");

            foreach (string sceneName in _testFacilityScenes)
            {
                bool isValid = SceneConstants.IsValidScene(sceneName);
                bool canLoad = BuildSettingsValidator.CanLoadScene(sceneName);
                int buildIndex = SceneConstants.GetBuildIndex(sceneName);
                
                string status = $"Scene: {sceneName} | Valid: {isValid} | Can Load: {canLoad} | Build Index: {buildIndex}";
                Debug.Log($"[FacilitySceneIntegrationTest] {status}");
            }
        }
    }
}