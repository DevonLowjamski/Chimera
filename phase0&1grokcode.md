\[1 tool called\]

\# \*\*PHASE 0 & PHASE 1 CORRECTION ROADMAP\*\*

\#\# \*\*OVERVIEW\*\*

This roadmap addresses the critical failures identified in the codebase review. The current state represents \*\*incomplete implementation\*\* of both Phase 0 and Phase 1, with fundamental architectural issues blocking Phase 2 development.

\*\*Total Timeline\*\*: 6-8 weeks    
\*\*Critical Path\*\*: 2-3 weeks for blocking issues    
\*\*Success Criteria\*\*: All anti-patterns eliminated, core architecture functional, CI/CD enforcing quality

\---

\#\# \*\*PHASE 0: IMMEDIATE CRISIS RESPONSE (Week 1-2)\*\*

\#\#\# \*\*Priority 1A: FindObjectOfType Elimination (CRITICAL BLOCKER)\*\*  
\*\*Status\*\*: 184 calls remaining across 91 files    
\*\*Impact\*\*: Prevents testing, violates DI principles, performance issues    
\*\*Time\*\*: 3-4 days

\#\#\#\# \*\*Day 1-2: Core Infrastructure (6 calls)\*\*  
\*\*Target Files\*\*:  
\- \`/Core/ServiceBootstrapper.cs:161\`   
\- \`/Core/ServiceManager.cs:43\`  
\- \`/Core/ChimeraServiceModule.cs:114-123\`  
\- \`/Core/DependencyInjection/ManagerInitializer.cs:142\`

\*\*Implementation\*\*:  
\`\`\`csharp  
// REPLACE THIS PATTERN:  
\_instance \= FindObjectOfType\<ServiceBootstrapper\>();

// WITH THIS PATTERN:  
\_instance \= ServiceContainerFactory.Instance?.TryResolve\<ServiceBootstrapper\>();  
if (\_instance \== null) {  
    \_instance \= gameObject.AddComponent\<ServiceBootstrapper\>();  
    ServiceContainerFactory.Instance?.RegisterInstance(\_instance);  
}  
\`\`\`

\*\*Success Criteria\*\*: All 6 core calls replaced, services resolve correctly.

\#\#\#\# \*\*Day 3-4: UI Management Systems (15 calls)\*\*  
\*\*Target Systems\*\*: ConstructionPaletteManager, SchematicLibraryManager, UIManager  
\*\*Pattern\*\*: Replace with DI resolution \+ fallback creation  
\*\*Success Criteria\*\*: UI components properly injected, no null reference exceptions

\#\#\#\# \*\*Day 5-6: Gameplay Systems (89 calls)\*\*  
\*\*Target Systems\*\*: Analytics, Save, Construction, Economy, Camera  
\*\*Pattern\*\*: Constructor injection where possible, service resolution where needed  
\*\*Success Criteria\*\*: All gameplay systems use DI, no scene traversal

\#\#\#\# \*\*Day 7-8: Peripheral Systems (29 calls)\*\*  
\*\*Target Systems\*\*: Audio, SpeedTree, Testing infrastructure  
\*\*Pattern\*\*: Service registration \+ resolution  
\*\*Success Criteria\*\*: All FindObjectOfType calls eliminated

\#\#\# \*\*Priority 1B: Quality Gates Implementation (CRITICAL BLOCKER)\*\*  
\*\*Status\*\*: Current gates are toothless    
\*\*Time\*\*: 2 days

\#\#\#\# \*\*Implement Anti-Pattern Detection\*\*  
\`\`\`csharp  
// Add to QualityGates.cs  
public static readonly string\[\] ForbiddenPatterns \= {  
    "FindObjectOfType\<",  
    "FindObjectsOfType\<",   
    "Resources\\.Load",  
    "Debug\\.Log",  
    "Debug\\.LogWarning",   
    "Debug\\.LogError"  
};  
\`\`\`

\#\#\#\# \*\*CI/CD Integration\*\*  
\- \*\*Unity Cloud Build\*\*: Add pre-build quality checks  
\- \*\*GitHub Actions\*\*: Automated PR validation  
\- \*\*Local Development\*\*: Editor-time validation

\*\*Success Criteria\*\*:   
\- PRs automatically fail with forbidden patterns  
\- Build pipeline blocks anti-patterns  
\- Developer feedback within 30 seconds of commits

\---

\#\# \*\*PHASE 0: CORE FOUNDATION COMPLETION (Week 3-4)\*\*

\#\#\# \*\*Priority 2A: Service Container Unification (HIGH PRIORITY)\*\*  
\*\*Status\*\*: ServiceLocator and DependencyInjection namespaces still exist    
\*\*Time\*\*: 3 days

\#\#\#\# \*\*Consolidate DI Patterns\*\*  
1\. \*\*Remove parallel namespaces\*\*: ServiceLocator, DependencyInjection  
2\. \*\*Standardize registration\*\*: All services use ServiceContainer  
3\. \*\*Implement auto-registration\*\*: Components automatically register with DI container  
4\. \*\*Add validation\*\*: Ensure all services are properly registered at startup

\#\#\#\# \*\*Service Health Monitoring\*\*  
\`\`\`csharp  
public class ServiceHealthMonitor {  
    public void ValidateServiceHealth() {  
        // Check all registered services  
        // Report missing dependencies    
        // Validate service initialization  
    }  
}  
\`\`\`

\*\*Success Criteria\*\*: Single DI container, no parallel systems, all services auto-registered

\#\#\# \*\*Priority 2B: Dangerous Reflection Removal (HIGH PRIORITY)\*\*  
\*\*Status\*\*: Reflection-based field injection still exists    
\*\*Time\*\*: 2 days

\#\#\#\# \*\*Replace Reflection with Explicit DI\*\*  
1\. \*\*Remove field injection\*\*: \`CultivationManager.cs\` reflection calls  
2\. \*\*Constructor injection\*\*: Explicit dependencies in constructors  
3\. \*\*Factory pattern\*\*: For complex object creation  
4\. \*\*Validation\*\*: Runtime checks for missing dependencies

\*\*Success Criteria\*\*: No reflection-based injection, all dependencies explicit

\#\#\# \*\*Priority 2C: Logging Migration Completion (MEDIUM PRIORITY)\*\*  
\*\*Status\*\*: 24 files still use Debug.Log    
\*\*Time\*\*: 3 days

\#\#\#\# \*\*Systematic Replacement\*\*  
\`\`\`csharp  
// REPLACE:  
Debug.Log("Plant growth: " \+ growthRate);

// WITH:  
ChimeraLogger.Log($"Plant growth: {growthRate}", gameObject);  
\`\`\`

\#\#\#\# \*\*Build Configuration\*\*  
\- \*\*Development\*\*: Full logging enabled  
\- \*\*Production\*\*: Compile-time stripping of debug logs  
\- \*\*Performance\*\*: Zero overhead in release builds

\*\*Success Criteria\*\*:   
\- All Debug.Log calls replaced  
\- Build-time log stripping functional  
\- Performance impact eliminated

\---

\#\# \*\*PHASE 1: CORE ARCHITECTURAL REFACTORING (Week 5-6)\*\*

\#\#\# \*\*Priority 3A: Central Update Bus Implementation (HIGH PRIORITY)\*\*  
\*\*Status\*\*: 30 Update methods still exist    
\*\*Time\*\*: 4 days

\#\#\#\# \*\*Migration Strategy\*\*  
1\. \*\*Identify all Update methods\*\*:  
\`\`\`bash  
find Assets/ \-name "\*.cs" \-exec grep \-l "void Update()" {} \\;  
\`\`\`

2\. \*\*Create ITickable implementations\*\*:  
\`\`\`csharp  
public class PlantGrowthSystem : MonoBehaviour, ITickable {  
    public void Tick(float deltaTime) {  
        // Plant growth logic here  
    }  
}  
\`\`\`

3\. \*\*Register with UpdateOrchestrator\*\*:  
\`\`\`csharp  
void Awake() {  
    UpdateOrchestrator.Instance?.RegisterTickable(this);  
}  
\`\`\`

4\. \*\*Priority-based execution\*\*:  
\`\`\`csharp  
// UpdateOrchestrator.cs  
private readonly List\<(ITickable, int)\> \_prioritizedTickables \= new();

public void RegisterTickable(ITickable tickable, int priority \= 0\) {  
    \_prioritizedTickables.Add((tickable, priority));  
    \_prioritizedTickables.Sort((a, b) \=\> b.Item2.CompareTo(a.Item2)); // Higher priority first  
}  
\`\`\`

\#\#\#\# \*\*Performance Optimizations\*\*  
\- \*\*Frame rate consistency\*\*: Central control prevents frame drops  
\- \*\*Conditional updates\*\*: Pause systems when not needed  
\- \*\*Load balancing\*\*: Distribute updates across frames

\*\*Success Criteria\*\*:   
\- All Update methods migrated to ITickable  
\- Frame rate stable at 60 FPS  
\- Systems can be paused/throttled independently

\#\#\# \*\*Priority 3B: Addressables Migration (HIGH PRIORITY)\*\*  
\*\*Status\*\*: Still using Resources.Load    
\*\*Time\*\*: 5 days

\#\#\#\# \*\*Migration Plan\*\*  
1\. \*\*Set up Addressables configuration\*\*:  
\`\`\`csharp  
// Addressables setup  
Addressables.InitializeAsync().Completed \+= (op) \=\> {  
    // Addressables ready  
};  
\`\`\`

2\. \*\*Asset migration\*\*:  
\`\`\`csharp  
// OLD:  
GameObject prefab \= Resources.Load\<GameObject\>("Prefabs/Plant");

// NEW:  
AsyncOperationHandle\<GameObject\> handle \= Addressables.LoadAssetAsync\<GameObject\>("Prefabs/Plant");  
handle.Completed \+= (op) \=\> {  
    GameObject prefab \= op.Result;  
};  
\`\`\`

3\. \*\*Asset management system\*\*:  
\`\`\`csharp  
public class AssetManager {  
    private readonly Dictionary\<string, AsyncOperationHandle\> \_loadedAssets \= new();  
      
    public async Task\<T\> LoadAssetAsync\<T\>(string key) where T : Object {  
        if (\_loadedAssets.ContainsKey(key)) {  
            return \_loadedAssets\[key\] as T;  
        }  
          
        var handle \= Addressables.LoadAssetAsync\<T\>(key);  
        await handle.Task;  
          
        \_loadedAssets\[key\] \= handle;  
        return handle.Result;  
    }  
}  
\`\`\`

\#\#\#\# \*\*Memory Management\*\*  
\- \*\*Reference counting\*\*: Track asset usage  
\- \*\*Automatic unloading\*\*: Release unused assets  
\- \*\*Memory monitoring\*\*: Prevent memory leaks

\*\*Success Criteria\*\*:   
\- All Resources.Load calls replaced  
\- Asynchronous loading implemented  
\- Memory usage optimized  
\- No loading hitches in gameplay

\#\#\# \*\*Priority 3C: SRP Refactoring (MEDIUM PRIORITY)\*\*  
\*\*Status\*\*: Large classes still exist    
\*\*Time\*\*: 4 days

\#\#\#\# \*\*Identify Large Classes\*\*  
\`\`\`bash  
find Assets/ \-name "\*.cs" \-exec wc \-l {} \\; | sort \-nr | head \-10  
\`\`\`

\#\#\#\# \*\*Refactoring Pattern\*\*  
1\. \*\*Extract interfaces\*\*:  
\`\`\`csharp  
public interface IPlantStrainData {  
    string Name { get; }  
    float THCContent { get; }  
    float Yield { get; }  
}

public interface IPlantStrainGenetics {  
    void CalculateInheritance(PlantStrain parent1, PlantStrain parent2);  
}  
\`\`\`

2\. \*\*Split responsibilities\*\*:  
\`\`\`csharp  
// BEFORE: PlantStrainSO.cs (400+ lines)  
public class PlantStrainSO : ScriptableObject {  
    // Data, genetics, UI, serialization all mixed  
}

// AFTER: Multiple focused classes  
public class PlantStrainData : ScriptableObject, IPlantStrainData { }  
public class PlantStrainGenetics : IPlantStrainGenetics { }  
public class PlantStrainUI : IPlantStrainUI { }  
\`\`\`

\*\*Success Criteria\*\*:   
\- No class exceeds 300 lines  
\- Single responsibility per class  
\- Clear separation of concerns  
\- Improved testability

\---

\#\# \*\*PHASE 1: ADVANCED FEATURES (Week 7-8)\*\*

\#\#\# \*\*Priority 4A: Performance Optimization (MEDIUM PRIORITY)\*\*  
\*\*Time\*\*: 3 days

\#\#\#\# \*\*Burst Compiler Integration\*\*  
\`\`\`csharp  
\[BurstCompile\]  
public struct PlantGrowthJob : IJobParallelFor {  
    \[ReadOnly\] public NativeArray\<float\> currentHeights;  
    \[WriteOnly\] public NativeArray\<float\> newHeights;  
      
    public void Execute(int index) {  
        // Burst-compiled plant growth calculations  
        newHeights\[index\] \= currentHeights\[index\] \* growthRate;  
    }  
}  
\`\`\`

\#\#\#\# \*\*Job System Implementation\*\*  
\- \*\*Parallel plant updates\*\*: 1000+ plants at 60 FPS  
\- \*\*Environment calculations\*\*: Physics-based airflow  
\- \*\*Genetic computations\*\*: Deterministic calculations

\*\*Success Criteria\*\*:   
\- 1000+ plants at stable 60 FPS  
\- Environment calculations in real-time  
\- Genetic operations optimized

\#\#\# \*\*Priority 4B: Memory Management (MEDIUM PRIORITY)\*\*  
\*\*Time\*\*: 2 days

\#\#\#\# \*\*Object Pooling\*\*  
\`\`\`csharp  
public class PlantObjectPool {  
    private readonly Queue\<GameObject\> \_availablePlants \= new();  
      
    public GameObject GetPlant() {  
        if (\_availablePlants.Count \> 0\) {  
            return \_availablePlants.Dequeue();  
        }  
        return Instantiate(\_plantPrefab);  
    }  
      
    public void ReturnPlant(GameObject plant) {  
        plant.SetActive(false);  
        \_availablePlants.Enqueue(plant);  
    }  
}  
\`\`\`

\#\#\#\# \*\*Asset Memory Optimization\*\*  
\- \*\*Texture atlasing\*\*: Reduce draw calls  
\- \*\*Mesh combining\*\*: Static geometry optimization  
\- \*\*Reference counting\*\*: Automatic cleanup

\*\*Success Criteria\*\*:   
\- Stable memory usage  
\- No memory leaks  
\- Fast object instantiation

\---

\#\# \*\*QUALITY ASSURANCE & TESTING (Week 9-10)\*\*

\#\#\# \*\*Priority 5A: Comprehensive Testing (HIGH PRIORITY)\*\*  
\*\*Time\*\*: 5 days

\#\#\#\# \*\*Unit Tests\*\*  
\`\`\`csharp  
\[Test\]  
public void ServiceContainer\_Resolve\_RegisteredService\_ReturnsInstance() {  
    // Arrange  
    var container \= new ServiceContainer();  
    var service \= new TestService();  
    container.RegisterInstance\<ITestService\>(service);  
      
    // Act  
    var resolved \= container.Resolve\<ITestService\>();  
      
    // Assert  
    Assert.AreEqual(service, resolved);  
}  
\`\`\`

\#\#\#\# \*\*Integration Tests\*\*  
\- \*\*DI Container\*\*: All services resolve correctly  
\- \*\*Update Bus\*\*: All systems tick properly  
\- \*\*Asset Loading\*\*: Addressables load without errors  
\- \*\*Performance\*\*: Frame rate stability under load

\#\#\#\# \*\*Anti-Pattern Detection Tests\*\*  
\`\`\`csharp  
\[Test\]  
public void CodeQuality\_NoFindObjectOfType\_Calls() {  
    var files \= Directory.GetFiles("Assets/", "\*.cs", SearchOption.AllDirectories);  
    foreach (var file in files) {  
        var content \= File.ReadAllText(file);  
        Assert.IsFalse(content.Contains("FindObjectOfType\<"),   
            $"File {file} contains forbidden FindObjectOfType call");  
    }  
}  
\`\`\`

\*\*Success Criteria\*\*:   
\- 80%+ code coverage  
\- All integration tests pass  
\- Anti-pattern detection working  
\- Performance benchmarks met

\#\#\# \*\*Priority 5B: Performance Benchmarking (MEDIUM PRIORITY)\*\*  
\*\*Time\*\*: 3 days

\#\#\#\# \*\*Automated Benchmarks\*\*  
\`\`\`csharp  
public class PerformanceBenchmark {  
    \[Test\]  
    public void GeneticsSystem\_1000Plants\_Under60ms() {  
        // Setup 1000 plants  
        var plants \= CreateTestPlants(1000);  
          
        // Measure execution time  
        var stopwatch \= Stopwatch.StartNew();  
        \_geneticsSystem.ProcessGeneration(plants);  
        stopwatch.Stop();  
          
        Assert.Less(stopwatch.ElapsedMilliseconds, 60,   
            "Genetics processing took too long");  
    }  
}  
\`\`\`

\*\*Success Criteria\*\*:   
\- 1000+ plants process in \<60ms  
\- Memory usage stable  
\- No frame drops under load

\---

\#\# \*\*VALIDATION & DOCUMENTATION (Week 11-12)\*\*

\#\#\# \*\*Priority 6A: Architecture Documentation (HIGH PRIORITY)\*\*  
\*\*Time\*\*: 4 days

\#\#\#\# \*\*System Documentation\*\*  
\- \*\*Dependency Injection\*\*: Service registration patterns  
\- \*\*Update System\*\*: ITickable implementation guide  
\- \*\*Asset Management\*\*: Addressables usage patterns  
\- \*\*Performance Guidelines\*\*: Optimization best practices

\#\#\#\# \*\*API Documentation\*\*  
\- \*\*Public Interfaces\*\*: Complete XML documentation  
\- \*\*Usage Examples\*\*: Code samples for each system  
\- \*\*Migration Guide\*\*: From old patterns to new architecture

\#\#\# \*\*Priority 6B: Final Validation (HIGH PRIORITY)\*\*  
\*\*Time\*\*: 4 days

\#\#\#\# \*\*Comprehensive Audit\*\*  
\- \*\*Anti-pattern check\*\*: Zero forbidden calls remaining  
\- \*\*Architecture validation\*\*: All patterns correctly implemented  
\- \*\*Performance validation\*\*: All benchmarks pass  
\- \*\*Integration validation\*\*: All systems work together

\#\#\#\# \*\*Phase 2 Readiness Assessment\*\*  
\- \*\*Genetics\*\*: Ready for fractal mathematics implementation  
\- \*\*Cultivation\*\*: Ready for biological accuracy improvements  
\- \*\*Construction\*\*: Ready for meaningful integration  
\- \*\*Overall\*\*: Architecture supports sophisticated systems

\---

\#\# \*\*SUCCESS CRITERIA & VALIDATION\*\*

\#\#\# \*\*Phase 0 Completion Criteria\*\*  
\- ✅ 0 FindObjectOfType calls remaining  
\- ✅ Single unified DI container  
\- ✅ No reflection-based injection  
\- ✅ Centralized logging system  
\- ✅ CI/CD quality gates active

\#\#\# \*\*Phase 1 Completion Criteria\*\*  
\- ✅ Central update bus managing all systems  
\- ✅ Addressables migration complete  
\- ✅ SRP violations resolved  
\- ✅ Performance optimizations implemented  
\- ✅ Comprehensive test coverage

\#\#\# \*\*Overall Project Readiness\*\*  
\- ✅ Architecture supports 1000+ plants at 60 FPS  
\- ✅ Memory management optimized  
\- ✅ All systems properly decoupled  
\- ✅ Documentation complete  
\- ✅ Phase 2 development can begin immediately

\---

\#\# \*\*RISK MITIGATION\*\*

\#\#\# \*\*Contingency Plans\*\*  
1\. \*\*Timeline Slip\*\*: Focus on critical blockers first (FindObjectOfType elimination)  
2\. \*\*Technical Debt\*\*: Regular refactoring sessions, automated quality checks  
3\. \*\*Team Coordination\*\*: Daily standups, clear ownership of components  
4\. \*\*Testing\*\*: Continuous integration, automated regression testing

\#\#\# \*\*Rollback Strategy\*\*  
\- \*\*Git Branches\*\*: Feature branches for each major change  
\- \*\*Incremental Changes\*\*: Small, testable commits  
\- \*\*Validation Gates\*\*: Automated checks prevent broken builds

\#\#\# \*\*Success Metrics\*\*  
\- \*\*Code Quality\*\*: 0 anti-pattern violations  
\- \*\*Performance\*\*: 60 FPS with 1000+ plants  
\- \*\*Maintainability\*\*: Classes \<300 lines, clear responsibilities  
\- \*\*Testability\*\*: 80%+ code coverage, all integration tests pass

This roadmap provides a clear, actionable path to properly complete Phase 0 and Phase 1, establishing a solid foundation for the sophisticated Phase 2 systems described in the gameplay document.  
