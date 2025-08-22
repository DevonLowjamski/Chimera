using UnityEngine;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Data.Save;
using ProjectChimera.Core;
using ProjectChimera.Systems.Save;

namespace ProjectChimera.Systems.Save.Components
{
    /// <summary>
    /// Handles file validation and verification for Project Chimera's save system.
    /// Provides integrity checking, corruption detection, and data validation
    /// for cannabis cultivation save data.
    /// </summary>
    public class StorageValidationService : MonoBehaviour
    {
        [Header("Validation Configuration")]
        [SerializeField] private bool _enableDebugLogging = false;
        
        // Validation settings
        private bool _enableIntegrityChecking = true;
        private bool _enableCorruptionDetection = true;
        private bool _enableContentValidation = true;
        private HashAlgorithmType _hashAlgorithm = HashAlgorithmType.SHA256;
        private int _maxValidationRetries = 3;
        private float _validationTimeout = 30f; // seconds
        
        // Validation metrics
        private ValidationMetrics _metrics = new ValidationMetrics();
        private Dictionary<string, FileValidationCache> _validationCache = new Dictionary<string, FileValidationCache>();
        
        public void Initialize(bool enableIntegrityChecking, bool enableCorruptionDetection, 
            bool enableContentValidation, HashAlgorithmType hashAlgorithm, int maxRetries, float timeout)
        {
            _enableIntegrityChecking = enableIntegrityChecking;
            _enableCorruptionDetection = enableCorruptionDetection;
            _enableContentValidation = enableContentValidation;
            _hashAlgorithm = hashAlgorithm;
            _maxValidationRetries = maxRetries;
            _validationTimeout = timeout;
            
            LogInfo("Storage validation service initialized for cannabis cultivation data integrity");
        }
        
        /// <summary>
        /// Validate file data before saving
        /// </summary>
        public async Task<ValidationResult> ValidateBeforeSaveAsync(string slotName, byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return ValidationResult.CreateFailure("No data to validate");
            }
            
            var startTime = DateTime.Now;
            
            try
            {
                var result = new ValidationResult { SlotName = slotName, Success = true };
                
                // File size validation
                if (!ValidateFileSize(data, result))
                {
                    _metrics.TotalValidationFailures++;
                    return result;
                }
                
                // Content structure validation
                if (_enableContentValidation && !await ValidateContentStructureAsync(data, result))
                {
                    _metrics.TotalValidationFailures++;
                    return result;
                }
                
                // Generate integrity hash if enabled
                if (_enableIntegrityChecking)
                {
                    result.IntegrityHash = await GenerateIntegrityHashAsync(data);
                }
                
                // Cache validation result
                CacheValidationResult(slotName, data, result);
                
                var validationTime = DateTime.Now - startTime;
                UpdateValidationMetrics(data.Length, validationTime, true);
                
                LogInfo($"Pre-save validation successful for {slotName}: {data.Length} bytes, {validationTime.TotalMilliseconds:F1}ms");
                
                return result;
            }
            catch (Exception ex)
            {
                _metrics.TotalValidationFailures++;
                LogError($"Pre-save validation failed for {slotName}: {ex.Message}");
                return ValidationResult.CreateFailure(ex.Message, ex);
            }
        }
        
        /// <summary>
        /// Validate file data after loading
        /// </summary>
        public async Task<ValidationResult> ValidateAfterLoadAsync(string slotName, byte[] data, string expectedHash = null)
        {
            if (data == null || data.Length == 0)
            {
                return ValidationResult.CreateFailure("No data to validate");
            }
            
            var startTime = DateTime.Now;
            int attempt = 0;
            
            while (attempt < _maxValidationRetries)
            {
                attempt++;
                
                try
                {
                    var result = new ValidationResult { SlotName = slotName, Success = true };
                    
                    // Integrity validation
                    if (_enableIntegrityChecking && !string.IsNullOrEmpty(expectedHash))
                    {
                        if (!await ValidateIntegrityAsync(data, expectedHash, result))
                        {
                            if (attempt < _maxValidationRetries)
                            {
                                LogWarning($"Integrity validation failed for {slotName}, attempt {attempt}/{_maxValidationRetries}");
                                await Task.Delay(100 * attempt); // Progressive backoff
                                continue;
                            }
                            
                            _metrics.TotalValidationFailures++;
                            return result;
                        }
                    }
                    
                    // Corruption detection
                    if (_enableCorruptionDetection && !await DetectCorruptionAsync(data, result))
                    {
                        if (attempt < _maxValidationRetries)
                        {
                            LogWarning($"Corruption detected for {slotName}, attempt {attempt}/{_maxValidationRetries}");
                            await Task.Delay(100 * attempt);
                            continue;
                        }
                        
                        _metrics.TotalValidationFailures++;
                        return result;
                    }
                    
                    // Content validation
                    if (_enableContentValidation && !await ValidateContentStructureAsync(data, result))
                    {
                        if (attempt < _maxValidationRetries)
                        {
                            LogWarning($"Content validation failed for {slotName}, attempt {attempt}/{_maxValidationRetries}");
                            await Task.Delay(100 * attempt);
                            continue;
                        }
                        
                        _metrics.TotalValidationFailures++;
                        return result;
                    }
                    
                    var validationTime = DateTime.Now - startTime;
                    UpdateValidationMetrics(data.Length, validationTime, true);
                    
                    LogInfo($"Post-load validation successful for {slotName}: {data.Length} bytes, {validationTime.TotalMilliseconds:F1}ms");
                    
                    return result;
                }
                catch (Exception ex)
                {
                    if (attempt >= _maxValidationRetries)
                    {
                        _metrics.TotalValidationFailures++;
                        LogError($"Post-load validation failed for {slotName} after {attempt} attempts: {ex.Message}");
                        return ValidationResult.CreateFailure(ex.Message, ex);
                    }
                    
                    LogWarning($"Validation attempt {attempt} failed for {slotName}: {ex.Message}");
                    await Task.Delay(100 * attempt);
                }
            }
            
            return ValidationResult.CreateFailure($"Validation failed after {_maxValidationRetries} attempts");
        }
        
        /// <summary>
        /// Validate file exists and is accessible
        /// </summary>
        public async Task<ValidationResult> ValidateFileAccessAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return ValidationResult.CreateFailure($"File does not exist: {filePath}");
                }
                
                var fileInfo = new FileInfo(filePath);
                
                // Check file size
                if (fileInfo.Length == 0)
                {
                    return ValidationResult.CreateFailure($"File is empty: {filePath}");
                }
                
                // Check read access
                try
                {
                    using var stream = File.OpenRead(filePath);
                    var buffer = new byte[Math.Min(1024, fileInfo.Length)];
                    await stream.ReadAsync(buffer, 0, buffer.Length);
                }
                catch (UnauthorizedAccessException)
                {
                    return ValidationResult.CreateFailure($"No read access to file: {filePath}");
                }
                catch (IOException ex)
                {
                    return ValidationResult.CreateFailure($"File access error: {ex.Message}");
                }
                
                LogInfo($"File access validation successful: {filePath}");
                return ValidationResult.CreateSuccess();
            }
            catch (Exception ex)
            {
                LogError($"File access validation failed: {ex.Message}");
                return ValidationResult.CreateFailure(ex.Message, ex);
            }
        }
        
        /// <summary>
        /// Detect potential data corruption
        /// </summary>
        public async Task<CorruptionAnalysis> AnalyzeCorruptionAsync(byte[] data)
        {
            var analysis = new CorruptionAnalysis { DataSize = data.Length };
            
            try
            {
                // Check for null bytes in unexpected locations
                analysis.NullByteCount = data.Count(b => b == 0);
                analysis.NullByteRatio = (float)analysis.NullByteCount / data.Length;
                
                // Check for repeated patterns (potential corruption indicator)
                analysis.RepeatedPatternCount = DetectRepeatedPatterns(data);
                
                // Check entropy (corrupted data often has very low or very high entropy)
                analysis.DataEntropy = CalculateEntropy(data);
                
                // Magic number validation for known file types
                analysis.HasValidMagicNumber = ValidateMagicNumber(data);
                
                // Calculate corruption probability
                analysis.CorruptionProbability = CalculateCorruptionProbability(analysis);
                
                // Determine if data appears corrupted
                analysis.IsCorrupted = analysis.CorruptionProbability > 0.7f ||
                                     analysis.NullByteRatio > 0.9f ||
                                     analysis.DataEntropy < 0.1f ||
                                     !analysis.HasValidMagicNumber;
                
                LogInfo($"Corruption analysis: {analysis.CorruptionProbability:P1} probability, entropy: {analysis.DataEntropy:F3}");
                
                return analysis;
            }
            catch (Exception ex)
            {
                LogError($"Corruption analysis failed: {ex.Message}");
                analysis.IsCorrupted = true;
                analysis.CorruptionProbability = 1.0f;
                return analysis;
            }
        }
        
        /// <summary>
        /// Generate integrity hash for data
        /// </summary>
        public async Task<string> GenerateIntegrityHashAsync(byte[] data)
        {
            try
            {
                System.Security.Cryptography.HashAlgorithm hashAlgorithm = _hashAlgorithm switch
                {
                    HashAlgorithmType.MD5 => MD5.Create(),
                    HashAlgorithmType.SHA1 => SHA1.Create(),
                    HashAlgorithmType.SHA256 => SHA256.Create(),
                    HashAlgorithmType.SHA512 => SHA512.Create(),
                    _ => SHA256.Create()
                };
                
                using (hashAlgorithm)
                {
                    byte[] hashBytes = await Task.Run(() => hashAlgorithm.ComputeHash(data));
                    return Convert.ToBase64String(hashBytes);
                }
            }
            catch (Exception ex)
            {
                LogError($"Hash generation failed: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Get validation performance metrics
        /// </summary>
        public ValidationMetrics GetValidationMetrics()
        {
            return _metrics;
        }
        
        /// <summary>
        /// Clear validation cache
        /// </summary>
        public void ClearValidationCache()
        {
            _validationCache.Clear();
            LogInfo("Validation cache cleared");
        }
        
        private bool ValidateFileSize(byte[] data, ValidationResult result)
        {
            const long maxFileSize = 500 * 1024 * 1024; // 500MB limit for save files
            const long minFileSize = 10; // Minimum 10 bytes
            
            if (data.Length < minFileSize)
            {
                result.Success = false;
                result.ErrorMessage = $"File too small: {data.Length} bytes (minimum: {minFileSize})";
                return false;
            }
            
            if (data.Length > maxFileSize)
            {
                result.Success = false;
                result.ErrorMessage = $"File too large: {data.Length} bytes (maximum: {maxFileSize})";
                return false;
            }
            
            return true;
        }
        
        private async Task<bool> ValidateContentStructureAsync(byte[] data, ValidationResult result)
        {
            try
            {
                // Basic structure validation for Project Chimera save data
                // Check for expected headers, version info, etc.
                
                if (data.Length < 16)
                {
                    result.Success = false;
                    result.ErrorMessage = "Data too short for valid save file structure";
                    return false;
                }
                
                // Check for valid UTF-8 encoding in first part of file (metadata section)
                var headerBytes = data.Take(Math.Min(512, data.Length)).ToArray();
                try
                {
                    string headerText = System.Text.Encoding.UTF8.GetString(headerBytes);
                    if (headerText.Contains("\0\0\0\0")) // Multiple consecutive null characters indicate corruption
                    {
                        result.Success = false;
                        result.ErrorMessage = "Invalid character encoding detected in file header";
                        return false;
                    }
                }
                catch (Exception)
                {
                    // UTF-8 decoding failure indicates corruption
                    result.Success = false;
                    result.ErrorMessage = "File header contains invalid UTF-8 data";
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Content structure validation failed: {ex.Message}";
                return false;
            }
        }
        
        private async Task<bool> ValidateIntegrityAsync(byte[] data, string expectedHash, ValidationResult result)
        {
            try
            {
                string actualHash = await GenerateIntegrityHashAsync(data);
                
                if (string.IsNullOrEmpty(actualHash))
                {
                    result.Success = false;
                    result.ErrorMessage = "Failed to generate integrity hash";
                    return false;
                }
                
                if (actualHash != expectedHash)
                {
                    result.Success = false;
                    result.ErrorMessage = "Integrity hash mismatch - file may be corrupted";
                    return false;
                }
                
                result.IntegrityHash = actualHash;
                return true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Integrity validation failed: {ex.Message}";
                return false;
            }
        }
        
        private async Task<bool> DetectCorruptionAsync(byte[] data, ValidationResult result)
        {
            try
            {
                var analysis = await AnalyzeCorruptionAsync(data);
                
                if (analysis.IsCorrupted)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Data corruption detected (probability: {analysis.CorruptionProbability:P1})";
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Corruption detection failed: {ex.Message}";
                return false;
            }
        }
        
        private int DetectRepeatedPatterns(byte[] data)
        {
            int patternCount = 0;
            const int patternLength = 4;
            const int minRepeats = 10;
            
            for (int i = 0; i <= data.Length - patternLength * minRepeats; i++)
            {
                var pattern = data.Skip(i).Take(patternLength).ToArray();
                int repeats = 1;
                
                for (int j = i + patternLength; j <= data.Length - patternLength; j += patternLength)
                {
                    var nextPattern = data.Skip(j).Take(patternLength).ToArray();
                    if (pattern.SequenceEqual(nextPattern))
                    {
                        repeats++;
                    }
                    else
                    {
                        break;
                    }
                }
                
                if (repeats >= minRepeats)
                {
                    patternCount++;
                    i += repeats * patternLength - 1; // Skip past this pattern
                }
            }
            
            return patternCount;
        }
        
        private float CalculateEntropy(byte[] data)
        {
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
            
            return (float)(entropy / 8.0); // Normalize to 0-1 range
        }
        
        private bool ValidateMagicNumber(byte[] data)
        {
            if (data.Length < 4) return false;
            
            // Check for common file type magic numbers that might indicate valid structure
            // This is a simplified check - in reality, you'd check for your specific save file format
            var header = data.Take(4).ToArray();
            
            // Avoid obviously corrupted patterns
            if (header.All(b => b == 0) || header.All(b => b == 0xFF))
            {
                return false;
            }
            
            return true;
        }
        
        private float CalculateCorruptionProbability(CorruptionAnalysis analysis)
        {
            float probability = 0f;
            
            // High null byte ratio increases corruption probability
            if (analysis.NullByteRatio > 0.5f)
                probability += 0.3f;
            
            // Very low entropy suggests corruption
            if (analysis.DataEntropy < 0.2f)
                probability += 0.4f;
            
            // Very high entropy might also suggest corruption
            if (analysis.DataEntropy > 0.95f)
                probability += 0.2f;
            
            // Repeated patterns suggest corruption
            if (analysis.RepeatedPatternCount > 5)
                probability += 0.3f;
            
            // Invalid magic number
            if (!analysis.HasValidMagicNumber)
                probability += 0.2f;
            
            return Math.Min(1.0f, probability);
        }
        
        private void CacheValidationResult(string slotName, byte[] data, ValidationResult result)
        {
            _validationCache[slotName] = new FileValidationCache
            {
                SlotName = slotName,
                DataHash = result.IntegrityHash,
                ValidationTime = DateTime.Now,
                DataSize = data.Length,
                IsValid = result.Success
            };
        }
        
        private void UpdateValidationMetrics(long dataSize, TimeSpan validationTime, bool success)
        {
            _metrics.TotalValidations++;
            _metrics.TotalBytesValidated += dataSize;
            _metrics.TotalValidationTime += validationTime;
            
            if (success)
            {
                _metrics.SuccessfulValidations++;
            }
        }
        
        private void LogInfo(string message)
        {
            if (_enableDebugLogging)
                Debug.Log($"[StorageValidationService] {message}");
        }
        
        private void LogWarning(string message)
        {
            if (_enableDebugLogging)
                Debug.LogWarning($"[StorageValidationService] {message}");
        }
        
        private void LogError(string message)
        {
            if (_enableDebugLogging)
                Debug.LogError($"[StorageValidationService] {message}");
        }
    }
    

    
    /// <summary>
    /// Result of validation operation
    /// </summary>
    [System.Serializable]
    public class ValidationResult
    {
        public bool Success;
        public string SlotName;
        public string IntegrityHash;
        public string ErrorMessage;
        public Exception Exception;
        
        public static ValidationResult CreateSuccess()
        {
            return new ValidationResult { Success = true };
        }
        
        public static ValidationResult CreateFailure(string errorMessage, Exception exception = null)
        {
            return new ValidationResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                Exception = exception
            };
        }
    }
    
    /// <summary>
    /// Data corruption analysis results
    /// </summary>
    [System.Serializable]
    public class CorruptionAnalysis
    {
        public long DataSize;
        public int NullByteCount;
        public float NullByteRatio;
        public int RepeatedPatternCount;
        public float DataEntropy;
        public bool HasValidMagicNumber;
        public float CorruptionProbability;
        public bool IsCorrupted;
    }
    
    /// <summary>
    /// Validation performance metrics
    /// </summary>
    [System.Serializable]
    public class ValidationMetrics
    {
        public int TotalValidations;
        public int SuccessfulValidations;
        public int TotalValidationFailures;
        public long TotalBytesValidated;
        public TimeSpan TotalValidationTime;
        
        public float SuccessRate => TotalValidations > 0 ? (float)SuccessfulValidations / TotalValidations : 0f;
        public float AverageValidationSpeed => TotalValidationTime.TotalSeconds > 0 ? (float)(TotalBytesValidated / TotalValidationTime.TotalSeconds) : 0f;
    }
    
    /// <summary>
    /// Cached validation result
    /// </summary>
    [System.Serializable]
    public class FileValidationCache
    {
        public string SlotName;
        public string DataHash;
        public DateTime ValidationTime;
        public long DataSize;
        public bool IsValid;
    }
}