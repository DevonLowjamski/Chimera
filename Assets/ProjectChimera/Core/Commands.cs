using System;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Interface for menu commands
    /// </summary>
    public interface IMenuCommand
    {
        string CommandId { get; }
        string DisplayName { get; }
        bool CanExecute();
        CommandResult Execute();
        CommandResult Undo();
    }
    
    /// <summary>
    /// Result of command execution
    /// </summary>
    public class CommandResult
    {
        public bool IsSuccess { get; private set; }
        public string Message { get; private set; }
        public object Data { get; private set; }
        
        private CommandResult(bool success, string message, object data = null)
        {
            IsSuccess = success;
            Message = message;
            Data = data;
        }
        
        public static CommandResult Success(string message = "Success", object data = null)
        {
            return new CommandResult(true, message, data);
        }
        
        public static CommandResult Failure(string message)
        {
            return new CommandResult(false, message);
        }
    }
}
