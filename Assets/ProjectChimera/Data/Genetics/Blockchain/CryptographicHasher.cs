using System;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using ProjectChimera.Data.Genetics;

namespace ProjectChimera.Data.Genetics.Blockchain
{
    /// <summary>
    /// Cryptographic hashing utilities for genetic blockchain.
    /// Provides SHA-256 hashing for genome fingerprinting and blockchain verification.
    ///
    /// GAMEPLAY PURPOSE:
    /// - Creates unique "fingerprints" for each genetic combination
    /// - Enables strain authenticity verification (can't be forged)
    /// - Powers the "✅ Verified Strain" badge players see
    ///
    /// TECHNICAL NOTE:
    /// Uses SHA-256 (industry standard) for cryptographic security.
    /// Deterministic: same genetics → same hash (always).
    /// Collision-resistant: different genetics → different hash (virtually impossible to fake).
    /// </summary>
    public static class CryptographicHasher
    {
        /// <summary>
        /// Computes SHA-256 hash of a string.
        /// Returns hex string (e.g., "a3f5c9e2...") for blockchain verification.
        /// PERFORMANCE: ~0.001ms per hash - negligible gameplay impact.
        /// </summary>
        public static string ComputeSHA256(string data)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(data);
                var hashBytes = sha256.ComputeHash(bytes);
                return ByteArrayToHexString(hashBytes);
            }
        }

        /// <summary>
        /// Computes cryptographic hash of a plant's complete genotype.
        /// This creates a unique "genetic fingerprint" for strain identification.
        ///
        /// GAMEPLAY: Powers strain verification and prevents genetic forgery.
        /// If someone tries to fake "Blue Dream", the hash won't match - instant detection.
        ///
        /// DETERMINISTIC: Same genetics always produce the same hash.
        /// This is critical for blockchain verification.
        /// </summary>
        public static string ComputeGenomeHash(PlantGenotype genotype)
        {
            if (genotype == null)
                return string.Empty;

            var sb = new StringBuilder();

            // Include basic genotype identity
            sb.Append($"ID:{genotype.GenotypeID ?? "unknown"}|");
            sb.Append($"Strain:{genotype.StrainName}|");
            sb.Append($"Species:{genotype.PlantSpecies}|");

            // Include core cannabis traits (from CannabisGenotype base)
            sb.Append($"Yield:{genotype.YieldPotential:F8}|");
            sb.Append($"Potency:{genotype.PotencyPotential:F8}|");
            sb.Append($"FlowerTime:{genotype.FloweringTime}|");
            sb.Append($"Height:{genotype.MaxHeight:F8}|");
            sb.Append($"Type:{genotype.PlantType}|");

            // Include plant-specific traits
            sb.Append($"RootDepth:{genotype.RootSystemDepth:F8}|");
            sb.Append($"LeafThick:{genotype.LeafThickness:F8}|");
            sb.Append($"StemStr:{genotype.StemStrength:F8}|");
            sb.Append($"Photoperiod:{genotype.PhotoperiodSensitivity:F8}|");

            // Include genotype dictionary if present (must be deterministic - sort by key)
            if (genotype.Genotype != null && genotype.Genotype.Count > 0)
            {
                var sortedGenotype = genotype.Genotype.OrderBy(kvp => kvp.Key);
                foreach (var kvp in sortedGenotype)
                {
                    sb.Append($"{kvp.Key}:{kvp.Value}|");
                }
            }

            // Hash the complete genetic data
            var genomeString = sb.ToString();
            return ComputeSHA256(genomeString);
        }

        /// <summary>
        /// Verifies that a genotype matches its claimed hash.
        /// GAMEPLAY: Used to detect save file tampering or corrupted genetics.
        /// Returns true if genetics are authentic and unmodified.
        /// </summary>
        public static bool VerifyGenomeHash(PlantGenotype genotype, string claimedHash)
        {
            var actualHash = ComputeGenomeHash(genotype);
            return actualHash == claimedHash;
        }

        /// <summary>
        /// Creates a shortened hash for display in UI.
        /// Full hash: "a3f5c9e2b1d8..."  (64 characters)
        /// Short hash: "a3f5...d8f1"      (12 characters)
        ///
        /// GAMEPLAY: Shows in strain info UI as "Blockchain ID: a3f5...d8f1"
        /// Players can verify strains match in marketplace by comparing short IDs.
        /// </summary>
        public static string GetShortHash(string fullHash, int prefixLength = 8, int suffixLength = 4)
        {
            if (string.IsNullOrEmpty(fullHash) || fullHash.Length < prefixLength + suffixLength)
                return fullHash;

            var prefix = fullHash.Substring(0, prefixLength);
            var suffix = fullHash.Substring(fullHash.Length - suffixLength);

            return $"{prefix}...{suffix}";
        }

        /// <summary>
        /// Converts byte array to hexadecimal string.
        /// Used internally for hash formatting.
        /// </summary>
        private static string ByteArrayToHexString(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                sb.Append(b.ToString("x2")); // Lowercase hex

            return sb.ToString();
        }

        /// <summary>
        /// Converts hexadecimal string to byte array.
        /// Used for hash deserialization.
        /// </summary>
        public static byte[] HexStringToByteArray(string hex)
        {
            if (hex.Length % 2 != 0)
                throw new ArgumentException("Hex string must have even length");

            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }

            return bytes;
        }

        /// <summary>
        /// Checks if a hash meets proof-of-work difficulty requirement.
        /// Difficulty = number of leading zeros required.
        ///
        /// Example (difficulty = 4):
        /// ✅ Valid:   "0000a3f5c9e2..."  (starts with 4 zeros)
        /// ❌ Invalid: "000fa3f5c9e2..."  (only 3 zeros)
        ///
        /// GAMEPLAY: This is checked during breeding - genetic calculations
        /// must produce a hash with enough leading zeros to be "valid".
        /// This is what makes the blockchain secure (can't fake breeding events).
        /// </summary>
        public static bool MeetsDifficulty(string hash, int difficulty)
        {
            if (string.IsNullOrEmpty(hash) || hash.Length < difficulty)
                return false;

            var prefix = new string('0', difficulty);
            return hash.StartsWith(prefix);
        }

        /// <summary>
        /// Counts leading zeros in a hash.
        /// Used for difficulty verification and debugging.
        /// </summary>
        public static int CountLeadingZeros(string hash)
        {
            if (string.IsNullOrEmpty(hash))
                return 0;

            int count = 0;
            foreach (char c in hash)
            {
                if (c == '0')
                    count++;
                else
                    break;
            }

            return count;
        }
    }
}
