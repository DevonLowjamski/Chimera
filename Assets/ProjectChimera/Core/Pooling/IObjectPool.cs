namespace ProjectChimera.Core.Pooling
{
    /// <summary>
    /// Interface for object pools to allow stats access without reflection
    /// </summary>
    public interface IObjectPool
    {
        int CountInactive { get; }
        int CountActive { get; }
        int CountAll { get; }
    }
}
