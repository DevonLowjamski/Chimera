using System.Collections.Generic;
using ProjectChimera.Core.Foundation;

namespace ProjectChimera.Core.Foundation.Performance
{
	/// <summary>
	/// Summary performance report produced by the performance core.
	/// Value type to allow nullable usage in legacy adapters.
	/// </summary>
	public struct PerformanceReport
	{
		public float ReportTime;
		public float OverallPerformanceScore;
		public PerformanceCategory PerformanceCategory;
		public int TotalSystems;
		public int ExcellentSystems;
		public int GoodSystems;
		public int AcceptableSystems;
		public int PoorSystems;
		public List<SystemPerformanceSummary> SystemDetails;

		// Legacy aliases for backward compatibility
		public float OverallScore => OverallPerformanceScore;
		public int SystemCount => TotalSystems;
		public SystemReport[] SystemReports => SystemDetails == null ? null : ConvertSystemSummaries(SystemDetails);

		private static SystemReport[] ConvertSystemSummaries(List<SystemPerformanceSummary> details)
		{
			if (details == null) return null;
			var arr = new SystemReport[details.Count];
			for (int i = 0; i < details.Count; i++)
			{
				arr[i] = new SystemReport
				{
					SystemName = details[i].SystemName,
					PerformanceScore = details[i].PerformanceScore,
					Category = details[i].Category,
					LastUpdateTime = details[i].LastUpdateTime,
					Recommendations = details[i].OptimizationRecommendations
				};
			}
			return arr;
		}
	}

	/// <summary>
	/// Per-system performance report entry.
	/// </summary>
	public struct SystemReport
	{
		public string SystemName;
		public float PerformanceScore;
		public PerformanceCategory Category;
		public float LastUpdateTime;
		public List<string> Recommendations;
	}

	public struct SystemPerformanceSummary
	{
		public string SystemName;
		public float PerformanceScore;
		public PerformanceCategory Category;
		public float LastUpdateTime;
		public List<string> OptimizationRecommendations;
	}
}


