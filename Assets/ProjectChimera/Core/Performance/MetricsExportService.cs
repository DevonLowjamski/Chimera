using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ProjectChimera.Shared;

namespace ProjectChimera.Core.Performance
{
    /// <summary>
    /// REFACTORED: Metrics Export Service
    /// Single Responsibility: Metric data export, file management, and format conversion
    /// Extracted from MetricsCollectionFramework for better separation of concerns
    /// </summary>
    public class MetricsExportService : MonoBehaviour
    {
        [Header("Export Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _autoExportMetrics = false;
        [SerializeField] private float _exportInterval = 300f; // 5 minutes
        [SerializeField] private string _exportPath = "ProjectChimera/Metrics";

        [Header("Format Settings")]
        [SerializeField] private ExportFormat _defaultExportFormat = ExportFormat.CSV;
        [SerializeField] private bool _includeTimestamps = true;
        [SerializeField] private bool _compressExports = false;
        [SerializeField] private int _maxFilesPerDirectory = 100;

        // Export tracking
        private float _lastExportTime;
        private bool _isInitialized = false;
        private string _fullExportPath;

        // Statistics
        private MetricsExportStats _stats = new MetricsExportStats();

        // Events
        public event System.Action<string, List<MetricSnapshot>> OnMetricsExported;
        public event System.Action<string, ExportResult> OnExportCompleted;
        public event System.Action<string> OnExportFailed;

        public bool IsInitialized => _isInitialized;
        public MetricsExportStats Stats => _stats;
        public string ExportPath => _fullExportPath;

        public void Initialize()
        {
            if (_isInitialized) return;

            SetupExportPath();
            ResetStats();

            _isInitialized = true;

            if (_enableLogging)
            {
                SharedLogger.Log("METRICS", $"Metrics Export Service initialized - Export path: {_fullExportPath}", this);
            }
        }

        /// <summary>
        /// Process automatic export (call periodically)
        /// </summary>
        public void ProcessAutoExport(Dictionary<string, List<MetricSnapshot>> systemMetrics)
        {
            if (!_isInitialized || !_autoExportMetrics) return;

            if (Time.time - _lastExportTime >= _exportInterval)
            {
                ExportAllMetrics(systemMetrics);
                _lastExportTime = Time.time;
            }
        }

        /// <summary>
        /// Export all metrics to files
        /// </summary>
        public void ExportAllMetrics(Dictionary<string, List<MetricSnapshot>> systemMetrics)
        {
            if (!_isInitialized || systemMetrics == null)
            {
                if (_enableLogging)
                {
                    SharedLogger.LogWarning("METRICS", "Cannot export - service not initialized or no metrics provided", this);
                }
                return;
            }

            var exportBatch = new ExportBatch
            {
                BatchId = System.Guid.NewGuid().ToString(),
                ExportTime = System.DateTime.Now,
                Format = _defaultExportFormat
            };

            foreach (var kvp in systemMetrics)
            {
                if (kvp.Value?.Count > 0)
                {
                    ExportSystemMetrics(kvp.Key, kvp.Value, exportBatch);
                }
            }

            // Create batch summary
            CreateBatchSummary(exportBatch);

            _stats.ExportBatches++;
            _stats.LastExportTime = Time.time;

            if (_enableLogging)
            {
                SharedLogger.Log("METRICS", $"Exported metrics batch {exportBatch.BatchId} - {exportBatch.SystemsExported} systems", this);
            }
        }

        /// <summary>
        /// Export metrics for a specific system
        /// </summary>
        public ExportResult ExportSystemMetrics(string systemName, List<MetricSnapshot> metrics, ExportBatch batch = null)
        {
            if (!_isInitialized || string.IsNullOrEmpty(systemName) || metrics == null || metrics.Count == 0)
            {
                var failResult = new ExportResult
                {
                    Success = false,
                    FilePath = string.Empty,
                    Message = "Invalid parameters for export"
                };
                OnExportFailed?.Invoke($"Export failed for {systemName}: {failResult.Message}");
                return failResult;
            }

            try
            {
                var exportStartTime = System.DateTime.Now;
                var fileName = GenerateFileName(systemName, _defaultExportFormat, exportStartTime);
                var filePath = Path.Combine(_fullExportPath, fileName);

                // Export based on format
                var exportedBytes = 0;
                switch (_defaultExportFormat)
                {
                    case ExportFormat.CSV:
                        exportedBytes = ExportToCSV(filePath, metrics);
                        break;
                    case ExportFormat.JSON:
                        exportedBytes = ExportToJSON(filePath, metrics);
                        break;
                    case ExportFormat.XML:
                        exportedBytes = ExportToXML(filePath, metrics);
                        break;
                    default:
                        exportedBytes = ExportToCSV(filePath, metrics);
                        break;
                }

                // Compress if enabled
                if (_compressExports)
                {
                    var compressedPath = CompressFile(filePath);
                    if (!string.IsNullOrEmpty(compressedPath))
                    {
                        File.Delete(filePath);
                        filePath = compressedPath;
                    }
                }

                var result = new ExportResult
                {
                    Success = true,
                    FilePath = filePath,
                    Message = $"Exported {metrics.Count} records ({exportedBytes} bytes)"
                };

                // Update batch if provided
                if (batch != null)
                {
                    batch.SystemsExported++;
                    batch.TotalRecords += metrics.Count;
                    batch.TotalBytes += exportedBytes;
                }

                // Update statistics
                _stats.SystemsExported++;
                _stats.TotalRecordsExported += metrics.Count;
                _stats.TotalBytesExported += exportedBytes;

                OnMetricsExported?.Invoke(systemName, metrics);
                OnExportCompleted?.Invoke(systemName, result);

                if (_enableLogging)
                {
                    SharedLogger.Log("METRICS", $"Exported {metrics.Count} metrics for {systemName} to {fileName} ({exportedBytes} bytes)", this);
                }

                return result;
            }
            catch (System.Exception ex)
            {
                var errorResult = new ExportResult
                {
                    Success = false,
                    FilePath = string.Empty,
                    Message = ex.Message
                };

                _stats.ExportErrors++;
                OnExportFailed?.Invoke($"Export failed for {systemName}: {ex.Message}");

                if (_enableLogging)
                {
                    SharedLogger.LogError("METRICS", $"Export failed for {systemName}: {ex.Message}", this);
                }

                return errorResult;
            }
        }

        /// <summary>
        /// Export metrics to CSV format
        /// </summary>
        private int ExportToCSV(string filePath, List<MetricSnapshot> metrics)
        {
            var csv = new StringBuilder();

            // Header
            var headers = new List<string> { "SystemName", "Timestamp", "FrameCount", "UpdateTime", "UpdateCount" };

            // Add custom metric headers
            var allCustomKeys = metrics
                .Where(m => m.CustomMetrics != null)
                .SelectMany(m => m.CustomMetrics.Keys)
                .Distinct()
                .OrderBy(k => k)
                .ToList();
            headers.AddRange(allCustomKeys);

            csv.AppendLine(string.Join(",", headers));

            // Data rows
            foreach (var metric in metrics)
            {
                var row = new List<string>
                {
                    EscapeCSV(metric.SystemName ?? ""),
                    _includeTimestamps ? metric.Timestamp.ToString("F3") : "",
                    metric.FrameCount.ToString(),
                    metric.UpdateTime.ToString("F3"),
                    metric.UpdateCount.ToString()
                };

                // Add custom metric values
                foreach (var key in allCustomKeys)
                {
                    var value = "";
                    if (metric.CustomMetrics?.TryGetValue(key, out var obj) == true)
                    {
                        value = obj?.ToString() ?? "";
                    }
                    row.Add(EscapeCSV(value));
                }

                csv.AppendLine(string.Join(",", row));
            }

            var content = csv.ToString();
            File.WriteAllText(filePath, content);
            return System.Text.Encoding.UTF8.GetByteCount(content);
        }

        /// <summary>
        /// Export metrics to JSON format
        /// </summary>
        private int ExportToJSON(string filePath, List<MetricSnapshot> metrics)
        {
            var content = JsonUtility.ToJson(new MetricExportData { Metrics = metrics.ToArray() }, true);
            File.WriteAllText(filePath, content);
            return System.Text.Encoding.UTF8.GetByteCount(content);
        }

        /// <summary>
        /// Export metrics to XML format
        /// </summary>
        private int ExportToXML(string filePath, List<MetricSnapshot> metrics)
        {
            var xml = new StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            xml.AppendLine("<MetricExport>");

            foreach (var metric in metrics)
            {
                xml.AppendLine("  <Metric>");
                xml.AppendLine($"    <SystemName>{EscapeXML(metric.SystemName ?? "")}</SystemName>");
                if (_includeTimestamps)
                    xml.AppendLine($"    <Timestamp>{metric.Timestamp}</Timestamp>");
                xml.AppendLine($"    <FrameCount>{metric.FrameCount}</FrameCount>");
                xml.AppendLine($"    <UpdateTime>{metric.UpdateTime}</UpdateTime>");
                xml.AppendLine($"    <UpdateCount>{metric.UpdateCount}</UpdateCount>");

                if (metric.CustomMetrics != null)
                {
                    xml.AppendLine("    <CustomMetrics>");
                    foreach (var kvp in metric.CustomMetrics)
                    {
                        xml.AppendLine($"      <{EscapeXML(kvp.Key)}>{EscapeXML(kvp.Value?.ToString() ?? "")}</{EscapeXML(kvp.Key)}>");
                    }
                    xml.AppendLine("    </CustomMetrics>");
                }

                xml.AppendLine("  </Metric>");
            }

            xml.AppendLine("</MetricExport>");

            var content = xml.ToString();
            File.WriteAllText(filePath, content);
            return System.Text.Encoding.UTF8.GetByteCount(content);
        }

        /// <summary>
        /// Generate filename for export
        /// </summary>
        private string GenerateFileName(string systemName, ExportFormat format, System.DateTime exportTime)
        {
            var timestamp = exportTime.ToString("yyyy-MM-dd_HH-mm-ss");
            var extension = format.ToString().ToLower();
            return $"metrics_{systemName}_{timestamp}.{extension}";
        }

        /// <summary>
        /// Setup export directory path
        /// </summary>
        private void SetupExportPath()
        {
            _fullExportPath = Path.Combine(Application.persistentDataPath, _exportPath);

            try
            {
                Directory.CreateDirectory(_fullExportPath);
            }
            catch (System.Exception ex)
            {
                if (_enableLogging)
                {
                    SharedLogger.LogError("METRICS", $"Failed to create export directory: {ex.Message}", this);
                }
            }
        }

        /// <summary>
        /// Create batch summary file
        /// </summary>
        private void CreateBatchSummary(ExportBatch batch)
        {
            try
            {
                var summaryFileName = $"export_batch_{batch.ExportTime:yyyy-MM-dd_HH-mm-ss}_summary.txt";
                var summaryPath = Path.Combine(_fullExportPath, summaryFileName);

                var summary = new StringBuilder();
                summary.AppendLine($"Export Batch Summary");
                summary.AppendLine($"Batch ID: {batch.BatchId}");
                summary.AppendLine($"Export Time: {batch.ExportTime}");
                summary.AppendLine($"Format: {batch.Format}");
                summary.AppendLine($"Systems Exported: {batch.SystemsExported}");
                summary.AppendLine($"Total Records: {batch.TotalRecords}");
                summary.AppendLine($"Total Size: {batch.TotalBytes} bytes");

                File.WriteAllText(summaryPath, summary.ToString());
            }
            catch (System.Exception ex)
            {
                if (_enableLogging)
                {
                    SharedLogger.LogError("METRICS", $"Failed to create batch summary: {ex.Message}", this);
                }
            }
        }

        /// <summary>
        /// Compress exported file
        /// </summary>
        private string CompressFile(string filePath)
        {
            // Placeholder for compression implementation
            // Could use System.IO.Compression.GZipStream
            return null;
        }

        /// <summary>
        /// Escape CSV values
        /// </summary>
        private string EscapeCSV(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }

        /// <summary>
        /// Escape XML values
        /// </summary>
        private string EscapeXML(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("&", "&amp;")
                       .Replace("<", "&lt;")
                       .Replace(">", "&gt;")
                       .Replace("\"", "&quot;")
                       .Replace("'", "&apos;");
        }

        /// <summary>
        /// Set export format
        /// </summary>
        public void SetExportFormat(ExportFormat format)
        {
            _defaultExportFormat = format;

            if (_enableLogging)
            {
                SharedLogger.Log("METRICS", $"Export format set to {format}", this);
            }
        }

        /// <summary>
        /// Reset statistics
        /// </summary>
        private void ResetStats()
        {
            _stats = new MetricsExportStats
            {
                ExportBatches = 0,
                SystemsExported = 0,
                TotalRecordsExported = 0,
                TotalBytesExported = 0,
                ExportErrors = 0,
                LastExportTime = Time.time
            };
        }
    }





    /// <summary>
    /// Export batch tracking
    /// </summary>
    [System.Serializable]
    public class ExportBatch
    {
        public string BatchId;
        public System.DateTime ExportTime;
        public ExportFormat Format;
        public int SystemsExported;
        public int TotalRecords;
        public int TotalBytes;
    }

    /// <summary>
    /// JSON export wrapper
    /// </summary>
    [System.Serializable]
    public class MetricExportData
    {
        public MetricSnapshot[] Metrics;
    }

    /// <summary>
    /// Export statistics
    /// </summary>
    [System.Serializable]
    public struct MetricsExportStats
    {
        public int ExportBatches;
        public int SystemsExported;
        public int TotalRecordsExported;
        public int TotalBytesExported;
        public int ExportErrors;
        public float LastExportTime;
    }
}
