namespace ProjectChimera.Core
{
    /// <summary>
    /// Interface for components that need to be notified when game speed penalties are applied
    /// </summary>
    public interface ISpeedPenaltyListener
    {
        /// <summary>
        /// Called when a speed penalty is applied
        /// </summary>
        /// <param name="penaltyMultiplier">The penalty multiplier (0.0 to 1.0)</param>
        /// <param name="reason">The reason for the penalty</param>
        void OnSpeedPenaltyApplied(float penaltyMultiplier, string reason);

        /// <summary>
        /// Called when a speed penalty is removed
        /// </summary>
        /// <param name="reason">The reason the penalty was removed</param>
        void OnSpeedPenaltyRemoved(string reason);
    }
}