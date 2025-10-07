# PROJECT CHIMERA: ULTIMATE IMPLEMENTATION ROADMAP
## Part 5: Phase 2 Preparation, Testing & Validation

**Document Version:** 2.0 - Updated Based on Comprehensive Codebase Assessment
**Phase Duration:** Weeks 14-20 (7 weeks)
**Prerequisites:** All Phase 0/1 complete, three pillars at 80%+, core systems operational

---

## WEEK 14-15: COMPREHENSIVE INTEGRATION TESTING

**Goal:** Validate all systems work together seamlessly, identify integration issues, ensure performance targets

### Week 14, Day 1-2: Test Infrastructure Setup

**Comprehensive Test Framework:**

```csharp
// Tests/Integration/IntegrationTestFramework.cs
public class IntegrationTestFramework : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private bool _runOnStartup = false;
    [SerializeField] private bool _generateReport = true;
    [SerializeField] private string _reportOutputPath = "Documents/TestReports/";

    private TestResults _results;
    private IServiceContainer _container;

    private void Start()
    {
        if (_runOnStartup)
        {
            _ = RunFullTestSuiteAsync();
        }
    }

    public async Task<TestResults> RunFullTestSuiteAsync()
    {
        _results = new TestResults
        {
            StartTime = DateTime.UtcNow,
            TestSuiteName = "Phase 1 Integration Test Suite"
        };

        ChimeraLogger.Log("TEST", "=== STARTING COMPREHENSIVE INTEGRATION TEST SUITE ===", this);

        // Test Category 1: Architecture & Core Systems
        await RunArchitectureTests();

        // Test Category 2: Three Pillars Integration
        await RunPillarIntegrationTests();

        // Test Category 3: Performance & Stress Tests
        await RunPerformanceTests();

        // Test Category 4: Data Persistence & Save/Load
        await RunPersistenceTests();

        // Test Category 5: UI/UX Integration
        await RunUIIntegrationTests();

        _results.EndTime = DateTime.UtcNow;
        _results.Duration = (_results.EndTime - _results.StartTime).TotalSeconds;

        ChimeraLogger.Log("TEST",
            $"=== TEST SUITE COMPLETE: {_results.PassedTests}/{_results.TotalTests} passed in {_results.Duration:F2}s ===", this);

        if (_generateReport)
        {
            GenerateTestReport(_results);
        }

        return _results;
    }

    private async Task RunArchitectureTests()
    {
        ChimeraLogger.Log("TEST", "--- Running Architecture Tests ---", this);

        // Test 1: All services resolve without errors
        await RunTest("Service Resolution", async () =>
        {
            var requiredServices = new[]
            {
                typeof(IConstructionManager),
                typeof(ICultivationManager),
                typeof(IGeneticsService),
                typeof(IBlockchainGeneticsService),
                typeof(IGridSystem),
                typeof(IPlantGrowthSystem),
                typeof(ISaveManager),
                typeof(ITimeManager),
                typeof(IEventManager),
                typeof(IMarketplaceManager),
                typeof(IProgressionManager)
            };

            foreach (var serviceType in requiredServices)
            {
                var service = ServiceContainer.Resolve(serviceType);
                if (service == null)
                    throw new Exception($"Service {serviceType.Name} failed to resolve");
            }

            return true;
        });

        // Test 2: No FindObjectOfType violations
        await RunTest("Anti-Pattern Validation", async () =>
        {
            var violations = CountCodePattern("FindObjectOfType");
            if (violations > 0)
                throw new Exception($"Found {violations} FindObjectOfType violations");
            return true;
        });

        // Test 3: UpdateOrchestrator managing all game loops
        await RunTest("Update Orchestration", async () =>
        {
            var orchestrator = ServiceContainer.Resolve<UpdateOrchestrator>();
            var tickableCount = orchestrator.GetRegisteredTickableCount();

            if (tickableCount < 10) // Expect at least 10 major systems
                throw new Exception($"Only {tickableCount} tickables registered (expected 10+)");

            return true;
        });

        // Test 4: Quality gates enforcing
        await RunTest("Quality Gate Enforcement", async () =>
        {
            var qualityGate = new QualityGateRunner();
            var result = qualityGate.RunAllChecks();

            if (result.HasAnyViolations())
                throw new Exception($"Quality gate violations detected: {result.GetViolationSummary()}");

            return true;
        });
    }

    private async Task RunPillarIntegrationTests()
    {
        ChimeraLogger.Log("TEST", "--- Running Three Pillars Integration Tests ---", this);

        // Test 1: Construction → Cultivation Integration
        await RunTest("Construction-Cultivation Integration", async () =>
        {
            var construction = ServiceContainer.Resolve<IConstructionManager>();
            var cultivation = ServiceContainer.Resolve<ICultivationManager>();
            var grid = ServiceContainer.Resolve<IGridSystem>();

            // Place a grow table
            var tableSchematic = await LoadTestSchematic("GrowTable_4x4");
            var position = new Vector3Int(5, 0, 5);

            if (!grid.CanPlace(tableSchematic, position))
                throw new Exception("Cannot place grow table on empty grid");

            var placementSuccess = await construction.PlaceStructureAsync(tableSchematic, position);
            if (!placementSuccess)
                throw new Exception("Failed to place grow table");

            // Verify environmental zone created
            var environment = cultivation.GetEnvironmentAtPosition(position.ToVector3());
            if (environment == null)
                throw new Exception("No environmental data at table position");

            return true;
        });

        // Test 2: Cultivation → Genetics Integration
        await RunTest("Cultivation-Genetics Integration", async () =>
        {
            var cultivation = ServiceContainer.Resolve<ICultivationManager>();
            var genetics = ServiceContainer.Resolve<IGeneticsService>();
            var blockchain = ServiceContainer.Resolve<IBlockchainGeneticsService>();

            // Create test plant
            var testStrain = await genetics.GetStrainByName("TestStrain_OG");
            var plant = await cultivation.CreatePlantAsync(testStrain, Vector3.zero);

            if (plant == null)
                throw new Exception("Failed to create plant from genotype");

            // Verify blockchain verification
            if (!blockchain.VerifyStrainAuthenticity(testStrain))
                throw new Exception("Test strain failed blockchain verification");

            // Simulate growth
            for (int day = 0; day < 90; day++)
            {
                cultivation.UpdatePlant(plant.PlantId, 86400f); // 1 day
            }

            // Verify harvest readiness
            if (plant.CurrentStage != GrowthStage.Flowering)
                throw new Exception("Plant did not reach flowering after 90 days");

            return true;
        });

        // Test 3: Genetics → Blockchain Integration
        await RunTest("Genetics-Blockchain Integration", async () =>
        {
            var genetics = ServiceContainer.Resolve<IGeneticsService>();
            var blockchain = ServiceContainer.Resolve<IBlockchainGeneticsService>();

            // Load two parent strains
            var parent1 = await genetics.GetStrainByName("TestStrain_Sativa");
            var parent2 = await genetics.GetStrainByName("TestStrain_Indica");

            // Perform breeding (includes blockchain mining)
            var breedingStartTime = Time.realtimeSinceStartup;
            var offspring = await blockchain.BreedPlantsAsync(parent1, parent2, 12345ul);
            var breedingDuration = Time.realtimeSinceStartup - breedingStartTime;

            if (offspring == null)
                throw new Exception("Breeding failed to produce offspring");

            if (string.IsNullOrEmpty(offspring.BlockchainHash))
                throw new Exception("Offspring missing blockchain hash");

            if (breedingDuration > 5f)
                throw new Exception($"Breeding took {breedingDuration:F2}s (target: <5s)");

            // Verify blockchain entry
            if (!blockchain.VerifyStrainAuthenticity(offspring))
                throw new Exception("Offspring failed blockchain verification");

            return true;
        });

        // Test 4: Full Pillar Integration (Construction → Cultivation → Genetics)
        await RunTest("Full Pillar Integration", async () =>
        {
            var construction = ServiceContainer.Resolve<IConstructionManager>();
            var cultivation = ServiceContainer.Resolve<ICultivationManager>();
            var genetics = ServiceContainer.Resolve<IGeneticsService>();
            var processing = ServiceContainer.Resolve<IProcessingSystem>();

            // 1. Build facility
            var facilitySchematic = await LoadTestSchematic("SmallGrowRoom");
            await construction.PlaceStructureAsync(facilitySchematic, Vector3Int.zero);

            // 2. Plant from genetics
            var strain = await genetics.GetStrainByName("TestStrain_Hybrid");
            var plant = await cultivation.CreatePlantAsync(strain, new Vector3(1, 0, 1));

            // 3. Grow to harvest
            await cultivation.GrowPlantToStageAsync(plant.PlantId, GrowthStage.Flowering);

            // 4. Harvest
            var harvest = await cultivation.HarvestPlantAsync(plant.PlantId);
            if (harvest.TotalWeight <= 0)
                throw new Exception("Harvest produced no yield");

            // 5. Process
            var dryingBatch = processing.StartDrying(harvest, DryingMethod.HangDry);
            await WaitForDryingComplete(dryingBatch);

            var curingBatch = processing.StartCuring(dryingBatch, CuringMethod.JarCuring, 30);
            await WaitForCuringComplete(curingBatch);

            var product = processing.FinalizeBatch(curingBatch.BatchId);
            if (product == null)
                throw new Exception("Failed to finalize processed product");

            if (product.Quality < 0.5f)
                throw new Exception($"Product quality too low: {product.Quality:P0}");

            return true;
        });
    }

    private async Task RunPerformanceTests()
    {
        ChimeraLogger.Log("TEST", "--- Running Performance Tests ---", this);

        // Test 1: 1000 plants @ 60 FPS
        await RunTest("1000 Plant Performance", async () =>
        {
            var cultivation = ServiceContainer.Resolve<ICultivationManager>();
            var jobManager = ServiceContainer.Resolve<PlantSystemJobManager>();

            // Create 1000 plants
            var plants = new List<PlantInstance>();
            for (int i = 0; i < 1000; i++)
            {
                var position = new Vector3(i % 100, 0, i / 100);
                var plant = await cultivation.CreatePlantAsync(GetTestStrain(), position);
                plants.Add(plant);
            }

            // Measure update performance over 60 frames
            var frameTimes = new List<float>();

            for (int frame = 0; frame < 60; frame++)
            {
                var frameStart = Time.realtimeSinceStartup;

                jobManager.Tick(0.016f);
                await Task.Run(() => jobManager.CurrentJobHandle.Complete());

                var frameTime = Time.realtimeSinceStartup - frameStart;
                frameTimes.Add(frameTime);
            }

            var averageFrameTime = frameTimes.Average();
            var maxFrameTime = frameTimes.Max();

            if (averageFrameTime > 0.016f)
                throw new Exception($"Average frame time {averageFrameTime * 1000f:F2}ms exceeds 16.67ms (60 FPS)");

            if (maxFrameTime > 0.033f)
                throw new Exception($"Max frame time {maxFrameTime * 1000f:F2}ms exceeds 33ms");

            ChimeraLogger.Log("TEST",
                $"1000 plants: Avg {averageFrameTime * 1000f:F2}ms, Max {maxFrameTime * 1000f:F2}ms", this);

            return true;
        });

        // Test 2: Memory leak detection
        await RunTest("Memory Leak Detection", async () =>
        {
            var initialMemory = GC.GetTotalMemory(true);

            // Simulate 30 minutes of gameplay (condensed to 30 seconds)
            for (int i = 0; i < 1800; i++) // 30 minutes at 60fps
            {
                UpdateOrchestrator.Instance.Tick(0.016f);

                if (i % 300 == 0) // Every 5 seconds
                {
                    GC.Collect();
                    var currentMemory = GC.GetTotalMemory(true);
                    var memoryIncrease = currentMemory - initialMemory;

                    if (memoryIncrease > 100 * 1024 * 1024) // 100MB threshold
                        throw new Exception($"Memory leak detected: {memoryIncrease / 1024 / 1024}MB increase");
                }

                await Task.Yield();
            }

            return true;
        });

        // Test 3: Blockchain mining performance
        await RunTest("Blockchain Mining Performance", async () =>
        {
            var blockchain = ServiceContainer.Resolve<IBlockchainGeneticsService>();
            var parent1 = GetTestStrain();
            var parent2 = GetTestStrain();

            var miningTimes = new List<float>();

            for (int i = 0; i < 10; i++)
            {
                var startTime = Time.realtimeSinceStartup;
                var offspring = await blockchain.BreedPlantsAsync(parent1, parent2, (ulong)i);
                var miningTime = Time.realtimeSinceStartup - startTime;

                miningTimes.Add(miningTime);
            }

            var avgMiningTime = miningTimes.Average();

            if (avgMiningTime > 1.0f)
                throw new Exception($"Blockchain mining avg {avgMiningTime:F2}s exceeds 1s target (GPU)");

            ChimeraLogger.Log("TEST",
                $"Blockchain mining: Avg {avgMiningTime:F3}s, Max {miningTimes.Max():F3}s", this);

            return true;
        });
    }

    private async Task RunPersistenceTests()
    {
        ChimeraLogger.Log("TEST", "--- Running Data Persistence Tests ---", this);

        // Test 1: Save/Load full game state
        await RunTest("Save/Load Game State", async () =>
        {
            var saveManager = ServiceContainer.Resolve<ISaveManager>();

            // Create complex game state
            await CreateComplexGameState();

            // Save
            var saveSuccess = await saveManager.SaveGameAsync("IntegrationTest");
            if (!saveSuccess)
                throw new Exception("Failed to save game");

            // Get current state snapshot
            var preLoadState = CaptureGameState();

            // Clear current state
            ClearGameState();

            // Load
            var loadSuccess = await saveManager.LoadGameAsync("IntegrationTest");
            if (!loadSuccess)
                throw new Exception("Failed to load game");

            // Verify state matches
            var postLoadState = CaptureGameState();

            if (!CompareGameStates(preLoadState, postLoadState))
                throw new Exception("Loaded state does not match saved state");

            return true;
        });

        // Test 2: Offline progression calculation
        await RunTest("Offline Progression", async () =>
        {
            var saveManager = ServiceContainer.Resolve<ISaveManager>();
            var cultivation = ServiceContainer.Resolve<ICultivationManager>();

            // Create plants
            var plant1 = await cultivation.CreatePlantAsync(GetTestStrain(), Vector3.zero);
            var plant2 = await cultivation.CreatePlantAsync(GetTestStrain(), Vector3.one);

            var initialAge1 = plant1.Age;
            var initialAge2 = plant2.Age;

            // Save with current timestamp
            await saveManager.SaveGameAsync("OfflineTest");

            // Simulate 7 days offline (modify save file timestamp)
            var saveData = await saveManager.LoadSaveDataAsync("OfflineTest");
            saveData.LastPlayedTimestamp = DateTime.UtcNow.AddDays(-7);
            await saveManager.SaveGameDataAsync("OfflineTest", saveData);

            // Load with offline progression
            await saveManager.LoadGameAsync("OfflineTest");

            // Verify plants aged
            var loadedPlant1 = cultivation.GetPlant(plant1.PlantId);
            var loadedPlant2 = cultivation.GetPlant(plant2.PlantId);

            if (loadedPlant1.Age <= initialAge1)
                throw new Exception("Plant 1 did not age during offline progression");

            if (loadedPlant2.Age <= initialAge2)
                throw new Exception("Plant 2 did not age during offline progression");

            ChimeraLogger.Log("TEST",
                $"Offline progression: Plants aged from {initialAge1:F1}/{initialAge2:F1} to {loadedPlant1.Age:F1}/{loadedPlant2.Age:F1} days", this);

            return true;
        });
    }

    private async Task RunUIIntegrationTests()
    {
        ChimeraLogger.Log("TEST", "--- Running UI Integration Tests ---", this);

        // Test 1: Mode switching updates menu
        await RunTest("Mode Switching Integration", async () =>
        {
            var modeController = ServiceContainer.Resolve<IGameplayModeController>();
            var menuController = GameObject.FindObjectOfType<ContextualMenuController>();

            // Switch to each mode and verify menu updates
            var modes = new[] { GameplayMode.Construction, GameplayMode.Cultivation, GameplayMode.Genetics };

            foreach (var mode in modes)
            {
                modeController.SetMode(mode);
                await Task.Delay(100); // Allow UI to update

                var currentMenu = menuController.GetCurrentMenu();
                if (currentMenu.Mode != mode)
                    throw new Exception($"Menu did not update to {mode} mode");
            }

            return true;
        });

        // Test 2: Contextual menu item selection triggers construction
        await RunTest("Menu-Construction Integration", async () =>
        {
            var modeController = ServiceContainer.Resolve<IGameplayModeController>();
            var construction = ServiceContainer.Resolve<IConstructionManager>();

            modeController.SetMode(GameplayMode.Construction);

            // Simulate selecting a wall from menu
            var wallSchematic = await LoadTestSchematic("BasicWall");
            construction.SelectItemForPlacement(wallSchematic);

            if (construction.GetSelectedItem() != wallSchematic)
                throw new Exception("Menu selection did not update construction manager");

            return true;
        });

        // Test 3: Camera integration with hierarchical levels
        await RunTest("Camera Hierarchy Integration", async () =>
        {
            var cameraController = ServiceContainer.Resolve<ICameraController>();

            // Test all 4 levels
            var levels = new[]
            {
                CameraLevel.Facility,
                CameraLevel.Room,
                CameraLevel.Bench,
                CameraLevel.Plant
            };

            foreach (var level in levels)
            {
                cameraController.TransitionToLevel(level);
                await Task.Delay(200); // Allow transition

                if (cameraController.GetCurrentLevel() != level)
                    throw new Exception($"Camera did not transition to {level} level");
            }

            return true;
        });
    }

    private async Task<bool> RunTest(string testName, Func<Task<bool>> testFunc)
    {
        _results.TotalTests++;

        try
        {
            var testStart = Time.realtimeSinceStartup;
            var success = await testFunc();
            var testDuration = Time.realtimeSinceStartup - testStart;

            if (success)
            {
                _results.PassedTests++;
                ChimeraLogger.Log("TEST", $"✅ {testName} - PASSED ({testDuration:F3}s)", this);
            }
            else
            {
                _results.FailedTests++;
                _results.Failures.Add(new TestFailure
                {
                    TestName = testName,
                    ErrorMessage = "Test returned false"
                });
                ChimeraLogger.LogError("TEST", $"❌ {testName} - FAILED", this);
            }

            return success;
        }
        catch (Exception ex)
        {
            _results.FailedTests++;
            _results.Failures.Add(new TestFailure
            {
                TestName = testName,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            });
            ChimeraLogger.LogError("TEST", $"❌ {testName} - FAILED: {ex.Message}", this);
            return false;
        }
    }

    private void GenerateTestReport(TestResults results)
    {
        var report = new StringBuilder();
        report.AppendLine("# PROJECT CHIMERA INTEGRATION TEST REPORT");
        report.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine();
        report.AppendLine("## SUMMARY");
        report.AppendLine($"- **Total Tests**: {results.TotalTests}");
        report.AppendLine($"- **Passed**: {results.PassedTests}");
        report.AppendLine($"- **Failed**: {results.FailedTests}");
        report.AppendLine($"- **Success Rate**: {(results.PassedTests / (float)results.TotalTests * 100f):F1}%");
        report.AppendLine($"- **Duration**: {results.Duration:F2}s");
        report.AppendLine();

        if (results.FailedTests > 0)
        {
            report.AppendLine("## FAILURES");
            foreach (var failure in results.Failures)
            {
                report.AppendLine($"### {failure.TestName}");
                report.AppendLine($"**Error**: {failure.ErrorMessage}");
                if (!string.IsNullOrEmpty(failure.StackTrace))
                {
                    report.AppendLine("```");
                    report.AppendLine(failure.StackTrace);
                    report.AppendLine("```");
                }
                report.AppendLine();
            }
        }

        var reportPath = Path.Combine(Application.dataPath, "..", _reportOutputPath, $"IntegrationTest_{DateTime.UtcNow:yyyyMMdd_HHmmss}.md");
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
        File.WriteAllText(reportPath, report.ToString());

        ChimeraLogger.Log("TEST", $"Test report saved: {reportPath}", this);
    }

    // Helper methods
    private int CountCodePattern(string pattern) { /* Implementation */ return 0; }
    private async Task<SchematicSO> LoadTestSchematic(string name) { /* Implementation */ return null; }
    private PlantGenotype GetTestStrain() { /* Implementation */ return null; }
    private async Task CreateComplexGameState() { /* Implementation */ }
    private GameStateSnapshot CaptureGameState() { /* Implementation */ return null; }
    private void ClearGameState() { /* Implementation */ }
    private bool CompareGameStates(GameStateSnapshot a, GameStateSnapshot b) { /* Implementation */ return true; }
    private async Task WaitForDryingComplete(DryingBatch batch) { /* Implementation */ }
    private async Task WaitForCuringComplete(CuringBatch batch) { /* Implementation */ }
}

// Tests/Integration/TestResults.cs
public class TestResults
{
    public string TestSuiteName;
    public DateTime StartTime;
    public DateTime EndTime;
    public double Duration;
    public int TotalTests;
    public int PassedTests;
    public int FailedTests;
    public List<TestFailure> Failures = new();
}

public class TestFailure
{
    public string TestName;
    public string ErrorMessage;
    public string StackTrace;
}

public class GameStateSnapshot
{
    public int PlantCount;
    public int ConstructionItemCount;
    public int GeneticsCount;
    public float PlayerCurrency;
    public int SkillPoints;
    // ... additional state
}
```

### Week 14, Day 3-5 & Week 15: End-to-End Integration Testing

**Manual Test Scenarios:**

```markdown
# Manual Integration Test Scenarios

## Scenario 1: New Player Experience (Tutorial → First Harvest)
**Duration**: ~2 hours
**Steps**:
1. Complete tutorial in massive custom facility
2. Start new game in 15x15 storage bay
3. Place first grow table using Construction mode
4. Connect electricity and water utilities
5. Plant first seeds from starting genetics
6. Switch to Cultivation mode and monitor growth
7. Adjust environmental controls (temperature, humidity)
8. Perform first plant work (topping)
9. Monitor for pests (IPM system)
10. Harvest first plant
11. Dry and cure product
12. Sell product for currency
13. Unlock first skill node with Skill Points
14. Verify all systems integrated correctly

**Success Criteria**:
- No crashes or errors
- All mode transitions smooth
- Environmental controls affect plant growth
- Harvest produces expected yield
- Economy updates correctly
- Skill tree unlocks properly

## Scenario 2: Advanced Genetics Workflow
**Duration**: ~1 hour
**Steps**:
1. Select two parent strains from seed bank
2. Initiate breeding (blockchain mining occurs)
3. Verify blockchain hash on offspring
4. Create tissue culture from offspring
5. Wait for culture maturation
6. Preserve culture in cryogenic storage
7. Start micropropagation batch (50 clones)
8. Monitor batch progress through 3 stages
9. Harvest clones when complete
10. Plant clones in facility
11. Verify all clones have identical genetics
12. List strain on marketplace for Skill Points
13. Purchase another strain from marketplace
14. Verify blockchain authenticity of purchased strain

**Success Criteria**:
- Breeding completes in <5 seconds
- Blockchain verification works
- Tissue culture lifecycle realistic
- Micropropagation produces correct quantity
- Marketplace transactions execute properly
- Genetic authenticity maintained

## Scenario 3: Facility Progression
**Duration**: ~3 hours
**Steps**:
1. Start in small storage bay (15x15)
2. Complete 10 harvests
3. Accumulate required currency and Skill Points
4. Upgrade to large warehouse bay (40x40)
5. Build multi-room facility with specialization
6. Implement utility systems (electrical grid, plumbing)
7. Create HVAC zones for different growth stages
8. Use schematics to replicate room designs
9. Save schematic to marketplace
10. Progress to small standalone warehouse
11. Test construction at maximum scale
12. Verify all construction systems scale properly

**Success Criteria**:
- Facility upgrades load correct scenes
- Grid system scales without performance loss
- Utilities connect across larger distances
- Schematics work at all facility sizes
- No placement glitches at higher tiers

## Scenario 4: Time Scale & Offline Progression
**Duration**: ~1 hour active, 24 hours passive
**Steps**:
1. Plant 20 plants at Real-Time scale
2. Switch to 1x Baseline (1 week = 1 hour)
3. Verify plants grow 144x faster
4. Note genetic potential at baseline (95%)
5. Switch to 8x speed (60 days = 1 hour)
6. Verify genetic potential reduces to 75%
7. Test lock-in period (5 minute wait)
8. Attempt scale change during lock-in (should fail)
9. Save game with active plants
10. Close game for 24 hours
11. Reopen and observe offline progression summary
12. Verify plants aged appropriately
13. Check for any deaths due to neglect

**Success Criteria**:
- Time transitions smooth with 5-second inertia
- Genetic potential modifier applies correctly
- Lock-in prevents rapid switching
- Offline progression calculates accurately
- No data corruption from extended offline period
```

---

## WEEK 16-17: PERFORMANCE OPTIMIZATION & STRESS TESTING

### Week 16, Day 1-3: Performance Profiling

**Unity Profiler Deep Dive:**

```csharp
// Systems/Diagnostics/PerformanceProfiler.cs
public class PerformanceProfiler : MonoBehaviour, ITickable
{
    public int TickPriority => 0;
    public bool IsTickable => _isActive;

    [SerializeField] private bool _isActive = true;
    [SerializeField] private float _reportInterval = 10f;

    private Dictionary<string, ProfileData> _systemProfiles = new();
    private float _timeSinceLastReport;

    public void Tick(float deltaTime)
    {
        ProfileSystemPerformance();

        _timeSinceLastReport += deltaTime;
        if (_timeSinceLastReport >= _reportInterval)
        {
            GeneratePerformanceReport();
            _timeSinceLastReport = 0f;
        }
    }

    private void ProfileSystemPerformance()
    {
        // Profile plant growth system
        ProfileSystem("PlantGrowth", () =>
        {
            var jobManager = ServiceContainer.Resolve<PlantSystemJobManager>();
            return jobManager.GetLastFrameTime();
        });

        // Profile construction system
        ProfileSystem("Construction", () =>
        {
            var construction = ServiceContainer.Resolve<IConstructionManager>();
            return construction.GetLastUpdateTime();
        });

        // Profile IPM system
        ProfileSystem("IPM", () =>
        {
            var ipm = ServiceContainer.Resolve<IActiveIPMSystem>();
            return ipm.GetLastUpdateTime();
        });

        // Profile rendering
        ProfileSystem("Rendering", () =>
        {
            return Time.smoothDeltaTime - Time.deltaTime;
        });

        // Profile garbage collection
        ProfileSystem("GC", () =>
        {
            return GC.GetTotalMemory(false) / 1024f / 1024f; // MB
        });
    }

    private void ProfileSystem(string systemName, Func<float> measurementFunc)
    {
        if (!_systemProfiles.ContainsKey(systemName))
        {
            _systemProfiles[systemName] = new ProfileData { SystemName = systemName };
        }

        var data = _systemProfiles[systemName];
        var measurement = measurementFunc();

        data.Measurements.Add(measurement);
        if (data.Measurements.Count > 600) // Keep last 10 seconds at 60fps
            data.Measurements.RemoveAt(0);
    }

    private void GeneratePerformanceReport()
    {
        var report = new StringBuilder();
        report.AppendLine("=== PERFORMANCE REPORT ===");
        report.AppendLine($"Timestamp: {DateTime.UtcNow:HH:mm:ss}");
        report.AppendLine();

        foreach (var profile in _systemProfiles.Values)
        {
            if (profile.Measurements.Count == 0) continue;

            var avg = profile.Measurements.Average();
            var max = profile.Measurements.Max();
            var min = profile.Measurements.Min();

            report.AppendLine($"{profile.SystemName}:");
            report.AppendLine($"  Avg: {avg:F3}ms | Max: {max:F3}ms | Min: {min:F3}ms");
        }

        report.AppendLine();
        report.AppendLine($"FPS: {1f / Time.smoothDeltaTime:F1}");
        report.AppendLine($"Memory: {GC.GetTotalMemory(false) / 1024f / 1024f:F2}MB");

        ChimeraLogger.Log("PERFORMANCE", report.ToString(), this);
    }
}

public class ProfileData
{
    public string SystemName;
    public List<float> Measurements = new();
}
```

### Week 16, Day 4-5 & Week 17: Stress Testing

**Extreme Load Scenarios:**

```csharp
// Tests/Stress/StressTestScenarios.cs
public class StressTestScenarios : MonoBehaviour
{
    public async Task RunMaximumCapacityTest()
    {
        ChimeraLogger.Log("STRESS", "=== MAXIMUM CAPACITY STRESS TEST ===", this);

        var cultivation = ServiceContainer.Resolve<ICultivationManager>();
        var construction = ServiceContainer.Resolve<IConstructionManager>();
        var genetics = ServiceContainer.Resolve<IGeneticsService>();

        // Scenario: Massive Custom Facility at maximum capacity
        // Target: 5000 plants, 2000 equipment items, 100+ schematics

        // 1. Build maximum infrastructure
        ChimeraLogger.Log("STRESS", "Phase 1: Building maximum infrastructure", this);
        for (int room = 0; room < 20; room++)
        {
            var roomPosition = new Vector3Int(room % 5 * 40, 0, room / 5 * 40);
            await construction.PlaceStructureAsync(GetGrowRoomSchematic(), roomPosition);

            // Add equipment to each room
            for (int equipment = 0; equipment < 100; equipment++)
            {
                var equipPos = roomPosition + new Vector3Int(equipment % 10, 0, equipment / 10);
                await construction.PlaceStructureAsync(GetRandomEquipment(), equipPos);
            }
        }

        ChimeraLogger.Log("STRESS", $"Infrastructure complete: {construction.GetTotalPlacedItems()} items", this);

        // 2. Plant maximum plants
        ChimeraLogger.Log("STRESS", "Phase 2: Planting 5000 plants", this);
        var plantTasks = new List<Task<PlantInstance>>();

        for (int i = 0; i < 5000; i++)
        {
            var position = new Vector3(i % 200, 0, i / 200);
            plantTasks.Add(cultivation.CreatePlantAsync(GetRandomStrain(), position));

            if (i % 500 == 0)
            {
                await Task.WhenAll(plantTasks);
                plantTasks.Clear();
                ChimeraLogger.Log("STRESS", $"Planted {i + 1}/5000 plants", this);
            }
        }

        await Task.WhenAll(plantTasks);
        ChimeraLogger.Log("STRESS", "Phase 2 complete: 5000 plants active", this);

        // 3. Run simulation for 10 minutes real-time
        ChimeraLogger.Log("STRESS", "Phase 3: 10-minute simulation at 8x speed", this);
        var timeManager = ServiceContainer.Resolve<ITimeManager>();
        timeManager.SetTimeScale(TimeScaleType.OctSpeed);

        var frameTimes = new List<float>();
        var memoryReadings = new List<long>();

        for (int frame = 0; frame < 36000; frame++) // 10 min @ 60fps
        {
            var frameStart = Time.realtimeSinceStartup;

            UpdateOrchestrator.Instance.Tick(0.016f);

            var frameTime = Time.realtimeSinceStartup - frameStart;
            frameTimes.Add(frameTime);

            if (frame % 600 == 0) // Every 10 seconds
            {
                var memory = GC.GetTotalMemory(true);
                memoryReadings.Add(memory);

                var avgFps = 1f / frameTimes.GetRange(Math.Max(0, frameTimes.Count - 600), Math.Min(600, frameTimes.Count)).Average();

                ChimeraLogger.Log("STRESS",
                    $"Frame {frame}/36000: {avgFps:F1} FPS, {memory / 1024 / 1024}MB", this);
            }

            await Task.Yield();
        }

        // 4. Analyze results
        var avgFrameTime = frameTimes.Average();
        var maxFrameTime = frameTimes.Max();
        var frameDrops = frameTimes.Count(t => t > 0.033f); // >33ms = below 30fps

        var memoryGrowth = memoryReadings.Last() - memoryReadings.First();

        ChimeraLogger.Log("STRESS", "=== STRESS TEST RESULTS ===", this);
        ChimeraLogger.Log("STRESS", $"Avg Frame Time: {avgFrameTime * 1000f:F2}ms", this);
        ChimeraLogger.Log("STRESS", $"Max Frame Time: {maxFrameTime * 1000f:F2}ms", this);
        ChimeraLogger.Log("STRESS", $"Frame Drops: {frameDrops} ({frameDrops / 360f:F1}% of frames)", this);
        ChimeraLogger.Log("STRESS", $"Memory Growth: {memoryGrowth / 1024 / 1024}MB over 10 minutes", this);

        // 5. Success criteria
        var success = avgFrameTime < 0.016f && frameDrops < 360 && memoryGrowth < 50 * 1024 * 1024;

        if (success)
            ChimeraLogger.Log("STRESS", "✅ STRESS TEST PASSED", this);
        else
            ChimeraLogger.LogError("STRESS", "❌ STRESS TEST FAILED", this);
    }

    public async Task RunBreedingMarathonTest()
    {
        ChimeraLogger.Log("STRESS", "=== BREEDING MARATHON STRESS TEST ===", this);

        var blockchain = ServiceContainer.Resolve<IBlockchainGeneticsService>();
        var genetics = ServiceContainer.Resolve<IGeneticsService>();

        // Perform 1000 consecutive breeding operations
        var breedingTimes = new List<float>();
        var offspring = new List<PlantGenotype>();

        var parent1 = GetRandomStrain();
        var parent2 = GetRandomStrain();

        for (int i = 0; i < 1000; i++)
        {
            var startTime = Time.realtimeSinceStartup;

            var child = await blockchain.BreedPlantsAsync(parent1, parent2, (ulong)i);

            var breedingTime = Time.realtimeSinceStartup - startTime;
            breedingTimes.Add(breedingTime);

            offspring.Add(child);

            // Use offspring as next parent
            if (i % 2 == 0)
                parent1 = child;
            else
                parent2 = child;

            if (i % 100 == 0)
            {
                ChimeraLogger.Log("STRESS", $"Breeding {i + 1}/1000 complete", this);
            }
        }

        // Validate blockchain integrity
        var ledger = ServiceContainer.Resolve<GeneticLedger>();
        var chainValid = ledger.ValidateChain();

        var avgBreedingTime = breedingTimes.Average();
        var maxBreedingTime = breedingTimes.Max();

        ChimeraLogger.Log("STRESS", "=== BREEDING MARATHON RESULTS ===", this);
        ChimeraLogger.Log("STRESS", $"Total Breedings: 1000", this);
        ChimeraLogger.Log("STRESS", $"Avg Breeding Time: {avgBreedingTime:F3}s", this);
        ChimeraLogger.Log("STRESS", $"Max Breeding Time: {maxBreedingTime:F3}s", this);
        ChimeraLogger.Log("STRESS", $"Blockchain Valid: {chainValid}", this);

        var success = avgBreedingTime < 1.0f && chainValid;

        if (success)
            ChimeraLogger.Log("STRESS", "✅ BREEDING MARATHON PASSED", this);
        else
            ChimeraLogger.LogError("STRESS", "❌ BREEDING MARATHON FAILED", this);
    }

    private SchematicSO GetGrowRoomSchematic() { /* Implementation */ return null; }
    private SchematicSO GetRandomEquipment() { /* Implementation */ return null; }
    private PlantGenotype GetRandomStrain() { /* Implementation */ return null; }
}
```

---

## WEEK 18-19: TUTORIAL SYSTEM IMPLEMENTATION

### Week 18: Tutorial Design & Flow

**Guided Tutorial in Massive Custom Facility:**

```csharp
// Systems/Tutorial/TutorialManager.cs
public class TutorialManager : MonoBehaviour, ITutorialManager
{
    [SerializeField] private TutorialStep[] _tutorialSteps;
    [SerializeField] private GameObject _tutorialUIPanel;
    [SerializeField] private Text _tutorialTextDisplay;
    [SerializeField] private Button _nextButton;
    [SerializeField] private Button _skipButton;

    private int _currentStepIndex = 0;
    private bool _tutorialActive = false;

    public void StartTutorial()
    {
        _tutorialActive = true;
        _currentStepIndex = 0;

        // Load tutorial scene (Massive Custom Facility - fully built)
        var sceneManager = ServiceContainer.Resolve<ISceneManager>();
        sceneManager.LoadSceneAsync("MassiveCustomFacility_Tutorial");

        ChimeraLogger.Log("TUTORIAL", "Tutorial started", this);

        ExecuteStep(_tutorialSteps[0]);
    }

    private void ExecuteStep(TutorialStep step)
    {
        _tutorialTextDisplay.text = step.InstructionText;
        _tutorialUIPanel.SetActive(true);

        // Execute step logic
        switch (step.StepType)
        {
            case TutorialStepType.CameraControls:
                TeachCameraControls();
                break;

            case TutorialStepType.ModeToggling:
                TeachModeToggling();
                break;

            case TutorialStepType.PlantInspection:
                TeachPlantInspection();
                break;

            case TutorialStepType.WateringPlant:
                TeachWateringPlant();
                break;

            case TutorialStepType.EnvironmentalAdjustment:
                TeachEnvironmentalAdjustment();
                break;

            case TutorialStepType.ConstructionBasics:
                TeachConstructionBasics();
                break;

            case TutorialStepType.GeneticsIntroduction:
                TeachGeneticsBasics();
                break;

            case TutorialStepType.HarvestingProcess:
                TeachHarvesting();
                break;

            case TutorialStepType.TimeControl:
                TeachTimeControl();
                break;

            case TutorialStepType.ResourceManagement:
                TeachResourceManagement();
                break;
        }
    }

    private void TeachCameraControls()
    {
        var cameraController = ServiceContainer.Resolve<ICameraController>();

        _tutorialTextDisplay.text = @"
## CAMERA CONTROLS

Use these controls to navigate:
- **Mouse Scroll**: Zoom in/out
- **Right Click + Drag**: Rotate camera
- **WASD / Arrow Keys**: Pan camera
- **F**: Focus on selected object

Try zooming into the grow room below, then zoom back out to the facility level.
        ";

        // Wait for player to zoom in and out
        StartCoroutine(WaitForCameraZoom(() =>
        {
            ChimeraLogger.Log("TUTORIAL", "Camera controls learned", this);
            NextStep();
        }));
    }

    private void TeachPlantInspection()
    {
        var cameraController = ServiceContainer.Resolve<ICameraController>();

        _tutorialTextDisplay.text = @"
## PLANT INSPECTION

Click on the highlighted plant to inspect it. Notice:
- Health indicator
- Growth stage
- Environmental needs
- Genetic traits

The camera will automatically zoom to Plant View.
        ";

        // Highlight a specific plant
        var tutorialPlant = GameObject.Find("TutorialPlant_01");
        HighlightObject(tutorialPlant);

        // Wait for player to click plant
        StartCoroutine(WaitForPlantClick(tutorialPlant, () =>
        {
            ChimeraLogger.Log("TUTORIAL", "Plant inspection learned", this);
            NextStep();
        }));
    }

    private void TeachWateringPlant()
    {
        var cultivationModeController = ServiceContainer.Resolve<IGameplayModeController>();
        cultivationModeController.SetMode(GameplayMode.Cultivation);

        _tutorialTextDisplay.text = @"
## WATERING A PLANT

1. Open the **Cultivation Mode** menu (bottom of screen)
2. Select the **Tools** tab
3. Choose **Watering** from the sub-tabs
4. Click **Watering Can**
5. Click on the highlighted plant to water it

Watch the moisture level increase!
        ";

        var tutorialPlant = GameObject.Find("TutorialPlant_02");
        HighlightObject(tutorialPlant);

        StartCoroutine(WaitForPlantWatered(tutorialPlant, () =>
        {
            ChimeraLogger.Log("TUTORIAL", "Plant watering learned", this);
            NextStep();
        }));
    }

    private void TeachConstructionBasics()
    {
        var constructionModeController = ServiceContainer.Resolve<IGameplayModeController>();
        constructionModeController.SetMode(GameplayMode.Construction);

        _tutorialTextDisplay.text = @"
## CONSTRUCTION BASICS

Let's build a small structure:
1. Switch to **Construction Mode** (click icon in top corner)
2. Select **Rooms** tab → **Walls** sub-tab
3. Choose a wall piece
4. Place it in the highlighted area
5. Rotate with **R** key if needed

Notice the blueprint visualization showing valid/invalid placement.
        ";

        var buildArea = GameObject.Find("TutorialBuildArea");
        HighlightArea(buildArea);

        StartCoroutine(WaitForWallPlaced(buildArea, () =>
        {
            ChimeraLogger.Log("TUTORIAL", "Construction basics learned", this);
            NextStep();
        }));
    }

    private void TeachGeneticsBasics()
    {
        var geneticsModeController = ServiceContainer.Resolve<IGameplayModeController>();
        geneticsModeController.SetMode(GameplayMode.Genetics);

        _tutorialTextDisplay.text = @"
## GENETICS INTRODUCTION

1. Switch to **Genetics Mode**
2. Open **Seed Bank** tab
3. View your starting strains
4. Click the **ⓘ** icon on a strain to see:
   - THC/CBD levels
   - Yield potential
   - Growth characteristics
   - Blockchain verification ✅

Notice the verification badge - this strain is authenticated!
        ";

        StartCoroutine(WaitForStrainInspected(() =>
        {
            ChimeraLogger.Log("TUTORIAL", "Genetics basics learned", this);
            NextStep();
        }));
    }

    private void TeachTimeControl()
    {
        var timeManager = ServiceContainer.Resolve<ITimeManager>();

        _tutorialTextDisplay.text = @"
## TIME CONTROL

You can speed up or slow down time:
1. Click the **Clock Icon** in the top right
2. Select a time scale:
   - Real-Time: 1:1 with reality (slowest, best genetics)
   - 1x Baseline: 1 week = 1 hour
   - 8x Speed: 60 days = 1 hour (fastest, reduced genetics)

Try changing the time scale to 2x Speed.

**Tip**: Higher speeds sacrifice genetic potential for faster progress!
        ";

        StartCoroutine(WaitForTimeScaleChange(TimeScaleType.DoubleSpeed, () =>
        {
            ChimeraLogger.Log("TUTORIAL", "Time control learned", this);
            NextStep();
        }));
    }

    private void TeachHarvesting()
    {
        _tutorialTextDisplay.text = @"
## HARVESTING

This plant is ready to harvest:
1. Click on the plant
2. Select **Harvest** from the action menu
3. Choose drying method (Hang Dry recommended)
4. Monitor drying progress in **Processing** tab
5. Once dry, start curing for best quality

Watch the quality percentage improve during curing!
        ";

        var harvestReadyPlant = GameObject.Find("TutorialPlant_HarvestReady");
        HighlightObject(harvestReadyPlant);

        StartCoroutine(WaitForHarvest(harvestReadyPlant, () =>
        {
            ChimeraLogger.Log("TUTORIAL", "Harvesting learned", this);
            NextStep();
        }));
    }

    private void NextStep()
    {
        _currentStepIndex++;

        if (_currentStepIndex >= _tutorialSteps.Length)
        {
            CompleteTutorial();
        }
        else
        {
            ExecuteStep(_tutorialSteps[_currentStepIndex]);
        }
    }

    private void CompleteTutorial()
    {
        _tutorialActive = false;
        _tutorialUIPanel.SetActive(false);

        // Show completion summary
        var completionPanel = GameObject.Find("TutorialCompletionPanel");
        completionPanel.SetActive(true);

        var completionText = completionPanel.GetComponentInChildren<Text>();
        completionText.text = @"
# TUTORIAL COMPLETE! 🎉

You've learned the basics of Project Chimera:
✅ Camera navigation
✅ Plant care and cultivation
✅ Construction and building
✅ Genetics and breeding
✅ Time control
✅ Harvesting and processing

**You're now ready to start your own grow operation!**

You'll begin in a small 15'x15' storage bay.
Use what you've learned to build your cannabis empire!

Good luck, Cultivator! 🌱
        ";

        // Award starting bonuses
        var progressionManager = ServiceContainer.Resolve<IProgressionManager>();
        progressionManager.AwardSkillPoints(5);

        var economyManager = ServiceContainer.Resolve<IEconomyManager>();
        economyManager.AddFunds(10000f);

        ChimeraLogger.Log("TUTORIAL", "Tutorial completed", this);

        // After player clicks "Start Game", load into actual starting facility
        var startButton = completionPanel.transform.Find("StartGameButton").GetComponent<Button>();
        startButton.onClick.AddListener(() =>
        {
            var sceneManager = ServiceContainer.Resolve<ISceneManager>();
            sceneManager.LoadSceneAsync("SmallStorageBay_15x15");
        });
    }

    // Helper coroutines
    private IEnumerator WaitForCameraZoom(System.Action onComplete) { /* Implementation */ yield return null; onComplete(); }
    private IEnumerator WaitForPlantClick(GameObject plant, System.Action onComplete) { /* Implementation */ yield return null; onComplete(); }
    private IEnumerator WaitForPlantWatered(GameObject plant, System.Action onComplete) { /* Implementation */ yield return null; onComplete(); }
    private IEnumerator WaitForWallPlaced(GameObject area, System.Action onComplete) { /* Implementation */ yield return null; onComplete(); }
    private IEnumerator WaitForStrainInspected(System.Action onComplete) { /* Implementation */ yield return null; onComplete(); }
    private IEnumerator WaitForTimeScaleChange(TimeScaleType targetScale, System.Action onComplete) { /* Implementation */ yield return null; onComplete(); }
    private IEnumerator WaitForHarvest(GameObject plant, System.Action onComplete) { /* Implementation */ yield return null; onComplete(); }

    private void HighlightObject(GameObject obj) { /* Add visual highlight */ }
    private void HighlightArea(GameObject area) { /* Add area highlight */ }
}

// Data/Tutorial/TutorialData.cs
public enum TutorialStepType
{
    CameraControls,
    ModeToggling,
    PlantInspection,
    WateringPlant,
    EnvironmentalAdjustment,
    ConstructionBasics,
    GeneticsIntroduction,
    HarvestingProcess,
    TimeControl,
    ResourceManagement
}

[System.Serializable]
public class TutorialStep
{
    public TutorialStepType StepType;
    public string InstructionText;
    public float EstimatedDuration; // seconds
}
```

---

## WEEK 20: PHASE 2 READINESS CERTIFICATION

### Final Validation & Certification

**Comprehensive Readiness Checklist:**

```csharp
// Systems/Validation/Phase2ReadinessCertification.cs
public class Phase2ReadinessCertification : MonoBehaviour
{
    public async Task<CertificationReport> CertifyPhase2Readiness()
    {
        var report = new CertificationReport
        {
            CertificationDate = DateTime.UtcNow,
            CertifierVersion = Application.version
        };

        ChimeraLogger.Log("CERTIFICATION", "=== PHASE 2 READINESS CERTIFICATION STARTING ===", this);

        // CATEGORY 1: Architecture (25 points)
        report.Categories.Add(await ValidateArchitecture());

        // CATEGORY 2: Three Pillars (30 points)
        report.Categories.Add(await ValidateThreePillars());

        // CATEGORY 3: Core Systems (25 points)
        report.Categories.Add(await ValidateCoreSystems());

        // CATEGORY 4: Performance (10 points)
        report.Categories.Add(await ValidatePerformance());

        // CATEGORY 5: Quality & Polish (10 points)
        report.Categories.Add(await ValidateQualityPolish());

        // Calculate final score
        report.TotalScore = report.Categories.Sum(c => c.Score);
        report.MaxScore = report.Categories.Sum(c => c.MaxScore);
        report.PercentageScore = (report.TotalScore / report.MaxScore) * 100f;

        // Determine certification
        report.IsCertified = report.PercentageScore >= 90f; // 90% required for Phase 2

        GenerateCertificationReport(report);

        return report;
    }

    private async Task<CertificationCategory> ValidateArchitecture()
    {
        var category = new CertificationCategory
        {
            CategoryName = "Architecture Health",
            MaxScore = 25
        };

        // Check 1: Zero anti-patterns (10 points)
        var antiPatterns = new Dictionary<string, int>
        {
            ["FindObjectOfType"] = CountPattern("FindObjectOfType"),
            ["Debug.Log"] = CountPattern("Debug\\.Log"),
            ["Resources.Load"] = CountPattern("Resources\\.Load"),
            ["Reflection"] = CountPattern("GetField\\(|GetProperty\\("),
            ["Update()"] = CountPattern("void Update\\(\\)")
        };

        var totalViolations = antiPatterns.Values.Sum();
        category.Checks.Add(new CertificationCheck
        {
            CheckName = "Anti-Pattern Elimination",
            MaxPoints = 10,
            PointsAwarded = totalViolations == 0 ? 10 : 0,
            Status = totalViolations == 0 ? "✅ PASS" : $"❌ FAIL ({totalViolations} violations)",
            Details = string.Join(", ", antiPatterns.Select(kvp => $"{kvp.Key}: {kvp.Value}"))
        });

        // Check 2: File size compliance (5 points)
        var oversizedFiles = FindFilesExceedingSize(400);
        category.Checks.Add(new CertificationCheck
        {
            CheckName = "File Size Compliance",
            MaxPoints = 5,
            PointsAwarded = oversizedFiles.Count == 0 ? 5 : 0,
            Status = oversizedFiles.Count == 0 ? "✅ PASS" : $"❌ FAIL ({oversizedFiles.Count} files >400 lines)",
            Details = oversizedFiles.Count > 0 ? string.Join(", ", oversizedFiles.Take(5)) : "All files compliant"
        });

        // Check 3: ServiceContainer DI (5 points)
        var allServicesResolve = ValidateAllServicesResolve();
        category.Checks.Add(new CertificationCheck
        {
            CheckName = "Dependency Injection",
            MaxPoints = 5,
            PointsAwarded = allServicesResolve ? 5 : 0,
            Status = allServicesResolve ? "✅ PASS" : "❌ FAIL",
            Details = "All required services resolve via ServiceContainer"
        });

        // Check 4: Quality gates enforcing (5 points)
        var qualityGatesEnforced = ValidateQualityGatesEnforced();
        category.Checks.Add(new CertificationCheck
        {
            CheckName = "Quality Gate Enforcement",
            MaxPoints = 5,
            PointsAwarded = qualityGatesEnforced ? 5 : 0,
            Status = qualityGatesEnforced ? "✅ PASS" : "❌ FAIL",
            Details = "CI/CD and pre-commit hooks blocking violations"
        });

        category.Score = category.Checks.Sum(c => c.PointsAwarded);
        return category;
    }

    private async Task<CertificationCategory> ValidateThreePillars()
    {
        var category = new CertificationCategory
        {
            CategoryName = "Three Pillars Implementation",
            MaxScore = 30
        };

        // Construction Pillar (10 points)
        var constructionComplete = await ValidateConstructionPillar();
        category.Checks.Add(new CertificationCheck
        {
            CheckName = "Construction Pillar",
            MaxPoints = 10,
            PointsAwarded = constructionComplete.Score,
            Status = constructionComplete.IsComplete ? "✅ COMPLETE" : "⚠️ INCOMPLETE",
            Details = constructionComplete.Summary
        });

        // Cultivation Pillar (10 points)
        var cultivationComplete = await ValidateCultivationPillar();
        category.Checks.Add(new CertificationCheck
        {
            CheckName = "Cultivation Pillar",
            MaxPoints = 10,
            PointsAwarded = cultivationComplete.Score,
            Status = cultivationComplete.IsComplete ? "✅ COMPLETE" : "⚠️ INCOMPLETE",
            Details = cultivationComplete.Summary
        });

        // Genetics Pillar (10 points)
        var geneticsComplete = await ValidateGeneticsPillar();
        category.Checks.Add(new CertificationCheck
        {
            CheckName = "Genetics Pillar",
            MaxPoints = 10,
            PointsAwarded = geneticsComplete.Score,
            Status = geneticsComplete.IsComplete ? "✅ COMPLETE" : "⚠️ INCOMPLETE",
            Details = geneticsComplete.Summary
        });

        category.Score = category.Checks.Sum(c => c.PointsAwarded);
        return category;
    }

    private async Task<PillarValidation> ValidateConstructionPillar()
    {
        var validation = new PillarValidation { MaxScore = 10 };
        var features = new List<string>();

        // Required features
        if (ValidateFeatureExists(typeof(IGridSystem))) { validation.Score += 2; features.Add("Grid system"); }
        if (ValidateFeatureExists(typeof(IElectricalSystem))) { validation.Score += 2; features.Add("Electrical"); }
        if (ValidateFeatureExists(typeof(IPlumbingSystem))) { validation.Score += 2; features.Add("Plumbing"); }
        if (ValidateFeatureExists(typeof(IHVACSystem))) { validation.Score += 2; features.Add("HVAC"); }
        if (ValidateFeatureExists(typeof(IFacilityProgressionManager))) { validation.Score += 1; features.Add("Facility progression"); }
        if (ValidateFeatureExists(typeof(ISchematicManager))) { validation.Score += 1; features.Add("Schematics"); }

        validation.IsComplete = validation.Score >= 8; // 80% required
        validation.Summary = $"Features: {string.Join(", ", features)} ({validation.Score}/10)";

        return validation;
    }

    private async Task<PillarValidation> ValidateCultivationPillar()
    {
        var validation = new PillarValidation { MaxScore = 10 };
        var features = new List<string>();

        if (ValidateFeatureExists(typeof(IPlantGrowthSystem))) { validation.Score += 2; features.Add("Growth system"); }
        if (ValidateFeatureExists(typeof(IActiveIPMSystem))) { validation.Score += 2; features.Add("IPM"); }
        if (ValidateFeatureExists(typeof(IPlantWorkSystem))) { validation.Score += 2; features.Add("Plant work"); }
        if (ValidateFeatureExists(typeof(IProcessingSystem))) { validation.Score += 2; features.Add("Processing"); }
        if (ValidateFeatureExists(typeof(ICultivationEnvironmentalController))) { validation.Score += 1; features.Add("Environmental"); }
        if (ValidateFeatureExists(typeof(IHarvestManager))) { validation.Score += 1; features.Add("Harvesting"); }

        validation.IsComplete = validation.Score >= 8;
        validation.Summary = $"Features: {string.Join(", ", features)} ({validation.Score}/10)";

        return validation;
    }

    private async Task<PillarValidation> ValidateGeneticsPillar()
    {
        var validation = new PillarValidation { MaxScore = 10 };
        var features = new List<string>();

        if (ValidateFeatureExists(typeof(IBlockchainGeneticsService))) { validation.Score += 3; features.Add("Blockchain"); }
        if (ValidateFeatureExists(typeof(ITissueCultureSystem))) { validation.Score += 2; features.Add("Tissue culture"); }
        if (ValidateFeatureExists(typeof(IMicropropagationSystem))) { validation.Score += 2; features.Add("Micropropagation"); }
        if (ValidateFeatureExists(typeof(IFractalGeneticsEngine))) { validation.Score += 2; features.Add("Fractal genetics"); }
        if (ValidateFeatureExists(typeof(ITraitExpressionEngine))) { validation.Score += 1; features.Add("Trait expression"); }

        validation.IsComplete = validation.Score >= 8;
        validation.Summary = $"Features: {string.Join(", ", features)} ({validation.Score}/10)";

        return validation;
    }

    private void GenerateCertificationReport(CertificationReport report)
    {
        var reportText = new StringBuilder();
        reportText.AppendLine("# PROJECT CHIMERA PHASE 2 READINESS CERTIFICATION");
        reportText.AppendLine($"**Date**: {report.CertificationDate:yyyy-MM-dd HH:mm:ss}");
        reportText.AppendLine($"**Version**: {report.CertifierVersion}");
        reportText.AppendLine();
        reportText.AppendLine("---");
        reportText.AppendLine();

        reportText.AppendLine("## OVERALL RESULT");
        reportText.AppendLine($"**Score**: {report.TotalScore}/{report.MaxScore} ({report.PercentageScore:F1}%)");
        reportText.AppendLine($"**Status**: {(report.IsCertified ? "✅ **CERTIFIED FOR PHASE 2**" : "❌ **NOT READY FOR PHASE 2**")}");
        reportText.AppendLine();

        if (!report.IsCertified)
        {
            reportText.AppendLine("**Minimum required**: 90% (90/100 points)");
            reportText.AppendLine($"**Gap**: {90 - report.PercentageScore:F1}% ({90 - report.TotalScore} points)");
            reportText.AppendLine();
        }

        reportText.AppendLine("---");
        reportText.AppendLine();

        foreach (var category in report.Categories)
        {
            reportText.AppendLine($"## {category.CategoryName}");
            reportText.AppendLine($"**Score**: {category.Score}/{category.MaxScore}");
            reportText.AppendLine();

            foreach (var check in category.Checks)
            {
                reportText.AppendLine($"### {check.CheckName}");
                reportText.AppendLine($"- **Status**: {check.Status}");
                reportText.AppendLine($"- **Points**: {check.PointsAwarded}/{check.MaxPoints}");
                reportText.AppendLine($"- **Details**: {check.Details}");
                reportText.AppendLine();
            }

            reportText.AppendLine("---");
            reportText.AppendLine();
        }

        if (!report.IsCertified)
        {
            reportText.AppendLine("## REQUIRED ACTIONS");
            reportText.AppendLine("Address all failed checks to achieve Phase 2 certification:");
            reportText.AppendLine();

            var failedChecks = report.Categories.SelectMany(c => c.Checks).Where(ch => ch.PointsAwarded < ch.MaxPoints);
            foreach (var check in failedChecks)
            {
                reportText.AppendLine($"- **{check.CheckName}**: {check.Status}");
            }
        }

        var reportPath = Path.Combine(Application.dataPath, "..", "Documents", $"Phase2_Certification_{DateTime.UtcNow:yyyyMMdd_HHmmss}.md");
        File.WriteAllText(reportPath, reportText.ToString());

        ChimeraLogger.Log("CERTIFICATION", $"Certification report saved: {reportPath}", this);
    }

    // Helper methods
    private int CountPattern(string pattern) { /* Implementation */ return 0; }
    private List<string> FindFilesExceedingSize(int maxLines) { /* Implementation */ return new List<string>(); }
    private bool ValidateAllServicesResolve() { /* Implementation */ return true; }
    private bool ValidateQualityGatesEnforced() { /* Implementation */ return true; }
    private bool ValidateFeatureExists(Type interfaceType) { /* Implementation */ return true; }
}

// Data/Validation/CertificationData.cs
public class CertificationReport
{
    public DateTime CertificationDate;
    public string CertifierVersion;
    public List<CertificationCategory> Categories = new();
    public float TotalScore;
    public float MaxScore;
    public float PercentageScore;
    public bool IsCertified;
}

public class CertificationCategory
{
    public string CategoryName;
    public float MaxScore;
    public float Score;
    public List<CertificationCheck> Checks = new();
}

public class CertificationCheck
{
    public string CheckName;
    public float MaxPoints;
    public float PointsAwarded;
    public string Status;
    public string Details;
}

public class PillarValidation
{
    public float MaxScore;
    public float Score;
    public bool IsComplete;
    public string Summary;
}
```

---

## FINAL SUCCESS CRITERIA FOR PHASE 2 READINESS

**ALL must be achieved:**

### Architecture (25/25 points required)
- ✅ Zero FindObjectOfType violations
- ✅ Zero Debug.Log violations
- ✅ Zero Resources.Load violations
- ✅ Zero reflection violations
- ✅ ≤5 Update() methods
- ✅ Zero files >400 lines
- ✅ 100% ServiceContainer DI
- ✅ Quality gates enforcing

### Three Pillars (27/30 points minimum)
- ✅ Construction: Grid, utilities (E/W/HVAC), facility progression, schematics
- ✅ Cultivation: Growth, IPM, plant work, processing, environmental
- ✅ Genetics: Blockchain, tissue culture, micropropagation, fractal math

### Core Systems (22/25 points minimum)
- ✅ Contextual menu UI operational
- ✅ Time mechanics with 6 scales
- ✅ Progression system (skill tree)
- ✅ Marketplace platform
- ✅ Save/load with offline progression
- ✅ Tutorial system complete

### Performance (9/10 points minimum)
- ✅ 1000 plants @ 60 FPS
- ✅ Zero memory leaks
- ✅ Blockchain <1s per breed

### Quality & Polish (8/10 points minimum)
- ✅ 80% test coverage
- ✅ Complete documentation
- ✅ Integration tests passing
- ✅ Tutorial polished

**MINIMUM TOTAL: 90/100 points (90%)**

---

## TIMELINE SUMMARY

**Total Duration: 16-20 weeks**

- **Weeks 1-5**: Phase 0 - Foundation (anti-patterns, file size, quality gates)
- **Weeks 5-7**: Blockchain genetics & utilities
- **Weeks 8-10**: Three pillars completion
- **Weeks 10-13**: Advanced systems & UI
- **Weeks 14-15**: Integration testing
- **Weeks 16-17**: Performance optimization
- **Weeks 18-19**: Tutorial system
- **Week 20**: Certification

**ONLY AFTER 90%+ CERTIFICATION: BEGIN PHASE 2 DEVELOPMENT**

---

*End of Part 5: Phase 2 Preparation & Validation (Weeks 14-20)*
*END OF ULTIMATE IMPLEMENTATION ROADMAP*
*Project Chimera is Phase 2 Ready!*
