using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectChimera.Data.Genetics.Blockchain;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.UI.Genetics
{
    /// <summary>
    /// Lineage visualization panel - displays breeding history as a family tree.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST:
    /// =====================================
    /// Makes breeding history VISUAL and ENGAGING:
    ///
    /// 1. **Family Tree View** - Like genealogy for plants
    ///    - Visual parent → child relationships
    ///    - Timeline showing breeding dates
    ///    - "I created this entire lineage!"
    ///
    /// 2. **Breeding Achievement** - Shows player's work
    ///    - Trace back to original purchased seeds
    ///    - See how many generations bred
    ///    - "From Blue Dream → 5 generations → My Custom Strain"
    ///
    /// 3. **Strategic Planning** - Plan future crosses
    ///    - See which strains were bred together
    ///    - Identify traits from specific ancestors
    ///    - "Let me cross this back to the grandparent for trait stabilization"
    ///
    /// 4. **Marketplace Transparency** - Trust in trading
    ///    - Buyers can see complete strain history
    ///    - "This strain really is descended from OG Kush!"
    ///    - Verified lineage increases value
    ///
    /// VISUAL DESIGN:
    /// Simple tree layout with nodes and connecting lines.
    /// Click node → see strain details. Scroll/zoom for large lineages.
    /// </summary>
    public class LineageVisualizationPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Transform _lineageContainer;
        [SerializeField] private ScrollRect _scrollView;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _statsText;
        [SerializeField] private Button _closeButton;

        [Header("Prefabs")]
        [SerializeField] private GameObject _lineageNodePrefab;
        [SerializeField] private GameObject _lineConnectionPrefab;

        [Header("Layout Configuration")]
        [SerializeField] private float _nodeSpacingX = 200f;
        [SerializeField] private float _nodeSpacingY = 150f;
        [SerializeField] private float _generationOffset = 250f;

        [Header("Colors")]
        [SerializeField] private Color _genesisNodeColor = new Color(0.2f, 0.6f, 1.0f);  // Blue
        [SerializeField] private Color _bredNodeColor = new Color(0.3f, 0.8f, 0.3f);     // Green
        [SerializeField] private Color _currentNodeColor = new Color(1.0f, 0.8f, 0.2f);  // Gold

        private List<GameObject> _instantiatedNodes = new List<GameObject>();
        private List<GameObject> _instantiatedConnections = new List<GameObject>();

        private void Awake()
        {
            // Hide panel by default
            if (_panelRoot != null)
                _panelRoot.SetActive(false);

            // Setup button listeners
            if (_closeButton != null)
                _closeButton.onClick.AddListener(OnCloseClicked);
        }

        /// <summary>
        /// Shows lineage visualization for a strain's breeding history.
        ///
        /// GAMEPLAY: Player clicks "View Lineage" → sees family tree.
        /// Visual representation of breeding achievements!
        /// </summary>
        public void ShowLineage(List<GeneEventPacket> lineage, string currentStrainName = "")
        {
            if (lineage == null || lineage.Count == 0)
            {
                ChimeraLogger.LogWarning("UI",
                    "Cannot show lineage: no breeding events provided", this);
                return;
            }

            // Clear existing visualization
            ClearLineage();

            // Set title
            if (_titleText != null)
            {
                _titleText.text = string.IsNullOrEmpty(currentStrainName)
                    ? "Strain Lineage"
                    : $"{currentStrainName} - Breeding History";
            }

            // Set stats
            if (_statsText != null)
            {
                _statsText.text = $"{lineage.Count} breeding events | " +
                                 $"Generation {lineage[lineage.Count - 1].Generation}";
            }

            // Create visual lineage tree
            CreateLineageTree(lineage);

            // Show panel
            if (_panelRoot != null)
                _panelRoot.SetActive(true);

            ChimeraLogger.Log("UI",
                $"Showing lineage with {lineage.Count} events", this);
        }

        /// <summary>
        /// Creates visual family tree from lineage data.
        ///
        /// ALGORITHM:
        /// 1. Layout nodes by generation (vertical levels)
        /// 2. Position siblings horizontally
        /// 3. Draw connecting lines between parents and offspring
        /// 4. Color code: Genesis (blue), Bred (green), Current (gold)
        ///
        /// GAMEPLAY: Player sees visual breeding history at a glance!
        /// </summary>
        private void CreateLineageTree(List<GeneEventPacket> lineage)
        {
            if (_lineageContainer == null)
            {
                ChimeraLogger.LogError("UI",
                    "LineageContainer not assigned", this);
                return;
            }

            // Group lineage events by generation for layout
            var generationGroups = new Dictionary<int, List<GeneEventPacket>>();

            foreach (var packet in lineage)
            {
                if (!generationGroups.ContainsKey(packet.Generation))
                    generationGroups[packet.Generation] = new List<GeneEventPacket>();

                generationGroups[packet.Generation].Add(packet);
            }

            // Create nodes for each generation
            foreach (var generation in generationGroups.Keys)
            {
                var events = generationGroups[generation];

                // Layout nodes horizontally within generation
                float startX = -(events.Count - 1) * _nodeSpacingX / 2f;
                float yPosition = -generation * _generationOffset;

                for (int i = 0; i < events.Count; i++)
                {
                    var packet = events[i];
                    float xPosition = startX + (i * _nodeSpacingX);

                    CreateLineageNode(packet, new Vector2(xPosition, yPosition), generation);
                }
            }

            // Create connecting lines between parents and offspring
            // (Simplified for now - would need parent tracking for full tree)
            CreateConnectionLines(lineage);

            // Auto-scroll to show current strain (last in lineage)
            if (_scrollView != null)
            {
                Canvas.ForceUpdateCanvases();
                _scrollView.verticalNormalizedPosition = 0f; // Scroll to bottom (newest)
            }
        }

        /// <summary>
        /// Creates a single lineage node (representing one breeding event).
        ///
        /// VISUAL ELEMENTS:
        /// - Strain name
        /// - Generation label (F1, F2, etc.)
        /// - Breeding date
        /// - Color coding (genesis/bred/current)
        /// - Click interaction for details
        /// </summary>
        private void CreateLineageNode(GeneEventPacket packet, Vector2 position, int generation)
        {
            if (_lineageNodePrefab == null)
            {
                ChimeraLogger.LogWarning("UI",
                    "LineageNodePrefab not assigned - using placeholder", this);
                return;
            }

            // Instantiate node
            var nodeObj = Instantiate(_lineageNodePrefab, _lineageContainer);
            nodeObj.transform.localPosition = position;

            // Setup node UI
            var nodeUI = nodeObj.GetComponent<LineageNode>();
            if (nodeUI != null)
            {
                // Determine node color
                Color nodeColor;
                if (packet.IsGenesis())
                    nodeColor = _genesisNodeColor;  // Blue for purchased seeds
                else if (generation == GetMaxGeneration())
                    nodeColor = _currentNodeColor;  // Gold for current strain
                else
                    nodeColor = _bredNodeColor;     // Green for bred strains

                nodeUI.SetData(
                    strainName: packet.StrainName,
                    generation: generation,
                    breedingDate: packet.Timestamp,
                    isGenesis: packet.IsGenesis(),
                    nodeColor: nodeColor);
            }

            _instantiatedNodes.Add(nodeObj);
        }

        /// <summary>
        /// Creates visual connection lines between parent and offspring nodes.
        ///
        /// GAMEPLAY: Shows breeding relationships visually.
        /// "This strain came from crossing these two parents"
        /// </summary>
        private void CreateConnectionLines(List<GeneEventPacket> lineage)
        {
            // Simplified implementation - would need parent node tracking for full tree
            // For now, just connect sequential generations

            if (_lineConnectionPrefab == null)
                return;

            for (int i = 1; i < lineage.Count; i++)
            {
                var currentPacket = lineage[i];
                var previousPacket = lineage[i - 1];

                // Create connection line
                var lineObj = Instantiate(_lineConnectionPrefab, _lineageContainer);

                // Position line between nodes
                var lineRenderer = lineObj.GetComponent<LineRenderer>();
                if (lineRenderer != null)
                {
                    // Calculate positions
                    float currentY = -currentPacket.Generation * _generationOffset;
                    float previousY = -previousPacket.Generation * _generationOffset;

                    lineRenderer.SetPosition(0, new Vector3(0, previousY, 0));
                    lineRenderer.SetPosition(1, new Vector3(0, currentY, 0));
                }

                _instantiatedConnections.Add(lineObj);
            }
        }

        /// <summary>
        /// Gets maximum generation in lineage (for color coding).
        /// </summary>
        private int GetMaxGeneration()
        {
            int maxGen = 0;
            foreach (var node in _instantiatedNodes)
            {
                var lineageNode = node.GetComponent<LineageNode>();
                if (lineageNode != null && lineageNode.Generation > maxGen)
                    maxGen = lineageNode.Generation;
            }
            return maxGen;
        }

        /// <summary>
        /// Clears existing lineage visualization.
        /// </summary>
        private void ClearLineage()
        {
            // Destroy all instantiated nodes
            foreach (var node in _instantiatedNodes)
            {
                if (node != null)
                    Destroy(node);
            }
            _instantiatedNodes.Clear();

            // Destroy all instantiated connections
            foreach (var connection in _instantiatedConnections)
            {
                if (connection != null)
                    Destroy(connection);
            }
            _instantiatedConnections.Clear();
        }

        /// <summary>
        /// Closes the lineage panel.
        /// </summary>
        private void OnCloseClicked()
        {
            if (_panelRoot != null)
                _panelRoot.SetActive(false);

            ClearLineage();
        }

        /// <summary>
        /// Hides the panel (can be called externally).
        /// </summary>
        public void Hide()
        {
            OnCloseClicked();
        }

        /// <summary>
        /// Quick check if panel is currently visible.
        /// </summary>
        public bool IsVisible()
        {
            return _panelRoot != null && _panelRoot.activeSelf;
        }

        private void OnDestroy()
        {
            // Clean up button listeners
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(OnCloseClicked);

            // Clean up instantiated objects
            ClearLineage();
        }
    }

    /// <summary>
    /// Individual lineage node component (attach to prefab).
    /// Displays information for one breeding event in the tree.
    /// </summary>
    public class LineageNode : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _strainNameText;
        [SerializeField] private TextMeshProUGUI _generationText;
        [SerializeField] private TextMeshProUGUI _dateText;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Button _nodeButton;

        public int Generation { get; private set; }
        private string _strainName;
        private long _breedingTimestamp;
        private bool _isGenesis;

        /// <summary>
        /// Sets node data and updates visual display.
        /// </summary>
        public void SetData(
            string strainName,
            int generation,
            long breedingDate,
            bool isGenesis,
            Color nodeColor)
        {
            _strainName = strainName;
            Generation = generation;
            _breedingTimestamp = breedingDate;
            _isGenesis = isGenesis;

            // Update UI
            if (_strainNameText != null)
            {
                _strainNameText.text = strainName;
            }

            if (_generationText != null)
            {
                if (isGenesis)
                {
                    _generationText.text = "Purchased Seed";
                }
                else
                {
                    _generationText.text = $"Generation F{generation}";
                }
            }

            if (_dateText != null)
            {
                var date = System.DateTimeOffset.FromUnixTimeSeconds(breedingDate);
                _dateText.text = date.ToString("yyyy-MM-dd");
            }

            if (_backgroundImage != null)
            {
                _backgroundImage.color = nodeColor;
            }

            // Setup click interaction
            if (_nodeButton != null)
            {
                _nodeButton.onClick.AddListener(OnNodeClicked);
            }
        }

        /// <summary>
        /// Called when player clicks this node.
        /// Could show detailed breeding event info.
        /// </summary>
        private void OnNodeClicked()
        {
            ChimeraLogger.Log("UI",
                $"Lineage node clicked: {_strainName} (Gen {Generation})", this);

            // TODO: Show detailed breeding event popup
            // Could display parent strain names, exact genetics, etc.
        }

        private void OnDestroy()
        {
            if (_nodeButton != null)
                _nodeButton.onClick.RemoveListener(OnNodeClicked);
        }
    }
}
