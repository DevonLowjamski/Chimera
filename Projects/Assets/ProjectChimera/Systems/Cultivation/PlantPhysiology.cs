using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Cultivation;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Core.Events;
using DataEnvironmental = ProjectChimera.Data.Shared.EnvironmentalConditions;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// Real-time plant physiology component that manages plant health, growth, and trait expression.
    /// This component should be attached to plant GameObjects in the scene for runtime simulation.
    /// </summary>
    [RequireComponent(typeof(Transform))]
    public class PlantPhysiology : ChimeraMonoBehaviour
    {
        [Header("Plant Data")]
        [SerializeField] private PlantInstanceSO _plantData;
        [SerializeField] private GrowthCalculationSO _growthCalculation;
        
        [Header("Visual Integration")]
        [SerializeField] private MonoBehaviour _visualInstance; // Loosely coupled - will be cast to PlantVisualInstance if available
        [SerializeField] private bool _autoConnectVisuals = true;
        
        [Header("Runtime Settings")]
        [SerializeField] private bool _enableRealtimeGrowth = true;
        [SerializeField] private float _growthUpdateInterval = 3600f; // 1 hour in seconds
        [SerializeField] private bool _enableStressVisualization = true;
        
        [Header("Environmental Sampling")]
        [SerializeField] private float _environmentSampleRadius = 2f;
        [SerializeField] private LayerMask _environmentLayers = -1;
        [SerializeField] private bool _useZoneEnvironment = true;
        [SerializeField] private string _environmentZoneId = "default";
        
        [Header("Debug Visualization")]
        [SerializeField] private bool _showGrowthGizmos = false;
        [SerializeField] private bool _showEnvironmentSampling = false;
        [SerializeField] private Color _healthyColor = Color.green;
        [SerializeField] private Color _stressedColor = Color.red;
        [SerializeField] private Color _dyingColor = Color.black;
        
        // Runtime state
        private CultivationManager _cultivationManager;
        private TimeManager _timeManager;
        private DataEnvironmental _currentEnvironment;
        private float _lastGrowthUpdate;
        private float _accumulatedGrowthTime;
        private Vector3 _initialScale;
        private Vector3 _targetScale;
        
        // Growth tracking
        private float _previousHeight;
        private float _previousHealth;
        private PlantGrowthStage _previousStage;
        
        // Events
        public System.Action<PlantPhysiology> OnPlantDied;
        public System.Action<PlantPhysiology, PlantGrowthStage, PlantGrowthStage> OnStageChanged;
        public System.Action<PlantPhysiology, float> OnHealthChanged;
        public System.Action<PlantPhysiology, float> OnGrowthOccurred;
        
        // Properties
        public PlantInstanceSO PlantData => _plantData;
        public bool IsAlive => _plantData != null && _plantData.OverallHealth > 0f;
        public bool IsHealthy => _plantData != null && _plantData.OverallHealth > 0.7f;
        public bool IsStressed => _plantData != null && _plantData.StressLevel > 0.5f;
        public bool NeedsWater => _plantData != null && _plantData.WaterLevel < 0.3f;
        public bool NeedsNutrients => _plantData != null && _plantData.NutrientLevel < 0.3f;
        public bool IsReadyForHarvest => _plantData != null && _plantData.CurrentGrowthStage == PlantGrowthStage.Harvest;
        public DataEnvironmental CurrentEnvironment => _currentEnvironment;
        
        protected override void OnEnable()
        {
            base.OnEnable();
            
            // Get initial scale for growth scaling
            _initialScale = transform.localScale;
            _targetScale = _initialScale;
            
            // Store initial values for change detection
            if (_plantData != null)
            {
                _previousHeight = _plantData.CurrentHeight;
                _previousHealth = _plantData.OverallHealth;
                _previousStage = _plantData.CurrentGrowthStage;
            }
            
            _lastGrowthUpdate = Time.time;
        }
        
        protected override void Start()
        {
            base.Start();
            
            // Get manager references
            _cultivationManager = GameManager.Instance?.GetManager<CultivationManager>();
            _timeManager = GameManager.Instance?.GetManager<TimeManager>();
            
            // Auto-connect visual instance if enabled
            if (_autoConnectVisuals && _visualInstance == null)
            {
                // Use reflection to find PlantVisualInstance without assembly dependency
                var visualComponents = GetComponents<MonoBehaviour>();
                foreach (var component in visualComponents)
                {
                    if (component.GetType().Name == "PlantVisualInstance")
                    {
                        _visualInstance = component;
                        break;
                    }
                }
            }
            
            // Initialize environmental conditions
            UpdateEnvironmentalConditions();
            
            if (_plantData == null)
            {
                Debug.LogWarning($"[PlantPhysiology] Plant '{name}' has no PlantInstanceSO assigned.", this);
            }
            
            if (_growthCalculation == null)
            {
                Debug.LogWarning($"[PlantPhysiology] Plant '{name}' has no GrowthCalculationSO assigned.", this);
            }
        }
        
        private void Update()
        {
            if (!_enableRealtimeGrowth || _plantData == null || !IsAlive) return;
            
            // Update environmental conditions periodically
            UpdateEnvironmentalConditions();
            
            // Accumulate growth time
            float deltaTime = GetEffectiveDeltaTime();
            _accumulatedGrowthTime += deltaTime;
            
            // Check if it's time for a growth update
            if (_accumulatedGrowthTime >= _growthUpdateInterval)
            {
                ProcessPhysiologyUpdate(_accumulatedGrowthTime / 86400f); // Convert to days
                _accumulatedGrowthTime = 0f;
                _lastGrowthUpdate = Time.time;
            }
            
            // Update visual representation
            UpdateVisualization();
        }
        
        /// <summary>
        /// Manually processes physiology update for a specific time period.
        /// </summary>
        public void ProcessPhysiologyUpdate(float timeInDays)
        {
            if (_plantData == null || !IsAlive) return;
            
            // Store previous values for change detection
            float previousHeight = _plantData.CurrentHeight;
            float previousHealth = _plantData.OverallHealth;
            PlantGrowthStage previousStage = _plantData.CurrentGrowthStage;
            
            // Process plant growth and development
            _plantData.ProcessDailyGrowth(_currentEnvironment, timeInDays);
            
            // Apply growth calculations if available
            if (_growthCalculation != null)
            {
                ApplyAdvancedGrowthCalculations(timeInDays);
            }
            
            // Detect and handle changes
            HandlePhysiologyChanges(previousHeight, previousHealth, previousStage);
            
            // Update visual representation
            UpdatePlantScale();
            
            // Check for critical conditions
            CheckCriticalConditions();
        }
        
        /// <summary>
        /// Sets the plant data for this physiology component.
        /// </summary>
        public void InitializePlant(PlantInstanceSO plantData, GrowthCalculationSO growthCalculation = null)
        {
            _plantData = plantData;
            _growthCalculation = growthCalculation;
            
            if (_plantData != null)
            {
                _previousHeight = _plantData.CurrentHeight;
                _previousHealth = _plantData.OverallHealth;
                _previousStage = _plantData.CurrentGrowthStage;
                
                // Set transform position if plant has world position
                if (_plantData.WorldPosition != Vector3.zero)
                {
                    transform.position = _plantData.WorldPosition;
                }
                
                UpdatePlantScale();
            }
            
            // Connect to visual instance if available
            if (_visualInstance != null && _plantData != null)
            {
                // Initialize visual instance with plant data using reflection
                var initializeMethod = _visualInstance.GetType().GetMethod("Initialize");
                if (initializeMethod != null)
                {
                    try
                    {
                        initializeMethod.Invoke(_visualInstance, new object[] { _plantData.PlantID, _plantData.Strain, null, Camera.main });
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"Failed to initialize visual instance: {ex.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Waters this plant with the specified amount.
        /// </summary>
        public void Water(float amount = 0.5f)
        {
            if (_plantData == null) return;
            
            _plantData.Water(amount, System.DateTime.Now);
            Debug.Log($"[PlantPhysiology] Watered plant '{_plantData.PlantName}' with {amount * 100f}% water.");
        }
        
        /// <summary>
        /// Feeds this plant with the specified nutrient amount.
        /// </summary>
        public void Feed(float amount = 0.4f)
        {
            if (_plantData == null) return;
            
            _plantData.Feed(amount, System.DateTime.Now);
            Debug.Log($"[PlantPhysiology] Fed plant '{_plantData.PlantName}' with {amount * 100f}% nutrients.");
        }
        
        /// <summary>
        /// Applies training to this plant.
        /// </summary>
        public void Train(string trainingType)
        {
            if (_plantData == null) return;
            
            _plantData.ApplyTraining(trainingType, System.DateTime.Now);
            Debug.Log($"[PlantPhysiology] Applied '{trainingType}' training to plant '{_plantData.PlantName}'.");
        }
        
        /// <summary>
        /// Harvests this plant if it's ready.
        /// </summary>
        public bool Harvest()
        {
            if (_plantData == null || !IsReadyForHarvest) return false;
            
            float yield = _plantData.CalculateYieldPotential();
            float potency = _plantData.CalculatePotencyPotential();
            
            Debug.Log($"[PlantPhysiology] Harvested plant '{_plantData.PlantName}': {yield:F1}g at {potency:F1}% potency");
            
            // Remove from cultivation manager
            if (_cultivationManager != null)
            {
                _cultivationManager.RemovePlant(_plantData.PlantID, true);
            }
            
            // Destroy this GameObject
            Destroy(gameObject);
            
            return true;
        }
        
        /// <summary>
        /// Gets the current stress level visualization color.
        /// </summary>
        public Color GetHealthColor()
        {
            if (_plantData == null) return Color.gray;
            
            if (_plantData.OverallHealth > 0.7f)
                return _healthyColor;
            else if (_plantData.OverallHealth > 0.3f)
                return Color.Lerp(_stressedColor, _healthyColor, (_plantData.OverallHealth - 0.3f) / 0.4f);
            else
                return Color.Lerp(_dyingColor, _stressedColor, _plantData.OverallHealth / 0.3f);
        }
        
        private float GetEffectiveDeltaTime()
        {
            float baseTime = Time.deltaTime;
            
            // Apply time acceleration from TimeManager if available
            if (_timeManager != null)
            {
                baseTime *= _timeManager.CurrentTimeScale;
            }
            
            return baseTime;
        }
        
        private void UpdateEnvironmentalConditions()
        {
            if (_useZoneEnvironment && _cultivationManager != null)
            {
                // Get environment from cultivation manager's zone system
                _currentEnvironment = _cultivationManager.GetZoneEnvironment(_environmentZoneId);
            }
            else
            {
                // Sample environment from nearby sources (future enhancement)
                _currentEnvironment = SampleLocalEnvironment();
            }
        }
        
        private DataEnvironmental SampleLocalEnvironment()
        {
            // For now, use default indoor conditions
            // Future enhancement: Sample from environmental controllers in radius
            return DataEnvironmental.CreateIndoorDefault();
        }
        
        private void ApplyAdvancedGrowthCalculations(float timeInDays)
        {
            if (_growthCalculation == null || _plantData == null) return;
            
            // Calculate advanced growth rate
            float advancedGrowthRate = _growthCalculation.CalculateGrowthRate(_plantData, _currentEnvironment);
            
            // Calculate resource consumption
            float waterConsumption = _growthCalculation.CalculateWaterConsumption(_plantData, _currentEnvironment) * timeInDays;
            float nutrientConsumption = _growthCalculation.CalculateNutrientConsumption(_plantData, _currentEnvironment) * timeInDays;
            float energyConsumption = _growthCalculation.CalculateEnergyConsumption(_plantData, _currentEnvironment) * timeInDays;
            
            // Calculate health changes
            float healthChange = _growthCalculation.CalculateHealthChange(_plantData, _currentEnvironment) * timeInDays;
            
            // Apply consumption (these would normally be handled by the plant data, but we can override/supplement)
            // Note: The consumption is already handled in PlantInstanceSO.ProcessDailyGrowth()
            // This is where we could apply more sophisticated models if needed
        }
        
        private void HandlePhysiologyChanges(float previousHeight, float previousHealth, PlantGrowthStage previousStage)
        {
            // Height change (growth)
            if (_plantData.CurrentHeight != previousHeight)
            {
                float growthAmount = _plantData.CurrentHeight - previousHeight;
                OnGrowthOccurred?.Invoke(this, growthAmount);
            }
            
            // Health change
            if (_plantData.OverallHealth != previousHealth)
            {
                OnHealthChanged?.Invoke(this, _plantData.OverallHealth);
                
                // Check for death
                if (previousHealth > 0f && _plantData.OverallHealth <= 0f)
                {
                    OnPlantDied?.Invoke(this);
                    Debug.LogWarning($"[PlantPhysiology] Plant '{_plantData.PlantName}' has died!");
                }
            }
            
            // Stage change
            if (_plantData.CurrentGrowthStage != previousStage)
            {
                OnStageChanged?.Invoke(this, previousStage, _plantData.CurrentGrowthStage);
                Debug.Log($"[PlantPhysiology] Plant '{_plantData.PlantName}' transitioned from {previousStage} to {_plantData.CurrentGrowthStage}");
            }
        }
        
        private void UpdatePlantScale()
        {
            if (_plantData == null) return;
            
            // Calculate scale based on plant height (assuming 1 Unity unit = 1 meter)
            float heightInMeters = _plantData.CurrentHeight / 100f; // Convert cm to meters
            float scaleMultiplier = Mathf.Max(0.01f, heightInMeters);
            
            _targetScale = _initialScale * scaleMultiplier;
        }
        
        private void UpdateVisualization()
        {
            // Smoothly scale to target size
            if (Vector3.Distance(transform.localScale, _targetScale) > 0.001f)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * 2f);
            }
            
            // Update visual instance if connected
            if (_visualInstance != null)
            {
                var updateMethod = _visualInstance.GetType().GetMethod("UpdateVisualParameters");
                if (updateMethod != null)
                {
                    try
                    {
                        updateMethod.Invoke(_visualInstance, null);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"Failed to update visual parameters: {ex.Message}");
                    }
                }
            }
            
            // Update stress visualization
            if (_enableStressVisualization)
            {
                UpdateStressVisualization();
            }
        }
        
        private void UpdateStressVisualization()
        {
            if (_plantData == null) return;
            
            // Get renderers and update colors based on health/stress
            var renderers = GetComponentsInChildren<Renderer>();
            Color targetColor = GetHealthColor();
            
            foreach (var renderer in renderers)
            {
                if (renderer.material.HasProperty("_Color"))
                {
                    renderer.material.color = Color.Lerp(renderer.material.color, targetColor, Time.deltaTime);
                }
            }
        }
        
        private void CheckCriticalConditions()
        {
            if (_plantData == null) return;
            
            // Check for critical water/nutrient levels
            if (NeedsWater)
            {
                Debug.LogWarning($"[PlantPhysiology] Plant '{_plantData.PlantName}' critically needs water! ({_plantData.WaterLevel * 100f:F0}%)");
            }
            
            if (NeedsNutrients)
            {
                Debug.LogWarning($"[PlantPhysiology] Plant '{_plantData.PlantName}' critically needs nutrients! ({_plantData.NutrientLevel * 100f:F0}%)");
            }
            
            // Check for harvest readiness
            if (IsReadyForHarvest)
            {
                Debug.Log($"[PlantPhysiology] Plant '{_plantData.PlantName}' is ready for harvest!");
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!_showGrowthGizmos && !_showEnvironmentSampling) return;
            
            if (_showGrowthGizmos && _plantData != null)
            {
                // Draw health indicator
                Gizmos.color = GetHealthColor();
                Vector3 healthPos = transform.position + Vector3.up * 2f;
                Gizmos.DrawWireSphere(healthPos, 0.2f);
                
                // Draw growth stage indicator
                Gizmos.color = Color.blue;
                Vector3 stagePos = transform.position + Vector3.up * 2.5f;
                float stageRadius = 0.1f + (int)_plantData.CurrentGrowthStage * 0.05f;
                Gizmos.DrawWireSphere(stagePos, stageRadius);
            }
            
            if (_showEnvironmentSampling)
            {
                // Draw environment sampling radius
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, _environmentSampleRadius);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (_plantData == null) return;
            
            // Draw detailed plant information when selected
            Gizmos.color = Color.cyan;
            
            // Draw plant size representation
            Vector3 size = new Vector3(_plantData.CurrentWidth / 100f, _plantData.CurrentHeight / 100f, _plantData.CurrentWidth / 100f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * size.y * 0.5f, size);
            
            // Draw root zone
            Gizmos.color = Color.brown;
            float rootRadius = _plantData.CurrentWidth / 200f * _plantData.RootMassPercentage / 20f;
            Gizmos.DrawWireSphere(transform.position, rootRadius);
        }
    }
}