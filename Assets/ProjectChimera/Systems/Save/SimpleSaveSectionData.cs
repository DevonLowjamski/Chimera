// REFACTORED: Simple Save Section Data
// Extracted from SimpleSaveProvider for better separation of concerns

using System;

namespace ProjectChimera.Systems.Save
{
    /// <summary>
    /// Simple save section data implementation
    /// </summary>
    public class SimpleSaveSectionData : ISaveSectionData
    {
        public object Data;

        public string SectionKey { get; set; } = "simple_save";
        public string DataVersion { get; set; } = "1.0.0";
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public long EstimatedSize => 1024; // Estimate 1KB
        public string DataHash { get; set; } = "";

        public bool IsValid()
        {
            return Data != null && !string.IsNullOrEmpty(SectionKey);
        }

        public string GetSummary()
        {
            return $"SimpleSave data created at {Timestamp}";
        }
    }
}

