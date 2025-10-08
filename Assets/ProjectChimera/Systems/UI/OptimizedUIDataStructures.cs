// REFACTORED: Optimized UI Data Structures
// Extracted from OptimizedUIManager for better separation of concerns

using System;
using UnityEngine;

namespace ProjectChimera.Systems.UI
{
    /// <summary>
    /// UI update request with priority and targeting
    /// </summary>
    [Serializable]
    public struct UIUpdateRequest
    {
        public UIUpdateType Type;
        public object Data;
        public float Priority;

        // Additional properties for UI performance optimizer
        public GameObject Target;
        public Vector3 Position;
        public bool IsVisible;
    }

    /// <summary>
    /// UI update types for optimized batching
    /// </summary>
    public enum UIUpdateType
    {
        PlantInfo,
        Progress,
        Notification,
        Canvas,
        Animation,
        Transform,
        Visibility,
        Content
    }

    /// <summary>
    /// Plant info data structure for UI display
    /// </summary>
    [Serializable]
    public struct PlantInfoData
    {
        public string PlantId;
        public string PlantName;
        public float Health;
        public float Growth;
    }

    /// <summary>
    /// Progress data structure for UI progress bars
    /// </summary>
    [Serializable]
    public struct ProgressData
    {
        public string Id;
        public float Value;
        public float MaxValue;
    }
}

