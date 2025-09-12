using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Save
{
    /// <summary>
    /// BASIC: Simple contract save service for Project Chimera.
    /// Focuses on essential contract saving without complex systems and validation.
    /// </summary>
    public class ContractSaveService : MonoBehaviour
    {
        [Header("Basic Contract Save Settings")]
        [SerializeField] private bool _enableBasicSaving = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private int _maxContractsToSave = 50;

        // Basic contract data storage
        private readonly List<ContractSaveData> _activeContracts = new List<ContractSaveData>();
        private readonly List<ContractSaveData> _completedContracts = new List<ContractSaveData>();
        private bool _isInitialized = false;

        /// <summary>
        /// Events for save operations
        /// </summary>
        public event System.Action OnContractsSaved;
        public event System.Action OnContractsLoaded;

        /// <summary>
        /// Initialize basic contract save service
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("[ContractSaveService] Initialized successfully");
            }
        }

        /// <summary>
        /// Save active contracts
        /// </summary>
        public void SaveActiveContracts(List<ContractData> contracts)
        {
            if (!_enableBasicSaving || !_isInitialized) return;

            _activeContracts.Clear();

            foreach (var contract in contracts)
            {
                if (_activeContracts.Count >= _maxContractsToSave) break;

                var saveData = ContractDataToSaveData(contract);
                _activeContracts.Add(saveData);
            }

            OnContractsSaved?.Invoke();

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[ContractSaveService] Saved {_activeContracts.Count} active contracts");
            }
        }

        /// <summary>
        /// Load active contracts
        /// </summary>
        public List<ContractData> LoadActiveContracts()
        {
            if (!_enableBasicSaving || !_isInitialized) return new List<ContractData>();

            var contracts = new List<ContractData>();

            foreach (var saveData in _activeContracts)
            {
                var contract = SaveDataToContractData(saveData);
                if (contract != null)
                {
                    contracts.Add(contract);
                }
            }

            OnContractsLoaded?.Invoke();

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[ContractSaveService] Loaded {contracts.Count} active contracts");
            }

            return contracts;
        }

        /// <summary>
        /// Save completed contract
        /// </summary>
        public void SaveCompletedContract(ContractData contract)
        {
            if (!_enableBasicSaving || !_isInitialized || contract == null) return;

            var saveData = ContractDataToSaveData(contract);
            _completedContracts.Add(saveData);

            // Keep only the most recent contracts
            if (_completedContracts.Count > _maxContractsToSave)
            {
                _completedContracts.RemoveAt(0);
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[ContractSaveService] Saved completed contract: {contract.ContractId}");
            }
        }

        /// <summary>
        /// Get completed contracts
        /// </summary>
        public List<ContractData> GetCompletedContracts()
        {
            if (!_enableBasicSaving || !_isInitialized) return new List<ContractData>();

            var contracts = new List<ContractData>();

            foreach (var saveData in _completedContracts)
            {
                var contract = SaveDataToContractData(saveData);
                if (contract != null)
                {
                    contracts.Add(contract);
                }
            }

            return contracts;
        }

        /// <summary>
        /// Clear all saved contracts
        /// </summary>
        public void ClearAllContracts()
        {
            _activeContracts.Clear();
            _completedContracts.Clear();

            if (_enableLogging)
            {
                ChimeraLogger.Log("[ContractSaveService] Cleared all saved contracts");
            }
        }

        /// <summary>
        /// Check if contract exists in save data
        /// </summary>
        public bool HasContract(string contractId)
        {
            return _activeContracts.Exists(c => c.ContractId == contractId) ||
                   _completedContracts.Exists(c => c.ContractId == contractId);
        }

        /// <summary>
        /// Get contract statistics
        /// </summary>
        public ContractStats GetStats()
        {
            return new ContractStats
            {
                ActiveContracts = _activeContracts.Count,
                CompletedContracts = _completedContracts.Count,
                TotalContracts = _activeContracts.Count + _completedContracts.Count,
                IsSavingEnabled = _enableBasicSaving
            };
        }

        /// <summary>
        /// Create contract save data from contract data
        /// </summary>
        private ContractSaveData ContractDataToSaveData(ContractData contract)
        {
            return new ContractSaveData
            {
                ContractId = contract.ContractId,
                ContractType = contract.ContractType,
                Description = contract.Description,
                Reward = contract.Reward,
                IsCompleted = contract.IsCompleted,
                Progress = contract.Progress,
                Deadline = contract.Deadline,
                SaveTime = System.DateTime.Now
            };
        }

        /// <summary>
        /// Create contract data from save data
        /// </summary>
        private ContractData SaveDataToContractData(ContractSaveData saveData)
        {
            return new ContractData
            {
                ContractId = saveData.ContractId,
                ContractType = saveData.ContractType,
                Description = saveData.Description,
                Reward = saveData.Reward,
                IsCompleted = saveData.IsCompleted,
                Progress = saveData.Progress,
                Deadline = saveData.Deadline
            };
        }
    }

    /// <summary>
    /// Basic contract save data
    /// </summary>
    [System.Serializable]
    public class ContractSaveData
    {
        public string ContractId;
        public string ContractType;
        public string Description;
        public float Reward;
        public bool IsCompleted;
        public float Progress; // 0-1
        public System.DateTime Deadline;
        public System.DateTime SaveTime;
    }

    /// <summary>
    /// Basic contract data (simplified)
    /// </summary>
    [System.Serializable]
    public class ContractData
    {
        public string ContractId;
        public string ContractType;
        public string Description;
        public float Reward;
        public bool IsCompleted;
        public float Progress; // 0-1
        public System.DateTime Deadline;
    }

    /// <summary>
    /// Contract statistics
    /// </summary>
    [System.Serializable]
    public struct ContractStats
    {
        public int ActiveContracts;
        public int CompletedContracts;
        public int TotalContracts;
        public bool IsSavingEnabled;
    }
}
