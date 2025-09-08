# Project Chimera Quality Gates

This directory contains automated quality assurance tools that prevent architectural anti-patterns from being introduced into the codebase.

## 🚨 Quality Gate Rules

### 1. FindObjectOfType Violations
- **Rule**: No `FindObjectOfType<T>()` calls in runtime assemblies
- **Reason**: Creates tight coupling and breaks dependency injection patterns
- **Fix**: Use `ServiceContainer.Resolve<T>()` instead

### 2. Resources.Load Violations  
- **Rule**: No `Resources.Load()` calls
- **Reason**: Non-performant asset loading, blocks main thread
- **Fix**: Use Addressables or direct asset references

### 3. Raw Debug.Log Violations
- **Rule**: No raw `Debug.Log()` calls outside `Systems/Diagnostics`
- **Reason**: Logs in production builds, no conditional compilation
- **Fix**: Use `ChimeraLogger.Log()` for conditional compilation

### 4. GetComponent in Lifecycle (Warning)
- **Rule**: Avoid `GetComponent()` in `Awake/Start/Update` methods
- **Reason**: Performance impact, repeated reflection calls
- **Fix**: Cache references or use dependency injection

## 🔧 Setup Instructions

### For New Contributors

1. **Install git hooks** (run once after cloning):
```bash
.github/scripts/setup-hooks.sh
```

This installs pre-commit hooks that check your changes before allowing commits.

### For CI/CD

The GitHub Action `.github/workflows/quality-gate.yml` automatically runs on:
- Push to main/develop branches
- All pull requests
- Feature and hotfix branches

## 🧪 Manual Testing

Test the quality gates locally:

```bash
# Test FindObjectOfType detection
grep -r "FindObjectOfType" Assets/ProjectChimera --include="*.cs" | grep -v "Test"

# Test Resources.Load detection  
grep -r "Resources\.Load" Assets/ProjectChimera --include="*.cs" | grep -v "Test"

# Test raw Debug.Log detection
grep -r "Debug\.Log" Assets/ProjectChimera --include="*.cs" | grep -v "Systems/Diagnostics"
```

## 📊 Current Status

As of Phase 0.5 implementation:
- ✅ Quality gate CI workflow active
- ✅ Pre-commit hooks available
- 🔍 **323 FindObjectOfType calls** need elimination (Days 2-4)
- 🔍 **Multiple Resources.Load calls** need replacement
- 🔍 **2300+ Debug.Log calls** need ChimeraLogger migration

## 🎯 Success Metrics

Quality gates pass when:
- Zero FindObjectOfType calls in runtime code
- Zero Resources.Load calls (or documented exceptions)
- All Debug.Log routed through ChimeraLogger
- Build succeeds with ServiceLocator marked [Obsolete]

## 🚀 Next Steps (Phase 0.5)

1. **Days 2-4**: Eliminate all FindObjectOfType calls
2. **Days 5-6**: Implement central Update Bus (ITickable)  
3. **Day 7**: Quick wins (ServiceLocator deprecation, ChimeraLogger)

---

*This quality gate system ensures Phase 1 development starts on a solid architectural foundation.*