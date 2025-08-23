using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core;
using ProjectChimera.Core.Events;
using ProjectChimera.Data.Construction;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Grid-based construction management system for Project Chimera.
    /// Serves as the primary shell coordinating the GridSystem, GridPlacementController,
    /// and ConstructionCostManager for comprehensive construction management.
    /// </summary>
    public class ConstructionManager : DIChimeraManager
    {
        [Header("Grid Construction System")]
        [SerializeField] private GridSystem _gridSystem;
        [SerializeField] private GridPlacementController _placementController;
        [SerializeField] private InteractiveFacilityConstructor _facilityConstructor;
        [SerializeField] private ConstructionCostManager _costManager;
        
        [Header("Construction Catalog")]
        [SerializeField] private ConstructionCatalog _constructionCatalog;
        
        [Header("Schematic Integration")]
        [SerializeField] private bool _enableSchematicIntegration = true;
        
        [Header("Construction Configuration")]
        [SerializeField] private bool _enableCostTracking = true;
        [SerializeField] private bool _enableResourceTracking = true;
        [SerializeField] private bool _enableProgressTracking = true;
        [SerializeField] private float _autoSaveInterval = 300f; // 5 minutes
        
        [Header("Event Channels")]
        [SerializeField] private SimpleGameEventSO _onProjectStarted;
        [SerializeField] private SimpleGameEventSO _onProjectCompleted;
        [SerializeField] private SimpleGameEventSO _onConstructionProgress;
        [SerializeField] private SimpleGameEventSO _onConstructionError;
        
        // Grid-based construction tracking
        private Dictionary<string, GridConstructionProject> _activeProjects = new Dictionary<string, GridConstructionProject>();
        private Dictionary<Vector3Int, string> _reservedPositions = new Dictionary<Vector3Int, string>();
        private Dictionary<string, GridConstructionProgress> _projectProgress = new Dictionary<string, GridConstructionProgress>();
        
        // Schematic integration
        private SchematicConstructionIntegration _schematicIntegration;
        
        // Construction metrics and performance
        private GridConstructionMetrics _constructionMetrics = new GridConstructionMetrics();
        private float _lastAutoSave = 0f;
        
        public override ManagerPriority Priority => ManagerPriority.High;
        
        // Public Properties
        public GridSystem GridSystem => _gridSystem;
        public GridPlacementController PlacementController => _placementController;
        public ConstructionCostManager CostManager => _costManager;
        public ConstructionCatalog ConstructionCatalog => _constructionCatalog;
        public InteractiveFacilityConstructor FacilityConstructor => _facilityConstructor;
        public Dictionary<string, GridConstructionProject> ActiveProjects => _activeProjects;
        public GridConstructionMetrics ConstructionMetrics => _constructionMetrics;
        public int ActiveProjectCount => _activeProjects.Count;
        public bool IsInPlacementMode => _placementController != null && _placementController.IsInPlacementMode;
        
        // Events
        public System.Action<string, GridConstructionProject> OnProjectStarted;
        public System.Action<string, GridConstructionProject> OnProjectCompleted;
        public System.Action<GridPlaceable> OnObjectPlaced;
        public System.Action<GridPlaceable> OnObjectRemoved;
        public System.Action<string, float> OnConstructionProgress;
        public System.Action<string, string> OnConstructionError;
        
        protected override void OnManagerInitialize()
        {
            InitializeGridConstructionSystems();
            InitializeConstructionTracking();
            InitializeSchematicIntegration();
            SubscribeToEvents();
            
            _constructionMetrics = new GridConstructionMetrics();
            
            LogInfo("Grid-based ConstructionManager initialized successfully");
        }
        
        private void Update()
        {
            if (!IsInitialized) return;
            
            float currentTime = Time.time;
            
            UpdateConstructionProgress();
            UpdateConstructionMetrics();
            
            // Auto-save periodically
            if (currentTime - _lastAutoSave >= _autoSaveInterval)
            {
                SaveConstructionState();
                _lastAutoSave = currentTime;
            }
        }
        
        protected override void OnManagerShutdown()
        {
            UnsubscribeFromEvents();
            SaveConstructionState();
            
            _activeProjects.Clear();
            _reservedPositions.Clear();
            _projectProgress.Clear();
            
            LogInfo("Grid-based ConstructionManager shutdown completed");
        }
        
        #region Public API - Grid Construction Management
        
        /// <summary>
        /// Get active construction project by ID
        /// </summary>
        public GridConstructionProject GetProject(string projectId)
        {
            return _activeProjects.GetValueOrDefault(projectId);
        }
        
        /// <summary>
        /// Start construction from template at grid coordinates
        /// </summary>
        public string StartConstruction(GridConstructionTemplate template, Vector3Int gridCoordinate, int rotation = 0)
        {
            if (template == null)
            {
                HandleConstructionError("Cannot start construction - template is null");
                return null;
            }
            
            // Check if position is available
            if (IsPositionReserved(gridCoordinate))
            {
                HandleConstructionError($"Position {gridCoordinate} is already reserved");
                return null;
            }
            
            // Check resource availability
            if (_enableResourceTracking && _costManager != null && !_costManager.CheckResourceAvailability(template))
            {
                HandleConstructionError($"Insufficient resources for {template.TemplateName}");
                return null;
            }
            
            // Create construction project
            var project = CreateGridConstructionProject(template, gridCoordinate, rotation);
            
            // Reserve position
            _reservedPositions[gridCoordinate] = project.ProjectId;
            
            // Create cost estimate and budget if cost tracking is enabled
            if (_enableCostTracking && _costManager != null)
            {
                var costEstimate = _costManager.CreateCostEstimate(project.ProjectId, template);
                var budget = _costManager.CreateProjectBudget(project.ProjectId, costEstimate, costEstimate.TotalCost);
                
                // Allocate resources
                if (_enableResourceTracking)
                {
                    _costManager.AllocateResources(project.ProjectId, template);
                }
            }
            
            // Store project
            _activeProjects[project.ProjectId] = project;
            
            // Initialize progress tracking
            if (_enableProgressTracking)
            {
                _projectProgress[project.ProjectId] = new GridConstructionProgress
                {
                    ProjectId = project.ProjectId,
                    Progress = 0f,
                    StartTime = DateTime.Now,
                    EstimatedCompletion = DateTime.Now.AddSeconds(template.ConstructionTime)
                };
            }
            
            // Update project status to in progress
            project.Status = GridConstructionStatus.InProgress;
            
            // Trigger events
            OnProjectStarted?.Invoke(project.ProjectId, project);
            _onProjectStarted?.Raise();
            
            LogInfo($"Started construction project: {template.TemplateName} at {gridCoordinate}");
            return project.ProjectId;
        }
        
        /// <summary>
        /// Start construction from catalog template by name
        /// </summary>
        public string StartConstructionByName(string templateName, Vector3Int gridCoordinate, int rotation = 0)
        {
            if (_constructionCatalog == null)
            {
                HandleConstructionError("Cannot start construction - no construction catalog available");
                return null;
            }
            
            var template = _constructionCatalog.FindTemplate(templateName);
            if (template == null)
            {
                HandleConstructionError($"Template '{templateName}' not found in catalog");
                return null;
            }
            
            return StartConstruction(template, gridCoordinate, rotation);
        }
        
        /// <summary>
        /// Get available construction templates, filtered by unlocked schematics
        /// </summary>
        public List<GridConstructionTemplate> GetAvailableTemplates()
        {
            if (_constructionCatalog == null)
                return new List<GridConstructionTemplate>();
            
            var allTemplates = _constructionCatalog.Templates;
            
            // If schematic integration is disabled, return all templates
            if (!_enableSchematicIntegration || _schematicIntegration == null)
                return allTemplates;
            
            // Filter templates based on unlocked schematics
            var unlockedTemplateNames = _schematicIntegration.GetUnlockedConstructionTemplates();
            var availableTemplates = new List<GridConstructionTemplate>();
            
            foreach (var template in allTemplates)
            {
                // Check if template requires schematic unlock
                bool requiresSchematicUnlock = template.RequiredUnlocks.Any(unlock => 
                    unlock.StartsWith("Schematic_") || unlockedTemplateNames.Contains(unlock));
                
                if (!requiresSchematicUnlock)
                {
                    // Template doesn't require schematic unlock - always available
                    availableTemplates.Add(template);
                }
                else
                {
                    // Check if the required schematic is unlocked
                    bool isUnlocked = template.RequiredUnlocks.Any(unlock => 
                        unlockedTemplateNames.Contains(unlock) || 
                        _schematicIntegration.IsConstructionTemplateUnlocked(template.TemplateName));
                    
                    if (isUnlocked)
                    {
                        availableTemplates.Add(template);
                    }
                }
            }
            
            return availableTemplates;
        }
        
        /// <summary>
        /// Get all construction templates (ignoring unlock status) 
        /// </summary>
        public List<GridConstructionTemplate> GetAllTemplates()
        {
            if (_constructionCatalog == null)
                return new List<GridConstructionTemplate>();
            
            return _constructionCatalog.Templates;
        }
        
        /// <summary>
        /// Complete construction project
        /// </summary>
        public bool CompleteConstruction(string projectId)
        {
            if (!_activeProjects.TryGetValue(projectId, out var project))
            {
                HandleConstructionError($"Project {projectId} not found");
                return false;
            }
            
            // Update project status
            project.Status = GridConstructionStatus.Completed;
            project.CompletionTime = DateTime.Now;
            
            // Update progress
            if (_projectProgress.TryGetValue(projectId, out var progress))
            {
                progress.Progress = 1f;
                progress.ActualCompletion = DateTime.Now;
            }
            
            // Place the actual object on the grid
            if (_gridSystem != null && project.Template.Prefab != null)
            {
                var worldPosition = _gridSystem.GridToWorldPosition(project.GridCoordinate);
                var placedObject = Instantiate(project.Template.Prefab, worldPosition, Quaternion.Euler(0, project.Rotation * 90, 0));
                
                var placeable = placedObject.GetComponent<GridPlaceable>();
                if (placeable != null)
                {
                    // Use the simplified placement API on GridPlaceable
                    placeable.GridCoordinate = project.GridCoordinate;
                    placeable.SetRotation(project.Rotation * 90f);
                    placeable.PlaceAt(_gridSystem.GridToWorldPosition(project.GridCoordinate));
                    OnObjectPlaced?.Invoke(placeable);
                }
            }
            
            // Remove position reservation
            _reservedPositions.Remove(project.GridCoordinate);
            
            // Consume resources
            if (_enableResourceTracking && _costManager != null)
            {
                _costManager.ConsumeResources(project.Template);
            }
            
            // Update metrics
            _constructionMetrics.TotalProjectsCompleted++;
            
            // Trigger events
            OnProjectCompleted?.Invoke(projectId, project);
            _onProjectCompleted?.Raise();
            
            LogInfo($"Completed construction project: {project.Template.TemplateName}");
            return true;
        }
        
        /// <summary>
        /// Cancel construction project
        /// </summary>
        public bool CancelConstruction(string projectId)
        {
            if (!_activeProjects.TryGetValue(projectId, out var project))
            {
                HandleConstructionError($"Project {projectId} not found");
                return false;
            }
            
            // Update project status
            project.Status = GridConstructionStatus.Cancelled;
            
            // Remove position reservation
            _reservedPositions.Remove(project.GridCoordinate);
            
            // Remove from tracking
            _activeProjects.Remove(projectId);
            _projectProgress.Remove(projectId);
            
            // Update metrics
            _constructionMetrics.TotalProjectsCancelled++;
            
            LogInfo($"Cancelled construction project: {project.Template.TemplateName}");
            return true;
        }
        
        /// <summary>
        /// Start placement mode for template
        /// </summary>
        public void StartPlacementMode(string templateName)
        {
            if (_facilityConstructor != null)
            {
                _facilityConstructor.StartPlacement(templateName);
            }
            else
            {
                HandleConstructionError("Cannot start placement mode - InteractiveFacilityConstructor not available");
            }
        }
        
        /// <summary>
        /// Cancel current placement mode
        /// </summary>
        public void CancelPlacementMode()
        {
            if (_facilityConstructor != null)
            {
                _facilityConstructor.CancelPlacement();
            }
        }
        
        /// <summary>
        /// Check if can afford construction template
        /// </summary>
        public bool CanAffordConstruction(string templateName, float availableFunds)
        {
            if (_constructionCatalog == null || _costManager == null)
                return false;
            
            var template = _constructionCatalog.FindTemplate(templateName);
            if (template == null)
                return false;
            
            return _costManager.CanAfford(template, availableFunds);
        }
        
        /// <summary>
        /// Get quick cost estimate for template
        /// </summary>
        public float GetQuickCostEstimate(string templateName)
        {
            if (_constructionCatalog == null || _costManager == null)
                return 0f;
            
            var template = _constructionCatalog.FindTemplate(templateName);
            if (template == null)
                return 0f;
            
            return _costManager.GetQuickCostEstimate(template);
        }
        
        #endregion
        
        #region Private Implementation
        
        private void InitializeGridConstructionSystems()
        {
            // Find core system components
            if (_gridSystem == null) _gridSystem = FindObjectOfType<GridSystem>();
            if (_placementController == null) _placementController = FindObjectOfType<GridPlacementController>();
            if (_facilityConstructor == null) _facilityConstructor = FindObjectOfType<InteractiveFacilityConstructor>();
            if (_costManager == null) _costManager = FindObjectOfType<ConstructionCostManager>();
            
            // Validate critical components
            if (_gridSystem == null)
            {
                LogError("GridSystem not found - construction system cannot function properly");
            }
            
            if (_constructionCatalog == null)
            {
                LogWarning("ConstructionCatalog not assigned - no templates available for construction");
            }
        }
        
        private void InitializeConstructionTracking()
        {
            _activeProjects.Clear();
            _reservedPositions.Clear();
            _projectProgress.Clear();
            
            _constructionMetrics = new GridConstructionMetrics
            {
                TotalProjectsStarted = 0,
                TotalProjectsCompleted = 0,
                TotalProjectsCancelled = 0,
                TotalBudgetAllocated = 0f,
                TotalBudgetSpent = 0f,
                ActiveProjects = 0,
                LastUpdated = DateTime.Now
            };
        }
        
        private void InitializeSchematicIntegration()
        {
            if (!_enableSchematicIntegration)
            {
                LogInfo("Schematic integration disabled for ConstructionManager");
                return;
            }
            
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                _schematicIntegration = gameManager.GetManager<SchematicConstructionIntegration>();
            }
            
            if (_schematicIntegration == null)
            {
                LogWarning("SchematicConstructionIntegration service not found - construction templates will not be filtered by schematics");
            }
            else
            {
                LogInfo($"Schematic integration initialized - {_schematicIntegration.IntegratedSchematicsCount} schematics integrated");
            }
        }
        
        private void SubscribeToEvents()
        {
            if (_facilityConstructor != null)
            {
                _facilityConstructor.OnObjectPlaced += HandleObjectPlaced;
                _facilityConstructor.OnObjectRemoved += HandleObjectRemoved;
                _facilityConstructor.OnError += HandleConstructionError;
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            if (_facilityConstructor != null)
            {
                _facilityConstructor.OnObjectPlaced -= HandleObjectPlaced;
                _facilityConstructor.OnObjectRemoved -= HandleObjectRemoved;
                _facilityConstructor.OnError -= HandleConstructionError;
            }
        }
        
        private GridConstructionProject CreateGridConstructionProject(GridConstructionTemplate template, Vector3Int gridCoordinate, int rotation)
        {
            return new GridConstructionProject
            {
                ProjectId = Guid.NewGuid().ToString(),
                Template = template,
                GridCoordinate = gridCoordinate,
                Rotation = rotation,
                Status = GridConstructionStatus.Planning,
                Progress = 0f,
                StartTime = DateTime.Now,
                EstimatedCompletion = DateTime.Now.AddSeconds(template.ConstructionTime)
            };
        }
        
        private void UpdateConstructionProgress()
        {
            if (!_enableProgressTracking) return;
            
            foreach (var kvp in _projectProgress.ToList())
            {
                var progress = kvp.Value;
                if (progress.Progress >= 1f) continue;
                
                var project = _activeProjects.GetValueOrDefault(progress.ProjectId);
                if (project == null || project.Status != GridConstructionStatus.InProgress) continue;
                
                // Update progress based on time
                var elapsed = DateTime.Now - progress.StartTime;
                var totalDuration = progress.EstimatedCompletion - progress.StartTime;
                var newProgress = Mathf.Clamp01((float)(elapsed.TotalSeconds / totalDuration.TotalSeconds));
                
                if (newProgress != progress.Progress)
                {
                    progress.Progress = newProgress;
                    project.Progress = newProgress;
                    
                    OnConstructionProgress?.Invoke(progress.ProjectId, newProgress);
                    _onConstructionProgress?.Raise();
                    
                    // Auto-complete when progress reaches 100%
                    if (newProgress >= 1f)
                    {
                        CompleteConstruction(progress.ProjectId);
                    }
                }
            }
        }
        
        private void UpdateConstructionMetrics()
        {
            _constructionMetrics.ActiveProjects = _activeProjects.Count;
            _constructionMetrics.TotalProjectsStarted = _constructionMetrics.TotalProjectsCompleted + 
                                                       _constructionMetrics.TotalProjectsCancelled + 
                                                       _constructionMetrics.ActiveProjects;
            _constructionMetrics.LastUpdated = DateTime.Now;
            
            // Update budget metrics from cost manager
            if (_costManager != null)
            {
                _constructionMetrics.TotalBudgetAllocated = _costManager.TotalBudgetAllocated;
                _constructionMetrics.TotalBudgetSpent = _costManager.TotalBudgetSpent;
            }
        }
        
        private void SaveConstructionState()
        {
            // Auto-save construction state
            // Implementation depends on save system architecture
            LogInfo("Construction state saved");
        }
        
        private void HandleObjectPlaced(GridPlaceable placeable)
        {
            OnObjectPlaced?.Invoke(placeable);
        }
        
        private void HandleObjectRemoved(GridPlaceable placeable)
        {
            OnObjectRemoved?.Invoke(placeable);
        }
        
        private void HandleConstructionError(string error)
        {
            LogError($"Construction Error: {error}");
            OnConstructionError?.Invoke("ConstructionManager", error);
            _onConstructionError?.Raise();
        }
        
        /// <summary>
        /// Check if grid position is reserved for construction
        /// </summary>
        public bool IsPositionReserved(Vector3Int gridCoordinate)
        {
            return _reservedPositions.ContainsKey(gridCoordinate);
        }
        
        /// <summary>
        /// Get construction progress for project
        /// </summary>
        public float GetConstructionProgress(string projectId)
        {
            var progress = _projectProgress.GetValueOrDefault(projectId);
            return progress?.Progress ?? 0f;
        }
        
        #endregion
    }
    
    #region Supporting Data Structures
    
    [System.Serializable]
    public class GridConstructionProject
    {
        public string ProjectId;
        public GridConstructionTemplate Template;
        public Vector3Int GridCoordinate;
        public int Rotation;
        public GridConstructionStatus Status;
        public float Progress;
        public DateTime StartTime;
        public DateTime EstimatedCompletion;
        public DateTime CompletionTime;
    }
    
    [System.Serializable]
    public class GridConstructionProgress
    {
        public string ProjectId;
        public float Progress;
        public DateTime StartTime;
        public DateTime EstimatedCompletion;
        public DateTime ActualCompletion;
    }
    
    [System.Serializable]
    public class GridConstructionMetrics
    {
        public int TotalProjectsStarted;
        public int TotalProjectsCompleted;
        public int TotalProjectsCancelled;
        public int ActiveProjects;
        public float TotalBudgetAllocated;
        public float TotalBudgetSpent;
        public DateTime LastUpdated;
    }
    
    public enum GridConstructionStatus
    {
        Planning,
        InProgress,
        Completed,
        Cancelled,
        OnHold
    }
    
    #endregion
}