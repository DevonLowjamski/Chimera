using UnityEngine;
using ProjectChimera.Data.Shared;
using System;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Rendering
{
    /// <summary>
    /// Rendering service interfaces for dependency injection
    /// Eliminates FindObjectOfType anti-patterns in rendering systems
    /// </summary>

    // Placeholder types for missing rendering classes
    public enum RenderingQuality
    {
        Low,
        Medium,
        High,
        Ultra
    }

    [System.Serializable]
    public class PlantRenderingData
    {
        public string PlantId;
        public Vector3 Position;
        public float Scale;
        public Color LeafColor;
        public float Health;

        // Additional properties for advanced rendering
        public Quaternion Rotation = Quaternion.identity;
        public int LODLevel = 0;
        public PlantGrowthStage GrowthStage = PlantGrowthStage.Seedling;
        public bool CastShadows = true;
        public bool ReceiveShadows = true;
        public Matrix4x4 TransformMatrix = Matrix4x4.identity;
        public Vector4 PlantParameters = Vector4.zero;
        public Color PlantColor = Color.green;
    }

    [System.Serializable]
    public class GrowLight
    {
        public string LightId;
        public int ID;
        public Vector3 Position;
        public Color Color;
        public float Intensity;
        public float Range;
        public bool IsActive;
        public GameObject LightObject;

        // Additional properties for CustomLightingRenderer compatibility
        public UnityEngine.LightType LightType = UnityEngine.LightType.Point;
        public UnityEngine.LightShadows ShadowType = UnityEngine.LightShadows.None;
        public object CoreReference; // Reference to core system object
    }

    public enum WeatherType
    {
        Clear,
        Cloudy,
        Overcast,
        LightRain,
        HeavyRain,
        Rainy,
        Storm,
        Stormy,
        Fog,
        Foggy,
        Snow,
        Snowy,
        Windy
    }

    public interface IWindAffected
    {
        void ApplyWindForce(Vector3 windDirection, float windStrength);
        Vector3 Position { get; }
        bool IsActive { get; }
    }

    public interface IAdvancedRenderingManager
    {
        bool IsInitialized { get; }
        RenderingQuality CurrentQuality { get; }
        void Initialize();
        void SetRenderingQuality(RenderingQuality quality);
        void RegisterPlantForRendering(GameObject plantObject, PlantRenderingData renderData);
        void UnregisterPlantFromRendering(GameObject plantObject);
        void OptimizeRendering();
    }

    public interface IPlantInstancedRenderer
    {
        int MaxInstances { get; }
        int RegisteredCount { get; }
        int VisibleCount { get; }
        bool IsInitialized { get; }
        void Initialize(int maxInstances, float cullingDistance);
        int RegisterPlant(GameObject plantObject, PlantRenderingData renderData);
        void UnregisterPlant(GameObject plantObject);
        void UpdatePlantData(GameObject plantObject, PlantRenderingData renderData);
        void UpdateRenderer();
    }

    public interface ICustomLightingRenderer
    {
        bool IsInitialized { get; }
        int ActiveLightCount { get; }
        int GrowLightCount { get; }
        void Initialize(bool enableVolumetric = false);
        GrowLight AddGrowLight(Vector3 position, Color color, float intensity, float range);
        void RemoveGrowLight(GrowLight growLight);
        void UpdateLighting();
        void OptimizeLighting();
    }

    public interface IEnvironmentalRenderer
    {
        bool IsInitialized { get; }
        WeatherType CurrentWeather { get; }
        Vector3 CurrentWindDirection { get; }
        float CurrentWindStrength { get; }
        void Initialize();
        void SetWeather(WeatherType weatherType, bool immediate = false);
        void SetWind(Vector3 direction, float strength);
        void RegisterWindAffectedObject(IWindAffected windObject);
        void UnregisterWindAffectedObject(IWindAffected windObject);
        void UpdateEnvironmental();
        void OptimizeEnvironmental();
    }

    public interface ILightingService
    {
        Light GetMainLight();
        void SetMainLight(Light light);
        Light[] GetAllLights();
        void RegisterLight(Light light);
        void UnregisterLight(Light light);
    }
}
