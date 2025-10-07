using ProjectChimera.Data.Save;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Save
{
    /// <summary>
    /// Interface for cloud storage providers
    /// </summary>
    public interface ICloudStorageProvider
    {
        bool IsConnected { get; }
        string ProviderName { get; }
        
        Task<bool> Initialize();
        Task<CloudSyncResult> UploadAsync(string fileName, byte[] data);
        Task<CloudSyncResult> DownloadAsync(string fileName);
        Task<bool> DeleteFileAsync(string fileName);
        Task<List<string>> ListFilesAsync();
    }
    
    /// <summary>
    /// Base implementation for cloud storage providers
    /// </summary>
    public abstract class BaseCloudStorageProvider : ICloudStorageProvider
    {
        public abstract bool IsConnected { get; }
        public abstract string ProviderName { get; }
        
        public abstract Task<bool> Initialize();
        public abstract Task<CloudSyncResult> UploadAsync(string fileName, byte[] data);
        public abstract Task<CloudSyncResult> DownloadAsync(string fileName);
        public abstract Task<bool> DeleteFileAsync(string fileName);
        public abstract Task<List<string>> ListFilesAsync();
    }
}