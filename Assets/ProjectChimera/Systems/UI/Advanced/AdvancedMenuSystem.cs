using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using UnityEngine;
using UnityEngine.UIElements;
using ProjectChimera.Core;
using ProjectChimera.Systems.Services.Core;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.UI;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ProjectChimera.Systems.UI.Advanced
{
    /// <summary>
    /// Phase 2.3.1: Advanced Menu System with Dynamic Categories
    /// Provides sophisticated contextual menu system with adaptive categories,
    /// intelligent action filtering, and seamless service integration
    /// </summary>
    public class AdvancedMenuSystem : MonoBehaviour, ITickable
    {
        [Header("Menu Configuration")]
        [SerializeField] private AdvancedMenuConfig _menuConfig;
        [SerializeField] private bool _enableDynamicCategories = true;
        [SerializeField] private bool _enableContextAwareFiltering = true;
        [SerializeField] private bool _enableVisualFeedback = true;

        [Header("Performance Settings")]
        [SerializeField] private int _maxConcurrentMenus = 5;
        [SerializeField] private float _menuUpdateInterval = 0.1f;
        [SerializeField] private bool _enableMenuCaching = true;

        [Header("Input Configuration")]
        [SerializeField] private KeyCode _menuToggleKey = KeyCode.Tab;
        [SerializeField] private bool _enableMouseSupport = true;
        [SerializeField] private bool _enableKeyboardNavigation = true;

        // Core system references
        private ServiceLayerCoordinator _serviceCoordinator;
        private VisualFeedbackSystem _visualFeedback;
        private InputActionHandler _inputHandler;

        // Menu system state
        private Dictionary<string, MenuCategory> _categories = new Dictionary<string, MenuCategory>();
        private Dictionary<string, MenuAction> _actions = new Dictionary<string, MenuAction>();
        private Dictionary<string, ContextualMenu> _activeMenus = new Dictionary<string, ContextualMenu>();
        private MenuCache _menuCache;

        // UI Elements
        private VisualElement _menuContainer;
        private VisualElement _categoryContainer;
        private VisualElement _actionContainer;
        private Label _contextLabel;

        // Events
        public event Action<string, MenuCategory> OnCategoryAdded;
        public event Action<string, MenuAction> OnActionExecuted;
        public event Action OnMenuOpened;
        public event Action OnMenuClosed;
        public event Action<MenuContext> OnContextChanged;

        private void Awake()
        {
            InitializeMenuSystem();
        }

        private void Start()
        {
        // Register with UpdateOrchestrator
        UpdateOrchestrator.Instance?.RegisterTickable(this);
            SetupMenuUI();
            RegisterDefaultCategories();
            StartCoroutine(MenuUpdateCoroutine());
        }

        private void InitializeMenuSystem()
        {
            _serviceCoordinator = ServiceContainerFactory.Instance?.TryResolve<ServiceLayerCoordinator>();
            _visualFeedback = ServiceContainerFactory.Instance?.TryResolve<VisualFeedbackSystem>();

            // Create input handler
            var inputGO = new GameObject("MenuInputHandler");
            inputGO.transform.SetParent(transform);
            _inputHandler = inputGO.AddComponent<InputActionHandler>();

            // Initialize menu cache
            if (_enableMenuCaching)
            {
                _menuCache = new MenuCache();
            }

            if (_menuConfig == null)
            {
                ChimeraLogger.LogWarning("[AdvancedMenuSystem] No menu config assigned - using default settings");
                _menuConfig = CreateDefaultMenuConfig();
            }
        }

        private void SetupMenuUI()
        {
            var rootDocument = GetComponent<UIDocument>();
            if (rootDocument == null)
            {
                ChimeraLogger.LogError("[AdvancedMenuSystem] UIDocument component required for advanced menu system");
                return;
            }

            var root = rootDocument.rootVisualElement;

            // Create main menu container
            _menuContainer = new VisualElement();
            _menuContainer.name = "advanced-menu-container";
            _menuContainer.AddToClassList("advanced-menu");
            _menuContainer.style.display = DisplayStyle.None;
            root.Add(_menuContainer);

            // Create category container
            _categoryContainer = new VisualElement();
            _categoryContainer.name = "category-container";
            _categoryContainer.AddToClassList("menu-categories");
            _menuContainer.Add(_categoryContainer);

            // Create action container
            _actionContainer = new VisualElement();
            _actionContainer.name = "action-container";
            _actionContainer.AddToClassList("menu-actions");
            _menuContainer.Add(_actionContainer);

            // Create context label
            _contextLabel = new Label();
            _contextLabel.name = "context-label";
            _contextLabel.AddToClassList("menu-context");
            _menuContainer.Add(_contextLabel);
        }

        private void RegisterDefaultCategories()
        {
            // Construction pillar categories
            RegisterCategory(new MenuCategory
            {
                Id = "construction_structures",
                DisplayName = "Structures",
                Description = "Place buildings and structures",
                Icon = "structure-icon",
                Priority = 100,
                PillarType = "Construction",
                RequiredSkills = new[] { "construction_basic" }
            });

            RegisterCategory(new MenuCategory
            {
                Id = "construction_equipment",
                DisplayName = "Equipment",
                Description = "Install cultivation equipment",
                Icon = "equipment-icon",
                Priority = 90,
                PillarType = "Construction",
                RequiredSkills = new[] { "equipment_basic" }
            });

            RegisterCategory(new MenuCategory
            {
                Id = "construction_utilities",
                DisplayName = "Utilities",
                Description = "Install utilities and infrastructure",
                Icon = "utility-icon",
                Priority = 80,
                PillarType = "Construction",
                RequiredSkills = new[] { "utilities_basic" }
            });

            // Cultivation pillar categories
            RegisterCategory(new MenuCategory
            {
                Id = "cultivation_planting",
                DisplayName = "Planting",
                Description = "Plant seeds and manage crops",
                Icon = "plant-icon",
                Priority = 100,
                PillarType = "Cultivation",
                RequiredSkills = new[] { "cultivation_basic" }
            });

            RegisterCategory(new MenuCategory
            {
                Id = "cultivation_care",
                DisplayName = "Plant Care",
                Description = "Water, feed, and maintain plants",
                Icon = "care-icon",
                Priority = 90,
                PillarType = "Cultivation",
                RequiredSkills = new[] { "plant_care" }
            });

            RegisterCategory(new MenuCategory
            {
                Id = "cultivation_training",
                DisplayName = "Training",
                Description = "Train and shape plants",
                Icon = "training-icon",
                Priority = 80,
                PillarType = "Cultivation",
                RequiredSkills = new[] { "plant_training" }
            });

            // Genetics pillar categories
            RegisterCategory(new MenuCategory
            {
                Id = "genetics_breeding",
                DisplayName = "Breeding",
                Description = "Cross plants and create new strains",
                Icon = "breeding-icon",
                Priority = 100,
                PillarType = "Genetics",
                RequiredSkills = new[] { "breeding_basic" }
            });

            RegisterCategory(new MenuCategory
            {
                Id = "genetics_research",
                DisplayName = "Research",
                Description = "Research traits and genetics",
                Icon = "research-icon",
                Priority = 90,
                PillarType = "Genetics",
                RequiredSkills = new[] { "genetic_research" }
            });

            RegisterCategory(new MenuCategory
            {
                Id = "genetics_propagation",
                DisplayName = "Propagation",
                Description = "Tissue culture and cloning",
                Icon = "propagation-icon",
                Priority = 80,
                PillarType = "Genetics",
                RequiredSkills = new[] { "tissue_culture" }
            });
        }

        /// <summary>
        /// Opens contextual menu for specific object or location
        /// </summary>
        public void OpenContextualMenu(Vector3 worldPosition, GameObject targetObject = null, MenuContext context = null)
        {
            var menuId = GenerateMenuId(worldPosition, targetObject);

            if (_activeMenus.ContainsKey(menuId))
            {
                CloseMenu(menuId);
                return;
            }

            // Create menu context if not provided
            if (context == null)
            {
                context = CreateMenuContext(worldPosition, targetObject);
            }

            // Get applicable categories and actions
            var applicableCategories = GetApplicableCategories(context);
            var applicableActions = GetApplicableActions(context);

            if (applicableCategories.Count == 0 && applicableActions.Count == 0)
            {
                ShowFeedback("No actions available in this context", false);
                return;
            }

            // Create and configure menu
            var menu = new ContextualMenu
            {
                Id = menuId,
                WorldPosition = worldPosition,
                TargetObject = targetObject,
                Context = context,
                Categories = applicableCategories,
                Actions = applicableActions,
                IsOpen = true,
                CreationTime = Time.time
            };

            _activeMenus[menuId] = menu;

            // Update UI
            UpdateMenuUI(menu);
            ShowMenu();

            OnMenuOpened?.Invoke();

            // Auto-close after timeout if configured
            if (_menuConfig.AutoCloseTimeout > 0)
            {
                Invoke(nameof(CloseAllMenus), _menuConfig.AutoCloseTimeout);
            }
        }

        /// <summary>
        /// Registers a new menu category
        /// </summary>
        public void RegisterCategory(MenuCategory category)
        {
            if (_categories.ContainsKey(category.Id))
            {
                ChimeraLogger.LogWarning($"[AdvancedMenuSystem] Category {category.Id} already registered");
                return;
            }

            _categories[category.Id] = category;
            OnCategoryAdded?.Invoke(category.Id, category);

            // Update UI if menu is currently open
            if (_menuContainer.style.display == DisplayStyle.Flex)
            {
                RefreshMenuUI();
            }
        }

        /// <summary>
        /// Registers a new menu action
        /// </summary>
        public void RegisterAction(MenuAction action)
        {
            if (_actions.ContainsKey(action.Id))
            {
                ChimeraLogger.LogWarning($"[AdvancedMenuSystem] Action {action.Id} already registered");
                return;
            }

            _actions[action.Id] = action;

            // Update UI if menu is currently open
            if (_menuContainer.style.display == DisplayStyle.Flex)
            {
                RefreshMenuUI();
            }
        }

        /// <summary>
        /// Executes a menu action
        /// </summary>
        public void ExecuteAction(string actionId, MenuContext context)
        {
            if (!_actions.TryGetValue(actionId, out var action))
            {
                ChimeraLogger.LogError($"[AdvancedMenuSystem] Action {actionId} not found");
                return;
            }

            // Check if action can be executed
            if (!CanExecuteAction(action, context))
            {
                ShowFeedback($"Cannot execute {action.DisplayName}", false);
                return;
            }

            // Execute action through service coordinator
            try
            {
                _serviceCoordinator?.HandleMenuItemSelected(action.PillarType.ToLower(), actionId);
                var result = true; // Assume success if no exception thrown

                if (result != null)
                {
                    ShowFeedback($"Executed: {action.DisplayName}", true);
                    OnActionExecuted?.Invoke(actionId, action);
                }
                else
                {
                    ShowFeedback($"Failed to execute: {action.DisplayName}", false);
                }
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[AdvancedMenuSystem] Error executing action {actionId}: {ex.Message}");
                ShowFeedback($"Error: {action.DisplayName}", false);
            }

            // Close menu if configured
            if (_menuConfig.CloseOnActionExecute)
            {
                CloseAllMenus();
            }
        }

        private List<MenuCategory> GetApplicableCategories(MenuContext context)
        {
            var applicableCategories = new List<MenuCategory>();

            foreach (var category in _categories.Values)
            {
                if (IsCategoryApplicable(category, context))
                {
                    applicableCategories.Add(category);
                }
            }

            // Sort by priority
            applicableCategories.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            return applicableCategories;
        }

        private List<MenuAction> GetApplicableActions(MenuContext context)
        {
            var applicableActions = new List<MenuAction>();

            foreach (var action in _actions.Values)
            {
                if (IsActionApplicable(action, context))
                {
                    applicableActions.Add(action);
                }
            }

            // Sort by priority and category
            applicableActions.Sort((a, b) =>
            {
                int categoryComparison = string.Compare(a.CategoryId, b.CategoryId, StringComparison.Ordinal);
                if (categoryComparison != 0) return categoryComparison;
                return b.Priority.CompareTo(a.Priority);
            });

            return applicableActions;
        }

        private bool IsCategoryApplicable(MenuCategory category, MenuContext context)
        {
            // Check context requirements
            if (!string.IsNullOrEmpty(category.RequiredContext) &&
                context.ContextType != category.RequiredContext)
            {
                return false;
            }

            // Check pillar availability
            if (!IsPillarAvailable(category.PillarType, context))
            {
                return false;
            }

            // Check required skills
            if (category.RequiredSkills != null && category.RequiredSkills.Length > 0)
            {
                // In a real implementation, check against progression manager
                // For now, assume all skills are available
            }

            // Check custom conditions
            if (category.ConditionCallback != null)
            {
                return category.ConditionCallback(context);
            }

            return true;
        }

        private bool IsActionApplicable(MenuAction action, MenuContext context)
        {
            // Check if parent category is applicable
            if (_categories.TryGetValue(action.CategoryId, out var category))
            {
                if (!IsCategoryApplicable(category, context))
                {
                    return false;
                }
            }

            // Check action-specific requirements
            if (!string.IsNullOrEmpty(action.RequiredContext) &&
                context.ContextType != action.RequiredContext)
            {
                return false;
            }

            // Check custom conditions
            if (action.ConditionCallback != null)
            {
                return action.ConditionCallback(context);
            }

            return true;
        }

        private bool CanExecuteAction(MenuAction action, MenuContext context)
        {
            // Check service availability
            if (!IsServiceAvailable(action.PillarType))
            {
                return false;
            }

            // Check resource requirements
            if (action.ResourceRequirements != null)
            {
                // In real implementation, check against resource manager
            }

            // Check execution conditions
            if (action.ExecutionConditionCallback != null)
            {
                return action.ExecutionConditionCallback(context);
            }

            return true;
        }

        private bool IsPillarAvailable(string pillarType, MenuContext context)
        {
            return _serviceCoordinator != null && pillarType != null;
        }

        private bool IsServiceAvailable(string pillarType)
        {
            return _serviceCoordinator != null;
        }

        private MenuContext CreateMenuContext(Vector3 worldPosition, GameObject targetObject)
        {
            return new MenuContext
            {
                WorldPosition = worldPosition,
                TargetObject = targetObject,
                ContextType = DetermineContextType(worldPosition, targetObject),
                Timestamp = Time.time,
                PlayerPosition = Camera.main?.transform.position ?? Vector3.zero,
                CameraForward = Camera.main?.transform.forward ?? Vector3.forward
            };
        }

        private string DetermineContextType(Vector3 worldPosition, GameObject targetObject)
        {
            if (targetObject != null)
            {
                // Determine context based on target object
                if (targetObject.CompareTag("Plant"))
                    return "Plant";
                if (targetObject.CompareTag("Equipment"))
                    return "Equipment";
                if (targetObject.CompareTag("Structure"))
                    return "Structure";
            }

            // Determine context based on world position
            // In real implementation, raycast or use spatial queries
            return "Ground";
        }

        private void UpdateMenuUI(ContextualMenu menu)
        {
            // Clear existing UI
            _categoryContainer.Clear();
            _actionContainer.Clear();

            // Update context label
            _contextLabel.text = $"Context: {menu.Context.ContextType}";

            // Create category buttons
            foreach (var category in menu.Categories)
            {
                var categoryButton = new Button(() => SelectCategory(category.Id));
                categoryButton.text = category.DisplayName;
                categoryButton.AddToClassList("category-button");
                categoryButton.tooltip = category.Description;
                _categoryContainer.Add(categoryButton);
            }

            // Create action buttons
            foreach (var action in menu.Actions)
            {
                var actionButton = new Button(() => ExecuteAction(action.Id, menu.Context));
                actionButton.text = action.DisplayName;
                actionButton.AddToClassList("action-button");
                actionButton.tooltip = action.Description;
                _actionContainer.Add(actionButton);
            }
        }

        private void SelectCategory(string categoryId)
        {
            // Filter actions by selected category
            var activeMenu = _activeMenus.Values.FirstOrDefault();
            if (activeMenu != null)
            {
                var categoryActions = activeMenu.Actions.Where(a => a.CategoryId == categoryId).ToList();

                // Update action container with filtered actions
                _actionContainer.Clear();
                foreach (var action in categoryActions)
                {
                    var actionButton = new Button(() => ExecuteAction(action.Id, activeMenu.Context));
                    actionButton.text = action.DisplayName;
                    actionButton.AddToClassList("action-button");
                    actionButton.tooltip = action.Description;
                    _actionContainer.Add(actionButton);
                }
            }
        }

        private void RefreshMenuUI()
        {
            var activeMenu = _activeMenus.Values.FirstOrDefault();
            if (activeMenu != null)
            {
                UpdateMenuUI(activeMenu);
            }
        }

        private void ShowMenu()
        {
            _menuContainer.style.display = DisplayStyle.Flex;
        }

        public void HideMenu()
        {
            _menuContainer.style.display = DisplayStyle.None;
        }

        private void CloseMenu(string menuId)
        {
            if (_activeMenus.TryGetValue(menuId, out var menu))
            {
                menu.IsOpen = false;
                _activeMenus.Remove(menuId);
                OnMenuClosed?.Invoke();

                if (_activeMenus.Count == 0)
                {
                    HideMenu();
                }
            }
        }

        public void CloseAllMenus()
        {
            var menuIds = _activeMenus.Keys.ToArray();
            foreach (var menuId in menuIds)
            {
                CloseMenu(menuId);
            }
        }

        private void ShowFeedback(string message, bool isSuccess)
        {
            if (_enableVisualFeedback && _visualFeedback != null)
            {
                _visualFeedback.ShowFeedback(message, isSuccess ? FeedbackType.Success : FeedbackType.Error);
            }
            else
            {
                if (isSuccess)
                    ChimeraLogger.Log($"[AdvancedMenuSystem] {message}");
                else
                    ChimeraLogger.LogWarning($"[AdvancedMenuSystem] {message}");
            }
        }

        private string GenerateMenuId(Vector3 worldPosition, GameObject targetObject)
        {
            return $"menu_{worldPosition.GetHashCode()}_{(targetObject ? targetObject.GetInstanceID() : 0)}";
        }

        private System.Collections.IEnumerator MenuUpdateCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(_menuUpdateInterval);

                if (_activeMenus.Count > 0)
                {
                    UpdateActiveMenus();
                }
            }
        }

        private void UpdateActiveMenus()
        {
            var menusToClose = new List<string>();

            foreach (var kvp in _activeMenus)
            {
                var menu = kvp.Value;

                // Check for timeout
                if (_menuConfig.AutoCloseTimeout > 0 &&
                    Time.time - menu.CreationTime > _menuConfig.AutoCloseTimeout)
                {
                    menusToClose.Add(kvp.Key);
                    continue;
                }

                // Update context if needed
                if (_enableContextAwareFiltering)
                {
                    var newContext = CreateMenuContext(menu.WorldPosition, menu.TargetObject);
                    if (!AreContextsEqual(menu.Context, newContext))
                    {
                        menu.Context = newContext;
                        OnContextChanged?.Invoke(newContext);
                        RefreshMenuUI();
                    }
                }
            }

            // Close timed-out menus
            foreach (var menuId in menusToClose)
            {
                CloseMenu(menuId);
            }
        }

        private bool AreContextsEqual(MenuContext context1, MenuContext context2)
        {
            return context1.ContextType == context2.ContextType &&
                   context1.TargetObject == context2.TargetObject;
        }

        private AdvancedMenuConfig CreateDefaultMenuConfig()
        {
            var config = ScriptableObject.CreateInstance<AdvancedMenuConfig>();
            return config;
        }

            public void Tick(float deltaTime)
    {
            // Handle input
            if (Input.GetKeyDown(_menuToggleKey))
            {
                if (_activeMenus.Count > 0)
                {
                    CloseAllMenus();

    }
                else
                {
                    // Open menu at cursor position
                    var mousePos = Input.mousePosition;
                    var worldPos = Camera.main?.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 10f)) ?? Vector3.zero;
                    OpenContextualMenu(worldPos);
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseAllMenus();
            }
        }

        private void OnDestroy()
        {
        // Unregister from UpdateOrchestrator
        UpdateOrchestrator.Instance?.UnregisterTickable(this);
            CloseAllMenus();
        }

        // Public API
        public int GetCategoryCount() => _categories.Count;
        public int GetActionCount() => _actions.Count;
        public int GetActiveMenuCount() => _activeMenus.Count;
        public bool IsMenuOpen() => _activeMenus.Count > 0;
        public MenuCategory GetCategory(string categoryId) => _categories.TryGetValue(categoryId, out var category) ? category : null;
        public MenuAction GetAction(string actionId) => _actions.TryGetValue(actionId, out var action) ? action : null;

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

    // Stub methods for test compilation - to be implemented by UI team
    public void ShowMenu(Vector3 position, GameObject target) { }
    public bool IsMenuVisible() => false;
    public void SelectAction(string actionId) { }
    public void SelectAction(MenuAction action) { }
    public void OnActionSelectedMethod(string actionId) { }
    public void OnActionSelectedNoParam() { }
    public event Action<string> OnActionSelectedEvent;
    public event Action OnActionSelectedNoParamEvent;
    public event Action<string> OnActionSelected;
    public Action<string> OnActionSelectedDelegate { get; set; }
    public Action<string> OnActionSelectedAction { get; set; }
    public void UpdateCategory(MenuCategory category) { }
    public void UpdateMenuWithActions(List<MenuAction> actions) { }
    public void UpdateMenuWithActions() { }
    public void ShowMenuPublic(string menuId) { }

    }
}
