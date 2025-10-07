using UnityEngine;
using UnityEngine.UIElements;
using ProjectChimera.Core.Logging;
using System.Collections.Generic;
using System.Linq;
using Logger = ProjectChimera.Core.Logging.ChimeraLogger;

namespace ProjectChimera.Systems.Gameplay
{
    /// <summary>
    /// Genetics Toolbar Manager - Manages genetics tools and menu interface
    /// Provides the genetics mode menu with Seed Bank, Tissue Culture, and Micropropagation tabs
    /// Handles tool selection and genetic operations as described in gameplay document
    /// </summary>
    public class GeneticsToolbarManager : MonoBehaviour
    {
        [Header("UI Documents")]
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private VisualTreeAsset _toolbarTemplate;

        [Header("Toolbar Settings")]
        [SerializeField] private bool _enableSeedBank = true;
        [SerializeField] private bool _enableTissueCulture = true;
        [SerializeField] private bool _enableMicropropagation = true;

        // UI elements
        private VisualElement _toolbarContainer;
        private VisualElement _tabContainer;
        private Dictionary<string, VisualElement> _tabs = new Dictionary<string, VisualElement>();
        private string _currentTab = "seedbank";

        // Genetics data (would integrate with actual genetics system)
        private List<GeneticStrain> _availableStrains = new List<GeneticStrain>();
        private List<TissueCulture> _tissueCultures = new List<TissueCulture>();
        private Dictionary<string, int> _clonesInProgress = new Dictionary<string, int>();

        private void Awake()
        {
            InitializeToolbar();
            InitializeSampleData();
        }

        /// <summary>
        /// Initializes the genetics toolbar
        /// </summary>
        private void InitializeToolbar()
        {
            if (_uiDocument == null)
            {
                Logger.Log("OTHER", "$1", this);
                return;
            }

            var root = _uiDocument.rootVisualElement;

            // Create toolbar container
            _toolbarContainer = new VisualElement();
            _toolbarContainer.name = "genetics-toolbar";
            _toolbarContainer.AddToClassList("genetics-toolbar");

            // Create tab container
            _tabContainer = new VisualElement();
            _tabContainer.name = "genetics-tabs";
            _tabContainer.AddToClassList("genetics-tabs");

            _toolbarContainer.Add(_tabContainer);
            root.Add(_toolbarContainer);

            // Initially hide toolbar
            _toolbarContainer.style.display = DisplayStyle.None;

            CreateTabs();
            Logger.Log("OTHER", "$1", this);
        }

        /// <summary>
        /// Creates the genetics tabs
        /// </summary>
        private void CreateTabs()
        {
            if (_enableSeedBank)
            {
                CreateTab("seedbank", "Seed Bank", "View and manage genetic strains");
            }

            if (_enableTissueCulture)
            {
                CreateTab("tissueculture", "Tissue Culture", "Preserve genetics long-term");
            }

            if (_enableMicropropagation)
            {
                CreateTab("micropropagation", "Micropropagation", "Rapid genetic multiplication");
            }
        }

        /// <summary>
        /// Creates a tab element
        /// </summary>
        private void CreateTab(string tabId, string tabName, string description)
        {
            var tab = new Button();
            tab.text = tabName;
            tab.tooltip = description;
            tab.AddToClassList("genetics-tab");
            tab.clicked += () => SelectTab(tabId);

            _tabs[tabId] = tab;
            _tabContainer.Add(tab);
        }

        /// <summary>
        /// Initializes sample genetics data
        /// </summary>
        private void InitializeSampleData()
        {
            // Sample strains (would come from actual genetics system)
            _availableStrains.Add(new GeneticStrain
            {
                StrainID = "strain_001",
                StrainName = "Northern Lights",
                THCContent = 18f,
                CBDContent = 0.8f,
                Yield = 450f,
                FloweringTime = 9,
                Available = true
            });

            _availableStrains.Add(new GeneticStrain
            {
                StrainID = "strain_002",
                StrainName = "Blue Dream",
                THCContent = 17f,
                CBDContent = 1.2f,
                Yield = 520f,
                FloweringTime = 9,
                Available = true
            });

            // Sample tissue cultures
            _tissueCultures.Add(new TissueCulture
            {
                CultureID = "culture_001",
                StrainID = "strain_001",
                CreationDate = System.DateTime.Now.AddDays(-30),
                Health = 0.95f
            });
        }

        /// <summary>
        /// Shows the genetics toolbar
        /// </summary>
        public void ShowToolbar()
        {
            if (_toolbarContainer != null)
            {
                _toolbarContainer.style.display = DisplayStyle.Flex;
                UpdateToolbarContent();
            }
        }

        /// <summary>
        /// Hides the genetics toolbar
        /// </summary>
        public void HideToolbar()
        {
            if (_toolbarContainer != null)
            {
                _toolbarContainer.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// Selects a tab and shows its content
        /// </summary>
        private void SelectTab(string tabId)
        {
            if (!_tabs.ContainsKey(tabId)) return;

            _currentTab = tabId;

            // Update tab selection visual
            foreach (var tab in _tabs)
            {
                if (tab.Key == tabId)
                {
                    tab.Value.AddToClassList("selected");
                }
                else
                {
                    tab.Value.RemoveFromClassList("selected");
                }
            }

            UpdateToolbarContent();
            Logger.Log("OTHER", "$1", this);
        }

        /// <summary>
        /// Updates the toolbar content based on selected tab
        /// </summary>
        private void UpdateToolbarContent()
        {
            // Clear existing content
            var contentContainer = _toolbarContainer.Q("tab-content");
            if (contentContainer == null)
            {
                contentContainer = new VisualElement();
                contentContainer.name = "tab-content";
                contentContainer.AddToClassList("tab-content");
                _toolbarContainer.Add(contentContainer);
            }
            contentContainer.Clear();

            // Add content based on current tab
            switch (_currentTab)
            {
                case "seedbank":
                    CreateSeedBankContent(contentContainer);
                    break;
                case "tissueculture":
                    CreateTissueCultureContent(contentContainer);
                    break;
                case "micropropagation":
                    CreateMicropropagationContent(contentContainer);
                    break;
            }
        }

        /// <summary>
        /// Creates seed bank tab content
        /// </summary>
        private void CreateSeedBankContent(VisualElement container)
        {
            var title = new Label("Seed Bank");
            title.AddToClassList("tab-title");
            container.Add(title);

            foreach (var strain in _availableStrains)
            {
                var strainItem = CreateStrainItem(strain);
                container.Add(strainItem);
            }

            var plantButton = new Button();
            plantButton.text = "Plant Selected Strain";
            plantButton.clicked += OnPlantStrain;
            container.Add(plantButton);
        }

        /// <summary>
        /// Creates tissue culture tab content
        /// </summary>
        private void CreateTissueCultureContent(VisualElement container)
        {
            var title = new Label("Tissue Culture");
            title.AddToClassList("tab-title");
            container.Add(title);

            foreach (var culture in _tissueCultures)
            {
                var cultureItem = CreateCultureItem(culture);
                container.Add(cultureItem);
            }

            var createButton = new Button();
            createButton.text = "Create Tissue Culture";
            createButton.clicked += OnCreateTissueCulture;
            container.Add(createButton);
        }

        /// <summary>
        /// Creates micropropagation tab content
        /// </summary>
        private void CreateMicropropagationContent(VisualElement container)
        {
            var title = new Label("Micropropagation");
            title.AddToClassList("tab-title");
            container.Add(title);

            var description = new Label("Rapidly multiply your best genetics");
            description.AddToClassList("tab-description");
            container.Add(description);

            var startButton = new Button();
            startButton.text = "Start Micropropagation";
            startButton.clicked += OnStartMicropropagation;
            container.Add(startButton);

            var harvestButton = new Button();
            harvestButton.text = "Harvest Clones";
            harvestButton.clicked += OnHarvestClones;
            container.Add(harvestButton);
        }

        /// <summary>
        /// Creates a strain item for the seed bank
        /// </summary>
        private VisualElement CreateStrainItem(GeneticStrain strain)
        {
            var item = new VisualElement();
            item.AddToClassList("strain-item");

            var nameLabel = new Label(strain.StrainName);
            nameLabel.AddToClassList("strain-name");

            var thcLabel = new Label($"THC: {strain.THCContent:F1}%");
            var yieldLabel = new Label($"Yield: {strain.Yield:F0}g");

            item.Add(nameLabel);
            item.Add(thcLabel);
            item.Add(yieldLabel);

            if (!strain.Available)
            {
                item.AddToClassList("unavailable");
            }

            return item;
        }

        /// <summary>
        /// Creates a tissue culture item
        /// </summary>
        private VisualElement CreateCultureItem(TissueCulture culture)
        {
            var item = new VisualElement();
            item.AddToClassList("culture-item");

            var strainName = _availableStrains.Find(s => s.StrainID == culture.StrainID)?.StrainName ?? "Unknown";
            var nameLabel = new Label($"Culture: {strainName}");
            var healthLabel = new Label($"Health: {(culture.Health * 100):F1}%");

            item.Add(nameLabel);
            item.Add(healthLabel);

            return item;
        }

        // Event handlers (would integrate with actual genetics systems)

        private void OnPlantStrain()
        {
            Logger.Log("OTHER", "$1", this);
            // Would integrate with planting system
        }

        private void OnCreateTissueCulture()
        {
            Logger.Log("OTHER", "$1", this);
            // Would integrate with tissue culture system
        }

        private void OnStartMicropropagation()
        {
            Logger.Log("OTHER", "$1", this);
            // Would integrate with micropropagation system
        }

        private void OnHarvestClones()
        {
            Logger.Log("OTHER", "$1", this);
            // Would integrate with harvesting system
        }

        /// <summary>
        /// Gets the current selected tab
        /// </summary>
        public string GetCurrentTab()
        {
            return _currentTab;
        }

        /// <summary>
        /// Checks if the toolbar is visible
        /// </summary>
        public bool IsToolbarVisible()
        {
            return _toolbarContainer != null &&
                   _toolbarContainer.style.display == DisplayStyle.Flex;
        }

        /// <summary>
        /// Gets available strains count
        /// </summary>
        public int GetAvailableStrainsCount()
        {
            return _availableStrains.Count(s => s.Available);
        }

        /// <summary>
        /// Gets tissue cultures count
        /// </summary>
        public int GetTissueCulturesCount()
        {
            return _tissueCultures.Count;
        }
    }

    /// <summary>
    /// Genetic strain data structure
    /// </summary>
    [System.Serializable]
    public class GeneticStrain
    {
        public string StrainID;
        public string StrainName;
        public float THCContent;
        public float CBDContent;
        public float Yield;
        public int FloweringTime;
        public bool Available;
    }

    /// <summary>
    /// Tissue culture data structure
    /// </summary>
    [System.Serializable]
    public class TissueCulture
    {
        public string CultureID;
        public string StrainID;
        public System.DateTime CreationDate;
        public float Health;
    }
}

