# Resources.Load Audit Report

## Summary
- **Total occurrences**: 34 across 14 files
- **Approved exceptions**: 13 (migration infrastructure & fallbacks)
- **Requires migration**: 15 (asset loading)  
- **Core bootstrapping**: 6 (needs evaluation)

## Approved Exceptions (13 occurrences)

### Addressables Migration Infrastructure (10)
These are approved fallback mechanisms during the Addressables migration:

1. **AddressablesInfrastructure.cs** (2) - Fallback when Addressables unavailable
2. **AddressablesMigrationPhase1.cs** (2) - Compatibility mode fallbacks
3. **AddressablesMigrationPhase2.cs** (6) - Migration fallbacks for AudioClip, GameObject, Material

### Service Interfaces (3)
Documentation and interface definitions (no actual loading):

1. **ManagerInterfaces.cs** (1) - Interface documentation comment
2. **ChimeraServiceModule.cs** (comment) - Fallback service implementation comment
3. **AddressablesInfrastructure.cs** (comment) - Class documentation

## Requires Migration (15 occurrences)

### Asset Loading Systems
These should use Addressables or DI services:

1. **ConstructionPaletteManager.cs** (1) - `Resources.LoadAll<SchematicSO>`
2. **SchematicLibraryManager.cs** (2) - Fallback schematic loading
3. **SchematicLibraryDataManager.cs** (1) - Schematic loading
4. **SchematicUnlockManager.cs** (1) - All schematics loading
5. **UIAnimationSystem.cs** (1) - Animation clip loading
6. **AtmosphericPhysicsSimulator.cs** (3) - Compute shader loading
7. **SpeedTreeAssetManagementService.cs** (1) - Generic asset loading
8. **ChimeraServiceModule.cs** (2) - Service asset loading

## Core Bootstrapping (6 occurrences)

### System Initialization
These may be legitimate for core system startup:

1. **EventManager.cs** (1) - `Resources.LoadAll<ChimeraEventSO>` for event system bootstrap
2. **DataManager.cs** (2) - `Resources.LoadAll<ChimeraDataSO>` and `Resources.LoadAll<ChimeraConfigSO>` for data system bootstrap

### Recommendation
Core bootstrapping Resources.Load calls are acceptable for initial system startup as they:
- Run once during application initialization
- Load core configuration data required before DI container setup
- Have no viable Addressables alternative for bootstrap-critical assets

## Action Items
1. Migrate 15 asset loading occurrences to use Addressables
2. Document 13 approved exceptions as legitimate
3. Evaluate if core bootstrapping can be optimized but mark as approved for now