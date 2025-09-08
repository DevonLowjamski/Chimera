using UnityEngine;
using ProjectChimera.Core.Updates;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core;
using ProjectChimera.Data.Construction;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Advanced blueprint overlay rendering system with unlit materials and outline effects.
    /// Provides visual feedback during schematic placement with configurable rendering modes.
    /// </summary>
    public class BlueprintOverlayRenderer : ChimeraManager, ITickable{
        [Header("Overlay Configuration")]
        [SerializeField] private bool _enableOverlayRendering = true;
        [SerializeField] private LayerMask _overlayLayer = 30; // Blueprint overlay layer
        [SerializeField] private UnityEngine.Camera _overlayCamera;
        [SerializeField] private RenderTexture _overlayRenderTexture;

        [Header("Material Settings")]
        [SerializeField] private Material _blueprintMaterial;
        [SerializeField] private Material _validPlacementMaterial;
        [SerializeField] private Material _invalidPlacementMaterial;
        [SerializeField] private Material _outlineMaterial;

        [Header("Visual Properties")]
        [SerializeField] private Color _blueprintColor = new Color(0.2f, 0.6f, 1.0f, 0.4f);
        [SerializeField] private Color _validColor = new Color(0.2f, 1.0f, 0.2f, 0.6f);
        [SerializeField] private Color _invalidColor = new Color(1.0f, 0.2f, 0.2f, 0.6f);
        [SerializeField] private Color _outlineColor = new Color(1.0f, 1.0f, 1.0f, 0.8f);
        [SerializeField] private float _outlineWidth = 0.02f;

        [Header("Animation Settings")]
        [SerializeField] private bool _enablePulseAnimation = true;
        [SerializeField] private float _pulseSpeed = 2.0f;
        [SerializeField] private float _pulseIntensity = 0.3f;
        [SerializeField] private bool _enableFadeTransitions = true;
        [SerializeField] private float _fadeSpeed = 3.0f;

        [Header("Performance Settings")]
        [SerializeField] private bool _enableLOD = true;
        [SerializeField] private float _maxRenderDistance = 50f;
        [SerializeField] private int _maxVisibleOverlays = 100;
        [SerializeField] private bool _enableCulling = true;

        // System references
        private GridPlacementController _placementController;
        private UnityEngine.Camera _mainCamera;

        // Overlay tracking
        private Dictionary<GameObject, OverlayInstance> _activeOverlays = new Dictionary<GameObject, OverlayInstance>();
        private List<OverlayInstance> _overlayPool = new List<OverlayInstance>();
        private Queue<OverlayInstance> _availableOverlays = new Queue<OverlayInstance>();

        // Rendering state
        private CommandBuffer _outlineCommandBuffer;
        private RenderTargetIdentifier[] _outlineTargets;
        private bool _isRenderingEnabled = true;

        // Animation state
        private float _animationTime = 0f;
        private Dictionary<OverlayInstance, float> _fadeStates = new Dictionary<OverlayInstance, float>();

        // Events
        public System.Action<OverlayInstance> OnOverlayCreated;
        public System.Action<OverlayInstance> OnOverlayDestroyed;
        public System.Action<bool> OnRenderingStateChanged;

        public override ManagerPriority Priority => ManagerPriority.Normal;

        // Public Properties
        public bool OverlayRenderingEnabled => _enableOverlayRendering && _isRenderingEnabled;
        public int ActiveOverlayCount => _activeOverlays.Count;
        public UnityEngine.Camera OverlayCamera => _overlayCamera;
        public Material BlueprintMaterial => _blueprintMaterial;

        protected override void OnManagerInitialize()
        {
            InitializeRenderingSystem();
            SetupOverlayCamera();
            CreateMaterials();
            InitializeObjectPooling();
            SetupCommandBuffers();

            LogInfo($"BlueprintOverlayRenderer initialized - Layer: {_overlayLayer}, Max overlays: {_maxVisibleOverlays}");
        }

        public void Tick(float deltaTime)
 {
            if (!OverlayRenderingEnabled) return;

            UpdateAnimations();
            UpdateLOD();
            UpdateCulling();
            ProcessFadeTransitions();
        }

        /// <summary>
        /// Create blueprint overlay for a schematic
        /// </summary>
        public OverlayInstance CreateSchematicOverlay(SchematicSO schematic, Vector3 position, Quaternion rotation, OverlayType overlayType = OverlayType.Blueprint)
        {
            if (!OverlayRenderingEnabled || schematic == null)
            {
                LogWarning("Cannot create overlay - rendering disabled or invalid schematic");
                return null;
            }

            var overlayInstance = GetOrCreateOverlayInstance();

            overlayInstance.SchematicId = schematic.name;
            overlayInstance.OverlayType = overlayType;
            overlayInstance.Position = position;
            overlayInstance.Rotation = rotation;
            overlayInstance.IsVisible = true;
            overlayInstance.CreationTime = Time.time;

            // Create overlay objects for each item in schematic
            overlayInstance.OverlayObjects.Clear();
            foreach (var item in schematic.Items)
            {
                var overlayObject = CreateOverlayObject(item, position, rotation, overlayType);
                if (overlayObject != null)
                {
                    overlayInstance.OverlayObjects.Add(overlayObject);
                    _activeOverlays[overlayObject] = overlayInstance;
                }
            }

            ApplyOverlayMaterials(overlayInstance, overlayType);
            SetupOverlayAnimations(overlayInstance);

            OnOverlayCreated?.Invoke(overlayInstance);

            LogInfo($"Created schematic overlay for '{schematic.SchematicName}' with {overlayInstance.OverlayObjects.Count} objects");
            return overlayInstance;
        }

        /// <summary>
        /// Update overlay validation state (valid/invalid placement)
        /// </summary>
        public void UpdateOverlayValidation(OverlayInstance overlay, bool isValidPlacement)
        {
            if (overlay == null) return;

            var newType = isValidPlacement ? OverlayType.ValidPlacement : OverlayType.InvalidPlacement;
            if (overlay.OverlayType != newType)
            {
                overlay.OverlayType = newType;
                ApplyOverlayMaterials(overlay, newType);
            }
        }

        /// <summary>
        /// Move overlay to new position with smooth transition
        /// </summary>
        public void MoveOverlay(OverlayInstance overlay, Vector3 newPosition, Quaternion newRotation, bool smoothTransition = true)
        {
            if (overlay == null) return;

            if (smoothTransition)
            {
                StartCoroutine(SmoothMoveOverlay(overlay, newPosition, newRotation));
            }
            else
            {
                SetOverlayTransform(overlay, newPosition, newRotation);
            }
        }

        /// <summary>
        /// Remove overlay instance
        /// </summary>
        public void DestroyOverlay(OverlayInstance overlay)
        {
            if (overlay == null) return;

            // Remove from active tracking
            foreach (var obj in overlay.OverlayObjects)
            {
                if (obj != null)
                {
                    _activeOverlays.Remove(obj);
                    ReturnOverlayObjectToPool(obj);
                }
            }

            overlay.OverlayObjects.Clear();
            overlay.IsVisible = false;

            // Return to pool
            _availableOverlays.Enqueue(overlay);

            OnOverlayDestroyed?.Invoke(overlay);

            LogInfo($"Destroyed overlay for schematic: {overlay.SchematicId}");
        }

        /// <summary>
        /// Clear all active overlays
        /// </summary>
        public void ClearAllOverlays()
        {
            var overlaysToDestroy = _activeOverlays.Values.Distinct().ToList();
            foreach (var overlay in overlaysToDestroy)
            {
                DestroyOverlay(overlay);
            }

            LogInfo($"Cleared {overlaysToDestroy.Count} overlay instances");
        }

        /// <summary>
        /// Toggle overlay rendering on/off
        /// </summary>
        public void SetRenderingEnabled(bool enabled)
        {
            if (_isRenderingEnabled == enabled) return;

            _isRenderingEnabled = enabled;

            if (_overlayCamera != null)
                _overlayCamera.enabled = enabled && _enableOverlayRendering;

            // Update visibility of all overlays
            foreach (var overlay in _activeOverlays.Values.Distinct())
            {
                SetOverlayVisibility(overlay, enabled);
            }

            OnRenderingStateChanged?.Invoke(enabled);

            LogInfo($"Overlay rendering {(enabled ? "enabled" : "disabled")}");
        }

        private void InitializeRenderingSystem()
        {
            _mainCamera = UnityEngine.Camera.main ?? ServiceContainerFactory.Instance?.TryResolve<UnityEngine.Camera>();
            if (_mainCamera == null)
            {
                LogError("No main camera found for overlay rendering");
                return;
            }

            // Setup render texture for overlay effects
            if (_overlayRenderTexture == null)
            {
                _overlayRenderTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
                _overlayRenderTexture.name = "BlueprintOverlayRT";
            }
        }

        private void SetupOverlayCamera()
        {
            if (_overlayCamera == null)
            {
                var cameraGO = new GameObject("BlueprintOverlayCamera");
                _overlayCamera = cameraGO.AddComponent<UnityEngine.Camera>();
                cameraGO.transform.SetParent(transform);
            }

            // Configure overlay camera
            _overlayCamera.cullingMask = _overlayLayer;
            _overlayCamera.clearFlags = CameraClearFlags.Nothing;
            _overlayCamera.depth = _mainCamera.depth + 1;
            _overlayCamera.targetTexture = _overlayRenderTexture;
            _overlayCamera.enabled = _enableOverlayRendering;

            // Match main camera properties
            _overlayCamera.fieldOfView = _mainCamera.fieldOfView;
            _overlayCamera.transform.position = _mainCamera.transform.position;
            _overlayCamera.transform.rotation = _mainCamera.transform.rotation;
        }

        private void CreateMaterials()
        {
            if (_blueprintMaterial == null)
            {
                _blueprintMaterial = CreateUnlitMaterial("BlueprintMaterial", _blueprintColor);
            }

            if (_validPlacementMaterial == null)
            {
                _validPlacementMaterial = CreateUnlitMaterial("ValidPlacementMaterial", _validColor);
            }

            if (_invalidPlacementMaterial == null)
            {
                _invalidPlacementMaterial = CreateUnlitMaterial("InvalidPlacementMaterial", _invalidColor);
            }

            if (_outlineMaterial == null)
            {
                _outlineMaterial = CreateOutlineMaterial("OutlineMaterial", _outlineColor, _outlineWidth);
            }
        }

        private Material CreateUnlitMaterial(string name, Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.name = name;
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Surface", 1); // Transparent
            material.SetFloat("_Blend", 0); // Alpha blend
            material.renderQueue = (int)RenderQueue.Transparent;

            // Enable transparency
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);

            return material;
        }

        private Material CreateOutlineMaterial(string name, Color color, float width)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.name = name;
            material.SetColor("_BaseColor", color);
            material.SetFloat("_OutlineWidth", width);

            return material;
        }

        private void InitializeObjectPooling()
        {
            _overlayPool = new List<OverlayInstance>();
            _availableOverlays = new Queue<OverlayInstance>();

            // Pre-allocate overlay instances
            for (int i = 0; i < _maxVisibleOverlays; i++)
            {
                var instance = new OverlayInstance();
                _overlayPool.Add(instance);
                _availableOverlays.Enqueue(instance);
            }
        }

        private void SetupCommandBuffers()
        {
            if (_overlayCamera == null) return;

            _outlineCommandBuffer = new CommandBuffer();
            _outlineCommandBuffer.name = "BlueprintOutlines";

            _overlayCamera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, _outlineCommandBuffer);
        }

        private GameObject CreateOverlayObject(SchematicItem item, Vector3 basePosition, Quaternion baseRotation, OverlayType overlayType)
        {
            // Create a simple overlay representation
            var overlayObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            overlayObject.name = $"Overlay_{item.ItemName}";
            overlayObject.layer = _overlayLayer;

            // Remove collider for overlay
            if (overlayObject.GetComponent<Collider>())
                DestroyImmediate(overlayObject.GetComponent<Collider>());

            // Set transform using grid position and rotation
            Vector3 gridWorldPos = new Vector3(item.GridPosition.x, item.Height, item.GridPosition.y);
            overlayObject.transform.position = basePosition + gridWorldPos;
            overlayObject.transform.rotation = baseRotation * Quaternion.Euler(0, item.Rotation, 0);
            overlayObject.transform.localScale = new Vector3(item.GridSize.x, 1f, item.GridSize.y);

            return overlayObject;
        }

        private OverlayInstance GetOrCreateOverlayInstance()
        {
            if (_availableOverlays.Count > 0)
            {
                return _availableOverlays.Dequeue();
            }

            // If pool is exhausted, create new instance
            var newInstance = new OverlayInstance();
            _overlayPool.Add(newInstance);
            return newInstance;
        }

        private void ApplyOverlayMaterials(OverlayInstance overlay, OverlayType overlayType)
        {
            Material material = overlayType switch
            {
                OverlayType.Blueprint => _blueprintMaterial,
                OverlayType.ValidPlacement => _validPlacementMaterial,
                OverlayType.InvalidPlacement => _invalidPlacementMaterial,
                _ => _blueprintMaterial
            };

            foreach (var obj in overlay.OverlayObjects)
            {
                if (obj != null && obj.GetComponent<Renderer>())
                {
                    obj.GetComponent<Renderer>().material = material;
                }
            }
        }

        private void SetupOverlayAnimations(OverlayInstance overlay)
        {
            if (!_enablePulseAnimation && !_enableFadeTransitions) return;

            overlay.AnimationState = new OverlayAnimationState
            {
                PulsePhase = Random.Range(0f, Mathf.PI * 2f),
                FadeTarget = 1f,
                CurrentFade = 0f
            };

            _fadeStates[overlay] = 0f;
        }

        private void UpdateAnimations()
        {
            _animationTime += Time.deltaTime;

            foreach (var overlay in _activeOverlays.Values.Distinct())
            {
                if (overlay.AnimationState == null) continue;

                // Update pulse animation
                if (_enablePulseAnimation)
                {
                    float pulseValue = 1f + Mathf.Sin(_animationTime * _pulseSpeed + overlay.AnimationState.PulsePhase) * _pulseIntensity;
                    UpdateOverlayPulse(overlay, pulseValue);
                }
            }
        }

        private void UpdateOverlayPulse(OverlayInstance overlay, float pulseValue)
        {
            foreach (var obj in overlay.OverlayObjects)
            {
                if (obj != null && obj.GetComponent<Renderer>())
                {
                    var renderer = obj.GetComponent<Renderer>();
                    var material = renderer.material;

                    Color currentColor = material.GetColor("_BaseColor");
                    currentColor.a = currentColor.a * pulseValue;
                    material.SetColor("_BaseColor", currentColor);
                }
            }
        }

        private void UpdateLOD()
        {
            if (!_enableLOD || _mainCamera == null) return;

            Vector3 cameraPos = _mainCamera.transform.position;

            foreach (var overlay in _activeOverlays.Values.Distinct())
            {
                float distance = Vector3.Distance(cameraPos, overlay.Position);
                bool shouldBeVisible = distance <= _maxRenderDistance;

                if (overlay.IsVisible != shouldBeVisible)
                {
                    SetOverlayVisibility(overlay, shouldBeVisible);
                }
            }
        }

        private void UpdateCulling()
        {
            if (!_enableCulling || _overlayCamera == null) return;

            var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(_overlayCamera);

            foreach (var overlay in _activeOverlays.Values.Distinct())
            {
                bool inFrustum = IsOverlayInFrustum(overlay, frustumPlanes);

                foreach (var obj in overlay.OverlayObjects)
                {
                    if (obj != null)
                        obj.SetActive(inFrustum && overlay.IsVisible);
                }
            }
        }

        private bool IsOverlayInFrustum(OverlayInstance overlay, Plane[] frustumPlanes)
        {
            var bounds = new Bounds(overlay.Position, Vector3.one * 2f); // Simple bounds approximation
            return GeometryUtility.TestPlanesAABB(frustumPlanes, bounds);
        }

        private void ProcessFadeTransitions()
        {
            if (!_enableFadeTransitions) return;

            var fadeKeys = _fadeStates.Keys.ToList();
            foreach (var overlay in fadeKeys)
            {
                if (overlay == null || overlay.AnimationState == null) continue;

                float currentFade = _fadeStates[overlay];
                float targetFade = overlay.AnimationState.FadeTarget;

                if (Mathf.Abs(currentFade - targetFade) > 0.01f)
                {
                    currentFade = Mathf.MoveTowards(currentFade, targetFade, _fadeSpeed * Time.deltaTime);
                    _fadeStates[overlay] = currentFade;

                    UpdateOverlayFade(overlay, currentFade);
                }
            }
        }

        private void UpdateOverlayFade(OverlayInstance overlay, float fadeValue)
        {
            foreach (var obj in overlay.OverlayObjects)
            {
                if (obj != null && obj.GetComponent<Renderer>())
                {
                    var renderer = obj.GetComponent<Renderer>();
                    var material = renderer.material;

                    Color currentColor = material.GetColor("_BaseColor");
                    currentColor.a = fadeValue;
                    material.SetColor("_BaseColor", currentColor);
                }
            }
        }

        private void SetOverlayVisibility(OverlayInstance overlay, bool visible)
        {
            overlay.IsVisible = visible;

            if (overlay.AnimationState != null)
            {
                overlay.AnimationState.FadeTarget = visible ? 1f : 0f;
            }

            foreach (var obj in overlay.OverlayObjects)
            {
                if (obj != null)
                    obj.SetActive(visible && _isRenderingEnabled);
            }
        }

        private void SetOverlayTransform(OverlayInstance overlay, Vector3 position, Quaternion rotation)
        {
            overlay.Position = position;
            overlay.Rotation = rotation;

            // Update all overlay objects relative to new position
            for (int i = 0; i < overlay.OverlayObjects.Count; i++)
            {
                var obj = overlay.OverlayObjects[i];
                if (obj != null)
                {
                    // Calculate relative transform from schematic data would go here
                    obj.transform.position = position;
                    obj.transform.rotation = rotation;
                }
            }
        }

        private System.Collections.IEnumerator SmoothMoveOverlay(OverlayInstance overlay, Vector3 targetPosition, Quaternion targetRotation)
        {
            Vector3 startPosition = overlay.Position;
            Quaternion startRotation = overlay.Rotation;
            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, t);
                Quaternion currentRot = Quaternion.Lerp(startRotation, targetRotation, t);

                SetOverlayTransform(overlay, currentPos, currentRot);

                yield return null;
            }

            SetOverlayTransform(overlay, targetPosition, targetRotation);
        }

        private void ReturnOverlayObjectToPool(GameObject obj)
        {
            if (obj != null)
            {
                obj.SetActive(false);
                // In a full implementation, would maintain object pools
                DestroyImmediate(obj);
            }
        }

        protected override void OnManagerShutdown()
        {
            ClearAllOverlays();

            if (_outlineCommandBuffer != null)
            {
                if (_overlayCamera != null)
                    _overlayCamera.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, _outlineCommandBuffer);

                _outlineCommandBuffer.Release();
            }

            if (_overlayRenderTexture != null)
            {
                _overlayRenderTexture.Release();
                DestroyImmediate(_overlayRenderTexture);
            }

            LogInfo($"BlueprintOverlayRenderer shutdown - {ActiveOverlayCount} overlays destroyed");
        }

        #region Unity Lifecycle

        protected override void Start()
        {
            base.Start();
            // Register with UpdateOrchestrator
            UpdateOrchestrator.Instance?.RegisterTickable(this);
        }

        protected override void OnDestroy()
        {
            // Unregister from UpdateOrchestrator
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
            base.OnDestroy();
        }

        #endregion

        #region ITickable Implementation

        int ITickable.Priority => 0;
        public bool Enabled => enabled && gameObject.activeInHierarchy;

        public virtual void OnRegistered()
        {
            // Override in derived classes if needed
        }

        public virtual void OnUnregistered()
        {
            // Override in derived classes if needed
        }

        #endregion
    }

    /// <summary>
    /// Overlay instance representing a schematic visualization
    /// </summary>
    [System.Serializable]
    public class OverlayInstance
    {
        public string SchematicId;
        public OverlayType OverlayType;
        public Vector3 Position;
        public Quaternion Rotation;
        public bool IsVisible;
        public float CreationTime;
        public List<GameObject> OverlayObjects = new List<GameObject>();
        public OverlayAnimationState AnimationState;
    }

    /// <summary>
    /// Animation state for overlay instances
    /// </summary>
    [System.Serializable]
    public class OverlayAnimationState
    {
        public float PulsePhase;
        public float FadeTarget;
        public float CurrentFade;
    }

    /// <summary>
    /// Types of overlay rendering
    /// </summary>
    public enum OverlayType
    {
        Blueprint,
        ValidPlacement,
        InvalidPlacement,
        Preview
    }
}
