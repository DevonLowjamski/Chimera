using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectChimera.Core.Logging;
using ProjectChimera.Systems.Processing;
using ProjectChimera.Data.Processing;
using System;

namespace ProjectChimera.UI.Processing
{
    /// <summary>
    /// Processing batch card - individual batch UI card.
    ///
    /// GAMEPLAY PURPOSE:
    /// Shows batch status, quality, and actions in a compact card format.
    ///
    /// **Card Layout**:
    /// - Batch name and strain
    /// - Stage indicator (Drying/Curing/Complete)
    /// - Progress bar and time remaining
    /// - Quality score with color coding
    /// - Action buttons (Burp, Transfer, Complete)
    /// - Risk warnings (mold, over-dry)
    /// </summary>
    public class ProcessingBatchCard : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _batchNameText;
        [SerializeField] private TextMeshProUGUI _strainNameText;
        [SerializeField] private TextMeshProUGUI _stageText;
        [SerializeField] private TextMeshProUGUI _progressText;
        [SerializeField] private Slider _progressBar;
        [SerializeField] private TextMeshProUGUI _qualityText;
        [SerializeField] private Image _qualityColorIndicator;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private GameObject _warningIcon;
        [SerializeField] private TextMeshProUGUI _warningText;

        [Header("Action Buttons")]
        [SerializeField] private Button _actionButton;
        [SerializeField] private TextMeshProUGUI _actionButtonText;
        [SerializeField] private Button _detailsButton;

        [Header("Colors")]
        [SerializeField] private Color _premiumColor = new Color(1f, 0.84f, 0f);    // Gold
        [SerializeField] private Color _excellentColor = Color.green;
        [SerializeField] private Color _goodColor = new Color(0.5f, 1f, 0.5f);
        [SerializeField] private Color _fairColor = Color.yellow;
        [SerializeField] private Color _poorColor = Color.red;

        // Batch data
        private ProcessingBatch _batch;
        private DryingSystem _dryingSystem;
        private CuringSystem _curingSystem;
        private ProcessingBatchManager _batchManager;

        /// <summary>
        /// Sets up card with batch data.
        /// </summary>
        public void Setup(ProcessingBatch batch, DryingSystem dryingSystem,
            CuringSystem curingSystem, ProcessingBatchManager batchManager)
        {
            _batch = batch;
            _dryingSystem = dryingSystem;
            _curingSystem = curingSystem;
            _batchManager = batchManager;

            UpdateDisplay();
            SetupButtons();
        }

        /// <summary>
        /// Updates all display elements.
        /// Called when batch data changes via events.
        /// </summary>
        private void UpdateDisplay()
        {
            if (_batch == null)
                return;

            // Batch name
            if (_batchNameText != null)
                _batchNameText.text = _batch.BatchId;

            // Strain name
            if (_strainNameText != null)
                _strainNameText.text = _batch.StrainName;

            // Stage
            if (_stageText != null)
                _stageText.text = GetStageDisplayText();

            // Progress
            UpdateProgress();

            // Quality
            UpdateQuality();

            // Status
            UpdateStatus();

            // Warnings
            UpdateWarnings();

            // Action button
            UpdateActionButton();
        }

        /// <summary>
        /// Gets display text for stage.
        /// </summary>
        private string GetStageDisplayText()
        {
            switch (_batch.Stage)
            {
                case ProcessingStage.Fresh:
                    return "Fresh - Ready to Dry";
                case ProcessingStage.Drying:
                    return $"Drying - Day {_batch.DryingDaysElapsed}/{_batch.TargetDryingDays}";
                case ProcessingStage.Dried:
                    return "Dried - Ready to Cure";
                case ProcessingStage.Curing:
                    return $"Curing - Week {_batch.CuringWeeksElapsed}/{_batch.TargetCuringWeeks}";
                case ProcessingStage.Cured:
                    return "Cured - Ready to Sell";
                case ProcessingStage.Spoiled:
                    return "Spoiled";
                default:
                    return "Unknown";
            }
        }

        /// <summary>
        /// Updates progress bar and text.
        /// </summary>
        private void UpdateProgress()
        {
            float progress = 0f;
            string progressText = "";

            switch (_batch.Stage)
            {
                case ProcessingStage.Drying:
                    if (_batch.TargetDryingDays > 0)
                    {
                        progress = (float)_batch.DryingDaysElapsed / _batch.TargetDryingDays;
                        int daysRemaining = _batch.TargetDryingDays - _batch.DryingDaysElapsed;
                        progressText = $"{daysRemaining} days remaining";
                    }
                    break;

                case ProcessingStage.Curing:
                    if (_batch.TargetCuringWeeks > 0)
                    {
                        progress = (float)_batch.CuringWeeksElapsed / _batch.TargetCuringWeeks;
                        int weeksRemaining = _batch.TargetCuringWeeks - _batch.CuringWeeksElapsed;
                        progressText = $"{weeksRemaining} weeks remaining";
                    }
                    break;

                case ProcessingStage.Fresh:
                case ProcessingStage.Dried:
                    progress = 0f;
                    progressText = "Ready for next stage";
                    break;

                case ProcessingStage.Cured:
                    progress = 1f;
                    progressText = "Complete";
                    break;

                case ProcessingStage.Spoiled:
                    progress = 0f;
                    progressText = "Spoiled";
                    break;
            }

            if (_progressBar != null)
                _progressBar.value = progress;

            if (_progressText != null)
                _progressText.text = progressText;
        }

        /// <summary>
        /// Updates quality display.
        /// </summary>
        private void UpdateQuality()
        {
            float quality = _batch.CurrentQuality;

            if (_qualityText != null)
                _qualityText.text = $"Quality: {quality:F0}%";

            // Color indicator
            Color qualityColor = GetQualityColor(quality);
            if (_qualityColorIndicator != null)
                _qualityColorIndicator.color = qualityColor;
        }

        /// <summary>
        /// Gets color for quality score.
        /// </summary>
        private Color GetQualityColor(float quality)
        {
            if (quality >= 90f) return _premiumColor;
            if (quality >= 80f) return _excellentColor;
            if (quality >= 70f) return _goodColor;
            if (quality >= 60f) return _fairColor;
            return _poorColor;
        }

        /// <summary>
        /// Updates status text.
        /// </summary>
        private void UpdateStatus()
        {
            string status = "";

            switch (_batch.Stage)
            {
                case ProcessingStage.Drying:
                    if (_dryingSystem != null)
                    {
                        var metrics = _dryingSystem.GetDryingMetrics(_batch.BatchId);
                        status = metrics.Status;
                    }
                    break;

                case ProcessingStage.Curing:
                    if (_curingSystem != null)
                    {
                        var metrics = _curingSystem.GetCuringMetrics(_batch.BatchId);
                        status = metrics.Status;
                    }
                    break;

                case ProcessingStage.Cured:
                    status = ProcessingQualityCalculator.GetQualityGrade(_batch.CurrentQuality);
                    break;

                case ProcessingStage.Spoiled:
                    status = "Batch ruined - dispose";
                    break;
            }

            if (_statusText != null)
                _statusText.text = status;
        }

        /// <summary>
        /// Updates warning indicators.
        /// </summary>
        private void UpdateWarnings()
        {
            bool hasWarning = false;
            string warningMessage = "";

            // Check mold risk
            if (_batch.MoldRisk > 0.7f)
            {
                hasWarning = true;
                warningMessage = "⚠️ High mold risk";
            }
            // Check over-dry risk
            else if (_batch.OverDryRisk > 0.7f)
            {
                hasWarning = true;
                warningMessage = "⚠️ Over-drying";
            }
            // Check burp reminder
            else if (_batch.Stage == ProcessingStage.Curing && _curingSystem != null)
            {
                var metrics = _curingSystem.GetCuringMetrics(_batch.BatchId);
                if (metrics.NeedsBurping)
                {
                    hasWarning = true;
                    warningMessage = "⏰ Time to burp";
                }
            }

            if (_warningIcon != null)
                _warningIcon.SetActive(hasWarning);

            if (_warningText != null)
                _warningText.text = warningMessage;
        }

        /// <summary>
        /// Updates action button based on batch state.
        /// </summary>
        private void UpdateActionButton()
        {
            if (_actionButton == null || _actionButtonText == null)
                return;

            bool buttonEnabled = true;
            string buttonText = "";

            switch (_batch.Stage)
            {
                case ProcessingStage.Fresh:
                    buttonText = "Start Drying";
                    break;

                case ProcessingStage.Drying:
                    buttonText = "Adjust Conditions";
                    break;

                case ProcessingStage.Dried:
                    buttonText = "Start Curing";
                    break;

                case ProcessingStage.Curing:
                    if (_curingSystem != null)
                    {
                        var metrics = _curingSystem.GetCuringMetrics(_batch.BatchId);
                        if (metrics.NeedsBurping)
                            buttonText = "Burp Jar";
                        else
                        {
                            var timeUntilBurp = _curingSystem.GetTimeUntilNextBurp(_batch.BatchId);
                            buttonText = $"Next burp: {timeUntilBurp.Hours}h {timeUntilBurp.Minutes}m";
                            buttonEnabled = false;
                        }
                    }
                    break;

                case ProcessingStage.Cured:
                    buttonText = "Move to Inventory";
                    break;

                case ProcessingStage.Spoiled:
                    buttonText = "Dispose";
                    break;
            }

            _actionButtonText.text = buttonText;
            _actionButton.interactable = buttonEnabled;
        }

        /// <summary>
        /// Sets up button listeners.
        /// </summary>
        private void SetupButtons()
        {
            if (_actionButton != null)
                _actionButton.onClick.AddListener(OnActionClicked);

            if (_detailsButton != null)
                _detailsButton.onClick.AddListener(OnDetailsClicked);
        }

        /// <summary>
        /// Handles action button click.
        /// </summary>
        private void OnActionClicked()
        {
            switch (_batch.Stage)
            {
                case ProcessingStage.Fresh:
                    StartDrying();
                    break;

                case ProcessingStage.Dried:
                    StartCuring();
                    break;

                case ProcessingStage.Curing:
                    BurpJar();
                    break;

                case ProcessingStage.Cured:
                    MoveToInventory();
                    break;

                case ProcessingStage.Spoiled:
                    Dispose();
                    break;
            }
        }

        /// <summary>
        /// Starts drying process.
        /// </summary>
        private void StartDrying()
        {
            // Use ideal conditions by default
            var conditions = DryingConditions.Ideal;
            _batchManager?.StartDrying(_batch.BatchId, conditions);
        }

        /// <summary>
        /// Starts curing process.
        /// </summary>
        private void StartCuring()
        {
            // Use ideal jar config by default
            var jarConfig = CuringJarConfig.Ideal;
            _batchManager?.StartCuring(_batch.BatchId, jarConfig, 4); // 4 weeks default
        }

        /// <summary>
        /// Burps the jar.
        /// </summary>
        private void BurpJar()
        {
            _batchManager?.BurpJar(_batch.BatchId);
        }

        /// <summary>
        /// Moves batch to inventory (placeholder).
        /// </summary>
        private void MoveToInventory()
        {
            // TODO: Integrate with inventory system
            ChimeraLogger.Log("PROCESSING", $"Moving batch {_batch.BatchId} to inventory", this);
        }

        /// <summary>
        /// Disposes of spoiled batch.
        /// </summary>
        private void Dispose()
        {
            // TODO: Remove batch and clean up
            ChimeraLogger.Log("PROCESSING", $"Disposing batch {_batch.BatchId}", this);
        }

        /// <summary>
        /// Shows batch details panel.
        /// </summary>
        private void OnDetailsClicked()
        {
            // TODO: Open detailed view panel
            ChimeraLogger.Log("PROCESSING", $"Showing details for {_batch.BatchId}", this);
        }

        private void OnDestroy()
        {
            if (_actionButton != null)
                _actionButton.onClick.RemoveListener(OnActionClicked);

            if (_detailsButton != null)
                _detailsButton.onClick.RemoveListener(OnDetailsClicked);
        }
    }
}
