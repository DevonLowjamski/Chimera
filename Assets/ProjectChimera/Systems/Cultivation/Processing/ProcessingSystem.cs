using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Interfaces;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.Cultivation.Processing
{
    /// <summary>
    /// Processing system - drying and curing for quality optimization.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST:
    /// ====================================
    /// "Turn fresh harvest into premium product - patience pays off!"
    ///
    /// **Player Experience**:
    /// - Drying: HangDry (10 days), RackDry (8 days), FreezeDry (3 days)
    /// - Moisture loss tracking (75% → 10-12% target)
    /// - Weight calculation (expect ~25% of wet weight)
    /// - Curing: JarCuring (burping), TurkeyBag, GroveBag
    /// - 2-8 week timeline with quality improvement
    /// - Terpene preservation (85% → develops over time)
    /// - Final quality grade affects market value
    ///
    /// **Strategic Depth**:
    /// - HangDry: Traditional, best quality (+5% quality)
    /// - RackDry: Faster, good for large harvests
    /// - FreezeDry: Fastest, preserves terpenes (+10% terpenes)
    /// - JarCuring: Requires burping (daily week 1-2, then reduce)
    /// - TurkeyBag: Lower maintenance, slightly lower quality
    /// - GroveBag: Premium, self-burping, best results (+5% quality)
    ///
    /// **Integration**:
    /// - Links to harvest system (input: fresh weight, plant data)
    /// - Quality affects market pricing (Premium+ 2.0x → Poor 0.4x)
    /// - ITickable hourly updates for drying/curing
    /// - Time acceleration compatible
    ///
    /// INVISIBLE COMPLEXITY:
    /// Players see: "Batch DR-001: Day 7/10, 45% moisture, Good grade" → simple!
    /// Behind scenes: Moisture curves, terpene degradation, quality calculations, burping schedules.
    /// </summary>
    public class ProcessingSystem : MonoBehaviour, IProcessingSystem, ITickable
    {
        [Header("Drying Configuration")]
        [SerializeField] private float _hangDryDays = 10f;
        [SerializeField] private float _rackDryDays = 8f;
        [SerializeField] private float _freezeDryDays = 3f;
        [SerializeField] private float _targetMoisture = 0.11f;
        [SerializeField] private float _hangDryQualityBonus = 0.05f;
        [SerializeField] private float _freezeDryTerpeneBonus = 0.10f;

        [Header("Curing Configuration")]
        [SerializeField] private float _minCuringWeeks = 2f;
        [SerializeField] private float _optimalCuringWeeks = 6f;
        [SerializeField] private float _maxCuringWeeks = 8f;
        [SerializeField] private float _jarCuringQuality = 1.0f;
        [SerializeField] private float _turkeyBagQuality = 0.95f;
        [SerializeField] private float _groveBagQuality = 1.05f;

        [Header("Quality Grading")]
        [SerializeField] private float _premiumPlusThreshold = 0.95f;
        [SerializeField] private float _premiumThreshold = 0.85f;
        [SerializeField] private float _goodThreshold = 0.70f;
        [SerializeField] private float _averageThreshold = 0.50f;

        // Processing batch tracking
        private Dictionary<string, DryingBatch> _dryingBatches = new Dictionary<string, DryingBatch>();
        private Dictionary<string, CuringBatch> _curingBatches = new Dictionary<string, CuringBatch>();

        // ITickable properties
        public int TickPriority => 40;
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        // Events
        public event Action<string> OnDryingStarted;
        public event Action<string> OnDryingCompleted;
        public event Action<string> OnCuringStarted;
        public event Action<string> OnCuringCompleted;
        public event Action<string, ProcessingQuality> OnQualityGraded;

        private float _timeSinceLastUpdate = 0f;
        private const float UPDATE_INTERVAL_SECONDS = 3600f;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            var container = ServiceContainerFactory.Instance;
            container?.RegisterSingleton<IProcessingSystem>(this);

            var orchestrator = container?.Resolve<UpdateOrchestrator>();
            orchestrator?.RegisterTickable(this);

            ChimeraLogger.Log("PROCESSING",
                "Processing system initialized - drying and curing ready!", this);
        }

        #region ITickable Implementation

        public void Tick(float deltaTime)
        {
            _timeSinceLastUpdate += deltaTime;

            if (_timeSinceLastUpdate >= UPDATE_INTERVAL_SECONDS)
            {
                UpdateDryingBatches(UPDATE_INTERVAL_SECONDS);
                UpdateCuringBatches(UPDATE_INTERVAL_SECONDS);
                _timeSinceLastUpdate = 0f;
            }
        }

        #endregion

        #region Drying Operations

        /// <summary>
        /// Starts drying process for harvested material.
        /// GAMEPLAY: Player harvests → Select drying method → Start drying.
        /// </summary>
        public bool StartDrying(string batchId, float wetWeightGrams, DryingMethod method)
        {
            if (_dryingBatches.ContainsKey(batchId))
            {
                ChimeraLogger.LogWarning("PROCESSING", $"Batch {batchId} already drying", this);
                return false;
            }

            float durationDays = ProcessingHelpers.GetDryingDuration(method,
                _hangDryDays, _rackDryDays, _freezeDryDays);

            var batch = new DryingBatch
            {
                BatchId = batchId,
                Method = method,
                WetWeightGrams = wetWeightGrams,
                CurrentMoisture = 0.75f,
                TargetMoisture = _targetMoisture,
                DurationDays = durationDays,
                ElapsedDays = 0f,
                StartDate = DateTime.Now,
                IsComplete = false
            };

            _dryingBatches[batchId] = batch;
            OnDryingStarted?.Invoke(batchId);

            ChimeraLogger.Log("PROCESSING",
                $"Drying started: {batchId}, {method}, {wetWeightGrams:F0}g wet, {durationDays:F0} days", this);

            return true;
        }

        /// <summary>
        /// Updates drying batches (moisture loss over time).
        /// </summary>
        private void UpdateDryingBatches(float deltaTimeSeconds)
        {
            float deltaTimeDays = deltaTimeSeconds / (24f * 60f * 60f);

            foreach (var kvp in _dryingBatches.ToList())
            {
                var batch = kvp.Value;
                if (batch.IsComplete) continue;

                batch.ElapsedDays += deltaTimeDays;
                float progress = Mathf.Clamp01(batch.ElapsedDays / batch.DurationDays);

                batch.CurrentMoisture = ProcessingHelpers.CalculateMoistureLoss(
                    0.75f, batch.TargetMoisture, progress);

                if (progress >= 1f)
                {
                    batch.IsComplete = true;
                    batch.CompletionDate = DateTime.Now;
                    batch.DryWeightGrams = batch.WetWeightGrams * 0.25f;

                    OnDryingCompleted?.Invoke(batch.BatchId);
                    ChimeraLogger.Log("PROCESSING",
                        $"Drying complete: {batch.BatchId}, {batch.DryWeightGrams:F0}g dry", this);
                }

                _dryingBatches[kvp.Key] = batch;
            }
        }

        #endregion

        #region Curing Operations

        /// <summary>
        /// Starts curing process for dried material.
        /// GAMEPLAY: Drying complete → Select curing method → Start curing.
        /// </summary>
        public bool StartCuring(string batchId, float dryWeightGrams, CuringMethod method)
        {
            if (_curingBatches.ContainsKey(batchId))
            {
                ChimeraLogger.LogWarning("PROCESSING", $"Batch {batchId} already curing", this);
                return false;
            }

            var batch = new CuringBatch
            {
                BatchId = batchId,
                Method = method,
                WeightGrams = dryWeightGrams,
                TerpenePreservation = 0.85f,
                BaseQuality = 0.70f,
                CurrentQuality = 0.70f,
                ElapsedWeeks = 0f,
                BurpCount = 0,
                LastBurpDate = DateTime.Now,
                StartDate = DateTime.Now,
                IsComplete = false
            };

            _curingBatches[batchId] = batch;
            OnCuringStarted?.Invoke(batchId);

            ChimeraLogger.Log("PROCESSING",
                $"Curing started: {batchId}, {method}, {dryWeightGrams:F0}g", this);

            return true;
        }

        /// <summary>
        /// Updates curing batches (quality improvement over time).
        /// </summary>
        private void UpdateCuringBatches(float deltaTimeSeconds)
        {
            float deltaTimeWeeks = deltaTimeSeconds / (7f * 24f * 60f * 60f);

            foreach (var kvp in _curingBatches.ToList())
            {
                var batch = kvp.Value;
                if (batch.IsComplete) continue;

                batch.ElapsedWeeks += deltaTimeWeeks;

                float methodMultiplier = ProcessingHelpers.GetCuringQualityMultiplier(batch.Method,
                    _jarCuringQuality, _turkeyBagQuality, _groveBagQuality);

                batch.CurrentQuality = ProcessingHelpers.CalculateCuringQuality(
                    batch.BaseQuality, batch.ElapsedWeeks, _minCuringWeeks,
                    _optimalCuringWeeks, methodMultiplier);

                batch.TerpenePreservation = ProcessingHelpers.CalculateTerpenePreservation(
                    batch.ElapsedWeeks, _maxCuringWeeks);

                if (batch.ElapsedWeeks >= _minCuringWeeks && batch.ElapsedWeeks < _maxCuringWeeks)
                {
                    CheckBurpingRequired(batch);
                }

                if (batch.ElapsedWeeks >= _maxCuringWeeks)
                {
                    batch.IsComplete = true;
                    batch.CompletionDate = DateTime.Now;
                    batch.FinalQuality = GradeQuality(batch.CurrentQuality);

                    OnCuringCompleted?.Invoke(batch.BatchId);
                    OnQualityGraded?.Invoke(batch.BatchId, batch.FinalQuality);

                    ChimeraLogger.Log("PROCESSING",
                        $"Curing complete: {batch.BatchId}, {batch.FinalQuality} grade", this);
                }

                _curingBatches[kvp.Key] = batch;
            }
        }

        /// <summary>
        /// Checks if jar curing batch needs burping.
        /// </summary>
        private void CheckBurpingRequired(CuringBatch batch)
        {
            if (batch.Method != CuringMethod.JarCuring) return;

            int requiredBurps = ProcessingHelpers.CalculateRequiredBurps(batch.ElapsedWeeks);
            if (batch.BurpCount < requiredBurps)
            {
                batch.NeedsBurping = true;
            }
        }

        /// <summary>
        /// Performs burping on jar curing batch.
        /// GAMEPLAY: Player receives alert → Burp jars → Quality maintained.
        /// </summary>
        public bool BurpJars(string batchId)
        {
            if (!_curingBatches.TryGetValue(batchId, out var batch))
                return false;

            if (batch.Method != CuringMethod.JarCuring)
            {
                ChimeraLogger.LogWarning("PROCESSING", $"Batch {batchId} is not jar curing", this);
                return false;
            }

            batch.BurpCount++;
            batch.LastBurpDate = DateTime.Now;
            batch.NeedsBurping = false;
            _curingBatches[batchId] = batch;

            ChimeraLogger.Log("PROCESSING", $"Jars burped: {batchId}, count: {batch.BurpCount}", this);
            return true;
        }

        #endregion

        #region Quality Grading

        /// <summary>
        /// Grades final quality based on score.
        /// </summary>
        private ProcessingQuality GradeQuality(float qualityScore)
        {
            if (qualityScore >= _premiumPlusThreshold) return ProcessingQuality.PremiumPlus;
            if (qualityScore >= _premiumThreshold) return ProcessingQuality.Premium;
            if (qualityScore >= _goodThreshold) return ProcessingQuality.Good;
            if (qualityScore >= _averageThreshold) return ProcessingQuality.Average;
            return ProcessingQuality.Poor;
        }

        /// <summary>
        /// Gets market value multiplier for quality grade.
        /// </summary>
        public float GetQualityMultiplier(ProcessingQuality quality)
        {
            return ProcessingHelpers.GetQualityMarketMultiplier(quality);
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Gets drying batch info.
        /// </summary>
        public DryingBatch GetDryingBatch(string batchId)
        {
            return _dryingBatches.TryGetValue(batchId, out var batch) ? batch : default;
        }

        /// <summary>
        /// Gets curing batch info.
        /// </summary>
        public CuringBatch GetCuringBatch(string batchId)
        {
            return _curingBatches.TryGetValue(batchId, out var batch) ? batch : default;
        }

        /// <summary>
        /// Gets all active drying batches.
        /// </summary>
        public List<DryingBatch> GetActiveDryingBatches()
        {
            return _dryingBatches.Values.Where(b => !b.IsComplete).ToList();
        }

        /// <summary>
        /// Gets all active curing batches.
        /// </summary>
        public List<CuringBatch> GetActiveCuringBatches()
        {
            return _curingBatches.Values.Where(b => !b.IsComplete).ToList();
        }

        /// <summary>
        /// Gets processing statistics for UI display.
        /// </summary>
        public ProcessingStats GetStatistics()
        {
            var activeDrying = _dryingBatches.Values.Where(b => !b.IsComplete).ToList();
            var activeCuring = _curingBatches.Values.Where(b => !b.IsComplete).ToList();
            var completedBatches = _curingBatches.Values.Where(b => b.IsComplete).ToList();

            return new ProcessingStats
            {
                ActiveDryingBatches = activeDrying.Count,
                ActiveCuringBatches = activeCuring.Count,
                TotalWeightDrying = activeDrying.Sum(b => b.WetWeightGrams),
                TotalWeightCuring = activeCuring.Sum(b => b.WeightGrams),
                AverageCuringQuality = activeCuring.Any() ? activeCuring.Average(b => b.CurrentQuality) : 0f,
                CompletedBatches = completedBatches.Count,
                AverageFinalQuality = completedBatches.Any() ?
                    completedBatches.Average(b => (float)b.FinalQuality) : 0f
            };
        }

        #endregion
    }
}
