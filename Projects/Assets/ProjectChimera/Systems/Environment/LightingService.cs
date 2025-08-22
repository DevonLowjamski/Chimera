using LightSpectrumData = ProjectChimera.Data.Shared.LightSpectrumData;
using ProjectChimera.Data.Simulation;

namespace ProjectChimera.Systems.Environment
{
    // Minimal interface for lighting interactions referenced elsewhere (renamed to avoid duplicate with existing ILightingService)
    public interface ILightingControlService
    {
        void SetSpectrumProfile(string zoneId, LightSpectrumData spectrum);
        void SetPhotoperiod(string zoneId, float hours);
        void SetTargetPPFD(string zoneId, float ppfd);
    }
}

