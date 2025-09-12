# FindObjectOfType Violations - Complete Inventory & Categorization
**Generated**: $(date)
**Total Violations Found**: 18 calls (significantly lower than roadmap estimate of 184+)

## Critical Priority (Core Systems) - 2 calls
**Impact**: System bootstrapping and core service resolution failures
- `Assets/ProjectChimera/Core/ManagerRegistry.cs:61` - FindObjectsOfType<ChimeraManager>()
- `Assets/ProjectChimera/Core/SimpleDI/SimpleManagerRegistry.cs:63` - FindObjectsOfType<MonoBehaviour>()

## High Priority (Gameplay Systems) - 6 calls
**Impact**: Core gameplay functionality failures

### Construction System (3 calls)
- `Assets/ProjectChimera/Systems/Construction/GridInputHandler.cs:41` - FindObjectOfType<Camera>()
- `Assets/ProjectChimera/Systems/Save/ConstructionSaveProvider.cs:84` - FindObjectOfType<MonoBehaviour>() as IConstructionSystem
- `Assets/ProjectChimera/Systems/Construction/GridSelectionManager.cs:322` - TODO comment referencing FindObjectsOfType replacement

### Camera System (2 calls)
- `Assets/ProjectChimera/Systems/Camera/CameraLevelContextualMenuIntegrator.cs:39` - FindObjectOfType<Camera>()
- `Assets/ProjectChimera/Systems/Camera/CameraService.cs:15` - FindObjectOfType<UnityEngine.Camera>()

### Environmental System (1 call)
- `Assets/ProjectChimera/Systems/Gameplay/EnvironmentalDisplay.cs:165` - FindObjectOfType<EnvironmentalController>()

## Medium Priority (Performance & Services) - 5 calls
**Impact**: Performance monitoring and environmental effects

### Performance System (1 call)
- `Assets/ProjectChimera/Systems/Performance/SimplePerformanceManager.cs:221` - FindObjectsOfType<MeshRenderer>()

### SpeedTree Environmental System (4 calls)
- `Assets/ProjectChimera/Systems/Services/SpeedTree/Environmental/WindSystem.cs:81` - FindObjectsOfType<WindZone>()
- `Assets/ProjectChimera/Systems/Services/SpeedTree/Environmental/WindSystem.cs:227` - FindObjectsOfType<Renderer>()
- `Assets/ProjectChimera/Systems/Services/SpeedTree/Environmental/WindSystem.cs:243` - FindObjectsOfType<Renderer>()

## Low Priority (Testing & Editor) - 5 calls
**Impact**: Development and testing functionality only

### Test Files (4 calls)
- `Assets/ProjectChimera/Testing/Phase2_2/BreedingSystemIntegrationTest.cs:46` - FindObjectOfType<FractalGeneticsEngine>()
- `Assets/ProjectChimera/Testing/Phase2_2/BreedingSystemIntegrationTest.cs:69` - FindObjectOfType<FractalGeneticsEngine>()
- `Assets/ProjectChimera/Testing/Phase2_2/BreedingSystemIntegrationTest.cs:110` - FindObjectOfType<FractalGeneticsEngine>()
- `Assets/ProjectChimera/Testing/DIGameManagerCoreTests.cs:250` - FindObjectsOfType<DIGameManager>()

### Editor Scripts (1 call)
- `Assets/ProjectChimera/Editor/CreateModeChangedEventAsset.cs:66` - FindObjectsOfType(componentType, true)

## Key Findings:
1. **Actual violations (18) are significantly lower than roadmap estimate (184+)**
2. **Core systems have minimal violations (2 calls)**
3. **Most violations are in SpeedTree environmental system and testing**
4. **Several files have TODO comments indicating awareness of needed changes**

## Migration Priority Order:
1. **Critical**: Core/ManagerRegistry.cs and Core/SimpleDI/SimpleManagerRegistry.cs
2. **High**: Construction and Camera systems
3. **Medium**: Performance and SpeedTree systems  
4. **Low**: Test and Editor files (may be acceptable to keep some)

## Notes:
- Several violations appear to be in test files which may be acceptable
- WindSystem has multiple violations that could be consolidated
- Some files already have TODO comments acknowledging the need for DI migration