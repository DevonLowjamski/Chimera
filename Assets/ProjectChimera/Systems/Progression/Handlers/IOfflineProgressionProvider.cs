using System;
using System.Threading.Tasks;
using ProjectChimera.Data.Save.Structures;

namespace ProjectChimera.Systems.Progression
{
    /// <summary>
    /// Interface for domain-specific offline progression providers
    /// </summary>
    public interface IOfflineProgressionProvider
    {
        string GetProviderId();
        float GetPriority();
        
        Task<OfflineProgressionResult> CalculateOfflineProgressionAsync(TimeSpan offlineTime);
        Task ApplyOfflineProgressionAsync(OfflineProgressionResult result);
    }
}
