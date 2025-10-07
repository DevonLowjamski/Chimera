using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using System.Collections.Generic;
using System.Linq;
using Logger = ProjectChimera.Core.Logging.ChimeraLogger;

namespace ProjectChimera.Systems.Gameplay
{
    /// <summary>
    /// Plant Monitor - Handles plant monitoring and visualization.
    /// Tracks plant health, growth stages, and provides visual indicators
    /// for the cultivation mode overlay system.
    /// </summary>
    public class PlantMonitor : MonoBehaviour, ITickable
    {
        [Header("Monitoring Settings")]
        [SerializeField] private float _updateInterval = 2f;
        [SerializeField] private bool _showHealthIndicators = true;
        [SerializeField] private bool _showGrowthIndicators = true;
        [SerializeField] private bool _showHarvestReadiness = true;

        // ITickable implementation
        public int TickPriority => ProjectChimera.Core.Updates.TickPriority.HUD;
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        // Plant tracking
        private List<PlantInfo> _monitoredPlants = new List<PlantInfo>();
        private float _updateTimer = 0f;

        // Visual indicators
        private Dictionary<PlantInfo, GameObject> _healthIndicators = new Dictionary<PlantInfo, GameObject>();
        private Dictionary<PlantInfo, GameObject> _growthIndicators = new Dictionary<PlantInfo, GameObject>();
        private Dictionary<PlantInfo, GameObject> _readinessIndicators = new Dictionary<PlantInfo, GameObject>();

        private void Awake()
        {
            // Register with update system
            UpdateOrchestrator.Instance?.RegisterTickable(this);

            InitializeMonitoring();
        }

        private void OnDestroy()
        {
            // Unregister from update system
            UpdateOrchestrator.Instance?.UnregisterTickable(this);

            CleanupIndicators();
        }

        /// <summary>
        /// Initializes the plant monitoring system
        /// </summary>
        private void InitializeMonitoring()
        {
            RefreshPlantList();
            Logger.Log("OTHER", "Plant monitoring system initialized", this);
        }

        /// <summary>
        /// Updates plant monitoring (called by UpdateOrchestrator)
        /// </summary>
        public void Tick(float deltaTime)
        {
            _updateTimer += deltaTime;

            if (_updateTimer >= _updateInterval)
            {
                _updateTimer = 0f;
                UpdatePlantMonitoring();
            }
        }

        /// <summary>
        /// Updates all plant monitoring data and visual indicators
        /// </summary>
        private void UpdatePlantMonitoring()
        {
            foreach (var plantInfo in _monitoredPlants)
            {
                UpdatePlantInfo(plantInfo);
                UpdatePlantIndicators(plantInfo);
            }
        }

        /// <summary>
        /// Refreshes the list of plants being monitored
        /// </summary>
        public void RefreshPlantList()
        {
            _monitoredPlants.Clear();

            // Find all plants in the scene (simplified approach)
            var plantGameObjects = GameObject.FindGameObjectsWithTag("Plant");

            if (plantGameObjects?.Length > 0)
            {
                Logger.Log("OTHER", "Plants discovered in scene", this);
            }
            else
            {
                Logger.Log("OTHER", "No plants found in scene", this);
            }

            var plantTransforms = plantGameObjects.Select(obj => obj.transform).ToArray();

            foreach (var plantTransform in plantTransforms)
            {
                var plantInfo = CreatePlantInfoFromTransform(plantTransform);
                _monitoredPlants.Add(plantInfo);
            }

            Logger.Log("OTHER", $"Plant list refreshed: {_monitoredPlants.Count} plants found", this);
        }

        /// <summary>
        /// Creates PlantInfo from transform (placeholder implementation)
        /// </summary>
        private PlantInfo CreatePlantInfoFromTransform(Transform plantTransform)
        {
            return new PlantInfo
            {
                PlantID = plantTransform.GetInstanceID().ToString(),
                PlantName = plantTransform.name,
                StrainName = "Unknown",
                CurrentStage = ProjectChimera.Data.Shared.PlantGrowthStage.Vegetative,
                AgeInDays = 30f,
                OverallHealth = Random.Range(0.5f, 1f),
                Position = plantTransform.position,
                CurrentHeight = Random.Range(0.5f, 2f),
                CurrentWidth = Random.Range(0.3f, 1f),
                GrowthProgress = Random.Range(0.3f, 0.8f),
                MaturityLevel = Random.Range(0.2f, 0.9f),
                WaterLevel = Random.Range(0.4f, 1f),
                NutrientLevel = Random.Range(0.4f, 1f),
                LightExposure = 0.8f,
                Temperature = 22f,
                Humidity = 60f,
                StressLevel = Random.Range(0f, 0.5f),
                Vigor = Random.Range(0.6f, 1f),
                HasIssues = false,
                CurrentIssues = new string[0],
                IsHarvestReady = Random.Range(0f, 1f) > 0.8f,
                EstimatedYield = Random.Range(30f, 80f),
                EstimatedHarvestDate = System.DateTime.Now.AddDays(Random.Range(1, 14)),
                QualityRating = Random.Range(0.6f, 1f)
            };
        }

        /// <summary>
        /// Updates information for a specific plant
        /// </summary>
        private void UpdatePlantInfo(PlantInfo plantInfo)
        {
            // Update plant data (would integrate with actual plant systems)
            plantInfo.OverallHealth = Random.Range(0.5f, 1f); // Placeholder
            plantInfo.CurrentStage = ProjectChimera.Data.Shared.PlantGrowthStage.Vegetative; // Placeholder
            // Note: HarvestReadiness and LastUpdate don't exist in current PlantInfo
            // These would need to be added to PlantInfo or handled differently
        }

        /// <summary>
        /// Updates visual indicators for a specific plant
        /// </summary>
        private void UpdatePlantIndicators(PlantInfo plantInfo)
        {
            if (_showHealthIndicators)
            {
                UpdateHealthIndicator(plantInfo);
            }

            if (_showGrowthIndicators)
            {
                UpdateGrowthIndicator(plantInfo);
            }

            if (_showHarvestReadiness)
            {
                UpdateReadinessIndicator(plantInfo);
            }
        }

        /// <summary>
        /// Updates health indicator for a plant
        /// </summary>
        private void UpdateHealthIndicator(PlantInfo plantInfo)
        {
            if (!_healthIndicators.ContainsKey(plantInfo))
            {
                CreateHealthIndicator(plantInfo);
            }

            var indicator = _healthIndicators[plantInfo];
            if (indicator != null)
            {
                // Update indicator based on plant health
                var renderer = indicator.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color healthColor = GetHealthColor(plantInfo.OverallHealth);
                    renderer.material.color = healthColor;
                }

                // Position above plant
                indicator.transform.position = plantInfo.Position + Vector3.up * 2f;
            }
        }

        /// <summary>
        /// Updates growth stage indicator for a plant
        /// </summary>
        private void UpdateGrowthIndicator(PlantInfo plantInfo)
        {
            if (!_growthIndicators.ContainsKey(plantInfo))
            {
                CreateGrowthIndicator(plantInfo);
            }

            var indicator = _growthIndicators[plantInfo];
            if (indicator != null)
            {
                // Update based on growth stage
                var textMesh = indicator.GetComponent<TextMesh>();
                if (textMesh != null)
                {
                    textMesh.text = plantInfo.CurrentStage.ToString();
                }

                // Position above plant
                indicator.transform.position = plantInfo.Position + Vector3.up * 2.5f;
            }
        }

        /// <summary>
        /// Updates harvest readiness indicator for a plant
        /// </summary>
        private void UpdateReadinessIndicator(PlantInfo plantInfo)
        {
            if (!_readinessIndicators.ContainsKey(plantInfo))
            {
                CreateReadinessIndicator(plantInfo);
            }

            var indicator = _readinessIndicators[plantInfo];
            if (indicator != null)
            {
                // Update based on harvest readiness
                var renderer = indicator.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color readinessColor = GetReadinessColor(plantInfo.IsHarvestReady ? 1f : 0f);
                    renderer.material.color = readinessColor;
                }

                // Position above plant
                indicator.transform.position = plantInfo.Position + Vector3.up * 3f;

                // Only show if plant is getting ready for harvest
                indicator.SetActive(plantInfo.IsHarvestReady);
            }
        }

        /// <summary>
        /// Creates a health indicator for a plant
        /// </summary>
        private void CreateHealthIndicator(PlantInfo plantInfo)
        {
            var indicatorObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicatorObj.name = $"HealthIndicator_{plantInfo.PlantName}";
            indicatorObj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

            _healthIndicators[plantInfo] = indicatorObj;
        }

        /// <summary>
        /// Creates a growth indicator for a plant
        /// </summary>
        private void CreateGrowthIndicator(PlantInfo plantInfo)
        {
            var indicatorObj = new GameObject($"GrowthIndicator_{plantInfo.PlantName}");
            var textMesh = indicatorObj.AddComponent<TextMesh>();
            textMesh.fontSize = 12;
            textMesh.color = Color.white;
            textMesh.anchor = TextAnchor.MiddleCenter;

            _growthIndicators[plantInfo] = indicatorObj;
        }

        /// <summary>
        /// Creates a harvest readiness indicator for a plant
        /// </summary>
        private void CreateReadinessIndicator(PlantInfo plantInfo)
        {
            var indicatorObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            indicatorObj.name = $"ReadinessIndicator_{plantInfo.PlantName}";
            indicatorObj.transform.localScale = new Vector3(0.05f, 0.2f, 0.05f);

            _readinessIndicators[plantInfo] = indicatorObj;
        }

        /// <summary>
        /// Gets color based on plant health
        /// </summary>
        private Color GetHealthColor(float health)
        {
            if (health >= 0.8f) return Color.green;
            if (health >= 0.6f) return Color.yellow;
            if (health >= 0.4f) return Color.orange;
            return Color.red;
        }

        /// <summary>
        /// Gets color based on harvest readiness
        /// </summary>
        private Color GetReadinessColor(float readiness)
        {
            if (readiness >= 0.9f) return Color.magenta; // Fully ready
            if (readiness >= 0.7f) return Color.cyan;     // Approaching ready
            return Color.gray;                           // Not ready
        }

        /// <summary>
        /// Cleans up all visual indicators
        /// </summary>
        private void CleanupIndicators()
        {
            foreach (var indicator in _healthIndicators.Values)
            {
                if (indicator != null) Destroy(indicator);
            }

            foreach (var indicator in _growthIndicators.Values)
            {
                if (indicator != null) Destroy(indicator);
            }

            foreach (var indicator in _readinessIndicators.Values)
            {
                if (indicator != null) Destroy(indicator);
            }

            _healthIndicators.Clear();
            _growthIndicators.Clear();
            _readinessIndicators.Clear();
        }

        /// <summary>
        /// Gets the list of monitored plants
        /// </summary>
        public List<PlantInfo> GetMonitoredPlants()
        {
            return new List<PlantInfo>(_monitoredPlants);
        }

        /// <summary>
        /// Gets monitoring statistics
        /// </summary>
        public PlantMonitoringStats GetMonitoringStats()
        {
            return new PlantMonitoringStats
            {
                TotalPlants = _monitoredPlants.Count,
                HealthyPlants = _monitoredPlants.Count(p => p.OverallHealth >= 0.8f),
                PlantsNeedingAttention = _monitoredPlants.Count(p => p.OverallHealth < 0.6f),
                HarvestReadyPlants = _monitoredPlants.Count(p => p.IsHarvestReady),
                LastUpdate = Time.time
            };
        }
    }

    /// <summary>
    /// Plant monitoring statistics
    /// </summary>
    public struct PlantMonitoringStats
    {
        public int TotalPlants;
        public int HealthyPlants;
        public int PlantsNeedingAttention;
        public int HarvestReadyPlants;
        public float LastUpdate;
    }
}
