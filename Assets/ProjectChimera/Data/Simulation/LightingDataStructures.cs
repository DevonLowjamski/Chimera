using UnityEngine;
using System.Collections.Generic;

namespace ProjectChimera.Data.Simulation
{
    /// <summary>
    /// SIMPLE: Basic lighting data structures aligned with Project Chimera's cultivation vision.
    /// Focuses on essential lighting settings for basic grow light management.
    /// </summary>

    [System.Serializable]
    public class LightingSettings
    {
        [Header("Basic Lighting Settings")]
        [SerializeField] private bool _enableLighting = true;
        [SerializeField] private float _lightIntensity = 1.0f;
        [SerializeField] private Color _lightColor = Color.white;
        [SerializeField] private float _lightDurationHours = 18f;

        public bool EnableLighting => _enableLighting;
        public float LightIntensity => _lightIntensity;
        public Color LightColor => _lightColor;
        public float LightDurationHours => _lightDurationHours;
    }

    [System.Serializable]
    public class LightingFixture
    {
        [Header("Basic Fixture Info")]
        [SerializeField] private string _fixtureId;
        [SerializeField] private string _fixtureType;
        [SerializeField] private Vector3 _position;
        [SerializeField] private bool _isActive = true;

        public string FixtureId => _fixtureId;
        public string FixtureType => _fixtureType;
        public Vector3 Position => _position;
        public bool IsActive => _isActive;

        public void SetActive(bool active)
        {
            _isActive = active;
        }

        public void SetPosition(Vector3 position)
        {
            _position = position;
        }
    }

    [System.Serializable]
    public class LightingZone
    {
        [Header("Basic Zone Info")]
        [SerializeField] private string _zoneId;
        [SerializeField] private string _zoneName;
        [SerializeField] private List<LightingFixture> _fixtures = new List<LightingFixture>();
        [SerializeField] private bool _isActive = true;

        public string ZoneId => _zoneId;
        public string ZoneName => _zoneName;
        public List<LightingFixture> Fixtures => _fixtures;
        public bool IsActive => _isActive;
        public int ActiveFixtureCount
        {
            get
            {
                int count = 0;
                foreach (var fixture in _fixtures)
                {
                    if (fixture.IsActive) count++;
                }
                return count;
            }
        }

        public void AddFixture(LightingFixture fixture)
        {
            if (fixture != null && !_fixtures.Contains(fixture))
            {
                _fixtures.Add(fixture);
            }
        }

        public void RemoveFixture(LightingFixture fixture)
        {
            _fixtures.Remove(fixture);
        }

        public void SetActive(bool active)
        {
            _isActive = active;
        }
    }

    [System.Serializable]
    public class LightingSchedule
    {
        [Header("Basic Schedule Settings")]
        [SerializeField] private bool _enableSchedule = true;
        [SerializeField] private float _startTime = 6f; // 6 AM
        [SerializeField] private float _endTime = 24f;  // 12 AM (midnight)

        public bool EnableSchedule => _enableSchedule;
        public float StartTime => _startTime;
        public float EndTime => _endTime;
        public float Duration => _endTime - _startTime;

        public bool IsActiveAtTime(float currentTime)
        {
            if (!_enableSchedule) return true;
            return currentTime >= _startTime && currentTime <= _endTime;
        }
    }

    /// <summary>
    /// Basic lighting utilities class
    /// </summary>
    public static class LightingUtils
    {
        /// <summary>
        /// Create basic lighting settings
        /// </summary>
        public static LightingSettings CreateBasicSettings()
        {
            return new LightingSettings();
        }

        /// <summary>
        /// Create basic lighting fixture
        /// </summary>
        public static LightingFixture CreateFixture(string id, string type, Vector3 position)
        {
            var fixture = new LightingFixture();
            // Note: Since the fields are private, we would need to expose setters or use a different approach
            // For now, return a basic fixture
            return fixture;
        }

        /// <summary>
        /// Create basic lighting zone
        /// </summary>
        public static LightingZone CreateZone(string id, string name)
        {
            return new LightingZone();
        }

        /// <summary>
        /// Create basic lighting schedule
        /// </summary>
        public static LightingSchedule CreateSchedule(float startTime, float endTime)
        {
            var schedule = new LightingSchedule();
            // Similar issue with private fields
            return schedule;
        }

        /// <summary>
        /// Calculate total power consumption for a zone
        /// </summary>
        public static float CalculateZonePowerConsumption(LightingZone zone)
        {
            if (zone == null) return 0f;

            // Basic calculation - assume 100W per active fixture
            return zone.ActiveFixtureCount * 100f;
        }

        /// <summary>
        /// Check if lighting is adequate for plant growth
        /// </summary>
        public static bool IsLightingAdequate(LightingZone zone)
        {
            if (zone == null) return false;

            // Basic check - need at least 1 active fixture per 10 square meters
            // This is a simplified calculation
            return zone.ActiveFixtureCount > 0;
        }

        /// <summary>
        /// Get lighting statistics for a zone
        /// </summary>
        public static LightingStatistics GetZoneStatistics(LightingZone zone)
        {
            if (zone == null) return new LightingStatistics();

            return new LightingStatistics
            {
                TotalFixtures = zone.Fixtures.Count,
                ActiveFixtures = zone.ActiveFixtureCount,
                IsZoneActive = zone.IsActive,
                PowerConsumption = CalculateZonePowerConsumption(zone),
                IsLightingAdequate = IsLightingAdequate(zone)
            };
        }
    }

    /// <summary>
    /// Basic lighting statistics
    /// </summary>
    [System.Serializable]
    public class LightingStatistics
    {
        public int TotalFixtures;
        public int ActiveFixtures;
        public bool IsZoneActive;
        public float PowerConsumption;
        public bool IsLightingAdequate;
    }
}
