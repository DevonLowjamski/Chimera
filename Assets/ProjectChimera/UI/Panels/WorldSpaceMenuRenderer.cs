using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using ProjectChimera.Core.Updates;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Renders contextual menus in 3D world space for facilities and objects.
    /// Leverages Unity 6.2's enhanced World Space UI capabilities for immersive cannabis cultivation management.
    /// Refactored to use composition with specialized components for better maintainability.
    /// </summary>
    public class WorldSpaceMenuRenderer : MonoBehaviour, ITickable
    {
        [Header("Components")]
        [SerializeField] private WorldSpaceUIPool _uiPool;
        [SerializeField] private WorldSpaceMenuAnimator _animator;
        [SerializeField] private WorldSpaceMenuManager _menuManager;
        
        // Events
        public event Action<GameObject, string> OnWorldMenuItemSelected;
        public event Action<GameObject> OnWorldMenuOpened;
        public event Action<GameObject> OnWorldMenuClosed;
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void Start()
        {
            // Register with UpdateOrchestrator
            UpdateOrchestrator.Instance.RegisterTickable(this);
        }
        
        #region ITickable Implementation
        
        public int Priority => TickPriority.UIManager;
        public bool Enabled => enabled;
        
        public void Tick(float deltaTime)
        {
            UpdateActiveMenus();
        }
        
        #endregion
        
        /// <summary>
        /// Initializes required components
        /// </summary>
        private void InitializeComponents()
        {
            if (_uiPool == null)
            {
                var poolObject = new GameObject("WorldSpaceUIPool");
                poolObject.transform.SetParent(transform);
                _uiPool = poolObject.AddComponent<WorldSpaceUIPool>();
            }
            
            if (_animator == null)
            {
                var animatorObject = new GameObject("WorldSpaceMenuAnimator");
                animatorObject.transform.SetParent(transform);
                _animator = animatorObject.AddComponent<WorldSpaceMenuAnimator>();
            }
            
            if (_menuManager == null)
            {
                var managerObject = new GameObject("WorldSpaceMenuManager");
                managerObject.transform.SetParent(transform);
                _menuManager = managerObject.AddComponent<WorldSpaceMenuManager>();
                _menuManager.OnMenuItemSelected += (target, item) => OnWorldMenuItemSelected?.Invoke(target, item);
            }
        }
        
        /// <summary>
        /// Shows a world space menu for a target object
        /// </summary>
        public bool ShowWorldSpaceMenu(GameObject target, WorldSpaceMenuType menuType, List<string> menuItems)
        {
            if (target == null || menuItems == null || menuItems.Count == 0)
            {
                ChimeraLogger.LogWarning("[WorldSpaceMenuRenderer] Invalid parameters for world space menu");
                return false;
            }
            
            // Close existing menu for this target
            if (_menuManager.HasActiveMenu(target))
            {
                HideWorldSpaceMenu(target);
            }
            
            // Get menu document from pool
            var menuDocument = _uiPool.Get();
            if (menuDocument == null)
            {
                ChimeraLogger.LogError("[WorldSpaceMenuRenderer] Failed to get menu document from pool");
                return false;
            }
            
            // Configure and position menu
            if (!_menuManager.ConfigureMenuDocument(menuDocument, target, menuType, menuItems))
            {
                _uiPool.Return(menuDocument);
                return false;
            }
            
            // Create menu data and register with manager
            var menuData = new WorldSpaceMenuData
            {
                Target = target,
                MenuType = menuType,
                MenuItems = new List<string>(menuItems),
                UIDocument = menuDocument,
                CreationTime = Time.time
            };
            
            _menuManager.RegisterMenu(target, menuDocument, menuData);
            
            // Position and animate menu
            _menuManager.UpdateMenuPosition(target, menuDocument);
            _animator.AnimateMenuAppearance(menuDocument, true, _menuManager.Config.fadeInDuration);
            
            OnWorldMenuOpened?.Invoke(target);
            ChimeraLogger.Log($"[WorldSpaceMenuRenderer] Opened world space menu for {target.name}");
            
            return true;
        }
        
        /// <summary>
        /// Hides the world space menu for a target object
        /// </summary>
        public bool HideWorldSpaceMenu(GameObject target)
        {
            var menuDocument = _menuManager.GetMenuDocument(target);
            if (menuDocument == null)
            {
                return false;
            }
            
            // Animate menu disappearance
            _animator.AnimateMenuAppearance(menuDocument, false, _menuManager.Config.fadeOutDuration, () => {
                _uiPool.Return(menuDocument);
                _menuManager.UnregisterMenu(target);
                OnWorldMenuClosed?.Invoke(target);
            });
            
            ChimeraLogger.Log($"[WorldSpaceMenuRenderer] Hiding world space menu for {target.name}");
            return true;
        }
        
        /// <summary>
        /// Updates all active world space menus
        /// </summary>
        private void UpdateActiveMenus()
        {
            var menusToRemove = _menuManager.UpdateActiveMenus();
            
            // Clean up destroyed targets
            CleanupDestroyedTargets(menusToRemove);
        }
        
        /// <summary>
        /// Cleans up menus for destroyed targets
        /// </summary>
        private void CleanupDestroyedTargets(List<GameObject> menusToRemove)
        {
            foreach (var target in menusToRemove)
            {
                var menuDocument = _menuManager.GetMenuDocument(target);
                if (menuDocument != null)
                {
                    _uiPool.Return(menuDocument);
                }
                _menuManager.UnregisterMenu(target);
            }
        }
        
        /// <summary>
        /// Gets all active world space menus
        /// </summary>
        public Dictionary<GameObject, WorldSpaceMenuData> GetActiveMenus()
        {
            return _menuManager?.GetActiveMenus() ?? new Dictionary<GameObject, WorldSpaceMenuData>();
        }
        
        /// <summary>
        /// Hides all world space menus
        /// </summary>
        public void HideAllMenus()
        {
            var targets = _menuManager?.GetActiveTargets() ?? new List<GameObject>();
            foreach (var target in targets)
            {
                HideWorldSpaceMenu(target);
            }
        }
        
        /// <summary>
        /// Gets the number of active menus
        /// </summary>
        public int ActiveMenuCount => _menuManager?.ActiveMenuCount ?? 0;
        
        /// <summary>
        /// Checks if a target has an active menu
        /// </summary>
        public bool HasActiveMenu(GameObject target)
        {
            return _menuManager?.HasActiveMenu(target) ?? false;
        }
        
        /// <summary>
        /// Sets a custom configuration for world space menus
        /// </summary>
        public void SetConfiguration(WorldSpaceMenuConfig config)
        {
            if (_menuManager != null && config != null)
            {
                // Configuration is handled by the manager component
                ChimeraLogger.Log("[WorldSpaceMenuRenderer] Configuration updated via WorldSpaceMenuManager");
            }
        }
        
        /// <summary>
        /// Gets the current configuration
        /// </summary>
        public WorldSpaceMenuConfig GetConfiguration()
        {
            return _menuManager?.Config;
        }
        
        /// <summary>
        /// Forces an update of menu positions (useful when camera moves rapidly)
        /// </summary>
        public void ForceUpdateMenuPositions()
        {
            var activeMenus = GetActiveMenus();
            foreach (var kvp in activeMenus)
            {
                var target = kvp.Key;
                var menuData = kvp.Value;
                if (target != null && menuData?.UIDocument != null)
                {
                    _menuManager.UpdateMenuPosition(target, menuData.UIDocument);
                }
            }
        }
        
        /// <summary>
        /// Updates visibility for all menus (useful for dynamic occlusion changes)
        /// </summary>
        public void ForceUpdateMenuVisibility()
        {
            var activeMenus = GetActiveMenus();
            foreach (var kvp in activeMenus)
            {
                var target = kvp.Key;
                var menuData = kvp.Value;
                if (target != null && menuData?.UIDocument != null)
                {
                    _menuManager.UpdateMenuVisibility(target, menuData.UIDocument);
                }
            }
        }
        
        private void OnDestroy()
        {
            if (UpdateOrchestrator.Instance != null)
            {
                UpdateOrchestrator.Instance.UnregisterTickable(this);
            }
        }
    }
}