using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectChimera.Systems.Genetics;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.UI.Genetics
{
    /// <summary>
    /// Tissue culture creation UI panel - allows players to create tissue cultures from plants.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST:
    /// =====================================
    /// Makes tissue culture feel POWERFUL and STRATEGIC:
    ///
    /// 1. **Preserve Elite Genetics** - Insurance policy
    ///    - Player finds exceptional phenotype (rare 0.5% roll)
    ///    - "I need to preserve this before something happens!"
    ///    - Creates tissue culture → genetics saved forever
    ///
    /// 2. **Success Rate Feedback** - Risk/Reward decision
    ///    - Shows 85% success rate based on plant health
    ///    - "Should I try now or improve plant health first?"
    ///    - Visual feedback: Green = healthy, Yellow = risky
    ///
    /// 3. **Culture Viability** - Time pressure mechanic
    ///    - Cultures start at 100% viability, decay over time
    ///    - "I need to micropropagate before viability drops!"
    ///    - Strategic timing creates engagement
    ///
    /// 4. **Named Cultures** - Player ownership
    ///    - "Elite Blue Dream Pheno #3"
    ///    - Players name their achievements
    ///    - Builds emotional connection to genetics
    ///
    /// INVISIBLE COMPLEXITY:
    /// Players see simple "Create Tissue Culture" button, not the complex backend.
    /// They experience strategic choices without technical overwhelm.
    /// </summary>
    public class TissueCulturePanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _plantNameText;
        [SerializeField] private TextMeshProUGUI _plantHealthText;
        [SerializeField] private TextMeshProUGUI _successRateText;
        [SerializeField] private Slider _viabilitySlider;
        [SerializeField] private TextMeshProUGUI _viabilityPercentText;
        [SerializeField] private TMP_InputField _cultureNameInput;
        [SerializeField] private Button _createCultureButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private TextMeshProUGUI _resultMessageText;
        [SerializeField] private GameObject _resultMessagePanel;

        [Header("Colors")]
        [SerializeField] private Color _healthyColor = Color.green;
        [SerializeField] private Color _moderateColor = Color.yellow;
        [SerializeField] private Color _unhealthyColor = Color.red;
        [SerializeField] private Color _successColor = Color.green;
        [SerializeField] private Color _failureColor = Color.red;

        [Header("Success Rate Thresholds")]
        [SerializeField] private float _healthyThreshold = 0.75f;
        [SerializeField] private float _moderateThreshold = 0.50f;

        private TissueCultureManager _tissueCultureManager;
        private string _selectedPlantId;
        private PlantGenotype _selectedGenotype;
        private float _currentSuccessRate;

        private void Awake()
        {
            // Hide panel by default
            if (_panelRoot != null)
                _panelRoot.SetActive(false);

            if (_resultMessagePanel != null)
                _resultMessagePanel.SetActive(false);

            // Setup button listeners
            if (_createCultureButton != null)
                _createCultureButton.onClick.AddListener(OnCreateCultureClicked);

            if (_cancelButton != null)
                _cancelButton.onClick.AddListener(OnCancelClicked);
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
                    "TissueCulturePanel: TissueCultureManager not found in service container", this);
            }
            else
            {
                // Subscribe to tissue culture events
                _tissueCultureManager.OnTissueCultureCreated += OnTissueCultureCreated;
            }
        }

        /// <summary>
        /// Shows tissue culture panel for a specific plant.
        ///
        /// GAMEPLAY: Called when player selects plant and clicks "Create Tissue Culture".
        /// Displays plant info, success rate, and culture creation UI.
        /// </summary>
        public void ShowPanel(string plantId, PlantGenotype genotype, float plantHealth = 1.0f)
        {
            if (string.IsNullOrEmpty(plantId) || genotype == null)
            {
                ChimeraLogger.LogWarning("UI",
                    "Cannot show tissue culture panel: invalid plant data", this);
                return;
            }

            if (_tissueCultureManager == null)
            {
                ChimeraLogger.LogWarning("UI",
                    "Cannot show tissue culture panel: manager unavailable", this);
                return;
            }

            _selectedPlantId = plantId;
            _selectedGenotype = genotype;

            // Display plant information
            if (_plantNameText != null)
            {
                _plantNameText.text = $"Source Plant: {genotype.StrainName}";
            }

            // Display plant health
            if (_plantHealthText != null)
            {
                _plantHealthText.text = $"Plant Health: {plantHealth * 100f:F0}%";
                _plantHealthText.color = GetHealthColor(plantHealth);
            }

            // Calculate and display success rate
            _currentSuccessRate = CalculateSuccessRate(plantHealth);
            if (_successRateText != null)
            {
                _successRateText.text = $"Success Rate: {_currentSuccessRate * 100f:F0}%";
                _successRateText.color = GetSuccessRateColor(_currentSuccessRate);
            }

            // Initialize viability slider (cultures start at 100%)
            if (_viabilitySlider != null)
            {
                _viabilitySlider.value = 1.0f;
            }

            if (_viabilityPercentText != null)
            {
                _viabilityPercentText.text = "100%";
            }

            // Pre-fill culture name with strain name
            if (_cultureNameInput != null)
            {
                _cultureNameInput.text = $"{genotype.StrainName} Culture";
            }

            // Hide result message
            if (_resultMessagePanel != null)
                _resultMessagePanel.SetActive(false);

            // Enable create button
            if (_createCultureButton != null)
                _createCultureButton.interactable = true;

            // Show panel
            if (_panelRoot != null)
                _panelRoot.SetActive(true);

            ChimeraLogger.Log("UI",
                $"Showing tissue culture panel for: {genotype.StrainName} (Success rate: {_currentSuccessRate * 100f:F0}%)", this);
        }

        /// <summary>
        /// Calculates success rate based on plant health.
        ///
        /// GAMEPLAY:
        /// - Healthy plants (75%+) → 85% success rate (standard)
        /// - Moderate health (50-75%) → 70% success rate (risky)
        /// - Unhealthy plants (<50%) → 50% success rate (very risky)
        ///
        /// This creates strategic decision: "Should I improve plant health first?"
        /// </summary>
        private float CalculateSuccessRate(float plantHealth)
        {
            // Base success rate: 85% for healthy plants
            const float baseSuccessRate = 0.85f;
            const float minSuccessRate = 0.50f;

            if (plantHealth >= _healthyThreshold)
            {
                // Healthy: 85% base rate
                return baseSuccessRate;
            }
            else if (plantHealth >= _moderateThreshold)
            {
                // Moderate: Lerp between 70-85%
                float t = (plantHealth - _moderateThreshold) / (_healthyThreshold - _moderateThreshold);
                return Mathf.Lerp(0.70f, baseSuccessRate, t);
            }
            else
            {
                // Unhealthy: Lerp between 50-70%
                float t = plantHealth / _moderateThreshold;
                return Mathf.Lerp(minSuccessRate, 0.70f, t);
            }
        }

        /// <summary>
        /// Gets color for health display (green/yellow/red).
        /// </summary>
        private Color GetHealthColor(float health)
        {
            if (health >= _healthyThreshold)
                return _healthyColor;
            else if (health >= _moderateThreshold)
                return _moderateColor;
            else
                return _unhealthyColor;
        }

        /// <summary>
        /// Gets color for success rate display.
        /// </summary>
        private Color GetSuccessRateColor(float successRate)
        {
            if (successRate >= 0.75f)
                return _healthyColor;
            else if (successRate >= 0.60f)
                return _moderateColor;
            else
                return _unhealthyColor;
        }

        /// <summary>
        /// Called when player clicks "Create Tissue Culture" button.
        ///
        /// GAMEPLAY:
        /// - Validates culture name (not empty)
        /// - Calls backend TissueCultureManager.CreateTissueCulture()
        /// - Shows success/failure message with visual feedback
        /// - On success: Culture is created and stored in manager
        /// - On failure: Encourages player to try again or improve plant health
        /// </summary>
        private void OnCreateCultureClicked()
        {
            if (_tissueCultureManager == null || string.IsNullOrEmpty(_selectedPlantId) || _selectedGenotype == null)
            {
                ShowResultMessage("Error: Cannot create tissue culture", false);
                return;
            }

            // Validate culture name
            string cultureName = _cultureNameInput != null ? _cultureNameInput.text.Trim() : "";
            if (string.IsNullOrEmpty(cultureName))
            {
                ShowResultMessage("Please enter a culture name", false);
                return;
            }

            // Disable button during creation
            if (_createCultureButton != null)
                _createCultureButton.interactable = false;

            // Create tissue culture via backend
            bool success = _tissueCultureManager.CreateTissueCulture(
                _selectedPlantId,
                cultureName,
                _selectedGenotype
            );

            // Event will be fired by manager, showing result
            // If creation fails immediately (validation), show failure message
            if (!success)
            {
                ShowResultMessage($"Failed to create tissue culture '{cultureName}'", false);
                if (_createCultureButton != null)
                    _createCultureButton.interactable = true;
            }
        }

        /// <summary>
        /// Event handler for tissue culture creation completion.
        /// Called by TissueCultureManager when culture creation succeeds/fails.
        ///
        /// GAMEPLAY: Shows visual feedback - green checkmark or red X.
        /// </summary>
        private void OnTissueCultureCreated(string cultureId, string cultureName, bool success, float viability)
        {
            if (success)
            {
                ShowResultMessage(
                    $"✅ Tissue culture '{cultureName}' created successfully!\nViability: {viability * 100f:F0}%",
                    true
                );

                ChimeraLogger.Log("UI",
                    $"Tissue culture created: {cultureName} (ID: {cultureId}, Viability: {viability * 100f:F0}%)", this);

                // Close panel after short delay
                Invoke(nameof(OnCancelClicked), 2.0f);
            }
            else
            {
                ShowResultMessage(
                    $"❌ Tissue culture creation failed.\nTry improving plant health or try again.",
                    false
                );

                // Re-enable create button for retry
                if (_createCultureButton != null)
                    _createCultureButton.interactable = true;
            }
        }

        /// <summary>
        /// Shows result message with success/failure styling.
        ///
        /// GAMEPLAY: Visual feedback makes outcome clear.
        /// Green text + checkmark = success (dopamine hit!)
        /// Red text + X = failure (motivates improvement)
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

            _selectedPlantId = null;
            _selectedGenotype = null;

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
                _tissueCultureManager.OnTissueCultureCreated -= OnTissueCultureCreated;
            }

            // Clean up button listeners
            if (_createCultureButton != null)
                _createCultureButton.onClick.RemoveListener(OnCreateCultureClicked);

            if (_cancelButton != null)
                _cancelButton.onClick.RemoveListener(OnCancelClicked);
        }
    }
}
