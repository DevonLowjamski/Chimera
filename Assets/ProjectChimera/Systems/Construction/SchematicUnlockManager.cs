using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core;
using ProjectChimera.Data.Construction;
using ProjectChimera.Data.Economy;
using ProjectChimera.Shared;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Manager for handling schematic unlock system integration with skill points economy.
    /// Manages blueprint unlock requirements, validation, and progression tracking.
    /// Uses event-based communication with Economy system for skill point transactions.
    /// </summary>
    public class SchematicUnlockManager : ChimeraManager
    {
        [Header("Unlock Configuration")]
        [SerializeField] private bool _enableUnlockSystem = true;
        [SerializeField] private bool _validatePrerequisites = true;
        [SerializeField] private bool _autoUnlockBasicSchematics = true;
        [SerializeField] private int _playerStartingLevel = 1;
        
        [Header("Unlock Notifications")]
        [SerializeField] private bool _showUnlockNotifications = true;
        [SerializeField] private bool _showProgressHints = true;
        [SerializeField] private float _notificationDuration = 3f;
        
        [Header("Performance Settings")]
        [SerializeField] private bool _enableUnlockCaching = true;
        [SerializeField] private float _cacheRefreshInterval = 30f;
        [SerializeField] private int _maxCachedResults = 200;
        
        // System references
        private GridPlacementController _placementController;
        
        // Unlock data
        private Dictionary<string, SchematicUnlockStatus> _unlockStatus = new Dictionary<string, SchematicUnlockStatus>();
        private List<SchematicSO> _allSchematics = new List<SchematicSO>();
        private Dictionary<string, List<string>> _dependencyGraph = new Dictionary<string, List<string>>();
        
        // Caching system
        private Dictionary<string, bool> _unlockCache = new Dictionary<string, bool>();
        private float _lastCacheRefresh = 0f;
        
        // Events
        public System.Action<SchematicSO> OnSchematicUnlocked;
        public System.Action<SchematicSO, float> OnSchematicPurchased;
        public System.Action<SchematicSO, string> OnUnlockFailed;
        public System.Action<int> OnUnlockProgressChanged;
        
        public override ManagerPriority Priority => ManagerPriority.Normal;
        
        // Public Properties
        public bool UnlockSystemEnabled => _enableUnlockSystem;
        public int TotalSchematics => _allSchematics.Count;
        public int UnlockedSchematics => _unlockStatus.Values.Count(s => s.IsUnlocked);
        public int AvailableForUnlock => _unlockStatus.Values.Count(s => s.CanUnlock && !s.IsUnlocked);
        public float UnlockProgress => TotalSchematics > 0 ? (float)UnlockedSchematics / TotalSchematics : 0f;
        
        protected override void OnManagerInitialize()
        {
            FindSystemReferences();
            InitializeUnlockSystem();
            LoadSchematicsData();
            BuildDependencyGraph();
            RefreshUnlockStatus();
            
            if (_autoUnlockBasicSchematics)
            {
                UnlockBasicSchematics();
            }
            
            LogInfo($"SchematicUnlockManager initialized - {TotalSchematics} schematics, {UnlockedSchematics} unlocked");
        }
        
        private void Update()
        {
            if (!IsInitialized || !_enableUnlockSystem) return;
            
            // Refresh unlock cache periodically
            if (_enableUnlockCaching && Time.time - _lastCacheRefresh >= _cacheRefreshInterval)
            {
                RefreshUnlockCache();
                _lastCacheRefresh = Time.time;
            }
        }
        
        /// <summary>
        /// Check if a schematic is unlocked for the player
        /// </summary>
        public bool IsSchematicUnlocked(SchematicSO schematic)
        {
            if (!_enableUnlockSystem || schematic == null) return true;
            
            // Check cache first
            if (_enableUnlockCaching && _unlockCache.TryGetValue(schematic.name, out bool cachedResult))
            {
                return cachedResult;
            }
            
            // Calculate unlock status
            bool isUnlocked = CalculateUnlockStatus(schematic);
            
            // Cache result
            if (_enableUnlockCaching)
            {
                _unlockCache[schematic.name] = isUnlocked;
            }
            
            return isUnlocked;
        }
        
        /// <summary>
        /// Check if a schematic can be unlocked (meets all requirements)
        /// </summary>
        public bool CanUnlockSchematic(SchematicSO schematic)
        {
            if (!_enableUnlockSystem || schematic == null || !schematic.RequiresUnlock)
                return true;
            
            if (IsSchematicUnlocked(schematic))
                return false; // Already unlocked
            
            // Check all unlock requirements
            var requirements = GetUnlockRequirements(schematic);
            return requirements.AllRequirementsMet;
        }
        
        /// <summary>
        /// Attempt to unlock a schematic using skill points
        /// </summary>
        public bool UnlockSchematic(SchematicSO schematic)
        {
            if (schematic == null)
            {
                LogWarning("Cannot unlock null schematic");
                return false;
            }
            
            if (!_enableUnlockSystem)
            {
                LogInfo($"Unlock system disabled - automatically unlocking {schematic.SchematicName}");
                return true;
            }
            
            if (IsSchematicUnlocked(schematic))
            {
                LogInfo($"Schematic already unlocked: {schematic.SchematicName}");
                return true;
            }
            
            if (!schematic.RequiresUnlock)
            {
                // Schematic doesn't require unlock - mark as unlocked
                SetSchematicUnlocked(schematic, true);
                return true;
            }
            
            // Validate unlock requirements
            var requirements = GetUnlockRequirements(schematic);
            if (!requirements.AllRequirementsMet)
            {
                var reason = GetUnlockFailureReason(requirements);
                OnUnlockFailed?.Invoke(schematic, reason);
                LogWarning($"Cannot unlock {schematic.SchematicName}: {reason}");
                return false;
            }
            
            // Process skill point payment
            if (schematic.SkillPointCost > 0)
            {
                var progressionManager = GameManager.Instance?.GetComponent<MonoBehaviour>() as IProgressionManager ??
                                        FindObjectOfType<MonoBehaviour>() as IProgressionManager;
                bool paymentSuccess = false;
                
                if (progressionManager != null && progressionManager.SkillPoints >= schematic.SkillPointCost)
                {
                    // Deduct skill points for the unlock
                    progressionManager.SpendSkillPoints((int)schematic.SkillPointCost);
                    paymentSuccess = true;
                }
                
                if (!paymentSuccess)
                {
                    OnUnlockFailed?.Invoke(schematic, "Insufficient skill points");
                    LogWarning($"Insufficient skill points to unlock {schematic.SchematicName}. Required: {schematic.SkillPointCost}, Available: {progressionManager?.SkillPoints ?? 0}");
                    return false;
                }
                
                OnSchematicPurchased?.Invoke(schematic, schematic.SkillPointCost);
            }
            
            // Unlock the schematic
            SetSchematicUnlocked(schematic, true);
            
            // Refresh dependent schematics
            RefreshDependentSchematics(schematic);
            
            // Trigger events
            OnSchematicUnlocked?.Invoke(schematic);
            OnUnlockProgressChanged?.Invoke(UnlockedSchematics);
            
            if (_showUnlockNotifications)
            {
                ShowUnlockNotification(schematic);
            }
            
            LogInfo($"Successfully unlocked schematic: {schematic.SchematicName} for {schematic.SkillPointCost} skill points");
            return true;
        }
        
        /// <summary>
        /// Get detailed unlock requirements for a schematic
        /// </summary>
        public SchematicUnlockRequirements GetUnlockRequirements(SchematicSO schematic)
        {
            var requirements = new SchematicUnlockRequirements
            {
                Schematic = schematic,
                RequiresUnlock = schematic.RequiresUnlock
            };
            
            if (!schematic.RequiresUnlock)
            {
                requirements.AllRequirementsMet = true;
                return requirements;
            }
            
            // Check skill point requirement
            requirements.SkillPointCost = schematic.SkillPointCost;
            var progressionManager = GameManager.Instance?.GetComponent<MonoBehaviour>() as IProgressionManager ??
                                    FindObjectOfType<MonoBehaviour>() as IProgressionManager;
            requirements.HasSufficientSkillPoints = progressionManager != null && 
                                                   progressionManager.SkillPoints >= schematic.SkillPointCost;
            
            // Check level requirement
            requirements.RequiredLevel = schematic.RequiredLevel;
            requirements.CurrentLevel = GetPlayerLevel();
            requirements.MeetsLevelRequirement = requirements.CurrentLevel >= requirements.RequiredLevel;
            
            // Check prerequisite schematics
            requirements.PrerequisiteSchematicIds = new List<string>(schematic.PrerequisiteSchematicIds);
            requirements.UnlockedPrerequisites = new List<string>();
            requirements.MissingPrerequisites = new List<string>();
            
            foreach (var prereqId in schematic.PrerequisiteSchematicIds)
            {
                var prereqSchematic = FindSchematicById(prereqId);
                if (prereqSchematic != null && IsSchematicUnlocked(prereqSchematic))
                {
                    requirements.UnlockedPrerequisites.Add(prereqId);
                }
                else
                {
                    requirements.MissingPrerequisites.Add(prereqId);
                }
            }
            
            requirements.MeetsPrerequisiteRequirement = requirements.MissingPrerequisites.Count == 0;
            
            // Calculate overall status
            requirements.AllRequirementsMet = requirements.HasSufficientSkillPoints &&
                                           requirements.MeetsLevelRequirement &&
                                           requirements.MeetsPrerequisiteRequirement;
            
            return requirements;
        }
        
        /// <summary>
        /// Get all schematics that can currently be unlocked
        /// </summary>
        public List<SchematicSO> GetUnlockableSchematics()
        {
            var unlockable = new List<SchematicSO>();
            
            foreach (var schematic in _allSchematics)
            {
                if (CanUnlockSchematic(schematic))
                {
                    unlockable.Add(schematic);
                }
            }
            
            return unlockable.OrderBy(s => s.SkillPointCost).ThenBy(s => s.RequiredLevel).ToList();
        }
        
        /// <summary>
        /// Get schematics that are locked but will become unlockable with progression
        /// </summary>
        public List<SchematicSO> GetProgressionLockedSchematics()
        {
            var progressionLocked = new List<SchematicSO>();
            
            foreach (var schematic in _allSchematics)
            {
                if (!IsSchematicUnlocked(schematic) && schematic.RequiresUnlock)
                {
                    var requirements = GetUnlockRequirements(schematic);
                    if (!requirements.AllRequirementsMet)
                    {
                        progressionLocked.Add(schematic);
                    }
                }
            }
            
            return progressionLocked.OrderBy(s => s.RequiredLevel).ThenBy(s => s.SkillPointCost).ToList();
        }
        
        /// <summary>
        /// Get unlock status for UI display
        /// </summary>
        public SchematicUnlockDisplayData GetSchematicDisplayData(SchematicSO schematic)
        {
            var displayData = new SchematicUnlockDisplayData
            {
                Schematic = schematic,
                IsUnlocked = IsSchematicUnlocked(schematic),
                CanUnlock = CanUnlockSchematic(schematic),
                Requirements = GetUnlockRequirements(schematic)
            };
            
            if (!displayData.IsUnlocked && schematic.RequiresUnlock)
            {
                displayData.UnlockHint = GenerateUnlockHint(displayData.Requirements);
                displayData.ProgressPercentage = CalculateUnlockProgress(displayData.Requirements);
            }
            else
            {
                displayData.ProgressPercentage = 1f;
            }
            
            return displayData;
        }
        
        /// <summary>
        /// Set unlock status for a schematic (for save/load or admin functions)
        /// </summary>
        public void SetSchematicUnlocked(SchematicSO schematic, bool unlocked)
        {
            if (schematic == null) return;
            
            var status = GetOrCreateUnlockStatus(schematic);
            status.IsUnlocked = unlocked;
            status.UnlockDate = unlocked ? System.DateTime.Now : System.DateTime.MinValue;
            
            // Clear cache for this schematic
            if (_enableUnlockCaching)
            {
                _unlockCache.Remove(schematic.name);
            }
            
            LogInfo($"Set {schematic.SchematicName} unlock status to: {unlocked}");
        }
        
        /// <summary>
        /// Reset all unlock progress (for new game or testing)
        /// </summary>
        public void ResetAllUnlocks()
        {
            _unlockStatus.Clear();
            _unlockCache.Clear();
            
            if (_autoUnlockBasicSchematics)
            {
                UnlockBasicSchematics();
            }
            
            OnUnlockProgressChanged?.Invoke(UnlockedSchematics);
            LogInfo("Reset all schematic unlocks");
        }
        
        private void FindSystemReferences()
        {
            _placementController = FindObjectOfType<GridPlacementController>();
            
            // Note: Economy system interactions handled through events to prevent circular dependencies
            LogInfo("SchematicUnlockManager initialized with event-based economy integration");
        }
        
        private void InitializeUnlockSystem()
        {
            _unlockStatus = new Dictionary<string, SchematicUnlockStatus>();
            _dependencyGraph = new Dictionary<string, List<string>>();
            
            if (_enableUnlockCaching)
            {
                _unlockCache = new Dictionary<string, bool>();
            }
        }
        
        private void LoadSchematicsData()
        {
            // Load all schematics from Resources
            var schematics = Resources.LoadAll<SchematicSO>("");
            _allSchematics.AddRange(schematics);
            
            LogInfo($"Loaded {_allSchematics.Count} schematics for unlock system");
        }
        
        private void BuildDependencyGraph()
        {
            _dependencyGraph.Clear();
            
            foreach (var schematic in _allSchematics)
            {
                if (schematic.PrerequisiteSchematicIds.Count > 0)
                {
                    _dependencyGraph[schematic.name] = new List<string>(schematic.PrerequisiteSchematicIds);
                }
            }
            
            LogInfo($"Built dependency graph with {_dependencyGraph.Count} dependent schematics");
        }
        
        private void RefreshUnlockStatus()
        {
            foreach (var schematic in _allSchematics)
            {
                GetOrCreateUnlockStatus(schematic);
            }
        }
        
        private void UnlockBasicSchematics()
        {
            var basicSchematics = _allSchematics.Where(s => 
                !s.RequiresUnlock || 
                (s.RequiredLevel <= 1 && s.SkillPointCost <= 0 && s.PrerequisiteSchematicIds.Count == 0)).ToList();
            
            foreach (var schematic in basicSchematics)
            {
                SetSchematicUnlocked(schematic, true);
            }
            
            LogInfo($"Auto-unlocked {basicSchematics.Count} basic schematics");
        }
        
        private bool CalculateUnlockStatus(SchematicSO schematic)
        {
            if (!schematic.RequiresUnlock) return true;
            
            var status = GetOrCreateUnlockStatus(schematic);
            return status.IsUnlocked;
        }
        
        private SchematicUnlockStatus GetOrCreateUnlockStatus(SchematicSO schematic)
        {
            if (!_unlockStatus.TryGetValue(schematic.name, out var status))
            {
                status = new SchematicUnlockStatus
                {
                    SchematicId = schematic.name,
                    SchematicName = schematic.SchematicName,
                    IsUnlocked = false,
                    UnlockDate = System.DateTime.MinValue,
                    SkillPointsSpent = 0f
                };
                _unlockStatus[schematic.name] = status;
            }
            
            return status;
        }
        
        private void RefreshUnlockCache()
        {
            if (!_enableUnlockCaching) return;
            
            _unlockCache.Clear();
            
            foreach (var schematic in _allSchematics)
            {
                _unlockCache[schematic.name] = CalculateUnlockStatus(schematic);
            }
            
            // Limit cache size
            if (_unlockCache.Count > _maxCachedResults)
            {
                var keysToRemove = _unlockCache.Keys.Take(_unlockCache.Count - _maxCachedResults).ToList();
                foreach (var key in keysToRemove)
                {
                    _unlockCache.Remove(key);
                }
            }
        }
        
        private void RefreshDependentSchematics(SchematicSO unlockedSchematic)
        {
            // Find all schematics that depend on this one
            var dependentSchematics = _allSchematics.Where(s => 
                s.PrerequisiteSchematicIds.Contains(unlockedSchematic.name)).ToList();
            
            // Clear cache for dependent schematics
            if (_enableUnlockCaching)
            {
                foreach (var dependent in dependentSchematics)
                {
                    _unlockCache.Remove(dependent.name);
                }
            }
            
            LogInfo($"Refreshed {dependentSchematics.Count} dependent schematics after unlocking {unlockedSchematic.SchematicName}");
        }
        
        private SchematicSO FindSchematicById(string schematicId)
        {
            return _allSchematics.FirstOrDefault(s => s.name == schematicId);
        }
        
        private int GetPlayerLevel()
        {
            // In a full implementation, this would get from progression system
            // For now, return a basic level calculation based on unlocked schematics
            var totalSkillPointsSpent = _unlockStatus.Values.Sum(s => s.SkillPointsSpent);
            return _playerStartingLevel + Mathf.FloorToInt(totalSkillPointsSpent / 10f);
        }
        
        /// <summary>
        /// Calculate skill point progress for unlock progress calculation
        /// </summary>
        private float CalculateSkillPointProgress(float requiredSkillPoints)
        {
            // Try to get current skill points from progression manager
            var progressionManager = GameManager.Instance?.GetComponent<MonoBehaviour>() as IProgressionManager ??
                                    FindObjectOfType<MonoBehaviour>() as IProgressionManager;
            
            if (progressionManager != null)
            {
                float currentSkillPoints = progressionManager.SkillPoints;
                return Mathf.Clamp01(currentSkillPoints / requiredSkillPoints);
            }
            
            // Fallback: assume we have sufficient skill points if we can't check
            return 1f;
        }
        
        private string GetUnlockFailureReason(SchematicUnlockRequirements requirements)
        {
            if (!requirements.HasSufficientSkillPoints)
                return $"Need {requirements.SkillPointCost} skill points";
            
            if (!requirements.MeetsLevelRequirement)
                return $"Need level {requirements.RequiredLevel} (currently level {requirements.CurrentLevel})";
            
            if (!requirements.MeetsPrerequisiteRequirement)
            {
                var missingNames = requirements.MissingPrerequisites.Select(id => 
                {
                    var schematic = FindSchematicById(id);
                    return schematic?.SchematicName ?? id;
                }).ToList();
                return $"Missing prerequisites: {string.Join(", ", missingNames)}";
            }
            
            return "Unknown requirement not met";
        }
        
        private string GenerateUnlockHint(SchematicUnlockRequirements requirements)
        {
            if (!requirements.HasSufficientSkillPoints)
                return $"Earn {requirements.SkillPointCost} skill points";
            
            if (!requirements.MeetsLevelRequirement)
                return $"Reach level {requirements.RequiredLevel}";
            
            if (!requirements.MeetsPrerequisiteRequirement && requirements.MissingPrerequisites.Count > 0)
            {
                var firstMissing = FindSchematicById(requirements.MissingPrerequisites[0]);
                return $"First unlock: {firstMissing?.SchematicName ?? "Unknown"}";
            }
            
            return "Requirements met - ready to unlock!";
        }
        
        private float CalculateUnlockProgress(SchematicUnlockRequirements requirements)
        {
            float progress = 0f;
            int totalRequirements = 0;
            
            // Skill points progress
            if (requirements.SkillPointCost > 0)
            {
                totalRequirements++;
                var skillPointProgress = CalculateSkillPointProgress(requirements.SkillPointCost);
                progress += skillPointProgress;
            }
            
            // Level progress
            if (requirements.RequiredLevel > 1)
            {
                totalRequirements++;
                var levelProgress = Mathf.Clamp01((float)requirements.CurrentLevel / requirements.RequiredLevel);
                progress += levelProgress;
            }
            
            // Prerequisites progress
            if (requirements.PrerequisiteSchematicIds.Count > 0)
            {
                totalRequirements++;
                var prereqProgress = (float)requirements.UnlockedPrerequisites.Count / requirements.PrerequisiteSchematicIds.Count;
                progress += prereqProgress;
            }
            
            return totalRequirements > 0 ? progress / totalRequirements : 1f;
        }
        
        private void ShowUnlockNotification(SchematicSO schematic)
        {
            // In a full implementation, this would show a UI notification
            LogInfo($"🎯 Blueprint Unlocked: {schematic.SchematicName}!");
        }
        
        protected override void OnManagerShutdown()
        {
            LogInfo($"SchematicUnlockManager shutdown - {UnlockedSchematics}/{TotalSchematics} schematics unlocked");
        }
    }
}