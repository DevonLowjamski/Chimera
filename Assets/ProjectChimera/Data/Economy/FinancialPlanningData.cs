using UnityEngine;
using System;
using System.Collections.Generic;

namespace ProjectChimera.Data.Economy
{
    /// <summary>
    /// Data structures for financial planning, budgeting, and projections
    /// </summary>

    [System.Serializable]
    public class FinancialPlan
    {
        public string PlanId;
        public string PlanName;
        public DateTime PlanStartDate;
        public DateTime PlanEndDate;
        public PlanningHorizon Horizon;
        public List<FinancialGoal> Goals = new List<FinancialGoal>();
        public List<CashFlowProjection> Projections = new List<CashFlowProjection>();
        public BudgetPlan Budget;
        public List<string> Assumptions = new List<string>();
        public FinancialStrategy Strategy;
        public bool IsActive;
        public DateTime LastUpdated;
    }

    [System.Serializable]
    public class FinancialGoal
    {
        public string GoalId;
        public string GoalName;
        public GoalType Type;
        public float TargetAmount;
        public float CurrentAmount;
        public DateTime TargetDate;
        public GoalPriority Priority;
        public bool IsAchieved;
        public List<string> Milestones = new List<string>();
        public Dictionary<string, object> GoalParameters = new Dictionary<string, object>();
    }

    [System.Serializable]
    public class CashFlowProjection
    {
        public string ProjectionId;
        public DateTime ProjectionDate;
        public PlanningHorizon TimeHorizon;
        public List<CashFlowPeriod> Periods = new List<CashFlowPeriod>();
        public List<CashFlowScenario> Scenarios = new List<CashFlowScenario>();
        public float TotalProjectedInflow;
        public float TotalProjectedOutflow;
        public float NetCashFlow;
        public DateTime LastUpdated;
    }

    [System.Serializable]
    public class CashFlowPeriod
    {
        public DateTime StartDate;
        public DateTime EndDate;
        public List<CashFlowItem> Inflows = new List<CashFlowItem>();
        public List<CashFlowItem> Outflows = new List<CashFlowItem>();
        public float TotalInflow;
        public float TotalOutflow;
        public float NetCashFlow;
        public float CumulativeCashFlow;
        public bool IsActual;
    }

    [System.Serializable]
    public class CashFlowItem
    {
        public string ItemId;
        public string Description;
        public float Amount;
        public CashFlowCategory Category;
        public bool IsRecurring;
        public DateTime Date;
        public Dictionary<string, object> ItemDetails = new Dictionary<string, object>();
    }

    [System.Serializable]
    public class BudgetPlan
    {
        public string BudgetId;
        public string BudgetName;
        public BudgetType Type;
        public DateTime BudgetPeriodStart;
        public DateTime BudgetPeriodEnd;
        public List<BudgetCategory> Categories = new List<BudgetCategory>();
        public float TotalBudgetedIncome;
        public float TotalBudgetedExpenses;
        public BudgetStatus Status;
        public List<BudgetAlert> Alerts = new List<BudgetAlert>();
        public DateTime LastReviewed;
    }

    [System.Serializable]
    public class BudgetCategory
    {
        public string CategoryId;
        public string CategoryName;
        public BudgetCategoryType Type;
        public float BudgetedAmount;
        public float ActualAmount;
        public float Variance;
        public List<BudgetLineItem> LineItems = new List<BudgetLineItem>();
        public bool IsFlexible;
        public float Priority;
    }

    [System.Serializable]
    public class BudgetLineItem
    {
        public string LineItemId;
        public string Description;
        public float BudgetedAmount;
        public float ActualAmount;
        public DateTime TransactionDate;
        public string AccountCode;
    }

    [System.Serializable]
    public class BudgetAlert
    {
        public string AlertId;
        public string AlertType;
        public AlertSeverity Severity;
        public string Message;
        public DateTime AlertDate;
        public bool IsRead;
        public string CategoryId;
    }

    [System.Serializable]
    public class CashFlowScenario
    {
        public string ScenarioId;
        public string ScenarioName;
        public ScenarioType Type;
        public float Probability;
        public Dictionary<string, float> Assumptions = new Dictionary<string, float>();
        public List<CashFlowPeriod> ProjectedPeriods = new List<CashFlowPeriod>();
        public float NetImpact;
    }

    [System.Serializable]
    public class FinancialProjections
    {
        public string ProjectionId;
        public List<ProjectionPeriod> Periods = new List<ProjectionPeriod>();
        public SensitivityAnalysis SensitivityAnalysis;
        public Dictionary<string, float> KeyAssumptions = new Dictionary<string, float>();
        public DateTime ProjectionDate;
    }

    [System.Serializable]
    public class ProjectionPeriod
    {
        public DateTime PeriodStart;
        public DateTime PeriodEnd;
        public float ProjectedRevenue;
        public float ProjectedExpenses;
        public float ProjectedProfit;
        public float ProjectedCashFlow;
        public Dictionary<string, float> DetailedProjections = new Dictionary<string, float>();
    }

    [System.Serializable]
    public class SensitivityAnalysis
    {
        public string AnalysisId;
        public List<SensitivityVariable> Variables = new List<SensitivityVariable>();
        public Dictionary<string, float> BaseCase = new Dictionary<string, float>();
        public Dictionary<string, float> BestCase = new Dictionary<string, float>();
        public Dictionary<string, float> WorstCase = new Dictionary<string, float>();
        public DateTime AnalysisDate;
    }

    [System.Serializable]
    public class SensitivityVariable
    {
        public string VariableName;
        public float BaseValue;
        public float MinValue;
        public float MaxValue;
        public float Impact; // Impact on key metrics
        public string VariableType; // "Revenue", "Cost", "Volume", etc.
    }

    [System.Serializable]
    public class FinancialStrategy
    {
        public string StrategyId;
        public string StrategyName;
        public StrategyType Type;
        public StrategyStatus Status;
        public List<string> Objectives = new List<string>();
        public Dictionary<string, float> TargetMetrics = new Dictionary<string, float>();
        public List<string> ActionItems = new List<string>();
        public DateTime ImplementationDate;
        public DateTime ReviewDate;
    }

    [System.Serializable]
    public class SeasonalityAnalysis
    {
        public string AnalysisId;
        public List<SeasonalFactor> SeasonalFactors = new List<SeasonalFactor>();
        public Dictionary<string, float> MonthlyAdjustments = new Dictionary<string, float>();
        public DateTime AnalysisDate;
    }

    [System.Serializable]
    public class SeasonalFactor
    {
        public string FactorName;
        public int Month; // 1-12
        public float AdjustmentFactor; // Multiplier for seasonal adjustment
        public string Description;
    }

    // Financial Planning related enumerations
    public enum GoalType
    {
        SavingsGoal,
        InvestmentGoal,
        DebtReduction,
        RetirementPlanning,
        EducationFunding,
        EmergencyFund,
        MajorPurchase,
        BusinessExpansion,
        TaxOptimization,
        EstatePlanning,
        Other
    }

    public enum GoalPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum PlanningHorizon
    {
        ShortTerm, // < 1 year
        MediumTerm, // 1-5 years
        LongTerm // > 5 years
    }

    public enum CashFlowCategory
    {
        Operating,
        OperatingInflow,
        OperatingOutflow,
        InvestingInflow,
        InvestingOutflow,
        FinancingInflow,
        FinancingOutflow,
        Revenue,
        CostOfGoodsSold,
        OperatingExpenses,
        Taxes,
        Interest,
        Dividends,
        Other
    }

    public enum BudgetType
    {
        Operating,
        Capital,
        Cash,
        Master,
        Flexible,
        Static,
        ZeroBased
    }

    public enum BudgetCategoryType
    {
        Revenue,
        FixedExpense,
        VariableExpense,
        CapitalExpenditure,
        Savings,
        Investment
    }

    public enum BudgetStatus
    {
        Draft,
        Approved,
        Active,
        UnderReview,
        Revised,
        Closed
    }

    public enum ScenarioType
    {
        Base,
        Optimistic,
        Pessimistic,
        Stress,
        Custom
    }

    public enum StrategyType
    {
        AssetAllocation,
        Manual,
        Conservative,
        Balanced,
        Growth,
        Aggressive,
        Defensive,
        ValueBased,
        Custom
    }

    public enum StrategyStatus
    {
        Draft,
        Active,
        UnderReview,
        Paused,
        Completed,
        Cancelled
    }

    public enum AlertSeverity
    {
        Info,
        Warning,
        Critical,
        Emergency
    }
}
