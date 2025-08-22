using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.DependencyInjection;
using ProjectChimera.Data.Events;
using System.Linq;

namespace ProjectChimera.Systems.Gameplay
{
    /// <summary>
    /// Centralized controller for gameplay modes - drives UI context and overlays
    /// Phase 2 implementation following the roadmap requirements
    /// </summary>
    public class GameplayModeController : DIChimeraManager, IGameplayModeController
    {
        [Header("Mode Configuration")]
        [SerializeField] private GameplayMode _defaultMode = GameplayMode.Cultivation;
        [SerializeField] private bool _enableModeLogging = true;
        [SerializeField] private bool _enableKeyboardShortcuts = true;
        
        [Header("State Management")]
        [SerializeField] private bool _enableTransitionValidation = true;
        [SerializeField] private bool _enableModeHistory = true;
        [SerializeField] private int _maxHistorySize = 10;
        [SerializeField] private float _transitionCooldown = 0.1f; // Prevent rapid mode switching

        [Header("Event Channels")]
        [SerializeField] private ModeChangedEventSO _modeChangedEvent;

        // Current state
        private GameplayMode _currentMode;
        private GameplayMode _previousMode;
        
        // Advanced state management
        private System.Collections.Generic.List<ModeTransitionRecord> _modeHistory;
        private float _lastTransitionTime;
        private System.Collections.Generic.Dictionary<GameplayMode, System.Action> _modeEntryCallbacks;
        private System.Collections.Generic.Dictionary<GameplayMode, System.Action> _modeExitCallbacks;

        // Properties
        public GameplayMode CurrentMode => _currentMode;
        public GameplayMode PreviousMode => _previousMode;
        public bool IsInitialized { get; private set; }
        public System.Collections.Generic.IReadOnlyList<ModeTransitionRecord> ModeHistory => _modeHistory?.AsReadOnly();

        protected override void OnManagerInitialize()
        {
            Debug.Log("[GameplayModeController] Initializing Gameplay Mode Controller...");

            // Phase 2 Verification: Ensure ModeChangedEventSO is assigned
            if (_modeChangedEvent == null)
            {
                Debug.LogError("[GameplayModeController] Phase 2 Verification FAILED: ModeChangedEventSO is not assigned! Please assign the shared event asset.");
            }
            else
            {
                Debug.Log("[GameplayModeController] Phase 2 Verification: ModeChangedEventSO properly assigned");
            }

            // Initialize state management systems
            InitializeStateManagement();

            // Set default mode
            _currentMode = _defaultMode;
            _previousMode = _defaultMode;

            // Service registration is handled automatically by DIChimeraManager base class
            Debug.Log("[GameplayModeController] Service registration handled by DIChimeraManager");

            IsInitialized = true;

            // Log initial state
            if (_enableModeLogging)
            {
                Debug.Log($"[GameplayModeController] Initialized with mode: {_currentMode}");
            }
        }

        protected override void OnManagerShutdown()
        {
            Debug.Log("[GameplayModeController] Shutting down Gameplay Mode Controller...");

            // Service unregistration is handled automatically by DIChimeraManager base class

            IsInitialized = false;
        }

        private void Update()
        {
            if (!IsInitialized || !_enableKeyboardShortcuts) return;

            // Handle keyboard shortcuts for mode switching (1/2/3 keys)
            // Phase 2 Verification: Keyboard hotkeys with proper input handling
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                if (_enableModeLogging)
                    Debug.Log("[GameplayModeController] Keyboard shortcut '1' pressed - switching to Cultivation mode");
                SetMode(GameplayMode.Cultivation, "Keyboard");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                if (_enableModeLogging)
                    Debug.Log("[GameplayModeController] Keyboard shortcut '2' pressed - switching to Construction mode");
                SetMode(GameplayMode.Construction, "Keyboard");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                if (_enableModeLogging)
                    Debug.Log("[GameplayModeController] Keyboard shortcut '3' pressed - switching to Genetics mode");
                SetMode(GameplayMode.Genetics, "Keyboard");
            }
        }

        public bool SetMode(GameplayMode newMode, string triggerSource = "")
        {
            if (string.IsNullOrEmpty(triggerSource))
                triggerSource = "API";
                
            // Early exit if same mode
            if (newMode == _currentMode) return false;

            // Check transition cooldown
            if (Time.time - _lastTransitionTime < _transitionCooldown)
            {
                if (_enableModeLogging)
                {
                    Debug.LogWarning($"[GameplayModeController] Mode transition rejected due to cooldown: {_currentMode} → {newMode}");
                }
                return false;
            }

            // Validate transition if enabled
            if (_enableTransitionValidation && !IsValidModeTransition(_currentMode, newMode))
            {
                if (_enableModeLogging)
                {
                    Debug.LogWarning($"[GameplayModeController] Invalid mode transition rejected: {_currentMode} → {newMode}");
                }
                return false;
            }

            if (_enableModeLogging)
            {
                Debug.Log($"[GameplayModeController] Mode change: {_currentMode} → {newMode} (triggered by {triggerSource})");
            }

            // Execute exit callbacks for current mode
            ExecuteModeExitCallbacks(_currentMode);

            // Update state
            _previousMode = _currentMode;
            _currentMode = newMode;
            _lastTransitionTime = Time.time;

            // Record transition in history
            RecordModeTransition(_previousMode, _currentMode, triggerSource);

            // Execute entry callbacks for new mode
            ExecuteModeEntryCallbacks(_currentMode);

            // Emit mode changed event with detailed data
            EmitModeChangedEvent(triggerSource);
            
            return true;
        }


        #region Advanced State Management

        private void InitializeStateManagement()
        {
            if (_enableModeHistory)
            {
                _modeHistory = new System.Collections.Generic.List<ModeTransitionRecord>();
            }

            _modeEntryCallbacks = new System.Collections.Generic.Dictionary<GameplayMode, System.Action>();
            _modeExitCallbacks = new System.Collections.Generic.Dictionary<GameplayMode, System.Action>();
            _lastTransitionTime = 0f;

            Debug.Log("[GameplayModeController] State management initialized");
        }

        public bool IsValidModeTransition(GameplayMode from, GameplayMode to)
        {
            // Basic validation - can be extended with business rules
            if (from == to) return false;

            // Future business rules could include:
            // - Can't switch to Construction during active cultivation processes
            // - Can't switch to Genetics without completing tutorials
            // - Time-based restrictions (e.g., no mode switching during critical operations)
            
            return true; // All transitions currently allowed
        }

        private void RecordModeTransition(GameplayMode from, GameplayMode to, string triggerSource)
        {
            if (!_enableModeHistory || _modeHistory == null) return;

            var record = new ModeTransitionRecord(from, to, triggerSource);

            _modeHistory.Add(record);

            // Maintain history size limit
            if (_modeHistory.Count > _maxHistorySize)
            {
                _modeHistory.RemoveAt(0);
            }

            if (_enableModeLogging)
            {
                Debug.Log($"[GameplayModeController] Recorded transition: {from} → {to} (history size: {_modeHistory.Count})");
            }
        }

        private void ExecuteModeEntryCallbacks(GameplayMode mode)
        {
            if (_modeEntryCallbacks.TryGetValue(mode, out var callback))
            {
                try
                {
                    callback?.Invoke();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[GameplayModeController] Error in mode entry callback for {mode}: {ex.Message}");
                }
            }
        }

        private void ExecuteModeExitCallbacks(GameplayMode mode)
        {
            if (_modeExitCallbacks.TryGetValue(mode, out var callback))
            {
                try
                {
                    callback?.Invoke();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[GameplayModeController] Error in mode exit callback for {mode}: {ex.Message}");
                }
            }
        }

        private void EmitModeChangedEvent(string triggerSource)
        {
            if (_modeChangedEvent != null)
            {
                var eventData = new ModeChangeEventData
                {
                    NewMode = _currentMode,
                    PreviousMode = _previousMode,
                    Timestamp = System.DateTime.Now,
                    IsValid = true,
                    TriggerSource = triggerSource,
                    UserContext = _currentMode.ToString()
                };
                _modeChangedEvent.Invoke(eventData);
            }
            else if (_enableModeLogging)
            {
                Debug.LogWarning("[GameplayModeController] ModeChangedEvent not assigned - event not fired");
            }
        }

        public void RegisterModeEntryCallback(GameplayMode mode, System.Action callback)
        {
            if (!_modeEntryCallbacks.ContainsKey(mode))
            {
                _modeEntryCallbacks[mode] = callback;
            }
            else
            {
                _modeEntryCallbacks[mode] += callback;
            }
        }

        public void RegisterModeExitCallback(GameplayMode mode, System.Action callback)
        {
            if (!_modeExitCallbacks.ContainsKey(mode))
            {
                _modeExitCallbacks[mode] = callback;
            }
            else
            {
                _modeExitCallbacks[mode] += callback;
            }
        }


        #endregion
    }



}