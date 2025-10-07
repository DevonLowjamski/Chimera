using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Cultivation.Plant;
using ProjectChimera.Core.Pooling;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// BASIC: Simple plant component for Project Chimera's cultivation system.
    /// Focuses on essential plant visualization and interaction.
    /// </summary>
    public class PlantInstanceComponent : MonoBehaviour, IPoolable
    {
        [Header("Basic Plant Settings")]
        [SerializeField] private string _plantId = "";
        [SerializeField] private string _strainName = "Basic Plant";
        [SerializeField] private PlantGrowthStage _currentStage = PlantGrowthStage.Seedling;
        [SerializeField] private float _health = 1f;
        [SerializeField] private float _growthProgress = 0f;

        [Header("Visual Settings")]
        [SerializeField] private Color _healthyColor = Color.green;
        [SerializeField] private Color _stressedColor = Color.yellow;
        [SerializeField] private Color _deadColor = Color.brown;
        [SerializeField] private float _maxScale = 1f;

        // Basic plant data
        private PlantInstance _plantData;
        private Renderer _renderer;
        private bool _isInitialized = false;

        /// <summary>
        /// Events for plant interactions
        /// </summary>
        public event System.Action<PlantInstanceComponent> OnPlantClicked;
        public event System.Action<PlantInstanceComponent> OnPlantHealthChanged;

        /// <summary>
        /// Initialize basic plant component
        /// </summary>
        public void Initialize(string plantId, string strainName)
        {
            Initialize(new PlantInstance
            {
                PlantId = plantId,
                StrainName = strainName,
                GrowthStage = PlantGrowthStage.Seedling,
                Health = 1f
            });
        }

        /// <summary>
        /// Initialize with PlantInstance data
        /// </summary>
        public void Initialize(PlantInstance plantData)
        {
            if (_isInitialized) return;

            _plantData = plantData;
            _plantId = plantData.PlantId;
            _strainName = plantData.StrainName;
            _currentStage = plantData.GrowthStage;
            _health = plantData.Health;

            _renderer = GetComponent<Renderer>();
            UpdateVisuals();

            _isInitialized = true;

            ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
        }

        /// <summary>
        /// Update plant health
        /// </summary>
        public void UpdateHealth(float newHealth)
        {
            if (_health == newHealth) return;

            _health = Mathf.Clamp01(newHealth);
            if (_plantData != null)
            {
                _plantData.Health = _health;
            }

            UpdateVisuals();
            OnPlantHealthChanged?.Invoke(this);

            ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
        }

        /// <summary>
        /// Update growth progress
        /// </summary>
        public void UpdateGrowth(float progress)
        {
            _growthProgress = Mathf.Clamp01(progress);
            if (_plantData != null)
            {
                _plantData.GrowthProgress = _growthProgress;
            }

            UpdateScale();
        }

        /// <summary>
        /// Update growth stage
        /// </summary>
        public void UpdateGrowthStage(PlantGrowthStage stage)
        {
            _currentStage = stage;
            if (_plantData != null)
            {
                _plantData.CurrentGrowthStage = stage;
            }
        }

        /// <summary>
        /// Water the plant
        /// </summary>
        public void WaterPlant(float amount)
        {
            UpdateHealth(_health + amount * 0.1f); // Water improves health

            if (_plantData != null)
            {
                _plantData.LastWatering = System.DateTime.Now;
            }

            ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
        }

        /// <summary>
        /// Apply nutrients to the plant
        /// </summary>
        public void ApplyNutrients(float amount)
        {
            UpdateHealth(_health + amount * 0.05f); // Nutrients improve health slightly

            if (_plantData != null)
            {
                _plantData.LastFeeding = System.DateTime.Now;
            }

            ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
        }

        /// <summary>
        /// Handle mouse click
        /// </summary>
        private void OnMouseDown()
        {
            OnPlantClicked?.Invoke(this);
            ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
        }

        /// <summary>
        /// Get plant data
        /// </summary>
        public PlantInstance GetPlantData()
        {
            return _plantData;
        }

        /// <summary>
        /// Check if plant is alive
        /// </summary>
        public bool IsAlive()
        {
            return _health > 0f;
        }

        /// <summary>
        /// Check if plant is harvestable
        /// </summary>
        public bool IsHarvestable()
        {
            return _currentStage == PlantGrowthStage.Flowering && _growthProgress >= 0.9f;
        }

        /// <summary>
        /// Get plant status
        /// </summary>
        public PlantStatus GetStatus()
        {
            return new PlantStatus
            {
                PlantId = _plantId,
                StrainName = _strainName,
                CurrentStage = _currentStage,
                Health = _health,
                GrowthProgress = _growthProgress,
                IsAlive = IsAlive(),
                IsHarvestable = IsHarvestable()
            };
        }

        #region Private Methods

        private void UpdateVisuals()
        {
            if (_renderer == null) return;

            // Update color based on health
            Color healthColor;
            if (_health > 0.7f)
            {
                healthColor = _healthyColor;
            }
            else if (_health > 0.3f)
            {
                healthColor = Color.Lerp(_stressedColor, _healthyColor, (_health - 0.3f) / 0.4f);
            }
            else
            {
                healthColor = Color.Lerp(_deadColor, _stressedColor, _health / 0.3f);
            }

            _renderer.material.color = healthColor;
        }

        private void UpdateScale()
        {
            // Scale based on growth progress
            float scale = Mathf.Lerp(0.1f, _maxScale, _growthProgress);
            transform.localScale = new Vector3(scale, scale, scale);
        }

        #endregion

        #region Pooling Support

        /// <summary>
        /// Update visuals with plant instance data
        /// </summary>
        public void UpdateVisuals(PlantInstance plantData)
        {
            if (plantData != null)
            {
                _plantData = plantData;
                _health = plantData.Health;
                _currentStage = plantData.GrowthStage;
                UpdateVisuals();
                UpdateScale();
            }
        }

        /// <summary>
        /// Reset plant for pool reuse
        /// </summary>
        public void ResetForPool()
        {
            _health = 1f;
            _growthProgress = 0f;
            _currentStage = PlantGrowthStage.Seedling;
            _isInitialized = false;
            transform.localScale = Vector3.one * 0.1f;
            
            if (_renderer != null)
            {
                _renderer.material.color = _healthyColor;
            }
        }

        /// <summary>
        /// Cleanup before destruction
        /// </summary>
        public void CleanupForDestroy()
        {
            OnPlantClicked = null;
            OnPlantHealthChanged = null;
            _plantData = null;
        }

        /// <summary>
        /// IPoolable implementation - called when retrieved from pool
        /// </summary>
        public void OnGetFromPool()
        {
            gameObject.SetActive(true);
            _isInitialized = false; // Allow re-initialization
        }

        /// <summary>
        /// IPoolable implementation - called when returned to pool
        /// </summary>
        public void OnReturnToPool()
        {
            ResetForPool();
            gameObject.SetActive(false);
        }

        #endregion
    }

    /// <summary>
    /// Plant status information
    /// </summary>
    [System.Serializable]
    public struct PlantStatus
    {
        public string PlantId;
        public string StrainName;
        public PlantGrowthStage CurrentStage;
        public float Health;
        public float GrowthProgress;
        public bool IsAlive;
        public bool IsHarvestable;
    }
}
