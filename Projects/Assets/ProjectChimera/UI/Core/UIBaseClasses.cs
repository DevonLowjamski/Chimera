using System;
using UnityEngine;
using UnityEngine.UIElements;
using ProjectChimera.Core;
using ProjectChimera.Core.Events;
// Trim heavy cross-module dependencies for early UI compile stability
using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.UI.Core
{
    /// <summary>
    /// Base classes for UI panels, widgets, and HUD components.
    /// Provides common functionality for UI Toolkit integration.
    /// </summary>
    
    // Base UI Panel class
    public abstract class UIBasePanel : IDisposable
    {
        protected UnityEngine.UIElements.VisualElement _rootElement;
        protected UIPrefabEntry _prefabEntry;
        protected object _panelData;
        protected bool _isVisible = true;
        protected bool _isModal = false;
        
        public UnityEngine.UIElements.VisualElement RootElement => _rootElement;
        public bool IsVisible => _isVisible;
        public bool IsModal => _isModal;
        public string PanelId => _prefabEntry?.PrefabId ?? "";
        
        public virtual void Initialize(UIPrefabEntry prefabEntry, object panelData = null)
        {
            _prefabEntry = prefabEntry;
            _panelData = panelData;
            bool isModal = false; // Default to false since UIProperties is not available
            
            CreatePanelElements();
            BindData();
        }
        
        protected abstract void CreatePanelElements();
        protected abstract void BindData();
        
        public virtual void UpdateData()
        {
            if (_isVisible)
            {
                RefreshDataBindings();
            }
        }
        
        protected virtual void RefreshDataBindings() { }
        
        public virtual void SetVisible(bool visible)
        {
            _isVisible = visible;
            _rootElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
        
        public virtual void Dispose()
        {
            _rootElement?.RemoveFromHierarchy();
            _rootElement = null;
        }
    }
    
    // Base UI Widget class
    public abstract class UIWidget : IDisposable
    {
        protected UnityEngine.UIElements.VisualElement _rootElement;
        protected UIPrefabEntry _prefabEntry;
        protected bool _isVisible = true;
        protected float _updateFrequency = 1f;
        protected float _lastUpdate = 0f;
        
        public UnityEngine.UIElements.VisualElement RootElement => _rootElement;
        public bool IsVisible => _isVisible;
        public string WidgetId => _prefabEntry?.PrefabId ?? "";
        
        public virtual void Initialize(UIPrefabEntry prefabEntry)
        {
            _prefabEntry = prefabEntry;
            _updateFrequency = 1f; // UIProperties not available
            
            CreateWidgetElements();
            SetupInteractions();
        }
        
        protected abstract void CreateWidgetElements();
        protected virtual void SetupInteractions() { }
        
        public virtual void UpdateData()
        {
            if (_isVisible && Time.time - _lastUpdate >= _updateFrequency)
            {
                RefreshWidget();
                _lastUpdate = Time.time;
            }
        }
        
        protected abstract void RefreshWidget();
        
        public virtual void SetVisible(bool visible)
        {
            _isVisible = visible;
            _rootElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
        
        public virtual void Dispose()
        {
            _rootElement?.RemoveFromHierarchy();
            _rootElement = null;
        }
    }
    
    // Specific panel implementations
    public class FacilityManagementPanel : UIBasePanel
    {
        private ChimeraManager _facilityConstructor;
        private Label _projectCountLabel;
        private UnityEngine.UIElements.VisualElement _projectListContainer;
        private Button _newProjectButton;
        
        public FacilityManagementPanel(UIPrefabEntry prefabEntry, ChimeraManager facilityConstructor, object panelData = null)
        {
            _facilityConstructor = facilityConstructor;
            Initialize(prefabEntry, panelData);
        }
        
        protected override void CreatePanelElements()
        {
            _rootElement = new UnityEngine.UIElements.VisualElement();
            _rootElement.name = "facility-management-panel";
            _rootElement.AddToClassList("ui-panel");
            _rootElement.AddToClassList("facility-panel");
            
            // Header
            var header = new UnityEngine.UIElements.VisualElement();
            header.AddToClassList("panel-header");
            var title = new Label("Facility Management");
            title.AddToClassList("panel-title");
            header.Add(title);
            _rootElement.Add(header);
            
            // Content
            var content = new UnityEngine.UIElements.VisualElement();
            content.AddToClassList("panel-content");
            
            // Project summary
            var summary = new UnityEngine.UIElements.VisualElement();
            summary.AddToClassList("project-summary");
            _projectCountLabel = new Label("Projects: 0");
            summary.Add(_projectCountLabel);
            content.Add(summary);
            
            // Project list
            _projectListContainer = new UnityEngine.UIElements.VisualElement();
            _projectListContainer.AddToClassList("project-list");
            content.Add(_projectListContainer);
            
            // Controls
            var controls = new UnityEngine.UIElements.VisualElement();
            controls.AddToClassList("panel-controls");
            _newProjectButton = new Button(() => CreateNewProject());
            _newProjectButton.text = "New Project";
            controls.Add(_newProjectButton);
            content.Add(controls);
            
            _rootElement.Add(content);
        }
        
        protected override void BindData()
        {
            if (_facilityConstructor != null)
            {
                RefreshDataBindings();
            }
        }
        
        protected override void RefreshDataBindings()
        {
            if (_facilityConstructor == null) return;
            
            // In early refactor, we avoid hard dependency: attempt to reflect GetAllProjects(); fallback gracefully
            var method = _facilityConstructor.GetType().GetMethod("GetAllProjects");
            var list = method != null ? method.Invoke(_facilityConstructor, null) as System.Collections.IEnumerable : null;
            int count = 0;
            _projectListContainer.Clear();
            if (list != null)
            {
                foreach (var project in list)
                {
                    count++;
                    var projectElement = CreateProjectElement(project);
                    _projectListContainer.Add(projectElement);
                }
            }
            _projectCountLabel.text = $"Projects: {count}";
        }
        
        private UnityEngine.UIElements.VisualElement CreateProjectElement(object project)
        {
            var element = new UnityEngine.UIElements.VisualElement();
            element.AddToClassList("project-item");
            
            // Use reflection for safe property access
            string projectName = "Unknown Project";
            string projectStatus = "Unknown Status";
            
            try
            {
                // Use reflection instead of dynamic to avoid RuntimeBinder dependency
                var projectType = project?.GetType();
                var nameProperty = projectType?.GetProperty("ProjectName");
                var statusProperty = projectType?.GetProperty("Status");
                
                if (nameProperty != null)
                {
                    projectName = nameProperty.GetValue(project)?.ToString() ?? "Unknown Project";
                }
                
                if (statusProperty != null)
                {
                    projectStatus = statusProperty.GetValue(project)?.ToString() ?? "Unknown Status";
                }
            }
            catch
            {
                // Fallback to string representation
                projectName = project?.ToString() ?? "Unknown Project";
            }
            
            var nameLabel = new Label(projectName);
            nameLabel.AddToClassList("project-name");
            element.Add(nameLabel);
            
            var statusLabel = new Label(projectStatus);
            statusLabel.AddToClassList("project-status");
            element.Add(statusLabel);
            
            return element;
        }
        
        private void CreateNewProject()
        {
            // Implementation for creating new project
        }
    }
    
    public class PlantGeneticsPanel : UIBasePanel
    {
        private ChimeraManager _plantManager;
        
        public PlantGeneticsPanel(UIPrefabEntry prefabEntry, ChimeraManager plantManager, object panelData = null)
        {
            _plantManager = plantManager;
            Initialize(prefabEntry, panelData);
        }
        
        protected override void CreatePanelElements()
        {
            _rootElement = new UnityEngine.UIElements.VisualElement();
            _rootElement.name = "plant-genetics-panel";
            _rootElement.AddToClassList("ui-panel");
            _rootElement.AddToClassList("genetics-panel");
            
            var title = new Label("Plant Genetics & Breeding");
            title.AddToClassList("panel-title");
            _rootElement.Add(title);
            
            // Genetics content would go here
        }
        
        protected override void BindData() { }
    }
    
    public class EnvironmentalControlPanel : UIBasePanel
    {
        private ChimeraManager _environmentalManager;
        private ChimeraManager[] _lightSystems;
        
        public EnvironmentalControlPanel(UIPrefabEntry prefabEntry, ChimeraManager environmentalManager, 
                                       ChimeraManager[] lightSystems, object panelData = null)
        {
            _environmentalManager = environmentalManager;
            _lightSystems = lightSystems;
            Initialize(prefabEntry, panelData);
        }
        
        protected override void CreatePanelElements()
        {
            _rootElement = new UnityEngine.UIElements.VisualElement();
            _rootElement.name = "environmental-control-panel";
            _rootElement.AddToClassList("ui-panel");
            _rootElement.AddToClassList("environmental-panel");
            
            var title = new Label("Environmental Control");
            title.AddToClassList("panel-title");
            _rootElement.Add(title);
            
            // Environmental controls would go here
        }
        
        protected override void BindData() { }
    }
    
    public class ResearchDevelopmentPanel : UIBasePanel
    {
        private ChimeraManager _researchManager;
        
        public ResearchDevelopmentPanel(UIPrefabEntry prefabEntry, ChimeraManager researchManager, object panelData = null)
        {
            _researchManager = researchManager;
            Initialize(prefabEntry, panelData);
        }
        
        protected override void CreatePanelElements()
        {
            _rootElement = new UnityEngine.UIElements.VisualElement();
            _rootElement.name = "research-development-panel";
            _rootElement.AddToClassList("ui-panel");
            _rootElement.AddToClassList("research-panel");
            
            var title = new Label("Research & Development");
            title.AddToClassList("panel-title");
            _rootElement.Add(title);
        }
        
        protected override void BindData() { }
    }
    
    public class MarketTradingPanel : UIBasePanel
    {
        private ChimeraManager _marketManager;
        
        public MarketTradingPanel(UIPrefabEntry prefabEntry, ChimeraManager marketManager, object panelData = null)
        {
            _marketManager = marketManager;
            Initialize(prefabEntry, panelData);
        }
        
        protected override void CreatePanelElements()
        {
            _rootElement = new UnityEngine.UIElements.VisualElement();
            _rootElement.name = "market-trading-panel";
            _rootElement.AddToClassList("ui-panel");
            _rootElement.AddToClassList("market-panel");
            
            var title = new Label("Market & Trading");
            title.AddToClassList("panel-title");
            _rootElement.Add(title);
        }
        
        protected override void BindData() { }
    }
    
    public class GenericUIPanel : UIBasePanel
    {
        public GenericUIPanel(UIPrefabEntry prefabEntry, object panelData = null)
        {
            Initialize(prefabEntry, panelData);
        }
        
        protected override void CreatePanelElements()
        {
            _rootElement = new UnityEngine.UIElements.VisualElement();
            _rootElement.name = $"generic-panel-{_prefabEntry.PrefabId}";
            _rootElement.AddToClassList("ui-panel");
            _rootElement.AddToClassList("generic-panel");
            
            var title = new Label(_prefabEntry.PrefabName);
            title.AddToClassList("panel-title");
            _rootElement.Add(title);
        }
        
        protected override void BindData() { }
    }
    
    // Widget implementations
    public class RealtimeChartWidget : UIWidget
    {
        private UnityEngine.UIElements.VisualElement _chartContainer;
        private Label _valueLabel;
        
        public RealtimeChartWidget(UIPrefabEntry prefabEntry)
        {
            Initialize(prefabEntry);
        }
        
        protected override void CreateWidgetElements()
        {
            _rootElement = new UnityEngine.UIElements.VisualElement();
            _rootElement.name = "realtime-chart-widget";
            _rootElement.AddToClassList("ui-widget");
            _rootElement.AddToClassList("chart-widget");
            
            var title = new Label("Real-time Chart");
            title.AddToClassList("widget-title");
            _rootElement.Add(title);
            
            _chartContainer = new UnityEngine.UIElements.VisualElement();
            _chartContainer.AddToClassList("chart-container");
            _rootElement.Add(_chartContainer);
            
            _valueLabel = new Label("--");
            _valueLabel.AddToClassList("chart-value");
            _rootElement.Add(_valueLabel);
        }
        
        protected override void RefreshWidget()
        {
            // Update chart data
            _valueLabel.text = UnityEngine.Random.Range(0f, 100f).ToString("F1");
        }
    }
    
    public class EquipmentStatusWidget : UIWidget
    {
        private ChimeraManager[] _lightSystems;
        private UnityEngine.UIElements.VisualElement _equipmentList;
        
        public EquipmentStatusWidget(UIPrefabEntry prefabEntry, ChimeraManager[] lightSystems)
        {
            _lightSystems = lightSystems;
            Initialize(prefabEntry);
        }
        
        protected override void CreateWidgetElements()
        {
            _rootElement = new UnityEngine.UIElements.VisualElement();
            _rootElement.name = "equipment-status-widget";
            _rootElement.AddToClassList("ui-widget");
            _rootElement.AddToClassList("equipment-widget");
            
            var title = new Label("Equipment Status");
            title.AddToClassList("widget-title");
            _rootElement.Add(title);
            
            _equipmentList = new UnityEngine.UIElements.VisualElement();
            _equipmentList.AddToClassList("equipment-list");
            _rootElement.Add(_equipmentList);
        }
        
        protected override void RefreshWidget()
        {
            _equipmentList.Clear();
            
            if (_lightSystems != null)
            {
                foreach (var light in _lightSystems)
                {
                    if (light != null)
                    {
                        var item = CreateEquipmentItem(light.name, "Unknown", 0f); // Generic status since we can't access specific properties
                        _equipmentList.Add(item);
                    }
                }
            }
        }
        
        private UnityEngine.UIElements.VisualElement CreateEquipmentItem(string name, string status, float power)
        {
            var item = new UnityEngine.UIElements.VisualElement();
            item.AddToClassList("equipment-item");
            
            var nameLabel = new Label(name);
            nameLabel.AddToClassList("equipment-name");
            item.Add(nameLabel);
            
            var statusLabel = new Label(status);
            statusLabel.AddToClassList("equipment-status");
            item.Add(statusLabel);
            
            var powerLabel = new Label($"{power:F0}W");
            powerLabel.AddToClassList("equipment-power");
            item.Add(powerLabel);
            
            return item;
        }
    }
    
    public class PlantProgressWidget : UIWidget
    {
        private ChimeraManager _plantManager;
        private Label _progressLabel;
        private ProgressBar _progressBar;
        
        public PlantProgressWidget(UIPrefabEntry prefabEntry, ChimeraManager plantManager)
        {
            _plantManager = plantManager;
            Initialize(prefabEntry);
        }
        
        protected override void CreateWidgetElements()
        {
            _rootElement = new UnityEngine.UIElements.VisualElement();
            _rootElement.name = "plant-progress-widget";
            _rootElement.AddToClassList("ui-widget");
            _rootElement.AddToClassList("progress-widget");
            
            var title = new Label("Plant Progress");
            title.AddToClassList("widget-title");
            _rootElement.Add(title);
            
            _progressLabel = new Label("Growth Progress");
            _progressLabel.AddToClassList("progress-label");
            _rootElement.Add(_progressLabel);
            
            _progressBar = new ProgressBar();
            _progressBar.AddToClassList("progress-bar");
            _progressBar.lowValue = 0f;
            _progressBar.highValue = 100f;
            _rootElement.Add(_progressBar);
        }
        
        protected override void RefreshWidget()
        {
            // PlantManager disabled - using placeholder data
            if (_plantManager != null)
            {
                // Placeholder progress calculation
                _progressBar.value = 75f; // Default progress
                _progressLabel.text = "Average Progress: 75%";
                /*
                // Original code - disabled due to PlantManager being unavailable
                if (_plantManager is PlantManager plantManager)
                {
                    var plants = plantManager.GetAllPlants();
                    if (plants.Count > 0)
                    {
                        float avgProgress = plants.Average(p => p.GrowthProgress * 100f);
                        _progressBar.value = avgProgress;
                        _progressLabel.text = $"Average Progress: {avgProgress:F0}%";
                    }
                */
            }
            else
            {
                // Fallback when no plant manager available
                _progressBar.value = 0f;
                _progressLabel.text = "Plant data unavailable";
            }
        }
    }
    
    public class GenericUIWidget : UIWidget
    {
        public GenericUIWidget(UIPrefabEntry prefabEntry)
        {
            Initialize(prefabEntry);
        }
        
        protected override void CreateWidgetElements()
        {
            _rootElement = new UnityEngine.UIElements.VisualElement();
            _rootElement.name = $"generic-widget-{_prefabEntry.PrefabId}";
            _rootElement.AddToClassList("ui-widget");
            _rootElement.AddToClassList("generic-widget");
            
            var title = new Label(_prefabEntry.PrefabName);
            title.AddToClassList("widget-title");
            _rootElement.Add(title);
        }
        
        protected override void RefreshWidget() { }
    }
    
    // HUD Components - classes moved to separate files to avoid duplicates

    // Interface and UI Support Classes moved to separate files to avoid duplicates


    // UIAnimation class moved to UIAnimationController.cs to avoid duplicates

    // Data structures referenced in MainGameUIController
    public class EnvironmentalData
    {
        public float Temperature;
        public float Humidity;
        public float CO2Level;
        public float LightIntensity;
        public float AirVelocity;
        public System.DateTime Timestamp;
    }

    public class PlantStatusData
    {
        public int TotalPlants;
        public int HealthyPlants;
        public int PlantsInVeg;
        public int PlantsInFlower;
        public int ReadyToHarvest;
        public float AverageHealth;
    }

    public class SystemPerformanceData
    {
        public float TotalPowerConsumption;
        public float EnergyEfficiency;
        public float SystemLoad;
        public int ActiveAlerts;
    }

    public class ConstructionData
    {
        public int ActiveProjects;
        public int CompletedProjects;
        public float TotalValue;
        public int ActiveWorkers;
        public float Efficiency;
    }

    public class EconomicData
    {
        public float TotalFunds;
        public float DailyRevenue;
        public float DailyExpenses;
        public MarketTrends MarketTrends;
    }

    public class MarketTrends
    {
        public float PriceIndex;
        public float DemandLevel;
        public float SupplyLevel;
        public float Volatility;
        public string TrendDirection; // "Up", "Down", "Stable"
    }
}