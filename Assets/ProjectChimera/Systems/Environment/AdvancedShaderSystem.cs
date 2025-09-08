using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using UnityEngine;
#if UNITY_PIPELINE_URP
using UnityEngine.Rendering.Universal;
#endif
using ProjectChimera.Core;
using ProjectChimera.Data.Shared;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Facilities;
// using ProjectChimera.Systems.Cultivation; // Removed to avoid cross-assembly dependency
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;

namespace ProjectChimera.Systems.Environment
{
    /// <summary>
    /// Advanced shader system for realistic plant materials, spectrum visualization,
    /// and dynamic environmental effects in cannabis cultivation simulation.
    /// </summary>
    public class AdvancedShaderSystem : MonoBehaviour, ITickable
    {
        [Header("Shader Configuration")]
        [SerializeField] private bool _enableDynamicShaders = true;
        [SerializeField] private bool _enableSpectrumVisualization = true;
        [SerializeField] private bool _enablePlantMaturation = true;
        [SerializeField] private bool _enableEnvironmentalEffects = true;
        [SerializeField] private bool _enablePerformanceOptimization = true;

        [Header("Plant Shaders")]
        [SerializeField] private Shader _plantBaseShader;
        [SerializeField] private Shader _plantSpectrumShader;
        [SerializeField] private Shader _trichomeShader;
        [SerializeField] private Shader _flowerShader;
        [SerializeField] private Shader _leafShader;

        [Header("Environment Shaders")]
        [SerializeField] private Shader _lightBeamShader;
        [SerializeField] private Shader _spectrumVisualizationShader;
        [SerializeField] private Shader _environmentalEffectShader;
        [SerializeField] private Shader _growMediumShader;

        [Header("Performance")]
        [SerializeField] private int _maxMaterialInstances = 500;
        [SerializeField] private float _lodDistanceMultiplier = 1f;
        [SerializeField] private bool _enableBatching = true;
        [SerializeField] private bool _enableGPUInstancing = true;

        // Material Management
        private Dictionary<string, Material> _plantMaterials = new Dictionary<string, Material>();
        private Dictionary<string, MaterialPropertyBlock> _propertyBlocks = new Dictionary<string, MaterialPropertyBlock>();
        private List<MaterialInstance> _activeMaterials = new List<MaterialInstance>();

        // Spectrum Visualization
        private Material _spectrumVisualizationMaterial;
        private RenderTexture _spectrumTexture;
        private ComputeShader _spectrumCompute;

        // Plant Maturation System
        private Dictionary<ProjectChimera.Data.Shared.PlantGrowthStage, MaterialTemplate> _growthStageMaterials;
        private AnimationCurve _maturationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        // Environmental Effects
        private Material _airParticleMaterial;
        private Material _moistureEffectMaterial;
        private Material _temperatureEffectMaterial;

        // Performance Monitoring
        private int _materialUpdatesPerFrame = 0;
        private float _shaderComputeTime = 0f;
        private Queue<float> _performanceHistory = new Queue<float>();

        // Shader Properties
        private static readonly int SpectrumR = Shader.PropertyToID("_SpectrumR");
        private static readonly int SpectrumG = Shader.PropertyToID("_SpectrumG");
        private static readonly int SpectrumB = Shader.PropertyToID("_SpectrumB");
        private static readonly int SpectrumUV = Shader.PropertyToID("_SpectrumUV");
        private static readonly int SpectrumFR = Shader.PropertyToID("_SpectrumFR");
        private static readonly int MaturationLevel = Shader.PropertyToID("_MaturationLevel");
        private static readonly int HealthLevel = Shader.PropertyToID("_HealthLevel");
        private static readonly int MoistureLevel = Shader.PropertyToID("_MoistureLevel");
        private static readonly int TemperatureLevel = Shader.PropertyToID("_TemperatureLevel");
        private static readonly int TrichomeDensity = Shader.PropertyToID("_TrichomeDensity");
        private static readonly int EnvironmentalStress = Shader.PropertyToID("_EnvironmentalStress");

        // Events
        public System.Action<string, Material> OnMaterialCreated;
        public System.Action<string, MaterialPropertyBlock> OnMaterialUpdated;
        public System.Action<float> OnPerformanceChanged;

        private void Awake()
        {
            InitializeShaderSystem();
        }

        private void Start()
        {
        // Register with UpdateOrchestrator
        UpdateOrchestrator.Instance?.RegisterTickable(this);
            LoadShaders();
            SetupMaterialTemplates();
            InitializeSpectrumVisualization();
            StartPerformanceMonitoring();
        }

            public void Tick(float deltaTime)
    {
            float startTime = Time.realtimeSinceStartup;

            _materialUpdatesPerFrame = 0;

            if (_enableDynamicShaders)
            {
                UpdateDynamicMaterials();

    }

            if (_enableSpectrumVisualization)
            {
                UpdateSpectrumVisualization();
            }

            if (_enableEnvironmentalEffects)
            {
                UpdateEnvironmentalEffects();
            }

            _shaderComputeTime = Time.realtimeSinceStartup - startTime;
            UpdatePerformanceMetrics();
        }

        #region Initialization

        private void InitializeShaderSystem()
        {
            _growthStageMaterials = new Dictionary<ProjectChimera.Data.Shared.PlantGrowthStage, MaterialTemplate>();

            // Create render texture for spectrum visualization
            _spectrumTexture = new RenderTexture(256, 64, 0, RenderTextureFormat.ARGB32);
            _spectrumTexture.enableRandomWrite = true;
            _spectrumTexture.Create();
        }

        private void LoadShaders()
        {
            // Load default shaders if not assigned
            if (_plantBaseShader == null)
                _plantBaseShader = Shader.Find("Universal Render Pipeline/Lit");

            if (_lightBeamShader == null)
                _lightBeamShader = Shader.Find("Universal Render Pipeline/Unlit");

            if (_spectrumVisualizationShader == null)
                _spectrumVisualizationShader = Shader.Find("ProjectChimera/SpectrumVisualization");
        }

        private void SetupMaterialTemplates()
        {
            // Create material templates for each growth stage
            _growthStageMaterials[ProjectChimera.Data.Shared.PlantGrowthStage.Seed] = CreateMaterialTemplate(
                "Seed Material", Color.brown, 0.1f, 0f, 0f);

            _growthStageMaterials[ProjectChimera.Data.Shared.PlantGrowthStage.Seedling] = CreateMaterialTemplate(
                "Seedling Material", Color.green, 0.3f, 0f, 0f);

            _growthStageMaterials[ProjectChimera.Data.Shared.PlantGrowthStage.Vegetative] = CreateMaterialTemplate(
                "Vegetative Material", new Color(0.2f, 0.8f, 0.2f), 0.6f, 0.1f, 0f);

            _growthStageMaterials[ProjectChimera.Data.Shared.PlantGrowthStage.Flowering] = CreateMaterialTemplate(
                "Flowering Material", new Color(0.3f, 0.7f, 0.3f), 0.8f, 0.5f, 0.3f);

            _growthStageMaterials[ProjectChimera.Data.Shared.PlantGrowthStage.Harvest] = CreateMaterialTemplate(
                "Harvest Material", new Color(0.4f, 0.6f, 0.2f), 1f, 0.8f, 0.7f);
        }

        private MaterialTemplate CreateMaterialTemplate(string name, Color baseColor,
                                                       float maturation, float trichomes, float resin)
        {
            return new MaterialTemplate
            {
                Name = name,
                BaseColor = baseColor,
                MaturationLevel = maturation,
                TrichomeDensity = trichomes,
                ResinContent = resin,
                Metallic = 0.1f,
                Smoothness = 0.3f + resin * 0.4f,
                NormalStrength = 1f,
                EmissionIntensity = trichomes * 0.2f
            };
        }

        #endregion

        #region Material Management

        public Material CreatePlantMaterial(string plantId, ProjectChimera.Data.Shared.PlantGrowthStage growthStage)
        {
            if (_plantMaterials.ContainsKey(plantId))
            {
                return _plantMaterials[plantId];
            }

            var template = _growthStageMaterials[growthStage];
            Material material = new Material(_plantBaseShader);

            // Apply template properties
            ApplyMaterialTemplate(material, template);

            // Store material
            _plantMaterials[plantId] = material;

            // Create property block for dynamic updates
            var propertyBlock = new MaterialPropertyBlock();
            _propertyBlocks[plantId] = propertyBlock;

            // Track material instance
            _activeMaterials.Add(new MaterialInstance
            {
                PlantId = plantId,
                Material = material,
                PropertyBlock = propertyBlock,
                GrowthStage = growthStage,
                LastUpdate = Time.time
            });

            OnMaterialCreated?.Invoke(plantId, material);
            return material;
        }

        private void ApplyMaterialTemplate(Material material, MaterialTemplate template)
        {
            material.SetColor("_BaseColor", template.BaseColor);
            material.SetFloat("_Metallic", template.Metallic);
            material.SetFloat("_Smoothness", template.Smoothness);
            material.SetFloat(MaturationLevel, template.MaturationLevel);
            material.SetFloat(TrichomeDensity, template.TrichomeDensity);

            if (template.EmissionIntensity > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", template.BaseColor * template.EmissionIntensity);
            }
        }

        public void UpdatePlantMaterial(string plantId, PlantData plantData)
        {
            if (!_propertyBlocks.TryGetValue(plantId, out var propertyBlock))
                return;

            var materialInstance = _activeMaterials.FirstOrDefault(m => m.PlantId == plantId);
            if (materialInstance == null) return;

            // Update maturation based on growth progress
            float maturation = CalculateMaturationLevel(plantData);
            propertyBlock.SetFloat(MaturationLevel, maturation);

            // Update health effects
            propertyBlock.SetFloat(HealthLevel, plantData.Health / 100f);

            // Update moisture effects
            propertyBlock.SetFloat(MoistureLevel, plantData.WaterLevel / 100f);

            // Update spectrum response
            if (plantData.LightSpectrum.GetTotalPAR() > 0f)
            {
                ApplySpectrumEffects(propertyBlock, plantData.LightSpectrum);
            }

            // Update environmental stress
            float stress = CalculateEnvironmentalStress(plantData);
            propertyBlock.SetFloat(EnvironmentalStress, stress);

            // Update trichome development for flowering plants
            if (materialInstance.GrowthStage == ProjectChimera.Data.Shared.PlantGrowthStage.Flowering ||
                materialInstance.GrowthStage == ProjectChimera.Data.Shared.PlantGrowthStage.Harvest)
            {
                float trichomes = CalculateTrichomeDensity(plantData);
                propertyBlock.SetFloat(TrichomeDensity, trichomes);
            }

            materialInstance.LastUpdate = Time.time;
            _materialUpdatesPerFrame++;

            OnMaterialUpdated?.Invoke(plantId, propertyBlock);
        }

        private float CalculateMaturationLevel(PlantData plantData)
        {
            // Calculate maturation based on growth stage and progress
            float baseMaturation = plantData.GrowthStage switch
            {
                ProjectChimera.Data.Shared.PlantGrowthStage.Seed => 0.1f,
                ProjectChimera.Data.Shared.PlantGrowthStage.Seedling => 0.3f,
                ProjectChimera.Data.Shared.PlantGrowthStage.Vegetative => 0.6f,
                ProjectChimera.Data.Shared.PlantGrowthStage.Flowering => 0.8f,
                ProjectChimera.Data.Shared.PlantGrowthStage.Harvest => 1f,
                _ => 0.5f
            };

            // Apply growth progress within stage
            float progressModifier = plantData.GrowthProgress * 0.2f;
            float maturation = baseMaturation + progressModifier;

            return _maturationCurve.Evaluate(Mathf.Clamp01(maturation));
        }

        private float CalculateEnvironmentalStress(PlantData plantData)
        {
            float stress = 0f;

            // Temperature stress
            if (plantData.Temperature < 18f || plantData.Temperature > 30f)
            {
                stress += Mathf.Abs(plantData.Temperature - 24f) / 10f;
            }

            // Humidity stress
            if (plantData.Humidity < 40f || plantData.Humidity > 80f)
            {
                stress += Mathf.Abs(plantData.Humidity - 60f) / 40f;
            }

            // Light stress
            if (plantData.LightIntensity < 200f)
            {
                stress += (200f - plantData.LightIntensity) / 200f;
            }

            // Nutrient stress
            if (plantData.NutrientLevel < 50f)
            {
                stress += (50f - plantData.NutrientLevel) / 50f;
            }

            return Mathf.Clamp01(stress);
        }

        private float CalculateTrichomeDensity(PlantData plantData)
        {
            if (plantData.GrowthStage != ProjectChimera.Data.Shared.PlantGrowthStage.Flowering &&
                plantData.GrowthStage != ProjectChimera.Data.Shared.PlantGrowthStage.Harvest)
                return 0f;

            float baseDensity = plantData.GrowthProgress;

            // Environmental factors affect trichome development
            float lightFactor = Mathf.Clamp01(plantData.LightIntensity / 800f);
            float tempFactor = 1f - Mathf.Abs(plantData.Temperature - 22f) / 10f;
            float healthFactor = plantData.Health / 100f;

            float density = baseDensity * lightFactor * tempFactor * healthFactor;
            return Mathf.Clamp01(density);
        }

        #endregion

        #region Spectrum Visualization

        private void InitializeSpectrumVisualization()
        {
            if (_spectrumVisualizationShader != null)
            {
                _spectrumVisualizationMaterial = new Material(_spectrumVisualizationShader);
                _spectrumVisualizationMaterial.SetTexture("_SpectrumTexture", _spectrumTexture);
            }
        }

        private void UpdateSpectrumVisualization()
        {
            // Find all active grow lights
            var growLights = UnityEngine.Object.FindObjectsByType<AdvancedGrowLightSystem>(FindObjectsSortMode.None);

            foreach (var light in growLights)
            {
                if (light.IsOn)
                {
                    UpdateLightSpectrumVisualization(light);
                }
            }
        }

        private void UpdateLightSpectrumVisualization(AdvancedGrowLightSystem lightSystem)
        {
            if (!_enableSpectrumVisualization || lightSystem == null) return;

            var spectrum = lightSystem.CurrentSpectrum;
            if (spectrum.GetTotalPAR() <= 0f) return;

            // Set spectrum data for visualization shader
            _spectrumVisualizationMaterial.SetFloat(SpectrumR, spectrum.Red_630_660nm);
            _spectrumVisualizationMaterial.SetFloat(SpectrumG, spectrum.Green_490_550nm);
            _spectrumVisualizationMaterial.SetFloat(SpectrumB, spectrum.Blue_420_490nm);
            _spectrumVisualizationMaterial.SetFloat(SpectrumUV, spectrum.UV_A_315_400nm);
            _spectrumVisualizationMaterial.SetFloat(SpectrumFR, spectrum.FarRed_700_850nm);

            // Render spectrum visualization
            Graphics.Blit(null, _spectrumTexture, _spectrumVisualizationMaterial);

            RenderTexture.active = null;
        }

        private void ApplySpectrumEffects(MaterialPropertyBlock propertyBlock, LightSpectrumData spectrum)
        {
            if (spectrum.GetTotalPAR() <= 0f) return;

            // Apply spectrum values to shader properties
            propertyBlock.SetFloat(SpectrumR, spectrum.Red_630_660nm);
            propertyBlock.SetFloat(SpectrumG, spectrum.Green_490_550nm);
            propertyBlock.SetFloat(SpectrumB, spectrum.Blue_420_490nm);
            propertyBlock.SetFloat(SpectrumUV, spectrum.UV_A_315_400nm);
            propertyBlock.SetFloat(SpectrumFR, spectrum.FarRed_700_850nm);

            // Calculate overall spectrum intensity
            float totalIntensity = spectrum.Red_630_660nm + spectrum.Green_490_550nm + spectrum.Blue_420_490nm + spectrum.UV_A_315_400nm + spectrum.FarRed_700_850nm;

            // Apply spectrum-based color tinting
            Color spectrumColor = new Color(
                spectrum.Red_630_660nm / totalIntensity,
                spectrum.Green_490_550nm / totalIntensity,
                spectrum.Blue_420_490nm / totalIntensity,
                1f
            );

            propertyBlock.SetColor("_SpectrumTint", spectrumColor);
            propertyBlock.SetFloat("_SpectrumIntensity", totalIntensity);
        }

        #endregion

        #region Environmental Effects

        private void UpdateEnvironmentalEffects()
        {
            UpdateAtmosphericEffects();
            UpdateTemperatureEffects();
            UpdateHumidityEffects();
        }

        private void UpdateAtmosphericEffects()
        {
            // Create air particle effects based on air flow
            // var ventilationSystems = UnityEngine.Object.FindObjectsByType<VentilationController>(FindObjectsSortMode.None);

            // foreach (var vent in ventilationSystems)
            // {
            //     if (vent.AirFlowRate > 0.5f)
            //     {
            //         CreateAirFlowVisualization(vent);
            //     }
            // }
        }

        private void UpdateTemperatureEffects()
        {
            // Create heat shimmer effects for high temperatures
            // var hvacSystems = UnityEngine.Object.FindObjectsByType<HVACController>(FindObjectsSortMode.None);

            // foreach (var hvac in hvacSystems)
            // {
            //     if (hvac.CurrentTemperature > 28f)
            //     {
            //         CreateHeatShimmerEffect(hvac);
            //     }
            // }
        }

        private void UpdateHumidityEffects()
        {
            // Create moisture condensation effects for high humidity
            // var hvacSystems = UnityEngine.Object.FindObjectsByType<HVACController>(FindObjectsSortMode.None);

            // foreach (var hvac in hvacSystems)
            // {
            //     if (hvac.CurrentHumidity > 75f)
            //     {
            //         CreateMoistureEffect(hvac);
            //     }
            // }
        }

        // private void CreateAirFlowVisualization(VentilationController vent)
        // {
            // if (_airParticleMaterial == null)
            // {
                // _airParticleMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                // _airParticleMaterial.SetColor("_BaseColor", new Color(0.8f, 0.8f, 1f, 0.1f));
            // }

            // Apply air flow material properties
            // var particleSystem = vent.GetComponent<ParticleSystem>();
            // if (particleSystem != null)
            // {
            //     var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            //     renderer.material = _airParticleMaterial;
            // }
        // }

        // private void CreateHeatShimmerEffect(HVACController hvac)
        // {
            // if (_temperatureEffectMaterial == null)
            // {
                // _temperatureEffectMaterial = new Material(Shader.Find("ProjectChimera/HeatShimmer"));
            // }

            // Update heat shimmer intensity based on temperature
            // float intensity = (hvac.CurrentTemperature - 28f) / 10f;
            // _temperatureEffectMaterial.SetFloat("_ShimmerIntensity", intensity);
        // }

        // private void CreateMoistureEffect(HVACController hvac)
        // {
            // if (_moistureEffectMaterial == null)
            // {
                // _moistureEffectMaterial = new Material(Shader.Find("ProjectChimera/Moisture"));
            // }

            // Update moisture effect based on humidity
            // float density = (hvac.CurrentHumidity - 75f) / 25f;
            // _moistureEffectMaterial.SetFloat("_MoistureDensity", density);
        // }

        #endregion

        #region Dynamic Materials

        private void UpdateDynamicMaterials()
        {
            // Update all active plant materials
            foreach (var materialInstance in _activeMaterials)
            {
                if (Time.time - materialInstance.LastUpdate > 0.1f) // Update every 100ms
                {
                    // var plantData = GetPlantData(materialInstance.PlantId);
                    // if (plantData != null)
                    // {
                    //     UpdatePlantMaterial(materialInstance.PlantId, plantData);
                    // }
                }
            }

            // Clean up unused materials
            CleanupUnusedMaterials();
        }

        // private PlantData GetPlantData(string plantId)
        // {
        //     // Find plant component by ID
        //     var plants = UnityEngine.Object.FindObjectsByType<InteractivePlantComponent>(FindObjectsSortMode.None);
        //     var plant = plants.FirstOrDefault(p => p.PlantData?.PlantId == plantId);
        //
        //     if (plant != null)
        //     {
        //         return new PlantData
        //         {
        //             PlantId = plantId,
        //             GrowthStage = plant.CurrentStage,
        //             GrowthProgress = plant.GrowthProgress,
        //             Health = plant.Health,
        //             WaterLevel = plant.WaterLevel,
        //             NutrientLevel = plant.NutrientLevel,
        //             Temperature = plant.CurrentConditions.Temperature,
        //             Humidity = plant.CurrentConditions.Humidity,
        //             LightIntensity = plant.CurrentConditions.LightIntensity,
        //             LightSpectrum = null // Would need to get from light system
        //         };
        //     }
        //
        //     return null;
        // }

        private void CleanupUnusedMaterials()
        {
            var toRemove = new List<MaterialInstance>();

            foreach (var materialInstance in _activeMaterials)
            {
                // Check if plant still exists
                // var plantData = GetPlantData(materialInstance.PlantId);
                // if (plantData == null)
                // {
                //     toRemove.Add(materialInstance);
                // }
            }

            foreach (var instance in toRemove)
            {
                RemoveMaterialInstance(instance);
            }
        }

        private void RemoveMaterialInstance(MaterialInstance instance)
        {
            _activeMaterials.Remove(instance);
            _plantMaterials.Remove(instance.PlantId);
            _propertyBlocks.Remove(instance.PlantId);

            if (instance.Material != null)
            {
                DestroyImmediate(instance.Material);
            }
        }

        #endregion

        #region Performance Monitoring

        private void StartPerformanceMonitoring()
        {
            InvokeRepeating(nameof(CollectPerformanceData), 1f, 1f);
        }

        private void UpdatePerformanceMetrics()
        {
            // Calculate performance score
            float performanceScore = 1f;

            // Penalize high material update counts
            if (_materialUpdatesPerFrame > 50)
            {
                performanceScore *= 0.8f;
            }

            // Penalize high compute time
            if (_shaderComputeTime > 0.016f) // 16ms (60 FPS target)
            {
                performanceScore *= 0.7f;
            }

            // Penalize too many active materials
            if (_activeMaterials.Count > _maxMaterialInstances)
            {
                performanceScore *= 0.6f;
            }

            OnPerformanceChanged?.Invoke(performanceScore);
        }

        private void CollectPerformanceData()
        {
            float currentPerformance = CalculatePerformanceScore();
            _performanceHistory.Enqueue(currentPerformance);

            // Maintain history size
            while (_performanceHistory.Count > 60) // 1 minute of data
            {
                _performanceHistory.Dequeue();
            }

            // Auto-optimize if performance is consistently low
            if (_performanceHistory.Count >= 10)
            {
                float avgPerformance = _performanceHistory.Average();
                if (avgPerformance < 0.7f)
                {
                    OptimizePerformance();
                }
            }
        }

        private float CalculatePerformanceScore()
        {
            float score = 1f;

            // Factor in material count
            score *= 1f - (float)_activeMaterials.Count / (_maxMaterialInstances * 2);

            // Factor in update frequency
            score *= 1f - _materialUpdatesPerFrame / 100f;

            // Factor in compute time
            score *= 1f - _shaderComputeTime / 0.033f; // 33ms max

            return Mathf.Clamp01(score);
        }

        private void OptimizePerformance()
        {
            ChimeraLogger.Log("Auto-optimizing shader system performance");

            // Reduce update frequency for distant materials
            foreach (var instance in _activeMaterials)
            {
                float distance = Vector3.Distance(Camera.main.transform.position,
                                                 GetPlantPosition(instance.PlantId));

                if (distance > 20f * _lodDistanceMultiplier)
                {
                    // Reduce update frequency for distant plants
                    instance.UpdateInterval = 1f; // Update every second instead
                }
            }

            // Enable GPU instancing if available
            if (_enableGPUInstancing && SystemInfo.supportsInstancing)
            {
                EnableGPUInstancing();
            }
        }

        private Vector3 GetPlantPosition(string plantId)
        {
            // var plants = UnityEngine.Object.FindObjectsByType<InteractivePlantComponent>(FindObjectsSortMode.None);
            // var plant = plants.FirstOrDefault(p => p.PlantData?.PlantId == plantId);
            // return plant?.transform.position ?? Vector3.zero;
            return Vector3.zero; // Placeholder - InteractivePlantComponent not available in Environment assembly
        }

        private void EnableGPUInstancing()
        {
            foreach (var material in _plantMaterials.Values)
            {
                material.enableInstancing = true;
            }
        }

        #endregion

        #region Public Interface

        public void SetSpectrumVisualizationEnabled(bool enabled)
        {
            _enableSpectrumVisualization = enabled;
        }

        public void SetEnvironmentalEffectsEnabled(bool enabled)
        {
            _enableEnvironmentalEffects = enabled;
        }

        public void SetPerformanceOptimizationEnabled(bool enabled)
        {
            _enablePerformanceOptimization = enabled;
        }

        public Material GetPlantMaterial(string plantId)
        {
            return _plantMaterials.TryGetValue(plantId, out var material) ? material : null;
        }

        public void ForceUpdateAllMaterials()
        {
            foreach (var instance in _activeMaterials)
            {
                // var plantData = GetPlantData(instance.PlantId);
                // if (plantData != null)
                // {
                //     UpdatePlantMaterial(instance.PlantId, plantData);
                // }
            }
        }

        public ShaderSystemStatus GetSystemStatus()
        {
            return new ShaderSystemStatus
            {
                ActiveMaterials = _activeMaterials.Count,
                MaterialUpdatesPerFrame = _materialUpdatesPerFrame,
                ShaderComputeTime = _shaderComputeTime,
                PerformanceScore = CalculatePerformanceScore(),
                SpectrumVisualizationEnabled = _enableSpectrumVisualization,
                EnvironmentalEffectsEnabled = _enableEnvironmentalEffects
            };
        }

        #endregion

        private void OnDestroy()
        {
        // Unregister from UpdateOrchestrator
        UpdateOrchestrator.Instance?.UnregisterTickable(this);
            // Clean up render textures and materials
            if (_spectrumTexture != null)
            {
                _spectrumTexture.Release();
                DestroyImmediate(_spectrumTexture);
            }

            foreach (var material in _plantMaterials.Values)
            {
                if (material != null)
                {
                    DestroyImmediate(material);
                }
            }

            CancelInvoke();
        }

    // ITickable implementation
    public int Priority => 0;
    public bool Enabled => enabled && gameObject.activeInHierarchy;

    public virtual void OnRegistered()
    {
        // Override in derived classes if needed
    }

    public virtual void OnUnregistered()
    {
        // Override in derived classes if needed
    }

}

    // Supporting data structures
    [System.Serializable]
    public class MaterialInstance
    {
        public string PlantId;
        public Material Material;
        public MaterialPropertyBlock PropertyBlock;
        public ProjectChimera.Data.Shared.PlantGrowthStage GrowthStage;
        public float LastUpdate;
        public float UpdateInterval = 0.1f;
    }

    [System.Serializable]
    public class MaterialTemplate
    {
        public string Name;
        public Color BaseColor;
        public float MaturationLevel;
        public float TrichomeDensity;
        public float ResinContent;
        public float Metallic;
        public float Smoothness;
        public float NormalStrength;
        public float EmissionIntensity;
    }

    [System.Serializable]
    public class PlantData
    {
        public string PlantId;
        public ProjectChimera.Data.Shared.PlantGrowthStage GrowthStage;
        public float GrowthProgress;
        public float Health;
        public float WaterLevel;
        public float NutrientLevel;
        public float Temperature;
        public float Humidity;
        public float LightIntensity;
        public LightSpectrumData LightSpectrum;
    }

    [System.Serializable]
    public class ShaderSystemStatus
    {
        public int ActiveMaterials;
        public int MaterialUpdatesPerFrame;
        public float ShaderComputeTime;
        public float PerformanceScore;
        public bool SpectrumVisualizationEnabled;
        public bool EnvironmentalEffectsEnabled;
    }
}
