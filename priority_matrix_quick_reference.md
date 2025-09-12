# Priority Matrix - Quick Reference Table

| Tier | Priority | File | Line | Pattern | Impact | Complexity | Order |
|------|----------|------|------|---------|---------|------------|-------|
| **1** | CRITICAL | ManagerRegistry.cs | 61 | FindObjectsOfType\<ChimeraManager\>() | Game-breaking | Moderate | #1 |
| **1** | CRITICAL | SimpleManagerRegistry.cs | 63 | FindObjectsOfType\<MonoBehaviour\>() | Game-breaking | Moderate | #2 |
| **1** | CRITICAL | GameSystemInitializer.cs | 143 | FindObjectsByType\<ChimeraManager\>() | Game-breaking | Simple | #3 |
| **2A** | HIGH | GridInputHandler.cs | 41 | FindObjectOfType\<Camera\>() | Feature-breaking | Simple | #4 |
| **2A** | HIGH | ConstructionSaveProvider.cs | 84 | FindObjectOfType\<MonoBehaviour\>() | Feature-breaking | Moderate | #5 |
| **2A** | HIGH | RefactoredPlacementPaymentService.cs | 136 | GameObject.Find("CurrencyManager") | Feature-breaking | Simple | #6 |
| **2A** | HIGH | RefactoredPlacementPaymentService.cs | 139 | GameObject.Find("TradingManager") | Feature-breaking | Simple | #7 |
| **2A** | HIGH | PlacementPaymentService.cs | 136 | GameObject.Find("CurrencyManager") | Feature-breaking | Simple | #8 |
| **2A** | HIGH | PlacementPaymentService.cs | 139 | GameObject.Find("TradingManager") | Feature-breaking | Simple | #9 |
| **2B** | HIGH | CameraLevelContextualMenuIntegrator.cs | 39 | FindObjectOfType\<Camera\>() | Feature-breaking | Simple | #10 |
| **2B** | HIGH | CameraService.cs | 15 | FindObjectOfType\<Camera\>() | Feature-breaking | Simple | #11 |
| **2C** | HIGH | EnvironmentalDisplay.cs | 165 | FindObjectOfType\<EnvironmentalController\>() | Feature-breaking | Simple | #12 |
| **2C** | HIGH | PlantMonitor.cs | 94 | FindGameObjectsWithTag("Plant") | Feature-breaking | Moderate | #13 |
| **2C** | HIGH | GeneticVisualizationManager.cs | 63 | FindGameObjectsWithTag("Plant") | Feature-breaking | Moderate | #14 |
| **2C** | HIGH | DebugOverlayManager.cs | 242 | FindGameObjectsWithTag("Plant") | Feature-breaking | Moderate | #15 |
| **3** | MEDIUM | SimplePerformanceManager.cs | 221 | FindObjectsOfType\<MeshRenderer\>() | Performance | Complex | #16 |
| **3** | MEDIUM | WindSystem.cs | 81 | FindObjectsOfType\<WindZone\>() | Performance | Complex | #17 |
| **3** | MEDIUM | WindSystem.cs | 227 | FindObjectsOfType\<Renderer\>() | Performance | Complex | #18 |
| **3** | MEDIUM | WindSystem.cs | 243 | FindObjectsOfType\<Renderer\>() | Performance | Complex | #19 |
| **4** | LOW | BreedingSystemIntegrationTest.cs | 46 | GameObject.FindObjectOfType\<...\>() | None | Simple | #20 |
| **4** | LOW | BreedingSystemIntegrationTest.cs | 69 | GameObject.FindObjectOfType\<...\>() | None | Simple | #21 |
| **4** | LOW | BreedingSystemIntegrationTest.cs | 110 | GameObject.FindObjectOfType\<...\>() | None | Simple | #22 |
| **4** | LOW | DIGameManagerCoreTests.cs | 250 | FindObjectsOfType\<DIGameManager\>() | None | Simple | #23 |
| **4** | LOW | CreateModeChangedEventAsset.cs | 66 | FindObjectsOfType(componentType) | None | Simple | #24 |

## Summary Statistics:
- **CRITICAL (Tier 1)**: 3 violations - Must fix first
- **HIGH (Tier 2)**: 12 violations - Core gameplay systems
- **MEDIUM (Tier 3)**: 4 violations - Performance systems  
- **LOW (Tier 4)**: 5 violations - Development tools (optional)
- **Total**: 28 violations

## Migration Timeline:
- **Week 1**: Tiers 1 & 2A (Critical + Construction) = 9 violations
- **Week 2**: Tiers 2B & 2C (Camera + Environmental) = 6 violations  
- **Week 2 End**: Tier 3 (Performance) = 4 violations
- **Week 3+**: Tier 4 (Optional cleanup) = 5 violations