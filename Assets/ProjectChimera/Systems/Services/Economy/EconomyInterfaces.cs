using ProjectChimera.Core;

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
}
