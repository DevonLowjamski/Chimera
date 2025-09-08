using System;

namespace ProjectChimera.Systems.Economy.Trading
{
    [Serializable]
    public class FinancialRecord
    {
        public string RecordId;
        public FinancialTransactionType TransactionType;
        public float Amount;
        public DateTime Timestamp;
        public string Category;
        public string Description;

        public FinancialRecord()
        {
            RecordId = Guid.NewGuid().ToString();
            Timestamp = DateTime.Now;
        }
    }
}
