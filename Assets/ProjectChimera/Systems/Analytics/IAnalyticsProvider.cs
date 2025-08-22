using System.Collections.Generic;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Base interface for managers that provide analytics data
    /// Allows managers to expose metrics to the analytics system
    /// </summary>
    public interface IAnalyticsProvider
    {
        /// <summary>
        /// Get all available metrics this provider can expose
        /// </summary>
        IEnumerable<string> GetAvailableMetrics();

        /// <summary>
        /// Get the current value for a specific metric
        /// </summary>
        float GetMetricValue(string metricName);

        /// <summary>
        /// Check if this provider supports a specific metric
        /// </summary>
        bool SupportsMetric(string metricName);

        /// <summary>
        /// Get display name for a metric (user-friendly name)
        /// </summary>
        string GetMetricDisplayName(string metricName);

        /// <summary>
        /// Get unit of measurement for a metric
        /// </summary>
        string GetMetricUnit(string metricName);
    }

    /// <summary>
    /// Interface for managers that provide cultivation-related metrics
    /// </summary>
    public interface ICultivationAnalyticsProvider : IAnalyticsProvider
    {
        /// <summary>
        /// Get current plant count by growth stage
        /// </summary>
        Dictionary<string, int> GetPlantCountByStage();

        /// <summary>
        /// Get average plant health across all plants
        /// </summary>
        float GetAveragePlantHealth();

        /// <summary>
        /// Get total yield harvested (lifetime)
        /// </summary>
        float GetTotalYieldHarvested();

        /// <summary>
        /// Get current yield rate (grams per hour)
        /// </summary>
        float GetCurrentYieldRate();

        /// <summary>
        /// Get facility utilization percentage
        /// </summary>
        float GetFacilityUtilization();
    }

    /// <summary>
    /// Interface for managers that provide economic/financial metrics
    /// </summary>
    public interface IEconomicAnalyticsProvider : IAnalyticsProvider
    {
        /// <summary>
        /// Get current cash balance
        /// </summary>
        float GetCurrentCashBalance();

        /// <summary>
        /// Get total revenue (lifetime)
        /// </summary>
        float GetTotalRevenue();

        /// <summary>
        /// Get total expenses (lifetime)
        /// </summary>
        float GetTotalExpenses();

        /// <summary>
        /// Get current daily revenue rate
        /// </summary>
        float GetDailyRevenue();

        /// <summary>
        /// Get current daily expense rate
        /// </summary>
        float GetDailyExpenses();

        /// <summary>
        /// Get profit margin percentage
        /// </summary>
        float GetProfitMargin();
    }

    /// <summary>
    /// Interface for managers that provide environmental metrics
    /// </summary>
    public interface IEnvironmentalAnalyticsProvider : IAnalyticsProvider
    {
        /// <summary>
        /// Get current temperature across all zones
        /// </summary>
        float GetAverageTemperature();

        /// <summary>
        /// Get current humidity across all zones
        /// </summary>
        float GetAverageHumidity();

        /// <summary>
        /// Get current energy consumption (kWh)
        /// </summary>
        float GetCurrentEnergyConsumption();

        /// <summary>
        /// Get energy efficiency (yield per kWh)
        /// </summary>
        float GetEnergyEfficiency();

        /// <summary>
        /// Get environmental stability score
        /// </summary>
        float GetEnvironmentalStability();
    }

    /// <summary>
    /// Interface for managers that provide facility/construction metrics
    /// </summary>
    public interface IFacilityAnalyticsProvider : IAnalyticsProvider
    {
        /// <summary>
        /// Get total facility capacity
        /// </summary>
        float GetTotalFacilityCapacity();

        /// <summary>
        /// Get current facility usage
        /// </summary>
        float GetCurrentFacilityUsage();

        /// <summary>
        /// Get facility expansion cost
        /// </summary>
        float GetExpansionCost();

        /// <summary>
        /// Get equipment efficiency score
        /// </summary>
        float GetEquipmentEfficiency();

        /// <summary>
        /// Get maintenance cost (daily)
        /// </summary>
        float GetMaintenanceCost();
    }

    /// <summary>
    /// Interface for managers that provide operational metrics
    /// </summary>
    public interface IOperationalAnalyticsProvider : IAnalyticsProvider
    {
        /// <summary>
        /// Get operational efficiency score
        /// </summary>
        float GetOperationalEfficiency();

        /// <summary>
        /// Get automation level percentage
        /// </summary>
        float GetAutomationLevel();

        /// <summary>
        /// Get average task completion time
        /// </summary>
        float GetAverageTaskTime();

        /// <summary>
        /// Get error rate percentage
        /// </summary>
        float GetErrorRate();

        /// <summary>
        /// Get system uptime percentage
        /// </summary>
        float GetSystemUptime();
    }
}