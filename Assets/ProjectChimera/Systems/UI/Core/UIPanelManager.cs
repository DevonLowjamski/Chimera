using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.UI.Core
{
    /// <summary>
    /// REFACTORED: Focused UI Panel Management
    /// Handles only panel registration, lifecycle, and state management
    /// </summary>
    public class UIPanelManager : MonoBehaviour
    {
        [Header("Panel Management Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private int _maxConcurrentPanels = 10;

        // Panel registry
        private readonly Dictionary<string, OptimizedUIPanel> _activePanels = new Dictionary<string, OptimizedUIPanel>();
        private readonly Dictionary<string, PanelState> _panelStates = new Dictionary<string, PanelState>();

        // Events
        public System.Action<string> OnPanelRegistered;
        public System.Action<string> OnPanelUnregistered;
        public System.Action<string> OnPanelOpened;
        public System.Action<string> OnPanelClosed;

        /// <summary>
        /// Register a UI panel for management
        /// </summary>
        public void RegisterPanel(string panelId, OptimizedUIPanel panel)
        {
            if (string.IsNullOrEmpty(panelId) || panel == null)
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("UI", "Cannot register panel - invalid parameters", this);
                return;
            }

            if (_activePanels.ContainsKey(panelId))
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("UI", $"Panel {panelId} already registered", this);
                return;
            }

            if (_activePanels.Count >= _maxConcurrentPanels)
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("UI", $"Maximum concurrent panels ({_maxConcurrentPanels}) reached", this);
                return;
            }

            _activePanels[panelId] = panel;
            _panelStates[panelId] = new PanelState
            {
                IsVisible = panel.IsVisible,
                LastUpdateTime = Time.time
            };

            OnPanelRegistered?.Invoke(panelId);

            if (_enableLogging)
                ChimeraLogger.Log("UI", $"✅ Registered panel: {panelId}", this);
        }

        /// <summary>
        /// Unregister a UI panel
        /// </summary>
        public void UnregisterPanel(string panelId)
        {
            if (!_activePanels.ContainsKey(panelId))
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("UI", $"Panel {panelId} not found for unregistration", this);
                return;
            }

            _activePanels.Remove(panelId);
            _panelStates.Remove(panelId);

            OnPanelUnregistered?.Invoke(panelId);

            if (_enableLogging)
                ChimeraLogger.Log("UI", $"✅ Unregistered panel: {panelId}", this);
        }

        /// <summary>
        /// Get panel by ID
        /// </summary>
        public OptimizedUIPanel GetPanel(string panelId)
        {
            _activePanels.TryGetValue(panelId, out var panel);
            return panel;
        }

        /// <summary>
        /// Show panel
        /// </summary>
        public void ShowPanel(string panelId)
        {
            var panel = GetPanel(panelId);
            if (panel != null)
            {
                panel.Show();
                _panelStates[panelId] = new PanelState
                {
                    IsVisible = true,
                    LastUpdateTime = Time.time
                };
                OnPanelOpened?.Invoke(panelId);
            }
        }

        /// <summary>
        /// Hide panel
        /// </summary>
        public void HidePanel(string panelId)
        {
            var panel = GetPanel(panelId);
            if (panel != null)
            {
                panel.Hide();
                _panelStates[panelId] = new PanelState
                {
                    IsVisible = false,
                    LastUpdateTime = Time.time
                };
                OnPanelClosed?.Invoke(panelId);
            }
        }

        /// <summary>
        /// Update all managed panels
        /// </summary>
        public void UpdatePanels(float deltaTime)
        {
            foreach (var kvp in _activePanels)
            {
                var panel = kvp.Value;
                if (panel != null && panel.IsVisible)
                {
                    panel.UpdatePanel(deltaTime);
                }
            }
        }

        /// <summary>
        /// Get all visible panels
        /// </summary>
        public List<string> GetVisiblePanels()
        {
            var visiblePanels = new List<string>();
            foreach (var kvp in _panelStates)
            {
                if (kvp.Value.IsVisible)
                {
                    visiblePanels.Add(kvp.Key);
                }
            }
            return visiblePanels;
        }

        /// <summary>
        /// Get managed panel count
        /// </summary>
        public int GetManagedPanelCount()
        {
            return _activePanels.Count;
        }

        /// <summary>
        /// Close all panels
        /// </summary>
        public void CloseAllPanels()
        {
            var panelIds = new List<string>(_activePanels.Keys);
            foreach (var panelId in panelIds)
            {
                HidePanel(panelId);
            }

            if (_enableLogging)
                ChimeraLogger.Log("UI", "✅ Closed all panels", this);
        }
    }

    /// <summary>
    /// Panel state tracking
    /// </summary>
    [System.Serializable]
    public struct PanelState
    {
        public bool IsVisible;
        public float LastUpdateTime;
    }
}