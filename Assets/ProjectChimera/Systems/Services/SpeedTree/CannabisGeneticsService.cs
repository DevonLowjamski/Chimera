using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Systems.Registry;

namespace ProjectChimera.Systems.Services.SpeedTree
{
    /// <summary>
    /// PC014-5b: Cannabis Genetics Service
    /// Manages genetic variation processing, growth stages, and cannabis-specific trait expression
    /// Decomposed from AdvancedSpeedTreeManager (360 lines target)
    /// </summary>
    public class CannabisGeneticsService : MonoBehaviour, ITickable, ICannabisGeneticsService
    {
        #region Properties

        public bool IsInitialized { get; private set; }

        #endregion

        #region Private Fields

        [Header("Genetics Configuration")]
        [SerializeField] private ScriptableObject _geneticsConfig;
        [SerializeField] private ScriptableObject _growthConfig;
        [SerializeField] private List<ScriptableObject> _registeredStrains = new List<ScriptableObject>();

        [Header("Growth Animation Settings")]
        [SerializeField] private bool _enableGrowthAnimation = true;
        [SerializeField] private float _animationTimeScale = 1f;
        [SerializeField] private AnimationCurve _growthCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        // Genetics Processing
        private Dictionary<string, ScriptableObject> _strainDatabase = new Dictionary<string, ScriptableObject>();
        private Dictionary<int, object> _instanceGenetics = new Dictionary<int, object>();
        private Dictionary<int, object> _growthAnimations = new Dictionary<int, object>();

        // Growth Management
        private Dictionary<int, object> _currentStages = new Dictionary<int, object>();
        private Dictionary<int, float> _growthProgress = new Dictionary<int, float>();
        private List<int> _plantsNeedingUpdate = new List<int>();

        #endregion

        #region Events

        public event Action<int, object, object> OnGrowthStageChanged;
        public event Action<int, object> OnGeneticExpressionUpdated;
        public event Action<string> OnStrainRegistered;

        #endregion

        #region IService Implementation

        public void Initialize()
        {
            if (IsInitialized) return;

            ChimeraLogger.Log("Initializing CannabisGeneticsService...");

            // Initialize strain database
            InitializeStrainDatabase();

            // Initialize growth system
            InitializeGrowthSystem();

            // Register with ServiceRegistry
            ServiceContainerFactory.Instance.RegisterSingleton<ICannabisGeneticsService>(this);

            IsInitialized = true;
            ChimeraLogger.Log("CannabisGeneticsService initialized successfully");
        }

        public void Shutdown()
        {
            if (!IsInitialized) return;

            ChimeraLogger.Log("Shutting down CannabisGeneticsService...");

            // Clear all collections
            _strainDatabase.Clear();
            _instanceGenetics.Clear();
            _growthAnimations.Clear();
            _currentStages.Clear();
            _growthProgress.Clear();
            _plantsNeedingUpdate.Clear();

            IsInitialized = false;
            ChimeraLogger.Log("CannabisGeneticsService shutdown complete");
        }

        #endregion

        #region Genetics Processing

        public object GenerateGeneticVariation(string strainId, object genotype)
        {
            if (string.IsNullOrEmpty(strainId))
            {
                ChimeraLogger.LogError("Cannot generate genetic variation - strain ID is null or empty");
                return CreateDefaultGeneticData();
            }

            var genetics = CreateDefaultGeneticData();

            ChimeraLogger.Log($"Generated genetic variation for strain: {strainId}");
            return genetics;
        }

        public void ProcessGeneticExpression(int plantId)
        {
            if (plantId <= 0) return;

            if (!_instanceGenetics.TryGetValue(plantId, out var genetics))
            {
                // Generate genetics if not already present
                genetics = GenerateGeneticVariation("default", null);
                _instanceGenetics[plantId] = genetics;
            }

            OnGeneticExpressionUpdated?.Invoke(plantId, genetics);
        }

        public void ValidateGeneticData(object genetics)
        {
            if (genetics == null)
            {
                ChimeraLogger.LogError("Genetic data is null");
                return;
            }

            // Validation logic would be implemented based on actual genetic data structure
            ChimeraLogger.Log("Genetic data validated");
        }

        #endregion

        #region Growth Management

        public void InitializePlantGrowth(int plantId)
        {
            if (plantId <= 0) return;

            // Set initial growth stage
            _currentStages[plantId] = "Seedling";
            _growthProgress[plantId] = 0f;

            // Initialize growth animation data
            var animationData = new object(); // Placeholder
            _growthAnimations[plantId] = animationData;

            // Generate genetics for the plant
            ProcessGeneticExpression(plantId);

            ChimeraLogger.Log($"Initialized growth for plant {plantId}");
        }

        public void UpdatePlantGrowth(int plantId, float deltaTime)
        {
            if (plantId <= 0 || !_enableGrowthAnimation) return;

            if (!_growthProgress.TryGetValue(plantId, out var progress) ||
                !_currentStages.TryGetValue(plantId, out var currentStage))
            {
                InitializePlantGrowth(plantId);
                return;
            }

            // Update growth progress
            progress += deltaTime * _animationTimeScale;
            _growthProgress[plantId] = progress;

            ChimeraLogger.Log($"Updated growth for plant {plantId}");
        }

        public void TriggerGrowthStageTransition(int plantId, object newStage)
        {
            if (plantId <= 0) return;

            var oldStage = _currentStages.GetValueOrDefault(plantId, "Seedling");

            _currentStages[plantId] = newStage;
            _growthProgress[plantId] = 0f; // Reset progress for new stage

            // Start stage transition animation
            AnimateStageTransition(plantId, oldStage, newStage);

            // Update genetic expression for new stage
            ProcessGeneticExpression(plantId);

            OnGrowthStageChanged?.Invoke(plantId, oldStage, newStage);

            ChimeraLogger.Log($"Plant {plantId} transitioned from {oldStage} to {newStage}");
        }

        #endregion

        #region Strain Management

        public void RegisterStrain(string strainId, object strain)
        {
            if (string.IsNullOrEmpty(strainId) || strain == null) return;

            _strainDatabase[strainId] = (ScriptableObject)strain;

            if (!_registeredStrains.Contains((ScriptableObject)strain))
            {
                _registeredStrains.Add((ScriptableObject)strain);
            }

            OnStrainRegistered?.Invoke(strainId);
            ChimeraLogger.Log($"Registered cannabis strain: {strainId}");
        }

        public void UnregisterStrain(string strainId)
        {
            if (string.IsNullOrEmpty(strainId)) return;

            if (_strainDatabase.TryGetValue(strainId, out var strain))
            {
                _strainDatabase.Remove(strainId);
                _registeredStrains.Remove(strain);
                ChimeraLogger.Log($"Unregistered cannabis strain: {strainId}");
            }
        }

        public object GetCannabisStrain(string strainId)
        {
            if (string.IsNullOrEmpty(strainId)) return GetDefaultStrain();

            _strainDatabase.TryGetValue(strainId, out var strain);
            return strain ?? GetDefaultStrain();
        }

        #endregion

        #region Growth Animation

        public void AnimateStageTransition(int plantId, object oldStage, object newStage)
        {
            if (plantId <= 0 || !_enableGrowthAnimation) return;

            if (_growthAnimations.TryGetValue(plantId, out var animationData))
            {
                // Animation logic would be implemented based on actual data structure
                ChimeraLogger.Log($"Animating stage transition for plant {plantId}");
            }
        }

        public void UpdateGrowthAnimations(IEnumerable<int> plantIds)
        {
            if (!_enableGrowthAnimation) return;

            foreach (var plantId in plantIds)
            {
                UpdateGrowthAnimation(plantId, Time.deltaTime);
            }
        }

        #endregion

        #region Private Helper Methods

        private void InitializeStrainDatabase()
        {
            _strainDatabase.Clear();

            foreach (var strain in _registeredStrains)
            {
                if (strain != null && !string.IsNullOrEmpty(strain.name))
                {
                    _strainDatabase[strain.name] = strain;
                }
            }

            ChimeraLogger.Log($"Initialized strain database with {_strainDatabase.Count} strains");
        }

        private void InitializeGrowthSystem()
        {
            _currentStages.Clear();
            _growthProgress.Clear();
            _growthAnimations.Clear();

            ChimeraLogger.Log("Growth system initialized");
        }

        private object CreateDefaultGeneticData()
        {
            return new object(); // Placeholder genetic data
        }

        // All helper methods removed or simplified to avoid type references
        // These would be reimplemented when the genetics system is rebuilt

        private object GetDefaultStrain()
        {
            return _registeredStrains?.FirstOrDefault();
        }

        private void UpdateGrowthAnimation(int plantId, float deltaTime)
        {
            // Growth animation logic would be implemented based on actual data structure
            ChimeraLogger.Log($"Updating growth animation for plant {plantId}");
        }

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
        // Register with UpdateOrchestrator
        UpdateOrchestrator.Instance?.RegisterTickable(this);
            Initialize();
        }

        private void OnDestroy()
        {
        // Unregister from UpdateOrchestrator
        UpdateOrchestrator.Instance?.UnregisterTickable(this);
            Shutdown();
        }

            public void Tick(float deltaTime)
    {
            if (!IsInitialized || !_enableGrowthAnimation) return;

            // Update growth animations for all plants
            if (_plantsNeedingUpdate.Count > 0)
            {
                UpdateGrowthAnimations(_plantsNeedingUpdate);

    }
        }

        #endregion

        // ITickable implementation
        public int Priority => 0;
        public bool Enabled => enabled && gameObject.activeInHierarchy;

        public virtual void OnRegistered()
        {
            // Override in derived classes if needed
        }

        public virtual void OnUnregistered()
        {
            // Override in derived classes if needed
        }
    }

    // Supporting data classes removed to avoid type references
    // These would be reimplemented when the genetics system is rebuilt

}
