using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Specialized renderer for 3D height level indicators in grid construction system.
    /// Handles animated height markers, level transitions, and visual feedback for vertical placement.
    /// </summary>
    public class GridHeightIndicatorRenderer : MonoBehaviour
    {
        [Header("Height Indicator Settings")]
        [SerializeField] private GameObject _heightIndicatorPrefab;
        [SerializeField] private Color _heightIndicatorColor = new Color(0.2f, 0.8f, 1f, 0.7f);
        [SerializeField] private Color _activeHeightColor = new Color(1f, 1f, 0f, 1f);
        [SerializeField] private float _indicatorScale = 0.5f;
        [SerializeField] private float _animationSpeed = 2f;
        [SerializeField] private bool _enableAnimations = true;
        
        [Header("Multi-Level Display")]
        [SerializeField] private int _maxVisibleIndicators = 10;
        [SerializeField] private float _heightSpacing = 1f;
        [SerializeField] private bool _showRelativeHeights = true;
        [SerializeField] private float _fadeDistance = 5f;
        
        // Core references
        private GridSystem _gridSystem;
        private GridInputHandler _inputHandler;
        
        // Height indicator management
        private List<GameObject> _heightIndicators = new List<GameObject>();
        private GameObject _indicatorParent;
        private int _currentActiveHeight = 0;
        private Vector3Int _currentIndicatorPosition;
        
        // Animation state
        private float _animationTime = 0f;
        private Dictionary<GameObject, float> _indicatorPhases = new Dictionary<GameObject, float>();
        
        // Events
        public System.Action<int> OnHeightLevelChanged;
        public System.Action<Vector3Int, int> OnIndicatorPositionChanged;
        
        private void Awake()
        {
            _gridSystem = FindObjectOfType<GridSystem>();
            _inputHandler = GetComponent<GridInputHandler>();
            
            _indicatorParent = new GameObject("HeightIndicators");
            _indicatorParent.transform.SetParent(transform);
        }
        
        private void Start()
        {
            if (_inputHandler != null)
            {
                _inputHandler.OnHeightLevelChanged += OnHeightChanged;
                _inputHandler.OnGridCoordinateChanged += OnPositionChanged;
            }
        }
        
        private void Update()
        {
            if (_enableAnimations)
                UpdateIndicatorAnimations();
        }
        
        #region Public API
        
        /// <summary>
        /// Show height indicators at grid position for specified max height
        /// </summary>
        public void ShowHeightIndicators(Vector3Int gridPosition, int maxHeight)
        {
            _currentIndicatorPosition = gridPosition;
            ClearHeightIndicators();
            
            if (_heightIndicatorPrefab == null) return;
            
            int visibleCount = Mathf.Min(maxHeight + 1, _maxVisibleIndicators);
            
            for (int h = 0; h < visibleCount; h++)
            {
                CreateHeightIndicator(gridPosition, h, h == _currentActiveHeight);
            }
        }
        
        /// <summary>
        /// Update active height level highlighting
        /// </summary>
        public void SetActiveHeightLevel(int heightLevel)
        {
            if (_currentActiveHeight == heightLevel) return;
            
            _currentActiveHeight = heightLevel;
            UpdateIndicatorColors();
            OnHeightLevelChanged?.Invoke(heightLevel);
        }
        
        /// <summary>
        /// Hide all height indicators
        /// </summary>
        public void HideHeightIndicators()
        {
            ClearHeightIndicators();
        }
        
        /// <summary>
        /// Update indicator visibility based on camera distance
        /// </summary>
        public void UpdateIndicatorVisibility(Vector3 cameraPosition)
        {
            foreach (var indicator in _heightIndicators)
            {
                if (indicator != null)
                {
                    float distance = Vector3.Distance(indicator.transform.position, cameraPosition);
                    float alpha = Mathf.Clamp01((_fadeDistance - distance) / _fadeDistance);
                    
                    var renderer = indicator.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        Color color = renderer.material.color;
                        color.a = alpha * (_enableAnimations ? GetAnimationAlpha(indicator) : 1f);
                        renderer.material.color = color;
                    }
                }
            }
        }
        
        #endregion
        
        #region Indicator Creation and Management
        
        private void CreateHeightIndicator(Vector3Int gridPosition, int heightLevel, bool isActive)
        {
            Vector3Int heightPos = new Vector3Int(gridPosition.x, gridPosition.y, heightLevel);
            Vector3 worldPos = _gridSystem.GridToWorldPosition(heightPos);
            
            GameObject indicator = Instantiate(_heightIndicatorPrefab, worldPos, Quaternion.identity, _indicatorParent.transform);
            indicator.name = $"HeightIndicator_H{heightLevel}";
            indicator.transform.localScale = Vector3.one * _indicatorScale;
            
            SetupIndicatorVisuals(indicator, heightLevel, isActive);
            
            _heightIndicators.Add(indicator);
            
            // Store animation phase offset for this indicator
            _indicatorPhases[indicator] = heightLevel * 0.2f;
        }
        
        private void SetupIndicatorVisuals(GameObject indicator, int heightLevel, bool isActive)
        {
            var renderer = indicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color baseColor = isActive ? _activeHeightColor : _heightIndicatorColor;
                
                if (_showRelativeHeights)
                {
                    // Adjust color brightness based on height level
                    float heightFactor = (float)heightLevel / _maxVisibleIndicators;
                    baseColor = Color.Lerp(baseColor, Color.white, heightFactor * 0.3f);
                }
                
                // Create material instance to avoid shared material issues
                Material indicatorMaterial = new Material(renderer.material);
                indicatorMaterial.color = baseColor;
                renderer.material = indicatorMaterial;
            }
            
            // Add subtle scaling based on importance
            if (isActive)
            {
                indicator.transform.localScale *= 1.2f;
            }
        }
        
        private void UpdateIndicatorColors()
        {
            for (int i = 0; i < _heightIndicators.Count; i++)
            {
                var indicator = _heightIndicators[i];
                if (indicator != null)
                {
                    bool isActive = i == _currentActiveHeight;
                    SetupIndicatorVisuals(indicator, i, isActive);
                }
            }
        }
        
        #endregion
        
        #region Animation System
        
        private void UpdateIndicatorAnimations()
        {
            _animationTime += Time.deltaTime * _animationSpeed;
            
            foreach (var indicator in _heightIndicators)
            {
                if (indicator != null)
                {
                    UpdateIndicatorAnimation(indicator);
                }
            }
        }
        
        private void UpdateIndicatorAnimation(GameObject indicator)
        {
            if (!_indicatorPhases.ContainsKey(indicator)) return;
            
            float phase = _indicatorPhases[indicator];
            float alpha = GetAnimationAlpha(indicator);
            
            // Update transparency
            var renderer = indicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color color = renderer.material.color;
                color.a = alpha;
                renderer.material.color = color;
            }
            
            // Add subtle bobbing animation for active indicator
            if (IsActiveIndicator(indicator))
            {
                Vector3 basePos = GetBasePosition(indicator);
                float bobOffset = Mathf.Sin(_animationTime + phase) * 0.1f;
                indicator.transform.position = basePos + Vector3.up * bobOffset;
            }
        }
        
        private float GetAnimationAlpha(GameObject indicator)
        {
            if (!_indicatorPhases.ContainsKey(indicator)) return 1f;
            
            float phase = _indicatorPhases[indicator];
            return 0.6f + 0.4f * Mathf.Sin(_animationTime + phase);
        }
        
        private bool IsActiveIndicator(GameObject indicator)
        {
            int index = _heightIndicators.IndexOf(indicator);
            return index == _currentActiveHeight;
        }
        
        private Vector3 GetBasePosition(GameObject indicator)
        {
            int index = _heightIndicators.IndexOf(indicator);
            if (index == -1) return indicator.transform.position;
            
            Vector3Int heightPos = new Vector3Int(
                _currentIndicatorPosition.x, 
                _currentIndicatorPosition.y, 
                index
            );
            return _gridSystem.GridToWorldPosition(heightPos);
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnHeightChanged(int newHeight)
        {
            SetActiveHeightLevel(newHeight);
            
            // Update indicator position if showing indicators
            if (_heightIndicators.Count > 0)
            {
                ShowHeightIndicators(_currentIndicatorPosition, _heightIndicators.Count - 1);
            }
        }
        
        private void OnPositionChanged(Vector3Int newPosition)
        {
            if (_heightIndicators.Count > 0)
            {
                ShowHeightIndicators(newPosition, _heightIndicators.Count - 1);
            }
            
            OnIndicatorPositionChanged?.Invoke(newPosition, _currentActiveHeight);
        }
        
        #endregion
        
        #region Cleanup
        
        private void ClearHeightIndicators()
        {
            foreach (var indicator in _heightIndicators)
            {
                if (indicator != null) 
                    DestroyImmediate(indicator);
            }
            
            _heightIndicators.Clear();
            _indicatorPhases.Clear();
        }
        
        private void OnDestroy()
        {
            if (_inputHandler != null)
            {
                _inputHandler.OnHeightLevelChanged -= OnHeightChanged;
                _inputHandler.OnGridCoordinateChanged -= OnPositionChanged;
            }
            
            ClearHeightIndicators();
        }
        
        #endregion
    }
}