# PROJECT CHIMERA: ULTIMATE IMPLEMENTATION ROADMAP
## Part 3: Three Pillars Completion & Advanced Features

**Document Version:** 2.0 - Updated Based on Comprehensive Codebase Assessment
**Phase Duration:** Weeks 8-13 (6 weeks)
**Prerequisites:** Phase 0 complete, Blockchain genetics operational, utilities implemented

---

## WEEK 8: GENETICS PILLAR COMPLETION - TISSUE CULTURE & MICROPROPAGATION

**Current Gap:** Both systems completely missing (0%)
**Goal:** Implement preservation and rapid cloning mechanics as described in gameplay document

### Day 1-2: Tissue Culture System

**Tissue Culture Architecture:**

```csharp
// Systems/Genetics/TissueCulture/TissueCultureSystem.cs
public class TissueCultureSystem : MonoBehaviour, ITissueCultureSystem
{
    private Dictionary<string, TissueCultureSample> _activeCultures = new();
    private Dictionary<string, TissueCultureSample> _preservedCultures = new();
    private int _maxActiveCultures = 50;
    private int _maxPreservedCultures = 500;

    public async Task<TissueCultureSample> CreateCultureAsync(PlantInstance sourceP lant)
    {
        if (_activeCultures.Count >= _maxActiveCultures)
        {
            ChimeraLogger.LogWarning("TISSUE_CULTURE",
                $"Maximum active cultures reached ({_maxActiveCultures}). Transfer to preservation or discard.", this);
            return null;
        }

        // Extract genetic material from source plant
        var genotype = sourceP lant.Genotype;

        var sample = new TissueCultureSample
        {
            SampleId = Guid.NewGuid().ToString(),
            SourcePlantId = sourceP lant.PlantId,
            Genotype = genotype.Clone(),
            BlockchainHash = genotype.BlockchainHash,
            CreationDate = DateTime.UtcNow,
            Status = CultureStatus.Initiating,
            HealthPercentage = 100f,
            ContaminationRisk = 0f,
            DaysSinceCreation = 0
        };

        // Initiation phase (simulated time)
        await SimulateCultureInitiation(sample);

        _activeCultures[sample.SampleId] = sample;

        ChimeraLogger.Log("TISSUE_CULTURE",
            $"Created tissue culture {sample.SampleId} from plant {sourceP lant.PlantId}", this);

        return sample;
    }

    public bool PreserveCulture(string sampleId)
    {
        if (!_activeCultures.TryGetValue(sampleId, out var sample))
        {
            ChimeraLogger.LogWarning("TISSUE_CULTURE",
                $"Sample {sampleId} not found in active cultures", this);
            return false;
        }

        if (_preservedCultures.Count >= _maxPreservedCultures)
        {
            ChimeraLogger.LogWarning("TISSUE_CULTURE",
                $"Maximum preserved cultures reached ({_maxPreservedCultures})", this);
            return false;
        }

        // Move to preservation (cryogenic storage)
        sample.Status = CultureStatus.Preserved;
        sample.PreservationDate = DateTime.UtcNow;

        _preservedCultures[sampleId] = sample;
        _activeCultures.Remove(sampleId);

        ChimeraLogger.Log("TISSUE_CULTURE",
            $"Preserved culture {sampleId}. Active: {_activeCultures.Count}, Preserved: {_preservedCultures.Count}", this);

        return true;
    }

    public async Task<TissueCultureSample> ReactivateCultureAsync(string sampleId)
    {
        if (!_preservedCultures.TryGetValue(sampleId, out var sample))
        {
            ChimeraLogger.LogWarning("TISSUE_CULTURE",
                $"Sample {sampleId} not found in preserved cultures", this);
            return null;
        }

        if (_activeCultures.Count >= _maxActiveCultures)
        {
            ChimeraLogger.LogWarning("TISSUE_CULTURE",
                $"Maximum active cultures reached. Cannot reactivate.", this);
            return null;
        }

        // Reactivation phase
        sample.Status = CultureStatus.Reactivating;
        await SimulateCultureReactivation(sample);

        sample.Status = CultureStatus.Active;
        sample.ReactivationDate = DateTime.UtcNow;

        _activeCultures[sampleId] = sample;
        _preservedCultures.Remove(sampleId);

        ChimeraLogger.Log("TISSUE_CULTURE",
            $"Reactivated culture {sampleId}", this);

        return sample;
    }

    public void UpdateCultures(float deltaTime)
    {
        // Update active cultures
        foreach (var culture in _activeCultures.Values.ToList())
        {
            UpdateCultureHealth(culture, deltaTime);
            UpdateContaminationRisk(culture, deltaTime);

            // Check for culture failure
            if (culture.HealthPercentage <= 0f || culture.ContaminationRisk >= 100f)
            {
                ChimeraLogger.LogWarning("TISSUE_CULTURE",
                    $"Culture {culture.SampleId} failed. Health: {culture.HealthPercentage:F1}%, Contamination: {culture.ContaminationRisk:F1}%", this);

                _activeCultures.Remove(culture.SampleId);
            }
        }
    }

    private void UpdateCultureHealth(TissueCultureSample culture, float deltaTime)
    {
        // Health degrades slowly over time without maintenance
        var degradationRate = 0.1f; // % per day
        var daysElapsed = deltaTime / (24f * 60f * 60f); // Convert seconds to days

        culture.HealthPercentage -= degradationRate * daysElapsed;
        culture.HealthPercentage = Mathf.Max(0f, culture.HealthPercentage);
        culture.DaysSinceCreation += daysElapsed;
    }

    private void UpdateContaminationRisk(TissueCultureSample culture, float deltaTime)
    {
        // Contamination risk increases with time and poor environmental control
        var baseRiskIncrease = 0.05f; // % per day
        var daysElapsed = deltaTime / (24f * 60f * 60f);

        culture.ContaminationRisk += baseRiskIncrease * daysElapsed;
        culture.ContaminationRisk = Mathf.Min(100f, culture.ContaminationRisk);
    }

    public bool MaintainCulture(string sampleId)
    {
        if (!_activeCultures.TryGetValue(sampleId, out var culture))
            return false;

        // Reset health and reduce contamination
        culture.HealthPercentage = 100f;
        culture.ContaminationRisk = Mathf.Max(0f, culture.ContaminationRisk - 20f);

        ChimeraLogger.Log("TISSUE_CULTURE",
            $"Maintained culture {sampleId}. Health: 100%, Contamination: {culture.ContaminationRisk:F1}%", this);

        return true;
    }

    private async Task SimulateCultureInitiation(TissueCultureSample sample)
    {
        // Simulate 7-day initiation period (scaled by time mechanics)
        var initiationDays = 7f;
        var scaledTime = CalculateScaledTime(initiationDays);

        await Task.Delay((int)(scaledTime * 1000)); // Convert to milliseconds

        sample.Status = CultureStatus.Active;
    }

    private async Task SimulateCultureReactivation(TissueCultureSample sample)
    {
        // Simulate 3-day reactivation period
        var reactivationDays = 3f;
        var scaledTime = CalculateScaledTime(reactivationDays);

        await Task.Delay((int)(scaledTime * 1000));
    }

    private float CalculateScaledTime(float gameDays)
    {
        // Convert game days to real seconds based on time scale
        var timeManager = ServiceContainer.Resolve<ITimeManager>();
        return gameDays * timeManager.GetSecondsPerGameDay();
    }

    public List<TissueCultureSample> GetActiveCultures() => _activeCultures.Values.ToList();
    public List<TissueCultureSample> GetPreservedCultures() => _preservedCultures.Values.ToList();
}

// Data/Genetics/TissueCulture/TissueCultureSample.cs
[System.Serializable]
public class TissueCultureSample
{
    public string SampleId;
    public string SourcePlantId;
    public PlantGenotype Genotype;
    public string BlockchainHash;
    public DateTime CreationDate;
    public DateTime? PreservationDate;
    public DateTime? ReactivationDate;
    public CultureStatus Status;
    public float HealthPercentage;
    public float ContaminationRisk;
    public float DaysSinceCreation;

    public TissueCultureSample Clone()
    {
        return new TissueCultureSample
        {
            SampleId = Guid.NewGuid().ToString(),
            SourcePlantId = SourcePlantId,
            Genotype = Genotype.Clone(),
            BlockchainHash = BlockchainHash,
            CreationDate = DateTime.UtcNow,
            Status = CultureStatus.Initiating,
            HealthPercentage = 100f,
            ContaminationRisk = 0f,
            DaysSinceCreation = 0f
        };
    }
}

public enum CultureStatus
{
    Initiating,
    Active,
    Preserved,
    Reactivating,
    Failed
}
```

### Day 3-5: Micropropagation System

**Micropropagation (Rapid Cloning):**

```csharp
// Systems/Genetics/Micropropagation/MicropropagationSystem.cs
public class MicropropagationSystem : MonoBehaviour, IMicropropagationSystem
{
    private Dictionary<string, MicropropagationBatch> _activeBatches = new();
    private int _maxBatches = 20;

    public async Task<MicropropagationBatch> StartBatchAsync(
        TissueCultureSample sourceSample,
        int targetQuantity)
    {
        if (_activeBatches.Count >= _maxBatches)
        {
            ChimeraLogger.LogWarning("MICROPROPAGATION",
                $"Maximum batches reached ({_maxBatches})", this);
            return null;
        }

        if (targetQuantity > 100)
        {
            ChimeraLogger.LogWarning("MICROPROPAGATION",
                $"Maximum batch size is 100 clones", this);
            return null;
        }

        var batch = new MicropropagationBatch
        {
            BatchId = Guid.NewGuid().ToString(),
            SourceSampleId = sourceSample.SampleId,
            SourceGenotype = sourceSample.Genotype.Clone(),
            BlockchainHash = sourceSample.BlockchainHash,
            TargetQuantity = targetQuantity,
            CurrentStage = PropagationStage.Multiplication,
            Progress = 0f,
            StartDate = DateTime.UtcNow,
            EstimatedCompletionDays = CalculateCompletionTime(targetQuantity),
            SuccessRate = CalculateSuccessRate(sourceSample)
        };

        _activeBatches[batch.BatchId] = batch;

        ChimeraLogger.Log("MICROPROPAGATION",
            $"Started batch {batch.BatchId}: {targetQuantity} clones, est. {batch.EstimatedCompletionDays:F1} days", this);

        // Start async propagation process
        _ = ProcessBatchAsync(batch);

        return batch;
    }

    private async Task ProcessBatchAsync(MicropropagationBatch batch)
    {
        // Stage 1: Multiplication (50% of time)
        batch.CurrentStage = PropagationStage.Multiplication;
        await SimulateStage(batch, 0.5f);

        // Stage 2: Rooting (30% of time)
        batch.CurrentStage = PropagationStage.Rooting;
        await SimulateStage(batch, 0.3f);

        // Stage 3: Acclimatization (20% of time)
        batch.CurrentStage = PropagationStage.Acclimatization;
        await SimulateStage(batch, 0.2f);

        // Calculate final success
        var actualQuantity = CalculateActualYield(batch);
        batch.ActualQuantity = actualQuantity;
        batch.CurrentStage = PropagationStage.Complete;
        batch.Progress = 100f;
        batch.CompletionDate = DateTime.UtcNow;

        ChimeraLogger.Log("MICROPROPAGATION",
            $"Batch {batch.BatchId} complete: {actualQuantity}/{batch.TargetQuantity} clones ({batch.SuccessRate:F1}% success)", this);
    }

    private async Task SimulateStage(MicropropagationBatch batch, float stagePercentage)
    {
        var stageDuration = batch.EstimatedCompletionDays * stagePercentage;
        var scaledTime = CalculateScaledTime(stageDuration);

        var checkInterval = 100; // Update every 100ms
        var totalChecks = (int)(scaledTime * 1000 / checkInterval);

        for (int i = 0; i < totalChecks; i++)
        {
            await Task.Delay(checkInterval);

            batch.Progress += (stagePercentage * 100f) / totalChecks;
            batch.DaysElapsed = (DateTime.UtcNow - batch.StartDate).TotalDays;
        }
    }

    private int CalculateActualYield(MicropropagationBatch batch)
    {
        // Apply success rate with some randomness
        var baseYield = batch.TargetQuantity * (batch.SuccessRate / 100f);
        var variance = UnityEngine.Random.Range(-0.1f, 0.1f);
        var actualYield = baseYield * (1f + variance);

        return Mathf.RoundToInt(Mathf.Clamp(actualYield, 1, batch.TargetQuantity));
    }

    public List<PlantInstance> HarvestBatch(string batchId)
    {
        if (!_activeBatches.TryGetValue(batchId, out var batch))
        {
            ChimeraLogger.LogWarning("MICROPROPAGATION",
                $"Batch {batchId} not found", this);
            return null;
        }

        if (batch.CurrentStage != PropagationStage.Complete)
        {
            ChimeraLogger.LogWarning("MICROPROPAGATION",
                $"Batch {batchId} not ready for harvest. Current stage: {batch.CurrentStage}", this);
            return null;
        }

        // Generate plant instances from batch
        var plantInstances = new List<PlantInstance>();

        for (int i = 0; i < batch.ActualQuantity; i++)
        {
            var clone = new PlantInstance
            {
                PlantId = Guid.NewGuid().ToString(),
                Genotype = batch.SourceGenotype.Clone(),
                CurrentStage = GrowthStage.Seedling,
                Age = 0f,
                Health = 100f,
                IsClone = true,
                SourceBatchId = batchId
            };

            plantInstances.Add(clone);
        }

        _activeBatches.Remove(batchId);

        ChimeraLogger.Log("MICROPROPAGATION",
            $"Harvested {plantInstances.Count} clones from batch {batchId}", this);

        return plantInstances;
    }

    public void UpdateBatches(float deltaTime)
    {
        // Batches update automatically via async processing
        // This method monitors for failures

        foreach (var batch in _activeBatches.Values.ToList())
        {
            // Check for environmental failures
            var failureRisk = CalculateFailureRisk(batch);

            if (UnityEngine.Random.value < failureRisk * deltaTime)
            {
                batch.CurrentStage = PropagationStage.Failed;
                batch.Progress = 0f;

                ChimeraLogger.LogWarning("MICROPROPAGATION",
                    $"Batch {batch.BatchId} failed due to environmental conditions", this);
            }
        }
    }

    private float CalculateCompletionTime(int quantity)
    {
        // Base time: 21 days for small batch
        // Scales with quantity (larger batches take longer)
        var baseDays = 21f;
        var scaleFactor = Mathf.Log10(quantity + 1) / 2f;
        return baseDays * (1f + scaleFactor);
    }

    private float CalculateSuccessRate(TissueCultureSample source)
    {
        // Base success rate: 85%
        // Modified by source health and contamination
        var baseRate = 85f;
        var healthModifier = source.HealthPercentage / 100f;
        var contaminationPenalty = source.ContaminationRisk * 0.3f;

        return Mathf.Clamp(baseRate * healthModifier - contaminationPenalty, 30f, 98f);
    }

    private float CalculateFailureRisk(MicropropagationBatch batch)
    {
        // Risk increases with time and poor environmental control
        // Base risk: 0.1% per day
        return 0.001f;
    }

    private float CalculateScaledTime(float gameDays)
    {
        var timeManager = ServiceContainer.Resolve<ITimeManager>();
        return gameDays * timeManager.GetSecondsPerGameDay();
    }

    public List<MicropropagationBatch> GetActiveBatches() => _activeBatches.Values.ToList();
}

// Data/Genetics/Micropropagation/MicropropagationBatch.cs
[System.Serializable]
public class MicropropagationBatch
{
    public string BatchId;
    public string SourceSampleId;
    public PlantGenotype SourceGenotype;
    public string BlockchainHash;
    public int TargetQuantity;
    public int ActualQuantity;
    public PropagationStage CurrentStage;
    public float Progress; // 0-100%
    public DateTime StartDate;
    public DateTime? CompletionDate;
    public float EstimatedCompletionDays;
    public float DaysElapsed;
    public float SuccessRate; // % of target that will succeed
}

public enum PropagationStage
{
    Multiplication,
    Rooting,
    Acclimatization,
    Complete,
    Failed
}
```

---

## WEEK 8-9: CULTIVATION PILLAR COMPLETION

### Day 1-2: Active IPM System Implementation

**Current State:** Data structures only (IPMSystemSO.cs exists, no active simulation)
**Goal:** Implement pest lifecycle, spread, and integrated pest management

**Active Pest Simulation:**

```csharp
// Systems/Cultivation/IPM/ActiveIPMSystem.cs
public class ActiveIPMSystem : MonoBehaviour, ITickable, IActiveIPMSystem
{
    public int TickPriority => 60;
    public bool IsTickable => enabled;

    private Dictionary<string, PestInfestation> _activeInfestations = new();
    private List<BeneficialOrganismColony> _beneficialColonies = new();
    private float _monitoringInterval = 3600f; // Check every hour (game time)
    private float _timeSinceLastMonitoring = 0f;

    private ICultivationManager _cultivationManager;

    public void Tick(float deltaTime)
    {
        UpdateInfestations(deltaTime);
        UpdateBeneficialOrganisms(deltaTime);
        CheckForNewInfestations(deltaTime);
        ProcessMonitoring(deltaTime);
    }

    private void UpdateInfestations(float deltaTime)
    {
        foreach (var infestation in _activeInfestations.Values.ToList())
        {
            // Update infestation lifecycle
            infestation.DaysActive += deltaTime / (24f * 60f * 60f);
            infestation.Population = CalculatePopulationGrowth(infestation, deltaTime);

            // Spread to nearby plants
            if (infestation.Population > infestation.SpreadThreshold)
            {
                SpreadInfestation(infestation, deltaTime);
            }

            // Damage affected plants
            DamagePlants(infestation, deltaTime);

            // Check for natural die-off
            if (infestation.Population <= 0f)
            {
                ChimeraLogger.Log("IPM",
                    $"Infestation {infestation.InfestationId} eliminated naturally", this);
                _activeInfestations.Remove(infestation.InfestationId);
            }
        }
    }

    private float CalculatePopulationGrowth(PestInfestation infestation, float deltaTime)
    {
        var pest = infestation.PestType;
        var environment = GetEnvironmentForPlant(infestation.AffectedPlantId);

        // Growth rate based on environmental conditions
        var tempFactor = CalculateTemperatureFactor(environment.Temperature, pest.OptimalTemperature);
        var humidityFactor = CalculateHumidityFactor(environment.Humidity, pest.OptimalHumidity);

        var growthRate = pest.ReproductionRate * tempFactor * humidityFactor;

        // Logistic growth model (population caps at carrying capacity)
        var carryingCapacity = 10000f;
        var growth = growthRate * infestation.Population * (1f - infestation.Population / carryingCapacity);

        // Subtract mortality
        growth -= pest.MortalityRate * infestation.Population;

        // Apply beneficial organism predation
        var predationLoss = CalculatePredation(infestation);
        growth -= predationLoss;

        // Apply treatment effects
        if (infestation.ActiveTreatment != null)
        {
            var treatmentLoss = infestation.ActiveTreatment.Efficacy * infestation.Population;
            growth -= treatmentLoss;
        }

        var daysElapsed = deltaTime / (24f * 60f * 60f);
        return Mathf.Max(0f, infestation.Population + growth * daysElapsed);
    }

    private void SpreadInfestation(PestInfestation infestation, float deltaTime)
    {
        // Get nearby plants within spread radius
        var sourcePosition = GetPlantPosition(infestation.AffectedPlantId);
        var nearbyPlants = _cultivationManager.GetPlantsInRadius(sourcePosition, infestation.PestType.SpreadRadius);

        foreach (var plant in nearbyPlants)
        {
            if (plant.PlantId == infestation.AffectedPlantId)
                continue;

            // Spread probability based on distance and population
            var distance = Vector3.Distance(sourcePosition, plant.Position);
            var spreadChance = (infestation.Population / 10000f) * (1f - distance / infestation.PestType.SpreadRadius);

            if (UnityEngine.Random.value < spreadChance * deltaTime)
            {
                CreateInfestation(plant.PlantId, infestation.PestType, infestation.Population * 0.1f);

                ChimeraLogger.LogWarning("IPM",
                    $"Pest {infestation.PestType.Name} spread from plant {infestation.AffectedPlantId} to {plant.PlantId}", this);
            }
        }
    }

    private void DamagePlants(PestInfestation infestation, float deltaTime)
    {
        var plant = _cultivationManager.GetPlant(infestation.AffectedPlantId);
        if (plant == null) return;

        // Calculate damage based on pest type and population
        var damagePerDay = infestation.PestType.DamageRate * (infestation.Population / 1000f);
        var daysElapsed = deltaTime / (24f * 60f * 60f);
        var damage = damagePerDay * daysElapsed;

        plant.Health -= damage;

        // Reduce yield potential
        plant.YieldPotentialModifier *= (1f - damage * 0.01f);

        if (plant.Health <= 0f)
        {
            ChimeraLogger.LogWarning("IPM",
                $"Plant {plant.PlantId} killed by {infestation.PestType.Name} infestation", this);
        }
    }

    private float CalculatePredation(PestInfestation infestation)
    {
        var totalPredation = 0f;

        foreach (var colony in _beneficialColonies)
        {
            if (colony.TargetPests.Contains(infestation.PestType.PestId))
            {
                var predationRate = colony.OrganismType.PredationRate * colony.Population;
                totalPredation += predationRate;
            }
        }

        return totalPredation;
    }

    private void UpdateBeneficialOrganisms(float deltaTime)
    {
        foreach (var colony in _beneficialColonies.ToList())
        {
            // Update population based on prey availability
            var preyAvailable = _activeInfestations.Values
                .Where(i => colony.TargetPests.Contains(i.PestType.PestId))
                .Sum(i => i.Population);

            if (preyAvailable > 0f)
            {
                // Population grows with available prey
                var growthRate = colony.OrganismType.ReproductionRate * Mathf.Min(1f, preyAvailable / 1000f);
                var daysElapsed = deltaTime / (24f * 60f * 60f);
                colony.Population += growthRate * colony.Population * daysElapsed;
            }
            else
            {
                // Population declines without prey
                var declineRate = 0.1f; // 10% per day
                var daysElapsed = deltaTime / (24f * 60f * 60f);
                colony.Population -= declineRate * colony.Population * daysElapsed;
            }

            colony.Population = Mathf.Clamp(colony.Population, 0f, colony.OrganismType.MaxPopulation);

            if (colony.Population <= 0f)
            {
                ChimeraLogger.Log("IPM",
                    $"Beneficial organism colony {colony.ColonyId} died out", this);
                _beneficialColonies.Remove(colony);
            }
        }
    }

    private void CheckForNewInfestations(float deltaTime)
    {
        // Random pest introduction based on environmental conditions and prevention measures
        var infestationRisk = CalculateBaseInfestationRisk();
        var preventionModifier = CalculatePreventionModifier();

        var finalRisk = infestationRisk * preventionModifier * deltaTime;

        if (UnityEngine.Random.value < finalRisk)
        {
            // Select random pest type
            var pestLibrary = ServiceContainer.Resolve<IPestLibrary>();
            var randomPest = pestLibrary.GetRandomPest();

            // Select random plant
            var allPlants = _cultivationManager.GetAllPlants();
            if (allPlants.Count > 0)
            {
                var randomPlant = allPlants[UnityEngine.Random.Range(0, allPlants.Count)];
                CreateInfestation(randomPlant.PlantId, randomPest, 10f); // Start with small population

                ChimeraLogger.LogWarning("IPM",
                    $"New {randomPest.Name} infestation detected on plant {randomPlant.PlantId}", this);
            }
        }
    }

    private void ProcessMonitoring(float deltaTime)
    {
        _timeSinceLastMonitoring += deltaTime;

        if (_timeSinceLastMonitoring >= _monitoringInterval)
        {
            _timeSinceLastMonitoring = 0f;

            // Early detection through monitoring
            foreach (var infestation in _activeInfestations.Values)
            {
                if (!infestation.Detected && infestation.Population >= 100f)
                {
                    infestation.Detected = true;
                    infestation.DetectionDate = DateTime.UtcNow;

                    ChimeraLogger.LogWarning("IPM",
                        $"Monitoring detected {infestation.PestType.Name} on plant {infestation.AffectedPlantId}", this);

                    // Trigger alert event
                    var pestAlert = ScriptableObject.CreateInstance<PestDetectionEventSO>();
                    pestAlert.Raise(infestation);
                }
            }
        }
    }

    public void CreateInfestation(string plantId, PestData pest, float initialPopulation)
    {
        var infestation = new PestInfestation
        {
            InfestationId = Guid.NewGuid().ToString(),
            AffectedPlantId = plantId,
            PestType = pest,
            Population = initialPopulation,
            StartDate = DateTime.UtcNow,
            DaysActive = 0f,
            Detected = false,
            SpreadThreshold = 500f
        };

        _activeInfestations[infestation.InfestationId] = infestation;
    }

    public void IntroduceBeneficialOrganism(BeneficialOrganismData organism, float initialPopulation, List<string> targetPests)
    {
        var colony = new BeneficialOrganismColony
        {
            ColonyId = Guid.NewGuid().ToString(),
            OrganismType = organism,
            Population = initialPopulation,
            TargetPests = targetPests,
            IntroductionDate = DateTime.UtcNow
        };

        _beneficialColonies.Add(colony);

        ChimeraLogger.Log("IPM",
            $"Introduced {organism.Name} colony (population: {initialPopulation})", this);
    }

    public void ApplyTreatment(string infestationId, TreatmentData treatment)
    {
        if (_activeInfestations.TryGetValue(infestationId, out var infestation))
        {
            infestation.ActiveTreatment = treatment;
            infestation.TreatmentApplicationDate = DateTime.UtcNow;

            ChimeraLogger.Log("IPM",
                $"Applied {treatment.Name} to infestation {infestationId} (efficacy: {treatment.Efficacy:P0})", this);
        }
    }

    private float CalculateBaseInfestationRisk()
    {
        // Base risk: 0.1% per hour
        return 0.001f / 3600f;
    }

    private float CalculatePreventionModifier()
    {
        // TODO: Integrate with facility sanitation, air filtration, etc.
        return 1.0f; // No prevention = 100% risk
    }

    private EnvironmentData GetEnvironmentForPlant(string plantId)
    {
        var plant = _cultivationManager.GetPlant(plantId);
        return _cultivationManager.GetEnvironmentAtPosition(plant.Position);
    }

    private Vector3 GetPlantPosition(string plantId)
    {
        var plant = _cultivationManager.GetPlant(plantId);
        return plant?.Position ?? Vector3.zero;
    }

    public List<PestInfestation> GetActiveInfestations() => _activeInfestations.Values.ToList();
    public List<BeneficialOrganismColony> GetBeneficialColonies() => _beneficialColonies.ToList();
}

// Data/Cultivation/IPM/ActiveIPMData.cs
[System.Serializable]
public class PestInfestation
{
    public string InfestationId;
    public string AffectedPlantId;
    public PestData PestType;
    public float Population;
    public DateTime StartDate;
    public DateTime? DetectionDate;
    public DateTime? TreatmentApplicationDate;
    public float DaysActive;
    public bool Detected;
    public float SpreadThreshold;
    public TreatmentData ActiveTreatment;
}

[System.Serializable]
public class BeneficialOrganismColony
{
    public string ColonyId;
    public BeneficialOrganismData OrganismType;
    public float Population;
    public List<string> TargetPests;
    public DateTime IntroductionDate;
}

[System.Serializable]
public class PestData
{
    public string PestId;
    public string Name;
    public float ReproductionRate; // Population multiplier per day
    public float MortalityRate; // Natural death rate per day
    public float DamageRate; // Health damage per 1000 population per day
    public float SpreadRadius; // Units
    public float OptimalTemperature; // °C
    public float OptimalHumidity; // %
}

[System.Serializable]
public class TreatmentData
{
    public string TreatmentId;
    public string Name;
    public float Efficacy; // 0-1, population reduction per day
    public int DurationDays;
    public bool IsOrganic;
}
```

### Day 3-5: Plant Work Tasks (Pruning, Training, Defoliation)

**Interactive Plant Care Mechanics:**

```csharp
// Systems/Cultivation/PlantWork/PlantWorkSystem.cs
public class PlantWorkSystem : MonoBehaviour, IPlantWorkSystem
{
    private ICultivationManager _cultivationManager;
    private Dictionary<string, List<PlantWorkRecord>> _workHistory = new();

    public PlantWorkResult PerformPruning(string plantId, PruningType pruningType)
    {
        var plant = _cultivationManager.GetPlant(plantId);
        if (plant == null)
            return PlantWorkResult.Failed("Plant not found");

        if (plant.CurrentStage < GrowthStage.Vegetative)
            return PlantWorkResult.Failed("Plant too young for pruning");

        var result = new PlantWorkResult();

        switch (pruningType)
        {
            case PruningType.Topping:
                result = PerformTopping(plant);
                break;

            case PruningType.FIMming:
                result = PerformFIMming(plant);
                break;

            case PruningType.Lollipopping:
                result = PerformLollipopping(plant);
                break;

            case PruningType.Defoliation:
                result = PerformDefoliation(plant);
                break;
        }

        if (result.Success)
        {
            RecordWork(plantId, new PlantWorkRecord
            {
                WorkType = PlantWorkType.Pruning,
                PruningType = pruningType,
                Timestamp = DateTime.UtcNow,
                Result = result
            });
        }

        return result;
    }

    private PlantWorkResult PerformTopping(PlantInstance plant)
    {
        // Remove apical meristem to encourage lateral growth
        plant.TopNodes += 2; // Creates 2 main colas
        plant.YieldPotentialModifier *= 1.15f; // +15% yield potential
        plant.Height *= 0.8f; // Reduces height
        plant.BranchCount += 2;

        // Temporary stress
        plant.Health -= 5f;
        plant.StressLevel += 10f;

        ChimeraLogger.Log("PLANT_WORK",
            $"Topped plant {plant.PlantId}. New colas: {plant.TopNodes}, Height: {plant.Height:F2}cm", this);

        return PlantWorkResult.Success(
            $"Successfully topped plant. +15% yield potential, -20% height, +2 colas");
    }

    private PlantWorkResult PerformFIMming(PlantInstance plant)
    {
        // "F**k I Missed" - partial apical meristem removal
        var newColas = UnityEngine.Random.Range(3, 5);
        plant.TopNodes += newColas;
        plant.YieldPotentialModifier *= 1.12f; // +12% yield potential
        plant.Height *= 0.85f; // Less height reduction than topping
        plant.BranchCount += newColas;

        plant.Health -= 3f;
        plant.StressLevel += 7f;

        ChimeraLogger.Log("PLANT_WORK",
            $"FIMmed plant {plant.PlantId}. New colas: {newColas}", this);

        return PlantWorkResult.Success(
            $"Successfully FIMmed plant. +{newColas} colas, +12% yield potential");
    }

    private PlantWorkResult PerformLollipopping(PlantInstance plant)
    {
        if (plant.CurrentStage != GrowthStage.Flowering)
            return PlantWorkResult.Failed("Lollipopping best performed in early flowering");

        // Remove lower growth to focus energy on top buds
        var lowerBranchesRemoved = Mathf.FloorToInt(plant.BranchCount * 0.3f);
        plant.BranchCount -= lowerBranchesRemoved;
        plant.YieldPotentialModifier *= 1.08f; // +8% quality of remaining buds
        plant.BudQualityModifier *= 1.12f; // +12% bud quality

        // Less stress than other techniques
        plant.Health -= 2f;
        plant.StressLevel += 3f;

        ChimeraLogger.Log("PLANT_WORK",
            $"Lollipoped plant {plant.PlantId}. Removed {lowerBranchesRemoved} lower branches", this);

        return PlantWorkResult.Success(
            $"Removed {lowerBranchesRemoved} lower branches. +8% yield, +12% quality");
    }

    private PlantWorkResult PerformDefoliation(PlantInstance plant)
    {
        // Strategic leaf removal to improve light penetration
        var leavesRemoved = Mathf.FloorToInt(plant.LeafCount * 0.2f); // Remove 20%
        plant.LeafCount -= leavesRemoved;
        plant.LightPenetration += 15f; // +15% light penetration
        plant.YieldPotentialModifier *= 1.05f; // +5% yield

        // Temporary photosynthesis reduction
        plant.PhotosynthesisEfficiency -= 10f;
        plant.Health -= 5f;
        plant.StressLevel += 8f;

        ChimeraLogger.Log("PLANT_WORK",
            $"Defoliated plant {plant.PlantId}. Removed {leavesRemoved} leaves", this);

        return PlantWorkResult.Success(
            $"Removed {leavesRemoved} leaves. +15% light penetration, +5% yield");
    }

    public PlantWorkResult PerformTraining(string plantId, TrainingType trainingType)
    {
        var plant = _cultivationManager.GetPlant(plantId);
        if (plant == null)
            return PlantWorkResult.Failed("Plant not found");

        if (plant.CurrentStage < GrowthStage.Vegetative)
            return PlantWorkResult.Failed("Plant too young for training");

        var result = new PlantWorkResult();

        switch (trainingType)
        {
            case TrainingType.LST: // Low Stress Training
                result = PerformLST(plant);
                break;

            case TrainingType.HST: // High Stress Training
                result = PerformHST(plant);
                break;

            case TrainingType.ScrOG: // Screen of Green
                result = PerformScrOG(plant);
                break;

            case TrainingType.Supercropping:
                result = PerformSupercropping(plant);
                break;
        }

        if (result.Success)
        {
            RecordWork(plantId, new PlantWorkRecord
            {
                WorkType = PlantWorkType.Training,
                TrainingType = trainingType,
                Timestamp = DateTime.UtcNow,
                Result = result
            });
        }

        return result;
    }

    private PlantWorkResult PerformLST(PlantInstance plant)
    {
        // Bend branches to create even canopy
        plant.CanopyEvenness += 25f; // +25% canopy evenness
        plant.LightExposure += 20f; // +20% light exposure
        plant.YieldPotentialModifier *= 1.10f; // +10% yield
        plant.Height *= 0.7f; // -30% height

        // Very low stress
        plant.StressLevel += 2f;

        ChimeraLogger.Log("PLANT_WORK",
            $"Applied LST to plant {plant.PlantId}. Canopy evenness: {plant.CanopyEvenness:F1}%", this);

        return PlantWorkResult.Success(
            "LST applied. +10% yield, +20% light exposure, -30% height");
    }

    private PlantWorkResult PerformHST(PlantInstance plant)
    {
        // Aggressive bending/breaking of branches
        plant.CanopyEvenness += 30f;
        plant.LightExposure += 25f;
        plant.YieldPotentialModifier *= 1.15f; // +15% yield
        plant.Height *= 0.6f; // -40% height

        // Significant stress
        plant.Health -= 10f;
        plant.StressLevel += 15f;

        ChimeraLogger.Log("PLANT_WORK",
            $"Applied HST to plant {plant.PlantId}", this);

        return PlantWorkResult.Success(
            "HST applied. +15% yield, +25% light exposure, -40% height. High stress.");
    }

    private PlantWorkResult PerformScrOG(PlantInstance plant)
    {
        // Screen of green - weave through screen
        if (!plant.HasScrOGScreen)
        {
            plant.HasScrOGScreen = true;
            plant.ScrOGLevel = 0;
        }

        plant.ScrOGLevel++;
        plant.CanopyEvenness += 35f;
        plant.LightExposure += 30f;
        plant.YieldPotentialModifier *= 1.20f; // +20% yield
        plant.Height = Mathf.Min(plant.Height, 100f); // Max 100cm

        plant.StressLevel += 3f;

        ChimeraLogger.Log("PLANT_WORK",
            $"Applied ScrOG level {plant.ScrOGLevel} to plant {plant.PlantId}", this);

        return PlantWorkResult.Success(
            $"ScrOG applied (level {plant.ScrOGLevel}). +20% yield, extreme canopy control");
    }

    private PlantWorkResult PerformSupercropping(PlantInstance plant)
    {
        // Intentional stem damage to increase nutrient flow
        var branchesSupercropped = Mathf.Min(3, plant.BranchCount);

        plant.NutrientUptakeModifier *= 1.15f; // +15% nutrient uptake
        plant.YieldPotentialModifier *= 1.12f; // +12% yield
        plant.BudDensityModifier *= 1.10f; // +10% bud density

        // Moderate stress with recovery period
        plant.Health -= 15f;
        plant.StressLevel += 20f;
        plant.RecoveryDaysNeeded = 3f;

        ChimeraLogger.Log("PLANT_WORK",
            $"Supercropped {branchesSupercropped} branches on plant {plant.PlantId}", this);

        return PlantWorkResult.Success(
            $"Supercropped {branchesSupercropped} branches. +12% yield, +15% nutrient uptake. Needs 3 days recovery.");
    }

    private void RecordWork(string plantId, PlantWorkRecord record)
    {
        if (!_workHistory.ContainsKey(plantId))
            _workHistory[plantId] = new List<PlantWorkRecord>();

        _workHistory[plantId].Add(record);
    }

    public List<PlantWorkRecord> GetWorkHistory(string plantId)
    {
        return _workHistory.TryGetValue(plantId, out var history) ? history : new List<PlantWorkRecord>();
    }
}

// Data/Cultivation/PlantWork/PlantWorkData.cs
public enum PruningType
{
    Topping,
    FIMming,
    Lollipopping,
    Defoliation
}

public enum TrainingType
{
    LST, // Low Stress Training
    HST, // High Stress Training
    ScrOG, // Screen of Green
    Supercropping
}

public enum PlantWorkType
{
    Pruning,
    Training,
    Maintenance
}

[System.Serializable]
public class PlantWorkResult
{
    public bool Success;
    public string Message;

    public static PlantWorkResult Success(string message) => new PlantWorkResult { Success = true, Message = message };
    public static PlantWorkResult Failed(string message) => new PlantWorkResult { Success = false, Message = message };
}

[System.Serializable]
public class PlantWorkRecord
{
    public PlantWorkType WorkType;
    public PruningType? PruningType;
    public TrainingType? TrainingType;
    public DateTime Timestamp;
    public PlantWorkResult Result;
}
```

### Day 6-7: Processing Pipeline (Drying & Curing)

**Post-Harvest Processing System:**

```csharp
// Systems/Cultivation/Processing/ProcessingSystem.cs
public class ProcessingSystem : MonoBehaviour, ITickable, IProcessingSystem
{
    public int TickPriority => 50;
    public bool IsTickable => enabled;

    private Dictionary<string, DryingBatch> _dryingBatches = new();
    private Dictionary<string, CuringBatch> _curingBatches = new();

    public void Tick(float deltaTime)
    {
        UpdateDryingBatches(deltaTime);
        UpdateCuringBatches(deltaTime);
    }

    public DryingBatch StartDrying(HarvestBatch harvest, DryingMethod method)
    {
        var batch = new DryingBatch
        {
            BatchId = Guid.NewGuid().ToString(),
            HarvestId = harvest.HarvestId,
            Method = method,
            StartDate = DateTime.UtcNow,
            TargetMoisture = method == DryingMethod.HangDry ? 12f : 10f,
            CurrentMoisture = 75f, // Fresh harvest moisture
            TargetDays = CalculateDryingDays(method),
            DaysElapsed = 0f,
            WeightWet = harvest.TotalWeight,
            WeightCurrent = harvest.TotalWeight,
            QualityModifier = 1.0f
        };

        _dryingBatches[batch.BatchId] = batch;

        ChimeraLogger.Log("PROCESSING",
            $"Started drying batch {batch.BatchId} ({method}, {harvest.TotalWeight:F2}g)", this);

        return batch;
    }

    private void UpdateDryingBatches(float deltaTime)
    {
        foreach (var batch in _dryingBatches.Values.ToList())
        {
            var daysElapsed = deltaTime / (24f * 60f * 60f);
            batch.DaysElapsed += daysElapsed;

            // Calculate moisture loss based on method and environment
            var moistureLossRate = CalculateMoistureLossRate(batch.Method);
            var environmentModifier = CalculateDryingEnvironmentModifier();

            batch.CurrentMoisture -= moistureLossRate * environmentModifier * daysElapsed;
            batch.CurrentMoisture = Mathf.Max(batch.TargetMoisture, batch.CurrentMoisture);

            // Update weight (as moisture is lost)
            var moistureRatio = batch.CurrentMoisture / 75f;
            batch.WeightCurrent = batch.WeightWet * (0.25f + moistureRatio * 0.75f);

            // Quality degradation if environment is poor
            if (environmentModifier < 0.8f)
            {
                batch.QualityModifier -= 0.01f * daysElapsed;
            }

            // Check for completion
            if (batch.CurrentMoisture <= batch.TargetMoisture)
            {
                batch.CompletionDate = DateTime.UtcNow;
                batch.Status = ProcessingStatus.Complete;

                ChimeraLogger.Log("PROCESSING",
                    $"Drying complete: {batch.BatchId} ({batch.WeightCurrent:F2}g dry, {batch.QualityModifier:P0} quality)", this);
            }

            // Check for over-drying
            if (batch.CurrentMoisture < batch.TargetMoisture - 2f)
            {
                batch.QualityModifier *= 0.85f; // -15% quality for over-drying
                ChimeraLogger.LogWarning("PROCESSING",
                    $"Batch {batch.BatchId} over-dried. Quality reduced.", this);
            }
        }
    }

    public CuringBatch StartCuring(DryingBatch driedBatch, CuringMethod method, int targetDays)
    {
        if (driedBatch.Status != ProcessingStatus.Complete)
        {
            ChimeraLogger.LogWarning("PROCESSING",
                $"Cannot cure batch {driedBatch.BatchId} - drying not complete", this);
            return null;
        }

        var batch = new CuringBatch
        {
            BatchId = Guid.NewGuid().ToString(),
            DryingBatchId = driedBatch.BatchId,
            Method = method,
            StartDate = DateTime.UtcNow,
            TargetDays = targetDays,
            DaysElapsed = 0f,
            CurrentMoisture = driedBatch.CurrentMoisture,
            Weight = driedBatch.WeightCurrent,
            QualityModifier = driedBatch.QualityModifier,
            TerpenePreservation = 85f, // Start at 85% preservation
            PotencyModifier = 1.0f
        };

        _curingBatches[batch.BatchId] = batch;

        ChimeraLogger.Log("PROCESSING",
            $"Started curing batch {batch.BatchId} ({method}, target: {targetDays} days)", this);

        return batch;
    }

    private void UpdateCuringBatches(float deltaTime)
    {
        foreach (var batch in _curingBatches.Values.ToList())
        {
            var daysElapsed = deltaTime / (24f * 60f * 60f);
            batch.DaysElapsed += daysElapsed;

            // Curing improves quality over time (to a point)
            var optimalCuringDays = 30f;
            var curingProgress = batch.DaysElapsed / optimalCuringDays;

            if (curingProgress <= 1.0f)
            {
                // Quality improves during optimal curing period
                batch.QualityModifier += 0.005f * daysElapsed; // +0.5% per day
                batch.TerpenePreservation += 0.3f * daysElapsed; // Terpenes develop
                batch.PotencyModifier += 0.002f * daysElapsed; // Potency increases slightly
            }
            else
            {
                // After optimal period, quality plateaus then slowly degrades
                batch.TerpenePreservation -= 0.1f * daysElapsed;
            }

            // Burping (moisture management during cure)
            if (batch.Method == CuringMethod.JarCuring)
            {
                // Simulate burping schedule
                var needsBurping = (batch.DaysElapsed % 1f) < 0.1f && batch.DaysElapsed < 14f;
                if (needsBurping && !batch.BurpedToday)
                {
                    batch.CurrentMoisture -= 0.5f; // Release moisture
                    batch.BurpedToday = true;

                    ChimeraLogger.Log("PROCESSING",
                        $"Burped jar {batch.BatchId}. Moisture: {batch.CurrentMoisture:F1}%", this);
                }
                else if ((batch.DaysElapsed % 1f) >= 0.1f)
                {
                    batch.BurpedToday = false;
                }
            }

            // Check for completion
            if (batch.DaysElapsed >= batch.TargetDays)
            {
                batch.CompletionDate = DateTime.UtcNow;
                batch.Status = ProcessingStatus.Complete;

                // Calculate final quality
                batch.FinalQuality = CalculateFinalQuality(batch);

                ChimeraLogger.Log("PROCESSING",
                    $"Curing complete: {batch.BatchId} (Quality: {batch.FinalQuality:P0}, Terpenes: {batch.TerpenePreservation:F1}%)", this);
            }
        }
    }

    private float CalculateMoistureLossRate(DryingMethod method)
    {
        return method switch
        {
            DryingMethod.HangDry => 5f, // 5% per day
            DryingMethod.RackDry => 6f, // Slightly faster
            DryingMethod.FreezeDry => 15f, // Very fast
            _ => 5f
        };
    }

    private float CalculateDryingEnvironmentModifier()
    {
        // Ideal: 60°F, 60% RH
        // TODO: Integrate with actual environmental controller
        return 1.0f;
    }

    private float CalculateDryingDays(DryingMethod method)
    {
        return method switch
        {
            DryingMethod.HangDry => 10f,
            DryingMethod.RackDry => 8f,
            DryingMethod.FreezeDry => 3f,
            _ => 10f
        };
    }

    private float CalculateFinalQuality(CuringBatch batch)
    {
        var baseQuality = batch.QualityModifier;
        var terpeneBonus = (batch.TerpenePreservation / 100f) * 0.15f; // Up to +15%
        var potencyBonus = (batch.PotencyModifier - 1.0f);

        return Mathf.Clamp01(baseQuality + terpeneBonus + potencyBonus);
    }

    public ProcessedProduct FinalizeBatch(string curingBatchId)
    {
        if (!_curingBatches.TryGetValue(curingBatchId, out var batch))
            return null;

        if (batch.Status != ProcessingStatus.Complete)
        {
            ChimeraLogger.LogWarning("PROCESSING",
                $"Cannot finalize batch {curingBatchId} - curing not complete", this);
            return null;
        }

        var product = new ProcessedProduct
        {
            ProductId = Guid.NewGuid().ToString(),
            SourceBatchId = curingBatchId,
            Weight = batch.Weight,
            Quality = batch.FinalQuality,
            TerpeneProfile = batch.TerpenePreservation,
            Potency = batch.PotencyModifier,
            ProcessedDate = DateTime.UtcNow,
            MarketValue = CalculateMarketValue(batch)
        };

        _curingBatches.Remove(curingBatchId);

        ChimeraLogger.Log("PROCESSING",
            $"Finalized product {product.ProductId}: {product.Weight:F2}g @ ${product.MarketValue:F2} (Quality: {product.Quality:P0})", this);

        return product;
    }

    private float CalculateMarketValue(CuringBatch batch)
    {
        var basePrice = 10f; // $10/gram base
        var qualityMultiplier = batch.FinalQuality;
        var terpeneBonus = (batch.TerpenePreservation / 100f) * 1.5f;

        return batch.Weight * basePrice * qualityMultiplier * terpeneBonus;
    }

    public List<DryingBatch> GetActiveDryingBatches() => _dryingBatches.Values.ToList();
    public List<CuringBatch> GetActiveCuringBatches() => _curingBatches.Values.ToList();
}

// Data/Cultivation/Processing/ProcessingData.cs
public enum DryingMethod
{
    HangDry,
    RackDry,
    FreezeDry
}

public enum CuringMethod
{
    JarCuring,
    TurkeyBag,
    GroveBag
}

public enum ProcessingStatus
{
    InProgress,
    Complete,
    Failed
}

[System.Serializable]
public class DryingBatch
{
    public string BatchId;
    public string HarvestId;
    public DryingMethod Method;
    public DateTime StartDate;
    public DateTime? CompletionDate;
    public float TargetMoisture; // %
    public float CurrentMoisture; // %
    public float TargetDays;
    public float DaysElapsed;
    public float WeightWet; // grams
    public float WeightCurrent; // grams
    public float QualityModifier; // 0-1+
    public ProcessingStatus Status;
}

[System.Serializable]
public class CuringBatch
{
    public string BatchId;
    public string DryingBatchId;
    public CuringMethod Method;
    public DateTime StartDate;
    public DateTime? CompletionDate;
    public float TargetDays;
    public float DaysElapsed;
    public float CurrentMoisture; // %
    public float Weight; // grams
    public float QualityModifier; // 0-1+
    public float TerpenePreservation; // %
    public float PotencyModifier; // 1.0 = baseline
    public float FinalQuality;
    public bool BurpedToday;
    public ProcessingStatus Status;
}

[System.Serializable]
public class ProcessedProduct
{
    public string ProductId;
    public string SourceBatchId;
    public float Weight; // grams
    public float Quality; // 0-1
    public float TerpeneProfile; // %
    public float Potency; // multiplier
    public DateTime ProcessedDate;
    public float MarketValue; // $
}
```

---

## WEEK 9-10: CONSTRUCTION PILLAR COMPLETION

### Week 9, Day 1-3: Facility Progression System

**Current Gap:** No tier/size scaling (Storage Bay → Large Warehouse → Custom Facility)

```csharp
// Systems/Construction/FacilityProgression/FacilityProgressionManager.cs
public class FacilityProgressionManager : MonoBehaviour, IFacilityProgressionManager
{
    private FacilityTier _currentTier;
    private List<FacilityUpgradeRequirement> _upgradeRequirements;

    private IEconomyManager _economy;
    private IConstructionManager _construction;

    public void Initialize(FacilityTier startingTier)
    {
        _currentTier = startingTier;
        LoadUpgradeRequirements();

        ChimeraLogger.Log("FACILITY",
            $"Initialized facility at tier: {startingTier.TierName}", this);
    }

    private void LoadUpgradeRequirements()
    {
        // Define progression path
        _upgradeRequirements = new List<FacilityUpgradeRequirement>
        {
            new FacilityUpgradeRequirement // Storage Bay → Large Bay
            {
                FromTier = FacilityTierType.SmallStorageBay,
                ToTier = FacilityTierType.LargeWarehouseBay,
                CurrencyCost = 50000f,
                SkillPointCost = 10,
                RequiredLevel = 5,
                UnlockRequirements = new List<string> { "Complete 10 harvests", "Reach 500kg total yield" }
            },
            new FacilityUpgradeRequirement // Large Bay → Standalone Warehouse
            {
                FromTier = FacilityTierType.LargeWarehouseBay,
                ToTier = FacilityTierType.SmallStandaloneWarehouse,
                CurrencyCost = 250000f,
                SkillPointCost = 25,
                RequiredLevel = 10,
                UnlockRequirements = new List<string> { "Complete 50 harvests", "Master all cultivation skills" }
            },
            new FacilityUpgradeRequirement // Standalone → Large Standalone
            {
                FromTier = FacilityTierType.SmallStandaloneWarehouse,
                ToTier = FacilityTierType.LargeStandaloneWarehouse,
                CurrencyCost = 750000f,
                SkillPointCost = 50,
                RequiredLevel = 15,
                UnlockRequirements = new List<string> { "Complete 200 harvests", "Unlock all genetics" }
            },
            new FacilityUpgradeRequirement // Large Standalone → Custom Massive
            {
                FromTier = FacilityTierType.LargeStandaloneWarehouse,
                ToTier = FacilityTierType.MassiveCustomFacility,
                CurrencyCost = 2000000f,
                SkillPointCost = 100,
                RequiredLevel = 20,
                UnlockRequirements = new List<string> { "Complete progression leaf", "1000+ harvests" }
            }
        };
    }

    public bool CanUpgrade()
    {
        var nextUpgrade = _upgradeRequirements.FirstOrDefault(r => r.FromTier == _currentTier.Type);
        if (nextUpgrade == null)
            return false; // Max tier

        // Check requirements
        if (_economy.GetBalance() < nextUpgrade.CurrencyCost)
            return false;

        var progressionManager = ServiceContainer.Resolve<IProgressionManager>();
        if (progressionManager.GetPlayerLevel() < nextUpgrade.RequiredLevel)
            return false;

        if (progressionManager.GetSkillPoints() < nextUpgrade.SkillPointCost)
            return false;

        return true;
    }

    public async Task<bool> UpgradeFacilityAsync()
    {
        var nextUpgrade = _upgradeRequirements.FirstOrDefault(r => r.FromTier == _currentTier.Type);
        if (nextUpgrade == null || !CanUpgrade())
            return false;

        // Deduct costs
        _economy.DeductFunds(nextUpgrade.CurrencyCost);
        var progressionManager = ServiceContainer.Resolve<IProgressionManager>();
        progressionManager.SpendSkillPoints(nextUpgrade.SkillPointCost);

        // Load new facility scene
        var sceneManager = ServiceContainer.Resolve<ISceneManager>();
        await sceneManager.LoadFacilitySceneAsync(nextUpgrade.ToTier);

        // Update tier
        _currentTier = GetTierData(nextUpgrade.ToTier);

        ChimeraLogger.Log("FACILITY",
            $"Upgraded facility to {_currentTier.TierName}. New size: {_currentTier.GridSize}", this);

        return true;
    }

    private FacilityTier GetTierData(FacilityTierType tierType)
    {
        return tierType switch
        {
            FacilityTierType.SmallStorageBay => new FacilityTier
            {
                Type = tierType,
                TierName = "Small Storage Bay",
                GridSize = new Vector3Int(15, 10, 15),
                MaxPlants = 50,
                MaxEquipment = 20,
                SceneName = "SmallStorageBay"
            },
            FacilityTierType.LargeWarehouseBay => new FacilityTier
            {
                Type = tierType,
                TierName = "Large Warehouse Bay",
                GridSize = new Vector3Int(40, 20, 40),
                MaxPlants = 200,
                MaxEquipment = 80,
                SceneName = "LargeWarehouseBay"
            },
            FacilityTierType.SmallStandaloneWarehouse => new FacilityTier
            {
                Type = tierType,
                TierName = "Small Standalone Warehouse",
                GridSize = new Vector3Int(60, 25, 60),
                MaxPlants = 500,
                MaxEquipment = 200,
                SceneName = "SmallStandaloneWarehouse"
            },
            FacilityTierType.LargeStandaloneWarehouse => new FacilityTier
            {
                Type = tierType,
                TierName = "Large Standalone Warehouse",
                GridSize = new Vector3Int(100, 30, 100),
                MaxPlants = 1500,
                MaxEquipment = 600,
                SceneName = "LargeStandaloneWarehouse"
            },
            FacilityTierType.MassiveCustomFacility => new FacilityTier
            {
                Type = tierType,
                TierName = "Massive Custom Facility",
                GridSize = new Vector3Int(200, 40, 200),
                MaxPlants = 5000,
                MaxEquipment = 2000,
                SceneName = "MassiveCustomFacility"
            },
            _ => null
        };
    }

    public FacilityTier GetCurrentTier() => _currentTier;
    public List<FacilityUpgradeRequirement> GetUpgradeRequirements() => _upgradeRequirements;
}

// Data/Facilities/FacilityProgressionData.cs
public enum FacilityTierType
{
    SmallStorageBay,
    LargeWarehouseBay,
    SmallStandaloneWarehouse,
    LargeStandaloneWarehouse,
    MassiveCustomFacility
}

[System.Serializable]
public class FacilityTier
{
    public FacilityTierType Type;
    public string TierName;
    public Vector3Int GridSize;
    public int MaxPlants;
    public int MaxEquipment;
    public string SceneName;
}

[System.Serializable]
public class FacilityUpgradeRequirement
{
    public FacilityTierType FromTier;
    public FacilityTierType ToTier;
    public float CurrencyCost;
    public int SkillPointCost;
    public int RequiredLevel;
    public List<string> UnlockRequirements;
}
```

---

## SUCCESS METRICS - WEEK 8-10

**Genetics Pillar (Complete):**
- ✅ Tissue culture system operational (preservation, reactivation, maintenance)
- ✅ Micropropagation functional (batch processing, 3 stages, realistic timing)
- ✅ Both systems integrated with contextual menu

**Cultivation Pillar (Complete):**
- ✅ Active IPM: pest lifecycle, spread, beneficial organisms, treatments
- ✅ Plant work: 4 pruning types, 4 training types with realistic effects
- ✅ Processing: drying (3 methods), curing (3 methods), quality tracking

**Construction Pillar (Complete):**
- ✅ Facility progression: 5 tiers with unlock requirements
- ✅ Scene transitions between facility sizes
- ✅ Grid scaling with tier upgrades

---

*End of Part 3: Three Pillars Completion (Weeks 8-10)*
*Continue to Part 4: Advanced Systems & Integration (Weeks 11-13)*
