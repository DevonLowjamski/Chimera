using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectChimera.Core.Interfaces;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.UI.Genetics
{
    /// <summary>
    /// Breeding UI panel - allows players to cross two plants using blockchain genetics.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST:
    /// =====================================
    /// Makes breeding feel EXCITING and STRATEGIC:
    ///
    /// 1. **Parent Preview** - See what you're working with
    ///    - Parent 1: "Blue Dream (28% THC, 1.5kg yield)"
    ///    - Parent 2: "OG Kush (24% THC, 1.2kg yield)"
    ///    - Visual trait comparison side-by-side
    ///
    /// 2. **Expected Offspring** - Strategic planning
    ///    - "Expected THC: 24-28% (89% heritable - predictable)"
    ///    - "Expected Yield: 0.8-1.8kg (47% heritable - variable!)"
    ///    - Player learns trait heritability through UI
    ///
    /// 3. **Instant Breeding** - GPU magic
    ///    - Click "Breed" → Brief animation (<0.1s GPU mining)
    ///    - "✅ Breeding complete! Offspring seed created."
    ///    - Blockchain verification happens invisibly
    ///
    /// 4. **Verification Badge** - Achievement feeling
    ///    - "✅ Blockchain-Verified Genetics"
    ///    - Shows generation (F1, F2, etc.)
    ///    - Player feels accomplishment
    ///
    /// INVISIBLE BLOCKCHAIN:
    /// Players see "Breed" button, not "Mine proof-of-work with GPU compute shader"
    /// They experience fast, verified breeding without technical complexity.
    /// </summary>
    public class BreedingPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TextMeshProUGUI _titleText;

        [Header("Parent 1 Display")]
        [SerializeField] private TextMeshProUGUI _parent1NameText;
        [SerializeField] private TextMeshProUGUI _parent1THCText;
        [SerializeField] private TextMeshProUGUI _parent1YieldText;
        [SerializeField] private TextMeshProUGUI _parent1FloweringText;

        [Header("Parent 2 Display")]
        [SerializeField] private TextMeshProUGUI _parent2NameText;
        [SerializeField] private TextMeshProUGUI _parent2THCText;
        [SerializeField] private TextMeshProUGUI _parent2YieldText;
        [SerializeField] private TextMeshProUGUI _parent2FloweringText;

        [Header("Expected Offspring")]
        [SerializeField] private TextMeshProUGUI _expectedTHCText;
        [SerializeField] private TextMeshProUGUI _expectedYieldText;
        [SerializeField] private TextMeshProUGUI _expectedFloweringText;
        [SerializeField] private TextMeshProUGUI _generationText;

        [Header("Strain Naming")]
        [SerializeField] private TMP_InputField _strainNameInput;

        [Header("Buttons")]
        [SerializeField] private Button _breedButton;
        [SerializeField] private Button _cancelButton;

        [Header("Result Display")]
        [SerializeField] private TextMeshProUGUI _resultMessageText;
        [SerializeField] private GameObject _resultMessagePanel;

        [Header("Colors")]
        [SerializeField] private Color _successColor = Color.green;
        [SerializeField] private Color _processingColor = Color.yellow;
        [SerializeField] private Color _errorColor = Color.red;

        private IBlockchainGeneticsService _blockchainService;
        private PlantGenotype _parent1;
        private PlantGenotype _parent2;
        private bool _isBreeding = false;

        private void Awake()
        {
            // Hide panel by default
            if (_panelRoot != null)
                _panelRoot.SetActive(false);

            if (_resultMessagePanel != null)
                _resultMessagePanel.SetActive(false);

            // Setup button listeners
            if (_breedButton != null)
                _breedButton.onClick.AddListener(OnBreedClicked);

            if (_cancelButton != null)
                _cancelButton.onClick.AddListener(OnCancelClicked);
        }

        private void Start()
        {
            // Get blockchain service from container
            var container = ServiceContainerFactory.Instance;
            if (container != null)
            {
                _blockchainService = container.Resolve<IBlockchainGeneticsService>();
            }

            if (_blockchainService == null)
            {
                ChimeraLogger.LogWarning("UI",
                    "BreedingPanel: BlockchainGeneticsService not found", this);
            }
        }

        /// <summary>
        /// Shows breeding panel for two selected plants.
        ///
        /// GAMEPLAY: Called when player selects two plants and clicks "Cross Plants".
        /// Displays parent traits and expected offspring preview.
        /// </summary>
        public void ShowPanel(PlantGenotype parent1, PlantGenotype parent2)
        {
            if (parent1 == null || parent2 == null)
            {
                ChimeraLogger.LogWarning("UI",
                    "Cannot show breeding panel: one or both parents are null", this);
                return;
            }

            if (_blockchainService == null)
            {
                ChimeraLogger.LogWarning("UI",
                    "Cannot show breeding panel: blockchain service unavailable", this);
                return;
            }

            _parent1 = parent1;
            _parent2 = parent2;

            // Display parent 1 info
            DisplayParentInfo(
                parent1,
                _parent1NameText,
                _parent1THCText,
                _parent1YieldText,
                _parent1FloweringText
            );

            // Display parent 2 info
            DisplayParentInfo(
                parent2,
                _parent2NameText,
                _parent2THCText,
                _parent2YieldText,
                _parent2FloweringText
            );

            // Calculate and display expected offspring traits
            DisplayExpectedOffspring(parent1, parent2);

            // Pre-fill strain name
            if (_strainNameInput != null)
            {
                _strainNameInput.text = $"{parent1.StrainName} × {parent2.StrainName}";
            }

            // Hide result message
            if (_resultMessagePanel != null)
                _resultMessagePanel.SetActive(false);

            // Enable breed button
            if (_breedButton != null)
                _breedButton.interactable = true;

            // Show panel
            if (_panelRoot != null)
                _panelRoot.SetActive(true);

            ChimeraLogger.Log("UI",
                $"Showing breeding panel: {parent1.StrainName} × {parent2.StrainName}", this);
        }

        /// <summary>
        /// Displays parent plant information.
        /// </summary>
        private void DisplayParentInfo(
            PlantGenotype parent,
            TextMeshProUGUI nameText,
            TextMeshProUGUI thcText,
            TextMeshProUGUI yieldText,
            TextMeshProUGUI floweringText)
        {
            if (nameText != null)
                nameText.text = parent.StrainName;

            if (thcText != null)
                thcText.text = $"THC: {parent.PotencyPotential:F1}%";

            if (yieldText != null)
                yieldText.text = $"Yield: {parent.YieldPotential:F2}kg";

            if (floweringText != null)
                floweringText.text = $"Flowering: {parent.FloweringTime} days";
        }

        /// <summary>
        /// Calculates and displays expected offspring traits.
        ///
        /// GAMEPLAY:
        /// Shows player what to expect based on trait heritability.
        /// - THC (89% heritable): Narrow range, predictable
        /// - Yield (47% heritable): Wide range, environment-dependent
        /// - Flowering (78% heritable): Moderate range
        ///
        /// This teaches players about genetics through UI!
        /// </summary>
        private void DisplayExpectedOffspring(PlantGenotype parent1, PlantGenotype parent2)
        {
            // THC: High heritability (89%) = narrow range
            float avgTHC = (parent1.PotencyPotential + parent2.PotencyPotential) / 2f;
            float thcVariation = avgTHC * 0.12f; // 12% variation coefficient
            if (_expectedTHCText != null)
            {
                _expectedTHCText.text =
                    $"Expected THC: {avgTHC - thcVariation:F1}-{avgTHC + thcVariation:F1}%\n" +
                    "(89% heritable - predictable)";
            }

            // Yield: Low heritability (47%) = wide range
            float avgYield = (parent1.YieldPotential + parent2.YieldPotential) / 2f;
            float yieldVariation = avgYield * 0.25f; // 25% variation coefficient
            if (_expectedYieldText != null)
            {
                _expectedYieldText.text =
                    $"Expected Yield: {avgYield - yieldVariation:F2}-{avgYield + yieldVariation:F2}kg\n" +
                    "(47% heritable - environment matters!)";
            }

            // Flowering: Moderate heritability (78%)
            float avgFlowering = (parent1.FloweringTime + parent2.FloweringTime) / 2f;
            float floweringVariation = avgFlowering * 0.10f; // 10% variation coefficient
            if (_expectedFloweringText != null)
            {
                _expectedFloweringText.text =
                    $"Expected Flowering: {avgFlowering - floweringVariation:F0}-{avgFlowering + floweringVariation:F0} days\n" +
                    "(78% heritable - fairly stable)";
            }

            // Generation
            int parent1Gen = _blockchainService.GetGeneration(parent1);
            int parent2Gen = _blockchainService.GetGeneration(parent2);
            int expectedGen = Mathf.Max(parent1Gen, parent2Gen) + 1;

            if (_generationText != null)
            {
                _generationText.text = $"Generation: F{expectedGen}";
            }
        }

        /// <summary>
        /// Called when player clicks "Breed" button.
        ///
        /// GAMEPLAY:
        /// - Validates strain name
        /// - Shows "Breeding..." message (GPU mining happening!)
        /// - Calls BlockchainGeneticsService.BreedPlantsAsync()
        /// - GPU mines proof-of-work (<0.1s typical)
        /// - Shows success message with offspring info
        /// - Offspring seed added to inventory
        ///
        /// INVISIBLE BLOCKCHAIN:
        /// Player sees: "Breeding... → ✅ Complete!"
        /// Under the hood: SHA-256 hashing, GPU compute shader, proof-of-work, blockchain ledger
        /// </summary>
        private async void OnBreedClicked()
        {
            if (_isBreeding)
                return;

            if (_blockchainService == null || _parent1 == null || _parent2 == null)
            {
                ShowResultMessage("Error: Cannot breed plants", _errorColor);
                return;
            }

            // Validate strain name
            string strainName = _strainNameInput != null ? _strainNameInput.text.Trim() : "";
            if (string.IsNullOrEmpty(strainName))
            {
                ShowResultMessage("Please enter a strain name", _errorColor);
                return;
            }

            _isBreeding = true;

            // Disable breed button during breeding
            if (_breedButton != null)
                _breedButton.interactable = false;

            // Show processing message
            ShowResultMessage("Breeding... (GPU mining proof-of-work)", _processingColor);

            try
            {
                // Perform breeding with blockchain genetics
                // This includes:
                // 1. Fractal genetics calculation (trait inheritance)
                // 2. GPU proof-of-work mining (instant on GPU)
                // 3. Blockchain ledger update (immutable record)
                // 4. Offspring genotype creation
                PlantGenotype offspring = await _blockchainService.BreedPlantsAsync(
                    _parent1,
                    _parent2,
                    strainName
                );

                if (offspring != null)
                {
                    // Get verification info for display
                    var verificationInfo = _blockchainService.GetVerificationInfo(offspring);

                    ShowResultMessage(
                        $"✅ Breeding complete!\n" +
                        $"Offspring: {offspring.StrainName}\n" +
                        $"THC: {offspring.PotencyPotential:F1}%\n" +
                        $"Yield: {offspring.YieldPotential:F2}kg\n" +
                        $"Generation: {verificationInfo.GenerationLabel}\n" +
                        $"✅ Blockchain-Verified\n" +
                        $"Seed added to inventory.",
                        _successColor
                    );

                    ChimeraLogger.Log("UI",
                        $"Breeding successful: {offspring.StrainName} " +
                        $"(THC: {offspring.PotencyPotential:F1}%, Yield: {offspring.YieldPotential:F2}kg, " +
                        $"Gen: {verificationInfo.GenerationLabel})", this);

                    // Close panel after delay
                    Invoke(nameof(OnCancelClicked), 3.0f);
                }
                else
                {
                    ShowResultMessage("❌ Breeding failed. Please try again.", _errorColor);

                    // Re-enable button
                    if (_breedButton != null)
                        _breedButton.interactable = true;
                }
            }
            catch (System.Exception ex)
            {
                ShowResultMessage($"❌ Breeding error: {ex.Message}", _errorColor);
                ChimeraLogger.LogError("UI", $"Breeding error: {ex.Message}", this);

                // Re-enable button
                if (_breedButton != null)
                    _breedButton.interactable = true;
            }
            finally
            {
                _isBreeding = false;
            }
        }

        /// <summary>
        /// Shows result message with color coding.
        /// </summary>
        private void ShowResultMessage(string message, Color color)
        {
            if (_resultMessageText != null)
            {
                _resultMessageText.text = message;
                _resultMessageText.color = color;
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

            _parent1 = null;
            _parent2 = null;
            _isBreeding = false;

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
            // Clean up button listeners
            if (_breedButton != null)
                _breedButton.onClick.RemoveListener(OnBreedClicked);

            if (_cancelButton != null)
                _cancelButton.onClick.RemoveListener(OnCancelClicked);
        }
    }
}
