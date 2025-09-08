using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// System health checker component placeholder
    /// </summary>
    public class SystemHealthChecker : MonoBehaviour
    {
        public void RegisterHealthProvider(string systemId, IHealthCheckProvider provider)
        {
            ChimeraLogger.Log($"[SystemHealthChecker] Registered health provider for {systemId}");
        }
    }
}
