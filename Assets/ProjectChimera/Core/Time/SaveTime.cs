using ProjectChimera.Core.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Implementation for time tracking, persistence, and display formatting
    /// </summary>
    public class SaveTime : ISaveTime
    {
        private DateTime _gameStartTime;
        private DateTime _sessionStartTime;
        private float _accumulatedGameTime = 0.0f;
        private float _accumulatedRealTime = 0.0f;
        private readonly Queue<float> _frameTimeHistory = new Queue<float>();
        private const int MAX_FRAME_HISTORY = 60;

        // Display settings
        private bool _showCombinedTimeDisplay = true;
        private bool _showTimeEfficiencyRatio = true;
        private bool _showPenaltyInformation = true;
        private TimeDisplayFormat _timeFormat = TimeDisplayFormat.Adaptive;

        public DateTime SessionStartTime => _sessionStartTime;
        public DateTime GameStartTime => _gameStartTime;
        public TimeSpan TotalGameTime => DateTime.Now - _gameStartTime;
        public TimeSpan SessionDuration => DateTime.Now - _sessionStartTime;
        public bool IsTimePaused { get; private set; }

        public void Initialize()
        {
            _gameStartTime = DateTime.Now;
            _sessionStartTime = DateTime.Now;
            _accumulatedGameTime = 0.0f;
            _accumulatedRealTime = 0.0f;
            _frameTimeHistory.Clear();

            ChimeraLogger.Log("[SaveTime] Time tracking system initialized");
        }

        public void Shutdown()
        {
            _frameTimeHistory.Clear();
            ChimeraLogger.Log("[SaveTime] Time tracking system shutdown");
        }

        public void SetGameStartTime(DateTime startTime)
        {
            _gameStartTime = startTime;
        }

        public void SetSessionStartTime(DateTime startTime)
        {
            _sessionStartTime = startTime;
        }

        public void TrackFrameTime()
        {
            _frameTimeHistory.Enqueue(Time.unscaledDeltaTime);
            
            if (_frameTimeHistory.Count > MAX_FRAME_HISTORY)
            {
                _frameTimeHistory.Dequeue();
            }
        }

        public void UpdateAccumulatedTimes(float deltaTime, float currentTimeScale, bool isPaused)
        {
            IsTimePaused = isPaused;

            if (!isPaused)
            {
                _accumulatedGameTime += deltaTime * currentTimeScale;
                _accumulatedRealTime += deltaTime;
            }

            TrackFrameTime();
        }

        public string GetGameTimeString()
        {
            TimeSpan gameTime = TimeSpan.FromSeconds(_accumulatedGameTime);
            
            if (gameTime.TotalDays >= 1)
            {
                return $"{(int)gameTime.TotalDays}d {gameTime.Hours:D2}h {gameTime.Minutes:D2}m";
            }
            else if (gameTime.TotalHours >= 1)
            {
                return $"{gameTime.Hours}h {gameTime.Minutes:D2}m {gameTime.Seconds:D2}s";
            }
            else if (gameTime.TotalMinutes >= 1)
            {
                return $"{gameTime.Minutes}m {gameTime.Seconds:D2}s";
            }
            else
            {
                return $"{gameTime.Seconds}s";
            }
        }

        public string GetRealTimeString()
        {
            TimeSpan realTime = SessionDuration;
            
            if (realTime.TotalHours >= 1)
            {
                return $"{(int)realTime.TotalHours}h {realTime.Minutes:D2}m";
            }
            else if (realTime.TotalMinutes >= 1)
            {
                return $"{realTime.Minutes}m {realTime.Seconds:D2}s";
            }
            else
            {
                return $"{realTime.Seconds}s";
            }
        }

        public string GetCombinedTimeString()
        {
            return $"Game: {GetGameTimeString()} | Real: {GetRealTimeString()}";
        }

        public string GetTimeEfficiencyString()
        {
            if (_accumulatedRealTime <= 0) return "1:1";
            
            float ratio = _accumulatedGameTime / _accumulatedRealTime;
            return $"{ratio:F1}:1";
        }

        public string GetCompactTimeStatus()
        {
            if (IsTimePaused)
            {
                return $"⏸ {GetGameTimeString()}";
            }
            else
            {
                return GetGameTimeString();
            }
        }

        public string GetDetailedTimeStatus()
        {
            var status = new StringBuilder();
            
            status.AppendLine($"Game Time: {GetGameTimeString()}");
            status.AppendLine($"Real Time: {GetRealTimeString()}");
            
            if (_showTimeEfficiencyRatio)
            {
                status.AppendLine($"Efficiency: {GetTimeEfficiencyString()}");
            }
            
            if (IsTimePaused)
            {
                status.AppendLine("⏸ PAUSED");
            }
            
            return status.ToString().TrimEnd();
        }

        public TimeDisplayData GetTimeDisplayData()
        {
            return new TimeDisplayData
            {
                GameTime = TimeSpan.FromSeconds(_accumulatedGameTime),
                RealTime = SessionDuration,
                TotalGameTime = TotalGameTime,
                TimeEfficiencyRatio = _accumulatedRealTime > 0 ? _accumulatedGameTime / _accumulatedRealTime : 1.0f,
                IsPaused = IsTimePaused
            };
        }

        public string FormatDurationWithConfig(float seconds)
        {
            return TimeManager.FormatDuration(seconds, _timeFormat);
        }

        public void SetTimeFormat(TimeDisplayFormat format)
        {
            _timeFormat = format;
            ChimeraLogger.Log($"[SaveTime] Time display format set to {format}");
        }

        public void SetDisplayOptions(bool showCombined, bool showEfficiency, bool showPenalty)
        {
            _showCombinedTimeDisplay = showCombined;
            _showTimeEfficiencyRatio = showEfficiency;
            _showPenaltyInformation = showPenalty;
            
            ChimeraLogger.Log($"[SaveTime] Display options updated - Combined: {showCombined}, Efficiency: {showEfficiency}, Penalty: {showPenalty}");
        }

        public float GetAccumulatedGameTime()
        {
            return _accumulatedGameTime;
        }

        public float GetAccumulatedRealTime()
        {
            return _accumulatedRealTime;
        }

        public float GetAverageFrameTime()
        {
            if (_frameTimeHistory.Count == 0) return 0f;
            
            float sum = 0f;
            foreach (float frameTime in _frameTimeHistory)
            {
                sum += frameTime;
            }
            return sum / _frameTimeHistory.Count;
        }
    }
}
