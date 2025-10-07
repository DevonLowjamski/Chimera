using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Camera
{
    /// <summary>
    /// Manages camera state history for undo/redo functionality.
    /// Separated from CameraStateManager to maintain modular architecture.
    /// </summary>
    public class CameraStateHistory
    {
        private List<CameraSnapshot> _stateHistory = new List<CameraSnapshot>();
        private int _currentHistoryIndex = -1;
        private int _maxHistoryEntries;

        public System.Action<CameraSnapshot> OnStateRecorded;

        public CameraStateHistory(int maxEntries = 20)
        {
            _maxHistoryEntries = maxEntries;
        }

        /// <summary>
        /// Record a state snapshot to history
        /// </summary>
        public void RecordState(CameraSnapshot snapshot)
        {
            // Remove any history entries after current index (for redo functionality)
            if (_currentHistoryIndex < _stateHistory.Count - 1)
            {
                _stateHistory.RemoveRange(_currentHistoryIndex + 1, _stateHistory.Count - _currentHistoryIndex - 1);
            }

            // Add new snapshot
            _stateHistory.Add(snapshot);
            _currentHistoryIndex = _stateHistory.Count - 1;

            // Limit history size
            if (_stateHistory.Count > _maxHistoryEntries)
            {
                _stateHistory.RemoveAt(0);
                _currentHistoryIndex--;
            }

            OnStateRecorded?.Invoke(snapshot);
        }

        /// <summary>
        /// Undo to previous state
        /// </summary>
        public bool TryUndo(out CameraSnapshot state)
        {
            state = new CameraSnapshot();

            if (_currentHistoryIndex <= 0)
                return false;

            _currentHistoryIndex--;
            state = _stateHistory[_currentHistoryIndex];
            return true;
        }

        /// <summary>
        /// Redo to next state
        /// </summary>
        public bool TryRedo(out CameraSnapshot state)
        {
            state = new CameraSnapshot();

            if (_currentHistoryIndex >= _stateHistory.Count - 1)
                return false;

            _currentHistoryIndex++;
            state = _stateHistory[_currentHistoryIndex];
            return true;
        }

        /// <summary>
        /// Clear all history
        /// </summary>
        public void ClearHistory()
        {
            _stateHistory.Clear();
            _currentHistoryIndex = -1;
        }

        /// <summary>
        /// Get current history count
        /// </summary>
        public int HistoryCount => _stateHistory.Count;

        /// <summary>
        /// Check if undo is available
        /// </summary>
        public bool CanUndo => _currentHistoryIndex > 0;

        /// <summary>
        /// Check if redo is available
        /// </summary>
        public bool CanRedo => _currentHistoryIndex < _stateHistory.Count - 1;
    }
}