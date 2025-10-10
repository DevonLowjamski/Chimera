using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectChimera.Data.Cultivation.Plant;

namespace ProjectChimera.Systems.Genetics.TissueCulture
{
    /// <summary>
    /// Interface for Tissue Culture system.
    /// Allows for dependency injection and testing.
    /// </summary>
    public interface ITissueCultureSystem
    {
        // Events
        event Action<string> OnCultureCreated;
        event Action<string> OnCulturePreserved;
        event Action<string> OnCultureReactivated;
        event Action<string> OnCultureMaintained;
        event Action<string> OnCultureContaminated;
        event Action<string, float, float> OnCultureHealthChanged;

        // Culture lifecycle
        Task<TissueCultureSample> CreateCultureAsync(PlantInstance sourcePlant);
        bool PreserveCulture(string sampleId);
        Task<bool> ReactivateCultureAsync(string sampleId);
        bool MaintainCulture(string sampleId);

        // Query methods
        TissueCultureSample GetCulture(string sampleId);
        List<TissueCultureSample> GetActiveCultures();
        List<TissueCultureSample> GetPreservedCultures();
        TissueCultureStats GetStatistics();
    }
}
