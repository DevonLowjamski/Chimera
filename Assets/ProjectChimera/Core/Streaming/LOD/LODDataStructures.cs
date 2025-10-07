using UnityEngine;
using System.Collections.Generic;

namespace ProjectChimera.Core.Streaming.LOD
{
    /// <summary>
    /// REFACTORED: Shared LOD Data Structures
    /// Common data structures used across LOD system components
    /// </summary>

    /// <summary>
    /// LOD object data structure
    /// </summary>
    [System.Serializable]
    public struct LODObject
    {
        public int ObjectId;
        public GameObject GameObject;
        public Transform Transform;
        public LODObjectType ObjectType;
        public float CustomBias;
        public int CurrentLODLevel;
        public float LastUpdateTime;
        public bool IsVisible;
        public float DistanceFromCenter;
        public LODComponentCache OriginalComponents;
    }

    /// <summary>
    /// LOD update request
    /// </summary>
    [System.Serializable]
    public struct LODUpdateRequest
    {
        public int ObjectId;
        public int NewLODLevel;
        public float RequestTime;
    }

    /// <summary>
    /// LOD object types for specialized handling
    /// </summary>
    public enum LODObjectType
    {
        Standard,
        Plant,
        Building,
        Equipment,
        UI,
        Effect
    }

    /// <summary>
    /// Adaptive LOD settings
    /// </summary>
    [System.Serializable]
    public struct AdaptiveLODSettings
    {
        public float TargetFrameTime;
        public float AdaptationSpeed;
        public float MinLODMultiplier;
        public float MaxLODMultiplier;
    }
}