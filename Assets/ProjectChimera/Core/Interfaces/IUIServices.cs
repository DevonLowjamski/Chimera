using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.UI
{
    /// <summary>
    /// UI service interfaces for dependency injection
    /// Eliminates FindObjectOfType anti-patterns in UI systems
    /// </summary>

    [System.Serializable]
    public class UIPerformanceMetrics
    {
        public float FrameTime;
        public float MemoryUsage;
        public int ActiveUIElements;
        public int DrawCalls;
        public System.DateTime Timestamp;
    }

    [System.Serializable]
    public class CultivationDashboardData
    {
        public int TotalPlants;
        public int HealthyPlants;
        public float AverageHealth;
        public float WaterLevel;
        public float NutrientLevel;
        public float LightIntensity;
    }

    public interface IUIPerformanceMonitor
    {
        bool IsMonitoring { get; }
        bool IsInitialized { get; }
        void Initialize();
        void StartMonitoring();
        void StopMonitoring();
        void RecordFrameData(float frameTime, float memoryUsage);
        UIPerformanceMetrics GetCurrentMetrics();
        UIPerformanceMetrics[] GetMetricsHistory();
        void ClearMetrics();
    }

    public interface ICultivationDashboard
    {
        bool IsInitialized { get; }
        bool IsVisible { get; set; }
        void Initialize();
        void Show();
        void Hide();
        void UpdateData(CultivationDashboardData data);
        void RefreshDisplay();
        CultivationDashboardData GetCurrentData();
    }

    public interface IUIManager
    {
        bool IsInitialized { get; }
        void Initialize();
        void ShowPanel(string panelName);
        void HidePanel(string panelName);
        void TogglePanel(string panelName);
        bool IsPanelVisible(string panelName);
        void RegisterPanel(string panelName, GameObject panel);
        void UnregisterPanel(string panelName);
    }

    public interface IUIAccessibilityManager
    {
        bool IsInitialized { get; }
        bool AccessibilityEnabled { get; set; }
        void Initialize();
        void EnableAccessibility();
        void DisableAccessibility();
        void SetTextScale(float scale);
        void SetHighContrast(bool enabled);
        void SetReducedMotion(bool enabled);
    }
}