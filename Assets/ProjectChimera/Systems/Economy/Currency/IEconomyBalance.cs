using ProjectChimera.Data.Economy;
using System;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Economy
{
    /// <summary>
    /// Interface for budget tracking, financial analytics, and reporting
    /// </summary>
    public interface IEconomyBalance
    {
        FinancialStatistics Statistics { get; }
        List<FinancialReport> Reports { get; }
        CashFlowData CashFlow { get; }
        
        void CreateBudget(string categoryName, float monthlyLimit, BudgetPeriod period = BudgetPeriod.Monthly);
        void UpdateBudgetTracking(Transaction transaction);
        void CheckBudgetAlerts();
        
        void GenerateFinancialReport();
        void UpdateCashFlowPredictions();
        void CheckFinancialMilestones();
        
        void ProcessRecurringPayments();
        void AddRecurringPayment(string paymentId, float amount);
        void RemoveRecurringPayment(string paymentId);
        
        void DetectCurrencyChanges(Dictionary<CurrencyType, float> currencies, Dictionary<CurrencyType, float> lastKnownBalances);
        
        // Events for financial milestones and budget alerts
        Action<float> OnFinancialMilestone { get; set; }
        Action<string, float, float> OnBudgetAlert { get; set; }
        Action<CurrencyType, float, float> OnCurrencyChanged { get; set; }
        
        void Initialize(bool enableBudgetTracking, bool enableReports, bool enableCashFlowPrediction);
        void Tick(float deltaTime);
        void Shutdown();
    }
}
