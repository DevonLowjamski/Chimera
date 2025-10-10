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
    /// Curing system - manages jar curing with burping mechanics.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST:
    /// ====================================
    /// "Curing develops the smooth, rich flavors that command premium prices"
    ///
    /// **Curing Process (2-8 weeks)**:
    /// - Place dried material in mason jars (75% full)
    /// - Maintain 62% humidity inside jars
    /// - "Burp" jars daily (open to release moisture)
    /// - Reduce burping frequency over time
    /// - Longer cure = better quality/flavor
    ///
    /// **Player Experience**:
    /// - Fill jars with dried material
    /// - Get daily reminders to burp jars
    /// - Watch quality improve week by week
    /// - Choose when to stop (2 weeks minimum, 8 weeks for premium)
    ///
    /// **Burping Mechanics**:
    /// - Week 1-2: Burp daily (24 hours)
    /// - Week 3-4: Burp every 2-3 days
    /// - Week 5+: Burp weekly
    /// - Miss burps = humidity rises = mold risk
    ///
    /// INVISIBLE COMPLEXITY:
    /// Players see: "Week 3 - Time to burp! Quality: 85% (+5% this week)"
    /// Behind scenes: Humidity equilibrium models, terpene development curves.
    /// </summary>
    public class CuringSystem : MonoBehaviour, ITickable
    {
        [Header("Curing Configuration")]
        [SerializeField] private float _tickIntervalSeconds = 3600f; // Tick every game hour
        [SerializeField] private bool _enableDebugLogging = false;

        // ITickable implementation
        public int TickPriority => 50; // Mid-priority processing system
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        [Header("Ideal Conditions")]
        [SerializeField] private float _idealJarHumidity = 0.62f;    // 62% Boveda pack standard
        [SerializeField] private float _idealFillPercentage = 0.75f; // 75% full
        [SerializeField] private float _idealTemperature = 18f;      // 18°C cool storage

        [Header("Curing Parameters")]
        [SerializeField] private int _minimumCuringWeeks = 2;        // 2 weeks minimum
        [SerializeField] private int _optimalCuringWeeks = 6;        // 6 weeks optimal
        [SerializeField] private int _maximumCuringWeeks = 12;       // 12 weeks max benefit

        [Header("Burping Schedule")]
        [SerializeField] private int _week1BurpHours = 24;           // Daily week 1-2
        [SerializeField] private int _week3BurpHours = 48;           // Every 2 days week 3-4
        [SerializeField] private int _week5BurpHours = 168;          // Weekly week 5+

        // Active curing batches
        private Dictionary<string, ProcessingBatch> _curingBatches = new Dictionary<string, ProcessingBatch>();
        private Dictionary<string, CuringJarConfig> _jarConfigs = new Dictionary<string, CuringJarConfig>();

        // Services
        private ITimeManager _timeManager;

        // Tick tracking
        private float _tickTimer = 0f;

        // Events
        public event Action<ProcessingBatch> OnCuringStarted;
        public event Action<ProcessingBatch, CuringMetrics> OnCuringProgress;
        public event Action<ProcessingBatch> OnCuringComplete;
        public event Action<ProcessingBatch, string> OnBurpReminder;
        public event Action<ProcessingBatch, string> OnCuringIssue;

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
                container.RegisterSingleton<CuringSystem>(this);
            }

            ChimeraLogger.Log("PROCESSING",
                "Curing system initialized", this);
        }

        /// <summary>
        /// Starts curing process for a dried batch.
        ///
        /// GAMEPLAY:
        /// - Player finishes drying
        /// - Places material in mason jars
        /// - Selects cure duration (2-8+ weeks)
        /// - Gets daily burp reminders
        /// </summary>
        public bool StartCuring(ProcessingBatch batch, CuringJarConfig jarConfig)
        {
            if (batch == null)
            {
                ChimeraLogger.LogWarning("PROCESSING",
                    "Cannot start curing: batch is null", this);
                return false;
            }

            if (batch.Stage != ProcessingStage.Dried)
            {
                ChimeraLogger.LogWarning("PROCESSING",
                    $"Cannot start curing: batch {batch.BatchId} not dried (Stage: {batch.Stage})", this);
                return false;
            }

            if (_curingBatches.ContainsKey(batch.BatchId))
            {
                ChimeraLogger.LogWarning("PROCESSING",
                    $"Batch {batch.BatchId} already curing", this);
                return false;
            }

            // Set up batch for curing
            batch.Stage = ProcessingStage.Curing;
            batch.CuringStartDate = DateTime.Now;
            batch.CuringWeeksElapsed = 0;
            batch.JarHumidity = 0.65f; // Starts slightly high, burping reduces it
            jarConfig.LastBurpTime = DateTime.Now; // Just sealed, burp in 24hrs

            _curingBatches[batch.BatchId] = batch;
            _jarConfigs[batch.BatchId] = jarConfig;

            OnCuringStarted?.Invoke(batch);

            ChimeraLogger.Log("PROCESSING",
                $"Started curing: {batch.StrainName} ({batch.WeightGrams}g) - " +
                $"Target: {batch.TargetCuringWeeks} weeks", this);

            return true;
        }

        /// <summary>
        /// ITickable implementation - processes curing every game hour.
        /// </summary>
        public void Tick(float deltaTime)
        {
            _tickTimer += deltaTime;

            if (_tickTimer >= _tickIntervalSeconds)
            {
                _tickTimer = 0f;
                ProcessCuringTick();
            }
        }

        /// <summary>
        /// Processes curing for all active batches.
        /// </summary>
        private void ProcessCuringTick()
        {
            if (_curingBatches.Count == 0)
                return;

            var batchesToComplete = new List<string>();

            foreach (var kvp in _curingBatches)
            {
                var batch = kvp.Value;
                var jarConfig = _jarConfigs[kvp.Key];

                // Advance curing by one hour
                AdvanceCuring(batch, jarConfig, 1f / 24f / 7f); // 1 hour = 1/(24*7)th of a week

                // Update weeks elapsed
                batch.CuringWeeksElapsed = Mathf.FloorToInt((DateTime.Now - batch.CuringStartDate).Days / 7);

                // Check burp reminders
                if (jarConfig.NeedsBurping())
                {
                    OnBurpReminder?.Invoke(batch, $"Time to burp jar - {batch.StrainName}");
                }

                // Check if curing complete
                if (batch.CuringWeeksElapsed >= batch.TargetCuringWeeks)
                {
                    batchesToComplete.Add(kvp.Key);
                }

                // Report progress
                var metrics = GetCuringMetrics(batch, jarConfig);
                OnCuringProgress?.Invoke(batch, metrics);
            }

            // Complete batches
            foreach (var batchId in batchesToComplete)
            {
                CompleteCuring(batchId);
            }
        }

        /// <summary>
        /// Advances curing for a batch by a fraction of a week.
        /// </summary>
        private void AdvanceCuring(ProcessingBatch batch, CuringJarConfig jarConfig, float weekFraction)
        {
            // Update jar humidity (rises over time without burping)
            float hoursSinceLastBurp = (float)(DateTime.Now - jarConfig.LastBurpTime).TotalHours;
            float humidityIncrease = hoursSinceLastBurp * 0.001f; // +0.1% per hour
            batch.JarHumidity = Mathf.Min(0.75f, batch.JarHumidity + humidityIncrease * weekFraction);

            // Calculate quality improvement from curing
            ApplyCuringQualityEffects(batch, jarConfig, weekFraction);

            // Check for issues
            UpdateCuringRisks(batch, jarConfig);
        }

        /// <summary>
        /// Applies quality improvements from curing.
        /// </summary>
        private void ApplyCuringQualityEffects(ProcessingBatch batch, CuringJarConfig jarConfig, float weekFraction)
        {
            float qualityChange = 0f;

            // Ideal humidity range improves quality
            if (batch.JarHumidity >= 0.60f && batch.JarHumidity <= 0.64f)
            {
                // Perfect range: +1-2% quality per week depending on current cure time
                float improvementRate = CalculateImprovementRate(batch.CuringWeeksElapsed);
                qualityChange += improvementRate * weekFraction;
            }
            else if (batch.JarHumidity >= 0.58f && batch.JarHumidity <= 0.66f)
            {
                // Good range: +0.5% per week
                qualityChange += 0.5f * weekFraction;
            }

            // Too humid degrades quality (mold risk)
            if (batch.JarHumidity > 0.70f)
            {
                qualityChange -= (batch.JarHumidity - 0.70f) * 10f * weekFraction;
            }

            // Too dry degrades quality (terpene loss)
            if (batch.JarHumidity < 0.55f)
            {
                qualityChange -= (0.55f - batch.JarHumidity) * 5f * weekFraction;
            }

            // Overfilled jar reduces air circulation
            if (jarConfig.FillPercentage > 0.85f)
            {
                qualityChange -= 0.2f * weekFraction;
            }

            // Apply change
            batch.CurrentQuality = Mathf.Clamp(batch.CurrentQuality + qualityChange, 0f, 100f);
        }

        /// <summary>
        /// Calculates quality improvement rate based on cure time.
        /// Early weeks improve fastest, diminishing returns later.
        /// </summary>
        private float CalculateImprovementRate(int weeksElapsed)
        {
            // Week 1-2: +2% per week (rapid improvement)
            if (weeksElapsed < 2) return 2.0f;

            // Week 3-4: +1.5% per week
            if (weeksElapsed < 4) return 1.5f;

            // Week 5-6: +1% per week
            if (weeksElapsed < 6) return 1.0f;

            // Week 7-8: +0.5% per week
            if (weeksElapsed < 8) return 0.5f;

            // Week 9+: +0.2% per week (minimal improvement)
            return 0.2f;
        }

        /// <summary>
        /// Updates curing risks (mold, over-drying).
        /// </summary>
        private void UpdateCuringRisks(ProcessingBatch batch, CuringJarConfig jarConfig)
        {
            // Mold risk from high humidity
            if (batch.JarHumidity > 0.70f)
            {
                batch.MoldRisk = (batch.JarHumidity - 0.70f) * 10f;
                if (batch.MoldRisk > 0.7f)
                {
                    OnCuringIssue?.Invoke(batch, "High humidity - burp jars immediately!");
                }
            }
            else
            {
                batch.MoldRisk = 0f;
            }

            // Over-dry risk from low humidity
            if (batch.JarHumidity < 0.55f)
            {
                batch.OverDryRisk = (0.55f - batch.JarHumidity) * 5f;
                if (batch.OverDryRisk > 0.7f)
                {
                    OnCuringIssue?.Invoke(batch, "Humidity too low - add humidity pack or reduce burping");
                }
            }
            else
            {
                batch.OverDryRisk = 0f;
            }
        }

        /// <summary>
        /// Player burps a jar (opens to release moisture).
        ///
        /// GAMEPLAY:
        /// - Player clicks "Burp Jar" button
        /// - Jar opens for 15-30 minutes
        /// - Humidity drops 3-5%
        /// - Quality preserved
        /// - Next burp scheduled based on week
        /// </summary>
        public void BurpJar(string batchId)
        {
            if (!_curingBatches.TryGetValue(batchId, out var batch) ||
                !_jarConfigs.TryGetValue(batchId, out var jarConfig))
            {
                ChimeraLogger.LogWarning("PROCESSING",
                    $"Cannot burp: batch {batchId} not found", this);
                return;
            }

            // Reduce humidity from burping
            float humidityReduction = UnityEngine.Random.Range(0.03f, 0.05f); // 3-5% drop
            batch.JarHumidity = Mathf.Max(0.55f, batch.JarHumidity - humidityReduction);

            // Update last burp time
            jarConfig.LastBurpTime = DateTime.Now;
            _jarConfigs[batchId] = jarConfig;

            // Update burp frequency based on cure time
            int newBurpFrequency = CuringSystemHelpers.GetBurpFrequency(batch.CuringWeeksElapsed);
            jarConfig.BurpFrequencyHours = newBurpFrequency;

            ChimeraLogger.Log("PROCESSING",
                $"Burped jar: {batch.StrainName} - " +
                $"Humidity: {batch.JarHumidity * 100f:F1}%, " +
                $"Next burp in {newBurpFrequency} hours", this);
        }

        /// <summary>
        /// Completes curing for a batch.
        /// </summary>
        private void CompleteCuring(string batchId)
        {
            if (!_curingBatches.TryGetValue(batchId, out var batch))
                return;

            // Check if spoiled
            if (batch.MoldRisk > 0.9f)
            {
                batch.Stage = ProcessingStage.Spoiled;
                batch.CurrentQuality = 0f;
                OnCuringIssue?.Invoke(batch, "Batch spoiled - mold growth from missed burps");
            }
            else
            {
                batch.Stage = ProcessingStage.Cured;
                batch.CompletionDate = DateTime.Now;

                // Bonus for extended cure
                if (batch.CuringWeeksElapsed >= _optimalCuringWeeks)
                {
                    batch.CurrentQuality = Mathf.Min(100f, batch.CurrentQuality + 2f);
                }
            }

            _curingBatches.Remove(batchId);
            _jarConfigs.Remove(batchId);

            OnCuringComplete?.Invoke(batch);

            ChimeraLogger.Log("PROCESSING",
                $"Curing complete: {batch.StrainName} - " +
                $"Weeks cured: {batch.CuringWeeksElapsed}, " +
                $"Final quality: {batch.CurrentQuality:F1}", this);
        }

        /// <summary>
        /// Gets curing metrics for a batch.
        /// </summary>
        public CuringMetrics GetCuringMetrics(string batchId)
        {
            if (!_curingBatches.TryGetValue(batchId, out var batch) ||
                !_jarConfigs.TryGetValue(batchId, out var jarConfig))
            {
                return default;
            }

            return GetCuringMetrics(batch, jarConfig);
        }

        /// <summary>
        /// Gets curing metrics for a batch.
        /// </summary>
        private CuringMetrics GetCuringMetrics(ProcessingBatch batch, CuringJarConfig jarConfig)
        {
            int weeksRemaining = Mathf.Max(0, batch.TargetCuringWeeks - batch.CuringWeeksElapsed);
            float qualityGain = batch.CurrentQuality - batch.InitialQuality;

            return new CuringMetrics
            {
                WeeksElapsed = batch.CuringWeeksElapsed,
                WeeksRemaining = weeksRemaining,
                JarHumidity = batch.JarHumidity,
                NeedsBurping = jarConfig.NeedsBurping(),
                QualityImprovement = qualityGain,
                TerpenePreservation = CuringSystemHelpers.CalculateTerpenePreservation(batch),
                Status = CuringSystemHelpers.GetCuringStatus(batch, jarConfig)
            };
        }


        /// <summary>
        /// Gets all active curing batches.
        /// </summary>
        public List<ProcessingBatch> GetActiveBatches()
        {
            return new List<ProcessingBatch>(_curingBatches.Values);
        }

        /// <summary>
        /// Checks if a batch is currently curing.
        /// </summary>
        public bool IsBatchCuring(string batchId)
        {
            return _curingBatches.ContainsKey(batchId);
        }

        /// <summary>
        /// Gets time until next burp (for UI countdown).
        /// </summary>
        public TimeSpan GetTimeUntilNextBurp(string batchId)
        {
            if (!_jarConfigs.TryGetValue(batchId, out var jarConfig))
                return TimeSpan.Zero;

            var timeSinceLastBurp = DateTime.Now - jarConfig.LastBurpTime;
            var timeUntilNextBurp = TimeSpan.FromHours(jarConfig.BurpFrequencyHours) - timeSinceLastBurp;

            return timeUntilNextBurp > TimeSpan.Zero ? timeUntilNextBurp : TimeSpan.Zero;
        }

        private void OnDestroy()
        {
            var container = ServiceContainerFactory.Instance;
            var orchestrator = container?.Resolve<IUpdateOrchestrator>();
            orchestrator?.UnregisterTickable(this);
        }
    }
}
