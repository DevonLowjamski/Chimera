using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using ProjectChimera.Core.Logging;

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
            ChimeraLogger.LogInfo("EDITOR", "Refreshing assembly definitions...");

            // Force Unity to refresh assets
            AssetDatabase.Refresh();

            // Request script compilation
            CompilationPipeline.RequestScriptCompilation();

            ChimeraLogger.LogInfo("EDITOR", "Assembly definitions refreshed successfully");
        }
        
        [MenuItem("Tools/Project Chimera/Force Recompile")]
        public static void ForceRecompile()
        {
            ChimeraLogger.LogInfo("EDITOR", "Force recompiling project...");

            // Refresh assets
            AssetDatabase.Refresh();

            // Force garbage collection
            System.GC.Collect();

            // Request script compilation
            CompilationPipeline.RequestScriptCompilation();

            ChimeraLogger.LogInfo("EDITOR", "Force recompile completed");
        }
    }
} 