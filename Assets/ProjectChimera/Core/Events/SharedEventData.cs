using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectChimera.Core.Events
{
    /// <summary>
    /// Shared event data structures that can be used across Data and Systems assemblies
    /// without creating circular dependencies. Uses generic types to avoid enum dependencies.
    /// </summary>

    /// <summary>
    /// Event data for automation unlock events
    /// </summary>
    [System.Serializable]
    public class AutomationUnlockEventData
    {
        public string PlayerId;
        public string TaskType; // Changed to string to avoid enum dependency
        public string SystemType; // Changed to string to avoid enum dependency
        public string NewLevel; // Changed to string to avoid enum dependency
        public float UnlockTimestamp;
        public string Result; // Changed to string to avoid enum dependency
        public float UnlockCosts;
        public float Timestamp;
        public List<string> UnlockedSystems = new List<string>();
    }

    /// <summary>
    /// Event data for plant care actions in the cultivation system
    /// </summary>
    [System.Serializable]
    public class PlantCareEventData
    {
        public string PlantId;
        public string CareActionString; // Original string version
        public float EffectivenessScore;
        public float Timestamp;
        
        // Additional properties for InteractivePlantCareSystem compatibility
        public object PlantInstance; // Generic object to avoid type dependencies
        public object CareAction; // Object version expected by InteractivePlantCareSystem
        public string CareQuality; // Changed to string to avoid enum dependency
        public object CareEffects;
        public float PlayerSkillLevel;
        public string TaskType; // Changed to string to avoid enum dependency
        public string CareType; // Added CareType property for compatibility
        public int PlantId_Int; // For numeric plant ID compatibility
        
        // Compatibility properties to avoid breaking existing code
        public object InteractivePlant
        {
            get => PlantInstance;
            set => PlantInstance = value;
        }
    }
}