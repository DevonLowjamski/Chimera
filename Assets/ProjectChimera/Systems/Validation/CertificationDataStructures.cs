using System;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Validation
{
    /// <summary>
    /// Data structures for Phase 2 Readiness Certification.
    /// Extracted to maintain Phase 0 file size compliance.
    /// </summary>

    #region Certification Report

    /// <summary>
    /// Complete certification report with scoring.
    /// </summary>
    [Serializable]
    public class CertificationReport
    {
        public DateTime CertificationDate;
        public string CertifierVersion;
        public List<CertificationCategory> Categories = new();
        public float TotalScore;
        public float MaxScore;
        public float PercentageScore;
        public bool IsCertified;
        public string CertificationStatus;
        public List<string> RequiredActions = new();
    }

    #endregion

    #region Certification Category

    /// <summary>
    /// Individual category of certification checks.
    /// </summary>
    [Serializable]
    public class CertificationCategory
    {
        public string CategoryName;
        public float MaxScore;
        public float Score;
        public List<CertificationCheck> Checks = new();
    }

    #endregion

    #region Certification Check

    /// <summary>
    /// Single certification check with pass/fail status.
    /// </summary>
    [Serializable]
    public class CertificationCheck
    {
        public string CheckName;
        public float MaxPoints;
        public float PointsAwarded;
        public string Status;
        public string Details;
    }

    #endregion

    #region Pillar Validation

    /// <summary>
    /// Validation result for individual pillar (Construction/Cultivation/Genetics).
    /// </summary>
    [Serializable]
    public class PillarValidation
    {
        public float MaxScore;
        public float Score;
        public bool IsComplete;
        public string Summary;
        public List<string> CompletedFeatures = new();
        public List<string> MissingFeatures = new();
    }

    #endregion

    #region Validation Helpers

    /// <summary>
    /// Anti-pattern violation counts.
    /// </summary>
    [Serializable]
    public class AntiPatternCounts
    {
        public int FindObjectOfType;
        public int DebugLog;
        public int ResourcesLoad;
        public int Reflection;
        public int UpdateMethods;
        public int OversizedFiles;

        public int TotalViolations => FindObjectOfType + DebugLog + ResourcesLoad +
                                       Reflection + UpdateMethods + OversizedFiles;
    }

    #endregion
}
