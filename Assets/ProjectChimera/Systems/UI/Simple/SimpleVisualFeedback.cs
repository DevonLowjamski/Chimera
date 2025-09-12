using UnityEngine;
using UnityEngine.UIElements;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.UI.Simple
{
    /// <summary>
    /// Simple Visual Feedback - Aligned with Project Chimera's vision
    /// Provides basic visual cues and feedback as described in gameplay document
    /// Focuses on essential visual indicators without complex animations
    /// </summary>
    public class SimpleVisualFeedback : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _hoverColor = new Color(0.9f, 0.9f, 0.9f);
        [SerializeField] private Color _selectedColor = new Color(0.8f, 0.9f, 1f);
        [SerializeField] private Color _disabledColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private Color _errorColor = Color.red;
        [SerializeField] private Color _successColor = Color.green;

        [Header("Feedback Settings")]
        [SerializeField] private float _transitionDuration = 0.2f;
        [SerializeField] private float _highlightIntensity = 1.2f;

        /// <summary>
        /// Applies visual state to a UI element
        /// </summary>
        public void SetVisualState(VisualElement element, string state)
        {
            if (element == null) return;

            switch (state.ToLower())
            {
                case "normal":
                    ApplyColor(element, _normalColor);
                    break;
                case "hover":
                    ApplyColor(element, _hoverColor);
                    break;
                case "selected":
                    ApplyColor(element, _selectedColor);
                    ApplyHighlight(element);
                    break;
                case "disabled":
                    ApplyColor(element, _disabledColor);
                    element.SetEnabled(false);
                    break;
                case "error":
                    ApplyColor(element, _errorColor);
                    break;
                case "success":
                    ApplyColor(element, _successColor);
                    break;
                default:
                    ApplyColor(element, _normalColor);
                    break;
            }

            ChimeraLogger.LogVerbose($"[SimpleVisualFeedback] Applied {state} state to {element.name}");
        }

        /// <summary>
        /// Shows a simple visual cue for mode switching (as described in gameplay document)
        /// </summary>
        public void ShowModeTransition(VisualElement container, string newMode)
        {
            if (container == null) return;

            // Simple color change for mode transition as mentioned in gameplay document
            Color modeColor = GetModeColor(newMode);
            ApplyColorTransition(container, modeColor);

            ChimeraLogger.Log($"[SimpleVisualFeedback] Transitioned to {newMode} mode");
        }

        /// <summary>
        /// Displays trait overlay on plants (as described in genetics mode)
        /// </summary>
        public void ShowTraitOverlay(GameObject plant, string traitInfo)
        {
            if (plant == null) return;

            // Simple overlay - could be a floating text or simple visual indicator
            // This aligns with "trait overlays on plants" mentioned in gameplay document
            var overlay = plant.GetComponent<TraitOverlay>();
            if (overlay == null)
            {
                overlay = plant.AddComponent<TraitOverlay>();
            }

            overlay.ShowTraitInfo(traitInfo);
        }

        /// <summary>
        /// Shows time mode indicator (as described in gameplay document)
        /// </summary>
        public void ShowTimeModeIndicator(VisualElement indicator, string timeMode)
        {
            if (indicator == null) return;

            // Simple visual cue for time mode as mentioned in gameplay document
            Color indicatorColor = GetTimeModeColor(timeMode);
            ApplyColor(indicator, indicatorColor);

            // Add simple text indicator
            if (indicator is Label label)
            {
                label.text = timeMode;
            }
        }

        /// <summary>
        /// Shows real-time alert for critical events (as described in gameplay document)
        /// </summary>
        public void ShowCriticalAlert(VisualElement alertPanel, string message)
        {
            if (alertPanel == null) return;

            // Simple alert display as mentioned in gameplay document
            alertPanel.style.display = DisplayStyle.Flex;

            var messageLabel = alertPanel.Q<Label>("alert-message");
            if (messageLabel != null)
            {
                messageLabel.text = message;
                ApplyColor(messageLabel, _errorColor);
            }

            // Auto-hide after a few seconds
            StartCoroutine(HideAlertAfterDelay(alertPanel, 3f));

            ChimeraLogger.Log($"[SimpleVisualFeedback] Showed critical alert: {message}");
        }

        /// <summary>
        /// Applies blueprint overlay for construction mode (as described in gameplay document)
        /// </summary>
        public void ApplyBlueprintOverlay(Camera camera)
        {
            if (camera == null) return;

            // Simple overlay effect for construction mode as mentioned in gameplay document
            // This could be a simple color tint or shader effect
            var overlay = camera.GetComponent<BlueprintOverlay>();
            if (overlay == null)
            {
                overlay = camera.AddComponent<BlueprintOverlay>();
            }

            overlay.Enable();
        }

        /// <summary>
        /// Removes blueprint overlay
        /// </summary>
        public void RemoveBlueprintOverlay(Camera camera)
        {
            if (camera == null) return;

            var overlay = camera.GetComponent<BlueprintOverlay>();
            if (overlay != null)
            {
                overlay.Disable();
            }
        }

        /// <summary>
        /// Highlights a selected element
        /// </summary>
        public void HighlightElement(VisualElement element)
        {
            if (element == null) return;

            ApplyHighlight(element);
        }

        /// <summary>
        /// Removes highlight from element
        /// </summary>
        public void RemoveHighlight(VisualElement element)
        {
            if (element == null) return;

            RemoveHighlight(element);
        }

        // Private helper methods

        private void ApplyColor(VisualElement element, Color color)
        {
            if (element.style.color != null)
            {
                element.style.color = new StyleColor(color);
            }
        }

        private void ApplyColorTransition(VisualElement element, Color targetColor)
        {
            // Simple instant color change - could be enhanced with simple tween if needed
            ApplyColor(element, targetColor);
        }

        private void ApplyHighlight(VisualElement element)
        {
            // Simple highlight effect
            Color highlightColor = _selectedColor * _highlightIntensity;
            ApplyColor(element, highlightColor);
        }

        private void RemoveHighlight(VisualElement element)
        {
            ApplyColor(element, _normalColor);
        }

        private Color GetModeColor(string mode)
        {
            switch (mode.ToLower())
            {
                case "construction": return new Color(0.8f, 0.9f, 1f); // Light blue
                case "cultivation": return new Color(0.9f, 1f, 0.8f);  // Light green
                case "genetics": return new Color(1f, 0.9f, 0.8f);     // Light orange
                default: return _normalColor;
            }
        }

        private Color GetTimeModeColor(string timeMode)
        {
            switch (timeMode.ToLower())
            {
                case "realtime": return Color.green;
                case "accelerated": return Color.yellow;
                case "paused": return Color.red;
                default: return _normalColor;
            }
        }

        private System.Collections.IEnumerator HideAlertAfterDelay(VisualElement alertPanel, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (alertPanel != null)
            {
                alertPanel.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// Checks if the visual feedback system is ready
        /// </summary>
        public bool IsSystemReady()
        {
            return true; // Simple system is always ready
        }
    }

    /// <summary>
    /// Simple trait overlay component
    /// </summary>
    public class TraitOverlay : MonoBehaviour
    {
        private UnityEngine.UI.Text _traitText;

        private void Awake()
        {
            // Create simple text overlay
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var textObj = new GameObject("TraitText");
            textObj.transform.SetParent(transform);
            _traitText = textObj.AddComponent<UnityEngine.UI.Text>();
            _traitText.fontSize = 12;
            _traitText.color = Color.white;
            _traitText.alignment = TextAnchor.MiddleCenter;

            // Position above the plant
            textObj.transform.localPosition = new Vector3(0, 2, 0);

            gameObject.SetActive(false);
        }

        public void ShowTraitInfo(string info)
        {
            if (_traitText != null)
            {
                _traitText.text = info;
                gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Simple blueprint overlay component
    /// </summary>
    public class BlueprintOverlay : MonoBehaviour
    {
        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            // Simple blueprint effect - could be enhanced with a shader
            Graphics.Blit(src, dest);
        }

        public void Enable()
        {
            enabled = true;
        }

        public void Disable()
        {
            enabled = false;
        }
    }
}
