using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectChimera.Systems.Processing;
using ProjectChimera.Data.Processing;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;
using System.Collections.Generic;

namespace ProjectChimera.UI.Processing
{
    /// <summary>
    /// Processing dashboard panel - main UI for managing drying/curing pipeline.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST:
    /// ====================================
    /// "Your processing station - turn fresh harvest into premium product"
    ///
    /// **Dashboard Views**:
    /// - Active Batches: All batches currently drying or curing
    /// - Drying Room: Monitor temperature, humidity, days remaining
    /// - Curing Jars: Check humidity, burp reminders, quality progress
    /// - Completed: View finished batches ready to sell
    ///
    /// **Player Actions**:
    /// - Start drying fresh batches
    /// - Adjust drying conditions
    /// - Transfer to curing jars
    /// - Burp jars when prompted
    /// - View quality predictions
    /// - Complete processing when ready
    ///
    /// INVISIBLE COMPLEXITY:
    /// Players see: Clean dashboard with batch cards and status indicators.
    /// Behind scenes: Real-time quality calculations, risk assessments, predictions.
    /// </summary>
    public class ProcessingDashboardPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private Button _closeButton;

        [Header("Tab Navigation")]
        [SerializeField] private Button _activeBatchesTab;
        [SerializeField] private Button _dryingRoomTab;
        [SerializeField] private Button _curingJarsTab;
        [SerializeField] private Button _completedTab;

        [Header("Tab Content")]
        [SerializeField] private GameObject _activeBatchesContent;
        [SerializeField] private GameObject _dryingRoomContent;
        [SerializeField] private GameObject _curingJarsContent;
        [SerializeField] private GameObject _completedContent;

        [Header("Batch List")]
        [SerializeField] private Transform _batchListContainer;
        [SerializeField] private GameObject _batchCardPrefab;

        [Header("Stats Display")]
        [SerializeField] private TextMeshProUGUI _totalBatchesText;
        [SerializeField] private TextMeshProUGUI _activeBatchesText;
        [SerializeField] private TextMeshProUGUI _completedBatchesText;
        [SerializeField] private TextMeshProUGUI _spoiledBatchesText;

        // Services
        private ProcessingBatchManager _batchManager;
        private DryingSystem _dryingSystem;
        private CuringSystem _curingSystem;

        // State
        private ProcessingStage _currentFilter = ProcessingStage.Drying;
        private List<GameObject> _batchCards = new List<GameObject>();

        private void Start()
        {
            InitializePanel();
            SetupButtonListeners();
        }

        private void InitializePanel()
        {
            // Get services
            var container = ServiceContainerFactory.Instance;
            if (container != null)
            {
                _batchManager = container.Resolve<ProcessingBatchManager>();
                _dryingSystem = container.Resolve<DryingSystem>();
                _curingSystem = container.Resolve<CuringSystem>();
            }

            if (_batchManager == null)
            {
                ChimeraLogger.LogWarning("UI",
                    "ProcessingDashboardPanel: BatchManager not found", this);
                return;
            }

            // Subscribe to events
            _batchManager.OnBatchCreated += OnBatchCreated;
            _batchManager.OnBatchStageChanged += OnBatchStageChanged;
            _batchManager.OnBatchCompleted += OnBatchCompleted;
            _batchManager.OnBatchSpoiled += OnBatchSpoiled;

            // Hide by default
            if (_panelRoot != null)
                _panelRoot.SetActive(false);

            ChimeraLogger.Log("UI",
                "Processing dashboard initialized", this);
        }

        private void SetupButtonListeners()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(OnCloseClicked);

            if (_activeBatchesTab != null)
                _activeBatchesTab.onClick.AddListener(() => ShowTab(ProcessingStage.Drying));

            if (_dryingRoomTab != null)
                _dryingRoomTab.onClick.AddListener(() => ShowTab(ProcessingStage.Drying));

            if (_curingJarsTab != null)
                _curingJarsTab.onClick.AddListener(() => ShowTab(ProcessingStage.Curing));

            if (_completedTab != null)
                _completedTab.onClick.AddListener(() => ShowTab(ProcessingStage.Cured));
        }

        /// <summary>
        /// Shows the processing dashboard.
        /// </summary>
        public void ShowPanel()
        {
            if (_batchManager == null)
                return;

            UpdateStatistics();
            ShowTab(ProcessingStage.Drying);

            if (_panelRoot != null)
                _panelRoot.SetActive(true);

            ChimeraLogger.Log("UI",
                "Processing dashboard opened", this);
        }

        /// <summary>
        /// Shows a specific tab (filter).
        /// </summary>
        private void ShowTab(ProcessingStage stage)
        {
            _currentFilter = stage;

            // Hide all content
            if (_activeBatchesContent != null)
                _activeBatchesContent.SetActive(false);
            if (_dryingRoomContent != null)
                _dryingRoomContent.SetActive(false);
            if (_curingJarsContent != null)
                _curingJarsContent.SetActive(false);
            if (_completedContent != null)
                _completedContent.SetActive(false);

            // Show selected content
            switch (stage)
            {
                case ProcessingStage.Drying:
                    if (_dryingRoomContent != null)
                        _dryingRoomContent.SetActive(true);
                    RefreshBatchList(ProcessingStage.Drying);
                    break;

                case ProcessingStage.Curing:
                    if (_curingJarsContent != null)
                        _curingJarsContent.SetActive(true);
                    RefreshBatchList(ProcessingStage.Curing);
                    break;

                case ProcessingStage.Cured:
                    if (_completedContent != null)
                        _completedContent.SetActive(true);
                    RefreshBatchList(ProcessingStage.Cured);
                    break;

                default:
                    if (_activeBatchesContent != null)
                        _activeBatchesContent.SetActive(true);
                    RefreshActiveBatches();
                    break;
            }

            UpdateStatistics();
        }

        /// <summary>
        /// Refreshes batch list for specific stage.
        /// </summary>
        private void RefreshBatchList(ProcessingStage stage)
        {
            // Clear existing cards
            foreach (var card in _batchCards)
            {
                if (card != null)
                    Destroy(card);
            }
            _batchCards.Clear();

            // Get batches for stage
            var batches = _batchManager.GetBatchesByStage(stage);

            // Create cards
            foreach (var batch in batches)
            {
                CreateBatchCard(batch);
            }
        }

        /// <summary>
        /// Refreshes all active batches (drying + curing).
        /// </summary>
        private void RefreshActiveBatches()
        {
            // Clear existing cards
            foreach (var card in _batchCards)
            {
                if (card != null)
                    Destroy(card);
            }
            _batchCards.Clear();

            // Get active batches
            var batches = _batchManager.GetActiveBatches();

            // Create cards
            foreach (var batch in batches)
            {
                CreateBatchCard(batch);
            }
        }

        /// <summary>
        /// Creates a batch card UI element.
        /// </summary>
        private void CreateBatchCard(ProcessingBatch batch)
        {
            if (_batchCardPrefab == null || _batchListContainer == null)
                return;

            var cardObj = Instantiate(_batchCardPrefab, _batchListContainer);
            var card = cardObj.GetComponent<ProcessingBatchCard>();

            if (card != null)
            {
                card.Setup(batch, _dryingSystem, _curingSystem, _batchManager);
            }

            _batchCards.Add(cardObj);
        }

        /// <summary>
        /// Updates statistics display.
        /// </summary>
        private void UpdateStatistics()
        {
            if (_batchManager == null)
                return;

            var stats = _batchManager.GetStatistics();

            if (_totalBatchesText != null)
                _totalBatchesText.text = $"Total: {stats.total}";

            if (_activeBatchesText != null)
                _activeBatchesText.text = $"Active: {stats.active}";

            if (_completedBatchesText != null)
                _completedBatchesText.text = $"Completed: {stats.completed}";

            if (_spoiledBatchesText != null)
                _spoiledBatchesText.text = $"Spoiled: {stats.spoiled}";
        }

        #region Event Handlers

        private void OnBatchCreated(ProcessingBatch batch)
        {
            UpdateStatistics();
            if (_currentFilter == batch.Stage || _currentFilter == ProcessingStage.Fresh)
            {
                RefreshBatchList(_currentFilter);
            }
        }

        private void OnBatchStageChanged(ProcessingBatch batch)
        {
            UpdateStatistics();
            RefreshBatchList(_currentFilter);
        }

        private void OnBatchCompleted(ProcessingBatch batch, ProcessingQualityReport report)
        {
            UpdateStatistics();

            if (_currentFilter == ProcessingStage.Cured)
            {
                RefreshBatchList(_currentFilter);
            }

            // Show notification
            ChimeraLogger.Log("UI",
                $"Batch completed: {batch.StrainName} - {report.QualityGrade} quality", this);
        }

        private void OnBatchSpoiled(ProcessingBatch batch, string reason)
        {
            UpdateStatistics();
            RefreshBatchList(_currentFilter);

            // Show warning
            ChimeraLogger.LogWarning("UI",
                $"Batch spoiled: {batch.StrainName} - {reason}", this);
        }

        #endregion

        private void OnCloseClicked()
        {
            if (_panelRoot != null)
                _panelRoot.SetActive(false);
        }

        public void Hide()
        {
            OnCloseClicked();
        }

        public bool IsVisible()
        {
            return _panelRoot != null && _panelRoot.activeSelf;
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (_batchManager != null)
            {
                _batchManager.OnBatchCreated -= OnBatchCreated;
                _batchManager.OnBatchStageChanged -= OnBatchStageChanged;
                _batchManager.OnBatchCompleted -= OnBatchCompleted;
                _batchManager.OnBatchSpoiled -= OnBatchSpoiled;
            }

            // Clean up button listeners
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(OnCloseClicked);
        }
    }
}
