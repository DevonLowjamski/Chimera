using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Economy;
using ProjectChimera.Data.Shared;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ProjectChimera.Systems.Economy
{
    /// <summary>
    /// Service responsible for contract-related notifications integration for Phase 8 MVP
    /// Manages contract progress alerts, deadline warnings, and completion notifications
    /// </summary>
    public class ContractNotificationService : ChimeraManager
    {
        [Header("Notification Configuration")]
        [SerializeField] private bool _enableContractNotifications = true;
        [SerializeField] private bool _enableProgressNotifications = true;
        [SerializeField] private bool _enableDeadlineWarnings = true;
        [SerializeField] private bool _enableQualityAlerts = true;
        [SerializeField] private bool _enableCompletionNotifications = true;
        
        [Header("Progress Notifications")]
        [SerializeField] private float _progressNotificationInterval = 0.25f; // Every 25%
        [SerializeField] private bool _showQuantityProgress = true;
        [SerializeField] private bool _showQualityProgress = true;
        [SerializeField] private float _progressNotificationDuration = 5f;
        
        [Header("Deadline Warnings")]
        [SerializeField] private List<int> _warningDaysThresholds = new List<int> { 7, 3, 1 };
        [SerializeField] private bool _showUrgentWarnings = true;
        [SerializeField] private float _urgentWarningDuration = 10f;
        [SerializeField] private bool _enablePersistentDeadlineWarnings = true;
        
        [Header("Quality Notifications")]
        [SerializeField] private bool _showQualityGradeNotifications = true;
        [SerializeField] private bool _showQualityConsistencyAlerts = true;
        [SerializeField] private bool _showQualityIssueAlerts = true;
        [SerializeField] private float _qualityNotificationDuration = 7f;
        
        [Header("Completion Notifications")]
        [SerializeField] private bool _showContractAcceptedNotifications = true;
        [SerializeField] private bool _showContractCompletedNotifications = true;
        [SerializeField] private bool _showContractExpiredNotifications = true;
        [SerializeField] private bool _showDeliveryNotifications = true;
        [SerializeField] private float _completionNotificationDuration = 8f;
        
        // Service dependencies
        private IContractNotificationProvider _notificationProvider;
        private ContractGenerationService _contractService;
        private ContractTrackingService _trackingService;
        
        // Notification tracking
        private Dictionary<string, ContractNotificationState> _contractStates = new Dictionary<string, ContractNotificationState>();
        private Dictionary<string, List<string>> _activeNotifications = new Dictionary<string, List<string>>();
        private HashSet<string> _persistentNotifications = new HashSet<string>();
        
        // Notification templates
        private ContractNotificationTemplates _templates;
        
        public override ManagerPriority Priority => ManagerPriority.Normal;
        
        // Public Properties
        public bool NotificationsEnabled { get => _enableContractNotifications; set => _enableContractNotifications = value; }
        public int ActiveContractNotifications => _activeNotifications.Values.Sum(list => list.Count);
        public int PersistentNotifications => _persistentNotifications.Count;
        
        // Events
        public System.Action<string, string> OnContractNotificationSent; // contractId, notificationId
        public System.Action<string> OnNotificationDismissed; // notificationId
        
        protected override void OnManagerInitialize()
        {
            InitializeSystemReferences();
            InitializeNotificationTemplates();
            SubscribeToContractEvents();
            
            LogInfo("ContractNotificationService initialized with comprehensive notification support");
        }
        
        protected override void OnManagerUpdate()
        {
            if (!_enableContractNotifications) return;
            
            // Update contract state tracking
            UpdateContractStates();
            
            // Process pending notifications
            ProcessPendingNotifications();
        }
        
        /// <summary>
        /// Send a contract progress notification
        /// </summary>
        public void SendProgressNotification(string contractId, float progress, string message = null)
        {
            if (!_enableProgressNotifications || !_enableContractNotifications) return;
            
            var contract = GetActiveContract(contractId);
            if (contract == null) return;
            
            // Check if we should send progress notification
            var state = GetOrCreateContractState(contractId);
            float progressThreshold = Mathf.Floor(progress / _progressNotificationInterval) * _progressNotificationInterval;
            
            if (progressThreshold > state.LastProgressNotified)
            {
                string notificationMessage = message ?? _templates.GetProgressMessage(contract, progress);
                
                var notificationData = new ContractNotificationData
                {
                    Title = $"Contract Progress: {contract.ContractTitle}",
                    Message = notificationMessage,
                    Severity = GetProgressSeverity(progress),
                    Duration = _progressNotificationDuration,
                    Priority = ContractNotificationPriority.Normal,
                    Position = ContractNotificationPosition.TopRight
                };
                
                string notificationId = _notificationProvider.ShowNotification(notificationData);
                TrackNotification(contractId, notificationId);
                
                state.LastProgressNotified = progressThreshold;
                OnContractNotificationSent?.Invoke(contractId, notificationId);
                
                LogInfo($"Progress notification sent for contract {contractId}: {progress:P0}");
            }
        }
        
        /// <summary>
        /// Send a deadline warning notification
        /// </summary>
        public void SendDeadlineWarning(string contractId, int daysRemaining)
        {
            if (!_enableDeadlineWarnings || !_enableContractNotifications) return;
            
            var contract = GetActiveContract(contractId);
            if (contract == null) return;
            
            // Check if we should send warning for this threshold
            if (!_warningDaysThresholds.Contains(daysRemaining)) return;
            
            var state = GetOrCreateContractState(contractId);
            if (state.DeadlineWarningsSent.Contains(daysRemaining)) return;
            
            string notificationMessage = _templates.GetDeadlineWarningMessage(contract, daysRemaining);
            ContractNotificationSeverity severity = GetDeadlineSeverity(daysRemaining);
            
            var notificationData = new ContractNotificationData
            {
                Title = $"Contract Deadline Warning",
                Message = notificationMessage,
                Severity = severity,
                Duration = daysRemaining <= 1 ? _urgentWarningDuration : _progressNotificationDuration,
                Priority = daysRemaining <= 1 ? ContractNotificationPriority.High : ContractNotificationPriority.Normal,
                Position = ContractNotificationPosition.TopRight
            };
            
            string notificationId;
            
            // Use persistent notification for urgent warnings
            if (_enablePersistentDeadlineWarnings && daysRemaining <= 1)
            {
                string persistentKey = $"deadline_{contractId}";
                notificationId = _notificationProvider.ShowPersistentNotification(persistentKey, notificationMessage, severity);
                _persistentNotifications.Add(notificationId);
            }
            else
            {
                notificationId = _notificationProvider.ShowNotification(notificationData);
            }
            
            TrackNotification(contractId, notificationId);
            state.DeadlineWarningsSent.Add(daysRemaining);
            OnContractNotificationSent?.Invoke(contractId, notificationId);
            
            LogInfo($"Deadline warning sent for contract {contractId}: {daysRemaining} days remaining");
        }
        
        /// <summary>
        /// Send a quality assessment notification
        /// </summary>
        public void SendQualityNotification(string contractId, QualityGrade grade, string details = null)
        {
            if (!_enableQualityAlerts || !_enableContractNotifications) return;
            
            var contract = GetActiveContract(contractId);
            if (contract == null) return;
            
            string notificationMessage = _templates.GetQualityGradeMessage(contract, grade, details);
            
            var notificationData = new ContractNotificationData
            {
                Title = $"Quality Assessment: {contract.ContractTitle}",
                Message = notificationMessage,
                Severity = GetQualitySeverity(grade),
                Duration = _qualityNotificationDuration,
                Priority = ContractNotificationPriority.Normal,
                Position = ContractNotificationPosition.TopRight
            };
            
            string notificationId = _notificationProvider.ShowNotification(notificationData);
            TrackNotification(contractId, notificationId);
            OnContractNotificationSent?.Invoke(contractId, notificationId);
            
            LogInfo($"Quality notification sent for contract {contractId}: Grade {grade}");
        }
        
        /// <summary>
        /// Send a quality consistency alert
        /// </summary>
        public void SendQualityConsistencyAlert(string contractId, float variance)
        {
            if (!_enableQualityAlerts || !_showQualityConsistencyAlerts || !_enableContractNotifications) return;
            
            var contract = GetActiveContract(contractId);
            if (contract == null) return;
            
            string notificationMessage = _templates.GetQualityConsistencyMessage(contract, variance);
            
            var notificationData = new ContractNotificationData
            {
                Title = $"Quality Consistency Alert",
                Message = notificationMessage,
                Severity = ContractNotificationSeverity.Warning,
                Duration = _qualityNotificationDuration,
                Priority = ContractNotificationPriority.Normal,
                Position = ContractNotificationPosition.TopRight
            };
            
            string notificationId = _notificationProvider.ShowNotification(notificationData);
            TrackNotification(contractId, notificationId);
            OnContractNotificationSent?.Invoke(contractId, notificationId);
            
            LogInfo($"Quality consistency alert sent for contract {contractId}: Variance {variance:F3}");
        }
        
        /// <summary>
        /// Send a contract completion notification
        /// </summary>
        public void SendContractCompletedNotification(string contractId, ContractCompletionResult result)
        {
            if (!_enableCompletionNotifications || !_showContractCompletedNotifications || !_enableContractNotifications) return;
            
            var contract = GetActiveContract(contractId);
            if (contract == null) return;
            
            string notificationMessage = _templates.GetCompletionMessage(contract, result);
            
            var notificationData = new ContractNotificationData
            {
                Title = $"Contract Completed!",
                Message = notificationMessage,
                Severity = ContractNotificationSeverity.Success,
                Duration = _completionNotificationDuration,
                Priority = ContractNotificationPriority.High,
                Position = ContractNotificationPosition.TopCenter
            };
            
            string notificationId = _notificationProvider.ShowNotification(notificationData);
            TrackNotification(contractId, notificationId);
            OnContractNotificationSent?.Invoke(contractId, notificationId);
            
            // Clean up persistent notifications for this contract
            CleanupContractNotifications(contractId);
            
            LogInfo($"Contract completion notification sent for {contractId}: ${result.TotalPayout:F2}");
        }
        
        /// <summary>
        /// Send a contract acceptance notification
        /// </summary>
        public void SendContractAcceptedNotification(ActiveContractSO contract)
        {
            if (!_enableCompletionNotifications || !_showContractAcceptedNotifications || !_enableContractNotifications) return;
            
            string notificationMessage = _templates.GetAcceptanceMessage(contract);
            
            var notificationData = new ContractNotificationData
            {
                Title = $"Contract Accepted",
                Message = notificationMessage,
                Severity = ContractNotificationSeverity.Info,
                Duration = _progressNotificationDuration,
                Priority = ContractNotificationPriority.Normal,
                Position = ContractNotificationPosition.TopRight
            };
            
            string notificationId = _notificationProvider.ShowNotification(notificationData);
            TrackNotification(contract.ContractId, notificationId);
            OnContractNotificationSent?.Invoke(contract.ContractId, notificationId);
            
            LogInfo($"Contract acceptance notification sent for {contract.ContractId}");
        }
        
        /// <summary>
        /// Send a contract expiration notification
        /// </summary>
        public void SendContractExpiredNotification(ActiveContractSO contract)
        {
            if (!_enableCompletionNotifications || !_showContractExpiredNotifications || !_enableContractNotifications) return;
            
            string notificationMessage = _templates.GetExpirationMessage(contract);
            
            var notificationData = new ContractNotificationData
            {
                Title = $"Contract Expired",
                Message = notificationMessage,
                Severity = ContractNotificationSeverity.Error,
                Duration = _completionNotificationDuration,
                Priority = ContractNotificationPriority.High,
                Position = ContractNotificationPosition.TopCenter
            };
            
            string notificationId = _notificationProvider.ShowNotification(notificationData);
            TrackNotification(contract.ContractId, notificationId);
            OnContractNotificationSent?.Invoke(contract.ContractId, notificationId);
            
            // Clean up persistent notifications for this contract
            CleanupContractNotifications(contract.ContractId);
            
            LogInfo($"Contract expiration notification sent for {contract.ContractId}");
        }
        
        /// <summary>
        /// Send delivery ready notification
        /// </summary>
        public void SendDeliveryReadyNotification(string contractId, ContractCompletionResult result)
        {
            if (!_enableCompletionNotifications || !_showDeliveryNotifications || !_enableContractNotifications) return;
            
            var contract = GetActiveContract(contractId);
            if (contract == null) return;
            
            string notificationMessage = _templates.GetDeliveryReadyMessage(contract, result);
            
            var notificationData = new ContractNotificationData
            {
                Title = $"Ready for Delivery",
                Message = notificationMessage,
                Severity = ContractNotificationSeverity.Success,
                Duration = _completionNotificationDuration,
                Priority = ContractNotificationPriority.High,
                Position = ContractNotificationPosition.TopCenter,
                ActionText = "Deliver Now",
                OnAction = () => {
                    // Could trigger delivery UI here
                    LogInfo($"Delivery action triggered for contract {contractId}");
                }
            };
            
            string notificationId = _notificationProvider.ShowNotification(notificationData);
            TrackNotification(contractId, notificationId);
            OnContractNotificationSent?.Invoke(contractId, notificationId);
            
            LogInfo($"Delivery ready notification sent for contract {contractId}");
        }
        
        /// <summary>
        /// Dismiss all notifications for a specific contract
        /// </summary>
        public void DismissContractNotifications(string contractId)
        {
            if (_activeNotifications.TryGetValue(contractId, out var notifications))
            {
                foreach (string notificationId in notifications.ToList())
                {
                    _notificationProvider.DismissNotification(notificationId);
                    _persistentNotifications.Remove(notificationId);
                }
                
                _activeNotifications[contractId].Clear();
            }
        }
        
        /// <summary>
        /// Get notification statistics for a contract
        /// </summary>
        public ContractNotificationStats GetNotificationStats(string contractId)
        {
            var state = GetOrCreateContractState(contractId);
            var activeCount = _activeNotifications.TryGetValue(contractId, out var notifications) ? notifications.Count : 0;
            
            return new ContractNotificationStats
            {
                ContractId = contractId,
                TotalNotificationsSent = state.TotalNotificationsSent,
                ActiveNotifications = activeCount,
                LastProgressNotified = state.LastProgressNotified,
                DeadlineWarningsSent = state.DeadlineWarningsSent.Count,
                QualityAlertsSent = state.QualityAlertsSent
            };
        }
        
        private void InitializeSystemReferences()
        {
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                _contractService = gameManager.GetManager<ContractGenerationService>();
                _trackingService = gameManager.GetManager<ContractTrackingService>();
                
                // Try to get notification provider through dependency injection or manager lookup
                var providers = FindObjectsOfType<MonoBehaviour>().OfType<IContractNotificationProvider>();
                _notificationProvider = providers.FirstOrDefault();
            }
            
            if (_notificationProvider == null)
            {
                LogWarning("Contract notification provider not found - notifications will use fallback logging");
                _notificationProvider = new FallbackNotificationProvider();
            }
        }
        
        private void InitializeNotificationTemplates()
        {
            _templates = new ContractNotificationTemplates();
        }
        
        private void SubscribeToContractEvents()
        {
            if (_contractService != null)
            {
                _contractService.OnContractAccepted += OnContractAccepted;
                _contractService.OnContractCompleted += OnContractCompleted;
                _contractService.OnContractExpired += OnContractExpired;
            }
            
            if (_trackingService != null)
            {
                _trackingService.OnContractProgressUpdated += OnContractProgressUpdated;
                _trackingService.OnDeadlineWarning += OnDeadlineWarning;
                _trackingService.OnContractReadyForDelivery += OnContractReadyForDelivery;
                // Note: OnQualityGradeAssigned and OnQualityConsistencyAlert are handled via validation service
            }
        }
        
        private void UpdateContractStates()
        {
            // Update contract states based on active contracts
            if (_contractService != null)
            {
                var activeContracts = _contractService.ActiveContracts;
                foreach (var contract in activeContracts)
                {
                    GetOrCreateContractState(contract.ContractId);
                }
            }
        }
        
        private void ProcessPendingNotifications()
        {
            // Could implement delayed notification processing here
            // For now, notifications are sent immediately
        }
        
        private void OnContractAccepted(ActiveContractSO contract)
        {
            SendContractAcceptedNotification(contract);
        }
        
        private void OnContractCompleted(ActiveContractSO contract)
        {
            // Contract completion notification will be sent by the delivery system
            // This is just cleanup
            CleanupContractNotifications(contract.ContractId);
        }
        
        private void OnContractExpired(ActiveContractSO contract)
        {
            SendContractExpiredNotification(contract);
        }
        
        private void OnContractProgressUpdated(ActiveContractSO contract, float progress)
        {
            SendProgressNotification(contract.ContractId, progress);
        }
        
        private void OnDeadlineWarning(ActiveContractSO contract, int daysRemaining)
        {
            SendDeadlineWarning(contract.ContractId, daysRemaining);
        }
        
        private void OnContractReadyForDelivery(ActiveContractSO contract, ContractCompletionResult result)
        {
            SendDeliveryReadyNotification(contract.ContractId, result);
        }
        
        private void OnQualityGradeAssigned(string contractId, QualityGrade grade)
        {
            SendQualityNotification(contractId, grade);
        }
        
        private void OnQualityConsistencyAlert(string contractId, float variance)
        {
            SendQualityConsistencyAlert(contractId, variance);
        }
        
        private ContractNotificationState GetOrCreateContractState(string contractId)
        {
            if (!_contractStates.TryGetValue(contractId, out var state))
            {
                state = new ContractNotificationState
                {
                    ContractId = contractId,
                    CreationTime = DateTime.Now
                };
                _contractStates[contractId] = state;
                _activeNotifications[contractId] = new List<string>();
            }
            
            return state;
        }
        
        private void TrackNotification(string contractId, string notificationId)
        {
            if (!_activeNotifications.ContainsKey(contractId))
            {
                _activeNotifications[contractId] = new List<string>();
            }
            
            _activeNotifications[contractId].Add(notificationId);
            
            var state = GetOrCreateContractState(contractId);
            state.TotalNotificationsSent++;
        }
        
        private void CleanupContractNotifications(string contractId)
        {
            DismissContractNotifications(contractId);
            
            // Remove persistent notifications
            string persistentKey = $"deadline_{contractId}";
            _notificationProvider.DismissNotification(persistentKey);
            _persistentNotifications.Remove(persistentKey);
            
            // Clean up state
            _contractStates.Remove(contractId);
            _activeNotifications.Remove(contractId);
        }
        
        private ActiveContractSO GetActiveContract(string contractId)
        {
            if (_contractService != null)
            {
                return _contractService.ActiveContracts.FirstOrDefault(c => c.ContractId == contractId);
            }
            
            return null;
        }
        
        private ContractNotificationSeverity GetProgressSeverity(float progress)
        {
            if (progress >= 0.9f) return ContractNotificationSeverity.Success;
            if (progress >= 0.5f) return ContractNotificationSeverity.Info;
            return ContractNotificationSeverity.Info;
        }
        
        private ContractNotificationSeverity GetDeadlineSeverity(int daysRemaining)
        {
            if (daysRemaining <= 1) return ContractNotificationSeverity.Critical;
            if (daysRemaining <= 3) return ContractNotificationSeverity.Warning;
            return ContractNotificationSeverity.Info;
        }
        
        private ContractNotificationSeverity GetQualitySeverity(QualityGrade grade)
        {
            return grade switch
            {
                QualityGrade.Premium => ContractNotificationSeverity.Success,
                QualityGrade.Excellent => ContractNotificationSeverity.Success,
                QualityGrade.Good => ContractNotificationSeverity.Info,
                QualityGrade.Standard => ContractNotificationSeverity.Info,
                QualityGrade.Acceptable => ContractNotificationSeverity.Warning,
                QualityGrade.BelowStandard => ContractNotificationSeverity.Error,
                _ => ContractNotificationSeverity.Info
            };
        }
        
        protected override void OnManagerShutdown()
        {
            // Unsubscribe from events
            if (_contractService != null)
            {
                _contractService.OnContractAccepted -= OnContractAccepted;
                _contractService.OnContractCompleted -= OnContractCompleted;
                _contractService.OnContractExpired -= OnContractExpired;
            }
            
            if (_trackingService != null)
            {
                _trackingService.OnContractProgressUpdated -= OnContractProgressUpdated;
                _trackingService.OnDeadlineWarning -= OnDeadlineWarning;
                _trackingService.OnContractReadyForDelivery -= OnContractReadyForDelivery;
                // Note: Quality events unsubscribed via validation service
            }
            
            LogInfo($"ContractNotificationService shutdown - {ActiveContractNotifications} active notifications dismissed");
        }
    }
    
    /// <summary>
    /// Contract notification state tracking
    /// </summary>
    [System.Serializable]
    public class ContractNotificationState
    {
        public string ContractId;
        public DateTime CreationTime;
        public float LastProgressNotified;
        public HashSet<int> DeadlineWarningsSent = new HashSet<int>();
        public int TotalNotificationsSent;
        public int QualityAlertsSent;
    }
    
    /// <summary>
    /// Contract notification statistics
    /// </summary>
    [System.Serializable]
    public class ContractNotificationStats
    {
        public string ContractId;
        public int TotalNotificationsSent;
        public int ActiveNotifications;
        public float LastProgressNotified;
        public int DeadlineWarningsSent;
        public int QualityAlertsSent;
    }
    
    /// <summary>
    /// Interface for contract notification providers to avoid UI assembly dependency
    /// </summary>
    public interface IContractNotificationProvider
    {
        string ShowNotification(ContractNotificationData data);
        string ShowPersistentNotification(string key, string message, ContractNotificationSeverity severity);
        bool DismissNotification(string notificationId);
        void DismissAllNotifications();
    }
    
    /// <summary>
    /// Contract notification data structure
    /// </summary>
    [System.Serializable]
    public class ContractNotificationData
    {
        public string Id;
        public string Title;
        public string Message;
        public ContractNotificationSeverity Severity = ContractNotificationSeverity.Info;
        public ContractNotificationPriority Priority = ContractNotificationPriority.Normal;
        public ContractNotificationPosition Position = ContractNotificationPosition.TopRight;
        public float Duration = 5f;
        public float CreationTime;
        public string ActionText;
        public System.Action OnClick;
        public System.Action OnAction;
    }
    
    /// <summary>
    /// Contract notification severity levels
    /// </summary>
    public enum ContractNotificationSeverity
    {
        Info,
        Success,
        Warning,
        Error,
        Critical
    }
    
    /// <summary>
    /// Contract notification priority levels
    /// </summary>
    public enum ContractNotificationPriority
    {
        Low,
        Normal,
        High,
        Critical
    }
    
    /// <summary>
    /// Contract notification position options
    /// </summary>
    public enum ContractNotificationPosition
    {
        TopLeft,
        TopCenter,
        TopRight,
        BottomLeft,
        BottomCenter,
        BottomRight,
        Center
    }
    
    /// <summary>
    /// Fallback notification provider for when UI system is not available
    /// </summary>
    public class FallbackNotificationProvider : IContractNotificationProvider
    {
        public string ShowNotification(ContractNotificationData data)
        {
            Debug.Log($"[Contract Notification] {data.Title}: {data.Message}");
            return System.Guid.NewGuid().ToString();
        }
        
        public string ShowPersistentNotification(string key, string message, ContractNotificationSeverity severity)
        {
            Debug.Log($"[Contract Notification - Persistent] {key}: {message}");
            return System.Guid.NewGuid().ToString();
        }
        
        public bool DismissNotification(string notificationId)
        {
            Debug.Log($"[Contract Notification] Dismissed: {notificationId}");
            return true;
        }
        
        public void DismissAllNotifications()
        {
            Debug.Log("[Contract Notification] Dismissed all notifications");
        }
    }
    
    /// <summary>
    /// Contract notification message templates
    /// </summary>
    public class ContractNotificationTemplates
    {
        public string GetProgressMessage(ActiveContractSO contract, float progress)
        {
            return $"Contract progress: {progress:P0} complete\n" +
                   $"Strain: {contract.RequiredStrainType}\n" +
                   $"Target: {contract.QuantityRequired:F1}kg at {contract.MinimumQuality:P0} quality";
        }
        
        public string GetDeadlineWarningMessage(ActiveContractSO contract, int daysRemaining)
        {
            string urgency = daysRemaining switch
            {
                1 => "URGENT - Only 1 day left!",
                <= 3 => "Time is running short!",
                _ => "Deadline approaching"
            };
            
            return $"{urgency}\n" +
                   $"Contract: {contract.ContractTitle}\n" +
                   $"Days remaining: {daysRemaining}\n" +
                   $"Value: ${contract.ContractValue:F0}";
        }
        
        public string GetQualityGradeMessage(ActiveContractSO contract, QualityGrade grade, string details)
        {
            string gradeDescription = grade switch
            {
                QualityGrade.Premium => "Outstanding quality achieved!",
                QualityGrade.Excellent => "Excellent quality production",
                QualityGrade.Good => "Good quality standards met",
                QualityGrade.Standard => "Standard quality achieved",
                QualityGrade.Acceptable => "Minimum quality reached",
                QualityGrade.BelowStandard => "Quality below standards",
                _ => "Quality assessment complete"
            };
            
            return $"{gradeDescription}\n" +
                   $"Grade: {grade}\n" +
                   $"Contract: {contract.ContractTitle}" +
                   (string.IsNullOrEmpty(details) ? "" : $"\n{details}");
        }
        
        public string GetQualityConsistencyMessage(ActiveContractSO contract, float variance)
        {
            return $"Quality variance detected in production\n" +
                   $"Contract: {contract.ContractTitle}\n" +
                   $"Variance: {variance:F3}\n" +
                   $"Consider reviewing growing conditions";
        }
        
        public string GetCompletionMessage(ActiveContractSO contract, ContractCompletionResult result)
        {
            string bonusText = "";
            if (result.QualityBonus > 0)
                bonusText += $"\n🌟 Quality bonus: ${result.QualityBonus:F0}";
            if (result.EarlyBonus > 0)
                bonusText += $"\n⚡ Early completion bonus: ${result.EarlyBonus:F0}";
            
            return $"Contract successfully completed!\n" +
                   $"Total payout: ${result.TotalPayout:F0}\n" +
                   $"Quality: {result.FinalQuality:P1}" +
                   bonusText;
        }
        
        public string GetAcceptanceMessage(ActiveContractSO contract)
        {
            return $"New contract accepted!\n" +
                   $"Strain: {contract.RequiredStrainType}\n" +
                   $"Quantity: {contract.QuantityRequired:F1}kg\n" +
                   $"Deadline: {contract.GetDaysRemaining()} days\n" +
                   $"Value: ${contract.ContractValue:F0}";
        }
        
        public string GetExpirationMessage(ActiveContractSO contract)
        {
            return $"Contract has expired\n" +
                   $"Contract: {contract.ContractTitle}\n" +
                   $"Lost value: ${contract.ContractValue:F0}\n" +
                   $"Consider better time management for future contracts";
        }
        
        public string GetDeliveryReadyMessage(ActiveContractSO contract, ContractCompletionResult result)
        {
            return $"Contract ready for delivery!\n" +
                   $"Quality: {result.FinalQuality:P1}\n" +
                   $"Expected payout: ${result.TotalPayout:F0}\n" +
                   $"Tap to deliver now";
        }
    }
}