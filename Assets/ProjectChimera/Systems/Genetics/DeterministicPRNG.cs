using UnityEngine;
using System;
using Unity.Collections;
using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Genetics
{
    /// <summary>
    /// Phase 2.1.2: Deterministic Pseudo-Random Number Generator for Genetics
    /// Provides reproducible randomness for genetic calculations, breeding, and testing
    /// Uses XorShift algorithm for high-quality, fast pseudorandom numbers
    /// Essential for stable genetics simulation and performance testing
    /// </summary>
    public class DeterministicPRNG : MonoBehaviour
    {
        #region Configuration

        [Header("PRNG Configuration")]
        [SerializeField] private uint _baseSeed = 12345;
        [SerializeField] private bool _enableSeededRandomization = true;
        [SerializeField] private bool _enablePerformanceLogging = false;

        [Header("Algorithm Selection")]
        [SerializeField] private PRNGAlgorithm _algorithm = PRNGAlgorithm.XorShift128Plus;
        [SerializeField] private bool _enableMultiThreading = true;
        [SerializeField] private int _threadPoolSize = 4;

        [Header("Validation")]
        [SerializeField] private bool _enableStatisticalValidation = true;
        [SerializeField] private int _validationSampleSize = 10000;
        [SerializeField] private float _acceptableDeviationThreshold = 0.05f;

        #endregion

        #region Private Fields

        private PRNGState _mainState;
        private PRNGState[] _threadStates;
        private bool _isInitialized = false;
        private PRNGStatistics _statistics;
        private DateTime _lastPerformanceCheck;

        #endregion

        #region Public Properties

        public bool IsInitialized => _isInitialized;
        public uint CurrentSeed => _mainState.Seed;
        public PRNGAlgorithm Algorithm => _algorithm;
        public PRNGStatistics Statistics => _statistics;

        #endregion

        #region Initialization

        public void Initialize(uint? customSeed = null)
        {
            if (_isInitialized) return;

            uint seed = customSeed ?? _baseSeed;
            ChimeraLogger.Log($"[DeterministicPRNG] Initializing with seed: {seed}, algorithm: {_algorithm}");

            // Initialize main state
            _mainState = new PRNGState(seed, _algorithm);

            // Initialize thread states for multi-threading
            if (_enableMultiThreading)
            {
                InitializeThreadStates(seed);
            }

            // Initialize statistics tracking
            _statistics = new PRNGStatistics();
            _lastPerformanceCheck = DateTime.Now;

            // Validate randomness quality
            if (_enableStatisticalValidation)
            {
                ValidateRandomnessQuality();
            }

            _isInitialized = true;
            ChimeraLogger.Log("[DeterministicPRNG] Deterministic PRNG initialized successfully");
        }

        private void InitializeThreadStates(uint baseSeed)
        {
            _threadStates = new PRNGState[_threadPoolSize];
            for (int i = 0; i < _threadPoolSize; i++)
            {
                // Create unique seed for each thread to avoid correlation
                uint threadSeed = baseSeed + (uint)(i * 1000000);
                _threadStates[i] = new PRNGState(threadSeed, _algorithm);
            }
        }

        private void ValidateRandomnessQuality()
        {
            ChimeraLogger.Log("[DeterministicPRNG] Validating randomness quality...");

            var validation = new PRNGValidation();
            bool isValid = validation.ValidateDistribution(this, _validationSampleSize, _acceptableDeviationThreshold);

            if (!isValid)
            {
                ChimeraLogger.LogWarning($"[DeterministicPRNG] Randomness quality validation failed. Consider adjusting algorithm or seed.");
            }
            else
            {
                ChimeraLogger.Log("[DeterministicPRNG] Randomness quality validation passed");
            }
        }

        #endregion

        #region Core Random Number Generation

        /// <summary>
        /// Generate next random float in range [0, 1)
        /// </summary>
        public float NextFloat()
        {
            if (!_isInitialized)
            {
                ChimeraLogger.LogWarning("[DeterministicPRNG] PRNG not initialized, using Unity's Random");
                return UnityEngine.Random.value;
            }

            uint randomBits = NextUInt();
            float result = (randomBits >> 8) * (1.0f / 16777216.0f); // Convert to [0,1) range

            _statistics.IncrementCount();
            return result;
        }

        /// <summary>
        /// Generate next random float in specified range
        /// </summary>
        public float NextFloat(float min, float max)
        {
            return min + NextFloat() * (max - min);
        }

        /// <summary>
        /// Generate next random integer
        /// </summary>
        public int NextInt()
        {
            return (int)NextUInt();
        }

        /// <summary>
        /// Generate next random integer in specified range [min, max)
        /// </summary>
        public int NextInt(int min, int max)
        {
            if (min >= max) return min;
            uint range = (uint)(max - min);
            return min + (int)(NextUInt() % range);
        }

        /// <summary>
        /// Generate next random unsigned integer (core method)
        /// </summary>
        public uint NextUInt()
        {
            if (!_isInitialized) return 0;

            uint result = GenerateUInt(ref _mainState);

            if (_enablePerformanceLogging && _statistics.TotalGenerated % 100000 == 0)
            {
                LogPerformanceMetrics();
            }

            return result;
        }

        /// <summary>
        /// Generate random boolean with 50% probability
        /// </summary>
        public bool NextBool()
        {
            return NextFloat() < 0.5f;
        }

        /// <summary>
        /// Generate random boolean with specified probability
        /// </summary>
        public bool NextBool(float probability)
        {
            return NextFloat() < probability;
        }

        /// <summary>
        /// Generate random Vector2
        /// </summary>
        public Vector2 NextVector2()
        {
            return new Vector2(NextFloat(), NextFloat());
        }

        /// <summary>
        /// Generate random Vector3
        /// </summary>
        public Vector3 NextVector3()
        {
            return new Vector3(NextFloat(), NextFloat(), NextFloat());
        }

        /// <summary>
        /// Generate random point on unit circle
        /// </summary>
        public Vector2 NextUnitCircle()
        {
            float angle = NextFloat() * 2f * Mathf.PI;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        /// <summary>
        /// Generate random point in unit sphere
        /// </summary>
        public Vector3 NextUnitSphere()
        {
            float theta = NextFloat() * 2f * Mathf.PI;
            float phi = Mathf.Acos(2f * NextFloat() - 1f);
            float sinPhi = Mathf.Sin(phi);

            return new Vector3(
                sinPhi * Mathf.Cos(theta),
                sinPhi * Mathf.Sin(theta),
                Mathf.Cos(phi)
            );
        }

        #endregion

        #region Algorithm Implementations

        private uint GenerateUInt(ref PRNGState state)
        {
            switch (state.Algorithm)
            {
                case PRNGAlgorithm.XorShift32:
                    return XorShift32(ref state);
                case PRNGAlgorithm.XorShift128Plus:
                    return XorShift128Plus(ref state);
                case PRNGAlgorithm.LinearCongruential:
                    return LinearCongruential(ref state);
                case PRNGAlgorithm.MersenneTwister:
                    return MersenneTwister(ref state);
                default:
                    return XorShift128Plus(ref state);
            }
        }

        private uint XorShift32(ref PRNGState state)
        {
            state.x ^= state.x << 13;
            state.x ^= state.x >> 17;
            state.x ^= state.x << 5;
            return state.x;
        }

        private uint XorShift128Plus(ref PRNGState state)
        {
            uint x = state.x;
            uint y = state.y;
            state.x = y;
            x ^= x << 23;
            state.y = x ^ y ^ (x >> 17) ^ (y >> 26);
            return state.y + y;
        }

        private uint LinearCongruential(ref PRNGState state)
        {
            // Using parameters from Numerical Recipes
            state.x = (state.x * 1664525u + 1013904223u);
            return state.x;
        }

        private uint MersenneTwister(ref PRNGState state)
        {
            // Simplified MT19937 implementation
            // This is a basic version - full MT would require more state
            const uint a = 0x9908B0DFu;
            const uint upperMask = 0x80000000u;
            const uint lowerMask = 0x7FFFFFFFu;

            uint y = (state.x & upperMask) + (state.y & lowerMask);
            state.x = state.y;
            state.y = state.z;
            state.z = state.w;
            state.w = state.w ^ (state.w >> 19) ^ (y >> 1) ^ ((y & 1u) * a);

            return state.w;
        }

        #endregion

        #region Genetics-Specific Methods

        /// <summary>
        /// Generate deterministic allele values for genetics
        /// </summary>
        public float[] GenerateAlleleValues(string geneId, int alleleCount)
        {
            // Create deterministic seed from gene ID
            uint geneSeed = HashString(geneId) ^ _mainState.Seed;
            PRNGState tempState = new PRNGState(geneSeed, _algorithm);

            float[] values = new float[alleleCount];
            for (int i = 0; i < alleleCount; i++)
            {
                values[i] = (GenerateUInt(ref tempState) >> 8) * (1.0f / 16777216.0f);
            }

            return values;
        }

        /// <summary>
        /// Generate consistent breeding results
        /// </summary>
        public DeterministicBreedingResult GenerateBreedingResult(string parentA, string parentB, int generation)
        {
            // Create deterministic seed from parents and generation
            uint breedingSeed = HashString(parentA) ^ HashString(parentB) ^ (uint)generation ^ _mainState.Seed;
            PRNGState tempState = new PRNGState(breedingSeed, _algorithm);

            return new DeterministicBreedingResult
            {
                OffspringId = $"{parentA}x{parentB}_G{generation}_{GenerateUInt(ref tempState)}",
                InheritancePattern = GenerateUInt(ref tempState) % 4, // 0-3 for different patterns
                MutationChance = (GenerateUInt(ref tempState) >> 8) * (1.0f / 16777216.0f),
                HybridVigor = (GenerateUInt(ref tempState) >> 8) * (1.0f / 16777216.0f)
            };
        }

        /// <summary>
        /// Generate mutation effects with consistency
        /// </summary>
        public MutationEffect GenerateMutationEffect(string geneId, float mutationRate)
        {
            uint mutationSeed = HashString(geneId) ^ _mainState.Seed;
            PRNGState tempState = new PRNGState(mutationSeed, _algorithm);

            float mutationRoll = (GenerateUInt(ref tempState) >> 8) * (1.0f / 16777216.0f);

            if (mutationRoll > mutationRate)
            {
                return MutationEffect.None;
            }

            return new MutationEffect
            {
                Type = (MutationType)(GenerateUInt(ref tempState) % 4),
                Magnitude = (GenerateUInt(ref tempState) >> 8) * (2.0f / 16777216.0f) - 1.0f, // [-1, 1]
                IsDeleterious = mutationRoll < mutationRate * 0.3f // 30% of mutations are harmful
            };
        }

        #endregion

        #region Thread-Safe Operations

        /// <summary>
        /// Get thread-specific random float (thread-safe)
        /// </summary>
        public float NextFloatThreadSafe(int threadId = 0)
        {
            if (!_enableMultiThreading || _threadStates == null)
            {
                return NextFloat();
            }

            threadId = threadId % _threadPoolSize;
            uint randomBits = GenerateUInt(ref _threadStates[threadId]);
            return (randomBits >> 8) * (1.0f / 16777216.0f);
        }

        /// <summary>
        /// Generate batch of random floats efficiently
        /// </summary>
        public void GenerateFloatBatch(float[] output, int threadId = 0)
        {
            if (!_enableMultiThreading || _threadStates == null)
            {
                for (int i = 0; i < output.Length; i++)
                {
                    output[i] = NextFloat();
                }
                return;
            }

            threadId = threadId % _threadPoolSize;
            ref PRNGState state = ref _threadStates[threadId];

            for (int i = 0; i < output.Length; i++)
            {
                uint randomBits = GenerateUInt(ref state);
                output[i] = (randomBits >> 8) * (1.0f / 16777216.0f);
            }
        }

        #endregion

        #region Seeding and State Management

        /// <summary>
        /// Reset PRNG to initial state with new seed
        /// </summary>
        public void SetSeed(uint newSeed)
        {
            _mainState = new PRNGState(newSeed, _algorithm);

            if (_enableMultiThreading)
            {
                InitializeThreadStates(newSeed);
            }

            _statistics.Reset();
            ChimeraLogger.Log($"[DeterministicPRNG] Seed updated to: {newSeed}");
        }

        /// <summary>
        /// Get current PRNG state for saving
        /// </summary>
        public PRNGState GetState()
        {
            return _mainState;
        }

        /// <summary>
        /// Restore PRNG state from save
        /// </summary>
        public void SetState(PRNGState state)
        {
            _mainState = state;
            _algorithm = state.Algorithm;
        }

        /// <summary>
        /// Create deterministic hash from string
        /// </summary>
        private uint HashString(string input)
        {
            if (string.IsNullOrEmpty(input)) return 0;

            uint hash = 2166136261u; // FNV-1a hash
            foreach (char c in input)
            {
                hash ^= c;
                hash *= 16777619u;
            }
            return hash;
        }

        #endregion

        #region Performance and Statistics

        private void LogPerformanceMetrics()
        {
            var now = DateTime.Now;
            var elapsed = (now - _lastPerformanceCheck).TotalMilliseconds;
            var rate = 100000.0 / elapsed * 1000.0; // Numbers per second

            ChimeraLogger.Log($"[DeterministicPRNG] Performance: {rate:F0} numbers/sec, Total: {_statistics.TotalGenerated}");
            _lastPerformanceCheck = now;
        }

        /// <summary>
        /// Get detailed performance statistics
        /// </summary>
        public string GetPerformanceReport()
        {
            return $"PRNG Performance Report:\n" +
                   $"Algorithm: {_algorithm}\n" +
                   $"Total Generated: {_statistics.TotalGenerated:N0}\n" +
                   $"Thread Pool Size: {_threadPoolSize}\n" +
                   $"Average: {_statistics.Average:F6}\n" +
                   $"Min: {_statistics.MinValue:F6}\n" +
                   $"Max: {_statistics.MaxValue:F6}";
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            if (_enablePerformanceLogging && _isInitialized)
            {
                ChimeraLogger.Log(GetPerformanceReport());
            }
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// PRNG algorithm types
    /// </summary>
    public enum PRNGAlgorithm
    {
        XorShift32,
        XorShift128Plus,
        LinearCongruential,
        MersenneTwister
    }

    /// <summary>
    /// PRNG internal state
    /// </summary>
    [System.Serializable]
    public struct PRNGState
    {
        public uint Seed;
        public uint x, y, z, w; // State variables for different algorithms
        public PRNGAlgorithm Algorithm;

        public PRNGState(uint seed, PRNGAlgorithm algorithm)
        {
            Seed = seed;
            Algorithm = algorithm;

            // Initialize state based on seed
            x = seed;
            y = seed * 1812433253u + 1u;
            z = y * 1812433253u + 1u;
            w = z * 1812433253u + 1u;
        }
    }

    /// <summary>
    /// PRNG statistics tracking
    /// </summary>
    [System.Serializable]
    public class PRNGStatistics
    {
        public long TotalGenerated = 0;
        public double Sum = 0.0;
        public float MinValue = float.MaxValue;
        public float MaxValue = float.MinValue;

        public double Average => TotalGenerated > 0 ? Sum / TotalGenerated : 0.0;

        public void IncrementCount()
        {
            TotalGenerated++;
        }

        public void UpdateStats(float value)
        {
            IncrementCount();
            Sum += value;
            if (value < MinValue) MinValue = value;
            if (value > MaxValue) MaxValue = value;
        }

        public void Reset()
        {
            TotalGenerated = 0;
            Sum = 0.0;
            MinValue = float.MaxValue;
            MaxValue = float.MinValue;
        }
    }

    /// <summary>
    /// Deterministic breeding result with PRNG data
    /// </summary>
    [System.Serializable]
    public struct DeterministicBreedingResult
    {
        public string OffspringId;
        public uint InheritancePattern;
        public float MutationChance;
        public float HybridVigor;
    }

    /// <summary>
    /// Mutation types for genetics
    /// </summary>
    public enum MutationType
    {
        Point,
        Insertion,
        Deletion,
        Duplication
    }

    /// <summary>
    /// Mutation effect data
    /// </summary>
    [System.Serializable]
    public struct MutationEffect
    {
        public MutationType Type;
        public float Magnitude;
        public bool IsDeleterious;

        public static MutationEffect None => new MutationEffect
        {
            Type = MutationType.Point,
            Magnitude = 0f,
            IsDeleterious = false
        };
    }

    /// <summary>
    /// PRNG validation utility
    /// </summary>
    public class PRNGValidation
    {
        public bool ValidateDistribution(DeterministicPRNG prng, int sampleSize, float threshold)
        {
            float[] samples = new float[sampleSize];
            for (int i = 0; i < sampleSize; i++)
            {
                samples[i] = prng.NextFloat();
            }

            // Check uniform distribution
            double mean = 0.0;
            foreach (float sample in samples)
            {
                mean += sample;
            }
            mean /= sampleSize;

            // Should be close to 0.5 for uniform [0,1) distribution
            double deviation = Math.Abs(mean - 0.5);
            return deviation < threshold;
        }
    }

    #endregion
}
