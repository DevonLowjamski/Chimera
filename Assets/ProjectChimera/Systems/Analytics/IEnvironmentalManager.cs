using ProjectChimera.Data.Environment;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Placeholder interface for environmental management
    /// </summary>
    public interface IEnvironmentalManager
    {
        // Placeholder methods - to be implemented based on actual requirements
        bool IsInitialized { get; }
        void Initialize();
        void Shutdown();
        ZoneEnvironmentDTO GetZoneEnvironment(string zoneName);
        float GetAverageTemperature();
        float GetAverageHumidity();
    }
}
