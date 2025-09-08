using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;


namespace ProjectChimera.Systems.Scene
{
    /// <summary>
    /// Simple UI component for displaying boot progress
    /// This can be placed on a Canvas in the Boot scene to provide visual feedback
    /// </summary>
    public class BootLoadingUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Text _statusText;
        [SerializeField] private Slider _progressSlider;
        [SerializeField] private Image _backgroundImage;

        [Header("Configuration")]
        [SerializeField] private bool _fadeBackground = true;
        [SerializeField] private float _fadeSpeed = 2.0f;
        [SerializeField] private bool _enableProgressAnimation = true;

        private DIGameManager _diGameManager;
        private Coroutine _statusUpdateCoroutine;

        private void Start()
        {
            // Wait a moment for DIGameManager to be created by BootManager
            StartCoroutine(FindDIGameManager());
        }

        private IEnumerator FindDIGameManager()
        {
            // Wait for DIGameManager to be created
            float timeout = 10f;
            float elapsed = 0f;
            
            while (_diGameManager == null && elapsed < timeout)
            {
                _diGameManager = ServiceContainerFactory.Instance.TryResolve<DIGameManager>();
                if (_diGameManager == null)
                {
                    yield return new WaitForSeconds(0.1f);
                    elapsed += 0.1f;
                }
            }
            
            if (_diGameManager == null)
            {
                ChimeraLogger.LogError("[BootLoadingUI] DIGameManager not found in scene!");
                yield break;
            }

            InitializeUI();
            _statusUpdateCoroutine = StartCoroutine(UpdateBootStatus());
        }

        private void InitializeUI()
        {
            // Set initial UI state
            if (_statusText != null)
            {
                _statusText.text = "Initializing Project Chimera...";
            }

            if (_progressSlider != null)
            {
                _progressSlider.value = 0f;
            }

            // Set initial background alpha if fade is enabled
            if (_fadeBackground && _backgroundImage != null)
            {
                var color = _backgroundImage.color;
                color.a = 1f;
                _backgroundImage.color = color;
            }
        }

        private IEnumerator UpdateBootStatus()
        {
            float bootStartTime = Time.time;
            
            while (_diGameManager.CurrentGameState != GameState.Running)
            {
                float elapsed = Time.time - bootStartTime;
                
                // Update status text based on elapsed time and game state
                UpdateStatusText(elapsed);
                
                // Update progress slider
                if (_progressSlider != null && _enableProgressAnimation)
                {
                    UpdateProgressSlider(elapsed);
                }
                
                // Check for error state
                if (_diGameManager.CurrentGameState == GameState.Error)
                {
                    if (_statusText != null)
                    {
                        _statusText.text = "Boot Error - Check Console";
                    }
                    yield break;
                }
                
                yield return new WaitForSeconds(0.1f);
            }

            // Boot complete - final UI updates
            if (_statusText != null)
            {
                _statusText.text = "Loading Main Menu...";
            }

            if (_progressSlider != null)
            {
                _progressSlider.value = 1f;
            }

            // Fade background if enabled
            if (_fadeBackground && _backgroundImage != null)
            {
                yield return StartCoroutine(FadeBackgroundOut());
            }
        }

        private void UpdateStatusText(float elapsed)
        {
            if (_statusText == null) return;

            string statusMessage;
            
            if (elapsed < 1.0f)
            {
                statusMessage = "Initializing Project Chimera...";
            }
            else if (elapsed < 3.0f)
            {
                statusMessage = "Loading Core Managers...";
            }
            else if (elapsed < 4.0f)
            {
                statusMessage = "Validating Systems...";
            }
            else
            {
                statusMessage = "Preparing Scene Transition...";
            }

            _statusText.text = statusMessage;
        }

        private void UpdateProgressSlider(float elapsed)
        {
            // Estimate progress based on typical boot phases
            float estimatedTotalTime = 5.0f; // Assume 5 second total boot time
            float progress = Mathf.Clamp01(elapsed / estimatedTotalTime);
            
            // Smooth the progress animation
            _progressSlider.value = Mathf.Lerp(_progressSlider.value, progress, Time.deltaTime * 2f);
        }

        private IEnumerator FadeBackgroundOut()
        {
            var startColor = _backgroundImage.color;
            var targetColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
            
            float elapsed = 0f;
            float fadeDuration = 1f / _fadeSpeed;
            
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                
                _backgroundImage.color = Color.Lerp(startColor, targetColor, t);
                
                yield return null;
            }
            
            _backgroundImage.color = targetColor;
        }

        private void OnDestroy()
        {
            if (_statusUpdateCoroutine != null)
            {
                StopCoroutine(_statusUpdateCoroutine);
            }
        }
    }
}