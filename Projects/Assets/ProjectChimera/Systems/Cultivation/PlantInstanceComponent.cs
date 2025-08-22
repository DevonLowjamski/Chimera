using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Interfaces;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Environment;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;
using ProjectChimera.Data.Cultivation;
using ProjectChimera.Data.Facilities;
using EnvironmentSystems = ProjectChimera.Systems.Environment;
using CultivationSystems = ProjectChimera.Systems.Cultivation;
using System;
using System.Collections.Generic;
// Explicit aliases to resolve type conflicts
using HarvestResult = ProjectChimera.Data.Facilities.HarvestResult;
using HarvestResults = ProjectChimera.Systems.Cultivation.HarvestResults;
using SystemsHarvestResults = ProjectChimera.Systems.Cultivation.SystemsHarvestResults;
using PlantInstance = ProjectChimera.Systems.Cultivation.PlantInstance;
using Camera = UnityEngine.Camera;
using GameManager = ProjectChimera.Core.GameManager;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// Unity component that represents a physical plant instance in the scene.
    /// Bridges the PlantInstance data with Unity GameObject visualization and interaction.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PlantInstanceComponent : MonoBehaviour, IWaterableComponent
    {
        [Header("Plant Configuration")]
        [SerializeField] private PlantStrainSO _strainData;
        [SerializeField] private bool _autoInitialize = true;
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private ParticleSystem _healthEffects;
        
        [Header("Growth Visualization")]
        [SerializeField] private float _growthAnimationSpeed = 1f;
        [SerializeField] private AnimationCurve _sizeCurve = AnimationCurve.Linear(0, 0.1f, 1, 1f);
        [SerializeField] private AnimationCurve _colorCurve = AnimationCurve.Linear(0, 0, 1, 1);
        [SerializeField] private Gradient _healthGradient = new Gradient();
        
        [Header("Interaction")]
        [SerializeField] private LayerMask _interactionMask = -1;
        [SerializeField] private float _interactionRange = 2f;
        [SerializeField] private bool _enableHover = true;
        
        // Plant Data
        private CultivationSystems.PlantInstance _plantInstance;
        private CultivationSystems.CultivationManager _cultivationManager;
        private EnvironmentSystems.EnvironmentManager _environmentManager;
        
        // Visual Components
        private Renderer _renderer;
        private MaterialPropertyBlock _materialBlock;
        private Vector3 _baseScale;
        private Color _baseColor;
        
        // Interaction State
        private bool _isHovered = false;
        private bool _isSelected = false;
        private UnityEngine.Camera _playerCamera;
        
        // Performance
        private float _lastUpdateTime;
        private float _updateInterval = 0.1f; // 10 FPS for plant updates
        
        // Events
        public System.Action<PlantInstanceComponent> OnPlantClicked;
        public System.Action<PlantInstanceComponent> OnPlantHovered;
        public System.Action<PlantInstanceComponent> OnPlantGrowthStageChanged;
        public System.Action<PlantInstanceComponent> OnPlantHealthChanged;
        
        // Properties
        public CultivationSystems.PlantInstance PlantData => _plantInstance;
        public PlantStrainSO StrainData => _strainData;
        public bool IsAlive => _plantInstance?.CurrentHealth > 0;
        public float GrowthProgress => _plantInstance?.GrowthProgress ?? 0f;
        public PlantGrowthStage CurrentStage => _plantInstance?.CurrentStage ?? PlantGrowthStage.Seed;
        public bool IsHarvestable => _plantInstance?.IsHarvestable ?? false;
        
        // IWaterableComponent Implementation
        public string ComponentId => _plantInstance?.PlantId ?? Guid.NewGuid().ToString();
        public float WaterLevel => _plantInstance?.WaterLevel ?? 0f;
        public float MaxWaterCapacity => 100f;
        public bool NeedsWatering => WaterLevel < 30f;
        public Transform Transform => transform;
        
        public void AddWater(float amount)
        {
            WaterPlant(amount);
        }
        
        public float GetMoistureLevel()
        {
            return WaterLevel;
        }
        
        public bool IsWithinRange(Vector3 sourcePosition, float range)
        {
            return Vector3.Distance(transform.position, sourcePosition) <= range;
        }
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void Start()
        {
            if (_autoInitialize)
            {
                InitializePlant();
            }
        }
        
        private void Update()
        {
            if (Time.time - _lastUpdateTime >= _updateInterval)
            {
                UpdatePlantVisualization();
                CheckInteraction();
                _lastUpdateTime = Time.time;
            }
        }
        
        private void OnMouseEnter()
        {
            if (_enableHover && !_isHovered)
            {
                _isHovered = true;
                OnPlantHovered?.Invoke(this);
                UpdateHoverEffect(true);
            }
        }
        
        private void OnMouseExit()
        {
            if (_isHovered)
            {
                _isHovered = false;
                UpdateHoverEffect(false);
            }
        }
        
        private void OnMouseDown()
        {
            if (Input.GetMouseButtonDown(0)) // Left click
            {
                OnPlantClicked?.Invoke(this);
                ToggleSelection();
            }
        }
        
        #region Initialization
        
        private void InitializeComponents()
        {
            _renderer = GetComponentInChildren<Renderer>();
            if (_renderer == null)
            {
                Debug.LogError($"No Renderer found on plant {name}");
                return;
            }
            
            _materialBlock = new MaterialPropertyBlock();
            _baseScale = transform.localScale;
            _baseColor = _renderer.material.color;
            
            // Find visual root if not set
            if (_visualRoot == null)
                _visualRoot = transform;
            
            // Get player camera
            _playerCamera = UnityEngine.Camera.main;
            if (_playerCamera == null)
                _playerCamera = FindObjectOfType<UnityEngine.Camera>();
        }
        
        /// <summary>
        /// Initialize plant with strain data
        /// </summary>
        public void InitializePlant(PlantStrainSO strainData = null)
        {
            if (strainData != null)
                _strainData = strainData;
            
            if (_strainData == null)
            {
                Debug.LogError($"No strain data provided for plant {name}");
                return;
            }
            
            // Get managers
            _cultivationManager = GameManager.Instance?.GetManager<CultivationSystems.CultivationManager>();
            _environmentManager = GameManager.Instance?.GetManager<EnvironmentSystems.EnvironmentManager>();
            
            if (_cultivationManager == null)
            {
                Debug.LogError("CultivationManager not found");
                return;
            }
            
            // Get or add PlantInstance component
            _plantInstance = GetComponent<PlantInstance>();
            if (_plantInstance == null)
            {
                _plantInstance = gameObject.AddComponent<PlantInstance>();
            }
            
            // Initialize plant instance data through proper methods
            _plantInstance.InitializeFromStrain(_strainData);
            
            // Register with plant manager
            // Register with cultivation manager (if it has a registration method)
            // _cultivationManager.RegisterPlantInstance(_plantInstance);
            
            // Initialize visual state
            UpdatePlantVisualization();
            
            Debug.Log($"Initialized plant: {_strainData.StrainName} at {transform.position}");
        }
        
        #endregion
        
        #region Plant Management
        
        /// <summary>
        /// Water the plant
        /// </summary>
        public void WaterPlant(float amount = 25f)
        {
            if (_plantInstance == null || !IsAlive) return;
            
            _plantInstance.WaterLevel = Mathf.Clamp(_plantInstance.WaterLevel + amount, 0f, 100f);
            _plantInstance.LastWatered = DateTime.Now;
            
            // Show water effect
            ShowWaterEffect();
            
            Debug.Log($"Watered plant {name} - Water level: {_plantInstance.WaterLevel:F1}%");
        }
        
        /// <summary>
        /// Add nutrients to the plant
        /// </summary>
        public void AddNutrients(float amount = 25f, string nutrientType = "General")
        {
            if (_plantInstance == null || !IsAlive) return;
            
            _plantInstance.NutrientLevel = Mathf.Clamp(_plantInstance.NutrientLevel + amount, 0f, 100f);
            _plantInstance.LastFed = DateTime.Now;
            
            Debug.Log($"Fed plant {name} with {nutrientType} - Nutrient level: {_plantInstance.NutrientLevel:F1}%");
        }
        
        /// <summary>
        /// Harvest the plant
        /// </summary>
        public SystemsHarvestResults HarvestPlant()
        {
            if (_plantInstance == null || !IsHarvestable)
            {
                Debug.LogWarning($"Plant {name} is not ready for harvest");
                return null;
            }
            
            bool harvestSuccessful = _cultivationManager?.HarvestPlant(_plantInstance.PlantId) ?? false;
            if (harvestSuccessful)
            {
                // Disable the plant GameObject or switch to harvested state
                ShowHarvestEffect();
                gameObject.SetActive(false);
                
                Debug.Log($"Harvested plant {name} successfully");
            }
            
            return harvestSuccessful ? new SystemsHarvestResults { TotalYieldGrams = 0f } : null;
        }
        
        /// <summary>
        /// Remove/destroy the plant
        /// </summary>
        public void RemovePlant()
        {
            if (_plantInstance != null && _cultivationManager != null)
            {
                _cultivationManager.RemovePlant(_plantInstance.PlantId);
            }
            
            Destroy(gameObject);
        }
        
        #endregion
        
        #region Visualization
        
        private void UpdatePlantVisualization()
        {
            if (_plantInstance == null || _renderer == null) return;
            
            // Update scale based on growth
            float growthSize = _sizeCurve.Evaluate(_plantInstance.GrowthProgress);
            _visualRoot.localScale = _baseScale * growthSize;
            
            // Update color based on health
            float healthPercent = _plantInstance.CurrentHealth / 100f;
            Color healthColor = _healthGradient.Evaluate(healthPercent);
            
            // Update color based on growth stage
            Color stageColor = GetStageColor(_plantInstance.CurrentStage);
            Color finalColor = Color.Lerp(healthColor, stageColor, 0.5f);
            
            _materialBlock.SetColor("_Color", finalColor);
            _renderer.SetPropertyBlock(_materialBlock);
            
            // Update particle effects
            UpdateHealthEffects();
        }
        
        private Color GetStageColor(PlantGrowthStage stage)
        {
            return stage switch
            {
                PlantGrowthStage.Seed => new Color(0.4f, 0.2f, 0.1f), // Brown
                PlantGrowthStage.Germination => new Color(0.6f, 0.8f, 0.4f), // Light green
                PlantGrowthStage.Seedling => new Color(0.3f, 0.8f, 0.3f), // Green
                PlantGrowthStage.Vegetative => new Color(0.2f, 0.9f, 0.2f), // Bright green
                PlantGrowthStage.Flowering => new Color(0.4f, 0.7f, 0.9f), // Blue-green
                PlantGrowthStage.Harvestable => new Color(0.9f, 0.8f, 0.3f), // Golden
                PlantGrowthStage.Drying => new Color(0.7f, 0.5f, 0.2f), // Brown-gold
                PlantGrowthStage.Curing => new Color(0.6f, 0.4f, 0.2f), // Dark brown
                _ => Color.white
            };
        }
        
        private void UpdateHealthEffects()
        {
            if (_healthEffects == null) return;
            
            float healthPercent = _plantInstance.CurrentHealth / 100f;
            
            if (healthPercent < 0.3f)
            {
                // Show stress/disease effects
                if (!_healthEffects.isPlaying)
                    _healthEffects.Play();
                
                var main = _healthEffects.main;
                main.startColor = Color.red;
            }
            else if (healthPercent > 0.8f && _plantInstance.CurrentStage == PlantGrowthStage.Flowering)
            {
                // Show healthy flowering effects
                if (!_healthEffects.isPlaying)
                    _healthEffects.Play();
                
                var main = _healthEffects.main;
                main.startColor = Color.yellow;
            }
            else
            {
                if (_healthEffects.isPlaying)
                    _healthEffects.Stop();
            }
        }
        
        private void UpdateHoverEffect(bool isHovered)
        {
            if (_renderer == null) return;
            
            if (isHovered)
            {
                _materialBlock.SetFloat("_Outline", 0.02f);
                _materialBlock.SetColor("_OutlineColor", Color.yellow);
            }
            else
            {
                _materialBlock.SetFloat("_Outline", 0f);
            }
            
            _renderer.SetPropertyBlock(_materialBlock);
        }
        
        private void ShowWaterEffect()
        {
            // Simple water effect - could be enhanced with particles
            StartCoroutine(ScaleEffect(1.1f, 0.3f));
        }
        
        private void ShowHarvestEffect()
        {
            // Simple harvest effect
            StartCoroutine(FadeOut(1f));
        }
        
        #endregion
        
        #region Interaction
        
        private void CheckInteraction()
        {
            if (_playerCamera == null) return;
            
            // Check if player is looking at this plant
            Vector3 screenPoint = _playerCamera.WorldToScreenPoint(transform.position);
            bool isInView = screenPoint.z > 0 && 
                           screenPoint.x >= 0 && screenPoint.x <= Screen.width &&
                           screenPoint.y >= 0 && screenPoint.y <= Screen.height;
            
            float distance = Vector3.Distance(_playerCamera.transform.position, transform.position);
            bool isInRange = distance <= _interactionRange;
            
            // Enable/disable interaction based on visibility and range
            var collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = isInView && isInRange;
            }
        }
        
        private void ToggleSelection()
        {
            _isSelected = !_isSelected;
            
            if (_isSelected)
            {
                _materialBlock.SetFloat("_Rim", 0.8f);
                _materialBlock.SetColor("_RimColor", Color.blue);
            }
            else
            {
                _materialBlock.SetFloat("_Rim", 0f);
            }
            
            _renderer.SetPropertyBlock(_materialBlock);
        }
        
        #endregion
        
        #region Animation Coroutines
        
        private System.Collections.IEnumerator ScaleEffect(float targetScale, float duration)
        {
            Vector3 originalScale = _visualRoot.localScale;
            Vector3 target = originalScale * targetScale;
            
            float elapsed = 0f;
            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.5f);
                _visualRoot.localScale = Vector3.Lerp(originalScale, target, t);
                yield return null;
            }
            
            elapsed = 0f;
            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.5f);
                _visualRoot.localScale = Vector3.Lerp(target, originalScale, t);
                yield return null;
            }
            
            _visualRoot.localScale = originalScale;
        }
        
        private System.Collections.IEnumerator FadeOut(float duration)
        {
            Color originalColor = _renderer.material.color;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                
                Color fadeColor = originalColor;
                fadeColor.a = alpha;
                _materialBlock.SetColor("_Color", fadeColor);
                _renderer.SetPropertyBlock(_materialBlock);
                
                yield return null;
            }
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Get plant status for UI display
        /// </summary>
        public PlantStatusInfo GetStatusInfo()
        {
            if (_plantInstance == null) return null;
            
            return new PlantStatusInfo
            {
                PlantId = _plantInstance.PlantId,
                StrainName = _strainData?.StrainName ?? "Unknown",
                CurrentStage = _plantInstance.CurrentStage,
                Health = _plantInstance.CurrentHealth,
                GrowthProgress = _plantInstance.GrowthProgress,
                WaterLevel = _plantInstance.WaterLevel,
                NutrientLevel = _plantInstance.NutrientLevel,
                DaysOld = (DateTime.Now - _plantInstance.PlantedDate).Days,
                IsHarvestable = _plantInstance.IsHarvestable,
                EstimatedYield = 0f // TODO: Implement yield calculation through cultivation manager services
            };
        }
        
        /// <summary>
        /// Update plant instance data (called by PlantManager)
        /// </summary>
        public void UpdatePlantData(PlantInstance updatedData)
        {
            var oldStage = _plantInstance?.CurrentStage;
            _plantInstance = updatedData;
            
            // Check for stage changes
            if (oldStage != _plantInstance.CurrentStage)
            {
                OnPlantGrowthStageChanged?.Invoke(this);
                Debug.Log($"Plant {name} advanced to {_plantInstance.CurrentStage}");
            }
            
            // Check for health changes
            if (_plantInstance.CurrentHealth <= 0 && IsAlive)
            {
                OnPlantHealthChanged?.Invoke(this);
                Debug.Log($"Plant {name} has died");
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Data structure for plant status display
    /// </summary>
    [System.Serializable]
    public class PlantStatusInfo
    {
        public string PlantId;
        public string StrainName;
        public PlantGrowthStage CurrentStage;
        public float Health;
        public float GrowthProgress;
        public float WaterLevel;
        public float NutrientLevel;
        public int DaysOld;
        public bool IsHarvestable;
        public float EstimatedYield;
    }
}