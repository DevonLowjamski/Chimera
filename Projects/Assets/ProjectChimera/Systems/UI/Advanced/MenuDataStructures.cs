using UnityEngine;
using System;
using System.Collections.Generic;

namespace ProjectChimera.Systems.UI.Advanced
{
    /// <summary>
    /// Data structures for advanced menu system
    /// Supports dynamic categories, contextual actions, and intelligent filtering
    /// </summary>
    
    /// <summary>
    /// Represents a menu category with dynamic behavior
    /// </summary>
    [System.Serializable]
    public class MenuCategory
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public string Icon;
        public int Priority = 50;
        public string PillarType; // Construction, Cultivation, Genetics
        public string RequiredContext; // Optional context requirement
        public string[] RequiredSkills = new string[0];
        public bool IsDynamic = true;
        public bool IsVisible = true;
        public Color CategoryColor = Color.white;
        
        // Dynamic condition checking
        public Func<MenuContext, bool> ConditionCallback;
        
        // Category metadata
        public Dictionary<string, object> Metadata = new Dictionary<string, object>();
        
        public MenuCategory()
        {
            Id = Guid.NewGuid().ToString();
        }
        
        public MenuCategory(string id, string displayName, string pillarType)
        {
            Id = id;
            DisplayName = displayName;
            PillarType = pillarType;
        }
    }
    
    /// <summary>
    /// Represents a contextual menu action
    /// </summary>
    [System.Serializable]
    public class MenuAction
    {
        public string Id;
        public string CategoryId;
        public string DisplayName;
        public string Description;
        public string Icon;
        public int Priority = 50;
        public string PillarType; // Construction, Cultivation, Genetics
        public string RequiredContext; // Optional context requirement
        public string[] RequiredSkills = new string[0];
        public bool IsEnabled = true;
        public bool IsVisible = true;
        public Color ActionColor = Color.white;
        
        // Resource requirements
        public ResourceRequirement[] ResourceRequirements;
        
        // Condition callbacks
        public Func<MenuContext, bool> ConditionCallback;
        public Func<MenuContext, bool> ExecutionConditionCallback;
        
        // Action metadata
        public Dictionary<string, object> Parameters = new Dictionary<string, object>();
        
        public MenuAction()
        {
            Id = Guid.NewGuid().ToString();
        }
        
        public MenuAction(string id, string categoryId, string displayName, string pillarType)
        {
            Id = id;
            CategoryId = categoryId;
            DisplayName = displayName;
            PillarType = pillarType;
        }
    }
    
    /// <summary>
    /// Context information for menu display and action execution
    /// </summary>
    [System.Serializable]
    public class MenuContext
    {
        public Vector3 WorldPosition;
        public GameObject TargetObject;
        public string ContextType; // Plant, Equipment, Structure, Ground, etc.
        public float Timestamp;
        public Vector3 PlayerPosition;
        public Vector3 CameraForward;
        
        // Environmental context
        public string ZoneId;
        public string RoomId;
        public string FacilityId;
        
        // Game state context
        public int PlayerLevel;
        public string[] UnlockedSkills;
        public Dictionary<string, float> Resources;
        
        // Dynamic properties
        public Dictionary<string, object> Properties = new Dictionary<string, object>();
        
        public MenuContext()
        {
            Timestamp = Time.time;
            Resources = new Dictionary<string, float>();
            UnlockedSkills = new string[0];
        }
    }
    
    /// <summary>
    /// Represents an active contextual menu instance
    /// </summary>
    [System.Serializable]
    public class ContextualMenu
    {
        public string Id;
        public Vector3 WorldPosition;
        public GameObject TargetObject;
        public MenuContext Context;
        public List<MenuCategory> Categories = new List<MenuCategory>();
        public List<MenuAction> Actions = new List<MenuAction>();
        public bool IsOpen;
        public float CreationTime;
        public float LastUpdateTime;
        
        // UI state
        public string SelectedCategoryId;
        public Vector2 ScrollPosition;
        public bool IsExpanded;
        
        public ContextualMenu()
        {
            Id = Guid.NewGuid().ToString();
            CreationTime = Time.time;
        }
    }
    
    /// <summary>
    /// Resource requirement for actions
    /// </summary>
    [System.Serializable]
    public struct ResourceRequirement
    {
        public string ResourceType;
        public float Amount;
        public bool IsRequired;
        public string Description;
        
        public ResourceRequirement(string resourceType, float amount, bool isRequired = true, string description = "")
        {
            ResourceType = resourceType;
            Amount = amount;
            IsRequired = isRequired;
            Description = description;
        }
    }
    
    /// <summary>
    /// Menu cache for performance optimization
    /// </summary>
    public class MenuCache
    {
        private Dictionary<string, CachedMenuData> _cache = new Dictionary<string, CachedMenuData>();
        private const float CACHE_LIFETIME = 30f;
        
        public void CacheMenuData(string contextKey, List<MenuCategory> categories, List<MenuAction> actions)
        {
            _cache[contextKey] = new CachedMenuData
            {
                Categories = new List<MenuCategory>(categories),
                Actions = new List<MenuAction>(actions),
                CacheTime = Time.time
            };
        }
        
        public bool TryGetCachedData(string contextKey, out List<MenuCategory> categories, out List<MenuAction> actions)
        {
            categories = null;
            actions = null;
            
            if (_cache.TryGetValue(contextKey, out var cachedData))
            {
                if (Time.time - cachedData.CacheTime < CACHE_LIFETIME)
                {
                    categories = cachedData.Categories;
                    actions = cachedData.Actions;
                    return true;
                }
                else
                {
                    _cache.Remove(contextKey);
                }
            }
            
            return false;
        }
        
        public void ClearCache()
        {
            _cache.Clear();
        }
        
        public void ClearExpiredEntries()
        {
            var keysToRemove = new List<string>();
            
            foreach (var kvp in _cache)
            {
                if (Time.time - kvp.Value.CacheTime >= CACHE_LIFETIME)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }
        }
        
        private class CachedMenuData
        {
            public List<MenuCategory> Categories;
            public List<MenuAction> Actions;
            public float CacheTime;
        }
    }
    
    /// <summary>
    /// Input action handler for menu system
    /// </summary>
    public class InputActionHandler : MonoBehaviour
    {
        public event Action<Vector3> OnContextMenuRequested;
        public event Action OnMenuCancelRequested;
        public event Action<Vector2> OnNavigationInput;
        public event Action OnConfirmInput;
        
        [Header("Input Settings")]
        [SerializeField] private bool _enableMouseInput = true;
        [SerializeField] private bool _enableKeyboardInput = true;
        [SerializeField] private bool _enableControllerInput = false;
        
        [Header("Mouse Settings")]
        [SerializeField] private int _contextMenuButton = 1; // Right mouse button
        [SerializeField] private float _clickThreshold = 0.2f;
        
        [Header("Keyboard Settings")]
        [SerializeField] private KeyCode _contextMenuKey = KeyCode.Tab;
        [SerializeField] private KeyCode _cancelKey = KeyCode.Escape;
        [SerializeField] private KeyCode _confirmKey = KeyCode.Return;
        
        private float _mouseDownTime;
        private bool _mouseWasPressed;
        
        private void Update()
        {
            HandleMouseInput();
            HandleKeyboardInput();
        }
        
        private void HandleMouseInput()
        {
            if (!_enableMouseInput) return;
            
            // Context menu on right click
            if (Input.GetMouseButtonDown(_contextMenuButton))
            {
                _mouseDownTime = Time.time;
                _mouseWasPressed = true;
            }
            
            if (Input.GetMouseButtonUp(_contextMenuButton) && _mouseWasPressed)
            {
                if (Time.time - _mouseDownTime < _clickThreshold)
                {
                    var mousePosition = Input.mousePosition;
                    var worldPosition = Camera.main?.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, 10f)) ?? Vector3.zero;
                    OnContextMenuRequested?.Invoke(worldPosition);
                }
                _mouseWasPressed = false;
            }
            
            // Cancel on left click or escape
            if (Input.GetMouseButtonDown(0))
            {
                OnMenuCancelRequested?.Invoke();
            }
        }
        
        private void HandleKeyboardInput()
        {
            if (!_enableKeyboardInput) return;
            
            // Context menu key
            if (Input.GetKeyDown(_contextMenuKey))
            {
                var cameraPosition = Camera.main?.transform.position ?? Vector3.zero;
                var cameraForward = Camera.main?.transform.forward ?? Vector3.forward;
                var worldPosition = cameraPosition + cameraForward * 5f;
                OnContextMenuRequested?.Invoke(worldPosition);
            }
            
            // Cancel key
            if (Input.GetKeyDown(_cancelKey))
            {
                OnMenuCancelRequested?.Invoke();
            }
            
            // Confirm key
            if (Input.GetKeyDown(_confirmKey))
            {
                OnConfirmInput?.Invoke();
            }
            
            // Navigation input
            var horizontal = Input.GetAxis("Horizontal");
            var vertical = Input.GetAxis("Vertical");
            
            if (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f)
            {
                OnNavigationInput?.Invoke(new Vector2(horizontal, vertical));
            }
        }
    }
    
    /// <summary>
    /// Visual feedback types for menu system
    /// </summary>
    public enum FeedbackType
    {
        Success,
        Error,
        Warning,
        Info,
        Progress
    }
    
    /// <summary>
    /// Visual feedback system interface
    /// </summary>
    public interface IVisualFeedbackSystem
    {
        void ShowFeedback(string message, FeedbackType type, float duration = 3f);
        void ShowProgress(string message, float progress);
        void HideFeedback();
        void ShowTooltip(string text, Vector3 worldPosition);
        void HideTooltip();
    }
}