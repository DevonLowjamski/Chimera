using ProjectChimera.Data.Save;
using ProjectChimera.Systems.Save.Components;
using System.Threading.Tasks;
using System.IO.Compression;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Save
{
    /// <summary>
    /// Interface for data serialization, compression, and validation
    /// </summary>
    public interface ISerializationHelpers
    {
        // Compression
        Task<byte[]> CompressDataAsync(byte[] data);
        Task<byte[]> DecompressDataAsync(byte[] compressedData);
        bool ShouldCompress(byte[] data);
        float CalculateCompressionRatio(byte[] originalData, byte[] compressedData);

        // Validation and Integrity
        Task<ProjectChimera.Data.Save.ValidationResult> ValidateDataAsync(byte[] data);
        Task<string> CalculateHashAsync(byte[] data);
        Task<bool> VerifyHashAsync(byte[] data, string expectedHash);
        Task<bool> CheckDataIntegrityAsync(string filePath);

        // File Format Support
        Task<StorageDataResult> ProcessIncomingDataAsync(byte[] rawData);
        Task<byte[]> ProcessOutgoingDataAsync(byte[] data);

        // Serialization
        Task<byte[]> SerializeDataAsync(object data);
        Task<T> DeserializeDataAsync<T>(byte[] data);

        // Corruption Detection
        Task<ProjectChimera.Data.Save.CorruptionScanResult> ScanForCorruptionAsync(string filePath);
        Task<StorageResult> RepairCorruptedFileAsync(string filePath);

        void Initialize(bool enableCompression, System.IO.Compression.CompressionLevel compressionLevel,
                       ProjectChimera.Systems.Save.Storage.CompressionAlgorithm compressionAlgorithm, bool enableIntegrityChecking,
                       ProjectChimera.Data.Save.HashAlgorithmType hashAlgorithm);
        void Shutdown();
    }
}
