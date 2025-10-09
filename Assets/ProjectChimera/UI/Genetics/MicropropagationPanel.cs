using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectChimera.Systems.Genetics;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;
using TissueCultureData = ProjectChimera.Systems.Genetics.TissueCulture;

namespace ProjectChimera.UI.Genetics
{
    /// <summary>
    /// Micropropagation UI panel - allows players to clone plants from tissue cultures.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST:
    /// =====================================
    /// Makes micropropagation feel POWERFUL and STRATEGIC:
    ///
    /// 1. **Infinite Genetics** - The payoff for tissue culture
    ///    - Player created tissue culture of elite phenotype
    ///    - Now can create 10-100 identical clones!
    ///    - "I found one amazing plant, now I can fill my facility with it!"
    ///
    /// 2. **Viability Decay** - Time pressure mechanic
    ///    - Cultures decay from 100% → 0% over time
    ///    - High viability (80%+) → 90% success per clone
    ///    - Low viability (20%) → 20% success per clone
    ///    - "I need to propagate before viability drops!"
    ///
    /// 3. **Batch Cloning** - Resource management
    ///    - Slider: 1-100 clones
    ///    - Shows expected successful clones based on viability
    ///    - "Do I make 10 high-quality clones or 100 risky clones?"
    ///
    /// 4. **Success Tracking** - Clear feedback
    ///    - "90 successful clones, 10 failed"
    ///    - Visual feedback on clone quality
    ///    - Failed clones don't waste resources (just fewer seeds)
    ///
    /// INVISIBLE COMPLEXITY:
    /// Players see "Create 50 Clones" not "micropropagation with 78% viability modifier"
    /// They experience strategic choices without technical overwhelm.
    /// </summary>
    public class MicropropagationPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _cultureNameText;
        [SerializeField] private TextMeshProUGUI _strainNameText;
        [SerializeField] private Slider _viabilitySlider;
        [SerializeField] private TextMeshProUGUI _viabilityPercentText;
        [SerializeField] private Slider _quantitySlider;
        [SerializeField] private TextMeshProUGUI _quantityText;
        [SerializeField] private TextMeshProUGUI _expectedSuccessText;
        [SerializeField] private TextMeshProUGUI _successRateText;
        [SerializeField] private Button _micropropagateButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private TextMeshProUGUI _resultMessageText;
        [SerializeField] private GameObject _resultMessagePanel;

        [Header("Colors")]
        [SerializeField] private Color _highViabilityColor = Color.green;
        [SerializeField] private Color _mediumViabilityColor = Color.yellow;
        [SerializeField] private Color _lowViabilityColor = Color.red;
        [SerializeField] private Color _successColor = Color.green;
        [SerializeField] private Color _failureColor = Color.red;

        [Header("Viability Thresholds")]
        [SerializeField] private float _highViabilityThreshold = 0.75f;
        [SerializeField] private float _mediumViabilityThreshold = 0.40f;

        [Header("Quantity Limits")]
        [SerializeField] private int _minQuantity = 1;
        [SerializeField] private int _maxQuantity = 100;

        private TissueCultureManager _tissueCultureManager;
        private string _selectedCultureId;
        private TissueCultureData? _selectedCulture;

        private void Awake()
        {
            // Hide panel by default
            if (_panelRoot != null)
                _panelRoot.SetActive(false);

            if (_resultMessagePanel != null)
                _resultMessagePanel.SetActive(false);

            // Setup button listeners
            if (_micropropagateButton != null)
                _micropropagateButton.onClick.AddListener(OnMicropropagateClicked);

            if (_cancelButton != null)
                _cancelButton.onClick.AddListener(OnCancelClicked);

            // Setup quantity slider
            if (_quantitySlider != null)
            {
                _quantitySlider.minValue = _minQuantity;
                _quantitySlider.maxValue = _maxQuantity;
                _quantitySlider.wholeNumbers = true;
                _quantitySlider.value = 10; // Default to 10 clones
                _quantitySlider.onValueChanged.AddListener(OnQuantityChanged);
            }
        }

        private void Start()
        {
            // Get tissue culture manager from container
            var container = ServiceContainerFactory.Instance;
            if (container != null)
            {
                _tissueCultureManager = container.Resolve<TissueCultureManager>();
            }

            if (_tissueCultureManager == null)
            {
                ChimeraLogger.LogWarning("UI",
                    "MicropropagationPanel: TissueCultureManager not found in service container", this);
            }
            else
            {
                // Subscribe to micropropagation events
                _tissueCultureManager.OnMicropropagationCompleted += OnMicropropagationCompleted;
            }
        }

        /// <summary>
        /// Shows micropropagation panel for a specific tissue culture.
        ///
        /// GAMEPLAY: Called when player selects culture and clicks "Micropropagate".
        /// Displays culture info, viability, and clone quantity selector.
        /// </summary>
        public void ShowPanel(string cultureId)
        {
            if (string.IsNullOrEmpty(cultureId))
            {
                ChimeraLogger.LogWarning("UI",
                    "Cannot show micropropagation panel: invalid culture ID", this);
                return;
            }

            if (_tissueCultureManager == null)
            {
                ChimeraLogger.LogWarning("UI",
                    "Cannot show micropropagation panel: manager unavailable", this);
                return;
            }

            // Get culture data from manager
            var culture = _tissueCultureManager.GetCulture(cultureId);
            if (!culture.HasValue)
            {
                ChimeraLogger.LogWarning("UI",
                    $"Cannot show micropropagation panel: culture {cultureId} not found", this);
                return;
            }

            _selectedCultureId = cultureId;
            _selectedCulture = culture;

            // Display culture name
            if (_cultureNameText != null)
            {
                _cultureNameText.text = $"Culture: {culture.Value.CultureName}";
            }

            // Display strain name
            if (_strainNameText != null && culture.Value.SourceGenotype != null)
            {
                _strainNameText.text = $"Strain: {culture.Value.SourceGenotype.StrainName}";
            }

            // Display viability
            UpdateViabilityDisplay(culture.Value.CurrentViability);

            // Reset quantity slider to default
            if (_quantitySlider != null)
            {
                _quantitySlider.value = 10;
            }

            // Update expected success
            UpdateExpectedSuccess();

            // Hide result message
            if (_resultMessagePanel != null)
                _resultMessagePanel.SetActive(false);

            // Enable micropropagate button
            if (_micropropagateButton != null)
                _micropropagateButton.interactable = true;

            // Show panel
            if (_panelRoot != null)
                _panelRoot.SetActive(true);

            ChimeraLogger.Log("UI",
                $"Showing micropropagation panel for: {_selectedCulture.Value.CultureName} (Viability: {_selectedCulture.Value.CurrentViability * 100f:F0}%)", this);
        }

        /// <summary>
        /// Updates viability display with color coding.
        ///
        /// GAMEPLAY:
        /// - Green (75%+): High success rate, optimal time to propagate
        /// - Yellow (40-75%): Medium success rate, acceptable but declining
        /// - Red (<40%): Low success rate, risky to propagate
        /// </summary>
        private void UpdateViabilityDisplay(float viability)
        {
            if (_viabilitySlider != null)
            {
                _viabilitySlider.value = viability;
            }

            if (_viabilityPercentText != null)
            {
                _viabilityPercentText.text = $"{viability * 100f:F0}%";
                _viabilityPercentText.color = GetViabilityColor(viability);
            }
        }

        /// <summary>
        /// Gets color for viability display (green/yellow/red).
        /// </summary>
        private Color GetViabilityColor(float viability)
        {
            if (viability >= _highViabilityThreshold)
                return _highViabilityColor;
            else if (viability >= _mediumViabilityThreshold)
                return _mediumViabilityColor;
            else
                return _lowViabilityColor;
        }

        /// <summary>
        /// Called when quantity slider value changes.
        /// Updates expected success count display.
        /// </summary>
        private void OnQuantityChanged(float value)
        {
            UpdateExpectedSuccess();
        }

        /// <summary>
        /// Updates expected success count based on quantity and viability.
        ///
        /// GAMEPLAY:
        /// Shows player what to expect: "10 clones requested → expect ~9 successful"
        /// Helps player make informed decision about batch size.
        /// </summary>
        private void UpdateExpectedSuccess()
        {
            if (!_selectedCulture.HasValue || string.IsNullOrEmpty(_selectedCultureId))
                return;

            int requestedQuantity = _quantitySlider != null ? (int)_quantitySlider.value : 10;

            // Calculate success rate based on viability
            // High viability (90%) → 90% success per clone
            // Low viability (20%) → 20% success per clone
            float successRate = _selectedCulture.Value.CurrentViability * 0.90f; // Max 90% success rate
            int expectedSuccess = Mathf.RoundToInt(requestedQuantity * successRate);

            // Update quantity text
            if (_quantityText != null)
            {
                _quantityText.text = $"{requestedQuantity} clones";
            }

            // Update expected success text
            if (_expectedSuccessText != null)
            {
                _expectedSuccessText.text = $"Expected Success: ~{expectedSuccess} clones";
            }

            // Update success rate text
            if (_successRateText != null)
            {
                _successRateText.text = $"Success Rate: {successRate * 100f:F0}%";
                _successRateText.color = GetViabilityColor(_selectedCulture.Value.CurrentViability);
            }
        }

        /// <summary>
        /// Called when player clicks "Micropropagate" button.
        ///
        /// GAMEPLAY:
        /// - Gets quantity from slider
        /// - Calls backend TissueCultureManager.Micropropagate()
        /// - Shows success/failure message with clone count
        /// - Successful clones are added to player's seed inventory
        /// - Failed clones just don't appear (not a punishment, just fewer seeds)
        /// </summary>
        private void OnMicropropagateClicked()
        {
            if (_tissueCultureManager == null || string.IsNullOrEmpty(_selectedCultureId))
            {
                ShowResultMessage("Error: Cannot micropropagate", false);
                return;
            }

            int quantity = _quantitySlider != null ? (int)_quantitySlider.value : 10;

            // Disable button during propagation
            if (_micropropagateButton != null)
                _micropropagateButton.interactable = false;

            // Perform micropropagation via backend
            BreedingSeed[] cloneSeeds;
            bool success = _tissueCultureManager.Micropropagate(_selectedCultureId, quantity, out cloneSeeds);

            // Event will be fired by manager, but we can show immediate feedback
            if (!success || cloneSeeds == null || cloneSeeds.Length == 0)
            {
                ShowResultMessage(
                    $"❌ Micropropagation failed.\nCulture may have expired or become contaminated.",
                    false
                );

                // Re-enable button
                if (_micropropagateButton != null)
                    _micropropagateButton.interactable = true;
            }
            // Success case handled by event callback
        }

        /// <summary>
        /// Event handler for micropropagation completion.
        /// Called by TissueCultureManager when propagation succeeds/fails.
        ///
        /// GAMEPLAY: Shows visual feedback with clone count.
        /// </summary>
        private void OnMicropropagationCompleted(string cultureId, int requestedQuantity, int successfulClones, BreedingSeed[] cloneSeeds)
        {
            // Only handle events for our selected culture
            if (cultureId != _selectedCultureId)
                return;

            if (successfulClones > 0)
            {
                float successRate = (float)successfulClones / requestedQuantity * 100f;

                ShowResultMessage(
                    $"✅ Micropropagation complete!\n" +
                    $"{successfulClones} of {requestedQuantity} clones successful ({successRate:F0}% success rate)\n" +
                    $"Clone seeds added to inventory.",
                    true
                );

                ChimeraLogger.Log("UI",
                    $"Micropropagation successful: {successfulClones}/{requestedQuantity} clones from culture {cultureId}", this);

                // Close panel after short delay
                Invoke(nameof(OnCancelClicked), 3.0f);
            }
            else
            {
                ShowResultMessage(
                    $"❌ Micropropagation failed.\n" +
                    $"0 of {requestedQuantity} clones successful.\n" +
                    $"Culture viability may be too low.",
                    false
                );

                // Re-enable button for retry
                if (_micropropagateButton != null)
                    _micropropagateButton.interactable = true;
            }
        }

        /// <summary>
        /// Shows result message with success/failure styling.
        ///
        /// GAMEPLAY: Visual feedback makes outcome clear.
        /// Green text + clone count = success (dopamine hit!)
        /// Red text + guidance = failure (motivates improvement)
        /// </summary>
        private void ShowResultMessage(string message, bool isSuccess)
        {
            if (_resultMessageText != null)
            {
                _resultMessageText.text = message;
                _resultMessageText.color = isSuccess ? _successColor : _failureColor;
            }

            if (_resultMessagePanel != null)
            {
                _resultMessagePanel.SetActive(true);
            }
        }

        /// <summary>
        /// Closes the panel and resets state.
        /// </summary>
        private void OnCancelClicked()
        {
            if (_panelRoot != null)
                _panelRoot.SetActive(false);

            if (_resultMessagePanel != null)
                _resultMessagePanel.SetActive(false);

            _selectedCultureId = null;
            _selectedCulture = null; // Nullable struct, so this is valid

            // Cancel any pending delayed close
            CancelInvoke(nameof(OnCancelClicked));
        }

        /// <summary>
        /// Hides the panel (can be called externally).
        /// </summary>
        public void Hide()
        {
            OnCancelClicked();
        }

        /// <summary>
        /// Quick check if panel is currently visible.
        /// </summary>
        public bool IsVisible()
        {
            return _panelRoot != null && _panelRoot.activeSelf;
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (_tissueCultureManager != null)
            {
                _tissueCultureManager.OnMicropropagationCompleted -= OnMicropropagationCompleted;
            }

            // Clean up button listeners
            if (_micropropagateButton != null)
                _micropropagateButton.onClick.RemoveListener(OnMicropropagateClicked);

            if (_cancelButton != null)
                _cancelButton.onClick.RemoveListener(OnCancelClicked);

            if (_quantitySlider != null)
                _quantitySlider.onValueChanged.RemoveListener(OnQuantityChanged);
        }
    }
}
