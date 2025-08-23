using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Economy;
using DataTransactionType = ProjectChimera.Data.Economy.TransactionType;
// using ProjectChimera.Data.Progression; // Removed - namespace deleted during cleanup
using System.Collections.Generic;
using System.Linq;
using System;
using ProjectChimera.Core.Events;
using SimpleGameEventSO = ProjectChimera.Data.Events.SimpleGameEventSO;

namespace ProjectChimera.Systems.Economy
{
    /// <summary>
    /// Comprehensive currency management system for Project Chimera.
    /// Handles multiple currency types, transactions, income/expense tracking,
    /// financial analytics, budgeting, and economic balance to create engaging
    /// resource management gameplay with meaningful financial decisions.
    /// </summary>
    public class CurrencyManager : DIChimeraManager
    {
        [Header("Currency Configuration")]
        [SerializeField] private float _startingCash = 25000f;
        [SerializeField] private bool _enableMultipleCurrencies = true;
        [SerializeField] private bool _enableCreditSystem = true;
        [SerializeField] private bool _enableTaxation = false;
        [SerializeField] private float _creditLimit = 50000f;
        
        [Header("Transaction Settings")]
        [SerializeField] private bool _enableTransactionHistory = true;
        [SerializeField] private int _maxTransactionHistory = 1000;
        [SerializeField] private bool _enableTransactionValidation = true;
        [SerializeField] private bool _enableFraudDetection = true;
        
        [Header("Financial Analytics")]
        [SerializeField] private bool _enableFinancialReports = true;
        [SerializeField] private bool _enableBudgetTracking = true;
        [SerializeField] private bool _enableCashFlowPrediction = true;
        [SerializeField] private float _reportGenerationInterval = 3600f; // 1 hour
        
        [Header("Event Channels")]
        [SerializeField] private SimpleGameEventSO _onCurrencyChanged;
        [SerializeField] private SimpleGameEventSO _onTransactionCompleted;
        [SerializeField] private SimpleGameEventSO _onInsufficientFunds;
        [SerializeField] private SimpleGameEventSO _onCreditLimitReached;
        [SerializeField] private SimpleGameEventSO _onFinancialMilestone;
        [SerializeField] private SimpleGameEventSO _onBudgetAlert;
        
        // Core currency data
        private Dictionary<CurrencyType, float> _currencies = new Dictionary<CurrencyType, float>();
        private Dictionary<CurrencyType, CurrencySettings> _currencySettings = new Dictionary<CurrencyType, CurrencySettings>();
        private List<ProjectChimera.Data.Economy.Transaction> _transactionHistory = new List<ProjectChimera.Data.Economy.Transaction>();
        private Dictionary<string, Budget> _budgets = new Dictionary<string, Budget>();
        
        // Financial tracking
        private FinancialStatistics _statistics = new FinancialStatistics();
        private List<FinancialReport> _reports = new List<FinancialReport>();
        private CashFlowData _cashFlow = new CashFlowData();
        private Dictionary<string, float> _recurringPayments = new Dictionary<string, float>();
        
        // Credit and loans
        private CreditAccount _creditAccount = new CreditAccount();
        private List<Loan> _activeLoans = new List<Loan>();
        private Dictionary<string, Investment> _investments = new Dictionary<string, Investment>();
        
        // System references
        // private ProgressionManager _progressionManager; // Removed to prevent circular dependency
        
        // Performance and state
        private float _lastReportGeneration = 0f;
        private Dictionary<CurrencyType, float> _lastKnownBalances = new Dictionary<CurrencyType, float>();
        private TransactionValidator _validator;
        
        public override ManagerPriority Priority => ManagerPriority.High;
        
        // Public Properties
        public float Cash => GetCurrencyAmount(CurrencyType.Cash);
        public float SkillPoints => GetCurrencyAmount(CurrencyType.SkillPoints);
        public float TotalNetWorth => CalculateNetWorth();
        public float AvailableCredit => _creditAccount.CreditLimit - _creditAccount.UsedCredit;
        public bool HasSufficientFunds(float amount) => Cash >= amount;
        public bool HasSufficientSkillPoints(float amount) => SkillPoints >= amount;
        public List<ProjectChimera.Data.Economy.Transaction> RecentTransactions => _transactionHistory.TakeLast(50).ToList();
        public FinancialStatistics Statistics => _statistics;
        public Dictionary<CurrencyType, float> AllCurrencies => new Dictionary<CurrencyType, float>(_currencies);
        
        // Events
        public System.Action<CurrencyType, float, float> OnCurrencyChanged; // type, oldAmount, newAmount
        public System.Action<ProjectChimera.Data.Economy.Transaction> OnTransactionCompleted;
        public System.Action<float, string> OnInsufficientFunds; // amount needed, reason
        public System.Action<float> OnFinancialMilestone; // milestone amount
        public System.Action<string, float, float> OnBudgetAlert; // category, spent, budget
        
        protected override void OnManagerInitialize()
        {
            InitializeSystemReferences();
            InitializeCurrencies();
            InitializeCurrencySettings();
            InitializeBudgets();
            InitializeCreditSystem();
            InitializeValidator();
            
            LogInfo($"CurrencyManager initialized with ${_startingCash:F2} starting cash");
        }
        
        private void Update()
        {
            if (!IsInitialized) return;
            
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
            
            // Detect currency changes for events
            DetectCurrencyChanges();
        }
        
        /// <summary>
        /// Add currency to the player's account
        /// </summary>
        public bool AddCurrency(CurrencyType currencyType, float amount, string reason = "", TransactionCategory category = TransactionCategory.Other)
        {
            if (amount <= 0)
            {
                LogWarning($"Cannot add negative or zero amount: {amount}");
                return false;
            }
            
            float oldAmount = GetCurrencyAmount(currencyType);
            _currencies[currencyType] = oldAmount + amount;
            
            // Record transaction
            var transaction = new ProjectChimera.Data.Economy.Transaction
            {
                TransactionId = Guid.NewGuid().ToString(),
                TransactionType = DataTransactionType.Income,
                CurrencyType = currencyType,
                Amount = amount,
                Category = category,
                Description = reason,
                Timestamp = DateTime.Now,
                BalanceAfter = _currencies[currencyType]
            };
            
            RecordTransaction(transaction);
            
            // Update statistics
            _statistics.TotalIncome += amount;
            _statistics.TransactionCount++;
            UpdateCategoryStatistics(category, amount, true);
            
            // Trigger events
            _onCurrencyChanged?.Invoke();
            OnCurrencyChanged?.Invoke(currencyType, oldAmount, _currencies[currencyType]);
            OnTransactionCompleted?.Invoke(transaction);
            
            // Check for financial milestones
            CheckFinancialMilestones();
            
            LogInfo($"Added ${amount:F2} {currencyType} - {reason}. New balance: ${_currencies[currencyType]:F2}");
            return true;
        }
        
        /// <summary>
        /// Spend currency from the player's account
        /// </summary>
        public bool SpendCurrency(CurrencyType currencyType, float amount, string reason = "", TransactionCategory category = TransactionCategory.Other, bool allowCredit = false)
        {
            if (amount <= 0)
            {
                LogWarning($"Cannot spend negative or zero amount: {amount}");
                return false;
            }
            
            float currentAmount = GetCurrencyAmount(currencyType);
            
            // Check if player has sufficient funds
            if (currentAmount < amount)
            {
                if (allowCredit && _enableCreditSystem && currencyType == CurrencyType.Cash)
                {
                    float creditNeeded = amount - currentAmount;
                    if (AvailableCredit >= creditNeeded)
                    {
                        return SpendWithCredit(amount, reason, category);
                    }
                }
                
                _onInsufficientFunds?.Invoke();
                OnInsufficientFunds?.Invoke(amount - currentAmount, reason);
                LogWarning($"Insufficient funds for {reason}: Need ${amount:F2}, Have ${currentAmount:F2}");
                return false;
            }
            
            // Validate transaction if enabled
            if (_enableTransactionValidation && !_validator.ValidateTransaction(amount, category, reason))
            {
                LogWarning($"Transaction validation failed: {reason}");
                return false;
            }
            
            float oldAmount = currentAmount;
            _currencies[currencyType] = currentAmount - amount;
            
            // Record transaction
            var transaction = new ProjectChimera.Data.Economy.Transaction
            {
                TransactionId = Guid.NewGuid().ToString(),
                TransactionType = DataTransactionType.Expense,
                CurrencyType = currencyType,
                Amount = amount,
                Category = category,
                Description = reason,
                Timestamp = DateTime.Now,
                BalanceAfter = _currencies[currencyType]
            };
            
            RecordTransaction(transaction);
            
            // Update statistics
            _statistics.TotalExpenses += amount;
            _statistics.TransactionCount++;
            UpdateCategoryStatistics(category, amount, false);
            
            // Trigger events
            _onCurrencyChanged?.Invoke();
            OnCurrencyChanged?.Invoke(currencyType, oldAmount, _currencies[currencyType]);
            OnTransactionCompleted?.Invoke(transaction);
            
            LogInfo($"Spent ${amount:F2} {currencyType} - {reason}. New balance: ${_currencies[currencyType]:F2}");
            return true;
        }
        
        /// <summary>
        /// Transfer currency between types or players
        /// </summary>
        public bool TransferCurrency(CurrencyType fromType, CurrencyType toType, float amount, string reason = "")
        {
            if (SpendCurrency(fromType, amount, $"Transfer to {toType}: {reason}", TransactionCategory.Transfer))
            {
                return AddCurrency(toType, amount, $"Transfer from {fromType}: {reason}", TransactionCategory.Transfer);
            }
            return false;
        }
        
        /// <summary>
        /// Get current amount of specific currency
        /// </summary>
        public float GetCurrencyAmount(CurrencyType currencyType)
        {
            return _currencies.TryGetValue(currencyType, out float amount) ? amount : 0f;
        }

        /// <summary>
        /// Get balance (alias for GetCurrencyAmount with Cash)
        /// </summary>
        public float GetBalance()
        {
            return GetCurrencyAmount(CurrencyType.Cash);
        }
        
        /// <summary>
        /// Get balance for specific currency type
        /// </summary>
        public float GetBalance(CurrencyType currencyType)
        {
            return GetCurrencyAmount(currencyType);
        }
        
        /// <summary>
        /// Set currency amount (for loading saves or admin functions)
        /// </summary>
        public void SetCurrencyAmount(CurrencyType currencyType, float amount, string reason = "System Set")
        {
            float oldAmount = GetCurrencyAmount(currencyType);
            _currencies[currencyType] = Mathf.Max(0f, amount);
            
            var transaction = new ProjectChimera.Data.Economy.Transaction
            {
                TransactionId = Guid.NewGuid().ToString(),
                TransactionType = amount > oldAmount ? DataTransactionType.Income : DataTransactionType.Expense,
                CurrencyType = currencyType,
                Amount = Mathf.Abs(amount - oldAmount),
                Category = TransactionCategory.System,
                Description = reason,
                Timestamp = DateTime.Now,
                BalanceAfter = _currencies[currencyType]
            };
            
            RecordTransaction(transaction);
            
            _onCurrencyChanged?.Invoke();
            OnCurrencyChanged?.Invoke(currencyType, oldAmount, _currencies[currencyType]);
            
            LogInfo($"Set {currencyType} to ${amount:F2} - {reason}");
        }
        
        /// <summary>
        /// Create a new budget for expense tracking
        /// </summary>
        public void CreateBudget(string categoryName, float monthlyLimit, BudgetPeriod period = BudgetPeriod.Monthly)
        {
            var budget = new Budget
            {
                BudgetId = Guid.NewGuid().ToString(),
                CategoryName = categoryName,
                Limit = monthlyLimit,
                Period = period,
                StartDate = DateTime.Now,
                CurrentSpent = 0f,
                IsActive = true
            };
            
            _budgets[categoryName] = budget;
            LogInfo($"Created budget for {categoryName}: ${monthlyLimit:F2} per {period}");
        }
        
        /// <summary>
        /// Take out a loan
        /// </summary>
        public bool TakeLoan(float amount, float interestRate, int termDays, string purpose = "")
        {
            if (!_enableCreditSystem) return false;
            
            var loan = new Loan
            {
                LoanId = Guid.NewGuid().ToString(),
                PrincipalAmount = amount,
                InterestRate = interestRate,
                TermDays = termDays,
                StartDate = DateTime.Now,
                Purpose = purpose,
                RemainingBalance = amount,
                IsActive = true
            };
            
            _activeLoans.Add(loan);
            AddCurrency(CurrencyType.Cash, amount, $"Loan: {purpose}", TransactionCategory.Loan);
            
            LogInfo($"Took loan: ${amount:F2} at {interestRate:P2} for {termDays} days");
            return true;
        }
        
        /// <summary>
        /// Make an investment
        /// </summary>
        public bool MakeInvestment(string investmentType, float amount, float expectedReturn, int maturityDays)
        {
            if (!SpendCurrency(CurrencyType.Cash, amount, $"Investment: {investmentType}", TransactionCategory.Investment))
            {
                return false;
            }
            
            var investment = new Investment
            {
                InvestmentId = Guid.NewGuid().ToString(),
                InvestmentName = investmentType,
                InvestmentType = InvestmentType.Technology_Upgrade, // Default type, should be parsed from string
                InvestmentDate = DateTime.Now,
                InitialAmount = amount,
                CurrentValue = amount,
                ExpectedReturn = expectedReturn,
                ActualReturn = 0f,
                RiskLevel = InvestmentRisk.Moderate,
                DurationMonths = maturityDays / 30,
                Status = InvestmentStatus.Active,
                MaturityDate = DateTime.Now.AddDays(maturityDays),
                IsLiquid = true,
                ManagementFee = 0f,
                InvestmentDescription = $"Investment in {investmentType}"
            };
            
            _investments[investment.InvestmentId] = investment;
            
            LogInfo($"Made investment: ${amount:F2} in {investmentType}");
            return true;
        }
        
        /// <summary>
        /// Get detailed financial report
        /// </summary>
        public FinancialReport GetCurrentFinancialReport()
        {
            return new FinancialReport
            {
                ReportId = Guid.NewGuid().ToString(),
                GeneratedDate = DateTime.Now,
                Period = ReportPeriod.Current,
                
                // Balances
                CashBalance = Cash,
                TotalAssets = CalculateTotalAssets(),
                TotalLiabilities = CalculateTotalLiabilities(),
                NetWorth = CalculateNetWorth(),
                
                // Income/Expenses
                TotalIncome = _statistics.TotalIncome,
                TotalExpenses = _statistics.TotalExpenses,
                NetIncome = _statistics.TotalIncome - _statistics.TotalExpenses,
                
                // Performance metrics
                BurnRate = CalculateBurnRate(),
                RunwayDays = CalculateRunwayDays(),
                ProfitMargin = CalculateProfitMargin(),
                
                // Detailed breakdowns
                IncomeByCategory = GetIncomeByCategory(),
                ExpensesByCategory = GetExpensesByCategory(),
                BudgetMonitoring = GetBudgetMonitoringStatus(),
                
                // Predictions
                CashFlowPrediction = _cashFlow
            };
        }
        
        /// <summary>
        /// Get currency display information for UI
        /// </summary>
        public List<CurrencyDisplayData> GetCurrencyDisplayData()
        {
            var displayData = new List<CurrencyDisplayData>();
            
            foreach (var currency in _currencies)
            {
                var settings = _currencySettings[currency.Key];
                displayData.Add(new CurrencyDisplayData
                {
                    CurrencyType = currency.Key,
                    Amount = currency.Value,
                    FormattedAmount = FormatCurrency(currency.Value, currency.Key),
                    Icon = settings.IconString,
                    Color = settings.DisplayColor,
                    IsPositive = currency.Value >= 0,
                    ChangeAmount = GetRecentChange(currency.Key),
                    ChangePercentage = GetRecentChangePercentage(currency.Key)
                });
            }
            
            return displayData;
        }
        
        #region Skill Points Management
        
        /// <summary>
        /// Add skill points to the player's account
        /// </summary>
        public bool AddSkillPoints(float amount, string reason = "")
        {
            return AddCurrency(CurrencyType.SkillPoints, amount, reason, TransactionCategory.System);
        }
        
        /// <summary>
        /// Spend skill points from the player's account
        /// </summary>
        public bool SpendSkillPoints(float amount, string reason = "")
        {
            return SpendCurrency(CurrencyType.SkillPoints, amount, reason, TransactionCategory.System);
        }
        
        /// <summary>
        /// Get current skill points balance
        /// </summary>
        public float GetSkillPointsBalance()
        {
            return GetCurrencyAmount(CurrencyType.SkillPoints);
        }
        
        /// <summary>
        /// Set skill points amount (for loading saves or admin functions)
        /// </summary>
        public void SetSkillPoints(float amount, string reason = "System Set")
        {
            SetCurrencyAmount(CurrencyType.SkillPoints, amount, reason);
        }
        
        /// <summary>
        /// Award skill points for achievements, milestones, or gameplay actions
        /// </summary>
        public bool AwardSkillPoints(float amount, string achievementReason)
        {
            if (amount <= 0)
            {
                LogWarning($"Cannot award negative or zero skill points: {amount}");
                return false;
            }
            
            bool success = AddSkillPoints(amount, $"Achievement: {achievementReason}");
            
            if (success)
            {
                LogInfo($"Awarded {amount} skill points for: {achievementReason}");
            }
            
            return success;
        }
        
        /// <summary>
        /// Purchase a skill or upgrade using skill points
        /// </summary>
        public bool PurchaseWithSkillPoints(float cost, string skillName, string description = "")
        {
            if (cost <= 0)
            {
                LogWarning($"Cannot purchase with negative or zero cost: {cost}");
                return false;
            }
            
            if (!HasSufficientSkillPoints(cost))
            {
                LogWarning($"Insufficient skill points for {skillName}: Need {cost}, Have {SkillPoints}");
                return false;
            }
            
            bool success = SpendSkillPoints(cost, $"Purchased {skillName}: {description}");
            
            if (success)
            {
                LogInfo($"Purchased {skillName} for {cost} skill points");
            }
            
            return success;
        }
        
        #endregion
        
        private void InitializeSystemReferences()
        {
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                // _progressionManager = gameManager.GetManager<ProgressionManager>();
            }
        }
        
        private void InitializeCurrencies()
        {
            // Initialize all currency types
            _currencies[CurrencyType.Cash] = _startingCash;
            
            if (_enableMultipleCurrencies)
            {
                _currencies[CurrencyType.Credits] = 0f;
                _currencies[CurrencyType.ResearchPoints] = 0f;
                _currencies[CurrencyType.ReputationPoints] = 0f;
                _currencies[CurrencyType.SkillPoints] = 0f; // Start with 0 skill points
            }
            
            // Initialize last known balances for change detection
            _lastKnownBalances = new Dictionary<CurrencyType, float>(_currencies);
        }
        
        private void InitializeCurrencySettings()
        {
            _currencySettings[CurrencyType.Cash] = new CurrencySettings
            {
                Name = "Cash",
                Symbol = "$",
                IconString = "💰",
                DisplayColor = new Color(0.2f, 0.8f, 0.2f, 1f),
                DecimalPlaces = 2,
                IsExchangeable = true
            };
            
            _currencySettings[CurrencyType.Credits] = new CurrencySettings
            {
                Name = "Credits",
                Symbol = "CR",
                IconString = "🪙",
                DisplayColor = new Color(0.8f, 0.6f, 0.2f, 1f),
                DecimalPlaces = 0,
                IsExchangeable = true
            };
            
            _currencySettings[CurrencyType.ResearchPoints] = new CurrencySettings
            {
                Name = "Research Points",
                Symbol = "RP",
                IconString = "🔬",
                DisplayColor = new Color(0.2f, 0.6f, 0.8f, 1f),
                DecimalPlaces = 0,
                IsExchangeable = false
            };
            
            _currencySettings[CurrencyType.ReputationPoints] = new CurrencySettings
            {
                Name = "Reputation",
                Symbol = "REP",
                IconString = "⭐",
                DisplayColor = new Color(0.8f, 0.2f, 0.8f, 1f),
                DecimalPlaces = 0,
                IsExchangeable = false
            };
            
            _currencySettings[CurrencyType.SkillPoints] = new CurrencySettings
            {
                Name = "Skill Points",
                Symbol = "SP",
                IconString = "🎯",
                DisplayColor = new Color(0.9f, 0.7f, 0.2f, 1f),
                DecimalPlaces = 0,
                IsExchangeable = false
            };
        }
        
        private void InitializeBudgets()
        {
            if (!_enableBudgetTracking) return;
            
            // Create default budgets
            CreateBudget("Equipment", 5000f);
            CreateBudget("Seeds", 1000f);
            CreateBudget("Utilities", 2000f);
            CreateBudget("Marketing", 1500f);
            CreateBudget("Research", 2500f);
        }
        
        private void InitializeCreditSystem()
        {
            if (!_enableCreditSystem) return;
            
            _creditAccount = new CreditAccount
            {
                CreditLimit = _creditLimit,
                UsedCredit = 0f,
                InterestRate = 0.12f, // 12% annual
                PaymentDue = 0f,
                LastPaymentDate = DateTime.Now,
                CreditScore = 750 // Start with good credit
            };
        }
        
        private void InitializeValidator()
        {
            _validator = new TransactionValidator();
        }
        
        private bool SpendWithCredit(float amount, string reason, TransactionCategory category)
        {
            float cashAvailable = Cash;
            float creditNeeded = amount - cashAvailable;
            
            if (AvailableCredit < creditNeeded)
            {
                _onCreditLimitReached?.Invoke();
                return false;
            }
            
            // Spend all available cash first
            if (cashAvailable > 0)
            {
                SpendCurrency(CurrencyType.Cash, cashAvailable, $"{reason} (Cash portion)", category);
            }
            
            // Use credit for the remainder
            _creditAccount.UsedCredit += creditNeeded;
            _creditAccount.PaymentDue += creditNeeded * 1.02f; // Add 2% transaction fee
            
            var transaction = new Transaction
            {
                TransactionId = Guid.NewGuid().ToString(),
                TransactionType = DataTransactionType.Credit,
                CurrencyType = CurrencyType.Cash,
                Amount = creditNeeded,
                Category = category,
                Description = $"{reason} (Credit)",
                Timestamp = DateTime.Now,
                BalanceAfter = 0f
            };
            
            RecordTransaction(transaction);
            
            LogInfo($"Used ${creditNeeded:F2} credit for {reason}. Credit used: ${_creditAccount.UsedCredit:F2}");
            return true;
        }
        
        private void RecordTransaction(ProjectChimera.Data.Economy.Transaction transaction)
        {
            if (!_enableTransactionHistory) return;
            
            _transactionHistory.Add(transaction);
            
            // Limit history size
            if (_transactionHistory.Count > _maxTransactionHistory)
            {
                _transactionHistory.RemoveRange(0, _transactionHistory.Count - _maxTransactionHistory);
            }
            
            // Update budget tracking
            UpdateBudgetTracking(transaction);
        }
        
        private void UpdateBudgetTracking(ProjectChimera.Data.Economy.Transaction transaction)
        {
            if (!_enableBudgetTracking || transaction.TransactionType != DataTransactionType.Expense) return;
            
            string categoryName = transaction.Category.ToString();
            if (_budgets.TryGetValue(categoryName, out var budget))
            {
                budget.CurrentSpent += transaction.Amount;
            }
        }
        
        private void UpdateCategoryStatistics(TransactionCategory category, float amount, bool isIncome)
        {
            if (isIncome)
            {
                if (!_statistics.IncomeByCategory.ContainsKey(category))
                    _statistics.IncomeByCategory[category] = 0f;
                _statistics.IncomeByCategory[category] += amount;
            }
            else
            {
                if (!_statistics.ExpensesByCategory.ContainsKey(category))
                    _statistics.ExpensesByCategory[category] = 0f;
                _statistics.ExpensesByCategory[category] += amount;
            }
        }
        
        private void ProcessRecurringPayments()
        {
            // Process loan payments, subscriptions, etc.
            foreach (var loan in _activeLoans.Where(l => l.IsActive))
            {
                if (ShouldProcessLoanPayment(loan))
                {
                    ProcessLoanPayment(loan);
                }
            }
        }
        
        private bool ShouldProcessLoanPayment(Loan loan)
        {
            // Simplified - in real implementation would check payment schedule
            return DateTime.Now.Subtract(loan.LastPaymentDate).TotalDays >= 30;
        }
        
        private void ProcessLoanPayment(Loan loan)
        {
            float monthlyPayment = CalculateMonthlyPayment(loan);
            
            if (SpendCurrency(CurrencyType.Cash, monthlyPayment, $"Loan payment: {loan.Purpose}", TransactionCategory.Loan))
            {
                loan.RemainingBalance -= monthlyPayment * 0.8f; // 80% principal, 20% interest
                loan.LastPaymentDate = DateTime.Now;
                
                if (loan.RemainingBalance <= 0)
                {
                    loan.IsActive = false;
                    LogInfo($"Loan paid off: {loan.Purpose}");
                }
            }
        }
        
        private float CalculateMonthlyPayment(Loan loan)
        {
            // Simplified loan payment calculation
            float monthlyRate = loan.InterestRate / 12f;
            var currentDate = DateTime.Now;
            var loanEndDate = loan.StartDate.AddDays(loan.TermDays);
            var remainingTime = loanEndDate - currentDate;
            int paymentsRemaining = Math.Max(1, (int)(remainingTime.TotalDays / 30));
            
            return loan.RemainingBalance / paymentsRemaining * (1 + monthlyRate);
        }
        
        private void UpdateCashFlowPredictions()
        {
            // Calculate projected cash flow for next 30 days
            _cashFlow.ProjectedIncome = EstimateProjectedIncome();
            _cashFlow.ProjectedExpenses = EstimateProjectedExpenses();
            _cashFlow.NetCashFlow = _cashFlow.ProjectedIncome - _cashFlow.ProjectedExpenses;
            _cashFlow.ProjectedBalance = Cash + _cashFlow.NetCashFlow;
        }
        
        private float EstimateProjectedIncome()
        {
            // Analyze recent income patterns
            DateTime cutoffDate = DateTime.Now.AddDays(-30);
            var recentIncome = _transactionHistory
                .Where(t => t.TransactionType == DataTransactionType.Income && t.Timestamp > cutoffDate)
                .Sum(t => t.Amount);
            
            return recentIncome; // Simplified projection
        }
        
        private float EstimateProjectedExpenses()
        {
            // Analyze recent expense patterns plus known recurring costs
            DateTime cutoffDate = DateTime.Now.AddDays(-30);
            var recentExpenses = _transactionHistory
                .Where(t => t.TransactionType == DataTransactionType.Expense && t.Timestamp > cutoffDate)
                .Sum(t => t.Amount);
            
            var recurringCosts = _activeLoans.Sum(l => CalculateMonthlyPayment(l));
            
            return recentExpenses + recurringCosts;
        }
        
        private void CheckBudgetAlerts()
        {
            foreach (var budget in _budgets.Values.Where(b => b.IsActive))
            {
                float percentageUsed = budget.CurrentSpent / budget.Limit;
                
                if (percentageUsed >= 0.9f) // 90% of budget used
                {
                    _onBudgetAlert?.Invoke();
                    OnBudgetAlert?.Invoke(budget.CategoryName, budget.CurrentSpent, budget.Limit);
                }
            }
        }
        
        private void DetectCurrencyChanges()
        {
            foreach (var currency in _currencies)
            {
                if (_lastKnownBalances.TryGetValue(currency.Key, out float lastAmount))
                {
                    if (Mathf.Abs(currency.Value - lastAmount) > 0.01f)
                    {
                        // Currency changed significantly
                        _lastKnownBalances[currency.Key] = currency.Value;
                    }
                }
            }
        }
        
        private void CheckFinancialMilestones()
        {
            float netWorth = CalculateNetWorth();
            
            // Check for milestone achievements
            if (netWorth >= 100000f && _statistics.LastMilestoneAmount < 100000f)
            {
                _onFinancialMilestone?.Invoke();
                OnFinancialMilestone?.Invoke(100000f);
                _statistics.LastMilestoneAmount = 100000f;
                _statistics.LastMilestone = DateTime.Now;
            }
            else if (netWorth >= 50000f && _statistics.LastMilestoneAmount < 50000f)
            {
                _onFinancialMilestone?.Invoke();
                OnFinancialMilestone?.Invoke(50000f);
                _statistics.LastMilestoneAmount = 50000f;
                _statistics.LastMilestone = DateTime.Now;
            }
        }
        
        private void GenerateFinancialReport()
        {
            var report = GetCurrentFinancialReport();
            _reports.Add(report);
            
            // Keep only last 30 reports
            if (_reports.Count > 30)
            {
                _reports.RemoveAt(0);
            }
            
            LogInfo($"Generated financial report - Net Worth: ${report.NetWorth:F2}");
        }
        
        private float CalculateNetWorth()
        {
            float assets = CalculateTotalAssets();
            float liabilities = CalculateTotalLiabilities();
            return assets - liabilities;
        }
        
        private float CalculateTotalAssets()
        {
            float totalAssets = Cash;
            totalAssets += _investments.Values.Sum(i => i.InitialAmount * (1 + i.ExpectedReturn));
            return totalAssets;
        }
        
        private float CalculateTotalLiabilities()
        {
            float totalLiabilities = _creditAccount.PaymentDue;
            totalLiabilities += _activeLoans.Sum(l => l.RemainingBalance);
            return totalLiabilities;
        }
        
        private float CalculateBurnRate()
        {
            var cutoffDate = DateTime.Now.AddDays(-30);
            var recentExpenses = _transactionHistory
                .Where(t => t.TransactionType == DataTransactionType.Expense && t.Timestamp > cutoffDate)
                .Sum(t => t.Amount);
            
            return recentExpenses / 30f; // Daily burn rate
        }
        
        private float CalculateRunwayDays()
        {
            float burnRate = CalculateBurnRate();
            return burnRate > 0 ? Cash / burnRate : float.MaxValue;
        }
        
        private float CalculateProfitMargin()
        {
            return _statistics.TotalIncome > 0 
                ? (_statistics.TotalIncome - _statistics.TotalExpenses) / _statistics.TotalIncome 
                : 0f;
        }
        
        private Dictionary<string, float> GetIncomeByCategory()
        {
            return _statistics.IncomeByCategory.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);
        }
        
        private Dictionary<string, float> GetExpensesByCategory()
        {
            return _statistics.ExpensesByCategory.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);
        }
        
        private Dictionary<string, BudgetStatusReport> GetBudgetMonitoringStatus()
        {
            var budgetStatus = new Dictionary<string, BudgetStatusReport>();
            
            foreach (var budget in _budgets.Values)
            {
                budgetStatus[budget.CategoryName] = new BudgetStatusReport
                {
                    CategoryName = budget.CategoryName,
                    Spent = budget.CurrentSpent,
                    Limit = budget.Limit,
                    Remaining = budget.Limit - budget.CurrentSpent,
                    Percentage = budget.CurrentSpent / budget.Limit,
                    IsOverBudget = budget.CurrentSpent > budget.Limit,
                    LastUpdated = DateTime.Now
                };
            }
            
            return budgetStatus;
        }
        
        private float GetRecentChange(CurrencyType currencyType)
        {
            // Would calculate change from yesterday or last session
            return 0f; // Simplified for now
        }
        
        private float GetRecentChangePercentage(CurrencyType currencyType)
        {
            // Would calculate percentage change
            return 0f; // Simplified for now
        }
        
        private string FormatCurrency(float amount, CurrencyType currencyType)
        {
            var settings = _currencySettings[currencyType];
            string formatString = settings.DecimalPlaces > 0 ? $"F{settings.DecimalPlaces}" : "F0";
            return $"{settings.Symbol}{amount.ToString(formatString)}";
        }
        
        protected override void OnManagerShutdown()
        {
            LogInfo($"CurrencyManager shutdown - Final balance: ${Cash:F2}, {_transactionHistory.Count} transactions recorded");
        }
        
        #region Testing Support
        
        /// <summary>
        /// Sets currency amount for testing purposes (bypasses normal transaction validation)
        /// </summary>
        public void SetCurrencyForTesting(CurrencyType currencyType, float amount)
        {
            _currencies[currencyType] = amount;
            _onCurrencyChanged?.Invoke();
            Debug.Log($"[CurrencyManager] Set {currencyType} to {amount:F2} for testing");
        }
        
        #endregion
    }
}