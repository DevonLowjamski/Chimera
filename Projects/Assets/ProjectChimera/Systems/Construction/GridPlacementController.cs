using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Systems.Construction;
using ProjectChimera.Data.Construction;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Lightweight orchestrator for 3D grid placement system.
    /// Delegates all functionality to specialized 3D-aware components.
    /// Refactored from 2,692-line monolith to modular architecture.
    /// </summary>
    public class GridPlacementController : MonoBehaviour
    {
        [Header("Component References")]
        [SerializeField] private GridInputHandler _inputHandler;
        [SerializeField] private GridPlacementValidator _validator;
        [SerializeField] private GridPlacementPreviewRenderer _previewRenderer;
        [SerializeField] private SchematicPlacementHandler _schematicHandler;
        [SerializeField] private PlacementPaymentService _paymentService;
        [SerializeField] private GridSelectionManager _selectionManager;
        [SerializeField] private VerticalPlacementManager _verticalManager;
        [SerializeField] private GridSystem _gridSystem;
        
        [Header("3D Placement Configuration")]
        [SerializeField] private bool _enable3DPlacement = true;
        [SerializeField] private bool _enableVerticalSnapping = true;
        [SerializeField] private bool _enableHeightValidation = true;
        [SerializeField] private bool _enableDebugLogging = false;
        
        // Public API - maintains compatibility
        public bool IsInPlacementMode { get; private set; }
        public bool IsInSchematicPlacementMode => _schematicHandler?.IsActive ?? false;
        public Vector3Int CurrentGridPosition => _inputHandler?.CurrentGridCoordinate ?? Vector3Int.zero;
        public bool IsValidPlacement => _validator?.LastValidationResult?.IsValid ?? false;
        
        // Events - forward from components
        public System.Action<GridPlaceable> OnObjectPlaced;
        public System.Action<GridPlaceable> OnObjectRemoved;
        public System.Action<Vector3Int> OnGridPositionChanged;
        
        // Legacy compatibility properties
        public List<GridPlaceable> SelectedObjects => _selectionManager?.SelectedObjects ?? new List<GridPlaceable>();
        public List<ConstructionCategory> ActiveFilterCategories => _selectionManager?.ActiveFilterCategories ?? new List<ConstructionCategory>();
        public Dictionary<ConstructionCategory, int> CategoryCounts => _selectionManager?.CategoryCounts ?? new Dictionary<ConstructionCategory, int>();
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void Start()
        {
            WireComponentEvents();
            ValidateSystemIntegrity();
        }
        
        private void Update()
        {
            if (IsInPlacementMode)
            {
                _inputHandler?.ProcessPlacementInput();
                _previewRenderer?.UpdatePreview();
            }
            
            // Let individual components handle their own updates
        }
        
        private void InitializeComponents()
        {
            // Find or create required components
            if (_gridSystem == null) _gridSystem = FindObjectOfType<GridSystem>();
            if (_inputHandler == null) _inputHandler = GetComponent<GridInputHandler>();
            if (_validator == null) _validator = GetComponent<GridPlacementValidator>();
            if (_previewRenderer == null) _previewRenderer = GetComponent<GridPlacementPreviewRenderer>();
            if (_selectionManager == null) _selectionManager = GetComponent<GridSelectionManager>();
            
            // Initialize 3D systems
            if (_enable3DPlacement && _verticalManager == null)
                _verticalManager = GetComponent<VerticalPlacementManager>();
                
            LogDebug("Grid placement components initialized");
        }
        
        private void WireComponentEvents()
        {
            // Wire input events
            if (_inputHandler != null)
            {
                _inputHandler.OnPlacementRequested += () => HandlePlacementRequest();
                _inputHandler.OnCancelRequested += HandleCancelRequest;
                _inputHandler.OnGridCoordinateChanged += HandleGridCoordinateChanged;
            }
            
            // Wire validation events
            if (_validator != null)
            {
                _validator.OnValidationCompleted += HandleValidationResult;
            }
            
            // Wire selection manager events
            if (_selectionManager != null)
            {
                _selectionManager.OnSelectionChanged += HandleSelectionChanged;
            }
            
            LogDebug("Component events wired");
        }
        
        // Public API methods - delegate to components
        public void EnterPlacementMode(GridPlaceable placeable)
        {
            IsInPlacementMode = true;
            _inputHandler?.SetPlacementTarget(placeable);
            _previewRenderer?.ShowPreview(placeable);
            LogDebug($"Entered placement mode for {placeable.name}");
        }
        
        public void ExitPlacementMode()
        {
            IsInPlacementMode = false;
            _inputHandler?.ClearPlacementTarget();
            _previewRenderer?.HidePreview();
            LogDebug("Exited placement mode");
        }
        
        public bool PlaceObject(GridPlaceable placeable, Vector3Int gridPosition)
        {
            // Orchestrate placement through components
            if (!_validator.ValidatePosition(gridPosition, placeable))
            {
                LogDebug($"Placement validation failed at {gridPosition}");
                return false;
            }
            
            if (!_paymentService.ValidateAndReserveFunds(placeable))
            {
                LogDebug("Payment validation failed");
                return false;
            }
            
            // Execute placement
            if (_gridSystem.PlaceObject(placeable, gridPosition))
            {
                _paymentService.CompletePurchase(placeable);
                OnObjectPlaced?.Invoke(placeable);
                LogDebug($"Successfully placed {placeable.name} at {gridPosition}");
                return true;
            }
            
            return false;
        }
        
        // Legacy API methods - delegate to appropriate components
        public void StartPlacement(GameObject prefab)
        {
            var placeable = prefab.GetComponent<GridPlaceable>();
            if (placeable != null)
                EnterPlacementMode(placeable);
        }
        
        public void StartPlacement(GridPlaceable placeable)
        {
            EnterPlacementMode(placeable);
        }
        
        public void CancelPlacement()
        {
            ExitPlacementMode();
        }
        
        public void SelectObject(GridPlaceable placeable)
        {
            _selectionManager?.SelectObject(placeable);
        }
        
        public void ClearSelection()
        {
            _selectionManager?.ClearSelection();
        }
        
        public void RemoveSelectedObject()
        {
            _selectionManager?.RemoveSelectedObjects();
        }
        
        public void ClearMultipleSelection()
        {
            _selectionManager?.ClearMultipleSelection();
        }
        
        public void AddToSelection(GridPlaceable obj)
        {
            _selectionManager?.AddToSelection(obj);
        }
        
        public void RemoveFromSelection(GridPlaceable obj)
        {
            _selectionManager?.RemoveFromSelection(obj);
        }
        
        public bool IsObjectSelected(GridPlaceable obj)
        {
            return _selectionManager?.IsObjectSelected(obj) ?? false;
        }
        
        // Category filtering methods - delegate to selection manager
        public void SetCategoryFilter(List<ConstructionCategory> categories)
        {
            _selectionManager?.SetCategoryFilter(categories);
        }
        
        public void AddCategoryToFilter(ConstructionCategory category)
        {
            _selectionManager?.AddCategoryToFilter(category);
        }
        
        public void RemoveCategoryFromFilter(ConstructionCategory category)
        {
            _selectionManager?.RemoveCategoryFromFilter(category);
        }
        
        public void ToggleCategoryFilter(ConstructionCategory category)
        {
            _selectionManager?.ToggleCategoryFilter(category);
        }
        
        public List<GridPlaceable> GetObjectsByCategory(ConstructionCategory category)
        {
            return _selectionManager?.GetObjectsByCategory(category) ?? new List<GridPlaceable>();
        }
        
        public List<GridPlaceable> GetFilteredObjects()
        {
            return _selectionManager?.GetFilteredObjects() ?? new List<GridPlaceable>();
        }
        
        public void EnableCategoryFiltering(bool enable)
        {
            _selectionManager?.EnableCategoryFiltering(enable);
        }
        
        // Schematic methods - delegate to schematic handler
        public SchematicSO CreateSchematicFromSelection(string schematicName, string description = "", string createdBy = "Player")
        {
            return _schematicHandler?.CreateSchematicFromSelection(schematicName, description, createdBy);
        }
        
        public bool SaveSchematicToAssets(SchematicSO schematic, string folderPath = "Assets/ProjectChimera/Data/Schematics/")
        {
            return _schematicHandler?.SaveSchematicToAssets(schematic, folderPath) ?? false;
        }
        
        public void StartSchematicPlacement(SchematicSO schematic)
        {
            _schematicHandler?.StartSchematicPlacement(schematic);
        }
        
        public bool ApplySchematic(SchematicSO schematic, Vector3Int gridPosition)
        {
            bool result = _schematicHandler?.ApplySchematic(schematic, gridPosition) ?? false;
            if (result)
            {
                // Fire event when schematic is successfully applied
                OnSchematicApplied?.Invoke(schematic, gridPosition, new List<GameObject>());
            }
            return result;
        }
        
        public void CancelSchematicPlacement()
        {
            _schematicHandler?.CancelSchematicPlacement();
        }
        
        public void SaveSelectionAsSchematic()
        {
            _schematicHandler?.SaveSelectionAsSchematic();
        }
        
        // Events
        public System.Action<SchematicSO, Vector3Int, List<GameObject>> OnSchematicApplied;
        
        // Properties for external access
        public SchematicSO CurrentSchematic => _schematicHandler?.CurrentSchematic;
        
        public bool IsSchematicPlacementValid(Vector3Int position)
        {
            return _schematicHandler?.IsSchematicPlacementValid(position) ?? false;
        }
        
        public GridPlaceable SelectedObject => _selectionManager?.GetSelectedObject();
        
        // Payment system methods
        public void UpdatePlayerFunds(float funds)
        {
            _paymentService?.UpdatePlayerFunds(funds);
        }
        
        public void UpdatePlayerResources(Dictionary<string, int> resources)
        {
            _paymentService?.UpdatePlayerResources(resources);
        }
        
        // Utility methods
        public string GetValidationSummary()
        {
            return _validator?.GetValidationSummary() ?? "Validation not available";
        }
        
        public List<GridPlaceable> GetObjectsNearPosition(Vector3 worldPosition, float radius)
        {
            return _gridSystem?.GetObjectsNearPosition(worldPosition, radius) ?? new List<GridPlaceable>();
        }
        
        // Event handlers - orchestrate component interactions
        private void HandlePlacementRequest()
        {
            var gridPosition = _inputHandler?.CurrentGridCoordinate ?? Vector3Int.zero;
            HandlePlacementRequest(gridPosition);
        }
        
        private void HandlePlacementRequest(Vector3Int gridPosition)
        {
            if (IsInPlacementMode && _inputHandler.CurrentPlaceable != null)
            {
                PlaceObject(_inputHandler.CurrentPlaceable, gridPosition);
            }
        }
        
        private void HandleCancelRequest()
        {
            ExitPlacementMode();
        }
        
        private void HandleGridCoordinateChanged(Vector3Int newPosition)
        {
            OnGridPositionChanged?.Invoke(newPosition);
            
            // Update preview position
            _previewRenderer?.UpdatePreviewPosition(newPosition);
            
            // Validate new position
            if (_inputHandler.CurrentPlaceable != null)
            {
                _validator?.ValidatePosition(newPosition, _inputHandler.CurrentPlaceable);
            }
        }
        
        private void HandleValidationResult(Vector3Int position, PlacementValidationResult result)
        {
            _previewRenderer?.UpdateValidationVisuals(result);
        }
        
        private void HandleSelectionChanged(List<GridPlaceable> selectedObjects)
        {
            LogDebug($"Selection changed: {selectedObjects.Count} objects selected");
        }
        
        private void ValidateSystemIntegrity()
        {
            bool isValid = true;
            
            if (_gridSystem == null)
            {
                Debug.LogError("[GridPlacementController] GridSystem not found!");
                isValid = false;
            }
            
            if (_inputHandler == null)
            {
                Debug.LogError("[GridPlacementController] GridInputHandler not found!");
                isValid = false;
            }
            
            if (!isValid)
            {
                enabled = false;
            }
        }
        
        private void LogDebug(string message)
        {
            if (_enableDebugLogging)
                Debug.Log($"[GridPlacementController] {message}");
        }
        
        // Cleanup
        private void OnDestroy()
        {
            if (_inputHandler != null)
            {
                _inputHandler.OnPlacementRequested -= () => HandlePlacementRequest();
                _inputHandler.OnCancelRequested -= HandleCancelRequest;
                _inputHandler.OnGridCoordinateChanged -= HandleGridCoordinateChanged;
            }
            
            if (_validator != null)
            {
                _validator.OnValidationCompleted -= HandleValidationResult;
            }
            
            if (_selectionManager != null)
            {
                _selectionManager.OnSelectionChanged -= HandleSelectionChanged;
            }
        }
    }
}