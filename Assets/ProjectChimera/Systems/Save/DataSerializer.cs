using UnityEngine;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace ProjectChimera.Systems.Save
{
    /// <summary>
    /// High-performance data serializer for Project Chimera save system.
    /// Handles binary serialization, compression, encryption, and async operations
    /// to ensure efficient and secure save/load operations with data integrity.
    /// </summary>
    public class DataSerializer
    {
        private readonly bool _enableCompression;
        private readonly bool _enableEncryption;
        private readonly System.IO.Compression.CompressionLevel _compressionLevel;
        private readonly string _encryptionKey;
        
        // Performance metrics
        private DataSerializationMetrics _metrics = new DataSerializationMetrics();
        
        public DataSerializer(bool enableCompression = true, bool enableEncryption = false, System.IO.Compression.CompressionLevel compressionLevel = System.IO.Compression.CompressionLevel.Optimal)
        {
            _enableCompression = enableCompression;
            _enableEncryption = enableEncryption;
            _compressionLevel = compressionLevel;
            _encryptionKey = GenerateEncryptionKey();
        }
        
        public DataSerializationMetrics Metrics => _metrics;
        
        /// <summary>
        /// Serialize object to byte array synchronously
        /// </summary>
        public byte[] Serialize<T>(T data)
        {
            var startTime = DateTime.Now;
            
            try
            {
                // Convert to JSON first (Unity's JsonUtility is reliable)
                string jsonData = JsonUtility.ToJson(data, true);
                
                // Validate and sanitize Unicode before encoding
                jsonData = SanitizeUnicodeString(jsonData);
                
                byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonData);
                
                byte[] result = jsonBytes;
                
                // Apply compression if enabled
                if (_enableCompression)
                {
                    result = CompressData(result);
                    _metrics.CompressionRatio = (float)result.Length / jsonBytes.Length;
                }
                
                // Apply encryption if enabled
                if (_enableEncryption)
                {
                    result = EncryptData(result);
                }
                
                // Add header with metadata
                result = AddDataHeader(result, jsonBytes.Length);
                
                // Update metrics
                _metrics.TotalSerializations++;
                _metrics.TotalBytesSerialize += result.Length;
                _metrics.LastSerializationTime = DateTime.Now - startTime;
                _metrics.AverageSerializationTime = CalculateAverageTime(_metrics.AverageSerializationTime, _metrics.LastSerializationTime, _metrics.TotalSerializations);
                
                return result;
            }
            catch (Exception ex)
            {
                _metrics.SerializationErrors++;
                Debug.LogError($"Serialization failed: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Deserialize byte array to object synchronously
        /// </summary>
        public T Deserialize<T>(byte[] data)
        {
            var startTime = DateTime.Now;
            
            try
            {
                // Remove and validate header
                var headerInfo = ReadDataHeader(data);
                byte[] actualData = new byte[data.Length - headerInfo.HeaderSize];
                Array.Copy(data, headerInfo.HeaderSize, actualData, 0, actualData.Length);
                
                // Apply decryption if enabled
                if (_enableEncryption)
                {
                    actualData = DecryptData(actualData);
                }
                
                // Apply decompression if enabled
                if (_enableCompression)
                {
                    actualData = DecompressData(actualData);
                }
                
                // Validate decompressed size matches header
                if (actualData.Length != headerInfo.OriginalSize)
                {
                    throw new InvalidDataException($"Decompressed size mismatch. Expected: {headerInfo.OriginalSize}, Actual: {actualData.Length}");
                }
                
                // Convert from JSON
                string jsonData = Encoding.UTF8.GetString(actualData);
                T result = JsonUtility.FromJson<T>(jsonData);
                
                // Update metrics
                _metrics.TotalDeserializations++;
                _metrics.TotalBytesDeserialized += data.Length;
                _metrics.LastDeserializationTime = DateTime.Now - startTime;
                _metrics.AverageDeserializationTime = CalculateAverageTime(_metrics.AverageDeserializationTime, _metrics.LastDeserializationTime, _metrics.TotalDeserializations);
                
                return result;
            }
            catch (Exception ex)
            {
                _metrics.DeserializationErrors++;
                Debug.LogError($"Deserialization failed: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Serialize object to byte array asynchronously
        /// </summary>
        public async Task<byte[]> SerializeAsync<T>(T data)
        {
            return await Task.Run(() => Serialize(data));
        }
        
        /// <summary>
        /// Deserialize byte array to object asynchronously
        /// </summary>
        public async Task<T> DeserializeAsync<T>(byte[] data)
        {
            return await Task.Run(() => Deserialize<T>(data));
        }
        
        /// <summary>
        /// Compress data using GZip compression
        /// </summary>
        private byte[] CompressData(byte[] data)
        {
            using (var output = new MemoryStream())
            {
                using (var gzip = new GZipStream(output, _compressionLevel))
                {
                    gzip.Write(data, 0, data.Length);
                }
                return output.ToArray();
            }
        }
        
        /// <summary>
        /// Decompress data using GZip decompression
        /// </summary>
        private byte[] DecompressData(byte[] compressedData)
        {
            using (var input = new MemoryStream(compressedData))
            using (var gzip = new GZipStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                gzip.CopyTo(output);
                return output.ToArray();
            }
        }
        
        /// <summary>
        /// Encrypt data using AES encryption
        /// </summary>
        private byte[] EncryptData(byte[] data)
        {
            if (!_enableEncryption) return data;
            
            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(_encryptionKey.Substring(0, 32)); // 256-bit key
                aes.GenerateIV();
                
                using (var encryptor = aes.CreateEncryptor())
                using (var msEncrypt = new MemoryStream())
                {
                    // Prepend IV to encrypted data
                    msEncrypt.Write(aes.IV, 0, aes.IV.Length);
                    
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(data, 0, data.Length);
                    }
                    
                    return msEncrypt.ToArray();
                }
            }
        }
        
        /// <summary>
        /// Decrypt data using AES decryption
        /// </summary>
        private byte[] DecryptData(byte[] encryptedData)
        {
            if (!_enableEncryption) return encryptedData;
            
            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(_encryptionKey.Substring(0, 32)); // 256-bit key
                
                // Extract IV from beginning of encrypted data
                byte[] iv = new byte[16]; // AES block size
                Array.Copy(encryptedData, 0, iv, 0, iv.Length);
                aes.IV = iv;
                
                // Get actual encrypted data (skip IV)
                byte[] actualEncryptedData = new byte[encryptedData.Length - 16];
                Array.Copy(encryptedData, 16, actualEncryptedData, 0, actualEncryptedData.Length);
                
                using (var decryptor = aes.CreateDecryptor())
                using (var msDecrypt = new MemoryStream(actualEncryptedData))
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (var msResult = new MemoryStream())
                {
                    csDecrypt.CopyTo(msResult);
                    return msResult.ToArray();
                }
            }
        }
        
        /// <summary>
        /// Add metadata header to serialized data
        /// </summary>
        private byte[] AddDataHeader(byte[] data, int originalSize)
        {
            var header = new DataHeader
            {
                Magic = DataHeader.MAGIC_NUMBER,
                Version = DataHeader.CURRENT_VERSION,
                Flags = 0,
                OriginalSize = originalSize,
                CompressedSize = data.Length,
                Checksum = CalculateChecksum(data),
                Timestamp = DateTime.Now.ToBinary()
            };
            
            // Set flags
            if (_enableCompression) header.Flags |= DataHeader.FLAG_COMPRESSED;
            if (_enableEncryption) header.Flags |= DataHeader.FLAG_ENCRYPTED;
            
            byte[] headerBytes = SerializeHeader(header);
            byte[] result = new byte[headerBytes.Length + data.Length];
            
            Array.Copy(headerBytes, 0, result, 0, headerBytes.Length);
            Array.Copy(data, 0, result, headerBytes.Length, data.Length);
            
            return result;
        }
        
        /// <summary>
        /// Read and validate metadata header from serialized data
        /// </summary>
        private DataHeaderInfo ReadDataHeader(byte[] data)
        {
            if (data.Length < DataHeader.HEADER_SIZE)
            {
                throw new InvalidDataException("Data too small to contain valid header");
            }
            
            byte[] headerBytes = new byte[DataHeader.HEADER_SIZE];
            Array.Copy(data, 0, headerBytes, 0, DataHeader.HEADER_SIZE);
            
            var header = DeserializeHeader(headerBytes);
            
            // Validate magic number
            if (header.Magic != DataHeader.MAGIC_NUMBER)
            {
                throw new InvalidDataException("Invalid data format - magic number mismatch");
            }
            
            // Validate version
            if (header.Version > DataHeader.CURRENT_VERSION)
            {
                throw new InvalidDataException($"Unsupported data version: {header.Version}");
            }
            
            // Validate checksum
            byte[] actualData = new byte[data.Length - DataHeader.HEADER_SIZE];
            Array.Copy(data, DataHeader.HEADER_SIZE, actualData, 0, actualData.Length);
            
            uint calculatedChecksum = CalculateChecksum(actualData);
            if (calculatedChecksum != header.Checksum)
            {
                throw new InvalidDataException("Data integrity check failed - checksum mismatch");
            }
            
            return new DataHeaderInfo
            {
                Header = header,
                HeaderSize = DataHeader.HEADER_SIZE,
                OriginalSize = header.OriginalSize,
                CompressedSize = header.CompressedSize,
                IsCompressed = (header.Flags & DataHeader.FLAG_COMPRESSED) != 0,
                IsEncrypted = (header.Flags & DataHeader.FLAG_ENCRYPTED) != 0
            };
        }
        
        /// <summary>
        /// Serialize header to byte array
        /// </summary>
        private byte[] SerializeHeader(DataHeader header)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(header.Magic);
                writer.Write(header.Version);
                writer.Write(header.Flags);
                writer.Write(header.OriginalSize);
                writer.Write(header.CompressedSize);
                writer.Write(header.Checksum);
                writer.Write(header.Timestamp);
                
                return stream.ToArray();
            }
        }
        
        /// <summary>
        /// Deserialize header from byte array
        /// </summary>
        private DataHeader DeserializeHeader(byte[] headerBytes)
        {
            using (var stream = new MemoryStream(headerBytes))
            using (var reader = new BinaryReader(stream))
            {
                return new DataHeader
                {
                    Magic = reader.ReadUInt32(),
                    Version = reader.ReadUInt16(),
                    Flags = reader.ReadUInt16(),
                    OriginalSize = reader.ReadInt32(),
                    CompressedSize = reader.ReadInt32(),
                    Checksum = reader.ReadUInt32(),
                    Timestamp = reader.ReadInt64()
                };
            }
        }
        
        /// <summary>
        /// Calculate CRC32 checksum for data integrity
        /// </summary>
        private uint CalculateChecksum(byte[] data)
        {
            uint crc = 0xFFFFFFFF;
            
            for (int i = 0; i < data.Length; i++)
            {
                crc ^= data[i];
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 1) != 0)
                        crc = (crc >> 1) ^ 0xEDB88320;
                    else
                        crc >>= 1;
                }
            }
            
            return ~crc;
        }
        
        /// <summary>
        /// Generate a secure encryption key
        /// </summary>
        private string GenerateEncryptionKey()
        {
            // In a real implementation, this would be generated more securely
            // and stored in a secure location
            return "ProjectChimera_SaveSystem_Key_2024_SecureEnc32Chars!";
        }
        
        /// <summary>
        /// Calculate running average for performance metrics
        /// </summary>
        private TimeSpan CalculateAverageTime(TimeSpan currentAverage, TimeSpan newTime, int count)
        {
            if (count <= 1) return newTime;
            
            long currentTicks = currentAverage.Ticks * (count - 1);
            long newTicks = newTime.Ticks;
            long averageTicks = (currentTicks + newTicks) / count;
            
            return new TimeSpan(averageTicks);
        }
        
        /// <summary>
        /// Get current serialization performance metrics
        /// </summary>
        public DataSerializationMetrics GetMetrics()
        {
            return new DataSerializationMetrics
            {
                TotalSerializations = _metrics.TotalSerializations,
                TotalDeserializations = _metrics.TotalDeserializations,
                TotalBytesSerialize = _metrics.TotalBytesSerialize,
                TotalBytesDeserialized = _metrics.TotalBytesDeserialized,
                AverageSerializationTime = _metrics.AverageSerializationTime,
                AverageDeserializationTime = _metrics.AverageDeserializationTime,
                LastSerializationTime = _metrics.LastSerializationTime,
                LastDeserializationTime = _metrics.LastDeserializationTime,
                SerializationErrors = _metrics.SerializationErrors,
                DeserializationErrors = _metrics.DeserializationErrors,
                CompressionRatio = _metrics.CompressionRatio
            };
        }
        
        /// <summary>
        /// Reset performance metrics
        /// </summary>
        public void ResetMetrics()
        {
            _metrics = new DataSerializationMetrics();
        }

        /// <summary>
        /// Sanitize Unicode string to prevent invalid surrogate pairs
        /// </summary>
        private string SanitizeUnicodeString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var stringBuilder = new StringBuilder();
            
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                
                // Check for high surrogate
                if (char.IsHighSurrogate(c))
                {
                    // Ensure there's a valid low surrogate following
                    if (i + 1 < input.Length && char.IsLowSurrogate(input[i + 1]))
                    {
                        // Valid surrogate pair, add both characters
                        stringBuilder.Append(c);
                        stringBuilder.Append(input[i + 1]);
                        i++; // Skip the low surrogate in next iteration
                    }
                    else
                    {
                        // Invalid high surrogate, replace with Unicode replacement character
                        stringBuilder.Append('\uFFFD');
                        Debug.LogWarning($"Invalid Unicode high surrogate at position {i}, replaced with Unicode replacement character");
                    }
                }
                else if (char.IsLowSurrogate(c))
                {
                    // Orphaned low surrogate, replace with Unicode replacement character
                    stringBuilder.Append('\uFFFD');
                    Debug.LogWarning($"Orphaned Unicode low surrogate at position {i}, replaced with Unicode replacement character");
                }
                else
                {
                    // Regular character, add as-is
                    stringBuilder.Append(c);
                }
            }
            
            return stringBuilder.ToString();
        }
    }
    
    /// <summary>
    /// Data header structure for save files
    /// </summary>
    [System.Serializable]
    public struct DataHeader
    {
        public const uint MAGIC_NUMBER = 0x43484D52; // "CHMR" in hex
        public const ushort CURRENT_VERSION = 1;
        public const int HEADER_SIZE = 28; // Size in bytes
        
        // Flags
        public const ushort FLAG_COMPRESSED = 1 << 0;
        public const ushort FLAG_ENCRYPTED = 1 << 1;
        
        public uint Magic;        // 4 bytes - Magic number for file format identification
        public ushort Version;    // 2 bytes - Data format version
        public ushort Flags;      // 2 bytes - Feature flags (compression, encryption, etc.)
        public int OriginalSize;  // 4 bytes - Original uncompressed data size
        public int CompressedSize;// 4 bytes - Compressed data size
        public uint Checksum;     // 4 bytes - CRC32 checksum for integrity
        public long Timestamp;    // 8 bytes - Creation timestamp
    }
    
    /// <summary>
    /// Data header information after parsing
    /// </summary>
    [System.Serializable]
    public struct DataHeaderInfo
    {
        public DataHeader Header;
        public int HeaderSize;
        public int OriginalSize;
        public int CompressedSize;
        public bool IsCompressed;
        public bool IsEncrypted;
    }
    
    /// <summary>
    /// Serialization performance metrics for DataSerializer
    /// </summary>
    [System.Serializable]
    public class DataSerializationMetrics
    {
        public int TotalSerializations = 0;
        public int TotalDeserializations = 0;
        public long TotalBytesSerialize = 0;
        public long TotalBytesDeserialized = 0;
        public TimeSpan AverageSerializationTime = TimeSpan.Zero;
        public TimeSpan AverageDeserializationTime = TimeSpan.Zero;
        public TimeSpan LastSerializationTime = TimeSpan.Zero;
        public TimeSpan LastDeserializationTime = TimeSpan.Zero;
        public int SerializationErrors = 0;
        public int DeserializationErrors = 0;
        public float CompressionRatio = 1.0f;
        
        public float ErrorRate => TotalSerializations + TotalDeserializations > 0 
            ? (float)(SerializationErrors + DeserializationErrors) / (TotalSerializations + TotalDeserializations) 
            : 0f;
            
        public float AverageCompressionRatio => CompressionRatio;
        
        public string GetSummary()
        {
            return $"Serializations: {TotalSerializations}, Deserializations: {TotalDeserializations}, " +
                   $"Avg Serialize Time: {AverageSerializationTime.TotalMilliseconds:F2}ms, " +
                   $"Avg Deserialize Time: {AverageDeserializationTime.TotalMilliseconds:F2}ms, " +
                   $"Error Rate: {ErrorRate:P2}, Compression: {CompressionRatio:P1}";
        }
    }
}