using ProjectChimera.Core.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace ProjectChimera.Systems.Progression
{
    /// <summary>
    /// Handles building completion, construction project progression during offline periods
    /// </summary>
    public class ConstructionOfflineProvider : IOfflineProgressionProvider
    {
        [Header("Construction Configuration")]
        [SerializeField] private float _constructionSpeedMultiplier = 1.0f;
        [SerializeField] private int _maxConcurrentProjects = 5;
        [SerializeField] private bool _enableAutoConstruction = true;
        [SerializeField] private bool _requireResourcesForConstruction = true;
        
        private readonly List<OfflineProgressionEvent> _constructionEvents = new List<OfflineProgressionEvent>();
        
        public string GetProviderId() => "construction_offline";
        public float GetPriority() => 0.8f;
        
        public async Task<OfflineProgressionCalculationResult> CalculateOfflineProgressionAsync(TimeSpan offlineTime)
        {
            await Task.Delay(40);
            
            var result = new OfflineProgressionCalculationResult();
            var hours = (float)offlineTime.TotalHours;
            
            try
            {
                // Calculate construction project progression
                var projectData = await CalculateConstructionProjectsAsync(hours);
                result.ProgressionData.Add("construction_projects", projectData);
                
                // Calculate building completion
                var buildingData = await CalculateBuildingCompletionAsync(hours);
                result.ProgressionData.Add("building_completion", buildingData);
                
                // Calculate resource consumption for construction
                var resourceConsumption = CalculateConstructionResourceConsumption(projectData, buildingData);
                foreach (var consumption in resourceConsumption)
                {
                    result.ResourceChanges[consumption.Key] = -consumption.Value; // Negative for consumption
                }
                
                // Add construction events
                result.Events.AddRange(_constructionEvents);
                _constructionEvents.Clear();
                
                // Generate notifications
                if (buildingData.CompletedBuildings > 0)
                {
                    result.Notifications.Add($"{buildingData.CompletedBuildings} construction projects completed while you were away");
                }
                
                if (projectData.ActiveProjects > 0)
                {
                    result.Notifications.Add($"{projectData.ActiveProjects} construction projects made progress");
                }
                
                ChimeraLogger.Log($"[ConstructionOfflineProvider] Processed {hours:F1} hours of construction progression");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Construction calculation failed: {ex.Message}";
            }
            
            return result;
        }
        
        public async Task ApplyOfflineProgressionAsync(OfflineProgressionResult result)
        {
            await Task.Delay(25);
            
            if (result.ProgressionData.TryGetValue("construction_projects", out var projectObj) && projectObj is ConstructionProjectData projectData)
            {
                await ApplyConstructionProjectProgressionAsync(projectData);
            }
            
            if (result.ProgressionData.TryGetValue("building_completion", out var buildingObj) && buildingObj is BuildingCompletionData buildingData)
            {
                await ApplyBuildingCompletionAsync(buildingData);
            }
            
            ChimeraLogger.Log($"[ConstructionOfflineProvider] Applied construction progression for session {result.SessionId}");
        }
        
        private async Task<ConstructionProjectData> CalculateConstructionProjectsAsync(float hours)
        {
            await Task.Delay(15);
            
            var projectData = new ConstructionProjectData();
            
            // Simulate active construction projects
            var activeProjects = UnityEngine.Random.Range(1, _maxConcurrentProjects + 1);
            var constructionProgress = hours * _constructionSpeedMultiplier * 0.05f; // 5% per hour base rate
            
            projectData.ActiveProjects = activeProjects;
            projectData.AverageProgressMade = constructionProgress;
            projectData.TotalWorkHours = hours * activeProjects;
            
            // Calculate projects that would advance stages
            projectData.StageAdvances = Mathf.FloorToInt(activeProjects * constructionProgress * 2f);
            
            if (projectData.StageAdvances > 0)
            {
                _constructionEvents.Add(new OfflineProgressionEvent
                {
                    EventType = "construction_stage_advance",
                    Title = "Construction Progress",
                    Description = $"{projectData.StageAdvances} construction stages completed",
                    Priority = EventPriority.Normal,
                    Timestamp = DateTime.UtcNow.AddHours(-hours * 0.3)
                });
            }
            
            return projectData;
        }
        
        private async Task<BuildingCompletionData> CalculateBuildingCompletionAsync(float hours)
        {
            await Task.Delay(20);
            
            var buildingData = new BuildingCompletionData();
            
            if (!_enableAutoConstruction)
            {
                buildingData.AutoConstructionDisabled = true;
                return buildingData;
            }
            
            // Calculate buildings that would complete during offline time
            var completionRate = hours * 0.1f; // Buildings per hour
            var completedBuildings = Mathf.FloorToInt(completionRate);
            
            buildingData.CompletedBuildings = completedBuildings;
            buildingData.TotalConstructionTime = hours;
            
            // Generate completed building types
            var buildingTypes = new[] { "greenhouse", "storage", "processing_facility", "research_lab", "automation_hub" };
            for (int i = 0; i < completedBuildings; i++)
            {
                var buildingType = buildingTypes[UnityEngine.Random.Range(0, buildingTypes.Length)];
                buildingData.CompletedBuildingTypes.Add(buildingType);
            }
            
            if (completedBuildings > 0)
            {
                _constructionEvents.Add(new OfflineProgressionEvent
                {
                    EventType = "building_completed",
                    Title = "Buildings Completed",
                    Description = $"{completedBuildings} buildings finished construction",
                    Priority = EventPriority.High,
                    Timestamp = DateTime.UtcNow.AddHours(-hours * 0.7)
                });
            }
            
            return buildingData;
        }
        
        private Dictionary<string, float> CalculateConstructionResourceConsumption(ConstructionProjectData projectData, BuildingCompletionData buildingData)
        {
            var consumption = new Dictionary<string, float>();
            
            if (!_requireResourcesForConstruction)
                return consumption;
            
            // Resource consumption for ongoing projects
            consumption["construction_materials"] = projectData.TotalWorkHours * 5f;
            consumption["energy"] = projectData.TotalWorkHours * 2f;
            
            // Additional consumption for completed buildings
            if (buildingData.CompletedBuildings > 0)
            {
                consumption["construction_materials"] += buildingData.CompletedBuildings * 100f;
                consumption["specialized_components"] = buildingData.CompletedBuildings * 25f;
            }
            
            return consumption;
        }
        
        private async Task ApplyConstructionProjectProgressionAsync(ConstructionProjectData projectData)
        {
            await Task.Delay(10);
            // Apply construction progress to actual building projects
        }
        
        private async Task ApplyBuildingCompletionAsync(BuildingCompletionData buildingData)
        {
            await Task.Delay(15);
            // Complete buildings and add them to the facility
        }
    }
}
