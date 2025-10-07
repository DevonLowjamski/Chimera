using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// ITickable implementation for GrowthProcessor - Part of Phase 0.5 Central Update Bus
    /// Provides centralized update management for automated plant growth
    /// </summary>
    public partial class GrowthProcessor
    {
        // ITickable implementation - already defined in main GrowthProcessor.cs
        // This file exists for organizational purposes only

        // The actual ITickable methods are implemented in the main GrowthProcessor.cs:
        // - public void Tick(float deltaTime)
        // - public int TickPriority => TickPriority.PlantLifecycle;
        // - public bool IsTickable => _enableAutoGrowth;
    }
}