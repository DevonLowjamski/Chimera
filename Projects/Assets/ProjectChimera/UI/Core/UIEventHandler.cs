using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Collections;
using System;

namespace ProjectChimera.UI.Core
{
    /// <summary>
    /// Handles UI events, input processing, and event routing for the UI system.
    /// Manages keyboard navigation, mouse interactions, and custom UI events.
    /// </summary>
    public class UIEventHandler
    {
        private Queue<UIEvent> _pendingEvents = new Queue<UIEvent>();
        private Dictionary<string, List<System.Action<UIEvent>>> _eventHandlers = new Dictionary<string, List<System.Action<UIEvent>>>();
        
        // Input handling
        private bool _isKeyboardNavigationEnabled = true;
        private VisualElement _currentFocusedElement;
        private List<VisualElement> _focusableElements = new List<VisualElement>();
        
        public void RegisterEventHandler(string eventType, System.Action<UIEvent> handler)
        {
            if (!_eventHandlers.ContainsKey(eventType))
            {
                _eventHandlers[eventType] = new List<System.Action<UIEvent>>();
            }
            
            _eventHandlers[eventType].Add(handler);
        }
        
        public void UnregisterEventHandler(string eventType, System.Action<UIEvent> handler)
        {
            if (_eventHandlers.TryGetValue(eventType, out var handlers))
            {
                handlers.Remove(handler);
            }
        }
        
        public void QueueEvent(UIEvent uiEvent)
        {
            _pendingEvents.Enqueue(uiEvent);
        }
        
        public void ProcessPendingEvents()
        {
            while (_pendingEvents.Count > 0)
            {
                var uiEvent = _pendingEvents.Dequeue();
                ProcessEvent(uiEvent);
            }
            
            HandleKeyboardInput();
        }
        
        private void ProcessEvent(UIEvent uiEvent)
        {
            if (_eventHandlers.TryGetValue(uiEvent.EventType, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    try
                    {
                        handler.Invoke(uiEvent);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"Error processing UI event {uiEvent.EventType}: {ex.Message}");
                    }
                }
            }
        }
        
        private void HandleKeyboardInput()
        {
            if (!_isKeyboardNavigationEnabled) return;
            
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                NavigateToNextElement();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                QueueEvent(new UIEvent("escape_pressed", null));
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                ActivateCurrentElement();
            }
        }
        
        private void NavigateToNextElement()
        {
            if (_focusableElements.Count == 0) return;
            
            int currentIndex = _currentFocusedElement != null ? 
                _focusableElements.IndexOf(_currentFocusedElement) : -1;
            
            int nextIndex = (currentIndex + 1) % _focusableElements.Count;
            var nextElement = _focusableElements[nextIndex];
            
            SetFocus(nextElement);
        }
        
        private void ActivateCurrentElement()
        {
            if (_currentFocusedElement is Button button)
            {
                // Use proper event disposal pattern
                using (var clickEvent = ClickEvent.GetPooled())
                {
                    clickEvent.target = button;
                    button.SendEvent(clickEvent);
                }
            }
            else if (_currentFocusedElement != null)
            {
                QueueEvent(new UIEvent("element_activated", _currentFocusedElement));
            }
        }
        
        public void SetFocus(VisualElement element)
        {
            if (_currentFocusedElement != null)
            {
                _currentFocusedElement.RemoveFromClassList("focused");
            }
            
            _currentFocusedElement = element;
            
            if (element != null)
            {
                element.AddToClassList("focused");
                element.Focus();
            }
        }
        
        public void RegisterFocusableElement(VisualElement element)
        {
            if (!_focusableElements.Contains(element))
            {
                _focusableElements.Add(element);
            }
        }
        
        public void UnregisterFocusableElement(VisualElement element)
        {
            _focusableElements.Remove(element);
            
            if (_currentFocusedElement == element)
            {
                _currentFocusedElement = null;
            }
        }
        
        public void EnableKeyboardNavigation(bool enabled)
        {
            _isKeyboardNavigationEnabled = enabled;
        }
        
        public void ClearFocusableElements()
        {
            _focusableElements.Clear();
            _currentFocusedElement = null;
        }
    }
    
    [System.Serializable]
    public class UIEvent
    {
        public string EventType;
        public object Data;
        public VisualElement Source;
        public System.DateTime Timestamp;
        
        public UIEvent(string eventType, object data, VisualElement source = null)
        {
            EventType = eventType;
            Data = data;
            Source = source;
            Timestamp = System.DateTime.Now;
        }
    }
}