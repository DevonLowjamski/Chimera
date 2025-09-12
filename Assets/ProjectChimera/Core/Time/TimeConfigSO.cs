using UnityEngine;

namespace ProjectChimera.Core
{
    /// <summary>
    /// ScriptableObject configuration for time management settings
    /// </summary>
    [CreateAssetMenu(fileName = "TimeConfig", menuName = "Project Chimera/Core/Time Config")]
    public class TimeConfigSO : ScriptableObject
    {
        [Header("Time Scale Settings")]
        [SerializeField] private float _baseTimeScale = 1f;
        [SerializeField] private float _minTimeScale = 0.1f;
        [SerializeField] private float _maxTimeScale = 10f;

        [Header("Offline Progression")]
        [SerializeField] private bool _enableOfflineProgression = true;
        [SerializeField] private float _maxOfflineHours = 168f; // 7 days
        [SerializeField] private float _offlineEfficiencyMultiplier = 0.5f;

        [Header("Speed Penalties")]
        [SerializeField] private bool _enableSpeedPenalties = true;
        [SerializeField] private float _lowHealthPenalty = 0.5f;
        [SerializeField] private float _criticalHealthPenalty = 0.1f;

        [Header("Display Settings")]
        [SerializeField] private TimeDisplayFormat _defaultDisplayFormat = TimeDisplayFormat.Hour12;
        [SerializeField] private bool _showRealTime = true;
        [SerializeField] private bool _showGameTime = true;

        // Public Properties
        public float BaseTimeScale => _baseTimeScale;
        public float MinTimeScale => _minTimeScale;
        public float MaxTimeScale => _maxTimeScale;
        public bool EnableOfflineProgression => _enableOfflineProgression;
        public float MaxOfflineHours => _maxOfflineHours;
        public float OfflineEfficiencyMultiplier => _offlineEfficiencyMultiplier;
        public bool EnableSpeedPenalties => _enableSpeedPenalties;
        public float LowHealthPenalty => _lowHealthPenalty;
        public float CriticalHealthPenalty => _criticalHealthPenalty;
        public TimeDisplayFormat DefaultDisplayFormat => _defaultDisplayFormat;
        public bool ShowRealTime => _showRealTime;
        public bool ShowGameTime => _showGameTime;
    }
}