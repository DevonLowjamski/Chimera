using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core;

namespace ProjectChimera.UI.Core
{
    /// <summary>
    /// Keyboard navigation system for UI accessibility in Project Chimera.
    /// Provides comprehensive keyboard navigation support for all UI elements.
    /// </summary>
    public class UIKeyboardNavigator
    {
        private Dictionary<KeyCode, UINavigationAction> _keyMappings;
        private Dictionary<string, UINavigationGroup> _navigationGroups;
        private UINavigationGroup _currentGroup;
        private bool _isInitialized = false;
        private float _lastNavigationTime;
        private float _navigationRepeatDelay = 0.5f;
        
        // Navigation state
        private UINavigationMode _currentMode = UINavigationMode.Linear;
        private UINavigationDirection _lastDirection = UINavigationDirection.None;
        private int _repeatCount = 0;
        
        // Events
        public System.Action<UINavigationDirection> OnNavigationRequested;
        public System.Action OnActivationRequested;
        public System.Action<UINavigationGroup> OnNavigationGroupChanged;
        public System.Action<UINavigationMode> OnNavigationModeChanged;
        
        // Properties
        public bool IsInitialized => _isInitialized;
        public UINavigationMode CurrentMode => _currentMode;
        public UINavigationGroup CurrentGroup => _currentGroup;
        public int NavigationGroupCount => _navigationGroups?.Count ?? 0;
        
        /// <summary>
        /// Initialize keyboard navigator
        /// </summary>
        public void Initialize()
        {
            InitializeKeyMappings();
            InitializeNavigationGroups();
            
            _isInitialized = true;
            
            Debug.Log("Keyboard navigator initialized successfully");
        }
        
        /// <summary>
        /// Initialize key mappings
        /// </summary>
        private void InitializeKeyMappings()
        {
            _keyMappings = new Dictionary<KeyCode, UINavigationAction>
            {
                // Tab navigation
                [KeyCode.Tab] = new UINavigationAction(UINavigationDirection.Next, UINavigationModifier.None),
                
                // Arrow navigation
                [KeyCode.UpArrow] = new UINavigationAction(UINavigationDirection.Up, UINavigationModifier.None),
                [KeyCode.DownArrow] = new UINavigationAction(UINavigationDirection.Down, UINavigationModifier.None),
                [KeyCode.LeftArrow] = new UINavigationAction(UINavigationDirection.Left, UINavigationModifier.None),
                [KeyCode.RightArrow] = new UINavigationAction(UINavigationDirection.Right, UINavigationModifier.None),
                
                // Activation
                [KeyCode.Return] = new UINavigationAction(UINavigationDirection.Activate, UINavigationModifier.None),
                [KeyCode.KeypadEnter] = new UINavigationAction(UINavigationDirection.Activate, UINavigationModifier.None),
                [KeyCode.Space] = new UINavigationAction(UINavigationDirection.Activate, UINavigationModifier.None),
                
                // Navigation modes
                [KeyCode.F6] = new UINavigationAction(UINavigationDirection.NextGroup, UINavigationModifier.None),
                [KeyCode.F7] = new UINavigationAction(UINavigationDirection.ToggleMode, UINavigationModifier.None),
                
                // Quick navigation
                [KeyCode.Home] = new UINavigationAction(UINavigationDirection.First, UINavigationModifier.None),
                [KeyCode.End] = new UINavigationAction(UINavigationDirection.Last, UINavigationModifier.None),
                [KeyCode.PageUp] = new UINavigationAction(UINavigationDirection.PageUp, UINavigationModifier.None),
                [KeyCode.PageDown] = new UINavigationAction(UINavigationDirection.PageDown, UINavigationModifier.None),
                
                // Escape
                [KeyCode.Escape] = new UINavigationAction(UINavigationDirection.Cancel, UINavigationModifier.None)
            };
        }
        
        /// <summary>
        /// Initialize navigation groups
        /// </summary>
        private void InitializeNavigationGroups()
        {
            _navigationGroups = new Dictionary<string, UINavigationGroup>();
            
            // Create default navigation groups
            CreateNavigationGroup("main", "Main Navigation", UINavigationMode.Linear);
            CreateNavigationGroup("modal", "Modal Navigation", UINavigationMode.Contained);
            CreateNavigationGroup("menu", "Menu Navigation", UINavigationMode.Hierarchical);
            CreateNavigationGroup("form", "Form Navigation", UINavigationMode.Grid);
            CreateNavigationGroup("list", "List Navigation", UINavigationMode.Linear);
            CreateNavigationGroup("grid", "Grid Navigation", UINavigationMode.Grid);
            
            // Set default group
            _currentGroup = _navigationGroups["main"];
        }
        
        /// <summary>
        /// Create navigation group
        /// </summary>
        public void CreateNavigationGroup(string id, string name, UINavigationMode mode)
        {
            var group = new UINavigationGroup
            {
                Id = id,
                Name = name,
                Mode = mode,
                Elements = new List<VisualElement>(),
                IsActive = false,
                Priority = 0
            };
            
            _navigationGroups[id] = group;
        }
        
        /// <summary>
        /// Add element to navigation group
        /// </summary>
        public void AddElementToGroup(string groupId, VisualElement element, int tabIndex = 0)
        {
            if (!_navigationGroups.TryGetValue(groupId, out var group) || element == null)
                return;
            
            if (!group.Elements.Contains(element))
            {
                group.Elements.Add(element);
                
                // Set tab index
                element.tabIndex = tabIndex;
                
                // Sort elements by tab index and position
                SortGroupElements(group);
                
                Debug.Log($"Added element {element.name} to navigation group {groupId}");
            }
        }
        
        /// <summary>
        /// Remove element from navigation group
        /// </summary>
        public void RemoveElementFromGroup(string groupId, VisualElement element)
        {
            if (!_navigationGroups.TryGetValue(groupId, out var group) || element == null)
                return;
            
            group.Elements.Remove(element);
            
            Debug.Log($"Removed element {element.name} from navigation group {groupId}");
        }
        
        /// <summary>
        /// Set current navigation group
        /// </summary>
        public void SetCurrentGroup(string groupId)
        {
            if (!_navigationGroups.TryGetValue(groupId, out var group))
                return;
            
            if (_currentGroup != null)
                _currentGroup.IsActive = false;
            
            _currentGroup = group;
            _currentGroup.IsActive = true;
            _currentMode = group.Mode;
            
            OnNavigationGroupChanged?.Invoke(_currentGroup);
            OnNavigationModeChanged?.Invoke(_currentMode);
            
            Debug.Log($"Switched to navigation group: {groupId} (mode: {_currentMode})");
        }
        
        /// <summary>
        /// Process keyboard input
        /// </summary>
        public bool ProcessKeyboardInput(KeyCode keyCode, bool shift, bool ctrl, bool alt)
        {
            if (!_isInitialized)
                return false;
            
            // Check for key mapping
            if (!_keyMappings.TryGetValue(keyCode, out var action))
                return false;
            
            // Apply modifiers
            var modifiedAction = ApplyModifiers(action, shift, ctrl, alt);
            
            // Check navigation timing
            if (!CanNavigate(modifiedAction.Direction))
                return false;
            
            // Process the navigation action
            return ProcessNavigationAction(modifiedAction);
        }
        
        /// <summary>
        /// Apply keyboard modifiers to navigation action
        /// </summary>
        private UINavigationAction ApplyModifiers(UINavigationAction action, bool shift, bool ctrl, bool alt)
        {
            var modifiedAction = action;
            
            // Shift modifier
            if (shift)
            {
                switch (action.Direction)
                {
                    case UINavigationDirection.Next:
                        modifiedAction.Direction = UINavigationDirection.Previous;
                        break;
                    case UINavigationDirection.Tab:
                        modifiedAction.Direction = UINavigationDirection.Previous;
                        break;
                }
            }
            
            // Ctrl modifier
            if (ctrl)
            {
                modifiedAction.Modifier |= UINavigationModifier.Fast;
                
                switch (action.Direction)
                {
                    case UINavigationDirection.Up:
                        modifiedAction.Direction = UINavigationDirection.First;
                        break;
                    case UINavigationDirection.Down:
                        modifiedAction.Direction = UINavigationDirection.Last;
                        break;
                    case UINavigationDirection.Left:
                        modifiedAction.Direction = UINavigationDirection.LineStart;
                        break;
                    case UINavigationDirection.Right:
                        modifiedAction.Direction = UINavigationDirection.LineEnd;
                        break;
                }
            }
            
            // Alt modifier
            if (alt)
            {
                modifiedAction.Modifier |= UINavigationModifier.Group;
            }
            
            return modifiedAction;
        }
        
        /// <summary>
        /// Check if navigation is allowed based on timing
        /// </summary>
        private bool CanNavigate(UINavigationDirection direction)
        {
            var currentTime = Time.time;
            var timeSinceLastNav = currentTime - _lastNavigationTime;
            
            // Allow immediate navigation for different directions
            if (direction != _lastDirection)
            {
                _repeatCount = 0;
                return true;
            }
            
            // Apply repeat delay for same direction
            var requiredDelay = _navigationRepeatDelay / (1 + _repeatCount * 0.1f); // Accelerate on repeat
            
            if (timeSinceLastNav >= requiredDelay)
            {
                _repeatCount++;
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Process navigation action
        /// </summary>
        private bool ProcessNavigationAction(UINavigationAction action)
        {
            _lastNavigationTime = Time.time;
            _lastDirection = action.Direction;
            
            switch (action.Direction)
            {
                case UINavigationDirection.Next:
                case UINavigationDirection.Previous:
                case UINavigationDirection.Up:
                case UINavigationDirection.Down:
                case UINavigationDirection.Left:
                case UINavigationDirection.Right:
                    OnNavigationRequested?.Invoke(action.Direction);
                    return true;
                    
                case UINavigationDirection.Activate:
                    OnActivationRequested?.Invoke();
                    return true;
                    
                case UINavigationDirection.NextGroup:
                    SwitchToNextGroup();
                    return true;
                    
                case UINavigationDirection.ToggleMode:
                    ToggleNavigationMode();
                    return true;
                    
                case UINavigationDirection.First:
                    NavigateToFirst();
                    return true;
                    
                case UINavigationDirection.Last:
                    NavigateToLast();
                    return true;
                    
                case UINavigationDirection.PageUp:
                    NavigatePageUp();
                    return true;
                    
                case UINavigationDirection.PageDown:
                    NavigatePageDown();
                    return true;
                    
                case UINavigationDirection.Cancel:
                    NavigateCancel();
                    return true;
                    
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Switch to next navigation group
        /// </summary>
        private void SwitchToNextGroup()
        {
            var groups = _navigationGroups.Values.OrderBy(g => g.Priority).ToList();
            var currentIndex = groups.IndexOf(_currentGroup);
            var nextIndex = (currentIndex + 1) % groups.Count;
            
            SetCurrentGroup(groups[nextIndex].Id);
        }
        
        /// <summary>
        /// Toggle navigation mode
        /// </summary>
        private void ToggleNavigationMode()
        {
            var modes = System.Enum.GetValues(typeof(UINavigationMode)).Cast<UINavigationMode>().ToArray();
            var currentIndex = System.Array.IndexOf(modes, _currentMode);
            var nextIndex = (currentIndex + 1) % modes.Length;
            
            _currentMode = modes[nextIndex];
            
            if (_currentGroup != null)
            {
                _currentGroup.Mode = _currentMode;
            }
            
            OnNavigationModeChanged?.Invoke(_currentMode);
            
            Debug.Log($"Navigation mode changed to: {_currentMode}");
        }
        
        /// <summary>
        /// Navigate to first element
        /// </summary>
        private void NavigateToFirst()
        {
            OnNavigationRequested?.Invoke(UINavigationDirection.First);
        }
        
        /// <summary>
        /// Navigate to last element
        /// </summary>
        private void NavigateToLast()
        {
            OnNavigationRequested?.Invoke(UINavigationDirection.Last);
        }
        
        /// <summary>
        /// Navigate page up
        /// </summary>
        private void NavigatePageUp()
        {
            OnNavigationRequested?.Invoke(UINavigationDirection.PageUp);
        }
        
        /// <summary>
        /// Navigate page down
        /// </summary>
        private void NavigatePageDown()
        {
            OnNavigationRequested?.Invoke(UINavigationDirection.PageDown);
        }
        
        /// <summary>
        /// Handle cancel navigation
        /// </summary>
        private void NavigateCancel()
        {
            OnNavigationRequested?.Invoke(UINavigationDirection.Cancel);
        }
        
        /// <summary>
        /// Sort elements in navigation group
        /// </summary>
        private void SortGroupElements(UINavigationGroup group)
        {
            switch (group.Mode)
            {
                case UINavigationMode.Linear:
                    SortLinear(group);
                    break;
                case UINavigationMode.Grid:
                    SortGrid(group);
                    break;
                case UINavigationMode.Hierarchical:
                    SortHierarchical(group);
                    break;
                case UINavigationMode.Spatial:
                    SortSpatial(group);
                    break;
            }
        }
        
        /// <summary>
        /// Sort elements linearly (tab order and position)
        /// </summary>
        private void SortLinear(UINavigationGroup group)
        {
            group.Elements.Sort((a, b) =>
            {
                // Sort by tab index first
                var tabComparison = a.tabIndex.CompareTo(b.tabIndex);
                if (tabComparison != 0)
                    return tabComparison;
                
                // Then by vertical position
                var aPos = a.worldBound.position;
                var bPos = b.worldBound.position;
                
                var yComparison = aPos.y.CompareTo(bPos.y);
                if (Mathf.Abs(yComparison) > 10f)
                    return yComparison;
                
                // Finally by horizontal position
                return aPos.x.CompareTo(bPos.x);
            });
        }
        
        /// <summary>
        /// Sort elements in grid pattern
        /// </summary>
        private void SortGrid(UINavigationGroup group)
        {
            // Sort by row first, then column
            group.Elements.Sort((a, b) =>
            {
                var aPos = a.worldBound.position;
                var bPos = b.worldBound.position;
                
                // Group by rows (with tolerance)
                var rowTolerance = 50f;
                var rowDiff = Mathf.Abs(aPos.y - bPos.y);
                
                if (rowDiff > rowTolerance)
                {
                    return aPos.y.CompareTo(bPos.y);
                }
                
                // Same row - sort by column
                return aPos.x.CompareTo(bPos.x);
            });
        }
        
        /// <summary>
        /// Sort elements hierarchically (parent-child relationships)
        /// </summary>
        private void SortHierarchical(UINavigationGroup group)
        {
            // Sort by hierarchy depth and sibling order
            group.Elements.Sort((a, b) =>
            {
                var aDepth = GetHierarchyDepth(a);
                var bDepth = GetHierarchyDepth(b);
                
                if (aDepth != bDepth)
                    return aDepth.CompareTo(bDepth);
                
                // Same depth - sort by sibling index
                return GetSiblingIndex(a).CompareTo(GetSiblingIndex(b));
            });
        }
        
        /// <summary>
        /// Sort elements spatially (by position)
        /// </summary>
        private void SortSpatial(UINavigationGroup group)
        {
            // Sort by distance from top-left corner
            group.Elements.Sort((a, b) =>
            {
                var aPos = a.worldBound.position;
                var bPos = b.worldBound.position;
                
                var aDistance = aPos.magnitude;
                var bDistance = bPos.magnitude;
                
                return aDistance.CompareTo(bDistance);
            });
        }
        
        /// <summary>
        /// Get hierarchy depth of element
        /// </summary>
        private int GetHierarchyDepth(VisualElement element)
        {
            int depth = 0;
            var parent = element.parent;
            
            while (parent != null)
            {
                depth++;
                parent = parent.parent;
            }
            
            return depth;
        }
        
        /// <summary>
        /// Get sibling index of element
        /// </summary>
        private int GetSiblingIndex(VisualElement element)
        {
            if (element.parent == null)
                return 0;
            
            return element.parent.IndexOf(element);
        }
        
        /// <summary>
        /// Set navigation repeat delay
        /// </summary>
        public void SetNavigationRepeatDelay(float delay)
        {
            _navigationRepeatDelay = Mathf.Max(0.1f, delay);
        }
        
        /// <summary>
        /// Add custom key mapping
        /// </summary>
        public void AddKeyMapping(KeyCode key, UINavigationDirection direction, UINavigationModifier modifier = UINavigationModifier.None)
        {
            _keyMappings[key] = new UINavigationAction(direction, modifier);
        }
        
        /// <summary>
        /// Remove key mapping
        /// </summary>
        public void RemoveKeyMapping(KeyCode key)
        {
            _keyMappings.Remove(key);
        }
        
        /// <summary>
        /// Get navigation statistics
        /// </summary>
        public UIKeyboardNavigatorStats GetStats()
        {
            return new UIKeyboardNavigatorStats
            {
                IsInitialized = _isInitialized,
                CurrentMode = _currentMode,
                CurrentGroupId = _currentGroup?.Id ?? "",
                NavigationGroupCount = _navigationGroups.Count,
                KeyMappingCount = _keyMappings.Count,
                LastNavigationTime = _lastNavigationTime,
                RepeatCount = _repeatCount
            };
        }
        
        /// <summary>
        /// Cleanup keyboard navigator
        /// </summary>
        public void Cleanup()
        {
            _keyMappings?.Clear();
            _navigationGroups?.Clear();
            _currentGroup = null;
            _isInitialized = false;
            
            Debug.Log("Keyboard navigator cleaned up");
        }
        
        /// <summary>
        /// Shutdown keyboard navigator (alias for Cleanup)
        /// </summary>
        public void Shutdown()
        {
            Cleanup();
        }
    }
    
    /// <summary>
    /// Navigation group data
    /// </summary>
    public class UINavigationGroup
    {
        public string Id;
        public string Name;
        public UINavigationMode Mode;
        public List<VisualElement> Elements;
        public bool IsActive;
        public int Priority;
    }
    
    /// <summary>
    /// Navigation action data
    /// </summary>
    public struct UINavigationAction
    {
        public UINavigationDirection Direction;
        public UINavigationModifier Modifier;
        
        public UINavigationAction(UINavigationDirection direction, UINavigationModifier modifier)
        {
            Direction = direction;
            Modifier = modifier;
        }
    }
    
    /// <summary>
    /// Keyboard navigator statistics
    /// </summary>
    public struct UIKeyboardNavigatorStats
    {
        public bool IsInitialized;
        public UINavigationMode CurrentMode;
        public string CurrentGroupId;
        public int NavigationGroupCount;
        public int KeyMappingCount;
        public float LastNavigationTime;
        public int RepeatCount;
    }
    
    /// <summary>
    /// Navigation directions
    /// </summary>
    public enum UINavigationDirection
    {
        None,
        Next,
        Previous,
        Up,
        Down,
        Left,
        Right,
        First,
        Last,
        PageUp,
        PageDown,
        LineStart,
        LineEnd,
        Activate,
        Cancel,
        NextGroup,
        PreviousGroup,
        ToggleMode,
        Tab
    }
    
    /// <summary>
    /// Navigation modes
    /// </summary>
    public enum UINavigationMode
    {
        Linear,      // Tab order navigation
        Grid,        // 2D grid navigation
        Hierarchical, // Tree-like navigation
        Spatial,     // Spatial position-based
        Contained    // Navigation within container only
    }
    
    /// <summary>
    /// Navigation modifiers
    /// </summary>
    [System.Flags]
    public enum UINavigationModifier
    {
        None = 0,
        Fast = 1,
        Group = 2,
        Precise = 4
    }
}