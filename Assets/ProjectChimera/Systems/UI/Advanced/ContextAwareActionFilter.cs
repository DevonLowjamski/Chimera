using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Systems.UI.Advanced;
using ProjectChimera.Systems.Services.Core;
using ProjectChimera.Data.Shared;
using System.Collections.Generic;
using System.Linq;
using System;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.UI.Advanced
{
    /// <summary>
    /// Phase 2.3.2: Context-Aware Action Filtering
    /// Intelligently filters menu actions based on context, player state, and environmental factors
    /// Provides sophisticated relevance scoring and dynamic action prioritization
    /// </summary>
    public class ContextAwareActionFilter : MonoBehaviour
    {
        [Header("Filtering Configuration")]
        [SerializeField] private bool _enableIntelligentFiltering = true;
        [SerializeField] private bool _enableRelevanceScoring = true;
        [SerializeField] private bool _enableContextPrediction = true;
        [SerializeField] private bool _enableLearningSystem = false;

        [Header("Filtering Thresholds")]
        [SerializeField, Range(0f, 1f)] private float _minimumRelevanceScore = 0.3f;
        [SerializeField, Range(1, 50)] private int _maxActionsPerContext = 20;
        [SerializeField, Range(1, 10)] private int _maxCategoriesPerContext = 6;
        [SerializeField] private bool _showLowRelevanceActions = false;

        [Header("Context Analysis")]
        [SerializeField] private bool _analyzeObjectProperties = true;
        [SerializeField] private bool _analyzePlayerSkills = true;
        [SerializeField] private bool _analyzeEnvironmentalConditions = true;
        [SerializeField] private bool _analyzeResourceAvailability = true;
        [SerializeField] private bool _analyzeTimeContext = true;

        [Header("Learning System")]
        [SerializeField] private bool _trackActionUsage = true;
        [SerializeField] private bool _adaptToBehavior = true;
        [SerializeField] private int _learningDataRetention = 1000;

        // System references
        private ServiceLayerCoordinator _serviceCoordinator;
        private AdvancedMenuSystem _menuSystem;

        // Filtering data
        private Dictionary<string, ContextFilter> _contextFilters = new Dictionary<string, ContextFilter>();
        private Dictionary<string, ActionRelevanceData> _actionRelevance = new Dictionary<string, ActionRelevanceData>();
        private List<ActionUsageData> _usageHistory = new List<ActionUsageData>();

        // Context analysis cache
        private Dictionary<string, ContextAnalysisResult> _analysisCache = new Dictionary<string, ContextAnalysisResult>();
        private float _cacheExpiration = 5f;

        // Events
        public event Action<MenuContext, List<MenuAction>> OnActionsFiltered;
        public event Action<MenuContext, List<MenuCategory>> OnCategoriesFiltered;
        public event Action<string, float> OnRelevanceScoreUpdated;

        // Stub methods for test compilation - to be implemented by UI team
        public void FilterCategories(List<MenuCategory> categories, string contextType) { }
        public List<MenuCategory> FilterCategories(List<MenuCategory> categories, MenuContext context)
        {
            return categories;
        }
        public float CalculateRelevanceScore(MenuContext context) => 0.5f;
        public float CalculateRelevanceScore(string contextType) => 0.5f;
        public float CalculateRelevanceScore(MenuAction action, MenuContext context)
        {
            return 0.5f;
        }
        public void AnalyzeContextRelevance(MenuContext context, MenuAction action) { }
        public void AnalyzeContextRelevance(string contextType, string actionId) { }
        public void AnalyzeContextRelevance(MenuContext context, string actionId) { }
        public void AnalyzeContextRelevance(string contextType, MenuAction action) { }
        public void AnalyzeContextRelevance(List<MenuCategory> categories, MenuContext context) { }
        public float AnalyzeContextRelevance(MenuContext context)
        {
            return 0.5f;
        }
        public event Action<int> OnFilteringCompleted;

        private void Awake()
        {
            InitializeFilterSystem();
        }

        private void Start()
        {
            SetupContextFilters();
            StartCoroutine(UpdateFilteringSystem());
        }

        private void InitializeFilterSystem()
        {
            _serviceCoordinator = ServiceContainerFactory.Instance?.TryResolve<ServiceLayerCoordinator>();
            _menuSystem = GetComponent<AdvancedMenuSystem>();

            if (_menuSystem == null)
            {
                ChimeraLogger.LogError("[ContextAwareActionFilter] AdvancedMenuSystem component required");
                enabled = false;
                return;
            }

            // Subscribe to menu events
            _menuSystem.OnActionExecuted += OnActionExecuted;
            _menuSystem.OnContextChanged += OnContextChanged;
        }

        private void SetupContextFilters()
        {
            // Create filters for different context types
            RegisterContextFilter(new ContextFilter
            {
                ContextType = "Plant",
                RequiredComponents = new[] { "Plant", "PlantHealth", "PlantGrowth" },
                PreferredPillars = new[] { "Cultivation", "Genetics" },
                EnvironmentalFactors = new[] { "Temperature", "Humidity", "Light" },
                RelevanceMultiplier = 1.2f
            });

            RegisterContextFilter(new ContextFilter
            {
                ContextType = "Equipment",
                RequiredComponents = new[] { "Equipment", "PowerConsumer" },
                PreferredPillars = new[] { "Construction", "Cultivation" },
                EnvironmentalFactors = new[] { "Power", "Maintenance" },
                RelevanceMultiplier = 1.1f
            });

            RegisterContextFilter(new ContextFilter
            {
                ContextType = "Structure",
                RequiredComponents = new[] { "Structure", "Buildable" },
                PreferredPillars = new[] { "Construction" },
                EnvironmentalFactors = new[] { "Space", "Infrastructure" },
                RelevanceMultiplier = 1.0f
            });

            RegisterContextFilter(new ContextFilter
            {
                ContextType = "Ground",
                RequiredComponents = new string[0],
                PreferredPillars = new[] { "Construction", "Cultivation" },
                EnvironmentalFactors = new[] { "Space", "Terrain" },
                RelevanceMultiplier = 0.8f
            });
        }

        /// <summary>
        /// Filter actions based on current context with intelligent scoring
        /// </summary>
        public List<MenuAction> FilterActions(List<MenuAction> actions, MenuContext context)
        {
            if (!_enableIntelligentFiltering || actions == null || actions.Count == 0)
                return actions;

            var filteredActions = new List<MenuAction>();
            var contextKey = GenerateContextKey(context);

            // Get or create context analysis
            var analysis = GetContextAnalysis(context, contextKey);

            foreach (var action in actions)
            {
                var relevanceScore = CalculateActionRelevance(action, context, analysis);

                if (relevanceScore >= _minimumRelevanceScore || _showLowRelevanceActions)
                {
                    // Create scored action
                    var scoredAction = CloneActionWithScore(action, relevanceScore);
                    filteredActions.Add(scoredAction);

                    // Update relevance tracking
                    UpdateActionRelevance(action.Id, relevanceScore, context);
                }
            }

            // Sort by relevance score
            filteredActions = filteredActions
                .OrderByDescending(a => GetActionScore(a))
                .Take(_maxActionsPerContext)
                .ToList();

            OnActionsFiltered?.Invoke(context, filteredActions);
            return filteredActions;
        }

        /// <summary>
        /// Filter categories based on current context and available actions
        /// </summary>
        public List<MenuCategory> FilterCategories(List<MenuCategory> categories, MenuContext context, List<MenuAction> availableActions)
        {
            if (!_enableIntelligentFiltering || categories == null || categories.Count == 0)
                return categories;

            var filteredCategories = new List<MenuCategory>();
            var contextKey = GenerateContextKey(context);
            var analysis = GetContextAnalysis(context, contextKey);

            foreach (var category in categories)
            {
                var relevanceScore = CalculateCategoryRelevance(category, context, analysis, availableActions);

                if (relevanceScore >= _minimumRelevanceScore)
                {
                    var scoredCategory = CloneCategoryWithScore(category, relevanceScore);
                    filteredCategories.Add(scoredCategory);
                }
            }

            // Sort by relevance and pillar preference
            filteredCategories = filteredCategories
                .OrderByDescending(c => GetCategoryScore(c, analysis))
                .Take(_maxCategoriesPerContext)
                .ToList();

            OnCategoriesFiltered?.Invoke(context, filteredCategories);
            return filteredCategories;
        }

        private ContextAnalysisResult GetContextAnalysis(MenuContext context, string contextKey)
        {
            // Check cache first
            if (_analysisCache.TryGetValue(contextKey, out var cachedResult))
            {
                if (Time.time - cachedResult.Timestamp < _cacheExpiration)
                {
                    return cachedResult;
                }
            }

            // Perform new analysis
            var analysis = AnalyzeContext(context);
            _analysisCache[contextKey] = analysis;

            return analysis;
        }

        private ContextAnalysisResult AnalyzeContext(MenuContext context)
        {
            var analysis = new ContextAnalysisResult
            {
                Context = context,
                Timestamp = Time.time,
                ObjectProperties = new Dictionary<string, object>(),
                EnvironmentalFactors = new Dictionary<string, float>(),
                PlayerCapabilities = new Dictionary<string, float>(),
                ResourceAvailability = new Dictionary<string, float>()
            };

            // Analyze object properties
            if (_analyzeObjectProperties && context.TargetObject != null)
            {
                AnalyzeObjectProperties(context.TargetObject, analysis);
            }

            // Analyze player skills and capabilities
            if (_analyzePlayerSkills)
            {
                AnalyzePlayerCapabilities(analysis);
            }

            // Analyze environmental conditions
            if (_analyzeEnvironmentalConditions)
            {
                AnalyzeEnvironmentalConditions(context, analysis);
            }

            // Analyze resource availability
            if (_analyzeResourceAvailability)
            {
                AnalyzeResourceAvailability(analysis);
            }

            // Analyze time context
            if (_analyzeTimeContext)
            {
                AnalyzeTimeContext(context, analysis);
            }

            return analysis;
        }

        private void AnalyzeObjectProperties(GameObject targetObject, ContextAnalysisResult analysis)
        {
            // Analyze object components and properties
            var components = targetObject.GetComponents<MonoBehaviour>();

            foreach (var component in components)
            {
                var componentType = component.GetType().Name;
                analysis.ObjectProperties[componentType] = component;

                // Specific property analysis
                if (componentType.Contains("Plant"))
                {
                    analysis.ObjectProperties["IsPlant"] = true;
                    // Could analyze plant health, growth stage, etc.
                }
                else if (componentType.Contains("Equipment"))
                {
                    analysis.ObjectProperties["IsEquipment"] = true;
                    // Could analyze equipment status, efficiency, etc.
                }
                else if (componentType.Contains("Structure"))
                {
                    analysis.ObjectProperties["IsStructure"] = true;
                    // Could analyze structure integrity, capacity, etc.
                }
            }

            // Analyze tags and layers
            analysis.ObjectProperties["Tag"] = targetObject.tag;
            analysis.ObjectProperties["Layer"] = LayerMask.LayerToName(targetObject.layer);
        }

        private void AnalyzePlayerCapabilities(ContextAnalysisResult analysis)
        {
            // In a real implementation, this would query the progression manager
            analysis.PlayerCapabilities["construction_basic"] = 1f;
            analysis.PlayerCapabilities["cultivation_basic"] = 1f;
            analysis.PlayerCapabilities["breeding_basic"] = 0.8f;
            analysis.PlayerCapabilities["equipment_basic"] = 0.9f;
            analysis.PlayerCapabilities["plant_care"] = 0.7f;
            analysis.PlayerCapabilities["plant_training"] = 0.6f;
            analysis.PlayerCapabilities["tissue_culture"] = 0.4f;
            analysis.PlayerCapabilities["genetic_research"] = 0.3f;
        }

        private void AnalyzeEnvironmentalConditions(MenuContext context, ContextAnalysisResult analysis)
        {
            // Analyze environmental factors affecting action relevance
            analysis.EnvironmentalFactors["Temperature"] = 22f;
            analysis.EnvironmentalFactors["Humidity"] = 55f;
            analysis.EnvironmentalFactors["LightLevel"] = 800f;
            analysis.EnvironmentalFactors["CO2Level"] = 400f;
            analysis.EnvironmentalFactors["AirFlow"] = 0.5f;

            // Contextual environmental factors
            if (context.ContextType == "Plant")
            {
                analysis.EnvironmentalFactors["PlantStress"] = 0.2f;
                analysis.EnvironmentalFactors["GrowthRate"] = 0.8f;
            }
            else if (context.ContextType == "Equipment")
            {
                analysis.EnvironmentalFactors["PowerLoad"] = 0.7f;
                analysis.EnvironmentalFactors["MaintenanceNeeded"] = 0.1f;
            }
        }

        private void AnalyzeResourceAvailability(ContextAnalysisResult analysis)
        {
            // In a real implementation, this would query resource managers
            analysis.ResourceAvailability["Money"] = 10000f;
            analysis.ResourceAvailability["Power"] = 0.8f;
            analysis.ResourceAvailability["Water"] = 0.9f;
            analysis.ResourceAvailability["Seeds"] = 50f;
            analysis.ResourceAvailability["Nutrients"] = 100f;
            analysis.ResourceAvailability["Equipment"] = 20f;
            analysis.ResourceAvailability["Materials"] = 500f;
        }

        private void AnalyzeTimeContext(MenuContext context, ContextAnalysisResult analysis)
        {
            var currentTime = DateTime.Now;
            analysis.EnvironmentalFactors["TimeOfDay"] = currentTime.Hour;
            analysis.EnvironmentalFactors["DayOfWeek"] = (float)currentTime.DayOfWeek;
            analysis.EnvironmentalFactors["GameTime"] = Time.time;

            // Seasonal factors (simplified)
            var month = currentTime.Month;
            analysis.EnvironmentalFactors["Season"] = month switch
            {
                12 or 1 or 2 => 0f, // Winter
                3 or 4 or 5 => 1f, // Spring
                6 or 7 or 8 => 2f, // Summer
                9 or 10 or 11 => 3f, // Fall
                _ => 1f
            };
        }

        private float CalculateActionRelevance(MenuAction action, MenuContext context, ContextAnalysisResult analysis)
        {
            float relevanceScore = 0.5f; // Base relevance

            // Context type matching
            if (_contextFilters.TryGetValue(context.ContextType, out var contextFilter))
            {
                if (contextFilter.PreferredPillars.Contains(action.PillarType))
                {
                    relevanceScore += 0.3f;
                }
                relevanceScore *= contextFilter.RelevanceMultiplier;
            }

            // Player capability matching
            if (action.RequiredSkills != null)
            {
                foreach (var skill in action.RequiredSkills)
                {
                    if (analysis.PlayerCapabilities.TryGetValue(skill, out var capability))
                    {
                        relevanceScore += capability * 0.2f;
                    }
                    else
                    {
                        relevanceScore -= 0.1f; // Penalty for missing skills
                    }
                }
            }

            // Resource availability matching
            if (action.ResourceRequirements != null)
            {
                foreach (var requirement in action.ResourceRequirements)
                {
                    if (analysis.ResourceAvailability.TryGetValue(requirement.ResourceType, out var available))
                    {
                        if (available >= requirement.Amount)
                        {
                            relevanceScore += 0.1f;
                        }
                        else
                        {
                            relevanceScore -= 0.2f; // Penalty for insufficient resources
                        }
                    }
                }
            }

            // Environmental condition matching
            relevanceScore += CalculateEnvironmentalRelevance(action, analysis);

            // Usage history bonus
            if (_trackActionUsage)
            {
                var usageBonus = CalculateUsageBonus(action.Id, context);
                relevanceScore += usageBonus;
            }

            // Learning system adjustment
            if (_enableLearningSystem && _adaptToBehavior)
            {
                var learningAdjustment = CalculateLearningAdjustment(action.Id, context);
                relevanceScore += learningAdjustment;
            }

            return Mathf.Clamp01(relevanceScore);
        }

        private float CalculateCategoryRelevance(MenuCategory category, MenuContext context, ContextAnalysisResult analysis, List<MenuAction> availableActions)
        {
            float relevanceScore = 0.5f; // Base relevance

            // Context type matching
            if (_contextFilters.TryGetValue(context.ContextType, out var contextFilter))
            {
                if (contextFilter.PreferredPillars.Contains(category.PillarType))
                {
                    relevanceScore += 0.3f;
                }
            }

            // Count relevant actions in category
            var categoryActions = availableActions.Where(a => a.CategoryId == category.Id).ToList();
            var relevantActionCount = categoryActions.Count(a => GetActionScore(a) >= _minimumRelevanceScore);

            if (relevantActionCount > 0)
            {
                relevanceScore += (relevantActionCount / (float)categoryActions.Count) * 0.4f;
            }
            else
            {
                relevanceScore -= 0.3f; // Penalty for no relevant actions
            }

            return Mathf.Clamp01(relevanceScore);
        }

        private float CalculateEnvironmentalRelevance(MenuAction action, ContextAnalysisResult analysis)
        {
            float environmentalScore = 0f;

            // Action-specific environmental relevance
            switch (action.PillarType.ToLower())
            {
                case "cultivation":
                    if (analysis.EnvironmentalFactors.TryGetValue("PlantStress", out var stress))
                    {
                        if (action.Id.Contains("water") && stress > 0.3f)
                            environmentalScore += 0.2f;
                        if (action.Id.Contains("feed") && stress > 0.2f)
                            environmentalScore += 0.2f;
                    }
                    break;

                case "construction":
                    if (analysis.EnvironmentalFactors.TryGetValue("PowerLoad", out var powerLoad))
                    {
                        if (action.Id.Contains("power") && powerLoad > 0.8f)
                            environmentalScore += 0.3f;
                    }
                    break;

                case "genetics":
                    if (analysis.EnvironmentalFactors.TryGetValue("Season", out var season))
                    {
                        if (action.Id.Contains("breed") && (season == 1f || season == 2f)) // Spring/Summer
                            environmentalScore += 0.1f;
                    }
                    break;
            }

            return environmentalScore;
        }

        private float CalculateUsageBonus(string actionId, MenuContext context)
        {
            var recentUsage = _usageHistory
                .Where(u => u.ActionId == actionId && u.ContextType == context.ContextType)
                .Where(u => Time.time - u.Timestamp < 3600f) // Last hour
                .Count();

            return Mathf.Min(recentUsage * 0.05f, 0.2f); // Max 0.2 bonus
        }

        private float CalculateLearningAdjustment(string actionId, MenuContext context)
        {
            // In a real implementation, this would use machine learning
            // For now, simple pattern recognition
            var contextTypeUsage = _usageHistory
                .Where(u => u.ContextType == context.ContextType)
                .GroupBy(u => u.ActionId)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            if (contextTypeUsage?.Key == actionId)
            {
                return 0.1f; // Bonus for most used action in this context
            }

            return 0f;
        }

        private void RegisterContextFilter(ContextFilter filter)
        {
            _contextFilters[filter.ContextType] = filter;
        }

        private void OnActionExecuted(string actionId, MenuAction action)
        {
            if (_trackActionUsage)
            {
                var usageData = new ActionUsageData
                {
                    ActionId = actionId,
                    ContextType = "Unknown", // Would need to track current context
                    Timestamp = Time.time,
                    PillarType = action.PillarType
                };

                _usageHistory.Add(usageData);

                // Limit usage history size
                if (_usageHistory.Count > _learningDataRetention)
                {
                    _usageHistory.RemoveAt(0);
                }
            }
        }

        private void OnContextChanged(MenuContext newContext)
        {
            // Clear relevant cache entries when context changes
            var contextKey = GenerateContextKey(newContext);
            var keysToRemove = _analysisCache.Keys
                .Where(key => key.StartsWith(newContext.ContextType))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _analysisCache.Remove(key);
            }
        }

        private void UpdateActionRelevance(string actionId, float relevanceScore, MenuContext context)
        {
            if (!_actionRelevance.TryGetValue(actionId, out var data))
            {
                data = new ActionRelevanceData { ActionId = actionId };
                _actionRelevance[actionId] = data;
            }

            data.UpdateScore(relevanceScore, context.ContextType);
            OnRelevanceScoreUpdated?.Invoke(actionId, relevanceScore);
        }

        private MenuAction CloneActionWithScore(MenuAction original, float score)
        {
            var cloned = new MenuAction(original.Id, original.CategoryId, original.DisplayName, original.PillarType)
            {
                Description = original.Description,
                Icon = original.Icon,
                Priority = original.Priority,
                RequiredContext = original.RequiredContext,
                RequiredSkills = original.RequiredSkills,
                IsEnabled = original.IsEnabled,
                IsVisible = original.IsVisible,
                ActionColor = original.ActionColor,
                ResourceRequirements = original.ResourceRequirements,
                ConditionCallback = original.ConditionCallback,
                ExecutionConditionCallback = original.ExecutionConditionCallback
            };

            // Store relevance score in metadata
            cloned.Parameters["RelevanceScore"] = score;

            return cloned;
        }

        private MenuCategory CloneCategoryWithScore(MenuCategory original, float score)
        {
            var cloned = new MenuCategory(original.Id, original.DisplayName, original.PillarType)
            {
                Description = original.Description,
                Icon = original.Icon,
                Priority = original.Priority,
                RequiredContext = original.RequiredContext,
                RequiredSkills = original.RequiredSkills,
                IsDynamic = original.IsDynamic,
                IsVisible = original.IsVisible,
                CategoryColor = original.CategoryColor,
                ConditionCallback = original.ConditionCallback
            };

            // Store relevance score in metadata
            cloned.Metadata["RelevanceScore"] = score;

            return cloned;
        }

        private float GetActionScore(MenuAction action)
        {
            return action.Parameters.TryGetValue("RelevanceScore", out var score) ? (float)score : 0.5f;
        }

        private float GetCategoryScore(MenuCategory category, ContextAnalysisResult analysis)
        {
            var baseScore = category.Metadata.TryGetValue("RelevanceScore", out var score) ? (float)score : 0.5f;

            // Apply context filter multiplier
            if (_contextFilters.TryGetValue(analysis.Context.ContextType, out var filter))
            {
                baseScore *= filter.RelevanceMultiplier;
            }

            return baseScore;
        }

        private string GenerateContextKey(MenuContext context)
        {
            return $"{context.ContextType}_{context.TargetObject?.GetInstanceID() ?? 0}_{Mathf.FloorToInt(context.Timestamp / _cacheExpiration)}";
        }

        private System.Collections.IEnumerator UpdateFilteringSystem()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);

                // Clear expired cache entries
                var expiredKeys = _analysisCache
                    .Where(kvp => Time.time - kvp.Value.Timestamp > _cacheExpiration)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    _analysisCache.Remove(key);
                }
            }
        }

        // Public API
        public int GetCachedAnalysisCount() => _analysisCache.Count;
        public int GetUsageHistoryCount() => _usageHistory.Count;
        public float GetActionRelevance(string actionId, string contextType)
        {
            return _actionRelevance.TryGetValue(actionId, out var data)
                ? data.GetAverageScore(contextType)
                : 0.5f;
        }
    }

    // Supporting data structures
    [System.Serializable]
    public class ContextFilter
    {
        public string ContextType;
        public string[] RequiredComponents;
        public string[] PreferredPillars;
        public string[] EnvironmentalFactors;
        public float RelevanceMultiplier = 1f;
    }

    public class ContextAnalysisResult
    {
        public MenuContext Context;
        public float Timestamp;
        public Dictionary<string, object> ObjectProperties;
        public Dictionary<string, float> EnvironmentalFactors;
        public Dictionary<string, float> PlayerCapabilities;
        public Dictionary<string, float> ResourceAvailability;
    }

    public class ActionRelevanceData
    {
        public string ActionId;
        public Dictionary<string, List<float>> ContextScores = new Dictionary<string, List<float>>();
        public float LastUpdated;

        public void UpdateScore(float score, string contextType)
        {
            if (!ContextScores.TryGetValue(contextType, out var scores))
            {
                scores = new List<float>();
                ContextScores[contextType] = scores;
            }

            scores.Add(score);

            // Keep only recent scores
            if (scores.Count > 20)
            {
                scores.RemoveAt(0);
            }

            LastUpdated = Time.time;
        }

        public float GetAverageScore(string contextType)
        {
            if (ContextScores.TryGetValue(contextType, out var scores) && scores.Count > 0)
            {
                return scores.Average();
            }
            return 0.5f;
        }
    }

    public struct ActionUsageData
    {
        public string ActionId;
        public string ContextType;
        public string PillarType;
        public float Timestamp;
    }
}
