// REFACTORED: Plant Instanced Renderer Data Structures
// Extracted from PlantInstancedRenderer for better separation of concerns

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectChimera.Systems.Rendering
{
    /// <summary>
    /// Plant instance rendering data
    /// </summary>
    public struct PlantInstanceData
    {
        public int InstanceID;
        public GameObject GameObject;
        public PlantRenderingData RenderData;
        public float LastUpdateTime;
        public bool IsVisible;
    }

    /// <summary>
    /// Plant rendering batch
    /// </summary>
    [Serializable]
    public struct PlantRenderBatch
    {
        public int LODLevel;
        public List<Matrix4x4> Matrices;
        public List<Vector4> InstanceData;
        public List<Vector4> InstanceColors;
    }

    /// <summary>
    /// Plant rendering statistics
    /// </summary>
    [Serializable]
    public struct PlantRenderingStats
    {
        public int RegisteredPlants;
        public int VisibleInstances;
        public int CulledInstances;
        public int DrawCalls;
        public int UpdateCalls;
        public float LastUpdateTime;
    }
}

