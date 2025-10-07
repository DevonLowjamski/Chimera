
using UnityEngine;
namespace ProjectChimera.Data.Environment
{
    // Minimal shared environmental types used by legacy cultivation systems
    [System.Serializable]
    public struct EnvironmentalConditions
    {
        public float Temperature;
        public float Humidity;
        public float CO2Level;
        public float LightIntensity;
        public float AirFlow;
    }
}
