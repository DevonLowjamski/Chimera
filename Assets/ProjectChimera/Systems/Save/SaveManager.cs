using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.Save
{
    /// <summary>
    /// SIMPLE: Basic save system aligned with Project Chimera's save needs.
    /// Focuses on essential save/load functionality without complex systems.
    /// </summary>
    public class SaveManager : ChimeraManager, ITickable
    {
        [Header("Basic Save Settings")]
        [SerializeField] private bool _enableAutoSave = true;
        [SerializeField] private float _autoSaveInterval = 300f; // 5 minutes
        [SerializeField] private string _saveFileName = "game_save";

        // Basic state
        private GameData _currentGameData;
        private bool _isSaving = false;
        private bool _isLoading = false;
        private float _lastAutoSaveTime;
        private bool _isInitialized = false;
        private readonly List<object> _registeredSaveServices = new List<object>();

        /// <summary>
        /// Events for save/load operations
        /// </summary>
        public event System.Action OnSaveStarted;
        public event System.Action OnSaveCompleted;
        public event System.Action OnLoadStarted;
        public event System.Action OnLoadCompleted;
        public event System.Action<string> OnSaveError;
        public event System.Action<string> OnLoadError;

        /// <summary>
        /// Initialize basic save system
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;
            _lastAutoSaveTime = Time.time;

            if (_enableAutoSave)
            {
                // Try to load existing save
                LoadGame();
            }

            ChimeraLogger.Log("OTHER", "$1", this);
        }

        /// <summary>
        /// Save the current game state
        /// </summary>
        public void SaveGame()
        {
            if (_isSaving || !_isInitialized) return;

            _isSaving = true;
            OnSaveStarted?.Invoke();

            try
            {
                // Create basic game data
                _currentGameData = new GameData
                {
                    SaveTime = System.DateTime.Now,
                    GameVersion = "1.0",
                    PlayerData = new PlayerData(),
                    CultivationData = new CultivationData(),
                    ConstructionData = new ConstructionData(),
                    EconomyData = new EconomyData()
                };

                // Collect data from game systems
                CollectGameData(_currentGameData);

                // Save to file (simple JSON for now)
                string jsonData = JsonUtility.ToJson(_currentGameData);
                string filePath = System.IO.Path.Combine(Application.persistentDataPath, _saveFileName + ".json");
                System.IO.File.WriteAllText(filePath, jsonData);

                ChimeraLogger.Log("OTHER", "$1", this);
                OnSaveCompleted?.Invoke();
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.Log("OTHER", "$1", this);
                OnSaveError?.Invoke(ex.Message);
            }
            finally
            {
                _isSaving = false;
            }
        }

        /// <summary>
        /// Load the saved game state
        /// </summary>
        public void LoadGame()
        {
            if (_isLoading || !_isInitialized) return;

            _isLoading = true;
            OnLoadStarted?.Invoke();

            try
            {
                string filePath = System.IO.Path.Combine(Application.persistentDataPath, _saveFileName + ".json");

                if (System.IO.File.Exists(filePath))
                {
                    string jsonData = System.IO.File.ReadAllText(filePath);
                    _currentGameData = JsonUtility.FromJson<GameData>(jsonData);

                    // Apply loaded data to game systems
                    ApplyGameData(_currentGameData);

                    ChimeraLogger.Log("OTHER", "$1", this);
                    OnLoadCompleted?.Invoke();
                }
                else
                {
                    // No save file exists, start new game
                    ChimeraLogger.Log("OTHER", "$1", this);
                    _currentGameData = null;
                    OnLoadCompleted?.Invoke();
                }
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.Log("OTHER", "$1", this);
                OnLoadError?.Invoke(ex.Message);
            }
            finally
            {
                _isLoading = false;
            }
        }

        /// <summary>
        /// Update auto-save functionality
        /// </summary>
    public int TickPriority => 100;
    public bool IsTickable => enabled && gameObject.activeInHierarchy;

    public void Tick(float deltaTime)
    {
            if (!_enableAutoSave || !_isInitialized) return;

            if (Time.time - _lastAutoSaveTime >= _autoSaveInterval)
                _lastAutoSaveTime = Time.time;
                SaveGame();
    }

    private void Awake()
    {
        UpdateOrchestrator.Instance.RegisterTickable(this);
    }

    private void OnDestroy()
    {
        UpdateOrchestrator.Instance.UnregisterTickable(this);
    }

        /// <summary>
        /// Check if save file exists
        /// </summary>
        public bool SaveFileExists()
        {
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, _saveFileName + ".json");
            return System.IO.File.Exists(filePath);
        }

        /// <summary>
        /// Delete save file
        /// </summary>
        public void DeleteSaveFile()
        {
            try
            {
                string filePath = System.IO.Path.Combine(Application.persistentDataPath, _saveFileName + ".json");
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    ChimeraLogger.Log("OTHER", "$1", this);
                }
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.Log("OTHER", "$1", this);
            }
        }

        /// <summary>
        /// Get save file info
        /// </summary>
        public SaveFileInfo GetSaveFileInfo()
        {
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, _saveFileName + ".json");

            if (System.IO.File.Exists(filePath))
            {
                var fileInfo = new System.IO.FileInfo(filePath);
                return new SaveFileInfo
                {
                    FileSize = fileInfo.Length,
                    LastModified = fileInfo.LastWriteTime,
                    Exists = true
                };
            }

            return new SaveFileInfo { Exists = false };
        }

        /// <summary>
        /// Register a save service provider
        /// </summary>
        public void RegisterSaveService(object saveService)
        {
            if (saveService == null)
            {
                ChimeraLogger.Log("OTHER", "$1", this);
                return;
            }

            if (!_registeredSaveServices.Contains(saveService))
            {
                _registeredSaveServices.Add(saveService);
                ChimeraLogger.Log("OTHER", "$1", this);
            }
        }

        /// <summary>
        /// Unregister a save service provider
        /// </summary>
        public void UnregisterSaveService(object saveService)
        {
            if (saveService != null && _registeredSaveServices.Contains(saveService))
            {
                _registeredSaveServices.Remove(saveService);
                ChimeraLogger.Log("OTHER", "$1", this);
            }
        }

        /// <summary>
        /// Get all registered save services
        /// </summary>
        public IReadOnlyList<object> GetRegisteredSaveServices()
        {
            return _registeredSaveServices.AsReadOnly();
        }

        #region Private Methods

        private void CollectGameData(GameData gameData)
        {
            // Collect data from various game systems
            // This would be implemented to gather data from cultivation, construction, economy systems
            // For now, using placeholder data
        }

        private void ApplyGameData(GameData gameData)
        {
            // Apply loaded data to various game systems
            // This would be implemented to restore data to cultivation, construction, economy systems
            // For now, just logging
            ChimeraLogger.Log("OTHER", "$1", this);
        }

        #endregion

        #region ChimeraManager Implementation

        /// <summary>
        /// ChimeraManager initialization hook
        /// </summary>
        protected override void OnManagerInitialize()
        {
            Initialize();
        }

        /// <summary>
        /// ChimeraManager shutdown hook
        /// </summary>
        protected override void OnManagerShutdown()
        {
            // Save before shutdown if auto-save is enabled
            if (_enableAutoSave && _isInitialized)
            {
                SaveGame();
            }
        }

        #endregion
    }

    /// <summary>
    /// Basic game data structure
    /// </summary>
    [System.Serializable]
    public class GameData
    {
        public System.DateTime SaveTime;
        public string GameVersion;
        public PlayerData PlayerData;
        public CultivationData CultivationData;
        public ConstructionData ConstructionData;
        public EconomyData EconomyData;
    }

    /// <summary>
    /// Basic player data
    /// </summary>
    [System.Serializable]
    public class PlayerData
    {
        public string PlayerName = "Player";
        public int ExperiencePoints = 0;
        public int SkillPoints = 0;
    }

    /// <summary>
    /// Basic cultivation data
    /// </summary>
    [System.Serializable]
    public class CultivationData
    {
        public List<PlantData> Plants = new List<PlantData>();
        public float FacilityTemperature = 25f;
        public float FacilityHumidity = 60f;
    }

    /// <summary>
    /// Basic construction data
    /// </summary>
    [System.Serializable]
    public class ConstructionData
    {
        public List<RoomData> Rooms = new List<RoomData>();
        public float FacilitySize = 100f;
    }

    /// <summary>
    /// Basic economy data
    /// </summary>
    [System.Serializable]
    public class EconomyData
    {
        public float Currency = 1000f;
        public List<ItemData> Inventory = new List<ItemData>();
    }

    /// <summary>
    /// Basic plant data
    /// </summary>
    [System.Serializable]
    public class PlantData
    {
        public string PlantId;
        public string StrainName;
        public float Age;
        public float Health;
    }

    /// <summary>
    /// Basic room data
    /// </summary>
    [System.Serializable]
    public class RoomData
    {
        public string RoomType;
        public Vector3 Position;
        public Vector3 Size;
    }

    /// <summary>
    /// Basic item data
    /// </summary>
    [System.Serializable]
    public class ItemData
    {
        public string ItemName;
        public int Quantity;
    }

    /// <summary>
    /// Save file information
    /// </summary>
    [System.Serializable]
    public class SaveFileInfo
    {
        public bool Exists;
        public long FileSize;
        public System.DateTime LastModified;
    }
}
