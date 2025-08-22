using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using System.Linq;
using ProjectChimera.UI.Components;
using ProjectChimera.Core;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Handles all click event wiring and command execution for contextual menus.
    /// Implements command pattern for menu actions with validation and error handling.
    /// Extracted from ContextualMenuController.cs for better maintainability.
    /// </summary>
    public class ContextualMenuEventHandler
    {
        // Core Components
        private readonly MenuCommandManager _commandManager = new MenuCommandManager();
        private readonly MenuCommandValidator _validator;
        private readonly MenuEventWiringManager _wiringManager = new MenuEventWiringManager();
        
        // Event forwarding
        public event System.Action<string, string> OnMenuItemClicked;
        public event System.Action<string> OnMenuOpened;
        public event System.Action<string> OnMenuClosed;
        public event System.Action<IMenuCommand, CommandResult> OnCommandExecuted;
        public event System.Action<string> OnCommandValidationFailed;
        
        public bool EventsEnabled => _validator.EventsEnabled;
        public string CurrentMode => _wiringManager.CurrentMode;
        public int CommandHistoryCount => _commandManager.CommandHistoryCount;
        
        public ContextualMenuEventHandler()
        {
            // Initialize validator with command manager
            _validator = new MenuCommandValidator(_commandManager);
            
            // Connect events
            ConnectComponentEvents();
        }
        
        /// <summary>
        /// Connects events between components
        /// </summary>
        private void ConnectComponentEvents()
        {
            // Forward events from wiring manager
            _wiringManager.OnMenuItemClicked += (mode, commandId) => {
                OnMenuItemClicked?.Invoke(mode, commandId);
                HandleMenuItemClick(commandId, mode);
            };
            _wiringManager.OnMenuOpened += (mode) => OnMenuOpened?.Invoke(mode);
            _wiringManager.OnMenuClosed += (mode) => OnMenuClosed?.Invoke(mode);
            
            // Forward events from command manager
            _commandManager.OnCommandExecuted += (cmd, result) => OnCommandExecuted?.Invoke(cmd, result);
            _commandManager.OnCommandValidationFailed += (msg) => OnCommandValidationFailed?.Invoke(msg);
        }
        
        /// <summary>
        /// Wires click events to a visual element
        /// </summary>
        public void WireClickEvent(VisualElement element, string commandId, string mode = null)
        {
            _wiringManager.WireClickEvent(element, commandId, mode);
        }
        
        /// <summary>
        /// Unwires click events from a visual element
        /// </summary>
        public void UnwireClickEvent(VisualElement element)
        {
            _wiringManager.UnwireClickEvent(element);
        }
        
        /// <summary>
        /// Handles menu item click events
        /// </summary>
        private void HandleMenuItemClick(string commandId, string mode)
        {
            try
            {
                // Validate the command
                var validationResult = _validator.ValidateCommand(commandId, mode);
                if (!validationResult.IsValid)
                {
                    OnCommandValidationFailed?.Invoke(validationResult.ErrorMessage);
                    Debug.LogWarning($"[ContextualMenuEventHandler] Command validation failed: {validationResult.ErrorMessage}");
                    return;
                }
                
                // Execute the command through command manager
                _commandManager.ExecuteCommand(commandId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ContextualMenuEventHandler] Error handling menu item click: {ex.Message}");
                OnCommandValidationFailed?.Invoke($"Error executing command: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Registers a command for execution
        /// </summary>
        public void RegisterCommand(string commandId, IMenuCommand command)
        {
            _commandManager.RegisterCommand(commandId, command);
        }
        
        /// <summary>
        /// Unregisters a command
        /// </summary>
        public void UnregisterCommand(string commandId)
        {
            _commandManager.UnregisterCommand(commandId);
        }
        
        /// <summary>
        /// Executes a command with validation and error handling
        /// </summary>
        public CommandResult ExecuteCommand(IMenuCommand command)
        {
            return _commandManager.ExecuteCommand(command);
        }
        
        
        /// <summary>
        /// Sets the current mode for event handling
        /// </summary>
        public void SetMode(string mode)
        {
            _wiringManager.SetMode(mode);
        }
        
        /// <summary>
        /// Enables or disables event handling
        /// </summary>
        public void SetEventsEnabled(bool enabled)
        {
            _validator.SetEventsEnabled(enabled);
            _wiringManager.SetEventsEnabled(enabled);
        }
        
        /// <summary>
        /// Gets available commands for a mode
        /// </summary>
        public List<string> GetAvailableCommands(string mode)
        {
            return _commandManager.GetAvailableCommands(mode);
        }
        
        
        /// <summary>
        /// Clears all event handlers and registered commands
        /// </summary>
        public void ClearAll()
        {
            _commandManager.Clear();
            _wiringManager.Clear();
            _validator.Reset();
            
            Debug.Log("[ContextualMenuEventHandler] Cleared all handlers and commands");
        }
    }
    

    
    /// <summary>
    /// Result of command validation
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; private set; }
        public string ErrorMessage { get; private set; }
        
        private ValidationResult(bool isValid, string errorMessage = null)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }
        
        public static ValidationResult Valid()
        {
            return new ValidationResult(true);
        }
        
        public static ValidationResult Invalid(string errorMessage)
        {
            return new ValidationResult(false, errorMessage);
        }
    }
    
    /// <summary>
    /// Command validator with fluent interface
    /// </summary>
    public class CommandValidator
    {
        private readonly List<System.Func<string, string, ValidationResult>> _validationRules = new List<System.Func<string, string, ValidationResult>>();
        
        public CommandValidator RequireMode(string requiredMode = null)
        {
            _validationRules.Add((commandId, mode) =>
            {
                if (requiredMode != null && mode != requiredMode)
                {
                    return ValidationResult.Invalid($"Command requires mode: {requiredMode}");
                }
                return ValidationResult.Valid();
            });
            return this;
        }
        
        public CommandValidator RequireValidCommand()
        {
            _validationRules.Add((commandId, mode) =>
            {
                if (string.IsNullOrEmpty(commandId))
                {
                    return ValidationResult.Invalid("Command ID cannot be empty");
                }
                return ValidationResult.Valid();
            });
            return this;
        }
        
        public CommandValidator RequireEnabledEvents()
        {
            _validationRules.Add((commandId, mode) =>
            {
                // This would need access to the handler instance
                return ValidationResult.Valid();
            });
            return this;
        }
        
        public CommandValidator RequireCustomValidation(System.Func<string, bool> customValidation)
        {
            _validationRules.Add((commandId, mode) =>
            {
                if (!customValidation(commandId))
                {
                    return ValidationResult.Invalid($"Custom validation failed for command: {commandId}");
                }
                return ValidationResult.Valid();
            });
            return this;
        }
        
        public ValidationResult Validate(string commandId, string mode)
        {
            foreach (var rule in _validationRules)
            {
                var result = rule(commandId, mode);
                if (!result.IsValid)
                {
                    return result;
                }
            }
            return ValidationResult.Valid();
        }
    }
}