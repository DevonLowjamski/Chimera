// Centralized aliases to resolve common type ambiguities across assemblies
// Ensures unqualified "ChimeraLogger" maps to the Core logger implementation
// This relies on C# 10 global using support available in Unity 6+
global using ChimeraLogger = ProjectChimera.Core.Logging.ChimeraLogger;


