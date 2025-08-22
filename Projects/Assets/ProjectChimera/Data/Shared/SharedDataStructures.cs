using UnityEngine;

namespace ProjectChimera.Data.Shared
{
    [System.Serializable]
    public struct EnvironmentalConditions
    {
        public float Temperature;
        public float Humidity;
        public float CO2Level;
        public float LightIntensity;
        public float AirFlow;
        public float AirVelocity;
        public LightSpectrumData LightSpectrumData;
        // Added fields referenced across systems
        public float DailyLightIntegral;
        public float AirflowRate;
        public float VaporPressureDeficit;
        public float BarometricPressure;
        public float AirQualityIndex;
        public System.DateTime LastMeasurement;

        // Optional fields used by some systems
        public float PhotoperiodHours;
        public float pH;
        public float WaterAvailability;
        public float ElectricalConductivity;

        public static EnvironmentalConditions CreateIndoorDefault()
        {
            return new EnvironmentalConditions
            {
                Temperature = 23f,
                Humidity = 55f,
                CO2Level = 800f,
                LightIntensity = 600f,
                AirFlow = 0.2f,
                AirVelocity = 0.3f,
                PhotoperiodHours = 18f,
                pH = 6.0f,
                LightSpectrumData = new LightSpectrumData()
            };
        }

        public float CalculateOverallSuitability()
        {
            // Simple heuristic suitability score 0..1
            float temp = Mathf.Clamp01(1f - Mathf.Abs(23f - Temperature) / 20f);
            float hum = Mathf.Clamp01(1f - Mathf.Abs(55f - Humidity) / 45f);
            float light = Mathf.Clamp01(LightIntensity / 1200f);
            float co2 = Mathf.Clamp01(CO2Level / 2000f);
            float phScore = Mathf.Clamp01(1f - Mathf.Abs(6f - pH) / 3f);
            return (temp * 0.25f + hum * 0.2f + light * 0.25f + co2 * 0.15f + phScore * 0.15f);
        }

        public float CalculateEnvironmentalStress()
        {
            // Inverse of suitability as a simple stress proxy
            return 1f - CalculateOverallSuitability();
        }

        public bool IsInitialized()
        {
            // Consider conditions initialized if any key field is non-zero/non-default
            return Temperature != 0f || Humidity != 0f || CO2Level != 0f || LightIntensity != 0f || PhotoperiodHours != 0f || pH != 0f;
        }

        public float GetValue(EnvironmentalFactor factor)
        {
            switch (factor)
            {
                case EnvironmentalFactor.Temperature:
                    return Temperature;
                case EnvironmentalFactor.Light:
                    return LightIntensity;
                case EnvironmentalFactor.Humidity:
                    return Humidity;
                case EnvironmentalFactor.CO2:
                    return CO2Level;
                default:
                    return 0f;
            }
        }

        // Minimal helpers referenced by systems
        public void UpdateVPD()
        {
            // Simple VPD based on current temperature and humidity
            float svp = 0.6108f * Mathf.Exp((17.27f * Temperature) / (Temperature + 237.3f));
            float avp = svp * (Humidity / 100f);
            VaporPressureDeficit = Mathf.Max(0f, svp - avp);
        }

        public float GetEnvironmentalQuality(object parameters = null)
        {
            // Fallback quality evaluation independent of Simulation assembly
            return CalculateOverallSuitability();
        }

        public CannabinoidProductionPotential PredictCannabinoidProduction()
        {
            // Heuristic prediction for THC, terpenes, trichomes
            float quality = CalculateOverallSuitability();
            return new CannabinoidProductionPotential
            {
                THCPotential = quality,
                CBDPotential = quality * 0.85f,
                TerpenePotential = quality * 0.9f,
                TrichomePotential = quality * 0.95f,
                OverallQuality = quality
            };
        }
    }

    [System.Serializable]
    public struct CannabinoidProductionPotential
    {
        public float THCPotential;
        public float CBDPotential;
        public float TerpenePotential;
        public float TrichomePotential;
        public float OverallQuality;
    }
}
