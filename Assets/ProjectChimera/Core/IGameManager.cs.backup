namespace ProjectChimera.Core
{
    /// <summary>
    /// Interface for the main game manager functionality
    /// Defines the contract for game lifecycle management
    /// </summary>
    public interface IGameManager
    {
        /// <summary>
        /// Current game state
        /// </summary>
        GameState CurrentGameState { get; }
        
        /// <summary>
        /// Whether the game is currently paused
        /// </summary>
        bool IsGamePaused { get; }
        
        /// <summary>
        /// Total time the game has been running
        /// </summary>
        System.TimeSpan TotalGameTime { get; }
        
        /// <summary>
        /// Pause the game
        /// </summary>
        void PauseGame();
        
        /// <summary>
        /// Resume the game
        /// </summary>
        void ResumeGame();
        
        /// <summary>
        /// Shutdown the game
        /// </summary>
        void ShutdownGame();
        
        /// <summary>
        /// Get a manager of a specific type
        /// </summary>
        T GetManager<T>() where T : ChimeraManager;
        
        /// <summary>
        /// Get a manager by type
        /// </summary>
        ChimeraManager GetManager(System.Type managerType);
        
        /// <summary>
        /// Register a manager
        /// </summary>
        void RegisterManager<T>(T manager) where T : ChimeraManager;
    }
}
