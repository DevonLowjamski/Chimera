using UnityEngine;
using ProjectChimera.Core.Updates;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core;
using ProjectChimera.Data.Construction;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Integration component that connects UtilityLayerRenderer with BlueprintOverlayRenderer.
    /// Provides unified utility visualization within blueprint overlays for schematic placement.
    /// </summary>
    public class UtilityOverlayIntegration : ChimeraManager, ITickable{
        [Header("Integration Configuration")]
        [SerializeField] private bool _enableUtilityIntegration = true;
        [SerializeField] private bool _showUtilitiesInBlueprintMode = true;
        [SerializeField] private bool _hideUtilitiesWhenNotPlacing = false;
        [SerializeField] private bool _autoValidateConnections = true;

        [Header("Visual Configuration")]
        [SerializeField] private bool _useColorCodedValidation = true;
        [SerializeField] private bool _showCapacityLabels = true;
        [SerializeField] private bool _enableUtilityPreview = true;
        [SerializeField] private float _previewOpacity = 0.7f;

        [Header("Utility Type Visibility")]
        [SerializeField] private bool _showElectrical = true;
        [SerializeField] private bool _showWater = true;
        [SerializeField] private bool _showAir = true;
        [SerializeField] private bool _showData = true;

        [Header("Performance Settings")]
        [SerializeField] private float _updateInterval = 0.2f;
        [SerializeField] private bool _enableAdaptiveDetail = true;
        [SerializeField] private int _maxSimultaneousValidations = 50;

        // System references
        private BlueprintOverlayRenderer _overlayRenderer;
        private UtilityLayerRenderer _utilityRenderer;
        private BlueprintOverlayIntegration _overlayIntegration;

        // Integration state
        private bool _isUtilityVisualizationActive = false;
        private Dictionary<UtilityType, bool> _utilityTypeVisibility = new Dictionary<UtilityType, bool>();
        private List<UtilityValidationResult> _lastValidationResults = new List<UtilityValidationResult>();
        private float _lastUpdateTime = 0f;

        // UI Elements for utility information
        private Dictionary<string, GameObject> _utilityLabels = new Dictionary<string, GameObject>();
        private List<UtilityCapacityIndicator> _capacityIndicators = new List<UtilityCapacityIndicator>();

        // Events
        public System.Action<SchematicSO> OnUtilityVisualizationStarted;
        public System.Action OnUtilityVisualizationStopped;
        public System.Action<List<UtilityValidationResult>> OnUtilityValidationChanged;
        public System.Action<UtilityType, bool> OnUtilityTypeVisibilityChanged;

        public override ManagerPriority Priority => ManagerPriority.Normal;

        // Public Properties
        public bool UtilityIntegrationEnabled => _enableUtilityIntegration;
        public bool IsUtilityVisualizationActive => _isUtilityVisualizationActive;
        public Dictionary<UtilityType, bool> UtilityTypeVisibility => new Dictionary<UtilityType, bool>(_utilityTypeVisibility);
        public List<UtilityValidationResult> LastValidationResults => new List<UtilityValidationResult>(_lastValidationResults);

        protected override void OnManagerInitialize()
        {
            InitializeIntegration();
            SetupUtilityTypeVisibility();
            SetupEventHandlers();

            LogInfo($"UtilityOverlayIntegration initialized - Integration enabled: {_enableUtilityIntegration}");
        }

        public void Tick(float deltaTime)


        {
            if (!_enableUtilityIntegration) return;

            UpdateIntegrationState();
            UpdateUtilityLabels();
            UpdateValidationDisplay();
        }

        /// <summary>
        /// Start utility visualization for a schematic
        /// </summary>
        public void StartUtilityVisualization(SchematicSO schematic, Vector3 position, Quaternion rotation)
        {
            if (!_enableUtilityIntegration || schematic == null)
            {
                LogWarning("Cannot start utility visualization - integration disabled or invalid schematic");
                return;
            }

            if (_utilityRenderer != null)
            {
                _utilityRenderer.CreateUtilityVisualization(schematic, position, rotation);
                _isUtilityVisualizationActive = true;

                // Apply visibility settings
                ApplyUtilityTypeVisibility();

                // Create capacity indicators
                if (_showCapacityLabels)
                {
                    CreateCapacityIndicators(schematic);
                }

                OnUtilityVisualizationStarted?.Invoke(schematic);

                LogInfo($"Started utility visualization for: {schematic.SchematicName}");
            }
        }

        /// <summary>
        /// Stop utility visualization
        /// </summary>
        public void StopUtilityVisualization()
        {
            if (_utilityRenderer != null)
            {
                _utilityRenderer.ClearUtilityVisualization();
                _isUtilityVisualizationActive = false;

                ClearCapacityIndicators();
                ClearUtilityLabels();

                OnUtilityVisualizationStopped?.Invoke();

                LogInfo("Stopped utility visualization");
            }
        }

        /// <summary>
        /// Update utility visualization position
        /// </summary>
        public void UpdateUtilityVisualizationPosition(Vector3 newPosition, Quaternion newRotation)
        {
            if (_isUtilityVisualizationActive && _utilityRenderer != null)
            {
                _utilityRenderer.UpdateUtilityPosition(newPosition, newRotation);
                UpdateCapacityIndicatorPositions();
            }
        }

        /// <summary>
        /// Toggle visibility of specific utility type
        /// </summary>
        public void SetUtilityTypeVisible(UtilityType utilityType, bool visible)
        {
            if (_utilityTypeVisibility.ContainsKey(utilityType))
            {
                _utilityTypeVisibility[utilityType] = visible;

                if (_utilityRenderer != null)
                {
                    _utilityRenderer.SetUtilityTypeVisible(utilityType, visible);
                }

                UpdateCapacityIndicatorVisibility(utilityType, visible);

                OnUtilityTypeVisibilityChanged?.Invoke(utilityType, visible);

                LogInfo($"Set {utilityType} utility visibility: {visible}");
            }
        }

        /// <summary>
        /// Get utility information at world position
        /// </summary>
        public UtilityPositionInfo GetUtilityInfoAtPosition(Vector3 worldPosition, float radius = 1f)
        {
            if (_utilityRenderer == null || !_isUtilityVisualizationActive)
            {
                return new UtilityPositionInfo { HasUtilities = false };
            }

            var connections = _utilityRenderer.GetConnectionInfoAtPosition(worldPosition, radius);

            return new UtilityPositionInfo
            {
                HasUtilities = connections.Count > 0,
                Position = worldPosition,
                Connections = connections,
                UtilityTypes = connections.Select(c => c.UtilityType).Distinct().ToList(),
                TotalCapacity = connections.Sum(c => c.Capacity),
                TotalFlow = connections.Sum(c => c.CurrentFlow)
            };
        }

        /// <summary>
        /// Validate current utility layout
        /// </summary>
        public List<UtilityValidationResult> ValidateCurrentLayout()
        {
            if (_utilityRenderer != null && _isUtilityVisualizationActive)
            {
                _lastValidationResults = _utilityRenderer.ValidateUtilityLayout();
                OnUtilityValidationChanged?.Invoke(_lastValidationResults);
                return _lastValidationResults;
            }

            return new List<UtilityValidationResult>();
        }

        /// <summary>
        /// Get comprehensive utility summary
        /// </summary>
        public UtilitySummary GetUtilitySummary()
        {
            if (_utilityRenderer == null || !_isUtilityVisualizationActive)
            {
                return new UtilitySummary { IsActive = false };
            }

            var nodeCounts = _utilityRenderer.UtilityNodeCounts;
            var validationResults = _lastValidationResults;

            return new UtilitySummary
            {
                IsActive = true,
                TotalConnections = _utilityRenderer.ActiveConnectionCount,
                UtilityTypeCounts = nodeCounts,
                ValidationResults = validationResults,
                OverallValid = validationResults.All(r => r.IsValid),
                TotalCapacity = validationResults.Sum(r => r.TotalCapacity),
                TotalFlow = validationResults.Sum(r => r.TotalFlow)
            };
        }

        private void InitializeIntegration()
        {
            _overlayRenderer = ServiceContainerFactory.Instance?.TryResolve<BlueprintOverlayRenderer>();
            _utilityRenderer = ServiceContainerFactory.Instance?.TryResolve<UtilityLayerRenderer>();
            _overlayIntegration = ServiceContainerFactory.Instance?.TryResolve<BlueprintOverlayIntegration>();

            if (_overlayRenderer == null)
            {
                LogWarning("BlueprintOverlayRenderer not found - utility overlay integration limited");
            }

            if (_utilityRenderer == null)
            {
                LogError("UtilityLayerRenderer not found - utility integration disabled");
                _enableUtilityIntegration = false;
            }
        }

        private void SetupUtilityTypeVisibility()
        {
            _utilityTypeVisibility[UtilityType.Electrical] = _showElectrical;
            _utilityTypeVisibility[UtilityType.Water] = _showWater;
            _utilityTypeVisibility[UtilityType.Air] = _showAir;
            _utilityTypeVisibility[UtilityType.Data] = _showData;
        }

        private void SetupEventHandlers()
        {
            if (_overlayIntegration != null)
            {
                _overlayIntegration.OnSchematicOverlayCreated += OnSchematicOverlayCreated;
                _overlayIntegration.OnOverlayMoved += OnOverlayMoved;
            }
        }

        private void OnSchematicOverlayCreated(SchematicSO schematic)
        {
            if (_showUtilitiesInBlueprintMode && _overlayIntegration != null)
            {
                var overlayPos = _overlayIntegration.CurrentOverlay?.Position ?? Vector3.zero;
                var overlayRot = _overlayIntegration.CurrentOverlay?.Rotation ?? Quaternion.identity;

                StartUtilityVisualization(schematic, overlayPos, overlayRot);
            }
        }

        private void OnOverlayMoved(Vector3 newPosition, Quaternion newRotation)
        {
            if (_isUtilityVisualizationActive)
            {
                UpdateUtilityVisualizationPosition(newPosition, newRotation);
            }
        }

        private void UpdateIntegrationState()
        {
            if (Time.time - _lastUpdateTime < _updateInterval) return;

            _lastUpdateTime = Time.time;

            // Monitor overlay integration state
            if (_overlayIntegration != null)
            {
                bool shouldShowUtilities = _overlayIntegration.HasActiveOverlay && _showUtilitiesInBlueprintMode;

                if (shouldShowUtilities && !_isUtilityVisualizationActive)
                {
                    // Auto-start utility visualization when overlay becomes active
                    // This is a simplified approach - full implementation would be more sophisticated
                }
                else if (!shouldShowUtilities && _isUtilityVisualizationActive && _hideUtilitiesWhenNotPlacing)
                {
                    StopUtilityVisualization();
                }
            }

            // Auto-validate if enabled
            if (_autoValidateConnections && _isUtilityVisualizationActive)
            {
                ValidateCurrentLayout();
            }
        }

        private void ApplyUtilityTypeVisibility()
        {
            foreach (var kvp in _utilityTypeVisibility)
            {
                if (_utilityRenderer != null)
                {
                    _utilityRenderer.SetUtilityTypeVisible(kvp.Key, kvp.Value);
                }
            }
        }

        private void CreateCapacityIndicators(SchematicSO schematic)
        {
            if (_utilityRenderer == null) return;

            var nodeCounts = _utilityRenderer.UtilityNodeCounts;

            foreach (var kvp in nodeCounts)
            {
                if (kvp.Value > 0)
                {
                    var indicator = new UtilityCapacityIndicator
                    {
                        UtilityType = kvp.Key,
                        NodeCount = kvp.Value,
                        TotalCapacity = GetTotalCapacityForType(kvp.Key),
                        IndicatorObject = CreateCapacityIndicatorVisual(kvp.Key, kvp.Value)
                    };

                    _capacityIndicators.Add(indicator);
                }
            }
        }

        private GameObject CreateCapacityIndicatorVisual(UtilityType utilityType, int nodeCount)
        {
            var indicatorGO = new GameObject($"CapacityIndicator_{utilityType}");

            // Add UI text component for capacity display
            var canvas = indicatorGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;

            var textMesh = indicatorGO.AddComponent<TextMesh>();
            textMesh.text = $"{utilityType}: {nodeCount} nodes";
            textMesh.fontSize = 10;
            textMesh.color = GetUtilityColor(utilityType);
            textMesh.anchor = TextAnchor.MiddleCenter;

            return indicatorGO;
        }

        private Color GetUtilityColor(UtilityType utilityType)
        {
            return utilityType switch
            {
                UtilityType.Electrical => Color.yellow,
                UtilityType.Water => Color.blue,
                UtilityType.Air => Color.cyan,
                UtilityType.Data => Color.green,
                _ => Color.white
            };
        }

        private float GetTotalCapacityForType(UtilityType utilityType)
        {
            var result = _lastValidationResults.FirstOrDefault(r => r.UtilityType == utilityType);
            return result?.TotalCapacity ?? 0f;
        }

        private void UpdateUtilityLabels()
        {
            if (!_showCapacityLabels) return;

            // Update label positions and content based on current utility state
            foreach (var indicator in _capacityIndicators)
            {
                if (indicator.IndicatorObject != null)
                {
                    var textMesh = indicator.IndicatorObject.GetComponent<TextMesh>();
                    if (textMesh != null)
                    {
                        var capacity = GetTotalCapacityForType(indicator.UtilityType);
                        textMesh.text = $"{indicator.UtilityType}: {capacity:F1} capacity";
                    }
                }
            }
        }

        private void UpdateValidationDisplay()
        {
            if (!_useColorCodedValidation) return;

            foreach (var result in _lastValidationResults)
            {
                UpdateValidationColorForType(result.UtilityType, result.IsValid);
            }
        }

        private void UpdateValidationColorForType(UtilityType utilityType, bool isValid)
        {
            var indicator = _capacityIndicators.FirstOrDefault(i => i.UtilityType == utilityType);
            if (indicator?.IndicatorObject != null)
            {
                var textMesh = indicator.IndicatorObject.GetComponent<TextMesh>();
                if (textMesh != null)
                {
                    textMesh.color = isValid ? GetUtilityColor(utilityType) : Color.red;
                }
            }
        }

        private void UpdateCapacityIndicatorPositions()
        {
            // Update indicator positions when overlay moves
            for (int i = 0; i < _capacityIndicators.Count; i++)
            {
                var indicator = _capacityIndicators[i];
                if (indicator.IndicatorObject != null)
                {
                    // Position indicators in a grid above the overlay
                    Vector3 basePosition = Vector3.up * 3f; // Above the overlay
                    Vector3 offset = new Vector3((i % 2) * 2f - 1f, 0f, (i / 2) * 1f);
                    indicator.IndicatorObject.transform.position = basePosition + offset;
                }
            }
        }

        private void UpdateCapacityIndicatorVisibility(UtilityType utilityType, bool visible)
        {
            var indicator = _capacityIndicators.FirstOrDefault(i => i.UtilityType == utilityType);
            if (indicator?.IndicatorObject != null)
            {
                indicator.IndicatorObject.SetActive(visible && _showCapacityLabels);
            }
        }

        private void ClearCapacityIndicators()
        {
            foreach (var indicator in _capacityIndicators)
            {
                if (indicator.IndicatorObject != null)
                {
                    DestroyImmediate(indicator.IndicatorObject);
                }
            }
            _capacityIndicators.Clear();
        }

        private void ClearUtilityLabels()
        {
            foreach (var label in _utilityLabels.Values)
            {
                if (label != null)
                {
                    DestroyImmediate(label);
                }
            }
            _utilityLabels.Clear();
        }

        protected override void OnManagerShutdown()
        {
            StopUtilityVisualization();

            if (_overlayIntegration != null)
            {
                _overlayIntegration.OnSchematicOverlayCreated -= OnSchematicOverlayCreated;
                _overlayIntegration.OnOverlayMoved -= OnOverlayMoved;
            }

            LogInfo("UtilityOverlayIntegration shutdown");
        }

        protected override void Start()
        {
            base.Start();
            // Register with UpdateOrchestrator
            UpdateOrchestrator.Instance?.RegisterTickable(this);
        }

        protected override void OnDestroy()
        {
            // Unregister from UpdateOrchestrator
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
            base.OnDestroy();
        }

        #region ITickable Implementation

        int ITickable.Priority => 0;
        public bool Enabled => enabled && gameObject.activeInHierarchy;

        public virtual void OnRegistered()
        {
            // Override in derived classes if needed
        }

        public virtual void OnUnregistered()
        {
            // Override in derived classes if needed
        }



        #endregion
    }

    /// <summary>
    /// Information about utilities at a specific position
    /// </summary>
    [System.Serializable]
    public class UtilityPositionInfo
    {
        public bool HasUtilities;
        public Vector3 Position;
        public List<UtilityConnectionInfo> Connections = new List<UtilityConnectionInfo>();
        public List<UtilityType> UtilityTypes = new List<UtilityType>();
        public float TotalCapacity;
        public float TotalFlow;
    }

    /// <summary>
    /// Summary of all utility systems
    /// </summary>
    [System.Serializable]
    public class UtilitySummary
    {
        public bool IsActive;
        public int TotalConnections;
        public Dictionary<UtilityType, int> UtilityTypeCounts = new Dictionary<UtilityType, int>();
        public List<UtilityValidationResult> ValidationResults = new List<UtilityValidationResult>();
        public bool OverallValid;
        public float TotalCapacity;
        public float TotalFlow;
    }

    /// <summary>
    /// Capacity indicator for a utility type
    /// </summary>
    [System.Serializable]
    public class UtilityCapacityIndicator
    {
        public UtilityType UtilityType;
        public int NodeCount;
        public float TotalCapacity;
        public GameObject IndicatorObject;
    }
}
