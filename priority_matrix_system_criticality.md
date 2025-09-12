# Priority Matrix by System Criticality - 28 Violations
**Analysis Date**: $(date)
**Based on**: Definitive 28-violation inventory

## PRIORITY MATRIX FRAMEWORK

### Risk Assessment Criteria:
- **System Impact**: Core/High/Medium/Low
- **Business Impact**: Game-breaking/Feature-breaking/Performance/Cosmetic  
- **Migration Complexity**: Simple/Moderate/Complex
- **Dependency Chain**: Independent/Dependent/Blocking Others

---

## CRITICAL PRIORITY (TIER 1) - 3 Violations
**Migration Window**: Week 1, Days 3-4
**Risk Level**: EXTREME - System boot failures

### 1. Core/ManagerRegistry.cs:61
- **Pattern**: `FindObjectsOfType<ChimeraManager>()`
- **System Impact**: CRITICAL - Core system discovery
- **Business Impact**: Game-breaking - prevents manager initialization
- **Migration Complexity**: Moderate - requires ServiceContainer integration
- **Dependencies**: BLOCKING - all other managers depend on this
- **Migration Order**: #1 (FIRST)

### 2. Core/SimpleDI/SimpleManagerRegistry.cs:63  
- **Pattern**: `FindObjectsOfType<MonoBehaviour>()`
- **System Impact**: CRITICAL - DI system discovery
- **Business Impact**: Game-breaking - breaks dependency injection
- **Migration Complexity**: Moderate - requires unified DI approach
- **Dependencies**: BLOCKING - DI system foundation
- **Migration Order**: #2

### 3. Core/GameSystemInitializer.cs:143
- **Pattern**: `UnityEngine.Object.FindObjectsByType<ChimeraManager>()`  
- **System Impact**: CRITICAL - System initialization orchestration
- **Business Impact**: Game-breaking - prevents proper startup sequence
- **Migration Complexity**: Simple - direct replacement with ServiceContainer
- **Dependencies**: DEPENDS ON - ManagerRegistry (#1)
- **Migration Order**: #3

---

## HIGH PRIORITY (TIER 2A) - Construction System - 6 Violations
**Migration Window**: Week 1, Day 5
**Risk Level**: HIGH - Core gameplay functionality

### 4. Systems/Construction/GridInputHandler.cs:41
- **Pattern**: `FindObjectOfType<Camera>()`
- **System Impact**: HIGH - Construction input handling
- **Business Impact**: Feature-breaking - construction mode unusable
- **Migration Complexity**: Simple - Camera service injection
- **Dependencies**: Independent
- **Migration Order**: #4

### 5. Systems/Save/ConstructionSaveProvider.cs:84
- **Pattern**: `FindObjectOfType<MonoBehaviour>() as IConstructionSystem`
- **System Impact**: HIGH - Construction save/load
- **Business Impact**: Feature-breaking - construction persistence fails
- **Migration Complexity**: Moderate - interface resolution via DI
- **Dependencies**: Independent
- **Migration Order**: #5

### 6-9. Payment Services (4 violations - 2 files, 2 calls each)
- **Files**: PlacementPaymentService.cs, RefactoredPlacementPaymentService.cs
- **Patterns**: `GameObject.Find("CurrencyManager")`, `GameObject.Find("TradingManager")`
- **System Impact**: HIGH - Construction economy integration
- **Business Impact**: Feature-breaking - construction costs broken
- **Migration Complexity**: Simple - direct service injection
- **Dependencies**: Independent (duplicate implementations)
- **Migration Order**: #6-9 (can be done in parallel)

---

## HIGH PRIORITY (TIER 2B) - Camera System - 2 Violations  
**Migration Window**: Week 2, Day 1
**Risk Level**: HIGH - UI/UX functionality

### 10. Systems/Camera/CameraLevelContextualMenuIntegrator.cs:39
- **Pattern**: `FindObjectOfType<Camera>()`
- **System Impact**: HIGH - Camera integration with UI
- **Business Impact**: Feature-breaking - contextual menus broken
- **Migration Complexity**: Simple - Camera service injection
- **Dependencies**: Independent
- **Migration Order**: #10

### 11. Systems/Camera/CameraService.cs:15  
- **Pattern**: `FindObjectOfType<UnityEngine.Camera>()`
- **System Impact**: HIGH - Core camera service
- **Business Impact**: Feature-breaking - camera system broken
- **Migration Complexity**: Simple - direct service registration
- **Dependencies**: Independent
- **Migration Order**: #11

---

## HIGH PRIORITY (TIER 2C) - Gameplay/Environmental - 4 Violations
**Migration Window**: Week 2, Days 2-3  
**Risk Level**: HIGH - Core simulation functionality

### 12. Systems/Gameplay/EnvironmentalDisplay.cs:165
- **Pattern**: `FindObjectOfType<EnvironmentalController>()`
- **System Impact**: HIGH - Environmental simulation display
- **Business Impact**: Feature-breaking - environmental UI broken
- **Migration Complexity**: Simple - service injection
- **Dependencies**: Independent
- **Migration Order**: #12

### 13-15. Plant Monitoring Systems (3 violations)
- **Files**: PlantMonitor.cs, GeneticVisualizationManager.cs, DebugOverlayManager.cs
- **Pattern**: `GameObject.FindGameObjectsWithTag("Plant")`
- **System Impact**: HIGH - Plant system monitoring
- **Business Impact**: Feature-breaking - plant visualization/monitoring broken
- **Migration Complexity**: Moderate - requires plant registry system
- **Dependencies**: Independent
- **Migration Order**: #13-15

---

## MEDIUM PRIORITY (TIER 3) - Performance & Services - 4 Violations
**Migration Window**: Week 2, Days 4-5
**Risk Level**: MEDIUM - Performance monitoring and visual effects

### 16. Systems/Performance/SimplePerformanceManager.cs:221
- **Pattern**: `FindObjectsOfType<MeshRenderer>()`  
- **System Impact**: MEDIUM - Performance monitoring
- **Business Impact**: Performance - profiling data missing
- **Migration Complexity**: Complex - requires renderer registry or caching
- **Dependencies**: Independent
- **Migration Order**: #16

### 17-19. SpeedTree Environmental System (3 violations)
- **File**: Systems/Services/SpeedTree/Environmental/WindSystem.cs
- **Patterns**: `FindObjectsOfType<WindZone>()`, `FindObjectsOfType<Renderer>()` (x2)
- **System Impact**: MEDIUM - Environmental visual effects
- **Business Impact**: Performance - wind effects may be degraded
- **Migration Complexity**: Complex - requires environmental object registry
- **Dependencies**: Independent
- **Migration Order**: #17-19

---

## LOW PRIORITY (TIER 4) - Testing & Editor - 7 Violations  
**Migration Window**: Week 3+ (Optional)
**Risk Level**: LOW - Development tools only

### 20-25. Testing Files (6 violations)
- **Files**: BreedingSystemIntegrationTest.cs (3), DIGameManagerCoreTests.cs (1)
- **System Impact**: LOW - Test infrastructure
- **Business Impact**: None - development only
- **Migration Complexity**: Simple - test doubles/mocks
- **Dependencies**: Independent
- **Migration Order**: #20-25 (OPTIONAL)

### 26. Editor Scripts (1 violation)  
- **File**: Editor/CreateModeChangedEventAsset.cs
- **System Impact**: LOW - Editor tooling
- **Business Impact**: None - development only
- **Migration Complexity**: Simple - editor service pattern
- **Dependencies**: Independent
- **Migration Order**: #26 (OPTIONAL)

---

## MIGRATION EXECUTION PLAN

### Phase 1: Critical Infrastructure (Days 3-4)
1. **ManagerRegistry.cs** - Replace with ServiceContainer.GetAll<ChimeraManager>()
2. **SimpleManagerRegistry.cs** - Unify with main ServiceContainer  
3. **GameSystemInitializer.cs** - Inject dependency instead of finding

### Phase 2: High Priority Systems (Day 5 - Week 2 Day 3)
4. **Construction System** (6 violations) - Service injection pattern
5. **Camera System** (2 violations) - Camera service registration  
6. **Environmental/Gameplay** (4 violations) - Plant registry + service injection

### Phase 3: Medium Priority Systems (Week 2 Days 4-5)  
7. **Performance Systems** (4 violations) - Registry pattern or caching

### Phase 4: Optional Cleanup (Week 3+)
8. **Testing & Editor** (7 violations) - Test patterns and editor services

## SUCCESS METRICS
- **Tier 1**: Zero game boot failures
- **Tier 2**: All core gameplay systems functional via DI
- **Tier 3**: Performance monitoring restored
- **Tier 4**: Clean development environment

## ROLLBACK CHECKPOINTS
- After each Tier completion
- Before each file modification
- Daily Git commits with working state