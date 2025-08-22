using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core.Events;
using ProjectChimera.Systems.Registry;
using ProjectChimera.Data.Economy;
// using ProjectChimera.Data.Economy.Transactions; // Removed to avoid ambiguous type references

namespace ProjectChimera.Systems.Services.Economy
{
    /// <summary>
    /// PC014-4c: Financial Management Service
    /// Handles player finances, credit, loans, and financial record keeping
    /// Decomposed from TradingManager (500 lines target)
    /// </summary>
    public class FinancialManagementService : MonoBehaviour, IFinancialManagementService
    {
        #region Properties
        
        public bool IsInitialized { get; private set; }
        
        #endregion

        #region Private Fields
        
        [Header("Financial Configuration")]
        [SerializeField] private bool _enableFinancialSystem = true;
        [SerializeField] private float _startingCashBalance = 10000f;
        [SerializeField] private float _baseCreditLimit = 5000f;
        [SerializeField] private float _interestRate = 0.05f; // 5% annually
        [SerializeField] private int _creditHistoryDays = 365;
        
        [Header("Player Finances")]
        [SerializeField] private ProjectChimera.Data.Economy.PlayerFinances _playerFinances;
        [SerializeField] private List<ProjectChimera.Data.Economy.FinancialTransaction> _transactionHistory = new List<ProjectChimera.Data.Economy.FinancialTransaction>();
        [SerializeField] private List<ProjectChimera.Data.Economy.LoanContract> _activeLoans = new List<ProjectChimera.Data.Economy.LoanContract>();
        [SerializeField] private ProjectChimera.Data.Economy.CreditProfile _playerCreditProfile;
        
        [Header("Financial Analytics")]
        [SerializeField] private FinancialAnalysis _financialAnalysis;
        [SerializeField] private Dictionary<string, float> _monthlyExpenses = new Dictionary<string, float>();
        [SerializeField] private Dictionary<string, float> _monthlyIncome = new Dictionary<string, float>();
        
        [Header("Events")]
        [SerializeField] private GameEventSO<ProjectChimera.Data.Economy.PlayerFinances> _financialStatusChangedEvent;
        [SerializeField] private GameEventSO<ProjectChimera.Data.Economy.CreditProfile> _creditScoreChangedEvent;
        [SerializeField] private GameEventSO<ProjectChimera.Data.Economy.LoanContract> _loanStatusChangedEvent;
        
        private float _lastInterestCalculation;
        
        #endregion

        #region Events
        
        public event Action<ProjectChimera.Data.Economy.PlayerFinances> OnFinancialStatusChanged;
        public event Action<ProjectChimera.Data.Economy.FinancialTransaction> OnTransactionRecorded;
        public event Action<ProjectChimera.Data.Economy.CreditProfile> OnCreditScoreChanged;
        public event Action<ProjectChimera.Data.Economy.LoanContract> OnLoanStatusChanged;
        
        // IFinancialManagementService events
        public event Action<string, float, float> OnCashChanged;
        public event Action<string, ProjectChimera.Data.Economy.InventoryItem, float> OnInventoryChanged;
        public event Action<string, ProjectChimera.Data.Economy.FinancialMetrics> OnFinancialMetricsUpdated;
        
        #endregion

        #region IService Implementation
        
        public void Initialize()
        {
            if (IsInitialized) return;
            
            Debug.Log("Initializing FinancialManagementService...");
            
            // Initialize financial system
            InitializeFinancialSystem();
            
            // Load player finances
            LoadPlayerFinances();
            
            // Initialize credit scoring
            InitializeCreditScoring();
            
            // Register with central ServiceRegistry
            ServiceRegistry.Instance.RegisterService<IFinancialManagementService>(this, ServiceDomain.Economy);
            
            IsInitialized = true;
            Debug.Log("FinancialManagementService initialized successfully");
        }

        public void Shutdown()
        {
            if (!IsInitialized) return;
            
            Debug.Log("Shutting down FinancialManagementService...");
            
            // Save financial data
            SaveFinancialData();
            
            // Clear collections
            _transactionHistory.Clear();
            _activeLoans.Clear();
            _monthlyExpenses.Clear();
            _monthlyIncome.Clear();
            
            IsInitialized = false;
            Debug.Log("FinancialManagementService shutdown complete");
        }
        
        #endregion

        #region Financial Operations
        
        public bool ProcessPayment(float amount, ProjectChimera.Data.Economy.PaymentMethodType paymentMethod, string description = "")
        {
            if (!_enableFinancialSystem || amount <= 0)
                return false;

            switch (paymentMethod)
            {
                case ProjectChimera.Data.Economy.PaymentMethodType.Cash:
                    return ProcessCashPayment(amount, description);
                
                case ProjectChimera.Data.Economy.PaymentMethodType.Credit:
                    return ProcessCreditPayment(amount, description);
                
                case ProjectChimera.Data.Economy.PaymentMethodType.Barter:
                    return ProcessLoanPayment(amount, description);
                
                default:
                    Debug.LogError($"Unknown payment method: {paymentMethod}");
                    return false;
            }
        }

        public bool ReceivePayment(float amount, ProjectChimera.Data.Economy.PaymentMethodType method, string description = "")
        {
            if (!_enableFinancialSystem || amount <= 0)
                return false;

            _playerFinances.CashBalance += amount;
            
            RecordTransaction(new ProjectChimera.Data.Economy.FinancialTransaction
            {
                TransactionId = Guid.NewGuid().ToString(),
                Amount = amount,
                Description = description,
                Category = "Income",
                Type = CashTransferType.Income,
                Date = DateTime.Now
            });

            UpdateFinancialAnalytics(amount, true);
            OnFinancialStatusChanged?.Invoke(_playerFinances);
            
            Debug.Log($"Received payment: ${amount:F2} via {method}");
            return true;
        }

        public float GetAvailableFunds()
        {
            return _playerFinances.CashBalance + GetAvailableCredit();
        }

        public float GetAvailableCredit()
        {
            return Mathf.Max(0, _playerFinances.CreditLimit - _playerFinances.UsedCredit);
        }

        public ProjectChimera.Data.Economy.PlayerFinances GetPlayerFinances()
        {
            return _playerFinances;
        }

        public List<ProjectChimera.Data.Economy.FinancialTransaction> GetTransactionHistory(int maxRecords = 100)
        {
            return _transactionHistory
                .OrderByDescending(r => r.Date)
                .Take(maxRecords)
                .ToList();
        }
        
        #endregion

        #region Credit Management
        
        public bool ApplyForCredit(float requestedAmount)
        {
            if (!_enableFinancialSystem)
                return false;

            float maxEligibleCredit = CalculateMaxEligibleCredit();
            
            if (requestedAmount <= maxEligibleCredit)
            {
                _playerFinances.CreditLimit += requestedAmount;
                UpdateCreditScore(5); // Small positive impact for successful application
                
                RecordTransaction(new ProjectChimera.Data.Economy.FinancialTransaction
                {
                    TransactionId = Guid.NewGuid().ToString(),
                    Amount = requestedAmount,
                    Category = "Credit",
                    Type = ProjectChimera.Data.Economy.CashTransferType.Income,
                    Description = $"Credit limit increased by ${requestedAmount:F2}",
                    Date = DateTime.Now
                });

                OnFinancialStatusChanged?.Invoke(_playerFinances);
                Debug.Log($"Credit approved: ${requestedAmount:F2}. New limit: ${_playerFinances.CreditLimit:F2}");
                return true;
            }

            UpdateCreditScore(-2); // Small negative impact for declined application
            Debug.Log($"Credit application declined. Requested: ${requestedAmount:F2}, Max eligible: ${maxEligibleCredit:F2}");
            return false;
        }

        public CreditProfile GetCreditScore()
        {
            return _playerCreditProfile;
        }

        public void UpdateCreditScore(int change)
        {
            int oldScore = _playerCreditProfile.CreditScore;
            _playerCreditProfile.CreditScore = Mathf.Clamp(_playerCreditProfile.CreditScore + change, 300, 850);
            _playerCreditProfile.LastUpdated = DateTime.Now;
            
            if (oldScore != _playerCreditProfile.CreditScore)
            {
                _playerCreditProfile.CreditRating = CalculateCreditRating(_playerCreditProfile.CreditScore);
                OnCreditScoreChanged?.Invoke(_playerCreditProfile);
                _creditScoreChangedEvent?.Raise(_playerCreditProfile);
                
                Debug.Log($"Credit score updated: {oldScore} → {_playerCreditProfile.CreditScore} ({_playerCreditProfile.CreditRating})");
            }
        }

        public bool PayCreditBalance(float amount)
        {
            if (amount <= 0 || amount > _playerFinances.CashBalance)
                return false;

            float paymentAmount = Mathf.Min(amount, _playerFinances.UsedCredit);
            
            _playerFinances.CashBalance -= paymentAmount;
            _playerFinances.UsedCredit -= paymentAmount;
            
            RecordTransaction(new ProjectChimera.Data.Economy.FinancialTransaction
            {
                TransactionId = Guid.NewGuid().ToString(),
                Amount = -paymentAmount,
                Category = "Credit Payment",
                Type = ProjectChimera.Data.Economy.CashTransferType.Expense,
                Description = $"Credit balance payment: ${paymentAmount:F2}",
                Date = DateTime.Now
            });

            UpdateCreditScore(2); // Positive impact for paying down credit
            OnFinancialStatusChanged?.Invoke(_playerFinances);
            
            Debug.Log($"Paid ${paymentAmount:F2} toward credit balance");
            return true;
        }
        
        #endregion

        #region Loan Management
        
        public bool ApplyForLoan(float amount, LoanType loanType, int termMonths)
        {
            if (!_enableFinancialSystem || amount <= 0)
                return false;

            float maxLoanAmount = CalculateMaxLoanAmount();
            
            if (amount > maxLoanAmount)
            {
                Debug.Log($"Loan application declined. Requested: ${amount:F2}, Max eligible: ${maxLoanAmount:F2}");
                return false;
            }

            var loanContract = new LoanContract
            {
                LoanId = Guid.NewGuid().ToString(),
                ContractNumber = Guid.NewGuid().ToString(),
                PrincipalAmount = amount,
                InterestRate = CalculateLoanInterestRate(loanType),
                MonthlyPayment = CalculateMonthlyPayment(amount, _interestRate, termMonths),
                OriginationDate = DateTime.Now,
                MaturityDate = DateTime.Now.AddMonths(termMonths),
                CurrentBalance = amount,
                Status = LoanStatus.Active
            };

            _activeLoans.Add(loanContract);
            _playerFinances.CashBalance += amount;

            RecordTransaction(new ProjectChimera.Data.Economy.FinancialTransaction
            {
                TransactionId = Guid.NewGuid().ToString(),
                Amount = amount,
                Category = "Loan",
                Type = ProjectChimera.Data.Economy.CashTransferType.Income,
                Description = $"Loan disbursement: {loanType}",
                Date = DateTime.Now
            });

            OnLoanStatusChanged?.Invoke(loanContract);
            OnFinancialStatusChanged?.Invoke(_playerFinances);
            _loanStatusChangedEvent?.Raise(loanContract);
            
            Debug.Log($"Loan approved: ${amount:F2} at {loanContract.InterestRate:P2} for {termMonths} months");
            return true;
        }

        public List<LoanContract> GetActiveLoans()
        {
            return _activeLoans.Where(l => l.Status == LoanStatus.Active).ToList();
        }

        public bool MakeLoanPayment(string loanId, float amount)
        {
            var loan = _activeLoans.FirstOrDefault(l => l.LoanId == loanId && l.Status == LoanStatus.Active);
            if (loan == null || amount <= 0 || amount > _playerFinances.CashBalance)
                return false;

            _playerFinances.CashBalance -= amount;
            loan.CurrentBalance -= amount;
            loan.LastPaymentDate = DateTime.Now;

            if (loan.CurrentBalance <= 0)
            {
                loan.Status = LoanStatus.Paid_Off;
                UpdateCreditScore(10); // Significant positive impact for paying off loan
            }
            else
            {
                UpdateCreditScore(1); // Small positive impact for making payment
            }

            RecordTransaction(new ProjectChimera.Data.Economy.FinancialTransaction
            {
                TransactionId = Guid.NewGuid().ToString(),
                Amount = -amount,
                Category = "Loan Payment",
                Type = ProjectChimera.Data.Economy.CashTransferType.Expense,
                Description = $"Loan payment",
                Date = DateTime.Now
            });

            OnLoanStatusChanged?.Invoke(loan);
            OnFinancialStatusChanged?.Invoke(_playerFinances);
            
            Debug.Log($"Loan payment: ${amount:F2}. Remaining balance: ${loan.CurrentBalance:F2}");
            return true;
        }
        
        #endregion

        #region Financial Analytics
        
        public FinancialAnalysis GetFinancialAnalytics()
        {
            UpdateFinancialAnalyticsData();
            return _financialAnalysis;
        }

        public Dictionary<string, float> GetMonthlyExpenses()
        {
            return new Dictionary<string, float>(_monthlyExpenses);
        }

        public Dictionary<string, float> GetMonthlyIncome()
        {
            return new Dictionary<string, float>(_monthlyIncome);
        }

        public float CalculateNetWorth()
        {
            float assets = _playerFinances.CashBalance;
            float liabilities = _playerFinances.UsedCredit + GetTotalLoanBalance();
            
            return assets - liabilities;
        }

        public float CalculateDebtToIncomeRatio()
        {
            float monthlyDebt = CalculateMonthlyDebtPayments();
            float monthlyIncome = _monthlyIncome.Values.Sum();
            
            return monthlyIncome > 0 ? monthlyDebt / monthlyIncome : 0f;
        }
        
        #endregion

        #region IFinancialManagementService Implementation
        
        public float GetCashBalance(string playerId)
        {
            return _playerFinances.CashBalance;
        }

        public float GetNetWorth(string playerId)
        {
            return CalculateNetWorth();
        }

        public bool TransferCash(string playerId, float amount, CashTransferType transferType)
        {
            if (amount <= 0) return false;

            float oldBalance = _playerFinances.CashBalance;
            
            switch (transferType)
            {
            case ProjectChimera.Data.Economy.CashTransferType.Income:
                    _playerFinances.CashBalance += amount;
                    break;
            case ProjectChimera.Data.Economy.CashTransferType.Expense:
                    if (_playerFinances.CashBalance < amount) return false;
                    _playerFinances.CashBalance -= amount;
                    break;
                default:
                    return false;
            }

            OnCashChanged?.Invoke(playerId, oldBalance, _playerFinances.CashBalance);
            return true;
        }

        public FinancialMetrics GetFinancialMetrics(string playerId)
        {
            return _playerFinances.Metrics ?? new FinancialMetrics
            {
                NetWorth = CalculateNetWorth(),
                LiquidAssets = _playerFinances.CashBalance,
                TotalDebt = GetTotalLoanBalance() + _playerFinances.UsedCredit,
                MonthlyIncome = _monthlyIncome.Values.Sum(),
                MonthlyExpenses = _monthlyExpenses.Values.Sum(),
                CashFlow = _monthlyIncome.Values.Sum() - _monthlyExpenses.Values.Sum(),
                DebtToIncomeRatio = CalculateDebtToIncomeRatio(),
                LastCalculation = DateTime.Now
            };
        }

        public PlayerInventory GetPlayerInventory(string playerId)
        {
            // For now, return a basic inventory - this would typically be managed by a separate inventory service
            return new PlayerInventory
            {
                PlayerId = playerId,
                Items = new List<InventoryItem>(),
                MaxCapacity = 1000f,
                CurrentCapacity = 0f,
                LastUpdate = DateTime.Now
            };
        }

        public List<InventoryItem> GetInventoryForProduct(string playerId, MarketProductSO product)
        {
            // This would typically delegate to an inventory service
            return new List<InventoryItem>();
        }

        public float GetTotalInventoryQuantity(string playerId, MarketProductSO product)
        {
            // This would typically delegate to an inventory service
            return 0f;
        }

        public bool AddToInventory(string playerId, InventoryItem item)
        {
            // This would typically delegate to an inventory service
            OnInventoryChanged?.Invoke(playerId, item, item.Quantity);
            return true;
        }

        public bool RemoveFromInventory(string playerId, string itemId, float quantity)
        {
            // This would typically delegate to an inventory service
            return false;
        }

        public TradingProfitabilityAnalysis AnalyzeProfitability(MarketProductSO product, float quantity, TradingTransactionType transactionType)
        {
            return new TradingProfitabilityAnalysis
            {
                Product = product,
                Quantity = quantity,
                TransactionType = transactionType,
                EstimatedProfit = 0f,
                ProfitMargin = 0f,
                BreakEvenPrice = 0f,
                RiskAssessment = 0.5f,
                Recommendation = "Requires market analysis",
                AnalysisDate = DateTime.Now
            };
        }

        public float CalculateBreakEvenPrice(MarketProductSO product, float quantity)
        {
            // Basic break-even calculation - would be more sophisticated in practice
            return product.BaseWholesalePrice;
        }

        public float EstimateProfit(MarketProductSO product, float quantity, float buyPrice, float sellPrice)
        {
            return (sellPrice - buyPrice) * quantity;
        }
        
        #endregion

        #region Private Helper Methods
        
        private void InitializeFinancialSystem()
        {
            if (_playerFinances == null)
            {
                _playerFinances = new ProjectChimera.Data.Economy.PlayerFinances
                {
                    PlayerId = "default_player",
                    CashBalance = _startingCashBalance,
                    CreditLimit = _baseCreditLimit,
                    UsedCredit = 0f,
                    Accounts = new List<ProjectChimera.Data.Economy.FinancialAccount>()
                };
            }

            if (_transactionHistory == null)
                _transactionHistory = new List<ProjectChimera.Data.Economy.FinancialTransaction>();

            if (_activeLoans == null)
                _activeLoans = new List<ProjectChimera.Data.Economy.LoanContract>();

            Debug.Log("Financial system initialized");
        }

        private void LoadPlayerFinances()
        {
            // TODO: Load from persistent storage
            Debug.Log("Loading player finances...");
        }

        private void InitializeCreditScoring()
        {
            if (_playerCreditProfile == null)
            {
                _playerCreditProfile = new ProjectChimera.Data.Economy.CreditProfile
                {
                    CreditScore = 650, // Start with fair credit
                    CreditRating = ProjectChimera.Data.Economy.CreditRating.Good_670_739,
                    LastUpdated = DateTime.Now
                };
            }
        }

        private void SaveFinancialData()
        {
            // TODO: Save to persistent storage
            Debug.Log("Saving financial data...");
        }

        private bool ProcessCashPayment(float amount, string description)
        {
            if (_playerFinances.CashBalance < amount)
                return false;

            _playerFinances.CashBalance -= amount;
            
            RecordTransaction(new ProjectChimera.Data.Economy.FinancialTransaction
            {
                TransactionId = Guid.NewGuid().ToString(),
                Amount = -amount,
                Category = "Expense",
                Type = ProjectChimera.Data.Economy.CashTransferType.Expense,
                Description = description,
                Date = DateTime.Now
            });

            UpdateFinancialAnalytics(amount, false);
            OnFinancialStatusChanged?.Invoke(_playerFinances);
            return true;
        }

        private bool ProcessCreditPayment(float amount, string description)
        {
            float availableCredit = GetAvailableCredit();
            if (availableCredit < amount)
                return false;

            _playerFinances.UsedCredit += amount;
            
            RecordTransaction(new ProjectChimera.Data.Economy.FinancialTransaction
            {
                TransactionId = Guid.NewGuid().ToString(),
                Amount = -amount,
                Category = "Credit Purchase",
                Type = ProjectChimera.Data.Economy.CashTransferType.Expense,
                Description = description,
                Date = DateTime.Now
            });

            UpdateFinancialAnalytics(amount, false);
            OnFinancialStatusChanged?.Invoke(_playerFinances);
            return true;
        }

        private bool ProcessLoanPayment(float amount, string description)
        {
            // For loan-based purchases, would typically require pre-approved loan
            // For now, treat as cash payment with potential loan origination
            Debug.LogWarning("Loan payment processing not fully implemented");
            return ProcessCashPayment(amount, description);
        }

        private void RecordTransaction(ProjectChimera.Data.Economy.FinancialTransaction record)
        {
            _transactionHistory.Add(record);

            // Keep only recent history
            var cutoffDate = DateTime.Now.AddDays(-_creditHistoryDays);
            _transactionHistory.RemoveAll(r => r.Date < cutoffDate);

            OnTransactionRecorded?.Invoke(record);
            _financialStatusChangedEvent?.Raise(_playerFinances);
        }

        private void UpdateFinancialAnalytics(float amount, bool isIncome)
        {
            string currentMonth = DateTime.Now.ToString("yyyy-MM");
            
            if (isIncome)
            {
                _monthlyIncome[currentMonth] = _monthlyIncome.GetValueOrDefault(currentMonth, 0f) + amount;
            }
            else
            {
                _monthlyExpenses[currentMonth] = _monthlyExpenses.GetValueOrDefault(currentMonth, 0f) + amount;
            }
        }

        private void UpdateFinancialAnalyticsData()
        {
            if (_financialAnalysis == null)
                _financialAnalysis = new FinancialAnalysis();

            _financialAnalysis.AnalysisDate = DateTime.Now;
            // The FinancialAnalysis has different structure, so we'll update what we can
            _financialAnalysis.OverallFinancialHealth = (CalculateNetWorth() > 0) ? 0.75f : 0.25f;
        }

        private float CalculateMaxEligibleCredit()
        {
            float baseCredit = _baseCreditLimit;
            float creditScoreMultiplier = _playerCreditProfile.CreditScore / 850f;
            return baseCredit * creditScoreMultiplier * 2f; // Up to 2x base for excellent credit
        }

        private float CalculateMaxLoanAmount()
        {
            float monthlyIncome = _monthlyIncome.Values.Sum();
            float existingDebt = CalculateMonthlyDebtPayments();
            float availableIncome = monthlyIncome - existingDebt;
            
            // Conservative 28% debt-to-income ratio
            return (availableIncome * 0.28f) * 12f; // Annual capacity
        }

        private float CalculateLoanInterestRate(LoanType loanType)
        {
            float baseRate = _interestRate;
            float creditAdjustment = (850f - _playerCreditProfile.CreditScore) / 850f * 0.05f; // Up to 5% penalty
            
            float typeMultiplier = loanType switch
            {
                LoanType.Equipment_Financing => 1.0f,
                LoanType.Real_Estate => 0.8f, // Lower rate for secured facility loans
                LoanType.Working_Capital => 1.2f, // Higher rate for unsecured working capital
                _ => 1.0f
            };

            return baseRate * typeMultiplier + creditAdjustment;
        }

        private float CalculateMonthlyPayment(float principal, float annualRate, int months)
        {
            float monthlyRate = annualRate / 12f;
            return principal * (monthlyRate * Mathf.Pow(1 + monthlyRate, months)) / 
                   (Mathf.Pow(1 + monthlyRate, months) - 1);
        }

        private ProjectChimera.Data.Economy.CreditRating CalculateCreditRating(int score)
        {
            return score switch
            {
                >= 800 => ProjectChimera.Data.Economy.CreditRating.Excellent_800_Plus,
                >= 740 => ProjectChimera.Data.Economy.CreditRating.Very_Good_740_799,
                >= 670 => ProjectChimera.Data.Economy.CreditRating.Good_670_739,
                >= 580 => ProjectChimera.Data.Economy.CreditRating.Fair_580_669,
                _ => ProjectChimera.Data.Economy.CreditRating.Poor_Below_580
            };
        }

        private float GetTotalLoanBalance()
        {
            return _activeLoans.Where(l => l.Status == LoanStatus.Active).Sum(l => l.CurrentBalance);
        }

        private float CalculateMonthlyDebtPayments()
        {
            float loanPayments = _activeLoans.Where(l => l.Status == LoanStatus.Active).Sum(l => l.MonthlyPayment);
            float creditPayments = _playerFinances.UsedCredit * 0.03f; // Assume 3% minimum payment
            
            return loanPayments + creditPayments;
        }
        
        #endregion

        #region Unity Lifecycle
        
        private void Start()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        private void Update()
        {
            if (!IsInitialized || !_enableFinancialSystem) return;

            // Calculate interest on loans and credit monthly
            if (UnityEngine.Time.time - _lastInterestCalculation >= 2592000f) // 30 days in seconds
            {
                CalculateMonthlyInterest();
                _lastInterestCalculation = UnityEngine.Time.time;
            }
        }

        private void CalculateMonthlyInterest()
        {
            // Apply interest to credit balance
            if (_playerFinances.UsedCredit > 0)
            {
                float monthlyInterest = _playerFinances.UsedCredit * (_interestRate / 12f);
                _playerFinances.UsedCredit += monthlyInterest;
                
                RecordTransaction(new FinancialTransaction
                {
                    TransactionId = Guid.NewGuid().ToString(),
                    Amount = -monthlyInterest,
                    Category = "Interest",
                Type = ProjectChimera.Data.Economy.CashTransferType.Expense,
                    Description = "Monthly credit interest",
                    Date = DateTime.Now
                });
            }

            OnFinancialStatusChanged?.Invoke(_playerFinances);
        }
        
        #endregion
    }
}