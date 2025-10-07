using ProjectChimera.Core.Logging;
using UnityEngine;
using System;
using Logger = ProjectChimera.Core.Logging.ChimeraLogger;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Implementation for time scale management including speed levels and penalties
    /// </summary>
    public class TimeScale : ITimeScale
    {
        // Speed level to multiplier mapping
        private static readonly float[] SpeedMultipliers = { 0.5f, 1.0f, 2.0f, 4.0f, 8.0f };

        // Risk/reward penalty multipliers for each speed level
        private static readonly float[] PenaltyMultipliers = { 1.1f, 1.0f, 0.95f, 0.85f, 0.7f };

        private TimeSpeedLevel _currentSpeedLevel = TimeSpeedLevel.Normal;
        private float _currentTimeScale = 1.0f;
        private bool _enableSpeedPenalties = true;
        private float _penaltySeverityMultiplier = 1.0f;
        private ITimeEvents _timeEvents;

        public TimeSpeedLevel CurrentSpeedLevel
        {
            get => _currentSpeedLevel;
            private set
            {
                var previousScale = _currentTimeScale;
                _currentSpeedLevel = value;
                _currentTimeScale = SpeedMultipliers[(int)value];

                // Notify events if available
                _timeEvents?.NotifyTimeScaleListeners(previousScale, _currentTimeScale);
                _timeEvents?.NotifySpeedPenaltyListeners(_currentSpeedLevel, CurrentPenaltyMultiplier);
            }
        }

        public float CurrentTimeScale => _currentTimeScale;

        public float CurrentPenaltyMultiplier
        {
            get
            {
                if (!_enableSpeedPenalties) return 1.0f;

                float basePenalty = PenaltyMultipliers[(int)_currentSpeedLevel];
                return Mathf.Lerp(1.0f, basePenalty, _penaltySeverityMultiplier);
            }
        }

        public bool HasSpeedPenalty => _enableSpeedPenalties && CurrentSpeedLevel > TimeSpeedLevel.Normal;
        public bool SpeedPenaltiesEnabled => _enableSpeedPenalties;

        public TimeScale(ITimeEvents timeEvents = null)
        {
            _timeEvents = timeEvents;
        }

        public void SetSpeedLevel(TimeSpeedLevel newSpeedLevel)
        {
            if (newSpeedLevel == _currentSpeedLevel) return;

            Logger.Log("TIME", $"Speed level set to {newSpeedLevel}");
            CurrentSpeedLevel = newSpeedLevel;

            _timeEvents?.TriggerTimeScaleChanged(_currentTimeScale);
            _timeEvents?.TriggerSpeedPenaltyChanged();
        }

        public void IncreaseSpeedLevel()
        {
            if (CurrentSpeedLevel < TimeSpeedLevel.Maximum)
            {
                SetSpeedLevel(CurrentSpeedLevel + 1);
            }
        }

        public void DecreaseSpeedLevel()
        {
            if (CurrentSpeedLevel > TimeSpeedLevel.Slow)
            {
                SetSpeedLevel(CurrentSpeedLevel - 1);
            }
        }

        public void ResetSpeedLevel()
        {
            SetSpeedLevel(TimeSpeedLevel.Normal);
        }

        public float ApplySpeedPenalty(float baseValue)
        {
            return baseValue * CurrentPenaltyMultiplier;
        }

        public (float min, float max) ApplySpeedPenaltyToRange(float minValue, float maxValue)
        {
            float penalty = CurrentPenaltyMultiplier;
            return (minValue * penalty, maxValue * penalty);
        }

        public string GetSpeedLevelDisplayString()
        {
            return CurrentSpeedLevel switch
            {
                TimeSpeedLevel.Slow => "0.5x (Slow)",
                TimeSpeedLevel.Normal => "1x (Normal)",
                TimeSpeedLevel.Fast => "2x (Fast)",
                TimeSpeedLevel.VeryFast => "4x (Very Fast)",
                TimeSpeedLevel.Maximum => "8x (Maximum)",
                _ => $"{CurrentTimeScale:F1}x"
            };
        }

        public string GetPenaltyDescription()
        {
            if (!_enableSpeedPenalties)
            {
                return "Speed penalties disabled";
            }

            int percentage = GetPenaltyPercentage();
            if (percentage > 0)
            {
                return $"+{percentage}% quality bonus";
            }
            else if (percentage < 0)
            {
                return $"{percentage}% quality penalty";
            }
            else
            {
                return "No penalty or bonus";
            }
        }

        public int GetPenaltyPercentage()
        {
            return Mathf.RoundToInt((CurrentPenaltyMultiplier - 1.0f) * 100);
        }

        public float RealTimeToGameTime(float realTime)
        {
            return realTime * CurrentTimeScale;
        }

        public float GameTimeToRealTime(float gameTime)
        {
            return gameTime / CurrentTimeScale;
        }

        public float GetScaledDeltaTime()
        {
            return Time.unscaledDeltaTime * CurrentTimeScale;
        }

        public string GetEstimatedRealTime(float gameTimeSeconds)
        {
            if (CurrentTimeScale <= 0) return "∞";

            float realTimeSeconds = gameTimeSeconds / CurrentTimeScale;
            return RefactoredTimeManager.FormatDuration(realTimeSeconds);
        }

        public void SetSpeedPenaltiesEnabled(bool enabled)
        {
            _enableSpeedPenalties = enabled;
            Logger.Log("TIME", $"Speed penalties {(enabled ? "enabled" : "disabled")}");

            _timeEvents?.TriggerSpeedPenaltyChanged();
        }

        public void SetPenaltySeverityMultiplier(float multiplier)
        {
            _penaltySeverityMultiplier = Mathf.Clamp01(multiplier);
            Logger.Log("TIME", $"Penalty severity set to {_penaltySeverityMultiplier:F2}");

            _timeEvents?.TriggerSpeedPenaltyChanged();
        }

        public void SetTimeEvents(ITimeEvents timeEvents)
        {
            _timeEvents = timeEvents;
        }
    }
}
