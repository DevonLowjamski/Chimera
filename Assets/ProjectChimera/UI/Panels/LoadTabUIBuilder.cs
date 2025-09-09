using UnityEngine;
using UnityEngine.UIElements;
using ProjectChimera.Data.Save;
using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// UI Builder for the Load tab in the Save/Load Panel.
    /// Handles creation and management of load-related UI elements and save slot display.
    /// </summary>
    public class LoadTabUIBuilder
    {
        private readonly SaveLoadPanelCore _panelCore;
        
        // Load Tab UI Elements
        private ScrollView _saveSlotScrollView;
        private VisualElement _saveSlotContainer;
        private VisualElement _slotDetailsPanel;
        private Button _loadButton;
        private Button _deleteButton;
        private Button _quickLoadButton;
        private Label _loadStatusLabel;
        
        // Save Slot Management
        private List<VisualElement> _saveSlotElements = new List<VisualElement>();
        private Dictionary<string, SaveSlotData> _slotDataMap = new Dictionary<string, SaveSlotData>();
        private string _selectedSlotName = "";
        
        public string SelectedSlotName => _selectedSlotName;
        
        public LoadTabUIBuilder(SaveLoadPanelCore panelCore)
        {
            _panelCore = panelCore;
        }
        
        public void CreateLoadTab()
        {
            var loadContent = _panelCore.GetLoadContent();
            
            CreateSlotListContainer(loadContent);
            CreateSlotDetailsPanel(loadContent);
        }
        
        private void CreateSlotListContainer(VisualElement parent)
        {
            // Save slot list (left side)
            var slotListContainer = new VisualElement();
            slotListContainer.style.width = new Length(60, LengthUnit.Percent);
            slotListContainer.style.marginRight = 15f;
            slotListContainer.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            slotListContainer.style.borderTopLeftRadius = 8f;
            slotListContainer.style.borderTopRightRadius = 8f;
            slotListContainer.style.borderBottomLeftRadius = 8f;
            slotListContainer.style.borderBottomRightRadius = 8f;
            slotListContainer.style.paddingTop = new StyleLength(15f);
            
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
            
            parent.Add(slotListContainer);
        }
        
        private void CreateSlotDetailsPanel(VisualElement parent)
        {
            // Slot details panel (right side)
            _slotDetailsPanel = new VisualElement();
            _slotDetailsPanel.style.width = new Length(40, LengthUnit.Percent);
            _slotDetailsPanel.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            _slotDetailsPanel.style.borderTopLeftRadius = 8f;
            _slotDetailsPanel.style.borderTopRightRadius = 8f;
            _slotDetailsPanel.style.borderBottomLeftRadius = 8f;
            _slotDetailsPanel.style.borderBottomRightRadius = 8f;
            _slotDetailsPanel.style.paddingTop = new StyleLength(15f);
            
            CreateSlotDetailsPanelContent();
            
            parent.Add(_slotDetailsPanel);
        }
        
        private void CreateSlotDetailsPanelContent()
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
            
            CreateLoadButtons();
            CreateLoadStatusLabel();
            
            _slotDetailsPanel.Add(detailsTitle);
            _slotDetailsPanel.Add(noSelectionLabel);
        }
        
        private void CreateLoadButtons()
        {
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
            _loadButton.style.paddingTop = new StyleLength(10f);
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
            _deleteButton.style.paddingTop = new StyleLength(10f);
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
            _quickLoadButton.style.paddingTop = new StyleLength(10f);
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
            
            _slotDetailsPanel.Add(loadButtonContainer);
        }
        
        private void CreateLoadStatusLabel()
        {
            // Load status label
            _loadStatusLabel = new Label("");
            _loadStatusLabel.name = "load-status-label";
            _loadStatusLabel.style.fontSize = 12f;
            _loadStatusLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _loadStatusLabel.style.position = Position.Absolute;
            _loadStatusLabel.style.bottom = -20f;
            _loadStatusLabel.style.left = 0f;
            _loadStatusLabel.style.right = 0f;
            
            _slotDetailsPanel.Add(_loadStatusLabel);
        }
        
        public void RefreshSaveSlots()
        {
            // Placeholder data for compilation - in production, get from SaveManager
            var availableSlots = new List<SaveSlotData>();
            
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
            
            // Create slot elements using the slot renderer
            var slotRenderer = GetSlotRenderer();
            foreach (var slotData in availableSlots.Take(_panelCore.MaxVisibleSlots))
            {
                var slotElement = slotRenderer?.CreateSaveSlotElement(slotData, OnSaveSlotSelected);
                if (slotElement != null)
                {
                    _saveSlotContainer.Add(slotElement);
                    _saveSlotElements.Add(slotElement);
                    _slotDataMap[slotData.SlotName] = slotData;
                }
            }
        }
        
        public void OnSaveSlotSelected(string slotName)
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
                UpdateSlotDetailContent(slotData);
            }
        }
        
        private void UpdateSlotDetailContent(SaveSlotData slotData)
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
        
        private void OnLoadButtonClicked()
        {
            if (_panelCore.IsCurrentlyLoading || string.IsNullOrEmpty(_selectedSlotName)) return;
            
            var operationHandler = GetOperationHandler();
            operationHandler?.HandleLoadGame(_selectedSlotName, OnLoadCompleted);
        }
        
        private void OnQuickLoadClicked()
        {
            if (_panelCore.IsCurrentlyLoading) return;
            
            var operationHandler = GetOperationHandler();
            operationHandler?.HandleQuickLoad(OnQuickLoadCompleted);
        }
        
        private void OnDeleteButtonClicked()
        {
            if (string.IsNullOrEmpty(_selectedSlotName)) return;
            
            var operationHandler = GetOperationHandler();
            operationHandler?.HandleDeleteSave(_selectedSlotName, OnDeleteCompleted);
        }
        
        private void OnLoadCompleted(bool success, string message)
        {
            ShowLoadStatus(message, success ? Color.green : Color.red);
        }
        
        private void OnQuickLoadCompleted(bool success, string message)
        {
            ShowLoadStatus(message, success ? Color.green : Color.red);
        }
        
        private void OnDeleteCompleted(bool success, string message)
        {
            ShowLoadStatus(message, success ? Color.yellow : Color.red);
            if (success)
            {
                _selectedSlotName = "";
                RefreshSaveSlots();
                UpdateSlotDetailsPanel("");
            }
        }
        
        public void ShowLoadStatus(string message, Color color)
        {
            _loadStatusLabel.text = message;
            _loadStatusLabel.style.color = color;
            
            // Clear status after 3 seconds
            var rootContainer = _panelCore.GetRootContainer();
            rootContainer?.schedule.Execute(() => _loadStatusLabel.text = "").ExecuteLater(3000);
        }
        
        public void UpdateLoadButtonStates(bool canLoad, bool hasSelection)
        {
            if (_loadButton != null)
            {
                _loadButton.SetEnabled(canLoad && hasSelection);
            }
            
            if (_quickLoadButton != null)
            {
                _quickLoadButton.SetEnabled(canLoad);
            }
            
            if (_deleteButton != null)
            {
                _deleteButton.SetEnabled(canLoad && hasSelection);
            }
        }
        
        private SaveLoadOperationHandler GetOperationHandler()
        {
            // Access operation handler through reflection or public property
            var operationHandlerField = _panelCore.GetType()
                .GetField("_operationHandler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return operationHandlerField?.GetValue(_panelCore) as SaveLoadOperationHandler;
        }
        
        private SaveSlotUIRenderer GetSlotRenderer()
        {
            // Access slot renderer through reflection or public property
            var slotRendererField = _panelCore.GetType()
                .GetField("_slotRenderer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return slotRendererField?.GetValue(_panelCore) as SaveSlotUIRenderer;
        }
    }
}