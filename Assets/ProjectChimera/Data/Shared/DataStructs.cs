namespace ProjectChimera.Data.Shared
{
    public enum PlantGrowthStage
    {
        Unknown,          // Initial/undefined state
        Seed,
        Sprout,            // Early visible emergence
        Germination,      // Added for compatibility with legacy code
        Seedling,
        Vegetative,
        PreFlowering,
        PreFlower,        // Alias for compatibility with legacy enum
        Flowering,
        Harvestable,      // Stage indicating plant is ready to harvest
        Ripening,
        Mature,           // Alias used by some systems
        Harvest,
        Drying,
        Curing,
        Complete,         // Final completed state
        Dormant           // Post-harvest/seed dormancy state
    }

    [System.Serializable]
    public struct NutritionStatus
    {
        public float Nitrogen;
        public float Phosphorus;
        public float Potassium;
        public float pH;
    }

    [System.Serializable]
    public struct WaterStatus
    {
        public float MoistureLevel;
        public float DrainageRate;
        public float WaterQuality;
    }
}