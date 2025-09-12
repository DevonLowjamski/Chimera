using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Shared;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// SIMPLE: Basic plant visualization system aligned with Project Chimera's cultivation vision.
    /// Focuses on essential plant visual representation without complex animations or effects.
    /// </summary>
    public class PlantVisualizationSystem : MonoBehaviour
    {
        [Header("Basic Visual Settings")]
        [SerializeField] private bool _enableBasicVisualization = true;
        [SerializeField] private bool _enableSizeUpdates = true;
        [SerializeField] private bool _enableColorUpdates = true;
        [SerializeField] private bool _enableLogging = true;

        [Header("Visual Components")]
        [SerializeField] private Transform _plantTransform;
        [SerializeField] private Renderer _plantRenderer;

        // Basic visual state
        private Vector3 _baseScale = Vector3.one;
        private Color _healthyColor = Color.green;
        private Color _unhealthyColor = Color.yellow;
        private bool _isInitialized = false;

        /// <summary>
        /// Initialize the basic visualization system
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            if (_plantTransform != null)
            {
                _baseScale = _plantTransform.localScale;
            }

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("[PlantVisualizationSystem] Initialized successfully");
            }
        }

        /// <summary>
        /// Update plant visualization
        /// </summary>
        public void UpdateVisualization(float deltaTime)
        {
            if (!_enableBasicVisualization || !_isInitialized) return;

            // Basic updates can be added here if needed
            // For now, the system is ready for basic operations
        }

        /// <summary>
        /// Update plant size based on growth stage
        /// </summary>
        public void UpdatePlantSize(PlantGrowthStage stage)
        {
            if (!_enableSizeUpdates || _plantTransform == null) return;

            float sizeMultiplier = GetSizeMultiplierForStage(stage);
            Vector3 targetScale = _baseScale * sizeMultiplier;

            _plantTransform.localScale = targetScale;

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[PlantVisualizationSystem] Updated size for stage {stage}: {sizeMultiplier:F2}x");
            }
        }

        /// <summary>
        /// Update plant color based on health
        /// </summary>
        public void UpdatePlantColor(float health)
        {
            if (!_enableColorUpdates || _plantRenderer == null) return;

            Color targetColor = Color.Lerp(_unhealthyColor, _healthyColor, health);
            _plantRenderer.material.color = targetColor;

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[PlantVisualizationSystem] Updated color for health {health:F2}");
            }
        }

        /// <summary>
        /// Set plant visibility
        /// </summary>
        public void SetPlantVisible(bool visible)
        {
            if (_plantRenderer != null)
            {
                _plantRenderer.enabled = visible;
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[PlantVisualizationSystem] Plant visibility set to {visible}");
            }
        }

        /// <summary>
        /// Get current plant size
        /// </summary>
        public Vector3 GetCurrentSize()
        {
            return _plantTransform != null ? _plantTransform.localScale : Vector3.one;
        }

        /// <summary>
        /// Get current plant color
        /// </summary>
        public Color GetCurrentColor()
        {
            return _plantRenderer != null ? _plantRenderer.material.color : Color.white;
        }

        /// <summary>
        /// Reset plant to base appearance
        /// </summary>
        public void ResetToBaseAppearance()
        {
            if (_plantTransform != null)
            {
                _plantTransform.localScale = _baseScale;
            }

            if (_plantRenderer != null)
            {
                _plantRenderer.material.color = _healthyColor;
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("[PlantVisualizationSystem] Reset to base appearance");
            }
        }

        /// <summary>
        /// Set base scale for the plant
        /// </summary>
        public void SetBaseScale(Vector3 scale)
        {
            _baseScale = scale;

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[PlantVisualizationSystem] Base scale set to {scale}");
            }
        }

        /// <summary>
        /// Set healthy color
        /// </summary>
        public void SetHealthyColor(Color color)
        {
            _healthyColor = color;

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[PlantVisualizationSystem] Healthy color set to {color}");
            }
        }

        /// <summary>
        /// Set unhealthy color
        /// </summary>
        public void SetUnhealthyColor(Color color)
        {
            _unhealthyColor = color;

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[PlantVisualizationSystem] Unhealthy color set to {color}");
            }
        }

        /// <summary>
        /// Get visualization statistics
        /// </summary>
        public VisualizationStatistics GetVisualizationStatistics()
        {
            return new VisualizationStatistics
            {
                IsInitialized = _isInitialized,
                CurrentSize = GetCurrentSize(),
                CurrentColor = GetCurrentColor(),
                BaseScale = _baseScale,
                IsVisible = _plantRenderer != null ? _plantRenderer.enabled : false
            };
        }

        #region Private Methods

        private float GetSizeMultiplierForStage(PlantGrowthStage stage)
        {
            switch (stage)
            {
                case PlantGrowthStage.Seedling:
                    return 0.3f;
                case PlantGrowthStage.Vegetative:
                    return 0.7f;
                case PlantGrowthStage.Flowering:
                    return 1.0f;
                default:
                    return 1.0f;
            }
        }

        #endregion
    }

    /// <summary>
    /// Visualization statistics
    /// </summary>
    [System.Serializable]
    public class VisualizationStatistics
    {
        public bool IsInitialized;
        public Vector3 CurrentSize;
        public Color CurrentColor;
        public Vector3 BaseScale;
        public bool IsVisible;
    }
}
