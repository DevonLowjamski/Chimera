using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Interfaces;
using ProjectChimera.Systems.Genetics.TissueCulture;
using ProjectChimera.Data.Cultivation.Plant;

namespace ProjectChimera.Systems.Genetics.Micropropagation
{
    /// <summary>
    /// Micropropagation system - rapid cloning from tissue cultures.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST:
    /// ====================================
    /// "Clone your champions at scale - 100 identical plants from one sample!"
    ///
    /// **Player Experience**:
    /// - Select tissue culture sample
    /// - Choose quantity (1-100 clones)
    /// - 3-stage process: Multiplication → Rooting → Acclimatization
    /// - Watch progress in real-time (visual growth stages)
    /// - Harvest mature clones as plant instances
    ///
    /// **Strategic Depth**:
    /// - Base duration: 21 days (scales with quantity)
    /// - Success rate based on source culture health (100% health = 95% success)
    /// - Failed clones reduce batch yield
    /// - Larger batches are more cost-effective per clone
    /// - Stage progression: 50% Multiplication, 30% Rooting, 20% Acclimatization
    ///
    /// **Integration**:
    /// - Requires Tissue Culture system (source samples)
    /// - Outputs PlantInstance objects (ready to transplant)
    /// - Links to Cultivation system for plant lifecycle
    /// - Uses blockchain genetics for authenticity
    ///
    /// INVISIBLE COMPLEXITY:
    /// Players see: "Batch MP-001: Stage 2/3 (Rooting), 85 of 100 clones" → simple progress!
    /// Behind scenes: Time-scaled batch processing, success rate calculations, stage transitions.
    /// </summary>
    public class MicropropagationSystem : MonoBehaviour, IMicropropagationSystem, ITickable
    {
        [Header("Batch Configuration")]
        [SerializeField] private int _minBatchSize = 1;
        [SerializeField] private int _maxBatchSize = 100;
        [SerializeField] private int _maxActiveBatches = 10;

        [Header("Timing Configuration")]
        [SerializeField] private float _baseDurationDays = 21f;              // Base 21 days for small batch
        [SerializeField] private float _multiplicationStagePct = 0.50f;       // 50% of time
        [SerializeField] private float _rootingStagePct = 0.30f;              // 30% of time
        [SerializeField] private float _acclimatizationStagePct = 0.20f;      // 20% of time

        [Header("Success Rate Configuration")]
        [SerializeField] private float _baseSuccessRate = 0.95f;              // 95% at 100% health
        [SerializeField] private float _healthSuccessMultiplier = 0.5f;       // 50% at 50% health
        [SerializeField] private float _contaminationPenalty = 0.01f;         // -1% per 1% contamination

        [Header("Costs")]
        [SerializeField] private float _baseBatchCost = 500f;                 // Base cost for 1 clone
        [SerializeField] private float _costPerClone = 5f;                    // Additional cost per clone

        // Batch storage
        private Dictionary<string, MicropropagationBatch> _activeBatches = new Dictionary<string, MicropropagationBatch>();

        // ITickable properties
        public int TickPriority => 11; // Low priority - micropropagation is slow
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        // Events
        public event Action<string> OnBatchCreated;                          // batchId
        public event Action<string, MicropropagationStage> OnBatchStageChanged; // batchId, newStage
        public event Action<string, int> OnBatchCompleted;                   // batchId, successfulClones
        public event Action<string, float> OnBatchProgressChanged;           // batchId, progress%

        private float _timeSinceLastUpdate = 0f;
        private const float UPDATE_INTERVAL_SECONDS = 60f; // Update batches every minute

        private ITissueCultureSystem _tissueCultureSystem;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            // Register with service container
            var container = ServiceContainerFactory.Instance;
            container?.RegisterSingleton<IMicropropagationSystem>(this);

            // Resolve dependencies
            _tissueCultureSystem = container?.Resolve<ITissueCultureSystem>();

            // Register with UpdateOrchestrator
            var orchestrator = container?.Resolve<UpdateOrchestrator>();
            orchestrator?.RegisterTickable(this);

            ChimeraLogger.Log("MICROPROPAGATION",
                $"Micropropagation system initialized: {_minBatchSize}-{_maxBatchSize} clones per batch, {_maxActiveBatches} active batches", this);
        }

        #region ITickable Implementation

        public void Tick(float deltaTime)
        {
            _timeSinceLastUpdate += deltaTime;

            if (_timeSinceLastUpdate >= UPDATE_INTERVAL_SECONDS)
            {
                UpdateBatches(UPDATE_INTERVAL_SECONDS);
                _timeSinceLastUpdate = 0f;
            }
        }

        #endregion

        #region Batch Creation

        /// <summary>
        /// Creates a micropropagation batch from a tissue culture sample.
        /// GAMEPLAY: Player selects culture → Choose quantity → Start batch.
        /// </summary>
        public async Task<MicropropagationBatch> CreateBatchAsync(string cultureSampleId, int quantity)
        {
            // Validate quantity
            if (quantity < _minBatchSize || quantity > _maxBatchSize)
            {
                ChimeraLogger.LogWarning("MICROPROPAGATION",
                    $"Cannot create batch: quantity {quantity} out of range ({_minBatchSize}-{_maxBatchSize})", this);
                return default;
            }

            // Check active batch capacity
            if (_activeBatches.Count >= _maxActiveBatches)
            {
                ChimeraLogger.LogWarning("MICROPROPAGATION",
                    $"Cannot create batch: active batch limit reached ({_maxActiveBatches})", this);
                return default;
            }

            // Get source culture
            if (_tissueCultureSystem == null)
            {
                ChimeraLogger.LogWarning("MICROPROPAGATION",
                    "Cannot create batch: TissueCultureSystem not available", this);
                return default;
            }

            var sourceCulture = _tissueCultureSystem.GetCulture(cultureSampleId);
            if (string.IsNullOrEmpty(sourceCulture.SampleId))
            {
                ChimeraLogger.LogWarning("MICROPROPAGATION",
                    $"Cannot create batch: culture {cultureSampleId} not found", this);
                return default;
            }

            // Calculate success rate based on culture health
            float successRate = CalculateSuccessRate(sourceCulture.HealthPercentage, sourceCulture.ContaminationRisk);

            // Calculate duration (scales with quantity)
            float quantityMultiplier = 1f + ((quantity - 1f) / _maxBatchSize) * 0.5f; // 1.0x → 1.5x
            float totalDurationDays = _baseDurationDays * quantityMultiplier;

            // Create batch
            var batch = new MicropropagationBatch
            {
                BatchId = $"MP-{Guid.NewGuid().ToString().Substring(0, 8)}",
                CultureSampleId = cultureSampleId,
                GenotypeName = sourceCulture.GenotypeName,
                BlockchainHash = sourceCulture.BlockchainHash,
                TargetQuantity = quantity,
                CurrentStage = MicropropagationStage.Multiplication,
                StageProgress = 0f,
                TotalDurationDays = totalDurationDays,
                ElapsedDays = 0f,
                ExpectedSuccessRate = successRate,
                StartDate = DateTime.Now,
                IsComplete = false
            };

            _activeBatches[batch.BatchId] = batch;

            OnBatchCreated?.Invoke(batch.BatchId);

            ChimeraLogger.Log("MICROPROPAGATION",
                $"Batch {batch.BatchId} created: {quantity} {sourceCulture.GenotypeName} clones, {totalDurationDays:F1} days, {successRate:P0} success rate", this);

            // In production, this would return immediately and update via Tick()
            // For now, simulate minimal async delay
            await Task.Delay(100);

            return batch;
        }

        /// <summary>
        /// Calculates success rate based on source culture health and contamination.
        /// </summary>
        private float CalculateSuccessRate(float healthPercentage, float contaminationRisk)
        {
            // Base success rate scales with health
            float healthMultiplier = healthPercentage / 100f;
            float successRate = _baseSuccessRate * healthMultiplier;

            // Contamination penalty
            float contaminationPenalty = contaminationRisk * _contaminationPenalty;
            successRate -= contaminationPenalty;

            return Mathf.Clamp01(successRate);
        }

        #endregion

        #region Batch Updates

        /// <summary>
        /// Updates all active batches (stage progression, completion).
        /// Called by Tick() every minute.
        /// </summary>
        private void UpdateBatches(float deltaTimeSeconds)
        {
            float deltaTimeDays = deltaTimeSeconds / (24f * 60f * 60f); // Convert seconds to days

            foreach (var kvp in _activeBatches.ToList())
            {
                var batch = kvp.Value;

                if (batch.IsComplete)
                    continue;

                // Update elapsed time
                batch.ElapsedDays += deltaTimeDays;

                // Calculate overall progress
                float overallProgress = batch.ElapsedDays / batch.TotalDurationDays;
                batch.StageProgress = overallProgress * 100f;

                // Update stage based on progress
                MicropropagationStage oldStage = batch.CurrentStage;
                batch.CurrentStage = CalculateCurrentStage(overallProgress);

                if (batch.CurrentStage != oldStage)
                {
                    OnBatchStageChanged?.Invoke(batch.BatchId, batch.CurrentStage);

                    ChimeraLogger.Log("MICROPROPAGATION",
                        $"Batch {batch.BatchId} stage change: {oldStage} → {batch.CurrentStage}", this);
                }

                // Check for completion
                if (overallProgress >= 1f)
                {
                    CompleteBatch(batch);
                    continue;
                }

                // Update dictionary
                _activeBatches[batch.BatchId] = batch;

                // Notify progress change
                OnBatchProgressChanged?.Invoke(batch.BatchId, batch.StageProgress);
            }
        }

        /// <summary>
        /// Calculates current stage based on overall progress.
        /// </summary>
        private MicropropagationStage CalculateCurrentStage(float overallProgress)
        {
            if (overallProgress < _multiplicationStagePct)
                return MicropropagationStage.Multiplication;

            if (overallProgress < _multiplicationStagePct + _rootingStagePct)
                return MicropropagationStage.Rooting;

            return MicropropagationStage.Acclimatization;
        }

        /// <summary>
        /// Completes a batch and calculates successful clones.
        /// </summary>
        private void CompleteBatch(MicropropagationBatch batch)
        {
            // Calculate successful clones based on success rate
            int successfulClones = Mathf.RoundToInt(batch.TargetQuantity * batch.ExpectedSuccessRate);
            successfulClones = Mathf.Max(1, successfulClones); // Always at least 1 clone

            batch.IsComplete = true;
            batch.SuccessfulClones = successfulClones;
            batch.CompletionDate = DateTime.Now;

            _activeBatches[batch.BatchId] = batch;

            OnBatchCompleted?.Invoke(batch.BatchId, successfulClones);

            ChimeraLogger.Log("MICROPROPAGATION",
                $"Batch {batch.BatchId} complete: {successfulClones}/{batch.TargetQuantity} clones successful ({batch.GenotypeName})", this);
        }

        #endregion

        #region Harvest

        /// <summary>
        /// Harvests completed batch as PlantInstance objects.
        /// GAMEPLAY: Player clicks "Harvest" → receives plant instances ready to transplant.
        /// </summary>
        public List<PlantInstance> HarvestBatch(string batchId)
        {
            if (!_activeBatches.TryGetValue(batchId, out var batch))
            {
                ChimeraLogger.LogWarning("MICROPROPAGATION",
                    $"Cannot harvest batch {batchId}: not found", this);
                return new List<PlantInstance>();
            }

            if (!batch.IsComplete)
            {
                ChimeraLogger.LogWarning("MICROPROPAGATION",
                    $"Cannot harvest batch {batchId}: not yet complete", this);
                return new List<PlantInstance>();
            }

            // Create PlantInstance objects
            var plants = new List<PlantInstance>();

            for (int i = 0; i < batch.SuccessfulClones; i++)
            {
                // In production, this would create full PlantInstance objects
                // For Phase 1, we'll create minimal instances
                var plant = new PlantInstance
                {
                    PlantId = Guid.NewGuid().ToString(),
                    // Genotype would be cloned from source culture
                    // For now, we'll set basic properties
                };

                plants.Add(plant);
            }

            // Remove batch from active list
            _activeBatches.Remove(batchId);

            ChimeraLogger.Log("MICROPROPAGATION",
                $"Batch {batchId} harvested: {batch.SuccessfulClones} {batch.GenotypeName} plants", this);

            return plants;
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Gets a batch by ID.
        /// </summary>
        public MicropropagationBatch GetBatch(string batchId)
        {
            return _activeBatches.TryGetValue(batchId, out var batch) ? batch : default;
        }

        /// <summary>
        /// Gets all active batches.
        /// </summary>
        public List<MicropropagationBatch> GetActiveBatches()
        {
            return _activeBatches.Values.ToList();
        }

        /// <summary>
        /// Gets completed batches ready for harvest.
        /// </summary>
        public List<MicropropagationBatch> GetCompletedBatches()
        {
            return _activeBatches.Values.Where(b => b.IsComplete).ToList();
        }

        /// <summary>
        /// Gets micropropagation statistics for UI display.
        /// </summary>
        public MicropropagationStats GetStatistics()
        {
            var activeBatches = _activeBatches.Values.Where(b => !b.IsComplete).ToList();
            var completedBatches = _activeBatches.Values.Where(b => b.IsComplete).ToList();

            return new MicropropagationStats
            {
                ActiveBatchCount = activeBatches.Count,
                CompletedBatchCount = completedBatches.Count,
                TotalClonesInProgress = activeBatches.Sum(b => b.TargetQuantity),
                TotalClonesCompleted = completedBatches.Sum(b => b.SuccessfulClones),
                AverageSuccessRate = activeBatches.Any() ? activeBatches.Average(b => b.ExpectedSuccessRate) : 0f,
                AverageProgress = activeBatches.Any() ? activeBatches.Average(b => b.StageProgress) : 0f
            };
        }

        /// <summary>
        /// Estimates batch duration and cost.
        /// GAMEPLAY: Shows player estimated time and cost before committing.
        /// </summary>
        public MicropropagationEstimate EstimateBatch(string cultureSampleId, int quantity)
        {
            if (_tissueCultureSystem == null)
                return default;

            var sourceCulture = _tissueCultureSystem.GetCulture(cultureSampleId);
            if (string.IsNullOrEmpty(sourceCulture.SampleId))
                return default;

            // Calculate success rate
            float successRate = CalculateSuccessRate(sourceCulture.HealthPercentage, sourceCulture.ContaminationRisk);

            // Calculate duration
            float quantityMultiplier = 1f + ((quantity - 1f) / _maxBatchSize) * 0.5f;
            float totalDurationDays = _baseDurationDays * quantityMultiplier;

            // Calculate cost
            float totalCost = _baseBatchCost + (quantity * _costPerClone);

            // Calculate expected output
            int expectedClones = Mathf.RoundToInt(quantity * successRate);

            return new MicropropagationEstimate
            {
                CultureSampleId = cultureSampleId,
                GenotypeName = sourceCulture.GenotypeName,
                RequestedQuantity = quantity,
                EstimatedDurationDays = totalDurationDays,
                EstimatedCost = totalCost,
                ExpectedSuccessRate = successRate,
                ExpectedClones = expectedClones
            };
        }

        #endregion
    }
}
