using UnityEngine;

namespace ProjectChimera.Data.Shared
{
    [System.Serializable]
    public struct LightSpectrumData
    {
        // Percent-style aliases used broadly in systems code
        public float UVPercent;
        public float BluePercent;
        public float GreenPercent;
        public float RedPercent;
        public float FarRedPercent;
        public float FarRed_700_850nm;
        public float Violet_400_420nm;
        public float UV_B_280_315nm;
        public float NearInfrared_700_800nm;
        public float Cannabis_Specific_285nm;
        public float Cannabis_Specific_365nm;
        public float Cannabis_Specific_385nm;
        public float Cannabis_Specific_660nm;
        public float Cannabis_Specific_730nm;
        public float PhotosyntheticEfficiency;
        public float RedToFarRedRatio;
        public float BlueToRedRatio;
        public float UVToVisibleRatio;
        public bool SpectrumStability;
        public float FlickerFrequency;
        public float ChromaticCoordinates_X;
        public float ChromaticCoordinates_Y;
        public float DailyPhotoperiod;
        public bool CircadianLighting;
        public float Blue_420_490nm;
        public float Red_630_660nm;
        public float DeepRed_660_700nm;
        public float UV_A_315_400nm;
        public float Green_490_550nm;
        public float Yellow_550_590nm;
        public float Orange_590_630nm;
        public float ColorTemperature;

        // Convenience helpers expected by systems
        public float GetTotalPAR()
        {
            // Sum photosynthetically active radiation approximations
            return Mathf.Max(0f,
                Blue_420_490nm + Green_490_550nm + Red_630_660nm + DeepRed_660_700nm + FarRed_700_850nm);
        }

        public SpectrumCannabinoidResponse GetCannabinoidResponse()
        {
            // Minimal heuristic response used by EnvironmentalManager
            return new SpectrumCannabinoidResponse
            {
                THCEnhancement = 1.0f + (Red_630_660nm + DeepRed_660_700nm) / 1000f,
                TrichomeEnhancement = 1.0f + (UV_A_315_400nm + Blue_420_490nm) / 1000f
            };
        }
    }

    [System.Serializable]
    public struct SpectrumCannabinoidResponse
    {
        public float THCEnhancement;
        public float TrichomeEnhancement;
    }
}
