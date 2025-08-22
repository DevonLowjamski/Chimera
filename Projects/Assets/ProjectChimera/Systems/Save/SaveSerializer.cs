using UnityEngine;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using ProjectChimera.Data.Save;
using ProjectChimera.Core;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace ProjectChimera.Systems.Save
{
    /// <summary>
    /// High-performance serialization system for Project Chimera save data.
    /// Handles binary serialization, compression, encryption, and data integrity validation.
    /// Supports asynchronous operations and multiple serialization formats.
    /// </summary>
    public class SaveSerializer : MonoBehaviour
    {
        [Header("Serialization Configuration")]
        [SerializeField] private SerializationFormat _defaultFormat = SerializationFormat.Binary;
        [SerializeField] private bool _enableCompression = true;
        [SerializeField] private bool _enableEncryption = false;
        [SerializeField] private CompressionLevel _compressionLevel = CompressionLevel.Optimal;
        
        [Header("Performance Settings")]
        [SerializeField] private bool _enableAsyncOperations = true;
        [SerializeField] private int _bufferSize = 65536; // 64KB
        [SerializeField] private bool _enableMemoryPooling = true;
        [SerializeField] private int _maxPooledBuffers = 8;
        
        [Header("Validation Settings")]
        [SerializeField] private bool _enableDataValidation = true;
        [SerializeField] private bool _enableChecksumValidation = true;
        [SerializeField] private HashAlgorithmType _hashAlgorithm = HashAlgorithmType.SHA256;
        [SerializeField] private bool _enableCorruptionDetection = true;
        
        [Header("Encryption Settings")]
        [SerializeField] private EncryptionAlgorithm _encryptionAlgorithm = EncryptionAlgorithm.AES256;
        [SerializeField] private bool _generateRandomKeys = true;
        [SerializeField] private bool _enableKeyDerivation = true;
        [SerializeField] private int _keyDerivationIterations = 100000;

        // Serialization systems
        private IBinarySerializer _binarySerializer;
        private IJsonSerializer _jsonSerializer;
        private IMessagePackSerializer _messagePackSerializer;
        
        // Compression and encryption
        private ICompressionProvider _compressionProvider;
        private IEncryptionProvider _encryptionProvider;
        private IHashProvider _hashProvider;
        
        // Memory management
        private IMemoryPool _memoryPool;
        private byte[][] _pooledBuffers;
        private bool[] _bufferUsageFlags;
        
        // State tracking
        private SerializationMetrics _metrics = new SerializationMetrics();
        private bool _isInitialized = false;

        #region Initialization

        private void Awake()
        {
            InitializeSerializationSystems();
        }

        private void InitializeSerializationSystems()
        {
            if (_isInitialized) return;

            // Initialize serializers
            _binarySerializer = new ProjectChimeraBinarySerializer();
            _jsonSerializer = new ProjectChimeraJsonSerializer();
            _messagePackSerializer = new ProjectChimeraMessagePackSerializer();
            
            // Initialize compression provider
            _compressionProvider = new GZipCompressionProvider(_compressionLevel);
            
            // Initialize encryption provider if enabled
            if (_enableEncryption)
            {
                _encryptionProvider = CreateEncryptionProvider();
            }
            
            // Initialize hash provider
            _hashProvider = new HashProvider(_hashAlgorithm);
            
            // Initialize memory pool if enabled
            if (_enableMemoryPooling)
            {
                InitializeMemoryPool();
            }
            
            _isInitialized = true;
            LogInfo($"SaveSerializer initialized - Format: {_defaultFormat}, Compression: {_enableCompression}, Encryption: {_enableEncryption}");
        }

        private void InitializeMemoryPool()
        {
            _pooledBuffers = new byte[_maxPooledBuffers][];
            _bufferUsageFlags = new bool[_maxPooledBuffers];
            
            for (int i = 0; i < _maxPooledBuffers; i++)
            {
                _pooledBuffers[i] = new byte[_bufferSize];
                _bufferUsageFlags[i] = false;
            }
        }

        #endregion

        #region Public Serialization Methods

        /// <summary>
        /// Serialize data using the default format
        /// </summary>
        public byte[] Serialize<T>(T data) where T : class
        {
            return SerializeWithFormat(data, _defaultFormat);
        }

        /// <summary>
        /// Serialize data using a specific format
        /// </summary>
        public byte[] SerializeWithFormat<T>(T data, SerializationFormat format) where T : class
        {
            if (!_isInitialized) InitializeSerializationSystems();
            
            var startTime = DateTime.Now;
            
            try
            {
                // Get serializer for format
                var serializer = GetSerializer(format);
                if (serializer == null)
                {
                    throw new InvalidOperationException($"No serializer available for format: {format}");
                }

                // Serialize to bytes
                byte[] serializedData = serializer.Serialize(data);
                
                // Apply compression if enabled
                if (_enableCompression)
                {
                    serializedData = _compressionProvider.Compress(serializedData);
                }
                
                // Apply encryption if enabled
                if (_enableEncryption && _encryptionProvider != null)
                {
                    serializedData = _encryptionProvider.Encrypt(serializedData);
                }
                
                // Add checksum if enabled
                if (_enableChecksumValidation)
                {
                    serializedData = AddChecksum(serializedData);
                }
                
                // Update metrics
                var duration = DateTime.Now - startTime;
                UpdateSerializationMetrics(serializedData.Length, duration, true);
                
                LogInfo($"Serialized {typeof(T).Name}: {serializedData.Length} bytes in {duration.TotalMilliseconds:F2}ms");
                return serializedData;
            }
            catch (Exception ex)
            {
                LogError($"Serialization failed for {typeof(T).Name}: {ex.Message}");
                _metrics.TotalErrors++;
                throw;
            }
        }

        /// <summary>
        /// Asynchronously serialize data using the default format
        /// </summary>
        public async Task<byte[]> SerializeAsync<T>(T data) where T : class
        {
            return await SerializeWithFormatAsync(data, _defaultFormat);
        }

        /// <summary>
        /// Asynchronously serialize data using a specific format
        /// </summary>
        public async Task<byte[]> SerializeWithFormatAsync<T>(T data, SerializationFormat format) where T : class
        {
            if (!_enableAsyncOperations)
            {
                return SerializeWithFormat(data, format);
            }

            return await Task.Run(() => SerializeWithFormat(data, format));
        }

        #endregion

        #region Public Deserialization Methods

        /// <summary>
        /// Deserialize data using the default format
        /// </summary>
        public T Deserialize<T>(byte[] data) where T : class
        {
            return DeserializeWithFormat<T>(data, _defaultFormat);
        }

        /// <summary>
        /// Deserialize data using a specific format
        /// </summary>
        public T DeserializeWithFormat<T>(byte[] data, SerializationFormat format) where T : class
        {
            if (!_isInitialized) InitializeSerializationSystems();
            
            var startTime = DateTime.Now;
            
            try
            {
                byte[] processedData = data;
                
                // Validate checksum if enabled
                if (_enableChecksumValidation)
                {
                    processedData = ValidateAndRemoveChecksum(processedData);
                }
                
                // Decrypt if enabled
                if (_enableEncryption && _encryptionProvider != null)
                {
                    processedData = _encryptionProvider.Decrypt(processedData);
                }
                
                // Decompress if enabled
                if (_enableCompression)
                {
                    processedData = _compressionProvider.Decompress(processedData);
                }
                
                // Get serializer for format
                var serializer = GetSerializer(format);
                if (serializer == null)
                {
                    throw new InvalidOperationException($"No serializer available for format: {format}");
                }

                // Deserialize from bytes
                T result = serializer.Deserialize<T>(processedData);
                
                // Validate result if enabled
                if (_enableDataValidation && result != null)
                {
                    ValidateDeserializedData(result);
                }
                
                // Update metrics
                var duration = DateTime.Now - startTime;
                UpdateSerializationMetrics(data.Length, duration, false);
                
                LogInfo($"Deserialized {typeof(T).Name}: {data.Length} bytes in {duration.TotalMilliseconds:F2}ms");
                return result;
            }
            catch (Exception ex)
            {
                LogError($"Deserialization failed for {typeof(T).Name}: {ex.Message}");
                _metrics.TotalErrors++;
                throw;
            }
        }

        /// <summary>
        /// Asynchronously deserialize data using the default format
        /// </summary>
        public async Task<T> DeserializeAsync<T>(byte[] data) where T : class
        {
            return await DeserializeWithFormatAsync<T>(data, _defaultFormat);
        }

        /// <summary>
        /// Asynchronously deserialize data using a specific format
        /// </summary>
        public async Task<T> DeserializeWithFormatAsync<T>(byte[] data, SerializationFormat format) where T : class
        {
            if (!_enableAsyncOperations)
            {
                return DeserializeWithFormat<T>(data, format);
            }

            return await Task.Run(() => DeserializeWithFormat<T>(data, format));
        }

        #endregion

        #region Data Validation and Integrity

        private byte[] AddChecksum(byte[] data)
        {
            byte[] hash = _hashProvider.ComputeHash(data);
            byte[] result = new byte[data.Length + hash.Length + 4]; // +4 for hash length
            
            // Write hash length
            BitConverter.GetBytes(hash.Length).CopyTo(result, 0);
            
            // Write hash
            hash.CopyTo(result, 4);
            
            // Write data
            data.CopyTo(result, 4 + hash.Length);
            
            return result;
        }

        private byte[] ValidateAndRemoveChecksum(byte[] data)
        {
            if (data.Length < 8) // Minimum size for hash length + some hash + data
            {
                throw new InvalidDataException("Data too short to contain valid checksum");
            }
            
            // Read hash length
            int hashLength = BitConverter.ToInt32(data, 0);
            
            if (hashLength <= 0 || hashLength > data.Length - 4)
            {
                throw new InvalidDataException("Invalid hash length in data");
            }
            
            // Extract hash
            byte[] storedHash = new byte[hashLength];
            Array.Copy(data, 4, storedHash, 0, hashLength);
            
            // Extract data
            int dataStart = 4 + hashLength;
            int dataLength = data.Length - dataStart;
            byte[] actualData = new byte[dataLength];
            Array.Copy(data, dataStart, actualData, 0, dataLength);
            
            // Compute hash of actual data
            byte[] computedHash = _hashProvider.ComputeHash(actualData);
            
            // Validate hash
            if (!HashesEqual(storedHash, computedHash))
            {
                throw new InvalidDataException("Data checksum validation failed - data may be corrupted");
            }
            
            return actualData;
        }

        private bool HashesEqual(byte[] hash1, byte[] hash2)
        {
            if (hash1.Length != hash2.Length) return false;
            
            for (int i = 0; i < hash1.Length; i++)
            {
                if (hash1[i] != hash2[i]) return false;
            }
            
            return true;
        }

        private void ValidateDeserializedData<T>(T data) where T : class
        {
            // Basic validation - can be extended
            if (data == null)
            {
                throw new InvalidDataException("Deserialized data is null");
            }
            
            // Check for specific validation based on type
            if (data is SaveGameData saveData)
            {
                ValidateSaveGameData(saveData);
            }
        }

        private void ValidateSaveGameData(SaveGameData saveData)
        {
            if (string.IsNullOrEmpty(saveData.GameVersion))
            {
                throw new InvalidDataException("Save data missing game version");
            }
            
            if (saveData.SaveTimestamp == default(DateTime))
            {
                throw new InvalidDataException("Save data missing timestamp");
            }
            
            if (saveData.PlayTime.TotalSeconds < 0)
            {
                throw new InvalidDataException("Save data has invalid play time");
            }
        }

        #endregion

        #region Helper Methods

        private IDataSerializer GetSerializer(SerializationFormat format)
        {
            return format switch
            {
                SerializationFormat.Binary => _binarySerializer,
                SerializationFormat.Json => _jsonSerializer,
                SerializationFormat.MessagePack => _messagePackSerializer,
                _ => null
            };
        }

        private IEncryptionProvider CreateEncryptionProvider()
        {
            return _encryptionAlgorithm switch
            {
                EncryptionAlgorithm.AES256 => new AESEncryptionProvider(_generateRandomKeys, _enableKeyDerivation, _keyDerivationIterations),
                EncryptionAlgorithm.ChaCha20 => new ChaCha20EncryptionProvider(),
                _ => null
            };
        }

        private void UpdateSerializationMetrics(int dataSize, TimeSpan duration, bool isSerialize)
        {
            if (isSerialize)
            {
                _metrics.TotalSerializations++;
                _metrics.TotalSerializedBytes += dataSize;
                _metrics.TotalSerializationTime += duration;
            }
            else
            {
                _metrics.TotalDeserializations++;
                _metrics.TotalDeserializedBytes += dataSize;
                _metrics.TotalDeserializationTime += duration;
            }
            
            _metrics.LastOperationTime = DateTime.Now;
        }

        #endregion

        #region Memory Management

        private byte[] GetPooledBuffer()
        {
            if (!_enableMemoryPooling) return new byte[_bufferSize];
            
            for (int i = 0; i < _maxPooledBuffers; i++)
            {
                if (!_bufferUsageFlags[i])
                {
                    _bufferUsageFlags[i] = true;
                    return _pooledBuffers[i];
                }
            }
            
            // All buffers in use, create new one
            return new byte[_bufferSize];
        }

        private void ReturnPooledBuffer(byte[] buffer)
        {
            if (!_enableMemoryPooling) return;
            
            for (int i = 0; i < _maxPooledBuffers; i++)
            {
                if (_pooledBuffers[i] == buffer)
                {
                    _bufferUsageFlags[i] = false;
                    break;
                }
            }
        }

        #endregion

        #region Public Properties and Methods

        /// <summary>
        /// Get current serialization metrics
        /// </summary>
        public SerializationMetrics GetMetrics() => _metrics;

        /// <summary>
        /// Reset serialization metrics
        /// </summary>
        public void ResetMetrics()
        {
            _metrics = new SerializationMetrics();
        }

        /// <summary>
        /// Get estimated compression ratio for the current settings
        /// </summary>
        public float GetEstimatedCompressionRatio()
        {
            if (!_enableCompression) return 1.0f;
            
            return _compressionLevel switch
            {
                CompressionLevel.Optimal => 0.3f,     // ~70% compression
                CompressionLevel.Fastest => 0.5f,     // ~50% compression
                CompressionLevel.NoCompression => 1.0f, // No compression
                _ => 0.4f
            };
        }

        /// <summary>
        /// Test serialization/deserialization with sample data
        /// </summary>
        public async Task<SerializationTestResult> TestSerializationAsync<T>(T testData) where T : class
        {
            var result = new SerializationTestResult
            {
                TestedType = typeof(T).Name,
                TestStartTime = DateTime.Now
            };
            
            try
            {
                // Test serialization
                var serializeStart = DateTime.Now;
                byte[] serializedData = await SerializeAsync(testData);
                result.SerializationTime = DateTime.Now - serializeStart;
                result.SerializedSize = serializedData.Length;
                
                // Test deserialization
                var deserializeStart = DateTime.Now;
                T deserializedData = await DeserializeAsync<T>(serializedData);
                result.DeserializationTime = DateTime.Now - deserializeStart;
                
                // Basic validation
                result.Success = deserializedData != null;
                result.TestEndTime = DateTime.Now;
                
                LogInfo($"Serialization test completed for {typeof(T).Name}: " +
                       $"Serialize: {result.SerializationTime.TotalMilliseconds:F2}ms, " +
                       $"Deserialize: {result.DeserializationTime.TotalMilliseconds:F2}ms, " +
                       $"Size: {result.SerializedSize} bytes");
                
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.TestEndTime = DateTime.Now;
                
                LogError($"Serialization test failed for {typeof(T).Name}: {ex.Message}");
                return result;
            }
        }

        #endregion

        #region Logging

        private void LogInfo(string message)
        {
            Debug.Log($"[SaveSerializer] {message}");
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[SaveSerializer] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[SaveSerializer] {message}");
        }

        #endregion
    }
}