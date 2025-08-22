namespace ProjectChimera.Core
{
    /// <summary>
    /// Enumeration of possible game states for the DIGameManager
    /// </summary>
    public enum GameState
    {
        /// <summary>
        /// Game has not been initialized yet
        /// </summary>
        Uninitialized,
        
        /// <summary>
        /// Game is currently initializing
        /// </summary>
        Initializing,
        
        /// <summary>
        /// Game is running normally
        /// </summary>
        Running,
        
        /// <summary>
        /// Game is paused
        /// </summary>
        Paused,
        
        /// <summary>
        /// Game is in an error state
        /// </summary>
        Error,
        
        /// <summary>
        /// Game is shutting down
        /// </summary>
        Shutdown,
        
        /// <summary>
        /// Game is shutting down (alternative name)
        /// </summary>
        Shutting_Down,
        
        /// <summary>
        /// Game is loading
        /// </summary>
        Loading,
        
        /// <summary>
        /// In-game state
        /// </summary>
        InGame,
        
        /// <summary>
        /// Main menu state
        /// </summary>
        MainMenu
    }
}
