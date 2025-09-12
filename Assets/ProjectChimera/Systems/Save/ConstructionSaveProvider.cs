using ProjectChimera.Core.Logging;
using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Data.Save;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Save
{
    /// <summary>
    /// SIMPLE: Basic construction save provider aligned with Project Chimera's save system vision.
    /// Focuses on essential construction data saving and loading without over-engineering.
    /// </summary>
    public class ConstructionSaveProvider : MonoBehaviour, ISaveSectionProvider
    {
        [Header("Basic Settings")]
        [SerializeField] private string _sectionVersion = "1.0.0";
        [SerializeField] private bool _enableSaving = true;

        // System references
        private IConstructionSystem _constructionSystem;

        // Basic state
        private ConstructionStateDTO _lastSavedState;
        private bool _isInitialized = false;

        #region ISaveSectionProvider Implementation

        public string SectionKey => SaveSectionKeys.CONSTRUCTION;
        public string SectionName => "Construction System";
        public string SectionVersion => _sectionVersion;
        public int Priority => 1;
        public bool IsRequired => false;
        public bool SupportsIncrementalSave => false;
        public long EstimatedDataSize => 0;
        public IReadOnlyList<string> Dependencies => new string[0];

        public System.Threading.Tasks.Task<ISaveSectionData> GatherSectionDataAsync()
        {
            if (!_enableSaving)
                return System.Threading.Tasks.Task.FromResult<ISaveSectionData>(null);

            InitializeSystemsIfNeeded();

            var constructionData = new ConstructionSectionData
            {
                SectionKey = SectionKey,
                DataVersion = SectionVersion,
                Timestamp = DateTime.Now,
                ConstructionState = GatherConstructionState()
            };

            _lastSavedState = constructionData.ConstructionState;

            LogInfo($"Construction data gathered: {constructionData.ConstructionState?.PlacedObjects?.Count ?? 0} objects");

            return System.Threading.Tasks.Task.FromResult<ISaveSectionData>(constructionData);
        }

        public System.Threading.Tasks.Task RestoreSectionDataAsync(ISaveSectionData sectionData)
        {
            if (!_enableSaving || sectionData == null)
                return System.Threading.Tasks.Task.CompletedTask;

            var constructionData = sectionData as ConstructionSectionData;
            if (constructionData?.ConstructionState != null)
            {
                RestoreConstructionState(constructionData.ConstructionState);
                LogInfo("Construction data restored");
            }

            return System.Threading.Tasks.Task.CompletedTask;
        }

        #endregion

        #region Private Methods

        private void InitializeSystemsIfNeeded()
        {
            if (_isInitialized) return;

            // Find construction system
            _constructionSystem = FindObjectOfType<MonoBehaviour>() as IConstructionSystem;

            _isInitialized = true;
        }

        private ConstructionStateDTO GatherConstructionState()
        {
            if (_constructionSystem == null)
                return new ConstructionStateDTO();

            // Simple gathering of construction state
            return _constructionSystem.GetConstructionState();
        }

        private void RestoreConstructionState(ConstructionStateDTO state)
        {
            if (_constructionSystem == null || state == null)
                return;

            // Simple restoration of construction state
            _constructionSystem.RestoreConstructionState(state);
        }

        #endregion
    }
}
