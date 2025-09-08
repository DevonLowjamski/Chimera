using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Type definitions and data structures for World Space Interaction System.
    /// Contains all configuration classes, enums, and data structures for 3D UI interaction.
    /// </summary>
    
    /// <summary>
    /// Configuration settings for world space interaction system
    /// </summary>
    [System.Serializable]
    public class WorldSpaceInteractionConfig
    {
        [Header("Interaction Settings")]
        public float maxInteractionDistance = 10f;
        public bool enableHoverEffects = true;
        public bool enableClickEffects = true;
        public bool enableHoverAudio = true;
        public bool enableClickAudio = true;
        
        [Header("Visual Effects")]
        public float hoverScaleMultiplier = 1.1f;
        public float hoverAlphaMultiplier = 1.2f;
        public float clickScaleMultiplier = 0.95f;
        public float clickEffectDuration = 0.15f;
        
        [Header("Audio Settings")]
        [Range(0f, 1f)]
        public float audioVolume = 0.5f;
        
        [Header("Gesture Recognition")]
        public float gestureMinMovement = 5f;
        public float gestureMinDistance = 20f;
        public float gestureMinDuration = 0.1f;
        public float swipeThreshold = 50f;
        
        [Header("Performance")]
        public int maxSimultaneousInteractions = 10;
        public float interactionUpdateRate = 60f;
    }
    
    /// <summary>
    /// Settings for individual interactive elements
    /// </summary>
    [System.Serializable]
    public class InteractionSettings
    {
        public bool isInteractable = true;
        public bool enableHover = true;
        public bool enableClick = true;
        public bool enableGestures = true;
        public float hoverDelay = 0f;
        public float clickDelay = 0f;
        public LayerMask raycastLayers = -1;
        
        public InteractionSettings()
        {
            // Default settings for cannabis facility UI
            isInteractable = true;
            enableHover = true;
            enableClick = true;
            enableGestures = true;
        }
    }
    
    /// <summary>
    /// Data for tracking interaction state of UI elements
    /// </summary>
    public class InteractionData
    {
        public UIDocument Element { get; set; }
        public InteractionSettings Settings { get; set; }
        public Vector3 OriginalScale { get; set; }
        public float OriginalAlpha { get; set; }
        public float RegistrationTime { get; set; }
        
        // State tracking
        public bool IsHovered { get; set; }
        public bool IsPressed { get; set; }
        public bool IsSelected { get; set; }
        public Vector3 LastHoverPosition { get; set; }
        public float LastHoverTime { get; set; }
        public float LastClickTime { get; set; }
        public int ClickCount { get; set; }
        
        // Interaction history
        public float TotalHoverTime { get; set; }
        public int TotalClicks { get; set; }
        public List<GestureData> GestureHistory { get; set; }
        
        public InteractionData()
        {
            GestureHistory = new List<GestureData>();
        }
        
        /// <summary>
        /// Age of the interactive element in seconds
        /// </summary>
        public float Age => Time.time - RegistrationTime;
        
        /// <summary>
        /// Time since last interaction in seconds
        /// </summary>
        public float TimeSinceLastInteraction => Mathf.Min(Time.time - LastHoverTime, Time.time - LastClickTime);
    }
    
    /// <summary>
    /// Types of gestures that can be recognized
    /// </summary>
    public enum GestureType
    {
        None,
        Tap,
        DoubleTap,
        LongPress,
        Drag,
        SwipeLeft,
        SwipeRight,
        SwipeUp,
        SwipeDown,
        Pinch,
        Spread,
        Rotate
    }
    
    /// <summary>
    /// Data structure for gesture information
    /// </summary>
    public class GestureData
    {
        public GestureType Type { get; set; }
        public Vector2 StartPosition { get; set; }
        public Vector2 EndPosition { get; set; }
        public Vector2 OverallDirection { get; set; }
        public float Duration { get; set; }
        public float TotalDistance { get; set; }
        public List<Vector2> Points { get; set; }
        public bool IsValid { get; set; }
        public float Timestamp { get; set; }
        
        public GestureData()
        {
            Points = new List<Vector2>();
            Timestamp = Time.time;
        }
        
        /// <summary>
        /// Gets the velocity of the gesture
        /// </summary>
        public float Velocity => Duration > 0 ? TotalDistance / Duration : 0f;
        
        /// <summary>
        /// Gets the magnitude of the overall movement
        /// </summary>
        public float Magnitude => OverallDirection.magnitude;
    }
    
    /// <summary>
    /// Event arguments for interaction events
    /// </summary>
    public class InteractionEventArgs : EventArgs
    {
        public UIDocument Element { get; set; }
        public Vector3 WorldPosition { get; set; }
        public Vector2 ScreenPosition { get; set; }
        public InteractionData InteractionData { get; set; }
        public float Timestamp { get; set; }
        
        public InteractionEventArgs(UIDocument element, Vector3 worldPosition, Vector2 screenPosition, InteractionData data)
        {
            Element = element;
            WorldPosition = worldPosition;
            ScreenPosition = screenPosition;
            InteractionData = data;
            Timestamp = Time.time;
        }
    }
    
    /// <summary>
    /// Interface for objects that can handle world space interactions
    /// </summary>
    public interface IWorldSpaceInteractable
    {
        bool CanInteract { get; }
        void OnHoverEnter(InteractionEventArgs args);
        void OnHoverExit(InteractionEventArgs args);
        void OnClick(InteractionEventArgs args);
        void OnPress(InteractionEventArgs args);
        void OnRelease(InteractionEventArgs args);
        void OnGesture(GestureData gestureData);
    }
    
    /// <summary>
    /// Cannabis plant-specific interactable component
    /// </summary>
    public class PlantInteractable : MonoBehaviour, IWorldSpaceInteractable
    {
        [Header("Plant Interaction")]
        [SerializeField] private bool _canInteract = true;
        [SerializeField] private PlantStatusProvider _statusProvider;
        [SerializeField] private WorldSpaceStatusRenderer _statusRenderer;
        
        public bool CanInteract => _canInteract && gameObject.activeInHierarchy;
        
        public void OnHoverEnter(InteractionEventArgs args)
        {
            // Show detailed plant status
            if (_statusProvider != null && _statusRenderer != null)
            {
                var statusData = _statusProvider.GetCurrentStatus();
                _statusRenderer.ShowStatusDisplay(gameObject, StatusDisplayType.PlantHealth, statusData);
            }
            
            ChimeraLogger.Log($"[PlantInteractable] Hover enter on plant: {gameObject.name}");
        }
        
        public void OnHoverExit(InteractionEventArgs args)
        {
            // Hide detailed status after delay
            if (_statusRenderer != null)
            {
                _statusRenderer.HideStatusDisplay(gameObject);
            }
            
            ChimeraLogger.Log($"[PlantInteractable] Hover exit on plant: {gameObject.name}");
        }
        
        public void OnClick(InteractionEventArgs args)
        {
            // Open plant details menu
            ChimeraLogger.Log($"[PlantInteractable] Clicked on plant: {gameObject.name}");
            
            // Could trigger detailed plant inspection UI
            // GetComponent<PlantInspectionUI>()?.ShowInspectionPanel();
        }
        
        public void OnPress(InteractionEventArgs args)
        {
            ChimeraLogger.Log($"[PlantInteractable] Press on plant: {gameObject.name}");
        }
        
        public void OnRelease(InteractionEventArgs args)
        {
            ChimeraLogger.Log($"[PlantInteractable] Release on plant: {gameObject.name}");
        }
        
        public void OnGesture(GestureData gestureData)
        {
            switch (gestureData.Type)
            {
                case GestureType.SwipeUp:
                    // Quick harvest gesture
                    ChimeraLogger.Log($"[PlantInteractable] Swipe up gesture - Quick harvest attempt");
                    break;
                case GestureType.SwipeDown:
                    // Water plant gesture
                    ChimeraLogger.Log($"[PlantInteractable] Swipe down gesture - Water plant");
                    break;
                case GestureType.Drag:
                    // Move plant (if in pot)
                    ChimeraLogger.Log($"[PlantInteractable] Drag gesture - Move plant");
                    break;
            }
        }
    }
    
    /// <summary>
    /// Facility equipment interactable component
    /// </summary>
    public class FacilityInteractable : MonoBehaviour, IWorldSpaceInteractable
    {
        [Header("Facility Interaction")]
        [SerializeField] private bool _canInteract = true;
        [SerializeField] private FacilityStatusProvider _statusProvider;
        [SerializeField] private WorldSpaceStatusRenderer _statusRenderer;
        [SerializeField] private WorldSpaceMenuRenderer _menuRenderer;
        
        public bool CanInteract => _canInteract && gameObject.activeInHierarchy;
        
        public void OnHoverEnter(InteractionEventArgs args)
        {
            // Show facility status
            if (_statusProvider != null && _statusRenderer != null)
            {
                var statusData = _statusProvider.GetCurrentStatus();
                _statusRenderer.ShowStatusDisplay(gameObject, StatusDisplayType.FacilityStatus, statusData);
            }
            
            ChimeraLogger.Log($"[FacilityInteractable] Hover enter on facility: {gameObject.name}");
        }
        
        public void OnHoverExit(InteractionEventArgs args)
        {
            // Hide status display
            if (_statusRenderer != null)
            {
                _statusRenderer.HideStatusDisplay(gameObject);
            }
            
            ChimeraLogger.Log($"[FacilityInteractable] Hover exit on facility: {gameObject.name}");
        }
        
        public void OnClick(InteractionEventArgs args)
        {
            // Show facility menu
            if (_menuRenderer != null)
            {
                var menuItems = new List<string> { "Control Panel", "Settings", "Maintenance", "Analytics" };
                _menuRenderer.ShowWorldSpaceMenu(gameObject, WorldSpaceMenuType.Facility, menuItems);
            }
            
            ChimeraLogger.Log($"[FacilityInteractable] Clicked on facility: {gameObject.name}");
        }
        
        public void OnPress(InteractionEventArgs args)
        {
            ChimeraLogger.Log($"[FacilityInteractable] Press on facility: {gameObject.name}");
        }
        
        public void OnRelease(InteractionEventArgs args)
        {
            ChimeraLogger.Log($"[FacilityInteractable] Release on facility: {gameObject.name}");
        }
        
        public void OnGesture(GestureData gestureData)
        {
            switch (gestureData.Type)
            {
                case GestureType.SwipeLeft:
                    // Previous facility view
                    ChimeraLogger.Log($"[FacilityInteractable] Swipe left - Previous view");
                    break;
                case GestureType.SwipeRight:
                    // Next facility view
                    ChimeraLogger.Log($"[FacilityInteractable] Swipe right - Next view");
                    break;
                case GestureType.LongPress:
                    // Advanced facility controls
                    ChimeraLogger.Log($"[FacilityInteractable] Long press - Advanced controls");
                    break;
            }
        }
    }
    
    /// <summary>
    /// Interaction system statistics for debugging and optimization
    /// </summary>
    public class InteractionSystemStats
    {
        public int RegisteredElements { get; set; }
        public int ActiveInteractions { get; set; }
        public int TotalInteractions { get; set; }
        public int GesturesRecognized { get; set; }
        public float AverageResponseTime { get; set; }
        public Dictionary<GestureType, int> GestureFrequency { get; set; }
        
        public InteractionSystemStats()
        {
            GestureFrequency = new Dictionary<GestureType, int>();
        }
    }
}