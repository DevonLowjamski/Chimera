// REFACTORED: UI Component Analyzer Data Structures
// Extracted from UIComponentAnalyzer for better separation of concerns

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectChimera.Systems.UI.Performance
{
    /// <summary>
    /// Canvas analysis data
    /// </summary>
    public struct CanvasAnalysisData
    {
        public Canvas Canvas;
        public int ComponentCount;
        public int DrawCalls;
        public bool IsOptimized;
        public List<string> Issues;
    }

    /// <summary>
    /// UI performance issue
    /// </summary>
    [Serializable]
    public struct UIPerformanceIssue
    {
        public UIPerformanceIssueType IssueType;
        public UIPerformanceIssueSeverity Severity;
        public string Description;
        public Type ComponentType;
        public Component Component;
        public Canvas Canvas;
    }

    /// <summary>
    /// UI performance issue types
    /// </summary>
    public enum UIPerformanceIssueType
    {
        SlowComponent,
        ExcessiveComponents,
        ExcessiveDrawCalls,
        MemoryLeak,
        UnoptimizedCanvas,
        RedundantUpdates
    }

    /// <summary>
    /// UI performance issue severity
    /// </summary>
    public enum UIPerformanceIssueSeverity
    {
        Info,
        Warning,
        Critical
    }

    /// <summary>
    /// UI component analysis statistics
    /// </summary>
    [Serializable]
    public struct UIComponentAnalysisStats
    {
        public int AnalysisRuns;
        public int ComponentsAnalyzed;
        public int PerformanceIssues;
        public int CriticalIssues;
        public int WarningIssues;
        public int CanvasesAnalyzed;
    }
}

