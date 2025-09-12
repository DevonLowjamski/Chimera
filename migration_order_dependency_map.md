# Migration Order with Dependencies - Comprehensive Mapping
**Analysis Date**: $(date)
**Based on**: Code analysis of 28 violations and their dependency chains

## DEPENDENCY ANALYSIS SUMMARY

### Core Infrastructure Dependencies:
1. **ServiceContainer** (already exists) → Foundation for all DI
2. **ManagerRegistry** → Depends on ServiceContainer
3. **SimpleManagerRegistry** → Depends on ServiceContainer + ServiceLocator  
4. **GameSystemInitializer** → Depends on ManagerRegistry being migrated first

### System Dependencies:
- **Construction Systems** → Need Camera service + Currency/Trading managers
- **Camera Systems** → Independent (can provide services to others)
- **Environmental Systems** → Independent
- **Performance Systems** → Independent (can gather from registry)

---

## PHASE 1: CRITICAL INFRASTRUCTURE (Week 1, Days 3-4)
**Goal**: Establish unified DI foundation
**Success Criteria**: All managers discoverable via ServiceContainer

### Migration Order #1: ManagerRegistry.cs (BLOCKING)
**File**: `Assets/ProjectChimera/Core/ManagerRegistry.cs:61`
**Current**: `var managers = FindObjectsOfType<ChimeraManager>();`
**Dependencies**: 
- **REQUIRES**: ServiceContainer (✅ exists)
- **BLOCKS**: GameSystemInitializer, all other systems

**Migration Strategy**:
```csharp
// BEFORE
var managers = FindObjectsOfType<ChimeraManager>();

// AFTER  
var managers = _serviceContainer.ResolveAll<ChimeraManager>();
// OR if ResolveAll doesn't exist:
var managers = ServiceContainerFactory.Instance.GetRegisteredServices<ChimeraManager>();
```

**Dependencies Chain**:
```
ServiceContainer (✅) → ManagerRegistry → GameSystemInitializer
                    ↓
                    All other systems
```

### Migration Order #2: SimpleManagerRegistry.cs (BLOCKING)
**File**: `Assets/ProjectChimera/Core/SimpleDI/SimpleManagerRegistry.cs:63`
**Current**: `var managers = FindObjectsOfType<MonoBehaviour>();`
**Dependencies**:
- **REQUIRES**: ServiceContainer, ServiceLocator
- **BLOCKS**: DI system unification

**Migration Strategy**:
```csharp
// BEFORE
var managers = FindObjectsOfType<MonoBehaviour>();

// AFTER - Unify with main ServiceContainer
// Option 1: Deprecate SimpleManagerRegistry, use main ManagerRegistry
// Option 2: Refactor to use ServiceContainer for discovery
var managers = _serviceContainer.ResolveAll<IChimeraManager>();
```

**Dependencies Chain**:
```
ServiceContainer + ServiceLocator → SimpleManagerRegistry → DI Unification
```

### Migration Order #3: GameSystemInitializer.cs (DEPENDS ON #1)
**File**: `Assets/ProjectChimera/Core/GameSystemInitializer.cs:143`
**Current**: `var allManagers = UnityEngine.Object.FindObjectsByType<ChimeraManager>(FindObjectsSortMode.None);`
**Dependencies**:
- **REQUIRES**: ManagerRegistry (#1) to be migrated first
- **BLOCKS**: System initialization process

**Migration Strategy**:
```csharp
// BEFORE
var allManagers = UnityEngine.Object.FindObjectsByType<ChimeraManager>(FindObjectsSortMode.None);

// AFTER
var allManagers = ServiceContainerFactory.Instance.ResolveAll<ChimeraManager>();
// OR inject ManagerRegistry and use it:
var allManagers = _managerRegistry.GetAllRegisteredManagers();
```

**Dependencies Chain**:
```
ManagerRegistry (#1) → GameSystemInitializer → System Initialization
```

---

## PHASE 2A: CONSTRUCTION SYSTEM (Week 1, Day 5)
**Goal**: Construction functionality via DI
**Prerequisites**: Phase 1 complete

### Migration Order #4: GridInputHandler.cs (SIMPLE)
**File**: `Assets/ProjectChimera/Systems/Construction/GridInputHandler.cs:41`
**Current**: `_mainCamera = FindObjectOfType<Camera>();`
**Dependencies**: **INDEPENDENT** (can provide Camera service to others)

**Migration Strategy**:
```csharp
// BEFORE
_mainCamera = FindObjectOfType<Camera>();

// AFTER - Register Camera as service
public void Initialize()
{
    _mainCamera = Camera.main ?? Camera.current;
    if (_mainCamera != null)
    {
        ServiceContainerFactory.Instance.RegisterInstance<Camera>(_mainCamera);
    }
}
```

### Migration Order #5: ConstructionSaveProvider.cs (MODERATE)
**File**: `Assets/ProjectChimera/Systems/Save/ConstructionSaveProvider.cs:84`
**Current**: `_constructionSystem = FindObjectOfType<MonoBehaviour>() as IConstructionSystem;`
**Dependencies**: **INDEPENDENT**

**Migration Strategy**:
```csharp
// BEFORE
_constructionSystem = FindObjectOfType<MonoBehaviour>() as IConstructionSystem;

// AFTER
_constructionSystem = ServiceContainerFactory.Instance.Resolve<IConstructionSystem>();
```

### Migration Order #6-9: Payment Services (SIMPLE - DUPLICATE CODE)
**Files**: 
- `PlacementPaymentService.cs:136,139`
- `RefactoredPlacementPaymentService.cs:136,139`

**Current**: 
```csharp
var currencyObj = GameObject.Find("CurrencyManager"); 
var tradingObj = GameObject.Find("TradingManager");
```

**Dependencies**: **REQUIRES** Currency/Trading managers to be registered in ServiceContainer first

**Migration Strategy**:
```csharp
// BEFORE
var currencyObj = GameObject.Find("CurrencyManager");
var tradingObj = GameObject.Find("TradingManager");

// AFTER
_currencyManager = ServiceContainerFactory.Instance.Resolve<ICurrencyManager>();
_tradingManager = ServiceContainerFactory.Instance.Resolve<ITradingManager>();
```

**Prerequisites**: Need to register Currency/Trading managers first:
```csharp
// In currency/trading manager initialization:
ServiceContainerFactory.Instance.RegisterInstance<ICurrencyManager>(this);
ServiceContainerFactory.Instance.RegisterInstance<ITradingManager>(this);
```

---

## PHASE 2B: CAMERA SYSTEM (Week 2, Day 1)  
**Goal**: Unified camera service
**Prerequisites**: Phase 1 complete

### Migration Order #10-11: Camera Services (SIMPLE)
**Files**:
- `CameraLevelContextualMenuIntegrator.cs:39`
- `CameraService.cs:15`

**Strategy**: Create unified Camera service, eliminate duplicate lookups
```csharp
// Create CameraServiceProvider
public class CameraServiceProvider : MonoBehaviour
{
    void Awake()
    {
        var mainCamera = Camera.main ?? FindObjectOfType<Camera>();
        ServiceContainerFactory.Instance.RegisterInstance<Camera>(mainCamera);
    }
}

// Update consumers to use injected camera
_mainCamera = ServiceContainerFactory.Instance.Resolve<Camera>();
```

---

## PHASE 2C: ENVIRONMENTAL/GAMEPLAY (Week 2, Days 2-3)
**Goal**: Environmental and plant monitoring via services
**Prerequisites**: Phase 1 complete

### Migration Order #12: EnvironmentalDisplay.cs (SIMPLE)
**File**: `Systems/Gameplay/EnvironmentalDisplay.cs:165`
**Current**: `FindObjectOfType<Systems.Environment.EnvironmentalController>()`
**Strategy**: Service injection for EnvironmentalController

### Migration Order #13-15: Plant Monitoring (MODERATE - REQUIRES PLANT REGISTRY)
**Files**: PlantMonitor.cs, GeneticVisualizationManager.cs, DebugOverlayManager.cs
**Current**: `GameObject.FindGameObjectsWithTag("Plant")`
**Strategy**: Create PlantRegistryService
```csharp
public interface IPlantRegistryService  
{
    IEnumerable<GameObject> GetAllPlants();
    void RegisterPlant(GameObject plant);
    void UnregisterPlant(GameObject plant);
}

// Replace FindGameObjectsWithTag with:
var plants = ServiceContainerFactory.Instance.Resolve<IPlantRegistryService>().GetAllPlants();
```

---

## PHASE 3: PERFORMANCE SYSTEMS (Week 2, Days 4-5)
**Goal**: Performance monitoring via registries
**Prerequisites**: Phase 1 complete

### Migration Order #16-19: Performance/WindSystem (COMPLEX)
**Strategy**: Create object registries for efficient access
```csharp
public interface IRendererRegistryService
{
    IEnumerable<MeshRenderer> GetMeshRenderers();
    IEnumerable<Renderer> GetAllRenderers();  
    IEnumerable<WindZone> GetWindZones();
}
```

---

## MIGRATION EXECUTION TIMELINE

### Week 1: Critical Infrastructure + Construction
```
Day 3: Migration #1 (ManagerRegistry) 
Day 3: Migration #2 (SimpleManagerRegistry)
Day 4: Migration #3 (GameSystemInitializer) 
Day 4: Validate Phase 1 - All core services resolve
Day 5: Migrations #4-9 (Construction System)
```

### Week 2: Remaining High Priority
```
Day 1: Migrations #10-11 (Camera System)
Day 2: Migration #12 (Environmental Display)  
Day 3: Migrations #13-15 (Plant Monitoring) - Requires PlantRegistryService
Day 4: Migrations #16-19 (Performance Systems) - Requires Registry Services
Day 5: Full system validation
```

## CRITICAL SUCCESS FACTORS

1. **ServiceContainer Foundation**: Must be solid before any migration
2. **Sequential Dependencies**: #1 → #3, others can be parallel
3. **Service Registration**: Each migrated system must register its services
4. **Validation at Each Step**: Ensure services resolve before proceeding
5. **Rollback Points**: Git tag after each successful migration

## RISK MITIGATION

- **Circular Dependencies**: Core systems (#1-3) must avoid circular references
- **Service Discovery**: Need ResolveAll<T>() method in ServiceContainer
- **Registration Order**: Services must be registered before they're resolved
- **Testing**: Each migration must have integration test before proceeding