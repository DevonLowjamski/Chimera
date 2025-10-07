using UnityEngine;
using System;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Services.SpeedTree
{
    /// <summary>
    /// Interface for Cannabis Genetics Service
    /// </summary>
    public interface ICannabisGeneticsService
    {
        bool IsInitialized { get; }
        void Initialize();
        void Shutdown();
        void UpdateGenetics(float deltaTime);
        void ProcessGeneticVariation(int plantId, GeneticProfile profile);
        void UpdateGrowthStage(int plantId, GrowthStage stage);
        void ApplyTraitExpression(int plantId, string[] traits);
        GeneticProfile GetGeneticProfile(int plantId);
        GrowthStage GetCurrentGrowthStage(int plantId);
    }

    /// <summary>
    /// Interface for SpeedTree Asset Service
    /// </summary>
    public interface ISpeedTreeAssetService
    {
        bool IsInitialized { get; }
        void Initialize();
        void Shutdown();
        void LoadAsset(string assetPath);
        void UnloadAsset(string assetPath);
        void SetQualitySettings(SpeedTreeQualitySettings settings);
        void RefreshRenderers();
        bool IsAssetLoaded(string assetPath);
        int GetLoadedAssetCount();
    }

    /// <summary>
    /// Supporting types for genetics system
    /// </summary>
    [System.Serializable]
    public class GeneticProfile
    {
        public string ProfileId;
        public string[] DominantTraits;
        public string[] RecessiveTraits;
        public float GrowthRate;
        public float YieldPotential;
        public float ResistanceLevel;
    }

    public enum GrowthStage
    {
        Seedling,
        Vegetative,
        PreFlower,
        Flower,
        Harvest
    }

    [System.Serializable]
    public class SpeedTreeQualitySettings
    {
        public int LODCount = 4;
        public float LODDistanceMultiplier = 1f;
        public bool EnableShadows = true;
        public bool EnableWind = true;
        public int MaxVisibleTrees = 1000;
    }
}