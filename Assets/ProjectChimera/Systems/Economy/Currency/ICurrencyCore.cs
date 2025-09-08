using ProjectChimera.Data.Economy;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Economy
{
    /// <summary>
    /// Core interface for basic currency operations and balance management
    /// </summary>
    public interface ICurrencyCore
    {
        float Cash { get; }
        float SkillPoints { get; }
        float TotalNetWorth { get; }
        Dictionary<CurrencyType, float> AllCurrencies { get; }

        bool HasSufficientFunds(float amount);
        bool HasSufficientSkillPoints(float amount);
        
        float GetCurrencyAmount(CurrencyType currencyType);
        float GetBalance();
        float GetBalance(CurrencyType currencyType);
        
        bool AddCurrency(CurrencyType currencyType, float amount, string reason = "", TransactionCategory category = TransactionCategory.Other);
        bool SpendCurrency(CurrencyType currencyType, float amount, string reason = "", TransactionCategory category = TransactionCategory.Other, bool allowCredit = false);
        void SetCurrencyAmount(CurrencyType currencyType, float amount, string reason = "System Set");
        
        bool AddSkillPoints(float amount, string reason = "");
        bool SpendSkillPoints(float amount, string reason = "");
        float GetSkillPointsBalance();
        void SetSkillPoints(float amount, string reason = "System Set");
        bool AwardSkillPoints(float amount, string achievementReason);
        
        float CalculateNetWorth();
        
        void Initialize(float startingCash);
        void Shutdown();
    }
}
