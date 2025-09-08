namespace ProjectChimera.Systems.Analytics
{
    public interface ICultivationManager
    {
        float GetTotalYieldHarvested();
        int GetActivePlantCount();
        float AveragePlantHealth { get; }
        int ActivePlantCount { get; }
        float TotalYieldHarvested { get; }
        int TotalPlantsHarvested { get; }
        // Add other cultivation-related metrics needed by analytics
    }

    /// <summary>
    /// Placeholder implementation of ICultivationManager
    /// This will be replaced when proper system integration is completed
    /// </summary>
    public class CultivationManagerPlaceholder : ICultivationManager
    {
        public float GetTotalYieldHarvested()
        {
            return 0f;
        }

        public int GetActivePlantCount()
        {
            return 0;
        }

        public float AveragePlantHealth => 1f;
        public int ActivePlantCount => 0;
        public float TotalYieldHarvested => 0f;
        public int TotalPlantsHarvested => 0;
    }
}
