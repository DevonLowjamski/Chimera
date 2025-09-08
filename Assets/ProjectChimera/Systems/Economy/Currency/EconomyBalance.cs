using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Economy;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectChimera.Systems.Economy
{
    /// <summary>
    /// Implementation for budget tracking, financial analytics, and reporting
    /// </summary>
    public class EconomyBalance : IEconomyBalance
    {
        private Dictionary<string, Budget> _budgets = new Dictionary<string, Budget>();
        private FinancialStatistics _statistics = new FinancialStatistics();
        private List<FinancialReport> _reports = new List<FinancialReport>();
        private CashFlowData _cashFlow = new CashFlowData();
        private Dictionary<string, float> _recurringPayments = new Dictionary<string, float>();

        private bool _enableBudgetTracking = true;
        private bool _enableFinancialReports = true;
        private bool _enableCashFlowPrediction = true;
        private float _reportGenerationInterval = 3600f; // 1 hour
        private float _lastReportGeneration = 0f;

        public FinancialStatistics Statistics => _statistics;
        public List<FinancialReport> Reports => new List<FinancialReport>(_reports);
        public CashFlowData CashFlow => _cashFlow;

        public Action<float> OnFinancialMilestone { get; set; }
        public Action<string, float, float> OnBudgetAlert { get; set; }
        public Action<CurrencyType, float, float> OnCurrencyChanged { get; set; }

        public void Initialize(bool enableBudgetTracking, bool enableReports, bool enableCashFlowPrediction)
        {
            _enableBudgetTracking = enableBudgetTracking;
            _enableFinancialReports = enableReports;
            _enableCashFlowPrediction = enableCashFlowPrediction;

            if (_enableBudgetTracking)
            {
                InitializeBudgets();
            }

            ChimeraLogger.Log("[EconomyBalance] Economy balance system initialized");
        }

        public void Shutdown()
        {
            _budgets.Clear();
            _reports.Clear();
            _recurringPayments.Clear();
            ChimeraLogger.Log("[EconomyBalance] Economy balance system shutdown");
        }

        public void CreateBudget(string categoryName, float monthlyLimit, BudgetPeriod period = BudgetPeriod.Monthly)
        {
            if (!_enableBudgetTracking) return;

            var budget = new Budget
            {
                CategoryName = categoryName,
                MonthlyLimit = monthlyLimit,
                CurrentSpent = 0f,
                Period = BudgetPeriod.Monthly, // Default to monthly
                StartDate = DateTime.Now.Date,
                IsActive = true
            };

            _budgets[categoryName] = budget;
            ChimeraLogger.Log($"[EconomyBalance] Created budget for {categoryName}: ${monthlyLimit:F2}/{period}");
        }

        public void UpdateBudgetTracking(Transaction transaction)
        {
            if (!_enableBudgetTracking || transaction.TransactionType != TransactionType.Expense) return;

            string categoryName = transaction.Category.ToString();
            if (_budgets.TryGetValue(categoryName, out Budget budget))
            {
                budget.CurrentSpent += transaction.Amount;

                // Check for budget alerts
                float percentageUsed = budget.CurrentSpent / budget.MonthlyLimit;
                if (percentageUsed >= 0.8f) // Alert at 80% of budget
                {
                    OnBudgetAlert?.Invoke(categoryName, budget.CurrentSpent, budget.Limit);
                    ChimeraLogger.LogWarning($"[EconomyBalance] Budget alert for {categoryName}: {percentageUsed:P1} used");
                }
            }
        }

        public void CheckBudgetAlerts()
        {
            if (!_enableBudgetTracking) return;

            foreach (var budget in _budgets.Values)
            {
                if (!budget.IsActive) continue;

                float percentageUsed = budget.CurrentSpent / budget.MonthlyLimit;

                if (percentageUsed >= 1.0f && !budget.HasExceededAlert)
                {
                    OnBudgetAlert?.Invoke(budget.CategoryName, budget.CurrentSpent, budget.MonthlyLimit);
                    budget.HasExceededAlert = true;
                    ChimeraLogger.LogWarning($"[EconomyBalance] Budget exceeded for {budget.CategoryName}: ${budget.CurrentSpent:F2} > ${budget.MonthlyLimit:F2}");
                }
            }
        }

        public void GenerateFinancialReport()
        {
            if (!_enableFinancialReports) return;

            var report = new FinancialReport
            {
                ReportId = Guid.NewGuid().ToString(),
                GeneratedAt = DateTime.Now,
                ReportPeriod = BudgetPeriod.Monthly,
                TotalIncome = _statistics.TotalIncome,
                TotalExpenses = _statistics.TotalExpenses,
                NetIncome = _statistics.TotalIncome - _statistics.TotalExpenses,
                CategoryBreakdown = CalculateCategoryBreakdown(),
                // CashFlowProjection = _cashFlow // Commented out due to type mismatch
            };

            _reports.Add(report);

            // Maintain report history (keep last 12 months)
            if (_reports.Count > 12)
            {
                _reports.RemoveAt(0);
            }

            ChimeraLogger.Log($"[EconomyBalance] Generated financial report: Net Income ${report.NetIncome:F2}");
        }

        public void UpdateCashFlowPredictions()
        {
            if (!_enableCashFlowPrediction) return;

            // Simple cash flow prediction based on historical data
            float avgMonthlyIncome = CalculateAverageMonthlyIncome();
            float avgMonthlyExpenses = CalculateAverageMonthlyExpenses();
            float projectedRecurringExpenses = _recurringPayments.Values.Sum();

            _cashFlow.ProjectedIncome = avgMonthlyIncome;
            _cashFlow.ProjectedExpenses = avgMonthlyExpenses + projectedRecurringExpenses;
            _cashFlow.ProjectedNetFlow = _cashFlow.ProjectedIncome - _cashFlow.ProjectedExpenses;
            _cashFlow.LastUpdated = DateTime.Now;

            if (ChimeraLogger.EnableDebugLogging)
            {
                ChimeraLogger.Log($"[EconomyBalance] Updated cash flow: Net ${_cashFlow.ProjectedNetFlow:F2}/month");
            }
        }

        public void CheckFinancialMilestones()
        {
            // Check for financial milestones (net worth targets)
            float[] milestones = { 50000f, 100000f, 250000f, 500000f, 1000000f };

            // This would need access to current net worth from CurrencyCore
            // For now, we'll use total income as a proxy
            foreach (float milestone in milestones)
            {
                if (_statistics.TotalIncome >= milestone /* && !_statistics.MilestonesReached.Contains(milestone) */ )
                {
                    // _statistics.MilestonesReached.Add(milestone); // Property not available
                    OnFinancialMilestone?.Invoke(milestone);
                    ChimeraLogger.Log($"[EconomyBalance] Financial milestone reached: ${milestone:F0}");
                }
            }
        }

        public void ProcessRecurringPayments()
        {
            foreach (var payment in _recurringPayments.ToList())
            {
                // In a real implementation, this would check payment schedules
                // For now, we'll just log the recurring payments
                if (ChimeraLogger.EnableDebugLogging)
                {
                    ChimeraLogger.Log($"[EconomyBalance] Processing recurring payment: {payment.Key} ${payment.Value:F2}");
                }
            }
        }

        public void AddRecurringPayment(string paymentId, float amount)
        {
            _recurringPayments[paymentId] = amount;
            ChimeraLogger.Log($"[EconomyBalance] Added recurring payment: {paymentId} ${amount:F2}");
        }

        public void RemoveRecurringPayment(string paymentId)
        {
            if (_recurringPayments.Remove(paymentId))
            {
                ChimeraLogger.Log($"[EconomyBalance] Removed recurring payment: {paymentId}");
            }
        }

        public void DetectCurrencyChanges(Dictionary<CurrencyType, float> currencies, Dictionary<CurrencyType, float> lastKnownBalances)
        {
            foreach (var currency in currencies)
            {
                if (lastKnownBalances.TryGetValue(currency.Key, out float lastAmount))
                {
                    if (Mathf.Abs(currency.Value - lastAmount) > 0.01f) // Detect meaningful changes
                    {
                        OnCurrencyChanged?.Invoke(currency.Key, lastAmount, currency.Value);
                        lastKnownBalances[currency.Key] = currency.Value;
                    }
                }
                else
                {
                    lastKnownBalances[currency.Key] = currency.Value;
                }
            }
        }

        public void Tick(float deltaTime)
        {
            float currentTime = Time.time;

            // Generate periodic financial reports
            if (_enableFinancialReports && currentTime - _lastReportGeneration >= _reportGenerationInterval)
            {
                GenerateFinancialReport();
                _lastReportGeneration = currentTime;
            }

            // Process recurring payments
            ProcessRecurringPayments();

            // Update cash flow predictions
            if (_enableCashFlowPrediction)
            {
                UpdateCashFlowPredictions();
            }

            // Check budget alerts
            if (_enableBudgetTracking)
            {
                CheckBudgetAlerts();
            }

            // Check financial milestones
            CheckFinancialMilestones();
        }

        private void InitializeBudgets()
        {
            // Create default budgets
            CreateBudget("Equipment", 5000f);
            CreateBudget("Seeds", 1000f);
            CreateBudget("Utilities", 2000f);
            CreateBudget("Marketing", 1500f);
            CreateBudget("Research", 2500f);
        }

        private Dictionary<string, float> CalculateCategoryBreakdown()
        {
            var breakdown = new Dictionary<string, float>();

            foreach (var budget in _budgets.Values)
            {
                breakdown[budget.CategoryName] = budget.CurrentSpent;
            }

            return breakdown;
        }

        private float CalculateAverageMonthlyIncome()
        {
            if (_reports.Count == 0) return 0f;

            return _reports.TakeLast(6).Average(r => r.TotalIncome); // Last 6 months average
        }

        private float CalculateAverageMonthlyExpenses()
        {
            if (_reports.Count == 0) return 0f;

            return _reports.TakeLast(6).Average(r => r.TotalExpenses); // Last 6 months average
        }
    }
}
