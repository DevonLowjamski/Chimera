using UnityEngine;
using ProjectChimera.Core.Logging;
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
    public class GeneticVisualizationManager : MonoBehaviour, ITickable
    {
        [Header("Visualization Settings")]
        [SerializeField] private bool _enableTraitOverlays = true;
        [SerializeField] private bool _enableHeatmaps = true;
        [SerializeField] private float _updateInterval = 1f;
        
        // ITickable implementation
        public int Priority => TickPriority.HUD + 10;
        public bool Enabled => enabled && gameObject.activeInHierarchy;

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
            // Primary: Try ServiceContainer resolution for registered plant GameObjects
            var plants = ServiceContainerFactory.Instance.ResolveAll<GameObject>()
                ?.Where(go => go.CompareTag("Plant")).ToArray();
                
            if (plants?.Any() != true)
            {
                // Fallback: Find all plants in the scene and set up visualizations
                plants = GameObject.FindGameObjectsWithTag("Plant");
                
                // Auto-register discovered plants in ServiceContainer for future use
                foreach (var plant in plants)
                {
                    ServiceContainerFactory.Instance.RegisterInstance<GameObject>(plant);
                }
                
                ChimeraLogger.Log($"[GeneticVisualizationManager] Registered {plants.Length} plant GameObjects in ServiceContainer");
            }
            else
            {
                ChimeraLogger.Log("[GeneticVisualizationManager] Using plant GameObjects from ServiceContainer");
            }
            
            foreach (var plant in plants)
            {
                RegisterPlantForVisualization(plant);
            }

            ChimeraLogger.Log($"[GeneticVisualizationManager] Initialized visualizations for {plants.Length} plants");
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
                Destroy(_traitOverlays[plant]);
                _traitOverlays.Remove(plant);
            }

            if (_heatmapRenderers.ContainsKey(plant))
            {
                Destroy(_heatmapRenderers[plant]);
                _heatmapRenderers.Remove(plant);
            }
        }

        /// <summary>
        /// Creates a trait overlay for a plant
        /// </summary>
        private void CreateTraitOverlay(GameObject plant)
        {
            var overlay = plant.AddComponent<TraitOverlay>();
            overlay.Initialize(_overlayHeight);
            _traitOverlays[plant] = overlay;
        }

        /// <summary>
        /// Creates a heatmap renderer for a plant
        /// </summary>
        private void CreateHeatmapRenderer(GameObject plant)
        {
            var heatmapRenderer = plant.AddComponent<HeatmapRenderer>();
            heatmapRenderer.Initialize(_potencyGradient, _yieldGradient, _healthGradient);
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

            // Update overlay with trait information
            overlay.UpdateTraits(traits);
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
            renderer.UpdateHeatmap(potency, yield, health);
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
                    overlay.SetVisible(enabled);
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
                    renderer.SetVisible(enabled);
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
                    renderer.SetHeatmapType(type);
                }
            }
        }

        /// <summary>
        /// Cleans up all visualizations
        /// </summary>
        private void CleanupVisualizations()
        {
            foreach (var overlay in _traitOverlays.Values)
            {
                if (overlay != null)
                {
                    Destroy(overlay);
                }
            }

            foreach (var renderer in _heatmapRenderers.Values)
            {
                if (renderer != null)
                {
                    Destroy(renderer);
                }
            }

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

