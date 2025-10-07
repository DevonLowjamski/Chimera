using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Data.Save.Structures;

namespace ProjectChimera.Data.Save.Structures
{
    public class EconomyStateDTO
    {
        public System.DateTime SaveTimestamp;
        public float Currency;
        public float IncomeRate;
        public float ExpenseRate;
        public int ItemCount;
        public System.DateTime LastUpdate;
    }
}
