// REFACTORED: Malfunction Risk Assessor Data Structures
// Extracted from MalfunctionRiskAssessor for better separation of concerns

using System;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// Malfunction risk assessor statistics
    /// </summary>
    public struct MalfunctionRiskAssessorStats
    {
        public int TotalAssessments;
        public int LowRiskAssessments;
        public int MediumRiskAssessments;
        public int HighRiskAssessments;
        public int CriticalRiskAssessments;
        public int AssessmentsWithoutProfile;
        public int AssessmentErrors;
        public float TotalAssessmentTime;
        public float AverageAssessmentTime;
        public float MaxAssessmentTime;
    }

    /// <summary>
    /// Risk component breakdown
    /// </summary>
    [Serializable]
    public struct RiskComponents
    {
        public float WearRisk;
        public float EnvironmentalRisk;
        public float MaintenanceRisk;
    }
}

