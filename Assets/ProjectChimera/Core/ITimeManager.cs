using System;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Interface for time management operations
    /// </summary>
    public interface ITimeManager
    {
        /// <summary>
        /// Current game time scale
        /// </summary>
        float TimeScale { get; set; }

        /// <summary>
        /// Whether the game is currently paused
        /// </summary>
        bool IsPaused { get; }

        /// <summary>
        /// Current game time
        /// </summary>
        DateTime CurrentGameTime { get; }

        /// <summary>
        /// Elapsed game time since start
        /// </summary>
        TimeSpan ElapsedGameTime { get; }

        /// <summary>
        /// Initialize the time manager
        /// </summary>
        void Initialize();

        /// <summary>
        /// Pause the game
        /// </summary>
        void Pause();

        /// <summary>
        /// Resume the game
        /// </summary>
        void Resume();

        /// <summary>
        /// Set time scale with validation
        /// </summary>
        /// <param name="scale">The desired time scale</param>
        /// <returns>True if time scale was set successfully</returns>
        bool SetTimeScale(float scale);

        /// <summary>
        /// Format current time according to display format
        /// </summary>
        /// <param name="format">The display format to use</param>
        /// <returns>Formatted time string</returns>
        string FormatCurrentTime(TimeDisplayFormat format);

        /// <summary>
        /// Register for offline progression listener
        /// </summary>
        /// <param name="listener">The listener to register</param>
        void RegisterOfflineProgressionListener(IOfflineProgressionListener listener);

        /// <summary>
        /// Unregister offline progression listener
        /// </summary>
        /// <param name="listener">The listener to unregister</param>
        void UnregisterOfflineProgressionListener(IOfflineProgressionListener listener);

        /// <summary>
        /// Register for speed penalty listener
        /// </summary>
        /// <param name="listener">The listener to register</param>
        void RegisterSpeedPenaltyListener(ISpeedPenaltyListener listener);

        /// <summary>
        /// Unregister speed penalty listener
        /// </summary>
        /// <param name="listener">The listener to unregister</param>
        void UnregisterSpeedPenaltyListener(ISpeedPenaltyListener listener);
    }
}