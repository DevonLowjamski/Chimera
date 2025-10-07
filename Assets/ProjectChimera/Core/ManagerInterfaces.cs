using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core
{
    /// <summary>
    /// SIMPLE: Basic manager interfaces aligned with Project Chimera's interface needs.
    /// Focuses on essential interface contracts without complex systems.
    /// </summary>

    /// <summary>
    /// Basic manager priority levels
    /// </summary>
    public enum ManagerPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }

    /// <summary>
    /// Basic time speed options
    /// </summary>
    public enum TimeSpeedLevel
    {
        Slow = 0,      // 0.5x speed
        Normal = 1,    // 1x speed
        Fast = 2,      // 2x speed
        VeryFast = 4,  // 4x speed
        Maximum = 8    // 8x speed
    }

    /// <summary>
    /// Basic manager interface
    /// </summary>
    public interface IManager
    {
        ManagerPriority Priority { get; }
        bool IsInitialized { get; }
        void Initialize();
        void Shutdown();
    }

    /// <summary>
    /// Basic update interface for managers that need per-frame updates.
    /// NOTE: The centralized update system uses ProjectChimera.Core.Updates.ITickable.
    /// This simplified interface is renamed to avoid ambiguity with the unified ITickable.
    /// </summary>
    public interface ISimpleTickable
    {
        void Tick(float deltaTime);
    }

    /// <summary>
    /// Basic time scale listener interface
    /// </summary>
    public interface ITimeScaleListener
    {
        void OnTimeScaleChanged(float newScale);
    }

    /// <summary>
    /// Basic save/load interface
    /// </summary>
    public interface ISaveLoadable
    {
        void SaveData();
        void LoadData();
    }

    /// <summary>
    /// Basic data provider interface
    /// </summary>
    public interface IDataProvider<T>
    {
        T GetData();
        void SetData(T data);
    }

    // IServiceLocator interface moved to SimpleDI/ServiceTypes.cs to avoid duplication

    /// <summary>
    /// Basic event system interface
    /// </summary>
    public interface IEventSystem
    {
        void Subscribe<T>(System.Action<T> handler) where T : struct;
        void Unsubscribe<T>(System.Action<T> handler) where T : struct;
        void Publish<T>(T eventData) where T : struct;
    }

    /// <summary>
    /// Basic time display data
    /// </summary>
    [System.Serializable]
    public struct TimeDisplayData
    {
        public float GameTimeHours;
        public float RealTimeHours;
        public TimeSpeedLevel CurrentSpeedLevel;
        public float CurrentTimeScale;

        public string GetFormattedTime()
        {
            int hours = Mathf.FloorToInt(GameTimeHours);
            int minutes = Mathf.FloorToInt((GameTimeHours - hours) * 60);
            return $"{hours:D2}:{minutes:D2}";
        }
    }

    /// <summary>
    /// Basic game state
    /// </summary>
    [System.Serializable]
    public enum GameState
    {
        Initializing,
        Menu,
        Playing,
        Running,
        Paused,
        Error,
        Loading
    }

    /// <summary>
    /// Basic game state listener
    /// </summary>
    public interface IGameStateListener
    {
        void OnGameStateChanged(GameState newState);
    }

    /// <summary>
    /// Basic input action data
    /// </summary>
    [System.Serializable]
    public struct InputAction
    {
        public string ActionName;
        public float Value;
        public bool IsPressed;
        public Vector2 Position;
    }

    /// <summary>
    /// Basic input handler interface
    /// </summary>
    public interface IInputHandler
    {
        void HandleInput(InputAction action);
        bool CanHandleInput(string actionName);
    }

    /// <summary>
    /// Basic UI manager interface
    /// </summary>
    public interface IUIManager
    {
        void ShowPanel(string panelName);
        void HidePanel(string panelName);
        void UpdateUI();
    }

    /// <summary>
    /// Basic audio manager interface
    /// </summary>
    public interface IAudioManager
    {
        void PlaySound(string soundName);
        void StopSound(string soundName);
        void SetMasterVolume(float volume);
    }

    /// <summary>
    /// Basic notification system interface
    /// </summary>
    public interface INotificationSystem
    {
        void ShowNotification(string message);
        void ShowNotification(string message, float duration);
    }

    /// <summary>
    /// Basic settings interface
    /// </summary>
    public interface ISettingsManager
    {
        T GetSetting<T>(string key);
        void SetSetting<T>(string key, T value);
        void SaveSettings();
        void LoadSettings();
    }
}
