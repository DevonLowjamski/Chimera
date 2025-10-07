using UnityEngine;
using UnityEditor;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Editor
{
    [InitializeOnLoad]
    public static class ForceRefreshEditor
    {
        static ForceRefreshEditor()
        {
            // Force asset database refresh
            AssetDatabase.Refresh();
            
            // Force recompilation
            EditorApplication.LockReloadAssemblies();
            EditorApplication.UnlockReloadAssemblies();
        }
    }
}