using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.SimpleDI;

namespace ProjectChimera.Core.Performance
{
    /// <summary>
    /// Standard metric collector implementations for the MetricsCollector system
    /// Provides IMetricCollector implementations for each core system
    /// </summary>

    /// <summary>
    /// Cultivation system metric collector
    /// </summary>
    public class CultivationMetricCollector : IMetricCollector
    {
        private IGameObjectRegistry _registry;

        public CultivationMetricCollector()
        {
            _registry = ServiceContainerFactory.Instance?.TryResolve<IGameObjectRegistry>();
        }

        public MetricSnapshot CollectMetrics()
        {
            var snapshot = new MetricSnapshot
            {
                SystemName = "Cultivation",
                Timestamp = Time.time,
                FrameCount = Time.frameCount,
                Metrics = new Dictionary<string, float>()
            };

            try
            {
                // Basic cultivation system metrics
                snapshot.Metrics["ActivePlants"] = GetActivePlantCount();
                snapshot.Metrics["GrowthRate"] = GetAverageGrowthRate();
                snapshot.Metrics["HarvestReadyPlants"] = GetHarvestReadyCount();
                snapshot.Metrics["MemoryUsage"] = GetCultivationMemoryUsage();
                snapshot.Metrics["UpdateTime"] = GetCultivationUpdateTime();
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError("METRICS", $"Error collecting cultivation metrics: {ex.Message}");
            }

            return snapshot;
        }

        private float GetActivePlantCount()
        {
            // Use registry instead of FindObjectsOfType
            if (_registry != null)
            {
                return _registry.GetTotalMonoBehaviourCount();
            }
            return 0f; // Fallback if registry not available
        }

        private float GetAverageGrowthRate()
        {
            return Random.Range(0.8f, 1.2f); // Placeholder
        }

        private float GetHarvestReadyCount()
        {
            return Random.Range(0f, 10f); // Placeholder
        }

        private float GetCultivationMemoryUsage()
        {
            return UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
        }

        private float GetCultivationUpdateTime()
        {
            return Time.deltaTime * 1000f; // Convert to ms
        }
    }

    /// <summary>
    /// Construction system metric collector
    /// </summary>
    public class ConstructionMetricCollector : IMetricCollector
    {
        public MetricSnapshot CollectMetrics()
        {
            var snapshot = new MetricSnapshot
            {
                SystemName = "Construction",
                Timestamp = Time.time,
                FrameCount = Time.frameCount,
                Metrics = new Dictionary<string, float>()
            };

            try
            {
                snapshot.Metrics["ActiveProjects"] = GetActiveProjectCount();
                snapshot.Metrics["CompletedProjects"] = GetCompletedProjectCount();
                snapshot.Metrics["ConstructionEfficiency"] = GetConstructionEfficiency();
                snapshot.Metrics["MemoryUsage"] = GetConstructionMemoryUsage();
                snapshot.Metrics["UpdateTime"] = GetConstructionUpdateTime();
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError("METRICS", $"Error collecting construction metrics: {ex.Message}");
            }

            return snapshot;
        }

        private float GetActiveProjectCount()
        {
            return Random.Range(0f, 5f); // Placeholder
        }

        private float GetCompletedProjectCount()
        {
            return Random.Range(0f, 20f); // Placeholder
        }

        private float GetConstructionEfficiency()
        {
            return Random.Range(0.7f, 1.0f); // Placeholder
        }

        private float GetConstructionMemoryUsage()
        {
            return UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
        }

        private float GetConstructionUpdateTime()
        {
            return Time.deltaTime * 1000f;
        }
    }

    /// <summary>
    /// UI system metric collector adapter
    /// </summary>
    public class UIMetricCollector : IMetricCollector
    {
        private IGameObjectRegistry _registry;

        public UIMetricCollector()
        {
            _registry = ServiceContainerFactory.Instance?.TryResolve<IGameObjectRegistry>();
        }

        public MetricSnapshot CollectMetrics()
        {
            var snapshot = new MetricSnapshot
            {
                SystemName = "UI",
                Timestamp = Time.time,
                FrameCount = Time.frameCount,
                Metrics = new Dictionary<string, float>()
            };

            try
            {
                // Try to resolve existing UI metrics collector from ServiceContainer
                var uiMetricsCollector = ServiceContainerFactory.Instance?.TryResolve<IUIMetricsProvider>();
                if (uiMetricsCollector != null)
                {
                    // Use interface-based access instead of reflection
                    snapshot.Metrics["FrameTime"] = uiMetricsCollector.FrameTime;
                    snapshot.Metrics["MemoryUsage"] = uiMetricsCollector.MemoryUsage / (1024f * 1024f);
                    snapshot.Metrics["ActiveComponents"] = uiMetricsCollector.ActiveComponents;
                    snapshot.Metrics["UIDrawCalls"] = uiMetricsCollector.UIDrawCalls;
                    snapshot.Metrics["UpdateTime"] = uiMetricsCollector.UIUpdateTime;
                }
                else
                {
                    // Fallback basic UI metrics
                    snapshot.Metrics["FrameTime"] = Time.deltaTime * 1000f;
                    snapshot.Metrics["MemoryUsage"] = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
                    snapshot.Metrics["ActiveComponents"] = GetUIComponentCount();
                    snapshot.Metrics["UIDrawCalls"] = GetUIDrawCallCount();
                    snapshot.Metrics["UpdateTime"] = Time.deltaTime * 1000f;
                }
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError("METRICS", $"Error collecting UI metrics: {ex.Message}");
            }

            return snapshot;
        }

        private float GetUIComponentCount()
        {
            // Use registry instead of FindObjectsOfType
            if (_registry != null)
            {
                return _registry.GetUIComponentCount();
            }
            return 0f; // Fallback if registry not available
        }

        private float GetUIDrawCallCount()
        {
            return Random.Range(10f, 50f); // Placeholder - actual draw call counting is complex
        }
    }

    /// <summary>
    /// Save system metric collector
    /// </summary>
    public class SaveSystemMetricCollector : IMetricCollector
    {
        public MetricSnapshot CollectMetrics()
        {
            var snapshot = new MetricSnapshot
            {
                SystemName = "SaveSystem",
                Timestamp = Time.time,
                FrameCount = Time.frameCount,
                Metrics = new Dictionary<string, float>()
            };

            try
            {
                snapshot.Metrics["SaveCount"] = GetSaveCount();
                snapshot.Metrics["LoadCount"] = GetLoadCount();
                snapshot.Metrics["SaveFileSize"] = GetSaveFileSize();
                snapshot.Metrics["LastSaveTime"] = GetLastSaveTime();
                snapshot.Metrics["MemoryUsage"] = GetSaveSystemMemoryUsage();
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError("METRICS", $"Error collecting save system metrics: {ex.Message}");
            }

            return snapshot;
        }

        private float GetSaveCount()
        {
            return Random.Range(0f, 100f); // Placeholder
        }

        private float GetLoadCount()
        {
            return Random.Range(0f, 50f); // Placeholder
        }

        private float GetSaveFileSize()
        {
            return Random.Range(1f, 100f); // MB, placeholder
        }

        private float GetLastSaveTime()
        {
            return Time.time - Random.Range(0f, 300f); // Within last 5 minutes
        }

        private float GetSaveSystemMemoryUsage()
        {
            return UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
        }
    }

    /// <summary>
    /// Basic system performance metric collector
    /// </summary>
    public class SystemPerformanceCollector : IMetricCollector
    {
        private IGameObjectRegistry _registry;

        public SystemPerformanceCollector()
        {
            _registry = ServiceContainerFactory.Instance?.TryResolve<IGameObjectRegistry>();
        }

        public MetricSnapshot CollectMetrics()
        {
            var snapshot = new MetricSnapshot
            {
                SystemName = "SystemPerformance",
                Timestamp = Time.time,
                FrameCount = Time.frameCount,
                Metrics = new Dictionary<string, float>()
            };

            try
            {
                snapshot.Metrics["FPS"] = 1.0f / Time.deltaTime;
                snapshot.Metrics["FrameTime"] = Time.deltaTime * 1000f;
                snapshot.Metrics["CPU_Usage"] = GetCPUUsage();
                snapshot.Metrics["ActiveGameObjects"] = GetActiveGameObjectCount();
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError("METRICS", $"Error collecting system performance metrics: {ex.Message}");
            }

            return snapshot;
        }

        private float GetCPUUsage()
        {
            return Random.Range(10f, 80f); // Placeholder
        }

        private float GetActiveGameObjectCount()
        {
            // Use registry instead of FindObjectsOfType
            if (_registry != null)
            {
                return _registry.GetTotalGameObjectCount();
            }
            return 0f; // Fallback if registry not available
        }
    }

    /// <summary>
    /// Memory usage metric collector
    /// </summary>
    public class MemoryUsageCollector : IMetricCollector
    {
        public MetricSnapshot CollectMetrics()
        {
            var snapshot = new MetricSnapshot
            {
                SystemName = "MemoryUsage",
                Timestamp = Time.time,
                FrameCount = Time.frameCount,
                Metrics = new Dictionary<string, float>()
            };

            try
            {
                snapshot.Metrics["TotalMemory"] = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
                snapshot.Metrics["ReservedMemory"] = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / (1024f * 1024f);
                snapshot.Metrics["UnusedReservedMemory"] = UnityEngine.Profiling.Profiler.GetTotalUnusedReservedMemoryLong() / (1024f * 1024f);
                snapshot.Metrics["GCMemory"] = System.GC.GetTotalMemory(false) / (1024f * 1024f);
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError("METRICS", $"Error collecting memory metrics: {ex.Message}");
            }

            return snapshot;
        }
    }

    /// <summary>
    /// Cultivation system metric collector alias
    /// </summary>
    public class CultivationSystemCollector : CultivationMetricCollector { }

    /// <summary>
    /// Construction system metric collector alias
    /// </summary>
    public class ConstructionSystemCollector : ConstructionMetricCollector { }

    /// <summary>
    /// UI system metric collector alias
    /// </summary>
    public class UISystemCollector : UIMetricCollector { }

    /// <summary>
    /// Save system metric collector alias
    /// </summary>
    public class SaveSystemCollector : SaveSystemMetricCollector { }

    /// <summary>
    /// Economy system metric collector alias
    /// </summary>
    public class EconomySystemCollector : EconomyMetricCollector { }

    /// <summary>
    /// Economy system metric collector
    /// </summary>
    public class EconomyMetricCollector : IMetricCollector
    {
        public MetricSnapshot CollectMetrics()
        {
            var snapshot = new MetricSnapshot
            {
                SystemName = "Economy",
                Timestamp = Time.time,
                FrameCount = Time.frameCount,
                Metrics = new Dictionary<string, float>()
            };

            try
            {
                snapshot.Metrics["PlayerCurrency"] = GetPlayerCurrency();
                snapshot.Metrics["TransactionCount"] = GetTransactionCount();
                snapshot.Metrics["MarketActivity"] = GetMarketActivity();
                snapshot.Metrics["EconomyBalance"] = GetEconomyBalance();
                snapshot.Metrics["MemoryUsage"] = GetEconomyMemoryUsage();
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError("METRICS", $"Error collecting economy metrics: {ex.Message}");
            }

            return snapshot;
        }

        private float GetPlayerCurrency()
        {
            return Random.Range(1000f, 50000f); // Placeholder
        }

        private float GetTransactionCount()
        {
            return Random.Range(0f, 100f); // Placeholder
        }

        private float GetMarketActivity()
        {
            return Random.Range(0.5f, 2.0f); // Activity multiplier
        }

        private float GetEconomyBalance()
        {
            return Random.Range(0.8f, 1.2f); // Balance factor
        }

        private float GetEconomyMemoryUsage()
        {
            return UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory() / (1024f * 1024f);
        }
    }
}

