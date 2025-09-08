using System;
using System.Threading.Tasks;

namespace ProjectChimera.Systems.Progression
{
    /// <summary>
    /// Interface for domain-specific offline progression providers
    /// </summary>
    public interface IOfflineProgressionProvider
    {
        string GetProviderId();
        float GetPriority();
        
        Task<OfflineProgressionCalculationResult> CalculateOfflineProgressionAsync(TimeSpan offlineTime);
        Task ApplyOfflineProgressionAsync(OfflineProgressionResult result);
    }
}
