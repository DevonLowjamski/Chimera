namespace ProjectChimera.Systems.Environment
{
    /// <summary>
    /// PC014-2b: Interface for HVAC system management service
    /// </summary>
    public interface IHVACService : IEnvironmentalService
    {
        void SetTargetTemperature(string zoneId, float temperature);
        void SetTargetHumidity(string zoneId, float humidity);
        void SetAirflowRate(string zoneId, float airflowCFM);
        float CalculateVPD(string zoneId);
        void EnableAutomaticControl(string zoneId, bool enabled);
    }
}