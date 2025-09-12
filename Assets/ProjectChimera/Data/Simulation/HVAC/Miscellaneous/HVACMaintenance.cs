using System;
using UnityEngine;
using System.Collections.Generic;

namespace ProjectChimera.Data.Simulation.HVAC.Miscellaneous
{
    /// <summary>
    /// HVAC maintenance and monitoring systems
    /// Handles alarms, maintenance schedules, and equipment monitoring
    /// </summary>

    /// <summary>
    /// HVAC alarm system
    /// </summary>
    [System.Serializable]
    public class HVACAlarm
    {
        public string AlarmId;
        public string EquipmentId;
        public string ZoneId;
        public HVACAlarmType AlarmType;
        public HVACAlarmPriority Priority;
        public string Message;
        public DateTime Timestamp;
        public bool IsActive = true;
        public bool IsAcknowledged = false;
        public float ThresholdValue;
        public float ActualValue;
        public string RecommendedAction;

        /// <summary>
        /// Create a new alarm
        /// </summary>
        public static HVACAlarm CreateAlarm(
            string equipmentId,
            string zoneId,
            HVACAlarmType type,
            string message,
            float threshold = 0f,
            float actual = 0f,
            string action = "")
        {
            return new HVACAlarm
            {
                AlarmId = Guid.NewGuid().ToString(),
                EquipmentId = equipmentId,
                ZoneId = zoneId,
                AlarmType = type,
                Priority = GetPriorityForType(type),
                Message = message,
                Timestamp = DateTime.Now,
                ThresholdValue = threshold,
                ActualValue = actual,
                RecommendedAction = action,
                IsActive = true,
                IsAcknowledged = false
            };
        }

        /// <summary>
        /// Acknowledge the alarm
        /// </summary>
        public void Acknowledge()
        {
            IsAcknowledged = true;
        }

        /// <summary>
        /// Clear the alarm
        /// </summary>
        public void Clear()
        {
            IsActive = false;
        }

        /// <summary>
        /// Get alarm age in minutes
        /// </summary>
        public double GetAgeMinutes()
        {
            return (DateTime.Now - Timestamp).TotalMinutes;
        }

        /// <summary>
        /// Get alarm summary
        /// </summary>
        public string GetSummary()
        {
            string status = IsActive ? (IsAcknowledged ? "ACK" : "ACTIVE") : "CLEARED";
            return $"[{Priority}] {AlarmType}: {Message} ({status})";
        }

        /// <summary>
        /// Get priority for alarm type
        /// </summary>
        private static HVACAlarmPriority GetPriorityForType(HVACAlarmType type)
        {
            switch (type)
            {
                case HVACAlarmType.PowerFailure:
                case HVACAlarmType.EquipmentFailure:
                case HVACAlarmType.Safety:
                    return HVACAlarmPriority.Critical;
                case HVACAlarmType.SensorFailure:
                case HVACAlarmType.Performance:
                    return HVACAlarmPriority.High;
                case HVACAlarmType.MaintenanceRequired:
                case HVACAlarmType.CommunicationError:
                    return HVACAlarmPriority.Medium;
                default:
                    return HVACAlarmPriority.Low;
            }
        }
    }

    /// <summary>
    /// HVAC maintenance schedule
    /// </summary>
    [System.Serializable]
    public class HVACMaintenanceSchedule
    {
        public string ScheduleId;
        public string EquipmentId;
        public MaintenanceType MaintenanceType;
        public MaintenancePriority Priority;
        public DateTime NextMaintenanceDate;
        public DateTime LastMaintenanceDate;
        public int IntervalHours; // Hours between maintenance
        public string Description;
        public List<string> RequiredParts = new List<string>();
        public float EstimatedDurationHours;
        public float EstimatedCost;
        public bool IsOverdue = false;

        /// <summary>
        /// Calculate next maintenance date
        /// </summary>
        public void CalculateNextMaintenance()
        {
            LastMaintenanceDate = DateTime.Now;
            NextMaintenanceDate = LastMaintenanceDate.AddHours(IntervalHours);
            IsOverdue = false;
        }

        /// <summary>
        /// Check if maintenance is due
        /// </summary>
        public bool IsDue()
        {
            return DateTime.Now >= NextMaintenanceDate;
        }

        /// <summary>
        /// Get days until maintenance is due
        /// </summary>
        public int GetDaysUntilDue()
        {
            TimeSpan timeUntil = NextMaintenanceDate - DateTime.Now;
            return Mathf.Max(0, (int)timeUntil.TotalDays);
        }

        /// <summary>
        /// Mark as overdue
        /// </summary>
        public void MarkOverdue()
        {
            IsOverdue = true;
        }

        /// <summary>
        /// Get maintenance summary
        /// </summary>
        public string GetSummary()
        {
            string status = IsOverdue ? "OVERDUE" : IsDue() ? "DUE" : "SCHEDULED";
            return $"{MaintenanceType}: {Description} ({status})";
        }
    }

    /// <summary>
    /// HVAC equipment snapshot for monitoring
    /// </summary>
    [System.Serializable]
    public class HVACEquipmentSnapshot
    {
        public string EquipmentId;
        public string EquipmentName;
        public DateTime Timestamp;
        public bool IsOnline;
        public float Temperature;
        public float Humidity;
        public float AirflowRate;
        public float PowerConsumption;
        public float Efficiency;
        public EquipmentStatus Status;
        public Dictionary<string, float> SensorReadings = new Dictionary<string, float>();

        /// <summary>
        /// Create snapshot from equipment
        /// </summary>
        public static HVACEquipmentSnapshot CreateSnapshot(
            string equipmentId,
            string equipmentName,
            float temperature,
            float humidity,
            float airflow,
            float power,
            float efficiency)
        {
            return new HVACEquipmentSnapshot
            {
                EquipmentId = equipmentId,
                EquipmentName = equipmentName,
                Timestamp = DateTime.Now,
                IsOnline = true,
                Temperature = temperature,
                Humidity = humidity,
                AirflowRate = airflow,
                PowerConsumption = power,
                Efficiency = efficiency,
                Status = EquipmentStatus.Running
            };
        }

        /// <summary>
        /// Add sensor reading
        /// </summary>
        public void AddSensorReading(string sensorName, float value)
        {
            SensorReadings[sensorName] = value;
        }

        /// <summary>
        /// Get sensor reading
        /// </summary>
        public float GetSensorReading(string sensorName)
        {
            return SensorReadings.TryGetValue(sensorName, out float value) ? value : 0f;
        }

        /// <summary>
        /// Check if snapshot is recent (within last minute)
        /// </summary>
        public bool IsRecent()
        {
            return (DateTime.Now - Timestamp).TotalMinutes < 1;
        }

        /// <summary>
        /// Get snapshot summary
        /// </summary>
        public string GetSummary()
        {
            return $"{EquipmentName}: {Temperature:F1}°C, {Humidity:F0}%, {Efficiency:P0} efficient";
        }
    }

    /// <summary>
    /// HVAC control performance metrics
    /// </summary>
    [System.Serializable]
    public class HVACControlPerformance
    {
        public string ZoneId;
        public DateTime Timestamp;
        public float TemperatureAccuracy; // Average error in °C
        public float HumidityAccuracy; // Average error in %
        public float ResponseTime; // Average response time in seconds
        public float EnergyEfficiency; // kWh per hour
        public float StabilityScore; // 0-100, higher is better
        public int AlarmCount;
        public float UptimePercentage; // 0-100

        /// <summary>
        /// Calculate overall performance score
        /// </summary>
        public float GetOverallScore()
        {
            // Weighted score based on key metrics
            float tempScore = Mathf.Max(0, 100 - TemperatureAccuracy * 20); // 20 points per °C error
            float humidityScore = Mathf.Max(0, 100 - Mathf.Abs(HumidityAccuracy) * 2); // 2 points per % error
            float efficiencyScore = EnergyEfficiency * 10; // Efficiency multiplier
            float stabilityScore = StabilityScore;
            float uptimeScore = UptimePercentage;

            return (tempScore + humidityScore + efficiencyScore + stabilityScore + uptimeScore) / 5f;
        }

        /// <summary>
        /// Get performance rating
        /// </summary>
        public string GetPerformanceRating()
        {
            float score = GetOverallScore();
            if (score >= 90) return "Excellent";
            if (score >= 80) return "Good";
            if (score >= 70) return "Fair";
            if (score >= 60) return "Poor";
            return "Critical";
        }

        /// <summary>
        /// Get performance summary
        /// </summary>
        public string GetSummary()
        {
            return $"Zone {ZoneId}: {GetPerformanceRating()} ({GetOverallScore():F1}/100)";
        }
    }

    /// <summary>
    /// Equipment optimization data
    /// </summary>
    [System.Serializable]
    public class EquipmentOptimization
    {
        public string EquipmentId;
        public DateTime AnalysisDate;
        public float CurrentEfficiency;
        public float OptimalEfficiency;
        public float EnergySavingsPotential; // kWh per day
        public float CostSavingsPotential; // $ per month
        public List<OptimizationRecommendation> Recommendations = new List<OptimizationRecommendation>();
        public Dictionary<string, float> OptimizationMetrics = new Dictionary<string, float>();

        /// <summary>
        /// Get efficiency improvement potential
        /// </summary>
        public float GetEfficiencyImprovement()
        {
            return OptimalEfficiency - CurrentEfficiency;
        }

        /// <summary>
        /// Get payback period in months
        /// </summary>
        public float GetPaybackPeriod(float implementationCost)
        {
            if (CostSavingsPotential <= 0) return float.MaxValue;
            return implementationCost / CostSavingsPotential;
        }

        /// <summary>
        /// Get top recommendation
        /// </summary>
        public OptimizationRecommendation GetTopRecommendation()
        {
            if (Recommendations.Count == 0) return null;
            return Recommendations[0]; // Assuming sorted by priority
        }

        /// <summary>
        /// Get optimization summary
        /// </summary>
        public string GetSummary()
        {
            return $"{EquipmentId}: {GetEfficiencyImprovement():P1} improvement potential";
        }
    }

    /// <summary>
    /// Environmental prediction data
    /// </summary>
    [System.Serializable]
    public class EnvironmentalPrediction
    {
        public string ZoneId;
        public PredictionTimeframe Timeframe;
        public DateTime PredictionDate;
        public DateTime TargetDate;
        public float PredictedTemperature;
        public float PredictedHumidity;
        public float ConfidenceLevel; // 0-1
        public string WeatherConditions;
        public Dictionary<string, float> PredictionFactors = new Dictionary<string, float>();

        /// <summary>
        /// Get prediction accuracy range
        /// </summary>
        public (float min, float max) GetTemperatureRange()
        {
            float variance = (1 - ConfidenceLevel) * 5f; // 5°C max variance
            return (PredictedTemperature - variance, PredictedTemperature + variance);
        }

        /// <summary>
        /// Check if prediction is expired
        /// </summary>
        public bool IsExpired()
        {
            return DateTime.Now > TargetDate;
        }

        /// <summary>
        /// Get days until target date
        /// </summary>
        public int GetDaysUntilTarget()
        {
            TimeSpan timeUntil = TargetDate - DateTime.Now;
            return Mathf.Max(0, (int)timeUntil.TotalDays);
        }

        /// <summary>
        /// Get prediction summary
        /// </summary>
        public string GetSummary()
        {
            return $"{Timeframe}: {PredictedTemperature:F1}°C, {ConfidenceLevel:P0} confidence";
        }
    }

    /// <summary>
    /// Optimization recommendation for HVAC systems
    /// </summary>
    [System.Serializable]
    public class OptimizationRecommendation
    {
        public string RecommendationId;
        public string Title;
        public string Description;
        public OptimizationCategory Category;
        public OptimizationPriority Priority;
        public float PotentialSavings; // $ per month
        public float ImplementationCost; // $
        public float EfficiencyImprovement; // percentage
        public int ImplementationTime; // hours
        public bool RequiresShutdown;
        public string RequiredMaterials;
        public string RequiredSkills;

        /// <summary>
        /// Calculate return on investment in months
        /// </summary>
        public float CalculateROI()
        {
            if (PotentialSavings <= 0) return float.MaxValue;
            return ImplementationCost / PotentialSavings;
        }

        /// <summary>
        /// Check if recommendation is cost-effective
        /// </summary>
        public bool IsCostEffective(float maxPaybackMonths = 12f)
        {
            return CalculateROI() <= maxPaybackMonths;
        }
    }

    /// <summary>
    /// Optimization recommendation categories
    /// </summary>
    public enum OptimizationCategory
    {
        Maintenance,
        Efficiency,
        Equipment,
        Controls,
        Ventilation,
        Heating,
        Cooling
    }

    /// <summary>
    /// Optimization recommendation priorities
    /// </summary>
    public enum OptimizationPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
}
