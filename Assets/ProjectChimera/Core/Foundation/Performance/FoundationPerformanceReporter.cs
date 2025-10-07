using UnityEngine;
using System.Collections.Generic;

namespace ProjectChimera.Core.Foundation.Performance
{
	/// <summary>
	/// Foundation Performance Reporter - Generates summary reports of current performance state
	/// Single Responsibility: Reporting/aggregation only (no collection/analysis)
	/// </summary>
	public class FoundationPerformanceReporter : MonoBehaviour
	{
		[SerializeField] private bool _enableReporting = true;
		[SerializeField] private bool _enableLogging = false;

		// Dependencies
		private FoundationPerformanceMetrics _metrics;
		private FoundationPerformanceAnalyzer _analyzer;

		public bool IsEnabled { get; private set; } = true;

		private void Awake()
		{
			_metrics = GetComponent<FoundationPerformanceMetrics>();
			_analyzer = GetComponent<FoundationPerformanceAnalyzer>();
		}

		public void SetEnabled(bool enabled)
		{
			IsEnabled = enabled;
		}

		public void UpdateReports()
		{
			if (!IsEnabled || !_enableReporting) return;
			// Intentionally minimal; hook for editor/UI to poll or push
		}

		public PerformanceReport GeneratePerformanceReport()
		{
			// Use coordinator data structures already defined in Foundation namespace
			var report = new PerformanceReport
			{
				ReportTime = Time.time,
				OverallPerformanceScore = _metrics != null ? _metrics.GetOverallPerformanceScore() : 1.0f,
				PerformanceCategory = _analyzer != null ? _analyzer.GetPerformanceCategory() : PerformanceCategory.Acceptable,
				TotalSystems = 0,
				ExcellentSystems = 0,
				GoodSystems = 0,
				AcceptableSystems = 0,
				PoorSystems = 0,
				SystemDetails = new List<SystemPerformanceSummary>()
			};

			// If metrics exposes per-system data, include summaries
			if (_metrics != null)
			{
				var all = _metrics.GetAllSystemPerformance();
				report.TotalSystems = all != null ? all.Length : 0;
				if (all != null)
				{
					foreach (var perf in all)
					{
						var category = _analyzer != null ? _analyzer.GetPerformanceCategory(perf.PerformanceScore) : PerformanceCategory.Acceptable;
						switch (category)
						{
							case PerformanceCategory.Excellent: report.ExcellentSystems++; break;
							case PerformanceCategory.Good: report.GoodSystems++; break;
							case PerformanceCategory.Acceptable: report.AcceptableSystems++; break;
							case PerformanceCategory.Poor: report.PoorSystems++; break;
						}

						report.SystemDetails.Add(new SystemPerformanceSummary
						{
							SystemName = perf.SystemName,
							PerformanceScore = perf.PerformanceScore,
							Category = category,
							LastUpdateTime = perf.LastUpdateTime,
							OptimizationRecommendations = perf.OptimizationRecommendations
						});
					}
				}
			}

			return report;
		}
	}
}
