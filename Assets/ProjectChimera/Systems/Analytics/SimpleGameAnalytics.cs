using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Simple analytics system aligned with Project Chimera vision
    /// Basic cultivation and gameplay tracking
    /// </summary>
    public class SimpleGameAnalytics : MonoBehaviour
    {
        [Header("Analytics Settings")]
        [SerializeField] private bool _enableAnalytics = true;
        [SerializeField] private int _maxStoredEvents = 1000;

        private AnalyticsData _analyticsData;

        // Events
        public event Action<HarvestData> OnHarvestTracked;
        public event Action<FacilityUpgradeData> OnFacilityUpgradeTracked;

        private void Awake()
        {
            InitializeAnalytics();
        }

        private void InitializeAnalytics()
        {
            _analyticsData = new AnalyticsData
            {
                SessionStartTime = DateTime.Now,
                Events = new List<GameEvent>(),
                HarvestMetrics = new HarvestMetrics(),
                FacilityMetrics = new FacilityMetrics(),
                PlayerMetrics = new PlayerMetrics()
            };

            ChimeraLogger.LogInfo("ANALYTICS", "Analytics action completed", this);
        }

        #region Harvest Tracking

        /// <summary>
        /// Track a harvest event
        /// </summary>
        public void TrackHarvest(string plantType, float weight, float quality, float thcContent, float revenue)
        {
            if (!_enableAnalytics) return;

            var harvestData = new HarvestData
            {
                PlantType = plantType,
                Weight = weight,
                Quality = quality,
                THCContent = thcContent,
                Revenue = revenue,
                Timestamp = DateTime.Now
            };

            // Update metrics
            _analyticsData.HarvestMetrics.TotalHarvests++;
            _analyticsData.HarvestMetrics.TotalWeight += weight;
            _analyticsData.HarvestMetrics.TotalRevenue += revenue;
            _analyticsData.HarvestMetrics.AverageQuality = ((_analyticsData.HarvestMetrics.AverageQuality * (_analyticsData.HarvestMetrics.TotalHarvests - 1)) + quality) / _analyticsData.HarvestMetrics.TotalHarvests;

            // Store event
            var gameEvent = new GameEvent
            {
                EventType = GameEventType.Harvest,
                Timestamp = DateTime.Now,
                Data = JsonUtility.ToJson(harvestData)
            };

            _analyticsData.Events.Add(gameEvent);
            TrimEvents();

            OnHarvestTracked?.Invoke(harvestData);
            ChimeraLogger.LogInfo("ANALYTICS", "Analytics action completed", this);
        }

        /// <summary>
        /// Get harvest metrics
        /// </summary>
        public HarvestMetrics GetHarvestMetrics()
        {
            return _analyticsData.HarvestMetrics;
        }

        #endregion

        #region Facility Tracking

        /// <summary>
        /// Track facility construction/upgrade
        /// </summary>
        public void TrackFacilityUpgrade(string upgradeType, float cost, string facilityName = "")
        {
            if (!_enableAnalytics) return;

            var upgradeData = new FacilityUpgradeData
            {
                UpgradeType = upgradeType,
                Cost = cost,
                FacilityName = facilityName,
                Timestamp = DateTime.Now
            };

            // Update metrics
            _analyticsData.FacilityMetrics.TotalUpgrades++;
            _analyticsData.FacilityMetrics.TotalSpent += cost;

            // Store event
            var gameEvent = new GameEvent
            {
                EventType = GameEventType.FacilityUpgrade,
                Timestamp = DateTime.Now,
                Data = JsonUtility.ToJson(upgradeData)
            };

            _analyticsData.Events.Add(gameEvent);
            TrimEvents();

            OnFacilityUpgradeTracked?.Invoke(upgradeData);
            ChimeraLogger.LogInfo("ANALYTICS", "Analytics action completed", this);
        }

        /// <summary>
        /// Track environmental condition changes
        /// </summary>
        public void TrackEnvironmentalChange(string conditionType, float value, string location = "")
        {
            if (!_enableAnalytics) return;

            var envData = new EnvironmentalData
            {
                ConditionType = conditionType,
                Value = value,
                Location = location,
                Timestamp = DateTime.Now
            };

            var gameEvent = new GameEvent
            {
                EventType = GameEventType.EnvironmentalChange,
                Timestamp = DateTime.Now,
                Data = JsonUtility.ToJson(envData)
            };

            _analyticsData.Events.Add(gameEvent);
            TrimEvents();

            ChimeraLogger.LogInfo("ANALYTICS", "Analytics action completed", this);
        }

        /// <summary>
        /// Get facility metrics
        /// </summary>
        public FacilityMetrics GetFacilityMetrics()
        {
            return _analyticsData.FacilityMetrics;
        }

        #endregion

        #region Player Progression Tracking

        /// <summary>
        /// Track player level progression
        /// </summary>
        public void TrackLevelProgression(int newLevel, string skillType)
        {
            if (!_enableAnalytics) return;

            _analyticsData.PlayerMetrics.CurrentLevel = newLevel;

            var progressionData = new ProgressionData
            {
                NewLevel = newLevel,
                SkillType = skillType,
                Timestamp = DateTime.Now
            };

            var gameEvent = new GameEvent
            {
                EventType = GameEventType.Progression,
                Timestamp = DateTime.Now,
                Data = JsonUtility.ToJson(progressionData)
            };

            _analyticsData.Events.Add(gameEvent);
            TrimEvents();

            ChimeraLogger.LogInfo("ANALYTICS", "Analytics action completed", this);
        }

        /// <summary>
        /// Track skill point spending
        /// </summary>
        public void TrackSkillPointSpend(string skillName, int pointsSpent)
        {
            if (!_enableAnalytics) return;

            _analyticsData.PlayerMetrics.TotalSkillPointsSpent += pointsSpent;

            var skillData = new SkillData
            {
                SkillName = skillName,
                PointsSpent = pointsSpent,
                Timestamp = DateTime.Now
            };

            var gameEvent = new GameEvent
            {
                EventType = GameEventType.SkillUpgrade,
                Timestamp = DateTime.Now,
                Data = JsonUtility.ToJson(skillData)
            };

            _analyticsData.Events.Add(gameEvent);
            TrimEvents();

            ChimeraLogger.LogInfo("ANALYTICS", "Analytics action completed", this);
        }

        /// <summary>
        /// Get player metrics
        /// </summary>
        public PlayerMetrics GetPlayerMetrics()
        {
            return _analyticsData.PlayerMetrics;
        }

        #endregion

        #region Session Management

        /// <summary>
        /// Start a new session
        /// </summary>
        public void StartSession()
        {
            _analyticsData.SessionStartTime = DateTime.Now;
            _analyticsData.SessionDuration = TimeSpan.Zero;
            ChimeraLogger.LogInfo("ANALYTICS", "Analytics action completed", this);
        }

        /// <summary>
        /// End current session
        /// </summary>
        public void EndSession()
        {
            _analyticsData.SessionDuration = DateTime.Now - _analyticsData.SessionStartTime;
            ChimeraLogger.LogInfo("ANALYTICS", "Analytics action completed", this);
        }

        /// <summary>
        /// Get session info
        /// </summary>
        public SessionInfo GetSessionInfo()
        {
            return new SessionInfo
            {
                StartTime = _analyticsData.SessionStartTime,
                Duration = _analyticsData.SessionDuration,
                EventCount = _analyticsData.Events.Count
            };
        }

        #endregion

        #region Event Management

        private void TrimEvents()
        {
            if (_analyticsData.Events.Count > _maxStoredEvents)
            {
                _analyticsData.Events.RemoveRange(0, _analyticsData.Events.Count - _maxStoredEvents);
            }
        }

        /// <summary>
        /// Get recent events
        /// </summary>
        public List<GameEvent> GetRecentEvents(int count = 50)
        {
            int startIndex = Mathf.Max(0, _analyticsData.Events.Count - count);
            return _analyticsData.Events.GetRange(startIndex, Mathf.Min(count, _analyticsData.Events.Count));
        }

        /// <summary>
        /// Clear all events
        /// </summary>
        public void ClearEvents()
        {
            _analyticsData.Events.Clear();
            ChimeraLogger.LogInfo("ANALYTICS", "Analytics action completed", this);
        }

        #endregion

        #region Save/Load

        /// <summary>
        /// Get analytics data for saving
        /// </summary>
        public AnalyticsData GetSaveData()
        {
            return _analyticsData;
        }

        /// <summary>
        /// Load analytics data
        /// </summary>
        public void LoadSaveData(AnalyticsData data)
        {
            _analyticsData = data ?? new AnalyticsData();
            ChimeraLogger.LogInfo("ANALYTICS", "Analytics action completed", this);
        }

        #endregion
    }

    #region Data Structures

    [System.Serializable]
    public class AnalyticsData
    {
        public DateTime SessionStartTime;
        public TimeSpan SessionDuration;
        public List<GameEvent> Events = new List<GameEvent>();
        public HarvestMetrics HarvestMetrics = new HarvestMetrics();
        public FacilityMetrics FacilityMetrics = new FacilityMetrics();
        public PlayerMetrics PlayerMetrics = new PlayerMetrics();
    }

    [System.Serializable]
    public enum GameEventType
    {
        Harvest,
        FacilityUpgrade,
        EnvironmentalChange,
        Progression,
        SkillUpgrade,
        Session
    }

    [System.Serializable]
    public class GameEvent
    {
        public GameEventType EventType;
        public DateTime Timestamp;
        public string Data; // JSON serialized event data
    }

    [System.Serializable]
    public class HarvestData
    {
        public string PlantType;
        public float Weight;
        public float Quality;
        public float THCContent;
        public float Revenue;
        public DateTime Timestamp;
    }

    [System.Serializable]
    public class FacilityUpgradeData
    {
        public string UpgradeType;
        public float Cost;
        public string FacilityName;
        public DateTime Timestamp;
    }

    [System.Serializable]
    public class EnvironmentalData
    {
        public string ConditionType;
        public float Value;
        public string Location;
        public DateTime Timestamp;
    }

    [System.Serializable]
    public class ProgressionData
    {
        public int NewLevel;
        public string SkillType;
        public DateTime Timestamp;
    }

    [System.Serializable]
    public class SkillData
    {
        public string SkillName;
        public int PointsSpent;
        public DateTime Timestamp;
    }

    [System.Serializable]
    public class HarvestMetrics
    {
        public int TotalHarvests;
        public float TotalWeight;
        public float TotalRevenue;
        public float AverageQuality;
    }

    [System.Serializable]
    public class FacilityMetrics
    {
        public int TotalUpgrades;
        public float TotalSpent;
        public int ActiveFacilities;
    }

    [System.Serializable]
    public class PlayerMetrics
    {
        public int CurrentLevel;
        public int TotalSkillPointsSpent;
        public float TotalPlayTime;
    }

    [System.Serializable]
    public class SessionInfo
    {
        public DateTime StartTime;
        public TimeSpan Duration;
        public int EventCount;
    }

    #endregion
}
