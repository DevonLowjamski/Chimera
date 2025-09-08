using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Save;
using ProjectChimera.Systems.Save.Components;
using ProjectChimera.Systems.Save.Storage;
using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HashAlgorithmType = ProjectChimera.Data.Save.HashAlgorithmType;
using StorageDataResult = ProjectChimera.Data.Save.StorageDataResult;
using StorageResult = ProjectChimera.Data.Save.StorageResult;
using SystemCompressionLevel = System.IO.Compression.CompressionLevel;
using CompressionAlgorithm = ProjectChimera.Systems.Save.Storage.CompressionAlgorithm;

namespace ProjectChimera.Systems.Save
{
    /// <summary>
    /// Implementation for data serialization, compression, and validation
    /// </summary>
    public class SerializationHelpers : ISerializationHelpers
    {
        private bool _enableCompression = true;
        private SystemCompressionLevel _compressionLevel = SystemCompressionLevel.Optimal;
        private CompressionAlgorithm _compressionAlgorithm = CompressionAlgorithm.GZip;
        private float _compressionThreshold = 0.1f;

        private bool _enableIntegrityChecking = true;
        private HashAlgorithmType _hashAlgorithm = HashAlgorithmType.SHA256;

        private StorageCompressionService _compressionService;
        private StorageValidationService _validationService;
        private bool _isInitialized = false;

        public void Initialize(bool enableCompression, SystemCompressionLevel compressionLevel,
                             CompressionAlgorithm compressionAlgorithm, bool enableIntegrityChecking,
                             HashAlgorithmType hashAlgorithm)
        {
            _enableCompression = enableCompression;
            _compressionLevel = compressionLevel;
            _compressionAlgorithm = compressionAlgorithm;
            _enableIntegrityChecking = enableIntegrityChecking;
            _hashAlgorithm = hashAlgorithm;

            InitializeServices();
            _isInitialized = true;

            ChimeraLogger.Log("[SerializationHelpers] Serialization helpers initialized");
        }

        public void Shutdown()
        {
            _compressionService = null;
            _validationService = null;
            _isInitialized = false;

            ChimeraLogger.Log("[SerializationHelpers] Serialization helpers shutdown");
        }

        public async Task<byte[]> CompressDataAsync(byte[] data)
        {
            if (!_enableCompression || !_isInitialized || data == null || data.Length == 0)
            {
                return data;
            }

            try
            {
                if (!ShouldCompress(data))
                {
                    return data;
                }

                byte[] compressedData = null;

                switch (_compressionAlgorithm)
                {
                    case ProjectChimera.Systems.Save.Storage.CompressionAlgorithm.GZip:
                        compressedData = await CompressWithGZipAsync(data);
                        break;
                    case ProjectChimera.Systems.Save.Storage.CompressionAlgorithm.Deflate:
                        compressedData = await CompressWithDeflateAsync(data);
                        break;
                    case ProjectChimera.Systems.Save.Storage.CompressionAlgorithm.Brotli:
                        compressedData = await CompressWithBrotliAsync(data);
                        break;
                    default:
                        return data;
                }

                // Only return compressed data if it's actually smaller
                if (compressedData != null && compressedData.Length < data.Length)
                {
                    ChimeraLogger.Log($"[SerializationHelpers] Compressed {data.Length} bytes to {compressedData.Length} bytes ({CalculateCompressionRatio(data, compressedData):P1} ratio)");
                    return compressedData;
                }

                return data;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[SerializationHelpers] Compression failed: {ex.Message}");
                return data; // Return original data if compression fails
            }
        }

        public async Task<byte[]> DecompressDataAsync(byte[] compressedData)
        {
            if (!_enableCompression || !_isInitialized || compressedData == null || compressedData.Length == 0)
            {
                return compressedData;
            }

            try
            {
                // Try to detect compression type and decompress accordingly
                var decompressedData = await TryDecompressAsync(compressedData);

                if (decompressedData != null)
                {
                    ChimeraLogger.Log($"[SerializationHelpers] Decompressed {compressedData.Length} bytes to {decompressedData.Length} bytes");
                    return decompressedData;
                }

                // If decompression fails, assume data wasn't compressed
                return compressedData;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[SerializationHelpers] Decompression failed: {ex.Message}");
                return compressedData; // Return original data if decompression fails
            }
        }

        public bool ShouldCompress(byte[] data)
        {
            if (data == null || data.Length < 1024) // Don't compress very small files
            {
                return false;
            }

            // Simple entropy check - if data has low entropy (many repeating bytes), compress it
            var entropy = CalculateEntropy(data);
            return entropy < _compressionThreshold;
        }

        public float CalculateCompressionRatio(byte[] originalData, byte[] compressedData)
        {
            if (originalData == null || compressedData == null || originalData.Length == 0)
            {
                return 1.0f;
            }

            return (float)compressedData.Length / originalData.Length;
        }

        public async Task<ProjectChimera.Data.Save.ValidationResult> ValidateDataAsync(byte[] data)
        {
            if (!_enableIntegrityChecking || !_isInitialized)
            {
                return new ProjectChimera.Data.Save.ValidationResult { IsValid = true };
            }

            try
            {
                if (data == null || data.Length == 0)
                {
                    return new ProjectChimera.Data.Save.ValidationResult { IsValid = false, Message = "No data to validate" };
                }

                // Perform basic validation checks
                var result = new ProjectChimera.Data.Save.ValidationResult { IsValid = true };

                // Check for common corruption patterns
                if (IsDataCorrupted(data))
                {
                    result.IsValid = false;
                    result.Message = "Data corruption detected";
                    return result;
                }

                // Calculate hash for integrity  
                var hash = await CalculateHashAsync(data);
                result.Message = "Data validation successful";

                ChimeraLogger.Log($"[SerializationHelpers] Data validation successful - Hash: {hash.Substring(0, 8)}...");
                return result;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[SerializationHelpers] Data validation failed: {ex.Message}");
                return new ProjectChimera.Data.Save.ValidationResult { IsValid = false, Message = ex.Message };
            }
        }

        public async Task<string> CalculateHashAsync(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return string.Empty;
            }

            try
            {
                using var hashAlgorithm = CreateHashAlgorithm();
                var hashBytes = await Task.Run(() => hashAlgorithm.ComputeHash(data));
                return Convert.ToBase64String(hashBytes);
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[SerializationHelpers] Hash calculation failed: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task<bool> VerifyHashAsync(byte[] data, string expectedHash)
        {
            if (string.IsNullOrEmpty(expectedHash))
            {
                return true; // No hash to verify against
            }

            try
            {
                var actualHash = await CalculateHashAsync(data);
                return string.Equals(actualHash, expectedHash, StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[SerializationHelpers] Hash verification failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CheckDataIntegrityAsync(string filePath)
        {
            if (!_enableIntegrityChecking || !File.Exists(filePath))
            {
                return true; // Assume valid if checking is disabled or file doesn't exist
            }

            try
            {
                var data = await File.ReadAllBytesAsync(filePath);
                var validationResult = await ValidateDataAsync(data);
                return validationResult.IsValid;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[SerializationHelpers] Integrity check failed for {filePath}: {ex.Message}");
                return false;
            }
        }

        public async Task<StorageDataResult> ProcessIncomingDataAsync(byte[] rawData)
        {
            try
            {
                // Step 1: Validate data
                var validationResult = await ValidateDataAsync(rawData);
                if (!validationResult.IsValid)
                {
                    return StorageDataResult.CreateFailure($"Data validation failed: {validationResult.Message}");
                }

                // Step 2: Decompress if needed
                var processedData = await DecompressDataAsync(rawData);

                return StorageDataResult.CreateSuccess(processedData);
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[SerializationHelpers] Processing incoming data failed: {ex.Message}");
                return StorageDataResult.CreateFailure($"Data processing failed: {ex.Message}");
            }
        }

        public async Task<byte[]> ProcessOutgoingDataAsync(byte[] data)
        {
            try
            {
                // Step 1: Compress if beneficial
                var processedData = await CompressDataAsync(data);

                return processedData;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[SerializationHelpers] Processing outgoing data failed: {ex.Message}");
                return data; // Return original data if processing fails
            }
        }

        public async Task<ProjectChimera.Data.Save.CorruptionScanResult> ScanForCorruptionAsync(string filePath)
        {
            var result = new ProjectChimera.Data.Save.CorruptionScanResult();

            try
            {
                if (!File.Exists(filePath))
                {
                    result.IsCorrupted = true;
                    result.ErrorMessage = "File not found";
                    return result;
                }

                var data = await File.ReadAllBytesAsync(filePath);

                // Perform corruption checks
                result.IsCorrupted = IsDataCorrupted(data);
                result.FileSize = data.Length;
                result.ScanDate = DateTime.Now;

                if (!result.IsCorrupted)
                {
                    var validationResult = await ValidateDataAsync(data);
                    result.IsCorrupted = !validationResult.IsValid;
                    result.ErrorMessage = validationResult.Message;
                }

                ChimeraLogger.Log($"[SerializationHelpers] Corruption scan completed for {filePath} - Corrupted: {result.IsCorrupted}");
                return result;
            }
            catch (Exception ex)
            {
                result.IsCorrupted = true;
                result.ErrorMessage = ex.Message;
                ChimeraLogger.LogError($"[SerializationHelpers] Corruption scan failed for {filePath}: {ex.Message}");
                return result;
            }
        }

        public async Task<StorageResult> RepairCorruptedFileAsync(string filePath)
        {
            try
            {
                // Simple repair attempt - try to recover readable portions
                // In a real implementation, this would be more sophisticated

                var data = await File.ReadAllBytesAsync(filePath);

                // Attempt basic repair by removing obvious corruption markers
                var repairedData = AttemptBasicRepair(data);

                if (repairedData != null && repairedData.Length > 0)
                {
                    var backupPath = filePath + ".corrupted_backup";
                    File.Copy(filePath, backupPath, true);

                    await File.WriteAllBytesAsync(filePath, repairedData);

                    ChimeraLogger.Log($"[SerializationHelpers] File repair attempted for {filePath}");
                    return StorageResult.CreateSuccess("File repair attempted - original backed up");
                }

                return StorageResult.CreateFailure("File could not be repaired");
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[SerializationHelpers] File repair failed for {filePath}: {ex.Message}");
                return StorageResult.CreateFailure($"File repair failed: {ex.Message}");
            }
        }

        private void InitializeServices()
        {
            // In a real implementation, these would be more sophisticated services
            _compressionService = new StorageCompressionService();
            _validationService = new StorageValidationService();
        }

        private async Task<byte[]> CompressWithGZipAsync(byte[] data)
        {
            using var output = new MemoryStream();
            using var gzipStream = new GZipStream(output, _compressionLevel);
            await gzipStream.WriteAsync(data, 0, data.Length);
            gzipStream.Close();
            return output.ToArray();
        }

        private async Task<byte[]> CompressWithDeflateAsync(byte[] data)
        {
            using var output = new MemoryStream();
            using var deflateStream = new DeflateStream(output, _compressionLevel);
            await deflateStream.WriteAsync(data, 0, data.Length);
            deflateStream.Close();
            return output.ToArray();
        }

        private async Task<byte[]> CompressWithBrotliAsync(byte[] data)
        {
            using var output = new MemoryStream();
            using var brotliStream = new BrotliStream(output, _compressionLevel);
            await brotliStream.WriteAsync(data, 0, data.Length);
            brotliStream.Close();
            return output.ToArray();
        }

        private async Task<byte[]> TryDecompressAsync(byte[] data)
        {
            // Try different decompression methods
            var methods = new Func<byte[], Task<byte[]>>[]
            {
                TryDecompressGZip,
                TryDecompressDeflate,
                TryDecompressBrotli
            };

            foreach (var method in methods)
            {
                try
                {
                    var result = await method(data);
                    if (result != null)
                    {
                        return result;
                    }
                }
                catch
                {
                    // Try next method
                }
            }

            return null;
        }

        private async Task<byte[]> TryDecompressGZip(byte[] data)
        {
            using var input = new MemoryStream(data);
            using var gzipStream = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            await gzipStream.CopyToAsync(output);
            return output.ToArray();
        }

        private async Task<byte[]> TryDecompressDeflate(byte[] data)
        {
            using var input = new MemoryStream(data);
            using var deflateStream = new DeflateStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            await deflateStream.CopyToAsync(output);
            return output.ToArray();
        }

        private async Task<byte[]> TryDecompressBrotli(byte[] data)
        {
            using var input = new MemoryStream(data);
            using var brotliStream = new BrotliStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            await brotliStream.CopyToAsync(output);
            return output.ToArray();
        }

        private float CalculateEntropy(byte[] data)
        {
            if (data == null || data.Length == 0) return 0f;

            var frequency = new int[256];
            foreach (byte b in data)
            {
                frequency[b]++;
            }

            float entropy = 0f;
            foreach (int count in frequency)
            {
                if (count > 0)
                {
                    float probability = (float)count / data.Length;
                    entropy -= probability * (float)Math.Log(probability, 2);
                }
            }

            return entropy / 8f; // Normalize to 0-1 range
        }

        private bool IsDataCorrupted(byte[] data)
        {
            if (data == null || data.Length == 0) return true;

            // Simple corruption detection - check for patterns that suggest corruption
            int nullByteCount = 0;
            int consecutiveNulls = 0;
            int maxConsecutiveNulls = 0;

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == 0)
                {
                    nullByteCount++;
                    consecutiveNulls++;
                    maxConsecutiveNulls = Math.Max(maxConsecutiveNulls, consecutiveNulls);
                }
                else
                {
                    consecutiveNulls = 0;
                }
            }

            // Flag as corrupted if too many null bytes or too many consecutive nulls
            float nullPercentage = (float)nullByteCount / data.Length;
            return nullPercentage > 0.5f || maxConsecutiveNulls > 1024;
        }

        private byte[] AttemptBasicRepair(byte[] data)
        {
            if (data == null || data.Length == 0) return null;

            // Simple repair - remove obvious corruption patterns
            var repairedData = new byte[data.Length];
            int writeIndex = 0;
            int consecutiveNulls = 0;

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == 0)
                {
                    consecutiveNulls++;
                    if (consecutiveNulls < 10) // Allow some null bytes
                    {
                        repairedData[writeIndex++] = data[i];
                    }
                }
                else
                {
                    consecutiveNulls = 0;
                    repairedData[writeIndex++] = data[i];
                }
            }

            var result = new byte[writeIndex];
            Array.Copy(repairedData, result, writeIndex);
            return result;
        }

        private HashAlgorithm CreateHashAlgorithm()
        {
            return _hashAlgorithm switch
            {
                HashAlgorithmType.SHA256 => SHA256.Create(),
                HashAlgorithmType.SHA512 => SHA512.Create(),
                HashAlgorithmType.MD5 => MD5.Create(),
                _ => SHA256.Create()
            };
        }

        public async Task<byte[]> SerializeDataAsync(object data)
        {
            try
            {
                if (data == null) return new byte[0];

                var json = UnityEngine.JsonUtility.ToJson(data);
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);

                if (ShouldCompress(bytes))
                {
                    return await CompressDataAsync(bytes);
                }

                return bytes;
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError($"[SerializationHelpers] Failed to serialize data: {ex.Message}");
                return new byte[0];
            }
        }

        public async Task<T> DeserializeDataAsync<T>(byte[] data)
        {
            try
            {
                if (data == null || data.Length == 0) return default(T);

                var decompressedData = await DecompressDataAsync(data);
                var json = System.Text.Encoding.UTF8.GetString(decompressedData);

                return UnityEngine.JsonUtility.FromJson<T>(json);
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError($"[SerializationHelpers] Failed to deserialize data: {ex.Message}");
                return default(T);
            }
        }
    }
}
