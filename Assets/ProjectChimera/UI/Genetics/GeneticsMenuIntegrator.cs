using UnityEngine;
using ProjectChimera.UI.Genetics;
using ProjectChimera.UI.Simple;
using ProjectChimera.Systems.Genetics;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Interfaces;

namespace ProjectChimera.UI.Genetics
{
    /// <summary>
    /// Integrates genetics UI panels with the contextual menu system.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST:
    /// =====================================
    /// Makes genetics features ACCESSIBLE and INTUITIVE:
    ///
    /// 1. **Contextual Menu Actions** - Right place, right time
    ///    - "Create Tissue Culture" → Opens TissueCulturePanel
    ///    - "Start Micropropagation" → Opens MicropropagationPanel
    ///    - "Cross Plants" → Opens breeding UI with blockchain genetics
    ///    - "View Strain Info" → Opens StrainVerificationPanel
    ///
    /// 2. **Seamless Flow** - No menu hunting
    ///    - Player in Genetics mode → sees genetics actions
    ///    - Clicks action → panel appears with pre-filled data
    ///    - Completes action → panel closes, results visible
    ///
    /// 3. **Smart Defaults** - Minimal clicks
    ///    - Selected plant auto-fills tissue culture panel
    ///    - Selected culture auto-fills micropropagation panel
    ///    - Parent plants auto-fill breeding panel
    ///
    /// INVISIBLE INTEGRATION:
    /// Players just see "genetics menu works perfectly"
    /// They don't know there's a sophisticated integration layer underneath.
    /// </summary>
    public class GeneticsMenuIntegrator : MonoBehaviour
    {
        [Header("UI Panel References")]
        [SerializeField] private TissueCulturePanel _tissueCulturePanel;
        [SerializeField] private MicropropagationPanel _micropropagationPanel;
        [SerializeField] private BreedingPanel _breedingPanel;
        [SerializeField] private StrainVerificationPanel _strainVerificationPanel;
        [SerializeField] private LineageVisualizationPanel _lineageVisualizationPanel;

        [Header("Menu Reference")]
        [SerializeField] private SimpleContextualMenu _contextualMenu;

        [Header("Debug")]
        [SerializeField] private bool _enableDebugLogging = false;

        // Services
        private TissueCultureManager _tissueCultureManager;
        private IBlockchainGeneticsService _blockchainService;

        // Current selection state (set by other systems)
        private string _selectedPlantId;
        private PlantGenotype _selectedPlantGenotype;
        private float _selectedPlantHealth = 1.0f;
        private string _selectedCultureId;

        // Breeding requires two parents
        private PlantGenotype _breedingParent1;
        private PlantGenotype _breedingParent2;

        private void Start()
        {
            InitializeIntegrator();
            SubscribeToMenuEvents();
        }

        private void InitializeIntegrator()
        {
            // Get services from container
            var container = ServiceContainerFactory.Instance;
            if (container != null)
            {
                _tissueCultureManager = container.Resolve<TissueCultureManager>();
                _blockchainService = container.Resolve<IBlockchainGeneticsService>();
            }

            if (_tissueCultureManager == null)
            {
                ChimeraLogger.LogWarning("UI",
                    "GeneticsMenuIntegrator: TissueCultureManager not found", this);
            }

            if (_blockchainService == null)
            {
                ChimeraLogger.LogWarning("UI",
                    "GeneticsMenuIntegrator: BlockchainGeneticsService not found", this);
            }

            if (_enableDebugLogging)
            {
                ChimeraLogger.Log("UI",
                    "GeneticsMenuIntegrator initialized", this);
            }
        }

        private void SubscribeToMenuEvents()
        {
            if (_contextualMenu != null)
            {
                _contextualMenu.OnActionSelected += OnMenuActionSelected;
            }
            else
            {
                ChimeraLogger.LogWarning("UI",
                    "GeneticsMenuIntegrator: SimpleContextualMenu not assigned", this);
            }
        }

        /// <summary>
        /// Called when player selects action from contextual menu.
        /// Routes genetics actions to appropriate UI panels.
        ///
        /// GAMEPLAY: Makes menu actions actually do something!
        /// </summary>
        private void OnMenuActionSelected(string actionName)
        {
            switch (actionName)
            {
                case "Create Tissue Culture":
                    OnCreateTissueCultureAction();
                    break;

                case "Start Micropropagation":
                    OnMicropropagationAction();
                    break;

                case "Cross Plants":
                    OnCrossplantsAction();
                    break;

                case "View Strain Info":
                    OnViewStrainInfoAction();
                    break;

                case "View Lineage":
                    OnViewLineageAction();
                    break;

                default:
                    // Not a genetics action - ignore
                    break;
            }
        }

        #region Action Handlers

        /// <summary>
        /// Opens tissue culture panel for selected plant.
        ///
        /// GAMEPLAY:
        /// - Player selects plant in scene
        /// - Opens genetics menu → clicks "Create Tissue Culture"
        /// - Panel appears with plant info pre-filled
        /// - Player enters culture name → clicks Create
        /// - Culture created and added to inventory
        /// </summary>
        private void OnCreateTissueCultureAction()
        {
            if (_tissueCulturePanel == null)
            {
                ChimeraLogger.LogWarning("UI",
                    "Cannot create tissue culture: TissueCulturePanel not assigned", this);
                return;
            }

            // Validate selection
            if (string.IsNullOrEmpty(_selectedPlantId) || _selectedPlantGenotype == null)
            {
                ChimeraLogger.LogWarning("UI",
                    "Cannot create tissue culture: No plant selected", this);
                ShowErrorMessage("Please select a plant first");
                return;
            }

            // Open panel with selected plant data
            _tissueCulturePanel.ShowPanel(
                _selectedPlantId,
                _selectedPlantGenotype,
                _selectedPlantHealth
            );

            if (_enableDebugLogging)
            {
                ChimeraLogger.Log("UI",
                    $"Opening tissue culture panel for plant {_selectedPlantId}", this);
            }
        }

        /// <summary>
        /// Opens micropropagation panel for selected culture.
        ///
        /// GAMEPLAY:
        /// - Player views tissue culture list
        /// - Selects culture → clicks "Start Micropropagation"
        /// - Panel appears with culture viability and clone slider
        /// - Player sets quantity → clicks Micropropagate
        /// - Clone seeds created and added to inventory
        /// </summary>
        private void OnMicropropagationAction()
        {
            if (_micropropagationPanel == null)
            {
                ChimeraLogger.LogWarning("UI",
                    "Cannot micropropagate: MicropropagationPanel not assigned", this);
                return;
            }

            // Validate selection
            if (string.IsNullOrEmpty(_selectedCultureId))
            {
                ChimeraLogger.LogWarning("UI",
                    "Cannot micropropagate: No culture selected", this);
                ShowErrorMessage("Please select a tissue culture first");
                return;
            }

            // Open panel with selected culture
            _micropropagationPanel.ShowPanel(_selectedCultureId);

            if (_enableDebugLogging)
            {
                ChimeraLogger.Log("UI",
                    $"Opening micropropagation panel for culture {_selectedCultureId}", this);
            }
        }

        /// <summary>
        /// Opens breeding UI for crossing selected plants.
        ///
        /// GAMEPLAY:
        /// - Player selects two plants
        /// - Clicks "Cross Plants" → Breeding UI opens
        /// - Shows parent traits, expected offspring
        /// - Click Breed → Blockchain genetics + GPU mining
        /// - Offspring seed created with verified lineage
        /// </summary>
        private void OnCrossplantsAction()
        {
            if (_breedingPanel == null)
            {
                ChimeraLogger.LogWarning("UI",
                    "Cannot breed plants: BreedingPanel not assigned", this);
                return;
            }

            // Validate selection - need two parents
            if (_breedingParent1 == null || _breedingParent2 == null)
            {
                ChimeraLogger.LogWarning("UI",
                    "Cannot breed plants: Two parent plants required", this);
                ShowErrorMessage("Please select two parent plants for breeding");
                return;
            }

            // Open breeding panel with parent plants
            _breedingPanel.ShowPanel(_breedingParent1, _breedingParent2);

            if (_enableDebugLogging)
            {
                ChimeraLogger.Log("UI",
                    $"Opening breeding panel: {_breedingParent1.StrainName} × {_breedingParent2.StrainName}", this);
            }
        }

        /// <summary>
        /// Opens strain verification panel for selected plant.
        ///
        /// GAMEPLAY:
        /// - Player selects plant → clicks "View Strain Info"
        /// - Panel shows blockchain verification status
        /// - ✅ Verified badge if blockchain-verified
        /// - Shows generation (F1, F2, etc.)
        /// - "View Lineage" button to see family tree
        /// </summary>
        private void OnViewStrainInfoAction()
        {
            if (_strainVerificationPanel == null)
            {
                ChimeraLogger.LogWarning("UI",
                    "Cannot view strain info: StrainVerificationPanel not assigned", this);
                return;
            }

            // Validate selection
            if (_selectedPlantGenotype == null)
            {
                ChimeraLogger.LogWarning("UI",
                    "Cannot view strain info: No plant selected", this);
                ShowErrorMessage("Please select a plant first");
                return;
            }

            // Open verification panel
            _strainVerificationPanel.ShowVerification(_selectedPlantGenotype);

            if (_enableDebugLogging)
            {
                ChimeraLogger.Log("UI",
                    $"Opening strain verification panel for {_selectedPlantGenotype.StrainName}", this);
            }
        }

        /// <summary>
        /// Opens lineage visualization panel for selected plant.
        ///
        /// GAMEPLAY:
        /// - Player clicks "View Lineage"
        /// - Panel shows family tree visualization
        /// - Genesis strains (purchased) in blue
        /// - Bred strains in green
        /// - Current strain highlighted in gold
        /// - Lines show parent → offspring relationships
        /// </summary>
        private void OnViewLineageAction()
        {
            if (_lineageVisualizationPanel == null)
            {
                ChimeraLogger.LogWarning("UI",
                    "Cannot view lineage: LineageVisualizationPanel not assigned", this);
                return;
            }

            if (_blockchainService == null)
            {
                ChimeraLogger.LogWarning("UI",
                    "Cannot view lineage: BlockchainGeneticsService unavailable", this);
                return;
            }

            // Validate selection
            if (_selectedPlantGenotype == null)
            {
                ChimeraLogger.LogWarning("UI",
                    "Cannot view lineage: No plant selected", this);
                ShowErrorMessage("Please select a plant first");
                return;
            }

            // Get lineage from blockchain
            var lineage = _blockchainService.GetStrainLineage(_selectedPlantGenotype);

            if (lineage == null || lineage.Count == 0)
            {
                ChimeraLogger.Log("UI",
                    "No lineage data available for this strain", this);
                ShowErrorMessage("This strain has no recorded lineage");
                return;
            }

            // Open lineage panel
            _lineageVisualizationPanel.ShowLineage(lineage, _selectedPlantGenotype.StrainName);

            if (_enableDebugLogging)
            {
                ChimeraLogger.Log("UI",
                    $"Opening lineage panel for {_selectedPlantGenotype.StrainName} ({lineage.Count} events)", this);
            }
        }

        #endregion

        #region Public API - Selection Management

        /// <summary>
        /// Sets the currently selected plant for genetics actions.
        /// Called by plant selection system when player selects a plant.
        ///
        /// GAMEPLAY: Keeps menu actions in sync with player's selection.
        /// </summary>
        public void SetSelectedPlant(string plantId, PlantGenotype genotype, float health = 1.0f)
        {
            _selectedPlantId = plantId;
            _selectedPlantGenotype = genotype;
            _selectedPlantHealth = health;

            if (_enableDebugLogging)
            {
                ChimeraLogger.Log("UI",
                    $"Selected plant: {plantId} ({genotype?.StrainName ?? "unknown"})", this);
            }
        }

        /// <summary>
        /// Clears plant selection.
        /// Called when player deselects plant.
        /// </summary>
        public void ClearPlantSelection()
        {
            _selectedPlantId = null;
            _selectedPlantGenotype = null;
            _selectedPlantHealth = 1.0f;
        }

        /// <summary>
        /// Sets the currently selected tissue culture for micropropagation.
        /// Called by culture list UI when player selects a culture.
        /// </summary>
        public void SetSelectedCulture(string cultureId)
        {
            _selectedCultureId = cultureId;

            if (_enableDebugLogging)
            {
                ChimeraLogger.Log("UI",
                    $"Selected culture: {cultureId}", this);
            }
        }

        /// <summary>
        /// Clears culture selection.
        /// </summary>
        public void ClearCultureSelection()
        {
            _selectedCultureId = null;
        }

        /// <summary>
        /// Sets breeding parents for cross-pollination.
        /// Called when player selects two plants for breeding.
        ///
        /// GAMEPLAY: Player selects first plant, then second plant → menu shows "Cross Plants"
        /// </summary>
        public void SetBreedingParents(PlantGenotype parent1, PlantGenotype parent2)
        {
            _breedingParent1 = parent1;
            _breedingParent2 = parent2;

            if (_enableDebugLogging)
            {
                ChimeraLogger.Log("UI",
                    $"Breeding parents set: {parent1?.StrainName ?? "null"} × {parent2?.StrainName ?? "null"}", this);
            }
        }

        /// <summary>
        /// Clears breeding parent selection.
        /// </summary>
        public void ClearBreedingParents()
        {
            _breedingParent1 = null;
            _breedingParent2 = null;
        }

        #endregion

        #region Utility

        /// <summary>
        /// Shows error message to player (placeholder - future UI toast/modal).
        /// </summary>
        private void ShowErrorMessage(string message)
        {
            ChimeraLogger.LogWarning("UI", $"Genetics Menu Error: {message}", this);
            // TODO: Show UI toast or modal dialog
        }

        #endregion

        private void OnDestroy()
        {
            // Unsubscribe from menu events
            if (_contextualMenu != null)
            {
                _contextualMenu.OnActionSelected -= OnMenuActionSelected;
            }
        }
    }
}
