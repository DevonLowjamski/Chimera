using UnityEngine;
using ProjectChimera.Systems.Genetics;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Systems.Services.Commands;
using ProjectChimera.Systems.Services.Core;
using ProjectChimera.Core;
using System.Collections.Generic;

namespace ProjectChimera.Testing.Phase2_2
{
    /// <summary>
    /// Test suite for Phase 2.2.4: Breeding System Integration
    /// Validates "infinite diversity from seeds" implementation with minimal data serialization
    /// </summary>
    public class BreedingSystemIntegrationTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool _runTestsOnStart = true;
        [SerializeField] private bool _enableDetailedLogging = true;
        [SerializeField] private int _testIterations = 10;
        
        [Header("Test Results")]
        [SerializeField] private bool _allTestsPassed = false;
        [SerializeField] private int _totalTests = 0;
        [SerializeField] private int _passedTests = 0;
        [SerializeField] private List<string> _testResults = new List<string>();
        
        private BreedingSystemIntegration _breedingSystem;
        private MockGeneticsService _mockGeneticsService;
        private MockProgressionManager _mockProgressionManager;
        
        private void Start()
        {
            if (_runTestsOnStart)
            {
                RunAllTests();
            }
        }
        
        public void RunAllTests()
        {
            Debug.Log("[BreedingSystemIntegrationTest] Starting Phase 2.2.4 breeding system tests...");
            
            _testResults.Clear();
            _totalTests = 0;
            _passedTests = 0;
            
            SetupTestEnvironment();
            
            // Core breeding system tests
            TestBreedingSystemInitialization();
            TestMinimalSeedDataStructure();
            TestBreedingOperations();
            TestTissueCultureOperations();
            TestMicropropagationOperations();
            TestInfiniteDiversityFromSeeds();
            TestGeneticsCommandIntegration();
            TestBreedingValidation();
            TestDeterministicGeneration();
            TestPerformanceWithLargeDatasets();
            
            _allTestsPassed = (_passedTests == _totalTests);
            
            Debug.Log($"[BreedingSystemIntegrationTest] Tests completed: {_passedTests}/{_totalTests} passed");
            
            if (_allTestsPassed)
            {
                Debug.Log("✅ Phase 2.2.4: Breeding System Integration - ALL TESTS PASSED!");
            }
            else
            {
                Debug.LogWarning($"⚠️ Phase 2.2.4: Breeding System Integration - {_totalTests - _passedTests} tests failed");
            }
        }
        
        private void SetupTestEnvironment()
        {
            // Create or find breeding system
            _breedingSystem = FindObjectOfType<BreedingSystemIntegration>();
            if (_breedingSystem == null)
            {
                var go = new GameObject("Test_BreedingSystem");
                _breedingSystem = go.AddComponent<BreedingSystemIntegration>();
            }
            
            // Create mock services
            _mockGeneticsService = new MockGeneticsService();
            _mockProgressionManager = new MockProgressionManager();
        }
        
        private void TestBreedingSystemInitialization()
        {
            _totalTests++;
            bool passed = true;
            string testName = "Breeding System Initialization";
            
            try
            {
                if (_breedingSystem == null)
                {
                    passed = false;
                    LogTest(testName, false, "Breeding system not found");
                    return;
                }
                
                // Test that the system initializes properly
                if (_breedingSystem.GetSeedCount() < 0)
                {
                    passed = false;
                    LogTest(testName, false, "Invalid seed count after initialization");
                    return;
                }
                
                LogTest(testName, true, "Breeding system initialized successfully");
                _passedTests++;
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void TestMinimalSeedDataStructure()
        {
            _totalTests++;
            string testName = "Minimal Seed Data Structure";
            
            try
            {
                // Test that BreedingSeed contains only essential data
                var seed = new BreedingSeed
                {
                    ParentHash1 = "HASH1234",
                    ParentHash2 = "HASH5678",
                    PRNGSeed = 12345,
                    IsClone = false,
                    CreationTime = Time.time
                };
                
                // Verify minimal data approach
                bool hasMinimalData = !string.IsNullOrEmpty(seed.ParentHash1) &&
                                    !string.IsNullOrEmpty(seed.ParentHash2) &&
                                    seed.PRNGSeed != 0;
                
                if (hasMinimalData)
                {
                    LogTest(testName, true, "Seed structure contains minimal essential data");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, "Seed structure missing essential data");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void TestBreedingOperations()
        {
            _totalTests++;
            string testName = "Breeding Operations";
            
            try
            {
                var result = _breedingSystem.BreedPlants("plant1", "plant2");
                
                if (result.Success && !string.IsNullOrEmpty(result.SeedId))
                {
                    // Verify seed was created
                    bool hasSeed = _breedingSystem.HasSeed(result.SeedId);
                    
                    if (hasSeed)
                    {
                        LogTest(testName, true, $"Breeding successful, seed created: {result.SeedId}");
                        _passedTests++;
                    }
                    else
                    {
                        LogTest(testName, false, "Breeding succeeded but seed not found in bank");
                    }
                }
                else
                {
                    LogTest(testName, false, $"Breeding failed: {result.ErrorMessage}");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void TestTissueCultureOperations()
        {
            _totalTests++;
            string testName = "Tissue Culture Operations";
            
            try
            {
                bool success = _breedingSystem.CreateTissueCulture("plant1", "TestCulture");
                
                if (success)
                {
                    // Check if culture was created
                    int cultureCount = _breedingSystem.GetCultureCount();
                    
                    if (cultureCount > 0)
                    {
                        LogTest(testName, true, "Tissue culture created successfully");
                        _passedTests++;
                    }
                    else
                    {
                        LogTest(testName, false, "Culture creation succeeded but not found in collection");
                    }
                }
                else
                {
                    LogTest(testName, false, "Tissue culture creation failed");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void TestMicropropagationOperations()
        {
            _totalTests++;
            string testName = "Micropropagation Operations";
            
            try
            {
                // First create a culture
                bool cultureCreated = _breedingSystem.CreateTissueCulture("plant1", "PropagationTest");
                
                if (cultureCreated)
                {
                    var cultureIds = _breedingSystem.GetAvailableCultureIds();
                    if (cultureIds.Length > 0)
                    {
                        string cultureId = cultureIds[0];
                        bool success = _breedingSystem.Micropropagate(cultureId, 5, out string[] seedIds);
                        
                        if (success && seedIds.Length == 5)
                        {
                            LogTest(testName, true, $"Micropropagation successful, {seedIds.Length} clones created");
                            _passedTests++;
                        }
                        else
                        {
                            LogTest(testName, false, $"Micropropagation failed or incorrect seed count: {seedIds.Length}");
                        }
                    }
                    else
                    {
                        LogTest(testName, false, "No cultures available for micropropagation");
                    }
                }
                else
                {
                    LogTest(testName, false, "Could not create culture for micropropagation test");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void TestInfiniteDiversityFromSeeds()
        {
            _totalTests++;
            string testName = "Infinite Diversity From Seeds";
            
            try
            {
                var genotypes = new List<PlantGenotype>();
                
                // Generate multiple genotypes from same parents with different PRNG seeds
                for (int i = 0; i < 5; i++)
                {
                    var result = _breedingSystem.BreedPlants("parent1", "parent2");
                    if (result.Success)
                    {
                        var genotype = _breedingSystem.GenerateGenotypeFromSeed(result.SeedId);
                        if (genotype != null)
                        {
                            genotypes.Add(genotype);
                        }
                    }
                }
                
                // Verify diversity (all genotypes should be different)
                bool hasDiversity = true;
                for (int i = 0; i < genotypes.Count - 1; i++)
                {
                    for (int j = i + 1; j < genotypes.Count; j++)
                    {
                        if (genotypes[i].GenotypeID == genotypes[j].GenotypeID)
                        {
                            hasDiversity = false;
                            break;
                        }
                    }
                    if (!hasDiversity) break;
                }
                
                if (hasDiversity && genotypes.Count >= 3)
                {
                    LogTest(testName, true, $"Generated {genotypes.Count} diverse genotypes from same parents");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, $"Limited diversity: {genotypes.Count} genotypes, diversity={hasDiversity}");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void TestGeneticsCommandIntegration()
        {
            _totalTests++;
            string testName = "Genetics Command Integration";
            
            try
            {
                var breedCommand = new BreedPlantsCommand("plant1", "plant2", _mockGeneticsService, _mockProgressionManager);
                
                if (breedCommand.CanExecute())
                {
                    var result = breedCommand.Execute();
                    
                    if (result.IsSuccess)
                    {
                        LogTest(testName, true, "Genetics command integration working");
                        _passedTests++;
                    }
                    else
                    {
                        LogTest(testName, false, $"Command execution failed: {result.Message}");
                    }
                }
                else
                {
                    LogTest(testName, false, "Breed command cannot execute");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void TestBreedingValidation()
        {
            _totalTests++;
            string testName = "Breeding Validation";
            
            try
            {
                // Test invalid breeding scenarios
                var invalidResult1 = _breedingSystem.BreedPlants("", "plant2");
                var invalidResult2 = _breedingSystem.BreedPlants("plant1", "plant1"); // Self-breeding
                var invalidResult3 = _breedingSystem.BreedPlants(null, "plant2");
                
                bool validationWorking = !invalidResult1.Success && 
                                       !invalidResult2.Success && 
                                       !invalidResult3.Success;
                
                if (validationWorking)
                {
                    LogTest(testName, true, "Breeding validation working correctly");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, "Breeding validation not working properly");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void TestDeterministicGeneration()
        {
            _totalTests++;
            string testName = "Deterministic Generation";
            
            try
            {
                var result = _breedingSystem.BreedPlants("plant1", "plant2");
                
                if (result.Success)
                {
                    // Generate genotype twice from same seed
                    var genotype1 = _breedingSystem.GenerateGenotypeFromSeed(result.SeedId);
                    var genotype2 = _breedingSystem.GenerateGenotypeFromSeed(result.SeedId);
                    
                    if (genotype1 != null && genotype2 != null && 
                        genotype1.GenotypeID == genotype2.GenotypeID)
                    {
                        LogTest(testName, true, "Deterministic generation working - same seed produces same genotype");
                        _passedTests++;
                    }
                    else
                    {
                        LogTest(testName, false, "Deterministic generation failed - same seed produced different genotypes");
                    }
                }
                else
                {
                    LogTest(testName, false, "Could not create seed for deterministic test");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void TestPerformanceWithLargeDatasets()
        {
            _totalTests++;
            string testName = "Performance With Large Datasets";
            
            try
            {
                var startTime = Time.realtimeSinceStartup;
                int breedsToCreate = 100;
                int successfulBreeds = 0;
                
                for (int i = 0; i < breedsToCreate; i++)
                {
                    var result = _breedingSystem.BreedPlants($"plant{i % 10}", $"plant{(i + 1) % 10}");
                    if (result.Success)
                    {
                        successfulBreeds++;
                    }
                }
                
                var endTime = Time.realtimeSinceStartup;
                var duration = endTime - startTime;
                var breedsPerSecond = successfulBreeds / duration;
                
                if (breedsPerSecond > 50 && successfulBreeds > breedsToCreate * 0.7f)
                {
                    LogTest(testName, true, $"Performance good: {breedsPerSecond:F1} breeds/sec, {successfulBreeds}/{breedsToCreate} successful");
                    _passedTests++;
                }
                else
                {
                    LogTest(testName, false, $"Performance poor: {breedsPerSecond:F1} breeds/sec, {successfulBreeds}/{breedsToCreate} successful");
                }
            }
            catch (System.Exception ex)
            {
                LogTest(testName, false, $"Exception: {ex.Message}");
            }
        }
        
        private void LogTest(string testName, bool passed, string message)
        {
            string result = passed ? "✅ PASS" : "❌ FAIL";
            string fullMessage = $"{result}: {testName} - {message}";
            
            _testResults.Add(fullMessage);
            
            if (_enableDetailedLogging)
            {
                if (passed)
                    Debug.Log($"[BreedingSystemIntegrationTest] {fullMessage}");
                else
                    Debug.LogWarning($"[BreedingSystemIntegrationTest] {fullMessage}");
            }
        }
        
        // Mock services for testing
        private class MockGeneticsService : IGeneticsService
        {
            public string ServiceName => "MockGeneticsService";
            public bool IsInitialized => true;
            
            public bool CanBreedPlants(string parentId1, string parentId2) => 
                !string.IsNullOrEmpty(parentId1) && !string.IsNullOrEmpty(parentId2) && parentId1 != parentId2;
            public bool BreedPlants(string parentId1, string parentId2, out string newStrainId) { newStrainId = "MockStrain"; return true; }
            public bool CanCreateTissueCulture(string plantId) => !string.IsNullOrEmpty(plantId);
            public bool CreateTissueCulture(string plantId, string cultureName) => true;
            public bool CanMicropropagate(string cultureId) => !string.IsNullOrEmpty(cultureId);
            public bool Micropropagate(string cultureId, int quantity, out string[] seedIds) { seedIds = new string[quantity]; return true; }
            public PlantStrainSO GetStrain(string strainId) => null;
            public PlantStrainSO[] GetAvailableStrains() => new PlantStrainSO[0];
            public bool IsStrainUnlocked(string strainId) => true;
            public bool HasStrain(string strainId) => true;
            public bool HasSeeds(string strainId) => true;
            public int GetSeedCount(string strainId) => 10;
            public bool CanAffordSeeds(string strainId, int quantity) => true;
            public bool PurchaseSeeds(string strainId, int quantity) => true;
            public bool CanResearchTrait(string traitId) => true;
            public bool ResearchTrait(string traitId) => true;
            public bool IsTraitDiscovered(string traitId) => false;
            public int GetDiscoveredTraitCount() => 5;
            public int GetAvailableStrainCount() => 10;
            public float GetBreedingSuccessRate(string parentId1, string parentId2) => 0.8f;
            public ServiceValidationResult ValidateOperation(string operationType, params object[] parameters) => ServiceValidationResult.Success();
            public void Initialize() { }
            public void Shutdown() { }
        }
        
        private class MockProgressionManager : IProgressionManager
        {
            public string ManagerName => "MockProgressionManager";
            public bool IsInitialized => true;
            public int PlayerLevel => 1;
            public float CurrentExperience => 0f;
            public float ExperienceToNextLevel => 100f;
            public int SkillPoints => 100;
            public int UnlockedAchievements => 0;
            
            public bool IsSkillUnlocked(string skillId) => true;
            public bool IsAchievementUnlocked(string achievementId) => false;
            public void AddExperience(float amount, string source = null) { }
            public void AddSkillPoints(int amount, string source = null) { }
            public void SpendSkillPoints(int amount, string reason = null) { }
            public void UnlockSkill(string skillId) { }
            public void UnlockAchievement(string achievementId) { }
            public System.Collections.Generic.IEnumerable<string> GetUnlockedSkills() => new string[0];
            public System.Collections.Generic.IEnumerable<string> GetUnlockedAchievements() => new string[0];
            public void Initialize() { }
            public void Shutdown() { }

            // IChimeraManager implementation
            public ManagerMetrics GetMetrics()
            {
                return new ManagerMetrics
                {
                    ManagerName = ManagerName,
                    IsHealthy = true,
                    Performance = 1f,
                    ManagedItems = 0,
                    Uptime = 0f,
                    LastActivity = "Mock Test Implementation"
                };
            }

            public string GetStatus() => $"Mock Progression Manager - Level {PlayerLevel}, {SkillPoints} SP";
            public bool ValidateHealth() => true;
        }
    }
}