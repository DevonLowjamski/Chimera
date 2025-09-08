using ProjectChimera.Data.Economy;
using System;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Economy
{
    /// <summary>
    /// Interface for credit system, loans, investments, and currency exchange
    /// </summary>
    public interface IExchangeRates
    {
        float AvailableCredit { get; }
        CreditAccount CreditAccount { get; }
        List<LoanContract> ActiveLoans { get; }
        Dictionary<string, Investment> Investments { get; }

        bool TakeLoan(float amount, float interestRate, int termDays, string purpose = "");
        bool MakeInvestment(string investmentType, float amount, float expectedReturn, int maturityDays);
        void ProcessLoanPayment(LoanContract loan);

        bool SpendWithCredit(float amount, string reason, TransactionCategory category);
        bool CanAfford(float amount, bool includeCredit = false);

        float GetExchangeRate(CurrencyType fromCurrency, CurrencyType toCurrency);
        bool ExchangeCurrency(CurrencyType fromType, CurrencyType toType, float amount);

        void UpdateCreditScore();
        void CalculateInterest();

        // Events for credit-related activities
        Action OnCreditLimitReached { get; set; }

        void Initialize(bool enableCreditSystem, float creditLimit);
        void Tick(float deltaTime);
        void Shutdown();
    }
}
