using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.Rendering.Core
{
    /// <summary>
    /// REFACTORED: Plant Rendering Manager
    /// Focused component for managing plant-specific instanced rendering optimizations
    /// </summary>
    public class PlantRenderingManager : MonoBehaviour, ITickable
    {
        [Header("Plant Rendering Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableInstancingForPlants = true;
        [SerializeField] private int _maxInstancedPlants = 1000;
        [SerializeField] private float _instanceCullingDistance = 100f;
        [SerializeField] private int _maxInstancesPerBatch = 1023;

        // Plant tracking
        private readonly Dictionary<GameObject, PlantRenderingData> _registeredPlants = new Dictionary<GameObject, PlantRenderingData>();
        private readonly Dictionary<string, List<PlantRenderInstance>> _instancedPlantsByType = new Dictionary<string, List<PlantRenderInstance>>();

        // Instancing batches
        private readonly Dictionary<string, PlantInstanceBatch> _plantBatches = new Dictionary<string, PlantInstanceBatch>();

        // Performance tracking
        private PlantRenderingStats _stats = new PlantRenderingStats();
        private Transform _viewerTransform;

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public int RegisteredPlantCount => _registeredPlants.Count;
        public PlantRenderingStats Stats => _stats;

        // Events
        public System.Action<GameObject, PlantRenderingData> OnPlantRegistered;
        public System.Action<GameObject> OnPlantUnregistered;
        public System.Action<int> OnInstanceBatchUpdated;

        // ITickable Implementation
        public int TickPriority => 38; // After construction and planning, near rendering
        public bool IsTickable => IsEnabled && _enableInstancingForPlants;

        private void Start()
        {
            Initialize();
            UpdateOrchestrator.Instance.RegisterTickable(this);
        }

        private void Initialize()
        {
            InitializeViewer();
            ResetStats();

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", "✅ Plant Rendering Manager initialized", this);
        }

        public void Tick(float deltaTime)
        {
            if (IsEnabled && _enableInstancingForPlants)
            {
                UpdateInstancedRendering();
            }
        }

        /// <summary>
        /// Register plant for instanced rendering
        /// </summary>
        public void RegisterPlant(GameObject plantObject, PlantRenderingData renderData)
        {
            if (!IsEnabled || plantObject == null) return;

            if (_registeredPlants.ContainsKey(plantObject))
            {
                UpdatePlantData(plantObject, renderData);
                return;
            }

            _registeredPlants[plantObject] = renderData;
            AddToInstanceBatch(plantObject, renderData);

            _stats.RegisteredPlants++;
            OnPlantRegistered?.Invoke(plantObject, renderData);

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"Plant registered for rendering: {plantObject.name}", this);
        }

        /// <summary>
        /// Unregister plant from instanced rendering
        /// </summary>
        public void UnregisterPlant(GameObject plantObject)
        {
            if (!_registeredPlants.ContainsKey(plantObject)) return;

            var renderData = _registeredPlants[plantObject];
            RemoveFromInstanceBatch(plantObject, renderData);
            _registeredPlants.Remove(plantObject);

            _stats.RegisteredPlants = Mathf.Max(0, _stats.RegisteredPlants - 1);
            OnPlantUnregistered?.Invoke(plantObject);

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"Plant unregistered from rendering: {plantObject.name}", this);
        }

        /// <summary>
        /// Update plant rendering data
        /// </summary>
        public void UpdatePlantData(GameObject plantObject, PlantRenderingData renderData)
        {
            if (!_registeredPlants.ContainsKey(plantObject)) return;

            var oldData = _registeredPlants[plantObject];
            _registeredPlants[plantObject] = renderData;

            // If type changed, move to different batch
            string oldType = GetPlantTypeKey(oldData);
            string newType = GetPlantTypeKey(renderData);

            if (oldType != newType)
            {
                RemoveFromInstanceBatch(plantObject, oldData);
                AddToInstanceBatch(plantObject, renderData);
            }
            else
            {
                UpdateInstanceInBatch(plantObject, renderData);
            }
        }

        /// <summary>
        /// Set instanced rendering enabled/disabled
        /// </summary>
        public void SetInstancedRenderingEnabled(bool enabled)
        {
            _enableInstancingForPlants = enabled;

            if (!enabled)
            {
                ClearAllBatches();
            }

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"Plant instanced rendering: {(enabled ? "enabled" : "disabled")}", this);
        }

        /// <summary>
        /// Get plant rendering statistics
        /// </summary>
        public PlantRenderingStats GetRenderingStats()
        {
            UpdateRenderingStats();
            return _stats;
        }

        /// <summary>
        /// Force update all instance batches
        /// </summary>
        public void ForceUpdateInstanceBatches()
        {
            foreach (var kvp in _plantBatches)
            {
                UpdateInstanceBatch(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Set plant rendering enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                ClearAllBatches();
            }

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"Plant rendering manager: {(enabled ? "enabled" : "disabled")}", this);
        }

        private void InitializeViewer()
        {
            var mainCamera = UnityEngine.Camera.main;
            _viewerTransform = mainCamera != null ? mainCamera.transform : transform;
        }

        private void UpdateInstancedRendering()
        {
            if (_viewerTransform == null) return;

            Vector3 viewerPosition = _viewerTransform.position;

            foreach (var kvp in _plantBatches)
            {
                var batch = kvp.Value;
                UpdateInstanceVisibility(batch, viewerPosition);

                if (batch.NeedsUpdate)
                {
                    UpdateInstanceBatch(kvp.Key, batch);
                    batch.NeedsUpdate = false;
                    OnInstanceBatchUpdated?.Invoke(batch.VisibleInstances.Count);
                }
            }
        }

        private void AddToInstanceBatch(GameObject plantObject, PlantRenderingData renderData)
        {
            string plantTypeKey = GetPlantTypeKey(renderData);

            if (!_instancedPlantsByType.ContainsKey(plantTypeKey))
            {
                _instancedPlantsByType[plantTypeKey] = new List<PlantRenderInstance>();
            }

            if (!_plantBatches.ContainsKey(plantTypeKey))
            {
                _plantBatches[plantTypeKey] = new PlantInstanceBatch
                {
                    PlantType = plantTypeKey,
                    Instances = new List<PlantRenderInstance>(),
                    VisibleInstances = new List<PlantRenderInstance>(),
                    NeedsUpdate = true
                };
            }

            var instance = new PlantRenderInstance
            {
                PlantObject = plantObject,
                RenderData = renderData,
                Matrix = Matrix4x4.TRS(renderData.Position, renderData.Rotation, renderData.Scale),
                IsVisible = true,
                DistanceFromViewer = float.MaxValue
            };

            _instancedPlantsByType[plantTypeKey].Add(instance);
            _plantBatches[plantTypeKey].Instances.Add(instance);
            _plantBatches[plantTypeKey].NeedsUpdate = true;
        }

        private void RemoveFromInstanceBatch(GameObject plantObject, PlantRenderingData renderData)
        {
            string plantTypeKey = GetPlantTypeKey(renderData);

            if (_instancedPlantsByType.TryGetValue(plantTypeKey, out var instances))
            {
                instances.RemoveAll(i => i.PlantObject == plantObject);

                if (_plantBatches.TryGetValue(plantTypeKey, out var batch))
                {
                    batch.Instances.RemoveAll(i => i.PlantObject == plantObject);
                    batch.VisibleInstances.RemoveAll(i => i.PlantObject == plantObject);
                    batch.NeedsUpdate = true;
                }
            }
        }

        private void UpdateInstanceInBatch(GameObject plantObject, PlantRenderingData renderData)
        {
            string plantTypeKey = GetPlantTypeKey(renderData);

            if (_plantBatches.TryGetValue(plantTypeKey, out var batch))
            {
                for (int i = 0; i < batch.Instances.Count; i++)
                {
                    if (batch.Instances[i].PlantObject == plantObject)
                    {
                        var instance = batch.Instances[i];
                        instance.RenderData = renderData;
                        instance.Matrix = Matrix4x4.TRS(renderData.Position, renderData.Rotation, renderData.Scale);
                        batch.Instances[i] = instance;
                        batch.NeedsUpdate = true;
                        break;
                    }
                }
            }
        }

        private void UpdateInstanceVisibility(PlantInstanceBatch batch, Vector3 viewerPosition)
        {
            batch.VisibleInstances.Clear();

            foreach (var instance in batch.Instances)
            {
                float distance = Vector3.Distance(viewerPosition, instance.RenderData.Position);

                if (distance <= _instanceCullingDistance)
                {
                    var visibleInstance = instance;
                    visibleInstance.DistanceFromViewer = distance;
                    visibleInstance.IsVisible = true;
                    batch.VisibleInstances.Add(visibleInstance);
                }
            }
        }

        private void UpdateInstanceBatch(string plantType, PlantInstanceBatch batch)
        {
            // In a real implementation, this would update GPU instancing buffers
            // For now, we'll focus on the data management architecture

            if (_enableLogging && batch.VisibleInstances.Count != batch.PreviousVisibleCount)
            {
                ChimeraLogger.Log("RENDERING",
                    $"Instance batch updated for {plantType}: {batch.VisibleInstances.Count} visible instances", this);
                batch.PreviousVisibleCount = batch.VisibleInstances.Count;
            }
        }

        private void UpdateRenderingStats()
        {
            _stats.InstancedPlants = 0;
            _stats.VisiblePlants = 0;
            _stats.CulledPlants = 0;

            foreach (var batch in _plantBatches.Values)
            {
                _stats.InstancedPlants += batch.Instances.Count;
                _stats.VisiblePlants += batch.VisibleInstances.Count;
                _stats.CulledPlants += (batch.Instances.Count - batch.VisibleInstances.Count);
            }

            _stats.InstancedRenderingEfficiency = _stats.InstancedPlants > 0
                ? (float)_stats.VisiblePlants / _stats.InstancedPlants
                : 0f;
        }

        private void ResetStats()
        {
            _stats = new PlantRenderingStats
            {
                RegisteredPlants = 0,
                InstancedPlants = 0,
                VisiblePlants = 0,
                CulledPlants = 0,
                LODTransitions = 0,
                InstancedRenderingEfficiency = 0f
            };
        }

        private void ClearAllBatches()
        {
            _instancedPlantsByType.Clear();
            _plantBatches.Clear();
            ResetStats();
        }

        private string GetPlantTypeKey(PlantRenderingData renderData)
        {
            return $"{renderData.GrowthStage}_{renderData.LODLevel}";
        }

        private void OnDestroy()
        {
            if (UpdateOrchestrator.Instance != null)
            {
                UpdateOrchestrator.Instance.UnregisterTickable(this);
            }
        }
    }

    /// <summary>
    /// Plant render instance data
    /// </summary>
    [System.Serializable]
    public struct PlantRenderInstance
    {
        public GameObject PlantObject;
        public PlantRenderingData RenderData;
        public Matrix4x4 Matrix;
        public bool IsVisible;
        public float DistanceFromViewer;
    }

    /// <summary>
    /// Plant instance batch for GPU instancing
    /// </summary>
    [System.Serializable]
    public class PlantInstanceBatch
    {
        public string PlantType;
        public List<PlantRenderInstance> Instances;
        public List<PlantRenderInstance> VisibleInstances;
        public bool NeedsUpdate;
        public int PreviousVisibleCount;
    }
}
