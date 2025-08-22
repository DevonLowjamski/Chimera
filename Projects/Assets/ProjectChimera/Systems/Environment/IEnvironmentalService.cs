namespace ProjectChimera.Systems.Environment
{
    /// <summary>
    /// PC014-2b: Base interface for all environmental services
    /// Provides common properties and methods for environmental system components
    /// </summary>
    public interface IEnvironmentalService
    {
        /// <summary>
        /// Whether this service is initialized and ready for use
        /// </summary>
        bool IsInitialized { get; }
        
        /// <summary>
        /// Initialize the service
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Shutdown the service and clean up resources
        /// </summary>
        void Shutdown();
    }
}