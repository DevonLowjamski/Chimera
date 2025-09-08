using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Updates;
using ProjectChimera.UI.Core;
using ProjectChimera.Systems.Facilities;
using ProjectChimera.Data.Facilities;
using ProjectChimera.Core.Logging;


namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Controller for the facility world map panel.
    /// Handles opening/closing the world map and integration with input systems.
    /// </summary>
    public class FacilityWorldMapController : ChimeraMonoBehaviour, ITickable
    {
        [Header("World Map Configuration")]
        [SerializeField] private KeyCode _openWorldMapKey = KeyCode.M;
        [SerializeField] private bool _pauseGameWhenOpen = true;
        [SerializeField] private string _worldMapPanelId = "FacilityWorldMapPanel";

        [Header("UI References")]
        [SerializeField] private FacilityWorldMapPanel _worldMapPanel;

        // Manager references
        private UIManager _uiManager;
        private IFacilityManager _facilityManager; // Placeholder for FacilityManager

        private void Start()
        {
            // Get manager references
            _uiManager = GameManager.Instance?.GetManager<UIManager>();
            _facilityManager = ServiceContainerFactory.Instance?.TryResolve<IFacilityManager>(); // Placeholder - FacilityManager not yet implemented

            if (_uiManager == null)
            {
                ChimeraLogger.LogError("[FacilityWorldMapController] UIManager not found!");
            }

            if (_facilityManager == null)
            {
                ChimeraLogger.LogError("[FacilityWorldMapController] FacilityManager not found!");
            }

            // Register with UpdateOrchestrator
            UpdateOrchestrator.Instance.RegisterTickable(this);

            ChimeraLogger.Log("[FacilityWorldMapController] Initialized facility world map controller");
        }

        #region ITickable Implementation

        public int Priority => TickPriority.UIManager;
        public bool Enabled => enabled;

        public void Tick(float deltaTime)
        {
            // Handle world map toggle input
            if (Input.GetKeyDown(_openWorldMapKey))
            {
                ToggleWorldMap();
            }
        }

        #endregion

        /// <summary>
        /// Toggles the world map panel open/closed
        /// </summary>
        public void ToggleWorldMap()
        {
            if (_worldMapPanel == null)
            {
                ChimeraLogger.LogError("[FacilityWorldMapController] World map panel reference not set!");
                return;
            }

            if (_worldMapPanel.IsVisible)
            {
                CloseWorldMap();
            }
            else
            {
                OpenWorldMap();
            }
        }

        /// <summary>
        /// Opens the world map panel
        /// </summary>
        public void OpenWorldMap()
        {
            if (_worldMapPanel == null || _facilityManager == null)
            {
                ChimeraLogger.LogError("[FacilityWorldMapController] Missing references for world map!");
                return;
            }

            ChimeraLogger.Log("[FacilityWorldMapController] Opening facility world map");

            // Pause game if configured
            if (_pauseGameWhenOpen)
            {
                Time.timeScale = 0f;
            }

			// Show the panel
			_worldMapPanel.Show();

            // Force refresh of facility data
            _worldMapPanel.RefreshFacilityData();
        }

        /// <summary>
        /// Closes the world map panel
        /// </summary>
        public void CloseWorldMap()
        {
            if (_worldMapPanel == null)
            {
                ChimeraLogger.LogError("[FacilityWorldMapController] World map panel reference not set!");
                return;
            }

            ChimeraLogger.Log("[FacilityWorldMapController] Closing facility world map");

            // Resume game if it was paused
            if (_pauseGameWhenOpen)
            {
                Time.timeScale = 1f;
            }

			// Hide the panel
			_worldMapPanel.Hide();
        }

        /// <summary>
        /// Quick switch to facility by tier name (for testing)
        /// </summary>
        public async void QuickSwitchFacility(string tierName)
        {
            if (_facilityManager == null)
            {
                ChimeraLogger.LogError("[FacilityWorldMapController] FacilityManager not available for quick switch");
                return;
            }

            ChimeraLogger.Log($"[FacilityWorldMapController] Quick switching to facility with tier: {tierName}");

            var success = await _facilityManager.QuickSwitchByTierName(tierName);
            if (success)
            {
                ChimeraLogger.Log($"[FacilityWorldMapController] Quick switch to {tierName} successful");

                // Refresh the world map if it's open
                if (_worldMapPanel != null && _worldMapPanel.IsVisible)
                {
                    _worldMapPanel.RefreshFacilityData();
                }
            }
            else
            {
                ChimeraLogger.LogWarning($"[FacilityWorldMapController] Quick switch to {tierName} failed");
            }
        }

        /// <summary>
        /// Forces facility progression evaluation (for testing upgrade availability)
        /// </summary>
        public void CheckForUpgrades()
        {
            if (_facilityManager == null)
            {
                ChimeraLogger.LogError("[FacilityWorldMapController] FacilityManager not available");
                return;
            }

            _facilityManager.CheckForUpgradeAvailability();

            // Refresh the world map if it's open
            if (_worldMapPanel != null && _worldMapPanel.IsVisible)
            {
                _worldMapPanel.RefreshFacilityData();
            }

            ChimeraLogger.Log("[FacilityWorldMapController] Checked for facility upgrades");
        }

        #region Public API for External Systems

        /// <summary>
        /// Checks if the world map is currently open
        /// </summary>
        public bool IsWorldMapOpen => _worldMapPanel != null && _worldMapPanel.IsVisible;

        /// <summary>
        /// Gets the current selected facility (for other UI systems)
        /// </summary>
        public string GetCurrentFacilityId()
        {
            return _facilityManager?.CurrentFacilityId ?? "";
        }

        /// <summary>
        /// Gets facility statistics for other UI displays
        /// </summary>
        public FacilityProgressionStatistics GetFacilityStatistics()
        {
            return _facilityManager?.GetProgressionStatisticsTyped();
        }

        #endregion

        private void OnDestroy()
        {
            if (UpdateOrchestrator.Instance != null)
            {
                UpdateOrchestrator.Instance.UnregisterTickable(this);
            }
        }
    }
}
