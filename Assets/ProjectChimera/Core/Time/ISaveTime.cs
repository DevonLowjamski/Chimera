using System;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Interface for time tracking, persistence, and display formatting
    /// </summary>
    public interface ISaveTime
    {
        DateTime SessionStartTime { get; }
        DateTime GameStartTime { get; }
        TimeSpan TotalGameTime { get; }
        TimeSpan SessionDuration { get; }
        bool IsTimePaused { get; }

        void SetGameStartTime(DateTime startTime);
        void SetSessionStartTime(DateTime startTime);
        void TrackFrameTime();
        void UpdateAccumulatedTimes(float deltaTime, float currentTimeScale, bool isPaused);

        string GetGameTimeString();
        string GetRealTimeString();
        string GetCombinedTimeString();
        string GetTimeEfficiencyString();
        string GetCompactTimeStatus();
        string GetDetailedTimeStatus();
        
        TimeDisplayData GetTimeDisplayData();
        string FormatDurationWithConfig(float seconds);
        
        void SetTimeFormat(TimeDisplayFormat format);
        void SetDisplayOptions(bool showCombined, bool showEfficiency, bool showPenalty);

        void Initialize();
        void Shutdown();
    }
}
