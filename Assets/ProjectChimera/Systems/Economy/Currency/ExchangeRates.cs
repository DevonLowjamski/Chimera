using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Economy;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectChimera.Systems.Economy
{
    /// <summary>
    /// Implementation for credit system, loans, investments, and currency exchange
    /// </summary>
    public class ExchangeRates : IExchangeRates
    {
        private CreditAccount _creditAccount = new CreditAccount();
        private List<LoanContract> _activeLoans = new List<LoanContract>();
        private Dictionary<string, Investment> _investments = new Dictionary<string, Investment>();
        private bool _enableCreditSystem = true;
        private ITransactions _transactions;

        public float AvailableCredit => _creditAccount.CreditLimit - _creditAccount.UsedCredit;
        public CreditAccount CreditAccount => _creditAccount;
        public List<LoanContract> ActiveLoans => new List<LoanContract>(_activeLoans);
        public Dictionary<string, Investment> Investments => new Dictionary<string, Investment>(_investments);

        public Action OnCreditLimitReached { get; set; }

        public ExchangeRates(ITransactions transactions = null)
        {
            _transactions = transactions;
        }

        public void Initialize(bool enableCreditSystem, float creditLimit)
        {
            _enableCreditSystem = enableCreditSystem;

            if (_enableCreditSystem)
            {
                InitializeCreditSystem(creditLimit);
            }

            ChimeraLogger.Log("[ExchangeRates] Exchange rates and credit system initialized");
        }

        public void Shutdown()
        {
            _activeLoans.Clear();
            _investments.Clear();
            ChimeraLogger.Log("[ExchangeRates] Exchange rates system shutdown");
        }

        public bool TakeLoan(float amount, float interestRate, int termDays, string purpose = "")
        {
            if (!_enableCreditSystem)
            {
                ChimeraLogger.LogWarning("[ExchangeRates] Credit system is disabled");
                return false;
            }

            if (amount <= 0)
            {
                ChimeraLogger.LogWarning($"[ExchangeRates] Invalid loan amount: {amount}");
                return false;
            }

            // Basic loan approval logic
            if (amount > _creditAccount.CreditLimit * 0.5f) // Max loan is 50% of credit limit
            {
                ChimeraLogger.LogWarning($"[ExchangeRates] Loan amount too large: {amount:F2} > {_creditAccount.CreditLimit * 0.5f:F2}");
                return false;
            }

            var loan = new LoanContract
            {
                LoanId = Guid.NewGuid().ToString(),
                PrincipalAmount = amount,
                InterestRate = interestRate,
                OriginationDate = DateTime.Now,
                MaturityDate = DateTime.Now.AddDays(termDays),
                CurrentBalance = amount,
                TotalPayments = termDays / 30,
                Status = LoanStatus.Active
            };

            _activeLoans.Add(loan);

            // Record the loan as income transaction
            if (_transactions != null)
            {
                var transaction = new Transaction
                {
                    TransactionId = Guid.NewGuid().ToString(),
                    TransactionType = TransactionType.Income,
                    CurrencyType = CurrencyType.Cash,
                    Amount = amount,
                    Category = TransactionCategory.Loan,
                    Description = $"Loan: {purpose}",
                    Timestamp = DateTime.Now,
                    BalanceAfter = 0f // This would be updated by the currency core
                };

                _transactions.RecordTransaction(transaction);
            }

            ChimeraLogger.Log($"[ExchangeRates] Loan approved: ${amount:F2} at {interestRate:P2} for {termDays} days");
            return true;
        }

        public bool MakeInvestment(string investmentType, float amount, float expectedReturn, int maturityDays)
        {
            if (amount <= 0)
            {
                ChimeraLogger.LogWarning($"[ExchangeRates] Invalid investment amount: {amount}");
                return false;
            }

            string investmentId = Guid.NewGuid().ToString();
            var investment = new Investment
            {
                InvestmentId = investmentId,
                InvestmentType = InvestmentType.Bond, // Default investment type
                InitialAmount = amount,
                CurrentValue = amount,
                ExpectedReturn = expectedReturn,
                MaturityDate = DateTime.Now.AddDays(maturityDays),
                RiskLevel = InvestmentRisk.Moderate, // Default risk level
                PurchaseDate = DateTime.Now,
                IsActive = true
            };

            _investments[investmentId] = investment;

            ChimeraLogger.Log($"[ExchangeRates] Investment made: {investmentType} ${amount:F2} expecting {expectedReturn:P2} return");
            return true;
        }

        public void ProcessLoanPayment(LoanContract loan)
        {
            if (loan.Status != LoanStatus.Active) return;

            float payment = loan.MonthlyPayment;
            float interestPayment = loan.CurrentBalance * (loan.InterestRate / 12f);
            float principalPayment = payment - interestPayment;

            loan.CurrentBalance -= principalPayment;
            loan.TotalInterestPaid += interestPayment;

            if (loan.CurrentBalance <= 0)
            {
                loan.CurrentBalance = 0;
                loan.Status = LoanStatus.PaidOff;
                ChimeraLogger.Log($"[ExchangeRates] Loan {loan.LoanId} paid off");
            }

            // Record the payment as expense transaction
            if (_transactions != null)
            {
                var transaction = new Transaction
                {
                    TransactionId = Guid.NewGuid().ToString(),
                    TransactionType = TransactionType.Expense,
                    CurrencyType = CurrencyType.Cash,
                    Amount = payment,
                    Category = ProjectChimera.Data.Economy.TransactionCategory.LoanPayment,
                    Description = $"Loan payment: {loan.Purpose}",
                    Timestamp = DateTime.Now,
                    BalanceAfter = 0f // This would be updated by the currency core
                };

                _transactions.RecordTransaction(transaction);
            }
        }

        public bool SpendWithCredit(float amount, string reason, TransactionCategory category)
        {
            if (!_enableCreditSystem)
            {
                ChimeraLogger.LogWarning("[ExchangeRates] Credit system is disabled");
                return false;
            }

            if (AvailableCredit < amount)
            {
                OnCreditLimitReached?.Invoke();
                ChimeraLogger.LogWarning($"[ExchangeRates] Credit limit reached: {AvailableCredit:F2} < {amount:F2}");
                return false;
            }

            // Use credit for the transaction
            _creditAccount.UsedCredit += (float)amount;
            // _creditAccount.PaymentDue += amount * 1.02f; // PaymentDue property not available

            var transaction = new Transaction
            {
                TransactionId = Guid.NewGuid().ToString(),
                TransactionType = TransactionType.Credit,
                CurrencyType = CurrencyType.Cash,
                Amount = amount,
                Category = category,
                Description = $"{reason} (Credit)",
                Timestamp = DateTime.Now,
                BalanceAfter = 0f
            };

            _transactions?.RecordTransaction(transaction);

            ChimeraLogger.Log($"[ExchangeRates] Used ${amount:F2} credit for {reason}. Credit used: ${_creditAccount.UsedCredit:F2}");
            return true;
        }

        public bool CanAfford(float amount, bool includeCredit = false)
        {
            // This would require access to currency core to check cash balance
            // For now, we'll just check credit availability
            if (includeCredit && _enableCreditSystem)
            {
                return AvailableCredit >= amount;
            }

            return false; // Would need currency core integration for cash check
        }

        public float GetExchangeRate(CurrencyType fromCurrency, CurrencyType toCurrency)
        {
            // Simple exchange rates - in a real system this might be more dynamic
            if (fromCurrency == toCurrency) return 1.0f;

            return (fromCurrency, toCurrency) switch
            {
                (CurrencyType.Cash, CurrencyType.Credits) => 0.1f, // $1 = 0.1 Credits
                (CurrencyType.Credits, CurrencyType.Cash) => 10.0f, // 1 Credit = $10
                (CurrencyType.Cash, CurrencyType.ResearchPoints) => 0.01f, // $1 = 0.01 RP
                (CurrencyType.ResearchPoints, CurrencyType.Cash) => 100.0f, // 1 RP = $100
                _ => 1.0f // Default 1:1 rate
            };
        }

        public bool ExchangeCurrency(CurrencyType fromType, CurrencyType toType, float amount)
        {
            if (fromType == toType) return false;

            float exchangeRate = GetExchangeRate(fromType, toType);
            float convertedAmount = amount * exchangeRate;

            // This would require integration with currency core for actual currency exchange
            ChimeraLogger.Log($"[ExchangeRates] Exchange rate {fromType} → {toType}: {exchangeRate:F4} ({amount:F2} → {convertedAmount:F2})");

            return true; // Would perform actual exchange with currency core integration
        }

        public void UpdateCreditScore()
        {
            // Simple credit score calculation based on payment history and utilization
            float utilizationRatio = _creditAccount.UsedCredit / _creditAccount.CreditLimit;
            int onTimePayments = 0; // Would track payment history

            // Credit score calculation (simplified)
            float baseScore = 750f;
            float utilizationPenalty = utilizationRatio * 100f; // Penalty for high utilization
            float paymentBonus = onTimePayments * 2f; // Bonus for on-time payments

            _creditAccount.CreditScore = (int)Mathf.Clamp(baseScore - utilizationPenalty + paymentBonus, 300f, 850f);
        }

        public void CalculateInterest()
        {
            // Calculate interest on credit account balance
            if (_creditAccount.UsedCredit > 0)
            {
                float dailyInterestRate = _creditAccount.InterestRate / 365f;
                float interestCharge = _creditAccount.UsedCredit * dailyInterestRate;
                _creditAccount.PaymentDue += interestCharge;
            }

            // Update investment values
            foreach (var investment in _investments.Values)
            {
                if (investment.IsActive)
                {
                    // Simple compound interest calculation
                    float dailyRate = investment.ExpectedReturn / 365f;
                    investment.CurrentValue *= (1f + dailyRate);
                }
            }
        }

        public void Tick(float deltaTime)
        {
            // Process loan payments (would check if payment is due)
            foreach (var loan in _activeLoans.Where(l => l.IsActive).ToList())
            {
                // In a real implementation, this would check payment schedules
                // For now, we'll just update interest calculations
            }

            // Calculate daily interest
            // CalculateInterest(); // Commented out until interest calculation is implemented

            // Update credit score periodically
            // UpdateCreditScore(); // Commented out until credit score calculation is implemented
        }

        public void SetTransactionHandler(ITransactions transactions)
        {
            _transactions = transactions;
        }

        private void InitializeCreditSystem(float creditLimit)
        {
            _creditAccount = new CreditAccount
            {
                CreditLimit = creditLimit,
                UsedCredit = 0f,
                InterestRate = 0.12f, // 12% annual
                PaymentDue = 0f,
                LastPaymentDate = DateTime.Now,
                // CreditScore = 750 // Property not available // Start with good credit
            };
        }

        private float CalculateMonthlyPayment(float principal, float annualRate, int termDays)
        {
            float monthlyRate = annualRate / 12f;
            int numPayments = termDays / 30; // Approximate months

            if (monthlyRate == 0) return principal / numPayments;

            return principal * (monthlyRate * Mathf.Pow(1 + monthlyRate, numPayments)) /
                   (Mathf.Pow(1 + monthlyRate, numPayments) - 1);
        }
    }
}
