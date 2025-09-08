using ProjectChimera.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace ProjectChimera.Systems.Progression
{
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
                
                ChimeraLogger.Log($"[EconomyOfflineProvider] Processed {hours:F1} hours of economic progression");
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
            
            ChimeraLogger.Log($"[EconomyOfflineProvider] Applied economy progression for session {result.SessionId}");
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
}
