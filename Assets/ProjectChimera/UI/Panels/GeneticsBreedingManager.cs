using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Manages breeding operations and history for genetics context menu.
    /// Extracted from GeneticsContextMenu.cs to reduce file size and improve separation of concerns.
    /// </summary>
    public class GeneticsBreedingManager
    {
        // Breeding State
        private bool _isInBreedingMode = false;
        private string _selectedParentA = string.Empty;
        private string _selectedParentB = string.Empty;
        private readonly List<string> _breedingHistory = new List<string>();
        
        // Events
        public event Action<string, string> OnBreedingStarted;
        public event Action<string, string> OnGeneticsActionTriggered;
        public event Action<List<string>> OnMenuItemsChanged;
        
        public bool IsInBreedingMode => _isInBreedingMode;
        public string SelectedParentA => _selectedParentA;
        public string SelectedParentB => _selectedParentB;
        
        /// <summary>
        /// Starts breeding mode with selected strains
        /// </summary>
        public bool StartBreedingMode(HashSet<string> selectedStrains)
        {
            if (selectedStrains.Count != 2)
            {
                Debug.LogWarning("[GeneticsBreedingManager] Exactly 2 strains must be selected for breeding");
                return false;
            }
            
            _isInBreedingMode = true;
            var strains = selectedStrains.ToArray();
            _selectedParentA = strains[0];
            _selectedParentB = strains[1];
            
            OnBreedingStarted?.Invoke(_selectedParentA, _selectedParentB);
            OnGeneticsActionTriggered?.Invoke("breeding-started", $"{_selectedParentA}x{_selectedParentB}");
            
            Debug.Log($"[GeneticsBreedingManager] Started breeding: {_selectedParentA} x {_selectedParentB}");
            return true;
        }
        
        /// <summary>
        /// Executes the breeding operation
        /// </summary>
        public bool ExecuteBreeding()
        {
            if (!_isInBreedingMode || string.IsNullOrEmpty(_selectedParentA) || string.IsNullOrEmpty(_selectedParentB))
            {
                return false;
            }
            
            var crossName = $"{_selectedParentA} x {_selectedParentB}";
            AddToBreedingHistory(crossName);
            
            OnGeneticsActionTriggered?.Invoke("execute-breeding", crossName);
            
            _isInBreedingMode = false;
            _selectedParentA = string.Empty;
            _selectedParentB = string.Empty;
            
            Debug.Log($"[GeneticsBreedingManager] Executed breeding: {crossName}");
            return true;
        }
        
        /// <summary>
        /// Cancels breeding mode
        /// </summary>
        public bool CancelBreeding()
        {
            _isInBreedingMode = false;
            _selectedParentA = string.Empty;
            _selectedParentB = string.Empty;
            
            OnGeneticsActionTriggered?.Invoke("cancel-breeding", "");
            
            Debug.Log("[GeneticsBreedingManager] Canceled breeding");
            return true;
        }
        
        /// <summary>
        /// Adds breeding operation to history
        /// </summary>
        private void AddToBreedingHistory(string operation)
        {
            _breedingHistory.Insert(0, operation);
            
            // Limit history size
            const int maxHistorySize = 10;
            if (_breedingHistory.Count > maxHistorySize)
            {
                _breedingHistory.RemoveAt(_breedingHistory.Count - 1);
            }
        }
        
        /// <summary>
        /// Gets breeding history
        /// </summary>
        public List<string> GetBreedingHistory()
        {
            return new List<string>(_breedingHistory);
        }
        
        /// <summary>
        /// Resets all breeding state
        /// </summary>
        public void Reset()
        {
            _isInBreedingMode = false;
            _selectedParentA = string.Empty;
            _selectedParentB = string.Empty;
            // Keep history intact on reset
        }
        
        /// <summary>
        /// Clears breeding history
        /// </summary>
        public void ClearHistory()
        {
            _breedingHistory.Clear();
        }
        
        /// <summary>
        /// Connects events to main handlers
        /// </summary>
        public void ConnectToMainHandlers(
            Action<string, string> breedingStarted,
            Action<string, string> actionTriggered,
            Action<List<string>> menuItemsChanged)
        {
            OnBreedingStarted = breedingStarted;
            OnGeneticsActionTriggered = actionTriggered;
            OnMenuItemsChanged = menuItemsChanged;
        }
    }
}