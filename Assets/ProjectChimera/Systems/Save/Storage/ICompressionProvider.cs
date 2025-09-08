using System.Threading.Tasks;

namespace ProjectChimera.Systems.Save.Storage
{
    /// <summary>
    /// Interface for compression providers
    /// </summary>
    public interface ICompressionProvider
    {
        string ProviderName { get; }
        Task<byte[]> CompressAsync(byte[] data);
        Task<byte[]> DecompressAsync(byte[] compressedData);
    }
    
    /// <summary>
    /// Interface for encryption providers
    /// </summary>
    public interface IEncryptionProvider
    {
        string ProviderName { get; }
        Task<byte[]> EncryptAsync(byte[] data, string key);
        Task<byte[]> DecryptAsync(byte[] encryptedData, string key);
    }
    
    /// <summary>
    /// Interface for hash providers
    /// </summary>
    public interface IHashProvider
    {
        ProjectChimera.Data.Save.HashAlgorithmType AlgorithmType { get; }
        Task<string> CalculateHashAsync(byte[] data);
        Task<bool> VerifyHashAsync(byte[] data, string expectedHash);
    }
    
    /// <summary>
    /// Interface for data serializers
    /// </summary>
    public interface IDataSerializer
    {
        SerializationFormat Format { get; }
        Task<byte[]> SerializeAsync<T>(T obj);
        Task<T> DeserializeAsync<T>(byte[] data);
    }
    
    /// <summary>
    /// Serialization format enum
    /// </summary>
    public enum SerializationFormat
    {
        Binary,
        JSON,
        XML,
        MessagePack,
        Protobuf
    }
}