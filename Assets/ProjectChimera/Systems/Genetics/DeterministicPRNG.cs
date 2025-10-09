using System;

namespace ProjectChimera.Systems.Genetics
{
    /// <summary>
    /// Deterministic pseudo-random number generator for reproducible genetics.
    /// Same seed → same offspring (critical for blockchain verification!)
    ///
    /// USAGE: Used by EnhancedFractalGeneticsEngine for deterministic breeding.
    /// IMPORTANT: Must be deterministic across platforms for blockchain consensus.
    /// </summary>
    public class DeterministicPRNG
    {
        private System.Random _rng;

        /// <summary>
        /// Create a new deterministic PRNG with the given seed.
        /// Same seed will always produce the same sequence of random numbers.
        /// </summary>
        public DeterministicPRNG(ulong seed)
        {
            // Convert ulong to int for System.Random (platform-consistent)
            _rng = new System.Random((int)(seed % int.MaxValue));
        }

        /// <summary>
        /// Generate a random float between min and max (inclusive).
        /// </summary>
        public float NextFloat(float min, float max)
        {
            return (float)(_rng.NextDouble() * (max - min) + min);
        }

        /// <summary>
        /// Generate a random boolean.
        /// </summary>
        public bool NextBool()
        {
            return _rng.Next(2) == 0;
        }

        /// <summary>
        /// Generate a random integer between min (inclusive) and max (exclusive).
        /// </summary>
        public int NextInt(int min, int max)
        {
            return _rng.Next(min, max);
        }
    }
}
