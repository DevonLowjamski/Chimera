namespace ProjectChimera.Core.Performance
{
    /// <summary>
    /// Interface for UI metrics providers to replace reflection-based metric collection
    /// </summary>
    public interface IUIMetricsProvider
    {
        float FrameTime { get; }
        long MemoryUsage { get; }
        int ActiveComponents { get; }
        int UIDrawCalls { get; }
        float UIUpdateTime { get; }
    }
}
