using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;

namespace ProjectChimera.Editor
{
    /// <summary>
    /// Tool to refresh assembly definitions and force Unity to recompile.
    /// Use this when assembly references are changed or when experiencing compilation issues.
    /// </summary>
    public static class AssemblyRefreshTool
    {
        [MenuItem("Tools/Project Chimera/Refresh Assembly Definitions")]
        public static void RefreshAssemblyDefinitions()
        {
            Debug.Log("Refreshing Assembly Definitions...");
            
            // Force Unity to refresh assets
            AssetDatabase.Refresh();
            
            // Request script compilation
            CompilationPipeline.RequestScriptCompilation();
            
            Debug.Log("Assembly refresh completed. Check console for compilation results.");
        }
        
        [MenuItem("Tools/Project Chimera/Force Recompile")]
        public static void ForceRecompile()
        {
            Debug.Log("Forcing Unity recompilation...");
            
            // Refresh assets
            AssetDatabase.Refresh();
            
            // Force garbage collection
            System.GC.Collect();
            
            // Request script compilation
            CompilationPipeline.RequestScriptCompilation();
            
            Debug.Log("Forced recompilation requested. Check console for compilation results.");
        }
    }
} 