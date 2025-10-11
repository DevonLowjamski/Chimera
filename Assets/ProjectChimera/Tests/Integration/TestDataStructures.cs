using System;
using System.Collections.Generic;

namespace ProjectChimera.Tests.Integration
{
    /// <summary>
    /// Data structures for integration testing.
    /// Extracted to maintain Phase 0 file size compliance.
    /// </summary>

    #region Test Results

    [Serializable]
    public class TestResults
    {
        public string TestSuiteName;
        public DateTime StartTime;
        public DateTime EndTime;
        public double Duration;
        public int TotalTests;
        public int PassedTests;
        public int FailedTests;
        public List<TestFailure> Failures = new();
    }

    [Serializable]
    public class TestFailure
    {
        public string TestName;
        public string ErrorMessage;
        public string StackTrace;
    }

    #endregion

    #region Game State Snapshot

    [Serializable]
    public class GameStateSnapshot
    {
        public int PlantCount;
        public int ConstructionItemCount;
        public int GeneticsCount;
        public float PlayerCurrency;
        public int SkillPoints;
        public int ActiveIPMInfestations;
        public int ProcessingBatches;
        public string CurrentFacilityId;
        public float GameTime;
    }

    #endregion
}
