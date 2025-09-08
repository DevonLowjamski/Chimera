using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Implementation for offline progression calculation and management
    /// </summary>
    public class OfflineProgression : IOfflineProgression
    {
        private bool _enableOfflineProgression = true;
        private DateTime _lastSaveTime = DateTime.Now;
        private readonly List<IOfflineProgressionListener> _offlineProgressionListeners = new List<IOfflineProgressionListener>();
        private FloatGameEventSO _onOfflineProgressionCalculated;

        public bool EnableOfflineProgression 
        { 
            get => _enableOfflineProgression; 
            set => _enableOfflineProgression = value; 
        }

        public DateTime LastSaveTime 
        { 
            get => _lastSaveTime; 
            set => _lastSaveTime = value; 
        }

        public void Initialize()
        {
            _lastSaveTime = DateTime.Now;
            ChimeraLogger.Log("[OfflineProgression] Offline progression system initialized");
        }

        public void Shutdown()
        {
            // Record shutdown time for offline progression
            _lastSaveTime = DateTime.Now;
            _offlineProgressionListeners.Clear();
            ChimeraLogger.Log("[OfflineProgression] Offline progression system shutdown");
        }

        public void RegisterOfflineProgressionListener(IOfflineProgressionListener listener)
        {
            if (listener != null && !_offlineProgressionListeners.Contains(listener))
            {
                _offlineProgressionListeners.Add(listener);
                ChimeraLogger.Log($"[OfflineProgression] Registered offline progression listener: {listener.GetType().Name}");
            }
        }

        public void UnregisterOfflineProgressionListener(IOfflineProgressionListener listener)
        {
            if (_offlineProgressionListeners.Remove(listener))
            {
                ChimeraLogger.Log($"[OfflineProgression] Unregistered offline progression listener: {listener.GetType().Name}");
            }
        }

        public IEnumerator CalculateOfflineProgressionCoroutine()
        {
            yield return new WaitForEndOfFrame(); // Wait for other systems to initialize

            // This would normally load the last save time from save data
            // For now, we'll simulate no offline time
            DateTime lastPlayTime = _lastSaveTime;
            DateTime currentTime = DateTime.Now;
            TimeSpan offlineTime = currentTime - lastPlayTime;

            if (offlineTime.TotalMinutes > 1.0) // Only calculate if offline for more than 1 minute
            {
                ChimeraLogger.Log($"[OfflineProgression] Calculating offline progression for {offlineTime.TotalHours:F2} hours");

                float offlineHours = (float)offlineTime.TotalHours;
                
                // Notify offline progression listeners
                NotifyOfflineProgressionListeners(offlineHours);
                _onOfflineProgressionCalculated?.Raise(offlineHours);

                ChimeraLogger.Log($"[OfflineProgression] Offline progression calculated: {offlineHours:F2} hours processed");
            }
            else
            {
                ChimeraLogger.Log("[OfflineProgression] No significant offline time detected");
            }
        }

        public void NotifyOfflineProgressionListeners(float offlineHours)
        {
            for (int i = _offlineProgressionListeners.Count - 1; i >= 0; i--)
            {
                try
                {
                    _offlineProgressionListeners[i]?.OnOfflineProgressionCalculated(offlineHours);
                }
                catch (Exception e)
                {
                    ChimeraLogger.LogError($"[OfflineProgression] Error notifying offline progression listener: {e.Message}");
                    _offlineProgressionListeners.RemoveAt(i);
                }
            }
        }

        public bool CanTriggerOfflineEvents()
        {
            return _enableOfflineProgression && _offlineProgressionListeners.Count > 0;
        }

        public bool CanCalculateOfflineTime()
        {
            return _enableOfflineProgression && _lastSaveTime != default(DateTime);
        }

        public int GetOfflineProgressionListenerCount()
        {
            return _offlineProgressionListeners.Count;
        }

        public void TriggerOfflineProgressionForTesting(float offlineHours)
        {
            if (!CanTriggerOfflineEvents())
            {
                ChimeraLogger.LogWarning("[OfflineProgression] Cannot trigger offline progression - prerequisites not met");
                return;
            }

            ChimeraLogger.Log($"[OfflineProgression] Triggering offline progression test for {offlineHours:F2} hours with {_offlineProgressionListeners.Count} listeners");

            foreach (var listener in _offlineProgressionListeners.ToArray()) // ToArray to avoid modification during iteration
            {
                try
                {
                    listener.OnOfflineProgressionCalculated(offlineHours);
                    ChimeraLogger.Log($"[OfflineProgression] Offline progression test processed for {listener.GetType().Name}");
                }
                catch (Exception ex)
                {
                    ChimeraLogger.LogError($"[OfflineProgression] Error processing offline progression test for {listener.GetType().Name}: {ex.Message}");
                }
            }

            // Trigger offline progression event
            if (_onOfflineProgressionCalculated != null)
            {
                _onOfflineProgressionCalculated.Raise(offlineHours);
            }
        }

        public void SetOfflineProgressionEvent(FloatGameEventSO offlineProgressionEvent)
        {
            _onOfflineProgressionCalculated = offlineProgressionEvent;
        }
    }
}
