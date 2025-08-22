using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Manages menu configurations, available modes, and menu history.
    /// Extracted from ContextualMenuStateManager.cs to reduce file size and improve separation of concerns.
    /// </summary>
    public class MenuConfigurationManager
    {
        // Menu Configuration
        private readonly Dictionary<string, MenuConfig> _menuConfigs = new Dictionary<string, MenuConfig>();
        private readonly HashSet<string> _availableModes = new HashSet<string>();
        private readonly Dictionary<string, List<string>> _menuHistory = new Dictionary<string, List<string>>();
        
        public IEnumerable<string> AvailableModes => _availableModes;
        
        public MenuConfigurationManager()
        {
            InitializeDefaultConfigs();
        }
        
        /// <summary>
        /// Initializes default menu configurations for each mode
        /// </summary>
        private void InitializeDefaultConfigs()
        {
            // Construction Mode
            var constructionConfig = new MenuConfig
            {
                Mode = "construction",
                AutoCloseOnSelection = true,
                AllowMultipleSelection = false,
                MaxMenuItems = 12,
                DefaultPosition = MenuPosition.Cursor,
                TransitionType = MenuTransition.Fade,
                TransitionDuration = 0.2f
            };
            RegisterMode("construction", constructionConfig);
            
            // Cultivation Mode
            var cultivationConfig = new MenuConfig
            {
                Mode = "cultivation",
                AutoCloseOnSelection = false,
                AllowMultipleSelection = true,
                MaxMenuItems = 8,
                DefaultPosition = MenuPosition.Fixed,
                TransitionType = MenuTransition.Slide,
                TransitionDuration = 0.25f
            };
            RegisterMode("cultivation", cultivationConfig);
            
            // Genetics Mode
            var geneticsConfig = new MenuConfig
            {
                Mode = "genetics",
                AutoCloseOnSelection = true,
                AllowMultipleSelection = false,
                MaxMenuItems = 10,
                DefaultPosition = MenuPosition.Context,
                TransitionType = MenuTransition.Scale,
                TransitionDuration = 0.15f
            };
            RegisterMode("genetics", geneticsConfig);
            
            Debug.Log($"[MenuConfigurationManager] Initialized {_menuConfigs.Count} default configurations");
        }
        
        /// <summary>
        /// Gets menu configuration for a mode
        /// </summary>
        public MenuConfig GetMenuConfig(string mode)
        {
            return _menuConfigs.TryGetValue(mode, out var config) ? config : CreateDefaultConfig(mode);
        }
        
        /// <summary>
        /// Registers a new menu mode with configuration
        /// </summary>
        public void RegisterMode(string mode, MenuConfig config)
        {
            if (string.IsNullOrEmpty(mode) || config == null)
            {
                Debug.LogWarning("[MenuConfigurationManager] Invalid mode or config parameters");
                return;
            }
            
            _menuConfigs[mode] = config;
            _availableModes.Add(mode);
            
            Debug.Log($"[MenuConfigurationManager] Registered mode: {mode}");
        }
        
        /// <summary>
        /// Unregisters a menu mode
        /// </summary>
        public bool UnregisterMode(string mode)
        {
            if (string.IsNullOrEmpty(mode))
            {
                return false;
            }
            
            var removed = _menuConfigs.Remove(mode) && _availableModes.Remove(mode);
            if (removed)
            {
                _menuHistory.Remove(mode);
                Debug.Log($"[MenuConfigurationManager] Unregistered mode: {mode}");
            }
            
            return removed;
        }
        
        /// <summary>
        /// Checks if a mode is available
        /// </summary>
        public bool IsModeAvailable(string mode)
        {
            return !string.IsNullOrEmpty(mode) && _availableModes.Contains(mode);
        }
        
        /// <summary>
        /// Updates configuration for an existing mode
        /// </summary>
        public bool UpdateModeConfig(string mode, MenuConfig config)
        {
            if (!IsModeAvailable(mode) || config == null)
            {
                return false;
            }
            
            _menuConfigs[mode] = config;
            Debug.Log($"[MenuConfigurationManager] Updated config for mode: {mode}");
            return true;
        }
        
        /// <summary>
        /// Gets all configured modes with their configurations
        /// </summary>
        public Dictionary<string, MenuConfig> GetAllConfigurations()
        {
            return new Dictionary<string, MenuConfig>(_menuConfigs);
        }
        
        /// <summary>
        /// Adds mode to access history
        /// </summary>
        public void AddToHistory(string mode)
        {
            if (string.IsNullOrEmpty(mode))
            {
                return;
            }
            
            if (!_menuHistory.ContainsKey(mode))
            {
                _menuHistory[mode] = new List<string>();
            }
            
            var history = _menuHistory[mode];
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            
            if (history.Count == 0 || history[history.Count - 1] != timestamp)
            {
                history.Add(timestamp);
                
                // Limit history size
                const int maxHistorySize = 10;
                if (history.Count > maxHistorySize)
                {
                    history.RemoveAt(0);
                }
            }
        }
        
        /// <summary>
        /// Gets menu access history for a mode
        /// </summary>
        public List<string> GetMenuHistory(string mode)
        {
            return _menuHistory.TryGetValue(mode, out var history) ? new List<string>(history) : new List<string>();
        }
        
        /// <summary>
        /// Clears history for a specific mode or all modes
        /// </summary>
        public void ClearHistory(string mode = null)
        {
            if (string.IsNullOrEmpty(mode))
            {
                _menuHistory.Clear();
                Debug.Log("[MenuConfigurationManager] Cleared all menu history");
            }
            else if (_menuHistory.Remove(mode))
            {
                Debug.Log($"[MenuConfigurationManager] Cleared history for mode: {mode}");
            }
        }
        
        /// <summary>
        /// Gets default position based on position type
        /// </summary>
        public (float x, float y) GetDefaultPosition(string positionType, float currentX = 0f, float currentY = 0f)
        {
            switch (positionType)
            {
                case MenuPosition.Cursor:
                    var mousePos = UnityEngine.Input.mousePosition;
                    return (mousePos.x, mousePos.y);
                case MenuPosition.Center:
                    return (UnityEngine.Screen.width / 2f, UnityEngine.Screen.height / 2f);
                case MenuPosition.Fixed:
                    return (100f, 100f);
                case MenuPosition.Context:
                    return (currentX, currentY); // Use provided current position
                default:
                    return (0f, 0f);
            }
        }
        
        /// <summary>
        /// Validates a menu configuration
        /// </summary>
        public bool ValidateConfig(MenuConfig config)
        {
            if (config == null)
            {
                return false;
            }
            
            // Check required fields
            if (string.IsNullOrEmpty(config.Mode))
            {
                return false;
            }
            
            // Check valid values
            if (config.MaxMenuItems <= 0 || config.TransitionDuration < 0f)
            {
                return false;
            }
            
            // Check valid position type
            var validPositions = new[] { MenuPosition.Cursor, MenuPosition.Center, MenuPosition.Fixed, MenuPosition.Context };
            if (!validPositions.Contains(config.DefaultPosition))
            {
                return false;
            }
            
            // Check valid transition type
            var validTransitions = new[] { MenuTransition.None, MenuTransition.Fade, MenuTransition.Slide, MenuTransition.Scale };
            if (!validTransitions.Contains(config.TransitionType))
            {
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Creates a default configuration for an unknown mode
        /// </summary>
        private MenuConfig CreateDefaultConfig(string mode)
        {
            return new MenuConfig
            {
                Mode = mode,
                AutoCloseOnSelection = true,
                AllowMultipleSelection = false,
                MaxMenuItems = 8,
                DefaultPosition = MenuPosition.Cursor,
                TransitionType = MenuTransition.Fade,
                TransitionDuration = 0.2f
            };
        }
        
        /// <summary>
        /// Gets configuration statistics
        /// </summary>
        public MenuConfigStats GetStats()
        {
            return new MenuConfigStats
            {
                RegisteredModeCount = _menuConfigs.Count,
                AvailableModeCount = _availableModes.Count,
                TotalHistoryEntries = _menuHistory.Values.Sum(h => h.Count),
                ModesWithHistory = _menuHistory.Keys.Count
            };
        }
    }
    
    /// <summary>
    /// Statistics about menu configuration
    /// </summary>
    public class MenuConfigStats
    {
        public int RegisteredModeCount { get; set; }
        public int AvailableModeCount { get; set; }
        public int TotalHistoryEntries { get; set; }
        public int ModesWithHistory { get; set; }
    }
}