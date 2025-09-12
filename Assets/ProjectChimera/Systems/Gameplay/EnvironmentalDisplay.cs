using UnityEngine;
using UnityEngine.UIElements;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Environment;
using ProjectChimera.Systems.Environment;

namespace ProjectChimera.Systems.Gameplay
{
    /// <summary>
    /// Environmental Display - Shows environmental readings and controls.
    /// Displays current environmental conditions and provides controls
    /// for adjusting temperature, humidity, CO2, and light intensity.
    /// </summary>
    public class EnvironmentalDisplay : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private string _environmentalPanelName = "environmental-panel";

        // UI element references
        private VisualElement _environmentalPanel;
        private Label _temperatureLabel;
        private Label _humidityLabel;
        private Label _co2Label;
        private Label _lightLabel;

        private Slider _temperatureSlider;
        private Slider _humiditySlider;
        private Slider _co2Slider;
        private Slider _lightSlider;

        // Environmental controller reference
        private EnvironmentalController _environmentalController;

        private void Awake()
        {
            InitializeUI();
            FindEnvironmentalController();
        }

        /// <summary>
        /// Initializes the environmental display UI
        /// </summary>
        private void InitializeUI()
        {
            if (_uiDocument == null)
            {
                ChimeraLogger.LogWarning("[EnvironmentalDisplay] No UI document assigned");
                return;
            }

            var root = _uiDocument.rootVisualElement;

            // Find or create environmental panel
            _environmentalPanel = root.Q(_environmentalPanelName);
            if (_environmentalPanel == null)
            {
                _environmentalPanel = new VisualElement();
                _environmentalPanel.name = _environmentalPanelName;
                _environmentalPanel.AddToClassList("environmental-panel");

                CreateEnvironmentalUI();
                root.Add(_environmentalPanel);
            }
            else
            {
                // Find existing UI elements
                FindExistingUIElements();
            }

            // Initially hide the panel
            _environmentalPanel.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Creates the environmental display UI elements
        /// </summary>
        private void CreateEnvironmentalUI()
        {
            // Temperature control
            var tempContainer = CreateControlContainer("Temperature", out _temperatureLabel, out _temperatureSlider);
            _temperatureSlider.lowValue = 15f;
            _temperatureSlider.highValue = 35f;
            _temperatureSlider.value = 25f;
            _temperatureSlider.RegisterValueChangedCallback(evt => OnTemperatureChanged(evt.newValue));

            // Humidity control
            var humidityContainer = CreateControlContainer("Humidity", out _humidityLabel, out _humiditySlider);
            _humiditySlider.lowValue = 30f;
            _humiditySlider.highValue = 80f;
            _humiditySlider.value = 60f;
            _humiditySlider.RegisterValueChangedCallback(evt => OnHumidityChanged(evt.newValue));

            // CO2 control
            var co2Container = CreateControlContainer("CO2 Level", out _co2Label, out _co2Slider);
            _co2Slider.lowValue = 300f;
            _co2Slider.highValue = 1500f;
            _co2Slider.value = 800f;
            _co2Slider.RegisterValueChangedCallback(evt => OnCO2Changed(evt.newValue));

            // Light control
            var lightContainer = CreateControlContainer("Light Intensity", out _lightLabel, out _lightSlider);
            _lightSlider.lowValue = 0f;
            _lightSlider.highValue = 1f;
            _lightSlider.value = 0.8f;
            _lightSlider.RegisterValueChangedCallback(evt => OnLightChanged(evt.newValue));

            // Add reset button
            var resetButton = new Button();
            resetButton.text = "Reset to Optimal";
            resetButton.clicked += OnResetToOptimal;
            resetButton.AddToClassList("reset-button");

            _environmentalPanel.Add(tempContainer);
            _environmentalPanel.Add(humidityContainer);
            _environmentalPanel.Add(co2Container);
            _environmentalPanel.Add(lightContainer);
            _environmentalPanel.Add(resetButton);

            UpdateDisplayValues();
        }

        /// <summary>
        /// Creates a control container with label and slider
        /// </summary>
        private VisualElement CreateControlContainer(string labelText, out Label label, out Slider slider)
        {
            var container = new VisualElement();
            container.AddToClassList("control-container");

            label = new Label();
            label.text = $"{labelText}: --";
            label.AddToClassList("control-label");

            slider = new Slider();
            slider.AddToClassList("control-slider");

            container.Add(label);
            container.Add(slider);

            return container;
        }

        /// <summary>
        /// Finds existing UI elements in the scene
        /// </summary>
        private void FindExistingUIElements()
        {
            _temperatureLabel = _environmentalPanel.Q<Label>("temperature-label");
            _humidityLabel = _environmentalPanel.Q<Label>("humidity-label");
            _co2Label = _environmentalPanel.Q<Label>("co2-label");
            _lightLabel = _environmentalPanel.Q<Label>("light-label");

            _temperatureSlider = _environmentalPanel.Q<Slider>("temperature-slider");
            _humiditySlider = _environmentalPanel.Q<Slider>("humidity-slider");
            _co2Slider = _environmentalPanel.Q<Slider>("co2-slider");
            _lightSlider = _environmentalPanel.Q<Slider>("light-slider");
        }

        /// <summary>
        /// Finds the environmental controller in the scene
        /// </summary>
        private void FindEnvironmentalController()
        {
            // Primary: Try ServiceContainer resolution
            if (ServiceContainerFactory.Instance.TryResolve<Systems.Environment.EnvironmentalController>(out var serviceController))
            {
                _environmentalController = serviceController;
                ChimeraLogger.Log("[EnvironmentalDisplay] Using EnvironmentalController from ServiceContainer");
            }
            else
            {
                // Fallback: Scene discovery + auto-registration
                _environmentalController = UnityEngine.Object.FindObjectOfType<Systems.Environment.EnvironmentalController>();
                if (_environmentalController != null)
                {
                    ServiceContainerFactory.Instance.RegisterInstance<Systems.Environment.EnvironmentalController>(_environmentalController);
                    ChimeraLogger.Log("[EnvironmentalDisplay] EnvironmentalController registered in ServiceContainer for system-wide access");
                }
            }

            if (_environmentalController == null)
            {
                ChimeraLogger.LogWarning("[EnvironmentalDisplay] No EnvironmentalController found in scene");
            }
        }

        /// <summary>
        /// Shows the environmental display panel
        /// </summary>
        public void ShowEnvironmentalPanel()
        {
            if (_environmentalPanel != null)
            {
                _environmentalPanel.style.display = DisplayStyle.Flex;
                UpdateDisplayValues();
            }
        }

        /// <summary>
        /// Hides the environmental display panel
        /// </summary>
        public void HideEnvironmentalPanel()
        {
            if (_environmentalPanel != null)
            {
                _environmentalPanel.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// Updates display values from environmental controller
        /// </summary>
        public void UpdateDisplayValues()
        {
            if (_environmentalController == null)
                return;

            var conditions = _environmentalController.GetCurrentConditions();

            if (_temperatureLabel != null)
                _temperatureLabel.text = $"Temperature: {conditions.Temperature:F1}°C";
            if (_humidityLabel != null)
                _humidityLabel.text = $"Humidity: {conditions.Humidity:F1}%";
            if (_co2Label != null)
                _co2Label.text = $"CO2 Level: {conditions.CO2Level:F0}ppm";
            if (_lightLabel != null)
                _lightLabel.text = $"Light Intensity: {conditions.LightIntensity:F2}";

            // Update slider values
            if (_temperatureSlider != null) _temperatureSlider.value = conditions.Temperature;
            if (_humiditySlider != null) _humiditySlider.value = conditions.Humidity;
            if (_co2Slider != null) _co2Slider.value = conditions.CO2Level;
            if (_lightSlider != null) _lightSlider.value = conditions.LightIntensity;
        }

        /// <summary>
        /// Handles temperature slider changes
        /// </summary>
        private void OnTemperatureChanged(float value)
        {
            if (_environmentalController != null)
            {
                _environmentalController.SetTemperature(value);
                UpdateDisplayValues();
                ChimeraLogger.Log($"[EnvironmentalDisplay] Temperature set to {value:F1}°C");
            }
        }

        /// <summary>
        /// Handles humidity slider changes
        /// </summary>
        private void OnHumidityChanged(float value)
        {
            if (_environmentalController != null)
            {
                _environmentalController.SetHumidity(value);
                UpdateDisplayValues();
                ChimeraLogger.Log($"[EnvironmentalDisplay] Humidity set to {value:F1}%");
            }
        }

        /// <summary>
        /// Handles CO2 slider changes
        /// </summary>
        private void OnCO2Changed(float value)
        {
            if (_environmentalController != null)
            {
                _environmentalController.SetCO2Level(value);
                UpdateDisplayValues();
                ChimeraLogger.Log($"[EnvironmentalDisplay] CO2 level set to {value:F0}ppm");
            }
        }

        /// <summary>
        /// Handles light slider changes
        /// </summary>
        private void OnLightChanged(float value)
        {
            if (_environmentalController != null)
            {
                _environmentalController.SetLightIntensity(value);
                UpdateDisplayValues();
                ChimeraLogger.Log($"[EnvironmentalDisplay] Light intensity set to {value:F2}");
            }
        }

        /// <summary>
        /// Handles reset to optimal button
        /// </summary>
        private void OnResetToOptimal()
        {
            if (_environmentalController != null)
            {
                _environmentalController.ResetToOptimal();
                UpdateDisplayValues();
                ChimeraLogger.Log("[EnvironmentalDisplay] Reset to optimal environmental conditions");
            }
        }

        /// <summary>
        /// Gets environmental recommendations
        /// </summary>
        public string GetEnvironmentalRecommendations()
        {
            if (_environmentalController != null)
            {
                return _environmentalController.GetEnvironmentalRecommendations();
            }
            return "Environmental controller not available";
        }

        /// <summary>
        /// Checks if the environmental panel is visible
        /// </summary>
        public bool IsPanelVisible()
        {
            return _environmentalPanel != null &&
                   _environmentalPanel.style.display == DisplayStyle.Flex;
        }
    }
}
