// REFACTORED: Plant Serialization Data Structures
// Extracted from PlantSerializationManager for better separation of concerns

using System;
using UnityEngine;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// Serialization format options
    /// </summary>
    public enum SerializationFormat
    {
        JSON = 0,
        Binary = 1,
        XML = 2
    }

    /// <summary>
    /// Serialization handler interface
    /// </summary>
    public interface ISerializationHandler
    {
        string Serialize(SerializedPlantData data, bool prettyPrint = false);
        SerializedPlantData Deserialize(string serializedData);
    }

    /// <summary>
    /// JSON serialization handler
    /// </summary>
    public class JsonSerializationHandler : ISerializationHandler
    {
        public string Serialize(SerializedPlantData data, bool prettyPrint = false)
        {
            return JsonUtility.ToJson(data, prettyPrint);
        }

        public SerializedPlantData Deserialize(string serializedData)
        {
            return JsonUtility.FromJson<SerializedPlantData>(serializedData);
        }
    }

    /// <summary>
    /// Serialization metadata
    /// </summary>
    [System.Serializable]
    public struct SerializationMetadata
    {
        public string PlantID;
        public DateTime SerializationTime;
        public string Format;
        public string Version;
    }

    /// <summary>
    /// Wrapped plant data with metadata
    /// </summary>
    [System.Serializable]
    public struct SerializedPlantDataWrapper
    {
        public SerializationMetadata Metadata;
        public SerializedPlantData PlantData;
    }

    /// <summary>
    /// Serialization statistics
    /// </summary>
    [System.Serializable]
    public struct SerializationStats
    {
        public int SerializationOperations;
        public int DeserializationOperations;
        public int SerializationErrors;
        public int DeserializationErrors;
        public int FileSaveOperations;
        public int FileLoadOperations;
        public int FileErrors;
        public float TotalSerializationTime;
        public float TotalDeserializationTime;
        public long DataSize;
    }

    /// <summary>
    /// Serialization result
    /// </summary>
    [System.Serializable]
    public struct SerializationResult
    {
        public bool Success;
        public string ErrorMessage;
        public string SerializedData;
        public float SerializationTime;
        public int DataSize;
        public SerializationFormat Format;
    }

    /// <summary>
    /// Deserialization result
    /// </summary>
    [System.Serializable]
    public struct DeserializationResult
    {
        public bool Success;
        public string ErrorMessage;
        public SerializedPlantData PlantData;
        public SerializationMetadata? Metadata;
        public float DeserializationTime;
        public int DataSize;
    }

    /// <summary>
    /// File operation result
    /// </summary>
    [System.Serializable]
    public struct FileOperationResult
    {
        public bool Success;
        public string ErrorMessage;
        public string FilePath;
        public int DataSize;
    }

    /// <summary>
    /// Load from file result
    /// </summary>
    [System.Serializable]
    public struct LoadFromFileResult
    {
        public bool Success;
        public string ErrorMessage;
        public string FilePath;
        public SerializedPlantData PlantData;
        public SerializationMetadata? Metadata;
        public int DataSize;
    }

    /// <summary>
    /// Serialization error details
    /// </summary>
    [System.Serializable]
    public struct SerializationError
    {
        public string PlantID;
        public string Operation;
        public string ErrorMessage;
        public DateTime Timestamp;
    }

    /// <summary>
    /// Serialization summary
    /// </summary>
    [System.Serializable]
    public struct SerializationSummary
    {
        public int TotalOperations;
        public int SuccessfulOperations;
        public long TotalDataProcessed;
        public float AverageSerializationTime;
        public float AverageDeserializationTime;
        public int CachedItems;
        public SerializationFormat CurrentFormat;
        public int TotalErrors;
    }
}

