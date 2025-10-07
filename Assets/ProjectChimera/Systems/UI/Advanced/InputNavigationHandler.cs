using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.UI.Advanced
{
    /// <summary>
    /// BASIC: Simple input navigation for Project Chimera's UI system.
    /// Focuses on essential navigation without complex navigation modes and focus management.
    /// </summary>
    public class InputNavigationHandler : MonoBehaviour
    {
        [Header("Basic Navigation Settings")]
        [SerializeField] private bool _enableBasicNavigation = true;
        [SerializeField] private float _navigationCooldown = 0.2f;

        // Basic navigation state
        private readonly List<VisualElement> _navigableElements = new List<VisualElement>();
        private int _currentIndex = -1;
        private float _lastNavigationTime = 0f;
        private bool _isInitialized = false;

        /// <summary>
        /// Initialize basic navigation
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;
        }

        /// <summary>
        /// Handle basic navigation input
        /// </summary>
        public void HandleNavigation(Vector2 input)
        {
            if (!_enableBasicNavigation || !_isInitialized) return;
            if (Time.time - _lastNavigationTime < _navigationCooldown) return;
            if (_navigableElements.Count == 0) return;

            // Handle vertical navigation
            if (input.y > 0.5f) // Up
            {
                NavigateUp();
            }
            else if (input.y < -0.5f) // Down
            {
                NavigateDown();
            }

            // Handle horizontal navigation
            if (input.x > 0.5f) // Right
            {
                NavigateNext();
            }
            else if (input.x < -0.5f) // Left
            {
                NavigatePrevious();
            }

            _lastNavigationTime = Time.time;
        }

        /// <summary>
        /// Handle tab navigation
        /// </summary>
        public void HandleTabNavigation(bool shiftPressed)
        {
            if (!_enableBasicNavigation || !_isInitialized) return;
            if (Time.time - _lastNavigationTime < _navigationCooldown) return;

            if (shiftPressed)
            {
                NavigatePrevious();
            }
            else
            {
                NavigateNext();
            }

            _lastNavigationTime = Time.time;
        }

        /// <summary>
        /// Select current element
        /// </summary>
        public void SelectCurrentElement()
        {
            if (_currentIndex >= 0 && _currentIndex < _navigableElements.Count)
            {
                var element = _navigableElements[_currentIndex];
                // Simulate click on element
                if (element != null)
                {
                    element.Focus();
                    // Trigger click event if element supports it
                    var clickable = element.GetFirstOfType<Clickable>();
                    if (clickable != null)
                    {
                        // Simulate a click event
                        using (var evt = new ClickEvent() { target = element })
                        {
                            element.SendEvent(evt);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Register navigable element
        /// </summary>
        public void RegisterElement(VisualElement element)
        {
            if (element != null && !_navigableElements.Contains(element))
            {
                _navigableElements.Add(element);
                if (_currentIndex == -1)
                {
                    _currentIndex = 0;
                    UpdateFocus();
                }
            }
        }

        /// <summary>
        /// Unregister navigable element
        /// </summary>
        public void UnregisterElement(VisualElement element)
        {
            if (element != null)
            {
                int index = _navigableElements.IndexOf(element);
                _navigableElements.Remove(element);

                if (index == _currentIndex)
                {
                    _currentIndex = Mathf.Min(_currentIndex, _navigableElements.Count - 1);
                    UpdateFocus();
                }
                else if (index < _currentIndex)
                {
                    _currentIndex--;
                }
            }
        }

        /// <summary>
        /// Clear all elements
        /// </summary>
        public void ClearElements()
        {
            _navigableElements.Clear();
            _currentIndex = -1;
        }

        /// <summary>
        /// Get current focused element
        /// </summary>
        public VisualElement GetCurrentElement()
        {
            if (_currentIndex >= 0 && _currentIndex < _navigableElements.Count)
            {
                return _navigableElements[_currentIndex];
            }
            return null;
        }

        /// <summary>
        /// Get navigation info
        /// </summary>
        public NavigationInfo GetNavigationInfo()
        {
            return new NavigationInfo
            {
                TotalElements = _navigableElements.Count,
                CurrentIndex = _currentIndex,
                IsNavigationEnabled = _enableBasicNavigation,
                CooldownRemaining = Mathf.Max(0, _navigationCooldown - (Time.time - _lastNavigationTime))
            };
        }

        #region Private Methods

        private void NavigateUp()
        {
            // Simple up navigation - could be extended for grid layouts
            NavigatePrevious();
        }

        private void NavigateDown()
        {
            // Simple down navigation - could be extended for grid layouts
            NavigateNext();
        }

        private void NavigateNext()
        {
            if (_navigableElements.Count == 0) return;

            _currentIndex = (_currentIndex + 1) % _navigableElements.Count;
            UpdateFocus();
        }

        private void NavigatePrevious()
        {
            if (_navigableElements.Count == 0) return;

            _currentIndex = (_currentIndex - 1 + _navigableElements.Count) % _navigableElements.Count;
            UpdateFocus();
        }

        private void UpdateFocus()
        {
            // Clear previous focus
            foreach (var element in _navigableElements)
            {
                if (element != null)
                {
                    element.RemoveFromClassList("navigation-focused");
                }
            }

            // Set new focus
            if (_currentIndex >= 0 && _currentIndex < _navigableElements.Count)
            {
                var currentElement = _navigableElements[_currentIndex];
                if (currentElement != null)
                {
                    currentElement.AddToClassList("navigation-focused");
                    currentElement.Focus();
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Navigation information
    /// </summary>
    [System.Serializable]
    public struct NavigationInfo
    {
        public int TotalElements;
        public int CurrentIndex;
        public bool IsNavigationEnabled;
        public float CooldownRemaining;
    }
}
