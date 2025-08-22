namespace ProjectChimera.Core
{
    /// <summary>
    /// Base interface for all services in Project Chimera
    /// </summary>
    public interface IService
    {
        bool IsInitialized { get; }
        void Initialize();
        void Shutdown();
    }
}
