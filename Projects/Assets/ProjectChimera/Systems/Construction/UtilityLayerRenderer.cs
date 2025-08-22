using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core;
using ProjectChimera.Data.Construction;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Advanced utility layer visualization system for blueprint overlays.
    /// Renders utility connections, flow directions, capacity indicators, and connection validation.
    /// </summary>
    public class UtilityLayerRenderer : ChimeraManager
    {
        [Header("Utility Visualization Configuration")]
        [SerializeField] private bool _enableUtilityVisualization = true;
        [SerializeField] private bool _showAllUtilityTypes = true;
        [SerializeField] private bool _showFlowDirections = true;
        [SerializeField] private bool _showCapacityIndicators = true;
        [SerializeField] private bool _showConnectionValidation = true;
        
        [Header("Rendering Settings")]
        [SerializeField] private LayerMask _utilityLayer = 31; // Utility visualization layer
        [SerializeField] private Material _connectionLineMaterial;
        [SerializeField] private Material _flowIndicatorMaterial;
        [SerializeField] private Material _capacityIndicatorMaterial;
        
        [Header("Visual Properties")]
        [SerializeField] private float _connectionLineWidth = 0.1f;
        [SerializeField] private float _flowArrowSize = 0.3f;
        [SerializeField] private float _capacityIndicatorSize = 0.5f;
        [SerializeField] private Color _electricalColor = Color.yellow;
        [SerializeField] private Color _waterColor = Color.blue;
        [SerializeField] private Color _airColor = Color.cyan;
        [SerializeField] private Color _dataColor = Color.green;
        [SerializeField] private Color _validConnectionColor = Color.green;
        [SerializeField] private Color _invalidConnectionColor = Color.red;
        
        [Header("Animation Settings")]
        [SerializeField] private bool _enableFlowAnimation = true;
        [SerializeField] private float _flowAnimationSpeed = 2f;
        [SerializeField] private bool _enablePulseIndicators = true;
        [SerializeField] private float _pulseSpeed = 1.5f;
        [SerializeField] private float _pulseIntensity = 0.4f;
        
        [Header("Performance Settings")]
        [SerializeField] private bool _enableLOD = true;
        [SerializeField] private float _maxUtilityRenderDistance = 25f;
        [SerializeField] private int _maxVisibleConnections = 200;
        [SerializeField] private bool _enableConnectionCulling = true;
        
        // System references
        private BlueprintOverlayRenderer _overlayRenderer;
        private Camera _utilityCamera;
        
        // Utility tracking
        private Dictionary<string, UtilityConnection> _activeConnections = new Dictionary<string, UtilityConnection>();
        private Dictionary<UtilityType, List<UtilityNode>> _utilityNodes = new Dictionary<UtilityType, List<UtilityNode>>();
        private List<UtilityFlowIndicator> _flowIndicators = new List<UtilityFlowIndicator>();
        private Queue<UtilityConnection> _connectionPool = new Queue<UtilityConnection>();
        
        // Rendering state
        private LineRenderer[] _connectionLines;
        private Dictionary<UtilityType, Color> _utilityColors = new Dictionary<UtilityType, Color>();
        private float _animationTime = 0f;
        
        // Events
        public System.Action<UtilityConnection> OnConnectionCreated;
        public System.Action<UtilityConnection> OnConnectionRemoved;
        public System.Action<UtilityType, bool> OnUtilityTypeToggled;
        public System.Action<List<UtilityValidationResult>> OnValidationUpdated;
        
        public override ManagerPriority Priority => ManagerPriority.Normal;
        
        // Public Properties
        public bool UtilityVisualizationEnabled => _enableUtilityVisualization;
        public int ActiveConnectionCount => _activeConnections.Count;
        public Dictionary<UtilityType, int> UtilityNodeCounts => _utilityNodes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);
        public Camera UtilityCamera => _utilityCamera;
        
        protected override void OnManagerInitialize()
        {
            InitializeUtilitySystem();
            SetupUtilityCamera();
            CreateUtilityMaterials();
            InitializeUtilityColors();
            SetupConnectionPooling();
            
            LogInfo($"UtilityLayerRenderer initialized - Max connections: {_maxVisibleConnections}");
        }
        
        private void Update()
        {
            if (!_enableUtilityVisualization) return;
            
            UpdateAnimations();
            UpdateFlowIndicators();
            UpdateLOD();
            UpdateValidation();
        }
        
        /// <summary>
        /// Create utility visualization for a schematic
        /// </summary>
        public void CreateUtilityVisualization(SchematicSO schematic, Vector3 position, Quaternion rotation)
        {
            if (!_enableUtilityVisualization || schematic == null)
            {
                LogWarning("Cannot create utility visualization - disabled or invalid schematic");
                return;
            }
            
            ClearUtilityVisualization();
            
            // Analyze schematic for utility requirements
            var utilityData = AnalyzeSchematicUtilities(schematic);
            
            // Create utility nodes for each item
            foreach (var item in schematic.Items)
            {
                CreateUtilityNodesForItem(item, position, rotation);
            }
            
            // Create connections between nodes
            GenerateUtilityConnections(utilityData);
            
            // Create flow indicators
            GenerateFlowIndicators();
            
            // Validate connections
            ValidateUtilityLayout();
            
            LogInfo($"Created utility visualization for '{schematic.SchematicName}' with {_activeConnections.Count} connections");
        }
        
        /// <summary>
        /// Update utility visualization position
        /// </summary>
        public void UpdateUtilityPosition(Vector3 newPosition, Quaternion newRotation)
        {
            foreach (var node in _utilityNodes.Values.SelectMany(nodes => nodes))
            {
                // Update node positions relative to new schematic position
                node.UpdatePosition(newPosition, newRotation);
            }
            
            // Update connection line positions
            UpdateConnectionVisuals();
        }
        
        /// <summary>
        /// Toggle visibility of specific utility type
        /// </summary>
        public void SetUtilityTypeVisible(UtilityType utilityType, bool visible)
        {
            if (_utilityNodes.ContainsKey(utilityType))
            {
                foreach (var node in _utilityNodes[utilityType])
                {
                    node.SetVisible(visible);
                }
                
                // Update connections of this type
                UpdateConnectionsForUtilityType(utilityType, visible);
                
                OnUtilityTypeToggled?.Invoke(utilityType, visible);
            }
        }
        
        /// <summary>
        /// Get utility validation results for current layout
        /// </summary>
        public List<UtilityValidationResult> ValidateUtilityLayout()
        {
            var results = new List<UtilityValidationResult>();
            
            foreach (var utilityType in _utilityNodes.Keys)
            {
                var validationResult = ValidateUtilityType(utilityType);
                results.Add(validationResult);
            }
            
            OnValidationUpdated?.Invoke(results);
            return results;
        }
        
        /// <summary>
        /// Clear all utility visualizations
        /// </summary>
        public void ClearUtilityVisualization()
        {
            // Clear connections
            foreach (var connection in _activeConnections.Values)
            {
                ReturnConnectionToPool(connection);
            }
            _activeConnections.Clear();
            
            // Clear nodes
            foreach (var nodeList in _utilityNodes.Values)
            {
                foreach (var node in nodeList)
                {
                    if (node.VisualObject != null)
                        DestroyImmediate(node.VisualObject);
                }
                nodeList.Clear();
            }
            
            // Clear flow indicators
            foreach (var indicator in _flowIndicators)
            {
                if (indicator.VisualObject != null)
                    DestroyImmediate(indicator.VisualObject);
            }
            _flowIndicators.Clear();
            
            LogInfo("Cleared all utility visualizations");
        }
        
        /// <summary>
        /// Get utility connection info at position
        /// </summary>
        public List<UtilityConnectionInfo> GetConnectionInfoAtPosition(Vector3 worldPosition, float radius = 1f)
        {
            var connections = new List<UtilityConnectionInfo>();
            
            foreach (var connection in _activeConnections.Values)
            {
                if (IsConnectionNearPosition(connection, worldPosition, radius))
                {
                    connections.Add(new UtilityConnectionInfo
                    {
                        ConnectionId = connection.ConnectionId,
                        UtilityType = connection.UtilityType,
                        FromNode = connection.FromNode,
                        ToNode = connection.ToNode,
                        Capacity = connection.Capacity,
                        CurrentFlow = connection.CurrentFlow,
                        IsValid = connection.IsValid
                    });
                }
            }
            
            return connections;
        }
        
        private void InitializeUtilitySystem()
        {
            _overlayRenderer = FindObjectOfType<BlueprintOverlayRenderer>();
            if (_overlayRenderer == null)
            {
                LogWarning("BlueprintOverlayRenderer not found - utility integration limited");
            }
            
            // Initialize utility node dictionaries
            foreach (UtilityType utilityType in System.Enum.GetValues(typeof(UtilityType)))
            {
                _utilityNodes[utilityType] = new List<UtilityNode>();
            }
        }
        
        private void SetupUtilityCamera()
        {
            if (_utilityCamera == null)
            {
                var cameraGO = new GameObject("UtilityLayerCamera");
                _utilityCamera = cameraGO.AddComponent<Camera>();
                cameraGO.transform.SetParent(transform);
            }
            
            _utilityCamera.cullingMask = _utilityLayer;
            _utilityCamera.clearFlags = CameraClearFlags.Nothing;
            _utilityCamera.depth = 10; // Render on top of overlay
            _utilityCamera.enabled = _enableUtilityVisualization;
        }
        
        private void CreateUtilityMaterials()
        {
            if (_connectionLineMaterial == null)
            {
                _connectionLineMaterial = CreateUtilityMaterial("UtilityConnectionMaterial", Color.white);
            }
            
            if (_flowIndicatorMaterial == null)
            {
                _flowIndicatorMaterial = CreateUtilityMaterial("UtilityFlowMaterial", Color.cyan);
            }
            
            if (_capacityIndicatorMaterial == null)
            {
                _capacityIndicatorMaterial = CreateUtilityMaterial("UtilityCapacityMaterial", Color.yellow);
            }
        }
        
        private Material CreateUtilityMaterial(string name, Color color)
        {
            var material = new Material(Shader.Find("Legacy Shaders/Transparent/Diffuse"));
            material.name = name;
            material.color = color;
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 100;
            return material;
        }
        
        private void InitializeUtilityColors()
        {
            _utilityColors[UtilityType.Electrical] = _electricalColor;
            _utilityColors[UtilityType.Water] = _waterColor;
            _utilityColors[UtilityType.Air] = _airColor;
            _utilityColors[UtilityType.Data] = _dataColor;
        }
        
        private void SetupConnectionPooling()
        {
            _connectionPool = new Queue<UtilityConnection>();
            
            // Pre-allocate connection objects
            for (int i = 0; i < _maxVisibleConnections; i++)
            {
                var connection = new UtilityConnection();
                _connectionPool.Enqueue(connection);
            }
        }
        
        private SchematicUtilityData AnalyzeSchematicUtilities(SchematicSO schematic)
        {
            var utilityData = new SchematicUtilityData();
            
            foreach (var item in schematic.Items)
            {
                // Analyze each item for utility requirements
                var requirements = GetUtilityRequirementsForItem(item);
                utilityData.AddRequirements(item.ItemId, requirements);
            }
            
            return utilityData;
        }
        
        private List<UtilityRequirement> GetUtilityRequirementsForItem(SchematicItem item)
        {
            var requirements = new List<UtilityRequirement>();
            
            // Determine utility requirements based on item category and properties
            switch (item.ItemCategory)
            {
                case ConstructionCategory.Equipment:
                    requirements.Add(new UtilityRequirement { Type = UtilityType.Electrical, Capacity = 10f });
                    break;
                case ConstructionCategory.Utility:
                    requirements.Add(new UtilityRequirement { Type = UtilityType.Water, Capacity = 5f });
                    requirements.Add(new UtilityRequirement { Type = UtilityType.Air, Capacity = 3f });
                    break;
                case ConstructionCategory.Structure:
                    requirements.Add(new UtilityRequirement { Type = UtilityType.Data, Capacity = 1f });
                    break;
            }
            
            return requirements;
        }
        
        private void CreateUtilityNodesForItem(SchematicItem item, Vector3 basePosition, Quaternion baseRotation)
        {
            var requirements = GetUtilityRequirementsForItem(item);
            Vector3 itemWorldPos = basePosition + new Vector3(item.GridPosition.x, item.Height, item.GridPosition.y);
            
            foreach (var requirement in requirements)
            {
                var node = new UtilityNode
                {
                    NodeId = System.Guid.NewGuid().ToString(),
                    ItemId = item.ItemId,
                    UtilityType = requirement.Type,
                    Position = itemWorldPos,
                    Capacity = requirement.Capacity,
                    IsSource = requirement.IsSource,
                    IsActive = true
                };
                
                // Create visual representation
                node.VisualObject = CreateUtilityNodeVisual(node);
                
                _utilityNodes[requirement.Type].Add(node);
            }
        }
        
        private GameObject CreateUtilityNodeVisual(UtilityNode node)
        {
            var nodeObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            nodeObj.name = $"UtilityNode_{node.UtilityType}_{node.NodeId[..8]}";
            nodeObj.layer = _utilityLayer;
            nodeObj.transform.position = node.Position;
            nodeObj.transform.localScale = Vector3.one * 0.2f;
            
            // Remove collider
            if (nodeObj.GetComponent<Collider>())
                DestroyImmediate(nodeObj.GetComponent<Collider>());
            
            // Set material color based on utility type
            var renderer = nodeObj.GetComponent<Renderer>();
            if (renderer != null && _utilityColors.ContainsKey(node.UtilityType))
            {
                var material = new Material(_connectionLineMaterial);
                material.color = _utilityColors[node.UtilityType];
                renderer.material = material;
            }
            
            return nodeObj;
        }
        
        private void GenerateUtilityConnections(SchematicUtilityData utilityData)
        {
            foreach (var utilityType in _utilityNodes.Keys)
            {
                var nodes = _utilityNodes[utilityType];
                if (nodes.Count < 2) continue;
                
                // Generate connections between nearby nodes
                for (int i = 0; i < nodes.Count; i++)
                {
                    for (int j = i + 1; j < nodes.Count; j++)
                    {
                        var distance = Vector3.Distance(nodes[i].Position, nodes[j].Position);
                        if (distance <= GetMaxConnectionDistance(utilityType))
                        {
                            CreateConnection(nodes[i], nodes[j]);
                        }
                    }
                }
            }
        }
        
        private void CreateConnection(UtilityNode fromNode, UtilityNode toNode)
        {
            if (_connectionPool.Count == 0)
            {
                LogWarning("Connection pool exhausted - cannot create new connection");
                return;
            }
            
            var connection = _connectionPool.Dequeue();
            connection.ConnectionId = System.Guid.NewGuid().ToString();
            connection.FromNode = fromNode;
            connection.ToNode = toNode;
            connection.UtilityType = fromNode.UtilityType;
            connection.Capacity = Mathf.Min(fromNode.Capacity, toNode.Capacity);
            connection.CurrentFlow = 0f;
            connection.IsValid = ValidateConnection(fromNode, toNode);
            
            // Create visual line
            connection.LineRenderer = CreateConnectionLine(fromNode.Position, toNode.Position, connection.UtilityType);
            
            _activeConnections[connection.ConnectionId] = connection;
            OnConnectionCreated?.Invoke(connection);
        }
        
        private LineRenderer CreateConnectionLine(Vector3 start, Vector3 end, UtilityType utilityType)
        {
            var lineObj = new GameObject($"UtilityConnection_{utilityType}");
            lineObj.layer = _utilityLayer;
            
            var lineRenderer = lineObj.AddComponent<LineRenderer>();
            lineRenderer.material = _connectionLineMaterial;
            
            // Set material color for utility type
            var materialInstance = new Material(_connectionLineMaterial);
            materialInstance.color = _utilityColors[utilityType];
            lineRenderer.material = materialInstance;
            lineRenderer.startWidth = _connectionLineWidth;
            lineRenderer.endWidth = _connectionLineWidth;
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;
            lineRenderer.sortingOrder = 100;
            
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
            
            return lineRenderer;
        }
        
        private void GenerateFlowIndicators()
        {
            if (!_showFlowDirections) return;
            
            foreach (var connection in _activeConnections.Values)
            {
                if (connection.IsValid && connection.CurrentFlow > 0f)
                {
                    CreateFlowIndicator(connection);
                }
            }
        }
        
        private void CreateFlowIndicator(UtilityConnection connection)
        {
            var midPoint = (connection.FromNode.Position + connection.ToNode.Position) * 0.5f;
            var direction = (connection.ToNode.Position - connection.FromNode.Position).normalized;
            
            var indicator = new UtilityFlowIndicator
            {
                IndicatorId = System.Guid.NewGuid().ToString(),
                Connection = connection,
                Position = midPoint,
                FlowDirection = direction,
                FlowSpeed = _flowAnimationSpeed,
                IsActive = true
            };
            
            // Create visual arrow
            indicator.VisualObject = CreateFlowArrow(midPoint, direction, connection.UtilityType);
            
            _flowIndicators.Add(indicator);
        }
        
        private GameObject CreateFlowArrow(Vector3 position, Vector3 direction, UtilityType utilityType)
        {
            var arrowObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arrowObj.name = $"FlowIndicator_{utilityType}";
            arrowObj.layer = _utilityLayer;
            arrowObj.transform.position = position;
            arrowObj.transform.rotation = Quaternion.LookRotation(direction);
            arrowObj.transform.localScale = new Vector3(0.1f, 0.1f, _flowArrowSize);
            
            // Remove collider
            if (arrowObj.GetComponent<Collider>())
                DestroyImmediate(arrowObj.GetComponent<Collider>());
            
            // Set material
            var renderer = arrowObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(_flowIndicatorMaterial);
                material.color = _utilityColors[utilityType];
                renderer.material = material;
            }
            
            return arrowObj;
        }
        
        private void UpdateAnimations()
        {
            _animationTime += Time.deltaTime;
            
            if (_enableFlowAnimation)
            {
                UpdateFlowAnimations();
            }
            
            if (_enablePulseIndicators)
            {
                UpdatePulseAnimations();
            }
        }
        
        private void UpdateFlowAnimations()
        {
            foreach (var indicator in _flowIndicators)
            {
                if (indicator.IsActive && indicator.VisualObject != null)
                {
                    // Animate flow movement along connection
                    float t = Mathf.PingPong(_animationTime * indicator.FlowSpeed, 1f);
                    Vector3 animatedPos = Vector3.Lerp(
                        indicator.Connection.FromNode.Position,
                        indicator.Connection.ToNode.Position,
                        t
                    );
                    
                    indicator.VisualObject.transform.position = animatedPos;
                }
            }
        }
        
        private void UpdatePulseAnimations()
        {
            float pulse = 1f + Mathf.Sin(_animationTime * _pulseSpeed) * _pulseIntensity;
            
            foreach (var nodeList in _utilityNodes.Values)
            {
                foreach (var node in nodeList)
                {
                    if (node.VisualObject != null && node.IsActive)
                    {
                        node.VisualObject.transform.localScale = Vector3.one * 0.2f * pulse;
                    }
                }
            }
        }
        
        private void UpdateFlowIndicators()
        {
            // Update flow rates based on connection validation and capacity
            foreach (var connection in _activeConnections.Values)
            {
                if (connection.IsValid)
                {
                    connection.CurrentFlow = Mathf.Min(connection.Capacity, GetCalculatedFlow(connection));
                }
                else
                {
                    connection.CurrentFlow = 0f;
                }
            }
        }
        
        private void UpdateLOD()
        {
            if (!_enableLOD) return;
            
            var cameraPos = Camera.main?.transform.position ?? Vector3.zero;
            
            foreach (var nodeList in _utilityNodes.Values)
            {
                foreach (var node in nodeList)
                {
                    if (node.VisualObject != null)
                    {
                        float distance = Vector3.Distance(cameraPos, node.Position);
                        bool shouldBeVisible = distance <= _maxUtilityRenderDistance;
                        node.VisualObject.SetActive(shouldBeVisible && _enableUtilityVisualization);
                    }
                }
            }
        }
        
        private void UpdateValidation()
        {
            if (!_showConnectionValidation) return;
            
            foreach (var connection in _activeConnections.Values)
            {
                bool wasValid = connection.IsValid;
                connection.IsValid = ValidateConnection(connection.FromNode, connection.ToNode);
                
                if (wasValid != connection.IsValid && connection.LineRenderer != null)
                {
                    // Update connection color based on validation
                    var material = connection.LineRenderer.material;
                    material.color = connection.IsValid ? _validConnectionColor : _invalidConnectionColor;
                }
            }
        }
        
        private void UpdateConnectionVisuals()
        {
            foreach (var connection in _activeConnections.Values)
            {
                if (connection.LineRenderer != null)
                {
                    connection.LineRenderer.SetPosition(0, connection.FromNode.Position);
                    connection.LineRenderer.SetPosition(1, connection.ToNode.Position);
                }
            }
        }
        
        private void UpdateConnectionsForUtilityType(UtilityType utilityType, bool visible)
        {
            foreach (var connection in _activeConnections.Values)
            {
                if (connection.UtilityType == utilityType && connection.LineRenderer != null)
                {
                    connection.LineRenderer.enabled = visible;
                }
            }
        }
        
        private UtilityValidationResult ValidateUtilityType(UtilityType utilityType)
        {
            var nodes = _utilityNodes[utilityType];
            var connections = _activeConnections.Values.Where(c => c.UtilityType == utilityType).ToList();
            
            return new UtilityValidationResult
            {
                UtilityType = utilityType,
                NodeCount = nodes.Count,
                ConnectionCount = connections.Count,
                ValidConnections = connections.Count(c => c.IsValid),
                TotalCapacity = nodes.Sum(n => n.Capacity),
                TotalFlow = connections.Sum(c => c.CurrentFlow),
                IsValid = connections.All(c => c.IsValid) && nodes.Count > 0
            };
        }
        
        private bool ValidateConnection(UtilityNode fromNode, UtilityNode toNode)
        {
            // Basic validation - can be expanded with more complex rules
            if (fromNode.UtilityType != toNode.UtilityType) return false;
            
            float distance = Vector3.Distance(fromNode.Position, toNode.Position);
            return distance <= GetMaxConnectionDistance(fromNode.UtilityType);
        }
        
        private float GetMaxConnectionDistance(UtilityType utilityType)
        {
            return utilityType switch
            {
                UtilityType.Electrical => 5f,
                UtilityType.Water => 3f,
                UtilityType.Air => 4f,
                UtilityType.Data => 6f,
                _ => 3f
            };
        }
        
        private float GetCalculatedFlow(UtilityConnection connection)
        {
            // Simplified flow calculation - can be expanded with more complex logic
            return connection.Capacity * 0.7f; // Assume 70% capacity utilization
        }
        
        private bool IsConnectionNearPosition(UtilityConnection connection, Vector3 position, float radius)
        {
            Vector3 lineStart = connection.FromNode.Position;
            Vector3 lineEnd = connection.ToNode.Position;
            
            Vector3 closestPoint = GetClosestPointOnLine(lineStart, lineEnd, position);
            return Vector3.Distance(closestPoint, position) <= radius;
        }
        
        private Vector3 GetClosestPointOnLine(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
        {
            Vector3 lineDirection = lineEnd - lineStart;
            float lineLength = lineDirection.magnitude;
            lineDirection.Normalize();
            
            float projectedDistance = Mathf.Clamp(Vector3.Dot(point - lineStart, lineDirection), 0f, lineLength);
            return lineStart + lineDirection * projectedDistance;
        }
        
        private void ReturnConnectionToPool(UtilityConnection connection)
        {
            if (connection.LineRenderer != null)
            {
                DestroyImmediate(connection.LineRenderer.gameObject);
                connection.LineRenderer = null;
            }
            
            connection.Reset();
            _connectionPool.Enqueue(connection);
        }
        
        protected override void OnManagerShutdown()
        {
            ClearUtilityVisualization();
            LogInfo($"UtilityLayerRenderer shutdown - {ActiveConnectionCount} connections cleaned up");
        }
    }
    
    /// <summary>
    /// Utility node representing a connection point
    /// </summary>
    [System.Serializable]
    public class UtilityNode
    {
        public string NodeId;
        public string ItemId;
        public UtilityType UtilityType;
        public Vector3 Position;
        public float Capacity;
        public bool IsSource;
        public bool IsActive;
        public GameObject VisualObject;
        
        public void UpdatePosition(Vector3 basePosition, Quaternion rotation)
        {
            // Update position relative to base - simplified implementation
            if (VisualObject != null)
            {
                VisualObject.transform.position = Position;
            }
        }
        
        public void SetVisible(bool visible)
        {
            if (VisualObject != null)
            {
                VisualObject.SetActive(visible);
            }
        }
    }
    
    /// <summary>
    /// Utility connection between two nodes
    /// </summary>
    [System.Serializable]
    public class UtilityConnection
    {
        public string ConnectionId;
        public UtilityNode FromNode;
        public UtilityNode ToNode;
        public UtilityType UtilityType;
        public float Capacity;
        public float CurrentFlow;
        public bool IsValid;
        public LineRenderer LineRenderer;
        
        public void Reset()
        {
            ConnectionId = null;
            FromNode = null;
            ToNode = null;
            Capacity = 0f;
            CurrentFlow = 0f;
            IsValid = false;
            LineRenderer = null;
        }
    }
    
    /// <summary>
    /// Flow indicator for animated utility flow
    /// </summary>
    [System.Serializable]
    public class UtilityFlowIndicator
    {
        public string IndicatorId;
        public UtilityConnection Connection;
        public Vector3 Position;
        public Vector3 FlowDirection;
        public float FlowSpeed;
        public bool IsActive;
        public GameObject VisualObject;
    }
    
    /// <summary>
    /// Utility requirement for schematic items
    /// </summary>
    [System.Serializable]
    public class UtilityRequirement
    {
        public UtilityType Type;
        public float Capacity;
        public bool IsSource = false;
        public bool IsRequired = true;
    }
    
    /// <summary>
    /// Utility data for a complete schematic
    /// </summary>
    [System.Serializable]
    public class SchematicUtilityData
    {
        public Dictionary<string, List<UtilityRequirement>> ItemRequirements = new Dictionary<string, List<UtilityRequirement>>();
        
        public void AddRequirements(string itemId, List<UtilityRequirement> requirements)
        {
            ItemRequirements[itemId] = requirements;
        }
    }
    
    /// <summary>
    /// Validation result for utility systems
    /// </summary>
    [System.Serializable]
    public class UtilityValidationResult
    {
        public UtilityType UtilityType;
        public int NodeCount;
        public int ConnectionCount;
        public int ValidConnections;
        public float TotalCapacity;
        public float TotalFlow;
        public bool IsValid;
        public string ValidationMessage;
    }
    
    /// <summary>
    /// Information about a utility connection
    /// </summary>
    [System.Serializable]
    public class UtilityConnectionInfo
    {
        public string ConnectionId;
        public UtilityType UtilityType;
        public UtilityNode FromNode;
        public UtilityNode ToNode;
        public float Capacity;
        public float CurrentFlow;
        public bool IsValid;
    }
    
    /// <summary>
    /// Types of utilities that can be visualized
    /// </summary>
    public enum UtilityType
    {
        Electrical,
        Water,
        Air,
        Data,
        Steam,
        Gas
    }
}