using UnityEngine;
using UnityEngine.UIElements;
using ProjectChimera.Data.Save;
using System;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Handles rendering and interaction for individual save slot UI elements.
    /// Creates save slot visual elements with hover effects and click handling.
    /// </summary>
    public class SaveSlotUIRenderer
    {
        private readonly SaveLoadPanelCore _panelCore;

        public delegate void SlotSelectionCallback(string slotName);

        public SaveSlotUIRenderer(SaveLoadPanelCore panelCore)
        {
            _panelCore = panelCore;
        }

        public VisualElement CreateSaveSlotElement(SaveSlotData slotData, SlotSelectionCallback onSelection)
        {
            var slotContainer = new VisualElement();
            slotContainer.name = $"slot-{slotData.SlotName}";
            slotContainer.AddToClassList("save-slot");

            ConfigureSlotContainerStyle(slotContainer, slotData);
            CreateSlotHeader(slotContainer, slotData);
            CreateSlotDetails(slotContainer, slotData);
            SetupSlotInteractions(slotContainer, slotData, onSelection);

            return slotContainer;
        }

        private void ConfigureSlotContainerStyle(VisualElement container, SaveSlotData slotData)
        {
            container.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            container.style.borderTopLeftRadius = 6f;
            container.style.borderTopRightRadius = 6f;
            container.style.borderBottomLeftRadius = 6f;
            container.style.borderBottomRightRadius = 6f;
            container.style.paddingTop = new StyleLength(12f);
            container.style.paddingLeft = new StyleLength(12f);
            container.style.paddingRight = new StyleLength(12f);
            container.style.paddingBottom = new StyleLength(12f);
            container.style.marginBottom = 8f;
            container.style.borderLeftWidth = 3f;

            // Different border color for auto saves
            Color borderColor = slotData.IsAutoSave
                ? new Color(0.6f, 0.6f, 0.6f, 1f)
                : _panelCore.SaveSlotColor;
            container.style.borderLeftColor = borderColor;
        }

        private void CreateSlotHeader(VisualElement parent, SaveSlotData slotData)
        {
            var headerContainer = new VisualElement();
            headerContainer.style.flexDirection = FlexDirection.Row;
            headerContainer.style.justifyContent = Justify.SpaceBetween;
            headerContainer.style.alignItems = Align.Center;
            headerContainer.style.marginBottom = 8f;

            var slotName = new Label(slotData.SlotName);
            slotName.style.fontSize = 14f;
            slotName.style.color = Color.white;
            slotName.style.unityFontStyleAndWeight = FontStyle.Bold;

            var typeIcon = CreateTypeIcon(slotData);

            headerContainer.Add(slotName);
            headerContainer.Add(typeIcon);
            parent.Add(headerContainer);
        }

        private Label CreateTypeIcon(SaveSlotData slotData)
        {
            var typeIcon = new Label(slotData.IsAutoSave ? "⚙️" : "💾");
            typeIcon.style.fontSize = 16f;

            // Add tooltip information
            typeIcon.tooltip = slotData.IsAutoSave ? "Auto Save" : "Manual Save";

            return typeIcon;
        }

        private void CreateSlotDetails(VisualElement parent, SaveSlotData slotData)
        {
            var detailsContainer = new VisualElement();

            CreateDescriptionLabel(detailsContainer, slotData);
            CreateInfoContainer(detailsContainer, slotData);

            parent.Add(detailsContainer);
        }

        private void CreateDescriptionLabel(VisualElement parent, SaveSlotData slotData)
        {
            string description = string.IsNullOrEmpty(slotData.Description)
                ? "No description"
                : slotData.Description;

            var descriptionLabel = new Label(description);
            descriptionLabel.style.fontSize = 12f;
            descriptionLabel.style.color = string.IsNullOrEmpty(slotData.Description)
                ? new Color(0.6f, 0.6f, 0.6f, 1f)  // Dimmer for placeholder text
                : new Color(0.8f, 0.8f, 0.8f, 1f); // Normal for actual description
            descriptionLabel.style.marginBottom = 4f;

            // Truncate long descriptions
            if (description.Length > 60)
            {
                descriptionLabel.text = description.Substring(0, 57) + "...";
                descriptionLabel.tooltip = description; // Full text in tooltip
            }

            parent.Add(descriptionLabel);
        }

        private void CreateInfoContainer(VisualElement parent, SaveSlotData slotData)
        {
            var infoContainer = new VisualElement();
            infoContainer.style.flexDirection = FlexDirection.Row;
            infoContainer.style.justifyContent = Justify.SpaceBetween;

            var levelInfo = CreateLevelInfo(slotData);
            var dateInfo = CreateDateInfo(slotData);

            infoContainer.Add(levelInfo);
            infoContainer.Add(dateInfo);
            parent.Add(infoContainer);
        }

        private Label CreateLevelInfo(SaveSlotData slotData)
        {
            var levelInfo = new Label($"Level {slotData.PlayerLevel}");
            levelInfo.style.fontSize = 11f;
            levelInfo.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);

            // Add additional info in tooltip
            levelInfo.tooltip = $"Player Level: {slotData.PlayerLevel}\nPlay Time: {slotData.PlayTime:hh\\:mm\\:ss}\nPlants: {slotData.TotalPlants}";

            return levelInfo;
        }

        private Label CreateDateInfo(SaveSlotData slotData)
        {
            var dateInfo = new Label(slotData.LastSaveTime.ToString("MMM dd, HH:mm"));
            dateInfo.style.fontSize = 11f;
            dateInfo.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);

            // Add full date in tooltip
            dateInfo.tooltip = $"Saved: {slotData.LastSaveTime:yyyy-MM-dd HH:mm:ss}\nFile Size: {slotData.FileSizeBytes / 1024f:F1} KB";

            return dateInfo;
        }

        private void SetupSlotInteractions(VisualElement container, SaveSlotData slotData, SlotSelectionCallback onSelection)
        {
            // Add click handler
            container.RegisterCallback<ClickEvent>(evt =>
            {
                onSelection?.Invoke(slotData.SlotName);
                evt.StopPropagation();
            });

            // Add hover effects
            SetupHoverEffects(container, slotData);

            // Add double-click handler for quick load
            SetupDoubleClickHandler(container, slotData);
        }

        private void SetupHoverEffects(VisualElement container, SaveSlotData slotData)
        {
            var originalBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            var hoverBackgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);

            container.RegisterCallback<MouseEnterEvent>(evt =>
            {
                container.style.backgroundColor = hoverBackgroundColor;

                // Add subtle animation if enabled
                if (_panelCore.EnableSaveAnimations)
                {
                    AnimateHoverIn(container);
                }
            });

            container.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                // Only reset if this slot is not selected
                var loadTabBuilder = GetLoadTabBuilder();
                if (loadTabBuilder?.SelectedSlotName != slotData.SlotName)
                {
                    container.style.backgroundColor = originalBackgroundColor;

                    if (_panelCore.EnableSaveAnimations)
                    {
                        AnimateHoverOut(container);
                    }
                }
            });
        }

        private void SetupDoubleClickHandler(VisualElement container, SaveSlotData slotData)
        {
            var lastClickTime = 0f;
            const float doubleClickThreshold = 0.5f; // 500ms

            container.RegisterCallback<ClickEvent>(evt =>
            {
                var currentTime = Time.unscaledTime;
                if (currentTime - lastClickTime < doubleClickThreshold)
                {
                    // Double-click detected - trigger quick load
                    HandleDoubleClick(slotData);
                    evt.StopPropagation();
                }
                lastClickTime = currentTime;
            });
        }

        private void HandleDoubleClick(SaveSlotData slotData)
        {
            if (_panelCore.IsCurrentlyLoading || _panelCore.IsCurrentlySaving) return;

            // Trigger load operation for this specific slot
            var operationHandler = GetOperationHandler();
            operationHandler?.HandleLoadGame(slotData.SlotName, (success, message) =>
            {
                var loadTabBuilder = GetLoadTabBuilder();
                loadTabBuilder?.ShowLoadStatus(message, success ? Color.green : Color.red);
            });
        }

        #region Animation Methods

        private void AnimateHoverIn(VisualElement element)
        {
            // Subtle scale animation on hover
            element.experimental.animation.Scale(1.02f, 100);
        }

        private void AnimateHoverOut(VisualElement element)
        {
            // Return to normal scale
            element.experimental.animation.Scale(1.0f, 100);
        }

        #endregion

        #region Helper Methods

        private LoadTabUIBuilder GetLoadTabBuilder()
        {
            var loadTabBuilderField = _panelCore.GetType()
                .GetField("_loadTabBuilder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return loadTabBuilderField?.GetValue(_panelCore) as LoadTabUIBuilder;
        }

        private SaveLoadOperationHandler GetOperationHandler()
        {
            var operationHandlerField = _panelCore.GetType()
                .GetField("_operationHandler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return operationHandlerField?.GetValue(_panelCore) as SaveLoadOperationHandler;
        }

        #endregion
    }
}
