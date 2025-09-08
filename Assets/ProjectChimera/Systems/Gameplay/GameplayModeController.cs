using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Updates;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Events;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Gameplay
{
    /// <summary>
    /// Centralized controller for gameplay modes - drives UI context and overlays
    /// Phase 2 implementation following the roadmap requirements
    /// </summary>
    public class GameplayModeController : DIChimeraManager, ITickable, IGameplayModeController
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
        // Additional event channels can be added when implemented
        // [SerializeField] private ModeTransitionEventSO _modeTransitionEvent;
        // [SerializeField] private InputModeEventSO _inputModeEvent;
        
        #region State Management
        
        private GameplayMode _currentMode;
        private GameplayMode _previousMode;
        private float _lastTransitionTime;
        private List<GameplayMode> _modeHistory = new List<GameplayMode>();
        
        private Dictionary<GameplayMode, System.Action> _modeEntryCallbacks = new Dictionary<GameplayMode, System.Action>();
        private Dictionary<GameplayMode, System.Action> _modeExitCallbacks = new Dictionary<GameplayMode, System.Action>();
        
        #endregion
        
        #region Properties
        
        public GameplayMode CurrentMode => _currentMode;
        public GameplayMode PreviousMode => _previousMode;
        public bool CanTransition => Time.time - _lastTransitionTime >= _transitionCooldown;
        public List<GameplayMode> BasicModeHistory => new List<GameplayMode>(_modeHistory);
        
        // IGameplayModeController implementation
        public bool IsInitialized => isActiveAndEnabled;
        public IReadOnlyList<ModeTransitionRecord> ModeHistory => _transitionHistory;
        
        private List<ModeTransitionRecord> _transitionHistory = new List<ModeTransitionRecord>();
        
        #endregion
        
        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
            _currentMode = _defaultMode;
            _previousMode = _defaultMode;
            
            if (_enableModeLogging)
            {
                ChimeraLogger.Log($"[GameplayModeController] Initialized with default mode: {_defaultMode}");
            }
        }

        protected override void Start()
        {
            base.Start();
            // Register with UpdateOrchestrator
            UpdateOrchestrator.Instance?.RegisterTickable(this);
        }

        protected override void OnDestroy()
        {
            // Unregister from UpdateOrchestrator
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
            base.OnDestroy();
        }
        
        private void Update()
        {
            if (_enableKeyboardShortcuts)
            {
                HandleModeShortcuts();
            }
        }
        
        #endregion
        
        #region Mode Transition
        
        public bool SetMode(GameplayMode newMode, string triggerSource = "Manual")
        {
            return TrySetMode(newMode, triggerSource);
        }
        
        public bool TrySetMode(GameplayMode newMode, string triggerSource = "Manual")
        {
            if (!CanTransition)
            {
                if (_enableModeLogging)
                {
                    ChimeraLogger.LogWarning($"[GameplayModeController] Mode transition blocked by cooldown. Current: {_currentMode}, Requested: {newMode}");
                }
                return false;
            }
            
            if (_currentMode == newMode)
            {
                if (_enableModeLogging)
                {
                    ChimeraLogger.LogWarning($"[GameplayModeController] Already in mode: {newMode}");
                }
                return false;
            }
            
            if (_enableTransitionValidation && !ValidateTransition(_currentMode, newMode))
            {
                if (_enableModeLogging)
                {
                    ChimeraLogger.LogWarning($"[GameplayModeController] Invalid transition: {_currentMode} -> {newMode}");
                }
                return false;
            }
            
            // Execute transition
            _previousMode = _currentMode;
            _currentMode = newMode;
            _lastTransitionTime = Time.time;
            
            // Update history
            if (_enableModeHistory)
            {
                UpdateModeHistory(newMode);
                _transitionHistory.Add(new ModeTransitionRecord(_previousMode, newMode, triggerSource));
            }
            
            // Execute callbacks
            ExecuteModeExitCallbacks(_previousMode);
            ExecuteModeEntryCallbacks(_currentMode);
            
            // Fire events
            EmitModeChangedEvent(triggerSource);
            
            if (_enableModeLogging)
            {
                ChimeraLogger.Log($"[GameplayModeController] Mode changed: {_previousMode} -> {_currentMode} (Source: {triggerSource})");
            }
            
            return true;
        }
        
        public void ForceSetMode(GameplayMode mode, string triggerSource = "Force")
        {
            _previousMode = _currentMode;
            _currentMode = mode;
            _lastTransitionTime = Time.time;
            
            if (_enableModeHistory)
            {
                UpdateModeHistory(mode);
            }
            
            ExecuteModeExitCallbacks(_previousMode);
            ExecuteModeEntryCallbacks(_currentMode);
            EmitModeChangedEvent(triggerSource);
            
            if (_enableModeLogging)
            {
                ChimeraLogger.Log($"[GameplayModeController] Mode forced: {_previousMode} -> {_currentMode} (Source: {triggerSource})");
            }
        }
        
        #endregion
        
        #region Private Methods
        
        public bool IsValidModeTransition(GameplayMode fromMode, GameplayMode toMode)
        {
            return ValidateTransition(fromMode, toMode);
        }
        
        private bool ValidateTransition(GameplayMode from, GameplayMode to)
        {
            // Basic validation - can be extended based on game rules
            return true;
        }
        
        private void UpdateModeHistory(GameplayMode mode)
        {
            _modeHistory.Add(mode);
            
            if (_modeHistory.Count > _maxHistorySize)
            {
                _modeHistory.RemoveAt(0);
            }
        }
        
        private void HandleModeShortcuts()
        {
            if (Input.GetKeyDown(KeyCode.F1))
                TrySetMode(GameplayMode.Cultivation, "Keyboard");
            else if (Input.GetKeyDown(KeyCode.F2))
                TrySetMode(GameplayMode.Construction, "Keyboard");
            else if (Input.GetKeyDown(KeyCode.F3))
                TrySetMode(GameplayMode.Genetics, "Keyboard");
            else if (Input.GetKeyDown(KeyCode.F4))
                TrySetMode(GameplayMode.Business, "Keyboard");
            else if (Input.GetKeyDown(KeyCode.F5))
                TrySetMode(GameplayMode.Research, "Keyboard");
        }
        
        private void ExecuteModeEntryCallbacks(GameplayMode mode)
        {
            if (_modeEntryCallbacks.TryGetValue(mode, out var callback))
            {
                callback?.Invoke();
            }
        }
        
        private void ExecuteModeExitCallbacks(GameplayMode mode)
        {
            if (_modeExitCallbacks.TryGetValue(mode, out var callback))
            {
                callback?.Invoke();
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
                ChimeraLogger.LogWarning("[GameplayModeController] ModeChangedEvent not assigned - event not fired");
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

        // ITickable implementation
        public int Priority => 0;
        public bool Enabled => enabled && gameObject.activeInHierarchy;
        
        public void Tick(float deltaTime)
        {
            // No per-frame logic needed - mode changes are event-driven
        }
        
        public virtual void OnRegistered() 
        { 
            // Override in derived classes if needed
        }
        
        public virtual void OnUnregistered() 
        { 
            // Override in derived classes if needed
        }

        // ChimeraManager abstract method implementations
        protected override void OnManagerInitialize()
        {
            // Manager initialization logic
            if (_enableModeLogging)
            {
                ChimeraLogger.Log("[GameplayModeController] Manager initialized successfully");
            }
        }

        protected override void OnManagerShutdown()
        {
            // Cleanup mode history and callbacks
            _modeHistory.Clear();
            _transitionHistory.Clear();
            _modeEntryCallbacks.Clear();
            _modeExitCallbacks.Clear();
            
            if (_enableModeLogging)
            {
                ChimeraLogger.Log("[GameplayModeController] Manager shutdown completed");
            }
        }

        #endregion
    }
}