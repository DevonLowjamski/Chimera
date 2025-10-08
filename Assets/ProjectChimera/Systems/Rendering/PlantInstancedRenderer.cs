using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using ProjectChimera.Core.Memory;
using Unity.Collections;
#if UNITY_MATHEMATICS
using Unity.Mathematics;
#endif
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;
using ProjectChimera.Core.Interfaces;
using ProjectChimera.Systems.Camera;

namespace ProjectChimera.Systems.Rendering
{
    /// <summary>
    /// RENDERING: GPU instanced renderer for plant objects
    /// Efficiently renders thousands of plants using GPU instancing
    /// Week 13: Advanced Rendering Pipeline Implementation
    /// </summary>
    public class PlantInstancedRenderer : MonoBehaviour
    {
        [Header("Instancing Settings")]
        [SerializeField] private int _maxInstances = 1000;
        [SerializeField] private float _cullingDistance = 100f;
        [SerializeField] private bool _enableFrustumCulling = true;
        [SerializeField] private bool _enableDistanceCulling = true;
        [SerializeField] private bool _enableLODRendering = true;

        [Header("Plant Rendering")]
        [SerializeField] private Mesh _plantMeshLOD0;
        [SerializeField] private Mesh _plantMeshLOD1;
        [SerializeField] private Mesh _plantMeshLOD2;
        [SerializeField] private Material _plantMaterial;
        [SerializeField] private ShadowCastingMode _shadowCastingMode = ShadowCastingMode.On;
        [SerializeField] private bool _receiveShadows = true;

        [Header("LOD Settings")]
        [SerializeField] private float _lod1Distance = 30f;
        [SerializeField] private float _lod2Distance = 60f;
        [SerializeField] private float _cullDistance = 100f;

        // Instance data arrays
        private NativeArray<Matrix4x4> _instanceMatrices;
        private NativeArray<Vector4> _instanceData;
        private NativeArray<Vector4> _instanceColors;
        private NativeArray<float> _instanceLODs;

        // Managed collections for tracking
        private readonly Dictionary<int, PlantInstanceData> _registeredPlants = new Dictionary<int, PlantInstanceData>();
        private readonly List<int> _visibleInstances = new List<int>();
        private readonly List<Matrix4x4> _renderMatrices = new List<Matrix4x4>();
        private readonly List<Vector4> _renderData = new List<Vector4>();
        private readonly List<Vector4> _renderColors = new List<Vector4>();

        // Performance tracking
        private UnityEngine.Camera _mainCamera;
        private Plane[] _cameraFrustumPlanes = new Plane[6];
        private PlantRenderingStats _stats = new PlantRenderingStats();

        // Rendering batches
        private readonly List<PlantRenderBatch> _renderBatches = new List<PlantRenderBatch>();
        private readonly Dictionary<int, List<Matrix4x4>> _lodBatches = new Dictionary<int, List<Matrix4x4>>();

        // Material property blocks
        private MaterialPropertyBlock _propertyBlock;
        private int _plantDataProperty;
        private int _plantColorsProperty;

        // Instance management
        private int _nextInstanceID = 0;
        private readonly Queue<int> _freeInstanceIDs = new Queue<int>();

        public int MaxInstances => _maxInstances;
        public int RegisteredCount => _registeredPlants.Count;
        public int VisibleCount => _visibleInstances.Count;
        public PlantRenderingStats Stats => _stats;

        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Initialize the plant instanced renderer
        /// </summary>
        public void Initialize(int maxInstances, float cullingDistance)
        {
            if (IsInitialized) return;

            _maxInstances = maxInstances;
            _cullingDistance = cullingDistance;

            InitializeNativeArrays();
            InitializeRenderingComponents();
            SetupMaterialProperties();

            _mainCamera = UnityEngine.Camera.main;
            if (_mainCamera == null)
            {
                _mainCamera = UnityEngine.Camera.main ?? ServiceContainerFactory.Instance?.TryResolve<ICameraService>()?.MainCamera;
            }

            IsInitialized = true;
            ChimeraLogger.Log("RENDER", "PlantInstancedRenderer initialized", this);
        }

        /// <summary>
        /// Register a plant for instanced rendering
        /// </summary>
        public int RegisterPlant(GameObject plantObject, PlantRenderingData renderData)
        {
            if (!IsInitialized || _registeredPlants.Count >= _maxInstances)
            {
                return -1;
            }

            int instanceID = GetNextInstanceID();

            var instanceData = new PlantInstanceData
            {
                InstanceID = instanceID,
                GameObject = plantObject,
                RenderData = renderData,
                LastUpdateTime = Time.time,
                IsVisible = true
            };

            _registeredPlants[instanceID] = instanceData;

            // Update native arrays
            if (instanceID < _instanceMatrices.Length)
            {
                _instanceMatrices[instanceID] = renderData.TransformMatrix;
                _instanceData[instanceID] = renderData.PlantParameters;
                _instanceColors[instanceID] = renderData.PlantColor;
                _instanceLODs[instanceID] = renderData.LODLevel;
            }

            _stats.RegisteredPlants++;
            return instanceID;
        }

        /// <summary>
        /// Unregister a plant from instanced rendering
        /// </summary>
        public void UnregisterPlant(GameObject plantObject)
        {
            int instanceIDToRemove = -1;

            foreach (var kvp in _registeredPlants)
            {
                if (kvp.Value.GameObject == plantObject)
                {
                    instanceIDToRemove = kvp.Key;
                    break;
                }
            }

            if (instanceIDToRemove >= 0)
            {
                UnregisterPlant(instanceIDToRemove);
            }
        }

        /// <summary>
        /// Unregister a plant by instance ID
        /// </summary>
        public void UnregisterPlant(int instanceID)
        {
            if (_registeredPlants.ContainsKey(instanceID))
            {
                _registeredPlants.Remove(instanceID);
                _freeInstanceIDs.Enqueue(instanceID);
                _stats.RegisteredPlants--;
            }
        }

        /// <summary>
        /// Update plant rendering data
        /// </summary>
        public void UpdatePlantData(GameObject plantObject, PlantRenderingData renderData)
        {
            foreach (var kvp in _registeredPlants)
            {
                if (kvp.Value.GameObject == plantObject)
                {
                    var instanceData = kvp.Value;
                    instanceData.RenderData = renderData;
                    instanceData.LastUpdateTime = Time.time;
                    _registeredPlants[kvp.Key] = instanceData;

                    // Update native arrays
                    int instanceID = kvp.Key;
                    if (instanceID < _instanceMatrices.Length)
                    {
                        _instanceMatrices[instanceID] = renderData.TransformMatrix;
                        _instanceData[instanceID] = renderData.PlantParameters;
                        _instanceColors[instanceID] = renderData.PlantColor;
                        _instanceLODs[instanceID] = renderData.LODLevel;
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Set maximum instances for performance optimization
        /// </summary>
        public void SetMaxInstances(int maxInstances)
        {
            if (maxInstances != _maxInstances && maxInstances > 0)
            {
                _maxInstances = maxInstances;

                if (IsInitialized)
                {
                    ReallocateNativeArrays();
                }
            }
        }

        /// <summary>
        /// Set culling distance for performance optimization
        /// </summary>
        public void SetCullingDistance(float distance)
        {
            _cullingDistance = distance;
            _cullDistance = distance;
        }

        /// <summary>
        /// Update renderer - called every frame
        /// </summary>
        public void UpdateRenderer()
        {
            if (!IsInitialized) return;

            UpdateCameraFrustum();
            CullInstances();
            PrepareBatches();
            RenderBatches();
            UpdateStats();
        }

        #region Private Methods

        /// <summary>
        /// Initialize native arrays for instance data
        /// </summary>
        private void InitializeNativeArrays()
        {
            _instanceMatrices = new NativeArray<Matrix4x4>(_maxInstances, Allocator.Persistent);
            _instanceData = new NativeArray<Vector4>(_maxInstances, Allocator.Persistent);
            _instanceColors = new NativeArray<Vector4>(_maxInstances, Allocator.Persistent);
            _instanceLODs = new NativeArray<float>(_maxInstances, Allocator.Persistent);
        }

        /// <summary>
        /// Reallocate native arrays when max instances changes
        /// </summary>
        private void ReallocateNativeArrays()
        {
            // Dispose existing arrays
            if (_instanceMatrices.IsCreated) _instanceMatrices.Dispose();
            if (_instanceData.IsCreated) _instanceData.Dispose();
            if (_instanceColors.IsCreated) _instanceColors.Dispose();
            if (_instanceLODs.IsCreated) _instanceLODs.Dispose();

            // Create new arrays
            InitializeNativeArrays();
        }

        /// <summary>
        /// Initialize rendering components
        /// </summary>
        private void InitializeRenderingComponents()
        {
            _propertyBlock = new MaterialPropertyBlock();

            // Initialize LOD batches
            _lodBatches[0] = new List<Matrix4x4>();
            _lodBatches[1] = new List<Matrix4x4>();
            _lodBatches[2] = new List<Matrix4x4>();
        }

        /// <summary>
        /// Setup material properties for instanced rendering
        /// </summary>
        private void SetupMaterialProperties()
        {
            _plantDataProperty = Shader.PropertyToID("_PlantData");
            _plantColorsProperty = Shader.PropertyToID("_PlantColors");
        }

        /// <summary>
        /// Get next available instance ID
        /// </summary>
        private int GetNextInstanceID()
        {
            if (_freeInstanceIDs.Count > 0)
            {
                return _freeInstanceIDs.Dequeue();
            }
            return _nextInstanceID++;
        }

        /// <summary>
        /// Update camera frustum planes for culling
        /// </summary>
        private void UpdateCameraFrustum()
        {
            if (_mainCamera == null) return;

            GeometryUtility.CalculateFrustumPlanes(_mainCamera, _cameraFrustumPlanes);
        }

        /// <summary>
        /// Cull instances based on distance and frustum
        /// </summary>
        private void CullInstances()
        {
            _visibleInstances.Clear();
            Vector3 cameraPos = _mainCamera != null ? _mainCamera.transform.position : Vector3.zero;

            foreach (var kvp in _registeredPlants)
            {
                var instanceData = kvp.Value;
                Vector3 plantPos = instanceData.RenderData.TransformMatrix.GetColumn(3);

                // Distance culling
                if (_enableDistanceCulling)
                {
                    float distance = Vector3.Distance(cameraPos, plantPos);
                    if (distance > _cullingDistance)
                    {
                        continue;
                    }
                }

                // Frustum culling
                if (_enableFrustumCulling && _mainCamera != null)
                {
                    var bounds = new Bounds(plantPos, Vector3.one * 2f); // Approximate plant bounds
                    if (!GeometryUtility.TestPlanesAABB(_cameraFrustumPlanes, bounds))
                    {
                        continue;
                    }
                }

                _visibleInstances.Add(kvp.Key);
            }

            _stats.VisibleInstances = _visibleInstances.Count;
            _stats.CulledInstances = _registeredPlants.Count - _visibleInstances.Count;
        }

        /// <summary>
        /// Prepare rendering batches based on LOD levels
        /// </summary>
        private void PrepareBatches()
        {
            // Clear LOD batches
            foreach (var lodBatch in _lodBatches.Values)
            {
                lodBatch.Clear();
            }

            Vector3 cameraPos = _mainCamera != null ? _mainCamera.transform.position : Vector3.zero;

            foreach (int instanceID in _visibleInstances)
            {
                if (_registeredPlants.TryGetValue(instanceID, out var instanceData))
                {
                    Vector3 plantPos = instanceData.RenderData.TransformMatrix.GetColumn(3);
                    float distance = Vector3.Distance(cameraPos, plantPos);

                    // Determine LOD level based on distance
                    int lodLevel = 0;
                    if (_enableLODRendering)
                    {
                        if (distance > _lod2Distance)
                            lodLevel = 2;
                        else if (distance > _lod1Distance)
                            lodLevel = 1;
                    }

                    if (_lodBatches.ContainsKey(lodLevel))
                    {
                        _lodBatches[lodLevel].Add(instanceData.RenderData.TransformMatrix);
                    }
                }
            }
        }

        /// <summary>
        /// Render all batches using GPU instancing
        /// </summary>
        private void RenderBatches()
        {
            if (_plantMaterial == null) return;

            int totalDrawCalls = 0;

            // Render each LOD level
            for (int lodLevel = 0; lodLevel < 3; lodLevel++)
            {
                if (!_lodBatches.ContainsKey(lodLevel) || _lodBatches[lodLevel].Count == 0)
                    continue;

                Mesh lodMesh = GetLODMesh(lodLevel);
                if (lodMesh == null) continue;

                var matrices = _lodBatches[lodLevel];

                // Render in batches of 1023 (Unity's instancing limit)
                const int maxBatchSize = 1023;
                for (int i = 0; i < matrices.Count; i += maxBatchSize)
                {
                    int batchSize = Mathf.Min(maxBatchSize, matrices.Count - i);
                    var batchMatrices = matrices.GetRange(i, batchSize);

                    Graphics.DrawMeshInstanced(
                        lodMesh,
                        0,
                        _plantMaterial,
                        batchMatrices.ToArray(),
                        batchSize,
                        _propertyBlock,
                        _shadowCastingMode,
                        _receiveShadows
                    );

                    totalDrawCalls++;
                }
            }

            _stats.DrawCalls = totalDrawCalls;
        }

        /// <summary>
        /// Get mesh for specified LOD level
        /// </summary>
        private Mesh GetLODMesh(int lodLevel)
        {
            return lodLevel switch
            {
                0 => _plantMeshLOD0,
                1 => _plantMeshLOD1,
                2 => _plantMeshLOD2,
                _ => _plantMeshLOD0
            };
        }

        /// <summary>
        /// Update performance statistics
        /// </summary>
        private void UpdateStats()
        {
            _stats.UpdateCalls++;
            _stats.LastUpdateTime = Time.time;
        }

        #endregion

        private void OnDestroy()
        {
            if (_instanceMatrices.IsCreated) _instanceMatrices.Dispose();
            if (_instanceData.IsCreated) _instanceData.Dispose();
            if (_instanceColors.IsCreated) _instanceColors.Dispose();
            if (_instanceLODs.IsCreated) _instanceLODs.Dispose();
        }
    }

}
