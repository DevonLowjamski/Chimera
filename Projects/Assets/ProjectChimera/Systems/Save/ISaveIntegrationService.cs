using UnityEngine;
using ProjectChimera.Data.Save;
using System.Threading.Tasks;

namespace ProjectChimera.Systems.Save
{
    /// <summary>
    /// Interface for systems that integrate with the save/load system
    /// Provides contracts for gathering system state and applying loaded data
    /// </summary>
    public interface ISaveIntegrationService
    {
        /// <summary>
        /// System name for logging and identification
        /// </summary>
        string SystemName { get; }

        /// <summary>
        /// Whether this system is currently active and available for save/load operations
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Whether this system supports offline progression
        /// </summary>
        bool SupportsOfflineProgression { get; }
    }

    /// <summary>
    /// Interface for cultivation system save/load integration
    /// </summary>
    public interface ICultivationSaveService : ISaveIntegrationService
    {
        /// <summary>
        /// Gather current cultivation system state for saving
        /// </summary>
        /// <returns>DTO containing cultivation system state</returns>
        CultivationStateDTO GatherCultivationState();

        /// <summary>
        /// Apply loaded cultivation state to the system
        /// </summary>
        /// <param name="cultivationData">State data to apply</param>
        /// <returns>Task representing the async operation</returns>
        Task ApplyCultivationState(CultivationStateDTO cultivationData);

        /// <summary>
        /// Process offline progression for cultivation systems
        /// </summary>
        /// <param name="offlineHours">Hours the player was offline</param>
        /// <returns>Results of offline progression</returns>
        OfflineProgressionResult ProcessOfflineProgression(float offlineHours);
    }

    /// <summary>
    /// Interface for economy system save/load integration
    /// </summary>
    public interface IEconomySaveService : ISaveIntegrationService
    {
        /// <summary>
        /// Gather current economy system state for saving
        /// </summary>
        /// <returns>DTO containing economy system state</returns>
        EconomyStateDTO GatherEconomyState();

        /// <summary>
        /// Apply loaded economy state to the system
        /// </summary>
        /// <param name="economyData">State data to apply</param>
        /// <returns>Task representing the async operation</returns>
        Task ApplyEconomyState(EconomyStateDTO economyData);

        /// <summary>
        /// Process offline progression for economy systems
        /// </summary>
        /// <param name="offlineHours">Hours the player was offline</param>
        /// <returns>Results of offline progression</returns>
        OfflineProgressionResult ProcessOfflineProgression(float offlineHours);
    }

    /// <summary>
    /// Interface for facility system save/load integration
    /// </summary>
    public interface IFacilitySaveService : ISaveIntegrationService
    {
        /// <summary>
        /// Gather current facility system state for saving
        /// </summary>
        /// <returns>DTO containing facility system state</returns>
        FacilityStateDTO GatherFacilityState();

        /// <summary>
        /// Apply loaded facility state to the system
        /// </summary>
        /// <param name="facilityData">State data to apply</param>
        /// <returns>Task representing the async operation</returns>
        Task ApplyFacilityState(FacilityStateDTO facilityData);

        /// <summary>
        /// Process offline progression for facility systems
        /// </summary>
        /// <param name="offlineHours">Hours the player was offline</param>
        /// <returns>Results of offline progression</returns>
        OfflineProgressionResult ProcessOfflineProgression(float offlineHours);
    }

    /// <summary>
    /// Interface for construction system save/load integration
    /// </summary>
    public interface IConstructionSaveService : ISaveIntegrationService
    {
        /// <summary>
        /// Gather current construction system state for saving
        /// </summary>
        /// <returns>DTO containing construction system state</returns>
        ConstructionStateDTO GatherConstructionState();

        /// <summary>
        /// Apply loaded construction state to the system
        /// </summary>
        /// <param name="constructionData">State data to apply</param>
        /// <returns>Task representing the async operation</returns>
        Task ApplyConstructionState(ConstructionStateDTO constructionData);

        /// <summary>
        /// Process offline progression for construction systems
        /// </summary>
        /// <param name="offlineHours">Hours the player was offline</param>
        /// <returns>Results of offline progression</returns>
        OfflineProgressionResult ProcessOfflineProgression(float offlineHours);
    }

    /// <summary>
    /// Interface for progression system save/load integration
    /// </summary>
    public interface IProgressionSaveService : ISaveIntegrationService
    {
        /// <summary>
        /// Gather current progression system state for saving
        /// </summary>
        /// <returns>DTO containing progression system state</returns>
        ProgressionStateDTO GatherProgressionState();

        /// <summary>
        /// Apply loaded progression state to the system
        /// </summary>
        /// <param name="progressionData">State data to apply</param>
        /// <returns>Task representing the async operation</returns>
        Task ApplyProgressionState(ProgressionStateDTO progressionData);

        /// <summary>
        /// Process offline progression for progression systems
        /// </summary>
        /// <param name="offlineHours">Hours the player was offline</param>
        /// <returns>Results of offline progression</returns>
        OfflineProgressionResult ProcessOfflineProgression(float offlineHours);
    }

    /// <summary>
    /// Interface for UI system save/load integration
    /// </summary>
    public interface IUISaveService : ISaveIntegrationService
    {
        /// <summary>
        /// Gather current UI system state for saving
        /// </summary>
        /// <returns>DTO containing UI system state</returns>
        UIStateDTO GatherUIState();

        /// <summary>
        /// Apply loaded UI state to the system
        /// </summary>
        /// <param name="uiData">State data to apply</param>
        /// <returns>Task representing the async operation</returns>
        Task ApplyUIState(UIStateDTO uiData);
    }

    /// <summary>
    /// Interface for contract system save/load integration
    /// </summary>
    public interface IContractSaveService : ISaveIntegrationService
    {
        /// <summary>
        /// Gather current contract system state for saving
        /// </summary>
        /// <returns>DTO containing contract system state</returns>
        ContractsStateDTO GatherContractState();

        /// <summary>
        /// Apply loaded contract state to the system
        /// </summary>
        /// <param name="contractData">State data to apply</param>
        /// <returns>Task representing the async operation</returns>
        Task ApplyContractState(ContractsStateDTO contractData);

        /// <summary>
        /// Process offline progression for contract systems
        /// </summary>
        /// <param name="offlineHours">Hours the player was offline</param>
        /// <returns>Results of offline progression</returns>
        OfflineProgressionResult ProcessOfflineProgression(float offlineHours);
    }
}