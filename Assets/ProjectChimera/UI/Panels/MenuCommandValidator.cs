using UnityEngine;
using System.Collections.Generic;
using System;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Validates menu commands before execution with configurable validation rules.
    /// Extracted from ContextualMenuEventHandler.cs to reduce file size and improve separation of concerns.
    /// </summary>
    public class MenuCommandValidator
    {
        private readonly HashSet<string> _validModes = new HashSet<string> { "construction", "cultivation", "genetics" };
        private readonly Dictionary<string, CommandValidator> _commandValidators = new Dictionary<string, CommandValidator>();
        private readonly MenuCommandManager _commandManager;
        private bool _eventsEnabled = true;
        
        public bool EventsEnabled => _eventsEnabled;
        
        public MenuCommandValidator(MenuCommandManager commandManager)
        {
            _commandManager = commandManager ?? throw new ArgumentNullException(nameof(commandManager));
            InitializeCommandValidators();
        }
        
        /// <summary>
        /// Initializes command validators for each command type
        /// </summary>
        private void InitializeCommandValidators()
        {
            // Basic validation for all commands
            var basicValidator = new CommandValidator()
                .RequireMode()
                .RequireValidCommand()
                .RequireEnabledEvents();
            
            // Construction-specific validation
            var constructionValidator = new CommandValidator()
                .RequireMode("construction")
                .RequireValidCommand()
                .RequireEnabledEvents()
                .RequireCustomValidation(cmd => ValidateConstructionCommand(cmd));
            
            // Genetics-specific validation
            var geneticsValidator = new CommandValidator()
                .RequireMode("genetics")
                .RequireValidCommand()
                .RequireEnabledEvents()
                .RequireCustomValidation(cmd => ValidateGeneticsCommand(cmd));
            
            // Cultivation-specific validation
            var cultivationValidator = new CommandValidator()
                .RequireMode("cultivation")
                .RequireValidCommand()
                .RequireEnabledEvents()
                .RequireCustomValidation(cmd => ValidateCultivationCommand(cmd));
            
            // Apply validators to specific commands
            foreach (var command in _commandManager.GetAvailableCommands("construction"))
            {
                _commandValidators[command] = constructionValidator;
            }
            
            foreach (var command in _commandManager.GetAvailableCommands("genetics"))
            {
                _commandValidators[command] = geneticsValidator;
            }
            
            foreach (var command in _commandManager.GetAvailableCommands("cultivation"))
            {
                _commandValidators[command] = cultivationValidator;
            }
            
            // Set basic validator as default for any remaining commands
            foreach (var mode in _commandManager.AvailableModes)
            {
                foreach (var command in _commandManager.GetAvailableCommands(mode))
                {
                    if (!_commandValidators.ContainsKey(command))
                    {
                        _commandValidators[command] = basicValidator;
                    }
                }
            }
            
            ChimeraLogger.Log($"[MenuCommandValidator] Initialized validators for {_commandValidators.Count} commands");
        }
        
        /// <summary>
        /// Validates a command before execution
        /// </summary>
        public ValidationResult ValidateCommand(string commandId, string mode)
        {
            // Check if events are enabled
            if (!_eventsEnabled)
            {
                return ValidationResult.Invalid("Events are disabled");
            }
            
            // Check if mode is valid
            if (!_validModes.Contains(mode))
            {
                return ValidationResult.Invalid($"Invalid mode: {mode}");
            }
            
            // Check if command exists for mode
            if (!_commandManager.IsCommandAvailableInMode(commandId, mode))
            {
                return ValidationResult.Invalid($"Command {commandId} not available in mode {mode}");
            }
            
            // Use specific validator if available
            if (_commandValidators.TryGetValue(commandId, out var validator))
            {
                return validator.Validate(commandId, mode);
            }
            
            return ValidationResult.Valid();
        }
        
        /// <summary>
        /// Validates construction-specific commands
        /// </summary>
        private bool ValidateConstructionCommand(string commandId)
        {
            switch (commandId)
            {
                case "place-facility":
                case "move-facility":
                case "delete-facility":
                    // Could check for selection, valid position, etc.
                    return true;
                case "save-schematic":
                case "load-schematic":
                    // Could check for valid selection or available schematics
                    return true;
                case "confirm-placement":
                    // Could check if there's an active placement
                    return true;
                case "cancel-placement":
                    // Could check if there's an active placement to cancel
                    return true;
                default:
                    return true;
            }
        }
        
        /// <summary>
        /// Validates genetics-specific commands
        /// </summary>
        private bool ValidateGeneticsCommand(string commandId)
        {
            switch (commandId)
            {
                case "breed-plants":
                case "cross-pollinate":
                    // Could check if two plants are selected
                    return true;
                case "analyze-traits":
                case "view-genetics":
                    // Could check if at least one plant is selected
                    return true;
                case "save-strain":
                case "load-strain":
                    // Could check for valid strain data
                    return true;
                case "execute-breeding":
                    // Could check if parents are selected
                    return true;
                default:
                    return true;
            }
        }
        
        /// <summary>
        /// Validates cultivation-specific commands
        /// </summary>
        private bool ValidateCultivationCommand(string commandId)
        {
            switch (commandId)
            {
                case "plant-seed":
                    // Could check if valid planting location and seeds available
                    return true;
                case "water-plant":
                case "apply-nutrients":
                case "prune-plant":
                    // Could check if plant is selected and action is appropriate
                    return true;
                case "harvest-plant":
                    // Could check if plant is ready for harvest
                    return true;
                case "check-health":
                    // Could check if plant is selected
                    return true;
                default:
                    return true;
            }
        }
        
        /// <summary>
        /// Adds a custom validator for a command
        /// </summary>
        public void AddCustomValidator(string commandId, CommandValidator validator)
        {
            if (string.IsNullOrEmpty(commandId) || validator == null)
            {
                ChimeraLogger.LogWarning("[MenuCommandValidator] Invalid validator parameters");
                return;
            }
            
            _commandValidators[commandId] = validator;
            ChimeraLogger.Log($"[MenuCommandValidator] Added custom validator for command: {commandId}");
        }
        
        /// <summary>
        /// Removes a validator for a command
        /// </summary>
        public void RemoveValidator(string commandId)
        {
            if (_commandValidators.Remove(commandId))
            {
                ChimeraLogger.Log($"[MenuCommandValidator] Removed validator for command: {commandId}");
            }
        }
        
        /// <summary>
        /// Adds a valid mode
        /// </summary>
        public void AddValidMode(string mode)
        {
            if (!string.IsNullOrEmpty(mode) && _validModes.Add(mode))
            {
                ChimeraLogger.Log($"[MenuCommandValidator] Added valid mode: {mode}");
            }
        }
        
        /// <summary>
        /// Removes a valid mode
        /// </summary>
        public void RemoveValidMode(string mode)
        {
            if (_validModes.Remove(mode))
            {
                ChimeraLogger.Log($"[MenuCommandValidator] Removed valid mode: {mode}");
            }
        }
        
        /// <summary>
        /// Enables or disables event handling
        /// </summary>
        public void SetEventsEnabled(bool enabled)
        {
            _eventsEnabled = enabled;
            ChimeraLogger.Log($"[MenuCommandValidator] Events {(enabled ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// Gets all valid modes
        /// </summary>
        public HashSet<string> GetValidModes()
        {
            return new HashSet<string>(_validModes);
        }
        
        /// <summary>
        /// Gets validation statistics
        /// </summary>
        public ValidationStats GetValidationStats()
        {
            return new ValidationStats
            {
                ValidatorCount = _commandValidators.Count,
                ValidModeCount = _validModes.Count,
                EventsEnabled = _eventsEnabled
            };
        }
        
        /// <summary>
        /// Clears all validators and resets to defaults
        /// </summary>
        public void Reset()
        {
            _commandValidators.Clear();
            _eventsEnabled = true;
            InitializeCommandValidators();
            ChimeraLogger.Log("[MenuCommandValidator] Reset to default state");
        }
    }
    
    /// <summary>
    /// Validation statistics
    /// </summary>
    public class ValidationStats
    {
        public int ValidatorCount { get; set; }
        public int ValidModeCount { get; set; }
        public bool EventsEnabled { get; set; }
    }
}