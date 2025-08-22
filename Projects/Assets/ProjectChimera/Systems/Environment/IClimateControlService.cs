using ProjectChimera.Data.Shared;

namespace ProjectChimera.Systems.Environment
{
    /// <summary>
    /// PC014-2b: Interface for climate control service
    /// </summary>
    public interface IClimateControlService : IEnvironmentalService
    {
        void SetOptimalConditions(string zoneId, EnvironmentalConditions conditions);
        EnvironmentalConditions GetCurrentConditions(string zoneId);
        void EnableStressMonitoring(string zoneId, bool enabled);
        void UpdateClimateProfile(string zoneId, ClimateProfile profile);
    }

    [System.Serializable]
    public class ClimateProfile
    {
        public string Name;
        public EnvironmentalConditions OptimalConditions;
        public EnvironmentalConditions ToleranceRanges;
        public bool EnableAutomaticAdjustment;
        public float AggressivenessLevel;
    }
}