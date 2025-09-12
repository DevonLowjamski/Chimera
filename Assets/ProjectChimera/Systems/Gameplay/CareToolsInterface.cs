using UnityEngine;
using UnityEngine.UIElements;
using ProjectChimera.Core.Logging;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Gameplay
{
    /// <summary>
    /// Care Tools Interface - Manages plant care tool selection and usage.
    /// Provides the interface for selecting and using various plant care tools
    /// in cultivation mode as described in the gameplay document.
    /// </summary>
    public class CareToolsInterface : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private string _toolsPanelName = "care-tools-panel";

        // Tool definitions
        private readonly ToolDefinition[] _availableTools = new ToolDefinition[]
        {
            new ToolDefinition
            {
                Name = "Watering Can",
                Description = "Water plants to maintain soil moisture",
                Icon = "watering_icon",
                ToolType = CareToolType.Watering,
                Cooldown = 2f
            },
            new ToolDefinition
            {
                Name = "Nutrient Syringe",
                Description = "Apply liquid nutrients to plants",
                Icon = "nutrient_icon",
                ToolType = CareToolType.NutrientApplication,
                Cooldown = 5f
            },
            new ToolDefinition
            {
                Name = "Pruning Shears",
                Description = "Trim and shape plant growth",
                Icon = "pruning_icon",
                ToolType = CareToolType.Pruning,
                Cooldown = 3f
            },
            new ToolDefinition
            {
                Name = "Inspection Tool",
                Description = "Examine plant health and conditions",
                Icon = "inspection_icon",
                ToolType = CareToolType.Inspection,
                Cooldown = 1f
            },
            new ToolDefinition
            {
                Name = "Training Wire",
                Description = "Guide plant growth direction",
                Icon = "training_icon",
                ToolType = CareToolType.Training,
                Cooldown = 4f
            },
            new ToolDefinition
            {
                Name = "pH Tester",
                Description = "Measure and adjust nutrient pH",
                Icon = "ph_icon",
                ToolType = CareToolType.PHAdjustment,
                Cooldown = 2f
            }
        };

        // UI elements
        private VisualElement _toolsPanel;
        private VisualElement _toolButtonsContainer;
        private Label _selectedToolLabel;
        private Label _toolDescriptionLabel;

        // Tool state
        private CareToolType _selectedTool = CareToolType.None;
        private Dictionary<CareToolType, float> _toolCooldowns = new Dictionary<CareToolType, float>();
        private bool _isToolActive = false;

        private void Awake()
        {
            InitializeUI();
            InitializeToolCooldowns();
        }

        private void Update()
        {
            UpdateToolCooldowns();
        }

        /// <summary>
        /// Initializes the care tools UI
        /// </summary>
        private void InitializeUI()
        {
            if (_uiDocument == null)
            {
                ChimeraLogger.LogWarning("[CareToolsInterface] No UI document assigned");
                return;
            }

            var root = _uiDocument.rootVisualElement;

            // Find or create tools panel
            _toolsPanel = root.Q(_toolsPanelName);
            if (_toolsPanel == null)
            {
                _toolsPanel = new VisualElement();
                _toolsPanel.name = _toolsPanelName;
                _toolsPanel.AddToClassList("care-tools-panel");

                CreateToolsUI();
                root.Add(_toolsPanel);
            }
            else
            {
                // Find existing UI elements
                FindExistingUIElements();
            }

            // Initially hide the panel
            _toolsPanel.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Creates the care tools UI elements
        /// </summary>
        private void CreateToolsUI()
        {
            // Tool buttons container
            _toolButtonsContainer = new VisualElement();
            _toolButtonsContainer.AddToClassList("tool-buttons-container");

            // Create buttons for each tool
            foreach (var tool in _availableTools)
            {
                var toolButton = CreateToolButton(tool);
                _toolButtonsContainer.Add(toolButton);
            }

            // Selected tool info
            var infoContainer = new VisualElement();
            infoContainer.AddToClassList("tool-info-container");

            _selectedToolLabel = new Label();
            _selectedToolLabel.text = "No tool selected";
            _selectedToolLabel.AddToClassList("selected-tool-label");

            _toolDescriptionLabel = new Label();
            _toolDescriptionLabel.text = "Select a tool to begin caring for your plants";
            _toolDescriptionLabel.AddToClassList("tool-description-label");

            infoContainer.Add(_selectedToolLabel);
            infoContainer.Add(_toolDescriptionLabel);

            _toolsPanel.Add(_toolButtonsContainer);
            _toolsPanel.Add(infoContainer);
        }

        /// <summary>
        /// Creates a tool button for a specific tool
        /// </summary>
        private Button CreateToolButton(ToolDefinition tool)
        {
            var button = new Button();
            button.text = tool.Name;
            button.tooltip = tool.Description;
            button.AddToClassList("tool-button");
            button.clicked += () => OnToolSelected(tool);

            return button;
        }

        /// <summary>
        /// Finds existing UI elements
        /// </summary>
        private void FindExistingUIElements()
        {
            _toolButtonsContainer = _toolsPanel.Q("tool-buttons-container");
            _selectedToolLabel = _toolsPanel.Q<Label>("selected-tool-label");
            _toolDescriptionLabel = _toolsPanel.Q<Label>("tool-description-label");
        }

        /// <summary>
        /// Initializes tool cooldowns
        /// </summary>
        private void InitializeToolCooldowns()
        {
            foreach (var tool in _availableTools)
            {
                _toolCooldowns[tool.ToolType] = 0f;
            }
        }

        /// <summary>
        /// Updates tool cooldowns
        /// </summary>
        private void UpdateToolCooldowns()
        {
            var keys = new List<CareToolType>(_toolCooldowns.Keys);
            foreach (var toolType in keys)
            {
                if (_toolCooldowns[toolType] > 0f)
                {
                    _toolCooldowns[toolType] -= Time.deltaTime;
                    if (_toolCooldowns[toolType] < 0f)
                    {
                        _toolCooldowns[toolType] = 0f;
                    }
                }
            }
        }

        /// <summary>
        /// Handles tool selection
        /// </summary>
        private void OnToolSelected(ToolDefinition tool)
        {
            // Check if tool is on cooldown
            if (_toolCooldowns[tool.ToolType] > 0f)
            {
                ChimeraLogger.Log($"[CareToolsInterface] Tool {tool.Name} is on cooldown: {_toolCooldowns[tool.ToolType]:F1}s remaining");
                return;
            }

            _selectedTool = tool.ToolType;

            if (_selectedToolLabel != null)
                _selectedToolLabel.text = $"Selected: {tool.Name}";

            if (_toolDescriptionLabel != null)
                _toolDescriptionLabel.text = tool.Description;

            _isToolActive = true;

            ChimeraLogger.Log($"[CareToolsInterface] Selected tool: {tool.Name}");
        }

        /// <summary>
        /// Shows the care tools panel
        /// </summary>
        public void ShowToolsPanel()
        {
            if (_toolsPanel != null)
            {
                _toolsPanel.style.display = DisplayStyle.Flex;
                UpdateToolStates();
            }
        }

        /// <summary>
        /// Hides the care tools panel
        /// </summary>
        public void HideToolsPanel()
        {
            if (_toolsPanel != null)
            {
                _toolsPanel.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// Updates the visual state of tool buttons based on cooldowns
        /// </summary>
        private void UpdateToolStates()
        {
            if (_toolButtonsContainer == null) return;

            var toolButtons = _toolButtonsContainer.Query<Button>().ToList();

            for (int i = 0; i < toolButtons.Count && i < _availableTools.Length; i++)
            {
                var button = toolButtons[i];
                var tool = _availableTools[i];

                bool isOnCooldown = _toolCooldowns[tool.ToolType] > 0f;

                if (isOnCooldown)
                {
                    button.SetEnabled(false);
                    button.text = $"{tool.Name} ({_toolCooldowns[tool.ToolType]:F1}s)";
                }
                else
                {
                    button.SetEnabled(true);
                    button.text = tool.Name;
                }

                // Highlight selected tool
                if (_selectedTool == tool.ToolType && _isToolActive)
                {
                    button.AddToClassList("selected-tool");
                }
                else
                {
                    button.RemoveFromClassList("selected-tool");
                }
            }
        }

        /// <summary>
        /// Uses the currently selected tool on a target
        /// </summary>
        public bool UseTool(GameObject target)
        {
            if (!_isToolActive || _selectedTool == CareToolType.None)
            {
                ChimeraLogger.Log("[CareToolsInterface] No tool selected");
                return false;
            }

            // Check cooldown
            if (_toolCooldowns[_selectedTool] > 0f)
            {
                ChimeraLogger.Log("[CareToolsInterface] Tool is on cooldown");
                return false;
            }

            // Apply tool effect
            bool success = ApplyToolEffect(_selectedTool, target);

            if (success)
            {
                // Start cooldown
                var toolDef = System.Array.Find(_availableTools, t => t.ToolType == _selectedTool);
                if (toolDef != null)
                {
                    _toolCooldowns[_selectedTool] = toolDef.Cooldown;
                }

                UpdateToolStates();
                ChimeraLogger.Log($"[CareToolsInterface] Used {_selectedTool} on {target.name}");
            }

            return success;
        }

        /// <summary>
        /// Applies the tool effect to the target
        /// </summary>
        private bool ApplyToolEffect(CareToolType toolType, GameObject target)
        {
            // This would integrate with the actual plant care systems
            // For now, just log the action
            switch (toolType)
            {
                case CareToolType.Watering:
                    // Apply water to plant
                    ChimeraLogger.Log($"[CareToolsInterface] Watering plant: {target.name}");
                    return true;

                case CareToolType.NutrientApplication:
                    // Apply nutrients to plant
                    ChimeraLogger.Log($"[CareToolsInterface] Applying nutrients to plant: {target.name}");
                    return true;

                case CareToolType.Pruning:
                    // Prune plant
                    ChimeraLogger.Log($"[CareToolsInterface] Pruning plant: {target.name}");
                    return true;

                case CareToolType.Inspection:
                    // Inspect plant
                    ChimeraLogger.Log($"[CareToolsInterface] Inspecting plant: {target.name}");
                    return true;

                case CareToolType.Training:
                    // Train plant
                    ChimeraLogger.Log($"[CareToolsInterface] Training plant: {target.name}");
                    return true;

                case CareToolType.PHAdjustment:
                    // Adjust pH
                    ChimeraLogger.Log($"[CareToolsInterface] Adjusting pH for plant: {target.name}");
                    return true;

                default:
                    ChimeraLogger.LogWarning($"[CareToolsInterface] Unknown tool type: {toolType}");
                    return false;
            }
        }

        /// <summary>
        /// Gets the currently selected tool
        /// </summary>
        public CareToolType GetSelectedTool()
        {
            return _selectedTool;
        }

        /// <summary>
        /// Checks if a tool is currently active
        /// </summary>
        public bool IsToolActive()
        {
            return _isToolActive;
        }

        /// <summary>
        /// Deselects the current tool
        /// </summary>
        public void DeselectTool()
        {
            _selectedTool = CareToolType.None;
            _isToolActive = false;

            if (_selectedToolLabel != null)
                _selectedToolLabel.text = "No tool selected";

            if (_toolDescriptionLabel != null)
                _toolDescriptionLabel.text = "Select a tool to begin caring for your plants";

            UpdateToolStates();
        }

        /// <summary>
        /// Gets the list of available tools
        /// </summary>
        public ToolDefinition[] GetAvailableTools()
        {
            return _availableTools;
        }

        /// <summary>
        /// Checks if the tools panel is visible
        /// </summary>
        public bool IsPanelVisible()
        {
            return _toolsPanel != null &&
                   _toolsPanel.style.display == DisplayStyle.Flex;
        }
    }

    /// <summary>
    /// Tool definition structure
    /// </summary>
    public struct ToolDefinition
    {
        public string Name;
        public string Description;
        public string Icon;
        public CareToolType ToolType;
        public float Cooldown;
    }

    /// <summary>
    /// Care tool types
    /// </summary>
    public enum CareToolType
    {
        None,
        Watering,
        NutrientApplication,
        Pruning,
        Inspection,
        Training,
        PHAdjustment
    }
}
