using UnityEngine;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Gameplay
{
    /// <summary>
    /// Heatmap renderer for visualizing data across cultivation areas
    /// Provides visual representation of environmental conditions, plant health, etc.
    /// </summary>
    [System.Serializable]
    public class HeatmapRenderer
    {
        [Header("Heatmap Configuration")]
        public string HeatmapName = "Data Heatmap";
        public bool IsActive = true;
        public float Resolution = 1f; // meters per pixel
        public Vector2Int GridSize = new Vector2Int(100, 100);
        
        [Header("Visual Settings")]
        public Color MinColor = Color.blue;
        public Color MaxColor = Color.red;
        public float Opacity = 0.7f;
        public bool ShowGrid = false;
        
        [Header("Data Settings")]
        public Vector2 ValueRange = new Vector2(0f, 1f);
        public float UpdateInterval = 1f; // seconds
        public bool SmoothTransitions = true;
        
        private Texture2D _heatmapTexture;
        private Material _heatmapMaterial;
        private float[,] _dataGrid;
        private float _lastUpdateTime;
        private Dictionary<Vector2Int, float> _dataPoints = new Dictionary<Vector2Int, float>();
        
        /// <summary>
        /// Initializes the heatmap renderer
        /// </summary>
        public void Initialize()
        {
            _dataGrid = new float[GridSize.x, GridSize.y];
            _heatmapTexture = new Texture2D(GridSize.x, GridSize.y, TextureFormat.RGBA32, false);
            _heatmapMaterial = new Material(Shader.Find("Sprites/Default"));
            _heatmapMaterial.mainTexture = _heatmapTexture;
        }
        
        /// <summary>
        /// Updates the heatmap with new data points
        /// </summary>
        public void UpdateData(Dictionary<Vector2Int, float> newDataPoints)
        {
            if (Time.time - _lastUpdateTime < UpdateInterval) return;
            
            _dataPoints = new Dictionary<Vector2Int, float>(newDataPoints);
            RegenerateHeatmap();
            _lastUpdateTime = Time.time;
        }
        
        /// <summary>
        /// Adds a single data point to the heatmap
        /// </summary>
        public void AddDataPoint(Vector2Int position, float value)
        {
            if (position.x >= 0 && position.x < GridSize.x && 
                position.y >= 0 && position.y < GridSize.y)
            {
                _dataPoints[position] = value;
            }
        }
        
        /// <summary>
        /// Gets the data value at a specific grid position
        /// </summary>
        public float GetDataValue(Vector2Int position)
        {
            return _dataPoints.ContainsKey(position) ? _dataPoints[position] : 0f;
        }
        
        /// <summary>
        /// Regenerates the heatmap texture from current data
        /// </summary>
        private void RegenerateHeatmap()
        {
            if (_heatmapTexture == null) Initialize();
            
            // Clear the grid
            for (int x = 0; x < GridSize.x; x++)
            {
                for (int y = 0; y < GridSize.y; y++)
                {
                    _dataGrid[x, y] = 0f;
                }
            }
            
            // Apply data points with interpolation
            foreach (var dataPoint in _dataPoints)
            {
                ApplyDataPoint(dataPoint.Key, dataPoint.Value);
            }
            
            // Update texture
            UpdateTexture();
        }
        
        /// <summary>
        /// Applies a data point to the grid with optional smoothing
        /// </summary>
        private void ApplyDataPoint(Vector2Int position, float value)
        {
            if (SmoothTransitions)
            {
                // Apply with gaussian blur
                int radius = 2;
                for (int x = -radius; x <= radius; x++)
                {
                    for (int y = -radius; y <= radius; y++)
                    {
                        int gridX = position.x + x;
                        int gridY = position.y + y;
                        
                        if (gridX >= 0 && gridX < GridSize.x && 
                            gridY >= 0 && gridY < GridSize.y)
                        {
                            float distance = Vector2.Distance(Vector2.zero, new Vector2(x, y));
                            float influence = Mathf.Exp(-distance * distance / (2f * radius * radius));
                            _dataGrid[gridX, gridY] = Mathf.Max(_dataGrid[gridX, gridY], value * influence);
                        }
                    }
                }
            }
            else
            {
                // Direct application
                if (position.x >= 0 && position.x < GridSize.x && 
                    position.y >= 0 && position.y < GridSize.y)
                {
                    _dataGrid[position.x, position.y] = value;
                }
            }
        }
        
        /// <summary>
        /// Updates the texture from the data grid
        /// </summary>
        private void UpdateTexture()
        {
            for (int x = 0; x < GridSize.x; x++)
            {
                for (int y = 0; y < GridSize.y; y++)
                {
                    float normalizedValue = Mathf.InverseLerp(ValueRange.x, ValueRange.y, _dataGrid[x, y]);
                    Color pixelColor = Color.Lerp(MinColor, MaxColor, normalizedValue);
                    pixelColor.a = Opacity;
                    _heatmapTexture.SetPixel(x, y, pixelColor);
                }
            }
            
            _heatmapTexture.Apply();
        }
        
        /// <summary>
        /// Renders the heatmap using Graphics.DrawTexture
        /// </summary>
        public void Render(Rect screenRect)
        {
            if (_heatmapTexture != null && IsActive)
            {
                Graphics.DrawTexture(screenRect, _heatmapTexture, _heatmapMaterial);
            }
        }
        
        /// <summary>
        /// Gets the heatmap texture for external rendering
        /// </summary>
        public Texture2D GetTexture()
        {
            return _heatmapTexture;
        }
        
        /// <summary>
        /// Clears all data from the heatmap
        /// </summary>
        public void ClearData()
        {
            _dataPoints.Clear();
            RegenerateHeatmap();
        }
        
        /// <summary>
        /// Sets the active state of the heatmap
        /// </summary>
        public void SetActive(bool active)
        {
            IsActive = active;
        }
    }
}