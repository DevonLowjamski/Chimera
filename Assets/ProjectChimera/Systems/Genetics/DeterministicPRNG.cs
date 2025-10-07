using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Genetics
{
    /// <summary>
    /// SIMPLE: Basic deterministic random number generator aligned with Project Chimera's genetics vision.
    /// Focuses on essential randomization for basic breeding mechanics.
    /// </summary>
    public class DeterministicPRNG : MonoBehaviour
    {
        [Header("Basic Random Settings")]
        [SerializeField] private int _seed = 12345;
        [SerializeField] private bool _enableLogging = true;

        // Basic random state
        private System.Random _random;
        private bool _isInitialized = false;

        /// <summary>
        /// Initialize the basic PRNG
        /// </summary>
        public void Initialize(int? customSeed = null)
        {
            if (_isInitialized) return;

            int seed = customSeed ?? _seed;
            _random = new System.Random(seed);
            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("OTHER", "$1", this);
            }
        }

        /// <summary>
        /// Generate a random float between 0 and 1
        /// </summary>
        public float NextFloat()
        {
            EnsureInitialized();
            return (float)_random.NextDouble();
        }

        /// <summary>
        /// Generate a random float between min and max
        /// </summary>
        public float NextFloat(float min, float max)
        {
            EnsureInitialized();
            return Mathf.Lerp(min, max, (float)_random.NextDouble());
        }

        /// <summary>
        /// Generate a random integer
        /// </summary>
        public int NextInt()
        {
            EnsureInitialized();
            return _random.Next();
        }

        /// <summary>
        /// Generate a random integer between min and max (exclusive)
        /// </summary>
        public int NextInt(int min, int max)
        {
            EnsureInitialized();
            return _random.Next(min, max);
        }

        /// <summary>
        /// Generate a random boolean
        /// </summary>
        public bool NextBool()
        {
            EnsureInitialized();
            return _random.Next(2) == 1;
        }

        /// <summary>
        /// Generate a random Vector3
        /// </summary>
        public Vector3 NextVector3(float min, float max)
        {
            return new Vector3(
                NextFloat(min, max),
                NextFloat(min, max),
                NextFloat(min, max)
            );
        }

        /// <summary>
        /// Set a new seed
        /// </summary>
        public void SetSeed(int newSeed)
        {
            _seed = newSeed;
            _random = new System.Random(_seed);

            if (_enableLogging)
            {
                ChimeraLogger.Log("OTHER", "$1", this);
            }
        }

        /// <summary>
        /// Get current seed
        /// </summary>
        public int GetCurrentSeed()
        {
            return _seed;
        }

        /// <summary>
        /// Reset to original seed
        /// </summary>
        public void ResetToOriginalSeed()
        {
            SetSeed(_seed);
        }

        /// <summary>
        /// Check if initialized
        /// </summary>
        public bool IsInitialized()
        {
            return _isInitialized;
        }

        /// <summary>
        /// Get random statistics
        /// </summary>
        public RandomStatistics GetStatistics()
        {
            return new RandomStatistics
            {
                IsInitialized = _isInitialized,
                CurrentSeed = _seed,
                EnableLogging = _enableLogging
            };
        }

        #region Private Methods

        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
        }

        #endregion
    }

    /// <summary>
    /// Random statistics
    /// </summary>
    [System.Serializable]
    public class RandomStatistics
    {
        public bool IsInitialized;
        public int CurrentSeed;
        public bool EnableLogging;
    }
}
