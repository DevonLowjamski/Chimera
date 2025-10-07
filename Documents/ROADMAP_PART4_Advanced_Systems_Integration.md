# PROJECT CHIMERA: ULTIMATE IMPLEMENTATION ROADMAP
## Part 4: Advanced Systems & UI/UX Implementation

**Document Version:** 2.0 - Updated Based on Comprehensive Codebase Assessment
**Phase Duration:** Weeks 10-13 (4 weeks)
**Prerequisites:** Three pillars at 80%+ completion, blockchain operational

---

## WEEK 10-11: CONTEXTUAL MENU UI SYSTEM

**Current Status:** 30% - Mode switching exists, visual UI missing
**Goal:** Build the wide rectangular bottom-screen menu with tabs, sub-tabs, and mode-specific displays

### Week 10, Day 1-2: Core Menu Framework

**Contextual Menu Architecture:**

```csharp
// UI/ContextualMenu/ContextualMenuController.cs
public class ContextualMenuController : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject _menuContainer;
    [SerializeField] private RectTransform _menuPanel;
    [SerializeField] private CanvasGroup _menuCanvasGroup;

    [Header("Mode Indicators")]
    [SerializeField] private Image _modeIcon;
    [SerializeField] private Text _modeLabel;
    [SerializeField] private GameObject _constructionIndicator;
    [SerializeField] private GameObject _cultivationIndicator;
    [SerializeField] private GameObject _geneticsIndicator;

    [Header("Tab System")]
    [SerializeField] private Transform _tabContainer;
    [SerializeField] private GameObject _tabButtonPrefab;
    [SerializeField] private Transform _contentContainer;

    [Header("Color Themes")]
    [SerializeField] private Color _constructionColor = new Color(0.2f, 0.5f, 1.0f); // Blue
    [SerializeField] private Color _cultivationColor = new Color(0.3f, 0.8f, 0.3f); // Green
    [SerializeField] private Color _geneticsColor = new Color(0.8f, 0.3f, 0.8f); // Purple

    private GameplayMode _currentMode;
    private Dictionary<string, MenuTab> _currentTabs = new();
    private MenuTab _activeTab;

    private IGameplayModeController _modeController;
    private IConstructionManager _constructionManager;
    private ICultivationManager _cultivationManager;
    private IGeneticsService _geneticsService;

    private void Awake()
    {
        _modeController = ServiceContainer.Resolve<IGameplayModeController>();
        _constructionManager = ServiceContainer.Resolve<IConstructionManager>();
        _cultivationManager = ServiceContainer.Resolve<ICultivationManager>();
        _geneticsService = ServiceContainer.Resolve<IGeneticsService>();

        // Subscribe to mode changes
        _modeController.OnModeChanged += OnGameplayModeChanged;
    }

    private void Start()
    {
        // Initialize with current mode
        OnGameplayModeChanged(_modeController.GetCurrentMode());
    }

    private void OnGameplayModeChanged(GameplayMode newMode)
    {
        _currentMode = newMode;

        // Update visual theme
        UpdateModeTheme(newMode);

        // Load mode-specific tabs
        LoadTabsForMode(newMode);

        ChimeraLogger.Log("UI_MENU", $"Contextual menu switched to {newMode} mode", this);
    }

    private void UpdateModeTheme(GameplayMode mode)
    {
        Color themeColor = mode switch
        {
            GameplayMode.Construction => _constructionColor,
            GameplayMode.Cultivation => _cultivationColor,
            GameplayMode.Genetics => _geneticsColor,
            _ => Color.white
        };

        // Update menu border/background tint
        _menuPanel.GetComponent<Image>().color = new Color(themeColor.r, themeColor.g, themeColor.b, 0.3f);

        // Update mode indicators
        _constructionIndicator.SetActive(mode == GameplayMode.Construction);
        _cultivationIndicator.SetActive(mode == GameplayMode.Cultivation);
        _geneticsIndicator.SetActive(mode == GameplayMode.Genetics);

        _modeLabel.text = mode.ToString().ToUpper();
        _modeLabel.color = themeColor;
    }

    private void LoadTabsForMode(GameplayMode mode)
    {
        // Clear existing tabs
        foreach (Transform child in _tabContainer)
            Destroy(child.gameObject);

        _currentTabs.Clear();

        // Load mode-specific tabs
        switch (mode)
        {
            case GameplayMode.Construction:
                LoadConstructionTabs();
                break;

            case GameplayMode.Cultivation:
                LoadCultivationTabs();
                break;

            case GameplayMode.Genetics:
                LoadGeneticsTabs();
                break;
        }

        // Activate first tab
        if (_currentTabs.Count > 0)
        {
            var firstTab = _currentTabs.Values.First();
            ActivateTab(firstTab);
        }
    }

    private void LoadConstructionTabs()
    {
        // Tab 1: Rooms
        var roomsTab = CreateTab("Rooms", "construction_rooms_icon");
        roomsTab.SubTabs = new List<MenuSubTab>
        {
            CreateSubTab("Walls", () => LoadWallOptions()),
            CreateSubTab("Doors", () => LoadDoorOptions()),
            CreateSubTab("Windows", () => LoadWindowOptions()),
            CreateSubTab("Floors", () => LoadFloorOptions())
        };
        _currentTabs[roomsTab.TabId] = roomsTab;

        // Tab 2: Equipment
        var equipmentTab = CreateTab("Equipment", "construction_equipment_icon");
        equipmentTab.SubTabs = new List<MenuSubTab>
        {
            CreateSubTab("Lights", () => LoadLightOptions()),
            CreateSubTab("HVAC", () => LoadHVACOptions()),
            CreateSubTab("Irrigation", () => LoadIrrigationOptions()),
            CreateSubTab("Tables", () => LoadTableOptions()),
            CreateSubTab("Pots", () => LoadPotOptions())
        };
        _currentTabs[equipmentTab.TabId] = equipmentTab;

        // Tab 3: Utilities
        var utilitiesTab = CreateTab("Utilities", "construction_utilities_icon");
        utilitiesTab.SubTabs = new List<MenuSubTab>
        {
            CreateSubTab("Electrical", () => LoadElectricalOptions()),
            CreateSubTab("Plumbing", () => LoadPlumbingOptions()),
            CreateSubTab("Ventilation", () => LoadVentilationOptions())
        };
        _currentTabs[utilitiesTab.TabId] = utilitiesTab;

        // Tab 4: Schematics
        var schematicsTab = CreateTab("Schematics", "construction_schematics_icon");
        schematicsTab.SubTabs = new List<MenuSubTab>
        {
            CreateSubTab("My Schematics", () => LoadMySchematicsOptions()),
            CreateSubTab("Create New", () => LoadSchematicCreator()),
            CreateSubTab("Marketplace", () => LoadSchematicMarketplace())
        };
        _currentTabs[schematicsTab.TabId] = schematicsTab;
    }

    private void LoadCultivationTabs()
    {
        // Tab 1: Tools
        var toolsTab = CreateTab("Tools", "cultivation_tools_icon");
        toolsTab.SubTabs = new List<MenuSubTab>
        {
            CreateSubTab("Watering", () => LoadWateringTools()),
            CreateSubTab("Nutrition", () => LoadNutritionTools()),
            CreateSubTab("Pruning", () => LoadPruningTools()),
            CreateSubTab("Training", () => LoadTrainingTools())
        };
        _currentTabs[toolsTab.TabId] = toolsTab;

        // Tab 2: Environmental Controls
        var environmentTab = CreateTab("Environment", "cultivation_environment_icon");
        environmentTab.SubTabs = new List<MenuSubTab>
        {
            CreateSubTab("Climate", () => LoadClimateControls()),
            CreateSubTab("Lighting", () => LoadLightingControls()),
            CreateSubTab("CO2", () => LoadCO2Controls())
        };
        _currentTabs[environmentTab.TabId] = environmentTab;

        // Tab 3: Plant Care
        var careTab = CreateTab("Plant Care", "cultivation_care_icon");
        careTab.SubTabs = new List<MenuSubTab>
        {
            CreateSubTab("Health", () => LoadHealthMonitoring()),
            CreateSubTab("IPM", () => LoadIPMOptions()),
            CreateSubTab("Harvest", () => LoadHarvestOptions())
        };
        _currentTabs[careTab.TabId] = careTab;

        // Tab 4: Processing
        var processingTab = CreateTab("Processing", "cultivation_processing_icon");
        processingTab.SubTabs = new List<MenuSubTab>
        {
            CreateSubTab("Drying", () => LoadDryingOptions()),
            CreateSubTab("Curing", () => LoadCuringOptions()),
            CreateSubTab("Storage", () => LoadStorageOptions())
        };
        _currentTabs[processingTab.TabId] = processingTab;
    }

    private void LoadGeneticsTabs()
    {
        // Tab 1: Seed Bank
        var seedBankTab = CreateTab("Seed Bank", "genetics_seedbank_icon");
        seedBankTab.SubTabs = new List<MenuSubTab>
        {
            CreateSubTab("My Strains", () => LoadMyStrainsInventory()),
            CreateSubTab("Breeding Queue", () => LoadBreedingQueue()),
            CreateSubTab("Phenotypes", () => LoadPhenotypeLibrary())
        };
        _currentTabs[seedBankTab.TabId] = seedBankTab;

        // Tab 2: Tissue Culture
        var tissueCultureTab = CreateTab("Tissue Culture", "genetics_tissueculture_icon");
        tissueCultureTab.SubTabs = new List<MenuSubTab>
        {
            CreateSubTab("Active Cultures", () => LoadActiveCultures()),
            CreateSubTab("Preserved", () => LoadPreservedCultures()),
            CreateSubTab("Create New", () => LoadCultureCreation())
        };
        _currentTabs[tissueCultureTab.TabId] = tissueCultureTab;

        // Tab 3: Micropropagation
        var micropropagationTab = CreateTab("Micropropagation", "genetics_micropropagation_icon");
        micropropagationTab.SubTabs = new List<MenuSubTab>
        {
            CreateSubTab("Active Batches", () => LoadActiveBatches()),
            CreateSubTab("Start Batch", () => LoadBatchCreation()),
            CreateSubTab("Harvest", () => LoadBatchHarvest())
        };
        _currentTabs[micropropagationTab.TabId] = micropropagationTab;

        // Tab 4: Analysis
        var analysisTab = CreateTab("Analysis", "genetics_analysis_icon");
        analysisTab.SubTabs = new List<MenuSubTab>
        {
            CreateSubTab("Trait Viewer", () => LoadTraitViewer()),
            CreateSubTab("Lineage", () => LoadLineageViewer()),
            CreateSubTab("Verification", () => LoadBlockchainVerification())
        };
        _currentTabs[analysisTab.TabId] = analysisTab;
    }

    private MenuTab CreateTab(string tabName, string iconKey)
    {
        var tabButton = Instantiate(_tabButtonPrefab, _tabContainer);
        var tab = new MenuTab
        {
            TabId = Guid.NewGuid().ToString(),
            TabName = tabName,
            TabButton = tabButton,
            SubTabs = new List<MenuSubTab>()
        };

        // Configure button
        var buttonComponent = tabButton.GetComponent<Button>();
        buttonComponent.onClick.AddListener(() => ActivateTab(tab));

        var buttonText = tabButton.GetComponentInChildren<Text>();
        buttonText.text = tabName;

        // Set icon (load from resources)
        var iconImage = tabButton.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImage != null)
        {
            // TODO: Load actual icon sprite from atlas
            // iconImage.sprite = IconAtlas.GetIcon(iconKey);
        }

        return tab;
    }

    private MenuSubTab CreateSubTab(string subTabName, System.Action onActivate)
    {
        return new MenuSubTab
        {
            SubTabId = Guid.NewGuid().ToString(),
            SubTabName = subTabName,
            OnActivate = onActivate
        };
    }

    private void ActivateTab(MenuTab tab)
    {
        if (_activeTab != null)
        {
            // Deactivate previous tab
            _activeTab.TabButton.GetComponent<Image>().color = Color.gray;
        }

        _activeTab = tab;
        _activeTab.TabButton.GetComponent<Image>().color = Color.white;

        // Load sub-tabs for this tab
        LoadSubTabsUI(tab);
    }

    private void LoadSubTabsUI(MenuTab tab)
    {
        // Clear content area
        foreach (Transform child in _contentContainer)
            Destroy(child.gameObject);

        // Create sub-tab buttons
        foreach (var subTab in tab.SubTabs)
        {
            var subTabButton = Instantiate(_tabButtonPrefab, _contentContainer);
            subTabButton.GetComponentInChildren<Text>().text = subTab.SubTabName;

            var buttonComponent = subTabButton.GetComponent<Button>();
            buttonComponent.onClick.AddListener(() =>
            {
                subTab.OnActivate?.Invoke();
            });
        }

        // Activate first sub-tab automatically
        if (tab.SubTabs.Count > 0)
        {
            tab.SubTabs[0].OnActivate?.Invoke();
        }
    }

    // Example content loaders for Construction mode
    private void LoadWallOptions()
    {
        var walls = _constructionManager.GetAvailableWalls();
        LoadConstructionItems(walls);
    }

    private void LoadLightOptions()
    {
        var lights = _constructionManager.GetAvailableLights();
        LoadConstructionItems(lights);
    }

    private void LoadConstructionItems(List<ConstructionItem> items)
    {
        // Clear existing items
        var itemGrid = _contentContainer.Find("ItemGrid");
        if (itemGrid == null)
        {
            var gridObj = new GameObject("ItemGrid");
            gridObj.transform.SetParent(_contentContainer, false);
            var gridLayout = gridObj.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(100, 120);
            gridLayout.spacing = new Vector2(10, 10);
            itemGrid = gridObj.transform;
        }

        foreach (Transform child in itemGrid)
            Destroy(child.gameObject);

        // Create item cards
        foreach (var item in items)
        {
            CreateItemCard(item, itemGrid);
        }
    }

    private void CreateItemCard(ConstructionItem item, Transform parent)
    {
        var card = new GameObject($"Card_{item.ItemId}");
        card.transform.SetParent(parent, false);

        var cardImage = card.AddComponent<Image>();
        cardImage.sprite = item.IconSprite;

        // Check affordability
        var canAfford = _constructionManager.CanAffordItem(item);
        cardImage.color = canAfford ? Color.white : new Color(0.5f, 0.5f, 0.5f);

        // Add price/quantity label
        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(card.transform, false);
        var labelText = labelObj.AddComponent<Text>();
        labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        labelText.fontSize = 12;
        labelText.alignment = TextAnchor.LowerCenter;

        if (item.IsOwned)
            labelText.text = $"Qty: {item.OwnedQuantity}";
        else
            labelText.text = $"${item.Cost:N0}";

        // Add button functionality
        var button = card.AddComponent<Button>();
        button.onClick.AddListener(() => OnItemSelected(item));

        // Add info button
        var infoButton = new GameObject("InfoButton");
        infoButton.transform.SetParent(card.transform, false);
        var infoImage = infoButton.AddComponent<Image>();
        var infoButtonComponent = infoButton.AddComponent<Button>();
        infoButtonComponent.onClick.AddListener(() => ShowItemInfo(item));
    }

    private void OnItemSelected(ConstructionItem item)
    {
        _constructionManager.SelectItemForPlacement(item);
        ChimeraLogger.Log("UI_MENU", $"Selected {item.ItemName} for placement", this);
    }

    private void ShowItemInfo(ConstructionItem item)
    {
        // Open detailed information panel
        var infoPanel = ServiceContainer.Resolve<IInfoPanelManager>();
        infoPanel.ShowItemDetails(item);
    }

    // Similar loaders for Cultivation and Genetics modes...
    private void LoadWateringTools() { /* Implementation */ }
    private void LoadMyStrainsInventory() { /* Implementation */ }
    private void LoadActiveCultures() { /* Implementation */ }
    // ... etc

    private void OnDestroy()
    {
        if (_modeController != null)
            _modeController.OnModeChanged -= OnGameplayModeChanged;
    }
}

// UI/ContextualMenu/MenuData.cs
[System.Serializable]
public class MenuTab
{
    public string TabId;
    public string TabName;
    public GameObject TabButton;
    public List<MenuSubTab> SubTabs;
}

[System.Serializable]
public class MenuSubTab
{
    public string SubTabId;
    public string SubTabName;
    public System.Action OnActivate;
}

[System.Serializable]
public class ConstructionItem
{
    public string ItemId;
    public string ItemName;
    public Sprite IconSprite;
    public float Cost;
    public bool IsOwned;
    public int OwnedQuantity;
    public string Description;
}
```

### Week 10, Day 3-5: Mode-Specific Content Population

**Genetics Mode - Strain Inventory Display:**

```csharp
// UI/ContextualMenu/GeneticsMenuContent.cs
public class GeneticsMenuContent : MonoBehaviour
{
    [SerializeField] private Transform _strainInventoryContainer;
    [SerializeField] private GameObject _strainCardPrefab;

    private IGeneticsService _geneticsService;
    private IBlockchainGeneticsService _blockchainService;

    public void LoadMyStrainsInventory()
    {
        var myStrains = _geneticsService.GetPlayerStrains();

        // Clear existing
        foreach (Transform child in _strainInventoryContainer)
            Destroy(child.gameObject);

        foreach (var strain in myStrains)
        {
            CreateStrainCard(strain);
        }
    }

    private void CreateStrainCard(PlantGenotype strain)
    {
        var card = Instantiate(_strainCardPrefab, _strainInventoryContainer);
        var cardUI = card.GetComponent<StrainCard>();

        // Populate card data
        cardUI.SetStrainName(strain.StrainName);
        cardUI.SetTraitPreview(strain.GetDominantTraits());

        // Check availability
        var availableSeeds = _geneticsService.GetAvailableSeedCount(strain.GenotypeId);
        if (availableSeeds > 0)
        {
            cardUI.SetQuantity(availableSeeds);
            cardUI.SetSelectable(true);
        }
        else
        {
            cardUI.SetGreyedOut(true);
            cardUI.SetLabel("Out of Stock");
        }

        // Blockchain verification indicator
        var isVerified = _blockchainService.VerifyStrainAuthenticity(strain);
        cardUI.SetVerificationBadge(isVerified);

        // Click handler
        cardUI.OnCardClicked += () => OnStrainSelected(strain);
        cardUI.OnInfoClicked += () => ShowStrainDetails(strain);
    }

    private void OnStrainSelected(PlantGenotype strain)
    {
        // Initiate planting workflow
        var plantingController = ServiceContainer.Resolve<IPlantingController>();
        plantingController.BeginPlanting(strain);
    }

    private void ShowStrainDetails(PlantGenotype strain)
    {
        var detailsPanel = ServiceContainer.Resolve<IStrainDetailsPanel>();
        detailsPanel.Show(strain);
    }
}

// UI/ContextualMenu/StrainCard.cs
public class StrainCard : MonoBehaviour
{
    [SerializeField] private Text _strainNameText;
    [SerializeField] private Text _quantityText;
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private GameObject _verificationBadge;
    [SerializeField] private Button _mainButton;
    [SerializeField] private Button _infoButton;

    public event System.Action OnCardClicked;
    public event System.Action OnInfoClicked;

    private void Awake()
    {
        _mainButton.onClick.AddListener(() => OnCardClicked?.Invoke());
        _infoButton.onClick.AddListener(() => OnInfoClicked?.Invoke());
    }

    public void SetStrainName(string name)
    {
        _strainNameText.text = name;
    }

    public void SetQuantity(int quantity)
    {
        _quantityText.text = $"x{quantity}";
    }

    public void SetLabel(string label)
    {
        _quantityText.text = label;
    }

    public void SetGreyedOut(bool greyedOut)
    {
        _backgroundImage.color = greyedOut ? new Color(0.5f, 0.5f, 0.5f, 0.5f) : Color.white;
        _mainButton.interactable = !greyedOut;
    }

    public void SetSelectable(bool selectable)
    {
        _mainButton.interactable = selectable;
    }

    public void SetVerificationBadge(bool verified)
    {
        _verificationBadge.SetActive(verified);
    }

    public void SetTraitPreview(Dictionary<TraitType, float> dominantTraits)
    {
        // Display mini trait icons/values
        // Implementation depends on UI design
    }
}
```

---

## WEEK 11: TIME MECHANICS SOPHISTICATION & PROGRESSION SYSTEM

### Week 11, Day 1-2: Advanced Time Scale System

**Current Gap:** Basic time control exists, predefined scales and risk/reward not implemented

```csharp
// Core/Time/AdvancedTimeManager.cs
public class AdvancedTimeManager : MonoBehaviour, ITimeManager, ITickable
{
    public int TickPriority => 1000; // Highest priority
    public bool IsTickable => !_isPaused;

    [Header("Time Scales")]
    [SerializeField] private TimeScaleDefinitionSO _timeScaleLibrary;

    private TimeScaleDefinition _currentScale;
    private bool _isPaused;
    private float _gameTimeElapsed; // In game seconds
    private DateTime _gameStartDate;
    private float _transitionProgress = 1f; // 0-1, used for smooth transitions
    private TimeScaleDefinition _targetScale;

    // Risk/reward modifiers
    private Dictionary<string, float> _scaleModifiers = new();

    public void Tick(float deltaTime)
    {
        if (_isPaused) return;

        // Handle time scale transitions (gradual)
        if (_transitionProgress < 1f)
        {
            _transitionProgress += deltaTime * 0.2f; // 5 second transition
            _transitionProgress = Mathf.Min(1f, _transitionProgress);

            if (_transitionProgress >= 1f)
            {
                _currentScale = _targetScale;
                ChimeraLogger.Log("TIME",
                    $"Time scale transition complete: {_currentScale.ScaleName}", this);
            }
        }

        // Calculate effective time scale (blend during transition)
        float effectiveScale = _transitionProgress < 1f
            ? Mathf.Lerp(_currentScale.TimeMultiplier, _targetScale.TimeMultiplier, _transitionProgress)
            : _currentScale.TimeMultiplier;

        // Update game time
        var gameTimeDelta = deltaTime * effectiveScale;
        _gameTimeElapsed += gameTimeDelta;

        // Broadcast time events
        BroadcastTimeEvents(gameTimeDelta);
    }

    public void SetTimeScale(TimeScaleType scaleType)
    {
        var newScale = _timeScaleLibrary.GetScale(scaleType);
        if (newScale == null)
        {
            ChimeraLogger.LogWarning("TIME", $"Invalid time scale: {scaleType}", this);
            return;
        }

        // Check lock-in period
        if (!CanChangeTimeScale())
        {
            ChimeraLogger.LogWarning("TIME",
                $"Cannot change time scale during lock-in period ({GetRemainingLockInTime():F1}s)", this);
            return;
        }

        // Start transition
        _targetScale = newScale;
        _transitionProgress = 0f;

        // Apply lock-in period
        _lastScaleChangeTime = Time.realtimeSinceStartup;

        ChimeraLogger.Log("TIME",
            $"Changing time scale: {_currentScale.ScaleName} → {newScale.ScaleName}", this);
    }

    private float _lastScaleChangeTime;
    private const float LOCK_IN_PERIOD = 300f; // 5 minutes real time

    private bool CanChangeTimeScale()
    {
        if (_lastScaleChangeTime == 0f) return true;
        return (Time.realtimeSinceStartup - _lastScaleChangeTime) >= LOCK_IN_PERIOD;
    }

    private float GetRemainingLockInTime()
    {
        var elapsed = Time.realtimeSinceStartup - _lastScaleChangeTime;
        return Mathf.Max(0f, LOCK_IN_PERIOD - elapsed);
    }

    public float GetGeneticPotentialModifier()
    {
        // Genetic potential reduces at higher time scales (risk/reward)
        // Real-Time: 100%
        // 0.5x: 98%
        // 1x: 95%
        // 2x: 90%
        // 4x: 85%
        // 8x: 75%

        return _currentScale.Type switch
        {
            TimeScaleType.RealTime => 1.00f,
            TimeScaleType.HalfSpeed => 0.98f,
            TimeScaleType.Baseline => 0.95f,
            TimeScaleType.DoubleSpeed => 0.90f,
            TimeScaleType.QuadSpeed => 0.85f,
            TimeScaleType.OctSpeed => 0.75f,
            _ => 1.00f
        };
    }

    public DateTime GetCurrentGameDate()
    {
        return _gameStartDate.AddSeconds(_gameTimeElapsed);
    }

    public float GetSecondsPerGameDay()
    {
        return 86400f / _currentScale.TimeMultiplier;
    }

    public string GetFormattedGameTime()
    {
        var gameDate = GetCurrentGameDate();
        return gameDate.ToString("yyyy-MM-dd HH:mm");
    }

    public void Pause()
    {
        _isPaused = true;
        Time.timeScale = 0f;
        ChimeraLogger.Log("TIME", "Game paused", this);
    }

    public void Resume()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        ChimeraLogger.Log("TIME", "Game resumed", this);
    }

    private void BroadcastTimeEvents(float gameTimeDelta)
    {
        // Broadcast to systems that need to know about time progression
        var timeEvent = ScriptableObject.CreateInstance<TimeProgressionEventSO>();
        timeEvent.GameTimeDelta = gameTimeDelta;
        timeEvent.CurrentGameTime = GetCurrentGameDate();
        timeEvent.TimeScale = _currentScale.TimeMultiplier;
        timeEvent.Raise();
    }

    public TimeScaleDefinition GetCurrentScale() => _currentScale;
}

// Data/Time/TimeScaleDefinitionSO.cs
[CreateAssetMenu(fileName = "TimeScaleLibrary", menuName = "Chimera/Time/Time Scale Library")]
public class TimeScaleDefinitionSO : ScriptableObject
{
    [SerializeField] private List<TimeScaleDefinition> _scales = new()
    {
        new TimeScaleDefinition
        {
            Type = TimeScaleType.RealTime,
            ScaleName = "Real-Time",
            TimeMultiplier = 1f,
            Description = "1 hour real = 1 hour game (1:1)"
        },
        new TimeScaleDefinition
        {
            Type = TimeScaleType.HalfSpeed,
            ScaleName = "0.5x Speed",
            TimeMultiplier = 72f, // 1 day = 20 minutes
            Description = "1 game day = 20 minutes real"
        },
        new TimeScaleDefinition
        {
            Type = TimeScaleType.Baseline,
            ScaleName = "1x Baseline",
            TimeMultiplier = 144f, // 1 week = 1 hour
            Description = "1 game week (6 days) = 1 hour real"
        },
        new TimeScaleDefinition
        {
            Type = TimeScaleType.DoubleSpeed,
            ScaleName = "2x Speed",
            TimeMultiplier = 360f, // 15 days = 1 hour
            Description = "15 game days = 1 hour real"
        },
        new TimeScaleDefinition
        {
            Type = TimeScaleType.QuadSpeed,
            ScaleName = "4x Speed",
            TimeMultiplier = 720f, // 30 days = 1 hour
            Description = "30 game days = 1 hour real"
        },
        new TimeScaleDefinition
        {
            Type = TimeScaleType.OctSpeed,
            ScaleName = "8x Speed",
            TimeMultiplier = 1440f, // 60 days = 1 hour
            Description = "60 game days = 1 hour real"
        }
    };

    public TimeScaleDefinition GetScale(TimeScaleType type)
    {
        return _scales.FirstOrDefault(s => s.Type == type);
    }

    public List<TimeScaleDefinition> GetAllScales() => _scales;
}

[System.Serializable]
public class TimeScaleDefinition
{
    public TimeScaleType Type;
    public string ScaleName;
    public float TimeMultiplier; // Game time / real time
    public string Description;
}

public enum TimeScaleType
{
    RealTime,
    HalfSpeed,
    Baseline,
    DoubleSpeed,
    QuadSpeed,
    OctSpeed
}
```

### Week 11, Day 3-5: Progression System (Skill Tree UI)

**Current Status:** 25% - Data structures only, entire UI missing

**Cannabis Leaf Skill Tree Visualization:**

```csharp
// UI/Progression/SkillTreeUI.cs
public class SkillTreeUI : MonoBehaviour
{
    [Header("Leaf Visualization")]
    [SerializeField] private RectTransform _leafContainer;
    [SerializeField] private GameObject _skillNodePrefab;
    [SerializeField] private LineRenderer _connectionLinePrefab;

    [Header("Branch Positions (5 points of leaf)")]
    [SerializeField] private Vector2[] _branchRootPositions = new Vector2[5];

    private Dictionary<string, SkillNodeUI> _nodeUIs = new();
    private Dictionary<SkillBranch, List<SkillNode>> _skillTree;
    private IProgressionManager _progressionManager;

    private void Awake()
    {
        _progressionManager = ServiceContainer.Resolve<IProgressionManager>();
        LoadSkillTree();
        CreateSkillTreeVisualization();
    }

    private void LoadSkillTree()
    {
        _skillTree = new Dictionary<SkillBranch, List<SkillNode>>();

        // Branch 1: Cultivation (Top point)
        _skillTree[SkillBranch.Cultivation] = new List<SkillNode>
        {
            new SkillNode { NodeId = "cult_basic", NodeName = "Basic Cultivation", Cost = 1, IsUnlocked = true },
            new SkillNode { NodeId = "cult_irrigation", NodeName = "Irrigation Systems", Cost = 2 },
            new SkillNode { NodeId = "cult_nutrition", NodeName = "Advanced Nutrition", Cost = 3 },
            new SkillNode { NodeId = "cult_ipm", NodeName = "Integrated Pest Management", Cost = 5 },
            new SkillNode { NodeId = "cult_training", NodeName = "Plant Training Techniques", Cost = 5 },
            new SkillNode { NodeId = "cult_harvest", NodeName = "Harvest Optimization", Cost = 7 },
            new SkillNode { NodeId = "cult_processing", NodeName = "Post-Harvest Processing", Cost = 8 },
            new SkillNode { NodeId = "cult_master", NodeName = "Master Cultivator", Cost = 10 }
        };

        // Branch 2: Construction (Right point)
        _skillTree[SkillBranch.Construction] = new List<SkillNode>
        {
            new SkillNode { NodeId = "cons_basic", NodeName = "Basic Building", Cost = 1, IsUnlocked = true },
            new SkillNode { NodeId = "cons_plumbing", NodeName = "Plumbing Systems", Cost = 3 },
            new SkillNode { NodeId = "cons_electrical", NodeName = "Electrical Infrastructure", Cost = 3 },
            new SkillNode { NodeId = "cons_hvac", NodeName = "HVAC Integration", Cost = 5 },
            new SkillNode { NodeId = "cons_rooms", NodeName = "Room Specialization", Cost = 5 },
            new SkillNode { NodeId = "cons_schematics", NodeName = "Schematic Mastery", Cost = 7 },
            new SkillNode { NodeId = "cons_automation", NodeName = "Automated Construction", Cost = 10 }
        };

        // Branch 3: Genetics (Bottom left point)
        _skillTree[SkillBranch.Genetics] = new List<SkillNode>
        {
            new SkillNode { NodeId = "gen_basic", NodeName = "Basic Breeding", Cost = 1, IsUnlocked = true },
            new SkillNode { NodeId = "gen_pheno", NodeName = "Pheno-Hunting", Cost = 3 },
            new SkillNode { NodeId = "gen_tissue", NodeName = "Tissue Culture", Cost = 5 },
            new SkillNode { NodeId = "gen_micro", NodeName = "Micropropagation", Cost = 5 },
            new SkillNode { NodeId = "gen_analysis", NodeName = "Genetic Analysis", Cost = 7 },
            new SkillNode { NodeId = "gen_blockchain", NodeName = "Blockchain Verification", Cost = 8 },
            new SkillNode { NodeId = "gen_fractal", NodeName = "Fractal Genetics Mastery", Cost = 10 }
        };

        // Branch 4: Automation (Bottom right point)
        _skillTree[SkillBranch.Automation] = new List<SkillNode>
        {
            new SkillNode { NodeId = "auto_basic", NodeName = "Basic Automation", Cost = 2 },
            new SkillNode { NodeId = "auto_irrigation", NodeName = "Auto-Irrigation", Cost = 3 },
            new SkillNode { NodeId = "auto_climate", NodeName = "Climate Control", Cost = 5 },
            new SkillNode { NodeId = "auto_employee", NodeName = "Hire Employees", Cost = 10 }
        };

        // Branch 5: Research (Left point)
        _skillTree[SkillBranch.Research] = new List<SkillNode>
        {
            new SkillNode { NodeId = "research_lab", NodeName = "Research Lab", Cost = 5 },
            new SkillNode { NodeId = "research_advanced", NodeName = "Advanced Research", Cost = 10 }
        };
    }

    private void CreateSkillTreeVisualization()
    {
        int branchIndex = 0;

        foreach (var branch in _skillTree)
        {
            var branchRoot = _branchRootPositions[branchIndex];
            var nodes = branch.Value;

            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];

                // Calculate position along branch
                var nodePosition = CalculateNodePosition(branchRoot, i, nodes.Count);

                // Create node UI
                var nodeUI = CreateSkillNodeUI(node, nodePosition);
                _nodeUIs[node.NodeId] = nodeUI;

                // Create connection line to previous node
                if (i > 0)
                {
                    var previousNode = _nodeUIs[nodes[i - 1].NodeId];
                    CreateConnectionLine(previousNode.transform.position, nodeUI.transform.position);
                }
            }

            branchIndex++;
        }

        // Create cross-branch connections (interdependencies)
        CreateInterdependencyConnections();
    }

    private Vector2 CalculateNodePosition(Vector2 branchRoot, int nodeIndex, int totalNodes)
    {
        // Distribute nodes along a curve from center to branch tip
        var t = (float)(nodeIndex + 1) / totalNodes;
        var distance = Mathf.Lerp(50f, 300f, t); // Distance from center

        return branchRoot.normalized * distance;
    }

    private SkillNodeUI CreateSkillNodeUI(SkillNode node, Vector2 position)
    {
        var nodeObj = Instantiate(_skillNodePrefab, _leafContainer);
        nodeObj.transform.localPosition = position;

        var nodeUI = nodeObj.GetComponent<SkillNodeUI>();
        nodeUI.Initialize(node);

        // Check if unlocked
        var isUnlocked = _progressionManager.IsNodeUnlocked(node.NodeId);
        nodeUI.SetUnlocked(isUnlocked);

        // Check if affordable
        var canAfford = _progressionManager.GetSkillPoints() >= node.Cost;
        nodeUI.SetAffordable(canAfford);

        // Add click handler
        nodeUI.OnNodeClicked += () => OnSkillNodeClicked(node);

        return nodeUI;
    }

    private void CreateConnectionLine(Vector3 from, Vector3 to)
    {
        var line = Instantiate(_connectionLinePrefab, _leafContainer);
        line.SetPosition(0, from);
        line.SetPosition(1, to);
        line.startWidth = 2f;
        line.endWidth = 2f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = new Color(0.3f, 0.6f, 0.3f);
        line.endColor = new Color(0.3f, 0.6f, 0.3f);
    }

    private void CreateInterdependencyConnections()
    {
        // Example: Genetics "Plant Sex" node affects Cultivation "IPM"
        // Draw dotted line between these nodes
        if (_nodeUIs.TryGetValue("gen_analysis", out var genNode) &&
            _nodeUIs.TryGetValue("cult_ipm", out var cultNode))
        {
            var line = CreateConnectionLine(genNode.transform.position, cultNode.transform.position);
            line.material.SetFloat("_DashSize", 5f); // Dotted line for cross-branch
        }
    }

    private void OnSkillNodeClicked(SkillNode node)
    {
        if (_progressionManager.IsNodeUnlocked(node.NodeId))
        {
            // Already unlocked, show info
            ShowNodeInfo(node);
            return;
        }

        // Check if can unlock
        if (!_progressionManager.CanUnlockNode(node.NodeId))
        {
            ChimeraLogger.LogWarning("PROGRESSION",
                $"Cannot unlock {node.NodeName} - prerequisites not met", this);
            return;
        }

        if (_progressionManager.GetSkillPoints() < node.Cost)
        {
            ChimeraLogger.LogWarning("PROGRESSION",
                $"Cannot unlock {node.NodeName} - insufficient Skill Points ({_progressionManager.GetSkillPoints()}/{node.Cost})", this);
            return;
        }

        // Unlock node
        var success = _progressionManager.UnlockNode(node.NodeId, node.Cost);
        if (success)
        {
            _nodeUIs[node.NodeId].SetUnlocked(true);
            AnimateLeafGrowth();

            ChimeraLogger.Log("PROGRESSION",
                $"Unlocked {node.NodeName}. Remaining Skill Points: {_progressionManager.GetSkillPoints()}", this);
        }
    }

    private void AnimateLeafGrowth()
    {
        // Visual animation: leaf expands slightly when new node unlocked
        StartCoroutine(LeafGrowthAnimation());
    }

    private IEnumerator LeafGrowthAnimation()
    {
        var originalScale = _leafContainer.localScale;
        var targetScale = originalScale * 1.05f;

        // Grow
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 2f;
            _leafContainer.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        // Shrink back
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 2f;
            _leafContainer.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }
    }

    private void ShowNodeInfo(SkillNode node)
    {
        var infoPanel = ServiceContainer.Resolve<IInfoPanelManager>();
        infoPanel.ShowSkillNodeInfo(node);
    }
}

// UI/Progression/SkillNodeUI.cs
public class SkillNodeUI : MonoBehaviour
{
    [SerializeField] private Image _nodeImage;
    [SerializeField] private Text _costText;
    [SerializeField] private GameObject _lockIcon;
    [SerializeField] private Button _button;

    public event System.Action OnNodeClicked;

    private SkillNode _node;

    private void Awake()
    {
        _button.onClick.AddListener(() => OnNodeClicked?.Invoke());
    }

    public void Initialize(SkillNode node)
    {
        _node = node;
        _costText.text = node.Cost.ToString();
    }

    public void SetUnlocked(bool unlocked)
    {
        _lockIcon.SetActive(!unlocked);
        _nodeImage.color = unlocked ? Color.green : Color.gray;
    }

    public void SetAffordable(bool affordable)
    {
        if (!_node.IsUnlocked)
        {
            _costText.color = affordable ? Color.white : Color.red;
        }
    }
}

// Data/Progression/SkillTreeData.cs
public enum SkillBranch
{
    Cultivation,
    Construction,
    Genetics,
    Automation,
    Research
}

[System.Serializable]
public class SkillNode
{
    public string NodeId;
    public string NodeName;
    public int Cost; // Skill Points
    public bool IsUnlocked;
    public string Description;
    public List<string> Prerequisites; // NodeIds that must be unlocked first
}
```

---

## WEEK 12-13: MARKETPLACE & ECONOMY COMPLETION

### Week 12, Day 1-3: External Marketplace Platform

**Current Gap:** Economy exists, no external trading platform for genetics/schematics

```csharp
// Systems/Marketplace/ExternalMarketplaceManager.cs
public class ExternalMarketplaceManager : MonoBehaviour, IMarketplaceManager
{
    private Dictionary<string, MarketplaceListing> _activeListings = new();
    private List<MarketplaceTransaction> _transactionHistory = new();

    private IProgressionManager _progressionManager;
    private IGeneticsService _geneticsService;
    private IConstructionManager _constructionManager;

    public void ListGeneticsForSale(PlantGenotype genotype, int quantity, int skillPointPrice)
    {
        // Verify ownership and availability
        var available = _geneticsService.GetAvailableSeedCount(genotype.GenotypeId);
        if (available < quantity)
        {
            ChimeraLogger.LogWarning("MARKETPLACE",
                $"Cannot list {quantity} seeds - only {available} available", this);
            return;
        }

        // Verify blockchain authenticity
        var blockchainService = ServiceContainer.Resolve<IBlockchainGeneticsService>();
        if (!blockchainService.VerifyStrainAuthenticity(genotype))
        {
            ChimeraLogger.LogWarning("MARKETPLACE",
                $"Cannot list unverified strain {genotype.StrainName}", this);
            return;
        }

        var listing = new MarketplaceListing
        {
            ListingId = Guid.NewGuid().ToString(),
            ListingType = ListingType.Genetics,
            SellerId = GetPlayerId(),
            SellerName = GetPlayerName(),
            ItemId = genotype.GenotypeId,
            ItemName = genotype.StrainName,
            Quantity = quantity,
            PriceSkillPoints = skillPointPrice,
            ListingDate = DateTime.UtcNow,
            IsActive = true,
            GeneticsData = new GeneticsListingData
            {
                Genotype = genotype,
                BlockchainHash = genotype.BlockchainHash,
                TraitSummary = genotype.GetTraitSummary()
            }
        };

        _activeListings[listing.ListingId] = listing;

        // Reserve seeds (remove from player's available stock)
        _geneticsService.ReserveSeeds(genotype.GenotypeId, quantity);

        ChimeraLogger.Log("MARKETPLACE",
            $"Listed {quantity}x {genotype.StrainName} for {skillPointPrice} SP", this);
    }

    public void ListSchematicForSale(SchematicSO schematic, int skillPointPrice)
    {
        var listing = new MarketplaceListing
        {
            ListingId = Guid.NewGuid().ToString(),
            ListingType = ListingType.Schematic,
            SellerId = GetPlayerId(),
            SellerName = GetPlayerName(),
            ItemId = schematic.SchematicId,
            ItemName = schematic.SchematicName,
            Quantity = 1, // Schematics are infinitely available once purchased
            PriceSkillPoints = skillPointPrice,
            ListingDate = DateTime.UtcNow,
            IsActive = true,
            SchematicData = new SchematicListingData
            {
                Schematic = schematic,
                PreviewImage = schematic.PreviewSprite,
                MaterialCost = schematic.GetTotalMaterialCost(),
                Description = schematic.Description
            }
        };

        _activeListings[listing.ListingId] = listing;

        ChimeraLogger.Log("MARKETPLACE",
            $"Listed schematic '{schematic.SchematicName}' for {skillPointPrice} SP", this);
    }

    public async Task<bool> PurchaseListingAsync(string listingId)
    {
        if (!_activeListings.TryGetValue(listingId, out var listing))
        {
            ChimeraLogger.LogWarning("MARKETPLACE", $"Listing {listingId} not found", this);
            return false;
        }

        // Check Skill Points
        if (_progressionManager.GetSkillPoints() < listing.PriceSkillPoints)
        {
            ChimeraLogger.LogWarning("MARKETPLACE",
                $"Insufficient Skill Points ({_progressionManager.GetSkillPoints()}/{listing.PriceSkillPoints})", this);
            return false;
        }

        // Process purchase
        switch (listing.ListingType)
        {
            case ListingType.Genetics:
                return await PurchaseGeneticsAsync(listing);

            case ListingType.Schematic:
                return await PurchaseSchematicAsync(listing);

            default:
                return false;
        }
    }

    private async Task<bool> PurchaseGeneticsAsync(MarketplaceListing listing)
    {
        // Deduct Skill Points
        _progressionManager.SpendSkillPoints(listing.PriceSkillPoints);

        // Add genetics to player's seed bank
        var genotype = listing.GeneticsData.Genotype;
        _geneticsService.AddSeedsToInventory(genotype.GenotypeId, listing.Quantity);

        // Transfer Skill Points to seller (system-wide, handled by backend)
        await TransferSkillPointsToSellerAsync(listing.SellerId, listing.PriceSkillPoints);

        // Reduce quantity or remove listing
        listing.Quantity--;
        if (listing.Quantity <= 0)
        {
            listing.IsActive = false;
            _activeListings.Remove(listing.ListingId);
        }

        // Record transaction
        RecordTransaction(listing, TransactionType.Purchase);

        ChimeraLogger.Log("MARKETPLACE",
            $"Purchased {genotype.StrainName} for {listing.PriceSkillPoints} SP", this);

        return true;
    }

    private async Task<bool> PurchaseSchematicAsync(MarketplaceListing listing)
    {
        // Deduct Skill Points
        _progressionManager.SpendSkillPoints(listing.PriceSkillPoints);

        // Add schematic to player's library
        var schematic = listing.SchematicData.Schematic;
        _constructionManager.UnlockSchematic(schematic);

        // Transfer Skill Points to seller
        await TransferSkillPointsToSellerAsync(listing.SellerId, listing.PriceSkillPoints);

        // Record transaction
        RecordTransaction(listing, TransactionType.Purchase);

        ChimeraLogger.Log("MARKETPLACE",
            $"Purchased schematic '{schematic.SchematicName}' for {listing.PriceSkillPoints} SP", this);

        return true;
    }

    private async Task TransferSkillPointsToSellerAsync(string sellerId, int amount)
    {
        // In a real implementation, this would communicate with backend server
        // For single-player, we simulate the transfer
        await Task.Delay(100);

        ChimeraLogger.Log("MARKETPLACE",
            $"Transferred {amount} SP to seller {sellerId}", this);
    }

    private void RecordTransaction(MarketplaceListing listing, TransactionType type)
    {
        var transaction = new MarketplaceTransaction
        {
            TransactionId = Guid.NewGuid().ToString(),
            ListingId = listing.ListingId,
            BuyerId = GetPlayerId(),
            SellerId = listing.SellerId,
            Type = type,
            SkillPointsTransferred = listing.PriceSkillPoints,
            Timestamp = DateTime.UtcNow
        };

        _transactionHistory.Add(transaction);
    }

    public List<MarketplaceListing> SearchListings(string searchQuery, ListingType? filterType = null)
    {
        var results = _activeListings.Values.Where(l => l.IsActive);

        if (!string.IsNullOrEmpty(searchQuery))
        {
            results = results.Where(l => l.ItemName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));
        }

        if (filterType.HasValue)
        {
            results = results.Where(l => l.ListingType == filterType.Value);
        }

        return results.OrderByDescending(l => l.ListingDate).ToList();
    }

    public List<MarketplaceListing> GetMyListings()
    {
        var playerId = GetPlayerId();
        return _activeListings.Values.Where(l => l.SellerId == playerId).ToList();
    }

    public void CancelListing(string listingId)
    {
        if (!_activeListings.TryGetValue(listingId, out var listing))
            return;

        if (listing.SellerId != GetPlayerId())
        {
            ChimeraLogger.LogWarning("MARKETPLACE",
                "Cannot cancel listing - not the seller", this);
            return;
        }

        // Return reserved items to seller
        if (listing.ListingType == ListingType.Genetics)
        {
            _geneticsService.UnreserveSeeds(listing.ItemId, listing.Quantity);
        }

        _activeListings.Remove(listingId);
        listing.IsActive = false;

        ChimeraLogger.Log("MARKETPLACE",
            $"Cancelled listing: {listing.ItemName}", this);
    }

    private string GetPlayerId()
    {
        // Return unique player ID (account-based)
        return SystemInfo.deviceUniqueIdentifier;
    }

    private string GetPlayerName()
    {
        // Return player's display name
        return "Player"; // TODO: Implement profile system
    }
}

// Data/Marketplace/MarketplaceData.cs
public enum ListingType
{
    Genetics,
    Schematic
}

public enum TransactionType
{
    Purchase,
    Sale
}

[System.Serializable]
public class MarketplaceListing
{
    public string ListingId;
    public ListingType ListingType;
    public string SellerId;
    public string SellerName;
    public string ItemId;
    public string ItemName;
    public int Quantity;
    public int PriceSkillPoints;
    public DateTime ListingDate;
    public bool IsActive;

    public GeneticsListingData GeneticsData;
    public SchematicListingData SchematicData;
}

[System.Serializable]
public class GeneticsListingData
{
    public PlantGenotype Genotype;
    public string BlockchainHash;
    public Dictionary<TraitType, float> TraitSummary;
}

[System.Serializable]
public class SchematicListingData
{
    public SchematicSO Schematic;
    public Sprite PreviewImage;
    public float MaterialCost;
    public string Description;
}

[System.Serializable]
public class MarketplaceTransaction
{
    public string TransactionId;
    public string ListingId;
    public string BuyerId;
    public string SellerId;
    public TransactionType Type;
    public int SkillPointsTransferred;
    public DateTime Timestamp;
}
```

### Week 12, Day 4-5 & Week 13, Day 1-2: Marketplace UI

```csharp
// UI/Marketplace/MarketplaceUI.cs
public class MarketplaceUI : MonoBehaviour
{
    [Header("Search")]
    [SerializeField] private InputField _searchInput;
    [SerializeField] private Dropdown _filterDropdown;
    [SerializeField] private Button _searchButton;

    [Header("Listings Display")]
    [SerializeField] private Transform _listingsContainer;
    [SerializeField] private GameObject _listingCardPrefab;

    [Header("My Listings")]
    [SerializeField] private Transform _myListingsContainer;
    [SerializeField] private Button _createListingButton;

    [Header("Create Listing Panel")]
    [SerializeField] private GameObject _createListingPanel;
    [SerializeField] private Dropdown _itemSelectDropdown;
    [SerializeField] private InputField _quantityInput;
    [SerializeField] private InputField _priceInput;
    [SerializeField] private Button _confirmListingButton;

    private IMarketplaceManager _marketplace;

    private void Awake()
    {
        _marketplace = ServiceContainer.Resolve<IMarketplaceManager>();

        _searchButton.onClick.AddListener(OnSearchClicked);
        _createListingButton.onClick.AddListener(ShowCreateListingPanel);
        _confirmListingButton.onClick.AddListener(OnConfirmListingClicked);
    }

    private void Start()
    {
        RefreshListings();
        RefreshMyListings();
    }

    private void OnSearchClicked()
    {
        var searchQuery = _searchInput.text;
        ListingType? filterType = _filterDropdown.value switch
        {
            0 => null, // All
            1 => ListingType.Genetics,
            2 => ListingType.Schematic,
            _ => null
        };

        var results = _marketplace.SearchListings(searchQuery, filterType);
        DisplayListings(results);
    }

    private void DisplayListings(List<MarketplaceListing> listings)
    {
        // Clear existing
        foreach (Transform child in _listingsContainer)
            Destroy(child.gameObject);

        foreach (var listing in listings)
        {
            var card = Instantiate(_listingCardPrefab, _listingsContainer);
            var cardUI = card.GetComponent<MarketplaceListingCard>();
            cardUI.SetListing(listing);
            cardUI.OnPurchaseClicked += () => OnPurchaseClicked(listing);
        }
    }

    private async void OnPurchaseClicked(MarketplaceListing listing)
    {
        var success = await _marketplace.PurchaseListingAsync(listing.ListingId);

        if (success)
        {
            RefreshListings();
            ShowPurchaseConfirmation(listing);
        }
    }

    private void RefreshListings()
    {
        var allListings = _marketplace.SearchListings("");
        DisplayListings(allListings);
    }

    private void RefreshMyListings()
    {
        var myListings = _marketplace.GetMyListings();

        foreach (Transform child in _myListingsContainer)
            Destroy(child.gameObject);

        foreach (var listing in myListings)
        {
            var card = Instantiate(_listingCardPrefab, _myListingsContainer);
            var cardUI = card.GetComponent<MarketplaceListingCard>();
            cardUI.SetListing(listing);
            cardUI.SetOwnerMode(true);
            cardUI.OnCancelClicked += () => OnCancelListingClicked(listing);
        }
    }

    private void ShowCreateListingPanel()
    {
        _createListingPanel.SetActive(true);
        PopulateItemDropdown();
    }

    private void PopulateItemDropdown()
    {
        // Get player's available genetics and schematics
        var geneticsService = ServiceContainer.Resolve<IGeneticsService>();
        var constructionManager = ServiceContainer.Resolve<IConstructionManager>();

        var availableItems = new List<string>();

        // Add genetics
        foreach (var genotype in geneticsService.GetPlayerStrains())
        {
            availableItems.Add($"[G] {genotype.StrainName}");
        }

        // Add schematics
        foreach (var schematic in constructionManager.GetUnlockedSchematics())
        {
            availableItems.Add($"[S] {schematic.SchematicName}");
        }

        _itemSelectDropdown.ClearOptions();
        _itemSelectDropdown.AddOptions(availableItems);
    }

    private void OnConfirmListingClicked()
    {
        var selectedItem = _itemSelectDropdown.options[_itemSelectDropdown.value].text;
        var quantity = int.Parse(_quantityInput.text);
        var price = int.Parse(_priceInput.text);

        if (selectedItem.StartsWith("[G]"))
        {
            // Genetics listing
            var strainName = selectedItem.Substring(4);
            var geneticsService = ServiceContainer.Resolve<IGeneticsService>();
            var genotype = geneticsService.GetStrainByName(strainName);

            _marketplace.ListGeneticsForSale(genotype, quantity, price);
        }
        else if (selectedItem.StartsWith("[S]"))
        {
            // Schematic listing
            var schematicName = selectedItem.Substring(4);
            var constructionManager = ServiceContainer.Resolve<IConstructionManager>();
            var schematic = constructionManager.GetSchematicByName(schematicName);

            _marketplace.ListSchematicForSale(schematic, price);
        }

        _createListingPanel.SetActive(false);
        RefreshMyListings();
    }

    private void OnCancelListingClicked(MarketplaceListing listing)
    {
        _marketplace.CancelListing(listing.ListingId);
        RefreshMyListings();
    }

    private void ShowPurchaseConfirmation(MarketplaceListing listing)
    {
        // Show confirmation popup
        ChimeraLogger.Log("MARKETPLACE",
            $"Successfully purchased {listing.ItemName}", this);
    }
}
```

---

## SUCCESS METRICS - WEEK 10-13

**Contextual Menu UI:**
- ✅ Bottom-screen rectangular menu with mode-specific colors
- ✅ Tab system with 4 tabs per mode (Construction, Cultivation, Genetics)
- ✅ Sub-tab navigation functional
- ✅ Item display with costs, inventory quantities, greyed-out unaffordable items
- ✅ Mode switching updates menu automatically

**Time Mechanics:**
- ✅ 6 predefined time scales (Real-Time through 8x)
- ✅ Smooth transitions with inertia (5 seconds)
- ✅ Lock-in period (5 minutes) prevents abuse
- ✅ Risk/reward: genetic potential reduces at higher scales
- ✅ Offline progression framework ready

**Progression System:**
- ✅ Cannabis leaf visualization with 5 branches
- ✅ 40+ skill nodes across all branches
- ✅ Skill Point economy integrated
- ✅ Visual leaf growth animation on unlock
- ✅ Cross-branch interdependencies visualized

**Marketplace:**
- ✅ Genetics trading with Skill Points
- ✅ Schematic trading with Skill Points
- ✅ Search and filter functionality
- ✅ Blockchain verification for genetics
- ✅ Transaction history tracking
- ✅ Create/cancel listing management

---

*End of Part 4: Advanced Systems & UI/UX (Weeks 10-13)*
*Continue to Part 5: Phase 2 Preparation & Validation (Weeks 14-20)*
