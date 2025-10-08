// REFACTORED: Repair History Tracker
// Extracted from MalfunctionRepairProcessor for better separation of concerns

using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// Tracks repair history and provides analytics
    /// </summary>
    public class RepairHistoryTracker
    {
        private readonly List<RepairResult> _repairHistory;
        private readonly Dictionary<string, List<RepairResult>> _equipmentRepairHistory;
        private readonly int _maxHistorySize;

        public RepairHistoryTracker(int maxHistorySize = 1000)
        {
            _repairHistory = new List<RepairResult>();
            _equipmentRepairHistory = new Dictionary<string, List<RepairResult>>();
            _maxHistorySize = maxHistorySize;
        }

        public void AddRepair(RepairResult result, string equipmentId)
        {
            _repairHistory.Add(result);

            if (!_equipmentRepairHistory.ContainsKey(equipmentId))
                _equipmentRepairHistory[equipmentId] = new List<RepairResult>();

            _equipmentRepairHistory[equipmentId].Add(result);

            // Limit history size
            if (_repairHistory.Count > _maxHistorySize)
                _repairHistory.RemoveAt(0);

            if (_equipmentRepairHistory[equipmentId].Count > _maxHistorySize / 10)
                _equipmentRepairHistory[equipmentId].RemoveAt(0);
        }

        public List<RepairResult> GetAllRepairHistory() => _repairHistory;

        public List<RepairResult> GetEquipmentRepairHistory(string equipmentId)
        {
            return _equipmentRepairHistory.TryGetValue(equipmentId, out var history) ? 
                history : 
                new List<RepairResult>();
        }

        public int GetRepairCount(string equipmentId)
        {
            return _equipmentRepairHistory.TryGetValue(equipmentId, out var history) ? 
                history.Count : 
                0;
        }

        public float GetAverageRepairCost(string equipmentId)
        {
            if (!_equipmentRepairHistory.TryGetValue(equipmentId, out var history) || history.Count == 0)
                return 0f;

            return history.Average(r => r.ActualCost);
        }

        public float GetSuccessRate(string equipmentId)
        {
            if (!_equipmentRepairHistory.TryGetValue(equipmentId, out var history) || history.Count == 0)
                return 0f;

            int successfulRepairs = history.Count(r => r.Success);
            return (float)successfulRepairs / history.Count;
        }

        public void Clear()
        {
            _repairHistory.Clear();
            _equipmentRepairHistory.Clear();
        }
    }
}

