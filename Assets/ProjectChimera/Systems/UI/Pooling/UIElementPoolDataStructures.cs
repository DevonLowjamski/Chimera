// REFACTORED: UI Element Pool Data Structures
// Extracted from UIElementPool for better separation of concerns

using System;
using UnityEngine;

namespace ProjectChimera.Systems.UI.Pooling
{
    /// <summary>
    /// UI pool configuration
    /// </summary>
    public struct UIPoolConfig
    {
        public UIElementPool.UIElementType elementType;
        public GameObject prefab;
        public int initialSize;
        public int maxSize;
        public bool expandable;
    }

    /// <summary>
    /// Statistics for UI pool
    /// </summary>
    [Serializable]
    public struct UIPoolStats
    {
        public int TotalGets;
        public int TotalReturns;
        public float AverageGetTime;
        public int CountInactive;
        public int CountActive;
        public int CountAll;
    }
}

