using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using ProjectChimera.Core.Memory;

namespace ProjectChimera.Core.Streaming.LOD
{
    /// <summary>
    /// REFACTORED: Core LOD Management System
    /// Central coordination for Level-of-Detail management with focused responsibility
    /// </summary>
    public class LODCore : MonoBehaviour, ITickable
    {
        [Header("Core LOD Settings")]
        [SerializeField] private bool _enableLOD = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _lodUpdateInterval = 0.2f;
        [SerializeField] private int _maxLODObjects = 1000;

        // LOD subsystems
        private LODDistanceCalculator _distanceCalculator;
        private LODRenderer _lodRenderer;
        private LODAdaptivePolicy _adaptiveSystem;
        private LODStatistics _statistics;

        // LOD tracking
        private readonly Dictionary<int, LODObject> _lodObjects = new Dictionary<int, LODObject>();
        private readonly MemoryOptimizedList<int> _visibleObjects = new MemoryOptimizedList<int>();
        private readonly MemoryOptimizedQueue<LODUpdateRequest> _updateQueue = new MemoryOptimizedQueue<LODUpdateRequest>();

        // Core state
        private Transform _lodCenter;
        private float _lastLODUpdate;
        private int _nextObjectId = 1;

        // Singleton pattern
        private static LODCore _instance;
        public static LODCore Instance
        {
            get
            {
                if (_instance == null)
                {
                    var serviceContainer = ServiceContainerFactory.Instance;
                    _instance = serviceContainer?.TryResolve<LODCore>();

                    if (_instance == null)
                    {
                        var go = new GameObject("LODCore");
                        _instance = go.AddComponent<LODCore>();
                        DontDestroyOnLoad(go);
                        serviceContainer?.RegisterSingleton<LODCore>(_instance);
                    }
                }
                return _instance;
            }
        }

        // Properties
        public bool IsInitialized { get; private set; }
        public int RegisteredObjectCount => _lodObjects.Count;
        public Transform LODCenter => _lodCenter;
        public LODDistanceCalculator DistanceCalculator => _distanceCalculator;
        public LODRenderer Renderer => _lodRenderer;
        public LODAdaptivePolicy AdaptiveSystem => _adaptiveSystem;
        public LODStatistics Statistics => _statistics;

        // ITickable implementation
        public int TickPriority => 100;
        public bool IsTickable => enabled && gameObject.activeInHierarchy && _enableLOD;

        // Events
        public System.Action<int, int> OnLODChanged;
        public System.Action<int, bool> OnVisibilityChanged;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Initialize()
        {
            if (IsInitialized) return;

            InitializeLODCenter();
            InitializeSubsystems();

            _lastLODUpdate = Time.time;
            IsInitialized = true;

            if (_enableLogging)
                ChimeraLogger.Log("LOD", "✅ LODCore initialized", this);
        }

        public void Tick(float deltaTime)
        {
            if (!IsInitialized || !_enableLOD) return;

            if (Time.time - _lastLODUpdate >= _lodUpdateInterval)
            {
                UpdateLODSystem();
                _lastLODUpdate = Time.time;
            }

            ProcessUpdateQueue();
        }

        /// <summary>
        /// Register object for LOD management
        /// </summary>
        public int RegisterLODObject(GameObject gameObject, LODObjectType objectType = LODObjectType.Standard, float customBias = 1f)
        {
            if (gameObject == null) return -1;

            if (_lodObjects.Count >= _maxLODObjects)
            {
                if (_enableLogging)
                    ChimeraLogger.Log("LOD", "⚠️ Maximum LOD objects reached, cannot register new object", this);
                return -1;
            }

            int objectId = _nextObjectId++;

            var lodObject = new LODObject
            {
                ObjectId = objectId,
                GameObject = gameObject,
                Transform = gameObject.transform,
                ObjectType = objectType,
                CustomBias = customBias,
                CurrentLODLevel = -1,
                LastUpdateTime = Time.time,
                IsVisible = true,
                OriginalComponents = _lodRenderer != null ? _lodRenderer.CacheOriginalComponents(gameObject) : default
            };

            _lodObjects[objectId] = lodObject;
            _visibleObjects.Add(objectId);
            _statistics?.OnObjectRegistered();

            if (_enableLogging)
                ChimeraLogger.Log("LOD", $"Registered LOD object: {gameObject.name} (ID: {objectId})", this);

            return objectId;
        }

        /// <summary>
        /// Unregister object from LOD management
        /// </summary>
        public void UnregisterLODObject(int objectId)
        {
            if (_lodObjects.TryGetValue(objectId, out var lodObject))
            {
                _lodRenderer?.RestoreOriginalComponents(lodObject);
                _lodObjects.Remove(objectId);
                _visibleObjects.Remove(objectId);
                _statistics?.OnObjectUnregistered();

                if (_enableLogging)
                    ChimeraLogger.Log("LOD", $"Unregistered LOD object (ID: {objectId})", this);
            }
        }

        /// <summary>
        /// Force update LOD level for specific object
        /// </summary>
        public void ForceUpdateLOD(int objectId, int lodLevel = -1)
        {
            if (_lodObjects.TryGetValue(objectId, out var lodObject))
            {
                if (lodLevel >= 0)
                {
                    ApplyLODLevel(lodObject, lodLevel);
                }
                else
                {
                    var calculatedLOD = _distanceCalculator?.CalculateObjectLOD(lodObject, _lodCenter) ?? 0;
                    QueueLODUpdate(objectId, calculatedLOD);
                }
            }
        }

        /// <summary>
        /// Set LOD center (usually main camera)
        /// </summary>
        public void SetLODCenter(Transform center)
        {
            _lodCenter = center;
            if (_enableLogging)
                ChimeraLogger.Log("LOD", $"LOD center set to: {center?.name}", this);
        }

        /// <summary>
        /// Enable/disable LOD system
        /// </summary>
        public void SetLODEnabled(bool enabled)
        {
            _enableLOD = enabled;
            if (_enableLogging)
                ChimeraLogger.Log("LOD", $"LOD system: {(enabled ? "enabled" : "disabled")}", this);
        }

        /// <summary>
        /// Get LOD object by ID
        /// </summary>
        public LODObject GetLODObject(int objectId)
        {
            _lodObjects.TryGetValue(objectId, out var lodObject);
            return lodObject;
        }

        /// <summary>
        /// Get all LOD objects
        /// </summary>
        public IEnumerable<LODObject> GetAllLODObjects()
        {
            return _lodObjects.Values;
        }

        private void InitializeLODCenter()
        {
            if (_lodCenter == null)
            {
                var mainCamera = UnityEngine.Camera.main;
                _lodCenter = mainCamera != null ? mainCamera.transform : transform;
            }
        }

        private void InitializeSubsystems()
        {
            // Initialize distance calculator
            var calculatorGO = new GameObject("LODDistanceCalculator");
            calculatorGO.transform.SetParent(transform);
            _distanceCalculator = calculatorGO.AddComponent<LODDistanceCalculator>();

            // Initialize renderer
            var rendererGO = new GameObject("LODRenderer");
            rendererGO.transform.SetParent(transform);
            _lodRenderer = rendererGO.AddComponent<LODRenderer>();

            // Initialize adaptive system
            var adaptiveGO = new GameObject("LODAdaptiveSystem");
            adaptiveGO.transform.SetParent(transform);
            _adaptiveSystem = adaptiveGO.AddComponent<LODAdaptivePolicy>();

            // Initialize statistics
            var statsGO = new GameObject("LODStatistics");
            statsGO.transform.SetParent(transform);
            _statistics = statsGO.AddComponent<LODStatistics>();
        }

        private void UpdateLODSystem()
        {
            if (_lodCenter == null) return;

            _statistics?.OnUpdateCycle();

            foreach (var kvp in _lodObjects)
            {
                var lodObject = kvp.Value;
                if (lodObject.GameObject == null) continue;

                var newLODLevel = _distanceCalculator.CalculateObjectLOD(lodObject, _lodCenter);
                var adaptiveLODLevel = _adaptiveSystem.ApplyAdaptiveAdjustment(newLODLevel);

                if (lodObject.CurrentLODLevel != adaptiveLODLevel)
                {
                    QueueLODUpdate(kvp.Key, adaptiveLODLevel);
                }
            }
        }

        private void ProcessUpdateQueue()
        {
            var processedThisFrame = 0;
            const int maxUpdatesPerFrame = 10;

            while (_updateQueue.Count > 0 && processedThisFrame < maxUpdatesPerFrame)
            {
                var updateRequest = _updateQueue.Dequeue();
                if (_lodObjects.TryGetValue(updateRequest.ObjectId, out var lodObject))
                {
                    ApplyLODLevel(lodObject, updateRequest.NewLODLevel);
                    processedThisFrame++;
                }
            }
        }

        private void QueueLODUpdate(int objectId, int newLODLevel)
        {
            _updateQueue.Enqueue(new LODUpdateRequest
            {
                ObjectId = objectId,
                NewLODLevel = newLODLevel,
                RequestTime = Time.time
            });
        }

        private void ApplyLODLevel(LODObject lodObject, int lodLevel)
        {
            if (lodObject.CurrentLODLevel == lodLevel) return;

            var previousLOD = lodObject.CurrentLODLevel;
            lodObject.CurrentLODLevel = lodLevel;
            lodObject.LastUpdateTime = Time.time;

            _lodRenderer?.ApplyLODLevel(lodObject, lodLevel);
            _statistics?.OnLODChanged(previousLOD, lodLevel);

            OnLODChanged?.Invoke(lodObject.ObjectId, lodLevel);

            if (_enableLogging && previousLOD != -1)
                ChimeraLogger.Log("LOD", $"LOD changed for {lodObject.GameObject.name}: {previousLOD} → {lodLevel}", this);
        }

        private void OnDestroy()
        {
            foreach (var lodObject in _lodObjects.Values)
            {
                _lodRenderer?.RestoreOriginalComponents(lodObject);
            }

            _lodObjects.Clear();
            _visibleObjects?.Dispose();
            _updateQueue?.Dispose();

            if (_instance == this)
                _instance = null;
        }
    }
}
