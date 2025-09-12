using UnityEngine;
using ProjectChimera.Shared;
using ProjectChimera.Data.Cultivation.Plant;
using System;
using System.Collections.Generic;
using System.Linq;
using CultivationPestType = ProjectChimera.Data.Cultivation.IPM.IPMDataStructures.PestType;
using IPMPestType = ProjectChimera.Data.Cultivation.IPM.PestType;
using PestMonitoringData = ProjectChimera.Data.Cultivation.IPM.IPMDataStructures.PestMonitoringData;
using BiologicalControlPlan = ProjectChimera.Data.Cultivation.IPM.IPMDataStructures.BiologicalControlPlan;
using MonitoringPlan = ProjectChimera.Data.Cultivation.IPM.IPMDataStructures.MonitoringPlan;
using IPMEffectivenessReport = ProjectChimera.Data.Cultivation.IPM.IPMDataStructures.IPMEffectivenessReport;
using EnvironmentalRiskFactor = ProjectChimera.Data.Cultivation.IPM.IPMDataStructures.EnvironmentalRiskFactor;
using IPMRecommendation = ProjectChimera.Data.Cultivation.IPM.IPMDataStructures.IPMRecommendation;
using MonitoringStation = ProjectChimera.Data.Cultivation.IPM.IPMDataStructures.MonitoringStation;
using ImprovementRecommendation = ProjectChimera.Data.Cultivation.IPM.IPMDataStructures.ImprovementRecommendation;

namespace ProjectChimera.Data.Cultivation.IPM
{
    /// <summary>
    /// Simplified Integrated Pest Management system
    /// Orchestrates pest monitoring, biological control, and treatment strategies
    /// </summary>
    [CreateAssetMenu(fileName = "New IPM System", menuName = "Project Chimera/Cultivation/IPM System")]
    public class IPMSystem : ChimeraConfigSO
    {
        [Header("IPM Strategy Configuration")]
        [SerializeField] private IPMApproach _ipmApproach = IPMApproach.Biological_First;
        [SerializeField] private IPMComplexityLevel _systemComplexity = IPMComplexityLevel.Professional;

        [Header("Control Methods")]
        [SerializeField] private bool _enableBiologicalControls = true;
        [SerializeField] private bool _enableCulturalControls = true;
        [SerializeField] private bool _enableMechanicalControls = true;
        [SerializeField] private bool _enableChemicalControls = false; // Last resort only

        [Header("Core Components")]
        [SerializeField] private BeneficialOrganism[] _beneficialOrganisms;
        [SerializeField] private MonitoringProtocol _monitoringProtocol;
        [SerializeField] private CulturalPractice[] _culturalPractices;

        // Current state
        private PestMonitoringData[] _recentMonitoringData;
        private IPMRecommendation[] _activeRecommendations;

        #region Properties

        public IPMApproach Approach => _ipmApproach;
        public IPMComplexityLevel Complexity => _systemComplexity;
        public bool BiologicalControlsEnabled => _enableBiologicalControls;
        public bool CulturalControlsEnabled => _enableCulturalControls;
        public bool MechanicalControlsEnabled => _enableMechanicalControls;
        public bool ChemicalControlsEnabled => _enableChemicalControls;

        #endregion

        #region Core IPM Methods

        /// <summary>
        /// Assess current pest situation and generate recommendations
        /// </summary>
        public IPMAssessment AssessSituation(PestMonitoringData[] monitoringData, EnvironmentalData environmentalData)
        {
            _recentMonitoringData = monitoringData;

            var assessment = new IPMAssessment
            {
                AssessmentTime = DateTime.Now,
                OverallRisk = CalculateOverallRisk(monitoringData),
                DetectedPests = IdentifyDetectedPests(monitoringData),
                EnvironmentalFactors = AnalyzeEnvironmentalFactors(environmentalData)
            };

            assessment.Recommendations = GenerateRecommendations(assessment);

            return assessment;
        }

        /// <summary>
        /// Get biological control plan for specific pest
        /// </summary>
        public BiologicalControlPlan GetBiologicalControlPlan(IPMPestType targetPest)
        {
            var suitableBeneficials = FindSuitableBeneficials(targetPest);

            return new BiologicalControlPlan
            {
                TargetPest = targetPest,
                RecommendedBeneficials = suitableBeneficials,
                ReleaseStrategy = CreateReleaseStrategy(suitableBeneficials),
                MonitoringRequirements = CreateMonitoringRequirements(targetPest)
            };
        }

        /// <summary>
        /// Create monitoring plan
        /// </summary>
        public MonitoringPlan CreateMonitoringPlan()
        {
            return new MonitoringPlan
            {
                Protocol = _monitoringProtocol,
                Stations = CreateMonitoringStations(),
                Schedule = CreateInspectionSchedule(),
                AlertThresholds = _monitoringProtocol?.ActionThresholds
            };
        }

        /// <summary>
        /// Evaluate IPM effectiveness
        /// </summary>
        public IPMEffectivenessReport EvaluateEffectiveness(PestMonitoringData[] beforeData, PestMonitoringData[] afterData)
        {
            float pestReduction = CalculatePestReduction(beforeData, afterData);
            float costEffectiveness = CalculateCostEffectiveness();
            float environmentalImpact = CalculateEnvironmentalImpact();

            return new IPMEffectivenessReport
            {
                OverallEffectiveness = (pestReduction + costEffectiveness + (1f - environmentalImpact)) / 3f,
                PestReduction = pestReduction,
                CostEffectiveness = costEffectiveness,
                EnvironmentalImpact = environmentalImpact,
                Recommendations = GenerateImprovementRecommendations(pestReduction, environmentalImpact)
            };
        }

        #endregion

        #region Helper Methods

        private RiskLevel CalculateOverallRisk(PestMonitoringData[] data)
        {
            if (data == null || data.Length == 0) return RiskLevel.Low;

            float maxPopulation = 0f;
            foreach (var pestData in data)
            {
                maxPopulation = Mathf.Max(maxPopulation, pestData.Population);
            }

            if (maxPopulation > 1.0f) return RiskLevel.Critical;
            if (maxPopulation > 0.5f) return RiskLevel.High;
            if (maxPopulation > 0.2f) return RiskLevel.Moderate;
            return RiskLevel.Low;
        }

        private PestDetectionResult[] IdentifyDetectedPests(PestMonitoringData[] data)
        {
            // Group by pest type and find max population
            var pestGroups = new System.Collections.Generic.Dictionary<PestType, float>();

            foreach (var pestData in data)
            {
                if (!pestGroups.ContainsKey(pestData.Pest))
                    pestGroups[pestData.Pest] = 0f;

                pestGroups[pestData.Pest] = Mathf.Max(pestGroups[pestData.Pest], pestData.Population);
            }

            var results = new System.Collections.Generic.List<PestDetectionResult>();
            foreach (var kvp in pestGroups)
            {
                results.Add(new PestDetectionResult
                {
                    PestType = kvp.Key,
                    PopulationLevel = kvp.Value,
                    DetectionMethod = "Automated Monitoring"
                });
            }

            return results.ToArray();
        }

        private EnvironmentalRiskFactor[] AnalyzeEnvironmentalFactors(EnvironmentalData data)
        {
            var factors = new System.Collections.Generic.List<EnvironmentalRiskFactor>();

            // Temperature stress
            if (data.Temperature < 20f || data.Temperature > 30f)
            {
                factors.Add(new EnvironmentalRiskFactor
                {
                    Factor = "Temperature",
                    RiskLevel = data.Temperature < 15f || data.Temperature > 35f ? 0.9f : 0.6f,
                    Description = $"Temperature {data.Temperature}°C is outside optimal range"
                });
            }

            // Humidity stress
            if (data.Humidity < 40f || data.Humidity > 80f)
            {
                factors.Add(new EnvironmentalRiskFactor
                {
                    Factor = "Humidity",
                    RiskLevel = data.Humidity < 30f || data.Humidity > 90f ? 0.8f : 0.5f,
                    Description = $"Humidity {data.Humidity}% is outside optimal range"
                });
            }

            return factors.ToArray();
        }

        private IPMRecommendation[] GenerateRecommendations(IPMAssessment assessment)
        {
            var recommendations = new System.Collections.Generic.List<IPMRecommendation>();

            // Biological control recommendations
            if (_enableBiologicalControls && assessment.OverallRisk >= RiskLevel.Moderate)
            {
                foreach (var pest in assessment.DetectedPests)
                {
                    var bioPlan = GetBiologicalControlPlan(pest.PestType);
                    if (bioPlan.RecommendedBeneficials.Length > 0)
                    {
                        recommendations.Add(new IPMRecommendation
                        {
                            Action = $"Implement biological control for {pest.PestType}",
                            Priority = InterventionPriority.Medium,
                            Justification = $"Detected {pest.PopulationLevel} population of {pest.PestType}"
                        });
                    }
                }
            }

            // Cultural practice recommendations
            if (_enableCulturalControls)
            {
                foreach (var practice in _culturalPractices)
                {
                    recommendations.Add(new IPMRecommendation
                    {
                        Action = $"Implement {practice.PracticeName}",
                        Priority = InterventionPriority.Low,
                        Justification = "Preventative cultural practice"
                    });
                }
            }

            return recommendations.ToArray();
        }

        private BeneficialOrganism[] FindSuitableBeneficials(IPMPestType targetPest)
        {
            var suitable = new System.Collections.Generic.List<BeneficialOrganism>();

            foreach (var beneficial in _beneficialOrganisms)
            {
                foreach (var target in beneficial.TargetPests)
                {
                    if (target.Contains(targetPest.ToString()))
                    {
                        suitable.Add(beneficial);
                        break;
                    }
                }
            }

            return suitable.ToArray();
        }

        private ReleaseStrategy CreateReleaseStrategy(BeneficialOrganism[] beneficials)
        {
            if (beneficials.Length == 0) return null;

            var strategy = beneficials[0].GetRecommendedSchedule();
            return new ReleaseStrategy
            {
                ReleaseDate = DateTime.Now,
                Quantity = strategy.InitialRelease,
                ReleaseLocations = new[] { "Main cultivation area" }
            };
        }

        private BiologicalMonitoringPlan CreateMonitoringRequirements(IPMPestType pest)
        {
            return new BiologicalMonitoringPlan
            {
                MonitoringMethods = new[] { "Visual inspection", "Sticky traps", "Leaf sampling" },
                MonitoringFrequency = 3f // days
            };
        }

        private MonitoringStation[] CreateMonitoringStations()
        {
            return new[]
            {
                new MonitoringStation
                {
                    Location = Vector3.zero,
                    Equipment = new[] { "Sticky traps", "Magnifying glass" },
                    TargetPests = new[] { IPMPestType.SpiderMites, IPMPestType.Thrips, IPMPestType.Aphids }
                }
            };
        }

        private InspectionSchedule CreateInspectionSchedule()
        {
            return new InspectionSchedule
            {
                InspectionTimes = new[] { DateTime.Now.AddHours(9), DateTime.Now.AddHours(14) },
                InspectionTypes = new[] { "Visual inspection", "Trap monitoring" }
            };
        }

        private float CalculatePestReduction(PestMonitoringData[] beforeData, PestMonitoringData[] afterData)
        {
            if (beforeData.Length == 0 || afterData.Length == 0) return 0f;

            float beforeAvg = 0f, afterAvg = 0f;

            foreach (var data in beforeData) beforeAvg += data.Population;
            foreach (var data in afterData) afterAvg += data.Population;

            beforeAvg /= beforeData.Length;
            afterAvg /= afterData.Length;

            return beforeAvg > 0 ? Mathf.Max(0f, (beforeAvg - afterAvg) / beforeAvg) : 0f;
        }

        private float CalculateCostEffectiveness()
        {
            // Simple cost calculation - would be more complex in real implementation
            return 0.7f; // 70% cost effective
        }

        private float CalculateEnvironmentalImpact()
        {
            // Calculate based on chemical usage
            return _enableChemicalControls ? 0.3f : 0.1f;
        }

        private ImprovementRecommendation[] GenerateImprovementRecommendations(float pestReduction, float environmentalImpact)
        {
            var recommendations = new System.Collections.Generic.List<ImprovementRecommendation>();

            if (pestReduction < 0.5f)
            {
                recommendations.Add(new ImprovementRecommendation
                {
                    Category = "Effectiveness",
                    Recommendation = "Increase monitoring frequency",
                    PotentialImprovement = 0.2f
                });
            }

            if (environmentalImpact > 0.5f)
            {
                recommendations.Add(new ImprovementRecommendation
                {
                    Category = "Sustainability",
                    Recommendation = "Shift to more biological controls",
                    PotentialImprovement = 0.3f
                });
            }

            return recommendations.ToArray();
        }

        #endregion
    }

    // Supporting data classes
    [System.Serializable]
    public class IPMAssessment
    {
        public DateTime AssessmentTime;
        public RiskLevel OverallRisk;
        public PestDetectionResult[] DetectedPests;
        public EnvironmentalRiskFactor[] EnvironmentalFactors;
        public IPMRecommendation[] Recommendations;
    }

    [System.Serializable]
    public class EnvironmentalData
    {
        public float Temperature;
        public float Humidity;
        public float LightIntensity;
        public float CO2Level;
    }
}
