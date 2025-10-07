using UnityEngine;
using ProjectChimera.Core.Logging;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Services.SpeedTree
{
    /// <summary>
    /// Simple Plant Rendering Service - Aligned with Project Chimera's vision
    /// Provides basic plant visualization with LOD for performance
    /// Focuses on immersion through visual quality rather than complex optimization
    /// </summary>
    public class SimplePlantRenderingService : MonoBehaviour
    {
        [Header("Basic Rendering Settings")]
        [SerializeField] private float _renderDistance = 50f;
        [SerializeField] private int _maxVisiblePlants = 100;
        [SerializeField] private bool _enableLOD = true;

        [Header("LOD Settings")]
        [SerializeField] private float _highDetailDistance = 20f;
        [SerializeField] private float _mediumDetailDistance = 35f;
        [SerializeField] private float _lowDetailDistance = 50f;

        private List<GameObject> _visiblePlants = new List<GameObject>();
        private UnityEngine.Camera _mainCamera;

        private void Awake()
        {
            _mainCamera = UnityEngine.Camera.main;
            InitializeRendering();
        }

        private void InitializeRendering()
        {
            // Simple LOD setup based on gameplay document requirements
            if (_enableLOD)
            {
                SetupBasicLOD();
            }

            ProjectChimera.Core.Logging.ChimeraLogger.Log("SPEEDTREE/RENDER", "Initialized simple plant rendering service", this);
        }

        private void SetupBasicLOD()
        {
            // Simple distance-based LOD as mentioned in gameplay document
            // High detail: Close to camera (immersive experience)
            // Medium detail: Medium distance (balanced quality)
            // Low detail: Far distance (performance optimization)

            QualitySettings.lodBias = 1.0f; // Standard LOD quality
        }

        /// <summary>
        /// Updates visible plants based on camera position
        /// </summary>
        public void UpdateVisiblePlants(List<GameObject> allPlants)
        {
            if (_mainCamera == null || allPlants == null)
                return;

            _visiblePlants.Clear();
            Vector3 cameraPosition = _mainCamera.transform.position;

            foreach (var plant in allPlants)
            {
                if (plant == null) continue;

                float distance = Vector3.Distance(cameraPosition, plant.transform.position);

                if (distance <= _renderDistance)
                {
                    plant.SetActive(true);
                    _visiblePlants.Add(plant);

                    // Apply simple LOD based on distance
                    if (_enableLOD)
                    {
                        ApplySimpleLOD(plant, distance);
                    }

                    // Limit visible plants for performance
                    if (_visiblePlants.Count >= _maxVisiblePlants)
                        break;
                }
                else
                {
                    plant.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Applies simple LOD based on distance from camera
        /// </summary>
        private void ApplySimpleLOD(GameObject plant, float distance)
        {
            var lodGroup = plant.GetComponent<LODGroup>();
            if (lodGroup == null) return;

            // Simple distance-based LOD switching
            if (distance <= _highDetailDistance)
            {
                // High detail - full quality for immersion
                lodGroup.ForceLOD(0);
            }
            else if (distance <= _mediumDetailDistance)
            {
                // Medium detail - balanced quality
                lodGroup.ForceLOD(1);
            }
            else if (distance <= _lowDetailDistance)
            {
                // Low detail - reduced quality for performance
                lodGroup.ForceLOD(2);
            }
            else
            {
                // Very low detail or cull
                lodGroup.ForceLOD(-1); // Cull completely
            }
        }

        /// <summary>
        /// Gets current rendering statistics
        /// </summary>
        public PlantRenderingStats GetRenderingStats()
        {
            return new PlantRenderingStats
            {
                VisiblePlants = _visiblePlants.Count,
                RenderDistance = _renderDistance,
                MaxVisiblePlants = _maxVisiblePlants,
                LODEnabled = _enableLOD,
                LastUpdate = Time.time
            };
        }

        /// <summary>
        /// Adjusts render quality for performance
        /// </summary>
        public void AdjustQualityForPerformance(float performanceFactor)
        {
            if (performanceFactor < 0.5f)
            {
                // Low performance - reduce quality
                _renderDistance *= 0.8f;
                _maxVisiblePlants = Mathf.FloorToInt(_maxVisiblePlants * 0.7f);
                ProjectChimera.Core.Logging.ChimeraLogger.Log("SPEEDTREE/RENDER", "Reduced max visible plants for performance", this);
            }
            else if (performanceFactor > 0.8f)
            {
                // Good performance - increase quality slightly
                _renderDistance *= 1.1f;
                _maxVisiblePlants = Mathf.FloorToInt(_maxVisiblePlants * 1.2f);
                ProjectChimera.Core.Logging.ChimeraLogger.Log("SPEEDTREE/RENDER", "Increased max visible plants", this);
            }
        }

        /// <summary>
        /// Highlights a specific plant for player attention
        /// </summary>
        public void HighlightPlant(GameObject plant)
        {
            if (plant == null) return;

            // Simple highlight effect using emission
            var renderer = plant.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", Color.yellow * 0.3f);
            }
        }

        /// <summary>
        /// Removes highlight from a plant
        /// </summary>
        public void RemoveHighlight(GameObject plant)
        {
            if (plant == null) return;

            var renderer = plant.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.DisableKeyword("_EMISSION");
            }
        }
    }

    /// <summary>
    /// Simple rendering statistics
    /// </summary>
    public struct PlantRenderingStats
    {
        public int VisiblePlants;
        public float RenderDistance;
        public int MaxVisiblePlants;
        public bool LODEnabled;
        public float LastUpdate;
    }
}
