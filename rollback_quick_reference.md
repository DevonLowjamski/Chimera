# Rollback Quick Reference - Decision Tree & Checklists

## **IMMEDIATE ROLLBACK DECISION TREE**

```
Migration Failure Detected
         ↓
    Build Failing?
         ├─ YES → Single File Rollback → `git checkout HEAD -- <file>`
         └─ NO
             ↓
    Runtime Exception?
         ├─ YES → Check Scope
         │        ├─ Single System → Migration Rollback → `git reset --hard <migration-tag>`
         │        └─ Multiple Systems → Phase Rollback → `git reset --hard <phase-tag>`
         └─ NO
             ↓
    Service Resolution Issues?
         ├─ YES → Phase Rollback → `git reset --hard <phase-tag>`
         └─ NO
             ↓
    System Won't Boot?
         ├─ YES → CRITICAL ROLLBACK → `git reset --hard pre-manager-registry-migration`
         └─ NO → Log Issue & Continue with Caution
```

## **ROLLBACK COMMANDS CHEAT SHEET**

| Scenario | Command | Recovery Time |
|----------|---------|---------------|
| **Single file broken** | `git checkout HEAD -- <filepath>` | 1-2 min |
| **Migration #1 failed** | `git reset --hard pre-manager-registry-migration` | 5 min |
| **Migration #2 failed** | `git reset --hard manager-registry-migrated` | 5 min |
| **Migration #3 failed** | `git reset --hard manager-registry-migrated` | 5 min |
| **Phase 1 complete failure** | `git reset --hard pre-manager-registry-migration` | 10 min |
| **Construction system failed** | `git reset --hard core-infrastructure-complete` | 5 min |
| **Camera system failed** | `git reset --hard construction-system-migrated` | 3 min |
| **Environmental failed** | `git reset --hard pre-environmental-migration` | 3 min |
| **Performance failed** | `git reset --hard pre-performance-migration` | 2 min |
| **CATASTROPHIC FAILURE** | `git reset --hard <initial-state-tag>` | 15 min |

## **PRE-MIGRATION VALIDATION CHECKLIST**

### **Before Each Migration**:
- [ ] Current build compiles successfully
- [ ] Git working directory is clean (`git status`)
- [ ] Backup tag created (`git tag <backup-name>`)
- [ ] Required services are registered (if applicable)
- [ ] Automated tests pass (if available)

### **Specific Pre-Migration Checks**:

#### **Before Migration #1 (ManagerRegistry)**:
- [ ] `ServiceContainer.ResolveAll<T>()` method exists
- [ ] ServiceContainerFactory.Instance is available
- [ ] No existing null reference exceptions during startup

#### **Before Migration #2 (SimpleManagerRegistry)**:
- [ ] Migration #1 completed successfully
- [ ] ServiceLocator integration is working
- [ ] No duplicate manager registrations

#### **Before Migration #3 (GameSystemInitializer)**:
- [ ] Migration #1 completed successfully (ManagerRegistry functional)
- [ ] Manager discovery working through ServiceContainer
- [ ] No initialization sequence failures

#### **Before Construction Migrations (#4-9)**:
- [ ] Phase 1 (Migrations #1-3) complete and stable
- [ ] Camera service available for injection
- [ ] Currency/Trading managers registered in ServiceContainer

#### **Before Plant Monitoring (#13-15)**:
- [ ] IPlantRegistryService implemented and registered
- [ ] Plant tracking events functional
- [ ] Performance acceptable with plant registry

## **POST-MIGRATION VALIDATION CHECKLIST**

### **After Each Migration**:
- [ ] Build compiles without errors
- [ ] No new runtime exceptions
- [ ] Target system functionality works
- [ ] Performance within acceptable range
- [ ] Git commit created with clear message

### **Specific Post-Migration Validations**:

#### **After Migration #1 (ManagerRegistry)**:
- [ ] All managers discovered via ServiceContainer
- [ ] No FindObjectsOfType calls remain in ManagerRegistry.cs
- [ ] Manager initialization sequence unchanged
- [ ] Performance impact minimal

#### **After Migration #2 (SimpleManagerRegistry)**:
- [ ] DI systems unified without conflicts
- [ ] No duplicate service registrations
- [ ] ServiceLocator integration maintained

#### **After Migration #3 (GameSystemInitializer)**:
- [ ] System initialization completes all phases
- [ ] All discovery phases work correctly
- [ ] No manager initialization failures

#### **After Construction Migrations**:
- [ ] Construction mode functional (click to place)
- [ ] Payment processing works
- [ ] Save/load construction state works
- [ ] Camera integration functional

## **EMERGENCY ROLLBACK PROCEDURES**

### **CRITICAL SYSTEM FAILURE** (System won't boot):
```bash
# IMMEDIATE ACTION - Don't investigate, just restore
git reset --hard pre-manager-registry-migration
git clean -fd
echo "CRITICAL ROLLBACK EXECUTED - System restored" >> emergency_log.txt

# Then investigate from stable state
```

### **BUILD BROKEN** (Compilation errors):
```bash
# Quick file restoration
git checkout HEAD -- <failing-file>
# Or if multiple files affected:
git reset --hard <last-working-tag>
```

### **RUNTIME EXCEPTIONS** (Service resolution failures):
```bash
# Reset to last stable migration point
git reset --hard <appropriate-rollback-tag>
# Validate services are working:
# - Check ServiceContainer registration
# - Verify interface implementations
```

## **VALIDATION SCRIPTS**

### **Quick Health Check** (Run after each migration):
```bash
#!/bin/bash
echo "🔍 Quick Migration Health Check"

# Build check
if dotnet build --verbosity quiet > /dev/null 2>&1; then
    echo "✅ Build: PASS"
else
    echo "❌ Build: FAIL - Consider rollback"
    exit 1
fi

# Basic Unity validation (if available)
if [ -f "validate_unity.sh" ]; then
    if ./validate_unity.sh > /dev/null 2>&1; then
        echo "✅ Unity: PASS"
    else
        echo "❌ Unity: FAIL - Check console for errors"
    fi
fi

echo "✅ Health check complete"
```

### **Service Container Validation**:
```csharp
// Quick validation in Unity Console
ServiceContainerFactory.Instance?.GetRegisteredTypes().ToList().ForEach(t => 
    Debug.Log($"Registered: {t.Name}"));

// Validate critical services
var criticalServices = new[] { typeof(IManagerRegistry), typeof(IGridSystem) };
foreach(var service in criticalServices) {
    if (ServiceContainerFactory.Instance.IsRegistered(service))
        Debug.Log($"✅ {service.Name}");
    else
        Debug.LogError($"❌ {service.Name} NOT REGISTERED");
}
```

## **ROLLBACK SUCCESS CRITERIA**

After any rollback, verify:
- [ ] **System boots** without exceptions
- [ ] **Core gameplay** functional (can enter construction mode)
- [ ] **Managers initialize** correctly 
- [ ] **Build compiles** successfully
- [ ] **Performance** returned to baseline
- [ ] **Git state** clean and tagged

## **ESCALATION PROCEDURES**

If rollback fails or doesn't resolve issues:
1. **Document** the failure state and attempted solutions
2. **Create** emergency branch from current state
3. **Reset** to last known good state (potentially initial state)
4. **Analyze** root cause before retrying migration
5. **Consider** breaking migration into smaller steps