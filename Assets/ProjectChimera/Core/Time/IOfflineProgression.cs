using System;
using System.Collections;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Interface for offline progression calculation and management
    /// </summary>
    public interface IOfflineProgression
    {
        bool EnableOfflineProgression { get; set; }
        DateTime LastSaveTime { get; set; }

        void RegisterOfflineProgressionListener(IOfflineProgressionListener listener);
        void UnregisterOfflineProgressionListener(IOfflineProgressionListener listener);
        
        IEnumerator CalculateOfflineProgressionCoroutine();
        void NotifyOfflineProgressionListeners(float offlineHours);
        
        // Testing support
        bool CanTriggerOfflineEvents();
        bool CanCalculateOfflineTime();
        int GetOfflineProgressionListenerCount();
        void TriggerOfflineProgressionForTesting(float offlineHours);
        
        void Initialize();
        void Shutdown();
    }
}
