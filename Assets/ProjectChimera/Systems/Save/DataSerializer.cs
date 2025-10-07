using UnityEngine;
using System.IO;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Save
{
    /// <summary>
    /// BASIC: Simple data serializer for Project Chimera's save system.
    /// Focuses on essential serialization without complex compression and encryption.
    /// </summary>
    public static class DataSerializer
    {
        /// <summary>
        /// Serialize object to JSON string
        /// </summary>
        public static string SerializeToJson<T>(T data)
        {
            if (data == null) return null;

            try
            {
                string json = JsonUtility.ToJson(data, true);
                ChimeraLogger.Log("OTHER", "$1", null);
                return json;
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.Log("OTHER", "$1", null);
                return null;
            }
        }

        /// <summary>
        /// Deserialize JSON string to object
        /// </summary>
        public static T DeserializeFromJson<T>(string json)
        {
            if (string.IsNullOrEmpty(json)) return default;

            try
            {
                T data = JsonUtility.FromJson<T>(json);
                ChimeraLogger.Log("OTHER", "$1", null);
                return data;
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.Log("OTHER", "$1", null);
                return default;
            }
        }

        /// <summary>
        /// Serialize and save to file
        /// </summary>
        public static bool SerializeToFile<T>(T data, string filePath)
        {
            string json = SerializeToJson(data);
            if (json == null) return false;

            try
            {
                // Ensure directory exists
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(filePath, json);
                ChimeraLogger.Log("OTHER", "$1", null);
                return true;
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.Log("OTHER", "$1", null);
                return false;
            }
        }

        /// <summary>
        /// Load and deserialize from file
        /// </summary>
        public static T DeserializeFromFile<T>(string filePath)
        {
            if (!File.Exists(filePath))
            {
                ChimeraLogger.Log("OTHER", "$1", null);
                return default;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                T data = DeserializeFromJson<T>(json);

                if (data != null)
                {
                    ChimeraLogger.Log("OTHER", "$1", null);
                }

                return data;
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.Log("OTHER", "$1", null);
                return default;
            }
        }

        /// <summary>
        /// Check if file can be deserialized
        /// </summary>
        public static bool CanDeserializeFile(string filePath)
        {
            if (!File.Exists(filePath)) return false;

            try
            {
                string json = File.ReadAllText(filePath);
                return !string.IsNullOrEmpty(json) && json.TrimStart().StartsWith("{");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get file serialization info
        /// </summary>
        public static SerializationInfo GetFileInfo(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            try
            {
                var fileInfo = new FileInfo(filePath);
                string json = File.ReadAllText(filePath);

                return new SerializationInfo
                {
                    FilePath = filePath,
                    FileSize = fileInfo.Length,
                    LastModified = fileInfo.LastWriteTime,
                    IsValidJson = CanDeserializeFile(filePath),
                    CharacterCount = json.Length
                };
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.Log("OTHER", "$1", null);
                return null;
            }
        }

        /// <summary>
        /// Validate JSON string
        /// </summary>
        public static bool IsValidJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;

            try
            {
                // Simple validation - try to parse
                JsonUtility.FromJson<SimpleTestObject>(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Create backup of file
        /// </summary>
        public static bool CreateBackup(string originalPath, string backupPath)
        {
            if (!File.Exists(originalPath)) return false;

            try
            {
                File.Copy(originalPath, backupPath, true);
                ChimeraLogger.Log("OTHER", "$1", null);
                return true;
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.Log("OTHER", "$1", null);
                return false;
            }
        }

        /// <summary>
        /// Get serializer statistics
        /// </summary>
        public static SerializerStats GetStats()
        {
            return new SerializerStats
            {
                UsesJsonSerialization = true,
                SupportsFileOperations = true,
                SupportsValidation = true
            };
        }

        #region Private Classes

        private class SimpleTestObject
        {
            public string test;
        }

        #endregion
    }

    /// <summary>
    /// Serialization information
    /// </summary>
    [System.Serializable]
    public class SerializationInfo
    {
        public string FilePath;
        public long FileSize;
        public System.DateTime LastModified;
        public bool IsValidJson;
        public int CharacterCount;
    }

    /// <summary>
    /// Serializer statistics
    /// </summary>
    [System.Serializable]
    public struct SerializerStats
    {
        public bool UsesJsonSerialization;
        public bool SupportsFileOperations;
        public bool SupportsValidation;
    }
}
