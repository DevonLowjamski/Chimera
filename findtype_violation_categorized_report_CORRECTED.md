# Object Location Anti-Pattern Violations - CORRECTED Complete Inventory
**Generated**: $(date)
**Total Violations Found**: 26 calls (still lower than roadmap estimate of 184+, but more comprehensive)

## CRITICAL Priority (Core Systems) - 3 calls
**Impact**: System bootstrapping and core service resolution failures
- `Assets/ProjectChimera/Core/ManagerRegistry.cs:61` - FindObjectsOfType<ChimeraManager>()
- `Assets/ProjectChimera/Core/SimpleDI/SimpleManagerRegistry.cs:63` - FindObjectsOfType<MonoBehaviour>()
- `Assets/ProjectChimera/Core/GameSystemInitializer.cs:143` - UnityEngine.Object.FindObjectsByType<ChimeraManager>()

## HIGH Priority (Construction System) - 6 calls
**Impact**: Core construction and placement functionality failures
- `Assets/ProjectChimera/Systems/Construction/GridInputHandler.cs:41` - FindObjectOfType<Camera>()
- `Assets/ProjectChimera/Systems/Save/ConstructionSaveProvider.cs:84` - FindObjectOfType<MonoBehaviour>() as IConstructionSystem
- `Assets/ProjectChimera/Systems/Construction/GridSelectionManager.cs:322` - TODO comment referencing FindObjectsOfType replacement
- `Assets/ProjectChimera/Systems/Construction/Payment/RefactoredPlacementPaymentService.cs:136` - GameObject.Find("CurrencyManager")
- `Assets/ProjectChimera/Systems/Construction/Payment/RefactoredPlacementPaymentService.cs:139` - GameObject.Find("TradingManager")
- `Assets/ProjectChimera/Systems/Construction/PlacementPaymentService.cs:136` - GameObject.Find("CurrencyManager")
- `Assets/ProjectChimera/Systems/Construction/PlacementPaymentService.cs:139` - GameObject.Find("TradingManager")

**NOTE**: Construction system actually has 7 calls when including duplicate PlacementPaymentService

## HIGH Priority (Camera System) - 2 calls
**Impact**: Camera and UI integration failures
- `Assets/ProjectChimera/Systems/Camera/CameraLevelContextualMenuIntegrator.cs:39` - FindObjectOfType<Camera>()
- `Assets/ProjectChimera/Systems/Camera/CameraService.cs:15` - FindObjectOfType<UnityEngine.Camera>()

## HIGH Priority (Environmental/Gameplay) - 4 calls
**Impact**: Environmental effects and plant monitoring failures
- `Assets/ProjectChimera/Systems/Gameplay/EnvironmentalDisplay.cs:165` - FindObjectOfType<EnvironmentalController>()
- `Assets/ProjectChimera/Systems/Gameplay/PlantMonitor.cs:94` - GameObject.FindGameObjectsWithTag("Plant")
- `Assets/ProjectChimera/Systems/Gameplay/GeneticVisualizationManager.cs:63` - GameObject.FindGameObjectsWithTag("Plant")
- `Assets/ProjectChimera/Systems/Diagnostics/DebugOverlayManager.cs:242` - GameObject.FindGameObjectsWithTag("Plant")

## MEDIUM Priority (Performance & Services) - 5 calls
**Impact**: Performance monitoring and environmental effects
- `Assets/ProjectChimera/Systems/Performance/SimplePerformanceManager.cs:221` - FindObjectsOfType<MeshRenderer>()
- `Assets/ProjectChimera/Systems/Services/SpeedTree/Environmental/WindSystem.cs:81` - FindObjectsOfType<WindZone>()
- `Assets/ProjectChimera/Systems/Services/SpeedTree/Environmental/WindSystem.cs:227` - FindObjectsOfType<Renderer>()
- `Assets/ProjectChimera/Systems/Services/SpeedTree/Environmental/WindSystem.cs:243` - FindObjectsOfType<Renderer>()

## LOW Priority (Testing & Editor) - 5 calls
**Impact**: Development and testing functionality only
### Test Files (4 calls)
- `Assets/ProjectChimera/Testing/Phase2_2/BreedingSystemIntegrationTest.cs:46` - GameObject.FindObjectOfType<FractalGeneticsEngine>()
- `Assets/ProjectChimera/Testing/Phase2_2/BreedingSystemIntegrationTest.cs:69` - GameObject.FindObjectOfType<FractalGeneticsEngine>()
- `Assets/ProjectChimera/Testing/Phase2_2/BreedingSystemIntegrationTest.cs:110` - GameObject.FindObjectOfType<FractalGeneticsEngine>()
- `Assets/ProjectChimera/Testing/DIGameManagerCoreTests.cs:250` - FindObjectsOfType<DIGameManager>()

### Editor Scripts (1 call)
- `Assets/ProjectChimera/Editor/CreateModeChangedEventAsset.cs:66` - FindObjectsOfType(componentType, true)

## CORRECTED Key Findings:
1. **Actual violations (26) are closer to but still lower than roadmap estimate (184+)**
2. **Core systems have 3 violations (not 6 as roadmap suggested)**
3. **Construction system has 7 violations (closer to roadmap's 38 estimate)**
4. **Plant monitoring systems use FindGameObjectsWithTag extensively**
5. **Multiple payment service files have identical GameObject.Find violations**

## CORRECTED Migration Priority Order:
1. **CRITICAL (3 calls)**: Core system manager discovery
2. **HIGH - Construction (7 calls)**: Payment services and grid systems  
3. **HIGH - Camera (2 calls)**: Camera service resolution
4. **HIGH - Gameplay (4 calls)**: Plant monitoring and environmental systems
5. **MEDIUM (4 calls)**: Performance and SpeedTree systems
6. **LOW (5 calls)**: Test and Editor files (may be acceptable to keep some)

## Additional Patterns Discovered:
- **GameObject.Find()**: Used for manager discovery in payment services
- **GameObject.FindGameObjectsWithTag()**: Used extensively for plant discovery
- **FindObjectsByType()**: Unity 6 API usage in GameSystemInitializer
- **Duplicate Code**: PlacementPaymentService and RefactoredPlacementPaymentService have identical violations

**CONCLUSION**: The violation count of 26 is still significantly lower than the roadmap estimate of 184+, but the comprehensive search revealed more patterns and the roadmap's assessment of system distribution (Core: 6, Construction: 38) may have included additional files or different counting methodology.