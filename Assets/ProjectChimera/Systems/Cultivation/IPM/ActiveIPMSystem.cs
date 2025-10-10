using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Interfaces;
using ProjectChimera.Core.Updates;
using ProjectChimera.Data.Cultivation.Plant;

namespace ProjectChimera.Systems.Cultivation.IPM
{
    /// <summary>
    /// Active Integrated Pest Management system - realistic pest lifecycle simulation.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST:
    /// ====================================
    /// "Protect your crops from tiny threats - vigilance is profitable!"
    ///
    /// **Player Experience**:
    /// - Monitor plants for pest infestations (spider mites, aphids, fungus gnats, thrips)
    /// - Watch pests spread between nearby plants if untreated
    /// - Apply treatments (organic/chemical sprays, beneficial organisms)
    /// - Use beneficial insects for biological control (predatory mites, ladybugs, parasitic wasps)
    /// - Environmental factors affect pest pressure (high humidity = more issues)
    ///
    /// **Strategic Depth**:
    /// - Early detection prevents catastrophic losses
    /// - Prevention cheaper than treatment (IPM best practices)
    /// - Beneficial organisms provide long-term control
    /// - Chemical treatments can harm beneficial populations
    /// - Quarantine infected plants to prevent spread
    ///
    /// **Integration**:
    /// - Links to plant health system (stress, vigor)
    /// - Environmental conditions affect pest lifecycles
    /// - Treatment application integrated with cultivation tasks
    /// - Pest damage reduces yield and quality
    ///
    /// INVISIBLE COMPLEXITY:
    /// Players see: "Zone 1: Spider Mites detected on 3 plants" → simple alert!
    /// Behind scenes: Pest lifecycle, spread algorithms, environmental modifiers, population dynamics.
    /// </summary>
    public class ActiveIPMSystem : MonoBehaviour, IActiveIPMSystem, ITickable
    {
        [Header("Pest Lifecycle Configuration")]
        [SerializeField] private float _spiderMiteGenerationDays = 7f;
        [SerializeField] private float _aphidGenerationDays = 10f;
        [SerializeField] private float _fungusGnatGenerationDays = 14f;
        [SerializeField] private float _thripsGenerationDays = 12f;

        [Header("Spread Configuration")]
        [SerializeField] private float _spreadCheckIntervalHours = 6f;
        [SerializeField] private float _baseSpreadChance = 0.15f;
        [SerializeField] private float _spreadDistanceMax = 5f;
        [SerializeField] private float _environmentalMultiplier = 1.5f;

        [Header("Detection Configuration")]
        [SerializeField] private float _monitoringIntervalDays = 3f;
        [SerializeField] private float _detectionThreshold = 0.3f;

        // Pest infestation tracking
        private Dictionary<string, PlantInfestation> _infestations = new Dictionary<string, PlantInfestation>();
        private Dictionary<string, BeneficialColony> _beneficialOrganisms = new Dictionary<string, BeneficialColony>();
        private List<TreatmentApplication> _activetreatments = new List<TreatmentApplication>();

        // ITickable properties
        public int TickPriority => 50;
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        // Events
        public event Action<string, PestType> OnInfestationDetected;
        public event Action<string, PestType> OnInfestationSpread;
        public event Action<string, PestType> OnInfestationCleared;
        public event Action<string> OnTreatmentApplied;
        public event Action<string, BeneficialType> OnBeneficialReleased;

        private float _timeSinceLastUpdate = 0f;
        private float _timeSinceLastSpreadCheck = 0f;
        private const float UPDATE_INTERVAL_SECONDS = 3600f;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            // Register with service container
            var container = ServiceContainerFactory.Instance;
            container?.RegisterSingleton<IActiveIPMSystem>(this);

            // Register with UpdateOrchestrator
            var orchestrator = container?.Resolve<UpdateOrchestrator>();
            orchestrator?.RegisterTickable(this);

            ChimeraLogger.Log("IPM",
                "Active IPM system initialized - pest management ready!", this);
        }

        #region ITickable Implementation

        public void Tick(float deltaTime)
        {
            _timeSinceLastUpdate += deltaTime;
            _timeSinceLastSpreadCheck += deltaTime;

            // Update pest lifecycles every hour
            if (_timeSinceLastUpdate >= UPDATE_INTERVAL_SECONDS)
            {
                UpdatePestPopulations(UPDATE_INTERVAL_SECONDS);
                UpdateBeneficialOrganisms(UPDATE_INTERVAL_SECONDS);
                UpdateTreatments(UPDATE_INTERVAL_SECONDS);
                _timeSinceLastUpdate = 0f;
            }

            // Check pest spread every 6 hours
            if (_timeSinceLastSpreadCheck >= _spreadCheckIntervalHours * 3600f)
            {
                CheckPestSpread();
                _timeSinceLastSpreadCheck = 0f;
            }
        }

        #endregion

        #region Pest Infestation

        /// <summary>
        /// Introduces a pest infestation to a plant.
        /// GAMEPLAY: Random events, introduction via contaminated clones, or environmental conditions.
        /// </summary>
        public bool InfestPlant(string plantId, PestType pestType, float initialSeverity = 0.1f)
        {
            if (_infestations.ContainsKey(plantId))
            {
                // Plant already infested, increase severity
                var existing = _infestations[plantId];
                existing.Severity = Mathf.Min(1f, existing.Severity + initialSeverity);
                _infestations[plantId] = existing;
                return true;
            }

            var infestation = new PlantInfestation
            {
                PlantId = plantId,
                PestType = pestType,
                Severity = initialSeverity,
                DetectionDate = initialSeverity >= _detectionThreshold ? DateTime.Now : (DateTime?)null,
                FirstInfestationDate = DateTime.Now,
                GenerationCount = 0
            };

            _infestations[plantId] = infestation;

            if (infestation.DetectionDate.HasValue)
            {
                OnInfestationDetected?.Invoke(plantId, pestType);
                ChimeraLogger.Log("IPM",
                    $"Infestation detected: Plant {plantId}, {pestType}, {initialSeverity:P0} severity", this);
            }

            return true;
        }

        /// <summary>
        /// Updates pest populations based on lifecycle and environmental factors.
        /// </summary>
        private void UpdatePestPopulations(float deltaTimeSeconds)
        {
            float deltaTimeDays = deltaTimeSeconds / (24f * 60f * 60f); // Convert seconds to days

            foreach (var kvp in _infestations.ToList())
            {
                var infestation = kvp.Value;

                // Get pest generation time
                float generationDays = GetPestGenerationTime(infestation.PestType);

                // Calculate population growth
                float environmentalMod = CalculateEnvironmentalModifier(infestation.PestType);
                float predationReduction = CalculatePredationReduction(infestation.PestType);
                float populationMultiplier = IPMCalculationHelpers.CalculatePopulationGrowth(
                    deltaTimeDays, generationDays, environmentalMod, predationReduction);

                // Update severity
                float oldSeverity = infestation.Severity;
                infestation.Severity = Mathf.Clamp01(infestation.Severity * populationMultiplier);

                // Check for detection threshold
                if (!infestation.DetectionDate.HasValue && infestation.Severity >= _detectionThreshold)
                {
                    infestation.DetectionDate = DateTime.Now;
                    OnInfestationDetected?.Invoke(infestation.PlantId, infestation.PestType);

                    ChimeraLogger.Log("IPM",
                        $"Infestation NOW detectable: Plant {infestation.PlantId}, {infestation.PestType}, {infestation.Severity:P0} severity", this);
                }

                // Update generation count
                infestation.GenerationCount += Mathf.FloorToInt(deltaTimeDays / generationDays);

                // Check if cleared
                if (infestation.Severity < 0.01f)
                {
                    _infestations.Remove(infestation.PlantId);
                    OnInfestationCleared?.Invoke(infestation.PlantId, infestation.PestType);
                }
                else
                {
                    _infestations[kvp.Key] = infestation;
                }
            }
        }

        /// <summary>
        /// Gets pest generation time based on type.
        /// </summary>
        private float GetPestGenerationTime(PestType pestType)
        {
            return IPMCalculationHelpers.GetPestGenerationTime(pestType,
                _spiderMiteGenerationDays, _aphidGenerationDays, _fungusGnatGenerationDays, _thripsGenerationDays);
        }

        /// <summary>
        /// Calculates environmental modifier for pest growth.
        /// TODO Phase 2: Integrate with HVACIntegration for actual environmental data.
        /// </summary>
        private float CalculateEnvironmentalModifier(PestType pestType)
        {
            return 1.0f; // Neutral for Phase 1
        }

        /// <summary>
        /// Calculates predation reduction from beneficial organisms.
        /// </summary>
        private float CalculatePredationReduction(PestType pestType)
        {
            float totalReduction = 0f;

            foreach (var colony in _beneficialOrganisms.Values)
            {
                if (IPMCalculationHelpers.IsBeneficialEffectiveAgainst(colony.BeneficialType, pestType))
                {
                    float effectiveness = colony.PopulationStrength * 0.2f;
                    totalReduction += effectiveness;
                }
            }

            return Mathf.Clamp01(totalReduction);
        }

        #endregion

        #region Pest Spread

        /// <summary>
        /// Checks if pests spread from infested plants to nearby plants.
        /// </summary>
        private void CheckPestSpread()
        {
            foreach (var infestation in _infestations.Values.ToList())
            {
                if (infestation.Severity < _detectionThreshold) continue;

                float spreadChance = _baseSpreadChance * infestation.Severity;
                var nearbyPlants = GetNearbyPlants(infestation.PlantId, _spreadDistanceMax);

                foreach (var nearbyPlantId in nearbyPlants)
                {
                    if (_infestations.ContainsKey(nearbyPlantId)) continue;

                    if (UnityEngine.Random.value < spreadChance)
                    {
                        InfestPlant(nearbyPlantId, infestation.PestType, 0.05f);
                        OnInfestationSpread?.Invoke(nearbyPlantId, infestation.PestType);
                        ChimeraLogger.Log("IPM",
                            $"Pest spread: {infestation.PestType} from {infestation.PlantId} to {nearbyPlantId}", this);
                    }
                }
            }
        }

        /// <summary>
        /// Gets nearby plants within spread distance.
        /// TODO Phase 2: Integrate with spatial partitioning system.
        /// </summary>
        private List<string> GetNearbyPlants(string sourceId, float maxDistance)
        {
            return new List<string>();
        }

        #endregion

        #region Treatment Application

        /// <summary>
        /// Applies pest treatment to a plant.
        /// GAMEPLAY: Player selects plant → Apply Treatment → Choose treatment type.
        /// </summary>
        public bool ApplyTreatment(string plantId, TreatmentType treatmentType)
        {
            if (!_infestations.ContainsKey(plantId))
            {
                ChimeraLogger.LogWarning("IPM",
                    $"Cannot apply treatment to plant {plantId}: no infestation found", this);
                return false;
            }

            var infestation = _infestations[plantId];

            // Calculate treatment efficacy
            float efficacy = IPMCalculationHelpers.GetTreatmentEfficacy(treatmentType, infestation.PestType);

            // Apply treatment
            var treatment = new TreatmentApplication
            {
                TreatmentId = Guid.NewGuid().ToString(),
                PlantId = plantId,
                TreatmentType = treatmentType,
                ApplicationDate = DateTime.Now,
                Efficacy = efficacy,
                DurationDays = IPMCalculationHelpers.GetTreatmentDuration(treatmentType)
            };

            _activetreatments.Add(treatment);

            // Immediate reduction
            infestation.Severity *= (1f - efficacy);
            _infestations[plantId] = infestation;

            // Check if cleared
            if (infestation.Severity < 0.01f)
            {
                _infestations.Remove(plantId);
                OnInfestationCleared?.Invoke(plantId, infestation.PestType);
            }

            OnTreatmentApplied?.Invoke(plantId);

            ChimeraLogger.Log("IPM",
                $"Treatment applied: Plant {plantId}, {treatmentType}, {efficacy:P0} efficacy", this);

            return true;
        }


        /// <summary>
        /// Updates active treatments (degrade over time).
        /// </summary>
        private void UpdateTreatments(float deltaTimeSeconds)
        {
            float deltaTimeDays = deltaTimeSeconds / (24f * 60f * 60f);

            foreach (var treatment in _activetreatments.ToList())
            {
                var daysElapsed = (DateTime.Now - treatment.ApplicationDate).TotalDays;

                if (daysElapsed >= treatment.DurationDays)
                {
                    _activetreatments.Remove(treatment);
                }
            }
        }

        #endregion

        #region Beneficial Organisms

        /// <summary>
        /// Releases beneficial organisms for biological control.
        /// GAMEPLAY: Player purchases beneficial insects → Release in zone → Long-term control.
        /// </summary>
        public bool ReleaseBeneficialOrganisms(string zoneId, BeneficialType beneficialType, float initialPopulation = 1.0f)
        {
            string colonyId = $"{zoneId}_{beneficialType}";

            if (_beneficialOrganisms.ContainsKey(colonyId))
            {
                // Boost existing colony
                var existing = _beneficialOrganisms[colonyId];
                existing.PopulationStrength = Mathf.Min(2.0f, existing.PopulationStrength + initialPopulation);
                _beneficialOrganisms[colonyId] = existing;
                return true;
            }

            var colony = new BeneficialColony
            {
                ColonyId = colonyId,
                ZoneId = zoneId,
                BeneficialType = beneficialType,
                PopulationStrength = initialPopulation,
                ReleaseDate = DateTime.Now,
                LastUpdateDate = DateTime.Now
            };

            _beneficialOrganisms[colonyId] = colony;

            OnBeneficialReleased?.Invoke(zoneId, beneficialType);

            ChimeraLogger.Log("IPM",
                $"Beneficial organisms released: Zone {zoneId}, {beneficialType}, {initialPopulation:F1} population", this);

            return true;
        }

        /// <summary>
        /// Updates beneficial organism populations.
        /// </summary>
        private void UpdateBeneficialOrganisms(float deltaTimeSeconds)
        {
            float deltaTimeDays = deltaTimeSeconds / (24f * 60f * 60f);

            foreach (var kvp in _beneficialOrganisms.ToList())
            {
                var colony = kvp.Value;

                // Update population based on pest pressure
                float pestPressure = CalculatePestPressureInZone(colony.ZoneId);
                float populationChange = IPMCalculationHelpers.CalculateBeneficialPopulationChange(pestPressure, deltaTimeDays);

                colony.PopulationStrength += populationChange;
                colony.PopulationStrength = Mathf.Clamp(colony.PopulationStrength, 0f, 2.0f);
                colony.LastUpdateDate = DateTime.Now;

                // Remove extinct colonies
                if (colony.PopulationStrength < 0.01f)
                {
                    _beneficialOrganisms.Remove(kvp.Key);
                    continue;
                }

                _beneficialOrganisms[kvp.Key] = colony;
            }
        }

        /// <summary>
        /// Calculates pest pressure in a zone.
        /// TODO Phase 2: Query plants by zone from spatial system.
        /// </summary>
        private float CalculatePestPressureInZone(string zoneId)
        {
            return _infestations.Count > 0 ? 0.5f : 0f;
        }


        #endregion

        #region Query Methods

        /// <summary>
        /// Gets infestation data for a plant.
        /// </summary>
        public PlantInfestation GetInfestation(string plantId)
        {
            return _infestations.TryGetValue(plantId, out var infestation) ? infestation : default;
        }

        /// <summary>
        /// Checks if plant is infested.
        /// </summary>
        public bool IsPlantInfested(string plantId)
        {
            return _infestations.ContainsKey(plantId);
        }

        /// <summary>
        /// Gets all active infestations.
        /// </summary>
        public List<PlantInfestation> GetAllInfestations()
        {
            return _infestations.Values.ToList();
        }

        /// <summary>
        /// Gets IPM statistics for UI display.
        /// </summary>
        public IPMStats GetStatistics()
        {
            var infestations = _infestations.Values.ToList();

            return new IPMStats
            {
                TotalInfestations = infestations.Count,
                DetectableInfestations = infestations.Count(i => i.DetectionDate.HasValue),
                AverageSeverity = infestations.Any() ? infestations.Average(i => i.Severity) : 0f,
                BeneficialColonies = _beneficialOrganisms.Count,
                ActiveTreatments = _activetreatments.Count,
                MostCommonPest = infestations.GroupBy(i => i.PestType)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault()
            };
        }

        #endregion
    }
}
