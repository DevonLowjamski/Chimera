using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Services.SpeedTree.Performance
{
	/// <summary>
	/// SpeedTree Performance Service - Main Orchestrator
	/// Coordinates LOD, batching, culling, memory optimization, and performance metrics
	///
	/// REFACTORED: Previously 859-line monolithic file with 5 classes
	/// Now: Modular orchestrator coordinating focused subsystems
	/// </summary>
	public class SpeedTreePerformanceService : MonoBehaviour, ITickable, ISpeedTreePerformanceService
	{
		#region Properties

		public bool IsInitialized { get; private set; }

		#endregion

		#region Private Fields

		[Header("Performance Configuration")]
		[SerializeField] private int _maxVisiblePlants = 500;
		[SerializeField] private float _cullingDistance = 100f;
		[SerializeField] private bool _enableGPUInstancing = true;
		[SerializeField] private bool _enableDynamicBatching = true;
		[SerializeField] private SpeedTreeQualityLevel _defaultQuality = SpeedTreeQualityLevel.Medium;

		[Header("LOD Settings")]
		[SerializeField] private float[] _lodDistances = { 25f, 50f, 100f, 200f };
		[SerializeField] private float[] _lodQualityMultipliers = { 1.0f, 0.8f, 0.6f, 0.3f };

		[Header("Performance Monitoring")]
		[SerializeField] private bool _enablePerformanceMonitoring = true;
		[SerializeField] private float _performanceUpdateInterval = 1.0f;
		[SerializeField] private int _targetFrameRate = 60;
		[SerializeField] private float _memoryWarningThreshold = 512f;

		// Modular subsystems
		private LODManager _lodManager;
		private BatchingManager _batchingManager;
		private CullingManager _cullingManager;
		private MemoryManager _memoryManager;

		// SpeedTree tracking
		private List<GameObject> _trackedSpeedTrees = new List<GameObject>();
		private Dictionary<GameObject, SpeedTreeRendererData> _rendererData = new Dictionary<GameObject, SpeedTreeRendererData>();

		// Camera reference
		private UnityEngine.Camera _mainCamera;

		#endregion

		#region Events

		public event Action<SpeedTreePerformanceMetrics> OnPerformanceMetricsUpdated;
		public event Action<SpeedTreeQualityLevel> OnQualityLevelChanged;
		public event Action<float> OnMemoryUsageChanged;

		#endregion

		#region IService Implementation

		public void Initialize()
		{
			if (IsInitialized) return;

			ChimeraLogger.Log("SPEEDTREE/PERF", "SpeedTreePerformanceService Initialize", this);

			// Find main camera
			_mainCamera = UnityEngine.Camera.main;
			if (_mainCamera == null)
			{
				ChimeraLogger.LogWarning("SPEEDTREE/PERF", "Main camera not found", this);
				return;
			}

			// Initialize subsystems
			InitializeSubsystems();

			IsInitialized = true;
			ChimeraLogger.Log("SPEEDTREE/PERF", "SpeedTreePerformanceService Initialized", this);
		}

		public void Shutdown()
		{
			if (!IsInitialized) return;

			// Clear all tracked SpeedTrees
			ClearAllSpeedTrees();

			// Shutdown subsystems
			_lodManager?.ClearLODData();
			_batchingManager?.ClearBatches();
			_cullingManager?.ClearCulling();
			_memoryManager?.ForceGarbageCollection();

			IsInitialized = false;
			ChimeraLogger.Log("SPEEDTREE/PERF", "SpeedTreePerformanceService Shutdown", this);
		}

		#endregion

		#region ITickable Implementation

		public int TickPriority => ProjectChimera.Core.Updates.TickPriority.ConstructionSystem;
		public bool IsTickable => enabled && IsInitialized;

		public void Tick(float deltaTime)
		{
			if (!IsTickable) return;

			// Update performance monitoring
			if (_enablePerformanceMonitoring)
			{
				UpdatePerformance();
			}

			// Update memory monitoring
			// MemoryManager tracks via its Tick; no explicit monitor call needed here

			// Update LODs for tracked SpeedTrees
			if (_lodManager != null && _mainCamera != null)
			{
				_lodManager.UpdateLODs(_trackedSpeedTrees.ToArray(), _mainCamera.transform.position);
			}

			// Update batching
			_batchingManager?.UpdateBatches();

			// Update culling
			if (_cullingManager != null && _mainCamera != null)
			{
				Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(_mainCamera);
				_cullingManager.UpdateCulling(_trackedSpeedTrees.ToArray(), _mainCamera.transform.position, frustumPlanes);
			}
		}

		public void OnRegistered() { }
		public void OnUnregistered() { }

		#endregion

		#region Public API

		/// <summary>
		/// Update performance metrics and trigger optimizations
		/// </summary>
		public void UpdatePerformance()
		{
			SpeedTreePerformanceMetrics metrics = GetPerformanceMetrics();

			// Trigger performance events
			OnPerformanceMetricsUpdated?.Invoke(metrics);
			OnMemoryUsageChanged?.Invoke(metrics.MemoryUsageMB);

			// Auto quality adjustment
			if (metrics.AverageFrameTime > 33f) // Less than 30 FPS
			{
				SetQualityLevel(SpeedTreeQualityLevel.Low);
			}
			else if (metrics.AverageFrameTime < 16f && metrics.CurrentQuality < SpeedTreeQualityLevel.Ultra) // More than 60 FPS
			{
				SetQualityLevel(metrics.CurrentQuality + 1);
			}
		}

		/// <summary>
		/// Get current performance metrics
		/// </summary>
		public SpeedTreePerformanceMetrics GetPerformanceMetrics()
		{
			var metrics = new SpeedTreePerformanceMetrics(DateTime.Now)
			{
				VisiblePlants = _cullingManager?.GetVisibleCount() ?? 0,
				CurrentQuality = _defaultQuality
			};

			// Get frame time from Unity
			metrics.AverageFrameTime = Time.deltaTime * 1000f;
			metrics.PeakFrameTime = metrics.AverageFrameTime; // Simplified

			// Get memory usage
			var memoryStats = _memoryManager?.GetStats();
			metrics.MemoryUsageMB = memoryStats?.CurrentMemoryUsage ?? 0f;

			// Get rendering stats (simplified)
			metrics.DrawCalls = _trackedSpeedTrees.Count;
			metrics.Triangles = metrics.DrawCalls * 1000; // Rough estimate
			metrics.Batches = _batchingManager?.GetBatchCount() ?? 0;

			return metrics;
		}

		/// <summary>
		/// Set quality level for all SpeedTree rendering
		/// </summary>
		public void SetQualityLevel(SpeedTreeQualityLevel level)
		{
			if (_defaultQuality == level) return;

			_defaultQuality = level;
			OnQualityLevelChanged?.Invoke(level);

			// Apply quality settings to all subsystems
			ApplyQualitySettings(level);

			ChimeraLogger.Log("SPEEDTREE/PERF", "Quality level changed", this);
		}

		/// <summary>
		/// Optimize for maximum distance (reduce quality for better performance)
		/// </summary>
		public void OptimizeForDistance(float maxDistance)
		{
			_cullingManager?.SetCullingDistance(maxDistance);
			_lodManager?.SetLODDistances(new float[] { maxDistance * 0.25f, maxDistance * 0.5f, maxDistance * 0.75f, maxDistance });

			ChimeraLogger.Log("SPEEDTREE/PERF", "Optimized for distance", this);
		}

		/// <summary>
		/// Optimize for specific quality level
		/// </summary>
		public void OptimizeForQuality(SpeedTreeQualityLevel quality)
		{
			SetQualityLevel(quality);
		}

		/// <summary>
		/// Force garbage collection
		/// </summary>
		public void ForceGC()
		{
			_memoryManager?.ForceGarbageCollection();
		}

		/// <summary>
		/// Clear cache and reset performance data
		/// </summary>
		public void ClearCache()
		{
			_batchingManager?.ClearBatches();
			_cullingManager?.ClearCulling();
			_memoryManager?.ForceGarbageCollection();

			ChimeraLogger.Log("SPEEDTREE/PERF", "Cleared cache", this);
		}

		/// <summary>
		/// Add a SpeedTree for performance tracking
		/// </summary>
		public void AddSpeedTree(GameObject speedTree)
		{
			if (speedTree == null || _trackedSpeedTrees.Contains(speedTree)) return;

			_trackedSpeedTrees.Add(speedTree);

			// Track with subsystems
			_lodManager?.AddSpeedTree(speedTree);
			_cullingManager?.AddSpeedTree(speedTree);

			// Track assets for memory management
			TrackSpeedTreeAssets(speedTree);

			ChimeraLogger.Log("SPEEDTREE/PERF", "Added SpeedTree", this);
		}

		/// <summary>
		/// Remove a SpeedTree from performance tracking
		/// </summary>
		public void RemoveSpeedTree(GameObject speedTree)
		{
			if (speedTree == null || !_trackedSpeedTrees.Contains(speedTree)) return;

			_trackedSpeedTrees.Remove(speedTree);

			// Remove from subsystems
			_lodManager?.RemoveSpeedTree(speedTree);
			_cullingManager?.RemoveSpeedTree(speedTree);
			_batchingManager?.RemoveFromBatch(speedTree);

			_rendererData.Remove(speedTree);

			ChimeraLogger.Log("SPEEDTREE/PERF", "Removed SpeedTree", this);
		}

		/// <summary>
		/// Get performance summary string
		/// </summary>
		public string GetPerformanceSummary()
		{
			var metrics = GetPerformanceMetrics();

			return $"SpeedTree Performance:\n" +
				   $"- Visible Plants: {metrics.VisiblePlants}\n" +
				   $"- Average Frame Time: {metrics.AverageFrameTime:F2}ms\n" +
				   $"- Memory Usage: {metrics.MemoryUsageMB:F1}MB\n" +
				   $"- Draw Calls: {metrics.DrawCalls}\n" +
				   $"- Batches: {metrics.Batches}\n" +
				   $"- Quality Level: {_defaultQuality}";
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Initialize all subsystems
		/// </summary>
		private void InitializeSubsystems()
		{
			// Initialize LOD manager
			var lodConfig = new SpeedTreeLODConfig(_lodDistances, _lodQualityMultipliers);
			_lodManager = new LODManager();
			_lodManager.Initialize(lodConfig);

			// Initialize batching manager
			SpeedTreeBatchingMethod batchingMethod = _enableGPUInstancing ?
				SpeedTreeBatchingMethod.GPUInstancing :
				(_enableDynamicBatching ? SpeedTreeBatchingMethod.DynamicBatching : SpeedTreeBatchingMethod.None);

			_batchingManager = new BatchingManager();
			_batchingManager.Initialize(batchingMethod);

			// Initialize culling manager
			_cullingManager = new CullingManager();
			_cullingManager.Initialize(SpeedTreeCullingStrategy.Hybrid, _cullingDistance);

			// Initialize memory manager
			_memoryManager = new MemoryManager();
			_memoryManager.Initialize();
			_memoryManager.SetMonitoringParameters(_performanceUpdateInterval, _memoryWarningThreshold);

			// Wire subsystem events
			WireSubsystemEvents();
		}

		/// <summary>
		/// Wire events between subsystems
		/// </summary>
		private void WireSubsystemEvents()
		{
			_memoryManager.OnMemoryCheck += (memory) => OnMemoryUsageChanged?.Invoke(memory);
			_memoryManager.OnMemoryWarning += (memory) => OnMemoryUsageChanged?.Invoke(memory);
		}

		/// <summary>
		/// Apply quality settings to all subsystems
		/// </summary>
		private void ApplyQualitySettings(SpeedTreeQualityLevel level)
		{
			float qualityMultiplier = GetQualityMultiplier(level);

			// Adjust LOD distances based on quality
			float[] adjustedDistances = new float[_lodDistances.Length];
			for (int i = 0; i < _lodDistances.Length; i++)
			{
				adjustedDistances[i] = _lodDistances[i] * qualityMultiplier;
			}
			_lodManager?.SetLODDistances(adjustedDistances);

			// Adjust culling distance
			_cullingManager?.SetCullingDistance(_cullingDistance * qualityMultiplier);

			// Adjust batching settings
			_batchingManager.EnableGPUInstancing = level >= SpeedTreeQualityLevel.Medium;
			_batchingManager.EnableDynamicBatching = level >= SpeedTreeQualityLevel.Low;
		}

		/// <summary>
		/// Get quality multiplier for a quality level
		/// </summary>
		private float GetQualityMultiplier(SpeedTreeQualityLevel level)
		{
			switch (level)
			{
				case SpeedTreeQualityLevel.Ultra: return 1.0f;
				case SpeedTreeQualityLevel.High: return 0.9f;
				case SpeedTreeQualityLevel.Medium: return 0.7f;
				case SpeedTreeQualityLevel.Low: return 0.5f;
				case SpeedTreeQualityLevel.Minimal: return 0.3f;
				default: return 0.7f;
			}
		}

		/// <summary>
		/// Track assets used by a SpeedTree for memory management
		/// </summary>
		private void TrackSpeedTreeAssets(GameObject speedTree)
		{
			if (_memoryManager == null) return;

			// Basic manager doesn't track materials/meshes; no-op for now
		}

		/// <summary>
		/// Clear all tracked SpeedTrees
		/// </summary>
		private void ClearAllSpeedTrees()
		{
			foreach (GameObject speedTree in _trackedSpeedTrees.ToArray())
			{
				RemoveSpeedTree(speedTree);
			}

			_trackedSpeedTrees.Clear();
			ChimeraLogger.Log("SPEEDTREE/PERF", "Cleared all SpeedTrees", this);
		}

		#endregion

		#region Unity Lifecycle

		private void OnDestroy()
		{
			Shutdown();
		}

		#endregion
	}
}
