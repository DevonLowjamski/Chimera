using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Systems.Services.Commands;
using ProjectChimera.Core;
using ProjectChimera.Data.Genetics;

namespace ProjectChimera.Systems.Services.Core
{
    /// <summary>
    /// Phase 2.1: Service Layer Coordinator
    /// Coordinates between UI contextual menu system and backend service implementations
    /// Implements the service layer pattern between UI and Manager layers
    /// </summary>
    public class ServiceLayerCoordinator : MonoBehaviour
    {
        [Header("Service Layer Configuration")]
        [SerializeField] private bool _enableServiceValidation = true;
        [SerializeField] private bool _enableCommandLogging = true;
        
        [Header("Service References")]
        [SerializeField] private MonoBehaviour _contextualMenuController;
        
        // Service interfaces - will be injected via DI
        private IConstructionService _constructionService;
        private ICultivationService _cultivationService;
        private IGeneticsService _geneticsService;
        
        // Manager interfaces - will be injected via DI
        private IEconomyManager _economyManager;
        private IProgressionManager _progressionManager;
        private ITimeManager _timeManager;
        
        // Command registries for each pillar
        private readonly Dictionary<string, IMenuCommand> _constructionCommands = new Dictionary<string, IMenuCommand>();
        private readonly Dictionary<string, IMenuCommand> _cultivationCommands = new Dictionary<string, IMenuCommand>();
        private readonly Dictionary<string, IMenuCommand> _geneticsCommands = new Dictionary<string, IMenuCommand>();
        
        // Events for service layer communication
        public event Action<string, CommandResult> OnCommandExecuted;
        public event Action<string, string> OnServiceError;
        public event Action<string> OnServiceStateChanged;
        
        private bool _isInitialized = false;
        
        private void Awake()
        {
            // Validate contextual menu reference
            if (_contextualMenuController == null)
            {
                // Find ContextualMenuController using reflection to avoid assembly dependency
                var contextualMenuControllers = FindObjectsOfType<MonoBehaviour>();
                foreach (var controller in contextualMenuControllers)
                {
                    if (controller.GetType().Name == "ContextualMenuController")
                    {
                        _contextualMenuController = controller;
                        break;
                    }
                }
                
                if (_contextualMenuController == null)
                {
                    Debug.LogError("[ServiceLayerCoordinator] ContextualMenuController not found!");
                    return;
                }
            }
        }
        
        private void Start()
        {
            // Initialize after other systems are ready
            InitializeServiceLayer();
        }
        
        /// <summary>
        /// Initialize the service layer and wire up commands
        /// </summary>
        public void InitializeServiceLayer()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[ServiceLayerCoordinator] Already initialized");
                return;
            }
            
            try
            {
                // Resolve service dependencies via DI
                ResolveDependencies();
                
                // Register commands for each pillar
                RegisterConstructionCommands();
                RegisterCultivationCommands();
                RegisterGeneticsCommands();
                
                // Wire up contextual menu events
                WireContextualMenuEvents();
                
                // Connect time manager events
                ConnectTimeManagerEvents();
                
                _isInitialized = true;
                OnServiceStateChanged?.Invoke("ServiceLayer_Initialized");
                
                if (_enableCommandLogging)
                {
                    Debug.Log("[ServiceLayerCoordinator] Service layer initialized successfully");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ServiceLayerCoordinator] Failed to initialize service layer: {ex.Message}");
                OnServiceError?.Invoke("ServiceLayer", ex.Message);
            }
        }
        
        /// <summary>
        /// Resolve service dependencies through DI system
        /// </summary>
        private void ResolveDependencies()
        {
            // Get managers through the DI system
            var gameManager = FindObjectOfType<DIGameManager>();
            if (gameManager == null)
            {
                throw new InvalidOperationException("DIGameManager not found - required for service resolution");
            }
            
            // Resolve manager interfaces using reflection to avoid generic constraints
            _economyManager = FindManagerComponent<IEconomyManager>();
            _progressionManager = FindManagerComponent<IProgressionManager>();
            _timeManager = FindManagerComponent<ITimeManager>();
            
            // For now, create mock service implementations
            // In a full implementation, these would be resolved via DI as well
            _constructionService = new MockConstructionService();
            _cultivationService = new MockCultivationService();
            _geneticsService = new MockGeneticsService();
            
            // Initialize services
            _constructionService?.Initialize();
            _cultivationService?.Initialize();
            _geneticsService?.Initialize();
        }
        
        /// <summary>
        /// Register construction pillar commands
        /// </summary>
        private void RegisterConstructionCommands()
        {
            // Example construction commands - in full implementation, these would be registered dynamically
            var placeWallCommand = new PlaceStructureCommand("wall", Vector3Int.zero, _constructionService, _economyManager);
            var placeLightCommand = new PlaceEquipmentCommand("grow_light", Vector3Int.zero, _constructionService, _economyManager);
            var applySchematicCommand = new ApplySchematicCommand("basic_room", Vector3Int.zero, _constructionService, _economyManager);
            
            _constructionCommands[placeWallCommand.CommandId] = placeWallCommand;
            _constructionCommands[placeLightCommand.CommandId] = placeLightCommand;
            _constructionCommands[applySchematicCommand.CommandId] = applySchematicCommand;
            
            if (_enableCommandLogging)
            {
                Debug.Log($"[ServiceLayerCoordinator] Registered {_constructionCommands.Count} construction commands");
            }
        }
        
        /// <summary>
        /// Register cultivation pillar commands
        /// </summary>
        private void RegisterCultivationCommands()
        {
            // Example cultivation commands
            var plantSeedCommand = new PlantSeedCommand("og_kush", Vector3Int.zero, "My Plant", _cultivationService, _economyManager);
            var waterPlantCommand = new WaterPlantCommand("plant_001", _cultivationService, _economyManager);
            var feedPlantCommand = new FeedPlantCommand("plant_001", _cultivationService, _economyManager);
            var harvestCommand = new HarvestPlantCommand("plant_001", _cultivationService, _economyManager);
            
            _cultivationCommands[plantSeedCommand.CommandId] = plantSeedCommand;
            _cultivationCommands[waterPlantCommand.CommandId] = waterPlantCommand;
            _cultivationCommands[feedPlantCommand.CommandId] = feedPlantCommand;
            _cultivationCommands[harvestCommand.CommandId] = harvestCommand;
            
            if (_enableCommandLogging)
            {
                Debug.Log($"[ServiceLayerCoordinator] Registered {_cultivationCommands.Count} cultivation commands");
            }
        }
        
        /// <summary>
        /// Register genetics pillar commands
        /// </summary>
        private void RegisterGeneticsCommands()
        {
            // Example genetics commands
            var breedCommand = new BreedPlantsCommand("plant_001", "plant_002", _geneticsService, _progressionManager);
            var tissueCultureCommand = new CreateTissueCultureCommand("plant_001", "Culture_001", _geneticsService, _progressionManager);
            var micropropagateCommand = new MicropropagateCommand("culture_001", 5, _geneticsService, _progressionManager);
            var researchCommand = new ResearchTraitCommand("thc_content", _geneticsService, _progressionManager);
            
            _geneticsCommands[breedCommand.CommandId] = breedCommand;
            _geneticsCommands[tissueCultureCommand.CommandId] = tissueCultureCommand;
            _geneticsCommands[micropropagateCommand.CommandId] = micropropagateCommand;
            _geneticsCommands[researchCommand.CommandId] = researchCommand;
            
            if (_enableCommandLogging)
            {
                Debug.Log($"[ServiceLayerCoordinator] Registered {_geneticsCommands.Count} genetics commands");
            }
        }
        
        /// <summary>
        /// Wire up contextual menu events to service commands
        /// </summary>
        private void WireContextualMenuEvents()
        {
            if (_contextualMenuController == null) return;
            
            // Subscribe to contextual menu events using reflection
            var eventInfo = _contextualMenuController.GetType().GetEvent("OnMenuItemSelected");
            if (eventInfo != null)
            {
                var methodInfo = GetType().GetMethod("HandleMenuItemSelected", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (methodInfo != null)
                {
                    var handler = System.Delegate.CreateDelegate(eventInfo.EventHandlerType, this, methodInfo);
                    eventInfo.AddEventHandler(_contextualMenuController, handler);
                }
            }
            
            // Register all commands with the contextual menu system
            RegisterCommandsWithMenuSystem();
        }
        
        /// <summary>
        /// Register all commands with the contextual menu command manager
        /// </summary>
        private void RegisterCommandsWithMenuSystem()
        {
            // Find ContextualMenuEventHandler using reflection to avoid assembly dependency
            MonoBehaviour eventHandler = null;
            var components = _contextualMenuController.GetComponents<MonoBehaviour>();
            foreach (var component in components)
            {
                if (component.GetType().Name == "ContextualMenuEventHandler")
                {
                    eventHandler = component;
                    break;
                }
            }
            
            if (eventHandler == null)
            {
                Debug.LogError("[ServiceLayerCoordinator] ContextualMenuEventHandler not found!");
                return;
            }
            
            // Register commands using reflection
            var registerMethod = eventHandler.GetType().GetMethod("RegisterCommand");
            if (registerMethod != null)
            {
                // Register construction commands
                foreach (var kvp in _constructionCommands)
                {
                    registerMethod.Invoke(eventHandler, new object[] { kvp.Key, kvp.Value });
                }
                
                // Register cultivation commands
                foreach (var kvp in _cultivationCommands)
                {
                    registerMethod.Invoke(eventHandler, new object[] { kvp.Key, kvp.Value });
                }
                
                // Register genetics commands
                foreach (var kvp in _geneticsCommands)
                {
                    registerMethod.Invoke(eventHandler, new object[] { kvp.Key, kvp.Value });
                }
            }
        }
        
        /// <summary>
        /// Handle menu item selection and route to appropriate service command
        /// </summary>
        public void HandleMenuItemSelected(string mode, string commandId)
        {
            try
            {
                IMenuCommand command = null;
                
                // Route command based on mode/pillar
                switch (mode.ToLower())
                {
                    case "construction":
                        _constructionCommands.TryGetValue(commandId, out command);
                        break;
                    case "cultivation":
                        _cultivationCommands.TryGetValue(commandId, out command);
                        break;
                    case "genetics":
                        _geneticsCommands.TryGetValue(commandId, out command);
                        break;
                    default:
                        Debug.LogWarning($"[ServiceLayerCoordinator] Unknown mode: {mode}");
                        return;
                }
                
                if (command == null)
                {
                    Debug.LogWarning($"[ServiceLayerCoordinator] Command not found: {commandId} in mode {mode}");
                    return;
                }
                
                // Execute command with validation
                if (_enableServiceValidation && !command.CanExecute())
                {
                    OnServiceError?.Invoke(mode, $"Command {commandId} cannot be executed");
                    return;
                }
                
                var result = command.Execute();
                OnCommandExecuted?.Invoke(commandId, result);
                
                if (_enableCommandLogging)
                {
                    Debug.Log($"[ServiceLayerCoordinator] Executed {commandId}: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ServiceLayerCoordinator] Error handling menu item selection: {ex.Message}");
                OnServiceError?.Invoke(mode, ex.Message);
            }
        }
        
        /// <summary>
        /// Connect time manager events for time-based service updates
        /// </summary>
        private void ConnectTimeManagerEvents()
        {
            if (_timeManager == null) return;
            
            // Listen for time-related events to coordinate service updates
            // This will be expanded when time-based service updates are implemented
            OnServiceStateChanged?.Invoke("TimeManager_Connected");
        }
        
        /// <summary>
        /// Get command by ID for external access
        /// </summary>
        public IMenuCommand GetCommand(string commandId)
        {
            // Search all command registries
            if (_constructionCommands.TryGetValue(commandId, out var constructionCmd))
                return constructionCmd;
            if (_cultivationCommands.TryGetValue(commandId, out var cultivationCmd))
                return cultivationCmd;
            if (_geneticsCommands.TryGetValue(commandId, out var geneticsCmd))
                return geneticsCmd;
                
            return null;
        }
        
        /// <summary>
        /// Get all commands for a specific pillar/mode
        /// </summary>
        public IEnumerable<IMenuCommand> GetCommandsForMode(string mode)
        {
            switch (mode.ToLower())
            {
                case "construction":
                    return _constructionCommands.Values;
                case "cultivation":
                    return _cultivationCommands.Values;
                case "genetics":
                    return _geneticsCommands.Values;
                default:
                    return new List<IMenuCommand>();
            }
        }
        
        /// <summary>
        /// Helper method to find manager components using reflection to avoid generic constraints
        /// </summary>
        private T FindManagerComponent<T>() where T : class
        {
            var allManagers = FindObjectsOfType<MonoBehaviour>();
            foreach (var manager in allManagers)
            {
                if (manager is T managerInterface)
                {
                    return managerInterface;
                }
            }
            return null;
        }

        private void OnDestroy()
        {
            // Cleanup events using reflection
            if (_contextualMenuController != null)
            {
                var eventInfo = _contextualMenuController.GetType().GetEvent("OnMenuItemSelected");
                if (eventInfo != null)
                {
                    var methodInfo = GetType().GetMethod("HandleMenuItemSelected", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (methodInfo != null)
                    {
                        var handler = System.Delegate.CreateDelegate(eventInfo.EventHandlerType, this, methodInfo);
                        eventInfo.RemoveEventHandler(_contextualMenuController, handler);
                    }
                }
            }
            
            // Shutdown services
            _constructionService?.Shutdown();
            _cultivationService?.Shutdown();
            _geneticsService?.Shutdown();
        }
    }
    
    // Mock service implementations for testing - these would be replaced with real implementations
    public class MockConstructionService : IConstructionService
    {
        public bool IsInitialized { get; private set; }
        
        public void Initialize() => IsInitialized = true;
        public void Shutdown() => IsInitialized = false;
        
        public bool CanPlaceStructure(string structureId, Vector3Int gridPosition) => true;
        public bool PlaceStructure(string structureId, Vector3Int gridPosition) => true;
        public bool RemoveStructure(Vector3Int gridPosition) => true;
        public bool CanAffordStructure(string structureId) => true;
        public bool CanPlaceEquipment(string equipmentId, Vector3Int gridPosition) => true;
        public bool PlaceEquipment(string equipmentId, Vector3Int gridPosition) => true;
        public bool RemoveEquipment(Vector3Int gridPosition) => true;
        public bool CanAffordEquipment(string equipmentId) => true;
        public bool InstallUtility(string utilityType, Vector3Int startPosition, Vector3Int endPosition) => true;
        public bool RemoveUtility(Vector3Int position) => true;
        public bool CanAffordUtility(string utilityType, float length) => true;
        public bool CanApplySchematic(string schematicId, Vector3Int position) => true;
        public bool ApplySchematic(string schematicId, Vector3Int position) => true;
        public bool SaveSchematic(string name, Vector3Int startPosition, Vector3Int endPosition) => true;
        public float GetStructureCost(string structureId) => 100f;
        public float GetEquipmentCost(string equipmentId) => 200f;
        public float GetUtilityCost(string utilityType, float length) => 50f * length;
        public bool IsPositionOccupied(Vector3Int gridPosition) => false;
    }
    
    public class MockCultivationService : ICultivationService
    {
        public bool IsInitialized { get; private set; }
        
        public void Initialize() => IsInitialized = true;
        public void Shutdown() => IsInitialized = false;
        
        public bool CanPlantSeed(string strainId, Vector3Int gridPosition) => true;
        public bool PlantSeed(string strainId, Vector3Int gridPosition, string plantName = null) => true;
        public bool RemovePlant(string plantId) => true;
        public bool HarvestPlant(string plantId) => true;
        public bool WaterPlant(string plantId) => true;
        public bool FeedPlant(string plantId) => true;
        public bool TrainPlant(string plantId, string trainingType) => true;
        public bool PrunePlant(string plantId) => true;
        public bool SetEnvironmentalConditions(string zoneId, ProjectChimera.Data.Shared.EnvironmentalConditions conditions) => true;
        public ProjectChimera.Data.Shared.EnvironmentalConditions GetEnvironmentalConditions(string zoneId) => new ProjectChimera.Data.Shared.EnvironmentalConditions();
        public bool CanAdjustEnvironment(string zoneId) => true;
        public int GetPlantCount() => 5;
        public int GetHealthyPlantCount() => 4;
        public int GetHarvestReadyPlantCount() => 1;
        public float GetAverageHealthScore() => 0.85f;
        public bool HasPlantsNeedingAttention() => true;
    }
    
    public class MockGeneticsService : IGeneticsService
    {
        public bool IsInitialized { get; private set; }
        
        public void Initialize() => IsInitialized = true;
        public void Shutdown() => IsInitialized = false;
        
        public bool CanBreedPlants(string parentId1, string parentId2) => true;
        public bool BreedPlants(string parentId1, string parentId2, out string newStrainId) { newStrainId = "hybrid_001"; return true; }
        public bool CanCreateTissueCulture(string plantId) => true;
        public bool CreateTissueCulture(string plantId, string cultureName) => true;
        public bool CanMicropropagate(string cultureId) => true;
        public bool Micropropagate(string cultureId, int quantity, out string[] seedIds) { seedIds = new string[quantity]; return true; }
        public PlantStrainSO GetStrain(string strainId) => null;
        public PlantStrainSO[] GetAvailableStrains() => new PlantStrainSO[0];
        public bool IsStrainUnlocked(string strainId) => true;
        public bool HasStrain(string strainId) => true;
        public bool HasSeeds(string strainId) => true;
        public int GetSeedCount(string strainId) => 10;
        public bool CanAffordSeeds(string strainId, int quantity) => true;
        public bool PurchaseSeeds(string strainId, int quantity) => true;
        public bool CanResearchTrait(string traitId) => true;
        public bool ResearchTrait(string traitId) => true;
        public bool IsTraitDiscovered(string traitId) => false;
        public int GetDiscoveredTraitCount() => 5;
        public int GetAvailableStrainCount() => 3;
        public float GetBreedingSuccessRate(string parentId1, string parentId2) => 0.75f;
    }
}