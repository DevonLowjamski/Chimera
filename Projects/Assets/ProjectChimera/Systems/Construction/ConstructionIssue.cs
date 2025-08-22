using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Data.Construction;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Construction issue tracking for Project Chimera facility construction system.
    /// Represents problems, errors, or warnings that occur during construction processes.
    /// </summary>
    [Serializable]
    public class ConstructionIssue
    {
        [Header("Issue Identification")]
        public string IssueId;
        public string IssueName;
        public string Title; // Title for the issue
        public string Description;
        public ConstructionIssueType IssueType;
        public ConstructionIssueSeverity Severity;
        public string Category; // Category classification
        public ConstructionIssueStatus Status; // Current status of the issue
        
        [Header("Issue Context")]
        public string AffectedModuleId;
        public string AffectedSystemName;
        public Vector3 IssueLocation;
        public string ConstructionPhase;
        
        [Header("Issue Details")]
        public DateTime IssueDetected;
        public DateTime IssueResolved;
        public DateTime ReportDate; // Date when issue was reported
        public string ReportedBy; // Who reported the issue
        public bool IsResolved;
        public float EstimatedRepairCost;
        public float EstimatedRepairTime;
        
        [Header("Issue Resolution")]
        public string ResolutionMethod;
        public string ResolutionNotes;
        public List<string> RequiredResources;
        public List<string> RequiredSkills;
        
        // Properties
        public bool IsActive => !IsResolved;
        public TimeSpan TimeSinceDetected => DateTime.Now - IssueDetected;
        public TimeSpan ResolutionTime => IsResolved ? (IssueResolved - IssueDetected) : TimeSpan.Zero;
        
        public ConstructionIssue()
        {
            IssueId = Guid.NewGuid().ToString();
            IssueDetected = DateTime.Now;
            ReportDate = DateTime.Now;
            ReportedBy = "System";
            Status = ConstructionIssueStatus.Reported;
            Category = "General";
            Title = IssueName ?? "Untitled Issue";
            RequiredResources = new List<string>();
            RequiredSkills = new List<string>();
        }
        
        public ConstructionIssue(string issueName, ConstructionIssueType type, ConstructionIssueSeverity severity)
        {
            IssueId = Guid.NewGuid().ToString();
            IssueName = issueName;
            Title = issueName;
            IssueType = type;
            Severity = severity;
            IssueDetected = DateTime.Now;
            ReportDate = DateTime.Now;
            ReportedBy = "System";
            Status = ConstructionIssueStatus.Reported;
            Category = type.ToString();
            RequiredResources = new List<string>();
            RequiredSkills = new List<string>();
        }
        
        /// <summary>
        /// Mark issue as resolved
        /// </summary>
        public void ResolveIssue(string resolutionMethod, string notes = "")
        {
            IsResolved = true;
            IssueResolved = DateTime.Now;
            ResolutionMethod = resolutionMethod;
            ResolutionNotes = notes;
        }
        
        /// <summary>
        /// Get issue priority score for sorting
        /// </summary>
        public int GetPriorityScore()
        {
            int severityScore = (int)Severity * 10;
            int typeScore = GetTypeScore();
            int timeScore = Mathf.Min(10, (int)TimeSinceDetected.TotalHours);
            
            return severityScore + typeScore + timeScore;
        }
        
        /// <summary>
        /// Get type-based priority score
        /// </summary>
        private int GetTypeScore()
        {
            return IssueType switch
            {
                ConstructionIssueType.Safety => 15,
                ConstructionIssueType.Structural => 12,
                ConstructionIssueType.Electrical => 10,
                ConstructionIssueType.Plumbing => 8,
                ConstructionIssueType.HVAC => 8,
                ConstructionIssueType.Material => 6,
                ConstructionIssueType.Quality => 5,
                ConstructionIssueType.Schedule => 3,
                ConstructionIssueType.Cost => 2,
                _ => 1
            };
        }
        
        /// <summary>
        /// Get human-readable status
        /// </summary>
        public string GetStatusText()
        {
            if (IsResolved)
                return $"Resolved ({ResolutionTime.TotalHours:F1}h)";
            
            return $"Active ({TimeSinceDetected.TotalHours:F1}h)";
        }
        
        /// <summary>
        /// Get issue impact description
        /// </summary>
        public string GetImpactDescription()
        {
            return Severity switch
            {
                ConstructionIssueSeverity.Critical => "Construction halted - immediate action required",
                ConstructionIssueSeverity.High => "Significant delay risk - priority attention needed", 
                ConstructionIssueSeverity.Medium => "Moderate impact - schedule for resolution",
                ConstructionIssueSeverity.Low => "Minor issue - can be addressed during maintenance",
                _ => "Unknown impact level"
            };
        }
    }
    
    /// <summary>
    /// Types of construction issues
    /// </summary>
    public enum ConstructionIssueType
    {
        Safety,
        Structural,
        Electrical,
        Plumbing,
        HVAC,
        Material,
        Quality,
        Schedule,
        Cost,
        Environmental,
        Regulatory,
        Equipment,
        Labor,
        Weather,
        ValidationFailed,
        InvalidPlacement,
        WeatherDelay,
        MaterialShortage,
        Other
    }
    
    /// <summary>
    /// Severity levels for construction issues
    /// </summary>
    public enum ConstructionIssueSeverity
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }
    
    /// <summary>
    /// Status levels for construction issues
    /// </summary>
    public enum ConstructionIssueStatus
    {
        Reported,
        Acknowledged,
        InProgress,
        UnderReview,
        Resolved,
        Verified,
        Closed,
        Reopened
    }
    
    /// <summary>
    /// Construction issue tracking and management
    /// </summary>
    [Serializable]
    public class ConstructionIssueTracker
    {
        public List<ConstructionIssue> ActiveIssues;
        public List<ConstructionIssue> ResolvedIssues;
        public Dictionary<string, int> IssueCountByType;
        public float TotalRepairCosts;
        public float TotalRepairTime;
        
        public ConstructionIssueTracker()
        {
            ActiveIssues = new List<ConstructionIssue>();
            ResolvedIssues = new List<ConstructionIssue>();
            IssueCountByType = new Dictionary<string, int>();
        }
        
        /// <summary>
        /// Add new construction issue
        /// </summary>
        public void AddIssue(ConstructionIssue issue)
        {
            if (issue == null) return;
            
            ActiveIssues.Add(issue);
            UpdateIssueStatistics();
        }
        
        /// <summary>
        /// Resolve construction issue
        /// </summary>
        public void ResolveIssue(string issueId, string resolutionMethod, string notes = "")
        {
            var issue = ActiveIssues.Find(i => i.IssueId == issueId);
            if (issue != null)
            {
                issue.ResolveIssue(resolutionMethod, notes);
                ActiveIssues.Remove(issue);
                ResolvedIssues.Add(issue);
                UpdateIssueStatistics();
            }
        }
        
        /// <summary>
        /// Get issues by priority
        /// </summary>
        public List<ConstructionIssue> GetIssuesByPriority()
        {
            var sortedIssues = new List<ConstructionIssue>(ActiveIssues);
            sortedIssues.Sort((a, b) => b.GetPriorityScore().CompareTo(a.GetPriorityScore()));
            return sortedIssues;
        }
        
        /// <summary>
        /// Get issues by type
        /// </summary>
        public List<ConstructionIssue> GetIssuesByType(ConstructionIssueType type)
        {
            return ActiveIssues.FindAll(i => i.IssueType == type);
        }
        
        /// <summary>
        /// Get issues by severity
        /// </summary>
        public List<ConstructionIssue> GetIssuesBySeverity(ConstructionIssueSeverity severity)
        {
            return ActiveIssues.FindAll(i => i.Severity == severity);
        }
        
        /// <summary>
        /// Update issue statistics
        /// </summary>
        private void UpdateIssueStatistics()
        {
            IssueCountByType.Clear();
            TotalRepairCosts = 0f;
            TotalRepairTime = 0f;
            
            foreach (var issue in ActiveIssues)
            {
                string typeKey = issue.IssueType.ToString();
                IssueCountByType[typeKey] = IssueCountByType.ContainsKey(typeKey) ? IssueCountByType[typeKey] + 1 : 1;
                TotalRepairCosts += issue.EstimatedRepairCost;
                TotalRepairTime += issue.EstimatedRepairTime;
            }
        }
        
        /// <summary>
        /// Get issue summary statistics
        /// </summary>
        public ConstructionIssueSummary GetIssueSummary()
        {
            return new ConstructionIssueSummary
            {
                TotalActiveIssues = ActiveIssues.Count,
                TotalResolvedIssues = ResolvedIssues.Count,
                CriticalIssues = GetIssuesBySeverity(ConstructionIssueSeverity.Critical).Count,
                HighPriorityIssues = GetIssuesBySeverity(ConstructionIssueSeverity.High).Count,
                EstimatedRepairCosts = TotalRepairCosts,
                EstimatedRepairTime = TotalRepairTime,
                IssuesByType = new Dictionary<string, int>(IssueCountByType)
            };
        }
    }
    
    /// <summary>
    /// Construction issue summary data
    /// </summary>
    [Serializable]
    public struct ConstructionIssueSummary
    {
        public int TotalActiveIssues;
        public int TotalResolvedIssues;
        public int CriticalIssues;
        public int HighPriorityIssues;
        public float EstimatedRepairCosts;
        public float EstimatedRepairTime;
        public Dictionary<string, int> IssuesByType;
    }
}