using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Systems.Analytics;

namespace ProjectChimera.UI.Panels.Components
{
    /// <summary>
    /// Handles configuration controls and filtering for the analytics dashboard in Project Chimera's game.
    /// Manages time range selection, facility filtering, and user preferences for cannabis cultivation analytics.
    /// </summary>
    public class DataDashboardConfigurationManager : MonoBehaviour
    {
        [Header("Configuration Settings")]
        [SerializeField] private bool _enableDebugLogging = false;
        [SerializeField] private string _defaultTimeRange = "Last 24 Hours";
        [SerializeField] private string _defaultFacility = "All Facilities";

        // UI Container reference
        private VisualElement _filtersContainer;

        // Filter controls
        private DropdownField _timeRangeDropdown;
        private DropdownField _facilityDropdown;
        private Button _refreshButton;
        private VisualElement _quickRangeContainer;

        // Configuration state
        private TimeRange _selectedTimeRange;
        private string _selectedFacility;
        private IAnalyticsService _analyticsService;

        // Events
        public System.Action<TimeRange> OnTimeRangeChanged;
        public System.Action<string> OnFacilityChanged;
        public System.Action OnRefreshRequested;
        public System.Action<string> OnQuickTimeRangeSelected;

        // Properties
        public TimeRange SelectedTimeRange => _selectedTimeRange;
        public string SelectedFacility => _selectedFacility;
        public bool HasValidConfiguration => _timeRangeDropdown != null && _facilityDropdown != null;

        public void Initialize(VisualElement filtersContainer, IAnalyticsService analyticsService)
        {
            _filtersContainer = filtersContainer;
            _analyticsService = analyticsService;

            CreateFilterControls();
            SetDefaultValues();

            LogInfo("Configuration manager initialized for game dashboard");
        }

        #region Filter Control Creation

        private void CreateFilterControls()
        {
            CreateTimeRangeFilter();
            CreateFacilityFilter();
            CreateQuickTimeRangeButtons();
            CreateRefreshButton();
        }

        private void CreateTimeRangeFilter()
        {
            _timeRangeDropdown = new DropdownField("Time Range:");
            _timeRangeDropdown.choices = GetTimeRangeChoices();
            _timeRangeDropdown.value = _defaultTimeRange;
            _timeRangeDropdown.RegisterValueChangedCallback(OnTimeRangeDropdownChanged);
            _filtersContainer.Add(_timeRangeDropdown);

            LogInfo("Time range filter created with game-specific options");
        }

        private void CreateFacilityFilter()
        {
            _facilityDropdown = new DropdownField("Facility:");
            PopulateFacilityDropdown();
            _facilityDropdown.RegisterValueChangedCallback(OnFacilityDropdownChanged);
            _filtersContainer.Add(_facilityDropdown);

            LogInfo("Facility filter created for cannabis cultivation facilities");
        }

        private void CreateQuickTimeRangeButtons()
        {
            _quickRangeContainer = new VisualElement();
            _quickRangeContainer.name = "quick-range-container";
            _quickRangeContainer.AddToClassList("quick-range-buttons");
            _quickRangeContainer.style.flexDirection = FlexDirection.Row;
            _quickRangeContainer.style.marginTop = 5;
            _quickRangeContainer.style.marginBottom = 5;

            // Create quick access buttons for common time ranges in cannabis cultivation
            var quickRanges = new[]
            {
                ("1H", "Last Hour"),
                ("24H", "Last 24 Hours"),
                ("1W", "Last Week"),
                ("1M", "Last Month")
            };

            foreach (var (buttonText, timeRangeValue) in quickRanges)
            {
                var quickButton = new Button(() => SetQuickTimeRange(timeRangeValue))
                {
                    text = buttonText
                };
                quickButton.AddToClassList("quick-range-button");
                ApplyQuickButtonStyling(quickButton);
                
                _quickRangeContainer.Add(quickButton);
            }

            _filtersContainer.Add(_quickRangeContainer);
        }

        private void CreateRefreshButton()
        {
            _refreshButton = new Button(HandleRefreshRequested) { text = "Refresh Data" };
            _refreshButton.AddToClassList("dashboard-refresh-button");
            _refreshButton.tooltip = "Refresh cannabis cultivation analytics data";
            _filtersContainer.Add(_refreshButton);
        }

        private void ApplyQuickButtonStyling(Button button)
        {
            button.style.marginRight = 5;
            button.style.paddingLeft = 8;
            button.style.paddingRight = 8;
            button.style.paddingTop = 4;
            button.style.paddingBottom = 4;
            button.style.fontSize = 11;
        }

        #endregion

        #region Facility Management

        private void PopulateFacilityDropdown()
        {
            var facilities = GetAvailableFacilities();
            _facilityDropdown.choices = facilities;
            _facilityDropdown.value = facilities.Count > 0 ? facilities[0] : _defaultFacility;
        }

        private List<string> GetAvailableFacilities()
        {
            if (_analyticsService is AnalyticsManager analyticsManager)
            {
                var facilities = analyticsManager.GetAvailableFacilities();
                if (facilities.Count > 0)
                {
                    LogInfo($"Found {facilities.Count} cannabis cultivation facilities");
                    return facilities;
                }
            }

            // Fallback facilities for cannabis cultivation game
            var fallbackFacilities = new List<string> 
            { 
                "All Facilities", 
                "Main Cultivation Facility", 
                "Secondary Growing Facility",
                "Indoor Hydroponic Lab",
                "Outdoor Growing Plots"
            };
            
            LogWarning("Using fallback facility list - AnalyticsManager not available");
            return fallbackFacilities;
        }

        public void RefreshFacilityList()
        {
            PopulateFacilityDropdown();
            LogInfo("Facility list refreshed for cannabis cultivation facilities");
        }

        #endregion

        #region Time Range Management

        private List<string> GetTimeRangeChoices()
        {
            return new List<string>
            {
                "Last Hour",
                "Last 6 Hours",
                "Last 12 Hours",
                "Last 24 Hours",
                "Last 3 Days",
                "Last Week",
                "Last 2 Weeks",
                "Last Month",
                "Last 3 Months",
                "Last 6 Months",
                "Last Year",
                "All Time"
            };
        }

        private TimeRange ConvertStringToTimeRange(string timeRangeString)
        {
            return timeRangeString switch
            {
                "Last Hour" => TimeRange.LastHour,
                "Last 6 Hours" => TimeRange.Last6Hours,
                "Last 12 Hours" => TimeRange.Last12Hours,
                "Last 24 Hours" => TimeRange.Last24Hours,
                "Last 3 Days" => TimeRange.Last3Days,
                "Last Week" => TimeRange.LastWeek,
                "Last 2 Weeks" => TimeRange.Last2Weeks,
                "Last Month" => TimeRange.LastMonth,
                "Last 3 Months" => TimeRange.Last3Months,
                "Last 6 Months" => TimeRange.Last6Months,
                "Last Year" => TimeRange.LastYear,
                "All Time" => TimeRange.AllTime,
                _ => TimeRange.Last24Hours
            };
        }

        private string ConvertTimeRangeToString(TimeRange timeRange)
        {
            return timeRange switch
            {
                TimeRange.LastHour => "Last Hour",
                TimeRange.Last6Hours => "Last 6 Hours",
                TimeRange.Last12Hours => "Last 12 Hours",
                TimeRange.Last24Hours => "Last 24 Hours",
                TimeRange.Last3Days => "Last 3 Days",
                TimeRange.LastWeek => "Last Week",
                TimeRange.Last2Weeks => "Last 2 Weeks",
                TimeRange.LastMonth => "Last Month",
                TimeRange.Last3Months => "Last 3 Months",
                TimeRange.Last6Months => "Last 6 Months",
                TimeRange.LastYear => "Last Year",
                TimeRange.AllTime => "All Time",
                _ => "Last 24 Hours"
            };
        }

        #endregion

        #region Event Handlers

        private void OnTimeRangeDropdownChanged(ChangeEvent<string> evt)
        {
            var newTimeRange = ConvertStringToTimeRange(evt.newValue);
            _selectedTimeRange = newTimeRange;

            OnTimeRangeChanged?.Invoke(newTimeRange);

            LogInfo($"Time range changed to: {evt.newValue} for cannabis cultivation analytics");
        }

        private void OnFacilityDropdownChanged(ChangeEvent<string> evt)
        {
            _selectedFacility = evt.newValue;

            // Update analytics service filter
            if (_analyticsService is AnalyticsManager analyticsManager)
            {
                analyticsManager.SetFacilityFilter(evt.newValue);
            }

            OnFacilityChanged?.Invoke(evt.newValue);

            LogInfo($"Facility filter changed to: {evt.newValue}");
        }

        private void SetQuickTimeRange(string timeRangeValue)
        {
            if (_timeRangeDropdown != null)
            {
                _timeRangeDropdown.value = timeRangeValue;
                OnQuickTimeRangeSelected?.Invoke(timeRangeValue);
                
                LogInfo($"Quick time range selected: {timeRangeValue}");
            }
        }

        private void HandleRefreshRequested()
        {
            OnRefreshRequested?.Invoke();
            LogInfo("Data refresh requested from configuration panel");
        }

        #endregion

        #region Configuration State Management

        private void SetDefaultValues()
        {
            _selectedTimeRange = ConvertStringToTimeRange(_defaultTimeRange);
            _selectedFacility = _defaultFacility;
        }

        public void SetTimeRangeFilter(TimeRange timeRange)
        {
            string timeRangeString = ConvertTimeRangeToString(timeRange);
            
            if (_timeRangeDropdown != null)
            {
                var choices = _timeRangeDropdown.choices;
                if (choices.Contains(timeRangeString))
                {
                    _timeRangeDropdown.value = timeRangeString;
                    _selectedTimeRange = timeRange;
                }
                else
                {
                    LogWarning($"Time range '{timeRangeString}' not found in dropdown choices");
                }
            }
        }

        public void SetFacilityFilter(string facilityName)
        {
            if (_facilityDropdown != null)
            {
                var choices = _facilityDropdown.choices;
                if (choices.Contains(facilityName))
                {
                    _facilityDropdown.value = facilityName;
                    _selectedFacility = facilityName;
                }
                else
                {
                    LogWarning($"Facility '{facilityName}' not found in dropdown choices");
                }
            }

            // Update analytics service filter directly
            if (_analyticsService is AnalyticsManager analyticsManager)
            {
                analyticsManager.SetFacilityFilter(facilityName);
            }
        }

        #endregion

        #region Information Methods

        public string GetTimeRangeDescription()
        {
            return _selectedTimeRange switch
            {
                TimeRange.LastHour => "Cannabis data from the last hour",
                TimeRange.Last6Hours => "Cannabis data from the last 6 hours",
                TimeRange.Last12Hours => "Cannabis data from the last 12 hours",
                TimeRange.Last24Hours => "Cannabis data from the last 24 hours",
                TimeRange.Last3Days => "Cannabis data from the last 3 days",
                TimeRange.LastWeek => "Cannabis data from the last week",
                TimeRange.Last2Weeks => "Cannabis data from the last 2 weeks",
                TimeRange.LastMonth => "Cannabis data from the last month",
                TimeRange.Last3Months => "Cannabis data from the last 3 months",
                TimeRange.Last6Months => "Cannabis data from the last 6 months",
                TimeRange.LastYear => "Cannabis data from the last year",
                TimeRange.AllTime => "All available cannabis cultivation data",
                _ => "Current cannabis cultivation data view"
            };
        }

        public string GetAggregationInfo()
        {
            return _selectedTimeRange switch
            {
                TimeRange.LastHour or TimeRange.Last6Hours => "Real-time cultivation data points",
                TimeRange.Last12Hours => "5-minute cultivation averages",
                TimeRange.Last24Hours => "15-minute cultivation averages",
                TimeRange.Last3Days => "1-hour cultivation averages",
                TimeRange.LastWeek => "2-hour cultivation averages",
                TimeRange.Last2Weeks => "4-hour cultivation averages",
                TimeRange.LastMonth => "8-hour cultivation averages",
                TimeRange.Last3Months => "Daily cultivation averages",
                TimeRange.Last6Months => "2-day cultivation averages",
                TimeRange.LastYear => "Weekly cultivation averages",
                TimeRange.AllTime => "Monthly cultivation averages",
                _ => "Processed cultivation data"
            };
        }

        public string GetDashboardTitle()
        {
            var timeRangeText = ConvertTimeRangeToString(_selectedTimeRange);
            
            return _selectedFacility == "All Facilities" 
                ? $"Cannabis Analytics Dashboard - All Facilities ({timeRangeText})"
                : $"Cannabis Analytics Dashboard - {_selectedFacility} ({timeRangeText})";
        }

        #endregion

        #region UI State Management

        public void SetRefreshButtonEnabled(bool enabled)
        {
            if (_refreshButton != null)
            {
                _refreshButton.SetEnabled(enabled);
                _refreshButton.text = enabled ? "Refresh Data" : "Refreshing...";
            }
        }

        public void SetQuickRangeButtonsEnabled(bool enabled)
        {
            if (_quickRangeContainer != null)
            {
                _quickRangeContainer.SetEnabled(enabled);
                _quickRangeContainer.style.opacity = enabled ? 1.0f : 0.5f;
            }
        }

        public void HighlightActiveQuickButton(string timeRangeValue)
        {
            if (_quickRangeContainer == null) return;

            // Remove highlight from all buttons
            var buttons = _quickRangeContainer.Query<Button>().ToList();
            foreach (var button in buttons)
            {
                button.RemoveFromClassList("quick-range-active");
            }

            // Add highlight to active button
            var activeButton = buttons.FirstOrDefault(b => 
            {
                return timeRangeValue switch
                {
                    "Last Hour" => b.text == "1H",
                    "Last 24 Hours" => b.text == "24H",
                    "Last Week" => b.text == "1W",
                    "Last Month" => b.text == "1M",
                    _ => false
                };
            });

            activeButton?.AddToClassList("quick-range-active");
        }

        #endregion

        #region Validation and Error Handling

        public bool ValidateConfiguration()
        {
            bool isValid = true;
            var errors = new List<string>();

            if (_timeRangeDropdown == null)
            {
                errors.Add("Time range dropdown not initialized");
                isValid = false;
            }

            if (_facilityDropdown == null)
            {
                errors.Add("Facility dropdown not initialized");
                isValid = false;
            }

            if (_facilityDropdown?.choices.Count == 0)
            {
                errors.Add("No facilities available for selection");
                isValid = false;
            }

            if (!isValid)
            {
                LogWarning($"Configuration validation failed: {string.Join(", ", errors)}");
            }

            return isValid;
        }

        #endregion

        #region Public API

        public void UpdateAnalyticsService(IAnalyticsService newService)
        {
            _analyticsService = newService;
            RefreshFacilityList();
            LogInfo("Analytics service updated in configuration manager");
        }

        public Dictionary<string, object> GetCurrentConfiguration()
        {
            return new Dictionary<string, object>
            {
                ["TimeRange"] = _selectedTimeRange,
                ["Facility"] = _selectedFacility,
                ["TimeRangeString"] = ConvertTimeRangeToString(_selectedTimeRange),
                ["Description"] = GetTimeRangeDescription(),
                ["AggregationInfo"] = GetAggregationInfo(),
                ["Title"] = GetDashboardTitle()
            };
        }

        public void ResetToDefaults()
        {
            SetTimeRangeFilter(ConvertStringToTimeRange(_defaultTimeRange));
            SetFacilityFilter(_defaultFacility);
            LogInfo("Configuration reset to default values");
        }

        #endregion

        private void LogInfo(string message)
        {
            if (_enableDebugLogging)
                Debug.Log($"[DataDashboardConfiguration] {message}");
        }

        private void LogWarning(string message)
        {
            if (_enableDebugLogging)
                Debug.LogWarning($"[DataDashboardConfiguration] {message}");
        }
    }
}