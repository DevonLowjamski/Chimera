using System;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Cultivation.IPM
{
    /// <summary>
    /// Interface for Active IPM system.
    /// Allows for dependency injection and testing.
    /// </summary>
    public interface IActiveIPMSystem
    {
        // Events
        event Action<string, PestType> OnInfestationDetected;
        event Action<string, PestType> OnInfestationSpread;
        event Action<string, PestType> OnInfestationCleared;
        event Action<string> OnTreatmentApplied;
        event Action<string, BeneficialType> OnBeneficialReleased;

        // Pest management
        bool InfestPlant(string plantId, PestType pestType, float initialSeverity = 0.1f);
        bool ApplyTreatment(string plantId, TreatmentType treatmentType);
        bool ReleaseBeneficialOrganisms(string zoneId, BeneficialType beneficialType, float initialPopulation = 1.0f);

        // Query methods
        PlantInfestation GetInfestation(string plantId);
        bool IsPlantInfested(string plantId);
        List<PlantInfestation> GetAllInfestations();
        IPMStats GetStatistics();
    }
}
