using UnityEngine;
using UnityEngine.UIElements;
using ProjectChimera.Core;
using ProjectChimera.Core.Updates;
using ProjectChimera.Data.Save;
using SaveResult = ProjectChimera.Data.Save.SaveResult;
using LoadResult = ProjectChimera.Data.Save.LoadResult;
// using ProjectChimera.Systems.Save;
using System.Collections.Generic;
using System.Linq;
using System;
using ProjectChimera.UI.Core;
using ProjectChimera.Data.UI;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// UI Panel for save and load operations in Project Chimera.
    /// Provides an intuitive interface for managing save slots, creating new saves,
    /// loading existing games, and viewing save file details with visual feedback.
    /// </summary>
    public class SaveLoadPanel : UIPanel, ITickable
    {
        [Header("Save/Load Configuration")]
        [SerializeField] private bool _enableSavePreview = true;
        [SerializeField] private bool _enableAutoRefresh = true;
        [SerializeField] private float _refreshInterval = 5f;
        [SerializeField] private int _maxVisibleSlots = 10;
        
        [Header("Visual Settings")]
        [SerializeField] private bool _enableSaveAnimations = true;
        [SerializeField] private bool _showDetailedInfo = true;
        [SerializeField] private bool _enableDragAndDrop = false;
        [SerializeField] private Color _saveSlotColor = new Color(0.2f, 0.6f, 0.8f, 1f);
        
        // System References
        // private SaveManager _saveManager;
        
        // UI Elements
        private VisualElement _rootContainer;
        private VisualElement _tabContainer;
        private Button _saveTab;
        private Button _loadTab;
        
        // Save Tab Elements
        private VisualElement _saveContent;
        private TextField _saveNameField;
        private TextField _saveDescriptionField;
        private Button _saveButton;
        private Button _quickSaveButton;
        private Label _saveStatusLabel;
        
        // Load Tab Elements
        private VisualElement _loadContent;
        private ScrollView _saveSlotScrollView;
        private VisualElement _saveSlotContainer;
        private VisualElement _slotDetailsPanel;
        private Button _loadButton;
        private Button _deleteButton;
        private Button _quickLoadButton;
        private Label _loadStatusLabel;
        
        // Save Slot Management
        private List<VisualElement> _saveSlotElements = new List<VisualElement>();
        private Dictionary<string, ProjectChimera.Data.Save.SaveSlotData> _slotDataMap = new Dictionary<string, ProjectChimera.Data.Save.SaveSlotData>();
        private string _selectedSlotName = "";
        private bool _isCurrentlyLoading = false;
        private bool _isCurrentlySaving = false;
        private float _lastRefreshTime = 0f;
        
        // State Management
        private bool _isSaveTabActive = true;
        
        protected override void OnPanelInitialized()
        {
            base.OnPanelInitialized();
            
            // Find system references (intentionally omitted in early refactor)
            CreateUI();
            SubscribeToEvents();
            RefreshSaveSlots();
            
            // Register with UpdateOrchestrator
            UpdateOrchestrator.Instance.RegisterTickable(this);
            
            LogInfo("SaveLoadPanel initialized");
            return;
        }
        
        #region ITickable Implementation
        
        public int Priority => TickPriority.UIManager;
        public bool Enabled => IsInitialized && _enableAutoRefresh;
        
        public void Tick(float deltaTime)
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
        
        private void CreateUI()
        {
            _rootContainer = new VisualElement();
            _rootContainer.name = "save-load-panel";
            _rootContainer.AddToClassList("save-load-panel");
            _rootContainer.style.width = new Length(100, LengthUnit.Percent);
            _rootContainer.style.height = new Length(100, LengthUnit.Percent);
            _rootContainer.style.paddingTop = new StyleLength(20f); // Fixed padding;
            
            CreateHeader();
            CreateTabSystem();
            CreateSaveTab();
            CreateLoadTab();
            
            _contentContainer.Add(_rootContainer);
            
            // Show save tab by default
            ShowSaveTab();
        }
        
        private void CreateHeader()
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
        
        private void CreateTabSystem()
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
            _saveTab.style.paddingTop = new StyleLength(12f); // Fixed padding;
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
            _loadTab.style.paddingTop = new StyleLength(12f); // Fixed padding;
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
        
        private void CreateSaveTab()
        {
            _saveContent = new VisualElement();
            _saveContent.name = "save-content";
            _saveContent.style.flexGrow = 1f;
            _saveContent.style.paddingTop = new StyleLength(20f); // Fixed padding;
            _saveContent.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            _saveContent.style.borderTopLeftRadius = 8f;
            _saveContent.style.borderTopRightRadius = 8f;
            _saveContent.style.borderBottomLeftRadius = 8f;
            _saveContent.style.borderBottomRightRadius = 8f;
            
            // Save form container
            var saveFormContainer = new VisualElement();
            saveFormContainer.style.maxWidth = 500f;
            saveFormContainer.style.alignSelf = Align.Center;
            
            // Save name input
            var saveNameContainer = new VisualElement();
            saveNameContainer.style.marginBottom = 15f;
            
            var saveNameLabel = new Label("Save Name:");
            saveNameLabel.style.fontSize = 14f;
            saveNameLabel.style.color = Color.white;
            saveNameLabel.style.marginBottom = 5f;
            
            _saveNameField = new TextField();
            _saveNameField.name = "save-name-field";
            _saveNameField.value = $"Save_{DateTime.Now:yyyyMMdd_HHmm}";
            _saveNameField.style.fontSize = 14f;
            _saveNameField.style.paddingTop = new StyleLength(8f); // Fixed padding;
            _saveNameField.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            _saveNameField.style.color = Color.white;
            _saveNameField.style.borderTopLeftRadius = 4f;
            _saveNameField.style.borderTopRightRadius = 4f;
            _saveNameField.style.borderBottomLeftRadius = 4f;
            _saveNameField.style.borderBottomRightRadius = 4f;
            
            saveNameContainer.Add(saveNameLabel);
            saveNameContainer.Add(_saveNameField);
            
            // Save description input
            var saveDescContainer = new VisualElement();
            saveDescContainer.style.marginBottom = 20f;
            
            var saveDescLabel = new Label("Description (Optional):");
            saveDescLabel.style.fontSize = 14f;
            saveDescLabel.style.color = Color.white;
            saveDescLabel.style.marginBottom = 5f;
            
            _saveDescriptionField = new TextField();
            _saveDescriptionField.name = "save-description-field";
            _saveDescriptionField.multiline = true;
            _saveDescriptionField.style.fontSize = 14f;
            _saveDescriptionField.style.paddingTop = new StyleLength(8f); // Fixed padding;
            _saveDescriptionField.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            _saveDescriptionField.style.color = Color.white;
            _saveDescriptionField.style.borderTopLeftRadius = 4f;
            _saveDescriptionField.style.borderTopRightRadius = 4f;
            _saveDescriptionField.style.borderBottomLeftRadius = 4f;
            _saveDescriptionField.style.borderBottomRightRadius = 4f;
            _saveDescriptionField.style.height = 60f;
            
            saveDescContainer.Add(saveDescLabel);
            saveDescContainer.Add(_saveDescriptionField);
            
            // Button container
            var buttonContainer = new VisualElement();
            buttonContainer.style.flexDirection = FlexDirection.Row;
            buttonContainer.style.justifyContent = Justify.Center;
            buttonContainer.style.marginBottom = 15f;
            
            // Save button
            _saveButton = new Button();
            _saveButton.text = "💾 Save Game";
            _saveButton.style.paddingTop = new StyleLength(12f); // Fixed padding;
            _saveButton.style.marginRight = 10f;
            _saveButton.style.backgroundColor = new Color(0.2f, 0.6f, 0.2f, 1f);
            _saveButton.style.color = Color.white;
            _saveButton.style.borderTopLeftRadius = 6f;
            _saveButton.style.borderTopRightRadius = 6f;
            _saveButton.style.borderBottomLeftRadius = 6f;
            _saveButton.style.borderBottomRightRadius = 6f;
            _saveButton.style.borderTopWidth = 0f;
            _saveButton.style.borderRightWidth = 0f;
            _saveButton.style.borderBottomWidth = 0f;
            _saveButton.style.borderLeftWidth = 0f;
            _saveButton.style.minWidth = 120f;
            _saveButton.clicked += OnSaveButtonClicked;
            
            // Quick save button
            _quickSaveButton = new Button();
            _quickSaveButton.text = "⚡ Quick Save";
            _quickSaveButton.style.paddingTop = new StyleLength(12f); // Fixed padding;
            _quickSaveButton.style.backgroundColor = new Color(0.6f, 0.4f, 0.2f, 1f);
            _quickSaveButton.style.color = Color.white;
            _quickSaveButton.style.borderTopLeftRadius = 6f;
            _quickSaveButton.style.borderTopRightRadius = 6f;
            _quickSaveButton.style.borderBottomLeftRadius = 6f;
            _quickSaveButton.style.borderBottomRightRadius = 6f;
            _quickSaveButton.style.borderTopWidth = 0f;
            _quickSaveButton.style.borderRightWidth = 0f;
            _quickSaveButton.style.borderBottomWidth = 0f;
            _quickSaveButton.style.borderLeftWidth = 0f;
            _quickSaveButton.style.minWidth = 120f;
            _quickSaveButton.clicked += OnQuickSaveClicked;
            
            buttonContainer.Add(_saveButton);
            buttonContainer.Add(_quickSaveButton);
            
            // Save status label
            _saveStatusLabel = new Label("");
            _saveStatusLabel.name = "save-status-label";
            _saveStatusLabel.style.fontSize = 14f;
            _saveStatusLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _saveStatusLabel.style.marginTop = 10f;
            
            saveFormContainer.Add(saveNameContainer);
            saveFormContainer.Add(saveDescContainer);
            saveFormContainer.Add(buttonContainer);
            saveFormContainer.Add(_saveStatusLabel);
            
            _saveContent.Add(saveFormContainer);
            _rootContainer.Add(_saveContent);
        }
        
        private void CreateLoadTab()
        {
            _loadContent = new VisualElement();
            _loadContent.name = "load-content";
            _loadContent.style.flexGrow = 1f;
            _loadContent.style.display = DisplayStyle.None;
            _loadContent.style.flexDirection = FlexDirection.Row;
            
            // Save slot list (left side)
            var slotListContainer = new VisualElement();
            slotListContainer.style.width = new Length(60, LengthUnit.Percent);
            slotListContainer.style.marginRight = 15f;
            slotListContainer.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            slotListContainer.style.borderTopLeftRadius = 8f;
            slotListContainer.style.borderTopRightRadius = 8f;
            slotListContainer.style.borderBottomLeftRadius = 8f;
            slotListContainer.style.borderBottomRightRadius = 8f;
            slotListContainer.style.paddingTop = new StyleLength(15f); // Fixed padding;
            
            var slotListTitle = new Label("Save Slots");
            slotListTitle.style.fontSize = 16f;
            slotListTitle.style.color = Color.white;
            slotListTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            slotListTitle.style.marginBottom = 10f;
            slotListTitle.style.unityTextAlign = TextAnchor.MiddleCenter;
            
            _saveSlotScrollView = new ScrollView();
            _saveSlotScrollView.style.flexGrow = 1f;
            _saveSlotScrollView.style.maxHeight = 400f;
            
            _saveSlotContainer = new VisualElement();
            _saveSlotContainer.name = "save-slot-container";
            
            _saveSlotScrollView.Add(_saveSlotContainer);
            
            slotListContainer.Add(slotListTitle);
            slotListContainer.Add(_saveSlotScrollView);
            
            // Slot details panel (right side)
            _slotDetailsPanel = new VisualElement();
            _slotDetailsPanel.style.width = new Length(40, LengthUnit.Percent);
            _slotDetailsPanel.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            _slotDetailsPanel.style.borderTopLeftRadius = 8f;
            _slotDetailsPanel.style.borderTopRightRadius = 8f;
            _slotDetailsPanel.style.borderBottomLeftRadius = 8f;
            _slotDetailsPanel.style.borderBottomRightRadius = 8f;
            _slotDetailsPanel.style.paddingTop = new StyleLength(15f); // Fixed padding;
            
            CreateSlotDetailsPanel();
            
            _loadContent.Add(slotListContainer);
            _loadContent.Add(_slotDetailsPanel);
            _rootContainer.Add(_loadContent);
        }
        
        private void CreateSlotDetailsPanel()
        {
            var detailsTitle = new Label("Save Details");
            detailsTitle.style.fontSize = 16f;
            detailsTitle.style.color = Color.white;
            detailsTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            detailsTitle.style.marginBottom = 15f;
            detailsTitle.style.unityTextAlign = TextAnchor.MiddleCenter;
            
            // Placeholder for no selection
            var noSelectionLabel = new Label("Select a save slot to view details");
            noSelectionLabel.name = "no-selection-placeholder";
            noSelectionLabel.style.fontSize = 14f;
            noSelectionLabel.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            noSelectionLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            noSelectionLabel.style.marginTop = 50f;
            
            // Load button container
            var loadButtonContainer = new VisualElement();
            loadButtonContainer.name = "load-button-container";
            loadButtonContainer.style.position = Position.Absolute;
            loadButtonContainer.style.bottom = 15f;
            loadButtonContainer.style.left = 15f;
            loadButtonContainer.style.right = 15f;
            loadButtonContainer.style.display = DisplayStyle.None;
            
            var buttonRow1 = new VisualElement();
            buttonRow1.style.flexDirection = FlexDirection.Row;
            buttonRow1.style.marginBottom = 10f;
            
            _loadButton = new Button();
            _loadButton.text = "📁 Load Game";
            _loadButton.style.paddingTop = new StyleLength(10f); // Fixed padding;
            _loadButton.style.marginRight = 5f;
            _loadButton.style.backgroundColor = new Color(0.2f, 0.6f, 0.2f, 1f);
            _loadButton.style.color = Color.white;
            _loadButton.style.borderTopLeftRadius = 4f;
            _loadButton.style.borderTopRightRadius = 4f;
            _loadButton.style.borderBottomLeftRadius = 4f;
            _loadButton.style.borderBottomRightRadius = 4f;
            _loadButton.style.borderTopWidth = 0f;
            _loadButton.style.borderRightWidth = 0f;
            _loadButton.style.borderBottomWidth = 0f;
            _loadButton.style.borderLeftWidth = 0f;
            _loadButton.style.flexGrow = 1f;
            _loadButton.clicked += OnLoadButtonClicked;
            
            _deleteButton = new Button();
            _deleteButton.text = "🗑️ Delete";
            _deleteButton.style.paddingTop = new StyleLength(10f); // Fixed padding;
            _deleteButton.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 1f);
            _deleteButton.style.color = Color.white;
            _deleteButton.style.borderTopLeftRadius = 4f;
            _deleteButton.style.borderTopRightRadius = 4f;
            _deleteButton.style.borderBottomLeftRadius = 4f;
            _deleteButton.style.borderBottomRightRadius = 4f;
            _deleteButton.style.borderTopWidth = 0f;
            _deleteButton.style.borderRightWidth = 0f;
            _deleteButton.style.borderBottomWidth = 0f;
            _deleteButton.style.borderLeftWidth = 0f;
            _deleteButton.style.flexGrow = 1f;
            _deleteButton.clicked += OnDeleteButtonClicked;
            
            buttonRow1.Add(_loadButton);
            buttonRow1.Add(_deleteButton);
            
            _quickLoadButton = new Button();
            _quickLoadButton.text = "⚡ Quick Load (Most Recent)";
            _quickLoadButton.style.paddingTop = new StyleLength(10f); // Fixed padding;
            _quickLoadButton.style.backgroundColor = new Color(0.6f, 0.4f, 0.2f, 1f);
            _quickLoadButton.style.color = Color.white;
            _quickLoadButton.style.borderTopLeftRadius = 4f;
            _quickLoadButton.style.borderTopRightRadius = 4f;
            _quickLoadButton.style.borderBottomLeftRadius = 4f;
            _quickLoadButton.style.borderBottomRightRadius = 4f;
            _quickLoadButton.style.borderTopWidth = 0f;
            _quickLoadButton.style.borderRightWidth = 0f;
            _quickLoadButton.style.borderBottomWidth = 0f;
            _quickLoadButton.style.borderLeftWidth = 0f;
            _quickLoadButton.clicked += OnQuickLoadClicked;
            
            loadButtonContainer.Add(buttonRow1);
            loadButtonContainer.Add(_quickLoadButton);
            
            // Load status label
            _loadStatusLabel = new Label("");
            _loadStatusLabel.name = "load-status-label";
            _loadStatusLabel.style.fontSize = 12f;
            _loadStatusLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _loadStatusLabel.style.position = Position.Absolute;
            _loadStatusLabel.style.bottom = -20f;
            _loadStatusLabel.style.left = 0f;
            _loadStatusLabel.style.right = 0f;
            
            _slotDetailsPanel.Add(detailsTitle);
            _slotDetailsPanel.Add(noSelectionLabel);
            _slotDetailsPanel.Add(loadButtonContainer);
            _slotDetailsPanel.Add(_loadStatusLabel);
        }
        
        private void ShowSaveTab()
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
        
        private void ShowLoadTab()
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
        
        private void RefreshSaveSlots()
        {
            // if (_saveManager == null) return;
            
            // var availableSlots = _saveManager.AvailableSaveSlots;
            
            // Placeholder data for compilation
            var availableSlots = new List<ProjectChimera.Data.Save.SaveSlotData>();
            
            // Clear existing slot elements
            _saveSlotContainer.Clear();
            _saveSlotElements.Clear();
            _slotDataMap.Clear();
            
            if (availableSlots.Count == 0)
            {
                var noSlotsLabel = new Label("No save files found");
                noSlotsLabel.style.fontSize = 14f;
                noSlotsLabel.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                noSlotsLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                noSlotsLabel.style.marginTop = 50f;
                _saveSlotContainer.Add(noSlotsLabel);
                return;
            }
            
            // Create slot elements
            foreach (var slotData in availableSlots.Take(_maxVisibleSlots))
            {
                var slotElement = CreateSaveSlotElement(slotData);
                _saveSlotContainer.Add(slotElement);
                _saveSlotElements.Add(slotElement);
                _slotDataMap[slotData.SlotName] = slotData;
            }
        }
        
        private VisualElement CreateSaveSlotElement(ProjectChimera.Data.Save.SaveSlotData slotData)
        {
            var slotContainer = new VisualElement();
            slotContainer.name = $"slot-{slotData.SlotName}";
            slotContainer.AddToClassList("save-slot");
            slotContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            slotContainer.style.borderTopLeftRadius = 6f;
            slotContainer.style.borderTopRightRadius = 6f;
            slotContainer.style.borderBottomLeftRadius = 6f;
            slotContainer.style.borderBottomRightRadius = 6f;
            slotContainer.style.paddingTop = new StyleLength(12f); // Fixed padding;
            slotContainer.style.marginBottom = 8f;
            slotContainer.style.borderLeftWidth = 3f;
            slotContainer.style.borderLeftColor = slotData.IsAutoSave ? new Color(0.6f, 0.6f, 0.6f, 1f) : _saveSlotColor;
            
            // Slot header
            var headerContainer = new VisualElement();
            headerContainer.style.flexDirection = FlexDirection.Row;
            headerContainer.style.justifyContent = Justify.SpaceBetween;
            headerContainer.style.alignItems = Align.Center;
            headerContainer.style.marginBottom = 8f;
            
            var slotName = new Label(slotData.SlotName);
            slotName.style.fontSize = 14f;
            slotName.style.color = Color.white;
            slotName.style.unityFontStyleAndWeight = FontStyle.Bold;
            
            var typeIcon = new Label(slotData.IsAutoSave ? "⚙️" : "💾");
            typeIcon.style.fontSize = 16f;
            
            headerContainer.Add(slotName);
            headerContainer.Add(typeIcon);
            
            // Slot details
            var detailsContainer = new VisualElement();
            
            var descriptionLabel = new Label(string.IsNullOrEmpty(slotData.Description) ? "No description" : slotData.Description);
            descriptionLabel.style.fontSize = 12f;
            descriptionLabel.style.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            descriptionLabel.style.marginBottom = 4f;
            
            var infoContainer = new VisualElement();
            infoContainer.style.flexDirection = FlexDirection.Row;
            infoContainer.style.justifyContent = Justify.SpaceBetween;
            
            var levelInfo = new Label($"Level {slotData.PlayerLevel}");
            levelInfo.style.fontSize = 11f;
            levelInfo.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            
            var dateInfo = new Label(slotData.LastSaveTime.ToString("MMM dd, HH:mm"));
            dateInfo.style.fontSize = 11f;
            dateInfo.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            
            infoContainer.Add(levelInfo);
            infoContainer.Add(dateInfo);
            
            detailsContainer.Add(descriptionLabel);
            detailsContainer.Add(infoContainer);
            
            slotContainer.Add(headerContainer);
            slotContainer.Add(detailsContainer);
            
            // Add click handler
            slotContainer.RegisterCallback<ClickEvent>(evt => OnSaveSlotSelected(slotData.SlotName));
            
            // Add hover effects
            slotContainer.RegisterCallback<MouseEnterEvent>(evt =>
            {
                slotContainer.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);
            });
            
            slotContainer.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                if (_selectedSlotName != slotData.SlotName)
                {
                    slotContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                }
            });
            
            return slotContainer;
        }
        
        private void OnSaveSlotSelected(string slotName)
        {
            // Update visual selection
            foreach (var element in _saveSlotElements)
            {
                element.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            }
            
            var selectedElement = _saveSlotElements.FirstOrDefault(e => e.name == $"slot-{slotName}");
            if (selectedElement != null)
            {
                selectedElement.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f, 0.9f);
            }
            
            _selectedSlotName = slotName;
            UpdateSlotDetailsPanel(slotName);
        }
        
        private void UpdateSlotDetailsPanel(string slotName)
        {
            var placeholder = _slotDetailsPanel.Q<Label>("no-selection-placeholder");
            var buttonContainer = _slotDetailsPanel.Q<VisualElement>("load-button-container");
            
            if (string.IsNullOrEmpty(slotName))
            {
                placeholder.style.display = DisplayStyle.Flex;
                buttonContainer.style.display = DisplayStyle.None;
                return;
            }
            
            placeholder.style.display = DisplayStyle.None;
            buttonContainer.style.display = DisplayStyle.Flex;
            
            if (_slotDataMap.TryGetValue(slotName, out var slotData))
            {
                // Update slot details display
                // This would show more detailed information about the save
                UpdateSlotDetailContent(slotData);
            }
        }
        
        private void UpdateSlotDetailContent(ProjectChimera.Data.Save.SaveSlotData slotData)
        {
            // Create detailed info display
            var existingDetails = _slotDetailsPanel.Q<VisualElement>("detailed-info");
            existingDetails?.RemoveFromHierarchy();
            
            var detailsContainer = new VisualElement();
            detailsContainer.name = "detailed-info";
            detailsContainer.style.marginTop = 10f;
            detailsContainer.style.marginBottom = 60f;
            
            var details = new List<(string label, string value)>
            {
                ("Save Name:", slotData.SlotName),
                ("Description:", string.IsNullOrEmpty(slotData.Description) ? "None" : slotData.Description),
                ("Player Level:", slotData.PlayerLevel.ToString()),
                ("Play Time:", slotData.PlayTime.ToString(@"hh\:mm\:ss")),
                ("Plants:", slotData.TotalPlants.ToString()),
                ("Currency:", $"${slotData.Currency:F0}"),
                ("Last Saved:", slotData.LastSaveTime.ToString("yyyy-MM-dd HH:mm:ss")),
                ("File Size:", $"{slotData.FileSizeBytes / 1024f:F1} KB"),
                ("Type:", slotData.IsAutoSave ? "Auto Save" : "Manual Save")
            };
            
            foreach (var (label, value) in details)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.justifyContent = Justify.SpaceBetween;
                row.style.marginBottom = 5f;
                
                var labelElement = new Label(label);
                labelElement.style.fontSize = 12f;
                labelElement.style.color = new Color(0.8f, 0.8f, 0.8f, 1f);
                
                var valueElement = new Label(value);
                valueElement.style.fontSize = 12f;
                valueElement.style.color = Color.white;
                valueElement.style.unityTextAlign = TextAnchor.MiddleRight;
                
                row.Add(labelElement);
                row.Add(valueElement);
                detailsContainer.Add(row);
            }
            
            _slotDetailsPanel.Insert(2, detailsContainer);
        }
        
        private async void OnSaveButtonClicked()
        {
            if (_isCurrentlySaving) return;
            
            string saveName = _saveNameField.value.Trim();
            string description = _saveDescriptionField.value.Trim();
            
            if (string.IsNullOrEmpty(saveName))
            {
                ShowSaveStatus("Please enter a save name", Color.red);
                return;
            }
            
            _isCurrentlySaving = true;
            ShowSaveStatus("Saving game...", Color.yellow);
            
            // var result = await _saveManager.CreateNewSave(saveName, description);
            
            // Placeholder result for compilation
            var result = new { Success = true, ErrorMessage = "" };
            
            if (result.Success)
            {
                ShowSaveStatus("Game saved successfully!", Color.green);
                _saveNameField.value = $"Save_{DateTime.Now:yyyyMMdd_HHmm}";
                _saveDescriptionField.value = "";
            }
            // else
            // {
                ShowSaveStatus($"Save failed: {result.ErrorMessage}", Color.red);
            // }
            
            _isCurrentlySaving = false;
        }
        
        private async void OnQuickSaveClicked()
        {
            if (_isCurrentlySaving) return;
            
            _isCurrentlySaving = true;
            ShowSaveStatus("Quick saving...", Color.yellow);
            
            // var result = await _saveManager.QuickSave();
            
            // Placeholder result for compilation
            var result = new { Success = true, ErrorMessage = "" };
            
            if (result.Success)
            {
                ShowSaveStatus("Quick save successful!", Color.green);
            }
            // else
            // {
                ShowSaveStatus($"Quick save failed: {result.ErrorMessage}", Color.red);
            // }
            
            _isCurrentlySaving = false;
        }
        
        private async void OnLoadButtonClicked()
        {
            if (_isCurrentlyLoading || string.IsNullOrEmpty(_selectedSlotName)) return;
            
            _isCurrentlyLoading = true;
            ShowLoadStatus("Loading game...", Color.yellow);
            
            // var result = await _saveManager.LoadGame(_selectedSlotName);
            
            // Placeholder result for compilation
            var result = new { Success = true, ErrorMessage = "" };
            
            if (result.Success)
            {
                ShowLoadStatus("Game loaded successfully!", Color.green);
            }
            // else
            // {
                ShowLoadStatus($"Load failed: {result.ErrorMessage}", Color.red);
            // }
            
            _isCurrentlyLoading = false;
        }
        
        private async void OnQuickLoadClicked()
        {
            if (_isCurrentlyLoading) return;
            
            _isCurrentlyLoading = true;
            ShowLoadStatus("Quick loading...", Color.yellow);
            
            // var result = await _saveManager.QuickLoad();
            
            // Placeholder result for compilation
            var result = new { Success = true, ErrorMessage = "" };
            
            if (result.Success)
            {
                ShowLoadStatus("Quick load successful!", Color.green);
            }
            // else
            // {
                ShowLoadStatus($"Quick load failed: {result.ErrorMessage}", Color.red);
            // }
            
            _isCurrentlyLoading = false;
        }
        
        private void OnDeleteButtonClicked()
        {
            if (string.IsNullOrEmpty(_selectedSlotName)) return;
            
            // Show confirmation dialog (simplified for now)
            // if (_saveManager.DeleteSaveSlot(_selectedSlotName))
            // {
                ShowLoadStatus("Save slot deleted", Color.yellow);
                _selectedSlotName = "";
                RefreshSaveSlots();
                UpdateSlotDetailsPanel("");
            // }
            // else
            // {
                ShowLoadStatus("Failed to delete save slot", Color.red);
            // }
        }
        
        private void ShowSaveStatus(string message, Color color)
        {
            _saveStatusLabel.text = message;
            _saveStatusLabel.style.color = color;
            
            // Clear status after 3 seconds
            _rootContainer?.schedule.Execute(() => _saveStatusLabel.text = "").ExecuteLater(3000);
        }
        
        private void ShowLoadStatus(string message, Color color)
        {
            _loadStatusLabel.text = message;
            _loadStatusLabel.style.color = color;
            
            // Clear status after 3 seconds
            _rootContainer?.schedule.Execute(() => _loadStatusLabel.text = "").ExecuteLater(3000);
        }
        
        private void UpdateUIState()
        {
            // Update UI elements based on current operations
            if (_saveButton != null)
            {
                _saveButton.SetEnabled(!_isCurrentlySaving && !_isCurrentlyLoading);
            }
            
            if (_quickSaveButton != null)
            {
                _quickSaveButton.SetEnabled(!_isCurrentlySaving && !_isCurrentlyLoading);
            }
            
            if (_loadButton != null)
            {
                _loadButton.SetEnabled(!_isCurrentlyLoading && !_isCurrentlySaving && !string.IsNullOrEmpty(_selectedSlotName));
            }
            
            if (_quickLoadButton != null)
            {
                _quickLoadButton.SetEnabled(!_isCurrentlyLoading && !_isCurrentlySaving);
            }
            
            if (_deleteButton != null)
            {
                _deleteButton.SetEnabled(!_isCurrentlyLoading && !_isCurrentlySaving && !string.IsNullOrEmpty(_selectedSlotName));
            }
        }
        
        private void SubscribeToEvents()
        {
            // if (_saveManager != null)
            // {
                // _saveManager.OnSaveResult += OnSaveResult;
                // _saveManager.OnLoadResult += OnLoadResult;
                // _saveManager.OnAutoSaveCompleted += OnAutoSaveCompleted;
                // _saveManager.OnSaveSlotCreated += OnSaveSlotCreated;
            // }
        }
        
        private void OnSaveResult(SaveResult result)
        {
            LogInfo($"Save Result: {(result.Success ? "Success" : "Failed")} - {result.SlotName}");
            RefreshSaveSlots();
        }
        
        private void OnLoadResult(LoadResult result)
        {
            LogInfo($"Load Result: {(result.Success ? "Success" : "Failed")}");
        }
        
        private void OnAutoSaveCompleted(string slotName)
        {
            LogInfo($"Auto-save completed: {slotName}");
            RefreshSaveSlots();
        }
        
        private void OnSaveSlotCreated(ProjectChimera.Data.Save.SaveSlotData slotData)
        {
            LogInfo($"New save slot created: {slotData.SlotName}");
            RefreshSaveSlots();
        }
        
        protected override void OnBeforeHide()
        {
            // Unsubscribe from events
            // if (_saveManager != null)
            // {
                // _saveManager.OnSaveResult -= OnSaveResult;
                // _saveManager.OnLoadResult -= OnLoadResult;
                // _saveManager.OnAutoSaveCompleted -= OnAutoSaveCompleted;
                // _saveManager.OnSaveSlotCreated -= OnSaveSlotCreated;
            // }
            
            base.OnBeforeHide();
        }
        
        private void OnDestroy()
        {
            if (UpdateOrchestrator.Instance != null)
            {
                UpdateOrchestrator.Instance.UnregisterTickable(this);
            }
        }
    }
}