using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Shared;
using ProjectChimera.Core;
using PlantInstance = ProjectChimera.Systems.Cultivation.PlantInstance;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// SIMPLE: Basic plant lifecycle management aligned with Project Chimera's cultivation vision.
    /// Focuses on essential plant creation and management without complex lifecycle systems.
    /// </summary>
    public class PlantLifecycleService : MonoBehaviour
    {
        [Header("Basic Plant Settings")]
        [SerializeField] private bool _enableBasicManagement = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private int _maxPlants = 50;

        // Basic plant tracking
        private readonly List<PlantInstance> _activePlants = new List<PlantInstance>();
        private bool _isInitialized = false;

        /// <summary>
        /// Events for plant lifecycle
        /// </summary>
        public event System.Action<PlantInstance> OnPlantCreated;
        public event System.Action<PlantInstance> OnPlantDestroyed;

        /// <summary>
        /// Initialize basic plant management
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
            }
        }

        /// <summary>
        /// Create a new plant
        /// </summary>
        public PlantInstance CreatePlant(Vector3 position, string plantName = "Plant")
        {
            if (!_enableBasicManagement || !_isInitialized) return null;

            if (_activePlants.Count >= _maxPlants)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
                }
                return null;
            }

            // Create basic plant instance
            var plant = new PlantInstance();

            // Initialize using available setters
            plant.PlantID = System.Guid.NewGuid().ToString();
            // PlantName is read-only, cannot be set after creation
            // WorldPosition property doesn't exist, will be handled by position parameter
            plant.CurrentStage = PlantGrowthStage.Seedling;
            plant.AgeInDays = (int)0f; // Convert to int to match property type
            plant.Health = 1f;
            plant.LastWatered = System.DateTime.Now;
            plant.LastFed = System.DateTime.Now.AddDays(-1);

            _activePlants.Add(plant);
            OnPlantCreated?.Invoke(plant);

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
            }

            return plant;
        }

        /// <summary>
        /// Destroy a plant
        /// </summary>
        public bool DestroyPlant(PlantInstance plant)
        {
            if (plant == null) return false;

            if (_activePlants.Remove(plant))
            {
                OnPlantDestroyed?.Invoke(plant);

                if (_enableLogging)
                {
                    ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Destroy plant by ID
        /// </summary>
        public bool DestroyPlant(string plantId)
        {
            var plant = GetPlantById(plantId);
            return DestroyPlant(plant);
        }

        /// <summary>
        /// Get plant by ID
        /// </summary>
        public PlantInstance GetPlantById(string plantId)
        {
            return _activePlants.Find(p => p.PlantID == plantId);
        }

        /// <summary>
        /// Get all active plants
        /// </summary>
        public List<PlantInstance> GetAllPlants()
        {
            return new List<PlantInstance>(_activePlants);
        }

        /// <summary>
        /// Update all plants
        /// </summary>
        public void UpdateAllPlants(float deltaTime)
        {
            if (!_enableBasicManagement) return;

            foreach (var plant in _activePlants)
            {
                UpdatePlant(plant, deltaTime);
            }
        }

        /// <summary>
        /// Get plant count
        /// </summary>
        public int GetPlantCount()
        {
            return _activePlants.Count;
        }

        /// <summary>
        /// Check if plant exists
        /// </summary>
        public bool PlantExists(string plantId)
        {
            return GetPlantById(plantId) != null;
        }

        /// <summary>
        /// Clear all plants
        /// </summary>
        public void ClearAllPlants()
        {
            var plantsToDestroy = new List<PlantInstance>(_activePlants);
            _activePlants.Clear();

            foreach (var plant in plantsToDestroy)
            {
                OnPlantDestroyed?.Invoke(plant);
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
            }
        }

        /// <summary>
        /// Get plants by growth stage
        /// </summary>
        public List<PlantInstance> GetPlantsByStage(PlantGrowthStage stage)
        {
            return _activePlants.FindAll(p => p.CurrentGrowthStage == stage);
        }

        /// <summary>
        /// Get plants needing care
        /// </summary>
        public List<PlantInstance> GetPlantsNeedingCare()
        {
            return _activePlants.FindAll(p =>
                p.Health < 0.8f ||
                (System.DateTime.Now - p.LastWatering).TotalDays > 1 ||
                (System.DateTime.Now - p.LastFeeding).TotalDays > 3);
        }

        /// <summary>
        /// Get lifecycle statistics
        /// </summary>
        public LifecycleStatistics GetLifecycleStatistics()
        {
            if (_activePlants.Count == 0)
            {
                return new LifecycleStatistics();
            }

            float averageHealth = 0f;
            float averageAge = 0f;
            var stageCounts = new Dictionary<PlantGrowthStage, int>();

            foreach (var plant in _activePlants)
            {
                averageHealth += plant.Health;
                averageAge += plant.AgeInDays;

                if (!stageCounts.ContainsKey(plant.CurrentGrowthStage))
                {
                    stageCounts[plant.CurrentGrowthStage] = 0;
                }
                stageCounts[plant.CurrentGrowthStage]++;
            }

            averageHealth /= _activePlants.Count;
            averageAge /= _activePlants.Count;

            return new LifecycleStatistics
            {
                TotalPlants = _activePlants.Count,
                AverageHealth = averageHealth,
                AverageAge = averageAge,
                PlantsNeedingCare = GetPlantsNeedingCare().Count,
                StageDistribution = stageCounts
            };
        }

        #region Private Methods

        private void UpdatePlant(PlantInstance plant, float deltaTime)
        {
            if (plant == null) return;

            // Basic aging (convert float days to int)
            plant.AgeInDays += (int)(deltaTime / 86400f); // Convert to days

            // Basic health decay if not cared for
            float daysSinceWatering = (float)(System.DateTime.Now - plant.LastWatering).TotalDays;
            float daysSinceFeeding = (float)(System.DateTime.Now - plant.LastFeeding).TotalDays;

            float healthLoss = 0f;
            if (daysSinceWatering > 1) healthLoss += 0.01f * deltaTime;
            if (daysSinceFeeding > 3) healthLoss += 0.005f * deltaTime;

            plant.Health = Mathf.Max(0f, plant.Health - healthLoss);
        }

        #endregion
    }

    /// <summary>
    /// Basic plant instance data for lifecycle tracking
    /// </summary>
    [System.Serializable]
    internal class LifecyclePlantInstance
    {
        public string PlantID;
        public string PlantName;
        public Vector3 WorldPosition;
        public PlantGrowthStage CurrentGrowthStage;
        public float AgeInDays;
        public float Health;
        public System.DateTime LastWatering;
        public System.DateTime LastFeeding;
    }

    /// <summary>
    /// Lifecycle statistics
    /// </summary>
    [System.Serializable]
    public class LifecycleStatistics
    {
        public int TotalPlants;
        public float AverageHealth;
        public float AverageAge;
        public int PlantsNeedingCare;
        public Dictionary<PlantGrowthStage, int> StageDistribution = new Dictionary<PlantGrowthStage, int>();
    }
}
