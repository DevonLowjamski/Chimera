using UnityEngine;
using ProjectChimera.Core.Logging;
using Logger = ProjectChimera.Core.Logging.ChimeraLogger;
using ProjectChimera.Core.Updates;
using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.Systems.Gameplay
{
    /// <summary>
    /// Genetic Visualization Manager - Handles trait overlays and heatmap visualizations
    /// Provides visual feedback for genetic traits as described in genetics mode
    /// Shows trait overlays on plants and heatmap visualizations for genetic analysis
    /// </summary>
    public class GeneticVisualizationManager : MonoBehaviour, ProjectChimera.Core.Updates.ITickable
    {
        [Header("Visualization Settings")]
        [SerializeField] private bool _enableTraitOverlays = true;
        [SerializeField] private bool _enableHeatmaps = true;
        [SerializeField] private float _updateInterval = 1f;

        // ITickable implementation (unified Core.Updates)
        public int TickPriority => ProjectChimera.Core.Updates.TickPriority.HUD + 10;
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        [Header("Trait Overlay Settings")]
        [SerializeField] private Color _highTraitColor = Color.green;
        [SerializeField] private Color _mediumTraitColor = Color.yellow;
        [SerializeField] private Color _lowTraitColor = Color.red;
        [SerializeField] private float _overlayHeight = 2f;

        [Header("Heatmap Settings")]
        [SerializeField] private Gradient _potencyGradient;
        [SerializeField] private Gradient _yieldGradient;
        [SerializeField] private Gradient _healthGradient;

        // Visualization tracking
        private Dictionary<GameObject, TraitOverlay> _traitOverlays = new Dictionary<GameObject, TraitOverlay>();
        private Dictionary<GameObject, HeatmapRenderer> _heatmapRenderers = new Dictionary<GameObject, HeatmapRenderer>();
        private float _updateTimer;

        private void Awake()
        {
            // Register with update system
            UpdateOrchestrator.Instance?.RegisterTickable(this);

            InitializeVisualizations();
        }

        private void OnDestroy()
        {
            // Unregister from update system
            UpdateOrchestrator.Instance?.UnregisterTickable(this);

            CleanupVisualizations();
        }

        /// <summary>
        /// Initializes the genetic visualization system
        /// </summary>
        private void InitializeVisualizations()
        {
            // Find all plants in the scene for genetic visualization
            var plants = GameObject.FindGameObjectsWithTag("Plant");

            if (plants?.Length > 0)
            {
                Logger.Log("GENETICS", "Plants found for genetic visualization", this);
            }
            else
            {
                Logger.Log("GENETICS", "No plants found for genetic visualization", this);
            }

            foreach (var plant in plants)
            {
                RegisterPlantForVisualization(plant);
            }

            Logger.Log("GENETICS", "Genetic visualizations initialized", this);
        }

        /// <summary>
        /// Updates genetic visualizations (called by UpdateOrchestrator)
        /// </summary>
        public void Tick(float deltaTime)
        {
            _updateTimer += deltaTime;

            if (_updateTimer >= _updateInterval)
            {
                _updateTimer = 0f;
                UpdateVisualizations();
            }
        }

        /// <summary>
        /// Updates all genetic visualizations
        /// </summary>
        private void UpdateVisualizations()
        {
            foreach (var kvp in _traitOverlays)
            {
                UpdateTraitOverlay(kvp.Key, kvp.Value);
            }

            foreach (var kvp in _heatmapRenderers)
            {
                UpdateHeatmapRenderer(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Registers a plant for genetic visualization
        /// </summary>
        public void RegisterPlantForVisualization(GameObject plant)
        {
            if (plant == null) return;

            if (_enableTraitOverlays)
            {
                CreateTraitOverlay(plant);
            }

            if (_enableHeatmaps)
            {
                CreateHeatmapRenderer(plant);
            }
        }

        /// <summary>
        /// Unregisters a plant from genetic visualization
        /// </summary>
        public void UnregisterPlantFromVisualization(GameObject plant)
        {
            if (plant == null) return;

            if (_traitOverlays.ContainsKey(plant))
            {
                // TraitOverlay is a data class, just remove from dictionary
                _traitOverlays.Remove(plant);
            }

            if (_heatmapRenderers.ContainsKey(plant))
            {
                // HeatmapRenderer is a data class, just remove from dictionary
                _heatmapRenderers.Remove(plant);
            }
        }

        /// <summary>
        /// Creates a trait overlay for a plant
        /// </summary>
        private void CreateTraitOverlay(GameObject plant)
        {
            var overlay = new TraitOverlay();
            // TraitOverlay is a data class, not a component
            _traitOverlays[plant] = overlay;
        }

        /// <summary>
        /// Creates a heatmap renderer for a plant
        /// </summary>
        private void CreateHeatmapRenderer(GameObject plant)
        {
            var heatmapRenderer = new HeatmapRenderer();
            // HeatmapRenderer is a data class, not a component
            _heatmapRenderers[plant] = heatmapRenderer;
        }

        /// <summary>
        /// Updates a trait overlay for a plant
        /// </summary>
        private void UpdateTraitOverlay(GameObject plant, TraitOverlay overlay)
        {
            if (overlay == null) return;

            // Get plant's genetic traits (would integrate with actual genetics system)
            var traits = GetPlantGeneticTraits(plant);

            // Update overlay with trait information (TraitOverlay is a data class)
            // The actual visualization would be handled by a rendering system
        }

        /// <summary>
        /// Updates a heatmap renderer for a plant
        /// </summary>
        private void UpdateHeatmapRenderer(GameObject plant, HeatmapRenderer renderer)
        {
            if (renderer == null) return;

            // Get plant's genetic data for heatmap
            var potency = GetPlantPotency(plant);
            var yield = GetPlantYield(plant);
            var health = GetPlantHealth(plant);

            // Update heatmap visualization
            // Update heatmap data (HeatmapRenderer is a data class)
            // The actual rendering would be handled by a separate system
        }

        /// <summary>
        /// Gets genetic traits for a plant (placeholder - would integrate with genetics system)
        /// </summary>
        private GeneticTraits GetPlantGeneticTraits(GameObject plant)
        {
            // This would integrate with the actual genetics system
            // For now, return placeholder data
            return new GeneticTraits
            {
                THCContent = Random.Range(15f, 25f),
                CBDContent = Random.Range(0.5f, 2f),
                Yield = Random.Range(400f, 600f),
                FloweringTime = Random.Range(8, 10),
                Height = Random.Range(100f, 180f)
            };
        }

        /// <summary>
        /// Gets plant potency value (placeholder)
        /// </summary>
        private float GetPlantPotency(GameObject plant)
        {
            // This would calculate actual potency from genetics
            return Random.Range(0f, 1f);
        }

        /// <summary>
        /// Gets plant yield value (placeholder)
        /// </summary>
        private float GetPlantYield(GameObject plant)
        {
            // This would calculate actual yield from genetics
            return Random.Range(0f, 1f);
        }

        /// <summary>
        /// Gets plant health value (placeholder)
        /// </summary>
        private float GetPlantHealth(GameObject plant)
        {
            // This would get actual health from plant system
            return Random.Range(0.5f, 1f);
        }

        /// <summary>
        /// Enables or disables trait overlays
        /// </summary>
        public void SetTraitOverlaysEnabled(bool enabled)
        {
            _enableTraitOverlays = enabled;

            foreach (var overlay in _traitOverlays.Values)
            {
                if (overlay != null)
                {
                    // SetVisible not available on data class - would be handled by rendering system
                }
            }
        }

        /// <summary>
        /// Enables or disables heatmaps
        /// </summary>
        public void SetHeatmapsEnabled(bool enabled)
        {
            _enableHeatmaps = enabled;

            foreach (var renderer in _heatmapRenderers.Values)
            {
                if (renderer != null)
                {
                    // SetVisible not available on data class - would be handled by rendering system
                }
            }
        }

        /// <summary>
        /// Sets the heatmap type to display
        /// </summary>
        public void SetHeatmapType(HeatmapType type)
        {
            foreach (var renderer in _heatmapRenderers.Values)
            {
                if (renderer != null)
                {
                    // SetHeatmapType not available on data class - would be handled by rendering system
                }
            }
        }

        /// <summary>
        /// Cleans up all visualizations
        /// </summary>
        private void CleanupVisualizations()
        {
            // TraitOverlay and HeatmapRenderer are data classes, not UnityEngine.Objects
            // No need to call Destroy() on them - just clear the collections
            _traitOverlays.Clear();
            _heatmapRenderers.Clear();
        }

        /// <summary>
        /// Gets the number of plants being visualized
        /// </summary>
        public int GetVisualizedPlantCount()
        {
            return _traitOverlays.Count;
        }
    }

    /// <summary>
    /// Genetic traits structure
    /// </summary>
    public struct GeneticTraits
    {
        public float THCContent;
        public float CBDContent;
        public float Yield;
        public int FloweringTime;
        public float Height;
    }

    /// <summary>
    /// Heatmap types
    /// </summary>
    public enum HeatmapType
    {
        Potency,
        Yield,
        Health,
        GeneticDiversity
    }
}

