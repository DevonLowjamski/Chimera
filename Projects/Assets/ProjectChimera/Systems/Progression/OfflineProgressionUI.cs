using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using ProjectChimera.Systems.UI.Advanced;

namespace ProjectChimera.Systems.Progression
{
    /// <summary>
    /// UI system for displaying offline progression results and managing offline progression settings
    /// Provides summary screens, event notifications, and progression replay functionality
    /// </summary>
    public class OfflineProgressionUI : MonoBehaviour
    {
        [Header("UI Configuration")]
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private string _offlineProgressionScreenClass = "offline-progression-screen";
        [SerializeField] private string _progressionSummaryClass = "progression-summary";
        [SerializeField] private string _eventNotificationClass = "event-notification";
        [SerializeField] private bool _enableAnimations = true;
        [SerializeField] private float _animationDuration = 0.3f;
        [SerializeField] private bool _enableAutoShow = true;
        
        [Header("Display Settings")]
        [SerializeField] private int _maxEventsToShow = 10;
        [SerializeField] private int _maxNotificationsToShow = 5;
        [SerializeField] private bool _groupSimilarEvents = true;
        [SerializeField] private bool _showDetailedBreakdown = true;
        [SerializeField] private bool _enableProgressionReplay = true;
        
        // Core Systems
        private OfflineProgressionArchitecture _progressionArchitecture;
        private AdvancedMenuSystem _menuSystem;
        
        // UI Elements
        private VisualElement _rootElement;
        private VisualElement _progressionScreen;
        private VisualElement _summaryContainer;
        private VisualElement _eventsContainer;
        private VisualElement _notificationsContainer;
        private VisualElement _detailsContainer;
        private VisualElement _settingsContainer;
        private Button _continueButton;
        private Button _detailsButton;
        private Button _settingsButton;
        private Button _replayButton;
        
        // State Management
        private OfflineProgressionSession _currentSession;
        private bool _isShowingProgression = false;
        private List<OfflineProgressionEvent> _displayedEvents = new List<OfflineProgressionEvent>();
        private List<string> _displayedNotifications = new List<string>();
        
        // Events
        public event Action OnProgressionScreenShown;
        public event Action OnProgressionScreenClosed;
        public event Action<OfflineProgressionEvent> OnEventClicked;
        public event Action OnReplayRequested;
        
        private void Awake()
        {
            InitializeUI();
        }
        
        private void Start()
        {
            FindSystemReferences();
            SetupEventListeners();
        }
        
        private void InitializeUI()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
            
            if (_uiDocument != null)
            {
                _rootElement = _uiDocument.rootVisualElement;
                CreateUIStructure();
            }
        }
        
        private void FindSystemReferences()
        {
            _progressionArchitecture = UnityEngine.Object.FindObjectOfType<OfflineProgressionArchitecture>();
            _menuSystem = UnityEngine.Object.FindObjectOfType<AdvancedMenuSystem>();
            
            if (_progressionArchitecture == null)
            {
                Debug.LogWarning("[OfflineProgressionUI] OfflineProgressionArchitecture not found");
                return;
            }
        }
        
        private void SetupEventListeners()
        {
            if (_progressionArchitecture != null)
            {
                _progressionArchitecture.OnOfflineProgressionCompleted += ShowOfflineProgressionResults;
            }
        }
        
        private void CreateUIStructure()
        {
            if (_rootElement == null) return;
            
            // Create main progression screen
            _progressionScreen = new VisualElement();
            _progressionScreen.AddToClassList(_offlineProgressionScreenClass);
            _progressionScreen.style.display = DisplayStyle.None;
            _rootElement.Add(_progressionScreen);
            
            // Create header
            var header = CreateHeader();
            _progressionScreen.Add(header);
            
            // Create summary container
            _summaryContainer = new VisualElement();
            _summaryContainer.AddToClassList(_progressionSummaryClass);
            _progressionScreen.Add(_summaryContainer);
            
            // Create events container
            _eventsContainer = new ScrollView(ScrollViewMode.Vertical);
            _eventsContainer.AddToClassList("events-container");
            _progressionScreen.Add(_eventsContainer);
            
            // Create notifications container
            _notificationsContainer = new VisualElement();
            _notificationsContainer.AddToClassList("notifications-container");
            _progressionScreen.Add(_notificationsContainer);
            
            // Create details container (initially hidden)
            _detailsContainer = new VisualElement();
            _detailsContainer.AddToClassList("details-container");
            _detailsContainer.style.display = DisplayStyle.None;
            _progressionScreen.Add(_detailsContainer);
            
            // Create settings container (initially hidden)
            _settingsContainer = new VisualElement();
            _settingsContainer.AddToClassList("settings-container");
            _settingsContainer.style.display = DisplayStyle.None;
            _progressionScreen.Add(_settingsContainer);
            
            // Create buttons
            var buttonContainer = CreateButtonContainer();
            _progressionScreen.Add(buttonContainer);
            
            Debug.Log("[OfflineProgressionUI] UI structure created");
        }
        
        private VisualElement CreateHeader()
        {
            var header = new VisualElement();
            header.AddToClassList("progression-header");
            
            var title = new Label("Welcome Back!");
            title.AddToClassList("progression-title");
            header.Add(title);
            
            var subtitle = new Label("Here's what happened while you were away");
            subtitle.AddToClassList("progression-subtitle");
            header.Add(subtitle);
            
            return header;
        }
        
        private VisualElement CreateButtonContainer()
        {
            var buttonContainer = new VisualElement();
            buttonContainer.AddToClassList("button-container");
            
            _continueButton = new Button(() => CloseProgressionScreen()) { text = "Continue" };
            _continueButton.AddToClassList("primary-button");
            buttonContainer.Add(_continueButton);
            
            _detailsButton = new Button(() => ToggleDetails()) { text = "Details" };
            _detailsButton.AddToClassList("secondary-button");
            buttonContainer.Add(_detailsButton);
            
            _settingsButton = new Button(() => ToggleSettings()) { text = "Settings" };
            _settingsButton.AddToClassList("secondary-button");
            buttonContainer.Add(_settingsButton);
            
            if (_enableProgressionReplay)
            {
                _replayButton = new Button(() => TriggerReplay()) { text = "Replay" };
                _replayButton.AddToClassList("secondary-button");
                buttonContainer.Add(_replayButton);
            }
            
            return buttonContainer;
        }
        
        public async void ShowOfflineProgressionResults(OfflineProgressionSession session)
        {
            if (session == null || !session.Success)
            {
                Debug.LogWarning("[OfflineProgressionUI] Cannot show results for null or failed session");
                return;
            }
            
            _currentSession = session;
            
            if (_enableAutoShow)
            {
                await ShowProgressionScreenAsync();
            }
        }
        
        public async Task ShowProgressionScreenAsync()
        {
            if (_isShowingProgression || _currentSession == null)
                return;
            
            _isShowingProgression = true;
            
            try
            {
                // Update UI content
                await UpdateProgressionSummaryAsync();
                await UpdateEventsDisplayAsync();
                await UpdateNotificationsDisplayAsync();
                
                if (_showDetailedBreakdown)
                {
                    await UpdateDetailedBreakdownAsync();
                }
                
                // Show the screen
                _progressionScreen.style.display = DisplayStyle.Flex;
                
                // Animate in if enabled
                if (_enableAnimations)
                {
                    await AnimateScreenInAsync();
                }
                
                OnProgressionScreenShown?.Invoke();
                
                Debug.Log($"[OfflineProgressionUI] Showing progression results for session {_currentSession.SessionId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OfflineProgressionUI] Error showing progression screen: {ex.Message}");
                _isShowingProgression = false;
            }
        }
        
        private async Task UpdateProgressionSummaryAsync()
        {
            await Task.Delay(10);
            
            _summaryContainer.Clear();
            
            if (_currentSession == null) return;
            
            // Time summary
            var timeSummary = CreateTimeSummary();
            _summaryContainer.Add(timeSummary);
            
            // Resource changes summary
            var resourceSummary = CreateResourceSummary();
            _summaryContainer.Add(resourceSummary);
            
            // Provider results summary
            var providerSummary = CreateProviderSummary();
            _summaryContainer.Add(providerSummary);
        }
        
        private VisualElement CreateTimeSummary()
        {
            var timeContainer = new VisualElement();
            timeContainer.AddToClassList("time-summary");
            
            var offlineHours = _currentSession.OfflineTime.TotalHours;
            var timeLabel = new Label($"Offline Time: {FormatTimeSpan(_currentSession.OfflineTime)}");
            timeLabel.AddToClassList("summary-label");
            timeContainer.Add(timeLabel);
            
            if (_currentSession.AccelerationMultiplier > 1.0f)
            {
                var accelerationLabel = new Label($"Acceleration Bonus: {_currentSession.AccelerationMultiplier:F1}x");
                accelerationLabel.AddToClassList("bonus-label");
                timeContainer.Add(accelerationLabel);
            }
            
            var processingLabel = new Label($"Processing Time: {_currentSession.ProcessingDuration.TotalSeconds:F1}s");
            processingLabel.AddToClassList("info-label");
            timeContainer.Add(processingLabel);
            
            return timeContainer;
        }
        
        private VisualElement CreateResourceSummary()
        {
            var resourceContainer = new VisualElement();
            resourceContainer.AddToClassList("resource-summary");
            
            var title = new Label("Resource Changes");
            title.AddToClassList("summary-title");
            resourceContainer.Add(title);
            
            // Aggregate all resource changes from providers
            var totalResourceChanges = new Dictionary<string, float>();
            foreach (var result in _currentSession.ProviderResults)
            {
                if (result.ResourceChanges != null)
                {
                    foreach (var change in result.ResourceChanges)
                    {
                        totalResourceChanges[change.Key] = totalResourceChanges.GetValueOrDefault(change.Key, 0f) + change.Value;
                    }
                }
            }
            
            // Display significant resource changes
            var significantChanges = totalResourceChanges.Where(kvp => Math.Abs(kvp.Value) > 1f).ToList();
            foreach (var change in significantChanges.Take(8)) // Limit display
            {
                var resourceElement = CreateResourceChangeElement(change.Key, change.Value);
                resourceContainer.Add(resourceElement);
            }
            
            if (significantChanges.Count == 0)
            {
                var noChangesLabel = new Label("No significant resource changes");
                noChangesLabel.AddToClassList("no-data-label");
                resourceContainer.Add(noChangesLabel);
            }
            
            return resourceContainer;
        }
        
        private VisualElement CreateResourceChangeElement(string resourceName, float change)
        {
            var resourceElement = new VisualElement();
            resourceElement.AddToClassList("resource-change");
            
            var nameLabel = new Label(FormatResourceName(resourceName));
            nameLabel.AddToClassList("resource-name");
            resourceElement.Add(nameLabel);
            
            var changeLabel = new Label(FormatResourceChange(change));
            changeLabel.AddToClassList(change >= 0 ? "positive-change" : "negative-change");
            resourceElement.Add(changeLabel);
            
            return resourceElement;
        }
        
        private VisualElement CreateProviderSummary()
        {
            var providerContainer = new VisualElement();
            providerContainer.AddToClassList("provider-summary");
            
            var title = new Label("System Activity");
            title.AddToClassList("summary-title");
            providerContainer.Add(title);
            
            var successfulProviders = _currentSession.ProviderResults.Count(r => r.Success);
            var totalProviders = _currentSession.ProviderResults.Count;
            
            var statusLabel = new Label($"{successfulProviders}/{totalProviders} systems processed successfully");
            statusLabel.AddToClassList("provider-status");
            providerContainer.Add(statusLabel);
            
            // Show brief provider summaries
            foreach (var result in _currentSession.ProviderResults.Take(4))
            {
                var providerElement = CreateProviderSummaryElement(result);
                providerContainer.Add(providerElement);
            }
            
            return providerContainer;
        }
        
        private VisualElement CreateProviderSummaryElement(OfflineProgressionResult result)
        {
            var element = new VisualElement();
            element.AddToClassList("provider-element");
            
            var nameLabel = new Label(FormatProviderName(result.ProviderId));
            nameLabel.AddToClassList("provider-name");
            element.Add(nameLabel);
            
            var statusIcon = new VisualElement();
            statusIcon.AddToClassList(result.Success ? "success-icon" : "error-icon");
            element.Add(statusIcon);
            
            if (!result.Success && !string.IsNullOrEmpty(result.ErrorMessage))
            {
                var errorLabel = new Label(result.ErrorMessage);
                errorLabel.AddToClassList("error-message");
                element.Add(errorLabel);
            }
            
            return element;
        }
        
        private async Task UpdateEventsDisplayAsync()
        {
            await Task.Delay(5);
            
            _eventsContainer.Clear();
            _displayedEvents.Clear();
            
            if (_currentSession?.ProviderResults == null) return;
            
            // Collect all events from providers
            var allEvents = new List<OfflineProgressionEvent>();
            foreach (var result in _currentSession.ProviderResults)
            {
                if (result.Events != null)
                {
                    allEvents.AddRange(result.Events);
                }
            }
            
            // Sort by priority and timestamp
            var sortedEvents = allEvents
                .OrderByDescending(e => e.Priority)
                .ThenByDescending(e => e.Timestamp)
                .Take(_maxEventsToShow)
                .ToList();
            
            if (_groupSimilarEvents)
            {
                sortedEvents = GroupSimilarEvents(sortedEvents);
            }
            
            // Create event elements
            var eventsTitle = new Label("Notable Events");
            eventsTitle.AddToClassList("events-title");
            _eventsContainer.Add(eventsTitle);
            
            foreach (var eventItem in sortedEvents)
            {
                var eventElement = CreateEventElement(eventItem);
                _eventsContainer.Add(eventElement);
                _displayedEvents.Add(eventItem);
            }
            
            if (sortedEvents.Count == 0)
            {
                var noEventsLabel = new Label("No significant events occurred");
                noEventsLabel.AddToClassList("no-data-label");
                _eventsContainer.Add(noEventsLabel);
            }
        }
        
        private List<OfflineProgressionEvent> GroupSimilarEvents(List<OfflineProgressionEvent> events)
        {
            var groupedEvents = new List<OfflineProgressionEvent>();
            var eventGroups = events.GroupBy(e => e.EventType).ToList();
            
            foreach (var group in eventGroups)
            {
                if (group.Count() == 1)
                {
                    groupedEvents.Add(group.First());
                }
                else
                {
                    // Create a grouped event
                    var firstEvent = group.First();
                    var groupedEvent = new OfflineProgressionEvent
                    {
                        EventType = firstEvent.EventType,
                        Title = firstEvent.Title,
                        Description = $"{firstEvent.Description} (and {group.Count() - 1} similar events)",
                        Priority = group.Max(e => e.Priority),
                        Timestamp = group.Max(e => e.Timestamp)
                    };
                    
                    groupedEvents.Add(groupedEvent);
                }
            }
            
            return groupedEvents;
        }
        
        private VisualElement CreateEventElement(OfflineProgressionEvent eventItem)
        {
            var eventElement = new VisualElement();
            eventElement.AddToClassList(_eventNotificationClass);
            eventElement.AddToClassList($"priority-{eventItem.Priority.ToString().ToLower()}");
            
            // Make clickable
            eventElement.RegisterCallback<ClickEvent>(evt => OnEventClicked?.Invoke(eventItem));
            
            var titleLabel = new Label(eventItem.Title);
            titleLabel.AddToClassList("event-title");
            eventElement.Add(titleLabel);
            
            var descriptionLabel = new Label(eventItem.Description);
            descriptionLabel.AddToClassList("event-description");
            eventElement.Add(descriptionLabel);
            
            var timeLabel = new Label(FormatEventTime(eventItem.Timestamp));
            timeLabel.AddToClassList("event-time");
            eventElement.Add(timeLabel);
            
            var priorityIcon = new VisualElement();
            priorityIcon.AddToClassList($"priority-icon-{eventItem.Priority.ToString().ToLower()}");
            eventElement.Add(priorityIcon);
            
            return eventElement;
        }
        
        private async Task UpdateNotificationsDisplayAsync()
        {
            await Task.Delay(5);
            
            _notificationsContainer.Clear();
            _displayedNotifications.Clear();
            
            if (_currentSession?.ProviderResults == null) return;
            
            // Collect all notifications from providers
            var allNotifications = new List<string>();
            foreach (var result in _currentSession.ProviderResults)
            {
                if (result.Notifications != null)
                {
                    allNotifications.AddRange(result.Notifications);
                }
            }
            
            var limitedNotifications = allNotifications.Take(_maxNotificationsToShow).ToList();
            
            if (limitedNotifications.Count > 0)
            {
                var notificationsTitle = new Label("Summary Notifications");
                notificationsTitle.AddToClassList("notifications-title");
                _notificationsContainer.Add(notificationsTitle);
                
                foreach (var notification in limitedNotifications)
                {
                    var notificationElement = CreateNotificationElement(notification);
                    _notificationsContainer.Add(notificationElement);
                    _displayedNotifications.Add(notification);
                }
            }
        }
        
        private VisualElement CreateNotificationElement(string notification)
        {
            var element = new VisualElement();
            element.AddToClassList("notification-item");
            
            var icon = new VisualElement();
            icon.AddToClassList("notification-icon");
            element.Add(icon);
            
            var textLabel = new Label(notification);
            textLabel.AddToClassList("notification-text");
            element.Add(textLabel);
            
            return element;
        }
        
        private async Task UpdateDetailedBreakdownAsync()
        {
            await Task.Delay(10);
            
            _detailsContainer.Clear();
            
            if (_currentSession?.ProviderResults == null) return;
            
            var detailsTitle = new Label("Detailed Breakdown");
            detailsTitle.AddToClassList("details-title");
            _detailsContainer.Add(detailsTitle);
            
            foreach (var result in _currentSession.ProviderResults)
            {
                var providerDetails = CreateProviderDetailsElement(result);
                _detailsContainer.Add(providerDetails);
            }
        }
        
        private VisualElement CreateProviderDetailsElement(OfflineProgressionResult result)
        {
            var element = new VisualElement();
            element.AddToClassList("provider-details");
            
            var header = new Label(FormatProviderName(result.ProviderId));
            header.AddToClassList("provider-details-header");
            element.Add(header);
            
            var processingTime = new Label($"Processing Time: {result.ProcessingDuration.TotalMilliseconds:F0}ms");
            processingTime.AddToClassList("detail-info");
            element.Add(processingTime);
            
            if (result.ResourceChanges?.Count > 0)
            {
                var resourcesLabel = new Label("Resource Changes:");
                resourcesLabel.AddToClassList("detail-section-title");
                element.Add(resourcesLabel);
                
                foreach (var change in result.ResourceChanges.Take(5))
                {
                    var changeElement = new Label($"  {FormatResourceName(change.Key)}: {FormatResourceChange(change.Value)}");
                    changeElement.AddToClassList("detail-item");
                    element.Add(changeElement);
                }
            }
            
            if (result.ValidationWarnings?.Count > 0)
            {
                var warningsLabel = new Label("Warnings:");
                warningsLabel.AddToClassList("detail-section-title");
                element.Add(warningsLabel);
                
                foreach (var warning in result.ValidationWarnings.Take(3))
                {
                    var warningElement = new Label($"  {warning}");
                    warningElement.AddToClassList("warning-item");
                    element.Add(warningElement);
                }
            }
            
            return element;
        }
        
        private async Task AnimateScreenInAsync()
        {
            if (!_enableAnimations) return;
            
            // Simple fade-in animation simulation
            await Task.Delay((int)(_animationDuration * 1000));
        }
        
        private async Task AnimateScreenOutAsync()
        {
            if (!_enableAnimations) return;
            
            // Simple fade-out animation simulation
            await Task.Delay((int)(_animationDuration * 1000));
        }
        
        public async void CloseProgressionScreen()
        {
            if (!_isShowingProgression) return;
            
            // Animate out if enabled
            if (_enableAnimations)
            {
                await AnimateScreenOutAsync();
            }
            
            _progressionScreen.style.display = DisplayStyle.None;
            _isShowingProgression = false;
            
            OnProgressionScreenClosed?.Invoke();
            
            Debug.Log("[OfflineProgressionUI] Progression screen closed");
        }
        
        private void ToggleDetails()
        {
            var isVisible = _detailsContainer.style.display == DisplayStyle.Flex;
            _detailsContainer.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
            _detailsButton.text = isVisible ? "Details" : "Hide Details";
        }
        
        private void ToggleSettings()
        {
            var isVisible = _settingsContainer.style.display == DisplayStyle.Flex;
            _settingsContainer.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
            _settingsButton.text = isVisible ? "Settings" : "Hide Settings";
            
            if (!isVisible)
            {
                CreateSettingsUI();
            }
        }
        
        private void CreateSettingsUI()
        {
            _settingsContainer.Clear();
            
            var settingsTitle = new Label("Offline Progression Settings");
            settingsTitle.AddToClassList("settings-title");
            _settingsContainer.Add(settingsTitle);
            
            // Auto-show setting
            var autoShowToggle = new Toggle("Auto-show progression results")
            {
                value = _enableAutoShow
            };
            autoShowToggle.RegisterValueChangedCallback(evt => _enableAutoShow = evt.newValue);
            _settingsContainer.Add(autoShowToggle);
            
            // Animation setting
            var animationToggle = new Toggle("Enable animations")
            {
                value = _enableAnimations
            };
            animationToggle.RegisterValueChangedCallback(evt => _enableAnimations = evt.newValue);
            _settingsContainer.Add(animationToggle);
            
            // Max events setting
            var maxEventsField = new IntegerField("Max events to show")
            {
                value = _maxEventsToShow
            };
            maxEventsField.RegisterValueChangedCallback(evt => _maxEventsToShow = Math.Max(1, Math.Min(20, evt.newValue)));
            _settingsContainer.Add(maxEventsField);
            
            // Group similar events setting
            var groupEventsToggle = new Toggle("Group similar events")
            {
                value = _groupSimilarEvents
            };
            groupEventsToggle.RegisterValueChangedCallback(evt => _groupSimilarEvents = evt.newValue);
            _settingsContainer.Add(groupEventsToggle);
        }
        
        private void TriggerReplay()
        {
            OnReplayRequested?.Invoke();
        }
        
        public void ShowProgressionSettings()
        {
            ToggleSettings();
        }
        
        public void SetCurrentSession(OfflineProgressionSession session)
        {
            _currentSession = session;
        }
        
        public bool IsShowingProgression()
        {
            return _isShowingProgression;
        }
        
        public List<OfflineProgressionEvent> GetDisplayedEvents()
        {
            return new List<OfflineProgressionEvent>(_displayedEvents);
        }
        
        public List<string> GetDisplayedNotifications()
        {
            return new List<string>(_displayedNotifications);
        }
        
        // Utility Methods
        
        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 1)
            {
                return $"{timeSpan.Days}d {timeSpan.Hours}h {timeSpan.Minutes}m";
            }
            else if (timeSpan.TotalHours >= 1)
            {
                return $"{timeSpan.Hours}h {timeSpan.Minutes}m";
            }
            else
            {
                return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
            }
        }
        
        private string FormatResourceName(string resourceName)
        {
            return resourceName.Replace("_", " ").ToTitleCase();
        }
        
        private string FormatResourceChange(float change)
        {
            var prefix = change >= 0 ? "+" : "";
            return $"{prefix}{change:F1}";
        }
        
        private string FormatProviderName(string providerId)
        {
            return providerId.Replace("_", " ").ToTitleCase();
        }
        
        private string FormatEventTime(DateTime timestamp)
        {
            var timeSince = DateTime.UtcNow - timestamp;
            
            if (timeSince.TotalDays >= 1)
            {
                return $"{timeSince.Days} days ago";
            }
            else if (timeSince.TotalHours >= 1)
            {
                return $"{timeSince.Hours} hours ago";
            }
            else
            {
                return $"{timeSince.Minutes} minutes ago";
            }
        }
        
        private void OnDestroy()
        {
            if (_progressionArchitecture != null)
            {
                _progressionArchitecture.OnOfflineProgressionCompleted -= ShowOfflineProgressionResults;
            }
        }
    }
    
    /// <summary>
    /// Extension method for title case formatting
    /// </summary>
    public static class StringExtensions
    {
        public static string ToTitleCase(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            
            var words = input.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
                }
            }
            
            return string.Join(" ", words);
        }
    }
    
    /// <summary>
    /// Offline progression UI settings
    /// </summary>
    [System.Serializable]
    public class OfflineProgressionUISettings
    {
        public bool EnableAutoShow = true;
        public bool EnableAnimations = true;
        public int MaxEventsToShow = 10;
        public int MaxNotificationsToShow = 5;
        public bool GroupSimilarEvents = true;
        public bool ShowDetailedBreakdown = true;
        public bool EnableProgressionReplay = true;
        public float AnimationDuration = 0.3f;
    }
}