using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ProjectChimera.Core.Logging;
using System.Linq;

namespace ProjectChimera.Core.Performance
{
    /// <summary>
    /// REFACTORED: Metrics Exporter - Focused data export and file operations
    /// Single Responsibility: Exporting metrics data to various formats and destinations
    /// Extracted from MetricsCollectionFramework for better SRP compliance
    /// </summary>
    public class MetricsExporter
    {
        private readonly string _basePath;
        private readonly bool _enableLogging;

        // Export state
        private float _lastExportTime;

        // Events
        public event System.Action<string, ExportResult> OnExportCompleted;

        public MetricsExporter(string basePath = "ProjectChimera/Metrics", bool enableLogging = false)
        {
            _basePath = basePath;
            _enableLogging = enableLogging;
            _lastExportTime = Time.time;

            // Ensure export directory exists
            EnsureDirectoryExists();
        }

        #region Export Operations

        /// <summary>
        /// Export metrics for a specific system
        /// </summary>
        public ExportResult ExportSystemMetrics(string systemName, MetricSnapshot[] snapshots, ExportFormat format = ExportFormat.CSV)
        {
            if (snapshots == null || snapshots.Length == 0)
            {
                return new ExportResult { Success = false, Message = "No data to export" };
            }

            try
            {
                var fileName = GenerateFileName(systemName, format);
                var filePath = Path.Combine(GetExportPath(), fileName);

                switch (format)
                {
                    case ExportFormat.CSV:
                        return ExportToCSV(filePath, snapshots);
                    case ExportFormat.JSON:
                        return ExportToJSON(filePath, snapshots);
                    case ExportFormat.XML:
                        return ExportToXML(filePath, snapshots);
                    default:
                        return new ExportResult { Success = false, Message = "Unsupported export format" };
                }
            }
            catch (System.Exception ex)
            {
                var result = new ExportResult
                {
                    Success = false,
                    Message = $"Export failed: {ex.Message}"
                };

                ChimeraLogger.LogError("METRICS", $"Export error for {systemName}: {ex.Message}", null);
                return result;
            }
        }

        /// <summary>
        /// Export aggregated metrics
        /// </summary>
        public ExportResult ExportAggregates(Dictionary<string, MetricAggregates> aggregates, ExportFormat format = ExportFormat.CSV)
        {
            if (aggregates == null || aggregates.Count == 0)
            {
                return new ExportResult { Success = false, Message = "No aggregates to export" };
            }

            try
            {
                var fileName = GenerateFileName("Aggregates", format);
                var filePath = Path.Combine(GetExportPath(), fileName);

                switch (format)
                {
                    case ExportFormat.CSV:
                        return ExportAggregatesToCSV(filePath, aggregates);
                    case ExportFormat.JSON:
                        return ExportAggregatesToJSON(filePath, aggregates);
                    default:
                        return new ExportResult { Success = false, Message = "Unsupported format for aggregates" };
                }
            }
            catch (System.Exception ex)
            {
                var result = new ExportResult
                {
                    Success = false,
                    Message = $"Aggregates export failed: {ex.Message}"
                };

                ChimeraLogger.LogError("METRICS", $"Aggregates export error: {ex.Message}", null);
                return result;
            }
        }

        /// <summary>
        /// Export performance report
        /// </summary>
        public ExportResult ExportPerformanceReport(Dictionary<string, MetricAggregates> aggregates, string reportName = "PerformanceReport")
        {
            try
            {
                var fileName = $"{reportName}_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
                var filePath = Path.Combine(GetExportPath(), fileName);

                var report = GeneratePerformanceReport(aggregates);

                File.WriteAllText(filePath, report);

                var result = new ExportResult
                {
                    Success = true,
                    FilePath = filePath,
                    Message = $"Performance report exported successfully"
                };

                OnExportCompleted?.Invoke(reportName, result);

                if (_enableLogging)
                {
                    ChimeraLogger.LogInfo("METRICS", $"Performance report exported: {filePath}", null);
                }

                return result;
            }
            catch (System.Exception ex)
            {
                var result = new ExportResult
                {
                    Success = false,
                    Message = $"Report export failed: {ex.Message}"
                };

                ChimeraLogger.LogError("METRICS", $"Report export error: {ex.Message}", null);
                return result;
            }
        }

        #endregion

        #region Format-Specific Export Methods

        private ExportResult ExportToCSV(string filePath, MetricSnapshot[] snapshots)
        {
            var csv = new StringBuilder();

            // Header
            var firstSnapshot = snapshots[0];
            var headers = new List<string> { "Timestamp", "SystemName" };
            headers.AddRange(firstSnapshot.Metrics.Keys);
            csv.AppendLine(string.Join(",", headers));

            // Data rows
            foreach (var snapshot in snapshots)
            {
                var row = new List<string>
                {
                    snapshot.Timestamp.ToString("F3"),
                    snapshot.SystemName
                };

                foreach (var header in headers.Skip(2))
                {
                    var value = snapshot.Metrics.TryGetValue(header, out var v) ? v.ToString("F3") : "0";
                    row.Add(value);
                }

                csv.AppendLine(string.Join(",", row));
            }

            File.WriteAllText(filePath, csv.ToString());

            var result = new ExportResult
            {
                Success = true,
                FilePath = filePath,
                Message = $"CSV export completed: {snapshots.Length} records"
            };

            OnExportCompleted?.Invoke(Path.GetFileNameWithoutExtension(filePath), result);
            return result;
        }

        private ExportResult ExportToJSON(string filePath, MetricSnapshot[] snapshots)
        {
            var json = JsonUtility.ToJson(new SerializableMetricArray { snapshots = snapshots }, true);
            File.WriteAllText(filePath, json);

            var result = new ExportResult
            {
                Success = true,
                FilePath = filePath,
                Message = $"JSON export completed: {snapshots.Length} records"
            };

            OnExportCompleted?.Invoke(Path.GetFileNameWithoutExtension(filePath), result);
            return result;
        }

        private ExportResult ExportToXML(string filePath, MetricSnapshot[] snapshots)
        {
            var xml = new StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            xml.AppendLine("<MetricsData>");

            foreach (var snapshot in snapshots)
            {
                xml.AppendLine($"  <Snapshot timestamp=\"{snapshot.Timestamp}\" system=\"{snapshot.SystemName}\">");
                foreach (var metric in snapshot.Metrics)
                {
                    xml.AppendLine($"    <Metric name=\"{metric.Key}\" value=\"{metric.Value}\" />");
                }
                xml.AppendLine("  </Snapshot>");
            }

            xml.AppendLine("</MetricsData>");

            File.WriteAllText(filePath, xml.ToString());

            var result = new ExportResult
            {
                Success = true,
                FilePath = filePath,
                Message = $"XML export completed: {snapshots.Length} records"
            };

            OnExportCompleted?.Invoke(Path.GetFileNameWithoutExtension(filePath), result);
            return result;
        }

        private ExportResult ExportAggregatesToCSV(string filePath, Dictionary<string, MetricAggregates> aggregates)
        {
            var csv = new StringBuilder();
            csv.AppendLine("System,Metric,Count,Min,Max,Average,Sum,StandardDeviation");

            foreach (var system in aggregates)
            {
                foreach (var stat in system.Value.Statistics)
                {
                    csv.AppendLine($"{system.Key},{stat.Key},{stat.Value.Count}," +
                                  $"{stat.Value.Min:F3},{stat.Value.Max:F3},{stat.Value.Average:F3}," +
                                  $"{stat.Value.Sum:F3},{stat.Value.StandardDeviation:F3}");
                }
            }

            File.WriteAllText(filePath, csv.ToString());

            var result = new ExportResult
            {
                Success = true,
                FilePath = filePath,
                Message = $"Aggregates CSV export completed: {aggregates.Count} systems"
            };

            OnExportCompleted?.Invoke(Path.GetFileNameWithoutExtension(filePath), result);
            return result;
        }

        private ExportResult ExportAggregatesToJSON(string filePath, Dictionary<string, MetricAggregates> aggregates)
        {
            // Convert to serializable format
            var serializableData = new SerializableAggregatesData();
            foreach (var kvp in aggregates)
            {
                serializableData.systems.Add(new SerializableSystemAggregates
                {
                    systemName = kvp.Key,
                    aggregates = kvp.Value
                });
            }

            var json = JsonUtility.ToJson(serializableData, true);
            File.WriteAllText(filePath, json);

            var result = new ExportResult
            {
                Success = true,
                FilePath = filePath,
                Message = $"Aggregates JSON export completed: {aggregates.Count} systems"
            };

            OnExportCompleted?.Invoke(Path.GetFileNameWithoutExtension(filePath), result);
            return result;
        }

        #endregion

        #region Report Generation

        /// <summary>
        /// Generate a performance report from aggregates (public API)
        /// </summary>
        public string GeneratePerformanceReport(Dictionary<string, MetricAggregates> aggregates)
        {
            return GeneratePerformanceReportInternal(aggregates);
        }

        private string GeneratePerformanceReportInternal(Dictionary<string, MetricAggregates> aggregates)
        {
            var report = new StringBuilder();
            report.AppendLine("PERFORMANCE METRICS REPORT");
            report.AppendLine("==========================");
            report.AppendLine($"Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Systems Analyzed: {aggregates.Count}");
            report.AppendLine();

            foreach (var system in aggregates.OrderBy(kvp => kvp.Key))
            {
                report.AppendLine($"SYSTEM: {system.Key}");
                report.AppendLine("----------");
                report.AppendLine($"Data Points: {system.Value.Count}");
                report.AppendLine($"Time Range: {system.Value.MinTimestamp:F1}s - {system.Value.MaxTimestamp:F1}s");
                report.AppendLine();

                foreach (var stat in system.Value.Statistics.OrderBy(kvp => kvp.Key))
                {
                    report.AppendLine($"  {stat.Key}:");
                    report.AppendLine($"    Min: {stat.Value.Min:F3}");
                    report.AppendLine($"    Max: {stat.Value.Max:F3}");
                    report.AppendLine($"    Avg: {stat.Value.Average:F3}");
                    report.AppendLine($"    StdDev: {stat.Value.StandardDeviation:F3}");
                }

                report.AppendLine();
            }

            return report.ToString();
        }

        #endregion

        #region Utility Methods

        private void EnsureDirectoryExists()
        {
            var fullPath = GetExportPath();
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                if (_enableLogging)
                {
                    ChimeraLogger.LogInfo("METRICS", $"Created export directory: {fullPath}", null);
                }
            }
        }

        private string GetExportPath()
        {
            return Path.Combine(Application.persistentDataPath, _basePath);
        }

        private string GenerateFileName(string prefix, ExportFormat format)
        {
            var timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var extension = format.ToString().ToLower();
            return $"{prefix}_{timestamp}.{extension}";
        }

        /// <summary>
        /// Get export status information
        /// </summary>
        public ExportStatus GetStatus()
        {
            var exportPath = GetExportPath();
            var files = Directory.Exists(exportPath) ? Directory.GetFiles(exportPath).Length : 0;

            return new ExportStatus
            {
                ExportPath = exportPath,
                ExportedFiles = files,
                LastExportTime = _lastExportTime,
                TimeSinceLastExport = Time.time - _lastExportTime
            };
        }

        #endregion
    }

    /// <summary>
    /// Export formats
    /// </summary>
    public enum ExportFormat
    {
        CSV,
        JSON,
        XML
    }

    /// <summary>
    /// Export result information
    /// </summary>
    [System.Serializable]
    public struct ExportResult
    {
        public bool Success;
        public string FilePath;
        public string Message;
    }

    /// <summary>
    /// Export status information
    /// </summary>
    [System.Serializable]
    public struct ExportStatus
    {
        public string ExportPath;
        public int ExportedFiles;
        public float LastExportTime;
        public float TimeSinceLastExport;
    }

    /// <summary>
    /// Serializable wrapper for metric arrays
    /// </summary>
    [System.Serializable]
    public class SerializableMetricArray
    {
        public MetricSnapshot[] snapshots;
    }

    /// <summary>
    /// Serializable wrapper for aggregates data
    /// </summary>
    [System.Serializable]
    public class SerializableAggregatesData
    {
        public List<SerializableSystemAggregates> systems = new List<SerializableSystemAggregates>();
    }

    /// <summary>
    /// Serializable system aggregates
    /// </summary>
    [System.Serializable]
    public class SerializableSystemAggregates
    {
        public string systemName;
        public MetricAggregates aggregates;
    }
}