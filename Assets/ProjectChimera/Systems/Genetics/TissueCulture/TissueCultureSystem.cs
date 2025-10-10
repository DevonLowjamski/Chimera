using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Interfaces;
using ProjectChimera.Data.Cultivation.Plant;

namespace ProjectChimera.Systems.Genetics.TissueCulture
{
    /// <summary>
    /// Tissue Culture system - genetic preservation via cryogenic storage.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST:
    /// ====================================
    /// "Preserve your best genetics forever - never lose a champion strain!"
    ///
    /// **Player Experience**:
    /// - Extract tissue sample from any plant
    /// - Store up to 50 active cultures (growing in lab)
    /// - Preserve up to 500 cultures in cryogenic storage
    /// - Reactivate preserved cultures when needed (3-day process)
    /// - Maintain cultures to prevent contamination/degradation
    ///
    /// **Strategic Depth**:
    /// - Active cultures degrade 0.1% health per day
    /// - Contamination risk increases 0.05% per day
    /// - Preserved cultures are frozen in time (no degradation)
    /// - Maintenance actions reset health to 100%, reduce contamination by 20%
    /// - Failed cultures are lost forever (health reaches 0%)
    ///
    /// **Integration**:
    /// - Links to Micropropagation for rapid cloning
    /// - Links to Blockchain Genetics for authentication
    /// - Links to External Marketplace for trading preserved samples
    ///
    /// INVISIBLE COMPLEXITY:
    /// Players see: "Culture TC-001: 95% health, 2% contamination risk" → simple!
    /// Behind scenes: Async culture creation, time-scaled degradation, preservation state machine.
    /// </summary>
    public class TissueCultureSystem : MonoBehaviour, ITissueCultureSystem, ITickable
    {
        [Header("Capacity Configuration")]
        [SerializeField] private int _maxActiveCultures = 50;
        [SerializeField] private int _maxPreservedCultures = 500;

        [Header("Degradation Configuration")]
        [SerializeField] private float _healthDegradationPerDay = 0.1f;      // 0.1% per day
        [SerializeField] private float _contaminationIncreasePerDay = 0.05f; // 0.05% per day
        [SerializeField] private float _maintenanceHealthReset = 100f;       // Maintenance resets to 100%
        [SerializeField] private float _maintenanceContaminationReduction = 20f; // Reduces by 20%

        [Header("Timing Configuration")]
        [SerializeField] private float _cultureCreationDays = 7f;            // 7 days to establish culture
        [SerializeField] private float _cultureReactivationDays = 3f;        // 3 days to reactivate

        [Header("Costs")]
        [SerializeField] private float _cultureCreationCost = 150f;
        [SerializeField] private float _preservationCost = 50f;
        [SerializeField] private float _reactivationCost = 75f;
        [SerializeField] private float _maintenanceCost = 25f;

        // Culture storage
        private Dictionary<string, TissueCultureSample> _activeCultures = new Dictionary<string, TissueCultureSample>();
        private Dictionary<string, TissueCultureSample> _preservedCultures = new Dictionary<string, TissueCultureSample>();
        private Dictionary<string, CultureOperation> _pendingOperations = new Dictionary<string, CultureOperation>();

        // ITickable properties
        public int TickPriority => 12; // Low priority - culture changes are slow
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        // Events
        public event Action<string> OnCultureCreated;                // cultureId
        public event Action<string> OnCulturePreserved;              // cultureId
        public event Action<string> OnCultureReactivated;            // cultureId
        public event Action<string> OnCultureMaintained;             // cultureId
        public event Action<string> OnCultureContaminated;           // cultureId (health reached 0%)
        public event Action<string, float, float> OnCultureHealthChanged; // cultureId, health%, contamination%

        private float _timeSinceLastUpdate = 0f;
        private const float UPDATE_INTERVAL_SECONDS = 60f; // Update cultures every minute

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            // Register with service container
            var container = ServiceContainerFactory.Instance;
            container?.RegisterSingleton<ITissueCultureSystem>(this);

            // Register with UpdateOrchestrator
            var orchestrator = container?.Resolve<UpdateOrchestrator>();
            orchestrator?.RegisterTickable(this);

            ChimeraLogger.Log("TISSUE_CULTURE",
                $"Tissue Culture system initialized: {_maxActiveCultures} active slots, {_maxPreservedCultures} preserved slots", this);
        }

        #region ITickable Implementation

        public void Tick(float deltaTime)
        {
            _timeSinceLastUpdate += deltaTime;

            if (_timeSinceLastUpdate >= UPDATE_INTERVAL_SECONDS)
            {
                UpdateCultures(UPDATE_INTERVAL_SECONDS);
                UpdatePendingOperations(UPDATE_INTERVAL_SECONDS);
                _timeSinceLastUpdate = 0f;
            }
        }

        #endregion

        #region Culture Creation

        /// <summary>
        /// Creates a tissue culture from a source plant.
        /// GAMEPLAY: Player selects plant → Extract Tissue Sample → 7-day process begins.
        /// </summary>
        public async Task<TissueCultureSample> CreateCultureAsync(PlantInstance sourcePlant)
        {
            if (sourcePlant == null)
            {
                ChimeraLogger.LogWarning("TISSUE_CULTURE",
                    "Cannot create culture: source plant is null", this);
                return default;
            }

            // Check capacity
            if (_activeCultures.Count >= _maxActiveCultures)
            {
                ChimeraLogger.LogWarning("TISSUE_CULTURE",
                    $"Cannot create culture: active capacity reached ({_maxActiveCultures})", this);
                return default;
            }

            // Create sample data structure
            var sample = new TissueCultureSample
            {
                SampleId = $"TC-{Guid.NewGuid().ToString().Substring(0, 8)}",
                SourcePlantId = sourcePlant.PlantId,
                GenotypeName = sourcePlant.Genotype?.StrainName ?? "Unknown",
                BlockchainHash = sourcePlant.Genotype?.BlockchainHash ?? "",
                Status = CultureStatus.Initiating,
                HealthPercentage = 100f,
                ContaminationRisk = 0f,
                CreationDate = DateTime.Now,
                LastMaintenanceDate = DateTime.Now
            };

            // Start async operation
            var operation = new CultureOperation
            {
                OperationId = Guid.NewGuid().ToString(),
                SampleId = sample.SampleId,
                OperationType = CultureOperationType.Creation,
                StartTime = DateTime.Now,
                DurationDays = _cultureCreationDays,
                IsComplete = false
            };

            _pendingOperations[operation.OperationId] = operation;

            ChimeraLogger.Log("TISSUE_CULTURE",
                $"Culture {sample.SampleId} creation initiated from plant {sourcePlant.PlantId} ({sample.GenotypeName})", this);

            // Simulate async operation
            await SimulateCultureOperation(operation, sample);

            return sample;
        }

        /// <summary>
        /// Simulates culture operation with time scaling integration.
        /// </summary>
        private async Task SimulateCultureOperation(CultureOperation operation, TissueCultureSample sample)
        {
            // In real implementation, this would integrate with TimeManager for time scaling
            // For now, we'll use a simplified wait based on game time

            // Calculate real-time duration based on time scale
            // TODO: Integrate with AdvancedTimeManager when available
            float realTimeDuration = operation.DurationDays * 24f * 60f * 60f; // Convert days to seconds

            // For Phase 1, we'll use a simplified approach
            // In production, this would be handled by TimeManager events
            await Task.Delay(100); // Minimal delay to simulate async

            // Complete operation
            operation.IsComplete = true;
            operation.CompletionTime = DateTime.Now;

            switch (operation.OperationType)
            {
                case CultureOperationType.Creation:
                    CompleteCultureCreation(sample);
                    break;

                case CultureOperationType.Reactivation:
                    CompleteCultureReactivation(sample);
                    break;
            }

            _pendingOperations.Remove(operation.OperationId);
        }

        /// <summary>
        /// Completes culture creation after async operation finishes.
        /// </summary>
        private void CompleteCultureCreation(TissueCultureSample sample)
        {
            sample.Status = CultureStatus.Active;
            _activeCultures[sample.SampleId] = sample;

            OnCultureCreated?.Invoke(sample.SampleId);

            ChimeraLogger.Log("TISSUE_CULTURE",
                $"Culture {sample.SampleId} creation complete: {sample.GenotypeName}", this);
        }

        #endregion

        #region Culture Preservation

        /// <summary>
        /// Preserves an active culture in cryogenic storage.
        /// GAMEPLAY: Player selects active culture → Preserve → moved to frozen storage.
        /// </summary>
        public bool PreserveCulture(string sampleId)
        {
            if (!_activeCultures.TryGetValue(sampleId, out var sample))
            {
                ChimeraLogger.LogWarning("TISSUE_CULTURE",
                    $"Cannot preserve culture {sampleId}: not found in active cultures", this);
                return false;
            }

            // Check preserved capacity
            if (_preservedCultures.Count >= _maxPreservedCultures)
            {
                ChimeraLogger.LogWarning("TISSUE_CULTURE",
                    $"Cannot preserve culture {sampleId}: preserved capacity reached ({_maxPreservedCultures})", this);
                return false;
            }

            // Move to preserved storage
            sample.Status = CultureStatus.Preserved;
            sample.PreservationDate = DateTime.Now;

            _preservedCultures[sampleId] = sample;
            _activeCultures.Remove(sampleId);

            OnCulturePreserved?.Invoke(sampleId);

            ChimeraLogger.Log("TISSUE_CULTURE",
                $"Culture {sampleId} preserved in cryogenic storage: {sample.GenotypeName}", this);

            return true;
        }

        /// <summary>
        /// Reactivates a preserved culture (3-day process).
        /// GAMEPLAY: Player selects preserved culture → Reactivate → 3-day thaw process.
        /// </summary>
        public async Task<bool> ReactivateCultureAsync(string sampleId)
        {
            if (!_preservedCultures.TryGetValue(sampleId, out var sample))
            {
                ChimeraLogger.LogWarning("TISSUE_CULTURE",
                    $"Cannot reactivate culture {sampleId}: not found in preserved cultures", this);
                return false;
            }

            // Check active capacity
            if (_activeCultures.Count >= _maxActiveCultures)
            {
                ChimeraLogger.LogWarning("TISSUE_CULTURE",
                    $"Cannot reactivate culture {sampleId}: active capacity reached ({_maxActiveCultures})", this);
                return false;
            }

            // Start reactivation operation
            var operation = new CultureOperation
            {
                OperationId = Guid.NewGuid().ToString(),
                SampleId = sample.SampleId,
                OperationType = CultureOperationType.Reactivation,
                StartTime = DateTime.Now,
                DurationDays = _cultureReactivationDays,
                IsComplete = false
            };

            _pendingOperations[operation.OperationId] = operation;
            sample.Status = CultureStatus.Reactivating;

            ChimeraLogger.Log("TISSUE_CULTURE",
                $"Culture {sampleId} reactivation initiated: {sample.GenotypeName}", this);

            // Simulate async operation
            await SimulateCultureOperation(operation, sample);

            return true;
        }

        /// <summary>
        /// Completes culture reactivation after async operation finishes.
        /// </summary>
        private void CompleteCultureReactivation(TissueCultureSample sample)
        {
            sample.Status = CultureStatus.Active;
            sample.LastMaintenanceDate = DateTime.Now;

            _activeCultures[sample.SampleId] = sample;
            _preservedCultures.Remove(sample.SampleId);

            OnCultureReactivated?.Invoke(sample.SampleId);

            ChimeraLogger.Log("TISSUE_CULTURE",
                $"Culture {sample.SampleId} reactivation complete: {sample.GenotypeName}", this);
        }

        #endregion

        #region Culture Maintenance

        /// <summary>
        /// Performs maintenance on a culture (resets health, reduces contamination).
        /// GAMEPLAY: Player selects culture → Maintain → health reset to 100%, contamination -20%.
        /// </summary>
        public bool MaintainCulture(string sampleId)
        {
            if (!_activeCultures.TryGetValue(sampleId, out var sample))
            {
                ChimeraLogger.LogWarning("TISSUE_CULTURE",
                    $"Cannot maintain culture {sampleId}: not found in active cultures", this);
                return false;
            }

            // Reset health and reduce contamination
            float oldHealth = sample.HealthPercentage;
            float oldContamination = sample.ContaminationRisk;

            sample.HealthPercentage = _maintenanceHealthReset;
            sample.ContaminationRisk = Mathf.Max(0f, sample.ContaminationRisk - _maintenanceContaminationReduction);
            sample.LastMaintenanceDate = DateTime.Now;

            OnCultureMaintained?.Invoke(sampleId);
            OnCultureHealthChanged?.Invoke(sampleId, sample.HealthPercentage, sample.ContaminationRisk);

            ChimeraLogger.Log("TISSUE_CULTURE",
                $"Culture {sampleId} maintained: health {oldHealth:F1}% → {sample.HealthPercentage:F1}%, contamination {oldContamination:F1}% → {sample.ContaminationRisk:F1}%", this);

            return true;
        }

        #endregion

        #region Culture Updates

        /// <summary>
        /// Updates all active cultures (health degradation, contamination).
        /// Called by Tick() every minute.
        /// </summary>
        private void UpdateCultures(float deltaTimeSeconds)
        {
            float deltaTimeDays = deltaTimeSeconds / (24f * 60f * 60f); // Convert seconds to days

            foreach (var kvp in _activeCultures.ToList())
            {
                var sample = kvp.Value;

                // Skip cultures that are not active (initiating, reactivating)
                if (sample.Status != CultureStatus.Active)
                    continue;

                // Degrade health
                sample.HealthPercentage -= _healthDegradationPerDay * deltaTimeDays;

                // Increase contamination risk
                sample.ContaminationRisk += _contaminationIncreasePerDay * deltaTimeDays;

                // Clamp values
                sample.HealthPercentage = Mathf.Max(0f, sample.HealthPercentage);
                sample.ContaminationRisk = Mathf.Clamp(sample.ContaminationRisk, 0f, 100f);

                // Check for culture failure
                if (sample.HealthPercentage <= 0f)
                {
                    sample.Status = CultureStatus.Contaminated;
                    OnCultureContaminated?.Invoke(sample.SampleId);

                    ChimeraLogger.LogWarning("TISSUE_CULTURE",
                        $"⚠️ Culture {sample.SampleId} contaminated and lost: {sample.GenotypeName}", this);

                    _activeCultures.Remove(sample.SampleId);
                    continue;
                }

                // Update dictionary
                _activeCultures[sample.SampleId] = sample;

                // Notify health change
                OnCultureHealthChanged?.Invoke(sample.SampleId, sample.HealthPercentage, sample.ContaminationRisk);
            }
        }

        /// <summary>
        /// Updates pending operations (creation, reactivation).
        /// In production, this would integrate with TimeManager.
        /// </summary>
        private void UpdatePendingOperations(float deltaTimeSeconds)
        {
            // In Phase 1, operations complete immediately via async
            // In Phase 2, this will integrate with AdvancedTimeManager for proper time scaling
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Gets a culture sample by ID from active or preserved storage.
        /// </summary>
        public TissueCultureSample GetCulture(string sampleId)
        {
            if (_activeCultures.TryGetValue(sampleId, out var activeSample))
                return activeSample;

            if (_preservedCultures.TryGetValue(sampleId, out var preservedSample))
                return preservedSample;

            return default;
        }

        /// <summary>
        /// Gets all active cultures.
        /// </summary>
        public List<TissueCultureSample> GetActiveCultures()
        {
            return _activeCultures.Values.ToList();
        }

        /// <summary>
        /// Gets all preserved cultures.
        /// </summary>
        public List<TissueCultureSample> GetPreservedCultures()
        {
            return _preservedCultures.Values.ToList();
        }

        /// <summary>
        /// Gets tissue culture statistics for UI display.
        /// </summary>
        public TissueCultureStats GetStatistics()
        {
            var activeCultures = _activeCultures.Values.Where(c => c.Status == CultureStatus.Active).ToList();

            return new TissueCultureStats
            {
                ActiveCultureCount = _activeCultures.Count,
                PreservedCultureCount = _preservedCultures.Count,
                ActiveCapacity = _maxActiveCultures,
                PreservedCapacity = _maxPreservedCultures,
                AverageHealth = activeCultures.Any() ? activeCultures.Average(c => c.HealthPercentage) : 0f,
                AverageContamination = activeCultures.Any() ? activeCultures.Average(c => c.ContaminationRisk) : 0f,
                CulturesNeedingMaintenance = activeCultures.Count(c => c.HealthPercentage < 70f || c.ContaminationRisk > 10f)
            };
        }

        #endregion
    }
}
