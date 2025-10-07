# PROJECT CHIMERA: ULTIMATE IMPLEMENTATION ROADMAP
## Part 2: Phase 1 Core Systems Implementation

**Document Version:** 2.0 - Updated Based on Comprehensive Codebase Assessment
**Phase Duration:** 6-8 weeks (Weeks 5-13)
**Prerequisites:** Phase 0 complete (all anti-patterns eliminated, architecture stabilized)

---

## PHASE 1 OVERVIEW

**Goal:** Implement critical missing systems to achieve 80%+ completion for all three pillars, focusing on the blockchain genetics flagship feature, core UX systems, and essential gameplay mechanics.

**Key Deliverables:**
1. Blockchain genetics system (Week 5-7)
2. Missing Construction utilities (electricity, water, HVAC) (Week 6-7)
3. Contextual menu UI system (Week 7-8)
4. Tissue culture & micropropagation (Week 8)
5. Active IPM system (Week 8-9)
6. Time mechanics sophistication (Week 9)
7. Progression system (Skill Tree UI) (Week 10-11)
8. Marketplace platform (Week 11-12)
9. Processing pipeline (drying/curing) (Week 12-13)

---

## WEEK 5-7: BLOCKCHAIN GENETICS IMPLEMENTATION (FLAGSHIP FEATURE)

**Status:** Currently 0% - Completely missing
**Priority:** CRITICAL - Flagship feature, core differentiator
**Complexity:** High - Requires compute shaders, cryptography, distributed verification

### Week 5: Blockchain Foundation & Architecture

#### Day 1-2: Blockchain System Design & Core Data Structures

**Genetic Blockchain Architecture:**

```csharp
// Data/Genetics/Blockchain/GeneEventPacket.cs
[System.Serializable]
public struct GeneEventPacket
{
    public string PacketId;              // Unique packet identifier
    public string ParentGenomeHash1;     // Cryptographic hash of parent 1
    public string ParentGenomeHash2;     // Cryptographic hash of parent 2
    public ulong MutationSeed;           // Deterministic mutation seed
    public long Timestamp;               // Unix timestamp of breeding event
    public string BreederSignature;      // Player's cryptographic signature
    public string PreviousBlockHash;     // Hash of previous block in chain
    public int Nonce;                    // Proof-of-work nonce
    public string BlockHash;             // This packet's cryptographic hash

    public string CalculateHash()
    {
        var dataToHash = $"{ParentGenomeHash1}{ParentGenomeHash2}{MutationSeed}{Timestamp}{BreederSignature}{PreviousBlockHash}{Nonce}";
        return CryptographicHasher.ComputeSHA256(dataToHash);
    }

    public bool ValidateProofOfWork(int difficulty)
    {
        var hash = CalculateHash();
        var prefix = new string('0', difficulty);
        return hash.StartsWith(prefix);
    }
}

// Data/Genetics/Blockchain/GeneticLedger.cs
public class GeneticLedger
{
    private List<GeneEventPacket> _chain = new();
    private Dictionary<string, GeneEventPacket> _hashLookup = new();
    private Dictionary<string, List<GeneEventPacket>> _genomeIndex = new();

    public const int DIFFICULTY = 4; // Number of leading zeros required

    public void AddBlock(GeneEventPacket packet)
    {
        if (!ValidateBlock(packet))
            throw new InvalidOperationException("Invalid block - validation failed");

        _chain.Add(packet);
        _hashLookup[packet.BlockHash] = packet;
        IndexGenome(packet);
    }

    public bool ValidateBlock(GeneEventPacket packet)
    {
        // 1. Verify proof-of-work
        if (!packet.ValidateProofOfWork(DIFFICULTY))
            return false;

        // 2. Verify parent hashes exist in chain (except genesis)
        if (_chain.Count > 0)
        {
            if (!_hashLookup.ContainsKey(packet.ParentGenomeHash1) &&
                !_hashLookup.ContainsKey(packet.ParentGenomeHash2))
                return false;
        }

        // 3. Verify hash integrity
        if (packet.BlockHash != packet.CalculateHash())
            return false;

        // 4. Verify previous block hash
        if (_chain.Count > 0)
        {
            var lastBlock = _chain[_chain.Count - 1];
            if (packet.PreviousBlockHash != lastBlock.BlockHash)
                return false;
        }

        return true;
    }

    public bool ValidateChain()
    {
        for (int i = 1; i < _chain.Count; i++)
        {
            var currentBlock = _chain[i];
            var previousBlock = _chain[i - 1];

            // Verify hash integrity
            if (currentBlock.BlockHash != currentBlock.CalculateHash())
                return false;

            // Verify chain linkage
            if (currentBlock.PreviousBlockHash != previousBlock.BlockHash)
                return false;

            // Verify proof-of-work
            if (!currentBlock.ValidateProofOfWork(DIFFICULTY))
                return false;
        }

        return true;
    }

    public GeneEventPacket GetPacketByHash(string hash)
    {
        return _hashLookup.TryGetValue(hash, out var packet) ? packet : default;
    }

    public List<GeneEventPacket> GetLineage(string genomeHash)
    {
        var lineage = new List<GeneEventPacket>();
        var current = GetPacketByHash(genomeHash);

        while (current.BlockHash != null)
        {
            lineage.Add(current);

            // Trace back to parent (use first parent for linear trace)
            if (!string.IsNullOrEmpty(current.ParentGenomeHash1))
                current = GetPacketByHash(current.ParentGenomeHash1);
            else
                break;
        }

        return lineage;
    }

    private void IndexGenome(GeneEventPacket packet)
    {
        // Index by genome hash for quick lineage lookups
        if (!_genomeIndex.ContainsKey(packet.BlockHash))
            _genomeIndex[packet.BlockHash] = new List<GeneEventPacket>();

        _genomeIndex[packet.BlockHash].Add(packet);
    }
}
```

**Cryptographic Utilities:**

```csharp
// Systems/Genetics/Blockchain/CryptographicHasher.cs
public static class CryptographicHasher
{
    public static string ComputeSHA256(string data)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            var hashBytes = sha256.ComputeHash(bytes);
            return ByteArrayToHexString(hashBytes);
        }
    }

    public static string ComputeGenomeHash(PlantGenotype genotype)
    {
        // Create deterministic string representation of genotype
        var sb = new StringBuilder();

        foreach (var trait in genotype.Traits.OrderBy(t => t.Key))
        {
            sb.Append($"{trait.Key}:");
            sb.Append($"{trait.Value.DominantAllele.GeneValue:F8}|");
            sb.Append($"{trait.Value.RecessiveAllele.GeneValue:F8};");
        }

        return ComputeSHA256(sb.ToString());
    }

    private static string ByteArrayToHexString(byte[] bytes)
    {
        var sb = new StringBuilder();
        foreach (var b in bytes)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
```

#### Day 3-5: Proof-of-Work & Mining System (Gameplay Integration)

**Genetic Calculation as Proof-of-Work:**

```csharp
// Systems/Genetics/Blockchain/GeneticProofOfWork.cs
public class GeneticProofOfWork
{
    private ComputeShader _geneticComputeShader;
    private const int THREAD_GROUP_SIZE = 64;

    public async Task<GeneEventPacket> MineBlockAsync(
        PlantGenotype parent1,
        PlantGenotype parent2,
        ulong mutationSeed,
        string breederSignature,
        string previousBlockHash)
    {
        var packet = new GeneEventPacket
        {
            PacketId = Guid.NewGuid().ToString(),
            ParentGenomeHash1 = CryptographicHasher.ComputeGenomeHash(parent1),
            ParentGenomeHash2 = CryptographicHasher.ComputeGenomeHash(parent2),
            MutationSeed = mutationSeed,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            BreederSignature = breederSignature,
            PreviousBlockHash = previousBlockHash,
            Nonce = 0
        };

        // GPU-accelerated proof-of-work mining
        var minedPacket = await MineWithGPUAsync(packet);

        ChimeraLogger.Log("BLOCKCHAIN",
            $"Block mined: {minedPacket.BlockHash} (nonce: {minedPacket.Nonce})", this);

        return minedPacket;
    }

    private async Task<GeneEventPacket> MineWithGPUAsync(GeneEventPacket packet)
    {
        // Prepare compute shader buffers
        var resultBuffer = new ComputeBuffer(1, sizeof(int) * 2); // [nonce, success]

        _geneticComputeShader.SetString("_ParentHash1", packet.ParentGenomeHash1);
        _geneticComputeShader.SetString("_ParentHash2", packet.ParentGenomeHash2);
        _geneticComputeShader.SetInt("_MutationSeed", (int)packet.MutationSeed);
        _geneticComputeShader.SetInt("_Timestamp", (int)packet.Timestamp);
        _geneticComputeShader.SetInt("_Difficulty", GeneticLedger.DIFFICULTY);
        _geneticComputeShader.SetBuffer(0, "_ResultBuffer", resultBuffer);

        // Dispatch compute shader (search in batches)
        var maxAttempts = 1000000;
        var batchSize = 10000;
        var kernelHandle = _geneticComputeShader.FindKernel("MineBlock");

        for (int startNonce = 0; startNonce < maxAttempts; startNonce += batchSize)
        {
            _geneticComputeShader.SetInt("_StartNonce", startNonce);
            _geneticComputeShader.Dispatch(kernelHandle, batchSize / THREAD_GROUP_SIZE, 1, 1);

            // Check if solution found
            var result = new int[2];
            resultBuffer.GetData(result);

            if (result[1] == 1) // Success flag
            {
                packet.Nonce = result[0];
                packet.BlockHash = packet.CalculateHash();

                resultBuffer.Release();
                return packet;
            }

            await Task.Yield(); // Allow other operations
        }

        resultBuffer.Release();
        throw new InvalidOperationException("Failed to mine block - max attempts reached");
    }

    // Fallback CPU mining for platforms without compute shader support
    private GeneEventPacket MineWithCPU(GeneEventPacket packet)
    {
        var nonce = 0;
        var difficulty = GeneticLedger.DIFFICULTY;

        while (true)
        {
            packet.Nonce = nonce;
            packet.BlockHash = packet.CalculateHash();

            if (packet.ValidateProofOfWork(difficulty))
                return packet;

            nonce++;

            if (nonce % 10000 == 0)
            {
                // Periodically yield to prevent blocking
                Thread.Sleep(1);
            }
        }
    }
}
```

**Compute Shader for GPU Mining:**

```hlsl
// Resources/Shaders/GeneticMining.compute
#pragma kernel MineBlock

// Input parameters
string _ParentHash1;
string _ParentHash2;
int _MutationSeed;
int _Timestamp;
int _Difficulty;
int _StartNonce;

// Output buffer: [nonce, success_flag]
RWStructuredBuffer<int> _ResultBuffer;

// SHA256 implementation (simplified for compute shader)
uint ComputeHash(string data)
{
    // Compute shader implementation of SHA256
    // For production, use optimized crypto library
    // This is a placeholder for the actual implementation
    return 0;
}

[numthreads(64,1,1)]
void MineBlock (uint3 id : SV_DispatchThreadID)
{
    int nonce = _StartNonce + id.x;

    // Construct data string
    // Format: ParentHash1 + ParentHash2 + MutationSeed + Timestamp + Nonce

    // Compute hash
    uint hash = ComputeHash(data);

    // Check if hash meets difficulty requirement
    // (Number of leading zeros in hash)
    int leadingZeros = 0;
    uint mask = 0xF0000000;

    for (int i = 0; i < 8; i++)
    {
        if ((hash & mask) == 0)
            leadingZeros++;
        else
            break;
        mask >>= 4;
    }

    if (leadingZeros >= _Difficulty)
    {
        // Solution found!
        InterlockedExchange(_ResultBuffer[0], nonce);
        InterlockedExchange(_ResultBuffer[1], 1);
    }
}
```

### Week 6: Witness Node System & Consensus

#### Day 1-3: Distributed Verification Network

**Witness Node Architecture:**

```csharp
// Systems/Genetics/Blockchain/WitnessNode.cs
public class WitnessNode : MonoBehaviour
{
    private GeneticLedger _localLedger;
    private List<WitnessNode> _peerNodes = new();
    private Queue<GeneEventPacket> _unverifiedPackets = new();

    public async Task<bool> ProposeBlock(GeneEventPacket packet)
    {
        // 1. Validate block locally
        if (!_localLedger.ValidateBlock(packet))
        {
            ChimeraLogger.LogWarning("WITNESS",
                $"Block {packet.BlockHash} failed local validation", this);
            return false;
        }

        // 2. Request verification from peer nodes
        var verificationRequests = _peerNodes
            .Select(peer => peer.VerifyBlockAsync(packet))
            .ToArray();

        var verifications = await Task.WhenAll(verificationRequests);

        // 3. Require consensus (>50% agreement)
        var approvals = verifications.Count(v => v);
        var consensusThreshold = _peerNodes.Count / 2 + 1;

        if (approvals >= consensusThreshold)
        {
            // Consensus achieved - add to chain
            _localLedger.AddBlock(packet);
            BroadcastNewBlock(packet);

            ChimeraLogger.Log("WITNESS",
                $"Block {packet.BlockHash} achieved consensus ({approvals}/{_peerNodes.Count})", this);

            return true;
        }
        else
        {
            ChimeraLogger.LogWarning("WITNESS",
                $"Block {packet.BlockHash} rejected - insufficient consensus ({approvals}/{_peerNodes.Count})", this);
            return false;
        }
    }

    public async Task<bool> VerifyBlockAsync(GeneEventPacket packet)
    {
        // Independent verification of block validity
        await Task.Delay(10); // Simulate network latency

        // Verify proof-of-work
        if (!packet.ValidateProofOfWork(GeneticLedger.DIFFICULTY))
            return false;

        // Verify hash integrity
        if (packet.BlockHash != packet.CalculateHash())
            return false;

        // Verify parent existence in local chain
        if (_localLedger.GetPacketByHash(packet.ParentGenomeHash1).BlockHash == null &&
            _localLedger.GetPacketByHash(packet.ParentGenomeHash2).BlockHash == null)
            return false;

        return true;
    }

    private void BroadcastNewBlock(GeneEventPacket packet)
    {
        // Notify all peer nodes of new verified block
        foreach (var peer in _peerNodes)
        {
            peer.ReceiveVerifiedBlock(packet);
        }
    }

    public void ReceiveVerifiedBlock(GeneEventPacket packet)
    {
        // Add verified block to local chain if not already present
        if (_localLedger.GetPacketByHash(packet.BlockHash).BlockHash == null)
        {
            _localLedger.AddBlock(packet);
        }
    }

    public void RegisterPeerNode(WitnessNode peer)
    {
        if (!_peerNodes.Contains(peer))
        {
            _peerNodes.Add(peer);
            ChimeraLogger.Log("WITNESS", $"Peer node registered: {peer.name}", this);
        }
    }
}
```

**Invisible Blockchain Integration (Gameplay-Driven):**

```csharp
// Systems/Genetics/BlockchainGeneticsIntegration.cs
public class BlockchainGeneticsIntegration : MonoBehaviour, IBlockchainGeneticsService
{
    private GeneticLedger _ledger;
    private GeneticProofOfWork _proofOfWork;
    private WitnessNode _witnessNode;
    private string _playerSignature;

    public async Task<PlantGenotype> BreedPlantsAsync(
        PlantGenotype parent1,
        PlantGenotype parent2,
        ulong mutationSeed)
    {
        // 1. Perform genetic breeding calculation (this IS the proof-of-work)
        var breedingStartTime = Time.realtimeSinceStartup;

        var previousBlockHash = _ledger.GetLatestBlockHash();

        // Mining happens transparently during breeding
        var geneEventPacket = await _proofOfWork.MineBlockAsync(
            parent1,
            parent2,
            mutationSeed,
            _playerSignature,
            previousBlockHash);

        var breedingDuration = Time.realtimeSinceStartup - breedingStartTime;

        ChimeraLogger.Log("GENETICS",
            $"Breeding completed in {breedingDuration:F2}s (includes blockchain mining)", this);

        // 2. Achieve consensus with witness nodes
        var consensusAchieved = await _witnessNode.ProposeBlock(geneEventPacket);

        if (!consensusAchieved)
        {
            throw new InvalidOperationException(
                "Breeding failed - blockchain consensus not achieved. This should be extremely rare.");
        }

        // 3. Generate offspring genotype from verified genetic event
        var offspringGenotype = GenerateOffspringFromPacket(parent1, parent2, geneEventPacket);

        // 4. Store genotype with blockchain verification
        offspringGenotype.BlockchainHash = geneEventPacket.BlockHash;
        offspringGenotype.IsVerified = true;

        return offspringGenotype;
    }

    private PlantGenotype GenerateOffspringFromPacket(
        PlantGenotype parent1,
        PlantGenotype parent2,
        GeneEventPacket packet)
    {
        // Use mutation seed from packet for deterministic generation
        var rng = new DeterministicPRNG(packet.MutationSeed);

        var offspring = new PlantGenotype
        {
            GenotypeId = Guid.NewGuid().ToString(),
            BlockchainHash = packet.BlockHash,
            ParentGenotype1 = parent1.GenotypeId,
            ParentGenotype2 = parent2.GenotypeId,
            Traits = new Dictionary<TraitType, AllelePair>()
        };

        // Mendelian inheritance with fractal variation
        foreach (TraitType traitType in System.Enum.GetValues(typeof(TraitType)))
        {
            var parent1Alleles = parent1.GetTrait(traitType);
            var parent2Alleles = parent2.GetTrait(traitType);

            // Randomly select one allele from each parent
            var p1Allele = rng.NextBool() ? parent1Alleles.DominantAllele : parent1Alleles.RecessiveAllele;
            var p2Allele = rng.NextBool() ? parent2Alleles.DominantAllele : parent2Alleles.RecessiveAllele;

            // Apply fractal mutation based on seed
            var mutationFactor = rng.NextFloat(-0.05f, 0.05f);
            p1Allele.GeneValue += mutationFactor;
            p2Allele.GeneValue += mutationFactor;

            offspring.SetTrait(traitType, new AllelePair(p1Allele, p2Allele));
        }

        return offspring;
    }

    public bool VerifyStrainAuthenticity(PlantGenotype genotype)
    {
        // Verify that the genotype's blockchain hash exists in the ledger
        var packet = _ledger.GetPacketByHash(genotype.BlockchainHash);

        if (packet.BlockHash == null)
            return false;

        // Verify lineage integrity
        return _ledger.ValidateChain();
    }

    public List<GeneEventPacket> GetStrainLineage(PlantGenotype genotype)
    {
        return _ledger.GetLineage(genotype.BlockchainHash);
    }
}
```

#### Day 4-5: Blockchain Visualization & Player Transparency

**Strain Verification UI:**

```csharp
// UI/Genetics/StrainVerificationPanel.cs
public class StrainVerificationPanel : MonoBehaviour
{
    [SerializeField] private Text _verificationStatusText;
    [SerializeField] private Text _blockchainHashText;
    [SerializeField] private Button _viewLineageButton;
    [SerializeField] private GameObject _lineageVisualizerPrefab;

    private IBlockchainGeneticsService _blockchainService;
    private PlantGenotype _currentGenotype;

    public void ShowVerification(PlantGenotype genotype)
    {
        _currentGenotype = genotype;

        var isVerified = _blockchainService.VerifyStrainAuthenticity(genotype);

        if (isVerified)
        {
            _verificationStatusText.text = "✅ VERIFIED STRAIN";
            _verificationStatusText.color = Color.green;
            _blockchainHashText.text = $"Blockchain ID: {genotype.BlockchainHash.Substring(0, 16)}...";
        }
        else
        {
            _verificationStatusText.text = "❌ UNVERIFIED";
            _verificationStatusText.color = Color.red;
            _blockchainHashText.text = "No blockchain record";
        }
    }

    public void OnViewLineageClicked()
    {
        var lineage = _blockchainService.GetStrainLineage(_currentGenotype);

        // Instantiate lineage visualizer
        var visualizer = Instantiate(_lineageVisualizerPrefab);
        var lineageViz = visualizer.GetComponent<LineageVisualizer>();
        lineageViz.DisplayLineage(lineage);
    }
}

// UI/Genetics/LineageVisualizer.cs
public class LineageVisualizer : MonoBehaviour
{
    [SerializeField] private Transform _lineageContainer;
    [SerializeField] private GameObject _lineageNodePrefab;

    public void DisplayLineage(List<GeneEventPacket> lineage)
    {
        // Clear existing nodes
        foreach (Transform child in _lineageContainer)
            Destroy(child.gameObject);

        // Create visual tree of lineage
        for (int i = 0; i < lineage.Count; i++)
        {
            var packet = lineage[i];
            var node = Instantiate(_lineageNodePrefab, _lineageContainer);
            var nodeUI = node.GetComponent<LineageNode>();

            nodeUI.SetData(
                generation: i + 1,
                blockHash: packet.BlockHash,
                timestamp: packet.Timestamp,
                isVerified: true
            );

            // Position in tree
            node.transform.localPosition = new Vector3(
                i % 3 * 150f,
                -i / 3 * 100f,
                0f
            );
        }
    }
}
```

### Week 7: Fractal Genetics Engine Enhancement

#### Day 1-3: True Fractal Mathematics Implementation

**Current State:** Simplified averaging (~40% accurate to vision)
**Goal:** Recursive fractal algorithms with harmonic interference

**Enhanced Fractal Genetics Engine:**

```csharp
// Systems/Genetics/EnhancedFractalGeneticsEngine.cs
public class EnhancedFractalGeneticsEngine : MonoBehaviour, IFractalGeneticsEngine
{
    private ComputeShader _fractalComputeShader;
    private const int MAX_FRACTAL_DEPTH = 8;
    private const float HARMONIC_INTERFERENCE_STRENGTH = 0.15f;

    public PlantGenotype GenerateOffspringWithFractalGenetics(
        PlantGenotype parent1,
        PlantGenotype parent2,
        ulong mutationSeed,
        EnvironmentalProfile environment)
    {
        var offspring = new PlantGenotype
        {
            GenotypeId = Guid.NewGuid().ToString(),
            ParentGenotype1 = parent1.GenotypeId,
            ParentGenotype2 = parent2.GenotypeId,
            Traits = new Dictionary<TraitType, AllelePair>()
        };

        // Process each trait with fractal genetics
        foreach (TraitType traitType in System.Enum.GetValues(typeof(TraitType)))
        {
            var traitConfig = GetTraitConfiguration(traitType);

            var offspringTrait = CalculateFractalTrait(
                parent1.GetTrait(traitType),
                parent2.GetTrait(traitType),
                traitConfig,
                mutationSeed,
                environment);

            offspring.SetTrait(traitType, offspringTrait);
        }

        return offspring;
    }

    private AllelePair CalculateFractalTrait(
        AllelePair parent1Alleles,
        AllelePair parent2Alleles,
        TraitConfiguration config,
        ulong mutationSeed,
        EnvironmentalProfile environment)
    {
        var rng = new DeterministicPRNG(mutationSeed);

        // 1. Mendelian selection
        var p1Selected = rng.NextBool() ? parent1Alleles.DominantAllele : parent1Alleles.RecessiveAllele;
        var p2Selected = rng.NextBool() ? parent2Alleles.DominantAllele : parent2Alleles.RecessiveAllele;

        // 2. Base inheritance value (midpoint with dominance)
        var baseValue = (p1Selected.GeneValue * p1Selected.Dominance +
                         p2Selected.GeneValue * p2Selected.Dominance) /
                        (p1Selected.Dominance + p2Selected.Dominance);

        // 3. Apply recursive fractal variation
        var fractalVariation = CalculateRecursiveFractalNoise(
            baseValue,
            config.Heritability,
            mutationSeed,
            depth: 0);

        // 4. Apply harmonic interference (sibling variation)
        var harmonicVariation = CalculateHarmonicInterference(
            parent1Alleles,
            parent2Alleles,
            config.VariationCoefficient,
            rng);

        // 5. Combine variations
        var finalValue = baseValue + fractalVariation + harmonicVariation;

        // 6. Apply environmental modulation (GxE)
        var gxeModifier = CalculateGxEModifier(finalValue, config, environment);
        finalValue *= gxeModifier;

        // 7. Clamp to valid range
        finalValue = Mathf.Clamp(finalValue, config.MinValue, config.MaxValue);

        // 8. Create offspring alleles (both alleles get slight variation)
        var dominantAllele = new Allele
        {
            GeneValue = finalValue,
            Dominance = Mathf.Lerp(p1Selected.Dominance, p2Selected.Dominance, 0.5f)
        };

        var recessiveVariation = rng.NextFloat(-config.VariationCoefficient, config.VariationCoefficient);
        var recessiveAllele = new Allele
        {
            GeneValue = finalValue + recessiveVariation,
            Dominance = 1.0f - dominantAllele.Dominance
        };

        return new AllelePair(dominantAllele, recessiveAllele);
    }

    private float CalculateRecursiveFractalNoise(
        float baseValue,
        float heritability,
        ulong seed,
        int depth)
    {
        if (depth >= MAX_FRACTAL_DEPTH)
            return 0f;

        var rng = new DeterministicPRNG(seed + (ulong)depth);

        // Fractal variation decreases with heritability and depth
        var variationStrength = (1.0f - heritability) * Mathf.Pow(0.5f, depth);

        // Generate variation at this depth
        var variation = rng.NextFloat(-variationStrength, variationStrength) * baseValue;

        // Recurse to next depth with modified seed
        var childVariation = CalculateRecursiveFractalNoise(
            baseValue,
            heritability,
            seed * 31 + (ulong)(depth + 1),
            depth + 1);

        return variation + childVariation * 0.5f; // Each level contributes less
    }

    private float CalculateHarmonicInterference(
        AllelePair parent1,
        AllelePair parent2,
        float variationCoefficient,
        DeterministicPRNG rng)
    {
        // Calculate genetic distance between parents
        var geneticDistance = Mathf.Abs(
            parent1.GetExpressedValue() - parent2.GetExpressedValue());

        // Harmonic interference strength based on genetic distance
        // More distant parents = more variation potential
        var interferenceStrength = geneticDistance * HARMONIC_INTERFERENCE_STRENGTH;

        // F2 generation diversity calibration (from cannabis research)
        var roll = rng.NextFloat(0f, 1f);

        float variationMultiplier;
        if (roll < 0.005f) // 0.5% exceptional variation
            variationMultiplier = 3.0f;
        else if (roll < 0.305f) // 30% significant variation
            variationMultiplier = 1.5f;
        else // 60% moderate variation (remaining 9.5% minimal)
            variationMultiplier = 0.8f;

        return rng.NextFloat(-interferenceStrength, interferenceStrength) *
               variationMultiplier * variationCoefficient;
    }

    private float CalculateGxEModifier(
        float geneticValue,
        TraitConfiguration config,
        EnvironmentalProfile environment)
    {
        // Environmental sensitivity varies by trait heritability
        // High heritability (e.g., CBD 96%) = low environmental impact
        // Low heritability (e.g., stress tolerance 40%) = high environmental impact

        var environmentalSensitivity = 1.0f - config.Heritability;

        // Calculate environmental stress/benefit
        var tempModifier = CalculateTemperatureModifier(environment.Temperature, config);
        var lightModifier = CalculateLightModifier(environment.LightIntensity, config);
        var humidityModifier = CalculateHumidityModifier(environment.Humidity, config);

        var combinedModifier = (tempModifier + lightModifier + humidityModifier) / 3.0f;

        // Blend genetic potential with environmental reality
        return Mathf.Lerp(1.0f, combinedModifier, environmentalSensitivity);
    }

    private TraitConfiguration GetTraitConfiguration(TraitType traitType)
    {
        // Research-calibrated trait parameters (2018-2023 cannabis studies)
        switch (traitType)
        {
            case TraitType.THC:
                return new TraitConfiguration
                {
                    Heritability = 0.89f,
                    VariationCoefficient = 0.12f,
                    MinValue = 0f,
                    MaxValue = 35f,
                    OptimalTemperature = 25f,
                    OptimalLight = 800f,
                    OptimalHumidity = 50f
                };

            case TraitType.CBD:
                return new TraitConfiguration
                {
                    Heritability = 0.96f,
                    VariationCoefficient = 0.08f,
                    MinValue = 0f,
                    MaxValue = 25f,
                    OptimalTemperature = 24f,
                    OptimalLight = 750f,
                    OptimalHumidity = 50f
                };

            case TraitType.Yield:
                return new TraitConfiguration
                {
                    Heritability = 0.47f,
                    VariationCoefficient = 0.25f,
                    MinValue = 0.1f,
                    MaxValue = 3.0f,
                    OptimalTemperature = 26f,
                    OptimalLight = 1000f,
                    OptimalHumidity = 55f
                };

            case TraitType.StressTolerance:
                return new TraitConfiguration
                {
                    Heritability = 0.40f,
                    VariationCoefficient = 0.35f,
                    MinValue = 0f,
                    MaxValue = 100f,
                    OptimalTemperature = 23f,
                    OptimalLight = 700f,
                    OptimalHumidity = 60f
                };

            // ... additional traits
            default:
                return TraitConfiguration.Default;
        }
    }
}

// Data/Genetics/TraitConfiguration.cs
[System.Serializable]
public struct TraitConfiguration
{
    public float Heritability;           // 0-1, how much genetics vs environment
    public float VariationCoefficient;   // Expected variation range
    public float MinValue;               // Trait minimum
    public float MaxValue;               // Trait maximum
    public float OptimalTemperature;     // °C
    public float OptimalLight;           // PPFD
    public float OptimalHumidity;        // %

    public static TraitConfiguration Default => new TraitConfiguration
    {
        Heritability = 0.70f,
        VariationCoefficient = 0.15f,
        MinValue = 0f,
        MaxValue = 100f,
        OptimalTemperature = 25f,
        OptimalLight = 800f,
        OptimalHumidity = 50f
    };
}
```

#### Day 4-5: Compute Shader Optimization for Fractal Calculations

**GPU-Accelerated Fractal Genetics:**

```hlsl
// Resources/Shaders/FractalGenetics.compute
#pragma kernel ComputeFractalTraits

struct AllelePair
{
    float dominantValue;
    float dominantDominance;
    float recessiveValue;
    float recessiveDominance;
};

struct TraitConfig
{
    float heritability;
    float variationCoefficient;
    float minValue;
    float maxValue;
    float optimalTemp;
    float optimalLight;
    float optimalHumidity;
};

struct EnvironmentData
{
    float temperature;
    float lightIntensity;
    float humidity;
    float co2;
};

// Input buffers
StructuredBuffer<AllelePair> _Parent1Traits;
StructuredBuffer<AllelePair> _Parent2Traits;
StructuredBuffer<TraitConfig> _TraitConfigs;
StructuredBuffer<EnvironmentData> _Environment;
StructuredBuffer<uint> _MutationSeeds;

// Output buffer
RWStructuredBuffer<AllelePair> _OffspringTraits;

// Constants
static const int MAX_FRACTAL_DEPTH = 8;
static const float HARMONIC_STRENGTH = 0.15;

// Deterministic random number generator
float Random(uint seed, uint index)
{
    uint n = seed * 747796405u + 2891336453u + index;
    n = ((n >> ((n >> 28u) + 4u)) ^ n) * 277803737u;
    return ((n >> 22u) ^ n) / 4294967295.0;
}

// Recursive fractal noise calculation
float CalculateFractalNoise(float baseValue, float heritability, uint seed, int depth)
{
    if (depth >= MAX_FRACTAL_DEPTH)
        return 0.0;

    float variationStrength = (1.0 - heritability) * pow(0.5, depth);
    float variation = (Random(seed, depth) * 2.0 - 1.0) * variationStrength * baseValue;

    // Recurse
    float childVariation = CalculateFractalNoise(
        baseValue,
        heritability,
        seed * 31u + depth + 1,
        depth + 1);

    return variation + childVariation * 0.5;
}

// Harmonic interference calculation
float CalculateHarmonicInterference(
    AllelePair parent1,
    AllelePair parent2,
    float variationCoeff,
    uint seed)
{
    float p1Expressed = (parent1.dominantValue * parent1.dominantDominance +
                         parent1.recessiveValue * parent1.recessiveDominance) /
                        (parent1.dominantDominance + parent1.recessiveDominance);

    float p2Expressed = (parent2.dominantValue * parent2.dominantDominance +
                         parent2.recessiveValue * parent2.recessiveDominance) /
                        (parent2.dominantDominance + parent2.recessiveDominance);

    float geneticDistance = abs(p1Expressed - p2Expressed);
    float interferenceStrength = geneticDistance * HARMONIC_STRENGTH;

    // F2 diversity calibration
    float roll = Random(seed, 100);
    float variationMultiplier = 0.8; // Default: moderate

    if (roll < 0.005)
        variationMultiplier = 3.0; // Exceptional
    else if (roll < 0.305)
        variationMultiplier = 1.5; // Significant

    return (Random(seed, 101) * 2.0 - 1.0) * interferenceStrength *
           variationMultiplier * variationCoeff;
}

// GxE modifier calculation
float CalculateGxEModifier(
    float geneticValue,
    TraitConfig config,
    EnvironmentData env)
{
    float envSensitivity = 1.0 - config.heritability;

    // Temperature response curve
    float tempDelta = abs(env.temperature - config.optimalTemp);
    float tempModifier = 1.0 - (tempDelta / 10.0) * 0.3; // 30% max impact

    // Light response curve
    float lightRatio = env.lightIntensity / config.optimalLight;
    float lightModifier = clamp(lightRatio, 0.3, 1.5);

    // Humidity response curve
    float humidityDelta = abs(env.humidity - config.optimalHumidity);
    float humidityModifier = 1.0 - (humidityDelta / 30.0) * 0.2; // 20% max impact

    float combinedModifier = (tempModifier + lightModifier + humidityModifier) / 3.0;

    return lerp(1.0, combinedModifier, envSensitivity);
}

[numthreads(8,1,1)]
void ComputeFractalTraits(uint3 id : SV_DispatchThreadID)
{
    uint traitIndex = id.x;

    AllelePair p1 = _Parent1Traits[traitIndex];
    AllelePair p2 = _Parent2Traits[traitIndex];
    TraitConfig config = _TraitConfigs[traitIndex];
    EnvironmentData env = _Environment[0];
    uint seed = _MutationSeeds[traitIndex];

    // Mendelian selection
    float p1Selected = Random(seed, 0) > 0.5 ? p1.dominantValue : p1.recessiveValue;
    float p1Dominance = Random(seed, 0) > 0.5 ? p1.dominantDominance : p1.recessiveDominance;
    float p2Selected = Random(seed, 1) > 0.5 ? p2.dominantValue : p2.recessiveValue;
    float p2Dominance = Random(seed, 1) > 0.5 ? p2.dominantDominance : p2.recessiveDominance;

    // Base value with dominance
    float baseValue = (p1Selected * p1Dominance + p2Selected * p2Dominance) /
                      (p1Dominance + p2Dominance);

    // Apply fractal variation
    float fractalVariation = CalculateFractalNoise(
        baseValue,
        config.heritability,
        seed,
        0);

    // Apply harmonic interference
    float harmonicVariation = CalculateHarmonicInterference(
        p1, p2,
        config.variationCoefficient,
        seed);

    // Combine
    float finalValue = baseValue + fractalVariation + harmonicVariation;

    // Apply GxE
    float gxeModifier = CalculateGxEModifier(finalValue, config, env);
    finalValue *= gxeModifier;

    // Clamp
    finalValue = clamp(finalValue, config.minValue, config.maxValue);

    // Create offspring alleles
    AllelePair offspring;
    offspring.dominantValue = finalValue;
    offspring.dominantDominance = lerp(p1Dominance, p2Dominance, 0.5);
    offspring.recessiveValue = finalValue + (Random(seed, 2) * 2.0 - 1.0) * config.variationCoefficient;
    offspring.recessiveDominance = 1.0 - offspring.dominantDominance;

    _OffspringTraits[traitIndex] = offspring;
}
```

---

## WEEK 6-7: CONSTRUCTION UTILITIES IMPLEMENTATION

**Current Gap:** Electricity, water, and HVAC systems are missing from construction

### Day 1-2: Electricity System

**Electrical Infrastructure:**

```csharp
// Systems/Construction/Utilities/ElectricalSystem.cs
public class ElectricalSystem : MonoBehaviour, IElectricalSystem
{
    private Dictionary<Vector3Int, ElectricalNode> _nodes = new();
    private Dictionary<Vector3Int, WireSegment> _wires = new();
    private float _totalCapacity = 100f; // kW
    private float _currentLoad = 0f;

    public bool CanAddEquipment(IElectricalDevice device)
    {
        return (_currentLoad + device.PowerDraw) <= _totalCapacity;
    }

    public bool ConnectDevice(IElectricalDevice device, Vector3Int position)
    {
        if (!CanAddEquipment(device))
        {
            ChimeraLogger.LogWarning("ELECTRICAL",
                $"Insufficient capacity for {device.DeviceName} ({device.PowerDraw}kW). Available: {_totalCapacity - _currentLoad}kW", this);
            return false;
        }

        // Find nearest electrical node
        var nearestNode = FindNearestNode(position);

        if (nearestNode == null)
        {
            ChimeraLogger.LogWarning("ELECTRICAL",
                $"No electrical node in range for {device.DeviceName}", this);
            return false;
        }

        // Create wire connection
        var wire = CreateWireConnection(nearestNode.Position, position);
        _wires[position] = wire;

        // Add to load
        _currentLoad += device.PowerDraw;
        device.Connect(nearestNode);

        ChimeraLogger.Log("ELECTRICAL",
            $"Connected {device.DeviceName}. Load: {_currentLoad}/{_totalCapacity}kW ({(_currentLoad / _totalCapacity * 100f):F1}%)", this);

        return true;
    }

    public void PlaceElectricalNode(Vector3Int position, float capacity)
    {
        var node = new ElectricalNode
        {
            Position = position,
            Capacity = capacity,
            ConnectedDevices = new List<IElectricalDevice>()
        };

        _nodes[position] = node;
        _totalCapacity += capacity;
    }

    public void PlaceWire(Vector3Int start, Vector3Int end)
    {
        // Calculate wire path
        var path = CalculateWirePath(start, end);

        foreach (var segment in path)
        {
            var wire = new WireSegment
            {
                Start = segment.Start,
                End = segment.End,
                Capacity = 10f // kW per wire
            };

            _wires[segment.Start] = wire;
        }
    }

    private ElectricalNode FindNearestNode(Vector3Int position)
    {
        ElectricalNode nearest = null;
        float minDistance = float.MaxValue;

        foreach (var node in _nodes.Values)
        {
            var distance = Vector3Int.Distance(position, node.Position);
            if (distance < minDistance && distance <= 10f) // Max 10 units
            {
                minDistance = distance;
                nearest = node;
            }
        }

        return nearest;
    }

    private WireSegment CreateWireConnection(Vector3Int from, Vector3Int to)
    {
        return new WireSegment
        {
            Start = from,
            End = to,
            Capacity = 10f
        };
    }

    public float GetCurrentLoad() => _currentLoad;
    public float GetTotalCapacity() => _totalCapacity;
    public float GetLoadPercentage() => (_currentLoad / _totalCapacity) * 100f;
}

// Data/Construction/ElectricalData.cs
public struct ElectricalNode
{
    public Vector3Int Position;
    public float Capacity; // kW
    public List<IElectricalDevice> ConnectedDevices;
}

public struct WireSegment
{
    public Vector3Int Start;
    public Vector3Int End;
    public float Capacity; // kW
}

public interface IElectricalDevice
{
    string DeviceName { get; }
    float PowerDraw { get; } // kW
    void Connect(ElectricalNode node);
    void Disconnect();
    bool IsConnected { get; }
}
```

### Day 3-4: Water/Plumbing System

**Plumbing Infrastructure:**

```csharp
// Systems/Construction/Utilities/PlumbingSystem.cs
public class PlumbingSystem : MonoBehaviour, IPlumbingSystem
{
    private Dictionary<Vector3Int, WaterNode> _waterNodes = new();
    private Dictionary<Vector3Int, PipeSegment> _pipes = new();
    private float _waterPressure = 60f; // PSI
    private float _flowRate = 0f; // GPM (gallons per minute)

    public bool ConnectIrrigation(IWaterDevice device, Vector3Int position)
    {
        var nearestNode = FindNearestWaterNode(position);

        if (nearestNode == null)
        {
            ChimeraLogger.LogWarning("PLUMBING",
                $"No water source in range for {device.DeviceName}", this);
            return false;
        }

        // Check pressure requirements
        if (nearestNode.Pressure < device.RequiredPressure)
        {
            ChimeraLogger.LogWarning("PLUMBING",
                $"Insufficient pressure for {device.DeviceName}. Required: {device.RequiredPressure}PSI, Available: {nearestNode.Pressure}PSI", this);
            return false;
        }

        // Create pipe connection
        var pipe = CreatePipeConnection(nearestNode.Position, position);
        _pipes[position] = pipe;

        device.Connect(nearestNode);
        _flowRate += device.FlowRate;

        ChimeraLogger.Log("PLUMBING",
            $"Connected {device.DeviceName}. Total flow rate: {_flowRate}GPM", this);

        return true;
    }

    public void PlaceWaterNode(Vector3Int position, float pressure, float capacity)
    {
        var node = new WaterNode
        {
            Position = position,
            Pressure = pressure,
            Capacity = capacity,
            ConnectedDevices = new List<IWaterDevice>()
        };

        _waterNodes[position] = node;
    }

    public void PlacePipe(Vector3Int start, Vector3Int end, float diameter)
    {
        var path = CalculatePipePath(start, end);

        foreach (var segment in path)
        {
            var pipe = new PipeSegment
            {
                Start = segment.Start,
                End = segment.End,
                Diameter = diameter,
                MaxFlowRate = CalculateMaxFlowRate(diameter)
            };

            _pipes[segment.Start] = pipe;
        }
    }

    private float CalculateMaxFlowRate(float diameter)
    {
        // Pipe flow rate based on diameter (simplified)
        // 0.5" = 5 GPM, 1.0" = 20 GPM, 2.0" = 80 GPM
        return Mathf.Pow(diameter / 0.5f, 2) * 5f;
    }

    private WaterNode FindNearestWaterNode(Vector3Int position)
    {
        WaterNode nearest = null;
        float minDistance = float.MaxValue;

        foreach (var node in _waterNodes.Values)
        {
            var distance = Vector3Int.Distance(position, node.Position);
            if (distance < minDistance && distance <= 15f)
            {
                minDistance = distance;
                nearest = node;
            }
        }

        return nearest;
    }
}

// Data/Construction/PlumbingData.cs
public struct WaterNode
{
    public Vector3Int Position;
    public float Pressure; // PSI
    public float Capacity; // Gallons
    public List<IWaterDevice> ConnectedDevices;
}

public struct PipeSegment
{
    public Vector3Int Start;
    public Vector3Int End;
    public float Diameter; // inches
    public float MaxFlowRate; // GPM
}

public interface IWaterDevice
{
    string DeviceName { get; }
    float RequiredPressure { get; } // PSI
    float FlowRate { get; } // GPM
    void Connect(WaterNode node);
    void Disconnect();
    bool IsConnected { get; }
}
```

### Day 5: HVAC Integration

**HVAC System Integration with Environmental Simulation:**

```csharp
// Systems/Construction/Utilities/HVACIntegration.cs
public class HVACIntegration : MonoBehaviour, IHVACSystem
{
    private Dictionary<string, HVACZone> _zones = new();
    private Dictionary<Vector3Int, HVACUnit> _units = new();

    private ICultivationEnvironmentalController _environmentController;

    public void CreateZone(string zoneId, List<Vector3Int> coverage)
    {
        var zone = new HVACZone
        {
            ZoneId = zoneId,
            Coverage = coverage,
            TargetTemperature = 25f,
            TargetHumidity = 50f,
            CurrentTemperature = 20f,
            CurrentHumidity = 45f
        };

        _zones[zoneId] = zone;

        ChimeraLogger.Log("HVAC",
            $"Created HVAC zone: {zoneId} covering {coverage.Count} cells", this);
    }

    public void PlaceHVACUnit(Vector3Int position, HVACUnitType type, string assignedZone)
    {
        var unit = new HVACUnit
        {
            Position = position,
            Type = type,
            AssignedZone = assignedZone,
            Capacity = GetUnitCapacity(type),
            PowerDraw = GetUnitPowerDraw(type),
            IsActive = false
        };

        _units[position] = unit;

        ChimeraLogger.Log("HVAC",
            $"Placed {type} unit at {position} for zone {assignedZone}", this);
    }

    public void UpdateZones(float deltaTime)
    {
        foreach (var zone in _zones.Values)
        {
            // Get active units for this zone
            var zoneUnits = _units.Values.Where(u => u.AssignedZone == zone.ZoneId && u.IsActive);

            // Calculate total heating/cooling capacity
            float totalHeating = zoneUnits.Where(u => u.Type == HVACUnitType.Heater)
                .Sum(u => u.Capacity);
            float totalCooling = zoneUnits.Where(u => u.Type == HVACUnitType.AirConditioner)
                .Sum(u => u.Capacity);
            float totalHumidification = zoneUnits.Where(u => u.Type == HVACUnitType.Humidifier)
                .Sum(u => u.Capacity);
            float totalDehumidification = zoneUnits.Where(u => u.Type == HVACUnitType.Dehumidifier)
                .Sum(u => u.Capacity);

            // Update zone temperature
            var tempDelta = zone.TargetTemperature - zone.CurrentTemperature;
            if (tempDelta > 0)
                zone.CurrentTemperature += (totalHeating * deltaTime * 0.1f);
            else
                zone.CurrentTemperature -= (totalCooling * deltaTime * 0.1f);

            // Update zone humidity
            var humidityDelta = zone.TargetHumidity - zone.CurrentHumidity;
            if (humidityDelta > 0)
                zone.CurrentHumidity += (totalHumidification * deltaTime * 0.05f);
            else
                zone.CurrentHumidity -= (totalDehumidification * deltaTime * 0.05f);

            // Push environmental data to cultivation system
            UpdateEnvironmentalController(zone);
        }
    }

    private void UpdateEnvironmentalController(HVACZone zone)
    {
        // Update environmental data for plants in this zone
        foreach (var position in zone.Coverage)
        {
            _environmentController.SetZoneEnvironment(position, new EnvironmentData
            {
                Temperature = zone.CurrentTemperature,
                Humidity = zone.CurrentHumidity,
                // Other environmental factors set elsewhere (lights, CO2, etc.)
            });
        }
    }

    private float GetUnitCapacity(HVACUnitType type)
    {
        return type switch
        {
            HVACUnitType.Heater => 5000f, // BTU
            HVACUnitType.AirConditioner => 12000f, // BTU
            HVACUnitType.Humidifier => 50f, // GPD (gallons per day)
            HVACUnitType.Dehumidifier => 70f, // Pints per day
            _ => 0f
        };
    }

    private float GetUnitPowerDraw(HVACUnitType type)
    {
        return type switch
        {
            HVACUnitType.Heater => 1.5f, // kW
            HVACUnitType.AirConditioner => 3.5f, // kW
            HVACUnitType.Humidifier => 0.5f, // kW
            HVACUnitType.Dehumidifier => 0.8f, // kW
            _ => 0f
        };
    }
}
```

---

## SUCCESS METRICS - WEEK 5-7

**Blockchain Genetics:**
- ✅ Genetic ledger operational with consensus
- ✅ Proof-of-work mining <1 second (GPU) or <5 seconds (CPU)
- ✅ 100% strain verification for all bred plants
- ✅ Lineage tracking functional
- ✅ True fractal mathematics implemented
- ✅ Research-calibrated trait heritability (89% THC, 96% CBD, 47% Yield, 40% Stress)

**Construction Utilities:**
- ✅ Electricity system: capacity planning, wire routing
- ✅ Plumbing system: pressure calculation, pipe routing
- ✅ HVAC integration: zone management, environmental control
- ✅ All utilities integrated with grid placement
- ✅ Cost calculation for utility installation

---

*End of Part 2: Phase 1 Core Systems (Weeks 5-7)*
*Continue to Part 3: Three Pillars Implementation (Weeks 8-10)*
