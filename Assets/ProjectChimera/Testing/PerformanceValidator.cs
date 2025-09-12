using UnityEngine;
using System.Collections;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Testing
{
    /// <summary>
    /// BASIC: Simple performance validator for Project Chimera's testing system.
    /// Focuses on essential performance checks without complex validation systems and memory testing.
    /// </summary>
    public class PerformanceValidator : MonoBehaviour
    {
        [Header("Basic Performance Settings")]
        [SerializeField] private bool _enableBasicValidation = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private int _testIterations = 50;
        [SerializeField] private float _targetFrameRate = 60f;
        [SerializeField] private float _maxFrameTime = 16.67f; // ~60 FPS in milliseconds

        // Basic performance tracking
        private float _testStartTime;
        private int _framesTested = 0;
        private float _totalFrameTime = 0f;
        private float _minFrameTime = float.MaxValue;
        private float _maxFrameTime = 0f;
        private bool _isRunning = false;

        /// <summary>
        /// Events for performance testing
        /// </summary>
        public event System.Action<PerformanceResults> OnPerformanceTestCompleted;

        /// <summary>
        /// Start basic performance test
        /// </summary>
        public void StartPerformanceTest()
        {
            if (!_enableBasicValidation || _isRunning) return;

            _isRunning = true;
            _testStartTime = Time.realtimeSinceStartup;
            _framesTested = 0;
            _totalFrameTime = 0f;
            _minFrameTime = float.MaxValue;
            _maxFrameTime = 0f;

            StartCoroutine(RunPerformanceTest());

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[PerformanceValidator] Started performance test with {_testIterations} iterations");
            }
        }

        /// <summary>
        /// Stop performance test
        /// </summary>
        public void StopPerformanceTest()
        {
            _isRunning = false;

            if (_enableLogging)
            {
                ChimeraLogger.Log("[PerformanceValidator] Stopped performance test");
            }
        }

        /// <summary>
        /// Run basic performance test
        /// </summary>
        private IEnumerator RunPerformanceTest()
        {
            for (int i = 0; i < _testIterations && _isRunning; i++)
            {
                float frameStartTime = Time.realtimeSinceStartup;

                // Wait for next frame
                yield return null;

                float frameTime = (Time.realtimeSinceStartup - frameStartTime) * 1000f; // Convert to milliseconds

                _framesTested++;
                _totalFrameTime += frameTime;
                _minFrameTime = Mathf.Min(_minFrameTime, frameTime);
                _maxFrameTime = Mathf.Max(_maxFrameTime, frameTime);
            }

            CompletePerformanceTest();
        }

        /// <summary>
        /// Complete performance test and report results
        /// </summary>
        private void CompletePerformanceTest()
        {
            _isRunning = false;

            if (_framesTested == 0) return;

            float averageFrameTime = _totalFrameTime / _framesTested;
            float averageFPS = 1000f / averageFrameTime;
            float minFPS = 1000f / _maxFrameTime;
            float maxFPS = 1000f / _minFrameTime;

            var results = new PerformanceResults
            {
                TestDuration = Time.realtimeSinceStartup - _testStartTime,
                FramesTested = _framesTested,
                AverageFrameTime = averageFrameTime,
                MinFrameTime = _minFrameTime,
                MaxFrameTime = _maxFrameTime,
                AverageFPS = averageFPS,
                MinFPS = minFPS,
                MaxFPS = maxFPS,
                TargetFPS = _targetFrameRate,
                MeetsTargetFPS = averageFPS >= _targetFrameRate,
                PerformanceRating = GetPerformanceRating(averageFrameTime)
            };

            OnPerformanceTestCompleted?.Invoke(results);

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[PerformanceValidator] Test completed - Avg FPS: {averageFPS:F1}, Rating: {results.PerformanceRating}");
            }
        }

        /// <summary>
        /// Get quick performance check
        /// </summary>
        public PerformanceSnapshot GetQuickSnapshot()
        {
            float currentFrameTime = Time.deltaTime * 1000f;
            float currentFPS = 1f / Time.deltaTime;

            return new PerformanceSnapshot
            {
                CurrentFPS = currentFPS,
                CurrentFrameTime = currentFrameTime,
                MeetsTargetFPS = currentFPS >= _targetFrameRate,
                Timestamp = Time.realtimeSinceStartup
            };
        }

        /// <summary>
        /// Check if performance test is running
        /// </summary>
        public bool IsTestRunning()
        {
            return _isRunning;
        }

        /// <summary>
        /// Get performance validator statistics
        /// </summary>
        public ValidatorStats GetStats()
        {
            return new ValidatorStats
            {
                IsEnabled = _enableBasicValidation,
                IsRunning = _isRunning,
                TestIterations = _testIterations,
                TargetFrameRate = _targetFrameRate,
                MaxFrameTime = _maxFrameTime,
                FramesTested = _framesTested
            };
        }

        /// <summary>
        /// Set performance test parameters
        /// </summary>
        public void SetTestParameters(int iterations, float targetFPS)
        {
            _testIterations = Mathf.Max(1, iterations);
            _targetFrameRate = Mathf.Max(1f, targetFPS);
            _maxFrameTime = 1000f / _targetFrameRate;

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[PerformanceValidator] Updated test parameters - Iterations: {_testIterations}, Target FPS: {_targetFrameRate}");
            }
        }

        #region Private Methods

        private string GetPerformanceRating(float averageFrameTime)
        {
            if (averageFrameTime <= _maxFrameTime)
                return "Excellent";
            else if (averageFrameTime <= _maxFrameTime * 1.5f)
                return "Good";
            else if (averageFrameTime <= _maxFrameTime * 2f)
                return "Fair";
            else
                return "Poor";
        }

        #endregion
    }

    /// <summary>
    /// Performance test results
    /// </summary>
    [System.Serializable]
    public struct PerformanceResults
    {
        public float TestDuration;
        public int FramesTested;
        public float AverageFrameTime;
        public float MinFrameTime;
        public float MaxFrameTime;
        public float AverageFPS;
        public float MinFPS;
        public float MaxFPS;
        public float TargetFPS;
        public bool MeetsTargetFPS;
        public string PerformanceRating;
    }

    /// <summary>
    /// Performance snapshot
    /// </summary>
    [System.Serializable]
    public struct PerformanceSnapshot
    {
        public float CurrentFPS;
        public float CurrentFrameTime;
        public bool MeetsTargetFPS;
        public float Timestamp;
    }

    /// <summary>
    /// Validator statistics
    /// </summary>
    [System.Serializable]
    public struct ValidatorStats
    {
        public bool IsEnabled;
        public bool IsRunning;
        public int TestIterations;
        public float TargetFrameRate;
        public float MaxFrameTime;
        public int FramesTested;
    }
}
