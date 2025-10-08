using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// REFACTORED: Plant Serialization Manager
    /// Single Responsibility: JSON import/export, data persistence, and serialization format management
    /// Extracted from PlantDataSynchronizer for better separation of concerns
    /// </summary>
    [System.Serializable]
    public class PlantSerializationManager
    {
        [Header("Serialization Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _prettyPrint = true;
        [SerializeField] private bool _includeTimestamp = true;
        [SerializeField] private SerializationFormat _format = SerializationFormat.JSON;

        // Serialization state
        private SerializationStats _stats = new SerializationStats();
        private Dictionary<string, SerializedPlantData> _serializedCache = new Dictionary<string, SerializedPlantData>();
        private bool _isInitialized = false;

        // Format handlers
        private Dictionary<SerializationFormat, ISerializationHandler> _formatHandlers = new Dictionary<SerializationFormat, ISerializationHandler>();

        // Events
        public event System.Action<string, SerializationResult> OnSerializationComplete;
        public event System.Action<string, DeserializationResult> OnDeserializationComplete;
        public event System.Action<string> OnCacheUpdated;
        public event System.Action<SerializationError> OnSerializationError;

        public bool IsInitialized => _isInitialized;
        public SerializationStats Stats => _stats;
        public SerializationFormat CurrentFormat => _format;
        public int CachedDataCount => _serializedCache.Count;

        public void Initialize()
        {
            if (_isInitialized) return;

            _serializedCache.Clear();
            ResetStats();
            InitializeFormatHandlers();

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", "Plant Serialization Manager initialized");
            }
        }

        /// <summary>
        /// Serialize plant data to string
        /// </summary>
        public SerializationResult SerializeToString(SerializedPlantData data, string plantId = null)
        {
            if (!_isInitialized)
            {
                return new SerializationResult
                {
                    Success = false,
                    ErrorMessage = "Serialization manager not initialized",
                    SerializedData = ""
                };
            }

            var startTime = DateTime.Now;
            plantId = plantId ?? data.PlantID ?? "unknown";

            try
            {
                string serializedData = "";

                if (_formatHandlers.TryGetValue(_format, out var handler))
                {
                    serializedData = handler.Serialize(data, _prettyPrint);
                }
                else
                {
                    // Fallback to JSON
                    serializedData = JsonUtility.ToJson(data, _prettyPrint);
                }

                // Add metadata if requested
                if (_includeTimestamp)
                {
                    var metadata = new SerializationMetadata
                    {
                        PlantID = plantId,
                        SerializationTime = DateTime.Now,
                        Format = _format.ToString(),
                        Version = "1.0"
                    };

                    var wrappedData = new SerializedPlantDataWrapper
                    {
                        Metadata = metadata,
                        PlantData = data
                    };

                    if (_format == SerializationFormat.JSON)
                    {
                        serializedData = JsonUtility.ToJson(wrappedData, _prettyPrint);
                    }
                }

                var serializationTime = (float)(DateTime.Now - startTime).TotalMilliseconds;
                _stats.SerializationOperations++;
                _stats.TotalSerializationTime += serializationTime;
                _stats.DataSize += serializedData.Length;

                // Cache the serialized data
                _serializedCache[plantId] = data;

                var result = new SerializationResult
                {
                    Success = true,
                    SerializedData = serializedData,
                    SerializationTime = serializationTime,
                    DataSize = serializedData.Length,
                    Format = _format
                };

                OnSerializationComplete?.Invoke(plantId, result);
                OnCacheUpdated?.Invoke(plantId);

                if (_enableLogging)
                {
                    ChimeraLogger.Log("PLANT", $"Serialized plant {plantId}: {serializedData.Length} bytes, {serializationTime:F2}ms");
                }

                return result;
            }
            catch (Exception ex)
            {
                _stats.SerializationErrors++;

                var error = new SerializationError
                {
                    PlantID = plantId,
                    Operation = "Serialize",
                    ErrorMessage = ex.Message,
                    Timestamp = DateTime.Now
                };

                OnSerializationError?.Invoke(error);

                if (_enableLogging)
                {
                    ChimeraLogger.LogError("PLANT", $"Serialization failed for {plantId}: {ex.Message}");
                }

                return new SerializationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    SerializedData = "",
                    SerializationTime = (float)(DateTime.Now - startTime).TotalMilliseconds
                };
            }
        }

        /// <summary>
        /// Deserialize plant data from string
        /// </summary>
        public DeserializationResult DeserializeFromString(string serializedData, string plantId = null)
        {
            if (!_isInitialized)
            {
                return new DeserializationResult
                {
                    Success = false,
                    ErrorMessage = "Serialization manager not initialized"
                };
            }

            if (string.IsNullOrEmpty(serializedData))
            {
                return new DeserializationResult
                {
                    Success = false,
                    ErrorMessage = "No data provided for deserialization"
                };
            }

            var startTime = DateTime.Now;

            try
            {
                SerializedPlantData plantData;
                SerializationMetadata? metadata = null;

                // Try to detect if data is wrapped with metadata
                if (serializedData.Contains("\"Metadata\"") && serializedData.Contains("\"PlantData\""))
                {
                    var wrapper = JsonUtility.FromJson<SerializedPlantDataWrapper>(serializedData);
                    plantData = wrapper.PlantData;
                    metadata = wrapper.Metadata;
                    plantId = plantId ?? metadata.Value.PlantID;
                }
                else
                {
                    // Direct deserialization
                    if (_formatHandlers.TryGetValue(_format, out var handler))
                    {
                        plantData = handler.Deserialize(serializedData);
                    }
                    else
                    {
                        // Fallback to JSON
                        plantData = JsonUtility.FromJson<SerializedPlantData>(serializedData);
                    }

                    plantId = plantId ?? plantData.PlantID ?? "unknown";
                }

                var deserializationTime = (float)(DateTime.Now - startTime).TotalMilliseconds;
                _stats.DeserializationOperations++;
                _stats.TotalDeserializationTime += deserializationTime;

                // Cache the deserialized data
                _serializedCache[plantId] = plantData;

                var result = new DeserializationResult
                {
                    Success = true,
                    PlantData = plantData,
                    Metadata = metadata,
                    DeserializationTime = deserializationTime,
                    DataSize = serializedData.Length
                };

                OnDeserializationComplete?.Invoke(plantId, result);
                OnCacheUpdated?.Invoke(plantId);

                if (_enableLogging)
                {
                    ChimeraLogger.Log("PLANT", $"Deserialized plant {plantId}: {serializedData.Length} bytes, {deserializationTime:F2}ms");
                }

                return result;
            }
            catch (Exception ex)
            {
                _stats.DeserializationErrors++;

                var error = new SerializationError
                {
                    PlantID = plantId ?? "unknown",
                    Operation = "Deserialize",
                    ErrorMessage = ex.Message,
                    Timestamp = DateTime.Now
                };

                OnSerializationError?.Invoke(error);

                if (_enableLogging)
                {
                    ChimeraLogger.LogError("PLANT", $"Deserialization failed: {ex.Message}");
                }

                return new DeserializationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    DeserializationTime = (float)(DateTime.Now - startTime).TotalMilliseconds
                };
            }
        }

        /// <summary>
        /// Save plant data to file
        /// </summary>
        public FileOperationResult SaveToFile(SerializedPlantData data, string filePath, string plantId = null)
        {
            var serializationResult = SerializeToString(data, plantId);
            if (!serializationResult.Success)
            {
                return new FileOperationResult
                {
                    Success = false,
                    ErrorMessage = $"Serialization failed: {serializationResult.ErrorMessage}",
                    FilePath = filePath
                };
            }

            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(filePath, serializationResult.SerializedData);

                _stats.FileSaveOperations++;

                if (_enableLogging)
                {
                    ChimeraLogger.Log("PLANT", $"Saved plant data to file: {filePath}");
                }

                return new FileOperationResult
                {
                    Success = true,
                    FilePath = filePath,
                    DataSize = serializationResult.DataSize
                };
            }
            catch (Exception ex)
            {
                _stats.FileErrors++;

                if (_enableLogging)
                {
                    ChimeraLogger.LogError("PLANT", $"Failed to save to file {filePath}: {ex.Message}");
                }

                return new FileOperationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    FilePath = filePath
                };
            }
        }

        /// <summary>
        /// Load plant data from file
        /// </summary>
        public LoadFromFileResult LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return new LoadFromFileResult
                    {
                        Success = false,
                        ErrorMessage = $"File not found: {filePath}",
                        FilePath = filePath
                    };
                }

                var fileContent = File.ReadAllText(filePath);
                var deserializationResult = DeserializeFromString(fileContent);

                _stats.FileLoadOperations++;

                if (_enableLogging)
                {
                    ChimeraLogger.Log("PLANT", $"Loaded plant data from file: {filePath}");
                }

                return new LoadFromFileResult
                {
                    Success = deserializationResult.Success,
                    ErrorMessage = deserializationResult.ErrorMessage,
                    FilePath = filePath,
                    PlantData = deserializationResult.PlantData,
                    Metadata = deserializationResult.Metadata,
                    DataSize = fileContent.Length
                };
            }
            catch (Exception ex)
            {
                _stats.FileErrors++;

                if (_enableLogging)
                {
                    ChimeraLogger.LogError("PLANT", $"Failed to load from file {filePath}: {ex.Message}");
                }

                return new LoadFromFileResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    FilePath = filePath
                };
            }
        }

        /// <summary>
        /// Get cached plant data
        /// </summary>
        public bool TryGetCachedData(string plantId, out SerializedPlantData data)
        {
            return _serializedCache.TryGetValue(plantId, out data);
        }

        /// <summary>
        /// Update cached plant data
        /// </summary>
        public void UpdateCache(string plantId, SerializedPlantData data)
        {
            _serializedCache[plantId] = data;
            OnCacheUpdated?.Invoke(plantId);

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Updated cache for plant {plantId}");
            }
        }

        /// <summary>
        /// Clear cached data
        /// </summary>
        public void ClearCache(string plantId = null)
        {
            if (plantId == null)
            {
                var count = _serializedCache.Count;
                _serializedCache.Clear();

                if (_enableLogging)
                {
                    ChimeraLogger.Log("PLANT", $"Cleared {count} items from serialization cache");
                }
            }
            else if (_serializedCache.Remove(plantId))
            {
                if (_enableLogging)
                {
                    ChimeraLogger.Log("PLANT", $"Removed {plantId} from serialization cache");
                }
            }
        }

        /// <summary>
        /// Set serialization format
        /// </summary>
        public void SetSerializationFormat(SerializationFormat format)
        {
            _format = format;

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Serialization format set to: {format}");
            }
        }

        /// <summary>
        /// Get serialization summary
        /// </summary>
        public SerializationSummary GetSerializationSummary()
        {
            return new SerializationSummary
            {
                TotalOperations = _stats.SerializationOperations + _stats.DeserializationOperations,
                SuccessfulOperations = _stats.SerializationOperations + _stats.DeserializationOperations - _stats.SerializationErrors - _stats.DeserializationErrors,
                TotalDataProcessed = _stats.DataSize,
                AverageSerializationTime = _stats.SerializationOperations > 0 ? _stats.TotalSerializationTime / _stats.SerializationOperations : 0f,
                AverageDeserializationTime = _stats.DeserializationOperations > 0 ? _stats.TotalDeserializationTime / _stats.DeserializationOperations : 0f,
                CachedItems = _serializedCache.Count,
                CurrentFormat = _format,
                TotalErrors = _stats.SerializationErrors + _stats.DeserializationErrors + _stats.FileErrors
            };
        }

        /// <summary>
        /// Initialize format handlers
        /// </summary>
        private void InitializeFormatHandlers()
        {
            _formatHandlers[SerializationFormat.JSON] = new JsonSerializationHandler();
            // Additional formats can be added here
        }

        /// <summary>
        /// Reset serialization statistics
        /// </summary>
        private void ResetStats()
        {
            _stats = new SerializationStats();
        }

        /// <summary>
        /// Set serialization options
        /// </summary>
        public void SetSerializationOptions(bool prettyPrint, bool includeTimestamp)
        {
            _prettyPrint = prettyPrint;
            _includeTimestamp = includeTimestamp;

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Serialization options updated: PrettyPrint={prettyPrint}, IncludeTimestamp={includeTimestamp}");
            }
        }

        /// <summary>
        /// Get list of cached plant IDs
        /// </summary>
        public List<string> GetCachedPlantIds()
        {
            return new List<string>(_serializedCache.Keys);
        }
    }
}
