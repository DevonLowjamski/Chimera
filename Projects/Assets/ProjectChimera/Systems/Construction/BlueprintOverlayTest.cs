using UnityEngine;
using ProjectChimera.Data.Construction;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Test component for the Blueprint Overlay Rendering system.
    /// Validates unlit rendering, outline effects, and integration with placement system.
    /// </summary>
    public class BlueprintOverlayTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool _runTestsOnStart = false;
        [SerializeField] private SchematicSO _testSchematic;
        [SerializeField] private Vector3 _testPosition = Vector3.zero;
        [SerializeField] private float _testDuration = 5f;
        
        [Header("Test Results")]
        [SerializeField] private bool _testsPassed = false;
        [SerializeField] private string _testResults = "";
        
        private BlueprintOverlayRenderer _overlayRenderer;
        private BlueprintOverlayIntegration _overlayIntegration;
        private GridPlacementController _placementController;
        
        private void Start()
        {
            if (_runTestsOnStart)
            {
                Invoke(nameof(RunOverlayTests), 1f);
            }
        }
        
        [ContextMenu("Run Overlay Tests")]
        public void RunOverlayTests()
        {
            var results = new System.Text.StringBuilder();
            bool allTestsPassed = true;
            
            results.AppendLine("=== Blueprint Overlay System Tests ===");
            
            // Test 1: System References
            allTestsPassed &= TestSystemReferences(results);
            
            // Test 2: Overlay Creation
            allTestsPassed &= TestOverlayCreation(results);
            
            // Test 3: Material Rendering
            allTestsPassed &= TestMaterialRendering(results);
            
            // Test 4: Validation State Changes
            allTestsPassed &= TestValidationStates(results);
            
            // Test 5: Animation System
            allTestsPassed &= TestAnimationSystem(results);
            
            // Test 6: Performance Metrics
            allTestsPassed &= TestPerformanceMetrics(results);
            
            _testsPassed = allTestsPassed;
            _testResults = results.ToString();
            
            results.AppendLine($"\\n=== OVERALL RESULT: {(allTestsPassed ? "PASSED" : "FAILED")} ===");
            Debug.Log(results.ToString());
        }
        
        private bool TestSystemReferences(System.Text.StringBuilder results)
        {
            results.AppendLine("\\n1. Testing System References:");
            
            _overlayRenderer = FindObjectOfType<BlueprintOverlayRenderer>();
            _overlayIntegration = FindObjectOfType<BlueprintOverlayIntegration>();
            _placementController = FindObjectOfType<GridPlacementController>();
            
            bool rendererFound = _overlayRenderer != null;
            bool integrationFound = _overlayIntegration != null;
            bool placementFound = _placementController != null;
            
            results.AppendLine($"   - BlueprintOverlayRenderer: {(rendererFound ? "FOUND" : "NOT FOUND")}");
            results.AppendLine($"   - BlueprintOverlayIntegration: {(integrationFound ? "FOUND" : "NOT FOUND")}");
            results.AppendLine($"   - GridPlacementController: {(placementFound ? "FOUND" : "NOT FOUND")}");
            
            bool passed = rendererFound || integrationFound; // At least one should exist
            results.AppendLine($"   Result: {(passed ? "PASS" : "FAIL")}");
            
            return passed;
        }
        
        private bool TestOverlayCreation(System.Text.StringBuilder results)
        {
            results.AppendLine("\\n2. Testing Overlay Creation:");
            
            if (_overlayRenderer == null || _testSchematic == null)
            {
                results.AppendLine("   SKIP - Required components not available");
                return false;
            }
            
            var overlay = _overlayRenderer.CreateSchematicOverlay(_testSchematic, _testPosition, Quaternion.identity);
            
            bool overlayCreated = overlay != null;
            bool hasObjects = overlay?.OverlayObjects.Count > 0;
            bool correctPosition = overlay != null && Vector3.Distance(overlay.Position, _testPosition) < 0.1f;
            
            results.AppendLine($"   Schematic: {_testSchematic.SchematicName}");
            results.AppendLine($"   Overlay Created: {overlayCreated}");
            results.AppendLine($"   Has Objects: {hasObjects}");
            results.AppendLine($"   Position Correct: {correctPosition}");
            
            if (overlay != null)
            {
                results.AppendLine($"   Object Count: {overlay.OverlayObjects.Count}");
                results.AppendLine($"   Overlay Type: {overlay.OverlayType}");
            }
            
            bool passed = overlayCreated && hasObjects && correctPosition;
            results.AppendLine($"   Result: {(passed ? "PASS" : "FAIL")}");
            
            return passed;
        }
        
        private bool TestMaterialRendering(System.Text.StringBuilder results)
        {
            results.AppendLine("\\n3. Testing Material Rendering:");
            
            if (_overlayRenderer == null)
            {
                results.AppendLine("   SKIP - OverlayRenderer not available");
                return false;
            }
            
            bool hasBlueprintMaterial = _overlayRenderer.BlueprintMaterial != null;
            bool cameraConfigured = _overlayRenderer.OverlayCamera != null;
            bool renderingEnabled = _overlayRenderer.OverlayRenderingEnabled;
            
            results.AppendLine($"   Blueprint Material: {(hasBlueprintMaterial ? "FOUND" : "NOT FOUND")}");
            results.AppendLine($"   Overlay Camera: {(cameraConfigured ? "CONFIGURED" : "NOT CONFIGURED")}");
            results.AppendLine($"   Rendering Enabled: {renderingEnabled}");
            
            if (hasBlueprintMaterial)
            {
                var material = _overlayRenderer.BlueprintMaterial;
                results.AppendLine($"   Material Shader: {material.shader.name}");
                results.AppendLine($"   Material Queue: {material.renderQueue}");
            }
            
            bool passed = hasBlueprintMaterial && cameraConfigured;
            results.AppendLine($"   Result: {(passed ? "PASS" : "FAIL")}");
            
            return passed;
        }
        
        private bool TestValidationStates(System.Text.StringBuilder results)
        {
            results.AppendLine("\\n4. Testing Validation States:");
            
            if (_overlayRenderer == null || _testSchematic == null)
            {
                results.AppendLine("   SKIP - Required components not available");
                return false;
            }
            
            // Create test overlay
            var overlay = _overlayRenderer.CreateSchematicOverlay(_testSchematic, _testPosition, Quaternion.identity);
            if (overlay == null)
            {
                results.AppendLine("   FAIL - Could not create test overlay");
                return false;
            }
            
            var initialType = overlay.OverlayType;
            results.AppendLine($"   Initial Type: {initialType}");
            
            // Test valid placement state
            _overlayRenderer.UpdateOverlayValidation(overlay, true);
            var validType = overlay.OverlayType;
            results.AppendLine($"   Valid State Type: {validType}");
            
            // Test invalid placement state
            _overlayRenderer.UpdateOverlayValidation(overlay, false);
            var invalidType = overlay.OverlayType;
            results.AppendLine($"   Invalid State Type: {invalidType}");
            
            bool stateChanges = validType != initialType || invalidType != validType;
            bool passed = stateChanges;
            
            results.AppendLine($"   State Changes: {stateChanges}");
            results.AppendLine($"   Result: {(passed ? "PASS" : "FAIL")}");
            
            // Cleanup
            _overlayRenderer.DestroyOverlay(overlay);
            
            return passed;
        }
        
        private bool TestAnimationSystem(System.Text.StringBuilder results)
        {
            results.AppendLine("\\n5. Testing Animation System:");
            
            if (_overlayRenderer == null || _testSchematic == null)
            {
                results.AppendLine("   SKIP - Required components not available");
                return false;
            }
            
            // Create test overlay
            var overlay = _overlayRenderer.CreateSchematicOverlay(_testSchematic, _testPosition, Quaternion.identity);
            if (overlay == null)
            {
                results.AppendLine("   FAIL - Could not create test overlay");
                return false;
            }
            
            bool hasAnimationState = overlay.AnimationState != null;
            bool isVisible = overlay.IsVisible;
            float creationTime = overlay.CreationTime;
            
            results.AppendLine($"   Animation State: {(hasAnimationState ? "PRESENT" : "MISSING")}");
            results.AppendLine($"   Initially Visible: {isVisible}");
            results.AppendLine($"   Creation Time: {creationTime}");
            
            // Test smooth movement
            Vector3 newPosition = _testPosition + Vector3.right * 2f;
            _overlayRenderer.MoveOverlay(overlay, newPosition, Quaternion.identity, true);
            
            bool positionUpdated = Vector3.Distance(overlay.Position, newPosition) < 0.1f;
            results.AppendLine($"   Position Updated: {positionUpdated}");
            
            bool passed = hasAnimationState && isVisible && positionUpdated;
            results.AppendLine($"   Result: {(passed ? "PASS" : "FAIL")}");
            
            // Cleanup
            _overlayRenderer.DestroyOverlay(overlay);
            
            return passed;
        }
        
        private bool TestPerformanceMetrics(System.Text.StringBuilder results)
        {
            results.AppendLine("\\n6. Testing Performance Metrics:");
            
            if (_overlayRenderer == null)
            {
                results.AppendLine("   SKIP - OverlayRenderer not available");
                return false;
            }
            
            int initialOverlayCount = _overlayRenderer.ActiveOverlayCount;
            results.AppendLine($"   Initial Overlay Count: {initialOverlayCount}");
            
            // Test multiple overlay creation
            var overlays = new System.Collections.Generic.List<OverlayInstance>();
            int testOverlayCount = 5;
            
            for (int i = 0; i < testOverlayCount; i++)
            {
                if (_testSchematic != null)
                {
                    var overlay = _overlayRenderer.CreateSchematicOverlay(
                        _testSchematic, 
                        _testPosition + Vector3.right * i * 2f, 
                        Quaternion.identity
                    );
                    if (overlay != null)
                    {
                        overlays.Add(overlay);
                    }
                }
            }
            
            int finalOverlayCount = _overlayRenderer.ActiveOverlayCount;
            int createdCount = overlays.Count;
            
            results.AppendLine($"   Created Overlays: {createdCount}");
            results.AppendLine($"   Final Overlay Count: {finalOverlayCount}");
            
            // Test cleanup
            foreach (var overlay in overlays)
            {
                _overlayRenderer.DestroyOverlay(overlay);
            }
            
            int cleanupCount = _overlayRenderer.ActiveOverlayCount;
            bool cleanupSuccessful = cleanupCount == initialOverlayCount;
            
            results.AppendLine($"   After Cleanup Count: {cleanupCount}");
            results.AppendLine($"   Cleanup Successful: {cleanupSuccessful}");
            
            bool passed = createdCount > 0 && cleanupSuccessful;
            results.AppendLine($"   Result: {(passed ? "PASS" : "FAIL")}");
            
            return passed;
        }
        
        [ContextMenu("Test Overlay Movement")]
        public void TestOverlayMovement()
        {
            if (_overlayRenderer == null || _testSchematic == null)
            {
                Debug.LogWarning("Required components not available for movement test");
                return;
            }
            
            var overlay = _overlayRenderer.CreateSchematicOverlay(_testSchematic, _testPosition, Quaternion.identity);
            if (overlay != null)
            {
                StartCoroutine(MoveOverlayInPattern(overlay));
            }
        }
        
        private System.Collections.IEnumerator MoveOverlayInPattern(OverlayInstance overlay)
        {
            Vector3[] positions = {
                _testPosition,
                _testPosition + Vector3.right * 3f,
                _testPosition + Vector3.right * 3f + Vector3.forward * 3f,
                _testPosition + Vector3.forward * 3f,
                _testPosition
            };
            
            for (int i = 0; i < positions.Length; i++)
            {
                _overlayRenderer.MoveOverlay(overlay, positions[i], Quaternion.identity, true);
                yield return new WaitForSeconds(1f);
                
                // Alternate validation states
                _overlayRenderer.UpdateOverlayValidation(overlay, i % 2 == 0);
                yield return new WaitForSeconds(0.5f);
            }
            
            // Cleanup
            yield return new WaitForSeconds(1f);
            _overlayRenderer.DestroyOverlay(overlay);
            
            Debug.Log("Overlay movement test completed");
        }
        
        [ContextMenu("Test Integration System")]
        public void TestIntegrationSystem()
        {
            if (_overlayIntegration == null)
            {
                Debug.LogWarning("BlueprintOverlayIntegration not found");
                return;
            }
            
            Debug.Log($"Integration Enabled: {_overlayIntegration.OverlayIntegrationEnabled}");
            Debug.Log($"Has Active Overlay: {_overlayIntegration.HasActiveOverlay}");
            Debug.Log($"Overlay Renderer: {(_overlayIntegration.OverlayRenderer != null ? "Found" : "Not Found")}");
            
            if (_testSchematic != null)
            {
                _overlayIntegration.CreateSchematicOverlay(_testSchematic, _testPosition, Quaternion.identity);
                Debug.Log("Created test overlay through integration system");
                
                // Test validation
                var validationData = _overlayIntegration.GetValidationData();
                Debug.Log($"Validation: {validationData.IsValid} - {validationData.ValidationMessage}");
                
                // Cleanup after test duration
                Invoke(nameof(CleanupIntegrationTest), _testDuration);
            }
        }
        
        private void CleanupIntegrationTest()
        {
            if (_overlayIntegration != null)
            {
                _overlayIntegration.ClearCurrentOverlay();
                Debug.Log("Integration test cleanup completed");
            }
        }
    }
}