using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Interfaces;
using ProjectChimera.Systems.Camera;

namespace ProjectChimera.Systems.Rendering.Core
{
    /// <summary>
    /// REFACTORED: Volumetric Lighting Manager
    /// Focused component for managing volumetric lighting effects and command buffers
    /// </summary>
    public class VolumetricLightingManager : MonoBehaviour
    {
        [Header("Volumetric Lighting Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableVolumetricLighting = false;
        [SerializeField] private float _volumetricQuality = 0.5f; // 0.5 = half resolution

        // Volumetric components
        private Material _volumetricMaterial;
        private CommandBuffer _volumetricCommandBuffer;
        private RenderTexture _volumetricTexture;
        private UnityEngine.Camera _mainCamera;

        // Performance tracking
        private VolumetricStats _stats = new VolumetricStats();

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public bool IsVolumetricEnabled => _enableVolumetricLighting && IsEnabled;
        public VolumetricStats Stats => _stats;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _mainCamera = UnityEngine.Camera.main;

            if (_mainCamera == null)
            {
                var cameraService = ServiceContainerFactory.Instance?.TryResolve<ICameraService>();
                _mainCamera = cameraService?.MainCamera;
            }

            if (_enableVolumetricLighting)
            {
                InitializeVolumetricLighting();
            }

            ResetStats();

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", "✅ VolumetricLightingManager initialized", this);
        }

        /// <summary>
        /// Enable or disable volumetric lighting
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                DisableVolumetricLighting();
            }
            else if (_enableVolumetricLighting)
            {
                InitializeVolumetricLighting();
            }

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"VolumetricLightingManager: {(enabled ? "enabled" : "disabled")}", this);
        }

        /// <summary>
        /// Set volumetric lighting enabled/disabled
        /// </summary>
        public void SetVolumetricEnabled(bool enabled)
        {
            if (_enableVolumetricLighting == enabled) return;

            _enableVolumetricLighting = enabled;

            if (enabled && IsEnabled)
            {
                InitializeVolumetricLighting();
            }
            else
            {
                DisableVolumetricLighting();
            }

            _stats.VolumetricToggleEvents++;

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"Volumetric lighting: {(enabled ? "enabled" : "disabled")}", this);
        }

        /// <summary>
        /// Set volumetric quality (0.25 to 1.0)
        /// </summary>
        public void SetVolumetricQuality(float quality)
        {
            quality = Mathf.Clamp(quality, 0.25f, 1.0f);

            if (Mathf.Approximately(_volumetricQuality, quality)) return;

            _volumetricQuality = quality;

            // Recreate render texture with new quality
            if (_enableVolumetricLighting && IsEnabled)
            {
                RecreateVolumetricTexture();
            }

            _stats.QualityChangeEvents++;

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"Volumetric quality set to: {quality:F2}", this);
        }

        /// <summary>
        /// Update volumetric effects (called by performance optimizer)
        /// </summary>
        public void UpdateVolumetricEffects()
        {
            if (!IsVolumetricEnabled || _volumetricCommandBuffer == null) return;

            // Update command buffer contents
            _volumetricCommandBuffer.Clear();

            if (_volumetricTexture != null && _volumetricMaterial != null)
            {
                // Set up volumetric rendering commands
                _volumetricCommandBuffer.SetRenderTarget(_volumetricTexture);
                _volumetricCommandBuffer.ClearRenderTarget(true, true, Color.clear);

                // Render volumetric lighting pass
                _volumetricCommandBuffer.Blit(null, _volumetricTexture, _volumetricMaterial, 0);

                _stats.VolumetricUpdates++;
            }
        }

        /// <summary>
        /// Get volumetric performance statistics
        /// </summary>
        public VolumetricStats GetStats()
        {
            _stats.IsVolumetricActive = IsVolumetricEnabled;
            _stats.VolumetricQuality = _volumetricQuality;
            _stats.HasVolumetricTexture = _volumetricTexture != null;

            if (_volumetricTexture != null)
            {
                _stats.VolumetricTextureMemory = _volumetricTexture.width * _volumetricTexture.height * 8; // Assume ARGBHalf = 8 bytes per pixel
            }

            return _stats;
        }

        private void InitializeVolumetricLighting()
        {
            if (_mainCamera == null) return;

            CreateVolumetricMaterial();
            CreateVolumetricCommandBuffer();
            CreateVolumetricTexture();
            AttachCommandBufferToCamera();

            _stats.VolumetricInitializationEvents++;

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", "Volumetric lighting initialized", this);
        }

        private void CreateVolumetricMaterial()
        {
            var volumetricShader = Shader.Find("ProjectChimera/Lighting/VolumetricLighting");
            if (volumetricShader != null)
            {
                _volumetricMaterial = new Material(volumetricShader);
                _stats.VolumetricMaterialCreated = true;
            }
            else
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("RENDERING", "Volumetric lighting shader not found", this);
            }
        }

        private void CreateVolumetricCommandBuffer()
        {
            if (_volumetricCommandBuffer != null)
            {
                _volumetricCommandBuffer.Dispose();
            }

            _volumetricCommandBuffer = new CommandBuffer();
            _volumetricCommandBuffer.name = "Volumetric Lighting";
        }

        private void CreateVolumetricTexture()
        {
            if (_mainCamera == null) return;

            int width = Mathf.RoundToInt(_mainCamera.pixelWidth * _volumetricQuality);
            int height = Mathf.RoundToInt(_mainCamera.pixelHeight * _volumetricQuality);

            if (_volumetricTexture != null)
            {
                _volumetricTexture.Release();
            }

            _volumetricTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBHalf);
            _volumetricTexture.name = "VolumetricLightingTexture";
        }

        private void RecreateVolumetricTexture()
        {
            CreateVolumetricTexture();
        }

        private void AttachCommandBufferToCamera()
        {
            if (_mainCamera != null && _volumetricCommandBuffer != null)
            {
                _mainCamera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, _volumetricCommandBuffer);
            }
        }

        private void DisableVolumetricLighting()
        {
            // Remove command buffer from camera
            if (_volumetricCommandBuffer != null && _mainCamera != null)
            {
                _mainCamera.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, _volumetricCommandBuffer);
            }

            // Release render texture
            if (_volumetricTexture != null)
            {
                _volumetricTexture.Release();
                _volumetricTexture = null;
            }

            // Dispose command buffer
            if (_volumetricCommandBuffer != null)
            {
                _volumetricCommandBuffer.Dispose();
                _volumetricCommandBuffer = null;
            }

            _stats.VolumetricDisableEvents++;
        }

        private void ResetStats()
        {
            _stats = new VolumetricStats
            {
                IsVolumetricActive = false,
                VolumetricQuality = _volumetricQuality,
                VolumetricUpdates = 0,
                VolumetricToggleEvents = 0,
                QualityChangeEvents = 0,
                VolumetricInitializationEvents = 0,
                VolumetricDisableEvents = 0,
                VolumetricMaterialCreated = false,
                HasVolumetricTexture = false,
                VolumetricTextureMemory = 0
            };
        }

        private void OnDestroy()
        {
            DisableVolumetricLighting();

            if (_volumetricMaterial != null)
            {
                DestroyImmediate(_volumetricMaterial);
                _volumetricMaterial = null;
            }
        }
    }

    /// <summary>
    /// Volumetric lighting statistics
    /// </summary>
    [System.Serializable]
    public struct VolumetricStats
    {
        public bool IsVolumetricActive;
        public float VolumetricQuality;
        public int VolumetricUpdates;
        public int VolumetricToggleEvents;
        public int QualityChangeEvents;
        public int VolumetricInitializationEvents;
        public int VolumetricDisableEvents;
        public bool VolumetricMaterialCreated;
        public bool HasVolumetricTexture;
        public int VolumetricTextureMemory;
    }
}
