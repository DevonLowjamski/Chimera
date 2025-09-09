using UnityEngine;
using System;
using System.Collections.Generic;

namespace ProjectChimera.Data.Save
{
    /// <summary>
    /// UI save/load operations and validation data structures.
    /// Contains save result DTOs, load result DTOs, and validation structures for UI persistence.
    /// </summary>
    
    /// <summary>
    /// Result DTO for UI save operations
    /// </summary>
    [System.Serializable]
    public class UISaveResult
    {
        public bool Success;
        public DateTime SaveTime;
        public string ErrorMessage;
        public long DataSizeBytes;
        public TimeSpan SaveDuration;
        public int PanelsSaved;
        public int PreferencesSaved;
        public int LayoutsSaved;
        public string SaveVersion;
    }

    /// <summary>
    /// Result DTO for UI load operations
    /// </summary>
    [System.Serializable]
    public class UILoadResult
    {
        public bool Success;
        public DateTime LoadTime;
        public string ErrorMessage;
        public TimeSpan LoadDuration;
        public int PanelsLoaded;
        public int PreferencesLoaded;
        public int LayoutsLoaded;
        public bool RequiredMigration;
        public string LoadedVersion;
        public UIStateDTO UIState;
    }

    /// <summary>
    /// DTO for UI system validation
    /// </summary>
    [System.Serializable]
    public class UIValidationResult
    {
        public bool IsValid;
        public DateTime ValidationTime;
        public List<string> Errors = new List<string>();
        public List<string> Warnings = new List<string>();
        
        [Header("UI State Validation")]
        public bool UIModeValid;
        public bool UILevelValid;
        public bool PanelStatesValid;
        
        [Header("User Preferences Validation")]
        public bool PreferencesValid;
        public bool ThemeValid;
        public bool LocalizationValid;
        
        [Header("Layout Validation")]
        public bool WindowLayoutValid;
        public bool PanelLayoutValid;
        public bool NavigationValid;
        
        [Header("Data Integrity")]
        public int TotalPanels;
        public int ValidPanels;
        public int TotalLayouts;
        public int ValidLayouts;
        public float DataIntegrityScore;
    }
}