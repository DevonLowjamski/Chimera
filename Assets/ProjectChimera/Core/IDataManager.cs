using System.Collections.Generic;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Interface for data management operations
    /// </summary>
    public interface IDataManager
    {
        /// <summary>
        /// Initialize the data manager
        /// </summary>
        void Initialize();

        /// <summary>
        /// Save data to persistent storage
        /// </summary>
        /// <param name="key">The key to save data under</param>
        /// <param name="data">The data to save</param>
        /// <returns>True if save was successful</returns>
        bool SaveData(string key, object data);

        /// <summary>
        /// Load data from persistent storage
        /// </summary>
        /// <typeparam name="T">The type of data to load</typeparam>
        /// <param name="key">The key to load data from</param>
        /// <returns>The loaded data or default if not found</returns>
        T LoadData<T>(string key);

        /// <summary>
        /// Check if data exists for a given key
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns>True if data exists</returns>
        bool HasData(string key);

        /// <summary>
        /// Delete data for a given key
        /// </summary>
        /// <param name="key">The key to delete</param>
        /// <returns>True if deletion was successful</returns>
        bool DeleteData(string key);

        /// <summary>
        /// Get all available data keys
        /// </summary>
        /// <returns>List of all data keys</returns>
        List<string> GetAllKeys();

        /// <summary>
        /// Clear all data
        /// </summary>
        void ClearAll();
    }
}