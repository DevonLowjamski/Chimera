using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;
using ProjectChimera.Systems.Registry;

namespace ProjectChimera.Systems.Registry
{
    /// <summary>
    /// PC014-4: Manager Decomposition Pipeline
    /// Standardized system for analyzing monolithic managers and creating specialized services
    /// Automates the decomposition process with consistent patterns and validation
    /// </summary>
    public class ManagerDecompositionPipeline : MonoBehaviour
    {
        #region Configuration
        
        [Header("Decomposition Settings")]
        [SerializeField] private int _maxServiceLines = 500;
        [SerializeField] private int _minServiceLines = 200;
        [SerializeField] private bool _generateInterfaces = true;
        [SerializeField] private bool _generateImplementations = true;
        [SerializeField] private bool _generateTests = true;
        [SerializeField] private bool _createDocumentation = true;
        
        [Header("File Paths")]
        [SerializeField] private string _servicesOutputPath = "Assets/ProjectChimera/Systems/Services/";
        [SerializeField] private string _interfacesOutputPath = "Assets/ProjectChimera/Systems/Interfaces/";
        [SerializeField] private string _testsOutputPath = "Assets/ProjectChimera/Testing/Services/";
        [SerializeField] private string _documentationPath = "Assets/ProjectChimera/Documentation/Services/";
        
        #endregion

        #region Public API
        
        /// <summary>
        /// Analyze a manager and generate decomposition plan
        /// </summary>
        public ManagerAnalysisResult AnalyzeManager(string managerFilePath)
        {
            if (!File.Exists(managerFilePath))
            {
                ChimeraLogger.LogError($"Manager file not found: {managerFilePath}");
                return null;
            }

            var result = new ManagerAnalysisResult
            {
                ManagerPath = managerFilePath,
                ManagerName = Path.GetFileNameWithoutExtension(managerFilePath),
                AnalysisDate = DateTime.Now
            };

            try
            {
                string content = File.ReadAllText(managerFilePath);
                result.TotalLines = content.Split('\n').Length;
                
                // Analyze structure
                AnalyzeManagerStructure(content, result);
                
                // Generate decomposition plan
                GenerateDecompositionPlan(result);
                
                // Validate decomposition feasibility
                ValidateDecomposition(result);
                
                ChimeraLogger.Log($"Analysis complete for {result.ManagerName}: {result.TotalLines} lines → {result.ProposedServices.Count} services");
                return result;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"Failed to analyze manager {managerFilePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Execute full decomposition pipeline for a manager
        /// </summary>
        public DecompositionResult DecomposeManager(ManagerAnalysisResult analysis)
        {
            if (analysis == null || !analysis.IsValid)
            {
                ChimeraLogger.LogError("Invalid analysis result provided for decomposition");
                return null;
            }

            var result = new DecompositionResult
            {
                ManagerName = analysis.ManagerName,
                DecompositionDate = DateTime.Now,
                OriginalLines = analysis.TotalLines,
                TargetServices = analysis.ProposedServices.Count
            };

            try
            {
                // Create service interfaces
                if (_generateInterfaces)
                {
                    CreateServiceInterfaces(analysis, result);
                }

                // Create service implementations
                if (_generateImplementations)
                {
                    CreateServiceImplementations(analysis, result);
                }

                // Create unit tests
                if (_generateTests)
                {
                    CreateServiceTests(analysis, result);
                }

                // Create documentation
                if (_createDocumentation)
                {
                    CreateServiceDocumentation(analysis, result);
                }

                // Register services with registry
                RegisterDecomposedServices(analysis, result);

                result.IsSuccessful = true;
                ChimeraLogger.Log($"Decomposition complete for {analysis.ManagerName}: {result.CreatedFiles.Count} files created");
                
                return result;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"Failed to decompose manager {analysis.ManagerName}: {ex.Message}");
                result.IsSuccessful = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Get predefined decomposition plan for known managers
        /// </summary>
        public ManagerAnalysisResult GetPredefinedPlan(string managerName)
        {
            switch (managerName)
            {
                case "CannabisCupManager":
                    return CreateCannabisCupDecompositionPlan();
                case "ResearchManager":
                    return CreateResearchManagerDecompositionPlan();
                case "ComprehensiveProgressionManager":
                    return CreateProgressionManagerDecompositionPlan();
                case "TradingManager":
                    return CreateTradingManagerDecompositionPlan();
                case "AdvancedSpeedTreeManager":
                    return CreateSpeedTreeDecompositionPlan();
                case "LiveEventsManager":
                    return CreateLiveEventsDecompositionPlan();
                default:
                    ChimeraLogger.LogWarning($"No predefined plan available for {managerName}");
                    return null;
            }
        }

        #endregion

        #region Analysis Methods

        private void AnalyzeManagerStructure(string content, ManagerAnalysisResult result)
        {
            var lines = content.Split('\n');
            
            // Analyze methods
            result.MethodCount = CountMatches(content, @"(public|private|protected)\s+\w+\s+\w+\s*\(");
            
            // Analyze properties
            result.PropertyCount = CountMatches(content, @"(public|private|protected)\s+\w+\s+\w+\s*{\s*(get|set)");
            
            // Analyze fields
            result.FieldCount = CountMatches(content, @"(public|private|protected)\s+\w+\s+\w+\s*[;=]");
            
            // Analyze nested classes
            result.NestedClassCount = CountMatches(content, @"(public|private|protected)?\s*class\s+\w+");
            
            // Identify functional areas
            IdentifyFunctionalAreas(content, result);
            
            // Calculate complexity metrics
            CalculateComplexityMetrics(content, result);
        }

        private void IdentifyFunctionalAreas(string content, ManagerAnalysisResult result)
        {
            var functionalAreas = new List<FunctionalArea>();
            
            // Pattern-based area identification
            var patterns = new Dictionary<string, string[]>
            {
                ["Management"] = new[] { "Create", "Initialize", "Setup", "Configure", "Manage" },
                ["Processing"] = new[] { "Process", "Calculate", "Update", "Execute", "Run" },
                ["Analysis"] = new[] { "Analyze", "Evaluate", "Assess", "Measure", "Monitor" },
                ["Data"] = new[] { "Save", "Load", "Store", "Retrieve", "Cache" },
                ["Events"] = new[] { "Event", "Trigger", "Handle", "Notify", "Broadcast" },
                ["UI"] = new[] { "Display", "Show", "Hide", "Render", "Draw" },
                ["Validation"] = new[] { "Validate", "Check", "Verify", "Confirm", "Test" },
                ["Communication"] = new[] { "Send", "Receive", "Connect", "Disconnect", "Sync" }
            };

            foreach (var pattern in patterns)
            {
                int matches = 0;
                foreach (var keyword in pattern.Value)
                {
                    matches += CountMatches(content, $@"\b{keyword}\w*\s*\(");
                }
                
                if (matches > 2)
                {
                    functionalAreas.Add(new FunctionalArea
                    {
                        Name = pattern.Key,
                        MethodCount = matches,
                        EstimatedLines = matches * 25 // Rough estimate
                    });
                }
            }

            result.FunctionalAreas = functionalAreas;
        }

        private void CalculateComplexityMetrics(string content, ManagerAnalysisResult result)
        {
            // Cyclomatic complexity estimation
            result.CyclomaticComplexity = CountMatches(content, @"\b(if|while|for|foreach|switch|case|\?)\b");
            
            // Dependency count
            result.DependencyCount = CountMatches(content, @"using\s+\w+");
            
            // Comment ratio
            int commentLines = CountMatches(content, @"^\s*//") + CountMatches(content, @"^\s*/\*");
            result.CommentRatio = (float)commentLines / result.TotalLines;
        }

        private int CountMatches(string content, string pattern)
        {
            try
            {
                return System.Text.RegularExpressions.Regex.Matches(content, pattern).Count;
            }
            catch
            {
                return 0;
            }
        }

        #endregion

        #region Decomposition Planning

        private void GenerateDecompositionPlan(ManagerAnalysisResult result)
        {
            var services = new List<ProposedService>();
            
            // Group functional areas into services based on cohesion
            var sortedAreas = result.FunctionalAreas.OrderByDescending(a => a.EstimatedLines).ToList();
            
            foreach (var area in sortedAreas)
            {
                var service = new ProposedService
                {
                    Name = $"{area.Name}Service",
                    Domain = DetermineServiceDomain(area.Name),
                    EstimatedLines = Math.Max(_minServiceLines, Math.Min(_maxServiceLines, area.EstimatedLines)),
                    Methods = GenerateServiceMethods(area),
                    Dependencies = GenerateServiceDependencies(area),
                    Priority = DetermineServicePriority(area)
                };
                
                services.Add(service);
            }
            
            // Ensure we don't exceed line limits
            OptimizeServiceSizes(services, result.TotalLines);
            
            result.ProposedServices = services;
        }

        private ServiceDomain DetermineServiceDomain(string areaName)
        {
            return areaName switch
            {
                "Management" => ServiceDomain.Core,
                "Processing" => ServiceDomain.Core,
                "Analysis" => ServiceDomain.Analytics,
                "Data" => ServiceDomain.Core,
                "Events" => ServiceDomain.Events,
                "UI" => ServiceDomain.UI,
                "Validation" => ServiceDomain.Testing,
                "Communication" => ServiceDomain.Events,
                _ => ServiceDomain.Core
            };
        }

        private List<string> GenerateServiceMethods(FunctionalArea area)
        {
            // Generate common method signatures based on area type
            var methods = new List<string>();
            
            switch (area.Name)
            {
                case "Management":
                    methods.AddRange(new[] { "Initialize()", "Configure()", "Start()", "Stop()", "GetStatus()" });
                    break;
                case "Processing":
                    methods.AddRange(new[] { "Process()", "Update()", "Execute()", "Calculate()", "Transform()" });
                    break;
                case "Analysis":
                    methods.AddRange(new[] { "Analyze()", "Evaluate()", "GenerateReport()", "GetMetrics()", "GetInsights()" });
                    break;
                default:
                    methods.AddRange(new[] { "Initialize()", "Execute()", "GetResult()", "Cleanup()" });
                    break;
            }
            
            return methods;
        }

        private List<string> GenerateServiceDependencies(FunctionalArea area)
        {
            // Generate likely dependencies based on functional area
            var dependencies = new List<string> { "IService" };
            
            switch (area.Name)
            {
                case "Analysis":
                    dependencies.Add("IAnalyticsService");
                    break;
                case "Data":
                    dependencies.Add("IDataService");
                    break;
                case "Events":
                    dependencies.Add("IEventService");
                    break;
                case "UI":
                    dependencies.Add("IUIService");
                    break;
            }
            
            return dependencies;
        }

        private ServicePriority DetermineServicePriority(FunctionalArea area)
        {
            return area.Name switch
            {
                "Management" => ServicePriority.Critical,
                "Processing" => ServicePriority.High,
                "Analysis" => ServicePriority.Medium,
                "Data" => ServicePriority.High,
                "Events" => ServicePriority.Medium,
                "UI" => ServicePriority.Low,
                "Validation" => ServicePriority.Medium,
                "Communication" => ServicePriority.Medium,
                _ => ServicePriority.Low
            };
        }

        private void OptimizeServiceSizes(List<ProposedService> services, int totalLines)
        {
            int targetTotalLines = totalLines;
            int currentTotalLines = services.Sum(s => s.EstimatedLines);
            
            if (currentTotalLines > targetTotalLines * 1.2f) // Allow 20% expansion
            {
                float scaleFactor = (targetTotalLines * 1.2f) / currentTotalLines;
                foreach (var service in services)
                {
                    service.EstimatedLines = Math.Max(_minServiceLines, 
                        (int)(service.EstimatedLines * scaleFactor));
                }
            }
        }

        private void ValidateDecomposition(ManagerAnalysisResult result)
        {
            var issues = new List<string>();
            
            // Check service count
            if (result.ProposedServices.Count < 2)
            {
                issues.Add("Insufficient service decomposition - need at least 2 services");
            }
            
            if (result.ProposedServices.Count > 8)
            {
                issues.Add("Over-decomposition - consider consolidating similar services");
            }
            
            // Check service sizes
            foreach (var service in result.ProposedServices)
            {
                if (service.EstimatedLines < _minServiceLines)
                {
                    issues.Add($"Service {service.Name} too small ({service.EstimatedLines} lines)");
                }
                
                if (service.EstimatedLines > _maxServiceLines)
                {
                    issues.Add($"Service {service.Name} too large ({service.EstimatedLines} lines)");
                }
            }
            
            // Check total coverage
            int totalServiceLines = result.ProposedServices.Sum(s => s.EstimatedLines);
            if (totalServiceLines < result.TotalLines * 0.8f)
            {
                issues.Add("Decomposition may be missing significant functionality");
            }
            
            result.ValidationIssues = issues;
            result.IsValid = issues.Count == 0;
        }

        #endregion

        #region Predefined Plans

        private ManagerAnalysisResult CreateCannabisCupDecompositionPlan()
        {
            return new ManagerAnalysisResult
            {
                ManagerName = "CannabisCupManager",
                TotalLines = 1873,
                ProposedServices = new List<ProposedService>
                {
                    new ProposedService
                    {
                        Name = "CompetitionManagementService",
                        Domain = ServiceDomain.Competition,
                        EstimatedLines = 470,
                        Priority = ServicePriority.Critical,
                        Methods = new List<string> { "CreateTournament", "ScheduleCompetition", "StartCompetition", "EndCompetition" }
                    },
                    new ProposedService
                    {
                        Name = "JudgingEvaluationService",
                        Domain = ServiceDomain.Competition,
                        EstimatedLines = 460,
                        Priority = ServicePriority.Critical,
                        Methods = new List<string> { "CalculateScore", "AssignJudge", "CalculateResults", "ValidateResults" }
                    },
                    new ProposedService
                    {
                        Name = "ParticipantRegistrationService",
                        Domain = ServiceDomain.Competition,
                        EstimatedLines = 470,
                        Priority = ServicePriority.High,
                        Methods = new List<string> { "RegisterParticipant", "ValidateRegistration", "SubmitEntry", "CheckQualification" }
                    },
                    new ProposedService
                    {
                        Name = "CompetitionRewardsService",
                        Domain = ServiceDomain.Competition,
                        EstimatedLines = 473,
                        Priority = ServicePriority.Medium,
                        Methods = new List<string> { "DistributePrizes", "ProcessAchievements", "RecognizeWinner", "GetPlayerRewards" }
                    }
                },
                IsValid = true
            };
        }

        private ManagerAnalysisResult CreateResearchManagerDecompositionPlan()
        {
            return new ManagerAnalysisResult
            {
                ManagerName = "ResearchManager",
                TotalLines = 1840,
                ProposedServices = new List<ProposedService>
                {
                    // Note: Research services removed - research system deleted from Project Chimera
                },
                IsValid = true
            };
        }

        private ManagerAnalysisResult CreateProgressionManagerDecompositionPlan()
        {
            return new ManagerAnalysisResult
            {
                ManagerName = "ComprehensiveProgressionManager",
                TotalLines = 1771,
                ProposedServices = new List<ProposedService>
                {
                    new ProposedService { Name = "ExperienceManagementService", Domain = ServiceDomain.Progression, EstimatedLines = 354, Priority = ServicePriority.Critical },
                    new ProposedService { Name = "SkillTreeManagementService", Domain = ServiceDomain.Progression, EstimatedLines = 354, Priority = ServicePriority.Critical },
                    new ProposedService { Name = "ProgressionAchievementService", Domain = ServiceDomain.Progression, EstimatedLines = 354, Priority = ServicePriority.High },
                    new ProposedService { Name = "ProgressionAnalyticsService", Domain = ServiceDomain.Progression, EstimatedLines = 354, Priority = ServicePriority.Medium },
                    new ProposedService { Name = "MilestoneTrackingService", Domain = ServiceDomain.Progression, EstimatedLines = 355, Priority = ServicePriority.Medium }
                },
                IsValid = true
            };
        }

        private ManagerAnalysisResult CreateTradingManagerDecompositionPlan()
        {
            return new ManagerAnalysisResult
            {
                ManagerName = "TradingManager",
                TotalLines = 1508,
                ProposedServices = new List<ProposedService>
                {
                    new ProposedService { Name = "MarketOperationsService", Domain = ServiceDomain.Economy, EstimatedLines = 503, Priority = ServicePriority.Critical },
                    new ProposedService { Name = "TradeExecutionService", Domain = ServiceDomain.Economy, EstimatedLines = 503, Priority = ServicePriority.Critical },
                    new ProposedService { Name = "TradingAnalyticsService", Domain = ServiceDomain.Economy, EstimatedLines = 502, Priority = ServicePriority.High }
                },
                IsValid = true
            };
        }


        private ManagerAnalysisResult CreateSpeedTreeDecompositionPlan()
        {
            return new ManagerAnalysisResult
            {
                ManagerName = "AdvancedSpeedTreeManager",
                TotalLines = 1441,
                ProposedServices = new List<ProposedService>
                {
                    new ProposedService { Name = "SpeedTreeRenderingService", Domain = ServiceDomain.SpeedTree, EstimatedLines = 360, Priority = ServicePriority.Critical },
                    new ProposedService { Name = "PlantVisualizationService", Domain = ServiceDomain.SpeedTree, EstimatedLines = 360, Priority = ServicePriority.High },
                    new ProposedService { Name = "EnvironmentalResponseService", Domain = ServiceDomain.SpeedTree, EstimatedLines = 360, Priority = ServicePriority.High },
                    new ProposedService { Name = "SpeedTreeOptimizationService", Domain = ServiceDomain.SpeedTree, EstimatedLines = 361, Priority = ServicePriority.Medium }
                },
                IsValid = true
            };
        }

        private ManagerAnalysisResult CreateLiveEventsDecompositionPlan()
        {
            return new ManagerAnalysisResult
            {
                ManagerName = "LiveEventsManager",
                TotalLines = 1418,
                ProposedServices = new List<ProposedService>
                {
                    new ProposedService { Name = "EventLifecycleService", Domain = ServiceDomain.Events, EstimatedLines = 473, Priority = ServicePriority.High },
                    new ProposedService { Name = "SeasonalContentService", Domain = ServiceDomain.Events, EstimatedLines = 473, Priority = ServicePriority.Medium },
                    new ProposedService { Name = "CommunityEventService", Domain = ServiceDomain.Events, EstimatedLines = 472, Priority = ServicePriority.Medium }
                },
                IsValid = true
            };
        }


        #endregion

        #region Service Generation

        private void CreateServiceInterfaces(ManagerAnalysisResult analysis, DecompositionResult result)
        {
            foreach (var service in analysis.ProposedServices)
            {
                string interfaceName = $"I{service.Name}";
                string filePath = Path.Combine(_interfacesOutputPath, $"{interfaceName}.cs");
                
                var interfaceCode = GenerateServiceInterface(service, analysis.ManagerName);
                
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, interfaceCode);
                
                result.CreatedFiles.Add(filePath);
            }
        }

        private void CreateServiceImplementations(ManagerAnalysisResult analysis, DecompositionResult result)
        {
            foreach (var service in analysis.ProposedServices)
            {
                string filePath = Path.Combine(_servicesOutputPath, $"{service.Name}.cs");
                
                var implementationCode = GenerateServiceImplementation(service, analysis.ManagerName);
                
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, implementationCode);
                
                result.CreatedFiles.Add(filePath);
            }
        }

        private void CreateServiceTests(ManagerAnalysisResult analysis, DecompositionResult result)
        {
            foreach (var service in analysis.ProposedServices)
            {
                string filePath = Path.Combine(_testsOutputPath, $"{service.Name}Tests.cs");
                
                var testCode = GenerateServiceTests(service, analysis.ManagerName);
                
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, testCode);
                
                result.CreatedFiles.Add(filePath);
            }
        }

        private void CreateServiceDocumentation(ManagerAnalysisResult analysis, DecompositionResult result)
        {
            string docPath = Path.Combine(_documentationPath, $"{analysis.ManagerName}_Decomposition.md");
            
            var documentation = GenerateDecompositionDocumentation(analysis);
            
            Directory.CreateDirectory(Path.GetDirectoryName(docPath));
            File.WriteAllText(docPath, documentation);
            
            result.CreatedFiles.Add(docPath);
        }

        private void RegisterDecomposedServices(ManagerAnalysisResult analysis, DecompositionResult result)
        {
            // Services will be registered when they are instantiated
            // This method can be used for additional registry configuration
            ChimeraLogger.Log($"Prepared {analysis.ProposedServices.Count} services for registration in ServiceRegistry");
        }

        private string GenerateServiceInterface(ProposedService service, string originalManager)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using ProjectChimera.Systems.Registry;");
            sb.AppendLine();
            sb.AppendLine($"namespace ProjectChimera.Systems.Services");
            sb.AppendLine("{");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Interface for {service.Name}");
            sb.AppendLine($"    /// Decomposed from {originalManager}");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public interface I{service.Name} : IService");
            sb.AppendLine("    {");
            
            foreach (var method in service.Methods)
            {
                sb.AppendLine($"        void {method};");
            }
            
            sb.AppendLine("    }");
            sb.AppendLine("}");
            
            return sb.ToString();
        }

        private string GenerateServiceImplementation(ProposedService service, string originalManager)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using ProjectChimera.Systems.Registry;");
            sb.AppendLine();
            sb.AppendLine($"namespace ProjectChimera.Systems.Services");
            sb.AppendLine("{");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Implementation of {service.Name}");
            sb.AppendLine($"    /// Decomposed from {originalManager}");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public class {service.Name} : I{service.Name}");
            sb.AppendLine("    {");
            sb.AppendLine("        public bool IsInitialized { get; private set; }");
            sb.AppendLine();
            sb.AppendLine("        public void Initialize()");
            sb.AppendLine("        {");
            sb.AppendLine("            // TODO: Implement initialization logic from original manager");
            sb.AppendLine("            IsInitialized = true;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public void Shutdown()");
            sb.AppendLine("        {");
            sb.AppendLine("            // TODO: Implement cleanup logic");
            sb.AppendLine("            IsInitialized = false;");
            sb.AppendLine("        }");
            sb.AppendLine();
            
            foreach (var method in service.Methods)
            {
                sb.AppendLine($"        public void {method}");
                sb.AppendLine("        {");
                sb.AppendLine($"            // TODO: Implement {method} logic from original manager");
                sb.AppendLine("            #if CHIMERA_FEATURE_DEVELOPMENT");
                sb.AppendLine("            ChimeraLogger.LogWarning($\"Method {method} not yet implemented in decomposed service\");");
                sb.AppendLine("            #else");
                sb.AppendLine("            throw new NotImplementedException($\"Method {method} requires full implementation\");");
                sb.AppendLine("            #endif");
                sb.AppendLine("        }");
                sb.AppendLine();
            }
            
            sb.AppendLine("    }");
            sb.AppendLine("}");
            
            return sb.ToString();
        }

        private string GenerateServiceTests(ProposedService service, string originalManager)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using NUnit.Framework;");
            sb.AppendLine("using ProjectChimera.Systems.Services;");
            sb.AppendLine();
            sb.AppendLine($"namespace ProjectChimera.Testing.Services");
            sb.AppendLine("{");
            sb.AppendLine($"    [TestFixture]");
            sb.AppendLine($"    public class {service.Name}Tests");
            sb.AppendLine("    {");
            sb.AppendLine($"        private {service.Name} _service;");
            sb.AppendLine();
            sb.AppendLine("        [SetUp]");
            sb.AppendLine("        public void Setup()");
            sb.AppendLine("        {");
            sb.AppendLine($"            _service = new {service.Name}();");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        [Test]");
            sb.AppendLine("        public void Initialize_SetsIsInitializedTrue()");
            sb.AppendLine("        {");
            sb.AppendLine("            _service.Initialize();");
            sb.AppendLine("            Assert.IsTrue(_service.IsInitialized);");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        [TearDown]");
            sb.AppendLine("        public void TearDown()");
            sb.AppendLine("        {");
            sb.AppendLine("            _service?.Shutdown();");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            
            return sb.ToString();
        }

        private string GenerateDecompositionDocumentation(ManagerAnalysisResult analysis)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# {analysis.ManagerName} Decomposition");
            sb.AppendLine();
            sb.AppendLine($"**Original Lines**: {analysis.TotalLines}");
            sb.AppendLine($"**Target Services**: {analysis.ProposedServices.Count}");
            sb.AppendLine($"**Analysis Date**: {analysis.AnalysisDate:yyyy-MM-dd}");
            sb.AppendLine();
            sb.AppendLine("## Proposed Services");
            sb.AppendLine();
            
            foreach (var service in analysis.ProposedServices)
            {
                sb.AppendLine($"### {service.Name}");
                sb.AppendLine($"- **Domain**: {service.Domain}");
                sb.AppendLine($"- **Priority**: {service.Priority}");
                sb.AppendLine($"- **Estimated Lines**: {service.EstimatedLines}");
                sb.AppendLine($"- **Methods**: {string.Join(", ", service.Methods)}");
                sb.AppendLine();
            }
            
            return sb.ToString();
        }

        #endregion
    }

    #region Data Structures

    [System.Serializable]
    public class ManagerAnalysisResult
    {
        public string ManagerPath;
        public string ManagerName;
        public int TotalLines;
        public int MethodCount;
        public int PropertyCount;
        public int FieldCount;
        public int NestedClassCount;
        public int CyclomaticComplexity;
        public int DependencyCount;
        public float CommentRatio;
        public DateTime AnalysisDate;
        public List<FunctionalArea> FunctionalAreas = new List<FunctionalArea>();
        public List<ProposedService> ProposedServices = new List<ProposedService>();
        public List<string> ValidationIssues = new List<string>();
        public bool IsValid;
    }

    [System.Serializable]
    public class FunctionalArea
    {
        public string Name;
        public int MethodCount;
        public int EstimatedLines;
        public List<string> Keywords = new List<string>();
    }

    [System.Serializable]
    public class ProposedService
    {
        public string Name;
        public ServiceDomain Domain;
        public int EstimatedLines;
        public ServicePriority Priority;
        public List<string> Methods = new List<string>();
        public List<string> Dependencies = new List<string>();
    }

    [System.Serializable]
    public class DecompositionResult
    {
        public string ManagerName;
        public DateTime DecompositionDate;
        public int OriginalLines;
        public int TargetServices;
        public List<string> CreatedFiles = new List<string>();
        public bool IsSuccessful;
        public string ErrorMessage;
    }

    public enum ServicePriority
    {
        Critical,
        High,
        Medium,
        Low
    }

    #endregion
}