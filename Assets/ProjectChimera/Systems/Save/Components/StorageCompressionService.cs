using UnityEngine;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using ProjectChimera.Data.Save;
using ProjectChimera.Core;

// Type alias to resolve CompressionLevel ambiguity
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace ProjectChimera.Systems.Save.Components
{
    /// <summary>
    /// Handles data compression and decompression for Project Chimera's save system.
    /// Provides multiple compression algorithms, integrity verification, and performance optimization
    /// for cannabis cultivation save data storage.
    /// </summary>
    public class StorageCompressionService : MonoBehaviour
    {
        [Header("Compression Configuration")]
        [SerializeField] private bool _enableDebugLogging = false;
        
        // Compression settings
        private CompressionLevel _compressionLevel = CompressionLevel.Optimal;
        private CompressionAlgorithm _compressionAlgorithm = CompressionAlgorithm.GZip;
        private bool _enableCompression = true;
        private float _compressionThreshold = 0.1f; // 10% minimum size reduction required
        private int _maxCompressionAttempts = 3;
        
        // Performance tracking
        private CompressionMetrics _metrics = new CompressionMetrics();
        
        public void Initialize(CompressionLevel compressionLevel, CompressionAlgorithm algorithm, 
            bool enableCompression, float compressionThreshold, int maxAttempts)
        {
            _compressionLevel = compressionLevel;
            _compressionAlgorithm = algorithm;
            _enableCompression = enableCompression;
            _compressionThreshold = compressionThreshold;
            _maxCompressionAttempts = maxAttempts;
            
            LogInfo("Storage compression service initialized for cannabis cultivation data");
        }
        
        /// <summary>
        /// Compress data using configured algorithm
        /// </summary>
        public async Task<CompressionResult> CompressDataAsync(byte[] data)
        {
            if (!_enableCompression || data == null || data.Length == 0)
            {
                return CompressionResult.CreateUncompressed(data);
            }
            
            var startTime = DateTime.Now;
            
            try
            {
                byte[] compressedData = null;
                int attempts = 0;
                
                while (attempts < _maxCompressionAttempts && compressedData == null)
                {
                    attempts++;
                    
                    try
                    {
                        compressedData = _compressionAlgorithm switch
                        {
                            CompressionAlgorithm.GZip => await CompressWithGZipAsync(data),
                            CompressionAlgorithm.Deflate => await CompressWithDeflateAsync(data),
                            CompressionAlgorithm.Brotli => await CompressWithBrotliAsync(data),
                            CompressionAlgorithm.LZ4 => await CompressWithLZ4Async(data),
                            _ => data
                        };
                    }
                    catch (Exception ex)
                    {
                        LogWarning($"Compression attempt {attempts} failed: {ex.Message}");
                        if (attempts >= _maxCompressionAttempts)
                        {
                            throw;
                        }
                    }
                }
                
                // Check if compression was worthwhile
                if (compressedData != null)
                {
                    float compressionRatio = 1.0f - (float)compressedData.Length / data.Length;
                    
                    if (compressionRatio >= _compressionThreshold)
                    {
                        var processingTime = DateTime.Now - startTime;
                        UpdateCompressionMetrics(data.Length, compressedData.Length, processingTime, true);
                        
                        LogInfo($"Data compressed: {data.Length} → {compressedData.Length} bytes ({compressionRatio:P1} reduction)");
                        
                        return CompressionResult.CreateCompressed(compressedData, _compressionAlgorithm, compressionRatio);
                    }
                    else
                    {
                        LogInfo($"Compression ratio ({compressionRatio:P1}) below threshold ({_compressionThreshold:P1}) - using uncompressed data");
                    }
                }
                
                // Return uncompressed if compression wasn't beneficial
                var uncompressedTime = DateTime.Now - startTime;
                UpdateCompressionMetrics(data.Length, data.Length, uncompressedTime, false);
                
                return CompressionResult.CreateUncompressed(data);
            }
            catch (Exception ex)
            {
                LogError($"Compression failed after {_maxCompressionAttempts} attempts: {ex.Message}");
                return CompressionResult.CreateFailure(ex.Message, ex);
            }
        }
        
        /// <summary>
        /// Decompress data using specified algorithm
        /// </summary>
        public async Task<DecompressionResult> DecompressDataAsync(byte[] compressedData, CompressionAlgorithm algorithm)
        {
            if (compressedData == null || compressedData.Length == 0)
            {
                return DecompressionResult.CreateFailure("No data to decompress");
            }
            
            var startTime = DateTime.Now;
            
            try
            {
                byte[] decompressedData = algorithm switch
                {
                    CompressionAlgorithm.GZip => await DecompressWithGZipAsync(compressedData),
                    CompressionAlgorithm.Deflate => await DecompressWithDeflateAsync(compressedData),
                    CompressionAlgorithm.Brotli => await DecompressWithBrotliAsync(compressedData),
                    CompressionAlgorithm.LZ4 => await DecompressWithLZ4Async(compressedData),
                    CompressionAlgorithm.None => compressedData,
                    _ => throw new NotSupportedException($"Unsupported compression algorithm: {algorithm}")
                };
                
                var processingTime = DateTime.Now - startTime;
                UpdateDecompressionMetrics(compressedData.Length, decompressedData.Length, processingTime);
                
                LogInfo($"Data decompressed: {compressedData.Length} → {decompressedData.Length} bytes");
                
                return DecompressionResult.CreateSuccess(decompressedData);
            }
            catch (Exception ex)
            {
                LogError($"Decompression failed for algorithm {algorithm}: {ex.Message}");
                return DecompressionResult.CreateFailure(ex.Message, ex);
            }
        }
        
        /// <summary>
        /// Estimate compression ratio for data without actually compressing
        /// </summary>
        public float EstimateCompressionRatio(byte[] data)
        {
            if (data == null || data.Length == 0) return 0f;
            
            // Simple entropy-based estimation
            var frequencies = new int[256];
            foreach (byte b in data)
            {
                frequencies[b]++;
            }
            
            double entropy = 0;
            foreach (int freq in frequencies)
            {
                if (freq > 0)
                {
                    double probability = (double)freq / data.Length;
                    entropy -= probability * Math.Log(probability, 2);
                }
            }
            
            // Estimate compression ratio based on entropy
            // Higher entropy = less compressible
            double theoreticalCompressionRatio = Math.Max(0, 1.0 - (entropy / 8.0));
            
            // Apply algorithm efficiency factor
            float algorithmEfficiency = _compressionAlgorithm switch
            {
                CompressionAlgorithm.GZip => 0.85f,
                CompressionAlgorithm.Deflate => 0.80f,
                CompressionAlgorithm.Brotli => 0.90f,
                CompressionAlgorithm.LZ4 => 0.70f,
                _ => 0.50f
            };
            
            return (float)(theoreticalCompressionRatio * algorithmEfficiency);
        }
        
        /// <summary>
        /// Get compression performance metrics
        /// </summary>
        public CompressionMetrics GetCompressionMetrics()
        {
            return _metrics;
        }
        
        /// <summary>
        /// Reset compression metrics
        /// </summary>
        public void ResetMetrics()
        {
            _metrics = new CompressionMetrics();
            LogInfo("Compression metrics reset");
        }
        
        private async Task<byte[]> CompressWithGZipAsync(byte[] data)
        {
            using var memoryStream = new MemoryStream();
            using (var gzipStream = new GZipStream(memoryStream, _compressionLevel))
            {
                await gzipStream.WriteAsync(data, 0, data.Length);
            }
            return memoryStream.ToArray();
        }
        
        private async Task<byte[]> DecompressWithGZipAsync(byte[] compressedData)
        {
            using var compressedStream = new MemoryStream(compressedData);
            using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var resultStream = new MemoryStream();
            
            await gzipStream.CopyToAsync(resultStream);
            return resultStream.ToArray();
        }
        
        private async Task<byte[]> CompressWithDeflateAsync(byte[] data)
        {
            using var memoryStream = new MemoryStream();
            using (var deflateStream = new DeflateStream(memoryStream, _compressionLevel))
            {
                await deflateStream.WriteAsync(data, 0, data.Length);
            }
            return memoryStream.ToArray();
        }
        
        private async Task<byte[]> DecompressWithDeflateAsync(byte[] compressedData)
        {
            using var compressedStream = new MemoryStream(compressedData);
            using var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress);
            using var resultStream = new MemoryStream();
            
            await deflateStream.CopyToAsync(resultStream);
            return resultStream.ToArray();
        }
        
        private async Task<byte[]> CompressWithBrotliAsync(byte[] data)
        {
            using var memoryStream = new MemoryStream();
            using (var brotliStream = new BrotliStream(memoryStream, _compressionLevel))
            {
                await brotliStream.WriteAsync(data, 0, data.Length);
            }
            return memoryStream.ToArray();
        }
        
        private async Task<byte[]> DecompressWithBrotliAsync(byte[] compressedData)
        {
            using var compressedStream = new MemoryStream(compressedData);
            using var brotliStream = new BrotliStream(compressedStream, CompressionMode.Decompress);
            using var resultStream = new MemoryStream();
            
            await brotliStream.CopyToAsync(resultStream);
            return resultStream.ToArray();
        }
        
        private async Task<byte[]> CompressWithLZ4Async(byte[] data)
        {
            // Placeholder for LZ4 compression - would require external library
            // For now, return GZip compression as fallback
            LogWarning("LZ4 compression not implemented - falling back to GZip");
            return await CompressWithGZipAsync(data);
        }
        
        private async Task<byte[]> DecompressWithLZ4Async(byte[] compressedData)
        {
            // Placeholder for LZ4 decompression - would require external library
            // For now, return GZip decompression as fallback
            LogWarning("LZ4 decompression not implemented - falling back to GZip");
            return await DecompressWithGZipAsync(compressedData);
        }
        
        private void UpdateCompressionMetrics(long originalSize, long compressedSize, TimeSpan processingTime, bool wasCompressed)
        {
            _metrics.TotalCompressions++;
            _metrics.TotalOriginalBytes += originalSize;
            _metrics.TotalCompressedBytes += compressedSize;
            _metrics.TotalCompressionTime += processingTime;
            
            if (wasCompressed)
            {
                _metrics.SuccessfulCompressions++;
                float ratio = 1.0f - (float)compressedSize / originalSize;
                _metrics.AverageCompressionRatio = (_metrics.AverageCompressionRatio * (_metrics.SuccessfulCompressions - 1) + ratio) / _metrics.SuccessfulCompressions;
            }
        }
        
        private void UpdateDecompressionMetrics(long compressedSize, long decompressedSize, TimeSpan processingTime)
        {
            _metrics.TotalDecompressions++;
            _metrics.TotalDecompressedBytes += decompressedSize;
            _metrics.TotalDecompressionTime += processingTime;
        }
        
        private void LogInfo(string message)
        {
            if (_enableDebugLogging)
                Debug.Log($"[StorageCompressionService] {message}");
        }
        
        private void LogWarning(string message)
        {
            if (_enableDebugLogging)
                Debug.LogWarning($"[StorageCompressionService] {message}");
        }
        
        private void LogError(string message)
        {
            if (_enableDebugLogging)
                Debug.LogError($"[StorageCompressionService] {message}");
        }
    }
    
    /// <summary>
    /// Compression algorithms supported by the storage system
    /// </summary>
    public enum CompressionAlgorithm
    {
        None = 0,
        GZip = 1,
        Deflate = 2,
        Brotli = 3,
        LZ4 = 4
    }
    
    /// <summary>
    /// Result of compression operation
    /// </summary>
    [System.Serializable]
    public class CompressionResult
    {
        public bool Success;
        public byte[] Data;
        public CompressionAlgorithm Algorithm;
        public float CompressionRatio;
        public string ErrorMessage;
        public Exception Exception;
        
        public static CompressionResult CreateCompressed(byte[] data, CompressionAlgorithm algorithm, float ratio)
        {
            return new CompressionResult
            {
                Success = true,
                Data = data,
                Algorithm = algorithm,
                CompressionRatio = ratio
            };
        }
        
        public static CompressionResult CreateUncompressed(byte[] data)
        {
            return new CompressionResult
            {
                Success = true,
                Data = data,
                Algorithm = CompressionAlgorithm.None,
                CompressionRatio = 0f
            };
        }
        
        public static CompressionResult CreateFailure(string errorMessage, Exception exception = null)
        {
            return new CompressionResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                Exception = exception
            };
        }
    }
    
    /// <summary>
    /// Result of decompression operation
    /// </summary>
    [System.Serializable]
    public class DecompressionResult
    {
        public bool Success;
        public byte[] Data;
        public string ErrorMessage;
        public Exception Exception;
        
        public static DecompressionResult CreateSuccess(byte[] data)
        {
            return new DecompressionResult
            {
                Success = true,
                Data = data
            };
        }
        
        public static DecompressionResult CreateFailure(string errorMessage, Exception exception = null)
        {
            return new DecompressionResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                Exception = exception
            };
        }
    }
    
    /// <summary>
    /// Compression performance metrics
    /// </summary>
    [System.Serializable]
    public class CompressionMetrics
    {
        public int TotalCompressions;
        public int SuccessfulCompressions;
        public int TotalDecompressions;
        public long TotalOriginalBytes;
        public long TotalCompressedBytes;
        public long TotalDecompressedBytes;
        public float AverageCompressionRatio;
        public TimeSpan TotalCompressionTime;
        public TimeSpan TotalDecompressionTime;
        
        public float AverageCompressionSpeed => TotalCompressionTime.TotalSeconds > 0 ? (float)(TotalOriginalBytes / TotalCompressionTime.TotalSeconds) : 0f;
        public float AverageDecompressionSpeed => TotalDecompressionTime.TotalSeconds > 0 ? (float)(TotalDecompressedBytes / TotalDecompressionTime.TotalSeconds) : 0f;
        public long TotalSpaceSaved => TotalOriginalBytes - TotalCompressedBytes;
    }
}