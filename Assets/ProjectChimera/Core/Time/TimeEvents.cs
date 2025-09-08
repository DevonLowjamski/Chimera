using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Events;
using System;
using System.Collections.Generic;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Implementation for time-related event management and listener registration
    /// </summary>
    public class TimeEvents : ITimeEvents
    {
        private readonly List<ITimeScaleListener> _timeScaleListeners = new List<ITimeScaleListener>();
        private readonly List<ISpeedPenaltyListener> _speedPenaltyListeners = new List<ISpeedPenaltyListener>();

        private FloatGameEventSO _onTimeScaleChanged;
        private SimpleGameEventSO _onTimePaused;
        private SimpleGameEventSO _onTimeResumed;
        private SimpleGameEventSO _onSpeedPenaltyChanged;

        public void Initialize()
        {
            ChimeraLogger.Log("[TimeEvents] Time events system initialized");
        }

        public void Shutdown()
        {
            _timeScaleListeners.Clear();
            _speedPenaltyListeners.Clear();
            ChimeraLogger.Log("[TimeEvents] Time events system shutdown");
        }

        public void RegisterTimeScaleListener(ITimeScaleListener listener)
        {
            if (listener != null && !_timeScaleListeners.Contains(listener))
            {
                _timeScaleListeners.Add(listener);
                ChimeraLogger.Log($"[TimeEvents] Registered time scale listener: {listener.GetType().Name}");
            }
        }

        public void UnregisterTimeScaleListener(ITimeScaleListener listener)
        {
            if (_timeScaleListeners.Remove(listener))
            {
                ChimeraLogger.Log($"[TimeEvents] Unregistered time scale listener: {listener.GetType().Name}");
            }
        }

        public void RegisterSpeedPenaltyListener(ISpeedPenaltyListener listener)
        {
            if (listener != null && !_speedPenaltyListeners.Contains(listener))
            {
                _speedPenaltyListeners.Add(listener);
                ChimeraLogger.Log($"[TimeEvents] Registered speed penalty listener: {listener.GetType().Name}");
            }
        }

        public void UnregisterSpeedPenaltyListener(ISpeedPenaltyListener listener)
        {
            if (_speedPenaltyListeners.Remove(listener))
            {
                ChimeraLogger.Log($"[TimeEvents] Unregistered speed penalty listener: {listener.GetType().Name}");
            }
        }

        public void NotifyTimeScaleListeners(float previousScale, float newScale)
        {
            for (int i = _timeScaleListeners.Count - 1; i >= 0; i--)
            {
                try
                {
                    _timeScaleListeners[i]?.OnTimeScaleChanged(previousScale, newScale);
                }
                catch (Exception e)
                {
                    ChimeraLogger.LogError($"[TimeEvents] Error notifying time scale listener: {e.Message}");
                    _timeScaleListeners.RemoveAt(i);
                }
            }
        }

        public void NotifySpeedPenaltyListeners(TimeSpeedLevel speedLevel, float penaltyMultiplier)
        {
            for (int i = _speedPenaltyListeners.Count - 1; i >= 0; i--)
            {
                try
                {
                    _speedPenaltyListeners[i]?.OnSpeedPenaltyChanged(speedLevel, penaltyMultiplier);
                }
                catch (Exception e)
                {
                    ChimeraLogger.LogError($"[TimeEvents] Error notifying speed penalty listener: {e.Message}");
                    _speedPenaltyListeners.RemoveAt(i);
                }
            }
        }

        public void SetTimeScaleEvent(FloatGameEventSO timeScaleEvent)
        {
            _onTimeScaleChanged = timeScaleEvent;
        }

        public void SetTimePausedEvent(SimpleGameEventSO timePausedEvent)
        {
            _onTimePaused = timePausedEvent;
        }

        public void SetTimeResumedEvent(SimpleGameEventSO timeResumedEvent)
        {
            _onTimeResumed = timeResumedEvent;
        }

        public void SetSpeedPenaltyEvent(SimpleGameEventSO speedPenaltyEvent)
        {
            _onSpeedPenaltyChanged = speedPenaltyEvent;
        }

        public void TriggerTimeScaleChanged(float newScale)
        {
            _onTimeScaleChanged?.Raise(newScale);
        }

        public void TriggerTimePaused()
        {
            _onTimePaused?.Raise();
        }

        public void TriggerTimeResumed()
        {
            _onTimeResumed?.Raise();
        }

        public void TriggerSpeedPenaltyChanged()
        {
            _onSpeedPenaltyChanged?.Raise();
        }
    }
}
