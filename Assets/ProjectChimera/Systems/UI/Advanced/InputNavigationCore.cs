using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using System;

#if UNITY_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ProjectChimera.Systems.UI.Advanced
{
#if UNITY_INPUT_SYSTEM
    /// <summary>
    /// Core navigation functionality for advanced menu systems.
    /// Handles element registration, focus management, and navigation calculations.
    /// </summary>
    public class InputNavigationCore : MonoBehaviour
    {
        [Header("Navigation Configuration")]
        [SerializeField] private bool _enableTabNavigation = true;
        [SerializeField] private bool _enableArrowNavigation = true;
        [SerializeField, Range(0.5f, 3f)] private float _navigationSpeed = 1f;
        
        // Navigation state
        private List<VisualElement> _navigableElements = new List<VisualElement>();
        private int _currentNavigationIndex = -1;
        private VisualElement _currentFocusedElement;
        
        // Events
        public event Action<VisualElement> OnElementFocused;
        public event Action<VisualElement> OnElementSelected;
        
        // Properties
        public VisualElement CurrentFocusedElement => _currentFocusedElement;
        public int NavigableElementCount => _navigableElements.Count;
        public bool EnableTabNavigation { get => _enableTabNavigation; set => _enableTabNavigation = value; }
        public bool EnableArrowNavigation { get => _enableArrowNavigation; set => _enableArrowNavigation = value; }
        
        private void Awake()
        {
            InitializeNavigation();
        }
        
        private void InitializeNavigation()
        {
            ChimeraLogger.LogInfo("InputNavigationCore", "$1");
        }
        
        /// <summary>
        /// Register an element for keyboard/controller navigation
        /// </summary>
        public void RegisterNavigableElement(VisualElement element, int priority = 0)
        {
            if (element == null || _navigableElements.Contains(element))
                return;
            
            _navigableElements.Add(element);
            
            // Sort by priority and position
            _navigableElements.Sort((a, b) =>
            {
                var aPriority = GetElementPriority(a);
                var bPriority = GetElementPriority(b);
                
                if (aPriority != bPriority)
                    return bPriority.CompareTo(aPriority);
                
                // Sort by vertical position, then horizontal
                var aRect = a.layout;
                var bRect = b.layout;
                
                if (Mathf.Abs(aRect.y - bRect.y) > 5f)
                    return aRect.y.CompareTo(bRect.y);
                
                return aRect.x.CompareTo(bRect.x);
            });
            
            // Setup element for navigation
            SetupElementForNavigation(element);
            
            ChimeraLogger.LogInfo("InputNavigationCore", "$1");
        }
        
        /// <summary>
        /// Unregister an element from navigation
        /// </summary>
        public void UnregisterNavigableElement(VisualElement element)
        {
            if (!_navigableElements.Remove(element))
                return;
            
            if (_currentFocusedElement == element)
            {
                ClearCurrentFocus();
            }
            
            ChimeraLogger.LogInfo("InputNavigationCore", "$1");
        }
        
        /// <summary>
        /// Set focus to a specific element
        /// </summary>
        public void SetFocus(VisualElement element)
        {
            if (element == null)
            {
                ClearCurrentFocus();
                return;
            }
            
            var index = _navigableElements.IndexOf(element);
            if (index >= 0)
            {
                SetNavigationIndex(index);
            }
        }
        
        /// <summary>
        /// Navigate to the next element in sequence
        /// </summary>
        public void NavigateNext()
        {
            if (_navigableElements.Count == 0)
                return;
            
            SetNavigationIndex((_currentNavigationIndex + 1) % _navigableElements.Count);
        }
        
        /// <summary>
        /// Navigate to the previous element in sequence
        /// </summary>
        public void NavigatePrevious()
        {
            if (_navigableElements.Count == 0)
                return;
            
            SetNavigationIndex((_currentNavigationIndex - 1 + _navigableElements.Count) % _navigableElements.Count);
        }
        
        /// <summary>
        /// Navigate to the nearest element above current
        /// </summary>
        public void NavigateUp()
        {
            if (_navigableElements.Count == 0 || _currentNavigationIndex < 0)
                return;
            
            var currentElement = _navigableElements[_currentNavigationIndex];
            var bestMatch = FindNearestElement(currentElement, Vector2.up);
            
            if (bestMatch != null)
            {
                var index = _navigableElements.IndexOf(bestMatch);
                SetNavigationIndex(index);
            }
        }
        
        /// <summary>
        /// Navigate to the nearest element below current
        /// </summary>
        public void NavigateDown()
        {
            if (_navigableElements.Count == 0 || _currentNavigationIndex < 0)
                return;
            
            var currentElement = _navigableElements[_currentNavigationIndex];
            var bestMatch = FindNearestElement(currentElement, Vector2.down);
            
            if (bestMatch != null)
            {
                var index = _navigableElements.IndexOf(bestMatch);
                SetNavigationIndex(index);
            }
        }
        
        /// <summary>
        /// Navigate to the nearest element to the left
        /// </summary>
        public void NavigateLeft()
        {
            if (_navigableElements.Count == 0 || _currentNavigationIndex < 0)
                return;
            
            var currentElement = _navigableElements[_currentNavigationIndex];
            var bestMatch = FindNearestElement(currentElement, Vector2.left);
            
            if (bestMatch != null)
            {
                var index = _navigableElements.IndexOf(bestMatch);
                SetNavigationIndex(index);
            }
        }
        
        /// <summary>
        /// Navigate to the nearest element to the right
        /// </summary>
        public void NavigateRight()
        {
            if (_navigableElements.Count == 0 || _currentNavigationIndex < 0)
                return;
            
            var currentElement = _navigableElements[_currentNavigationIndex];
            var bestMatch = FindNearestElement(currentElement, Vector2.right);
            
            if (bestMatch != null)
            {
                var index = _navigableElements.IndexOf(bestMatch);
                SetNavigationIndex(index);
            }
        }
        
        /// <summary>
        /// Clear all navigation state
        /// </summary>
        public void ClearNavigation()
        {
            ClearCurrentFocus();
            _navigableElements.Clear();
            ChimeraLogger.LogInfo("InputNavigationCore", "$1");
        }
        
        /// <summary>
        /// Refresh all navigable elements from UI hierarchy
        /// </summary>
        public void RefreshNavigableElements()
        {
            _navigableElements.Clear();
            
            var rootDocument = GetComponent<UIDocument>();
            if (rootDocument != null)
            {
                var root = rootDocument.rootVisualElement;
                FindNavigableElements(root);
            }
            
            ChimeraLogger.LogInfo("InputNavigationCore", "$1");
        }
        
        /// <summary>
        /// Select/activate the currently focused element
        /// </summary>
        public void SelectCurrentElement()
        {
            if (_currentFocusedElement != null)
            {
                ExecuteElementAction(_currentFocusedElement);
                OnElementSelected?.Invoke(_currentFocusedElement);
            }
        }
        
        private void SetNavigationIndex(int index)
        {
            if (index < 0 || index >= _navigableElements.Count)
                return;
            
            // Remove focus from previous element
            if (_currentFocusedElement != null)
            {
                RemoveElementFocus(_currentFocusedElement);
            }
            
            // Set new focus
            _currentNavigationIndex = index;
            _currentFocusedElement = _navigableElements[index];
            
            ApplyElementFocus(_currentFocusedElement);
            OnElementFocused?.Invoke(_currentFocusedElement);
        }
        
        private void ClearCurrentFocus()
        {
            if (_currentFocusedElement != null)
            {
                RemoveElementFocus(_currentFocusedElement);
            }
            
            _currentFocusedElement = null;
            _currentNavigationIndex = -1;
        }
        
        private VisualElement FindNearestElement(VisualElement currentElement, Vector2 direction)
        {
            var currentRect = currentElement.layout;
            VisualElement bestMatch = null;
            float bestDistance = float.MaxValue;
            
            foreach (var element in _navigableElements)
            {
                if (element == currentElement)
                    continue;
                
                var rect = element.layout;
                var elementDirection = (rect.center - currentRect.center).normalized;
                
                // Check if element is in the desired direction
                if (Vector2.Dot(elementDirection, direction) > 0.3f)
                {
                    float distance = Vector2.Distance(rect.center, currentRect.center);
                    
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestMatch = element;
                    }
                }
            }
            
            return bestMatch;
        }
        
        private void SetupElementForNavigation(VisualElement element)
        {
            // Add navigation styling
            element.AddToClassList("navigable-element");
            
            // Setup keyboard focus
            element.focusable = true;
            element.tabIndex = GetElementPriority(element);
            
            // Setup focus events
            element.RegisterCallback<FocusInEvent>(OnElementFocusIn);
            element.RegisterCallback<FocusOutEvent>(OnElementFocusOut);
            
            // Setup keyboard events
            element.RegisterCallback<KeyDownEvent>(OnElementKeyDown);
        }
        
        private int GetElementPriority(VisualElement element)
        {
            // Check if element has priority metadata
            if (element.userData is Dictionary<string, object> metadata)
            {
                if (metadata.TryGetValue("NavigationPriority", out var priority))
                {
                    return (int)priority;
                }
            }
            
            // Default priority based on element type
            if (element.ClassListContains("menu-category-item"))
                return 100;
            if (element.ClassListContains("menu-action-item"))
                return 80;
            if (element.ClassListContains("button"))
                return 60;
            
            return 50;
        }
        
        private void ApplyElementFocus(VisualElement element)
        {
            element.AddToClassList("navigation-focused");
            element.style.borderLeftWidth = 3f;
            element.style.borderRightWidth = 3f;
            element.style.borderTopWidth = 3f;
            element.style.borderBottomWidth = 3f;
            element.style.borderLeftColor = Color.cyan;
            element.style.borderRightColor = Color.cyan;
            element.style.borderTopColor = Color.cyan;
            element.style.borderBottomColor = Color.cyan;
            
            element.Focus();
            
            // Scroll element into view if needed
            ScrollIntoView(element);
        }
        
        private void RemoveElementFocus(VisualElement element)
        {
            element.RemoveFromClassList("navigation-focused");
            element.style.borderLeftWidth = 0f;
            element.style.borderRightWidth = 0f;
            element.style.borderTopWidth = 0f;
            element.style.borderBottomWidth = 0f;
        }
        
        private void ScrollIntoView(VisualElement element)
        {
            var scrollView = element.GetFirstAncestorOfType<ScrollView>();
            if (scrollView != null)
            {
                scrollView.ScrollTo(element);
            }
        }
        
        private void ExecuteElementAction(VisualElement element)
        {
            // Trigger click event on element
            using (var clickEvent = ClickEvent.GetPooled())
            {
                clickEvent.target = element;
                element.SendEvent(clickEvent);
            }
        }
        
        private void FindNavigableElements(VisualElement root)
        {
            if (root == null)
                return;
            
            // Check if this element is navigable
            if (root.focusable && root.style.display == DisplayStyle.Flex)
            {
                RegisterNavigableElement(root);
            }
            
            // Recursively check children
            foreach (var child in root.Children())
            {
                FindNavigableElements(child);
            }
        }
        
        // Event handlers
        private void OnElementFocusIn(FocusInEvent evt)
        {
            var element = evt.target as VisualElement;
            if (element != null && _navigableElements.Contains(element))
            {
                var index = _navigableElements.IndexOf(element);
                SetNavigationIndex(index);
            }
        }
        
        private void OnElementFocusOut(FocusOutEvent evt)
        {
            var element = evt.target as VisualElement;
            if (element != null)
            {
                RemoveElementFocus(element);
            }
        }
        
        private void OnElementKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                case KeyCode.Space:
                    ExecuteElementAction(evt.target as VisualElement);
                    evt.StopPropagation();
                    break;
                    
                case KeyCode.Tab:
                    if (evt.shiftKey)
                        NavigatePrevious();
                    else
                        NavigateNext();
                    evt.StopPropagation();
                    break;
                    
                case KeyCode.UpArrow:
                    NavigateUp();
                    evt.StopPropagation();
                    break;
                    
                case KeyCode.DownArrow:
                    NavigateDown();
                    evt.StopPropagation();
                    break;
                    
                case KeyCode.LeftArrow:
                    NavigateLeft();
                    evt.StopPropagation();
                    break;
                    
                case KeyCode.RightArrow:
                    NavigateRight();
                    evt.StopPropagation();
                    break;
            }
        }
    }
#endif
}