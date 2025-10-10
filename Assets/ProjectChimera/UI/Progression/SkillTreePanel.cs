using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectChimera.Systems.Progression;
using ProjectChimera.Data.Progression;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;
using System.Collections.Generic;

namespace ProjectChimera.UI.Progression
{
    /// <summary>
    /// Skill tree UI panel - cannabis leaf visualization with 5 branch points.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST (from Gameplay Doc):
    /// =========================================================
    /// "The progression system is visualized as a cannabis leaf with five points"
    /// "The leaf visually expands as players progress"
    ///
    /// **Visual Design:**
    /// - Cannabis leaf shape in center of screen
    /// - 5 branches radiating from center (72° apart)
    /// - Nodes arranged along each branch
    /// - Unlocked nodes glow/illuminate
    /// - Locked nodes appear dim/greyed
    /// - Available nodes pulse (can unlock now)
    ///
    /// **Branch Layout (from center, clockwise):**
    /// 1. Cultivation (top, 90°)
    /// 2. Genetics (top-right, 18°)
    /// 3. Construction (bottom-right, 306°)
    /// 4. Automation (bottom-left, 234°)
    /// 5. Research (top-left, 162°)
    ///
    /// **Interaction:**
    /// - Click node → Show details panel
    /// - Click "Unlock" → Spend skill points
    /// - Hover node → Show tooltip
    /// - Zoom/pan to see all nodes clearly
    ///
    /// INVISIBLE COMPLEXITY:
    /// Players see a beautiful, growing leaf - not a complex tech tree graph.
    /// They experience visual progress as the leaf "blooms" with unlocked skills.
    /// </summary>
    public class SkillTreePanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _skillPointsText;
        [SerializeField] private TextMeshProUGUI _completionText;
        [SerializeField] private Button _closeButton;

        [Header("Leaf Visualization")]
        [SerializeField] private Transform _leafCenter;
        [SerializeField] private Image _leafBackgroundImage;
        [SerializeField] private GameObject _skillNodePrefab;

        [Header("Node Details Panel")]
        [SerializeField] private GameObject _nodeDetailsPanel;
        [SerializeField] private TextMeshProUGUI _nodeNameText;
        [SerializeField] private TextMeshProUGUI _nodeDescriptionText;
        [SerializeField] private TextMeshProUGUI _nodeEffectText;
        [SerializeField] private TextMeshProUGUI _nodeCostText;
        [SerializeField] private Button _unlockNodeButton;
        [SerializeField] private TextMeshProUGUI _unlockButtonText;

        [Header("Branch Colors")]
        [SerializeField] private Color _cultivationColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _geneticsColor = new Color(0.8f, 0.2f, 0.8f);
        [SerializeField] private Color _constructionColor = new Color(0.2f, 0.5f, 0.8f);
        [SerializeField] private Color _automationColor = new Color(0.8f, 0.6f, 0.2f);
        [SerializeField] private Color _researchColor = new Color(0.8f, 0.2f, 0.2f);

        [Header("Layout Configuration")]
        [SerializeField] private float _branchLength = 300f;
        [SerializeField] private float _nodeSpacing = 60f;

        // Services
        private SkillTreeManager _skillTreeManager;

        // Runtime state
        private Dictionary<string, SkillNodeUI> _nodeUIElements = new Dictionary<string, SkillNodeUI>();
        private SkillNode _selectedNode;

        private void Start()
        {
            InitializePanel();
            SetupButtonListeners();
        }

        private void InitializePanel()
        {
            // Get skill tree manager
            var container = ServiceContainerFactory.Instance;
            if (container != null)
            {
                _skillTreeManager = container.Resolve<SkillTreeManager>();
            }

            if (_skillTreeManager == null)
            {
                ChimeraLogger.LogWarning("UI",
                    "SkillTreePanel: SkillTreeManager not found", this);
                return;
            }

            // Subscribe to events
            _skillTreeManager.OnNodeUnlocked += OnNodeUnlocked;
            _skillTreeManager.OnSkillPointsChanged += OnSkillPointsChanged;

            // Hide panels by default
            if (_panelRoot != null)
                _panelRoot.SetActive(false);

            if (_nodeDetailsPanel != null)
                _nodeDetailsPanel.SetActive(false);

            ChimeraLogger.Log("UI",
                "Skill tree panel initialized", this);
        }

        private void SetupButtonListeners()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(OnCloseClicked);

            if (_unlockNodeButton != null)
                _unlockNodeButton.onClick.AddListener(OnUnlockNodeClicked);
        }

        /// <summary>
        /// Shows the skill tree panel and builds the leaf visualization.
        ///
        /// GAMEPLAY:
        /// - Player opens progression menu
        /// - Cannabis leaf appears with 5 branches
        /// - Unlocked nodes glow, available nodes pulse
        /// - Completion percentage shown ("Progression Leaf: 45%")
        /// </summary>
        public void ShowPanel()
        {
            if (_skillTreeManager == null)
                return;

            // Build the skill tree visualization
            BuildLeafVisualization();

            // Update UI
            UpdateSkillPointsDisplay();
            UpdateCompletionDisplay();

            // Show panel
            if (_panelRoot != null)
                _panelRoot.SetActive(true);

            ChimeraLogger.Log("UI",
                "Skill tree panel opened", this);
        }

        /// <summary>
        /// Builds the cannabis leaf visualization with all nodes.
        ///
        /// GAMEPLAY:
        /// Creates the iconic leaf shape with nodes arranged along 5 branches.
        /// Each branch radiates from center at specific angles.
        /// </summary>
        private void BuildLeafVisualization()
        {
            // Clear existing nodes
            foreach (var nodeUI in _nodeUIElements.Values)
            {
                if (nodeUI != null)
                    Destroy(nodeUI.gameObject);
            }
            _nodeUIElements.Clear();

            var skillTreeData = _skillTreeManager.GetSkillTreeData();
            if (skillTreeData == null)
                return;

            // Build each branch (5 leaf points)
            var branches = skillTreeData.GetAllBranches();
            for (int i = 0; i < branches.Count; i++)
            {
                var branch = branches[i];
                if (branch == null)
                    continue;

                // Calculate branch angle (72° apart, starting from top)
                float branchAngle = GetBranchAngle(branch.BranchType);
                Color branchColor = GetBranchColor(branch.BranchType);

                // Create nodes along this branch
                for (int nodeIndex = 0; nodeIndex < branch.Nodes.Count; nodeIndex++)
                {
                    var node = branch.Nodes[nodeIndex];
                    CreateNodeUI(node, branchAngle, nodeIndex, branchColor);
                }
            }
        }

        /// <summary>
        /// Creates UI element for a single skill node.
        /// </summary>
        private void CreateNodeUI(SkillNode node, float branchAngle, int nodeIndex, Color branchColor)
        {
            if (_skillNodePrefab == null || _leafCenter == null)
                return;

            // Calculate position along branch
            float distanceFromCenter = _nodeSpacing * (nodeIndex + 1);
            Vector2 nodePosition = CalculateNodePosition(branchAngle, distanceFromCenter);

            // Instantiate node UI
            var nodeObj = Instantiate(_skillNodePrefab, _leafCenter);
            var rectTransform = nodeObj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = nodePosition;
            }

            // Setup node UI component
            var nodeUI = nodeObj.GetComponent<SkillNodeUI>();
            if (nodeUI != null)
            {
                bool isUnlocked = _skillTreeManager.IsNodeUnlocked(node.NodeId);
                bool canUnlock = _skillTreeManager.CanUnlockNode(node.NodeId);

                nodeUI.Setup(node, branchColor, isUnlocked, canUnlock);
                nodeUI.OnNodeClicked += OnNodeSelected;

                _nodeUIElements[node.NodeId] = nodeUI;
            }
        }

        /// <summary>
        /// Calculates branch angle based on type.
        /// Cannabis leaf has 5 points arranged symmetrically.
        /// </summary>
        private float GetBranchAngle(SkillBranchType branchType)
        {
            return branchType switch
            {
                SkillBranchType.Cultivation => 90f,      // Top
                SkillBranchType.Genetics => 18f,         // Top-right
                SkillBranchType.Construction => 306f,    // Bottom-right
                SkillBranchType.Automation => 234f,      // Bottom-left
                SkillBranchType.Research => 162f,        // Top-left
                _ => 0f
            };
        }

        /// <summary>
        /// Gets branch color for visual differentiation.
        /// </summary>
        private Color GetBranchColor(SkillBranchType branchType)
        {
            return branchType switch
            {
                SkillBranchType.Cultivation => _cultivationColor,
                SkillBranchType.Genetics => _geneticsColor,
                SkillBranchType.Construction => _constructionColor,
                SkillBranchType.Automation => _automationColor,
                SkillBranchType.Research => _researchColor,
                _ => Color.white
            };
        }

        /// <summary>
        /// Calculates node position on canvas.
        /// </summary>
        private Vector2 CalculateNodePosition(float angleDegrees, float distance)
        {
            float angleRadians = angleDegrees * Mathf.Deg2Rad;
            float x = Mathf.Cos(angleRadians) * distance;
            float y = Mathf.Sin(angleRadians) * distance;
            return new Vector2(x, y);
        }

        /// <summary>
        /// Called when player selects a node.
        /// Shows node details panel with unlock button.
        /// </summary>
        private void OnNodeSelected(SkillNode node)
        {
            _selectedNode = node;

            if (_nodeDetailsPanel == null)
                return;

            // Show details panel
            _nodeDetailsPanel.SetActive(true);

            // Update node info
            if (_nodeNameText != null)
                _nodeNameText.text = node.NodeName;

            if (_nodeDescriptionText != null)
                _nodeDescriptionText.text = node.Description;

            if (_nodeEffectText != null)
                _nodeEffectText.text = $"Effect: {node.Effect.GetEffectDescription()}";

            if (_nodeCostText != null)
                _nodeCostText.text = $"Cost: {node.SkillPointCost} Skill Points";

            // Update unlock button
            bool isUnlocked = _skillTreeManager.IsNodeUnlocked(node.NodeId);
            bool canUnlock = _skillTreeManager.CanUnlockNode(node.NodeId);

            if (_unlockNodeButton != null)
            {
                _unlockNodeButton.interactable = canUnlock;

                if (_unlockButtonText != null)
                {
                    if (isUnlocked)
                        _unlockButtonText.text = "✅ Unlocked";
                    else if (canUnlock)
                        _unlockButtonText.text = "Unlock";
                    else
                        _unlockButtonText.text = "Locked";
                }
            }
        }

        /// <summary>
        /// Called when player clicks unlock button.
        /// Attempts to unlock the selected node.
        /// </summary>
        private void OnUnlockNodeClicked()
        {
            if (_selectedNode == null || _skillTreeManager == null)
                return;

            bool success = _skillTreeManager.UnlockNode(_selectedNode.NodeId);

            if (success)
            {
                // Update node UI
                if (_nodeUIElements.TryGetValue(_selectedNode.NodeId, out var nodeUI))
                {
                    bool canUnlock = _skillTreeManager.CanUnlockNode(_selectedNode.NodeId);
                    nodeUI.SetUnlocked(true, canUnlock);
                }

                // Refresh details panel
                OnNodeSelected(_selectedNode);

                // Update nearby nodes (they might now be available)
                RefreshNodeStates();

                ChimeraLogger.Log("UI",
                    $"Node unlocked via UI: {_selectedNode.NodeName}", this);
            }
        }

        /// <summary>
        /// Event handler for node unlocked.
        /// </summary>
        private void OnNodeUnlocked(SkillNode node)
        {
            // Update UI for this node
            if (_nodeUIElements.TryGetValue(node.NodeId, out var nodeUI))
            {
                nodeUI.SetUnlocked(true, false);
            }

            // Refresh other nodes (prerequisites might now be met)
            RefreshNodeStates();
        }

        /// <summary>
        /// Event handler for skill points changed.
        /// </summary>
        private void OnSkillPointsChanged(int newAmount)
        {
            UpdateSkillPointsDisplay();
            RefreshNodeStates();
        }

        /// <summary>
        /// Refreshes unlock state for all nodes.
        /// </summary>
        private void RefreshNodeStates()
        {
            foreach (var kvp in _nodeUIElements)
            {
                var nodeId = kvp.Key;
                var nodeUI = kvp.Value;

                bool isUnlocked = _skillTreeManager.IsNodeUnlocked(nodeId);
                bool canUnlock = _skillTreeManager.CanUnlockNode(nodeId);

                nodeUI.SetUnlocked(isUnlocked, canUnlock);
            }
        }

        /// <summary>
        /// Updates skill points display.
        /// </summary>
        private void UpdateSkillPointsDisplay()
        {
            if (_skillPointsText != null && _skillTreeManager != null)
            {
                int available = _skillTreeManager.AvailableSkillPoints;
                _skillPointsText.text = $"Skill Points: {available}";
            }
        }

        /// <summary>
        /// Updates completion percentage display.
        /// </summary>
        private void UpdateCompletionDisplay()
        {
            if (_completionText != null && _skillTreeManager != null)
            {
                float completion = _skillTreeManager.GetOverallCompletion();
                _completionText.text = $"Progression Leaf: {completion * 100f:F0}% Complete";
            }
        }

        /// <summary>
        /// Closes the skill tree panel.
        /// </summary>
        private void OnCloseClicked()
        {
            if (_panelRoot != null)
                _panelRoot.SetActive(false);

            if (_nodeDetailsPanel != null)
                _nodeDetailsPanel.SetActive(false);

            _selectedNode = null;
        }

        /// <summary>
        /// Hides the panel (external call).
        /// </summary>
        public void Hide()
        {
            OnCloseClicked();
        }

        /// <summary>
        /// Quick check if panel is visible.
        /// </summary>
        public bool IsVisible()
        {
            return _panelRoot != null && _panelRoot.activeSelf;
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (_skillTreeManager != null)
            {
                _skillTreeManager.OnNodeUnlocked -= OnNodeUnlocked;
                _skillTreeManager.OnSkillPointsChanged -= OnSkillPointsChanged;
            }

            // Clean up button listeners
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(OnCloseClicked);

            if (_unlockNodeButton != null)
                _unlockNodeButton.onClick.RemoveListener(OnUnlockNodeClicked);

            // Clean up node UI listeners
            foreach (var nodeUI in _nodeUIElements.Values)
            {
                if (nodeUI != null)
                    nodeUI.OnNodeClicked -= OnNodeSelected;
            }
        }
    }
}
