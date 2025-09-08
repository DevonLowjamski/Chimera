using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.UI.Components;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Simple test script to validate ContextualMenuController basic functionality.
    /// Tests open/close menu operations and component delegation.
    /// </summary>
    public class ContextualMenuControllerTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private ContextualMenuController _menuController;
        [SerializeField] private bool _runTestsOnStart = false;
        [SerializeField] private bool _enableTestLogging = true;
        
        private void Start()
        {
            if (_runTestsOnStart)
            {
                TestBasicFunctionality();
            }
        }
        
        [ContextMenu("Test Basic Functionality")]
        public void TestBasicFunctionality()
        {
            if (_menuController == null)
            {
                _menuController = ServiceContainerFactory.Instance?.TryResolve<ContextualMenuController>();
            }
            
            if (_menuController == null)
            {
                LogTest("ERROR: ContextualMenuController not found");
                return;
            }
            
            LogTest("=== ContextualMenuController Basic Functionality Test ===");
            
            // Test 1: Component initialization
            LogTest("Test 1: Component Initialization");
            bool initialState = _menuController.IsMenuOpen;
            string initialMode = _menuController.CurrentMode;
            LogTest($"Initial state - IsOpen: {initialState}, Mode: {initialMode}");
            
            // Test 2: Open menu
            LogTest("Test 2: Opening Construction Menu");
            _menuController.OpenMenu("construction", new Vector2(100, 100));
            LogTest($"After open - IsOpen: {_menuController.IsMenuOpen}, Mode: {_menuController.CurrentMode}");
            
            // Test 3: Select menu item
            LogTest("Test 3: Selecting Menu Item");
            _menuController.SelectMenuItem("build_wall");
            
            // Test 4: Close menu
            LogTest("Test 4: Closing Menu");
            _menuController.CloseMenu();
            LogTest($"After close - IsOpen: {_menuController.IsMenuOpen}, Mode: {_menuController.CurrentMode}");
            
            // Test 5: Notification system
            LogTest("Test 5: Notification System");
            _menuController.ShowNotification("Test notification", UIStatus.Info, 3f);
            _menuController.ShowNotificationEnhanced("Test Title", "Enhanced notification test", UIStatus.Success, 3f);
            _menuController.ShowPersistentNotification("test_key", "Persistent test notification", UIStatus.Warning);
            _menuController.DismissPersistentNotification("test_key");
            
            // Test 6: Update controls
            LogTest("Test 6: Update Controls");
            _menuController.SetUpdatesPaused(true);
            _menuController.SetUpdatesPaused(false);
            
            LogTest("=== All Basic Functionality Tests Completed ===");
        }
        
        [ContextMenu("Test Mode-Specific Menus")]
        public void TestModeSpecificMenus()
        {
            if (_menuController == null) return;
            
            LogTest("=== Testing Mode-Specific Menu Operations ===");
            
            // Test construction menu
            LogTest("Testing Construction Menu");
            _menuController.OpenMenu("construction");
            _menuController.SelectMenuItem("build_wall");
            _menuController.CloseMenu();
            
            // Test cultivation menu  
            LogTest("Testing Cultivation Menu");
            _menuController.OpenMenu("cultivation");
            _menuController.SelectMenuItem("water_plant");
            _menuController.CloseMenu();
            
            // Test genetics menu
            LogTest("Testing Genetics Menu");
            _menuController.OpenMenu("genetics");
            _menuController.SelectMenuItem("analyze_genes");
            _menuController.CloseMenu();
            
            LogTest("=== Mode-Specific Tests Completed ===");
        }
        
        private void LogTest(string message)
        {
            if (_enableTestLogging)
            {
                ChimeraLogger.Log($"[ContextualMenuTest] {message}");
            }
        }
    }
}