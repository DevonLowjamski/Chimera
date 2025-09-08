namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Interface for health check providers
    /// </summary>
    public interface IHealthCheckProvider
    {
        bool IsHealthy { get; }
        string GetHealthStatus();
        void PerformHealthCheck();
    }
}
