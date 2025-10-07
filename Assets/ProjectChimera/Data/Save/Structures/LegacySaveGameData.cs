using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Data.Save.Structures;

namespace ProjectChimera.Data.Save
{
    public class LegacySaveGameData
    {
        [Header("Legacy Save Meta Information")]
        public string SlotName;
        public string Description;
        public DateTime SaveTimestamp;
        public string GameVersion;
        public string SaveSystemVersion;
        public TimeSpan PlayTime;

        [Header("Legacy Game Data - Deprecated")]
        public PlayerSaveData PlayerData;
        public CultivationSaveData CultivationData;
        public EconomySaveData EconomyData;
        public EnvironmentSaveData EnvironmentData;
        public ProgressionSaveData ProgressionData;
        public ObjectiveSaveData ObjectiveData;
        public EventSaveData EventData;
        public GameSettingsSaveData SettingsData;
    }

    // Note: Legacy save data classes are defined in ContractsDTO.cs
    // PlayerSaveData, CultivationSaveData, PlantSaveData, EconomySaveData, and TransactionSaveData
    // are available from the main namespace and should be used instead of duplicating here
}
