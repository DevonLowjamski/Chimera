using ProjectChimera.Core.Events;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Interface for time-related event management and listener registration
    /// </summary>
    public interface ITimeEvents
    {
        void RegisterTimeScaleListener(ITimeScaleListener listener);
        void UnregisterTimeScaleListener(ITimeScaleListener listener);
        void RegisterSpeedPenaltyListener(ISpeedPenaltyListener listener);
        void UnregisterSpeedPenaltyListener(ISpeedPenaltyListener listener);

        void NotifyTimeScaleListeners(float previousScale, float newScale);
        void NotifySpeedPenaltyListeners(TimeSpeedLevel speedLevel, float penaltyMultiplier);

        void SetTimeScaleEvent(FloatGameEventSO timeScaleEvent);
        void SetTimePausedEvent(SimpleGameEventSO timePausedEvent);
        void SetTimeResumedEvent(SimpleGameEventSO timeResumedEvent);
        void SetSpeedPenaltyEvent(SimpleGameEventSO speedPenaltyEvent);

        void TriggerTimeScaleChanged(float newScale);
        void TriggerTimePaused();
        void TriggerTimeResumed();
        void TriggerSpeedPenaltyChanged();

        void Initialize();
        void Shutdown();
    }
}
