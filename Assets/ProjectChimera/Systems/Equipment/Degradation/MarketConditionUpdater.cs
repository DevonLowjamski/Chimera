// REFACTORED: Market Condition Updater
// Extracted from MarketPricingService for better separation of concerns

using System;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// Handles periodic market condition updates and trend calculations
    /// </summary>
    public class MarketConditionUpdater
    {
        private MarketConditions _currentConditions;
        private readonly float _priceUpdateInterval;
        private float _lastMarketUpdate;
        private float _lastPriceUpdate;

        public MarketConditions CurrentConditions => _currentConditions;

        public MarketConditionUpdater(float priceUpdateInterval)
        {
            _priceUpdateInterval = priceUpdateInterval;
            _currentConditions = new MarketConditions
            {
                Supply = 1f,
                Demand = 1f,
                Trend = 0f,
                Confidence = 1f,
                LastUpdateTime = 0f
            };
            _lastMarketUpdate = 0f;
            _lastPriceUpdate = 0f;
        }

        public void UpdateMarketConditions(float currentTime, Func<float, float, float> getSin, Func<float, float, float> getRandomRange)
        {
            if (currentTime - _lastMarketUpdate < 300f) // Update every 5 minutes
                return;

            _lastMarketUpdate = currentTime;

            // Update supply and demand with realistic fluctuations
            _currentConditions.Supply = Clamp01(0.5f + getSin(currentTime * 0.01f, 0.5f));
            _currentConditions.Demand = Clamp01(0.5f + getSin(currentTime * 0.015f, 0.5f));

            // Calculate market trend based on supply/demand ratio
            float supplyDemandRatio = _currentConditions.Demand / _currentConditions.Supply;
            _currentConditions.Trend = (supplyDemandRatio - 1f) * 0.5f; // -0.5 to +0.5 range

            // Update confidence based on volatility
            float volatilityFactor = Math.Abs(getSin(currentTime * 0.02f, 0.3f));
            _currentConditions.Confidence = Clamp01(1f - volatilityFactor);

            _currentConditions.LastUpdateTime = currentTime;
        }

        public bool ShouldUpdatePrices(float currentTime)
        {
            return currentTime - _lastPriceUpdate >= _priceUpdateInterval;
        }

        public void MarkPricesUpdated(float currentTime)
        {
            _lastPriceUpdate = currentTime;
        }

        public void ProcessPeriodicUpdate(float currentTime, Func<float, float, float> getSin, Func<float, float, float> getRandomRange)
        {
            UpdateMarketConditions(currentTime, getSin, getRandomRange);
        }

        public void SetMarketConditions(MarketConditions conditions)
        {
            _currentConditions = conditions;
        }

        private float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }
}

