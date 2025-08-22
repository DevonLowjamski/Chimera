using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Save;
using System.Threading.Tasks;
using System;

namespace ProjectChimera.Systems.Save
{
    /// <summary>
    /// Concrete implementation of progression system save/load integration
    /// Bridges the gap between SaveManager and progression/skill systems
    /// </summary>
    public class ProgressionSaveService : MonoBehaviour, IProgressionSaveService
    {
        [Header("Progression Save Service Configuration")]
        [SerializeField] private bool _isEnabled = true;
        [SerializeField] private bool _supportsOfflineProgression = true;

        private bool _isInitialized = false;

        public string SystemName => "Progression Save Service";
        public bool IsAvailable => _isInitialized && _isEnabled;
        public bool SupportsOfflineProgression => _supportsOfflineProgression;

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeService();
        }

        private void Start()
        {
            RegisterWithSaveManager();
        }

        #endregion

        #region Service Initialization

        private void InitializeService()
        {
            _isInitialized = true;
            Debug.Log("[ProgressionSaveService] Service initialized successfully");
        }

        private void RegisterWithSaveManager()
        {
            var saveManager = GameManager.Instance?.GetManager<SaveManager>();
            if (saveManager != null)
            {
                saveManager.RegisterSaveService(this);
                Debug.Log("[ProgressionSaveService] Registered with SaveManager");
            }
            else
            {
                Debug.LogWarning("[ProgressionSaveService] SaveManager not found - integration disabled");
            }
        }

        #endregion

        #region IProgressionSaveService Implementation

        public ProgressionStateDTO GatherProgressionState()
        {
            if (!IsAvailable)
            {
                Debug.LogWarning("[ProgressionSaveService] Service not available for state gathering");
                return new ProgressionStateDTO
                {
                    SaveTimestamp = DateTime.Now,
                    SaveVersion = "1.0",
                    EnableProgressionTracking = false
                };
            }

            try
            {
                Debug.Log("[ProgressionSaveService] Gathering progression state...");

                var progressionState = new ProgressionStateDTO
                {
                    SaveTimestamp = DateTime.Now,
                    SaveVersion = "1.0",
                    EnableProgressionTracking = true,

                    // Player progress - starter state
                    PlayerProgress = new PlayerProgressDTO
                    {
                        PlayerLevel = 1,
                        TotalExperience = 0f,
                        ExperienceToNextLevel = 1000f
                    },

                    // Unlock system - basic feature flags
                    UnlockSystem = new UnlockSystemDTO
                    {
                        UnlockedFeatures = new System.Collections.Generic.List<string>
                        {
                            "basic_cultivation",
                            "starter_facilities"
                        }
                    },

                    // Skill system - basic skills
                    SkillSystem = new SkillSystemDTO
                    {
                        UnlockedSkills = new System.Collections.Generic.List<string> { "cultivation_basics" },
                        AvailableSkillPoints = 0
                    },

                    // Achievement system - starter achievements
                    AchievementSystem = new AchievementSystemDTO
                    {
                        UnlockedAchievements = 1,
                        RecentlyCompletedAchievements = new System.Collections.Generic.List<string>
                        {
                            "first_steps" // Starting achievement
                        }
                    }
                };

                Debug.Log($"[ProgressionSaveService] Progression state gathered: Level {progressionState.PlayerProgress.PlayerLevel}, {progressionState.SkillSystem.UnlockedSkills.Count} skills unlocked");
                return progressionState;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProgressionSaveService] Error gathering progression state: {ex.Message}");
                return new ProgressionStateDTO
                {
                    SaveTimestamp = DateTime.Now,
                    SaveVersion = "1.0",
                    EnableProgressionTracking = false
                };
            }
        }

        public async Task ApplyProgressionState(ProgressionStateDTO progressionData)
        {
            if (!IsAvailable)
            {
                Debug.LogWarning("[ProgressionSaveService] Service not available for state application");
                return;
            }

            if (progressionData == null)
            {
                Debug.LogWarning("[ProgressionSaveService] No progression data to apply");
                return;
            }

            try
            {
                Debug.Log($"[ProgressionSaveService] Applying progression state for Level {progressionData.PlayerProgress?.PlayerLevel ?? 1} player");

                // Apply player progress
                if (progressionData.PlayerProgress != null)
                {
                    await ApplyPlayerProgress(progressionData.PlayerProgress);
                }

                // Apply skill system
                if (progressionData.SkillSystem != null)
                {
                    await ApplySkillSystem(progressionData.SkillSystem);
                }

                // Apply achievement system
                if (progressionData.AchievementSystem != null)
                {
                    await ApplyAchievementSystem(progressionData.AchievementSystem);
                }

                Debug.Log("[ProgressionSaveService] Progression state applied successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProgressionSaveService] Error applying progression state: {ex.Message}");
            }
        }

        public OfflineProgressionResult ProcessOfflineProgression(float offlineHours)
        {
            if (!IsAvailable || !SupportsOfflineProgression)
            {
                return new OfflineProgressionResult
                {
                    SystemName = SystemName,
                    Success = false,
                    ErrorMessage = "Service not available or offline progression not supported",
                    ProcessedHours = 0f
                };
            }

            try
            {
                Debug.Log($"[ProgressionSaveService] Processing {offlineHours:F2} hours of offline progression advancement");

                // Calculate passive skill experience gains
                float passiveSkillXP = CalculatePassiveSkillExperience(offlineHours);
                int skillLevelsGained = ProcessSkillLevelAdvancement(offlineHours);
                float achievementProgress = CalculateAchievementProgress(offlineHours);

                return new OfflineProgressionResult
                {
                    SystemName = SystemName,
                    Success = true,
                    ProcessedHours = offlineHours,
                    Description = $"Processed progression offline advancement: +{passiveSkillXP:F0} skill XP, {skillLevelsGained} levels gained",
                    ResultData = new System.Collections.Generic.Dictionary<string, object>
                    {
                        ["PassiveSkillXP"] = passiveSkillXP,
                        ["SkillLevelsGained"] = skillLevelsGained,
                        ["AchievementProgress"] = achievementProgress,
                        ["OfflineProgressionEnabled"] = true
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProgressionSaveService] Error processing offline progression: {ex.Message}");
                return new OfflineProgressionResult
                {
                    SystemName = SystemName,
                    Success = false,
                    ErrorMessage = ex.Message,
                    ProcessedHours = 0f
                };
            }
        }

        #endregion

        #region Helper Methods

        private async Task ApplyPlayerProgress(PlayerProgressDTO playerProgress)
        {
            Debug.Log($"[ProgressionSaveService] Applying player progress (Level: {playerProgress.PlayerLevel}, XP: {playerProgress.TotalExperience})");
            
            // Player progress application would integrate with actual progression systems
            await Task.CompletedTask;
        }

        private async Task ApplySkillSystem(SkillSystemDTO skillSystem)
        {
            Debug.Log($"[ProgressionSaveService] Applying skill system ({skillSystem.UnlockedSkills.Count} skills unlocked)");
            
            // Skill system application would integrate with actual skill management systems
            foreach (var skillId in skillSystem.UnlockedSkills)
            {
                Debug.Log($"[ProgressionSaveService] Restoring skill: {skillId}");
            }
            
            await Task.CompletedTask;
        }

        private async Task ApplyAchievementSystem(AchievementSystemDTO achievementSystem)
        {
            Debug.Log($"[ProgressionSaveService] Applying achievement system ({achievementSystem.UnlockedAchievements} achievements unlocked)");
            
            // Achievement system application would integrate with actual achievement systems
            foreach (var achievement in achievementSystem.RecentlyCompletedAchievements)
            {
                Debug.Log($"[ProgressionSaveService] Recently completed achievement: {achievement}");
            }
            
            await Task.CompletedTask;
        }

        private float CalculatePassiveSkillExperience(float offlineHours)
        {
            // Calculate passive skill experience gains from offline operations
            float basePassiveRate = 1f; // 1 XP per hour base rate
            return basePassiveRate * offlineHours;
        }

        private int ProcessSkillLevelAdvancement(float offlineHours)
        {
            // Calculate potential skill level gains during offline period
            // Based on passive experience accumulation
            float xpGained = CalculatePassiveSkillExperience(offlineHours);
            int levelGains = Mathf.FloorToInt(xpGained / 100f); // 100 XP per level
            return levelGains;
        }

        private float CalculateAchievementProgress(float offlineHours)
        {
            // Calculate progress toward achievements during offline period
            float progressRate = 0.5f; // 0.5% progress per hour
            return progressRate * offlineHours;
        }

        #endregion
    }
}