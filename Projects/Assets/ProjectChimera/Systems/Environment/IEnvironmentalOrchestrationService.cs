using System.Collections.Generic;

namespace ProjectChimera.Systems.Environment
{
    /// <summary>
    /// PC014-2b: Interface for environmental orchestration service
    /// </summary>
    public interface IEnvironmentalOrchestrationService : IEnvironmentalService
    {
        void OptimizeEnvironment(string zoneId);
        void RunAutomationSequence(string sequenceId);
        void SetGlobalEnvironmentalProfile(EnvironmentalProfile profile);
        void EnableAdvancedModeling(bool enabled);
        SystemHealthStatus GetOverallSystemHealth();
    }

    [System.Serializable]
    public class SystemHealthStatus
    {
        public float OverallHealth;
        public int CriticalAlerts;
        public int WarningAlerts;
        public float SystemUptime;
        public List<string> SystemIssues;
    }

    [System.Serializable]
    public class EnvironmentalProfile
    {
        public string Name;
        public Dictionary<string, ClimateProfile> ZoneProfiles;
        public bool EnableGlobalOptimization;
        public float EnergyEfficiencyTarget;
        public float PlantStressTarget;
    }
}