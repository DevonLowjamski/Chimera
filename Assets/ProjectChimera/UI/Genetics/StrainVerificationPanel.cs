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
    /// Strain verification UI panel - shows blockchain verification status.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST:
    /// =====================================
    /// Makes blockchain benefits VISIBLE and REWARDING to players:
    ///
    /// 1. **✅ Verified Badge** - Achievement feeling
    ///    - Players see their breeding success validated
    ///    - "This is MY strain, proven authentic!"
    ///
    /// 2. **Blockchain ID** - Unique fingerprint
    ///    - Short hash like "a3f5...d8f1" (technical but cool)
    ///    - Players can compare IDs when trading
    ///
    /// 3. **Generation Label** - Progression tracking
    ///    - "F1", "F2", "F3" shows breeding depth
    ///    - Achievement: "Breed an F10 strain!"
    ///
    /// 4. **Lineage Access** - Family tree button
    ///    - "View Lineage" opens family tree visualization
    ///    - See breeding history and parent strains
    ///
    /// INVISIBLE BLOCKCHAIN:
    /// Players see verification results, not blockchain technical details.
    /// "✅ Verified Strain" NOT "Proof-of-work validated with 4 leading zeros"
    /// </summary>
    public class StrainVerificationPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TextMeshProUGUI _strainNameText;
        [SerializeField] private TextMeshProUGUI _verificationStatusText;
        [SerializeField] private Image _verificationIcon;
        [SerializeField] private TextMeshProUGUI _blockchainIDText;
        [SerializeField] private TextMeshProUGUI _generationText;
        [SerializeField] private TextMeshProUGUI _breedingDateText;
        [SerializeField] private TextMeshProUGUI _breederNameText;
        [SerializeField] private Button _viewLineageButton;
        [SerializeField] private Button _closeButton;

        [Header("Verification Icons")]
        [SerializeField] private Sprite _verifiedIcon;
        [SerializeField] private Sprite _unverifiedIcon;

        [Header("Colors")]
        [SerializeField] private Color _verifiedColor = Color.green;
        [SerializeField] private Color _unverifiedColor = Color.yellow;

        private IBlockchainGeneticsService _blockchainService;
        private PlantGenotype _currentGenotype;

        private void Awake()
        {
            // Hide panel by default
            if (_panelRoot != null)
                _panelRoot.SetActive(false);

            // Setup button listeners
            if (_viewLineageButton != null)
                _viewLineageButton.onClick.AddListener(OnViewLineageClicked);

            if (_closeButton != null)
                _closeButton.onClick.AddListener(OnCloseClicked);
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
                    "StrainVerificationPanel: BlockchainGeneticsService not found", this);
            }
        }

        /// <summary>
        /// Shows verification panel for a specific strain.
        ///
        /// GAMEPLAY: Called when player clicks strain in UI.
        /// Displays verification status and blockchain info.
        /// </summary>
        public void ShowVerification(PlantGenotype genotype)
        {
            if (genotype == null)
            {
                ChimeraLogger.LogWarning("UI",
                    "Cannot show verification: genotype is null", this);
                return;
            }

            if (_blockchainService == null)
            {
                ChimeraLogger.LogWarning("UI",
                    "Cannot show verification: blockchain service unavailable", this);
                return;
            }

            _currentGenotype = genotype;

            // Get verification info from blockchain service
            var verificationInfo = _blockchainService.GetVerificationInfo(genotype);

            // Display strain name
            if (_strainNameText != null)
            {
                _strainNameText.text = string.IsNullOrEmpty(verificationInfo.StrainName)
                    ? genotype.StrainName
                    : verificationInfo.StrainName;
            }

            // Display verification status
            if (verificationInfo.IsVerified)
            {
                ShowVerifiedStatus(verificationInfo);
            }
            else
            {
                ShowUnverifiedStatus();
            }

            // Display blockchain ID
            if (_blockchainIDText != null)
            {
                if (!string.IsNullOrEmpty(verificationInfo.ShortHash))
                {
                    _blockchainIDText.text = $"Blockchain ID: {verificationInfo.ShortHash}";
                }
                else
                {
                    _blockchainIDText.text = "Blockchain ID: Not recorded";
                }
            }

            // Display generation
            if (_generationText != null)
            {
                _generationText.text = $"Generation: {verificationInfo.GenerationLabel}";
            }

            // Display breeding date (if available)
            if (_breedingDateText != null)
            {
                if (!string.IsNullOrEmpty(verificationInfo.BreedingDate))
                {
                    _breedingDateText.text = $"Bred: {verificationInfo.BreedingDate}";
                }
                else
                {
                    _breedingDateText.text = "Bred: Unknown";
                }
            }

            // Display breeder name (if available)
            if (_breederNameText != null)
            {
                if (!string.IsNullOrEmpty(verificationInfo.BreederName))
                {
                    _breederNameText.text = $"Breeder: {verificationInfo.BreederName}";
                }
                else
                {
                    _breederNameText.text = "";
                }
            }

            // Enable/disable lineage button
            if (_viewLineageButton != null)
            {
                _viewLineageButton.interactable = verificationInfo.HasLineage;
            }

            // Show panel
            if (_panelRoot != null)
                _panelRoot.SetActive(true);

            ChimeraLogger.Log("UI",
                $"Showing verification for: {verificationInfo.StrainName} (Verified: {verificationInfo.IsVerified})", this);
        }

        /// <summary>
        /// Shows verified strain status with green checkmark.
        ///
        /// GAMEPLAY: Player sees ✅ and feels achievement!
        /// "My strain is authentic and verified!"
        /// </summary>
        private void ShowVerifiedStatus(BlockchainVerificationInfo info)
        {
            // Set verification text
            if (_verificationStatusText != null)
            {
                _verificationStatusText.text = info.GetStatusMessage();
                _verificationStatusText.color = _verifiedColor;
            }

            // Set verification icon
            if (_verificationIcon != null && _verifiedIcon != null)
            {
                _verificationIcon.sprite = _verifiedIcon;
                _verificationIcon.color = _verifiedColor;
            }
        }

        /// <summary>
        /// Shows unverified strain status with warning.
        ///
        /// GAMEPLAY: Player sees warning - strain may not be tradeable.
        /// Encourages breeding or purchasing verified strains.
        /// </summary>
        private void ShowUnverifiedStatus()
        {
            // Set verification text
            if (_verificationStatusText != null)
            {
                _verificationStatusText.text = "⚠️ Unverified Strain\nNot blockchain-verified (may not be tradeable)";
                _verificationStatusText.color = _unverifiedColor;
            }

            // Set verification icon
            if (_verificationIcon != null && _unverifiedIcon != null)
            {
                _verificationIcon.sprite = _unverifiedIcon;
                _verificationIcon.color = _unverifiedColor;
            }

            // Disable lineage button
            if (_viewLineageButton != null)
            {
                _viewLineageButton.interactable = false;
            }
        }

        /// <summary>
        /// Opens lineage visualization when player clicks "View Lineage".
        ///
        /// GAMEPLAY: Shows family tree of breeding history.
        /// Parent strains → offspring → current strain.
        /// </summary>
        private void OnViewLineageClicked()
        {
            if (_currentGenotype == null || _blockchainService == null)
                return;

            // Get lineage data
            var lineage = _blockchainService.GetStrainLineage(_currentGenotype);

            if (lineage == null || lineage.Count == 0)
            {
                ChimeraLogger.Log("UI",
                    "No lineage data available for this strain", this);
                return;
            }

            // TODO: Open LineageVisualizationPanel with lineage data
            // For now, log lineage information
            ChimeraLogger.Log("UI",
                $"Strain has {lineage.Count} breeding events in lineage", this);

            // You would create and show LineageVisualizationPanel here
            // LineageVisualizationPanel.Instance.Show(lineage);
        }

        /// <summary>
        /// Closes the verification panel.
        /// </summary>
        private void OnCloseClicked()
        {
            if (_panelRoot != null)
                _panelRoot.SetActive(false);

            _currentGenotype = null;
        }

        /// <summary>
        /// Hides the panel (can be called externally).
        /// </summary>
        public void Hide()
        {
            OnCloseClicked();
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
            if (_viewLineageButton != null)
                _viewLineageButton.onClick.RemoveListener(OnViewLineageClicked);

            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(OnCloseClicked);
        }
    }
}
