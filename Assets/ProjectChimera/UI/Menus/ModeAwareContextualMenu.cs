using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.UI;
using ProjectChimera.Data.Events;
using System.Collections.Generic;

namespace ProjectChimera.UI.Menus
{
    /// <summary>
    /// DEPRECATED: Mode-aware contextual menu has been broken down into focused components.
    /// This file now serves as a reference point for the decomposed menu system structure.
    ///
    /// New Component Structure:
    /// - MenuCore.cs: Core menu infrastructure and component coordination
    /// - MenuActionProvider.cs: Context-aware menu actions and validation
    /// - MenuRenderer.cs: Visual rendering and positioning of menus
    /// - MenuInputHandler.cs: Input detection and processing
    /// - MenuAnimationController.cs: Menu animations and visual effects
    /// </summary>

    // The ModeAwareContextualMenu functionality has been moved to focused component files.
    // This file is kept for reference and to prevent breaking changes during migration.
    //
    // To use the new component structure, inherit from MenuCore:
    //
    // public class ModeAwareContextualMenu : MenuCore
    // {
    //     // Your custom contextual menu implementation
    // }
    //
    // The following classes are now available in their focused components:
    //
    // From MenuCore.cs:
    // - MenuCore (base class with core functionality)
    // - Menu initialization and component orchestration
    // - Event handling and state management
    //
    // From MenuActionProvider.cs:
    // - MenuActionProvider (context-aware menu actions)
    // - Action validation and execution delegation
    // - Mode and object-specific menu item filtering
    //
    // From MenuRenderer.cs:
    // - MenuRenderer (visual rendering and positioning)
    // - Menu button creation and styling
    // - Screen positioning and layout management
    //
    // From MenuInputHandler.cs:
    // - MenuInputHandler (input detection and processing)
    // - Mouse, keyboard, and touch input handling
    // - Gesture recognition and shortcut support
    //
    // From MenuAnimationController.cs:
    // - MenuAnimationController (animations and visual effects)
    // - Fade, scale, and slide animations
    // - Advanced effects and particle systems

    /// <summary>
    /// Concrete implementation of ModeAwareContextualMenu using the new component structure.
    /// Inherits all functionality from MenuCore and specialized components.
    /// </summary>
    public class ModeAwareContextualMenu : MenuCore, IContextualMenuProvider
    {
        [Header("Mode-Specific Menus")]
        [SerializeField] private GameObject _cultivationContextMenu;
        [SerializeField] private GameObject _constructionContextMenu;
        [SerializeField] private GameObject _geneticsContextMenu;

        [Header("Object-Specific Menus")]
        [SerializeField] private GameObject _plantContextMenu;
        [SerializeField] private GameObject _equipmentContextMenu;
        [SerializeField] private GameObject _facilityContextMenu;

        [Header("Right-Click Menu")]
        [SerializeField] private GameObject _rightClickMenuPanel;
        [SerializeField] private Transform _rightClickItemsContainer;
        [SerializeField] private UnityEngine.UI.Button _rightClickItemPrefab;

        // Legacy support properties
        public GameObject CultivationContextMenu => _cultivationContextMenu;
        public GameObject ConstructionContextMenu => _constructionContextMenu;
        public GameObject GeneticsContextMenu => _geneticsContextMenu;
        public GameObject PlantContextMenu => _plantContextMenu;
        public GameObject EquipmentContextMenu => _equipmentContextMenu;
        public GameObject FacilityContextMenu => _facilityContextMenu;

        protected override void InitializeMenu()
        {
            base.InitializeMenu();

            // Initialize mode-specific menus
            UpdateModeSpecificMenus(_currentMode);
        }

        protected override void OnModeChanged(Data.Events.ModeChangeEventData eventData)
        {
            base.OnModeChanged(eventData);

            // Update mode-specific menu visibility
            UpdateModeSpecificMenus(eventData.NewMode);
        }

        private void UpdateModeSpecificMenus(GameplayMode mode)
        {
            // Show/hide mode-specific context menu panels
            if (_cultivationContextMenu != null)
            {
                _cultivationContextMenu.SetActive(mode == GameplayMode.Cultivation);
            }

            if (_constructionContextMenu != null)
            {
                _constructionContextMenu.SetActive(mode == GameplayMode.Construction);
            }

            if (_geneticsContextMenu != null)
            {
                _geneticsContextMenu.SetActive(mode == GameplayMode.Genetics);
            }
        }

        protected override void HideAllMenus()
        {
            base.HideAllMenus();

            // Hide mode-specific menus
            if (_rightClickMenuPanel != null) _rightClickMenuPanel.SetActive(false);
            if (_cultivationContextMenu != null) _cultivationContextMenu.SetActive(false);
            if (_constructionContextMenu != null) _constructionContextMenu.SetActive(false);
            if (_geneticsContextMenu != null) _geneticsContextMenu.SetActive(false);
            if (_plantContextMenu != null) _plantContextMenu.SetActive(false);
            if (_equipmentContextMenu != null) _equipmentContextMenu.SetActive(false);
            if (_facilityContextMenu != null) _facilityContextMenu.SetActive(false);
        }

        /// <summary>
        /// Legacy support method for backward compatibility
        /// </summary>
        public void ShowRightClickMenu(Vector3 screenPosition, GameObject targetObject = null)
        {
            // Delegate to the main context menu system
            ShowContextMenu(screenPosition, targetObject);
        }

        /// <summary>
        /// Legacy support method for backward compatibility
        /// </summary>
        public void HideRightClickMenu()
        {
            HideContextMenu();
        }

        /// <summary>
        /// Hide the context menu if visible (IContextualMenuProvider implementation)
        /// </summary>
        public void HideMenu()
        {
            HideContextMenu();
        }

        /// <summary>
        /// Get the currently active menu items
        /// </summary>
        public List<ContextMenuItem> GetCurrentMenuItems()
        {
            return _currentMenuItems;
        }

        /// <summary>
        /// Check if a specific menu type is currently visible
        /// </summary>
        public bool IsMenuTypeVisible(string menuType)
        {
            return menuType switch
            {
                "Cultivation" => _cultivationContextMenu != null && _cultivationContextMenu.activeInHierarchy,
                "Construction" => _constructionContextMenu != null && _constructionContextMenu.activeInHierarchy,
                "Genetics" => _geneticsContextMenu != null && _geneticsContextMenu.activeInHierarchy,
                "Plant" => _plantContextMenu != null && _plantContextMenu.activeInHierarchy,
                "Equipment" => _equipmentContextMenu != null && _equipmentContextMenu.activeInHierarchy,
                "Facility" => _facilityContextMenu != null && _facilityContextMenu.activeInHierarchy,
                _ => false
            };
        }

        /// <summary>
        /// Enable or disable specific menu types
        /// </summary>
        public void SetMenuTypeEnabled(string menuType, bool enabled)
        {
            GameObject targetMenu = menuType switch
            {
                "Cultivation" => _cultivationContextMenu,
                "Construction" => _constructionContextMenu,
                "Genetics" => _geneticsContextMenu,
                "Plant" => _plantContextMenu,
                "Equipment" => _equipmentContextMenu,
                "Facility" => _facilityContextMenu,
                _ => null
            };

            if (targetMenu != null)
            {
                targetMenu.SetActive(enabled);
                LogDebug($"{menuType} menu {(enabled ? "enabled" : "disabled")}");
            }
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Editor-only method for testing context menu
        /// </summary>
        [ContextMenu("Test Context Menu")]
        private void TestContextMenu()
        {
            if (Application.isPlaying)
            {
                Vector3 testPosition = new Vector3(Screen.width / 2, Screen.height / 2, 0);
                ShowContextMenu(testPosition);
            }
            else
            {
                ChimeraLogger.Log("[ModeAwareContextualMenu] Test only works during play mode");
            }
        }
        #endif
    }
}
