# Rollback Points & Migration Strategy - Risk Mitigation Framework
**Analysis Date**: $(date)
**Migration Scope**: 28 violations across 4 phases
**Risk Level**: CRITICAL - Core system modifications with cascading dependencies

## ROLLBACK STRATEGY FRAMEWORK

### **Rollback Principles**:
1. **Git-based checkpoints** at every milestone
2. **Functional verification** before proceeding
3. **Isolation boundaries** to contain failures
4. **Automated rollback detection** where possible
5. **Documentation** of rollback procedures

---

## **PHASE 1: CRITICAL INFRASTRUCTURE ROLLBACK POINTS**
**Timeline**: Week 1, Days 3-4
**Risk Level**: EXTREME (system boot failures possible)

### **Rollback Point 1A: Pre-ManagerRegistry Migration**
**Git Tag**: `pre-manager-registry-migration`
**Location**: Before modifying `ManagerRegistry.cs:61`
**Trigger Conditions**:
- ServiceContainer.ResolveAll<ChimeraManager>() method doesn't exist
- Build failures after modification
- Runtime exceptions during manager discovery

**Rollback Procedure**:
```bash
git reset --hard pre-manager-registry-migration
# Restore working state
git clean -fd
# Verify build compiles
dotnet build # or Unity build
```

**Critical Files to Backup**:
- `Assets/ProjectChimera/Core/ManagerRegistry.cs`
- `Assets/ProjectChimera/Core/ServiceContainer.cs` (verify ResolveAll method)

**Validation Before Proceeding**:
- [ ] ServiceContainer has ResolveAll<T>() method
- [ ] All managers still register correctly in scene
- [ ] No null reference exceptions on startup
- [ ] Build compiles successfully

### **Rollback Point 1B: Post-ManagerRegistry, Pre-SimpleManagerRegistry**
**Git Tag**: `manager-registry-migrated`
**Location**: After ManagerRegistry migration, before SimpleManagerRegistry
**Trigger Conditions**:
- ManagerRegistry migration successful ✅
- SimpleManagerRegistry conflicts with main registry
- DI container unification issues

**Rollback Procedure**:
```bash
# Rollback only SimpleManagerRegistry if ManagerRegistry works
git checkout HEAD -- Assets/ProjectChimera/Core/SimpleDI/SimpleManagerRegistry.cs
# OR full phase rollback if both systems conflict
git reset --hard pre-manager-registry-migration
```

**Critical Files to Backup**:
- `Assets/ProjectChimera/Core/SimpleDI/SimpleManagerRegistry.cs`
- Any ServiceLocator integration files

**Validation Before Proceeding**:
- [ ] Both registry systems coexist without conflicts
- [ ] DI container unified approach working
- [ ] No duplicate manager registrations
- [ ] Performance acceptable (no major slowdowns)

### **Rollback Point 1C: Post-Core Infrastructure**  
**Git Tag**: `core-infrastructure-complete`
**Location**: After all 3 core migrations (#1-3) complete
**Trigger Conditions**:
- All core migrations successful ✅
- System initialization works correctly
- Ready to proceed to construction systems

**Rollback Procedure**:
```bash
# This is a MAJOR checkpoint - only rollback if catastrophic failure
git reset --hard pre-manager-registry-migration
# Re-run entire Phase 1 with fixes
```

**Critical Files to Backup**:
- `Assets/ProjectChimera/Core/ManagerRegistry.cs`
- `Assets/ProjectChimera/Core/SimpleDI/SimpleManagerRegistry.cs`
- `Assets/ProjectChimera/Core/GameSystemInitializer.cs`

**Validation Before Proceeding**:
- [ ] All 3 core files migrated successfully
- [ ] System boots without errors
- [ ] All managers initialize correctly
- [ ] GameSystemInitializer completes all phases
- [ ] Performance within acceptable range
- [ ] No memory leaks detected

---

## **PHASE 2: CONSTRUCTION SYSTEM ROLLBACK POINTS**
**Timeline**: Week 1, Day 5
**Risk Level**: HIGH (construction functionality failures)

### **Rollback Point 2A: Pre-Construction Migration**
**Git Tag**: `pre-construction-migration`
**Location**: Before any construction system changes
**Dependencies**: Phase 1 must be complete and stable

**Rollback Procedure**:
```bash
# Reset only construction files if core is stable
git checkout HEAD -- Assets/ProjectChimera/Systems/Construction/
git checkout HEAD -- Assets/ProjectChimera/Systems/Save/ConstructionSaveProvider.cs
```

**Critical Files to Backup**:
- `Assets/ProjectChimera/Systems/Construction/GridInputHandler.cs`
- `Assets/ProjectChimera/Systems/Save/ConstructionSaveProvider.cs` 
- `Assets/ProjectChimera/Systems/Construction/PlacementPaymentService.cs`
- `Assets/ProjectChimera/Systems/Construction/Payment/RefactoredPlacementPaymentService.cs`

### **Rollback Point 2B: Post-GridInputHandler Migration**
**Git Tag**: `gridinputhandler-migrated`
**Location**: After GridInputHandler migration (#4)
**Validation Requirements**:
- [ ] Construction input still works (mouse clicks register)
- [ ] Camera service resolves correctly
- [ ] No null reference exceptions in construction mode

**Rollback Procedure**:
```bash
# Selective rollback of just GridInputHandler if other migrations pending
git checkout HEAD -- Assets/ProjectChimera/Systems/Construction/GridInputHandler.cs
```

### **Rollback Point 2C: Pre-Payment Services Migration**
**Git Tag**: `pre-payment-services-migration`
**Location**: Before migrating payment services (#6-9)
**Prerequisites**: Currency/Trading managers must be registered in ServiceContainer

**Critical Validation**:
- [ ] ICurrencyManager registered in ServiceContainer
- [ ] ITradingManager registered in ServiceContainer
- [ ] Services resolve without GameObject.Find()

**Rollback Procedure**:
```bash
git checkout HEAD -- Assets/ProjectChimera/Systems/Construction/PlacementPaymentService.cs
git checkout HEAD -- Assets/ProjectChimera/Systems/Construction/Payment/RefactoredPlacementPaymentService.cs
```

### **Rollback Point 2D: Post-Construction Complete**
**Git Tag**: `construction-system-migrated`
**Location**: After all construction migrations (#4-9) complete
**Success Criteria**: Full construction workflow functional via DI

---

## **PHASE 3: CAMERA & ENVIRONMENTAL ROLLBACK POINTS**
**Timeline**: Week 2, Days 1-3
**Risk Level**: MEDIUM (UI/visualization issues)

### **Rollback Point 3A: Pre-Camera Migration**
**Git Tag**: `pre-camera-migration`
**Location**: Before camera system changes (#10-11)

**Rollback Procedure**:
```bash
git checkout HEAD -- Assets/ProjectChimera/Systems/Camera/
```

### **Rollback Point 3B: Pre-Environmental Migration** 
**Git Tag**: `pre-environmental-migration`
**Location**: Before environmental system changes (#12-15)
**Prerequisites**: PlantRegistryService implementation required for #13-15

**Critical Validation**:
- [ ] IPlantRegistryService implemented and registered
- [ ] Plant registration/unregistration events working
- [ ] No performance degradation from plant tracking

**Rollback Procedure**:
```bash
git checkout HEAD -- Assets/ProjectChimera/Systems/Gameplay/
git checkout HEAD -- Assets/ProjectChimera/Systems/Diagnostics/DebugOverlayManager.cs
```

---

## **PHASE 4: PERFORMANCE SYSTEMS ROLLBACK POINTS**
**Timeline**: Week 2, Days 4-5  
**Risk Level**: LOW (performance monitoring only)

### **Rollback Point 4A: Pre-Performance Migration**
**Git Tag**: `pre-performance-migration`
**Location**: Before performance system changes (#16-19)
**Prerequisites**: RendererRegistryService implementation required

**Rollback Procedure**:
```bash
git checkout HEAD -- Assets/ProjectChimera/Systems/Performance/
git checkout HEAD -- Assets/ProjectChimera/Systems/Services/SpeedTree/Environmental/WindSystem.cs
```

---

## **AUTOMATED ROLLBACK DETECTION**

### **Build System Integration**:
```bash
#!/bin/bash
# migration_rollback_check.sh
# Automated validation script to run after each migration

validate_migration() {
    local migration_name=$1
    local git_tag=$2
    
    echo "🔍 Validating migration: $migration_name"
    
    # Build check
    if ! dotnet build --no-restore --verbosity quiet; then
        echo "❌ Build failed - triggering rollback to $git_tag"
        git reset --hard $git_tag
        exit 1
    fi
    
    # Basic runtime check (if tests available)
    if [ -f "run_basic_tests.sh" ]; then
        if ! ./run_basic_tests.sh; then
            echo "❌ Runtime tests failed - triggering rollback to $git_tag"
            git reset --hard $git_tag
            exit 1
        fi
    fi
    
    echo "✅ Migration validated: $migration_name"
}

# Usage after each migration:
# validate_migration "ManagerRegistry" "pre-manager-registry-migration"
```

### **Unity-Specific Validation**:
```csharp
// ValidationHelper.cs - Unity Editor script
public static class MigrationValidator
{
    [MenuItem("Chimera/Validate Migration")]
    public static bool ValidateCurrentMigration()
    {
        var errors = new List<string>();
        
        // Check for null reference exceptions
        if (!ValidateServiceResolution(errors))
            return LogErrorsAndFail(errors);
            
        // Check for missing service registrations  
        if (!ValidateRequiredServices(errors))
            return LogErrorsAndFail(errors);
            
        // Check for performance degradation
        if (!ValidatePerformance(errors))
            return LogErrorsAndFail(errors);
            
        Debug.Log("✅ Migration validation passed");
        return true;
    }
    
    private static bool ValidateServiceResolution(List<string> errors)
    {
        try
        {
            var container = ServiceContainerFactory.Instance;
            if (container == null)
            {
                errors.Add("ServiceContainer not available");
                return false;
            }
            
            // Test critical service resolution
            container.Resolve<IManagerRegistry>();
            container.ResolveAll<ChimeraManager>();
            
            return true;
        }
        catch (System.Exception ex)
        {
            errors.Add($"Service resolution failed: {ex.Message}");
            return false;
        }
    }
}
```

---

## **ROLLBACK DECISION MATRIX**

| Failure Type | Rollback Scope | Git Command | Recovery Time |
|--------------|----------------|-------------|---------------|
| **Build Failure** | Single File | `git checkout HEAD -- <file>` | < 5 minutes |
| **Runtime Exception** | Single Migration | `git reset --hard <tag>` | < 15 minutes |
| **Service Resolution Failure** | Phase Rollback | `git reset --hard <phase-tag>` | < 30 minutes |
| **System Boot Failure** | Full Phase 1 Rollback | `git reset --hard pre-manager-registry-migration` | < 1 hour |
| **Cascading Failures** | Complete Rollback | `git reset --hard <initial-tag>` | < 2 hours |

---

## **COMMUNICATION PLAN**

### **Rollback Notification**:
```bash
# rollback_notification.sh
notify_rollback() {
    local migration=$1
    local reason=$2
    local tag=$3
    
    echo "🚨 ROLLBACK TRIGGERED: $migration"
    echo "Reason: $reason"
    echo "Rolled back to: $tag"
    echo "Status: System restored to working state"
    
    # Log to file for tracking
    echo "$(date): ROLLBACK - $migration - $reason - $tag" >> migration_log.txt
}
```

### **Success Documentation**:
- Each successful migration gets documented with validation results
- Performance metrics captured before/after each phase
- Service registration state documented at each checkpoint

---

## **ROLLBACK TESTING PLAN**

### **Pre-Migration Rollback Drills**:
1. **Practice rollback procedures** on non-critical branches
2. **Validate restoration time** for each rollback point  
3. **Test automated rollback detection** scripts
4. **Verify backup integrity** before starting migrations

### **Post-Rollback Validation**:
1. **Full system functionality check**
2. **Performance baseline restoration** 
3. **Service registration state verification**
4. **Build and runtime stability confirmation**

This comprehensive rollback framework ensures safe migration execution with multiple recovery points and automated failure detection.