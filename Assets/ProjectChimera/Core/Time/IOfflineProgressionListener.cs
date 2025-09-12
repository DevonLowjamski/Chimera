using System;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Interface for components that need to be notified about offline progression events
    /// </summary>
    public interface IOfflineProgressionListener
    {
        /// <summary>
        /// Called when offline progression starts being calculated
        /// </summary>
        /// <param name="offlineTime">How long the game was offline</param>
        void OnOfflineProgressionStart(TimeSpan offlineTime);

        /// <summary>
        /// Called when offline progression calculation is complete
        /// </summary>
        /// <param name="progressionResults">The results of offline progression</param>
        void OnOfflineProgressionComplete(object progressionResults);

        /// <summary>
        /// Called when offline progression is applied to game state
        /// </summary>
        void OnOfflineProgressionApplied();
    }
}