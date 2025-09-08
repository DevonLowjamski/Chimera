using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using ProjectChimera.Core.Updates;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Renders 3D health bars, status indicators, and vital information for cannabis plants and facilities.
    /// Utilizes Unity 6.2's enhanced World Space UI capabilities for immersive cultivation management.
    /// </summary>
    public class WorldSpaceStatusRenderer : MonoBehaviour, ITickable
    {
        [Header("Status Display Configuration")]
        [SerializeField] private WorldSpaceStatusConfig _config = new WorldSpaceStatusConfig();
        [SerializeField] private Camera _targetCamera;
        
        [Header("Status Indicator Templates")]
        [SerializeField] private VisualTreeAsset _plantHealthTemplate;
        [SerializeField] private VisualTreeAsset _facilityStatusTemplate;
        [SerializeField] private VisualTreeAsset _equipmentStatusTemplate;
        
        [Header("Positioning & Visibility")]
        [SerializeField] private float _statusHeightOffset = 2.0f;
        [SerializeField] private float _maxDisplayDistance = 15.0f;
        [SerializeField] private LayerMask _obstacleLayer = 1 << 0;
        [SerializeField] private bool _enableOcclusionCulling = true;
        
        // Active status displays
        private readonly Dictionary<GameObject, StatusDisplayData> _activeDisplays = new Dictionary<GameObject, StatusDisplayData>();
        private readonly Queue<UIDocument> _displayPool = new Queue<UIDocument>();
        
        // Update optimization
        private int _currentUpdateIndex = 0;
        private const int MaxUpdatesPerFrame = 10;
        
        public WorldSpaceStatusConfig Config => _config;
        public int ActiveDisplayCount => _activeDisplays.Count;
        
        private void Awake()
        {
            if (_targetCamera == null)
                _targetCamera = Camera.main;
            
            InitializeDisplayPool();
        }
        
        private void Start()
        {
            // Register with UpdateOrchestrator
            UpdateOrchestrator.Instance.RegisterTickable(this);
        }
        
        #region ITickable Implementation
        
        public int Priority => TickPriority.UIManager;
        public bool Enabled => enabled && _targetCamera != null;
        
        public void Tick(float deltaTime)
        {
            UpdateStatusDisplays();
        }
        
        #endregion
        
        /// <summary>
        /// Initializes the UI document pool for performance
        /// </summary>
        private void InitializeDisplayPool()
        {
            for (int i = 0; i < _config.poolSize; i++)
            {
                var statusDocument = CreateStatusDocument();
                statusDocument.gameObject.SetActive(false);
                _displayPool.Enqueue(statusDocument);
            }
        }
        
        /// <summary>
        /// Creates a new status display UI document
        /// </summary>
        private UIDocument CreateStatusDocument()
        {
            var statusObject = new GameObject("StatusDisplay");
            statusObject.transform.SetParent(transform);
            
            var uiDocument = statusObject.AddComponent<UIDocument>();
            var canvasGroup = statusObject.AddComponent<CanvasGroup>();
            
            // Configure for world space rendering
            uiDocument.panelSettings = _config.panelSettings;
            uiDocument.sortingOrder = _config.sortingOrder;
            
            return uiDocument;
        }
        
        /// <summary>
        /// Shows status indicators for a target object
        /// </summary>
        public bool ShowStatusDisplay(GameObject target, StatusDisplayType displayType, StatusData statusData)
        {
            if (target == null || statusData == null)
            {
                ChimeraLogger.LogWarning("[WorldSpaceStatusRenderer] Invalid parameters for status display");
                return false;
            }
            
            // Update existing display or create new one
            if (_activeDisplays.TryGetValue(target, out var existingDisplay))
            {
                UpdateStatusData(existingDisplay, statusData);
                return true;
            }
            
            // Get display from pool
            var statusDocument = GetStatusDocument();
            if (statusDocument == null)
            {
                ChimeraLogger.LogWarning("[WorldSpaceStatusRenderer] No available status documents in pool");
                return false;
            }
            
            // Configure status display
            if (!ConfigureStatusDisplay(statusDocument, target, displayType, statusData))
            {
                ReturnStatusDocument(statusDocument);
                return false;
            }
            
            // Create and register display data
            var displayData = new StatusDisplayData
            {
                Target = target,
                DisplayType = displayType,
                UIDocument = statusDocument,
                StatusData = statusData,
                LastUpdateTime = Time.time,
                CreationTime = Time.time
            };
            
            _activeDisplays[target] = displayData;
            
            // Position and show display
            UpdateDisplayPosition(displayData);
            statusDocument.gameObject.SetActive(true);
            
            ChimeraLogger.Log($"[WorldSpaceStatusRenderer] Created status display for {target.name}");
            return true;
        }
        
        /// <summary>
        /// Hides status display for a target object
        /// </summary>
        public bool HideStatusDisplay(GameObject target)
        {
            if (!_activeDisplays.TryGetValue(target, out var displayData))
                return false;
            
            displayData.UIDocument.gameObject.SetActive(false);
            ReturnStatusDocument(displayData.UIDocument);
            _activeDisplays.Remove(target);
            
            ChimeraLogger.Log($"[WorldSpaceStatusRenderer] Removed status display for {target.name}");
            return true;
        }
        
        /// <summary>
        /// Updates status data for an existing display
        /// </summary>
        public void UpdateStatusData(GameObject target, StatusData newStatusData)
        {
            if (_activeDisplays.TryGetValue(target, out var displayData))
            {
                UpdateStatusData(displayData, newStatusData);
            }
        }
        
        /// <summary>
        /// Updates status data for a display
        /// </summary>
        private void UpdateStatusData(StatusDisplayData displayData, StatusData newStatusData)
        {
            displayData.StatusData = newStatusData;
            displayData.LastUpdateTime = Time.time;
            
            var rootElement = displayData.UIDocument.rootVisualElement;
            PopulateStatusElements(rootElement, displayData.DisplayType, newStatusData);
        }
        
        /// <summary>
        /// Configures a status display with template and data
        /// </summary>
        private bool ConfigureStatusDisplay(UIDocument statusDocument, GameObject target, StatusDisplayType displayType, StatusData statusData)
        {
            var template = GetTemplateForDisplayType(displayType);
            if (template == null)
            {
                ChimeraLogger.LogWarning($"[WorldSpaceStatusRenderer] No template found for display type: {displayType}");
                return false;
            }
            
            statusDocument.visualTreeAsset = template;
            statusDocument.gameObject.SetActive(true);
            
            var rootElement = statusDocument.rootVisualElement;
            PopulateStatusElements(rootElement, displayType, statusData);
            
            return true;
        }
        
        /// <summary>
        /// Populates status UI elements with data
        /// </summary>
        private void PopulateStatusElements(VisualElement rootElement, StatusDisplayType displayType, StatusData statusData)
        {
            switch (displayType)
            {
                case StatusDisplayType.PlantHealth:
                    PopulatePlantHealthElements(rootElement, statusData);
                    break;
                case StatusDisplayType.FacilityStatus:
                    PopulateFacilityStatusElements(rootElement, statusData);
                    break;
                case StatusDisplayType.EquipmentStatus:
                    PopulateEquipmentStatusElements(rootElement, statusData);
                    break;
            }
        }
        
        /// <summary>
        /// Populates plant health display elements
        /// </summary>
        private void PopulatePlantHealthElements(VisualElement rootElement, StatusData statusData)
        {
            // Health bar
            var healthBar = rootElement.Q<ProgressBar>("health-bar");
            if (healthBar != null)
            {
                healthBar.value = statusData.Health;
                healthBar.title = $"Health: {statusData.Health:F1}%";
            }
            
            // Growth stage indicator
            var stageLabel = rootElement.Q<Label>("growth-stage");
            if (stageLabel != null)
            {
                stageLabel.text = statusData.GrowthStage ?? "Unknown";
            }
            
            // Stress indicators
            var stressContainer = rootElement.Q<VisualElement>("stress-indicators");
            if (stressContainer != null)
            {
                stressContainer.Clear();
                foreach (var stressor in statusData.StressFactors ?? new List<string>())
                {
                    var stressIcon = new Label(stressor);
                    stressIcon.AddToClassList("stress-indicator");
                    stressContainer.Add(stressIcon);
                }
            }
            
            // Days until harvest
            var harvestLabel = rootElement.Q<Label>("harvest-countdown");
            if (harvestLabel != null && statusData.DaysToHarvest.HasValue)
            {
                harvestLabel.text = $"{statusData.DaysToHarvest.Value}d";
                harvestLabel.style.display = statusData.DaysToHarvest.Value > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        
        /// <summary>
        /// Populates facility status display elements
        /// </summary>
        private void PopulateFacilityStatusElements(VisualElement rootElement, StatusData statusData)
        {
            // Power status
            var powerBar = rootElement.Q<ProgressBar>("power-bar");
            if (powerBar != null && statusData.PowerLevel.HasValue)
            {
                powerBar.value = statusData.PowerLevel.Value;
                powerBar.title = $"Power: {statusData.PowerLevel.Value:F1}%";
            }
            
            // Temperature display
            var tempLabel = rootElement.Q<Label>("temperature");
            if (tempLabel != null && statusData.Temperature.HasValue)
            {
                tempLabel.text = $"{statusData.Temperature.Value:F1}°C";
            }
            
            // Humidity display
            var humidityLabel = rootElement.Q<Label>("humidity");
            if (humidityLabel != null && statusData.Humidity.HasValue)
            {
                humidityLabel.text = $"{statusData.Humidity.Value:F0}%";
            }
        }
        
        /// <summary>
        /// Populates equipment status display elements
        /// </summary>
        private void PopulateEquipmentStatusElements(VisualElement rootElement, StatusData statusData)
        {
            // Operating status
            var statusIcon = rootElement.Q<VisualElement>("status-icon");
            if (statusIcon != null)
            {
                statusIcon.ClearClassList();
                statusIcon.AddToClassList("status-icon");
                var statusClass = statusData.OperationalStatus != null ? statusData.OperationalStatus.ToLower() : "unknown";
                statusIcon.AddToClassList($"status-{statusClass}");
            }
            
            // Efficiency meter
            var efficiencyBar = rootElement.Q<ProgressBar>("efficiency-bar");
            if (efficiencyBar != null && statusData.Efficiency.HasValue)
            {
                efficiencyBar.value = statusData.Efficiency.Value;
                efficiencyBar.title = $"Efficiency: {statusData.Efficiency.Value:F1}%";
            }
        }
        
        /// <summary>
        /// Gets the appropriate template for a status display type
        /// </summary>
        private VisualTreeAsset GetTemplateForDisplayType(StatusDisplayType displayType)
        {
            return displayType switch
            {
                StatusDisplayType.PlantHealth => _plantHealthTemplate,
                StatusDisplayType.FacilityStatus => _facilityStatusTemplate,
                StatusDisplayType.EquipmentStatus => _equipmentStatusTemplate,
                _ => _plantHealthTemplate
            };
        }
        
        /// <summary>
        /// Updates all active status displays (optimized for performance)
        /// </summary>
        private void UpdateStatusDisplays()
        {
            if (_activeDisplays.Count == 0 || _targetCamera == null) return;
            
            var displayKeys = new List<GameObject>(_activeDisplays.Keys);
            int displaysToUpdate = Mathf.Min(MaxUpdatesPerFrame, displayKeys.Count);
            
            for (int i = 0; i < displaysToUpdate; i++)
            {
                var targetIndex = (_currentUpdateIndex + i) % displayKeys.Count;
                var target = displayKeys[targetIndex];
                
                if (target == null)
                {
                    _activeDisplays.Remove(target);
                    continue;
                }
                
                if (_activeDisplays.TryGetValue(target, out var displayData))
                {
                    UpdateDisplayPosition(displayData);
                    UpdateDisplayVisibility(displayData);
                }
            }
            
            _currentUpdateIndex = (_currentUpdateIndex + displaysToUpdate) % Mathf.Max(1, displayKeys.Count);
        }
        
        /// <summary>
        /// Updates the position of a status display
        /// </summary>
        private void UpdateDisplayPosition(StatusDisplayData displayData)
        {
            if (displayData.Target == null || displayData.UIDocument == null) return;
            
            var targetPosition = displayData.Target.transform.position;
            var displayPosition = targetPosition + Vector3.up * _statusHeightOffset;
            
            displayData.UIDocument.transform.position = displayPosition;
            
            // Billboard behavior
            if (_config.billboardMode && _targetCamera != null)
            {
                var cameraPosition = _targetCamera.transform.position;
                var lookDirection = (cameraPosition - displayPosition).normalized;
                displayData.UIDocument.transform.rotation = Quaternion.LookRotation(lookDirection);
            }
            
            // Distance-based scaling
            if (_config.adaptiveScaling && _targetCamera != null)
            {
                var distance = Vector3.Distance(displayPosition, _targetCamera.transform.position);
                var scaleFactor = _config.distanceScaleCurve.Evaluate(distance) * _config.statusScale;
                displayData.UIDocument.transform.localScale = Vector3.one * scaleFactor;
            }
        }
        
        /// <summary>
        /// Updates the visibility of a status display
        /// </summary>
        private void UpdateDisplayVisibility(StatusDisplayData displayData)
        {
            if (displayData.Target == null || displayData.UIDocument == null || _targetCamera == null) return;
            
            var canvasGroup = displayData.UIDocument.GetComponent<CanvasGroup>();
            if (canvasGroup == null) return;
            
            var targetPosition = displayData.Target.transform.position;
            var cameraPosition = _targetCamera.transform.position;
            var distance = Vector3.Distance(targetPosition, cameraPosition);
            
            float alpha = 1f;
            
            // Distance fade
            if (distance > _maxDisplayDistance)
            {
                alpha = 0f;
            }
            else if (distance > _maxDisplayDistance * 0.8f)
            {
                var fadeRange = _maxDisplayDistance * 0.2f;
                var fadeDistance = distance - (_maxDisplayDistance * 0.8f);
                alpha = Mathf.Lerp(1f, 0f, fadeDistance / fadeRange);
            }
            
            // Occlusion check
            if (alpha > 0f && _enableOcclusionCulling)
            {
                var direction = (targetPosition - cameraPosition).normalized;
                if (Physics.Raycast(cameraPosition, direction, distance, _obstacleLayer))
                {
                    alpha *= 0.3f; // Reduce visibility when occluded
                }
            }
            
            canvasGroup.alpha = alpha;
        }
        
        /// <summary>
        /// Gets a status document from the pool
        /// </summary>
        private UIDocument GetStatusDocument()
        {
            if (_displayPool.Count > 0)
            {
                return _displayPool.Dequeue();
            }
            
            // Create new document if pool is empty
            return CreateStatusDocument();
        }
        
        /// <summary>
        /// Returns a status document to the pool
        /// </summary>
        private void ReturnStatusDocument(UIDocument statusDocument)
        {
            if (statusDocument != null)
            {
                statusDocument.gameObject.SetActive(false);
                statusDocument.visualTreeAsset = null;
                _displayPool.Enqueue(statusDocument);
            }
        }
        
        /// <summary>
        /// Hides all status displays
        /// </summary>
        public void HideAllDisplays()
        {
            var targets = new List<GameObject>(_activeDisplays.Keys);
            foreach (var target in targets)
            {
                HideStatusDisplay(target);
            }
        }
        
        /// <summary>
        /// Gets all active status displays
        /// </summary>
        public Dictionary<GameObject, StatusDisplayData> GetActiveDisplays()
        {
            return new Dictionary<GameObject, StatusDisplayData>(_activeDisplays);
        }
        
        /// <summary>
        /// Checks if a target has an active status display
        /// </summary>
        public bool HasStatusDisplay(GameObject target)
        {
            return target != null && _activeDisplays.ContainsKey(target);
        }
        
        private void OnDestroy()
        {
            if (UpdateOrchestrator.Instance != null)
            {
                UpdateOrchestrator.Instance.UnregisterTickable(this);
            }
            HideAllDisplays();
        }
    }
}