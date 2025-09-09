using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Construction;
using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Specialized renderer for utility nodes and connection points.
    /// Handles node visualization, LOD management, and node interaction rendering.
    /// </summary>
    public class UtilityNodeRenderer
    {
        private readonly UtilityRenderingCore _renderingCore;

        // Node tracking
        private Dictionary<UtilityType, List<UtilityNode>> _utilityNodes = new Dictionary<UtilityType, List<UtilityNode>>();
        private Queue<GameObject> _nodeVisualPool = new Queue<GameObject>();

        // Events
        public System.Action<UtilityNode> OnNodeCreated;
        public System.Action<UtilityNode> OnNodeDestroyed;
        public System.Action<UtilityType, int> OnNodeCountChanged;

        public UtilityNodeRenderer(UtilityRenderingCore renderingCore)
        {
            _renderingCore = renderingCore;
            InitializeNodeSystem();
        }

        private void InitializeNodeSystem()
        {
            // Initialize utility node dictionaries
            foreach (UtilityType utilityType in System.Enum.GetValues(typeof(UtilityType)))
            {
                _utilityNodes[utilityType] = new List<UtilityNode>();
            }

            // Pre-populate node visual pool
            for (int i = 0; i < 50; i++)
            {
                var nodeVisual = CreatePooledNodeVisual();
                nodeVisual.SetActive(false);
                _nodeVisualPool.Enqueue(nodeVisual);
            }
        }

        /// <summary>
        /// Create utility nodes for a schematic item
        /// </summary>
        public void CreateUtilityNodesForItem(SchematicItem item, Vector3 basePosition, Quaternion baseRotation)
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
                OnNodeCreated?.Invoke(node);
                OnNodeCountChanged?.Invoke(requirement.Type, _utilityNodes[requirement.Type].Count);
            }
        }

        /// <summary>
        /// Update node positions for schematic movement
        /// </summary>
        public void UpdateNodePositions(Vector3 newPosition, Quaternion newRotation)
        {
            foreach (var node in _utilityNodes.Values.SelectMany(nodes => nodes))
            {
                node.UpdatePosition(newPosition, newRotation);
            }
        }

        /// <summary>
        /// Set visibility for all nodes of specific utility type
        /// </summary>
        public void SetUtilityTypeVisible(UtilityType utilityType, bool visible)
        {
            if (_utilityNodes.ContainsKey(utilityType))
            {
                foreach (var node in _utilityNodes[utilityType])
                {
                    node.SetVisible(visible);
                }
            }
        }

        /// <summary>
        /// Update LOD for all nodes based on camera distance
        /// </summary>
        public void UpdateLOD()
        {
            if (!_renderingCore.EnableLOD) return;

            var cameraPos = UnityEngine.Camera.main?.transform.position ?? Vector3.zero;

            foreach (var nodeList in _utilityNodes.Values)
            {
                foreach (var node in nodeList)
                {
                    if (node.VisualObject != null)
                    {
                        float distance = Vector3.Distance(cameraPos, node.Position);
                        bool shouldBeVisible = distance <= _renderingCore.MaxUtilityRenderDistance;
                        node.VisualObject.SetActive(shouldBeVisible && _renderingCore.UtilityVisualizationEnabled);

                        // Scale based on distance for LOD
                        if (shouldBeVisible)
                        {
                            float lodScale = Mathf.Lerp(1f, 0.5f, distance / _renderingCore.MaxUtilityRenderDistance);
                            node.VisualObject.transform.localScale = Vector3.one * 0.2f * lodScale;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get all nodes of specified utility type
        /// </summary>
        public List<UtilityNode> GetNodesOfType(UtilityType utilityType)
        {
            return _utilityNodes.TryGetValue(utilityType, out var nodes) ? nodes : new List<UtilityNode>();
        }

        /// <summary>
        /// Get node counts by utility type
        /// </summary>
        public Dictionary<UtilityType, int> GetNodeCounts()
        {
            return _utilityNodes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);
        }

        /// <summary>
        /// Find nearest node to position within radius
        /// </summary>
        public UtilityNode FindNearestNode(Vector3 position, float radius, UtilityType? filterType = null)
        {
            UtilityNode nearestNode = null;
            float nearestDistance = float.MaxValue;

            var nodesToCheck = filterType.HasValue ?
                _utilityNodes[filterType.Value] :
                _utilityNodes.Values.SelectMany(nodes => nodes);

            foreach (var node in nodesToCheck)
            {
                float distance = Vector3.Distance(position, node.Position);
                if (distance <= radius && distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestNode = node;
                }
            }

            return nearestNode;
        }

        /// <summary>
        /// Clear all nodes
        /// </summary>
        public void ClearAllNodes()
        {
            foreach (var nodeList in _utilityNodes.Values)
            {
                foreach (var node in nodeList)
                {
                    ReturnNodeVisualToPool(node.VisualObject);
                    OnNodeDestroyed?.Invoke(node);
                }
                nodeList.Clear();
            }

            // Update counts
            foreach (var utilityType in _utilityNodes.Keys)
            {
                OnNodeCountChanged?.Invoke(utilityType, 0);
            }
        }

        /// <summary>
        /// Apply pulse animation to nodes
        /// </summary>
        public void ApplyPulseAnimation(float pulseValue)
        {
            foreach (var nodeList in _utilityNodes.Values)
            {
                foreach (var node in nodeList)
                {
                    if (node.VisualObject != null && node.IsActive)
                    {
                        var currentScale = node.VisualObject.transform.localScale;
                        var baseScale = 0.2f;
                        node.VisualObject.transform.localScale = Vector3.one * baseScale * pulseValue;
                    }
                }
            }
        }

        private GameObject CreateUtilityNodeVisual(UtilityNode node)
        {
            GameObject nodeObj;

            // Try to get from pool first
            if (_nodeVisualPool.Count > 0)
            {
                nodeObj = _nodeVisualPool.Dequeue();
                nodeObj.SetActive(true);
            }
            else
            {
                nodeObj = CreatePooledNodeVisual();
            }

            // Configure node visual
            nodeObj.name = $"UtilityNode_{node.UtilityType}_{node.NodeId[..8]}";
            nodeObj.transform.position = node.Position;
            nodeObj.transform.localScale = Vector3.one * 0.2f;

            // Set material color based on utility type
            var renderer = nodeObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(_renderingCore.GetRenderingMaterial(UtilityRenderingMaterialType.Connection));
                material.color = _renderingCore.GetUtilityColor(node.UtilityType);
                renderer.material = material;
            }

            // Add node component for identification
            var nodeComponent = nodeObj.GetComponent<UtilityNodeComponent>();
            if (nodeComponent == null)
            {
                nodeComponent = nodeObj.AddComponent<UtilityNodeComponent>();
            }
            nodeComponent.Initialize(node);

            return nodeObj;
        }

        private GameObject CreatePooledNodeVisual()
        {
            var nodeObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            nodeObj.layer = _renderingCore.UtilityLayer;

            // Remove collider for performance
            if (nodeObj.GetComponent<Collider>())
                Object.DestroyImmediate(nodeObj.GetComponent<Collider>());

            return nodeObj;
        }

        private void ReturnNodeVisualToPool(GameObject nodeVisual)
        {
            if (nodeVisual != null)
            {
                nodeVisual.SetActive(false);
                nodeVisual.transform.position = Vector3.zero;
                nodeVisual.transform.localScale = Vector3.one;
                _nodeVisualPool.Enqueue(nodeVisual);
            }
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

        public void Cleanup()
        {
            ClearAllNodes();

            // Clear pool
            while (_nodeVisualPool.Count > 0)
            {
                var nodeVisual = _nodeVisualPool.Dequeue();
                if (nodeVisual != null)
                    Object.DestroyImmediate(nodeVisual);
            }
        }
    }

    /// <summary>
    /// Component for identifying utility nodes in the scene
    /// </summary>
    public class UtilityNodeComponent : MonoBehaviour
    {
        public UtilityNode NodeData { get; private set; }

        public void Initialize(UtilityNode nodeData)
        {
            NodeData = nodeData;
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
