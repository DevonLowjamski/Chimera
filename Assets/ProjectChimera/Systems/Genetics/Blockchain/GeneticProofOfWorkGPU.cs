using System;
using System.Threading.Tasks;
using UnityEngine;
using ProjectChimera.Data.Genetics.Blockchain;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Genetics.Blockchain
{
    /// <summary>
    /// GPU-accelerated proof-of-work mining for genetic breeding.
    /// Makes breeding feel INSTANT in gameplay (<0.1 second vs 0.5-2 seconds CPU).
    ///
    /// GAMEPLAY IMPACT:
    /// Before (CPU): Player clicks "Breed" → waits 1-2 seconds → offspring appears
    /// After (GPU):  Player clicks "Breed" → brief animation → offspring appears instantly
    ///
    /// TECHNICAL APPROACH:
    /// - Uses compute shader to test 65,536 nonces in parallel
    /// - Searches batches until valid nonce found
    /// - Falls back to CPU if GPU unavailable (mobile, old hardware)
    ///
    /// VIDEO GAME FIRST:
    /// Player never knows this is happening - they just experience fast, responsive breeding.
    /// The blockchain runs invisibly at GPU speed!
    /// </summary>
    public class GeneticProofOfWorkGPU : MonoBehaviour
    {
        [Header("GPU Configuration")]
        [SerializeField] private ComputeShader _miningComputeShader;
        [SerializeField] private int _threadsPerGroup = 256;
        [SerializeField] private int _maxBatches = 1000;
        [SerializeField] private bool _enableGPUMining = true;

        [Header("Performance Monitoring")]
        [SerializeField] private bool _logPerformanceMetrics = true;

        private int _mineBlockKernel = -1;
        private ComputeBuffer _resultBuffer;

        private bool _isInitialized = false;

        /// <summary>
        /// Initialize GPU mining system.
        /// Called automatically on first use.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
                return;

            // Check if compute shaders are supported
            if (!SystemInfo.supportsComputeShaders)
            {
                ChimeraLogger.LogWarning("BLOCKCHAIN",
                    "⚠️ Compute shaders not supported - GPU mining disabled (using CPU fallback)", this);
                _enableGPUMining = false;
                _isInitialized = true;
                return;
            }

            // Load compute shader if not assigned
            if (_miningComputeShader == null)
            {
                // Fallback: Compute shaders not supported by IAssetManager/Addressables (legitimate exception)
                _miningComputeShader = Resources.Load<ComputeShader>("Shaders/GeneticProofOfWork"); // Fallback to Resources

                if (_miningComputeShader == null)
                {
                    ChimeraLogger.LogWarning("BLOCKCHAIN",
                        "⚠️ Mining compute shader not found - GPU mining disabled (using CPU fallback)", this);
                    _enableGPUMining = false;
                    _isInitialized = true;
                    return;
                }
            }

            // Find kernel
            _mineBlockKernel = _miningComputeShader.FindKernel("MineBlock");

            if (_mineBlockKernel < 0)
            {
                ChimeraLogger.LogWarning("BLOCKCHAIN",
                    "⚠️ MineBlock kernel not found - GPU mining disabled", this);
                _enableGPUMining = false;
                _isInitialized = true;
                return;
            }

            // Create result buffer (2 uints: [nonce, success_flag])
            _resultBuffer = new ComputeBuffer(2, sizeof(uint));

            _isInitialized = true;

            ChimeraLogger.Log("BLOCKCHAIN",
                "🚀 GPU mining initialized - breeding will feel instant!", this);
        }

        /// <summary>
        /// Mines a breeding event block using GPU acceleration.
        ///
        /// GAMEPLAY PERFORMANCE TARGET: <0.1 second (instant feel)
        ///
        /// TECHNICAL DETAILS:
        /// - Dispatches 256 thread groups × 256 threads = 65,536 parallel searches per batch
        /// - Each batch tests 65,536 nonce values simultaneously
        /// - Typical success: 1-5 batches (0.01-0.05 seconds)
        ///
        /// Returns: Mined packet with valid nonce
        /// </summary>
        public async Task<GeneEventPacket> MineBlockAsync(GeneEventPacket packet, int difficulty)
        {
            if (!_isInitialized)
                Initialize();

            // Fall back to CPU if GPU unavailable
            if (!_enableGPUMining || _miningComputeShader == null)
            {
                return await MineBlockCPUAsync(packet, difficulty);
            }

            var startTime = Time.realtimeSinceStartup;
            int totalAttempts = 0;
            int batchCount = 0;

            try
            {
                // Prepare hash data for compute shader
                var parentHash1 = CryptographicHasher.HexStringToByteArray(packet.ParentGenomeHash1);
                var parentHash2 = CryptographicHasher.HexStringToByteArray(packet.ParentGenomeHash2);

                // Convert to uint arrays for shader
                uint[] hash1Uints = BytesToUints(parentHash1);
                uint[] hash2Uints = BytesToUints(parentHash2);

                // Set shader parameters
                _miningComputeShader.SetInts("_ParentHash1", Array.ConvertAll(hash1Uints, x => (int)x));
                _miningComputeShader.SetInts("_ParentHash2", Array.ConvertAll(hash2Uints, x => (int)x));
                _miningComputeShader.SetInt("_MutationSeed", (int)packet.MutationSeed);
                _miningComputeShader.SetInt("_Timestamp", (int)packet.Timestamp);
                _miningComputeShader.SetInt("_Difficulty", difficulty);
                _miningComputeShader.SetBuffer(_mineBlockKernel, "_ResultBuffer", _resultBuffer);

                // Search in batches of 65,536 nonces
                int batchSize = _threadsPerGroup * _threadsPerGroup; // 256 * 256 = 65,536

                for (int batch = 0; batch < _maxBatches; batch++)
                {
                    batchCount = batch + 1;
                    int startNonce = batch * batchSize;

                    // Reset result buffer
                    uint[] resetData = new uint[] { 0, 0 };
                    _resultBuffer.SetData(resetData);

                    // Set start nonce for this batch
                    _miningComputeShader.SetInt("_StartNonce", startNonce);

                    // Dispatch GPU threads (256 groups of 256 threads)
                    _miningComputeShader.Dispatch(_mineBlockKernel, _threadsPerGroup, 1, 1);

                    // Read result
                    uint[] result = new uint[2];
                    _resultBuffer.GetData(result);

                    totalAttempts += batchSize;

                    // Check if valid nonce found
                    if (result[1] == 1) // Success flag
                    {
                        packet.Nonce = (int)result[0];
                        packet.BlockHash = packet.CalculateHash();

                        var duration = Time.realtimeSinceStartup - startTime;
                        var hashRate = totalAttempts / duration;

                        if (_logPerformanceMetrics)
                        {
                            ChimeraLogger.Log("BLOCKCHAIN",
                                $"⚡ GPU mining complete: {duration * 1000:F1}ms ({batchCount} batches, {hashRate / 1000000:F1}M H/s)", this);
                        }

                        return packet;
                    }

                    // Yield every few batches to keep gameplay responsive
                    if (batch % 5 == 0)
                        await Task.Yield();
                }

                // Max batches reached without finding solution (extremely rare)
                throw new InvalidOperationException(
                    $"GPU mining failed after {totalAttempts:N0} attempts ({_maxBatches} batches). " +
                    "This is extremely rare - consider lowering difficulty.");
            }
            catch (Exception ex)
            {
                var duration = Time.realtimeSinceStartup - startTime;

                ChimeraLogger.LogError("BLOCKCHAIN",
                    $"❌ GPU mining error after {duration:F2}s: {ex.Message}. Falling back to CPU...", this);

                // Fall back to CPU mining
                return await MineBlockCPUAsync(packet, difficulty);
            }
        }

        /// <summary>
        /// CPU fallback mining for compatibility.
        /// Used when: GPU unavailable, compute shaders unsupported, or GPU mining fails.
        ///
        /// PERFORMANCE: 0.5-2 seconds (slower but reliable)
        /// GAMEPLAY: Still acceptable - player sees brief "Breeding..." message
        /// </summary>
        private async Task<GeneEventPacket> MineBlockCPUAsync(GeneEventPacket packet, int difficulty)
        {
            var startTime = Time.realtimeSinceStartup;
            int attempts = 0;
            int maxAttempts = 10000000; // 10 million (should be plenty)

            ChimeraLogger.Log("BLOCKCHAIN",
                "🔄 Using CPU mining (GPU unavailable)", this);

            while (attempts < maxAttempts)
            {
                packet.Nonce = attempts;
                packet.BlockHash = packet.CalculateHash();

                if (packet.ValidateProofOfWork(difficulty))
                {
                    var duration = Time.realtimeSinceStartup - startTime;
                    var hashRate = attempts / duration;

                    if (_logPerformanceMetrics)
                    {
                        ChimeraLogger.Log("BLOCKCHAIN",
                            $"⛏️ CPU mining complete: {duration:F2}s ({attempts:N0} attempts, {hashRate / 1000:F1}K H/s)", this);
                    }

                    return packet;
                }

                attempts++;

                // Yield every 10,000 attempts to keep UI responsive
                if (attempts % 10000 == 0)
                    await Task.Yield();
            }

            throw new InvalidOperationException(
                $"CPU mining failed after {maxAttempts:N0} attempts. " +
                "This should never happen - check difficulty setting.");
        }

        /// <summary>
        /// Converts byte array to uint array for compute shader.
        /// SHA-256 hashes are 256 bits = 32 bytes = 8 uints
        /// </summary>
        private uint[] BytesToUints(byte[] bytes)
        {
            if (bytes == null || bytes.Length != 32)
            {
                // Return zero hash if invalid
                return new uint[8];
            }

            uint[] uints = new uint[8];
            for (int i = 0; i < 8; i++)
            {
                // Convert 4 bytes to 1 uint (big-endian)
                uints[i] = (uint)(
                    (bytes[i * 4] << 24) |
                    (bytes[i * 4 + 1] << 16) |
                    (bytes[i * 4 + 2] << 8) |
                    bytes[i * 4 + 3]);
            }

            return uints;
        }

        /// <summary>
        /// Estimates mining time based on difficulty and GPU performance.
        /// Used for UI "estimated time" display.
        ///
        /// GAMEPLAY: Could show "Breeding: ~2 seconds" or progress bar
        /// </summary>
        public float EstimateMiningTime(int difficulty)
        {
            if (_enableGPUMining)
            {
                // GPU: Typically 0.01-0.1 seconds
                // Difficulty 4 ≈ 0.05s, Difficulty 5 ≈ 0.5s, Difficulty 6 ≈ 5s
                return Mathf.Pow(16f, difficulty - 4) * 0.05f;
            }
            else
            {
                // CPU: Typically 0.5-5 seconds
                // Difficulty 4 ≈ 1s, Difficulty 5 ≈ 16s (too slow for gameplay)
                return Mathf.Pow(16f, difficulty - 4) * 1.0f;
            }
        }

        /// <summary>
        /// Checks if GPU mining is available and enabled.
        /// </summary>
        public bool IsGPUMiningAvailable()
        {
            if (!_isInitialized)
                Initialize();

            return _enableGPUMining && _miningComputeShader != null;
        }

        /// <summary>
        /// Gets current mining performance stats for UI/debugging.
        /// </summary>
        public string GetPerformanceInfo()
        {
            if (!_isInitialized)
                return "Not initialized";

            if (_enableGPUMining)
            {
                return $"GPU Mining: {_threadsPerGroup * _threadsPerGroup:N0} parallel threads";
            }
            else
            {
                return "CPU Mining: Sequential (GPU unavailable)";
            }
        }

        private void OnDestroy()
        {
            // Clean up compute buffer
            if (_resultBuffer != null)
            {
                _resultBuffer.Release();
                _resultBuffer = null;
            }
        }
    }
}
