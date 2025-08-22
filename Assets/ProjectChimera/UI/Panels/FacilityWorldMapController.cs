using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.UI.Core;
using ProjectChimera.Systems.Facilities;
using ProjectChimera.Data.Facilities;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Controller for the facility world map panel.
    /// Handles opening/closing the world map and integration with input systems.
    /// </summary>
    public class FacilityWorldMapController : ChimeraMonoBehaviour
    {
        [Header("World Map Configuration")]
        [SerializeField] private KeyCode _openWorldMapKey = KeyCode.M;
        [SerializeField] private bool _pauseGameWhenOpen = true;
        [SerializeField] private string _worldMapPanelId = "FacilityWorldMapPanel";

        [Header("UI References")]
        [SerializeField] private FacilityWorldMapPanel _worldMapPanel;

        // Manager references
        private UIManager _uiManager;
        private FacilityManager _facilityManager;

        private void Start()
        {
            // Get manager references
            _uiManager = GameManager.Instance?.GetManager<UIManager>();
            _facilityManager = GameManager.Instance?.GetManager<FacilityManager>();

            if (_uiManager == null)
            {
                Debug.LogError("[FacilityWorldMapController] UIManager not found!");
            }

            if (_facilityManager == null)
            {
                Debug.LogError("[FacilityWorldMapController] FacilityManager not found!");
            }

            Debug.Log("[FacilityWorldMapController] Initialized facility world map controller");
        }

        private void Update()
        {
            // Handle world map toggle input
            if (Input.GetKeyDown(_openWorldMapKey))
            {
                ToggleWorldMap();
            }
        }

        /// <summary>
        /// Toggles the world map panel open/closed
        /// </summary>
        public void ToggleWorldMap()
        {
            if (_worldMapPanel == null)
            {
                Debug.LogError("[FacilityWorldMapController] World map panel reference not set!");
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
                Debug.LogError("[FacilityWorldMapController] Missing references for world map!");
                return;
            }

            Debug.Log("[FacilityWorldMapController] Opening facility world map");

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
                Debug.LogError("[FacilityWorldMapController] World map panel reference not set!");
                return;
            }

            Debug.Log("[FacilityWorldMapController] Closing facility world map");

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
                Debug.LogError("[FacilityWorldMapController] FacilityManager not available for quick switch");
                return;
            }

            Debug.Log($"[FacilityWorldMapController] Quick switching to facility with tier: {tierName}");

            var success = await _facilityManager.QuickSwitchByTierName(tierName);
            if (success)
            {
                Debug.Log($"[FacilityWorldMapController] Quick switch to {tierName} successful");
                
                // Refresh the world map if it's open
                if (_worldMapPanel != null && _worldMapPanel.IsVisible)
                {
                    _worldMapPanel.RefreshFacilityData();
                }
            }
            else
            {
                Debug.LogWarning($"[FacilityWorldMapController] Quick switch to {tierName} failed");
            }
        }

        /// <summary>
        /// Forces facility progression evaluation (for testing upgrade availability)
        /// </summary>
        public void CheckForUpgrades()
        {
            if (_facilityManager == null)
            {
                Debug.LogError("[FacilityWorldMapController] FacilityManager not available");
                return;
            }

            _facilityManager.CheckForUpgradeAvailability();

            // Refresh the world map if it's open
            if (_worldMapPanel != null && _worldMapPanel.IsVisible)
            {
                _worldMapPanel.RefreshFacilityData();
            }

            Debug.Log("[FacilityWorldMapController] Checked for facility upgrades");
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
            return _facilityManager?.GetProgressionStatistics();
        }

        #endregion
    }
}