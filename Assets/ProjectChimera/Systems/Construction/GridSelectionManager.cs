using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Updates;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Data.Construction;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Manages object selection, multi-selection, and category filtering for grid-based construction.
    /// Handles drag selection, category-based filtering, and selection state management.
    /// Extracted from GridPlacementController for modular architecture.
    /// </summary>
    public class GridSelectionManager : MonoBehaviour, ITickable
    {
        [Header("Selection Settings")]
        [SerializeField] private bool _enableMultiSelection = true;
        [SerializeField] private bool _enableDragSelection = true;
        [SerializeField] private bool _enableCategoryFiltering = true;
        [SerializeField] private KeyCode _addToSelectionKey = KeyCode.LeftShift;
        [SerializeField] private float _dragThreshold = 10f;

        [Header("Visual Feedback")]
        [SerializeField] private Material _selectionMaterial;
        [SerializeField] private Color _selectionColor = Color.yellow;
        [SerializeField] private bool _enableSelectionOutlines = true;

        // Selection state
        private List<GridPlaceable> _selectedObjects = new List<GridPlaceable>();
        private HashSet<GridPlaceable> _selectedObjectsSet = new HashSet<GridPlaceable>();

        // Category filtering
        private List<ConstructionCategory> _activeFilterCategories = new List<ConstructionCategory>();
        private Dictionary<ConstructionCategory, List<GridPlaceable>> _categorizedObjects = new Dictionary<ConstructionCategory, List<GridPlaceable>>();
        private bool _categoryFilteringEnabled = true;

        // Drag selection
        private bool _isDragSelecting = false;
        private Vector2 _dragStartPosition;
        private Vector2 _dragCurrentPosition;
        private Rect _selectionBounds;

        // Events
        public System.Action<List<GridPlaceable>> OnSelectionChanged;
        public System.Action<GridPlaceable> OnObjectSelected;
        public System.Action<GridPlaceable> OnObjectDeselected;

        // Properties
        public List<GridPlaceable> SelectedObjects => new List<GridPlaceable>(_selectedObjects);
        public List<ConstructionCategory> ActiveFilterCategories => new List<ConstructionCategory>(_activeFilterCategories);
        public Dictionary<ConstructionCategory, int> CategoryCounts => GetCategoryCounts();
        public bool HasSelection => _selectedObjects.Count > 0;

            public int TickPriority => 0;
            public bool IsTickable => enabled && gameObject.activeInHierarchy;

            public void Tick(float deltaTime)
    {
            if (_enableDragSelection)
            {
                HandleDragSelection();

    }
        }

        /// <summary>
        /// Select a single object
        /// </summary>
        public void SelectObject(GridPlaceable placeable)
        {
            if (placeable == null) return;

            bool addToSelection = Input.GetKey(_addToSelectionKey) && _enableMultiSelection;

            if (!addToSelection)
            {
                ClearSelection();
            }

            if (!_selectedObjectsSet.Contains(placeable))
            {
                _selectedObjects.Add(placeable);
                _selectedObjectsSet.Add(placeable);

                ApplySelectionVisual(placeable, true);
                OnObjectSelected?.Invoke(placeable);
                OnSelectionChanged?.Invoke(_selectedObjects);
            }
        }

        /// <summary>
        /// Clear all selections
        /// </summary>
        public void ClearSelection()
        {
            foreach (var obj in _selectedObjects)
            {
                ApplySelectionVisual(obj, false);
            }

            _selectedObjects.Clear();
            _selectedObjectsSet.Clear();
            OnSelectionChanged?.Invoke(_selectedObjects);
        }

        /// <summary>
        /// Remove selected objects from grid
        /// </summary>
        public void RemoveSelectedObjects()
        {
            var objectsToRemove = new List<GridPlaceable>(_selectedObjects);
            ClearSelection();

            foreach (var obj in objectsToRemove)
            {
                // Implementation would remove object from grid
                if (obj != null)
                {
                    DestroyImmediate(obj.gameObject);
                }
            }
        }

        /// <summary>
        /// Clear multiple selection (keep only last selected)
        /// </summary>
        public void ClearMultipleSelection()
        {
            if (_selectedObjects.Count <= 1) return;

            var lastSelected = _selectedObjects.LastOrDefault();
            ClearSelection();

            if (lastSelected != null)
            {
                SelectObject(lastSelected);
            }
        }

        /// <summary>
        /// Add object to current selection
        /// </summary>
        public void AddToSelection(GridPlaceable obj)
        {
            if (obj == null || !_enableMultiSelection) return;

            if (!_selectedObjectsSet.Contains(obj))
            {
                _selectedObjects.Add(obj);
                _selectedObjectsSet.Add(obj);

                ApplySelectionVisual(obj, true);
                OnObjectSelected?.Invoke(obj);
                OnSelectionChanged?.Invoke(_selectedObjects);
            }
        }

        /// <summary>
        /// Remove object from current selection
        /// </summary>
        public void RemoveFromSelection(GridPlaceable obj)
        {
            if (obj == null) return;

            if (_selectedObjectsSet.Contains(obj))
            {
                _selectedObjects.Remove(obj);
                _selectedObjectsSet.Remove(obj);

                ApplySelectionVisual(obj, false);
                OnObjectDeselected?.Invoke(obj);
                OnSelectionChanged?.Invoke(_selectedObjects);
            }
        }

        /// <summary>
        /// Check if object is selected
        /// </summary>
        public bool IsObjectSelected(GridPlaceable obj)
        {
            return _selectedObjectsSet.Contains(obj);
        }

        // Category filtering methods
        /// <summary>
        /// Set category filter
        /// </summary>
        public void SetCategoryFilter(List<ConstructionCategory> categories)
        {
            _activeFilterCategories.Clear();
            _activeFilterCategories.AddRange(categories);
            RefreshCategoryFilter();
        }

        /// <summary>
        /// Add category to filter
        /// </summary>
        public void AddCategoryToFilter(ConstructionCategory category)
        {
            if (!_activeFilterCategories.Contains(category))
            {
                _activeFilterCategories.Add(category);
                RefreshCategoryFilter();
            }
        }

        /// <summary>
        /// Remove category from filter
        /// </summary>
        public void RemoveCategoryFromFilter(ConstructionCategory category)
        {
            if (_activeFilterCategories.Remove(category))
            {
                RefreshCategoryFilter();
            }
        }

        /// <summary>
        /// Toggle category filter
        /// </summary>
        public void ToggleCategoryFilter(ConstructionCategory category)
        {
            if (_activeFilterCategories.Contains(category))
            {
                RemoveCategoryFromFilter(category);
            }
            else
            {
                AddCategoryToFilter(category);
            }
        }

        /// <summary>
        /// Get objects by category
        /// </summary>
        public List<GridPlaceable> GetObjectsByCategory(ConstructionCategory category)
        {
            if (_categorizedObjects.TryGetValue(category, out var objects))
            {
                return new List<GridPlaceable>(objects);
            }
            return new List<GridPlaceable>();
        }

        /// <summary>
        /// Get filtered objects based on active categories
        /// </summary>
        public List<GridPlaceable> GetFilteredObjects()
        {
            if (!_categoryFilteringEnabled || _activeFilterCategories.Count == 0)
            {
                return new List<GridPlaceable>(_selectedObjects);
            }

            var filteredObjects = new List<GridPlaceable>();
            foreach (var category in _activeFilterCategories)
            {
                filteredObjects.AddRange(GetObjectsByCategory(category));
            }

            return filteredObjects.Distinct().ToList();
        }

        /// <summary>
        /// Get the currently selected object
        /// </summary>
        public GridPlaceable GetSelectedObject()
        {
            return _selectedObjects.FirstOrDefault();
        }

        /// <summary>
        /// Enable/disable category filtering
        /// </summary>
        public void EnableCategoryFiltering(bool enable)
        {
            _categoryFilteringEnabled = enable;
            RefreshCategoryFilter();
        }

        private void HandleDragSelection()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _dragStartPosition = Input.mousePosition;
                _isDragSelecting = false;
            }

            if (Input.GetMouseButton(0))
            {
                _dragCurrentPosition = Input.mousePosition;
                float dragDistance = Vector2.Distance(_dragStartPosition, _dragCurrentPosition);

                if (dragDistance > _dragThreshold && !_isDragSelecting)
                {
                    _isDragSelecting = true;
                }

                if (_isDragSelecting)
                {
                    UpdateSelectionBounds();
                }
            }

            if (Input.GetMouseButtonUp(0) && _isDragSelecting)
            {
                PerformDragSelection();
                _isDragSelecting = false;
            }
        }

        private void UpdateSelectionBounds()
        {
            Vector2 min = Vector2.Min(_dragStartPosition, _dragCurrentPosition);
            Vector2 max = Vector2.Max(_dragStartPosition, _dragCurrentPosition);
            _selectionBounds = new Rect(min, max - min);
        }

        private void PerformDragSelection()
        {
            var camera = UnityEngine.Camera.main;
            if (camera == null) return;

            var objectsInBounds = new List<GridPlaceable>();

            // Primary: Try GameObjectRegistry for placeable tracking
            var registry = ServiceContainerFactory.Instance?.TryResolve<ProjectChimera.Core.Performance.IGameObjectRegistry>();
            var allPlaceables = registry?.GetAll<GridPlaceable>();

            if (allPlaceables == null || !allPlaceables.Any())
            {
                ChimeraLogger.LogWarning("CONSTRUCTION", "GridSelectionManager: No GridPlaceable objects found - ensure they register with GameObjectRegistry in Awake()", this);
                return;
            }

            foreach (var placeable in allPlaceables)
            {
                if (placeable == null) continue;

                Vector3 screenPos = camera.WorldToScreenPoint(placeable.transform.position);
                if (_selectionBounds.Contains(screenPos))
                {
                    objectsInBounds.Add(placeable);
                }
            }

            bool addToSelection = Input.GetKey(_addToSelectionKey);
            if (!addToSelection)
            {
                ClearSelection();
            }

            foreach (var obj in objectsInBounds)
            {
                AddToSelection(obj);
            }
        }

        private void ApplySelectionVisual(GridPlaceable placeable, bool selected)
        {
            if (placeable == null) return;

            var renderer = placeable.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (selected)
                {
                    // Apply selection visual
                    renderer.material.color = _selectionColor;
                }
                else
                {
                    // Remove selection visual
                    renderer.material.color = Color.white;
                }
            }
        }

        private Dictionary<ConstructionCategory, int> GetCategoryCounts()
        {
            var counts = new Dictionary<ConstructionCategory, int>();
            foreach (var kvp in _categorizedObjects)
            {
                counts[kvp.Key] = kvp.Value.Count;
            }
            return counts;
        }

        private void RefreshCategoryFilter()
        {
            // Refresh category-based object visibility/filtering
            // Implementation would update object visibility based on active filters
        }

    // ITickable implementation


    public virtual void OnRegistered()
    {
        // Override in derived classes if needed
    }

    public virtual void OnUnregistered()
    {
        // Override in derived classes if needed
    }


    // NOTE: Unity lifecycle methods moved inside class
    protected virtual void Start()
    {
        // Register with UpdateOrchestrator
        UpdateOrchestrator.Instance?.RegisterTickable(this);
    }

    protected virtual void OnDestroy()
    {
        // Unregister from UpdateOrchestrator
        UpdateOrchestrator.Instance?.UnregisterTickable(this);
    }
}
}
