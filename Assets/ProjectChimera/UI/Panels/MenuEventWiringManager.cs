using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Manages event wiring between UI elements and command handlers for contextual menus.
    /// Extracted from ContextualMenuEventHandler.cs to reduce file size and improve separation of concerns.
    /// </summary>
    public class MenuEventWiringManager
    {
        // Event wiring storage
        private readonly Dictionary<VisualElement, string> _elementToCommandMap = new Dictionary<VisualElement, string>();
        private readonly Dictionary<VisualElement, Action> _elementEventHandlers = new Dictionary<VisualElement, Action>();
        private readonly List<VisualElement> _wiredElements = new List<VisualElement>();
        
        // Events
        public event Action<string, string> OnMenuItemClicked;
        public event Action<string> OnMenuOpened;
        public event Action<string> OnMenuClosed;
        
        // State
        private string _currentMode = "none";
        private bool _eventsEnabled = true;
        
        public string CurrentMode => _currentMode;
        public bool EventsEnabled => _eventsEnabled;
        public int WiredElementCount => _wiredElements.Count;
        
        /// <summary>
        /// Wires click events to a visual element
        /// </summary>
        public void WireClickEvent(VisualElement element, string commandId, string mode = null)
        {
            if (element == null || string.IsNullOrEmpty(commandId))
            {
                ChimeraLogger.LogWarning("[MenuEventWiringManager] Invalid element or command ID");
                return;
            }
            
            // Unwire existing events if already wired
            if (_elementToCommandMap.ContainsKey(element))
            {
                UnwireClickEvent(element);
            }
            
            // Store command mapping
            _elementToCommandMap[element] = commandId;
            
            // Create event handler
            Action eventHandler = () => HandleMenuItemClick(commandId, mode ?? _currentMode);
            _elementEventHandlers[element] = eventHandler;
            
            // Wire the click event based on element type
            if (element is Button button)
            {
                button.clicked += eventHandler;
            }
            else
            {
                element.RegisterCallback<ClickEvent>(evt => eventHandler());
            }
            
            // Track wired element
            if (!_wiredElements.Contains(element))
            {
                _wiredElements.Add(element);
            }
            
            ChimeraLogger.Log($"[MenuEventWiringManager] Wired click event: {commandId} → {element.name}");
        }
        
        /// <summary>
        /// Unwires click events from a visual element
        /// </summary>
        public void UnwireClickEvent(VisualElement element)
        {
            if (element == null) return;
            
            // Remove from tracking
            _elementToCommandMap.Remove(element);
            _wiredElements.Remove(element);
            
            // Unwire event handler if stored
            if (_elementEventHandlers.TryGetValue(element, out var handler))
            {
                if (element is Button button)
                {
                    button.clicked -= handler;
                }
                // Note: For ClickEvent, Unity UI Toolkit doesn't provide easy unregistration
                // In a production system, you'd need to store the event registration tokens
                
                _elementEventHandlers.Remove(element);
            }
            
            ChimeraLogger.Log($"[MenuEventWiringManager] Unwired click event for element: {element.name}");
        }
        
        /// <summary>
        /// Wires multiple elements to commands
        /// </summary>
        public void WireMultipleElements(Dictionary<VisualElement, string> elementCommandMap, string mode = null)
        {
            foreach (var kvp in elementCommandMap)
            {
                WireClickEvent(kvp.Key, kvp.Value, mode);
            }
            
            ChimeraLogger.Log($"[MenuEventWiringManager] Wired {elementCommandMap.Count} elements");
        }
        
        /// <summary>
        /// Unwires all elements
        /// </summary>
        public void UnwireAllElements()
        {
            var elementsToUnwire = new List<VisualElement>(_wiredElements);
            
            foreach (var element in elementsToUnwire)
            {
                UnwireClickEvent(element);
            }
            
            ChimeraLogger.Log($"[MenuEventWiringManager] Unwired {elementsToUnwire.Count} elements");
        }
        
        /// <summary>
        /// Handles menu item click events
        /// </summary>
        private void HandleMenuItemClick(string commandId, string mode)
        {
            if (!_eventsEnabled)
            {
                ChimeraLogger.LogWarning("[MenuEventWiringManager] Events are disabled");
                return;
            }
            
            try
            {
                // Fire the click event
                OnMenuItemClicked?.Invoke(mode, commandId);
                
                ChimeraLogger.Log($"[MenuEventWiringManager] Menu item clicked: {commandId} in mode {mode}");
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[MenuEventWiringManager] Error handling menu item click: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Sets the current mode for event handling
        /// </summary>
        public void SetMode(string mode)
        {
            if (string.IsNullOrEmpty(mode))
            {
                mode = "none";
            }
            
            var oldMode = _currentMode;
            _currentMode = mode;
            
            ChimeraLogger.Log($"[MenuEventWiringManager] Mode changed from {oldMode} to {mode}");
        }
        
        /// <summary>
        /// Enables or disables event handling
        /// </summary>
        public void SetEventsEnabled(bool enabled)
        {
            _eventsEnabled = enabled;
            ChimeraLogger.Log($"[MenuEventWiringManager] Events {(enabled ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// Gets the command ID for a wired element
        /// </summary>
        public string GetCommandForElement(VisualElement element)
        {
            return _elementToCommandMap.GetValueOrDefault(element);
        }
        
        /// <summary>
        /// Checks if an element is wired
        /// </summary>
        public bool IsElementWired(VisualElement element)
        {
            return _elementToCommandMap.ContainsKey(element);
        }
        
        /// <summary>
        /// Gets all wired elements
        /// </summary>
        public List<VisualElement> GetWiredElements()
        {
            return new List<VisualElement>(_wiredElements);
        }
        
        /// <summary>
        /// Gets all wired elements for a specific command
        /// </summary>
        public List<VisualElement> GetElementsForCommand(string commandId)
        {
            var elements = new List<VisualElement>();
            
            foreach (var kvp in _elementToCommandMap)
            {
                if (kvp.Value == commandId)
                {
                    elements.Add(kvp.Key);
                }
            }
            
            return elements;
        }
        
        /// <summary>
        /// Fires menu opened event
        /// </summary>
        public void FireMenuOpened(string mode)
        {
            OnMenuOpened?.Invoke(mode);
            ChimeraLogger.Log($"[MenuEventWiringManager] Menu opened in mode: {mode}");
        }
        
        /// <summary>
        /// Fires menu closed event
        /// </summary>
        public void FireMenuClosed(string mode)
        {
            OnMenuClosed?.Invoke(mode);
            ChimeraLogger.Log($"[MenuEventWiringManager] Menu closed for mode: {mode}");
        }
        
        /// <summary>
        /// Gets wiring statistics
        /// </summary>
        public WiringStats GetWiringStats()
        {
            return new WiringStats
            {
                WiredElementCount = _wiredElements.Count,
                CommandMappingCount = _elementToCommandMap.Count,
                EventHandlerCount = _elementEventHandlers.Count,
                CurrentMode = _currentMode,
                EventsEnabled = _eventsEnabled
            };
        }
        
        /// <summary>
        /// Clears all event handlers and mappings
        /// </summary>
        public void Clear()
        {
            UnwireAllElements();
            _elementToCommandMap.Clear();
            _elementEventHandlers.Clear();
            _wiredElements.Clear();
            _currentMode = "none";
            _eventsEnabled = true;
            
            ChimeraLogger.Log("[MenuEventWiringManager] Cleared all event handlers and mappings");
        }
    }
    
    /// <summary>
    /// Statistics about event wiring
    /// </summary>
    public class WiringStats
    {
        public int WiredElementCount { get; set; }
        public int CommandMappingCount { get; set; }
        public int EventHandlerCount { get; set; }
        public string CurrentMode { get; set; }
        public bool EventsEnabled { get; set; }
    }
}