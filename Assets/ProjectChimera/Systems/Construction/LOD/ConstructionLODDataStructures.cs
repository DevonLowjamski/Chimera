// REFACTORED: Construction LOD Data Structures
// Extracted from ConstructionLODRendererController for better separation of concerns

using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectChimera.Systems.Construction.LOD
{
    /// <summary>
    /// LOD rendering configuration
    /// </summary>
    public struct LODRenderConfiguration
    {
        public float RendererRatio;      // Fraction of renderers to keep enabled
        public float ColliderRatio;      // Fraction of colliders to keep enabled
        public bool EnableShadowCasting; // Whether to enable shadow casting
        public bool EnableParticles;     // Whether to enable particle systems
        public int RendererCullingMask;  // Layer mask for culling
    }

    /// <summary>
    /// Construction object components
    /// </summary>
    [Serializable]
    public struct ConstructionObjectComponents
    {
        public string ObjectId;
        public GameObject GameObject;
        public Renderer[] Renderers;
        public Collider[] Colliders;
        public ParticleSystem[] ParticleSystems;
        public bool[] OriginalRendererStates;
        public ShadowCastingMode[] OriginalShadowModes;
        public bool[] OriginalColliderStates;
    }

    /// <summary>
    /// Renderer controller statistics
    /// </summary>
    [Serializable]
    public struct RendererControllerStats
    {
        public int RegisteredObjects;
        public int LODApplications;
        public int RendererStateChanges;
        public int ColliderStateChanges;
        public int ParticleStateChanges;
    }
}

