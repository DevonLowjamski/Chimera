using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Processing;
using ProjectChimera.Data.Cultivation;
using ProjectChimera.Systems.Cultivation;
using HarvestResult = ProjectChimera.Data.Cultivation.HarvestResults;

namespace ProjectChimera.Systems.Processing
{
    /// <summary>
    /// Processing batch manager - coordinates harvest → dry → cure pipeline.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST:
    /// ====================================
    /// "Master the art of post-harvest processing to maximize quality and profit"
    ///
    /// **Complete Pipeline**:
    /// 1. Harvest → Create batch
    /// 2. Dry (7-14 days) → Monitor temp/humidity
    /// 3. Cure (2-8 weeks) → Burp jars, improve quality
    /// 4. Sell/Store → Premium product ready
    ///
    /// **Player Experience**:
    /// - Harvest plants → automatic batch creation
    /// - Choose drying environment (affects quality/time)
    /// - Transfer to curing jars when dry
    /// - Manage burping schedule
    /// - Track quality improvements
    /// - Sell when ready (or cure longer for premium)
    ///
    /// INVISIBLE COMPLEXITY:
    /// Players see: "Batch #42: Week 3 of curing, Quality 88% (+8% gain)"
    /// Behind scenes: Coordinating DryingSystem, CuringSystem, quality calculations.
    /// </summary>
    public class ProcessingBatchManager : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private DryingSystem _dryingSystem;
        [SerializeField] private CuringSystem _curingSystem;

        [Header("Configuration")]
        [SerializeField] private bool _enableDebugLogging = false;

        // All batches (across all stages)
        private Dictionary<string, ProcessingBatch> _allBatches = new Dictionary<string, ProcessingBatch>();
        private List<ProcessingEvent> _processingHistory = new List<ProcessingEvent>();

        // Events
        public event Action<ProcessingBatch> OnBatchCreated;
        public event Action<ProcessingBatch> OnBatchStageChanged;
        public event Action<ProcessingBatch, ProcessingQualityReport> OnBatchCompleted;
        public event Action<ProcessingBatch, string> OnBatchSpoiled;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            // Get services via ServiceContainer
            var container = ServiceContainerFactory.Instance;
            if (container != null)
            {
                if (_dryingSystem == null)
                {
                    _dryingSystem = container.Resolve<DryingSystem>();
                }

                if (_curingSystem == null)
                {
                    _curingSystem = container.Resolve<CuringSystem>();
                }
            }

            // Subscribe to system events
            if (_dryingSystem != null)
            {
                _dryingSystem.OnDryingStarted += OnDryingStarted;
                _dryingSystem.OnDryingComplete += OnDryingComplete;
                _dryingSystem.OnDryingIssue += OnDryingIssue;
            }

            if (_curingSystem != null)
            {
                _curingSystem.OnCuringStarted += OnCuringStarted;
                _curingSystem.OnCuringComplete += OnCuringComplete;
                _curingSystem.OnCuringIssue += OnCuringIssue;
                _curingSystem.OnBurpReminder += OnBurpReminder;
            }

            // Register as service
            container?.RegisterSingleton<ProcessingBatchManager>(this);

            ChimeraLogger.Log("PROCESSING",
                "Processing batch manager initialized", this);
        }

        #region Batch Creation

        /// <summary>
        /// Creates a processing batch from harvest results.
        ///
        /// GAMEPLAY:
        /// - Player harvests plants
        /// - System automatically creates batch
        /// - Batch enters Fresh stage
        /// - Player chooses when to start drying
        /// </summary>
        public ProcessingBatch CreateBatchFromHarvest(HarvestResult harvestResult, string geneticHash = null)
        {
            if (harvestResult == null || harvestResult.TotalYield <= 0)
            {
                ChimeraLogger.LogWarning("PROCESSING",
                    "Cannot create batch from invalid or zero-yield harvest", this);
                return null;
            }

            // Generate batch ID from harvest data
            string batchId = GenerateBatchId(harvestResult);

            // Create batch
            var batch = ProcessingBatch.FromHarvest(
                batchId,
                harvestResult.PlantId, // Use plant ID as strain name for now
                harvestResult.TotalYield,
                harvestResult.QualityScore,
                geneticHash
            );

            _allBatches[batchId] = batch;

            LogEvent(batch, ProcessingEvent.ProcessingEventType.BatchCreated,
                $"Batch created from harvest: {harvestResult.TotalYield}g at {harvestResult.QualityScore:F1}% quality", 0f);

            OnBatchCreated?.Invoke(batch);

            ChimeraLogger.Log("PROCESSING",
                $"Created batch {batchId}: {batch.WeightGrams}g at {batch.InitialQuality:F1}% quality", this);

            return batch;
        }

        /// <summary>
        /// Generates unique batch ID.
        /// </summary>
        private string GenerateBatchId(HarvestResult harvest)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string plantId = harvest.PlantId.Substring(0, Mathf.Min(8, harvest.PlantId.Length));
            return $"BATCH_{plantId}_{timestamp}";
        }

        #endregion

        #region Pipeline Control

        /// <summary>
        /// Starts drying for a fresh batch.
        ///
        /// GAMEPLAY:
        /// - Player selects batch
        /// - Chooses drying environment
        /// - Starts drying process
        /// </summary>
        public bool StartDrying(string batchId, DryingConditions conditions)
        {
            if (!_allBatches.TryGetValue(batchId, out var batch))
            {
                ChimeraLogger.LogWarning("PROCESSING",
                    $"Batch {batchId} not found", this);
                return false;
            }

            if (batch.Stage != ProcessingStage.Fresh)
            {
                ChimeraLogger.LogWarning("PROCESSING",
                    $"Batch {batchId} not fresh (Stage: {batch.Stage})", this);
                return false;
            }

            if (_dryingSystem == null)
            {
                ChimeraLogger.LogWarning("PROCESSING",
                    "DryingSystem not available", this);
                return false;
            }

            return _dryingSystem.StartDrying(batch, conditions);
        }

        /// <summary>
        /// Starts curing for a dried batch.
        ///
        /// GAMEPLAY:
        /// - Drying complete
        /// - Player fills jars with dried material
        /// - Sets target cure duration
        /// - Begins burping schedule
        /// </summary>
        public bool StartCuring(string batchId, CuringJarConfig jarConfig, int targetWeeks)
        {
            if (!_allBatches.TryGetValue(batchId, out var batch))
            {
                ChimeraLogger.LogWarning("PROCESSING",
                    $"Batch {batchId} not found", this);
                return false;
            }

            if (batch.Stage != ProcessingStage.Dried)
            {
                ChimeraLogger.LogWarning("PROCESSING",
                    $"Batch {batchId} not dried (Stage: {batch.Stage})", this);
                return false;
            }

            if (_curingSystem == null)
            {
                ChimeraLogger.LogWarning("PROCESSING",
                    "CuringSystem not available", this);
                return false;
            }

            batch.TargetCuringWeeks = targetWeeks;
            return _curingSystem.StartCuring(batch, jarConfig);
        }

        /// <summary>
        /// Burps a curing jar.
        /// </summary>
        public void BurpJar(string batchId)
        {
            _curingSystem?.BurpJar(batchId);
        }

        #endregion

        #region Event Handlers

        private void OnDryingStarted(ProcessingBatch batch)
        {
            LogEvent(batch, ProcessingEvent.ProcessingEventType.DryingStarted,
                $"Drying started - Target: {batch.TargetDryingDays} days", 0f);

            OnBatchStageChanged?.Invoke(batch);
        }

        private void OnDryingComplete(ProcessingBatch batch)
        {
            LogEvent(batch, ProcessingEvent.ProcessingEventType.DryingComplete,
                $"Drying complete - {batch.DryingDaysElapsed} days, " +
                $"{batch.MoistureContent * 100f:F1}% moisture", 0f);

            OnBatchStageChanged?.Invoke(batch);

            // Check if spoiled
            if (batch.Stage == ProcessingStage.Spoiled)
            {
                HandleSpoiledBatch(batch, "Mold growth during drying");
            }
        }

        private void OnDryingIssue(ProcessingBatch batch, string issue)
        {
            float qualityImpact = batch.MoldRisk > 0.7f ? -5f : -1f;

            LogEvent(batch, ProcessingEvent.ProcessingEventType.QualityDegradation,
                $"Drying issue: {issue}", qualityImpact);
        }

        private void OnCuringStarted(ProcessingBatch batch)
        {
            LogEvent(batch, ProcessingEvent.ProcessingEventType.CuringStarted,
                $"Curing started - Target: {batch.TargetCuringWeeks} weeks", 0f);

            OnBatchStageChanged?.Invoke(batch);
        }

        private void OnCuringComplete(ProcessingBatch batch)
        {
            LogEvent(batch, ProcessingEvent.ProcessingEventType.CuringComplete,
                $"Curing complete - {batch.CuringWeeksElapsed} weeks, " +
                $"Final quality: {batch.CurrentQuality:F1}%", 0f);

            OnBatchStageChanged?.Invoke(batch);

            // Check if spoiled
            if (batch.Stage == ProcessingStage.Spoiled)
            {
                HandleSpoiledBatch(batch, "Mold growth during curing");
            }
            else
            {
                // Generate completion report
                var report = GenerateQualityReport(batch);
                OnBatchCompleted?.Invoke(batch, report);
            }
        }

        private void OnCuringIssue(ProcessingBatch batch, string issue)
        {
            float qualityImpact = batch.MoldRisk > 0.7f ? -3f : -0.5f;

            LogEvent(batch, ProcessingEvent.ProcessingEventType.QualityDegradation,
                $"Curing issue: {issue}", qualityImpact);
        }

        private void OnBurpReminder(ProcessingBatch batch, string message)
        {
            LogEvent(batch, ProcessingEvent.ProcessingEventType.JarBurped,
                message, 0f);
        }

        private void HandleSpoiledBatch(ProcessingBatch batch, string reason)
        {
            LogEvent(batch, ProcessingEvent.ProcessingEventType.BatchSpoiled,
                reason, -batch.CurrentQuality);

            OnBatchSpoiled?.Invoke(batch, reason);

            ChimeraLogger.LogWarning("PROCESSING",
                $"Batch {batch.BatchId} spoiled: {reason}", this);
        }

        #endregion

        #region Quality Reporting

        /// <summary>
        /// Generates final quality report for completed batch.
        /// </summary>
        private ProcessingQualityReport GenerateQualityReport(ProcessingBatch batch)
        {
            float qualityLoss = batch.InitialQuality - batch.CurrentQuality;
            var issues = new List<string>();
            var achievements = new List<string>();

            // Analyze drying
            if (batch.MoldRisk > 0.5f)
                issues.Add("Mold risk during drying");
            if (batch.OverDryRisk > 0.5f)
                issues.Add("Over-drying occurred");
            if (batch.DryingDaysElapsed <= 10 && batch.MoldRisk < 0.2f && batch.OverDryRisk < 0.2f)
                achievements.Add("Perfect dry achieved");

            // Analyze curing
            if (batch.CuringWeeksElapsed >= 6)
                achievements.Add("Extended cure completed");
            if (batch.JarHumidity >= 0.60f && batch.JarHumidity <= 0.64f)
                achievements.Add("Ideal jar humidity maintained");

            // Calculate attribute retention
            float potencyRetention = Mathf.Clamp01(1f - (qualityLoss / 100f) * 0.5f);
            float terpeneRetention = Mathf.Clamp01(1f - (qualityLoss / 100f) * 0.7f);

            return new ProcessingQualityReport
            {
                BatchId = batch.BatchId,
                FinalQuality = batch.CurrentQuality,
                QualityLoss = Mathf.Max(0f, qualityLoss),
                QualityGrade = ProcessingQualityReport.GetGrade(batch.CurrentQuality),
                PotencyRetention = potencyRetention,
                TerpeneRetention = terpeneRetention,
                AppearanceScore = Mathf.Clamp01(batch.CurrentQuality / 100f),
                AromaScore = Mathf.Clamp01(terpeneRetention),
                TotalDryingDays = batch.DryingDaysElapsed,
                TotalCuringWeeks = batch.CuringWeeksElapsed,
                AverageDryingTemp = batch.AverageTemp,
                AverageDryingHumidity = batch.AverageHumidity * 100f,
                AverageCuringHumidity = batch.JarHumidity * 100f,
                Issues = issues,
                Achievements = achievements
            };
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Gets a batch by ID.
        /// </summary>
        public ProcessingBatch GetBatch(string batchId)
        {
            return _allBatches.TryGetValue(batchId, out var batch) ? batch : null;
        }

        /// <summary>
        /// Gets all batches in a specific stage.
        /// </summary>
        public List<ProcessingBatch> GetBatchesByStage(ProcessingStage stage)
        {
            return _allBatches.Values.Where(b => b.Stage == stage).ToList();
        }

        /// <summary>
        /// Gets all active batches (not fresh or cured).
        /// </summary>
        public List<ProcessingBatch> GetActiveBatches()
        {
            return _allBatches.Values.Where(b =>
                b.Stage == ProcessingStage.Drying ||
                b.Stage == ProcessingStage.Curing).ToList();
        }

        /// <summary>
        /// Gets processing history for a batch.
        /// </summary>
        public List<ProcessingEvent> GetBatchHistory(string batchId)
        {
            return _processingHistory.Where(e => e.BatchId == batchId).ToList();
        }

        /// <summary>
        /// Gets overall processing statistics.
        /// </summary>
        public (int total, int active, int completed, int spoiled) GetStatistics()
        {
            int total = _allBatches.Count;
            int active = _allBatches.Values.Count(b => b.Stage == ProcessingStage.Drying || b.Stage == ProcessingStage.Curing);
            int completed = _allBatches.Values.Count(b => b.Stage == ProcessingStage.Cured);
            int spoiled = _allBatches.Values.Count(b => b.Stage == ProcessingStage.Spoiled);

            return (total, active, completed, spoiled);
        }

        #endregion

        #region Logging

        private void LogEvent(ProcessingBatch batch, ProcessingEvent.ProcessingEventType eventType,
            string description, float qualityImpact)
        {
            var evt = new ProcessingEvent
            {
                BatchId = batch.BatchId,
                Timestamp = DateTime.Now,
                EventType = eventType,
                Description = description,
                QualityImpact = qualityImpact
            };

            _processingHistory.Add(evt);

            if (_enableDebugLogging)
            {
                ChimeraLogger.Log("PROCESSING",
                    $"[{batch.BatchId}] {eventType}: {description} (Quality: {qualityImpact:+0.0;-0.0;0})", this);
            }
        }

        #endregion

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (_dryingSystem != null)
            {
                _dryingSystem.OnDryingStarted -= OnDryingStarted;
                _dryingSystem.OnDryingComplete -= OnDryingComplete;
                _dryingSystem.OnDryingIssue -= OnDryingIssue;
            }

            if (_curingSystem != null)
            {
                _curingSystem.OnCuringStarted -= OnCuringStarted;
                _curingSystem.OnCuringComplete -= OnCuringComplete;
                _curingSystem.OnCuringIssue -= OnCuringIssue;
                _curingSystem.OnBurpReminder -= OnBurpReminder;
            }
        }
    }
}
