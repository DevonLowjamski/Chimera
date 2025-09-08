using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Registry system for managing contextual menu providers by mode.
    /// Allows dynamic registration and switching between different menu providers.
    /// This completes the IContextualMenuProvider implementation.
    /// </summary>
    public class ContextualMenuProviderRegistry
    {
        // Registry storage
        private readonly Dictionary<string, IModeContextualMenuProvider> _providers = new Dictionary<string, IModeContextualMenuProvider>();
        private readonly Dictionary<string, System.Type> _providerTypes = new Dictionary<string, System.Type>();

        // Current state
        private string _currentMode = "none";
        private IModeContextualMenuProvider _activeProvider = null;

        // Events
        public event System.Action<string, IModeContextualMenuProvider> OnProviderRegistered;
        public event System.Action<string> OnProviderUnregistered;
        public event System.Action<string, string> OnModeChanged; // old mode, new mode
        public event System.Action<IModeContextualMenuProvider> OnProviderActivated;
        public event System.Action<IModeContextualMenuProvider> OnProviderDeactivated;

        // Properties
        public string CurrentMode => _currentMode;
        public IModeContextualMenuProvider ActiveProvider => _activeProvider;
        public int RegisteredProviderCount => _providers.Count;
        public IEnumerable<string> AvailableModes => _providers.Keys;

        public ContextualMenuProviderRegistry()
        {
            RegisterBuiltInProviders();
        }

        /// <summary>
        /// Registers built-in contextual menu providers
        /// </summary>
        private void RegisterBuiltInProviders()
        {
            // Register the mode-specific providers we've created
            RegisterProviderType<ConstructionContextMenu>("construction");
            RegisterProviderType<GeneticsContextMenu>("genetics");

            ChimeraLogger.Log("[ContextualMenuProviderRegistry] Registered built-in providers: construction, genetics");
        }

        /// <summary>
        /// Registers a provider type for lazy instantiation
        /// </summary>
        public void RegisterProviderType<T>(string mode) where T : class, IModeContextualMenuProvider, new()
        {
            if (string.IsNullOrEmpty(mode))
            {
                ChimeraLogger.LogWarning("[ContextualMenuProviderRegistry] Cannot register provider with null/empty mode");
                return;
            }

            _providerTypes[mode] = typeof(T);
            ChimeraLogger.Log("SYSTEM", $"[ContextualMenuProviderRegistry] Registered provider type {typeof(T).Name} for mode: {mode}");
        }

        /// <summary>
        /// Registers a provider instance directly
        /// </summary>
        public void RegisterProvider(string mode, IModeContextualMenuProvider provider)
        {
            if (string.IsNullOrEmpty(mode) || provider == null)
            {
                ChimeraLogger.LogWarning("[ContextualMenuProviderRegistry] Cannot register null provider or empty mode");
                return;
            }

            // Deactivate existing provider for this mode if any
            if (_providers.TryGetValue(mode, out var existingProvider))
            {
                existingProvider.Deactivate();
            }

            _providers[mode] = provider;
            OnProviderRegistered?.Invoke(mode, provider);

            ChimeraLogger.Log($"[ContextualMenuProviderRegistry] Registered provider for mode: {mode}");
        }

        /// <summary>
        /// Unregisters a provider for the specified mode
        /// </summary>
        public void UnregisterProvider(string mode)
        {
            if (string.IsNullOrEmpty(mode))
                return;

            if (_providers.TryGetValue(mode, out var provider))
            {
                // Deactivate if currently active
                if (_activeProvider == provider)
                {
                    SetMode("none");
                }

                provider.Deactivate();
                _providers.Remove(mode);
                _providerTypes.Remove(mode);

                OnProviderUnregistered?.Invoke(mode);
                ChimeraLogger.Log($"[ContextualMenuProviderRegistry] Unregistered provider for mode: {mode}");
            }
        }

        /// <summary>
        /// Sets the current mode and activates the corresponding provider
        /// </summary>
        public bool SetMode(string mode)
        {
            if (_currentMode == mode)
                return true; // Already in this mode

            var oldMode = _currentMode;

            // Deactivate current provider
            if (_activeProvider != null)
            {
                _activeProvider.Deactivate();
                OnProviderDeactivated?.Invoke(_activeProvider);
                _activeProvider = null;
            }

            _currentMode = mode;

            // Activate new provider if not "none"
            if (mode != "none")
            {
                var provider = GetProvider(mode);
                if (provider != null)
                {
                    _activeProvider = provider;
                    _activeProvider.Activate();
                    OnProviderActivated?.Invoke(_activeProvider);

                    ChimeraLogger.Log($"[ContextualMenuProviderRegistry] Switched to mode: {mode}");
                }
                else
                {
                    ChimeraLogger.LogWarning($"[ContextualMenuProviderRegistry] No provider found for mode: {mode}");
                    _currentMode = "none";
                    OnModeChanged?.Invoke(oldMode, _currentMode);
                    return false;
                }
            }

            OnModeChanged?.Invoke(oldMode, _currentMode);
            return true;
        }

        /// <summary>
        /// Gets a provider for the specified mode (lazy instantiation if needed)
        /// </summary>
        public IModeContextualMenuProvider GetProvider(string mode)
        {
            if (string.IsNullOrEmpty(mode) || mode == "none")
                return null;

            // Return existing provider if available
            if (_providers.TryGetValue(mode, out var provider))
                return provider;

            // Try to create from registered type
            if (_providerTypes.TryGetValue(mode, out var providerType))
            {
                try
                {
                    var instance = System.Activator.CreateInstance(providerType) as IModeContextualMenuProvider;
                    if (instance != null)
                    {
                        _providers[mode] = instance;
                        OnProviderRegistered?.Invoke(mode, instance);
                        ChimeraLogger.Log($"[ContextualMenuProviderRegistry] Lazy-created provider for mode: {mode}");
                        return instance;
                    }
                }
                catch (System.Exception ex)
                {
                    ChimeraLogger.LogError($"[ContextualMenuProviderRegistry] Failed to create provider for mode {mode}: {ex.Message}");
                }
            }

            return null;
        }

        /// <summary>
        /// Gets menu items for the current mode
        /// </summary>
        public List<string> GetCurrentMenuItems()
        {
            return _activeProvider?.GetMenuItems() ?? new List<string>();
        }

        /// <summary>
        /// Handles menu selection for the current mode
        /// </summary>
        public bool HandleMenuSelection(string menuItem)
        {
            return _activeProvider?.HandleMenuSelection(menuItem) ?? false;
        }

        /// <summary>
        /// Checks if a mode is available
        /// </summary>
        public bool IsModeAvailable(string mode)
        {
            return _providers.ContainsKey(mode) || _providerTypes.ContainsKey(mode);
        }

        /// <summary>
        /// Gets provider info for debugging
        /// </summary>
        public Dictionary<string, string> GetProviderInfo()
        {
            var info = new Dictionary<string, string>();

            foreach (var kvp in _providers)
            {
                info[kvp.Key] = $"{kvp.Value.GetType().Name} (Active: {kvp.Value.IsActive})";
            }

            foreach (var kvp in _providerTypes)
            {
                if (!info.ContainsKey(kvp.Key))
                {
                    info[kvp.Key] = $"{kvp.Value.Name} (Not instantiated)";
                }
            }

            return info;
        }

        /// <summary>
        /// Clears all providers and resets registry
        /// </summary>
        public void Clear()
        {
            // Deactivate all providers
            foreach (var provider in _providers.Values)
            {
                provider.Deactivate();
            }

            _providers.Clear();
            _providerTypes.Clear();
            _activeProvider = null;
            _currentMode = "none";

            // Re-register built-in providers
            RegisterBuiltInProviders();

            ChimeraLogger.Log("[ContextualMenuProviderRegistry] Registry cleared and reset");
        }
    }
}
