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
            ChimeraLogger.LogInfo("OfflineProgression", "$1");
        }

        public void Shutdown()
        {
            // Record shutdown time for offline progression
            _lastSaveTime = DateTime.Now;
            _offlineProgressionListeners.Clear();
            ChimeraLogger.LogInfo("OfflineProgression", "$1");
        }

        public void RegisterOfflineProgressionListener(IOfflineProgressionListener listener)
        {
            if (listener != null && !_offlineProgressionListeners.Contains(listener))
            {
                _offlineProgressionListeners.Add(listener);
                ChimeraLogger.LogInfo("OfflineProgression", "$1");
            }
        }

        public void UnregisterOfflineProgressionListener(IOfflineProgressionListener listener)
        {
            if (_offlineProgressionListeners.Remove(listener))
            {
                ChimeraLogger.LogInfo("OfflineProgression", "$1");
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
                ChimeraLogger.LogInfo("OfflineProgression", "$1");

                float offlineHours = (float)offlineTime.TotalHours;

                // Notify offline progression listeners
                NotifyOfflineProgressionListeners(offlineHours);
                _onOfflineProgressionCalculated?.Raise(offlineHours);

                ChimeraLogger.LogInfo("OfflineProgression", "$1");
            }
            else
            {
                ChimeraLogger.LogInfo("OfflineProgression", "$1");
            }
        }

        public void NotifyOfflineProgressionListeners(float offlineHours)
        {
            for (int i = _offlineProgressionListeners.Count - 1; i >= 0; i--)
            {
                try
                {
                    _offlineProgressionListeners[i]?.OnOfflineProgressionStart(TimeSpan.FromHours(offlineHours));
                }
                catch (Exception e)
                {
                    ChimeraLogger.LogInfo("OfflineProgression", "$1");
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
                ChimeraLogger.LogInfo("OfflineProgression", "$1");
                return;
            }

            ChimeraLogger.LogInfo("OfflineProgression", "$1");

            foreach (var listener in _offlineProgressionListeners.ToArray()) // ToArray to avoid modification during iteration
            {
                try
                {
                    listener.OnOfflineProgressionStart(TimeSpan.FromHours(offlineHours));
                    ChimeraLogger.LogInfo("OfflineProgression", "$1");
                }
                catch (Exception ex)
                {
                    ChimeraLogger.LogInfo("OfflineProgression", "$1");
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
