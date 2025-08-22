using UnityEngine;
using ProjectChimera.Systems.Cultivation;
using ProjectChimera.Systems.Economy;
using ProjectChimera.Systems.Environment;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Collection of metric collectors for various game systems
    /// Implements IMetricCollector interface for analytics data gathering
    /// </summary>

    #region Yield/Time Metrics

    /// <summary>
    /// Collects yield per hour metrics from cultivation system
    /// </summary>
    public class YieldPerHourCollector : IMetricCollector
    {
        private CultivationManager _cultivationManager;
        private float _lastYieldAmount;
        private float _lastRecordTime;

        public YieldPerHourCollector(CultivationManager cultivationManager)
        {
            _cultivationManager = cultivationManager;
            _lastRecordTime = Time.time;
        }

        public float CollectMetric()
        {
            if (_cultivationManager == null) return 0f;

            var currentYield = _cultivationManager.TotalYieldHarvested;
            var currentTime = Time.time;
            var timeDelta = (currentTime - _lastRecordTime) / 3600f; // Convert to hours

            if (timeDelta <= 0f) return 0f;

            var yieldDelta = currentYield - _lastYieldAmount;
            var yieldPerHour = yieldDelta / timeDelta;

            _lastYieldAmount = currentYield;
            _lastRecordTime = currentTime;

            return yieldPerHour;
        }
    }

    /// <summary>
    /// Collects total active plants metric
    /// </summary>
    public class ActivePlantsCollector : IMetricCollector
    {
        private CultivationManager _cultivationManager;

        public ActivePlantsCollector(CultivationManager cultivationManager)
        {
            _cultivationManager = cultivationManager;
        }

        public float CollectMetric()
        {
            return _cultivationManager?.ActivePlantCount ?? 0f;
        }
    }

    /// <summary>
    /// Collects average plant health metric
    /// </summary>
    public class PlantHealthCollector : IMetricCollector
    {
        private CultivationManager _cultivationManager;

        public PlantHealthCollector(CultivationManager cultivationManager)
        {
            _cultivationManager = cultivationManager;
        }

        public float CollectMetric()
        {
            return _cultivationManager?.AveragePlantHealth ?? 0f;
        }
    }

    /// <summary>
    /// Collects total harvested plants metric
    /// </summary>
    public class TotalHarvestedCollector : IMetricCollector
    {
        private CultivationManager _cultivationManager;

        public TotalHarvestedCollector(CultivationManager cultivationManager)
        {
            _cultivationManager = cultivationManager;
        }

        public float CollectMetric()
        {
            return _cultivationManager?.TotalPlantsHarvested ?? 0f;
        }
    }

    #endregion

    #region Cash Flow Metrics

    /// <summary>
    /// Collects current cash balance
    /// </summary>
    public class CashBalanceCollector : IMetricCollector
    {
        private CurrencyManager _currencyManager;

        public CashBalanceCollector(CurrencyManager currencyManager)
        {
            _currencyManager = currencyManager;
        }

        public float CollectMetric()
        {
            return _currencyManager?.Cash ?? 0f;
        }
    }

    /// <summary>
    /// Collects total revenue metric (stub implementation)
    /// </summary>
    public class TotalRevenueCollector : IMetricCollector
    {
        private CurrencyManager _currencyManager;
        private float _totalRevenue = 0f;
        private float _lastCashBalance;

        public TotalRevenueCollector(CurrencyManager currencyManager)
        {
            _currencyManager = currencyManager;
            _lastCashBalance = _currencyManager?.Cash ?? 0f;
        }

        public float CollectMetric()
        {
            if (_currencyManager == null) return 0f;

            // Stub implementation: track revenue as positive cash changes
            var currentCash = _currencyManager.Cash;
            var cashChange = currentCash - _lastCashBalance;
            
            if (cashChange > 0f)
            {
                _totalRevenue += cashChange;
            }
            
            _lastCashBalance = currentCash;
            return _totalRevenue;
        }
    }

    /// <summary>
    /// Collects total expenses metric (stub implementation)
    /// </summary>
    public class TotalExpensesCollector : IMetricCollector
    {
        private CurrencyManager _currencyManager;
        private float _totalExpenses = 0f;
        private float _lastCashBalance;

        public TotalExpensesCollector(CurrencyManager currencyManager)
        {
            _currencyManager = currencyManager;
            _lastCashBalance = _currencyManager?.Cash ?? 0f;
        }

        public float CollectMetric()
        {
            if (_currencyManager == null) return 0f;

            // Stub implementation: track expenses as negative cash changes
            var currentCash = _currencyManager.Cash;
            var cashChange = currentCash - _lastCashBalance;
            
            if (cashChange < 0f)
            {
                _totalExpenses += Mathf.Abs(cashChange);
            }
            
            _lastCashBalance = currentCash;
            return _totalExpenses;
        }
    }

    /// <summary>
    /// Collects net cash flow (revenue - expenses)
    /// </summary>
    public class NetCashFlowCollector : IMetricCollector
    {
        private TotalRevenueCollector _revenueCollector;
        private TotalExpensesCollector _expensesCollector;

        public NetCashFlowCollector(CurrencyManager currencyManager)
        {
            _revenueCollector = new TotalRevenueCollector(currencyManager);
            _expensesCollector = new TotalExpensesCollector(currencyManager);
        }

        public float CollectMetric()
        {
            return _revenueCollector.CollectMetric() - _expensesCollector.CollectMetric();
        }
    }

    #endregion

    #region Energy Metrics (Stub Implementation)

    /// <summary>
    /// Enhanced energy usage collector using EnergyTrackingSystem
    /// </summary>
    public class EnergyUsageCollector : IMetricCollector
    {
        private EnergyTrackingSystem _energyTrackingSystem;
        private bool _useTrackingSystem;

        public EnergyUsageCollector(EnvironmentManager environmentManager = null)
        {
            // Try to find or create EnergyTrackingSystem
            _energyTrackingSystem = UnityEngine.Object.FindObjectOfType<EnergyTrackingSystem>();
            
            if (_energyTrackingSystem == null)
            {
                // Create a new GameObject with EnergyTrackingSystem if none exists
                var energyTracker = new GameObject("EnergyTracker");
                _energyTrackingSystem = energyTracker.AddComponent<EnergyTrackingSystem>();
                Object.DontDestroyOnLoad(energyTracker);
            }
            
            _useTrackingSystem = _energyTrackingSystem != null;
        }

        public float CollectMetric()
        {
            if (_useTrackingSystem && _energyTrackingSystem != null)
            {
                return _energyTrackingSystem.CurrentHourlyUsage;
            }

            // Fallback implementation if tracking system isn't available
            var basePower = 100f; // kWh base consumption
            var cultivationManager = UnityEngine.Object.FindObjectOfType<CultivationManager>();
            if (cultivationManager != null)
            {
                var activePlants = cultivationManager.ActivePlantCount;
                basePower += activePlants * 1.3f; // Total power per plant
            }

            return basePower;
        }
    }

    /// <summary>
    /// Enhanced energy efficiency collector
    /// </summary>
    public class EnergyEfficiencyCollector : IMetricCollector
    {
        private CultivationManager _cultivationManager;
        private EnergyTrackingSystem _energyTrackingSystem;

        public EnergyEfficiencyCollector(CultivationManager cultivationManager, EnergyUsageCollector energyCollector)
        {
            _cultivationManager = cultivationManager;
            _energyTrackingSystem = UnityEngine.Object.FindObjectOfType<EnergyTrackingSystem>();
        }

        public float CollectMetric()
        {
            if (_cultivationManager == null) return 0f;

            // Use EnergyTrackingSystem if available
            if (_energyTrackingSystem != null)
            {
                return _energyTrackingSystem.GetEnergyEfficiency();
            }

            // Fallback calculation
            var totalYield = _cultivationManager.TotalYieldHarvested;
            var energyUsage = 100f; // Fallback energy usage

            return energyUsage > 0f ? totalYield / energyUsage : 0f;
        }
    }

    /// <summary>
    /// Collects total energy consumed metric
    /// </summary>
    public class TotalEnergyConsumedCollector : IMetricCollector
    {
        private EnergyTrackingSystem _energyTrackingSystem;

        public TotalEnergyConsumedCollector()
        {
            _energyTrackingSystem = UnityEngine.Object.FindObjectOfType<EnergyTrackingSystem>();
        }

        public float CollectMetric()
        {
            return _energyTrackingSystem?.TotalEnergyConsumed ?? 0f;
        }
    }

    /// <summary>
    /// Collects projected daily energy cost
    /// </summary>
    public class EnergyDailyCostCollector : IMetricCollector
    {
        private EnergyTrackingSystem _energyTrackingSystem;
        private float _electricityRate;

        public EnergyDailyCostCollector(float electricityRate = 0.12f)
        {
            _energyTrackingSystem = UnityEngine.Object.FindObjectOfType<EnergyTrackingSystem>();
            _electricityRate = electricityRate;
        }

        public float CollectMetric()
        {
            return _energyTrackingSystem?.GetProjectedDailyCost(_electricityRate) ?? 0f;
        }
    }

    #endregion

    #region Facility Metrics

    /// <summary>
    /// Calculates facility utilization based on plant capacity
    /// </summary>
    public class FacilityUtilizationCollector : IMetricCollector
    {
        private CultivationManager _cultivationManager;
        private float _maxPlantCapacity = 100f; // Default capacity

        public FacilityUtilizationCollector(CultivationManager cultivationManager, float maxCapacity = 100f)
        {
            _cultivationManager = cultivationManager;
            _maxPlantCapacity = maxCapacity;
        }

        public float CollectMetric()
        {
            if (_cultivationManager == null || _maxPlantCapacity <= 0f) return 0f;

            var activePlants = _cultivationManager.ActivePlantCount;
            return (activePlants / _maxPlantCapacity) * 100f;
        }
    }

    /// <summary>
    /// Tracks operational efficiency based on multiple factors
    /// </summary>
    public class OperationalEfficiencyCollector : IMetricCollector
    {
        private CultivationManager _cultivationManager;
        private CurrencyManager _currencyManager;

        public OperationalEfficiencyCollector(CultivationManager cultivationManager, CurrencyManager currencyManager)
        {
            _cultivationManager = cultivationManager;
            _currencyManager = currencyManager;
        }

        public float CollectMetric()
        {
            if (_cultivationManager == null) return 0f;

            // Simple efficiency calculation: average health * utilization
            var avgHealth = _cultivationManager.AveragePlantHealth;
            var plantCount = _cultivationManager.ActivePlantCount;
            var maxCapacity = 100f; // Default max capacity
            var utilization = plantCount / maxCapacity;

            return avgHealth * utilization * 100f;
        }
    }

    #endregion
}