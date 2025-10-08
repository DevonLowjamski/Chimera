using ProjectChimera.Core.Logging;
using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Core;
using System.Linq;
using Logger = ProjectChimera.Core.Logging.ChimeraLogger;

namespace ProjectChimera.Systems.Services.SpeedTree.Environmental
{
    /// <summary>
    /// REFACTORED: Wind System Coordinator for SpeedTree Plants
    /// Single Responsibility: Coordinate wind animations, zones, and effects through helper classes
    /// Reduced from 586 lines using composition with WindZoneManager and WindApplicator
    /// </summary>
    public class WindSystem : MonoBehaviour
    {
        [Header("Wind System Configuration")]
        [SerializeField] private bool _enableWindAnimation = true;
        [SerializeField] private float _globalWindStrength = 1.0f;
        [SerializeField] private Vector3 _globalWindDirection = Vector3.right;
        [SerializeField] private float _windVariationFrequency = 0.1f;
        [SerializeField] private float _windGustFrequency = 2.0f;

        // Helper components (Composition pattern for SRP)
        private WindZoneManager _windZoneManager;
        private WindApplicator _windApplicator;

        // Global wind state
        private float _currentWindStrength = 0f;
        private Vector3 _currentWindDirection = Vector3.zero;
        private float _windVariationTimer = 0f;
        private float _windGustTimer = 0f;
        private float _gustStrength = 0f;

        #region Public Events
        public event Action<float> OnWindStrengthChanged;
        public event Action<Vector3> OnWindDirectionChanged;
        public event Action<float> OnWindGust;
        #endregion

        #region Initialization
        public void Initialize()
        {
            Logger.Log("SPEEDTREE/WIND", "Initialize", this);

            // Initialize helper components
            _windZoneManager = new WindZoneManager();
            _windApplicator = new WindApplicator();

            // Initialize wind parameters
            _currentWindStrength = _globalWindStrength;
            _currentWindDirection = _globalWindDirection.normalized;

            // Find existing wind zones
            FindExistingWindZones();

            Logger.Log("SPEEDTREE/WIND", "Initialized", this);
        }

        public void Shutdown()
        {
            Logger.Log("SPEEDTREE/WIND", "Shutdown", this);

            _windZoneManager?.Clear();

            Logger.Log("SPEEDTREE/WIND", "Shutdown Complete", this);
        }

        private void FindExistingWindZones()
        {
            var registry = ServiceContainerFactory.Instance?.TryResolve<ProjectChimera.Core.Performance.IGameObjectRegistry>();
            var windZones = registry?.GetAll<WindZone>();

            if (windZones != null && windZones.Any())
            {
                Logger.Log("SPEEDTREE/WIND", "Resolved WindZones from GameObjectRegistry", this);
                foreach (var windZone in windZones)
                    _windZoneManager.RegisterWindZone(windZone);
            }
        }

        #endregion

        #region Wind Zone Management

        public void RegisterWindZone(WindZone windZone) =>
            _windZoneManager?.RegisterWindZone(windZone);

        public void UnregisterWindZone(WindZone windZone) =>
            _windZoneManager?.UnregisterWindZone(windZone);

        public void UpdateWindZone(WindZone windZone) =>
            _windZoneManager?.UpdateWindZone(windZone);

        #endregion

        #region Wind Update
        public void UpdateGlobalWind()
        {
            _windVariationTimer += Time.deltaTime;
            _windGustTimer += Time.deltaTime;

            if (_windVariationTimer >= _windVariationFrequency)
            {
                UpdateWindVariation();
                _windVariationTimer = 0f;
            }

            if (_windGustTimer >= _windGustFrequency)
            {
                UpdateWindGusts();
                _windGustTimer = 0f;
            }

            if (_enableWindAnimation)
            {
                _windApplicator?.ApplyGlobalWindToRenderers(_currentWindStrength, _currentWindDirection);

                // Apply wind to all active wind zones
                foreach (var windZone in _windZoneManager.ActiveWindZones)
                {
                    var settings = _windZoneManager.GetWindZoneSettings(windZone);
                    if (settings != null)
                        _windApplicator?.ApplyWindToZoneRenderers(windZone, settings, _currentWindStrength, _currentWindDirection);
                }
            }
        }

        private void UpdateWindVariation()
        {
            float variationStrength = UnityEngine.Random.Range(0.8f, 1.2f);
            float newStrength = _globalWindStrength * variationStrength;

            float angleVariation = UnityEngine.Random.Range(-10f, 10f);
            Vector3 newDirection = Quaternion.Euler(0, angleVariation, 0) * _globalWindDirection;

            if (Mathf.Abs(newStrength - _currentWindStrength) > 0.1f)
            {
                _currentWindStrength = newStrength;
                OnWindStrengthChanged?.Invoke(_currentWindStrength);
            }

            if (Vector3.Angle(newDirection, _currentWindDirection) > 5f)
            {
                _currentWindDirection = newDirection.normalized;
                OnWindDirectionChanged?.Invoke(_currentWindDirection);
            }
        }

        private void UpdateWindGusts()
        {
            if (UnityEngine.Random.value < 0.1f) // 10% chance per gust interval
            {
                float gustStrength = UnityEngine.Random.Range(1.5f, 3.0f);
                OnWindGust?.Invoke(gustStrength);
            }
        }

        #endregion

        #region Public Interface

        public void RegisterPlant(int plantId)
        {
            // Plants automatically affected by wind through renderer materials
            Logger.Log("SPEEDTREE/WIND", $"Plant {plantId} registered for wind effects", this);
        }

        public void UnregisterPlant(int plantId)
        {
            Logger.Log("SPEEDTREE/WIND", $"Plant {plantId} unregistered from wind effects", this);
        }

        public void UpdateWindEffects(int plantId, float windStrength, Vector3 windDirection)
        {
            // Wind effects are applied globally through materials
            Logger.LogVerbose("SPEEDTREE/WIND", $"Wind effect update requested for plant {plantId}", this);
        }

        public WindStatistics GetStatistics()
        {
            return new WindStatistics
            {
                GlobalStrength = _currentWindStrength,
                GlobalDirection = _currentWindDirection,
                ActiveWindZones = _windZoneManager?.WindZoneCount ?? 0,
                GustCount = 0,
                AverageStrength = _currentWindStrength,
                LastUpdate = Time.realtimeSinceStartup
            };
        }

        public bool EnableWindAnimation
        {
            get => _enableWindAnimation;
            set => _enableWindAnimation = value;
        }

        public void ResetWind()
        {
            _currentWindStrength = _globalWindStrength;
            _currentWindDirection = _globalWindDirection.normalized;
            _windVariationTimer = 0f;
            _windGustTimer = 0f;

            Logger.Log("SPEEDTREE/WIND", "Wind system reset", this);
        }

        public void SetWindStrength(float strength)
        {
            _globalWindStrength = Mathf.Max(0f, strength);
            _currentWindStrength = _globalWindStrength;
            Logger.Log("SPEEDTREE/WIND", $"Wind strength set to {strength}", this);
        }

        public void SetWindDirection(Vector3 direction)
        {
            _globalWindDirection = direction.normalized;
            _currentWindDirection = _globalWindDirection;
            Logger.Log("SPEEDTREE/WIND", $"Wind direction set to {direction}", this);
        }

        public float GetCurrentWindStrength() => _currentWindStrength;

        public Vector3 GetCurrentWindDirection() => _currentWindDirection;

        public IReadOnlyList<WindZone> GetActiveWindZones() =>
            _windZoneManager?.ActiveWindZones ?? new List<WindZone>();

        #endregion

        #region Update System
        public void Tick(float deltaTime)
        {
            if (!_enableWindAnimation) return;

            UpdateGlobalWind();

            // Update all wind zones
            foreach (var windZone in _windZoneManager.ActiveWindZones)
            {
                if (windZone != null && windZone.gameObject.activeInHierarchy)
                    _windZoneManager.UpdateWindZone(windZone);
            }
        }

        public WindState GetCurrentWindState()
        {
            return new WindState
            {
                Strength = _currentWindStrength,
                Direction = _currentWindDirection,
                IsActive = _enableWindAnimation,
                LastUpdate = Time.realtimeSinceStartup
            };
        }

        public void CreateWindGust(float gustStrength = 2.0f, float gustDuration = 1.5f)
        {
            StartCoroutine(CreateWindGustCoroutine(gustStrength, gustDuration));
        }

        private System.Collections.IEnumerator CreateWindGustCoroutine(float gustStrength, float gustDuration)
        {
            float originalStrength = _currentWindStrength;
            _gustStrength = gustStrength;

            // Gradually increase to gust strength
            float elapsed = 0f;
            float rampUpTime = gustDuration * 0.3f;

            while (elapsed < rampUpTime)
            {
                _currentWindStrength = Mathf.Lerp(originalStrength, originalStrength * gustStrength, elapsed / rampUpTime);
                OnWindStrengthChanged?.Invoke(_currentWindStrength);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Hold gust
            float holdTime = gustDuration * 0.4f;
            yield return new UnityEngine.WaitForSeconds(holdTime);

            // Gradually decrease back to original
            elapsed = 0f;
            float rampDownTime = gustDuration * 0.3f;

            while (elapsed < rampDownTime)
            {
                _currentWindStrength = Mathf.Lerp(originalStrength * gustStrength, originalStrength, elapsed / rampDownTime);
                OnWindStrengthChanged?.Invoke(_currentWindStrength);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _currentWindStrength = originalStrength;
            _gustStrength = 0f;
            OnWindGust?.Invoke(gustStrength);
        }

        #endregion
    }
}
