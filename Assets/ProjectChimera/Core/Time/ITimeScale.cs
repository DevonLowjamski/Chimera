using System;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Interface for time scale management including speed levels and penalties
    /// </summary>
    public interface ITimeScale
    {
        TimeSpeedLevel CurrentSpeedLevel { get; }
        float CurrentTimeScale { get; }
        float CurrentPenaltyMultiplier { get; }
        bool HasSpeedPenalty { get; }
        bool SpeedPenaltiesEnabled { get; }

        void SetSpeedLevel(TimeSpeedLevel newSpeedLevel);
        void IncreaseSpeedLevel();
        void DecreaseSpeedLevel();
        void ResetSpeedLevel();

        float ApplySpeedPenalty(float baseValue);
        (float min, float max) ApplySpeedPenaltyToRange(float minValue, float maxValue);
        string GetSpeedLevelDisplayString();
        string GetPenaltyDescription();
        int GetPenaltyPercentage();

        float RealTimeToGameTime(float realTime);
        float GameTimeToRealTime(float gameTime);
        float GetScaledDeltaTime();
        string GetEstimatedRealTime(float gameTimeSeconds);

        void SetSpeedPenaltiesEnabled(bool enabled);
        void SetPenaltySeverityMultiplier(float multiplier);
    }
}
