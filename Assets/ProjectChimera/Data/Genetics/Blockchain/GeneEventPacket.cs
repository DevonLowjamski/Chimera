using System;
using UnityEngine;

namespace ProjectChimera.Data.Genetics.Blockchain
{
    /// <summary>
    /// Represents a single breeding event in the genetic blockchain.
    /// This is the "block" in our blockchain - each successful breeding creates one.
    /// GAMEPLAY NOTE: Players never see this data structure directly - it's invisible infrastructure
    /// that enables trusted strain verification and lineage tracking for marketplace trading.
    /// </summary>
    [Serializable]
    public struct GeneEventPacket
    {
        [Header("Genetic Identity")]
        public string PacketId;              // Unique identifier for this breeding event
        public string ParentGenomeHash1;     // Cryptographic hash of parent 1's genetics
        public string ParentGenomeHash2;     // Cryptographic hash of parent 2's genetics
        public ulong MutationSeed;           // Deterministic seed for reproducible genetics

        [Header("Blockchain Data")]
        public long Timestamp;               // When this breeding occurred (Unix time)
        public string BreederSignature;      // Player's identifier (account ID)
        public string PreviousBlockHash;     // Links to previous block in chain
        public int Nonce;                    // Proof-of-work nonce (found during breeding calculation)
        public string BlockHash;             // This packet's unique cryptographic hash

        [Header("Gameplay Metadata")]
        public string OffspringGenomeHash;   // Hash of the resulting offspring
        public string StrainName;            // Player-given name (e.g., "Blue Dream F2")
        public int Generation;               // How many breeding generations deep

        /// <summary>
        /// Calculates the cryptographic hash of this breeding event.
        /// This hash uniquely identifies this genetic event and cannot be forged.
        /// PERFORMANCE NOTE: Only called during breeding - never in hot path.
        /// </summary>
        public string CalculateHash()
        {
            var dataToHash = $"{ParentGenomeHash1}{ParentGenomeHash2}{MutationSeed}{Timestamp}{BreederSignature}{PreviousBlockHash}{Nonce}";
            return CryptographicHasher.ComputeSHA256(dataToHash);
        }

        /// <summary>
        /// Validates that this packet's proof-of-work meets difficulty requirements.
        /// Difficulty = number of leading zeros required in hash (4 = "0000..." start).
        /// GAMEPLAY NOTE: This validation happens invisibly during breeding - the genetic
        /// calculations themselves serve as the proof-of-work.
        /// </summary>
        public bool ValidateProofOfWork(int difficulty)
        {
            var hash = CalculateHash();
            var prefix = new string('0', difficulty);
            return hash.StartsWith(prefix);
        }

        /// <summary>
        /// Creates a player-friendly description of this breeding event.
        /// Used in lineage UI to show "Blue Dream × OG Kush → Hybrid #7"
        /// </summary>
        public string GetBreedingDescription()
        {
            var date = DateTimeOffset.FromUnixTimeSeconds(Timestamp).ToString("yyyy-MM-dd");
            return $"Gen {Generation} breeding on {date}";
        }

        /// <summary>
        /// Checks if this packet represents a genesis (starter) strain.
        /// Genesis strains have no parents and start the blockchain.
        /// </summary>
        public bool IsGenesis()
        {
            return string.IsNullOrEmpty(ParentGenomeHash1) || string.IsNullOrEmpty(ParentGenomeHash2);
        }
    }
}
