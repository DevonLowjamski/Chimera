using LightSpectrumData = ProjectChimera.Data.Shared.LightSpectrumData;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Systems.Environment
{
    /// <summary>
    /// Lighting conditions data structure for environmental systems
    /// </summary>
    [System.Serializable]
    public class LightingConditions
    {
        public float LightIntensity = 600f; // PPFD
        public float DailyLightIntegral = 30f; // DLI
        public float PhotoperiodHours = 18f;
        public LightSpectrumData SpectrumData = new LightSpectrumData();
        public bool IsActive = true;
    }

    /// <summary>
    /// PC014-2b: Interface for lighting system management service
    /// </summary>
    public interface ILightingService : IEnvironmentalService
    {
        void SetLightingConditions(string zoneId, LightingConditions conditions);
        LightingConditions GetLightingConditions(string zoneId);
        void UpdatePhotoperiod(string zoneId, float photoperiodHours);
        float CalculateDLI(string zoneId);
        void SetSpectrumProfile(string zoneId, LightSpectrumData spectrum);
    }
}