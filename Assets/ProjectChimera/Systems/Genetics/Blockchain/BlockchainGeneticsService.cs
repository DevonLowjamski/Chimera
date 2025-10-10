using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Interfaces;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Genetics.Blockchain;

namespace ProjectChimera.Systems.Genetics.Blockchain
{
    /// Blockchain-verified genetics breeding service with invisible blockchain verification
    /// Wraps BreedingCore for secure, traceable genetics with player-friendly UI
    public class BlockchainGeneticsService : MonoBehaviour, IBlockchainGeneticsService
    {
        [Header("Blockchain Configuration")]
        [SerializeField] private bool _enableGPUMining = true;
        [SerializeField] private int _difficulty = GeneticLedger.DIFFICULTY; // 4 leading zeros
        [SerializeField] private int _maxMiningAttempts = 1000000; // Safety limit
        [Header("Player Identity")]
        [SerializeField] private string _playerSignature = "Player1"; // TODO: Replace with actual player ID
        private GeneticLedger _ledger;
        private BreedingCore _breedingCore;
        private GeneticProofOfWorkGPU _gpuMiner;
        private EnhancedFractalGeneticsEngine _fractalEngine;

        // Blockchain metadata storage (separate from PlantGenotype data structure)
        private Dictionary<string, BlockchainMetadata> _blockchainMetadata = new Dictionary<string, BlockchainMetadata>();
        private void Awake()
        {
            // Initialize blockchain ledger
            _ledger = new GeneticLedger();
            // Initialize GPU miner (if enabled)
            if (_enableGPUMining)
            {
                _gpuMiner = gameObject.AddComponent<GeneticProofOfWorkGPU>();
                _gpuMiner.Initialize();

                if (_gpuMiner.IsGPUMiningAvailable())
                {
                    ChimeraLogger.Log("BLOCKCHAIN",
                        "🚀 GPU mining enabled - breeding will be instant (<0.1s)!", this);
                }
                else
                {
                    ChimeraLogger.Log("BLOCKCHAIN",
                        "🔄 GPU unavailable - using CPU mining (<2s)", this);
                }
            }

            // Initialize enhanced fractal genetics engine
            _fractalEngine = gameObject.AddComponent<EnhancedFractalGeneticsEngine>();
            ChimeraLogger.Log("GENETICS",
                "🧬 Enhanced fractal genetics engine initialized (research-calibrated breeding)", this);
            // Register with service container
            var container = ServiceContainerFactory.Instance;
            if (container != null)
            {
                container.RegisterInstance<IBlockchainGeneticsService>(this);
            }
            ChimeraLogger.Log("BLOCKCHAIN",
                "Blockchain genetics service initialized (Video game mode: invisible blockchain)", this);
        }
        // Breeds two plants with blockchain verification (<1-2s with invisible blockchain proof-of-work)
        public async Task<PlantGenotype> BreedPlantsAsync(
            PlantGenotype parent1,
            PlantGenotype parent2,
            string strainName = null)
        {
            if (parent1 == null || parent2 == null)
            {
                ChimeraLogger.LogError("BLOCKCHAIN",
                    "Cannot breed: One or both parents are null", this);
                return null;
            }
            var startTime = Time.realtimeSinceStartup;
            // Auto-generate strain name if not provided
            if (string.IsNullOrEmpty(strainName))
            {
                var gen = CalculateOffspringGeneration(parent1, parent2);
                strainName = $"Hybrid F{gen}";
            }
            ChimeraLogger.Log("GENETICS",
                $"🧬 Breeding started: {strainName}", this);
            try
            {
                // STEP 1: Generate mutation seed (deterministic randomness for genetics)
                var mutationSeed = GenerateMutationSeed();
                // STEP 2: Perform genetic breeding calculations
                // This is where the "proof-of-work" happens - complex genetic math
                var offspring = await PerformGeneticBreeding(parent1, parent2, mutationSeed);
                // STEP 3: Create blockchain event packet
                var packet = await CreateBreedingEventPacket(
                    parent1, parent2, offspring,
                    mutationSeed, strainName);
                // STEP 4: Add to blockchain (validates and records)
                _ledger.AddBlock(packet);
                // STEP 5: Store blockchain metadata for offspring
                StoreBlockchainMetadata(offspring.GenotypeID, packet.BlockHash, packet.Generation);
                var duration = Time.realtimeSinceStartup - startTime;
                ChimeraLogger.Log("GENETICS",
                    $"✅ Breeding complete: {strainName} (Gen {packet.Generation}) in {duration:F2}s", this);
                return offspring;
            }
            catch (Exception ex)
            {
                var duration = Time.realtimeSinceStartup - startTime;
                ChimeraLogger.LogError("BLOCKCHAIN",
                    $"❌ Breeding failed after {duration:F2}s: {ex.Message}", this);
                return null;
            }
        }
        /// <summary>
        /// Performs the actual genetic breeding using enhanced fractal genetics.
        /// This async method allows UI to show progress while breeding calculates.
        ///
        /// GAMEPLAY: While this runs, UI shows animated "Calculating genetics..." message.
        /// Uses research-calibrated trait heritability for ultra-realistic breeding!
        /// </summary>
        private async Task<PlantGenotype> PerformGeneticBreeding(
            PlantGenotype parent1,
            PlantGenotype parent2,
            ulong mutationSeed)
        {
            // Yield to allow UI update
            await Task.Yield();
            // Use enhanced fractal genetics engine for realistic breeding
            // TODO: Get actual environmental data from cultivation system
            var environment = EnvironmentalProfile.Default;
            var offspring = _fractalEngine.GenerateOffspring(
                parent1,
                parent2,
                mutationSeed,
                environment);
            return offspring;
        }
        // Creates blockchain event packet with proof-of-work (invisible to player)
        private async Task<GeneEventPacket> CreateBreedingEventPacket(
            PlantGenotype parent1,
            PlantGenotype parent2,
            PlantGenotype offspring,
            ulong mutationSeed,
            string strainName)
        {
            var packet = new GeneEventPacket
            {
                PacketId = Guid.NewGuid().ToString(),
                ParentGenomeHash1 = CryptographicHasher.ComputeGenomeHash(parent1),
                ParentGenomeHash2 = CryptographicHasher.ComputeGenomeHash(parent2),
                OffspringGenomeHash = CryptographicHasher.ComputeGenomeHash(offspring),
                MutationSeed = mutationSeed,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                BreederSignature = _playerSignature,
                PreviousBlockHash = _ledger.GetLatestBlockHash(),
                StrainName = strainName,
                Generation = CalculateOffspringGeneration(parent1, parent2),
                Nonce = 0
            };

            // Find valid nonce (proof-of-work)
            // Use GPU mining if available, otherwise fall back to CPU
            if (_enableGPUMining && _gpuMiner != null && _gpuMiner.IsGPUMiningAvailable())
            {
                packet = await _gpuMiner.MineBlockAsync(packet, _difficulty);
            }
            else
            {
                packet = await MineBlockCPUAsync(packet);
            }

            return packet;
        }
        /// <summary>
        /// CPU-based proof-of-work mining (fallback).
        /// Searches for nonce that makes hash start with required zeros.
        ///
        /// PERFORMANCE: Typically completes in 0.1-2 seconds (difficulty = 4).
        /// GAMEPLAY: Player sees progress bar during this time.
        ///
        /// Week 6 TODO: Add GPU compute shader for <0.1s mining (instant feel).
        /// </summary>
        private async Task<GeneEventPacket> MineBlockCPUAsync(GeneEventPacket packet)
        {
            var startTime = Time.realtimeSinceStartup;
            int attempts = 0;

            // Search for valid nonce
            while (attempts < _maxMiningAttempts)
            {
                packet.Nonce = attempts;
                packet.BlockHash = packet.CalculateHash();

                // Check if hash meets difficulty (starts with enough zeros)
                if (packet.ValidateProofOfWork(_difficulty))
                {
                    var duration = Time.realtimeSinceStartup - startTime;
                    var hashRate = attempts / duration;

                    ChimeraLogger.Log("BLOCKCHAIN",
                        $"⛏️ Block mined in {duration:F2}s ({attempts:N0} attempts, {hashRate:F0} H/s)", this);

                    return packet;
                }

                attempts++;

                // Yield every 1000 attempts to keep UI responsive
                if (attempts % 1000 == 0)
                    await Task.Yield();
            }

            throw new InvalidOperationException(
                $"Failed to mine block after {_maxMiningAttempts:N0} attempts. " +
                "This should be extremely rare - check difficulty setting.");
        }
        /// <summary>
        /// Verifies strain authenticity by checking blockchain.
        ///
        /// GAMEPLAY USE CASES:
        /// 1. Marketplace purchase - check if strain is legit before buying
        /// 2. Achievement tracking - count verified breeds
        /// 3. Data integrity - detect save file tampering
        /// </summary>
        public bool VerifyStrainAuthenticity(PlantGenotype genotype)
        {
            if (genotype == null || string.IsNullOrEmpty(genotype.GenotypeID))
                return false;

            // Get blockchain metadata
            var metadata = GetBlockchainMetadata(genotype.GenotypeID);
            if (!metadata.BlockchainVerified || string.IsNullOrEmpty(metadata.BlockchainHash))
                return false;

            // Check if blockchain hash exists in ledger
            var packet = _ledger.GetPacketByHash(metadata.BlockchainHash);

            if (packet.BlockHash == null)
                return false; // Not in blockchain

            // Verify genome hash matches
            var actualHash = CryptographicHasher.ComputeGenomeHash(genotype);
            if (actualHash != packet.OffspringGenomeHash)
                return false; // Genome was modified after breeding

            return true;
        }
        /// <summary>
        /// Gets complete breeding lineage for strain.
        ///
        /// GAMEPLAY: Powers the family tree visualization UI.
        /// Shows chain of breeding from genesis → current strain.
        /// </summary>
        public List<GeneEventPacket> GetStrainLineage(PlantGenotype genotype)
        {
            if (genotype == null || string.IsNullOrEmpty(genotype.GenotypeID))
                return new List<GeneEventPacket>();

            var metadata = GetBlockchainMetadata(genotype.GenotypeID);
            if (string.IsNullOrEmpty(metadata.BlockchainHash))
                return new List<GeneEventPacket>();

            return _ledger.GetLineage(metadata.BlockchainHash);
        }
        /// <summary>
        /// Gets generation number (F1, F2, F3...).
        ///
        /// GAMEPLAY: Displayed in strain UI.
        /// Gen 0 = purchased seed
        /// Gen 1+ = bred by player
        /// </summary>
        public int GetGeneration(PlantGenotype genotype)
        {
            if (genotype == null)
                return 0;

            if (!string.IsNullOrEmpty(genotype.GenotypeID))
            {
                var metadata = GetBlockchainMetadata(genotype.GenotypeID);
                if (!string.IsNullOrEmpty(metadata.BlockchainHash))
                    return _ledger.GetGeneration(metadata.BlockchainHash);
            }

            return 0; // Default generation
        }
        /// <summary>
        /// Gets all strains player has bred.
        ///
        /// GAMEPLAY: Powers "My Strains" UI menu.
        /// </summary>
        public List<GeneEventPacket> GetPlayerBreedingHistory()
        {
            return _ledger.GetPlayerStrains(_playerSignature);
        }
        // Gets total breeding count for achievements.
        public int GetTotalBreedingCount()
        {
            return _ledger.GetChainLength();
        }
        /// <summary>
        /// Validates entire blockchain integrity.
        ///
        /// GAMEPLAY: Called on game load.
        /// Silent if successful, error message if corrupted.
        /// </summary>
        public bool ValidateBlockchain()
        {
            return _ledger.ValidateChain();
        }
        /// <summary>
        /// Registers a purchased/starter strain as genesis.
        ///
        /// GAMEPLAY: When player buys seed from marketplace or gets tutorial starter.
        /// </summary>
        public void RegisterGenesisStrain(PlantGenotype genotype, string strainName)
        {
            if (genotype == null)
            {
                ChimeraLogger.LogError("BLOCKCHAIN",
                    "Cannot register genesis strain: genotype is null", this);
                return;
            }

            var packet = new GeneEventPacket
            {
                PacketId = Guid.NewGuid().ToString(),
                ParentGenomeHash1 = string.Empty, // No parents (genesis)
                ParentGenomeHash2 = string.Empty,
                OffspringGenomeHash = CryptographicHasher.ComputeGenomeHash(genotype),
                MutationSeed = 0,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                BreederSignature = "GENESIS", // System-generated
                PreviousBlockHash = _ledger.GetLatestBlockHash(),
                StrainName = strainName,
                Generation = 0,
                Nonce = 0,
                BlockHash = "GENESIS_" + Guid.NewGuid().ToString() // Genesis blocks don't need mining
            };

            _ledger.AddBlock(packet);

            // Store blockchain metadata for genesis strain
            StoreBlockchainMetadata(genotype.GenotypeID, packet.BlockHash, 0);

            ChimeraLogger.Log("GENETICS",
                $"🌱 Genesis strain registered: {strainName}", this);
        }
        /// <summary>
        /// Gets blockchain verification info for UI display.
        ///
        /// GAMEPLAY: Powers strain info panel with player-friendly data.
        /// </summary>
        public BlockchainVerificationInfo GetVerificationInfo(PlantGenotype genotype)
        {
            var metadata = GetBlockchainMetadata(genotype?.GenotypeID);

            var info = new BlockchainVerificationInfo
            {
                IsVerified = VerifyStrainAuthenticity(genotype),
                FullHash = metadata.BlockchainHash ?? string.Empty,
                ShortHash = CryptographicHasher.GetShortHash(metadata.BlockchainHash ?? string.Empty),
                Generation = GetGeneration(genotype),
                HasLineage = false
            };

            // Get breeding event details if available
            if (!string.IsNullOrEmpty(metadata.BlockchainHash))
            {
                var packet = _ledger.GetPacketByHash(metadata.BlockchainHash);

                if (packet.BlockHash != null)
                {
                    info.StrainName = packet.StrainName;
                    info.BreederName = packet.BreederSignature;
                    info.BreedingDate = DateTimeOffset.FromUnixTimeSeconds(packet.Timestamp)
                        .ToString("yyyy-MM-dd");

                    var lineage = GetStrainLineage(genotype);
                    info.LineageDepth = lineage.Count;
                    info.HasLineage = lineage.Count > 0;
                }
            }

            // Generate generation label
            info.GenerationLabel = info.Generation == 0 ? "Purchased Seed" : $"F{info.Generation}";

            return info;
        }
        // ===== PRIVATE HELPER METHODS =====

        private ulong GenerateMutationSeed()
        {
            // Use Unity's Random for seed generation
            // In Week 7, this will integrate with fractal genetics system
            return (ulong)UnityEngine.Random.Range(0, int.MaxValue);
        }

        private int CalculateOffspringGeneration(PlantGenotype parent1, PlantGenotype parent2)
        {
            int gen1 = parent1?.Generation ?? 0;
            int gen2 = parent2?.Generation ?? 0;

            // Offspring is one generation higher than highest parent
            return Mathf.Max(gen1, gen2) + 1;
        }

        private PlantGenotype CreateOffspringGenotype(
            PlantGenotype parent1,
            PlantGenotype parent2,
            ulong mutationSeed)
        {
            // Create new genotype with simple trait averaging
            // TODO Phase 1 (Week 7): Replace with FractalGeneticsEngine for advanced breeding

            var offspring = new PlantGenotype
            {
                GenotypeID = Guid.NewGuid().ToString(),
                StrainName = $"{parent1.StrainName} × {parent2.StrainName}",
                PlantSpecies = "Cannabis"
            };

            // Create RNG from mutation seed for deterministic breeding
            var rng = new System.Random((int)(mutationSeed % int.MaxValue));

            // Simple trait averaging with minor random variation
            offspring.YieldPotential = AverageWithVariation(parent1.YieldPotential, parent2.YieldPotential, rng, 0.1f);
            offspring.PotencyPotential = AverageWithVariation(parent1.PotencyPotential, parent2.PotencyPotential, rng, 0.05f);
            offspring.FloweringTime = (parent1.FloweringTime + parent2.FloweringTime) / 2;
            offspring.MaxHeight = AverageWithVariation(parent1.MaxHeight, parent2.MaxHeight, rng, 0.15f);

            // Random plant type inheritance (simplified)
            offspring.PlantType = rng.Next(2) == 0 ? parent1.PlantType : parent2.PlantType;

            // Plant-specific traits
            offspring.RootSystemDepth = AverageWithVariation(parent1.RootSystemDepth, parent2.RootSystemDepth, rng, 0.1f);
            offspring.LeafThickness = AverageWithVariation(parent1.LeafThickness, parent2.LeafThickness, rng, 0.1f);
            offspring.StemStrength = AverageWithVariation(parent1.StemStrength, parent2.StemStrength, rng, 0.1f);
            offspring.PhotoperiodSensitivity = AverageWithVariation(parent1.PhotoperiodSensitivity, parent2.PhotoperiodSensitivity, rng, 0.1f);

            return offspring;
        }
        // Average two values with random variation
        private float AverageWithVariation(float val1, float val2, System.Random rng, float variationPercent)
        {
            var avg = (val1 + val2) / 2f;
            var variation = avg * variationPercent * ((float)rng.NextDouble() * 2f - 1f); // -variation to +variation
            return Mathf.Max(0, avg + variation);
        }

        private void OnDestroy()
        {
            // Cleanup if needed
            var container = ServiceContainerFactory.Instance;
            if (container != null && container.IsRegistered<IBlockchainGeneticsService>())
            {
                // Unregister logic if needed
            }
        }
        // Store blockchain metadata for a genotype
        private void StoreBlockchainMetadata(string genotypeId, string blockchainHash, int generation)
        {
            if (string.IsNullOrEmpty(genotypeId))
                genotypeId = Guid.NewGuid().ToString();

            _blockchainMetadata[genotypeId] = new BlockchainMetadata
            {
                BlockchainHash = blockchainHash,
                BlockchainVerified = true,
                Generation = generation,
                VerificationTimestamp = DateTime.Now
            };
        }
        // Get blockchain metadata for a genotype
        public BlockchainMetadata GetBlockchainMetadata(string genotypeId)
        {
            if (_blockchainMetadata.TryGetValue(genotypeId, out var metadata))
                return metadata;

            return new BlockchainMetadata { BlockchainVerified = false };
        }
        // Check if a genotype is blockchain-verified
        public bool IsBlockchainVerified(string genotypeId)
        {
            return _blockchainMetadata.ContainsKey(genotypeId) &&
                   _blockchainMetadata[genotypeId].BlockchainVerified;
        }
    }

    /// <summary>
    /// Blockchain metadata for genetics (stored separately from PlantGenotype)
    /// </summary>
    [Serializable]
    public struct BlockchainMetadata
    {
        public string BlockchainHash;
        public bool BlockchainVerified;
        public int Generation;
        public DateTime VerificationTimestamp;
    }
}
