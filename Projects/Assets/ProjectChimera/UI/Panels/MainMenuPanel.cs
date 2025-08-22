using UnityEngine;
using UnityEngine.UIElements;
using ProjectChimera.Core;
using ProjectChimera.UI.Core;
using ProjectChimera.UI.Events;
using ProjectChimera.UI.Components;
using ProjectChimera.Core.Events;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Main menu panel for Project Chimera.
    /// Provides entry point navigation and game setup options.
    /// </summary>
    public class MainMenuPanel : UIPanel
    {
        [Header("Menu Events")]
        [SerializeField] private SimpleGameEventSO _onNewGameClicked;
        [SerializeField] private SimpleGameEventSO _onContinueGameClicked;
        [SerializeField] private SimpleGameEventSO _onSettingsClicked;
        [SerializeField] private SimpleGameEventSO _onExitGameClicked;
        [SerializeField] private UIButtonClickEventSO _onMenuButtonClicked;
        
        [Header("Audio")]
        [SerializeField] private AudioClip _buttonHoverSound;
        [SerializeField] private AudioClip _buttonClickSound;
        
        // UI Elements
        private VisualElement _mainContainer;
        private VisualElement _menuContainer;
        private VisualElement _logoContainer;
        private Label _titleLabel;
        private Label _versionLabel;
        private Button _newGameButton;
        private Button _continueButton;
        private Button _tutorialButton;
        private Button _settingsButton;
        private Button _creditsButton;
        private Button _exitButton;
        private VisualElement _backgroundContainer;
        
        // Menu state
        private bool _hasSaveData = false;
        private string _gameVersion = "";
        
        protected override void SetupUIElements()
        {
            base.SetupUIElements();
            
            CreateMainLayout();
            CreateLogoSection();
            CreateMenuButtons();
            CreateVersionInfo();
            SetupBackgroundEffects();
            
            CheckForSaveData();
            UpdateMenuState();
        }
        
        protected override void BindUIEvents()
        {
            base.BindUIEvents();
            
            // Bind button events
            _newGameButton?.RegisterCallback<ClickEvent>(OnNewGameClicked);
            _continueButton?.RegisterCallback<ClickEvent>(OnContinueGameClicked);
            _tutorialButton?.RegisterCallback<ClickEvent>(OnTutorialClicked);
            _settingsButton?.RegisterCallback<ClickEvent>(OnSettingsClicked);
            _creditsButton?.RegisterCallback<ClickEvent>(OnCreditsClicked);
            _exitButton?.RegisterCallback<ClickEvent>(OnExitGameClicked);
            
            // Setup hover effects and animations
            SetupButtonEffects();
        }
        
        /// <summary>
        /// Create the main layout structure
        /// </summary>
        private void CreateMainLayout()
        {
            _rootElement.Clear();
            
            // Background container
            _backgroundContainer = new VisualElement();
            _backgroundContainer.name = "background-container";
            _backgroundContainer.style.position = Position.Absolute;
            _backgroundContainer.style.top = 0;
            _backgroundContainer.style.left = 0;
            _backgroundContainer.style.right = 0;
            _backgroundContainer.style.bottom = 0;
            
            // Main container
            _mainContainer = new VisualElement();
            _mainContainer.name = "main-container";
            _mainContainer.style.flexGrow = 1;
            _mainContainer.style.justifyContent = Justify.Center;
            _mainContainer.style.alignItems = Align.Center;
            _mainContainer.style.paddingTop = 50;
            _mainContainer.style.paddingBottom = 50;
            
            // Menu container
            _menuContainer = new VisualElement();
            _menuContainer.name = "menu-container";
            _menuContainer.style.alignItems = Align.Center;
            _menuContainer.style.justifyContent = Justify.Center;
            _menuContainer.style.minWidth = 400;
            _menuContainer.style.maxWidth = 600;
            
            _rootElement.Add(_backgroundContainer);
            _rootElement.Add(_mainContainer);
            _mainContainer.Add(_menuContainer);
        }
        
        /// <summary>
        /// Create the logo and title section
        /// </summary>
        private void CreateLogoSection()
        {
            _logoContainer = new VisualElement();
            _logoContainer.name = "logo-container";
            _logoContainer.style.alignItems = Align.Center;
            _logoContainer.style.marginBottom = 40;
            
            // Game title
            _titleLabel = new Label("PROJECT CHIMERA");
            _titleLabel.name = "game-title";
            _titleLabel.style.fontSize = 48;
            // _titleLabel.style.color = _uiManager.DesignSystem.ColorPalette.PrimaryGreen;
            _titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _titleLabel.style.marginBottom = 8;
            _titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _titleLabel.style.textShadow = new TextShadow
            {
                offset = new Vector2(2, 2),
                blurRadius = 4,
                color = new Color(0, 0, 0, 0.5f)
            };
            
            // Subtitle
            var subtitleLabel = new Label("Advanced Cannabis Cultivation Simulation");
            subtitleLabel.name = "game-subtitle";
            subtitleLabel.style.fontSize = 16;
            // subtitleLabel.style.color = _uiManager.DesignSystem.ColorPalette.TextSecondary;
            subtitleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            subtitleLabel.style.marginBottom = 20;
            
            _logoContainer.Add(_titleLabel);
            _logoContainer.Add(subtitleLabel);
            _menuContainer.Add(_logoContainer);
        }
        
        /// <summary>
        /// Create the main menu buttons
        /// </summary>
        private void CreateMenuButtons()
        {
            var buttonContainer = new VisualElement();
            buttonContainer.name = "button-container";
            buttonContainer.style.alignItems = Align.Stretch;
            buttonContainer.style.width = 300;
            
            // New Game button
            _newGameButton = CreateMenuButton("New Game", "Start a new cultivation business");
            // _uiManager.ApplyDesignSystemStyle(_newGameButton, UIStyleToken.PrimaryButton);
            
            // Continue button
            _continueButton = CreateMenuButton("Continue", "Continue your existing game");
            // _uiManager.ApplyDesignSystemStyle(_continueButton, UIStyleToken.PrimaryButton);
            
            // Tutorial button
            _tutorialButton = CreateMenuButton("Tutorial", "Learn the fundamentals of cannabis cultivation");
            // _uiManager.ApplyDesignSystemStyle(_tutorialButton, UIStyleToken.SecondaryButton);
            
            // Settings button
            _settingsButton = CreateMenuButton("Settings", "Configure game options and preferences");
            // _uiManager.ApplyDesignSystemStyle(_settingsButton, UIStyleToken.SecondaryButton);
            
            // Credits button
            _creditsButton = CreateMenuButton("Credits", "View game credits and acknowledgments");
            // _uiManager.ApplyDesignSystemStyle(_creditsButton, UIStyleToken.SecondaryButton);
            
            // Exit button
            _exitButton = CreateMenuButton("Exit", "Close the application");
            // _uiManager.ApplyDesignSystemStyle(_exitButton, UIStyleToken.SecondaryButton);
            
            buttonContainer.Add(_newGameButton);
            buttonContainer.Add(_continueButton);
            buttonContainer.Add(_tutorialButton);
            buttonContainer.Add(_settingsButton);
            buttonContainer.Add(_creditsButton);
            buttonContainer.Add(_exitButton);
            
            _menuContainer.Add(buttonContainer);
        }
        
        /// <summary>
        /// Create a menu button with consistent styling
        /// </summary>
        private Button CreateMenuButton(string text, string tooltip)
        {
            var button = new Button();
            button.name = text.ToLower().Replace(" ", "-") + "-button";
            button.text = text;
            button.style.height = 50;
            button.style.marginBottom = 12;
            button.style.fontSize = 16;
            
            // Add tooltip
            if (!string.IsNullOrEmpty(tooltip))
            {
                button.SetupTooltip(tooltip, _rootElement);
            }
            
            return button;
        }
        
        /// <summary>
        /// Create version information display
        /// </summary>
        private void CreateVersionInfo()
        {
            var versionContainer = new VisualElement();
            versionContainer.name = "version-container";
            versionContainer.style.position = Position.Absolute;
            versionContainer.style.bottom = 20;
            versionContainer.style.right = 20;
            versionContainer.style.alignItems = Align.FlexEnd;
            
            // Version label
            _gameVersion = Application.version;
            _versionLabel = new Label($"Version {_gameVersion}");
            _versionLabel.name = "version-label";
            _versionLabel.style.fontSize = 12;
            // _versionLabel.style.color = _uiManager.DesignSystem.ColorPalette.TextDisabled;
            
            // Build info (debug builds only)
            if (Debug.isDebugBuild)
            {
                var buildLabel = new Label("DEBUG BUILD");
                buildLabel.name = "build-label";
                buildLabel.style.fontSize = 10;
                // buildLabel.style.color = _uiManager.DesignSystem.ColorPalette.Warning;
                buildLabel.style.marginTop = 2;
                versionContainer.Add(buildLabel);
            }
            
            versionContainer.Add(_versionLabel);
            _rootElement.Add(versionContainer);
        }
        
        /// <summary>
        /// Setup background visual effects
        /// </summary>
        private void SetupBackgroundEffects()
        {
            // Background gradient
            // _backgroundContainer.style.backgroundColor = _uiManager.DesignSystem.ColorPalette.BackgroundDark;
            
            // Add subtle pattern or animation if needed
            // This could be enhanced with animated particles or background images
        }
        
        /// <summary>
        /// Setup button hover and click effects
        /// </summary>
        private void SetupButtonEffects()
        {
            var buttons = new[] { _newGameButton, _continueButton, _tutorialButton, _settingsButton, _creditsButton, _exitButton };
            
            foreach (var button in buttons)
            {
                if (button == null) continue;
                
                // Add click animation
                button.AddClickAnimation();
                
                // Add hover effects
                // var hoverColor = _uiManager.DesignSystem.ColorPalette.InteractiveHover;
                var hoverColor = new Color(0.67f, 0.47f, 1f, 1f); // Placeholder hover color
                var normalColor = button.style.backgroundColor.value;
                button.AddHoverEffects(hoverColor, normalColor);
                
                // Audio feedback
                button.RegisterCallback<MouseEnterEvent>(evt => PlayHoverSound());
                button.RegisterCallback<ClickEvent>(evt => PlayClickSound());
            }
        }
        
        /// <summary>
        /// Check for existing save data
        /// </summary>
        private void CheckForSaveData()
        {
            // This would integrate with the save system
            // For now, check if save files exist
            _hasSaveData = System.IO.File.Exists(Application.persistentDataPath + "/save.dat");
        }
        
        /// <summary>
        /// Update menu state based on save data
        /// </summary>
        private void UpdateMenuState()
        {
            if (_continueButton != null)
            {
                _continueButton.SetEnabled(_hasSaveData);
                
                if (!_hasSaveData)
                {
                    _continueButton.style.opacity = 0.5f;
                    // _continueButton.style.color = _uiManager.DesignSystem.ColorPalette.TextDisabled;
                }
            }
        }
        
        /// <summary>
        /// Handle new game button click
        /// </summary>
        private void OnNewGameClicked(ClickEvent evt)
        {
            LogInfo("New Game clicked");
            
            // Raise events
            _onNewGameClicked?.Raise();
            _onMenuButtonClicked?.RaiseButtonClick("new-game", PanelId, evt.position);
            
            // Transition to game setup or directly to gameplay
            // _uiManager.SetUIState(UIState.Gameplay);
        }
        
        /// <summary>
        /// Handle continue game button click
        /// </summary>
        private void OnContinueGameClicked(ClickEvent evt)
        {
            if (!_hasSaveData) return;
            
            LogInfo("Continue Game clicked");
            
            // Raise events
            _onContinueGameClicked?.Raise();
            _onMenuButtonClicked?.RaiseButtonClick("continue-game", PanelId, evt.position);
            
            // Load save data and transition to gameplay
            // _uiManager.SetUIState(UIState.Gameplay);
        }
        
        /// <summary>
        /// Handle tutorial button click
        /// </summary>
        private void OnTutorialClicked(ClickEvent evt)
        {
            LogInfo("Tutorial clicked");
            
            _onMenuButtonClicked?.RaiseButtonClick("tutorial", PanelId, evt.position);
            
            // Start tutorial system
            // _uiManager.SetUIState(UIState.Tutorial);
        }
        
        /// <summary>
        /// Handle settings button click
        /// </summary>
        private void OnSettingsClicked(ClickEvent evt)
        {
            LogInfo("Settings clicked");
            
            _onSettingsClicked?.Raise();
            _onMenuButtonClicked?.RaiseButtonClick("settings", PanelId, evt.position);
            
            // Show settings panel
            // _uiManager.ShowPanel("settings-menu");
        }
        
        /// <summary>
        /// Handle credits button click
        /// </summary>
        private void OnCreditsClicked(ClickEvent evt)
        {
            LogInfo("Credits clicked");
            
            _onMenuButtonClicked?.RaiseButtonClick("credits", PanelId, evt.position);
            
            // Show credits panel
            // _uiManager.ShowPanel("credits-panel");
        }
        
        /// <summary>
        /// Handle exit game button click
        /// </summary>
        private void OnExitGameClicked(ClickEvent evt)
        {
            LogInfo("Exit Game clicked");
            
            _onExitGameClicked?.Raise();
            _onMenuButtonClicked?.RaiseButtonClick("exit-game", PanelId, evt.position);
            
            // Show confirmation dialog or exit directly
            ShowExitConfirmation();
        }
        
        /// <summary>
        /// Show exit confirmation dialog
        /// </summary>
        private void ShowExitConfirmation()
        {
            var modalContent = new VisualElement();
            // modalContent.style.backgroundColor = _uiManager.DesignSystem.ColorPalette.SurfaceDark;
            modalContent.style.borderTopLeftRadius = 12;
            modalContent.style.borderTopRightRadius = 12;
            modalContent.style.borderBottomLeftRadius = 12;
            modalContent.style.borderBottomRightRadius = 12;
            modalContent.style.paddingTop = 24;
            modalContent.style.paddingBottom = 24;
            modalContent.style.paddingLeft = 32;
            modalContent.style.paddingRight = 32;
            modalContent.style.alignItems = Align.Center;
            modalContent.style.justifyContent = Justify.Center;
            modalContent.style.minWidth = 300;
            
            var titleLabel = new Label("Exit Game");
            titleLabel.style.fontSize = 18;
            // titleLabel.style.color = _uiManager.DesignSystem.ColorPalette.TextPrimary;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 16;
            
            var messageLabel = new Label("Are you sure you want to exit Project Chimera?");
            messageLabel.style.fontSize = 14;
            // messageLabel.style.color = _uiManager.DesignSystem.ColorPalette.TextSecondary;
            messageLabel.style.marginBottom = 24;
            messageLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            
            var buttonContainer = new VisualElement();
            buttonContainer.style.flexDirection = FlexDirection.Row;
            buttonContainer.style.justifyContent = Justify.Center;
            
            var confirmButton = new Button(() => Application.Quit());
            confirmButton.text = "Exit";
            confirmButton.style.marginRight = 12;
            // _uiManager.ApplyDesignSystemStyle(confirmButton, UIStyleToken.PrimaryButton);
            
            var cancelButton = new Button(() => { /* _uiManager.HideModal(); */ });
            cancelButton.text = "Cancel";
            // _uiManager.ApplyDesignSystemStyle(cancelButton, UIStyleToken.SecondaryButton);
            
            buttonContainer.Add(confirmButton);
            buttonContainer.Add(cancelButton);
            
            modalContent.Add(titleLabel);
            modalContent.Add(messageLabel);
            modalContent.Add(buttonContainer);
            
            // _uiManager.ShowModal(modalContent);
        }
        
        /// <summary>
        /// Play button hover sound
        /// </summary>
        private void PlayHoverSound()
        {
            if (_buttonHoverSound != null)
            {
                // This would integrate with the audio system
                // AudioSource.PlayClipAtPoint(_buttonHoverSound, Vector3.zero);
            }
        }
        
        /// <summary>
        /// Play button click sound
        /// </summary>
        private void PlayClickSound()
        {
            if (_buttonClickSound != null)
            {
                // This would integrate with the audio system
                // AudioSource.PlayClipAtPoint(_buttonClickSound, Vector3.zero);
            }
        }
        
        protected override void OnAfterShow()
        {
            base.OnAfterShow();
            
            // Refresh save data check when panel is shown
            CheckForSaveData();
            UpdateMenuState();
            
            // Log analytics
            LogInfo("Main menu displayed");
        }
        
        protected override void OnAfterHide()
        {
            base.OnAfterHide();
            
            // Clean up any temporary UI elements
            // _uiManager.HideModal();
        }
    }
}