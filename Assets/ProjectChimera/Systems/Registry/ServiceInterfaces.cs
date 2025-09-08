using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Economy;
using ProjectChimera.Data.Economy.Market;
using ProjectChimera.Data.Economy.Trading;
using ProjectChimera.Data.Economy.Configuration;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Genetics;

namespace ProjectChimera.Systems.Registry
{
    /// <summary>
    /// Comprehensive interface definitions for all 150+ specialized services
    /// Part of Module 2: Manager Decomposition - Service-Oriented Architecture
    /// </summary>

    #region Competition Services (CannabisCupManager → 4 services)

    /// <summary>
    /// PC014-1a: Competition Management Service Interface
    /// Handles tournament creation, scheduling, and lifecycle management
    /// </summary>
    public interface ICompetitionManagementService : IService
    {
        // Tournament Management
        string CreateTournament(string name, CompetitionType type, DateTime startDate, DateTime endDate);
        bool ScheduleCompetition(string tournamentId, DateTime scheduledDate);
        CompetitionStatus GetCompetitionStatus(string competitionId);
        List<Competition> GetActiveCompetitions();
        List<Competition> GetUpcomingCompetitions();
        bool CancelCompetition(string competitionId, string reason);

        // Event Lifecycle
        void StartCompetition(string competitionId);
        void EndCompetition(string competitionId);
        bool IsCompetitionActive(string competitionId);
        TimeSpan GetTimeUntilCompetition(string competitionId);

        // Rules and Formats
        void SetCompetitionRules(string competitionId, CompetitionRules rules);
        CompetitionRules GetCompetitionRules(string competitionId);
        List<CompetitionFormat> GetAvailableFormats();

        // Events
        event Action<string> OnCompetitionCreated;
        event Action<string> OnCompetitionStarted;
        event Action<string> OnCompetitionEnded;
    }

    /// <summary>
    /// PC014-1b: Judging Evaluation Service Interface
    /// Manages scoring algorithms, judge assignment, and results calculation
    /// </summary>
    public interface IJudgingEvaluationService : IService
    {
        // Scoring System
        float CalculateScore(string plantId, JudgingCriteria criteria);
        ScoreBreakdown GetDetailedScore(string plantId, string judgeId);
        void SubmitJudgeScore(string judgeId, string plantId, JudgeScorecard scorecard);

        // Judge Management
        bool AssignJudge(string judgeId, string competitionId);
        bool ValidateJudge(string judgeId);
        List<Judge> GetAssignedJudges(string competitionId);
        JudgeQualificationLevel GetJudgeLevel(string judgeId);

        // Results Processing
        CompetitionResults CalculateResults(string competitionId);
        PlantRanking GetPlantRanking(string competitionId);
        WinnerSelection DetermineWinners(string competitionId);
        bool ValidateResults(string competitionId);

        // Events
        event Action<string, string> OnScoreSubmitted; // judgeId, plantId
        event Action<string> OnResultsCalculated; // competitionId
    }

    /// <summary>
    /// PC014-1c: Participant Registration Service Interface
    /// Handles contestant registration, validation, and communication
    /// </summary>
    public interface IParticipantRegistrationService : IService
    {
        // Registration Management
        string RegisterParticipant(string playerId, string competitionId, PlantSubmission submission);
        bool ValidateRegistration(string registrationId);
        List<ParticipantRegistration> GetRegistrations(string competitionId);
        ParticipantRegistration GetRegistration(string registrationId);
        bool CancelRegistration(string registrationId);

        // Entry Management
        bool SubmitEntry(string registrationId, PlantEntry entry);
        PlantEntry GetEntry(string registrationId);
        bool ValidateEntry(PlantEntry entry, CompetitionRules rules);
        EntryStatus GetEntryStatus(string registrationId);

        // Qualification System
        bool CheckQualification(string playerId, CompetitionRequirements requirements);
        QualificationResult ValidateQualification(string playerId, string competitionId);
        List<string> GetQualificationRequirements(string competitionId);

        // Communication
        void NotifyParticipant(string registrationId, string message);
        void BroadcastToParticipants(string competitionId, string message);

        // Events
        event Action<string> OnParticipantRegistered;
        event Action<string> OnEntrySubmitted;
    }

    /// <summary>
    /// PC014-1d: Competition Rewards Service Interface
    /// Manages prize distribution and achievement integration
    /// </summary>
    public interface ICompetitionRewardsService : IService
    {
        // Prize System
        void DistributePrizes(string competitionId, CompetitionResults results);
        Prize GetPrize(PlacementPosition position, CompetitionType type);
        List<Prize> GetAvailablePrizes(string competitionId);
        bool ClaimPrize(string winnerId, string prizeId);

        // Achievement Integration
        void ProcessCompetitionAchievements(string competitionId, CompetitionResults results);
        List<Achievement> GetCompetitionAchievements();
        bool UnlockAchievement(string playerId, string achievementId);

        // Winner Recognition
        void RecognizeWinner(string winnerId, string competitionId, PlacementPosition position);
        WinnerProfile CreateWinnerProfile(string winnerId, CompetitionResults results);
        List<WinnerProfile> GetHallOfFame();

        // Reward History
        List<RewardHistory> GetPlayerRewards(string playerId);
        RewardStatistics GetRewardStatistics(string playerId);

        // Events
        event Action<string, string> OnPrizeDistributed; // winnerId, prizeId
        event Action<string, string> OnAchievementUnlocked; // playerId, achievementId
    }

    #endregion

    // Research Services section removed - Research namespace deleted during cleanup

    #region Progression Services (ComprehensiveProgressionManager → 5 services)

    /// <summary>
    /// PC014-3a: Experience Management Service Interface
    /// XP calculation, distribution, and level progression
    /// </summary>
    public interface IExperienceManagementService : IService
    {
        // Experience System - simplified after Progression namespace cleanup
        void AwardExperience(string playerId, string source, float amount);
        float GetExperience(string playerId);
        int GetLevel(string playerId);
        int GetLevelFromExperience(float experience);
        float GetExperienceForLevel(int level);
        float GetExperienceToNextLevel(string playerId);

        // Level Progression - simplified types
        bool CheckLevelUp(string playerId);
        string ProcessLevelUp(string playerId); // Simplified return type
        List<string> GetLevelRewards(int level); // Simplified to string list

        // Experience Sources - simplified
        void RegisterExperienceSource(string sourceName);
        List<string> GetExperienceSources();
        float GetExperienceMultiplier(string playerId);

        // Events
        event Action<string, float> OnExperienceAwarded;
        event Action<string, int> OnLevelUp;
    }

    /// <summary>
    /// PC014-3b: Skill Tree Management Service Interface
    /// Skill point allocation and ability unlock management
    /// </summary>
    public interface ISkillTreeManagementService : IService
    {
        // Skill Points
        void AwardSkillPoints(string playerId, int points);
        int GetAvailableSkillPoints(string playerId);
        int GetTotalSkillPoints(string playerId);
        bool SpendSkillPoints(string playerId, string skillId, int points);

        // Skill Management - simplified types
        bool UnlockSkill(string playerId, string skillId);
        bool IsSkillUnlocked(string playerId, string skillId);
        int GetSkillLevel(string playerId, string skillId);
        bool CanUnlockSkill(string playerId, string skillId);

        // Tree Navigation - simplified types
        List<string> GetAvailableSkills(string playerId);
        List<string> GetUnlockedSkills(string playerId);
        List<string> GetSkillDependencies(string skillId);
        string FindOptimalSkillPath(string playerId, string targetSkillId); // Simplified return type

        // Events
        event Action<string, string> OnSkillUnlocked;
        event Action<string, int> OnSkillPointsAwarded;
    }

    /// <summary>
    /// PC014-3c: Progression Achievement Service Interface
    /// Milestone tracking and achievement unlock logic
    /// </summary>
    public interface IProgressionAchievementService : IService
    {
        // Achievement Tracking - simplified after namespace cleanup
        void TrackAchievementProgress(string playerId, string achievementId, float progress);
        float GetAchievementProgress(string playerId, string achievementId);
        bool IsAchievementUnlocked(string playerId, string achievementId);
        List<Achievement> GetUnlockedAchievements(string playerId);

        // Milestone System - simplified types
        void RegisterMilestone(string playerId, string milestoneId);
        bool CheckMilestone(string playerId, string milestoneId);
        List<string> GetAchievedMilestones(string playerId);
        string GetMilestoneReward(string milestoneId); // Simplified return type

        // Reward Distribution - simplified types
        void DistributeAchievementReward(string playerId, string achievementId);
        List<string> GetPendingRewards(string playerId); // Simplified to string list
        void ClaimReward(string playerId, string rewardId);

        // Events
        event Action<string, string> OnAchievementUnlocked;
        event Action<string, string> OnMilestoneAchieved;
    }

    /// <summary>
    /// PC014-3d: Progression Analytics Service Interface
    /// Player progression data analysis and insights
    /// </summary>
    public interface IProgressionAnalyticsService : IService
    {
        // Analytics Collection - simplified after namespace cleanup
        void RecordProgressionEvent(string playerId, string eventName);
        string GetPlayerAnalytics(string playerId); // Simplified return type
        List<string> GetProgressionMetrics(string playerId, TimeSpan timeRange);

        // Performance Insights - simplified types
        string GenerateInsights(string playerId); // Simplified return type
        List<string> GetRecommendations(string playerId);
        float CalculateEfficiency(string playerId);

        // Comparative Analysis - simplified types
        int GetPlayerRanking(string playerId);
        string CompareWithPeers(string playerId); // Simplified return type
        List<string> GetBenchmarks();

        // Events
        event Action<string, string> OnProgressionEventRecorded;
        event Action<string, string> OnInsightGenerated;
    }

    /// <summary>
    /// PC014-3e: Milestone Tracking Service Interface
    /// Major milestone detection and long-term progression goals
    /// </summary>
    public interface IMilestoneTrackingService : IService
    {
        // Milestone Management - simplified after namespace cleanup
        void RegisterMilestone(string playerId, string milestoneId);
        bool CheckMilestoneCompletion(string playerId, string milestoneId);
        List<string> GetActiveMilestones(string playerId);
        List<string> GetCompletedMilestones(string playerId);

        // Progress Tracking
        void UpdateMilestoneProgress(string playerId, string milestoneId, float progress);
        float GetMilestoneProgress(string playerId, string milestoneId);
        TimeSpan GetEstimatedTimeToCompletion(string playerId, string milestoneId);

        // Reward System - simplified types
        void DistributeMilestoneReward(string playerId, string milestoneId);
        string GetMilestoneReward(string milestoneId); // Simplified return type
        List<string> GetPendingRewards(string playerId);

        // Long-term Goals - simplified types
        void SetLongTermGoal(string playerId, string goalDescription);
        string GetLongTermGoal(string playerId);
        float GetGoalProgress(string playerId);

        // Events
        event Action<string, string> OnMilestoneCompleted;
        event Action<string, string> OnMilestoneRewardDistributed;
    }

    #endregion

    #region Trading Services (TradingManager → 3 services)

    /// <summary>
    /// PC014-4a: Transaction Processing Service Interface
    /// Handles buy/sell transactions, payment processing, and transaction history
    /// </summary>
    public interface ITransactionProcessingService : IService
    {
        // Properties
        List<CompletedTransaction> TransactionHistory { get; }

        // Transaction Processing
        TransactionResult InitiateBuyTransaction(MarketProductSO product, float quantity, TradingPost tradingPost, PaymentMethod paymentMethod, string playerId);
        TransactionResult InitiateSellTransaction(InventoryItem inventoryItem, float quantity, TradingPost tradingPost, PaymentMethod paymentMethod, string playerId);
        bool CancelTransaction(string transactionId);
        string GetTransactionStatus(string transactionId);
        List<PendingTransaction> GetPendingTransactions(string playerId = null);

        // Payment Processing
        bool ProcessPayment(PendingTransaction transaction);
        List<PaymentMethod> GetAvailablePaymentMethods(string playerId);
        bool ValidatePaymentMethod(PaymentMethod paymentMethod, float transactionAmount);

        // Transaction History
        List<CompletedTransaction> GetTransactionHistory(string playerId = null, DateTime? startDate = null, DateTime? endDate = null);
        TradingPerformanceMetrics GetPerformanceMetrics(string playerId);

        // Events
        event Action<ProjectChimera.Data.Economy.CompletedTransaction> OnTransactionCompleted;
        event Action<PendingTransaction> OnTransactionStarted;
        event Action<string, string> OnTransactionFailed;
        event Action<string> OnTransactionCancelled;
    }

    /// <summary>
    /// PC014-4b: Trading Post Management Service Interface
    /// Manages trading posts, availability, pricing, and opportunities
    /// </summary>
    public interface ITradingPostManagementService : IService
    {
        // Trading Post Management
        List<TradingPost> GetAvailableTradingPosts();
        List<TradingPost> GetTradingPostsByType(TradingPostType type);
        TradingPost GetTradingPost(string tradingPostId);
        bool IsTradingPostAvailable(TradingPost tradingPost, MarketProductSO product, float quantity);
        TradingPostStatus GetTradingPostStatus(string tradingPostId);

        // Product Availability
        List<MarketProductSO> GetAvailableProducts(string tradingPostId);
        float GetProductQuantity(string tradingPostId, MarketProductSO product);
        bool IsProductAvailable(string tradingPostId, MarketProductSO product, float quantity);

        // Trading Opportunities
        List<TradingOpportunity> GetTradingOpportunities(OpportunityType opportunityType = OpportunityType.All);
        TradingOpportunity GetTradingOpportunity(string opportunityId);
        bool IsOpportunityValid(string opportunityId);
        void UpdateTradingOpportunities();

        // Events
        event Action<TradingPost> OnTradingPostStatusChanged;
        event Action<TradingOpportunity> OnTradingOpportunityAdded;
        event Action<string> OnTradingOpportunityExpired;
    }

    /// <summary>
    /// PC014-4c: Financial Management Service Interface
    /// Player finances, inventory tracking, and profitability analysis
    /// </summary>
    public interface IFinancialManagementService : IService
    {
        // Financial Management
        float GetCashBalance(string playerId);
        float GetNetWorth(string playerId);
        bool TransferCash(string playerId, float amount, ProjectChimera.Data.Economy.CashTransferType transferType);
        ProjectChimera.Data.Economy.FinancialMetrics GetFinancialMetrics(string playerId);

        // Inventory Management
        ProjectChimera.Data.Economy.PlayerInventory GetPlayerInventory(string playerId);
        List<ProjectChimera.Data.Economy.InventoryItem> GetInventoryForProduct(string playerId, MarketProductSO product);
        float GetTotalInventoryQuantity(string playerId, MarketProductSO product);
        bool AddToInventory(string playerId, InventoryItem item);
        bool RemoveFromInventory(string playerId, string itemId, float quantity);

        // Profitability Analysis
        TradingProfitabilityAnalysis AnalyzeProfitability(MarketProductSO product, float quantity, TradingTransactionType transactionType);
        float CalculateBreakEvenPrice(MarketProductSO product, float quantity);
        float EstimateProfit(MarketProductSO product, float quantity, float buyPrice, float sellPrice);

        // Events
        event Action<string, float, float> OnCashChanged; // playerId, oldAmount, newAmount
        event Action<string, ProjectChimera.Data.Economy.InventoryItem, float> OnInventoryChanged; // playerId, item, quantityChange
        event Action<string, ProjectChimera.Data.Economy.FinancialMetrics> OnFinancialMetricsUpdated; // playerId, metrics
    }

    #endregion

    #region SpeedTree Services (AdvancedSpeedTreeManager → 4 services)

    /// <summary>
    /// PC014-5a: SpeedTree Asset Management Service Interface
    /// Handles SpeedTree asset loading, renderer management, and cannabis-specific configurations
    /// </summary>
    public interface ISpeedTreeAssetService : IService
    {
        // Asset Management
        Task<UnityEngine.Object> LoadSpeedTreeAssetAsync(string assetPath);
        void UnloadSpeedTreeAsset(string assetPath);
        UnityEngine.Object GetSpeedTreeAssetForStrain(string strainId);
        bool IsAssetLoaded(string assetPath);

        // Renderer Management
        UnityEngine.GameObject CreateSpeedTreeRenderer(int plantId, Vector3 position, Quaternion rotation);
        void DestroySpeedTreeRenderer(UnityEngine.GameObject renderer);
        void ConfigureRendererForCannabis(UnityEngine.GameObject renderer, int plantId);

        // Material Management
        void ApplyGeneticVariationsToRenderer(UnityEngine.GameObject renderer, object genetics);
        void ApplyMorphologicalVariations(UnityEngine.GameObject renderer, object genetics);
        void UpdatePlantAppearanceForStage(int plantId, object stage);

        // Physics Integration
        void AddPhysicsInteraction(UnityEngine.GameObject renderer, int plantId);
        void RemovePhysicsInteraction(UnityEngine.GameObject renderer);

        // Events
        event System.Action<UnityEngine.GameObject> OnRendererCreated;
        event System.Action<UnityEngine.GameObject> OnRendererDestroyed;
        event System.Action<UnityEngine.Object> OnAssetLoaded;
    }

    /// <summary>
    /// PC014-5b: Cannabis Genetics Service Interface
    /// Manages genetic variation processing, growth stages, and cannabis-specific trait expression
    /// </summary>
    public interface ICannabisGeneticsService : IService
    {
        // Genetics Processing
        object GenerateGeneticVariation(string strainId, object genotype);
        void ProcessGeneticExpression(int plantId);
        void ValidateGeneticData(object genetics);

        // Growth Management
        void InitializePlantGrowth(int plantId);
        void UpdatePlantGrowth(int plantId, float deltaTime);
        void TriggerGrowthStageTransition(int plantId, object newStage);

        // Strain Management
        void RegisterStrain(string strainId, object strain);
        void UnregisterStrain(string strainId);
        object GetCannabisStrain(string strainId);

        // Growth Animation
        void AnimateStageTransition(int plantId, object oldStage, object newStage);
        void UpdateGrowthAnimations(System.Collections.Generic.IEnumerable<int> plantIds);

        // Events
        event System.Action<int, object, object> OnGrowthStageChanged;
        event System.Action<int, object> OnGeneticExpressionUpdated;
        event System.Action<string> OnStrainRegistered;
    }

    /// <summary>
    /// PC014-5c: Environmental Response Service Interface
    /// Handles environmental conditions, wind systems, stress visualization, and seasonal changes
    /// </summary>
    public interface ISpeedTreeEnvironmentalService : IService
    {
        // Environmental Response
        void UpdateEnvironmentalResponse(int plantId, object conditions);
        void ApplyEnvironmentalConditions(int plantId, object conditions);
        void UpdateSeasonalChanges(System.Collections.Generic.IEnumerable<int> plantIds);

        // Wind System
        void UpdateWindSystem();
        void UpdateWindZone(WindZone windZone);
        void ApplyWindSettings(object settings);
        void SetWindEnabled(bool enabled);

        // Stress Management
        void UpdateStressVisualization(System.Collections.Generic.IEnumerable<int> plantIds);
        void UpdatePlantHealthVisualization(int plantId, float health);
        void ApplyHealthVisualization(int plantId, float healthFactor, float stressFactor);

        // Lighting System
        void UpdatePlantLighting(int plantId, float intensity, Color color);
        void HandleLightingChange(object lightingConditions);

        // Events
        event System.Action<object> OnEnvironmentalConditionsChanged;
        event System.Action<float> OnWindStrengthChanged;
        event System.Action<int, float> OnPlantStressChanged;
    }

    /// <summary>
    /// PC014-5d: Performance Optimization Service Interface
    /// Manages LOD, batching, culling, performance metrics, and memory optimization
    /// </summary>
    public interface ISpeedTreePerformanceService : IService
    {
        // Performance Monitoring
        object GetCurrentMetrics();
        void UpdatePerformanceMetrics();
        void StartPerformanceMonitoring();
        void StopPerformanceMonitoring();

        // LOD Management
        void UpdateLODSystem(System.Collections.Generic.IEnumerable<int> plantIds);
        void SetQualityLevel(object quality);
        void ApplyQualitySettings(object quality);

        // Batching & Instancing
        void ProcessBatching();
        void RegisterRenderer(UnityEngine.GameObject renderer);
        void UnregisterRenderer(UnityEngine.GameObject renderer);
        void SetGPUInstancingEnabled(bool enabled);

        // Culling System
        void UpdateCullingSystem(System.Collections.Generic.IEnumerable<int> plantIds);
        void SetCullingDistance(float distance);
        int GetVisiblePlantCount();

        // Memory Management
        float GetMemoryUsage();
        void OptimizeMemoryUsage();
        void CleanupUnusedAssets();

        // Events
        event System.Action<object> OnPerformanceMetricsUpdated;
        event System.Action<object> OnQualityLevelChanged;
        event System.Action<float> OnMemoryUsageChanged;
    }

    #endregion

    // All data types moved to their respective Data namespace assemblies
    // ServiceInterfaces.cs only contains interface definitions
}
