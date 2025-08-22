using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Construction;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Acts as the central manager for the new grid-based construction system.
    /// This class serves as the public API for other game systems to interact with construction,
    /// delegating all complex logic to the GridSystem and GridPlacementController.
    /// This replaces the legacy procedural construction system.
    /// </summary>
    [RequireComponent(typeof(GridSystem), typeof(GridPlacementController))]
    public class InteractiveFacilityConstructor : ChimeraManager
    {
        [Header("System References")]
        [SerializeField] private GridSystem _gridSystem;
        [SerializeField] private GridPlacementController _placementController;

        [Header("Construction Catalog")]
        [SerializeField] private ConstructionCatalog _constructionCatalog;
        
        // Events
        public System.Action<GridPlaceable> OnObjectPlaced;
        public System.Action<GridPlaceable> OnObjectRemoved;
        public System.Action<string> OnError;
        
        // Properties
        public GridSystem Grid => _gridSystem;
        public GridPlacementController PlacementController => _placementController;
        public ConstructionCatalog Catalog => _constructionCatalog;
        public bool IsInPlacementMode => _placementController != null && _placementController.IsInPlacementMode;
        
        protected override void OnManagerInitialize()
        {
            FindCoreComponents();
            SubscribeToEvents();
            LogInfo("Grid-based InteractiveFacilityConstructor initialized.");
        }

        private void FindCoreComponents()
        {
            if (_gridSystem == null) _gridSystem = GetComponent<GridSystem>();
            if (_placementController == null) _placementController = GetComponent<GridPlacementController>();
        }

        private void SubscribeToEvents()
        {
            if (_placementController != null)
            {
                _placementController.OnObjectPlaced += HandleObjectPlaced;
                _placementController.OnObjectRemoved += HandleObjectRemoved;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_placementController != null)
            {
                _placementController.OnObjectPlaced -= HandleObjectPlaced;
                _placementController.OnObjectRemoved -= HandleObjectRemoved;
            }
        }

        #region Public API - Construction Management
        
        /// <summary>
        /// Starts the placement mode for a specific construction item from the catalog.
        /// </summary>
        public void StartPlacement(string templateName)
        {
            var template = _constructionCatalog?.FindTemplate(templateName);
            if (template == null)
            {
                HandleError($"Template '{templateName}' not found.");
                return;
            }

            var placeablePrefab = template.Prefab?.GetComponent<GridPlaceable>();
            if (placeablePrefab == null)
            {
                HandleError($"Prefab for '{templateName}' is invalid or missing GridPlaceable component.");
                return;
            }
            
            _placementController.StartPlacement(placeablePrefab);
        }
        
        /// <summary>
        /// Cancels the current placement mode.
        /// </summary>
        public void CancelPlacement()
        {
            if (IsInPlacementMode)
            {
                _placementController.CancelPlacement();
            }
        }
        
        /// <summary>
        /// Removes the currently selected object from the grid.
        /// </summary>
        public void RemoveSelectedObject()
        {
            if (_placementController.SelectedObject != null)
            {
                _placementController.RemoveSelectedObject();
            }
        }
        
        #endregion
        
        #region Event Handlers

        private void HandleObjectPlaced(GridPlaceable placeable)
        {
            LogInfo($"'{placeable.name}' placed. Propagating event.");
            OnObjectPlaced?.Invoke(placeable);
        }

        private void HandleObjectRemoved(GridPlaceable placeable)
        {
            LogInfo($"'{placeable.name}' removed. Propagating event.");
            OnObjectRemoved?.Invoke(placeable);
        }
        
        #endregion

        private void HandleError(string message)
        {
            Debug.LogError($"[InteractiveFacilityConstructor] {message}");
            OnError?.Invoke(message);
        }
        
        protected override void OnManagerShutdown()
        {
            UnsubscribeFromEvents();
            LogInfo("InteractiveFacilityConstructor shutdown complete.");
        }
    }
}
