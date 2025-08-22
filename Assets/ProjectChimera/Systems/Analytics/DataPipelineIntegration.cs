using UnityEngine;
using ProjectChimera.Systems.Services.Core;
using ProjectChimera.Data.Shared;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Phase 2.4.1: Data Pipeline Integration
    /// Provides comprehensive data collection, processing, and routing for all game systems.
    /// Implements ETL (Extract, Transform, Load) patterns with real-time streaming capabilities.
    /// </summary>
    public class DataPipelineIntegration : MonoBehaviour
    {
        [Header("Pipeline Configuration")]
        [SerializeField] private bool _enableDataCollection = true;
        [SerializeField] private bool _enableRealTimeProcessing = true;
        [SerializeField] private bool _enableBatchProcessing = true;
        [SerializeField] private bool _enableDataValidation = true;
        
        [Header("Collection Settings")]
        [SerializeField] private float _collectionInterval = 1f;
        [SerializeField] private int _maxEventsPerBatch = 100;
        [SerializeField] private int _bufferSize = 1000;
        [SerializeField] private bool _enableEventCompression = true;
        
        [Header("Processing Settings")]
        [SerializeField] private int _maxProcessingThreads = 2;
        [SerializeField] private float _processingTimeout = 30f;
        [SerializeField] private bool _enableStreamProcessing = true;
        [SerializeField] private bool _enableAggregation = true;
        
        [Header("Storage Settings")]
        [SerializeField] private bool _enableLocalStorage = true;
        [SerializeField] private bool _enableCloudSync = false;
        [SerializeField] private string _localStoragePath = "Analytics/Data";
        [SerializeField] private int _maxStorageSizeMB = 100;
        
        [Header("Data Quality")]
        [SerializeField] private bool _enableSchemaValidation = true;
        [SerializeField] private bool _enableDataCleaning = true;
        [SerializeField] private bool _enableDuplicateDetection = true;
        [SerializeField] private float _dataRetentionDays = 30f;
        
        // Core pipeline components
        private DataCollector _dataCollector;
        private DataProcessor _dataProcessor;
        private DataStorage _dataStorage;
        private DataValidator _dataValidator;
        
        // Pipeline queues and buffers
        private Queue<DataEvent> _collectionQueue = new Queue<DataEvent>();
        private Queue<ProcessedDataBatch> _processingQueue = new Queue<ProcessedDataBatch>();
        private List<DataStream> _activeStreams = new List<DataStream>();
        
        // Processing state
        private Dictionary<string, DataMetrics> _streamMetrics = new Dictionary<string, DataMetrics>();
        private Dictionary<string, DataSchema> _registeredSchemas = new Dictionary<string, DataSchema>();
        private bool _isProcessing = false;
        
        // Performance tracking
        private PipelineMetrics _pipelineMetrics = new PipelineMetrics();
        private float _lastMetricsUpdate;
        
        // Events
        public event Action<DataEvent> OnDataCollected;
        public event Action<ProcessedDataBatch> OnDataProcessed;
        public event Action<string, DataMetrics> OnStreamMetricsUpdated;
        public event Action<DataPipelineAlert> OnPipelineAlert;
        
        private void Awake()
        {
            InitializeDataPipeline();
        }
        
        private void Start()
        {
            StartPipeline();
            RegisterDefaultSchemas();
            StartCoroutine(PipelineUpdateLoop());
        }
        
        private void InitializeDataPipeline()
        {
            // Initialize core components
            _dataCollector = new DataCollector(this);
            _dataProcessor = new DataProcessor(_maxProcessingThreads);
            _dataStorage = new DataStorage(_localStoragePath, _maxStorageSizeMB);
            _dataValidator = new DataValidator();
            
            // Configure collection settings
            _dataCollector.SetCollectionInterval(_collectionInterval);
            _dataCollector.SetBufferSize(_bufferSize);
            _dataCollector.EnableCompression(_enableEventCompression);
            
            // Configure processing settings
            _dataProcessor.SetTimeout(_processingTimeout);
            _dataProcessor.EnableAggregation(_enableAggregation);
            _dataProcessor.EnableStreamProcessing(_enableStreamProcessing);
            
            // Configure storage settings
            _dataStorage.EnableLocalStorage(_enableLocalStorage);
            _dataStorage.EnableCloudSync(_enableCloudSync);
            _dataStorage.SetRetentionDays(_dataRetentionDays);
            
            // Configure validation settings
            _dataValidator.EnableSchemaValidation(_enableSchemaValidation);
            _dataValidator.EnableDataCleaning(_enableDataCleaning);
            _dataValidator.EnableDuplicateDetection(_enableDuplicateDetection);
        }
        
        private void StartPipeline()
        {
            if (!_enableDataCollection)
            {
                Debug.LogWarning("[DataPipelineIntegration] Data collection disabled - pipeline inactive");
                return;
            }
            
            _isProcessing = true;
            
            // Start collection
            _dataCollector.StartCollection();
            
            // Start processing threads
            if (_enableRealTimeProcessing)
            {
                _dataProcessor.StartRealTimeProcessing();
            }
            
            if (_enableBatchProcessing)
            {
                _dataProcessor.StartBatchProcessing();
            }
            
            // Initialize storage
            _dataStorage.Initialize();
            
            Debug.Log("[DataPipelineIntegration] Data pipeline started successfully");
        }
        
        /// <summary>
        /// Register a data stream for collection and processing
        /// </summary>
        public void RegisterDataStream(string streamId, DataStreamConfig config)
        {
            if (string.IsNullOrEmpty(streamId))
            {
                Debug.LogError("[DataPipelineIntegration] Stream ID cannot be null or empty");
                return;
            }
            
            var stream = new DataStream
            {
                StreamId = streamId,
                Config = config,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                EventCount = 0,
                LastEventTime = DateTime.UtcNow
            };
            
            _activeStreams.Add(stream);
            _streamMetrics[streamId] = new DataMetrics();
            
            Debug.Log($"[DataPipelineIntegration] Registered data stream: {streamId}");
        }
        
        /// <summary>
        /// Collect a data event and add it to the pipeline
        /// </summary>
        public void CollectEvent(string streamId, string eventType, object data, Dictionary<string, object> metadata = null)
        {
            if (!_enableDataCollection || !_isProcessing)
                return;
            
            var dataEvent = new DataEvent
            {
                EventId = Guid.NewGuid().ToString(),
                StreamId = streamId,
                EventType = eventType,
                Data = data,
                Metadata = metadata ?? new Dictionary<string, object>(),
                Timestamp = DateTime.UtcNow,
                SessionId = GetCurrentSessionId()
            };
            
            // Validate event if enabled
            if (_enableDataValidation)
            {
                var validationResult = _dataValidator.ValidateEvent(dataEvent);
                if (!validationResult.IsValid)
                {
                    OnPipelineAlert?.Invoke(new DataPipelineAlert
                    {
                        AlertType = PipelineAlertType.ValidationError,
                        Message = $"Event validation failed: {validationResult.ErrorMessage}",
                        StreamId = streamId,
                        Timestamp = DateTime.UtcNow
                    });
                    return;
                }
            }
            
            // Add to collection queue
            _collectionQueue.Enqueue(dataEvent);
            
            // Update stream metrics
            UpdateStreamMetrics(streamId, dataEvent);
            
            OnDataCollected?.Invoke(dataEvent);
        }
        
        /// <summary>
        /// Register a data schema for validation
        /// </summary>
        public void RegisterSchema(string schemaId, DataSchema schema)
        {
            if (schema == null || string.IsNullOrEmpty(schemaId))
            {
                Debug.LogError("[DataPipelineIntegration] Invalid schema or schema ID");
                return;
            }
            
            _registeredSchemas[schemaId] = schema;
            _dataValidator.RegisterSchema(schemaId, schema);
            
            Debug.Log($"[DataPipelineIntegration] Registered data schema: {schemaId}");
        }
        
        /// <summary>
        /// Get pipeline performance metrics
        /// </summary>
        public PipelineMetrics GetPipelineMetrics()
        {
            return _pipelineMetrics;
        }
        
        /// <summary>
        /// Get metrics for a specific data stream
        /// </summary>
        public DataMetrics GetStreamMetrics(string streamId)
        {
            return _streamMetrics.TryGetValue(streamId, out var metrics) ? metrics : null;
        }
        
        /// <summary>
        /// Process collected events in batches
        /// </summary>
        private async Task ProcessEventBatch()
        {
            if (_collectionQueue.Count == 0)
                return;
            
            var batchSize = Mathf.Min(_maxEventsPerBatch, _collectionQueue.Count);
            var batch = new List<DataEvent>();
            
            // Extract events from queue
            for (int i = 0; i < batchSize; i++)
            {
                if (_collectionQueue.Count > 0)
                {
                    batch.Add(_collectionQueue.Dequeue());
                }
            }
            
            if (batch.Count == 0)
                return;
            
            try
            {
                // Process the batch
                var processedBatch = await _dataProcessor.ProcessBatch(batch);
                
                if (processedBatch != null)
                {
                    // Store processed data
                    await _dataStorage.StoreBatch(processedBatch);
                    
                    // Add to processing queue for further analysis
                    _processingQueue.Enqueue(processedBatch);
                    
                    // Update metrics
                    UpdatePipelineMetrics(processedBatch);
                    
                    OnDataProcessed?.Invoke(processedBatch);
                }
            }
            catch (Exception ex)
            {
                OnPipelineAlert?.Invoke(new DataPipelineAlert
                {
                    AlertType = PipelineAlertType.ProcessingError,
                    Message = $"Batch processing failed: {ex.Message}",
                    Timestamp = DateTime.UtcNow
                });
                
                Debug.LogError($"[DataPipelineIntegration] Batch processing error: {ex.Message}");
            }
        }
        
        private void RegisterDefaultSchemas()
        {
            // Player action schema
            RegisterSchema("player_action", new DataSchema
            {
                SchemaId = "player_action",
                Version = "1.0",
                Fields = new Dictionary<string, FieldDefinition>
                {
                    ["action_type"] = new FieldDefinition { Type = FieldType.String, Required = true },
                    ["player_id"] = new FieldDefinition { Type = FieldType.String, Required = true },
                    ["position"] = new FieldDefinition { Type = FieldType.Object, Required = false },
                    ["timestamp"] = new FieldDefinition { Type = FieldType.DateTime, Required = true }
                }
            });
            
            // System performance schema
            RegisterSchema("system_performance", new DataSchema
            {
                SchemaId = "system_performance",
                Version = "1.0",
                Fields = new Dictionary<string, FieldDefinition>
                {
                    ["fps"] = new FieldDefinition { Type = FieldType.Float, Required = true },
                    ["memory_usage"] = new FieldDefinition { Type = FieldType.Long, Required = true },
                    ["cpu_usage"] = new FieldDefinition { Type = FieldType.Float, Required = false },
                    ["active_objects"] = new FieldDefinition { Type = FieldType.Integer, Required = false }
                }
            });
            
            // Game event schema
            RegisterSchema("game_event", new DataSchema
            {
                SchemaId = "game_event",
                Version = "1.0",
                Fields = new Dictionary<string, FieldDefinition>
                {
                    ["event_name"] = new FieldDefinition { Type = FieldType.String, Required = true },
                    ["event_data"] = new FieldDefinition { Type = FieldType.Object, Required = false },
                    ["session_id"] = new FieldDefinition { Type = FieldType.String, Required = true },
                    ["user_id"] = new FieldDefinition { Type = FieldType.String, Required = false }
                }
            });
        }
        
        private void UpdateStreamMetrics(string streamId, DataEvent dataEvent)
        {
            if (!_streamMetrics.TryGetValue(streamId, out var metrics))
            {
                metrics = new DataMetrics();
                _streamMetrics[streamId] = metrics;
            }
            
            metrics.TotalEvents++;
            metrics.LastEventTime = dataEvent.Timestamp;
            metrics.EventsPerSecond = CalculateEventsPerSecond(streamId);
            
            // Update data size metrics
            var eventSize = EstimateEventSize(dataEvent);
            metrics.TotalDataSize += eventSize;
            metrics.AverageEventSize = metrics.TotalDataSize / metrics.TotalEvents;
            
            // Update stream in active streams list
            var stream = _activeStreams.Find(s => s.StreamId == streamId);
            if (stream != null)
            {
                stream.EventCount = metrics.TotalEvents;
                stream.LastEventTime = dataEvent.Timestamp;
            }
            
            OnStreamMetricsUpdated?.Invoke(streamId, metrics);
        }
        
        private void UpdatePipelineMetrics(ProcessedDataBatch batch)
        {
            _pipelineMetrics.TotalBatchesProcessed++;
            _pipelineMetrics.TotalEventsProcessed += batch.Events.Count;
            _pipelineMetrics.LastProcessingTime = DateTime.UtcNow;
            _pipelineMetrics.ProcessingLatency = CalculateProcessingLatency(batch);
            _pipelineMetrics.DataThroughput = CalculateDataThroughput();
            _pipelineMetrics.ErrorRate = CalculateErrorRate();
        }
        
        private float CalculateEventsPerSecond(string streamId)
        {
            var stream = _activeStreams.Find(s => s.StreamId == streamId);
            if (stream == null) return 0f;
            
            var timeSpan = DateTime.UtcNow - stream.CreatedAt;
            if (timeSpan.TotalSeconds <= 0) return 0f;
            
            return (float)(stream.EventCount / timeSpan.TotalSeconds);
        }
        
        private long EstimateEventSize(DataEvent dataEvent)
        {
            // Simplified size estimation
            long size = 0;
            
            size += dataEvent.EventId?.Length * 2 ?? 0; // UTF-16 encoding
            size += dataEvent.StreamId?.Length * 2 ?? 0;
            size += dataEvent.EventType?.Length * 2 ?? 0;
            size += 8; // Timestamp
            size += EstimateObjectSize(dataEvent.Data);
            
            if (dataEvent.Metadata != null)
            {
                foreach (var kvp in dataEvent.Metadata)
                {
                    size += kvp.Key?.Length * 2 ?? 0;
                    size += EstimateObjectSize(kvp.Value);
                }
            }
            
            return size;
        }
        
        private long EstimateObjectSize(object obj)
        {
            if (obj == null) return 0;
            
            switch (obj)
            {
                case string str: return str.Length * 2;
                case int: return 4;
                case long: return 8;
                case float: return 4;
                case double: return 8;
                case bool: return 1;
                case Vector3: return 12;
                case Vector2: return 8;
                default: return 64; // Default estimation for complex objects
            }
        }
        
        private float CalculateProcessingLatency(ProcessedDataBatch batch)
        {
            if (batch.Events.Count == 0) return 0f;
            
            var oldestEvent = batch.Events.Min(e => e.Timestamp);
            var latency = DateTime.UtcNow - oldestEvent;
            
            return (float)latency.TotalMilliseconds;
        }
        
        private float CalculateDataThroughput()
        {
            var totalEvents = _streamMetrics.Values.Sum(m => m.TotalEvents);
            var timeSpan = Time.time - _lastMetricsUpdate;
            
            return timeSpan > 0 ? totalEvents / timeSpan : 0f;
        }
        
        private float CalculateErrorRate()
        {
            var totalEvents = _streamMetrics.Values.Sum(m => m.TotalEvents);
            var errorEvents = _pipelineMetrics.ErrorCount;
            
            return totalEvents > 0 ? (float)errorEvents / totalEvents : 0f;
        }
        
        private string GetCurrentSessionId()
        {
            // In a real implementation, this would be managed by a session manager
            return "session_" + Time.time.ToString("F0");
        }
        
        private IEnumerator PipelineUpdateLoop()
        {
            while (_isProcessing)
            {
                yield return new WaitForSeconds(_collectionInterval);
                
                // Process event batches
                if (_enableBatchProcessing && _collectionQueue.Count > 0)
                {
                    _ = ProcessEventBatch(); // Fire and forget async processing
                }
                
                // Update metrics
                if (Time.time - _lastMetricsUpdate >= 1f)
                {
                    _lastMetricsUpdate = Time.time;
                    UpdateOverallMetrics();
                }
                
                // Cleanup old data
                CleanupOldData();
                
                // Check pipeline health
                CheckPipelineHealth();
            }
        }
        
        private void UpdateOverallMetrics()
        {
            _pipelineMetrics.ActiveStreams = _activeStreams.Count(s => s.IsActive);
            _pipelineMetrics.QueueSize = _collectionQueue.Count;
            _pipelineMetrics.MemoryUsage = GC.GetTotalMemory(false);
            _pipelineMetrics.Uptime = Time.time;
        }
        
        private void CleanupOldData()
        {
            // Remove old streams that haven't had events recently
            var cutoffTime = DateTime.UtcNow.AddHours(-1);
            
            _activeStreams.RemoveAll(stream => 
                stream.LastEventTime < cutoffTime && 
                stream.EventCount == 0);
            
            // Clean up storage if needed
            _dataStorage.CleanupOldData();
        }
        
        private void CheckPipelineHealth()
        {
            // Check queue sizes
            if (_collectionQueue.Count > _bufferSize * 0.9f)
            {
                OnPipelineAlert?.Invoke(new DataPipelineAlert
                {
                    AlertType = PipelineAlertType.Warning,
                    Message = $"Collection queue near capacity: {_collectionQueue.Count}/{_bufferSize}",
                    Timestamp = DateTime.UtcNow
                });
            }
            
            // Check processing latency
            if (_pipelineMetrics.ProcessingLatency > _processingTimeout * 1000f * 0.8f)
            {
                OnPipelineAlert?.Invoke(new DataPipelineAlert
                {
                    AlertType = PipelineAlertType.Warning,
                    Message = $"High processing latency: {_pipelineMetrics.ProcessingLatency:F2}ms",
                    Timestamp = DateTime.UtcNow
                });
            }
            
            // Check error rate
            if (_pipelineMetrics.ErrorRate > 0.05f) // 5% error rate
            {
                OnPipelineAlert?.Invoke(new DataPipelineAlert
                {
                    AlertType = PipelineAlertType.Error,
                    Message = $"High error rate: {_pipelineMetrics.ErrorRate:P2}",
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        
        private void OnDestroy()
        {
            _isProcessing = false;
            
            // Clean shutdown
            _dataCollector?.StopCollection();
            _dataProcessor?.StopProcessing();
            _dataStorage?.Shutdown();
        }
        
        // Public API
        public int GetQueueSize() => _collectionQueue.Count;
        public int GetActiveStreamCount() => _activeStreams.Count(s => s.IsActive);
        public bool IsProcessing() => _isProcessing;
        public void SetCollectionEnabled(bool enabled) => _enableDataCollection = enabled;
        public void SetRealTimeProcessingEnabled(bool enabled) => _enableRealTimeProcessing = enabled;
        public void FlushPipeline() => _ = ProcessEventBatch();
    }
}