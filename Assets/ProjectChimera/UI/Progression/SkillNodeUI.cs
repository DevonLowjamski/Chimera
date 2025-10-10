using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using ProjectChimera.Data.Progression;
using System;

namespace ProjectChimera.UI.Progression
{
    /// <summary>
    /// Individual skill node UI element in the progression leaf.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST (from Gameplay Doc):
    /// =========================================================
    /// "The leaf visually expands as players progress"
    ///
    /// **Visual States:**
    /// - **Unlocked**: Node glows/illuminates, shows as completed
    /// - **Available**: Node pulses, player has enough points and prerequisites
    /// - **Locked**: Node appears dim/greyed, prerequisites not met or insufficient points
    ///
    /// **Interaction:**
    /// - Hover: Show tooltip with node name and requirements
    /// - Click: Open details panel for more info and unlock button
    ///
    /// **Visual Feedback:**
    /// Players instantly understand progression state through color and animation.
    /// No complex UI text needed - the leaf tells the story visually.
    /// </summary>
    public class SkillNodeUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("UI References")]
        [SerializeField] private Image _nodeBackground;
        [SerializeField] private Image _nodeIcon;
        [SerializeField] private Image _glowEffect;
        [SerializeField] private GameObject _lockedOverlay;
        [SerializeField] private GameObject _checkmarkIcon;
        [SerializeField] private TextMeshProUGUI _costText;

        [Header("Visual Settings")]
        [SerializeField] private Color _lockedColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        [SerializeField] private Color _availableColor = new Color(1f, 1f, 0.5f, 1f);
        [SerializeField] private Color _unlockedColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private float _pulseSpeed = 2f;
        [SerializeField] private float _pulseIntensity = 0.3f;

        [Header("Tooltip")]
        [SerializeField] private GameObject _tooltipPanel;
        [SerializeField] private TextMeshProUGUI _tooltipNameText;
        [SerializeField] private TextMeshProUGUI _tooltipDescriptionText;
        [SerializeField] private Vector2 _tooltipOffset = new Vector2(0, 50f);

        // Node data
        private SkillNode _node;
        private Color _branchColor;
        private bool _isUnlocked;
        private bool _canUnlock;

        // Events
        public event Action<SkillNode> OnNodeClicked;

        // Animation state
        private float _pulseTimer = 0f;

        /// <summary>
        /// Sets up the node with data and visual state.
        ///
        /// GAMEPLAY:
        /// - Locked node: dim, greyed out, shows cost
        /// - Available node: bright, pulsing, shows cost in green
        /// - Unlocked node: illuminated with branch color, checkmark visible
        /// </summary>
        public void Setup(SkillNode node, Color branchColor, bool isUnlocked, bool canUnlock)
        {
            _node = node;
            _branchColor = branchColor;
            _isUnlocked = isUnlocked;
            _canUnlock = canUnlock;

            UpdateVisuals();
        }

        /// <summary>
        /// Updates unlock state at runtime.
        /// Called when player unlocks node or skill points change.
        /// </summary>
        public void SetUnlocked(bool isUnlocked, bool canUnlock)
        {
            _isUnlocked = isUnlocked;
            _canUnlock = canUnlock;
            UpdateVisuals();
        }

        /// <summary>
        /// Updates all visual elements based on current state.
        /// </summary>
        private void UpdateVisuals()
        {
            if (_node == null)
                return;

            // Update icon
            if (_nodeIcon != null && _node.Icon != null)
            {
                _nodeIcon.sprite = _node.Icon;
            }

            // Update visual state
            if (_isUnlocked)
            {
                ShowUnlockedState();
            }
            else if (_canUnlock)
            {
                ShowAvailableState();
            }
            else
            {
                ShowLockedState();
            }

            // Update cost display
            if (_costText != null)
            {
                if (_isUnlocked)
                {
                    _costText.gameObject.SetActive(false);
                }
                else
                {
                    _costText.gameObject.SetActive(true);
                    _costText.text = _node.SkillPointCost.ToString();
                    _costText.color = _canUnlock ? Color.green : Color.white;
                }
            }
        }

        /// <summary>
        /// Visual state for unlocked nodes.
        ///
        /// GAMEPLAY:
        /// - Node glows with branch color
        /// - Checkmark visible
        /// - No locked overlay
        /// - Icon fully visible
        /// </summary>
        private void ShowUnlockedState()
        {
            if (_nodeBackground != null)
            {
                _nodeBackground.color = _branchColor;
            }

            if (_nodeIcon != null)
            {
                _nodeIcon.color = _unlockedColor;
            }

            if (_glowEffect != null)
            {
                _glowEffect.gameObject.SetActive(true);
                _glowEffect.color = _branchColor;
            }

            if (_lockedOverlay != null)
            {
                _lockedOverlay.SetActive(false);
            }

            if (_checkmarkIcon != null)
            {
                _checkmarkIcon.SetActive(true);
            }
        }

        /// <summary>
        /// Visual state for available nodes (can unlock now).
        ///
        /// GAMEPLAY:
        /// - Node pulses to draw attention
        /// - Bright yellow/gold color
        /// - No locked overlay
        /// - Cost shown in green
        /// </summary>
        private void ShowAvailableState()
        {
            if (_nodeBackground != null)
            {
                _nodeBackground.color = Color.Lerp(_branchColor, _availableColor, 0.5f);
            }

            if (_nodeIcon != null)
            {
                _nodeIcon.color = Color.white;
            }

            if (_glowEffect != null)
            {
                _glowEffect.gameObject.SetActive(true);
                _glowEffect.color = _availableColor;
            }

            if (_lockedOverlay != null)
            {
                _lockedOverlay.SetActive(false);
            }

            if (_checkmarkIcon != null)
            {
                _checkmarkIcon.SetActive(false);
            }
        }

        /// <summary>
        /// Visual state for locked nodes.
        ///
        /// GAMEPLAY:
        /// - Node appears dim/greyed
        /// - Locked overlay visible
        /// - No glow effect
        /// - Cost shown in white
        /// </summary>
        private void ShowLockedState()
        {
            if (_nodeBackground != null)
            {
                _nodeBackground.color = Color.Lerp(_branchColor, _lockedColor, 0.7f);
            }

            if (_nodeIcon != null)
            {
                _nodeIcon.color = _lockedColor;
            }

            if (_glowEffect != null)
            {
                _glowEffect.gameObject.SetActive(false);
            }

            if (_lockedOverlay != null)
            {
                _lockedOverlay.SetActive(true);
            }

            if (_checkmarkIcon != null)
            {
                _checkmarkIcon.SetActive(false);
            }
        }

        // NOTE: Pulse animation removed to comply with Phase 0 Update() limits
        // Alternative: Use Unity Animator or DOTween for UI animations

        #region Mouse Interactions

        /// <summary>
        /// Called when mouse enters node.
        /// Shows tooltip with node info.
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            ShowTooltip();
        }

        /// <summary>
        /// Called when mouse exits node.
        /// Hides tooltip.
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            HideTooltip();
        }

        /// <summary>
        /// Called when node is clicked.
        /// Opens details panel via event.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_node != null)
            {
                OnNodeClicked?.Invoke(_node);
                HideTooltip();
            }
        }

        #endregion

        #region Tooltip

        /// <summary>
        /// Shows tooltip with node name, description, and requirements.
        ///
        /// GAMEPLAY:
        /// - Quick info on hover
        /// - Shows what node unlocks
        /// - Shows cost and prerequisites
        /// - Color-coded status (green = available, grey = locked, gold = unlocked)
        /// </summary>
        private void ShowTooltip()
        {
            if (_tooltipPanel == null || _node == null)
                return;

            // Position tooltip
            var rectTransform = _tooltipPanel.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = _tooltipOffset;
            }

            // Update tooltip content
            if (_tooltipNameText != null)
            {
                _tooltipNameText.text = _node.NodeName;
                _tooltipNameText.color = GetStatusColor();
            }

            if (_tooltipDescriptionText != null)
            {
                string description = _node.Description;

                // Add requirement info
                if (!_isUnlocked)
                {
                    description += $"\n\nCost: {_node.SkillPointCost} Skill Points";

                    if (_node.Prerequisites != null && _node.Prerequisites.Count > 0)
                    {
                        description += $"\nPrerequisites: {_node.Prerequisites.Count} node(s)";
                    }
                }
                else
                {
                    description += "\n\n✅ Unlocked";
                }

                _tooltipDescriptionText.text = description;
            }

            _tooltipPanel.SetActive(true);
        }

        /// <summary>
        /// Hides tooltip.
        /// </summary>
        private void HideTooltip()
        {
            if (_tooltipPanel != null)
            {
                _tooltipPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Gets color for tooltip header based on node state.
        /// </summary>
        private Color GetStatusColor()
        {
            if (_isUnlocked)
                return new Color(1f, 0.84f, 0f); // Gold
            else if (_canUnlock)
                return Color.green;
            else
                return Color.grey;
        }

        #endregion

        /// <summary>
        /// Gets the node data.
        /// </summary>
        public SkillNode Node => _node;

        /// <summary>
        /// Gets unlock state.
        /// </summary>
        public bool IsUnlocked => _isUnlocked;

        /// <summary>
        /// Gets available state.
        /// </summary>
        public bool CanUnlock => _canUnlock;
    }
}
