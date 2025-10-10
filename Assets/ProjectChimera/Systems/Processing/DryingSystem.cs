using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using ProjectChimera.Data.Processing;

namespace ProjectChimera.Systems.Processing
{
    /// <summary>
    /// Drying system - manages post-harvest drying process.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST:
    /// ====================================
    /// "Drying is critical - too fast loses terpenes, too slow grows mold"
    ///
    /// **Drying Process (7-14 days)**:
    /// - Hang harvested plants in controlled environment
    /// - Monitor temperature (18-24°C ideal) and humidity (45-55% ideal)
    /// - Moisture drops from 75% → 10-12%
    /// - Dark environment preserves cannabinoids
    ///
    /// **Player Experience**:
    /// - Set up drying room with temp/humidity control
    /// - Check progress daily ("Day 5/10 - 45% moisture remaining")
    /// - Get warnings ("Humidity too high - mold risk!")
    /// - Achieve perfect dry for quality bonus
    ///
    /// INVISIBLE COMPLEXITY:
    /// Players see simple progress bar and status messages.
    /// Behind scenes: Moisture loss curves, mold growth models, quality degradation math.
    /// </summary>
    public class DryingSystem : MonoBehaviour, ITickable
    {
        [Header("Drying Configuration")]
        [SerializeField] private float _tickIntervalSeconds = 3600f; // Tick every game hour
        [SerializeField] private bool _enableDebugLogging = false;

        // ITickable implementation
        public int TickPriority => 50; // Mid-priority processing system
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        [Header("Ideal Conditions")]
        [SerializeField] private float _idealTemperature = 21f;      // 21°C
        [SerializeField] private float _idealHumidity = 0.50f;       // 50%
        [SerializeField] private float _idealAirflow = 0.5f;         // Moderate

        [Header("Drying Parameters")]
        [SerializeField] private float _baseDryingDays = 10f;        // Base drying time
        [SerializeField] private float _targetMoisture = 0.11f;      // 11% target
        [SerializeField] private float _moldRiskThreshold = 0.65f;   // >65% humidity = mold risk

        // Active drying batches
        private Dictionary<string, ProcessingBatch> _dryingBatches = new Dictionary<string, ProcessingBatch>();
        private Dictionary<string, DryingConditions> _batchConditions = new Dictionary<string, DryingConditions>();

        // Services
        private ITimeManager _timeManager;

        // Tick tracking
        private float _tickTimer = 0f;

        // Events
        public event Action<ProcessingBatch> OnDryingStarted;
        public event Action<ProcessingBatch, DryingMetrics> OnDryingProgress;
        public event Action<ProcessingBatch> OnDryingComplete;
        public event Action<ProcessingBatch, string> OnDryingIssue; // Mold, over-dry, etc.

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            // Get time manager
            var container = ServiceContainerFactory.Instance;
            if (container != null)
            {
                _timeManager = container.Resolve<ITimeManager>();

                // Register for ticking
                var orchestrator = container.Resolve<IUpdateOrchestrator>();
                orchestrator?.RegisterTickable(this);

                // Register self as service
                container.RegisterSingleton<DryingSystem>(this);
            }

            ChimeraLogger.Log("PROCESSING",
                "Drying system initialized", this);
        }

        /// <summary>
        /// Starts drying process for a harvested batch.
        ///
        /// GAMEPLAY:
        /// - Player harvests plants
        /// - Selects drying room/environment
        /// - Batch begins drying process
        /// - Daily checks show progress
        /// </summary>
        public bool StartDrying(ProcessingBatch batch, DryingConditions conditions)
        {
            if (batch == null)
            {
                ChimeraLogger.LogWarning("PROCESSING",
                    "Cannot start drying: batch is null", this);
                return false;
            }

            if (_dryingBatches.ContainsKey(batch.BatchId))
            {
                ChimeraLogger.LogWarning("PROCESSING",
                    $"Batch {batch.BatchId} already drying", this);
                return false;
            }

            // Set up batch for drying
            batch.Stage = ProcessingStage.Drying;
            batch.DryingStartDate = DateTime.Now;
            batch.MoistureContent = 0.75f; // Fresh harvest = 75% moisture
            batch.DryingDaysElapsed = 0;
            batch.TargetDryingDays = CalculateTargetDryingDays(conditions);

            _dryingBatches[batch.BatchId] = batch;
            _batchConditions[batch.BatchId] = conditions;

            OnDryingStarted?.Invoke(batch);

            ChimeraLogger.Log("PROCESSING",
                $"Started drying: {batch.StrainName} ({batch.WeightGrams}g) - " +
                $"Target: {batch.TargetDryingDays} days", this);

            return true;
        }

        /// <summary>
        /// Calculates target drying days based on conditions.
        /// Better conditions = faster, safer drying.
        /// </summary>
        private int CalculateTargetDryingDays(DryingConditions conditions)
        {
            float qualityScore = conditions.GetQualityScore();

            // Better conditions = slightly faster drying
            // Range: 7-14 days
            float dryingDays = _baseDryingDays * (1.1f - qualityScore * 0.3f);
            return Mathf.RoundToInt(Mathf.Clamp(dryingDays, 7f, 14f));
        }

        /// <summary>
        /// ITickable implementation - processes drying every game hour.
        /// </summary>
        public void Tick(float deltaTime)
        {
            _tickTimer += deltaTime;

            if (_tickTimer >= _tickIntervalSeconds)
            {
                _tickTimer = 0f;
                ProcessDryingTick();
            }
        }

        /// <summary>
        /// Processes drying for all active batches.
        /// </summary>
        private void ProcessDryingTick()
        {
            if (_dryingBatches.Count == 0)
                return;

            var batchesToComplete = new List<string>();

            foreach (var kvp in _dryingBatches)
            {
                var batch = kvp.Value;
                var conditions = _batchConditions[kvp.Key];

                // Advance drying by one hour
                AdvanceDrying(batch, conditions, 1f / 24f); // 1 hour = 1/24th of a day

                // Check if drying complete
                if (batch.MoistureContent <= _targetMoisture || batch.DryingDaysElapsed >= batch.TargetDryingDays)
                {
                    batchesToComplete.Add(kvp.Key);
                }

                // Report progress
                var metrics = GetDryingMetrics(batch, conditions);
                OnDryingProgress?.Invoke(batch, metrics);
            }

            // Complete batches
            foreach (var batchId in batchesToComplete)
            {
                CompleteDrying(batchId);
            }
        }

        /// <summary>
        /// Advances drying for a batch by a fraction of a day.
        /// </summary>
        private void AdvanceDrying(ProcessingBatch batch, DryingConditions conditions, float dayFraction)
        {
            // Calculate moisture loss rate based on conditions
            float moistureLossRate = CalculateMoistureLossRate(batch, conditions);
            batch.MoistureContent = Mathf.Max(0f, batch.MoistureContent - moistureLossRate * dayFraction);

            // Update time tracking
            batch.DryingDaysElapsed = Mathf.RoundToInt((DateTime.Now - batch.DryingStartDate).Days);

            // Update environment averages
            batch.AverageTemp = Mathf.Lerp(batch.AverageTemp, conditions.Temperature, 0.1f);
            batch.AverageHumidity = Mathf.Lerp(batch.AverageHumidity, conditions.Humidity, 0.1f);

            // Calculate risks
            UpdateDryingRisks(batch, conditions);

            // Apply quality changes
            ApplyQualityEffects(batch, conditions, dayFraction);
        }

        /// <summary>
        /// Calculates moisture loss rate per day.
        /// </summary>
        private float CalculateMoistureLossRate(ProcessingBatch batch, DryingConditions conditions)
        {
            // Base rate: lose ~7% moisture per day on average (75% → 11% in 10 days)
            float baseRate = 0.065f;

            // Temperature effect: warmer = faster drying
            float tempFactor = 1f + (conditions.Temperature - _idealTemperature) * 0.02f;

            // Humidity effect: lower humidity = faster drying
            float humidityFactor = 1f + (_idealHumidity - conditions.Humidity) * 0.5f;

            // Airflow effect: more airflow = faster drying
            float airflowFactor = 0.8f + conditions.Airflow * 0.4f;

            float rate = baseRate * tempFactor * humidityFactor * airflowFactor;
            return Mathf.Clamp(rate, 0.02f, 0.15f); // 2-15% per day
        }

        /// <summary>
        /// Updates mold and over-dry risks.
        /// </summary>
        private void UpdateDryingRisks(ProcessingBatch batch, DryingConditions conditions)
        {
            // Mold risk: high if humidity >65% or temp >24°C
            float moldHumidityRisk = Mathf.Max(0f, (conditions.Humidity - _moldRiskThreshold) * 2f);
            float moldTempRisk = Mathf.Max(0f, (conditions.Temperature - 24f) * 0.1f);
            float moldMoistureRisk = Mathf.Max(0f, (batch.MoistureContent - 0.60f) * 2f);
            batch.MoldRisk = Mathf.Clamp01(moldHumidityRisk + moldTempRisk + moldMoistureRisk);

            // Over-dry risk: high if moisture <8%
            float overDryMoistureRisk = Mathf.Max(0f, (0.08f - batch.MoistureContent) * 5f);
            float overDryTempRisk = Mathf.Max(0f, (conditions.Temperature - 24f) * 0.05f);
            batch.OverDryRisk = Mathf.Clamp01(overDryMoistureRisk + overDryTempRisk);

            // Check for issues
            if (batch.MoldRisk > 0.7f)
            {
                OnDryingIssue?.Invoke(batch, "High mold risk - reduce humidity or temperature");
                if (_enableDebugLogging)
                {
                    ChimeraLogger.LogWarning("PROCESSING",
                        $"Mold risk warning: {batch.StrainName} (Risk: {batch.MoldRisk * 100f:F0}%)", this);
                }
            }

            if (batch.OverDryRisk > 0.7f)
            {
                OnDryingIssue?.Invoke(batch, "Over-dry risk - material becoming too brittle");
                if (_enableDebugLogging)
                {
                    ChimeraLogger.LogWarning("PROCESSING",
                        $"Over-dry risk warning: {batch.StrainName} (Risk: {batch.OverDryRisk * 100f:F0}%)", this);
                }
            }
        }

        /// <summary>
        /// Applies quality effects based on drying conditions.
        /// </summary>
        private void ApplyQualityEffects(ProcessingBatch batch, DryingConditions conditions, float dayFraction)
        {
            float qualityChange = 0f;

            // Good conditions preserve or improve quality
            float conditionQuality = conditions.GetQualityScore();
            if (conditionQuality >= 0.9f)
            {
                qualityChange += 0.1f * dayFraction; // +0.1 per day for perfect conditions
            }

            // Bad conditions degrade quality
            if (batch.MoldRisk > 0.5f)
            {
                qualityChange -= batch.MoldRisk * 0.5f * dayFraction; // Mold damages quality
            }

            if (batch.OverDryRisk > 0.5f)
            {
                qualityChange -= batch.OverDryRisk * 0.3f * dayFraction; // Over-drying damages quality
            }

            // No light preserves cannabinoids
            if (!conditions.DarknessProvided)
            {
                qualityChange -= 0.2f * dayFraction; // -0.2 per day for light exposure
            }

            // Apply change
            batch.CurrentQuality = Mathf.Clamp(batch.CurrentQuality + qualityChange, 0f, 100f);
        }

        /// <summary>
        /// Completes drying for a batch.
        /// </summary>
        private void CompleteDrying(string batchId)
        {
            if (!_dryingBatches.TryGetValue(batchId, out var batch))
                return;

            // Check if spoiled
            if (batch.MoldRisk > 0.9f)
            {
                batch.Stage = ProcessingStage.Spoiled;
                batch.CurrentQuality = 0f;
                OnDryingIssue?.Invoke(batch, "Batch spoiled - mold detected");
            }
            else
            {
                batch.Stage = ProcessingStage.Dried;

                // Weight loss from moisture (dried = ~25% of wet weight)
                batch.WeightGrams *= (1f - batch.MoistureContent + 0.25f);
            }

            _dryingBatches.Remove(batchId);
            _batchConditions.Remove(batchId);

            OnDryingComplete?.Invoke(batch);

            ChimeraLogger.Log("PROCESSING",
                $"Drying complete: {batch.StrainName} - " +
                $"Final moisture: {batch.MoistureContent * 100f:F1}%, " +
                $"Quality: {batch.CurrentQuality:F1}", this);
        }

        /// <summary>
        /// Gets drying metrics for a batch.
        /// </summary>
        public DryingMetrics GetDryingMetrics(string batchId)
        {
            if (!_dryingBatches.TryGetValue(batchId, out var batch) ||
                !_batchConditions.TryGetValue(batchId, out var conditions))
            {
                return default;
            }

            return GetDryingMetrics(batch, conditions);
        }

        /// <summary>
        /// Gets drying metrics for a batch.
        /// </summary>
        private DryingMetrics GetDryingMetrics(ProcessingBatch batch, DryingConditions conditions)
        {
            float moisturePercentage = batch.MoistureContent * 100f;
            float dryingRate = CalculateMoistureLossRate(batch, conditions) * 100f;
            int daysRemaining = Mathf.Max(0, batch.TargetDryingDays - batch.DryingDaysElapsed);

            return new DryingMetrics
            {
                MoisturePercentage = moisturePercentage,
                TargetMoisture = _targetMoisture * 100f,
                DryingRate = dryingRate,
                DaysRemaining = daysRemaining,
                ConditionQuality = conditions.GetQualityScore(),
                MoldRisk = batch.MoldRisk,
                OverDryRisk = batch.OverDryRisk,
                Status = GetDryingStatus(batch, conditions)
            };
        }

        /// <summary>
        /// Gets human-readable drying status.
        /// </summary>
        private string GetDryingStatus(ProcessingBatch batch, DryingConditions conditions)
        {
            float moisture = batch.MoistureContent * 100f;

            if (batch.MoldRisk > 0.7f) return "⚠️ High mold risk";
            if (batch.OverDryRisk > 0.7f) return "⚠️ Over-drying";
            if (moisture > 60f) return "Drying - Monitor closely";
            if (moisture > 12f) return "✓ Drying well";
            if (moisture > 8f) return "✅ Perfect - Ready to cure";
            return "⚠️ Too dry";
        }

        /// <summary>
        /// Gets all active drying batches.
        /// </summary>
        public List<ProcessingBatch> GetActiveBatches()
        {
            return new List<ProcessingBatch>(_dryingBatches.Values);
        }

        /// <summary>
        /// Checks if a batch is currently drying.
        /// </summary>
        public bool IsBatchDrying(string batchId)
        {
            return _dryingBatches.ContainsKey(batchId);
        }

        /// <summary>
        /// Updates drying conditions for a batch (player adjusts environment).
        /// </summary>
        public void UpdateDryingConditions(string batchId, DryingConditions newConditions)
        {
            if (_batchConditions.ContainsKey(batchId))
            {
                _batchConditions[batchId] = newConditions;

                if (_enableDebugLogging)
                {
                    ChimeraLogger.Log("PROCESSING",
                        $"Updated drying conditions for {batchId}: " +
                        $"Temp: {newConditions.Temperature}°C, " +
                        $"Humidity: {newConditions.Humidity * 100f:F0}%", this);
                }
            }
        }

        private void OnDestroy()
        {
            var container = ServiceContainerFactory.Instance;
            var orchestrator = container?.Resolve<IUpdateOrchestrator>();
            orchestrator?.UnregisterTickable(this);
        }
    }
}
