using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using WeatherType = ProjectChimera.Systems.Rendering.WeatherType;

namespace ProjectChimera.Systems.Rendering.Environmental
{
    /// <summary>
    /// REFACTORED: Focused Wind Rendering System
    /// Handles only wind effects and object interactions
    /// </summary>
    public class WindRenderingSystem : MonoBehaviour
    {
        [Header("Wind Settings")]
        [SerializeField] private bool _enableWind = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private Vector3 _windDirection = Vector3.right;
        [SerializeField] private float _windStrength = 1f;
        [SerializeField] private float _windVariation = 0.5f;
        [SerializeField] private float _windUpdateFrequency = 0.1f;

        // Wind state
        private readonly List<IWindAffected> _windAffectedObjects = new List<IWindAffected>();
        private Vector3 _currentWindVector;
        private float _lastWindUpdate;

        // Properties
        public bool IsEnabled => _enableWind;
        public Vector3 CurrentWindDirection => _windDirection;
        public float CurrentWindStrength => _windStrength;
        public Vector3 CurrentWindVector => _currentWindVector;

        // Events
        public System.Action<Vector3, float> OnWindChanged;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            UpdateWindVector();

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", "✅ Wind rendering system initialized", this);
        }

        /// <summary>
        /// Update wind rendering - called by EnvironmentalRendererCore
        /// </summary>
        public void UpdateWind(float deltaTime)
        {
            if (!_enableWind) return;

            if (Time.time - _lastWindUpdate >= _windUpdateFrequency)
            {
                UpdateWindVariation();
                UpdateWindAffectedObjects();
                _lastWindUpdate = Time.time;
            }
        }

        /// <summary>
        /// Set wind parameters
        /// </summary>
        public void SetWind(Vector3 direction, float strength)
        {
            _windDirection = direction.normalized;
            _windStrength = strength;
            UpdateWindVector();

            OnWindChanged?.Invoke(_windDirection, _windStrength);

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"Wind updated: {_windDirection} strength {_windStrength}", this);
        }

        /// <summary>
        /// Set wind for specific weather type
        /// </summary>
        public void SetWeatherWind(WeatherType weatherType)
        {
            var windData = GetWeatherWindData(weatherType);
            SetWind(windData.direction, windData.strength);
        }

        /// <summary>
        /// Register object for wind effects
        /// </summary>
        public void RegisterWindAffectedObject(IWindAffected windObject)
        {
            if (windObject != null && !_windAffectedObjects.Contains(windObject))
            {
                _windAffectedObjects.Add(windObject);

                if (_enableLogging)
                    ChimeraLogger.Log("RENDERING", $"Registered wind-affected object: {windObject.GetType().Name}", this);
            }
        }

        /// <summary>
        /// Unregister object from wind effects
        /// </summary>
        public void UnregisterWindAffectedObject(IWindAffected windObject)
        {
            if (_windAffectedObjects.Remove(windObject))
            {
                if (_enableLogging)
                    ChimeraLogger.Log("RENDERING", $"Unregistered wind-affected object: {windObject?.GetType().Name}", this);
            }
        }

        /// <summary>
        /// Enable/disable wind rendering
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _enableWind = enabled;

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"Wind rendering: {(enabled ? "enabled" : "disabled")}", this);
        }

        private void UpdateWindVariation()
        {
            // Add variation to wind strength for realism
            var variation = Mathf.Sin(Time.time) * _windVariation;
            var currentStrength = _windStrength + variation;
            _currentWindVector = _windDirection * currentStrength;
        }

        private void UpdateWindAffectedObjects()
        {
            // Update all registered wind-affected objects
            for (int i = _windAffectedObjects.Count - 1; i >= 0; i--)
            {
                var windObject = _windAffectedObjects[i];
                if (windObject == null)
                {
                    _windAffectedObjects.RemoveAt(i);
                    continue;
                }

                windObject.ApplyWindForce(_windDirection, _windStrength);
            }
        }

        private void UpdateWindVector()
        {
            _currentWindVector = _windDirection * _windStrength;
        }

        private (Vector3 direction, float strength) GetWeatherWindData(WeatherType weatherType)
        {
            switch (weatherType)
            {
                case WeatherType.Clear:
                    return (Vector3.right, 0.5f);
                case WeatherType.Cloudy:
                    return (Vector3.right, 1f);
                case WeatherType.Storm:
                    return (Vector3.right, 3f);
                case WeatherType.HeavyRain:
                    return (Vector3.right, 2f);
                default:
                    return (_windDirection, _windStrength);
            }
        }

        /// <summary>
        /// Get wind performance statistics
        /// </summary>
        public WindPerformanceStats GetPerformanceStats()
        {
            return new WindPerformanceStats
            {
                IsEnabled = _enableWind,
                CurrentStrength = _windStrength,
                AffectedObjectCount = _windAffectedObjects.Count,
                WindVector = _currentWindVector
            };
        }
    }

    /// <summary>
    /// Interface for objects affected by wind
    /// </summary>
    public interface IWindAffected
    {
        void ApplyWindForce(Vector3 windDirection, float windStrength);
        Vector3 Position { get; }
        bool IsActive { get; }
    }

    /// <summary>
    /// Wind performance statistics
    /// </summary>
    [System.Serializable]
    public struct WindPerformanceStats
    {
        public bool IsEnabled;
        public float CurrentStrength;
        public int AffectedObjectCount;
        public Vector3 WindVector;
    }
}