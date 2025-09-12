using System;
using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Construction.Grid
{
    /// <summary>
    /// Manages grid configuration settings and parameters
    /// Handles loading, saving, and validation of grid settings
    /// </summary>
    public class GridSettings
    {
        // Grid configuration
        private GridSnapSettings _gridSettings = new GridSnapSettings
        {
            GridSize = 1.0f,
            SnapToGrid = true,
            ShowGrid = true,
            GridColor = new Color(0.5f, 0.5f, 0.5f, 0.3f)
        };

        // Grid bounds and dimensions
        private Vector3 _gridOrigin = Vector3.zero;
        private Vector3 _gridDimensions = new Vector3(100f, 100f, 50f);
        private float _gridHeight = 0.01f;
        private int _maxHeightLevels = 50;
        private float _heightLevelSpacing = 1f;

        // Placement settings
        private float _placementTolerance = 0.1f;
        private bool _validatePlacement = true;
        private bool _preventOverlap = true;

        // Layer settings
        private LayerMask _snapLayers = -1;

        // Events
        public Action OnSettingsChanged;

        /// <summary>
        /// Initialize settings with default values
        /// </summary>
        public void Initialize()
        {
            LoadSettings();
            ValidateSettings();
        }

        /// <summary>
        /// Load grid settings from PlayerPrefs or configuration files
        /// </summary>
        private void LoadSettings()
        {
            // Load basic grid settings
            _gridSettings.GridSize = PlayerPrefs.GetFloat("GridSize", 1.0f);
            _gridSettings.SnapToGrid = PlayerPrefs.GetInt("SnapToGrid", 1) == 1;
            _gridSettings.ShowGrid = PlayerPrefs.GetInt("ShowGrid", 1) == 1;

            // Load grid color
            float r = PlayerPrefs.GetFloat("GridColorR", 0.5f);
            float g = PlayerPrefs.GetFloat("GridColorG", 0.5f);
            float b = PlayerPrefs.GetFloat("GridColorB", 0.5f);
            float a = PlayerPrefs.GetFloat("GridColorA", 0.3f);
            _gridSettings.GridColor = new Color(r, g, b, a);

            // Load grid bounds
            _gridOrigin.x = PlayerPrefs.GetFloat("GridOriginX", 0f);
            _gridOrigin.y = PlayerPrefs.GetFloat("GridOriginY", 0f);
            _gridOrigin.z = PlayerPrefs.GetFloat("GridOriginZ", 0f);

            _gridDimensions.x = PlayerPrefs.GetFloat("GridDimensionsX", 100f);
            _gridDimensions.y = PlayerPrefs.GetFloat("GridDimensionsY", 100f);
            _gridDimensions.z = PlayerPrefs.GetFloat("GridDimensionsZ", 50f);

            // Load other settings
            _gridHeight = PlayerPrefs.GetFloat("GridHeight", 0.01f);
            _maxHeightLevels = PlayerPrefs.GetInt("MaxHeightLevels", 50);
            _heightLevelSpacing = PlayerPrefs.GetFloat("HeightLevelSpacing", 1f);

            _placementTolerance = PlayerPrefs.GetFloat("PlacementTolerance", 0.1f);
            _validatePlacement = PlayerPrefs.GetInt("ValidatePlacement", 1) == 1;
            _preventOverlap = PlayerPrefs.GetInt("PreventOverlap", 1) == 1;

            ChimeraLogger.LogVerbose("Grid settings loaded from PlayerPrefs");
        }

        /// <summary>
        /// Save current grid settings to PlayerPrefs
        /// </summary>
        public void SaveSettings()
        {
            // Save basic grid settings
            PlayerPrefs.SetFloat("GridSize", _gridSettings.GridSize);
            PlayerPrefs.SetInt("SnapToGrid", _gridSettings.SnapToGrid ? 1 : 0);
            PlayerPrefs.SetInt("ShowGrid", _gridSettings.ShowGrid ? 1 : 0);

            // Save grid color
            PlayerPrefs.SetFloat("GridColorR", _gridSettings.GridColor.r);
            PlayerPrefs.SetFloat("GridColorG", _gridSettings.GridColor.g);
            PlayerPrefs.SetFloat("GridColorB", _gridSettings.GridColor.b);
            PlayerPrefs.SetFloat("GridColorA", _gridSettings.GridColor.a);

            // Save grid bounds
            PlayerPrefs.SetFloat("GridOriginX", _gridOrigin.x);
            PlayerPrefs.SetFloat("GridOriginY", _gridOrigin.y);
            PlayerPrefs.SetFloat("GridOriginZ", _gridOrigin.z);

            PlayerPrefs.SetFloat("GridDimensionsX", _gridDimensions.x);
            PlayerPrefs.SetFloat("GridDimensionsY", _gridDimensions.y);
            PlayerPrefs.SetFloat("GridDimensionsZ", _gridDimensions.z);

            // Save other settings
            PlayerPrefs.SetFloat("GridHeight", _gridHeight);
            PlayerPrefs.SetInt("MaxHeightLevels", _maxHeightLevels);
            PlayerPrefs.SetFloat("HeightLevelSpacing", _heightLevelSpacing);

            PlayerPrefs.SetFloat("PlacementTolerance", _placementTolerance);
            PlayerPrefs.SetInt("ValidatePlacement", _validatePlacement ? 1 : 0);
            PlayerPrefs.SetInt("PreventOverlap", _preventOverlap ? 1 : 0);

            PlayerPrefs.Save();

            ChimeraLogger.LogVerbose("Grid settings saved to PlayerPrefs");
        }

        /// <summary>
        /// Reset all settings to default values
        /// </summary>
        public void ResetToDefaults()
        {
            _gridSettings = new GridSnapSettings
            {
                GridSize = 1.0f,
                SnapToGrid = true,
                ShowGrid = true,
                GridColor = new Color(0.5f, 0.5f, 0.5f, 0.3f)
            };

            _gridOrigin = Vector3.zero;
            _gridDimensions = new Vector3(100f, 100f, 50f);
            _gridHeight = 0.01f;
            _maxHeightLevels = 50;
            _heightLevelSpacing = 1f;

            _placementTolerance = 0.1f;
            _validatePlacement = true;
            _preventOverlap = true;

            _snapLayers = -1;

            OnSettingsChanged?.Invoke();
            ChimeraLogger.LogVerbose("Grid settings reset to defaults");
        }

        /// <summary>
        /// Validate that current settings are within acceptable ranges
        /// </summary>
        private void ValidateSettings()
        {
            // Validate grid size
            _gridSettings.GridSize = Mathf.Clamp(_gridSettings.GridSize, 0.1f, 10f);

            // Validate dimensions
            _gridDimensions.x = Mathf.Clamp(_gridDimensions.x, 10f, 1000f);
            _gridDimensions.y = Mathf.Clamp(_gridDimensions.y, 10f, 1000f);
            _gridDimensions.z = Mathf.Clamp(_gridDimensions.z, 5f, 200f);

            // Validate height settings
            _gridHeight = Mathf.Clamp(_gridHeight, 0f, 1f);
            _maxHeightLevels = Mathf.Clamp(_maxHeightLevels, 1, 200);
            _heightLevelSpacing = Mathf.Clamp(_heightLevelSpacing, 0.1f, 5f);

            // Validate placement settings
            _placementTolerance = Mathf.Clamp(_placementTolerance, 0.01f, 1f);
        }

        /// <summary>
        /// Update grid snap settings
        /// </summary>
        public void UpdateGridSnapSettings(GridSnapSettings newSettings)
        {
            _gridSettings = newSettings;
            ValidateSettings();
            OnSettingsChanged?.Invoke();
            ChimeraLogger.LogVerbose("Grid snap settings updated");
        }

        /// <summary>
        /// Update grid bounds
        /// </summary>
        public void UpdateGridBounds(Vector3 origin, Vector3 dimensions)
        {
            _gridOrigin = origin;
            _gridDimensions = dimensions;
            ValidateSettings();
            OnSettingsChanged?.Invoke();
            ChimeraLogger.LogVerbose("Grid bounds updated");
        }

        /// <summary>
        /// Update height level settings
        /// </summary>
        public void UpdateHeightSettings(float height, int maxLevels, float spacing)
        {
            _gridHeight = height;
            _maxHeightLevels = maxLevels;
            _heightLevelSpacing = spacing;
            ValidateSettings();
            OnSettingsChanged?.Invoke();
            ChimeraLogger.LogVerbose("Height settings updated");
        }

        /// <summary>
        /// Update placement settings
        /// </summary>
        public void UpdatePlacementSettings(float tolerance, bool validate, bool preventOverlap)
        {
            _placementTolerance = tolerance;
            _validatePlacement = validate;
            _preventOverlap = preventOverlap;
            ValidateSettings();
            OnSettingsChanged?.Invoke();
            ChimeraLogger.LogVerbose("Placement settings updated");
        }

        /// <summary>
        /// Get a copy of current grid settings
        /// </summary>
        public GridSnapSettings GetGridSnapSettings()
        {
            return _gridSettings;
        }

        // Public properties
        public GridSnapSettings GridSettings => _gridSettings;
        public Vector3 GridOrigin => _gridOrigin;
        public Vector3 GridDimensions => _gridDimensions;
        public float GridHeight => _gridHeight;
        public int MaxHeightLevels => _maxHeightLevels;
        public float HeightLevelSpacing => _heightLevelSpacing;
        public float PlacementTolerance => _placementTolerance;
        public bool ValidatePlacement => _validatePlacement;
        public bool PreventOverlap => _preventOverlap;
        public LayerMask SnapLayers => _snapLayers;
    }
}
