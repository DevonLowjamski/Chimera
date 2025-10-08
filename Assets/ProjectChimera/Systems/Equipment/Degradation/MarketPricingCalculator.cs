// REFACTORED: Market Pricing Calculator
// Extracted from MarketPricingService for better separation of concerns

using System;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// Handles market pricing calculations with volatility and trends
    /// </summary>
    public class MarketPricingCalculator
    {
        private readonly Dictionary<MalfunctionType, MarketPriceData> _marketPrices;
        private readonly Dictionary<MalfunctionType, List<PriceHistoryEntry>> _priceHistory;
        private readonly float _marketVolatility;
        private readonly float _marketTrendStrength;
        private readonly bool _useRealisticMarketFluctuations;

        public MarketPricingCalculator(
            Dictionary<MalfunctionType, MarketPriceData> marketPrices,
            Dictionary<MalfunctionType, List<PriceHistoryEntry>> priceHistory,
            float marketVolatility,
            float marketTrendStrength,
            bool useRealisticMarketFluctuations)
        {
            _marketPrices = marketPrices;
            _priceHistory = priceHistory;
            _marketVolatility = marketVolatility;
            _marketTrendStrength = marketTrendStrength;
            _useRealisticMarketFluctuations = useRealisticMarketFluctuations;
        }

        public float ApplyMarketPricing(float baseCost, MalfunctionType type, float realtimeSinceStartup, Func<float, float, float> getSin)
        {
            if (!_marketPrices.ContainsKey(type))
            {
                InitializeMarketPrice(type, baseCost);
            }

            var marketData = _marketPrices[type];

            // Calculate market fluctuation using sine wave
            float fluctuation = getSin(realtimeSinceStartup * _marketVolatility, 1f) * _marketTrendStrength;

            // Apply trend if using realistic fluctuations
            if (_useRealisticMarketFluctuations && _priceHistory.ContainsKey(type))
            {
                var history = _priceHistory[type];
                if (history.Count > 1)
                {
                    // Calculate trend from recent history
                    float trend = CalculatePriceTrend(history);
                    fluctuation += trend * 0.5f; // Trend has half the impact of fluctuation
                }
            }

            // Calculate final price
            float marketMultiplier = 1f + (marketData.Volatility * fluctuation);
            return baseCost * marketMultiplier;
        }

        public void UpdateMarketPrice(MalfunctionType type, float newPrice, float currentTime)
        {
            if (_marketPrices.ContainsKey(type))
            {
                _marketPrices[type] = new MarketPriceData
                {
                    BasePrice = newPrice,
                    CurrentPrice = newPrice,
                    Volatility = _marketPrices[type].Volatility,
                    LastUpdateTime = currentTime,
                    UpdateCount = _marketPrices[type].UpdateCount + 1
                };

                // Update price history
                if (!_priceHistory.ContainsKey(type))
                    _priceHistory[type] = new List<PriceHistoryEntry>();

                _priceHistory[type].Add(new PriceHistoryEntry
                {
                    Price = newPrice,
                    Timestamp = currentTime
                });

                // Limit history size
                int maxHistorySize = 100; // Could be configurable
                if (_priceHistory[type].Count > maxHistorySize)
                {
                    _priceHistory[type].RemoveAt(0);
                }
            }
        }

        private void InitializeMarketPrice(MalfunctionType type, float basePrice)
        {
            _marketPrices[type] = new MarketPriceData
            {
                BasePrice = basePrice,
                CurrentPrice = basePrice,
                Volatility = _marketVolatility,
                LastUpdateTime = 0f,
                UpdateCount = 0
            };
        }

        private float CalculatePriceTrend(List<PriceHistoryEntry> history)
        {
            if (history.Count < 2) return 0f;

            // Simple linear trend calculation
            int lookbackPeriod = Math.Min(history.Count, 10);
            float oldPrice = history[history.Count - lookbackPeriod].Price;
            float newPrice = history[history.Count - 1].Price;

            return (newPrice - oldPrice) / oldPrice;
        }
    }
}

