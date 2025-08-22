using UnityEngine;
using System;
using System.Text;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Helper class for safe JSON operations with Unicode validation.
    /// Prevents invalid surrogate pair errors that can occur with large JSON payloads.
    /// </summary>
    public static class JsonUtilityHelper
    {
        /// <summary>
        /// Safely serialize object to JSON with Unicode validation
        /// </summary>
        public static string ToJsonSafe<T>(T obj, bool prettyPrint = false)
        {
            try
            {
                string json = JsonUtility.ToJson(obj, prettyPrint);
                return SanitizeUnicodeString(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to serialize object to JSON: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Safely deserialize JSON to object with error handling
        /// </summary>
        public static T FromJsonSafe<T>(string json)
        {
            try
            {
                if (string.IsNullOrEmpty(json))
                {
                    return default(T);
                }

                // Sanitize input JSON before parsing
                string sanitizedJson = SanitizeUnicodeString(json);
                return JsonUtility.FromJson<T>(sanitizedJson);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to deserialize JSON to {typeof(T).Name}: {ex.Message}");
                return default(T);
            }
        }

        /// <summary>
        /// Safely overwrite existing object from JSON
        /// </summary>
        public static void FromJsonOverwriteSafe<T>(string json, T objectToOverwrite) where T : class
        {
            try
            {
                if (string.IsNullOrEmpty(json) || objectToOverwrite == null)
                {
                    return;
                }

                // Sanitize input JSON before parsing
                string sanitizedJson = SanitizeUnicodeString(json);
                JsonUtility.FromJsonOverwrite(sanitizedJson, objectToOverwrite);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to overwrite {typeof(T).Name} from JSON: {ex.Message}");
            }
        }

        /// <summary>
        /// Validate if a string contains valid JSON
        /// </summary>
        public static bool IsValidJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return false;

            try
            {
                string sanitizedJson = SanitizeUnicodeString(json);
                
                // Unity's JsonUtility doesn't support parsing to object type
                // Instead, we'll do basic JSON structure validation
                return ValidateJsonStructure(sanitizedJson);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Basic JSON structure validation
        /// </summary>
        private static bool ValidateJsonStructure(string json)
        {
            if (string.IsNullOrEmpty(json))
                return false;

            json = json.Trim();
            
            // Must start and end with { } or [ ]
            if (!(json.StartsWith("{") && json.EndsWith("}")) && 
                !(json.StartsWith("[") && json.EndsWith("]")))
            {
                return false;
            }

            // Basic brace/bracket matching
            int braceCount = 0;
            int bracketCount = 0;
            bool inString = false;
            bool escapeNext = false;

            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];

                if (escapeNext)
                {
                    escapeNext = false;
                    continue;
                }

                if (c == '\\' && inString)
                {
                    escapeNext = true;
                    continue;
                }

                if (c == '"')
                {
                    inString = !inString;
                    continue;
                }

                if (!inString)
                {
                    switch (c)
                    {
                        case '{':
                            braceCount++;
                            break;
                        case '}':
                            braceCount--;
                            break;
                        case '[':
                            bracketCount++;
                            break;
                        case ']':
                            bracketCount--;
                            break;
                    }

                    if (braceCount < 0 || bracketCount < 0)
                        return false;
                }
            }

            return braceCount == 0 && bracketCount == 0 && !inString;
        }

        /// <summary>
        /// Get the size of JSON string in bytes
        /// </summary>
        public static int GetJsonSizeInBytes(string json)
        {
            if (string.IsNullOrEmpty(json))
                return 0;

            return Encoding.UTF8.GetByteCount(json);
        }

        /// <summary>
        /// Sanitize Unicode string to prevent invalid surrogate pairs
        /// </summary>
        private static string SanitizeUnicodeString(string input)
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

        /// <summary>
        /// Clean large JSON strings by removing excessive whitespace while preserving structure
        /// </summary>
        public static string CompactJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return json;

            // First sanitize Unicode
            json = SanitizeUnicodeString(json);

            // Remove unnecessary whitespace (basic compaction)
            var result = new StringBuilder();
            bool insideString = false;
            bool escapeNext = false;

            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];

                if (escapeNext)
                {
                    result.Append(c);
                    escapeNext = false;
                    continue;
                }

                if (c == '\\' && insideString)
                {
                    escapeNext = true;
                    result.Append(c);
                    continue;
                }

                if (c == '"')
                {
                    insideString = !insideString;
                    result.Append(c);
                    continue;
                }

                if (!insideString && (c == ' ' || c == '\t' || c == '\n' || c == '\r'))
                {
                    // Skip whitespace outside of strings
                    continue;
                }

                result.Append(c);
            }

            return result.ToString();
        }
    }
} 