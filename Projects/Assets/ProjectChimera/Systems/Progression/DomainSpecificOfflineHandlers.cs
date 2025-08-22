using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace ProjectChimera.Systems.Progression
{
    /// <summary>
    /// Domain-specific offline progression handlers for Project Chimera systems
    /// Handles specialized offline calculations for cultivation, construction, economy, and equipment
    /// </summary>
    
    // ===== CULTIVATION OFFLINE PROVIDER =====
    
    /// <summary>
    /// Handles plant growth, harvest scheduling, and cultivation progression during offline periods
    /// </summary>
    public class CultivationOfflineProvider : IOfflineProgressionProvider
    {
        [Header("Cultivation Configuration")]
        [SerializeField] private float _baseGrowthRate = 1.0f;
        [SerializeField] private float _autoHarvestThreshold = 0.95f;
        [SerializeField] private int _maxPlantsToProcess = 100;
        [SerializeField] private bool _enableAutoHarvest = true;
        [SerializeField] private bool _enableAutoPlanting = false;
        
        private readonly List<OfflineProgressionEvent> _cultivationEvents = new List<OfflineProgressionEvent>();
        
        public string GetProviderId() => "cultivation_offline";
        public float GetPriority() => 0.9f;
        
        public async Task<OfflineProgressionCalculationResult> CalculateOfflineProgressionAsync(TimeSpan offlineTime)
        {
            await Task.Delay(50); // Simulate complex plant calculations
            
            var result = new OfflineProgressionCalculationResult();
            var hours = (float)offlineTime.TotalHours;
            
            try
            {
                // Simulate plant growth progression
                var plantData = await CalculatePlantGrowthAsync(hours);
                result.ProgressionData.Add("plant_growth", plantData);
                
                // Calculate harvests that would have occurred
                var harvestData = await CalculateOfflineHarvestsAsync(hours);
                result.ProgressionData.Add("harvests", harvestData);
                
                // Calculate resource generation from plants
                var resourceData = CalculatePlantResourceGeneration(hours, harvestData);
                foreach (var resource in resourceData)
                {
                    result.ResourceChanges[resource.Key] = resource.Value;
                }
                
                // Add cultivation events
                result.Events.AddRange(_cultivationEvents);
                _cultivationEvents.Clear();
                
                // Generate notifications
                if (harvestData.CompletedHarvests > 0)
                {
                    result.Notifications.Add($"{harvestData.CompletedHarvests} plants were automatically harvested while you were away");
                }
                
                if (plantData.NewGrowthStageTransitions > 0)
                {
                    result.Notifications.Add($"{plantData.NewGrowthStageTransitions} plants advanced to new growth stages");
                }
                
                Debug.Log($"[CultivationOfflineProvider] Processed {hours:F1} hours of cultivation progression");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Cultivation calculation failed: {ex.Message}";
            }
            
            return result;
        }
        
        public async Task ApplyOfflineProgressionAsync(OfflineProgressionResult result)
        {
            await Task.Delay(30);
            
            if (result.ProgressionData.TryGetValue("plant_growth", out var growthObj) && growthObj is PlantGrowthData growthData)
            {
                await ApplyPlantGrowthProgressionAsync(growthData);
            }
            
            if (result.ProgressionData.TryGetValue("harvests", out var harvestObj) && harvestObj is HarvestData harvestData)
            {
                await ApplyHarvestProgressionAsync(harvestData);
            }
            
            Debug.Log($"[CultivationOfflineProvider] Applied cultivation progression for session {result.SessionId}");
        }
        
        private async Task<PlantGrowthData> CalculatePlantGrowthAsync(float hours)
        {
            await Task.Delay(20);
            
            var growthData = new PlantGrowthData();
            
            // Simulate plant growth calculations
            var activePlants = Mathf.Min(_maxPlantsToProcess, UnityEngine.Random.Range(5, 25)); // Simulated active plants
            var growthProgress = hours * _baseGrowthRate * 0.1f; // 10% per hour base rate
            
            growthData.ProcessedPlants = activePlants;
            growthData.AverageGrowthProgress = growthProgress;
            growthData.NewGrowthStageTransitions = Mathf.FloorToInt(activePlants * growthProgress * 0.3f);
            
            // Calculate matured plants
            growthData.MaturedPlants = Mathf.FloorToInt(activePlants * Mathf.Clamp01(growthProgress - 0.8f));
            
            if (growthData.MaturedPlants > 0)
            {
                _cultivationEvents.Add(new OfflineProgressionEvent
                {
                    EventType = "plants_matured",
                    Title = "Plants Matured",
                    Description = $"{growthData.MaturedPlants} plants reached maturity while you were away",
                    Priority = EventPriority.Normal,
                    Timestamp = DateTime.UtcNow.AddHours(-hours * 0.5)
                });
            }
            
            return growthData;
        }
        
        private async Task<HarvestData> CalculateOfflineHarvestsAsync(float hours)
        {
            await Task.Delay(15);
            
            var harvestData = new HarvestData();
            
            if (!_enableAutoHarvest)
            {
                harvestData.AutoHarvestDisabled = true;
                return harvestData;
            }
            
            // Calculate plants ready for harvest
            var plantsReadyForHarvest = Mathf.FloorToInt(hours * 0.5f); // Simulate harvest readiness
            var actualHarvests = Mathf.Min(plantsReadyForHarvest, _maxPlantsToProcess / 4);
            
            harvestData.CompletedHarvests = actualHarvests;
            harvestData.TotalYield = actualHarvests * UnityEngine.Random.Range(15f, 35f);
            harvestData.QualityRating = UnityEngine.Random.Range(0.7f, 0.95f);
            
            // Calculate harvest timing
            for (int i = 0; i < actualHarvests; i++)
            {
                var harvestTime = DateTime.UtcNow.AddHours(-hours + (hours * i / actualHarvests));
                harvestData.HarvestTimes.Add(harvestTime);
            }
            
            if (actualHarvests > 0)
            {
                _cultivationEvents.Add(new OfflineProgressionEvent
                {
                    EventType = "auto_harvest_completed",
                    Title = "Automatic Harvest",
                    Description = $"Harvested {actualHarvests} mature plants (Total yield: {harvestData.TotalYield:F1})",
                    Priority = EventPriority.High,
                    Timestamp = harvestData.HarvestTimes.LastOrDefault()
                });
            }
            
            return harvestData;
        }
        
        private Dictionary<string, float> CalculatePlantResourceGeneration(float hours, HarvestData harvestData)
        {
            var resources = new Dictionary<string, float>();
            
            // Base resource generation from active plants
            resources["biomass"] = hours * 2.5f;
            resources["cultivation_experience"] = hours * 1.2f;
            
            // Harvest-specific resources
            if (harvestData.CompletedHarvests > 0)
            {
                resources["harvested_materials"] = harvestData.TotalYield;
                resources["quality_bonuses"] = harvestData.QualityRating * harvestData.CompletedHarvests * 10f;
                resources["harvest_experience"] = harvestData.CompletedHarvests * 25f;
            }
            
            // Efficiency bonuses for longer offline periods
            if (hours > 24f)
            {
                var efficiencyBonus = Math.Min(0.5f, (hours - 24f) * 0.02f);
                foreach (var key in resources.Keys.ToList())
                {
                    resources[key] *= (1f + efficiencyBonus);
                }
            }
            
            return resources;
        }
        
        private async Task ApplyPlantGrowthProgressionAsync(PlantGrowthData growthData)
        {
            await Task.Delay(10);
            // Apply growth progression to actual plant instances
            // This would integrate with the actual cultivation system
        }
        
        private async Task ApplyHarvestProgressionAsync(HarvestData harvestData)
        {
            await Task.Delay(15);
            // Apply harvest results to inventory and plant states
            // This would integrate with the actual harvest system
        }
    }
    
    // ===== CONSTRUCTION OFFLINE PROVIDER =====
    
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
                
                Debug.Log($"[ConstructionOfflineProvider] Processed {hours:F1} hours of construction progression");
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
            
            Debug.Log($"[ConstructionOfflineProvider] Applied construction progression for session {result.SessionId}");
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
    
    // ===== ECONOMY OFFLINE PROVIDER =====
    
    /// <summary>
    /// Handles market changes, contract fulfillment, and passive income during offline periods
    /// </summary>
    public class EconomyOfflineProvider : IOfflineProgressionProvider
    {
        [Header("Economy Configuration")]
        [SerializeField] private float _marketVolatility = 0.1f;
        [SerializeField] private float _passiveIncomeRate = 5f;
        [SerializeField] private bool _enableAutoTrading = false;
        [SerializeField] private bool _enableContractFulfillment = true;
        [SerializeField] private int _maxContractsToProcess = 10;
        
        private readonly List<OfflineProgressionEvent> _economyEvents = new List<OfflineProgressionEvent>();
        
        public string GetProviderId() => "economy_offline";
        public float GetPriority() => 0.7f;
        
        public async Task<OfflineProgressionCalculationResult> CalculateOfflineProgressionAsync(TimeSpan offlineTime)
        {
            await Task.Delay(60);
            
            var result = new OfflineProgressionCalculationResult();
            var hours = (float)offlineTime.TotalHours;
            
            try
            {
                // Calculate market changes
                var marketData = await CalculateMarketChangesAsync(hours);
                result.ProgressionData.Add("market_changes", marketData);
                
                // Calculate contract fulfillment
                var contractData = await CalculateContractFulfillmentAsync(hours);
                result.ProgressionData.Add("contract_fulfillment", contractData);
                
                // Calculate passive income
                var incomeData = CalculatePassiveIncome(hours, marketData);
                result.ProgressionData.Add("passive_income", incomeData);
                
                // Apply economic resource changes
                result.ResourceChanges["currency"] = incomeData.TotalIncome;
                result.ResourceChanges["market_reputation"] = contractData.ReputationChange;
                
                // Add economy events
                result.Events.AddRange(_economyEvents);
                _economyEvents.Clear();
                
                // Generate notifications
                if (incomeData.TotalIncome > 0)
                {
                    result.Notifications.Add($"Earned {incomeData.TotalIncome:F0} currency from passive income while away");
                }
                
                if (contractData.CompletedContracts > 0)
                {
                    result.Notifications.Add($"{contractData.CompletedContracts} contracts were automatically fulfilled");
                }
                
                if (marketData.SignificantPriceChanges.Count > 0)
                {
                    result.Notifications.Add($"Market prices changed for {marketData.SignificantPriceChanges.Count} commodities");
                }
                
                Debug.Log($"[EconomyOfflineProvider] Processed {hours:F1} hours of economic progression");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Economy calculation failed: {ex.Message}";
            }
            
            return result;
        }
        
        public async Task ApplyOfflineProgressionAsync(OfflineProgressionResult result)
        {
            await Task.Delay(35);
            
            if (result.ProgressionData.TryGetValue("market_changes", out var marketObj) && marketObj is MarketData marketData)
            {
                await ApplyMarketChangesAsync(marketData);
            }
            
            if (result.ProgressionData.TryGetValue("contract_fulfillment", out var contractObj) && contractObj is ContractFulfillmentData contractData)
            {
                await ApplyContractFulfillmentAsync(contractData);
            }
            
            Debug.Log($"[EconomyOfflineProvider] Applied economy progression for session {result.SessionId}");
        }
        
        private async Task<MarketData> CalculateMarketChangesAsync(float hours)
        {
            await Task.Delay(25);
            
            var marketData = new MarketData();
            
            // Simulate market price changes
            var commodities = new[] { "biomass", "processed_materials", "specialized_equipment", "research_data", "automation_components" };
            
            foreach (var commodity in commodities)
            {
                var basePrice = UnityEngine.Random.Range(10f, 100f);
                var volatilityFactor = UnityEngine.Random.Range(-_marketVolatility, _marketVolatility);
                var timeDecay = Mathf.Exp(-hours * 0.01f); // Prices stabilize over time
                
                var priceChange = basePrice * volatilityFactor * timeDecay;
                var newPrice = basePrice + priceChange;
                
                marketData.PriceChanges[commodity] = priceChange;
                marketData.CurrentPrices[commodity] = newPrice;
                
                if (Math.Abs(priceChange / basePrice) > 0.1f) // Significant change > 10%
                {
                    marketData.SignificantPriceChanges.Add(commodity);
                }
            }
            
            // Market trend analysis
            var positiveTrends = marketData.PriceChanges.Count(p => p.Value > 0);
            var negativeTrends = marketData.PriceChanges.Count(p => p.Value < 0);
            
            if (positiveTrends > negativeTrends * 2)
            {
                _economyEvents.Add(new OfflineProgressionEvent
                {
                    EventType = "market_bull",
                    Title = "Bull Market",
                    Description = "Market showed strong upward trends while you were away",
                    Priority = EventPriority.Normal,
                    Timestamp = DateTime.UtcNow.AddHours(-hours * 0.4)
                });
            }
            else if (negativeTrends > positiveTrends * 2)
            {
                _economyEvents.Add(new OfflineProgressionEvent
                {
                    EventType = "market_bear",
                    Title = "Bear Market",
                    Description = "Market experienced downward pressure during offline period",
                    Priority = EventPriority.Normal,
                    Timestamp = DateTime.UtcNow.AddHours(-hours * 0.6)
                });
            }
            
            return marketData;
        }
        
        private async Task<ContractFulfillmentData> CalculateContractFulfillmentAsync(float hours)
        {
            await Task.Delay(20);
            
            var contractData = new ContractFulfillmentData();
            
            if (!_enableContractFulfillment)
            {
                contractData.AutoFulfillmentDisabled = true;
                return contractData;
            }
            
            // Simulate contract completion
            var contractsPerHour = 0.5f;
            var completedContracts = Mathf.FloorToInt(hours * contractsPerHour);
            completedContracts = Mathf.Min(completedContracts, _maxContractsToProcess);
            
            contractData.CompletedContracts = completedContracts;
            contractData.TotalContractValue = completedContracts * UnityEngine.Random.Range(500f, 2000f);
            contractData.ReputationChange = completedContracts * UnityEngine.Random.Range(1f, 5f);
            
            // Generate contract types
            var contractTypes = new[] { "supply", "research", "processing", "logistics", "consultation" };
            for (int i = 0; i < completedContracts; i++)
            {
                var contractType = contractTypes[UnityEngine.Random.Range(0, contractTypes.Length)];
                contractData.CompletedContractTypes.Add(contractType);
            }
            
            if (completedContracts > 0)
            {
                _economyEvents.Add(new OfflineProgressionEvent
                {
                    EventType = "contracts_completed",
                    Title = "Contracts Fulfilled",
                    Description = $"Completed {completedContracts} contracts worth {contractData.TotalContractValue:F0} currency",
                    Priority = EventPriority.High,
                    Timestamp = DateTime.UtcNow.AddHours(-hours * 0.2)
                });
            }
            
            return contractData;
        }
        
        private PassiveIncomeData CalculatePassiveIncome(float hours, MarketData marketData)
        {
            var incomeData = new PassiveIncomeData();
            
            // Base passive income
            var baseIncome = hours * _passiveIncomeRate;
            
            // Market influence on passive income
            var averagePriceChange = marketData.PriceChanges.Values.Average();
            var marketMultiplier = 1f + (averagePriceChange * 0.01f); // 1% change per 1% market movement
            
            incomeData.BaseIncome = baseIncome;
            incomeData.MarketBonus = baseIncome * (marketMultiplier - 1f);
            incomeData.TotalIncome = baseIncome * marketMultiplier;
            
            // Efficiency bonus for longer offline periods
            if (hours > 48f)
            {
                var efficiencyBonus = Math.Min(0.25f, (hours - 48f) * 0.005f); // Up to 25% bonus
                incomeData.EfficiencyBonus = incomeData.TotalIncome * efficiencyBonus;
                incomeData.TotalIncome += incomeData.EfficiencyBonus;
            }
            
            return incomeData;
        }
        
        private async Task ApplyMarketChangesAsync(MarketData marketData)
        {
            await Task.Delay(15);
            // Apply market price changes to the economy system
        }
        
        private async Task ApplyContractFulfillmentAsync(ContractFulfillmentData contractData)
        {
            await Task.Delay(10);
            // Complete contracts and apply rewards
        }
    }
    
    // ===== EQUIPMENT OFFLINE PROVIDER =====
    
    /// <summary>
    /// Handles equipment degradation, maintenance requirements, and equipment-based production during offline periods
    /// </summary>
    public class EquipmentOfflineProvider : IOfflineProgressionProvider
    {
        [Header("Equipment Configuration")]
        [SerializeField] private float _degradationRate = 0.02f; // 2% per hour
        [SerializeField] private float _maintenanceThreshold = 0.3f; // Maintenance needed below 30%
        [SerializeField] private bool _enableAutoMaintenance = false;
        [SerializeField] private bool _enableEquipmentProduction = true;
        [SerializeField] private int _maxEquipmentToProcess = 50;
        
        private readonly List<OfflineProgressionEvent> _equipmentEvents = new List<OfflineProgressionEvent>();
        
        public string GetProviderId() => "equipment_offline";
        public float GetPriority() => 0.7f;
        
        public async Task<OfflineProgressionCalculationResult> CalculateOfflineProgressionAsync(TimeSpan offlineTime)
        {
            await Task.Delay(45);
            
            var result = new OfflineProgressionCalculationResult();
            var hours = (float)offlineTime.TotalHours;
            
            try
            {
                // Calculate equipment degradation
                var degradationData = await CalculateEquipmentDegradationAsync(hours);
                result.ProgressionData.Add("equipment_degradation", degradationData);
                
                // Calculate equipment production
                var productionData = await CalculateEquipmentProductionAsync(hours, degradationData);
                result.ProgressionData.Add("equipment_production", productionData);
                
                // Calculate maintenance requirements
                var maintenanceData = CalculateMaintenanceRequirements(degradationData);
                result.ProgressionData.Add("maintenance_requirements", maintenanceData);
                
                // Apply equipment-related resource changes
                foreach (var production in productionData.ResourceProduction)
                {
                    result.ResourceChanges[production.Key] = production.Value;
                }
                
                if (maintenanceData.MaintenanceCost > 0)
                {
                    result.ResourceChanges["maintenance_materials"] = -maintenanceData.MaintenanceCost;
                }
                
                // Add equipment events
                result.Events.AddRange(_equipmentEvents);
                _equipmentEvents.Clear();
                
                // Generate notifications
                if (maintenanceData.EquipmentNeedingMaintenance > 0)
                {
                    result.Notifications.Add($"{maintenanceData.EquipmentNeedingMaintenance} pieces of equipment require maintenance");
                }
                
                if (degradationData.CriticallyDegradedEquipment > 0)
                {
                    result.Notifications.Add($"{degradationData.CriticallyDegradedEquipment} pieces of equipment are in critical condition");
                }
                
                if (productionData.TotalProductionValue > 0)
                {
                    result.Notifications.Add($"Equipment generated {productionData.TotalProductionValue:F0} value in resources while offline");
                }
                
                Debug.Log($"[EquipmentOfflineProvider] Processed {hours:F1} hours of equipment progression");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Equipment calculation failed: {ex.Message}";
            }
            
            return result;
        }
        
        public async Task ApplyOfflineProgressionAsync(OfflineProgressionResult result)
        {
            await Task.Delay(30);
            
            if (result.ProgressionData.TryGetValue("equipment_degradation", out var degradationObj) && degradationObj is EquipmentDegradationData degradationData)
            {
                await ApplyEquipmentDegradationAsync(degradationData);
            }
            
            if (result.ProgressionData.TryGetValue("equipment_production", out var productionObj) && productionObj is EquipmentProductionData productionData)
            {
                await ApplyEquipmentProductionAsync(productionData);
            }
            
            Debug.Log($"[EquipmentOfflineProvider] Applied equipment progression for session {result.SessionId}");
        }
        
        private async Task<EquipmentDegradationData> CalculateEquipmentDegradationAsync(float hours)
        {
            await Task.Delay(20);
            
            var degradationData = new EquipmentDegradationData();
            
            // Simulate equipment degradation
            var totalEquipment = Mathf.Min(_maxEquipmentToProcess, UnityEngine.Random.Range(10, 30));
            var degradationAmount = hours * _degradationRate;
            
            degradationData.ProcessedEquipment = totalEquipment;
            degradationData.AverageDegradation = degradationAmount;
            
            // Calculate equipment in various condition states
            for (int i = 0; i < totalEquipment; i++)
            {
                var currentCondition = UnityEngine.Random.Range(0.2f, 1.0f);
                var newCondition = Mathf.Max(0.05f, currentCondition - degradationAmount);
                
                degradationData.EquipmentConditions.Add(newCondition);
                
                if (newCondition < _maintenanceThreshold)
                {
                    degradationData.EquipmentNeedingMaintenance++;
                }
                
                if (newCondition < 0.15f)
                {
                    degradationData.CriticallyDegradedEquipment++;
                }
            }
            
            if (degradationData.CriticallyDegradedEquipment > 0)
            {
                _equipmentEvents.Add(new OfflineProgressionEvent
                {
                    EventType = "equipment_critical",
                    Title = "Critical Equipment Condition",
                    Description = $"{degradationData.CriticallyDegradedEquipment} pieces of equipment are in critical condition",
                    Priority = EventPriority.Critical,
                    Timestamp = DateTime.UtcNow.AddHours(-hours * 0.8)
                });
            }
            
            return degradationData;
        }
        
        private async Task<EquipmentProductionData> CalculateEquipmentProductionAsync(float hours, EquipmentDegradationData degradationData)
        {
            await Task.Delay(25);
            
            var productionData = new EquipmentProductionData();
            
            if (!_enableEquipmentProduction)
            {
                productionData.ProductionDisabled = true;
                return productionData;
            }
            
            // Calculate production based on equipment condition
            var averageCondition = degradationData.EquipmentConditions.Count > 0 ? 
                degradationData.EquipmentConditions.Average() : 0.8f;
            
            var baseProductionRate = 3f; // Units per hour per equipment
            var conditionMultiplier = Mathf.Clamp01(averageCondition);
            var actualProductionRate = baseProductionRate * conditionMultiplier;
            
            // Calculate different resource productions
            productionData.ResourceProduction["processed_materials"] = hours * actualProductionRate * degradationData.ProcessedEquipment * 0.5f;
            productionData.ResourceProduction["energy_generated"] = hours * actualProductionRate * degradationData.ProcessedEquipment * 0.3f;
            productionData.ResourceProduction["automation_points"] = hours * actualProductionRate * degradationData.ProcessedEquipment * 0.2f;
            
            productionData.TotalProductionValue = productionData.ResourceProduction.Values.Sum() * 10f; // Assume 10 currency per unit
            productionData.ProductionEfficiency = conditionMultiplier;
            
            if (productionData.TotalProductionValue > 100f)
            {
                _equipmentEvents.Add(new OfflineProgressionEvent
                {
                    EventType = "equipment_production",
                    Title = "Equipment Production",
                    Description = $"Equipment generated {productionData.TotalProductionValue:F0} in resources",
                    Priority = EventPriority.Normal,
                    Timestamp = DateTime.UtcNow.AddHours(-hours * 0.3)
                });
            }
            
            return productionData;
        }
        
        private MaintenanceRequirementData CalculateMaintenanceRequirements(EquipmentDegradationData degradationData)
        {
            var maintenanceData = new MaintenanceRequirementData();
            
            maintenanceData.EquipmentNeedingMaintenance = degradationData.EquipmentNeedingMaintenance;
            maintenanceData.CriticalEquipmentCount = degradationData.CriticallyDegradedEquipment;
            
            // Calculate maintenance costs
            var standardMaintenanceCost = maintenanceData.EquipmentNeedingMaintenance * 50f;
            var criticalMaintenanceCost = maintenanceData.CriticalEquipmentCount * 150f;
            
            maintenanceData.MaintenanceCost = standardMaintenanceCost + criticalMaintenanceCost;
            
            // Auto-maintenance if enabled
            if (_enableAutoMaintenance && maintenanceData.MaintenanceCost > 0)
            {
                maintenanceData.AutoMaintenancePerformed = true;
                
                _equipmentEvents.Add(new OfflineProgressionEvent
                {
                    EventType = "auto_maintenance",
                    Title = "Automatic Maintenance",
                    Description = $"Performed maintenance on {maintenanceData.EquipmentNeedingMaintenance} pieces of equipment",
                    Priority = EventPriority.Normal,
                    Timestamp = DateTime.UtcNow.AddHours(-1)
                });
            }
            
            return maintenanceData;
        }
        
        private async Task ApplyEquipmentDegradationAsync(EquipmentDegradationData degradationData)
        {
            await Task.Delay(15);
            // Apply condition changes to actual equipment instances
        }
        
        private async Task ApplyEquipmentProductionAsync(EquipmentProductionData productionData)
        {
            await Task.Delay(10);
            // Add produced resources to inventory
        }
    }
    
    // ===== DATA STRUCTURES =====
    
    [System.Serializable]
    public class PlantGrowthData
    {
        public int ProcessedPlants;
        public float AverageGrowthProgress;
        public int NewGrowthStageTransitions;
        public int MaturedPlants;
    }
    
    [System.Serializable]
    public class HarvestData
    {
        public int CompletedHarvests;
        public float TotalYield;
        public float QualityRating;
        public List<DateTime> HarvestTimes = new List<DateTime>();
        public bool AutoHarvestDisabled;
    }
    
    [System.Serializable]
    public class ConstructionProjectData
    {
        public int ActiveProjects;
        public float AverageProgressMade;
        public float TotalWorkHours;
        public int StageAdvances;
    }
    
    [System.Serializable]
    public class BuildingCompletionData
    {
        public int CompletedBuildings;
        public float TotalConstructionTime;
        public List<string> CompletedBuildingTypes = new List<string>();
        public bool AutoConstructionDisabled;
    }
    
    [System.Serializable]
    public class MarketData
    {
        public Dictionary<string, float> PriceChanges = new Dictionary<string, float>();
        public Dictionary<string, float> CurrentPrices = new Dictionary<string, float>();
        public List<string> SignificantPriceChanges = new List<string>();
    }
    
    [System.Serializable]
    public class ContractFulfillmentData
    {
        public int CompletedContracts;
        public float TotalContractValue;
        public float ReputationChange;
        public List<string> CompletedContractTypes = new List<string>();
        public bool AutoFulfillmentDisabled;
    }
    
    [System.Serializable]
    public class PassiveIncomeData
    {
        public float BaseIncome;
        public float MarketBonus;
        public float EfficiencyBonus;
        public float TotalIncome;
    }
    
    [System.Serializable]
    public class EquipmentDegradationData
    {
        public int ProcessedEquipment;
        public float AverageDegradation;
        public int EquipmentNeedingMaintenance;
        public int CriticallyDegradedEquipment;
        public List<float> EquipmentConditions = new List<float>();
    }
    
    [System.Serializable]
    public class EquipmentProductionData
    {
        public Dictionary<string, float> ResourceProduction = new Dictionary<string, float>();
        public float TotalProductionValue;
        public float ProductionEfficiency;
        public bool ProductionDisabled;
    }
    
    [System.Serializable]
    public class MaintenanceRequirementData
    {
        public int EquipmentNeedingMaintenance;
        public int CriticalEquipmentCount;
        public float MaintenanceCost;
        public bool AutoMaintenancePerformed;
    }
}