using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Simple validation runner to test migrated ServiceContainer functionality
/// Can be run independently to verify migrations are working correctly
/// </summary>
public class ServiceContainerMigrationValidator
{
    public static void Main(string[] args)
    {
        Console.WriteLine("🔍 ServiceContainer Migration Validation");
        Console.WriteLine("=" + new string('=', 50));
        
        var validator = new ServiceContainerMigrationValidator();
        var results = validator.RunValidationTests();
        
        validator.PrintResults(results);
    }
    
    public ValidationResults RunValidationTests()
    {
        var results = new ValidationResults();
        
        try
        {
            // Test 1: Verify migrated files exist and are accessible
            results.AddTest("File Accessibility", TestMigratedFilesExist());
            
            // Test 2: Check for FindObjectOfType elimination in migrated files
            results.AddTest("FindObjectOfType Elimination", TestFindObjectOfTypeElimination());
            
            // Test 3: Verify ServiceContainer usage patterns
            results.AddTest("ServiceContainer Usage", TestServiceContainerUsagePatterns());
            
            // Test 4: Check for proper fallback implementations
            results.AddTest("Fallback Implementation", TestFallbackImplementations());
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Critical validation error: {ex.Message}");
            results.CriticalError = ex.Message;
        }
        
        return results;
    }
    
    private TestResult TestMigratedFilesExist()
    {
        var result = new TestResult { TestName = "File Accessibility" };
        
        try
        {
            var migratedFiles = new[]
            {
                "Assets/ProjectChimera/Core/ManagerRegistry.cs",
                "Assets/ProjectChimera/Core/SimpleDI/SimpleManagerRegistry.cs", 
                "Assets/ProjectChimera/Core/GameSystemInitializer.cs",
                "Assets/ProjectChimera/Systems/Construction/GridInputHandler.cs",
                "Assets/ProjectChimera/Systems/Save/ConstructionSaveProvider.cs",
                "Assets/ProjectChimera/Systems/Construction/PlacementPaymentService.cs",
                "Assets/ProjectChimera/Systems/Construction/Payment/RefactoredPlacementPaymentService.cs"
            };
            
            int accessibleFiles = 0;
            foreach (var file in migratedFiles)
            {
                if (File.Exists(file))
                {
                    accessibleFiles++;
                    Console.WriteLine($"  ✅ {Path.GetFileName(file)}");
                }
                else
                {
                    Console.WriteLine($"  ❌ {Path.GetFileName(file)} - NOT FOUND");
                }
            }
            
            result.Passed = accessibleFiles == migratedFiles.Length;
            result.Details = $"{accessibleFiles}/{migratedFiles.Length} files accessible";
            
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.ErrorMessage = ex.Message;
        }
        
        return result;
    }
    
    private TestResult TestFindObjectOfTypeElimination()
    {
        var result = new TestResult { TestName = "FindObjectOfType Elimination" };
        
        try
        {
            var migratedFiles = new[]
            {
                "Assets/ProjectChimera/Core/ManagerRegistry.cs",
                "Assets/ProjectChimera/Core/SimpleDI/SimpleManagerRegistry.cs", 
                "Assets/ProjectChimera/Core/GameSystemInitializer.cs",
                "Assets/ProjectChimera/Systems/Construction/GridInputHandler.cs",
                "Assets/ProjectChimera/Systems/Save/ConstructionSaveProvider.cs",
                "Assets/ProjectChimera/Systems/Construction/PlacementPaymentService.cs",
                "Assets/ProjectChimera/Systems/Construction/Payment/RefactoredPlacementPaymentService.cs"
            };
            
            int violationCount = 0;
            int checkedFiles = 0;
            
            foreach (var file in migratedFiles)
            {
                if (File.Exists(file))
                {
                    checkedFiles++;
                    var content = File.ReadAllText(file);
                    
                    // Check for old patterns that should be eliminated
                    var oldPatterns = new[]
                    {
                        "FindObjectOfType<Camera>()",
                        "FindObjectOfType<MonoBehaviour>() as IConstructionSystem",
                        "GameObject.Find(\"CurrencyManager\")",
                        "GameObject.Find(\"TradingManager\")",
                        "FindObjectsOfType<ChimeraManager>()" // Should be replaced with ServiceContainer
                    };
                    
                    foreach (var pattern in oldPatterns)
                    {
                        if (content.Contains(pattern))
                        {
                            violationCount++;
                            Console.WriteLine($"  ⚠️ {Path.GetFileName(file)} still contains: {pattern}");
                        }
                    }
                    
                    // Check for new ServiceContainer patterns
                    var newPatterns = new[]
                    {
                        "ServiceContainerFactory.Instance",
                        "TryResolve<",
                        "ResolveAll<",
                        "RegisterInstance<"
                    };
                    
                    bool hasNewPatterns = newPatterns.Any(pattern => content.Contains(pattern));
                    if (hasNewPatterns)
                    {
                        Console.WriteLine($"  ✅ {Path.GetFileName(file)} uses ServiceContainer patterns");
                    }
                    else
                    {
                        Console.WriteLine($"  ❓ {Path.GetFileName(file)} may not be fully migrated");
                    }
                }
            }
            
            result.Passed = violationCount == 0;
            result.Details = $"Found {violationCount} old patterns in {checkedFiles} files";
            
            if (violationCount == 0)
            {
                Console.WriteLine("  🎉 No old FindObjectOfType patterns found in migrated files!");
            }
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.ErrorMessage = ex.Message;
        }
        
        return result;
    }
    
    private TestResult TestServiceContainerUsagePatterns()
    {
        var result = new TestResult { TestName = "ServiceContainer Usage" };
        
        try
        {
            var migratedFiles = new[]
            {
                "Assets/ProjectChimera/Core/ManagerRegistry.cs",
                "Assets/ProjectChimera/Core/SimpleDI/SimpleManagerRegistry.cs", 
                "Assets/ProjectChimera/Core/GameSystemInitializer.cs",
                "Assets/ProjectChimera/Systems/Construction/GridInputHandler.cs",
                "Assets/ProjectChimera/Systems/Save/ConstructionSaveProvider.cs",
                "Assets/ProjectChimera/Systems/Construction/PlacementPaymentService.cs",
                "Assets/ProjectChimera/Systems/Construction/Payment/RefactoredPlacementPaymentService.cs"
            };
            
            int filesWithServiceContainer = 0;
            int checkedFiles = 0;
            
            foreach (var file in migratedFiles)
            {
                if (File.Exists(file))
                {
                    checkedFiles++;
                    var content = File.ReadAllText(file);
                    
                    if (content.Contains("ServiceContainerFactory.Instance"))
                    {
                        filesWithServiceContainer++;
                        Console.WriteLine($"  ✅ {Path.GetFileName(file)} uses ServiceContainerFactory");
                    }
                    else
                    {
                        Console.WriteLine($"  ❌ {Path.GetFileName(file)} missing ServiceContainer integration");
                    }
                }
            }
            
            result.Passed = filesWithServiceContainer >= checkedFiles * 0.8f; // 80% should use ServiceContainer
            result.Details = $"{filesWithServiceContainer}/{checkedFiles} files use ServiceContainer";
            
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.ErrorMessage = ex.Message;
        }
        
        return result;
    }
    
    private TestResult TestFallbackImplementations()
    {
        var result = new TestResult { TestName = "Fallback Implementation" };
        
        try
        {
            var migratedFiles = new[]
            {
                "Assets/ProjectChimera/Core/ManagerRegistry.cs",
                "Assets/ProjectChimera/Systems/Construction/GridInputHandler.cs",
                "Assets/ProjectChimera/Systems/Save/ConstructionSaveProvider.cs",
                "Assets/ProjectChimera/Systems/Construction/PlacementPaymentService.cs"
            };
            
            int filesWithFallbacks = 0;
            int checkedFiles = 0;
            
            foreach (var file in migratedFiles)
            {
                if (File.Exists(file))
                {
                    checkedFiles++;
                    var content = File.ReadAllText(file);
                    
                    // Look for fallback patterns
                    bool hasFallback = content.Contains("else") && 
                                     (content.Contains("FindObjectOfType") || 
                                      content.Contains("FindObjectsByType") ||
                                      content.Contains("GameObject.Find"));
                    
                    if (hasFallback)
                    {
                        filesWithFallbacks++;
                        Console.WriteLine($"  ✅ {Path.GetFileName(file)} has fallback mechanisms");
                    }
                    else
                    {
                        Console.WriteLine($"  ❓ {Path.GetFileName(file)} may not have fallback mechanisms");
                    }
                }
            }
            
            result.Passed = filesWithFallbacks >= checkedFiles * 0.5f; // 50% should have fallbacks
            result.Details = $"{filesWithFallbacks}/{checkedFiles} files have fallback mechanisms";
            
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.ErrorMessage = ex.Message;
        }
        
        return result;
    }
    
    private void PrintResults(ValidationResults results)
    {
        Console.WriteLine("\n📊 VALIDATION RESULTS");
        Console.WriteLine("=" + new string('=', 50));
        
        if (!string.IsNullOrEmpty(results.CriticalError))
        {
            Console.WriteLine($"💥 Critical Error: {results.CriticalError}");
            return;
        }
        
        Console.WriteLine($"Total Tests: {results.TotalTests}");
        Console.WriteLine($"Passed: {results.PassedTests}");
        Console.WriteLine($"Failed: {results.FailedTests}");
        Console.WriteLine($"Success Rate: {(results.PassedTests * 100.0 / results.TotalTests):F1}%");
        
        Console.WriteLine("\nTest Details:");
        foreach (var test in results.TestResults)
        {
            var status = test.Passed ? "✅" : "❌";
            Console.WriteLine($"{status} {test.TestName}: {test.Details}");
            
            if (!test.Passed && !string.IsNullOrEmpty(test.ErrorMessage))
            {
                Console.WriteLine($"    Error: {test.ErrorMessage}");
            }
        }
        
        if (results.PassedTests == results.TotalTests)
        {
            Console.WriteLine("\n🎉 ALL VALIDATION TESTS PASSED!");
            Console.WriteLine("Migration appears to be successful.");
        }
        else if (results.PassedTests >= results.TotalTests * 0.8)
        {
            Console.WriteLine("\n⚠️ Most tests passed - Minor issues detected");
        }
        else
        {
            Console.WriteLine("\n❌ Significant issues detected");
            Console.WriteLine("Migration may need review.");
        }
    }
    
    public class ValidationResults
    {
        public List<TestResult> TestResults { get; } = new List<TestResult>();
        public string CriticalError { get; set; }
        
        public int TotalTests => TestResults.Count;
        public int PassedTests => TestResults.Count(t => t.Passed);
        public int FailedTests => TestResults.Count(t => !t.Passed);
        
        public void AddTest(string name, TestResult result)
        {
            result.TestName = name;
            TestResults.Add(result);
        }
    }
    
    public class TestResult
    {
        public string TestName { get; set; }
        public bool Passed { get; set; }
        public string Details { get; set; }
        public string ErrorMessage { get; set; }
    }
}