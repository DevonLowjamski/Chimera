using System;

namespace ProjectChimera.Systems.Economy.Trading
{
    [Serializable]
    public enum FinancialTransactionType
    {
        Purchase,
        Sale,
        Transfer,
        Income,
        Expense,
        Investment,
        Loan,
        Credit,
        Debit,
        Refund
    }
}
