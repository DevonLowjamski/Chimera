using UnityEngine;
using UnityEngine.UIElements;
using System;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// UI Builder for the Save tab in the Save/Load Panel.
    /// Handles creation and management of save-related UI elements.
    /// </summary>
    public class SaveTabUIBuilder
    {
        private readonly SaveLoadPanelCore _panelCore;
        
        // Save Tab UI Elements
        private TextField _saveNameField;
        private TextField _saveDescriptionField;
        private Button _saveButton;
        private Button _quickSaveButton;
        private Label _saveStatusLabel;
        
        public SaveTabUIBuilder(SaveLoadPanelCore panelCore)
        {
            _panelCore = panelCore;
        }
        
        public void CreateSaveTab()
        {
            var saveContent = _panelCore.GetSaveContent();
            
            // Save form container
            var saveFormContainer = new VisualElement();
            saveFormContainer.style.maxWidth = 500f;
            saveFormContainer.style.alignSelf = Align.Center;
            
            CreateSaveNameInput(saveFormContainer);
            CreateSaveDescriptionInput(saveFormContainer);
            CreateSaveButtons(saveFormContainer);
            CreateSaveStatusLabel(saveFormContainer);
            
            saveContent.Add(saveFormContainer);
        }
        
        private void CreateSaveNameInput(VisualElement parent)
        {
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
            _saveNameField.style.paddingTop = new StyleLength(8f);
            _saveNameField.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            _saveNameField.style.color = Color.white;
            _saveNameField.style.borderTopLeftRadius = 4f;
            _saveNameField.style.borderTopRightRadius = 4f;
            _saveNameField.style.borderBottomLeftRadius = 4f;
            _saveNameField.style.borderBottomRightRadius = 4f;
            
            saveNameContainer.Add(saveNameLabel);
            saveNameContainer.Add(_saveNameField);
            parent.Add(saveNameContainer);
        }
        
        private void CreateSaveDescriptionInput(VisualElement parent)
        {
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
            _saveDescriptionField.style.paddingTop = new StyleLength(8f);
            _saveDescriptionField.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            _saveDescriptionField.style.color = Color.white;
            _saveDescriptionField.style.borderTopLeftRadius = 4f;
            _saveDescriptionField.style.borderTopRightRadius = 4f;
            _saveDescriptionField.style.borderBottomLeftRadius = 4f;
            _saveDescriptionField.style.borderBottomRightRadius = 4f;
            _saveDescriptionField.style.height = 60f;
            
            saveDescContainer.Add(saveDescLabel);
            saveDescContainer.Add(_saveDescriptionField);
            parent.Add(saveDescContainer);
        }
        
        private void CreateSaveButtons(VisualElement parent)
        {
            var buttonContainer = new VisualElement();
            buttonContainer.style.flexDirection = FlexDirection.Row;
            buttonContainer.style.justifyContent = Justify.Center;
            buttonContainer.style.marginBottom = 15f;
            
            // Save button
            _saveButton = new Button();
            _saveButton.text = "💾 Save Game";
            _saveButton.style.paddingTop = new StyleLength(12f);
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
            _quickSaveButton.style.paddingTop = new StyleLength(12f);
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
            parent.Add(buttonContainer);
        }
        
        private void CreateSaveStatusLabel(VisualElement parent)
        {
            _saveStatusLabel = new Label("");
            _saveStatusLabel.name = "save-status-label";
            _saveStatusLabel.style.fontSize = 14f;
            _saveStatusLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _saveStatusLabel.style.marginTop = 10f;
            
            parent.Add(_saveStatusLabel);
        }
        
        private void OnSaveButtonClicked()
        {
            if (_panelCore.IsCurrentlySaving) return;
            
            string saveName = _saveNameField.value.Trim();
            string description = _saveDescriptionField.value.Trim();
            
            if (string.IsNullOrEmpty(saveName))
            {
                ShowSaveStatus("Please enter a save name", Color.red);
                return;
            }
            
            // Delegate to operation handler
            var operationHandler = GetOperationHandler();
            operationHandler?.HandleSaveGame(saveName, description, OnSaveCompleted);
        }
        
        private void OnQuickSaveClicked()
        {
            if (_panelCore.IsCurrentlySaving) return;
            
            // Delegate to operation handler
            var operationHandler = GetOperationHandler();
            operationHandler?.HandleQuickSave(OnQuickSaveCompleted);
        }
        
        private void OnSaveCompleted(bool success, string message)
        {
            if (success)
            {
                ShowSaveStatus(message, Color.green);
                _saveNameField.value = $"Save_{DateTime.Now:yyyyMMdd_HHmm}";
                _saveDescriptionField.value = "";
            }
            else
            {
                ShowSaveStatus(message, Color.red);
            }
        }
        
        private void OnQuickSaveCompleted(bool success, string message)
        {
            ShowSaveStatus(message, success ? Color.green : Color.red);
        }
        
        public void ShowSaveStatus(string message, Color color)
        {
            _saveStatusLabel.text = message;
            _saveStatusLabel.style.color = color;
            
            // Clear status after 3 seconds
            var rootContainer = _panelCore.GetRootContainer();
            rootContainer?.schedule.Execute(() => _saveStatusLabel.text = "").ExecuteLater(3000);
        }
        
        public void UpdateSaveButtonStates(bool canSave)
        {
            if (_saveButton != null)
            {
                _saveButton.SetEnabled(canSave);
            }
            
            if (_quickSaveButton != null)
            {
                _quickSaveButton.SetEnabled(canSave);
            }
        }
        
        private SaveLoadOperationHandler GetOperationHandler()
        {
            // Access operation handler through reflection or public property
            // This is a simplified approach - in production, use proper dependency injection
            var operationHandlerField = _panelCore.GetType()
                .GetField("_operationHandler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return operationHandlerField?.GetValue(_panelCore) as SaveLoadOperationHandler;
        }
    }
}