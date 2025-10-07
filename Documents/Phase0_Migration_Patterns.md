# PROJECT CHIMERA - PHASE 0 MIGRATION PATTERNS
**Generated:** 2025-10-03
**Purpose:** Comprehensive guide for migrating all anti-patterns to zero-tolerance compliance
**Reference:** Roadmap Part 1, Section "Phase 0: Foundation Crisis Response"

---

## TABLE OF CONTENTS

1. [FindObjectOfType Migration Patterns](#1-findobjectoftype-migration-patterns)
2. [Debug.Log Migration Patterns](#2-debuglog-migration-patterns)
3. [Resources.Load Migration Patterns](#3-resourcesload-migration-patterns)
4. [Reflection Migration Patterns](#4-reflection-migration-patterns)
5. [Update() to ITickable Migration](#5-update-to-itickable-migration)
6. [File Size Refactoring Patterns](#6-file-size-refactoring-patterns)

---

## 1. FINDOBJECTOFTYPE MIGRATION PATTERNS

### Overview
Replace all `FindObjectOfType<T>()` calls with proper dependency injection via ServiceContainer.

### Target: 83 violations → 0

---

### Pattern 1A: Simple Service Resolution

**OLD (Anti-pattern):**
```csharp
public class CultivationManager : MonoBehaviour
{
    private TimeManager _timeManager;

    private void Start()
    {
        _timeManager = FindObjectOfType<TimeManager>();
    }
}
```

**NEW (ServiceContainer DI):**
```csharp
public class CultivationManager : MonoBehaviour, ICultivationManager
{
    private ITimeManager _timeManager;

    private void Awake()
    {
        // Resolve via ServiceContainer
        _timeManager = ServiceContainer.Resolve<ITimeManager>();

        if (_timeManager == null)
        {
            ChimeraLogger.LogError("CULTIVATION",
                "Failed to resolve ITimeManager - ensure it's registered in ServiceContainer", this);
        }
    }
}
```

**Key Changes:**
- Use `ServiceContainer.Resolve<T>()` instead of `FindObjectOfType<T>()`
- Resolve in `Awake()` (earlier than Start for dependency setup)
- Add null check with ChimeraLogger error
- Use interface types (`ITimeManager` not `TimeManager`)

---

### Pattern 1B: Manager Registration During Bootstrap

**OLD (Anti-pattern):**
```csharp
public class GameBootstrapper : MonoBehaviour
{
    private void Awake()
    {
        var cultivationManager = FindObjectOfType<CultivationManager>();
        // Use cultivationManager...
    }
}
```

**NEW (Explicit Component Reference):**
```csharp
public class GameBootstrapper : MonoBehaviour
{
    [Header("Explicit Service References - NO FindObjectOfType")]
    [SerializeField] private CultivationManager _cultivationManager;
    [SerializeField] private TimeManager _timeManager;
    [SerializeField] private GridSystem _gridSystem;

    private void Awake()
    {
        // Register all services
        ServiceContainer.RegisterInstance<ICultivationManager>(_cultivationManager);
        ServiceContainer.RegisterInstance<ITimeManager>(_timeManager);
        ServiceContainer.RegisterInstance<IGridSystem>(_gridSystem);

        ChimeraLogger.Log("BOOTSTRAP",
            "Registered 3 core services via explicit references", this);
    }
}
```

**Key Changes:**
- Use `[SerializeField]` for explicit component references
- Wire up references in Unity Inspector (manual one-time setup)
- Register instances during bootstrap
- No runtime FindObjectOfType calls

---

### Pattern 1C: Property Injection for MonoBehaviours

**OLD (Anti-pattern):**
```csharp
public class PlantGrowthSystem : MonoBehaviour
{
    private EnvironmentalController _envController;
    private CultivationManager _cultivationManager;

    private void Start()
    {
        _envController = FindObjectOfType<EnvironmentalController>();
        _cultivationManager = FindObjectOfType<CultivationManager>();
    }
}
```

**NEW (Property Injection):**
```csharp
public class PlantGrowthSystem : MonoBehaviour, IPlantGrowthSystem
{
    private IEnvironmentalController _envController;
    private ICultivationManager _cultivationManager;

    private void Awake()
    {
        // Property injection pattern
        InjectDependencies();
        ValidateDependencies();
    }

    private void InjectDependencies()
    {
        _envController = ServiceContainer.Resolve<IEnvironmentalController>();
        _cultivationManager = ServiceContainer.Resolve<ICultivationManager>();
    }

    private void ValidateDependencies()
    {
        if (_envController == null)
            throw new InvalidOperationException("IEnvironmentalController not registered");

        if (_cultivationManager == null)
            throw new InvalidOperationException("ICultivationManager not registered");
    }
}
```

**Key Changes:**
- Separate `InjectDependencies()` method for clarity
- Explicit `ValidateDependencies()` with exceptions (fail-fast)
- All dependencies resolved in Awake before Start

---

### Pattern 1D: Optional Dependency with Fallback

**OLD (Anti-pattern with fallback):**
```csharp
public class AssetReleaseManager : MonoBehaviour
{
    private AssetCacheManager _cacheManager;

    public AssetReleaseManager(AssetCacheManager cacheManager = null)
    {
        _cacheManager = cacheManager ?? FindObjectOfType<AssetCacheManager>();
    }
}
```

**NEW (ServiceContainer with optional resolution):**
```csharp
public class AssetReleaseManager : MonoBehaviour, IAssetReleaseManager
{
    private IAssetCacheManager _cacheManager;

    private void Awake()
    {
        // Try to resolve, but don't fail if not registered (optional dependency)
        _cacheManager = ServiceContainer.TryResolve<IAssetCacheManager>();

        if (_cacheManager == null)
        {
            ChimeraLogger.LogWarning("ASSETS",
                "AssetCacheManager not available - caching disabled", this);
        }
    }
}
```

**Key Changes:**
- Use `TryResolve<T>()` for optional dependencies (returns null if not found)
- Log warning for missing optional services
- No FindObjectOfType fallback

---

### Pattern 1E: Multiple Dependencies via Batch Resolution

**OLD (Anti-pattern with multiple lookups):**
```csharp
public class StreamingQualityManager : MonoBehaviour
{
    private StreamingPerformanceMonitor _performanceMonitor;
    private AssetStreamingManager _assetStreaming;
    private LODManager _lodManager;

    private void Start()
    {
        _performanceMonitor = FindObjectOfType<StreamingPerformanceMonitor>();
        _assetStreaming = FindObjectOfType<AssetStreamingManager>();
        _lodManager = FindObjectOfType<LODManager>();
    }
}
```

**NEW (Batch resolution helper):**
```csharp
public class StreamingQualityManager : MonoBehaviour, IStreamingQualityManager
{
    private IStreamingPerformanceMonitor _performanceMonitor;
    private IAssetStreamingManager _assetStreaming;
    private ILODManager _lodManager;

    private void Awake()
    {
        // Batch resolve all dependencies
        var dependencies = ServiceContainer.ResolveMany<IStreamingPerformanceMonitor,
                                                        IAssetStreamingManager,
                                                        ILODManager>();

        _performanceMonitor = dependencies.Item1;
        _assetStreaming = dependencies.Item2;
        _lodManager = dependencies.Item3;

        ValidateAllDependencies();
    }

    private void ValidateAllDependencies()
    {
        if (_performanceMonitor == null || _assetStreaming == null || _lodManager == null)
        {
            throw new InvalidOperationException(
                "StreamingQualityManager missing required dependencies");
        }
    }
}
```

**Key Changes:**
- Use batch resolution pattern (if ServiceContainer supports it)
- Or resolve individually in sequence
- Single validation method for all dependencies

---

### Pattern 1F: Singleton Pattern via ServiceContainer

**OLD (Anti-pattern with singleton):**
```csharp
public class UpdateOrchestrator : MonoBehaviour
{
    private static UpdateOrchestrator _instance;

    public static UpdateOrchestrator Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindObjectOfType<UpdateOrchestrator>();
            return _instance;
        }
    }
}
```

**NEW (ServiceContainer singleton):**
```csharp
public class UpdateOrchestrator : MonoBehaviour, IUpdateOrchestrator
{
    private void Awake()
    {
        // Register self as singleton
        ServiceContainer.RegisterSingleton<IUpdateOrchestrator>(this);
    }

    private void OnDestroy()
    {
        // Cleanup registration
        ServiceContainer.Unregister<IUpdateOrchestrator>();
    }
}

// Usage in other classes:
public class SomeSystem : MonoBehaviour
{
    private IUpdateOrchestrator _orchestrator;

    private void Awake()
    {
        _orchestrator = ServiceContainer.Resolve<IUpdateOrchestrator>();
    }
}
```

**Key Changes:**
- No static `Instance` property
- Self-registration in Awake
- Cleanup in OnDestroy
- Other systems resolve via ServiceContainer

---

### Pattern 1G: Performance Metrics Object Counting (Special Case)

**OLD (Anti-pattern for counting):**
```csharp
public class StandardMetricCollectors
{
    private int CountPlants()
    {
        var plants = Object.FindObjectsOfType<MonoBehaviour>().Length;
        return plants;
    }
}
```

**NEW (Centralized registry pattern):**
```csharp
// Create a central registry service
public interface IGameObjectRegistry
{
    int GetCount<T>() where T : MonoBehaviour;
    void RegisterObject<T>(T obj) where T : MonoBehaviour;
    void UnregisterObject<T>(T obj) where T : MonoBehaviour;
}

public class GameObjectRegistry : MonoBehaviour, IGameObjectRegistry
{
    private Dictionary<Type, HashSet<MonoBehaviour>> _registeredObjects = new();

    public int GetCount<T>() where T : MonoBehaviour
    {
        var type = typeof(T);
        return _registeredObjects.ContainsKey(type) ? _registeredObjects[type].Count : 0;
    }

    public void RegisterObject<T>(T obj) where T : MonoBehaviour
    {
        var type = typeof(T);
        if (!_registeredObjects.ContainsKey(type))
            _registeredObjects[type] = new HashSet<MonoBehaviour>();

        _registeredObjects[type].Add(obj);
    }

    public void UnregisterObject<T>(T obj) where T : MonoBehaviour
    {
        var type = typeof(T);
        if (_registeredObjects.ContainsKey(type))
            _registeredObjects[type].Remove(obj);
    }
}

// Usage in metrics:
public class StandardMetricCollectors
{
    private IGameObjectRegistry _registry;

    private int CountPlants()
    {
        return _registry.GetCount<PlantInstance>();
    }
}

// Plants register themselves:
public class PlantInstance : MonoBehaviour
{
    private void Awake()
    {
        var registry = ServiceContainer.Resolve<IGameObjectRegistry>();
        registry.RegisterObject(this);
    }

    private void OnDestroy()
    {
        var registry = ServiceContainer.Resolve<IGameObjectRegistry>();
        registry.UnregisterObject(this);
    }
}
```

**Key Changes:**
- Create centralized registry service
- Objects self-register/unregister
- Metrics query registry instead of scanning scene

---

### Special Cases: Transitional Code

**DependencyResolutionHelper (intentional fallback):**
```csharp
// This file is TRANSITIONAL ONLY - will be removed after migration
// Contains FindObjectOfType as fallback during migration period
// DO NOT use in new code - for legacy support only

public static class DependencyResolutionHelper
{
    [Obsolete("Use ServiceContainer.Resolve<T>() directly")]
    public static T SafeResolve<T>() where T : Component
    {
        // Try ServiceContainer first
        var service = ServiceContainer.TryResolve<T>();
        if (service != null) return service;

        // TEMPORARY fallback for migration period
        ChimeraLogger.LogWarning("MIGRATION",
            $"Falling back to FindObjectOfType for {typeof(T).Name} - update caller to use ServiceContainer",
            null);

        return Object.FindObjectOfType<T>();
    }
}
```

**Migration Plan:**
1. Keep `DependencyResolutionHelper` during Phase 0 Week 1
2. Add `[Obsolete]` attribute to all methods
3. Log warnings when fallback is used
4. Track fallback usage via ChimeraLogger
5. Remove entire file after Week 1 completion

---

### Validation Script

```csharp
// Editor/FindObjectOfTypeValidator.cs
public class FindObjectOfTypeValidator : EditorWindow
{
    [MenuItem("Chimera/Validation/Check FindObjectOfType Violations")]
    public static void ValidateNoFindObjectOfType()
    {
        var violations = new List<string>();

        var csFiles = Directory.GetFiles("Assets/ProjectChimera", "*.cs", SearchOption.AllDirectories);

        foreach (var file in csFiles)
        {
            // Skip transitional helper and migration tools
            if (file.Contains("DependencyResolutionHelper") ||
                file.Contains("AntiPatternMigrationTool") ||
                file.Contains("BatchMigrationScript"))
                continue;

            var content = File.ReadAllText(file);

            if (content.Contains("FindObjectOfType") || content.Contains("FindObjectsOfType"))
            {
                violations.Add(file);
            }
        }

        if (violations.Count == 0)
        {
            Debug.Log("✅ PASSED: Zero FindObjectOfType violations");
        }
        else
        {
            Debug.LogError($"❌ FAILED: {violations.Count} FindObjectOfType violations found:\n" +
                          string.Join("\n", violations));
        }
    }
}
```

---

## 2. DEBUG.LOG MIGRATION PATTERNS

### Overview
Replace all `Debug.Log` calls with `ChimeraLogger` for structured, categorized logging.

### Target: 61 violations → 0

---

### Pattern 2A: Simple Log Replacement

**OLD:**
```csharp
Debug.Log("Service initialized: " + serviceName);
```

**NEW:**
```csharp
ChimeraLogger.Log("CORE", $"Service initialized: {serviceName}", this);
```

**Category**: Use appropriate category for the system (see table below)

---

### Pattern 2B: Log with Context Object

**OLD:**
```csharp
Debug.Log("Plant growth: " + growth);
```

**NEW:**
```csharp
ChimeraLogger.Log("CULTIVATION",
    $"Plant {plantId} growth: {growth:F2} (Δ{deltaGrowth:F2})",
    this);
```

**Key Changes:**
- Category: "CULTIVATION"
- Structured format with plant ID
- Formatted numbers (:F2 for 2 decimals)
- Context object: `this` (for stack trace)

---

### Pattern 2C: Conditional/Development-Only Logs

**OLD:**
```csharp
#if DEBUG
Debug.Log("Detailed trace data: " + data);
#endif
```

**NEW:**
```csharp
#if CHIMERA_DEVELOPMENT_LOGGING
ChimeraLogger.LogVerbose("DEBUG", $"Detailed trace: {data}", this);
#endif
```

**Key Changes:**
- Use custom define: `CHIMERA_DEVELOPMENT_LOGGING`
- Use `LogVerbose()` for development-only logs
- Easily disable in production builds

---

### Pattern 2D: Error Logs

**OLD:**
```csharp
Debug.LogError("Failed to load asset: " + assetPath);
```

**NEW:**
```csharp
ChimeraLogger.LogError("ASSETS",
    $"Failed to load asset: {assetPath}",
    this);
```

---

### Pattern 2E: Warning Logs

**OLD:**
```csharp
Debug.LogWarning("Service not found: " + serviceName);
```

**NEW:**
```csharp
ChimeraLogger.LogWarning("CORE",
    $"Service not found: {serviceName}",
    this);
```

---

### Category Mapping Table

| Category | Usage | System Examples |
|----------|-------|----------------|
| **CORE** | Bootstrap, services, DI | ServiceContainer, GameManager, ChimeraManager |
| **CONSTRUCTION** | Grid, placement, building | GridSystem, ConstructionManager, SchematicUnlockManager |
| **CULTIVATION** | Plant growth, care, environment | PlantGrowthSystem, CultivationManager, EnvironmentalController |
| **GENETICS** | Breeding, traits, blockchain | FractalGeneticsEngine, BreedingSystem, GenotypeFactory |
| **SAVE** | Persistence, serialization | SaveManager, DataSerializer, SaveStorage |
| **ASSETS** | Addressables, resources | AddressableAssetManager, AssetPreloader, AssetReleaseManager |
| **PERFORMANCE** | Benchmarking, profiling | PerformanceMonitor, MetricsCollector, StreamingPerformanceMonitor |
| **UI** | Interface, input, menus | AdvancedMenuSystem, GameplayModeController, InputHandler |
| **CAMERA** | Camera controls, viewpoints | CameraService, CameraStateManager, CameraTransitionManager |
| **AUDIO** | Sound, music, effects | AudioManager, AudioLoadingService, AudioEffectsProcessor |
| **SCENE** | Scene loading, transitions | SceneLoader, BootManager, SceneTransition |
| **TIME** | Time management, scaling | TimeManager, OfflineProgression, TimeScale |
| **DEBUG** | Development/verbose logs | Any development-only logging |

---

### Automated Migration Tool

```bash
# Run automated Debug.Log migration (if tool exists)
# Assets/ProjectChimera/Editor/DebugLogMigrationTool.cs

# 1. Scan all files
# 2. Categorize by directory/namespace
# 3. Replace with ChimeraLogger + appropriate category
# 4. Manual review for complex cases
```

**Manual Review Cases:**
- Complex string interpolation
- Conditional logs with multiple conditions
- Logs inside loops (consider performance)
- Stack trace dependencies

---

### Validation Script

```csharp
// Editor/DebugLogValidator.cs
public static void ValidateNoDebugLog()
{
    var violations = new List<string>();
    var csFiles = Directory.GetFiles("Assets/ProjectChimera", "*.cs", SearchOption.AllDirectories);

    foreach (var file in csFiles)
    {
        // Skip ChimeraLogger itself and test files
        if (file.Contains("ChimeraLogger.cs") || file.Contains("Test"))
            continue;

        var content = File.ReadAllText(file);

        if (Regex.IsMatch(content, @"Debug\.Log"))
        {
            violations.Add(file);
        }
    }

    if (violations.Count == 0)
        Debug.Log("✅ PASSED: Zero Debug.Log violations");
    else
        Debug.LogError($"❌ FAILED: {violations.Count} Debug.Log violations");
}
```

---

## 3. RESOURCES.LOAD MIGRATION PATTERNS

### Overview
Replace all `Resources.Load` calls with Addressables system.

### Target: 14 violations → 0

---

### Pattern 3A: Asset Catalog ScriptableObject

**Create centralized asset catalog:**

```csharp
// Data/AssetCatalog/ChimeraAssetCatalog.cs
[CreateAssetMenu(fileName = "ChimeraAssetCatalog", menuName = "Chimera/Asset Catalog")]
public class ChimeraAssetCatalog : ScriptableObject
{
    [Header("Genetics Assets")]
    [SerializeField] private AssetReferenceComputeShader _fractalGeneticsShader;
    [SerializeField] private List<AssetReferenceScriptableObject> _plantStrains;

    [Header("SpeedTree Assets")]
    [SerializeField] private List<AssetReferenceGameObject> _speedTreeVariants;

    [Header("Audio Assets")]
    [SerializeField] private List<AssetReferenceAudioClip> _careToolSounds;
    [SerializeField] private List<AssetReferenceAudioClip> _ambientSounds;

    // Async loading methods
    public async Task<ComputeShader> LoadGeneticsShaderAsync()
    {
        var handle = _fractalGeneticsShader.LoadAssetAsync<ComputeShader>();
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
            return handle.Result;

        ChimeraLogger.LogError("ASSETS", "Failed to load fractal genetics shader", this);
        return null;
    }

    public async Task<PlantStrainSO> LoadStrainAsync(string strainId)
    {
        var strainRef = _plantStrains.Find(s => s.SubObjectName == strainId);
        if (strainRef == null)
        {
            ChimeraLogger.LogError("ASSETS", $"Strain not found in catalog: {strainId}", this);
            return null;
        }

        var handle = strainRef.LoadAssetAsync<PlantStrainSO>();
        await handle.Task;

        return handle.Status == AsyncOperationStatus.Succeeded ? handle.Result : null;
    }

    public async Task<GameObject> LoadSpeedTreeVariantAsync(string variantId)
    {
        var variant = _speedTreeVariants.Find(v => v.Asset.name == variantId);
        if (variant == null) return null;

        var handle = variant.LoadAssetAsync<GameObject>();
        await handle.Task;

        return handle.Status == AsyncOperationStatus.Succeeded ? handle.Result : null;
    }

    public async Task<AudioClip> LoadCareToolSoundAsync(string soundName)
    {
        var soundRef = _careToolSounds.Find(s => s.Asset.name == soundName);
        if (soundRef == null) return null;

        var handle = soundRef.LoadAssetAsync<AudioClip>();
        await handle.Task;

        return handle.Status == AsyncOperationStatus.Succeeded ? handle.Result : null;
    }
}
```

---

### Pattern 3B: Genetics Compute Shader Migration

**OLD:**
```csharp
public class FractalGeneticsEngine
{
    private ComputeShader _fractalShader;

    private void Start()
    {
        _fractalShader = Resources.Load<ComputeShader>("FractalGeneticsCompute");
    }
}
```

**NEW:**
```csharp
public class FractalGeneticsEngine : MonoBehaviour
{
    private IAssetCatalogService _assetCatalog;
    private ComputeShader _fractalShader;

    private void Awake()
    {
        _assetCatalog = ServiceContainer.Resolve<IAssetCatalogService>();
    }

    private async Task InitializeAsync()
    {
        _fractalShader = await _assetCatalog.LoadGeneticsShaderAsync();

        if (_fractalShader == null)
        {
            throw new InvalidOperationException(
                "Failed to load fractal genetics compute shader");
        }

        ChimeraLogger.Log("GENETICS", "Fractal shader loaded successfully", this);
    }
}
```

---

### Pattern 3C: SpeedTree Asset Migration

**OLD:**
```csharp
public class SpeedTreeManager
{
    private GameObject LoadTreeVariant(string variantName)
    {
        return Resources.Load<GameObject>($"SpeedTree/{variantName}");
    }
}
```

**NEW:**
```csharp
public class SpeedTreeAssetService : MonoBehaviour, ISpeedTreeAssetService
{
    private IAssetCatalogService _assetCatalog;

    private void Awake()
    {
        _assetCatalog = ServiceContainer.Resolve<IAssetCatalogService>();
    }

    public async Task<GameObject> LoadTreeVariantAsync(string variantId)
    {
        var treeVariant = await _assetCatalog.LoadSpeedTreeVariantAsync(variantId);

        if (treeVariant == null)
        {
            ChimeraLogger.LogWarning("ASSETS",
                $"SpeedTree variant not found: {variantId}", this);
        }

        return treeVariant;
    }
}
```

---

### Pattern 3D: Audio Clip Migration

**OLD:**
```csharp
public class AudioManager
{
    private AudioClip LoadSound(string soundName)
    {
        return Resources.Load<AudioClip>($"Audio/SFX/{soundName}");
    }
}
```

**NEW:**
```csharp
public class AudioLoadingService : MonoBehaviour, IAudioLoadingService
{
    private IAssetCatalogService _assetCatalog;
    private Dictionary<string, AudioClip> _cachedClips = new();

    private void Awake()
    {
        _assetCatalog = ServiceContainer.Resolve<IAssetCatalogService>();
    }

    public async Task<AudioClip> LoadSoundAsync(string soundName)
    {
        // Check cache first
        if (_cachedClips.ContainsKey(soundName))
            return _cachedClips[soundName];

        // Load from Addressables
        var clip = await _assetCatalog.LoadCareToolSoundAsync(soundName);

        if (clip != null)
        {
            _cachedClips[soundName] = clip;
            ChimeraLogger.Log("AUDIO", $"Loaded and cached audio clip: {soundName}", this);
        }

        return clip;
    }

    public void UnloadSound(string soundName)
    {
        if (_cachedClips.ContainsKey(soundName))
        {
            // Release Addressable asset
            Addressables.Release(_cachedClips[soundName]);
            _cachedClips.Remove(soundName);
        }
    }
}
```

---

### Pattern 3E: AssetCatalog Service Registration

```csharp
// Core/ServiceContainerBootstrapper.cs
public class ServiceContainerBootstrapper : MonoBehaviour
{
    [SerializeField] private ChimeraAssetCatalog _assetCatalog;

    private void Awake()
    {
        // Register asset catalog as singleton service
        ServiceContainer.RegisterInstance<IAssetCatalogService>(
            new AssetCatalogService(_assetCatalog));

        ChimeraLogger.Log("BOOTSTRAP", "AssetCatalog service registered", this);
    }
}

// Wrapper service for DI
public interface IAssetCatalogService
{
    Task<ComputeShader> LoadGeneticsShaderAsync();
    Task<PlantStrainSO> LoadStrainAsync(string strainId);
    Task<GameObject> LoadSpeedTreeVariantAsync(string variantId);
    Task<AudioClip> LoadCareToolSoundAsync(string soundName);
}

public class AssetCatalogService : IAssetCatalogService
{
    private readonly ChimeraAssetCatalog _catalog;

    public AssetCatalogService(ChimeraAssetCatalog catalog)
    {
        _catalog = catalog;
    }

    public Task<ComputeShader> LoadGeneticsShaderAsync()
        => _catalog.LoadGeneticsShaderAsync();

    public Task<PlantStrainSO> LoadStrainAsync(string strainId)
        => _catalog.LoadStrainAsync(strainId);

    public Task<GameObject> LoadSpeedTreeVariantAsync(string variantId)
        => _catalog.LoadSpeedTreeVariantAsync(variantId);

    public Task<AudioClip> LoadCareToolSoundAsync(string soundName)
        => _catalog.LoadCareToolSoundAsync(soundName);
}
```

---

### Addressables Setup Checklist

1. **Create Addressables Groups:**
   - `Genetics_ComputeShaders`
   - `SpeedTree_Assets`
   - `Audio_SFX`
   - `Audio_Music`

2. **Assign Asset Labels:**
   - Label assets with system name (e.g., "Genetics", "SpeedTree")
   - Use consistent naming conventions

3. **Configure Build Settings:**
   - Enable Addressables in Unity Build Settings
   - Set compression (LZ4 for speed, LZMA for size)
   - Configure remote/local content delivery

4. **Create Asset Catalog ScriptableObject:**
   - Assign Addressable references in Inspector
   - Wire up to ServiceContainer

---

## 4. REFLECTION MIGRATION PATTERNS

### Overview
Replace reflection-based access with compile-time interfaces and strategy patterns.

### Target: 30 violations → 0

---

### Pattern 4A: Property Access via Interfaces

**OLD (Reflection):**
```csharp
var healthProp = typeof(Plant).GetProperty("Health");
var currentHealth = (float)healthProp.GetValue(plant);
healthProp.SetValue(plant, newHealth);
```

**NEW (Interface):**
```csharp
public interface IHealthManager
{
    float Health { get; set; }
    float MaxHealth { get; }
}

public class Plant : MonoBehaviour, IHealthManager
{
    public float Health { get; set; }
    public float MaxHealth => 100f;
}

// Usage:
IHealthManager plant = GetPlant();
float currentHealth = plant.Health;
plant.Health = newHealth;
```

---

### Pattern 4B: Dynamic Method Invocation via Strategy Pattern

**OLD (Reflection):**
```csharp
var method = type.GetMethod("ProcessData");
method.Invoke(instance, new object[] { data });
```

**NEW (Strategy Pattern):**
```csharp
public interface IDataProcessor
{
    void ProcessData(object data);
}

public class PlantDataProcessor : IDataProcessor
{
    public void ProcessData(object data)
    {
        // Process plant data
    }
}

public class ProcessorFactory
{
    private Dictionary<Type, IDataProcessor> _processors = new();

    public void RegisterProcessor<T>(IDataProcessor processor)
    {
        _processors[typeof(T)] = processor;
    }

    public void Process<T>(object data)
    {
        if (_processors.TryGetValue(typeof(T), out var processor))
        {
            processor.ProcessData(data);
        }
        else
        {
            ChimeraLogger.LogError("PROCESSING",
                $"No processor registered for type {typeof(T).Name}", null);
        }
    }
}
```

---

### Pattern 4C: Field Scanning via Manual Registration

**OLD (Reflection attribute scanning):**
```csharp
var fields = type.GetFields()
    .Where(f => f.GetCustomAttribute<SerializeField>() != null);

foreach (var field in fields)
{
    // Process serialized fields
}
```

**NEW (Manual registration):**
```csharp
public static class SerializableFieldRegistry
{
    private static Dictionary<Type, List<FieldInfo>> _registeredFields = new();

    static SerializableFieldRegistry()
    {
        // Manual registration at compile-time
        RegisterFields<PlantInstance>(
            nameof(PlantInstance.Health),
            nameof(PlantInstance.Growth),
            nameof(PlantInstance.Stage)
        );

        RegisterFields<GridCell>(
            nameof(GridCell.IsOccupied),
            nameof(GridCell.OccupantId)
        );
    }

    private static void RegisterFields<T>(params string[] fieldNames)
    {
        var type = typeof(T);
        _registeredFields[type] = new List<FieldInfo>();

        foreach (var fieldName in fieldNames)
        {
            var field = type.GetField(fieldName);
            if (field != null)
                _registeredFields[type].Add(field);
        }
    }

    public static List<FieldInfo> GetRegisteredFields<T>()
    {
        return _registeredFields.ContainsKey(typeof(T))
            ? _registeredFields[typeof(T)]
            : new List<FieldInfo>();
    }
}
```

---

### Pattern 4D: Type-Based Behavior via Factory Pattern

**OLD (Reflection for type resolution):**
```csharp
var instance = Activator.CreateInstance(type);
var method = type.GetMethod("Initialize");
method.Invoke(instance, parameters);
```

**NEW (Factory with registration):**
```csharp
public interface IInitializable
{
    void Initialize(params object[] parameters);
}

public class ComponentFactory
{
    private Dictionary<string, Func<IInitializable>> _factories = new();

    public void RegisterFactory<T>(Func<IInitializable> factory) where T : IInitializable
    {
        _factories[typeof(T).Name] = factory;
    }

    public IInitializable Create(string typeName, params object[] parameters)
    {
        if (!_factories.ContainsKey(typeName))
        {
            ChimeraLogger.LogError("FACTORY", $"No factory for type: {typeName}", null);
            return null;
        }

        var instance = _factories[typeName]();
        instance.Initialize(parameters);
        return instance;
    }
}

// Registration:
factory.RegisterFactory<PlantInstance>(() => new PlantInstance());
factory.RegisterFactory<GridCell>(() => new GridCell());

// Usage:
var plant = factory.Create("PlantInstance", initialHealth, growthStage);
```

---

## 5. UPDATE() TO ITICKABLE MIGRATION

### Overview
Migrate MonoBehaviour `Update()` methods to centralized `ITickable` pattern via `UpdateOrchestrator`.

### Target: 12 violations → ≤5 (only Unity-required)

---

### Pattern 5A: Simple Update Migration

**OLD:**
```csharp
public class PlantHealthSystem : MonoBehaviour
{
    private void Update()
    {
        ProcessPlantHealth(Time.deltaTime);
    }

    private void ProcessPlantHealth(float deltaTime)
    {
        // Health processing logic
    }
}
```

**NEW:**
```csharp
public class PlantHealthSystem : MonoBehaviour, ITickable
{
    // ITickable implementation
    public int TickPriority => 80; // High priority
    public bool IsTickable => enabled && gameObject.activeInHierarchy;

    public void Tick(float deltaTime)
    {
        ProcessPlantHealth(deltaTime);
    }

    private void Awake()
    {
        var orchestrator = ServiceContainer.Resolve<IUpdateOrchestrator>();
        orchestrator.RegisterTickable(this);
    }

    private void OnDestroy()
    {
        var orchestrator = ServiceContainer.Resolve<IUpdateOrchestrator>();
        orchestrator.UnregisterTickable(this);
    }

    private void ProcessPlantHealth(float deltaTime)
    {
        // Health processing logic (unchanged)
    }
}
```

**Key Changes:**
- Implement `ITickable` interface
- Set `TickPriority` (0-100, higher = earlier execution)
- Implement `IsTickable` property
- Register in `Awake()`, unregister in `OnDestroy()`
- Move Update logic to `Tick(float deltaTime)`

---

### Pattern 5B: FixedUpdate Migration

**OLD:**
```csharp
public class PhysicsSystem : MonoBehaviour
{
    private void FixedUpdate()
    {
        ProcessPhysics(Time.fixedDeltaTime);
    }
}
```

**NEW:**
```csharp
public class PhysicsSystem : MonoBehaviour, IFixedTickable
{
    public int TickPriority => 90; // Very high priority for physics
    public bool IsTickable => enabled && gameObject.activeInHierarchy;

    public void FixedTick(float fixedDeltaTime)
    {
        ProcessPhysics(fixedDeltaTime);
    }

    private void Awake()
    {
        var orchestrator = ServiceContainer.Resolve<IUpdateOrchestrator>();
        orchestrator.RegisterFixedTickable(this);
    }

    private void OnDestroy()
    {
        var orchestrator = ServiceContainer.Resolve<IUpdateOrchestrator>();
        orchestrator.UnregisterFixedTickable(this);
    }
}
```

---

### Pattern 5C: LateUpdate Migration

**OLD:**
```csharp
public class CameraFollow : MonoBehaviour
{
    private void LateUpdate()
    {
        UpdateCameraPosition(Time.deltaTime);
    }
}
```

**NEW:**
```csharp
public class CameraFollow : MonoBehaviour, ILateTickable
{
    public int TickPriority => 10; // Low priority (after other updates)
    public bool IsTickable => enabled && gameObject.activeInHierarchy;

    public void LateTick(float deltaTime)
    {
        UpdateCameraPosition(deltaTime);
    }

    private void Awake()
    {
        var orchestrator = ServiceContainer.Resolve<IUpdateOrchestrator>();
        orchestrator.RegisterLateTickable(this);
    }

    private void OnDestroy()
    {
        var orchestrator = ServiceContainer.Resolve<IUpdateOrchestrator>();
        orchestrator.UnregisterLateTickable(this);
    }
}
```

---

### ITickable Priority Guidelines

| Priority Range | Usage | Examples |
|----------------|-------|----------|
| **90-100** | Critical systems, physics | PhysicsSystem, CoreManagers |
| **70-89** | Game logic, high priority | PlantHealthSystem, EnvironmentalController |
| **50-69** | Standard gameplay | PlantGrowthProcessor, CultivationManager |
| **30-49** | Secondary systems | UIAnimations, ParticleEffects |
| **10-29** | Low priority, post-processing | CameraFollow, DebugOverlay |
| **0-9** | Monitoring, analytics | PerformanceMonitor, HealthChecks |

---

### UpdateOrchestrator Enhancement

```csharp
// Core/Updates/UpdateOrchestrator.cs
public class UpdateOrchestrator : MonoBehaviour, IUpdateOrchestrator
{
    private List<ITickable> _tickables = new();
    private List<IFixedTickable> _fixedTickables = new();
    private List<ILateTickable> _lateTickables = new();

    private List<ITickable> _toAddTickables = new();
    private List<ITickable> _toRemoveTickables = new();

    private void Awake()
    {
        ServiceContainer.RegisterSingleton<IUpdateOrchestrator>(this);
    }

    public void RegisterTickable(ITickable tickable)
    {
        if (!_toAddTickables.Contains(tickable) && !_tickables.Contains(tickable))
            _toAddTickables.Add(tickable);
    }

    public void UnregisterTickable(ITickable tickable)
    {
        if (!_toRemoveTickables.Contains(tickable))
            _toRemoveTickables.Add(tickable);
    }

    public void RegisterFixedTickable(IFixedTickable tickable)
    {
        if (!_fixedTickables.Contains(tickable))
            _fixedTickables.Add(tickable);
    }

    public void RegisterLateTickable(ILateTickable tickable)
    {
        if (!_lateTickables.Contains(tickable))
            _lateTickables.Add(tickable);
    }

    private void Update()
    {
        // Process additions/removals
        if (_toAddTickables.Count > 0)
        {
            _tickables.AddRange(_toAddTickables);
            _tickables.Sort((a, b) => b.TickPriority.CompareTo(a.TickPriority));
            _toAddTickables.Clear();
        }

        if (_toRemoveTickables.Count > 0)
        {
            foreach (var tickable in _toRemoveTickables)
                _tickables.Remove(tickable);
            _toRemoveTickables.Clear();
        }

        // Tick all registered systems
        var deltaTime = Time.deltaTime;
        for (int i = 0; i < _tickables.Count; i++)
        {
            var tickable = _tickables[i];
            if (tickable.IsTickable)
            {
                try
                {
                    tickable.Tick(deltaTime);
                }
                catch (Exception ex)
                {
                    ChimeraLogger.LogError("UPDATE",
                        $"Tickable {tickable.GetType().Name} failed: {ex.Message}", this);
                }
            }
        }
    }

    private void FixedUpdate()
    {
        var fixedDeltaTime = Time.fixedDeltaTime;
        for (int i = 0; i < _fixedTickables.Count; i++)
        {
            var tickable = _fixedTickables[i];
            if (tickable.IsTickable)
                tickable.FixedTick(fixedDeltaTime);
        }
    }

    private void LateUpdate()
    {
        var deltaTime = Time.deltaTime;
        for (int i = 0; i < _lateTickables.Count; i++)
        {
            var tickable = _lateTickables[i];
            if (tickable.IsTickable)
                tickable.LateTick(deltaTime);
        }
    }
}
```

---

### Allowed Update() Methods (≤5)

**These CAN remain as Update() methods:**

1. **UpdateOrchestrator.cs** - Central dispatcher (required)
2. **UnityInputProcessor.cs** - Unity Input System integration (if needed)
3. **AnimatorController.cs** - Unity Animator state updates (if needed)
4. **PhysicsDebugger.cs** - Editor-only physics debugging (if needed)
5. **[One more Unity-specific requirement]**

**All other gameplay/logic MUST use ITickable.**

---

## 6. FILE SIZE REFACTORING PATTERNS

### Overview
Split all files >400 lines into smaller, focused components following Single Responsibility Principle.

### Target: 193 violations → 0

---

### Pattern 6A: Component Decomposition Strategy

**Example: GridSystem.cs (502 lines) → 4 components**

**Original Structure:**
```
GridSystem.cs (502 lines)
├── Grid data storage (125 lines)
├── Placement validation (120 lines)
├── Visualization (130 lines)
└── Placement algorithms (127 lines)
```

**Refactored Structure:**
```
GridSystem.cs (80 lines - Coordinator/Facade)
├── GridData.cs (125 lines - Data storage)
├── GridValidation.cs (120 lines - Validation logic)
├── GridVisualization.cs (130 lines - Rendering)
└── GridPlacementLogic.cs (127 lines - Placement algorithms)
```

---

### Pattern 6B: Interface-First Design

**Step 1: Define Interfaces**

```csharp
// Grid/IGridData.cs
public interface IGridData
{
    bool IsOccupied(Vector3Int position);
    GridCell GetCell(Vector3Int position);
    void SetCell(Vector3Int position, GridCell cell);
    void ClearCell(Vector3Int position);
    IEnumerable<GridCell> GetAllCells();
}

// Grid/IGridValidation.cs
public interface IGridValidation
{
    bool CanPlace(SchematicSO schematic, Vector3Int position);
    ValidationResult ValidatePlacement(SchematicSO schematic, Vector3Int position);
    bool CheckClearance(Vector3Int position, Vector3Int size);
}

// Grid/IGridVisualization.cs
public interface IGridVisualization
{
    void ShowGrid(bool visible);
    void HighlightCells(IEnumerable<Vector3Int> positions, Color color);
    void DrawPlacementPreview(SchematicSO schematic, Vector3Int position, bool valid);
}

// Grid/IGridPlacementLogic.cs
public interface IGridPlacementLogic
{
    Task<bool> PlaceItemAsync(SchematicSO schematic, Vector3Int position);
    bool RemoveItem(Vector3Int position);
    void MoveItem(Vector3Int from, Vector3Int to);
}
```

---

**Step 2: Implement Components**

```csharp
// Grid/GridData.cs (125 lines)
public class GridData : MonoBehaviour, IGridData
{
    private Dictionary<Vector3Int, GridCell> _cells = new();
    private Vector3Int _gridSize;

    public bool IsOccupied(Vector3Int position)
    {
        return _cells.ContainsKey(position) && _cells[position].IsOccupied;
    }

    public GridCell GetCell(Vector3Int position)
    {
        return _cells.ContainsKey(position) ? _cells[position] : null;
    }

    public void SetCell(Vector3Int position, GridCell cell)
    {
        _cells[position] = cell;
    }

    public void ClearCell(Vector3Int position)
    {
        if (_cells.ContainsKey(position))
            _cells.Remove(position);
    }

    public IEnumerable<GridCell> GetAllCells()
    {
        return _cells.Values;
    }
}
```

```csharp
// Grid/GridValidation.cs (120 lines)
public class GridValidation : MonoBehaviour, IGridValidation
{
    private IGridData _gridData;

    private void Awake()
    {
        _gridData = GetComponent<GridData>();
    }

    public bool CanPlace(SchematicSO schematic, Vector3Int position)
    {
        var result = ValidatePlacement(schematic, position);
        return result.IsValid;
    }

    public ValidationResult ValidatePlacement(SchematicSO schematic, Vector3Int position)
    {
        // Validation logic
        if (!CheckClearance(position, schematic.Size))
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = "Insufficient clearance"
            };
        }

        // Additional validation...

        return new ValidationResult { IsValid = true };
    }

    public bool CheckClearance(Vector3Int position, Vector3Int size)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    var checkPos = position + new Vector3Int(x, y, z);
                    if (_gridData.IsOccupied(checkPos))
                        return false;
                }
            }
        }
        return true;
    }
}
```

```csharp
// Grid/GridVisualization.cs (130 lines)
public class GridVisualization : MonoBehaviour, IGridVisualization
{
    private IGridData _gridData;
    private Material _gridMaterial;
    private GameObject _gridVisualizationRoot;

    private void Awake()
    {
        _gridData = GetComponent<GridData>();
    }

    public void ShowGrid(bool visible)
    {
        if (_gridVisualizationRoot != null)
            _gridVisualizationRoot.SetActive(visible);
    }

    public void HighlightCells(IEnumerable<Vector3Int> positions, Color color)
    {
        foreach (var pos in positions)
        {
            // Highlight logic
        }
    }

    public void DrawPlacementPreview(SchematicSO schematic, Vector3Int position, bool valid)
    {
        var previewColor = valid ? Color.green : Color.red;
        // Draw preview logic
    }
}
```

```csharp
// Grid/GridPlacementLogic.cs (127 lines)
public class GridPlacementLogic : MonoBehaviour, IGridPlacementLogic
{
    private IGridData _gridData;
    private IGridValidation _validation;

    private void Awake()
    {
        _gridData = GetComponent<GridData>();
        _validation = GetComponent<GridValidation>();
    }

    public async Task<bool> PlaceItemAsync(SchematicSO schematic, Vector3Int position)
    {
        if (!_validation.CanPlace(schematic, position))
        {
            ChimeraLogger.LogWarning("GRID",
                "Cannot place item - validation failed", this);
            return false;
        }

        // Placement logic
        var cell = new GridCell
        {
            Position = position,
            SchematicId = schematic.Id,
            IsOccupied = true
        };

        _gridData.SetCell(position, cell);

        ChimeraLogger.Log("GRID",
            $"Placed {schematic.Name} at {position}", this);

        return true;
    }

    public bool RemoveItem(Vector3Int position)
    {
        _gridData.ClearCell(position);
        return true;
    }

    public void MoveItem(Vector3Int from, Vector3Int to)
    {
        var cell = _gridData.GetCell(from);
        if (cell != null)
        {
            _gridData.ClearCell(from);
            cell.Position = to;
            _gridData.SetCell(to, cell);
        }
    }
}
```

---

**Step 3: Create Coordinator/Facade**

```csharp
// Grid/GridSystem.cs (80 lines - Coordinator)
public class GridSystem : MonoBehaviour, IGridSystem
{
    // Component references
    private IGridData _gridData;
    private IGridValidation _validation;
    private IGridVisualization _visualization;
    private IGridPlacementLogic _placementLogic;

    private void Awake()
    {
        // Get or create components
        _gridData = GetComponent<GridData>() ?? gameObject.AddComponent<GridData>();
        _validation = GetComponent<GridValidation>() ?? gameObject.AddComponent<GridValidation>();
        _visualization = GetComponent<GridVisualization>() ?? gameObject.AddComponent<GridVisualization>();
        _placementLogic = GetComponent<GridPlacementLogic>() ?? gameObject.AddComponent<GridPlacementLogic>();

        // Register with ServiceContainer
        ServiceContainer.RegisterInstance<IGridSystem>(this);

        ChimeraLogger.Log("GRID", "GridSystem initialized with all components", this);
    }

    // Facade methods - delegate to components
    public bool CanPlace(SchematicSO schematic, Vector3Int position)
        => _validation.CanPlace(schematic, position);

    public async Task<bool> PlaceItemAsync(SchematicSO schematic, Vector3Int position)
        => await _placementLogic.PlaceItemAsync(schematic, position);

    public bool RemoveItem(Vector3Int position)
        => _placementLogic.RemoveItem(position);

    public void ShowGrid(bool visible)
        => _visualization.ShowGrid(visible);

    public void HighlightCells(IEnumerable<Vector3Int> positions, Color color)
        => _visualization.HighlightCells(positions, color);

    public GridCell GetCell(Vector3Int position)
        => _gridData.GetCell(position);

    public IEnumerable<GridCell> GetAllCells()
        => _gridData.GetAllCells();
}
```

---

### Pattern 6C: Data Structure Files

**Example: PlantUpdateDataStructures.cs (506 lines) → 4 files**

**Original:**
```
PlantUpdateDataStructures.cs (506 lines)
├── Plant state data (120 lines)
├── Growth data (130 lines)
├── Environmental data (126 lines)
└── Update results (130 lines)
```

**Refactored:**
```
PlantStateData.cs (120 lines)
PlantGrowthData.cs (130 lines)
PlantEnvironmentalData.cs (126 lines)
PlantUpdateResults.cs (130 lines)
```

**Split by logical grouping:**

```csharp
// Cultivation/Data/PlantStateData.cs
namespace ProjectChimera.Cultivation.Data
{
    public struct PlantState
    {
        public string PlantId;
        public GrowthStage CurrentStage;
        public float Health;
        public float Growth;
        // ... state-related fields only
    }

    public struct PlantHealthState
    {
        public float Health;
        public float MaxHealth;
        public float HealthRegenRate;
        // ... health-related only
    }
}
```

```csharp
// Cultivation/Data/PlantGrowthData.cs
namespace ProjectChimera.Cultivation.Data
{
    public struct PlantGrowthState
    {
        public float GrowthProgress;
        public float GrowthRate;
        public GrowthStage TargetStage;
        // ... growth-related only
    }

    public struct GrowthModifiers
    {
        public float LightModifier;
        public float NutrientModifier;
        public float TemperatureModifier;
        // ... modifiers only
    }
}
```

---

### Pattern 6D: Large Service Classes

**Example: MarketPricingAdapter.cs (907 lines) → 4 services**

**Decomposition Strategy:**
1. Identify responsibilities via method grouping
2. Extract interfaces for each responsibility
3. Create focused service classes
4. Create coordinator/facade if needed

**Responsibilities in MarketPricingAdapter:**
- Price calculation (250 lines)
- Market data retrieval (220 lines)
- Trend analysis (237 lines)
- Price persistence (200 lines)

**Refactored:**
```
PriceCalculationService.cs (250 lines)
MarketDataService.cs (220 lines)
TrendAnalysisService.cs (237 lines)
PricePersistenceService.cs (200 lines)
MarketPricingCoordinator.cs (100 lines - facade)
```

---

### Refactoring Checklist

For each oversized file:

1. **Identify Responsibilities**
   - Read through file, identify distinct concerns
   - Group related methods/fields
   - Aim for 2-4 responsibilities per large file

2. **Define Interfaces**
   - Create interface for each responsibility
   - Keep interfaces focused (5-10 methods max)
   - Use clear, descriptive names

3. **Extract Components**
   - Create new file per responsibility
   - Implement interface
   - Move related code
   - Keep each file <400 lines

4. **Create Coordinator**
   - Optional facade pattern
   - Delegates to components
   - Maintains public API compatibility
   - <100 lines ideally

5. **Update Dependencies**
   - Update ServiceContainer registrations
   - Fix calling code to use interfaces
   - Add unit tests

6. **Validate**
   - Ensure no file >400 lines
   - Run integration tests
   - Check for null references
   - Validate ServiceContainer resolution

---

### Automated Refactoring Tool

```csharp
// Editor/FileSizeRefactoringTool.cs
public class FileSizeRefactoringTool : EditorWindow
{
    [MenuItem("Chimera/Refactoring/File Size Analyzer")]
    public static void ShowWindow()
    {
        GetWindow<FileSizeRefactoringTool>("File Size Refactoring");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Generate Refactoring Plan"))
        {
            GenerateRefactoringPlan();
        }

        if (GUILayout.Button("Validate All Files <400 Lines"))
        {
            ValidateFileSizes();
        }
    }

    private void GenerateRefactoringPlan()
    {
        var oversizedFiles = FindOversizedFiles(400);

        var report = new StringBuilder();
        report.AppendLine("# File Refactoring Plan");
        report.AppendLine($"Total files: {oversizedFiles.Count}");
        report.AppendLine();

        foreach (var file in oversizedFiles.OrderByDescending(f => f.LineCount))
        {
            report.AppendLine($"## {file.Name} ({file.LineCount} lines)");
            report.AppendLine($"Path: {file.Path}");
            report.AppendLine($"Suggested split: {(file.LineCount / 350) + 1} files");
            report.AppendLine();
        }

        File.WriteAllText("Assets/../Documents/Refactoring_Plan_Detailed.md",
                         report.ToString());

        Debug.Log($"Refactoring plan generated for {oversizedFiles.Count} files");
    }

    private void ValidateFileSizes()
    {
        var oversizedFiles = FindOversizedFiles(400);

        if (oversizedFiles.Count == 0)
        {
            Debug.Log("✅ PASSED: All files comply with 400-line limit");
        }
        else
        {
            Debug.LogError($"❌ FAILED: {oversizedFiles.Count} files exceed 400-line limit");

            foreach (var file in oversizedFiles.Take(10))
            {
                Debug.LogError($"  - {file.Name}: {file.LineCount} lines");
            }
        }
    }

    private List<FileInfo> FindOversizedFiles(int maxLines)
    {
        var result = new List<FileInfo>();
        var csFiles = Directory.GetFiles("Assets/ProjectChimera", "*.cs",
                                        SearchOption.AllDirectories);

        foreach (var file in csFiles)
        {
            var lineCount = File.ReadAllLines(file).Length;
            if (lineCount > maxLines)
            {
                result.Add(new FileInfo
                {
                    Path = file,
                    Name = Path.GetFileName(file),
                    LineCount = lineCount
                });
            }
        }

        return result;
    }

    private class FileInfo
    {
        public string Path;
        public string Name;
        public int LineCount;
    }
}
```

---

## VALIDATION & QUALITY GATES

### Comprehensive Validation Script

```csharp
// Editor/ComprehensiveQualityGate.cs
public class ComprehensiveQualityGate
{
    [MenuItem("Chimera/Quality Gates/Run All Validations")]
    public static void RunAllValidations()
    {
        var report = new QualityGateReport();

        report.FindTypeViolations = CountPattern(@"FindObjectOfType");
        report.DebugLogViolations = CountPattern(@"Debug\.Log",
            exclude: new[] { "ChimeraLogger.cs", "Test" });
        report.ResourcesLoadViolations = CountPattern(@"Resources\.Load");
        report.ReflectionViolations = CountPattern(@"GetField|GetProperty|GetMethod");
        report.UpdateMethodViolations = CountPattern(@"void Update\(\)");
        report.FileSizeViolations = CountOversizedFiles(400);

        Debug.Log(GenerateReport(report));

        if (report.HasAnyViolations())
        {
            Debug.LogError("❌ QUALITY GATE FAILED");
        }
        else
        {
            Debug.Log("✅ QUALITY GATE PASSED - Zero violations!");
        }
    }

    private static int CountPattern(string pattern, string[] exclude = null)
    {
        var csFiles = Directory.GetFiles("Assets/ProjectChimera", "*.cs",
                                        SearchOption.AllDirectories);
        int count = 0;

        foreach (var file in csFiles)
        {
            if (exclude != null && exclude.Any(ex => file.Contains(ex)))
                continue;

            var content = File.ReadAllText(file);
            var matches = Regex.Matches(content, pattern);
            count += matches.Count;
        }

        return count;
    }

    private static int CountOversizedFiles(int maxLines)
    {
        var csFiles = Directory.GetFiles("Assets/ProjectChimera", "*.cs",
                                        SearchOption.AllDirectories);
        int count = 0;

        foreach (var file in csFiles)
        {
            var lineCount = File.ReadAllLines(file).Length;
            if (lineCount > maxLines)
                count++;
        }

        return count;
    }

    private static string GenerateReport(QualityGateReport report)
    {
        return $@"
=== QUALITY GATE REPORT ===
FindObjectOfType: {report.FindTypeViolations} (target: 0)
Debug.Log: {report.DebugLogViolations} (target: 0)
Resources.Load: {report.ResourcesLoadViolations} (target: 0)
Reflection: {report.ReflectionViolations} (target: 0)
Update() methods: {report.UpdateMethodViolations} (target: ≤5)
Files >400 lines: {report.FileSizeViolations} (target: 0)

Total Violations: {report.TotalViolations}
Status: {(report.HasAnyViolations() ? "FAILED ❌" : "PASSED ✅")}
";
    }
}

public class QualityGateReport
{
    public int FindTypeViolations;
    public int DebugLogViolations;
    public int ResourcesLoadViolations;
    public int ReflectionViolations;
    public int UpdateMethodViolations;
    public int FileSizeViolations;

    public int TotalViolations =>
        FindTypeViolations + DebugLogViolations + ResourcesLoadViolations +
        ReflectionViolations + Math.Max(0, UpdateMethodViolations - 5) + FileSizeViolations;

    public bool HasAnyViolations() => TotalViolations > 0;
}
```

---

## NEXT STEPS

**After reviewing these patterns:**

1. **Validate understanding** of each pattern
2. **Begin Week 1 Day 1-2** - FindObjectOfType elimination
3. **Use patterns as reference** during migration
4. **Run validation scripts** after each major change
5. **Track progress** via todo list and quality gate reports

---

*End of Migration Patterns Document*
*Ready to begin Phase 0 execution with option 1: FindObjectOfType elimination*
