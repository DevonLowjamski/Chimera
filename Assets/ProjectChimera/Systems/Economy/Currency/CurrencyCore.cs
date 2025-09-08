using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Economy;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectChimera.Systems.Economy
{
    /// <summary>
    /// Core implementation for basic currency operations and balance management
    /// </summary>
    public class CurrencyCore : ICurrencyCore
    {
        private Dictionary<CurrencyType, float> _currencies = new Dictionary<CurrencyType, float>();
        private Dictionary<CurrencyType, CurrencySettings> _currencySettings = new Dictionary<CurrencyType, CurrencySettings>();
        private bool _enableMultipleCurrencies = true;
        private ITransactions _transactions;
        private IExchangeRates _exchangeRates;

        public float Cash => GetCurrencyAmount(CurrencyType.Cash);
        public float SkillPoints => GetCurrencyAmount(CurrencyType.SkillPoints);
        public float TotalNetWorth => CalculateNetWorth();
        public Dictionary<CurrencyType, float> AllCurrencies => new Dictionary<CurrencyType, float>(_currencies);

        public CurrencyCore(ITransactions transactions = null, IExchangeRates exchangeRates = null)
        {
            _transactions = transactions;
            _exchangeRates = exchangeRates;
        }

        public void Initialize(float startingCash)
        {
            InitializeCurrencies(startingCash);
            InitializeCurrencySettings();
            ChimeraLogger.Log($"[CurrencyCore] Initialized with ${startingCash:F2} starting cash");
        }

        public void Shutdown()
        {
            _currencies.Clear();
            _currencySettings.Clear();
            ChimeraLogger.Log("[CurrencyCore] Currency core system shutdown");
        }

        public bool HasSufficientFunds(float amount) => Cash >= amount;
        public bool HasSufficientSkillPoints(float amount) => SkillPoints >= amount;

        public float GetCurrencyAmount(CurrencyType currencyType)
        {
            return _currencies.TryGetValue(currencyType, out float amount) ? amount : 0f;
        }

        public float GetBalance() => Cash;

        public float GetBalance(CurrencyType currencyType)
        {
            return GetCurrencyAmount(currencyType);
        }

        public bool AddCurrency(CurrencyType currencyType, float amount, string reason = "", TransactionCategory category = TransactionCategory.Other)
        {
            if (amount <= 0)
            {
                ChimeraLogger.LogWarning($"[CurrencyCore] Attempted to add non-positive amount: {amount}");
                return false;
            }

            float oldAmount = GetCurrencyAmount(currencyType);
            _currencies[currencyType] = oldAmount + amount;

            var transaction = new Transaction
            {
                TransactionId = Guid.NewGuid().ToString(),
                TransactionType = TransactionType.Income,
                CurrencyType = currencyType,
                Amount = amount,
                Category = category,
                Description = reason,
                Timestamp = DateTime.Now,
                BalanceAfter = _currencies[currencyType]
            };

            _transactions?.RecordTransaction(transaction);
            _transactions?.UpdateCategoryStatistics(category, amount, true);

            ChimeraLogger.Log($"[CurrencyCore] Added {amount} {currencyType}. New balance: {_currencies[currencyType]:F2}");
            return true;
        }

        public bool SpendCurrency(CurrencyType currencyType, float amount, string reason = "", TransactionCategory category = TransactionCategory.Other, bool allowCredit = false)
        {
            if (amount <= 0)
            {
                ChimeraLogger.LogWarning($"[CurrencyCore] Attempted to spend non-positive amount: {amount}");
                return false;
            }

            // Check if we have sufficient funds
            float currentAmount = GetCurrencyAmount(currencyType);

            if (currentAmount < amount)
            {
                if (allowCredit && currencyType == CurrencyType.Cash && _exchangeRates != null)
                {
                    return _exchangeRates.SpendWithCredit(amount, reason, category);
                }

                ChimeraLogger.LogWarning($"[CurrencyCore] Insufficient {currencyType}: {currentAmount:F2} < {amount:F2}");
                _transactions?.OnInsufficientFunds?.Invoke(amount - currentAmount, reason);
                return false;
            }

            // Perform the transaction
            _currencies[currencyType] = currentAmount - amount;

            var transaction = new Transaction
            {
                TransactionId = Guid.NewGuid().ToString(),
                TransactionType = TransactionType.Expense,
                CurrencyType = currencyType,
                Amount = amount,
                Category = category,
                Description = reason,
                Timestamp = DateTime.Now,
                BalanceAfter = _currencies[currencyType]
            };

            _transactions?.RecordTransaction(transaction);
            _transactions?.UpdateCategoryStatistics(category, amount, false);

            ChimeraLogger.Log($"[CurrencyCore] Spent {amount} {currencyType}. New balance: {_currencies[currencyType]:F2}");
            return true;
        }

        public void SetCurrencyAmount(CurrencyType currencyType, float amount, string reason = "System Set")
        {
            float oldAmount = GetCurrencyAmount(currencyType);
            _currencies[currencyType] = Mathf.Max(0f, amount);

            var transaction = new Transaction
            {
                TransactionId = Guid.NewGuid().ToString(),
                TransactionType = amount > oldAmount ? TransactionType.Income : TransactionType.Expense,
                CurrencyType = currencyType,
                Amount = Mathf.Abs(amount - oldAmount),
                Category = TransactionCategory.System,
                Description = reason,
                Timestamp = DateTime.Now,
                BalanceAfter = _currencies[currencyType]
            };

            _transactions?.RecordTransaction(transaction);

            ChimeraLogger.Log($"[CurrencyCore] Set {currencyType} to {amount:F2}. Previous: {oldAmount:F2}");
        }

        public bool AddSkillPoints(float amount, string reason = "")
        {
            return AddCurrency(CurrencyType.SkillPoints, amount, reason, TransactionCategory.SkillProgression);
        }

        public bool SpendSkillPoints(float amount, string reason = "")
        {
            return SpendCurrency(CurrencyType.SkillPoints, amount, reason, TransactionCategory.SkillProgression);
        }

        public float GetSkillPointsBalance()
        {
            return GetCurrencyAmount(CurrencyType.SkillPoints);
        }

        public void SetSkillPoints(float amount, string reason = "System Set")
        {
            SetCurrencyAmount(CurrencyType.SkillPoints, amount, reason);
        }

        public bool AwardSkillPoints(float amount, string achievementReason)
        {
            if (amount <= 0) return false;

            bool success = AddSkillPoints(amount, $"Achievement: {achievementReason}");

            if (success)
            {
                ChimeraLogger.Log($"[CurrencyCore] Awarded {amount} Skill Points for: {achievementReason}");
            }

            return success;
        }

        public float CalculateNetWorth()
        {
            float netWorth = 0f;

            foreach (var currency in _currencies)
            {
                // Convert all currencies to cash equivalent for net worth calculation
                // For now, use 1:1 conversion, but this could be enhanced with exchange rates
                netWorth += currency.Value;
            }

            // Add value of investments if available
            if (_exchangeRates?.Investments != null)
            {
                foreach (var investment in _exchangeRates.Investments.Values)
                {
                    netWorth += investment.CurrentValue;
                }
            }

            // Subtract outstanding debt if available
            if (_exchangeRates?.CreditAccount != null)
            {
                netWorth -= _exchangeRates.CreditAccount.UsedCredit;
            }

            return netWorth;
        }

        public void SetTransactionHandler(ITransactions transactions)
        {
            _transactions = transactions;
        }

        public void SetExchangeRatesHandler(IExchangeRates exchangeRates)
        {
            _exchangeRates = exchangeRates;
        }

        private void InitializeCurrencies(float startingCash)
        {
            // Initialize all currency types
            _currencies[CurrencyType.Cash] = startingCash;

            if (_enableMultipleCurrencies)
            {
                _currencies[CurrencyType.Credits] = 0f;
                _currencies[CurrencyType.ResearchPoints] = 0f;
                _currencies[CurrencyType.ReputationPoints] = 0f;
                _currencies[CurrencyType.SkillPoints] = 0f;
            }
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
    }
}
