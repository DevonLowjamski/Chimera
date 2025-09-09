using UnityEngine;
using UnityEngine.UIElements;
using ProjectChimera.Core;
using ProjectChimera.Core.Updates;
using ProjectChimera.Data.Save;
using ProjectChimera.UI.Core;
using ProjectChimera.Data.UI;
using System.Collections.Generic;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Core infrastructure for the Save/Load Panel.
    /// Manages panel initialization, tab system, and core state management.
    /// </summary>
    public class SaveLoadPanelCore : UIPanel, ITickable
    {
        [Header("Save/Load Configuration")]
        [SerializeField] protected bool _enableSavePreview = true;
        [SerializeField] protected bool _enableAutoRefresh = true;
        [SerializeField] protected float _refreshInterval = 5f;
        [SerializeField] protected int _maxVisibleSlots = 10;
        
        [Header("Visual Settings")]
        [SerializeField] protected bool _enableSaveAnimations = true;
        [SerializeField] protected bool _showDetailedInfo = true;
        [SerializeField] protected bool _enableDragAndDrop = false;
        [SerializeField] protected Color _saveSlotColor = new Color(0.2f, 0.6f, 0.8f, 1f);
        
        // Core UI Elements
        protected VisualElement _rootContainer;
        protected VisualElement _tabContainer;
        protected Button _saveTab;
        protected Button _loadTab;
        
        // Tab Content Containers
        protected VisualElement _saveContent;
        protected VisualElement _loadContent;
        
        // State Management
        protected bool _isSaveTabActive = true;
        protected float _lastRefreshTime = 0f;
        protected bool _isCurrentlyLoading = false;
        protected bool _isCurrentlySaving = false;
        
        // Component References
        protected SaveTabUIBuilder _saveTabBuilder;
        protected LoadTabUIBuilder _loadTabBuilder;
        protected SaveLoadOperationHandler _operationHandler;
        protected SaveSlotUIRenderer _slotRenderer;
        
        // Public Properties
        public bool EnableSavePreview => _enableSavePreview;
        public bool EnableAutoRefresh => _enableAutoRefresh;
        public float RefreshInterval => _refreshInterval;
        public int MaxVisibleSlots => _maxVisibleSlots;
        public bool EnableSaveAnimations => _enableSaveAnimations;
        public bool ShowDetailedInfo => _showDetailedInfo;
        public Color SaveSlotColor => _saveSlotColor;
        public bool IsSaveTabActive => _isSaveTabActive;
        public bool IsCurrentlyLoading => _isCurrentlyLoading;
        public bool IsCurrentlySaving => _isCurrentlySaving;
        
        protected override void OnPanelInitialized()
        {
            base.OnPanelInitialized();
            
            InitializeComponents();
            CreateCoreUI();
            
            // Register with UpdateOrchestrator
            UpdateOrchestrator.Instance.RegisterTickable(this);
            
            LogInfo("SaveLoadPanelCore initialized");
        }
        
        #region ITickable Implementation
        
        public int Priority => TickPriority.UIManager;
        public bool Enabled => IsInitialized && _enableAutoRefresh;
        
        public virtual void Tick(float deltaTime)
        {
            // Auto-refresh save slots periodically
            if (_enableAutoRefresh && Time.time - _lastRefreshTime >= _refreshInterval)
            {
                RefreshSaveSlots();
                _lastRefreshTime = Time.time;
            }
            
            // Update UI state based on save/load operations
            UpdateUIState();
        }
        
        #endregion
        
        protected virtual void InitializeComponents()
        {
            _saveTabBuilder = new SaveTabUIBuilder(this);
            _loadTabBuilder = new LoadTabUIBuilder(this);
            _operationHandler = new SaveLoadOperationHandler(this);
            _slotRenderer = new SaveSlotUIRenderer(this);
        }
        
        protected virtual void CreateCoreUI()
        {
            CreateRootContainer();
            CreateHeader();
            CreateTabSystem();
            CreateTabContainers();
            
            _contentContainer.Add(_rootContainer);
            
            // Initialize tab builders
            _saveTabBuilder.CreateSaveTab();
            _loadTabBuilder.CreateLoadTab();
            
            // Show save tab by default
            ShowSaveTab();
        }
        
        protected virtual void CreateRootContainer()
        {
            _rootContainer = new VisualElement();
            _rootContainer.name = "save-load-panel";
            _rootContainer.AddToClassList("save-load-panel");
            _rootContainer.style.width = new Length(100, LengthUnit.Percent);
            _rootContainer.style.height = new Length(100, LengthUnit.Percent);
            _rootContainer.style.paddingTop = new StyleLength(20f);
        }
        
        protected virtual void CreateHeader()
        {
            var headerContainer = new VisualElement();
            headerContainer.name = "save-load-header";
            headerContainer.style.marginBottom = 20f;
            headerContainer.style.alignItems = Align.Center;
            
            var titleLabel = new Label("Save & Load Game");
            titleLabel.AddToClassList("panel-title");
            titleLabel.style.fontSize = 24f;
            titleLabel.style.color = new Color(0.9f, 0.7f, 0.2f, 1f);
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            
            headerContainer.Add(titleLabel);
            _rootContainer.Add(headerContainer);
        }
        
        protected virtual void CreateTabSystem()
        {
            _tabContainer = new VisualElement();
            _tabContainer.name = "tab-container";
            _tabContainer.style.flexDirection = FlexDirection.Row;
            _tabContainer.style.marginBottom = 15f;
            _tabContainer.style.justifyContent = Justify.Center;
            
            // Save Tab
            _saveTab = new Button();
            _saveTab.text = "💾 Save Game";
            _saveTab.name = "save-tab";
            _saveTab.AddToClassList("tab-button");
            _saveTab.style.paddingTop = new StyleLength(12f);
            _saveTab.style.marginRight = 5f;
            _saveTab.style.backgroundColor = _saveSlotColor;
            _saveTab.style.color = Color.white;
            _saveTab.style.borderTopLeftRadius = 8f;
            _saveTab.style.borderTopRightRadius = 8f;
            _saveTab.style.borderBottomWidth = 0f;
            _saveTab.style.minWidth = 150f;
            _saveTab.clicked += ShowSaveTab;
            
            // Load Tab
            _loadTab = new Button();
            _loadTab.text = "📁 Load Game";
            _loadTab.name = "load-tab";
            _loadTab.AddToClassList("tab-button");
            _loadTab.style.paddingTop = new StyleLength(12f);
            _loadTab.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            _loadTab.style.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            _loadTab.style.borderTopLeftRadius = 8f;
            _loadTab.style.borderTopRightRadius = 8f;
            _loadTab.style.borderBottomWidth = 0f;
            _loadTab.style.minWidth = 150f;
            _loadTab.clicked += ShowLoadTab;
            
            _tabContainer.Add(_saveTab);
            _tabContainer.Add(_loadTab);
            _rootContainer.Add(_tabContainer);
        }
        
        protected virtual void CreateTabContainers()
        {
            // Save Content Container
            _saveContent = new VisualElement();
            _saveContent.name = "save-content";
            _saveContent.style.flexGrow = 1f;
            _saveContent.style.paddingTop = new StyleLength(20f);
            _saveContent.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            _saveContent.style.borderTopLeftRadius = 8f;
            _saveContent.style.borderTopRightRadius = 8f;
            _saveContent.style.borderBottomLeftRadius = 8f;
            _saveContent.style.borderBottomRightRadius = 8f;
            
            // Load Content Container
            _loadContent = new VisualElement();
            _loadContent.name = "load-content";
            _loadContent.style.flexGrow = 1f;
            _loadContent.style.display = DisplayStyle.None;
            _loadContent.style.flexDirection = FlexDirection.Row;
            
            _rootContainer.Add(_saveContent);
            _rootContainer.Add(_loadContent);
        }
        
        public virtual void ShowSaveTab()
        {
            _isSaveTabActive = true;
            
            // Update tab appearances
            _saveTab.style.backgroundColor = _saveSlotColor;
            _saveTab.style.color = Color.white;
            _loadTab.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            _loadTab.style.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            
            // Show/hide content
            _saveContent.style.display = DisplayStyle.Flex;
            _loadContent.style.display = DisplayStyle.None;
        }
        
        public virtual void ShowLoadTab()
        {
            _isSaveTabActive = false;
            
            // Update tab appearances
            _loadTab.style.backgroundColor = _saveSlotColor;
            _loadTab.style.color = Color.white;
            _saveTab.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            _saveTab.style.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            
            // Show/hide content
            _loadContent.style.display = DisplayStyle.Flex;
            _saveContent.style.display = DisplayStyle.None;
            
            RefreshSaveSlots();
        }
        
        public virtual void RefreshSaveSlots()
        {
            if (_loadTabBuilder != null)
            {
                _loadTabBuilder.RefreshSaveSlots();
            }
        }
        
        public virtual void UpdateUIState()
        {
            if (_operationHandler != null)
            {
                _operationHandler.UpdateUIState();
            }
        }
        
        public virtual void SetOperationState(bool isSaving, bool isLoading)
        {
            _isCurrentlySaving = isSaving;
            _isCurrentlyLoading = isLoading;
        }
        
        // Accessors for components
        public VisualElement GetSaveContent() => _saveContent;
        public VisualElement GetLoadContent() => _loadContent;
        public VisualElement GetRootContainer() => _rootContainer;
        
        protected virtual void OnDestroy()
        {
            if (UpdateOrchestrator.Instance != null)
            {
                UpdateOrchestrator.Instance.UnregisterTickable(this);
            }
        }
    }
}