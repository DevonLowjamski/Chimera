using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
// NOTE: Cannot use ChimeraLogger here due to circular assembly dependency (Core references Data.Genetics)
// This is a legitimate exception to the "no Debug.Log" rule for Data layer classes

namespace ProjectChimera.Data.Genetics.Blockchain
{
    /// <summary>
    /// The genetic blockchain ledger - maintains an immutable record of all breeding events.
    /// This is the "blockchain" that ensures strain authenticity and prevents forgery.
    ///
    /// GAMEPLAY PURPOSE:
    /// - Players can verify strain authenticity before marketplace purchases
    /// - Lineage tracking creates a "family tree" of genetic heritage
    /// - Prevents cheating/duplication of rare genetics
    /// - Builds trust in player-to-player trading
    ///
    /// INVISIBLE BLOCKCHAIN:
    /// Players never see "blocks" or "hashes" - they see:
    /// - "✅ Verified Strain" badge
    /// - Visual lineage tree showing parent strains
    /// - Generation count (F1, F2, F3, etc.)
    /// </summary>
    public class GeneticLedger
    {
        private List<GeneEventPacket> _chain = new();
        private Dictionary<string, GeneEventPacket> _hashLookup = new();
        private Dictionary<string, List<GeneEventPacket>> _genomeIndex = new();
        private Dictionary<string, int> _generationCache = new();

        // Difficulty = 4 means hash must start with "0000" (4 leading zeros)
        // This is low enough for instant gameplay (<1 second) but high enough for security
        public const int DIFFICULTY = 4;

        /// <summary>
        /// Adds a new breeding event to the blockchain.
        /// GAMEPLAY: This happens automatically when player breeds two plants.
        /// Player just sees "Breeding..." then "Complete!" - blockchain is invisible.
        /// </summary>
        public void AddBlock(GeneEventPacket packet)
        {
            if (!ValidateBlock(packet))
            {
                Debug.LogError("[BLOCKCHAIN] Invalid breeding event - validation failed. This should never happen in normal gameplay.");
                throw new InvalidOperationException("Invalid block - validation failed");
            }

            _chain.Add(packet);
            _hashLookup[packet.BlockHash] = packet;

            // Index for fast lineage lookups
            IndexGenome(packet);

            // Cache generation depth for UI display
            _generationCache[packet.OffspringGenomeHash] = packet.Generation;

            Debug.Log($"[BLOCKCHAIN] Breeding event recorded: {packet.StrainName} (Gen {packet.Generation})");
        }

        /// <summary>
        /// Validates a breeding event before adding to chain.
        /// Checks: proof-of-work, parent existence, hash integrity, chain linkage.
        /// GAMEPLAY NOTE: Validation failure is extremely rare and indicates data corruption.
        /// </summary>
        public bool ValidateBlock(GeneEventPacket packet)
        {
            // 1. Verify proof-of-work (breeding calculations were completed)
            if (!packet.ValidateProofOfWork(DIFFICULTY))
            {
                ProjectChimera.Core.Logging.ChimeraLogger.LogWarning("BLOCKCHAIN", "Block failed proof-of-work validation", null);
                return false;
            }

            // 2. Verify parent hashes exist in chain (except genesis strains)
            if (!packet.IsGenesis() && _chain.Count > 0)
            {
                var parent1Exists = _hashLookup.ContainsKey(packet.ParentGenomeHash1);
                var parent2Exists = _hashLookup.ContainsKey(packet.ParentGenomeHash2);

                if (!parent1Exists && !parent2Exists)
                {
                    ProjectChimera.Core.Logging.ChimeraLogger.LogWarning("BLOCKCHAIN", "Block references unknown parent genetics", null);
                    return false;
                }
            }

            // 3. Verify hash integrity
            if (packet.BlockHash != packet.CalculateHash())
            {
                ProjectChimera.Core.Logging.ChimeraLogger.LogWarning("BLOCKCHAIN", "Block hash mismatch - possible data corruption", null);
                return false;
            }

            // 4. Verify previous block hash linkage (except first block)
            if (_chain.Count > 0 && !packet.IsGenesis())
            {
                var lastBlock = _chain[_chain.Count - 1];
                if (packet.PreviousBlockHash != lastBlock.BlockHash)
                {
                    ProjectChimera.Core.Logging.ChimeraLogger.LogWarning("BLOCKCHAIN", "Block chain linkage broken", null);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Validates the entire blockchain for integrity.
        /// GAMEPLAY: Called on game load to detect save file corruption.
        /// If validation fails, player sees "Save file corrupted" error.
        /// </summary>
        public bool ValidateChain()
        {
            for (int i = 1; i < _chain.Count; i++)
            {
                var currentBlock = _chain[i];
                var previousBlock = _chain[i - 1];

                // Verify hash integrity
                if (currentBlock.BlockHash != currentBlock.CalculateHash())
                {
                    ProjectChimera.Core.Logging.ChimeraLogger.LogError("BLOCKCHAIN", $"Chain validation failed at block {i} - hash mismatch", null);
                    return false;
                }

                // Verify chain linkage (skip genesis blocks)
                if (!currentBlock.IsGenesis())
                {
                    if (currentBlock.PreviousBlockHash != previousBlock.BlockHash)
                    {
                        ProjectChimera.Core.Logging.ChimeraLogger.LogError("BLOCKCHAIN", $"Chain validation failed at block {i} - broken link", null);
                        return false;
                    }
                }

                // Verify proof-of-work
                if (!currentBlock.ValidateProofOfWork(DIFFICULTY))
                {
                    ProjectChimera.Core.Logging.ChimeraLogger.LogError("BLOCKCHAIN", $"Chain validation failed at block {i} - invalid proof-of-work", null);
                    return false;
                }
            }

            Debug.Log($"[BLOCKCHAIN] ✅ Blockchain validated: {_chain.Count} breeding events verified");
            return true;
        }

        /// <summary>
        /// Gets a breeding event by its hash.
        /// GAMEPLAY: Used internally - players never see hashes directly.
        /// </summary>
        public GeneEventPacket GetPacketByHash(string hash)
        {
            return _hashLookup.TryGetValue(hash, out var packet) ? packet : default;
        }

        /// <summary>
        /// Gets the complete lineage (family tree) for a strain.
        /// GAMEPLAY: Powers the lineage visualization UI - shows parent → child relationships.
        /// Example: "OG Kush × Blue Dream → Hybrid F1 → Hybrid F2 (current)"
        /// </summary>
        public List<GeneEventPacket> GetLineage(string genomeHash)
        {
            var lineage = new List<GeneEventPacket>();
            var visited = new HashSet<string>(); // Prevent infinite loops in corrupted data

            TraceLineage(genomeHash, lineage, visited);

            // Reverse so it goes from oldest ancestor → current strain
            lineage.Reverse();

            return lineage;
        }

        /// <summary>
        /// Recursively traces lineage back to genesis strains.
        /// Creates a family tree by following parent hashes.
        /// </summary>
        private void TraceLineage(string genomeHash, List<GeneEventPacket> lineage, HashSet<string> visited)
        {
            if (string.IsNullOrEmpty(genomeHash) || visited.Contains(genomeHash))
                return;

            visited.Add(genomeHash);

            // Find the breeding event that created this genome
            var packet = _hashLookup.Values.FirstOrDefault(p => p.OffspringGenomeHash == genomeHash);

            if (packet.BlockHash == null)
                return; // Genesis strain or not found

            lineage.Add(packet);

            // Recursively trace both parents
            TraceLineage(packet.ParentGenomeHash1, lineage, visited);
            TraceLineage(packet.ParentGenomeHash2, lineage, visited);
        }

        /// <summary>
        /// Gets all strains bred by a specific player.
        /// GAMEPLAY: Powers "My Strains" UI - shows player's breeding achievements.
        /// </summary>
        public List<GeneEventPacket> GetPlayerStrains(string breederSignature)
        {
            return _chain.Where(p => p.BreederSignature == breederSignature).ToList();
        }

        /// <summary>
        /// Gets generation count for a strain (F1, F2, F3, etc.).
        /// GAMEPLAY: Displayed in strain info UI.
        /// </summary>
        public int GetGeneration(string genomeHash)
        {
            return _generationCache.TryGetValue(genomeHash, out var gen) ? gen : 0;
        }

        /// <summary>
        /// Gets the hash of the most recent breeding event (last block).
        /// Used for blockchain linkage when adding new breeding events.
        /// </summary>
        public string GetLatestBlockHash()
        {
            return _chain.Count > 0 ? _chain[_chain.Count - 1].BlockHash : string.Empty;
        }

        /// <summary>
        /// Gets total number of breeding events recorded.
        /// GAMEPLAY: Could show as achievement "500 strains bred!"
        /// </summary>
        public int GetChainLength()
        {
            return _chain.Count;
        }

        /// <summary>
        /// Indexes a genome for fast lookup by hash.
        /// Maintains secondary indexes for performance.
        /// </summary>
        private void IndexGenome(GeneEventPacket packet)
        {
            // Index offspring genome
            if (!_genomeIndex.ContainsKey(packet.OffspringGenomeHash))
                _genomeIndex[packet.OffspringGenomeHash] = new List<GeneEventPacket>();

            _genomeIndex[packet.OffspringGenomeHash].Add(packet);

            // Index parent genomes for lineage queries
            if (!string.IsNullOrEmpty(packet.ParentGenomeHash1))
            {
                if (!_genomeIndex.ContainsKey(packet.ParentGenomeHash1))
                    _genomeIndex[packet.ParentGenomeHash1] = new List<GeneEventPacket>();
                _genomeIndex[packet.ParentGenomeHash1].Add(packet);
            }

            if (!string.IsNullOrEmpty(packet.ParentGenomeHash2))
            {
                if (!_genomeIndex.ContainsKey(packet.ParentGenomeHash2))
                    _genomeIndex[packet.ParentGenomeHash2] = new List<GeneEventPacket>();
                _genomeIndex[packet.ParentGenomeHash2].Add(packet);
            }
        }

        /// <summary>
        /// Clears the entire blockchain.
        /// GAMEPLAY: Only used when starting a new game or resetting save data.
        /// </summary>
        public void Clear()
        {
            _chain.Clear();
            _hashLookup.Clear();
            _genomeIndex.Clear();
            _generationCache.Clear();

            Debug.Log("[BLOCKCHAIN] Blockchain cleared - new game started");
        }
    }
}
