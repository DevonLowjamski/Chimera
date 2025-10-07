using UnityEngine;
using UnityEngine.UIElements;
using System;
using ChimeraLogger = ProjectChimera.Core.Logging.ChimeraLogger;

namespace ProjectChimera.Systems.Progression
{
    /// <summary>
    /// Simple Offline Progression Summary - Aligned with Project Chimera's vision
    /// Provides a basic time-lapse summary of offline progression as described in gameplay document
    /// Focuses on essential information without complex UI animations or notifications
    /// </summary>
    public class SimpleOfflineProgressionSummary : MonoBehaviour
    {
        [Header("UI Settings")]
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private string _summaryPanelName = "offline-summary-panel";

        private VisualElement _summaryPanel;
        private Label _summaryText;
        private Button _continueButton;

        private void Awake()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            if (_uiDocument == null)
            {
                ChimeraLogger.Log("OTHER", "$1", this);
                return;
            }

            var root = _uiDocument.rootVisualElement;

            // Find or create summary panel
            _summaryPanel = root.Q(_summaryPanelName);
            if (_summaryPanel == null)
            {
                _summaryPanel = new VisualElement();
                _summaryPanel.name = _summaryPanelName;
                _summaryPanel.AddToClassList("offline-summary");

                // Simple layout
                _summaryText = new Label();
                _summaryText.AddToClassList("summary-text");

                _continueButton = new Button();
                _continueButton.text = "Continue";
                _continueButton.clicked += HideSummary;
                _continueButton.AddToClassList("continue-button");

                _summaryPanel.Add(_summaryText);
                _summaryPanel.Add(_continueButton);

                root.Add(_summaryPanel);
            }

            // Hide by default
            _summaryPanel.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Shows a simple time-lapse summary of offline progression
        /// </summary>
        public void ShowTimeLapseSummary(TimeSpan offlineDuration, int plantsHarvested, float totalYield, int newAchievements)
        {
            if (_summaryPanel == null || _summaryText == null)
                return;

            // Create simple summary text as described in gameplay document
            string summary = $"Welcome back!\n\n" +
                           $"Time away: {FormatDuration(offlineDuration)}\n" +
                           $"Plants harvested: {plantsHarvested}\n" +
                           $"Total yield: {totalYield:F1} grams\n";

            if (newAchievements > 0)
            {
                summary += $"New achievements: {newAchievements}\n";
            }

            summary += $"\nYour plants have been growing while you were away.";

            _summaryText.text = summary;
            _summaryPanel.style.display = DisplayStyle.Flex;

            ChimeraLogger.Log("OTHER", "$1", this);
        }

        /// <summary>
        /// Hides the summary panel
        /// </summary>
        public void HideSummary()
        {
            if (_summaryPanel != null)
            {
                _summaryPanel.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// Formats duration for display
        /// </summary>
        private string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalDays >= 1)
            {
                return $"{(int)duration.TotalDays} days, {(int)duration.Hours} hours";
            }
            else if (duration.TotalHours >= 1)
            {
                return $"{(int)duration.TotalHours} hours, {duration.Minutes} minutes";
            }
            else
            {
                return $"{duration.Minutes} minutes";
            }
        }

        /// <summary>
        /// Shows summary for basic offline events
        /// </summary>
        public void ShowBasicSummary(int eventsProcessed)
        {
            ShowTimeLapseSummary(
                TimeSpan.FromHours(2), // Default 2 hours
                eventsProcessed / 10,  // Rough estimate
                eventsProcessed * 5f,  // Rough yield estimate
                0 // No achievements by default
            );
        }

        /// <summary>
        /// Checks if summary is currently visible
        /// </summary>
        public bool IsVisible()
        {
            return _summaryPanel != null && _summaryPanel.style.display == DisplayStyle.Flex;
        }
    }
}
