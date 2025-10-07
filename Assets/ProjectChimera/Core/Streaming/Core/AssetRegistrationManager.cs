using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using System;

namespace ProjectChimera.Core.Streaming.Core
{
    /// <summary>
    /// REFACTORED: Asset Registration Manager - Focused asset lifecycle and tracking
    /// Handles asset registration, state management, and asset access
    /// Single Responsibility: Asset registration and lifecycle tracking
    /// </summary>
    public class AssetRegistrationManager : MonoBehaviour
    {
        [Header("Asset Registration Settings")]
        [SerializeField] private bool _enableAssetRegistration = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private int _maxRegisteredAssets = 1000;

        // Asset tracking
        private readonly Dictionary<string, StreamedAsset> _streamedAssets = new Dictionary<string, StreamedAsset>();
        private AssetRegistrationStats _stats = new AssetRegistrationStats();

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public AssetRegistrationStats GetStats() => _stats;
        public Dictionary<string, StreamedAsset> GetRegisteredAssets() => new Dictionary<string, StreamedAsset>(_streamedAssets);

        // Events
        public System.Action<string> OnAssetLoaded;
        public System.Action<string> OnAssetUnloaded;
        public System.Action<string> OnAssetRegistered;
        public System.Action<string> OnAssetUnregistered;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _stats = new AssetRegistrationStats();

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", "🗃️ AssetRegistrationManager initialized", this);
        }

        /// <summary>
        /// Register asset for streaming
        /// </summary>
        public bool RegisterAsset(string assetKey, Vector3 position, StreamingPriority priority = StreamingPriority.Medium, string[] tags = null)
        {
            if (!_enableAssetRegistration || string.IsNullOrEmpty(assetKey))
            {
                _stats.RegistrationErrors++;
                return false;
            }

            if (_streamedAssets.Count >= _maxRegisteredAssets)
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("STREAMING", $"Maximum asset limit ({_maxRegisteredAssets}) reached", this);
                _stats.RegistrationErrors++;
                return false;
            }

            if (_streamedAssets.ContainsKey(assetKey))
            {
                // Update existing asset
                var existing = _streamedAssets[assetKey];
                existing.Position = position;
                existing.Priority = priority;
                existing.LastAccessTime = Time.time;
                existing.Tags = tags ?? new string[0];

                if (_enableLogging)
                    ChimeraLogger.Log("STREAMING", $"Updated existing asset: {assetKey}", this);

                return true;
            }

            var streamedAsset = new StreamedAsset
            {
                AssetKey = assetKey,
                Position = position,
                Priority = priority,
                Tags = tags ?? new string[0],
                RegistrationTime = Time.time,
                LastAccessTime = Time.time,
                LoadState = AssetLoadState.Unloaded,
                DistanceFromCenter = float.MaxValue
            };

            _streamedAssets[assetKey] = streamedAsset;
            _stats.RegisteredAssets++;

            OnAssetRegistered?.Invoke(assetKey);

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", $"Registered asset: {assetKey} at {position}", this);

            return true;
        }

        /// <summary>
        /// Unregister asset from streaming
        /// </summary>
        public bool UnregisterAsset(string assetKey)
        {
            if (!_streamedAssets.ContainsKey(assetKey))
                return false;

            var asset = _streamedAssets[assetKey];

            // Update state based on current load state
            switch (asset.LoadState)
            {
                case AssetLoadState.Loaded:
                    // Asset is loaded, trigger unload event
                    OnAssetUnloaded?.Invoke(assetKey);
                    break;
                case AssetLoadState.Loading:
                    // Asset is loading, cancellation would be handled by queue manager
                    break;
            }

            _streamedAssets.Remove(assetKey);
            _stats.RegisteredAssets--;

            OnAssetUnregistered?.Invoke(assetKey);

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", $"Unregistered asset: {assetKey}", this);

            return true;
        }

        /// <summary>
        /// Get asset by key
        /// </summary>
        public StreamedAsset GetAsset(string assetKey)
        {
            return _streamedAssets.TryGetValue(assetKey, out var asset) ? asset : null;
        }

        /// <summary>
        /// Update asset state
        /// </summary>
        public bool UpdateAssetState(string assetKey, AssetLoadState newState)
        {
            if (!_streamedAssets.TryGetValue(assetKey, out var asset))
                return false;

            var previousState = asset.LoadState;
            asset.LoadState = newState;
            asset.LastAccessTime = Time.time;

            // Update statistics
            switch (newState)
            {
                case AssetLoadState.Loaded when previousState != AssetLoadState.Loaded:
                    _stats.LoadedAssets++;
                    OnAssetLoaded?.Invoke(assetKey);
                    break;
                case AssetLoadState.Loading when previousState != AssetLoadState.Loading:
                    _stats.LoadingAssets++;
                    break;
                case AssetLoadState.Unloaded when previousState == AssetLoadState.Loaded:
                    _stats.LoadedAssets--;
                    OnAssetUnloaded?.Invoke(assetKey);
                    break;
                case AssetLoadState.Failed:
                    if (previousState == AssetLoadState.Loading)
                        _stats.LoadingAssets--;
                    break;
            }

            // Update loading count
            if (previousState == AssetLoadState.Loading && newState != AssetLoadState.Loading)
                _stats.LoadingAssets--;

            return true;
        }

        /// <summary>
        /// Update asset distance from streaming center
        /// </summary>
        public bool UpdateAssetDistance(string assetKey, float distance)
        {
            if (!_streamedAssets.TryGetValue(assetKey, out var asset))
                return false;

            asset.DistanceFromCenter = distance;
            return true;
        }

        /// <summary>
        /// Get loaded asset if available
        /// </summary>
        public T GetLoadedAsset<T>(string assetKey) where T : UnityEngine.Object
        {
            if (_streamedAssets.TryGetValue(assetKey, out var asset))
            {
                if (asset.LoadState == AssetLoadState.Loaded && asset.AssetHandle != null)
                {
                    asset.LastAccessTime = Time.time;
                    // In a real implementation, this would return the actual asset from the handle
                    return null; // Placeholder since Addressables not available
                }
            }
            return null;
        }

        /// <summary>
        /// Check if asset is loaded
        /// </summary>
        public bool IsAssetLoaded(string assetKey)
        {
            if (_streamedAssets.TryGetValue(assetKey, out var asset))
            {
                return asset.LoadState == AssetLoadState.Loaded;
            }
            return false;
        }

        /// <summary>
        /// Get assets by state
        /// </summary>
        public List<StreamedAsset> GetAssetsByState(AssetLoadState state)
        {
            var result = new List<StreamedAsset>();
            foreach (var asset in _streamedAssets.Values)
            {
                if (asset.LoadState == state)
                {
                    result.Add(asset);
                }
            }
            return result;
        }

        /// <summary>
        /// Get assets by tag
        /// </summary>
        public List<StreamedAsset> GetAssetsByTag(string tag)
        {
            var result = new List<StreamedAsset>();
            foreach (var asset in _streamedAssets.Values)
            {
                if (asset.Tags != null && System.Array.IndexOf(asset.Tags, tag) >= 0)
                {
                    result.Add(asset);
                }
            }
            return result;
        }

        /// <summary>
        /// Get assets within distance
        /// </summary>
        public List<StreamedAsset> GetAssetsWithinDistance(float maxDistance)
        {
            var result = new List<StreamedAsset>();
            foreach (var asset in _streamedAssets.Values)
            {
                if (asset.DistanceFromCenter <= maxDistance)
                {
                    result.Add(asset);
                }
            }
            return result;
        }

        /// <summary>
        /// Clear all registered assets
        /// </summary>
        public void ClearAllAssets()
        {
            var assetKeys = new List<string>(_streamedAssets.Keys);
            foreach (var assetKey in assetKeys)
            {
                UnregisterAsset(assetKey);
            }

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", "All assets cleared from registration", this);
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", $"AssetRegistrationManager: {(enabled ? "enabled" : "disabled")}", this);
        }

        /// <summary>
        /// Get count of assets by state
        /// </summary>
        public int GetAssetCountByState(AssetLoadState state)
        {
            int count = 0;
            foreach (var asset in _streamedAssets.Values)
            {
                if (asset.LoadState == state)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Update asset handle after load completion
        /// </summary>
        public bool UpdateAssetHandle(string assetKey, object handle)
        {
            if (!_streamedAssets.TryGetValue(assetKey, out var asset))
                return false;

            asset.AssetHandle = handle;
            asset.LastAccessTime = Time.time;
            return true;
        }
    }

    /// <summary>
    /// Asset registration statistics
    /// </summary>
    [System.Serializable]
    public struct AssetRegistrationStats
    {
        public int RegisteredAssets;
        public int LoadedAssets;
        public int LoadingAssets;
        public int RegistrationErrors;
    }
}