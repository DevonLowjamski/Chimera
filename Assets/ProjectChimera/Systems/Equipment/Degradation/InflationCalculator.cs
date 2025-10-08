// REFACTORED: Inflation Calculator
// Extracted from MarketPricingService for better separation of concerns

using System;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// Handles inflation calculations with variable rates and economic cycles
    /// </summary>
    public class InflationCalculator
    {
        private InflationTracker _inflationTracker;
        private readonly bool _useVariableInflation;
        private readonly float _inflationVarianceRange;
        private readonly float _economicCycleLength;
        private readonly float _inflationCompoundingInterval;
        private DateTime _lastInflationUpdate;

        public float CurrentInflationRate => _inflationTracker.CurrentRate;

        public InflationCalculator(
            float baseInflationRate,
            bool useVariableInflation,
            float inflationVarianceRange,
            float economicCycleLength,
            float inflationCompoundingInterval)
        {
            _inflationTracker = new InflationTracker
            {
                CurrentRate = baseInflationRate,
                BaseRate = baseInflationRate,
                AccumulatedInflation = 0f,
                LastUpdateTime = DateTime.Now
            };
            _useVariableInflation = useVariableInflation;
            _inflationVarianceRange = inflationVarianceRange;
            _economicCycleLength = economicCycleLength;
            _inflationCompoundingInterval = inflationCompoundingInterval;
            _lastInflationUpdate = DateTime.Now;
        }

        public float ApplyInflation(float baseCost, float currentTime)
        {
            // Update inflation if needed
            UpdateInflationRate(currentTime);

            // Calculate time-based inflation
            float timeSinceStart = currentTime;
            float inflationMultiplier = CalculateCompoundInflation(timeSinceStart);

            return baseCost * inflationMultiplier;
        }

        public void UpdateInflationRate(float currentTime)
        {
            float lastUpdateFloat = (float)(_inflationTracker.LastUpdateTime - DateTime.MinValue).TotalSeconds;

            if (currentTime - lastUpdateFloat < _inflationCompoundingInterval)
                return;

            if (_useVariableInflation)
            {
                // Calculate economic cycle influence
                float cyclePhase = (currentTime % _economicCycleLength) / _economicCycleLength;
                float cycleInfluence = (float)Math.Sin(cyclePhase * 2 * Math.PI);

                // Vary inflation rate based on economic cycle
                float variance = _inflationVarianceRange * cycleInfluence;
                _inflationTracker.CurrentRate = _inflationTracker.BaseRate + variance;

                // Clamp to reasonable bounds
                _inflationTracker.CurrentRate = Clamp(_inflationTracker.CurrentRate, -0.05f, 0.20f);
            }

            // Update accumulated inflation
            float timeDelta = currentTime - lastUpdateFloat;
            if (timeDelta > 0f)
            {
                _inflationTracker.AccumulatedInflation += _inflationTracker.CurrentRate * timeDelta;
            }

            _inflationTracker.LastUpdateTime = DateTime.MinValue.AddSeconds(currentTime);
        }

        public void SetBaseInflationRate(float rate)
        {
            _inflationTracker.BaseRate = rate;
            if (!_useVariableInflation)
            {
                _inflationTracker.CurrentRate = rate;
            }
        }

        public InflationTracker GetInflationTracker()
        {
            return _inflationTracker;
        }

        private float CalculateCompoundInflation(float timePeriod)
        {
            // Compound inflation formula: (1 + r)^t
            float periods = timePeriod / _inflationCompoundingInterval;
            return (float)Math.Pow(1.0 + _inflationTracker.CurrentRate, periods);
        }

        private float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}

