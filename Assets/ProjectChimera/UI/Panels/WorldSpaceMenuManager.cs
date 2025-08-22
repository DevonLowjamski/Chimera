using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Manages world space menu configuration, positioning, and visibility updates.
    /// Extracted from WorldSpaceMenuRenderer.cs to reduce file size and improve separation of concerns.
    /// </summary>
    public class WorldSpaceMenuManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private WorldSpaceMenuConfig _config = new WorldSpaceMenuConfig();
        [SerializeField] private Camera _targetCamera;
        
        [Header("Spatial Positioning")]
        [SerializeField] private WorldSpaceSpatialPositioner _spatialPositioner;
        [SerializeField] private bool _useSpatialPositioning = true;
        
        [Header("Menu Templates")]
        [SerializeField] private VisualTreeAsset _facilityMenuTemplate;
        [SerializeField] private VisualTreeAsset _plantMenuTemplate;
        [SerializeField] private VisualTreeAsset _equipmentMenuTemplate;
        
        // Active menu tracking
        private readonly Dictionary<GameObject, UIDocument> _activeMenus = new Dictionary<GameObject, UIDocument>();
        private readonly Dictionary<GameObject, WorldSpaceMenuData> _menuData = new Dictionary<GameObject, WorldSpaceMenuData>();
        
        public WorldSpaceMenuConfig Config => _config;
        public Camera TargetCamera => _targetCamera;
        public int ActiveMenuCount => _activeMenus.Count;
        
        private void Awake()
        {
            if (_targetCamera == null)
                _targetCamera = Camera.main;
            
            // Initialize spatial positioner if not assigned
            if (_spatialPositioner == null && _useSpatialPositioning)
            {
                var positionerObject = new GameObject("SpatialPositioner");
                positionerObject.transform.SetParent(transform);
                _spatialPositioner = positionerObject.AddComponent<WorldSpaceSpatialPositioner>();
            }
        }
        
        /// <summary>
        /// Registers an active menu
        /// </summary>
        public void RegisterMenu(GameObject target, UIDocument menuDocument, WorldSpaceMenuData menuData)
        {
            if (target == null || menuDocument == null || menuData == null) return;
            
            _activeMenus[target] = menuDocument;
            _menuData[target] = menuData;
        }
        
        /// <summary>
        /// Unregisters an active menu
        /// </summary>
        public bool UnregisterMenu(GameObject target)
        {
            if (target == null) return false;
            
            var removed = _activeMenus.Remove(target);
            _menuData.Remove(target);
            
            return removed;
        }
        
        /// <summary>
        /// Gets menu document for target
        /// </summary>
        public UIDocument GetMenuDocument(GameObject target)
        {
            return _activeMenus.TryGetValue(target, out var document) ? document : null;
        }
        
        /// <summary>
        /// Gets menu data for target
        /// </summary>
        public WorldSpaceMenuData GetMenuData(GameObject target)
        {
            return _menuData.TryGetValue(target, out var data) ? data : null;
        }
        
        /// <summary>
        /// Gets the appropriate template for a menu type
        /// </summary>
        public VisualTreeAsset GetTemplateForMenuType(WorldSpaceMenuType menuType)
        {
            return menuType switch
            {
                WorldSpaceMenuType.Facility => _facilityMenuTemplate,
                WorldSpaceMenuType.Plant => _plantMenuTemplate,
                WorldSpaceMenuType.Equipment => _equipmentMenuTemplate,
                _ => _facilityMenuTemplate
            };
        }
        
        /// <summary>
        /// Configures a menu document with template and items
        /// </summary>
        public bool ConfigureMenuDocument(UIDocument document, GameObject target, WorldSpaceMenuType menuType, List<string> menuItems)
        {
            var template = GetTemplateForMenuType(menuType);
            if (template == null)
            {
                Debug.LogWarning($"[WorldSpaceMenuManager] No template found for menu type: {menuType}");
                return false;
            }
            
            document.visualTreeAsset = template;
            document.gameObject.SetActive(true);
            
            PopulateMenuItems(document, target, menuItems);
            SetMenuTitle(document, target, menuType);
            
            return true;
        }
        
        /// <summary>
        /// Populates menu items in the UI document
        /// </summary>
        private void PopulateMenuItems(UIDocument document, GameObject target, List<string> menuItems)
        {
            var root = document.rootVisualElement;
            var menuContainer = root.Q<VisualElement>("menu-container");
            
            if (menuContainer != null)
            {
                menuContainer.Clear();
                
                foreach (var item in menuItems)
                {
                    var menuButton = CreateMenuButton(item, target);
                    menuContainer.Add(menuButton);
                }
            }
        }
        
        /// <summary>
        /// Creates a menu button for an item
        /// </summary>
        private Button CreateMenuButton(string itemText, GameObject target)
        {
            var button = new Button();
            button.text = itemText;
            button.AddToClassList("world-menu-item");
            
            button.clicked += () => {
                OnMenuItemSelected?.Invoke(target, itemText);
                Debug.Log($"[WorldSpaceMenuManager] Menu item selected: {itemText} for {target.name}");
            };
            
            return button;
        }
        
        /// <summary>
        /// Sets the menu title
        /// </summary>
        private void SetMenuTitle(UIDocument document, GameObject target, WorldSpaceMenuType menuType)
        {
            var titleLabel = document.rootVisualElement.Q<Label>("menu-title");
            if (titleLabel != null)
            {
                titleLabel.text = GetMenuTitle(target, menuType);
            }
        }
        
        /// <summary>
        /// Gets the appropriate title for a menu
        /// </summary>
        private string GetMenuTitle(GameObject target, WorldSpaceMenuType menuType)
        {
            return menuType switch
            {
                WorldSpaceMenuType.Facility => $"Facility: {target.name}",
                WorldSpaceMenuType.Plant => $"Plant: {target.name}",
                WorldSpaceMenuType.Equipment => $"Equipment: {target.name}",
                _ => target.name
            };
        }
        
        /// <summary>
        /// Updates all active world space menus
        /// </summary>
        public List<GameObject> UpdateActiveMenus()
        {
            if (_targetCamera == null) return new List<GameObject>();
            
            var menusToRemove = new List<GameObject>();
            
            foreach (var kvp in _activeMenus)
            {
                var target = kvp.Key;
                var menuDocument = kvp.Value;
                
                if (target == null)
                {
                    menusToRemove.Add(target);
                    continue;
                }
                
                UpdateMenuPosition(target, menuDocument);
                UpdateMenuVisibility(target, menuDocument);
            }
            
            return menusToRemove;
        }
        
        /// <summary>
        /// Updates the position of a world space menu
        /// </summary>
        public void UpdateMenuPosition(GameObject target, UIDocument menuDocument)
        {
            if (target == null || menuDocument == null || _targetCamera == null) return;
            
            Vector3 menuPosition;
            
            // Use spatial positioning if enabled and available
            if (_useSpatialPositioning && _spatialPositioner != null && _menuData.TryGetValue(target, out var menuData))
            {
                // Calculate menu bounds for spatial positioning
                var rootElement = menuDocument.rootVisualElement;
                var menuSize = new Vector3(
                    rootElement.resolvedStyle.width / 100f, // Convert to world units
                    rootElement.resolvedStyle.height / 100f,
                    0.1f // Depth
                );
                
                menuPosition = _spatialPositioner.CalculateOptimalPosition(target, menuData.MenuType, menuSize);
            }
            else
            {
                // Fallback to basic positioning
                menuPosition = target.transform.position + _config.menuOffset;
            }
            
            var cameraPosition = _targetCamera.transform.position;
            var distance = Vector3.Distance(menuPosition, cameraPosition);
            
            // Position the menu
            menuDocument.transform.position = menuPosition;
            
            // Billboard behavior
            if (_config.billboardMode)
            {
                var lookDirection = (cameraPosition - menuPosition).normalized;
                menuDocument.transform.rotation = Quaternion.LookRotation(lookDirection);
            }
            
            // Adaptive scaling based on distance
            if (_config.adaptiveScaling)
            {
                var scaleFactor = _config.distanceScaleCurve.Evaluate(distance) * _config.menuScale;
                menuDocument.transform.localScale = Vector3.one * scaleFactor;
            }
        }
        
        /// <summary>
        /// Updates menu visibility based on distance and occlusion
        /// </summary>
        public void UpdateMenuVisibility(GameObject target, UIDocument menuDocument)
        {
            if (target == null || menuDocument == null || _targetCamera == null) return;
            
            var canvasGroup = menuDocument.GetComponent<CanvasGroup>();
            if (canvasGroup == null) return;
            
            var targetPosition = target.transform.position + _config.menuOffset;
            var cameraPosition = _targetCamera.transform.position;
            var distance = Vector3.Distance(targetPosition, cameraPosition);
            
            float alpha = CalculateDistanceFade(distance);
            
            // Occlusion check
            if (_config.enableDepthOcclusion && alpha > 0f)
            {
                alpha *= CalculateOcclusionFade(cameraPosition, targetPosition, distance);
            }
            
            canvasGroup.alpha = Mathf.Clamp01(alpha);
        }
        
        /// <summary>
        /// Calculates alpha based on distance
        /// </summary>
        private float CalculateDistanceFade(float distance)
        {
            if (distance > _config.fadeDistance)
            {
                return Mathf.Lerp(1f, 0f, (distance - _config.fadeDistance) / (_config.fadeDistance * 0.5f));
            }
            return 1f;
        }
        
        /// <summary>
        /// Calculates alpha based on occlusion
        /// </summary>
        private float CalculateOcclusionFade(Vector3 cameraPosition, Vector3 targetPosition, float distance)
        {
            var direction = (targetPosition - cameraPosition).normalized;
            if (Physics.Raycast(cameraPosition, direction, distance, _config.facilityLayers))
            {
                return 0.3f; // Reduce alpha when occluded
            }
            return 1f;
        }
        
        /// <summary>
        /// Gets all active world space menus
        /// </summary>
        public Dictionary<GameObject, WorldSpaceMenuData> GetActiveMenus()
        {
            return new Dictionary<GameObject, WorldSpaceMenuData>(_menuData);
        }
        
        /// <summary>
        /// Gets list of active menu targets
        /// </summary>
        public List<GameObject> GetActiveTargets()
        {
            return new List<GameObject>(_activeMenus.Keys);
        }
        
        /// <summary>
        /// Checks if a target has an active menu
        /// </summary>
        public bool HasActiveMenu(GameObject target)
        {
            return target != null && _activeMenus.ContainsKey(target);
        }
        
        /// <summary>
        /// Clears all active menus (without cleanup - for external cleanup)
        /// </summary>
        public void ClearAllMenus()
        {
            // Clear spatial positioning data
            if (_spatialPositioner != null)
            {
                foreach (var target in _activeMenus.Keys)
                {
                    _spatialPositioner.ClearPositionData(target);
                }
            }
            
            _activeMenus.Clear();
            _menuData.Clear();
        }
        
        /// <summary>
        /// Enables or disables spatial positioning
        /// </summary>
        public void SetSpatialPositioning(bool enabled)
        {
            _useSpatialPositioning = enabled;
        }
        
        /// <summary>
        /// Gets spatial positioning statistics
        /// </summary>
        public SpatialStats GetSpatialStats()
        {
            return _spatialPositioner?.GetSpatialStats() ?? new SpatialStats();
        }
        
        // Events
        public event Action<GameObject, string> OnMenuItemSelected;
    }
}