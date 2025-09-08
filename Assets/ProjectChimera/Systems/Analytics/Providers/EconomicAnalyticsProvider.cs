using ProjectChimera.Core.Logging;
using UnityEngine;

namespace ProjectChimera.Systems.Analytics.Providers
{
    /// <summary>
    /// Analytics provider for economic and financial metrics
    /// Integrates with CurrencyManager and economic systems
    /// </summary>
    public class EconomicAnalyticsProvider : AnalyticsProviderBase, IEconomicAnalyticsProvider
    {
        private ICurrencyManager _currencyManager;

        // Financial tracking
        private float _currentBalance = 10000f;
        private float _totalRevenue = 0f;
        private float _totalExpenses = 0f;
        private float _dailyRevenue = 0f;
        private float _dailyExpenses = 0f;

        // Performance tracking
        private float _lastUpdateTime;
        private float _revenueAccumulator;
        private float _expenseAccumulator;

        protected override void Awake()
        {
            _providerName = "EconomicAnalytics";
            base.Awake();
            _enableDebugLogging = true;
            _lastUpdateTime = Time.time;
        }

        #region Initialization

        public void Initialize(ICurrencyManager currencyManager)
        {
            _currencyManager = currencyManager;

            if (_currencyManager != null)
            {
                _currentBalance = _currencyManager.GetCurrentCash();
            }

            if (_enableDebugLogging)
                ChimeraLogger.Log("[EconomicAnalyticsProvider] Initialized with CurrencyManager");
        }

        #endregion

        #region Metric Registration

        protected override void RegisterMetrics()
        {
            // Core financial metrics
            RegisterMetric("CashBalance", "Cash Balance", "$", () => GetCurrentCashBalance());
            RegisterMetric("TotalRevenue", "Total Revenue", "$", () => GetTotalRevenue());
            RegisterMetric("TotalExpenses", "Total Expenses", "$", () => GetTotalExpenses());
            RegisterMetric("NetWorth", "Net Worth", "$", () => GetNetWorth());

            // Flow metrics
            RegisterMetric("DailyRevenue", "Daily Revenue", "$/day", () => GetDailyRevenue());
            RegisterMetric("DailyExpenses", "Daily Expenses", "$/day", () => GetDailyExpenses());
            RegisterMetric("NetCashFlow", "Net Cash Flow", "$/day", () => GetNetCashFlow());
            RegisterMetric("CashFlowRatio", "Cash Flow Ratio", "ratio", () => GetCashFlowRatio());

            // Performance metrics
            RegisterMetric("ProfitMargin", "Profit Margin", "%", () => GetProfitMargin());
            RegisterMetric("ROI", "Return on Investment", "%", () => GetROI());
            RegisterMetric("CashBurnRate", "Cash Burn Rate", "$/day", () => GetCashBurnRate());
            RegisterMetric("BreakevenPoint", "Breakeven Point", "days", () => GetBreakevenPoint());

            // Efficiency metrics
            RegisterMetric("RevenuePerPlant", "Revenue Per Plant", "$/plant", () => GetRevenuePerPlant());
            RegisterMetric("CostPerPlant", "Cost Per Plant", "$/plant", () => GetCostPerPlant());
            RegisterMetric("OperationalEfficiency", "Operational Efficiency", "%", () => GetOperationalEfficiency());
            RegisterMetric("FinancialHealth", "Financial Health", "%", () => GetFinancialHealth());

            // Market metrics
            RegisterMetric("MarketValue", "Market Value", "$", () => GetMarketValue());
            RegisterMetric("AssetUtilization", "Asset Utilization", "%", () => GetAssetUtilization());
        }

        #endregion

        #region IEconomicAnalyticsProvider Implementation

        public float GetCurrentCashBalance()
        {
            if (_currencyManager != null)
            {
                try
                {
                    return _currencyManager.GetBalance();
                }
                catch (System.Exception ex)
                {
                    ChimeraLogger.LogWarning($"[EconomicAnalyticsProvider] Error getting cash balance: {ex.Message}");
                }
            }

            return _currentBalance;
        }

        public float GetTotalRevenue()
        {
            UpdateFinancialTracking();
            return RoundForDisplay(_totalRevenue);
        }

        public float GetTotalExpenses()
        {
            UpdateFinancialTracking();
            return RoundForDisplay(_totalExpenses);
        }

        public float GetDailyRevenue()
        {
            UpdateFinancialTracking();
            return RoundForDisplay(_dailyRevenue);
        }

        public float GetDailyExpenses()
        {
            UpdateFinancialTracking();
            return RoundForDisplay(_dailyExpenses);
        }

        public float GetProfitMargin()
        {
            var revenue = GetDailyRevenue();
            if (revenue <= 0) return 0f;

            var profit = revenue - GetDailyExpenses();
            return RoundForDisplay(CalculatePercentage(profit, revenue));
        }

        #endregion

        #region Financial Calculations

        private void UpdateFinancialTracking()
        {
            var currentTime = Time.time;
            var deltaTime = currentTime - _lastUpdateTime;

            if (deltaTime <= 0) return;

            // Simulate revenue generation based on cultivation
            var hourlyRevenue = SimulateHourlyRevenue();
            var hourlyExpenses = SimulateHourlyExpenses();

            // Update accumulators
            _revenueAccumulator += hourlyRevenue * (deltaTime / 3600f);
            _expenseAccumulator += hourlyExpenses * (deltaTime / 3600f);

            // Update daily rates (smoothed)
            var smoothingFactor = 0.1f;
            _dailyRevenue = Mathf.Lerp(_dailyRevenue, hourlyRevenue * 24f, smoothingFactor);
            _dailyExpenses = Mathf.Lerp(_dailyExpenses, hourlyExpenses * 24f, smoothingFactor);

            // Update totals
            _totalRevenue += _revenueAccumulator;
            _totalExpenses += _expenseAccumulator;

            // Update cash balance
            _currentBalance += _revenueAccumulator - _expenseAccumulator;

            // Reset accumulators
            _revenueAccumulator = 0f;
            _expenseAccumulator = 0f;
            _lastUpdateTime = currentTime;
        }

        private float SimulateHourlyRevenue()
        {
            // Simulate revenue based on harvest sales
            var baseRevenue = 50f; // $50/hour base
            var efficiencyMultiplier = Random.Range(0.8f, 1.2f);
            var marketMultiplier = Mathf.Sin(Time.time * 0.01f) * 0.2f + 1f; // Market fluctuation

            return baseRevenue * efficiencyMultiplier * marketMultiplier;
        }

        private float SimulateHourlyExpenses()
        {
            // Simulate operational expenses
            var baseExpenses = 25f; // $25/hour base
            var facilitySize = GetFacilitySize();
            var sizeMultiplier = 1f + (facilitySize - 50f) / 100f; // Scale with facility size

            return baseExpenses * sizeMultiplier * Random.Range(0.9f, 1.1f);
        }

        private float GetNetWorth()
        {
            return GetCurrentCashBalance() + GetMarketValue() - GetTotalExpenses();
        }

        private float GetNetCashFlow()
        {
            return GetDailyRevenue() - GetDailyExpenses();
        }

        private float GetCashFlowRatio()
        {
            var expenses = GetDailyExpenses();
            return expenses > 0 ? GetDailyRevenue() / expenses : 0f;
        }

        private float GetROI()
        {
            var totalInvestment = GetTotalExpenses();
            if (totalInvestment <= 0) return 0f;

            var profit = GetTotalRevenue() - totalInvestment;
            return CalculatePercentage(profit, totalInvestment);
        }

        private float GetCashBurnRate()
        {
            var netFlow = GetNetCashFlow();
            return netFlow < 0 ? Mathf.Abs(netFlow) : 0f;
        }

        private float GetBreakevenPoint()
        {
            var netFlow = GetNetCashFlow();
            var currentBalance = GetCurrentCashBalance();

            if (netFlow >= 0) return 0f; // Already profitable
            if (netFlow == 0) return float.MaxValue; // Never breaks even

            return currentBalance / Mathf.Abs(netFlow);
        }

        private float GetRevenuePerPlant()
        {
            var plants = GetPlantCount();
            var revenue = GetDailyRevenue();

            return plants > 0 ? revenue / plants : 0f;
        }

        private float GetCostPerPlant()
        {
            var plants = GetPlantCount();
            var expenses = GetDailyExpenses();

            return plants > 0 ? expenses / plants : 0f;
        }

        private float GetOperationalEfficiency()
        {
            var revenuePerPlant = GetRevenuePerPlant();
            var costPerPlant = GetCostPerPlant();

            if (costPerPlant <= 0) return 100f;

            return CalculatePercentage(revenuePerPlant - costPerPlant, revenuePerPlant);
        }

        private float GetFinancialHealth()
        {
            // Composite score of financial health
            var profitMargin = GetProfitMargin();
            var cashRatio = CalculatePercentage(GetCurrentCashBalance(), GetDailyExpenses() * 30f); // 30 days of expenses
            var roiScore = Mathf.Clamp(GetROI(), 0f, 100f);

            return RoundForDisplay((profitMargin + cashRatio + roiScore) / 3f);
        }

        private float GetMarketValue()
        {
            // Simulate market value based on revenue potential
            var annualRevenue = GetDailyRevenue() * 365f;
            var marketMultiplier = 2.5f; // 2.5x revenue as market value

            return annualRevenue * marketMultiplier;
        }

        private float GetAssetUtilization()
        {
            // Simulate asset utilization
            var facilitySize = GetFacilitySize();
            var optimalSize = 75f; // Optimal facility size

            return CalculatePercentage(facilitySize, optimalSize);
        }

        #endregion

        #region Helper Methods

        private float GetPlantCount()
        {
            // Get plant count from analytics if available
            try
            {
                var analyticsService = AnalyticsManager.GetService();
                if (analyticsService != null)
                {
                    return analyticsService.GetMetric("ActivePlants");
                }
            }
            catch (System.Exception ex)
            {
                if (_enableDebugLogging)
                    ChimeraLogger.LogWarning($"[EconomicAnalyticsProvider] Could not get plant count: {ex.Message}");
            }

            return 50f; // Default plant count
        }

        private float GetFacilitySize()
        {
            // Get facility utilization from analytics if available
            try
            {
                var analyticsService = AnalyticsManager.GetService();
                if (analyticsService != null)
                {
                    return analyticsService.GetMetric("FacilityUtilization");
                }
            }
            catch (System.Exception ex)
            {
                if (_enableDebugLogging)
                    ChimeraLogger.LogWarning($"[EconomicAnalyticsProvider] Could not get facility size: {ex.Message}");
            }

            return 60f; // Default facility size
        }

        #endregion

        #region Public Configuration

        public void SetInitialBalance(float balance)
        {
            _currentBalance = balance;

            if (_enableDebugLogging)
                ChimeraLogger.Log($"[EconomicAnalyticsProvider] Initial balance set to ${balance:F2}");
        }

        public void RecordTransaction(float amount, bool isRevenue)
        {
            if (isRevenue)
            {
                _totalRevenue += amount;
                _currentBalance += amount;
            }
            else
            {
                _totalExpenses += amount;
                _currentBalance -= amount;
            }

            if (_enableDebugLogging)
                ChimeraLogger.Log($"[EconomicAnalyticsProvider] Recorded {(isRevenue ? "revenue" : "expense")}: ${amount:F2}");
        }

        #endregion
    }
}
