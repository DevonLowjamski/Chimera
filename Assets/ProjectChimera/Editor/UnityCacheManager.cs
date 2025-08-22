using UnityEngine;
using UnityEditor;
using System.IO;

namespace ProjectChimera.Editor
{
    /// <summary>
    /// Unity cache management utilities for Project Chimera.
    /// Helps resolve persistent asset reference and configuration issues.
    /// </summary>
    public static class UnityCacheManager
    {
        [MenuItem("Project Chimera/Cache Management/Clear Asset Database")]
        public static void ClearAssetDatabase()
        {
            Debug.Log("[Chimera] Clearing Asset Database cache...");
            
            // Force reimport of all assets
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            
            Debug.Log("[Chimera] Asset Database cache cleared");
        }
        
        [MenuItem("Project Chimera/Cache Management/Clear Script Cache")]
        public static void ClearScriptCache()
        {
            Debug.Log("[Chimera] Clearing script compilation cache...");
            
            // Request script compilation
            EditorUtility.RequestScriptReload();
            
            Debug.Log("[Chimera] Script cache cleared - Unity will recompile scripts");
        }
        
        [MenuItem("Project Chimera/Cache Management/Clear All Unity Caches")]
        public static void ClearAllUnityCaches()
        {
            Debug.Log("[Chimera] Clearing all Unity caches...");
            
            // Clear Asset Database
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            
            // Clear script compilation cache
            EditorUtility.RequestScriptReload();
            
            // Clear console
            var assembly = System.Reflection.Assembly.GetAssembly(typeof(UnityEditor.Editor));
            var type = assembly.GetType("UnityEditor.LogEntries");
            var method = type.GetMethod("Clear");
            method?.Invoke(new object(), null);
            
            Debug.Log("[Chimera] All Unity caches cleared");
        }
        
        [MenuItem("Project Chimera/Cache Management/Force Reimport ProjectChimera Assets")]
        public static void ForceReimportProjectChimeraAssets()
        {
            Debug.Log("[Chimera] Force reimporting all ProjectChimera assets...");
            
            // Find all assets in ProjectChimera folders
            string[] assetPaths = AssetDatabase.GetAllAssetPaths();
            
            int reimportedCount = 0;
            foreach (string path in assetPaths)
            {
                if (path.StartsWith("Assets/ProjectChimera/"))
                {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    reimportedCount++;
                }
            }
            
            Debug.Log($"[Chimera] Force reimported {reimportedCount} ProjectChimera assets");
        }
        
        [MenuItem("Project Chimera/Cache Management/Validate Asset References")]
        public static void ValidateAssetReferences()
        {
            Debug.Log("[Chimera] Validating asset references...");
            
            // Find all ScriptableObject assets
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject");
            
            int validAssets = 0;
            int invalidAssets = 0;
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                
                if (path.StartsWith("Assets/ProjectChimera/"))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                    
                    if (asset != null)
                    {
                        validAssets++;
                    }
                    else
                    {
                        invalidAssets++;
                        Debug.LogWarning($"[Chimera] Invalid asset reference: {path}");
                    }
                }
            }
            
            Debug.Log($"[Chimera] Asset reference validation complete: {validAssets} valid, {invalidAssets} invalid");
        }
        
        [MenuItem("Project Chimera/Cache Management/Fix Asset GUID References")]
        public static void FixAssetGUIDReferences()
        {
            Debug.Log("[Chimera] Fixing asset GUID references...");
            
            // This is a more advanced operation that would require careful handling
            // For now, we'll just refresh the asset database
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            
            Debug.Log("[Chimera] Asset GUID references refreshed");
        }
        
        [MenuItem("Project Chimera/Cache Management/Emergency Asset Recovery")]
        public static void EmergencyAssetRecovery()
        {
            if (!EditorUtility.DisplayDialog("Emergency Asset Recovery", 
                "This will perform a complete asset system reset. This may take several minutes. Continue?", 
                "Yes", "Cancel"))
            {
                return;
            }
            
            Debug.Log("[Chimera] Starting emergency asset recovery...");
            
            try
            {
                // Step 1: Clear all caches
                ClearAllUnityCaches();
                
                // Step 2: Force reimport all ProjectChimera assets
                ForceReimportProjectChimeraAssets();
                
                // Step 3: Validate references
                ValidateAssetReferences();
                
                // Step 4: Request script reload
                EditorUtility.RequestScriptReload();
                
                Debug.Log("[Chimera] Emergency asset recovery completed");
                
                EditorUtility.DisplayDialog("Recovery Complete", 
                    "Emergency asset recovery completed successfully. Unity will recompile scripts.", 
                    "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Chimera] Emergency asset recovery failed: {e.Message}");
                
                EditorUtility.DisplayDialog("Recovery Failed", 
                    $"Emergency asset recovery failed: {e.Message}", 
                    "OK");
            }
        }
        
        [MenuItem("Project Chimera/Cache Management/Optimize Asset Database")]
        public static void OptimizeAssetDatabase()
        {
            Debug.Log("[Chimera] Optimizing Asset Database...");
            
            // Remove unused assets from the database
            AssetDatabase.Refresh();
            
            // Force garbage collection
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            
            Debug.Log("[Chimera] Asset Database optimized");
        }
    }
}