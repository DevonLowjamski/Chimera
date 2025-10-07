// REFACTORED: Data Structures
// Extracted from CostConfigurationManager.cs for better separation of concerns

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
using ProjectChimera.Systems.Equipment.Degradation.Configuration;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    public class ConfigurationStats
    {
        public int ParameterRetrievals = 0;
        public int ParameterUpdates = 0;
        public int ParameterMisses = 0;
        public int ParameterErrors = 0;
        public int ParameterValidationFailures = 0;
        public DateTime LastParameterAccess = DateTime.MinValue;
    }

    public struct ConfigurationStatistics
    {
        public ProfileStatistics ProfileStats;
        public ValidationStatistics ValidationStats;
        public PersistenceStatistics PersistenceStats;
        public ConfigurationStats GeneralStats;
    }

}
