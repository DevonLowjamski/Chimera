// REFACTORED: UI Event Handler Data Structures
// Extracted from UIEventHandler for better separation of concerns

using System;
using UnityEngine;

namespace ProjectChimera.Systems.UI.Events
{
    /// <summary>
    /// UI event types
    /// </summary>
    public enum UIEventType
    {
        Click,
        Hover,
        Focus,
        KeyPress,
        Custom
    }

    /// <summary>
    /// UI event data structure
    /// </summary>
    [Serializable]
    public struct UIEvent
    {
        public UIEventType Type;
        public GameObject Source;
        public object Data;
        public float Timestamp;
    }

    /// <summary>
    /// UI interaction tracking data
    /// </summary>
    [Serializable]
    public class UIInteractionData
    {
        public float LastInteractionTime;
        public int InteractionCount;
    }

    /// <summary>
    /// Event processing statistics
    /// </summary>
    [Serializable]
    public struct EventProcessingStats
    {
        public int QueuedEvents;
        public int ProcessedEventsThisFrame;
        public float LastProcessingTime;
        public int RegisteredShortcuts;
        public int RegisteredEvents;
        public int CachedInteractions;
    }
}

