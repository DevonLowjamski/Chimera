using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Manages command registration, execution, and history for contextual menus.
    /// Extracted from ContextualMenuEventHandler.cs to reduce file size and improve separation of concerns.
    /// </summary>
    public class MenuCommandManager
    {
        // Command Management
        private readonly Dictionary<string, IMenuCommand> _registeredCommands = new Dictionary<string, IMenuCommand>();
        private readonly Queue<IMenuCommand> _commandHistory = new Queue<IMenuCommand>();
        private readonly Dictionary<string, List<string>> _modeCommands = new Dictionary<string, List<string>>();
        
        // Events
        public event Action<IMenuCommand, CommandResult> OnCommandExecuted;
        public event Action<string> OnCommandValidationFailed;
        
        public int CommandHistoryCount => _commandHistory.Count;
        public IEnumerable<string> AvailableModes => _modeCommands.Keys;
        
        public MenuCommandManager()
        {
            InitializeDefaultCommands();
        }
        
        /// <summary>
        /// Initializes default commands for each mode
        /// </summary>
        private void InitializeDefaultCommands()
        {
            // Construction Mode Commands
            var constructionCommands = new List<string>
            {
                "place-facility", "move-facility", "delete-facility",
                "rotate-facility", "upgrade-facility", "copy-facility",
                "save-schematic", "load-schematic", "confirm-placement",
                "cancel-placement", "show-facility-selector"
            };
            _modeCommands["construction"] = constructionCommands;
            
            // Cultivation Mode Commands
            var cultivationCommands = new List<string>
            {
                "plant-seed", "water-plant", "harvest-plant",
                "apply-nutrients", "prune-plant", "check-health",
                "set-schedule", "clone-plant", "configure-environment"
            };
            _modeCommands["cultivation"] = cultivationCommands;
            
            // Genetics Mode Commands
            var geneticsCommands = new List<string>
            {
                "view-genetics", "breed-plants", "analyze-traits",
                "save-strain", "load-strain", "mutate-genes",
                "cross-pollinate", "gene-edit", "select-parent",
                "execute-breeding", "cancel-breeding"
            };
            _modeCommands["genetics"] = geneticsCommands;
            
            ChimeraLogger.Log($"[MenuCommandManager] Initialized {_modeCommands.Count} command modes");
        }
        
        /// <summary>
        /// Registers a command for execution
        /// </summary>
        public void RegisterCommand(string commandId, IMenuCommand command)
        {
            if (string.IsNullOrEmpty(commandId) || command == null)
            {
                ChimeraLogger.LogWarning("[MenuCommandManager] Invalid command registration parameters");
                return;
            }
            
            _registeredCommands[commandId] = command;
            ChimeraLogger.Log($"[MenuCommandManager] Registered command: {commandId}");
        }
        
        /// <summary>
        /// Unregisters a command
        /// </summary>
        public void UnregisterCommand(string commandId)
        {
            if (_registeredCommands.Remove(commandId))
            {
                ChimeraLogger.Log($"[MenuCommandManager] Unregistered command: {commandId}");
            }
        }
        
        /// <summary>
        /// Executes a command with validation and error handling
        /// </summary>
        public CommandResult ExecuteCommand(IMenuCommand command)
        {
            if (command == null)
            {
                return CommandResult.Failure("Command is null");
            }
            
            try
            {
                // Check if command can execute
                if (!command.CanExecute())
                {
                    return CommandResult.Failure("Command cannot execute in current state");
                }
                
                // Execute the command
                var result = command.Execute();
                
                // Add to history if successful
                if (result.IsSuccess)
                {
                    AddToCommandHistory(command);
                }
                
                // Fire event
                OnCommandExecuted?.Invoke(command, result);
                
                return result;
            }
            catch (Exception ex)
            {
                var errorResult = CommandResult.Failure($"Command execution failed: {ex.Message}");
                OnCommandExecuted?.Invoke(command, errorResult);
                ChimeraLogger.LogError($"[MenuCommandManager] Command execution error: {ex}");
                return errorResult;
            }
        }
        
        /// <summary>
        /// Executes a command by ID
        /// </summary>
        public CommandResult ExecuteCommand(string commandId)
        {
            if (_registeredCommands.TryGetValue(commandId, out var command))
            {
                return ExecuteCommand(command);
            }
            
            var errorResult = CommandResult.Failure($"Command not found: {commandId}");
            OnCommandValidationFailed?.Invoke($"No command registered for ID: {commandId}");
            return errorResult;
        }
        
        /// <summary>
        /// Gets a registered command by ID
        /// </summary>
        public IMenuCommand GetCommand(string commandId)
        {
            return _registeredCommands.GetValueOrDefault(commandId);
        }
        
        /// <summary>
        /// Checks if a command is registered
        /// </summary>
        public bool IsCommandRegistered(string commandId)
        {
            return _registeredCommands.ContainsKey(commandId);
        }
        
        /// <summary>
        /// Gets available commands for a mode
        /// </summary>
        public List<string> GetAvailableCommands(string mode)
        {
            return _modeCommands.GetValueOrDefault(mode, new List<string>());
        }
        
        /// <summary>
        /// Checks if a command is available in a mode
        /// </summary>
        public bool IsCommandAvailableInMode(string commandId, string mode)
        {
            return _modeCommands.ContainsKey(mode) && _modeCommands[mode].Contains(commandId);
        }
        
        /// <summary>
        /// Adds a command to a mode
        /// </summary>
        public void AddCommandToMode(string mode, string commandId)
        {
            if (!_modeCommands.ContainsKey(mode))
            {
                _modeCommands[mode] = new List<string>();
            }
            
            if (!_modeCommands[mode].Contains(commandId))
            {
                _modeCommands[mode].Add(commandId);
                ChimeraLogger.Log($"[MenuCommandManager] Added command '{commandId}' to mode '{mode}'");
            }
        }
        
        /// <summary>
        /// Removes a command from a mode
        /// </summary>
        public void RemoveCommandFromMode(string mode, string commandId)
        {
            if (_modeCommands.ContainsKey(mode) && _modeCommands[mode].Remove(commandId))
            {
                ChimeraLogger.Log($"[MenuCommandManager] Removed command '{commandId}' from mode '{mode}'");
            }
        }
        
        /// <summary>
        /// Adds command to execution history
        /// </summary>
        private void AddToCommandHistory(IMenuCommand command)
        {
            _commandHistory.Enqueue(command);
            
            // Limit history size
            const int maxHistorySize = 50;
            while (_commandHistory.Count > maxHistorySize)
            {
                _commandHistory.Dequeue();
            }
        }
        
        /// <summary>
        /// Gets command history
        /// </summary>
        public List<IMenuCommand> GetCommandHistory()
        {
            return _commandHistory.ToList();
        }
        
        /// <summary>
        /// Gets command history for a specific command type
        /// </summary>
        public List<IMenuCommand> GetCommandHistory<T>() where T : IMenuCommand
        {
            return _commandHistory.Where(cmd => cmd is T).ToList();
        }
        
        /// <summary>
        /// Clears command history
        /// </summary>
        public void ClearHistory()
        {
            _commandHistory.Clear();
            ChimeraLogger.Log("[MenuCommandManager] Command history cleared");
        }
        
        /// <summary>
        /// Gets statistics about registered commands
        /// </summary>
        public CommandManagerStats GetStats()
        {
            return new CommandManagerStats
            {
                RegisteredCommandCount = _registeredCommands.Count,
                CommandHistoryCount = _commandHistory.Count,
                AvailableModeCount = _modeCommands.Count,
                TotalCommandsAcrossModes = _modeCommands.Values.SelectMany(cmds => cmds).Distinct().Count()
            };
        }
        
        /// <summary>
        /// Clears all registered commands and resets state
        /// </summary>
        public void Clear()
        {
            _registeredCommands.Clear();
            _commandHistory.Clear();
            // Keep mode commands as they are configuration
            
            ChimeraLogger.Log("[MenuCommandManager] Cleared all registered commands and history");
        }
    }
    
    /// <summary>
    /// Statistics about the command manager
    /// </summary>
    public class CommandManagerStats
    {
        public int RegisteredCommandCount { get; set; }
        public int CommandHistoryCount { get; set; }
        public int AvailableModeCount { get; set; }
        public int TotalCommandsAcrossModes { get; set; }
    }
}