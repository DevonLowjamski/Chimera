using UnityEngine;
using System.Threading.Tasks;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Core service interfaces for dependency injection
    /// Eliminates FindObjectOfType anti-patterns
    /// </summary>


    public interface IServiceHealthMonitor
    {
        bool IsHealthy { get; }
        void StartMonitoring();
        void StopMonitoring();
        void RegisterService<T>(T service) where T : class;
        void UnregisterService<T>(T service) where T : class;
    }

    public interface IGCOptimizationManager
    {
        bool IsOptimizationEnabled { get; set; }
        void Initialize();
        void ForceOptimizedGC();
        void NotifySceneTransitionStart();
        void NotifySceneTransitionEnd();
    }

    public interface IStreamingCoordinator
    {
        bool IsInitialized { get; }
        void Initialize();
        void SetQualityProfile(int profileIndex);
        void OptimizeStreaming();
        void ForceGarbageCollection();
    }

    public interface IMemoryProfiler
    {
        bool IsProfilerActive { get; }
        void StartProfiling();
        void StopProfiling();
        void TakeSnapshot();
        long GetCurrentMemoryUsage();
    }

    public interface IPoolManager
    {
        T GetPooledObject<T>() where T : Component;
        void ReturnToPool<T>(T obj) where T : Component;
        void CreatePool<T>(T prefab, int initialSize) where T : Component;
        void ClearPool<T>() where T : Component;
    }
}
