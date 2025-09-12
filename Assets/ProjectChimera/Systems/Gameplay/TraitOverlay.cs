using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Data.Genetics;

namespace ProjectChimera.Systems.Gameplay
{
    /// <summary>
    /// Trait overlay system for visualizing genetic traits in the gameplay view
    /// Provides visual representation of plant genetic characteristics
    /// </summary>
    [System.Serializable]
    public class TraitOverlay
    {
        [Header("Overlay Configuration")]
        public TraitType TraitType;
        public bool IsVisible = true;
        public Color OverlayColor = Color.green;
        public float Intensity = 1f;
        
        [Header("Visualization Settings")]
        public TraitVisualizationMode VisualizationMode = TraitVisualizationMode.Heatmap;
        public float UpdateFrequency = 1f; // seconds
        public bool ShowTooltips = true;
        
        [Header("Display Properties")]
        public string DisplayName;
        public string Description;
        public Vector2 ValueRange = new Vector2(0f, 1f);
        
        private Material _overlayMaterial;
        private float _lastUpdateTime;
        private Dictionary<string, float> _plantTraitValues = new Dictionary<string, float>();
        
        /// <summary>
        /// Updates the trait values for all plants
        /// </summary>
        public void UpdateTraitValues(Dictionary<string, float> plantTraitValues)
        {
            if (Time.time - _lastUpdateTime < UpdateFrequency) return;
            
            _plantTraitValues = new Dictionary<string, float>(plantTraitValues);
            _lastUpdateTime = Time.time;
        }
        
        /// <summary>
        /// Gets the trait value for a specific plant
        /// </summary>
        public float GetTraitValue(string plantID)
        {
            return _plantTraitValues.ContainsKey(plantID) ? _plantTraitValues[plantID] : 0f;
        }
        
        /// <summary>
        /// Gets the normalized color for a trait value
        /// </summary>
        public Color GetColorForValue(float value)
        {
            float normalizedValue = Mathf.InverseLerp(ValueRange.x, ValueRange.y, value);
            return Color.Lerp(Color.clear, OverlayColor, normalizedValue * Intensity);
        }
        
        /// <summary>
        /// Enables or disables the overlay
        /// </summary>
        public void SetVisible(bool visible)
        {
            IsVisible = visible;
        }
        
        /// <summary>
        /// Gets display information for UI
        /// </summary>
        public string GetDisplayInfo()
        {
            return $"{DisplayName}: {(IsVisible ? "Visible" : "Hidden")}";
        }
    }
    
    /// <summary>
    /// Trait visualization modes
    /// </summary>
    public enum TraitVisualizationMode
    {
        Heatmap,
        ColorOverlay,
        SizeModification,
        Highlighting,
        Icons
    }
}