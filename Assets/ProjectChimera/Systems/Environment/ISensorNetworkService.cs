using System.Collections.Generic;
// using ProjectChimera.Data.Automation; // Commented out - namespace removed

namespace ProjectChimera.Systems.Environment
{
    /// <summary>
    /// PC014-2b: Interface for sensor network management service
    /// </summary>
    public interface ISensorNetworkService : IEnvironmentalService
    {
        void RegisterSensor(string sensorId, SensorType type, string zoneId);
        SensorReading GetLatestReading(string sensorId);
        IEnumerable<SensorReading> GetHistoricalData(string sensorId, int hours);
        void CalibrateSensor(string sensorId, float calibrationOffset);
        void SetAlertThresholds(string sensorId, float minValue, float maxValue);
    }

    // Note: Sensor types moved to SensorNetworkManager.cs to avoid duplicates
}