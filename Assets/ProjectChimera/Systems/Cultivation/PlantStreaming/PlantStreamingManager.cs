using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Streaming;
using ProjectChimera.Systems.Cultivation.Pooling;
using ProjectChimera.Core.Updates;
using ProjectChimera.Core;
using ProjectChimera.Systems.Cultivation;

namespace ProjectChimera.Systems.Cultivation.PlantStreaming
{
    /// <summary>
    /// REFACTORED: Plant Streaming Manager
    /// Focused component for managing plant asset loading/unloading based on distance
    /// </summary>
    public class PlantStreamingManager : MonoBehaviour, ITickable
    {
        [Header("Streaming Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _streamingRadius = 75f;
        [SerializeField] private float _unloadRadius = 125f;
        [SerializeField] private int _maxPlantsPerFrame = 20;

        // Streaming queues
        private readonly Queue<PlantStreamingRequest> _loadQueue = new Queue<PlantStreamingRequest>();
        private readonly Queue<string> _unloadQueue = new Queue<string>();

        // Performance tracking
        private int _plantsStreamedThisFrame;

        // References
        private PlantObjectPool _plantPool;

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public float StreamingRadius => _streamingRadius;
        public float UnloadRadius => _unloadRadius;

        // Events
        public System.Action<string, GameObject> OnPlantLoaded;
        public System.Action<string> OnPlantUnloaded;

        // ITickable implementation
        public int TickPriority => 10; // Streaming should happen after LOD updates
        public bool IsTickable => enabled && gameObject.activeInHierarchy && IsEnabled;

        private void Start()
        {
            Initialize();
            RegisterWithUpdateOrchestrator();
        }

        private void Initialize()
        {
            // Initialize plant pool dependency - proper dependency injection should be used
            // TODO: Replace with proper constructor injection when refactoring to non-MonoBehaviour
            _plantPool = ServiceContainerFactory.Instance?.TryResolve<PlantObjectPool>();

            if (_plantPool != null)
            {
                _plantPool.Initialize();
            }

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", "✅ Plant Streaming Manager initialized", this);
        }

        public void Tick(float deltaTime)
        {
            ProcessStreamingQueues();
        }

        /// <summary>
        /// Update streaming decisions based on viewer position
        /// </summary>
        public void UpdateStreaming(Dictionary<string, StreamedPlant> streamedPlants, Vector3 viewerPosition)
        {
            if (!IsEnabled) return;

            foreach (var kvp in streamedPlants)
            {
                var plantId = kvp.Key;
                var streamedPlant = kvp.Value;

                bool shouldBeLoaded = streamedPlant.DistanceFromViewer <= _streamingRadius;
                bool shouldBeUnloaded = streamedPlant.DistanceFromViewer > _unloadRadius;

                if (shouldBeLoaded && !streamedPlant.IsLoaded)
                {
                    QueuePlantLoad(plantId, streamedPlant);
                }
                else if (shouldBeUnloaded && streamedPlant.IsLoaded)
                {
                    QueuePlantUnload(plantId);
                }
            }
        }

        /// <summary>
        /// Force load specific plants
        /// </summary>
        public void ForceLoadPlants(List<string> plantIds)
        {
            foreach (var plantId in plantIds)
            {
                var request = new PlantStreamingRequest
                {
                    PlantId = plantId,
                    RequestType = PlantStreamingRequestType.ForceLoad,
                    Priority = AssetStreamingManager.StreamingPriority.Critical,
                    RequestTime = Time.time
                };

                _loadQueue.Enqueue(request);
            }

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"Force loading {plantIds.Count} plants", this);
        }

        /// <summary>
        /// Unload specific plant
        /// </summary>
        public void UnloadPlant(string plantId)
        {
            _unloadQueue.Enqueue(plantId);
        }

        /// <summary>
        /// Set streaming enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                // Clear queues when disabled
                _loadQueue.Clear();
                _unloadQueue.Clear();
            }

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"Plant streaming manager: {(enabled ? "enabled" : "disabled")}", this);
        }

        /// <summary>
        /// Update streaming radii
        /// </summary>
        public void UpdateStreamingRadii(float streamingRadius, float unloadRadius)
        {
            _streamingRadius = streamingRadius;
            _unloadRadius = unloadRadius;

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"Streaming radii updated - Load: {streamingRadius}, Unload: {unloadRadius}", this);
        }

        private void QueuePlantLoad(string plantId, StreamedPlant streamedPlant)
        {
            var request = new PlantStreamingRequest
            {
                PlantId = plantId,
                RequestType = PlantStreamingRequestType.Load,
                Priority = CalculateLoadPriority(streamedPlant),
                RequestTime = Time.time
            };

            _loadQueue.Enqueue(request);
        }

        private void QueuePlantUnload(string plantId)
        {
            _unloadQueue.Enqueue(plantId);
        }

        private void ProcessStreamingQueues()
        {
            _plantsStreamedThisFrame = 0;

            // Process unload queue first (faster)
            while (_unloadQueue.Count > 0 && _plantsStreamedThisFrame < _maxPlantsPerFrame)
            {
                var plantId = _unloadQueue.Dequeue();
                ProcessPlantUnload(plantId);
                _plantsStreamedThisFrame++;
            }

            // Process load queue
            while (_loadQueue.Count > 0 && _plantsStreamedThisFrame < _maxPlantsPerFrame)
            {
                var request = _loadQueue.Dequeue();
                ProcessPlantLoad(request);
                _plantsStreamedThisFrame++;
            }
        }

        private void ProcessPlantLoad(PlantStreamingRequest request)
        {
            // For now, simulate loading by creating a basic plant object
            // In a real implementation, this would load from AssetStreamingManager

            StartCoroutine(LoadPlantAsync(request));
        }

        private IEnumerator LoadPlantAsync(PlantStreamingRequest request)
        {
            // Simulate async loading delay
            yield return null;

            GameObject plantGameObject = null;

            // Try to get from object pool first
            if (_plantPool != null)
            {
                plantGameObject = _plantPool.GetPlant(request.PlantId);
            }

            // If pool doesn't have it, create a basic placeholder
            if (plantGameObject == null)
            {
                plantGameObject = new GameObject($"Plant_{request.PlantId}");

                // Add basic visual representation
                var renderer = plantGameObject.AddComponent<MeshRenderer>();
                var meshFilter = plantGameObject.AddComponent<MeshFilter>();

                // Use primitive mesh for now
                meshFilter.mesh = CreateBasicPlantMesh();
                renderer.material = CreateBasicPlantMaterial();
            }

            OnPlantLoaded?.Invoke(request.PlantId, plantGameObject);

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"Plant loaded: {request.PlantId}", this);
        }

        private void ProcessPlantUnload(string plantId)
        {
            // Return to pool if using object pool
            if (_plantPool != null)
            {
                _plantPool.ReturnPlant(plantId);
            }

            OnPlantUnloaded?.Invoke(plantId);

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"Plant unloaded: {plantId}", this);
        }

        private AssetStreamingManager.StreamingPriority CalculateLoadPriority(StreamedPlant streamedPlant)
        {
            if (streamedPlant.DistanceFromViewer < _streamingRadius * 0.3f)
                return AssetStreamingManager.StreamingPriority.High;
            else if (streamedPlant.DistanceFromViewer < _streamingRadius * 0.7f)
                return AssetStreamingManager.StreamingPriority.Medium;
            else
                return AssetStreamingManager.StreamingPriority.Low;
        }

        private Mesh CreateBasicPlantMesh()
        {
            // Create a simple plant-like mesh (placeholder)
            var mesh = new Mesh();

            Vector3[] vertices = {
                // Simple cross shape for plant representation
                new Vector3(-0.5f, 0, 0), new Vector3(0.5f, 0, 0), new Vector3(0, 1f, 0),
                new Vector3(0, 0, -0.5f), new Vector3(0, 0, 0.5f), new Vector3(0, 1f, 0)
            };

            int[] triangles = { 0, 2, 1, 3, 5, 4 };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            return mesh;
        }

        private Material CreateBasicPlantMaterial()
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.2f, 0.8f, 0.2f, 1f); // Green color
            return material;
        }

        /// <summary>
        /// Get streaming manager performance stats
        /// </summary>
        public PlantStreamingManagerStats GetPerformanceStats()
        {
            return new PlantStreamingManagerStats
            {
                IsEnabled = IsEnabled,
                StreamingRadius = _streamingRadius,
                UnloadRadius = _unloadRadius,
                LoadQueueSize = _loadQueue.Count,
                UnloadQueueSize = _unloadQueue.Count,
                PlantsStreamedThisFrame = _plantsStreamedThisFrame
            };
        }

        private void RegisterWithUpdateOrchestrator()
        {
            var orchestrator = UpdateOrchestrator.Instance;
            orchestrator?.RegisterTickable(this);

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", "Plant Streaming Manager registered with UpdateOrchestrator", this);
        }

        private void OnDestroy()
        {
            var orchestrator = UpdateOrchestrator.Instance;
            orchestrator?.UnregisterTickable(this);
        }

        public void OnRegistered() { }
        public void OnUnregistered() { }
    }

    /// <summary>
    /// Plant streaming request data structure
    /// </summary>
    [System.Serializable]
    public struct PlantStreamingRequest
    {
        public string PlantId;
        public PlantStreamingRequestType RequestType;
        public AssetStreamingManager.StreamingPriority Priority;
        public float RequestTime;
    }

    /// <summary>
    /// Plant streaming request types
    /// </summary>
    public enum PlantStreamingRequestType
    {
        Load,
        Unload,
        ForceLoad,
        ForceUnload
    }

    /// <summary>
    /// Plant streaming manager performance statistics
    /// </summary>
    [System.Serializable]
    public struct PlantStreamingManagerStats
    {
        public bool IsEnabled;
        public float StreamingRadius;
        public float UnloadRadius;
        public int LoadQueueSize;
        public int UnloadQueueSize;
        public int PlantsStreamedThisFrame;
    }
}