using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Cultivation;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// Controls game speed and time acceleration features
    /// </summary>
    public class GameSpeedController : MonoBehaviour
    {
        [Header("Speed Configuration")]
        [SerializeField] private TimeAccelerationGamingConfigSO _speedConfig;
        
        private bool _isInitialized = false;
        private GameTimeScale _currentScale = GameTimeScale.Normal;
        
        public GameTimeScale CurrentScale => _currentScale;
        
        public void Initialize(TimeAccelerationGamingConfigSO config)
        {
            _speedConfig = config;
            _isInitialized = true;
        }
        
        public void UpdateSystem(float deltaTime)
        {
            if (!_isInitialized) return;
            
            // Update speed control system
        }
        
        public void SetGameSpeed(GameTimeScale scale)
        {
            _currentScale = scale;
            
            float timeScale = scale switch
            {
                GameTimeScale.Paused => 0f,
                GameTimeScale.Slow => 0.5f,
                GameTimeScale.Normal => 1f,
                GameTimeScale.Fast => 2f,
                GameTimeScale.VeryFast => 4f,
                GameTimeScale.UltraFast => 8f,
                _ => 1f
            };
            
            Time.timeScale = timeScale;
        }
    }
}