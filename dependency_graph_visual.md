# Visual Dependency Graph - Migration Order

## DEPENDENCY FLOW DIAGRAM

```
FOUNDATION LAYER (Week 1, Day 3-4):
═══════════════════════════════════════════════════════════

ServiceContainer (✅ EXISTS)
    ↓
    ├─→ [#1] ManagerRegistry ──────────────┐
    │   (CRITICAL - BLOCKS ALL)            │
    │                                      │
    └─→ [#2] SimpleManagerRegistry         │
        (CRITICAL - DI UNIFICATION)       │
                                          │
                                          ↓
                              [#3] GameSystemInitializer
                              (DEPENDS ON #1)
                                          │
                                          ↓
                                    System Initialization


CONSTRUCTION LAYER (Week 1, Day 5):
═══════════════════════════════════════════════════════════

Foundation Complete (✅)
    ↓
    ├─→ [#4] GridInputHandler ────→ Camera Service (provides to others)
    │   (INDEPENDENT)
    │
    ├─→ [#5] ConstructionSaveProvider
    │   (INDEPENDENT)
    │
    └─→ [#6-9] Payment Services ──→ REQUIRES: Currency/Trading Managers
        (DEPENDENT - need services registered first)


CAMERA LAYER (Week 2, Day 1):
═══════════════════════════════════════════════════════════

Foundation Complete (✅)
    ↓
    ├─→ [#10] CameraLevelContextualMenuIntegrator
    │   (INDEPENDENT)
    │
    └─→ [#11] CameraService ────→ Unified Camera Service
        (INDEPENDENT)


ENVIRONMENTAL LAYER (Week 2, Days 2-3):
═══════════════════════════════════════════════════════════

Foundation Complete (✅)
    ↓
    ├─→ [#12] EnvironmentalDisplay
    │   (INDEPENDENT)
    │
    └─→ [#13-15] Plant Monitoring Systems
        ↓
        REQUIRES: PlantRegistryService (NEW)
        ├─→ PlantMonitor
        ├─→ GeneticVisualizationManager  
        └─→ DebugOverlayManager


PERFORMANCE LAYER (Week 2, Days 4-5):
═══════════════════════════════════════════════════════════

Foundation Complete (✅)
    ↓
    ├─→ [#16] SimplePerformanceManager
    │   ↓
    │   REQUIRES: RendererRegistryService (NEW)
    │
    └─→ [#17-19] WindSystem (3 violations)
        ↓
        REQUIRES: WindZone + Renderer Registry (NEW)


TESTING LAYER (Week 3+ - Optional):
═══════════════════════════════════════════════════════════

Foundation Complete (✅)
    ↓
    └─→ [#20-24] Test Files (OPTIONAL)
        └─→ [#25] Editor Scripts (OPTIONAL)
```

## BLOCKING/DEPENDENCY MATRIX

| Migration | Blocks | Depends On | Can Run Parallel With |
|-----------|--------|------------|----------------------|
| #1 ManagerRegistry | #3, All Systems | ServiceContainer | #2 |
| #2 SimpleManagerRegistry | DI Unification | ServiceContainer | #1 |
| #3 GameSystemInitializer | System Init | #1 ManagerRegistry | - |
| #4 GridInputHandler | - | Phase 1 Complete | #5, #6-9 |
| #5 ConstructionSaveProvider | - | Phase 1 Complete | #4, #6-9 |
| #6-9 Payment Services | - | Currency/Trading Services | #4, #5 |
| #10-11 Camera Services | - | Phase 1 Complete | All Phase 2 |
| #12 Environmental Display | - | Phase 1 Complete | All Phase 2 |
| #13-15 Plant Monitoring | - | PlantRegistryService | All Phase 2 |
| #16-19 Performance/Wind | - | Registry Services | All Phase 3 |
| #20-24 Testing/Editor | - | - | All |

## PARALLEL EXECUTION OPPORTUNITIES

### Week 1 (Phase 1):
- **Sequential Required**: #1 → #3 (GameSystemInitializer depends on ManagerRegistry)
- **Parallel Possible**: #1 and #2 can run simultaneously

### Week 1 Day 5 (Phase 2A):
- **Parallel Possible**: #4, #5 independent
- **Sequential Required**: Currency/Trading registration → #6-9

### Week 2 (Phases 2B, 2C):
- **Parallel Possible**: All Phase 2 systems independent of each other
- **New Service Creation**: PlantRegistryService during #13-15

## NEW SERVICES REQUIRED

### PlantRegistryService (For #13-15)
```csharp
public interface IPlantRegistryService
{
    IEnumerable<GameObject> GetAllPlants();
    void RegisterPlant(GameObject plant);
    void UnregisterPlant(GameObject plant);
    event Action<GameObject> OnPlantRegistered;
    event Action<GameObject> OnPlantUnregistered;
}
```

### RendererRegistryService (For #16-19)  
```csharp
public interface IRendererRegistryService
{
    IEnumerable<MeshRenderer> GetMeshRenderers();
    IEnumerable<Renderer> GetAllRenderers();
    IEnumerable<WindZone> GetWindZones();
    void RegisterRenderer(Renderer renderer);
    void RegisterWindZone(WindZone windZone);
}
```

### CameraServiceProvider (For #10-11)
```csharp
public interface ICameraService
{
    Camera MainCamera { get; }
    Camera GetCamera();
    void RegisterCamera(Camera camera);
}
```

## CRITICAL PATH ANALYSIS

**LONGEST PATH (Critical)**: 
ServiceContainer → #1 ManagerRegistry → #3 GameSystemInitializer → System Initialization

**SHORTEST PATH (Independent)**:
ServiceContainer → Any Phase 2/3 system (after Phase 1 complete)

**BOTTLENECKS**:
1. **ManagerRegistry (#1)** - Blocks GameSystemInitializer and system initialization
2. **New Service Creation** - PlantRegistryService and RendererRegistryService creation