using UnityEngine;
using UnityEngine.UI;
using ProjectChimera.Core.DependencyInjection;
using ProjectChimera.Systems.Gameplay;
using ProjectChimera.Data.Events;

namespace ProjectChimera.UI.Gameplay
{
    /// <summary>
    /// UI component for gameplay mode switching toggles
    /// Provides buttons for Cultivation, Construction, and Genetics modes
    /// Phase 2 implementation following roadmap requirements
    /// </summary>
    public class ModeToggleUI : MonoBehaviour
    {
        [Header("Mode Toggle Buttons")]
        [SerializeField] private Button _cultivationButton;
        [SerializeField] private Button _constructionButton;
        [SerializeField] private Button _geneticsButton;
        
        [Header("Button Visual States")]
        [SerializeField] private Color _activeColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color _inactiveColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        [SerializeField] private Color _hoverColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        
        [Header("Mode Labels")]
        [SerializeField] private Text _cultivationLabel;
        [SerializeField] private Text _constructionLabel;
        [SerializeField] private Text _geneticsLabel;
        
        [Header("Event Channels")]
        [SerializeField] private ModeChangedEventSO _modeChangedEvent;
        
        [Header("Configuration")]
        [SerializeField] private bool _showTooltips = true;
        [SerializeField] private bool _enableHotkeys = true;
        [SerializeField] private bool _debugMode = false;
        
        // Services
        private IGameplayModeController _modeController;
        
        // State tracking
        private GameplayMode _currentMode = GameplayMode.Cultivation;
        private bool _isInitialized = false;
        
        // Button references for easier management
        private System.Collections.Generic.Dictionary<GameplayMode, Button> _modeButtons;
        private System.Collections.Generic.Dictionary<GameplayMode, Text> _modeLabels;
        
        private void Start()
        {
            InitializeUI();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            UnsubscribeFromButtons();
        }
        
        private void Update()
        {
            // Handle keyboard shortcuts if enabled
            if (_enableHotkeys && _isInitialized)
            {
                HandleKeyboardInput();
            }
        }
        
        private void InitializeUI()
        {
            try
            {
                // Get the gameplay mode controller service
                _modeController = ProjectChimera.Core.DependencyInjection.ServiceLocator.Instance.GetService<IGameplayModeController>();
                
                if (_modeController == null)
                {
                    Debug.LogError("[ModeToggleUI] GameplayModeController service not found!");
                    return;
                }
                
                // Initialize button and label dictionaries
                SetupButtonDictionaries();
                
                // Subscribe to mode change events
                SubscribeToEvents();
                
                // Subscribe to button click events
                SubscribeToButtons();
                
                // Initialize visual state
                _currentMode = _modeController.CurrentMode;
                UpdateButtonVisuals(_currentMode);
                
                _isInitialized = true;
                
                if (_debugMode)
                {
                    Debug.Log($"[ModeToggleUI] Initialized with current mode: {_currentMode}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ModeToggleUI] Error during initialization: {ex.Message}");
            }
        }
        
        private void SetupButtonDictionaries()
        {
            _modeButtons = new System.Collections.Generic.Dictionary<GameplayMode, Button>
            {
                { GameplayMode.Cultivation, _cultivationButton },
                { GameplayMode.Construction, _constructionButton },
                { GameplayMode.Genetics, _geneticsButton }
            };
            
            _modeLabels = new System.Collections.Generic.Dictionary<GameplayMode, Text>
            {
                { GameplayMode.Cultivation, _cultivationLabel },
                { GameplayMode.Construction, _constructionLabel },
                { GameplayMode.Genetics, _geneticsLabel }
            };
            
            // Validate all required UI components are assigned
            ValidateUIComponents();
        }
        
        private void ValidateUIComponents()
        {
            foreach (var kvp in _modeButtons)
            {
                if (kvp.Value == null)
                {
                    Debug.LogError($"[ModeToggleUI] {kvp.Key} button is not assigned!");
                }
            }
            
            foreach (var kvp in _modeLabels)
            {
                if (kvp.Value == null)
                {
                    Debug.LogWarning($"[ModeToggleUI] {kvp.Key} label is not assigned (optional)");
                }
            }
        }
        
        private void SubscribeToEvents()
        {
            if (_modeChangedEvent != null)
            {
                _modeChangedEvent.Subscribe(OnModeChanged);
            }
            else
            {
                Debug.LogWarning("[ModeToggleUI] ModeChangedEvent not assigned - UI may not update properly");
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            if (_modeChangedEvent != null)
            {
                _modeChangedEvent.Unsubscribe(OnModeChanged);
            }
        }
        
        private void SubscribeToButtons()
        {
            if (_cultivationButton != null)
            {
                _cultivationButton.onClick.AddListener(() => OnModeButtonClicked(GameplayMode.Cultivation));
            }
            
            if (_constructionButton != null)
            {
                _constructionButton.onClick.AddListener(() => OnModeButtonClicked(GameplayMode.Construction));
            }
            
            if (_geneticsButton != null)
            {
                _geneticsButton.onClick.AddListener(() => OnModeButtonClicked(GameplayMode.Genetics));
            }
        }
        
        private void UnsubscribeFromButtons()
        {
            if (_cultivationButton != null)
            {
                _cultivationButton.onClick.RemoveAllListeners();
            }
            
            if (_constructionButton != null)
            {
                _constructionButton.onClick.RemoveAllListeners();
            }
            
            if (_geneticsButton != null)
            {
                _geneticsButton.onClick.RemoveAllListeners();
            }
        }
        
        private void HandleKeyboardInput()
        {
            // These hotkeys complement the GameplayModeController's keyboard handling
            // Providing visual feedback through UI when keys are pressed
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                HighlightButtonTemporarily(GameplayMode.Cultivation);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                HighlightButtonTemporarily(GameplayMode.Construction);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                HighlightButtonTemporarily(GameplayMode.Genetics);
            }
        }
        
        private void OnModeButtonClicked(GameplayMode mode)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[ModeToggleUI] UI not initialized, ignoring button click");
                return;
            }
            
            if (_debugMode)
            {
                Debug.Log($"[ModeToggleUI] Mode button clicked: {mode}");
            }
            
            // Phase 2 Verification: UI button produces identical behavior to keyboard
            Debug.Log($"[ModeToggleUI] Phase 2 Verification - UI button click for {mode} mode (identical to keyboard behavior)");
            
            // Set the mode through the controller (which will trigger the event)
            // This produces the same result as keyboard shortcuts - single event, same validation, same cooldown
            _modeController?.SetMode(mode, "UI Button");
        }
        
        private void OnModeChanged(ModeChangeEventData eventData)
        {
            if (_debugMode)
            {
                Debug.Log($"[ModeToggleUI] Received mode change event: {eventData.PreviousMode} → {eventData.NewMode}");
            }
            
            _currentMode = eventData.NewMode;
            UpdateButtonVisuals(_currentMode);
        }
        
        private void UpdateButtonVisuals(GameplayMode activeMode)
        {
            foreach (var kvp in _modeButtons)
            {
                var mode = kvp.Key;
                var button = kvp.Value;
                
                if (button == null) continue;
                
                var isActive = mode == activeMode;
                var targetColor = isActive ? _activeColor : _inactiveColor;
                
                // Update button color
                var colors = button.colors;
                colors.normalColor = targetColor;
                colors.highlightedColor = isActive ? _activeColor : _hoverColor;
                colors.selectedColor = targetColor;
                button.colors = colors;
                
                // Update label text color if available
                if (_modeLabels.TryGetValue(mode, out var label) && label != null)
                {
                    label.color = isActive ? Color.white : Color.black;
                }
            }
        }
        
        private void HighlightButtonTemporarily(GameplayMode mode)
        {
            if (_modeButtons.TryGetValue(mode, out var button) && button != null)
            {
                // Visual feedback for keyboard shortcut usage
                var colors = button.colors;
                var originalColor = colors.normalColor;
                
                colors.normalColor = _hoverColor;
                button.colors = colors;
                
                // Reset after brief delay
                StartCoroutine(ResetButtonColor(button, originalColor, 0.1f));
            }
        }
        
        private System.Collections.IEnumerator ResetButtonColor(Button button, Color originalColor, float delay)
        {
            yield return new UnityEngine.WaitForSeconds(delay);
            
            if (button != null)
            {
                var colors = button.colors;
                colors.normalColor = originalColor;
                button.colors = colors;
            }
        }
        
        /// <summary>
        /// Public method to manually refresh the UI state
        /// Useful for debugging or if synchronization is lost
        /// </summary>
        public void RefreshUI()
        {
            if (_isInitialized && _modeController != null)
            {
                _currentMode = _modeController.CurrentMode;
                UpdateButtonVisuals(_currentMode);
                
                if (_debugMode)
                {
                    Debug.Log($"[ModeToggleUI] UI refreshed, current mode: {_currentMode}");
                }
            }
        }
        
        /// <summary>
        /// Enable or disable the UI toggles
        /// </summary>
        public void SetUIEnabled(bool enabled)
        {
            foreach (var button in _modeButtons.Values)
            {
                if (button != null)
                {
                    button.interactable = enabled;
                }
            }
        }
        
        /// <summary>
        /// Set debug mode on/off at runtime
        /// </summary>
        public void SetDebugMode(bool enabled)
        {
            _debugMode = enabled;
            Debug.Log($"[ModeToggleUI] Debug mode {(enabled ? "enabled" : "disabled")}");
        }
        
        #if UNITY_EDITOR
        
        /// <summary>
        /// Editor-only method for setting up default colors
        /// </summary>
        [ContextMenu("Setup Default Colors")]
        private void SetupDefaultColors()
        {
            _activeColor = new Color(0.2f, 0.8f, 0.2f, 1f);      // Green
            _inactiveColor = new Color(0.7f, 0.7f, 0.7f, 1f);    // Light Gray
            _hoverColor = new Color(0.9f, 0.9f, 0.9f, 1f);       // Very Light Gray
            
            Debug.Log("[ModeToggleUI] Default colors set up");
        }
        
        /// <summary>
        /// Editor-only method for testing UI state
        /// </summary>
        [ContextMenu("Test UI Refresh")]
        private void TestUIRefresh()
        {
            if (Application.isPlaying)
            {
                RefreshUI();
            }
            else
            {
                Debug.Log("[ModeToggleUI] UI refresh only works during play mode");
            }
        }
        
        #endif
    }
}