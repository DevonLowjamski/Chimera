using UnityEngine;
using System;
using System.Collections.Generic;

namespace ProjectChimera.Data.Economy
{
    /// <summary>
    /// LEGACY: Large monolithic financial data structures file - DEPRECATED
    /// This file has been refactored into focused financial domain components.
    /// Use the focused component files instead:
    ///
    /// New focused financial structure:
    /// - LoanManagementData.cs - Loan applications, contracts, payments
    /// - InvestmentManagementData.cs - Investment opportunities, transactions, performance
    /// - FinancialPlanningData.cs - Financial planning, budgeting, projections
    /// - CreditManagementData.cs - Credit profiles, accounts, business plans
    /// - FinancialAnalyticsData.cs - Risk assessment, performance analysis, ratios
    /// - FinancialSystemSettings.cs - System configuration and settings
    ///
    /// All financial data structures have been preserved and organized by domain responsibility.
    /// </summary>

    [System.Serializable]
    public class FinancialSettings
    {
        [Range(0.01f, 0.5f)] public float BaseInterestRate = 0.05f; // 5% annual
        [Range(0.01f, 0.2f)] public float RiskAdjustmentFactor = 0.02f;
        [Range(0.1f, 2f)] public float InflationRate = 0.03f; // 3% annual
        [Range(1000f, 1000000f)] public float MinimumInvestment = 5000f;
        [Range(10000f, 10000000f)] public float MaximumLoanAmount = 500000f;
        public bool EnableDynamicRates = true;
        public bool EnableCreditScoring = true;
        public int MaxActiveLoans = 5;
        public int MaxActiveInvestments = 10;
    }





































    // Supporting classes










    // Enums for financial system






































    // Additional supporting classes






























































    // Additional enums






















}
