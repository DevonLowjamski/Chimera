using ProjectChimera.Core;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Services.Economy
{
    public interface IFinancialManagementService : IService
    {
    }

    public interface ITradingPostManagementService : IService
    {
    }

    public interface ITransactionProcessingService : IService
    {
        string GetTransactionStatus(string transactionId);
    }

    public interface ICurrencyManager : IService
    {
        float CurrentBalance { get; }
        bool HasSufficientFunds(float amount);
        bool SpendFunds(float amount, string description = "");
        void AddFunds(float amount, string description = "");
        string GetBalanceString();
        bool IsInitialized { get; }
    }

    public interface ITradingManager : IService
    {
        bool IsInitialized { get; }
        bool ProcessTrade(string tradeId, float amount);
        float GetTradeValue(string itemId);
        bool HasSufficientInventory(string itemId, int quantity);
        void AddToInventory(string itemId, int quantity);
        void RemoveFromInventory(string itemId, int quantity);
    }
}
